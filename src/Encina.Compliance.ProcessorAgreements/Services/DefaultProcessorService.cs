using Encina.Caching;
using Encina.Compliance.ProcessorAgreements.Abstractions;
using Encina.Compliance.ProcessorAgreements.Aggregates;
using Encina.Compliance.ProcessorAgreements.Diagnostics;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Compliance.ProcessorAgreements.ReadModels;
using Encina.Marten;
using Encina.Marten.Projections;
using Encina.Tenancy;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
/// <b>Tenant isolation (#3517, #3679):</b> every query filters <see cref="ProcessorReadModel.TenantId"/>
/// against the ambient tenant of <see cref="IRequestContextAccessor.RequestContext"/> (the null-tenant
/// scope when there is none, never an unfiltered query). A processor of another tenant read or
/// mutated by id is reported as not found and never saved, and a sub-processor cannot be registered
/// under a parent of another tenant. <see cref="ProcessorAgreementOptions.RequireTenantContext"/>
/// decides whether a missing tenant fails closed with <see cref="ProcessorAgreementErrors.TenantRequiredCode"/>.
/// </para>
/// <para>
/// Cache key pattern: <c>"pa:tenant:{tenantId|-}:processor:{id}"</c> for individual processor lookup
/// by ID, so a cached processor of one tenant is never served to another. A mutation invalidates the
/// key of the tenant stored on the aggregate. Cache invalidation is fire-and-forget — cache misses
/// are acceptable.
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
    private readonly ProcessorAgreementTenantScope _tenantScope;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultProcessorService"/>.
    /// </summary>
    /// <param name="repository">The aggregate repository for processor aggregates.</param>
    /// <param name="readModelRepository">The read model repository for processor projections.</param>
    /// <param name="cache">The cache provider for read model caching.</param>
    /// <param name="options">The module options (tenant enforcement).</param>
    /// <param name="timeProvider">The time provider for UTC timestamps.</param>
    /// <param name="requestContextAccessor">The accessor of the ambient request context, the source of the tenant.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="tenantProvider">
    /// Present only when <c>Encina.Tenancy</c> is registered; used solely to detect a multi-tenant
    /// application for <see cref="ProcessorAgreementOptions.RequireTenantContext"/> auto-detection.
    /// </param>
    public DefaultProcessorService(
        IAggregateRepository<ProcessorAggregate> repository,
        IReadModelRepository<ProcessorReadModel> readModelRepository,
        ICacheProvider cache,
        IOptions<ProcessorAgreementOptions> options,
        TimeProvider timeProvider,
        IRequestContextAccessor requestContextAccessor,
        ILogger<DefaultProcessorService> logger,
        ITenantProvider? tenantProvider = null)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(readModelRepository);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(requestContextAccessor);
        ArgumentNullException.ThrowIfNull(logger);

        _repository = repository;
        _readModelRepository = readModelRepository;
        _cache = cache;
        _timeProvider = timeProvider;
        _logger = logger;
        _tenantScope = new ProcessorAgreementTenantScope(
            requestContextAccessor, options.Value, tenantProvider is not null, logger);
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

        if (!_tenantScope.TryResolveWrite("RegisterProcessor", tenantId, out var effectiveTenantId, out var tenantError))
        {
            return tenantError!.Value;
        }

        try
        {
            // A sub-processor is only attached to a parent of the same tenant; a parent that is
            // missing or belongs to another tenant is reported as not found (#3517 D19).
            if (await FindParentErrorAsync(parentProcessorId, effectiveTenantId, cancellationToken) is { } parentError)
            {
                return parentError;
            }

            var id = Guid.NewGuid();
            var occurredAtUtc = _timeProvider.GetUtcNow();

            var aggregate = ProcessorAggregate.Register(
                id, name, country, contactEmail, parentProcessorId,
                depth, authorizationType, occurredAtUtc, effectiveTenantId, moduleId);

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
            _logger.ProcessorRegistrationFailed(name, ex.GetType().Name);
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

        if (!_tenantScope.TryResolve("UpdateProcessor", out var tenantId, out var tenantError))
        {
            return tenantError!.Value;
        }

        try
        {
            var loadResult = await LoadScopedAsync(processorId, tenantId, cancellationToken);

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
                            InvalidateProcessorCache(processorId, aggregate.TenantId);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: error => error);
        }
        catch (InvalidOperationException ex)
        {
            _logger.ProcessorUpdateFailed(processorId.ToString(), ex.GetType().Name);
            return ProcessorAgreementErrors.ValidationFailed(processorId.ToString(), ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ProcessorUpdateFailed(processorId.ToString(), ex.GetType().Name);
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

        if (!_tenantScope.TryResolve("RemoveProcessor", out var tenantId, out var tenantError))
        {
            return tenantError!.Value;
        }

        try
        {
            var loadResult = await LoadScopedAsync(processorId, tenantId, cancellationToken);

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
                            InvalidateProcessorCache(processorId, aggregate.TenantId);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: error => error);
        }
        catch (InvalidOperationException ex)
        {
            _logger.ProcessorRemovalFailed(processorId.ToString(), ex.GetType().Name);
            return ProcessorAgreementErrors.ValidationFailed(processorId.ToString(), ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ProcessorRemovalFailed(processorId.ToString(), ex.GetType().Name);
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

        if (!_tenantScope.TryResolve("GetProcessor", out var tenantId, out var tenantError))
        {
            return tenantError!.Value;
        }

        var cacheKey = ProcessorCacheKey(processorId, tenantId);

        try
        {
            var cached = await _cache.GetAsync<ProcessorReadModel>(cacheKey, cancellationToken);

            // A tenant-mismatched cache entry is treated as a miss and re-read below.
            if (cached is not null && ProcessorAgreementTenantScope.Matches(cached.TenantId, tenantId))
            {
                return cached;
            }

            var result = await _readModelRepository.GetByIdAsync(processorId, cancellationToken);

            return await result.MatchAsync<Either<EncinaError, ProcessorReadModel>>(
                RightAsync: async readModel =>
                {
                    // A processor of another tenant is reported as not found, never returned.
                    if (!ProcessorAgreementTenantScope.Matches(readModel.TenantId, tenantId))
                    {
                        return ProcessorAgreementErrors.NotFound(processorId.ToString());
                    }

                    await _cache.SetAsync(cacheKey, readModel, DefaultCacheTtl, cancellationToken);
                    return readModel;
                },
                Left: _ => ProcessorAgreementErrors.NotFound(processorId.ToString()));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ProcessorRegistrationFailed(processorId.ToString(), ex.GetType().Name);
            return ProcessorAgreementErrors.StoreError("GetProcessor", ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<ProcessorReadModel>>> GetAllProcessorsAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all active processors");

        if (!_tenantScope.TryResolve("GetAllProcessors", out var tenantId, out var tenantError))
        {
            return tenantError!.Value;
        }

        try
        {
            return await _readModelRepository.QueryAsync(
                q => q.Where(p => !p.IsRemoved && p.TenantId == tenantId),
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

        if (!_tenantScope.TryResolve("GetSubProcessors", out var tenantId, out var tenantError))
        {
            return tenantError!.Value;
        }

        try
        {
            var result = await QueryChildrenAsync(processorId, tenantId, cancellationToken);

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

        if (!_tenantScope.TryResolve("GetFullSubProcessorChain", out var tenantId, out var tenantError))
        {
            return tenantError!.Value;
        }

        try
        {
            // BFS traversal to collect the full sub-processor chain
            var chain = new List<ProcessorReadModel>();
            var queue = new Queue<Guid>();
            queue.Enqueue(processorId);

            while (queue.Count > 0)
            {
                var currentId = queue.Dequeue();

                var childrenResult = await QueryChildrenAsync(currentId, tenantId, cancellationToken);

                // A failed level fails the whole chain: a partial chain reported as complete would
                // hide sub-processors from the controller (errors are never swallowed).
                if (childrenResult.IsLeft)
                {
                    return childrenResult;
                }

                foreach (var child in childrenResult.IfLeft([]))
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

    /// <summary>
    /// Queries the non-removed direct children of a processor within the resolved tenant.
    /// </summary>
    private Task<Either<EncinaError, IReadOnlyList<ProcessorReadModel>>> QueryChildrenAsync(
        Guid parentId, string? tenantId, CancellationToken cancellationToken) =>
        _readModelRepository.QueryAsync(
            q => q.Where(p => p.ParentProcessorId == parentId && !p.IsRemoved && p.TenantId == tenantId),
            cancellationToken);

    /// <summary>
    /// Loads a processor aggregate for a mutation or a reference check. A processor that does not
    /// exist, or that belongs to another tenant, is reported as not found.
    /// </summary>
    private async ValueTask<Either<EncinaError, ProcessorAggregate>> LoadScopedAsync(
        Guid processorId, string? tenantId, CancellationToken cancellationToken)
    {
        var loadResult = await _repository.LoadAsync(processorId, cancellationToken);

        return loadResult.Match<Either<EncinaError, ProcessorAggregate>>(
            Right: aggregate => ProcessorAgreementTenantScope.Matches(aggregate.TenantId, tenantId)
                ? aggregate
                : ProcessorAgreementErrors.NotFound(processorId.ToString()),
            Left: _ => ProcessorAgreementErrors.NotFound(processorId.ToString()));
    }

    /// <summary>
    /// Returns the not-found error when a parent processor is named and is missing or belongs to
    /// another tenant, or <c>null</c> when there is no parent or it is in scope.
    /// </summary>
    private async ValueTask<EncinaError?> FindParentErrorAsync(
        Guid? parentProcessorId, string? tenantId, CancellationToken cancellationToken)
    {
        if (parentProcessorId is not { } parentId)
        {
            return null;
        }

        var parent = await LoadScopedAsync(parentId, tenantId, cancellationToken);
        return parent.IsLeft ? ProcessorAgreementErrors.NotFound(parentId.ToString()) : (EncinaError?)null;
    }

    private static string ProcessorCacheKey(Guid processorId, string? tenantId) =>
        ProcessorAgreementTenantScope.CacheKey(tenantId, $"processor:{processorId}");

    private void InvalidateProcessorCache(Guid processorId, string? tenantId)
    {
        // Fire-and-forget cache invalidation — cache misses are acceptable. The key is the one of
        // the tenant stored on the aggregate, which the mutation already matched.
        _ = _cache.RemoveAsync(ProcessorCacheKey(processorId, tenantId), CancellationToken.None);
    }
}
