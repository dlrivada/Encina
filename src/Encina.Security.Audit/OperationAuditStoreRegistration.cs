using Microsoft.Extensions.DependencyInjection;

namespace Encina.Security.Audit;

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

        return descriptor.ServiceType == typeof(IOperationAuditStore) && UnwrapsToInMemoryDefault(descriptor);
    }

    // Walks the decorator chain. Every level is checked against keyed registrations (a keyed registration is
    // never the default and its implementation getters throw), and a visited set stops a cyclic chain.
    private static bool UnwrapsToInMemoryDefault(ServiceDescriptor descriptor)
    {
        var visited = new HashSet<ServiceDescriptor>(ReferenceEqualityComparer.Instance);
        var current = descriptor;

        while (visited.Add(current))
        {
            if (current.IsKeyedService)
            {
                return false;
            }

            if (current.ImplementationFactory?.Target is not IDecoratedServiceFactory decorating)
            {
                return current.ImplementationType == typeof(InMemoryOperationAuditStore);
            }

            current = decorating.Decorated;
        }

        return false;
    }
}
