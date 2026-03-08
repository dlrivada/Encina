using System.Collections.Concurrent;

namespace Encina.Security.ABAC;

/// <summary>
/// Default implementation of <see cref="IFunctionRegistry"/> that pre-registers
/// all standard XACML 3.0 functions.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 Appendix A — This registry contains all standard functions defined
/// in the XACML specification: equality, comparison, arithmetic, string manipulation,
/// logical, bag, set, higher-order, type conversion, and regular expression functions.
/// </para>
/// <para>
/// Custom functions can be added via <see cref="Register"/> after construction.
/// The registry is thread-safe and can be used concurrently during policy evaluation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var registry = new DefaultFunctionRegistry();
///
/// // Use a standard function
/// var fn = registry.GetFunction(XACMLFunctionIds.StringEqual);
/// var result = fn!.Evaluate(["admin", "admin"]); // true
///
/// // Register a custom function
/// registry.Register("custom-geo-within", new GeoWithinFunction());
/// </code>
/// </example>
public sealed class DefaultFunctionRegistry : IFunctionRegistry
{
    private readonly ConcurrentDictionary<string, IXACMLFunction> _functions = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultFunctionRegistry"/> class
    /// with all standard XACML 3.0 functions pre-registered.
    /// </summary>
    public DefaultFunctionRegistry()
    {
        EqualityFunctions.Register(this);
        ComparisonFunctions.Register(this);
        ArithmeticFunctions.Register(this);
        StringFunctions.Register(this);
        LogicalFunctions.Register(this);
        BagFunctions.Register(this);
        SetFunctions.Register(this);
        HigherOrderFunctions.Register(this);
        TypeConversionFunctions.Register(this);
        RegexFunctions.Register(this);
    }

    /// <inheritdoc />
    public IXACMLFunction? GetFunction(string functionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(functionId);
        return _functions.TryGetValue(functionId, out var fn) ? fn : null;
    }

    /// <inheritdoc />
    public void Register(string functionId, IXACMLFunction xacmlFunction)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(functionId);
        ArgumentNullException.ThrowIfNull(xacmlFunction);
        _functions[functionId] = xacmlFunction;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetAllFunctionIds()
    {
        var keys = _functions.Keys.ToList();
        keys.Sort(StringComparer.Ordinal);
        return keys;
    }

    /// <summary>
    /// Convenience method for registering a function using a delegate.
    /// </summary>
    /// <param name="functionId">The unique identifier for the function.</param>
    /// <param name="returnType">The XACML data type of the return value.</param>
    /// <param name="evaluate">The function evaluation delegate.</param>
    internal void RegisterFunction(
        string functionId,
        string returnType,
        Func<IReadOnlyList<object?>, object?> evaluate)
    {
        _functions[functionId] = new DelegateFunction(returnType, evaluate);
    }
}
