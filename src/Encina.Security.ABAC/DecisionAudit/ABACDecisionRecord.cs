namespace Encina.Security.ABAC.DecisionAudit;

/// <summary>
/// Everything the Policy Enforcement Point knows about one enforced decision: who asked for what,
/// which policies decided, what was enforced and why. One record is written per evaluated decision.
/// </summary>
/// <remarks>
/// <para>
/// The record holds codes, identifiers and attribute <em>names</em>. It never holds
/// <see cref="DecisionStatus.StatusMessage"/>, an <see cref="EncinaError.Message"/>, an exception
/// message, a request or response payload, or attribute values other than those the application
/// listed in <see cref="RecordedValues"/>.
/// </para>
/// <para>
/// The stored audit outcome is derived from <see cref="EnforcedOutcome"/> and <see cref="ReasonCode"/>
/// (the decision-path table of the implementation plan), never from message text.
/// </para>
/// </remarks>
public sealed record ABACDecisionRecord
{
    // ── Identity ─────────────────────────────────────────────────────

    /// <summary>The unique id of the decision; it becomes the id of the audit entry, which makes a retried write idempotent.</summary>
    public required Guid DecisionId { get; init; }

    /// <summary>
    /// The caller's user id, or <c>service:&lt;name&gt;</c> for a service identity; <c>null</c> when
    /// the caller is unauthenticated (the subject is unknown, only the reason code is stored).
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>The kind of the caller, read once from the request identity snapshot.</summary>
    public required IdentityKind IdentityKind { get; init; }

    // ── Context ──────────────────────────────────────────────────────

    /// <summary>The tenant of the request, when the request context carries one.</summary>
    public string? TenantId { get; init; }

    /// <summary>The correlation id of the request; it joins this record to the other audit rows of the request.</summary>
    public required string CorrelationId { get; init; }

    /// <summary>The module the request runs in, when module isolation is used.</summary>
    public string? ModuleId { get; init; }

    /// <summary>The client address of the request, when known.</summary>
    public string? IpAddress { get; init; }

    /// <summary>The client user agent of the request, when known.</summary>
    public string? UserAgent { get; init; }

    // ── Request ──────────────────────────────────────────────────────

    /// <summary>The full name of the request type: the XACML action of the decision.</summary>
    public required string RequestType { get; init; }

    /// <summary>
    /// The identifier of the accessed resource, from <see cref="IABACResourceIdentity"/> or the
    /// resource id attribute; <c>null</c> when the request declares none.
    /// </summary>
    public string? ResourceId { get; init; }

    // ── Decision ─────────────────────────────────────────────────────

    /// <summary>What the Policy Enforcement Point did with the decision.</summary>
    public required ABACEnforcedOutcome EnforcedOutcome { get; init; }

    /// <summary>
    /// The stable reason code of the outcome, taken from the <see cref="ABACErrors"/> code constants
    /// (for example <see cref="ABACErrors.AccessDeniedCode"/>) or the permit reason of the schema.
    /// </summary>
    public required string ReasonCode { get; init; }

    /// <summary>The enforcement mode in force when the decision was made.</summary>
    public required ABACEnforcementMode EnforcementMode { get; init; }

    /// <summary>
    /// The effect the Policy Decision Point answered, or <c>null</c> when no decision was reached
    /// (an exception, an unauthenticated caller, a condition decided without the PDP).
    /// </summary>
    public Effect? Effect { get; init; }

    /// <summary>
    /// The deciding policy: the first deciding policy in declaration order, or <c>condition:&lt;index&gt;</c>
    /// for a failed <see cref="RequireConditionAttribute"/> (never the expression text).
    /// </summary>
    public string? PolicyId { get; init; }

    /// <summary>The representative decisive rule of <see cref="PolicyId"/>, when traced.</summary>
    public string? RuleId { get; init; }

    /// <summary>The evaluation trace of every evaluated policy, in declaration order; empty when not traced.</summary>
    public IReadOnlyList<PolicyEvaluationTrace> EvaluatedPolicies { get; init; } = [];

    /// <summary><c>true</c> when the trace is incomplete because it reached the trace entry limit.</summary>
    public bool TraceTruncated { get; init; }

    /// <summary>The identifiers of the obligations the decision carried.</summary>
    public IReadOnlyList<string> ObligationIds { get; init; } = [];

    /// <summary>The identifiers of the advice the decision carried.</summary>
    public IReadOnlyList<string> AdviceIds { get; init; } = [];

    // ── Attributes ───────────────────────────────────────────────────

    /// <summary>
    /// The names of the attributes the providers supplied, per category. The built-in subject
    /// attributes (<c>subject-id</c> and <c>identity-kind</c>) are excluded so the names describe what
    /// the providers supplied; the identity kind is <see cref="IdentityKind"/>.
    /// </summary>
    public IReadOnlyDictionary<AttributeCategory, IReadOnlyList<string>> AttributeNames { get; init; } =
        new Dictionary<AttributeCategory, IReadOnlyList<string>>();

    /// <summary>
    /// The values of the attributes the application listed for recording, keyed by attribute name.
    /// Empty by default: values are personal data and are recorded only on an explicit allow-list.
    /// </summary>
    public IReadOnlyDictionary<string, string> RecordedValues { get; init; } =
        new Dictionary<string, string>();

    // ── Timing ───────────────────────────────────────────────────────

    /// <summary>When the decision started (read from the <see cref="TimeProvider"/> at the start).</summary>
    public required DateTimeOffset StartedAtUtc { get; init; }

    /// <summary>When the decision completed: one <see cref="TimeProvider"/> read at the end, also used for the entry timestamp.</summary>
    public required DateTimeOffset CompletedAtUtc { get; init; }
}
