using System.Diagnostics;

using Encina.Caching;
using Encina.Compliance.DataSubjectRights.Abstractions;
using Encina.Compliance.DataSubjectRights.Aggregates;
using Encina.Compliance.DataSubjectRights.Diagnostics;
using Encina.Compliance.DataSubjectRights.Projections;
using Encina.Compliance.GDPR;
using Encina.Diagnostics;
using Encina.Marten;
using Encina.Marten.Projections;
using Encina.Tenancy;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
///   <item><description><c>"dsr:tenant:{tenantId|-}:request:{id}"</c> — Individual request lookup by ID</description></item>
///   <item><description><c>"dsr:tenant:{tenantId|-}:restriction:{subjectId}"</c> — Active restriction check by subject</description></item>
/// </list>
/// Keys are scoped by the ambient tenant (the literal <c>"-"</c> when none is present) so a cached
/// answer from one tenant is never served to another (#1963).
/// Cache invalidation is fire-and-forget — cache misses are acceptable.
/// </para>
/// <para>
/// <b>Tenant isolation (#1963):</b> Every query reads the ambient tenant from
/// <see cref="IRequestContextAccessor.RequestContext"/> and filters
/// <see cref="DSRRequestReadModel.TenantId"/> against it; a request loaded by id that belongs to
/// another tenant is reported as not found and never mutated. This mirrors the Consent module
/// (#1315). <see cref="DataSubjectRightsOptions.RequireTenantContext"/> controls whether a missing
/// ambient tenant fails closed: its default (<c>null</c>) fails closed when <c>Encina.Tenancy</c>
/// is registered and allows the unscoped operation otherwise; an explicit <c>false</c> is an
/// opt-out that is logged once (SPEC-002 DEC-006, DEC-009).
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
    private readonly DataSubjectRightsOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DefaultDSRService> _logger;
    private readonly IEncina? _encina;
    private readonly IRequestContextAccessor _requestContextAccessor;
    private readonly bool _isMultiTenantApplication;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultDSRService"/>.
    /// </summary>
    /// <remarks>
    /// <paramref name="tenantProvider"/> is present only when <c>Encina.Tenancy</c> is registered.
    /// It is used solely to detect a multi-tenant application for
    /// <see cref="DataSubjectRightsOptions.RequireTenantContext"/> auto-detection (#1963); queries
    /// scope by the ambient <see cref="IRequestContext"/> directly, never through this provider.
    /// </remarks>
    public DefaultDSRService(
        IAggregateRepository<DSRRequestAggregate> repository,
        IReadModelRepository<DSRRequestReadModel> readModelRepository,
        IPersonalDataLocator locator,
        IDataErasureExecutor erasureExecutor,
        IDataPortabilityExporter portabilityExporter,
        IProcessingActivityRegistry processingActivityRegistry,
        ICacheProvider cache,
        IOptions<DataSubjectRightsOptions> options,
        TimeProvider timeProvider,
        IRequestContextAccessor requestContextAccessor,
        ILogger<DefaultDSRService> logger,
        IEncina? encina = null,
        ITenantProvider? tenantProvider = null)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(readModelRepository);
        ArgumentNullException.ThrowIfNull(locator);
        ArgumentNullException.ThrowIfNull(erasureExecutor);
        ArgumentNullException.ThrowIfNull(portabilityExporter);
        ArgumentNullException.ThrowIfNull(processingActivityRegistry);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(requestContextAccessor);
        ArgumentNullException.ThrowIfNull(logger);

        _repository = repository;
        _readModelRepository = readModelRepository;
        _locator = locator;
        _erasureExecutor = erasureExecutor;
        _portabilityExporter = portabilityExporter;
        _processingActivityRegistry = processingActivityRegistry;
        _cache = cache;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
        _encina = encina;
        _requestContextAccessor = requestContextAccessor;
        _isMultiTenantApplication = tenantProvider is not null;

        // An explicit opt-out (RequireTenantContext == false) while the application is
        // multi-tenant is exactly the case SPEC-002 DEC-006 requires to be logged, never silent.
        if (_options.RequireTenantContext == false && _isMultiTenantApplication)
        {
            _logger.DSRTenantEnforcementOptedOut();
        }
    }

    /// <summary>
    /// Reads the ambient tenant id from the request context, or <c>null</c> when no tenant is
    /// present.
    /// </summary>
    private string? CurrentTenantId => _requestContextAccessor.RequestContext?.TenantId;

    /// <summary>
    /// Whether a missing ambient tenant fails an operation closed: the explicit
    /// <see cref="DataSubjectRightsOptions.RequireTenantContext"/> value when set, otherwise
    /// auto-detected from whether the application registered <c>Encina.Tenancy</c>
    /// (SPEC-002 DEC-006/DEC-009).
    /// </summary>
    private bool IsTenantContextRequired => _options.RequireTenantContext ?? _isMultiTenantApplication;

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
        // The data subject's own identifier is never logged (#1429, following #1314); correlate
        // via the right type instead.
        _logger.DSRRequestStarted(rightType.ToString());

        if (!TryResolveWriteTenantScope("SubmitRequest", tenantId, out var effectiveTenantId, out var tenantError))
        {
            return tenantError!.Value;
        }

        try
        {
            var id = Guid.NewGuid();
            var receivedAtUtc = _timeProvider.GetUtcNow();

            var aggregate = DSRRequestAggregate.Submit(
                id, subjectId, rightType, receivedAtUtc, requestDetails, effectiveTenantId, moduleId);

            var result = await _repository.CreateAsync(aggregate, cancellationToken);

            return result.Match<Either<EncinaError, Guid>>(
                Right: _ =>
                {
                    _logger.DSRRequestCompleted(rightType.ToString());
                    DataSubjectRightsDiagnostics.RequestsTotal.Add(1,
                        new KeyValuePair<string, object?>(DataSubjectRightsDiagnostics.TagRightType, rightType.ToString()),
                        new KeyValuePair<string, object?>(DataSubjectRightsDiagnostics.TagOutcome, "submitted"));
                    return id;
                },
                Left: error => error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.DSRServiceError("SubmitRequest", ex.GetType().Name, ex.ForLogging());
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

        if (!TryResolveTenantScope("VerifyIdentity", out var tenantId, out var tenantError))
        {
            return tenantError!.Value;
        }

        try
        {
            var loadResult = await LoadScopedAsync(requestId, tenantId, cancellationToken);

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
            _logger.DSRInvalidStateTransition(requestId.ToString(), "VerifyIdentity", ex.GetType().Name);
            return DSRErrors.InvalidRequest(ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.DSRServiceError("VerifyIdentity", ex.GetType().Name, ex.ForLogging());
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

        if (!TryResolveTenantScope("StartProcessing", out var tenantId, out var tenantError))
        {
            return tenantError!.Value;
        }

        try
        {
            var loadResult = await LoadScopedAsync(requestId, tenantId, cancellationToken);

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
            _logger.DSRInvalidStateTransition(requestId.ToString(), "StartProcessing", ex.GetType().Name);
            return DSRErrors.InvalidRequest(ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.DSRServiceError("StartProcessing", ex.GetType().Name, ex.ForLogging());
            return DSRErrors.ServiceError("StartProcessing", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> CompleteRequestAsync(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Completing DSR request '{RequestId}'", requestId);

        if (!TryResolveTenantScope("CompleteRequest", out var tenantId, out var tenantError))
        {
            return tenantError!.Value;
        }

        try
        {
            var loadResult = await LoadScopedAsync(requestId, tenantId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var completedAtUtc = _timeProvider.GetUtcNow();
                    aggregate.Complete(completedAtUtc);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.DSRRequestCompleted(aggregate.RightType.ToString());
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
            _logger.DSRInvalidStateTransition(requestId.ToString(), "CompleteRequest", ex.GetType().Name);
            return DSRErrors.InvalidRequest(ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.DSRServiceError("CompleteRequest", ex.GetType().Name, ex.ForLogging());
            return DSRErrors.ServiceError("CompleteRequest", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> DenyRequestAsync(
        Guid requestId,
        DSRRejectionReason rejectionReason,
        string rejectionExplanation,
        CancellationToken cancellationToken = default)
    {
        using var activity = DataSubjectRightsDiagnostics.StartAggregateCommand("Deny", requestId);
        activity?.SetTag(DataSubjectRightsDiagnostics.TagRejectionReason, rejectionReason.ToString());

        Either<EncinaError, Unit> result = ValidateDenyInput(rejectionReason, rejectionExplanation) is { } invalid
            ? invalid
            : await DenyCoreAsync(requestId, rejectionReason, rejectionExplanation, activity, cancellationToken);

        result.IfRight(_ => DataSubjectRightsDiagnostics.RecordCompleted(activity));
        result.IfLeft(error => DataSubjectRightsDiagnostics.RecordFailed(activity, error.GetCode().IfNone("encina.unknown")));
        return result;
    }

    /// <summary>
    /// Validates the input of a denial at the service boundary: a defined reason, a non-blank
    /// explanation and, for a draft-law ground, the option that enables it.
    /// </summary>
    private EncinaError? ValidateDenyInput(DSRRejectionReason rejectionReason, string rejectionExplanation)
    {
        if (!Enum.IsDefined(rejectionReason))
        {
            return DSRErrors.InvalidRequest("The rejection reason is not a defined value.");
        }

        if (string.IsNullOrWhiteSpace(rejectionExplanation))
        {
            return DSRErrors.InvalidRequest("A rejection explanation is required.");
        }

        if (rejectionReason.IsDraftLawGround && !_options.EnableOmnibusArticle12RefusalGrounds)
        {
            _logger.DSRRejectionReasonNotEnabled(rejectionReason.ToString());
            return DSRErrors.RejectionReasonNotEnabled(rejectionReason);
        }

        return null;
    }

    private async ValueTask<Either<EncinaError, Unit>> DenyCoreAsync(
        Guid requestId,
        DSRRejectionReason rejectionReason,
        string rejectionExplanation,
        Activity? activity,
        CancellationToken cancellationToken)
    {
        if (!TryResolveTenantScope("DenyRequest", out var tenantId, out var tenantError))
        {
            return tenantError!.Value;
        }

        try
        {
            var loadResult = await LoadScopedAsync(requestId, tenantId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var deniedAtUtc = _timeProvider.GetUtcNow();
                    aggregate.Deny(rejectionReason, rejectionExplanation, deniedAtUtc);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            OnRequestDenied(aggregate, rejectionReason, activity);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => DSRErrors.RequestNotFound(requestId.ToString()));
        }
        catch (InvalidOperationException ex)
        {
            _logger.DSRInvalidStateTransition(requestId.ToString(), "DenyRequest", ex.GetType().Name);
            return DSRErrors.InvalidRequest(ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.DSRServiceError("DenyRequest", ex.GetType().Name, ex.ForLogging());
            return DSRErrors.ServiceError("DenyRequest", ex);
        }
    }

    private void OnRequestDenied(DSRRequestAggregate aggregate, DSRRejectionReason rejectionReason, Activity? activity)
    {
        if (aggregate.TenantId is not null)
        {
            activity?.SetTag(DataSubjectRightsDiagnostics.TagTenantId, aggregate.TenantId);
        }

        DataSubjectRightsDiagnostics.RecordDenied(rejectionReason, aggregate.RightType);
        _logger.DSRRequestDenied(aggregate.RightType.ToString(), rejectionReason.ToString());
        InvalidateRequestCache(aggregate.Id);
        InvalidateRestrictionCache(aggregate.SubjectId);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> ExtendDeadlineAsync(
        Guid requestId,
        string extensionReason,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Extending deadline for DSR request '{RequestId}'", requestId);

        if (!TryResolveTenantScope("ExtendDeadline", out var tenantId, out var tenantError))
        {
            return tenantError!.Value;
        }

        try
        {
            var loadResult = await LoadScopedAsync(requestId, tenantId, cancellationToken);

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
            _logger.DSRInvalidStateTransition(requestId.ToString(), "ExtendDeadline", ex.GetType().Name);
            return DSRErrors.InvalidRequest(ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.DSRServiceError("ExtendDeadline", ex.GetType().Name, ex.ForLogging());
            return DSRErrors.ServiceError("ExtendDeadline", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> ExpireRequestAsync(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Expiring DSR request '{RequestId}'", requestId);

        if (!TryResolveTenantScope("ExpireRequest", out var tenantId, out var tenantError))
        {
            return tenantError!.Value;
        }

        try
        {
            var loadResult = await LoadScopedAsync(requestId, tenantId, cancellationToken);

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
            _logger.DSRInvalidStateTransition(requestId.ToString(), "ExpireRequest", ex.GetType().Name);
            return DSRErrors.InvalidRequest(ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.DSRServiceError("ExpireRequest", ex.GetType().Name, ex.ForLogging());
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

        _logger.DSRRequestStarted(rightType.ToString());
        using var activity = DataSubjectRightsDiagnostics.StartDSRRequest(rightType);
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
                        Left: error => _logger.AccessProcessingActivitiesFailed(error.GetCode().IfNone("encina.unknown")));
                }

                var response = new AccessResponse
                {
                    SubjectId = request.SubjectId,
                    Data = locations,
                    ProcessingActivities = activities,
                    GeneratedAtUtc = _timeProvider.GetUtcNow()
                };

                _logger.AccessRequestCompleted(locations.Count, activities.Count);
                _logger.DSRRequestCompleted(rightType.ToString());
                RecordSuccess(activity, stopwatch, rightType);

                return Right<EncinaError, AccessResponse>(response);
            },
            Left: error =>
            {
                _logger.DSRRequestFailed(rightType.ToString(), error.GetCode().IfNone("encina.unknown"));
                RecordFailure(activity, stopwatch, rightType, error.GetCode().IfNone("encina.unknown"));

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

        _logger.DSRRequestStarted(rightType.ToString());
        using var activity = DataSubjectRightsDiagnostics.StartDSRRequest(rightType);
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

        _logger.RectificationCompleted(request.FieldName);
        _logger.DSRRequestCompleted(rightType.ToString());
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

        _logger.DSRRequestStarted(rightType.ToString());
        _logger.ErasureStarted(request.Reason.ToString());
        using var activity = DataSubjectRightsDiagnostics.StartDSRRequest(rightType);
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

                _logger.DSRRequestCompleted(rightType.ToString());
                RecordSuccess(activity, stopwatch, rightType);

                return Right<EncinaError, ErasureResult>(result);
            },
            Left: error =>
            {
                _logger.DSRRequestFailed(rightType.ToString(), error.GetCode().IfNone("encina.unknown"));
                RecordFailure(activity, stopwatch, rightType, error.GetCode().IfNone("encina.unknown"));

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

        _logger.DSRRequestStarted(rightType.ToString());
        using var activity = DataSubjectRightsDiagnostics.StartDSRRequest(rightType);
        var stopwatch = Stopwatch.StartNew();

        // The restriction is recorded under the ambient tenant, so it is visible to that tenant's
        // restriction checks and never to another tenant's (#1963).
        if (!TryResolveWriteTenantScope("HandleRestriction", null, out var tenantId, out var tenantError))
        {
            RecordFailure(activity, stopwatch, rightType, DSRErrors.TenantRequiredCode);
            return tenantError!.Value;
        }

        try
        {
            // Create a DSR request aggregate for the restriction
            var id = Guid.NewGuid();
            var aggregate = DSRRequestAggregate.Submit(
                id, request.SubjectId, DataSubjectRight.Restriction, now,
                $"Restriction reason: {request.Reason}", tenantId);

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

                    _logger.RestrictionApplied(request.Reason);
                    _logger.DSRRequestCompleted(rightType.ToString());
                    RecordSuccess(activity, stopwatch, rightType);
                    InvalidateRestrictionCache(request.SubjectId);

                    return Right<EncinaError, Unit>(unit);
                },
                Left: error =>
                {
                    _logger.DSRRequestFailed(rightType.ToString(), error.GetCode().IfNone("encina.unknown"));
                    RecordFailure(activity, stopwatch, rightType, error.GetCode().IfNone("encina.unknown"));
                    return error;
                });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.DSRServiceError("HandleRestriction", ex.GetType().Name, ex.ForLogging());
            RecordFailure(activity, stopwatch, rightType, ex.GetType().Name);
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

        _logger.DSRRequestStarted(rightType.ToString());
        using var activity = DataSubjectRightsDiagnostics.StartDSRRequest(rightType);
        var stopwatch = Stopwatch.StartNew();

        var exportResult = await _portabilityExporter.ExportAsync(
            request.SubjectId, request.Format, cancellationToken).ConfigureAwait(false);

        return exportResult.Match(
            Right: response =>
            {
                _logger.DSRRequestCompleted(rightType.ToString());
                RecordSuccess(activity, stopwatch, rightType);

                return Right<EncinaError, PortabilityResponse>(response);
            },
            Left: error =>
            {
                _logger.DSRRequestFailed(rightType.ToString(), error.GetCode().IfNone("encina.unknown"));
                RecordFailure(activity, stopwatch, rightType, error.GetCode().IfNone("encina.unknown"));

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

        _logger.DSRRequestStarted(rightType.ToString());
        using var activity = DataSubjectRightsDiagnostics.StartDSRRequest(rightType);
        var stopwatch = Stopwatch.StartNew();

        // Note: The actual decision to accept or reject the objection requires
        // a balancing test (Article 21(1)) unless it's a direct marketing objection
        // (which is absolute per Article 21(2-3)). This implementation records
        // the objection and defers the decision to the application.

        _logger.ObjectionRecorded(request.ProcessingPurpose);
        _logger.DSRRequestCompleted(rightType.ToString());
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

        if (!TryResolveTenantScope("GetRequest", out var tenantId, out var tenantError))
        {
            return tenantError!.Value;
        }

        var cacheKey = RequestCacheKey(requestId, tenantId);

        try
        {
            var cached = await _cache.GetAsync<DSRRequestReadModel>(cacheKey, cancellationToken);
            // A tenant-mismatched cache entry is treated as a miss and re-fetched from the read
            // model, which applies the tenant check below (#1963).
            if (cached is not null && TenantsMatch(cached.TenantId, tenantId))
            {
                return cached;
            }

            var result = await _readModelRepository.GetByIdAsync(requestId, cancellationToken);

            return await result.MatchAsync<Either<EncinaError, DSRRequestReadModel>>(
                RightAsync: async readModel =>
                {
                    // A request read by id that belongs to a different tenant is reported as not
                    // found, never returned (#1963).
                    if (!TenantsMatch(readModel.TenantId, tenantId))
                    {
                        return DSRErrors.RequestNotFound(requestId.ToString());
                    }

                    await _cache.SetAsync(cacheKey, readModel, TimeSpan.FromMinutes(5), cancellationToken);
                    return readModel;
                },
                Left: _ => DSRErrors.RequestNotFound(requestId.ToString()));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.DSRServiceError("GetRequest", ex.GetType().Name, ex.ForLogging());
            return DSRErrors.ServiceError("GetRequest", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DSRRequestReadModel>>> GetRequestsBySubjectAsync(
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        // The data subject's own identifier is never logged (#1429, following #1314).
        _logger.LogDebug("Getting DSR requests for subject");

        if (!TryResolveTenantScope("GetRequestsBySubject", out var tenantId, out var tenantError))
        {
            return tenantError!.Value;
        }

        try
        {
            return await _readModelRepository.QueryAsync(
                q => q.Where(r => r.SubjectId == subjectId && r.TenantId == tenantId),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.DSRServiceError("GetRequestsBySubject", ex.GetType().Name, ex.ForLogging());
            return DSRErrors.ServiceError("GetRequestsBySubject", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DSRRequestReadModel>>> GetPendingRequestsAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting pending DSR requests");

        if (!TryResolveTenantScope("GetPendingRequests", out var tenantId, out var tenantError))
        {
            return tenantError!.Value;
        }

        try
        {
            return await _readModelRepository.QueryAsync(
                q => q.Where(r =>
                    r.TenantId == tenantId
                    && (r.Status == DSRRequestStatus.Received
                        || r.Status == DSRRequestStatus.IdentityVerified
                        || r.Status == DSRRequestStatus.InProgress
                        || r.Status == DSRRequestStatus.Extended)),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.DSRServiceError("GetPendingRequests", ex.GetType().Name, ex.ForLogging());
            return DSRErrors.ServiceError("GetPendingRequests", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DSRRequestReadModel>>> GetOverdueRequestsAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting overdue DSR requests");

        if (!TryResolveTenantScope("GetOverdueRequests", out var tenantId, out var tenantError))
        {
            return tenantError!.Value;
        }

        try
        {
            var now = _timeProvider.GetUtcNow();

            var result = await _readModelRepository.QueryAsync(
                q => q.Where(r =>
                    r.TenantId == tenantId
                    && r.Status != DSRRequestStatus.Completed
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
            _logger.DSRServiceError("GetOverdueRequests", ex.GetType().Name, ex.ForLogging());
            return DSRErrors.ServiceError("GetOverdueRequests", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, bool>> HasActiveRestrictionAsync(
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        // The data subject's own identifier is never logged (#1429, following #1314).
        _logger.LogDebug("Checking active restriction for subject");

        if (!TryResolveTenantScope("HasActiveRestriction", out var tenantId, out var tenantError))
        {
            return tenantError!.Value;
        }

        // The cache key is scoped by tenant so one tenant's restriction answer is never served to
        // another (#1963).
        var cacheKey = RestrictionCacheKey(subjectId, tenantId);

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
                    && r.TenantId == tenantId
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
            _logger.DSRServiceError("HasActiveRestriction", ex.GetType().Name, ex.ForLogging());
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

    // A mutation only proceeds after the loaded request matched the ambient tenant, so the
    // ambient tenant names exactly the cache entries the mutation made stale (#1963).
    private void InvalidateRequestCache(Guid requestId)
    {
        _ = _cache.RemoveAsync(RequestCacheKey(requestId, CurrentTenantId), CancellationToken.None);
    }

    private void InvalidateRestrictionCache(string subjectId)
    {
        _ = _cache.RemoveAsync(RestrictionCacheKey(subjectId, CurrentTenantId), CancellationToken.None);
    }

    private static string RequestCacheKey(Guid requestId, string? tenantId) =>
        $"dsr:tenant:{TenantCacheSegment(tenantId)}:request:{requestId}";

    private static string RestrictionCacheKey(string subjectId, string? tenantId) =>
        $"dsr:tenant:{TenantCacheSegment(tenantId)}:restriction:{subjectId}";

    /// <summary>
    /// Renders a tenant id for use inside a cache key: the tenant itself, or the literal
    /// <c>"-"</c> when no tenant is present.
    /// </summary>
    private static string TenantCacheSegment(string? tenantId) =>
        string.IsNullOrEmpty(tenantId) ? "-" : tenantId;

    /// <summary>
    /// Compares two tenant ids treating <c>null</c> and empty string as the same "no tenant"
    /// value.
    /// </summary>
    private static bool TenantsMatch(string? left, string? right) =>
        string.Equals(TenantCacheSegment(left), TenantCacheSegment(right), StringComparison.Ordinal);

    /// <summary>
    /// Resolves the ambient tenant for a query or lifecycle command, failing closed when tenant
    /// isolation is required (<see cref="DataSubjectRightsOptions.RequireTenantContext"/>) and no
    /// tenant is present (#1963, SPEC-002 DEC-006).
    /// </summary>
    private bool TryResolveTenantScope(string operation, out string? tenantId, out EncinaError? error)
    {
        tenantId = NormalizeTenant(CurrentTenantId);

        if (tenantId is null && IsTenantContextRequired)
        {
            return Refuse(operation, out tenantId, out error);
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Resolves the tenant a new request should be recorded under: the explicit
    /// <paramref name="explicitTenantId"/> when it agrees with the ambient tenant, otherwise the
    /// ambient tenant. An explicit tenant that contradicts the ambient one is refused, so a request
    /// scoped to one tenant can never write into another tenant's data (#1963).
    /// </summary>
    private bool TryResolveWriteTenantScope(
        string operation, string? explicitTenantId, out string? tenantId, out EncinaError? error)
    {
        var ambientTenantId = NormalizeTenant(CurrentTenantId);
        var requestedTenantId = NormalizeTenant(explicitTenantId);

        if (Contradicts(requestedTenantId, ambientTenantId))
        {
            return Refuse(operation, out tenantId, out error);
        }

        tenantId = requestedTenantId ?? ambientTenantId;

        if (tenantId is null && IsTenantContextRequired)
        {
            return Refuse(operation, out tenantId, out error);
        }

        error = null;
        return true;
    }

    private static bool Contradicts(string? requestedTenantId, string? ambientTenantId) =>
        requestedTenantId is not null && ambientTenantId is not null
        && !TenantsMatch(requestedTenantId, ambientTenantId);

    private bool Refuse(string operation, out string? tenantId, out EncinaError? error)
    {
        _logger.DSRTenantContextMissing(operation);
        tenantId = null;
        error = DSRErrors.TenantRequired(operation);
        return false;
    }

    private static string? NormalizeTenant(string? tenantId) =>
        string.IsNullOrEmpty(tenantId) ? null : tenantId;

    /// <summary>
    /// Loads a request aggregate for a lifecycle command. A request that does not exist, or that
    /// belongs to another tenant, is reported as not found and is never handed to the caller for
    /// mutation (#1963).
    /// </summary>
    private async ValueTask<Either<EncinaError, DSRRequestAggregate>> LoadScopedAsync(
        Guid requestId, string? tenantId, CancellationToken cancellationToken)
    {
        var loadResult = await _repository.LoadAsync(requestId, cancellationToken);

        return loadResult.Match<Either<EncinaError, DSRRequestAggregate>>(
            Right: aggregate => TenantsMatch(aggregate.TenantId, tenantId)
                ? aggregate
                : DSRErrors.RequestNotFound(requestId.ToString()),
            Left: _ => DSRErrors.RequestNotFound(requestId.ToString()));
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
            Left: error => _logger.NotificationPublishFailed(typeof(TNotification).Name, error.GetCode().IfNone("encina.unknown")));
    }
}
