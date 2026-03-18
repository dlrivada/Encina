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
/// Default implementation of <see cref="IDataLocationService"/> that manages data location
/// lifecycle operations via event-sourced aggregates.
/// </summary>
/// <remarks>
/// <para>
/// Wraps <see cref="IAggregateRepository{TAggregate}"/> for <see cref="DataLocationAggregate"/> and
/// <see cref="IReadModelRepository{TReadModel}"/> for <see cref="DataLocationReadModel"/> to provide
/// a clean CQRS API for tracking data storage locations. All write operations go through the aggregate
/// (command side), while read operations use the projected read model (query side).
/// </para>
/// <para>
/// Cache key pattern: <c>"dr:location:{id}"</c> for individual location lookup by ID.
/// Cache invalidation is fire-and-forget — cache misses are acceptable.
/// </para>
/// <para>
/// The event stream captures the complete data movement history including migrations, verifications,
/// and sovereignty violations, serving as the audit trail for GDPR Article 30 (records of processing
/// activities), Article 5(2) accountability, and Article 58 supervisory authority inquiries.
/// </para>
/// </remarks>
internal sealed class DefaultDataLocationService : IDataLocationService
{
    private static readonly TimeSpan DefaultCacheTtl = TimeSpan.FromMinutes(5);

    private readonly IAggregateRepository<DataLocationAggregate> _repository;
    private readonly IReadModelRepository<DataLocationReadModel> _readModelRepository;
    private readonly ICacheProvider _cache;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DefaultDataLocationService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultDataLocationService"/>.
    /// </summary>
    /// <param name="repository">The aggregate repository for data location aggregates.</param>
    /// <param name="readModelRepository">The read model repository for data location projections.</param>
    /// <param name="cache">The cache provider for read model caching.</param>
    /// <param name="timeProvider">The time provider for UTC timestamps.</param>
    /// <param name="logger">The logger instance.</param>
    public DefaultDataLocationService(
        IAggregateRepository<DataLocationAggregate> repository,
        IReadModelRepository<DataLocationReadModel> readModelRepository,
        ICacheProvider cache,
        TimeProvider timeProvider,
        ILogger<DefaultDataLocationService> logger)
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
    public async ValueTask<Either<EncinaError, Guid>> RegisterLocationAsync(
        string entityId,
        string dataCategory,
        string regionCode,
        StorageType storageType,
        IReadOnlyDictionary<string, string>? metadata,
        string? tenantId,
        string? moduleId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Registering data location. EntityId='{EntityId}', DataCategory='{DataCategory}', Region='{Region}'",
            entityId, dataCategory, regionCode);

