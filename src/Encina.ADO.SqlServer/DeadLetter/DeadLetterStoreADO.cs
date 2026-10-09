using System.Data;
using System.Data.Common;
using Encina.Messaging;
using Encina.Messaging.DeadLetter;
using LanguageExt;
using Microsoft.Data.SqlClient;
using static LanguageExt.Prelude;

namespace Encina.ADO.SqlServer.DeadLetter;

/// <summary>
/// ADO.NET SQL Server implementation of <see cref="IDeadLetterStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Every statement is parameterized and runs asynchronously with the caller's
/// <see cref="CancellationToken"/>. "Now" comes from the injected <see cref="TimeProvider"/>.
/// The unique <c>(SourcePattern, SourceMessageId)</c> index (SQL Server errors 2627 and 2601) turns a
/// second capture of the same source message into <c>Right(false)</c>.
/// </para>
/// <para>
/// The connection must derive from <see cref="DbConnection"/>, so that no call needs a synchronous fallback.
/// </para>
/// </remarks>
public sealed class DeadLetterStoreADO : IDeadLetterStore
{
    private const string Columns =
        "Id, RequestType, RequestContent, ErrorCode, ExceptionType, ExceptionStackTrace, CorrelationId, " +
        "SourcePattern, SourceMessageId, TenantId, TotalRetryAttempts, FirstFailedAtUtc, DeadLetteredAtUtc, " +
        "ExpiresAtUtc, ReplayClaimedAtUtc, ReplayedAtUtc, ReplayResult";

    private readonly DbConnection _connection;
    private readonly string _tableName;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeadLetterStoreADO"/> class.
    /// </summary>
    /// <param name="connection">The database connection; it must derive from <see cref="DbConnection"/>.</param>
    /// <param name="tableName">The dead letter table name (default: DeadLetterMessages).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="connection"/> is not a <see cref="DbConnection"/> or <paramref name="tableName"/> is invalid.
    /// </exception>
    public DeadLetterStoreADO(
        IDbConnection connection,
        string tableName = "DeadLetterMessages",
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection as DbConnection
            ?? throw new ArgumentException("The connection must derive from DbConnection to run asynchronously.", nameof(connection));
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
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
            await using var command = await CreateCommandAsync(
                $@"INSERT INTO {_tableName} ({Columns})
                   VALUES (@Id, @RequestType, @RequestContent, @ErrorCode, @ExceptionType, @ExceptionStackTrace, @CorrelationId,
                           @SourcePattern, @SourceMessageId, @TenantId, @TotalRetryAttempts, @FirstFailedAtUtc, @DeadLetteredAtUtc,
                           @ExpiresAtUtc, @ReplayClaimedAtUtc, @ReplayedAtUtc, @ReplayResult)",
                cancellationToken).ConfigureAwait(false);
            BindMessage(command, message);

            try
            {
                return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) == 1;
            }
            catch (SqlException ex) when (ex.Number is 2627 or 2601)
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
            await using var command = await CreateCommandAsync(
                $"SELECT {Columns} FROM {_tableName} WHERE Id = @Id", cancellationToken).ConfigureAwait(false);
            AddParameter(command, "@Id", messageId);

            var rows = await ReadAllAsync(command, cancellationToken).ConfigureAwait(false);
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
            await using var command = await CreateCommandAsync(string.Empty, cancellationToken).ConfigureAwait(false);
            var where = BuildWhere(command, filter);
            command.CommandText =
                $@"SELECT {Columns} FROM {_tableName}{where}
                   ORDER BY DeadLetteredAtUtc {direction}, Id {direction}
                   OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
            AddParameter(command, "@Skip", skip);
            AddParameter(command, "@Take", take);

