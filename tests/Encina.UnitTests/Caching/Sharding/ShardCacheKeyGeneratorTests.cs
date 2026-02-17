using Encina.Caching.Sharding;

namespace Encina.UnitTests.Caching.Sharding;

/// <summary>
/// Unit tests for <see cref="ShardCacheKeyGenerator"/>.
/// </summary>
public sealed class ShardCacheKeyGeneratorTests
{
    // ────────────────────────────────────────────────────────────
    //  ForDirectory
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void ForDirectory_ReturnsExpectedFormat()
    {
        var key = ShardCacheKeyGenerator.ForDirectory("shard:dir", "customer-123");
        key.ShouldBe("shard:dir:customer-123");
    }

    [Fact]
    public void ForDirectory_DifferentKeys_ProduceDifferentResults()
    {
        var key1 = ShardCacheKeyGenerator.ForDirectory("prefix", "key-1");
        var key2 = ShardCacheKeyGenerator.ForDirectory("prefix", "key-2");
        key1.ShouldNotBe(key2);
    }

    [Fact]
    public void ForDirectory_DifferentPrefixes_ProduceDifferentResults()
    {
        var key1 = ShardCacheKeyGenerator.ForDirectory("prefix-a", "key");
        var key2 = ShardCacheKeyGenerator.ForDirectory("prefix-b", "key");
        key1.ShouldNotBe(key2);
    }

    // ────────────────────────────────────────────────────────────
    //  ForDirectoryAll
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void ForDirectoryAll_ReturnsExpectedFormat()
    {
        var key = ShardCacheKeyGenerator.ForDirectoryAll("shard:dir");
        key.ShouldBe("shard:dir:all");
    }

    // ────────────────────────────────────────────────────────────
    //  ForTopology
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void ForTopology_ReturnsExpectedFormat()
    {
        var key = ShardCacheKeyGenerator.ForTopology("shard:topology");
        key.ShouldBe("shard:topology:topology");
    }

    // ────────────────────────────────────────────────────────────
    //  ForScatterGather
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void ForScatterGather_ReturnsExpectedFormat()
    {
        var key = ShardCacheKeyGenerator.ForScatterGather("shard:scatter", "orders:active");
        key.ShouldBe("shard:scatter:orders:active");
    }

    [Fact]
    public void ForScatterGather_DifferentCacheKeys_ProduceDifferentResults()
    {
        var key1 = ShardCacheKeyGenerator.ForScatterGather("prefix", "query-a");
        var key2 = ShardCacheKeyGenerator.ForScatterGather("prefix", "query-b");
        key1.ShouldNotBe(key2);
    }

    // ────────────────────────────────────────────────────────────
    //  ForScatterGatherShard
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void ForScatterGatherShard_ReturnsExpectedFormat()
    {
        var key = ShardCacheKeyGenerator.ForScatterGatherShard("shard:scatter", "shard-1", "orders:active");
        key.ShouldBe("shard:scatter:shard-1:orders:active");
    }

    [Fact]
    public void ForScatterGatherShard_DifferentShards_ProduceDifferentResults()
    {
        var key1 = ShardCacheKeyGenerator.ForScatterGatherShard("prefix", "shard-1", "query");
        var key2 = ShardCacheKeyGenerator.ForScatterGatherShard("prefix", "shard-2", "query");
        key1.ShouldNotBe(key2);
    }

    // ────────────────────────────────────────────────────────────
    //  InvalidationPattern
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void InvalidationPattern_ReturnsWildcardFormat()
    {
        var pattern = ShardCacheKeyGenerator.InvalidationPattern("shard:scatter", "shard-1");
        pattern.ShouldBe("shard:scatter:shard-1:*");
    }

    [Fact]
    public void InvalidationPattern_DifferentShards_ProduceDifferentPatterns()
    {
        var p1 = ShardCacheKeyGenerator.InvalidationPattern("prefix", "shard-1");
        var p2 = ShardCacheKeyGenerator.InvalidationPattern("prefix", "shard-2");
        p1.ShouldNotBe(p2);
    }
}
