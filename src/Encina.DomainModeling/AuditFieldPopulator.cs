namespace Encina.DomainModeling;

/// <summary>
/// Provides static methods to populate audit fields on entities implementing audit interfaces.
/// </summary>
/// <remarks>
/// <para>
/// This class provides explicit helper methods for populating audit fields in scenarios where
/// automatic interceptors are not available, such as Dapper, ADO.NET, and MongoDB providers.
/// </para>
/// <para>
/// <b>Supported Interfaces</b>:
/// <list type="bullet">
/// <item><description><see cref="ICreatedAtUtc"/>: Creation timestamp</description></item>
/// <item><description><see cref="ICreatedBy"/>: Creation user</description></item>
/// <item><description><see cref="IModifiedAtUtc"/>: Modification timestamp</description></item>
/// <item><description><see cref="IModifiedBy"/>: Modification user</description></item>
/// <item><description><see cref="ISoftDeletable"/>: Soft delete fields</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Pattern</b>: Call these methods before persisting entities to the database
/// when using data access providers that don't support automatic interception.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Before inserting a new entity
/// var order = new Order { CustomerId = customerId, Total = 100m };
/// AuditFieldPopulator.PopulateForCreate(order, userId, TimeProvider.System);
/// await connection.ExecuteAsync("INSERT INTO Orders ...", order);
///
/// // Before updating an existing entity
/// AuditFieldPopulator.PopulateForUpdate(order, userId, TimeProvider.System);
/// await connection.ExecuteAsync("UPDATE Orders SET ...", order);
///
/// // For soft delete
/// AuditFieldPopulator.PopulateForDelete(order, userId, TimeProvider.System);
/// await connection.ExecuteAsync("UPDATE Orders SET IsDeleted = 1 ...", order);
/// </code>
/// </example>
public static class AuditFieldPopulator
{
    /// <summary>
    /// Populates creation audit fields on an entity.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to populate.</param>
    /// <param name="userId">The user ID to set as creator.</param>
    /// <param name="timeProvider">The time provider for timestamps.</param>
    /// <returns>The entity for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method checks if the entity implements <see cref="ICreatedAtUtc"/> and/or
    /// <see cref="ICreatedBy"/>, and sets the corresponding properties if present.
    /// </para>
    /// <para>
    /// <b>Properties Set</b>:
    /// <list type="bullet">
    /// <item><description><see cref="ICreatedAtUtc.CreatedAtUtc"/>: Set to current UTC time from <paramref name="timeProvider"/></description></item>
    /// <item><description><see cref="ICreatedBy.CreatedBy"/>: Set to <paramref name="userId"/> if not null</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> or <paramref name="timeProvider"/> is null.</exception>
    /// <example>
    /// <code>
    /// var order = new Order { CustomerId = customerId };
    /// AuditFieldPopulator.PopulateForCreate(order, context.UserId, TimeProvider.System);
    /// </code>
    /// </example>
    public static T PopulateForCreate<T>(T entity, string? userId, TimeProvider timeProvider)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(timeProvider);

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        if (entity is ICreatedAtUtc createdAtEntity)
        {
            createdAtEntity.CreatedAtUtc = nowUtc;
        }

        if (entity is ICreatedBy createdByEntity && userId is not null)
        {
            createdByEntity.CreatedBy = userId;
        }

