using System.Linq.Expressions;
using Encina.Messaging.DeadLetter;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.DeadLetter;

/// <summary>
/// Entity Framework Core implementation of <see cref="IDeadLetterStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// One implementation serves SQL Server, PostgreSQL and MySQL: no raw SQL, set-based changes through
/// <c>ExecuteUpdateAsync</c> and <c>ExecuteDeleteAsync</c>. <see cref="AddAsync"/> only tracks the new
/// entity; <see cref="SaveChangesAsync"/> writes it. A race between two hosts that capture the same
/// source message is rejected by the unique index at that point and surfaces as <c>Left(dlq.store_failed)</c>
/// from <see cref="SaveChangesAsync"/>.
/// </para>
/// <para>
/// <b>Shared DbContext.</b> The store uses the scoped <see cref="DbContext"/>, so
/// <see cref="SaveChangesAsync"/> also saves any other change tracked in that context. A caller in a
/// failure path (for example a processor that dead-letters after a failed unit of work) must use a new
/// scope so that it does not save, or fail on, the changes of the operation that failed.
/// </para>
/// </remarks>
public sealed class DeadLetterStoreEF : IDeadLetterStore
{
    private readonly DbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeadLetterStoreEF"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dbContext"/> is null.</exception>
    public DeadLetterStoreEF(DbContext dbContext, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;

    private DbSet<DeadLetterMessage> Messages => _dbContext.Set<DeadLetterMessage>();

    /// <inheritdoc />
    public async Task<Either<EncinaError, bool>> AddAsync(IDeadLetterMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message is not DeadLetterMessage entity)
        {
            return EncinaErrors.Create(
                DeadLetterErrorCodes.InvalidMessageType,
                $"{nameof(DeadLetterStoreEF)} requires messages of type {nameof(DeadLetterMessage)}, got {message.GetType().Name}");
        }

        return await EitherHelpers.TryAsync(async () =>
        {
            if (await IsCapturedAsync(entity, cancellationToken).ConfigureAwait(false))
            {
                return false;
            }

            await Messages.AddAsync(entity, cancellationToken).ConfigureAwait(false);
            return true;
        }, DeadLetterErrorCodes.StoreFailed).ConfigureAwait(false);
    }

