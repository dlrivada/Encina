using System.Reflection;

namespace Encina.Marten.Versioning;

/// <summary>
/// Configuration options for event versioning and upcasting.
/// </summary>
/// <remarks>
/// <para>
/// Event versioning enables transparent migration of old event schemas to new ones
/// during event replay. Upcasters are registered here and automatically integrated
/// with Marten's event store.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaMarten(options =>
/// {
///     options.EventVersioning.Enabled = true;
///
///     // Register upcaster by type
///     options.EventVersioning.AddUpcaster&lt;OrderCreatedV1ToV2Upcaster&gt;();
///
///     // Register inline upcaster
///     options.EventVersioning.AddUpcaster&lt;OrderCreatedV1, OrderCreatedV2&gt;(
///         old => new OrderCreatedV2(old.OrderId, old.CustomerName, "unknown@example.com"));
///
///     // Scan assembly for upcasters
///     options.EventVersioning.ScanAssembly(typeof(Program).Assembly);
/// });
/// </code>
/// </example>
public sealed class EventVersioningOptions
{
    private readonly List<Action<EventUpcasterRegistry>> _registrations = [];
    private readonly List<Assembly> _assembliesToScan = [];

    /// <summary>
    /// Gets or sets a value indicating whether event versioning is enabled.
    /// Default is false.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to throw on upcasting failures.
    /// Default is true. When false, errors are logged but do not stop processing.
    /// </summary>
    public bool ThrowOnUpcastFailure { get; set; } = true;

    /// <summary>
    /// Gets the assemblies configured for scanning.
    /// </summary>
    public IReadOnlyList<Assembly> AssembliesToScan => _assembliesToScan;

    /// <summary>
    /// Registers an upcaster by its type.
    /// </summary>
    /// <typeparam name="TUpcaster">The upcaster type.</typeparam>
    /// <returns>The options instance for chaining.</returns>
    public EventVersioningOptions AddUpcaster<TUpcaster>()
        where TUpcaster : class, IEventUpcaster, new()
    {
        _registrations.Add(registry => registry.Register<TUpcaster>());
        return this;
    }

    /// <summary>
    /// Registers an upcaster instance.
    /// </summary>
    /// <param name="upcaster">The upcaster instance.</param>
    /// <returns>The options instance for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="upcaster"/> is null.
    /// </exception>
    public EventVersioningOptions AddUpcaster(IEventUpcaster upcaster)
    {
        ArgumentNullException.ThrowIfNull(upcaster);

        _registrations.Add(registry => registry.Register(upcaster));
        return this;
    }

    /// <summary>
    /// Registers an upcaster by its runtime type.
    /// </summary>
    /// <param name="upcasterType">The type of the upcaster.</param>
    /// <returns>The options instance for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="upcasterType"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="upcasterType"/> does not implement <see cref="IEventUpcaster"/>.
    /// </exception>
    public EventVersioningOptions AddUpcaster(Type upcasterType)
    {
        ArgumentNullException.ThrowIfNull(upcasterType);

        if (!typeof(IEventUpcaster).IsAssignableFrom(upcasterType))
        {
            throw new ArgumentException(
                $"Type '{upcasterType.FullName}' does not implement IEventUpcaster.",
                nameof(upcasterType));
        }

        _registrations.Add(registry => registry.Register(upcasterType));
        return this;
    }

    /// <summary>
    /// Registers an inline upcaster using a transformation function.
    /// </summary>
    /// <typeparam name="TFrom">The source (old) event type.</typeparam>
    /// <typeparam name="TTo">The target (new) event type.</typeparam>
    /// <param name="upcastFunc">The transformation function.</param>
    /// <param name="eventTypeName">
    /// Optional custom event type name. If not specified, uses the name of <typeparamref name="TFrom"/>.
    /// </param>
    /// <returns>The options instance for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="upcastFunc"/> is null.
    /// </exception>
    public EventVersioningOptions AddUpcaster<TFrom, TTo>(
        Func<TFrom, TTo> upcastFunc,
        string? eventTypeName = null)
        where TFrom : class
        where TTo : class
    {
        ArgumentNullException.ThrowIfNull(upcastFunc);

        var upcaster = new LambdaEventUpcaster<TFrom, TTo>(upcastFunc, eventTypeName);
        _registrations.Add(registry => registry.Register(upcaster));
        return this;
    }

    /// <summary>
    /// Adds an assembly to scan for <see cref="IEventUpcaster"/> implementations.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>The options instance for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="assembly"/> is null.
    /// </exception>
    public EventVersioningOptions ScanAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        _assembliesToScan.Add(assembly);
        return this;
    }

    /// <summary>
    /// Adds assemblies to scan for <see cref="IEventUpcaster"/> implementations.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>The options instance for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="assemblies"/> is null.
    /// </exception>
    public EventVersioningOptions ScanAssemblies(params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        foreach (var assembly in assemblies)
        {
            ScanAssembly(assembly);
        }

        return this;
    }

    /// <summary>
    /// Applies all registrations to the specified registry.
    /// </summary>
    /// <param name="registry">The registry to populate.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="registry"/> is null.
    /// </exception>
    public void ApplyTo(EventUpcasterRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        // Apply explicit registrations
        foreach (var registration in _registrations)
        {
            registration(registry);
        }

        // Scan assemblies
        foreach (var assembly in _assembliesToScan)
        {
            registry.ScanAndRegister(assembly);
        }
    }
}
