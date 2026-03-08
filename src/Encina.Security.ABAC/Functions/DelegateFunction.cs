namespace Encina.Security.ABAC;

/// <summary>
/// Internal wrapper that adapts a delegate to the <see cref="IXACMLFunction"/> interface.
/// </summary>
/// <remarks>
/// This avoids creating 100+ individual classes for standard XACML functions while
/// still implementing the <see cref="IXACMLFunction"/> contract. Each function is
/// registered as a <see cref="DelegateFunction"/> with its return type and evaluation logic.
/// </remarks>
internal sealed class DelegateFunction(
    string returnType,
    Func<IReadOnlyList<object?>, object?> evaluate) : IXACMLFunction
{
    /// <inheritdoc />
    public string ReturnType => returnType;

    /// <inheritdoc />
    public object? Evaluate(IReadOnlyList<object?> arguments) => evaluate(arguments);
}
