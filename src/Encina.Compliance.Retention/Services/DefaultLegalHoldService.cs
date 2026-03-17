using Encina.Caching;
using Encina.Compliance.Retention.Abstractions;
using Encina.Compliance.Retention.Aggregates;
using Encina.Compliance.Retention.Diagnostics;
using Encina.Compliance.Retention.ReadModels;
using Encina.Marten;
using Encina.Marten.Projections;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Compliance.Retention.Services;

/// <summary>
/// Default implementation of <see cref="ILegalHoldService"/> that manages legal hold
/// lifecycle operations via event-sourced aggregates with cross-aggregate coordination.
/// </summary>
/// <remarks>
/// <para>
/// Wraps <see cref="IAggregateRepository{TAggregate}"/> for <see cref="LegalHoldAggregate"/> and
/// <see cref="IReadModelRepository{TReadModel}"/> for <see cref="LegalHoldReadModel"/> to provide
/// a clean CQRS API for managing legal holds. All write operations go through the aggregate
/// (command side), while read operations use the projected read model (query side).
/// </para>
/// <para>
/// <b>Cross-Aggregate Coordination</b>: When a hold is placed, the service uses
/// <see cref="IRetentionRecordService"/> to cascade <c>Hold</c> operations to all affected
/// retention records for the entity. When a hold is lifted and no other active holds remain,
/// it cascades <c>Release</c> operations to restore normal lifecycle processing.
/// </para>
/// <para>
/// Cache key pattern: <c>"ret:hold:{id}"</c> for individual hold lookup by ID.
/// Cache invalidation is fire-and-forget — cache misses are acceptable.
/// </para>
/// </remarks>
internal sealed class DefaultLegalHoldService : ILegalHoldService
{
    private static readonly TimeSpan DefaultCacheTtl = TimeSpan.FromMinutes(5);

    private readonly IAggregateRepository<LegalHoldAggregate> _repository;
    private readonly IReadModelRepository<LegalHoldReadModel> _readModelRepository;
    private readonly IReadModelRepository<RetentionRecordReadModel> _recordReadModelRepository;
    private readonly IRetentionRecordService _retentionRecordService;
    private readonly ICacheProvider _cache;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DefaultLegalHoldService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultLegalHoldService"/>.
    /// </summary>
    /// <param name="repository">The aggregate repository for legal hold aggregates.</param>
    /// <param name="readModelRepository">The read model repository for legal hold projections.</param>
    /// <param name="recordReadModelRepository">The read model repository for retention record projections (for cross-aggregate queries).</param>
    /// <param name="retentionRecordService">The retention record service for cascading hold/release operations.</param>
    /// <param name="cache">The cache provider for read model caching.</param>
    /// <param name="timeProvider">The time provider for UTC timestamps.</param>
    /// <param name="logger">The logger instance.</param>
    public DefaultLegalHoldService(
        IAggregateRepository<LegalHoldAggregate> repository,
        IReadModelRepository<LegalHoldReadModel> readModelRepository,
        IReadModelRepository<RetentionRecordReadModel> recordReadModelRepository,
        IRetentionRecordService retentionRecordService,
        ICacheProvider cache,
        TimeProvider timeProvider,
        ILogger<DefaultLegalHoldService> logger)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(readModelRepository);
        ArgumentNullException.ThrowIfNull(recordReadModelRepository);
        ArgumentNullException.ThrowIfNull(retentionRecordService);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _repository = repository;
        _readModelRepository = readModelRepository;
        _recordReadModelRepository = recordReadModelRepository;
        _retentionRecordService = retentionRecordService;
        _cache = cache;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    // ========================================================================
    // Command operations
    // ========================================================================

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Guid>> PlaceHoldAsync(
        string entityId,
        string reason,
        string appliedByUserId,
        string? tenantId = null,
        string? moduleId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Placing legal hold. EntityId='{EntityId}', Reason='{Reason}'",
            entityId, reason);

