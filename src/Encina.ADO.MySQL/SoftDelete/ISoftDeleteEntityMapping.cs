using Encina.ADO.MySQL.Repository;

namespace Encina.ADO.MySQL.SoftDelete;

/// <summary>
/// Extended entity mapping that includes soft delete information for ADO.NET with MySQL.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// This interface extends <see cref="IEntityMapping{TEntity, TId}"/> to include
/// soft delete-specific mapping information, such as the <c>IsDeleted</c> column name.
/// </para>
/// <para>
/// Use <see cref="SoftDeleteEntityMappingBuilder{TEntity, TId}"/> for fluent configuration.
/// </para>
/// </remarks>
public interface ISoftDeleteEntityMapping<TEntity, TId> : IEntityMapping<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    /// <summary>
    /// Gets a value indicating whether this entity supports soft delete.
    /// </summary>
    bool IsSoftDeletable { get; }

    /// <summary>
    /// Gets the column name for the soft delete flag in the database.
    /// </summary>
    string? IsDeletedColumnName { get; }

    /// <summary>
    /// Gets the property name for the soft delete flag in the entity.
    /// </summary>
    string? IsDeletedPropertyName { get; }

    /// <summary>
    /// Gets the column name for the deletion timestamp in the database.
    /// </summary>
    string? DeletedAtColumnName { get; }

    /// <summary>
    /// Gets the property name for the deletion timestamp in the entity.
    /// </summary>
    string? DeletedAtPropertyName { get; }

    /// <summary>
    /// Gets the column name for the user who deleted the entity.
    /// </summary>
    string? DeletedByColumnName { get; }

    /// <summary>
    /// Gets the property name for the user who deleted the entity.
    /// </summary>
    string? DeletedByPropertyName { get; }

    /// <summary>
    /// Gets the soft delete flag from an entity instance.
    /// </summary>
    bool? GetIsDeleted(TEntity entity);

    /// <summary>
    /// Sets the soft delete flag on an entity instance.
    /// </summary>
    void SetIsDeleted(TEntity entity, bool isDeleted);

    /// <summary>
    /// Gets the deletion timestamp from an entity instance.
    /// </summary>
    DateTime? GetDeletedAtUtc(TEntity entity);

    /// <summary>
    /// Sets the deletion timestamp on an entity instance.
    /// </summary>
    void SetDeletedAtUtc(TEntity entity, DateTime? deletedAtUtc);

    /// <summary>
    /// Gets the identifier of the user who deleted the entity.
    /// </summary>
    string? GetDeletedBy(TEntity entity);

    /// <summary>
    /// Sets the identifier of the user who deleted the entity.
    /// </summary>
    void SetDeletedBy(TEntity entity, string? deletedBy);
}
