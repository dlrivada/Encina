using Marten.Services.Json.Transformations;

namespace Encina.Marten.Versioning;

/// <summary>
/// Event upcaster that uses a lambda function for transformation.
/// </summary>
/// <typeparam name="TFrom">The source (old) event type.</typeparam>
/// <typeparam name="TTo">The target (new) event type.</typeparam>
/// <remarks>
/// <para>
/// This class enables inline registration of upcasters without creating dedicated classes.
/// Useful for simple transformations where a full class would be overkill.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// options.EventVersioning.AddUpcaster&lt;OrderCreatedV1, OrderCreatedV2&gt;(
///     old => new OrderCreatedV2(old.OrderId, old.CustomerName, "unknown@example.com"));
/// </code>
/// </example>
public sealed class LambdaEventUpcaster<TFrom, TTo>
    : EventUpcaster<TFrom, TTo>, IEventUpcaster<TFrom, TTo>
    where TFrom : class
    where TTo : class
{
    private readonly Func<TFrom, TTo> _upcastFunc;
    private readonly string _sourceEventTypeName;

    /// <summary>
    /// Initializes a new instance of the <see cref="LambdaEventUpcaster{TFrom, TTo}"/> class.
    /// </summary>
    /// <param name="upcastFunc">The transformation function.</param>
    /// <param name="eventTypeName">
    /// Optional custom event type name. If not specified, uses the name of <typeparamref name="TFrom"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="upcastFunc"/> is null.
    /// </exception>
    public LambdaEventUpcaster(Func<TFrom, TTo> upcastFunc, string? eventTypeName = null)
    {
        ArgumentNullException.ThrowIfNull(upcastFunc);

        _upcastFunc = upcastFunc;
        _sourceEventTypeName = eventTypeName ?? typeof(TFrom).Name;
    }

    /// <inheritdoc />
    public string SourceEventTypeName => _sourceEventTypeName;

    /// <inheritdoc />
    public Type TargetEventType => typeof(TTo);

    /// <inheritdoc />
    public Type SourceEventType => typeof(TFrom);

    /// <summary>
    /// Gets the event type name for Marten's upcaster registration.
    /// </summary>
    public override string EventTypeName => _sourceEventTypeName;

    /// <inheritdoc />
    protected override TTo Upcast(TFrom oldEvent)
    {
        return _upcastFunc(oldEvent);
    }

    /// <inheritdoc />
    TTo IEventUpcaster<TFrom, TTo>.Upcast(TFrom oldEvent)
    {
        ArgumentNullException.ThrowIfNull(oldEvent);
        return Upcast(oldEvent);
    }
}
