namespace Encina.Modules;

/// <summary>
/// Extends <see cref="IModule"/> with lifecycle hooks for startup and shutdown.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface when your module needs to perform initialization
/// or cleanup operations during application startup and shutdown.
/// </para>
/// <para>
/// Examples of lifecycle operations include:
/// <list type="bullet">
///   <item>Establishing connections to external services</item>
///   <item>Warming up caches</item>
///   <item>Running database migrations</item>
///   <item>Graceful shutdown of background processes</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderModule : IModuleLifecycle
/// {
///     public string Name => "Orders";
///
///     public void ConfigureServices(IServiceCollection services)
///     {
///         services.AddScoped&lt;IOrderRepository, OrderRepository&gt;();
///     }
///
///     public async Task OnStartAsync(CancellationToken cancellationToken)
///     {
///         // Initialize order processing pipeline
///         await _orderProcessor.WarmupAsync(cancellationToken);
///     }
///
///     public async Task OnStopAsync(CancellationToken cancellationToken)
///     {
///         // Complete pending orders before shutdown
///         await _orderProcessor.DrainAsync(cancellationToken);
///     }
/// }
/// </code>
/// </example>
public interface IModuleLifecycle : IModule
{
    /// <summary>
    /// Called when the application is starting, after all services are registered.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token that signals when the startup should be aborted.
    /// </param>
    /// <returns>A task representing the asynchronous startup operation.</returns>
    /// <remarks>
    /// <para>
    /// Modules are started in the order they were registered.
    /// If a module's startup fails, subsequent modules will not be started.
    /// </para>
    /// <para>
    /// Keep startup operations efficient to avoid delaying application readiness.
    /// For long-running initialization, consider using background services.
    /// </para>
    /// </remarks>
    Task OnStartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Called when the application is stopping, before services are disposed.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token that signals when the shutdown should be forced.
    /// </param>
    /// <returns>A task representing the asynchronous shutdown operation.</returns>
    /// <remarks>
    /// <para>
    /// Modules are stopped in reverse order of registration (LIFO).
    /// This ensures that dependent modules are stopped before their dependencies.
    /// </para>
    /// <para>
    /// Perform graceful cleanup here, such as completing in-flight requests
    /// or persisting state.
    /// </para>
    /// </remarks>
    Task OnStopAsync(CancellationToken cancellationToken);
}
