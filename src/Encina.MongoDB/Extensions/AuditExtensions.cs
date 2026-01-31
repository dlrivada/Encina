using Encina.DomainModeling;

namespace Encina.MongoDB.Extensions;

/// <summary>
/// Extension methods for populating audit fields on entities before persistence.
/// </summary>
/// <remarks>
/// <para>
/// These extension methods provide a fluent API for populating audit fields when using
/// MongoDB for data access. They wrap the <see cref="AuditFieldPopulator"/> static methods
/// to provide a more natural syntax.
/// </para>
/// <para>
/// <b>When to Use</b>: Use these methods before executing InsertOne/UpdateOne operations
/// with the MongoDB driver, as there is no automatic interception like in EF Core.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Before inserting a new document
/// var order = new Order { CustomerId = customerId, Total = 100m }
///     .WithAuditCreate(context.UserId, TimeProvider.System);
/// await collection.InsertOneAsync(order);
///
/// // Before updating an existing document
/// order.Status = OrderStatus.Shipped;
/// order.WithAuditUpdate(context.UserId, TimeProvider.System);
/// await collection.ReplaceOneAsync(filter, order);
///
/// // For soft delete
/// order.WithAuditDelete(context.UserId, TimeProvider.System);
/// await collection.ReplaceOneAsync(filter, order);
/// </code>
/// </example>
public static class AuditExtensions
{
    /// <summary>
    /// Populates creation audit fields on the entity.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to populate.</param>
    /// <param name="userId">The user ID to set as creator.</param>
    /// <param name="timeProvider">The time provider for timestamps.</param>
    /// <returns>The entity for method chaining.</returns>
    /// <remarks>
    /// Sets <see cref="ICreatedAtUtc.CreatedAtUtc"/> and <see cref="ICreatedBy.CreatedBy"/>
    /// if the entity implements the corresponding interfaces.
    /// </remarks>
    /// <example>
    /// <code>
    /// var order = new Order { CustomerId = customerId }
    ///     .WithAuditCreate(context.UserId, TimeProvider.System);
    /// </code>
    /// </example>
    public static T WithAuditCreate<T>(this T entity, string? userId, TimeProvider timeProvider)
        where T : class
    {
        return AuditFieldPopulator.PopulateForCreate(entity, userId, timeProvider);
    }

    /// <summary>
    /// Populates modification audit fields on the entity.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to populate.</param>
    /// <param name="userId">The user ID to set as modifier.</param>
    /// <param name="timeProvider">The time provider for timestamps.</param>
    /// <returns>The entity for method chaining.</returns>
    /// <remarks>
    /// Sets <see cref="IModifiedAtUtc.ModifiedAtUtc"/> and <see cref="IModifiedBy.ModifiedBy"/>
    /// if the entity implements the corresponding interfaces.
    /// </remarks>
    /// <example>
    /// <code>
    /// order.WithAuditUpdate(context.UserId, TimeProvider.System);
    /// </code>
    /// </example>
    public static T WithAuditUpdate<T>(this T entity, string? userId, TimeProvider timeProvider)
        where T : class
    {
        return AuditFieldPopulator.PopulateForUpdate(entity, userId, timeProvider);
    }

    /// <summary>
    /// Populates soft delete audit fields on the entity.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to populate.</param>
    /// <param name="userId">The user ID to set as deleter.</param>
    /// <param name="timeProvider">The time provider for timestamps.</param>
    /// <returns>The entity for method chaining.</returns>
    /// <remarks>
    /// Sets <see cref="ISoftDeletable.IsDeleted"/>, <see cref="ISoftDeletable.DeletedAtUtc"/>,
    /// and <see cref="ISoftDeletable.DeletedBy"/> if the entity implements
    /// <see cref="ISoftDeletable"/> and the properties have public setters.
    /// </remarks>
    /// <example>
    /// <code>
    /// order.WithAuditDelete(context.UserId, TimeProvider.System);
    /// </code>
    /// </example>
    public static T WithAuditDelete<T>(this T entity, string? userId, TimeProvider timeProvider)
        where T : class
    {
        return AuditFieldPopulator.PopulateForDelete(entity, userId, timeProvider);
    }

    /// <summary>
    /// Restores a soft-deleted entity by clearing deletion fields.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to restore.</param>
    /// <returns>The entity for method chaining.</returns>
    /// <remarks>
    /// Clears <see cref="ISoftDeletable.IsDeleted"/>, <see cref="ISoftDeletable.DeletedAtUtc"/>,
    /// and <see cref="ISoftDeletable.DeletedBy"/> if the entity implements
    /// <see cref="ISoftDeletable"/> and the properties have public setters.
    /// </remarks>
    /// <example>
    /// <code>
    /// order.WithAuditRestore();
    /// </code>
    /// </example>
    public static T WithAuditRestore<T>(this T entity)
        where T : class
    {
        return AuditFieldPopulator.RestoreFromDelete(entity);
    }
}
