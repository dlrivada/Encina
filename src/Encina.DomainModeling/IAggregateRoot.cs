namespace Encina.DomainModeling;

/// <summary>
/// Marker interface for aggregate roots in Domain-Driven Design.
/// </summary>
/// <remarks>
/// <para>
/// An aggregate root is the main entity that controls access to a cluster of related entities.
/// All external references should only be to the aggregate root.
/// </para>
/// </remarks>
public interface IAggregateRoot : IEntity
{
    /// <summary>
    /// Gets the domain events that have been raised but not yet dispatched.
    /// </summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    /// <summary>
    /// Clears the list of domain events after they have been dispatched.
    /// </summary>
    void ClearDomainEvents();
}

/// <summary>
/// Interface for aggregate roots with a strongly-typed identifier.
/// </summary>
/// <typeparam name="TId">The type of the aggregate root identifier.</typeparam>
public interface IAggregateRoot<out TId> : IAggregateRoot, IEntity<TId>
    where TId : notnull
{
}
