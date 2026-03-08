namespace Encina.Security.ABAC;

/// <summary>
/// Represents an XACML 3.0 VariableReference — a reference to a named
/// <see cref="VariableDefinition"/> within the same <see cref="Policy"/>.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 §7.8 — A VariableReference resolves to the value of the
/// <see cref="VariableDefinition"/> with the matching <see cref="VariableId"/>.
/// This enables reuse of common sub-expressions across multiple conditions
/// within the same policy without duplication.
/// </para>
/// <para>
/// The referenced <see cref="VariableDefinition"/> must exist in the containing
/// policy's <see cref="Policy.VariableDefinitions"/> collection. A reference to
/// an undefined variable produces an <see cref="Effect.Indeterminate"/> result.
/// </para>
/// </remarks>
public sealed record VariableReference : IExpression
{
    /// <summary>
    /// The identifier of the <see cref="VariableDefinition"/> to reference.
    /// </summary>
    /// <remarks>
    /// Must match the <see cref="VariableDefinition.VariableId"/> of a variable
    /// defined in the same policy.
    /// </remarks>
    public required string VariableId { get; init; }
}
