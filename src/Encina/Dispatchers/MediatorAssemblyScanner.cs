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

        foreach (var type in GetLoadableTypes(assembly).Where(t => t is not null && t.IsClass && !t.IsAbstract))
        {
            ProcessType(type!, result);
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
        if (IsExcludedFromScanning(type))
        {
            return;
        }

        AddWithOpenGenericFallback(result.Pipelines, implementedInterface, type, typeof(IPipelineBehavior<,>));
    }

    /// <summary>
    /// Behaviors that are never auto-registered as open-generic services:
    /// <list type="bullet">
    /// <item>Nested types - typically test helpers or private implementations with constructor dependencies
    /// that are not registered in the DI container.</item>
    /// <item><c>ValidationPipelineBehavior</c> - it requires <c>ValidationOrchestrator</c>, which only the
    /// validation packages (FluentValidation, DataAnnotations, ...) register.</item>
    /// <item>Adapted steps (<c>ModuleBehaviorAdapter</c> and the processor steps) - they wrap another instance,
    /// so they are never services. <c>ModuleBehaviorAdapter</c> is registered explicitly via
    /// <c>AddEncinaModuleBehavior</c>; the pipeline builders wrap processors per request.</item>
    /// <item>Shadow sharding behaviors - they require <c>IShadowShardRouter</c> and <c>ShadowShardingOptions</c>,
    /// which are only registered when shadow sharding is enabled (<c>AddShadowSharding</c>).</item>
    /// </list>
    /// </summary>
    private static bool IsExcludedFromScanning(Type type)
        => type.IsNested
            || type.FullName == "Encina.Validation.ValidationPipelineBehavior`2"
            || typeof(IPipelineStepIdentity).IsAssignableFrom(type)
            || type.FullName?.StartsWith("Encina.Sharding.Shadow.Behaviors.", StringComparison.Ordinal) is true;

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
