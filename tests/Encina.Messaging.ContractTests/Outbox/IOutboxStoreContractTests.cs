using Encina.Messaging.Outbox;

namespace Encina.Messaging.ContractTests.Outbox;

/// <summary>
/// Contract tests that verify all IOutboxStore implementations follow the same behavioral contract.
/// These tests ensure consistency across all outbox store implementations (EF Core, Dapper, ADO.NET, etc.).
/// Implements IAsyncLifetime to automatically clean up after each test.
/// </summary>
/// <remarks>
/// <para>
/// These tests target both transactional stores (EF Core, ADO.NET) and in-memory stores.
/// In-memory stores persist immediately on AddAsync, making SaveChangesAsync idempotent.
/// For true deferred-persistence testing, use integration tests with real databases.
/// </para>
/// </remarks>
public abstract class IOutboxStoreContractTests : IAsyncLifetime
{
    protected abstract IOutboxStore CreateStore();
    protected abstract IOutboxMessage CreateMessage(Guid id, string notificationType);
    protected abstract Task CleanupAsync();

    /// <summary>
    /// Called before each test. Override in derived classes if initialization is needed.
    /// </summary>
    public virtual Task InitializeAsync() => Task.CompletedTask;

    /// <summary>
    /// Called after each test to ensure proper cleanup and test isolation.
    /// </summary>
    public virtual Task DisposeAsync() => CleanupAsync();

    #region AddAsync Contract

