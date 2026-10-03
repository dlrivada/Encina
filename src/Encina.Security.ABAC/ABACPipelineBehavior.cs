using System.Diagnostics;

using Encina.Diagnostics;
using Encina.Security.ABAC.Diagnostics;
using Encina.Security.ABAC.EEL;
using Encina.Security.ABAC.Enforcement;

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
/// <item><description>Requiring a security context with a user: without one the request is denied
/// with <see cref="ABACErrors.MissingContextCode"/> in every enforcement mode, before any attribute
/// is collected.</description></item>
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
/// });
/// </code>
/// </example>
public sealed class ABACPipelineBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    // ── Static per-generic-type attribute caching ────────────────────
    private static readonly ABACAttributeInfo? CachedAttributeInfo = ABACAttributeInfo.Resolve<TRequest>();

    private readonly ABACRequirementEvaluator _requirementEvaluator;
    private readonly IAttributeProvider _attributeProvider;
    private readonly Security.ISecurityContextAccessor _securityContextAccessor;
    private readonly ObligationExecutor _obligationExecutor;
    private readonly ABACOptions _options;
    private readonly ILogger<ABACPipelineBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ABACPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="pdp">The policy decision point that evaluates each policy named by <see cref="RequirePolicyAttribute"/>.</param>
    /// <param name="attributeProvider">The attribute provider for collecting subject, resource, and environment attributes.</param>
    /// <param name="securityContextAccessor">Accessor for the current security context.</param>
    /// <param name="obligationExecutor">The executor for processing obligations and advice.</param>
    /// <param name="eelCompiler">The EEL compiler whose cached delegates evaluate <see cref="RequireConditionAttribute"/> expressions.</param>
    /// <param name="options">ABAC configuration options.</param>
    /// <param name="logger">Logger for ABAC evaluation tracing.</param>
    public ABACPipelineBehavior(
        IPolicyDecisionPoint pdp,
        IAttributeProvider attributeProvider,
        Security.ISecurityContextAccessor securityContextAccessor,
        ObligationExecutor obligationExecutor,
        EELCompiler eelCompiler,
        IOptions<ABACOptions> options,
        ILogger<ABACPipelineBehavior<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(pdp);
        ArgumentNullException.ThrowIfNull(attributeProvider);
        ArgumentNullException.ThrowIfNull(securityContextAccessor);
        ArgumentNullException.ThrowIfNull(obligationExecutor);
        ArgumentNullException.ThrowIfNull(eelCompiler);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _requirementEvaluator = new ABACRequirementEvaluator(pdp, eelCompiler, logger);
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
            // ── 4. Require a security context with a user ───────────
            var userId = ResolveUserId();
            if (userId is null)
            {
                return HandleMissingContext(startTimestamp, activity);
            }

            // ── 5. Collect attributes ───────────────────────────────
            var attributes = await CollectAttributesAsync(request, userId, cancellationToken)
                .ConfigureAwait(false);

            // ── 6. Evaluate the required policies and conditions ────
            var verdict = await _requirementEvaluator
                .EvaluateAsync(CachedAttributeInfo, attributes, typeof(TRequest), cancellationToken)
                .ConfigureAwait(false);

            ABACLogMessages.PdpDecisionReceived(_logger,
                requestTypeName,
                verdict.Decision.Effect.ToString(),
                verdict.Decision.PolicyId,
                verdict.Decision.EvaluationDuration.TotalMilliseconds);

            // ── 7. Process decision ─────────────────────────────────
            return await ProcessDecisionAsync(
                verdict, attributes.Context, nextStep, startTimestamp, activity, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            var elapsed = Stopwatch.GetElapsedTime(startTimestamp);

            ABACLogMessages.EvaluationFailed(_logger, ex.ForLogging(),
                requestTypeName,
                elapsed.TotalMilliseconds);

            ABACDiagnostics.EvaluationDuration.Record(elapsed.TotalMilliseconds);

            // The exception type only: its message can carry data and never reaches a tag.
            ABACDiagnostics.RecordIndeterminate(activity, ex.GetType().Name);

            ABACDiagnostics.EvaluationIndeterminate.Add(1,
                new KeyValuePair<string, object?>(ABACDiagnostics.TagRequestType, requestTypeName));

            return ABACErrors.EvaluationFailed(typeof(TRequest), ex);
        }
    }

    // ── Decision Processing ─────────────────────────────────────────

    // The requirement verdict is Permit, Deny or Indeterminate; a required policy that is
    // NotApplicable is already a Deny verdict.
    private async ValueTask<Either<EncinaError, TResponse>> ProcessDecisionAsync(
        ABACRequirementVerdict verdict,
        PolicyEvaluationContext evaluationContext,
        RequestHandlerCallback<TResponse> nextStep,
        long startTimestamp,
        Activity? activity,
        CancellationToken cancellationToken)
    {
        return verdict.Decision.Effect switch
        {
            Effect.Permit => await HandlePermitAsync(
                verdict.Decision, evaluationContext, nextStep, startTimestamp, activity, cancellationToken)
                .ConfigureAwait(false),

            Effect.Deny => await HandleDenyAsync(
                verdict, evaluationContext, nextStep, startTimestamp, activity, cancellationToken)
                .ConfigureAwait(false),

            _ => HandleIndeterminate(verdict.Decision, startTimestamp, activity)
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
                    error.GetCode().IfNone("encina.unknown"));
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
        ABACRequirementVerdict verdict,
        PolicyEvaluationContext evaluationContext,
        RequestHandlerCallback<TResponse> nextStep,
        long startTimestamp,
        Activity? activity,
        CancellationToken cancellationToken)
    {
        var decision = verdict.Decision;
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
                    error.GetCode().IfNone("encina.unknown")),
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

        // A missing policy or an unmet condition carries its own error code.
        var error = verdict.DenyError ?? ABACErrors.AccessDenied(typeof(TRequest), decision.PolicyId);

        return await ApplyEnforcementAsync(error, requestTypeName, nextStep).ConfigureAwait(false);
    }

    // ── Indeterminate ───────────────────────────────────────────────

    // An Indeterminate verdict is an error (a condition that does not compile or throws, a
    // policy store failure, a PDP error), not a definite verdict: it denies in every enforcement
    // mode, like an exception from the PDP or the attribute provider. Warn mode relaxes only
    // definite denials (see ApplyEnforcementAsync).
    private Either<EncinaError, TResponse> HandleIndeterminate(
        PolicyDecision decision,
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

        ABACLogMessages.EnforcementDenied(_logger, requestTypeName);

        return ABACErrors.Indeterminate(typeof(TRequest), reason);
    }

    // ── Missing Security Context ────────────────────────────────────

    // The user the subject attributes are collected for, or null when there is no security
    // context or it carries no user (no HttpContext, a background job, a misconfiguration).
    private string? ResolveUserId()
    {
        var userId = _securityContextAccessor.SecurityContext?.UserId;
        return string.IsNullOrWhiteSpace(userId) ? null : userId;
    }

    // A missing security context is not a definite policy verdict, so it denies in every
    // enforcement mode, before any attribute is collected: nothing is ever evaluated for an
    // empty user (#1676; AGENTS.md "compliance and security gates fail closed").
    private Either<EncinaError, TResponse> HandleMissingContext(long startTimestamp, Activity? activity)
    {
        var requestTypeName = typeof(TRequest).Name;

        RecordCompletion(startTimestamp, activity, Effect.Deny, null, ABACErrors.MissingContextCode);

        ABACDiagnostics.EvaluationDenied.Add(1,
            new KeyValuePair<string, object?>(ABACDiagnostics.TagRequestType, requestTypeName));

        ABACLogMessages.MissingSecurityContext(_logger, requestTypeName, ABACErrors.MissingContextCode);
        ABACLogMessages.EnforcementDenied(_logger, requestTypeName);

        return ABACErrors.MissingContext(typeof(TRequest));
    }

    // ── Attribute Collection ────────────────────────────────────────

    private async ValueTask<ABACCollectedAttributes> CollectAttributesAsync(
        TRequest request,
        string userId,
        CancellationToken cancellationToken)
    {
        var subjectAttributes = await _attributeProvider
            .GetSubjectAttributesAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        var resourceAttributes = await _attributeProvider
            .GetResourceAttributesAsync(request, cancellationToken)
            .ConfigureAwait(false);

        var environmentAttributes = await _attributeProvider
            .GetEnvironmentAttributesAsync(cancellationToken)
            .ConfigureAwait(false);

        var context = AttributeContextBuilder.Build(
            subjectAttributes,
            resourceAttributes,
            environmentAttributes,
            typeof(TRequest),
            _options.IncludeAdvice);

        return new ABACCollectedAttributes(subjectAttributes, resourceAttributes, environmentAttributes, context);
    }

    // ── Enforcement ─────────────────────────────────────────────────

    // Called only for definite denials (a Deny, a required policy that is NotApplicable or not
    // found, a condition that evaluates to false): Warn mode logs them and lets the request proceed.
    private async ValueTask<Either<EncinaError, TResponse>> ApplyEnforcementAsync(
        EncinaError error,
        string requestTypeName,
        RequestHandlerCallback<TResponse> nextStep)
    {
        if (_options.EnforcementMode == ABACEnforcementMode.Warn)
        {
            ABACLogMessages.EnforcementWarnMode(_logger,
                requestTypeName,
                error.GetCode().IfNone("encina.unknown"));

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
        }
    }

    private static void RecordDuration(long startTimestamp)
    {
        var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
        ABACDiagnostics.EvaluationDuration.Record(elapsed.TotalMilliseconds);
    }
}
