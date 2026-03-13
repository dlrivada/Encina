using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Messaging;

using LanguageExt;

using Microsoft.EntityFrameworkCore;

using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.ProcessorAgreements;

/// <summary>
/// Entity Framework Core implementation of <see cref="IDPAStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Uses EF Core LINQ queries for provider-agnostic Data Processing Agreement management across
/// SQLite, SQL Server, PostgreSQL, and MySQL. All operations follow Railway Oriented
/// Programming with <c>Either&lt;EncinaError, T&gt;</c> return types.
/// </para>
/// <para>
/// Each write operation immediately persists via <see cref="DbContext.SaveChangesAsync"/>
/// to ensure agreement records are never lost. The store uses
/// <see cref="DataProcessingAgreementMapper"/> to convert between domain and persistence models.
/// </para>
/// <para>
/// The 8 mandatory terms from <see cref="DPAMandatoryTerms"/> are stored as individual boolean
/// columns (not JSON) to support efficient queries and filtering per Article 28(3)(a)-(h).
/// </para>
/// </remarks>
public sealed class DPAStoreEF : IDPAStore
{
    private readonly DbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="DPAStoreEF"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public DPAStoreEF(DbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> AddAsync(
        DataProcessingAgreement agreement,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agreement);

        try
        {
            var exists = await _dbContext.Set<DataProcessingAgreementEntity>()
                .AnyAsync(e => e.Id == agreement.Id, cancellationToken);

            if (exists)
            {
                return ProcessorAgreementErrors.StoreError("AddDPA", $"A DPA with ID '{agreement.Id}' already exists.");
            }

            var entity = DataProcessingAgreementMapper.ToEntity(agreement);
            await _dbContext.Set<DataProcessingAgreementEntity>().AddAsync(entity, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Right(Unit.Default);
        }
        catch (DbUpdateException ex)
        {
            return ProcessorAgreementErrors.StoreError("AddDPA", ex.Message, ex);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ProcessorAgreementErrors.StoreError("AddDPA", ex.Message, ex);
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Option<DataProcessingAgreement>>> GetByIdAsync(
        string dpaId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dpaId);

        try
        {
            var entity = await _dbContext.Set<DataProcessingAgreementEntity>()
                .FirstOrDefaultAsync(e => e.Id == dpaId, cancellationToken);

            if (entity is null)
                return Right<EncinaError, Option<DataProcessingAgreement>>(None);

            var domain = DataProcessingAgreementMapper.ToDomain(entity);
            return domain is not null
                ? Right<EncinaError, Option<DataProcessingAgreement>>(Some(domain))
                : Right<EncinaError, Option<DataProcessingAgreement>>(None);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ProcessorAgreementErrors.StoreError("GetDPAById", ex.Message, ex);
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<DataProcessingAgreement>>> GetByProcessorIdAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processorId);

        try
        {
            var entities = await _dbContext.Set<DataProcessingAgreementEntity>()
                .Where(e => e.ProcessorId == processorId)
                .OrderBy(e => e.CreatedAtUtc)
                .ToListAsync(cancellationToken);

            var agreements = entities
                .Select(DataProcessingAgreementMapper.ToDomain)
                .Where(a => a is not null)
                .Cast<DataProcessingAgreement>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<DataProcessingAgreement>>(agreements);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ProcessorAgreementErrors.StoreError("GetDPAByProcessorId", ex.Message, ex);
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Option<DataProcessingAgreement>>> GetActiveByProcessorIdAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processorId);

        try
        {
            var activeStatus = (int)DPAStatus.Active;

            var entity = await _dbContext.Set<DataProcessingAgreementEntity>()
                .FirstOrDefaultAsync(
                    e => e.ProcessorId == processorId && e.StatusValue == activeStatus,
                    cancellationToken);

            if (entity is null)
                return Right<EncinaError, Option<DataProcessingAgreement>>(None);

            var domain = DataProcessingAgreementMapper.ToDomain(entity);
            return domain is not null
                ? Right<EncinaError, Option<DataProcessingAgreement>>(Some(domain))
                : Right<EncinaError, Option<DataProcessingAgreement>>(None);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ProcessorAgreementErrors.StoreError("GetActiveDPAByProcessorId", ex.Message, ex);
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> UpdateAsync(
        DataProcessingAgreement agreement,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agreement);

        try
        {
            var existing = await _dbContext.Set<DataProcessingAgreementEntity>()
                .FirstOrDefaultAsync(e => e.Id == agreement.Id, cancellationToken);

            if (existing is null)
            {
                return ProcessorAgreementErrors.DPANotFound(agreement.Id);
            }

            var updated = DataProcessingAgreementMapper.ToEntity(agreement);

            existing.ProcessorId = updated.ProcessorId;
            existing.StatusValue = updated.StatusValue;
            existing.SignedAtUtc = updated.SignedAtUtc;
            existing.ExpiresAtUtc = updated.ExpiresAtUtc;
            existing.HasSCCs = updated.HasSCCs;
            existing.ProcessingPurposesJson = updated.ProcessingPurposesJson;
            existing.ProcessOnDocumentedInstructions = updated.ProcessOnDocumentedInstructions;
            existing.ConfidentialityObligations = updated.ConfidentialityObligations;
            existing.SecurityMeasures = updated.SecurityMeasures;
            existing.SubProcessorRequirements = updated.SubProcessorRequirements;
            existing.DataSubjectRightsAssistance = updated.DataSubjectRightsAssistance;
            existing.ComplianceAssistance = updated.ComplianceAssistance;
            existing.DataDeletionOrReturn = updated.DataDeletionOrReturn;
            existing.AuditRights = updated.AuditRights;
            existing.TenantId = updated.TenantId;
            existing.ModuleId = updated.ModuleId;
            existing.LastUpdatedAtUtc = updated.LastUpdatedAtUtc;

            await _dbContext.SaveChangesAsync(cancellationToken);
            return Right(Unit.Default);
        }
        catch (DbUpdateException ex)
        {
            return ProcessorAgreementErrors.StoreError("UpdateDPA", ex.Message, ex);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ProcessorAgreementErrors.StoreError("UpdateDPA", ex.Message, ex);
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<DataProcessingAgreement>>> GetByStatusAsync(
        DPAStatus status,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var statusValue = (int)status;

            var entities = await _dbContext.Set<DataProcessingAgreementEntity>()
                .Where(e => e.StatusValue == statusValue)
                .OrderBy(e => e.CreatedAtUtc)
                .ToListAsync(cancellationToken);

            var agreements = entities
                .Select(DataProcessingAgreementMapper.ToDomain)
                .Where(a => a is not null)
                .Cast<DataProcessingAgreement>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<DataProcessingAgreement>>(agreements);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ProcessorAgreementErrors.StoreError("GetDPAByStatus", ex.Message, ex);
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<DataProcessingAgreement>>> GetExpiringAsync(
        DateTimeOffset threshold,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var activeStatus = (int)DPAStatus.Active;

            var entities = await _dbContext.Set<DataProcessingAgreementEntity>()
                .Where(e => e.StatusValue == activeStatus
                    && e.ExpiresAtUtc != null
                    && e.ExpiresAtUtc <= threshold)
                .OrderBy(e => e.ExpiresAtUtc)
                .ToListAsync(cancellationToken);

            var agreements = entities
                .Select(DataProcessingAgreementMapper.ToDomain)
                .Where(a => a is not null)
                .Cast<DataProcessingAgreement>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<DataProcessingAgreement>>(agreements);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ProcessorAgreementErrors.StoreError("GetExpiringDPAs", ex.Message, ex);
        }
    }
}
