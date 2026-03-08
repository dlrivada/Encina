namespace Encina.Security.ABAC;

/// <summary>
/// Identifies the eight standard combining algorithms defined by XACML 3.0 Appendix C.
/// </summary>
/// <remarks>
/// <para>
/// Combining algorithms determine how individual rule or policy evaluation results
/// are aggregated into a single authorization decision. Each algorithm implements
/// distinct conflict-resolution semantics appropriate for different authorization scenarios.
/// </para>
/// <para>
/// Algorithms apply at two levels: rule-combining within a <c>Policy</c>,
/// and policy-combining within a <c>PolicySet</c>. Both levels use the same set
/// of algorithm identifiers but operate on different input types
/// (<see cref="RuleEvaluationResult"/> vs <see cref="PolicyEvaluationResult"/>).
/// </para>
/// </remarks>
public enum CombiningAlgorithmId
{
    /// <summary>
    /// If any component returns <see cref="Effect.Deny"/>, the combined result is Deny,
    /// regardless of other results.
    /// </summary>
    /// <remarks>XACML 3.0 §C.1 — Deny takes precedence over Permit. Useful for mandatory access controls.</remarks>
    DenyOverrides,

    /// <summary>
    /// If any component returns <see cref="Effect.Permit"/>, the combined result is Permit,
    /// regardless of other results.
    /// </summary>
    /// <remarks>XACML 3.0 §C.2 — Permit takes precedence over Deny. Useful for discretionary access controls.</remarks>
    PermitOverrides,

    /// <summary>
    /// Returns the effect of the first applicable component. Components are evaluated
    /// in declaration order and the first applicable result is returned.
    /// </summary>
    /// <remarks>
    /// XACML 3.0 §C.3 — Order-sensitive; first match wins. Useful when policies have
    /// a natural priority ordering (e.g., specific overrides before general defaults).
    /// </remarks>
    FirstApplicable,

    /// <summary>
    /// Requires exactly one applicable component. If zero or more than one component
    /// is applicable, the result is <see cref="Effect.Indeterminate"/>.
    /// </summary>
    /// <remarks>
    /// XACML 3.0 §C.4 — Ensures no ambiguity in the decision. Useful for policy sets
    /// where overlapping applicability indicates a configuration error.
    /// </remarks>
    OnlyOneApplicable,

    /// <summary>
    /// If no component returns <see cref="Effect.Permit"/>, the combined result is Deny.
    /// Never returns <see cref="Effect.NotApplicable"/> or <see cref="Effect.Indeterminate"/>.
    /// </summary>
    /// <remarks>
    /// XACML 3.0 §C.5 — Default-deny unless explicitly permitted. The safest algorithm
    /// for security-critical systems where access should be denied by default.
    /// </remarks>
    DenyUnlessPermit,

    /// <summary>
    /// If no component returns <see cref="Effect.Deny"/>, the combined result is Permit.
    /// Never returns <see cref="Effect.NotApplicable"/> or <see cref="Effect.Indeterminate"/>.
    /// </summary>
    /// <remarks>
    /// XACML 3.0 §C.6 — Default-permit unless explicitly denied. Suitable for open systems
    /// where access is generally allowed and only specific denials are defined.
    /// </remarks>
    PermitUnlessDeny,

    /// <summary>
    /// Same as <see cref="DenyOverrides"/> but evaluates components in declaration order,
    /// which may affect obligation and advice collection.
    /// </summary>
    /// <remarks>
    /// XACML 3.0 §C.7 — Order-preserving variant of DenyOverrides. Guarantees deterministic
    /// obligation ordering when multiple policies are applicable.
    /// </remarks>
    OrderedDenyOverrides,

    /// <summary>
    /// Same as <see cref="PermitOverrides"/> but evaluates components in declaration order,
    /// which may affect obligation and advice collection.
    /// </summary>
    /// <remarks>
    /// XACML 3.0 §C.8 — Order-preserving variant of PermitOverrides. Guarantees deterministic
    /// obligation ordering when multiple policies are applicable.
    /// </remarks>
    OrderedPermitOverrides
}
