using Encina.Messaging.Inbox;

namespace Encina.Messaging.ContractTests.Inbox;

/// <summary>
/// Contract tests that verify all IInboxStore implementations follow the same behavioral contract.
/// These tests ensure consistency across all inbox store implementations (EF Core, Dapper, ADO.NET, etc.).
/// Implements IAsyncLifetime to automatically clean up after each test.
/// InitializeAsync and DisposeAsync are non-virtual interface implementations, preventing derived classes from overriding cleanup.
/// </summary>
public abstract class InboxStoreContractTests : IAsyncLifetime
{
    /// <summary>
    /// Gets the TimeProvider for deterministic time in tests.
    /// Override in derived classes to inject a custom provider.
    /// </summary>
    protected virtual TimeProvider TimeProvider => TimeProvider.System;

    protected abstract IInboxStore CreateStore();
    protected abstract IInboxMessage CreateMessage(string messageId, string requestType, DateTime? expiresAtUtc = null);

    /// <summary>
    /// The single override point for derived classes to implement cleanup logic.
    /// Called automatically after each test by DisposeAsync.
    /// </summary>
    protected abstract Task CleanupAsync();

    /// <summary>
    /// Called before each test. Non-virtual to prevent derived classes from bypassing base behavior.
    /// Override <see cref="InitializeTestAsync"/> if initialization is needed.
    /// </summary>
    public Task InitializeAsync() => InitializeTestAsync();

    /// <summary>
    /// Override in derived classes if per-test initialization is needed.
    /// </summary>
    protected virtual Task InitializeTestAsync() => Task.CompletedTask;

    /// <summary>
    /// Called after each test. Non-virtual to ensure cleanup always runs.
    /// Derived classes implement cleanup via <see cref="CleanupAsync"/>.
    /// </summary>
    public Task DisposeAsync() => CleanupAsync();

    #region GetMessageAsync Contract

    [Fact]
    public async Task GetMessageAsync_ExistingMessage_ShouldReturnMessage()
    {
        // Arrange
        var store = CreateStore();
        var messageId = Guid.NewGuid().ToString();
        var message = CreateMessage(messageId, "CreateOrder");
        await store.AddAsync(message);
        await store.SaveChangesAsync();

        // Act
        var retrieved = await store.GetMessageAsync(messageId);

        // Assert
        retrieved.ShouldNotBeNull();
        retrieved!.MessageId.ShouldBe(messageId);
    }

    [Fact]
    public async Task GetMessageAsync_NonExistentMessage_ShouldReturnNull()
    {
        // Arrange
        var store = CreateStore();
        var nonExistentId = Guid.NewGuid().ToString();

        // Act
        var retrieved = await store.GetMessageAsync(nonExistentId);

        // Assert
        retrieved.ShouldBeNull();
    }

    #endregion

    #region AddAsync Contract

    [Fact]
    public async Task AddAsync_ValidMessage_ShouldPersistMessage()
    {
        // Arrange
        var store = CreateStore();
        var messageId = Guid.NewGuid().ToString();
        var message = CreateMessage(messageId, "ProcessPayment");

        // Act
        await store.AddAsync(message);
        await store.SaveChangesAsync();

        // Assert
        var retrieved = await store.GetMessageAsync(messageId);
        retrieved.ShouldNotBeNull();
        retrieved!.MessageId.ShouldBe(messageId);
    }

    [Fact]
    public async Task AddAsync_ShouldPreserveAllProperties()
    {
        // Arrange
        var store = CreateStore();
        var messageId = Guid.NewGuid().ToString();
        var message = CreateMessage(messageId, "SendNotification");

        // Act
        await store.AddAsync(message);
        await store.SaveChangesAsync();

        // Assert
        var retrieved = await store.GetMessageAsync(messageId);
        retrieved.ShouldNotBeNull();
        retrieved!.RequestType.ShouldBe(message.RequestType);
        retrieved.RetryCount.ShouldBe(0);
    }

    [Fact]
    public async Task AddAsync_DuplicateIdempotencyKey_ShouldNotDuplicate()
    {
        // Arrange
        var store = CreateStore();
        var messageId = Guid.NewGuid().ToString();
        var message1 = CreateMessage(messageId, "Request1");
        var message2 = CreateMessage(messageId, "Request2");

        // Act
        await store.AddAsync(message1);
        await store.SaveChangesAsync();

        // Adding second message with same ID should be idempotent (no exception)
        await store.AddAsync(message2);
        await store.SaveChangesAsync();

        // Assert - original message should be preserved
        var retrieved = await store.GetMessageAsync(messageId);
        retrieved.ShouldNotBeNull();
        retrieved!.RequestType.ShouldBe("Request1");
    }

