namespace Encina.Security.ABAC;

/// <summary>
/// Represents an XACML 3.0 Match element — a comparison between an attribute value
/// from the request context and a literal value using a specified function.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 §7.6 — A Match compares the value of an <see cref="AttributeDesignator"/>
/// (resolved from the request context) with a literal <see cref="AttributeValue"/>
/// using the function identified by <see cref="FunctionId"/>.
/// </para>
/// <para>
/// The function must accept two arguments (the attribute value and the literal value)
/// and return a boolean. Common functions include <c>string-equal</c>, <c>integer-greater-than</c>,
/// and <c>string-regexp-match</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Match: subject.department == "Finance"
/// var match = new Match
/// {
///     FunctionId = "string-equal",
///     AttributeDesignator = new AttributeDesignator
///     {
///         Category = AttributeCategory.Subject,
///         AttributeId = "department",
///         DataType = "string"
///     },
///     AttributeValue = new AttributeValue
///     {
///         DataType = "string",
///         Value = "Finance"
///     }
/// };
/// </code>
/// </example>
public sealed record Match
{
    /// <summary>
    /// The identifier of the matching function to apply.
    /// </summary>
    /// <remarks>
    /// Must reference a function registered in the <c>IFunctionRegistry</c> that accepts
    /// two arguments and returns a boolean value.
    /// </remarks>
    public required string FunctionId { get; init; }

    /// <summary>
    /// The attribute designator that resolves the attribute value from the request context.
    /// </summary>
    public required AttributeDesignator AttributeDesignator { get; init; }

    /// <summary>
    /// The literal value to compare against the resolved attribute value.
    /// </summary>
    public required AttributeValue AttributeValue { get; init; }
}
