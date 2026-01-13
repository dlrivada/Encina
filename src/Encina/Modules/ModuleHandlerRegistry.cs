using System.Collections.Concurrent;
using System.Reflection;

namespace Encina.Modules;

/// <summary>
/// Default implementation of <see cref="IModuleHandlerRegistry"/>.
/// </summary>
/// <remarks>
/// <para>
/// Maps handlers to modules based on assembly association. When a module is registered
/// with an assembly for handler discovery, all handlers in that assembly are associated
/// with the module.
/// </para>
/// <para>
/// The registry uses a concurrent dictionary for thread-safe lookups at runtime.
/// </para>
/// </remarks>
internal sealed class ModuleHandlerRegistry : IModuleHandlerRegistry
{
    private readonly Dictionary<Assembly, IModule> _assemblyToModule;
    private readonly ConcurrentDictionary<Type, IModule?> _handlerModuleCache = new();
    private readonly ConcurrentDictionary<(Type RequestType, Type ResponseType), IModule?> _requestToModuleCache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleHandlerRegistry"/> class.
    /// </summary>
    /// <param name="moduleDescriptors">The module descriptors containing assembly mappings.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="moduleDescriptors"/> is <c>null</c>.
    /// </exception>
    public ModuleHandlerRegistry(IEnumerable<ModuleDescriptor> moduleDescriptors)
    {
        ArgumentNullException.ThrowIfNull(moduleDescriptors);

        var descriptorList = moduleDescriptors.ToList();

        // Build assembly-to-module mapping
        // Note: If multiple modules share the same assembly, the last one wins
        // (this is an edge case that should be avoided in practice)
        _assemblyToModule = descriptorList
            .GroupBy(d => d.HandlerAssembly)
            .ToDictionary(
                g => g.Key,
                g => g.Last().Module);

        // Pre-populate request-to-module cache by scanning handlers
        PopulateRequestToModuleCache(descriptorList);
    }

    /// <summary>
    /// Scans module assemblies to build the request-to-module mapping.
    /// </summary>
    private void PopulateRequestToModuleCache(IEnumerable<ModuleDescriptor> descriptors)
    {
        foreach (var descriptor in descriptors)
        {
            PopulateModuleHandlers(descriptor);
        }
    }

    private void PopulateModuleHandlers(ModuleDescriptor descriptor)
    {
        var assembly = descriptor.HandlerAssembly;
        var module = descriptor.Module;

        foreach (var type in GetLoadableTypes(assembly).Where(t => !t.IsAbstract && !t.IsInterface))
        {
            ProcessTypeInterfaces(type, module);
        }
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
    }

    private void ProcessTypeInterfaces(Type type, IModule module)
    {
        foreach (var iface in type.GetInterfaces().Where(IsRequestHandlerInterface))
        {
            var args = iface.GetGenericArguments();
            _requestToModuleCache[(args[0], args[1])] = module;
            _handlerModuleCache[type] = module;
        }
    }

    private static bool IsRequestHandlerInterface(Type iface)
    {
        return iface.IsGenericType &&
               iface.GetGenericTypeDefinition() == typeof(IRequestHandler<,>);
    }

    /// <inheritdoc />
    public string? GetModuleName(Type handlerType)
    {
        ArgumentNullException.ThrowIfNull(handlerType);

        var module = GetModule(handlerType);
        return module?.Name;
    }

    /// <inheritdoc />
    public IModule? GetModule(Type handlerType)
    {
        ArgumentNullException.ThrowIfNull(handlerType);

        // Check if it's a generic IRequestHandler<,> interface
        if (handlerType.IsInterface && handlerType.IsGenericType)
        {
            var genericDef = handlerType.GetGenericTypeDefinition();
            if (genericDef == typeof(IRequestHandler<,>))
            {
                var args = handlerType.GetGenericArguments();
                return GetModuleForRequest(args[0], args[1]);
            }
        }

        // Use cache for concrete handler types
        return _handlerModuleCache.GetOrAdd(handlerType, type =>
        {
            var assembly = type.Assembly;
            return _assemblyToModule.TryGetValue(assembly, out var module)
                ? module
                : null;
        });
    }

    /// <summary>
    /// Gets the module for a specific request/response type pair.
    /// </summary>
    /// <param name="requestType">The request type.</param>
    /// <param name="responseType">The response type.</param>
    /// <returns>The module owning the handler, or <c>null</c> if not found.</returns>
    public IModule? GetModuleForRequest(Type requestType, Type responseType)
    {
        ArgumentNullException.ThrowIfNull(requestType);
        ArgumentNullException.ThrowIfNull(responseType);

        return _requestToModuleCache.TryGetValue((requestType, responseType), out var module)
            ? module
            : null;
    }

    /// <inheritdoc />
    public bool BelongsToModule(Type handlerType, string moduleName)
    {
        ArgumentNullException.ThrowIfNull(handlerType);
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleName);

        var module = GetModule(handlerType);
        return module is not null &&
               string.Equals(module.Name, moduleName, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public bool BelongsToModule<TModule>(Type handlerType) where TModule : class, IModule
    {
        ArgumentNullException.ThrowIfNull(handlerType);

        var module = GetModule(handlerType);
        return module is TModule;
    }
}

/// <summary>
/// Null object pattern implementation for when modules are not configured.
/// </summary>
internal sealed class NullModuleHandlerRegistry : IModuleHandlerRegistry
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static readonly NullModuleHandlerRegistry Instance = new();

    private NullModuleHandlerRegistry()
    {
    }

    /// <inheritdoc />
    public string? GetModuleName(Type handlerType)
    {
        ArgumentNullException.ThrowIfNull(handlerType);
        return null;
    }

    /// <inheritdoc />
    public IModule? GetModule(Type handlerType)
    {
        ArgumentNullException.ThrowIfNull(handlerType);
        return null;
    }

    /// <inheritdoc />
    public bool BelongsToModule(Type handlerType, string moduleName)
    {
        ArgumentNullException.ThrowIfNull(handlerType);
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleName);
        return false;
    }

    /// <inheritdoc />
    public bool BelongsToModule<TModule>(Type handlerType) where TModule : class, IModule
    {
        ArgumentNullException.ThrowIfNull(handlerType);
        return false;
    }
}
