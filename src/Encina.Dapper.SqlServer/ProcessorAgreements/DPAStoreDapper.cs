using System.Data;
using Dapper;
using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Messaging;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.SqlServer.ProcessorAgreements;

/// <summary>
/// Dapper implementation of <see cref="IDPAStore"/> for SQL Server.
/// Manages Data Processing Agreement persistence per GDPR Article 28(3).
/// </summary>
/// <remarks>
/// <para>
/// SQL Server-specific considerations:
/// <list type="bullet">
/// <item><description>DateTimeOffset values are passed as <c>.UtcDateTime</c> for native parameter handling.</description></item>
/// <item><description>Uses PascalCase column names matching the entity properties.</description></item>
/// <item><description>Boolean values are stored as BIT columns natively.</description></item>
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
    /// <param name="tableName">The agreements table name (default: DataProcessingAgreements).</param>
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
            var existsCount = await _connection.ExecuteScalarAsync<int>(
                $"SELECT COUNT(1) FROM {_tableName} WHERE Id = @Id",
                new { Id = agreement.Id });

            if (existsCount > 0)
            {
                return Left(ProcessorAgreementErrors.StoreError(
                    "AddDPA", $"A Data Processing Agreement with ID '{agreement.Id}' already exists."));
            }

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
                SignedAtUtc = entity.SignedAtUtc.UtcDateTime,
                ExpiresAtUtc = entity.ExpiresAtUtc?.UtcDateTime,
                entity.HasSCCs,
                entity.ProcessingPurposesJson,
                entity.ProcessOnDocumentedInstructions,
                entity.ConfidentialityObligations,
                entity.SecurityMeasures,
                entity.SubProcessorRequirements,
                entity.DataSubjectRightsAssistance,
                entity.ComplianceAssistance,
                entity.DataDeletionOrReturn,
                entity.AuditRights,
                entity.TenantId,
                entity.ModuleId,
                CreatedAtUtc = entity.CreatedAtUtc.UtcDateTime,
                LastUpdatedAtUtc = entity.LastUpdatedAtUtc.UtcDateTime
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

            var domain = MapToDPA(row);
            return domain is null
                ? Right<EncinaError, Option<DataProcessingAgreement>>(None)
                : Right<EncinaError, Option<DataProcessingAgreement>>(Some(domain));
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
                .Select(row => MapToDPA(row))
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

            var domain = MapToDPA(row);
            return domain is null
                ? Right<EncinaError, Option<DataProcessingAgreement>>(None)
                : Right<EncinaError, Option<DataProcessingAgreement>>(Some(domain));
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
            var existsCount = await _connection.ExecuteScalarAsync<int>(
                $"SELECT COUNT(1) FROM {_tableName} WHERE Id = @Id",
                new { Id = agreement.Id });

            if (existsCount == 0)
            {
                return Left(ProcessorAgreementErrors.DPANotFound(agreement.Id));
            }

            var entity = DataProcessingAgreementMapper.ToEntity(agreement);

            var sql = $@"
                UPDATE {_tableName}
                SET ProcessorId = @ProcessorId, StatusValue = @StatusValue,
                    SignedAtUtc = @SignedAtUtc, ExpiresAtUtc = @ExpiresAtUtc,
                    HasSCCs = @HasSCCs, ProcessingPurposesJson = @ProcessingPurposesJson,
                    ProcessOnDocumentedInstructions = @ProcessOnDocumentedInstructions,
                    ConfidentialityObligations = @ConfidentialityObligations,
                    SecurityMeasures = @SecurityMeasures,
                    SubProcessorRequirements = @SubProcessorRequirements,
                    DataSubjectRightsAssistance = @DataSubjectRightsAssistance,
                    ComplianceAssistance = @ComplianceAssistance,
                    DataDeletionOrReturn = @DataDeletionOrReturn,
                    AuditRights = @AuditRights,
                    TenantId = @TenantId, ModuleId = @ModuleId,
                    LastUpdatedAtUtc = @LastUpdatedAtUtc
                WHERE Id = @Id";

            await _connection.ExecuteAsync(sql, new
            {
                entity.Id,
                entity.ProcessorId,
                entity.StatusValue,
                SignedAtUtc = entity.SignedAtUtc.UtcDateTime,
                ExpiresAtUtc = entity.ExpiresAtUtc?.UtcDateTime,
                entity.HasSCCs,
                entity.ProcessingPurposesJson,
                entity.ProcessOnDocumentedInstructions,
                entity.ConfidentialityObligations,
                entity.SecurityMeasures,
                entity.SubProcessorRequirements,
                entity.DataSubjectRightsAssistance,
                entity.ComplianceAssistance,
                entity.DataDeletionOrReturn,
                entity.AuditRights,
                entity.TenantId,
                entity.ModuleId,
                LastUpdatedAtUtc = entity.LastUpdatedAtUtc.UtcDateTime
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
                .Select(row => MapToDPA(row))
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
                WHERE StatusValue = @ActiveStatus AND ExpiresAtUtc IS NOT NULL AND ExpiresAtUtc <= @Threshold";

            var rows = await _connection.QueryAsync(sql, new
            {
                ActiveStatus = (int)DPAStatus.Active,
                Threshold = threshold.UtcDateTime
            });

            var results = rows
                .Select(row => MapToDPA(row))
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
            StatusValue = (int)row.StatusValue,
            SignedAtUtc = (DateTimeOffset)row.SignedAtUtc,
            ExpiresAtUtc = row.ExpiresAtUtc is null or DBNull ? null : (DateTimeOffset?)row.ExpiresAtUtc,
            HasSCCs = (bool)row.HasSCCs,
            ProcessingPurposesJson = row.ProcessingPurposesJson is null or DBNull ? null! : (string)row.ProcessingPurposesJson,
            ProcessOnDocumentedInstructions = (bool)row.ProcessOnDocumentedInstructions,
            ConfidentialityObligations = (bool)row.ConfidentialityObligations,
            SecurityMeasures = (bool)row.SecurityMeasures,
            SubProcessorRequirements = (bool)row.SubProcessorRequirements,
            DataSubjectRightsAssistance = (bool)row.DataSubjectRightsAssistance,
            ComplianceAssistance = (bool)row.ComplianceAssistance,
            DataDeletionOrReturn = (bool)row.DataDeletionOrReturn,
            AuditRights = (bool)row.AuditRights,
            TenantId = row.TenantId is null or DBNull ? null : (string)row.TenantId,
            ModuleId = row.ModuleId is null or DBNull ? null : (string)row.ModuleId,
            CreatedAtUtc = (DateTimeOffset)row.CreatedAtUtc,
            LastUpdatedAtUtc = (DateTimeOffset)row.LastUpdatedAtUtc
        };
        return DataProcessingAgreementMapper.ToDomain(entity);
    }
}
