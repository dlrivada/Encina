using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Model;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using static LanguageExt.Prelude;

namespace Encina.MongoDB.Retention;

/// <summary>
/// MongoDB implementation of <see cref="IRetentionPolicyStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Provides CRUD operations for <see cref="RetentionPolicy"/> records stored in a
/// MongoDB collection. Each policy defines how long data in a specific category
/// should be retained and whether automatic deletion is enabled.
/// </para>
/// <para>
/// Per GDPR Article 5(1)(e) (storage limitation), retention policies formalize
/// and enforce data retention periods per data category.
/// </para>
/// <para>
/// Uses MongoDB-specific features including filter builders for type-safe queries
/// and ReplaceOne for atomic updates.
/// </para>
/// </remarks>
public sealed class RetentionPolicyStoreMongoDB : IRetentionPolicyStore
{
    private readonly IMongoCollection<RetentionPolicyDocument> _collection;
    private readonly ILogger<RetentionPolicyStoreMongoDB> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetentionPolicyStoreMongoDB"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client.</param>
    /// <param name="options">The MongoDB options.</param>
    /// <param name="logger">The logger.</param>
    public RetentionPolicyStoreMongoDB(
        IMongoClient mongoClient,
        IOptions<EncinaMongoDbOptions> options,
        ILogger<RetentionPolicyStoreMongoDB> logger)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var config = options.Value;
        var database = mongoClient.GetDatabase(config.DatabaseName);
        _collection = database.GetCollection<RetentionPolicyDocument>(config.Collections.RetentionPolicies);
        _logger = logger;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> CreateAsync(
        RetentionPolicy policy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);

        try
        {
            var document = RetentionPolicyDocument.FromPolicy(policy);
            await _collection.InsertOneAsync(document, cancellationToken: cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("Created retention policy '{PolicyId}' for category '{DataCategory}'",
                policy.Id, policy.DataCategory);
            return Right(unit);
        }
        catch (Exception ex)
        {
            return Left(RetentionErrors.StoreError("CreatePolicy", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Option<RetentionPolicy>>> GetByIdAsync(
        string policyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyId);

        try
        {
            var filter = Builders<RetentionPolicyDocument>.Filter.Eq(d => d.Id, policyId);
            var document = await _collection
                .Find(filter)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (document is null)
            {
                return Right<EncinaError, Option<RetentionPolicy>>(None);
            }

            return Right<EncinaError, Option<RetentionPolicy>>(Some(document.ToPolicy()));
        }
        catch (Exception ex)
        {
            return Left<EncinaError, Option<RetentionPolicy>>(
                RetentionErrors.StoreError("GetPolicyById", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Option<RetentionPolicy>>> GetByCategoryAsync(
        string dataCategory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataCategory);

        try
        {
            var filter = Builders<RetentionPolicyDocument>.Filter.Eq(d => d.DataCategory, dataCategory);
            var document = await _collection
                .Find(filter)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (document is null)
            {
                return Right<EncinaError, Option<RetentionPolicy>>(None);
            }

            return Right<EncinaError, Option<RetentionPolicy>>(Some(document.ToPolicy()));
        }
        catch (Exception ex)
        {
            return Left<EncinaError, Option<RetentionPolicy>>(
                RetentionErrors.StoreError("GetPolicyByCategory", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<RetentionPolicy>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var documents = await _collection
                .Find(FilterDefinition<RetentionPolicyDocument>.Empty)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var policies = documents.Select(d => d.ToPolicy()).ToList();
            return Right<EncinaError, IReadOnlyList<RetentionPolicy>>(policies);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<RetentionPolicy>>(
                RetentionErrors.StoreError("GetAllPolicies", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> UpdateAsync(
        RetentionPolicy policy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);

        try
        {
            var document = RetentionPolicyDocument.FromPolicy(policy);
            var filter = Builders<RetentionPolicyDocument>.Filter.Eq(d => d.Id, policy.Id);
            var result = await _collection
                .ReplaceOneAsync(filter, document, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (result.MatchedCount == 0)
            {
                return Left(RetentionErrors.PolicyNotFound(policy.Id));
            }

            _logger.LogDebug("Updated retention policy '{PolicyId}'", policy.Id);
            return Right(unit);
        }
        catch (Exception ex)
        {
            return Left(RetentionErrors.StoreError("UpdatePolicy", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> DeleteAsync(
        string policyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyId);

        try
        {
            var filter = Builders<RetentionPolicyDocument>.Filter.Eq(d => d.Id, policyId);
            var result = await _collection
                .DeleteOneAsync(filter, cancellationToken)
                .ConfigureAwait(false);

            if (result.DeletedCount == 0)
            {
                return Left(RetentionErrors.PolicyNotFound(policyId));
            }

            _logger.LogDebug("Deleted retention policy '{PolicyId}'", policyId);
            return Right(unit);
        }
        catch (Exception ex)
        {
            return Left(RetentionErrors.StoreError("DeletePolicy", ex.Message, ex));
        }
    }
}
