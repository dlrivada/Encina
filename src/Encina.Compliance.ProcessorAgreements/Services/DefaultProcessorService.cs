using Encina.Caching;
using Encina.Compliance.ProcessorAgreements.Abstractions;
using Encina.Compliance.ProcessorAgreements.Aggregates;
using Encina.Compliance.ProcessorAgreements.Diagnostics;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Compliance.ProcessorAgreements.ReadModels;
using Encina.Marten;
using Encina.Marten.Projections;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Compliance.ProcessorAgreements.Services;

/// <summary>
/// Default implementation of <see cref="IProcessorService"/> that manages processor lifecycle
/// operations via event-sourced aggregates.
/// </summary>
/// <remarks>
/// <para>
/// Wraps <see cref="IAggregateRepository{TAggregate}"/> for <see cref="ProcessorAggregate"/> (command side)
/// and <see cref="IReadModelRepository{TReadModel}"/> for <see cref="ProcessorReadModel"/> (query side)
/// to provide a clean CQRS API for managing processors.
/// </para>
/// <para>
/// Cache key pattern: <c>"pa:processor:{id}"</c> for individual processor lookup by ID.
/// Cache invalidation is fire-and-forget — cache misses are acceptable.
/// </para>
/// </remarks>
internal sealed class DefaultProcessorService : IProcessorService
{
    private static readonly TimeSpan DefaultCacheTtl = TimeSpan.FromMinutes(5);

    private readonly IAggregateRepository<ProcessorAggregate> _repository;
    private readonly IReadModelRepository<ProcessorReadModel> _readModelRepository;
    private readonly ICacheProvider _cache;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DefaultProcessorService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultProcessorService"/>.
    /// </summary>
    /// <param name="repository">The aggregate repository for processor aggregates.</param>
    /// <param name="readModelRepository">The read model repository for processor projections.</param>
    /// <param name="cache">The cache provider for read model caching.</param>
    /// <param name="timeProvider">The time provider for UTC timestamps.</param>
    /// <param name="logger">The logger instance.</param>
    public DefaultProcessorService(
        IAggregateRepository<ProcessorAggregate> repository,
        IReadModelRepository<ProcessorReadModel> readModelRepository,
        ICacheProvider cache,
        TimeProvider timeProvider,
        ILogger<DefaultProcessorService> logger)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(readModelRepository);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _repository = repository;
        _readModelRepository = readModelRepository;
        _cache = cache;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    // ========================================================================
    // Command operations
    // ========================================================================

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Guid>> RegisterProcessorAsync(
        string name,
        string country,
        string? contactEmail,
        Guid? parentProcessorId,
        int depth,
        SubProcessorAuthorizationType authorizationType,
        string? tenantId = null,
        string? moduleId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Registering processor. Name='{Name}', Country='{Country}', Depth={Depth}",
            name, country, depth);

