namespace Encina.Messaging.Choreography;

/// <summary>
/// Represents a reaction to an event in a choreography saga.
/// </summary>
/// <typeparam name="TEvent">The type of event to react to.</typeparam>
/// <remarks>
/// <para>
/// Event reactions are the building blocks of choreography-based sagas.
/// Each reaction handles one event type and can:
/// <list type="bullet">
/// <item><description>Execute business logic</description></item>
/// <item><description>Publish new events to continue the flow</description></item>
/// <item><description>Register compensation actions</description></item>
/// </list>
/// </para>
/// <para>
/// Unlike orchestrated sagas where a central coordinator controls the flow,
/// choreography sagas let each participant decide the next step based on events.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderCreatedReaction : IEventReaction&lt;OrderCreatedEvent&gt;
/// {
///     private readonly IInventoryService _inventory;
///
///     public async Task ReactAsync(OrderCreatedEvent @event, IEventHandlerScope scope, CancellationToken ct)
///     {
///         // Reserve inventory
///         var reservationId = await _inventory.ReserveAsync(@event.Items, ct);
///
///         // Register compensation in case of later failure
///         scope.AddCompensation(async token =&gt; await _inventory.ReleaseAsync(reservationId, token));
///
///         // Publish next event in the chain
///         await scope.PublishAsync(new InventoryReservedEvent(
///             OrderId: @event.OrderId,
///             ReservationId: reservationId
///         ), ct);
///     }
/// }
/// </code>
/// </example>
public interface IEventReaction<in TEvent>
    where TEvent : class
{
    /// <summary>
    /// Handles the event and potentially triggers the next step in the choreography.
    /// </summary>
    /// <param name="domainEvent">The event to react to.</param>
    /// <param name="scope">The event handler scope providing saga context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ReactAsync(TEvent domainEvent, IEventHandlerScope scope, CancellationToken cancellationToken);
}
