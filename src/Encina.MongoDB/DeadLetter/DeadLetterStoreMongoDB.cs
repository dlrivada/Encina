using Encina.Messaging.DeadLetter;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using static LanguageExt.Prelude;

namespace Encina.MongoDB.DeadLetter;

/// <summary>
/// MongoDB implementation of <see cref="IDeadLetterStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Every driver call takes the caller's <see cref="CancellationToken"/>. "Now" comes from the injected
/// <see cref="TimeProvider"/>. The unique <c>(sourcePattern, sourceMessageId)</c> index turns a second capture
/// of the same source message (a duplicate key error) into <c>Right(false)</c>. Strings compare by exact
/// bytes (no collation), like the binary collations of the SQL providers. MongoDB stores dates with
/// millisecond precision.
/// </para>
/// <para>
/// There is no TTL index: expired messages are deleted by <see cref="DeleteExpiredAsync"/>, which the
/// dead letter cleanup processor drives on every provider.
/// </para>
/// </remarks>
public sealed class DeadLetterStoreMongoDB : IDeadLetterStore
{
    private static readonly FilterDefinitionBuilder<DeadLetterMessage> Filters = Builders<DeadLetterMessage>.Filter;

    private readonly IMongoCollection<DeadLetterMessage> _collection;
    private readonly ILogger<DeadLetterStoreMongoDB> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeadLetterStoreMongoDB"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client.</param>
    /// <param name="options">The MongoDB options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time provider. Defaults to <see cref="TimeProvider.System"/> if not specified.</param>
    public DeadLetterStoreMongoDB(
        IMongoClient mongoClient,
        IOptions<EncinaMongoDbOptions> options,
        ILogger<DeadLetterStoreMongoDB> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var config = options.Value;
        var database = mongoClient.GetDatabase(config.DatabaseName);
        _collection = database.GetCollection<DeadLetterMessage>(config.Collections.DeadLetterMessages);
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;

    /// <inheritdoc />
    public async Task<Either<EncinaError, bool>> AddAsync(IDeadLetterMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrEmpty(message.SourceMessageId);

        return await EitherHelpers.TryAsync(async () =>
        {
            var document = message as DeadLetterMessage ?? CopyOf(message);

            try
            {
                await _collection.InsertOneAsync(document, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return false;
            }

            Log.AddedDeadLetterMessage(_logger, document.Id, document.SourcePattern);
            return true;
        }, DeadLetterErrorCodes.StoreFailed).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Option<IDeadLetterMessage>>> GetAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        ValidateMessageId(messageId);

        return await EitherHelpers.TryAsync(async () =>
        {
            var message = await _collection
                .Find(Filters.Eq(m => m.Id, messageId))
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return message is null ? Option<IDeadLetterMessage>.None : Some<IDeadLetterMessage>(message);
        }, DeadLetterErrorCodes.GetFailed).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IEnumerable<IDeadLetterMessage>>> GetMessagesAsync(
        DeadLetterFilter? filter = null,
        int skip = 0,
        int take = 100,
        bool newestFirst = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        ArgumentOutOfRangeException.ThrowIfLessThan(take, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(take, DeadLetterStoreLimits.MaxPageSize);

        return await EitherHelpers.TryAsync<IEnumerable<IDeadLetterMessage>>(async () =>
        {
            var sort = Builders<DeadLetterMessage>.Sort;
            var order = newestFirst
                ? sort.Descending(m => m.DeadLetteredAtUtc).Descending(m => m.Id)
                : sort.Ascending(m => m.DeadLetteredAtUtc).Ascending(m => m.Id);

            var messages = await _collection
                .Find(BuildFilter(filter))
                .Sort(order)
                .Skip(skip)
                .Limit(take)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return messages.Cast<IDeadLetterMessage>().ToList();
        }, DeadLetterErrorCodes.QueryFailed).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, int>> GetCountAsync(DeadLetterFilter? filter = null, CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var count = await _collection
                .CountDocumentsAsync(BuildFilter(filter), cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return (int)count;
        }, DeadLetterErrorCodes.QueryFailed).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, bool>> TryClaimForReplayAsync(
        Guid messageId,
        DateTime claimExpiredBeforeUtc,
        CancellationToken cancellationToken = default)
    {
        ValidateMessageId(messageId);

        return await EitherHelpers.TryAsync(async () =>
        {
            var expiredBefore = AsUtc(claimExpiredBeforeUtc);
            var filter = Filters.And(
                Filters.Eq(m => m.Id, messageId),
                Filters.Eq(m => m.ReplayedAtUtc, null),
                Filters.Or(
                    Filters.Eq(m => m.ReplayClaimedAtUtc, null),
                    Filters.Lte(m => m.ReplayClaimedAtUtc, expiredBefore)));
            var update = Builders<DeadLetterMessage>.Update.Set(m => m.ReplayClaimedAtUtc, UtcNow());

            var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken).ConfigureAwait(false);
            return result.MatchedCount == 1;
        }, DeadLetterErrorCodes.ClaimFailed).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, bool>> MarkAsReplayedAsync(Guid messageId, string replayResult, CancellationToken cancellationToken = default)
    {
        ValidateMessageId(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(replayResult);

        return await EitherHelpers.TryAsync(async () =>
        {
            var filter = Filters.And(Filters.Eq(m => m.Id, messageId), Filters.Eq(m => m.ReplayedAtUtc, null));
            var update = Builders<DeadLetterMessage>.Update
                .Set(m => m.ReplayedAtUtc, UtcNow())
                .Set(m => m.ReplayResult, replayResult);

            var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken).ConfigureAwait(false);
            return result.MatchedCount == 1;
        }, DeadLetterErrorCodes.MarkReplayedFailed).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, bool>> DeleteAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        ValidateMessageId(messageId);

        return await EitherHelpers.TryAsync(async () =>
        {
            var result = await _collection
                .DeleteOneAsync(Filters.Eq(m => m.Id, messageId), cancellationToken)
                .ConfigureAwait(false);

            return result.DeletedCount > 0;
        }, DeadLetterErrorCodes.DeleteFailed).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, int>> DeleteManyAsync(DeadLetterFilter filter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        return await EitherHelpers.TryAsync(async () =>
        {
            var result = await _collection.DeleteManyAsync(BuildFilter(filter), cancellationToken).ConfigureAwait(false);
            return (int)result.DeletedCount;
        }, DeadLetterErrorCodes.DeleteFailed).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, int>> DeleteExpiredAsync(CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var expired = DeadLetterFilter.All;
            expired.ExpiresAtOrBeforeUtc = UtcNow();

            var result = await _collection.DeleteManyAsync(BuildFilter(expired), cancellationToken).ConfigureAwait(false);
            return (int)result.DeletedCount;
        }, DeadLetterErrorCodes.CleanupFailed).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, Unit>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // MongoDB writes immediately, no need for SaveChanges
        return Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
    }

    private static void ValidateMessageId(Guid messageId)
        => ArgumentOutOfRangeException.ThrowIfEqual(messageId, Guid.Empty);

    private static FilterDefinition<DeadLetterMessage> BuildFilter(DeadLetterFilter? filter)
    {
        if (filter is null)
        {
            return Filters.Empty;
        }

        var conditions = new List<FilterDefinition<DeadLetterMessage>>();
        AddIdentity(conditions, filter);
        AddState(conditions, filter);

        return conditions.Count == 0 ? Filters.Empty : Filters.And(conditions);
    }

    // A null or empty filter value matches everything (the contract shared with the fake store).
    private static void AddIdentity(List<FilterDefinition<DeadLetterMessage>> conditions, DeadLetterFilter filter)
    {
        AddEquals(conditions, m => m.SourcePattern, filter.SourcePattern);
        AddEquals(conditions, m => m.RequestType, filter.RequestType);
        AddEquals(conditions, m => m.ErrorCode, filter.ErrorCode);
        AddEquals(conditions, m => m.CorrelationId, filter.CorrelationId);
        AddEquals(conditions, m => m.TenantId, filter.TenantId);
        AddEquals(conditions, m => m.SourceMessageId, filter.SourceMessageId);
    }

    private static void AddState(List<FilterDefinition<DeadLetterMessage>> conditions, DeadLetterFilter filter)
    {
        if (filter.ExcludeReplayed is { } exclude)
        {
            conditions.Add(exclude ? Filters.Eq(m => m.ReplayedAtUtc, null) : Filters.Ne(m => m.ReplayedAtUtc, null));
        }

        AddInstant(conditions, filter.DeadLetteredAfterUtc, instant => Filters.Gte(m => m.DeadLetteredAtUtc, instant));
        AddInstant(conditions, filter.DeadLetteredBeforeUtc, instant => Filters.Lte(m => m.DeadLetteredAtUtc, instant));
        AddInstant(
            conditions,
            filter.ExpiresAtOrBeforeUtc,
            instant => Filters.And(Filters.Ne(m => m.ExpiresAtUtc, null), Filters.Lte(m => m.ExpiresAtUtc, instant)));
    }

    private static void AddEquals(
        List<FilterDefinition<DeadLetterMessage>> conditions,
        System.Linq.Expressions.Expression<Func<DeadLetterMessage, string?>> field,
        string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            conditions.Add(Filters.Eq(field, value));
        }
    }

    private static void AddInstant(
        List<FilterDefinition<DeadLetterMessage>> conditions,
        DateTime? value,
        Func<DateTime, FilterDefinition<DeadLetterMessage>> condition)
    {
        if (value is { } instant)
        {
            conditions.Add(condition(AsUtc(instant)));
        }
    }

    // The driver converts a DateTime of Kind.Unspecified as local time; every instant here is UTC.
    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static DeadLetterMessage CopyOf(IDeadLetterMessage message) => new()
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
}
