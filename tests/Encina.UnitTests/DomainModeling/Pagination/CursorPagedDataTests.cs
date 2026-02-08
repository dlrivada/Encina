using Encina.DomainModeling.Pagination;
using FluentAssertions;

namespace Encina.UnitTests.DomainModeling.Pagination;

/// <summary>
/// Unit tests for <see cref="CursorPagedData{T}"/> and related internal types.
/// These tests verify the internal cursor pagination types that preserve cursor-per-item granularity.
/// </summary>
public class CursorPagedDataTests
{
    #region CursorPagedData Tests

    [Fact]
    public void CursorPagedData_Constructor_ShouldSetAllProperties()
    {
        // Arrange
        var items = new List<CursorItem<string>>
        {
            new("item1", "cursor1"),
            new("item2", "cursor2"),
            new("item3", "cursor3")
        };
        var pageInfo = new CursorPageInfo(
            HasPreviousPage: true,
            HasNextPage: true,
            StartCursor: "cursor1",
            EndCursor: "cursor3");

        // Act
        var pagedData = new CursorPagedData<string>(items, pageInfo, TotalCount: 100);

        // Assert
        pagedData.Items.Should().HaveCount(3);
        pagedData.PageInfo.Should().Be(pageInfo);
        pagedData.TotalCount.Should().Be(100);
    }

    [Fact]
    public void CursorPagedData_TotalCount_WhenNull_ShouldBeNull()
    {
        // Arrange
        var items = new List<CursorItem<string>> { new("item", "cursor") };
        var pageInfo = new CursorPageInfo(false, false, "cursor", "cursor");

        // Act
        var pagedData = new CursorPagedData<string>(items, pageInfo);

        // Assert
        pagedData.TotalCount.Should().BeNull();
    }

    [Fact]
    public void CursorPagedData_Empty_ShouldReturnEmptyResult()
    {
        // Act
        var empty = CursorPagedData<string>.Empty();

        // Assert
        empty.Items.Should().BeEmpty();
        empty.PageInfo.HasPreviousPage.Should().BeFalse();
        empty.PageInfo.HasNextPage.Should().BeFalse();
        empty.PageInfo.StartCursor.Should().BeNull();
        empty.PageInfo.EndCursor.Should().BeNull();
        empty.TotalCount.Should().Be(0);
    }

    #endregion

    #region CursorItem Tests

    [Fact]
    public void CursorItem_Constructor_ShouldSetProperties()
    {
        // Act
        var item = new CursorItem<int>(42, "cursor-42");

        // Assert
        item.Item.Should().Be(42);
        item.Cursor.Should().Be("cursor-42");
    }

    [Fact]
    public void CursorItem_WithComplexType_ShouldWork()
    {
        // Arrange
        var order = new TestOrder(Guid.NewGuid(), "Test Order", 99.99m);

        // Act
        var item = new CursorItem<TestOrder>(order, "order-cursor");

        // Assert
        item.Item.Should().Be(order);
        item.Cursor.Should().Be("order-cursor");
    }

    [Fact]
    public void CursorItem_Equality_SameValues_ShouldBeEqual()
    {
        // Arrange
        var item1 = new CursorItem<string>("value", "cursor");
        var item2 = new CursorItem<string>("value", "cursor");

        // Assert
        item1.Should().Be(item2);
    }

    [Fact]
    public void CursorItem_Equality_DifferentCursor_ShouldNotBeEqual()
    {
        // Arrange
        var item1 = new CursorItem<string>("value", "cursor1");
        var item2 = new CursorItem<string>("value", "cursor2");

        // Assert
        item1.Should().NotBe(item2);
    }

    #endregion

    #region CursorPageInfo Tests

    [Fact]
    public void CursorPageInfo_Constructor_ShouldSetAllProperties()
    {
        // Act
        var pageInfo = new CursorPageInfo(
            HasPreviousPage: true,
            HasNextPage: true,
            StartCursor: "start",
            EndCursor: "end");

        // Assert
        pageInfo.HasPreviousPage.Should().BeTrue();
        pageInfo.HasNextPage.Should().BeTrue();
        pageInfo.StartCursor.Should().Be("start");
        pageInfo.EndCursor.Should().Be("end");
    }

