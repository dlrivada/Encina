using System.Diagnostics;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.Messaging.ScatterGather;

/// <summary>
/// Executes scatter-gather operations.
/// </summary>
public sealed class ScatterGatherRunner : IScatterGatherRunner
{
    private readonly ScatterGatherOptions _options;
    private readonly ILogger<ScatterGatherRunner> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScatterGatherRunner"/> class.
    /// </summary>
    /// <param name="options">The scatter-gather options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">Optional time provider for testing.</param>
    public ScatterGatherRunner(
        ScatterGatherOptions options,
        ILogger<ScatterGatherRunner> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, ScatterGatherResult<TResponse>>> ExecuteAsync<TRequest, TResponse>(
        BuiltScatterGatherDefinition<TRequest, TResponse> definition,
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : class
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(request);

        var operationId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();

        ScatterGatherLog.ExecutionStarted(_logger, definition.Name, operationId, definition.ScatterCount, definition.Strategy);

        try
        {
            // Apply timeout
            var timeout = definition.Timeout ?? _options.DefaultTimeout;
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            // Execute scatter phase
            var scatterResult = await ExecuteScatterPhaseAsync(
                definition,
                request,
                operationId,
                linkedCts).ConfigureAwait(false);

            if (scatterResult.IsLeft)
            {
                return scatterResult.Match(
                    Right: _ => throw new InvalidOperationException("Unexpected right value"),
                    Left: error => Left<EncinaError, ScatterGatherResult<TResponse>>(error));
            }

            var scatterResults = scatterResult.Match(
                Right: r => r,
                Left: _ => throw new InvalidOperationException("Unexpected left value"));

            // Execute gather phase
            var gatherResult = await ExecuteGatherPhaseAsync(
                definition,
                scatterResults,
                operationId,
                linkedCts.Token).ConfigureAwait(false);

            stopwatch.Stop();

            return gatherResult.Match(
                Right: response =>
                {
                    var result = new ScatterGatherResult<TResponse>(
                        operationId,
                        response,
                        scatterResults,
                        definition.Strategy,
                        stopwatch.Elapsed,
                        scatterResults.Count(r => r.Result.IsLeft && IsCancelled(r)),
                        _timeProvider.GetUtcNow().UtcDateTime);

                    ScatterGatherLog.ExecutionCompleted(
                        _logger,
                        definition.Name,
                        operationId,
                        stopwatch.Elapsed,
                        result.SuccessCount,
                        definition.ScatterCount);

                    return Right<EncinaError, ScatterGatherResult<TResponse>>(result);
                },
                Left: error => Left<EncinaError, ScatterGatherResult<TResponse>>(error));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            ScatterGatherLog.ExecutionCancelled(_logger, operationId);
            return Left<EncinaError, ScatterGatherResult<TResponse>>(
                EncinaErrors.Create(ScatterGatherErrorCodes.Cancelled, "The scatter-gather operation was cancelled."));
        }
        catch (OperationCanceledException)
        {
            // Timeout
            var timeout = definition.Timeout ?? _options.DefaultTimeout;
            ScatterGatherLog.ExecutionTimedOut(_logger, operationId, timeout);
            return Left<EncinaError, ScatterGatherResult<TResponse>>(
                EncinaErrors.Create(ScatterGatherErrorCodes.Timeout, $"The scatter-gather operation timed out after {timeout}."));
        }
        catch (Exception ex)
        {
            ScatterGatherLog.ExecutionException(_logger, operationId, ex.Message, ex);
            return Left<EncinaError, ScatterGatherResult<TResponse>>(
                EncinaErrors.Create(ScatterGatherErrorCodes.HandlerFailed, ex.Message));
        }
    }

    private async ValueTask<Either<EncinaError, IReadOnlyList<ScatterExecutionResult<TResponse>>>> ExecuteScatterPhaseAsync<TRequest, TResponse>(
        BuiltScatterGatherDefinition<TRequest, TResponse> definition,
        TRequest request,
        Guid operationId,
        CancellationTokenSource linkedCts)
        where TRequest : class
    {
        var executeInParallel = definition.ExecuteInParallel;
        var maxParallelism = definition.MaxDegreeOfParallelism ?? _options.MaxDegreeOfParallelism;

        return definition.Strategy switch
        {
            GatherStrategy.WaitForFirst => await ExecuteWaitForFirstAsync(
                definition, request, operationId, linkedCts).ConfigureAwait(false),
            GatherStrategy.WaitForQuorum => await ExecuteWaitForQuorumAsync(
                definition, request, operationId, linkedCts).ConfigureAwait(false),
            _ when executeInParallel => await ExecuteParallelScattersAsync(
                definition, request, operationId, maxParallelism, linkedCts.Token).ConfigureAwait(false),
            _ => await ExecuteSequentialScattersAsync(
                definition, request, operationId, linkedCts.Token).ConfigureAwait(false)
        };
    }

    private async ValueTask<Either<EncinaError, IReadOnlyList<ScatterExecutionResult<TResponse>>>> ExecuteParallelScattersAsync<TRequest, TResponse>(
        BuiltScatterGatherDefinition<TRequest, TResponse> definition,
        TRequest request,
        Guid operationId,
        int maxParallelism,
        CancellationToken cancellationToken)
        where TRequest : class
    {
        var results = new List<ScatterExecutionResult<TResponse>>(definition.ScatterCount);
        var semaphore = new SemaphoreSlim(maxParallelism);

        var tasks = definition.ScatterHandlers.Select(async handler =>
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await ExecuteScatterHandlerAsync(handler, request, operationId, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                semaphore.Release();
            }
        });

        var completedResults = await Task.WhenAll(tasks).ConfigureAwait(false);
        results.AddRange(completedResults);

        return ValidateScatterResults(results, definition.Strategy, operationId);
    }

