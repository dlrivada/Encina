using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SimpleMediator;

/// <summary>
/// Configures discovery and additional mediator components.
/// </summary>
/// <remarks>
/// Used by <see cref="ServiceCollectionExtensions.AddSimpleMediator(IServiceCollection, Action{SimpleMediatorConfiguration}, System.Reflection.Assembly[])"/>
/// to fine-tune automatic registrations.
/// </remarks>
public sealed class SimpleMediatorConfiguration
{
    private readonly HashSet<Assembly> _assemblies = new();
    private readonly List<Type> _pipelineBehaviorTypes = new();
    private readonly List<Type> _requestPreProcessorTypes = new();
    private readonly List<Type> _requestPostProcessorTypes = new();

    /// <summary>
    /// Lifetime applied to handlers registered through scanning.
    /// </summary>
    public ServiceLifetime HandlerLifetime { get; private set; } = ServiceLifetime.Scoped;

    internal IReadOnlyCollection<Assembly> Assemblies => _assemblies;
    internal IReadOnlyList<Type> PipelineBehaviorTypes => _pipelineBehaviorTypes;
    internal IReadOnlyList<Type> RequestPreProcessorTypes => _requestPreProcessorTypes;
    internal IReadOnlyList<Type> RequestPostProcessorTypes => _requestPostProcessorTypes;

    /// <summary>
    /// Sets the lifetime used for handlers registered through scanning.
    /// </summary>
    public SimpleMediatorConfiguration WithHandlerLifetime(ServiceLifetime lifetime)
    {
        HandlerLifetime = lifetime;
        return this;
    }

