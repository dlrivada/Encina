namespace Encina.Security.ABAC;

/// <summary>
/// Represents an XACML 3.0 Policy — a named container of rules that are combined
/// using a specified combining algorithm to produce a single authorization decision.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 §7.10 — A Policy groups related <see cref="Rule"/> elements under a common
/// <see cref="Target"/> and combines their individual effects using the specified
/// <see cref="Algorithm"/>. Policies can define <see cref="VariableDefinitions"/>
/// for reusable sub-expressions and carry <see cref="Obligations"/> and <see cref="Advice"/>
/// at the policy level.
/// </para>
/// <para>
/// A Policy can exist standalone or as a child of a <see cref="PolicySet"/>.
/// The <see cref="IsEnabled"/> flag allows policies to be temporarily disabled
/// without removing them from the hierarchy.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var policy = new Policy
/// {
///     Id = "finance-access-policy",
///     Version = "1.0",
///     Description = "Controls access to financial resources",
///     Target = financialResourceTarget,
///     Rules = [allowFinanceReadRule, denyExternalWriteRule],
///     Algorithm = CombiningAlgorithmId.DenyOverrides,
///     Obligations = [auditObligation],
///     Advice = [],
///     VariableDefinitions = [isHighValueVar]
/// };
/// </code>
/// </example>
public sealed record Policy
{
    /// <summary>
    /// The unique identifier for this policy.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The optional version identifier for this policy.
    /// </summary>
    /// <remarks>
    /// Follows XACML 3.0 version matching pattern (e.g., <c>"1.0"</c>, <c>"2.1.3"</c>).
    /// Used by the PAP for version-aware policy management.
    /// </remarks>
    public string? Version { get; init; }

    /// <summary>
    /// An optional human-readable description of what this policy controls.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The optional target that determines whether this policy applies to the request.
    /// </summary>
    /// <remarks>
    /// A <c>null</c> target means the policy applies to all requests (unconditional).
    /// </remarks>
    public Target? Target { get; init; }

    /// <summary>
    /// The rules contained in this policy, evaluated using the <see cref="Algorithm"/>.
    /// </summary>
    public required IReadOnlyList<Rule> Rules { get; init; }

    /// <summary>
    /// The combining algorithm used to combine the effects of the contained rules.
    /// </summary>
    /// <remarks>
    /// XACML 3.0 Appendix C — Determines how individual rule effects are aggregated
    /// into a single policy decision.
    /// </remarks>
    public required CombiningAlgorithmId Algorithm { get; init; }

    /// <summary>
    /// Obligations to include in the response when this policy's effect matches the decision.
    /// </summary>
    public required IReadOnlyList<Obligation> Obligations { get; init; }

    /// <summary>
    /// Advice expressions to include in the response when this policy's effect matches the decision.
    /// </summary>
    public required IReadOnlyList<AdviceExpression> Advice { get; init; }

    /// <summary>
    /// Reusable sub-expressions defined within this policy scope.
    /// </summary>
    /// <remarks>
    /// XACML 3.0 §7.8 — Variables are scoped to this policy and can be referenced
    /// from rule conditions via <see cref="VariableReference"/>.
    /// </remarks>
    public required IReadOnlyList<VariableDefinition> VariableDefinitions { get; init; }

    /// <summary>
    /// Whether this policy is enabled for evaluation.
    /// </summary>
    /// <remarks>
    /// Disabled policies are skipped during evaluation, producing <see cref="Effect.NotApplicable"/>.
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
