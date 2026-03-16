using Encina.Caching;
using Encina.Compliance.BreachNotification.Abstractions;
using Encina.Compliance.BreachNotification.Aggregates;
using Encina.Compliance.BreachNotification.Diagnostics;
using Encina.Compliance.BreachNotification.Model;
using Encina.Compliance.BreachNotification.ReadModels;
using Encina.Marten;
using Encina.Marten.Projections;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Compliance.BreachNotification.Services;

/// <summary>
/// Default implementation of <see cref="IBreachNotificationService"/> that manages breach
/// notification lifecycle operations via event-sourced aggregates.
/// </summary>
/// <remarks>
/// <para>
/// Wraps <see cref="IAggregateRepository{TAggregate}"/> for <see cref="BreachAggregate"/> and
/// <see cref="IReadModelRepository{TReadModel}"/> for <see cref="BreachReadModel"/> to provide
/// a clean CQRS API for managing breaches. All write operations go through the aggregate
/// (command side), while read operations use the projected read model (query side).
/// </para>
/// <para>
/// Cache key pattern: <c>"breach:{id}"</c> for individual breach lookup by ID.
/// Cache invalidation is fire-and-forget — cache misses are acceptable.
/// </para>
/// </remarks>
internal sealed class DefaultBreachNotificationService : IBreachNotificationService
{
    private static readonly TimeSpan DefaultCacheTtl = TimeSpan.FromMinutes(5);

    private readonly IAggregateRepository<BreachAggregate> _repository;
    private readonly IReadModelRepository<BreachReadModel> _readModelRepository;
    private readonly ICacheProvider _cache;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DefaultBreachNotificationService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultBreachNotificationService"/>.
    /// </summary>
    /// <param name="repository">The aggregate repository for breach aggregates.</param>
    /// <param name="readModelRepository">The read model repository for breach projections.</param>
    /// <param name="cache">The cache provider for read model caching.</param>
    /// <param name="timeProvider">The time provider for UTC timestamps.</param>
    /// <param name="logger">The logger instance.</param>
    public DefaultBreachNotificationService(
        IAggregateRepository<BreachAggregate> repository,
        IReadModelRepository<BreachReadModel> readModelRepository,
        ICacheProvider cache,
        TimeProvider timeProvider,
        ILogger<DefaultBreachNotificationService> logger)
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
    public async ValueTask<Either<EncinaError, Guid>> RecordBreachAsync(
        string nature,
        BreachSeverity severity,
        string detectedByRule,
        int estimatedAffectedSubjects,
        string description,
        string? detectedByUserId = null,
        string? tenantId = null,
        string? moduleId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Recording breach. Nature='{Nature}', Severity={Severity}, DetectedByRule='{Rule}'",
            nature, severity, detectedByRule);

