using System.Diagnostics;

using Encina.Security.ABAC.Diagnostics;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Security.ABAC;

/// <summary>
/// Pipeline behavior that acts as the XACML Policy Enforcement Point (PEP).
/// </summary>
/// <remarks>
/// <para>
/// Intercepts requests decorated with <see cref="RequirePolicyAttribute"/> and/or
/// <see cref="RequireConditionAttribute"/>, evaluates them against ABAC policies via the
/// <see cref="IPolicyDecisionPoint"/>, executes obligations, and enforces the authorization decision.
/// </para>
/// <para>
/// XACML 3.0 §7.18 — The PEP is responsible for:
/// </para>
/// <list type="number">
/// <item><description>Collecting attributes from <see cref="IAttributeProvider"/>.</description></item>
/// <item><description>Sending the request to the PDP for evaluation.</description></item>
/// <item><description>Executing obligations returned with the decision.</description></item>
/// <item><description>Enforcing the decision (Permit, Deny, NotApplicable, Indeterminate).</description></item>
/// </list>
/// <para>
/// If any mandatory obligation fails, access is denied regardless of the PDP decision.
/// Advice expressions are executed on a best-effort basis.
/// </para>
/// <para>
/// Uses static per-generic-type attribute caching for zero-cost attribute discovery
/// after the first invocation per request type.
/// </para>
/// </remarks>
/// <typeparam name="TRequest">The request type being processed.</typeparam>
/// <typeparam name="TResponse">The response type returned by the handler.</typeparam>
/// <example>
/// <code>
/// // Request decorated with ABAC attributes
/// [RequirePolicy("finance-access")]
/// public record GetFinancialReportQuery : IRequest&lt;FinancialReport&gt;
/// {
///     public string ReportId { get; init; }
/// }
///
/// // Registration
/// services.AddEncinaABAC(options =>
/// {
///     options.EnforcementMode = ABACEnforcementMode.Block;
///     options.DefaultNotApplicableEffect = Effect.Deny;
/// });
/// </code>
/// </example>
public sealed class ABACPipelineBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    // ── Static per-generic-type attribute caching ────────────────────
    private static readonly ABACAttributeInfo? CachedAttributeInfo = ABACAttributeInfo.Resolve<TRequest>();

    private readonly IPolicyDecisionPoint _pdp;
    private readonly IAttributeProvider _attributeProvider;
    private readonly Security.ISecurityContextAccessor _securityContextAccessor;
    private readonly ObligationExecutor _obligationExecutor;
    private readonly ABACOptions _options;
    private readonly ILogger<ABACPipelineBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ABACPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="pdp">The policy decision point for evaluating authorization decisions.</param>
    /// <param name="attributeProvider">The attribute provider for collecting subject, resource, and environment attributes.</param>
    /// <param name="securityContextAccessor">Accessor for the current security context.</param>
    /// <param name="obligationExecutor">The executor for processing obligations and advice.</param>
    /// <param name="options">ABAC configuration options.</param>
    /// <param name="logger">Logger for ABAC evaluation tracing.</param>
    public ABACPipelineBehavior(
        IPolicyDecisionPoint pdp,
        IAttributeProvider attributeProvider,
        Security.ISecurityContextAccessor securityContextAccessor,
        ObligationExecutor obligationExecutor,
        IOptions<ABACOptions> options,
        ILogger<ABACPipelineBehavior<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(pdp);
        ArgumentNullException.ThrowIfNull(attributeProvider);
        ArgumentNullException.ThrowIfNull(securityContextAccessor);
        ArgumentNullException.ThrowIfNull(obligationExecutor);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _pdp = pdp;
        _attributeProvider = attributeProvider;
        _securityContextAccessor = securityContextAccessor;
        _obligationExecutor = obligationExecutor;
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
        // ── 1. Disabled mode — skip entirely ────────────────────────
        if (_options.EnforcementMode == ABACEnforcementMode.Disabled)
        {
            return await nextStep().ConfigureAwait(false);
        }

        // ── 2. No ABAC attributes — skip evaluation ────────────────
        if (CachedAttributeInfo is null)
        {
            return await nextStep().ConfigureAwait(false);
        }

        var requestTypeName = typeof(TRequest).Name;

        ABACLogMessages.EvaluationStarting(_logger,
            requestTypeName,
            CachedAttributeInfo.PolicyAttributes.Count,
            CachedAttributeInfo.ConditionAttributes.Count);

        // ── 3. Start tracing ────────────────────────────────────────
        using var activity = ABACDiagnostics.StartEvaluation(requestTypeName);
        var startTimestamp = Stopwatch.GetTimestamp();

        ABACDiagnostics.EvaluationTotal.Add(1,
            new KeyValuePair<string, object?>(ABACDiagnostics.TagRequestType, requestTypeName),
            new KeyValuePair<string, object?>(ABACDiagnostics.TagEnforcementMode, _options.EnforcementMode.ToString()));

        try
        {
            // ── 4. Collect attributes ───────────────────────────────
            var evaluationContext = await CollectAttributesAsync(cancellationToken)
                .ConfigureAwait(false);

            // ── 5. Evaluate policies via PDP ────────────────────────
            var decision = await _pdp.EvaluateAsync(evaluationContext, cancellationToken)
                .ConfigureAwait(false);

            ABACLogMessages.PdpDecisionReceived(_logger,
                requestTypeName,
                decision.Effect.ToString(),
                decision.PolicyId,
                decision.EvaluationDuration.TotalMilliseconds);

            // ── 6. Process decision ─────────────────────────────────
            return await ProcessDecisionAsync(
                decision, evaluationContext, nextStep, startTimestamp, activity, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var elapsed = Stopwatch.GetElapsedTime(startTimestamp);

            ABACLogMessages.EvaluationFailed(_logger, ex,
                requestTypeName,
                elapsed.TotalMilliseconds);

            ABACDiagnostics.EvaluationDuration.Record(elapsed.TotalMilliseconds);
            ABACDiagnostics.RecordIndeterminate(activity, ex.Message);

            ABACDiagnostics.EvaluationIndeterminate.Add(1,
                new KeyValuePair<string, object?>(ABACDiagnostics.TagRequestType, requestTypeName));

            return ABACErrors.EvaluationFailed(typeof(TRequest), ex);
        }
    }

    // ── Decision Processing ─────────────────────────────────────────

    private async ValueTask<Either<EncinaError, TResponse>> ProcessDecisionAsync(
        PolicyDecision decision,
        PolicyEvaluationContext evaluationContext,
        RequestHandlerCallback<TResponse> nextStep,
        long startTimestamp,
        Activity? activity,
        CancellationToken cancellationToken)
    {
        return decision.Effect switch
        {
            Effect.Permit => await HandlePermitAsync(
                decision, evaluationContext, nextStep, startTimestamp, activity, cancellationToken)
                .ConfigureAwait(false),

            Effect.Deny => await HandleDenyAsync(
                decision, evaluationContext, nextStep, startTimestamp, activity, cancellationToken)
                .ConfigureAwait(false),

            Effect.NotApplicable => await HandleNotApplicableAsync(
                decision, evaluationContext, nextStep, startTimestamp, activity, cancellationToken)
                .ConfigureAwait(false),

            Effect.Indeterminate => await HandleIndeterminateAsync(
                decision, nextStep, startTimestamp, activity)
                .ConfigureAwait(false),

            _ => ABACErrors.Indeterminate(typeof(TRequest), "Unknown effect value.")
        };
    }

    // ── Permit ──────────────────────────────────────────────────────

    private async ValueTask<Either<EncinaError, TResponse>> HandlePermitAsync(
        PolicyDecision decision,
        PolicyEvaluationContext evaluationContext,
        RequestHandlerCallback<TResponse> nextStep,
        long startTimestamp,
        Activity? activity,
        CancellationToken cancellationToken)
    {
        var requestTypeName = typeof(TRequest).Name;

        // Execute OnPermit obligations (mandatory — failure means deny per XACML §7.18)
        var obligationResult = await _obligationExecutor.ExecuteObligationsAsync(
            decision.Obligations, evaluationContext, cancellationToken)
            .ConfigureAwait(false);

        var obligationFailed = obligationResult.Match(
            Left: error =>
            {
                ABACLogMessages.PermitObligationsFailed(_logger,
                    requestTypeName,
                    error.Message);
                return true;
            },
            Right: _ => false);

        if (obligationFailed)
        {
            RecordCompletion(startTimestamp, activity, Effect.Deny, decision.PolicyId,
                "Obligation execution failed");

            ABACDiagnostics.EvaluationDenied.Add(1,
                new KeyValuePair<string, object?>(ABACDiagnostics.TagRequestType, requestTypeName));

            return ABACErrors.ObligationFailed(
                "permit-obligations",
                "One or more mandatory obligations could not be fulfilled. Access denied per XACML §7.18.");
        }

        // Execute advice (best-effort — failures don't affect decision)
        if (decision.Advice.Count > 0)
        {
            await _obligationExecutor.ExecuteAdviceAsync(
                decision.Advice, evaluationContext, cancellationToken)
                .ConfigureAwait(false);
        }

        RecordCompletion(startTimestamp, activity, Effect.Permit, decision.PolicyId, null);

        ABACDiagnostics.EvaluationPermitted.Add(1,
            new KeyValuePair<string, object?>(ABACDiagnostics.TagRequestType, requestTypeName));

        ABACLogMessages.EvaluationPermitted(_logger, requestTypeName);

        return await nextStep().ConfigureAwait(false);
    }

    // ── Deny ────────────────────────────────────────────────────────

    private async ValueTask<Either<EncinaError, TResponse>> HandleDenyAsync(
        PolicyDecision decision,
        PolicyEvaluationContext evaluationContext,
        RequestHandlerCallback<TResponse> nextStep,
        long startTimestamp,
        Activity? activity,
        CancellationToken cancellationToken)
    {
        var requestTypeName = typeof(TRequest).Name;
        var reason = decision.Reason ?? "Access denied by ABAC policy.";

        // Execute OnDeny obligations (mandatory — per XACML §7.18, obligations
        // associated with the Deny effect must still be fulfilled)
        if (decision.Obligations.Count > 0)
        {
            var obligationResult = await _obligationExecutor.ExecuteObligationsAsync(
                decision.Obligations, evaluationContext, cancellationToken)
                .ConfigureAwait(false);

            obligationResult.Match(
                Left: error => ABACLogMessages.OnDenyObligationFailed(_logger,
                    requestTypeName,
                    error.Message),
                Right: _ => ABACLogMessages.OnDenyObligationsExecuted(_logger,
                    requestTypeName));
        }

        // Execute advice (best-effort)
        if (decision.Advice.Count > 0)
        {
            await _obligationExecutor.ExecuteAdviceAsync(
                decision.Advice, evaluationContext, cancellationToken)
                .ConfigureAwait(false);
        }

        RecordCompletion(startTimestamp, activity, Effect.Deny, decision.PolicyId, reason);

        ABACDiagnostics.EvaluationDenied.Add(1,
            new KeyValuePair<string, object?>(ABACDiagnostics.TagRequestType, requestTypeName));

        var error2 = ABACErrors.AccessDenied(typeof(TRequest), decision.PolicyId);

        return await ApplyEnforcementAsync(error2, requestTypeName, nextStep).ConfigureAwait(false);
    }

    // ── NotApplicable ───────────────────────────────────────────────

    private async ValueTask<Either<EncinaError, TResponse>> HandleNotApplicableAsync(
        PolicyDecision decision,
        PolicyEvaluationContext evaluationContext,
        RequestHandlerCallback<TResponse> nextStep,
        long startTimestamp,
        Activity? activity,
        CancellationToken cancellationToken)
    {
        var requestTypeName = typeof(TRequest).Name;

        if (_options.DefaultNotApplicableEffect == Effect.Permit)
        {
            ABACLogMessages.NotApplicablePermit(_logger, requestTypeName);

            // Execute any advice (best-effort)
            if (decision.Advice.Count > 0)
            {
                await _obligationExecutor.ExecuteAdviceAsync(
                    decision.Advice, evaluationContext, cancellationToken)
                    .ConfigureAwait(false);
            }

            ABACDiagnostics.RecordNotApplicable(activity);
            RecordDuration(startTimestamp);

            ABACDiagnostics.EvaluationNotApplicable.Add(1,
                new KeyValuePair<string, object?>(ABACDiagnostics.TagRequestType, requestTypeName));

            return await nextStep().ConfigureAwait(false);
        }

        // Default: Deny (closed-world assumption)
        ABACLogMessages.NotApplicableDeny(_logger, requestTypeName);

        RecordCompletion(startTimestamp, activity, Effect.NotApplicable, decision.PolicyId,
            "No applicable policy found");

        ABACDiagnostics.EvaluationNotApplicable.Add(1,
            new KeyValuePair<string, object?>(ABACDiagnostics.TagRequestType, requestTypeName));

        var error = ABACErrors.AccessDenied(typeof(TRequest), decision.PolicyId);
        return await ApplyEnforcementAsync(error, requestTypeName, nextStep).ConfigureAwait(false);
    }

    // ── Indeterminate ───────────────────────────────────────────────

    private async ValueTask<Either<EncinaError, TResponse>> HandleIndeterminateAsync(
        PolicyDecision decision,
        RequestHandlerCallback<TResponse> nextStep,
        long startTimestamp,
        Activity? activity)
    {
        var requestTypeName = typeof(TRequest).Name;
        var reason = decision.Reason ?? "Policy evaluation produced an indeterminate result.";

        RecordCompletion(startTimestamp, activity, Effect.Indeterminate, decision.PolicyId, reason);

        ABACDiagnostics.EvaluationIndeterminate.Add(1,
            new KeyValuePair<string, object?>(ABACDiagnostics.TagRequestType, requestTypeName));

        ABACLogMessages.EvaluationIndeterminate(_logger,
            requestTypeName,
            reason);

        var error = ABACErrors.Indeterminate(typeof(TRequest), reason);
        return await ApplyEnforcementAsync(error, requestTypeName, nextStep).ConfigureAwait(false);
    }

    // ── Attribute Collection ────────────────────────────────────────

    private async ValueTask<PolicyEvaluationContext> CollectAttributesAsync(
        CancellationToken cancellationToken)
    {
        var securityContext = _securityContextAccessor.SecurityContext;
        var userId = securityContext?.UserId ?? string.Empty;

        var subjectAttributes = await _attributeProvider
            .GetSubjectAttributesAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        var resourceAttributes = await _attributeProvider
            .GetResourceAttributesAsync<TRequest>(default!, cancellationToken)
            .ConfigureAwait(false);

        var environmentAttributes = await _attributeProvider
            .GetEnvironmentAttributesAsync(cancellationToken)
            .ConfigureAwait(false);

        return AttributeContextBuilder.Build(
            subjectAttributes,
            resourceAttributes,
            environmentAttributes,
            typeof(TRequest),
            _options.IncludeAdvice);
    }

    // ── Enforcement ─────────────────────────────────────────────────

    private async ValueTask<Either<EncinaError, TResponse>> ApplyEnforcementAsync(
        EncinaError error,
        string requestTypeName,
        RequestHandlerCallback<TResponse> nextStep)
    {
        if (_options.EnforcementMode == ABACEnforcementMode.Warn)
        {
            ABACLogMessages.EnforcementWarnMode(_logger,
                requestTypeName,
                error.Message);

            return await nextStep().ConfigureAwait(false);
        }

        ABACLogMessages.EnforcementDenied(_logger, requestTypeName);

        return Either<EncinaError, TResponse>.Left(error);
    }

    // ── Telemetry Helpers ───────────────────────────────────────────

    private static void RecordCompletion(
        long startTimestamp, Activity? activity, Effect effect, string? policyId, string? reason)
    {
        RecordDuration(startTimestamp);

        switch (effect)
        {
            case Effect.Permit:
                ABACDiagnostics.RecordPermitted(activity, policyId);
                break;
            case Effect.Deny:
                ABACDiagnostics.RecordDenied(activity, policyId, reason ?? "denied");
                break;
            case Effect.Indeterminate:
                ABACDiagnostics.RecordIndeterminate(activity, reason ?? "indeterminate");
                break;
            case Effect.NotApplicable:
                ABACDiagnostics.RecordNotApplicable(activity);
                break;
        }
    }

    private static void RecordDuration(long startTimestamp)
    {
        var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
        ABACDiagnostics.EvaluationDuration.Record(elapsed.TotalMilliseconds);
    }
}
