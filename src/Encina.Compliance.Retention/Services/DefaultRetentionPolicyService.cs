using Encina.Caching;
using Encina.Compliance.Retention.Abstractions;
using Encina.Compliance.Retention.Aggregates;
using Encina.Compliance.Retention.Diagnostics;
using Encina.Compliance.Retention.Model;
using Encina.Compliance.Retention.ReadModels;
using Encina.Marten;
using Encina.Marten.Projections;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.Retention.Services;

/// <summary>
/// Default implementation of <see cref="IRetentionPolicyService"/> that manages retention
/// policy lifecycle operations via event-sourced aggregates.
/// </summary>
/// <remarks>
/// <para>
/// Wraps <see cref="IAggregateRepository{TAggregate}"/> for <see cref="RetentionPolicyAggregate"/> and
/// <see cref="IReadModelRepository{TReadModel}"/> for <see cref="RetentionPolicyReadModel"/> to provide
/// a clean CQRS API for managing retention policies. All write operations go through the aggregate
/// (command side), while read operations use the projected read model (query side).
/// </para>
/// <para>
/// Cache key pattern: <c>"ret:policy:{id}"</c> for individual policy lookup by ID.
/// Cache invalidation is fire-and-forget — cache misses are acceptable.
/// </para>
/// <para>
/// The <see cref="GetRetentionPeriodAsync"/> method implements a fallback strategy:
/// first checks for a category-specific policy, then falls back to
/// <see cref="RetentionOptions.DefaultRetentionPeriod"/> if configured.
/// </para>
/// </remarks>
internal sealed class DefaultRetentionPolicyService : IRetentionPolicyService
{
    private static readonly TimeSpan DefaultCacheTtl = TimeSpan.FromMinutes(5);

    private readonly IAggregateRepository<RetentionPolicyAggregate> _repository;
    private readonly IReadModelRepository<RetentionPolicyReadModel> _readModelRepository;
    private readonly ICacheProvider _cache;
    private readonly IOptions<RetentionOptions> _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DefaultRetentionPolicyService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultRetentionPolicyService"/>.
    /// </summary>
    /// <param name="repository">The aggregate repository for retention policy aggregates.</param>
    /// <param name="readModelRepository">The read model repository for retention policy projections.</param>
    /// <param name="cache">The cache provider for read model caching.</param>
    /// <param name="options">The retention module configuration options.</param>
    /// <param name="timeProvider">The time provider for UTC timestamps.</param>
    /// <param name="logger">The logger instance.</param>
    public DefaultRetentionPolicyService(
        IAggregateRepository<RetentionPolicyAggregate> repository,
        IReadModelRepository<RetentionPolicyReadModel> readModelRepository,
        ICacheProvider cache,
        IOptions<RetentionOptions> options,
        TimeProvider timeProvider,
        ILogger<DefaultRetentionPolicyService> logger)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(readModelRepository);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _repository = repository;
        _readModelRepository = readModelRepository;
        _cache = cache;
        _options = options;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    // ========================================================================
    // Command operations
    // ========================================================================

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Guid>> CreatePolicyAsync(
        string dataCategory,
        TimeSpan retentionPeriod,
        bool autoDelete,
        RetentionPolicyType policyType,
        string? reason = null,
        string? legalBasis = null,
        string? tenantId = null,
        string? moduleId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Creating retention policy. DataCategory='{DataCategory}', RetentionPeriod={RetentionPeriod}",
            dataCategory, retentionPeriod);

