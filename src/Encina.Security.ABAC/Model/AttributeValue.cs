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
    /// The data type of the value (e.g., <c>"string"</c>, <c>"integer"</c>, <c>"boolean"</c>).
    /// </summary>
    /// <remarks>
    /// Should correspond to one of the XACML 3.0 data type identifiers. See <c>XACMLDataTypes</c>
    /// for the standard set of supported data types.
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
