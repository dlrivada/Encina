using Encina.Sharding.Colocation;
using LanguageExt;

namespace Encina.Sharding;

/// <summary>
/// Routes entities and shard keys to the appropriate shard.
/// </summary>
/// <remarks>
/// <para>
/// Implementations determine which shard a given entity or key belongs to
/// based on their routing strategy (hash, range, directory, geo, etc.).
/// </para>
/// <para>
/// All routing operations return <see cref="Either{EncinaError, T}"/>
/// following the Railway Oriented Programming pattern.
/// </para>
/// </remarks>
public interface IShardRouter
{
    /// <summary>
    /// Gets the shard ID for a given shard key string.
    /// </summary>
    /// <param name="shardKey">The shard key to route.</param>
    /// <returns>Right with the shard ID; Left with an error if routing fails.</returns>
    Either<EncinaError, string> GetShardId(string shardKey);

    /// <summary>
    /// Gets the shard ID for a given compound shard key.
    /// </summary>
    /// <param name="key">The compound shard key to route.</param>
    /// <returns>Right with the shard ID; Left with an error if routing fails.</returns>
    /// <remarks>
    /// The default implementation serializes the compound key using the pipe delimiter
    /// and delegates to <see cref="GetShardId(string)"/>. Router implementations may
    /// override this to provide compound-key-aware routing.
    /// </remarks>
    Either<EncinaError, string> GetShardId(CompoundShardKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return GetShardId(key.ToString());
    }

    /// <summary>
    /// Gets the shard IDs that may contain data matching a partial compound key.
    /// </summary>
    /// <param name="partialKey">
    /// A compound key with fewer components than the full key. Used for prefix-based
    /// scatter-gather routing (e.g., querying all shards for a given region).
    /// </param>
    /// <returns>
    /// Right with the list of matching shard IDs; Left with an error if partial key routing fails.
    /// </returns>
    /// <remarks>
    /// <para>
    /// For routers that cannot narrow results using a partial key (e.g., hash routers),
    /// the default implementation returns all shard IDs.
    /// </para>
    /// </remarks>
    Either<EncinaError, IReadOnlyList<string>> GetShardIds(CompoundShardKey partialKey)
    {
        ArgumentNullException.ThrowIfNull(partialKey);
        return Either<EncinaError, IReadOnlyList<string>>.Right(GetAllShardIds());
    }

    /// <summary>
    /// Gets all shard IDs known to this router.
    /// </summary>
    /// <returns>All shard IDs in the topology.</returns>
    IReadOnlyList<string> GetAllShardIds();

    /// <summary>
    /// Gets the connection string for a shard.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <returns>Right with the connection string; Left with an error if the shard is not found.</returns>
    Either<EncinaError, string> GetShardConnectionString(string shardId);

    /// <summary>
    /// Gets the co-location group for a given entity type, if it belongs to one.
    /// </summary>
    /// <param name="entityType">The entity type to look up (can be root or co-located).</param>
    /// <returns>
    /// The <see cref="IColocationGroup"/> if the entity type belongs to a co-location group;
    /// otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The default implementation returns <c>null</c>, indicating that the router has no
    /// co-location awareness. Built-in routers that are constructed with a
    /// <see cref="ColocationGroupRegistry"/> override this to provide co-location metadata.
    /// </para>
    /// <para>
    /// This method enables the routing layer and query planner to determine whether two
    /// entity types are co-located and can be joined locally on the same shard.
    /// </para>
    /// </remarks>
    IColocationGroup? GetColocationGroup(Type entityType)
    {
        return null;
    }
}

/// <summary>
/// Routes entities of type <typeparamref name="TEntity"/> to the appropriate shard.
/// </summary>
/// <typeparam name="TEntity">The entity type being routed.</typeparam>
/// <remarks>
/// <para>
/// This generic interface extends <see cref="IShardRouter"/> to add entity-aware routing.
/// The shard key is automatically extracted from the entity using <see cref="CompoundShardKeyExtractor"/>.
/// </para>
/// </remarks>
public interface IShardRouter<in TEntity> : IShardRouter
    where TEntity : notnull
{
    /// <summary>
    /// Gets the shard ID for a given entity.
    /// </summary>
    /// <param name="entity">The entity to route.</param>
    /// <returns>Right with the shard ID; Left with an error if routing fails.</returns>
    Either<EncinaError, string> GetShardId(TEntity entity);

    /// <summary>
    /// Gets the shard IDs that may contain data matching a partial key extracted from the entity.
    /// </summary>
    /// <param name="entity">The entity whose compound key is used for partial routing.</param>
    /// <returns>
    /// Right with the list of matching shard IDs; Left with an error if routing fails.
    /// </returns>
    Either<EncinaError, IReadOnlyList<string>> GetShardIds(TEntity entity);
}
