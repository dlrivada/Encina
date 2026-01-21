namespace Encina.MongoDB.Tenancy;

/// <summary>
/// Entity mapping that includes tenant information for MongoDB.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// This interface provides tenant-specific mapping information
/// for MongoDB entities, such as the tenant field name.
/// </para>
/// <para>
/// Use <see cref="TenantEntityMappingBuilder{TEntity, TId}"/> for fluent configuration.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddTenantAwareRepository&lt;Order, Guid&gt;(mapping =&gt;
///     mapping.ToCollection("orders")
///            .HasId(o =&gt; o.Id)
///            .HasTenantId(o =&gt; o.TenantId)
///            .MapField(o =&gt; o.CustomerId)
///            .MapField(o =&gt; o.Total));
/// </code>
/// </example>
public interface ITenantEntityMapping<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    /// <summary>
    /// Gets the MongoDB collection name for this entity.
    /// </summary>
    string CollectionName { get; }

    /// <summary>
    /// Gets the field name for the identifier in MongoDB documents.
    /// </summary>
    /// <value>Typically "_id" for MongoDB.</value>
    string IdFieldName { get; }

    /// <summary>
    /// Gets the identifier from an entity instance.
    /// </summary>
    /// <param name="entity">The entity to get the ID from.</param>
    /// <returns>The entity identifier.</returns>
    TId GetId(TEntity entity);

    /// <summary>
    /// Gets the field mappings from property names to MongoDB field names.
    /// </summary>
    IReadOnlyDictionary<string, string> FieldMappings { get; }

    /// <summary>
    /// Gets a value indicating whether this entity is tenant-scoped.
    /// </summary>
    /// <value><c>true</c> if the entity has a tenant ID property; otherwise, <c>false</c>.</value>
    bool IsTenantEntity { get; }

    /// <summary>
    /// Gets the field name for the tenant identifier in MongoDB documents.
    /// </summary>
    /// <value>The tenant field name, or <c>null</c> if this is not a tenant entity.</value>
    string? TenantFieldName { get; }

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
