using Encina.Security.Audit;
using FluentAssertions;

namespace Encina.UnitTests.Security.Audit;

/// <summary>
/// Unit tests for <see cref="PagedResult{T}"/>.
/// </summary>
public class PagedResultTests
{
    #region TotalPages Calculation Tests

    [Theory]
    [InlineData(0, 10, 0)]     // No items = 0 pages
    [InlineData(1, 10, 1)]     // 1 item with page size 10 = 1 page
    [InlineData(10, 10, 1)]    // Exactly 1 page
    [InlineData(11, 10, 2)]    // 11 items needs 2 pages
    [InlineData(20, 10, 2)]    // Exactly 2 pages
    [InlineData(21, 10, 3)]    // 21 items needs 3 pages
    [InlineData(100, 10, 10)]  // 100 items with 10 per page = 10 pages
    [InlineData(101, 10, 11)]  // 101 items needs 11 pages
    [InlineData(50, 50, 1)]    // 50 items with 50 per page = 1 page
    [InlineData(1000, 100, 10)] // 1000 items with 100 per page = 10 pages
    public void TotalPages_ShouldCalculateCorrectly(int totalCount, int pageSize, int expectedPages)
    {
        // Act
        var result = PagedResult<string>.Create([], totalCount, 1, pageSize);

        // Assert
        result.TotalPages.Should().Be(expectedPages);
    }

    [Fact]
    public void TotalPages_WithZeroPageSize_ShouldReturnZero()
    {
        // Act - This is an edge case; normally PageSize should be > 0
        var result = new PagedResult<string>
        {
            Items = [],
            TotalCount = 100,
            PageNumber = 1,
            PageSize = 0
        };

        // Assert - Division by zero protection
        result.TotalPages.Should().Be(0);
    }

    #endregion

    #region HasPreviousPage Tests

    [Theory]
    [InlineData(1, false)]  // First page has no previous
    [InlineData(2, true)]   // Second page has previous
    [InlineData(5, true)]   // Fifth page has previous
    [InlineData(100, true)] // Any page > 1 has previous
    public void HasPreviousPage_ShouldBeCorrect(int pageNumber, bool expected)
    {
        // Act
        var result = PagedResult<string>.Create([], 1000, pageNumber, 10);

        // Assert
        result.HasPreviousPage.Should().Be(expected);
    }

    #endregion

    #region HasNextPage Tests

