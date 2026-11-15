using Encina.Sharding;

namespace Encina.GuardTests.Sharding;

/// <summary>
/// Guard clause tests for the <see cref="ShardInfo"/> constructor.
/// </summary>
public sealed class ShardInfoGuardTests
{
    private const string ConnectionString = "Server=test;Database=db";

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespaceShardId_ThrowsArgumentException(string? shardId)
    {
        var ex = Should.Throw<ArgumentException>(() => new ShardInfo(shardId!, ConnectionString));
        ex.ParamName.ShouldBe("ShardId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespaceConnectionString_ThrowsArgumentException(string? connectionString)
    {
        var ex = Should.Throw<ArgumentException>(() => new ShardInfo("shard-1", connectionString!));
        ex.ParamName.ShouldBe("ConnectionString");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespaceReplicaConnectionString_ThrowsArgumentException(string? replica)
    {
        var ex = Should.Throw<ArgumentException>(() =>
            new ShardInfo("shard-1", ConnectionString, ReplicaConnectionStrings: ["Server=r1;Database=db", replica!]));
        ex.Message.ShouldContain("index 1");
        ex.Message.ShouldNotContain("Server=r1");
    }

    [Fact]
    public void Constructor_NullReplicaList_YieldsEmptyReplicasAndMaskedToString()
    {
        var shard = new ShardInfo("shard-1", ConnectionString, ReplicaConnectionStrings: null);

        shard.ReplicaConnectionStrings.ShouldBeEmpty();
        shard.HasReplicas.ShouldBeFalse();
        shard.ReplicaCount.ShouldBe(0);
        shard.ToString().ShouldNotContain(ConnectionString);
    }
}