    [Fact]
    public async Task AddAsync_ValidMessage_ShouldPersistMessage()
    {
        // Arrange
        var store = CreateStore();
        var message = CreateMessage(Guid.NewGuid(), "OrderCreated");

        // Act
        await store.AddAsync(message);
        await store.SaveChangesAsync();

        // Assert
        var pending = await store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3);
        pending.ShouldContain(m => m.Id == message.Id);
    }

    [Fact]
    public async Task AddAsync_ShouldPreserveAllProperties()
    {
        // Arrange
        var store = CreateStore();
        var id = Guid.NewGuid();
        var message = CreateMessage(id, "PaymentReceived");

        // Act
        await store.AddAsync(message);
        await store.SaveChangesAsync();

        // Assert
        var pending = await store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3);
        var retrieved = pending.FirstOrDefault(m => m.Id == id);
        retrieved.ShouldNotBeNull();
        retrieved!.NotificationType.ShouldBe(message.NotificationType);
        retrieved.Content.ShouldBe(message.Content);
        retrieved.RetryCount.ShouldBe(0);
    }

    #endregion

    #region GetPendingMessagesAsync Contract

    [Fact]
    public async Task GetPendingMessagesAsync_NoPendingMessages_ShouldReturnEmptyCollection()
    {
        // Arrange
        var store = CreateStore();

        // Act
        var pending = await store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
        pending.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPendingMessagesAsync_WithPendingMessages_ShouldReturnMessages()
    {
        // Arrange
        var store = CreateStore();
        await store.AddAsync(CreateMessage(Guid.NewGuid(), "Event1"));
        await store.AddAsync(CreateMessage(Guid.NewGuid(), "Event2"));
        await store.SaveChangesAsync();

        // Act
        var pending = await store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
        pending.Count().ShouldBe(2);
    }

    [Fact]
    public async Task GetPendingMessagesAsync_ShouldRespectBatchSize()
    {
        // Arrange
        var store = CreateStore();
        for (int i = 0; i < 10; i++)
        {
            await store.AddAsync(CreateMessage(Guid.NewGuid(), $"Event{i}"));
        }
        await store.SaveChangesAsync();

        // Act
        var pending = await store.GetPendingMessagesAsync(batchSize: 5, maxRetries: 3);

        // Assert
        pending.Count().ShouldBe(5);
    }

    [Fact]
    public async Task GetPendingMessagesAsync_ShouldExcludeProcessedMessages()
    {
        // Arrange
        var store = CreateStore();
        var processedId = Guid.NewGuid();
        var pendingId = Guid.NewGuid();

        await store.AddAsync(CreateMessage(processedId, "ProcessedEvent"));
        await store.AddAsync(CreateMessage(pendingId, "PendingEvent"));
        await store.SaveChangesAsync();

        await store.MarkAsProcessedAsync(processedId);
        await store.SaveChangesAsync();

        // Act
        var pending = await store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
        pending.ShouldNotContain(m => m.Id == processedId);
        pending.ShouldContain(m => m.Id == pendingId);
    }

    [Fact]
    public async Task GetPendingMessagesAsync_ShouldExcludeMessagesExceedingMaxRetries()
    {
        // Arrange
        var store = CreateStore();
        var overRetryId = Guid.NewGuid();
        var underRetryId = Guid.NewGuid();

        await store.AddAsync(CreateMessage(overRetryId, "OverRetryEvent"));
        await store.AddAsync(CreateMessage(underRetryId, "UnderRetryEvent"));
        await store.SaveChangesAsync();

        // Fail the first message multiple times to exceed maxRetries
        for (int i = 0; i <= 3; i++)
        {
            await store.MarkAsFailedAsync(overRetryId, "Test error", DateTime.UtcNow.AddMinutes(-1));
            await store.SaveChangesAsync();
        }

        // Act
        var pending = await store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
        pending.ShouldNotContain(m => m.Id == overRetryId);
        pending.ShouldContain(m => m.Id == underRetryId);
    }

    #endregion

    #region MarkAsProcessedAsync Contract

    [Fact]
    public async Task MarkAsProcessedAsync_ExistingMessage_ShouldSetProcessedTimestamp()
    {
        // Arrange
        var store = CreateStore();
        var id = Guid.NewGuid();
        var message = CreateMessage(id, "TestEvent");
        await store.AddAsync(message);
        await store.SaveChangesAsync();

        // Act
        await store.MarkAsProcessedAsync(id);
        await store.SaveChangesAsync();

        // Assert - message should no longer be pending
        var pending = await store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3);
        pending.ShouldNotContain(m => m.Id == id);
    }

    [Fact]
    public async Task MarkAsProcessedAsync_IsIdempotent()
    {
        // Arrange
        var store = CreateStore();
        var id = Guid.NewGuid();
        await store.AddAsync(CreateMessage(id, "TestEvent"));
        await store.SaveChangesAsync();

        // Act - Mark as processed twice
        await store.MarkAsProcessedAsync(id);
        await store.SaveChangesAsync();

        // Should not throw
        await store.MarkAsProcessedAsync(id);
        await store.SaveChangesAsync();

        // Assert
        var pending = await store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3);
        pending.ShouldNotContain(m => m.Id == id);
    }

    [Fact]
    public async Task MarkAsProcessedAsync_NonExistentMessage_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var store = CreateStore();
        var nonExistentId = Guid.NewGuid();

        // Act
        var act = async () => await store.MarkAsProcessedAsync(nonExistentId);

        // Assert
        var ex = await act.ShouldThrowAsync<KeyNotFoundException>();
        ex.Message.ShouldContain(nonExistentId.ToString());
    }

    #endregion

    #region MarkAsFailedAsync Contract

    [Fact]
    public async Task MarkAsFailedAsync_ShouldCaptureErrorMessage()
    {
        // Arrange
        var store = CreateStore();
        var id = Guid.NewGuid();
        await store.AddAsync(CreateMessage(id, "TestEvent"));
        await store.SaveChangesAsync();

        var errorMessage = "Processing failed due to timeout";
        // Use a past retry time so the message appears in pending
        var nextRetry = DateTime.UtcNow.AddMinutes(-1);

        // Act
        await store.MarkAsFailedAsync(id, errorMessage, nextRetry);
        await store.SaveChangesAsync();

        // Assert
        var pending = await store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 10);
        var failed = pending.FirstOrDefault(m => m.Id == id);
        failed.ShouldNotBeNull();
        failed!.ErrorMessage.ShouldBe(errorMessage);
    }

    [Fact]
    public async Task MarkAsFailedAsync_ShouldIncrementRetryCount()
    {
        // Arrange
        var store = CreateStore();
        var id = Guid.NewGuid();
        await store.AddAsync(CreateMessage(id, "TestEvent"));
        await store.SaveChangesAsync();

        // Act
        await store.MarkAsFailedAsync(id, "Error 1", DateTime.UtcNow.AddMinutes(-1));
        await store.SaveChangesAsync();

        // Assert
        var pending = await store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 10);
        var message = pending.FirstOrDefault(m => m.Id == id);
        message.ShouldNotBeNull();
        message!.RetryCount.ShouldBe(1);

        // Act again
        await store.MarkAsFailedAsync(id, "Error 2", DateTime.UtcNow.AddMinutes(-1));
        await store.SaveChangesAsync();

        // Assert
        pending = await store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 10);
        message = pending.FirstOrDefault(m => m.Id == id);
        message.ShouldNotBeNull();
        message!.RetryCount.ShouldBe(2);
    }

    [Fact]
    public async Task MarkAsFailedAsync_WithNullNextRetry_ShouldStillWork()
    {
        // Arrange
        var store = CreateStore();
        var id = Guid.NewGuid();
        await store.AddAsync(CreateMessage(id, "TestEvent"));
        await store.SaveChangesAsync();

        // Act
        await store.MarkAsFailedAsync(id, "Error", nextRetryAt: null);
        await store.SaveChangesAsync();

        // Assert - should not throw
        var pending = await store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 10);
        var message = pending.FirstOrDefault(m => m.Id == id);
        message.ShouldNotBeNull();
        message!.RetryCount.ShouldBe(1);
    }

    [Fact]
    public async Task MarkAsFailedAsync_NonExistentMessage_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var store = CreateStore();
        var nonExistentId = Guid.NewGuid();

        // Act
        var act = async () => await store.MarkAsFailedAsync(nonExistentId, "Error", nextRetryAt: null);

        // Assert
        var ex = await act.ShouldThrowAsync<KeyNotFoundException>();
        ex.Message.ShouldContain(nonExistentId.ToString());
    }

    #endregion

    #region SaveChangesAsync Contract

    /// <summary>
    /// Verifies SaveChangesAsync persists changes for transactional stores.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_ShouldPersistChanges()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var store = CreateStore();
        await store.AddAsync(CreateMessage(messageId, "TestEvent"));

        // Act
        await store.SaveChangesAsync();

        // Assert - message should be retrievable after save
        var afterSave = await store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3);
        afterSave.ShouldContain(m => m.Id == messageId, "Message should be persisted and retrievable after SaveChangesAsync");
    }

    #endregion
}

