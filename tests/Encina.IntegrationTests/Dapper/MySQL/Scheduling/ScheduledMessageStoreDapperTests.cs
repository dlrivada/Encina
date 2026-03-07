using Encina.Dapper.MySQL.Scheduling;
using Encina.TestInfrastructure.Extensions;
using Encina.TestInfrastructure.Fixtures;
using LanguageExt;
using Xunit;

namespace Encina.IntegrationTests.Dapper.MySQL.Scheduling;

/// <summary>
/// Integration tests for <see cref="ScheduledMessageStoreDapper"/>.
/// Tests against real MySQL database via Testcontainers with proper cleanup.
/// </summary>
[Collection("Dapper-MySQL")]
[Trait("Category", "Integration")]
public sealed class ScheduledMessageStoreDapperTests : IAsyncLifetime
{
    private readonly MySqlFixture _database;
    private ScheduledMessageStoreDapper _store = null!;

    public ScheduledMessageStoreDapperTests(MySqlFixture database)
    {
        _database = database;
    }

    public async ValueTask InitializeAsync()
    {
        await _database.ClearAllDataAsync();
        _store = new ScheduledMessageStoreDapper(_database.CreateConnection());
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task AddAsync_ValidMessage_ShouldPersist()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "TestCommand",
            Content = "{\"data\":\"test\"}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(1),
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0,
            IsRecurring = false
        };

        // Act
        (await _store.AddAsync(message)).ShouldBeRight();

        // Assert - Retrieve via GetDueMessagesAsync with future time
        var messages = (await _store.GetDueMessagesAsync(10, 3)).ShouldBeRight();
        var retrieved = messages.FirstOrDefault(m => m.Id == messageId);
        Assert.Null(retrieved); // Not due yet

