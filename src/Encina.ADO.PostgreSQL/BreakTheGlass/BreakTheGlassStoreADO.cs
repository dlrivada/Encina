using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;
using Encina.Messaging;
using Encina.Messaging.Transactions;
using Encina.Security.Audit;
using Encina.Security.BreakTheGlass;
using LanguageExt;
using Npgsql;
using static LanguageExt.Prelude;

namespace Encina.ADO.PostgreSQL.BreakTheGlass;

/// <summary>
/// ADO.NET PostgreSQL implementation of <see cref="IBreakTheGlassStore"/>.
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
/// violation (the primary key) stays <c>break_glass.store_failed</c>.
/// </para>
/// <para>
/// A blank or whitespace tenant is stored and matched as no tenant. A provider failure becomes
/// <c>Left(break_glass.store_failed)</c>; the error carries no message of the provider.
/// </para>
/// </remarks>
public sealed class BreakTheGlassStoreADO : IBreakTheGlassStore
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
    /// Initializes a new instance of the <see cref="BreakTheGlassStoreADO"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="transactionAccessor">Optional accessor of the business transaction on the shared connection, so that every write can leave it (ADR-048).</param>
    /// <param name="tableName">The elevations table name (default: BreakTheGlassElevations).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tableName"/> is not a valid identifier.</exception>
    public BreakTheGlassStoreADO(
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
            using var command = Command(
                lease,
                $@"UPDATE {_table} SET ""Status"" = @Expired, ""CloseReason"" = @StaleReason, ""ClosedAtUtc"" = @OpenedAtUtc
                   WHERE {ActiveKeyPredicate} AND ""ExpiresAtUtc"" <= @OpenedAtUtc;
                   INSERT INTO {_table} ({Columns})
                   VALUES (@ElevationId, @TenantId, @UserId, @SubjectId, @Scope, @Justification, @OpenedAtUtc, @ExpiresAtUtc, @Status,
                           @ClosedAtUtc, @CloseReason, @ExpiryRecordedAtUtc, @ReviewOutcome, @ReviewedByUserId, @ReviewedAtUtc, @ReviewNote)");
            Add(command, "@Active", (int)BreakTheGlassStatus.Active);
            Add(command, "@Expired", (int)BreakTheGlassStatus.Expired);
            Add(command, "@StaleReason", StaleCloseReason);
            Add(command, "@TenantKey", tenantId ?? string.Empty);
            Add(command, "@ElevationId", elevation.ElevationId);
            Add(command, "@TenantId", tenantId);
            Add(command, "@UserId", elevation.UserId);
            Add(command, "@SubjectId", elevation.SubjectId);
            Add(command, "@Scope", elevation.Scope);
            Add(command, "@Justification", elevation.Justification);
            AddDate(command, "@OpenedAtUtc", elevation.OpenedAtUtc);
            AddDate(command, "@ExpiresAtUtc", elevation.ExpiresAtUtc);
            Add(command, "@Status", (int)elevation.Status);
            AddDate(command, "@ClosedAtUtc", elevation.ClosedAtUtc);
            Add(command, "@CloseReason", elevation.CloseReason);
            AddDate(command, "@ExpiryRecordedAtUtc", elevation.ExpiryRecordedAtUtc);
            Add(command, "@ReviewOutcome", (int?)elevation.ReviewOutcome);
            Add(command, "@ReviewedByUserId", elevation.ReviewedByUserId);
            AddDate(command, "@ReviewedAtUtc", elevation.ReviewedAtUtc);
            Add(command, "@ReviewNote", elevation.ReviewNote);

            try
            {
                await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
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
            using var command = Command(lease, $"SELECT {Columns} FROM {_table} WHERE \"ElevationId\" = @ElevationId AND {TenantFilter(tenantId)}");
            Add(command, "@ElevationId", elevationId);
            AddTenant(command, tenantId);

            var rows = await ReadAsync(command, ct).ConfigureAwait(false);
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
            using var command = Command(
                lease,
                $"SELECT {Columns} FROM {_table} WHERE {ActiveKeyPredicate} AND \"ExpiresAtUtc\" > @NowUtc ORDER BY \"OpenedAtUtc\" DESC LIMIT 1");
            Add(command, "@Active", (int)BreakTheGlassStatus.Active);
            Add(command, "@TenantKey", tenantId ?? string.Empty);
            Add(command, "@UserId", userId);
            Add(command, "@SubjectId", subjectId);
            Add(command, "@Scope", scope);
            AddDate(command, "@NowUtc", nowUtc);

            var rows = await ReadAsync(command, ct).ConfigureAwait(false);
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
            using var command = Command(
                lease,
                $@"UPDATE {_table} SET ""Status"" = @Status, ""CloseReason"" = @Reason, ""ClosedAtUtc"" = @ClosedAtUtc
                   WHERE ""ElevationId"" = @ElevationId AND {TenantFilter(tenantId)} AND ""Status"" = @Active");
            Add(command, "@Status", (int)newStatus);
            Add(command, "@Reason", reason);
            AddDate(command, "@ClosedAtUtc", closedAtUtc);
            Add(command, "@ElevationId", elevationId);
            AddTenant(command, tenantId);
            Add(command, "@Active", (int)BreakTheGlassStatus.Active);

            return Right<EncinaError, bool>(await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false) == 1);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<BreakTheGlassElevation>>> GetExpiredUnrecordedAsync(
        DateTimeOffset nowUtc, int batchSize, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);

        return RunAsync<IReadOnlyList<BreakTheGlassElevation>>(async (lease, ct) =>
        {
            using var command = Command(
                lease,
                $@"SELECT {Columns} FROM {_table}
                   WHERE (""Status"" = @Active AND ""ExpiresAtUtc"" <= @NowUtc) OR (""Status"" = @Expired AND ""ExpiryRecordedAtUtc"" IS NULL)
                   ORDER BY ""ExpiresAtUtc"", ""ElevationId""
                   LIMIT @BatchSize");
            Add(command, "@BatchSize", batchSize);
            Add(command, "@Active", (int)BreakTheGlassStatus.Active);
            Add(command, "@Expired", (int)BreakTheGlassStatus.Expired);
            AddDate(command, "@NowUtc", nowUtc);

            return Right<EncinaError, IReadOnlyList<BreakTheGlassElevation>>(await ReadAsync(command, ct).ConfigureAwait(false));
        }, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> MarkExpiryRecordedAsync(string? tenantId, Guid elevationId, DateTimeOffset recordedAtUtc, CancellationToken cancellationToken = default)
    {
        tenantId = Normalise(tenantId);

        return RunAsync<Unit>(async (lease, ct) =>
        {
            using var command = Command(
                lease,
                $"UPDATE {_table} SET \"ExpiryRecordedAtUtc\" = @RecordedAtUtc WHERE \"ElevationId\" = @ElevationId AND {TenantFilter(tenantId)} AND \"Status\" = @Expired");
            AddDate(command, "@RecordedAtUtc", recordedAtUtc);
            Add(command, "@ElevationId", elevationId);
            AddTenant(command, tenantId);
            Add(command, "@Expired", (int)BreakTheGlassStatus.Expired);

            await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
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
            using var command = Command(
                lease,
                $@"UPDATE {_table} SET ""ReviewOutcome"" = @Outcome, ""ReviewedByUserId"" = @ReviewerUserId, ""ReviewNote"" = @Note, ""ReviewedAtUtc"" = @ReviewedAtUtc
                   WHERE ""ElevationId"" = @ElevationId AND {TenantFilter(tenantId)} AND ""ReviewOutcome"" IS NULL");
            Add(command, "@Outcome", (int)outcome);
            Add(command, "@ReviewerUserId", reviewerUserId);
            Add(command, "@Note", note);
            AddDate(command, "@ReviewedAtUtc", reviewedAtUtc);
            Add(command, "@ElevationId", elevationId);
            AddTenant(command, tenantId);

            return Right<EncinaError, bool>(await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false) == 1);
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
            AppendQueryFilters(filter, query);

            using var count = Command(lease, $"SELECT COUNT(*) FROM {_table} WHERE {filter}");
            AddTenant(count, tenantId);
            BindQueryFilters(count, query);
            var total = Convert.ToInt32(await count.ExecuteScalarAsync(ct).ConfigureAwait(false), CultureInfo.InvariantCulture);

            using var page = Command(
                lease,
                $@"SELECT {Columns} FROM {_table} WHERE {filter}
                   ORDER BY ""OpenedAtUtc"" ASC, ""ElevationId"" ASC
                   LIMIT @PageSize OFFSET @Offset");
            AddTenant(page, tenantId);
            BindQueryFilters(page, query);
            Add(page, "@Offset", (pageNumber - 1) * pageSize);
            Add(page, "@PageSize", pageSize);

            var items = await ReadAsync(page, ct).ConfigureAwait(false);
            return Right<EncinaError, PagedResult<BreakTheGlassElevation>>(PagedResult<BreakTheGlassElevation>.Create(items, total, pageNumber, pageSize));
        }, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, int>> PurgeAsync(DateTimeOffset reviewedBeforeUtc, CancellationToken cancellationToken = default)
    {
        return RunAsync<int>(async (lease, ct) =>
        {
            // Closed and reviewed; an Expired row also needs its expiry recorded, or the sweeper would never audit it.
            using var command = Command(
                lease,
                $@"DELETE FROM {_table}
                   WHERE ""Status"" <> @Active AND ""ReviewedAtUtc"" < @CutoffUtc AND (""Status"" <> @Expired OR ""ExpiryRecordedAtUtc"" IS NOT NULL)");
            Add(command, "@Active", (int)BreakTheGlassStatus.Active);
            Add(command, "@Expired", (int)BreakTheGlassStatus.Expired);
            AddDate(command, "@CutoffUtc", reviewedBeforeUtc);

            return Right<EncinaError, int>(await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false));
        }, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> ProbeAsync(CancellationToken cancellationToken = default)
    {
        return RunAsync<Unit>(async (lease, ct) =>
        {
            using var command = Command(lease, "SELECT 1");
            await command.ExecuteScalarAsync(ct).ConfigureAwait(false);
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

    private static void AddTenant(DbCommand command, string? tenantId)
    {
        if (tenantId is not null)
        {
            Add(command, "@TenantId", tenantId);
        }
    }

    // Appends the optional filters of the review queue (the same text for the count and the page).
    private static void AppendQueryFilters(StringBuilder filter, BreakTheGlassReviewQuery query)
    {
        if (query.Scope is not null)
        {
            filter.Append(" AND \"Scope\" = @Scope");
        }

        if (query.Status is not null)
        {
            filter.Append(" AND \"Status\" = @FilterStatus");
        }

        if (query.OpenedFromUtc is not null)
        {
            filter.Append(" AND \"OpenedAtUtc\" >= @OpenedFromUtc");
        }

        if (query.OpenedToUtc is not null)
        {
            filter.Append(" AND \"OpenedAtUtc\" < @OpenedToUtc");
        }
    }

    // Binds the parameters of the filters AppendQueryFilters added.
    private static void BindQueryFilters(DbCommand command, BreakTheGlassReviewQuery query)
    {
        if (query.Scope is not null)
        {
            Add(command, "@Scope", query.Scope);
        }

        if (query.Status is not null)
        {
            Add(command, "@FilterStatus", (int)query.Status.Value);
        }

        if (query.OpenedFromUtc is not null)
        {
            AddDate(command, "@OpenedFromUtc", query.OpenedFromUtc);
        }

        if (query.OpenedToUtc is not null)
        {
            AddDate(command, "@OpenedToUtc", query.OpenedToUtc);
        }
    }

    private static DbCommand Command(DbLease lease, string sql)
    {
        var command = lease.CreateCommand();
        if (command is not DbCommand dbCommand)
        {
            command.Dispose();
            throw new InvalidOperationException("The connection must create commands that derive from DbCommand, so that no call needs a synchronous fallback.");
        }

        dbCommand.CommandText = sql;
        return dbCommand;
    }

    private static void Add(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static void AddDate(DbCommand command, string name, DateTimeOffset? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = DbType.DateTimeOffset;
        parameter.Value = value is null ? DBNull.Value : value.Value.ToUniversalTime();
        command.Parameters.Add(parameter);
    }

    private static async Task<List<BreakTheGlassElevation>> ReadAsync(DbCommand command, CancellationToken cancellationToken)
    {
        var rows = new List<BreakTheGlassElevation>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            rows.Add(Map(reader));
        }

        return rows;
    }

    // Reads the columns in the order of Columns.
    private static BreakTheGlassElevation Map(DbDataReader reader) => new()
    {
        ElevationId = reader.GetGuid(0),
        TenantId = GetString(reader, 1),
        UserId = reader.GetString(2),
        SubjectId = reader.GetString(3),
        Scope = reader.GetString(4),
        Justification = reader.GetString(5),
        OpenedAtUtc = ToUtc(reader.GetDateTime(6)),
        ExpiresAtUtc = ToUtc(reader.GetDateTime(7)),
        Status = (BreakTheGlassStatus)reader.GetInt32(8),
        ClosedAtUtc = GetDate(reader, 9),
        CloseReason = GetString(reader, 10),
        ExpiryRecordedAtUtc = GetDate(reader, 11),
        ReviewOutcome = reader.IsDBNull(12) ? null : (BreakTheGlassReviewOutcome)reader.GetInt32(12),
        ReviewedByUserId = GetString(reader, 13),
        ReviewedAtUtc = GetDate(reader, 14),
        ReviewNote = GetString(reader, 15)
    };

    private static string? GetString(DbDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static DateTimeOffset? GetDate(DbDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : ToUtc(reader.GetDateTime(ordinal));

    private static DateTimeOffset ToUtc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
