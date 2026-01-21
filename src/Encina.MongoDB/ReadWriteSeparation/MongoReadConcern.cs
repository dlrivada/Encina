namespace Encina.MongoDB.ReadWriteSeparation;

/// <summary>
/// Specifies the read concern level for MongoDB operations.
/// </summary>
/// <remarks>
/// <para>
/// Read concern allows you to control the consistency and isolation properties
/// of the data read from replica sets and replica set shards.
/// </para>
/// <para>
/// Higher read concern levels provide stronger consistency guarantees but may
/// impact performance and availability.
/// </para>
/// </remarks>
public enum MongoReadConcern
{
    /// <summary>
    /// Use the server's default read concern.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Returns data from the instance with no guarantee that the data has been
    /// written to a majority of replica set members.
    /// </summary>
    /// <remarks>
    /// This is the fastest read concern but may return data that could be rolled back.
    /// </remarks>
    Local = 1,

    /// <summary>
    /// Returns data that reflects all successful majority-committed writes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the recommended read concern for most applications as it provides
    /// a balance between consistency and performance.
    /// </para>
    /// <para>
    /// Encina defaults to this mode when read/write separation is enabled.
    /// </para>
    /// </remarks>
    Majority = 2,

    /// <summary>
    /// Returns data from a majority of replica set members and provides read-your-writes
    /// guarantee for data written within the same session.
    /// </summary>
    /// <remarks>
    /// Use this in combination with sessions for causal consistency.
    /// </remarks>
    Linearizable = 3,

    /// <summary>
    /// For sharded clusters, returns data that has been acknowledged by the shard replica
    /// set members.
    /// </summary>
    Available = 4,

    /// <summary>
    /// Returns data from a snapshot of majority-committed data at a specific point in time.
    /// </summary>
    /// <remarks>
    /// Only available with MongoDB 4.0+ and when used with multi-document transactions.
    /// </remarks>
    Snapshot = 5
}
