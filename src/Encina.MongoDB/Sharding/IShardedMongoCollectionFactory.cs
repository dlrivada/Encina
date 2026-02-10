using LanguageExt;
using MongoDB.Driver;

namespace Encina.MongoDB.Sharding;

/// <summary>
/// Factory for creating MongoDB collections connected to specific shards.
/// </summary>
/// <remarks>
/// <para>
/// This interface supports two sharding modes:
/// <list type="bullet">
///   <item>
///     <description>
///       <b>Native sharding</b>: Returns collections from the default <see cref="IMongoClient"/>
///       connected to <c>mongos</c>. MongoDB handles routing transparently.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Application-level sharding</b>: Returns collections from shard-specific
///       <see cref="IMongoClient"/> instances, each connected to a different mongod/replica set.
///     </description>
///   </item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Get a collection for a specific shard
/// var result = factory.GetCollectionForShard&lt;Order&gt;("shard-0", "orders");
/// result.Match(
///     Right: collection =&gt; { /* use collection */ },
///     Left: error =&gt; logger.LogError("Failed: {Error}", error.Message));
/// </code>
/// </example>
public interface IShardedMongoCollectionFactory
{
    /// <summary>
    /// Gets a MongoDB collection for a specific shard.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="collectionName">The collection name.</param>
    /// <returns>Right with the collection; Left with an error if the shard is not found.</returns>
    /// <remarks>
    /// <para>
    /// In native sharding mode, this returns a collection from the default client
    /// regardless of the shard ID, since <c>mongos</c> handles routing.
    /// </para>
    /// <para>
    /// In application-level sharding mode, this creates a client connected to the
    /// shard's connection string and returns a collection from that client.
    /// </para>
    /// </remarks>
    Either<EncinaError, IMongoCollection<TEntity>> GetCollectionForShard<TEntity>(
        string shardId,
        string collectionName)
        where TEntity : class;

    /// <summary>
    /// Gets a MongoDB collection for the entity, using the shard key to determine routing.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="collectionName">The collection name.</param>
    /// <returns>
    /// Right with the collection; Left with an error if routing fails.
    /// In native sharding mode, always returns the collection from the default client.
    /// </returns>
    Either<EncinaError, IMongoCollection<TEntity>> GetDefaultCollection<TEntity>(
        string collectionName)
        where TEntity : class;

    /// <summary>
    /// Gets collections for all shards in the topology.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="collectionName">The collection name.</param>
    /// <returns>
    /// Right with a dictionary mapping shard IDs to their collections;
    /// Left with an error if any shard connection fails.
    /// </returns>
    /// <remarks>
    /// In native sharding mode, returns a single entry with the default collection
    /// since all data is accessed through the same <c>mongos</c> endpoint.
    /// </remarks>
    Either<EncinaError, IReadOnlyDictionary<string, IMongoCollection<TEntity>>> GetAllCollections<TEntity>(
        string collectionName)
        where TEntity : class;
}
