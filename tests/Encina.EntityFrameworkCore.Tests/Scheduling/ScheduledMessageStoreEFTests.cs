using Encina.EntityFrameworkCore.Scheduling;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Encina.EntityFrameworkCore.Tests.Scheduling;

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
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "TestCommand",
            Content = "{\"test\":\"data\"}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(1),
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0,
            IsRecurring = false
        };

        // Act
        await _store.AddAsync(message);
        await _store.SaveChangesAsync();

        // Assert
        var stored = await _dbContext.ScheduledMessages.FindAsync(message.Id);
        stored.Should().NotBeNull();
        stored!.RequestType.Should().Be("TestCommand");
    }

    [Fact]
    public async Task GetDueMessagesAsync_ShouldReturnDueMessages()
    {
        // Arrange
        var due1 = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "DueCommand1",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-10),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
            RetryCount = 0,
            IsRecurring = false
        };

        var due2 = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "DueCommand2",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-5),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
            RetryCount = 0,
            IsRecurring = false
        };

        var notDue = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "FutureCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(1),
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0,
            IsRecurring = false
        };

        var processed = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "ProcessedCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-15),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            ProcessedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            RetryCount = 0,
            IsRecurring = false
        };

        await _dbContext.ScheduledMessages.AddRangeAsync(due1, due2, notDue, processed);
        await _dbContext.SaveChangesAsync();

        // Act
        var messages = await _store.GetDueMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
        messages.Should().HaveCount(2);
        messages.Should().Contain(m => m.Id == due1.Id);
        messages.Should().Contain(m => m.Id == due2.Id);
    }

    [Fact]
    public async Task GetDueMessagesAsync_ShouldRespectBatchSize()
    {
        // Arrange
        for (var i = 0; i < 10; i++)
        {
            await _dbContext.ScheduledMessages.AddAsync(new ScheduledMessage
            {
                Id = Guid.NewGuid(),
                RequestType = $"Command{i}",
                Content = "{}",
                ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-i),
                CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
                RetryCount = 0,
                IsRecurring = false
            });
        }
        await _dbContext.SaveChangesAsync();

        // Act
        var messages = await _store.GetDueMessagesAsync(batchSize: 5, maxRetries: 3);

        // Assert
        messages.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetDueMessagesAsync_ShouldExcludeMaxRetriedMessages()
    {
        // Arrange
        var maxRetried = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "MaxRetriedCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-10),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
            RetryCount = 5,
            IsRecurring = false
        };

        await _dbContext.ScheduledMessages.AddAsync(maxRetried);
        await _dbContext.SaveChangesAsync();

        // Act
        var messages = await _store.GetDueMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
        messages.Should().BeEmpty();
    }

    [Fact]
    public async Task MarkAsProcessedAsync_ShouldUpdateMessage()
    {
        // Arrange
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "TestCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-10),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
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
        updated!.ProcessedAtUtc.Should().NotBeNull();
        updated.LastExecutedAtUtc.Should().NotBeNull();
        updated.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task MarkAsFailedAsync_ShouldUpdateMessageWithError()
    {
        // Arrange
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "TestCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-10),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
            RetryCount = 0,
            IsRecurring = false
        };

        await _dbContext.ScheduledMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        var nextRetry = DateTime.UtcNow.AddMinutes(5);

        // Act
        await _store.MarkAsFailedAsync(message.Id, "Test error", nextRetry);
        await _store.SaveChangesAsync();

        // Assert
        var updated = await _dbContext.ScheduledMessages.FindAsync(message.Id);
        updated!.ErrorMessage.Should().Be("Test error");
        updated.RetryCount.Should().Be(1);
        updated.NextRetryAtUtc.Should().BeCloseTo(nextRetry, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task RescheduleRecurringMessageAsync_ShouldResetMessage()
    {
        // Arrange
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "RecurringCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-10),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
            ProcessedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            RetryCount = 0,
            IsRecurring = true,
            CronExpression = "0 0 * * *"
        };

        await _dbContext.ScheduledMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        var nextScheduled = DateTime.UtcNow.AddHours(24);

        // Act
        await _store.RescheduleRecurringMessageAsync(message.Id, nextScheduled);
        await _store.SaveChangesAsync();

        // Assert
        var updated = await _dbContext.ScheduledMessages.FindAsync(message.Id);
        updated!.ScheduledAtUtc.Should().BeCloseTo(nextScheduled, TimeSpan.FromSeconds(1));
        updated.ProcessedAtUtc.Should().BeNull();
        updated.ErrorMessage.Should().BeNull();
        updated.RetryCount.Should().Be(0);
    }

    [Fact]
    public async Task CancelAsync_ShouldRemoveMessage()
    {
        // Arrange
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "CancelCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(1),
            CreatedAtUtc = DateTime.UtcNow,
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
        removed.Should().BeNull();
    }

    [Fact]
    public async Task IsDue_ShouldReturnTrueForDueMessages()
    {
        // Arrange
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "DueCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-1),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
            RetryCount = 0,
            IsRecurring = false
        };

        // Act & Assert
        message.IsDue().Should().BeTrue();
    }

    [Fact]
    public async Task IsDeadLettered_ShouldReturnTrueForMaxRetriedMessages()
    {
        // Arrange
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "DeadLetteredCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-10),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
            RetryCount = 5,
            IsRecurring = false
        };

        // Act & Assert
        message.IsDeadLettered(maxRetries: 3).Should().BeTrue();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
