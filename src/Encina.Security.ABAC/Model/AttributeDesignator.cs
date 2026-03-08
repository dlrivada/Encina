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
///     DataType = "string",
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
    /// The expected data type of the attribute value (e.g., <c>"string"</c>, <c>"integer"</c>, <c>"boolean"</c>).
    /// </summary>
    /// <remarks>
    /// Should correspond to one of the XACML 3.0 data type identifiers. See <c>XACMLDataTypes</c>
    /// for the standard set of supported data types.
    /// </remarks>
    public required string DataType { get; init; }

    /// <summary>
    /// Whether the attribute must be present in the evaluation context.
    /// </summary>
    /// <remarks>
    /// XACML 3.0 §7.3.5 — When <c>true</c>, a missing attribute causes the overall
    /// evaluation to return <see cref="Effect.Indeterminate"/>. When <c>false</c> (the default),
    /// a missing attribute produces an empty bag and evaluation continues.
    /// </remarks>
    public bool MustBePresent { get; init; }
}