        return entity;
    }

    /// <summary>
    /// Populates modification audit fields on an entity.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to populate.</param>
    /// <param name="userId">The user ID to set as modifier.</param>
    /// <param name="timeProvider">The time provider for timestamps.</param>
    /// <returns>The entity for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method checks if the entity implements <see cref="IModifiedAtUtc"/> and/or
    /// <see cref="IModifiedBy"/>, and sets the corresponding properties if present.
    /// </para>
    /// <para>
    /// <b>Properties Set</b>:
    /// <list type="bullet">
    /// <item><description><see cref="IModifiedAtUtc.ModifiedAtUtc"/>: Set to current UTC time from <paramref name="timeProvider"/></description></item>
    /// <item><description><see cref="IModifiedBy.ModifiedBy"/>: Set to <paramref name="userId"/> if not null</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> or <paramref name="timeProvider"/> is null.</exception>
    /// <example>
    /// <code>
    /// var order = existingOrder;
    /// order.Status = OrderStatus.Shipped;
    /// AuditFieldPopulator.PopulateForUpdate(order, context.UserId, TimeProvider.System);
    /// </code>
    /// </example>
    public static T PopulateForUpdate<T>(T entity, string? userId, TimeProvider timeProvider)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(timeProvider);

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        if (entity is IModifiedAtUtc modifiedAtEntity)
        {
            modifiedAtEntity.ModifiedAtUtc = nowUtc;
        }

        if (entity is IModifiedBy modifiedByEntity && userId is not null)
        {
            modifiedByEntity.ModifiedBy = userId;
        }

        return entity;
    }

    /// <summary>
    /// Populates soft delete audit fields on an entity.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to populate.</param>
    /// <param name="userId">The user ID to set as deleter.</param>
    /// <param name="timeProvider">The time provider for timestamps.</param>
    /// <returns>The entity for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method checks if the entity implements <see cref="ISoftDeletable"/>,
    /// and sets the deletion properties if present. This includes setting
    /// <c>IsDeleted</c> to <see langword="true"/>.
    /// </para>
    /// <para>
    /// <b>Properties Set</b>:
    /// <list type="bullet">
    /// <item><description><see cref="ISoftDeletable.IsDeleted"/>: Not directly set (entity's Delete method should handle this)</description></item>
    /// <item><description><see cref="ISoftDeletable.DeletedAtUtc"/>: Set to current UTC time from <paramref name="timeProvider"/></description></item>
    /// <item><description><see cref="ISoftDeletable.DeletedBy"/>: Set to <paramref name="userId"/> if not null</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Note</b>: For entities extending <see cref="SoftDeletableAggregateRoot{TId}"/> or
    /// <see cref="FullyAuditedAggregateRoot{TId}"/>, prefer using the <c>Delete(string?)</c>
    /// method on the entity itself, which properly sets all deletion fields including <c>IsDeleted</c>.
    /// This method is provided for cases where you need to set fields manually.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> or <paramref name="timeProvider"/> is null.</exception>
    /// <example>
    /// <code>
    /// // Option 1: Use entity's Delete method (preferred for aggregate roots)
    /// order.Delete(context.UserId);
    ///
    /// // Option 2: Use populator for manual control
    /// if (order is ISoftDeletable softDeletable)
    /// {
    ///     softDeletable.IsDeleted = true; // Must set manually
    /// }
    /// AuditFieldPopulator.PopulateForDelete(order, context.UserId, TimeProvider.System);
    /// </code>
    /// </example>
    public static T PopulateForDelete<T>(T entity, string? userId, TimeProvider timeProvider)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (entity is ISoftDeletable softDeletable)
        {
            var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

            // Note: We don't set IsDeleted here because ISoftDeletable.IsDeleted has only a getter.
            // For entities with public setters (like FullyAuditedAggregateRoot), we need to check
            // if the property has a setter and use it.
            var isDeletedProperty = entity.GetType().GetProperty(nameof(ISoftDeletable.IsDeleted));
            if (isDeletedProperty?.CanWrite == true)
            {
                isDeletedProperty.SetValue(entity, true);
            }

            var deletedAtProperty = entity.GetType().GetProperty(nameof(ISoftDeletable.DeletedAtUtc));
            if (deletedAtProperty?.CanWrite == true)
            {
                deletedAtProperty.SetValue(entity, nowUtc);
            }

            var deletedByProperty = entity.GetType().GetProperty(nameof(ISoftDeletable.DeletedBy));
            if (deletedByProperty?.CanWrite == true && userId is not null)
            {
                deletedByProperty.SetValue(entity, userId);
            }
        }

        return entity;
    }

    /// <summary>
    /// Restores a soft-deleted entity by clearing deletion fields.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to restore.</param>
    /// <returns>The entity for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method checks if the entity implements <see cref="ISoftDeletable"/>,
    /// and clears the deletion properties if present.
    /// </para>
    /// <para>
    /// <b>Properties Cleared</b>:
    /// <list type="bullet">
    /// <item><description><see cref="ISoftDeletable.IsDeleted"/>: Set to <see langword="false"/></description></item>
    /// <item><description><see cref="ISoftDeletable.DeletedAtUtc"/>: Set to <see langword="null"/></description></item>
    /// <item><description><see cref="ISoftDeletable.DeletedBy"/>: Set to <see langword="null"/></description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Note</b>: For entities extending <see cref="SoftDeletableAggregateRoot{TId}"/> or
    /// <see cref="FullyAuditedAggregateRoot{TId}"/>, prefer using the <c>Restore()</c>
    /// method on the entity itself.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is null.</exception>
    /// <example>
    /// <code>
    /// // Option 1: Use entity's Restore method (preferred for aggregate roots)
    /// order.Restore();
    ///
    /// // Option 2: Use populator for manual control
    /// AuditFieldPopulator.RestoreFromDelete(order);
    /// </code>
    /// </example>
    public static T RestoreFromDelete<T>(T entity)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity is ISoftDeletable)
        {
            var isDeletedProperty = entity.GetType().GetProperty(nameof(ISoftDeletable.IsDeleted));
            if (isDeletedProperty?.CanWrite == true)
            {
                isDeletedProperty.SetValue(entity, false);
            }

            var deletedAtProperty = entity.GetType().GetProperty(nameof(ISoftDeletable.DeletedAtUtc));
            if (deletedAtProperty?.CanWrite == true)
            {
                deletedAtProperty.SetValue(entity, null);
            }

            var deletedByProperty = entity.GetType().GetProperty(nameof(ISoftDeletable.DeletedBy));
            if (deletedByProperty?.CanWrite == true)
            {
                deletedByProperty.SetValue(entity, null);
            }
        }

        return entity;
    }
}
