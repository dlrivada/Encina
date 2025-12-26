namespace Encina.Marten.Versioning;

/// <summary>
/// Marker interface for event upcasters that transform old event schemas to new ones.
/// </summary>
/// <remarks>
/// <para>
/// Event upcasting is a process of transforming old event JSON schemas into new ones on-the-fly
/// during event deserialization. This enables schema evolution without modifying stored events.
/// </para>
/// <para>
/// Upcasters are executed each time an event is deserialized, so they should be pure functions
/// without side effects. Avoid external calls (database, network) in upcaster logic.
/// </para>
/// </remarks>
public interface IEventUpcaster
{
    /// <summary>
    /// Gets the CLR type name of the source event (as stored in the event store).
    /// </summary>
    /// <remarks>
    /// This should match the event type name stored in the database.
    /// By default, this is the simple name of the source event type.
    /// </remarks>
    string SourceEventTypeName { get; }

    /// <summary>
    /// Gets the target event type after upcasting.
    /// </summary>
    Type TargetEventType { get; }

    /// <summary>
    /// Gets the source event type before upcasting.
    /// </summary>
    Type SourceEventType { get; }
}

/// <summary>
/// Strongly-typed upcaster that transforms events from <typeparamref name="TFrom"/> to <typeparamref name="TTo"/>.
/// </summary>
/// <typeparam name="TFrom">The source (old) event type.</typeparam>
/// <typeparam name="TTo">The target (new) event type.</typeparam>
/// <remarks>
/// <para>
/// Implement this interface to define how old events should be transformed to new schemas.
/// The <see cref="Upcast"/> method should be a pure function that creates a new event
/// from the old one without side effects.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderCreatedV1ToV2Upcaster : EventUpcasterBase&lt;OrderCreatedV1, OrderCreatedV2&gt;
/// {
///     protected override OrderCreatedV2 Upcast(OrderCreatedV1 old)
///         =&gt; new(old.OrderId, old.CustomerName, Email: "unknown@example.com");
/// }
/// </code>
/// </example>
public interface IEventUpcaster<in TFrom, out TTo> : IEventUpcaster
    where TFrom : class
    where TTo : class
{
    /// <summary>
    /// Transforms an old event to the new schema.
    /// </summary>
    /// <param name="oldEvent">The event in the old schema.</param>
    /// <returns>The event transformed to the new schema.</returns>
    /// <remarks>
    /// This method should be a pure function without side effects.
    /// It is called each time an event of type <typeparamref name="TFrom"/> is deserialized.
    /// </remarks>
    TTo Upcast(TFrom oldEvent);
}
