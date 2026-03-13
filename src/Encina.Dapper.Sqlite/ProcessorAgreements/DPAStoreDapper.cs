using System.Data;
using System.Globalization;
using Dapper;
using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Messaging;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.Sqlite.ProcessorAgreements;

/// <summary>
/// Dapper implementation of <see cref="IDPAStore"/> for SQLite.
/// Manages Data Processing Agreement persistence per GDPR Article 28(3).
/// </summary>
/// <remarks>
/// <para>
/// SQLite-specific considerations:
/// <list type="bullet">
/// <item><description>DateTimeOffset values are stored as ISO 8601 text via <c>.ToString("O")</c>.</description></item>
/// <item><description>Boolean values are stored as integers (0/1) and read with <c>Convert.ToInt32() == 1</c>.</description></item>
/// <item><description>Integer values require <c>Convert.ToInt32()</c> for safe casting from SQLite's dynamic types.</description></item>
/// <item><description>Never uses <c>datetime('now')</c>; always uses parameterized values.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class DPAStoreDapper : IDPAStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DPAStoreDapper"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The DPA table name (default: DataProcessingAgreements).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    public DPAStoreDapper(
        IDbConnection connection,
        string tableName = "DataProcessingAgreements",
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> AddAsync(
        DataProcessingAgreement agreement,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agreement);

        try
        {
            var entity = DataProcessingAgreementMapper.ToEntity(agreement);
            var sql = $@"
                INSERT INTO {_tableName}
                (Id, ProcessorId, StatusValue, SignedAtUtc, ExpiresAtUtc, HasSCCs, ProcessingPurposesJson,
                 ProcessOnDocumentedInstructions, ConfidentialityObligations, SecurityMeasures,
                 SubProcessorRequirements, DataSubjectRightsAssistance, ComplianceAssistance,
                 DataDeletionOrReturn, AuditRights, TenantId, ModuleId, CreatedAtUtc, LastUpdatedAtUtc)
                VALUES
                (@Id, @ProcessorId, @StatusValue, @SignedAtUtc, @ExpiresAtUtc, @HasSCCs, @ProcessingPurposesJson,
                 @ProcessOnDocumentedInstructions, @ConfidentialityObligations, @SecurityMeasures,
                 @SubProcessorRequirements, @DataSubjectRightsAssistance, @ComplianceAssistance,
                 @DataDeletionOrReturn, @AuditRights, @TenantId, @ModuleId, @CreatedAtUtc, @LastUpdatedAtUtc)";

            await _connection.ExecuteAsync(sql, new
            {
                entity.Id,
                entity.ProcessorId,
                entity.StatusValue,
                SignedAtUtc = entity.SignedAtUtc.ToString("O"),
                ExpiresAtUtc = entity.ExpiresAtUtc?.ToString("O"),
                HasSCCs = entity.HasSCCs ? 1 : 0,
                entity.ProcessingPurposesJson,
                ProcessOnDocumentedInstructions = entity.ProcessOnDocumentedInstructions ? 1 : 0,
                ConfidentialityObligations = entity.ConfidentialityObligations ? 1 : 0,
                SecurityMeasures = entity.SecurityMeasures ? 1 : 0,
                SubProcessorRequirements = entity.SubProcessorRequirements ? 1 : 0,
                DataSubjectRightsAssistance = entity.DataSubjectRightsAssistance ? 1 : 0,
                ComplianceAssistance = entity.ComplianceAssistance ? 1 : 0,
                DataDeletionOrReturn = entity.DataDeletionOrReturn ? 1 : 0,
                AuditRights = entity.AuditRights ? 1 : 0,
                entity.TenantId,
                entity.ModuleId,
                CreatedAtUtc = entity.CreatedAtUtc.ToString("O"),
                LastUpdatedAtUtc = entity.LastUpdatedAtUtc.ToString("O")
            });

            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(ProcessorAgreementErrors.StoreError(
                "AddDPA", ex.Message, ex));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<DataProcessingAgreement>>> GetByIdAsync(
        string dpaId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dpaId);

        try
        {
            var sql = $@"
                SELECT Id, ProcessorId, StatusValue, SignedAtUtc, ExpiresAtUtc, HasSCCs, ProcessingPurposesJson,
                       ProcessOnDocumentedInstructions, ConfidentialityObligations, SecurityMeasures,
                       SubProcessorRequirements, DataSubjectRightsAssistance, ComplianceAssistance,
                       DataDeletionOrReturn, AuditRights, TenantId, ModuleId, CreatedAtUtc, LastUpdatedAtUtc
                FROM {_tableName}
                WHERE Id = @Id";

            var rows = await _connection.QueryAsync(sql, new { Id = dpaId });
            var row = rows.FirstOrDefault();

            if (row is null)
                return Right<EncinaError, Option<DataProcessingAgreement>>(None);

            var dpa = MapToDPA(row);
            return dpa is not null
                ? Right<EncinaError, Option<DataProcessingAgreement>>(Some(dpa))
                : Right<EncinaError, Option<DataProcessingAgreement>>(None);
        }
        catch (Exception ex)
        {
            return Left(ProcessorAgreementErrors.StoreError(
                "GetDPAById", ex.Message, ex));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DataProcessingAgreement>>> GetByProcessorIdAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processorId);

        try
        {
            var sql = $@"
                SELECT Id, ProcessorId, StatusValue, SignedAtUtc, ExpiresAtUtc, HasSCCs, ProcessingPurposesJson,
                       ProcessOnDocumentedInstructions, ConfidentialityObligations, SecurityMeasures,
                       SubProcessorRequirements, DataSubjectRightsAssistance, ComplianceAssistance,
                       DataDeletionOrReturn, AuditRights, TenantId, ModuleId, CreatedAtUtc, LastUpdatedAtUtc
                FROM {_tableName}
                WHERE ProcessorId = @ProcessorId";

            var rows = await _connection.QueryAsync(sql, new { ProcessorId = processorId });
            var results = rows
                .Select(MapToDPA)
                .Where(d => d is not null)
                .Cast<DataProcessingAgreement>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<DataProcessingAgreement>>(results);
        }
        catch (Exception ex)
        {
            return Left(ProcessorAgreementErrors.StoreError(
                "GetDPAByProcessorId", ex.Message, ex));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<DataProcessingAgreement>>> GetActiveByProcessorIdAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processorId);

        try
        {
            var sql = $@"
                SELECT Id, ProcessorId, StatusValue, SignedAtUtc, ExpiresAtUtc, HasSCCs, ProcessingPurposesJson,
                       ProcessOnDocumentedInstructions, ConfidentialityObligations, SecurityMeasures,
                       SubProcessorRequirements, DataSubjectRightsAssistance, ComplianceAssistance,
                       DataDeletionOrReturn, AuditRights, TenantId, ModuleId, CreatedAtUtc, LastUpdatedAtUtc
                FROM {_tableName}
                WHERE ProcessorId = @ProcessorId AND StatusValue = @ActiveStatus";

            var rows = await _connection.QueryAsync(sql, new
            {
                ProcessorId = processorId,
                ActiveStatus = (int)DPAStatus.Active
            });
            var row = rows.FirstOrDefault();

            if (row is null)
                return Right<EncinaError, Option<DataProcessingAgreement>>(None);

            var dpa = MapToDPA(row);
            return dpa is not null
                ? Right<EncinaError, Option<DataProcessingAgreement>>(Some(dpa))
                : Right<EncinaError, Option<DataProcessingAgreement>>(None);
        }
        catch (Exception ex)
        {
            return Left(ProcessorAgreementErrors.StoreError(
                "GetActiveDPAByProcessorId", ex.Message, ex));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> UpdateAsync(
        DataProcessingAgreement agreement,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agreement);

        try
        {
            var existsSql = $"SELECT COUNT(1) FROM {_tableName} WHERE Id = @Id";
            var exists = await _connection.ExecuteScalarAsync<long>(existsSql, new { agreement.Id });
            if (exists == 0)
            {
                return Left(ProcessorAgreementErrors.DPANotFound(agreement.Id));
            }

            var entity = DataProcessingAgreementMapper.ToEntity(agreement);
            var sql = $@"
                UPDATE {_tableName}
                SET ProcessorId = @ProcessorId,
                    StatusValue = @StatusValue,
                    SignedAtUtc = @SignedAtUtc,
                    ExpiresAtUtc = @ExpiresAtUtc,
                    HasSCCs = @HasSCCs,
                    ProcessingPurposesJson = @ProcessingPurposesJson,
                    ProcessOnDocumentedInstructions = @ProcessOnDocumentedInstructions,
                    ConfidentialityObligations = @ConfidentialityObligations,
                    SecurityMeasures = @SecurityMeasures,
                    SubProcessorRequirements = @SubProcessorRequirements,
                    DataSubjectRightsAssistance = @DataSubjectRightsAssistance,
                    ComplianceAssistance = @ComplianceAssistance,
                    DataDeletionOrReturn = @DataDeletionOrReturn,
                    AuditRights = @AuditRights,
                    TenantId = @TenantId,
                    ModuleId = @ModuleId,
                    LastUpdatedAtUtc = @LastUpdatedAtUtc
                WHERE Id = @Id";

            await _connection.ExecuteAsync(sql, new
            {
                entity.Id,
                entity.ProcessorId,
                entity.StatusValue,
                SignedAtUtc = entity.SignedAtUtc.ToString("O"),
                ExpiresAtUtc = entity.ExpiresAtUtc?.ToString("O"),
                HasSCCs = entity.HasSCCs ? 1 : 0,
                entity.ProcessingPurposesJson,
                ProcessOnDocumentedInstructions = entity.ProcessOnDocumentedInstructions ? 1 : 0,
                ConfidentialityObligations = entity.ConfidentialityObligations ? 1 : 0,
                SecurityMeasures = entity.SecurityMeasures ? 1 : 0,
                SubProcessorRequirements = entity.SubProcessorRequirements ? 1 : 0,
                DataSubjectRightsAssistance = entity.DataSubjectRightsAssistance ? 1 : 0,
                ComplianceAssistance = entity.ComplianceAssistance ? 1 : 0,
                DataDeletionOrReturn = entity.DataDeletionOrReturn ? 1 : 0,
                AuditRights = entity.AuditRights ? 1 : 0,
                entity.TenantId,
                entity.ModuleId,
                LastUpdatedAtUtc = entity.LastUpdatedAtUtc.ToString("O")
            });

            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(ProcessorAgreementErrors.StoreError(
                "UpdateDPA", ex.Message, ex));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DataProcessingAgreement>>> GetByStatusAsync(
        DPAStatus status,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = $@"
                SELECT Id, ProcessorId, StatusValue, SignedAtUtc, ExpiresAtUtc, HasSCCs, ProcessingPurposesJson,
                       ProcessOnDocumentedInstructions, ConfidentialityObligations, SecurityMeasures,
                       SubProcessorRequirements, DataSubjectRightsAssistance, ComplianceAssistance,
                       DataDeletionOrReturn, AuditRights, TenantId, ModuleId, CreatedAtUtc, LastUpdatedAtUtc
                FROM {_tableName}
                WHERE StatusValue = @StatusValue";

            var rows = await _connection.QueryAsync(sql, new { StatusValue = (int)status });
            var results = rows
                .Select(MapToDPA)
                .Where(d => d is not null)
                .Cast<DataProcessingAgreement>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<DataProcessingAgreement>>(results);
        }
        catch (Exception ex)
        {
            return Left(ProcessorAgreementErrors.StoreError(
                "GetDPAByStatus", ex.Message, ex));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DataProcessingAgreement>>> GetExpiringAsync(
        DateTimeOffset threshold,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = $@"
                SELECT Id, ProcessorId, StatusValue, SignedAtUtc, ExpiresAtUtc, HasSCCs, ProcessingPurposesJson,
                       ProcessOnDocumentedInstructions, ConfidentialityObligations, SecurityMeasures,
                       SubProcessorRequirements, DataSubjectRightsAssistance, ComplianceAssistance,
                       DataDeletionOrReturn, AuditRights, TenantId, ModuleId, CreatedAtUtc, LastUpdatedAtUtc
                FROM {_tableName}
                WHERE StatusValue = @ActiveStatus
                  AND ExpiresAtUtc IS NOT NULL
                  AND ExpiresAtUtc <= @Threshold";

            var rows = await _connection.QueryAsync(sql, new
            {
                ActiveStatus = (int)DPAStatus.Active,
                Threshold = threshold.ToString("O")
            });

            var results = rows
                .Select(MapToDPA)
                .Where(d => d is not null)
                .Cast<DataProcessingAgreement>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<DataProcessingAgreement>>(results);
        }
        catch (Exception ex)
        {
            return Left(ProcessorAgreementErrors.StoreError(
                "GetExpiringDPAs", ex.Message, ex));
        }
    }

    private static DataProcessingAgreement? MapToDPA(dynamic row)
    {
        var entity = new DataProcessingAgreementEntity
        {
            Id = (string)row.Id,
            ProcessorId = (string)row.ProcessorId,
            StatusValue = Convert.ToInt32(row.StatusValue),
            SignedAtUtc = DateTimeOffset.Parse((string)row.SignedAtUtc, null, DateTimeStyles.RoundtripKind),
            ExpiresAtUtc = row.ExpiresAtUtc is null or DBNull
                ? null
                : DateTimeOffset.Parse((string)row.ExpiresAtUtc, null, DateTimeStyles.RoundtripKind),
            HasSCCs = Convert.ToInt32(row.HasSCCs) == 1,
            ProcessingPurposesJson = row.ProcessingPurposesJson is null or DBNull ? "[]" : (string)row.ProcessingPurposesJson,
            ProcessOnDocumentedInstructions = Convert.ToInt32(row.ProcessOnDocumentedInstructions) == 1,
            ConfidentialityObligations = Convert.ToInt32(row.ConfidentialityObligations) == 1,
            SecurityMeasures = Convert.ToInt32(row.SecurityMeasures) == 1,
            SubProcessorRequirements = Convert.ToInt32(row.SubProcessorRequirements) == 1,
            DataSubjectRightsAssistance = Convert.ToInt32(row.DataSubjectRightsAssistance) == 1,
            ComplianceAssistance = Convert.ToInt32(row.ComplianceAssistance) == 1,
            DataDeletionOrReturn = Convert.ToInt32(row.DataDeletionOrReturn) == 1,
            AuditRights = Convert.ToInt32(row.AuditRights) == 1,
            TenantId = row.TenantId is null or DBNull ? null : (string)row.TenantId,
            ModuleId = row.ModuleId is null or DBNull ? null : (string)row.ModuleId,
            CreatedAtUtc = DateTimeOffset.Parse((string)row.CreatedAtUtc, null, DateTimeStyles.RoundtripKind),
            LastUpdatedAtUtc = DateTimeOffset.Parse((string)row.LastUpdatedAtUtc, null, DateTimeStyles.RoundtripKind)
        };
        return DataProcessingAgreementMapper.ToDomain(entity);
    }
}
