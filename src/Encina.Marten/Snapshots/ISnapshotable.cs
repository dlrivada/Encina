using Encina.DomainModeling;

namespace Encina.Marten.Snapshots;

/// <summary>
/// Marker interface indicating that an aggregate supports snapshotting.
/// Aggregates implementing this interface can have their state periodically
/// captured to improve load performance for long event streams.
/// </summary>
/// <remarks>
/// <para>
/// Snapshotting is an optimization technique for event-sourced aggregates
/// with many events. Instead of replaying all events from the beginning,
/// the system loads the most recent snapshot and only replays events
/// that occurred after the snapshot was taken.
/// </para>
/// <para>
/// Aggregates must implement <see cref="ISnapshotable{TAggregate}"/> with
/// themselves as the type parameter. Marten handles serialization automatically,
/// so no special snapshot creation logic is required.
/// </para>
/// </remarks>
/// <typeparam name="TAggregate">The aggregate type (must be the implementing type).</typeparam>
/// <example>
/// <code>
/// public sealed class Order : AggregateBase, ISnapshotable&lt;Order&gt;
/// {
///     public string CustomerName { get; private set; } = string.Empty;
///     public decimal Total { get; private set; }
///     public OrderStatus Status { get; private set; }
///
///     // ... event handling methods ...
///
///     protected override void Apply(object domainEvent)
///     {
///         switch (domainEvent)
///         {
///             case OrderCreated e:
///                 CustomerName = e.CustomerName;
///                 Status = OrderStatus.Created;
///                 break;
///             case OrderItemAdded e:
///                 Total += e.Price * e.Quantity;
///                 break;
///         }
///     }
/// }
/// </code>
/// </example>
public interface ISnapshotable<TAggregate>
    where TAggregate : class, IAggregate
{
}
