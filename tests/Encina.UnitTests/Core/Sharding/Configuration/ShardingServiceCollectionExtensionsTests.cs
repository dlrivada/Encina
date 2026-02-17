using Encina.Sharding;
using Encina.Sharding.Configuration;
using Encina.Sharding.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Core.Sharding.Configuration;

/// <summary>
/// Unit tests for <see cref="ShardingServiceCollectionExtensions"/>.
/// </summary>
public sealed class ShardingServiceCollectionExtensionsTests
{
    // ────────────────────────────────────────────────────────────
    //  Registration — null guards
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void AddEncinaSharding_NullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaSharding<TestOrder>(opts =>
            {
                opts.AddShard("shard-1", "conn1").UseHashRouting();
            }));
    }

    [Fact]
    public void AddEncinaSharding_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaSharding<TestOrder>(null!));
    }

    // ────────────────────────────────────────────────────────────
    //  Registration — validation
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void AddEncinaSharding_NoShards_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            services.AddEncinaSharding<TestOrder>(opts =>
            {
                opts.UseHashRouting();
            }));
    }

    [Fact]
    public void AddEncinaSharding_NoRouter_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            services.AddEncinaSharding<TestOrder>(opts =>
            {
                opts.AddShard("shard-1", "conn1");
            }));
    }

    // ────────────────────────────────────────────────────────────
    //  Registration — resolved types
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void AddEncinaSharding_RegistersShardTopology()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaSharding<TestOrder>(opts =>
        {
            opts.AddShard("shard-1", "conn1")
                .AddShard("shard-2", "conn2")
                .UseHashRouting();
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var topology = provider.GetService<ShardTopology>();
        topology.ShouldNotBeNull();
        topology.AllShardIds.Count.ShouldBe(2);
    }

    [Fact]
    public void AddEncinaSharding_RegistersIShardRouter()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaSharding<TestOrder>(opts =>
        {
            opts.AddShard("shard-1", "conn1").UseHashRouting();
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var router = provider.GetService<IShardRouter>();
        router.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaSharding_RegistersIShardRouterOfTEntity()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaSharding<TestOrder>(opts =>
        {
            opts.AddShard("shard-1", "conn1").UseHashRouting();
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var router = provider.GetService<IShardRouter<TestOrder>>();
        router.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaSharding_RegistersIShardTopologyProvider()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaSharding<TestOrder>(opts =>
        {
            opts.AddShard("shard-1", "conn1").UseHashRouting();
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var topologyProvider = provider.GetService<IShardTopologyProvider>();
        topologyProvider.ShouldNotBeNull();
        topologyProvider.ShouldBeOfType<DefaultShardTopologyProvider>();
    }

    [Fact]
    public void AddEncinaSharding_RegistersScatterGatherOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaSharding<TestOrder>(opts =>
        {
            opts.AddShard("shard-1", "conn1").UseHashRouting();
            opts.ScatterGather.MaxParallelism = 4;
            opts.ScatterGather.Timeout = TimeSpan.FromMinutes(1);
            opts.ScatterGather.AllowPartialResults = false;
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var scatterGatherOptions = provider.GetRequiredService<IOptions<ScatterGatherOptions>>().Value;
        scatterGatherOptions.MaxParallelism.ShouldBe(4);
        scatterGatherOptions.Timeout.ShouldBe(TimeSpan.FromMinutes(1));
        scatterGatherOptions.AllowPartialResults.ShouldBeFalse();
    }

    [Fact]
    public void AddEncinaSharding_ReturnsSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaSharding<TestOrder>(opts =>
        {
            opts.AddShard("shard-1", "conn1").UseHashRouting();
        });

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaSharding_WithHashRouting_ResolvesHashShardRouter()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaSharding<TestOrder>(opts =>
        {
            opts.AddShard("shard-1", "conn1").UseHashRouting();
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var router = provider.GetRequiredService<IShardRouter>();
        router.ShouldBeOfType<HashShardRouter>();
    }

    [Fact]
    public void AddEncinaSharding_EntityRouterDelegatesToBaseRouter()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncinaSharding<ShardableOrder>(opts =>
        {
            opts.AddShard("shard-1", "conn1")
                .AddShard("shard-2", "conn2")
                .UseHashRouting();
        });

        var provider = services.BuildServiceProvider();
        var entityRouter = provider.GetRequiredService<IShardRouter<ShardableOrder>>();
        var entity = new ShardableOrder { CustomerId = "customer-123" };

        // Act
        var result = entityRouter.GetShardId(entity);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    // ────────────────────────────────────────────────────────────
    //  Test entities
    // ────────────────────────────────────────────────────────────

    private sealed class TestOrder
    {
        public string OrderId { get; set; } = default!;
    }

    private sealed class ShardableOrder : IShardable
    {
        public string CustomerId { get; set; } = default!;
        public string GetShardKey() => CustomerId;
    }
}
