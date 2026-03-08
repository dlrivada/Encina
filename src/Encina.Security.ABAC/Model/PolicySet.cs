namespace Encina.Security.ABAC;

/// <summary>
/// Represents an XACML 3.0 PolicySet — a hierarchical container that groups
/// <see cref="Policy"/> and nested <see cref="PolicySet"/> elements under a single
/// combining algorithm.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 §7.11 — A PolicySet is the top-level authorization unit in the XACML
/// policy hierarchy. It can contain both individual policies and nested policy sets,
/// enabling multi-level authorization architectures (e.g., organization-level → department-level
/// → application-level policies).
/// </para>
/// <para>
/// The <see cref="Algorithm"/> determines how the combined effects of child policies
/// and policy sets are aggregated. Policy-level and policy-set-level obligations and advice
/// are propagated upward through the hierarchy according to XACML combining semantics.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var policySet = new PolicySet
/// {
///     Id = "organization-policies",
///     Version = "1.0",
///     Description = "Top-level organizational access policies",
///     Policies = [financePolicy, hrPolicy],
///     PolicySets = [departmentPolicySets],
///     Algorithm = CombiningAlgorithmId.DenyOverrides,
///     Obligations = [],
///     Advice = []
/// };
/// </code>
/// </example>
public sealed record PolicySet
{
    /// <summary>
    /// The unique identifier for this policy set.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The optional version identifier for this policy set.
    /// </summary>
    /// <remarks>
    /// Follows XACML 3.0 version matching pattern (e.g., <c>"1.0"</c>, <c>"2.1.3"</c>).
    /// </remarks>
    public string? Version { get; init; }

    /// <summary>
    /// An optional human-readable description of what this policy set controls.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The optional target that determines whether this policy set applies to the request.
    /// </summary>
    /// <remarks>
    /// A <c>null</c> target means the policy set applies to all requests (unconditional).
    /// </remarks>
    public Target? Target { get; init; }

    /// <summary>
    /// The policies contained directly in this policy set.
    /// </summary>
    public required IReadOnlyList<Policy> Policies { get; init; }

    /// <summary>
    /// Nested policy sets contained in this policy set, enabling hierarchical policy structures.
    /// </summary>
    /// <remarks>
    /// XACML 3.0 §7.11 — PolicySets can recursively contain other PolicySets,
    /// allowing arbitrarily deep policy hierarchies (e.g., organization → department → team).
    /// </remarks>
    public required IReadOnlyList<PolicySet> PolicySets { get; init; }

    /// <summary>
    /// The combining algorithm used to combine the effects of contained policies and policy sets.
    /// </summary>
    /// <remarks>
    /// XACML 3.0 Appendix C — Determines how individual policy and policy set effects
    /// are aggregated into a single decision for this policy set.
    /// </remarks>
    public required CombiningAlgorithmId Algorithm { get; init; }

    /// <summary>
    /// Obligations to include in the response when this policy set's effect matches the decision.
    /// </summary>
    public required IReadOnlyList<Obligation> Obligations { get; init; }

    /// <summary>
    /// Advice expressions to include in the response when this policy set's effect matches the decision.
    /// </summary>
    public required IReadOnlyList<AdviceExpression> Advice { get; init; }

    /// <summary>
    /// Whether this policy set is enabled for evaluation.
    /// </summary>
    /// <remarks>
    /// Disabled policy sets are skipped during evaluation, producing <see cref="Effect.NotApplicable"/>.
    /// Defaults to <c>true</c>.
    /// </remarks>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// The evaluation priority when used with ordered combining algorithms.
    /// </summary>
    /// <remarks>
    /// Lower values indicate higher priority. Used by <see cref="CombiningAlgorithmId.FirstApplicable"/>
    /// and ordered variants. Defaults to <c>0</c>.
    /// </remarks>
    public int Priority { get; init; }
}
