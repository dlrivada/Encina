using System.Reflection;
using System.Reflection.Emit;

namespace Encina.Testing.Modules;

/// <summary>
/// A simple mock builder for module API interfaces.
/// </summary>
/// <typeparam name="TModuleApi">
/// The module API interface type. <b>Must be an interface type.</b>
/// A runtime check is performed and an exception will be thrown if a non-interface type is used.
/// </typeparam>
/// <remarks>
/// <para>
/// This provides a lightweight way to mock module dependencies without requiring
/// a full mocking framework. For complex scenarios, consider using Moq or NSubstitute.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var mock = new MockModuleApi&lt;IInventoryModuleApi&gt;();
/// mock.Setup(nameof(IInventoryModuleApi.ReserveStockAsync),
///     (ReserveStockRequest request) =&gt;
///         Task.FromResult(Right&lt;EncinaError, ReservationId&gt;(new ReservationId("res-1"))));
/// var api = mock.Build();
/// </code>
/// </example>
public sealed class MockModuleApi<TModuleApi> where TModuleApi : class
{
    private readonly Dictionary<string, Delegate> _methodSetups = new(StringComparer.Ordinal);
    private readonly Dictionary<string, object?> _propertyValues = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="MockModuleApi{TModuleApi}"/> class.
    /// Throws <see cref="ArgumentException"/> if <typeparamref name="TModuleApi"/> is not an interface.
    /// </summary>
    public MockModuleApi()
    {
        if (!typeof(TModuleApi).IsInterface)
        {
            throw new ArgumentException($"TModuleApi must be an interface type. {typeof(TModuleApi).FullName} is not an interface.", nameof(TModuleApi));
        }
    }

    /// <summary>
    /// Sets up a method with the specified return value.
    /// </summary>
    /// <param name="methodName">The method name.</param>
    /// <param name="implementation">The implementation delegate.</param>
    /// <returns>This mock for chaining.</returns>
    public MockModuleApi<TModuleApi> Setup(string methodName, Delegate implementation)
    {
        ArgumentNullException.ThrowIfNull(methodName);
        ArgumentNullException.ThrowIfNull(implementation);

        // Validate that the method exists on the API and that the provided delegate
        // appears compatible with the method's signature to fail fast on misconfiguration.
        var apiType = typeof(TModuleApi);
        var methods = apiType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => string.Equals(m.Name, methodName, StringComparison.Ordinal))
            .ToArray();

        if (methods.Length == 0)
        {
            throw new InvalidOperationException(
                $"No method named '{methodName}' was found on {apiType.FullName}.");
        }

        var implMethod = implementation.Method;
        var implParams = implMethod.GetParameters().Select(p => p.ParameterType).ToArray();
        var implReturn = implMethod.ReturnType;

        bool anyMatch = methods.Any(m =>
        {
            var mParams = m.GetParameters().Select(p => p.ParameterType).ToArray();
            if (mParams.Length != implParams.Length) return false;

            for (int i = 0; i < mParams.Length; i++)
            {
                var mp = mParams[i];
                var ip = implParams[i];
                // Contravariant parameter compatibility: implementation parameter type
                // should be assignable to the method parameter type (best-effort).
                if (!(mp == ip || mp.IsAssignableFrom(ip)))
                    return false;
            }

            var mr = m.ReturnType;
            // Covariant return compatibility: the method's declared return type
            // must be assignable from the implementation's return type (i.e.,
            // callers expecting the declared return can receive the implementation value).
            if (!mr.IsAssignableFrom(implReturn))
                return false;

            return true;
        });

        if (!anyMatch)
        {
            var candidates = string.Join("; ", methods.Select(m => m.ToString()));
            throw new InvalidOperationException(
                $"No compatible overload of '{methodName}' was found on {apiType.FullName}. " +
                $"Provided delegate has signature '{implMethod}'. Candidates: {candidates}.");
        }

