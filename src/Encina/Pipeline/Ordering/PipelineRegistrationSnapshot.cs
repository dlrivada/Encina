using Microsoft.Extensions.DependencyInjection;

namespace Encina;

/// <summary>
/// One registration that contributes a step to a closed pipeline.
/// </summary>
/// <param name="Descriptor">The service descriptor.</param>
/// <param name="ImplementationType">
/// The concrete type the container creates (open generics already closed over the pipeline's type arguments), or
/// <see langword="null"/> when the descriptor is a factory.
/// </param>
internal readonly record struct PipelineRegistration(ServiceDescriptor Descriptor, Type? ImplementationType);

/// <summary>
/// Read-only view of the service collection that the pipeline order validation needs: the closed handlers, the
/// registrations that contribute steps to their pipelines, and the registrations the builder never resolves.
/// </summary>
/// <remarks>
/// The snapshot keeps a reference to the <see cref="IServiceCollection"/> itself, created by the first
/// <c>AddEncina</c> call, and reads it lazily. Read at host start, the collection holds every descriptor the host added,
/// including those added after <c>AddEncina</c>. Only types are inspected, never instances.
/// </remarks>
internal sealed class PipelineRegistrationSnapshot
{
    private readonly IServiceCollection _services;

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineRegistrationSnapshot"/> class.
    /// </summary>
    /// <param name="services">The service collection the host is built from.</param>
    public PipelineRegistrationSnapshot(IServiceCollection services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>
    /// Returns the closed <c>IRequestHandler&lt;,&gt;</c> and <c>IStreamRequestHandler&lt;,&gt;</c> service types.
    /// Open-generic handlers are skipped: their pipelines are validated on first use.
    /// </summary>
    public IReadOnlyList<Type> GetClosedHandlerServiceTypes()
    {
        var result = new List<Type>();
        foreach (var descriptor in _services)
        {
            if (!descriptor.IsKeyedService && IsClosedHandler(descriptor.ServiceType) && !result.Contains(descriptor.ServiceType))
            {
                result.Add(descriptor.ServiceType);
            }
        }

        return result;
    }

    /// <summary>
    /// Returns the registrations under <c>ICommandPipelineBehavior&lt;,&gt;</c> or <c>IQueryPipelineBehavior&lt;,&gt;</c>,
    /// open or closed: the builder resolves only <c>IPipelineBehavior&lt;,&gt;</c>, so they never run (the #2032 class).
    /// </summary>
    public IReadOnlyList<ServiceDescriptor> GetDeadPipelineRegistrations()
    {
        var result = new List<ServiceDescriptor>();
        foreach (var descriptor in _services)
        {
            if (IsDeadServiceType(descriptor.ServiceType))
            {
                result.Add(descriptor);
            }
        }

        return result;
    }

    /// <summary>
    /// Returns the registrations the container would use for a closed service type, in registration order: the exact
    /// descriptors plus the open-generic ones that can be closed over its type arguments.
    /// </summary>
    /// <param name="closedServiceType">For example <c>IPipelineBehavior&lt;Ping, string&gt;</c>.</param>
    public IReadOnlyList<PipelineRegistration> GetRegistrations(Type closedServiceType)
    {
        ArgumentNullException.ThrowIfNull(closedServiceType);

        var result = new List<PipelineRegistration>();
        foreach (var descriptor in _services)
        {
            if (!descriptor.IsKeyedService && TryClose(descriptor, closedServiceType, out var implementation))
            {
                result.Add(new PipelineRegistration(descriptor, implementation));
            }
        }

        return result;
    }

    private static bool TryClose(ServiceDescriptor descriptor, Type closedServiceType, out Type? implementation)
    {
        implementation = descriptor.ImplementationType ?? descriptor.ImplementationInstance?.GetType();
        if (descriptor.ServiceType == closedServiceType)
        {
            return true;
        }

        return descriptor.ServiceType.IsGenericTypeDefinition
            && descriptor.ServiceType == closedServiceType.GetGenericTypeDefinition()
            && TryCloseImplementation(descriptor.ImplementationType, closedServiceType, out implementation);
    }

    private static bool TryCloseImplementation(Type? definition, Type closedServiceType, out Type? implementation)
    {
        implementation = null;
        if (definition is null || !definition.IsGenericTypeDefinition)
        {
            return false;
        }

        try
        {
            implementation = definition.MakeGenericType(closedServiceType.GenericTypeArguments);
            return true;
        }
        catch (ArgumentException)
        {
            // The type arguments violate a generic constraint: the container skips this registration as well
            return false;
        }
    }

    private static bool IsClosedHandler(Type serviceType)
    {
        if (!serviceType.IsGenericType || serviceType.ContainsGenericParameters)
        {
            return false;
        }

        var definition = serviceType.GetGenericTypeDefinition();
        return definition == typeof(IRequestHandler<,>) || definition == typeof(IStreamRequestHandler<,>);
    }

    private static bool IsDeadServiceType(Type serviceType)
    {
        if (!serviceType.IsGenericType)
        {
            return false;
        }

        var definition = serviceType.GetGenericTypeDefinition();
        return definition == typeof(ICommandPipelineBehavior<,>) || definition == typeof(IQueryPipelineBehavior<,>);
    }
}
