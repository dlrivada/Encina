namespace Encina.Security.ABAC;

/// <summary>
/// Registry for XACML functions used during policy condition evaluation.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 Appendix A — The function registry manages all available functions
/// that can be referenced by <see cref="Apply.FunctionId"/> in policy conditions.
/// It is pre-populated with standard XACML 3.0 functions and supports registration
/// of custom user-defined functions.
/// </para>
/// <para>
/// Implementations must be thread-safe, as the registry is accessed concurrently
/// during policy evaluation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register a custom function
/// registry.Register("custom-geo-distance", new GeoDistanceFunction());
///
/// // Retrieve a function
/// var fn = registry.GetFunction(XACMLFunctionIds.StringEqual);
/// var result = fn?.Evaluate(["admin", "admin"]); // true
/// </code>
/// </example>
public interface IFunctionRegistry
{
    /// <summary>
    /// Retrieves a function by its identifier.
    /// </summary>
    /// <param name="functionId">
    /// The function identifier (e.g., <see cref="XACMLFunctionIds.StringEqual"/>).
    /// </param>
    /// <returns>The function if registered; otherwise, <c>null</c>.</returns>
    IXACMLFunction? GetFunction(string functionId);

    /// <summary>
    /// Registers a function with the given identifier, replacing any existing registration.
    /// </summary>
    /// <param name="functionId">The unique identifier for the function.</param>
    /// <param name="xacmlFunction">The function implementation.</param>
    void Register(string functionId, IXACMLFunction xacmlFunction);

    /// <summary>
    /// Returns all registered function identifiers.
    /// </summary>
    /// <returns>A read-only list of all registered function IDs.</returns>
    IReadOnlyList<string> GetAllFunctionIds();
}
