using Encina.Dapper.MySQL.Repository;

namespace Encina.Dapper.MySQL.Tenancy;

/// <summary>
/// Extended entity mapping that includes tenant information.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// This interface extends <see cref="IEntityMapping{TEntity, TId}"/> to include
/// tenant-specific mapping information, such as the tenant column name.
/// </para>
/// <para>
/// Use <see cref="TenantEntityMappingBuilder{TEntity, TId}"/> for fluent configuration.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddTenantAwareRepository&lt;Order, Guid&gt;(mapping =&gt;
///     mapping.ToTable("Orders")
///            .HasId(o =&gt; o.Id)
///            .HasTenantId(o =&gt; o.TenantId, "TenantId")
///            .MapProperty(o =&gt; o.CustomerId)
///            .MapProperty(o =&gt; o.Total));
/// </code>
/// </example>
public interface ITenantEntityMapping<TEntity, TId> : IEntityMapping<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    /// <summary>
    /// Gets a value indicating whether this entity is tenant-scoped.
    /// </summary>
    /// <value><c>true</c> if the entity has a tenant ID property; otherwise, <c>false</c>.</value>
    bool IsTenantEntity { get; }

    /// <summary>
    /// Gets the column name for the tenant identifier in the database.
    /// </summary>
    /// <value>The tenant column name, or <c>null</c> if this is not a tenant entity.</value>
    string? TenantColumnName { get; }

    /// <summary>
    /// Gets the property name for the tenant identifier in the entity.
    /// </summary>
    /// <value>The tenant property name, or <c>null</c> if this is not a tenant entity.</value>
    string? TenantPropertyName { get; }

    /// <summary>
    /// Gets the tenant ID from an entity instance.
    /// </summary>
    /// <param name="entity">The entity to get the tenant ID from.</param>
    /// <returns>The tenant ID, or <c>null</c> if this is not a tenant entity or the value is not set.</returns>
    string? GetTenantId(TEntity entity);

    /// <summary>
    /// Sets the tenant ID on an entity instance.
    /// </summary>
    /// <param name="entity">The entity to set the tenant ID on.</param>
    /// <param name="tenantId">The tenant ID to set.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this is not a tenant entity (<see cref="IsTenantEntity"/> is <c>false</c>).
    /// </exception>
    void SetTenantId(TEntity entity, string tenantId);
}
