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

    /// <summary>
    /// Copies all domain events from another aggregate root to this instance.
    /// </summary>
    /// <param name="source">The source aggregate root to copy events from.</param>
    /// <remarks>
    /// <para>
    /// This method is used to preserve domain events when working with immutable entities
    /// (C# records). When updating an immutable entity using a with-expression, the new
    /// instance won't have the domain events from the original. This method allows
    /// infrastructure code to transfer those events to the new instance.
    /// </para>
    /// <para>
    /// Example usage with immutable records:
    /// <code>
    /// var original = await repository.GetByIdAsync(orderId);
    /// var updated = original with { Status = OrderStatus.Shipped };
    /// updated.CopyEventsFrom(original);
    /// </code>
    /// </para>
    /// </remarks>
    void CopyEventsFrom(IAggregateRoot source);
}

/// <summary>
/// Interface for aggregate roots with a strongly-typed identifier.
/// </summary>
/// <typeparam name="TId">The type of the aggregate root identifier.</typeparam>
public interface IAggregateRoot<out TId> : IAggregateRoot, IEntity<TId>
    where TId : notnull
{
}
