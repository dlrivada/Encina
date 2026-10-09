using LanguageExt;

namespace Encina.Security.ABAC.DecisionAudit;

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
internal sealed record ABACEnforcementVerdict(
    bool Allow,
    EncinaError? Error,
    ABACEnforcedOutcome Enforced,
    string ReasonCode,
    bool AlwaysRecorded)
{
    /// <summary>The record of the decision; <c>null</c> when the decision audit is disabled.</summary>
    public ABACDecisionRecord? Record { get; init; }

    /// <summary>The request is permitted.</summary>
    public static ABACEnforcementVerdict Granted() =>
        new(true, null, ABACEnforcedOutcome.Granted, ABACDecisionAuditSchema.PermitReasonCode, false);

    /// <summary>The request is denied with <paramref name="error"/>; its code is the reason code.</summary>
    public static ABACEnforcementVerdict Denied(EncinaError error, bool alwaysRecorded = false) =>
        new(false, error, ABACEnforcedOutcome.Denied, ReasonOf(error), alwaysRecorded);

    /// <summary>A definite denial that Warn mode lets through; <paramref name="error"/> is what Block mode would return.</summary>
    public static ABACEnforcementVerdict NotEnforced(EncinaError error) =>
        new(true, null, ABACEnforcedOutcome.DeniedNotEnforced, ReasonOf(error), false);

    private static string ReasonOf(EncinaError error) => error.GetCode().IfNone("encina.unknown");
}
