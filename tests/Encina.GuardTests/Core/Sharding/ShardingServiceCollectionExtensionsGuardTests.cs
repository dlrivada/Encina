using Encina.Sharding;
using Encina.Sharding.Configuration;
using Encina.Sharding.Routing;

namespace Encina.GuardTests.Core.Sharding;

/// <summary>
/// Guard clause tests for <see cref="ShardingServiceCollectionExtensions"/>.
/// Verifies null parameter handling for the AddEncinaSharding extension method.
/// </summary>
public sealed class ShardingServiceCollectionExtensionsGuardTests
{
    #region AddEncinaSharding<TEntity> Guards

    /// <summary>
    /// Verifies that AddEncinaSharding throws when the service collection is null.
    /// </summary>
    [Fact]
    public void AddEncinaSharding_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaSharding<TestShardEntity>(opts =>
            {
                opts.UseHashRouting()
                    .AddShard("shard-0", "Server=s0;Database=db;");
            }));
    }

    /// <summary>
    /// Verifies that AddEncinaSharding throws when the configure delegate is null.
    /// </summary>
    [Fact]
    public void AddEncinaSharding_NullConfigure_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaSharding<TestShardEntity>(null!));
    }

    /// <summary>
    /// Verifies that AddEncinaSharding throws when no shards are configured.
    /// </summary>
    [Fact]
    public void AddEncinaSharding_NoShards_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();

        Should.Throw<InvalidOperationException>(() =>
            services.AddEncinaSharding<TestShardEntity>(opts =>
            {
                opts.UseHashRouting(); // no AddShard
            }));
    }

    /// <summary>
    /// Verifies that AddEncinaSharding throws when no routing strategy is configured.
    /// </summary>
    [Fact]
    public void AddEncinaSharding_NoRoutingStrategy_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();

        Should.Throw<InvalidOperationException>(() =>
            services.AddEncinaSharding<TestShardEntity>(opts =>
            {
                opts.AddShard("shard-0", "Server=s0;Database=db;");
                // no Use*Routing call
            }));
    }

    /// <summary>
    /// Verifies that AddEncinaSharding throws when a shard has a whitespace connection string.
    /// </summary>
    [Fact]
    public void AddEncinaSharding_WhitespaceConnectionString_ThrowsArgumentException()
    {
        var services = new ServiceCollection();

        Should.Throw<ArgumentException>(() =>
            services.AddEncinaSharding<TestShardEntity>(opts =>
            {
                opts.UseHashRouting()
                    .AddShard("shard-0", "   ");
            }));
    }

    /// <summary>
    /// Verifies that a valid configuration registers services without throwing.
    /// </summary>
    [Fact]
    public void AddEncinaSharding_ValidConfiguration_Succeeds()
    {
        var services = new ServiceCollection();

        Should.NotThrow(() =>
            services.AddEncinaSharding<TestShardEntity>(opts =>
            {
                opts.UseHashRouting()
                    .AddShard("shard-0", "Server=s0;Database=db;")
                    .AddShard("shard-1", "Server=s1;Database=db;");
            }));

        services.Count.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// Verifies that AddEncinaSharding returns the same service collection for chaining.
    /// </summary>
    [Fact]
    public void AddEncinaSharding_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddEncinaSharding<TestShardEntity>(opts =>
        {
            opts.UseHashRouting()
                .AddShard("shard-0", "Server=s0;Database=db;");
        });

        result.ShouldBeSameAs(services);
    }

    #endregion

    #region Routing Strategy Guards

    /// <summary>
    /// Verifies that UseRangeRouting throws when ranges is null.
    /// </summary>
    [Fact]
    public void UseRangeRouting_NullRanges_ThrowsArgumentNullException()
    {
        var options = new ShardingOptions<TestShardEntity>();

        Should.Throw<ArgumentNullException>(() =>
            options.UseRangeRouting(null!));
    }

    /// <summary>
    /// Verifies that UseDirectoryRouting throws when the store is null.
    /// </summary>
    [Fact]
    public void UseDirectoryRouting_NullStore_ThrowsArgumentNullException()
    {
        var options = new ShardingOptions<TestShardEntity>();

        Should.Throw<ArgumentNullException>(() =>
            options.UseDirectoryRouting(null!));
    }

    /// <summary>
    /// Verifies that UseGeoRouting throws when regions is null.
    /// </summary>
    [Fact]
    public void UseGeoRouting_NullRegions_ThrowsArgumentNullException()
    {
        var options = new ShardingOptions<TestShardEntity>();

        Should.Throw<ArgumentNullException>(() =>
            options.UseGeoRouting(null!, _ => "us"));
    }

    /// <summary>
    /// Verifies that UseGeoRouting throws when region resolver is null.
    /// </summary>
    [Fact]
    public void UseGeoRouting_NullRegionResolver_ThrowsArgumentNullException()
    {
        var options = new ShardingOptions<TestShardEntity>();

        Should.Throw<ArgumentNullException>(() =>
            options.UseGeoRouting([], null!));
    }

    /// <summary>
    /// Verifies that UseCompoundRouting throws when configure is null.
    /// </summary>
    [Fact]
    public void UseCompoundRouting_NullConfigure_ThrowsArgumentNullException()
    {
        var options = new ShardingOptions<TestShardEntity>();

        Should.Throw<ArgumentNullException>(() =>
            options.UseCompoundRouting(null!));
    }

    /// <summary>
    /// Verifies that UseCustomRouting throws when router factory is null.
    /// </summary>
    [Fact]
    public void UseCustomRouting_NullFactory_ThrowsArgumentNullException()
    {
        var options = new ShardingOptions<TestShardEntity>();

        Should.Throw<ArgumentNullException>(() =>
            options.UseCustomRouting(null!));
    }

    /// <summary>
    /// Verifies that UseTimeBasedRouting throws when configure is null.
    /// </summary>
    [Fact]
    public void UseTimeBasedRouting_NullConfigure_ThrowsArgumentNullException()
    {
        var options = new ShardingOptions<TestShardEntity>();

        Should.Throw<ArgumentNullException>(() =>
            options.UseTimeBasedRouting(null!));
    }

    #endregion

    #region Test Helpers

    private sealed class TestShardEntity : IShardable
    {
        public string GetShardKey() => "test-key";
    }

    #endregion
}
