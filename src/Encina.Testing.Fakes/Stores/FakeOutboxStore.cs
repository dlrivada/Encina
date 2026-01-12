using System.Collections.Concurrent;
using Encina.Messaging.Outbox;
using Encina.Testing.Fakes.Models;

namespace Encina.Testing.Fakes.Stores;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IOutboxStore"/> for testing.
/// </summary>
/// <remarks>
/// <para>
/// Provides full implementation of the outbox store interface using an in-memory
/// concurrent dictionary. All operations are synchronous but return completed tasks
/// for interface compatibility.
/// </para>
/// <para>
/// This store tracks all operations for verification in tests:
/// <list type="bullet">
/// <item><description><see cref="GetAddedMessages"/>: All messages added to the store</description></item>
/// <item><description><see cref="GetProcessedMessageIds"/>: IDs of messages marked as processed</description></item>
/// <item><description><see cref="GetFailedMessageIds"/>: IDs of messages marked as failed</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class FakeOutboxStore : IOutboxStore
{
    private readonly ConcurrentDictionary<Guid, FakeOutboxMessage> _messages = new();
    private readonly ConcurrentBag<IOutboxMessage> _addedMessages = new();
    private readonly ConcurrentBag<Guid> _processedMessageIds = new();
    private readonly ConcurrentBag<Guid> _failedMessageIds = new();
    private readonly object _lock = new();

    /// <summary>
    /// Gets a snapshot of all messages currently in the store.
    /// </summary>
    /// <returns>A point-in-time copy of all messages.</returns>
    public IReadOnlyCollection<FakeOutboxMessage> GetMessages() => _messages.Values.ToList().AsReadOnly();

    /// <summary>
    /// Gets a snapshot of all messages that have been added (for verification).
    /// </summary>
    /// <returns>A point-in-time copy of added messages.</returns>
    public IReadOnlyList<IOutboxMessage> GetAddedMessages() => _addedMessages.ToList().AsReadOnly();

    /// <summary>
    /// Gets a snapshot of the IDs of messages that have been marked as processed.
    /// </summary>
    /// <returns>A point-in-time copy of processed message IDs.</returns>
    public IReadOnlyList<Guid> GetProcessedMessageIds() => _processedMessageIds.ToList().AsReadOnly();

    /// <summary>
    /// Gets a snapshot of the IDs of messages that have been marked as failed.
    /// </summary>
    /// <returns>A point-in-time copy of failed message IDs.</returns>
    public IReadOnlyList<Guid> GetFailedMessageIds() => _failedMessageIds.ToList().AsReadOnly();

    /// <summary>
    /// Gets the number of times <see cref="SaveChangesAsync"/> was called.
    /// </summary>
    public int SaveChangesCallCount { get; private set; }

    /// <inheritdoc />
    public Task AddAsync(IOutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var fakeMessage = message as FakeOutboxMessage ?? new FakeOutboxMessage
        {
            Id = message.Id,
            NotificationType = message.NotificationType,
            Content = message.Content,
            CreatedAtUtc = message.CreatedAtUtc,
            ProcessedAtUtc = message.ProcessedAtUtc,
            ErrorMessage = message.ErrorMessage,
            RetryCount = message.RetryCount,
            NextRetryAtUtc = message.NextRetryAtUtc
        };

        _messages[fakeMessage.Id] = fakeMessage;

        lock (_lock)
        {
            _addedMessages.Add(fakeMessage.Clone());
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IEnumerable<IOutboxMessage>> GetPendingMessagesAsync(
        int batchSize,
        int maxRetries,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var pendingMessages = _messages.Values
            .Where(m => !m.IsProcessed &&
                        !m.IsDeadLettered(maxRetries) &&
                        (m.NextRetryAtUtc == null || m.NextRetryAtUtc <= now))
            .OrderBy(m => m.CreatedAtUtc)
            .Take(batchSize)
            .Cast<IOutboxMessage>()
            .ToList();

        return Task.FromResult<IEnumerable<IOutboxMessage>>(pendingMessages);
    }

    /// <inheritdoc />
    public Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        if (_messages.TryGetValue(messageId, out var message))
        {
            message.ProcessedAtUtc = DateTime.UtcNow;
            message.ErrorMessage = null;

            lock (_lock)
            {
                _processedMessageIds.Add(messageId);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task MarkAsFailedAsync(
        Guid messageId,
        string errorMessage,
        DateTime? nextRetryAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (_messages.TryGetValue(messageId, out var message))
        {
            message.ErrorMessage = errorMessage;
            message.RetryCount++;
            message.NextRetryAtUtc = nextRetryAtUtc;

            lock (_lock)
            {
                _failedMessageIds.Add(messageId);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCallCount++;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets a message by its ID.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <returns>The message if found, otherwise null.</returns>
    public FakeOutboxMessage? GetMessage(Guid messageId) =>
        _messages.TryGetValue(messageId, out var message) ? message : null;

    /// <summary>
    /// Clears all messages and resets verification state.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _messages.Clear();
            _addedMessages.Clear();
            _processedMessageIds.Clear();
            _failedMessageIds.Clear();
            SaveChangesCallCount = 0;
        }
    }

    /// <summary>
    /// Clears only the tracking collections while leaving persisted messages intact.
    /// </summary>
    /// <remarks>
    /// Use this method to reset tracking state (AddedMessages, ProcessedMessageIds, FailedMessageIds)
    /// without removing the actual messages from the store. This is useful for setting up
    /// "historical" messages in tests where the tracking should start fresh.
    /// </remarks>
    public void ClearTracking()
    {
        lock (_lock)
        {
            _addedMessages.Clear();
            _processedMessageIds.Clear();
            _failedMessageIds.Clear();
            SaveChangesCallCount = 0;
        }
    }

    /// <summary>
    /// Verifies that a message was added with the specified notification type.
    /// </summary>
    /// <param name="notificationType">The notification type to look for.</param>
    /// <returns>True if a message with the specified type was added.</returns>
    public bool WasMessageAdded(string notificationType)
    {
        lock (_lock)
        {
            return _addedMessages.Any(m => m.NotificationType == notificationType);
        }
    }

    /// <summary>
    /// Verifies that a message was added with the specified notification type.
    /// </summary>
    /// <typeparam name="TNotification">The notification type to look for.</typeparam>
    /// <returns>True if a message with the specified type was added.</returns>
    public bool WasMessageAdded<TNotification>()
    {
        lock (_lock)
        {
            return _addedMessages.Any(m => m.NotificationType == typeof(TNotification).FullName);
        }
    }
}
