using Encina.OpenTelemetry.Behaviors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Encina.OpenTelemetry;

/// <summary>
/// Extension methods for configuring Encina OpenTelemetry integration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina OpenTelemetry instrumentation to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddEncinaOpenTelemetry(
        this IServiceCollection services,
        Action<EncinaOpenTelemetryOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new EncinaOpenTelemetryOptions();
        configure?.Invoke(options);

        services.TryAddSingleton(options);

        // Register messaging enricher behavior if enabled
        if (options.EnableMessagingEnrichers)
        {
            services.TryAddTransient(typeof(IPipelineBehavior<,>), typeof(MessagingEnricherPipelineBehavior<,>));
        }

        // Register database pool metrics initialization as a hosted service.
        // DatabasePoolMetrics creates ObservableGauge instruments on the static "Encina"
        // meter during construction. Registration is conditional on IDatabaseHealthMonitor
        // being available.
        services.AddHostedService<DatabasePoolMetricsInitializer>();

        return services;
    }

    /// <summary>
    /// Configures OpenTelemetry with Encina instrumentation.
    /// </summary>
    /// <param name="builder">The OpenTelemetry builder.</param>
    /// <param name="options">The OpenTelemetry options.</param>
    /// <returns>The builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="builder"/> is null.</exception>
    public static OpenTelemetryBuilder WithEncina(
        this OpenTelemetryBuilder builder,
        EncinaOpenTelemetryOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        options ??= new EncinaOpenTelemetryOptions();

        builder.ConfigureResource(resource =>
            resource.AddService(
                serviceName: options.ServiceName,
                serviceVersion: options.ServiceVersion));

        builder.WithTracing(tracing =>
        {
            tracing.AddSource("Encina");
        });

        builder.WithMetrics(metrics =>
        {
            metrics.AddMeter("Encina");
            metrics.AddRuntimeInstrumentation();
        });

        return builder;
    }

    /// <summary>
    /// Adds Encina tracing to the TracerProviderBuilder.
    /// </summary>
    /// <param name="builder">The tracer provider builder.</param>
    /// <returns>The builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="builder"/> is null.</exception>
    public static TracerProviderBuilder AddEncinaInstrumentation(this TracerProviderBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddSource("Encina");
    }

    /// <summary>
    /// Adds Encina metrics to the MeterProviderBuilder.
    /// </summary>
    /// <param name="builder">The meter provider builder.</param>
    /// <returns>The builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="builder"/> is null.</exception>
    public static MeterProviderBuilder AddEncinaInstrumentation(this MeterProviderBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddMeter("Encina");
    }
}
