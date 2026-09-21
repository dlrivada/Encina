namespace Encina.DomainModeling;

/// <summary>
/// Base interface for aggregates that participate in event sourcing.
/// </summary>
/// <remarks>
/// <para>
/// This interface defines the contract for event-sourced aggregates where state is reconstructed
/// by replaying a sequence of domain events. Unlike <see cref="IAggregateRoot"/> which uses domain
/// events for side effects (notifications to other aggregates), <see cref="IAggregate"/> uses events
/// as the primary mechanism for state changes.
/// </para>
/// <para>
/// Key differences from <see cref="IAggregateRoot"/>:
/// <list type="bullet">
///   <item><description><see cref="IAggregate"/>: Events ARE the state - state is built by applying events.</description></item>
///   <item><description><see cref="IAggregateRoot"/>: Events are side effects - state is persisted directly.</description></item>
/// </list>
/// </para>
/// </remarks>
/// <seealso cref="AggregateBase"/>
/// <seealso cref="IAggregateRoot"/>
public interface IAggregate
{
    /// <summary>
    /// Gets the unique identifier for this aggregate.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets or sets the current version of the aggregate (number of events applied).
    /// </summary>
    /// <remarks>
    /// The setter is needed by snapshot-based repositories to restore the version recorded
    /// with a snapshot before replaying the events that follow it. Ordinary replay goes through
    /// <see cref="LoadFromHistory"/>, which maintains the version itself.
    /// </remarks>
    int Version { get; set; }

    /// <summary>
    /// Rebuilds the aggregate state from its persisted event history.
    /// </summary>
    /// <param name="history">The persisted events, oldest first.</param>
    /// <remarks>
    /// <para>
    /// Each event is applied to the aggregate state and increments <see cref="Version"/>,
    /// exactly as when it was originally raised, but without being added to
    /// <see cref="UncommittedEvents"/>: replayed events are already persisted.
    /// </para>
    /// <para>
    /// Event store repositories call this method after fetching a stream, which keeps
    /// event replay provider-agnostic: the aggregate, not the event store, decides how an
    /// event mutates its state.
    /// </para>
    /// </remarks>
    void LoadFromHistory(IEnumerable<object> history);

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
/// Base interface for event-sourced aggregates with strongly-typed ID.
/// </summary>
/// <typeparam name="TId">The type of the aggregate identifier.</typeparam>
/// <seealso cref="AggregateBase{TId}"/>
public interface IAggregate<out TId> : IAggregate
    where TId : notnull
{
    /// <summary>
    /// Gets the strongly-typed unique identifier for this aggregate.
    /// </summary>
    new TId Id { get; }
}
