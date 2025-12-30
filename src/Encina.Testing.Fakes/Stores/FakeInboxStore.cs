using System.Collections.Concurrent;
using Encina.Messaging.Inbox;
using Encina.Testing.Fakes.Models;

namespace Encina.Testing.Fakes.Stores;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IInboxStore"/> for testing.
/// </summary>
/// <remarks>
/// <para>
/// Provides full implementation of the inbox store interface using an in-memory
/// concurrent dictionary. All operations are synchronous but return completed tasks
/// for interface compatibility.
/// </para>
/// </remarks>
public sealed class FakeInboxStore : IInboxStore
{
    private readonly ConcurrentDictionary<string, FakeInboxMessage> _messages = new();
    private readonly ConcurrentBag<IInboxMessage> _addedMessages = new();
    private readonly ConcurrentBag<string> _processedMessageIds = new();
    private readonly ConcurrentBag<string> _failedMessageIds = new();
    private readonly ConcurrentBag<string> _removedMessageIds = new();
    private readonly object _lock = new();

    /// <summary>
    /// Gets all messages currently in the store.
    /// </summary>
    public IReadOnlyCollection<FakeInboxMessage> Messages => _messages.Values.ToList().AsReadOnly();

    /// <summary>
    /// Gets all messages that have been added (for verification).
    /// </summary>
    public IReadOnlyList<IInboxMessage> AddedMessages => _addedMessages.ToList().AsReadOnly();

    /// <summary>
    /// Gets the IDs of messages that have been marked as processed.
    /// </summary>
    public IReadOnlyList<string> ProcessedMessageIds => _processedMessageIds.ToList().AsReadOnly();

    /// <summary>
    /// Gets the IDs of messages that have been marked as failed.
    /// </summary>
    public IReadOnlyList<string> FailedMessageIds => _failedMessageIds.ToList().AsReadOnly();

    /// <summary>
    /// Gets the IDs of messages that have been removed.
    /// </summary>
    public IReadOnlyList<string> RemovedMessageIds => _removedMessageIds.ToList().AsReadOnly();

    /// <summary>
    /// Gets the number of times <see cref="SaveChangesAsync"/> was called.
    /// </summary>
    public int SaveChangesCallCount { get; private set; }

    /// <inheritdoc />
    public Task<IInboxMessage?> GetMessageAsync(string messageId, CancellationToken cancellationToken = default)
    {
        _messages.TryGetValue(messageId, out var message);
        return Task.FromResult<IInboxMessage?>(message);
    }

    /// <inheritdoc />
    public Task AddAsync(IInboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var fakeMessage = message as FakeInboxMessage ?? new FakeInboxMessage
        {
            MessageId = message.MessageId,
            RequestType = message.RequestType,
            Response = message.Response,
            ErrorMessage = message.ErrorMessage,
            ReceivedAtUtc = message.ReceivedAtUtc,
            ProcessedAtUtc = message.ProcessedAtUtc,
            ExpiresAtUtc = message.ExpiresAtUtc,
            RetryCount = message.RetryCount,
            NextRetryAtUtc = message.NextRetryAtUtc
        };

        _messages[fakeMessage.MessageId] = fakeMessage;

        lock (_lock)
        {
            _addedMessages.Add(fakeMessage.Clone());
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task MarkAsProcessedAsync(string messageId, string response, CancellationToken cancellationToken = default)
    {
        if (_messages.TryGetValue(messageId, out var message))
        {
            message.ProcessedAtUtc = DateTime.UtcNow;
            message.Response = response;
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
        string messageId,
        string errorMessage,
        DateTime? nextRetryAt,
        CancellationToken cancellationToken = default)
    {
        if (_messages.TryGetValue(messageId, out var message))
        {
            message.ErrorMessage = errorMessage;
            message.NextRetryAtUtc = nextRetryAt;

            lock (_lock)
            {
                _failedMessageIds.Add(messageId);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task IncrementRetryCountAsync(string messageId, CancellationToken cancellationToken = default)
    {
        if (_messages.TryGetValue(messageId, out var message))
        {
            message.RetryCount++;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IEnumerable<IInboxMessage>> GetExpiredMessagesAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var expiredMessages = _messages.Values
            .Where(m => m.IsExpired())
            .Take(batchSize)
            .Cast<IInboxMessage>()
            .ToList();

        return Task.FromResult<IEnumerable<IInboxMessage>>(expiredMessages);
    }

    /// <inheritdoc />
    public Task RemoveExpiredMessagesAsync(
        IEnumerable<string> messageIds,
        CancellationToken cancellationToken = default)
    {
        foreach (var messageId in messageIds)
        {
            if (_messages.TryRemove(messageId, out _))
            {
                lock (_lock)
                {
                    _removedMessageIds.Add(messageId);
                }
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
    public FakeInboxMessage? GetMessage(string messageId) =>
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
            _removedMessageIds.Clear();
            SaveChangesCallCount = 0;
        }
    }

    /// <summary>
    /// Checks if a message with the given ID has already been processed.
    /// </summary>
    /// <param name="messageId">The message ID to check.</param>
    /// <returns>True if the message exists and is processed.</returns>
    public bool IsMessageProcessed(string messageId) =>
        _messages.TryGetValue(messageId, out var message) && message.IsProcessed;
}
