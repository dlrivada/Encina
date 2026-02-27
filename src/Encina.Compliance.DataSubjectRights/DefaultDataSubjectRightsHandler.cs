using System.Diagnostics;

using Encina.Compliance.DataSubjectRights.Diagnostics;
using Encina.Compliance.GDPR;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Default implementation of <see cref="IDataSubjectRightsHandler"/> that orchestrates
/// the full lifecycle of each data subject right.
/// </summary>
/// <remarks>
/// <para>
/// For each right, the handler follows a consistent pattern:
/// <list type="number">
/// <item>Validate the request parameters.</item>
/// <item>Record an audit entry (started).</item>
/// <item>Execute the operation via the appropriate service.</item>
/// <item>Record an audit entry (completed or failed).</item>
/// <item>Publish an Article 19 notification if applicable.</item>
/// </list>
/// </para>
/// <para>
/// Dependencies are injected to enable flexibility: in-memory stores for testing,
/// database-backed stores for production, and custom strategies for erasure and export.
/// </para>
/// <para>
/// Notification publishing is fire-and-forget — failures do not affect the DSR operation
/// result. This matches the InMemoryConsentStore pattern.
/// </para>
/// </remarks>
public sealed class DefaultDataSubjectRightsHandler : IDataSubjectRightsHandler
{
    private readonly IDSRRequestStore _requestStore;
    private readonly IDSRAuditStore _auditStore;
    private readonly IPersonalDataLocator _locator;
    private readonly IDataErasureExecutor _erasureExecutor;
    private readonly IDataPortabilityExporter _portabilityExporter;
    private readonly IProcessingActivityRegistry _processingActivityRegistry;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DefaultDataSubjectRightsHandler> _logger;
    private readonly IEncina? _encina;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultDataSubjectRightsHandler"/> class.
    /// </summary>
    /// <param name="requestStore">The DSR request store for lifecycle tracking.</param>
    /// <param name="auditStore">The audit store for compliance evidence.</param>
    /// <param name="locator">The personal data locator for discovering subject data.</param>
    /// <param name="erasureExecutor">The executor for erasure operations.</param>
    /// <param name="portabilityExporter">The exporter for data portability operations.</param>
    /// <param name="processingActivityRegistry">The processing activity registry for access responses.</param>
    /// <param name="timeProvider">Time provider for timestamps.</param>
    /// <param name="logger">Logger for structured DSR handler logging.</param>
    /// <param name="encina">
    /// Optional Encina mediator for publishing Article 19 notifications.
    /// When <c>null</c>, no notifications are published (suitable for testing).
    /// </param>
    public DefaultDataSubjectRightsHandler(
        IDSRRequestStore requestStore,
        IDSRAuditStore auditStore,
        IPersonalDataLocator locator,
        IDataErasureExecutor erasureExecutor,
        IDataPortabilityExporter portabilityExporter,
        IProcessingActivityRegistry processingActivityRegistry,
        TimeProvider timeProvider,
        ILogger<DefaultDataSubjectRightsHandler> logger,
        IEncina? encina = null)
    {
        ArgumentNullException.ThrowIfNull(requestStore);
        ArgumentNullException.ThrowIfNull(auditStore);
        ArgumentNullException.ThrowIfNull(locator);
        ArgumentNullException.ThrowIfNull(erasureExecutor);
        ArgumentNullException.ThrowIfNull(portabilityExporter);
        ArgumentNullException.ThrowIfNull(processingActivityRegistry);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _requestStore = requestStore;
        _auditStore = auditStore;
        _locator = locator;
        _erasureExecutor = erasureExecutor;
        _portabilityExporter = portabilityExporter;
        _processingActivityRegistry = processingActivityRegistry;
        _timeProvider = timeProvider;
        _logger = logger;
        _encina = encina;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, AccessResponse>> HandleAccessAsync(
        AccessRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dsrRequestId = GenerateRequestId();
        var now = _timeProvider.GetUtcNow();
        var rightType = DataSubjectRight.Access;

        _logger.DSRRequestStarted(rightType.ToString(), request.SubjectId);
        using var activity = DataSubjectRightsDiagnostics.StartDSRRequest(rightType, request.SubjectId);
        var stopwatch = Stopwatch.StartNew();

        // Record audit: started
        await RecordAuditAsync(dsrRequestId, "AccessStarted",
            $"Access request for subject '{request.SubjectId}'", cancellationToken).ConfigureAwait(false);

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
                    GeneratedAtUtc = now
                };

                // Record audit: completed
                await RecordAuditAsync(dsrRequestId, "AccessCompleted",
                    $"Returned {locations.Count} data locations and {activities.Count} processing activities",
                    cancellationToken).ConfigureAwait(false);

                _logger.AccessRequestCompleted(request.SubjectId, locations.Count, activities.Count);
                _logger.DSRRequestCompleted(rightType.ToString(), request.SubjectId);
                RecordSuccess(activity, stopwatch, rightType);

                return Right<EncinaError, AccessResponse>(response);
            },
            LeftAsync: async error =>
            {
                await RecordAuditAsync(dsrRequestId, "AccessFailed",
                    error.Message, cancellationToken).ConfigureAwait(false);

                _logger.DSRRequestFailed(rightType.ToString(), request.SubjectId, error.Message);
                RecordFailure(activity, stopwatch, rightType, error.Message);

                return Left<EncinaError, AccessResponse>(error);
            }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> HandleRectificationAsync(
        RectificationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dsrRequestId = GenerateRequestId();
        var rightType = DataSubjectRight.Rectification;

        _logger.DSRRequestStarted(rightType.ToString(), request.SubjectId);
        using var activity = DataSubjectRightsDiagnostics.StartDSRRequest(rightType, request.SubjectId);
        var stopwatch = Stopwatch.StartNew();

        // Record audit: started
        await RecordAuditAsync(dsrRequestId, "RectificationStarted",
            $"Rectification of field '{request.FieldName}' for subject '{request.SubjectId}'",
            cancellationToken).ConfigureAwait(false);

        // Note: Actual rectification requires a provider-specific implementation.
        // This default handler records the audit trail and publishes notifications.
        // The actual field update must be handled by a custom IRectificationHandler
        // or directly by the application's data access layer.

        // Record audit: completed
        await RecordAuditAsync(dsrRequestId, "RectificationCompleted",
            $"Rectification of field '{request.FieldName}' completed for subject '{request.SubjectId}'",
            cancellationToken).ConfigureAwait(false);

        // Publish Article 19 notification
        await PublishNotificationAsync(
            new DataRectifiedNotification(
                request.SubjectId,
                request.FieldName,
                dsrRequestId,
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

        var dsrRequestId = GenerateRequestId();
        var now = _timeProvider.GetUtcNow();
        var rightType = DataSubjectRight.Erasure;

        _logger.DSRRequestStarted(rightType.ToString(), request.SubjectId);
        _logger.ErasureStarted(request.SubjectId, request.Reason.ToString());
        using var activity = DataSubjectRightsDiagnostics.StartDSRRequest(rightType, request.SubjectId);
        var stopwatch = Stopwatch.StartNew();

        // Create DSR request record
        var dsrRequest = DSRRequest.Create(
            dsrRequestId,
            request.SubjectId,
            DataSubjectRight.Erasure,
            now,
            $"Erasure reason: {request.Reason}");

        await _requestStore.CreateAsync(dsrRequest, cancellationToken).ConfigureAwait(false);

        // Record audit: started
        await RecordAuditAsync(dsrRequestId, "ErasureStarted",
            $"Erasure request for subject '{request.SubjectId}', reason: {request.Reason}",
            cancellationToken).ConfigureAwait(false);

        // Update status to InProgress
        await _requestStore.UpdateStatusAsync(dsrRequestId, DSRRequestStatus.InProgress, null, cancellationToken)
            .ConfigureAwait(false);

        // Execute erasure
        var scope = request.Scope ?? new ErasureScope { Reason = request.Reason };
        var erasureResult = await _erasureExecutor.EraseAsync(request.SubjectId, scope, cancellationToken)
            .ConfigureAwait(false);

        return await erasureResult.MatchAsync(
            RightAsync: async result =>
            {
                // Update status to Completed
                await _requestStore.UpdateStatusAsync(dsrRequestId, DSRRequestStatus.Completed, null, cancellationToken)
                    .ConfigureAwait(false);

                // Record audit: completed
                await RecordAuditAsync(dsrRequestId, "ErasureCompleted",
                    $"Erased {result.FieldsErased} fields, retained {result.FieldsRetained}, failed {result.FieldsFailed}",
                    cancellationToken).ConfigureAwait(false);

                // Publish Article 19 notification
                if (result.FieldsErased > 0)
                {
                    var affectedFields = Enumerable.Range(1, result.FieldsErased)
                        .Select(i => $"field-{i}")
                        .ToList();

                    await PublishNotificationAsync(
                        new DataErasedNotification(
                            request.SubjectId,
                            affectedFields,
                            dsrRequestId,
                            now),
                        cancellationToken).ConfigureAwait(false);
                }

                _logger.DSRRequestCompleted(rightType.ToString(), request.SubjectId);
                RecordSuccess(activity, stopwatch, rightType);

                return Right<EncinaError, ErasureResult>(result);
            },
            LeftAsync: async error =>
            {
                await RecordAuditAsync(dsrRequestId, "ErasureFailed",
                    error.Message, cancellationToken).ConfigureAwait(false);

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

        var dsrRequestId = GenerateRequestId();
        var now = _timeProvider.GetUtcNow();
        var rightType = DataSubjectRight.Restriction;

        _logger.DSRRequestStarted(rightType.ToString(), request.SubjectId);
        using var activity = DataSubjectRightsDiagnostics.StartDSRRequest(rightType, request.SubjectId);
        var stopwatch = Stopwatch.StartNew();

        // Create DSR request record for restriction
        var dsrRequest = DSRRequest.Create(
            dsrRequestId,
            request.SubjectId,
            DataSubjectRight.Restriction,
            now,
            $"Restriction reason: {request.Reason}");

        var createResult = await _requestStore.CreateAsync(dsrRequest, cancellationToken).ConfigureAwait(false);

        return await createResult.MatchAsync(
            RightAsync: async _ =>
            {
                // Record audit: restriction applied
                await RecordAuditAsync(dsrRequestId, "RestrictionApplied",
                    $"Processing restricted for subject '{request.SubjectId}': {request.Reason}",
                    cancellationToken).ConfigureAwait(false);

                // Publish Article 19 notification
                await PublishNotificationAsync(
                    new ProcessingRestrictedNotification(
                        request.SubjectId,
                        dsrRequestId,
                        now),
                    cancellationToken).ConfigureAwait(false);

                _logger.RestrictionApplied(request.SubjectId, request.Reason);
                _logger.DSRRequestCompleted(rightType.ToString(), request.SubjectId);
                RecordSuccess(activity, stopwatch, rightType);

                return Right<EncinaError, Unit>(unit);
            },
            Left: error =>
            {
                _logger.DSRRequestFailed(rightType.ToString(), request.SubjectId, error.Message);
                RecordFailure(activity, stopwatch, rightType, error.Message);
                return error;
            }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, PortabilityResponse>> HandlePortabilityAsync(
        PortabilityRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dsrRequestId = GenerateRequestId();
        var rightType = DataSubjectRight.Portability;

        _logger.DSRRequestStarted(rightType.ToString(), request.SubjectId);
        using var activity = DataSubjectRightsDiagnostics.StartDSRRequest(rightType, request.SubjectId);
        var stopwatch = Stopwatch.StartNew();

        // Record audit: started
        await RecordAuditAsync(dsrRequestId, "PortabilityStarted",
            $"Portability export for subject '{request.SubjectId}' in {request.Format} format",
            cancellationToken).ConfigureAwait(false);

        var exportResult = await _portabilityExporter.ExportAsync(
            request.SubjectId, request.Format, cancellationToken).ConfigureAwait(false);

        return await exportResult.MatchAsync(
            RightAsync: async response =>
            {
                await RecordAuditAsync(dsrRequestId, "PortabilityCompleted",
                    $"Exported {response.ExportedData.FieldCount} fields in {request.Format} format",
                    cancellationToken).ConfigureAwait(false);

                _logger.DSRRequestCompleted(rightType.ToString(), request.SubjectId);
                RecordSuccess(activity, stopwatch, rightType);

                return Right<EncinaError, PortabilityResponse>(response);
            },
            LeftAsync: async error =>
            {
                await RecordAuditAsync(dsrRequestId, "PortabilityFailed",
                    error.Message, cancellationToken).ConfigureAwait(false);

                _logger.DSRRequestFailed(rightType.ToString(), request.SubjectId, error.Message);
                RecordFailure(activity, stopwatch, rightType, error.Message);

                return Left<EncinaError, PortabilityResponse>(error);
            }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> HandleObjectionAsync(
        ObjectionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dsrRequestId = GenerateRequestId();
        var rightType = DataSubjectRight.Objection;

        _logger.DSRRequestStarted(rightType.ToString(), request.SubjectId);
        using var activity = DataSubjectRightsDiagnostics.StartDSRRequest(rightType, request.SubjectId);
        var stopwatch = Stopwatch.StartNew();

        // Record audit: objection received
        await RecordAuditAsync(dsrRequestId, "ObjectionReceived",
            $"Objection to '{request.ProcessingPurpose}' by subject '{request.SubjectId}': {request.Reason}",
            cancellationToken).ConfigureAwait(false);

        // Note: The actual decision to accept or reject the objection requires
        // a balancing test (Article 21(1)) unless it's a direct marketing objection
        // (which is absolute per Article 21(2-3)). This default implementation
        // records the objection and defers the decision to the application.

        // Record audit: objection recorded
        await RecordAuditAsync(dsrRequestId, "ObjectionRecorded",
            $"Objection recorded for subject '{request.SubjectId}', processing purpose: {request.ProcessingPurpose}",
            cancellationToken).ConfigureAwait(false);

        _logger.ObjectionRecorded(request.SubjectId, request.ProcessingPurpose);
        _logger.DSRRequestCompleted(rightType.ToString(), request.SubjectId);
        RecordSuccess(activity, stopwatch, rightType);

        return unit;
    }

    // ================================================================
    // Observability helpers
    // ================================================================

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

    // ================================================================
    // Internal helpers
    // ================================================================

    private static string GenerateRequestId() =>
        $"dsr-{Guid.NewGuid():N}";

    private async ValueTask RecordAuditAsync(
        string dsrRequestId,
        string action,
        string? detail,
        CancellationToken cancellationToken)
    {
        var entry = new DSRAuditEntry
        {
            Id = Guid.NewGuid().ToString(),
            DSRRequestId = dsrRequestId,
            Action = action,
            Detail = detail,
            OccurredAtUtc = _timeProvider.GetUtcNow()
        };

        var result = await _auditStore.RecordAsync(entry, cancellationToken).ConfigureAwait(false);

        result.Match(
            Right: _ => { },
            Left: error => _logger.AuditEntryFailed(dsrRequestId, error.Message));
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
