using Encina.DomainModeling;
using Encina.DomainModeling.Sharding;
using Encina.Sharding;

namespace Encina.UnitTests.DomainModeling.Sharding;

/// <summary>
/// Unit tests for <see cref="ShardedSpecificationExtensions"/>.
/// </summary>
public sealed class ShardedSpecificationExtensionsTests
{
    // ────────────────────────────────────────────────────────────
    //  Test entity, specification, and helpers
    // ────────────────────────────────────────────────────────────

    public sealed class TestEntity
    {
        public int Id { get; init; }
        public string Name { get; init; } = "";
    }

    private sealed class AllItemsSpec : Specification<TestEntity>
    {
        public override System.Linq.Expressions.Expression<Func<TestEntity, bool>> ToExpression()
            => _ => true;
    }

    private static Specification<TestEntity> CreateSpec() => new AllItemsSpec();

    /// <summary>
    /// Creates a mock repository that implements both
    /// <see cref="IFunctionalShardedRepository{TEntity, TId}"/> and
    /// <see cref="IShardedSpecificationSupport{TEntity, TId}"/>.
    /// </summary>
    private static IFunctionalShardedRepository<TestEntity, int> CreateSupportingRepository()
    {
        return Substitute.For<IFunctionalShardedRepository<TestEntity, int>,
            IShardedSpecificationSupport<TestEntity, int>>();
    }

    /// <summary>
    /// Creates a mock repository that does NOT implement
    /// <see cref="IShardedSpecificationSupport{TEntity, TId}"/>.
    /// </summary>
    private static IFunctionalShardedRepository<TestEntity, int> CreateUnsupportingRepository()
    {
        return Substitute.For<IFunctionalShardedRepository<TestEntity, int>>();
    }

    // ────────────────────────────────────────────────────────────
    //  QueryAllShardsAsync — null argument checks
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void QueryAllShardsAsync_NullRepository_ThrowsArgumentNullException()
    {
        // Arrange
        IFunctionalShardedRepository<TestEntity, int> repo = null!;
        var spec = CreateSpec();

        // Act & Assert
        Should.Throw<ArgumentNullException>(
            () => repo.QueryAllShardsAsync(spec));
    }

