using Encina.DomainModeling;
using FluentAssertions;

namespace Encina.UnitTests.DomainModeling.Pagination;

/// <summary>
/// Unit tests for <see cref="SortedPaginationOptions"/>.
/// </summary>
public class SortedPaginationOptionsTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithDefaults_ShouldHaveCorrectValues()
    {
        // Act
        var options = new SortedPaginationOptions();

        // Assert
        options.PageNumber.Should().Be(1);
        options.PageSize.Should().Be(20);
        options.SortBy.Should().BeNull();
        options.SortDescending.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithAllParameters_ShouldSetProperties()
    {
        // Act
        var options = new SortedPaginationOptions(
            PageNumber: 3,
            PageSize: 50,
            SortBy: "CreatedAtUtc",
            SortDescending: true);

        // Assert
        options.PageNumber.Should().Be(3);
        options.PageSize.Should().Be(50);
        options.SortBy.Should().Be("CreatedAtUtc");
        options.SortDescending.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithSortByOnly_ShouldUseDefaultDirection()
    {
        // Act
        var options = new SortedPaginationOptions(SortBy: "Name");

        // Assert
        options.SortBy.Should().Be("Name");
        options.SortDescending.Should().BeFalse();
    }

    #endregion

    #region Inheritance Tests

    [Fact]
    public void SortedPaginationOptions_ShouldInheritFromPaginationOptions()
    {
        // Arrange
        var options = new SortedPaginationOptions(5, 25, "Name", true);

        // Assert
        options.Should().BeAssignableTo<PaginationOptions>();
    }

    [Fact]
    public void Skip_ShouldBeInheritedFromBase()
    {
        // Arrange
        var options = new SortedPaginationOptions(PageNumber: 3, PageSize: 20);

        // Assert
        options.Skip.Should().Be(40); // (3-1) * 20
    }

    #endregion

    #region Default Property Tests

    [Fact]
    public void Default_ShouldReturnCorrectValues()
    {
        // Act
        var options = SortedPaginationOptions.Default;

        // Assert
        options.PageNumber.Should().Be(1);
        options.PageSize.Should().Be(20);
        options.SortBy.Should().BeNull();
        options.SortDescending.Should().BeFalse();
    }

    [Fact]
    public void Default_ShouldBeSingletonInstance()
    {
        // Act
        var options1 = SortedPaginationOptions.Default;
        var options2 = SortedPaginationOptions.Default;

        // Assert
        options1.Should().BeSameAs(options2);
    }

    #endregion

    #region WithSort Builder Tests

    [Theory]
    [InlineData("Name")]
    [InlineData("CreatedAtUtc")]
    [InlineData("Id")]
    [InlineData("Customer.Name")]
    public void WithSort_WithValidPropertyName_ShouldSetSortBy(string sortBy)
    {
        // Arrange
        var original = SortedPaginationOptions.Default;

        // Act
        var result = original.WithSort(sortBy);

        // Assert
        result.SortBy.Should().Be(sortBy);
        result.SortDescending.Should().BeFalse();
    }

    [Fact]
    public void WithSort_WithDescendingTrue_ShouldSetSortDescending()
    {
        // Arrange
        var original = SortedPaginationOptions.Default;

        // Act
        var result = original.WithSort("Name", descending: true);

        // Assert
        result.SortBy.Should().Be("Name");
        result.SortDescending.Should().BeTrue();
    }

    [Fact]
    public void WithSort_WithDescendingFalse_ShouldSetSortAscending()
    {
        // Arrange
        var original = new SortedPaginationOptions(SortDescending: true);

        // Act
        var result = original.WithSort("Name", descending: false);

        // Assert
        result.SortBy.Should().Be("Name");
        result.SortDescending.Should().BeFalse();
    }

    [Fact]
    public void WithSort_ShouldPreserveOtherProperties()
    {
        // Arrange
        var original = new SortedPaginationOptions(PageNumber: 5, PageSize: 50);

        // Act
        var result = original.WithSort("Name", true);

        // Assert
        result.PageNumber.Should().Be(5);
        result.PageSize.Should().Be(50);
        result.SortBy.Should().Be("Name");
        result.SortDescending.Should().BeTrue();
    }

    [Fact]
    public void WithSort_NullSortBy_ShouldThrowArgumentException()
    {
        // Arrange
        var options = SortedPaginationOptions.Default;

        // Act & Assert
        var action = () => options.WithSort(null!);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithSort_EmptySortBy_ShouldThrowArgumentException()
    {
        // Arrange
        var options = SortedPaginationOptions.Default;

        // Act & Assert
        var action = () => options.WithSort(string.Empty);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithSort_WhitespaceSortBy_ShouldThrowArgumentException()
    {
        // Arrange
        var options = SortedPaginationOptions.Default;

        // Act & Assert
        var action = () => options.WithSort("   ");
        action.Should().Throw<ArgumentException>();
    }

    #endregion

    #region WithPage Builder Tests

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(100)]
    public void WithPage_WithValidPageNumber_ShouldReturnSortedPaginationOptions(int pageNumber)
    {
        // Arrange
        var original = new SortedPaginationOptions(
            PageNumber: 1,
            PageSize: 20,
            SortBy: "Name",
            SortDescending: true);

        // Act
        var result = original.WithPage(pageNumber);

        // Assert
        result.Should().BeOfType<SortedPaginationOptions>();
        result.PageNumber.Should().Be(pageNumber);
        result.SortBy.Should().Be("Name");
        result.SortDescending.Should().BeTrue();
    }

    [Fact]
    public void WithPage_ShouldPreserveSortingProperties()
    {
        // Arrange
        var original = new SortedPaginationOptions(
            PageNumber: 1,
            PageSize: 20,
            SortBy: "CreatedAtUtc",
            SortDescending: true);

        // Act
        var result = original.WithPage(5);

        // Assert
        result.SortBy.Should().Be("CreatedAtUtc");
        result.SortDescending.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void WithPage_WithInvalidPageNumber_ShouldThrowArgumentOutOfRangeException(int pageNumber)
    {
        // Arrange
        var options = SortedPaginationOptions.Default;

        // Act & Assert
        var action = () => options.WithPage(pageNumber);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region WithSize Builder Tests

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(1000)]
    public void WithSize_WithValidPageSize_ShouldReturnSortedPaginationOptions(int pageSize)
    {
        // Arrange
        var original = new SortedPaginationOptions(
            PageNumber: 3,
            PageSize: 20,
            SortBy: "Id",
            SortDescending: false);

        // Act
        var result = original.WithSize(pageSize);

        // Assert
        result.Should().BeOfType<SortedPaginationOptions>();
        result.PageSize.Should().Be(pageSize);
        result.SortBy.Should().Be("Id");
    }

    [Fact]
    public void WithSize_ShouldPreserveSortingProperties()
    {
        // Arrange
        var original = new SortedPaginationOptions(
            PageNumber: 2,
            PageSize: 20,
            SortBy: "UpdatedAtUtc",
            SortDescending: true);

        // Act
        var result = original.WithSize(100);

        // Assert
        result.PageNumber.Should().Be(2);
        result.SortBy.Should().Be("UpdatedAtUtc");
        result.SortDescending.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void WithSize_WithInvalidPageSize_ShouldThrowArgumentOutOfRangeException(int pageSize)
    {
        // Arrange
        var options = SortedPaginationOptions.Default;

        // Act & Assert
        var action = () => options.WithSize(pageSize);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region Fluent Builder Chain Tests

    [Fact]
    public void FluentBuilder_FullChain_ShouldWorkCorrectly()
    {
        // Act
        var options = SortedPaginationOptions.Default
            .WithPage(3)
            .WithSize(50)
            .WithSort("CreatedAtUtc", descending: true);

        // Assert
        options.PageNumber.Should().Be(3);
        options.PageSize.Should().Be(50);
        options.SortBy.Should().Be("CreatedAtUtc");
        options.SortDescending.Should().BeTrue();
        options.Skip.Should().Be(100); // (3-1) * 50
    }

    [Fact]
    public void FluentBuilder_SortFirst_ShouldWorkCorrectly()
    {
        // Act
        var options = SortedPaginationOptions.Default
            .WithSort("Name")
            .WithPage(2)
            .WithSize(25);

        // Assert
        options.SortBy.Should().Be("Name");
        options.PageNumber.Should().Be(2);
        options.PageSize.Should().Be(25);
    }

    [Fact]
    public void FluentBuilder_ChangeSortMultipleTimes_ShouldUseLastValue()
    {
        // Act
        var options = SortedPaginationOptions.Default
            .WithSort("Name", descending: false)
            .WithSort("CreatedAtUtc", descending: true)
            .WithSort("Id", descending: false);

        // Assert
        options.SortBy.Should().Be("Id");
        options.SortDescending.Should().BeFalse();
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        // Arrange
        var options1 = new SortedPaginationOptions(3, 50, "Name", true);
        var options2 = new SortedPaginationOptions(3, 50, "Name", true);

        // Assert
        options1.Should().Be(options2);
    }

    [Fact]
    public void Equality_DifferentSortBy_ShouldNotBeEqual()
    {
        // Arrange
        var options1 = new SortedPaginationOptions(3, 50, "Name", true);
        var options2 = new SortedPaginationOptions(3, 50, "Id", true);

        // Assert
        options1.Should().NotBe(options2);
    }

    [Fact]
    public void Equality_DifferentSortDirection_ShouldNotBeEqual()
    {
        // Arrange
        var options1 = new SortedPaginationOptions(3, 50, "Name", true);
        var options2 = new SortedPaginationOptions(3, 50, "Name", false);

        // Assert
        options1.Should().NotBe(options2);
    }

    [Fact]
    public void Equality_NullVsNonNullSortBy_ShouldNotBeEqual()
    {
        // Arrange
        var options1 = new SortedPaginationOptions(3, 50, null, false);
        var options2 = new SortedPaginationOptions(3, 50, "Name", false);

        // Assert
        options1.Should().NotBe(options2);
    }

    #endregion

    #region With Expression Tests

    [Fact]
    public void WithExpression_ShouldCreateNewInstance()
    {
        // Arrange
        var original = new SortedPaginationOptions(1, 20, "Name", false);

        // Act
        var modified = original with { SortBy = "Id", SortDescending = true };

        // Assert
        modified.SortBy.Should().Be("Id");
        modified.SortDescending.Should().BeTrue();
        modified.PageNumber.Should().Be(1);
        modified.PageSize.Should().Be(20);
        original.SortBy.Should().Be("Name"); // Original unchanged
    }

    #endregion

    #region Practical Usage Scenarios

    [Fact]
    public void Scenario_SortedProductList_ByPriceDescending()
    {
        // Act
        var options = SortedPaginationOptions.Default
            .WithSort("Price", descending: true)
            .WithSize(10);

        // Assert
        options.SortBy.Should().Be("Price");
        options.SortDescending.Should().BeTrue();
        options.PageSize.Should().Be(10);
    }

    [Fact]
    public void Scenario_SortedAuditLog_ByTimestampDescending()
    {
        // Act
        var options = new SortedPaginationOptions(
            PageNumber: 1,
            PageSize: 100,
            SortBy: "TimestampUtc",
            SortDescending: true);

        // Assert
        options.SortBy.Should().Be("TimestampUtc");
        options.SortDescending.Should().BeTrue();
        options.PageSize.Should().Be(100);
    }

    [Fact]
    public void Scenario_AlphabeticalUserList()
    {
        // Act
        var options = SortedPaginationOptions.Default
            .WithSort("LastName")
            .WithPage(1)
            .WithSize(25);

        // Assert
        options.SortBy.Should().Be("LastName");
        options.SortDescending.Should().BeFalse(); // Ascending for A-Z
    }

    #endregion
}