        try
        {
            var id = Guid.NewGuid();
            var storedAtUtc = _timeProvider.GetUtcNow();

            var aggregate = DataLocationAggregate.Register(
                id, entityId, dataCategory, regionCode, storageType,
                storedAtUtc, metadata, tenantId, moduleId);

            var result = await _repository.CreateAsync(aggregate, cancellationToken);

            return result.Match<Either<EncinaError, Guid>>(
                Right: _ =>
                {
                    _logger.DataLocationRegisteredES(id, entityId, regionCode);
                    DataResidencyDiagnostics.LocationRecordsTotal.Add(1);
                    return id;
                },
                Left: error => error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ResidencyServiceError("RegisterLocation", ex);
            return DataResidencyErrors.ServiceError("RegisterLocation", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> MigrateLocationAsync(
        Guid locationId,
        string newRegionCode,
        string reason,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Migrating data location '{LocationId}' to region '{NewRegion}'", locationId, newRegionCode);

        try
        {
            var loadResult = await _repository.LoadAsync(locationId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    aggregate.Migrate(newRegionCode, reason);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.DataLocationMigratedES(locationId, newRegionCode);
                            InvalidateCache(locationId);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => DataResidencyErrors.LocationNotFound(locationId.ToString()));
        }
        catch (InvalidOperationException)
        {
            _logger.ResidencyInvalidStateTransition(locationId, "MigrateLocation");
            return DataResidencyErrors.InvalidStateTransition(locationId, "MigrateLocation");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ResidencyServiceError("MigrateLocation", ex);
            return DataResidencyErrors.ServiceError("MigrateLocation", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> VerifyLocationAsync(
        Guid locationId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Verifying data location '{LocationId}'", locationId);

        try
        {
            var loadResult = await _repository.LoadAsync(locationId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var verifiedAtUtc = _timeProvider.GetUtcNow();
                    aggregate.Verify(verifiedAtUtc);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.DataLocationVerifiedES(locationId);
                            InvalidateCache(locationId);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => DataResidencyErrors.LocationNotFound(locationId.ToString()));
        }
        catch (InvalidOperationException)
        {
            _logger.ResidencyInvalidStateTransition(locationId, "VerifyLocation");
            return DataResidencyErrors.InvalidStateTransition(locationId, "VerifyLocation");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ResidencyServiceError("VerifyLocation", ex);
            return DataResidencyErrors.ServiceError("VerifyLocation", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RemoveLocationAsync(
        Guid locationId,
        string reason,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Removing data location '{LocationId}'", locationId);

        try
        {
            var loadResult = await _repository.LoadAsync(locationId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    aggregate.Remove(reason);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.DataLocationRemovedES(locationId, reason);
                            InvalidateCache(locationId);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => DataResidencyErrors.LocationNotFound(locationId.ToString()));
        }
        catch (InvalidOperationException)
        {
            _logger.ResidencyInvalidStateTransition(locationId, "RemoveLocation");
            return DataResidencyErrors.InvalidStateTransition(locationId, "RemoveLocation");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ResidencyServiceError("RemoveLocation", ex);
            return DataResidencyErrors.ServiceError("RemoveLocation", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, int>> RemoveByEntityAsync(
        string entityId,
        string reason,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Removing all data locations for entity '{EntityId}'", entityId);

        try
        {
            // Find all active (non-removed) locations for the entity
            var queryResult = await _readModelRepository.QueryAsync(
                q => q.Where(l => l.EntityId == entityId && !l.IsRemoved),
                cancellationToken);

            return await queryResult.MatchAsync<Either<EncinaError, int>>(
                RightAsync: async locations =>
                {
                    var removedCount = 0;

                    foreach (var location in locations)
                    {
                        var removeResult = await RemoveLocationAsync(location.Id, reason, cancellationToken);
                        removeResult.Match(
                            Right: _ => removedCount++,
                            Left: _ => { }); // Log but continue with remaining locations
                    }

                    return removedCount;
                },
                Left: error => error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ResidencyServiceError("RemoveByEntity", ex);
            return DataResidencyErrors.ServiceError("RemoveByEntity", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> DetectViolationAsync(
        Guid locationId,
        string dataCategory,
        string violatingRegionCode,
        string details,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Detecting sovereignty violation on location '{LocationId}'", locationId);

        try
        {
            var loadResult = await _repository.LoadAsync(locationId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    aggregate.DetectViolation(dataCategory, violatingRegionCode, details);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.SovereigntyViolationDetectedES(locationId, dataCategory, violatingRegionCode);
                            DataResidencyDiagnostics.ViolationsTotal.Add(1);
                            InvalidateCache(locationId);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => DataResidencyErrors.LocationNotFound(locationId.ToString()));
        }
        catch (InvalidOperationException)
        {
            _logger.ResidencyInvalidStateTransition(locationId, "DetectViolation");
            return DataResidencyErrors.InvalidStateTransition(locationId, "DetectViolation");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ResidencyServiceError("DetectViolation", ex);
            return DataResidencyErrors.ServiceError("DetectViolation", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> ResolveViolationAsync(
        Guid locationId,
        string resolution,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Resolving sovereignty violation on location '{LocationId}'", locationId);

        try
        {
            var loadResult = await _repository.LoadAsync(locationId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    aggregate.ResolveViolation(resolution);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.SovereigntyViolationResolvedES(locationId, resolution);
                            InvalidateCache(locationId);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => DataResidencyErrors.LocationNotFound(locationId.ToString()));
        }
        catch (InvalidOperationException)
        {
            _logger.ResidencyInvalidStateTransition(locationId, "ResolveViolation");
            return DataResidencyErrors.InvalidStateTransition(locationId, "ResolveViolation");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ResidencyServiceError("ResolveViolation", ex);
            return DataResidencyErrors.ServiceError("ResolveViolation", ex);
        }
    }

    // ========================================================================
    // Query operations
    // ========================================================================

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, DataLocationReadModel>> GetLocationAsync(
        Guid locationId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting data location '{LocationId}'", locationId);

        var cacheKey = $"dr:location:{locationId}";

        try
        {
            var cached = await _cache.GetAsync<DataLocationReadModel>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                _logger.ResidencyCacheHit(cacheKey);
                return cached;
            }

            var result = await _readModelRepository.GetByIdAsync(locationId, cancellationToken);

            return await result.MatchAsync<Either<EncinaError, DataLocationReadModel>>(
                RightAsync: async readModel =>
                {
                    await _cache.SetAsync(cacheKey, readModel, DefaultCacheTtl, cancellationToken);
                    return readModel;
                },
                Left: _ => DataResidencyErrors.LocationNotFound(locationId.ToString()));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ResidencyServiceError("GetLocation", ex);
            return DataResidencyErrors.ServiceError("GetLocation", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DataLocationReadModel>>> GetByEntityAsync(
        string entityId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting data locations for entity '{EntityId}'", entityId);

        try
        {
            return await _readModelRepository.QueryAsync(
                q => q.Where(l => l.EntityId == entityId && !l.IsRemoved),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ResidencyServiceError("GetByEntity", ex);
            return DataResidencyErrors.ServiceError("GetByEntity", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DataLocationReadModel>>> GetByRegionAsync(
        string regionCode,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting data locations in region '{RegionCode}'", regionCode);

        try
        {
            return await _readModelRepository.QueryAsync(
                q => q.Where(l => l.RegionCode == regionCode && !l.IsRemoved),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ResidencyServiceError("GetByRegion", ex);
            return DataResidencyErrors.ServiceError("GetByRegion", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DataLocationReadModel>>> GetByCategoryAsync(
        string dataCategory,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting data locations for category '{DataCategory}'", dataCategory);

        try
        {
            return await _readModelRepository.QueryAsync(
                q => q.Where(l => l.DataCategory == dataCategory && !l.IsRemoved),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ResidencyServiceError("GetByCategory", ex);
            return DataResidencyErrors.ServiceError("GetByCategory", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DataLocationReadModel>>> GetViolationsAsync(
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting all data locations with active sovereignty violations");

        try
        {
            return await _readModelRepository.QueryAsync(
                q => q.Where(l => l.HasViolation && !l.IsRemoved),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ResidencyServiceError("GetViolations", ex);
            return DataResidencyErrors.ServiceError("GetViolations", ex);
        }
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<object>>> GetLocationHistoryAsync(
        Guid locationId,
        CancellationToken cancellationToken)
    {
        // Event history retrieval requires direct Marten event stream access,
        // which is not available through the generic IAggregateRepository.
        // This will be implemented when Marten-specific integration is configured (Phase 4+).
        _logger.LogDebug("Event history requested for location '{LocationId}' (not yet available)", locationId);
        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<object>>>(
            DataResidencyErrors.EventHistoryUnavailable(locationId));
    }

    // ========================================================================
    // Private helpers
    // ========================================================================

    private void InvalidateCache(Guid locationId)
    {
        var cacheKey = $"dr:location:{locationId}";
        // Fire-and-forget cache invalidation — cache misses are acceptable
        _ = _cache.RemoveAsync(cacheKey, CancellationToken.None);
        _logger.ResidencyCacheInvalidated(cacheKey);
    }
}
