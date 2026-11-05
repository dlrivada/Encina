using System.Security.Cryptography;
using System.Text;
using System.Transactions;
using Encina.Security.Audit;
using Encina.Security.BreakTheGlass;
using LanguageExt;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.BreakTheGlass;

/// <summary>
/// Entity Framework Core implementation of <see cref="IBreakTheGlassStore"/> for SQL Server.
/// </summary>
/// <typeparam name="TContext">The application's DbContext, whose model applies <see cref="BreakTheGlassModelBuilderExtensions.ApplyBreakTheGlassConfiguration"/>.</typeparam>
/// <remarks>
/// <para>
/// Every call runs on a context of its own created by <see cref="IDbContextFactory{TContext}"/>, never on the application's scoped
/// context, so a revoke, a review or a sweeper update survives a rollback of the request that issued it and the store never flushes
/// the application's pending changes. Conditional updates are single <c>ExecuteUpdateAsync</c> statements whose rows affected
/// decide the result. The store never reads the clock: every timestamp comes from its caller.
/// </para>
/// <para>
/// <b>Atomic add.</b> <c>ActiveKey</c> (computed by the database, see <see cref="BreakTheGlassElevationEntityConfiguration"/>) sits
/// under a unique index. <see cref="AddAsync"/> closes the stale Active rows of the key and inserts; of two concurrent inserts the
/// index lets one through and the other fails with SQL Server error 2627 or 2601 on that index, which becomes
/// <c>break_glass.already_active</c>. A blank or whitespace tenant is stored and matched as no tenant. A provider failure becomes
/// <c>Left(break_glass.store_failed)</c>; a cancelled call still throws.
/// </para>
/// </remarks>
public sealed class BreakTheGlassStoreEF<TContext> : IBreakTheGlassStore
    where TContext : DbContext
{
    private const string StaleCloseReason = "expired";
    private const char KeySeparator = '\u001f';

    private readonly IDbContextFactory<TContext> _contextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="BreakTheGlassStoreEF{TContext}"/> class.
    /// </summary>
    /// <param name="contextFactory">The factory of the contexts the store writes through.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="contextFactory"/> is null.</exception>
    public BreakTheGlassStoreEF(IDbContextFactory<TContext> contextFactory)
    {
        ArgumentNullException.ThrowIfNull(contextFactory);
        _contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> AddAsync(BreakTheGlassElevation elevation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(elevation);
        var entity = ToEntity(elevation);
        if (ContainsKeySeparator(entity.TenantId, entity.UserId, entity.SubjectId, entity.Scope))
        {
            // The separator would let two different key tuples hash to the same ActiveKey.
            return ValueTask.FromResult(Left<EncinaError, Unit>(BreakTheGlassErrors.ValidationFailed("key_part_separator")));
        }

        var key = ActiveKeyOf(entity.TenantId, entity.UserId, entity.SubjectId, entity.Scope);

        return RunAsync<Unit>(async (db, ct) =>
        {
            // Stale Active rows of the key are closed first (ExpiryRecordedAtUtc stays null, so the sweeper still audits them);
            // the unique ActiveKey index then arbitrates the insert.
            await db.Set<BreakTheGlassElevationEntity>()
                .Where(e => e.ActiveKey == key && e.ExpiresAtUtc <= entity.OpenedAtUtc)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(e => e.Status, BreakTheGlassStatus.Expired)
                        .SetProperty(e => e.CloseReason, StaleCloseReason)
                        .SetProperty(e => e.ClosedAtUtc, entity.OpenedAtUtc),
                    ct).ConfigureAwait(false);

            db.Add(entity);
            try
            {
                await db.SaveChangesAsync(ct).ConfigureAwait(false);
                return Right<EncinaError, Unit>(unit);
            }
            catch (DbUpdateException ex) when (IsActiveKeyViolation(ex))
            {
                return Left<EncinaError, Unit>(BreakTheGlassErrors.AlreadyActive());
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Option<BreakTheGlassElevation>>> GetByIdAsync(string? tenantId, Guid elevationId, CancellationToken cancellationToken = default)
    {
        tenantId = Normalise(tenantId);

        return RunAsync<Option<BreakTheGlassElevation>>(async (db, ct) =>
        {
            var row = await ForTenant(db, tenantId)
                .Where(e => e.ElevationId == elevationId)
                .FirstOrDefaultAsync(ct).ConfigureAwait(false);
            return Right<EncinaError, Option<BreakTheGlassElevation>>(row is null ? None : Some(ToModel(row)));
        }, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Option<BreakTheGlassElevation>>> FindEffectiveAsync(
        string? tenantId, string userId, string subjectId, string scope, DateTimeOffset nowUtc, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(subjectId);
        ArgumentNullException.ThrowIfNull(scope);
        tenantId = Normalise(tenantId);
        if (ContainsKeySeparator(tenantId, userId, subjectId, scope))
        {
            // AddAsync never stores such a tuple, so nothing can be effective for it.
            return ValueTask.FromResult(Right<EncinaError, Option<BreakTheGlassElevation>>(None));
        }

        var key = ActiveKeyOf(tenantId, userId, subjectId, scope);
        var now = nowUtc.UtcDateTime;

        return RunAsync<Option<BreakTheGlassElevation>>(async (db, ct) =>
        {
            // ActiveKey is set only for Active rows, and compares the parts exactly (a hash of their bytes).
            var row = await db.Set<BreakTheGlassElevationEntity>().AsNoTracking()
                .Where(e => e.ActiveKey == key && e.ExpiresAtUtc > now)
                .OrderByDescending(e => e.OpenedAtUtc)
                .FirstOrDefaultAsync(ct).ConfigureAwait(false);
            return Right<EncinaError, Option<BreakTheGlassElevation>>(row is null ? None : Some(ToModel(row)));
        }, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, bool>> TryCloseAsync(
        string? tenantId, Guid elevationId, BreakTheGlassStatus newStatus, string reason, DateTimeOffset closedAtUtc, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reason);
        tenantId = Normalise(tenantId);
        var closedAt = closedAtUtc.UtcDateTime;

        return RunAsync<bool>(async (db, ct) =>
        {
            var changed = await ForTenant(db, tenantId)
                .Where(e => e.ElevationId == elevationId && e.Status == BreakTheGlassStatus.Active)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(e => e.Status, newStatus)
                        .SetProperty(e => e.CloseReason, reason)
                        .SetProperty(e => e.ClosedAtUtc, closedAt),
                    ct).ConfigureAwait(false);
            return Right<EncinaError, bool>(changed == 1);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<BreakTheGlassElevation>>> GetExpiredUnrecordedAsync(
        DateTimeOffset nowUtc, int batchSize, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);
        var now = nowUtc.UtcDateTime;

        return RunAsync<IReadOnlyList<BreakTheGlassElevation>>(async (db, ct) =>
        {
            var rows = await db.Set<BreakTheGlassElevationEntity>().AsNoTracking()
                .Where(e => (e.Status == BreakTheGlassStatus.Active && e.ExpiresAtUtc <= now)
                    || (e.Status == BreakTheGlassStatus.Expired && e.ExpiryRecordedAtUtc == null))
                .OrderBy(e => e.ExpiresAtUtc).ThenBy(e => e.ElevationId)
                .Take(batchSize)
                .ToListAsync(ct).ConfigureAwait(false);
            return Right<EncinaError, IReadOnlyList<BreakTheGlassElevation>>(rows.ConvertAll(ToModel));
        }, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> MarkExpiryRecordedAsync(string? tenantId, Guid elevationId, DateTimeOffset recordedAtUtc, CancellationToken cancellationToken = default)
    {
        tenantId = Normalise(tenantId);
        var recordedAt = recordedAtUtc.UtcDateTime;

        return RunAsync<Unit>(async (db, ct) =>
        {
            await ForTenant(db, tenantId)
                .Where(e => e.ElevationId == elevationId && e.Status == BreakTheGlassStatus.Expired)
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.ExpiryRecordedAtUtc, recordedAt), ct).ConfigureAwait(false);
            return Right<EncinaError, Unit>(unit);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, bool>> TryRecordReviewAsync(
        string? tenantId, Guid elevationId, BreakTheGlassReviewOutcome outcome, string reviewerUserId, string? note, DateTimeOffset reviewedAtUtc, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reviewerUserId);
        tenantId = Normalise(tenantId);
        var reviewedAt = reviewedAtUtc.UtcDateTime;

        return RunAsync<bool>(async (db, ct) =>
        {
            var changed = await ForTenant(db, tenantId)
                .Where(e => e.ElevationId == elevationId && e.ReviewOutcome == null)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(e => e.ReviewOutcome, outcome)
                        .SetProperty(e => e.ReviewedByUserId, reviewerUserId)
                        .SetProperty(e => e.ReviewNote, note)
                        .SetProperty(e => e.ReviewedAtUtc, reviewedAt),
                    ct).ConfigureAwait(false);
            return Right<EncinaError, bool>(changed == 1);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, PagedResult<BreakTheGlassElevation>>> QueryReviewQueueAsync(
        string? tenantId, BreakTheGlassReviewQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        tenantId = Normalise(tenantId);
        var pageNumber = Math.Max(1, query.PageNumber);
        var pageSize = Math.Clamp(query.PageSize, 1, BreakTheGlassReviewQuery.MaxPageSize);

        return RunAsync<PagedResult<BreakTheGlassElevation>>(async (db, ct) =>
        {
            var filtered = ApplyFilters(ForTenant(db, tenantId).Where(e => e.ReviewOutcome == null), query);
            var total = await filtered.CountAsync(ct).ConfigureAwait(false);
            // The offset is computed in long: a huge page number must not wrap to a negative or a foreign offset.
            var offset = (long)(pageNumber - 1) * pageSize;
            var rows = offset >= total
                ? []
                : await filtered
                    .OrderBy(e => e.OpenedAtUtc).ThenBy(e => e.ElevationId)
                    .Skip((int)offset).Take(pageSize)
                    .ToListAsync(ct).ConfigureAwait(false);
            return Right<EncinaError, PagedResult<BreakTheGlassElevation>>(
                PagedResult<BreakTheGlassElevation>.Create(rows.ConvertAll(ToModel), total, pageNumber, pageSize));
        }, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, int>> PurgeAsync(DateTimeOffset reviewedBeforeUtc, CancellationToken cancellationToken = default)
    {
        var cutoff = reviewedBeforeUtc.UtcDateTime;

        return RunAsync<int>(async (db, ct) =>
        {
            // Closed and reviewed; an Expired row also needs its expiry recorded, or the sweeper would never audit it.
            var deleted = await db.Set<BreakTheGlassElevationEntity>()
                .Where(e => e.Status != BreakTheGlassStatus.Active
                    && e.ReviewedAtUtc < cutoff
                    && (e.Status != BreakTheGlassStatus.Expired || e.ExpiryRecordedAtUtc != null))
                .ExecuteDeleteAsync(ct).ConfigureAwait(false);
            return Right<EncinaError, int>(deleted);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> ProbeAsync(CancellationToken cancellationToken = default)
    {
        return RunAsync<Unit>(async (db, ct) =>
            await db.Database.CanConnectAsync(ct).ConfigureAwait(false)
                ? Right<EncinaError, Unit>(unit)
                : Left<EncinaError, Unit>(BreakTheGlassErrors.StoreFailed()),
            cancellationToken);
    }

    // The hash the table computes for an Active row: SHA-256 over the UTF-16 key parts joined with NCHAR(31) (a null tenant is
    // ''). It must match the ActiveKey expression of BreakTheGlassElevationEntityConfiguration and of the shipped script.
    internal static byte[] ActiveKeyOf(string? tenantId, string userId, string subjectId, string scope) =>
        SHA256.HashData(Encoding.Unicode.GetBytes(
            string.Concat(tenantId ?? string.Empty, KeySeparator, userId, KeySeparator, subjectId, KeySeparator, scope)));

    private static bool ContainsKeySeparator(string? tenantId, string userId, string subjectId, string scope) =>
        (tenantId?.Contains(KeySeparator) ?? false)
        || userId.Contains(KeySeparator)
        || subjectId.Contains(KeySeparator)
        || scope.Contains(KeySeparator);

    internal static bool IsActiveKeyViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2627 or 2601 } sql
        && sql.Message.Contains(BreakTheGlassElevationEntityConfiguration.ActiveKeyIndexName, StringComparison.Ordinal);

    // Runs the work on a context of its own. A provider failure is Left(store_failed); a cancelled call still cancels.
    private async ValueTask<Either<EncinaError, T>> RunAsync<T>(
        Func<DbContext, CancellationToken, Task<Either<EncinaError, T>>> work,
        CancellationToken cancellationToken)
    {
        try
        {
            // The store never joins the caller's ambient System.Transactions scope (SqlClient would enlist the new connection in
            // it): a revoke, a review or a purge must survive the rollback of the business transaction (plan decisions P15 = B and P6 = C).
            using var suppress = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled);
            await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            return await work(db, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            return Left<EncinaError, T>(BreakTheGlassErrors.StoreFailed(ex));
        }
    }

    private static string? Normalise(string? tenantId) => string.IsNullOrWhiteSpace(tenantId) ? null : tenantId;

    private static IQueryable<BreakTheGlassElevationEntity> ForTenant(DbContext db, string? tenantId)
    {
        var rows = db.Set<BreakTheGlassElevationEntity>().AsNoTracking();
        // SQL Server ignores trailing spaces in "=", so "acme" and "acme " would match; the length check keeps tenants apart.
        // The column has a binary collation, so the comparison is already case-sensitive.
        if (tenantId is null)
        {
            return rows.Where(e => e.TenantId == null);
        }

        var byteLength = tenantId.Length * sizeof(char);
        return rows.Where(e => e.TenantId == tenantId && EF.Functions.DataLength(e.TenantId) == byteLength);
    }

    private static IQueryable<BreakTheGlassElevationEntity> ApplyFilters(IQueryable<BreakTheGlassElevationEntity> rows, BreakTheGlassReviewQuery query)
    {
        if (query.Scope is { } scope)
        {
            rows = rows.Where(e => e.Scope == scope);
        }

        if (query.Status is { } status)
        {
            rows = rows.Where(e => e.Status == status);
        }

        if (query.OpenedFromUtc is { } from)
        {
            var fromUtc = from.UtcDateTime;
            rows = rows.Where(e => e.OpenedAtUtc >= fromUtc);
        }

        if (query.OpenedToUtc is { } to)
        {
            var toUtc = to.UtcDateTime;
            rows = rows.Where(e => e.OpenedAtUtc < toUtc);
        }

        return rows;
    }

    private static BreakTheGlassElevationEntity ToEntity(BreakTheGlassElevation e) => new()
    {
        ElevationId = e.ElevationId,
        TenantId = Normalise(e.TenantId),
        UserId = e.UserId,
        SubjectId = e.SubjectId,
        Scope = e.Scope,
        Justification = e.Justification,
        OpenedAtUtc = e.OpenedAtUtc.UtcDateTime,
        ExpiresAtUtc = e.ExpiresAtUtc.UtcDateTime,
        Status = e.Status,
        ClosedAtUtc = e.ClosedAtUtc?.UtcDateTime,
        CloseReason = e.CloseReason,
        ExpiryRecordedAtUtc = e.ExpiryRecordedAtUtc?.UtcDateTime,
        ReviewOutcome = e.ReviewOutcome,
        ReviewedByUserId = e.ReviewedByUserId,
        ReviewedAtUtc = e.ReviewedAtUtc?.UtcDateTime,
        ReviewNote = e.ReviewNote
    };

    private static BreakTheGlassElevation ToModel(BreakTheGlassElevationEntity e) => new()
    {
        ElevationId = e.ElevationId,
        TenantId = e.TenantId,
        UserId = e.UserId,
        SubjectId = e.SubjectId,
        Scope = e.Scope,
        Justification = e.Justification,
        OpenedAtUtc = ToUtc(e.OpenedAtUtc),
        ExpiresAtUtc = ToUtc(e.ExpiresAtUtc),
        Status = e.Status,
        ClosedAtUtc = ToUtc(e.ClosedAtUtc),
        CloseReason = e.CloseReason,
        ExpiryRecordedAtUtc = ToUtc(e.ExpiryRecordedAtUtc),
        ReviewOutcome = e.ReviewOutcome,
        ReviewedByUserId = e.ReviewedByUserId,
        ReviewedAtUtc = ToUtc(e.ReviewedAtUtc),
        ReviewNote = e.ReviewNote
    };

    private static DateTimeOffset ToUtc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static DateTimeOffset? ToUtc(DateTime? value) => value is null ? null : ToUtc(value.Value);
}