        _methodSetups[methodName] = implementation;
        return this;
    }

    /// <summary>
    /// Sets up a property with the specified value.
    /// </summary>
    /// <param name="propertyName">The property name.</param>
    /// <param name="value">The value to return.</param>
    /// <returns>This mock for chaining.</returns>
    public MockModuleApi<TModuleApi> SetupProperty(string propertyName, object? value)
    {
        ArgumentNullException.ThrowIfNull(propertyName);
        _propertyValues[propertyName] = value;
        return this;
    }

    /// <summary>
    /// Builds the mock instance.
    /// </summary>
    /// <returns>A proxy implementing the module API interface.</returns>
    public TModuleApi Build()
    {
        var apiType = typeof(TModuleApi);
        if (!apiType.IsInterface)
        {
            throw new InvalidOperationException(
                $"MockModuleApi requires an interface type for TModuleApi. {apiType.FullName} is not an interface.");
        }

        // For interfaces, we use a simple dispatch proxy approach
        // In a real scenario, you might want to use Castle.Core or similar
        return MockProxy<TModuleApi>.Create(_methodSetups, _propertyValues);
    }
}

/// <summary>
/// A dispatch proxy for creating mock implementations.
/// </summary>
/// <typeparam name="T">The interface type to mock.</typeparam>
/// <remarks>
/// <para>
/// This proxy invokes stored delegates using <c>Delegate.DynamicInvoke</c> inside
/// the <c>Invoke</c> method. That approach is intentionally lightweight
/// but has trade-offs:
/// </para>
/// <list type="bullet">
/// <item><description>Performance: <c>DynamicInvoke</c> uses reflection and boxing which is slower than direct delegate invocation.</description></item>
/// <item><description>Type safety: runtime invocation loses compile-time type checks and may throw <c>TargetParameterCountException</c>, <c>ArgumentException</c> or other invocation errors if signatures mismatch.</description></item>
/// </list>
/// <para>
/// If you need stronger performance or compile-time safety consider storing typed delegates
/// (e.g., <c>Func&lt;TParam, TResult&gt;</c>) or generating strongly-typed invocation stubs instead
/// of relying on <c>DynamicInvoke</c>. Another option is to use a full-featured mocking library
/// such as Moq or NSubstitute which can provide optimized proxies and better ergonomics.
/// </para>
/// </remarks>
#pragma warning disable CA1852 // Type can be sealed - DispatchProxy requires non-sealed types
internal class MockProxy<T> : DispatchProxy where T : class
#pragma warning restore CA1852
{
    private Dictionary<string, Delegate> _methodSetups = new();
    private Dictionary<string, object?> _propertyValues = new();

    /// <summary>
    /// Creates a mock proxy instance.
    /// </summary>
    /// <param name="methodSetups">The method implementations.</param>
    /// <param name="propertyValues">The property values.</param>
    /// <returns>A proxy implementing the interface.</returns>
    public static T Create(
        Dictionary<string, Delegate> methodSetups,
        Dictionary<string, object?> propertyValues)
    {
        var proxy = Create<T, MockProxy<T>>();
        var mockProxy = (MockProxy<T>)(object)proxy;
        mockProxy._methodSetups = methodSetups;
        mockProxy._propertyValues = propertyValues;
        return proxy;
    }

    /// <inheritdoc />
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod is null)
        {
            throw new InvalidOperationException("Target method is null.");
        }

        var methodName = targetMethod.Name;

        // Handle property getters (ensure this is a compiler-generated property accessor)
        if (targetMethod.IsSpecialName && methodName.StartsWith("get_", StringComparison.Ordinal))
        {
            var propertyName = methodName[4..];
            if (_propertyValues.TryGetValue(propertyName, out var value))
            {
                return value;
            }

            throw new NotImplementedException(
                $"Property '{propertyName}' on {typeof(T).Name} was not setup. " +
                $"Use SetupProperty(\"{propertyName}\", value) to configure it.");
        }

        // Handle property setters (ensure this is a compiler-generated property accessor)
        if (targetMethod.IsSpecialName && methodName.StartsWith("set_", StringComparison.Ordinal))
        {
            var propertyName = methodName[4..];
            _propertyValues[propertyName] = args?[0];
            return null;
        }

        // Handle methods
        if (_methodSetups.TryGetValue(methodName, out var implementation))
        {
            return implementation.DynamicInvoke(args);
        }

        throw new NotImplementedException(
            $"Method '{methodName}' on {typeof(T).Name} was not setup. " +
            $"Use Setup(\"{methodName}\", implementation) to configure it.");
    }
}
