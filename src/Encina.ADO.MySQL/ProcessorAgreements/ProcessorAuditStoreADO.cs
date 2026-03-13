using System.Data;
using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Messaging;
using LanguageExt;
using MySqlConnector;
using static LanguageExt.Prelude;

namespace Encina.ADO.MySQL.ProcessorAgreements;

/// <summary>
/// ADO.NET implementation of <see cref="IProcessorAuditStore"/> for MySQL.
/// Provides immutable audit trail for processor agreement actions per GDPR Article 5(2).
/// </summary>
/// <remarks>
/// <para>
/// MySQL-specific considerations:
/// <list type="bullet">
/// <item><description>DateTimeOffset values are stored as <c>DATETIME(6)</c> via <c>.UtcDateTime</c>.</description></item>
/// <item><description>Reads use <c>new DateTimeOffset(reader.GetDateTime(...), TimeSpan.Zero)</c> for UTC reconstruction.</description></item>
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
                (Id, ProcessorId, DPAId, Action, Detail, PerformedByUserId, OccurredAtUtc, TenantId, ModuleId)
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
            return Left(EncinaErrors.Create(
                code: "processor.audit_store_error",
                message: $"Failed to record processor agreement audit entry: {ex.Message}",
                details: new Dictionary<string, object?> { ["processorId"] = entry.ProcessorId, ["action"] = entry.Action }));
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
                SELECT Id, ProcessorId, DPAId, Action, Detail, PerformedByUserId, OccurredAtUtc, TenantId, ModuleId
                FROM {_tableName}
                WHERE ProcessorId = @ProcessorId
                ORDER BY OccurredAtUtc ASC";

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
            return Left(EncinaErrors.Create(
                code: "processor.audit_store_error",
                message: $"Failed to get processor agreement audit trail: {ex.Message}",
                details: new Dictionary<string, object?> { ["processorId"] = processorId }));
        }
    }

    private static ProcessorAgreementAuditEntryEntity MapToEntity(IDataReader reader)
    {
        var dpaIdOrd = reader.GetOrdinal("DPAId");
        var detailOrd = reader.GetOrdinal("Detail");
        var performedByUserIdOrd = reader.GetOrdinal("PerformedByUserId");
        var tenantIdOrd = reader.GetOrdinal("TenantId");
        var moduleIdOrd = reader.GetOrdinal("ModuleId");

        return new ProcessorAgreementAuditEntryEntity
        {
            Id = reader.GetString(reader.GetOrdinal("Id")),
            ProcessorId = reader.GetString(reader.GetOrdinal("ProcessorId")),
            DPAId = reader.IsDBNull(dpaIdOrd) ? null : reader.GetString(dpaIdOrd),
            Action = reader.GetString(reader.GetOrdinal("Action")),
            Detail = reader.IsDBNull(detailOrd) ? null : reader.GetString(detailOrd),
            PerformedByUserId = reader.IsDBNull(performedByUserIdOrd) ? null : reader.GetString(performedByUserIdOrd),
            OccurredAtUtc = new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("OccurredAtUtc")), TimeSpan.Zero),
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
