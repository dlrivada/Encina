using Encina.Sharding;
using Encina.Sharding.Configuration;
using Encina.Sharding.Execution;

using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

namespace Encina.GuardTests.Database.Sharding;

/// <summary>
/// Guard clause tests for <see cref="ShardedQueryExecutor"/>.
/// </summary>
public sealed class ShardedQueryExecutorGuardTests
{
    private static ShardTopology CreateTopology() =>
        new([new ShardInfo("shard-1", "conn-1")]);

    [Fact]
    public void Constructor_NullTopology_ThrowsArgumentNullException()
    {
        var options = new ScatterGatherOptions();
        var logger = NullLogger<ShardedQueryExecutor>.Instance;
        var ex = Should.Throw<ArgumentNullException>(() => new ShardedQueryExecutor(null!, options, logger));
        ex.ParamName.ShouldBe("topology");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var logger = NullLogger<ShardedQueryExecutor>.Instance;
        var ex = Should.Throw<ArgumentNullException>(() => new ShardedQueryExecutor(CreateTopology(), null!, logger));
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var options = new ScatterGatherOptions();
        var ex = Should.Throw<ArgumentNullException>(() => new ShardedQueryExecutor(CreateTopology(), options, null!));
        ex.ParamName.ShouldBe("logger");
    }
}
