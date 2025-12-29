namespace Encina.DomainModeling;

/// <summary>
/// Marker interface for entities in Domain-Driven Design.
/// Entities have identity that defines their equality.
/// </summary>
public interface IEntity
{
}

/// <summary>
/// Interface for entities with a strongly-typed identifier.
/// </summary>
/// <typeparam name="TId">The type of the entity identifier.</typeparam>
public interface IEntity<out TId> : IEntity
    where TId : notnull
{
    /// <summary>
    /// Gets the unique identifier for this entity.
    /// </summary>
    TId Id { get; }
}
