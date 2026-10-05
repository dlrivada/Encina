using Microsoft.Extensions.DependencyInjection;

namespace Encina.Security.Audit;

/// <summary>
/// Implemented by a service factory that wraps another registration (a decorator), so registration
/// code can look through the wrapper and see what it decorates.
/// </summary>
/// <remarks>
/// A decorating registration is a factory descriptor whose <see cref="ServiceDescriptor.ImplementationFactory"/>
/// is a method of an object implementing this interface; the descriptor itself no longer names the
/// decorated implementation type.
/// </remarks>
public interface IDecoratedServiceFactory
{
    /// <summary>
    /// Gets the registration this factory decorates, as it was before it was wrapped.
    /// </summary>
    ServiceDescriptor Decorated { get; }
}

/// <summary>
/// Registration helpers shared by the database providers that supply an <see cref="IOperationAuditStore"/>.
/// </summary>
public static class OperationAuditStoreRegistration
{
    /// <summary>
    /// Removes the <see cref="InMemoryOperationAuditStore"/> default that <c>AddEncinaAudit</c> registers,
    /// including when a decorator (for example the OpenTelemetry instrumentation) wrapped it, so a database
    /// store registered afterwards with <c>TryAdd</c> wins in any registration order.
    /// </summary>
    /// <param name="services">The service collection to clean.</param>
    /// <remarks>
    /// A store registered by the application (including a decorated one) is never removed.
    /// The database store registered afterwards is not wrapped by a decorator that wrapped the removed
    /// default: register the decorator (for example <c>AddEncinaOpenTelemetry</c>) after the provider to
    /// instrument the database store.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static void RemoveInMemoryDefault(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        for (var i = services.Count - 1; i >= 0; i--)
        {
            if (IsInMemoryDefault(services[i]))
            {
                services.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Determines whether a registration is the <see cref="InMemoryOperationAuditStore"/> default,
    /// directly or through any number of decorators.
    /// </summary>
    /// <param name="descriptor">The registration to inspect.</param>
    /// <returns><c>true</c> when the registration (or what it decorates) is the in-memory default.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="descriptor"/> is null.</exception>
    public static bool IsInMemoryDefault(ServiceDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        // A keyed registration is never the default, and its implementation getters throw.
        if (descriptor.IsKeyedService || descriptor.ServiceType != typeof(IOperationAuditStore))
        {
            return false;
        }

        var current = descriptor;
        while (current.ImplementationFactory?.Target is IDecoratedServiceFactory decorating)
        {
            current = decorating.Decorated;
        }

        return current.ImplementationType == typeof(InMemoryOperationAuditStore);
    }
}
