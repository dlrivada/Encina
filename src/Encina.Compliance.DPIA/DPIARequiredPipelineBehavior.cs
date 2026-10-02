using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;

using Encina.Compliance.DPIA.Abstractions;
using Encina.Compliance.DPIA.Diagnostics;
using Encina.Compliance.DPIA.Model;
using Encina.Compliance.DPIA.ReadModels;
using Encina.Diagnostics;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static LanguageExt.Prelude;

namespace Encina.Compliance.DPIA;

/// <summary>
/// Pipeline behavior that enforces DPIA requirements declared via <see cref="RequiresDPIAAttribute"/>.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// <para>
/// Per GDPR Article 35(1), the controller must carry out a Data Protection Impact Assessment
/// "where a type of processing [...] is likely to result in a high risk to the rights and
/// freedoms of natural persons." This behavior provides declarative enforcement at the request
/// pipeline level.
/// </para>
/// <para>
/// The behavior checks that a current, approved DPIA assessment exists <b>before</b> the handler
/// executes. An assessment is "current" when it is <see cref="DPIAAssessmentStatus.Approved"/>
/// and its review date (<see cref="DPIAReadModel.NextReviewAtUtc"/>) has not passed.
/// </para>
/// <para>
/// <b>Attribute resolution:</b> Uses a static <see cref="ConcurrentDictionary{TKey, TValue}"/>
/// to cache <see cref="RequiresDPIAAttribute"/> lookups per request type, ensuring zero
/// reflection overhead on subsequent calls.
/// </para>
/// <para>
/// The enforcement mode is controlled by <see cref="DPIAOptions.EnforcementMode"/>:
/// <see cref="DPIAEnforcementMode.Block"/> returns an error if no valid assessment exists,
/// <see cref="DPIAEnforcementMode.Warn"/> logs a warning but allows processing to continue,
/// <see cref="DPIAEnforcementMode.Disabled"/> skips all DPIA enforcement entirely.
/// </para>
/// <para>
/// <b>Observability:</b> Emits OpenTelemetry traces via <c>Encina.Compliance.DPIA</c> ActivitySource,
/// metrics via <c>Encina.Compliance.DPIA</c> Meter, and structured log messages via
/// <see cref="ILogger"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [RequiresDPIA(ProcessingType = "AutomatedDecisionMaking")]
/// public sealed record CreditScoringCommand(string CustomerId) : ICommand&lt;CreditScore&gt;;
///
/// // The pipeline behavior automatically:
/// // 1. Checks for [RequiresDPIA] attribute (cached)
/// // 2. Looks up existing DPIA assessment via IDPIAService
/// // 3. Validates: exists? + approved? + not expired?
/// // 4. In Block mode, returns error if assessment is invalid
/// // 5. In Warn mode, logs warning but allows through
/// </code>
/// </example>
public sealed class DPIARequiredPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly ConcurrentDictionary<Type, RequiresDPIAAttribute?> AttributeCache = new();

    private readonly IDPIAService _service;
    private readonly DPIAOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DPIARequiredPipelineBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DPIARequiredPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="service">The DPIA service for assessment lookups.</param>
    /// <param name="options">DPIA configuration options controlling enforcement mode.</param>
    /// <param name="timeProvider">Time provider for UTC time comparisons.</param>
    /// <param name="logger">Logger for structured DPIA compliance logging.</param>
    public DPIARequiredPipelineBehavior(
        IDPIAService service,
        IOptions<DPIAOptions> options,
        TimeProvider timeProvider,
        ILogger<DPIARequiredPipelineBehavior<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _service = service;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextStep);

        var requestType = typeof(TRequest);
        var requestTypeName = requestType.Name;

        // Step 1: Check enforcement mode — if disabled, skip entirely
        if (_options.EnforcementMode == DPIAEnforcementMode.Disabled)
        {
            _logger.DPIAPipelineDisabled(requestTypeName);
            return await nextStep().ConfigureAwait(false);
        }

        // Step 2: Check for [RequiresDPIA] attribute (cached)
        var attribute = AttributeCache.GetOrAdd(requestType, static type =>
            type.GetCustomAttribute<RequiresDPIAAttribute>());

        // No attribute — skip entirely
        if (attribute is null)
        {
            _logger.DPIAPipelineNoAttribute(requestTypeName);
            DPIADiagnostics.PipelineCheckSkipped.Add(1, new TagList
            {
                { DPIADiagnostics.TagRequestType, requestTypeName }
            });
            return await nextStep().ConfigureAwait(false);
        }

        // Step 3: Start tracing and logging
        var startedAt = Stopwatch.GetTimestamp();
        using var activity = DPIADiagnostics.StartPipelineCheck(requestTypeName);
        activity?.SetTag(DPIADiagnostics.TagEnforcementMode, _options.EnforcementMode.ToString());

        // Propagate tenant and module context to traces for cross-cutting observability
        if (context.TenantId is not null)
        {
            activity?.SetTag("encina.tenant_id", context.TenantId);
        }

        _logger.DPIAPipelineStarted(requestTypeName, _options.EnforcementMode.ToString());

        // Step 4: Look up existing DPIA assessment by request type's full name via IDPIAService
        var fullTypeName = requestType.FullName ?? requestTypeName;

        var check = new CheckContext(activity, startedAt, requestTypeName, fullTypeName, attribute, nextStep);

        try
        {
            var assessmentResult = await _service
                .GetAssessmentByRequestTypeAsync(fullTypeName, cancellationToken)
                .ConfigureAwait(false);

            return await assessmentResult.Match(
                Right: assessment => EvaluateAssessment(assessment, check),
                Left: error => HandleLookupError(error, check)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return await HandleUnhandledException(ex, check).ConfigureAwait(false);
        }
    }

    // ================================================================
    // Private helpers
    // ================================================================

    private readonly record struct CheckContext(
        Activity? Activity,
        long StartedAt,
        string RequestTypeName,
        string FullTypeName,
        RequiresDPIAAttribute Attribute,
        RequestHandlerCallback<TResponse> NextStep);

    private ValueTask<Either<EncinaError, TResponse>> EvaluateAssessment(DPIAReadModel assessment, CheckContext check)
    {
        var nowUtc = _timeProvider.GetUtcNow();

        // Step 5: Validate assessment is approved
        if (assessment.Status != DPIAAssessmentStatus.Approved)
        {
            var statusName = assessment.Status.ToString();
            var error = assessment.Status == DPIAAssessmentStatus.Rejected
                ? DPIAErrors.AssessmentRejected(assessment.Id, check.FullTypeName)
                : DPIAErrors.AssessmentRequired(check.FullTypeName);

            return HandleFailure(
                check.Activity, check.StartedAt, check.RequestTypeName,
                error,
                $"assessment_not_approved_{statusName}",
                () => _logger.DPIAPipelineNotApproved(check.RequestTypeName, assessment.Id, statusName),
                check.NextStep);
        }

        // Step 6: Validate assessment is not expired (if review is required)
        if (IsExpired(check.Attribute, assessment, nowUtc))
        {
            return HandleFailure(
                check.Activity, check.StartedAt, check.RequestTypeName,
                DPIAErrors.AssessmentExpired(assessment.Id, check.FullTypeName, assessment.NextReviewAtUtc!.Value),
                "assessment_expired",
                () => _logger.DPIAPipelineExpired(check.RequestTypeName, assessment.Id, assessment.NextReviewAtUtc),
                check.NextStep);
        }

        // Step 7: Assessment is valid — record success and proceed
        RecordPassed(check.Activity, check.StartedAt, check.RequestTypeName);
        _logger.DPIAPipelinePassed(check.RequestTypeName, assessment.Id);
        return check.NextStep();
    }

    private static bool IsExpired(RequiresDPIAAttribute attribute, DPIAReadModel assessment, DateTimeOffset nowUtc)
        => attribute.ReviewRequired && assessment.NextReviewAtUtc is not null && assessment.NextReviewAtUtc <= nowUtc;

    private ValueTask<Either<EncinaError, TResponse>> HandleLookupError(EncinaError error, CheckContext check)
    {
        // Assessment not found — treat as "no assessment exists"
        var isNotFound = error.GetCode().Match(
            Some: code => code == DPIAErrors.AssessmentNotFoundCode,
            None: () => false);

        if (isNotFound)
        {
            return HandleFailure(
                check.Activity, check.StartedAt, check.RequestTypeName,
                DPIAErrors.AssessmentRequired(check.FullTypeName),
                "assessment_required",
                () => _logger.DPIAPipelineNoAssessment(check.RequestTypeName),
                check.NextStep);
        }

        // Store/infrastructure error
        RecordFailed(check.Activity, check.StartedAt, check.RequestTypeName, "store_error");

        if (_options.EnforcementMode == DPIAEnforcementMode.Block)
        {
            _logger.DPIAPipelineBlocked(check.RequestTypeName, error.GetCode().IfNone("encina.unknown"));
            return ValueTask.FromResult<Either<EncinaError, TResponse>>(Left<EncinaError, TResponse>(error));
        }

        _logger.DPIAPipelineWarned(check.RequestTypeName, error.GetCode().IfNone("encina.unknown"));
        return check.NextStep();
    }

    private async ValueTask<Either<EncinaError, TResponse>> HandleUnhandledException(Exception ex, CheckContext check)
    {
        _logger.DPIAPipelineError(check.RequestTypeName, ex.ForLogging());
        RecordFailed(check.Activity, check.StartedAt, check.RequestTypeName, "unhandled_exception");

        if (_options.EnforcementMode == DPIAEnforcementMode.Block)
        {
            return Left<EncinaError, TResponse>(
                DPIAErrors.StoreError("PipelineCheck", ex.Message, ex));
        }

        // Warn mode — exception doesn't block the response
        return await check.NextStep().ConfigureAwait(false);
    }

    private async ValueTask<Either<EncinaError, TResponse>> HandleFailure(
        Activity? activity,
        long startedAt,
        string requestTypeName,
        EncinaError error,
        string failureReason,
        Action logAction,
        RequestHandlerCallback<TResponse> nextStep)
    {
        logAction();

        if (_options.EnforcementMode == DPIAEnforcementMode.Block)
        {
            RecordFailed(activity, startedAt, requestTypeName, failureReason);
            _logger.DPIAPipelineBlocked(requestTypeName, failureReason);
            return Left<EncinaError, TResponse>(error);
        }

        // Warn mode — log but proceed
        DPIADiagnostics.RecordWarned(activity, failureReason);
        RecordMetrics(startedAt, requestTypeName, isPass: false, failureReason: failureReason);
        _logger.DPIAPipelineWarned(requestTypeName, failureReason);
        return await nextStep().ConfigureAwait(false);
    }

    private static void RecordPassed(Activity? activity, long startedAt, string requestTypeName)
    {
        DPIADiagnostics.RecordPassed(activity);
        RecordMetrics(startedAt, requestTypeName, isPass: true, failureReason: null);
    }

    private static void RecordFailed(Activity? activity, long startedAt, string requestTypeName, string failureReason)
    {
        DPIADiagnostics.RecordFailed(activity, failureReason);
        RecordMetrics(startedAt, requestTypeName, isPass: false, failureReason: failureReason);
    }

    private static void RecordMetrics(long startedAt, string requestTypeName, bool isPass, string? failureReason)
    {
        var elapsed = Stopwatch.GetElapsedTime(startedAt);
        var tags = new TagList
        {
            { DPIADiagnostics.TagRequestType, requestTypeName }
        };

        DPIADiagnostics.PipelineCheckTotal.Add(1, tags);

        if (isPass)
        {
            DPIADiagnostics.PipelineCheckPassed.Add(1, tags);
        }
        else
        {
            var failedTags = new TagList
            {
                { DPIADiagnostics.TagRequestType, requestTypeName },
                { DPIADiagnostics.TagFailureReason, failureReason ?? "unknown" }
            };
            DPIADiagnostics.PipelineCheckFailed.Add(1, failedTags);
        }

        DPIADiagnostics.PipelineCheckDuration.Record(elapsed.TotalMilliseconds, tags);
    }
}
