using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Model;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using static LanguageExt.Prelude;

namespace Encina.MongoDB.Retention;

/// <summary>
/// MongoDB implementation of <see cref="IRetentionRecordStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Manages <see cref="RetentionRecord"/> instances that track the retention lifecycle
/// of individual data entities. Supports querying for expired records, records expiring
/// within a time window, and status updates.
/// </para>
/// <para>
/// Per GDPR Article 5(1)(e) (storage limitation), this store enables controllers to
/// demonstrate that personal data is not kept longer than necessary by maintaining an
/// auditable trail of data creation, expiration, and deletion timestamps.
/// </para>
/// <para>
/// Uses <see cref="TimeProvider"/> for time-based queries to support deterministic
/// testing and consistent UTC time resolution.
/// </para>
/// </remarks>
public sealed class RetentionRecordStoreMongoDB : IRetentionRecordStore
{
    private readonly IMongoCollection<RetentionRecordDocument> _collection;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RetentionRecordStoreMongoDB> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetentionRecordStoreMongoDB"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client.</param>
    /// <param name="options">The MongoDB options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time provider for time-based queries. Defaults to <see cref="TimeProvider.System"/>.</param>
    public RetentionRecordStoreMongoDB(
        IMongoClient mongoClient,
        IOptions<EncinaMongoDbOptions> options,
        ILogger<RetentionRecordStoreMongoDB> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var config = options.Value;
        var database = mongoClient.GetDatabase(config.DatabaseName);
        _collection = database.GetCollection<RetentionRecordDocument>(config.Collections.RetentionRecords);
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> CreateAsync(
        RetentionRecord record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        try
        {
            var document = RetentionRecordDocument.FromRecord(record);
            await _collection.InsertOneAsync(document, cancellationToken: cancellationToken).ConfigureAwait(false);

            _logger.LogDebug(
                "Created retention record '{RecordId}' for entity '{EntityId}' in category '{DataCategory}'",
                record.Id, record.EntityId, record.DataCategory);
            return Right(unit);
        }
        catch (Exception ex)
        {
            return Left(RetentionErrors.StoreError("CreateRecord", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Option<RetentionRecord>>> GetByIdAsync(
        string recordId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recordId);

        try
        {
            var filter = Builders<RetentionRecordDocument>.Filter.Eq(d => d.Id, recordId);
            var document = await _collection
                .Find(filter)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (document is null)
            {
                return Right<EncinaError, Option<RetentionRecord>>(None);
            }

            return Right<EncinaError, Option<RetentionRecord>>(Some(document.ToRecord()));
        }
        catch (Exception ex)
        {
            return Left<EncinaError, Option<RetentionRecord>>(
                RetentionErrors.StoreError("GetRecordById", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<RetentionRecord>>> GetByEntityIdAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        try
        {
            var filter = Builders<RetentionRecordDocument>.Filter.Eq(d => d.EntityId, entityId);
            var documents = await _collection
                .Find(filter)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var records = documents.Select(d => d.ToRecord()).ToList();
            return Right<EncinaError, IReadOnlyList<RetentionRecord>>(records);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<RetentionRecord>>(
                RetentionErrors.StoreError("GetRecordsByEntityId", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<RetentionRecord>>> GetExpiredRecordsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var filterBuilder = Builders<RetentionRecordDocument>.Filter;
            var filter = filterBuilder.Lt(d => d.ExpiresAtUtc, nowUtc)
                & filterBuilder.Eq(d => d.StatusValue, (int)RetentionStatus.Active);

            var documents = await _collection
                .Find(filter)
                .SortBy(d => d.ExpiresAtUtc)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var records = documents.Select(d => d.ToRecord()).ToList();

            _logger.LogDebug("Retrieved {Count} expired retention records", records.Count);
            return Right<EncinaError, IReadOnlyList<RetentionRecord>>(records);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<RetentionRecord>>(
                RetentionErrors.StoreError("GetExpiredRecords", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<RetentionRecord>>> GetExpiringWithinAsync(
        TimeSpan within,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var deadlineUtc = nowUtc.Add(within);
            var filterBuilder = Builders<RetentionRecordDocument>.Filter;

            var filter = filterBuilder.Gte(d => d.ExpiresAtUtc, nowUtc)
                & filterBuilder.Lte(d => d.ExpiresAtUtc, deadlineUtc)
                & filterBuilder.Eq(d => d.StatusValue, (int)RetentionStatus.Active);

            var documents = await _collection
                .Find(filter)
                .SortBy(d => d.ExpiresAtUtc)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var records = documents.Select(d => d.ToRecord()).ToList();

            _logger.LogDebug("Retrieved {Count} retention records expiring within {Within}", records.Count, within);
            return Right<EncinaError, IReadOnlyList<RetentionRecord>>(records);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<RetentionRecord>>(
                RetentionErrors.StoreError("GetExpiringWithin", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> UpdateStatusAsync(
        string recordId,
        RetentionStatus newStatus,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recordId);

        try
        {
            var filter = Builders<RetentionRecordDocument>.Filter.Eq(d => d.Id, recordId);
            var updateDefinition = Builders<RetentionRecordDocument>.Update
                .Set(d => d.StatusValue, (int)newStatus);

            if (newStatus == RetentionStatus.Deleted)
            {
                var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
                updateDefinition = updateDefinition.Set(d => d.DeletedAtUtc, nowUtc);
            }

            var result = await _collection
                .UpdateOneAsync(filter, updateDefinition, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (result.MatchedCount == 0)
            {
                return Left(RetentionErrors.RecordNotFound(recordId));
            }

            _logger.LogDebug("Updated retention record '{RecordId}' status to {NewStatus}", recordId, newStatus);
            return Right(unit);
        }
        catch (Exception ex)
        {
            return Left(RetentionErrors.StoreError("UpdateRecordStatus", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<RetentionRecord>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var documents = await _collection
                .Find(FilterDefinition<RetentionRecordDocument>.Empty)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var records = documents.Select(d => d.ToRecord()).ToList();
            return Right<EncinaError, IReadOnlyList<RetentionRecord>>(records);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<RetentionRecord>>(
                RetentionErrors.StoreError("GetAllRecords", ex.Message, ex));
        }
    }
}
