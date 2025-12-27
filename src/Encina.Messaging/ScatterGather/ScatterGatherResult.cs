namespace Encina.Messaging.ScatterGather;

/// <summary>
/// Represents the result of a scatter-gather operation.
/// </summary>
/// <typeparam name="TResponse">The type of the final aggregated response.</typeparam>
public sealed class ScatterGatherResult<TResponse>
{
    /// <summary>
    /// Gets the unique identifier for this scatter-gather operation.
    /// </summary>
    public Guid OperationId { get; }

    /// <summary>
    /// Gets the final aggregated response from the gather handler.
    /// </summary>
    public TResponse Response { get; }

    /// <summary>
    /// Gets the results from all scatter handler executions.
    /// </summary>
    public IReadOnlyList<ScatterExecutionResult<TResponse>> ScatterResults { get; }

    /// <summary>
    /// Gets the gather strategy that was used.
    /// </summary>
    public GatherStrategy Strategy { get; }

    /// <summary>
    /// Gets the total duration of the scatter-gather operation.
    /// </summary>
    public TimeSpan TotalDuration { get; }

    /// <summary>
    /// Gets the total number of scatter handlers that were executed.
    /// </summary>
    public int ScatterCount { get; }

    /// <summary>
    /// Gets the number of scatter handlers that completed successfully.
    /// </summary>
    public int SuccessCount { get; }

    /// <summary>
    /// Gets the number of scatter handlers that failed.
    /// </summary>
    public int FailureCount { get; }

    /// <summary>
    /// Gets the number of scatter handlers that were cancelled.
    /// </summary>
    public int CancelledCount { get; }

    /// <summary>
    /// Gets the UTC timestamp when the operation completed.
    /// </summary>
    public DateTime CompletedAtUtc { get; }

    /// <summary>
    /// Gets whether all scatter handlers completed successfully.
    /// </summary>
    public bool AllSucceeded => FailureCount == 0 && CancelledCount == 0;

    /// <summary>
    /// Gets whether any scatter handlers failed.
    /// </summary>
    public bool HasPartialFailures => FailureCount > 0;

    /// <summary>
    /// Gets the successful responses from scatter handlers.
    /// </summary>
    public IEnumerable<TResponse> SuccessfulResponses =>
        ScatterResults.Where(r => r.IsSuccess).Select(r => r.Result.Match(r => r, _ => default!));

    /// <summary>
    /// Gets the errors from failed scatter handlers.
    /// </summary>
    public IEnumerable<EncinaError> Errors =>
        ScatterResults.Where(r => r.IsFailure).Select(r => r.Result.Match(_ => default!, e => e));

    /// <summary>
    /// Initializes a new instance of the <see cref="ScatterGatherResult{TResponse}"/> class.
    /// </summary>
    public ScatterGatherResult(
        Guid operationId,
        TResponse response,
        IReadOnlyList<ScatterExecutionResult<TResponse>> scatterResults,
        GatherStrategy strategy,
        TimeSpan totalDuration,
        int cancelledCount,
        DateTime completedAtUtc)
    {
        OperationId = operationId;
        Response = response;
        ScatterResults = scatterResults;
        Strategy = strategy;
        TotalDuration = totalDuration;
        ScatterCount = scatterResults.Count;
        SuccessCount = scatterResults.Count(r => r.IsSuccess);
        FailureCount = scatterResults.Count(r => r.IsFailure);
        CancelledCount = cancelledCount;
        CompletedAtUtc = completedAtUtc;
    }
}
