using System.Data;
using System.Text;
using Dapper;
using Encina.Messaging;
using Encina.Messaging.Transactions;
using Encina.Security.Audit;
using Encina.Security.BreakTheGlass;
using LanguageExt;
using Npgsql;
using static LanguageExt.Prelude;

namespace Encina.Dapper.PostgreSQL.BreakTheGlass;

/// <summary>
/// Dapper PostgreSQL implementation of <see cref="IBreakTheGlassStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Every statement is parameterized and runs asynchronously with the caller's <see cref="CancellationToken"/> on a
/// <see cref="DbLease"/> that is independent of the business transaction (ADR-048), so a revoke, a review or a
/// sweeper update survives a rollback of the request that issued it. The store never reads the clock: every
/// timestamp comes from its caller and is written and read back as UTC (<c>TIMESTAMPTZ</c> stores an instant, so
/// the session time zone cannot change it).
/// </para>
/// <para>
/// <b>Atomic add.</b> The table carries a partial unique index <c>UX_BreakTheGlassElevations_ActiveKey</c> over the
/// tenant (no tenant is the empty string), user, subject and scope of the Active rows. <see cref="AddAsync"/> closes
/// the stale Active rows of the key and inserts; of two concurrent inserts the index lets one through and the other
/// fails with SQLSTATE 23505 on that index, which becomes <c>break_glass.already_active</c>. Any other unique
/// violation (the primary key) stays <c>break_glass.store_failed</c>. A blank tenant is stored and matched as no tenant.
/// </para>
/// </remarks>
public sealed class BreakTheGlassStoreDapper : IBreakTheGlassStore
{
    private const string Columns =
        "\"ElevationId\", \"TenantId\", \"UserId\", \"SubjectId\", \"Scope\", \"Justification\", \"OpenedAtUtc\", \"ExpiresAtUtc\", \"Status\", " +
        "\"ClosedAtUtc\", \"CloseReason\", \"ExpiryRecordedAtUtc\", \"ReviewOutcome\", \"ReviewedByUserId\", \"ReviewedAtUtc\", \"ReviewNote\"";

    // Must match the index expression of the table script (030_CreateBreakTheGlassElevationsTable.sql).
    private const string ActiveKeyPredicate =
        "\"Status\" = @Active AND COALESCE(\"TenantId\", '') = @TenantKey AND \"UserId\" = @UserId AND \"SubjectId\" = @SubjectId AND \"Scope\" = @Scope";

    private const string ActiveKeyIndex = "UX_BreakTheGlassElevations_ActiveKey";
    private const string UniqueViolation = "23505";
    private const string StaleCloseReason = "expired";

    private readonly IDbConnection _connection;
    private readonly IDbTransactionAccessor? _transactionAccessor;
    private readonly string _table;

