using Encina.Sharding;
using Encina.Sharding.Configuration;

using Shouldly;

namespace Encina.GuardTests.Database.Sharding;

/// <summary>
/// Guard clause tests for <see cref="ShardingOptions{TEntity}"/>.
/// </summary>
public sealed class ShardingOptionsGuardTests
{
    [Fact]
    public void AddShard_NullShardId_ThrowsArgumentNullException()
    {
        var options = new ShardingOptions<TestEntity>();
        var ex = Should.Throw<ArgumentNullException>(() => options.AddShard(null!, "conn"));
        ex.ParamName.ShouldBe("shardId");
    }

    [Fact]
    public void AddShard_NullConnectionString_ThrowsArgumentNullException()
    {
        var options = new ShardingOptions<TestEntity>();
        var ex = Should.Throw<ArgumentNullException>(() => options.AddShard("shard-1", null!));
        ex.ParamName.ShouldBe("connectionString");
    }

    private sealed class TestEntity : IShardable
    {
        public string Key { get; set; } = default!;
        public string GetShardKey() => Key;
    }
}
