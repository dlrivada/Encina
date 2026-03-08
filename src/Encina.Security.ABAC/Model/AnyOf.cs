namespace Encina.Security.ABAC;

/// <summary>
/// Represents an XACML 3.0 AnyOf element — a disjunction (OR) of <see cref="AllOf"/> elements
/// within a <see cref="Target"/>.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 §7.6 — An AnyOf element matches if <em>any</em> of its <see cref="AllOf"/>
/// child elements matches (logical OR). This is the second level of the Target's
/// triple-nesting structure.
/// </para>
/// <para>
/// Within the Target hierarchy: <c>Target</c> (AND of AnyOf) → <c>AnyOf</c> (OR of AllOf)
/// → <c>AllOf</c> (AND of Match).
/// </para>
/// </remarks>
public sealed record AnyOf
{
    /// <summary>
    /// The list of <see cref="AllOf"/> elements where at least one must match (logical OR).
    /// </summary>
    public required IReadOnlyList<AllOf> AllOfElements { get; init; }
}
