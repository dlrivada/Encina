using Encina.Caching;
using Encina.Caching.Sharding;
using Encina.Caching.Sharding.Configuration;
using Encina.Sharding;
using Encina.Sharding.Configuration;
using Encina.Sharding.Execution;
using Encina.Sharding.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Caching.Sharding;

/// <summary>
/// Unit tests for <see cref="ShardingCacheServiceCollectionExtensions"/>.
/// </summary>
public sealed class ShardingCacheServiceCollectionExtensionsTests
{
    private static ServiceCollection CreateServicesWithShardingAndCaching()
    {
        var services = new ServiceCollection();

        // Register base sharding services
        services.AddEncinaSharding<TestEntity>(opts =>
        {
            opts.AddShard("shard-1", "conn1")
                .AddShard("shard-2", "conn2")
                .UseHashRouting();
        });

        // Register mock cache provider
        services.AddSingleton(Substitute.For<ICacheProvider>());

        // Register logging (required by cached providers)
        services.AddLogging();

        return services;
    }

    // ────────────────────────────────────────────────────────────
    //  Null guard
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void AddEncinaShardingCaching_NullServices_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaShardingCaching());
    }

    // ────────────────────────────────────────────────────────────
    //  Returns same collection
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void AddEncinaShardingCaching_ReturnsSameServiceCollection()
    {
        var services = CreateServicesWithShardingAndCaching();

        var result = services.AddEncinaShardingCaching();

        result.ShouldBeSameAs(services);
    }

    // ────────────────────────────────────────────────────────────
    //  Options registration
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void AddEncinaShardingCaching_RegistersShardingCacheOptions()
    {
        var services = CreateServicesWithShardingAndCaching();

        services.AddEncinaShardingCaching(opts =>
        {
            opts.TopologyRefreshInterval = TimeSpan.FromMinutes(2);
            opts.TopologyCacheDuration = TimeSpan.FromMinutes(10);
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ShardingCacheOptions>>().Value;

        options.TopologyRefreshInterval.ShouldBe(TimeSpan.FromMinutes(2));
        options.TopologyCacheDuration.ShouldBe(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public void AddEncinaShardingCaching_RegistersDirectoryCacheOptions()
    {
        var services = CreateServicesWithShardingAndCaching();

        services.AddEncinaShardingCaching(opts =>
        {
            opts.DirectoryCache.CacheDuration = TimeSpan.FromMinutes(15);
            opts.DirectoryCache.InvalidationStrategy = CacheInvalidationStrategy.WriteThrough;
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<DirectoryCacheOptions>>().Value;

        options.CacheDuration.ShouldBe(TimeSpan.FromMinutes(15));
        options.InvalidationStrategy.ShouldBe(CacheInvalidationStrategy.WriteThrough);
    }

    [Fact]
    public void AddEncinaShardingCaching_RegistersScatterGatherCacheOptions()
    {
        var services = CreateServicesWithShardingAndCaching();

        services.AddEncinaShardingCaching(opts =>
        {
            opts.ScatterGatherCache.DefaultCacheDuration = TimeSpan.FromMinutes(5);
            opts.ScatterGatherCache.MaxCachedResultSize = 5000;
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ScatterGatherCacheOptions>>().Value;

        options.DefaultCacheDuration.ShouldBe(TimeSpan.FromMinutes(5));
        options.MaxCachedResultSize.ShouldBe(5000);
    }

    // ────────────────────────────────────────────────────────────
    //  Default options — all disabled
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void AddEncinaShardingCaching_DefaultOptions_AllDisabled()
    {
        var services = CreateServicesWithShardingAndCaching();

        services.AddEncinaShardingCaching();

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ShardingCacheOptions>>().Value;

        options.EnableBackgroundRefresh.ShouldBeFalse();
        options.EnableDirectoryCaching.ShouldBeFalse();
        options.EnableScatterGatherCaching.ShouldBeFalse();
    }

    // ────────────────────────────────────────────────────────────
    //  Background refresh — registers hosted service
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void AddEncinaShardingCaching_EnableBackgroundRefresh_RegistersTopologySource()
    {
        var services = CreateServicesWithShardingAndCaching();

        services.AddEncinaShardingCaching(opts =>
        {
            opts.EnableBackgroundRefresh = true;
        });

        var provider = services.BuildServiceProvider();
        var source = provider.GetService<IShardTopologySource>();
        source.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaShardingCaching_EnableBackgroundRefresh_RegistersCachedTopologyProvider()
    {
        var services = CreateServicesWithShardingAndCaching();

        services.AddEncinaShardingCaching(opts =>
        {
            opts.EnableBackgroundRefresh = true;
        });

        var provider = services.BuildServiceProvider();
        var topologyProvider = provider.GetService<IShardTopologyProvider>();
        topologyProvider.ShouldNotBeNull();
    }

    // ────────────────────────────────────────────────────────────
    //  No configure action
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void AddEncinaShardingCaching_NoConfigure_UsesDefaults()
    {
        var services = CreateServicesWithShardingAndCaching();

        services.AddEncinaShardingCaching();

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ShardingCacheOptions>>().Value;

        options.TopologyRefreshInterval.ShouldBe(TimeSpan.FromSeconds(30));
        options.TopologyCacheDuration.ShouldBe(TimeSpan.FromMinutes(5));
    }

    // ────────────────────────────────────────────────────────────
    //  Test entity
    // ────────────────────────────────────────────────────────────

    private sealed class TestEntity : IShardable
    {
        public string Key { get; set; } = default!;
        public string GetShardKey() => Key;
    }
}
