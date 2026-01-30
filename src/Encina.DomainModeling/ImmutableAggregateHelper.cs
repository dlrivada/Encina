namespace Encina.DomainModeling;

/// <summary>
/// Utility class for working with immutable aggregate roots in providers without change tracking.
/// </summary>
/// <remarks>
/// <para>
/// This helper is designed for non-EF Core providers (Dapper, ADO.NET, MongoDB) that lack
/// automatic change tracking. It provides a standardized pattern for preserving domain events
/// and tracking aggregates when updating immutable entities.
/// </para>
/// <para>
/// <b>Why This Helper Exists:</b>
/// When using C# records with <c>init</c> properties, the with-expression creates a new instance.
/// Domain events raised before the with-expression are on the original instance and would be lost
/// without manual intervention. Additionally, non-EF Core providers need explicit aggregate tracking
/// to collect and dispatch domain events after persistence.
/// </para>
/// <para>
/// <b>EF Core Users:</b> You typically don't need this helper. Use <c>UpdateImmutable</c> or
/// <c>UpdateImmutableAsync</c> on <see cref="IUnitOfWork"/> or <see cref="IFunctionalRepository{TEntity, TId}"/>
/// which handle everything automatically via change tracking.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Complete workflow for Dapper/ADO.NET with immutable aggregates
/// public class ShipOrderHandler : ICommandHandler&lt;ShipOrderCommand, Unit&gt;
/// {
///     private readonly IFunctionalRepository&lt;Order, Guid&gt; _repository;
///     private readonly IDomainEventCollector _eventCollector;
///     private readonly DomainEventDispatchHelper _dispatchHelper;
///
///     public async ValueTask&lt;Either&lt;EncinaError, Unit&gt;&gt; Handle(
///         ShipOrderCommand command,
///         IRequestContext context,
///         CancellationToken ct)
///     {
///         // 1. Load the order
///         var orderResult = await _repository.GetByIdAsync(command.OrderId, ct);
///
///         return await orderResult.MatchAsync(
///             RightAsync: async order =>
///             {
///                 // 2. Perform domain operation (raises event, returns new instance)
///                 var shippedOrder = order.Ship();
///
///                 // 3. Prepare for update (copies events, tracks aggregate)
///                 ImmutableAggregateHelper.PrepareForUpdate(shippedOrder, order, _eventCollector);
///
///                 // 4. Persist changes
///                 var updateResult = await _repository.UpdateAsync(shippedOrder, ct);
///
///                 // 5. Dispatch domain events
///                 if (updateResult.IsRight)
///                     await _dispatchHelper.DispatchCollectedEventsAsync(ct);
///
///                 return updateResult.Map(_ => Unit.Default);
///             },
///             Left: error => error);
///     }
/// }
/// </code>
/// </example>
public static class ImmutableAggregateHelper
{
    /// <summary>
    /// Prepares an immutable aggregate for update by copying domain events and registering for event dispatch.
    /// </summary>
    /// <typeparam name="TAggregate">The aggregate root type.</typeparam>
    /// <param name="modified">The modified aggregate instance (created via with-expression or clone).</param>
    /// <param name="original">The original aggregate instance containing the domain events.</param>
    /// <param name="collector">The domain event collector to track the aggregate for event dispatch.</param>
    /// <returns>The modified aggregate for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="modified"/>, <paramref name="original"/>, or <paramref name="collector"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method performs two essential operations:
    /// <list type="number">
    /// <item><description><b>Event Preservation:</b> Copies domain events from the original aggregate to the
    /// modified aggregate using <see cref="IAggregateRoot.CopyEventsFrom"/>. This ensures events raised
    /// before the with-expression are not lost.</description></item>
    /// <item><description><b>Event Tracking:</b> Registers the modified aggregate with the
    /// <see cref="IDomainEventCollector"/> so its events can be collected and dispatched after
    /// the persistence operation completes.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Important:</b> After calling this method, you must:
    /// <list type="bullet">
    /// <item><description>Call the repository's <c>UpdateAsync</c> method to persist changes</description></item>
    /// <item><description>Use <see cref="DomainEventDispatchHelper"/> to dispatch collected events</description></item>
    /// <item><description>Call <see cref="IDomainEventCollector.ClearCollectedEvents"/> after successful dispatch</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Simple usage
    /// var order = await repository.GetByIdAsync(orderId);
    /// var shippedOrder = order.Ship(); // Returns new instance with OrderShippedEvent
    ///
    /// // Prepare: copies events from 'order' to 'shippedOrder' and tracks 'shippedOrder'
    /// ImmutableAggregateHelper.PrepareForUpdate(shippedOrder, order, eventCollector);
    ///
    /// await repository.UpdateAsync(shippedOrder);
    /// await dispatchHelper.DispatchCollectedEventsAsync();
    ///
    /// // Fluent chaining
    /// var preparedOrder = ImmutableAggregateHelper.PrepareForUpdate(
    ///     order.Ship(),
    ///     order,
    ///     eventCollector);
    /// </code>
    /// </example>
    public static TAggregate PrepareForUpdate<TAggregate>(
        TAggregate modified,
        TAggregate original,
        IDomainEventCollector collector)
        where TAggregate : class, IAggregateRoot
    {
        ArgumentNullException.ThrowIfNull(modified);
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(collector);

        // Copy domain events from the original aggregate to the modified one
        modified.CopyEventsFrom(original);

        // Track the modified aggregate for event collection and dispatch
        collector.TrackAggregate(modified);

        return modified;
    }
}
