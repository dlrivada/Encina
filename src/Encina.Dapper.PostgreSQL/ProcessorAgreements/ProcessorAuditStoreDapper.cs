using System.Data;
using Dapper;
using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Messaging;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.PostgreSQL.ProcessorAgreements;

/// <summary>
/// Dapper implementation of <see cref="IProcessorAuditStore"/> for PostgreSQL.
/// Provides immutable audit trail for processor agreement actions per GDPR Article 5(2).
/// </summary>
/// <remarks>
/// <para>
/// PostgreSQL-specific considerations:
/// <list type="bullet">
/// <item><description>DateTimeOffset values are passed directly (native TIMESTAMPTZ support).</description></item>
/// <item><description>Column names are lowercase; Dapper returns lowercase property names.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class ProcessorAuditStoreDapper : IProcessorAuditStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessorAuditStoreDapper"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The audit entries table name (default: ProcessorAgreementAuditEntries).</param>
    public ProcessorAuditStoreDapper(
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

            await _connection.ExecuteAsync(sql, new
            {
                entity.Id,
                entity.ProcessorId,
                entity.DPAId,
                entity.Action,
                entity.Detail,
                entity.PerformedByUserId,
                entity.OccurredAtUtc,
                entity.TenantId,
                entity.ModuleId
            });

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

            var rows = await _connection.QueryAsync(sql, new { ProcessorId = processorId });
            var entries = rows
                .Select(row => (ProcessorAgreementAuditEntry)ProcessorAgreementAuditEntryMapper.ToDomain(MapToEntity(row)))
                .ToList();

            return Right<EncinaError, IReadOnlyList<ProcessorAgreementAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left(ProcessorAgreementErrors.StoreError(
                "GetAuditTrail", ex.Message, ex));
        }
    }

    private static ProcessorAgreementAuditEntryEntity MapToEntity(dynamic row)
    {
        return new ProcessorAgreementAuditEntryEntity
        {
            Id = (string)row.id,
            ProcessorId = (string)row.processorid,
            DPAId = row.dpaid is null or DBNull ? null : (string)row.dpaid,
            Action = (string)row.action,
            Detail = row.detail is null or DBNull ? null : (string)row.detail,
            PerformedByUserId = row.performedbyuserid is null or DBNull ? null : (string)row.performedbyuserid,
            OccurredAtUtc = (DateTimeOffset)row.occurredatutc,
            TenantId = row.tenantid is null or DBNull ? null : (string)row.tenantid,
            ModuleId = row.moduleid is null or DBNull ? null : (string)row.moduleid
        };
    }
}