        try
        {
            var id = Guid.NewGuid();
            var detectedAtUtc = _timeProvider.GetUtcNow();

            var aggregate = BreachAggregate.Detect(
                id, nature, severity, detectedByRule,
                estimatedAffectedSubjects, description,
                detectedByUserId, detectedAtUtc,
                tenantId, moduleId);

            var result = await _repository.CreateAsync(aggregate, cancellationToken);

            return result.Match<Either<EncinaError, Guid>>(
                Right: _ =>
                {
                    _logger.BreachRecorded(id.ToString(), severity.ToString(), nature);
                    BreachNotificationDiagnostics.BreachesDetectedTotal.Add(1);
                    return id;
                },
                Left: error => error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.BreachServiceError("RecordBreach", ex);
            return BreachNotificationErrors.ServiceError("RecordBreach", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> AssessBreachAsync(
        Guid breachId,
        BreachSeverity updatedSeverity,
        int updatedAffectedSubjects,
        string assessmentSummary,
        string assessedByUserId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Assessing breach '{BreachId}'", breachId);

        try
        {
            var loadResult = await _repository.LoadAsync(breachId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var assessedAtUtc = _timeProvider.GetUtcNow();
                    aggregate.Assess(updatedSeverity, updatedAffectedSubjects, assessmentSummary, assessedByUserId, assessedAtUtc);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.BreachAssessedService(breachId.ToString(), updatedSeverity.ToString());
                            BreachNotificationDiagnostics.BreachesAssessedTotal.Add(1);
                            InvalidateCache(breachId);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => BreachNotificationErrors.NotFound(breachId.ToString()));
        }
        catch (InvalidOperationException ex)
        {
            _logger.BreachInvalidStateTransition(breachId.ToString(), "AssessBreach", ex);
            return BreachNotificationErrors.InvalidStateTransition(breachId, "AssessBreach");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.BreachServiceError("AssessBreach", ex);
            return BreachNotificationErrors.ServiceError("AssessBreach", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> ReportToDPAAsync(
        Guid breachId,
        string authorityName,
        string authorityContactInfo,
        string reportSummary,
        string reportedByUserId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Reporting breach '{BreachId}' to DPA '{Authority}'", breachId, authorityName);

        try
        {
            var loadResult = await _repository.LoadAsync(breachId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var reportedAtUtc = _timeProvider.GetUtcNow();
                    aggregate.ReportToDPA(authorityName, authorityContactInfo, reportSummary, reportedByUserId, reportedAtUtc);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.BreachReportedToDPAService(breachId.ToString(), authorityName);
                            BreachNotificationDiagnostics.AuthorityNotificationsTotal.Add(1);
                            InvalidateCache(breachId);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => BreachNotificationErrors.NotFound(breachId.ToString()));
        }
        catch (InvalidOperationException ex)
        {
            _logger.BreachInvalidStateTransition(breachId.ToString(), "ReportToDPA", ex);
            return BreachNotificationErrors.InvalidStateTransition(breachId, "ReportToDPA");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.BreachServiceError("ReportToDPA", ex);
            return BreachNotificationErrors.ServiceError("ReportToDPA", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> NotifySubjectsAsync(
        Guid breachId,
        int subjectCount,
        string communicationMethod,
        SubjectNotificationExemption exemption,
        string notifiedByUserId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Notifying subjects for breach '{BreachId}', count={SubjectCount}", breachId, subjectCount);

        try
        {
            var loadResult = await _repository.LoadAsync(breachId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var notifiedAtUtc = _timeProvider.GetUtcNow();
                    aggregate.NotifySubjects(subjectCount, communicationMethod, exemption, notifiedByUserId, notifiedAtUtc);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.BreachSubjectsNotifiedService(breachId.ToString(), subjectCount);
                            BreachNotificationDiagnostics.SubjectNotificationsTotal.Add(1);
                            InvalidateCache(breachId);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => BreachNotificationErrors.NotFound(breachId.ToString()));
        }
        catch (InvalidOperationException ex)
        {
            _logger.BreachInvalidStateTransition(breachId.ToString(), "NotifySubjects", ex);
            return BreachNotificationErrors.InvalidStateTransition(breachId, "NotifySubjects");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.BreachServiceError("NotifySubjects", ex);
            return BreachNotificationErrors.ServiceError("NotifySubjects", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> AddPhasedReportAsync(
        Guid breachId,
        string reportContent,
        string submittedByUserId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adding phased report to breach '{BreachId}'", breachId);

        try
        {
            var loadResult = await _repository.LoadAsync(breachId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var submittedAtUtc = _timeProvider.GetUtcNow();
                    aggregate.AddPhasedReport(reportContent, submittedByUserId, submittedAtUtc);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.BreachPhasedReportAddedService(breachId.ToString());
                            BreachNotificationDiagnostics.PhasedReportsTotal.Add(1);
                            InvalidateCache(breachId);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => BreachNotificationErrors.NotFound(breachId.ToString()));
        }
        catch (InvalidOperationException ex)
        {
            _logger.BreachInvalidStateTransition(breachId.ToString(), "AddPhasedReport", ex);
            return BreachNotificationErrors.InvalidStateTransition(breachId, "AddPhasedReport");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.BreachServiceError("AddPhasedReport", ex);
            return BreachNotificationErrors.ServiceError("AddPhasedReport", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> ContainBreachAsync(
        Guid breachId,
        string containmentMeasures,
        string containedByUserId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Containing breach '{BreachId}'", breachId);

        try
        {
            var loadResult = await _repository.LoadAsync(breachId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var containedAtUtc = _timeProvider.GetUtcNow();
                    aggregate.Contain(containmentMeasures, containedByUserId, containedAtUtc);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.BreachContainedService(breachId.ToString());
                            BreachNotificationDiagnostics.BreachesResolvedTotal.Add(1);
                            InvalidateCache(breachId);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => BreachNotificationErrors.NotFound(breachId.ToString()));
        }
        catch (InvalidOperationException ex)
        {
            _logger.BreachInvalidStateTransition(breachId.ToString(), "ContainBreach", ex);
            return BreachNotificationErrors.InvalidStateTransition(breachId, "ContainBreach");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.BreachServiceError("ContainBreach", ex);
            return BreachNotificationErrors.ServiceError("ContainBreach", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> CloseBreachAsync(
        Guid breachId,
        string resolutionSummary,
        string closedByUserId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Closing breach '{BreachId}'", breachId);

        try
        {
            var loadResult = await _repository.LoadAsync(breachId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var closedAtUtc = _timeProvider.GetUtcNow();
                    aggregate.Close(resolutionSummary, closedByUserId, closedAtUtc);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.BreachClosedService(breachId.ToString());
                            BreachNotificationDiagnostics.BreachesClosedTotal.Add(1);
                            InvalidateCache(breachId);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => BreachNotificationErrors.NotFound(breachId.ToString()));
        }
        catch (InvalidOperationException ex)
        {
            _logger.BreachInvalidStateTransition(breachId.ToString(), "CloseBreach", ex);
            return BreachNotificationErrors.InvalidStateTransition(breachId, "CloseBreach");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.BreachServiceError("CloseBreach", ex);
            return BreachNotificationErrors.ServiceError("CloseBreach", ex);
        }
    }

    // ========================================================================
    // Query operations
    // ========================================================================

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, BreachReadModel>> GetBreachAsync(
        Guid breachId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting breach '{BreachId}'", breachId);

        var cacheKey = $"breach:{breachId}";

        try
        {
            var cached = await _cache.GetAsync<BreachReadModel>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                _logger.BreachCacheHit(cacheKey);
                return cached;
            }

            var result = await _readModelRepository.GetByIdAsync(breachId, cancellationToken);

            return await result.MatchAsync<Either<EncinaError, BreachReadModel>>(
                RightAsync: async readModel =>
                {
                    await _cache.SetAsync(cacheKey, readModel, DefaultCacheTtl, cancellationToken);
                    return readModel;
                },
                Left: _ => BreachNotificationErrors.NotFound(breachId.ToString()));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.BreachServiceError("GetBreach", ex);
            return BreachNotificationErrors.ServiceError("GetBreach", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<BreachReadModel>>> GetBreachesByStatusAsync(
        BreachStatus status,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting breaches by status '{Status}'", status);

        try
        {
            return await _readModelRepository.QueryAsync(
                q => q.Where(b => b.Status == status),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.BreachServiceError("GetBreachesByStatus", ex);
            return BreachNotificationErrors.ServiceError("GetBreachesByStatus", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<BreachReadModel>>> GetBreachesByTenantAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting breaches for tenant '{TenantId}'", tenantId);

        try
        {
            return await _readModelRepository.QueryAsync(
                q => q.Where(b => b.TenantId == tenantId),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.BreachServiceError("GetBreachesByTenant", ex);
            return BreachNotificationErrors.ServiceError("GetBreachesByTenant", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<BreachReadModel>>> GetApproachingDeadlineBreachesAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting breaches approaching deadline");

        try
        {
            var now = _timeProvider.GetUtcNow();
            var deadlineWindow = now.AddHours(24);

            return await _readModelRepository.QueryAsync(
                q => q.Where(b =>
                    b.ReportedToDPAAtUtc == null
                    && b.Status != BreachStatus.Closed
                    && b.DeadlineUtc <= deadlineWindow),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.BreachServiceError("GetApproachingDeadlineBreaches", ex);
            return BreachNotificationErrors.ServiceError("GetApproachingDeadlineBreaches", ex);
        }
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<object>>> GetBreachHistoryAsync(
        Guid breachId,
        CancellationToken cancellationToken = default)
    {
        // Event history retrieval requires direct Marten event stream access,
        // which is not available through the generic IAggregateRepository.
        // This will be implemented when Marten-specific integration is configured (Phase 4+).
        _logger.LogDebug("Event history requested for breach '{BreachId}' (not yet available)", breachId);
        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<object>>>(
            BreachNotificationErrors.EventHistoryUnavailable(breachId));
    }

    // ========================================================================
    // Private helpers
    // ========================================================================

    private void InvalidateCache(Guid breachId)
    {
        // Fire-and-forget cache invalidation — cache misses are acceptable
        _ = _cache.RemoveAsync($"breach:{breachId}", CancellationToken.None);
    }
}
