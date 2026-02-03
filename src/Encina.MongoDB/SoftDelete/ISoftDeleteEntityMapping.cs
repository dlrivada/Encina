using System.Linq.Expressions;

namespace Encina.MongoDB.SoftDelete;

/// <summary>
/// Defines mapping information for soft delete properties in MongoDB entities.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// This interface provides the mapping configuration needed for soft delete operations
/// in MongoDB. It maps entity properties to MongoDB document field names and provides
/// methods for getting and setting soft delete property values.
/// </para>
/// <para>
/// Implementations should be registered as singletons since mappings are immutable
/// after configuration.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var builder = new SoftDeleteEntityMappingBuilder&lt;Order, Guid&gt;();
/// var mapping = builder
///     .HasId(o =&gt; o.Id)
///     .HasSoftDelete(o =&gt; o.IsDeleted, "isDeleted")
///     .HasDeletedAt(o =&gt; o.DeletedAtUtc, "deletedAtUtc")
///     .HasDeletedBy(o =&gt; o.DeletedBy, "deletedBy")
///     .Build();
/// </code>
/// </example>
public interface ISoftDeleteEntityMapping<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    /// <summary>
    /// Gets a value indicating whether this entity type supports soft delete.
    /// </summary>
    /// <value>
    /// <c>true</c> if soft delete is configured for this entity type; otherwise, <c>false</c>.
    /// </value>
    bool IsSoftDeletable { get; }

    /// <summary>
    /// Gets the MongoDB field name for the IsDeleted property.
    /// </summary>
    /// <value>The field name, or <c>null</c> if not configured.</value>
    string IsDeletedFieldName { get; }

    /// <summary>
    /// Gets the MongoDB field name for the DeletedAtUtc property.
    /// </summary>
    /// <value>The field name, or <c>null</c> if not configured.</value>
    string? DeletedAtFieldName { get; }

    /// <summary>
    /// Gets the MongoDB field name for the DeletedBy property.
    /// </summary>
    /// <value>The field name, or <c>null</c> if not configured.</value>
    string? DeletedByFieldName { get; }

    /// <summary>
    /// Gets the expression that selects the ID property from an entity.
    /// </summary>
    Expression<Func<TEntity, TId>> IdSelector { get; }

    /// <summary>
    /// Gets the ID value from an entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>The entity's ID value.</returns>
    TId GetId(TEntity entity);

    /// <summary>
    /// Gets the IsDeleted value from an entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>The IsDeleted value.</returns>
    bool GetIsDeleted(TEntity entity);

    /// <summary>
    /// Sets the IsDeleted value on an entity.
    /// </summary>
    /// <param name="entity">The entity to modify.</param>
    /// <param name="value">The value to set.</param>
    void SetIsDeleted(TEntity entity, bool value);

    /// <summary>
    /// Gets the DeletedAtUtc value from an entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>The DeletedAtUtc value, or <c>null</c> if not set or not configured.</returns>
    DateTime? GetDeletedAt(TEntity entity);

    /// <summary>
    /// Sets the DeletedAtUtc value on an entity.
    /// </summary>
    /// <param name="entity">The entity to modify.</param>
    /// <param name="value">The value to set.</param>
    void SetDeletedAt(TEntity entity, DateTime? value);

    /// <summary>
    /// Gets the DeletedBy value from an entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>The DeletedBy value, or <c>null</c> if not set or not configured.</returns>
    string? GetDeletedBy(TEntity entity);

    /// <summary>
    /// Sets the DeletedBy value on an entity.
    /// </summary>
    /// <param name="entity">The entity to modify.</param>
    /// <param name="value">The value to set.</param>
    void SetDeletedBy(TEntity entity, string? value);
}
