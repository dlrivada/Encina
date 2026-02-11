using System.Linq.Expressions;
using System.Numerics;
using Encina.Sharding;
using Encina.Sharding.Extensions;
using LanguageExt;
using NSubstitute;

namespace Encina.UnitTests.Core.Sharding.Aggregation;

/// <summary>
/// Unit tests for <see cref="ShardedAggregationExtensions"/>.
/// </summary>
public sealed class ShardedAggregationExtensionsTests
{
    // ────────────────────────────────────────────────────────────
    //  Test entity and helpers
    // ────────────────────────────────────────────────────────────

    // Must be public for NSubstitute/Castle DynamicProxy to create proxies with generic interfaces
    public sealed class TestEntity
    {
        public int Id { get; init; }
        public decimal Amount { get; init; }
        public bool IsActive { get; init; }
    }

    private static AggregationResult<T> CreateResult<T>(T value, int shardsQueried = 3)
    {
        return new AggregationResult<T>(value, shardsQueried, [], TimeSpan.FromMilliseconds(50));
    }

    // ────────────────────────────────────────────────────────────
    //  CountAcrossShardsAsync — null argument checks
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CountAcrossShardsAsync_NullRepository_ThrowsArgumentNullException()
    {
        // Arrange
        IFunctionalShardedRepository<TestEntity, int> repo = null!;
        Expression<Func<TestEntity, bool>> predicate = e => e.IsActive;

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => repo.CountAcrossShardsAsync(predicate));
    }

    [Fact]
    public async Task CountAcrossShardsAsync_NullPredicate_ThrowsArgumentNullException()
    {
        // Arrange
        var repo = CreateSupportingRepository();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => repo.CountAcrossShardsAsync(null!));
    }

    // ────────────────────────────────────────────────────────────
    //  SumAcrossShardsAsync — null argument checks
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SumAcrossShardsAsync_NullRepository_ThrowsArgumentNullException()
    {
        // Arrange
        IFunctionalShardedRepository<TestEntity, int> repo = null!;
        Expression<Func<TestEntity, decimal>> selector = e => e.Amount;

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => repo.SumAcrossShardsAsync(selector));
    }

    [Fact]
    public async Task SumAcrossShardsAsync_NullSelector_ThrowsArgumentNullException()
    {
        // Arrange
        var repo = CreateSupportingRepository();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => repo.SumAcrossShardsAsync<TestEntity, int, decimal>(null!));
    }

    // ────────────────────────────────────────────────────────────
    //  AvgAcrossShardsAsync — null argument checks
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task AvgAcrossShardsAsync_NullRepository_ThrowsArgumentNullException()
    {
        // Arrange
        IFunctionalShardedRepository<TestEntity, int> repo = null!;
        Expression<Func<TestEntity, decimal>> selector = e => e.Amount;

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => repo.AvgAcrossShardsAsync(selector));
    }

    [Fact]
    public async Task AvgAcrossShardsAsync_NullSelector_ThrowsArgumentNullException()
    {
        // Arrange
        var repo = CreateSupportingRepository();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => repo.AvgAcrossShardsAsync<TestEntity, int, decimal>(null!));
    }

    // ────────────────────────────────────────────────────────────
    //  MinAcrossShardsAsync — null argument checks
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task MinAcrossShardsAsync_NullRepository_ThrowsArgumentNullException()
    {
        // Arrange
        IFunctionalShardedRepository<TestEntity, int> repo = null!;
        Expression<Func<TestEntity, decimal>> selector = e => e.Amount;

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => repo.MinAcrossShardsAsync(selector));
    }

    [Fact]
    public async Task MinAcrossShardsAsync_NullSelector_ThrowsArgumentNullException()
    {
        // Arrange
        var repo = CreateSupportingRepository();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => repo.MinAcrossShardsAsync<TestEntity, int, decimal>(null!));
    }

    // ────────────────────────────────────────────────────────────
    //  MaxAcrossShardsAsync — null argument checks
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task MaxAcrossShardsAsync_NullRepository_ThrowsArgumentNullException()
    {
        // Arrange
        IFunctionalShardedRepository<TestEntity, int> repo = null!;
        Expression<Func<TestEntity, decimal>> selector = e => e.Amount;

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => repo.MaxAcrossShardsAsync(selector));
    }

    [Fact]
    public async Task MaxAcrossShardsAsync_NullSelector_ThrowsArgumentNullException()
    {
        // Arrange
        var repo = CreateSupportingRepository();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => repo.MaxAcrossShardsAsync<TestEntity, int, decimal>(null!));
    }

    // ────────────────────────────────────────────────────────────
    //  NotSupportedException — repository doesn't implement interface
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CountAcrossShardsAsync_UnsupportedRepository_ThrowsNotSupportedException()
    {
        // Arrange: mock that does NOT implement IShardedAggregationSupport
        var repo = Substitute.For<IFunctionalShardedRepository<TestEntity, int>>();

        // Act & Assert
        var ex = await Should.ThrowAsync<NotSupportedException>(
            () => repo.CountAcrossShardsAsync(e => e.IsActive));
        ex.Message.ShouldContain("IShardedAggregationSupport");
    }

    [Fact]
    public async Task SumAcrossShardsAsync_UnsupportedRepository_ThrowsNotSupportedException()
    {
        // Arrange
        var repo = Substitute.For<IFunctionalShardedRepository<TestEntity, int>>();

        // Act & Assert
        var ex = await Should.ThrowAsync<NotSupportedException>(
            () => repo.SumAcrossShardsAsync<TestEntity, int, decimal>(e => e.Amount));
        ex.Message.ShouldContain("IShardedAggregationSupport");
    }

    [Fact]
    public async Task AvgAcrossShardsAsync_UnsupportedRepository_ThrowsNotSupportedException()
    {
        // Arrange
        var repo = Substitute.For<IFunctionalShardedRepository<TestEntity, int>>();

        // Act & Assert
        var ex = await Should.ThrowAsync<NotSupportedException>(
            () => repo.AvgAcrossShardsAsync<TestEntity, int, decimal>(e => e.Amount));
        ex.Message.ShouldContain("IShardedAggregationSupport");
    }

    [Fact]
    public async Task MinAcrossShardsAsync_UnsupportedRepository_ThrowsNotSupportedException()
    {
        // Arrange
        var repo = Substitute.For<IFunctionalShardedRepository<TestEntity, int>>();

        // Act & Assert
        var ex = await Should.ThrowAsync<NotSupportedException>(
            () => repo.MinAcrossShardsAsync<TestEntity, int, decimal>(e => e.Amount));
        ex.Message.ShouldContain("IShardedAggregationSupport");
    }

    [Fact]
    public async Task MaxAcrossShardsAsync_UnsupportedRepository_ThrowsNotSupportedException()
    {
        // Arrange
        var repo = Substitute.For<IFunctionalShardedRepository<TestEntity, int>>();

        // Act & Assert
        var ex = await Should.ThrowAsync<NotSupportedException>(
            () => repo.MaxAcrossShardsAsync<TestEntity, int, decimal>(e => e.Amount));
        ex.Message.ShouldContain("IShardedAggregationSupport");
    }

    // ────────────────────────────────────────────────────────────
    //  Delegation — supported repository delegates correctly
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CountAcrossShardsAsync_SupportedRepository_DelegatesToImplementation()
    {
        // Arrange
        var repo = CreateSupportingRepository();
        var expected = CreateResult(42L);
        Expression<Func<TestEntity, bool>> predicate = e => e.IsActive;

        ((IShardedAggregationSupport<TestEntity, int>)repo)
            .CountAcrossShardsAsync(predicate, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, AggregationResult<long>>>(expected));

        // Act
        var result = await repo.CountAcrossShardsAsync(predicate);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r => r.Value.ShouldBe(42L),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task SumAcrossShardsAsync_SupportedRepository_DelegatesToImplementation()
    {
        // Arrange
        var repo = CreateSupportingRepository();
        var expected = CreateResult(500m);
        Expression<Func<TestEntity, decimal>> selector = e => e.Amount;

        ((IShardedAggregationSupport<TestEntity, int>)repo)
            .SumAcrossShardsAsync(selector, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, AggregationResult<decimal>>>(expected));

        // Act
        var result = await repo.SumAcrossShardsAsync(selector);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r => r.Value.ShouldBe(500m),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task AvgAcrossShardsAsync_SupportedRepository_DelegatesToImplementation()
    {
        // Arrange
        var repo = CreateSupportingRepository();
        var expected = CreateResult(25.5m);
        Expression<Func<TestEntity, decimal>> selector = e => e.Amount;

        ((IShardedAggregationSupport<TestEntity, int>)repo)
            .AvgAcrossShardsAsync(selector, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, AggregationResult<decimal>>>(expected));

        // Act
        var result = await repo.AvgAcrossShardsAsync(selector);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r => r.Value.ShouldBe(25.5m),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task MinAcrossShardsAsync_SupportedRepository_DelegatesToImplementation()
    {
        // Arrange
        var repo = CreateSupportingRepository();
        var expected = CreateResult<decimal?>(1.5m);
        Expression<Func<TestEntity, decimal>> selector = e => e.Amount;

        ((IShardedAggregationSupport<TestEntity, int>)repo)
            .MinAcrossShardsAsync(selector, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, AggregationResult<decimal?>>>(expected));

        // Act
        var result = await repo.MinAcrossShardsAsync(selector);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r => r.Value.ShouldBe(1.5m),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task MaxAcrossShardsAsync_SupportedRepository_DelegatesToImplementation()
    {
        // Arrange
        var repo = CreateSupportingRepository();
        var expected = CreateResult<decimal?>(999.99m);
        Expression<Func<TestEntity, decimal>> selector = e => e.Amount;

        ((IShardedAggregationSupport<TestEntity, int>)repo)
            .MaxAcrossShardsAsync(selector, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, AggregationResult<decimal?>>>(expected));

        // Act
        var result = await repo.MaxAcrossShardsAsync(selector);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r => r.Value.ShouldBe(999.99m),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    // ────────────────────────────────────────────────────────────
    //  Error propagation
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CountAcrossShardsAsync_ProviderReturnsError_PropagatesError()
    {
        // Arrange
        var repo = CreateSupportingRepository();
        var error = EncinaErrors.Create(ShardingErrorCodes.AggregationFailed, "All shards failed");
        Expression<Func<TestEntity, bool>> predicate = e => e.IsActive;

        ((IShardedAggregationSupport<TestEntity, int>)repo)
            .CountAcrossShardsAsync(predicate, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, AggregationResult<long>>>(error));

        // Act
        var result = await repo.CountAcrossShardsAsync(predicate);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: e => e.Message.ShouldContain("All shards failed"));
    }

    // ────────────────────────────────────────────────────────────
    //  Optional predicate — Sum/Avg/Min/Max accept null predicate
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SumAcrossShardsAsync_NullPredicate_IsValid()
    {
        // Arrange
        var repo = CreateSupportingRepository();
        var expected = CreateResult(100m);
        Expression<Func<TestEntity, decimal>> selector = e => e.Amount;

        ((IShardedAggregationSupport<TestEntity, int>)repo)
            .SumAcrossShardsAsync(selector, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, AggregationResult<decimal>>>(expected));

        // Act
        var result = await repo.SumAcrossShardsAsync(selector, predicate: null);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    // ────────────────────────────────────────────────────────────
    //  Helper to create a mock that implements both interfaces
    // ────────────────────────────────────────────────────────────

    private static IFunctionalShardedRepository<TestEntity, int> CreateSupportingRepository()
    {
        // Create a substitute that implements both interfaces
        return Substitute.For<IFunctionalShardedRepository<TestEntity, int>,
            IShardedAggregationSupport<TestEntity, int>>();
    }
}
