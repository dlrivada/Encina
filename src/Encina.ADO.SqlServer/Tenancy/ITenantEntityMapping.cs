using Encina.ADO.SqlServer.Repository;

namespace Encina.ADO.SqlServer.Tenancy;

/// <summary>
/// Extended entity mapping interface with tenant-specific properties for ADO.NET.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// This interface extends <see cref="IEntityMapping{TEntity, TId}"/> with
/// tenant-aware capabilities including tenant column name, property access,
/// and getter/setter methods for the tenant ID.
/// </para>
/// <para>
/// Use <see cref="TenantEntityMappingBuilder{TEntity, TId}"/> for fluent configuration
/// of tenant entity mappings.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var mapping = new TenantEntityMappingBuilder&lt;Order, Guid&gt;()
///     .ToTable("Orders")
///     .HasId(o =&gt; o.Id)
///     .HasTenantId(o =&gt; o.TenantId)
///     .MapProperty(o =&gt; o.CustomerId)
///     .Build();
///
/// // Check if entity is tenant-aware
/// if (mapping.IsTenantEntity)
/// {
///     var tenantId = mapping.GetTenantId(entity);
///     // Apply tenant filtering
/// }
/// </code>
/// </example>
public interface ITenantEntityMapping<TEntity, TId> : IEntityMapping<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    /// <summary>
    /// Gets a value indicating whether this entity is tenant-scoped.
    /// </summary>
    /// <value><c>true</c> if <see cref="TenantColumnName"/> is configured; otherwise, <c>false</c>.</value>
    /// <remarks>
    /// When <c>true</c>, the repository will automatically apply tenant filtering
    /// to queries and validate tenant ownership on modifications.
    /// </remarks>
    bool IsTenantEntity { get; }

    /// <summary>
    /// Gets the database column name for the tenant identifier.
    /// </summary>
    /// <value>The column name, or <c>null</c> if not a tenant entity.</value>
    /// <remarks>
    /// This value is set via <see cref="TenantEntityMappingBuilder{TEntity, TId}.HasTenantId"/>.
    /// </remarks>
    string? TenantColumnName { get; }

    /// <summary>
    /// Gets the entity property name for the tenant identifier.
    /// </summary>
    /// <value>The property name, or <c>null</c> if not a tenant entity.</value>
    string? TenantPropertyName { get; }

    /// <summary>
    /// Gets the tenant ID from an entity instance.
    /// </summary>
    /// <param name="entity">The entity to extract the tenant ID from.</param>
    /// <returns>The tenant ID, or <c>null</c> if not set or not a tenant entity.</returns>
    string? GetTenantId(TEntity entity);

    /// <summary>
    /// Sets the tenant ID on an entity instance.
    /// </summary>
    /// <param name="entity">The entity to set the tenant ID on.</param>
    /// <param name="tenantId">The tenant ID to set.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when called on a non-tenant entity (IsTenantEntity is false).
    /// </exception>
    void SetTenantId(TEntity entity, string tenantId);
}