    [Fact]
    public void HasNextPage_OnFirstPageWithMorePages_ShouldBeTrue()
    {
        // Act - Total 100 items, page size 10 = 10 pages, on page 1
        var result = PagedResult<string>.Create([], 100, 1, 10);

        // Assert
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void HasNextPage_OnLastPage_ShouldBeFalse()
    {
        // Act - Total 100 items, page size 10 = 10 pages, on page 10
        var result = PagedResult<string>.Create([], 100, 10, 10);

        // Assert
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void HasNextPage_OnMiddlePage_ShouldBeTrue()
    {
        // Act - Total 100 items, page size 10 = 10 pages, on page 5
        var result = PagedResult<string>.Create([], 100, 5, 10);

        // Assert
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void HasNextPage_OnOnlyPage_ShouldBeFalse()
    {
        // Act - Total 5 items, page size 10 = 1 page, on page 1
        var result = PagedResult<string>.Create([], 5, 1, 10);

        // Assert
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void HasNextPage_WithNoItems_ShouldBeFalse()
    {
        // Act
        var result = PagedResult<string>.Create([], 0, 1, 10);

        // Assert
        result.HasNextPage.Should().BeFalse();
    }

    #endregion

    #region Count Tests

    [Fact]
    public void Count_ShouldReturnItemsCount()
    {
        // Arrange
        var items = new[] { "a", "b", "c" };

        // Act
        var result = PagedResult<string>.Create(items, 100, 1, 10);

        // Assert
        result.Count.Should().Be(3);
    }

    [Fact]
    public void Count_WithEmptyItems_ShouldReturnZero()
    {
        // Act
        var result = PagedResult<string>.Create([], 0, 1, 10);

        // Assert
        result.Count.Should().Be(0);
    }

    #endregion

    #region IsEmpty Tests

    [Fact]
    public void IsEmpty_WhenTotalCountIsZero_ShouldBeTrue()
    {
        // Act
        var result = PagedResult<string>.Create([], 0, 1, 10);

        // Assert
        result.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void IsEmpty_WhenTotalCountIsGreaterThanZero_ShouldBeFalse()
    {
        // Act
        var result = PagedResult<string>.Create(["item"], 1, 1, 10);

        // Assert
        result.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void IsEmpty_WhenItemsEmptyButTotalCountPositive_ShouldBeFalse()
    {
        // This can happen when requesting a page beyond the last page
        var result = PagedResult<string>.Create([], 100, 20, 10);

        // Assert - Based on TotalCount, not current page items
        result.IsEmpty.Should().BeFalse();
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
    public void Empty_WithDefaults_ShouldHaveCorrectPagination()
    {
        // Act
        var result = PagedResult<string>.Empty();

        // Assert
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(AuditQuery.DefaultPageSize);
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

    #region Create Factory Method Tests

    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        // Arrange
        var items = new[] { "a", "b", "c" };

        // Act
        var result = PagedResult<string>.Create(items, 100, 2, 25);

        // Assert
        result.Items.Should().BeEquivalentTo(items);
        result.TotalCount.Should().Be(100);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(25);
    }

    [Fact]
    public void Create_WithDifferentTypes_ShouldWork()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var auditEntries = new[]
        {
            new AuditEntry
            {
                Id = Guid.NewGuid(),
                Action = "Create",
                EntityType = "Order",
                CorrelationId = "c1",
                Outcome = AuditOutcome.Success,
                TimestampUtc = DateTime.UtcNow,
                StartedAtUtc = now,
                CompletedAtUtc = now,
                Metadata = new Dictionary<string, object?>()
            },
            new AuditEntry
            {
                Id = Guid.NewGuid(),
                Action = "Update",
                EntityType = "Order",
                CorrelationId = "c2",
                Outcome = AuditOutcome.Success,
                TimestampUtc = DateTime.UtcNow,
                StartedAtUtc = now,
                CompletedAtUtc = now,
                Metadata = new Dictionary<string, object?>()
            }
        };

        // Act
        var result = PagedResult<AuditEntry>.Create(auditEntries, 50, 1, 10);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(50);
    }

    #endregion

    #region Navigation Scenario Tests

    [Fact]
    public void NavigationScenario_FirstPage()
    {
        // Arrange - 95 items, page size 10, on page 1
        var result = PagedResult<string>.Create(Enumerable.Range(1, 10).Select(i => $"item-{i}").ToArray(), 95, 1, 10);

        // Assert
        result.TotalPages.Should().Be(10);
        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeTrue();
        result.Count.Should().Be(10);
    }

    [Fact]
    public void NavigationScenario_MiddlePage()
    {
        // Arrange - 95 items, page size 10, on page 5
        var result = PagedResult<string>.Create(Enumerable.Range(41, 10).Select(i => $"item-{i}").ToArray(), 95, 5, 10);

        // Assert
        result.TotalPages.Should().Be(10);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
        result.Count.Should().Be(10);
    }

    [Fact]
    public void NavigationScenario_LastPage()
    {
        // Arrange - 95 items, page size 10, on page 10 (only 5 items)
        var result = PagedResult<string>.Create(Enumerable.Range(91, 5).Select(i => $"item-{i}").ToArray(), 95, 10, 10);

        // Assert
        result.TotalPages.Should().Be(10);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeFalse();
        result.Count.Should().Be(5); // Last page has fewer items
    }

    [Fact]
    public void NavigationScenario_SinglePage()
    {
        // Arrange - 7 items, page size 10, only 1 page
        var result = PagedResult<string>.Create(Enumerable.Range(1, 7).Select(i => $"item-{i}").ToArray(), 7, 1, 10);

        // Assert
        result.TotalPages.Should().Be(1);
        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeFalse();
        result.Count.Should().Be(7);
    }

    #endregion
}
