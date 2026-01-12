using System.Collections.Concurrent;
using Encina.Messaging.DeadLetter;
using Encina.Testing.Fakes.Models;

namespace Encina.Testing.Fakes.Stores;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IDeadLetterStore"/> for testing.
/// </summary>
/// <remarks>
/// <para>
/// Provides full implementation of the dead letter store interface using an in-memory
/// concurrent dictionary. All operations are synchronous but return completed tasks
/// for interface compatibility.
/// </para>
/// </remarks>
public sealed class FakeDeadLetterStore : IDeadLetterStore
{
    private readonly ConcurrentDictionary<Guid, FakeDeadLetterMessage> _messages = new();
    private readonly ConcurrentBag<IDeadLetterMessage> _addedMessages = new();
    private readonly ConcurrentBag<Guid> _replayedMessageIds = new();
    private readonly ConcurrentBag<Guid> _deletedMessageIds = new();
    private readonly object _lock = new();

    /// <summary>
    /// Gets a snapshot of all messages currently in the store.
    /// </summary>
    /// <remarks>
    /// Returns a point-in-time copy of the messages. Each call creates a new snapshot
    /// for thread-safety. For repeated access in the same scope, cache the result locally.
    /// </remarks>
    /// <returns>A point-in-time copy of all messages.</returns>
    public IReadOnlyCollection<FakeDeadLetterMessage> GetMessages()
    {
        lock (_lock)
        {
            return _messages.Values.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Gets a snapshot of all messages that have been added (for verification).
    /// </summary>
    /// <remarks>
    /// Returns a point-in-time copy. Each call creates a new snapshot for thread-safety.
    /// For repeated access in the same scope, cache the result locally.
    /// </remarks>
    /// <returns>A point-in-time copy of added messages.</returns>
    public IReadOnlyList<IDeadLetterMessage> GetAddedMessages()
    {
        lock (_lock)
        {
            return _addedMessages.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Gets a snapshot of the IDs of messages that have been replayed.
    /// </summary>
    /// <remarks>
    /// Returns a point-in-time copy. Each call creates a new snapshot for thread-safety.
    /// For repeated access in the same scope, cache the result locally.
    /// </remarks>
    /// <returns>A point-in-time copy of replayed message IDs.</returns>
    public IReadOnlyList<Guid> GetReplayedMessageIds()
    {
        lock (_lock)
        {
            return _replayedMessageIds.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Gets a snapshot of the IDs of messages that have been deleted.
    /// </summary>
    /// <remarks>
    /// Returns a point-in-time copy. Each call creates a new snapshot for thread-safety.
    /// For repeated access in the same scope, cache the result locally.
    /// </remarks>
    /// <returns>A point-in-time copy of deleted message IDs.</returns>
    public IReadOnlyList<Guid> GetDeletedMessageIds()
    {
        lock (_lock)
        {
            return _deletedMessageIds.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Gets the number of times <see cref="SaveChangesAsync"/> was called.
    /// </summary>
    public int SaveChangesCallCount { get; private set; }

    /// <inheritdoc />
    public Task AddAsync(IDeadLetterMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var fakeMessage = message as FakeDeadLetterMessage ?? new FakeDeadLetterMessage
        {
            Id = message.Id,
            RequestType = message.RequestType,
            RequestContent = message.RequestContent,
            ErrorMessage = message.ErrorMessage,
            ExceptionType = message.ExceptionType,
            ExceptionMessage = message.ExceptionMessage,
            ExceptionStackTrace = message.ExceptionStackTrace,
            CorrelationId = message.CorrelationId,
            SourcePattern = message.SourcePattern,
            TotalRetryAttempts = message.TotalRetryAttempts,
            FirstFailedAtUtc = message.FirstFailedAtUtc,
            DeadLetteredAtUtc = message.DeadLetteredAtUtc,
            ExpiresAtUtc = message.ExpiresAtUtc,
            ReplayedAtUtc = message.ReplayedAtUtc,
            ReplayResult = message.ReplayResult
        };

        lock (_lock)
        {
            _messages[fakeMessage.Id] = fakeMessage;
            _addedMessages.Add(fakeMessage.Clone());
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IDeadLetterMessage?> GetAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        _messages.TryGetValue(messageId, out var message);
        return Task.FromResult<IDeadLetterMessage?>(message);
    }

    /// <inheritdoc />
    public Task<IEnumerable<IDeadLetterMessage>> GetMessagesAsync(
        DeadLetterFilter? filter = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilter(_messages.Values, filter);

        var messages = query
            .OrderByDescending(m => m.DeadLetteredAtUtc)
            .Skip(skip)
            .Take(take)
            .Cast<IDeadLetterMessage>()
            .ToList();

        return Task.FromResult<IEnumerable<IDeadLetterMessage>>(messages);
    }

    /// <inheritdoc />
    public Task<int> GetCountAsync(DeadLetterFilter? filter = null, CancellationToken cancellationToken = default)
    {
        var query = ApplyFilter(_messages.Values, filter);

        return Task.FromResult(query.Count());
    }

    private static IEnumerable<FakeDeadLetterMessage> ApplyFilter(
        IEnumerable<FakeDeadLetterMessage> source,
        DeadLetterFilter? filter)
    {
        var query = source.AsEnumerable();

        if (filter == null)
        {
            return query;
        }

        if (!string.IsNullOrEmpty(filter.SourcePattern))
        {
            query = query.Where(m => m.SourcePattern == filter.SourcePattern);
        }

        if (!string.IsNullOrEmpty(filter.RequestType))
        {
            query = query.Where(m => m.RequestType == filter.RequestType);
        }

        if (!string.IsNullOrEmpty(filter.CorrelationId))
        {
            query = query.Where(m => m.CorrelationId == filter.CorrelationId);
        }

        if (filter.ExcludeReplayed is true)
        {
            query = query.Where(m => !m.IsReplayed);
        }
        else if (filter.ExcludeReplayed is false)
        {
            query = query.Where(m => m.IsReplayed);
        }

        if (filter.DeadLetteredAfterUtc.HasValue)
        {
            query = query.Where(m => m.DeadLetteredAtUtc >= filter.DeadLetteredAfterUtc.Value);
        }

        if (filter.DeadLetteredBeforeUtc.HasValue)
        {
            query = query.Where(m => m.DeadLetteredAtUtc <= filter.DeadLetteredBeforeUtc.Value);
        }

        return query;
    }

    /// <inheritdoc />
    public Task MarkAsReplayedAsync(Guid messageId, string replayResult, CancellationToken cancellationToken = default)
    {
        if (_messages.TryGetValue(messageId, out var message))
        {
            message.ReplayedAtUtc = DateTime.UtcNow;
            message.ReplayResult = replayResult;

            lock (_lock)
            {
                _replayedMessageIds.Add(messageId);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var removed = _messages.TryRemove(messageId, out _);
        if (removed)
        {
            lock (_lock)
            {
                _deletedMessageIds.Add(messageId);
            }
        }

        return Task.FromResult(removed);
    }

    /// <inheritdoc />
    public Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default)
    {
        var expiredIds = _messages.Values
            .Where(m => m.IsExpired)
            .Select(m => m.Id)
            .ToList();

        foreach (var id in expiredIds)
        {
            if (_messages.TryRemove(id, out _))
            {
                lock (_lock)
                {
                    _deletedMessageIds.Add(id);
                }
            }
        }

        return Task.FromResult(expiredIds.Count);
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
    public FakeDeadLetterMessage? GetMessage(Guid messageId) =>
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
            _replayedMessageIds.Clear();
            _deletedMessageIds.Clear();
            SaveChangesCallCount = 0;
        }
    }

    /// <summary>
    /// Verifies that a message was dead-lettered from the specified source pattern.
    /// </summary>
    /// <param name="sourcePattern">The source pattern to look for (e.g., "Outbox", "Inbox").</param>
    /// <returns>True if a message from the specified source was dead-lettered.</returns>
    public bool WasMessageDeadLettered(string sourcePattern)
    {
        lock (_lock)
        {
            return _addedMessages.Any(m => m.SourcePattern == sourcePattern);
        }
    }

    /// <summary>
    /// Gets all messages from the specified source pattern.
    /// </summary>
    /// <param name="sourcePattern">The source pattern to filter by.</param>
    /// <returns>Collection of messages from the specified source.</returns>
    public IReadOnlyList<FakeDeadLetterMessage> GetMessagesBySource(string sourcePattern) =>
        _messages.Values.Where(m => m.SourcePattern == sourcePattern).ToList().AsReadOnly();
}
