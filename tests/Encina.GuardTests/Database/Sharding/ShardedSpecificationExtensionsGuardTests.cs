using System.Linq.Expressions;
using Encina.DomainModeling;
using Encina.DomainModeling.Sharding;
using Encina.Sharding;
using NSubstitute;

namespace Encina.GuardTests.Database.Sharding;

/// <summary>
/// Guard clause tests for <see cref="ShardedSpecificationExtensions"/> and <see cref="ShardedPaginationOptions"/>.
/// </summary>
public sealed class ShardedSpecificationExtensionsGuardTests
{
    #region QueryAllShardsAsync Guards

    [Fact]
    public void QueryAllShardsAsync_NullRepository_ThrowsArgumentNullException()
    {
        IFunctionalShardedRepository<SpecExtGuardTestEntity, int> repository = null!;
        var specification = new SpecExtGuardTestSpec();

        var ex = Should.Throw<ArgumentNullException>(
            () => repository.QueryAllShardsAsync(specification));
        ex.ParamName.ShouldBe("repository");
    }

    [Fact]
    public void QueryAllShardsAsync_NullSpecification_ThrowsArgumentNullException()
    {
        var repository = CreateMockRepo();
        Specification<SpecExtGuardTestEntity> specification = null!;

        var ex = Should.Throw<ArgumentNullException>(
            () => repository.QueryAllShardsAsync(specification));
        ex.ParamName.ShouldBe("specification");
    }

    #endregion

    #region QueryAllShardsPagedAsync Guards

    [Fact]
    public void QueryAllShardsPagedAsync_NullRepository_ThrowsArgumentNullException()
    {
        IFunctionalShardedRepository<SpecExtGuardTestEntity, int> repository = null!;
        var specification = new SpecExtGuardTestSpec();
        var pagination = new ShardedPaginationOptions();

        var ex = Should.Throw<ArgumentNullException>(
            () => repository.QueryAllShardsPagedAsync(specification, pagination));
        ex.ParamName.ShouldBe("repository");
    }

    [Fact]
    public void QueryAllShardsPagedAsync_NullSpecification_ThrowsArgumentNullException()
    {
        var repository = CreateMockRepo();
        var pagination = new ShardedPaginationOptions();

        var ex = Should.Throw<ArgumentNullException>(
            () => repository.QueryAllShardsPagedAsync(null!, pagination));
        ex.ParamName.ShouldBe("specification");
    }

    [Fact]
    public void QueryAllShardsPagedAsync_NullPagination_ThrowsArgumentNullException()
    {
        var repository = CreateMockRepo();
        var specification = new SpecExtGuardTestSpec();

        var ex = Should.Throw<ArgumentNullException>(
            () => repository.QueryAllShardsPagedAsync(specification, null!));
        ex.ParamName.ShouldBe("pagination");
    }

    #endregion

    #region CountAllShardsAsync Guards

    [Fact]
    public void CountAllShardsAsync_NullRepository_ThrowsArgumentNullException()
    {
        IFunctionalShardedRepository<SpecExtGuardTestEntity, int> repository = null!;
        var specification = new SpecExtGuardTestSpec();

        var ex = Should.Throw<ArgumentNullException>(
            () => repository.CountAllShardsAsync(specification));
        ex.ParamName.ShouldBe("repository");
    }

    [Fact]
    public void CountAllShardsAsync_NullSpecification_ThrowsArgumentNullException()
    {
        var repository = CreateMockRepo();

        var ex = Should.Throw<ArgumentNullException>(
            () => repository.CountAllShardsAsync(null!));
        ex.ParamName.ShouldBe("specification");
    }

    #endregion

    #region QueryShardsAsync Guards

    [Fact]
    public void QueryShardsAsync_NullRepository_ThrowsArgumentNullException()
    {
        IFunctionalShardedRepository<SpecExtGuardTestEntity, int> repository = null!;
        var specification = new SpecExtGuardTestSpec();
        IReadOnlyList<string> shardIds = ["shard-1"];

        var ex = Should.Throw<ArgumentNullException>(
            () => repository.QueryShardsAsync(specification, shardIds));
        ex.ParamName.ShouldBe("repository");
    }

    [Fact]
    public void QueryShardsAsync_NullSpecification_ThrowsArgumentNullException()
    {
        var repository = CreateMockRepo();
        IReadOnlyList<string> shardIds = ["shard-1"];

        var ex = Should.Throw<ArgumentNullException>(
            () => repository.QueryShardsAsync(null!, shardIds));
        ex.ParamName.ShouldBe("specification");
    }

    [Fact]
    public void QueryShardsAsync_NullShardIds_ThrowsArgumentNullException()
    {
        var repository = CreateMockRepo();
        var specification = new SpecExtGuardTestSpec();

        var ex = Should.Throw<ArgumentNullException>(
            () => repository.QueryShardsAsync(specification, null!));
        ex.ParamName.ShouldBe("shardIds");
    }

    [Fact]
    public void QueryShardsAsync_EmptyShardIds_ThrowsArgumentException()
    {
        var repository = CreateMockRepo();
        var specification = new SpecExtGuardTestSpec();
        IReadOnlyList<string> shardIds = [];

        var ex = Should.Throw<ArgumentException>(
            () => repository.QueryShardsAsync(specification, shardIds));
        ex.ParamName.ShouldBe("shardIds");
    }

    #endregion

    #region ShardedPaginationOptions Guards

    [Fact]
    public void PaginationOptions_PageZero_ThrowsArgumentOutOfRangeException()
    {
        var options = new ShardedPaginationOptions();

        Should.Throw<ArgumentOutOfRangeException>(() => options.Page = 0);
    }

    [Fact]
    public void PaginationOptions_PageNegative_ThrowsArgumentOutOfRangeException()
    {
        var options = new ShardedPaginationOptions();

        Should.Throw<ArgumentOutOfRangeException>(() => options.Page = -1);
    }

    [Fact]
    public void PaginationOptions_PageSizeZero_ThrowsArgumentOutOfRangeException()
    {
        var options = new ShardedPaginationOptions();

        Should.Throw<ArgumentOutOfRangeException>(() => options.PageSize = 0);
    }

    [Fact]
    public void PaginationOptions_PageSizeNegative_ThrowsArgumentOutOfRangeException()
    {
        var options = new ShardedPaginationOptions();

        Should.Throw<ArgumentOutOfRangeException>(() => options.PageSize = -5);
    }

    #endregion

    #region Helpers

    private static IFunctionalShardedRepository<SpecExtGuardTestEntity, int> CreateMockRepo()
        => Substitute.For<IFunctionalShardedRepository<SpecExtGuardTestEntity, int>>();

    #endregion
}

/// <summary>
/// Test entity for <see cref="ShardedSpecificationExtensionsGuardTests"/>.
/// Must be public for NSubstitute's Castle DynamicProxy to create proxies.
/// </summary>
public sealed class SpecExtGuardTestEntity
{
    public int Id { get; init; }
}

/// <summary>
/// Test specification for <see cref="ShardedSpecificationExtensionsGuardTests"/>.
/// Must be public for NSubstitute's Castle DynamicProxy to create proxies.
/// </summary>
public sealed class SpecExtGuardTestSpec : Specification<SpecExtGuardTestEntity>
{
    public override Expression<Func<SpecExtGuardTestEntity, bool>> ToExpression()
        => e => true;
}
