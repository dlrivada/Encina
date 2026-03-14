using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;

using Encina.Compliance.PrivacyByDesign.Diagnostics;
using Encina.Compliance.PrivacyByDesign.Model;
using Encina.Modules.Isolation;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static LanguageExt.Prelude;

namespace Encina.Compliance.PrivacyByDesign;

/// <summary>
/// Pipeline behavior that enforces Privacy by Design requirements declared via
/// <see cref="EnforceDataMinimizationAttribute"/>.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// <para>
/// Per GDPR Article 25(1), the controller must implement "appropriate technical and
/// organisational measures [...] designed to implement data-protection principles,
/// such as data minimisation, in an effective manner." This behavior provides declarative
/// enforcement at the request pipeline level.
/// </para>
/// <para>
/// The behavior validates requests decorated with <see cref="EnforceDataMinimizationAttribute"/>
/// <b>before</b> the handler executes. Depending on the configured
/// <see cref="PrivacyByDesignOptions.PrivacyLevel"/>, the following checks are applied:
/// <list type="bullet">
/// <item><description><see cref="PrivacyLevel.Minimum"/>: Data minimization only.</description></item>
/// <item><description><see cref="PrivacyLevel.Standard"/>: Data minimization + purpose limitation.</description></item>
/// <item><description><see cref="PrivacyLevel.Maximum"/>: Data minimization + purpose limitation + default privacy.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Attribute resolution:</b> Uses a static <see cref="ConcurrentDictionary{TKey, TValue}"/>
/// to cache <see cref="EnforceDataMinimizationAttribute"/> lookups per request type, ensuring zero
/// reflection overhead on subsequent calls.
/// </para>
/// <para>
/// The enforcement mode is controlled by <see cref="PrivacyByDesignOptions.EnforcementMode"/>:
/// <see cref="PrivacyByDesignEnforcementMode.Block"/> returns an error if violations are detected,
/// <see cref="PrivacyByDesignEnforcementMode.Warn"/> logs a warning but allows processing to continue,
/// <see cref="PrivacyByDesignEnforcementMode.Disabled"/> skips all enforcement entirely.
/// </para>
/// <para>
/// <b>Cross-cutting integrations:</b>
/// <list type="bullet">
/// <item><description><b>Multi-tenancy</b>: Propagates <c>TenantId</c> from <see cref="IRequestContext"/>
/// to activity tags, notifications, and structured logs.</description></item>
/// <item><description><b>Module isolation</b>: Resolves <see cref="IModuleExecutionContext"/> (optional)
/// to propagate module context to purpose lookups, activity tags, and notifications.</description></item>
/// <item><description><b>Notifications</b>: Publishes <see cref="DataMinimizationViolationDetected"/>
/// and <see cref="PrivacyDefaultOverridden"/> via <see cref="IEncina"/> (optional, non-blocking).</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Observability:</b> Emits OpenTelemetry traces via <c>Encina.Compliance.PrivacyByDesign</c>
/// ActivitySource, metrics via <c>Encina.Compliance.PrivacyByDesign</c> Meter, and structured
/// log messages via <see cref="ILogger"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [EnforceDataMinimization(Purpose = "Order Processing")]
/// public sealed record CreateOrderCommand(
///     string ProductId,
///     int Quantity,
///     [property: NotStrictlyNecessary(Reason = "Analytics only")]
///     string? ReferralSource) : ICommand&lt;OrderId&gt;;
///
/// // The pipeline behavior automatically:
/// // 1. Checks for [EnforceDataMinimization] attribute (cached)
/// // 2. Resolves module context (if available)
/// // 3. Runs data minimization analysis
/// // 4. Validates purpose limitation (if purpose declared, module-aware)
/// // 5. Checks privacy defaults (if PrivacyLevel.Maximum)
/// // 6. Publishes notifications on violations (non-blocking)
/// // 7. In Block mode, returns error if violations detected
/// // 8. In Warn mode, logs warning but allows through
/// </code>
/// </example>
public sealed class DataMinimizationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly ConcurrentDictionary<Type, EnforceDataMinimizationAttribute?> AttributeCache = new();
    private static readonly ConcurrentDictionary<Type, bool> ProcessesPersonalDataCache = new();

    private readonly IPrivacyByDesignValidator _validator;
    private readonly PrivacyByDesignOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DataMinimizationPipelineBehavior<TRequest, TResponse>> _logger;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="DataMinimizationPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="validator">The Privacy by Design validator for request validation.</param>
    /// <param name="options">Configuration options controlling enforcement mode and thresholds.</param>
    /// <param name="timeProvider">Time provider for UTC time comparisons.</param>
    /// <param name="logger">Logger for structured Privacy by Design compliance logging.</param>
    /// <param name="serviceProvider">Service provider for resolving optional cross-cutting dependencies.</param>
    public DataMinimizationPipelineBehavior(
        IPrivacyByDesignValidator validator,
        IOptions<PrivacyByDesignOptions> options,
        TimeProvider timeProvider,
        ILogger<DataMinimizationPipelineBehavior<TRequest, TResponse>> logger,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _validator = validator;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
        _serviceProvider = serviceProvider;
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
        if (_options.EnforcementMode == PrivacyByDesignEnforcementMode.Disabled)
        {
            _logger.PbDPipelineDisabled(requestTypeName);
            return await nextStep().ConfigureAwait(false);
        }

        // Step 2: Check for [EnforceDataMinimization] attribute (cached)
        var attribute = AttributeCache.GetOrAdd(requestType, static type =>
            type.GetCustomAttribute<EnforceDataMinimizationAttribute>());

        // No attribute — skip entirely
        if (attribute is null)
        {
            _logger.PbDPipelineNoAttribute(requestTypeName);
            PrivacyByDesignDiagnostics.PipelineCheckSkipped.Add(1, new TagList
            {
                { PrivacyByDesignDiagnostics.TagRequestType, requestTypeName }
            });
            return await nextStep().ConfigureAwait(false);
        }

        // Step 3: Resolve optional cross-cutting contexts
        var moduleContext = _serviceProvider.GetService<IModuleExecutionContext>();
        var moduleId = moduleContext?.CurrentModule;
        var tenantId = context.TenantId;

        // Step 3b: Check for [ProcessesPersonalData] from Encina.Compliance.GDPR (optional, by name)
        var processesPersonalData = ProcessesPersonalDataCache.GetOrAdd(requestType, static type =>
            type.GetCustomAttributes(inherit: true)
                .Any(static a => a.GetType().Name == "ProcessesPersonalDataAttribute"));

        // Step 4: Start tracing and timing
        var startedAt = Stopwatch.GetTimestamp();
        using var activity = PrivacyByDesignDiagnostics.StartPipelineCheck(requestTypeName);
        activity?.SetTag(PrivacyByDesignDiagnostics.TagEnforcementMode, _options.EnforcementMode.ToString());

        if (processesPersonalData)
        {
            activity?.SetTag("encina.processes_personal_data", true);
        }

        // Propagate tenant and module context to traces for cross-cutting observability
        if (tenantId is not null)
        {
            activity?.SetTag("encina.tenant_id", tenantId);
        }

        if (moduleId is not null)
        {
            activity?.SetTag("encina.module_id", moduleId);
        }

        _logger.PbDPipelineStarted(requestTypeName, _options.EnforcementMode.ToString());

        var fullTypeName = requestType.FullName ?? requestTypeName;

        try
        {
            // Step 5: Run validation via the orchestrator (module-aware)
            var validationResult = await _validator
                .ValidateAsync(request, moduleId, cancellationToken)
                .ConfigureAwait(false);

            // Handle validator infrastructure errors
            if (validationResult.IsLeft)
            {
                var validatorError = (EncinaError)validationResult;
                RecordFailed(activity, startedAt, requestTypeName, "validator_error");

                if (_options.EnforcementMode == PrivacyByDesignEnforcementMode.Block)
                {
                    _logger.PbDPipelineBlocked(requestTypeName, validatorError.Message);
                    return Left<EncinaError, TResponse>(validatorError);
                }

                _logger.PbDPipelineWarned(requestTypeName, validatorError.Message);
                return await nextStep().ConfigureAwait(false);
            }

            var result = (PrivacyValidationResult)validationResult;

            // Step 6: Check minimization score threshold
            if (result.MinimizationReport is not null
                && _options.MinimizationScoreThreshold > 0.0
                && result.MinimizationReport.MinimizationScore < _options.MinimizationScoreThreshold)
            {
                var score = result.MinimizationReport.MinimizationScore;
                _logger.PbDPipelineScoreBelowThreshold(requestTypeName, score, _options.MinimizationScoreThreshold);

                var scoreError = PrivacyByDesignErrors.MinimizationScoreBelowThreshold(
                    fullTypeName, score, _options.MinimizationScoreThreshold);

                return await HandleFailure(
                    activity, startedAt, requestTypeName, fullTypeName,
                    scoreError,
                    "minimization_score_below_threshold",
                    result, tenantId, moduleId, processesPersonalData,
                    nextStep, cancellationToken).ConfigureAwait(false);
            }

            // Step 7: Check for violations
            if (!result.IsCompliant)
            {
                var violationCount = result.Violations.Count;
                var minimizationScore = result.MinimizationReport?.MinimizationScore ?? 1.0;

                _logger.PbDPipelineViolations(requestTypeName, violationCount, minimizationScore);

                // Determine the primary violation type for the error
                var error = DetermineViolationError(result, fullTypeName);

                return await HandleFailure(
                    activity, startedAt, requestTypeName, fullTypeName,
                    error,
                    "privacy_violations_detected",
                    result, tenantId, moduleId, processesPersonalData,
                    nextStep, cancellationToken).ConfigureAwait(false);
            }

            // Step 8: All checks passed — record success and proceed
            var passScore = result.MinimizationReport?.MinimizationScore ?? 1.0;
            RecordPassed(activity, startedAt, requestTypeName);
            _logger.PbDPipelinePassed(requestTypeName, passScore);
            return await nextStep().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.PbDPipelineError(requestTypeName, ex);
            RecordFailed(activity, startedAt, requestTypeName, "unhandled_exception");

            if (_options.EnforcementMode == PrivacyByDesignEnforcementMode.Block)
            {
                return Left<EncinaError, TResponse>(
                    PrivacyByDesignErrors.StoreError("PipelineCheck", ex.Message, ex));
            }

            // Warn mode — exception doesn't block the response
            return await nextStep().ConfigureAwait(false);
        }
    }

    // ================================================================
    // Private helpers
    // ================================================================

    private async ValueTask<Either<EncinaError, TResponse>> HandleFailure(
        Activity? activity,
        long startedAt,
        string requestTypeName,
        string fullTypeName,
        EncinaError error,
        string failureReason,
        PrivacyValidationResult result,
        string? tenantId,
        string? moduleId,
        bool processesPersonalData,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        // Publish violation notification (non-blocking)
        await PublishViolationNotificationAsync(
            fullTypeName, result, tenantId, moduleId, processesPersonalData, cancellationToken).ConfigureAwait(false);

        if (_options.EnforcementMode == PrivacyByDesignEnforcementMode.Block)
        {
            RecordFailed(activity, startedAt, requestTypeName, failureReason);
            _logger.PbDPipelineBlocked(requestTypeName, failureReason);
            return Left<EncinaError, TResponse>(error);
        }

        // Warn mode — log but proceed
        PrivacyByDesignDiagnostics.RecordWarned(activity, failureReason);
        RecordMetrics(startedAt, requestTypeName, isPass: false, failureReason: failureReason);
        _logger.PbDPipelineWarned(requestTypeName, failureReason);
        return await nextStep().ConfigureAwait(false);
    }

    private async ValueTask PublishViolationNotificationAsync(
        string fullTypeName,
        PrivacyValidationResult result,
        string? tenantId,
        string? moduleId,
        bool processesPersonalData,
        CancellationToken cancellationToken)
    {
        try
        {
            var encina = _serviceProvider.GetService<IEncina>();
            if (encina is null)
            {
                return;
            }

            // When [ProcessesPersonalData] is also present, enrich violations with
            // processing activity context for cross-module GDPR compliance correlation.
            var enrichedViolations = processesPersonalData
                ? EnrichViolationsWithProcessingContext(result.Violations)
                : result.Violations;

            // Publish DataMinimizationViolationDetected if there are violations
            if (enrichedViolations.Count > 0)
            {
                var notification = new DataMinimizationViolationDetected(
                    RequestTypeName: fullTypeName,
                    Violations: enrichedViolations,
                    EnforcementMode: _options.EnforcementMode,
                    MinimizationScore: result.MinimizationReport?.MinimizationScore ?? 0.0,
                    TenantId: tenantId,
                    ModuleId: moduleId);

                await encina.Publish(notification, cancellationToken).ConfigureAwait(false);
                PrivacyByDesignDiagnostics.NotificationsPublishedTotal.Add(1);
                _logger.PbDNotificationPublished(fullTypeName, enrichedViolations.Count);
            }

            // Publish PrivacyDefaultOverridden if default privacy fields are overridden
            if (result.MinimizationReport is not null)
            {
                var overriddenFields = new List<DefaultPrivacyFieldInfo>();

                // Check for overridden defaults from the validation result
                // The validator already performed InspectDefaultsAsync in its ValidateAsync flow
                // and violations with PrivacyViolationType.DefaultPrivacy indicate overrides
                foreach (var violation in result.Violations)
                {
                    if (violation.ViolationType == PrivacyViolationType.DefaultPrivacy)
                    {
                        overriddenFields.Add(new DefaultPrivacyFieldInfo(
                            FieldName: violation.FieldName,
                            DeclaredDefault: null,
                            ActualValue: null,
                            MatchesDefault: false));
                    }
                }

                if (overriddenFields.Count > 0)
                {
                    var defaultNotification = new PrivacyDefaultOverridden(
                        RequestTypeName: fullTypeName,
                        OverriddenFields: overriddenFields,
                        TenantId: tenantId,
                        ModuleId: moduleId);

                    await encina.Publish(defaultNotification, cancellationToken).ConfigureAwait(false);
                    PrivacyByDesignDiagnostics.NotificationsPublishedTotal.Add(1);
                }
            }
        }
        catch (Exception ex)
        {
            // Notification failures are non-blocking
            PrivacyByDesignDiagnostics.NotificationsFailedTotal.Add(1);
            _logger.PbDNotificationFailed(fullTypeName, ex);
        }
    }

    /// <summary>
    /// Enriches violation messages with processing activity context when
    /// <c>[ProcessesPersonalData]</c> is also present on the request type.
    /// This provides cross-module GDPR compliance correlation (Article 30).
    /// </summary>
    private static List<PrivacyViolation> EnrichViolationsWithProcessingContext(
        IReadOnlyList<PrivacyViolation> violations)
    {
        var enriched = new List<PrivacyViolation>(violations.Count);

        foreach (var violation in violations)
        {
            enriched.Add(violation with
            {
                Message = $"[ProcessesPersonalData] {violation.Message}"
            });
        }

        return enriched;
    }

    private static EncinaError DetermineViolationError(PrivacyValidationResult result, string fullTypeName)
    {
        // Count violations by type for structured error reporting
        var dataMinViolations = 0;
        var purposeViolations = new List<string>();
        var defaultViolations = 0;

        foreach (var violation in result.Violations)
        {
            switch (violation.ViolationType)
            {
                case PrivacyViolationType.DataMinimization:
                    dataMinViolations++;
                    break;
                case PrivacyViolationType.PurposeLimitation:
                    purposeViolations.Add(violation.FieldName);
                    break;
                case PrivacyViolationType.DefaultPrivacy:
                    defaultViolations++;
                    break;
            }
        }

        // Return the most relevant error based on violation types present
        if (purposeViolations.Count > 0)
        {
            var declaredPurpose = result.PurposeValidation?.DeclaredPurpose ?? "unknown";
            return PrivacyByDesignErrors.PurposeLimitationViolation(
                fullTypeName, declaredPurpose, purposeViolations);
        }

        if (dataMinViolations > 0)
        {
            return PrivacyByDesignErrors.DataMinimizationViolation(
                fullTypeName,
                dataMinViolations,
                result.MinimizationReport?.MinimizationScore ?? 0.0);
        }

        return PrivacyByDesignErrors.DefaultPrivacyViolation(fullTypeName, defaultViolations);
    }

    private static void RecordPassed(Activity? activity, long startedAt, string requestTypeName)
    {
        PrivacyByDesignDiagnostics.RecordPassed(activity);
        RecordMetrics(startedAt, requestTypeName, isPass: true, failureReason: null);
    }

    private static void RecordFailed(Activity? activity, long startedAt, string requestTypeName, string failureReason)
    {
        PrivacyByDesignDiagnostics.RecordFailed(activity, failureReason);
        RecordMetrics(startedAt, requestTypeName, isPass: false, failureReason: failureReason);
    }

    private static void RecordMetrics(long startedAt, string requestTypeName, bool isPass, string? failureReason)
    {
        var elapsed = Stopwatch.GetElapsedTime(startedAt);
        var tags = new TagList
        {
            { PrivacyByDesignDiagnostics.TagRequestType, requestTypeName }
        };

        PrivacyByDesignDiagnostics.PipelineCheckTotal.Add(1, tags);

        if (isPass)
        {
            PrivacyByDesignDiagnostics.PipelineCheckPassed.Add(1, tags);
        }
        else
        {
            var failedTags = new TagList
            {
                { PrivacyByDesignDiagnostics.TagRequestType, requestTypeName },
                { PrivacyByDesignDiagnostics.TagFailureReason, failureReason ?? "unknown" }
            };
            PrivacyByDesignDiagnostics.PipelineCheckFailed.Add(1, failedTags);
        }

        PrivacyByDesignDiagnostics.PipelineCheckDuration.Record(elapsed.TotalMilliseconds, tags);
    }
}
