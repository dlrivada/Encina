using MongoDB.Driver;

namespace Encina.MongoDB.Tenancy;

/// <summary>
/// Factory for creating tenant-aware MongoDB collections.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to provide database-per-tenant or collection-per-tenant
/// routing. The factory determines the correct database based on the current tenant
/// context and returns the appropriate collection.
/// </para>
/// <para>
/// For shared database scenarios, use the default database from configuration.
/// For database-per-tenant scenarios, each tenant has their own database determined
/// by <see cref="MongoDbTenancyOptions.DatabaseNamePattern"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderService(IMongoCollectionFactory collectionFactory)
/// {
///     public async Task&lt;Order&gt; GetOrderAsync(string id, CancellationToken ct)
///     {
///         var collection = await collectionFactory.GetCollectionAsync&lt;Order&gt;("orders", ct);
///         return await collection.Find(o => o.Id == id).FirstOrDefaultAsync(ct);
///     }
/// }
/// </code>
/// </example>
public interface IMongoCollectionFactory
{
    /// <summary>
    /// Gets a MongoDB collection for the current tenant.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="collectionName">The collection name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A MongoDB collection configured for the current tenant.</returns>
    /// <remarks>
    /// <para>
    /// The returned collection is determined by the tenant isolation strategy:
    /// </para>
    /// <list type="bullet">
    /// <item><b>SharedDatabase:</b> Returns the collection from the default database</item>
    /// <item><b>DatabasePerTenant:</b> Returns the collection from the tenant's database</item>
    /// </list>
    /// <para>
    /// If no tenant context is available, the default database is used.
    /// </para>
    /// </remarks>
    ValueTask<IMongoCollection<TEntity>> GetCollectionAsync<TEntity>(
        string collectionName,
        CancellationToken cancellationToken = default)
        where TEntity : class;

    /// <summary>
    /// Gets a MongoDB collection for a specific tenant.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="collectionName">The collection name.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A MongoDB collection configured for the specified tenant.</returns>
    /// <remarks>
    /// Use this method when you need to explicitly specify the tenant,
    /// such as in background jobs or cross-tenant operations.
    /// </remarks>
    ValueTask<IMongoCollection<TEntity>> GetCollectionForTenantAsync<TEntity>(
        string collectionName,
        string tenantId,
        CancellationToken cancellationToken = default)
        where TEntity : class;

    /// <summary>
    /// Gets the database name for the current tenant without getting a collection.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The database name for the current tenant.</returns>
    ValueTask<string> GetDatabaseNameAsync(CancellationToken cancellationToken = default);
}
