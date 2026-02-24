using Encina.Compliance.GDPR;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.LawfulBasis;

/// <summary>
/// Entity Framework Core implementation of <see cref="ILIAStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Uses EF Core LINQ queries for provider-agnostic LIA record management across
/// SQLite, SQL Server, PostgreSQL, and MySQL. All operations follow Railway Oriented
/// Programming with <c>Either&lt;EncinaError, T&gt;</c> return types.
/// </para>
/// <para>
/// Each write operation immediately persists via <see cref="DbContext.SaveChangesAsync"/>
/// to ensure LIA records are never lost.
/// </para>
/// </remarks>
public sealed class LIAStoreEF : ILIAStore
{
    private readonly DbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="LIAStoreEF"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public LIAStoreEF(DbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> StoreAsync(
        LIARecord record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        try
        {
            var entity = LIARecordMapper.ToEntity(record);
            var set = _dbContext.Set<LIARecordEntity>();

            var existing = await set.FindAsync([entity.Id], cancellationToken).ConfigureAwait(false);

            if (existing is not null)
            {
                existing.Name = entity.Name;
                existing.Purpose = entity.Purpose;
                existing.LegitimateInterest = entity.LegitimateInterest;
                existing.Benefits = entity.Benefits;
                existing.ConsequencesIfNotProcessed = entity.ConsequencesIfNotProcessed;
                existing.NecessityJustification = entity.NecessityJustification;
                existing.AlternativesConsideredJson = entity.AlternativesConsideredJson;
                existing.DataMinimisationNotes = entity.DataMinimisationNotes;
                existing.NatureOfData = entity.NatureOfData;
                existing.ReasonableExpectations = entity.ReasonableExpectations;
                existing.ImpactAssessment = entity.ImpactAssessment;
                existing.SafeguardsJson = entity.SafeguardsJson;
                existing.OutcomeValue = entity.OutcomeValue;
                existing.Conclusion = entity.Conclusion;
                existing.Conditions = entity.Conditions;
                existing.AssessedAtUtc = entity.AssessedAtUtc;
                existing.AssessedBy = entity.AssessedBy;
                existing.DPOInvolvement = entity.DPOInvolvement;
                existing.NextReviewAtUtc = entity.NextReviewAtUtc;
            }
            else
            {
                await set.AddAsync(entity, cancellationToken).ConfigureAwait(false);
            }

            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Right(Unit.Default);
        }
        catch (DbUpdateException ex)
        {
            return Left(GDPRErrors.LIAStoreError("Store", ex.Message));
        }
        catch (OperationCanceledException)
        {
            return Left(GDPRErrors.LIAStoreError("Store", "Operation was cancelled"));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<LIARecord>>> GetByReferenceAsync(
        string liaReference,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(liaReference);

        try
        {
            var entity = await _dbContext.Set<LIARecordEntity>()
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == liaReference, cancellationToken)
                .ConfigureAwait(false);

            if (entity is not null)
            {
                var domain = LIARecordMapper.ToDomain(entity);
                return Right<EncinaError, Option<LIARecord>>(Some(domain));
            }

            return Right<EncinaError, Option<LIARecord>>(None);
        }
        catch (Exception ex)
        {
            return Left(GDPRErrors.LIAStoreError("GetByReference", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<LIARecord>>> GetPendingReviewAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var requiredReviewValue = (int)LIAOutcome.RequiresReview;

            var entities = await _dbContext.Set<LIARecordEntity>()
                .AsNoTracking()
                .Where(e => e.OutcomeValue == requiredReviewValue)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var results = entities.Select(LIARecordMapper.ToDomain).ToList();
            return Right<EncinaError, IReadOnlyList<LIARecord>>(results);
        }
        catch (Exception ex)
        {
            return Left(GDPRErrors.LIAStoreError("GetPendingReview", ex.Message));
        }
    }
}
