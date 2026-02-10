using Encina.Caching.Sharding.Configuration;

namespace Encina.UnitTests.Caching.Sharding;

/// <summary>
/// Unit tests for <see cref="ShardingCacheOptions"/>, <see cref="DirectoryCacheOptions"/>,
/// and <see cref="ScatterGatherCacheOptions"/>.
/// </summary>
public sealed class ShardingCacheOptionsTests
{
    // ────────────────────────────────────────────────────────────
    //  ShardingCacheOptions — defaults
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void ShardingCacheOptions_TopologyRefreshInterval_Default30Seconds()
    {
        var options = new ShardingCacheOptions();
        options.TopologyRefreshInterval.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void ShardingCacheOptions_TopologyCacheDuration_Default5Minutes()
    {
        var options = new ShardingCacheOptions();
        options.TopologyCacheDuration.ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void ShardingCacheOptions_EnableBackgroundRefresh_DefaultFalse()
    {
        var options = new ShardingCacheOptions();
        options.EnableBackgroundRefresh.ShouldBeFalse();
    }

    [Fact]
    public void ShardingCacheOptions_EnableDirectoryCaching_DefaultFalse()
    {
        var options = new ShardingCacheOptions();
        options.EnableDirectoryCaching.ShouldBeFalse();
    }

    [Fact]
    public void ShardingCacheOptions_EnableScatterGatherCaching_DefaultFalse()
    {
        var options = new ShardingCacheOptions();
        options.EnableScatterGatherCaching.ShouldBeFalse();
    }

    [Fact]
    public void ShardingCacheOptions_DirectoryCache_NotNull()
    {
        var options = new ShardingCacheOptions();
        options.DirectoryCache.ShouldNotBeNull();
    }

    [Fact]
    public void ShardingCacheOptions_ScatterGatherCache_NotNull()
    {
        var options = new ShardingCacheOptions();
        options.ScatterGatherCache.ShouldNotBeNull();
    }

    [Fact]
    public void ShardingCacheOptions_PropertiesCanBeModified()
    {
        var options = new ShardingCacheOptions
        {
            TopologyRefreshInterval = TimeSpan.FromMinutes(1),
            TopologyCacheDuration = TimeSpan.FromMinutes(10),
            EnableBackgroundRefresh = true,
            EnableDirectoryCaching = true,
            EnableScatterGatherCaching = true
        };

        options.TopologyRefreshInterval.ShouldBe(TimeSpan.FromMinutes(1));
        options.TopologyCacheDuration.ShouldBe(TimeSpan.FromMinutes(10));
        options.EnableBackgroundRefresh.ShouldBeTrue();
        options.EnableDirectoryCaching.ShouldBeTrue();
        options.EnableScatterGatherCaching.ShouldBeTrue();
    }

    // ────────────────────────────────────────────────────────────
    //  DirectoryCacheOptions — defaults
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void DirectoryCacheOptions_CacheDuration_Default5Minutes()
    {
        var options = new DirectoryCacheOptions();
        options.CacheDuration.ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void DirectoryCacheOptions_InvalidationStrategy_DefaultImmediate()
    {
        var options = new DirectoryCacheOptions();
        options.InvalidationStrategy.ShouldBe(CacheInvalidationStrategy.Immediate);
    }

    [Fact]
    public void DirectoryCacheOptions_KeyPrefix_DefaultShardDir()
    {
        var options = new DirectoryCacheOptions();
        options.KeyPrefix.ShouldBe("shard:dir");
    }

    [Fact]
    public void DirectoryCacheOptions_EnableDistributedInvalidation_DefaultFalse()
    {
        var options = new DirectoryCacheOptions();
        options.EnableDistributedInvalidation.ShouldBeFalse();
    }

    [Fact]
    public void DirectoryCacheOptions_InvalidationChannel_DefaultShardDirInvalidate()
    {
        var options = new DirectoryCacheOptions();
        options.InvalidationChannel.ShouldBe("shard:dir:invalidate");
    }

    [Fact]
    public void DirectoryCacheOptions_PropertiesCanBeModified()
    {
        var options = new DirectoryCacheOptions
        {
            CacheDuration = TimeSpan.FromMinutes(10),
            InvalidationStrategy = CacheInvalidationStrategy.WriteThrough,
            KeyPrefix = "custom:prefix",
            EnableDistributedInvalidation = true,
            InvalidationChannel = "custom:channel"
        };

        options.CacheDuration.ShouldBe(TimeSpan.FromMinutes(10));
        options.InvalidationStrategy.ShouldBe(CacheInvalidationStrategy.WriteThrough);
        options.KeyPrefix.ShouldBe("custom:prefix");
        options.EnableDistributedInvalidation.ShouldBeTrue();
        options.InvalidationChannel.ShouldBe("custom:channel");
    }

    // ────────────────────────────────────────────────────────────
    //  ScatterGatherCacheOptions — defaults
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void ScatterGatherCacheOptions_DefaultCacheDuration_Default2Minutes()
    {
        var options = new ScatterGatherCacheOptions();
        options.DefaultCacheDuration.ShouldBe(TimeSpan.FromMinutes(2));
    }

    [Fact]
    public void ScatterGatherCacheOptions_MaxCachedResultSize_Default10000()
    {
        var options = new ScatterGatherCacheOptions();
        options.MaxCachedResultSize.ShouldBe(10_000);
    }

    [Fact]
    public void ScatterGatherCacheOptions_InvalidationChannel_DefaultShardScatterInvalidate()
    {
        var options = new ScatterGatherCacheOptions();
        options.InvalidationChannel.ShouldBe("shard:scatter:invalidate");
    }

    [Fact]
    public void ScatterGatherCacheOptions_EnableResultCaching_DefaultFalse()
    {
        var options = new ScatterGatherCacheOptions();
        options.EnableResultCaching.ShouldBeFalse();
    }

    [Fact]
    public void ScatterGatherCacheOptions_PropertiesCanBeModified()
    {
        var options = new ScatterGatherCacheOptions
        {
            DefaultCacheDuration = TimeSpan.FromMinutes(5),
            MaxCachedResultSize = 5000,
            InvalidationChannel = "custom:scatter:channel",
            EnableResultCaching = true
        };

        options.DefaultCacheDuration.ShouldBe(TimeSpan.FromMinutes(5));
        options.MaxCachedResultSize.ShouldBe(5000);
        options.InvalidationChannel.ShouldBe("custom:scatter:channel");
        options.EnableResultCaching.ShouldBeTrue();
    }

    // ────────────────────────────────────────────────────────────
    //  CacheInvalidationStrategy enum
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void CacheInvalidationStrategy_HasExpectedValues()
    {
        CacheInvalidationStrategy.Immediate.ShouldBe((CacheInvalidationStrategy)0);
        CacheInvalidationStrategy.WriteThrough.ShouldBe((CacheInvalidationStrategy)1);
        CacheInvalidationStrategy.Lazy.ShouldBe((CacheInvalidationStrategy)2);
    }
}
