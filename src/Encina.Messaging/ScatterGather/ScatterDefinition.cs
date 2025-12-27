using LanguageExt;

namespace Encina.Messaging.ScatterGather;

/// <summary>
/// Represents a scatter handler definition.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
public sealed class ScatterDefinition<TRequest, TResponse>
    where TRequest : class
{
    /// <summary>
    /// Gets the name of the scatter handler.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the handler function.
    /// </summary>
    public Func<TRequest, CancellationToken, ValueTask<Either<EncinaError, TResponse>>> Handler { get; }

    /// <summary>
    /// Gets the priority of this scatter handler (lower values execute first in sequential mode).
    /// </summary>
    public int Priority { get; }

    /// <summary>
    /// Gets optional metadata associated with this scatter handler.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScatterDefinition{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="name">The name of the scatter handler.</param>
    /// <param name="handler">The handler function.</param>
    /// <param name="priority">The priority (default: 0).</param>
    /// <param name="metadata">Optional metadata.</param>
    public ScatterDefinition(
        string name,
        Func<TRequest, CancellationToken, ValueTask<Either<EncinaError, TResponse>>> handler,
        int priority = 0,
        IReadOnlyDictionary<string, object>? metadata = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(handler);

        Name = name;
        Handler = handler;
        Priority = priority;
        Metadata = metadata;
    }
}
