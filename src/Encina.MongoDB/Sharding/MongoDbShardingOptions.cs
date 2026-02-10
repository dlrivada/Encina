namespace Encina.MongoDB.Sharding;

/// <summary>
/// Configuration options for MongoDB sharding behavior.
/// </summary>
/// <remarks>
/// <para>
/// MongoDB supports two sharding modes:
/// <list type="bullet">
///   <item>
///     <description>
///       <b>Native sharding (default, recommended)</b>: Uses MongoDB's built-in <c>mongos</c>
///       router. The application connects to a single <c>mongos</c> endpoint and MongoDB
///       handles all routing transparently. This is the production-grade approach.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Application-level routing (fallback)</b>: Routes operations in the application
///       layer using <c>AddEncinaSharding&lt;TEntity&gt;</c> topology. Used when native
///       sharding is not available (e.g., standalone/replica set deployments).
///     </description>
///   </item>
/// </list>
/// </para>
/// <para>
/// The sharding mode is determined by <see cref="UseNativeSharding"/>. When <c>true</c>
/// (default), the application relies on <c>mongos</c> for routing and only needs the
/// shard key configuration for targeted queries. When <c>false</c>, the application
/// manages routing using <c>IShardRouter</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Native mongos routing (recommended)
/// services.AddEncinaMongoDBSharding&lt;Order, Guid&gt;(options =&gt;
/// {
///     options.UseNativeSharding = true;
///     options.ShardKeyField = "customerId";
///     options.CollectionName = "orders";
///     options.IdProperty = o =&gt; o.Id;
/// });
///
/// // Application-level routing (fallback)
/// services.AddEncinaSharding&lt;Order&gt;(shardOptions =&gt;
/// {
///     shardOptions.UseHashRouting()
///         .AddShard("shard-0", "mongodb://shard0:27017/Orders")
///         .AddShard("shard-1", "mongodb://shard1:27017/Orders");
/// });
/// services.AddEncinaMongoDBSharding&lt;Order, Guid&gt;(options =&gt;
/// {
///     options.UseNativeSharding = false;
///     options.CollectionName = "orders";
///     options.IdProperty = o =&gt; o.Id;
/// });
/// </code>
/// </example>
public sealed class MongoDbShardingOptions<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    /// <summary>
    /// Gets or sets a value indicating whether to use MongoDB's native <c>mongos</c> routing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c> (default), the application connects to <c>mongos</c> and MongoDB
    /// handles all shard routing transparently. This is the recommended production approach.
    /// </para>
    /// <para>
    /// When <c>false</c>, the application manages routing using the
    /// <c>AddEncinaSharding&lt;TEntity&gt;</c> topology and <c>IShardRouter</c>.
    /// This mode requires that core sharding services have been registered first.
    /// </para>
    /// </remarks>
    public bool UseNativeSharding { get; set; } = true;

    /// <summary>
    /// Gets or sets the shard key field name in MongoDB documents.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For native sharding, this identifies the field MongoDB uses as the shard key.
    /// The field must match the shard key configured in the MongoDB cluster via
    /// <c>sh.shardCollection()</c>.
    /// </para>
    /// <para>
    /// For application-level routing, this field is used to extract the shard key
    /// value from entities for routing purposes.
    /// </para>
    /// </remarks>
    /// <example>"customerId", "region", "tenantId"</example>
    public string? ShardKeyField { get; set; }

    /// <summary>
    /// Gets or sets the MongoDB collection name.
    /// </summary>
    /// <remarks>
    /// If not specified, defaults to the entity type name in lowercase with an 's' suffix
    /// (e.g., "orders" for <c>Order</c>).
    /// </remarks>
    public string? CollectionName { get; set; }

    /// <summary>
    /// Gets or sets the expression to select the ID property from an entity.
    /// </summary>
    /// <remarks>
    /// This expression is used to build filters for GetById, Update, and Delete operations.
    /// The property should typically be mapped to MongoDB's <c>_id</c> field.
    /// </remarks>
    public System.Linq.Expressions.Expression<Func<TEntity, TId>>? IdProperty { get; set; }

    /// <summary>
    /// Gets or sets the database name override for this sharded collection.
    /// </summary>
    /// <remarks>
    /// When set, overrides the default database from <see cref="EncinaMongoDbOptions.DatabaseName"/>.
    /// Useful when the sharded collection lives in a different database.
    /// </remarks>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// Gets the effective collection name, applying default naming if not specified.
    /// </summary>
    /// <returns>The collection name to use.</returns>
    internal string GetEffectiveCollectionName()
    {
        if (!string.IsNullOrWhiteSpace(CollectionName))
        {
            return CollectionName;
        }

        var entityName = typeof(TEntity).Name;
#pragma warning disable CA1308 // Normalize strings to uppercase - MongoDB convention is lowercase collection names
        return entityName.ToLowerInvariant() + "s";
#pragma warning restore CA1308
    }

    /// <summary>
    /// Validates the options and throws if required properties are missing.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="IdProperty"/> is not configured.
    /// </exception>
    internal void Validate()
    {
        if (IdProperty is null)
        {
            throw new InvalidOperationException(
                $"IdProperty must be configured for sharded repository of type {typeof(TEntity).Name}. " +
                "Use options.IdProperty = x => x.Id to specify the ID property.");
        }
    }
}
