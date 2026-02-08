using Encina.DomainModeling.Pagination;
using Encina.GraphQL.Pagination;
using FluentAssertions;

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
        connection.Edges.Should().HaveCount(2);
        connection.Edges[0].Node.Id.Should().Be(1);
        connection.Edges[0].Cursor.Should().Be("cursor_a");
        connection.Edges[1].Node.Id.Should().Be(2);
        connection.Edges[1].Cursor.Should().Be("cursor_b");
        connection.PageInfo.HasNextPage.Should().BeTrue();
        connection.TotalCount.Should().Be(50);
    }

    [Fact]
    public void ToConnection_FromPagedData_NullData_ThrowsArgumentNullException()
    {
        // Arrange
        CursorPagedData<TestProduct>? data = null;

        // Act
        var act = () => data!.ToConnection();

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("data");
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
        connection.Edges.Should().HaveCount(2);
        connection.PageInfo.HasNextPage.Should().BeTrue();
        connection.PageInfo.HasPreviousPage.Should().BeTrue();
        connection.PageInfo.StartCursor.Should().Be("prev_cursor");
        connection.PageInfo.EndCursor.Should().Be("next_cursor");
        connection.TotalCount.Should().Be(100);
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
        connection.Edges[0].Cursor.Should().Be("prev");
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
        connection.Edges[1].Cursor.Should().Be("next");
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
        connection.Edges.Should().HaveCount(1);
        connection.Edges[0].Cursor.Should().Be("cursor");
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
        connection.Edges.Should().BeEmpty();
        connection.PageInfo.HasNextPage.Should().BeFalse();
        connection.PageInfo.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void ToConnection_FromPaginatedResult_NullResult_ThrowsArgumentNullException()
    {
        // Arrange
        CursorPaginatedResult<TestProduct>? result = null;

        // Act
        var act = () => result!.ToConnection();

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("result");
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
        connection.Edges[0].Cursor.Should().Be("prev"); // First
        connection.Edges[1].Cursor.Should().Be("next"); // Middle - uses approximate
        connection.Edges[2].Cursor.Should().Be("next"); // Middle - uses approximate
        connection.Edges[3].Cursor.Should().Be("next"); // Last
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
        mappedConnection.Edges.Should().HaveCount(2);
        mappedConnection.Edges[0].Node.Id.Should().Be(1);
        mappedConnection.Edges[0].Node.Name.Should().Be("PRODUCT A");
        mappedConnection.Edges[1].Node.Id.Should().Be(2);
        mappedConnection.Edges[1].Node.Name.Should().Be("PRODUCT B");
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
        mappedConnection.Edges[0].Cursor.Should().Be("cursor_a");
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
        mappedConnection.PageInfo.Should().Be(connection.PageInfo);
        mappedConnection.TotalCount.Should().Be(100);
    }

    [Fact]
    public void Map_NullConnection_ThrowsArgumentNullException()
    {
        // Arrange
        Connection<TestProduct>? connection = null;

        // Act
        var act = () => connection!.Map(p => new TestProductDto(p.Id, p.Name));

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("connection");
    }

    [Fact]
    public void Map_NullSelector_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Connection<TestProduct>.Empty();

        // Act
        var act = () => connection.Map<TestProduct, TestProductDto>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("selector");
    }

    #endregion

    #region Test Types

    private sealed record TestProduct(int Id, string Name);
    private sealed record TestProductDto(int Id, string Name);

    #endregion
}
