using System.Collections.Concurrent;
using System.Reflection;

namespace SimpleMediator;

/// <summary>
/// Scans assemblies to discover mediator handlers, behaviors, and processors.
/// </summary>
internal static class MediatorAssemblyScanner
{
    private static readonly ConcurrentDictionary<Assembly, AssemblyScanResult> Cache = new();

    public static AssemblyScanResult GetRegistrations(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        return Cache.GetOrAdd(assembly, ScanAssembly);
    }

    private static AssemblyScanResult ScanAssembly(Assembly assembly)
    {
        var handlerRegistrations = new List<TypeRegistration>();
        var notificationRegistrations = new List<TypeRegistration>();
        var pipelineRegistrations = new List<TypeRegistration>();
        var preProcessorRegistrations = new List<TypeRegistration>();
        var postProcessorRegistrations = new List<TypeRegistration>();

        foreach (var type in GetLoadableTypes(assembly))
        {
            if (type is null || !type.IsClass || type.IsAbstract)
            {
                continue;
            }

            foreach (var implementedInterface in type.GetInterfaces().Where(i => i.IsGenericType))
            {
                var genericDefinition = implementedInterface.GetGenericTypeDefinition();

                if (genericDefinition == typeof(IRequestHandler<,>))
                {
                    handlerRegistrations.Add(new TypeRegistration(implementedInterface, type));
                }
                else if (genericDefinition == typeof(INotificationHandler<>))
                {
                    notificationRegistrations.Add(new TypeRegistration(implementedInterface, type));
                }
                else if (genericDefinition == typeof(IPipelineBehavior<,>))
                {
                    var serviceType = implementedInterface.ContainsGenericParameters
                        ? typeof(IPipelineBehavior<,>)
                        : implementedInterface;
                    pipelineRegistrations.Add(new TypeRegistration(serviceType, type));
                }
                else if (genericDefinition == typeof(IRequestPreProcessor<>))
                {
                    var serviceType = implementedInterface.ContainsGenericParameters
                        ? typeof(IRequestPreProcessor<>)
                        : implementedInterface;
                    preProcessorRegistrations.Add(new TypeRegistration(serviceType, type));
                }
                else if (genericDefinition == typeof(IRequestPostProcessor<,>))
                {
                    var serviceType = implementedInterface.ContainsGenericParameters
                        ? typeof(IRequestPostProcessor<,>)
                        : implementedInterface;
                    postProcessorRegistrations.Add(new TypeRegistration(serviceType, type));
                }
            }
        }

        return new AssemblyScanResult(
            handlerRegistrations,
            notificationRegistrations,
            pipelineRegistrations,
            preProcessorRegistrations,
            postProcessorRegistrations);
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
    IReadOnlyCollection<TypeRegistration> RequestPostProcessorRegistrations);
