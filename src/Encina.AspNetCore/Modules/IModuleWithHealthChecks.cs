using Encina.Messaging.Health;
using Encina.Modules;

namespace Encina.AspNetCore.Modules;

/// <summary>
/// Extends <see cref="IModule"/> to provide health checks specific to the module.
/// </summary>
/// <remarks>
/// <para>
/// Modules that implement this interface can expose their own health checks,
/// which are automatically registered when using <c>AddEncinaModuleHealthChecks()</c>
/// in ASP.NET Core.
/// </para>
/// <para>
/// This is an optional extension - modules that don't need health checks
/// can simply implement <see cref="IModule"/> without this interface.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrdersModule : IModuleWithHealthChecks
/// {
///     private readonly string _connectionString;
///
///     public OrdersModule(string connectionString)
///     {
///         _connectionString = connectionString;
///     }
///
///     public string Name => "Orders";
///
///     public void ConfigureServices(IServiceCollection services)
///     {
///         services.AddScoped&lt;IOrderRepository, OrderRepository&gt;();
///     }
///
///     public IEnumerable&lt;IEncinaHealthCheck&gt; GetHealthChecks()
///     {
///         yield return new OrdersDatabaseHealthCheck(_connectionString);
///         yield return new OrdersQueueHealthCheck();
///     }
/// }
/// </code>
/// </example>
public interface IModuleWithHealthChecks : IModule
{
    /// <summary>
    /// Gets the health checks associated with this module.
    /// </summary>
    /// <returns>
    /// An enumerable of health checks for this module.
    /// Returns an empty enumerable if no health checks are configured.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Health checks returned by this method are automatically registered
    /// with ASP.NET Core's health check system when using
    /// <c>AddEncinaModuleHealthChecks()</c>.
    /// </para>
    /// <para>
    /// Each health check should be tagged with the module name for easy filtering
    /// in health check endpoints.
    /// </para>
    /// </remarks>
    IEnumerable<IEncinaHealthCheck> GetHealthChecks();
}
