using Encina.Modules.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Encina.OpenTelemetry.Modules;

/// <summary>
/// Hosted service that initializes <see cref="ModuleMetrics"/> on application startup.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ModuleMetrics"/> registers metric instruments on the static <c>Encina</c>
/// meter during construction. This hosted service ensures the metrics instance is created
/// during application startup so that instruments are active when an OpenTelemetry meter
/// listener starts collecting.
/// </para>
/// <para>
/// If no <see cref="ModuleMetricsCallbacks"/> is registered in the service collection,
/// the metrics are created without observable gauges. The callbacks are registered by the
/// <c>Encina</c> package when modular monolith features are enabled.
/// </para>
/// </remarks>
internal sealed class ModuleMetricsInitializer : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private ModuleMetrics? _metrics;

    public ModuleMetricsInitializer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var callbacks = _serviceProvider.GetService<ModuleMetricsCallbacks>();
        _metrics = new ModuleMetrics(callbacks);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
