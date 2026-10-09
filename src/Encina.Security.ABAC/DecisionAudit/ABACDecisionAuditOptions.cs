namespace Encina.Security.ABAC.DecisionAudit;

/// <summary>
/// Configuration of the ABAC decision audit trail: whether the Policy Enforcement Point records the
/// decisions it enforces, which outcomes it records and what it does when a record cannot be written.
/// </summary>
/// <remarks>
/// <para>
/// The audit is opt-in and off by default (<see cref="Enabled"/>). When it is off, nothing is built
/// per request: no record, no clock read, no evaluation trace and no recorder call.
/// </para>
/// <para>
/// Reach it through <see cref="ABACOptions.DecisionAudit"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaABAC(options =>
/// {
///     options.DecisionAudit.Enabled = true;
///     options.DecisionAudit.Outcomes = ABACDecisionAuditOutcomes.Denied | ABACDecisionAuditOutcomes.NotEnforced;
///     options.DecisionAudit.RecordedAttributeValues.Add("department");
/// });
/// </code>
/// </example>
public sealed class ABACDecisionAuditOptions
{
    /// <summary>
    /// Gets or sets whether the Policy Enforcement Point records the decisions it enforces.
    /// Default is <c>false</c>.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets which enforced outcomes are recorded. Default is <see cref="ABACDecisionAuditOutcomes.All"/>.
    /// </summary>
    /// <remarks>
    /// A filter never hides an error: an indeterminate decision, an evaluation failure and an
    /// unauthenticated caller are always recorded when the audit is enabled.
    /// </remarks>
    public ABACDecisionAuditOutcomes Outcomes { get; set; } = ABACDecisionAuditOutcomes.All;

    /// <summary>
    /// Gets or sets what the Policy Enforcement Point does when a record cannot be written.
    /// Default is <see cref="ABACDecisionAuditFailureMode.FailClosed"/>.
    /// </summary>
    public ABACDecisionAuditFailureMode FailureMode { get; set; } = ABACDecisionAuditFailureMode.FailClosed;

    /// <summary>
    /// Gets or sets whether the records carry the evaluation trace (which policies and conditions were
    /// evaluated, and why each decided as it did). Default is <c>true</c>.
    /// </summary>
    public bool IncludeEvaluationTrace { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of trace nodes (policies, policy sets and conditions) one
    /// record carries, for the whole decision. Default is 64.
    /// </summary>
    /// <remarks>Nodes past the limit are dropped and the record is marked as truncated.</remarks>
    public int MaxTraceEntries { get; set; } = 64;

    /// <summary>
    /// Gets or sets the name of the resource attribute that holds the identifier of the accessed
    /// resource, read when the request does not implement <see cref="IABACResourceIdentity"/>.
    /// Default is <c>resourceId</c>.
    /// </summary>
    /// <remarks>
    /// Only explicit ids are recorded. A request that declares neither leaves the entity id of its
    /// audit entry empty.
    /// </remarks>
    public string ResourceIdAttributeName { get; set; } = "resourceId";

    /// <summary>
    /// Gets the names of the attributes whose values are recorded. Empty by default: records carry
    /// attribute names only, because values are personal data.
    /// </summary>
    public HashSet<string> RecordedAttributeValues { get; } = new(StringComparer.Ordinal);
}