    [Fact]
    public void QueryAllShardsAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var repo = CreateSupportingRepository();
        Specification<TestEntity> spec = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(
            () => repo.QueryAllShardsAsync(spec));
    }

    // ────────────────────────────────────────────────────────────
    //  QueryAllShardsAsync — NotSupportedException
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task QueryAllShardsAsync_RepoDoesNotSupportSpec_ThrowsNotSupportedException()
    {
        // Arrange
        var repo = CreateUnsupportingRepository();
        var spec = CreateSpec();

        // Act & Assert
        var ex = await Should.ThrowAsync<NotSupportedException>(
            () => repo.QueryAllShardsAsync(spec));
        ex.Message.ShouldContain("IShardedSpecificationSupport");
    }

    // ────────────────────────────────────────────────────────────
    //  QueryAllShardsAsync — delegation
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task QueryAllShardsAsync_RepoSupportsSpec_DelegatesToSupport()
    {
        // Arrange
        var repo = CreateSupportingRepository();
        var spec = CreateSpec();
        var expected = new ShardedSpecificationResult<TestEntity>(
            Items: [new TestEntity { Id = 1 }],
            ItemsPerShard: new Dictionary<string, int> { ["shard-1"] = 1 },
            TotalDuration: TimeSpan.FromMilliseconds(50),
            DurationPerShard: new Dictionary<string, TimeSpan> { ["shard-1"] = TimeSpan.FromMilliseconds(50) },
            FailedShards: []);

        ((IShardedSpecificationSupport<TestEntity, int>)repo)
            .QueryAllShardsAsync(spec, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, ShardedSpecificationResult<TestEntity>>>(expected));

        // Act
        var result = await repo.QueryAllShardsAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r => r.Items.Count.ShouldBe(1),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    // ────────────────────────────────────────────────────────────
    //  QueryAllShardsPagedAsync — null argument checks
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void QueryAllShardsPagedAsync_NullRepository_ThrowsArgumentNullException()
    {
        // Arrange
        IFunctionalShardedRepository<TestEntity, int> repo = null!;
        var spec = CreateSpec();
        var pagination = new ShardedPaginationOptions { Page = 1, PageSize = 10 };

        // Act & Assert
        Should.Throw<ArgumentNullException>(
            () => repo.QueryAllShardsPagedAsync(spec, pagination));
    }

    [Fact]
    public void QueryAllShardsPagedAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var repo = CreateSupportingRepository();
        Specification<TestEntity> spec = null!;
        var pagination = new ShardedPaginationOptions { Page = 1, PageSize = 10 };

        // Act & Assert
        Should.Throw<ArgumentNullException>(
            () => repo.QueryAllShardsPagedAsync(spec, pagination));
    }

    [Fact]
    public void QueryAllShardsPagedAsync_NullPagination_ThrowsArgumentNullException()
    {
        // Arrange
        var repo = CreateSupportingRepository();
        var spec = CreateSpec();
        ShardedPaginationOptions pagination = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(
            () => repo.QueryAllShardsPagedAsync(spec, pagination));
    }

    // ────────────────────────────────────────────────────────────
    //  QueryAllShardsPagedAsync — NotSupportedException
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task QueryAllShardsPagedAsync_RepoDoesNotSupportSpec_ThrowsNotSupportedException()
    {
        // Arrange
        var repo = CreateUnsupportingRepository();
        var spec = CreateSpec();
        var pagination = new ShardedPaginationOptions { Page = 1, PageSize = 10 };

        // Act & Assert
        var ex = await Should.ThrowAsync<NotSupportedException>(
            () => repo.QueryAllShardsPagedAsync(spec, pagination));
        ex.Message.ShouldContain("IShardedSpecificationSupport");
    }

    // ────────────────────────────────────────────────────────────
    //  CountAllShardsAsync — null argument checks
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void CountAllShardsAsync_NullRepository_ThrowsArgumentNullException()
    {
        // Arrange
        IFunctionalShardedRepository<TestEntity, int> repo = null!;
        var spec = CreateSpec();

        // Act & Assert
        Should.Throw<ArgumentNullException>(
            () => repo.CountAllShardsAsync(spec));
    }

    [Fact]
    public void CountAllShardsAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var repo = CreateSupportingRepository();
        Specification<TestEntity> spec = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(
            () => repo.CountAllShardsAsync(spec));
    }

    // ────────────────────────────────────────────────────────────
    //  CountAllShardsAsync — NotSupportedException
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CountAllShardsAsync_RepoDoesNotSupportSpec_ThrowsNotSupportedException()
    {
        // Arrange
        var repo = CreateUnsupportingRepository();
        var spec = CreateSpec();

        // Act & Assert
        var ex = await Should.ThrowAsync<NotSupportedException>(
            () => repo.CountAllShardsAsync(spec));
        ex.Message.ShouldContain("IShardedSpecificationSupport");
    }

    // ────────────────────────────────────────────────────────────
    //  QueryShardsAsync — null argument checks
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void QueryShardsAsync_NullRepository_ThrowsArgumentNullException()
    {
        // Arrange
        IFunctionalShardedRepository<TestEntity, int> repo = null!;
        var spec = CreateSpec();
        IReadOnlyList<string> shardIds = ["shard-1"];

        // Act & Assert
        Should.Throw<ArgumentNullException>(
            () => repo.QueryShardsAsync(spec, shardIds));
    }

    [Fact]
    public void QueryShardsAsync_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var repo = CreateSupportingRepository();
        Specification<TestEntity> spec = null!;
        IReadOnlyList<string> shardIds = ["shard-1"];

        // Act & Assert
        Should.Throw<ArgumentNullException>(
            () => repo.QueryShardsAsync(spec, shardIds));
    }

    [Fact]
    public void QueryShardsAsync_NullShardIds_ThrowsArgumentNullException()
    {
        // Arrange
        var repo = CreateSupportingRepository();
        var spec = CreateSpec();
        IReadOnlyList<string> shardIds = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(
            () => repo.QueryShardsAsync(spec, shardIds));
    }

    [Fact]
    public void QueryShardsAsync_EmptyShardIds_ThrowsArgumentException()
    {
        // Arrange
        var repo = CreateSupportingRepository();
        var spec = CreateSpec();
        IReadOnlyList<string> shardIds = [];

        // Act & Assert
        Should.Throw<ArgumentException>(
            () => repo.QueryShardsAsync(spec, shardIds));
    }

    // ────────────────────────────────────────────────────────────
    //  QueryShardsAsync — NotSupportedException
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task QueryShardsAsync_RepoDoesNotSupportSpec_ThrowsNotSupportedException()
    {
        // Arrange
        var repo = CreateUnsupportingRepository();
        var spec = CreateSpec();
        IReadOnlyList<string> shardIds = ["shard-1"];

        // Act & Assert
        var ex = await Should.ThrowAsync<NotSupportedException>(
            () => repo.QueryShardsAsync(spec, shardIds));
        ex.Message.ShouldContain("IShardedSpecificationSupport");
    }
}
