using Encina.Sharding;
using Encina.Sharding.Configuration;
using Encina.Sharding.Execution;
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
}
