namespace Encina.Security.ABAC;

/// <summary>
/// Represents an XACML 3.0 Target — a structured matching predicate that determines
/// whether a policy, policy set, or rule applies to a given access request.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 §7.6 — A Target uses a triple-nesting structure: the Target contains
/// <see cref="AnyOf"/> elements (all must match — logical AND), each <see cref="AnyOf"/>
/// contains <see cref="AllOf"/> elements (any can match — logical OR), and each
/// <see cref="AllOf"/> contains <see cref="Match"/> elements (all must match — logical AND).
/// </para>
/// <para>
/// An empty <see cref="AnyOfElements"/> list means the Target matches all requests
/// (unconditional applicability).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Target: subject.role == "Admin" AND resource.classification == "Secret"
/// var target = new Target
/// {
///     AnyOfElements =
///     [
///         new AnyOf { AllOfElements = [new AllOf { Matches = [roleMatch] }] },
///         new AnyOf { AllOfElements = [new AllOf { Matches = [classificationMatch] }] }
///     ]
/// };
/// </code>
/// </example>
public sealed record Target
{
    /// <summary>
    /// The list of <see cref="AnyOf"/> elements that must all match for the target to apply (logical AND).
    /// </summary>
    /// <remarks>
    /// An empty list means the target matches all requests (unconditional).
    /// Each <see cref="AnyOf"/> element represents a constraint; all constraints must be satisfied.
    /// </remarks>
    public required IReadOnlyList<AnyOf> AnyOfElements { get; init; }
}
