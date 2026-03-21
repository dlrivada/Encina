using Encina.Caching;
using Encina.Compliance.DPIA.Abstractions;
using Encina.Compliance.DPIA.Aggregates;
using Encina.Compliance.DPIA.Diagnostics;
using Encina.Compliance.DPIA.Model;
using Encina.Compliance.DPIA.ReadModels;
using Encina.Marten;
using Encina.Marten.Projections;
using LanguageExt;
using Marten;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.DPIA.Services;

/// <summary>
/// Default implementation of <see cref="IDPIAService"/> that manages DPIA assessment
/// lifecycle operations via event-sourced aggregates and projected read models.
/// </summary>
/// <remarks>
/// <para>
/// Wraps <see cref="IAggregateRepository{TAggregate}"/> for <see cref="DPIAAggregate"/>
/// to handle write operations (create, evaluate, approve, reject, etc.) and
/// <see cref="IReadModelRepository{TReadModel}"/> for <see cref="DPIAReadModel"/> to
/// handle query operations.
/// </para>
/// <para>
/// Cache key pattern: <c>"dpia:{id}"</c> for individual lookups,
/// <c>"dpia:type:{requestTypeName}"</c> for request-type-based lookups.
/// </para>
/// </remarks>
internal sealed class DefaultDPIAService : IDPIAService
{
    private readonly IAggregateRepository<DPIAAggregate> _aggregateRepository;
    private readonly IReadModelRepository<DPIAReadModel> _readModelRepository;
    private readonly IDPIAAssessmentEngine _assessmentEngine;
    private readonly IDocumentSession _session;
    private readonly ICacheProvider _cache;
    private readonly TimeProvider _timeProvider;
    private readonly DPIAOptions _options;
    private readonly ILogger<DefaultDPIAService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultDPIAService"/>.
    /// </summary>
    public DefaultDPIAService(
        IAggregateRepository<DPIAAggregate> aggregateRepository,
        IReadModelRepository<DPIAReadModel> readModelRepository,
        IDPIAAssessmentEngine assessmentEngine,
        IDocumentSession session,
        ICacheProvider cache,
        TimeProvider timeProvider,
        IOptions<DPIAOptions> options,
        ILogger<DefaultDPIAService> logger)
    {
        ArgumentNullException.ThrowIfNull(aggregateRepository);
        ArgumentNullException.ThrowIfNull(readModelRepository);
        ArgumentNullException.ThrowIfNull(assessmentEngine);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _aggregateRepository = aggregateRepository;
        _readModelRepository = readModelRepository;
        _assessmentEngine = assessmentEngine;
        _session = session;
        _cache = cache;
        _timeProvider = timeProvider;
        _options = options.Value;
        _logger = logger;
    }

    // ========================================================================
    // Write operations
    // ========================================================================

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Guid>> CreateAssessmentAsync(
        string requestTypeName,
        string? processingType = null,
        string? reason = null,
        string? tenantId = null,
        string? moduleId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Creating DPIA assessment for request type '{RequestType}'",
            requestTypeName);

