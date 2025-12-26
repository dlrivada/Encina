using Encina.Marten.Projections;
using Encina.Marten.Snapshots;
using Encina.Marten.Versioning;
using Encina.Messaging.Health;

namespace Encina.Marten;

/// <summary>
/// Configuration options for Encina Marten integration.
/// </summary>
public sealed class EncinaMartenOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to automatically publish domain events
    /// from aggregates after command execution. Default is true.
    /// </summary>
    public bool AutoPublishDomainEvents { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to use optimistic concurrency
    /// when saving aggregates. Default is true.
    /// </summary>
    public bool UseOptimisticConcurrency { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to throw on concurrency conflicts.
    /// When false, returns a EncinaError instead. Default is false.
    /// </summary>
    public bool ThrowOnConcurrencyConflict { get; set; }

    /// <summary>
    /// Gets or sets the default stream prefix for event streams.
    /// Default is empty (no prefix).
    /// </summary>
    public string StreamPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Gets the provider health check options.
    /// </summary>
    public ProviderHealthCheckOptions ProviderHealthCheck { get; } = new();

    /// <summary>
    /// Gets the projection options for CQRS read models.
    /// </summary>
    public ProjectionOptions Projections { get; } = new();

    /// <summary>
    /// Gets the snapshot options for optimizing aggregate loading.
    /// </summary>
    /// <example>
    /// <code>
    /// services.AddEncinaMarten(options =>
    /// {
    ///     options.Snapshots.Enabled = true;
    ///     options.Snapshots.SnapshotEvery = 100;
    ///     options.Snapshots.KeepSnapshots = 3;
    ///
    ///     // Configure specific aggregate
    ///     options.Snapshots.ConfigureAggregate&lt;Order&gt;(
    ///         snapshotEvery: 50,
    ///         keepSnapshots: 5);
    /// });
    /// </code>
    /// </example>
    public SnapshotOptions Snapshots { get; } = new();

    /// <summary>
    /// Gets the event versioning options for schema evolution support.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Event versioning enables transparent migration of old event schemas to new ones
    /// during event replay. This is essential for long-term event store maintenance.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaMarten(options =>
    /// {
    ///     options.EventVersioning.Enabled = true;
    ///
    ///     // Register upcaster by type
    ///     options.EventVersioning.AddUpcaster&lt;OrderCreatedV1ToV2Upcaster&gt;();
    ///
    ///     // Register inline upcaster
    ///     options.EventVersioning.AddUpcaster&lt;OrderCreatedV1, OrderCreatedV2&gt;(
    ///         old => new OrderCreatedV2(old.OrderId, old.CustomerName, "unknown@example.com"));
    ///
    ///     // Scan assembly for upcasters
    ///     options.EventVersioning.ScanAssembly(typeof(Program).Assembly);
    /// });
    /// </code>
    /// </example>
    public EventVersioningOptions EventVersioning { get; } = new();
}
