namespace Encina.Marten;

/// <summary>
/// Base class for event-sourced aggregates.
/// </summary>
public abstract class AggregateBase : IAggregate
{
    private readonly List<object> _uncommittedEvents = [];

    /// <inheritdoc />
    public Guid Id { get; protected set; }

    /// <inheritdoc />
    public int Version { get; protected set; }

    /// <inheritdoc />
    public IReadOnlyList<object> UncommittedEvents => _uncommittedEvents.AsReadOnly();

    /// <inheritdoc />
    public void ClearUncommittedEvents() => _uncommittedEvents.Clear();

    /// <summary>
    /// Applies an event to the aggregate and adds it to the uncommitted events list.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <param name="event">The event to apply.</param>
    protected void RaiseEvent<TEvent>(TEvent @event) where TEvent : notnull
    {
        ArgumentNullException.ThrowIfNull(@event);

        // Apply the event to update aggregate state
        Apply(@event);

        // Add to uncommitted events for persistence
        _uncommittedEvents.Add(@event);

        // Increment version
        Version++;
    }

    /// <summary>
    /// Applies an event to update the aggregate state.
    /// Override this method to handle specific event types.
    /// </summary>
    /// <param name="domainEvent">The domain event to apply.</param>
    protected abstract void Apply(object domainEvent);
}

/// <summary>
/// Base class for event-sourced aggregates with strongly-typed ID.
/// </summary>
/// <typeparam name="TId">The type of the aggregate identifier.</typeparam>
public abstract class AggregateBase<TId> : AggregateBase, IAggregate<TId>
    where TId : notnull
{
#pragma warning disable IDE0032 // Use auto property - cannot use auto property due to setter logic that syncs with base.Id
    private TId _typedId = default!;
#pragma warning restore IDE0032

    /// <inheritdoc />
    public new TId Id
    {
        get => _typedId;
        protected set
        {
            _typedId = value;
            if (value is Guid guidId)
            {
                base.Id = guidId;
            }
        }
    }
}
