using LanguageExt;

namespace Encina.Messaging.ScatterGather;

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
    public BuiltScatterGatherDefinition(
        string name,
        IReadOnlyList<ScatterDefinition<TRequest, TResponse>> scatterHandlers,
        Func<IReadOnlyList<ScatterExecutionResult<TResponse>>, CancellationToken, ValueTask<Either<EncinaError, TResponse>>> gatherHandler,
        GatherStrategy strategy,
        TimeSpan? timeout,
        int? quorumCount,
        bool executeInParallel,
        int? maxDegreeOfParallelism,
        IReadOnlyDictionary<string, object>? metadata)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(scatterHandlers);
        ArgumentNullException.ThrowIfNull(gatherHandler);

        if (scatterHandlers.Count == 0)
        {
            throw new ArgumentException("At least one scatter handler is required.", nameof(scatterHandlers));
        }

        if (strategy == GatherStrategy.WaitForQuorum && quorumCount.HasValue && quorumCount.Value > scatterHandlers.Count)
        {
            throw new ArgumentException(
                $"Quorum count ({quorumCount.Value}) cannot exceed scatter handler count ({scatterHandlers.Count}).",
                nameof(quorumCount));
        }

        Name = name;
        ScatterHandlers = scatterHandlers;
        GatherHandler = gatherHandler;
        Strategy = strategy;
        Timeout = timeout;
        QuorumCount = quorumCount;
        ExecuteInParallel = executeInParallel;
        MaxDegreeOfParallelism = maxDegreeOfParallelism;
        Metadata = metadata;
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
