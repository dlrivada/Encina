namespace Encina.Sharding.ReferenceTables;

/// <summary>
/// Defines the strategy used to detect and propagate changes from the primary shard
/// to all replica shards for a reference table.
/// </summary>
/// <remarks>
/// <para>
/// Each strategy represents a trade-off between latency, resource usage, and complexity:
/// </para>
/// <list type="bullet">
/// <item>
/// <term><see cref="CdcDriven"/></term>
/// <description>Near-real-time replication via Change Data Capture — lowest latency but
/// requires CDC infrastructure.</description>
/// </item>
/// <item>
/// <term><see cref="Polling"/></term>
/// <description>Periodic hash-based change detection — moderate latency with simple
/// infrastructure requirements.</description>
/// </item>
/// <item>
/// <term><see cref="Manual"/></term>
/// <description>Replication triggered explicitly via <c>IReferenceTableReplicator</c> —
/// full control for deployment-time or administrative sync.</description>
/// </item>
/// </list>
/// </remarks>
public enum RefreshStrategy
{
    /// <summary>
    /// Changes are detected via Change Data Capture (CDC) and propagated in near-real-time.
    /// Requires a configured <c>ICdcConnector</c> for the primary shard.
    /// </summary>
    CdcDriven = 0,

    /// <summary>
    /// Changes are detected periodically by comparing content hashes between the primary
    /// shard and replica shards. The polling interval is configured via
    /// <see cref="ReferenceTableOptions.PollingInterval"/>.
    /// </summary>
    Polling = 1,

    /// <summary>
    /// No automatic change detection. Replication is triggered explicitly by calling
    /// <see cref="IReferenceTableReplicator.ReplicateAsync{TEntity}"/> or
    /// <see cref="IReferenceTableReplicator.ReplicateAllAsync"/>.
    /// </summary>
    Manual = 2
}
