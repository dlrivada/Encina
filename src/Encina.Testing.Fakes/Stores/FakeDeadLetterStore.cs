using System.Collections.Concurrent;
using Encina.Messaging.DeadLetter;
using Encina.Testing.Fakes.Models;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Testing.Fakes.Stores;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IDeadLetterStore"/> for testing.
/// </summary>
/// <remarks>
/// <para>
/// Provides full implementation of the dead letter store interface using an in-memory
/// concurrent dictionary. All operations are synchronous but return completed tasks
/// for interface compatibility. It follows the same contract as the persistent stores: oldest-first
/// order (<c>DeadLetteredAtUtc</c>, then <c>Id</c>), a unique <c>(SourcePattern, SourceMessageId)</c>,
/// a conditional replay mark, an atomic replay claim, set-based deletes, and expiry
/// (<c>ExpiresAtUtc &lt;= now</c>) evaluated against the injected <see cref="TimeProvider"/>.
/// </para>
/// <para>
/// <b>Registration order.</b> <c>AddFakeDeadLetterStore</c> uses <c>TryAdd</c>, so the first registration
/// wins: a test that registered a provider store first calls <c>ReplaceWithFakes()</c> to swap in the fake.
/// </para>
/// </remarks>
public sealed class FakeDeadLetterStore : IDeadLetterStore
{
    private readonly ConcurrentDictionary<Guid, FakeDeadLetterMessage> _messages = new();
    private readonly ConcurrentBag<IDeadLetterMessage> _addedMessages = new();
    private readonly ConcurrentBag<Guid> _replayedMessageIds = new();
    private readonly ConcurrentBag<Guid> _deletedMessageIds = new();
    private readonly object _lock = new();
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeDeadLetterStore"/> class.
    /// </summary>
    /// <param name="timeProvider">Optional time provider for controlling time in tests. Defaults to <see cref="TimeProvider.System"/>.</param>
    public FakeDeadLetterStore(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;

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
    /// <remarks>
    /// A message whose timestamps are still at their default is stamped from the store's
    /// <see cref="TimeProvider"/>. An empty <c>SourceMessageId</c> throws <see cref="ArgumentException"/>, like the
    /// persistent stores: it is the idempotency key, so a hand-built message must name it.
    /// </remarks>
    public Task<Either<EncinaError, bool>> AddAsync(IDeadLetterMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrEmpty(message.SourceMessageId);

        var fakeMessage = message as FakeDeadLetterMessage ?? CopyOf(message);
        Stamp(fakeMessage);

        lock (_lock)
        {
            if (_messages.Values.Any(m => IsSameSource(m, fakeMessage)))
            {
                return Task.FromResult<Either<EncinaError, bool>>(false);
            }

            _messages[fakeMessage.Id] = fakeMessage;
            _addedMessages.Add(fakeMessage.Clone());
        }

        return Task.FromResult<Either<EncinaError, bool>>(true);
    }

    private static bool IsSameSource(FakeDeadLetterMessage existing, FakeDeadLetterMessage candidate)
        => existing.SourcePattern == candidate.SourcePattern && existing.SourceMessageId == candidate.SourceMessageId;

    private void Stamp(FakeDeadLetterMessage message)
    {
        if (message.DeadLetteredAtUtc == default)
        {
            message.DeadLetteredAtUtc = UtcNow();
        }

        if (message.FirstFailedAtUtc == default)
        {
            message.FirstFailedAtUtc = message.DeadLetteredAtUtc;
        }
    }

    private static FakeDeadLetterMessage CopyOf(IDeadLetterMessage message) => new()
    {
        Id = message.Id,
        RequestType = message.RequestType,
        RequestContent = message.RequestContent,
        ErrorCode = message.ErrorCode,
        ExceptionType = message.ExceptionType,
        ExceptionStackTrace = message.ExceptionStackTrace,
        CorrelationId = message.CorrelationId,
        SourcePattern = message.SourcePattern,
        SourceMessageId = message.SourceMessageId,
        TenantId = message.TenantId,
        TotalRetryAttempts = message.TotalRetryAttempts,
        FirstFailedAtUtc = message.FirstFailedAtUtc,
        DeadLetteredAtUtc = message.DeadLetteredAtUtc,
        ExpiresAtUtc = message.ExpiresAtUtc,
        ReplayClaimedAtUtc = message.ReplayClaimedAtUtc,
        ReplayedAtUtc = message.ReplayedAtUtc,
        ReplayResult = message.ReplayResult
    };

    /// <inheritdoc />
    public Task<Either<EncinaError, Option<IDeadLetterMessage>>> GetAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(messageId, Guid.Empty);

        _messages.TryGetValue(messageId, out var message);
        var option = message is not null
            ? Option<IDeadLetterMessage>.Some(message)
            : Option<IDeadLetterMessage>.None;
        return Task.FromResult<Either<EncinaError, Option<IDeadLetterMessage>>>(option);
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, IEnumerable<IDeadLetterMessage>>> GetMessagesAsync(
        DeadLetterFilter? filter = null,
        int skip = 0,
        int take = 100,
        bool newestFirst = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        ArgumentOutOfRangeException.ThrowIfLessThan(take, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(take, DeadLetterStoreLimits.MaxPageSize);

        var ordered = newestFirst
            ? ApplyFilter(_messages.Values, filter).OrderByDescending(m => m.DeadLetteredAtUtc).ThenByDescending(m => m.Id)
            : ApplyFilter(_messages.Values, filter).OrderBy(m => m.DeadLetteredAtUtc).ThenBy(m => m.Id);

        var messages = ordered
            .Skip(skip)
            .Take(take)
            .Cast<IDeadLetterMessage>()
            .ToList();

        return Task.FromResult<Either<EncinaError, IEnumerable<IDeadLetterMessage>>>(messages);
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, int>> GetCountAsync(DeadLetterFilter? filter = null, CancellationToken cancellationToken = default)
    {
        var query = ApplyFilter(_messages.Values, filter);

        return Task.FromResult<Either<EncinaError, int>>(query.Count());
    }

    private static IEnumerable<FakeDeadLetterMessage> ApplyFilter(
        IEnumerable<FakeDeadLetterMessage> source,
        DeadLetterFilter? filter)
        => filter is null
            ? source
            : source.Where(m => MatchesIdentity(m, filter) && MatchesState(m, filter));

    private static bool MatchesIdentity(FakeDeadLetterMessage m, DeadLetterFilter filter)
        => Equal(filter.SourcePattern, m.SourcePattern)
           && Equal(filter.RequestType, m.RequestType)
           && Equal(filter.ErrorCode, m.ErrorCode)
           && Equal(filter.CorrelationId, m.CorrelationId)
           && Equal(filter.TenantId, m.TenantId)
           && Equal(filter.SourceMessageId, m.SourceMessageId);

    private static bool MatchesState(FakeDeadLetterMessage m, DeadLetterFilter filter)
        => ReplayStateMatches(filter.ExcludeReplayed, m)
           && InWindow(m.DeadLetteredAtUtc, filter.DeadLetteredAfterUtc, filter.DeadLetteredBeforeUtc)
           && ExpiryMatches(filter.ExpiresAtOrBeforeUtc, m);

    private static bool InWindow(DateTime deadLetteredAtUtc, DateTime? after, DateTime? before)
        => (after is not { } from || deadLetteredAtUtc >= from)
           && (before is not { } to || deadLetteredAtUtc <= to);

    private static bool ExpiryMatches(DateTime? expiresAtOrBeforeUtc, FakeDeadLetterMessage m)
        => expiresAtOrBeforeUtc is not { } expiresBy || m.IsExpiredAt(expiresBy);

    // A null or empty filter value matches everything.
    private static bool Equal(string? filterValue, string? actual)
        => string.IsNullOrEmpty(filterValue) || filterValue == actual;

    private static bool ReplayStateMatches(bool? excludeReplayed, FakeDeadLetterMessage m)
        => excludeReplayed is not { } exclude || exclude != m.IsReplayed;

    /// <inheritdoc />
    public Task<Either<EncinaError, bool>> TryClaimForReplayAsync(
        Guid messageId,
        DateTime claimExpiredBeforeUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(messageId, Guid.Empty);

        lock (_lock)
        {
            if (!_messages.TryGetValue(messageId, out var message) || !IsClaimable(message, claimExpiredBeforeUtc))
            {
                return Task.FromResult<Either<EncinaError, bool>>(false);
            }

            message.ReplayClaimedAtUtc = UtcNow();
        }

        return Task.FromResult<Either<EncinaError, bool>>(true);
    }

    private static bool IsClaimable(FakeDeadLetterMessage message, DateTime claimExpiredBeforeUtc)
        => !message.IsReplayed
           && (message.ReplayClaimedAtUtc is not { } claimedAt || claimedAt <= claimExpiredBeforeUtc);

    /// <inheritdoc />
    public Task<Either<EncinaError, bool>> MarkAsReplayedAsync(Guid messageId, string replayResult, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(messageId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(replayResult);

        lock (_lock)
        {
            if (!_messages.TryGetValue(messageId, out var message) || message.IsReplayed)
            {
                return Task.FromResult<Either<EncinaError, bool>>(false);
            }

            message.ReplayedAtUtc = UtcNow();
            message.ReplayResult = replayResult;
            _replayedMessageIds.Add(messageId);
        }

        return Task.FromResult<Either<EncinaError, bool>>(true);
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, bool>> DeleteAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(messageId, Guid.Empty);

        var removed = Remove(messageId);

        return Task.FromResult<Either<EncinaError, bool>>(removed);
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, int>> DeleteManyAsync(DeadLetterFilter filter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var ids = ApplyFilter(_messages.Values, filter).Select(m => m.Id).ToList();

        return Task.FromResult<Either<EncinaError, int>>(ids.Count(Remove));
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, int>> DeleteExpiredAsync(CancellationToken cancellationToken = default)
    {
        var now = UtcNow();
        var expiredIds = _messages.Values
            .Where(m => m.IsExpiredAt(now))
            .Select(m => m.Id)
            .ToList();

        return Task.FromResult<Either<EncinaError, int>>(expiredIds.Count(Remove));
    }

    private bool Remove(Guid messageId)
    {
        lock (_lock)
        {
            var removed = _messages.TryRemove(messageId, out _);
            if (removed)
            {
                _deletedMessageIds.Add(messageId);
            }

            return removed;
        }
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, Unit>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCallCount++;
        return Task.FromResult<Either<EncinaError, Unit>>(Right(unit));
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