    #endregion

    #region MarkAsProcessedAsync Contract

    [Fact]
    public async Task MarkAsProcessedAsync_ExistingMessage_ShouldUpdateStatus()
    {
        // Arrange
        var store = CreateStore();
        var messageId = Guid.NewGuid().ToString();
        await store.AddAsync(CreateMessage(messageId, "TestRequest"));
        await store.SaveChangesAsync();

        var response = "{\"result\":\"success\"}";

        // Act
        await store.MarkAsProcessedAsync(messageId, response);
        await store.SaveChangesAsync();

        // Assert
        var retrieved = await store.GetMessageAsync(messageId);
        retrieved.ShouldNotBeNull();
        retrieved!.IsProcessed.ShouldBeTrue();
        retrieved.Response.ShouldBe(response);
    }

    [Fact]
    public async Task MarkAsProcessedAsync_ShouldSetProcessedTimestamp()
    {
        // Arrange
        var store = CreateStore();
        var messageId = Guid.NewGuid().ToString();
        await store.AddAsync(CreateMessage(messageId, "TestRequest"));
        await store.SaveChangesAsync();

        var beforeProcessing = TimeProvider.GetUtcNow().UtcDateTime;

        // Act
        await store.MarkAsProcessedAsync(messageId, "response");
        await store.SaveChangesAsync();

        // Assert
        var retrieved = await store.GetMessageAsync(messageId);
        retrieved.ShouldNotBeNull();
        retrieved!.ProcessedAtUtc.ShouldNotBeNull();
        retrieved.ProcessedAtUtc!.Value.ShouldBeGreaterThanOrEqualTo(beforeProcessing);
    }

    [Fact]
    public async Task MarkAsProcessedAsync_ResponseIsImmutable()
    {
        // Arrange
        var store = CreateStore();
        var messageId = Guid.NewGuid().ToString();
        await store.AddAsync(CreateMessage(messageId, "TestRequest"));
        await store.SaveChangesAsync();

        var originalResponse = "{\"result\":\"first\"}";
        await store.MarkAsProcessedAsync(messageId, originalResponse);
        await store.SaveChangesAsync();

        // Act - re-processing should be idempotent (no exception)
        await store.MarkAsProcessedAsync(messageId, "{\"result\":\"second\"}");
        await store.SaveChangesAsync();

        // Assert - original response should be preserved (idempotency)
        var retrieved = await store.GetMessageAsync(messageId);
        retrieved.ShouldNotBeNull();
        retrieved!.Response.ShouldBe(originalResponse);
    }

    #endregion

    #region MarkAsFailedAsync Contract

    [Fact]
    public async Task MarkAsFailedAsync_ShouldCaptureErrorDetails()
    {
        // Arrange
        var store = CreateStore();
        var messageId = Guid.NewGuid().ToString();
        await store.AddAsync(CreateMessage(messageId, "TestRequest"));
        await store.SaveChangesAsync();

        var errorMessage = "Processing failed: timeout";
        var nextRetry = TimeProvider.GetUtcNow().UtcDateTime.AddMinutes(5);

        // Act
        await store.MarkAsFailedAsync(messageId, errorMessage, nextRetry);
        await store.SaveChangesAsync();

        // Assert
        var retrieved = await store.GetMessageAsync(messageId);
        retrieved.ShouldNotBeNull();
        retrieved!.ErrorMessage.ShouldBe(errorMessage);
    }

    #endregion

    #region IncrementRetryCountAsync Contract

    [Fact]
    public async Task IncrementRetryCountAsync_ShouldIncrementCount()
    {
        // Arrange
        var store = CreateStore();
        var messageId = Guid.NewGuid().ToString();
        await store.AddAsync(CreateMessage(messageId, "TestRequest"));
        await store.SaveChangesAsync();

        // Act
        await store.IncrementRetryCountAsync(messageId);
        await store.SaveChangesAsync();

        // Assert
        var retrieved = await store.GetMessageAsync(messageId);
        retrieved.ShouldNotBeNull();
        retrieved!.RetryCount.ShouldBe(1);
    }

