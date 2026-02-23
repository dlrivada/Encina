using Encina.Compliance.Consent;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using static LanguageExt.Prelude;

namespace Encina.MongoDB.Consent;

/// <summary>
/// MongoDB implementation of <see cref="IConsentVersionManager"/>.
/// </summary>
/// <remarks>
/// <para>
/// Manages consent term versions and reconsent requirements. When a new version is published
/// with <see cref="ConsentVersion.RequiresExplicitReconsent"/> set to <c>true</c>, all active
/// consents for the affected purpose are transitioned to <see cref="ConsentStatus.RequiresReconsent"/>
/// using <c>UpdateManyAsync</c>.
/// </para>
/// <para>
/// This implementation requires access to both the consent versions collection and the consent
/// records collection (for the reconsent status update in <see cref="PublishNewVersionAsync"/>).
/// </para>
/// </remarks>
public sealed class ConsentVersionManagerMongoDB : IConsentVersionManager
{
    private readonly IMongoCollection<ConsentVersionDocument> _versionsCollection;
    private readonly IMongoCollection<ConsentRecordDocument> _consentsCollection;
    private readonly ILogger<ConsentVersionManagerMongoDB> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsentVersionManagerMongoDB"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client.</param>
    /// <param name="options">The MongoDB options.</param>
    /// <param name="logger">The logger.</param>
    public ConsentVersionManagerMongoDB(
        IMongoClient mongoClient,
        IOptions<EncinaMongoDbOptions> options,
        ILogger<ConsentVersionManagerMongoDB> logger)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var config = options.Value;
        var database = mongoClient.GetDatabase(config.DatabaseName);
        _versionsCollection = database.GetCollection<ConsentVersionDocument>(config.Collections.ConsentVersions);
        _consentsCollection = database.GetCollection<ConsentRecordDocument>(config.Collections.Consents);
        _logger = logger;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, ConsentVersion>> GetCurrentVersionAsync(
        string purpose,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        try
        {
            var filter = Builders<ConsentVersionDocument>.Filter.Eq(d => d.Purpose, purpose);

            var document = await _versionsCollection
                .Find(filter)
                .SortByDescending(d => d.EffectiveFromUtc)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (document is null)
            {
                return Left(ConsentErrors.MissingConsent("system", purpose));
            }

            return Right<EncinaError, ConsentVersion>(document.ToVersion());
        }
        catch (Exception ex)
        {
            Log.ConsentStoreOperationFailed(_logger, ex, "ConsentVersionManager.GetCurrentVersionAsync");
            return Left<EncinaError, ConsentVersion>(EncinaErrors.Create(
                code: "consent.version_store_error",
                message: $"Failed to get current consent version: {ex.Message}"));
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
            var document = ConsentVersionDocument.FromVersion(version);
            await _versionsCollection.InsertOneAsync(document, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (version.RequiresExplicitReconsent)
            {
                var filter = Builders<ConsentRecordDocument>.Filter.And(
                    Builders<ConsentRecordDocument>.Filter.Eq(d => d.Purpose, version.Purpose),
                    Builders<ConsentRecordDocument>.Filter.Eq(d => d.Status, (int)ConsentStatus.Active),
                    Builders<ConsentRecordDocument>.Filter.Ne(d => d.ConsentVersionId, version.VersionId));

                var update = Builders<ConsentRecordDocument>.Update
                    .Set(d => d.Status, (int)ConsentStatus.RequiresReconsent);

                await _consentsCollection
                    .UpdateManyAsync(filter, update, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }

            Log.PublishedConsentVersion(_logger, version.VersionId, version.Purpose);
            return Right(unit);
        }
        catch (Exception ex)
        {
            Log.ConsentStoreOperationFailed(_logger, ex, "ConsentVersionManager.PublishNewVersionAsync");
            return Left(EncinaErrors.Create(
                code: "consent.version_store_error",
                message: $"Failed to publish consent version: {ex.Message}"));
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
            // Get the consent record for this subject-purpose pair
            var consentFilter = Builders<ConsentRecordDocument>.Filter.And(
                Builders<ConsentRecordDocument>.Filter.Eq(d => d.SubjectId, subjectId),
                Builders<ConsentRecordDocument>.Filter.Eq(d => d.Purpose, purpose));

            var consent = await _consentsCollection
                .Find(consentFilter)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (consent is null)
            {
                return Right<EncinaError, bool>(false);
            }

            // Get the current version for this purpose
            var versionFilter = Builders<ConsentVersionDocument>.Filter.Eq(d => d.Purpose, purpose);

            var currentVersion = await _versionsCollection
                .Find(versionFilter)
                .SortByDescending(d => d.EffectiveFromUtc)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (currentVersion is null)
            {
                return Right<EncinaError, bool>(false);
            }

            var requiresReconsent = consent.ConsentVersionId != currentVersion.VersionId &&
                                    currentVersion.RequiresExplicitReconsent;

            return Right<EncinaError, bool>(requiresReconsent);
        }
        catch (Exception ex)
        {
            Log.ConsentStoreOperationFailed(_logger, ex, "ConsentVersionManager.RequiresReconsentAsync");
            return Left<EncinaError, bool>(EncinaErrors.Create(
                code: "consent.version_store_error",
                message: $"Failed to check reconsent requirement: {ex.Message}"));
        }
    }
}
