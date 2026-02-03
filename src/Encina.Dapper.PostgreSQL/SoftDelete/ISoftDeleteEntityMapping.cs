using Encina.Dapper.PostgreSQL.Repository;

namespace Encina.Dapper.PostgreSQL.SoftDelete;

/// <summary>
/// Extended entity mapping that includes soft delete information for PostgreSQL.
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
/// <example>
/// <code>
/// services.AddSoftDeleteRepository&lt;Order, Guid&gt;(mapping =&gt;
///     mapping.ToTable("Orders")
///            .HasId(o =&gt; o.Id)
///            .HasSoftDelete(o =&gt; o.IsDeleted, "IsDeleted")
///            .HasDeletedAt(o =&gt; o.DeletedAtUtc, "DeletedAtUtc")
///            .HasDeletedBy(o =&gt; o.DeletedBy, "DeletedBy")
///            .MapProperty(o =&gt; o.CustomerId)
///            .MapProperty(o =&gt; o.Total));
/// </code>
/// </example>
public interface ISoftDeleteEntityMapping<TEntity, TId> : IEntityMapping<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    /// <summary>
    /// Gets a value indicating whether this entity supports soft delete.
    /// </summary>
    /// <value><c>true</c> if the entity has an <c>IsDeleted</c> property; otherwise, <c>false</c>.</value>
    bool IsSoftDeletable { get; }

    /// <summary>
    /// Gets the column name for the soft delete flag in the database.
    /// </summary>
    /// <value>The <c>IsDeleted</c> column name, or <c>null</c> if soft delete is not enabled.</value>
    string? IsDeletedColumnName { get; }

    /// <summary>
    /// Gets the property name for the soft delete flag in the entity.
    /// </summary>
    /// <value>The <c>IsDeleted</c> property name, or <c>null</c> if soft delete is not enabled.</value>
    string? IsDeletedPropertyName { get; }

    /// <summary>
    /// Gets the column name for the deletion timestamp in the database.
    /// </summary>
    /// <value>The <c>DeletedAtUtc</c> column name, or <c>null</c> if not tracked.</value>
    string? DeletedAtColumnName { get; }

    /// <summary>
    /// Gets the property name for the deletion timestamp in the entity.
    /// </summary>
    /// <value>The <c>DeletedAtUtc</c> property name, or <c>null</c> if not tracked.</value>
    string? DeletedAtPropertyName { get; }

    /// <summary>
    /// Gets the column name for the user who deleted the entity in the database.
    /// </summary>
    /// <value>The <c>DeletedBy</c> column name, or <c>null</c> if not tracked.</value>
    string? DeletedByColumnName { get; }

    /// <summary>
    /// Gets the property name for the user who deleted the entity.
    /// </summary>
    /// <value>The <c>DeletedBy</c> property name, or <c>null</c> if not tracked.</value>
    string? DeletedByPropertyName { get; }

    /// <summary>
    /// Gets the soft delete flag from an entity instance.
    /// </summary>
    /// <param name="entity">The entity to get the flag from.</param>
    /// <returns>
    /// <c>true</c> if the entity is soft-deleted; <c>false</c> if not deleted;
    /// <c>null</c> if soft delete is not enabled for this entity.
    /// </returns>
    bool? GetIsDeleted(TEntity entity);

    /// <summary>
    /// Sets the soft delete flag on an entity instance.
    /// </summary>
    /// <param name="entity">The entity to set the flag on.</param>
    /// <param name="isDeleted">The soft delete flag value.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when soft delete is not enabled (<see cref="IsSoftDeletable"/> is <c>false</c>).
    /// </exception>
    void SetIsDeleted(TEntity entity, bool isDeleted);

    /// <summary>
    /// Gets the deletion timestamp from an entity instance.
    /// </summary>
    /// <param name="entity">The entity to get the timestamp from.</param>
    /// <returns>The deletion timestamp, or <c>null</c> if not tracked or not deleted.</returns>
    DateTime? GetDeletedAtUtc(TEntity entity);

    /// <summary>
    /// Sets the deletion timestamp on an entity instance.
    /// </summary>
    /// <param name="entity">The entity to set the timestamp on.</param>
    /// <param name="deletedAtUtc">The deletion timestamp (should be UTC).</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="DeletedAtColumnName"/> is <c>null</c>.
    /// </exception>
    void SetDeletedAtUtc(TEntity entity, DateTime? deletedAtUtc);

    /// <summary>
    /// Gets the identifier of the user who deleted the entity.
    /// </summary>
    /// <param name="entity">The entity to get the user ID from.</param>
    /// <returns>The user identifier, or <c>null</c> if not tracked or not deleted.</returns>
    string? GetDeletedBy(TEntity entity);

    /// <summary>
    /// Sets the identifier of the user who deleted the entity.
    /// </summary>
    /// <param name="entity">The entity to set the user ID on.</param>
    /// <param name="deletedBy">The user identifier.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="DeletedByColumnName"/> is <c>null</c>.
    /// </exception>
    void SetDeletedBy(TEntity entity, string? deletedBy);
}