        try
        {
            var id = Guid.NewGuid();
            var nowUtc = _timeProvider.GetUtcNow();
            var aggregate = DPIAAggregate.Create(
                id, requestTypeName, nowUtc, processingType, reason, tenantId, moduleId);

            var result = await _aggregateRepository.CreateAsync(aggregate, cancellationToken);

            return result.Match<Either<EncinaError, Guid>>(
                Right: _ =>
                {
                    _logger.AssessmentCreated(id, requestTypeName);
                    DPIADiagnostics.ServiceAssessmentCreated.Add(1);
                    InvalidateRequestTypeCache(requestTypeName, cancellationToken);
                    return id;
                },
                Left: error => error);
        }
        catch (ArgumentException ex)
        {
            _logger.ServiceOperationError("CreateAssessment", ex);
            DPIADiagnostics.ServiceOperationErrors.Add(1);
            return DPIAErrors.StoreError("CreateAssessment", ex.Message, ex);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ServiceOperationError("CreateAssessment", ex);
            DPIADiagnostics.ServiceOperationErrors.Add(1);
            return DPIAErrors.StoreError("CreateAssessment", ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, DPIAResult>> EvaluateAssessmentAsync(
        Guid assessmentId,
        DPIAContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Evaluating DPIA assessment '{AssessmentId}'",
            assessmentId);

        try
        {
            var loadResult = await _aggregateRepository.LoadAsync(assessmentId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, DPIAResult>>(
                RightAsync: async aggregate =>
                {
                    var assessResult = await _assessmentEngine.AssessAsync(context, cancellationToken);

                    return await assessResult.MatchAsync<Either<EncinaError, DPIAResult>>(
                        RightAsync: async dpiaResult =>
                        {
                            var nowUtc = _timeProvider.GetUtcNow();
                            aggregate.Evaluate(dpiaResult, nowUtc);
                            var saveResult = await _aggregateRepository.SaveAsync(aggregate, cancellationToken);

                            return saveResult.Match<Either<EncinaError, DPIAResult>>(
                                Right: _ =>
                                {
                                    _logger.AssessmentEvaluated(assessmentId, dpiaResult.OverallRisk.ToString());
                                    DPIADiagnostics.ServiceAssessmentEvaluated.Add(1);
                                    InvalidateAssessmentCache(assessmentId, aggregate.RequestTypeName, cancellationToken);
                                    return dpiaResult;
                                },
                                Left: error => error);
                        },
                        Left: error => error);
                },
                Left: _ => DPIAErrors.AssessmentNotFound(assessmentId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.ServiceOperationError("EvaluateAssessment", ex);
            DPIADiagnostics.ServiceOperationErrors.Add(1);
            return DPIAErrors.StoreError("EvaluateAssessment", ex.Message, ex);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ServiceOperationError("EvaluateAssessment", ex);
            DPIADiagnostics.ServiceOperationErrors.Add(1);
            return DPIAErrors.StoreError("EvaluateAssessment", ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Guid>> RequestDPOConsultationAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Requesting DPO consultation for assessment '{AssessmentId}'",
            assessmentId);

        try
        {
            var dpoName = _options.DPOName;
            var dpoEmail = _options.DPOEmail;

            if (string.IsNullOrWhiteSpace(dpoName) || string.IsNullOrWhiteSpace(dpoEmail))
            {
                _logger.DPOConsultationNoDPO(assessmentId);
                return DPIAErrors.DPOConsultationRequired(assessmentId);
            }

            _logger.DPOContactResolved("DPIAOptions", true);

            var loadResult = await _aggregateRepository.LoadAsync(assessmentId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Guid>>(
                RightAsync: async aggregate =>
                {
                    var consultationId = Guid.NewGuid();
                    var nowUtc = _timeProvider.GetUtcNow();
                    aggregate.RequestDPOConsultation(consultationId, dpoName, dpoEmail, nowUtc);
                    var saveResult = await _aggregateRepository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Guid>>(
                        Right: _ =>
                        {
                            _logger.DPOConsultationCreated(assessmentId, consultationId, RedactEmail(dpoEmail));
                            DPIADiagnostics.DPOConsultationTotal.Add(1);
                            InvalidateAssessmentCache(assessmentId, aggregate.RequestTypeName, cancellationToken);
                            return consultationId;
                        },
                        Left: error => error);
                },
                Left: _ => DPIAErrors.AssessmentNotFound(assessmentId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.ServiceOperationError("RequestDPOConsultation", ex);
            DPIADiagnostics.ServiceOperationErrors.Add(1);
            return DPIAErrors.StoreError("RequestDPOConsultation", ex.Message, ex);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ServiceOperationError("RequestDPOConsultation", ex);
            DPIADiagnostics.ServiceOperationErrors.Add(1);
            return DPIAErrors.StoreError("RequestDPOConsultation", ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RecordDPOResponseAsync(
        Guid assessmentId,
        Guid consultationId,
        DPOConsultationDecision decision,
        string? comments = null,
        string? conditions = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Recording DPO response for assessment '{AssessmentId}', consultation '{ConsultationId}'",
            assessmentId, consultationId);

        try
        {
            var loadResult = await _aggregateRepository.LoadAsync(assessmentId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var nowUtc = _timeProvider.GetUtcNow();
                    aggregate.RecordDPOResponse(consultationId, decision, nowUtc, comments, conditions);
                    var saveResult = await _aggregateRepository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.DPOResponseRecorded(assessmentId, consultationId, decision.ToString());
                            InvalidateAssessmentCache(assessmentId, aggregate.RequestTypeName, cancellationToken);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => DPIAErrors.AssessmentNotFound(assessmentId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.ServiceOperationError("RecordDPOResponse", ex);
            DPIADiagnostics.ServiceOperationErrors.Add(1);
            return DPIAErrors.StoreError("RecordDPOResponse", ex.Message, ex);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ServiceOperationError("RecordDPOResponse", ex);
            DPIADiagnostics.ServiceOperationErrors.Add(1);
            return DPIAErrors.StoreError("RecordDPOResponse", ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> ApproveAssessmentAsync(
        Guid assessmentId,
        string approvedBy,
        DateTimeOffset? nextReviewAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Approving DPIA assessment '{AssessmentId}' by '{ApprovedBy}'",
            assessmentId, approvedBy);

        try
        {
            var loadResult = await _aggregateRepository.LoadAsync(assessmentId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var nowUtc = _timeProvider.GetUtcNow();
                    var reviewAt = nextReviewAtUtc ?? nowUtc.Add(_options.DefaultReviewPeriod);
                    aggregate.Approve(approvedBy, nowUtc, reviewAt);
                    var saveResult = await _aggregateRepository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.AssessmentApproved(assessmentId, approvedBy);
                            DPIADiagnostics.ServiceAssessmentApproved.Add(1);
                            InvalidateAssessmentCache(assessmentId, aggregate.RequestTypeName, cancellationToken);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => DPIAErrors.AssessmentNotFound(assessmentId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.ServiceOperationError("ApproveAssessment", ex);
            DPIADiagnostics.ServiceOperationErrors.Add(1);
            return DPIAErrors.StoreError("ApproveAssessment", ex.Message, ex);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ServiceOperationError("ApproveAssessment", ex);
            DPIADiagnostics.ServiceOperationErrors.Add(1);
            return DPIAErrors.StoreError("ApproveAssessment", ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RejectAssessmentAsync(
        Guid assessmentId,
        string rejectedBy,
        string reason,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Rejecting DPIA assessment '{AssessmentId}' by '{RejectedBy}'",
            assessmentId, rejectedBy);

        try
        {
            var loadResult = await _aggregateRepository.LoadAsync(assessmentId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var nowUtc = _timeProvider.GetUtcNow();
                    aggregate.Reject(rejectedBy, reason, nowUtc);
                    var saveResult = await _aggregateRepository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.AssessmentRejected(assessmentId, rejectedBy);
                            DPIADiagnostics.ServiceAssessmentRejected.Add(1);
                            InvalidateAssessmentCache(assessmentId, aggregate.RequestTypeName, cancellationToken);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => DPIAErrors.AssessmentNotFound(assessmentId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.ServiceOperationError("RejectAssessment", ex);
            DPIADiagnostics.ServiceOperationErrors.Add(1);
            return DPIAErrors.StoreError("RejectAssessment", ex.Message, ex);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ServiceOperationError("RejectAssessment", ex);
            DPIADiagnostics.ServiceOperationErrors.Add(1);
            return DPIAErrors.StoreError("RejectAssessment", ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RequestRevisionAsync(
        Guid assessmentId,
        string requestedBy,
        string reason,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Requesting revision for DPIA assessment '{AssessmentId}' by '{RequestedBy}'",
            assessmentId, requestedBy);

        try
        {
            var loadResult = await _aggregateRepository.LoadAsync(assessmentId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var nowUtc = _timeProvider.GetUtcNow();
                    aggregate.RequestRevision(requestedBy, reason, nowUtc);
                    var saveResult = await _aggregateRepository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.AssessmentRevisionRequested(assessmentId, requestedBy);
                            DPIADiagnostics.ServiceRevisionRequested.Add(1);
                            InvalidateAssessmentCache(assessmentId, aggregate.RequestTypeName, cancellationToken);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => DPIAErrors.AssessmentNotFound(assessmentId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.ServiceOperationError("RequestRevision", ex);
            DPIADiagnostics.ServiceOperationErrors.Add(1);
            return DPIAErrors.StoreError("RequestRevision", ex.Message, ex);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ServiceOperationError("RequestRevision", ex);
            DPIADiagnostics.ServiceOperationErrors.Add(1);
            return DPIAErrors.StoreError("RequestRevision", ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> ExpireAssessmentAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Expiring DPIA assessment '{AssessmentId}'",
            assessmentId);

        try
        {
            var loadResult = await _aggregateRepository.LoadAsync(assessmentId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var nowUtc = _timeProvider.GetUtcNow();
                    aggregate.Expire(nowUtc);
                    var saveResult = await _aggregateRepository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.AssessmentExpired(assessmentId);
                            DPIADiagnostics.ServiceAssessmentExpired.Add(1);
                            InvalidateAssessmentCache(assessmentId, aggregate.RequestTypeName, cancellationToken);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => DPIAErrors.AssessmentNotFound(assessmentId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.ServiceOperationError("ExpireAssessment", ex);
            DPIADiagnostics.ServiceOperationErrors.Add(1);
            return DPIAErrors.StoreError("ExpireAssessment", ex.Message, ex);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ServiceOperationError("ExpireAssessment", ex);
            DPIADiagnostics.ServiceOperationErrors.Add(1);
            return DPIAErrors.StoreError("ExpireAssessment", ex.Message, ex);
        }
    }

    // ========================================================================
    // Read operations
    // ========================================================================

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, DPIAReadModel>> GetAssessmentAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"dpia:{assessmentId}";

        _logger.LogDebug("Getting DPIA assessment '{AssessmentId}'", assessmentId);

        try
        {
            var cached = await _cache.GetAsync<DPIAReadModel>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                return cached;
            }

            var result = await _readModelRepository.GetByIdAsync(assessmentId, cancellationToken);

            return result.Match<Either<EncinaError, DPIAReadModel>>(
                Right: readModel =>
                {
                    _ = _cache.SetAsync(cacheKey, readModel, null, cancellationToken);
                    return readModel;
                },
                Left: _ => DPIAErrors.AssessmentNotFound(assessmentId));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ServiceOperationError("GetAssessment", ex);
            DPIADiagnostics.ServiceOperationErrors.Add(1);
            return DPIAErrors.StoreError("GetAssessment", ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, DPIAReadModel>> GetAssessmentByRequestTypeAsync(
        string requestTypeName,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"dpia:type:{requestTypeName}";

        _logger.LogDebug(
            "Getting DPIA assessment for request type '{RequestType}'",
            requestTypeName);

        try
        {
            var cached = await _cache.GetAsync<DPIAReadModel>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                return cached;
            }

            var result = await _readModelRepository.QueryAsync(
                q => q.Where(rm => rm.RequestTypeName == requestTypeName)
                      .OrderByDescending(rm => rm.LastModifiedAtUtc)
                      .Take(1),
                cancellationToken);

            return result.Match<Either<EncinaError, DPIAReadModel>>(
                Right: readModels =>
                {
                    if (readModels.Count == 0)
                    {
                        return DPIAErrors.AssessmentNotFoundByRequestType(requestTypeName);
                    }

                    var readModel = readModels[0];
                    _ = _cache.SetAsync(cacheKey, readModel, null, cancellationToken);
                    return readModel;
                },
                Left: error => error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ServiceOperationError("GetAssessmentByRequestType", ex);
            DPIADiagnostics.ServiceOperationErrors.Add(1);
            return DPIAErrors.StoreError("GetAssessmentByRequestType", ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DPIAReadModel>>> GetExpiredAssessmentsAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting expired DPIA assessments");

        try
        {
            var nowUtc = _timeProvider.GetUtcNow();
            var result = await _readModelRepository.QueryAsync(
                q => q.Where(rm =>
                    rm.Status == DPIAAssessmentStatus.Approved &&
                    rm.NextReviewAtUtc != null &&
                    rm.NextReviewAtUtc <= nowUtc),
                cancellationToken);

            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ServiceOperationError("GetExpiredAssessments", ex);
            DPIADiagnostics.ServiceOperationErrors.Add(1);
            return DPIAErrors.StoreError("GetExpiredAssessments", ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DPIAReadModel>>> GetAllAssessmentsAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all DPIA assessments");

        try
        {
            return await _readModelRepository.QueryAsync(
                q => q.OrderByDescending(rm => rm.LastModifiedAtUtc),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ServiceOperationError("GetAllAssessments", ex);
            DPIADiagnostics.ServiceOperationErrors.Add(1);
            return DPIAErrors.StoreError("GetAllAssessments", ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<object>>> GetAssessmentHistoryAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Getting event history for DPIA assessment '{AssessmentId}'",
            assessmentId);

        try
        {
            var events = await _session.Events.FetchStreamAsync(assessmentId, token: cancellationToken);

            if (events.Count == 0)
            {
                return DPIAErrors.AssessmentNotFound(assessmentId);
            }

            IReadOnlyList<object> domainEvents = events
                .Select(e => e.Data)
                .ToList();

            return Either<EncinaError, IReadOnlyList<object>>.Right(domainEvents);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ServiceOperationError("GetAssessmentHistory", ex);
            DPIADiagnostics.ServiceOperationErrors.Add(1);
            return DPIAErrors.StoreError("GetAssessmentHistory", ex.Message, ex);
        }
    }

    // ========================================================================
    // Cache invalidation helpers
    // ========================================================================

    private void InvalidateAssessmentCache(Guid assessmentId, string requestTypeName, CancellationToken cancellationToken)
    {
        _ = _cache.RemoveAsync($"dpia:{assessmentId}", cancellationToken);
        InvalidateRequestTypeCache(requestTypeName, cancellationToken);
    }

    private void InvalidateRequestTypeCache(string requestTypeName, CancellationToken cancellationToken)
    {
        _ = _cache.RemoveAsync($"dpia:type:{requestTypeName}", cancellationToken);
    }

    /// <summary>
    /// Extracts only the domain portion of an email address for safe logging.
    /// Returns "***@domain" to avoid exposing PII in log output.
    /// </summary>
    private static string RedactEmail(string? email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return "[not configured]";
        }

        var atIndex = email.IndexOf('@', StringComparison.Ordinal);
        return atIndex >= 0 ? $"***{email[atIndex..]}" : "***";
    }
}
