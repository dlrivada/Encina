using System.Collections.Concurrent;
using System.Reflection;

namespace Encina.Marten.Versioning;

/// <summary>
/// Registry for discovering and managing event upcasters.
/// </summary>
/// <remarks>
/// <para>
/// The registry maintains a collection of <see cref="IEventUpcaster"/> implementations
/// indexed by their source event type name. This enables fast lookup during event deserialization.
/// </para>
/// <para>
/// Upcasters can be registered manually or discovered automatically from assemblies.
/// </para>
/// </remarks>
public sealed class EventUpcasterRegistry
{
    private readonly ConcurrentDictionary<string, IEventUpcaster> _upcasters = new(StringComparer.Ordinal);
    private readonly List<IEventUpcaster> _allUpcasters = [];
    private readonly object _lock = new();

    /// <summary>
    /// Registers an upcaster by its type.
    /// </summary>
    /// <typeparam name="TUpcaster">The upcaster type.</typeparam>
    /// <exception cref="InvalidOperationException">
    /// Thrown when an upcaster for the same source event type is already registered.
    /// </exception>
    public void Register<TUpcaster>()
        where TUpcaster : class, IEventUpcaster, new()
    {
        Register(new TUpcaster());
    }

    /// <summary>
    /// Registers an upcaster instance.
    /// </summary>
    /// <param name="upcaster">The upcaster instance to register.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="upcaster"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when an upcaster for the same source event type is already registered.
    /// </exception>
    public void Register(IEventUpcaster upcaster)
    {
        ArgumentNullException.ThrowIfNull(upcaster);

        lock (_lock)
        {
            if (!_upcasters.TryAdd(upcaster.SourceEventTypeName, upcaster))
            {
                throw new InvalidOperationException(
                    $"An upcaster for event type '{upcaster.SourceEventTypeName}' is already registered.");
            }

            _allUpcasters.Add(upcaster);
        }
    }

    /// <summary>
    /// Registers an upcaster by its type, creating an instance using the provided factory.
    /// </summary>
    /// <param name="upcasterType">The type of the upcaster.</param>
    /// <param name="factory">Optional factory to create the upcaster instance.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="upcasterType"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="upcasterType"/> does not implement <see cref="IEventUpcaster"/>.
    /// </exception>
    public void Register(Type upcasterType, Func<Type, IEventUpcaster>? factory = null)
    {
        ArgumentNullException.ThrowIfNull(upcasterType);

        if (!typeof(IEventUpcaster).IsAssignableFrom(upcasterType))
        {
            throw new ArgumentException(
                $"Type '{upcasterType.FullName}' does not implement IEventUpcaster.",
                nameof(upcasterType));
        }

        var upcaster = factory?.Invoke(upcasterType)
            ?? (IEventUpcaster)Activator.CreateInstance(upcasterType)!;

        Register(upcaster);
    }

    /// <summary>
    /// Tries to register an upcaster, returning false if already registered.
    /// </summary>
    /// <param name="upcaster">The upcaster to register.</param>
    /// <returns>True if registered successfully; false if already registered.</returns>
    public bool TryRegister(IEventUpcaster upcaster)
    {
        ArgumentNullException.ThrowIfNull(upcaster);

        lock (_lock)
        {
            if (_upcasters.TryAdd(upcaster.SourceEventTypeName, upcaster))
            {
                _allUpcasters.Add(upcaster);
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Gets the upcaster for a specific source event type name.
    /// </summary>
    /// <param name="eventTypeName">The event type name as stored in the event store.</param>
    /// <returns>The upcaster if found; otherwise, null.</returns>
    public IEventUpcaster? GetUpcasterForEventType(string eventTypeName)
    {
        ArgumentNullException.ThrowIfNull(eventTypeName);

        return _upcasters.TryGetValue(eventTypeName, out var upcaster) ? upcaster : null;
    }

    /// <summary>
    /// Gets all registered upcasters.
    /// </summary>
    /// <returns>A read-only list of all registered upcasters.</returns>
    public IReadOnlyList<IEventUpcaster> GetAllUpcasters()
    {
        lock (_lock)
        {
            return _allUpcasters.ToList();
        }
    }

    /// <summary>
    /// Checks if an upcaster is registered for a specific event type.
    /// </summary>
    /// <param name="eventTypeName">The event type name.</param>
    /// <returns>True if an upcaster is registered; otherwise, false.</returns>
    public bool HasUpcasterFor(string eventTypeName)
    {
        ArgumentNullException.ThrowIfNull(eventTypeName);

        return _upcasters.ContainsKey(eventTypeName);
    }

    /// <summary>
    /// Gets the count of registered upcasters.
    /// </summary>
    public int Count => _upcasters.Count;

    /// <summary>
    /// Scans an assembly for <see cref="IEventUpcaster"/> implementations and registers them.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <param name="factory">Optional factory to create upcaster instances.</param>
    /// <returns>The number of upcasters registered from the assembly.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="assembly"/> is null.
    /// </exception>
    public int ScanAndRegister(Assembly assembly, Func<Type, IEventUpcaster>? factory = null)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var upcasterTypes = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                && typeof(IEventUpcaster).IsAssignableFrom(t)
                && t.GetConstructor(Type.EmptyTypes) != null);

        var count = 0;
        foreach (var type in upcasterTypes)
        {
            var upcaster = factory?.Invoke(type)
                ?? (IEventUpcaster)Activator.CreateInstance(type)!;

            if (TryRegister(upcaster))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Clears all registered upcasters.
    /// </summary>
    /// <remarks>
    /// This method is primarily intended for testing purposes.
    /// </remarks>
    internal void Clear()
    {
        lock (_lock)
        {
            _upcasters.Clear();
            _allUpcasters.Clear();
        }
    }
}
