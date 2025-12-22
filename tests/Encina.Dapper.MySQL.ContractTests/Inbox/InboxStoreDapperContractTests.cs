using System.Diagnostics.CodeAnalysis;
using Encina.Dapper.MySQL.Inbox;
using Encina.TestInfrastructure.Extensions;
using Encina.TestInfrastructure.Fixtures;
using Encina.Messaging.Inbox;
using Xunit;

namespace Encina.Dapper.MySQL.Tests.Inbox;

/// <summary>
/// Contract tests for <see cref="InboxStoreDapper"/>.
/// Verifies compliance with <see cref="IInboxStore"/> interface contract.
/// Uses real MySQL database via Testcontainers.
/// </summary>
public sealed class InboxStoreDapperContractTests : IClassFixture<MySqlFixture>
{
    private readonly MySqlFixture _database;

    [SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance", Justification = "Contract tests intentionally use interface type to verify interface compliance")]
    private readonly IInboxStore _store;

    public InboxStoreDapperContractTests(MySqlFixture database)
    {
        _database = database;

        // Clear all data before each test to ensure clean state
        _database.ClearAllDataAsync().GetAwaiter().GetResult();

        _store = new InboxStoreDapper(_database.CreateConnection());
    }

    #region Contract: GetMessageAsync

    [Fact]
    public async Task GetMessageAsync_ImplementsContract_ReturnsIInboxMessage()
    {
        // Arrange
        var messageId = "contract-test-1";
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };
        await _store.AddAsync(message);

        // Act
        var result = await _store.GetMessageAsync(messageId);

        // Assert - Returns IInboxMessage interface
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IInboxMessage>(result);
    }

    [Fact]
    public async Task GetMessageAsync_NonExistentMessage_ReturnsNull()
    {
        // Act
        var result = await _store.GetMessageAsync("non-existent-id");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetMessageAsync_WithCancellationToken_AcceptsToken()
    {
        // Arrange
        var messageId = "cancellation-test";
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };
        await _store.AddAsync(message);
        using var cts = new CancellationTokenSource();

        // Act - Should not throw
        var result = await _store.GetMessageAsync(messageId, cts.Token);

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region Contract: AddAsync

    [Fact]
    public async Task AddAsync_AcceptsIInboxMessage_Succeeds()
    {
        // Arrange - Use interface type
        IInboxMessage message = new InboxMessage
        {
            MessageId = "interface-test",
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };

        // Act - Should not throw
        await _store.AddAsync(message);

        // Assert
        var retrieved = await _store.GetMessageAsync("interface-test");
        Assert.NotNull(retrieved);
    }

    [Fact]
    public async Task AddAsync_WithCancellationToken_AcceptsToken()
    {
        // Arrange
        var message = new InboxMessage
        {
            MessageId = "cancellation-add-test",
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };
        using var cts = new CancellationTokenSource();

        // Act - Should not throw
        await _store.AddAsync(message, cts.Token);

        // Assert
        var retrieved = await _store.GetMessageAsync("cancellation-add-test");
        Assert.NotNull(retrieved);
    }

    [Fact]
    public async Task AddAsync_PreservesAllRequiredProperties()
    {
        // Arrange
        var messageId = "properties-test";
        var receivedAt = DateTime.UtcNow;
        var expiresAt = DateTime.UtcNow.AddDays(30);
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "CreateOrderCommand",
            ReceivedAtUtc = receivedAt,
            ExpiresAtUtc = expiresAt,
            RetryCount = 0
        };

        // Act
        await _store.AddAsync(message);

        // Assert - All properties preserved
        var retrieved = await _store.GetMessageAsync(messageId);
        Assert.NotNull(retrieved);
        Assert.Equal(messageId, retrieved.MessageId);
        Assert.Equal("CreateOrderCommand", retrieved.RequestType);
        Assert.Equal(receivedAt, retrieved.ReceivedAtUtc, TimeSpan.FromSeconds(1));
        Assert.Equal(expiresAt, retrieved.ExpiresAtUtc, TimeSpan.FromSeconds(1));
        Assert.Equal(0, retrieved.RetryCount);
    }