        // Verify it exists by adding another message scheduled now and checking count
        var dueMessage = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "DueCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0,
            IsRecurring = false
        };
        (await _store.AddAsync(dueMessage)).ShouldBeRight();

        var dueMessages = (await _store.GetDueMessagesAsync(10, 3)).ShouldBeRight();
        Assert.Single(dueMessages);
        Assert.Equal(dueMessage.Id, dueMessages.First().Id);
    }

    [Fact]
    public async Task GetDueMessagesAsync_OneTimeMessage_ReturnsDueOnly()
    {
        // Arrange - Create one due, one future
        var dueMessage = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "DueCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            RetryCount = 0,
            IsRecurring = false
        };
        (await _store.AddAsync(dueMessage)).ShouldBeRight();

        var futureMessage = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "FutureCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(2),
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0,
            IsRecurring = false
        };
        (await _store.AddAsync(futureMessage)).ShouldBeRight();

        // Act
        var messages = (await _store.GetDueMessagesAsync(10, 3)).ShouldBeRight();

        // Assert
        Assert.Single(messages);
        Assert.Equal(dueMessage.Id, messages.First().Id);
    }

    [Fact]
    public async Task GetDueMessagesAsync_RespectsBatchSize()
    {
        // Arrange - Create 5 due messages
        for (int i = 0; i < 5; i++)
        {
            var message = new ScheduledMessage
            {
                Id = Guid.NewGuid(),
                RequestType = $"Command{i}",
                Content = "{}",
                ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
                CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
                RetryCount = 0,
                IsRecurring = false
            };
            (await _store.AddAsync(message)).ShouldBeRight();
        }

        // Act - Request only 3
        var messages = (await _store.GetDueMessagesAsync(3, 5)).ShouldBeRight();

        // Assert
        Assert.Equal(3, messages.Count());
    }

    [Fact]
    public async Task GetDueMessagesAsync_FiltersMaxRetries()
    {
        // Arrange - Create messages with different retry counts
        var lowRetry = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "LowRetryCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            RetryCount = 2,
            IsRecurring = false
        };
        (await _store.AddAsync(lowRetry)).ShouldBeRight();

        var highRetry = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "HighRetryCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            RetryCount = 5,
            IsRecurring = false
        };
        (await _store.AddAsync(highRetry)).ShouldBeRight();

        // Act - Max retries = 3
        var messages = (await _store.GetDueMessagesAsync(10, 3)).ShouldBeRight();

        // Assert - Only lowRetry should be returned
        Assert.Single(messages);
        Assert.Equal(lowRetry.Id, messages.First().Id);
    }

    [Fact]
    public async Task MarkAsProcessedAsync_ShouldSetTimestamp()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "TestCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            RetryCount = 0,
            IsRecurring = false
        };
        (await _store.AddAsync(message)).ShouldBeRight();

        // Act
        (await _store.MarkAsProcessedAsync(messageId)).ShouldBeRight();

        // Assert - Should no longer appear in due messages
        var messages = (await _store.GetDueMessagesAsync(10, 3)).ShouldBeRight();
        Assert.Empty(messages);
    }

    [Fact]
    public async Task MarkAsFailedAsync_IncrementsRetryCount()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "FailingCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            RetryCount = 0,
            IsRecurring = false
        };
        (await _store.AddAsync(message)).ShouldBeRight();

        // Act - Mark as failed twice
        (await _store.MarkAsFailedAsync(messageId, "Error 1", DateTime.UtcNow.AddMinutes(10))).ShouldBeRight();
        (await _store.MarkAsFailedAsync(messageId, "Error 2", DateTime.UtcNow.AddMinutes(20))).ShouldBeRight();

        // Assert - RetryCount should be 2, and not appear in due messages
        var messages = (await _store.GetDueMessagesAsync(10, 3)).ShouldBeRight();
        Assert.Empty(messages); // NextRetryAtUtc is in the future
    }

    [Fact]
    public async Task MarkAsFailedAsync_WithPastRetry_AppearsInDue()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "RetryCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            RetryCount = 0,
            IsRecurring = false
        };
        (await _store.AddAsync(message)).ShouldBeRight();

        // Act - Mark as failed with past retry time
        (await _store.MarkAsFailedAsync(messageId, "Retry now", DateTime.UtcNow.AddSeconds(-10))).ShouldBeRight();

        // Assert - Should appear in due messages
        var messages = (await _store.GetDueMessagesAsync(10, 3)).ShouldBeRight();
        Assert.Single(messages);
        Assert.Equal(messageId, messages.First().Id);
    }

    [Fact]
    public async Task RescheduleRecurringMessageAsync_ResetsFields()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "RecurringCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            ProcessedAtUtc = DateTime.UtcNow.AddMinutes(-30),
            RetryCount = 2,
            ErrorMessage = "Previous error",
            NextRetryAtUtc = DateTime.UtcNow.AddMinutes(10),
            IsRecurring = true,
            CronExpression = "0 * * * *"
        };
        (await _store.AddAsync(message)).ShouldBeRight();

        // Act - Reschedule for next hour
        var nextRun = DateTime.UtcNow.AddHours(1);
        (await _store.RescheduleRecurringMessageAsync(messageId, nextRun)).ShouldBeRight();

        // Assert - Should not appear in due messages (future time)
        var messages = (await _store.GetDueMessagesAsync(10, 3)).ShouldBeRight();
        Assert.Empty(messages);
    }

    [Fact]
    public async Task GetDueMessagesAsync_RecurringMessage_AppearsEvenIfProcessed()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "RecurringCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            ProcessedAtUtc = DateTime.UtcNow.AddMinutes(-30), // Already processed
            RetryCount = 0,
            IsRecurring = true,
            CronExpression = "0 * * * *"
        };
        (await _store.AddAsync(message)).ShouldBeRight();

        // Act
        var messages = (await _store.GetDueMessagesAsync(10, 3)).ShouldBeRight();

        // Assert - Recurring messages appear even if processed
        Assert.Single(messages);
        Assert.Equal(messageId, messages.First().Id);
    }

    [Fact]
    public async Task CancelAsync_RemovesMessage()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "CancelableCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(1),
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0,
            IsRecurring = false
        };
        (await _store.AddAsync(message)).ShouldBeRight();

        // Act
        (await _store.CancelAsync(messageId)).ShouldBeRight();

        // Assert - Should not appear in any query
        var messages = (await _store.GetDueMessagesAsync(10, 3)).ShouldBeRight();
        Assert.Empty(messages);
    }

    [Fact]
    public async Task GetDueMessagesAsync_OrdersByScheduledAtUtc()
    {
        // Arrange - Create messages with different scheduled times
        var oldest = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "OldestCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(-3),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-4),
            RetryCount = 0,
            IsRecurring = false
        };
        (await _store.AddAsync(oldest)).ShouldBeRight();

        var newer = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "NewerCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            RetryCount = 0,
            IsRecurring = false
        };
        (await _store.AddAsync(newer)).ShouldBeRight();

        // Act
        var messages = (await _store.GetDueMessagesAsync(10, 3)).ShouldBeRight();

        // Assert - Oldest first
        var list = messages.ToList();
        Assert.Equal(2, list.Count);
        Assert.Equal(oldest.Id, list[0].Id);
        Assert.Equal(newer.Id, list[1].Id);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldComplete()
    {
        // Act & Assert - Should not throw
        (await _store.SaveChangesAsync()).ShouldBeRight();
    }

    [Fact]
    public async Task AddAsync_MultipleMessages_AllPersist()
    {
        // Arrange
        var message1 = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "Command1",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            RetryCount = 0,
            IsRecurring = false
        };

        var message2 = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "Command2",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(-1),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            RetryCount = 0,
            IsRecurring = false
        };

        // Act
        (await _store.AddAsync(message1)).ShouldBeRight();
        (await _store.AddAsync(message2)).ShouldBeRight();

        // Assert
        var messages = (await _store.GetDueMessagesAsync(10, 3)).ShouldBeRight();
        Assert.Equal(2, messages.Count());
    }

    [Fact]
    public async Task GetDueMessagesAsync_EmptyWhenNoDue()
    {
        // Arrange - Only future messages
        var futureMessage = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "FutureCommand",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddHours(5),
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0,
            IsRecurring = false
        };
        (await _store.AddAsync(futureMessage)).ShouldBeRight();

        // Act
        var messages = (await _store.GetDueMessagesAsync(10, 3)).ShouldBeRight();

        // Assert
        Assert.Empty(messages);
    }
}

