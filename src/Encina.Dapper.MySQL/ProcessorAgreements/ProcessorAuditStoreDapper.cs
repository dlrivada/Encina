using System.Data;
using Dapper;
using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Messaging;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.MySQL.ProcessorAgreements;

/// <summary>
/// Dapper implementation of <see cref="IProcessorAuditStore"/> for MySQL.
/// Provides immutable audit trail for processor and DPA actions per GDPR Article 5(2).
/// </summary>
/// <remarks>
/// <para>
/// MySQL-specific considerations:
/// <list type="bullet">
/// <item><description>DateTimeOffset values are written via <c>.UtcDateTime</c>.</description></item>
/// <item><description>DateTimeOffset values are read via direct cast <c>(DateTimeOffset)row.Prop</c>.</description></item>
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
                (Id, ProcessorId, DPAId, Action, Detail, PerformedByUserId, OccurredAtUtc, TenantId, ModuleId)
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
                OccurredAtUtc = entity.OccurredAtUtc.UtcDateTime,
                entity.TenantId,
                entity.ModuleId
            });

            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "processor.store_error",
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

            var rows = await _connection.QueryAsync(sql, new { ProcessorId = processorId });
            var entries = rows
                .Select(row => ProcessorAgreementAuditEntryMapper.ToDomain(MapToEntity(row)))
                .ToList();

            return Right<EncinaError, IReadOnlyList<ProcessorAgreementAuditEntry>>(
                (IReadOnlyList<ProcessorAgreementAuditEntry>)entries);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "processor.store_error",
                message: $"Failed to get processor agreement audit trail: {ex.Message}",
                details: new Dictionary<string, object?> { ["processorId"] = processorId }));
        }
    }

    private static ProcessorAgreementAuditEntryEntity MapToEntity(dynamic row)
    {
        return new ProcessorAgreementAuditEntryEntity
        {
            Id = (string)row.Id,
            ProcessorId = (string)row.ProcessorId,
            DPAId = row.DPAId is null or DBNull ? null : (string)row.DPAId,
            Action = (string)row.Action,
            Detail = row.Detail is null or DBNull ? null : (string)row.Detail,
            PerformedByUserId = row.PerformedByUserId is null or DBNull ? null : (string)row.PerformedByUserId,
            OccurredAtUtc = (DateTimeOffset)row.OccurredAtUtc,
            TenantId = row.TenantId is null or DBNull ? null : (string)row.TenantId,
            ModuleId = row.ModuleId is null or DBNull ? null : (string)row.ModuleId
        };
    }
}
