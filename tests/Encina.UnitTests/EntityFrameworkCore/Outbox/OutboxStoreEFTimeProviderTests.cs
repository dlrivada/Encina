using Encina.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Outbox;
using Encina.Testing.Shouldly;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.EntityFrameworkCore.Outbox;

/// <summary>
/// Tests for <see cref="OutboxStoreEF"/> exercising custom TimeProvider injection
/// and full lifecycle flows that cover timestamp-dependent code paths.
/// </summary>
[Trait("Category", "Unit")]
public class OutboxStoreEFTimeProviderTests : IDisposable
{
    private readonly TestDbContext _dbContext;
    private readonly FakeTimeProvider _timeProvider;
    private readonly OutboxStoreEF _store;

    public OutboxStoreEFTimeProviderTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestDbContext(options);
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero));
        _store = new OutboxStoreEF(_dbContext, _timeProvider);
    }

    [Fact]
    public async Task MarkAsProcessedAsync_UsesInjectedTimeProvider()
    {
        // Arrange
        var baseTime = _timeProvider.GetUtcNow().UtcDateTime;
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "OrderPlacedEvent",
            Content = "{\"orderId\":\"abc\"}",
            CreatedAtUtc = baseTime.AddMinutes(-10),
            RetryCount = 0
        };

        await _dbContext.OutboxMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        // Act
        (await _store.MarkAsProcessedAsync(message.Id)).ShouldBeRight();
        (await _store.SaveChangesAsync()).ShouldBeRight();

        // Assert - timestamp should come from the FakeTimeProvider
        var updated = await _dbContext.OutboxMessages.FindAsync(message.Id);
        updated!.ProcessedAtUtc.ShouldNotBeNull();
        updated.ProcessedAtUtc!.Value.ShouldBe(baseTime, TimeSpan.FromSeconds(1));
        updated.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task MarkAsProcessedAsync_ClearsPreExistingError()
    {
        // Arrange - message with a previous failure
        var baseTime = _timeProvider.GetUtcNow().UtcDateTime;
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "PaymentEvent",
            Content = "{}",
            CreatedAtUtc = baseTime.AddMinutes(-30),
            RetryCount = 2,
            ErrorMessage = "Connection refused",
            NextRetryAtUtc = baseTime.AddMinutes(-5)
        };

        await _dbContext.OutboxMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        // Act
        (await _store.MarkAsProcessedAsync(message.Id)).ShouldBeRight();
        (await _store.SaveChangesAsync()).ShouldBeRight();

        // Assert - error should be cleared on successful processing
        var updated = await _dbContext.OutboxMessages.FindAsync(message.Id);
        updated!.ErrorMessage.ShouldBeNull();
        updated.ProcessedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetPendingMessagesAsync_UsesInjectedTimeProvider_ForRetryFiltering()
    {
        // Arrange
        var baseTime = _timeProvider.GetUtcNow().UtcDateTime;

        // Message with retry scheduled 30 minutes from now (should be excluded)
        var futureRetry = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "FutureRetry",
            Content = "{}",
            CreatedAtUtc = baseTime.AddHours(-1),
            RetryCount = 1,
            NextRetryAtUtc = baseTime.AddMinutes(30)
        };

        // Message with retry scheduled 10 minutes ago (should be included)
        var pastRetry = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "PastRetry",
            Content = "{}",
            CreatedAtUtc = baseTime.AddHours(-1),
            RetryCount = 1,
            NextRetryAtUtc = baseTime.AddMinutes(-10)
        };

        await _dbContext.OutboxMessages.AddRangeAsync(futureRetry, pastRetry);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = (await _store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 5)).ShouldBeRight();

        // Assert
        result.Count().ShouldBe(1);
        result.First().Id.ShouldBe(pastRetry.Id);
    }

    [Fact]
    public async Task GetPendingMessagesAsync_AfterTimeAdvance_IncludesPreviouslyExcludedRetry()
    {
        // Arrange
        var baseTime = _timeProvider.GetUtcNow().UtcDateTime;

        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "RetryEvent",
            Content = "{}",
            CreatedAtUtc = baseTime.AddHours(-1),
            RetryCount = 1,
            NextRetryAtUtc = baseTime.AddMinutes(30) // 30 minutes in future
        };

        await _dbContext.OutboxMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        // Act 1 - should not be included yet
        var before = (await _store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 5)).ShouldBeRight();
        before.ShouldBeEmpty();

        // Advance time past the retry point
        _timeProvider.Advance(TimeSpan.FromMinutes(45));

        // Act 2 - now should be included
        var after = (await _store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 5)).ShouldBeRight();
        after.Count().ShouldBe(1);
        after.First().Id.ShouldBe(message.Id);
    }

    [Fact]
    public async Task FullLifecycle_AddFailRetryAndProcess()
    {
        // Arrange
        var baseTime = _timeProvider.GetUtcNow().UtcDateTime;
        var messageId = Guid.NewGuid();
        var message = new OutboxMessage
        {
            Id = messageId,
            NotificationType = "InventoryReservedEvent",
            Content = "{\"sku\":\"WIDGET-001\",\"quantity\":5}",
            CreatedAtUtc = baseTime,
            RetryCount = 0
        };

        // Act - Add
        (await _store.AddAsync(message)).ShouldBeRight();
        (await _store.SaveChangesAsync()).ShouldBeRight();

        // Verify pending
        var pending = (await _store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3)).ShouldBeRight();
        pending.ShouldContain(m => m.Id == messageId);

        // Act - First failure
        var retryTime = baseTime.AddMinutes(5);
        (await _store.MarkAsFailedAsync(messageId, "Broker unavailable", retryTime)).ShouldBeRight();
        (await _store.SaveChangesAsync()).ShouldBeRight();

        var afterFail = await _dbContext.OutboxMessages.FindAsync(messageId);
        afterFail!.RetryCount.ShouldBe(1);
        afterFail.ErrorMessage.ShouldBe("Broker unavailable");
        afterFail.NextRetryAtUtc.ShouldBe(retryTime);

        // Before retry time - should not be pending
        var notYetReady = (await _store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3)).ShouldBeRight();
        notYetReady.ShouldBeEmpty();

        // Advance past retry time
        _timeProvider.Advance(TimeSpan.FromMinutes(10));

        // Now it should be pending again
        var readyForRetry = (await _store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3)).ShouldBeRight();
        readyForRetry.Count().ShouldBe(1);

        // Act - Finally process successfully
        (await _store.MarkAsProcessedAsync(messageId)).ShouldBeRight();
        (await _store.SaveChangesAsync()).ShouldBeRight();

        var final = await _dbContext.OutboxMessages.FindAsync(messageId);
        final!.IsProcessed.ShouldBeTrue();
        final.ErrorMessage.ShouldBeNull();
        final.ProcessedAtUtc.ShouldNotBeNull();

        // Should no longer appear as pending
        var afterProcess = (await _store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3)).ShouldBeRight();
        afterProcess.ShouldBeEmpty();
    }

    [Fact]
    public async Task MarkAsFailedAsync_IncrementsRetryCountOnMultipleFailures()
    {
        // Arrange
        var baseTime = _timeProvider.GetUtcNow().UtcDateTime;
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "RepeatedFailure",
            Content = "{}",
            CreatedAtUtc = baseTime,
            RetryCount = 0
        };

        await _dbContext.OutboxMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        // Act - Three consecutive failures
        (await _store.MarkAsFailedAsync(message.Id, "Error 1", baseTime.AddMinutes(1))).ShouldBeRight();
        (await _store.SaveChangesAsync()).ShouldBeRight();

        (await _store.MarkAsFailedAsync(message.Id, "Error 2", baseTime.AddMinutes(2))).ShouldBeRight();
        (await _store.SaveChangesAsync()).ShouldBeRight();

        (await _store.MarkAsFailedAsync(message.Id, "Error 3", baseTime.AddMinutes(3))).ShouldBeRight();
        (await _store.SaveChangesAsync()).ShouldBeRight();

        // Assert
        var updated = await _dbContext.OutboxMessages.FindAsync(message.Id);
        updated!.RetryCount.ShouldBe(3);
        updated.ErrorMessage.ShouldBe("Error 3");
        updated.IsDeadLettered(maxRetries: 3).ShouldBeTrue();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
