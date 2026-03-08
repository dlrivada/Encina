namespace Encina.Security.ABAC;

/// <summary>
/// Represents an XACML 3.0 AllOf element — a conjunction (AND) of <see cref="Match"/> elements
/// within an <see cref="AnyOf"/> element.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 §7.6 — An AllOf element matches if <em>all</em> of its <see cref="Match"/>
/// child elements match (logical AND). This is the innermost level of the Target's
/// triple-nesting structure.
/// </para>
/// <para>
/// Within the Target hierarchy: <c>Target</c> (AND of AnyOf) → <c>AnyOf</c> (OR of AllOf)
/// → <c>AllOf</c> (AND of Match).
/// </para>
/// </remarks>
public sealed record AllOf
{
    /// <summary>
    /// The list of <see cref="Match"/> elements that must all match (logical AND).
    /// </summary>
    public required IReadOnlyList<Match> Matches { get; init; }
}