        try
        {
            var id = Guid.NewGuid();
            var appliedAtUtc = _timeProvider.GetUtcNow();

            var aggregate = LegalHoldAggregate.Place(
                id, entityId, reason, appliedByUserId, appliedAtUtc, tenantId, moduleId);

            var result = await _repository.CreateAsync(aggregate, cancellationToken);

            return await result.MatchAsync<Either<EncinaError, Guid>>(
                RightAsync: async _ =>
                {
                    _logger.LegalHoldPlacedES(id, entityId, reason);
                    RetentionDiagnostics.LegalHoldsAppliedTotal.Add(1);

                    // Cross-aggregate coordination: hold all retention records for this entity
                    await CascadeHoldToRecordsAsync(entityId, id, cancellationToken);

                    return id;
                },
                Left: error => error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.RetentionServiceError("PlaceHold", ex);
            return RetentionErrors.ServiceError("PlaceHold", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> LiftHoldAsync(
        Guid holdId,
        string releasedByUserId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Lifting legal hold '{HoldId}'", holdId);

        try
        {
            var loadResult = await _repository.LoadAsync(holdId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var releasedAtUtc = _timeProvider.GetUtcNow();
                    aggregate.Lift(releasedByUserId, releasedAtUtc);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return await saveResult.MatchAsync<Either<EncinaError, Unit>>(
                        RightAsync: async _ =>
                        {
                            _logger.LegalHoldLiftedES(holdId, releasedByUserId);
                            RetentionDiagnostics.LegalHoldsReleasedTotal.Add(1);
                            InvalidateCache(holdId);

                            // Cross-aggregate coordination: release records if no other active holds
                            await CascadeReleaseToRecordsAsync(aggregate.EntityId, holdId, cancellationToken);

                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => RetentionErrors.HoldNotFound(holdId.ToString()));
        }
        catch (InvalidOperationException)
        {
            _logger.RetentionInvalidStateTransition(holdId, "LiftHold");
            return RetentionErrors.InvalidStateTransition(holdId, "LiftHold");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.RetentionServiceError("LiftHold", ex);
            return RetentionErrors.ServiceError("LiftHold", ex);
        }
    }

    // ========================================================================
    // Query operations
    // ========================================================================

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, LegalHoldReadModel>> GetHoldAsync(
        Guid holdId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting legal hold '{HoldId}'", holdId);

        var cacheKey = $"ret:hold:{holdId}";

        try
        {
            var cached = await _cache.GetAsync<LegalHoldReadModel>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                _logger.RetentionCacheHit(cacheKey);
                return cached;
            }

            var result = await _readModelRepository.GetByIdAsync(holdId, cancellationToken);

            return await result.MatchAsync<Either<EncinaError, LegalHoldReadModel>>(
                RightAsync: async readModel =>
                {
                    await _cache.SetAsync(cacheKey, readModel, DefaultCacheTtl, cancellationToken);
                    return readModel;
                },
                Left: _ => RetentionErrors.HoldNotFound(holdId.ToString()));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.RetentionServiceError("GetHold", ex);
            return RetentionErrors.ServiceError("GetHold", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<LegalHoldReadModel>>> GetActiveHoldsForEntityAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting active holds for entity '{EntityId}'", entityId);

        try
        {
            return await _readModelRepository.QueryAsync(
                q => q.Where(h => h.EntityId == entityId && h.IsActive),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.RetentionServiceError("GetActiveHoldsForEntity", ex);
            return RetentionErrors.ServiceError("GetActiveHoldsForEntity", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<LegalHoldReadModel>>> GetAllActiveHoldsAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all active legal holds");

        try
        {
            return await _readModelRepository.QueryAsync(
                q => q.Where(h => h.IsActive),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.RetentionServiceError("GetAllActiveHolds", ex);
            return RetentionErrors.ServiceError("GetAllActiveHolds", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, bool>> HasActiveHoldsAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking active holds for entity '{EntityId}'", entityId);

        try
        {
            var result = await _readModelRepository.QueryAsync(
                q => q.Where(h => h.EntityId == entityId && h.IsActive),
                cancellationToken);

            return result.Match<Either<EncinaError, bool>>(
                Right: holds => holds.Count > 0,
                Left: error => error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.RetentionServiceError("HasActiveHolds", ex);
            return RetentionErrors.ServiceError("HasActiveHolds", ex);
        }
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<object>>> GetHoldHistoryAsync(
        Guid holdId,
        CancellationToken cancellationToken = default)
    {
        // Event history retrieval requires direct Marten event stream access,
        // which is not available through the generic IAggregateRepository.
        // This will be implemented when Marten-specific integration is configured (Phase 4+).
        _logger.LogDebug("Event history requested for hold '{HoldId}' (not yet available)", holdId);
        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<object>>>(
            RetentionErrors.EventHistoryUnavailable(holdId));
    }

    // ========================================================================
    // Cross-aggregate coordination helpers
    // ========================================================================

    /// <summary>
    /// Cascades a legal hold to all retention records for the specified entity.
    /// </summary>
    private async Task CascadeHoldToRecordsAsync(
        string entityId,
        Guid legalHoldId,
        CancellationToken cancellationToken)
    {
        try
        {
            var recordsResult = await _recordReadModelRepository.QueryAsync(
                q => q.Where(r =>
                    r.EntityId == entityId
                    && r.Status != Model.RetentionStatus.Deleted),
                cancellationToken);

            if (recordsResult.IsLeft)
            {
                var error = (EncinaError)recordsResult;
                _logger.LegalHoldCascadeFailed(entityId, error.Message);
                return;
            }

            var records = recordsResult.Match(
                Right: r => r,
                Left: _ => (IReadOnlyList<RetentionRecordReadModel>)[]);
            var affectedCount = 0;
            foreach (var record in records)
            {
                // Best-effort cascade — individual record hold failures should not block the hold creation
                await _retentionRecordService.HoldRecordAsync(record.Id, legalHoldId, cancellationToken);
                affectedCount++;
            }

            _logger.RetentionCrossAggregateCascade(entityId, "Hold", affectedCount);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LegalHoldCascadeFailed(entityId, ex.Message);
        }
    }

    /// <summary>
    /// Cascades a release to all retention records for the specified entity if no other active holds remain.
    /// </summary>
    private async Task CascadeReleaseToRecordsAsync(
        string entityId,
        Guid legalHoldId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Check if there are other active holds for this entity
            var activeHoldsResult = await _readModelRepository.QueryAsync(
                q => q.Where(h => h.EntityId == entityId && h.IsActive),
                cancellationToken);

            var hasOtherHolds = activeHoldsResult.Match(
                Right: holds => holds.Count > 0,
                Left: _ => false);

            if (hasOtherHolds)
            {
                _logger.LegalHoldOtherHoldsRemain(entityId);
                return;
            }

            // No other active holds — release all held records for this entity
            var recordsResult = await _recordReadModelRepository.QueryAsync(
                q => q.Where(r =>
                    r.EntityId == entityId
                    && r.Status == Model.RetentionStatus.UnderLegalHold),
                cancellationToken);

            if (recordsResult.IsLeft)
            {
                var error = (EncinaError)recordsResult;
                _logger.LegalHoldCascadeFailed(entityId, error.Message);
                return;
            }

            var records = recordsResult.Match(
                Right: r => r,
                Left: _ => (IReadOnlyList<RetentionRecordReadModel>)[]);
            var affectedCount = 0;
            foreach (var record in records)
            {
                // Best-effort cascade — individual record release failures should not block the hold lift
                await _retentionRecordService.ReleaseRecordAsync(record.Id, legalHoldId, cancellationToken);
                affectedCount++;
            }

            _logger.RetentionCrossAggregateCascade(entityId, "Release", affectedCount);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LegalHoldCascadeFailed(entityId, ex.Message);
        }
    }

    // ========================================================================
    // Private helpers
    // ========================================================================

    private void InvalidateCache(Guid holdId)
    {
        var cacheKey = $"ret:hold:{holdId}";
        // Fire-and-forget cache invalidation — cache misses are acceptable
        _ = _cache.RemoveAsync(cacheKey, CancellationToken.None);
        _logger.RetentionCacheInvalidated(cacheKey);
    }
}
