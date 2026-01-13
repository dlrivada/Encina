using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.Messaging.ContentRouter;

/// <summary>
/// Factory for creating content router definitions.
/// </summary>
/// <remarks>
/// <para>
/// This is the entry point for the content-based router fluent API.
/// Use <see cref="Create{TMessage, TResult}"/> to start defining routing rules.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var router = ContentRouterBuilder.Create&lt;Order, OrderResult&gt;()
///     .When(o => o.Total > 10000)
///         .RouteTo(async (order, ct) => await ProcessHighValueOrder(order, ct))
///     .When(o => o.IsInternational)
///         .RouteTo(async (order, ct) => await ProcessInternationalOrder(order, ct))
///     .Default(async (order, ct) => await ProcessStandardOrder(order, ct))
///     .Build();
/// </code>
/// </example>
public static class ContentRouterBuilder
{
    /// <summary>
    /// Creates a new content router builder for messages with a result type.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to route.</typeparam>
    /// <typeparam name="TResult">The type of result from handlers.</typeparam>
    /// <returns>A new content router builder.</returns>
    public static ContentRouterBuilder<TMessage, TResult> Create<TMessage, TResult>()
        where TMessage : class
        => new();

    /// <summary>
    /// Creates a new content router builder for messages without a result type.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to route.</typeparam>
    /// <returns>A new content router builder.</returns>
    public static ContentRouterBuilder<TMessage, Unit> Create<TMessage>()
        where TMessage : class
        => new();
}

/// <summary>
/// Fluent builder for defining content-based routing rules.
/// </summary>
/// <typeparam name="TMessage">The type of message to route.</typeparam>
/// <typeparam name="TResult">The type of result from handlers.</typeparam>
public sealed class ContentRouterBuilder<TMessage, TResult>
    where TMessage : class
{
    private readonly List<RouteDefinition<TMessage, TResult>> _routes = [];
    private RouteDefinition<TMessage, TResult>? _defaultRoute;
    private int _routeCounter;

    internal ContentRouterBuilder()
    {
    }

    /// <summary>
    /// Adds a conditional route to the router.
    /// </summary>
    /// <param name="condition">The condition that determines if this route matches.</param>
    /// <returns>A route builder for configuring the handler.</returns>
    public ContentRouteBuilder<TMessage, TResult> When(Func<TMessage, bool> condition)
    {
        ArgumentNullException.ThrowIfNull(condition);
        return new ContentRouteBuilder<TMessage, TResult>(this, condition);
    }

    /// <summary>
    /// Adds a named conditional route to the router.
    /// </summary>
    /// <param name="name">The name of the route (for logging/debugging).</param>
    /// <param name="condition">The condition that determines if this route matches.</param>
    /// <returns>A route builder for configuring the handler.</returns>
    public ContentRouteBuilder<TMessage, TResult> When(string name, Func<TMessage, bool> condition)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(condition);
        return new ContentRouteBuilder<TMessage, TResult>(this, condition, name);
    }

    /// <summary>
    /// Sets the default route to use when no other routes match.
    /// </summary>
    /// <param name="handler">The handler to execute.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public ContentRouterBuilder<TMessage, TResult> Default(
        Func<TMessage, CancellationToken, ValueTask<Either<EncinaError, TResult>>> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _defaultRoute = new RouteDefinition<TMessage, TResult>(
            "Default",
            _ => true,
            handler,
            priority: int.MaxValue,
            isDefault: true);

        return this;
    }

    /// <summary>
    /// Sets the default route to use when no other routes match (synchronous handler).
    /// </summary>
    /// <param name="handler">The handler to execute.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public ContentRouterBuilder<TMessage, TResult> Default(
        Func<TMessage, Either<EncinaError, TResult>> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        return Default((message, _) => ValueTask.FromResult(handler(message)));
    }

    /// <summary>
    /// Sets a default result to return when no routes match.
    /// </summary>
    /// <param name="defaultResult">The default result.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public ContentRouterBuilder<TMessage, TResult> DefaultResult(TResult defaultResult)
    {
        return Default((_, _) => ValueTask.FromResult(Right<EncinaError, TResult>(defaultResult))); // NOSONAR S6966: LanguageExt Right is a pure function
    }

    /// <summary>
    /// Builds the content router definition.
    /// </summary>
    /// <returns>An immutable content router definition.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no routes have been configured.</exception>
    public BuiltContentRouterDefinition<TMessage, TResult> Build()
    {
        if (_routes.Count == 0 && _defaultRoute is null)
        {
            throw new InvalidOperationException(
                "At least one route or a default route must be configured.");
        }

        // Sort routes by priority
        var sortedRoutes = _routes
            .OrderBy(r => r.Priority)
            .ToList();

        return new BuiltContentRouterDefinition<TMessage, TResult>(
            sortedRoutes,
            _defaultRoute);
    }

    internal void AddRoute(RouteDefinition<TMessage, TResult> route)
    {
        _routes.Add(route);
    }

    internal string GenerateRouteName() => $"Route_{++_routeCounter}";
}

