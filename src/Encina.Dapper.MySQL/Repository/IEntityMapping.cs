namespace Encina.Dapper.MySQL.Repository;

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
    /// Gets the column name that maps to the entity's primary key.
    /// </summary>
    string IdColumnName { get; }

    /// <summary>
    /// Gets the mapping from property names to column names.
    /// </summary>
    IReadOnlyDictionary<string, string> ColumnMappings { get; }

    /// <summary>
    /// Extracts the ID from an entity instance.
    /// </summary>
    /// <param name="entity">The entity to extract the ID from.</param>
    /// <returns>The entity's identifier.</returns>
    TId GetId(TEntity entity);

    /// <summary>
    /// Gets the property names that should be excluded from INSERT operations.
    /// </summary>
    IReadOnlySet<string> InsertExcludedProperties { get; }

    /// <summary>
    /// Gets the property names that should be excluded from UPDATE operations.
    /// </summary>
    IReadOnlySet<string> UpdateExcludedProperties { get; }
}
