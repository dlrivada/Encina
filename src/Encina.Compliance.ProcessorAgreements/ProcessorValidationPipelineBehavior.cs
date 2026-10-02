using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;

using Encina.Compliance.ProcessorAgreements.Abstractions;
using Encina.Compliance.ProcessorAgreements.Diagnostics;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Diagnostics;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static LanguageExt.Prelude;

namespace Encina.Compliance.ProcessorAgreements;

/// <summary>
/// Pipeline behavior that enforces processor DPA requirements declared via <see cref="RequiresProcessorAttribute"/>.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// <para>
/// Per GDPR Article 28(3), "processing by a processor shall be governed by a contract or other
/// legal act [...] that is binding on the processor." This behavior provides declarative
/// enforcement at the request pipeline level.
/// </para>
/// <para>
/// The behavior checks that a current, active, and fully compliant Data Processing Agreement
/// exists for the referenced processor <b>before</b> the handler executes.
/// </para>
/// <para>
/// <b>Two-level validation:</b>
/// <list type="number">
/// <item><description><see cref="IDPAService.HasValidDPAAsync"/>: Fast boolean check for the
/// pass-through hot path. If valid, the request proceeds without further allocation.</description></item>
/// <item><description><see cref="IDPAService.ValidateDPAAsync"/>: Detailed validation only when
/// blocking, to include <see cref="DPAValidationResult"/> details (missing terms, expiration info)
/// in the error response.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Attribute resolution:</b> Uses a static <see cref="ConcurrentDictionary{TKey, TValue}"/>
/// to cache <see cref="RequiresProcessorAttribute"/> lookups per request type, ensuring zero
/// reflection overhead on subsequent calls.
/// </para>
/// <para>
/// The enforcement mode is controlled by <see cref="ProcessorAgreementOptions.EnforcementMode"/>:
/// <see cref="ProcessorAgreementEnforcementMode.Block"/> returns an error if no valid DPA exists,
/// <see cref="ProcessorAgreementEnforcementMode.Warn"/> logs a warning but allows processing,
/// <see cref="ProcessorAgreementEnforcementMode.Disabled"/> skips all DPA enforcement entirely.
/// </para>
/// <para>
/// <b>Observability:</b> Emits OpenTelemetry traces via <c>Encina.Compliance.ProcessorAgreements</c>
/// ActivitySource, metrics via <c>Encina.Compliance.ProcessorAgreements</c> Meter, and structured
/// log messages via <see cref="ILogger"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [RequiresProcessor(ProcessorId = "stripe-payments")]
/// public sealed record ProcessPaymentCommand(decimal Amount) : ICommand&lt;PaymentResult&gt;;
///
/// // The pipeline behavior automatically:
/// // 1. Checks for [RequiresProcessor] attribute (cached)
/// // 2. Calls IDPAValidator.HasValidDPAAsync for fast pass-through
/// // 3. If invalid + Block mode, calls ValidateAsync for detailed error
/// // 4. In Warn mode, logs warning but allows through
/// </code>
/// </example>
public sealed class ProcessorValidationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly ConcurrentDictionary<Type, RequiresProcessorAttribute?> AttributeCache = new();

    private readonly IDPAService _dpaService;
    private readonly ProcessorAgreementOptions _options;
    private readonly ILogger<ProcessorValidationPipelineBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessorValidationPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="dpaService">The DPA service for processor agreement validation.</param>
    /// <param name="options">Processor agreement configuration options controlling enforcement mode.</param>
    /// <param name="logger">Logger for structured processor agreement compliance logging.</param>
    public ProcessorValidationPipelineBehavior(
        IDPAService dpaService,
        IOptions<ProcessorAgreementOptions> options,
        ILogger<ProcessorValidationPipelineBehavior<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(dpaService);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _dpaService = dpaService;
        _options = options.Value;
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

        return await DispatchAsync(context, nextStep, cancellationToken).ConfigureAwait(false);
    }

    // ================================================================
    // Private helpers
    // ================================================================

    private ValueTask<Either<EncinaError, TResponse>> DispatchAsync(
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        var requestType = typeof(TRequest);
        var requestTypeName = requestType.Name;

        // Step 1: Check enforcement mode — if disabled, skip entirely
        if (_options.EnforcementMode == ProcessorAgreementEnforcementMode.Disabled)
        {
            _logger.ProcessorPipelineDisabled(requestTypeName);
            return nextStep();
        }

        // Step 2: Check for [RequiresProcessor] attribute (cached)
        var attribute = AttributeCache.GetOrAdd(requestType, static type =>
            type.GetCustomAttribute<RequiresProcessorAttribute>());

        // No attribute — skip entirely
        if (attribute is null)
        {
            _logger.ProcessorPipelineNoAttribute(requestTypeName);
            ProcessorAgreementDiagnostics.PipelineCheckSkipped.Add(1, new TagList
            {
                { ProcessorAgreementDiagnostics.TagRequestType, requestTypeName }
            });
            return nextStep();
        }

        var processorIdStr = attribute.ProcessorId;

        // Parse processor ID as Guid (Marten aggregates use Guid identifiers)
        if (!Guid.TryParse(processorIdStr, out var processorId))
        {
            _logger.ProcessorPipelineNoAttribute(requestTypeName);
            return ValueTask.FromResult(Left<EncinaError, TResponse>(
                ProcessorAgreementErrors.ValidationFailed(processorIdStr, $"ProcessorId '{processorIdStr}' is not a valid GUID.")));
        }

        return RunTracedCheckAsync(context, requestTypeName, processorIdStr, processorId, nextStep, cancellationToken);
    }

    private async ValueTask<Either<EncinaError, TResponse>> RunTracedCheckAsync(
        IRequestContext context,
        string requestTypeName,
        string processorIdStr,
        Guid processorId,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        // Step 3: Start tracing and logging
        var startedAt = Stopwatch.GetTimestamp();
        using var activity = ProcessorAgreementDiagnostics.StartPipelineCheck(requestTypeName);
        TagActivity(activity, context, processorIdStr);

        _logger.ProcessorPipelineStarted(requestTypeName, processorIdStr, _options.EnforcementMode.ToString());

        var check = new CheckContext(activity, startedAt, requestTypeName, processorIdStr, processorId, nextStep);

        try
        {
            return await RunCheckAsync(check, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return await HandleUnhandledException(ex, check).ConfigureAwait(false);
        }
    }

    private void TagActivity(Activity? activity, IRequestContext context, string processorIdStr)
    {
        activity?.SetTag(ProcessorAgreementDiagnostics.TagProcessorId, processorIdStr);
        activity?.SetTag(ProcessorAgreementDiagnostics.TagEnforcementMode, _options.EnforcementMode.ToString());

        // Propagate tenant context to traces for cross-cutting observability
        if (context.TenantId is not null)
        {
            activity?.SetTag("encina.tenant_id", context.TenantId);
        }
    }

    private readonly record struct CheckContext(
        Activity? Activity,
        long StartedAt,
        string RequestTypeName,
        string ProcessorIdStr,
        Guid ProcessorId,
        RequestHandlerCallback<TResponse> NextStep);

    private async ValueTask<Either<EncinaError, TResponse>> RunCheckAsync(
        CheckContext check,
        CancellationToken cancellationToken)
    {
        // Step 4: Fast path — lightweight boolean check via HasValidDPAAsync
        var hasValidResult = await _dpaService
            .HasValidDPAAsync(check.ProcessorId, cancellationToken)
            .ConfigureAwait(false);

        // Handle service infrastructure errors
        if (hasValidResult.IsLeft)
        {
            return await HandleValidatorError((EncinaError)hasValidResult, check).ConfigureAwait(false);
        }

        var hasValidDPA = (bool)hasValidResult;

        // Step 5: Fast pass-through — DPA is valid
        if (hasValidDPA)
        {
            RecordPassed(check.Activity, check.StartedAt, check.RequestTypeName);
            _logger.ProcessorPipelinePassed(check.RequestTypeName, check.ProcessorIdStr);
            return await check.NextStep().ConfigureAwait(false);
        }

        // Step 6: DPA is NOT valid — behavior depends on enforcement mode
        return await HandleNoValidDPA(check, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<Either<EncinaError, TResponse>> HandleValidatorError(
        EncinaError serviceError,
        CheckContext check)
    {
        RecordFailed(check.Activity, check.StartedAt, check.RequestTypeName, "validator_error");

        if (_options.EnforcementMode == ProcessorAgreementEnforcementMode.Block)
        {
            _logger.ProcessorPipelineBlocked(check.RequestTypeName, check.ProcessorIdStr, serviceError.GetCode().IfNone("encina.unknown"));
            return Left<EncinaError, TResponse>(serviceError);
        }

        _logger.ProcessorPipelineWarned(check.RequestTypeName, check.ProcessorIdStr, serviceError.GetCode().IfNone("encina.unknown"));
        return await check.NextStep().ConfigureAwait(false);
    }

    private async ValueTask<Either<EncinaError, TResponse>> HandleNoValidDPA(
        CheckContext check,
        CancellationToken cancellationToken)
    {
        _logger.ProcessorPipelineNoValidDPA(check.RequestTypeName, check.ProcessorIdStr);

        if (_options.EnforcementMode == ProcessorAgreementEnforcementMode.Block)
        {
            // Detailed validation to build a rich error with DPAValidationResult details
            var detailedResult = await _dpaService
                .ValidateDPAAsync(check.ProcessorId, cancellationToken)
                .ConfigureAwait(false);

            var error = BuildBlockError(check.ProcessorIdStr, detailedResult);
            var failureReason = BuildFailureReason(detailedResult);

            RecordFailed(check.Activity, check.StartedAt, check.RequestTypeName, failureReason);
            _logger.ProcessorPipelineBlocked(check.RequestTypeName, check.ProcessorIdStr, failureReason);

            return Left<EncinaError, TResponse>(error);
        }

        // Warn mode — log and proceed
        ProcessorAgreementDiagnostics.RecordWarned(check.Activity, "no_valid_dpa");
        RecordMetrics(check.StartedAt, check.RequestTypeName, isPass: false, failureReason: "no_valid_dpa");
        _logger.ProcessorPipelineWarned(check.RequestTypeName, check.ProcessorIdStr, "no_valid_dpa");
        return await check.NextStep().ConfigureAwait(false);
    }

    private async ValueTask<Either<EncinaError, TResponse>> HandleUnhandledException(Exception ex, CheckContext check)
    {
        _logger.ProcessorPipelineError(check.RequestTypeName, check.ProcessorIdStr, ex.ForLogging());
        RecordFailed(check.Activity, check.StartedAt, check.RequestTypeName, "unhandled_exception");

        if (_options.EnforcementMode == ProcessorAgreementEnforcementMode.Block)
        {
            return Left<EncinaError, TResponse>(
                ProcessorAgreementErrors.StoreError("PipelineCheck", ex.Message, ex));
        }

        // Warn mode — exception doesn't block the response
        return await check.NextStep().ConfigureAwait(false);
    }

    private static EncinaError BuildBlockError(
        string processorId,
        Either<EncinaError, DPAValidationResult> detailedResult)
    {
        // If the detailed validation itself failed, return that infrastructure error
        if (detailedResult.IsLeft)
        {
            return (EncinaError)detailedResult;
        }

        var validation = (DPAValidationResult)detailedResult;

        // No DPA at all
        if (validation.DPAId is null)
        {
            return ProcessorAgreementErrors.DPAMissing(processorId);
        }

        // DPA has missing mandatory terms
        if (validation.MissingTerms.Count > 0)
        {
            return ProcessorAgreementErrors.DPAIncomplete(processorId, validation.DPAId, validation.MissingTerms);
        }

        // DPA expired (DaysUntilExpiration is negative or zero)
        if (validation.DaysUntilExpiration is not null && validation.DaysUntilExpiration <= 0)
        {
            return ProcessorAgreementErrors.DPAExpired(
                processorId,
                validation.DPAId,
                validation.ValidatedAtUtc);
        }

        // Fallback — generic validation failure
        var warnings = validation.Warnings.Count > 0
            ? string.Join("; ", validation.Warnings)
            : "DPA validation failed";
        return ProcessorAgreementErrors.ValidationFailed(processorId, warnings);
    }

    private static string BuildFailureReason(Either<EncinaError, DPAValidationResult> detailedResult)
    {
        if (detailedResult.IsLeft)
        {
            return "validator_error";
        }

        var validation = (DPAValidationResult)detailedResult;

        if (validation.DPAId is null)
        {
            return "dpa_missing";
        }

        if (validation.MissingTerms.Count > 0)
        {
            return "dpa_incomplete";
        }

        if (validation.DaysUntilExpiration is not null && validation.DaysUntilExpiration <= 0)
        {
            return "dpa_expired";
        }

        return "dpa_invalid";
    }

    private static void RecordPassed(Activity? activity, long startedAt, string requestTypeName)
    {
        ProcessorAgreementDiagnostics.RecordPassed(activity);
        RecordMetrics(startedAt, requestTypeName, isPass: true, failureReason: null);
    }

    private static void RecordFailed(Activity? activity, long startedAt, string requestTypeName, string failureReason)
    {
        ProcessorAgreementDiagnostics.RecordFailed(activity, failureReason);
        RecordMetrics(startedAt, requestTypeName, isPass: false, failureReason: failureReason);
    }

    private static void RecordMetrics(long startedAt, string requestTypeName, bool isPass, string? failureReason)
    {
        var elapsed = Stopwatch.GetElapsedTime(startedAt);
        var tags = new TagList
        {
            { ProcessorAgreementDiagnostics.TagRequestType, requestTypeName }
        };

        ProcessorAgreementDiagnostics.PipelineCheckTotal.Add(1, tags);

        if (isPass)
        {
            ProcessorAgreementDiagnostics.PipelineCheckPassed.Add(1, tags);
        }
        else
        {
            var failedTags = new TagList
            {
                { ProcessorAgreementDiagnostics.TagRequestType, requestTypeName },
                { ProcessorAgreementDiagnostics.TagFailureReason, failureReason ?? "unknown" }
            };
            ProcessorAgreementDiagnostics.PipelineCheckFailed.Add(1, failedTags);
        }

        ProcessorAgreementDiagnostics.PipelineCheckDuration.Record(elapsed.TotalMilliseconds, tags);
    }
}
