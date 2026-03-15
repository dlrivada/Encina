using System.Diagnostics;

using Encina.Caching;
using Encina.Compliance.DataSubjectRights.Abstractions;
using Encina.Compliance.DataSubjectRights.Aggregates;
using Encina.Compliance.DataSubjectRights.Diagnostics;
using Encina.Compliance.DataSubjectRights.Projections;
using Encina.Compliance.GDPR;
using Encina.Marten;
using Encina.Marten.Projections;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Compliance.DataSubjectRights.Services;

/// <summary>
/// Default implementation of <see cref="IDSRService"/> that manages DSR request lifecycle,
/// handler operations, and queries via event-sourced aggregates.
/// </summary>
/// <remarks>
/// <para>
/// Wraps <see cref="IAggregateRepository{TAggregate}"/> for <see cref="DSRRequestAggregate"/>
/// (command side) and <see cref="IReadModelRepository{TReadModel}"/> for <see cref="DSRRequestReadModel"/>
/// (query side) to provide a clean CQRS API for DSR management.
/// </para>
/// <para>
/// Handler operations (access, erasure, portability, etc.) coordinate between the aggregate lifecycle
/// and existing executor/locator abstractions (<see cref="IPersonalDataLocator"/>,
/// <see cref="IDataErasureExecutor"/>, <see cref="IDataPortabilityExporter"/>).
/// </para>
/// <para>
/// Cache key patterns:
/// <list type="bullet">
///   <item><description><c>"dsr:request:{id}"</c> — Individual request lookup by ID</description></item>
///   <item><description><c>"dsr:restriction:{subjectId}"</c> — Active restriction check by subject</description></item>
/// </list>
/// Cache invalidation is fire-and-forget — cache misses are acceptable.
/// </para>
/// </remarks>
internal sealed class DefaultDSRService : IDSRService
{
    private readonly IAggregateRepository<DSRRequestAggregate> _repository;
    private readonly IReadModelRepository<DSRRequestReadModel> _readModelRepository;
    private readonly IPersonalDataLocator _locator;
    private readonly IDataErasureExecutor _erasureExecutor;
    private readonly IDataPortabilityExporter _portabilityExporter;
    private readonly IProcessingActivityRegistry _processingActivityRegistry;
    private readonly ICacheProvider _cache;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DefaultDSRService> _logger;
    private readonly IEncina? _encina;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultDSRService"/>.
    /// </summary>
    public DefaultDSRService(
        IAggregateRepository<DSRRequestAggregate> repository,
        IReadModelRepository<DSRRequestReadModel> readModelRepository,
        IPersonalDataLocator locator,
        IDataErasureExecutor erasureExecutor,
        IDataPortabilityExporter portabilityExporter,
        IProcessingActivityRegistry processingActivityRegistry,
        ICacheProvider cache,
        TimeProvider timeProvider,
        ILogger<DefaultDSRService> logger,
        IEncina? encina = null)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(readModelRepository);
        ArgumentNullException.ThrowIfNull(locator);
        ArgumentNullException.ThrowIfNull(erasureExecutor);
        ArgumentNullException.ThrowIfNull(portabilityExporter);
        ArgumentNullException.ThrowIfNull(processingActivityRegistry);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _repository = repository;
        _readModelRepository = readModelRepository;
        _locator = locator;
        _erasureExecutor = erasureExecutor;
        _portabilityExporter = portabilityExporter;
        _processingActivityRegistry = processingActivityRegistry;
        _cache = cache;
        _timeProvider = timeProvider;
        _logger = logger;
        _encina = encina;
    }

    // ========================================================================
    // Lifecycle commands (write-side via DSRRequestAggregate)
    // ========================================================================

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Guid>> SubmitRequestAsync(
        string subjectId,
        DataSubjectRight rightType,
        string? requestDetails = null,
        string? tenantId = null,
        string? moduleId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.DSRRequestStarted(rightType.ToString(), subjectId);

        try
        {
            var id = Guid.NewGuid();
            var receivedAtUtc = _timeProvider.GetUtcNow();

            var aggregate = DSRRequestAggregate.Submit(
                id, subjectId, rightType, receivedAtUtc, requestDetails, tenantId, moduleId);

            var result = await _repository.CreateAsync(aggregate, cancellationToken);

            return result.Match<Either<EncinaError, Guid>>(
                Right: _ =>
                {
                    _logger.DSRRequestCompleted(rightType.ToString(), subjectId);
                    DataSubjectRightsDiagnostics.RequestsTotal.Add(1,
                        new KeyValuePair<string, object?>(DataSubjectRightsDiagnostics.TagRightType, rightType.ToString()),
                        new KeyValuePair<string, object?>(DataSubjectRightsDiagnostics.TagOutcome, "submitted"));
                    return id;
                },
                Left: error => error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.DSRServiceError("SubmitRequest", ex.Message, ex);
            return DSRErrors.ServiceError("SubmitRequest", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> VerifyIdentityAsync(
        Guid requestId,
        string verifiedBy,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Verifying identity for DSR request '{RequestId}' by '{VerifiedBy}'", requestId, verifiedBy);

        try
        {
            var loadResult = await _repository.LoadAsync(requestId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var verifiedAtUtc = _timeProvider.GetUtcNow();
                    aggregate.Verify(verifiedBy, verifiedAtUtc);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            InvalidateRequestCache(requestId);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => DSRErrors.RequestNotFound(requestId.ToString()));
        }
        catch (InvalidOperationException ex)
        {
            _logger.DSRInvalidStateTransition(requestId.ToString(), "VerifyIdentity", ex.Message);
            return DSRErrors.InvalidRequest(ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.DSRServiceError("VerifyIdentity", ex.Message, ex);
            return DSRErrors.ServiceError("VerifyIdentity", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> StartProcessingAsync(
        Guid requestId,
        string? processedByUserId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting processing for DSR request '{RequestId}'", requestId);

        try
        {
            var loadResult = await _repository.LoadAsync(requestId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var startedAtUtc = _timeProvider.GetUtcNow();
                    aggregate.StartProcessing(processedByUserId, startedAtUtc);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            InvalidateRequestCache(requestId);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => DSRErrors.RequestNotFound(requestId.ToString()));
        }
        catch (InvalidOperationException ex)
        {
            _logger.DSRInvalidStateTransition(requestId.ToString(), "StartProcessing", ex.Message);
            return DSRErrors.InvalidRequest(ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.DSRServiceError("StartProcessing", ex.Message, ex);
            return DSRErrors.ServiceError("StartProcessing", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> CompleteRequestAsync(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Completing DSR request '{RequestId}'", requestId);

        try
        {
            var loadResult = await _repository.LoadAsync(requestId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var completedAtUtc = _timeProvider.GetUtcNow();
                    aggregate.Complete(completedAtUtc);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.DSRRequestCompleted(aggregate.RightType.ToString(), aggregate.SubjectId);
                            InvalidateRequestCache(requestId);
                            InvalidateRestrictionCache(aggregate.SubjectId);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => DSRErrors.RequestNotFound(requestId.ToString()));
        }
        catch (InvalidOperationException ex)
        {
            _logger.DSRInvalidStateTransition(requestId.ToString(), "CompleteRequest", ex.Message);
            return DSRErrors.InvalidRequest(ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.DSRServiceError("CompleteRequest", ex.Message, ex);
            return DSRErrors.ServiceError("CompleteRequest", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> DenyRequestAsync(
        Guid requestId,
        string rejectionReason,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Denying DSR request '{RequestId}'", requestId);

        try
        {
            var loadResult = await _repository.LoadAsync(requestId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var deniedAtUtc = _timeProvider.GetUtcNow();
                    aggregate.Deny(rejectionReason, deniedAtUtc);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            InvalidateRequestCache(requestId);
                            InvalidateRestrictionCache(aggregate.SubjectId);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => DSRErrors.RequestNotFound(requestId.ToString()));
        }
        catch (InvalidOperationException ex)
        {
            _logger.DSRInvalidStateTransition(requestId.ToString(), "DenyRequest", ex.Message);
            return DSRErrors.InvalidRequest(ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.DSRServiceError("DenyRequest", ex.Message, ex);
            return DSRErrors.ServiceError("DenyRequest", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> ExtendDeadlineAsync(
        Guid requestId,
        string extensionReason,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Extending deadline for DSR request '{RequestId}'", requestId);

        try
        {
            var loadResult = await _repository.LoadAsync(requestId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var extendedAtUtc = _timeProvider.GetUtcNow();
                    aggregate.Extend(extensionReason, extendedAtUtc);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            InvalidateRequestCache(requestId);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => DSRErrors.RequestNotFound(requestId.ToString()));
        }
        catch (InvalidOperationException ex)
        {
            _logger.DSRInvalidStateTransition(requestId.ToString(), "ExtendDeadline", ex.Message);
            return DSRErrors.InvalidRequest(ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.DSRServiceError("ExtendDeadline", ex.Message, ex);
            return DSRErrors.ServiceError("ExtendDeadline", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> ExpireRequestAsync(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Expiring DSR request '{RequestId}'", requestId);

        try
        {
            var loadResult = await _repository.LoadAsync(requestId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var expiredAtUtc = _timeProvider.GetUtcNow();
                    aggregate.Expire(expiredAtUtc);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            InvalidateRequestCache(requestId);
                            InvalidateRestrictionCache(aggregate.SubjectId);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => DSRErrors.RequestNotFound(requestId.ToString()));
        }
        catch (InvalidOperationException ex)
        {
            _logger.DSRInvalidStateTransition(requestId.ToString(), "ExpireRequest", ex.Message);
            return DSRErrors.InvalidRequest(ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.DSRServiceError("ExpireRequest", ex.Message, ex);
            return DSRErrors.ServiceError("ExpireRequest", ex);
        }
    }

    // ========================================================================
    // Handler operations (orchestrate right-specific processing)
    // ========================================================================

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, AccessResponse>> HandleAccessAsync(
        AccessRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var rightType = DataSubjectRight.Access;

        _logger.DSRRequestStarted(rightType.ToString(), request.SubjectId);
        using var activity = DataSubjectRightsDiagnostics.StartDSRRequest(rightType, request.SubjectId);
        var stopwatch = Stopwatch.StartNew();

        // Locate all personal data
        var locateResult = await _locator.LocateAllDataAsync(request.SubjectId, cancellationToken)
            .ConfigureAwait(false);

        return await locateResult.MatchAsync(
            RightAsync: async locations =>
            {
                // Optionally include processing activities
                IReadOnlyList<ProcessingActivity> activities = [];
                if (request.IncludeProcessingActivities)
                {
                    var activitiesResult = await _processingActivityRegistry
                        .GetAllActivitiesAsync(cancellationToken).ConfigureAwait(false);

                    activitiesResult.Match(
                        Right: a => activities = a,
                        Left: error => _logger.AccessProcessingActivitiesFailed(error.Message));
                }

                var response = new AccessResponse
                {
                    SubjectId = request.SubjectId,
                    Data = locations,
                    ProcessingActivities = activities,
                    GeneratedAtUtc = _timeProvider.GetUtcNow()
                };

                _logger.AccessRequestCompleted(request.SubjectId, locations.Count, activities.Count);
                _logger.DSRRequestCompleted(rightType.ToString(), request.SubjectId);
                RecordSuccess(activity, stopwatch, rightType);

                return Right<EncinaError, AccessResponse>(response);
            },
            Left: error =>
            {
                _logger.DSRRequestFailed(rightType.ToString(), request.SubjectId, error.Message);
                RecordFailure(activity, stopwatch, rightType, error.Message);

                return (Either<EncinaError, AccessResponse>)error;
            }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> HandleRectificationAsync(
        RectificationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var rightType = DataSubjectRight.Rectification;

        _logger.DSRRequestStarted(rightType.ToString(), request.SubjectId);
        using var activity = DataSubjectRightsDiagnostics.StartDSRRequest(rightType, request.SubjectId);
        var stopwatch = Stopwatch.StartNew();

        // Note: Actual rectification requires a provider-specific implementation.
        // This default handler records the event and publishes notifications.

        // Publish Article 19 notification
        await PublishNotificationAsync(
            new DataRectifiedNotification(
                request.SubjectId,
                request.FieldName,
                $"dsr-{Guid.NewGuid():N}",
                _timeProvider.GetUtcNow()),
            cancellationToken).ConfigureAwait(false);

        _logger.RectificationCompleted(request.SubjectId, request.FieldName);
        _logger.DSRRequestCompleted(rightType.ToString(), request.SubjectId);
        RecordSuccess(activity, stopwatch, rightType);

        return unit;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, ErasureResult>> HandleErasureAsync(
        ErasureRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = _timeProvider.GetUtcNow();
        var rightType = DataSubjectRight.Erasure;

        _logger.DSRRequestStarted(rightType.ToString(), request.SubjectId);
        _logger.ErasureStarted(request.SubjectId, request.Reason.ToString());
        using var activity = DataSubjectRightsDiagnostics.StartDSRRequest(rightType, request.SubjectId);
        var stopwatch = Stopwatch.StartNew();

        // Execute erasure
        var scope = request.Scope ?? new ErasureScope { Reason = request.Reason };
        var erasureResult = await _erasureExecutor.EraseAsync(request.SubjectId, scope, cancellationToken)
            .ConfigureAwait(false);

        return await erasureResult.MatchAsync(
            RightAsync: async result =>
            {
                // Publish Article 19 notification if fields were erased
                if (result.FieldsErased > 0)
                {
                    var affectedFields = Enumerable.Range(1, result.FieldsErased)
                        .Select(i => $"field-{i}")
                        .ToList();

                    await PublishNotificationAsync(
                        new DataErasedNotification(
                            request.SubjectId,
                            affectedFields,
                            $"dsr-{Guid.NewGuid():N}",
                            now),
                        cancellationToken).ConfigureAwait(false);
                }

                _logger.DSRRequestCompleted(rightType.ToString(), request.SubjectId);
                RecordSuccess(activity, stopwatch, rightType);

                return Right<EncinaError, ErasureResult>(result);
            },
            Left: error =>
            {
                _logger.DSRRequestFailed(rightType.ToString(), request.SubjectId, error.Message);
                RecordFailure(activity, stopwatch, rightType, error.Message);

                return Left<EncinaError, ErasureResult>(
                    DSRErrors.ErasureFailed(request.SubjectId, error.Message));
            }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> HandleRestrictionAsync(
        RestrictionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = _timeProvider.GetUtcNow();
        var rightType = DataSubjectRight.Restriction;

        _logger.DSRRequestStarted(rightType.ToString(), request.SubjectId);
        using var activity = DataSubjectRightsDiagnostics.StartDSRRequest(rightType, request.SubjectId);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Create a DSR request aggregate for the restriction
            var id = Guid.NewGuid();
            var aggregate = DSRRequestAggregate.Submit(
                id, request.SubjectId, DataSubjectRight.Restriction, now,
                $"Restriction reason: {request.Reason}");

            var createResult = await _repository.CreateAsync(aggregate, cancellationToken);

            return await createResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async _ =>
                {
                    // Publish Article 19 notification
                    await PublishNotificationAsync(
                        new ProcessingRestrictedNotification(
                            request.SubjectId,
                            id.ToString(),
                            now),
                        cancellationToken).ConfigureAwait(false);

                    _logger.RestrictionApplied(request.SubjectId, request.Reason);
                    _logger.DSRRequestCompleted(rightType.ToString(), request.SubjectId);
                    RecordSuccess(activity, stopwatch, rightType);
                    InvalidateRestrictionCache(request.SubjectId);

                    return Right<EncinaError, Unit>(unit);
                },
                Left: error =>
                {
                    _logger.DSRRequestFailed(rightType.ToString(), request.SubjectId, error.Message);
                    RecordFailure(activity, stopwatch, rightType, error.Message);
                    return error;
                });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.DSRServiceError("HandleRestriction", ex.Message, ex);
            RecordFailure(activity, stopwatch, rightType, ex.Message);
            return DSRErrors.ServiceError("HandleRestriction", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, PortabilityResponse>> HandlePortabilityAsync(
        PortabilityRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var rightType = DataSubjectRight.Portability;

        _logger.DSRRequestStarted(rightType.ToString(), request.SubjectId);
        using var activity = DataSubjectRightsDiagnostics.StartDSRRequest(rightType, request.SubjectId);
        var stopwatch = Stopwatch.StartNew();

        var exportResult = await _portabilityExporter.ExportAsync(
            request.SubjectId, request.Format, cancellationToken).ConfigureAwait(false);

        return exportResult.Match(
            Right: response =>
            {
                _logger.DSRRequestCompleted(rightType.ToString(), request.SubjectId);
                RecordSuccess(activity, stopwatch, rightType);

                return Right<EncinaError, PortabilityResponse>(response);
            },
            Left: error =>
            {
                _logger.DSRRequestFailed(rightType.ToString(), request.SubjectId, error.Message);
                RecordFailure(activity, stopwatch, rightType, error.Message);

                return Left<EncinaError, PortabilityResponse>(error);
            });
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> HandleObjectionAsync(
        ObjectionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var rightType = DataSubjectRight.Objection;

        _logger.DSRRequestStarted(rightType.ToString(), request.SubjectId);
        using var activity = DataSubjectRightsDiagnostics.StartDSRRequest(rightType, request.SubjectId);
        var stopwatch = Stopwatch.StartNew();

        // Note: The actual decision to accept or reject the objection requires
        // a balancing test (Article 21(1)) unless it's a direct marketing objection
        // (which is absolute per Article 21(2-3)). This implementation records
        // the objection and defers the decision to the application.

        _logger.ObjectionRecorded(request.SubjectId, request.ProcessingPurpose);
        _logger.DSRRequestCompleted(rightType.ToString(), request.SubjectId);
        RecordSuccess(activity, stopwatch, rightType);

        return ValueTask.FromResult<Either<EncinaError, Unit>>(unit);
    }

    // ========================================================================
    // Query operations (read-side via DSRRequestReadModel)
    // ========================================================================

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, DSRRequestReadModel>> GetRequestAsync(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting DSR request '{RequestId}'", requestId);

        var cacheKey = $"dsr:request:{requestId}";

        try
        {
            var cached = await _cache.GetAsync<DSRRequestReadModel>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                return cached;
            }

            var result = await _readModelRepository.GetByIdAsync(requestId, cancellationToken);

            return await result.MatchAsync<Either<EncinaError, DSRRequestReadModel>>(
                RightAsync: async readModel =>
                {
                    await _cache.SetAsync(cacheKey, readModel, TimeSpan.FromMinutes(5), cancellationToken);
                    return readModel;
                },
                Left: _ => DSRErrors.RequestNotFound(requestId.ToString()));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.DSRServiceError("GetRequest", ex.Message, ex);
            return DSRErrors.ServiceError("GetRequest", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DSRRequestReadModel>>> GetRequestsBySubjectAsync(
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting DSR requests for subject '{SubjectId}'", subjectId);

        try
        {
            return await _readModelRepository.QueryAsync(
                q => q.Where(r => r.SubjectId == subjectId),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.DSRServiceError("GetRequestsBySubject", ex.Message, ex);
            return DSRErrors.ServiceError("GetRequestsBySubject", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DSRRequestReadModel>>> GetPendingRequestsAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting pending DSR requests");

        try
        {
            return await _readModelRepository.QueryAsync(
                q => q.Where(r =>
                    r.Status == DSRRequestStatus.Received
                    || r.Status == DSRRequestStatus.IdentityVerified
                    || r.Status == DSRRequestStatus.InProgress
                    || r.Status == DSRRequestStatus.Extended),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.DSRServiceError("GetPendingRequests", ex.Message, ex);
            return DSRErrors.ServiceError("GetPendingRequests", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DSRRequestReadModel>>> GetOverdueRequestsAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting overdue DSR requests");

        try
        {
            var now = _timeProvider.GetUtcNow();

            var result = await _readModelRepository.QueryAsync(
                q => q.Where(r =>
                    r.Status != DSRRequestStatus.Completed
                    && r.Status != DSRRequestStatus.Rejected
                    && r.Status != DSRRequestStatus.Expired),
                cancellationToken);

            return result.Match<Either<EncinaError, IReadOnlyList<DSRRequestReadModel>>>(
                Right: readModels =>
                {
                    // Filter overdue using the read model helper (considers extended deadline)
                    var overdue = readModels.Where(r => r.IsOverdue(now)).ToList();
                    return overdue;
                },
                Left: error => error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.DSRServiceError("GetOverdueRequests", ex.Message, ex);
            return DSRErrors.ServiceError("GetOverdueRequests", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, bool>> HasActiveRestrictionAsync(
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking active restriction for subject '{SubjectId}'", subjectId);

        var cacheKey = $"dsr:restriction:{subjectId}";

        try
        {
            var cached = await _cache.GetAsync<bool?>(cacheKey, cancellationToken);
            if (cached.HasValue)
            {
                return cached.Value;
            }

            var result = await _readModelRepository.QueryAsync(
                q => q.Where(r =>
                    r.SubjectId == subjectId
                    && r.RightType == DataSubjectRight.Restriction
                    && (r.Status == DSRRequestStatus.Received
                        || r.Status == DSRRequestStatus.IdentityVerified
                        || r.Status == DSRRequestStatus.InProgress)),
                cancellationToken);

            return result.Match<Either<EncinaError, bool>>(
                Right: readModels =>
                {
                    var hasRestriction = readModels.Count > 0;

                    // Cache restriction check for 1 minute (lightweight, frequently called)
                    _ = _cache.SetAsync(cacheKey, (bool?)hasRestriction, TimeSpan.FromMinutes(1), cancellationToken);

                    return hasRestriction;
                },
                Left: error => error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.DSRServiceError("HasActiveRestriction", ex.Message, ex);
            return DSRErrors.ServiceError("HasActiveRestriction", ex);
        }
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<object>>> GetRequestHistoryAsync(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        // Event history retrieval requires direct Marten event stream access,
        // which is not available through the generic IAggregateRepository.
        // This will be implemented when Marten-specific integration is configured (Phase 4+).
        _logger.LogDebug("Event history requested for DSR request '{RequestId}' (not yet available)", requestId);
        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<object>>>(
            DSRErrors.EventHistoryUnavailable(requestId));
    }

    // ========================================================================
    // Private helpers
    // ========================================================================

    private static void RecordSuccess(Activity? activity, Stopwatch stopwatch, DataSubjectRight rightType)
    {
        stopwatch.Stop();
        DataSubjectRightsDiagnostics.RecordCompleted(activity);
        DataSubjectRightsDiagnostics.RequestsTotal.Add(1,
            new KeyValuePair<string, object?>(DataSubjectRightsDiagnostics.TagRightType, rightType.ToString()),
            new KeyValuePair<string, object?>(DataSubjectRightsDiagnostics.TagOutcome, "completed"));
        DataSubjectRightsDiagnostics.RequestDuration.Record(stopwatch.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>(DataSubjectRightsDiagnostics.TagRightType, rightType.ToString()));
    }

    private static void RecordFailure(Activity? activity, Stopwatch stopwatch, DataSubjectRight rightType, string reason)
    {
        stopwatch.Stop();
        DataSubjectRightsDiagnostics.RecordFailed(activity, reason);
        DataSubjectRightsDiagnostics.RequestsTotal.Add(1,
            new KeyValuePair<string, object?>(DataSubjectRightsDiagnostics.TagRightType, rightType.ToString()),
            new KeyValuePair<string, object?>(DataSubjectRightsDiagnostics.TagOutcome, "failed"));
        DataSubjectRightsDiagnostics.RequestDuration.Record(stopwatch.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>(DataSubjectRightsDiagnostics.TagRightType, rightType.ToString()));
    }

    private void InvalidateRequestCache(Guid requestId)
    {
        _ = _cache.RemoveAsync($"dsr:request:{requestId}", CancellationToken.None);
    }

    private void InvalidateRestrictionCache(string subjectId)
    {
        _ = _cache.RemoveAsync($"dsr:restriction:{subjectId}", CancellationToken.None);
    }

    private async ValueTask PublishNotificationAsync<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        if (_encina is null)
        {
            return;
        }

        var result = await _encina.Publish(notification, cancellationToken).ConfigureAwait(false);

        result.Match(
            Right: _ => _logger.NotificationPublished(typeof(TNotification).Name),
            Left: error => _logger.NotificationPublishFailed(typeof(TNotification).Name, error.Message));
    }
}
