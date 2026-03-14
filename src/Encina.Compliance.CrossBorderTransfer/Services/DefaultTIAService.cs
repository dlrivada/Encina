using System.Diagnostics.Metrics;
using Encina.Caching;
using Encina.Compliance.CrossBorderTransfer.Abstractions;
using Encina.Compliance.CrossBorderTransfer.Aggregates;
using Encina.Compliance.CrossBorderTransfer.Diagnostics;
using Encina.Compliance.CrossBorderTransfer.Errors;
using Encina.Compliance.CrossBorderTransfer.Model;
using Encina.Compliance.CrossBorderTransfer.ReadModels;
using Encina.Marten;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Compliance.CrossBorderTransfer.Services;

/// <summary>
/// Default implementation of <see cref="ITIAService"/> that manages Transfer Impact Assessment
/// lifecycle operations via event-sourced aggregates.
/// </summary>
/// <remarks>
/// <para>
/// Wraps <see cref="IAggregateRepository{TAggregate}"/> for <see cref="TIAAggregate"/> to provide
/// a clean API for creating, assessing, reviewing, and querying TIAs. All write operations
/// invalidate cached read models via <see cref="ICacheProvider"/>.
/// </para>
/// <para>
/// Cache key pattern: <c>"cbt:tia:{id}"</c> for individual TIA lookups.
/// Cache invalidation uses pattern-based removal: <c>"cbt:tia:*"</c>.
/// </para>
/// </remarks>
internal sealed class DefaultTIAService : ITIAService
{
    private readonly IAggregateRepository<TIAAggregate> _repository;
    private readonly ICacheProvider _cache;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DefaultTIAService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultTIAService"/>.
    /// </summary>
    /// <param name="repository">The aggregate repository for TIA aggregates.</param>
    /// <param name="cache">The cache provider for read model caching.</param>
    /// <param name="timeProvider">The time provider for UTC timestamps.</param>
    /// <param name="logger">The logger instance.</param>
    public DefaultTIAService(
        IAggregateRepository<TIAAggregate> repository,
        ICacheProvider cache,
        TimeProvider timeProvider,
        ILogger<DefaultTIAService> logger)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _repository = repository;
        _cache = cache;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Guid>> CreateTIAAsync(
        string sourceCountryCode,
        string destinationCountryCode,
        string dataCategory,
        string createdBy,
        string? tenantId = null,
        string? moduleId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Creating TIA for route {Source} → {Destination}, category '{Category}', by '{CreatedBy}'",
            sourceCountryCode, destinationCountryCode, dataCategory, createdBy);

