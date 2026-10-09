using LanguageExt;

namespace Encina.Security.ABAC.DecisionAudit;

/// <summary>
/// What the Policy Enforcement Point reports to telemetry (span status, counters) for a decision once
/// it is enforced.
/// </summary>
/// <param name="Effect">Permit, Deny or Indeterminate.</param>
/// <param name="PolicyId">The policy that decided, when known.</param>
/// <param name="Reason">A fixed reason: a code or an exception type name, never a message.</param>
internal readonly record struct ABACOutcomeTelemetry(Effect Effect, string? PolicyId, string Reason);

/// <summary>
/// What the Policy Enforcement Point decided to do with one request, before it records the decision
/// and before it calls the handler: the result of the "decide" step.
/// </summary>
/// <param name="Allow">Whether the request proceeds to the handler (a permit, or a definite denial in Warn mode).</param>
/// <param name="Error">The error to return when the request does not proceed; <c>null</c> when it does.</param>
/// <param name="Enforced">What the Policy Enforcement Point did with the decision.</param>
/// <param name="ReasonCode">The stable reason code of the outcome, taken from the <see cref="ABACErrors"/> code constants.</param>
/// <param name="AlwaysRecorded">
/// <c>true</c> for failure paths (indeterminate, exception, unauthenticated caller): they are recorded
/// whatever <see cref="ABACDecisionAuditOptions.Outcomes"/> says, so a filter can never hide an error.
/// </param>
/// <param name="Telemetry">What span status and counters report once the outcome is final.</param>
internal sealed record ABACEnforcementVerdict(
    bool Allow,
    EncinaError? Error,
    ABACEnforcedOutcome Enforced,
    string ReasonCode,
    bool AlwaysRecorded,
    ABACOutcomeTelemetry Telemetry)
{
    /// <summary>The record of the decision; <c>null</c> when the decision audit is disabled.</summary>
    public ABACDecisionRecord? Record { get; init; }

    /// <summary>The request is permitted.</summary>
    public static ABACEnforcementVerdict Granted(string? policyId) =>
        new(true, null, ABACEnforcedOutcome.Granted, ABACDecisionAuditSchema.PermitReasonCode, false,
            new(Effect.Permit, policyId, string.Empty));

    /// <summary>The request is denied with <paramref name="error"/>; its code is the reason code.</summary>
    public static ABACEnforcementVerdict Denied(
        EncinaError error, ABACOutcomeTelemetry telemetry, bool alwaysRecorded = false) =>
        new(false, error, ABACEnforcedOutcome.Denied, ReasonOf(error), alwaysRecorded, telemetry);

    /// <summary>A definite denial that Warn mode lets through; <paramref name="error"/> is what Block mode would return.</summary>
    public static ABACEnforcementVerdict NotEnforced(EncinaError error, ABACOutcomeTelemetry telemetry) =>
        new(true, null, ABACEnforcedOutcome.DeniedNotEnforced, ReasonOf(error), false, telemetry);

    private static string ReasonOf(EncinaError error) => error.GetCode().IfNone("encina.unknown");
}
