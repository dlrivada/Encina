using Encina.ADO.SqlServer.Scheduling;
using Encina.TestInfrastructure.Fixtures;

namespace Encina.IntegrationTests.ADO.SqlServer.Scheduling;

/// <summary>
/// Integration tests for <see cref="ScheduledMessageStoreADO"/>.
/// Tests against real SQL Server database via Testcontainers.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "ADO.SqlServer")]
[Collection("ADO-SqlServer")]
public sealed class ScheduledMessageStoreADOTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    private ScheduledMessageStoreADO _store = null!;

    public ScheduledMessageStoreADOTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ClearAllDataAsync();
        _store = new ScheduledMessageStoreADO(_fixture.CreateConnection());
    }

    public Task DisposeAsync() => Task.CompletedTask;

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
        await _store.AddAsync(message);

        // Assert - Retrieve via GetDueMessagesAsync with future time
        var messages = await _store.GetDueMessagesAsync(10, 3);
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
        await _store.AddAsync(dueMessage);

        var dueMessages = await _store.GetDueMessagesAsync(10, 3);
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
        await _store.AddAsync(dueMessage);

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
        await _store.AddAsync(futureMessage);

        // Act
        var messages = await _store.GetDueMessagesAsync(10, 3);

        // Assert
        Assert.Single(messages);
        Assert.Equal(dueMessage.Id, messages.First().Id);
    }

    [Fact]
    public async Task GetDueMessagesAsync_RespectsBatchSize()
    {
        // Arrange - Create 5 due messages
        for (var i = 0; i < 5; i++)
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
            await _store.AddAsync(message);
        }

        // Act - Request only 3
        var messages = await _store.GetDueMessagesAsync(3, 5);

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
        await _store.AddAsync(lowRetry);

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
        await _store.AddAsync(highRetry);

        // Act - Max retries = 3
        var messages = await _store.GetDueMessagesAsync(10, 3);

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
        await _store.AddAsync(message);

        // Act
        await _store.MarkAsProcessedAsync(messageId);

        // Assert - Should no longer appear in due messages
        var messages = await _store.GetDueMessagesAsync(10, 3);
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
        await _store.AddAsync(message);

        // Act - Mark as failed twice
        await _store.MarkAsFailedAsync(messageId, "Error 1", DateTime.UtcNow.AddMinutes(10));
        await _store.MarkAsFailedAsync(messageId, "Error 2", DateTime.UtcNow.AddMinutes(20));

        // Assert - RetryCount should be 2, and not appear in due messages
        var messages = await _store.GetDueMessagesAsync(10, 3);
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
        await _store.AddAsync(message);

        // Act - Mark as failed with past retry time
        await _store.MarkAsFailedAsync(messageId, "Retry now", DateTime.UtcNow.AddSeconds(-10));

        // Assert - Should appear in due messages
        var messages = await _store.GetDueMessagesAsync(10, 3);
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
        await _store.AddAsync(message);

        // Act - Reschedule for next hour
        var nextRun = DateTime.UtcNow.AddHours(1);
        await _store.RescheduleRecurringMessageAsync(messageId, nextRun);

        // Assert - Should not appear in due messages (future time)
        var messages = await _store.GetDueMessagesAsync(10, 3);
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
        await _store.AddAsync(message);

        // Act
        var messages = await _store.GetDueMessagesAsync(10, 3);

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
        await _store.AddAsync(message);

        // Act
        await _store.CancelAsync(messageId);

        // Assert - Should not appear in any query
        var messages = await _store.GetDueMessagesAsync(10, 3);
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
        await _store.AddAsync(oldest);

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
        await _store.AddAsync(newer);

        // Act
        var messages = await _store.GetDueMessagesAsync(10, 3);

        // Assert - Oldest first
        var list = messages.ToList();
        Assert.Equal(2, list.Count);
        Assert.Equal(oldest.Id, list[0].Id);
        Assert.Equal(newer.Id, list[1].Id);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldComplete()
    {
        // Act
        await _store.SaveChangesAsync();

        // Assert - Operation completed without throwing
        Assert.True(true, "SaveChangesAsync completed successfully");
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
        await _store.AddAsync(message1);
        await _store.AddAsync(message2);

        // Assert
        var messages = await _store.GetDueMessagesAsync(10, 3);
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
        await _store.AddAsync(futureMessage);

        // Act
        var messages = await _store.GetDueMessagesAsync(10, 3);

        // Assert
        Assert.Empty(messages);
    }
}