    #endregion

    #region Contract: MarkAsProcessedAsync

    [Fact]
    public async Task MarkAsProcessedAsync_SetsProcessedAtUtc()
    {
        // Arrange
        var messageId = "mark-processed-test";
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };
        await _store.AddAsync(message);

        // Act
        await _store.MarkAsProcessedAsync(messageId, "{\"result\":\"success\"}");

        // Assert
        var retrieved = await _store.GetMessageAsync(messageId);
        Assert.NotNull(retrieved);
        Assert.NotNull(retrieved.ProcessedAtUtc);
        Assert.True(retrieved.IsProcessed);
    }

    [Fact]
    public async Task MarkAsProcessedAsync_StoresResponse()
    {
        // Arrange
        var messageId = "response-contract-test";
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };
        await _store.AddAsync(message);
        var response = "{\"orderId\":123,\"total\":249.99}";

        // Act
        await _store.MarkAsProcessedAsync(messageId, response);

        // Assert
        var retrieved = await _store.GetMessageAsync(messageId);
        Assert.NotNull(retrieved);
        Assert.Equal(response, retrieved.Response);
    }

    [Fact]
    public async Task MarkAsProcessedAsync_WithCancellationToken_AcceptsToken()
    {
        // Arrange
        var messageId = "cancellation-mark-test";
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };
        await _store.AddAsync(message);
        using var cts = new CancellationTokenSource();

        // Act - Should not throw
        await _store.MarkAsProcessedAsync(messageId, "{\"result\":\"ok\"}", cts.Token);

        // Assert
        var retrieved = await _store.GetMessageAsync(messageId);
        Assert.NotNull(retrieved);
        Assert.True(retrieved.IsProcessed);
    }

    #endregion

    #region Contract: MarkAsFailedAsync

    [Fact]
    public async Task MarkAsFailedAsync_IncrementsRetryCount()
    {
        // Arrange
        var messageId = "retry-contract-test";
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };
        await _store.AddAsync(message);

        // Act
        await _store.MarkAsFailedAsync(messageId, "Connection timeout", null);

        // Assert
        var retrieved = await _store.GetMessageAsync(messageId);
        Assert.NotNull(retrieved);
        Assert.Equal(1, retrieved.RetryCount);
    }

    [Fact]
    public async Task MarkAsFailedAsync_StoresErrorMessage()
    {
        // Arrange
        var messageId = "error-contract-test";
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };
        await _store.AddAsync(message);
        var errorMessage = "Database connection failed";

        // Act
        await _store.MarkAsFailedAsync(messageId, errorMessage, null);

        // Assert
        var retrieved = await _store.GetMessageAsync(messageId);
        Assert.NotNull(retrieved);
        Assert.Equal(errorMessage, retrieved.ErrorMessage);
    }

    [Fact]
    public async Task MarkAsFailedAsync_StoresNextRetryAtUtc()
    {
        // Arrange
        var messageId = "next-retry-contract-test";
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };
        await _store.AddAsync(message);
        var nextRetry = DateTime.UtcNow.AddMinutes(15);

        // Act
        await _store.MarkAsFailedAsync(messageId, "Temporary error", nextRetry);

        // Assert
        var retrieved = await _store.GetMessageAsync(messageId);
        Assert.NotNull(retrieved);
        Assert.NotNull(retrieved.NextRetryAtUtc);
        Assert.Equal(nextRetry, retrieved.NextRetryAtUtc.Value, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task MarkAsFailedAsync_WithCancellationToken_AcceptsToken()
    {
        // Arrange
        var messageId = "cancellation-failed-test";
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };
        await _store.AddAsync(message);
        using var cts = new CancellationTokenSource();

        // Act - Should not throw
        await _store.MarkAsFailedAsync(messageId, "Error", null, cts.Token);

        // Assert
        var retrieved = await _store.GetMessageAsync(messageId);
        Assert.NotNull(retrieved);
        Assert.Equal(1, retrieved.RetryCount);
    }

    #endregion

    #region Contract: GetExpiredMessagesAsync

    [Fact]
    public async Task GetExpiredMessagesAsync_ReturnsIEnumerableOfIInboxMessage()
    {
        // Arrange
        var message = new InboxMessage
        {
            MessageId = "expired-contract-test",
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow.AddDays(-40),
            ProcessedAtUtc = DateTime.UtcNow.AddDays(-35),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-1),
            RetryCount = 0
        };
        await _store.AddAsync(message);

        // Act
        var result = await _store.GetExpiredMessagesAsync(10);

        // Assert - Returns IEnumerable<IInboxMessage>
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IEnumerable<IInboxMessage>>(result);
        Assert.Single(result);
    }

    [Fact]
    public async Task GetExpiredMessagesAsync_RespectsBatchSize()
    {
        // Arrange - Create 5 expired messages
        for (int i = 0; i < 5; i++)
        {
            var message = new InboxMessage
            {
                MessageId = $"batch-contract-{i}",
                RequestType = "TestRequest",
                ReceivedAtUtc = DateTime.UtcNow.AddDays(-40),
                ProcessedAtUtc = DateTime.UtcNow.AddDays(-35),
                ExpiresAtUtc = DateTime.UtcNow.AddDays(-i - 1),
                RetryCount = 0
            };
            await _store.AddAsync(message);
        }

        // Act - Request only 3
        var result = await _store.GetExpiredMessagesAsync(3);

        // Assert
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetExpiredMessagesAsync_WithCancellationToken_AcceptsToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act - Should not throw
        var result = await _store.GetExpiredMessagesAsync(10, cts.Token);

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region Contract: RemoveExpiredMessagesAsync

    [Fact]
    public async Task RemoveExpiredMessagesAsync_AcceptsIEnumerableOfString()
    {
        // Arrange
        var messageId = "remove-contract-test";
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow.AddDays(-40),
            ProcessedAtUtc = DateTime.UtcNow.AddDays(-35),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-1),
            RetryCount = 0
        };
        await _store.AddAsync(message);

        IEnumerable<string> messageIds = new[] { messageId };

        // Act - Should not throw
        await _store.RemoveExpiredMessagesAsync(messageIds);

        // Assert
        var retrieved = await _store.GetMessageAsync(messageId);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task RemoveExpiredMessagesAsync_EmptyCollection_DoesNotThrow()
    {
        // Arrange
        IEnumerable<string> emptyCollection = [];

        // Act & Assert - Should not throw
        await _store.RemoveExpiredMessagesAsync(emptyCollection);
    }

    [Fact]
    public async Task RemoveExpiredMessagesAsync_WithCancellationToken_AcceptsToken()
    {
        // Arrange
        var messageId = "cancellation-remove-test";
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow.AddDays(-40),
            ProcessedAtUtc = DateTime.UtcNow.AddDays(-35),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-1),
            RetryCount = 0
        };
        await _store.AddAsync(message);
        using var cts = new CancellationTokenSource();

        // Act - Should not throw
        await _store.RemoveExpiredMessagesAsync(new[] { messageId }, cts.Token);

        // Assert
        var retrieved = await _store.GetMessageAsync(messageId);
        Assert.Null(retrieved);
    }

    #endregion

    #region Contract: Interface Compliance

    [Fact]
    public void InboxStoreDapper_ImplementsIInboxStore()
    {
        // Assert
        Assert.IsAssignableFrom<IInboxStore>(_store);
    }

    [Fact]
    public void InboxStoreDapper_CanBeUsedThroughInterface()
    {
        // Arrange
        IInboxStore interfaceReference = new InboxStoreDapper(_database.CreateConnection());

        // Assert
        Assert.NotNull(interfaceReference);
    }

    #endregion
}
