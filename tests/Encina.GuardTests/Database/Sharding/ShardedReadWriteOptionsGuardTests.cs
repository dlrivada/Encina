using Encina.Sharding;
using Shouldly;

namespace Encina.GuardTests.Database.Sharding;

/// <summary>
/// Guard clause tests for <see cref="ShardedReadWriteOptions"/>.
/// </summary>
public sealed class ShardedReadWriteOptionsGuardTests
{
    [Fact]
    public void AddShard_NullShardId_ThrowsArgumentException()
    {
        var options = new ShardedReadWriteOptions();
        var ex = Should.Throw<ArgumentException>(() =>
            options.AddShard(null!, "conn"));
        ex.ParamName.ShouldBe("shardId");
    }

    [Fact]
    public void AddShard_EmptyShardId_ThrowsArgumentException()
    {
        var options = new ShardedReadWriteOptions();
        Should.Throw<ArgumentException>(() =>
            options.AddShard("", "conn"));
    }

    [Fact]
    public void AddShard_WhitespaceShardId_ThrowsArgumentException()
    {
        var options = new ShardedReadWriteOptions();
        Should.Throw<ArgumentException>(() =>
            options.AddShard("   ", "conn"));
    }

    [Fact]
    public void AddShard_NullPrimaryConnectionString_ThrowsArgumentException()
    {
        var options = new ShardedReadWriteOptions();
        var ex = Should.Throw<ArgumentException>(() =>
            options.AddShard("shard-0", null!));
        ex.ParamName.ShouldBe("primaryConnectionString");
    }

    [Fact]
    public void AddShard_EmptyPrimaryConnectionString_ThrowsArgumentException()
    {
        var options = new ShardedReadWriteOptions();
        Should.Throw<ArgumentException>(() =>
            options.AddShard("shard-0", ""));
    }

    [Fact]
    public void AddShard_WhitespacePrimaryConnectionString_ThrowsArgumentException()
    {
        var options = new ShardedReadWriteOptions();
        Should.Throw<ArgumentException>(() =>
            options.AddShard("shard-0", "   "));
    }
}
