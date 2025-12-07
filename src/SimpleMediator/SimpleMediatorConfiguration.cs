using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SimpleMediator;

/// <summary>
/// Configura el escaneo y los componentes adicionales del mediador.
/// </summary>
/// <remarks>
/// Se utiliza desde <see cref="ServiceCollectionExtensions.AddSimpleMediator(IServiceCollection, Action{SimpleMediatorConfiguration}, System.Reflection.Assembly[])"/>
/// para ajustar el comportamiento del registro automático.
/// </remarks>
public sealed class SimpleMediatorConfiguration
{
    private readonly HashSet<Assembly> _assemblies = new();
    private readonly List<Type> _pipelineBehaviorTypes = new();
    private readonly List<Type> _requestPreProcessorTypes = new();
    private readonly List<Type> _requestPostProcessorTypes = new();

    /// <summary>
    /// Ciclo de vida que se aplicará a los handlers resueltos automáticamente.
    /// </summary>
    public ServiceLifetime HandlerLifetime { get; private set; } = ServiceLifetime.Scoped;

    internal IReadOnlyCollection<Assembly> Assemblies => _assemblies;
    internal IReadOnlyList<Type> PipelineBehaviorTypes => _pipelineBehaviorTypes;
    internal IReadOnlyList<Type> RequestPreProcessorTypes => _requestPreProcessorTypes;
    internal IReadOnlyList<Type> RequestPostProcessorTypes => _requestPostProcessorTypes;

    /// <summary>
    /// Establece el ciclo de vida que se utilizará para los handlers registrados automáticamente.
    /// </summary>
    public SimpleMediatorConfiguration WithHandlerLifetime(ServiceLifetime lifetime)
    {
        HandlerLifetime = lifetime;
        return this;
    }

    /// <summary>
    /// Incluye un ensamblado para el escaneo de handlers, behaviors y processors.
    /// </summary>
    public SimpleMediatorConfiguration RegisterServicesFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        _assemblies.Add(assembly);
        return this;
    }

    /// <summary>
    /// Incluye múltiples ensamblados en el escaneo.
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
    /// Incluye el ensamblado que contiene al tipo indicado.
    /// </summary>
    public SimpleMediatorConfiguration RegisterServicesFromAssemblyContaining<T>()
        => RegisterServicesFromAssembly(typeof(T).Assembly);

    /// <summary>
    /// Agrega un behavior genérico al pipeline.
    /// </summary>
    public SimpleMediatorConfiguration AddPipelineBehavior<TBehavior>()
        where TBehavior : class
        => AddPipelineBehavior(typeof(TBehavior));

    /// <summary>
    /// Agrega un behavior específico al pipeline.
    /// </summary>
    public SimpleMediatorConfiguration AddPipelineBehavior(Type pipelineBehaviorType)
    {
        ArgumentNullException.ThrowIfNull(pipelineBehaviorType);

        if (!pipelineBehaviorType.IsClass || pipelineBehaviorType.IsAbstract)
        {
            throw new ArgumentException($"{pipelineBehaviorType.Name} debe ser un tipo de clase concreta.", nameof(pipelineBehaviorType));
        }

        if (!typeof(IPipelineBehavior<,>).IsAssignableFromGeneric(pipelineBehaviorType))
        {
            throw new ArgumentException($"{pipelineBehaviorType.Name} no implementa IPipelineBehavior<,>.", nameof(pipelineBehaviorType));
        }

        if (!_pipelineBehaviorTypes.Contains(pipelineBehaviorType))
        {
            _pipelineBehaviorTypes.Add(pipelineBehaviorType);
        }

        return this;
    }

    /// <summary>
    /// Agrega un pre-procesador genérico al pipeline.
    /// </summary>
    public SimpleMediatorConfiguration AddRequestPreProcessor<TProcessor>()
        where TProcessor : class
        => AddRequestPreProcessor(typeof(TProcessor));

    /// <summary>
    /// Agrega un pre-procesador específico al pipeline.
    /// </summary>
    public SimpleMediatorConfiguration AddRequestPreProcessor(Type processorType)
    {
        ArgumentNullException.ThrowIfNull(processorType);

        if (!processorType.IsClass || processorType.IsAbstract)
        {
            throw new ArgumentException($"{processorType.Name} debe ser un tipo de clase concreta.", nameof(processorType));
        }

        if (!typeof(IRequestPreProcessor<>).IsAssignableFromGeneric(processorType))
        {
            throw new ArgumentException($"{processorType.Name} no implementa IRequestPreProcessor<>.", nameof(processorType));
        }

        if (!_requestPreProcessorTypes.Contains(processorType))
        {
            _requestPreProcessorTypes.Add(processorType);
        }

        return this;
    }

    /// <summary>
    /// Agrega un post-procesador genérico al pipeline.
    /// </summary>
    public SimpleMediatorConfiguration AddRequestPostProcessor<TProcessor>()
        where TProcessor : class
        => AddRequestPostProcessor(typeof(TProcessor));

    /// <summary>
    /// Agrega un post-procesador específico al pipeline.
    /// </summary>
    public SimpleMediatorConfiguration AddRequestPostProcessor(Type processorType)
    {
        ArgumentNullException.ThrowIfNull(processorType);

        if (!processorType.IsClass || processorType.IsAbstract)
        {
            throw new ArgumentException($"{processorType.Name} debe ser un tipo de clase concreta.", nameof(processorType));
        }

        if (!typeof(IRequestPostProcessor<,>).IsAssignableFromGeneric(processorType))
        {
            throw new ArgumentException($"{processorType.Name} no implementa IRequestPostProcessor<,>.", nameof(processorType));
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
    /// Determina si un tipo genérico es asignable considerando sus argumentos.
    /// </summary>
    public static bool IsAssignableFromGeneric(this Type genericInterface, Type candidate)
    {
        if (genericInterface is null)
        {
            throw new ArgumentNullException(nameof(genericInterface));
        }

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
