using System.Data;
using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;
using Encina.Messaging;
using LanguageExt;
using Npgsql;
using static LanguageExt.Prelude;

namespace Encina.ADO.PostgreSQL.DPIA;

/// <summary>
/// ADO.NET implementation of <see cref="IDPIAAuditStore"/> for PostgreSQL.
/// Provides immutable audit trail for DPIA assessment actions per GDPR Article 5(2).
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
public sealed class DPIAAuditStoreADO : IDPIAAuditStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;

    /// <summary>
    /// Initializes a new instance of the <see cref="DPIAAuditStoreADO"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The audit entries table name (default: DPIAAuditEntries).</param>
    public DPIAAuditStoreADO(
        IDbConnection connection,
        string tableName = "DPIAAuditEntries")
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RecordAuditEntryAsync(
        DPIAAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            var entity = DPIAAuditEntryMapper.ToEntity(entry);
            var sql = $@"
                INSERT INTO {_tableName}
                (id, assessmentid, action, performedby, occurredatutc, details)
                VALUES
                (@Id, @AssessmentId, @Action, @PerformedBy, @OccurredAtUtc, @Details)";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", entity.Id);
            AddParameter(command, "@AssessmentId", entity.AssessmentId);
            AddParameter(command, "@Action", entity.Action);
            AddParameter(command, "@PerformedBy", entity.PerformedBy);
            AddParameter(command, "@OccurredAtUtc", entity.OccurredAtUtc.UtcDateTime);
            AddParameter(command, "@Details", entity.Details);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "dpia.audit_store_error",
                message: $"Failed to record DPIA audit entry: {ex.Message}",
                details: new Dictionary<string, object?> { ["assessmentId"] = entry.AssessmentId, ["action"] = entry.Action }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DPIAAuditEntry>>> GetAuditTrailAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = $@"
                SELECT id, assessmentid, action, performedby, occurredatutc, details
                FROM {_tableName}
                WHERE assessmentid = @AssessmentId
                ORDER BY occurredatutc ASC";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@AssessmentId", assessmentId.ToString("D"));

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var entries = new List<DPIAAuditEntry>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToEntity(reader);
                var domain = DPIAAuditEntryMapper.ToDomain(entity);
                if (domain is not null)
                    entries.Add(domain);
            }

            return Right<EncinaError, IReadOnlyList<DPIAAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "dpia.audit_store_error",
                message: $"Failed to get DPIA audit trail: {ex.Message}",
                details: new Dictionary<string, object?> { ["assessmentId"] = assessmentId }));
        }
    }

    private static DPIAAuditEntryEntity MapToEntity(IDataReader reader)
    {
        var performedByOrd = reader.GetOrdinal("performedby");
        var detailsOrd = reader.GetOrdinal("details");

        return new DPIAAuditEntryEntity
        {
            Id = reader.GetString(reader.GetOrdinal("id")),
            AssessmentId = reader.GetString(reader.GetOrdinal("assessmentid")),
            Action = reader.GetString(reader.GetOrdinal("action")),
            PerformedBy = reader.IsDBNull(performedByOrd) ? null : reader.GetString(performedByOrd),
            OccurredAtUtc = new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("occurredatutc")), TimeSpan.Zero),
            Details = reader.IsDBNull(detailsOrd) ? null : reader.GetString(detailsOrd)
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
