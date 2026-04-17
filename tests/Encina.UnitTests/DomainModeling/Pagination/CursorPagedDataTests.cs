using Encina.DomainModeling.Pagination;
using Shouldly;

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
        pagedData.Items.Count.ShouldBe(3);
        pagedData.PageInfo.ShouldBe(pageInfo);
        pagedData.TotalCount.ShouldBe(100);
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
        pagedData.TotalCount.ShouldBeNull();
    }

    [Fact]
    public void CursorPagedData_Empty_ShouldReturnEmptyResult()
    {
        // Act
        var empty = CursorPagedData<string>.Empty();

        // Assert
        empty.Items.ShouldBeEmpty();
        empty.PageInfo.HasPreviousPage.ShouldBeFalse();
        empty.PageInfo.HasNextPage.ShouldBeFalse();
        empty.PageInfo.StartCursor.ShouldBeNull();
        empty.PageInfo.EndCursor.ShouldBeNull();
        empty.TotalCount.ShouldBe(0);
    }

    #endregion

    #region CursorItem Tests

    [Fact]
    public void CursorItem_Constructor_ShouldSetProperties()
    {
        // Act
        var item = new CursorItem<int>(42, "cursor-42");

        // Assert
        item.Item.ShouldBe(42);
        item.Cursor.ShouldBe("cursor-42");
    }

    [Fact]
    public void CursorItem_WithComplexType_ShouldWork()
    {
        // Arrange
        var order = new TestOrder(Guid.NewGuid(), "Test Order", 99.99m);

        // Act
        var item = new CursorItem<TestOrder>(order, "order-cursor");

        // Assert
        item.Item.ShouldBe(order);
        item.Cursor.ShouldBe("order-cursor");
    }

    [Fact]
    public void CursorItem_Equality_SameValues_ShouldBeEqual()
    {
        // Arrange
        var item1 = new CursorItem<string>("value", "cursor");
        var item2 = new CursorItem<string>("value", "cursor");

        // Assert
        item1.ShouldBe(item2);
    }

    [Fact]
    public void CursorItem_Equality_DifferentCursor_ShouldNotBeEqual()
    {
        // Arrange
        var item1 = new CursorItem<string>("value", "cursor1");
        var item2 = new CursorItem<string>("value", "cursor2");

        // Assert
        item1.ShouldNotBe(item2);
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
        pageInfo.HasPreviousPage.ShouldBeTrue();
        pageInfo.HasNextPage.ShouldBeTrue();
        pageInfo.StartCursor.ShouldBe("start");
        pageInfo.EndCursor.ShouldBe("end");
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
        pageInfo.HasPreviousPage.ShouldBeFalse();
        pageInfo.HasNextPage.ShouldBeTrue();
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
        pageInfo.HasPreviousPage.ShouldBeTrue();
        pageInfo.HasNextPage.ShouldBeFalse();
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
        pageInfo.StartCursor.ShouldBeNull();
        pageInfo.EndCursor.ShouldBeNull();
    }

    [Fact]
    public void CursorPageInfo_Equality_SameValues_ShouldBeEqual()
    {
        // Arrange
        var info1 = new CursorPageInfo(true, true, "start", "end");
        var info2 = new CursorPageInfo(true, true, "start", "end");

        // Assert
        info1.ShouldBe(info2);
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
        result.Items.ShouldBe(["item1", "item2", "item3"]);
        result.PreviousCursor.ShouldBe("cursor1"); // StartCursor -> PreviousCursor
        result.NextCursor.ShouldBe("cursor3"); // EndCursor -> NextCursor
        result.HasPreviousPage.ShouldBeTrue();
        result.HasNextPage.ShouldBeTrue();
        result.TotalCount.ShouldBe(100);
    }

    [Fact]
    public void FromPagedData_EmptyData_ShouldProjectToEmptyResult()
    {
        // Arrange
        var pagedData = CursorPagedData<string>.Empty();

        // Act
        var result = CursorPaginatedResult<string>.FromPagedData(pagedData);

        // Assert
        result.Items.ShouldBeEmpty();
        result.PreviousCursor.ShouldBeNull();
        result.NextCursor.ShouldBeNull();
        result.HasPreviousPage.ShouldBeFalse();
        result.HasNextPage.ShouldBeFalse();
        result.TotalCount.ShouldBe(0);
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
        pagedData.Items.Count.ShouldBe(5);
        pagedData.Items[0].Cursor.ShouldBe("cursor-1");
        pagedData.Items[1].Cursor.ShouldBe("cursor-2");
        pagedData.Items[2].Cursor.ShouldBe("cursor-3");
        pagedData.Items[3].Cursor.ShouldBe("cursor-4");
        pagedData.Items[4].Cursor.ShouldBe("cursor-5");
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
        item.Item.ShouldNotBeNull();
        item.Cursor.ShouldNotBeNullOrEmpty();
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
        pageInfo.HasPreviousPage.ShouldBeTrue();
        pageInfo.HasNextPage.ShouldBeTrue();
        pageInfo.StartCursor.ShouldBe("startABC");
        pageInfo.EndCursor.ShouldBe("endXYZ");
    }

    #endregion

    private sealed record TestOrder(Guid Id, string Name, decimal Total);
}
