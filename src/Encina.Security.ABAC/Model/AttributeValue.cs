namespace Encina.Security.ABAC;

/// <summary>
/// Represents an XACML 3.0 AttributeValue — a typed literal value used in
/// <see cref="Match"/> elements and <see cref="Apply"/> function arguments.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 §7.3.1 — An AttributeValue carries both the value and its data type.
/// The data type is used by functions in the <c>IFunctionRegistry</c> to validate
/// arguments and perform type-appropriate comparisons.
/// </para>
/// <para>
/// An AttributeValue with a <c>null</c> <see cref="Value"/> represents an absent or
/// undefined value, which is distinct from an empty string or zero.
/// </para>
/// </remarks>
public sealed record AttributeValue : IExpression
{
    /// <summary>
    /// The data type identifier of the value, one of the <see cref="XACMLDataTypes"/> constants
    /// (e.g., <see cref="XACMLDataTypes.String"/>, <see cref="XACMLDataTypes.Integer"/>).
    /// </summary>
    /// <remarks>
    /// An <see cref="AttributeDesignator"/> selects a stored value only when this identifier equals
    /// its <see cref="AttributeDesignator.DataType"/> exactly, so use the full URI constants of
    /// <see cref="XACMLDataTypes"/>, never short names such as <c>"string"</c>.
    /// </remarks>
    public required string DataType { get; init; }

    /// <summary>
    /// The literal value, boxed as <see cref="object"/>.
    /// </summary>
    /// <remarks>
    /// The runtime type should be compatible with the declared <see cref="DataType"/>.
    /// Functions validate type compatibility at evaluation time.
    /// </remarks>
    public object? Value { get; init; }
}
