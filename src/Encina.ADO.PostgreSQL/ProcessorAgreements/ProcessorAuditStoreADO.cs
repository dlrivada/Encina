using System.Data;
using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Messaging;
using LanguageExt;
using Npgsql;
using static LanguageExt.Prelude;

namespace Encina.ADO.PostgreSQL.ProcessorAgreements;

/// <summary>
/// ADO.NET implementation of <see cref="IProcessorAuditStore"/> for PostgreSQL.
/// Provides immutable audit trail for processor and DPA actions per GDPR Article 5(2).
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
public sealed class ProcessorAuditStoreADO : IProcessorAuditStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessorAuditStoreADO"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The audit entries table name (default: ProcessorAgreementAuditEntries).</param>
    public ProcessorAuditStoreADO(
        IDbConnection connection,
        string tableName = "ProcessorAgreementAuditEntries")
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RecordAsync(
        ProcessorAgreementAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            var entity = ProcessorAgreementAuditEntryMapper.ToEntity(entry);
            var sql = $@"
                INSERT INTO {_tableName}
                (id, processorid, dpaid, action, detail, performedbyuserid, occurredatutc, tenantid, moduleid)
                VALUES
                (@Id, @ProcessorId, @DPAId, @Action, @Detail, @PerformedByUserId, @OccurredAtUtc, @TenantId, @ModuleId)";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", entity.Id);
            AddParameter(command, "@ProcessorId", entity.ProcessorId);
            AddParameter(command, "@DPAId", entity.DPAId);
            AddParameter(command, "@Action", entity.Action);
            AddParameter(command, "@Detail", entity.Detail);
            AddParameter(command, "@PerformedByUserId", entity.PerformedByUserId);
            AddParameter(command, "@OccurredAtUtc", entity.OccurredAtUtc.UtcDateTime);
            AddParameter(command, "@TenantId", entity.TenantId);
            AddParameter(command, "@ModuleId", entity.ModuleId);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(ProcessorAgreementErrors.StoreError(
                "RecordAuditEntry", ex.Message, ex));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<ProcessorAgreementAuditEntry>>> GetAuditTrailAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processorId);

        try
        {
            var sql = $@"
                SELECT id, processorid, dpaid, action, detail, performedbyuserid, occurredatutc, tenantid, moduleid
                FROM {_tableName}
                WHERE processorid = @ProcessorId
                ORDER BY occurredatutc ASC";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@ProcessorId", processorId);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var entries = new List<ProcessorAgreementAuditEntry>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToEntity(reader);
                var domain = ProcessorAgreementAuditEntryMapper.ToDomain(entity);
                entries.Add(domain);
            }

            return Right<EncinaError, IReadOnlyList<ProcessorAgreementAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left(ProcessorAgreementErrors.StoreError(
                "GetAuditTrail", ex.Message, ex));
        }
    }

    private static ProcessorAgreementAuditEntryEntity MapToEntity(IDataReader reader)
    {
        var dpaIdOrd = reader.GetOrdinal("dpaid");
        var detailOrd = reader.GetOrdinal("detail");
        var performedByUserIdOrd = reader.GetOrdinal("performedbyuserid");
        var tenantIdOrd = reader.GetOrdinal("tenantid");
        var moduleIdOrd = reader.GetOrdinal("moduleid");

        return new ProcessorAgreementAuditEntryEntity
        {
            Id = reader.GetString(reader.GetOrdinal("id")),
            ProcessorId = reader.GetString(reader.GetOrdinal("processorid")),
            DPAId = reader.IsDBNull(dpaIdOrd) ? null : reader.GetString(dpaIdOrd),
            Action = reader.GetString(reader.GetOrdinal("action")),
            Detail = reader.IsDBNull(detailOrd) ? null : reader.GetString(detailOrd),
            PerformedByUserId = reader.IsDBNull(performedByUserIdOrd) ? null : reader.GetString(performedByUserIdOrd),
            OccurredAtUtc = new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("occurredatutc")), TimeSpan.Zero),
            TenantId = reader.IsDBNull(tenantIdOrd) ? null : reader.GetString(tenantIdOrd),
            ModuleId = reader.IsDBNull(moduleIdOrd) ? null : reader.GetString(moduleIdOrd)
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
