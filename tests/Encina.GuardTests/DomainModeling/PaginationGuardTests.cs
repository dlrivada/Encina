using Encina.DomainModeling;

namespace Encina.GuardTests.DomainModeling;

/// <summary>
/// Guard tests for pagination types.
/// Tests only the guard clauses that exist in the actual implementation.
/// Note: Record constructors don't have validation - only builder methods do.
/// </summary>
public class PaginationGuardTests
{
    #region PaginationOptions.WithPage Guards

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void PaginationOptions_WithPage_InvalidValue_ThrowsArgumentOutOfRangeException(int page)
    {
        // Arrange
        var options = PaginationOptions.Default;

        // Act
        var act = () => options.WithPage(page);

        // Assert
        var ex = Should.Throw<ArgumentOutOfRangeException>(act);
        ex.ParamName.ShouldBe("pageNumber");
    }

    #endregion

    #region PaginationOptions.WithSize Guards

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void PaginationOptions_WithSize_InvalidValue_ThrowsArgumentOutOfRangeException(int size)
    {
        // Arrange
        var options = PaginationOptions.Default;

        // Act
        var act = () => options.WithSize(size);

        // Assert
        var ex = Should.Throw<ArgumentOutOfRangeException>(act);
        ex.ParamName.ShouldBe("pageSize");
    }

    #endregion

    #region SortedPaginationOptions.WithSort Guards

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SortedPaginationOptions_WithSort_InvalidSortBy_ThrowsArgumentException(string? sortBy)
    {
        // Arrange
        var options = SortedPaginationOptions.Default;

        // Act
        var act = () => options.WithSort(sortBy!, false);

        // Assert
        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("sortBy");
    }

    #endregion

    #region SortedPaginationOptions.WithPage Guards

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SortedPaginationOptions_WithPage_InvalidValue_ThrowsArgumentOutOfRangeException(int page)
    {
        // Arrange
        var options = SortedPaginationOptions.Default;

        // Act
        var act = () => options.WithPage(page);

        // Assert
        var ex = Should.Throw<ArgumentOutOfRangeException>(act);
        ex.ParamName.ShouldBe("pageNumber");
    }

    #endregion

    #region SortedPaginationOptions.WithSize Guards

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SortedPaginationOptions_WithSize_InvalidValue_ThrowsArgumentOutOfRangeException(int size)
    {
        // Arrange
        var options = SortedPaginationOptions.Default;

        // Act
        var act = () => options.WithSize(size);

        // Assert
        var ex = Should.Throw<ArgumentOutOfRangeException>(act);
        ex.ParamName.ShouldBe("pageSize");
    }

    #endregion

    #region PagedResult.Map Guards

    [Fact]
    public void PagedResult_Map_NullSelector_ThrowsArgumentNullException()
    {
        // Arrange
        var result = new PagedResult<int>([1, 2, 3], 1, 10, 100);

        // Act
        var act = () => result.Map<string>(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("selector");
    }

    #endregion

    #region PagedQuerySpecification Constructor Guards

    [Fact]
    public void PagedQuerySpecification_NullPagination_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new TestPagedSpec(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("pagination");
    }

    private sealed class TestPagedSpec : PagedQuerySpecification<TestEntity>
    {
        public TestPagedSpec(PaginationOptions pagination) : base(pagination)
        {
        }
    }

    private sealed class TestEntity
    {
        public int Id { get; init; }
    }

    #endregion
}
