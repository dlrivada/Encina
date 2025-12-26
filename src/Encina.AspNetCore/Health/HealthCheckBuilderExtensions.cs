using Encina.AspNetCore.Modules;
using Encina.Messaging.Health;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Encina.Modules;
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

        var allTags = CombineTags(tags, "outbox", "database", "messaging");

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

        var allTags = CombineTags(tags, "inbox", "database", "messaging");

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

        var allTags = CombineTags(tags, "saga", "database", "messaging");

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

        var allTags = CombineTags(tags, "scheduling", "database", "messaging");

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
