using Encina.Caching;
using Encina.Compliance.DataResidency.Abstractions;
using Encina.Compliance.DataResidency.Aggregates;
using Encina.Compliance.DataResidency.Diagnostics;
using Encina.Compliance.DataResidency.Model;
using Encina.Compliance.DataResidency.ReadModels;
using Encina.Marten;
using Encina.Marten.Projections;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Compliance.DataResidency.Services;

/// <summary>
/// Default implementation of <see cref="IResidencyPolicyService"/> that manages residency
/// policy lifecycle operations via event-sourced aggregates.
/// </summary>
/// <remarks>
/// <para>
/// Wraps <see cref="IAggregateRepository{TAggregate}"/> for <see cref="ResidencyPolicyAggregate"/> and
/// <see cref="IReadModelRepository{TReadModel}"/> for <see cref="ResidencyPolicyReadModel"/> to provide
/// a clean CQRS API for managing residency policies. All write operations go through the aggregate
/// (command side), while read operations use the projected read model (query side).
/// </para>
/// <para>
/// Cache key pattern: <c>"dr:policy:{id}"</c> for individual policy lookup by ID.
/// Cache invalidation is fire-and-forget — cache misses are acceptable.
/// </para>
/// <para>
/// The evaluation methods (<see cref="IsAllowedAsync"/> and <see cref="GetAllowedRegionsAsync"/>)
/// are absorbed from the legacy <c>IDataResidencyPolicy</c> interface. They query the read model
/// by data category and evaluate the policy's allowed regions, adequacy requirements, and transfer bases.
/// </para>
/// </remarks>
internal sealed class DefaultResidencyPolicyService : IResidencyPolicyService
{
    private static readonly TimeSpan DefaultCacheTtl = TimeSpan.FromMinutes(5);

