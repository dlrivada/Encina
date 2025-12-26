namespace Encina.Marten.Snapshots;

/// <summary>
/// Configuration options for snapshot behavior.
/// </summary>
public sealed class SnapshotOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether snapshotting is enabled.
    /// Default is false.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the default number of events after which a snapshot should be created.
    /// Default is 100 events.
    /// </summary>
    /// <remarks>
    /// This value can be overridden per aggregate type using <see cref="ConfigureAggregate{TAggregate}"/>.
    /// </remarks>
    public int SnapshotEvery { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum number of snapshots to retain per aggregate.
    /// Older snapshots beyond this limit will be deleted.
    /// Default is 3 (keep last 3 snapshots).
    /// </summary>
    /// <remarks>
    /// Set to 0 to keep all snapshots (not recommended for production).
    /// </remarks>
    public int KeepSnapshots { get; set; } = 3;

    /// <summary>
    /// Gets or sets a value indicating whether to create snapshots asynchronously
    /// after the save operation completes. Default is true.
    /// </summary>
    /// <remarks>
    /// When true, snapshot creation happens in the background and does not
    /// block the command execution. When false, snapshots are created
    /// synchronously as part of the save operation.
    /// </remarks>
    public bool AsyncSnapshotCreation { get; set; } = true;

    /// <summary>
    /// Gets the aggregate-specific configurations.
    /// </summary>
    internal Dictionary<Type, AggregateSnapshotConfig> AggregateConfigurations { get; } = [];

    /// <summary>
    /// Configures snapshot options for a specific aggregate type.
    /// </summary>
    /// <typeparam name="TAggregate">The aggregate type to configure.</typeparam>
    /// <param name="snapshotEvery">Number of events after which to create a snapshot.</param>
    /// <param name="keepSnapshots">Maximum number of snapshots to retain. Defaults to global setting.</param>
    /// <returns>The options instance for chaining.</returns>
    /// <example>
    /// <code>
    /// options.Snapshots.Enabled = true;
    /// options.Snapshots.SnapshotEvery = 100; // Default for all aggregates
    ///
    /// // Override for specific aggregate
    /// options.Snapshots.ConfigureAggregate&lt;Order&gt;(
    ///     snapshotEvery: 50,  // More frequent snapshots for Order
    ///     keepSnapshots: 5);
    /// </code>
    /// </example>
    public SnapshotOptions ConfigureAggregate<TAggregate>(
        int snapshotEvery,
        int? keepSnapshots = null)
        where TAggregate : class, IAggregate, ISnapshotable<TAggregate>
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(snapshotEvery, 1);

        if (keepSnapshots.HasValue)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(keepSnapshots.Value);
        }

        AggregateConfigurations[typeof(TAggregate)] = new AggregateSnapshotConfig(
            snapshotEvery,
            keepSnapshots ?? KeepSnapshots);

        return this;
    }

    /// <summary>
    /// Gets the snapshot configuration for a specific aggregate type.
    /// </summary>
    /// <typeparam name="TAggregate">The aggregate type.</typeparam>
    /// <returns>The configuration for the aggregate, or null if using defaults.</returns>
    internal AggregateSnapshotConfig GetConfigFor<TAggregate>()
        where TAggregate : class, IAggregate
    {
        if (AggregateConfigurations.TryGetValue(typeof(TAggregate), out var config))
        {
            return config;
        }

        return new AggregateSnapshotConfig(SnapshotEvery, KeepSnapshots);
    }

    /// <summary>
    /// Gets the snapshot configuration for a specific aggregate type.
    /// </summary>
    /// <param name="aggregateType">The aggregate type.</param>
    /// <returns>The configuration for the aggregate.</returns>
    internal AggregateSnapshotConfig GetConfigFor(Type aggregateType)
    {
        ArgumentNullException.ThrowIfNull(aggregateType);

        if (AggregateConfigurations.TryGetValue(aggregateType, out var config))
        {
            return config;
        }

        return new AggregateSnapshotConfig(SnapshotEvery, KeepSnapshots);
    }
}

/// <summary>
/// Configuration for a specific aggregate's snapshot behavior.
/// </summary>
/// <param name="SnapshotEvery">Number of events after which to create a snapshot.</param>
/// <param name="KeepSnapshots">Maximum number of snapshots to retain.</param>
internal sealed record AggregateSnapshotConfig(int SnapshotEvery, int KeepSnapshots);
