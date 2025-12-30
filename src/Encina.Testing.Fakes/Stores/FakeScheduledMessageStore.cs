using System.Collections.Concurrent;
using Encina.Messaging.Scheduling;
using Encina.Testing.Fakes.Models;

namespace Encina.Testing.Fakes.Stores;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IScheduledMessageStore"/> for testing.
/// </summary>
/// <remarks>
/// <para>
/// Provides full implementation of the scheduled message store interface using an in-memory
/// concurrent dictionary. All operations are synchronous but return completed tasks
/// for interface compatibility.
/// </para>
/// </remarks>
public sealed class FakeScheduledMessageStore : IScheduledMessageStore
{
    private readonly ConcurrentDictionary<Guid, FakeScheduledMessage> _messages = new();
    private readonly ConcurrentBag<IScheduledMessage> _addedMessages = new();
    private readonly ConcurrentBag<Guid> _processedMessageIds = new();
    private readonly ConcurrentBag<Guid> _failedMessageIds = new();
    private readonly ConcurrentBag<Guid> _cancelledMessageIds = new();
    private readonly ConcurrentBag<Guid> _rescheduledMessageIds = new();
    private readonly object _lock = new();

    /// <summary>
    /// Gets all messages currently in the store.
    /// </summary>
    public IReadOnlyCollection<FakeScheduledMessage> Messages
    {
        get
        {
            lock (_lock)
            {
                return _messages.Values.ToList().AsReadOnly();
            }
        }
    }

    /// <summary>
    /// Gets all messages that have been added (for verification).
    /// </summary>
    public IReadOnlyList<IScheduledMessage> AddedMessages => _addedMessages.ToList().AsReadOnly();

    /// <summary>
    /// Gets the IDs of messages that have been marked as processed.
    /// </summary>
    public IReadOnlyList<Guid> ProcessedMessageIds => _processedMessageIds.ToList().AsReadOnly();

    /// <summary>
    /// Gets the IDs of messages that have been marked as failed.
    /// </summary>
    public IReadOnlyList<Guid> FailedMessageIds => _failedMessageIds.ToList().AsReadOnly();

    /// <summary>
    /// Gets the IDs of messages that have been cancelled.
    /// </summary>
    public IReadOnlyList<Guid> CancelledMessageIds => _cancelledMessageIds.ToList().AsReadOnly();

    /// <summary>
    /// Gets the IDs of messages that have been rescheduled.
    /// </summary>
    public IReadOnlyList<Guid> RescheduledMessageIds => _rescheduledMessageIds.ToList().AsReadOnly();

    /// <summary>
    /// Gets the number of times <see cref="SaveChangesAsync"/> was called.
    /// </summary>
    public int SaveChangesCallCount { get; private set; }

    /// <inheritdoc />
    public Task AddAsync(IScheduledMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var fakeMessage = message as FakeScheduledMessage ?? new FakeScheduledMessage
        {
            Id = message.Id,
            RequestType = message.RequestType,
            Content = message.Content,
            ScheduledAtUtc = message.ScheduledAtUtc,
            CreatedAtUtc = message.CreatedAtUtc,
            ProcessedAtUtc = message.ProcessedAtUtc,
            ErrorMessage = message.ErrorMessage,
            RetryCount = message.RetryCount,
            NextRetryAtUtc = message.NextRetryAtUtc,
            IsRecurring = message.IsRecurring,
            CronExpression = message.CronExpression,
            LastExecutedAtUtc = message.LastExecutedAtUtc
        };

        lock (_lock)
        {
            _messages[fakeMessage.Id] = fakeMessage;
            _addedMessages.Add(fakeMessage.Clone());
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IEnumerable<IScheduledMessage>> GetDueMessagesAsync(
        int batchSize,
        int maxRetries,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var dueMessages = _messages.Values
            .Where(m => !m.IsProcessed &&
                        !m.IsCancelled &&
                        !m.IsDeadLettered(maxRetries) &&
                        m.ScheduledAtUtc <= now &&
                        (m.NextRetryAtUtc == null || m.NextRetryAtUtc <= now))
            .OrderBy(m => m.ScheduledAtUtc)
            .Take(batchSize)
            .Cast<IScheduledMessage>()
            .ToList();

        return Task.FromResult<IEnumerable<IScheduledMessage>>(dueMessages);
    }

    /// <inheritdoc />
    public Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_messages.TryGetValue(messageId, out var message))
            {
                message.ProcessedAtUtc = DateTime.UtcNow;
                message.LastExecutedAtUtc = DateTime.UtcNow;
                message.ErrorMessage = null;
                _processedMessageIds.Add(messageId);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task MarkAsFailedAsync(
        Guid messageId,
        string errorMessage,
        DateTime? nextRetryAt,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_messages.TryGetValue(messageId, out var message))
            {
                message.ErrorMessage = errorMessage;
                message.RetryCount++;
                message.NextRetryAtUtc = nextRetryAt;
                _failedMessageIds.Add(messageId);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RescheduleRecurringMessageAsync(
        Guid messageId,
        DateTime nextScheduledAt,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_messages.TryGetValue(messageId, out var message))
            {
                message.ScheduledAtUtc = nextScheduledAt;
                message.ProcessedAtUtc = null;
                message.ErrorMessage = null;
                message.RetryCount = 0;
                message.NextRetryAtUtc = null;
                _rescheduledMessageIds.Add(messageId);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CancelAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_messages.TryGetValue(messageId, out var message))
            {
                message.IsCancelled = true;
                _cancelledMessageIds.Add(messageId);
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
    public FakeScheduledMessage? GetMessage(Guid messageId) =>
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
            _cancelledMessageIds.Clear();
            _rescheduledMessageIds.Clear();
            SaveChangesCallCount = 0;
        }
    }

    /// <summary>
    /// Verifies that a message was scheduled with the specified request type.
    /// </summary>
    /// <param name="requestType">The request type to look for.</param>
    /// <returns>True if a message with the specified type was scheduled.</returns>
    public bool WasMessageScheduled(string requestType)
    {
        lock (_lock)
        {
            return _addedMessages.Any(m => m.RequestType == requestType);
        }
    }

    /// <summary>
    /// Verifies that a message was scheduled with the specified request type.
    /// </summary>
    /// <typeparam name="TRequest">The request type to look for.</typeparam>
    /// <returns>True if a message with the specified type was scheduled.</returns>
    public bool WasMessageScheduled<TRequest>()
    {
        var typeName = typeof(TRequest).FullName ?? typeof(TRequest).Name;
        lock (_lock)
        {
            return _addedMessages.Any(m => m.RequestType == typeName);
        }
    }
}
