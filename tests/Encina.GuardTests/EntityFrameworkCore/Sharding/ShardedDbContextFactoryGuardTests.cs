using Encina.EntityFrameworkCore.Sharding;
using Encina.Sharding;
using Encina.Sharding.Data;
using Microsoft.EntityFrameworkCore;

namespace Encina.GuardTests.EntityFrameworkCore.Sharding;

/// <summary>
/// Guard clause tests for <see cref="ShardedDbContextFactory{TContext}"/>.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "EntityFrameworkCore")]
public sealed class ShardedDbContextFactoryGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullTopology_ThrowsArgumentNullException()
    {
        // Arrange
        ShardTopology topology = null!;
        var router = Substitute.For<IShardRouter>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        Action<DbContextOptionsBuilder<TestShardDbContext>, string> configureOptions = (_, _) => { };

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new ShardedDbContextFactory<TestShardDbContext>(
                topology, router, serviceProvider, configureOptions));
        ex.ParamName.ShouldBe("topology");
    }

    [Fact]
    public void Constructor_NullRouter_ThrowsArgumentNullException()
    {
        // Arrange
        var topology = new ShardTopology(
        [
            new ShardInfo("shard-0", "Server=test;Database=shard0")
        ]);
        IShardRouter router = null!;
        var serviceProvider = Substitute.For<IServiceProvider>();
        Action<DbContextOptionsBuilder<TestShardDbContext>, string> configureOptions = (_, _) => { };

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new ShardedDbContextFactory<TestShardDbContext>(
                topology, router, serviceProvider, configureOptions));
        ex.ParamName.ShouldBe("router");
    }

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var topology = new ShardTopology(
        [
            new ShardInfo("shard-0", "Server=test;Database=shard0")
        ]);
        var router = Substitute.For<IShardRouter>();
        IServiceProvider serviceProvider = null!;
        Action<DbContextOptionsBuilder<TestShardDbContext>, string> configureOptions = (_, _) => { };

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new ShardedDbContextFactory<TestShardDbContext>(
                topology, router, serviceProvider, configureOptions));
        ex.ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public void Constructor_NullConfigureOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var topology = new ShardTopology(
        [
            new ShardInfo("shard-0", "Server=test;Database=shard0")
        ]);
        var router = Substitute.For<IShardRouter>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        Action<DbContextOptionsBuilder<TestShardDbContext>, string> configureOptions = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new ShardedDbContextFactory<TestShardDbContext>(
                topology, router, serviceProvider, configureOptions));
        ex.ParamName.ShouldBe("configureOptions");
    }

    #endregion

    #region Test Infrastructure

    private sealed class TestShardDbContext : DbContext
    {
        public TestShardDbContext(DbContextOptions<TestShardDbContext> options) : base(options)
        {
        }
    }

    #endregion
}
