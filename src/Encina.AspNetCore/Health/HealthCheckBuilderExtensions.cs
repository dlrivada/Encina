using Encina.AspNetCore.Modules;
using Encina.Caching.Health;
using Encina.Database;
using Encina.IdGeneration.Configuration;
using Encina.IdGeneration.Health;
using Encina.Messaging.Health;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Encina.Modules;
using Encina.Security.Audit;
using Encina.Security.Audit.Health;
using Encina.Sharding.ReferenceTables;
using Encina.Sharding.ReferenceTables.Health;
using Encina.Sharding.TimeBased;
using Encina.Sharding.TimeBased.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using AspNetHealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Encina.AspNetCore.Health;

/// <summary>
/// Extension methods for adding Encina health checks to ASP.NET Core.
/// </summary>
public static class HealthCheckBuilderExtensions
{
    /// <summary>
    /// Default tags for Encina health checks.
    /// </summary>
    private static readonly string[] DefaultTags = ["encina", "ready"];

    // Common tag constants to avoid duplicate string literals
    private const string TagDatabase = "database";
    private const string TagMessaging = "messaging";

    /// <summary>
    /// Adds all registered Encina health checks to the health check builder.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="tags">Additional tags to apply to all health checks.</param>
    /// <param name="failureStatus">The failure status to use. Defaults to <see cref="AspNetHealthStatus.Unhealthy"/>.</param>
    /// <returns>The health checks builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers all <see cref="IEncinaHealthCheck"/> instances that are
    /// registered in the DI container. Each health check is wrapped in an adapter
    /// that converts it to ASP.NET Core's <see cref="IHealthCheck"/> interface.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Services
    ///     .AddHealthChecks()
    ///     .AddEncinaHealthChecks();
    ///
    /// // Or with custom tags
    /// builder.Services
    ///     .AddHealthChecks()
    ///     .AddEncinaHealthChecks(tags: ["live", "ready"]);
    /// </code>
    /// </example>
    public static IHealthChecksBuilder AddEncinaHealthChecks(
        this IHealthChecksBuilder builder,
        IEnumerable<string>? tags = null,
        AspNetHealthStatus? failureStatus = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var allTags = tags is not null
            ? DefaultTags.Concat(tags).Distinct().ToArray()
            : DefaultTags;

        builder.Add(new HealthCheckRegistration(
            "encina",
            sp =>
            {
                var healthChecks = sp.GetServices<IEncinaHealthCheck>();
                return new CompositeEncinaHealthCheck(healthChecks);
            },
            failureStatus,
            allTags));

        return builder;
    }

    /// <summary>
    /// Adds the Outbox health check.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The name of the health check. Defaults to "encina-outbox".</param>
    /// <param name="options">Health check options.</param>
    /// <param name="tags">Additional tags to apply.</param>
    /// <param name="failureStatus">The failure status to use.</param>
    /// <returns>The health checks builder for chaining.</returns>
    public static IHealthChecksBuilder AddEncinaOutbox(
        this IHealthChecksBuilder builder,
        string name = "encina-outbox",
        OutboxHealthCheckOptions? options = null,
        IEnumerable<string>? tags = null,
        AspNetHealthStatus? failureStatus = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var allTags = CombineTags(tags, "outbox", TagDatabase, TagMessaging);

        builder.Add(new HealthCheckRegistration(
            name,
            sp =>
            {
                var store = sp.GetRequiredService<IOutboxStore>();
                return new EncinaHealthCheckAdapter(new OutboxHealthCheck(store, options));
            },
            failureStatus,
            allTags));

        return builder;
    }

