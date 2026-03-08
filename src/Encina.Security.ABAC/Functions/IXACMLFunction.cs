namespace Encina.Security.ABAC;

/// <summary>
/// Represents an individual XACML function that can be invoked during
/// policy condition evaluation.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 Appendix A — Functions are the building blocks of XACML conditions.
/// They take a list of arguments (which can be results of other function evaluations,
/// attribute values, or bag contents) and produce a result.
/// </para>
/// <para>
/// Implementations must handle type validation and coercion internally.
/// If arguments are invalid, the function should throw an
/// <see cref="InvalidOperationException"/> with a descriptive message, which
/// the evaluation engine converts to an Indeterminate result.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class StringEqualFunction : IXACMLFunction
/// {
///     public string ReturnType => XACMLDataTypes.Boolean;
///
///     public object? Evaluate(IReadOnlyList&lt;object?&gt; arguments)
///     {
///         if (arguments.Count != 2)
///             throw new InvalidOperationException("string-equal requires exactly 2 arguments.");
///         return string.Equals(
///             arguments[0]?.ToString(),
///             arguments[1]?.ToString(),
///             StringComparison.Ordinal);
///     }
/// }
/// </code>
/// </example>
public interface IXACMLFunction
{
    /// <summary>
    /// Evaluates the function with the given arguments.
    /// </summary>
    /// <param name="arguments">
    /// The function arguments. These may be primitive values, <see cref="AttributeValue"/>
    /// instances, or results of other function evaluations. The number and types of
    /// arguments depend on the specific function.
    /// </param>
    /// <returns>
    /// The function result, or <c>null</c> if the function produces no value.
    /// Boolean functions return <see cref="bool"/>, numeric functions return
    /// <see cref="int"/> or <see cref="double"/>, etc.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the argument count or types are invalid.
    /// </exception>
    object? Evaluate(IReadOnlyList<object?> arguments);

    /// <summary>
    /// Gets the XACML data type of the value returned by this function.
    /// </summary>
    /// <value>
    /// A data type identifier from <see cref="XACMLDataTypes"/> (e.g.,
    /// <see cref="XACMLDataTypes.Boolean"/> for comparison functions).
    /// </value>
    string ReturnType { get; }
}
