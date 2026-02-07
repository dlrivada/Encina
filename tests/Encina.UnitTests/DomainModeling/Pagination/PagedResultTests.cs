using System.Globalization;
using Encina.DomainModeling;
using FluentAssertions;

namespace Encina.UnitTests.DomainModeling.Pagination;

/// <summary>
/// Unit tests for <see cref="PagedResult{T}"/> in DomainModeling.
/// </summary>
public class PagedResultTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        // Arrange
        var items = new[] { "a", "b", "c" };

        // Act
        var result = new PagedResult<string>(items, PageNumber: 2, PageSize: 10, TotalCount: 50);

        // Assert
        result.Items.Should().BeEquivalentTo(items);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(50);
    }

    [Fact]
    public void Constructor_WithEmptyItems_ShouldWork()
    {
        // Act
        var result = new PagedResult<string>([], PageNumber: 1, PageSize: 10, TotalCount: 0);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    #endregion

    #region TotalPages Tests

    [Theory]
    [InlineData(0, 10, 0)]      // No items = 0 pages
    [InlineData(1, 10, 1)]      // 1 item = 1 page
    [InlineData(10, 10, 1)]     // Exactly 1 page
    [InlineData(11, 10, 2)]     // 11 items = 2 pages
    [InlineData(20, 10, 2)]     // Exactly 2 pages
    [InlineData(21, 10, 3)]     // 21 items = 3 pages
    [InlineData(100, 10, 10)]   // 100 items = 10 pages
    [InlineData(101, 10, 11)]   // 101 items = 11 pages
    [InlineData(95, 10, 10)]    // 95 items = 10 pages (last page partial)
    [InlineData(1000, 100, 10)] // 1000 items, 100 per page = 10 pages
    public void TotalPages_ShouldCalculateCorrectly(int totalCount, int pageSize, int expectedPages)
    {
        // Act
        var result = new PagedResult<string>([], 1, pageSize, totalCount);

        // Assert
        result.TotalPages.Should().Be(expectedPages);
    }

    [Fact]
    public void TotalPages_WithZeroPageSize_ShouldReturnZero()
    {
        // Act - Edge case: PageSize should normally be > 0
        var result = new PagedResult<string>([], 1, 0, 100);

        // Assert - Division by zero protection
        result.TotalPages.Should().Be(0);
    }

    #endregion

    #region HasPreviousPage Tests

    [Theory]
    [InlineData(1, false)]   // First page has no previous
    [InlineData(2, true)]    // Second page has previous
    [InlineData(5, true)]    // Fifth page has previous
    [InlineData(100, true)]  // Any page > 1 has previous
    public void HasPreviousPage_ShouldBeCorrect(int pageNumber, bool expected)
    {
        // Act
        var result = new PagedResult<string>([], pageNumber, 10, 1000);

        // Assert
        result.HasPreviousPage.Should().Be(expected);
    }

    #endregion

    #region HasNextPage Tests

    [Fact]
    public void HasNextPage_OnFirstPageWithMorePages_ShouldBeTrue()
    {
        // Arrange - 100 items, page size 10 = 10 pages, on page 1
        var result = new PagedResult<string>([], 1, 10, 100);

        // Assert
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void HasNextPage_OnLastPage_ShouldBeFalse()
    {
        // Arrange - 100 items, page size 10 = 10 pages, on page 10
        var result = new PagedResult<string>([], 10, 10, 100);

        // Assert
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void HasNextPage_OnMiddlePage_ShouldBeTrue()
    {
        // Arrange - 100 items, page size 10 = 10 pages, on page 5
        var result = new PagedResult<string>([], 5, 10, 100);

        // Assert
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void HasNextPage_OnOnlyPage_ShouldBeFalse()
    {
        // Arrange - 5 items, page size 10 = 1 page
        var result = new PagedResult<string>([], 1, 10, 5);

        // Assert
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void HasNextPage_WithNoItems_ShouldBeFalse()
    {
        // Arrange
        var result = new PagedResult<string>([], 1, 10, 0);

        // Assert
        result.HasNextPage.Should().BeFalse();
    }

    #endregion

    #region IsEmpty Tests

    [Fact]
    public void IsEmpty_WithEmptyItems_ShouldBeTrue()
    {
        // Act
        var result = new PagedResult<string>([], 1, 10, 0);

        // Assert
        result.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void IsEmpty_WithItems_ShouldBeFalse()
    {
        // Act
        var result = new PagedResult<string>(["item"], 1, 10, 1);

        // Assert
        result.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void IsEmpty_EmptyPageButTotalCountPositive_ShouldBeTrue()
    {
        // This happens when requesting a page beyond the last page
        var result = new PagedResult<string>([], 20, 10, 100);

        // Assert - Based on current page items count
        result.IsEmpty.Should().BeTrue();
    }

    #endregion

    #region FirstItemIndex Tests

    [Theory]
    [InlineData(1, 10, 100, 1)]   // Page 1, size 10 = item 1
    [InlineData(2, 10, 100, 11)]  // Page 2, size 10 = item 11
    [InlineData(3, 10, 100, 21)]  // Page 3, size 10 = item 21
    [InlineData(1, 20, 100, 1)]   // Page 1, size 20 = item 1
    [InlineData(2, 20, 100, 21)]  // Page 2, size 20 = item 21
    [InlineData(5, 20, 100, 81)]  // Page 5, size 20 = item 81
    [InlineData(1, 1, 100, 1)]    // Page 1, size 1 = item 1
    [InlineData(10, 1, 100, 10)]  // Page 10, size 1 = item 10
    public void FirstItemIndex_ShouldCalculateCorrectly(int pageNumber, int pageSize, int totalCount, int expectedIndex)
    {
        // Act
        var result = new PagedResult<string>([], pageNumber, pageSize, totalCount);

        // Assert
        result.FirstItemIndex.Should().Be(expectedIndex);
    }

    [Fact]
    public void FirstItemIndex_WithEmptyResult_ShouldBeZero()
    {
        // Act
        var result = new PagedResult<string>([], 1, 10, 0);

        // Assert
        result.FirstItemIndex.Should().Be(0);
    }

    [Fact]
    public void FirstItemIndex_Formula_ShouldBePageMinusOneTimesSizePlusOne()
    {
        // Arrange
        var result = new PagedResult<string>(["item"], 5, 20, 100);

        // Act & Assert
        result.FirstItemIndex.Should().Be((5 - 1) * 20 + 1);
        result.FirstItemIndex.Should().Be(81);
    }

    #endregion

    #region LastItemIndex Tests

    [Theory]
    [InlineData(1, 10, 100, 10)]  // Page 1, size 10, 100 items = item 10
    [InlineData(2, 10, 100, 20)]  // Page 2, size 10, 100 items = item 20
    [InlineData(10, 10, 100, 100)] // Page 10, size 10, 100 items = item 100
    [InlineData(10, 10, 95, 95)]  // Page 10, size 10, 95 items = item 95 (last page partial)
    [InlineData(1, 20, 45, 20)]   // Page 1, size 20, 45 items = item 20
    [InlineData(2, 20, 45, 40)]   // Page 2, size 20, 45 items = item 40
    [InlineData(3, 20, 45, 45)]   // Page 3, size 20, 45 items = item 45 (last page partial)
    public void LastItemIndex_ShouldCalculateCorrectly(int pageNumber, int pageSize, int totalCount, int expectedIndex)
    {
        // Act
        var result = new PagedResult<string>([], pageNumber, pageSize, totalCount);

        // Assert
        result.LastItemIndex.Should().Be(expectedIndex);
    }

    [Fact]
    public void LastItemIndex_WithEmptyResult_ShouldBeZero()
    {
        // Act
        var result = new PagedResult<string>([], 1, 10, 0);

        // Assert
        result.LastItemIndex.Should().Be(0);
    }

    [Fact]
    public void LastItemIndex_ShouldNotExceedTotalCount()
    {
        // Arrange - Last page with fewer items than page size
        var result = new PagedResult<string>([], 5, 20, 92);

        // Act & Assert
        // Page 5 would normally show items 81-100, but total is only 92
        result.LastItemIndex.Should().Be(92);
        result.LastItemIndex.Should().BeLessThanOrEqualTo(result.TotalCount);
    }

    #endregion

    #region Empty Factory Method Tests

    [Fact]
    public void Empty_ShouldCreateEmptyResult()
    {
        // Act
        var result = PagedResult<string>.Empty();

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Empty_WithDefaultParameters_ShouldHaveCorrectPagination()
    {
        // Act
        var result = PagedResult<string>.Empty();

        // Assert
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public void Empty_WithCustomPageNumber_ShouldUseIt()
    {
        // Act
        var result = PagedResult<string>.Empty(pageNumber: 5);

        // Assert
        result.PageNumber.Should().Be(5);
    }

    [Fact]
    public void Empty_WithCustomPageSize_ShouldUseIt()
    {
        // Act
        var result = PagedResult<string>.Empty(pageSize: 100);

        // Assert
        result.PageSize.Should().Be(100);
    }

    [Fact]
    public void Empty_WithCustomValues_ShouldUseAll()
    {
        // Act
        var result = PagedResult<string>.Empty(pageNumber: 3, pageSize: 25);

        // Assert
        result.PageNumber.Should().Be(3);
        result.PageSize.Should().Be(25);
    }

    #endregion

    #region Map Method Tests

    [Fact]
    public void Map_ShouldTransformItems()
    {
        // Arrange
        var items = new[] { 1, 2, 3 };
        var result = new PagedResult<int>(items, 1, 10, 100);

        // Act
        var mapped = result.Map(x => x * 2);

        // Assert
        mapped.Items.Should().BeEquivalentTo([2, 4, 6]);
    }

    [Fact]
    public void Map_ShouldPreservePaginationMetadata()
    {
        // Arrange
        var result = new PagedResult<int>([1, 2, 3], 2, 10, 50);

        // Act
        var mapped = result.Map(x => x.ToString(CultureInfo.InvariantCulture));

        // Assert
        mapped.PageNumber.Should().Be(2);
        mapped.PageSize.Should().Be(10);
        mapped.TotalCount.Should().Be(50);
        mapped.TotalPages.Should().Be(5);
    }

    [Fact]
    public void Map_WithEmptyItems_ShouldReturnEmptyResult()
    {
        // Arrange
        var result = new PagedResult<int>([], 1, 10, 0);

        // Act
        var mapped = result.Map(x => x.ToString(CultureInfo.InvariantCulture));

        // Assert
        mapped.Items.Should().BeEmpty();
        mapped.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Map_WithNullSelector_ShouldThrowArgumentNullException()
    {
        // Arrange
        var result = new PagedResult<int>([1], 1, 10, 1);

        // Act & Assert
        var action = () => result.Map<string>(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Map_ToComplexType_ShouldWork()
    {
        // Arrange
        var items = new[] { 1, 2, 3 };
        var result = new PagedResult<int>(items, 1, 10, 3);

        // Act
        var mapped = result.Map(x => new { Id = x, Name = $"Item-{x}" });

        // Assert
        mapped.Items.Should().HaveCount(3);
        mapped.Items[0].Id.Should().Be(1);
        mapped.Items[0].Name.Should().Be("Item-1");
    }

    #endregion

    #region Navigation Scenario Tests

    [Fact]
    public void NavigationScenario_FirstPage()
    {
        // Arrange - 95 items, page size 10, on page 1
        var items = Enumerable.Range(1, 10).Select(i => $"item-{i}").ToArray();
        var result = new PagedResult<string>(items, 1, 10, 95);

        // Assert
        result.TotalPages.Should().Be(10);
        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeTrue();
        result.FirstItemIndex.Should().Be(1);
        result.LastItemIndex.Should().Be(10);
    }

    [Fact]
    public void NavigationScenario_MiddlePage()
    {
        // Arrange - 95 items, page size 10, on page 5
        var items = Enumerable.Range(41, 10).Select(i => $"item-{i}").ToArray();
        var result = new PagedResult<string>(items, 5, 10, 95);

        // Assert
        result.TotalPages.Should().Be(10);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
        result.FirstItemIndex.Should().Be(41);
        result.LastItemIndex.Should().Be(50);
    }

    [Fact]
    public void NavigationScenario_LastPage()
    {
        // Arrange - 95 items, page size 10, on page 10 (only 5 items)
        var items = Enumerable.Range(91, 5).Select(i => $"item-{i}").ToArray();
        var result = new PagedResult<string>(items, 10, 10, 95);

        // Assert
        result.TotalPages.Should().Be(10);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeFalse();
        result.FirstItemIndex.Should().Be(91);
        result.LastItemIndex.Should().Be(95);
    }

    [Fact]
    public void NavigationScenario_SinglePage()
    {
        // Arrange - 7 items, page size 10, only 1 page
        var items = Enumerable.Range(1, 7).Select(i => $"item-{i}").ToArray();
        var result = new PagedResult<string>(items, 1, 10, 7);

        // Assert
        result.TotalPages.Should().Be(1);
        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeFalse();
        result.FirstItemIndex.Should().Be(1);
        result.LastItemIndex.Should().Be(7);
    }

    [Fact]
    public void NavigationScenario_DisplayText()
    {
        // Arrange - Typical "Showing X-Y of Z" display
        var items = Enumerable.Range(11, 10).ToArray();
        var result = new PagedResult<int>(items, 2, 10, 45);

        // Act
        var displayText = $"Showing {result.FirstItemIndex}-{result.LastItemIndex} of {result.TotalCount}";

        // Assert
        displayText.Should().Be("Showing 11-20 of 45");
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Equality_SameReference_ShouldBeEqual()
    {
        // Arrange
        var items = new List<int> { 1, 2, 3 };
        var result1 = new PagedResult<int>(items, 1, 10, 100);
        var result2 = new PagedResult<int>(items, 1, 10, 100);

        // Assert - Same Items reference means records are equal
        result1.Should().Be(result2);
        (result1 == result2).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentItemsReference_ShouldNotBeEqual()
    {
        // Arrange - Different array instances with same values
        // Records compare reference equality for IReadOnlyList
        var result1 = new PagedResult<int>([1, 2, 3], 1, 10, 100);
        var result2 = new PagedResult<int>([1, 2, 3], 1, 10, 100);

        // Assert - Different Items reference means records are not equal
        result1.Should().NotBe(result2);
    }

    [Fact]
    public void Equality_DifferentItems_ShouldNotBeEqual()
    {
        // Arrange
        var result1 = new PagedResult<int>([1, 2, 3], 1, 10, 100);
        var result2 = new PagedResult<int>([4, 5, 6], 1, 10, 100);

        // Assert
        result1.Should().NotBe(result2);
    }

    [Fact]
    public void BeEquivalentTo_SameValues_ShouldSucceed()
    {
        // Arrange - For structural comparison, use BeEquivalentTo
        var result1 = new PagedResult<int>([1, 2, 3], 1, 10, 100);
        var result2 = new PagedResult<int>([1, 2, 3], 1, 10, 100);

        // Assert - Structural equality comparison
        result1.Should().BeEquivalentTo(result2);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void LargeTotalCount_ShouldCalculateCorrectly()
    {
        // Arrange - 1 million items
        var result = new PagedResult<string>([], 1, 100, 1_000_000);

        // Assert
        result.TotalPages.Should().Be(10_000);
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void PageBeyondTotal_ShouldReturnEmptyWithCorrectMetadata()
    {
        // Arrange - Requesting page 20 when only 10 pages exist
        var result = new PagedResult<string>([], 20, 10, 95);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalPages.Should().Be(10);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeFalse(); // Page 20 > TotalPages (10)
    }

    #endregion
}
