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
/// MongoDB implementation of <see cref="IProcessorAuditStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Provides an immutable audit trail for processor agreement actions as required by
/// GDPR Article 5(2) (accountability principle). Uses MongoDB's native document
/// model for efficient append-only audit storage.
/// </para>
/// <para>
/// Uses <see cref="ProcessorAgreementAuditEntryDocument"/> for MongoDB-native BSON serialization
/// and <c>InsertOneAsync</c> for atomic, append-only audit entry persistence.
/// </para>
/// </remarks>
public sealed class ProcessorAuditStoreMongoDB : IProcessorAuditStore
{
    private readonly IMongoCollection<ProcessorAgreementAuditEntryDocument> _collection;
    private readonly ILogger<ProcessorAuditStoreMongoDB> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessorAuditStoreMongoDB"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client.</param>
    /// <param name="options">The MongoDB configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    public ProcessorAuditStoreMongoDB(
        IMongoClient mongoClient,
        IOptions<EncinaMongoDbOptions> options,
        ILogger<ProcessorAuditStoreMongoDB> logger)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var config = options.Value;
        var database = mongoClient.GetDatabase(config.DatabaseName);
        _collection = database.GetCollection<ProcessorAgreementAuditEntryDocument>(
            config.Collections.ProcessorAgreementAuditEntries);
        _logger = logger;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> RecordAsync(
        ProcessorAgreementAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            var document = ProcessorAgreementAuditEntryDocument.FromEntry(entry);
            await _collection.InsertOneAsync(document, cancellationToken: cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("Recorded processor agreement audit entry '{EntryId}': {Action} for processor '{ProcessorId}'",
                entry.Id, entry.Action, entry.ProcessorId);
            return Right(unit);
        }
        catch (Exception ex)
        {
            return Left(ProcessorAgreementErrors.StoreError(
                "RecordAuditEntry", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<ProcessorAgreementAuditEntry>>> GetAuditTrailAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processorId);

        try
        {
            var filter = Builders<ProcessorAgreementAuditEntryDocument>.Filter.Eq(d => d.ProcessorId, processorId);
            var sort = Builders<ProcessorAgreementAuditEntryDocument>.Sort.Ascending(d => d.OccurredAtUtc);
            var documents = await _collection.Find(filter).Sort(sort)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var entries = documents
                .Select(d => d.ToEntry())
                .ToList();

            return Right<EncinaError, IReadOnlyList<ProcessorAgreementAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<ProcessorAgreementAuditEntry>>(ProcessorAgreementErrors.StoreError(
                "GetAuditTrail", ex.Message, ex));
        }
    }
}