    [Fact]
    public async Task IncrementRetryCountAsync_IsMonotonicallyIncreasing()
    {
        // Arrange
        var store = CreateStore();
        var messageId = Guid.NewGuid().ToString();
        await store.AddAsync(CreateMessage(messageId, "TestRequest"));
        await store.SaveChangesAsync();

        // Act
        for (int i = 0; i < 5; i++)
        {
            await store.IncrementRetryCountAsync(messageId);
            await store.SaveChangesAsync();
        }

        // Assert
        var retrieved = await store.GetMessageAsync(messageId);
        retrieved.ShouldNotBeNull();
        retrieved!.RetryCount.ShouldBe(5);
    }

    #endregion

    #region GetExpiredMessagesAsync Contract

    [Fact]
    public async Task GetExpiredMessagesAsync_NoExpiredMessages_ShouldReturnEmptyCollection()
    {
        // Arrange
        var store = CreateStore();

        // Act
        var expired = await store.GetExpiredMessagesAsync(batchSize: 10);

        // Assert
        expired.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetExpiredMessagesAsync_ShouldRespectBatchSize()
    {
        // Arrange
        var store = CreateStore();
        var pastTime = TimeProvider.GetUtcNow().AddMinutes(-1).UtcDateTime;

        for (int i = 0; i < 10; i++)
        {
            var msg = CreateMessage(Guid.NewGuid().ToString(), "Request", expiresAtUtc: pastTime);
            await store.AddAsync(msg);
        }
        await store.SaveChangesAsync();

        // Act
        var expired = await store.GetExpiredMessagesAsync(batchSize: 5);

        // Assert
        expired.Count().ShouldBeLessThanOrEqualTo(5);
    }

    #endregion

    #region RemoveExpiredMessagesAsync Contract

    [Fact]
    public async Task RemoveExpiredMessagesAsync_ShouldRemoveSpecifiedMessages()
    {
        // Arrange
        var store = CreateStore();
        var id1 = Guid.NewGuid().ToString();
        var id2 = Guid.NewGuid().ToString();
        var id3 = Guid.NewGuid().ToString();

        await store.AddAsync(CreateMessage(id1, "Request1"));
        await store.AddAsync(CreateMessage(id2, "Request2"));
        await store.AddAsync(CreateMessage(id3, "Request3"));
        await store.SaveChangesAsync();

        // Act - remove only first two
        await store.RemoveExpiredMessagesAsync([id1, id2]);
        await store.SaveChangesAsync();

        // Assert
        var msg1 = await store.GetMessageAsync(id1);
        var msg2 = await store.GetMessageAsync(id2);
        var msg3 = await store.GetMessageAsync(id3);

        msg1.ShouldBeNull();
        msg2.ShouldBeNull();
        msg3.ShouldNotBeNull();
    }

    [Fact]
    public async Task RemoveExpiredMessagesAsync_EmptyList_ShouldNotThrow()
    {
        // Arrange
        var store = CreateStore();

        // Act & Assert - should not throw
        await store.RemoveExpiredMessagesAsync([]);
        await store.SaveChangesAsync();
    }

    #endregion

    #region SaveChangesAsync Contract

    [Fact]
    public async Task SaveChangesAsync_ShouldPersistChanges()
    {
        // Arrange
        var store = CreateStore();
        var messageId = Guid.NewGuid().ToString();
        await store.AddAsync(CreateMessage(messageId, "TestRequest"));

        // Act
        await store.SaveChangesAsync();

        // Assert
        var retrieved = await store.GetMessageAsync(messageId);
        retrieved.ShouldNotBeNull();
    }

    #endregion
}

/// <summary>
/// In-memory implementation of IInboxMessage for contract testing.
/// </summary>
internal sealed class InMemoryInboxMessageForContract : IInboxMessage
{
    public string MessageId { get; set; } = "";
    public string RequestType { get; set; } = "";
    public string? Response { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextRetryAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the time provider for deterministic testing.
    /// </summary>
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    public bool IsProcessed => ProcessedAtUtc.HasValue;
    public bool IsExpired() => TimeProvider.GetUtcNow().UtcDateTime >= ExpiresAtUtc;
}

/// <summary>
/// In-memory implementation of IInboxStore for contract testing.
/// </summary>
internal sealed class InMemoryInboxStoreForContract : IInboxStore
{
    private readonly List<InMemoryInboxMessageForContract> _messages = [];
    private readonly TimeProvider _timeProvider;

