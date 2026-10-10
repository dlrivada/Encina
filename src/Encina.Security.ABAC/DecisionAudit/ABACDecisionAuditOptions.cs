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

    /// <summary>
    /// Gets or sets how long one decision record write may take before it counts as failed.
    /// Default is 5 seconds.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The bound holds even for a store that ignores its cancellation token: the write is raced
    /// against this timeout. A write that fails or times out is checked once more, under a second bound
    /// of the same length, for a committed entry with the decision id, so a slow store that did persist
    /// the record does not deny the request; otherwise it is a failed write and <see cref="FailureMode"/>
    /// applies. A failed write therefore holds the request for at most twice this value.
    /// </para>
    /// <para>
    /// The value must be greater than zero and is validated when the application starts.
    /// </para>
    /// </remarks>
    public TimeSpan WriteTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets how long a failed decision audit write keeps <see cref="Health.ABACHealthCheck"/>
    /// Unhealthy under <see cref="ABACDecisionAuditFailureMode.FailClosed"/>. Default is 5 minutes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// There is no write probe: the health state changes only when a decision is audited. A failure
    /// older than this window, with no successful write since, reads as Degraded instead of Unhealthy,
    /// so a service that stopped receiving audited traffic does not stay Unhealthy forever. A
    /// successful write clears the failure at once.
    /// </para>
    /// <para>The value must be greater than zero and is validated when the application starts.</para>
    /// </remarks>
    public TimeSpan HealthFailureWindow { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets whether the decision audit reader may run without a tenant in a multi-tenant
    /// application. Default is <c>false</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When multi-tenancy is enabled (a multi-tenancy package registered
    /// <see cref="MultiTenancyMarker"/>) and the request carries no tenant, the reader denies the
    /// query. This option is the explicit opt-out for operator tooling that reads the trail of every
    /// tenant; each query it lets through is logged. An ambient tenant is always forced, whatever
    /// this option says. Single-tenant applications need no configuration.
    /// </para>
    /// </remarks>
    public bool AllowCrossTenantQueries { get; set; }
}
