using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MongoDB.Driver;

using static LanguageExt.Prelude;

namespace Encina.MongoDB.DataResidency;

/// <summary>
/// MongoDB implementation of <see cref="IResidencyPolicyStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Provides CRUD operations for <see cref="ResidencyPolicyDescriptor"/> records stored in a
/// MongoDB collection. Each policy defines which regions are allowed for a specific data
/// category, whether adequacy decisions are required, and which transfer legal bases are
/// permitted for cross-border transfers.
/// </para>
/// <para>
/// Per GDPR Article 44 (general principle for transfers), any transfer of personal data to a
/// third country shall take place only if the conditions of Chapter V are complied with.
/// Per Article 45, transfers may proceed on the basis of an adequacy decision.
/// Per Article 46, transfers may use appropriate safeguards such as SCCs or BCRs.
/// Residency policy descriptors encode these conditions as enforceable rules.
/// </para>
/// <para>
/// Uses <see cref="ResidencyPolicyEntity"/> as the MongoDB document type and
/// <see cref="ResidencyPolicyMapper"/> for domain-entity conversion. The
/// <see cref="ResidencyPolicyEntity.DataCategory"/> field acts as the natural key
/// for lookups and uniqueness enforcement.
/// </para>
/// </remarks>
public sealed class ResidencyPolicyStoreMongoDB : IResidencyPolicyStore
{
    private readonly IMongoCollection<ResidencyPolicyEntity> _collection;
    private readonly ILogger<ResidencyPolicyStoreMongoDB> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResidencyPolicyStoreMongoDB"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client.</param>
    /// <param name="options">The MongoDB options.</param>
    /// <param name="logger">The logger.</param>
    public ResidencyPolicyStoreMongoDB(
        IMongoClient mongoClient,
        IOptions<EncinaMongoDbOptions> options,
        ILogger<ResidencyPolicyStoreMongoDB> logger)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var config = options.Value;
        var database = mongoClient.GetDatabase(config.DatabaseName);
        _collection = database.GetCollection<ResidencyPolicyEntity>(config.Collections.ResidencyPolicies);
        _logger = logger;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> CreateAsync(
        ResidencyPolicyDescriptor policy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);

        try
        {
            var entity = ResidencyPolicyMapper.ToEntity(policy);
            await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken).ConfigureAwait(false);

            _logger.LogDebug(
                "Created residency policy for data category '{DataCategory}'",
                policy.DataCategory);
            return Right(unit);
        }
        catch (Exception ex)
        {
            return Left(DataResidencyErrors.StoreError("CreatePolicy", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Option<ResidencyPolicyDescriptor>>> GetByCategoryAsync(
        string dataCategory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataCategory);

        try
        {
            var filter = Builders<ResidencyPolicyEntity>.Filter.Eq(d => d.DataCategory, dataCategory);
            var document = await _collection
                .Find(filter)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (document is null)
            {
                return Right<EncinaError, Option<ResidencyPolicyDescriptor>>(None);
            }

            var policy = ResidencyPolicyMapper.ToDomain(document);
            if (policy is null)
            {
                return Right<EncinaError, Option<ResidencyPolicyDescriptor>>(None);
            }

            return Right<EncinaError, Option<ResidencyPolicyDescriptor>>(Some(policy));
        }
        catch (Exception ex)
        {
            return Left<EncinaError, Option<ResidencyPolicyDescriptor>>(
                DataResidencyErrors.StoreError("GetPolicyByCategory", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<ResidencyPolicyDescriptor>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var documents = await _collection
                .Find(FilterDefinition<ResidencyPolicyEntity>.Empty)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var policies = documents
                .Select(ResidencyPolicyMapper.ToDomain)
                .Where(p => p is not null)
                .Cast<ResidencyPolicyDescriptor>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<ResidencyPolicyDescriptor>>(policies);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<ResidencyPolicyDescriptor>>(
                DataResidencyErrors.StoreError("GetAllPolicies", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> UpdateAsync(
        ResidencyPolicyDescriptor policy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);

        try
        {
            var entity = ResidencyPolicyMapper.ToEntity(policy);
            entity.LastModifiedAtUtc = DateTimeOffset.UtcNow;

            var filter = Builders<ResidencyPolicyEntity>.Filter.Eq(d => d.DataCategory, policy.DataCategory);
            var result = await _collection
                .ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (result.MatchedCount == 0)
            {
                return Left(DataResidencyErrors.PolicyNotFound(policy.DataCategory));
            }

            _logger.LogDebug(
                "Updated residency policy for data category '{DataCategory}'",
                policy.DataCategory);
            return Right(unit);
        }
        catch (Exception ex)
        {
            return Left(DataResidencyErrors.StoreError("UpdatePolicy", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> DeleteAsync(
        string dataCategory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataCategory);

        try
        {
            var filter = Builders<ResidencyPolicyEntity>.Filter.Eq(d => d.DataCategory, dataCategory);
            var result = await _collection
                .DeleteOneAsync(filter, cancellationToken)
                .ConfigureAwait(false);

            if (result.DeletedCount == 0)
            {
                return Left(DataResidencyErrors.PolicyNotFound(dataCategory));
            }

            _logger.LogDebug(
                "Deleted residency policy for data category '{DataCategory}'",
                dataCategory);
            return Right(unit);
        }
        catch (Exception ex)
        {
            return Left(DataResidencyErrors.StoreError("DeletePolicy", ex.Message, ex));
        }
    }
}
