using Dapper;
using Encina.Dapper.PostgreSQL.Outbox;
using Encina.TestInfrastructure.Extensions;
using Encina.TestInfrastructure.Fixtures;
using Encina.Messaging.Outbox;
using Xunit;

namespace Encina.IntegrationTests.Dapper.PostgreSQL.Outbox;

/// <summary>
/// Integration tests for <see cref="OutboxStoreDapper"/>.
/// Tests the Dapper implementation of the Outbox pattern for reliable event publishing.
/// Uses real PostgreSQL database via Testcontainers for end-to-end verification.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "Dapper.PostgreSQL")]
public sealed class OutboxStoreDapperTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;
    private readonly OutboxStoreDapper _store;

    public OutboxStoreDapperTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;

        // Clear all data before each test to ensure clean state
        _fixture.ClearAllDataAsync().GetAwaiter().GetResult();

        _store = new OutboxStoreDapper(_fixture.CreateConnection());
    }

    #region AddAsync Tests

    /// <summary>
    /// Tests that AddAsync successfully inserts a valid message into the database.
    /// </summary>
    [Fact]
    public async Task AddAsync_ValidMessage_ShouldInsertToDatabase()
    {
        // Arrange
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "TestNotification",
            Content = "{\"test\":true}",
            CreatedAtUtc = DateTime.UtcNow,
            ProcessedAtUtc = null,
            ErrorMessage = null,
            RetryCount = 0,
            NextRetryAtUtc = null
        };

        // Act
        await _store.AddAsync(message);

        // Assert
        var pending = await _store.GetPendingMessagesAsync(10, 5);
        Assert.Single(pending);
    }

    /// <summary>
    /// Tests that AddAsync correctly persists all message properties.
    /// </summary>
    [Fact]
    public async Task AddAsync_ValidMessage_ShouldPersistAllProperties()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var message = new OutboxMessage
        {
            Id = messageId,
            NotificationType = "OrderCreatedEvent",
            Content = "{\"orderId\":123,\"total\":99.99}",
            CreatedAtUtc = createdAt,
            ProcessedAtUtc = null,
            ErrorMessage = null,
            RetryCount = 0,
            NextRetryAtUtc = null
        };

        // Act
        await _store.AddAsync(message);

        // Assert
        var messages = await _store.GetPendingMessagesAsync(10, 5);
        var retrieved = messages.Single();

        Assert.Equal(messageId, retrieved.Id);
        Assert.Equal("OrderCreatedEvent", retrieved.NotificationType);
        Assert.Equal("{\"orderId\":123,\"total\":99.99}", retrieved.Content);
        Assert.Null(retrieved.ProcessedAtUtc);
        Assert.Null(retrieved.ErrorMessage);
        Assert.Equal(0, retrieved.RetryCount);
        Assert.Null(retrieved.NextRetryAtUtc);
    }

    /// <summary>
    /// Tests that AddAsync can insert multiple messages without conflicts.
    /// </summary>
    [Fact]
    public async Task AddAsync_MultipleMessages_ShouldInsertAll()
    {
        // Arrange
        var message1 = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "Event1",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow
        };

        var message2 = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "Event2",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow.AddSeconds(1)
        };

        var message3 = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "Event3",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow.AddSeconds(2)
        };

        // Act
        await _store.AddAsync(message1);
        await _store.AddAsync(message2);
        await _store.AddAsync(message3);

        // Assert
        var pending = await _store.GetPendingMessagesAsync(10, 5);
        Assert.Equal(3, pending.Count());
    }

    #endregion

    #region GetPendingMessagesAsync Tests

    /// <summary>
    /// Tests that GetPendingMessagesAsync returns only unprocessed messages.
    /// </summary>
    [Fact]
    public async Task GetPendingMessagesAsync_OnlyUnprocessed_ShouldReturnPendingOnly()
    {
        // Arrange
        var pendingMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "PendingEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            ProcessedAtUtc = null
        };

        var processedMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "ProcessedEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow.AddSeconds(1),
            ProcessedAtUtc = DateTime.UtcNow.AddSeconds(10)
        };

        await _store.AddAsync(pendingMessage);
        await _store.AddAsync(processedMessage);

        // Act
        var pending = await _store.GetPendingMessagesAsync(10, 5);

        // Assert
        Assert.Single(pending);
        Assert.Equal(pendingMessage.Id, pending.First().Id);
    }

    /// <summary>
    /// Tests that GetPendingMessagesAsync respects the batch size limit.
    /// </summary>
    [Fact]
    public async Task GetPendingMessagesAsync_WithBatchSize_ShouldReturnLimitedResults()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            var message = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = $"Event{i}",
                Content = "{}",
                CreatedAtUtc = DateTime.UtcNow.AddSeconds(i)
            };
            await _store.AddAsync(message);
        }

        // Act
        var batch = await _store.GetPendingMessagesAsync(batchSize: 3, maxRetries: 5);

        // Assert
        Assert.Equal(3, batch.Count());
    }

    /// <summary>
    /// Tests that GetPendingMessagesAsync excludes messages that exceeded max retries.
    /// </summary>
    [Fact]
    public async Task GetPendingMessagesAsync_WithMaxRetries_ShouldExcludeExceededMessages()
    {
        // Arrange
        var normalMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "NormalEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 2
        };

        var exceededMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "ExceededEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow.AddSeconds(1),
            RetryCount = 5 // Equals max retries
        };

        await _store.AddAsync(normalMessage);
        await _store.AddAsync(exceededMessage);

        // Act
        var pending = await _store.GetPendingMessagesAsync(10, maxRetries: 5);

        // Assert
        Assert.Single(pending);
        Assert.Equal(normalMessage.Id, pending.First().Id);
    }

    /// <summary>
    /// Tests that GetPendingMessagesAsync excludes messages with NextRetryAtUtc in the future.
    /// </summary>
    [Fact]
    public async Task GetPendingMessagesAsync_WithNextRetryInFuture_ShouldExcludeNotReady()
    {
        // Arrange
        var readyMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "ReadyEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            NextRetryAtUtc = null
        };

        var notReadyMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "NotReadyEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow.AddSeconds(1),
            NextRetryAtUtc = DateTime.UtcNow.AddHours(1) // Future retry
        };

        await _store.AddAsync(readyMessage);
        await _store.AddAsync(notReadyMessage);

        // Act
        var pending = await _store.GetPendingMessagesAsync(10, 5);

        // Assert
        Assert.Single(pending);
        Assert.Equal(readyMessage.Id, pending.First().Id);
    }

    /// <summary>
    /// Tests that GetPendingMessagesAsync includes messages with NextRetryAtUtc in the past.
    /// </summary>
    [Fact]
    public async Task GetPendingMessagesAsync_WithNextRetryInPast_ShouldIncludeMessage()
    {
        // Arrange
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "RetryReadyEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            NextRetryAtUtc = DateTime.UtcNow.AddMinutes(-5) // Past retry time
        };

        await _store.AddAsync(message);

        // Act
        var pending = await _store.GetPendingMessagesAsync(10, 5);

        // Assert
        Assert.Single(pending);
        Assert.Equal(message.Id, pending.First().Id);
    }

    /// <summary>
    /// Tests that GetPendingMessagesAsync returns messages ordered by creation time.
    /// </summary>
    [Fact]
    public async Task GetPendingMessagesAsync_ShouldReturnOrderedByCreationTime()
    {
        // Arrange
        var message1 = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "Event1",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow.AddSeconds(2)
        };

        var message2 = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "Event2",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow.AddSeconds(1)
        };

        var message3 = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "Event3",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow.AddSeconds(3)
        };

        await _store.AddAsync(message1);
        await _store.AddAsync(message2);
        await _store.AddAsync(message3);

        // Act
        var pending = await _store.GetPendingMessagesAsync(10, 5);

        // Assert
        var orderedIds = pending.Select(m => m.Id).ToList();
        Assert.Equal(message2.Id, orderedIds[0]); // Oldest first
        Assert.Equal(message1.Id, orderedIds[1]);
        Assert.Equal(message3.Id, orderedIds[2]);
    }

    /// <summary>
    /// Tests that GetPendingMessagesAsync returns empty collection when no messages exist.
    /// </summary>
    [Fact]
    public async Task GetPendingMessagesAsync_NoMessages_ShouldReturnEmpty()
    {
        // Act
        var pending = await _store.GetPendingMessagesAsync(10, 5);

        // Assert
        Assert.Empty(pending);
    }

    #endregion

    #region MarkAsProcessedAsync Tests

    /// <summary>
    /// Tests that MarkAsProcessedAsync sets ProcessedAtUtc timestamp.
    /// </summary>
    [Fact]
    public async Task MarkAsProcessedAsync_ValidId_ShouldSetProcessedTimestamp()
    {
        // Arrange
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "TestEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow
        };

        await _store.AddAsync(message);

        // Act
        await _store.MarkAsProcessedAsync(message.Id);

        // Assert
        var pending = await _store.GetPendingMessagesAsync(10, 5);
        Assert.Empty(pending); // Should not appear in pending anymore
    }

    /// <summary>
    /// Tests that MarkAsProcessedAsync clears any existing error message.
    /// </summary>
    [Fact]
    public async Task MarkAsProcessedAsync_ShouldClearErrorMessage()
    {
        // Arrange
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "TestEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            ErrorMessage = "Previous error",
            RetryCount = 2
        };

        await _store.AddAsync(message);

        // Act
        await _store.MarkAsProcessedAsync(message.Id);

        // Assert
        var pending = await _store.GetPendingMessagesAsync(10, 5);
        Assert.Empty(pending);
    }

    /// <summary>
    /// Tests that MarkAsProcessedAsync does not affect other messages.
    /// </summary>
    [Fact]
    public async Task MarkAsProcessedAsync_ShouldNotAffectOtherMessages()
    {
        // Arrange
        var message1 = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "Event1",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow
        };

        var message2 = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "Event2",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow.AddSeconds(1)
        };

        await _store.AddAsync(message1);
        await _store.AddAsync(message2);

        // Act
        await _store.MarkAsProcessedAsync(message1.Id);

        // Assert
        var pending = await _store.GetPendingMessagesAsync(10, 5);
        Assert.Single(pending);
        Assert.Equal(message2.Id, pending.First().Id);
    }

    #endregion

    #region MarkAsFailedAsync Tests

    /// <summary>
    /// Tests that MarkAsFailedAsync increments the retry count.
    /// </summary>
    [Fact]
    public async Task MarkAsFailedAsync_ShouldIncrementRetryCount()
    {
        // Arrange
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "TestEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        await _store.AddAsync(message);

        // Act
        await _store.MarkAsFailedAsync(message.Id, "Test error", null);

        // Assert
        var pending = await _store.GetPendingMessagesAsync(10, 5);
        var updated = pending.First();
        Assert.Equal(1, updated.RetryCount);
    }

    /// <summary>
    /// Tests that MarkAsFailedAsync sets the error message.
    /// </summary>
    [Fact]
    public async Task MarkAsFailedAsync_ShouldSetErrorMessage()
    {
        // Arrange
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "TestEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow
        };

        await _store.AddAsync(message);

        // Act
        var errorMessage = "Network timeout after 30 seconds";
        await _store.MarkAsFailedAsync(message.Id, errorMessage, null);

        // Assert
        var pending = await _store.GetPendingMessagesAsync(10, 5);
        var updated = pending.First();
        Assert.Equal(errorMessage, updated.ErrorMessage);
    }

    /// <summary>
    /// Tests that MarkAsFailedAsync sets the next retry time.
    /// </summary>
    [Fact]
    public async Task MarkAsFailedAsync_ShouldSetNextRetryTime()
    {
        // Arrange
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "TestEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow
        };

        await _store.AddAsync(message);

        // Act
        var nextRetry = DateTime.UtcNow.AddMinutes(5);
        await _store.MarkAsFailedAsync(message.Id, "Test error", nextRetry);

        // Assert - Should not appear in pending yet (retry time in future)
        var pending = await _store.GetPendingMessagesAsync(10, 5);
        Assert.Empty(pending);
    }

    /// <summary>
    /// Tests that MarkAsFailedAsync can be called multiple times to increment retry count.
    /// </summary>
    [Fact]
    public async Task MarkAsFailedAsync_MultipleCalls_ShouldIncrementRetryCountEachTime()
    {
        // Arrange
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "TestEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        await _store.AddAsync(message);

        // Act
        await _store.MarkAsFailedAsync(message.Id, "Error 1", null);
        await _store.MarkAsFailedAsync(message.Id, "Error 2", null);
        await _store.MarkAsFailedAsync(message.Id, "Error 3", null);

        // Assert
        var pending = await _store.GetPendingMessagesAsync(10, 5);
        var updated = pending.First();
        Assert.Equal(3, updated.RetryCount);
        Assert.Equal("Error 3", updated.ErrorMessage); // Latest error
    }

    /// <summary>
    /// Tests that MarkAsFailedAsync does not affect other messages.
    /// </summary>
    [Fact]
    public async Task MarkAsFailedAsync_ShouldNotAffectOtherMessages()
    {
        // Arrange
        var message1 = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "Event1",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        var message2 = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "Event2",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow.AddSeconds(1),
            RetryCount = 0
        };

        await _store.AddAsync(message1);
        await _store.AddAsync(message2);

        // Act
        await _store.MarkAsFailedAsync(message1.Id, "Error", null);

        // Assert
        var pending = await _store.GetPendingMessagesAsync(10, 5);
        var message2Updated = pending.First(m => m.Id == message2.Id);
        Assert.Equal(0, message2Updated.RetryCount); // Should not be affected
        Assert.Null(message2Updated.ErrorMessage);
    }

    #endregion

    #region SaveChangesAsync Tests

    /// <summary>
    /// Tests that SaveChangesAsync returns a completed task (no-op for Dapper).
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_ShouldReturnCompletedTask()
    {
        // Act
        var task = _store.SaveChangesAsync();

        // Assert
        Assert.True(task.IsCompleted);
        await task; // Should not throw
    }

    #endregion

    #region Custom Table Name Tests

    /// <summary>
    /// Tests that OutboxStoreDapper can use a custom table name.
    /// </summary>
    [Fact]
    public async Task Constructor_CustomTableName_ShouldUseCustomTable()
    {
        // Arrange
        var customTableName = "CustomOutbox";
        var connection = _fixture.CreateConnection();

        // Create custom table inline using Dapper
        await connection.ExecuteAsync($@"
            CREATE TABLE IF NOT EXISTS {customTableName} (
                Id UUID PRIMARY KEY,
                NotificationType VARCHAR(500) NOT NULL,
                Content TEXT NOT NULL,
                CreatedAtUtc TIMESTAMP NOT NULL,
                ProcessedAtUtc TIMESTAMP NULL,
                ErrorMessage TEXT NULL,
                RetryCount INTEGER NOT NULL DEFAULT 0,
                NextRetryAtUtc TIMESTAMP NULL
            )");

        var customStore = new OutboxStoreDapper(connection, customTableName);

        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "TestEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow
        };

        // Act
        await customStore.AddAsync(message);

        // Assert
        var pending = await customStore.GetPendingMessagesAsync(10, 5);
        Assert.Single(pending);
    }

    #endregion
}