    [Fact]
    public void CursorPageInfo_FirstPage_ShouldHaveCorrectFlags()
    {
        // Arrange - First page
        var pageInfo = new CursorPageInfo(
            HasPreviousPage: false,
            HasNextPage: true,
            StartCursor: "start",
            EndCursor: "end");

        // Assert
        pageInfo.HasPreviousPage.Should().BeFalse();
        pageInfo.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void CursorPageInfo_LastPage_ShouldHaveCorrectFlags()
    {
        // Arrange - Last page
        var pageInfo = new CursorPageInfo(
            HasPreviousPage: true,
            HasNextPage: false,
            StartCursor: "start",
            EndCursor: "end");

        // Assert
        pageInfo.HasPreviousPage.Should().BeTrue();
        pageInfo.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void CursorPageInfo_EmptyResult_ShouldHaveNullCursors()
    {
        // Arrange - Empty result
        var pageInfo = new CursorPageInfo(
            HasPreviousPage: false,
            HasNextPage: false,
            StartCursor: null,
            EndCursor: null);

        // Assert
        pageInfo.StartCursor.Should().BeNull();
        pageInfo.EndCursor.Should().BeNull();
    }

    [Fact]
    public void CursorPageInfo_Equality_SameValues_ShouldBeEqual()
    {
        // Arrange
        var info1 = new CursorPageInfo(true, true, "start", "end");
        var info2 = new CursorPageInfo(true, true, "start", "end");

        // Assert
        info1.Should().Be(info2);
    }

    #endregion

    #region Projection Chain Tests

    [Fact]
    public void FromPagedData_ShouldProjectToPublicResult()
    {
        // Arrange
        var items = new List<CursorItem<string>>
        {
            new("item1", "cursor1"),
            new("item2", "cursor2"),
            new("item3", "cursor3")
        };
        var pageInfo = new CursorPageInfo(
            HasPreviousPage: true,
            HasNextPage: true,
            StartCursor: "cursor1",
            EndCursor: "cursor3");
        var pagedData = new CursorPagedData<string>(items, pageInfo, TotalCount: 100);

        // Act
        var result = CursorPaginatedResult<string>.FromPagedData(pagedData);

        // Assert
        result.Items.Should().BeEquivalentTo(["item1", "item2", "item3"]);
        result.PreviousCursor.Should().Be("cursor1"); // StartCursor -> PreviousCursor
        result.NextCursor.Should().Be("cursor3"); // EndCursor -> NextCursor
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
        result.TotalCount.Should().Be(100);
    }

    [Fact]
    public void FromPagedData_EmptyData_ShouldProjectToEmptyResult()
    {
        // Arrange
        var pagedData = CursorPagedData<string>.Empty();

        // Act
        var result = CursorPaginatedResult<string>.FromPagedData(pagedData);

        // Assert
        result.Items.Should().BeEmpty();
        result.PreviousCursor.Should().BeNull();
        result.NextCursor.Should().BeNull();
        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeFalse();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public void CursorPerItem_ShouldBePreservedInPagedData()
    {
        // Arrange - Each item has its own cursor
        var items = new List<CursorItem<int>>
        {
            new(1, "cursor-1"),
            new(2, "cursor-2"),
            new(3, "cursor-3"),
            new(4, "cursor-4"),
            new(5, "cursor-5")
        };
        var pageInfo = new CursorPageInfo(false, true, "cursor-1", "cursor-5");

        // Act
        var pagedData = new CursorPagedData<int>(items, pageInfo);

        // Assert - Each item should have its own cursor
        pagedData.Items.Should().HaveCount(5);
        pagedData.Items[0].Cursor.Should().Be("cursor-1");
        pagedData.Items[1].Cursor.Should().Be("cursor-2");
        pagedData.Items[2].Cursor.Should().Be("cursor-3");
        pagedData.Items[3].Cursor.Should().Be("cursor-4");
        pagedData.Items[4].Cursor.Should().Be("cursor-5");
    }

    #endregion

    #region GraphQL Edge Compatibility Tests

    [Fact]
    public void CursorItem_ShouldMapToGraphQLEdge()
    {
        // Arrange - CursorItem is the basis for GraphQL Edge type
        var item = new CursorItem<TestOrder>(
            new TestOrder(Guid.NewGuid(), "Order 1", 50.00m),
            "cursor-abc");

        // Assert - Properties match GraphQL Edge structure
        // edge.node -> CursorItem.Item
        // edge.cursor -> CursorItem.Cursor
        item.Item.Should().NotBeNull();
        item.Cursor.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CursorPageInfo_ShouldMapToGraphQLPageInfo()
    {
        // Arrange - CursorPageInfo follows GraphQL Relay spec
        var pageInfo = new CursorPageInfo(
            HasPreviousPage: true,
            HasNextPage: true,
            StartCursor: "startABC",
            EndCursor: "endXYZ");

        // Assert - Properties match GraphQL PageInfo
        pageInfo.HasPreviousPage.Should().BeTrue();
        pageInfo.HasNextPage.Should().BeTrue();
        pageInfo.StartCursor.Should().Be("startABC");
        pageInfo.EndCursor.Should().Be("endXYZ");
    }

    #endregion

    private sealed record TestOrder(Guid Id, string Name, decimal Total);
}
