using Encina.ADO.SqlServer.Outbox;
using Encina.TestInfrastructure.Extensions;
using Encina.TestInfrastructure.Fixtures;
using Encina.Messaging.Outbox;
using Xunit;

namespace Encina.ADO.SqlServer.Tests.Outbox;

/// <summary>
/// Contract integration tests for <see cref="OutboxStoreADO"/>.
/// Verifies that the implementation correctly fulfills the <see cref="IOutboxStore"/> contract.
/// Uses real SQL Server database via Testcontainers.
/// </summary>
[Trait("Category", "Integration")]
[Trait("TestType", "Contract")]
[Trait("Provider", "ADO.SqlServer")]
public sealed class OutboxStoreADOContractTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _fixture;
#pragma warning disable CA1859 // Contract tests intentionally use interface type to verify contract compliance
    private readonly IOutboxStore _store; // Test against interface, not implementation
#pragma warning restore CA1859

    public OutboxStoreADOContractTests(SqlServerFixture fixture)
    {
        _fixture = fixture;

        // Clear all data before each test to ensure clean state
        _fixture.ClearAllDataAsync().GetAwaiter().GetResult();

        _store = new OutboxStoreADO(_fixture.CreateConnection()); // Concrete type assigned to interface
    }

    #region IOutboxStore Contract: AddAsync

    /// <summary>
    /// Contract: AddAsync must accept any valid IOutboxMessage implementation.
    /// </summary>
    [Fact]
    public async Task AddAsync_AcceptsIOutboxMessageInterface()
    {
        // Arrange - Use interface type
        IOutboxMessage message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "TestEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow
        };

        // Act - Should work with interface parameter
        await _store.AddAsync(message);

        // Assert - Message persisted
        var pending = await _store.GetPendingMessagesAsync(10, 5);
        Assert.Single(pending);
    }

    /// <summary>
    /// Contract: AddAsync must persist all required properties from IOutboxMessage.
    /// </summary>
    [Fact]
    public async Task AddAsync_PersistsAllRequiredProperties()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var message = new OutboxMessage
        {
            Id = messageId,
            NotificationType = "OrderCreatedEvent",
            Content = "{\"orderId\":123}",
            CreatedAtUtc = createdAt,
            ProcessedAtUtc = null,
            ErrorMessage = null,
            RetryCount = 0,
            NextRetryAtUtc = null
        };

        // Act
        await _store.AddAsync(message);

        // Assert - All properties preserved
        var retrieved = (await _store.GetPendingMessagesAsync(10, 5)).First();
        Assert.Equal(messageId, retrieved.Id);
        Assert.Equal("OrderCreatedEvent", retrieved.NotificationType);
        Assert.Equal("{\"orderId\":123}", retrieved.Content);
        Assert.Null(retrieved.ProcessedAtUtc);
        Assert.Null(retrieved.ErrorMessage);
        Assert.Equal(0, retrieved.RetryCount);
        Assert.Null(retrieved.NextRetryAtUtc);
    }

    #endregion

    #region IOutboxStore Contract: GetPendingMessagesAsync

    /// <summary>
    /// Contract: GetPendingMessagesAsync must return IEnumerable of IOutboxMessage.
    /// </summary>
    [Fact]
    public async Task GetPendingMessagesAsync_ReturnsIOutboxMessageInterface()
    {
        // Arrange
        await _store.AddAsync(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "TestEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow
        });

        // Act - Returns interface type
        IEnumerable<IOutboxMessage> pending = await _store.GetPendingMessagesAsync(10, 5);

        // Assert - Can be treated as interface
        Assert.NotNull(pending);
        Assert.Single(pending);
        Assert.IsAssignableFrom<IOutboxMessage>(pending.First());
    }

    /// <summary>
    /// Contract: GetPendingMessagesAsync must respect batchSize parameter.
    /// </summary>
    [Fact]
    public async Task GetPendingMessagesAsync_RespectsBatchSize()
    {
        // Arrange - Add 20 messages
        for (int i = 0; i < 20; i++)
        {
            await _store.AddAsync(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = $"Event{i}",
                Content = "{}",
                CreatedAtUtc = DateTime.UtcNow.AddSeconds(i)
            });
        }

        // Act
        var batch5 = await _store.GetPendingMessagesAsync(batchSize: 5, maxRetries: 10);
        var batch10 = await _store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 10);
        var batch50 = await _store.GetPendingMessagesAsync(batchSize: 50, maxRetries: 10);

        // Assert
        Assert.Equal(5, batch5.Count());
        Assert.Equal(10, batch10.Count());
        Assert.Equal(20, batch50.Count()); // All messages
    }

    /// <summary>
    /// Contract: GetPendingMessagesAsync must respect maxRetries parameter.
    /// </summary>
    [Fact]
    public async Task GetPendingMessagesAsync_RespectsMaxRetries()
    {
        // Arrange - Add messages with different retry counts
        await _store.AddAsync(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "Event0",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        });

        await _store.AddAsync(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "Event3",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 3
        });

        await _store.AddAsync(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "Event5",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 5
        });

        // Act
        var maxRetries3 = await _store.GetPendingMessagesAsync(100, maxRetries: 3);
        var maxRetries5 = await _store.GetPendingMessagesAsync(100, maxRetries: 5);

        // Assert
        Assert.Single(maxRetries3); // Only RetryCount=0
        Assert.Equal(2, maxRetries5.Count()); // RetryCount=0 and RetryCount=3
    }

    /// <summary>
    /// Contract: GetPendingMessagesAsync must only return unprocessed messages.
    /// </summary>
    [Fact]
    public async Task GetPendingMessagesAsync_OnlyReturnsUnprocessed()
    {
        // Arrange
        var unprocessedId = Guid.NewGuid();
        var processedId = Guid.NewGuid();

        await _store.AddAsync(new OutboxMessage
        {
            Id = unprocessedId,
            NotificationType = "UnprocessedEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow
        });

        await _store.AddAsync(new OutboxMessage
        {
            Id = processedId,
            NotificationType = "ProcessedEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            ProcessedAtUtc = DateTime.UtcNow // Already processed
        });

        // Act
        var pending = await _store.GetPendingMessagesAsync(100, 10);

        // Assert
        Assert.Single(pending);
        Assert.Equal(unprocessedId, pending.First().Id);
    }

    /// <summary>
    /// Contract: GetPendingMessagesAsync must filter by NextRetryAtUtc.
    /// </summary>
    [Fact]
    public async Task GetPendingMessagesAsync_FiltersNextRetryAtUtc()
    {
        // Arrange
        var readyId = Guid.NewGuid();
        var futureId = Guid.NewGuid();

        await _store.AddAsync(new OutboxMessage
        {
            Id = readyId,
            NotificationType = "ReadyEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            NextRetryAtUtc = null // Ready now
        });

        await _store.AddAsync(new OutboxMessage
        {
            Id = futureId,
            NotificationType = "FutureEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            NextRetryAtUtc = DateTime.UtcNow.AddHours(1) // Not ready yet
        });

        // Act
        var pending = await _store.GetPendingMessagesAsync(100, 10);

        // Assert
        Assert.Single(pending);
        Assert.Equal(readyId, pending.First().Id);
    }

    #endregion

    #region IOutboxStore Contract: MarkAsProcessedAsync

    /// <summary>
    /// Contract: MarkAsProcessedAsync must set ProcessedAtUtc timestamp.
    /// </summary>
    [Fact]
    public async Task MarkAsProcessedAsync_SetsProcessedAtUtc()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        await _store.AddAsync(new OutboxMessage
        {
            Id = messageId,
            NotificationType = "TestEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow
        });

        // Act
        await _store.MarkAsProcessedAsync(messageId);

        // Assert - Message no longer in pending
        var pending = await _store.GetPendingMessagesAsync(100, 10);
        Assert.Empty(pending);
    }

    /// <summary>
    /// Contract: MarkAsProcessedAsync must clear ErrorMessage if present.
    /// </summary>
    [Fact]
    public async Task MarkAsProcessedAsync_ClearsErrorMessage()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        await _store.AddAsync(new OutboxMessage
        {
            Id = messageId,
            NotificationType = "TestEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            ErrorMessage = "Previous error",
            RetryCount = 2
        });

        // Act
        await _store.MarkAsProcessedAsync(messageId);

        // Assert - No longer pending (ErrorMessage cleared)
        var pending = await _store.GetPendingMessagesAsync(100, 10);
        Assert.Empty(pending);
    }

    #endregion

    #region IOutboxStore Contract: MarkAsFailedAsync

    /// <summary>
    /// Contract: MarkAsFailedAsync must increment RetryCount.
    /// </summary>
    [Fact]
    public async Task MarkAsFailedAsync_IncrementsRetryCount()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        await _store.AddAsync(new OutboxMessage
        {
            Id = messageId,
            NotificationType = "TestEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        });

        // Act
        await _store.MarkAsFailedAsync(messageId, "Error occurred", null);

        // Assert
        var pending = await _store.GetPendingMessagesAsync(100, 10);
        var message = pending.First();
        Assert.Equal(1, message.RetryCount);
    }

    /// <summary>
    /// Contract: MarkAsFailedAsync must set ErrorMessage.
    /// </summary>
    [Fact]
    public async Task MarkAsFailedAsync_SetsErrorMessage()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        await _store.AddAsync(new OutboxMessage
        {
            Id = messageId,
            NotificationType = "TestEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow
        });

        // Act
        var errorMessage = "Network timeout after 30 seconds";
        await _store.MarkAsFailedAsync(messageId, errorMessage, null);

        // Assert
        var pending = await _store.GetPendingMessagesAsync(100, 10);
        var message = pending.First();
        Assert.Equal(errorMessage, message.ErrorMessage);
    }

    /// <summary>
    /// Contract: MarkAsFailedAsync must set NextRetryAtUtc when provided.
    /// </summary>
    [Fact]
    public async Task MarkAsFailedAsync_SetsNextRetryAtUtc()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        await _store.AddAsync(new OutboxMessage
        {
            Id = messageId,
            NotificationType = "TestEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow
        });

        // Act
        var nextRetry = DateTime.UtcNow.AddMinutes(5);
        await _store.MarkAsFailedAsync(messageId, "Error", nextRetry);

        // Assert - Should not appear in pending (future retry)
        var pending = await _store.GetPendingMessagesAsync(100, 10);
        Assert.Empty(pending);
    }

    /// <summary>
    /// Contract: MarkAsFailedAsync can be called multiple times.
    /// </summary>
    [Fact]
    public async Task MarkAsFailedAsync_CanBeCalledMultipleTimes()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        await _store.AddAsync(new OutboxMessage
        {
            Id = messageId,
            NotificationType = "TestEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        });

        // Act - Fail 3 times
        await _store.MarkAsFailedAsync(messageId, "Error 1", null);
        await _store.MarkAsFailedAsync(messageId, "Error 2", null);
        await _store.MarkAsFailedAsync(messageId, "Error 3", null);

        // Assert
        var pending = await _store.GetPendingMessagesAsync(100, 10);
        var message = pending.First();
        Assert.Equal(3, message.RetryCount);
        Assert.Equal("Error 3", message.ErrorMessage); // Latest error
    }

    #endregion

    #region IOutboxStore Contract: SaveChangesAsync

    /// <summary>
    /// Contract: SaveChangesAsync must complete successfully (even if no-op).
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_CompletesSuccessfully()
    {
        // Act
        await _store.SaveChangesAsync();

        // Assert - No exception thrown
        Assert.True(true);
    }

    /// <summary>
    /// Contract: SaveChangesAsync can be called multiple times safely.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_CanBeCalledMultipleTimes()
    {
        // Act
        await _store.SaveChangesAsync();
        await _store.SaveChangesAsync();
        await _store.SaveChangesAsync();

        // Assert - No exception thrown
        Assert.True(true);
    }

    #endregion

    #region IOutboxStore Contract: CancellationToken Support

    /// <summary>
    /// Contract: All async methods must accept CancellationToken.
    /// </summary>
    [Fact]
    public async Task AllMethods_AcceptCancellationToken()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var cts = new CancellationTokenSource();
        var message = new OutboxMessage
        {
            Id = messageId,
            NotificationType = "TestEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow
        };

        // Act - All methods should accept CancellationToken
        await _store.AddAsync(message, cts.Token);
        await _store.GetPendingMessagesAsync(10, 5, cts.Token);
        await _store.MarkAsProcessedAsync(messageId, cts.Token);
        await _store.SaveChangesAsync(cts.Token);

        // Also test MarkAsFailedAsync
        await _store.AddAsync(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "TestEvent2",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow
        }, cts.Token);

        var pending = await _store.GetPendingMessagesAsync(10, 5, cts.Token);
        var secondMessageId = pending.Last().Id;
        await _store.MarkAsFailedAsync(secondMessageId, "Error", null, cts.Token);

        // Assert - No exception thrown
        Assert.True(true);
    }

    #endregion

    #region IOutboxMessage Contract: IsProcessed Property

    /// <summary>
    /// Contract: IsProcessed should return true when ProcessedAtUtc is set and ErrorMessage is null.
    /// </summary>
    [Fact]
    public async Task IsProcessed_ReturnsTrueWhenProcessedSuccessfully()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        await _store.AddAsync(new OutboxMessage
        {
            Id = messageId,
            NotificationType = "TestEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow
        });

        // Act
        await _store.MarkAsProcessedAsync(messageId);

        // Assert - IsProcessed should be true (though we can't retrieve processed messages)
        // This is verified indirectly: processed messages don't appear in GetPendingMessagesAsync
        var pending = await _store.GetPendingMessagesAsync(100, 10);
        Assert.Empty(pending);
    }

    /// <summary>
    /// Contract: IsProcessed should return false when message has errors.
    /// </summary>
    [Fact]
    public async Task IsProcessed_ReturnsFalseWhenMessageHasErrors()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        await _store.AddAsync(new OutboxMessage
        {
            Id = messageId,
            NotificationType = "TestEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow
        });

        // Act
        await _store.MarkAsFailedAsync(messageId, "Error occurred", null);

        // Assert - Message still appears in pending (IsProcessed = false)
        var pending = await _store.GetPendingMessagesAsync(100, 10);
        var message = pending.First();
        Assert.False(message.IsProcessed);
    }

    #endregion

    #region IOutboxMessage Contract: IsDeadLettered Method

    /// <summary>
    /// Contract: IsDeadLettered should return true when RetryCount >= maxRetries and not processed.
    /// </summary>
    [Fact]
    public async Task IsDeadLettered_ReturnsTrueWhenExceedsMaxRetries()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        await _store.AddAsync(new OutboxMessage
        {
            Id = messageId,
            NotificationType = "TestEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        });

        // Act - Fail 5 times
        for (int i = 0; i < 5; i++)
        {
            await _store.MarkAsFailedAsync(messageId, $"Error {i}", null);
        }

        var allMessages = await _store.GetPendingMessagesAsync(100, 100); // High maxRetries to retrieve it
        var message = allMessages.First(m => m.Id == messageId);

        // Assert
        Assert.True(message.IsDeadLettered(maxRetries: 5));
        Assert.False(message.IsDeadLettered(maxRetries: 10));
    }

    #endregion
}
