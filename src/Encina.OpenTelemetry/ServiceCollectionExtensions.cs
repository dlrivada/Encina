using Encina.Caching;
using Encina.DomainModeling;
using Encina.IdGeneration.Diagnostics;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Encina.OpenTelemetry.Audit;
using Encina.OpenTelemetry.Behaviors;
using Encina.OpenTelemetry.BulkOperations;
using Encina.OpenTelemetry.Cdc;
using Encina.OpenTelemetry.IdGeneration;
using Encina.OpenTelemetry.MessagingStores;
using Encina.OpenTelemetry.Migrations;
using Encina.OpenTelemetry.Modules;
using Encina.OpenTelemetry.QueryCache;
using Encina.OpenTelemetry.ReferenceTable;
using Encina.OpenTelemetry.Repository;
using Encina.OpenTelemetry.Resharding;
using Encina.OpenTelemetry.Sharding;
using Encina.OpenTelemetry.SoftDelete;
using Encina.OpenTelemetry.Tenancy;
using Encina.OpenTelemetry.UnitOfWork;
using Encina.Security.Audit;
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

        // Register co-location metrics initialization as a hosted service.
        // ColocationMetrics creates ObservableGauge instruments on the static "Encina"
        // meter during construction. Registration is conditional on ColocationGroupRegistry
        // being available.
        services.AddHostedService<ColocationMetricsInitializer>();

        // Register ID generation metrics initialization as a hosted service.
        // IdGenerationMetrics creates Counter and Histogram instruments on the static
        // "Encina" meter during construction.
        services.AddHostedService<IdGenerationMetricsInitializer>();

        // Register sharded CDC metrics initialization as a hosted service.
        // ShardedCdcMetrics creates Counter and ObservableGauge instruments on the static
        // "Encina" meter during construction. Registration is conditional on
        // ShardedCdcMetricsCallbacks being available (registered by Encina.Cdc).
        services.AddHostedService<ShardedCdcMetricsInitializer>();

        // Register reference table replication metrics initialization as a hosted service.
        // ReferenceTableMetrics creates Histogram, Counter, UpDownCounter, and ObservableGauge
        // instruments on the static "Encina" meter during construction. Registration is
        // conditional on ReferenceTableMetricsCallbacks being available.
        services.AddHostedService<ReferenceTableMetricsInitializer>();

        // Register time-based sharding metrics initialization as a hosted service.
        // TimeBasedShardingMetrics creates Counter, Histogram, and ObservableGauge instruments
        // on the static "Encina" meter during construction. Registration is conditional on
        // TimeBasedShardingMetricsCallbacks being available (registered by Encina.Sharding).
        services.AddHostedService<TimeBasedShardingMetricsInitializer>();

        // Register migration metrics initialization as a hosted service.
        // MigrationMetrics creates Counter, Histogram, and ObservableGauge instruments
        // on the static "Encina" meter during construction. Registration is conditional on
        // MigrationMetricsCallbacks or IShardedMigrationCoordinator being available.
        services.AddHostedService<MigrationMetricsInitializer>();

        // Register resharding metrics initialization as a hosted service.
        // ReshardingMetrics creates Counter, Histogram, and ObservableGauge instruments
        // on the static "Encina" meter during construction. Registration is conditional on
        // ReshardingMetricsCallbacks or IReshardingOrchestrator being available.
        services.AddHostedService<ReshardingMetricsInitializer>();

        // Register repository metrics initialization as a hosted service.
        services.AddHostedService<RepositoryMetricsInitializer>();

        // Register unit of work metrics initialization as a hosted service.
        services.AddHostedService<UnitOfWorkMetricsInitializer>();

        // Register bulk operations metrics initialization as a hosted service.
        services.AddHostedService<BulkOperationsMetricsInitializer>();

        // Register audit metrics initialization as a hosted service.
        services.AddHostedService<AuditMetricsInitializer>();

        // Register soft delete metrics initialization as a hosted service.
        services.AddHostedService<SoftDeleteMetricsInitializer>();

        // Register tenancy metrics initialization as a hosted service.
        services.AddHostedService<TenancyMetricsInitializer>();

        // Register query cache metrics initialization as a hosted service.
        services.AddHostedService<QueryCacheMetricsInitializer>();

        // Register module metrics initialization as a hosted service.
        // ModuleMetrics creates an ObservableGauge for active module count.
        // Registration is conditional on ModuleMetricsCallbacks being available.
        services.AddHostedService<ModuleMetricsInitializer>();

        // Register messaging store metrics initialization as a hosted service.
        // MessagingStoreMetrics creates ObservableGauge instruments for outbox pending
        // count and active saga count. Registration is conditional on
        // MessagingStoreMetricsCallbacks being available.
        services.AddHostedService<MessagingStoreMetricsInitializer>();

        // Register instrumented decorators for distributed tracing.
        // Each decorator wraps the existing service registration and adds
        // OpenTelemetry activity spans around operations. Decorators are only
        // applied when the inner service is already registered.
        DecorateService<IUnitOfWork>(services, inner => new InstrumentedUnitOfWork(inner));
        DecorateService<IOutboxStore>(services, inner => new InstrumentedOutboxStore(inner));
        DecorateService<IInboxStore>(services, inner => new InstrumentedInboxStore(inner));
        DecorateService<ISagaStore>(services, inner => new InstrumentedSagaStore(inner));
        DecorateService<IScheduledMessageStore>(services, inner => new InstrumentedScheduledMessageStore(inner));
        DecorateService<IAuditStore>(services, inner => new InstrumentedAuditStore(inner));
        DecorateService<ICacheProvider>(services, inner => new InstrumentedCacheProvider(inner));

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
            tracing.AddSource("Encina.Sharding");
            tracing.AddSource("Encina.Cdc.Sharded");
            tracing.AddSource("Encina.ReferenceTable");
            tracing.AddSource(MigrationActivitySource.SourceName);
            tracing.AddSource(ReshardingActivitySource.SourceName);
            tracing.AddSource(IdGenerationActivitySource.SourceName);

            // Data access observability
            tracing.AddSource("Encina.Repository");
            tracing.AddSource("Encina.UnitOfWork");
            tracing.AddSource("Encina.BulkOperations");
            tracing.AddSource("Encina.Audit");

            // EF Core feature observability
            tracing.AddSource("Encina.SoftDelete");
            tracing.AddSource("Encina.Tenancy");
            tracing.AddSource("Encina.QueryCache");

            // Modular monolith observability
            tracing.AddSource("Encina.Modules");

            // Messaging store observability
            tracing.AddSource("Encina.Messaging.Outbox");
            tracing.AddSource("Encina.Messaging.Inbox");
            tracing.AddSource("Encina.Messaging.Saga");
            tracing.AddSource("Encina.Messaging.Scheduling");
        });

        builder.WithMetrics(metrics =>
        {
            metrics.AddMeter("Encina");
            metrics.AddRuntimeInstrumentation();
        });

        return builder;
    }

    /// <summary>
    /// Decorates an existing service registration with an instrumented wrapper.
    /// If the service is not registered, this is a no-op.
    /// </summary>
    private static void DecorateService<TService>(
        IServiceCollection services,
        Func<TService, TService> decoratorFactory)
        where TService : class
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(TService));
        if (descriptor is null)
        {
            return;
        }

        services.Remove(descriptor);

        services.Add(ServiceDescriptor.Describe(
            typeof(TService),
            sp =>
            {
                var inner = ResolveFromDescriptor<TService>(sp, descriptor);
                return decoratorFactory(inner);
            },
            descriptor.Lifetime));
    }

    private static T ResolveFromDescriptor<T>(IServiceProvider sp, ServiceDescriptor descriptor)
        where T : class
    {
        if (descriptor.ImplementationInstance is T instance)
        {
            return instance;
        }

        if (descriptor.ImplementationFactory is not null)
        {
            return (T)descriptor.ImplementationFactory(sp);
        }

        if (descriptor.ImplementationType is not null)
        {
            return (T)ActivatorUtilities.CreateInstance(sp, descriptor.ImplementationType);
        }

        if (descriptor.IsKeyedService)
        {
            throw new InvalidOperationException(
                $"Cannot decorate keyed service {typeof(T).Name}.");
        }

        throw new InvalidOperationException(
            $"Cannot resolve {typeof(T).Name} from existing service descriptor.");
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