    /// <summary>
    /// Adds the Inbox health check.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The name of the health check. Defaults to "encina-inbox".</param>
    /// <param name="tags">Additional tags to apply.</param>
    /// <param name="failureStatus">The failure status to use.</param>
    /// <returns>The health checks builder for chaining.</returns>
    public static IHealthChecksBuilder AddEncinaInbox(
        this IHealthChecksBuilder builder,
        string name = "encina-inbox",
        IEnumerable<string>? tags = null,
        AspNetHealthStatus? failureStatus = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var allTags = CombineTags(tags, "inbox", TagDatabase, TagMessaging);

        builder.Add(new HealthCheckRegistration(
            name,
            sp =>
            {
                var store = sp.GetRequiredService<IInboxStore>();
                return new EncinaHealthCheckAdapter(new InboxHealthCheck(store));
            },
            failureStatus,
            allTags));

        return builder;
    }

    /// <summary>
    /// Adds the Saga health check.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The name of the health check. Defaults to "encina-saga".</param>
    /// <param name="options">Health check options.</param>
    /// <param name="tags">Additional tags to apply.</param>
    /// <param name="failureStatus">The failure status to use.</param>
    /// <returns>The health checks builder for chaining.</returns>
    public static IHealthChecksBuilder AddEncinaSaga(
        this IHealthChecksBuilder builder,
        string name = "encina-saga",
        SagaHealthCheckOptions? options = null,
        IEnumerable<string>? tags = null,
        AspNetHealthStatus? failureStatus = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var allTags = CombineTags(tags, "saga", TagDatabase, TagMessaging);

        builder.Add(new HealthCheckRegistration(
            name,
            sp =>
            {
                var store = sp.GetRequiredService<ISagaStore>();
                return new EncinaHealthCheckAdapter(new SagaHealthCheck(store, options));
            },
            failureStatus,
            allTags));

        return builder;
    }

    /// <summary>
    /// Adds the Scheduling health check.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The name of the health check. Defaults to "encina-scheduling".</param>
    /// <param name="options">Health check options.</param>
    /// <param name="tags">Additional tags to apply.</param>
    /// <param name="failureStatus">The failure status to use.</param>
    /// <returns>The health checks builder for chaining.</returns>
    public static IHealthChecksBuilder AddEncinaScheduling(
        this IHealthChecksBuilder builder,
        string name = "encina-scheduling",
        SchedulingHealthCheckOptions? options = null,
        IEnumerable<string>? tags = null,
        AspNetHealthStatus? failureStatus = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var allTags = CombineTags(tags, "scheduling", TagDatabase, TagMessaging);

        builder.Add(new HealthCheckRegistration(
            name,
            sp =>
            {
                var store = sp.GetRequiredService<IScheduledMessageStore>();
                return new EncinaHealthCheckAdapter(new SchedulingHealthCheck(store, options));
            },
            failureStatus,
            allTags));

        return builder;
    }

    /// <summary>
    /// Adds the database connection pool health check.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The name of the health check. Defaults to "encina-database-pool".</param>
    /// <param name="options">Health check options including pool utilization thresholds.</param>
    /// <param name="tags">Additional tags to apply.</param>
    /// <param name="failureStatus">The failure status to use.</param>
    /// <returns>The health checks builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This health check monitors the <see cref="IDatabaseHealthMonitor"/> registered by a
    /// database provider (ADO.NET, Dapper, EF Core, or MongoDB). It reports pool utilization
    /// against configurable thresholds (80% degraded, 95% unhealthy by default).
    /// </para>
    /// <para>
    /// If no <see cref="IDatabaseHealthMonitor"/> is registered, the health check reports healthy
    /// with a message indicating that pool monitoring is not configured.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Services
    ///     .AddHealthChecks()
    ///     .AddEncinaDatabasePool();
    ///
    /// // Or with custom thresholds
    /// builder.Services
    ///     .AddHealthChecks()
    ///     .AddEncinaDatabasePool(options: new DatabasePoolHealthCheckOptions
    ///     {
    ///         DegradedThreshold = 0.7,
    ///         UnhealthyThreshold = 0.9
    ///     });
    /// </code>
    /// </example>
    public static IHealthChecksBuilder AddEncinaDatabasePool(
        this IHealthChecksBuilder builder,
        string name = DatabasePoolHealthCheck.DefaultName,
        DatabasePoolHealthCheckOptions? options = null,
        IEnumerable<string>? tags = null,
        AspNetHealthStatus? failureStatus = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var allTags = CombineTags(tags, "pool", TagDatabase);

        builder.Add(new HealthCheckRegistration(
            name,
            sp =>
            {
                var monitor = sp.GetService<IDatabaseHealthMonitor>();
                if (monitor is null)
                {
                    return new EmptyHealthCheck();
                }

                return new EncinaHealthCheckAdapter(new DatabasePoolHealthCheck(monitor, options));
            },
            failureStatus,
            allTags));

        return builder;
    }

