using System.Diagnostics;
using Encina.Sharding.Configuration;
using Encina.Sharding.Diagnostics;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Sharding.Execution;

/// <summary>
/// Executes scatter-gather queries across multiple shards with parallelism control,
/// timeout management, and partial failure handling.
/// </summary>
/// <remarks>
/// <para>
/// This executor runs a query factory against multiple shards in parallel using
/// <see cref="SemaphoreSlim"/> for parallelism control (following the same pattern
/// as <c>ScatterGatherRunner</c> in Encina.Messaging).
/// </para>
/// <para>
/// Results from all successful shards are aggregated into a single
/// <see cref="ShardedQueryResult{T}"/>. Failed shards are tracked with their
/// error information in <see cref="ShardFailure"/> records.
/// </para>
/// <para>
/// Behavior is controlled by <see cref="ScatterGatherOptions"/>:
/// <list type="bullet">
///   <item><description><b>MaxParallelism</b>: Controls how many shards are queried simultaneously.</description></item>
///   <item><description><b>Timeout</b>: Maximum time for the entire scatter-gather operation.</description></item>
///   <item><description><b>AllowPartialResults</b>: When false, any shard failure fails the entire operation.</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var executor = new ShardedQueryExecutor(topology, scatterGatherOptions, logger);
///
/// var result = await executor.ExecuteAsync(
///     topology.ActiveShardIds,
///     async (shardId, ct) =>
///     {
///         var repo = GetRepositoryForShard(shardId);
///         return await repo.ListAsync(new ActiveOrdersSpec(), ct);
///     },
///     cancellationToken);
/// </code>
/// </example>
#pragma warning disable CA1848 // Use the LoggerMessage delegates
#pragma warning disable CA1873 // Logging parameter evaluation
public sealed class ShardedQueryExecutor : IShardedQueryExecutor
{
    private readonly ShardTopology _topology;
    private readonly ScatterGatherOptions _options;
    private readonly ILogger<ShardedQueryExecutor> _logger;
    private readonly ShardRoutingMetrics? _metrics;

