using Encina.Compliance.Consent;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using static LanguageExt.Prelude;

namespace Encina.MongoDB.Consent;

/// <summary>
/// MongoDB implementation of <see cref="IConsentStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses MongoDB-specific features:
/// <list type="bullet">
/// <item><description>Typed <see cref="IMongoCollection{T}"/> for consent record documents</description></item>
/// <item><description>Filter and update builders for type-safe queries</description></item>
/// <item><description>ReplaceOne with upsert for idempotent consent recording</description></item>
/// <item><description>BulkWrite for efficient batch operations</description></item>
/// </list>
/// </para>
/// <para>
/// A unique compound index on <c>{ subject_id: 1, purpose: 1 }</c> ensures at most one
/// consent record per subject-purpose pair. The <see cref="RecordConsentAsync"/> method
/// uses <c>ReplaceOneAsync</c> with upsert to handle both insert and update scenarios.
/// </para>
/// </remarks>
public sealed class ConsentStoreMongoDB : IConsentStore
{
    private readonly IMongoCollection<ConsentRecordDocument> _collection;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ConsentStoreMongoDB> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsentStoreMongoDB"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client.</param>
    /// <param name="options">The MongoDB options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time provider for expiration checks.</param>
    public ConsentStoreMongoDB(
        IMongoClient mongoClient,
        IOptions<EncinaMongoDbOptions> options,
        ILogger<ConsentStoreMongoDB> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var config = options.Value;
        var database = mongoClient.GetDatabase(config.DatabaseName);
        _collection = database.GetCollection<ConsentRecordDocument>(config.Collections.Consents);
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> RecordConsentAsync(
        ConsentRecord consent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consent);

        try
        {
            var document = ConsentRecordDocument.FromRecord(consent);
            var filter = Builders<ConsentRecordDocument>.Filter.Eq(d => d.Id, consent.Id);

            await _collection.ReplaceOneAsync(
                filter,
                document,
                new ReplaceOptions { IsUpsert = true },
                cancellationToken).ConfigureAwait(false);

            Log.RecordedConsent(_logger, consent.SubjectId, consent.Purpose);
            return Right(unit);
        }
        catch (Exception ex)
        {
            Log.ConsentStoreOperationFailed(_logger, ex, "RecordConsentAsync");
            return Left(EncinaErrors.Create(
                code: "consent.store_error",
                message: $"Failed to record consent: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Option<ConsentRecord>>> GetConsentAsync(
        string subjectId,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        try
        {
            var filter = Builders<ConsentRecordDocument>.Filter.And(
                Builders<ConsentRecordDocument>.Filter.Eq(d => d.SubjectId, subjectId),
                Builders<ConsentRecordDocument>.Filter.Eq(d => d.Purpose, purpose));

            var document = await _collection
                .Find(filter)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (document is null)
            {
                return Right<EncinaError, Option<ConsentRecord>>(None);
            }

            Log.RetrievedConsent(_logger, subjectId, purpose);
            return Right<EncinaError, Option<ConsentRecord>>(Some(document.ToRecord()));
        }
        catch (Exception ex)
        {
            Log.ConsentStoreOperationFailed(_logger, ex, "GetConsentAsync");
            return Left<EncinaError, Option<ConsentRecord>>(EncinaErrors.Create(
                code: "consent.store_error",
                message: $"Failed to retrieve consent: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<ConsentRecord>>> GetAllConsentsAsync(
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);

        try
        {
            var filter = Builders<ConsentRecordDocument>.Filter.Eq(d => d.SubjectId, subjectId);

            var documents = await _collection
                .Find(filter)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var records = documents.Select(d => d.ToRecord()).ToList();
            return Right<EncinaError, IReadOnlyList<ConsentRecord>>(records);
        }
        catch (Exception ex)
        {
            Log.ConsentStoreOperationFailed(_logger, ex, "GetAllConsentsAsync");
            return Left<EncinaError, IReadOnlyList<ConsentRecord>>(EncinaErrors.Create(
                code: "consent.store_error",
                message: $"Failed to retrieve consents: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> WithdrawConsentAsync(
        string subjectId,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        try
        {
            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var filter = Builders<ConsentRecordDocument>.Filter.And(
                Builders<ConsentRecordDocument>.Filter.Eq(d => d.SubjectId, subjectId),
                Builders<ConsentRecordDocument>.Filter.Eq(d => d.Purpose, purpose),
                Builders<ConsentRecordDocument>.Filter.Eq(d => d.Status, (int)ConsentStatus.Active));

            var update = Builders<ConsentRecordDocument>.Update
                .Set(d => d.Status, (int)ConsentStatus.Withdrawn)
                .Set(d => d.WithdrawnAtUtc, nowUtc);

            var result = await _collection
                .UpdateOneAsync(filter, update, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (result.MatchedCount == 0)
            {
                return Left(ConsentErrors.MissingConsent(subjectId, purpose));
            }

            Log.WithdrewConsent(_logger, subjectId, purpose);
            return Right(unit);
        }
        catch (Exception ex)
        {
            Log.ConsentStoreOperationFailed(_logger, ex, "WithdrawConsentAsync");
            return Left(EncinaErrors.Create(
                code: "consent.store_error",
                message: $"Failed to withdraw consent: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, bool>> HasValidConsentAsync(
        string subjectId,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        try
        {
            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var filterBuilder = Builders<ConsentRecordDocument>.Filter;

            var filter = filterBuilder.And(
                filterBuilder.Eq(d => d.SubjectId, subjectId),
                filterBuilder.Eq(d => d.Purpose, purpose),
                filterBuilder.Eq(d => d.Status, (int)ConsentStatus.Active),
                filterBuilder.Or(
                    filterBuilder.Eq(d => d.ExpiresAtUtc, null),
                    filterBuilder.Gt(d => d.ExpiresAtUtc, nowUtc)));

            var count = await _collection
                .CountDocumentsAsync(filter, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return Right<EncinaError, bool>(count > 0);
        }
        catch (Exception ex)
        {
            Log.ConsentStoreOperationFailed(_logger, ex, "HasValidConsentAsync");
            return Left<EncinaError, bool>(EncinaErrors.Create(
                code: "consent.store_error",
                message: $"Failed to check consent validity: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, BulkOperationResult>> BulkRecordConsentAsync(
        IEnumerable<ConsentRecord> consents,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consents);

        var consentList = consents as IReadOnlyList<ConsentRecord> ?? consents.ToList();

        if (consentList.Count == 0)
        {
            return Right<EncinaError, BulkOperationResult>(BulkOperationResult.Success(0));
        }

        try
        {
            var successCount = 0;
            var errors = new List<BulkOperationError>();

            var writeModels = new List<WriteModel<ConsentRecordDocument>>();

            foreach (var consent in consentList)
            {
                var document = ConsentRecordDocument.FromRecord(consent);
                var filter = Builders<ConsentRecordDocument>.Filter.Eq(d => d.Id, consent.Id);
                writeModels.Add(new ReplaceOneModel<ConsentRecordDocument>(filter, document) { IsUpsert = true });
            }

            var result = await _collection
                .BulkWriteAsync(writeModels, new BulkWriteOptions { IsOrdered = false }, cancellationToken)
                .ConfigureAwait(false);

            successCount = (int)(result.InsertedCount + result.ModifiedCount + result.Upserts.Count);
            var totalCount = consentList.Count;
            var failureCount = totalCount - successCount;

            Log.BulkRecordedConsents(_logger, successCount, failureCount);

            return failureCount == 0
                ? BulkOperationResult.Success(successCount)
                : BulkOperationResult.Partial(successCount, errors);
        }
        catch (MongoBulkWriteException<ConsentRecordDocument> ex)
        {
            var successCount = (int)(ex.Result.InsertedCount + ex.Result.ModifiedCount + ex.Result.Upserts.Count);
            var errors = ex.WriteErrors
                .Select(e => new BulkOperationError(
                    $"index:{e.Index}",
                    EncinaErrors.Create(
                        code: "consent.bulk_record_failed",
                        message: e.Message)))
                .ToList();

            Log.BulkRecordedConsents(_logger, successCount, errors.Count);
            return BulkOperationResult.Partial(successCount, errors);
        }
        catch (Exception ex)
        {
            Log.ConsentStoreOperationFailed(_logger, ex, "BulkRecordConsentAsync");
            return Left<EncinaError, BulkOperationResult>(EncinaErrors.Create(
                code: "consent.store_error",
                message: $"Failed to bulk record consents: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, BulkOperationResult>> BulkWithdrawConsentAsync(
        string subjectId,
        IEnumerable<string> purposes,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);
        ArgumentNullException.ThrowIfNull(purposes);

        var purposeList = purposes as IReadOnlyList<string> ?? purposes.ToList();

        if (purposeList.Count == 0)
        {
            return Right<EncinaError, BulkOperationResult>(BulkOperationResult.Success(0));
        }

        try
        {
            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var successCount = 0;
            var errors = new List<BulkOperationError>();

            foreach (var purpose in purposeList)
            {
                var filter = Builders<ConsentRecordDocument>.Filter.And(
                    Builders<ConsentRecordDocument>.Filter.Eq(d => d.SubjectId, subjectId),
                    Builders<ConsentRecordDocument>.Filter.Eq(d => d.Purpose, purpose),
                    Builders<ConsentRecordDocument>.Filter.Eq(d => d.Status, (int)ConsentStatus.Active));

                var update = Builders<ConsentRecordDocument>.Update
                    .Set(d => d.Status, (int)ConsentStatus.Withdrawn)
                    .Set(d => d.WithdrawnAtUtc, nowUtc);

                var result = await _collection
                    .UpdateOneAsync(filter, update, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                if (result.MatchedCount > 0)
                {
                    successCount++;
                }
                else
                {
                    errors.Add(new BulkOperationError(
                        $"{subjectId}:{purpose}",
                        ConsentErrors.MissingConsent(subjectId, purpose)));
                }
            }

            Log.BulkWithdrewConsents(_logger, subjectId, successCount, errors.Count);

            return errors.Count == 0
                ? BulkOperationResult.Success(successCount)
                : BulkOperationResult.Partial(successCount, errors);
        }
        catch (Exception ex)
        {
            Log.ConsentStoreOperationFailed(_logger, ex, "BulkWithdrawConsentAsync");
            return Left<EncinaError, BulkOperationResult>(EncinaErrors.Create(
                code: "consent.store_error",
                message: $"Failed to bulk withdraw consents: {ex.Message}"));
        }
    }
}