/// <summary>
/// Extension methods for IOutboxMessage query logic.
/// Extracts view/query logic from the data model to keep it focused on data.
/// </summary>
internal static class OutboxMessageExtensions
{
    /// <summary>
    /// Determines whether the message has been processed successfully.
    /// </summary>
    public static bool IsProcessed(this IOutboxMessage message) =>
        message.ProcessedAtUtc.HasValue;

    /// <summary>
    /// Determines whether the message should go to dead letter queue.
    /// A message is dead-lettered when it has reached the retry limit and has not been processed.
    /// </summary>
    public static bool IsDeadLettered(this IOutboxMessage message, int maxRetries) =>
        message.RetryCount >= maxRetries && !message.IsProcessed();
}

/// <summary>
/// In-memory implementation of IOutboxMessage for contract testing.
/// Contains only data properties; query logic is in OutboxMessageExtensions.
/// </summary>
internal sealed class InMemoryOutboxMessageForContract : IOutboxMessage
{
    public Guid Id { get; set; }
    public string NotificationType { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextRetryAtUtc { get; set; }

    // Interface implementation delegates to extension methods
    bool IOutboxMessage.IsProcessed => OutboxMessageExtensions.IsProcessed(this);
    bool IOutboxMessage.IsDeadLettered(int maxRetries) => OutboxMessageExtensions.IsDeadLettered(this, maxRetries);
}

/// <summary>
/// In-memory implementation of IOutboxStore for contract testing.
/// </summary>
internal sealed class InMemoryOutboxStoreForContract : IOutboxStore
{
    private readonly List<InMemoryOutboxMessageForContract> _messages = [];

    public Task AddAsync(IOutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message is not InMemoryOutboxMessageForContract inMemoryMessage)
        {
            throw new ArgumentException(
                $"Message must be of type {nameof(InMemoryOutboxMessageForContract)}, but was {message.GetType().Name}.",
                nameof(message));
        }

        _messages.Add(inMemoryMessage);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<IOutboxMessage>> GetPendingMessagesAsync(
        int batchSize,
        int maxRetries,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var pending = _messages
            .Where(m => !m.IsProcessed())
            .Where(m => !m.IsDeadLettered(maxRetries))
            .Where(m => !m.NextRetryAtUtc.HasValue || m.NextRetryAtUtc <= now)
            .OrderBy(m => m.CreatedAtUtc)
            .Take(batchSize)
            .Cast<IOutboxMessage>()
            .ToList();

        return Task.FromResult<IEnumerable<IOutboxMessage>>(pending);
    }

    public Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = _messages.FirstOrDefault(m => m.Id == messageId)
            ?? throw new KeyNotFoundException($"Outbox message with ID '{messageId}' was not found.");

        message.ProcessedAtUtc = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    public Task MarkAsFailedAsync(
        Guid messageId,
        string errorMessage,
        DateTime? nextRetryAt,
        CancellationToken cancellationToken = default)
    {
        var message = _messages.FirstOrDefault(m => m.Id == messageId)
            ?? throw new KeyNotFoundException($"Outbox message with ID '{messageId}' was not found.");

        message.ErrorMessage = errorMessage;
        message.RetryCount++;
        message.NextRetryAtUtc = nextRetryAt;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void Clear() => _messages.Clear();
}

/// <summary>
/// Concrete implementation of contract tests for in-memory outbox store.
/// </summary>
public sealed class InMemoryOutboxStoreContractTests : IOutboxStoreContractTests
{
    private InMemoryOutboxStoreForContract? _currentStore;

    protected override IOutboxStore CreateStore()
    {
        _currentStore = new InMemoryOutboxStoreForContract();
        return _currentStore;
    }

    protected override IOutboxMessage CreateMessage(Guid id, string notificationType)
    {
        return new InMemoryOutboxMessageForContract
        {
            Id = id,
            NotificationType = notificationType,
            Content = $"{{\"type\":\"{notificationType}\"}}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };
    }

    protected override Task CleanupAsync()
    {
        _currentStore?.Clear();
        return Task.CompletedTask;
    }
}
