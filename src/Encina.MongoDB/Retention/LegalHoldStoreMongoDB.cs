using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Model;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using static LanguageExt.Prelude;

namespace Encina.MongoDB.Retention;

/// <summary>
/// MongoDB implementation of <see cref="ILegalHoldStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Provides CRUD operations for <see cref="LegalHold"/> records stored in a MongoDB
/// collection. Legal holds suspend data deletion for specific entities as required
/// by GDPR Article 17(3)(e) for legal claims.
/// </para>
/// <para>
/// Uses MongoDB filter builders for efficient queries, particularly for the
/// <see cref="IsUnderHoldAsync"/> check which uses <c>CountDocumentsAsync</c>
/// for lightweight existence verification during enforcement cycles.
/// </para>
/// </remarks>
public sealed class LegalHoldStoreMongoDB : ILegalHoldStore
{
    private readonly IMongoCollection<LegalHoldDocument> _collection;
    private readonly ILogger<LegalHoldStoreMongoDB> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LegalHoldStoreMongoDB"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client.</param>
    /// <param name="options">The MongoDB options.</param>
    /// <param name="logger">The logger.</param>
    public LegalHoldStoreMongoDB(
        IMongoClient mongoClient,
        IOptions<EncinaMongoDbOptions> options,
        ILogger<LegalHoldStoreMongoDB> logger)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var config = options.Value;
        var database = mongoClient.GetDatabase(config.DatabaseName);
        _collection = database.GetCollection<LegalHoldDocument>(config.Collections.LegalHolds);
        _logger = logger;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> CreateAsync(
        LegalHold hold,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(hold);

        try
        {
            var document = LegalHoldDocument.FromHold(hold);
            await _collection.InsertOneAsync(document, cancellationToken: cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("Created legal hold '{HoldId}' for entity '{EntityId}'", hold.Id, hold.EntityId);
            return Right(unit);
        }
        catch (Exception ex)
        {
            return Left(RetentionErrors.StoreError("CreateHold", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Option<LegalHold>>> GetByIdAsync(
        string holdId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(holdId);

        try
        {
            var filter = Builders<LegalHoldDocument>.Filter.Eq(d => d.Id, holdId);
            var document = await _collection
                .Find(filter)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (document is null)
            {
                return Right<EncinaError, Option<LegalHold>>(None);
            }

            return Right<EncinaError, Option<LegalHold>>(Some(document.ToHold()));
        }
        catch (Exception ex)
        {
            return Left<EncinaError, Option<LegalHold>>(
                RetentionErrors.StoreError("GetHoldById", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<LegalHold>>> GetByEntityIdAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        try
        {
            var filter = Builders<LegalHoldDocument>.Filter.Eq(d => d.EntityId, entityId);
            var documents = await _collection
                .Find(filter)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var holds = documents.Select(d => d.ToHold()).ToList();
            return Right<EncinaError, IReadOnlyList<LegalHold>>(holds);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<LegalHold>>(
                RetentionErrors.StoreError("GetHoldsByEntityId", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, bool>> IsUnderHoldAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        try
        {
            var filterBuilder = Builders<LegalHoldDocument>.Filter;
            var filter = filterBuilder.Eq(d => d.EntityId, entityId)
                & filterBuilder.Eq(d => d.ReleasedAtUtc, null);

            var count = await _collection
                .CountDocumentsAsync(filter, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return Right<EncinaError, bool>(count > 0);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, bool>(
                RetentionErrors.StoreError("IsUnderHold", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<LegalHold>>> GetActiveHoldsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<LegalHoldDocument>.Filter.Eq(d => d.ReleasedAtUtc, null);
            var documents = await _collection
                .Find(filter)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var holds = documents.Select(d => d.ToHold()).ToList();

            _logger.LogDebug("Retrieved {Count} active legal holds", holds.Count);
            return Right<EncinaError, IReadOnlyList<LegalHold>>(holds);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<LegalHold>>(
                RetentionErrors.StoreError("GetActiveHolds", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> ReleaseAsync(
        string holdId,
        string? releasedByUserId,
        DateTimeOffset releasedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(holdId);

        try
        {
            var filterBuilder = Builders<LegalHoldDocument>.Filter;
            var filter = filterBuilder.Eq(d => d.Id, holdId)
                & filterBuilder.Eq(d => d.ReleasedAtUtc, null);

            var update = Builders<LegalHoldDocument>.Update
                .Set(d => d.ReleasedAtUtc, releasedAtUtc.UtcDateTime)
                .Set(d => d.ReleasedByUserId, releasedByUserId);

            var result = await _collection
                .UpdateOneAsync(filter, update, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (result.MatchedCount == 0)
            {
                // Check if the hold exists at all or was already released
                var existsFilter = filterBuilder.Eq(d => d.Id, holdId);
                var existsCount = await _collection
                    .CountDocumentsAsync(existsFilter, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                return existsCount == 0
                    ? Left(RetentionErrors.HoldNotFound(holdId))
                    : Left(RetentionErrors.HoldAlreadyReleased(holdId));
            }

            _logger.LogDebug("Released legal hold '{HoldId}'", holdId);
            return Right(unit);
        }
        catch (Exception ex)
        {
            return Left(RetentionErrors.StoreError("ReleaseHold", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<LegalHold>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var documents = await _collection
                .Find(FilterDefinition<LegalHoldDocument>.Empty)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var holds = documents.Select(d => d.ToHold()).ToList();
            return Right<EncinaError, IReadOnlyList<LegalHold>>(holds);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<LegalHold>>(
                RetentionErrors.StoreError("GetAllHolds", ex.Message, ex));
        }
    }
}
