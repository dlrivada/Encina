using Encina.Sharding;
using Encina.Sharding.Configuration;
using Encina.Sharding.Diagnostics;
using Encina.Sharding.Execution;
using Encina.UnitTests.Support;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.UnitTests.Core.Sharding.Execution;

/// <summary>
/// Unit tests for <see cref="ShardedQueryExecutor"/>.
/// </summary>
public sealed class ShardedQueryExecutorTests
{
    private readonly ILogger<ShardedQueryExecutor> _logger = NullLogger<ShardedQueryExecutor>.Instance;

    private static ShardTopology CreateTopology(params string[] shardIds)
    {
        var shards = shardIds.Select(id => new ShardInfo(id, $"conn-{id}")).ToList();
        return new ShardTopology(shards);
    }

    private static ScatterGatherOptions DefaultOptions() => new()
    {
        MaxParallelism = -1,
        Timeout = TimeSpan.FromSeconds(30),
        AllowPartialResults = true
    };

    // ────────────────────────────────────────────────────────────
    //  Constructor
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullTopology_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ShardedQueryExecutor(null!, DefaultOptions(), _logger));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ShardedQueryExecutor(CreateTopology("shard-1"), null!, _logger));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ShardedQueryExecutor(CreateTopology("shard-1"), DefaultOptions(), null!));
    }

    // ────────────────────────────────────────────────────────────
    //  ExecuteAsync — basic
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_EmptyShardIds_ReturnsEmptyResult()
    {
        // Arrange
        var executor = new ShardedQueryExecutor(CreateTopology("shard-1"), DefaultOptions(), _logger);

        // Act
        var result = await executor.ExecuteAsync<string>(
            Array.Empty<string>(),
            (_, _) => Task.FromResult(Either<EncinaError, IReadOnlyList<string>>.Right((IReadOnlyList<string>)["item"])),
            CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(qr =>
        {
            qr.Results.Count.ShouldBe(0);
            qr.SuccessfulShards.Count.ShouldBe(0);
            qr.FailedShards.Count.ShouldBe(0);
            qr.IsComplete.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task ExecuteAsync_AllShardsSucceed_ReturnsCompleteResult()
    {
        // Arrange
        var topology = CreateTopology("shard-1", "shard-2");
        var executor = new ShardedQueryExecutor(topology, DefaultOptions(), _logger);

        // Act
        var result = await executor.ExecuteAsync<string>(
            ["shard-1", "shard-2"],
            (shardId, _) => Task.FromResult(
                Either<EncinaError, IReadOnlyList<string>>.Right(
                    (IReadOnlyList<string>)[$"item-{shardId}"])),
            CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(qr =>
        {
            qr.Results.Count.ShouldBe(2);
            qr.SuccessfulShards.Count.ShouldBe(2);
            qr.FailedShards.Count.ShouldBe(0);
            qr.IsComplete.ShouldBeTrue();
            qr.IsPartial.ShouldBeFalse();
        });
    }

    [Fact]
    public async Task ExecuteAsync_SomeShardsFail_AllowPartialResults_ReturnsPartialResult()
    {
        // Arrange
        var topology = CreateTopology("shard-1", "shard-2");
        var options = DefaultOptions();
        options.AllowPartialResults = true;
        var executor = new ShardedQueryExecutor(topology, options, _logger);
        var error = EncinaErrors.Create(ShardingErrorCodes.ShardNotFound, "Not found");

        // Act
        var result = await executor.ExecuteAsync<string>(
            ["shard-1", "shard-2"],
            (shardId, _) => shardId == "shard-1"
                ? Task.FromResult(Either<EncinaError, IReadOnlyList<string>>.Right((IReadOnlyList<string>)["item1"]))
                : Task.FromResult(Either<EncinaError, IReadOnlyList<string>>.Left(error)),
            CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(qr =>
        {
            qr.Results.Count.ShouldBe(1);
            qr.SuccessfulShards.Count.ShouldBe(1);
            qr.FailedShards.Count.ShouldBe(1);
            qr.IsComplete.ShouldBeFalse();
            qr.IsPartial.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task ExecuteAsync_SomeShardsFail_DisallowPartialResults_ReturnsLeftError()
    {
        // Arrange
        var topology = CreateTopology("shard-1", "shard-2");
        var options = DefaultOptions();
        options.AllowPartialResults = false;
        var executor = new ShardedQueryExecutor(topology, options, _logger);
        var error = EncinaErrors.Create(ShardingErrorCodes.ShardNotFound, "Not found");

        // Act
        var result = await executor.ExecuteAsync<string>(
            ["shard-1", "shard-2"],
            (shardId, _) => shardId == "shard-1"
                ? Task.FromResult(Either<EncinaError, IReadOnlyList<string>>.Right((IReadOnlyList<string>)["item1"]))
                : Task.FromResult(Either<EncinaError, IReadOnlyList<string>>.Left(error)),
            CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_ShardThrowsException_CapturedAsFailure()
    {
        // Arrange
        var topology = CreateTopology("shard-1", "shard-2");
        var options = DefaultOptions();
        options.AllowPartialResults = true;
        var executor = new ShardedQueryExecutor(topology, options, _logger);

        // Act
        var result = await executor.ExecuteAsync<string>(
            ["shard-1", "shard-2"],
            (shardId, _) => shardId == "shard-1"
                ? Task.FromResult(Either<EncinaError, IReadOnlyList<string>>.Right((IReadOnlyList<string>)["item1"]))
                : throw new InvalidOperationException("Shard error"),
            CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(qr =>
        {
            qr.SuccessfulShards.Count.ShouldBe(1);
            qr.FailedShards.Count.ShouldBe(1);
            qr.FailedShards[0].ShardId.ShouldBe("shard-2");
        });
    }

    [Fact]
    public async Task ExecuteAsync_NullShardIds_ThrowsArgumentNullException()
    {
        // Arrange
        var executor = new ShardedQueryExecutor(CreateTopology("shard-1"), DefaultOptions(), _logger);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            executor.ExecuteAsync<string>(
                null!,
                (_, _) => Task.FromResult(Either<EncinaError, IReadOnlyList<string>>.Right((IReadOnlyList<string>)[])),
                CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_NullQueryFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var executor = new ShardedQueryExecutor(CreateTopology("shard-1"), DefaultOptions(), _logger);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            executor.ExecuteAsync<string>(
                ["shard-1"],
                null!,
                CancellationToken.None));
    }

    // ────────────────────────────────────────────────────────────
    //  ExecuteAsync — parallelism control
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_MaxParallelism_LimitsConcurrency()
    {
        // Arrange
        var topology = CreateTopology("shard-1", "shard-2", "shard-3");
        var options = DefaultOptions();
        options.MaxParallelism = 1; // Sequential execution
        var executor = new ShardedQueryExecutor(topology, options, _logger);

        var concurrentCount = 0;
        var maxConcurrent = 0;
        var lockObj = new object();

        // Act
        var result = await executor.ExecuteAsync<string>(
            ["shard-1", "shard-2", "shard-3"],
            async (shardId, ct) =>
            {
                lock (lockObj)
                {
                    concurrentCount++;
                    maxConcurrent = Math.Max(maxConcurrent, concurrentCount);
                }

                await Task.Delay(50, ct);

                lock (lockObj)
                {
                    concurrentCount--;
                }

                return Either<EncinaError, IReadOnlyList<string>>.Right(
                    (IReadOnlyList<string>)[$"item-{shardId}"]);
            },
            CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        maxConcurrent.ShouldBe(1);
    }

    // ────────────────────────────────────────────────────────────
    //  ExecuteAsync — timeout
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_Timeout_ReturnsLeftError()
    {
        // Arrange
        var topology = CreateTopology("shard-1");
        var options = DefaultOptions();
        options.Timeout = TimeSpan.FromMilliseconds(50);
        var executor = new ShardedQueryExecutor(topology, options, _logger);

        // Act
        var result = await executor.ExecuteAsync<string>(
            ["shard-1"],
            async (_, ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
                return Either<EncinaError, IReadOnlyList<string>>.Right(
                    (IReadOnlyList<string>)["item"]);
            },
            CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // ────────────────────────────────────────────────────────────
    //  ExecuteAsync — cancellation
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_Cancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var topology = CreateTopology("shard-1");
        var executor = new ShardedQueryExecutor(topology, DefaultOptions(), _logger);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            executor.ExecuteAsync<string>(
                ["shard-1"],
                async (_, ct) =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                    return Either<EncinaError, IReadOnlyList<string>>.Right(
                        (IReadOnlyList<string>)["item"]);
                },
                cts.Token));
    }

    // ────────────────────────────────────────────────────────────
    //  ExecuteAllAsync
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAllAsync_QueriesAllActiveShards()
    {
        // Arrange
        var shards = new[]
        {
            new ShardInfo("shard-1", "conn1"),
            new ShardInfo("shard-2", "conn2"),
            new ShardInfo("shard-3", "conn3", IsActive: false)
        };
        var topology = new ShardTopology(shards);
        var executor = new ShardedQueryExecutor(topology, DefaultOptions(), _logger);
        var queriedShards = new List<string>();

        // Act
        var result = await executor.ExecuteAllAsync<string>(
            (shardId, _) =>
            {
                lock (queriedShards) { queriedShards.Add(shardId); }
                return Task.FromResult(
                    Either<EncinaError, IReadOnlyList<string>>.Right(
                        (IReadOnlyList<string>)[$"item-{shardId}"]));
            },
            CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        queriedShards.Count.ShouldBe(2);
        queriedShards.ShouldContain("shard-1");
        queriedShards.ShouldContain("shard-2");
        queriedShards.ShouldNotContain("shard-3");
    }

    // ────────────────────────────────────────────────────────────
    //  ExecuteAsync — aggregation
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_AggregatesResultsFromMultipleShards()
    {
        // Arrange
        var topology = CreateTopology("shard-1", "shard-2");
        var executor = new ShardedQueryExecutor(topology, DefaultOptions(), _logger);

        // Act
        var result = await executor.ExecuteAsync<int>(
            ["shard-1", "shard-2"],
            (shardId, _) =>
            {
                IReadOnlyList<int> items = shardId == "shard-1" ? [1, 2, 3] : [4, 5];
                return Task.FromResult(Either<EncinaError, IReadOnlyList<int>>.Right(items));
            },
            CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(qr =>
        {
            qr.Results.Count.ShouldBe(5);
            qr.TotalShardsQueried.ShouldBe(2);
        });
    }

    // ────────────────────────────────────────────────────────────
    //  ExecuteAsync — characterization (#4235)
    // ────────────────────────────────────────────────────────────

    private static Task<Either<EncinaError, IReadOnlyList<string>>> Items(params string[] items)
        => Task.FromResult(Either<EncinaError, IReadOnlyList<string>>.Right(items));

    private static Task<Either<EncinaError, IReadOnlyList<string>>> Failed(string code)
        => Task.FromResult(Either<EncinaError, IReadOnlyList<string>>.Left(EncinaErrors.Create(code, "failed")));

    private static EncinaError LeftOf<T>(Either<EncinaError, T> result)
    {
        result.IsLeft.ShouldBeTrue();
        return result.Match(Right: _ => default, Left: e => e);
    }

    private static T RightOf<T>(Either<EncinaError, T> result)
    {
        result.IsRight.ShouldBeTrue();
        return result.Match(Right: r => r, Left: _ => default!);
    }

    [Fact]
    public async Task ExecuteAsync_Timeout_ReturnsScatterGatherTimeoutWithTheConfiguredTimeout()
    {
        var options = DefaultOptions();
        options.Timeout = TimeSpan.FromMilliseconds(50);
        var executor = new ShardedQueryExecutor(CreateTopology("shard-1"), options, _logger);

        var result = await executor.ExecuteAsync<string>(
            ["shard-1"],
            async (_, ct) =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                return Either<EncinaError, IReadOnlyList<string>>.Right((IReadOnlyList<string>)[]);
            });

        var error = LeftOf(result);
        error.GetEncinaCode().ShouldBe(ShardingErrorCodes.ScatterGatherTimeout);
        error.Message.ShouldBe($"Scatter-gather query timed out after {TimeSpan.FromMilliseconds(50)}.");
    }

    [Fact]
    public async Task ExecuteAsync_Timeout_CompletesScatterActivityWithEveryShardFailed()
    {
        var options = DefaultOptions();
        options.Timeout = TimeSpan.FromMilliseconds(50);
        var executor = new ShardedQueryExecutor(CreateTopology("shard-1", "shard-2"), options, _logger);
        using var capture = new ActivityCapture("Encina.Sharding");

        await executor.ExecuteAsync<string>(
            ["shard-1", "shard-2"],
            async (_, ct) =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                return Either<EncinaError, IReadOnlyList<string>>.Right((IReadOnlyList<string>)[]);
            });

        var scatter = capture.Stopped("Encina.Sharding.ScatterGather").ShouldHaveSingleItem();
        scatter.GetTagItem("encina.sharding.scatter.success_count").ShouldBe(0);
        scatter.GetTagItem("encina.sharding.scatter.failed_count").ShouldBe(2);
        scatter.GetTagItem("encina.sharding.scatter.result_count").ShouldBe(0);
    }

    [Fact]
    public async Task ExecuteAsync_CallerCancelsWhileRunning_ThrowsInsteadOfReportingTimeout()
    {
        var executor = new ShardedQueryExecutor(CreateTopology("shard-1"), DefaultOptions(), _logger);
        using var cts = new CancellationTokenSource();

        await Should.ThrowAsync<OperationCanceledException>(() => executor.ExecuteAsync<string>(
            ["shard-1"],
            async (_, ct) =>
            {
                await cts.CancelAsync();
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                return Either<EncinaError, IReadOnlyList<string>>.Right((IReadOnlyList<string>)[]);
            },
            cts.Token));
    }

    [Fact]
    public async Task ExecuteAsync_TimeoutAndCallerCancellationBothSignalled_ThrowsInsteadOfReportingTimeout()
    {
        var options = DefaultOptions();
        options.Timeout = TimeSpan.FromMilliseconds(50);
        var executor = new ShardedQueryExecutor(CreateTopology("shard-1"), options, _logger);
        using var cts = new CancellationTokenSource();

        await Should.ThrowAsync<OperationCanceledException>(() => executor.ExecuteAsync<string>(
            ["shard-1"],
            async (_, ct) =>
            {
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                }
                catch (OperationCanceledException)
                {
                    // The timeout has fired; the caller cancels too before the cancellation surfaces.
                    await cts.CancelAsync();
                    throw;
                }

                return Either<EncinaError, IReadOnlyList<string>>.Right((IReadOnlyList<string>)[]);
            },
            cts.Token));
    }

    [Fact]
    public async Task ExecuteAsync_PartialResultsNotAllowed_ReturnsPartialFailureListingFailedShardsInOrder()
    {
        var options = DefaultOptions();
        options.AllowPartialResults = false;
        var executor = new ShardedQueryExecutor(CreateTopology("s-a", "s-b", "s-c"), options, _logger);

        var result = await executor.ExecuteAsync<string>(
            ["s-c", "s-a", "s-b"],
            (shardId, _) => shardId == "s-a" ? Items("a") : Failed("test.failed"));

        var error = LeftOf(result);
        error.GetEncinaCode().ShouldBe(ShardingErrorCodes.ScatterGatherPartialFailure);
        error.Message.ShouldBe("2 of 3 shards failed. Failed shards: s-c, s-b.");
    }

    [Fact]
    public async Task ExecuteAsync_ResultsAndShardListsFollowTheRequestedShardOrder()
    {
        var executor = new ShardedQueryExecutor(CreateTopology("s-a", "s-b", "s-c", "s-d"), DefaultOptions(), _logger);

        var result = await executor.ExecuteAsync<string>(
            ["s-d", "s-b", "s-a", "s-c"],
            (shardId, _) => shardId == "s-b" ? Failed("test.failed") : Items($"{shardId}-1", $"{shardId}-2"));

        var queryResult = RightOf(result);
        string.Join(",", queryResult.Results).ShouldBe("s-d-1,s-d-2,s-a-1,s-a-2,s-c-1,s-c-2");
        string.Join(",", queryResult.SuccessfulShards).ShouldBe("s-d,s-a,s-c");
        var failure = queryResult.FailedShards.ShouldHaveSingleItem();
        failure.ShardId.ShouldBe("s-b");
        failure.Error.GetEncinaCode().ShouldBe("test.failed");
    }

    [Fact]
    public async Task ExecuteAsync_ShardThrows_RecordsShardNotFoundWithTheExceptionMessage()
    {
        var executor = new ShardedQueryExecutor(CreateTopology("shard-1"), DefaultOptions(), _logger);

        var result = await executor.ExecuteAsync<string>(
            ["shard-1"],
            (_, _) => throw new InvalidOperationException("Shard error"));

        var failure = RightOf(result).FailedShards.ShouldHaveSingleItem();
        failure.Error.GetEncinaCode().ShouldBe(ShardingErrorCodes.ShardNotFound);
        failure.Error.Message.ShouldBe("Query on shard 'shard-1' failed: Shard error");
    }

    [Fact]
    public async Task ExecuteAsync_LazyShardIds_AreEnumeratedOnce()
    {
        var executor = new ShardedQueryExecutor(CreateTopology("s-a", "s-b"), DefaultOptions(), _logger);
        var enumerations = 0;

        IEnumerable<string> ShardIds()
        {
            enumerations++;
            yield return "s-a";
            yield return "s-b";
        }

        var result = await executor.ExecuteAsync<string>(ShardIds(), (shardId, _) => Items(shardId));

        RightOf(result).SuccessfulShards.Count.ShouldBe(2);
        enumerations.ShouldBe(1);
    }

    [Fact]
    public async Task ExecuteAsync_MaxParallelismAboveShardCount_QueriesEveryShard()
    {
        var options = DefaultOptions();
        options.MaxParallelism = 64;
        var executor = new ShardedQueryExecutor(CreateTopology("s-a", "s-b"), options, _logger);

        var result = await executor.ExecuteAsync<string>(["s-a", "s-b"], (shardId, _) => Items(shardId));

        RightOf(result).SuccessfulShards.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ExecuteAsync_PartialFailure_CompletesScatterActivityWithCounts()
    {
        var executor = new ShardedQueryExecutor(CreateTopology("s-a", "s-b", "s-c"), DefaultOptions(), _logger);
        using var capture = new ActivityCapture("Encina.Sharding");

        await executor.ExecuteAsync<string>(
            ["s-a", "s-b", "s-c"],
            (shardId, _) => shardId == "s-b" ? Failed("test.failed") : Items("x", "y"));

        var scatter = capture.Stopped("Encina.Sharding.ScatterGather").ShouldHaveSingleItem();
        scatter.GetTagItem("encina.sharding.scatter.success_count").ShouldBe(2);
        scatter.GetTagItem("encina.sharding.scatter.failed_count").ShouldBe(1);
        scatter.GetTagItem("encina.sharding.scatter.result_count").ShouldBe(4);
        capture.Stopped("Encina.Sharding.ShardQuery").Count.ShouldBe(3);
    }

    [Fact]
    public async Task ExecuteAsync_WithMetrics_RecordsDurationAndPartialFailure()
    {
        var topology = CreateTopology("m-a", "m-b", "m-c", "m-d", "m-e");
        var executor = new ShardedQueryExecutor(topology, DefaultOptions(), _logger, new ShardRoutingMetrics(topology));
        var durations = new System.Collections.Concurrent.ConcurrentBag<KeyValuePair<string, object?>[]>();
        var partialFailures = new System.Collections.Concurrent.ConcurrentBag<KeyValuePair<string, object?>[]>();
        using var listener = new System.Diagnostics.Metrics.MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == "Encina" &&
                instrument.Name is "encina.sharding.scatter.duration_ms" or "encina.sharding.scatter.partial_failures")
            {
                l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<double>((_, _, tags, _) => durations.Add(tags.ToArray()));
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) => partialFailures.Add(tags.ToArray()));
        listener.Start();

        await executor.ExecuteAsync<string>(
            ["m-a", "m-b", "m-c", "m-d", "m-e"],
            (shardId, _) => shardId is "m-b" or "m-d" ? Failed("test.failed") : Items("1", "2", "3", "4", "5", "6", "7"));

        durations.ShouldContain(tags => tags.Any(t => t.Key == "result.count" && Equals(t.Value, 21)));
        partialFailures.ShouldContain(tags =>
            tags.Any(t => t.Key == "failed.count" && Equals(t.Value, 2)) &&
            tags.Any(t => t.Key == "total.count" && Equals(t.Value, 5)));
    }
}
