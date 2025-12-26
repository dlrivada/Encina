using Marten.Services.Json.Transformations;

namespace Encina.Marten.Versioning;

/// <summary>
/// Base class for event upcasters with CLR type transformation.
/// Wraps Marten's <see cref="EventUpcaster{TEvent,TNewEvent}"/> for seamless integration.
/// </summary>
/// <typeparam name="TFrom">The source (old) event type.</typeparam>
/// <typeparam name="TTo">The target (new) event type.</typeparam>
/// <remarks>
/// <para>
/// Inherit from this class to create an upcaster that transforms events from an old schema
/// to a new one. The transformation is applied transparently during event deserialization.
/// </para>
/// <para>
/// Keep the old event type (<typeparamref name="TFrom"/>) in your codebase for deserialization.
/// It can be marked as internal or placed in a legacy namespace.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Old event schema (keep for deserialization)
/// public record OrderCreatedV1(Guid OrderId, string CustomerName);
///
/// // New event schema
/// public record OrderCreatedV2(Guid OrderId, string CustomerName, string Email);
///
/// // Upcaster implementation
/// public class OrderCreatedV1ToV2Upcaster : EventUpcasterBase&lt;OrderCreatedV1, OrderCreatedV2&gt;
/// {
///     protected override OrderCreatedV2 Upcast(OrderCreatedV1 old)
///         =&gt; new(old.OrderId, old.CustomerName, Email: "unknown@example.com");
/// }
/// </code>
/// </example>
public abstract class EventUpcasterBase<TFrom, TTo>
    : EventUpcaster<TFrom, TTo>, IEventUpcaster<TFrom, TTo>
    where TFrom : class
    where TTo : class
{
    /// <inheritdoc />
    public virtual string SourceEventTypeName => typeof(TFrom).Name;

    /// <inheritdoc />
    public Type TargetEventType => typeof(TTo);

    /// <inheritdoc />
    public Type SourceEventType => typeof(TFrom);

    /// <inheritdoc />
    TTo IEventUpcaster<TFrom, TTo>.Upcast(TFrom oldEvent)
    {
        ArgumentNullException.ThrowIfNull(oldEvent);
        return Upcast(oldEvent);
    }
}
