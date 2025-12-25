using Encina.Messaging.Health;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
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
