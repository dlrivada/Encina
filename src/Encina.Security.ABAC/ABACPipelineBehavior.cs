using System.Diagnostics;

using Encina.Diagnostics;
using Encina.Security.ABAC.DecisionAudit;
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
/// <item><description>Requiring an authenticated caller: <see cref="IRequestContext.Identity"/> is read
/// once, and an anonymous identity is denied with <see cref="EncinaErrorCodes.AuthorizationUnauthenticated"/>
/// (<see cref="ABACErrors.UnauthenticatedCaller"/>) in every enforcement mode except
/// <see cref="ABACEnforcementMode.Disabled"/>, before any attribute is collected. A user and a
/// declared service identity are both evaluated; policies decide, the PEP never permits a kind by
/// itself.</description></item>
/// <item><description>Collecting attributes from <see cref="IAttributeProvider"/>, plus the built-in
/// subject attributes of <see cref="ABACSubjectAttributes"/>.</description></item>
/// <item><description>Sending the request to the PDP for evaluation.</description></item>
/// <item><description>Executing obligations returned with the decision.</description></item>
/// <item><description>Enforcing the decision (Permit, Deny, NotApplicable, Indeterminate).</description></item>
/// </list>
/// <para>
/// If any mandatory obligation fails, access is denied regardless of the PDP decision.
/// Advice expressions are executed on a best-effort basis.
/// </para>
/// <para>
/// <b>Decide, record, enforce.</b> <see cref="Handle"/> first decides (collects attributes, evaluates
/// the requirements, runs obligations and advice, applies Warn mode) without ever calling the next
/// step. When <see cref="ABACOptions.DecisionAudit"/> is enabled it then writes one
/// <see cref="ABACDecisionRecord"/> through <see cref="IABACDecisionRecorder"/> before the handler runs
/// (write-ahead), and only then enforces: the handler is called, outside any <c>try</c>, only when the
/// request proceeds. A handler exception therefore propagates unchanged and is never reported as an
/// evaluation failure. When the record cannot be written the request is denied with
/// <see cref="ABACErrors.DecisionAuditFailedCode"/> under
/// <see cref="ABACDecisionAuditFailureMode.FailClosed"/> (the default), and proceeds with a logged
/// failure under <see cref="ABACDecisionAuditFailureMode.BestEffort"/>; a request that is denied
/// anyway stays denied.
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
    private readonly ObligationExecutor _obligationExecutor;
    private readonly ABACOptions _options;
    private readonly IABACDecisionRecorder _decisionRecorder;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ABACPipelineBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ABACPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="pdp">The policy decision point that evaluates each policy named by <see cref="RequirePolicyAttribute"/>.</param>
    /// <param name="attributeProvider">The attribute provider for collecting subject, resource, and environment attributes.</param>
    /// <param name="obligationExecutor">The executor for processing obligations and advice.</param>
    /// <param name="eelCompiler">The EEL compiler whose cached delegates evaluate <see cref="RequireConditionAttribute"/> expressions.</param>
    /// <param name="options">ABAC configuration options.</param>
    /// <param name="decisionRecorder">The recorder that persists decision records when <see cref="ABACOptions.DecisionAudit"/> is enabled.</param>
    /// <param name="timeProvider">The clock for the timestamps of decision records; read only when the decision audit is enabled.</param>
    /// <param name="logger">Logger for ABAC evaluation tracing.</param>
    /// <remarks>
    /// The caller is read from the <see cref="IRequestContext"/> that <see cref="Handle"/> receives,
    /// so the behavior resolves no identity service of its own.
    /// </remarks>
    public ABACPipelineBehavior(
        IPolicyDecisionPoint pdp,
        IAttributeProvider attributeProvider,
        ObligationExecutor obligationExecutor,
        EELCompiler eelCompiler,
        IOptions<ABACOptions> options,
        IABACDecisionRecorder decisionRecorder,
        TimeProvider timeProvider,
        ILogger<ABACPipelineBehavior<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(pdp);
        ArgumentNullException.ThrowIfNull(attributeProvider);
        ArgumentNullException.ThrowIfNull(obligationExecutor);
        ArgumentNullException.ThrowIfNull(eelCompiler);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(decisionRecorder);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _requirementEvaluator = new ABACRequirementEvaluator(pdp, eelCompiler, logger);
        _attributeProvider = attributeProvider;
        _obligationExecutor = obligationExecutor;
        _options = options.Value;
        _decisionRecorder = decisionRecorder;
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

        // ── 4. Decide (never calls the next step) ───────────────────
        // The clock is read only when the audit is enabled: StartedAtUtc here, CompletedAtUtc once at the end.
        var startedAtUtc = _options.DecisionAudit.Enabled ? _timeProvider.GetUtcNow() : default;
        var verdict = await DecideAsync(request, context, startedAtUtc, startTimestamp, activity, cancellationToken)
            .ConfigureAwait(false);

        // ── 5. Record (write-ahead) ─────────────────────────────────
        var denial = await RecordAsync(verdict, requestTypeName, cancellationToken).ConfigureAwait(false);

        // ── 6. Enforce: the handler runs here, outside any try/catch ─
        return denial is { } error
            ? Either<EncinaError, TResponse>.Left(error)
            : await nextStep().ConfigureAwait(false);
    }

    // ── Decide ──────────────────────────────────────────────────────

    // Collects the attributes, evaluates the requirements and turns the decision into what the PEP
    // enforces. Any exception except the caller's cancellation is an evaluation failure that denies.
    private async ValueTask<ABACEnforcementVerdict> DecideAsync(
        TRequest request,
        IRequestContext context,
        DateTimeOffset startedAtUtc,
        long startTimestamp,
        Activity? activity,
        CancellationToken cancellationToken)
    {
        // The identity is read once (#1892): the record and the subject attributes share this snapshot.
        RequestIdentity? caller = null;
        ABACCollectedAttributes? attributes = null;
        ABACRequirementVerdict? requirement = null;
        ABACEnforcementVerdict verdict;

        try
        {
            caller = ResolveCaller(context);

            if (caller is null)
            {
                verdict = DecideUnauthenticated(startTimestamp, activity);
            }
            else
            {
                attributes = await CollectAttributesAsync(request, caller, cancellationToken).ConfigureAwait(false);
                requirement = await _requirementEvaluator
                    .EvaluateAsync(CachedAttributeInfo!, attributes, typeof(TRequest), cancellationToken)
                    .ConfigureAwait(false);

                ABACLogMessages.PdpDecisionReceived(_logger,
                    typeof(TRequest).Name,
                    requirement.Decision.Effect.ToString(),
                    requirement.Decision.PolicyId,
                    requirement.Decision.EvaluationDuration.TotalMilliseconds);

                verdict = await ProcessDecisionAsync(requirement, attributes.Context, startTimestamp, activity, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            verdict = DecideFailed(ex, startTimestamp, activity);
        }

        return _options.DecisionAudit.Enabled
            ? WithRecord(verdict, request, context, caller, attributes, requirement, startedAtUtc, startTimestamp, activity)
            : verdict;
    }

    // A record that cannot be built (a throwing resource-id getter, an unusable option) is an
    // evaluation failure: the request is denied rather than escaping as an exception.
    private ABACEnforcementVerdict WithRecord(
        ABACEnforcementVerdict verdict,
        TRequest request,
        IRequestContext context,
        RequestIdentity? caller,
        ABACCollectedAttributes? attributes,
        ABACRequirementVerdict? requirement,
        DateTimeOffset startedAtUtc,
        long startTimestamp,
        Activity? activity)
    {
        try
        {
            return verdict with { Record = BuildRecord(request, context, caller, attributes, requirement, verdict, startedAtUtc) };
        }
        catch (Exception ex)
        {
            return DecideFailed(ex, startTimestamp, activity);
        }
    }

    // The requirement verdict is Permit, Deny or Indeterminate; a required policy that is
    // NotApplicable is already a Deny verdict.
    private async ValueTask<ABACEnforcementVerdict> ProcessDecisionAsync(
        ABACRequirementVerdict verdict,
        PolicyEvaluationContext evaluationContext,
        long startTimestamp,
        Activity? activity,
        CancellationToken cancellationToken)
    {
        return verdict.Decision.Effect switch
        {
            Effect.Permit => await DecidePermitAsync(
                verdict.Decision, evaluationContext, startTimestamp, activity, cancellationToken)
                .ConfigureAwait(false),

            Effect.Deny => await DecideDenyAsync(
                verdict, evaluationContext, startTimestamp, activity, cancellationToken)
                .ConfigureAwait(false),

            _ => DecideIndeterminate(verdict.Decision, startTimestamp, activity)
        };
    }

    // ── Permit ──────────────────────────────────────────────────────

    private async ValueTask<ABACEnforcementVerdict> DecidePermitAsync(
        PolicyDecision decision,
        PolicyEvaluationContext evaluationContext,
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

            return ABACEnforcementVerdict.Denied(ABACErrors.ObligationFailed(
                "permit-obligations",
                "One or more mandatory obligations could not be fulfilled. Access denied per XACML §7.18."));
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

        return ABACEnforcementVerdict.Granted();
    }

    // ── Deny ────────────────────────────────────────────────────────

    private async ValueTask<ABACEnforcementVerdict> DecideDenyAsync(
        ABACRequirementVerdict verdict,
        PolicyEvaluationContext evaluationContext,
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

        return ApplyEnforcement(error, requestTypeName);
    }

    // ── Indeterminate ───────────────────────────────────────────────

    // An Indeterminate verdict is an error (a condition that does not compile or throws, a
    // policy store failure, a PDP error), not a definite verdict: it denies in every enforcement
    // mode, like an exception from the PDP or the attribute provider, and is always recorded.
    // Warn mode relaxes only definite denials (see ApplyEnforcement).
    private ABACEnforcementVerdict DecideIndeterminate(
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

        return ABACEnforcementVerdict.Denied(ABACErrors.Indeterminate(typeof(TRequest), reason), alwaysRecorded: true);
    }

    // ── Evaluation failure ──────────────────────────────────────────

    // An exception while collecting attributes or evaluating is not a verdict: it denies in every
    // enforcement mode and is always recorded. The exception type only reaches telemetry; its
    // message can carry data and never reaches a tag.
    private ABACEnforcementVerdict DecideFailed(Exception ex, long startTimestamp, Activity? activity)
    {
        var requestTypeName = typeof(TRequest).Name;
        var elapsed = Stopwatch.GetElapsedTime(startTimestamp);

        ABACLogMessages.EvaluationFailed(_logger, ex.ForLogging(),
            requestTypeName,
            elapsed.TotalMilliseconds);

        ABACDiagnostics.EvaluationDuration.Record(elapsed.TotalMilliseconds);

        ABACDiagnostics.RecordIndeterminate(activity, ex.GetType().Name);

        ABACDiagnostics.EvaluationIndeterminate.Add(1,
            new KeyValuePair<string, object?>(ABACDiagnostics.TagRequestType, requestTypeName));

        return ABACEnforcementVerdict.Denied(ABACErrors.EvaluationFailed(typeof(TRequest), ex), alwaysRecorded: true);
    }

    // ── Unauthenticated Caller ──────────────────────────────────────

    // The caller the subject attributes are collected for, read once from the request context
    // (the identity can turn anonymous between two reads once its scope ends, #1892), or null when
    // the request has no authenticated caller (no UseEncinaContext, a job without a service
    // identity scope, an ended scope). An authenticated identity always carries a user id.
    private static RequestIdentity? ResolveCaller(IRequestContext context) =>
        context.Identity is { IsAuthenticated: true } identity ? identity : null;

    // An unauthenticated caller is not a definite policy verdict, so it denies in every
    // enforcement mode, before any attribute is collected: nothing is ever evaluated for an
    // anonymous caller (#1676, #1705; AGENTS.md "compliance and security gates fail closed").
    private ABACEnforcementVerdict DecideUnauthenticated(long startTimestamp, Activity? activity)
    {
        var requestTypeName = typeof(TRequest).Name;

        RecordCompletion(startTimestamp, activity, Effect.Deny, null, EncinaErrorCodes.AuthorizationUnauthenticated);

        ABACDiagnostics.EvaluationDenied.Add(1,
            new KeyValuePair<string, object?>(ABACDiagnostics.TagRequestType, requestTypeName));

        ABACLogMessages.UnauthenticatedCaller(_logger, requestTypeName, EncinaErrorCodes.AuthorizationUnauthenticated);
        ABACLogMessages.EnforcementDenied(_logger, requestTypeName);

        return ABACEnforcementVerdict.Denied(ABACErrors.UnauthenticatedCaller(typeof(TRequest)), alwaysRecorded: true);
    }

    // ── Attribute Collection ────────────────────────────────────────

    private async ValueTask<ABACCollectedAttributes> CollectAttributesAsync(
        TRequest request,
        RequestIdentity caller,
        CancellationToken cancellationToken)
    {
        var providedSubjectAttributes = await _attributeProvider
            .GetSubjectAttributesAsync(caller, cancellationToken)
            .ConfigureAwait(false);

        // The built-in subject-id and identity-kind attributes are added last, so a provider
        // cannot replace them (decision N3 of #1705).
        var subjectAttributes = ABACSubjectAttributes.WithBuiltIns(providedSubjectAttributes, caller);

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

        return new ABACCollectedAttributes(subjectAttributes, resourceAttributes, environmentAttributes, WithTraceRequest(context));
    }

    // The PDP builds a trace only when the decision audit asks for one, so a request without
    // the audit allocates nothing for it.
    private PolicyEvaluationContext WithTraceRequest(PolicyEvaluationContext context) =>
        _options.DecisionAudit is { Enabled: true, IncludeEvaluationTrace: true } audit
            ? context with { IncludeEvaluationTrace = true, MaxTraceEntries = audit.MaxTraceEntries }
            : context;

    // ── Enforcement of a definite denial ────────────────────────────

    // Called only for definite denials (a Deny, a required policy that is NotApplicable or not
    // found, a condition that evaluates to false): Warn mode logs them and lets the request proceed.
    private ABACEnforcementVerdict ApplyEnforcement(EncinaError error, string requestTypeName)
    {
        if (_options.EnforcementMode == ABACEnforcementMode.Warn)
        {
            ABACLogMessages.EnforcementWarnMode(_logger,
                requestTypeName,
                error.GetCode().IfNone("encina.unknown"));

            return ABACEnforcementVerdict.NotEnforced(error);
        }

        ABACLogMessages.EnforcementDenied(_logger, requestTypeName);

        return ABACEnforcementVerdict.Denied(error);
    }

    // ── Record (write-ahead) ────────────────────────────────────────

    private ABACDecisionRecord BuildRecord(
        TRequest request,
        IRequestContext context,
        RequestIdentity? caller,
        ABACCollectedAttributes? attributes,
        ABACRequirementVerdict? requirement,
        ABACEnforcementVerdict verdict,
        DateTimeOffset startedAtUtc)
    {
        if (requirement is { TraceTruncated: true })
        {
            ABACLogMessages.EvaluationTraceTruncated(_logger, typeof(TRequest).Name, _options.DecisionAudit.MaxTraceEntries);
        }

        return ABACDecisionRecordFactory.Create(new ABACDecisionInputs
        {
            Audit = _options.DecisionAudit,
            EnforcementMode = _options.EnforcementMode,
            RequestType = typeof(TRequest),
            Request = request,
            Context = context,
            Caller = caller,
            Attributes = attributes,
            Requirement = requirement,
            Verdict = verdict,
            StartedAtUtc = startedAtUtc,
            // One read: the same instant is the completion time and the timestamp of the entry (A9).
            CompletedAtUtc = _timeProvider.GetUtcNow()
        });
    }

    // Writes the record when the audit is enabled and the outcome is selected, and returns the error
    // the request must be denied with, or null when it proceeds.
    private async ValueTask<EncinaError?> RecordAsync(
        ABACEnforcementVerdict verdict,
        string requestTypeName,
        CancellationToken cancellationToken)
    {
        if (verdict.Record is not { } record || !IsSelected(verdict))
        {
            return verdict.Error;
        }

        // A caller that is already gone gets no write; one that leaves during the write does not
        // abort it (the write is never linked to the client's token, A3).
        cancellationToken.ThrowIfCancellationRequested();
        var failure = await WriteAsync(record, requestTypeName).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (failure is null)
        {
            ABACLogMessages.DecisionRecorded(_logger, requestTypeName, verdict.Enforced.ToString(), verdict.ReasonCode);
            return verdict.Error;
        }

        return OnWriteFailed(verdict, requestTypeName, failure);
    }

    private bool IsSelected(ABACEnforcementVerdict verdict) =>
        verdict.AlwaysRecorded || _options.DecisionAudit.Outcomes.HasFlag(OutcomeFlag(verdict.Enforced));

    // crap-exempt: single-question switch — the Outcomes flag each enforced outcome is filtered by.
    private static ABACDecisionAuditOutcomes OutcomeFlag(ABACEnforcedOutcome enforced) => enforced switch
    {
        ABACEnforcedOutcome.Granted => ABACDecisionAuditOutcomes.Granted,
        ABACEnforcedOutcome.Denied => ABACDecisionAuditOutcomes.Denied,
        _ => ABACDecisionAuditOutcomes.NotEnforced
    };

    // Returns null when the record was written, otherwise the error code (or exception type name) of
    // the failure: never a message. A recorder that throws is a failed write, not a crash.
    private async ValueTask<string?> WriteAsync(ABACDecisionRecord record, string requestTypeName)
    {
        try
        {
            var result = await _decisionRecorder.RecordAsync(record, CancellationToken.None).ConfigureAwait(false);

            // MatchUnsafe: a written record is represented by null.
            return result.MatchUnsafe<string?>(
                Left: error => error.GetCode().IfNone("encina.unknown"),
                Right: _ => null);
        }
        catch (Exception ex)
        {
            ABACLogMessages.DecisionRecorderThrew(_logger, ex.ForLogging(), requestTypeName);
            return ex.GetType().Name;
        }
    }

    // A request that would proceed cannot without its evidence under FailClosed; a request that is
    // denied anyway stays denied, with the original error.
    private EncinaError? OnWriteFailed(ABACEnforcementVerdict verdict, string requestTypeName, string failure)
    {
        if (!verdict.Allow)
        {
            ABACLogMessages.DecisionAuditFailedForDeniedRequest(_logger, requestTypeName, failure);
            return verdict.Error;
        }

        if (_options.DecisionAudit.FailureMode == ABACDecisionAuditFailureMode.FailClosed)
        {
            ABACLogMessages.DecisionAuditFailedAccessDenied(_logger, requestTypeName, failure);
            return ABACErrors.DecisionAuditFailed(typeof(TRequest), failure);
        }

        ABACLogMessages.DecisionAuditFailedProceeding(_logger, requestTypeName, failure);
        return null;
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
