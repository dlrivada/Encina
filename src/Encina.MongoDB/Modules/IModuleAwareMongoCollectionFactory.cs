using MongoDB.Driver;

namespace Encina.MongoDB.Modules;

/// <summary>
/// Factory for creating module-aware MongoDB collections.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to provide database-per-module routing.
/// The factory determines the correct database based on the current module
/// context and returns the appropriate collection.
/// </para>
/// <para>
/// For shared database scenarios, use the default database from configuration.
/// For database-per-module scenarios, each module has their own database determined
/// by <see cref="MongoDbModuleIsolationOptions.DatabaseNamePattern"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderService(IModuleAwareMongoCollectionFactory collectionFactory)
/// {
///     public async Task&lt;Order&gt; GetOrderAsync(string id, CancellationToken ct)
///     {
///         var collection = await collectionFactory.GetCollectionAsync&lt;Order&gt;("orders", ct);
///         return await collection.Find(o => o.Id == id).FirstOrDefaultAsync(ct);
///     }
/// }
/// </code>
/// </example>
public interface IModuleAwareMongoCollectionFactory
{
    /// <summary>
    /// Gets a MongoDB collection for the current module.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="collectionName">The collection name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A MongoDB collection configured for the current module.</returns>
    /// <remarks>
    /// <para>
    /// The returned collection is determined by the module isolation strategy:
    /// </para>
    /// <list type="bullet">
    /// <item><b>SharedDatabase:</b> Returns the collection from the default database</item>
    /// <item><b>DatabasePerModule:</b> Returns the collection from the module's database</item>
    /// </list>
    /// <para>
    /// If no module context is available, the default database is used.
    /// </para>
    /// </remarks>
    ValueTask<IMongoCollection<TEntity>> GetCollectionAsync<TEntity>(
        string collectionName,
        CancellationToken cancellationToken = default)
        where TEntity : class;

    /// <summary>
    /// Gets a MongoDB collection for a specific module.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="collectionName">The collection name.</param>
    /// <param name="moduleName">The module name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A MongoDB collection configured for the specified module.</returns>
    /// <remarks>
    /// Use this method when you need to explicitly specify the module,
    /// such as in background jobs or cross-module operations.
    /// </remarks>
    ValueTask<IMongoCollection<TEntity>> GetCollectionForModuleAsync<TEntity>(
        string collectionName,
        string moduleName,
        CancellationToken cancellationToken = default)
        where TEntity : class;

    /// <summary>
    /// Gets the database name for the current module without getting a collection.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The database name for the current module.</returns>
    ValueTask<string> GetDatabaseNameAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the database name for a specific module.
    /// </summary>
    /// <param name="moduleName">The module name.</param>
    /// <returns>The database name for the specified module.</returns>
    string GetDatabaseNameForModule(string moduleName);
}