    // The pending (tracked, unsaved) messages count too: two captures in one unit of work are one capture.
    private async Task<bool> IsCapturedAsync(DeadLetterMessage entity, CancellationToken cancellationToken)
    {
        var pattern = entity.SourcePattern;
        var sourceId = entity.SourceMessageId;

        return Messages.Local.Any(m => m.SourcePattern == pattern && m.SourceMessageId == sourceId)
               || await Messages.AsNoTracking()
                   .AnyAsync(m => m.SourcePattern == pattern && m.SourceMessageId == sourceId, cancellationToken)
                   .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Option<IDeadLetterMessage>>> GetAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        ValidateMessageId(messageId);

        return await EitherHelpers.TryAsync(async () =>
        {
            var message = await Messages.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken)
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
            var query = ApplyFilter(Messages.AsNoTracking(), filter);
            var ordered = newestFirst
                ? query.OrderByDescending(m => m.DeadLetteredAtUtc).ThenByDescending(m => m.Id)
                : query.OrderBy(m => m.DeadLetteredAtUtc).ThenBy(m => m.Id);

            var messages = await ordered.Skip(skip).Take(take).ToListAsync(cancellationToken).ConfigureAwait(false);
            return messages.Cast<IDeadLetterMessage>().ToList();
        }, DeadLetterErrorCodes.QueryFailed).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, int>> GetCountAsync(DeadLetterFilter? filter = null, CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(
            async () => await ApplyFilter(Messages.AsNoTracking(), filter).CountAsync(cancellationToken).ConfigureAwait(false),
            DeadLetterErrorCodes.QueryFailed).ConfigureAwait(false);
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
            var now = AsUtc(UtcNow());
            var expiredBefore = AsUtc(claimExpiredBeforeUtc);

            var rows = await Messages
                .Where(m => m.Id == messageId
                            && m.ReplayedAtUtc == null
                            && (m.ReplayClaimedAtUtc == null || m.ReplayClaimedAtUtc <= expiredBefore))
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.ReplayClaimedAtUtc, (DateTime?)now), cancellationToken)
                .ConfigureAwait(false);

            return rows == 1;
        }, DeadLetterErrorCodes.ClaimFailed).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, bool>> MarkAsReplayedAsync(Guid messageId, string replayResult, CancellationToken cancellationToken = default)
    {
        ValidateMessageId(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(replayResult);

        return await EitherHelpers.TryAsync(async () =>
        {
            var now = AsUtc(UtcNow());

            var rows = await Messages
                .Where(m => m.Id == messageId && m.ReplayedAtUtc == null)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(m => m.ReplayedAtUtc, (DateTime?)now).SetProperty(m => m.ReplayResult, replayResult),
                    cancellationToken)
                .ConfigureAwait(false);

            return rows == 1;
        }, DeadLetterErrorCodes.MarkReplayedFailed).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, bool>> DeleteAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        ValidateMessageId(messageId);

        return await EitherHelpers.TryAsync(
            async () => await Messages.Where(m => m.Id == messageId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false) > 0,
            DeadLetterErrorCodes.DeleteFailed).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, int>> DeleteManyAsync(DeadLetterFilter filter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        return await EitherHelpers.TryAsync(
            async () => await ApplyFilter(Messages, filter).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false),
            DeadLetterErrorCodes.DeleteFailed).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, int>> DeleteExpiredAsync(CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var now = AsUtc(UtcNow());

            return await Messages
                .Where(m => m.ExpiresAtUtc != null && m.ExpiresAtUtc <= now)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
        }, DeadLetterErrorCodes.CleanupFailed).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(
            async () =>
            {
                try
                {
                    await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    // A rejected insert (for example a lost race on the unique source key) must not stay tracked,
                    // or every later save of this scoped context would fail again.
                    DetachPendingInserts();
                    throw;
                }
            },
            DeadLetterErrorCodes.StoreFailed).ConfigureAwait(false);
    }

    private void DetachPendingInserts()
    {
        foreach (var entry in _dbContext.ChangeTracker.Entries<DeadLetterMessage>().Where(e => e.State == EntityState.Added).ToList())
        {
            entry.State = EntityState.Detached;
        }
    }

    private static void ValidateMessageId(Guid messageId)
        => ArgumentOutOfRangeException.ThrowIfEqual(messageId, Guid.Empty);

    // Npgsql rejects a DateTime of Kind.Unspecified for timestamptz; every instant is a UTC instant.
    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static DateTime? AsUtc(DateTime? value) => value is { } instant ? AsUtc(instant) : null;

    private static IQueryable<DeadLetterMessage> ApplyFilter(IQueryable<DeadLetterMessage> query, DeadLetterFilter? filter)
    {
        return filter is null ? query : ApplyState(ApplyIdentity(query, filter), filter);
    }

    // A null or empty filter value matches everything (the contract shared with the fake store).
    private static IQueryable<DeadLetterMessage> ApplyIdentity(IQueryable<DeadLetterMessage> query, DeadLetterFilter filter)
    {
        var sourcePattern = filter.SourcePattern;
        var requestType = filter.RequestType;
        var errorCode = filter.ErrorCode;
        var correlationId = filter.CorrelationId;
        var tenantId = filter.TenantId;
        var sourceMessageId = filter.SourceMessageId;

        return query
            .WhereIf(!string.IsNullOrEmpty(sourcePattern), m => m.SourcePattern == sourcePattern)
            .WhereIf(!string.IsNullOrEmpty(requestType), m => m.RequestType == requestType)
            .WhereIf(!string.IsNullOrEmpty(errorCode), m => m.ErrorCode == errorCode)
            .WhereIf(!string.IsNullOrEmpty(correlationId), m => m.CorrelationId == correlationId)
            .WhereIf(!string.IsNullOrEmpty(tenantId), m => m.TenantId == tenantId)
            .WhereIf(!string.IsNullOrEmpty(sourceMessageId), m => m.SourceMessageId == sourceMessageId);
    }

    private static IQueryable<DeadLetterMessage> ApplyState(IQueryable<DeadLetterMessage> query, DeadLetterFilter filter)
    {
        var after = AsUtc(filter.DeadLetteredAfterUtc);
        var before = AsUtc(filter.DeadLetteredBeforeUtc);
        var expiresBy = AsUtc(filter.ExpiresAtOrBeforeUtc);

        return query
            .WhereIf(filter.ExcludeReplayed is true, m => m.ReplayedAtUtc == null)
            .WhereIf(filter.ExcludeReplayed is false, m => m.ReplayedAtUtc != null)
            .WhereIf(after.HasValue, m => m.DeadLetteredAtUtc >= after!.Value)
            .WhereIf(before.HasValue, m => m.DeadLetteredAtUtc <= before!.Value)
            .WhereIf(expiresBy.HasValue, m => m.ExpiresAtUtc != null && m.ExpiresAtUtc <= expiresBy!.Value);
    }
}

internal static class DeadLetterQueryableExtensions
{
    internal static IQueryable<T> WhereIf<T>(this IQueryable<T> query, bool condition, Expression<Func<T, bool>> predicate)
        => condition ? query.Where(predicate) : query;
}
