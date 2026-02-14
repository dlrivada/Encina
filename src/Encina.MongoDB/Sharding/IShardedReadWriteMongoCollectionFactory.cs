using LanguageExt;
using MongoDB.Driver;

namespace Encina.MongoDB.Sharding;

/// <summary>
/// Factory for creating MongoDB collections that combine shard routing with read/write separation.
/// </summary>
/// <remarks>
/// <para>
/// This interface unifies <see cref="IShardedMongoCollectionFactory"/> (shard routing) with
/// read/write separation so that each shard can route queries to secondary members and
/// commands to the primary using MongoDB read preferences.
/// </para>
/// <para>
/// It supports three usage modes:
/// </para>
/// <list type="number">
///   <item><description>
///     <b>Explicit read</b>: <see cref="GetReadCollectionForShard{TEntity}"/> always returns a
///     collection with a secondary-preferred read preference.
///   </description></item>
///   <item><description>
///     <b>Explicit write</b>: <see cref="GetWriteCollectionForShard{TEntity}"/> always returns a
///     collection with a primary read preference.
///   </description></item>
///   <item><description>
///     <b>Context-aware</b>: <see cref="GetCollectionForShard{TEntity}"/> reads the ambient
///     <c>DatabaseRoutingContext</c> (from <c>Encina.Messaging</c>) to decide automatically.
///   </description></item>
/// </list>
/// <para>
/// When used in application-level sharding mode, separate <see cref="IMongoClient"/> instances
/// are created per shard. In native <c>mongos</c> sharding mode, all collections come from
/// the default client with different read preferences applied.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Explicit read — uses SecondaryPreferred read preference on shard-0
/// var readResult = factory.GetReadCollectionForShard&lt;Order&gt;("shard-0", "orders");
///
/// // Explicit write — uses Primary read preference on shard-0
/// var writeResult = factory.GetWriteCollectionForShard&lt;Order&gt;("shard-0", "orders");
///
/// // Context-aware — uses DatabaseRoutingContext.CurrentIntent
/// DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Read;
/// var autoResult = factory.GetCollectionForShard&lt;Order&gt;("shard-0", "orders");
/// </code>
/// </example>
public interface IShardedReadWriteMongoCollectionFactory
{
    /// <summary>
    /// Gets a MongoDB collection configured for read operations on a specific shard.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="collectionName">The collection name.</param>
    /// <returns>
    /// Right with a collection using a secondary read preference;
    /// Left with an error if the shard is not found.
    /// </returns>
    /// <remarks>
    /// The read preference is determined by the configured <c>MongoReadWriteSeparationOptions</c>
    /// (default: <see cref="ReadPreference.SecondaryPreferred"/>).
    /// </remarks>
    Either<EncinaError, IMongoCollection<TEntity>> GetReadCollectionForShard<TEntity>(
        string shardId,
        string collectionName)
        where TEntity : class;

    /// <summary>
    /// Gets a MongoDB collection configured for write operations on a specific shard.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="collectionName">The collection name.</param>
    /// <returns>
    /// Right with a collection using <see cref="ReadPreference.Primary"/>;
    /// Left with an error if the shard is not found.
    /// </returns>
    Either<EncinaError, IMongoCollection<TEntity>> GetWriteCollectionForShard<TEntity>(
        string shardId,
        string collectionName)
        where TEntity : class;

    /// <summary>
    /// Gets a MongoDB collection for a specific shard, using the ambient
    /// <c>DatabaseRoutingContext</c> to determine the read preference.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="collectionName">The collection name.</param>
    /// <returns>
    /// Right with a collection configured with the appropriate read preference;
    /// Left with an error if the shard is not found.
    /// </returns>
    /// <remarks>
    /// When no routing context is set, defaults to <see cref="ReadPreference.Primary"/>
    /// (write) for safety.
    /// </remarks>
    Either<EncinaError, IMongoCollection<TEntity>> GetCollectionForShard<TEntity>(
        string shardId,
        string collectionName)
        where TEntity : class;

    /// <summary>
    /// Gets read collections for all active shards (scatter-gather reads).
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="collectionName">The collection name.</param>
    /// <returns>
    /// Right with a dictionary mapping shard IDs to their read collections;
    /// Left with an error if any shard connection fails.
    /// </returns>
    Either<EncinaError, IReadOnlyDictionary<string, IMongoCollection<TEntity>>> GetAllReadCollections<TEntity>(
        string collectionName)
        where TEntity : class;

    /// <summary>
    /// Gets write collections for all active shards (scatter-gather writes).
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="collectionName">The collection name.</param>
    /// <returns>
    /// Right with a dictionary mapping shard IDs to their write collections;
    /// Left with an error if any shard connection fails.
    /// </returns>
    Either<EncinaError, IReadOnlyDictionary<string, IMongoCollection<TEntity>>> GetAllWriteCollections<TEntity>(
        string collectionName)
        where TEntity : class;
}