        try
        {
            var id = Guid.NewGuid();
            var occurredAtUtc = _timeProvider.GetUtcNow();

            var aggregate = RetentionPolicyAggregate.Create(
                id, dataCategory, retentionPeriod, autoDelete, policyType,
                reason, legalBasis, occurredAtUtc, tenantId, moduleId);

            var result = await _repository.CreateAsync(aggregate, cancellationToken);

            return result.Match<Either<EncinaError, Guid>>(
                Right: _ =>
                {
                    _logger.RetentionPolicyCreatedES(id, dataCategory, retentionPeriod);
                    RetentionDiagnostics.PolicyResolutionsTotal.Add(1);
                    return id;
                },
                Left: error => error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.RetentionServiceError("CreatePolicy", ex);
            return RetentionErrors.ServiceError("CreatePolicy", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> UpdatePolicyAsync(
        Guid policyId,
        TimeSpan retentionPeriod,
        bool autoDelete,
        string? reason,
        string? legalBasis,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating retention policy '{PolicyId}'", policyId);

        try
        {
            var loadResult = await _repository.LoadAsync(policyId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var occurredAtUtc = _timeProvider.GetUtcNow();
                    aggregate.Update(retentionPeriod, autoDelete, reason, legalBasis, occurredAtUtc);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.RetentionPolicyUpdatedES(policyId, retentionPeriod);
                            InvalidateCache(policyId);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => RetentionErrors.PolicyNotFound(policyId.ToString()));
        }
        catch (InvalidOperationException)
        {
            _logger.RetentionInvalidStateTransition(policyId, "UpdatePolicy");
            return RetentionErrors.InvalidStateTransition(policyId, "UpdatePolicy");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.RetentionServiceError("UpdatePolicy", ex);
            return RetentionErrors.ServiceError("UpdatePolicy", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> DeactivatePolicyAsync(
        Guid policyId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deactivating retention policy '{PolicyId}'", policyId);

        try
        {
            var loadResult = await _repository.LoadAsync(policyId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var occurredAtUtc = _timeProvider.GetUtcNow();
                    aggregate.Deactivate(reason, occurredAtUtc);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.RetentionPolicyDeactivatedES(policyId, reason);
                            InvalidateCache(policyId);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => RetentionErrors.PolicyNotFound(policyId.ToString()));
        }
        catch (InvalidOperationException)
        {
            _logger.RetentionInvalidStateTransition(policyId, "DeactivatePolicy");
            return RetentionErrors.InvalidStateTransition(policyId, "DeactivatePolicy");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.RetentionServiceError("DeactivatePolicy", ex);
            return RetentionErrors.ServiceError("DeactivatePolicy", ex);
        }
    }

    // ========================================================================
    // Query operations
    // ========================================================================

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, RetentionPolicyReadModel>> GetPolicyAsync(
        Guid policyId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting retention policy '{PolicyId}'", policyId);

        var cacheKey = $"ret:policy:{policyId}";

        try
        {
            var cached = await _cache.GetAsync<RetentionPolicyReadModel>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                _logger.RetentionCacheHit(cacheKey);
                return cached;
            }

            var result = await _readModelRepository.GetByIdAsync(policyId, cancellationToken);

            return await result.MatchAsync<Either<EncinaError, RetentionPolicyReadModel>>(
                RightAsync: async readModel =>
                {
                    await _cache.SetAsync(cacheKey, readModel, DefaultCacheTtl, cancellationToken);
                    return readModel;
                },
                Left: _ => RetentionErrors.PolicyNotFound(policyId.ToString()));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.RetentionServiceError("GetPolicy", ex);
            return RetentionErrors.ServiceError("GetPolicy", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, RetentionPolicyReadModel>> GetPolicyByCategoryAsync(
        string dataCategory,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting retention policy for category '{DataCategory}'", dataCategory);

        try
        {
            var result = await _readModelRepository.QueryAsync(
                q => q.Where(p => p.DataCategory == dataCategory && p.IsActive),
                cancellationToken);

            return result.Match<Either<EncinaError, RetentionPolicyReadModel>>(
                Right: policies => policies.Count > 0
                    ? policies[0]
                    : RetentionErrors.NoPolicyForCategory(dataCategory),
                Left: error => error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.RetentionServiceError("GetPolicyByCategory", ex);
            return RetentionErrors.ServiceError("GetPolicyByCategory", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<RetentionPolicyReadModel>>> GetActivePoliciesAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all active retention policies");

        try
        {
            return await _readModelRepository.QueryAsync(
                q => q.Where(p => p.IsActive),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.RetentionServiceError("GetActivePolicies", ex);
            return RetentionErrors.ServiceError("GetActivePolicies", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, TimeSpan>> GetRetentionPeriodAsync(
        string dataCategory,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Resolving retention period for category '{DataCategory}'", dataCategory);

        try
        {
            var policyResult = await GetPolicyByCategoryAsync(dataCategory, cancellationToken);

            return policyResult.Match<Either<EncinaError, TimeSpan>>(
                Right: policy =>
                {
                    _logger.RetentionPeriodResolved(dataCategory, policy.RetentionPeriod.TotalDays, policy.Id.ToString());
                    return policy.RetentionPeriod;
                },
                Left: _ =>
                {
                    // Fall back to default retention period if configured
                    var defaultPeriod = _options.Value.DefaultRetentionPeriod;
                    if (defaultPeriod.HasValue)
                    {
                        _logger.RetentionPeriodResolvedFromDefault(dataCategory, defaultPeriod.Value.TotalDays);
                        return defaultPeriod.Value;
                    }

                    _logger.RetentionNoPolicyForCategory(dataCategory);
                    return RetentionErrors.NoPolicyForCategory(dataCategory);
                });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.RetentionServiceError("GetRetentionPeriod", ex);
            return RetentionErrors.ServiceError("GetRetentionPeriod", ex);
        }
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<object>>> GetPolicyHistoryAsync(
        Guid policyId,
        CancellationToken cancellationToken = default)
    {
        // Event history retrieval requires direct Marten event stream access,
        // which is not available through the generic IAggregateRepository.
        // This will be implemented when Marten-specific integration is configured (Phase 4+).
        _logger.LogDebug("Event history requested for policy '{PolicyId}' (not yet available)", policyId);
        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<object>>>(
            RetentionErrors.EventHistoryUnavailable(policyId));
    }

    // ========================================================================
    // Private helpers
    // ========================================================================

    private void InvalidateCache(Guid policyId)
    {
        var cacheKey = $"ret:policy:{policyId}";
        // Fire-and-forget cache invalidation — cache misses are acceptable
        _ = _cache.RemoveAsync(cacheKey, CancellationToken.None);
        _logger.RetentionCacheInvalidated(cacheKey);
    }
}
