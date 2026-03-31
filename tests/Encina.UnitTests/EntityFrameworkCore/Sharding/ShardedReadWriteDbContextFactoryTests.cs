using Encina.EntityFrameworkCore.Sharding;
using Encina.Messaging.ReadWriteSeparation;
using Encina.Sharding;
using Encina.Sharding.ReplicaSelection;
using Encina.Testing.Shouldly;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.EntityFrameworkCore.Sharding;

/// <summary>
/// Unit tests for <see cref="ShardedReadWriteDbContextFactory{TContext}"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ShardedReadWriteDbContextFactoryTests : IDisposable
{
    private static readonly string[] Shard0Replicas = ["Server=replica-0;Database=test;"];
    private static readonly string[] Shard1Replicas = ["Server=replica-1;Database=test;"];
    private static readonly string[] Shard0MultiReplicas = ["Server=replica-0a;", "Server=replica-0b;"];

    private readonly ServiceProvider _serviceProvider;
    private readonly IReplicaHealthTracker _healthTracker;

    public ShardedReadWriteDbContextFactoryTests()
    {
        var services = new ServiceCollection();
        _serviceProvider = services.BuildServiceProvider();

        _healthTracker = Substitute.For<IReplicaHealthTracker>();
        _healthTracker
            .GetAvailableReplicas(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>())
            .Returns(callInfo => callInfo.ArgAt<IReadOnlyList<string>>(1));
    }

    public void Dispose()
    {
        DatabaseRoutingContext.Clear();
        _serviceProvider.Dispose();
    }

    #region Constructor Guard Clauses

    [Fact]
    public void Constructor_WithNullTopology_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => CreateFactory(
            topology: null!,
            options: new ShardedReadWriteOptions()));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var topology = CreateTopologyWithReplicas("shard-0");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new ShardedReadWriteDbContextFactory<TestDbContext>(
            topology,
            null!,
            _healthTracker,
            _serviceProvider,
            (builder, cs) => builder.UseInMemoryDatabase(cs)));
    }

    [Fact]
    public void Constructor_WithNullHealthTracker_ThrowsArgumentNullException()
    {
        // Arrange
        var topology = CreateTopologyWithReplicas("shard-0");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new ShardedReadWriteDbContextFactory<TestDbContext>(
            topology,
            new ShardedReadWriteOptions(),
            null!,
            _serviceProvider,
            (builder, cs) => builder.UseInMemoryDatabase(cs)));
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var topology = CreateTopologyWithReplicas("shard-0");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new ShardedReadWriteDbContextFactory<TestDbContext>(
            topology,
            new ShardedReadWriteOptions(),
            _healthTracker,
            null!,
            (builder, cs) => builder.UseInMemoryDatabase(cs)));
    }

    [Fact]
    public void Constructor_WithNullConfigureOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var topology = CreateTopologyWithReplicas("shard-0");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new ShardedReadWriteDbContextFactory<TestDbContext>(
            topology,
            new ShardedReadWriteOptions(),
            _healthTracker,
            _serviceProvider,
            null!));
    }

    #endregion

    #region CreateWriteContextForShard

    [Fact]
    public void CreateWriteContextForShard_ValidShardId_ReturnsRightWithContext()
    {
        // Arrange
        var factory = CreateFactoryWithReplicas("shard-0");

        // Act
        var result = factory.CreateWriteContextForShard("shard-0");

        // Assert
        var context = result.ShouldBeRight();
        context.ShouldNotBeNull();
        context.ShouldBeOfType<TestDbContext>();
        context.Dispose();
    }

    [Fact]
    public void CreateWriteContextForShard_UnknownShardId_ReturnsLeft()
    {
        // Arrange
        var factory = CreateFactoryWithReplicas("shard-0");

        // Act
        var result = factory.CreateWriteContextForShard("unknown");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateWriteContextForShard_NullOrWhitespace_ThrowsArgumentException(string? shardId)
    {
        // Arrange
        var factory = CreateFactoryWithReplicas("shard-0");

        // Act & Assert
        Should.Throw<ArgumentException>(() => factory.CreateWriteContextForShard(shardId!));
    }

    #endregion

    #region CreateReadContextForShard

    [Fact]
    public void CreateReadContextForShard_ShardWithReplicas_ReturnsRightWithContext()
    {
        // Arrange
        var factory = CreateFactoryWithReplicas("shard-0");

        // Act
        var result = factory.CreateReadContextForShard("shard-0");

        // Assert
        var context = result.ShouldBeRight();
        context.ShouldNotBeNull();
        context.Dispose();
    }

    [Fact]
    public void CreateReadContextForShard_ShardWithReplicas_MarksReplicaHealthy()
    {
        // Arrange
        var factory = CreateFactoryWithReplicas("shard-0");

        // Act
        factory.CreateReadContextForShard("shard-0");

        // Assert
        _healthTracker.Received(1).MarkHealthy("shard-0", Arg.Any<string>());
    }

    [Fact]
    public void CreateReadContextForShard_NoReplicas_FallbackEnabled_ReturnsPrimaryContext()
    {
        // Arrange
        var options = new ShardedReadWriteOptions { FallbackToPrimaryWhenNoReplicas = true };
        var factory = CreateFactoryWithoutReplicas("shard-0", options);

        // Act
        var result = factory.CreateReadContextForShard("shard-0");

        // Assert
        result.ShouldBeRight().ShouldNotBeNull();
    }

    [Fact]
    public void CreateReadContextForShard_NoReplicas_FallbackDisabled_ReturnsLeft()
    {
        // Arrange
        var options = new ShardedReadWriteOptions { FallbackToPrimaryWhenNoReplicas = false };
        var factory = CreateFactoryWithoutReplicas("shard-0", options);

        // Act
        var result = factory.CreateReadContextForShard("shard-0");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void CreateReadContextForShard_AllReplicasUnhealthy_FallbackEnabled_ReturnsPrimaryContext()
    {
        // Arrange
        _healthTracker
            .GetAvailableReplicas(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>())
            .Returns(new List<string>());

        var options = new ShardedReadWriteOptions { FallbackToPrimaryWhenNoReplicas = true };
        var factory = CreateFactoryWithReplicas("shard-0", options);

        // Act
        var result = factory.CreateReadContextForShard("shard-0");

        // Assert
        result.ShouldBeRight().ShouldNotBeNull();
    }

    [Fact]
    public void CreateReadContextForShard_AllReplicasUnhealthy_FallbackDisabled_ReturnsLeft()
    {
        // Arrange
        _healthTracker
            .GetAvailableReplicas(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>())
            .Returns(new List<string>());

        var options = new ShardedReadWriteOptions { FallbackToPrimaryWhenNoReplicas = false };
        var factory = CreateFactoryWithReplicas("shard-0", options);

        // Act
        var result = factory.CreateReadContextForShard("shard-0");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void CreateReadContextForShard_UnknownShardId_ReturnsLeft()
    {
        // Arrange
        var factory = CreateFactoryWithReplicas("shard-0");

        // Act
        var result = factory.CreateReadContextForShard("unknown");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateReadContextForShard_NullOrWhitespace_ThrowsArgumentException(string? shardId)
    {
        // Arrange
        var factory = CreateFactoryWithReplicas("shard-0");

        // Act & Assert
        Should.Throw<ArgumentException>(() => factory.CreateReadContextForShard(shardId!));
    }

    #endregion

    #region CreateContextForShard (Ambient Routing)

    [Fact]
    public void CreateContextForShard_ReadIntent_UsesReadReplica()
    {
        // Arrange
        var factory = CreateFactoryWithReplicas("shard-0");
        DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Read;

        // Act
        var result = factory.CreateContextForShard("shard-0");

        // Assert
        result.ShouldBeRight().ShouldNotBeNull();
        _healthTracker.Received(1).MarkHealthy("shard-0", Arg.Any<string>());
    }

    [Fact]
    public void CreateContextForShard_WriteIntent_UsesPrimary()
    {
        // Arrange
        var factory = CreateFactoryWithReplicas("shard-0");
        DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Write;

        // Act
        var result = factory.CreateContextForShard("shard-0");

        // Assert
        result.ShouldBeRight().ShouldNotBeNull();
        _healthTracker.DidNotReceive().MarkHealthy(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void CreateContextForShard_NoIntent_DefaultsToWrite()
    {
        // Arrange
        var factory = CreateFactoryWithReplicas("shard-0");
        DatabaseRoutingContext.Clear();

        // Act
        var result = factory.CreateContextForShard("shard-0");

        // Assert
        result.ShouldBeRight().ShouldNotBeNull();
        _healthTracker.DidNotReceive().MarkHealthy(Arg.Any<string>(), Arg.Any<string>());
    }

    #endregion

    #region CreateAllReadContexts / CreateAllWriteContexts

    [Fact]
    public void CreateAllReadContexts_MultipleActiveShards_ReturnsAllContexts()
    {
        // Arrange
        var factory = CreateFactoryWithReplicas("shard-0", "shard-1");

        // Act
        var result = factory.CreateAllReadContexts();

        // Assert
        var contexts = result.ShouldBeRight();
        contexts.Count.ShouldBe(2);
        contexts.ShouldContainKey("shard-0");
        contexts.ShouldContainKey("shard-1");

        foreach (var ctx in contexts.Values)
        {
            ctx.Dispose();
        }
    }

    [Fact]
    public void CreateAllWriteContexts_MultipleActiveShards_ReturnsAllContexts()
    {
        // Arrange
        var factory = CreateFactoryWithReplicas("shard-0", "shard-1");

        // Act
        var result = factory.CreateAllWriteContexts();

        // Assert
        var contexts = result.ShouldBeRight();
        contexts.Count.ShouldBe(2);
        contexts.ShouldContainKey("shard-0");
        contexts.ShouldContainKey("shard-1");

        foreach (var ctx in contexts.Values)
        {
            ctx.Dispose();
        }
    }

    [Fact]
    public void CreateAllReadContexts_AllReplicasUnhealthy_FallbackDisabled_ReturnsLeft()
    {
        // Arrange
        _healthTracker
            .GetAvailableReplicas(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>())
            .Returns(new List<string>());

        var options = new ShardedReadWriteOptions { FallbackToPrimaryWhenNoReplicas = false };
        var factory = CreateFactoryWithReplicas("shard-0", "shard-1", options: options);

        // Act
        var result = factory.CreateAllReadContexts();

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void CreateAllWriteContexts_InactiveShardsExcluded()
    {
        // Arrange
        var shards = new[]
        {
            new ShardInfo("shard-0", "Server=shard-0;Database=test;", IsActive: true,
                ReplicaConnectionStrings: Shard0Replicas),
            new ShardInfo("shard-1", "Server=shard-1;Database=test;", IsActive: false,
                ReplicaConnectionStrings: Shard1Replicas)
        };
        var topology = new ShardTopology(shards);
        var options = new ShardedReadWriteOptions();

        var factory = new ShardedReadWriteDbContextFactory<TestDbContext>(
            topology, options, _healthTracker, _serviceProvider,
            (builder, cs) => builder.UseInMemoryDatabase(cs));

        // Act
        var result = factory.CreateAllWriteContexts();

        // Assert
        var contexts = result.ShouldBeRight();
        contexts.Count.ShouldBe(1);
        contexts.ShouldContainKey("shard-0");

        foreach (var ctx in contexts.Values)
        {
            ctx.Dispose();
        }
    }

    #endregion

    #region Async Methods

    [Fact]
    public async Task CreateReadContextForShardAsync_ValidShardId_ReturnsRightWithContext()
    {
        // Arrange
        var factory = CreateFactoryWithReplicas("shard-0");

        // Act
        var result = await factory.CreateReadContextForShardAsync("shard-0");

        // Assert
        result.ShouldBeRight().ShouldNotBeNull();
    }

    [Fact]
    public async Task CreateWriteContextForShardAsync_ValidShardId_ReturnsRightWithContext()
    {
        // Arrange
        var factory = CreateFactoryWithReplicas("shard-0");

        // Act
        var result = await factory.CreateWriteContextForShardAsync("shard-0");

        // Assert
        result.ShouldBeRight().ShouldNotBeNull();
    }

    [Fact]
    public async Task CreateContextForShardAsync_ReadIntent_UsesReadReplica()
    {
        // Arrange
        var factory = CreateFactoryWithReplicas("shard-0");
        DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Read;

        // Act
        var result = await factory.CreateContextForShardAsync("shard-0");

        // Assert
        result.ShouldBeRight().ShouldNotBeNull();
        _healthTracker.Received(1).MarkHealthy("shard-0", Arg.Any<string>());
    }

    #endregion

    #region Per-Shard Replica Strategy

    [Fact]
    public void CreateReadContextForShard_ShardWithCustomStrategy_UsesShardStrategy()
    {
        // Arrange
        var shards = new[]
        {
            new ShardInfo("shard-0", "Server=shard-0;Database=test;",
                ReplicaConnectionStrings: Shard0MultiReplicas,
                ReplicaStrategy: ReplicaSelectionStrategy.Random)
        };
        var topology = new ShardTopology(shards);
        var options = new ShardedReadWriteOptions { DefaultReplicaStrategy = ReplicaSelectionStrategy.RoundRobin };

        var factory = new ShardedReadWriteDbContextFactory<TestDbContext>(
            topology, options, _healthTracker, _serviceProvider,
            (builder, cs) => builder.UseInMemoryDatabase(cs));

        // Act - should not throw; the per-shard strategy is used
        var result = factory.CreateReadContextForShard("shard-0");

        // Assert
        result.ShouldBeRight().ShouldNotBeNull();
    }

    #endregion

    #region Helpers

    private ShardedReadWriteDbContextFactory<TestDbContext> CreateFactory(
        ShardTopology topology,
        ShardedReadWriteOptions options)
    {
        return new ShardedReadWriteDbContextFactory<TestDbContext>(
            topology, options, _healthTracker, _serviceProvider,
            (builder, cs) => builder.UseInMemoryDatabase(cs));
    }

    private ShardedReadWriteDbContextFactory<TestDbContext> CreateFactoryWithReplicas(
        params string[] shardIds)
    {
        return CreateFactoryWithReplicas(shardIds, options: new ShardedReadWriteOptions());
    }

    private ShardedReadWriteDbContextFactory<TestDbContext> CreateFactoryWithReplicas(
        string shardId, ShardedReadWriteOptions options)
    {
        return CreateFactoryWithReplicas(new[] { shardId }, options: options);
    }

    private ShardedReadWriteDbContextFactory<TestDbContext> CreateFactoryWithReplicas(
        string shardId1, string shardId2, ShardedReadWriteOptions? options = null)
    {
        return CreateFactoryWithReplicas(new[] { shardId1, shardId2 }, options: options ?? new ShardedReadWriteOptions());
    }

    private ShardedReadWriteDbContextFactory<TestDbContext> CreateFactoryWithReplicas(
        string[] shardIds,
        ShardedReadWriteOptions? options = null)
    {
        var topology = CreateTopologyWithReplicas(shardIds);
        return new ShardedReadWriteDbContextFactory<TestDbContext>(
            topology,
            options ?? new ShardedReadWriteOptions(),
            _healthTracker,
            _serviceProvider,
            (builder, cs) => builder.UseInMemoryDatabase(cs));
    }

    private ShardedReadWriteDbContextFactory<TestDbContext> CreateFactoryWithoutReplicas(
        string shardId, ShardedReadWriteOptions options)
    {
        var shards = new[] { new ShardInfo(shardId, $"Server={shardId};Database=test;") };
        var topology = new ShardTopology(shards);
        return new ShardedReadWriteDbContextFactory<TestDbContext>(
            topology, options, _healthTracker, _serviceProvider,
            (builder, cs) => builder.UseInMemoryDatabase(cs));
    }

    private static ShardTopology CreateTopologyWithReplicas(params string[] shardIds)
    {
        var shards = shardIds.Select(id =>
            new ShardInfo(id, $"Server={id};Database=test;",
                ReplicaConnectionStrings: new[] { $"Server=replica-{id};Database=test;" }))
            .ToList();
        return new ShardTopology(shards);
    }

    #endregion
}
