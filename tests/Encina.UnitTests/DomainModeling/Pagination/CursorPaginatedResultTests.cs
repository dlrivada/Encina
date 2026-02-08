using System.Globalization;
using Encina.DomainModeling.Pagination;
using FluentAssertions;

namespace Encina.UnitTests.DomainModeling.Pagination;

/// <summary>
/// Unit tests for <see cref="CursorPaginatedResult{T}"/>.
/// </summary>
public class CursorPaginatedResultTests
{
    #region Constructor and Properties Tests

    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        // Arrange
        var items = new[] { "a", "b", "c" };

        // Act
        var result = new CursorPaginatedResult<string>
        {
            Items = items,
            NextCursor = "next123",
            PreviousCursor = "prev456",
            HasNextPage = true,
            HasPreviousPage = true,
            TotalCount = 100
        };

        // Assert
        result.Items.Should().BeEquivalentTo(items);
        result.NextCursor.Should().Be("next123");
        result.PreviousCursor.Should().Be("prev456");
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeTrue();
        result.TotalCount.Should().Be(100);
    }

    [Fact]
    public void TotalCount_WhenNull_ShouldBeNull()
    {
        // Arrange & Act
        var result = new CursorPaginatedResult<string>
        {
            Items = ["item"],
            TotalCount = null
        };

        // Assert
        result.TotalCount.Should().BeNull();
    }

    #endregion

    #region IsEmpty Tests

    [Fact]
    public void IsEmpty_WithEmptyItems_ShouldBeTrue()
    {
        // Arrange & Act
        var result = new CursorPaginatedResult<string>
        {
            Items = [],
            HasNextPage = false,
            HasPreviousPage = false
        };

        // Assert
        result.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void IsEmpty_WithItems_ShouldBeFalse()
    {
        // Arrange & Act
        var result = new CursorPaginatedResult<string>
        {
            Items = ["item"],
            HasNextPage = false,
            HasPreviousPage = false
        };

        // Assert
        result.IsEmpty.Should().BeFalse();
    }

    #endregion

    #region Empty Factory Method Tests

    [Fact]
    public void Empty_ShouldCreateEmptyResult()
    {
        // Act
        var result = CursorPaginatedResult<string>.Empty();

        // Assert
        result.Items.Should().BeEmpty();
        result.NextCursor.Should().BeNull();
        result.PreviousCursor.Should().BeNull();
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeFalse();
        result.TotalCount.Should().Be(0);
        result.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Empty_WithCustomPageSize_ShouldUseIt()
    {
        // Act
        var result = CursorPaginatedResult<string>.Empty(pageSize: 50);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    #endregion

    #region Map Method Tests

    [Fact]
    public void Map_ShouldTransformItems()
    {
        // Arrange
        var result = new CursorPaginatedResult<int>
        {
            Items = [1, 2, 3],
            NextCursor = "next",
            PreviousCursor = "prev",
            HasNextPage = true,
            HasPreviousPage = true,
            TotalCount = 100
        };

        // Act
        var mapped = result.Map(x => x * 2);

        // Assert
        mapped.Items.Should().BeEquivalentTo([2, 4, 6]);
    }

    [Fact]
    public void Map_ShouldPreserveCursorsAndNavigation()
    {
        // Arrange
        var result = new CursorPaginatedResult<int>
        {
            Items = [1, 2, 3],
            NextCursor = "next123",
            PreviousCursor = "prev456",
            HasNextPage = true,
            HasPreviousPage = true,
            TotalCount = 50
        };

        // Act
        var mapped = result.Map(x => x.ToString(CultureInfo.InvariantCulture));

        // Assert
        mapped.NextCursor.Should().Be("next123");
        mapped.PreviousCursor.Should().Be("prev456");
        mapped.HasNextPage.Should().BeTrue();
        mapped.HasPreviousPage.Should().BeTrue();
        mapped.TotalCount.Should().Be(50);
    }

    [Fact]
    public void Map_WithEmptyItems_ShouldReturnEmptyResult()
    {
        // Arrange
        var result = new CursorPaginatedResult<int>
        {
            Items = [],
            HasNextPage = false,
            HasPreviousPage = false
        };

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
        var result = new CursorPaginatedResult<int>
        {
            Items = [1],
            HasNextPage = false,
            HasPreviousPage = false
        };

        // Act & Assert
        var action = () => result.Map<string>(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Map_ToComplexType_ShouldWork()
    {
        // Arrange
        var result = new CursorPaginatedResult<int>
        {
            Items = [1, 2, 3],
            NextCursor = "cursor",
            HasNextPage = true,
            HasPreviousPage = false
        };

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
        // Arrange - First page with more pages available
        var result = new CursorPaginatedResult<string>
        {
            Items = ["item1", "item2", "item3"],
            NextCursor = "cursor_after_3",
            PreviousCursor = null,
            HasNextPage = true,
            HasPreviousPage = false
        };

        // Assert
        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeTrue();
        result.PreviousCursor.Should().BeNull();
        result.NextCursor.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void NavigationScenario_MiddlePage()
    {
        // Arrange - Middle page with both navigation directions
        var result = new CursorPaginatedResult<string>
        {
            Items = ["item4", "item5", "item6"],
            NextCursor = "cursor_after_6",
            PreviousCursor = "cursor_before_4",
            HasNextPage = true,
            HasPreviousPage = true
        };

        // Assert
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
        result.PreviousCursor.Should().NotBeNullOrEmpty();
        result.NextCursor.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void NavigationScenario_LastPage()
    {
        // Arrange - Last page with no next page
        var result = new CursorPaginatedResult<string>
        {
            Items = ["item10", "item11"],
            NextCursor = null,
            PreviousCursor = "cursor_before_10",
            HasNextPage = false,
            HasPreviousPage = true
        };

        // Assert
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeFalse();
        result.PreviousCursor.Should().NotBeNullOrEmpty();
        result.NextCursor.Should().BeNull();
    }

    [Fact]
    public void NavigationScenario_SinglePage()
    {
        // Arrange - Only one page of results
        var result = new CursorPaginatedResult<string>
        {
            Items = ["item1", "item2"],
            NextCursor = null,
            PreviousCursor = null,
            HasNextPage = false,
            HasPreviousPage = false
        };

        // Assert
        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeFalse();
        result.PreviousCursor.Should().BeNull();
        result.NextCursor.Should().BeNull();
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Equality_SameReference_ShouldBeEqual()
    {
        // Arrange
        var items = new List<int> { 1, 2, 3 };
        var result1 = new CursorPaginatedResult<int>
        {
            Items = items,
            NextCursor = "next",
            HasNextPage = true,
            HasPreviousPage = false
        };
        var result2 = new CursorPaginatedResult<int>
        {
            Items = items,
            NextCursor = "next",
            HasNextPage = true,
            HasPreviousPage = false
        };

        // Assert - Same Items reference means records are equal
        result1.Should().Be(result2);
    }

    [Fact]
    public void BeEquivalentTo_SameValues_ShouldSucceed()
    {
        // Arrange
        var result1 = new CursorPaginatedResult<int>
        {
            Items = [1, 2, 3],
            NextCursor = "next",
            HasNextPage = true,
            HasPreviousPage = false
        };
        var result2 = new CursorPaginatedResult<int>
        {
            Items = [1, 2, 3],
            NextCursor = "next",
            HasNextPage = true,
            HasPreviousPage = false
        };

        // Assert - Structural equality comparison
        result1.Should().BeEquivalentTo(result2);
    }

    #endregion
}
