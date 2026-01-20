using System.Linq.Expressions;

namespace Encina.MongoDB.Repository;

/// <summary>
/// Configuration options for MongoDB repository registration.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
public sealed class MongoDbRepositoryOptions<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    /// <summary>
    /// Gets or sets the MongoDB collection name.
    /// </summary>
    /// <remarks>
    /// If not specified, defaults to the entity type name in lowercase with 's' suffix
    /// (e.g., "orders" for Order entity).
    /// </remarks>
    public string? CollectionName { get; set; }

    /// <summary>
    /// Gets or sets the expression to select the ID property from an entity.
    /// </summary>
    /// <remarks>
    /// This expression is used to:
    /// - Build filters for GetById, Update, and Delete operations
    /// - Extract the ID value from entities
    /// The property should typically be mapped to MongoDB's _id field.
    /// </remarks>
    public Expression<Func<TEntity, TId>>? IdProperty { get; set; }

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

        // Default naming convention: entity name with 's' suffix (lowercase preferred for MongoDB)
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
                $"IdProperty must be configured for repository of type {typeof(TEntity).Name}. " +
                $"Use config.IdProperty = x => x.Id to specify the ID property.");
        }
    }
}
