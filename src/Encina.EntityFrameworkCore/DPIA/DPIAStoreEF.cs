using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;
using Encina.Messaging;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.DPIA;

/// <summary>
/// Entity Framework Core implementation of <see cref="IDPIAStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Uses EF Core LINQ queries for provider-agnostic DPIA assessment management across
/// SQLite, SQL Server, PostgreSQL, and MySQL. All operations follow Railway Oriented
/// Programming with <c>Either&lt;EncinaError, T&gt;</c> return types.
/// </para>
/// <para>
/// Each write operation immediately persists via <see cref="DbContext.SaveChangesAsync"/>
/// to ensure assessment records are never lost. The store uses
/// <see cref="DPIAAssessmentMapper"/> to convert between domain and persistence models.
/// </para>
/// <para>
/// Upsert semantics: <see cref="SaveAssessmentAsync"/> uses EF Core's change tracking
/// to determine whether to insert or update the entity.
/// </para>
/// </remarks>
public sealed class DPIAStoreEF : IDPIAStore
{
    private readonly DbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="DPIAStoreEF"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public DPIAStoreEF(DbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> SaveAssessmentAsync(
        DPIAAssessment assessment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assessment);

        try
        {
            var entity = DPIAAssessmentMapper.ToEntity(assessment);
            var existing = await _dbContext.Set<DPIAAssessmentEntity>()
                .FirstOrDefaultAsync(e => e.Id == entity.Id, cancellationToken);

            if (existing is null)
            {
                await _dbContext.Set<DPIAAssessmentEntity>().AddAsync(entity, cancellationToken);
            }
            else
            {
                existing.RequestTypeName = entity.RequestTypeName;
                existing.StatusValue = entity.StatusValue;
                existing.ProcessingType = entity.ProcessingType;
                existing.Reason = entity.Reason;
                existing.ResultJson = entity.ResultJson;
                existing.DPOConsultationJson = entity.DPOConsultationJson;
                existing.CreatedAtUtc = entity.CreatedAtUtc;
                existing.ApprovedAtUtc = entity.ApprovedAtUtc;
                existing.NextReviewAtUtc = entity.NextReviewAtUtc;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return Right(Unit.Default);
        }
        catch (DbUpdateException ex)
        {
            return Left(EncinaErrors.Create(
                code: "dpia.store_error",
                message: $"Failed to save DPIA assessment: {ex.Message}",
                details: new Dictionary<string, object?> { ["assessmentId"] = assessment.Id }));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "dpia.store_error",
                message: $"Failed to save DPIA assessment: {ex.Message}",
                details: new Dictionary<string, object?> { ["assessmentId"] = assessment.Id }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Option<DPIAAssessment>>> GetAssessmentAsync(
        string requestTypeName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestTypeName);

        try
        {
            var entity = await _dbContext.Set<DPIAAssessmentEntity>()
                .FirstOrDefaultAsync(e => e.RequestTypeName == requestTypeName, cancellationToken);

            if (entity is null)
                return Right<EncinaError, Option<DPIAAssessment>>(None);

            return Right<EncinaError, Option<DPIAAssessment>>(Some(DPIAAssessmentMapper.ToDomain(entity)!));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left<EncinaError, Option<DPIAAssessment>>(EncinaErrors.Create(
                code: "dpia.store_error",
                message: $"Failed to retrieve DPIA assessment: {ex.Message}",
                details: new Dictionary<string, object?> { ["requestTypeName"] = requestTypeName }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Option<DPIAAssessment>>> GetAssessmentByIdAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var idStr = assessmentId.ToString("D");
            var entity = await _dbContext.Set<DPIAAssessmentEntity>()
                .FirstOrDefaultAsync(e => e.Id == idStr, cancellationToken);

            if (entity is null)
                return Right<EncinaError, Option<DPIAAssessment>>(None);

            return Right<EncinaError, Option<DPIAAssessment>>(Some(DPIAAssessmentMapper.ToDomain(entity)!));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left<EncinaError, Option<DPIAAssessment>>(EncinaErrors.Create(
                code: "dpia.store_error",
                message: $"Failed to retrieve DPIA assessment by ID: {ex.Message}",
                details: new Dictionary<string, object?> { ["assessmentId"] = assessmentId }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<DPIAAssessment>>> GetExpiredAssessmentsAsync(
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var approvedStatus = (int)DPIAAssessmentStatus.Approved;

            var entities = await _dbContext.Set<DPIAAssessmentEntity>()
                .Where(e => e.NextReviewAtUtc != null && e.NextReviewAtUtc < nowUtc && e.StatusValue == approvedStatus)
                .OrderBy(e => e.NextReviewAtUtc)
                .ToListAsync(cancellationToken);

            var assessments = entities
                .Select(DPIAAssessmentMapper.ToDomain)
                .Where(a => a is not null)
                .Cast<DPIAAssessment>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<DPIAAssessment>>(assessments);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<DPIAAssessment>>(EncinaErrors.Create(
                code: "dpia.store_error",
                message: $"Failed to retrieve expired DPIA assessments: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<DPIAAssessment>>> GetAllAssessmentsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _dbContext.Set<DPIAAssessmentEntity>()
                .ToListAsync(cancellationToken);

            var assessments = entities
                .Select(DPIAAssessmentMapper.ToDomain)
                .Where(a => a is not null)
                .Cast<DPIAAssessment>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<DPIAAssessment>>(assessments);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<DPIAAssessment>>(EncinaErrors.Create(
                code: "dpia.store_error",
                message: $"Failed to retrieve DPIA assessments: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> DeleteAssessmentAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var idStr = assessmentId.ToString("D");
            var existing = await _dbContext.Set<DPIAAssessmentEntity>()
                .FirstOrDefaultAsync(e => e.Id == idStr, cancellationToken);

            if (existing is null)
            {
                return Left(EncinaErrors.Create(
                    code: "dpia.not_found",
                    message: $"DPIA assessment '{assessmentId}' not found",
                    details: new Dictionary<string, object?> { ["assessmentId"] = assessmentId }));
            }

            _dbContext.Set<DPIAAssessmentEntity>().Remove(existing);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Right(Unit.Default);
        }
        catch (DbUpdateException ex)
        {
            return Left(EncinaErrors.Create(
                code: "dpia.store_error",
                message: $"Failed to delete DPIA assessment: {ex.Message}",
                details: new Dictionary<string, object?> { ["assessmentId"] = assessmentId }));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "dpia.store_error",
                message: $"Failed to delete DPIA assessment: {ex.Message}",
                details: new Dictionary<string, object?> { ["assessmentId"] = assessmentId }));
        }
    }
}
