using Confluent.Kafka;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.Kafka.Health;

/// <summary>
/// Health check for Kafka message broker connectivity.
/// </summary>
public sealed class KafkaHealthCheck : EncinaHealthCheck
{
    /// <summary>
    /// The default name for the Kafka health check.
    /// </summary>
    public const string DefaultName = "encina-kafka";

    private readonly IServiceProvider _serviceProvider;
    private readonly ProviderHealthCheckOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve IProducer from.</param>
    /// <param name="options">Configuration for the health check. If null, default options are used.</param>
    public KafkaHealthCheck(
        IServiceProvider serviceProvider,
        ProviderHealthCheckOptions? options)
        : base(options?.Name ?? DefaultName, options?.Tags ?? ["encina", "messaging", "kafka", "ready"])
    {
        _serviceProvider = serviceProvider;
        _options = options ?? new ProviderHealthCheckOptions();
    }

    /// <inheritdoc/>
    protected override Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            var producer = _serviceProvider.GetRequiredService<IProducer<string, byte[]>>();

            // Check if producer handle is valid by accessing it
            // If the handle is invalid, accessing it will throw
            _ = producer.Handle;

            return Task.FromResult(HealthCheckResult.Healthy($"{Name} is connected"));
        }
        catch (KafkaException ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy($"{Name} error: {ex.Error.Reason}"));
        }
    }
}
