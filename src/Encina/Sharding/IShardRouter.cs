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
}

/// <summary>
/// Routes entities of type <typeparamref name="TEntity"/> to the appropriate shard.
/// </summary>
/// <typeparam name="TEntity">The entity type being routed.</typeparam>
/// <remarks>
/// <para>
/// This generic interface extends <see cref="IShardRouter"/> to add entity-aware routing.
/// The shard key is automatically extracted from the entity using <see cref="ShardKeyExtractor"/>.
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
}
