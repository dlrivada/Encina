using System.Data;
using System.Data.Common;
using Dapper;
using Encina.Messaging;
using Encina.Messaging.DeadLetter;
using LanguageExt;
using Npgsql;
using static LanguageExt.Prelude;

namespace Encina.Dapper.PostgreSQL.DeadLetter;

/// <summary>
/// Dapper PostgreSQL implementation of <see cref="IDeadLetterStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Every call goes through <see cref="CommandDefinition"/> with the caller's <see cref="CancellationToken"/>.
/// "Now" comes from the injected <see cref="TimeProvider"/>. The unique <c>(SourcePattern, SourceMessageId)</c>
/// index (SQLSTATE 23505) turns a second capture of the same source message into <c>Right(false)</c>.
/// Table and column identifiers are quoted PascalCase, as created by <c>029_CreateDeadLetterMessagesTable.sql</c>.
/// </para>
/// <para>
/// The connection must derive from <see cref="DbConnection"/>, so that it is opened asynchronously.
/// </para>
/// </remarks>
public sealed class DeadLetterStoreDapper : IDeadLetterStore
{
    private const string Columns =
        "\"Id\", \"RequestType\", \"RequestContent\", \"ErrorCode\", \"ExceptionType\", \"ExceptionStackTrace\", \"CorrelationId\", " +
        "\"SourcePattern\", \"SourceMessageId\", \"TenantId\", \"TotalRetryAttempts\", \"FirstFailedAtUtc\", \"DeadLetteredAtUtc\", " +
        "\"ExpiresAtUtc\", \"ReplayClaimedAtUtc\", \"ReplayedAtUtc\", \"ReplayResult\"";