    private async ValueTask<Either<EncinaError, IReadOnlyList<ScatterExecutionResult<TResponse>>>> ExecuteSequentialScattersAsync<TRequest, TResponse>(
        BuiltScatterGatherDefinition<TRequest, TResponse> definition,
        TRequest request,
        Guid operationId,
        CancellationToken cancellationToken)
        where TRequest : class
    {
        var results = new List<ScatterExecutionResult<TResponse>>(definition.ScatterCount);

        foreach (var handler in definition.ScatterHandlers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await ExecuteScatterHandlerAsync(handler, request, operationId, cancellationToken).ConfigureAwait(false);
            results.Add(result);
        }

        return ValidateScatterResults(results, definition.Strategy, operationId);
    }

    private async ValueTask<Either<EncinaError, IReadOnlyList<ScatterExecutionResult<TResponse>>>> ExecuteWaitForFirstAsync<TRequest, TResponse>(
        BuiltScatterGatherDefinition<TRequest, TResponse> definition,
        TRequest request,
        Guid operationId,
        CancellationTokenSource linkedCts)
        where TRequest : class
    {
        var results = new List<ScatterExecutionResult<TResponse>>();
        var tcs = new TaskCompletionSource<ScatterExecutionResult<TResponse>>();

        var tasks = definition.ScatterHandlers.Select(async handler =>
        {
            try
            {
                var result = await ExecuteScatterHandlerAsync(handler, request, operationId, linkedCts.Token).ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    ScatterGatherLog.FirstResultReceived(_logger, operationId, handler.Name);
                    tcs.TrySetResult(result);
                    if (_options.CancelRemainingOnStrategyComplete)
                    {
                        await linkedCts.CancelAsync().ConfigureAwait(false);
                    }
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                return CreateCancelledResult<TResponse>(handler.Name);
            }
        }).ToList();

        // Wait for first successful or all to complete
        var firstSuccessTask = tcs.Task;
        var allTask = Task.WhenAll(tasks);

        var completedTask = await Task.WhenAny(firstSuccessTask, allTask).ConfigureAwait(false);

        if (completedTask == firstSuccessTask)
        {
            results.Add(await firstSuccessTask.ConfigureAwait(false));
            // Wait for remaining to finish (they should be cancelled)
            try
            {
                var remainingResults = await allTask.ConfigureAwait(false);
                results.AddRange(remainingResults.Where(r => r != results[0]));
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }
        else
        {
            results.AddRange(await allTask.ConfigureAwait(false));
        }

        return ValidateScatterResults(results, definition.Strategy, operationId);
    }

    private async ValueTask<Either<EncinaError, IReadOnlyList<ScatterExecutionResult<TResponse>>>> ExecuteWaitForQuorumAsync<TRequest, TResponse>(
        BuiltScatterGatherDefinition<TRequest, TResponse> definition,
        TRequest request,
        Guid operationId,
        CancellationTokenSource linkedCts)
        where TRequest : class
    {
        var quorumCount = definition.GetEffectiveQuorumCount();
        var tracker = new QuorumTracker(quorumCount);

        var tasks = CreateQuorumHandlerTasks(definition, request, operationId, linkedCts, tracker);
        var results = await CollectQuorumResults(tasks).ConfigureAwait(false);

        return ValidateQuorumReached(results, quorumCount, operationId);
    }

    private List<Task<ScatterExecutionResult<TResponse>>> CreateQuorumHandlerTasks<TRequest, TResponse>(
        BuiltScatterGatherDefinition<TRequest, TResponse> definition,
        TRequest request,
        Guid operationId,
        CancellationTokenSource linkedCts,
        QuorumTracker tracker)
        where TRequest : class
    {
        return definition.ScatterHandlers.Select(handler =>
            ExecuteQuorumHandler(handler, request, operationId, linkedCts, tracker, definition.ScatterCount)).ToList();
    }

    private async Task<ScatterExecutionResult<TResponse>> ExecuteQuorumHandler<TRequest, TResponse>(
        ScatterDefinition<TRequest, TResponse> handler,
        TRequest request,
        Guid operationId,
        CancellationTokenSource linkedCts,
        QuorumTracker tracker,
        int totalCount)
        where TRequest : class
    {
        try
        {
            var result = await ExecuteScatterHandlerAsync(handler, request, operationId, linkedCts.Token).ConfigureAwait(false);

            if (result.IsSuccess && tracker.RecordSuccess())
            {
                ScatterGatherLog.QuorumReached(_logger, operationId, tracker.SuccessCount, totalCount);
                if (_options.CancelRemainingOnStrategyComplete)
                {
                    await linkedCts.CancelAsync().ConfigureAwait(false);
                }
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            return CreateCancelledResult<TResponse>(handler.Name);
        }
    }

    private static async ValueTask<List<ScatterExecutionResult<TResponse>>> CollectQuorumResults<TResponse>(
        List<Task<ScatterExecutionResult<TResponse>>> tasks)
    {
        var results = new List<ScatterExecutionResult<TResponse>>();

        try
        {
            var completedResults = await Task.WhenAll(tasks).ConfigureAwait(false);
            results.AddRange(completedResults);
        }
        catch (OperationCanceledException)
        {
            foreach (var task in tasks)
            {
                if (task.IsCompletedSuccessfully)
                {
                    results.Add(task.Result);
                }
            }
        }

        return results;
    }

    private Either<EncinaError, IReadOnlyList<ScatterExecutionResult<TResponse>>> ValidateQuorumReached<TResponse>(
        List<ScatterExecutionResult<TResponse>> results,
        int quorumCount,
        Guid operationId)
    {
        var finalSuccessCount = results.Count(r => r.IsSuccess);

        if (finalSuccessCount < quorumCount)
        {
            ScatterGatherLog.QuorumNotReached(_logger, operationId, finalSuccessCount, quorumCount);
            return Left<EncinaError, IReadOnlyList<ScatterExecutionResult<TResponse>>>(
                EncinaErrors.Create(
                    ScatterGatherErrorCodes.QuorumNotReached,
                    $"Quorum not reached. Required {quorumCount}, got {finalSuccessCount}."));
        }

        return Right<EncinaError, IReadOnlyList<ScatterExecutionResult<TResponse>>>(results);
    }

    private sealed class QuorumTracker(int quorumCount)
    {
        private int _successCount;
        private readonly object _lockObj = new();

        public int SuccessCount => _successCount;

        public bool RecordSuccess()
        {
            lock (_lockObj)
            {
                _successCount++;
                return _successCount == quorumCount;
            }
        }
    }

    private async ValueTask<ScatterExecutionResult<TResponse>> ExecuteScatterHandlerAsync<TRequest, TResponse>(
        ScatterDefinition<TRequest, TResponse> handler,
        TRequest request,
        Guid operationId,
        CancellationToken cancellationToken)
        where TRequest : class
    {
        ScatterGatherLog.ScatterExecuting(_logger, operationId, handler.Name);

        var startedAt = _timeProvider.GetUtcNow().UtcDateTime;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await handler.Handler(request, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            result.Match(
                Right: _ => ScatterGatherLog.ScatterCompleted(_logger, operationId, handler.Name, stopwatch.Elapsed),
                Left: error => ScatterGatherLog.ScatterFailed(_logger, operationId, handler.Name, error.Message));

            return new ScatterExecutionResult<TResponse>(
                handler.Name,
                result,
                stopwatch.Elapsed,
                startedAt,
                _timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            ScatterGatherLog.ScatterCancelled(_logger, operationId, handler.Name);

            return ScatterExecutionResult.Failure<TResponse>(
                handler.Name,
                EncinaErrors.Create(ScatterGatherErrorCodes.Cancelled, "Handler was cancelled."),
                stopwatch.Elapsed,
                startedAt,
                _timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var error = EncinaErrors.Create(ScatterGatherErrorCodes.ScatterFailed, ex.Message);
            ScatterGatherLog.ScatterFailed(_logger, operationId, handler.Name, ex.Message);

            return ScatterExecutionResult.Failure<TResponse>(
                handler.Name,
                error,
                stopwatch.Elapsed,
                startedAt,
                _timeProvider.GetUtcNow().UtcDateTime);
        }
    }

    private async ValueTask<Either<EncinaError, TResponse>> ExecuteGatherPhaseAsync<TRequest, TResponse>(
        BuiltScatterGatherDefinition<TRequest, TResponse> definition,
        IReadOnlyList<ScatterExecutionResult<TResponse>> scatterResults,
        Guid operationId,
        CancellationToken cancellationToken)
        where TRequest : class
    {
        var resultsToGather = _options.IncludeFailedResultsInGather
            ? scatterResults
            : scatterResults.Where(r => r.IsSuccess).ToList();

        ScatterGatherLog.GatherExecuting(_logger, operationId, resultsToGather.Count);

        try
        {
            var result = await definition.GatherHandler(scatterResults, cancellationToken).ConfigureAwait(false);

            result.Match(
                Right: _ => ScatterGatherLog.GatherCompleted(_logger, operationId),
                Left: error => ScatterGatherLog.GatherFailed(_logger, operationId, error.Message));

            return result;
        }
        catch (OperationCanceledException)
        {
            return Left<EncinaError, TResponse>(
                EncinaErrors.Create(ScatterGatherErrorCodes.Cancelled, "Gather handler was cancelled."));
        }
        catch (Exception ex)
        {
            var error = EncinaErrors.Create(ScatterGatherErrorCodes.GatherFailed, ex.Message);
            ScatterGatherLog.GatherFailed(_logger, operationId, ex.Message);
            return Left<EncinaError, TResponse>(error);
        }
    }

    private Either<EncinaError, IReadOnlyList<ScatterExecutionResult<TResponse>>> ValidateScatterResults<TResponse>(
        IReadOnlyList<ScatterExecutionResult<TResponse>> results,
        GatherStrategy strategy,
        Guid operationId)
    {
        var successCount = results.Count(r => r.IsSuccess);

        // For WaitForAll, all must succeed
        if (strategy == GatherStrategy.WaitForAll && successCount < results.Count)
        {
            ScatterGatherLog.AllScattersFailed(_logger, operationId);
            return Left<EncinaError, IReadOnlyList<ScatterExecutionResult<TResponse>>>(
                EncinaErrors.Create(
                    ScatterGatherErrorCodes.ScatterFailed,
                    $"Not all scatter handlers succeeded. {successCount}/{results.Count} succeeded."));
        }

        // For other strategies (except WaitForAllAllowPartial), at least one must succeed
        if (strategy != GatherStrategy.WaitForAllAllowPartial && successCount == 0)
        {
            ScatterGatherLog.AllScattersFailed(_logger, operationId);
            return Left<EncinaError, IReadOnlyList<ScatterExecutionResult<TResponse>>>(
                EncinaErrors.Create(ScatterGatherErrorCodes.AllScattersFailed, "All scatter handlers failed."));
        }

        return Right<EncinaError, IReadOnlyList<ScatterExecutionResult<TResponse>>>(results);
    }

    private ScatterExecutionResult<TResponse> CreateCancelledResult<TResponse>(string handlerName)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        return ScatterExecutionResult.Failure<TResponse>(
            handlerName,
            EncinaErrors.Create(ScatterGatherErrorCodes.Cancelled, "Handler was cancelled."),
            TimeSpan.Zero,
            now,
            now);
    }

    private static bool IsCancelled<TResponse>(ScatterExecutionResult<TResponse> result)
    {
        return result.Result.Match(
            Right: _ => false,
            Left: error => error.GetCode().Match(
                Some: code => code == ScatterGatherErrorCodes.Cancelled,
                None: () => false));
    }
}
