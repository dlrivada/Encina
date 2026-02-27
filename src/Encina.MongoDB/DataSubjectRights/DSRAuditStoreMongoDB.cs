using Encina.Compliance.DataSubjectRights;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using static LanguageExt.Prelude;

namespace Encina.MongoDB.DataSubjectRights;

/// <summary>
/// MongoDB implementation of <see cref="IDSRAuditStore"/>.
/// </summary>
public sealed class DSRAuditStoreMongoDB : IDSRAuditStore
{
    private readonly IMongoCollection<DSRAuditEntryDocument> _collection;
    private readonly ILogger<DSRAuditStoreMongoDB> _logger;

    /// <inheritdoc />
    public DSRAuditStoreMongoDB(
        IMongoClient mongoClient,
        IOptions<EncinaMongoDbOptions> options,
        ILogger<DSRAuditStoreMongoDB> logger)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var config = options.Value;
        var database = mongoClient.GetDatabase(config.DatabaseName);
        _collection = database.GetCollection<DSRAuditEntryDocument>(config.Collections.DSRAuditEntries);
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RecordAsync(
        DSRAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        try
        {
            var document = DSRAuditEntryDocument.FromDomain(entry);
            await _collection.InsertOneAsync(document, cancellationToken: cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("DSR audit entry recorded for request '{RequestId}': {Action}",
                entry.DSRRequestId, entry.Action);
            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(DSRErrors.StoreError("Record", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DSRAuditEntry>>> GetAuditTrailAsync(
        string dsrRequestId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dsrRequestId);
        try
        {
            var filter = Builders<DSRAuditEntryDocument>.Filter.Eq(d => d.DSRRequestId, dsrRequestId);
            var sort = Builders<DSRAuditEntryDocument>.Sort.Ascending(d => d.OccurredAtUtc);
            var documents = await _collection.Find(filter).Sort(sort).ToListAsync(cancellationToken).ConfigureAwait(false);

            var results = documents.Select(d => d.ToDomain()).ToList();
            return Right<EncinaError, IReadOnlyList<DSRAuditEntry>>(results);
        }
        catch (Exception ex)
        {
            return Left(DSRErrors.StoreError("GetAuditTrail", ex.Message));
        }
    }
}
