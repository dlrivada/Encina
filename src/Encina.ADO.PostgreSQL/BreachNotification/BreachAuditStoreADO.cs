using System.Data;
using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.Model;
using Encina.Messaging;
using LanguageExt;
using Npgsql;
using static LanguageExt.Prelude;

namespace Encina.ADO.PostgreSQL.BreachNotification;

/// <summary>
/// ADO.NET implementation of <see cref="IBreachAuditStore"/> for PostgreSQL.
/// Provides immutable audit trail for GDPR Article 33(5) accountability.
/// </summary>
/// <remarks>
/// <para>
/// PostgreSQL-specific considerations:
/// <list type="bullet">
/// <item><description>DateTimeOffset values are stored as TIMESTAMPTZ via <c>.UtcDateTime</c>.</description></item>
/// <item><description>DateTimeOffset values are read back using <c>new DateTimeOffset(reader.GetDateTime(ord), TimeSpan.Zero)</c>.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class BreachAuditStoreADO : IBreachAuditStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;

    /// <summary>
    /// Initializes a new instance of the <see cref="BreachAuditStoreADO"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The audit entries table name (default: BreachAuditEntries).</param>
    public BreachAuditStoreADO(
        IDbConnection connection,
        string tableName = "BreachAuditEntries")
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RecordAsync(
        BreachAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            var entity = BreachAuditEntryMapper.ToEntity(entry);
            var sql = $@"
                INSERT INTO {_tableName}
                (Id, BreachId, Action, Detail, PerformedByUserId, OccurredAtUtc)
                VALUES
                (@Id, @BreachId, @Action, @Detail, @PerformedByUserId, @OccurredAtUtc)";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", entity.Id);
            AddParameter(command, "@BreachId", entity.BreachId);
            AddParameter(command, "@Action", entity.Action);
            AddParameter(command, "@Detail", entity.Detail);
            AddParameter(command, "@PerformedByUserId", entity.PerformedByUserId);
            AddParameter(command, "@OccurredAtUtc", entity.OccurredAtUtc.UtcDateTime);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "breachnotification.audit_store_error",
                message: $"Failed to record breach audit entry: {ex.Message}",
                details: new Dictionary<string, object?> { ["breachId"] = entry.BreachId, ["action"] = entry.Action }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<BreachAuditEntry>>> GetAuditTrailAsync(
        string breachId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(breachId);

        try
        {
            var sql = $@"
                SELECT Id, BreachId, Action, Detail, PerformedByUserId, OccurredAtUtc
                FROM {_tableName}
                WHERE BreachId = @BreachId
                ORDER BY OccurredAtUtc ASC";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@BreachId", breachId);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var entries = new List<BreachAuditEntry>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToEntity(reader);
                entries.Add(BreachAuditEntryMapper.ToDomain(entity));
            }

            return Right<EncinaError, IReadOnlyList<BreachAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "breachnotification.audit_store_error",
                message: $"Failed to get breach audit trail: {ex.Message}",
                details: new Dictionary<string, object?> { ["breachId"] = breachId }));
        }
    }

    private static BreachAuditEntryEntity MapToEntity(IDataReader reader)
    {
        var detailOrd = reader.GetOrdinal("Detail");
        var performedByOrd = reader.GetOrdinal("PerformedByUserId");

        return new BreachAuditEntryEntity
        {
            Id = reader.GetString(reader.GetOrdinal("Id")),
            BreachId = reader.GetString(reader.GetOrdinal("BreachId")),
            Action = reader.GetString(reader.GetOrdinal("Action")),
            Detail = reader.IsDBNull(detailOrd) ? null : reader.GetString(detailOrd),
            PerformedByUserId = reader.IsDBNull(performedByOrd) ? null : reader.GetString(performedByOrd),
            OccurredAtUtc = new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("OccurredAtUtc")), TimeSpan.Zero)
        };
    }

    private static void AddParameter(IDbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static Task OpenConnectionAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    private static async Task<IDataReader> ExecuteReaderAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is NpgsqlCommand npgsqlCommand)
            return await npgsqlCommand.ExecuteReaderAsync(cancellationToken);

        return await Task.Run(command.ExecuteReader, cancellationToken);
    }

    private static async Task<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is NpgsqlCommand npgsqlCommand)
            return await npgsqlCommand.ExecuteNonQueryAsync(cancellationToken);

        return await Task.Run(command.ExecuteNonQuery, cancellationToken);
    }

    private static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is NpgsqlDataReader npgsqlReader)
            return await npgsqlReader.ReadAsync(cancellationToken);

        return await Task.Run(reader.Read, cancellationToken);
    }
}