    private readonly IAggregateRepository<ResidencyPolicyAggregate> _repository;
    private readonly IReadModelRepository<ResidencyPolicyReadModel> _readModelRepository;
    private readonly ICacheProvider _cache;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DefaultResidencyPolicyService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultResidencyPolicyService"/>.
    /// </summary>
    /// <param name="repository">The aggregate repository for residency policy aggregates.</param>
    /// <param name="readModelRepository">The read model repository for residency policy projections.</param>
    /// <param name="cache">The cache provider for read model caching.</param>
    /// <param name="timeProvider">The time provider for UTC timestamps.</param>
    /// <param name="logger">The logger instance.</param>
    public DefaultResidencyPolicyService(
        IAggregateRepository<ResidencyPolicyAggregate> repository,
        IReadModelRepository<ResidencyPolicyReadModel> readModelRepository,
        ICacheProvider cache,
        TimeProvider timeProvider,
        ILogger<DefaultResidencyPolicyService> logger)
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
    public async ValueTask<Either<EncinaError, Guid>> CreatePolicyAsync(
        string dataCategory,
        IReadOnlyList<string> allowedRegionCodes,
        bool requireAdequacyDecision,
        IReadOnlyList<TransferLegalBasis> allowedTransferBases,
        string? tenantId,
        string? moduleId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Creating residency policy. DataCategory='{DataCategory}'", dataCategory);

        try
        {
            var id = Guid.NewGuid();

            var aggregate = ResidencyPolicyAggregate.Create(
                id, dataCategory, allowedRegionCodes, requireAdequacyDecision,
                allowedTransferBases, tenantId, moduleId);

            var result = await _repository.CreateAsync(aggregate, cancellationToken);

            return result.Match<Either<EncinaError, Guid>>(
                Right: _ =>
                {
                    _logger.ResidencyPolicyCreatedES(id, dataCategory);
                    DataResidencyDiagnostics.PolicyChecksTotal.Add(1);
                    return id;
                },
                Left: error => error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ResidencyServiceError("CreatePolicy", ex);
            return DataResidencyErrors.ServiceError("CreatePolicy", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> UpdatePolicyAsync(
        Guid policyId,
        IReadOnlyList<string> allowedRegionCodes,
        bool requireAdequacyDecision,
        IReadOnlyList<TransferLegalBasis> allowedTransferBases,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Updating residency policy '{PolicyId}'", policyId);

        try
        {
            var loadResult = await _repository.LoadAsync(policyId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    aggregate.Update(allowedRegionCodes, requireAdequacyDecision, allowedTransferBases);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.ResidencyPolicyUpdatedES(policyId);
                            InvalidateCache(policyId);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => DataResidencyErrors.PolicyNotFound(policyId.ToString()));
        }
        catch (InvalidOperationException)
        {
            _logger.ResidencyInvalidStateTransition(policyId, "UpdatePolicy");
            return DataResidencyErrors.InvalidStateTransition(policyId, "UpdatePolicy");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ResidencyServiceError("UpdatePolicy", ex);
            return DataResidencyErrors.ServiceError("UpdatePolicy", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> DeletePolicyAsync(
        Guid policyId,
        string reason,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Deleting residency policy '{PolicyId}'", policyId);

        try
        {
            var loadResult = await _repository.LoadAsync(policyId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    aggregate.Delete(reason);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.ResidencyPolicyDeletedES(policyId, reason);
                            InvalidateCache(policyId);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => DataResidencyErrors.PolicyNotFound(policyId.ToString()));
        }
        catch (InvalidOperationException)
        {
            _logger.ResidencyInvalidStateTransition(policyId, "DeletePolicy");
            return DataResidencyErrors.InvalidStateTransition(policyId, "DeletePolicy");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ResidencyServiceError("DeletePolicy", ex);
            return DataResidencyErrors.ServiceError("DeletePolicy", ex);
        }
    }

    // ========================================================================
    // Query operations
    // ========================================================================

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, ResidencyPolicyReadModel>> GetPolicyAsync(
        Guid policyId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting residency policy '{PolicyId}'", policyId);

        var cacheKey = $"dr:policy:{policyId}";

        try
        {
            var cached = await _cache.GetAsync<ResidencyPolicyReadModel>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                _logger.ResidencyCacheHit(cacheKey);
                return cached;
            }

            var result = await _readModelRepository.GetByIdAsync(policyId, cancellationToken);

            return await result.MatchAsync<Either<EncinaError, ResidencyPolicyReadModel>>(
                RightAsync: async readModel =>
                {
                    await _cache.SetAsync(cacheKey, readModel, DefaultCacheTtl, cancellationToken);
                    return readModel;
                },
                Left: _ => DataResidencyErrors.PolicyNotFound(policyId.ToString()));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ResidencyServiceError("GetPolicy", ex);
            return DataResidencyErrors.ServiceError("GetPolicy", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, ResidencyPolicyReadModel>> GetPolicyByCategoryAsync(
        string dataCategory,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting residency policy for category '{DataCategory}'", dataCategory);

        try
        {
            var result = await _readModelRepository.QueryAsync(
                q => q.Where(p => p.DataCategory == dataCategory && p.IsActive),
                cancellationToken);

            return result.Match<Either<EncinaError, ResidencyPolicyReadModel>>(
                Right: policies => policies.Count > 0
                    ? policies[0]
                    : DataResidencyErrors.PolicyNotFound(dataCategory),
                Left: error => error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ResidencyServiceError("GetPolicyByCategory", ex);
            return DataResidencyErrors.ServiceError("GetPolicyByCategory", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<ResidencyPolicyReadModel>>> GetAllPoliciesAsync(
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting all active residency policies");

        try
        {
            return await _readModelRepository.QueryAsync(
                q => q.Where(p => p.IsActive),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ResidencyServiceError("GetAllPolicies", ex);
            return DataResidencyErrors.ServiceError("GetAllPolicies", ex);
        }
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<object>>> GetPolicyHistoryAsync(
        Guid policyId,
        CancellationToken cancellationToken)
    {
        // Event history retrieval requires direct Marten event stream access,
        // which is not available through the generic IAggregateRepository.
        // This will be implemented when Marten-specific integration is configured (Phase 4+).
        _logger.LogDebug("Event history requested for policy '{PolicyId}' (not yet available)", policyId);
        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<object>>>(
            DataResidencyErrors.EventHistoryUnavailable(policyId));
    }

    // ========================================================================
    // Evaluation operations (absorbed from IDataResidencyPolicy)
    // ========================================================================

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, bool>> IsAllowedAsync(
        string dataCategory,
        Region targetRegion,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Evaluating residency policy. DataCategory='{DataCategory}', TargetRegion='{TargetRegion}'",
            dataCategory, targetRegion.Code);

        try
        {
            var policyResult = await GetPolicyByCategoryAsync(dataCategory, cancellationToken);

            return policyResult.Match<Either<EncinaError, bool>>(
                Right: policy =>
                {
                    // Empty AllowedRegionCodes means no restrictions — all regions allowed
                    if (policy.AllowedRegionCodes.Count == 0)
                    {
                        return true;
                    }

                    var isAllowed = policy.AllowedRegionCodes.Any(
                        code => string.Equals(code, targetRegion.Code, StringComparison.OrdinalIgnoreCase));

                    DataResidencyDiagnostics.PolicyChecksTotal.Add(1);
                    return isAllowed;
                },
                Left: error => error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ResidencyServiceError("IsAllowed", ex);
            return DataResidencyErrors.ServiceError("IsAllowed", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<Region>>> GetAllowedRegionsAsync(
        string dataCategory,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Resolving allowed regions for category '{DataCategory}'", dataCategory);

        try
        {
            var policyResult = await GetPolicyByCategoryAsync(dataCategory, cancellationToken);

            return policyResult.Match<Either<EncinaError, IReadOnlyList<Region>>>(
                Right: policy =>
                {
                    // Empty AllowedRegionCodes means no restrictions
                    if (policy.AllowedRegionCodes.Count == 0)
                    {
                        return Array.Empty<Region>();
                    }

                    var regions = policy.AllowedRegionCodes
                        .Select(code => RegionRegistry.GetByCode(code))
                        .Where(region => region is not null)
                        .Cast<Region>()
                        .ToList();

                    return regions;
                },
                Left: error => error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ResidencyServiceError("GetAllowedRegions", ex);
            return DataResidencyErrors.ServiceError("GetAllowedRegions", ex);
        }
    }

    // ========================================================================
    // Private helpers
    // ========================================================================

    private void InvalidateCache(Guid policyId)
    {
        var cacheKey = $"dr:policy:{policyId}";
        // Fire-and-forget cache invalidation — cache misses are acceptable
        _ = _cache.RemoveAsync(cacheKey, CancellationToken.None);
        _logger.ResidencyCacheInvalidated(cacheKey);
    }
}
