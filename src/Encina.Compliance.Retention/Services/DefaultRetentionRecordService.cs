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

namespace Encina.Compliance.Retention.Services;

/// <summary>
/// Default implementation of <see cref="IRetentionRecordService"/> that manages retention
/// record lifecycle operations via event-sourced aggregates.
/// </summary>
/// <remarks>
/// <para>
/// Wraps <see cref="IAggregateRepository{TAggregate}"/> for <see cref="RetentionRecordAggregate"/> and
/// <see cref="IReadModelRepository{TReadModel}"/> for <see cref="RetentionRecordReadModel"/> to provide
/// a clean CQRS API for managing retention records. All write operations go through the aggregate
/// (command side), while read operations use the projected read model (query side).
/// </para>
/// <para>
/// Cache key pattern: <c>"ret:record:{id}"</c> for individual record lookup by ID.
/// Cache invalidation is fire-and-forget — cache misses are acceptable.
/// </para>
/// <para>
/// The <see cref="TrackEntityAsync"/> method computes the expiration timestamp as
/// <c>now + retentionPeriod</c>, ensuring precise tracking per GDPR Article 5(1)(e)
/// storage limitation.
/// </para>
/// </remarks>
internal sealed class DefaultRetentionRecordService : IRetentionRecordService
{
    private static readonly TimeSpan DefaultCacheTtl = TimeSpan.FromMinutes(5);

    private readonly IAggregateRepository<RetentionRecordAggregate> _repository;
    private readonly IReadModelRepository<RetentionRecordReadModel> _readModelRepository;
    private readonly ICacheProvider _cache;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DefaultRetentionRecordService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultRetentionRecordService"/>.
    /// </summary>
    /// <param name="repository">The aggregate repository for retention record aggregates.</param>
    /// <param name="readModelRepository">The read model repository for retention record projections.</param>
    /// <param name="cache">The cache provider for read model caching.</param>
    /// <param name="timeProvider">The time provider for UTC timestamps.</param>
    /// <param name="logger">The logger instance.</param>
    public DefaultRetentionRecordService(
        IAggregateRepository<RetentionRecordAggregate> repository,
        IReadModelRepository<RetentionRecordReadModel> readModelRepository,
        ICacheProvider cache,
        TimeProvider timeProvider,
        ILogger<DefaultRetentionRecordService> logger)
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
    public async ValueTask<Either<EncinaError, Guid>> TrackEntityAsync(
        string entityId,
        string dataCategory,
        Guid policyId,
        TimeSpan retentionPeriod,
        string? tenantId = null,
        string? moduleId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Tracking entity for retention. EntityId='{EntityId}', DataCategory='{DataCategory}'",
            entityId, dataCategory);