    private readonly DbConnection _connection;
    private readonly string _table;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeadLetterStoreDapper"/> class.
    /// </summary>
    /// <param name="connection">The database connection; it must derive from <see cref="DbConnection"/>.</param>
    /// <param name="tableName">The dead letter table name (default: DeadLetterMessages); it is quoted in SQL.</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="connection"/> is not a <see cref="DbConnection"/> or <paramref name="tableName"/> is invalid.
    /// </exception>
    public DeadLetterStoreDapper(
        IDbConnection connection,
        string tableName = "DeadLetterMessages",
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection as DbConnection
            ?? throw new ArgumentException("The connection must derive from DbConnection to run asynchronously.", nameof(connection));
        _table = QuoteTable(SqlIdentifierValidator.ValidateTableName(tableName));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;

    // "schema.table" or "[schema].[table]" becomes "schema"."table"; PostgreSQL folds unquoted names to lower case.
    private static string QuoteTable(string validatedName)
        => string.Join('.', validatedName.Split('.').Select(part => "\"" + part.Trim('[', ']') + "\""));

    /// <inheritdoc />
    public async Task<Either<EncinaError, bool>> AddAsync(IDeadLetterMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        return await EitherHelpers.TryAsync(async () =>
        {
            var sql = $@"INSERT INTO {_table} ({Columns})
                   VALUES (@Id, @RequestType, @RequestContent, @ErrorCode, @ExceptionType, @ExceptionStackTrace, @CorrelationId,
                           @SourcePattern, @SourceMessageId, @TenantId, @TotalRetryAttempts, @FirstFailedAtUtc, @DeadLetteredAtUtc,
                           @ExpiresAtUtc, @ReplayClaimedAtUtc, @ReplayedAtUtc, @ReplayResult)";
            try
            {
                return await ExecuteAsync(sql, ToParameters(message), cancellationToken).ConfigureAwait(false) == 1;
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                return false;
            }
        }, DeadLetterErrorCodes.StoreFailed).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Option<IDeadLetterMessage>>> GetAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        ValidateMessageId(messageId);

        return await EitherHelpers.TryAsync(async () =>
        {
            var parameters = new DynamicParameters();
            parameters.Add("Id", messageId);

            var rows = await QueryAsync($"SELECT {Columns} FROM {_table} WHERE \"Id\" = @Id", parameters, cancellationToken).ConfigureAwait(false);
            return rows.Count == 0 ? Option<IDeadLetterMessage>.None : Some(rows[0]);
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
            var direction = newestFirst ? "DESC" : "ASC";
            var parameters = new DynamicParameters();
            var where = BuildWhere(parameters, filter);
            parameters.Add("Skip", skip);
            parameters.Add("Take", take);

            return await QueryAsync(
                $@"SELECT {Columns} FROM {_table}{where}
                   ORDER BY ""DeadLetteredAtUtc"" {direction}, ""Id"" {direction}
                   LIMIT @Take OFFSET @Skip",
                parameters,
                cancellationToken).ConfigureAwait(false);
        }, DeadLetterErrorCodes.QueryFailed).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, int>> GetCountAsync(DeadLetterFilter? filter = null, CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var parameters = new DynamicParameters();
            var sql = $"SELECT COUNT(*) FROM {_table}{BuildWhere(parameters, filter)}";

            await OpenAsync(cancellationToken).ConfigureAwait(false);
            return await _connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)).ConfigureAwait(false);
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
            var parameters = new DynamicParameters();
            parameters.Add("Id", messageId);
            parameters.Add("NowUtc", AsUtc(UtcNow()));
            parameters.Add("ClaimExpiredBeforeUtc", AsUtc(claimExpiredBeforeUtc));

            return await ExecuteAsync(
                $@"UPDATE {_table} SET ""ReplayClaimedAtUtc"" = @NowUtc
                   WHERE ""Id"" = @Id AND ""ReplayedAtUtc"" IS NULL
                     AND (""ReplayClaimedAtUtc"" IS NULL OR ""ReplayClaimedAtUtc"" <= @ClaimExpiredBeforeUtc)",
                parameters,
                cancellationToken).ConfigureAwait(false) == 1;
        }, DeadLetterErrorCodes.ClaimFailed).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, bool>> MarkAsReplayedAsync(Guid messageId, string replayResult, CancellationToken cancellationToken = default)
    {
        ValidateMessageId(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(replayResult);

        return await EitherHelpers.TryAsync(async () =>
        {
            var parameters = new DynamicParameters();
            parameters.Add("Id", messageId);
            parameters.Add("NowUtc", AsUtc(UtcNow()));
            parameters.Add("ReplayResult", replayResult);

            return await ExecuteAsync(
                $@"UPDATE {_table} SET ""ReplayedAtUtc"" = @NowUtc, ""ReplayResult"" = @ReplayResult
                   WHERE ""Id"" = @Id AND ""ReplayedAtUtc"" IS NULL",
                parameters,
                cancellationToken).ConfigureAwait(false) == 1;
        }, DeadLetterErrorCodes.MarkReplayedFailed).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, bool>> DeleteAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        ValidateMessageId(messageId);

        return await EitherHelpers.TryAsync(async () =>
        {
            var parameters = new DynamicParameters();
            parameters.Add("Id", messageId);

            return await ExecuteAsync($"DELETE FROM {_table} WHERE \"Id\" = @Id", parameters, cancellationToken).ConfigureAwait(false) > 0;
        }, DeadLetterErrorCodes.DeleteFailed).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, int>> DeleteManyAsync(DeadLetterFilter filter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        return await EitherHelpers.TryAsync(async () =>
        {
            var parameters = new DynamicParameters();
            var sql = $"DELETE FROM {_table}{BuildWhere(parameters, filter)}";

            return await ExecuteAsync(sql, parameters, cancellationToken).ConfigureAwait(false);
        }, DeadLetterErrorCodes.DeleteFailed).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, int>> DeleteExpiredAsync(CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var parameters = new DynamicParameters();
            parameters.Add("NowUtc", AsUtc(UtcNow()));

            return await ExecuteAsync(
                $"DELETE FROM {_table} WHERE \"ExpiresAtUtc\" IS NOT NULL AND \"ExpiresAtUtc\" <= @NowUtc",
                parameters,
                cancellationToken).ConfigureAwait(false);
        }, DeadLetterErrorCodes.CleanupFailed).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, Unit>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Dapper executes SQL immediately, no need for SaveChanges
        return Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
    }

    private static void ValidateMessageId(Guid messageId)
        => ArgumentOutOfRangeException.ThrowIfEqual(messageId, Guid.Empty);

    private async Task OpenAsync(CancellationToken cancellationToken)
    {
        if (_connection.State != ConnectionState.Open)
        {
            await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<int> ExecuteAsync(string sql, DynamicParameters parameters, CancellationToken cancellationToken)
    {
        await OpenAsync(cancellationToken).ConfigureAwait(false);
        return await _connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private async Task<List<IDeadLetterMessage>> QueryAsync(string sql, DynamicParameters parameters, CancellationToken cancellationToken)
    {
        await OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await _connection.QueryAsync<DeadLetterMessage>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows.Select(row => (IDeadLetterMessage)MarkUtc(row)).ToList();
    }

    private static string BuildWhere(DynamicParameters parameters, DeadLetterFilter? filter)
    {
        if (filter is null)
        {
            return string.Empty;
        }

        var conditions = new List<string>();
        AddEquals(conditions, parameters, "SourcePattern", filter.SourcePattern);
        AddEquals(conditions, parameters, "RequestType", filter.RequestType);
        AddEquals(conditions, parameters, "ErrorCode", filter.ErrorCode);
        AddEquals(conditions, parameters, "CorrelationId", filter.CorrelationId);
        AddEquals(conditions, parameters, "TenantId", filter.TenantId);
        AddEquals(conditions, parameters, "SourceMessageId", filter.SourceMessageId);
        AddReplayState(conditions, filter.ExcludeReplayed);
        AddRange(conditions, parameters, "\"DeadLetteredAtUtc\" >= @DeadLetteredAfterUtc", "DeadLetteredAfterUtc", filter.DeadLetteredAfterUtc);
        AddRange(conditions, parameters, "\"DeadLetteredAtUtc\" <= @DeadLetteredBeforeUtc", "DeadLetteredBeforeUtc", filter.DeadLetteredBeforeUtc);
        AddRange(conditions, parameters, "\"ExpiresAtUtc\" IS NOT NULL AND \"ExpiresAtUtc\" <= @ExpiresAtOrBeforeUtc", "ExpiresAtOrBeforeUtc", filter.ExpiresAtOrBeforeUtc);

        return conditions.Count == 0 ? string.Empty : " WHERE " + string.Join(" AND ", conditions);
    }

    // A null or empty filter value matches everything (the contract shared with the fake store).
    private static void AddEquals(List<string> conditions, DynamicParameters parameters, string column, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        conditions.Add($"\"{column}\" = @{column}");
        parameters.Add(column, value);
    }

    private static void AddReplayState(List<string> conditions, bool? excludeReplayed)
    {
        if (excludeReplayed is { } exclude)
        {
            conditions.Add(exclude ? "\"ReplayedAtUtc\" IS NULL" : "\"ReplayedAtUtc\" IS NOT NULL");
        }
    }

    private static void AddRange(List<string> conditions, DynamicParameters parameters, string condition, string name, DateTime? value)
    {
        if (value is not { } instant)
        {
            return;
        }

        conditions.Add(condition);
        parameters.Add(name, AsUtc(instant));
    }

    private static DynamicParameters ToParameters(IDeadLetterMessage message)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Id", message.Id);
        parameters.Add("RequestType", message.RequestType);
        parameters.Add("RequestContent", message.RequestContent);
        parameters.Add("ErrorCode", message.ErrorCode);
        parameters.Add("ExceptionType", message.ExceptionType);
        parameters.Add("ExceptionStackTrace", message.ExceptionStackTrace);
        parameters.Add("CorrelationId", message.CorrelationId);
        parameters.Add("SourcePattern", message.SourcePattern);
        parameters.Add("SourceMessageId", message.SourceMessageId);
        parameters.Add("TenantId", message.TenantId);
        parameters.Add("TotalRetryAttempts", message.TotalRetryAttempts);
        parameters.Add("FirstFailedAtUtc", AsUtc(message.FirstFailedAtUtc));
        parameters.Add("DeadLetteredAtUtc", AsUtc(message.DeadLetteredAtUtc));
        parameters.Add("ExpiresAtUtc", AsUtc(message.ExpiresAtUtc));
        parameters.Add("ReplayClaimedAtUtc", AsUtc(message.ReplayClaimedAtUtc));
        parameters.Add("ReplayedAtUtc", AsUtc(message.ReplayedAtUtc));
        parameters.Add("ReplayResult", message.ReplayResult);
        return parameters;
    }

    // Npgsql writes a DateTime of Kind.Utc to timestamptz and rejects Kind.Unspecified, so instants are normalized.
    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static DateTime? AsUtc(DateTime? value) => value is { } instant ? AsUtc(instant) : null;

    private static DeadLetterMessage MarkUtc(DeadLetterMessage row)
    {
        row.FirstFailedAtUtc = AsUtc(row.FirstFailedAtUtc);
        row.DeadLetteredAtUtc = AsUtc(row.DeadLetteredAtUtc);
        row.ExpiresAtUtc = AsUtc(row.ExpiresAtUtc);
        row.ReplayClaimedAtUtc = AsUtc(row.ReplayClaimedAtUtc);
        row.ReplayedAtUtc = AsUtc(row.ReplayedAtUtc);
        return row;
    }
}