    /// <summary>
    /// Initializes a new instance of the <see cref="BreakTheGlassStoreDapper"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="transactionAccessor">Optional accessor of the business transaction on the shared connection, so that every write can leave it (ADR-048).</param>
    /// <param name="tableName">The elevations table name (default: BreakTheGlassElevations).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tableName"/> is not a valid identifier.</exception>
    public BreakTheGlassStoreDapper(
        IDbConnection connection,
        IDbTransactionAccessor? transactionAccessor = null,
        string tableName = "BreakTheGlassElevations")
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _transactionAccessor = transactionAccessor;
        _table = $"\"{SqlIdentifierValidator.ValidateTableName(tableName)}\"";
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> AddAsync(BreakTheGlassElevation elevation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(elevation);
        var tenantId = Normalise(elevation.TenantId);

        return await RunAsync<Unit>(async (lease, ct) =>
        {
            // Stale Active rows of the key are closed first (ExpiryRecordedAtUtc stays null, so the sweeper still audits them);
            // the partial unique index then arbitrates the insert.
            var sql =
                $@"UPDATE {_table} SET ""Status"" = @Expired, ""CloseReason"" = @StaleReason, ""ClosedAtUtc"" = @OpenedAtUtc
                   WHERE {ActiveKeyPredicate} AND ""ExpiresAtUtc"" <= @OpenedAtUtc;
                   INSERT INTO {_table} ({Columns})
                   VALUES (@ElevationId, @TenantId, @UserId, @SubjectId, @Scope, @Justification, @OpenedAtUtc, @ExpiresAtUtc, @Status,
                           @ClosedAtUtc, @CloseReason, @ExpiryRecordedAtUtc, @ReviewOutcome, @ReviewedByUserId, @ReviewedAtUtc, @ReviewNote)";
            var parameters = new DynamicParameters();
            parameters.Add("Active", (int)BreakTheGlassStatus.Active, DbType.Int32);
            parameters.Add("Expired", (int)BreakTheGlassStatus.Expired, DbType.Int32);
            parameters.Add("StaleReason", StaleCloseReason, DbType.String);
            parameters.Add("TenantKey", tenantId ?? string.Empty, DbType.String);
            parameters.Add("ElevationId", elevation.ElevationId, DbType.Guid);
            parameters.Add("TenantId", tenantId, DbType.String);
            parameters.Add("UserId", elevation.UserId, DbType.String);
            parameters.Add("SubjectId", elevation.SubjectId, DbType.String);
            parameters.Add("Scope", elevation.Scope, DbType.String);
            parameters.Add("Justification", elevation.Justification, DbType.String);
            AddDate(parameters, "OpenedAtUtc", elevation.OpenedAtUtc);
            AddDate(parameters, "ExpiresAtUtc", elevation.ExpiresAtUtc);
            parameters.Add("Status", (int)elevation.Status, DbType.Int32);
            AddDate(parameters, "ClosedAtUtc", elevation.ClosedAtUtc);
            parameters.Add("CloseReason", elevation.CloseReason, DbType.String);
            AddDate(parameters, "ExpiryRecordedAtUtc", elevation.ExpiryRecordedAtUtc);
            parameters.Add("ReviewOutcome", (int?)elevation.ReviewOutcome, DbType.Int32);
            parameters.Add("ReviewedByUserId", elevation.ReviewedByUserId, DbType.String);
            AddDate(parameters, "ReviewedAtUtc", elevation.ReviewedAtUtc);
            parameters.Add("ReviewNote", elevation.ReviewNote, DbType.String);

            try
            {
                await lease.Connection.ExecuteAsync(new CommandDefinition(sql, parameters, lease.Transaction, cancellationToken: ct)).ConfigureAwait(false);
                return Right<EncinaError, Unit>(unit);
            }
            catch (PostgresException ex) when (ex.SqlState == UniqueViolation && ex.ConstraintName == ActiveKeyIndex)
            {
                return Left<EncinaError, Unit>(BreakTheGlassErrors.AlreadyActive());
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Option<BreakTheGlassElevation>>> GetByIdAsync(string? tenantId, Guid elevationId, CancellationToken cancellationToken = default)
    {
        tenantId = Normalise(tenantId);

        return RunAsync<Option<BreakTheGlassElevation>>(async (lease, ct) =>
        {
            var parameters = new DynamicParameters();
            parameters.Add("ElevationId", elevationId, DbType.Guid);
            AddTenant(parameters, tenantId);

            var rows = await QueryAsync(lease, $"SELECT {Columns} FROM {_table} WHERE \"ElevationId\" = @ElevationId AND {TenantFilter(tenantId)}", parameters, ct).ConfigureAwait(false);
            return Right<EncinaError, Option<BreakTheGlassElevation>>(rows.Count == 0 ? None : Some(rows[0]));
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

        return RunAsync<Option<BreakTheGlassElevation>>(async (lease, ct) =>
        {
            var parameters = new DynamicParameters();
            parameters.Add("Active", (int)BreakTheGlassStatus.Active, DbType.Int32);
            parameters.Add("TenantKey", tenantId ?? string.Empty, DbType.String);
            parameters.Add("UserId", userId, DbType.String);
            parameters.Add("SubjectId", subjectId, DbType.String);
            parameters.Add("Scope", scope, DbType.String);
            AddDate(parameters, "NowUtc", nowUtc);

            var rows = await QueryAsync(
                lease,
                $"SELECT {Columns} FROM {_table} WHERE {ActiveKeyPredicate} AND \"ExpiresAtUtc\" > @NowUtc ORDER BY \"OpenedAtUtc\" DESC LIMIT 1",
                parameters,
                ct).ConfigureAwait(false);
            return Right<EncinaError, Option<BreakTheGlassElevation>>(rows.Count == 0 ? None : Some(rows[0]));
        }, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, bool>> TryCloseAsync(
        string? tenantId, Guid elevationId, BreakTheGlassStatus newStatus, string reason, DateTimeOffset closedAtUtc, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reason);
        tenantId = Normalise(tenantId);

        return RunAsync<bool>(async (lease, ct) =>
        {
            var parameters = new DynamicParameters();
            parameters.Add("Status", (int)newStatus, DbType.Int32);
            parameters.Add("Reason", reason, DbType.String);
            AddDate(parameters, "ClosedAtUtc", closedAtUtc);
            parameters.Add("ElevationId", elevationId, DbType.Guid);
            AddTenant(parameters, tenantId);
            parameters.Add("Active", (int)BreakTheGlassStatus.Active, DbType.Int32);

            var affected = await ExecuteAsync(
                lease,
                $@"UPDATE {_table} SET ""Status"" = @Status, ""CloseReason"" = @Reason, ""ClosedAtUtc"" = @ClosedAtUtc
                   WHERE ""ElevationId"" = @ElevationId AND {TenantFilter(tenantId)} AND ""Status"" = @Active",
                parameters,
                ct).ConfigureAwait(false);
            return Right<EncinaError, bool>(affected == 1);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<BreakTheGlassElevation>>> GetExpiredUnrecordedAsync(
        DateTimeOffset nowUtc, int batchSize, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);

        return RunAsync<IReadOnlyList<BreakTheGlassElevation>>(async (lease, ct) =>
        {
            var parameters = new DynamicParameters();
            parameters.Add("BatchSize", batchSize, DbType.Int32);
            parameters.Add("Active", (int)BreakTheGlassStatus.Active, DbType.Int32);
            parameters.Add("Expired", (int)BreakTheGlassStatus.Expired, DbType.Int32);
            AddDate(parameters, "NowUtc", nowUtc);

            var rows = await QueryAsync(
                lease,
                $@"SELECT {Columns} FROM {_table}
                   WHERE (""Status"" = @Active AND ""ExpiresAtUtc"" <= @NowUtc) OR (""Status"" = @Expired AND ""ExpiryRecordedAtUtc"" IS NULL)
                   ORDER BY ""ExpiresAtUtc"", ""ElevationId""
                   LIMIT @BatchSize",
                parameters,
                ct).ConfigureAwait(false);
            return Right<EncinaError, IReadOnlyList<BreakTheGlassElevation>>(rows);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> MarkExpiryRecordedAsync(string? tenantId, Guid elevationId, DateTimeOffset recordedAtUtc, CancellationToken cancellationToken = default)
    {
        tenantId = Normalise(tenantId);

        return RunAsync<Unit>(async (lease, ct) =>
        {
            var parameters = new DynamicParameters();
            AddDate(parameters, "RecordedAtUtc", recordedAtUtc);
            parameters.Add("ElevationId", elevationId, DbType.Guid);
            AddTenant(parameters, tenantId);
            parameters.Add("Expired", (int)BreakTheGlassStatus.Expired, DbType.Int32);

            await ExecuteAsync(
                lease,
                $"UPDATE {_table} SET \"ExpiryRecordedAtUtc\" = @RecordedAtUtc WHERE \"ElevationId\" = @ElevationId AND {TenantFilter(tenantId)} AND \"Status\" = @Expired",
                parameters,
                ct).ConfigureAwait(false);
            return Right<EncinaError, Unit>(unit);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, bool>> TryRecordReviewAsync(
        string? tenantId, Guid elevationId, BreakTheGlassReviewOutcome outcome, string reviewerUserId, string? note, DateTimeOffset reviewedAtUtc, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reviewerUserId);
        tenantId = Normalise(tenantId);

        return RunAsync<bool>(async (lease, ct) =>
        {
            var parameters = new DynamicParameters();
            parameters.Add("Outcome", (int)outcome, DbType.Int32);
            parameters.Add("ReviewerUserId", reviewerUserId, DbType.String);
            parameters.Add("Note", note, DbType.String);
            AddDate(parameters, "ReviewedAtUtc", reviewedAtUtc);
            parameters.Add("ElevationId", elevationId, DbType.Guid);
            AddTenant(parameters, tenantId);

            var affected = await ExecuteAsync(
                lease,
                $@"UPDATE {_table} SET ""ReviewOutcome"" = @Outcome, ""ReviewedByUserId"" = @ReviewerUserId, ""ReviewNote"" = @Note, ""ReviewedAtUtc"" = @ReviewedAtUtc
                   WHERE ""ElevationId"" = @ElevationId AND {TenantFilter(tenantId)} AND ""ReviewOutcome"" IS NULL",
                parameters,
                ct).ConfigureAwait(false);
            return Right<EncinaError, bool>(affected == 1);
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

        return RunAsync<PagedResult<BreakTheGlassElevation>>(async (lease, ct) =>
        {
            var filter = new StringBuilder($"{TenantFilter(tenantId)} AND \"ReviewOutcome\" IS NULL");
            var parameters = new DynamicParameters();
            AddTenant(parameters, tenantId);
            AppendQueryFilters(filter, parameters, query);

            var total = await lease.Connection.ExecuteScalarAsync<int>(
                new CommandDefinition($"SELECT COUNT(*) FROM {_table} WHERE {filter}", parameters, lease.Transaction, cancellationToken: ct)).ConfigureAwait(false);

            parameters.Add("Offset", (pageNumber - 1) * pageSize, DbType.Int32);
            parameters.Add("PageSize", pageSize, DbType.Int32);
            var items = await QueryAsync(
                lease,
                $@"SELECT {Columns} FROM {_table} WHERE {filter}
                   ORDER BY ""OpenedAtUtc"" ASC, ""ElevationId"" ASC
                   LIMIT @PageSize OFFSET @Offset",
                parameters,
                ct).ConfigureAwait(false);
            return Right<EncinaError, PagedResult<BreakTheGlassElevation>>(PagedResult<BreakTheGlassElevation>.Create(items, total, pageNumber, pageSize));
        }, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, int>> PurgeAsync(DateTimeOffset reviewedBeforeUtc, CancellationToken cancellationToken = default)
    {
        return RunAsync<int>(async (lease, ct) =>
        {
            // Closed and reviewed; an Expired row also needs its expiry recorded, or the sweeper would never audit it.
            var parameters = new DynamicParameters();
            parameters.Add("Active", (int)BreakTheGlassStatus.Active, DbType.Int32);
            parameters.Add("Expired", (int)BreakTheGlassStatus.Expired, DbType.Int32);
            AddDate(parameters, "CutoffUtc", reviewedBeforeUtc);

            var deleted = await ExecuteAsync(
                lease,
                $"DELETE FROM {_table} WHERE \"Status\" <> @Active AND \"ReviewedAtUtc\" < @CutoffUtc AND (\"Status\" <> @Expired OR \"ExpiryRecordedAtUtc\" IS NOT NULL)",
                parameters,
                ct).ConfigureAwait(false);
            return Right<EncinaError, int>(deleted);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> ProbeAsync(CancellationToken cancellationToken = default)
    {
        return RunAsync<Unit>(async (lease, ct) =>
        {
            await lease.Connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT 1", transaction: lease.Transaction, cancellationToken: ct)).ConfigureAwait(false);
            return Right<EncinaError, Unit>(unit);
        }, cancellationToken);
    }

    // Runs the work on a lease that outlives a business rollback. A provider failure is Left(store_failed); a cancelled call still cancels.
    private async ValueTask<Either<EncinaError, T>> RunAsync<T>(
        Func<DbLease, CancellationToken, Task<Either<EncinaError, T>>> work,
        CancellationToken cancellationToken)
    {
        try
        {
            using var lease = await DbLease.IndependentAsync(_connection, _transactionAccessor, cancellationToken).ConfigureAwait(false);
            await lease.EnsureOpenAsync(cancellationToken).ConfigureAwait(false);
            return await work(lease, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            return Left<EncinaError, T>(BreakTheGlassErrors.StoreFailed(ex));
        }
    }

    private static string? Normalise(string? tenantId) => string.IsNullOrWhiteSpace(tenantId) ? null : tenantId;

    private static string TenantFilter(string? tenantId) => tenantId is null ? "\"TenantId\" IS NULL" : "\"TenantId\" = @TenantId";

    private static void AddTenant(DynamicParameters parameters, string? tenantId)
    {
        if (tenantId is not null)
        {
            parameters.Add("TenantId", tenantId, DbType.String);
        }
    }

    // A DateTimeOffset at offset zero is a TIMESTAMPTZ instant for Npgsql.
    private static void AddDate(DynamicParameters parameters, string name, DateTimeOffset? value) =>
        parameters.Add(name, value?.ToUniversalTime(), DbType.DateTimeOffset);

    // Appends the optional filters of the review queue and binds their parameters.
    private static void AppendQueryFilters(StringBuilder filter, DynamicParameters parameters, BreakTheGlassReviewQuery query)
    {
        if (query.Scope is not null)
        {
            filter.Append(" AND \"Scope\" = @Scope");
            parameters.Add("Scope", query.Scope, DbType.String);
        }

        if (query.Status is not null)
        {
            filter.Append(" AND \"Status\" = @FilterStatus");
            parameters.Add("FilterStatus", (int)query.Status.Value, DbType.Int32);
        }

        if (query.OpenedFromUtc is not null)
        {
            filter.Append(" AND \"OpenedAtUtc\" >= @OpenedFromUtc");
            AddDate(parameters, "OpenedFromUtc", query.OpenedFromUtc);
        }

        if (query.OpenedToUtc is not null)
        {
            filter.Append(" AND \"OpenedAtUtc\" < @OpenedToUtc");
            AddDate(parameters, "OpenedToUtc", query.OpenedToUtc);
        }
    }

    private static Task<int> ExecuteAsync(DbLease lease, string sql, DynamicParameters parameters, CancellationToken cancellationToken) =>
        lease.Connection.ExecuteAsync(new CommandDefinition(sql, parameters, lease.Transaction, cancellationToken: cancellationToken));

    private static async Task<List<BreakTheGlassElevation>> QueryAsync(DbLease lease, string sql, DynamicParameters parameters, CancellationToken cancellationToken)
    {
        var rows = await lease.Connection.QueryAsync<Row>(new CommandDefinition(sql, parameters, lease.Transaction, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows.Select(Map).ToList();
    }

    private static BreakTheGlassElevation Map(Row row) => new()
    {
        ElevationId = row.ElevationId,
        TenantId = row.TenantId,
        UserId = row.UserId,
        SubjectId = row.SubjectId,
        Scope = row.Scope,
        Justification = row.Justification,
        OpenedAtUtc = ToUtc(row.OpenedAtUtc),
        ExpiresAtUtc = ToUtc(row.ExpiresAtUtc),
        Status = (BreakTheGlassStatus)row.Status,
        ClosedAtUtc = ToUtc(row.ClosedAtUtc),
        CloseReason = row.CloseReason,
        ExpiryRecordedAtUtc = ToUtc(row.ExpiryRecordedAtUtc),
        ReviewOutcome = (BreakTheGlassReviewOutcome?)row.ReviewOutcome,
        ReviewedByUserId = row.ReviewedByUserId,
        ReviewedAtUtc = ToUtc(row.ReviewedAtUtc),
        ReviewNote = row.ReviewNote
    };

    private static DateTimeOffset ToUtc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static DateTimeOffset? ToUtc(DateTime? value) => value is null ? null : ToUtc(value.Value);

    // The row as the table stores it; Dapper fills it by column name.
    private sealed class Row
    {
        public Guid ElevationId { get; set; }
        public string? TenantId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string SubjectId { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public string Justification { get; set; } = string.Empty;
        public DateTime OpenedAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public int Status { get; set; }
        public DateTime? ClosedAtUtc { get; set; }
        public string? CloseReason { get; set; }
        public DateTime? ExpiryRecordedAtUtc { get; set; }
        public int? ReviewOutcome { get; set; }
        public string? ReviewedByUserId { get; set; }
        public DateTime? ReviewedAtUtc { get; set; }
        public string? ReviewNote { get; set; }
    }
}
