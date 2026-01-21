namespace Encina.MongoDB.ReadWriteSeparation;

/// <summary>
/// Specifies the read preference mode for MongoDB operations.
/// </summary>
/// <remarks>
/// <para>
/// Read preference describes how MongoDB clients route read operations to members of a replica set.
/// In read/write separation scenarios, read preferences control how queries are distributed
/// between primary and secondary nodes.
/// </para>
/// <para>
/// <b>Important:</b> Read operations routed to secondaries may return stale data due to
/// replication lag. For read-after-write consistency, use the <c>ForceWriteDatabaseAttribute</c>
/// on query classes that need to read from the primary.
/// </para>
/// </remarks>
public enum MongoReadPreference
{
    /// <summary>
    /// All read operations use only the primary member.
    /// </summary>
    /// <remarks>
    /// This is the default mode. Use this when you need to read the most recent
    /// version of documents and cannot tolerate stale data.
    /// </remarks>
    Primary = 0,

    /// <summary>
    /// Reads use the primary, but if unavailable, reads from a secondary.
    /// </summary>
    /// <remarks>
    /// Use this for applications that require the freshest data when available
    /// but can tolerate reading from secondaries if the primary is unavailable.
    /// </remarks>
    PrimaryPreferred = 1,

    /// <summary>
    /// All read operations use a secondary member.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this for offloading read workloads from the primary. Reads may return
    /// stale data due to replication lag.
    /// </para>
    /// <para>
    /// If no secondary is available, the operation fails.
    /// </para>
    /// </remarks>
    Secondary = 2,

    /// <summary>
    /// Reads use a secondary, but if unavailable, reads from the primary.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the recommended setting for read/write separation as it provides
    /// the best balance between offloading reads and availability.
    /// </para>
    /// <para>
    /// Encina defaults to this mode when read/write separation is enabled.
    /// </para>
    /// </remarks>
    SecondaryPreferred = 3,

    /// <summary>
    /// Reads use the member with the lowest network latency.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this for geographically distributed replica sets where you want to
    /// minimize read latency by selecting the nearest member.
    /// </para>
    /// <para>
    /// This may route reads to either primary or secondary members.
    /// </para>
    /// </remarks>
    Nearest = 4
}
