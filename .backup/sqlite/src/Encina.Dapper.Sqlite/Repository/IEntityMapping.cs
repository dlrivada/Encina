namespace Encina.Dapper.Sqlite.Repository;

/// <summary>
/// Defines the mapping configuration for an entity type to its database table.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
public interface IEntityMapping<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    /// <summary>
    /// Gets the database table name for this entity.
    /// </summary>
    string TableName { get; }

    /// <summary>
    /// Gets the primary key column name.
    /// </summary>
    string IdColumnName { get; }

    /// <summary>
    /// Gets the mapping from property names to column names.
    /// </summary>
    IReadOnlyDictionary<string, string> ColumnMappings { get; }

    /// <summary>
    /// Gets the identifier value from an entity instance.
    /// </summary>
    /// <param name="entity">The entity instance.</param>
    /// <returns>The identifier value.</returns>
    TId GetId(TEntity entity);

    /// <summary>
    /// Gets the set of property names to exclude from INSERT operations.
    /// </summary>
    IReadOnlySet<string> InsertExcludedProperties { get; }

    /// <summary>
    /// Gets the set of property names to exclude from UPDATE operations.
    /// </summary>
    IReadOnlySet<string> UpdateExcludedProperties { get; }
}
