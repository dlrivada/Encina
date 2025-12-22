namespace Encina.Marten;

/// <summary>
/// Base interface for aggregates that participate in event sourcing.
/// </summary>
public interface IAggregate
{
    /// <summary>
    /// Gets the unique identifier for this aggregate.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the current version of the aggregate (number of events applied).
    /// </summary>
    int Version { get; }

    /// <summary>
    /// Gets the uncommitted domain events that have been raised but not yet persisted.
    /// </summary>
    IReadOnlyList<object> UncommittedEvents { get; }

    /// <summary>
    /// Clears the list of uncommitted events after they have been persisted.
    /// </summary>
    void ClearUncommittedEvents();
}

/// <summary>
/// Base interface for aggregates with strongly-typed ID.
/// </summary>
/// <typeparam name="TId">The type of the aggregate identifier.</typeparam>
public interface IAggregate<out TId> : IAggregate
    where TId : notnull
{
    /// <summary>
    /// Gets the strongly-typed unique identifier for this aggregate.
    /// </summary>
    new TId Id { get; }
}