    /// <summary>
    /// Adds a custom Encina health check.
    /// </summary>
    /// <typeparam name="THealthCheck">The type of health check to add.</typeparam>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The name of the health check.</param>
    /// <param name="tags">Additional tags to apply.</param>
    /// <param name="failureStatus">The failure status to use.</param>
    /// <returns>The health checks builder for chaining.</returns>
    public static IHealthChecksBuilder AddEncinaHealthCheck<THealthCheck>(
        this IHealthChecksBuilder builder,
        string name,
        IEnumerable<string>? tags = null,
        AspNetHealthStatus? failureStatus = null)
        where THealthCheck : class, IEncinaHealthCheck
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var allTags = CombineTags(tags, "encina");

        builder.Services.AddTransient<THealthCheck>();

        builder.Add(new HealthCheckRegistration(
            name,
            sp =>
            {
                var healthCheck = sp.GetRequiredService<THealthCheck>();
                return new EncinaHealthCheckAdapter(healthCheck);
            },
            failureStatus,
            allTags));

        return builder;
    }

    /// <summary>
    /// Adds health checks from all registered modules that implement <see cref="IModuleWithHealthChecks"/>.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="tags">Additional tags to apply to all module health checks.</param>
    /// <param name="failureStatus">The failure status to use.</param>
    /// <returns>The health checks builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers individual health checks for each module that implements
    /// <see cref="IModuleWithHealthChecks"/>. Each module's health checks are registered
    /// with the module name as a tag for easy filtering.
    /// </para>
    /// <para>
    /// Health checks are named using the pattern "module-{moduleName}-{healthCheckName}".
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Services
    ///     .AddHealthChecks()
    ///     .AddEncinaModuleHealthChecks();
    ///
    /// // Query module-specific health checks
    /// app.MapHealthChecks("/health/modules", new HealthCheckOptions
    /// {
    ///     Predicate = check => check.Tags.Contains("modules")
    /// });
    ///
    /// // Query specific module's health checks
    /// app.MapHealthChecks("/health/modules/orders", new HealthCheckOptions
    /// {
    ///     Predicate = check => check.Tags.Contains("module-orders")
    /// });
    /// </code>
    /// </example>
    public static IHealthChecksBuilder AddEncinaModuleHealthChecks(
        this IHealthChecksBuilder builder,
        IEnumerable<string>? tags = null,
        AspNetHealthStatus? failureStatus = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Add(new HealthCheckRegistration(
            "encina-modules",
            sp =>
            {
                var registry = sp.GetService<IModuleRegistry>();
                if (registry is null)
                {
                    return new EmptyHealthCheck();
                }

                var healthChecks = new List<IEncinaHealthCheck>();

                foreach (var module in registry.Modules.OfType<IModuleWithHealthChecks>())
                {
                    healthChecks.AddRange(module.GetHealthChecks());
                }

                return new CompositeEncinaHealthCheck(healthChecks);
            },
            failureStatus,
            CombineTags(tags, "modules")));

        return builder;
    }

    /// <summary>
    /// Adds health checks from a specific module.
    /// </summary>
    /// <typeparam name="TModule">The module type that implements health checks.</typeparam>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="tags">Additional tags to apply to the module's health checks.</param>
    /// <param name="failureStatus">The failure status to use.</param>
    /// <returns>The health checks builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Use this method when you want to register health checks from a specific module
    /// rather than all modules. The health checks are aggregated into a single
    /// composite health check for the module.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Services
    ///     .AddHealthChecks()
    ///     .AddEncinaModuleHealthChecks&lt;OrdersModule&gt;();
    /// </code>
    /// </example>
    public static IHealthChecksBuilder AddEncinaModuleHealthChecks<TModule>(
        this IHealthChecksBuilder builder,
        IEnumerable<string>? tags = null,
        AspNetHealthStatus? failureStatus = null)
        where TModule : class, IModuleWithHealthChecks
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Add(new HealthCheckRegistration(
            $"encina-module-{typeof(TModule).Name.ToLowerInvariant().Replace("module", "")}",
            sp =>
            {
                var module = sp.GetService<TModule>();
                if (module is null)
                {
                    return new EmptyHealthCheck();
                }

                var healthChecks = module.GetHealthChecks();
                return new CompositeEncinaHealthCheck(healthChecks);
            },
            failureStatus,
            CombineTags(tags, "modules", $"module-{typeof(TModule).Name.ToLowerInvariant().Replace("module", "")}")));

        return builder;
    }

    /// <summary>
    /// Adds the ID generation health check.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The name of the health check. Defaults to "encina-id-generation".</param>
    /// <param name="options">Health check options including clock drift threshold.</param>
    /// <param name="tags">Additional tags to apply.</param>
    /// <param name="failureStatus">The failure status to use.</param>
    /// <returns>The health checks builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This health check monitors ID generation infrastructure health including clock drift
    /// detection and Snowflake machine ID configuration. If <see cref="SnowflakeOptions"/>
    /// is registered in DI, the health check also reports machine ID and shard bit allocation.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Services
    ///     .AddHealthChecks()
    ///     .AddEncinaIdGenerationHealthCheck();
    ///
    /// // Or with custom threshold
    /// builder.Services
    ///     .AddHealthChecks()
    ///     .AddEncinaIdGenerationHealthCheck(options: new IdGeneratorHealthCheckOptions
    ///     {
    ///         ClockDriftThresholdMs = 200
    ///     });
    /// </code>
    /// </example>
    public static IHealthChecksBuilder AddEncinaIdGenerationHealthCheck(
        this IHealthChecksBuilder builder,
        string name = IdGeneratorHealthCheck.DefaultName,
        IdGeneratorHealthCheckOptions? options = null,
        IEnumerable<string>? tags = null,
        AspNetHealthStatus? failureStatus = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var allTags = CombineTags(tags, "id-generation");

        builder.Add(new HealthCheckRegistration(
            name,
            sp =>
            {
                var snowflakeOptions = sp.GetService<SnowflakeOptions>();
                var timeProvider = sp.GetService<TimeProvider>() ?? TimeProvider.System;
                return new EncinaHealthCheckAdapter(
                    new IdGeneratorHealthCheck(options, snowflakeOptions, timeProvider));
            },
            failureStatus,
            allTags));

        return builder;
    }

    /// <summary>
    /// Adds the reference table replication health check.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The name of the health check. Defaults to "encina-reference-table-replication".</param>
    /// <param name="options">Health check options including staleness thresholds.</param>
    /// <param name="tags">Additional tags to apply.</param>
    /// <param name="failureStatus">The failure status to use.</param>
    /// <returns>The health checks builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This health check monitors reference table replication staleness by checking the last
    /// replication time for each registered reference table against configurable thresholds.
    /// Tables that have never been replicated are treated as unhealthy.
    /// </para>
    /// <para>
    /// Requires <see cref="IReferenceTableRegistry"/> and <see cref="IReferenceTableStateStore"/>
    /// to be registered in the DI container (automatically done when reference table replication
    /// is enabled).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Services
    ///     .AddHealthChecks()
    ///     .AddEncinaReferenceTableReplication();
    ///
    /// // Or with custom thresholds
    /// builder.Services
    ///     .AddHealthChecks()
    ///     .AddEncinaReferenceTableReplication(options: new ReferenceTableHealthCheckOptions
    ///     {
    ///         UnhealthyThreshold = TimeSpan.FromMinutes(10),
    ///         DegradedThreshold = TimeSpan.FromMinutes(2)
    ///     });
    /// </code>
    /// </example>
    public static IHealthChecksBuilder AddEncinaReferenceTableReplication(
        this IHealthChecksBuilder builder,
        string name = ReferenceTableHealthCheck.DefaultName,
        ReferenceTableHealthCheckOptions? options = null,
        IEnumerable<string>? tags = null,
        AspNetHealthStatus? failureStatus = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var allTags = CombineTags(tags, "sharding", "replication", TagDatabase);

        builder.Add(new HealthCheckRegistration(
            name,
            sp =>
            {
                var registry = sp.GetRequiredService<IReferenceTableRegistry>();
                var stateStore = sp.GetRequiredService<IReferenceTableStateStore>();
                return new EncinaHealthCheckAdapter(
                    new ReferenceTableHealthCheck(registry, stateStore, options));
            },
            failureStatus,
            allTags));

        return builder;
    }

    /// <summary>
    /// Adds the tier transition health check for time-based sharding.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The name of the health check. Defaults to "encina-tier-transition".</param>
    /// <param name="options">Health check options including per-tier age thresholds.</param>
    /// <param name="tags">Additional tags to apply.</param>
    /// <param name="failureStatus">The failure status to use.</param>
    /// <returns>The health checks builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This health check monitors whether tier transitions are running on schedule.
    /// Shards that have exceeded their expected tier age are flagged as degraded or unhealthy.
    /// </para>
    /// <para>
    /// Requires <see cref="ITierStore"/> to be registered in the DI container (automatically done
    /// when time-based sharding is enabled via <c>UseTimeBasedRouting</c>).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Services
    ///     .AddHealthChecks()
    ///     .AddEncinaTierTransition();
    ///
    /// // Or with custom thresholds
    /// builder.Services
    ///     .AddHealthChecks()
    ///     .AddEncinaTierTransition(options: new TierTransitionHealthCheckOptions
    ///     {
    ///         MaxExpectedHotAgeDays = 45,
    ///         MaxExpectedWarmAgeDays = 120,
    ///     });
    /// </code>
    /// </example>
    public static IHealthChecksBuilder AddEncinaTierTransition(
        this IHealthChecksBuilder builder,
        string name = TierTransitionHealthCheck.DefaultName,
        TierTransitionHealthCheckOptions? options = null,
        IEnumerable<string>? tags = null,
        AspNetHealthStatus? failureStatus = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var allTags = CombineTags(tags, "sharding", "tiering");

        builder.Add(new HealthCheckRegistration(
            name,
            sp =>
            {
                var tierStore = sp.GetRequiredService<ITierStore>();
                var timeProvider = sp.GetService<TimeProvider>() ?? TimeProvider.System;
                return new EncinaHealthCheckAdapter(
                    new TierTransitionHealthCheck(tierStore, options, timeProvider));
            },
            failureStatus,
            allTags));

        return builder;
    }

    /// <summary>
    /// Adds the shard creation health check for time-based sharding.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The name of the health check. Defaults to "encina-shard-creation".</param>
    /// <param name="options">Health check options including period and prefix configuration.</param>
    /// <param name="tags">Additional tags to apply.</param>
    /// <param name="failureStatus">The failure status to use.</param>
    /// <returns>The health checks builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This health check monitors whether expected time-period shards exist. A missing current-period
    /// shard is unhealthy; a missing next-period shard (when close to period end) is degraded.
    /// </para>
    /// <para>
    /// Requires <see cref="ITierStore"/> to be registered in the DI container (automatically done
    /// when time-based sharding is enabled via <c>UseTimeBasedRouting</c>).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Services
    ///     .AddHealthChecks()
    ///     .AddEncinaShardCreation(options: new ShardCreationHealthCheckOptions
    ///     {
    ///         Period = ShardPeriod.Monthly,
    ///         ShardIdPrefix = "orders",
    ///     });
    /// </code>
    /// </example>
    public static IHealthChecksBuilder AddEncinaShardCreation(
        this IHealthChecksBuilder builder,
        string name = ShardCreationHealthCheck.DefaultName,
        ShardCreationHealthCheckOptions? options = null,
        IEnumerable<string>? tags = null,
        AspNetHealthStatus? failureStatus = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var allTags = CombineTags(tags, "sharding", "tiering");

        builder.Add(new HealthCheckRegistration(
            name,
            sp =>
            {
                var tierStore = sp.GetRequiredService<ITierStore>();
                var timeProvider = sp.GetService<TimeProvider>() ?? TimeProvider.System;
                return new EncinaHealthCheckAdapter(
                    new ShardCreationHealthCheck(tierStore, options, timeProvider));
            },
            failureStatus,
            allTags));

        return builder;
    }

    /// <summary>
    /// Adds the audit store health check.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The name of the health check. Defaults to "encina-audit".</param>
    /// <param name="tags">Additional tags to apply.</param>
    /// <param name="failureStatus">The failure status to use.</param>
    /// <returns>The health checks builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This health check verifies that the audit store is accessible by performing
    /// a lightweight query. Requires <see cref="IAuditStore"/> to be registered.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Services
    ///     .AddHealthChecks()
    ///     .AddEncinaAudit();
    /// </code>
    /// </example>
    public static IHealthChecksBuilder AddEncinaAudit(
        this IHealthChecksBuilder builder,
        string name = AuditStoreHealthCheck.DefaultName,
        IEnumerable<string>? tags = null,
        AspNetHealthStatus? failureStatus = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var allTags = CombineTags(tags, "audit", "security");

        builder.Add(new HealthCheckRegistration(
            name,
            sp =>
            {
                var store = sp.GetRequiredService<IAuditStore>();
                return new EncinaHealthCheckAdapter(
                    new AuditStoreHealthCheck(store));
            },
            failureStatus,
            allTags));

        return builder;
    }

    /// <summary>
    /// Adds the query cache health check.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The name of the health check. Defaults to "encina-query-cache".</param>
    /// <param name="tags">Additional tags to apply.</param>
    /// <param name="failureStatus">The failure status to use.</param>
    /// <returns>The health checks builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This health check verifies that the cache provider is accessible. Returns degraded
    /// when no cache provider is registered, and unhealthy on cache errors.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Services
    ///     .AddHealthChecks()
    ///     .AddEncinaQueryCache();
    /// </code>
    /// </example>
    public static IHealthChecksBuilder AddEncinaQueryCache(
        this IHealthChecksBuilder builder,
        string name = QueryCacheHealthCheck.DefaultName,
        IEnumerable<string>? tags = null,
        AspNetHealthStatus? failureStatus = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var allTags = CombineTags(tags, "cache", "query-cache");

        builder.Add(new HealthCheckRegistration(
            name,
            sp => new EncinaHealthCheckAdapter(
                new QueryCacheHealthCheck(sp)),
            failureStatus,
            allTags));

        return builder;
    }

    private static string[] CombineTags(IEnumerable<string>? additionalTags, params string[] baseTags)
    {
        var combinedTags = new List<string>(DefaultTags);
        combinedTags.AddRange(baseTags);

        if (additionalTags is not null)
        {
            combinedTags.AddRange(additionalTags);
        }

        return [.. combinedTags.Distinct()];
    }
}
