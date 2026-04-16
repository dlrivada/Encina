using Encina.DomainModeling;
using Shouldly;

namespace Encina.UnitTests.DomainModeling.Pagination;

/// <summary>
/// Unit tests for <see cref="PaginationOptions"/>.
/// </summary>
public class PaginationOptionsTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithDefaults_ShouldHaveCorrectValues()
    {
        // Act
        var options = new PaginationOptions();

        // Assert
        options.PageNumber.ShouldBe(1);
        options.PageSize.ShouldBe(20);
    }

    [Fact]
    public void Constructor_WithCustomValues_ShouldSetProperties()
    {
        // Act
        var options = new PaginationOptions(PageNumber: 5, PageSize: 50);

        // Assert
        options.PageNumber.ShouldBe(5);
        options.PageSize.ShouldBe(50);
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(10, 100)]
    [InlineData(1, 1)]
    [InlineData(100, 1000)]
    public void Constructor_WithVariousValues_ShouldSetCorrectly(int pageNumber, int pageSize)
    {
        // Act
        var options = new PaginationOptions(pageNumber, pageSize);

        // Assert
        options.PageNumber.ShouldBe(pageNumber);
        options.PageSize.ShouldBe(pageSize);
    }

    #endregion

    #region Skip Calculation Tests

    [Theory]
    [InlineData(1, 10, 0)]      // Page 1, size 10 = skip 0
    [InlineData(2, 10, 10)]     // Page 2, size 10 = skip 10
    [InlineData(3, 10, 20)]     // Page 3, size 10 = skip 20
    [InlineData(1, 20, 0)]      // Page 1, size 20 = skip 0
    [InlineData(2, 20, 20)]     // Page 2, size 20 = skip 20
    [InlineData(5, 20, 80)]     // Page 5, size 20 = skip 80
    [InlineData(1, 1, 0)]       // Page 1, size 1 = skip 0
    [InlineData(10, 1, 9)]      // Page 10, size 1 = skip 9
    [InlineData(1, 100, 0)]     // Page 1, size 100 = skip 0
    [InlineData(5, 100, 400)]   // Page 5, size 100 = skip 400
    public void Skip_ShouldCalculateCorrectly(int pageNumber, int pageSize, int expectedSkip)
    {
        // Act
        var options = new PaginationOptions(pageNumber, pageSize);

        // Assert
        options.Skip.ShouldBe(expectedSkip);
    }

    [Fact]
    public void Skip_Formula_ShouldBePageMinusOneTimesSize()
    {
        // Arrange
        var options = new PaginationOptions(PageNumber: 7, PageSize: 25);

        // Act & Assert
        options.Skip.ShouldBe((7 - 1) * 25);
        options.Skip.ShouldBe(150);
    }

    #endregion

    #region Default Property Tests

    [Fact]
    public void Default_ShouldReturnPage1Size20()
    {
        // Act
        var options = PaginationOptions.Default;

        // Assert
        options.PageNumber.ShouldBe(1);
        options.PageSize.ShouldBe(20);
    }

    [Fact]
    public void Default_ShouldBeSingletonInstance()
    {
        // Act
        var options1 = PaginationOptions.Default;
        var options2 = PaginationOptions.Default;

        // Assert
        options1.ShouldBeSameAs(options2);
    }

    [Fact]
    public void Default_Skip_ShouldBeZero()
    {
        // Act
        var options = PaginationOptions.Default;

        // Assert
        options.Skip.ShouldBe(0);
    }

    #endregion

    #region WithPage Builder Tests

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(100)]
    [InlineData(1000)]
    public void WithPage_WithValidPageNumber_ShouldReturnNewInstance(int pageNumber)
    {
        // Arrange
        var original = PaginationOptions.Default;

        // Act
        var result = original.WithPage(pageNumber);

        // Assert
        result.PageNumber.ShouldBe(pageNumber);
        result.PageSize.ShouldBe(original.PageSize); // Size unchanged
        result.ShouldNotBeSameAs(original);
    }

    [Fact]
    public void WithPage_ShouldPreservePageSize()
    {
        // Arrange
        var original = new PaginationOptions(PageNumber: 1, PageSize: 50);

        // Act
        var result = original.WithPage(10);

        // Assert
        result.PageNumber.ShouldBe(10);
        result.PageSize.ShouldBe(50);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void WithPage_WithInvalidPageNumber_ShouldThrowArgumentOutOfRangeException(int pageNumber)
    {
        // Arrange
        var options = PaginationOptions.Default;

        // Act & Assert
        Action action = () => options.WithPage(pageNumber);
        Should.Throw<ArgumentOutOfRangeException>(action);
    }

    #endregion

    #region WithSize Builder Tests

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void WithSize_WithValidPageSize_ShouldReturnNewInstance(int pageSize)
    {
        // Arrange
        var original = PaginationOptions.Default;

        // Act
        var result = original.WithSize(pageSize);

        // Assert
        result.PageSize.ShouldBe(pageSize);
        result.PageNumber.ShouldBe(original.PageNumber); // Page unchanged
        result.ShouldNotBeSameAs(original);
    }

    [Fact]
    public void WithSize_ShouldPreservePageNumber()
    {
        // Arrange
        var original = new PaginationOptions(PageNumber: 5, PageSize: 20);

        // Act
        var result = original.WithSize(100);

        // Assert
        result.PageNumber.ShouldBe(5);
        result.PageSize.ShouldBe(100);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void WithSize_WithInvalidPageSize_ShouldThrowArgumentOutOfRangeException(int pageSize)
    {
        // Arrange
        var options = PaginationOptions.Default;

        // Act & Assert
        Action action = () => options.WithSize(pageSize);
        Should.Throw<ArgumentOutOfRangeException>(action);
    }

    #endregion

    #region Fluent Builder Chain Tests

    [Fact]
    public void FluentBuilder_ChainedCalls_ShouldWorkCorrectly()
    {
        // Act
        var options = PaginationOptions.Default
            .WithPage(5)
            .WithSize(50);

        // Assert
        options.PageNumber.ShouldBe(5);
        options.PageSize.ShouldBe(50);
        options.Skip.ShouldBe(200); // (5-1) * 50 = 200
    }

    [Fact]
    public void FluentBuilder_ReversedChain_ShouldWorkCorrectly()
    {
        // Act
        var options = PaginationOptions.Default
            .WithSize(25)
            .WithPage(3);

        // Assert
        options.PageNumber.ShouldBe(3);
        options.PageSize.ShouldBe(25);
        options.Skip.ShouldBe(50); // (3-1) * 25 = 50
    }

    [Fact]
    public void FluentBuilder_MultiplePageChanges_ShouldUseLastValue()
    {
        // Act
        var options = PaginationOptions.Default
            .WithPage(2)
            .WithPage(5)
            .WithPage(10);

        // Assert
        options.PageNumber.ShouldBe(10);
    }

    [Fact]
    public void FluentBuilder_MultipleSizeChanges_ShouldUseLastValue()
    {
        // Act
        var options = PaginationOptions.Default
            .WithSize(10)
            .WithSize(50)
            .WithSize(100);

        // Assert
        options.PageSize.ShouldBe(100);
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        // Arrange
        var options1 = new PaginationOptions(5, 50);
        var options2 = new PaginationOptions(5, 50);

        // Assert
        options1.ShouldBe(options2);
        (options1 == options2).ShouldBeTrue();
    }

    [Fact]
    public void Equality_DifferentPageNumber_ShouldNotBeEqual()
    {
        // Arrange
        var options1 = new PaginationOptions(5, 50);
        var options2 = new PaginationOptions(6, 50);

        // Assert
        options1.ShouldNotBe(options2);
    }

    [Fact]
    public void Equality_DifferentPageSize_ShouldNotBeEqual()
    {
        // Arrange
        var options1 = new PaginationOptions(5, 50);
        var options2 = new PaginationOptions(5, 100);

        // Assert
        options1.ShouldNotBe(options2);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void LargePageNumber_ShouldCalculateSkipCorrectly()
    {
        // Arrange
        var options = new PaginationOptions(PageNumber: 10000, PageSize: 100);

        // Act & Assert
        options.Skip.ShouldBe(999900); // (10000-1) * 100
    }

    [Fact]
    public void LargePageSize_ShouldCalculateSkipCorrectly()
    {
        // Arrange
        var options = new PaginationOptions(PageNumber: 2, PageSize: 10000);

        // Act & Assert
        options.Skip.ShouldBe(10000); // (2-1) * 10000
    }

    [Fact]
    public void WithExpression_ShouldCreateNewInstance()
    {
        // Arrange
        var original = new PaginationOptions(1, 20);

        // Act
        var modified = original with { PageNumber = 5 };

        // Assert
        modified.PageNumber.ShouldBe(5);
        modified.PageSize.ShouldBe(20);
        original.PageNumber.ShouldBe(1); // Original unchanged
    }

    #endregion
}