    /// <summary>
    /// Initializes a new instance of <see cref="ShardedQueryExecutor"/>.
    /// </summary>
    /// <param name="topology">The shard topology.</param>
    /// <param name="options">The scatter-gather options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="metrics">Optional sharding metrics for OpenTelemetry integration.</param>
    public ShardedQueryExecutor(
        ShardTopology topology,
        ScatterGatherOptions options,
        ILogger<ShardedQueryExecutor> logger,
        ShardRoutingMetrics? metrics = null)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _topology = topology;
        _options = options;
        _logger = logger;
        _metrics = metrics;
    }

    /// <summary>
    /// Executes a query across the specified shards and aggregates the results.
    /// </summary>
    /// <typeparam name="T">The type of the result items.</typeparam>
    /// <param name="shardIds">The shard IDs to query.</param>
    /// <param name="queryFactory">
    /// A factory that creates a query task for each shard. Receives the shard ID
    /// and a cancellation token, returns the results from that shard.
    /// </param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with a <see cref="ShardedQueryResult{T}"/> containing aggregated results;
    /// Left with an error if the operation fails entirely.
    /// </returns>
    public async Task<Either<EncinaError, ShardedQueryResult<T>>> ExecuteAsync<T>(
        IEnumerable<string> shardIds,
        Func<string, CancellationToken, Task<Either<EncinaError, IReadOnlyList<T>>>> queryFactory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(shardIds);
        ArgumentNullException.ThrowIfNull(queryFactory);

        var shardIdList = shardIds as IReadOnlyList<string> ?? shardIds.ToList();

        if (shardIdList.Count == 0)
        {
            return Either<EncinaError, ShardedQueryResult<T>>.Right(
                new ShardedQueryResult<T>([], [], []));
        }

        _logger.LogDebug(
            "Starting scatter-gather query across {ShardCount} shards with timeout {Timeout}",
            shardIdList.Count,
            _options.Timeout);

        // Start tracing activity for the scatter-gather operation
        var scatterActivity = ShardingActivitySource.StartScatterGather(shardIdList.Count, "targeted");
        var scatterStart = Stopwatch.GetTimestamp();

        // Create a linked cancellation token with the configured timeout
        using var timeoutCts = new CancellationTokenSource(_options.Timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutCts.Token);
        var linkedToken = linkedCts.Token;

        var maxParallelism = _options.MaxParallelism <= 0
            ? shardIdList.Count
            : Math.Min(_options.MaxParallelism, shardIdList.Count);

        using var semaphore = new SemaphoreSlim(maxParallelism, maxParallelism);

        var tasks = shardIdList.Select(shardId =>
            ExecuteOnShardAsync(shardId, queryFactory, semaphore, linkedToken));

        ShardQueryTaskResult<T>[] results;

        try
        {
            results = await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Scatter-gather query timed out after {Timeout}", _options.Timeout);

            ShardingActivitySource.CompleteScatterGather(scatterActivity, 0, shardIdList.Count, 0);

            return Either<EncinaError, ShardedQueryResult<T>>.Left(
                EncinaErrors.Create(
                    ShardingErrorCodes.ScatterGatherTimeout,
                    $"Scatter-gather query timed out after {_options.Timeout}."));
        }

        // Aggregate results
        var allResults = new List<T>();
        var successfulShards = new List<string>();
        var failedShards = new List<ShardFailure>();

        foreach (var result in results)
        {
            if (result.IsSuccess)
            {
                allResults.AddRange(result.Items);
                successfulShards.Add(result.ShardId);
            }
            else
            {
                failedShards.Add(new ShardFailure(result.ShardId, result.Error));
            }
        }

        // Record scatter-gather metrics
        var scatterElapsed = Stopwatch.GetElapsedTime(scatterStart);
        _metrics?.RecordScatterGatherDuration(shardIdList.Count, allResults.Count, scatterElapsed.TotalMilliseconds);

        if (failedShards.Count > 0)
        {
            _metrics?.RecordPartialFailure(failedShards.Count, shardIdList.Count);
        }

        ShardingActivitySource.CompleteScatterGather(
            scatterActivity, successfulShards.Count, failedShards.Count, allResults.Count);

        // If partial results are not allowed and any shard failed, return error
        if (!_options.AllowPartialResults && failedShards.Count > 0)
        {
            _logger.LogError(
                "Scatter-gather failed: {FailedCount}/{TotalCount} shards failed and AllowPartialResults is false",
                failedShards.Count,
                shardIdList.Count);

            return Either<EncinaError, ShardedQueryResult<T>>.Left(
                EncinaErrors.Create(
                    ShardingErrorCodes.ScatterGatherPartialFailure,
                    $"{failedShards.Count} of {shardIdList.Count} shards failed. " +
                    $"Failed shards: {string.Join(", ", failedShards.Select(f => f.ShardId))}."));
        }

        _logger.LogDebug(
            "Scatter-gather completed: {SuccessCount} succeeded, {FailedCount} failed, {ResultCount} total results",
            successfulShards.Count,
            failedShards.Count,
            allResults.Count);

        return Either<EncinaError, ShardedQueryResult<T>>.Right(
            new ShardedQueryResult<T>(allResults, successfulShards, failedShards));
    }

    /// <summary>
    /// Executes a query across all active shards and aggregates the results.
    /// </summary>
    /// <typeparam name="T">The type of the result items.</typeparam>
    /// <param name="queryFactory">
    /// A factory that creates a query task for each shard.
    /// </param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with a <see cref="ShardedQueryResult{T}"/> containing aggregated results;
    /// Left with an error if the operation fails entirely.
    /// </returns>
    public Task<Either<EncinaError, ShardedQueryResult<T>>> ExecuteAllAsync<T>(
        Func<string, CancellationToken, Task<Either<EncinaError, IReadOnlyList<T>>>> queryFactory,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            _topology.ActiveShardIds,
            queryFactory,
            cancellationToken);
    }

    private async Task<ShardQueryTaskResult<T>> ExecuteOnShardAsync<T>(
        string shardId,
        Func<string, CancellationToken, Task<Either<EncinaError, IReadOnlyList<T>>>> queryFactory,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);

        _metrics?.IncrementActiveQueries();
        var shardActivity = ShardingActivitySource.StartShardQuery(shardId);
        var shardStart = Stopwatch.GetTimestamp();

        try
        {
            _logger.LogTrace("Executing query on shard {ShardId}", shardId);

            var result = await queryFactory(shardId, cancellationToken);

            var shardElapsed = Stopwatch.GetElapsedTime(shardStart);
            _metrics?.RecordShardQueryDuration(shardId, shardElapsed.TotalMilliseconds);

            return result.Match(
                Right: items =>
                {
                    ShardingActivitySource.CompleteShardQuery(shardActivity, isSuccess: true);
                    return ShardQueryTaskResult<T>.Success(shardId, items);
                },
                Left: error =>
                {
                    _logger.LogWarning(
                        "Query on shard {ShardId} returned error: {ErrorMessage}",
                        shardId,
                        error.Message);
                    ShardingActivitySource.CompleteShardQuery(shardActivity, isSuccess: false, error.Message);
                    return ShardQueryTaskResult<T>.Failure(shardId, error);
                });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            ShardingActivitySource.CompleteShardQuery(shardActivity, isSuccess: false, "Operation cancelled");
            throw; // Propagate cancellation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Query on shard {ShardId} threw an exception", shardId);
            ShardingActivitySource.CompleteShardQuery(shardActivity, isSuccess: false, ex.Message);

            return ShardQueryTaskResult<T>.Failure(
                shardId,
                EncinaErrors.Create(
                    ShardingErrorCodes.ShardNotFound,
                    $"Query on shard '{shardId}' failed: {ex.Message}",
                    ex));
        }
        finally
        {
            _metrics?.DecrementActiveQueries();
            semaphore.Release();
        }
    }

    /// <summary>
    /// Internal result type to track per-shard query results before aggregation.
    /// </summary>
    private sealed class ShardQueryTaskResult<T>
    {
        public required string ShardId { get; init; }
        public bool IsSuccess { get; init; }
        public IReadOnlyList<T> Items { get; init; } = [];
        public EncinaError Error { get; init; }

        public static ShardQueryTaskResult<T> Success(string shardId, IReadOnlyList<T> items)
            => new() { ShardId = shardId, IsSuccess = true, Items = items };

        public static ShardQueryTaskResult<T> Failure(string shardId, EncinaError error)
            => new() { ShardId = shardId, IsSuccess = false, Error = error };
    }
}
#pragma warning restore CA1873
#pragma warning restore CA1848
