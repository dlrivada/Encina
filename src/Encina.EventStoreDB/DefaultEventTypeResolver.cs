using System.Collections.Concurrent;
using System.Reflection;

namespace Encina.EventStoreDB;

/// <summary>
/// Default implementation of event type resolution using convention-based naming.
/// </summary>
public sealed class DefaultEventTypeResolver : IEventTypeResolver
{
    private readonly ConcurrentDictionary<Type, string> _typeToName = new();
    private readonly ConcurrentDictionary<string, Type> _nameToType = new();
    private readonly List<Assembly> _scanAssemblies = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultEventTypeResolver"/> class.
    /// </summary>
    public DefaultEventTypeResolver()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultEventTypeResolver"/> class
    /// with assemblies to scan for event types.
    /// </summary>
    /// <param name="assemblies">Assemblies to scan for event types.</param>
    public DefaultEventTypeResolver(params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            _scanAssemblies.Add(assembly);
        }
    }

    /// <inheritdoc />
    public string GetTypeName(Type eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);

        return _typeToName.GetOrAdd(eventType, type =>
        {
            // Use simple type name as the event type name
            // This follows EventStoreDB conventions
            return type.Name;
        });
    }

    /// <inheritdoc />
    public Type? ResolveType(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
        {
            return null;
        }

        // Check registered types first
        if (_nameToType.TryGetValue(typeName, out var registeredType))
        {
            return registeredType;
        }

        // Search in scan assemblies
        foreach (var assembly in _scanAssemblies)
        {
            var matchingType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == typeName ||
                                    t.FullName == typeName ||
                                    GetTypeName(t) == typeName);

            if (matchingType is not null)
            {
                _nameToType.TryAdd(typeName, matchingType);
                return matchingType;
            }
        }

        // Try to find by name in all loaded assemblies (fallback)
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var matchingType = assembly.GetTypes()
                    .FirstOrDefault(t => t.Name == typeName);

                if (matchingType is not null)
                {
                    _nameToType.TryAdd(typeName, matchingType);
                    return matchingType;
                }
            }
            catch (ReflectionTypeLoadException)
            {
                // Skip assemblies that can't be loaded
                continue;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public void Register<TEvent>(string? typeName = null) where TEvent : class
    {
        Register(typeof(TEvent), typeName);
    }

    /// <inheritdoc />
    public void Register(Type eventType, string? typeName = null)
    {
        ArgumentNullException.ThrowIfNull(eventType);

        var name = typeName ?? eventType.Name;
        _typeToName[eventType] = name;
        _nameToType[name] = eventType;
    }

    /// <summary>
    /// Adds an assembly to scan for event types.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    public void AddAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        _scanAssemblies.Add(assembly);
    }
}
