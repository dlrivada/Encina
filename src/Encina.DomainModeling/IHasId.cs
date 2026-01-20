namespace Encina.DomainModeling;

/// <summary>
/// Interface for entities with an identifier.
/// </summary>
/// <typeparam name="TId">The type of the identifier.</typeparam>
/// <remarks>
/// <para>
/// This interface enables generic ID extraction in repository implementations
/// without requiring reflection. Entities implementing this interface can be
/// used with the generic repository pattern.
/// </para>
/// <para>
/// Unlike <see cref="IEntity{TId}"/>, this interface does not carry the
/// semantic meaning of a domain entity and can be implemented by any class
/// that has an identifier.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class Order : IHasId&lt;Guid&gt;
/// {
///     public Guid Id { get; init; }
///     public string CustomerName { get; set; }
/// }
/// </code>
/// </example>
public interface IHasId<out TId>
    where TId : notnull
{
    /// <summary>
    /// Gets the unique identifier for this object.
    /// </summary>
    TId Id { get; }
}

/// <summary>
/// Extension methods for <see cref="IHasId{TId}"/>.
/// </summary>
public static class HasIdExtensions
{
    /// <summary>
    /// Gets the ID from an object that has an identifier.
    /// </summary>
    /// <typeparam name="TId">The type of the identifier.</typeparam>
    /// <param name="entity">The entity to get the ID from.</param>
    /// <returns>The entity's identifier.</returns>
    /// <exception cref="ArgumentNullException">Thrown when entity is null.</exception>
    public static TId GetId<TId>(this IHasId<TId> entity)
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(entity);
        return entity.Id;
    }

    /// <summary>
    /// Checks if two entities have the same ID.
    /// </summary>
    /// <typeparam name="TId">The type of the identifier.</typeparam>
    /// <param name="entity">The first entity.</param>
    /// <param name="other">The second entity.</param>
    /// <returns>True if both entities have the same ID; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when entity is null.</exception>
    public static bool HasSameId<TId>(this IHasId<TId> entity, IHasId<TId>? other)
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (other is null)
        {
            return false;
        }

        return EqualityComparer<TId>.Default.Equals(entity.Id, other.Id);
    }

    /// <summary>
    /// Checks if an entity has a specific ID.
    /// </summary>
    /// <typeparam name="TId">The type of the identifier.</typeparam>
    /// <param name="entity">The entity to check.</param>
    /// <param name="id">The ID to compare against.</param>
    /// <returns>True if the entity has the specified ID; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when entity is null.</exception>
    public static bool HasId<TId>(this IHasId<TId> entity, TId id)
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(entity);
        return EqualityComparer<TId>.Default.Equals(entity.Id, id);
    }

    /// <summary>
    /// Extracts the IDs from a collection of entities.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The type of the identifier.</typeparam>
    /// <param name="entities">The collection of entities.</param>
    /// <returns>A list of IDs from the entities.</returns>
    /// <exception cref="ArgumentNullException">Thrown when entities is null.</exception>
    public static IReadOnlyList<TId> GetIds<TEntity, TId>(this IEnumerable<TEntity> entities)
        where TEntity : IHasId<TId>
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(entities);
        return entities.Select(e => e.Id).ToList();
    }
}