        try
        {
            var id = Guid.NewGuid();
            var now = _timeProvider.GetUtcNow();
            var expiresAtUtc = now + retentionPeriod;

            var aggregate = RetentionRecordAggregate.Track(
                id, entityId, dataCategory, policyId, retentionPeriod,
                expiresAtUtc, now, tenantId, moduleId);

            var result = await _repository.CreateAsync(aggregate, cancellationToken);

            return result.Match<Either<EncinaError, Guid>>(
                Right: _ =>
                {
                    _logger.RetentionRecordTrackedES(id, entityId, dataCategory, expiresAtUtc);
                    RetentionDiagnostics.RecordsCreatedTotal.Add(1);
                    return id;
                },
                Left: error => error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.RetentionServiceError("TrackEntity", ex);
            return RetentionErrors.ServiceError("TrackEntity", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> MarkExpiredAsync(
        Guid recordId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Marking retention record '{RecordId}' as expired", recordId);

        try
        {
            var loadResult = await _repository.LoadAsync(recordId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var occurredAtUtc = _timeProvider.GetUtcNow();
                    aggregate.MarkExpired(occurredAtUtc);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.RetentionRecordExpiredES(recordId);
                            InvalidateCache(recordId);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => RetentionErrors.RecordNotFound(recordId.ToString()));
        }
        catch (InvalidOperationException)
        {
            _logger.RetentionInvalidStateTransition(recordId, "MarkExpired");
            return RetentionErrors.InvalidStateTransition(recordId, "MarkExpired");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.RetentionServiceError("MarkExpired", ex);
            return RetentionErrors.ServiceError("MarkExpired", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> HoldRecordAsync(
        Guid recordId,
        Guid legalHoldId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Placing legal hold on retention record '{RecordId}'", recordId);

        try
        {
            var loadResult = await _repository.LoadAsync(recordId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var occurredAtUtc = _timeProvider.GetUtcNow();
                    aggregate.Hold(legalHoldId, occurredAtUtc);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.RetentionRecordHeldES(recordId, legalHoldId);
                            RetentionDiagnostics.RecordsHeldTotal.Add(1);
                            InvalidateCache(recordId);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => RetentionErrors.RecordNotFound(recordId.ToString()));
        }
        catch (InvalidOperationException)
        {
            _logger.RetentionInvalidStateTransition(recordId, "HoldRecord");
            return RetentionErrors.InvalidStateTransition(recordId, "HoldRecord");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.RetentionServiceError("HoldRecord", ex);
            return RetentionErrors.ServiceError("HoldRecord", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> ReleaseRecordAsync(
        Guid recordId,
        Guid legalHoldId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Releasing legal hold from retention record '{RecordId}'", recordId);

        try
        {
            var loadResult = await _repository.LoadAsync(recordId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var occurredAtUtc = _timeProvider.GetUtcNow();
                    aggregate.Release(legalHoldId, occurredAtUtc);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.RetentionRecordReleasedES(recordId);
                            InvalidateCache(recordId);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => RetentionErrors.RecordNotFound(recordId.ToString()));
        }
        catch (InvalidOperationException)
        {
            _logger.RetentionInvalidStateTransition(recordId, "ReleaseRecord");
            return RetentionErrors.InvalidStateTransition(recordId, "ReleaseRecord");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.RetentionServiceError("ReleaseRecord", ex);
            return RetentionErrors.ServiceError("ReleaseRecord", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> MarkDeletedAsync(
        Guid recordId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Marking retention record '{RecordId}' as deleted", recordId);

        try
        {
            var loadResult = await _repository.LoadAsync(recordId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var deletedAtUtc = _timeProvider.GetUtcNow();
                    aggregate.MarkDeleted(deletedAtUtc);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.RetentionDataDeletedES(recordId);
                            RetentionDiagnostics.RecordsDeletedTotal.Add(1);
                            InvalidateCache(recordId);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => RetentionErrors.RecordNotFound(recordId.ToString()));
        }
        catch (InvalidOperationException)
        {
            _logger.RetentionInvalidStateTransition(recordId, "MarkDeleted");
            return RetentionErrors.InvalidStateTransition(recordId, "MarkDeleted");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.RetentionServiceError("MarkDeleted", ex);
            return RetentionErrors.ServiceError("MarkDeleted", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> MarkAnonymizedAsync(
        Guid recordId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Marking retention record '{RecordId}' as anonymized", recordId);

        try
        {
            var loadResult = await _repository.LoadAsync(recordId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var anonymizedAtUtc = _timeProvider.GetUtcNow();
                    aggregate.MarkAnonymized(anonymizedAtUtc);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.RetentionDataAnonymizedES(recordId);
                            RetentionDiagnostics.RecordsDeletedTotal.Add(1);
                            InvalidateCache(recordId);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => RetentionErrors.RecordNotFound(recordId.ToString()));
        }
        catch (InvalidOperationException)
        {
            _logger.RetentionInvalidStateTransition(recordId, "MarkAnonymized");
            return RetentionErrors.InvalidStateTransition(recordId, "MarkAnonymized");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.RetentionServiceError("MarkAnonymized", ex);
            return RetentionErrors.ServiceError("MarkAnonymized", ex);
        }
    }

    // ========================================================================
    // Query operations
    // ========================================================================

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, RetentionRecordReadModel>> GetRecordAsync(
        Guid recordId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting retention record '{RecordId}'", recordId);

        var cacheKey = $"ret:record:{recordId}";

        try
        {
            var cached = await _cache.GetAsync<RetentionRecordReadModel>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                _logger.RetentionCacheHit(cacheKey);
                return cached;
            }

            var result = await _readModelRepository.GetByIdAsync(recordId, cancellationToken);

            return await result.MatchAsync<Either<EncinaError, RetentionRecordReadModel>>(
                RightAsync: async readModel =>
                {
                    await _cache.SetAsync(cacheKey, readModel, DefaultCacheTtl, cancellationToken);
                    return readModel;
                },
                Left: _ => RetentionErrors.RecordNotFound(recordId.ToString()));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.RetentionServiceError("GetRecord", ex);
            return RetentionErrors.ServiceError("GetRecord", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<RetentionRecordReadModel>>> GetRecordsByEntityAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting retention records for entity '{EntityId}'", entityId);

        try
        {
            return await _readModelRepository.QueryAsync(
                q => q.Where(r => r.EntityId == entityId),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.RetentionServiceError("GetRecordsByEntity", ex);
            return RetentionErrors.ServiceError("GetRecordsByEntity", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<RetentionRecordReadModel>>> GetRecordsByStatusAsync(
        RetentionStatus status,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting retention records by status '{Status}'", status);

        try
        {
            return await _readModelRepository.QueryAsync(
                q => q.Where(r => r.Status == status),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.RetentionServiceError("GetRecordsByStatus", ex);
            return RetentionErrors.ServiceError("GetRecordsByStatus", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<RetentionRecordReadModel>>> GetExpiredRecordsAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting expired retention records");

        try
        {
            var now = _timeProvider.GetUtcNow();

            return await _readModelRepository.QueryAsync(
                q => q.Where(r =>
                    r.Status == RetentionStatus.Active
                    && r.ExpiresAtUtc <= now),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.RetentionServiceError("GetExpiredRecords", ex);
            return RetentionErrors.ServiceError("GetExpiredRecords", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<RetentionRecordReadModel>>> GetRecordsByPolicyAsync(
        Guid policyId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting retention records for policy '{PolicyId}'", policyId);

        try
        {
            return await _readModelRepository.QueryAsync(
                q => q.Where(r => r.PolicyId == policyId),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.RetentionServiceError("GetRecordsByPolicy", ex);
            return RetentionErrors.ServiceError("GetRecordsByPolicy", ex);
        }
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<object>>> GetRecordHistoryAsync(
        Guid recordId,
        CancellationToken cancellationToken = default)
    {
        // Event history retrieval requires direct Marten event stream access,
        // which is not available through the generic IAggregateRepository.
        // This will be implemented when Marten-specific integration is configured (Phase 4+).
        _logger.LogDebug("Event history requested for record '{RecordId}' (not yet available)", recordId);
        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<object>>>(
            RetentionErrors.EventHistoryUnavailable(recordId));
    }

    // ========================================================================
    // Private helpers
    // ========================================================================

    private void InvalidateCache(Guid recordId)
    {
        var cacheKey = $"ret:record:{recordId}";
        // Fire-and-forget cache invalidation — cache misses are acceptable
        _ = _cache.RemoveAsync(cacheKey, CancellationToken.None);
        _logger.RetentionCacheInvalidated(cacheKey);
    }
}