        try
        {
            var id = Guid.NewGuid();
            var occurredAtUtc = _timeProvider.GetUtcNow();

            var aggregate = ProcessorAggregate.Register(
                id, name, country, contactEmail, parentProcessorId,
                depth, authorizationType, occurredAtUtc, tenantId, moduleId);

            var result = await _repository.CreateAsync(aggregate, cancellationToken);

            return result.Match<Either<EncinaError, Guid>>(
                Right: _ =>
                {
                    _logger.ProcessorRegistered(id.ToString(), name, depth);
                    ProcessorAgreementDiagnostics.RegistryOperationTotal.Add(1);
                    return id;
                },
                Left: error => error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ProcessorRegistrationFailed(name, ex.Message);
            return ProcessorAgreementErrors.StoreError("RegisterProcessor", ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> UpdateProcessorAsync(
        Guid processorId,
        string name,
        string country,
        string? contactEmail,
        SubProcessorAuthorizationType authorizationType,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating processor '{ProcessorId}'", processorId);

        try
        {
            var loadResult = await _repository.LoadAsync(processorId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var occurredAtUtc = _timeProvider.GetUtcNow();
                    aggregate.Update(name, country, contactEmail, authorizationType, occurredAtUtc);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.ProcessorUpdated(processorId.ToString());
                            ProcessorAgreementDiagnostics.RegistryOperationTotal.Add(1);
                            InvalidateProcessorCache(processorId);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => ProcessorAgreementErrors.NotFound(processorId.ToString()));
        }
        catch (InvalidOperationException ex)
        {
            _logger.ProcessorUpdateFailed(processorId.ToString(), ex.Message);
            return ProcessorAgreementErrors.ValidationFailed(processorId.ToString(), ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ProcessorUpdateFailed(processorId.ToString(), ex.Message);
            return ProcessorAgreementErrors.StoreError("UpdateProcessor", ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RemoveProcessorAsync(
        Guid processorId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Removing processor '{ProcessorId}'", processorId);

        try
        {
            var loadResult = await _repository.LoadAsync(processorId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var occurredAtUtc = _timeProvider.GetUtcNow();
                    aggregate.Remove(reason, occurredAtUtc);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.ProcessorRemoved(processorId.ToString());
                            ProcessorAgreementDiagnostics.RegistryOperationTotal.Add(1);
                            InvalidateProcessorCache(processorId);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => ProcessorAgreementErrors.NotFound(processorId.ToString()));
        }
        catch (InvalidOperationException ex)
        {
            _logger.ProcessorRemovalFailed(processorId.ToString(), ex.Message);
            return ProcessorAgreementErrors.ValidationFailed(processorId.ToString(), ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ProcessorRemovalFailed(processorId.ToString(), ex.Message);
            return ProcessorAgreementErrors.StoreError("RemoveProcessor", ex.Message, ex);
        }
    }

    // ========================================================================
    // Query operations
    // ========================================================================

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, ProcessorReadModel>> GetProcessorAsync(
        Guid processorId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting processor '{ProcessorId}'", processorId);

        var cacheKey = $"pa:processor:{processorId}";

        try
        {
            var cached = await _cache.GetAsync<ProcessorReadModel>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                return cached;
            }

            var result = await _readModelRepository.GetByIdAsync(processorId, cancellationToken);

            return await result.MatchAsync<Either<EncinaError, ProcessorReadModel>>(
                RightAsync: async readModel =>
                {
                    await _cache.SetAsync(cacheKey, readModel, DefaultCacheTtl, cancellationToken);
                    return readModel;
                },
                Left: _ => ProcessorAgreementErrors.NotFound(processorId.ToString()));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ProcessorRegistrationFailed(processorId.ToString(), ex.Message);
            return ProcessorAgreementErrors.StoreError("GetProcessor", ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<ProcessorReadModel>>> GetAllProcessorsAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all active processors");

        try
        {
            return await _readModelRepository.QueryAsync(
                q => q.Where(p => !p.IsRemoved),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProcessorAgreementErrors.StoreError("GetAllProcessors", ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<ProcessorReadModel>>> GetSubProcessorsAsync(
        Guid processorId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting sub-processors of '{ProcessorId}'", processorId);

        try
        {
            var result = await _readModelRepository.QueryAsync(
                q => q.Where(p => p.ParentProcessorId == processorId && !p.IsRemoved),
                cancellationToken);

            return result.Match<Either<EncinaError, IReadOnlyList<ProcessorReadModel>>>(
                Right: subProcessors =>
                {
                    _logger.SubProcessorsRetrieved(processorId.ToString(), subProcessors.Count);
                    return Either<EncinaError, IReadOnlyList<ProcessorReadModel>>.Right(subProcessors);
                },
                Left: error => error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProcessorAgreementErrors.StoreError("GetSubProcessors", ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<ProcessorReadModel>>> GetFullSubProcessorChainAsync(
        Guid processorId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting full sub-processor chain for '{ProcessorId}'", processorId);

        try
        {
            // BFS traversal to collect the full sub-processor chain
            var chain = new List<ProcessorReadModel>();
            var queue = new Queue<Guid>();
            queue.Enqueue(processorId);

            while (queue.Count > 0)
            {
                var currentId = queue.Dequeue();

                var childrenResult = await _readModelRepository.QueryAsync(
                    q => q.Where(p => p.ParentProcessorId == currentId && !p.IsRemoved),
                    cancellationToken);

                var children = childrenResult.Match(
                    Right: c => c,
                    Left: _ => (IReadOnlyList<ProcessorReadModel>)[]);

                foreach (var child in children)
                {
                    chain.Add(child);
                    queue.Enqueue(child.Id);
                }
            }

            _logger.SubProcessorChainResolved(processorId.ToString(), chain.Count);
            return chain;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProcessorAgreementErrors.StoreError("GetFullSubProcessorChain", ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<object>>> GetProcessorHistoryAsync(
        Guid processorId,
        CancellationToken cancellationToken = default)
    {
        // Event history retrieval requires direct Marten event stream access,
        // which is not available through the generic IAggregateRepository.
        // This will be implemented when Marten-specific integration is configured (Phase 4+).
        _logger.LogDebug("Event history requested for processor '{ProcessorId}' (not yet available)", processorId);
        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<object>>>(
            ProcessorAgreementErrors.StoreError("GetProcessorHistory", "Event history retrieval is not yet available via the generic aggregate repository."));
    }

    // ========================================================================
    // Private helpers
    // ========================================================================

    private void InvalidateProcessorCache(Guid processorId)
    {
        // Fire-and-forget cache invalidation — cache misses are acceptable
        _ = _cache.RemoveAsync($"pa:processor:{processorId}", CancellationToken.None);
    }
}
