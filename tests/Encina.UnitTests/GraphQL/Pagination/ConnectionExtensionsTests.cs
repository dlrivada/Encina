using Encina.DomainModeling.Pagination;
using Encina.GraphQL.Pagination;
using Shouldly;

namespace Encina.UnitTests.GraphQL.Pagination;

/// <summary>
/// Unit tests for ConnectionExtensions.
/// These tests verify the conversion between cursor pagination types and GraphQL Connection types.
/// </summary>
public sealed class ConnectionExtensionsTests
{
    #region ToConnection from CursorPagedData Tests

    [Fact]
    public void ToConnection_FromPagedData_ConvertsCorrectly()
    {
        // Arrange
        var items = new List<CursorItem<TestProduct>>
        {
            new(new TestProduct(1, "Product A"), "cursor_a"),
            new(new TestProduct(2, "Product B"), "cursor_b")
        };

        var pageInfo = new CursorPageInfo(
            HasPreviousPage: false,
            HasNextPage: true,
            StartCursor: "cursor_a",
            EndCursor: "cursor_b");

        var pagedData = new CursorPagedData<TestProduct>(items, pageInfo, TotalCount: 50);

        // Act
        var connection = pagedData.ToConnection();

        // Assert
        connection.Edges.Count.ShouldBe(2);
        connection.Edges[0].Node.Id.ShouldBe(1);
        connection.Edges[0].Cursor.ShouldBe("cursor_a");
        connection.Edges[1].Node.Id.ShouldBe(2);
        connection.Edges[1].Cursor.ShouldBe("cursor_b");
        connection.PageInfo.HasNextPage.ShouldBeTrue();
        connection.TotalCount.ShouldBe(50);
    }

    [Fact]
    public void ToConnection_FromPagedData_NullData_ThrowsArgumentNullException()
    {
        // Arrange
        CursorPagedData<TestProduct>? data = null;

        // Act
        Action act = () => data!.ToConnection();

        // Assert
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("data");
    }

    #endregion

    #region ToConnection from CursorPaginatedResult Tests

    [Fact]
    public void ToConnection_FromPaginatedResult_ConvertsCorrectly()
    {
        // Arrange
        var result = new CursorPaginatedResult<TestProduct>
        {
            Items = [new TestProduct(1, "Product A"), new TestProduct(2, "Product B")],
            NextCursor = "next_cursor",
            PreviousCursor = "prev_cursor",
            HasNextPage = true,
            HasPreviousPage = true,
            TotalCount = 100
        };

        // Act
        var connection = result.ToConnection();

        // Assert
        connection.Edges.Count.ShouldBe(2);
        connection.PageInfo.HasNextPage.ShouldBeTrue();
        connection.PageInfo.HasPreviousPage.ShouldBeTrue();
        connection.PageInfo.StartCursor.ShouldBe("prev_cursor");
        connection.PageInfo.EndCursor.ShouldBe("next_cursor");
        connection.TotalCount.ShouldBe(100);
    }

    [Fact]
    public void ToConnection_FromPaginatedResult_FirstEdgeGetsPreviousCursor()
    {
        // Arrange
        var result = new CursorPaginatedResult<TestProduct>
        {
            Items = [new TestProduct(1, "Product A"), new TestProduct(2, "Product B")],
            NextCursor = "next",
            PreviousCursor = "prev",
            HasNextPage = true,
            HasPreviousPage = true
        };

        // Act
        var connection = result.ToConnection();

        // Assert
        connection.Edges[0].Cursor.ShouldBe("prev");
    }

    [Fact]
    public void ToConnection_FromPaginatedResult_LastEdgeGetsNextCursor()
    {
        // Arrange
        var result = new CursorPaginatedResult<TestProduct>
        {
            Items = [new TestProduct(1, "Product A"), new TestProduct(2, "Product B")],
            NextCursor = "next",
            PreviousCursor = "prev",
            HasNextPage = true,
            HasPreviousPage = true
        };

        // Act
        var connection = result.ToConnection();

        // Assert
        connection.Edges[1].Cursor.ShouldBe("next");
    }

    [Fact]
    public void ToConnection_FromPaginatedResult_SingleItem_UsesPreviousCursor()
    {
        // Arrange
        var result = new CursorPaginatedResult<TestProduct>
        {
            Items = [new TestProduct(1, "Product A")],
            NextCursor = null,
            PreviousCursor = "cursor",
            HasNextPage = false,
            HasPreviousPage = true
        };

        // Act
        var connection = result.ToConnection();

        // Assert
        connection.Edges.Count.ShouldBe(1);
        connection.Edges[0].Cursor.ShouldBe("cursor");
    }

