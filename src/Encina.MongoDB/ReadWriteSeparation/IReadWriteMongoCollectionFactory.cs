using MongoDB.Driver;

namespace Encina.MongoDB.ReadWriteSeparation;

/// <summary>
/// Factory for creating MongoDB collections with read/write separation based on routing context.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides collection creation for read/write separation scenarios where
/// queries are routed to secondary members and commands to the primary using MongoDB
/// read preferences.
/// </para>
/// <para>
/// The factory uses <c>DatabaseRoutingContext</c> from Encina.Messaging to determine
/// which read preference to apply based on the current <c>DatabaseIntent</c>.
/// </para>
/// <para>
/// <b>Routing Logic:</b>
/// <list type="bullet">
///   <item>
///     <description>
///       <c>DatabaseIntent.Read</c>: Returns collection with configured read preference
///       (default: SecondaryPreferred)
///     </description>
///   </item>
///   <item>
///     <description>
///       <c>DatabaseIntent.Write</c> or <c>DatabaseIntent.ForceWrite</c>:
///       Returns collection with Primary read preference
///     </description>
///   </item>
///   <item>
///     <description>
///       No routing context: Returns collection with Primary read preference (safe default)
///     </description>
///   </item>
/// </list>
/// </para>
/// <para>
/// <b>Requirements:</b>
/// Read/write separation requires a MongoDB replica set deployment. Standalone
/// MongoDB servers do not support read preferences other than Primary.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderRepository
/// {
///     private readonly IReadWriteMongoCollectionFactory _collectionFactory;
///
///     public OrderRepository(IReadWriteMongoCollectionFactory collectionFactory)
///     {
///         _collectionFactory = collectionFactory;
///     }
///
///     // Query operations use secondary replicas
///     public async Task&lt;Order?&gt; GetByIdAsync(Guid id, CancellationToken ct)
///     {
///         var collection = await _collectionFactory.GetReadCollectionAsync&lt;Order&gt;("orders", ct);
///         return await collection.Find(o =&gt; o.Id == id).FirstOrDefaultAsync(ct);
///     }
///
///     // Command operations use the primary
///     public async Task InsertAsync(Order order, CancellationToken ct)
///     {
///         var collection = await _collectionFactory.GetWriteCollectionAsync&lt;Order&gt;("orders", ct);
///         await collection.InsertOneAsync(order, cancellationToken: ct);
///     }
/// }
/// </code>
/// </example>
public interface IReadWriteMongoCollectionFactory
{
    /// <summary>
    /// Gets a MongoDB collection configured for write operations (primary).
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="collectionName">The collection name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A MongoDB collection with <see cref="ReadPreference.Primary"/>.
    /// </returns>
    /// <remarks>
    /// Use this method for all write operations (Insert, Update, Delete) and for
    /// read operations that require the latest committed data (read-after-write consistency).
    /// </remarks>
    ValueTask<IMongoCollection<TEntity>> GetWriteCollectionAsync<TEntity>(
        string collectionName,
        CancellationToken cancellationToken = default)
        where TEntity : class;

    /// <summary>
    /// Gets a MongoDB collection configured for read operations (with configured read preference).
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="collectionName">The collection name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A MongoDB collection with the configured read preference (default: SecondaryPreferred).
    /// </returns>
    /// <remarks>
    /// <para>
    /// Use this method for read-only queries that can tolerate eventual consistency
    /// due to replication lag.
    /// </para>
    /// <para>
    /// The specific read preference is determined by <see cref="MongoReadWriteSeparationOptions.ReadPreference"/>.
    /// </para>
    /// </remarks>
    ValueTask<IMongoCollection<TEntity>> GetReadCollectionAsync<TEntity>(
        string collectionName,
        CancellationToken cancellationToken = default)
        where TEntity : class;

    /// <summary>
    /// Gets a MongoDB collection based on the current routing context.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="collectionName">The collection name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A MongoDB collection with read preference based on the current <c>DatabaseRoutingContext</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method uses the ambient <c>DatabaseRoutingContext</c> to determine
    /// which read preference to apply:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       <c>DatabaseIntent.Read</c>: Returns collection with configured read preference.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <c>DatabaseIntent.Write</c> or <c>DatabaseIntent.ForceWrite</c>:
    ///       Returns collection with Primary read preference.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       No routing context: Returns collection with Primary read preference (safe default).
    ///     </description>
    ///   </item>
    /// </list>
    /// </remarks>
    ValueTask<IMongoCollection<TEntity>> GetCollectionAsync<TEntity>(
        string collectionName,
        CancellationToken cancellationToken = default)
        where TEntity : class;

    /// <summary>
    /// Gets the database name for the current configuration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The database name.</returns>
    ValueTask<string> GetDatabaseNameAsync(CancellationToken cancellationToken = default);
}
