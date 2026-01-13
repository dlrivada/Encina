using LanguageExt;

namespace Encina.Messaging.ScatterGather;

/// <summary>
/// Options for a specific scatter-gather execution.
/// </summary>
/// <param name="Strategy">The gather strategy.</param>
/// <param name="Timeout">Optional timeout for the operation.</param>
/// <param name="QuorumCount">Optional quorum count for <see cref="GatherStrategy.WaitForQuorum"/>.</param>
/// <param name="ExecuteInParallel">Whether to execute scatter handlers in parallel.</param>
/// <param name="MaxDegreeOfParallelism">Optional maximum degree of parallelism.</param>
/// <param name="Metadata">Optional metadata for the operation.</param>
public sealed record ScatterGatherExecutionOptions(
    GatherStrategy Strategy,
    TimeSpan? Timeout = null,
    int? QuorumCount = null,
    bool ExecuteInParallel = true,
    int? MaxDegreeOfParallelism = null,
    IReadOnlyDictionary<string, object>? Metadata = null);

/// <summary>
/// Represents an immutable, fully-configured scatter-gather definition ready for execution.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response from scatter handlers.</typeparam>
public sealed class BuiltScatterGatherDefinition<TRequest, TResponse>
    where TRequest : class
{
    /// <summary>
    /// Gets the name of this scatter-gather operation.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the scatter handler definitions.
    /// </summary>
    public IReadOnlyList<ScatterDefinition<TRequest, TResponse>> ScatterHandlers { get; }

    /// <summary>
    /// Gets the gather handler function.
    /// </summary>
    public Func<IReadOnlyList<ScatterExecutionResult<TResponse>>, CancellationToken, ValueTask<Either<EncinaError, TResponse>>> GatherHandler { get; }

    /// <summary>
    /// Gets the gather strategy.
    /// </summary>
    public GatherStrategy Strategy { get; }

    /// <summary>
    /// Gets the timeout for the operation.
    /// </summary>
    public TimeSpan? Timeout { get; }

    /// <summary>
    /// Gets the quorum count for <see cref="GatherStrategy.WaitForQuorum"/>.
    /// </summary>
    public int? QuorumCount { get; }

    /// <summary>
    /// Gets whether to execute scatter handlers in parallel.
    /// </summary>
    public bool ExecuteInParallel { get; }

    /// <summary>
    /// Gets the maximum degree of parallelism.
    /// </summary>
    public int? MaxDegreeOfParallelism { get; }

    /// <summary>
    /// Gets optional metadata for the operation.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; }

    /// <summary>
    /// Gets the number of scatter handlers.
    /// </summary>
    public int ScatterCount => ScatterHandlers.Count;

    /// <summary>
    /// Initializes a new instance of the <see cref="BuiltScatterGatherDefinition{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="name">The name of the scatter-gather operation.</param>
    /// <param name="scatterHandlers">The scatter handler definitions.</param>
    /// <param name="gatherHandler">The gather handler function.</param>
    /// <param name="options">The execution options.</param>
    public BuiltScatterGatherDefinition(
        string name,
        IReadOnlyList<ScatterDefinition<TRequest, TResponse>> scatterHandlers,
        Func<IReadOnlyList<ScatterExecutionResult<TResponse>>, CancellationToken, ValueTask<Either<EncinaError, TResponse>>> gatherHandler,
        ScatterGatherExecutionOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(scatterHandlers);
        ArgumentNullException.ThrowIfNull(gatherHandler);
        ArgumentNullException.ThrowIfNull(options);

        if (scatterHandlers.Count == 0)
        {
            throw new ArgumentException("At least one scatter handler is required.", nameof(scatterHandlers));
        }

        if (options.Strategy == GatherStrategy.WaitForQuorum && options.QuorumCount.HasValue && options.QuorumCount.Value > scatterHandlers.Count)
        {
            throw new ArgumentException(
                $"Quorum count ({options.QuorumCount.Value}) cannot exceed scatter handler count ({scatterHandlers.Count}).",
                nameof(options));
        }

        Name = name;
        ScatterHandlers = scatterHandlers;
        GatherHandler = gatherHandler;
        Strategy = options.Strategy;
        Timeout = options.Timeout;
        QuorumCount = options.QuorumCount;
        ExecuteInParallel = options.ExecuteInParallel;
        MaxDegreeOfParallelism = options.MaxDegreeOfParallelism;
        Metadata = options.Metadata;
    }

    /// <summary>
    /// Gets the effective quorum count, calculating a default if not specified.
    /// </summary>
    /// <returns>The quorum count to use.</returns>
    public int GetEffectiveQuorumCount()
    {
        if (QuorumCount.HasValue)
        {
            return QuorumCount.Value;
        }

        // Default: majority (half + 1)
        return (ScatterCount / 2) + 1;
    }
}
