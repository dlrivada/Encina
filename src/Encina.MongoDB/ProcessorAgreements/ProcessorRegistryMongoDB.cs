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
/// MongoDB implementation of <see cref="IProcessorRegistry"/>.
/// </summary>
/// <remarks>
/// <para>
/// Manages processor identity and hierarchy per GDPR Article 28.
/// Uses <see cref="ProcessorDocument"/> for MongoDB-native BSON serialization
/// and <c>ReplaceOne</c> with upsert for save operations.
/// </para>
/// <para>
/// Supports bounded sub-processor hierarchy traversal via BFS
/// limited by <see cref="MaxSubProcessorDepth"/>.
/// </para>
/// </remarks>
public sealed class ProcessorRegistryMongoDB : IProcessorRegistry
{
    private readonly IMongoCollection<ProcessorDocument> _collection;
    private readonly ILogger<ProcessorRegistryMongoDB> _logger;

    /// <summary>
    /// Maximum allowed depth for the sub-processor hierarchy.
    /// </summary>
    internal const int MaxSubProcessorDepth = 5;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessorRegistryMongoDB"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client.</param>
    /// <param name="options">The MongoDB configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    public ProcessorRegistryMongoDB(
        IMongoClient mongoClient,
        IOptions<EncinaMongoDbOptions> options,
        ILogger<ProcessorRegistryMongoDB> logger)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var config = options.Value;
        var database = mongoClient.GetDatabase(config.DatabaseName);
        _collection = database.GetCollection<ProcessorDocument>(config.Collections.Processors);
        _logger = logger;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> RegisterProcessorAsync(
        Processor processor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processor);

        try
        {
            if (processor.Depth > MaxSubProcessorDepth)
            {
                return Left(ProcessorAgreementErrors.SubProcessorDepthExceeded(
                    processor.Id, processor.Depth, MaxSubProcessorDepth));
            }

            var filter = Builders<ProcessorDocument>.Filter.Eq(d => d.Id, processor.Id);
            var existing = await _collection.Find(filter)
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            if (existing is not null)
            {
                return Left(ProcessorAgreementErrors.AlreadyExists(processor.Id));
            }

            var document = ProcessorDocument.FromProcessor(processor);
            await _collection.InsertOneAsync(document, cancellationToken: cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("Registered processor '{ProcessorId}' (depth={Depth})",
                processor.Id, processor.Depth);
            return Right(unit);
        }
        catch (Exception ex)
        {
            return Left(ProcessorAgreementErrors.StoreError(
                "RegisterProcessor", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Option<Processor>>> GetProcessorAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processorId);

        try
        {
            var filter = Builders<ProcessorDocument>.Filter.Eq(d => d.Id, processorId);
            var document = await _collection.Find(filter)
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            if (document is null)
                return Right<EncinaError, Option<Processor>>(None);

            var processor = document.ToProcessor();
            return processor is not null
                ? Right<EncinaError, Option<Processor>>(Some(processor))
                : Right<EncinaError, Option<Processor>>(None);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, Option<Processor>>(ProcessorAgreementErrors.StoreError(
                "GetProcessor", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<Processor>>> GetAllProcessorsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var documents = await _collection.Find(FilterDefinition<ProcessorDocument>.Empty)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var processors = documents
                .Select(d => d.ToProcessor())
                .Where(p => p is not null)
                .Cast<Processor>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<Processor>>(processors);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<Processor>>(ProcessorAgreementErrors.StoreError(
                "GetAllProcessors", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> UpdateProcessorAsync(
        Processor processor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processor);

        try
        {
            var filter = Builders<ProcessorDocument>.Filter.Eq(d => d.Id, processor.Id);
            var existing = await _collection.Find(filter)
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            if (existing is null)
            {
                return Left(ProcessorAgreementErrors.NotFound(processor.Id));
            }

            var document = ProcessorDocument.FromProcessor(processor);
            await _collection.ReplaceOneAsync(
                filter,
                document,
                new ReplaceOptions { IsUpsert = false },
                cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("Updated processor '{ProcessorId}'", processor.Id);
            return Right(unit);
        }
        catch (Exception ex)
        {
            return Left(ProcessorAgreementErrors.StoreError(
                "UpdateProcessor", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> RemoveProcessorAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processorId);

        try
        {
            var filter = Builders<ProcessorDocument>.Filter.Eq(d => d.Id, processorId);
            var result = await _collection.DeleteOneAsync(filter, cancellationToken).ConfigureAwait(false);

            if (result.DeletedCount == 0)
            {
                return Left(ProcessorAgreementErrors.NotFound(processorId));
            }

            _logger.LogDebug("Removed processor '{ProcessorId}'", processorId);
            return Right(unit);
        }
        catch (Exception ex)
        {
            return Left(ProcessorAgreementErrors.StoreError(
                "RemoveProcessor", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<Processor>>> GetSubProcessorsAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processorId);

        try
        {
            var filter = Builders<ProcessorDocument>.Filter.Eq(d => d.ParentProcessorId, processorId);
            var documents = await _collection.Find(filter)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var processors = documents
                .Select(d => d.ToProcessor())
                .Where(p => p is not null)
                .Cast<Processor>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<Processor>>(processors);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<Processor>>(ProcessorAgreementErrors.StoreError(
                "GetSubProcessors", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<Processor>>> GetFullSubProcessorChainAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processorId);

        try
        {
            // Load all processors and traverse via BFS
            var documents = await _collection.Find(FilterDefinition<ProcessorDocument>.Empty)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var allProcessors = documents
                .Select(d => d.ToProcessor())
                .Where(p => p is not null)
                .Cast<Processor>()
                .ToList();

            var byParent = allProcessors
                .Where(p => p.ParentProcessorId is not null)
                .GroupBy(p => p.ParentProcessorId!)
                .ToDictionary(g => g.Key, g => g.ToList());

            var result = new List<Processor>();
            var queue = new Queue<string>();
            queue.Enqueue(processorId);

            while (queue.Count > 0)
            {
                var currentId = queue.Dequeue();
                if (!byParent.TryGetValue(currentId, out var children))
                    continue;

                foreach (var child in children)
                {
                    result.Add(child);
                    if (child.Depth < MaxSubProcessorDepth)
                    {
                        queue.Enqueue(child.Id);
                    }
                }
            }

            _logger.LogDebug("Retrieved {Count} sub-processors in chain for '{ProcessorId}'",
                result.Count, processorId);
            return Right<EncinaError, IReadOnlyList<Processor>>(result);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<Processor>>(ProcessorAgreementErrors.StoreError(
                "GetFullSubProcessorChain", ex.Message, ex));
        }
    }
}
