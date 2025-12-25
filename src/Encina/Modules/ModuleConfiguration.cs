using System.Reflection;

namespace Encina.Modules;

/// <summary>
/// Configures module registration and discovery.
/// </summary>
/// <remarks>
/// <para>
/// This class provides a fluent API for registering modules in the application.
/// Modules can be added individually or discovered from assemblies.
/// </para>
/// <para>
/// Each module's assembly is automatically scanned for handlers unless
/// explicitly disabled using <see cref="WithoutHandlerDiscovery"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaModules(config =>
/// {
///     config.AddModule&lt;OrderModule&gt;();
///     config.AddModule&lt;PaymentModule&gt;();
///     config.AddModule&lt;ShippingModule&gt;();
/// });
/// </code>
/// </example>
public sealed class ModuleConfiguration
{
    private readonly List<ModuleDescriptor> _moduleDescriptors = [];
    private bool _enableHandlerDiscovery = true;

    /// <summary>
    /// Gets the descriptors for all registered modules.
    /// </summary>
    internal IReadOnlyList<ModuleDescriptor> ModuleDescriptors => _moduleDescriptors;

    /// <summary>
    /// Gets a value indicating whether handler discovery is enabled.
    /// </summary>
    internal bool EnableHandlerDiscovery => _enableHandlerDiscovery;

    /// <summary>
    /// Adds a module by its type.
    /// </summary>
    /// <typeparam name="TModule">
    /// The type of the module. Must have a parameterless constructor.
    /// </typeparam>
    /// <returns>This configuration instance for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a module with the same name is already registered.
    /// </exception>
    public ModuleConfiguration AddModule<TModule>() where TModule : IModule, new()
    {
        return AddModule(new TModule());
    }

    /// <summary>
    /// Adds a module instance.
    /// </summary>
    /// <param name="module">The module instance to add.</param>
    /// <returns>This configuration instance for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="module"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a module with the same name is already registered.
    /// </exception>
    public ModuleConfiguration AddModule(IModule module)
    {
        ArgumentNullException.ThrowIfNull(module);

        if (_moduleDescriptors.Any(d => d.Module.Name.Equals(module.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"A module with name '{module.Name}' is already registered. " +
                "Each module must have a unique name.");
        }

        var descriptor = new ModuleDescriptor(module, module.GetType().Assembly);
        _moduleDescriptors.Add(descriptor);

        return this;
    }

    /// <summary>
    /// Adds a module instance with a specific assembly for handler discovery.
    /// </summary>
    /// <param name="module">The module instance to add.</param>
    /// <param name="handlerAssembly">
    /// The assembly to scan for handlers. If <c>null</c>, the module's assembly is used.
    /// </param>
    /// <returns>This configuration instance for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="module"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a module with the same name is already registered.
    /// </exception>
    public ModuleConfiguration AddModule(IModule module, Assembly? handlerAssembly)
    {
        ArgumentNullException.ThrowIfNull(module);

        if (_moduleDescriptors.Any(d => d.Module.Name.Equals(module.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"A module with name '{module.Name}' is already registered. " +
                "Each module must have a unique name.");
        }

        var descriptor = new ModuleDescriptor(module, handlerAssembly ?? module.GetType().Assembly);
        _moduleDescriptors.Add(descriptor);

        return this;
    }

    /// <summary>
    /// Disables automatic handler discovery from module assemblies.
    /// </summary>
    /// <returns>This configuration instance for chaining.</returns>
    /// <remarks>
    /// When disabled, you must manually register handlers using
    /// <c>AddEncina</c> or by calling <c>ConfigureServices</c> on each module.
    /// </remarks>
    public ModuleConfiguration WithoutHandlerDiscovery()
    {
        _enableHandlerDiscovery = false;
        return this;
    }
}

/// <summary>
/// Describes a registered module and its configuration.
/// </summary>
/// <param name="Module">The module instance.</param>
/// <param name="HandlerAssembly">The assembly to scan for handlers.</param>
internal sealed record ModuleDescriptor(IModule Module, Assembly HandlerAssembly);
