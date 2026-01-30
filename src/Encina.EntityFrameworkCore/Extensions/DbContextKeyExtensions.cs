using Microsoft.EntityFrameworkCore;

namespace Encina.EntityFrameworkCore.Extensions;

/// <summary>
/// Extension methods for extracting primary key values from entities using EF Core metadata.
/// </summary>
/// <remarks>
/// <para>
/// These utilities provide a way to extract primary key values from entities without
/// requiring knowledge of the entity's specific key property. This is useful for
/// generic operations like immutable updates, bulk operations, and caching.
/// </para>
/// </remarks>
public static class DbContextKeyExtensions
{
    /// <summary>
    /// Extracts the primary key value from an entity using EF Core metadata.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="context">The DbContext that contains the entity model.</param>
    /// <param name="entity">The entity to extract the key from.</param>
    /// <returns>The primary key value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> or <paramref name="entity"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the entity type is not found in the model or does not have a primary key defined.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method uses EF Core's metadata API to discover the primary key property
    /// and extract its value. It supports entities with simple (single-property) primary keys.
    /// </para>
    /// <para>
    /// For composite keys, this method returns only the first key property value.
    /// Use <see cref="GetPrimaryKeyValues{TEntity}"/> for composite keys.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var orderId = context.GetPrimaryKeyValue(order);
    /// var existingOrder = context.Orders.Local.FirstOrDefault(o =>
    ///     context.GetPrimaryKeyValue(o).Equals(orderId));
    /// </code>
    /// </example>
    public static object GetPrimaryKeyValue<TEntity>(this DbContext context, TEntity entity)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(entity);

        var entityType = context.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException(
                $"Entity type '{typeof(TEntity).Name}' is not part of the model for this DbContext.");

        var primaryKey = entityType.FindPrimaryKey()
            ?? throw new InvalidOperationException(
                $"Entity type '{typeof(TEntity).Name}' does not have a primary key defined.");

        var keyProperty = primaryKey.Properties[0];

        return context.Entry(entity).Property(keyProperty.Name).CurrentValue
            ?? throw new InvalidOperationException(
                $"Primary key value for entity type '{typeof(TEntity).Name}' is null.");
    }

    /// <summary>
    /// Extracts all primary key values from an entity using EF Core metadata.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="context">The DbContext that contains the entity model.</param>
    /// <param name="entity">The entity to extract the keys from.</param>
    /// <returns>An array of primary key values in the order they are defined.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> or <paramref name="entity"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the entity type is not found in the model or does not have a primary key defined.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method supports both simple and composite primary keys. For simple keys,
    /// the returned array will have a single element.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // For composite key (OrderId, ProductId)
    /// var keys = context.GetPrimaryKeyValues(orderLine);
    /// // keys[0] = OrderId value, keys[1] = ProductId value
    /// </code>
    /// </example>
    public static object[] GetPrimaryKeyValues<TEntity>(this DbContext context, TEntity entity)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(entity);

        var entityType = context.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException(
                $"Entity type '{typeof(TEntity).Name}' is not part of the model for this DbContext.");

        var primaryKey = entityType.FindPrimaryKey()
            ?? throw new InvalidOperationException(
                $"Entity type '{typeof(TEntity).Name}' does not have a primary key defined.");

        var entry = context.Entry(entity);
        var keyValues = new object[primaryKey.Properties.Count];

        for (var i = 0; i < primaryKey.Properties.Count; i++)
        {
            var keyProperty = primaryKey.Properties[i];
            keyValues[i] = entry.Property(keyProperty.Name).CurrentValue
                ?? throw new InvalidOperationException(
                    $"Primary key property '{keyProperty.Name}' for entity type '{typeof(TEntity).Name}' is null.");
        }

        return keyValues;
    }

    /// <summary>
    /// Gets the name of the primary key property for an entity type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="context">The DbContext that contains the entity model.</param>
    /// <returns>The name of the primary key property.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the entity type is not found in the model or does not have a primary key defined.
    /// </exception>
    /// <remarks>
    /// <para>
    /// For composite keys, this method returns only the name of the first key property.
    /// </para>
    /// </remarks>
    public static string GetPrimaryKeyPropertyName<TEntity>(this DbContext context)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(context);

        var entityType = context.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException(
                $"Entity type '{typeof(TEntity).Name}' is not part of the model for this DbContext.");

        var primaryKey = entityType.FindPrimaryKey()
            ?? throw new InvalidOperationException(
                $"Entity type '{typeof(TEntity).Name}' does not have a primary key defined.");

        return primaryKey.Properties[0].Name;
    }
}
