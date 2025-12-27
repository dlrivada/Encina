using LanguageExt;

namespace Encina.Messaging.ContentRouter;

/// <summary>
/// Represents a single routing rule in a content-based router.
/// </summary>
/// <typeparam name="TMessage">The type of message being routed.</typeparam>
/// <typeparam name="TResult">The type of result from handler execution.</typeparam>
public sealed class RouteDefinition<TMessage, TResult>
    where TMessage : class
{
    /// <summary>
    /// Gets the name of this route (for logging and debugging).
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the condition that determines if this route matches a message.
    /// </summary>
    public Func<TMessage, bool> Condition { get; }

    /// <summary>
    /// Gets the handler to execute when this route matches.
    /// </summary>
    public Func<TMessage, CancellationToken, ValueTask<Either<EncinaError, TResult>>> Handler { get; }

    /// <summary>
    /// Gets the priority of this route (lower values = higher priority).
    /// </summary>
    public int Priority { get; }

    /// <summary>
    /// Gets a value indicating whether this is the default route.
    /// </summary>
    public bool IsDefault { get; }

    /// <summary>
    /// Gets optional metadata associated with this route.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RouteDefinition{TMessage, TResult}"/> class.
    /// </summary>
    /// <param name="name">The route name.</param>
    /// <param name="condition">The matching condition.</param>
    /// <param name="handler">The route handler.</param>
    /// <param name="priority">The route priority.</param>
    /// <param name="isDefault">Whether this is the default route.</param>
    /// <param name="metadata">Optional metadata.</param>
    public RouteDefinition(
        string name,
        Func<TMessage, bool> condition,
        Func<TMessage, CancellationToken, ValueTask<Either<EncinaError, TResult>>> handler,
        int priority = 0,
        bool isDefault = false,
        IReadOnlyDictionary<string, object>? metadata = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(handler);

        Name = name;
        Condition = condition;
        Handler = handler;
        Priority = priority;
        IsDefault = isDefault;
        Metadata = metadata;
    }

    /// <summary>
    /// Evaluates whether this route matches the given message.
    /// </summary>
    /// <param name="message">The message to evaluate.</param>
    /// <returns>True if this route matches the message; otherwise, false.</returns>
    public bool Matches(TMessage message) => Condition(message);
}
