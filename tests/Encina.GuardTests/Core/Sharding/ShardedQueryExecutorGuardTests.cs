using Encina.Sharding;
using Encina.Sharding.Configuration;
using Encina.Sharding.Execution;

namespace Encina.GuardTests.Core.Sharding;

/// <summary>
/// Guard clause tests for <see cref="ShardedQueryExecutor"/>.
/// Verifies null parameter handling for the constructor and ExecuteAsync method.
/// </summary>
public sealed class ShardedQueryExecutorGuardTests
{
    private static readonly string[] SingleShardIds = ["shard-0"];
    private static readonly string[] TwoShardIds = ["shard-0", "shard-1"];

    private readonly ShardTopology _topology = CreateTestTopology();
    private readonly ScatterGatherOptions _options = new();
    private readonly ILogger<ShardedQueryExecutor> _logger = NullLogger<ShardedQueryExecutor>.Instance;

    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws when topology is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTopology_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ShardedQueryExecutor(null!, _options, _logger));
    }

    /// <summary>
    /// Verifies that the constructor throws when options is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ShardedQueryExecutor(_topology, null!, _logger));
    }

    /// <summary>
    /// Verifies that the constructor throws when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ShardedQueryExecutor(_topology, _options, null!));
    }

    /// <summary>
    /// Verifies that the constructor succeeds with valid parameters.
    /// </summary>
    [Fact]
    public void Constructor_ValidParameters_Succeeds()
    {
        var executor = new ShardedQueryExecutor(_topology, _options, _logger);
        executor.ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies that the constructor accepts optional null metrics parameter.
    /// </summary>
    [Fact]
    public void Constructor_NullMetrics_Succeeds()
    {
        var executor = new ShardedQueryExecutor(_topology, _options, _logger, metrics: null);
        executor.ShouldNotBeNull();
    }

    #endregion

    #region ExecuteAsync Guards

    /// <summary>
    /// Verifies that ExecuteAsync throws when shardIds is null.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_NullShardIds_ThrowsArgumentNullException()
    {
        var executor = CreateExecutor();

        await Should.ThrowAsync<ArgumentNullException>(() =>
            executor.ExecuteAsync<string>(
                null!,
                (shardId, ct) => Task.FromResult(
                    Either<EncinaError, IReadOnlyList<string>>.Right(
                        (IReadOnlyList<string>)new List<string>()))));
    }

    /// <summary>
    /// Verifies that ExecuteAsync throws when query factory is null.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_NullQueryFactory_ThrowsArgumentNullException()
    {
        var executor = CreateExecutor();

        await Should.ThrowAsync<ArgumentNullException>(() =>
            executor.ExecuteAsync<string>(
                SingleShardIds,
                null!));
    }

    /// <summary>
    /// Verifies that ExecuteAsync returns empty result for empty shard list.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_EmptyShardIds_ReturnsEmptyResult()
    {
        var executor = CreateExecutor();

        var result = await executor.ExecuteAsync<string>(
            Array.Empty<string>(),
            (shardId, ct) => Task.FromResult(
                Either<EncinaError, IReadOnlyList<string>>.Right(
                    (IReadOnlyList<string>)new List<string>())));

        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r => r.Results.Count,
            Left: _ => -1).ShouldBe(0);
    }

    /// <summary>
    /// Verifies that ExecuteAsync aggregates results from multiple shards.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_MultipleShards_AggregatesResults()
    {
        var executor = CreateExecutor();

        var result = await executor.ExecuteAsync<string>(
            TwoShardIds,
            (shardId, ct) => Task.FromResult(
                Either<EncinaError, IReadOnlyList<string>>.Right(
                    (IReadOnlyList<string>)new List<string> { $"result-from-{shardId}" })));

        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r => r.Results.Count,
            Left: _ => -1).ShouldBe(2);
    }

    /// <summary>
    /// Verifies that ExecuteAsync tracks failures when partial results are allowed.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_PartialFailure_AllowPartialResults_ReturnsPartialResult()
    {
        var options = new ScatterGatherOptions { AllowPartialResults = true };
        var executor = new ShardedQueryExecutor(_topology, options, _logger);

        var result = await executor.ExecuteAsync<string>(
            TwoShardIds,
            (shardId, ct) =>
            {
                if (shardId == "shard-1")
                {
                    return Task.FromResult(
                        Either<EncinaError, IReadOnlyList<string>>.Left(
                            EncinaErrors.Create("test.error", "Simulated failure")));
                }

                return Task.FromResult(
                    Either<EncinaError, IReadOnlyList<string>>.Right(
                        (IReadOnlyList<string>)new List<string> { "ok" }));
            });

        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r => r.FailedShards.Count,
            Left: _ => -1).ShouldBe(1);
    }

    /// <summary>
    /// Verifies that ExecuteAsync returns error when partial results are not allowed and a shard fails.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_PartialFailure_NoPartialResults_ReturnsLeftError()
    {
        var options = new ScatterGatherOptions { AllowPartialResults = false };
        var executor = new ShardedQueryExecutor(_topology, options, _logger);

        var result = await executor.ExecuteAsync<string>(
            TwoShardIds,
            (shardId, ct) =>
            {
                if (shardId == "shard-1")
                {
                    return Task.FromResult(
                        Either<EncinaError, IReadOnlyList<string>>.Left(
                            EncinaErrors.Create("test.error", "Simulated failure")));
                }

                return Task.FromResult(
                    Either<EncinaError, IReadOnlyList<string>>.Right(
                        (IReadOnlyList<string>)new List<string> { "ok" }));
            });

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region Test Helpers

    private ShardedQueryExecutor CreateExecutor() =>
        new(_topology, _options, _logger);

    private static ShardTopology CreateTestTopology() =>
        new(new[]
        {
            new ShardInfo("shard-0", "Server=s0;Database=db;"),
            new ShardInfo("shard-1", "Server=s1;Database=db;"),
        });

    #endregion
}