            return await ReadAllAsync(command, cancellationToken).ConfigureAwait(false);
        }, DeadLetterErrorCodes.QueryFailed).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, int>> GetCountAsync(DeadLetterFilter? filter = null, CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            await using var command = await CreateCommandAsync(string.Empty, cancellationToken).ConfigureAwait(false);
            command.CommandText = $"SELECT COUNT(*) FROM {_tableName}{BuildWhere(command, filter)}";

            var scalar = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return Convert.ToInt32(scalar, System.Globalization.CultureInfo.InvariantCulture);
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
            await using var command = await CreateCommandAsync(
                $@"UPDATE {_tableName} SET ReplayClaimedAtUtc = @NowUtc
                   WHERE Id = @Id AND ReplayedAtUtc IS NULL
                     AND (ReplayClaimedAtUtc IS NULL OR ReplayClaimedAtUtc <= @ClaimExpiredBeforeUtc)",
                cancellationToken).ConfigureAwait(false);
            AddParameter(command, "@Id", messageId);
            AddParameter(command, "@NowUtc", UtcNow());
            AddParameter(command, "@ClaimExpiredBeforeUtc", claimExpiredBeforeUtc);

            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) == 1;
        }, DeadLetterErrorCodes.ClaimFailed).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, bool>> MarkAsReplayedAsync(Guid messageId, string replayResult, CancellationToken cancellationToken = default)
    {
        ValidateMessageId(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(replayResult);

        return await EitherHelpers.TryAsync(async () =>
        {
            await using var command = await CreateCommandAsync(
                $@"UPDATE {_tableName} SET ReplayedAtUtc = @NowUtc, ReplayResult = @ReplayResult
                   WHERE Id = @Id AND ReplayedAtUtc IS NULL",
                cancellationToken).ConfigureAwait(false);
            AddParameter(command, "@Id", messageId);
            AddParameter(command, "@NowUtc", UtcNow());
            AddParameter(command, "@ReplayResult", replayResult);

            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) == 1;
        }, DeadLetterErrorCodes.MarkReplayedFailed).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, bool>> DeleteAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        ValidateMessageId(messageId);

        return await EitherHelpers.TryAsync(async () =>
        {
            await using var command = await CreateCommandAsync(
                $"DELETE FROM {_tableName} WHERE Id = @Id", cancellationToken).ConfigureAwait(false);
            AddParameter(command, "@Id", messageId);

            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) > 0;
        }, DeadLetterErrorCodes.DeleteFailed).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, int>> DeleteManyAsync(DeadLetterFilter filter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        return await EitherHelpers.TryAsync(async () =>
        {
            await using var command = await CreateCommandAsync(string.Empty, cancellationToken).ConfigureAwait(false);
            command.CommandText = $"DELETE FROM {_tableName}{BuildWhere(command, filter)}";

            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }, DeadLetterErrorCodes.DeleteFailed).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, int>> DeleteExpiredAsync(CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            await using var command = await CreateCommandAsync(
                $"DELETE FROM {_tableName} WHERE ExpiresAtUtc IS NOT NULL AND ExpiresAtUtc <= @NowUtc",
                cancellationToken).ConfigureAwait(false);
            AddParameter(command, "@NowUtc", UtcNow());

            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }, DeadLetterErrorCodes.CleanupFailed).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, Unit>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // ADO.NET executes SQL immediately, no need for SaveChanges
        return Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
    }

    private static void ValidateMessageId(Guid messageId)
        => ArgumentOutOfRangeException.ThrowIfEqual(messageId, Guid.Empty);

    private async Task<DbCommand> CreateCommandAsync(string sql, CancellationToken cancellationToken)
    {
        if (_connection.State != ConnectionState.Open)
        {
            await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        var command = _connection.CreateCommand();
        command.CommandText = sql;
        return command;
    }

    private static string BuildWhere(DbCommand command, DeadLetterFilter? filter)
    {
        if (filter is null)
        {
            return string.Empty;
        }

        var conditions = new List<string>();
        AddEquals(conditions, command, "SourcePattern", filter.SourcePattern);
        AddEquals(conditions, command, "RequestType", filter.RequestType);
        AddEquals(conditions, command, "ErrorCode", filter.ErrorCode);
        AddEquals(conditions, command, "CorrelationId", filter.CorrelationId);
        AddEquals(conditions, command, "TenantId", filter.TenantId);
        AddEquals(conditions, command, "SourceMessageId", filter.SourceMessageId);
        AddReplayState(conditions, filter.ExcludeReplayed);
        AddRange(conditions, command, "DeadLetteredAtUtc >= @DeadLetteredAfterUtc", "@DeadLetteredAfterUtc", filter.DeadLetteredAfterUtc);
        AddRange(conditions, command, "DeadLetteredAtUtc <= @DeadLetteredBeforeUtc", "@DeadLetteredBeforeUtc", filter.DeadLetteredBeforeUtc);
        AddRange(conditions, command, "ExpiresAtUtc IS NOT NULL AND ExpiresAtUtc <= @ExpiresAtOrBeforeUtc", "@ExpiresAtOrBeforeUtc", filter.ExpiresAtOrBeforeUtc);

        return conditions.Count == 0 ? string.Empty : " WHERE " + string.Join(" AND ", conditions);
    }

    // A null or empty filter value matches everything (the contract shared with the fake store).
    private static void AddEquals(List<string> conditions, DbCommand command, string column, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        conditions.Add($"{column} = @{column}");
        AddParameter(command, "@" + column, value);
    }

    private static void AddReplayState(List<string> conditions, bool? excludeReplayed)
    {
        if (excludeReplayed is { } exclude)
        {
            conditions.Add(exclude ? "ReplayedAtUtc IS NULL" : "ReplayedAtUtc IS NOT NULL");
        }
    }

    private static void AddRange(List<string> conditions, DbCommand command, string condition, string parameter, DateTime? value)
    {
        if (value is not { } instant)
        {
            return;
        }

        conditions.Add(condition);
        AddParameter(command, parameter, instant);
    }

    private static void BindMessage(DbCommand command, IDeadLetterMessage message)
    {
        AddParameter(command, "@Id", message.Id);
        AddParameter(command, "@RequestType", message.RequestType);
        AddParameter(command, "@RequestContent", message.RequestContent);
        AddParameter(command, "@ErrorCode", message.ErrorCode);
        AddParameter(command, "@ExceptionType", message.ExceptionType);
        AddParameter(command, "@ExceptionStackTrace", message.ExceptionStackTrace);
        AddParameter(command, "@CorrelationId", message.CorrelationId);
        AddParameter(command, "@SourcePattern", message.SourcePattern);
        AddParameter(command, "@SourceMessageId", message.SourceMessageId);
        AddParameter(command, "@TenantId", message.TenantId);
        AddParameter(command, "@TotalRetryAttempts", message.TotalRetryAttempts);
        AddParameter(command, "@FirstFailedAtUtc", message.FirstFailedAtUtc);
        AddParameter(command, "@DeadLetteredAtUtc", message.DeadLetteredAtUtc);
        AddParameter(command, "@ExpiresAtUtc", message.ExpiresAtUtc);
        AddParameter(command, "@ReplayClaimedAtUtc", message.ReplayClaimedAtUtc);
        AddParameter(command, "@ReplayedAtUtc", message.ReplayedAtUtc);
        AddParameter(command, "@ReplayResult", message.ReplayResult);
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        if (value is DateTime)
        {
            parameter.DbType = DbType.DateTime2;
        }

        command.Parameters.Add(parameter);
    }

    private static async Task<List<IDeadLetterMessage>> ReadAllAsync(DbCommand command, CancellationToken cancellationToken)
    {
        var messages = new List<IDeadLetterMessage>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            messages.Add(Map(reader));
        }

        return messages;
    }

    private static DeadLetterMessage Map(DbDataReader reader) => new()
    {
        Id = reader.GetGuid(reader.GetOrdinal("Id")),
        RequestType = reader.GetString(reader.GetOrdinal("RequestType")),
        RequestContent = reader.GetString(reader.GetOrdinal("RequestContent")),
        ErrorCode = reader.GetString(reader.GetOrdinal("ErrorCode")),
        ExceptionType = GetString(reader, "ExceptionType"),
        ExceptionStackTrace = GetString(reader, "ExceptionStackTrace"),
        CorrelationId = GetString(reader, "CorrelationId"),
        SourcePattern = reader.GetString(reader.GetOrdinal("SourcePattern")),
        SourceMessageId = reader.GetString(reader.GetOrdinal("SourceMessageId")),
        TenantId = GetString(reader, "TenantId"),
        TotalRetryAttempts = reader.GetInt32(reader.GetOrdinal("TotalRetryAttempts")),
        FirstFailedAtUtc = GetUtc(reader, "FirstFailedAtUtc"),
        DeadLetteredAtUtc = GetUtc(reader, "DeadLetteredAtUtc"),
        ExpiresAtUtc = GetNullableUtc(reader, "ExpiresAtUtc"),
        ReplayClaimedAtUtc = GetNullableUtc(reader, "ReplayClaimedAtUtc"),
        ReplayedAtUtc = GetNullableUtc(reader, "ReplayedAtUtc"),
        ReplayResult = GetString(reader, "ReplayResult")
    };

    private static string? GetString(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static DateTime GetUtc(DbDataReader reader, string column)
        => DateTime.SpecifyKind(reader.GetDateTime(reader.GetOrdinal(column)), DateTimeKind.Utc);

    private static DateTime? GetNullableUtc(DbDataReader reader, string column)
        => reader.IsDBNull(reader.GetOrdinal(column)) ? null : GetUtc(reader, column);
}
