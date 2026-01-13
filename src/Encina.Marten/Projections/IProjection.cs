namespace Encina.Marten.Projections;

/// <summary>
/// Defines a projection that transforms domain events into read model state.
/// </summary>
/// <typeparam name="TReadModel">The type of read model this projection produces.</typeparam>
/// <remarks>
/// <para>
/// Projections are the mechanism that transforms event streams into queryable read models.
/// Each projection handles specific event types and updates the read model accordingly.
/// </para>
/// <para>
/// <b>Event Handling</b>: Implement <see cref="IProjectionHandler{TEvent, TReadModel}"/>
/// for each event type that affects the read model.
/// </para>
/// <para>
/// <b>Idempotency</b>: Projections should be idempotent - applying the same event
/// multiple times should produce the same result. This enables safe replay and rebuild.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class OrderSummaryProjection : IProjection&lt;OrderSummary&gt;,
///     IProjectionHandler&lt;OrderCreated, OrderSummary&gt;,
///     IProjectionHandler&lt;OrderItemAdded, OrderSummary&gt;,
///     IProjectionHandler&lt;OrderCompleted, OrderSummary&gt;
/// {
///     public string ProjectionName =&gt; "OrderSummary";
///
///     public OrderSummary Create(OrderCreated domainEvent, ProjectionContext context)
///     {
///         return new OrderSummary
///         {
///             Id = context.StreamId,
///             CustomerName = domainEvent.CustomerName,
///             Status = "Created",
///             CreatedAtUtc = context.Timestamp
///         };
///     }
///
///     public OrderSummary Apply(OrderItemAdded domainEvent, OrderSummary current, ProjectionContext context)
///     {
///         current.TotalAmount += domainEvent.Price * domainEvent.Quantity;
///         current.ItemCount += domainEvent.Quantity;
///         return current;
///     }
///
///     public OrderSummary Apply(OrderCompleted domainEvent, OrderSummary current, ProjectionContext context)
///     {
///         current.Status = "Completed";
///         return current;
///     }
/// }
/// </code>
/// </example>
public interface IProjection<TReadModel> // NOSONAR S2326: TReadModel provides type-safe constraint for projection registration
    where TReadModel : class, IReadModel
{
    /// <summary>
    /// Gets the unique name of this projection.
    /// </summary>
    /// <remarks>
    /// Used for tracking projection progress and identifying projections in logs.
    /// </remarks>
    string ProjectionName { get; }
}

/// <summary>
/// Handles a specific event type within a projection.
/// </summary>
/// <typeparam name="TEvent">The event type to handle.</typeparam>
/// <typeparam name="TReadModel">The read model type being projected.</typeparam>
/// <remarks>
/// <para>
/// Implement this interface for each event type that creates or modifies the read model.
/// </para>
/// <para>
/// <b>Creating vs Updating</b>: Use <see cref="IProjectionCreator{TEvent, TReadModel}"/>
/// for events that create new read model instances (typically the first event in a stream).
/// Use this interface for events that update existing read models.
/// </para>
/// </remarks>
public interface IProjectionHandler<in TEvent, TReadModel>
    where TEvent : class
    where TReadModel : class, IReadModel
{
    /// <summary>
    /// Applies the event to an existing read model.
    /// </summary>
    /// <param name="domainEvent">The event to apply.</param>
    /// <param name="current">The current state of the read model.</param>
    /// <param name="context">Additional context about the event.</param>
    /// <returns>The updated read model.</returns>
    TReadModel Apply(TEvent domainEvent, TReadModel current, ProjectionContext context);
}

/// <summary>
/// Creates a new read model instance from an event.
/// </summary>
/// <typeparam name="TEvent">The event type that creates the read model.</typeparam>
/// <typeparam name="TReadModel">The read model type being created.</typeparam>
/// <remarks>
/// <para>
/// Implement this interface for events that create new read model instances.
/// This is typically the first event in an aggregate's event stream.
/// </para>
/// <para>
/// <b>Example</b>: An <c>OrderCreated</c> event would implement this interface
/// to create the initial <c>OrderSummary</c> read model.
/// </para>
/// </remarks>
public interface IProjectionCreator<in TEvent, TReadModel>
    where TEvent : class
    where TReadModel : class, IReadModel
{
    /// <summary>
    /// Creates a new read model instance from the event.
    /// </summary>
    /// <param name="domainEvent">The event that triggers creation.</param>
    /// <param name="context">Additional context about the event.</param>
    /// <returns>A new read model instance.</returns>
    TReadModel Create(TEvent domainEvent, ProjectionContext context);
}

/// <summary>
/// Handles deletion of a read model based on an event.
/// </summary>
/// <typeparam name="TEvent">The event type that triggers deletion.</typeparam>
/// <typeparam name="TReadModel">The read model type being deleted.</typeparam>
/// <remarks>
/// <para>
/// Implement this interface for events that should remove the read model.
/// This is useful for aggregates that have a "deleted" or "archived" state.
/// </para>
/// </remarks>
public interface IProjectionDeleter<in TEvent, TReadModel>
    where TEvent : class
    where TReadModel : class, IReadModel
{
    /// <summary>
    /// Determines whether the event should delete the read model.
    /// </summary>
    /// <param name="domainEvent">The event to evaluate.</param>
    /// <param name="current">The current state of the read model.</param>
    /// <param name="context">Additional context about the event.</param>
    /// <returns><c>true</c> if the read model should be deleted; otherwise, <c>false</c>.</returns>
    bool ShouldDelete(TEvent domainEvent, TReadModel current, ProjectionContext context);
}
