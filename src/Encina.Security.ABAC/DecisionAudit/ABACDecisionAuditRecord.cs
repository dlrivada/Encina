using System.Text;

using Encina.Security.Audit;

namespace Encina.Security.ABAC.DecisionAudit;

/// <summary>
/// One stored ABAC decision as the decision audit reader returns it: the typed view of an
/// operation audit entry whose action is <see cref="ABACDecisionAuditSchema.Action"/>.
/// </summary>
/// <remarks>
/// <para>
/// The values are those stored. An identifier that exceeded its column limit is stored as
/// <c>sha256:&lt;64 hex&gt;</c> and listed in <see cref="HashedFields"/>; an unusable client address
/// is stored as <c>null</c> and listed in <see cref="DroppedFields"/>; a truncated value is listed
/// in <see cref="TruncatedFields"/>.
/// </para>
/// <para>
/// The record carries reason codes, identifiers and attribute names, never an error or exception
/// message. It is also the shape of one line of the JSON Lines export (<see cref="Schema"/>).
/// </para>
/// </remarks>
public sealed record ABACDecisionAuditRecord
{
    /// <summary>The schema of the record shape and of the export line: <see cref="ABACDecisionAuditSchema.SchemaVersion"/>.</summary>
    public string Schema { get; } = ABACDecisionAuditSchema.SchemaVersion;

    /// <summary>The id of the decision (the id of the audit entry).</summary>
    public required Guid DecisionId { get; init; }

    /// <summary>The correlation id of the request; it joins this decision to the other audit rows of the request.</summary>
    public required string CorrelationId { get; init; }

    /// <summary>The caller's user id or <c>service:&lt;name&gt;</c>; <c>null</c> for an unauthenticated caller.</summary>
    public string? UserId { get; init; }

    /// <summary>The kind of the caller, when stored.</summary>
    public IdentityKind? IdentityKind { get; init; }

    /// <summary>The tenant of the request, when it carried one.</summary>
    public string? TenantId { get; init; }

    /// <summary>The module the request ran in, when module isolation is used.</summary>
    public string? ModuleId { get; init; }

    /// <summary>The name of the request type (the XACML action).</summary>
    public required string RequestType { get; init; }

    /// <summary>The declared id of the accessed resource; <c>null</c> when the request declared none.</summary>
    public string? ResourceId { get; init; }

    /// <summary>
    /// The stored audit outcome: <see cref="AuditOutcome.Success"/> when ABAC let the request
    /// through (including a would-deny in Warn mode, see <see cref="Enforced"/>),
    /// <see cref="AuditOutcome.Denied"/> for a denial, <see cref="AuditOutcome.Error"/> for an
    /// indeterminate decision or an evaluation failure.
    /// </summary>
    public required AuditOutcome Outcome { get; init; }

    /// <summary>The reason code of the outcome (an <see cref="ABACErrors"/> code, or <see cref="ABACDecisionAuditSchema.PermitReasonCode"/>).</summary>
    public string? ReasonCode { get; init; }

    /// <summary><c>false</c> for a definite denial that Warn mode let through; <c>true</c> otherwise.</summary>
    public required bool Enforced { get; init; }

    /// <summary>The enforcement mode in force when the decision was made, when stored.</summary>
    public ABACEnforcementMode? EnforcementMode { get; init; }

    /// <summary>The effect of the requirement evaluation; <c>null</c> when no evaluation was reached.</summary>
    public Effect? Effect { get; init; }

    /// <summary>The deciding policy, or <c>condition:&lt;index&gt;</c> for an unmet condition.</summary>
    public string? PolicyId { get; init; }

    /// <summary>The representative decisive rule of <see cref="PolicyId"/>.</summary>
    public string? RuleId { get; init; }

    /// <summary>The evaluation trace, in declaration order; empty when not traced.</summary>
    public IReadOnlyList<PolicyEvaluationTrace> EvaluatedPolicies { get; init; } = [];

    /// <summary><c>true</c> when the trace stopped at the trace entry limit.</summary>
    public bool TraceTruncated { get; init; }

    /// <summary>The obligation ids the decision carried.</summary>
    public IReadOnlyList<string> ObligationIds { get; init; } = [];

    /// <summary>The advice ids the decision carried.</summary>
    public IReadOnlyList<string> AdviceIds { get; init; } = [];

    /// <summary>The attribute names the providers supplied, keyed by category name.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> AttributeNames { get; init; } =
        new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>The allow-listed attribute values, keyed by attribute name.</summary>
    public IReadOnlyDictionary<string, string> RecordedValues { get; init; } = new Dictionary<string, string>();

    /// <summary>The client address, when known and valid.</summary>
    public string? IpAddress { get; init; }

    /// <summary>The client user agent, when known (at most 512 characters).</summary>
    public string? UserAgent { get; init; }

    /// <summary>The entry fields stored as a hash because they exceeded their column limit.</summary>
    public IReadOnlyList<string> HashedFields { get; init; } = [];

    /// <summary>The entry fields stored as <c>null</c> because they could not be bounded without misleading.</summary>
    public IReadOnlyList<string> DroppedFields { get; init; } = [];

    /// <summary>The entry fields or lists truncated to their limit.</summary>
    public IReadOnlyList<string> TruncatedFields { get; init; } = [];

    /// <summary>When the decision started.</summary>
    public required DateTimeOffset StartedAtUtc { get; init; }

    /// <summary>When the decision completed.</summary>
    public required DateTimeOffset CompletedAtUtc { get; init; }

    /// <summary>
    /// Prints only the decision id, the outcome and the reason code, so a log or diagnostic never
    /// receives the user, tenant, client address or recorded values.
    /// </summary>
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("DecisionId = ").Append(DecisionId)
            .Append(", Outcome = ").Append(Outcome)
            .Append(", ReasonCode = ").Append(ReasonCode);
        return true;
    }
}
