using Encina.EntityFrameworkCore.Sharding;
using Encina.Sharding;
using Encina.Sharding.Data;
using Encina.Testing.Shouldly;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.EntityFrameworkCore.Sharding;

/// <summary>
/// Unit tests for <see cref="ShardedDbContextFactory{TContext}"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ShardedDbContextFactoryTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IShardRouter _router;

    public ShardedDbContextFactoryTests()
    {
        var services = new ServiceCollection();
        _serviceProvider = services.BuildServiceProvider();
        _router = Substitute.For<IShardRouter>();
    }

    public void Dispose() => _serviceProvider.Dispose();

    #region Constructor Guard Clauses

    [Fact]
    public void Constructor_WithNullTopology_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new ShardedDbContextFactory<TestDbContext>(
            null!,
            _router,
            _serviceProvider,
            (builder, cs) => builder.UseInMemoryDatabase(cs)));
    }

    [Fact]
    public void Constructor_WithNullRouter_ThrowsArgumentNullException()
    {
        // Arrange
        var topology = CreateTopology("shard-0");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new ShardedDbContextFactory<TestDbContext>(
            topology,
            null!,
            _serviceProvider,
            (builder, cs) => builder.UseInMemoryDatabase(cs)));
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var topology = CreateTopology("shard-0");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new ShardedDbContextFactory<TestDbContext>(
            topology,
            _router,
            null!,
            (builder, cs) => builder.UseInMemoryDatabase(cs)));
    }

    [Fact]
    public void Constructor_WithNullConfigureOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var topology = CreateTopology("shard-0");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new ShardedDbContextFactory<TestDbContext>(
            topology,
            _router,
            _serviceProvider,
            null!));
    }

    #endregion

    #region CreateContextForShard

    [Fact]
    public void CreateContextForShard_ValidShardId_ReturnsRightWithContext()
    {
        // Arrange
        var factory = CreateFactory("shard-0");

        // Act
        var result = factory.CreateContextForShard("shard-0");

        // Assert
        var context = result.ShouldBeRight();
        context.ShouldNotBeNull();
        context.ShouldBeOfType<TestDbContext>();
        context.Dispose();
    }

    [Fact]
    public void CreateContextForShard_UnknownShardId_ReturnsLeft()
    {
        // Arrange
        var factory = CreateFactory("shard-0");

        // Act
        var result = factory.CreateContextForShard("unknown-shard");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void CreateContextForShard_NullOrWhitespaceShardId_ThrowsArgumentException(string? shardId)
    {
        // Arrange
        var factory = CreateFactory("shard-0");

        // Act & Assert
        Should.Throw<ArgumentException>(() => factory.CreateContextForShard(shardId!));
    }

    [Fact]
    public void CreateContextForShard_InvokesConfigureOptionsWithConnectionString()
    {
        // Arrange
        string? capturedConnectionString = null;
        var topology = CreateTopology("shard-0");

        var factory = new ShardedDbContextFactory<TestDbContext>(
            topology,
            _router,
            _serviceProvider,
            (builder, cs) =>
            {
                capturedConnectionString = cs;
                builder.UseInMemoryDatabase(cs);
            });

        // Act
        var result = factory.CreateContextForShard("shard-0");

        // Assert
        result.IsRight.ShouldBeTrue();
        capturedConnectionString.ShouldBe("Server=shard-0;Database=test;");
    }

    #endregion

    #region CreateContextForEntity

    [Fact]
    public void CreateContextForEntity_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var factory = CreateFactory("shard-0");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => factory.CreateContextForEntity<ShardableOrder>(null!));
    }

    [Fact]
    public void CreateContextForEntity_ShardableEntity_RoutesToCorrectShard()
    {
        // Arrange
        var topology = CreateTopology("shard-0", "shard-1");
        _router.GetShardId(Arg.Any<string>()).Returns(Either<EncinaError, string>.Right("shard-1"));

        var factory = new ShardedDbContextFactory<TestDbContext>(
            topology,
            _router,
            _serviceProvider,
            (builder, cs) => builder.UseInMemoryDatabase(cs));

        var entity = new ShardableOrder("customer-123");

        // Act
        var result = factory.CreateContextForEntity(entity);

        // Assert
        var context = result.ShouldBeRight();
        context.ShouldNotBeNull();
        context.Dispose();
    }

    [Fact]
    public void CreateContextForEntity_EntityWithShardKeyAttribute_RoutesToCorrectShard()
    {
        // Arrange
        var topology = CreateTopology("shard-0");
        _router.GetShardId(Arg.Any<string>()).Returns(Either<EncinaError, string>.Right("shard-0"));

        var factory = new ShardedDbContextFactory<TestDbContext>(
            topology,
            _router,
            _serviceProvider,
            (builder, cs) => builder.UseInMemoryDatabase(cs));

        var entity = new AttributeShardedEntity { TenantId = "tenant-1" };

        // Act
        var result = factory.CreateContextForEntity(entity);

        // Assert
        result.ShouldBeRight().ShouldNotBeNull();
    }

    [Fact]
    public void CreateContextForEntity_RouterReturnsLeft_ReturnsLeft()
    {
        // Arrange
        var topology = CreateTopology("shard-0");
        _router.GetShardId(Arg.Any<string>()).Returns(
            Either<EncinaError, string>.Left(
                EncinaErrors.Create("ROUTING_FAILED", "Routing failed")));

        var factory = new ShardedDbContextFactory<TestDbContext>(
            topology,
            _router,
            _serviceProvider,
            (builder, cs) => builder.UseInMemoryDatabase(cs));

        var entity = new ShardableOrder("customer-123");

        // Act
        var result = factory.CreateContextForEntity(entity);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void CreateContextForEntity_EntityWithoutShardKey_ReturnsLeft()
    {
        // Arrange
        var factory = CreateFactory("shard-0");
        var entity = new NoShardKeyEntity { Name = "test" };

        // Act
        var result = factory.CreateContextForEntity(entity);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region CreateAllContexts

    [Fact]
    public void CreateAllContexts_MultipleActiveShards_ReturnsAllContexts()
    {
        // Arrange
        var factory = CreateFactory("shard-0", "shard-1", "shard-2");

        // Act
        var result = factory.CreateAllContexts();

        // Assert
        var contexts = result.ShouldBeRight();
        contexts.Count.ShouldBe(3);
        contexts.ShouldContainKey("shard-0");
        contexts.ShouldContainKey("shard-1");
        contexts.ShouldContainKey("shard-2");

        foreach (var ctx in contexts.Values)
        {
            ctx.Dispose();
        }
    }

    [Fact]
    public void CreateAllContexts_NoActiveShards_ReturnsEmptyDictionary()
    {
        // Arrange
        var shards = new[] { new ShardInfo("shard-0", "Server=shard-0;Database=test;", IsActive: false) };
        var topology = new ShardTopology(shards);

        var factory = new ShardedDbContextFactory<TestDbContext>(
            topology,
            _router,
            _serviceProvider,
            (builder, cs) => builder.UseInMemoryDatabase(cs));

        // Act
        var result = factory.CreateAllContexts();

        // Assert
        var contexts = result.ShouldBeRight();
        contexts.Count.ShouldBe(0);
    }

    [Fact]
    public void CreateAllContexts_OnlyActiveShards_ExcludesInactiveShards()
    {
        // Arrange
        var shards = new[]
        {
            new ShardInfo("shard-0", "Server=shard-0;Database=test;", IsActive: true),
            new ShardInfo("shard-1", "Server=shard-1;Database=test;", IsActive: false),
            new ShardInfo("shard-2", "Server=shard-2;Database=test;", IsActive: true)
        };
        var topology = new ShardTopology(shards);

        var factory = new ShardedDbContextFactory<TestDbContext>(
            topology,
            _router,
            _serviceProvider,
            (builder, cs) => builder.UseInMemoryDatabase(cs));

        // Act
        var result = factory.CreateAllContexts();

        // Assert
        var contexts = result.ShouldBeRight();
        contexts.Count.ShouldBe(2);
        contexts.ShouldContainKey("shard-0");
        contexts.ShouldNotContainKey("shard-1");
        contexts.ShouldContainKey("shard-2");

        foreach (var ctx in contexts.Values)
        {
            ctx.Dispose();
        }
    }

    #endregion

    #region Async Methods

    [Fact]
    public async Task CreateContextForShardAsync_ValidShardId_ReturnsRightWithContext()
    {
        // Arrange
        var factory = CreateFactory("shard-0");

        // Act
        var result = await factory.CreateContextForShardAsync("shard-0");

        // Assert
        var context = result.ShouldBeRight();
        context.ShouldNotBeNull();
        context.Dispose();
    }

    [Fact]
    public async Task CreateContextForShardAsync_UnknownShardId_ReturnsLeft()
    {
        // Arrange
        var factory = CreateFactory("shard-0");

        // Act
        var result = await factory.CreateContextForShardAsync("unknown");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateContextForEntityAsync_ValidEntity_ReturnsRightWithContext()
    {
        // Arrange
        var topology = CreateTopology("shard-0");
        _router.GetShardId(Arg.Any<string>()).Returns(Either<EncinaError, string>.Right("shard-0"));

        var factory = new ShardedDbContextFactory<TestDbContext>(
            topology,
            _router,
            _serviceProvider,
            (builder, cs) => builder.UseInMemoryDatabase(cs));

        var entity = new ShardableOrder("customer-1");

        // Act
        var result = await factory.CreateContextForEntityAsync(entity);

        // Assert
        var context = result.ShouldBeRight();
        context.ShouldNotBeNull();
        context.Dispose();
    }

    [Fact]
    public async Task CreateAllContextsAsync_ReturnsAllActiveContexts()
    {
        // Arrange
        var factory = CreateFactory("shard-0", "shard-1");

        // Act
        var result = await factory.CreateAllContextsAsync();

        // Assert
        var contexts = result.ShouldBeRight();
        contexts.Count.ShouldBe(2);

        foreach (var ctx in contexts.Values)
        {
            ctx.Dispose();
        }
    }

    #endregion

    #region Helpers

    private ShardedDbContextFactory<TestDbContext> CreateFactory(params string[] shardIds)
    {
        var topology = CreateTopology(shardIds);
        return new ShardedDbContextFactory<TestDbContext>(
            topology,
            _router,
            _serviceProvider,
            (builder, cs) => builder.UseInMemoryDatabase(cs));
    }

    private static ShardTopology CreateTopology(params string[] shardIds)
    {
        var shards = shardIds.Select(id =>
            new ShardInfo(id, $"Server={id};Database=test;")).ToList();
        return new ShardTopology(shards);
    }

    #endregion

    #region Test Entities

    private sealed class ShardableOrder(string customerId) : IShardable
    {
        public string CustomerId { get; } = customerId;
        public string GetShardKey() => CustomerId;
    }

    private sealed class AttributeShardedEntity
    {
        [ShardKey]
        public string TenantId { get; set; } = string.Empty;
    }

    private sealed class NoShardKeyEntity
    {
        public string Name { get; set; } = string.Empty;
    }

    #endregion
}
