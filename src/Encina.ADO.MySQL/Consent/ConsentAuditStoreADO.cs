using System.Data;
using System.Text.Json;
using Encina.Compliance.Consent;
using Encina.Messaging;
using LanguageExt;
using MySqlConnector;
using static LanguageExt.Prelude;

namespace Encina.ADO.MySQL.Consent;

/// <summary>
/// ADO.NET implementation of <see cref="IConsentAuditStore"/> for MySQL.
/// Provides immutable audit trail for GDPR Article 7(1) demonstrability.
/// </summary>
public sealed class ConsentAuditStoreADO : IConsentAuditStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsentAuditStoreADO"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The audit entries table name (default: ConsentAuditEntries).</param>
    /// <param name="timeProvider">The time provider (unused, kept for constructor parity across providers).</param>
    public ConsentAuditStoreADO(
        IDbConnection connection,
        string tableName = "ConsentAuditEntries",
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
        _ = timeProvider; // Kept for constructor parity across providers
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RecordAsync(
        ConsentAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            var sql = $@"
                INSERT INTO {_tableName}
                (Id, SubjectId, Purpose, Action, OccurredAtUtc, PerformedBy, IpAddress, Metadata)
                VALUES
                (@Id, @SubjectId, @Purpose, @Action, @OccurredAtUtc, @PerformedBy, @IpAddress, @Metadata)";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", entry.Id);
            AddParameter(command, "@SubjectId", entry.SubjectId);
            AddParameter(command, "@Purpose", entry.Purpose);
            AddParameter(command, "@Action", (int)entry.Action);
            AddParameter(command, "@OccurredAtUtc", entry.OccurredAtUtc.UtcDateTime);
            AddParameter(command, "@PerformedBy", entry.PerformedBy);
            AddParameter(command, "@IpAddress", entry.IpAddress);
            AddParameter(command, "@Metadata", JsonSerializer.Serialize(entry.Metadata));

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "consent.audit_store_error",
                message: $"Failed to record audit entry: {ex.Message}",
                details: new Dictionary<string, object?> { ["subjectId"] = entry.SubjectId, ["purpose"] = entry.Purpose }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<ConsentAuditEntry>>> GetAuditTrailAsync(
        string subjectId,
        string? purpose = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);

        try
        {
            var sql = purpose is null
                ? $@"
                    SELECT Id, SubjectId, Purpose, Action, OccurredAtUtc, PerformedBy, IpAddress, Metadata
                    FROM {_tableName}
                    WHERE SubjectId = @SubjectId
                    ORDER BY OccurredAtUtc DESC"
                : $@"
                    SELECT Id, SubjectId, Purpose, Action, OccurredAtUtc, PerformedBy, IpAddress, Metadata
                    FROM {_tableName}
                    WHERE SubjectId = @SubjectId AND Purpose = @Purpose
                    ORDER BY OccurredAtUtc DESC";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@SubjectId", subjectId);
            if (purpose is not null)
                AddParameter(command, "@Purpose", purpose);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var entries = new List<ConsentAuditEntry>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var metadataJson = reader.GetString(reader.GetOrdinal("Metadata"));
                var metadata = JsonSerializer.Deserialize<Dictionary<string, object?>>(metadataJson)
                    ?? new Dictionary<string, object?>();

                entries.Add(new ConsentAuditEntry
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    SubjectId = reader.GetString(reader.GetOrdinal("SubjectId")),
                    Purpose = reader.GetString(reader.GetOrdinal("Purpose")),
                    Action = (ConsentAuditAction)reader.GetInt32(reader.GetOrdinal("Action")),
                    OccurredAtUtc = new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("OccurredAtUtc")), TimeSpan.Zero),
                    PerformedBy = reader.GetString(reader.GetOrdinal("PerformedBy")),
                    IpAddress = reader.IsDBNull(reader.GetOrdinal("IpAddress"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("IpAddress")),
                    Metadata = metadata
                });
            }

            return Right<EncinaError, IReadOnlyList<ConsentAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "consent.audit_store_error",
                message: $"Failed to get audit trail: {ex.Message}",
                details: new Dictionary<string, object?> { ["subjectId"] = subjectId, ["purpose"] = purpose }));
        }
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
        if (command is MySqlCommand mysqlCommand)
            return await mysqlCommand.ExecuteReaderAsync(cancellationToken);

        return await Task.Run(command.ExecuteReader, cancellationToken);
    }

    private static async Task<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is MySqlCommand mysqlCommand)
            return await mysqlCommand.ExecuteNonQueryAsync(cancellationToken);

        return await Task.Run(command.ExecuteNonQuery, cancellationToken);
    }

    private static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is MySqlDataReader mysqlReader)
            return await mysqlReader.ReadAsync(cancellationToken);

        return await Task.Run(reader.Read, cancellationToken);
    }
}