    /// <summary>
    /// Adds an assembly to scan for handlers, behaviors, and processors.
    /// </summary>
    public SimpleMediatorConfiguration RegisterServicesFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        _assemblies.Add(assembly);
        return this;
    }

    /// <summary>
    /// Adds multiple assemblies to the scan list.
    /// </summary>
    public SimpleMediatorConfiguration RegisterServicesFromAssemblies(params Assembly[] assemblies)
    {
        if (assemblies is null)
        {
            return this;
        }

        foreach (var assembly in assemblies)
        {
            if (assembly is not null)
            {
                _assemblies.Add(assembly);
            }
        }

        return this;
    }

    /// <summary>
    /// Adds the assembly that contains the specified type.
    /// </summary>
    public SimpleMediatorConfiguration RegisterServicesFromAssemblyContaining<T>()
        => RegisterServicesFromAssembly(typeof(T).Assembly);

    /// <summary>
    /// Adds a generic behavior to the pipeline.
    /// </summary>
    public SimpleMediatorConfiguration AddPipelineBehavior<TBehavior>()
        where TBehavior : class
        => AddPipelineBehavior(typeof(TBehavior));

    /// <summary>
    /// Adds a specific behavior to the pipeline.
    /// </summary>
    public SimpleMediatorConfiguration AddPipelineBehavior(Type pipelineBehaviorType)
    {
        ArgumentNullException.ThrowIfNull(pipelineBehaviorType);

        if (!pipelineBehaviorType.IsClass || pipelineBehaviorType.IsAbstract)
        {
            throw new ArgumentException($"{pipelineBehaviorType.Name} must be a concrete class type.", nameof(pipelineBehaviorType));
        }

        if (!typeof(IPipelineBehavior<,>).IsAssignableFromGeneric(pipelineBehaviorType))
        {
            throw new ArgumentException($"{pipelineBehaviorType.Name} does not implement IPipelineBehavior<,>.", nameof(pipelineBehaviorType));
        }

        if (!_pipelineBehaviorTypes.Contains(pipelineBehaviorType))
        {
            _pipelineBehaviorTypes.Add(pipelineBehaviorType);
        }

        return this;
    }

    /// <summary>
    /// Adds a generic pre-processor to the pipeline.
    /// </summary>
    public SimpleMediatorConfiguration AddRequestPreProcessor<TProcessor>()
        where TProcessor : class
        => AddRequestPreProcessor(typeof(TProcessor));

    /// <summary>
    /// Adds a specific pre-processor to the pipeline.
    /// </summary>
    public SimpleMediatorConfiguration AddRequestPreProcessor(Type processorType)
    {
        ArgumentNullException.ThrowIfNull(processorType);

        if (!processorType.IsClass || processorType.IsAbstract)
        {
            throw new ArgumentException($"{processorType.Name} must be a concrete class type.", nameof(processorType));
        }

        if (!typeof(IRequestPreProcessor<>).IsAssignableFromGeneric(processorType))
        {
            throw new ArgumentException($"{processorType.Name} does not implement IRequestPreProcessor<>.", nameof(processorType));
        }

        if (!_requestPreProcessorTypes.Contains(processorType))
        {
            _requestPreProcessorTypes.Add(processorType);
        }

        return this;
    }

    /// <summary>
    /// Adds a generic post-processor to the pipeline.
    /// </summary>
    public SimpleMediatorConfiguration AddRequestPostProcessor<TProcessor>()
        where TProcessor : class
        => AddRequestPostProcessor(typeof(TProcessor));

    /// <summary>
    /// Adds a specific post-processor to the pipeline.
    /// </summary>
    public SimpleMediatorConfiguration AddRequestPostProcessor(Type processorType)
    {
        ArgumentNullException.ThrowIfNull(processorType);

        if (!processorType.IsClass || processorType.IsAbstract)
        {
            throw new ArgumentException($"{processorType.Name} must be a concrete class type.", nameof(processorType));
        }

        if (!typeof(IRequestPostProcessor<,>).IsAssignableFromGeneric(processorType))
        {
            throw new ArgumentException($"{processorType.Name} does not implement IRequestPostProcessor<,>.", nameof(processorType));
        }

        if (!_requestPostProcessorTypes.Contains(processorType))
        {
            _requestPostProcessorTypes.Add(processorType);
        }

        return this;
    }

    internal void RegisterConfiguredPipelineBehaviors(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        foreach (var behaviorType in _pipelineBehaviorTypes)
        {
            RegisterPipelineBehavior(services, behaviorType);
        }
    }

    internal void RegisterConfiguredRequestPreProcessors(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        foreach (var processorType in _requestPreProcessorTypes)
        {
            RegisterProcessor(services, processorType, typeof(IRequestPreProcessor<>));
        }
    }

    internal void RegisterConfiguredRequestPostProcessors(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        foreach (var processorType in _requestPostProcessorTypes)
        {
            RegisterProcessor(services, processorType, typeof(IRequestPostProcessor<,>));
        }
    }

    private static void RegisterPipelineBehavior(IServiceCollection services, Type behaviorType)
    {
        var pipelineServiceType = ResolveServiceType(behaviorType, typeof(IPipelineBehavior<,>));
        services.TryAddEnumerable(ServiceDescriptor.Scoped(pipelineServiceType, behaviorType));

        if (typeof(ICommandPipelineBehavior<,>).IsAssignableFromGeneric(behaviorType))
        {
            var commandServiceType = ResolveServiceType(behaviorType, typeof(ICommandPipelineBehavior<,>));
            services.TryAddEnumerable(ServiceDescriptor.Scoped(commandServiceType, behaviorType));
        }

        if (typeof(IQueryPipelineBehavior<,>).IsAssignableFromGeneric(behaviorType))
        {
            var queryServiceType = ResolveServiceType(behaviorType, typeof(IQueryPipelineBehavior<,>));
            services.TryAddEnumerable(ServiceDescriptor.Scoped(queryServiceType, behaviorType));
        }
    }

    private static void RegisterProcessor(IServiceCollection services, Type implementationType, Type genericInterface)
    {
        var serviceType = ResolveServiceType(implementationType, genericInterface);
        services.TryAddEnumerable(ServiceDescriptor.Scoped(serviceType, implementationType));
    }

    private static Type ResolveServiceType(Type implementationType, Type genericInterface)
    {
        var interfaceType = implementationType
            .GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericInterface);

        if (implementationType.IsGenericTypeDefinition || interfaceType is null || interfaceType.ContainsGenericParameters)
        {
            return genericInterface;
        }

        return interfaceType;
    }
}

internal static class TypeExtensions
{
    /// <summary>
    /// Determines whether a generic interface is assignable when considering its arguments.
    /// </summary>
    public static bool IsAssignableFromGeneric(this Type genericInterface, Type candidate)
    {
        ArgumentNullException.ThrowIfNull(genericInterface);

        if (candidate is null)
        {
            return false;
        }

        if (!genericInterface.IsInterface || !genericInterface.IsGenericType)
        {
            return genericInterface.IsAssignableFrom(candidate);
        }

        return candidate
            .GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericInterface);
    }
}
