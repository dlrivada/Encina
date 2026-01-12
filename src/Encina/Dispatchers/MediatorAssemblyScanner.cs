using System.Collections.Concurrent;
using System.Reflection;

namespace Encina;

/// <summary>
/// Scans assemblies to discover Encina handlers, behaviors, and processors.
/// </summary>
internal static class EncinaAssemblyScanner
{
    private static readonly ConcurrentDictionary<Assembly, AssemblyScanResult> Cache = new();

    public static AssemblyScanResult GetRegistrations(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        return Cache.GetOrAdd(assembly, ScanAssembly);
    }

    private static AssemblyScanResult ScanAssembly(Assembly assembly)
    {
        var result = new ScanResultBuilder();

        foreach (var type in GetLoadableTypes(assembly))
        {
            if (type is null || !type.IsClass || type.IsAbstract)
            {
                continue;
            }

            ProcessType(type, result);
        }

        return result.Build();
    }

    private static void ProcessType(Type type, ScanResultBuilder result)
    {
        foreach (var implementedInterface in type.GetInterfaces().Where(i => i.IsGenericType))
        {
            var genericDefinition = implementedInterface.GetGenericTypeDefinition();
            ProcessInterface(type, implementedInterface, genericDefinition, result);
        }
    }

    private static void ProcessInterface(Type type, Type implementedInterface, Type genericDefinition, ScanResultBuilder result)
    {
        if (genericDefinition == typeof(IRequestHandler<,>))
        {
            result.Handlers.Add(new TypeRegistration(implementedInterface, type));
            return;
        }

        if (genericDefinition == typeof(INotificationHandler<>))
        {
            result.Notifications.Add(new TypeRegistration(implementedInterface, type));
            return;
        }

        if (genericDefinition == typeof(IPipelineBehavior<,>))
        {
            TryAddPipelineBehavior(type, implementedInterface, result);
            return;
        }

        if (genericDefinition == typeof(IRequestPreProcessor<>))
        {
            AddWithOpenGenericFallback(result.PreProcessors, implementedInterface, type, typeof(IRequestPreProcessor<>));
            return;
        }

        if (genericDefinition == typeof(IRequestPostProcessor<,>))
        {
            AddWithOpenGenericFallback(result.PostProcessors, implementedInterface, type, typeof(IRequestPostProcessor<,>));
            return;
        }

        if (genericDefinition == typeof(IStreamRequestHandler<,>))
        {
            result.StreamHandlers.Add(new TypeRegistration(implementedInterface, type));
            return;
        }

        if (genericDefinition == typeof(IStreamPipelineBehavior<,>))
        {
            AddWithOpenGenericFallback(result.StreamPipelines, implementedInterface, type, typeof(IStreamPipelineBehavior<,>));
        }
    }

    private static void TryAddPipelineBehavior(Type type, Type implementedInterface, ScanResultBuilder result)
    {
        // Skip ValidationPipelineBehavior - it requires ValidationOrchestrator
        // which is only registered by validation packages (FluentValidation, DataAnnotations, etc.)
        if (type.FullName == "Encina.Validation.ValidationPipelineBehavior`2")
        {
            return;
        }

        // Skip ModuleBehaviorAdapter - it has 3 type parameters but IPipelineBehavior has 2
        // It is registered explicitly via AddEncinaModuleBehavior extension method
        if (type.FullName?.StartsWith("Encina.Modules.ModuleBehaviorAdapter`", StringComparison.Ordinal) == true)
        {
            return;
        }

        AddWithOpenGenericFallback(result.Pipelines, implementedInterface, type, typeof(IPipelineBehavior<,>));
    }

    private static void AddWithOpenGenericFallback(
        List<TypeRegistration> registrations,
        Type implementedInterface,
        Type implementationType,
        Type openGenericType)
    {
        var serviceType = implementedInterface.ContainsGenericParameters
            ? openGenericType
            : implementedInterface;
        registrations.Add(new TypeRegistration(serviceType, implementationType));
    }

    private sealed class ScanResultBuilder
    {
        public List<TypeRegistration> Handlers { get; } = [];
        public List<TypeRegistration> Notifications { get; } = [];
        public List<TypeRegistration> Pipelines { get; } = [];
        public List<TypeRegistration> PreProcessors { get; } = [];
        public List<TypeRegistration> PostProcessors { get; } = [];
        public List<TypeRegistration> StreamHandlers { get; } = [];
        public List<TypeRegistration> StreamPipelines { get; } = [];

        public AssemblyScanResult Build() => new(
            Handlers,
            Notifications,
            Pipelines,
            PreProcessors,
            PostProcessors,
            StreamHandlers,
            StreamPipelines);
    }

    private static IEnumerable<Type?> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null);
        }
    }
}

/// <summary>
/// Stores the relationship between a generic service and its concrete implementation.
/// </summary>
internal sealed record TypeRegistration(Type ServiceType, Type ImplementationType);

/// <summary>
/// Result of scanning an assembly.
/// </summary>
internal sealed record AssemblyScanResult(
    IReadOnlyCollection<TypeRegistration> HandlerRegistrations,
    IReadOnlyCollection<TypeRegistration> NotificationRegistrations,
    IReadOnlyCollection<TypeRegistration> PipelineRegistrations,
    IReadOnlyCollection<TypeRegistration> RequestPreProcessorRegistrations,
    IReadOnlyCollection<TypeRegistration> RequestPostProcessorRegistrations,
    IReadOnlyCollection<TypeRegistration> StreamHandlerRegistrations,
    IReadOnlyCollection<TypeRegistration> StreamPipelineRegistrations);
