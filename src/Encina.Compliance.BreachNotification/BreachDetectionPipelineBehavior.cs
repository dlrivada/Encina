using System.Reflection;

using Encina.Compliance.BreachNotification.Abstractions;
using Encina.Compliance.BreachNotification.Detection;
using Encina.Compliance.BreachNotification.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static LanguageExt.Prelude;

namespace Encina.Compliance.BreachNotification;

/// <summary>
/// Pipeline behavior that evaluates requests decorated with <see cref="BreachMonitoredAttribute"/>
/// for potential breach indicators by generating <see cref="SecurityEvent"/> instances and
/// submitting them to the registered <see cref="IBreachDetectionRule"/> implementations.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// <para>
/// Per GDPR Article 33(1), the controller must become "aware" of a breach to trigger
/// the 72-hour notification obligation. This behavior establishes systematic awareness
/// by instrumenting request handlers and evaluating their execution context against
/// registered detection rules.
/// </para>
/// <para>
/// <b>Attribute resolution:</b> Each closed generic type resolves its attribute info exactly once
/// via a <c>static readonly</c> field. This ensures zero reflection overhead on subsequent calls
/// for the same <typeparamref name="TRequest"/>/<typeparamref name="TResponse"/> pair.
/// </para>
/// <para>
/// The behavior executes <c>nextStep()</c> first, then evaluates the result for breach indicators.
/// This ensures that the request handler processes normally before any detection occurs.
/// </para>
/// <para>
/// The enforcement mode is controlled by <see cref="BreachNotificationOptions.EnforcementMode"/>:
/// <see cref="BreachDetectionEnforcementMode.Block"/> returns an error if a breach is detected,
/// <see cref="BreachDetectionEnforcementMode.Warn"/> logs a warning but returns the response,
/// <see cref="BreachDetectionEnforcementMode.Disabled"/> skips all breach detection entirely.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Mark a command for breach monitoring
/// [BreachMonitored(EventType = SecurityEventType.DataExfiltration)]
/// public sealed record ExportCustomerDataCommand(string CustomerId) : ICommand&lt;ExportResult&gt;;
///
/// // The pipeline behavior automatically:
/// // 1. Executes the command handler
/// // 2. Generates a SecurityEvent from request metadata
/// // 3. Evaluates all registered detection rules
/// // 4. In Block mode, returns an error if breaches detected
/// // 5. In Warn mode, logs and allows the response through
/// </code>
/// </example>
public sealed class BreachDetectionPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Static per-generic-type attribute info. Each closed generic type (e.g.,
    /// <c>BreachDetectionPipelineBehavior&lt;ExportDataCommand, ExportResult&gt;</c>)
    /// resolves its own attribute info exactly once via the CLR's static field guarantee.
    /// </summary>
    private static readonly BreachMonitoredInfo? CachedAttributeInfo = ResolveAttributeInfo();

    private readonly IBreachDetector _detector;
    private readonly IBreachNotificationService _breachService;
    private readonly BreachNotificationOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<BreachDetectionPipelineBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BreachDetectionPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="detector">Breach detection engine for evaluating security events.</param>
    /// <param name="breachService">Breach notification service for recording detected breaches.</param>
    /// <param name="options">Breach notification options controlling enforcement mode.</param>
    /// <param name="timeProvider">Time provider for deterministic timestamps.</param>
    /// <param name="logger">Logger for structured diagnostic messages.</param>
    public BreachDetectionPipelineBehavior(
        IBreachDetector detector,
        IBreachNotificationService breachService,
        IOptions<BreachNotificationOptions> options,
        TimeProvider timeProvider,
        ILogger<BreachDetectionPipelineBehavior<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(detector);
        ArgumentNullException.ThrowIfNull(breachService);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _detector = detector;
        _breachService = breachService;
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

        var requestTypeName = typeof(TRequest).Name;

        // Step 1: Disabled mode — no-op, no logging, no metrics
        if (_options.EnforcementMode == BreachDetectionEnforcementMode.Disabled)
        {
            _logger.LogDebug(
                "Breach detection pipeline disabled for '{RequestType}'", requestTypeName);
            return await nextStep().ConfigureAwait(false);
        }

        var attrInfo = CachedAttributeInfo;

        // Step 2: No [BreachMonitored] attribute on request — skip entirely
        if (attrInfo is null)
        {
            return await nextStep().ConfigureAwait(false);
        }

        // Step 3: Execute the handler to get the response
        var result = await nextStep().ConfigureAwait(false);

        // Step 4: Create SecurityEvent from request metadata + context
        var securityEvent = SecurityEventFactory.Create(
            request, attrInfo.EventType, attrInfo.Source, context, _timeProvider);

        // Step 5: Run breach detection
        try
        {
            var detectResult = await _detector.DetectAsync(securityEvent, cancellationToken)
                .ConfigureAwait(false);

            // If detection itself failed
            if (detectResult.IsLeft)
            {
                var error = (EncinaError)detectResult;

                _logger.LogWarning(
                    "Breach detection failed for '{RequestType}': {ErrorMessage}",
                    requestTypeName, error.Message);

                if (_options.EnforcementMode == BreachDetectionEnforcementMode.Block)
                {
                    return Left<EncinaError, TResponse>(error);
                }

                // Warn mode — detection failure doesn't block the response
                return result;
            }

            var breaches = detectResult.Match(
                Right: r => r,
                Left: _ => (IReadOnlyList<PotentialBreach>)[]);

            // No breaches detected — return the original response
            if (breaches.Count == 0)
            {
                return result;
            }

            // Breaches detected
            var ruleNames = string.Join(", ", breaches.Select(b => b.DetectionRuleName));

            _logger.LogWarning(
                "Breach detection triggered for '{RequestType}': {BreachCount} potential breach(es) " +
                "detected by rules [{RuleNames}]",
                requestTypeName, breaches.Count, ruleNames);

            // Record each detected breach via the event-sourced service
            foreach (var breach in breaches)
            {
                await _breachService.RecordBreachAsync(
                    nature: breach.Description,
                    severity: breach.Severity,
                    detectedByRule: breach.DetectionRuleName,
                    estimatedAffectedSubjects: 0,
                    description: $"Auto-detected by pipeline for request '{requestTypeName}'",
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            // Block mode — return error to the caller
            if (_options.EnforcementMode == BreachDetectionEnforcementMode.Block)
            {
                return Left<EncinaError, TResponse>(
                    BreachNotificationErrors.BreachDetected(requestTypeName, ruleNames));
            }

            // Warn mode — log and return the original response
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unhandled exception during breach detection for '{RequestType}'", requestTypeName);

            if (_options.EnforcementMode == BreachDetectionEnforcementMode.Block)
            {
                return Left<EncinaError, TResponse>(
                    BreachNotificationErrors.DetectionFailed(
                        $"Unhandled exception during detection for '{requestTypeName}'", ex));
            }

            // Warn mode — exception doesn't block the response
            return result;
        }
    }

    // ================================================================
    // Attribute resolution (runs once per closed generic type)
    // ================================================================

    private static BreachMonitoredInfo? ResolveAttributeInfo()
    {
        var attr = typeof(TRequest).GetCustomAttribute<BreachMonitoredAttribute>();

        if (attr is null)
        {
            return null;
        }

        return new BreachMonitoredInfo(
            EventType: attr.EventType,
            Source: typeof(TRequest).FullName ?? typeof(TRequest).Name);
    }

    // ================================================================
    // Nested types
    // ================================================================

    /// <summary>
    /// Cached attribute information from the <see cref="BreachMonitoredAttribute"/>
    /// on the request type.
    /// </summary>
    /// <param name="EventType">The security event type to generate for this request.</param>
    /// <param name="Source">The source identifier (request type's full name).</param>
    private sealed record BreachMonitoredInfo(
        SecurityEventType EventType,
        string Source);
}
