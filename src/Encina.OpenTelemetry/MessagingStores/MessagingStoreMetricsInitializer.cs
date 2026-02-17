using Encina.Messaging.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Encina.OpenTelemetry.MessagingStores;

/// <summary>
/// Hosted service that initializes <see cref="MessagingStoreMetrics"/> on application startup.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MessagingStoreMetrics"/> registers metric instruments on the static <c>Encina</c>
/// meter during construction. This hosted service ensures the metrics instance is created
/// during application startup so that instruments are active when an OpenTelemetry meter
/// listener starts collecting.
/// </para>
/// <para>
/// If no <see cref="MessagingStoreMetricsCallbacks"/> is registered in the service collection,
/// the metrics are created without observable gauges. The callbacks are registered by the
/// messaging infrastructure when outbox/saga stores are enabled.
/// </para>
/// </remarks>
internal sealed class MessagingStoreMetricsInitializer : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private MessagingStoreMetrics? _metrics;

    public MessagingStoreMetricsInitializer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var callbacks = _serviceProvider.GetService<MessagingStoreMetricsCallbacks>();
        _metrics = new MessagingStoreMetrics(callbacks);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
