using global::Marten;
using global::Marten.Services.Json.Transformations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MartenIEventUpcaster = global::Marten.Services.Json.Transformations.IEventUpcaster;

namespace Encina.Marten.Versioning;

/// <summary>
/// Configures Marten with registered event upcasters.
/// </summary>
/// <remarks>
/// <para>
/// This class implements <see cref="IConfigureOptions{StoreOptions}"/> to integrate
/// Encina's event upcasters with Marten's event store configuration.
/// </para>
/// <para>
/// Upcasters registered through <see cref="EventVersioningOptions"/> are automatically
/// added to Marten's event store during startup.
/// </para>
/// </remarks>
internal sealed class ConfigureMartenEventVersioning : IConfigureOptions<StoreOptions>
{
    private readonly EventUpcasterRegistry _registry;
    private readonly IOptions<EncinaMartenOptions> _options;
    private readonly IEnumerable<IEventUpcasterRegistrar> _registrars;
    private readonly ILogger<ConfigureMartenEventVersioning> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigureMartenEventVersioning"/> class.
    /// </summary>
    /// <param name="registry">The event upcaster registry containing all registered upcasters.</param>
    /// <param name="options">The Encina Marten options.</param>
    /// <param name="registrars">The individual upcaster registrars.</param>
    /// <param name="logger">The logger.</param>
    public ConfigureMartenEventVersioning(
        EventUpcasterRegistry registry,
        IOptions<EncinaMartenOptions> options,
        IEnumerable<IEventUpcasterRegistrar> registrars,
        ILogger<ConfigureMartenEventVersioning> logger)
    {
        _registry = registry;
        _options = options;
        _registrars = registrars;
        _logger = logger;
    }

    /// <summary>
    /// Configures Marten's store options with the registered event upcasters.
    /// </summary>
    /// <param name="options">The Marten store options to configure.</param>
    public void Configure(StoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // Apply registrations from EventVersioningOptions
        _options.Value.EventVersioning.ApplyTo(_registry);

        // Apply registrations from individual IEventUpcasterRegistrar
        foreach (var registrar in _registrars)
        {
            registrar.Register(_registry);
        }

        // Add all upcasters to Marten
        var upcasters = _registry.GetAllUpcasters();

        VersioningLog.ConfiguringMartenUpcasters(_logger, upcasters.Count);

        foreach (var upcaster in upcasters)
        {
            // Marten's EventUpcaster base class is already compatible with StoreOptions.Events
            // Our upcasters inherit from Marten.Services.Json.Transformations.EventUpcaster
            // so we can add them directly to Marten's transformations
            if (upcaster is MartenIEventUpcaster martenUpcaster)
            {
                VersioningLog.AddingUpcasterToMarten(_logger, upcaster.GetType().Name);
                options.Events.Upcast(martenUpcaster);
            }
        }

        VersioningLog.EventVersioningEnabled(_logger, upcasters.Count);
    }
}
