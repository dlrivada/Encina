namespace Encina.Sharding.ReferenceTables;

/// <summary>
/// Configuration options for a single reference table registration.
/// </summary>
/// <remarks>
/// <para>
/// These options are provided per-table when calling <c>AddReferenceTable&lt;T&gt;()</c>
/// on the sharding options builder:
/// </para>
/// <code>
/// options.AddReferenceTable&lt;Country&gt;(rt =>
/// {
///     rt.RefreshStrategy = RefreshStrategy.Polling;
///     rt.PrimaryShardId = "shard-0";
///     rt.PollingInterval = TimeSpan.FromMinutes(10);
///     rt.BatchSize = 500;
///     rt.SyncOnStartup = true;
/// });
/// </code>
/// </remarks>
public sealed class ReferenceTableOptions
{
    /// <summary>
    /// Gets or sets the strategy used to detect and propagate changes from the primary shard.
    /// </summary>
    /// <value>Defaults to <see cref="ReferenceTables.RefreshStrategy.Polling"/>.</value>
    public RefreshStrategy RefreshStrategy { get; set; } = RefreshStrategy.Polling;

    /// <summary>
    /// Gets or sets the shard ID that holds the authoritative copy of the reference data.
    /// All writes should go to this shard, and replication reads from it.
    /// </summary>
    /// <value>Defaults to <c>null</c>, which means the first shard in the topology is used.</value>
    public string? PrimaryShardId { get; set; }

    /// <summary>
    /// Gets or sets the interval between polling cycles for change detection.
    /// Only applicable when <see cref="RefreshStrategy"/> is <see cref="ReferenceTables.RefreshStrategy.Polling"/>.
    /// </summary>
    /// <value>Defaults to 5 minutes.</value>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the maximum number of rows to upsert in a single batch during replication.
    /// </summary>
    /// <value>Defaults to 1000.</value>
    public int BatchSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets whether the reference table should be synchronized on application startup.
    /// </summary>
    /// <value>Defaults to <c>true</c>.</value>
    public bool SyncOnStartup { get; set; } = true;
}
