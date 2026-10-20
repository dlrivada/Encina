namespace Encina.Security.ABAC;

/// <summary>
/// Represents an XACML 3.0 AttributeDesignator — a formal reference to an attribute
/// that should be resolved from the request context during policy evaluation.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 §7.3 — An AttributeDesignator identifies an attribute by its
/// <see cref="Category"/> (Subject, Resource, Environment, Action), <see cref="AttributeId"/>
/// (the attribute name), and <see cref="DataType"/> (the expected value type).
/// </para>
/// <para>
/// When <see cref="MustBePresent"/> is <c>true</c> and the attribute cannot be resolved,
/// the evaluation result is <see cref="Effect.Indeterminate"/>. When <c>false</c>,
/// a missing attribute produces an empty <see cref="AttributeBag"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var designator = new AttributeDesignator
/// {
///     Category = AttributeCategory.Subject,
///     AttributeId = "department",
///     DataType = XACMLDataTypes.String,
///     MustBePresent = true
/// };
/// </code>
/// </example>
public sealed record AttributeDesignator : IExpression
{
    /// <summary>
    /// The attribute category indicating the source of the attribute.
    /// </summary>
    public required AttributeCategory Category { get; init; }

    /// <summary>
    /// The identifier of the attribute to resolve (e.g., <c>"department"</c>, <c>"classification"</c>).
    /// </summary>
    public required string AttributeId { get; init; }

    /// <summary>
    /// The data type identifier of the values to select, one of the <see cref="XACMLDataTypes"/>
    /// constants (e.g., <see cref="XACMLDataTypes.String"/>, <see cref="XACMLDataTypes.Integer"/>).
    /// </summary>
    /// <remarks>
    /// XACML 3.0 §7.3.5 — The designator keeps only the values of its attribute whose
    /// <see cref="AttributeValue.DataType"/> equals this identifier exactly (ordinal comparison of
    /// the full URI; a short name such as <c>"string"</c> matches nothing). Values of any other
    /// data type are not seen: with <see cref="MustBePresent"/> <c>false</c> a mistyped designator
    /// silently yields an empty bag, so a Deny rule or target that depends on it stops applying
    /// (<see cref="Effect.NotApplicable"/>). Set <see cref="MustBePresent"/> to <c>true</c> on
    /// designators that Deny rules depend on.
    /// </remarks>
    public required string DataType { get; init; }

    /// <summary>
    /// Whether the attribute must be present in the evaluation context.
    /// </summary>
    /// <remarks>
    /// XACML 3.0 §7.3.5 — Presence is decided for this attribute only, after the
    /// <see cref="DataType"/> filter: an attribute that is absent, or has no value of the
    /// designator's data type, is missing. When <c>true</c>, a missing attribute causes the overall
    /// evaluation to return <see cref="Effect.Indeterminate"/> (which denies). When <c>false</c>
    /// (the default), a missing attribute produces an empty bag and evaluation continues, so a
    /// target match is <see cref="Effect.NotApplicable"/>; use <c>true</c> for attributes that
    /// Deny rules depend on.
    /// </remarks>
    public bool MustBePresent { get; init; }
}
