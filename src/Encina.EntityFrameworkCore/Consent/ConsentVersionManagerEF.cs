using Encina.Compliance.Consent;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.Consent;

/// <summary>
/// Entity Framework Core implementation of <see cref="IConsentVersionManager"/>.
/// </summary>
/// <remarks>
/// <para>
/// Manages consent term versions and reconsent requirements. When a new version is published
/// with <see cref="ConsentVersion.RequiresExplicitReconsent"/> set to <c>true</c>, all active
/// consents for the affected purpose are transitioned to <see cref="ConsentStatus.RequiresReconsent"/>
/// using <see cref="RelationalQueryableExtensions.ExecuteUpdateAsync"/>.
/// </para>
/// </remarks>
public sealed class ConsentVersionManagerEF : IConsentVersionManager
{
    private readonly DbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsentVersionManagerEF"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public ConsentVersionManagerEF(DbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, ConsentVersion>> GetCurrentVersionAsync(
        string purpose,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        try
        {
            var entity = await _dbContext.Set<ConsentVersionEntity>()
                .Where(e => e.Purpose == purpose)
                .OrderByDescending(e => e.EffectiveFromUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (entity is null)
            {
                return Left(ConsentErrors.MissingConsent("system", purpose));
            }

            return Right<EncinaError, ConsentVersion>(MapToRecord(entity));
        }
        catch (OperationCanceledException)
        {
            return Left<EncinaError, ConsentVersion>(EncinaErrors.Create(
                code: "consent.version_store_error",
                message: "Operation was cancelled"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> PublishNewVersionAsync(
        ConsentVersion version,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(version);

        try
        {
            var entity = MapToEntity(version);
            await _dbContext.Set<ConsentVersionEntity>().AddAsync(entity, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (version.RequiresExplicitReconsent)
            {
                await _dbContext.Set<ConsentRecordEntity>()
                    .Where(e => e.Purpose == version.Purpose &&
                                e.Status == ConsentStatus.Active &&
                                e.ConsentVersionId != version.VersionId)
                    .ExecuteUpdateAsync(
                        e => e.SetProperty(x => x.Status, ConsentStatus.RequiresReconsent),
                        cancellationToken);
            }

            return Right(Unit.Default);
        }
        catch (DbUpdateException ex)
        {
            return Left(EncinaErrors.Create(
                code: "consent.version_store_error",
                message: $"Failed to publish consent version: {ex.Message}"));
        }
        catch (OperationCanceledException)
        {
            return Left(EncinaErrors.Create(
                code: "consent.version_store_error",
                message: "Operation was cancelled"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, bool>> RequiresReconsentAsync(
        string subjectId,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        try
        {
            var consent = await _dbContext.Set<ConsentRecordEntity>()
                .FirstOrDefaultAsync(e => e.SubjectId == subjectId && e.Purpose == purpose, cancellationToken);

            if (consent is null)
            {
                return Right<EncinaError, bool>(false);
            }

            var currentVersion = await _dbContext.Set<ConsentVersionEntity>()
                .Where(e => e.Purpose == purpose)
                .OrderByDescending(e => e.EffectiveFromUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (currentVersion is null)
            {
                return Right<EncinaError, bool>(false);
            }

            var requiresReconsent = consent.ConsentVersionId != currentVersion.VersionId &&
                                    currentVersion.RequiresExplicitReconsent;

            return Right<EncinaError, bool>(requiresReconsent);
        }
        catch (OperationCanceledException)
        {
            return Left<EncinaError, bool>(EncinaErrors.Create(
                code: "consent.version_store_error",
                message: "Operation was cancelled"));
        }
    }

    internal static ConsentVersionEntity MapToEntity(ConsentVersion version) => new()
    {
        VersionId = version.VersionId,
        Purpose = version.Purpose,
        EffectiveFromUtc = version.EffectiveFromUtc,
        Description = version.Description,
        RequiresExplicitReconsent = version.RequiresExplicitReconsent
    };

    internal static ConsentVersion MapToRecord(ConsentVersionEntity entity) => new()
    {
        VersionId = entity.VersionId,
        Purpose = entity.Purpose,
        EffectiveFromUtc = entity.EffectiveFromUtc,
        Description = entity.Description,
        RequiresExplicitReconsent = entity.RequiresExplicitReconsent
    };
}