/// <summary>
/// Builder for configuring a single route.
/// </summary>
/// <typeparam name="TMessage">The type of message to route.</typeparam>
/// <typeparam name="TResult">The type of result from handlers.</typeparam>
public sealed class ContentRouteBuilder<TMessage, TResult>
    where TMessage : class
{
    private readonly ContentRouterBuilder<TMessage, TResult> _parent;
    private readonly Func<TMessage, bool> _condition;
    private readonly string? _name;
    private int _priority;
    private Dictionary<string, object>? _metadata;

    internal ContentRouteBuilder(
        ContentRouterBuilder<TMessage, TResult> parent,
        Func<TMessage, bool> condition,
        string? name = null)
    {
        _parent = parent;
        _condition = condition;
        _name = name;
    }

    /// <summary>
    /// Sets the priority for this route (lower values = higher priority).
    /// </summary>
    /// <param name="priority">The priority value.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public ContentRouteBuilder<TMessage, TResult> WithPriority(int priority)
    {
        _priority = priority;
        return this;
    }

    /// <summary>
    /// Adds metadata to this route.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public ContentRouteBuilder<TMessage, TResult> WithMetadata(string key, object value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        _metadata ??= [];
        _metadata[key] = value;
        return this;
    }

    /// <summary>
    /// Configures the handler for this route.
    /// </summary>
    /// <param name="handler">The async handler to execute.</param>
    /// <returns>The parent builder for fluent chaining.</returns>
    public ContentRouterBuilder<TMessage, TResult> RouteTo(
        Func<TMessage, CancellationToken, ValueTask<Either<EncinaError, TResult>>> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var route = new RouteDefinition<TMessage, TResult>(
            _name ?? _parent.GenerateRouteName(),
            _condition,
            handler,
            _priority,
            isDefault: false,
            _metadata);

        _parent.AddRoute(route);
        return _parent;
    }

    /// <summary>
    /// Configures the handler for this route (synchronous version).
    /// </summary>
    /// <param name="handler">The synchronous handler to execute.</param>
    /// <returns>The parent builder for fluent chaining.</returns>
    public ContentRouterBuilder<TMessage, TResult> RouteTo(
        Func<TMessage, Either<EncinaError, TResult>> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        return RouteTo((message, _) => ValueTask.FromResult(handler(message)));
    }

    /// <summary>
    /// Configures the handler for this route (async without Either).
    /// </summary>
    /// <param name="handler">The async handler to execute.</param>
    /// <returns>The parent builder for fluent chaining.</returns>
    public ContentRouterBuilder<TMessage, TResult> RouteTo(
        Func<TMessage, CancellationToken, ValueTask<TResult>> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        return RouteTo(async (message, ct) =>
        {
            var result = await handler(message, ct).ConfigureAwait(false);
            return Right<EncinaError, TResult>(result); // NOSONAR S6966: LanguageExt Right is a pure function
        });
    }

    /// <summary>
    /// Configures the handler for this route (synchronous without Either).
    /// </summary>
    /// <param name="handler">The synchronous handler to execute.</param>
    /// <returns>The parent builder for fluent chaining.</returns>
    public ContentRouterBuilder<TMessage, TResult> RouteTo(Func<TMessage, TResult> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        return RouteTo((message, _) =>
            ValueTask.FromResult(Right<EncinaError, TResult>(handler(message)))); // NOSONAR S6966: LanguageExt Right is a pure function
    }
}

/// <summary>
/// Represents a built, immutable content router definition.
/// </summary>
/// <typeparam name="TMessage">The type of message to route.</typeparam>
/// <typeparam name="TResult">The type of result from handlers.</typeparam>
public sealed class BuiltContentRouterDefinition<TMessage, TResult>
    where TMessage : class
{
    /// <summary>
    /// Gets the configured routes, sorted by priority.
    /// </summary>
    public IReadOnlyList<RouteDefinition<TMessage, TResult>> Routes { get; }

    /// <summary>
    /// Gets the default route, if configured.
    /// </summary>
    public RouteDefinition<TMessage, TResult>? DefaultRoute { get; }

    /// <summary>
    /// Gets the total number of routes (excluding default).
    /// </summary>
    public int RouteCount => Routes.Count;

    /// <summary>
    /// Gets a value indicating whether a default route is configured.
    /// </summary>
    public bool HasDefaultRoute => DefaultRoute is not null;

    internal BuiltContentRouterDefinition(
        IReadOnlyList<RouteDefinition<TMessage, TResult>> routes,
        RouteDefinition<TMessage, TResult>? defaultRoute)
    {
        Routes = routes;
        DefaultRoute = defaultRoute;
    }
}
