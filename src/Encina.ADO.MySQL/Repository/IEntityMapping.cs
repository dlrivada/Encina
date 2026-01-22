namespace Encina.ADO.MySQL.Repository;

/// <summary>
/// Defines the mapping configuration for an entity type to its database table.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// This interface provides all the information needed to generate SQL statements
/// for CRUD operations on an entity. It includes table name, column mappings,
/// and ID extraction logic.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderMapping : IEntityMapping&lt;Order, OrderId&gt;
/// {
///     public string TableName =&gt; "orders";
///     public string IdColumnName =&gt; "id";
///     public IReadOnlyDictionary&lt;string, string&gt; ColumnMappings =&gt; new Dictionary&lt;string, string&gt;
///     {
///         ["Id"] = "id",
///         ["CustomerId"] = "customer_id",
///         ["Total"] = "total",
///         ["CreatedAtUtc"] = "created_at_utc"
///     };
///     public OrderId GetId(Order entity) =&gt; entity.Id;
/// }
/// </code>
/// </example>
public interface IEntityMapping<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    /// <summary>
    /// Gets the database table name for this entity.
    /// </summary>
    /// <remarks>
    /// The table name should be a valid MySQL identifier. It can include
    /// schema prefixes like "mydb.orders" or backtick-delimited names like `Orders`.
    /// </remarks>
    string TableName { get; }

    /// <summary>
    /// Gets the column name that maps to the entity's primary key.
    /// </summary>
    string IdColumnName { get; }

    /// <summary>
    /// Gets the mapping from property names to column names.
    /// </summary>
    /// <remarks>
    /// Keys are entity property names, values are database column names.
    /// This allows for different naming conventions between code and database.
    /// </remarks>
    IReadOnlyDictionary<string, string> ColumnMappings { get; }

    /// <summary>
    /// Extracts the ID from an entity instance.
    /// </summary>
    /// <param name="entity">The entity to extract the ID from.</param>
    /// <returns>The entity's identifier.</returns>
    TId GetId(TEntity entity);

    /// <summary>
    /// Gets the property names that should be excluded from INSERT operations
    /// (e.g., auto-generated columns).
    /// </summary>
    IReadOnlySet<string> InsertExcludedProperties { get; }

    /// <summary>
    /// Gets the property names that should be excluded from UPDATE operations
    /// (e.g., the primary key, audit columns).
    /// </summary>
    IReadOnlySet<string> UpdateExcludedProperties { get; }
}
