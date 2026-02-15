namespace Encina.Sharding.ReferenceTables;

/// <summary>
/// Creates <see cref="IReferenceTableStore"/> instances bound to a specific shard
/// connection string.
/// </summary>
/// <remarks>
/// <para>
/// The replicator needs to read from the primary shard and write to each target shard.
/// Since <see cref="IReferenceTableStore"/> operates on a single connection, the factory
/// produces per-shard store instances on demand.
/// </para>
/// <para>
/// Each data access provider (ADO.NET, Dapper, EF Core, MongoDB) implements this factory
/// to create provider-specific store instances.
/// </para>
/// </remarks>
public interface IReferenceTableStoreFactory
{
    /// <summary>
    /// Creates a reference table store for the specified shard connection string.
    /// </summary>
    /// <param name="connectionString">The connection string of the target shard.</param>
    /// <returns>An <see cref="IReferenceTableStore"/> bound to the given shard.</returns>
    IReferenceTableStore CreateForShard(string connectionString);
}
