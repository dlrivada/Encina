using Encina.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Scheduling;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.EntityFrameworkCore.Scheduling;

public class ScheduledMessageStoreEFTests : IDisposable
{
    private readonly TestDbContext _dbContext;
    private readonly ScheduledMessageStoreEF _store;

    public ScheduledMessageStoreEFTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestDbContext(options);
        _store = new ScheduledMessageStoreEF(_dbContext);
    }

    [Fact]
    public async Task AddAsync_ShouldAddMessageToStore()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "TestCommand",
            Content = "{\"test\":\"data\"}",
            ScheduledAtUtc = now.AddHours(1),
            CreatedAtUtc = now,
            RetryCount = 0,
            IsRecurring = false
        };

        // Act
        await _store.AddAsync(message);
        await _store.SaveChangesAsync();

        // Assert
        var stored = await _dbContext.ScheduledMessages.FindAsync(message.Id);
        stored.ShouldNotBeNull();
        stored!.RequestType.ShouldBe("TestCommand");
    }

    [Fact]
    public async Task GetDueMessagesAsync_ShouldReturnDueMessages()
    {
        // Arrange - Use current time to avoid date-based test failures
        var now = DateTime.UtcNow;
        var due1 = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "DueCommand1",
            Content = "{}",
            ScheduledAtUtc = now.AddMinutes(-10),
            CreatedAtUtc = now.AddHours(-1),
            RetryCount = 0,
            IsRecurring = false
        };

        var due2 = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "DueCommand2",
            Content = "{}",
            ScheduledAtUtc = now.AddMinutes(-5),
            CreatedAtUtc = now.AddHours(-1),
            RetryCount = 0,
            IsRecurring = false
        };

        var notDue = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "FutureCommand",
            Content = "{}",
            ScheduledAtUtc = now.AddHours(1),
            CreatedAtUtc = now,
            RetryCount = 0,
            IsRecurring = false
        };

        var processed = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "ProcessedCommand",
            Content = "{}",
            ScheduledAtUtc = now.AddMinutes(-15),
            CreatedAtUtc = now.AddHours(-2),
            ProcessedAtUtc = now.AddMinutes(-10),
            RetryCount = 0,
            IsRecurring = false
        };

        await _dbContext.ScheduledMessages.AddRangeAsync(due1, due2, notDue, processed);
        await _dbContext.SaveChangesAsync();

        // Act
        var messages = await _store.GetDueMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
        messages.Count().ShouldBe(2);
        messages.ShouldContain(m => m.Id == due1.Id);
        messages.ShouldContain(m => m.Id == due2.Id);
    }

    [Fact]
    public async Task GetDueMessagesAsync_ShouldRespectBatchSize()
    {
        // Arrange
        var now = DateTime.UtcNow;
        for (var i = 0; i < 10; i++)
        {
            await _dbContext.ScheduledMessages.AddAsync(new ScheduledMessage
            {
                Id = Guid.NewGuid(),
                RequestType = $"Command{i}",
                Content = "{}",
                ScheduledAtUtc = now.AddMinutes(-i),
                CreatedAtUtc = now.AddHours(-1),
                RetryCount = 0,
                IsRecurring = false
            });
        }
        await _dbContext.SaveChangesAsync();

        // Act
        var messages = await _store.GetDueMessagesAsync(batchSize: 5, maxRetries: 3);

        // Assert
        messages.Count().ShouldBe(5);
    }

    [Fact]
    public async Task GetDueMessagesAsync_ShouldExcludeMaxRetriedMessages()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var maxRetried = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "MaxRetriedCommand",
            Content = "{}",
            ScheduledAtUtc = now.AddMinutes(-10),
            CreatedAtUtc = now.AddHours(-1),
            RetryCount = 5,
            IsRecurring = false
        };

        await _dbContext.ScheduledMessages.AddAsync(maxRetried);
        await _dbContext.SaveChangesAsync();

        // Act
        var messages = await _store.GetDueMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
        messages.ShouldBeEmpty();
    }

    [Fact]
    public async Task MarkAsProcessedAsync_ShouldUpdateMessage()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "TestCommand",
            Content = "{}",
            ScheduledAtUtc = now.AddMinutes(-10),
            CreatedAtUtc = now.AddHours(-1),
            RetryCount = 0,
            IsRecurring = false
        };

        await _dbContext.ScheduledMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        // Act
        await _store.MarkAsProcessedAsync(message.Id);
        await _store.SaveChangesAsync();

        // Assert
        var updated = await _dbContext.ScheduledMessages.FindAsync(message.Id);
        updated!.ProcessedAtUtc.ShouldNotBeNull();
        updated.LastExecutedAtUtc.ShouldNotBeNull();
        updated.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task MarkAsFailedAsync_ShouldUpdateMessageWithError()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "TestCommand",
            Content = "{}",
            ScheduledAtUtc = now.AddMinutes(-10),
            CreatedAtUtc = now.AddHours(-1),
            RetryCount = 0,
            IsRecurring = false
        };

        await _dbContext.ScheduledMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        var nextRetry = now.AddMinutes(5);

        // Act
        await _store.MarkAsFailedAsync(message.Id, "Test error", nextRetry);
        await _store.SaveChangesAsync();

        // Assert
        var updated = await _dbContext.ScheduledMessages.FindAsync(message.Id);
        updated!.ErrorMessage.ShouldBe("Test error");
        updated.RetryCount.ShouldBe(1);
        updated.NextRetryAtUtc.ShouldBe(nextRetry);
    }

    [Fact]
    public async Task RescheduleRecurringMessageAsync_ShouldResetMessage()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "RecurringCommand",
            Content = "{}",
            ScheduledAtUtc = now.AddMinutes(-10),
            CreatedAtUtc = now.AddHours(-1),
            ProcessedAtUtc = now.AddMinutes(-5),
            RetryCount = 0,
            IsRecurring = true,
            CronExpression = "0 0 * * *"
        };

        await _dbContext.ScheduledMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        var nextScheduled = now.AddHours(24);

        // Act
        await _store.RescheduleRecurringMessageAsync(message.Id, nextScheduled);
        await _store.SaveChangesAsync();

        // Assert
        var updated = await _dbContext.ScheduledMessages.FindAsync(message.Id);
        updated!.ScheduledAtUtc.ShouldBe(nextScheduled);
        updated.ProcessedAtUtc.ShouldBeNull();
        updated.ErrorMessage.ShouldBeNull();
        updated.RetryCount.ShouldBe(0);
    }

    [Fact]
    public async Task CancelAsync_ShouldRemoveMessage()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "CancelCommand",
            Content = "{}",
            ScheduledAtUtc = now.AddHours(1),
            CreatedAtUtc = now,
            RetryCount = 0,
            IsRecurring = false
        };

        await _dbContext.ScheduledMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        // Act
        await _store.CancelAsync(message.Id);
        await _store.SaveChangesAsync();

        // Assert
        var removed = await _dbContext.ScheduledMessages.FindAsync(message.Id);
        removed.ShouldBeNull();
    }

    [Fact]
    public void IsDue_ShouldReturnTrueForDueMessages()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "DueCommand",
            Content = "{}",
            ScheduledAtUtc = now.AddMinutes(-1),
            CreatedAtUtc = now.AddHours(-1),
            RetryCount = 0,
            IsRecurring = false
        };

        // Act & Assert
        message.IsDue().ShouldBeTrue();
    }

    [Fact]
    public void IsDeadLettered_ShouldReturnTrueForMaxRetriedMessages()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "DeadLetteredCommand",
            Content = "{}",
            ScheduledAtUtc = now.AddMinutes(-10),
            CreatedAtUtc = now.AddHours(-1),
            RetryCount = 5,
            IsRecurring = false
        };

        // Act & Assert
        message.IsDeadLettered(maxRetries: 3).ShouldBeTrue();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