    [Fact]
    public void ToConnection_FromPaginatedResult_EmptyResult_ReturnsEmptyConnection()
    {
        // Arrange
        var result = new CursorPaginatedResult<TestProduct>
        {
            Items = [],
            NextCursor = null,
            PreviousCursor = null,
            HasNextPage = false,
            HasPreviousPage = false,
            TotalCount = 0
        };

        // Act
        var connection = result.ToConnection();

        // Assert
        connection.Edges.ShouldBeEmpty();
        connection.PageInfo.HasNextPage.ShouldBeFalse();
        connection.PageInfo.HasPreviousPage.ShouldBeFalse();
    }

    [Fact]
    public void ToConnection_FromPaginatedResult_NullResult_ThrowsArgumentNullException()
    {
        // Arrange
        CursorPaginatedResult<TestProduct>? result = null;

        // Act
        Action act = () => result!.ToConnection();

        // Assert
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("result");
    }

    [Fact]
    public void ToConnection_FromPaginatedResult_ManyItems_MiddleItemsGetApproximateCursor()
    {
        // Arrange
        var result = new CursorPaginatedResult<TestProduct>
        {
            Items =
            [
                new TestProduct(1, "A"),
                new TestProduct(2, "B"),
                new TestProduct(3, "C"),
                new TestProduct(4, "D")
            ],
            NextCursor = "next",
            PreviousCursor = "prev",
            HasNextPage = true,
            HasPreviousPage = true
        };

        // Act
        var connection = result.ToConnection();

        // Assert
        connection.Edges[0].Cursor.ShouldBe("prev"); // First
        connection.Edges[1].Cursor.ShouldBe("next"); // Middle - uses approximate
        connection.Edges[2].Cursor.ShouldBe("next"); // Middle - uses approximate
        connection.Edges[3].Cursor.ShouldBe("next"); // Last
    }

    #endregion

    #region Map Tests

    [Fact]
    public void Map_TransformsNodesCorrectly()
    {
        // Arrange
        var items = new List<CursorItem<TestProduct>>
        {
            new(new TestProduct(1, "Product A"), "cursor_a"),
            new(new TestProduct(2, "Product B"), "cursor_b")
        };

        var pageInfo = new CursorPageInfo(false, true, "cursor_a", "cursor_b");
        var pagedData = new CursorPagedData<TestProduct>(items, pageInfo, TotalCount: 50);
        var connection = pagedData.ToConnection();

        // Act
        var mappedConnection = connection.Map(p => new TestProductDto(p.Id, p.Name.ToUpperInvariant()));

        // Assert
        mappedConnection.Edges.Count.ShouldBe(2);
        mappedConnection.Edges[0].Node.Id.ShouldBe(1);
        mappedConnection.Edges[0].Node.Name.ShouldBe("PRODUCT A");
        mappedConnection.Edges[1].Node.Id.ShouldBe(2);
        mappedConnection.Edges[1].Node.Name.ShouldBe("PRODUCT B");
    }

    [Fact]
    public void Map_PreservesCursors()
    {
        // Arrange
        var items = new List<CursorItem<TestProduct>>
        {
            new(new TestProduct(1, "Product A"), "cursor_a")
        };

        var pageInfo = new CursorPageInfo(false, true, "cursor_a", "cursor_a");
        var pagedData = new CursorPagedData<TestProduct>(items, pageInfo);
        var connection = pagedData.ToConnection();

        // Act
        var mappedConnection = connection.Map(p => new TestProductDto(p.Id, p.Name));

        // Assert
        mappedConnection.Edges[0].Cursor.ShouldBe("cursor_a");
    }

    [Fact]
    public void Map_PreservesPageInfo()
    {
        // Arrange
        var items = new List<CursorItem<TestProduct>>
        {
            new(new TestProduct(1, "Product A"), "cursor_a")
        };

        var pageInfo = new CursorPageInfo(true, true, "start", "end");
        var pagedData = new CursorPagedData<TestProduct>(items, pageInfo, TotalCount: 100);
        var connection = pagedData.ToConnection();

        // Act
        var mappedConnection = connection.Map(p => new TestProductDto(p.Id, p.Name));

        // Assert
        mappedConnection.PageInfo.ShouldBe(connection.PageInfo);
        mappedConnection.TotalCount.ShouldBe(100);
    }

    [Fact]
    public void Map_NullConnection_ThrowsArgumentNullException()
    {
        // Arrange
        Connection<TestProduct>? connection = null;

        // Act
        Action act = () => connection!.Map(p => new TestProductDto(p.Id, p.Name));

        // Assert
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("connection");
    }

    [Fact]
    public void Map_NullSelector_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Connection<TestProduct>.Empty();

        // Act
        Action act = () => connection.Map<TestProduct, TestProductDto>(null!);

        // Assert
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("selector");
    }

    #endregion

    #region Test Types

    private sealed record TestProduct(int Id, string Name);
    private sealed record TestProductDto(int Id, string Name);

    #endregion
}