    public InMemoryInboxStoreForContract(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public Task<IInboxMessage?> GetMessageAsync(string messageId, CancellationToken cancellationToken = default)
    {
        var message = _messages.FirstOrDefault(m => m.MessageId == messageId);
        if (message != null)
        {
            message.TimeProvider = _timeProvider;
        }
        return Task.FromResult<IInboxMessage?>(message);
    }

    public Task AddAsync(IInboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message is not InMemoryInboxMessageForContract inMemoryMessage)
        {
            throw new ArgumentException(
                $"Message must be of type {nameof(InMemoryInboxMessageForContract)}, but was {message.GetType().Name}.",
                nameof(message));
        }

        // Check for duplicate
        if (!_messages.Any(m => m.MessageId == inMemoryMessage.MessageId))
        {
            inMemoryMessage.TimeProvider = _timeProvider;
            _messages.Add(inMemoryMessage);
        }

        return Task.CompletedTask;
    }

    public Task MarkAsProcessedAsync(string messageId, string response, CancellationToken cancellationToken = default)
    {
        var message = _messages.FirstOrDefault(m => m.MessageId == messageId);
        if (message != null && !message.IsProcessed)
        {
            message.ProcessedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
            message.Response = response;
        }
        return Task.CompletedTask;
    }

    public Task MarkAsFailedAsync(
        string messageId,
        string errorMessage,
        DateTime? nextRetryAt,
        CancellationToken cancellationToken = default)
    {
        var message = _messages.FirstOrDefault(m => m.MessageId == messageId);
        if (message != null)
        {
            message.ErrorMessage = errorMessage;
            message.NextRetryAtUtc = nextRetryAt;
        }
        return Task.CompletedTask;
    }

    public Task IncrementRetryCountAsync(string messageId, CancellationToken cancellationToken = default)
    {
        var message = _messages.FirstOrDefault(m => m.MessageId == messageId);
        if (message != null)
        {
            message.RetryCount++;
        }
        return Task.CompletedTask;
    }

    public Task<IEnumerable<IInboxMessage>> GetExpiredMessagesAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var expired = _messages
            .Where(m => m.ExpiresAtUtc <= now)
            .Take(batchSize)
            .Cast<IInboxMessage>()
            .ToList();

        return Task.FromResult<IEnumerable<IInboxMessage>>(expired);
    }

    public Task RemoveExpiredMessagesAsync(
        IEnumerable<string> messageIds,
        CancellationToken cancellationToken = default)
    {
        var idsSet = messageIds.ToHashSet();
        _messages.RemoveAll(m => idsSet.Contains(m.MessageId));
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void Clear() => _messages.Clear();
}

/// <summary>
/// Concrete implementation of contract tests for in-memory inbox store.
/// Uses FakeTimeProvider for deterministic time operations.
/// </summary>
public sealed class InMemoryInboxStoreContractTests : InboxStoreContractTests
{
    private static readonly DateTime BaseTime = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private Microsoft.Extensions.Time.Testing.FakeTimeProvider? _fakeTimeProvider;
    private InMemoryInboxStoreForContract? _currentStore;

    protected override TimeProvider TimeProvider =>
        _fakeTimeProvider ?? throw new InvalidOperationException(
            $"{nameof(_fakeTimeProvider)} has not been initialized. Ensure {nameof(InitializeTestAsync)} has been called.");

    protected override Task InitializeTestAsync()
    {
        _fakeTimeProvider = new Microsoft.Extensions.Time.Testing.FakeTimeProvider(BaseTime);
        return Task.CompletedTask;
    }

    protected override IInboxStore CreateStore()
    {
        _currentStore = new InMemoryInboxStoreForContract(TimeProvider);
        return _currentStore;
    }

    protected override IInboxMessage CreateMessage(string messageId, string requestType, DateTime? expiresAtUtc = null)
    {
        var fakeTimeProvider = (Microsoft.Extensions.Time.Testing.FakeTimeProvider)TimeProvider;
        var receivedUtc = fakeTimeProvider.GetUtcNow().UtcDateTime;

        return new InMemoryInboxMessageForContract
        {
            MessageId = messageId,
            RequestType = requestType,
            ReceivedAtUtc = receivedUtc,
            ExpiresAtUtc = expiresAtUtc ?? receivedUtc.AddDays(7),
            RetryCount = 0,
            TimeProvider = fakeTimeProvider
        };
    }

    protected override Task CleanupAsync()
    {
        _currentStore?.Clear();
        return Task.CompletedTask;
    }
}
