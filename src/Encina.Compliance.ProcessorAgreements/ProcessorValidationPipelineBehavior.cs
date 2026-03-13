using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;

using Encina.Compliance.ProcessorAgreements.Diagnostics;
using Encina.Compliance.ProcessorAgreements.Model;

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
/// <item><description><see cref="IDPAValidator.HasValidDPAAsync"/>: Fast boolean check for the
/// pass-through hot path. If valid, the request proceeds without further allocation.</description></item>
/// <item><description><see cref="IDPAValidator.ValidateAsync"/>: Detailed validation only when
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

    private readonly IDPAValidator _validator;
    private readonly IProcessorAuditStore _auditStore;
    private readonly ProcessorAgreementOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ProcessorValidationPipelineBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessorValidationPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="validator">The DPA validator for processor agreement lookups.</param>
    /// <param name="auditStore">The audit store for recording enforcement actions.</param>
    /// <param name="options">Processor agreement configuration options controlling enforcement mode.</param>
    /// <param name="timeProvider">Time provider for audit entry timestamps.</param>
    /// <param name="logger">Logger for structured processor agreement compliance logging.</param>
    public ProcessorValidationPipelineBehavior(
        IDPAValidator validator,
        IProcessorAuditStore auditStore,
        IOptions<ProcessorAgreementOptions> options,
        TimeProvider timeProvider,
        ILogger<ProcessorValidationPipelineBehavior<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(auditStore);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _validator = validator;
        _auditStore = auditStore;
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
        if (_options.EnforcementMode == ProcessorAgreementEnforcementMode.Disabled)
        {
            _logger.ProcessorPipelineDisabled(requestTypeName);
            return await nextStep().ConfigureAwait(false);
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
            return await nextStep().ConfigureAwait(false);
        }

        var processorId = attribute.ProcessorId;

        // Step 3: Start tracing and logging
        var startedAt = Stopwatch.GetTimestamp();
        using var activity = ProcessorAgreementDiagnostics.StartPipelineCheck(requestTypeName);
        activity?.SetTag(ProcessorAgreementDiagnostics.TagProcessorId, processorId);
        activity?.SetTag(ProcessorAgreementDiagnostics.TagEnforcementMode, _options.EnforcementMode.ToString());

        // Propagate tenant context to traces for cross-cutting observability
        if (context.TenantId is not null)
        {
            activity?.SetTag("encina.tenant_id", context.TenantId);
        }

        _logger.ProcessorPipelineStarted(requestTypeName, processorId, _options.EnforcementMode.ToString());

        try
        {
            // Step 4: Fast path — lightweight boolean check via HasValidDPAAsync
            var hasValidResult = await _validator
                .HasValidDPAAsync(processorId, cancellationToken)
                .ConfigureAwait(false);

            // Handle validator infrastructure errors
            if (hasValidResult.IsLeft)
            {
                var validatorError = (EncinaError)hasValidResult;
                RecordFailed(activity, startedAt, requestTypeName, "validator_error");

                if (_options.EnforcementMode == ProcessorAgreementEnforcementMode.Block)
                {
                    _logger.ProcessorPipelineBlocked(requestTypeName, processorId, validatorError.Message);
                    return Left<EncinaError, TResponse>(validatorError);
                }

                _logger.ProcessorPipelineWarned(requestTypeName, processorId, validatorError.Message);
                return await nextStep().ConfigureAwait(false);
            }

            var hasValidDPA = (bool)hasValidResult;

            // Step 5: Fast pass-through — DPA is valid
            if (hasValidDPA)
            {
                RecordPassed(activity, startedAt, requestTypeName);
                _logger.ProcessorPipelinePassed(requestTypeName, processorId);
                return await nextStep().ConfigureAwait(false);
            }

            // Step 6: DPA is NOT valid — behavior depends on enforcement mode
            _logger.ProcessorPipelineNoValidDPA(requestTypeName, processorId);

            if (_options.EnforcementMode == ProcessorAgreementEnforcementMode.Block)
            {
                // Detailed validation to build a rich error with DPAValidationResult details
                var detailedResult = await _validator
                    .ValidateAsync(processorId, cancellationToken)
                    .ConfigureAwait(false);

                var error = BuildBlockError(processorId, detailedResult);
                var failureReason = BuildFailureReason(detailedResult);

                RecordFailed(activity, startedAt, requestTypeName, failureReason);
                _logger.ProcessorPipelineBlocked(requestTypeName, processorId, failureReason);

                // Record audit entry for enforcement action (non-blocking)
                await RecordEnforcementAuditAsync(
                    processorId, requestTypeName, failureReason, context.TenantId, cancellationToken)
                    .ConfigureAwait(false);

                return Left<EncinaError, TResponse>(error);
            }

            // Warn mode — log and proceed
            ProcessorAgreementDiagnostics.RecordWarned(activity, "no_valid_dpa");
            RecordMetrics(startedAt, requestTypeName, isPass: false, failureReason: "no_valid_dpa");
            _logger.ProcessorPipelineWarned(requestTypeName, processorId, "no_valid_dpa");
            return await nextStep().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.ProcessorPipelineError(requestTypeName, processorId, ex);
            RecordFailed(activity, startedAt, requestTypeName, "unhandled_exception");

            if (_options.EnforcementMode == ProcessorAgreementEnforcementMode.Block)
            {
                return Left<EncinaError, TResponse>(
                    ProcessorAgreementErrors.StoreError("PipelineCheck", ex.Message, ex));
            }

            // Warn mode — exception doesn't block the response
            return await nextStep().ConfigureAwait(false);
        }
    }

    // ================================================================
    // Private helpers
    // ================================================================

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

    private async ValueTask RecordEnforcementAuditAsync(
        string processorId,
        string requestTypeName,
        string failureReason,
        string? tenantId,
        CancellationToken cancellationToken)
    {
        if (!_options.TrackAuditTrail)
        {
            return;
        }

        var auditEntry = new ProcessorAgreementAuditEntry
        {
            Id = Guid.NewGuid().ToString(),
            ProcessorId = processorId,
            Action = "Blocked",
            Detail = $"Request '{requestTypeName}' blocked by pipeline enforcement. Reason: {failureReason}.",
            PerformedByUserId = "System",
            OccurredAtUtc = _timeProvider.GetUtcNow(),
            TenantId = tenantId
        };

        try
        {
            await _auditStore.RecordAsync(auditEntry, cancellationToken).ConfigureAwait(false);
            _logger.AuditEntryRecorded(processorId, "Blocked", "System");
        }
        catch (Exception ex)
        {
            _logger.AuditEntryFailed(processorId, "Blocked", ex);
            // Non-blocking: audit failure does not prevent the enforcement action
        }
    }
}
