namespace Encina.DomainModeling;

/// <summary>
/// Base class for event-sourced aggregates where state is built by applying domain events.
/// </summary>
/// <remarks>
/// <para>
/// This class provides the infrastructure for event sourcing where aggregate state is reconstructed
/// by replaying a sequence of domain events. Each state change is captured as an event through
/// <see cref="RaiseEvent{TEvent}"/>, and the <see cref="Apply"/> method is called to update the aggregate state.
/// </para>
/// <para>
/// <strong>Event Sourcing vs State-Based Persistence:</strong>
/// </para>
/// <para>
/// Use <see cref="AggregateBase"/> when:
/// <list type="bullet">
///   <item><description>You need a complete audit trail of all changes.</description></item>
///   <item><description>You want to reconstruct past states (temporal queries).</description></item>
///   <item><description>You're using an event store like Marten or EventStoreDB.</description></item>
/// </list>
/// </para>
/// <para>
/// Use <see cref="AggregateRoot{TId}"/> when:
/// <list type="bullet">
///   <item><description>You're using state-based persistence (EF Core, Dapper, ADO.NET).</description></item>
///   <item><description>Domain events are for notifications, not state changes.</description></item>
///   <item><description>Simpler persistence requirements.</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class Order : AggregateBase&lt;Guid&gt;
/// {
///     public string CustomerName { get; private set; } = string.Empty;
///     public OrderStatus Status { get; private set; }
///
///     // Command - raises event
///     public void Create(Guid id, string customerName)
///     {
///         RaiseEvent(new OrderCreated(id, customerName));
///     }
///
///     // Apply - updates state from event
///     protected override void Apply(object domainEvent)
///     {
///         switch (domainEvent)
///         {
///             case OrderCreated e:
///                 Id = e.OrderId;
///                 CustomerName = e.CustomerName;
///                 Status = OrderStatus.Pending;
///                 break;
///         }
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="IAggregate"/>
/// <seealso cref="AggregateRoot{TId}"/>
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
/// <seealso cref="AggregateBase"/>
/// <seealso cref="IAggregate{TId}"/>
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
