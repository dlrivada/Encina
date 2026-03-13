using System.Data;

using Dapper;

using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Messaging;

using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.Dapper.PostgreSQL.ProcessorAgreements;

/// <summary>
/// Dapper implementation of <see cref="IDPAStore"/> for PostgreSQL.
/// Manages Data Processing Agreement persistence per GDPR Article 28(3).
/// </summary>
/// <remarks>
/// <para>
/// PostgreSQL-specific considerations:
/// <list type="bullet">
/// <item><description>DateTimeOffset values are passed directly (native TIMESTAMPTZ support).</description></item>
/// <item><description>Boolean values are passed directly (native BOOLEAN support).</description></item>
/// <item><description>Column names are lowercase; Dapper returns lowercase property names.</description></item>
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
            // Check if agreement already exists.
            var existsSql = $"SELECT COUNT(1) FROM {_tableName} WHERE id = @Id";
            var exists = await _connection.ExecuteScalarAsync<long>(existsSql, new { Id = agreement.Id });

            if (exists > 0)
            {
                return ProcessorAgreementErrors.StoreError("AddDPA", $"Agreement '{agreement.Id}' already exists.");
            }

            var entity = DataProcessingAgreementMapper.ToEntity(agreement);
            var sql = $@"
                INSERT INTO {_tableName}
                (id, processorid, statusvalue, signedatutc, expiresatutc, hassccs, processingpurposesjson,
                 processondocumentedinstructions, confidentialityobligations, securitymeasures,
                 subprocessorrequirements, datasubjectrightsassistance, complianceassistance,
                 datadeletionorreturn, auditrights, tenantid, moduleid, createdatutc, lastupdatedatutc)
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
                entity.SignedAtUtc,
                entity.ExpiresAtUtc,
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
                entity.CreatedAtUtc,
                entity.LastUpdatedAtUtc
            });

            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return ProcessorAgreementErrors.StoreError("AddDPA", ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<DataProcessingAgreement>>> GetByIdAsync(
        string dpaId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dpaId);

        try
        {
            var sql = $@"
                SELECT id, processorid, statusvalue, signedatutc, expiresatutc, hassccs, processingpurposesjson,
                       processondocumentedinstructions, confidentialityobligations, securitymeasures,
                       subprocessorrequirements, datasubjectrightsassistance, complianceassistance,
                       datadeletionorreturn, auditrights, tenantid, moduleid, createdatutc, lastupdatedatutc
                FROM {_tableName}
                WHERE id = @Id";

            var rows = await _connection.QueryAsync(sql, new { Id = dpaId });
            var row = rows.FirstOrDefault();

            if (row is null)
            {
                return Right<EncinaError, Option<DataProcessingAgreement>>(Option<DataProcessingAgreement>.None);
            }

            var dpa = MapToDPA(row);

            return dpa is not null
                ? Right<EncinaError, Option<DataProcessingAgreement>>(Some(dpa))
                : Right<EncinaError, Option<DataProcessingAgreement>>(Option<DataProcessingAgreement>.None);
        }
        catch (Exception ex)
        {
            return ProcessorAgreementErrors.StoreError("GetDPAById", ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DataProcessingAgreement>>> GetByProcessorIdAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processorId);

        try
        {
            var sql = $@"
                SELECT id, processorid, statusvalue, signedatutc, expiresatutc, hassccs, processingpurposesjson,
                       processondocumentedinstructions, confidentialityobligations, securitymeasures,
                       subprocessorrequirements, datasubjectrightsassistance, complianceassistance,
                       datadeletionorreturn, auditrights, tenantid, moduleid, createdatutc, lastupdatedatutc
                FROM {_tableName}
                WHERE processorid = @ProcessorId";

            var rows = await _connection.QueryAsync(sql, new { ProcessorId = processorId });
            var agreements = rows
                .Select(MapToDPA)
                .Where(a => a is not null)
                .Cast<DataProcessingAgreement>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<DataProcessingAgreement>>(agreements);
        }
        catch (Exception ex)
        {
            return ProcessorAgreementErrors.StoreError("GetDPAByProcessorId", ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<DataProcessingAgreement>>> GetActiveByProcessorIdAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processorId);

        try
        {
            var sql = $@"
                SELECT id, processorid, statusvalue, signedatutc, expiresatutc, hassccs, processingpurposesjson,
                       processondocumentedinstructions, confidentialityobligations, securitymeasures,
                       subprocessorrequirements, datasubjectrightsassistance, complianceassistance,
                       datadeletionorreturn, auditrights, tenantid, moduleid, createdatutc, lastupdatedatutc
                FROM {_tableName}
                WHERE processorid = @ProcessorId AND statusvalue = @ActiveStatus";

            var rows = await _connection.QueryAsync(sql, new
            {
                ProcessorId = processorId,
                ActiveStatus = (int)DPAStatus.Active
            });
            var row = rows.FirstOrDefault();

            if (row is null)
            {
                return Right<EncinaError, Option<DataProcessingAgreement>>(Option<DataProcessingAgreement>.None);
            }

            var dpa = MapToDPA(row);

            return dpa is not null
                ? Right<EncinaError, Option<DataProcessingAgreement>>(Some(dpa))
                : Right<EncinaError, Option<DataProcessingAgreement>>(Option<DataProcessingAgreement>.None);
        }
        catch (Exception ex)
        {
            return ProcessorAgreementErrors.StoreError("GetActiveDPAByProcessorId", ex.Message, ex);
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
            var existsSql = $"SELECT COUNT(1) FROM {_tableName} WHERE id = @Id";
            var exists = await _connection.ExecuteScalarAsync<long>(existsSql, new { Id = agreement.Id });

            if (exists == 0)
            {
                return ProcessorAgreementErrors.DPANotFound(agreement.Id);
            }

            var entity = DataProcessingAgreementMapper.ToEntity(agreement);
            var sql = $@"
                UPDATE {_tableName}
                SET processorid = @ProcessorId, statusvalue = @StatusValue, signedatutc = @SignedAtUtc,
                    expiresatutc = @ExpiresAtUtc, hassccs = @HasSCCs, processingpurposesjson = @ProcessingPurposesJson,
                    processondocumentedinstructions = @ProcessOnDocumentedInstructions,
                    confidentialityobligations = @ConfidentialityObligations,
                    securitymeasures = @SecurityMeasures, subprocessorrequirements = @SubProcessorRequirements,
                    datasubjectrightsassistance = @DataSubjectRightsAssistance,
                    complianceassistance = @ComplianceAssistance, datadeletionorreturn = @DataDeletionOrReturn,
                    auditrights = @AuditRights, tenantid = @TenantId, moduleid = @ModuleId,
                    lastupdatedatutc = @LastUpdatedAtUtc
                WHERE id = @Id";

            await _connection.ExecuteAsync(sql, new
            {
                entity.Id,
                entity.ProcessorId,
                entity.StatusValue,
                entity.SignedAtUtc,
                entity.ExpiresAtUtc,
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
                entity.LastUpdatedAtUtc
            });

            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return ProcessorAgreementErrors.StoreError("UpdateDPA", ex.Message, ex);
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
                SELECT id, processorid, statusvalue, signedatutc, expiresatutc, hassccs, processingpurposesjson,
                       processondocumentedinstructions, confidentialityobligations, securitymeasures,
                       subprocessorrequirements, datasubjectrightsassistance, complianceassistance,
                       datadeletionorreturn, auditrights, tenantid, moduleid, createdatutc, lastupdatedatutc
                FROM {_tableName}
                WHERE statusvalue = @StatusValue";

            var rows = await _connection.QueryAsync(sql, new { StatusValue = (int)status });
            var agreements = rows
                .Select(MapToDPA)
                .Where(a => a is not null)
                .Cast<DataProcessingAgreement>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<DataProcessingAgreement>>(agreements);
        }
        catch (Exception ex)
        {
            return ProcessorAgreementErrors.StoreError("GetDPAByStatus", ex.Message, ex);
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
                SELECT id, processorid, statusvalue, signedatutc, expiresatutc, hassccs, processingpurposesjson,
                       processondocumentedinstructions, confidentialityobligations, securitymeasures,
                       subprocessorrequirements, datasubjectrightsassistance, complianceassistance,
                       datadeletionorreturn, auditrights, tenantid, moduleid, createdatutc, lastupdatedatutc
                FROM {_tableName}
                WHERE statusvalue = @ActiveStatus AND expiresatutc IS NOT NULL AND expiresatutc <= @Threshold";

            var rows = await _connection.QueryAsync(sql, new
            {
                ActiveStatus = (int)DPAStatus.Active,
                Threshold = threshold
            });
            var agreements = rows
                .Select(MapToDPA)
                .Where(a => a is not null)
                .Cast<DataProcessingAgreement>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<DataProcessingAgreement>>(agreements);
        }
        catch (Exception ex)
        {
            return ProcessorAgreementErrors.StoreError("GetExpiringDPAs", ex.Message, ex);
        }
    }

    /// <summary>
    /// Maps a dynamic row from Dapper to a <see cref="DataProcessingAgreement"/>.
    /// DateTimeOffset and boolean values are cast directly (native PostgreSQL support).
    /// Property names are lowercase because PostgreSQL returns lowercase column names.
    /// </summary>
    /// <param name="row">The dynamic row returned by Dapper.</param>
    /// <returns>A populated DPA, or <c>null</c> if the entity contains invalid values.</returns>
    private static DataProcessingAgreement? MapToDPA(dynamic row)
    {
        var entity = new DataProcessingAgreementEntity
        {
            Id = (string)row.id,
            ProcessorId = (string)row.processorid,
            StatusValue = (int)row.statusvalue,
            SignedAtUtc = (DateTimeOffset)row.signedatutc,
            ExpiresAtUtc = row.expiresatutc is null or DBNull ? null : (DateTimeOffset?)row.expiresatutc,
            HasSCCs = (bool)row.hassccs,
            ProcessingPurposesJson = row.processingpurposesjson is null or DBNull ? null! : (string)row.processingpurposesjson,
            ProcessOnDocumentedInstructions = (bool)row.processondocumentedinstructions,
            ConfidentialityObligations = (bool)row.confidentialityobligations,
            SecurityMeasures = (bool)row.securitymeasures,
            SubProcessorRequirements = (bool)row.subprocessorrequirements,
            DataSubjectRightsAssistance = (bool)row.datasubjectrightsassistance,
            ComplianceAssistance = (bool)row.complianceassistance,
            DataDeletionOrReturn = (bool)row.datadeletionorreturn,
            AuditRights = (bool)row.auditrights,
            TenantId = row.tenantid is null or DBNull ? null : (string)row.tenantid,
            ModuleId = row.moduleid is null or DBNull ? null : (string)row.moduleid,
            CreatedAtUtc = (DateTimeOffset)row.createdatutc,
            LastUpdatedAtUtc = (DateTimeOffset)row.lastupdatedatutc
        };

        return DataProcessingAgreementMapper.ToDomain(entity);
    }
}
