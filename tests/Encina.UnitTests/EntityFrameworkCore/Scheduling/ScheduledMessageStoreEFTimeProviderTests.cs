using Encina.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Scheduling;
using Encina.Testing.Shouldly;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.EntityFrameworkCore.Scheduling;

/// <summary>
/// Tests for <see cref="ScheduledMessageStoreEF"/> exercising custom TimeProvider injection
/// and full lifecycle flows that cover timestamp-dependent code paths.
/// </summary>
[Trait("Category", "Unit")]
public class ScheduledMessageStoreEFTimeProviderTests : IDisposable
{
    private readonly TestDbContext _dbContext;
    private readonly FakeTimeProvider _timeProvider;
    private readonly ScheduledMessageStoreEF _store;

    public ScheduledMessageStoreEFTimeProviderTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestDbContext(options);
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero));
        _store = new ScheduledMessageStoreEF(_dbContext, _timeProvider);
    }

    [Fact]
    public async Task MarkAsProcessedAsync_UsesInjectedTimeProvider()
    {
        // Arrange
        var baseTime = _timeProvider.GetUtcNow().UtcDateTime;
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "SendReminderCommand",
            Content = "{\"userId\":\"u1\"}",
            ScheduledAtUtc = baseTime.AddMinutes(-10),
            CreatedAtUtc = baseTime.AddHours(-1),
            RetryCount = 0,
            IsRecurring = false
        };

        await _dbContext.ScheduledMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        // Act
        (await _store.MarkAsProcessedAsync(message.Id)).ShouldBeRight();
        (await _store.SaveChangesAsync()).ShouldBeRight();

        // Assert - both ProcessedAtUtc and LastExecutedAtUtc should use FakeTimeProvider
        var updated = await _dbContext.ScheduledMessages.FindAsync(message.Id);
        updated!.ProcessedAtUtc.ShouldNotBeNull();
        updated.ProcessedAtUtc!.Value.ShouldBe(baseTime, TimeSpan.FromSeconds(1));
        updated.LastExecutedAtUtc.ShouldNotBeNull();
        updated.LastExecutedAtUtc!.Value.ShouldBe(baseTime, TimeSpan.FromSeconds(1));
        updated.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task GetDueMessagesAsync_UsesInjectedTimeProvider()
    {
        // Arrange
        var baseTime = _timeProvider.GetUtcNow().UtcDateTime;

        // Due message (scheduled in the past)
        var dueMessage = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "DueCommand",
            Content = "{}",
            ScheduledAtUtc = baseTime.AddMinutes(-5),
            CreatedAtUtc = baseTime.AddHours(-1),
            RetryCount = 0,
            IsRecurring = false
        };

        // Future message (scheduled 1 hour from now)
        var futureMessage = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "FutureCommand",
            Content = "{}",
            ScheduledAtUtc = baseTime.AddHours(1),
            CreatedAtUtc = baseTime,
            RetryCount = 0,
            IsRecurring = false
        };

        await _dbContext.ScheduledMessages.AddRangeAsync(dueMessage, futureMessage);
        await _dbContext.SaveChangesAsync();

        // Act
        var due = (await _store.GetDueMessagesAsync(batchSize: 10, maxRetries: 3)).ShouldBeRight();

        // Assert
        due.Count().ShouldBe(1);
        due.First().Id.ShouldBe(dueMessage.Id);
    }

    [Fact]
    public async Task GetDueMessagesAsync_AfterTimeAdvance_IncludesFutureMessages()
    {
        // Arrange
        var baseTime = _timeProvider.GetUtcNow().UtcDateTime;

        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "DelayedCommand",
            Content = "{}",
            ScheduledAtUtc = baseTime.AddMinutes(30), // 30 min in future
            CreatedAtUtc = baseTime,
            RetryCount = 0,
            IsRecurring = false
        };

        await _dbContext.ScheduledMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        // Act 1 - not due yet
        var before = (await _store.GetDueMessagesAsync(batchSize: 10, maxRetries: 3)).ShouldBeRight();
        before.ShouldBeEmpty();

        // Advance time past the scheduled time
        _timeProvider.Advance(TimeSpan.FromMinutes(45));

        // Act 2 - now due
        var after = (await _store.GetDueMessagesAsync(batchSize: 10, maxRetries: 3)).ShouldBeRight();
        after.Count().ShouldBe(1);
        after.First().Id.ShouldBe(message.Id);
    }

    [Fact]
    public async Task RescheduleRecurringMessageAsync_FullFieldReset()
    {
        // Arrange - recurring message that had errors and was processed
        var baseTime = _timeProvider.GetUtcNow().UtcDateTime;
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "DailyCleanupCommand",
            Content = "{\"scope\":\"temp_files\"}",
            ScheduledAtUtc = baseTime.AddHours(-1),
            CreatedAtUtc = baseTime.AddDays(-30),
            ProcessedAtUtc = baseTime.AddMinutes(-30),
            ErrorMessage = "Partial failure: 2 files locked",
            RetryCount = 3,
            NextRetryAtUtc = baseTime.AddMinutes(15),
            IsRecurring = true,
            CronExpression = "0 2 * * *",
            LastExecutedAtUtc = baseTime.AddMinutes(-30)
        };

        await _dbContext.ScheduledMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        var nextScheduled = baseTime.AddDays(1).Date.AddHours(2); // next 2 AM

        // Act
        (await _store.RescheduleRecurringMessageAsync(message.Id, nextScheduled)).ShouldBeRight();
        (await _store.SaveChangesAsync()).ShouldBeRight();

        // Assert - all retry/error state should be reset
        var updated = await _dbContext.ScheduledMessages.FindAsync(message.Id);
        updated!.ScheduledAtUtc.ShouldBe(nextScheduled);
        updated.ProcessedAtUtc.ShouldBeNull();
        updated.ErrorMessage.ShouldBeNull();
        updated.RetryCount.ShouldBe(0);
        updated.NextRetryAtUtc.ShouldBeNull();
        // IsRecurring and CronExpression should be unchanged
        updated.IsRecurring.ShouldBeTrue();
        updated.CronExpression.ShouldBe("0 2 * * *");
    }

    [Fact]
    public async Task CancelAsync_NonExistentMessage_ReturnsRight()
    {
        // Act - cancel a message that doesn't exist
        var result = await _store.CancelAsync(Guid.NewGuid());

        // Assert - should succeed silently
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task FullLifecycle_ScheduleFailRescheduleAndProcess()
    {
        // Arrange
        var baseTime = _timeProvider.GetUtcNow().UtcDateTime;
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "CancelUnpaidOrderCommand",
            Content = "{\"orderId\":\"ORD-789\"}",
            ScheduledAtUtc = baseTime.AddMinutes(30), // due in 30 min
            CreatedAtUtc = baseTime,
            RetryCount = 0,
            IsRecurring = false,
            CorrelationId = "corr-order-789"
        };

        // Step 1: Schedule
        (await _store.AddAsync(message)).ShouldBeRight();
        (await _store.SaveChangesAsync()).ShouldBeRight();

        // Not due yet
        var notDue = (await _store.GetDueMessagesAsync(batchSize: 10, maxRetries: 3)).ShouldBeRight();
        notDue.ShouldBeEmpty();

        // Step 2: Advance time so it becomes due
        _timeProvider.Advance(TimeSpan.FromMinutes(35));
        var due = (await _store.GetDueMessagesAsync(batchSize: 10, maxRetries: 3)).ShouldBeRight();
        due.Count().ShouldBe(1);

        // Step 3: First execution fails
        var retryTime = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(5);
        (await _store.MarkAsFailedAsync(messageId, "Order service unavailable", retryTime)).ShouldBeRight();
        (await _store.SaveChangesAsync()).ShouldBeRight();

        var afterFail = await _dbContext.ScheduledMessages.FindAsync(messageId);
        afterFail!.RetryCount.ShouldBe(1);
        afterFail.ErrorMessage.ShouldBe("Order service unavailable");

        // Not ready for retry yet
        var notReady = (await _store.GetDueMessagesAsync(batchSize: 10, maxRetries: 3)).ShouldBeRight();
        notReady.ShouldBeEmpty();

        // Step 4: Advance past retry time
        _timeProvider.Advance(TimeSpan.FromMinutes(10));

        var readyForRetry = (await _store.GetDueMessagesAsync(batchSize: 10, maxRetries: 3)).ShouldBeRight();
        readyForRetry.Count().ShouldBe(1);

        // Step 5: Successfully process
        (await _store.MarkAsProcessedAsync(messageId)).ShouldBeRight();
        (await _store.SaveChangesAsync()).ShouldBeRight();

        var final = await _dbContext.ScheduledMessages.FindAsync(messageId);
        final!.IsProcessed.ShouldBeTrue();
        final.ProcessedAtUtc.ShouldNotBeNull();
        final.LastExecutedAtUtc.ShouldNotBeNull();
        final.ErrorMessage.ShouldBeNull();

        // No longer due
        var afterProcess = (await _store.GetDueMessagesAsync(batchSize: 10, maxRetries: 3)).ShouldBeRight();
        afterProcess.ShouldBeEmpty();
    }

    [Fact]
    public async Task FullLifecycle_RecurringMessage_ProcessRescheduleRepeat()
    {
        // Arrange
        var baseTime = _timeProvider.GetUtcNow().UtcDateTime;
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "HourlyHealthCheckCommand",
            Content = "{\"service\":\"api\"}",
            ScheduledAtUtc = baseTime.AddMinutes(-5), // already due
            CreatedAtUtc = baseTime.AddDays(-7),
            RetryCount = 0,
            IsRecurring = true,
            CronExpression = "0 * * * *"
        };

        (await _store.AddAsync(message)).ShouldBeRight();
        (await _store.SaveChangesAsync()).ShouldBeRight();

        // First execution: process
        var due1 = (await _store.GetDueMessagesAsync(batchSize: 10, maxRetries: 3)).ShouldBeRight();
        due1.Count().ShouldBe(1);

        (await _store.MarkAsProcessedAsync(messageId)).ShouldBeRight();
        (await _store.SaveChangesAsync()).ShouldBeRight();

        // Reschedule for next hour
        var nextHour = baseTime.AddHours(1);
        (await _store.RescheduleRecurringMessageAsync(messageId, nextHour)).ShouldBeRight();
        (await _store.SaveChangesAsync()).ShouldBeRight();

        // Verify reset state
        var rescheduled = await _dbContext.ScheduledMessages.FindAsync(messageId);
        rescheduled!.ProcessedAtUtc.ShouldBeNull();
        rescheduled.RetryCount.ShouldBe(0);
        rescheduled.ScheduledAtUtc.ShouldBe(nextHour);

        // Not due yet
        var notDue = (await _store.GetDueMessagesAsync(batchSize: 10, maxRetries: 3)).ShouldBeRight();
        notDue.ShouldBeEmpty();

        // Advance to next hour
        _timeProvider.Advance(TimeSpan.FromHours(1).Add(TimeSpan.FromMinutes(1)));

        // Now due again
        var due2 = (await _store.GetDueMessagesAsync(batchSize: 10, maxRetries: 3)).ShouldBeRight();
        due2.Count().ShouldBe(1);
    }

    [Fact]
    public async Task CancelAsync_RemovesExistingMessage()
    {
        // Arrange
        var baseTime = _timeProvider.GetUtcNow().UtcDateTime;
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "CancellableCommand",
            Content = "{}",
            ScheduledAtUtc = baseTime.AddHours(2),
            CreatedAtUtc = baseTime,
            RetryCount = 0,
            IsRecurring = false
        };

        await _dbContext.ScheduledMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        // Verify it exists
        var before = await _dbContext.ScheduledMessages.FindAsync(message.Id);
        before.ShouldNotBeNull();

        // Act
        (await _store.CancelAsync(message.Id)).ShouldBeRight();
        (await _store.SaveChangesAsync()).ShouldBeRight();

        // Assert
        var after = await _dbContext.ScheduledMessages.FindAsync(message.Id);
        after.ShouldBeNull();
    }

    [Fact]
    public async Task MarkAsFailedAsync_IncrementsRetryCountMultipleTimes()
    {
        // Arrange
        var baseTime = _timeProvider.GetUtcNow().UtcDateTime;
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "FlakyCommand",
            Content = "{}",
            ScheduledAtUtc = baseTime.AddMinutes(-10),
            CreatedAtUtc = baseTime.AddHours(-1),
            RetryCount = 0,
            IsRecurring = false
        };

        await _dbContext.ScheduledMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        // Act - Three consecutive failures
        (await _store.MarkAsFailedAsync(message.Id, "Timeout", baseTime.AddMinutes(5))).ShouldBeRight();
        (await _store.SaveChangesAsync()).ShouldBeRight();

        (await _store.MarkAsFailedAsync(message.Id, "Connection reset", baseTime.AddMinutes(15))).ShouldBeRight();
        (await _store.SaveChangesAsync()).ShouldBeRight();

        (await _store.MarkAsFailedAsync(message.Id, "Service unavailable", baseTime.AddMinutes(30))).ShouldBeRight();
        (await _store.SaveChangesAsync()).ShouldBeRight();

        // Assert
        var updated = await _dbContext.ScheduledMessages.FindAsync(message.Id);
        updated!.RetryCount.ShouldBe(3);
        updated.ErrorMessage.ShouldBe("Service unavailable");
        updated.NextRetryAtUtc.ShouldBe(baseTime.AddMinutes(30));
        updated.IsDeadLettered(maxRetries: 3).ShouldBeTrue();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
