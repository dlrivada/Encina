using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;

using Encina.Compliance.AIAct.Abstractions;
using Encina.Compliance.AIAct.Attributes;
using Encina.Compliance.AIAct.Diagnostics;
using Encina.Compliance.AIAct.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static LanguageExt.Prelude;

namespace Encina.Compliance.AIAct;

/// <summary>
/// Pipeline behavior that enforces EU AI Act compliance requirements declared via attributes.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// <para>
/// This behavior inspects the request type for AI Act-related attributes and enforces
/// compliance checks before the request is handled:
/// </para>
/// <list type="number">
/// <item><description>Detects <see cref="HighRiskAIAttribute"/>, <see cref="RequireHumanOversightAttribute"/>, and <see cref="AITransparencyAttribute"/>.</description></item>
/// <item><description>Resolves the AI system identifier from attributes or explicit parameter.</description></item>
/// <item><description>Runs the <see cref="IAIActComplianceValidator"/> for risk classification and compliance checks.</description></item>
/// <item><description>Blocks prohibited practices unconditionally (Art. 5).</description></item>
/// <item><description>Enforces human oversight requirements (Art. 14).</description></item>
/// <item><description>Validates transparency obligations (Art. 13, Art. 50).</description></item>
/// </list>
/// <para>
/// The behavior supports three enforcement modes via <see cref="AIActOptions.EnforcementMode"/>:
/// <see cref="AIActEnforcementMode.Block"/> blocks non-compliant requests,
/// <see cref="AIActEnforcementMode.Warn"/> logs warnings but allows processing,
/// <see cref="AIActEnforcementMode.Disabled"/> skips all checks.
/// </para>
/// <para>
/// <b>Important:</b> Prohibited practices (Art. 5) are <em>always</em> blocked regardless of
/// enforcement mode. The EU AI Act does not allow prohibited systems to operate under any circumstances.
/// </para>
/// <para>
/// <b>Observability:</b> Emits OpenTelemetry traces via <c>Encina.Compliance.AIAct</c> ActivitySource,
/// metrics via <c>Encina.Compliance.AIAct</c> Meter, and structured log messages via
/// <c>LoggerMessage.Define</c> (EventIds 9500-9510).
/// </para>
/// </remarks>
public sealed class AIActCompliancePipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly ConcurrentDictionary<Type, AIActAttributeInfo?> AttributeCache = new();

    private readonly IAIActComplianceValidator _validator;
    private readonly AIActOptions _options;
    private readonly ILogger<AIActCompliancePipelineBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIActCompliancePipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="validator">The AI Act compliance validator.</param>
    /// <param name="options">AI Act configuration options.</param>
    /// <param name="logger">Logger for structured compliance logging.</param>
    public AIActCompliancePipelineBehavior(
        IAIActComplianceValidator validator,
        IOptions<AIActOptions> options,
        ILogger<AIActCompliancePipelineBehavior<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _validator = validator;
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

        var requestType = typeof(TRequest);
        var requestTypeName = requestType.Name;

        // Step 1: Disabled mode — no-op
        if (_options.EnforcementMode == AIActEnforcementMode.Disabled)
        {
            _logger.PipelineDisabled(requestTypeName);
            return await nextStep().ConfigureAwait(false);
        }

        // Step 2: Check for AI Act attributes (cached)
        var attrInfo = AttributeCache.GetOrAdd(requestType, static type =>
        {
            var highRisk = type.GetCustomAttribute<HighRiskAIAttribute>();
            var oversight = type.GetCustomAttribute<RequireHumanOversightAttribute>();
            var transparency = type.GetCustomAttribute<AITransparencyAttribute>();

            if (highRisk is null && oversight is null && transparency is null)
            {
                return null;
            }

            var systemId = highRisk?.SystemId ?? oversight?.SystemId;

            return new AIActAttributeInfo(highRisk, oversight, transparency, systemId);
        });

        // No AI Act attributes — skip entirely (zero overhead for non-AI requests)
        if (attrInfo is null)
        {
            _logger.ComplianceCheckSkipped(requestTypeName);
            AIActDiagnostics.ComplianceCheckSkipped.Add(1, new TagList
            {
                { AIActDiagnostics.TagRequestType, requestTypeName }
            });
            return await nextStep().ConfigureAwait(false);
        }

        // Step 3: Start timing and activity span
        var startTimestamp = Stopwatch.GetTimestamp();
        var enforcementModeName = _options.EnforcementMode.ToString();
        using var activity = AIActDiagnostics.StartComplianceCheck(requestTypeName, enforcementModeName);

        var resolvedSystemId = attrInfo.SystemId ?? "auto";
        _logger.ComplianceCheckStarted(requestTypeName, resolvedSystemId);

        // Step 4: Run compliance validation
        var validationResult = await _validator
            .ValidateAsync(request, attrInfo.SystemId, cancellationToken)
            .ConfigureAwait(false);

        // Handle validator errors
        if (validationResult.IsLeft)
        {
            var innerError = (EncinaError)validationResult;
            var validatorError = AIActErrors.ValidatorError(requestType, innerError);
            _logger.ValidatorError(requestTypeName, innerError.Message);
            RecordFailed(activity, startTimestamp, requestTypeName, AIActErrors.ValidatorErrorCode);
            return Left<EncinaError, TResponse>(validatorError);
        }

        var compliance = (AIActComplianceResult)validationResult;
        AIActDiagnostics.SetSystemId(activity, compliance.SystemId);
        AIActDiagnostics.SetRiskLevel(activity, compliance.RiskLevel.ToString());

        // Step 5: Prohibited practices are ALWAYS blocked (Art. 5 — no exceptions)
        if (compliance.IsProhibited)
        {
            var error = AIActErrors.ProhibitedUse(requestType, compliance.SystemId, compliance.Violations);
            var violationsSummary = string.Join("; ", compliance.Violations);
            _logger.ProhibitedUseBlocked(requestTypeName, compliance.SystemId, violationsSummary);
            AIActDiagnostics.ProhibitedUseBlocked.Add(1, new TagList
            {
                { AIActDiagnostics.TagRequestType, requestTypeName },
                { AIActDiagnostics.TagSystemId, compliance.SystemId }
            });
            RecordFailed(activity, startTimestamp, requestTypeName, AIActErrors.ProhibitedUseCode);
            return Left<EncinaError, TResponse>(error);
        }

        // Step 6: Check for violations in Block vs Warn mode
        if (compliance.Violations.Count > 0)
        {
            var violationsSummary = string.Join("; ", compliance.Violations);

            if (_options.EnforcementMode == AIActEnforcementMode.Block)
            {
                var error = AIActErrors.ComplianceValidationFailed(
                    requestType, compliance.SystemId, compliance.RiskLevel, compliance.Violations);
                _logger.ViolationsBlocked(requestTypeName, compliance.SystemId, violationsSummary);
                RecordFailed(activity, startTimestamp, requestTypeName, AIActErrors.ComplianceValidationFailedCode);
                return Left<EncinaError, TResponse>(error);
            }

            // Warn mode — log but proceed
            _logger.ViolationsWarned(requestTypeName, compliance.SystemId, violationsSummary);
        }

        // Step 7: Human oversight check (Art. 14)
        if (compliance.RequiresHumanOversight)
        {
            _logger.HumanOversightRequired(requestTypeName, compliance.SystemId);
        }

        // Step 8: Transparency check (Art. 13, Art. 50)
        if (compliance.RequiresTransparency)
        {
            _logger.TransparencyObligation(requestTypeName, compliance.SystemId);
        }

        // Step 9: Record success and proceed
        RecordPassed(activity, startTimestamp, requestTypeName, compliance.RiskLevel.ToString());
        return await nextStep().ConfigureAwait(false);
    }

    private void RecordPassed(Activity? activity, long startTimestamp, string requestTypeName, string riskLevel)
    {
        var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
        var tags = new TagList
        {
            { AIActDiagnostics.TagRequestType, requestTypeName }
        };

        AIActDiagnostics.ComplianceCheckTotal.Add(1, tags);
        AIActDiagnostics.ComplianceCheckPassed.Add(1, tags);
        AIActDiagnostics.ComplianceCheckDuration.Record(elapsed.TotalMilliseconds, tags);
        AIActDiagnostics.RecordPassed(activity);
        _logger.ComplianceCheckPassed(requestTypeName, riskLevel);
    }

    private void RecordFailed(Activity? activity, long startTimestamp, string requestTypeName, string failureReason)
    {
        var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
        var tags = new TagList
        {
            { AIActDiagnostics.TagRequestType, requestTypeName },
            { AIActDiagnostics.TagFailureReason, failureReason }
        };

        AIActDiagnostics.ComplianceCheckTotal.Add(1, tags);
        AIActDiagnostics.ComplianceCheckFailed.Add(1, tags);
        AIActDiagnostics.ComplianceCheckDuration.Record(elapsed.TotalMilliseconds, tags);
        AIActDiagnostics.RecordFailed(activity, failureReason);
        _logger.ComplianceCheckFailed(requestTypeName, "", failureReason);
    }

    /// <summary>
    /// Cached AI Act attribute information for a request type.
    /// </summary>
    private sealed record AIActAttributeInfo(
        HighRiskAIAttribute? HighRisk,
        RequireHumanOversightAttribute? RequireHumanOversight,
        AITransparencyAttribute? Transparency,
        string? SystemId);
}
