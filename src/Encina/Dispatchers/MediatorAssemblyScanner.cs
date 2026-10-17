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

    // Handlers register under the exact interface they implement
    private static readonly Dictionary<Type, Func<ScanResultBuilder, List<TypeRegistration>>> ExactTargets = new()
    {
        [typeof(IRequestHandler<,>)] = static result => result.Handlers,
        [typeof(INotificationHandler<>)] = static result => result.Notifications,
        [typeof(IStreamRequestHandler<,>)] = static result => result.StreamHandlers
    };

    // Behaviors and processors register under their open generic interface when the implementation is open
    private static readonly Dictionary<Type, Func<ScanResultBuilder, List<TypeRegistration>>> OpenGenericTargets = new()
    {
        [typeof(IRequestPreProcessor<>)] = static result => result.PreProcessors,
        [typeof(IRequestPostProcessor<,>)] = static result => result.PostProcessors,
        [typeof(IRequestErrorProcessor<,>)] = static result => result.ErrorProcessors,
        [typeof(IRequestPostCommitProcessor<,>)] = static result => result.PostCommitProcessors,
        [typeof(IStreamPipelineBehavior<,>)] = static result => result.StreamPipelines
    };

    private static void ProcessInterface(Type type, Type implementedInterface, Type genericDefinition, ScanResultBuilder result)
    {
        if (ExactTargets.TryGetValue(genericDefinition, out var exactTarget))
        {
            exactTarget(result).Add(new TypeRegistration(implementedInterface, type));
        }
        else if (OpenGenericTargets.TryGetValue(genericDefinition, out var openTarget))
        {
            AddWithOpenGenericFallback(openTarget(result), implementedInterface, type, genericDefinition);
        }
        else if (genericDefinition == typeof(IPipelineBehavior<,>))
        {
            TryAddPipelineBehavior(type, implementedInterface, result);
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
        public List<TypeRegistration> ErrorProcessors { get; } = [];
        public List<TypeRegistration> PostCommitProcessors { get; } = [];
        public List<TypeRegistration> StreamHandlers { get; } = [];
        public List<TypeRegistration> StreamPipelines { get; } = [];

        public AssemblyScanResult Build() => new(
            Handlers,
            Notifications,
            Pipelines,
            PreProcessors,
            PostProcessors,
            ErrorProcessors,
            PostCommitProcessors,
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
    IReadOnlyCollection<TypeRegistration> RequestErrorProcessorRegistrations,
    IReadOnlyCollection<TypeRegistration> RequestPostCommitProcessorRegistrations,
    IReadOnlyCollection<TypeRegistration> StreamHandlerRegistrations,
    IReadOnlyCollection<TypeRegistration> StreamPipelineRegistrations);
