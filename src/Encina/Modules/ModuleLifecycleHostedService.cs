using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Encina.Modules;

/// <summary>
/// Hosted service that manages the lifecycle of modules implementing <see cref="IModuleLifecycle"/>.
/// </summary>
/// <remarks>
/// <para>
/// This service calls <see cref="IModuleLifecycle.OnStartAsync"/> for all lifecycle modules
/// during application startup, and <see cref="IModuleLifecycle.OnStopAsync"/> during shutdown.
/// </para>
/// <para>
/// Modules are started in registration order and stopped in reverse order (LIFO),
/// ensuring proper dependency ordering.
/// </para>
/// </remarks>
internal sealed class ModuleLifecycleHostedService : IHostedService
{
    private readonly IModuleRegistry _registry;
    private readonly ILogger<ModuleLifecycleHostedService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleLifecycleHostedService"/> class.
    /// </summary>
    /// <param name="registry">The module registry.</param>
    /// <param name="logger">The logger instance.</param>
    public ModuleLifecycleHostedService(
        IModuleRegistry registry,
        ILogger<ModuleLifecycleHostedService> logger)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var lifecycleModules = _registry.GetLifecycleModules();

        if (lifecycleModules.Count == 0)
        {
            return;
        }

        LogStartingModules(_logger, lifecycleModules.Count);

        foreach (var module in lifecycleModules)
        {
            try
            {
                LogModuleStarting(_logger, module.Name);
                await module.OnStartAsync(cancellationToken).ConfigureAwait(false);
                LogModuleStarted(_logger, module.Name);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                LogModuleStartCancelled(_logger, module.Name);
                throw;
            }
            catch (Exception ex)
            {
                LogModuleStartFailed(_logger, module.Name, ex);
                throw;
            }
        }

        LogAllModulesStarted(_logger, lifecycleModules.Count);
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var lifecycleModules = _registry.GetLifecycleModules();

        if (lifecycleModules.Count == 0)
        {
            return;
        }

        LogStoppingModules(_logger, lifecycleModules.Count);

        // Stop modules in reverse order (LIFO)
        var reversedModules = lifecycleModules.Reverse().ToList();
        var exceptions = new List<Exception>();

        foreach (var module in reversedModules)
        {
            try
            {
                LogModuleStopping(_logger, module.Name);
                await module.OnStopAsync(cancellationToken).ConfigureAwait(false);
                LogModuleStopped(_logger, module.Name);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                LogModuleStopCancelled(_logger, module.Name);
                // Continue stopping other modules even if one is cancelled
            }
            catch (Exception ex)
            {
                LogModuleStopFailed(_logger, module.Name, ex);
                exceptions.Add(ex);
                // Continue stopping other modules even if one fails
            }
        }

        LogAllModulesStopped(_logger, lifecycleModules.Count);

        if (exceptions.Count > 0)
        {
            throw new AggregateException(
                "One or more modules failed to stop gracefully.",
                exceptions);
        }
    }

#pragma warning disable CA1848 // Use the LoggerMessage delegates
#pragma warning disable CA1873 // Logging parameter evaluation

    private static void LogStartingModules(ILogger logger, int count)
        => logger.LogInformation("Starting {Count} module(s) with lifecycle hooks", count);

    private static void LogModuleStarting(ILogger logger, string moduleName)
        => logger.LogDebug("Starting module '{ModuleName}'", moduleName);

    private static void LogModuleStarted(ILogger logger, string moduleName)
        => logger.LogDebug("Module '{ModuleName}' started successfully", moduleName);

    private static void LogModuleStartCancelled(ILogger logger, string moduleName)
        => logger.LogWarning("Module '{ModuleName}' startup was cancelled", moduleName);

    private static void LogModuleStartFailed(ILogger logger, string moduleName, Exception ex)
        => logger.LogError(ex, "Module '{ModuleName}' failed to start", moduleName);

    private static void LogAllModulesStarted(ILogger logger, int count)
        => logger.LogInformation("All {Count} module(s) started successfully", count);

    private static void LogStoppingModules(ILogger logger, int count)
        => logger.LogInformation("Stopping {Count} module(s) with lifecycle hooks", count);

    private static void LogModuleStopping(ILogger logger, string moduleName)
        => logger.LogDebug("Stopping module '{ModuleName}'", moduleName);

    private static void LogModuleStopped(ILogger logger, string moduleName)
        => logger.LogDebug("Module '{ModuleName}' stopped successfully", moduleName);

    private static void LogModuleStopCancelled(ILogger logger, string moduleName)
        => logger.LogWarning("Module '{ModuleName}' stop was cancelled", moduleName);

    private static void LogModuleStopFailed(ILogger logger, string moduleName, Exception ex)
        => logger.LogError(ex, "Module '{ModuleName}' failed to stop gracefully", moduleName);

    private static void LogAllModulesStopped(ILogger logger, int count)
        => logger.LogInformation("All {Count} module(s) stopped", count);

#pragma warning restore CA1873
#pragma warning restore CA1848
}
