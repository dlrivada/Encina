using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Messaging;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using static LanguageExt.Prelude;

namespace Encina.MongoDB.ProcessorAgreements;

/// <summary>
/// MongoDB implementation of <see cref="IDPAStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Manages <see cref="DataProcessingAgreement"/> instances throughout their lifecycle
/// per GDPR Article 28(3). Uses <see cref="DataProcessingAgreementDocument"/> for
/// MongoDB-native BSON serialization.
/// </para>
/// <para>
/// DPA mandatory terms (Article 28(3)(a)-(h)) are stored as 8 individual boolean
/// fields in the BSON document. Processing purposes are stored as a native BSON array
/// (no JSON serialization needed).
/// </para>
/// </remarks>
public sealed class DPAStoreMongoDB : IDPAStore
{
    private readonly IMongoCollection<DataProcessingAgreementDocument> _collection;
    private readonly ILogger<DPAStoreMongoDB> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DPAStoreMongoDB"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client.</param>
    /// <param name="options">The MongoDB configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    public DPAStoreMongoDB(
        IMongoClient mongoClient,
        IOptions<EncinaMongoDbOptions> options,
        ILogger<DPAStoreMongoDB> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var config = options.Value;
        var database = mongoClient.GetDatabase(config.DatabaseName);
        _collection = database.GetCollection<DataProcessingAgreementDocument>(config.Collections.ProcessorAgreements);
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> AddAsync(
        DataProcessingAgreement agreement,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agreement);

        try
        {
            var filter = Builders<DataProcessingAgreementDocument>.Filter.Eq(d => d.Id, agreement.Id);
            var existing = await _collection.Find(filter)
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            if (existing is not null)
            {
                return Left(ProcessorAgreementErrors.StoreError(
                    "AddDPA", $"A Data Processing Agreement with ID '{agreement.Id}' already exists."));
            }

            var document = DataProcessingAgreementDocument.FromDPA(agreement);
            await _collection.InsertOneAsync(document, cancellationToken: cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("Added DPA '{DPAId}' for processor '{ProcessorId}'",
                agreement.Id, agreement.ProcessorId);
            return Right(unit);
        }
        catch (Exception ex)
        {
            return Left(ProcessorAgreementErrors.StoreError(
                "AddDPA", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Option<DataProcessingAgreement>>> GetByIdAsync(
        string dpaId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dpaId);

        try
        {
            var filter = Builders<DataProcessingAgreementDocument>.Filter.Eq(d => d.Id, dpaId);
            var document = await _collection.Find(filter)
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            if (document is null)
                return Right<EncinaError, Option<DataProcessingAgreement>>(None);

            var dpa = document.ToDPA();
            return dpa is not null
                ? Right<EncinaError, Option<DataProcessingAgreement>>(Some(dpa))
                : Right<EncinaError, Option<DataProcessingAgreement>>(None);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, Option<DataProcessingAgreement>>(ProcessorAgreementErrors.StoreError(
                "GetDPAById", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<DataProcessingAgreement>>> GetByProcessorIdAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processorId);

        try
        {
            var filter = Builders<DataProcessingAgreementDocument>.Filter.Eq(d => d.ProcessorId, processorId);
            var documents = await _collection.Find(filter)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var dpas = documents
                .Select(d => d.ToDPA())
                .Where(d => d is not null)
                .Cast<DataProcessingAgreement>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<DataProcessingAgreement>>(dpas);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<DataProcessingAgreement>>(ProcessorAgreementErrors.StoreError(
                "GetDPAsByProcessorId", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Option<DataProcessingAgreement>>> GetActiveByProcessorIdAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processorId);

        try
        {
            var activeStatus = (int)DPAStatus.Active;
            var filterBuilder = Builders<DataProcessingAgreementDocument>.Filter;
            var filter = filterBuilder.Eq(d => d.ProcessorId, processorId)
                & filterBuilder.Eq(d => d.StatusValue, activeStatus);

            var document = await _collection.Find(filter)
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            if (document is null)
                return Right<EncinaError, Option<DataProcessingAgreement>>(None);

            var dpa = document.ToDPA();
            return dpa is not null
                ? Right<EncinaError, Option<DataProcessingAgreement>>(Some(dpa))
                : Right<EncinaError, Option<DataProcessingAgreement>>(None);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, Option<DataProcessingAgreement>>(ProcessorAgreementErrors.StoreError(
                "GetActiveDPAByProcessorId", ex.Message, ex));
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
            var filter = Builders<DataProcessingAgreementDocument>.Filter.Eq(d => d.Id, agreement.Id);
            var existing = await _collection.Find(filter)
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            if (existing is null)
            {
                return Left(ProcessorAgreementErrors.DPANotFound(agreement.Id));
            }

            var document = DataProcessingAgreementDocument.FromDPA(agreement);
            await _collection.ReplaceOneAsync(
                filter,
                document,
                new ReplaceOptions { IsUpsert = false },
                cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("Updated DPA '{DPAId}' for processor '{ProcessorId}'",
                agreement.Id, agreement.ProcessorId);
            return Right(unit);
        }
        catch (Exception ex)
        {
            return Left(ProcessorAgreementErrors.StoreError(
                "UpdateDPA", ex.Message, ex));
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
            var filter = Builders<DataProcessingAgreementDocument>.Filter.Eq(d => d.StatusValue, statusValue);
            var documents = await _collection.Find(filter)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var dpas = documents
                .Select(d => d.ToDPA())
                .Where(d => d is not null)
                .Cast<DataProcessingAgreement>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<DataProcessingAgreement>>(dpas);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<DataProcessingAgreement>>(ProcessorAgreementErrors.StoreError(
                "GetDPAsByStatus", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<DataProcessingAgreement>>> GetExpiringAsync(
        DateTimeOffset threshold,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var thresholdDateTime = threshold.UtcDateTime;
            var activeStatus = (int)DPAStatus.Active;
            var filterBuilder = Builders<DataProcessingAgreementDocument>.Filter;
            var filter = filterBuilder.Eq(d => d.StatusValue, activeStatus)
                & filterBuilder.Ne(d => d.ExpiresAtUtc, null)
                & filterBuilder.Lte(d => d.ExpiresAtUtc, thresholdDateTime);

            var documents = await _collection.Find(filter)
                .SortBy(d => d.ExpiresAtUtc)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var dpas = documents
                .Select(d => d.ToDPA())
                .Where(d => d is not null)
                .Cast<DataProcessingAgreement>()
                .ToList();

            _logger.LogDebug("Retrieved {Count} DPAs expiring before {Threshold:O}", dpas.Count, threshold);
            return Right<EncinaError, IReadOnlyList<DataProcessingAgreement>>(dpas);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<DataProcessingAgreement>>(ProcessorAgreementErrors.StoreError(
                "GetExpiringDPAs", ex.Message, ex));
        }
    }
}