        try
        {
            var id = Guid.NewGuid();
            var aggregate = TIAAggregate.Create(id, sourceCountryCode, destinationCountryCode, dataCategory, createdBy, tenantId, moduleId);

            var result = await _repository.CreateAsync(aggregate, cancellationToken);

            return result.Match<Either<EncinaError, Guid>>(
                Right: _ =>
                {
                    _logger.TIACreated(id.ToString(), sourceCountryCode, destinationCountryCode, dataCategory);
                    CrossBorderTransferDiagnostics.TIACreated.Add(1);
                    return id;
                },
                Left: error => error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.TIAStoreError("CreateTIA", ex);
            return CrossBorderTransferErrors.StoreError("CreateTIA", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> AssessRiskAsync(
        Guid tiaId,
        double riskScore,
        string? findings,
        string assessorId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Assessing risk for TIA '{TIAId}' with score {RiskScore}", tiaId, riskScore);

        try
        {
            var loadResult = await _repository.LoadAsync(tiaId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    aggregate.AssessRisk(riskScore, findings, assessorId);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.TIARiskAssessed(tiaId.ToString(), riskScore);
                            InvalidateCacheForTIA(tiaId, aggregate, cancellationToken);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => CrossBorderTransferErrors.TIANotFound(tiaId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.TIAInvalidStateTransition(tiaId.ToString(), "AssessRisk", ex);
            return CrossBorderTransferErrors.InvalidStateTransition("current", "InProgress");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.TIAStoreError("AssessRisk", ex);
            return CrossBorderTransferErrors.StoreError("AssessRisk", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RequireSupplementaryMeasureAsync(
        Guid tiaId,
        SupplementaryMeasureType type,
        string description,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adding supplementary measure to TIA '{TIAId}', type: {MeasureType}", tiaId, type);

        try
        {
            var loadResult = await _repository.LoadAsync(tiaId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var measureId = Guid.NewGuid();
                    aggregate.RequireSupplementaryMeasure(measureId, type, description);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.TIASupplementaryMeasureAdded(tiaId.ToString(), measureId.ToString(), type.ToString());
                            InvalidateCacheForTIA(tiaId, aggregate, cancellationToken);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => CrossBorderTransferErrors.TIANotFound(tiaId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.TIAInvalidStateTransition(tiaId.ToString(), "RequireSupplementaryMeasure", ex);
            return CrossBorderTransferErrors.InvalidStateTransition("current", "InProgress");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.TIAStoreError("RequireSupplementaryMeasure", ex);
            return CrossBorderTransferErrors.StoreError("RequireSupplementaryMeasure", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> SubmitForDPOReviewAsync(
        Guid tiaId,
        string submittedBy,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Submitting TIA '{TIAId}' for DPO review by '{SubmittedBy}'", tiaId, submittedBy);

        try
        {
            var loadResult = await _repository.LoadAsync(tiaId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    aggregate.SubmitForDPOReview(submittedBy);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.TIADPOReviewSubmitted(tiaId.ToString(), submittedBy);
                            InvalidateCacheForTIA(tiaId, aggregate, cancellationToken);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => CrossBorderTransferErrors.TIANotFound(tiaId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.TIAInvalidStateTransition(tiaId.ToString(), "SubmitForDPOReview", ex);
            return CrossBorderTransferErrors.InvalidStateTransition("current", "PendingDPOReview");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.TIAStoreError("SubmitForDPOReview", ex);
            return CrossBorderTransferErrors.StoreError("SubmitForDPOReview", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> CompleteDPOReviewAsync(
        Guid tiaId,
        bool approved,
        string reviewedBy,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Completing DPO review for TIA '{TIAId}', approved: {Approved}, by '{ReviewedBy}'",
            tiaId, approved, reviewedBy);

        try
        {
            var loadResult = await _repository.LoadAsync(tiaId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    if (approved)
                    {
                        aggregate.ApproveDPOReview(reviewedBy);
                        aggregate.Complete();
                    }
                    else
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
                        aggregate.RejectDPOReview(reviewedBy, reason);
                    }

                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            if (approved)
                            {
                                _logger.TIACompleted(tiaId.ToString());
                                CrossBorderTransferDiagnostics.TIACompleted.Add(1);
                            }
                            else
                            {
                                _logger.TIARiskAssessed(tiaId.ToString(), 0);
                            }

                            InvalidateCacheForTIA(tiaId, aggregate, cancellationToken);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => CrossBorderTransferErrors.TIANotFound(tiaId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.TIAInvalidStateTransition(tiaId.ToString(), "CompleteDPOReview", ex);
            return CrossBorderTransferErrors.InvalidStateTransition("current", approved ? "Completed" : "InProgress");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.TIAStoreError("CompleteDPOReview", ex);
            return CrossBorderTransferErrors.StoreError("CompleteDPOReview", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, TIAReadModel>> GetTIAAsync(
        Guid tiaId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting TIA '{TIAId}'", tiaId);

        var cacheKey = $"cbt:tia:{tiaId}";

        try
        {
            var cached = await _cache.GetAsync<TIAReadModel>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                _logger.CacheHit(cacheKey, "TIA");
                return cached;
            }

            var loadResult = await _repository.LoadAsync(tiaId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, TIAReadModel>>(
                RightAsync: async aggregate =>
                {
                    var readModel = ProjectToReadModel(aggregate);
                    await _cache.SetAsync(cacheKey, readModel, TimeSpan.FromMinutes(5), cancellationToken);
                    return readModel;
                },
                Left: _ => CrossBorderTransferErrors.TIANotFound(tiaId));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.TIAStoreError("GetTIA", ex);
            return CrossBorderTransferErrors.StoreError("GetTIA", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, TIAReadModel>> GetTIAByRouteAsync(
        string sourceCountryCode,
        string destinationCountryCode,
        string dataCategory,
        CancellationToken cancellationToken = default)
    {
        var routeKey = $"{sourceCountryCode}:{destinationCountryCode}:{dataCategory}";
        var cacheKey = $"cbt:tia:route:{routeKey}";

        _logger.LogDebug("Getting TIA by route {RouteKey}", routeKey);

        try
        {
            var cached = await _cache.GetAsync<TIAReadModel>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                _logger.CacheHit(cacheKey, "TIA");
                return cached;
            }

            // Route-based lookup requires scanning — this is a known limitation of event-sourced aggregates
            // without a projection. In production, a Marten inline projection would provide indexed queries.
            // For now, we return a not-found error to indicate no TIA exists for this route.
            _logger.LogDebug("TIA route lookup for {RouteKey} requires projection support (not yet available)", routeKey);
            return CrossBorderTransferErrors.TIANotFoundByRoute(sourceCountryCode, destinationCountryCode, dataCategory);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.TIAStoreError("GetTIAByRoute", ex);
            return CrossBorderTransferErrors.StoreError("GetTIAByRoute", ex);
        }
    }

    private static TIAReadModel ProjectToReadModel(TIAAggregate aggregate)
    {
        var now = DateTimeOffset.UtcNow;
        return new TIAReadModel
        {
            Id = aggregate.Id,
            SourceCountryCode = aggregate.SourceCountryCode,
            DestinationCountryCode = aggregate.DestinationCountryCode,
            DataCategory = aggregate.DataCategory,
            RiskScore = aggregate.RiskScore,
            Status = aggregate.Status,
            Findings = aggregate.Findings,
            AssessorId = aggregate.AssessorId,
            DPOReviewedAtUtc = aggregate.DPOReviewedAtUtc,
            CompletedAtUtc = aggregate.CompletedAtUtc,
            RequiredSupplementaryMeasures = aggregate.RequiredSupplementaryMeasures,
            TenantId = aggregate.TenantId,
            ModuleId = aggregate.ModuleId,
            CreatedAtUtc = now,
            LastModifiedAtUtc = now
        };
    }

    private void InvalidateCacheForTIA(Guid tiaId, TIAAggregate aggregate, CancellationToken cancellationToken)
    {
        var routeKey = $"{aggregate.SourceCountryCode}:{aggregate.DestinationCountryCode}:{aggregate.DataCategory}";

        // Fire-and-forget cache invalidation — cache misses are acceptable
        _ = _cache.RemoveAsync($"cbt:tia:{tiaId}", cancellationToken);
        _ = _cache.RemoveAsync($"cbt:tia:route:{routeKey}", cancellationToken);
    }
}
