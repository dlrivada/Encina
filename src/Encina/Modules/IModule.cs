using Microsoft.Extensions.DependencyInjection;

namespace Encina.Modules;

/// <summary>
/// Defines a module that can register its own services and handlers.
/// </summary>
/// <remarks>
/// <para>
/// Modules provide a way to organize application components into cohesive units.
/// Each module encapsulates its own handlers, services, and configuration.
/// </para>
/// <para>
/// This interface is the foundation for building modular monolith architectures
/// where each module can be developed, tested, and potentially deployed independently.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderModule : IModule
/// {
///     public string Name => "Orders";
///
///     public void ConfigureServices(IServiceCollection services)
///     {
///         services.AddScoped&lt;IOrderRepository, OrderRepository&gt;();
///     }
/// }
/// </code>
/// </example>
public interface IModule
{
    /// <summary>
    /// Gets the unique name of the module.
    /// </summary>
    /// <remarks>
    /// The name should be unique across all modules in the application.
    /// It is used for identification, logging, and diagnostics purposes.
    /// </remarks>
    string Name { get; }

    /// <summary>
    /// Configures the services required by this module.
    /// </summary>
    /// <param name="services">The service collection to register services into.</param>
    /// <remarks>
    /// <para>
    /// This method is called during application startup to register
    /// all services, repositories, and other dependencies the module requires.
    /// </para>
    /// <para>
    /// Handlers within the module are automatically discovered and registered
    /// based on the module's assembly.
    /// </para>
    /// </remarks>
    void ConfigureServices(IServiceCollection services);
}
