using Encina.DomainModeling.Pagination;
using Encina.GraphQL.Pagination;
using Shouldly;

namespace Encina.UnitTests.GraphQL.Pagination;

/// <summary>
/// Unit tests for GraphQL Relay Connection types.
/// These tests verify the Connection, Edge, and PageInfo types follow the Relay spec.
/// </summary>
public sealed class ConnectionTests
{
    #region Connection.FromPagedData Tests

    [Fact]
    public void FromPagedData_WithItems_CreatesCorrectEdges()
    {
        // Arrange
        var items = new List<CursorItem<TestOrder>>
        {
            new(new TestOrder(1, "Order 1", 100m), "cursor_1"),
            new(new TestOrder(2, "Order 2", 200m), "cursor_2"),
            new(new TestOrder(3, "Order 3", 300m), "cursor_3")
        };

        var pageInfo = new CursorPageInfo(
            HasPreviousPage: false,
            HasNextPage: true,
            StartCursor: "cursor_1",
            EndCursor: "cursor_3");

        var pagedData = new CursorPagedData<TestOrder>(items, pageInfo, TotalCount: 10);

        // Act
        var connection = Connection<TestOrder>.FromPagedData(pagedData);

        // Assert
        connection.Edges.Count.ShouldBe(3);
        connection.Edges[0].Node.Id.ShouldBe(1);
        connection.Edges[0].Cursor.ShouldBe("cursor_1");
        connection.Edges[1].Node.Id.ShouldBe(2);
        connection.Edges[1].Cursor.ShouldBe("cursor_2");
        connection.Edges[2].Node.Id.ShouldBe(3);
        connection.Edges[2].Cursor.ShouldBe("cursor_3");
    }

    [Fact]
    public void FromPagedData_MapsPageInfoCorrectly()
    {
        // Arrange
        var items = new List<CursorItem<TestOrder>>
        {
            new(new TestOrder(1, "Order 1", 100m), "cursor_1")
        };

        var pageInfo = new CursorPageInfo(
            HasPreviousPage: true,
            HasNextPage: true,
            StartCursor: "start_cursor",
            EndCursor: "end_cursor");

        var pagedData = new CursorPagedData<TestOrder>(items, pageInfo, TotalCount: 100);

        // Act
        var connection = Connection<TestOrder>.FromPagedData(pagedData);

        // Assert
        connection.PageInfo.HasPreviousPage.ShouldBeTrue();
        connection.PageInfo.HasNextPage.ShouldBeTrue();
        connection.PageInfo.StartCursor.ShouldBe("start_cursor");
        connection.PageInfo.EndCursor.ShouldBe("end_cursor");
    }

    [Fact]
    public void FromPagedData_PreservesTotalCount()
    {
        // Arrange
        var pageInfo = new CursorPageInfo(false, false, null, null);
        var pagedData = new CursorPagedData<TestOrder>([], pageInfo, TotalCount: 42);

        // Act
        var connection = Connection<TestOrder>.FromPagedData(pagedData);

        // Assert
        connection.TotalCount.ShouldBe(42);
    }

    [Fact]
    public void FromPagedData_NullTotalCount_PreservesNull()
    {
        // Arrange
        var pageInfo = new CursorPageInfo(false, false, null, null);
        var pagedData = new CursorPagedData<TestOrder>([], pageInfo, TotalCount: null);

        // Act
        var connection = Connection<TestOrder>.FromPagedData(pagedData);

        // Assert
        connection.TotalCount.ShouldBeNull();
    }

    [Fact]
    public void FromPagedData_NullData_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => Connection<TestOrder>.FromPagedData(null!);

        // Assert
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("data");
    }

    #endregion

    #region Connection.Empty Tests

    [Fact]
    public void Empty_ReturnsEmptyConnection()
    {
        // Act
        var connection = Connection<TestOrder>.Empty();

        // Assert
        connection.Edges.ShouldBeEmpty();
        connection.Nodes.ShouldBeEmpty();
        connection.TotalCount.ShouldBe(0);
    }

    [Fact]
    public void Empty_HasCorrectPageInfo()
    {
        // Act
        var connection = Connection<TestOrder>.Empty();

        // Assert
        connection.PageInfo.HasPreviousPage.ShouldBeFalse();
        connection.PageInfo.HasNextPage.ShouldBeFalse();
        connection.PageInfo.StartCursor.ShouldBeNull();
        connection.PageInfo.EndCursor.ShouldBeNull();
    }

    #endregion

    #region Connection.Nodes Property Tests

    [Fact]
    public void Nodes_ReturnsItemsWithoutCursors()
    {
        // Arrange
        var items = new List<CursorItem<TestOrder>>
        {
            new(new TestOrder(1, "Order 1", 100m), "cursor_1"),
            new(new TestOrder(2, "Order 2", 200m), "cursor_2")
        };

        var pageInfo = new CursorPageInfo(false, false, "cursor_1", "cursor_2");
        var pagedData = new CursorPagedData<TestOrder>(items, pageInfo);

        // Act
        var connection = Connection<TestOrder>.FromPagedData(pagedData);

        // Assert
        connection.Nodes.Count.ShouldBe(2);
        connection.Nodes[0].Id.ShouldBe(1);
        connection.Nodes[1].Id.ShouldBe(2);
    }

    #endregion

    #region Edge Tests

    [Fact]
    public void Edge_HasRequiredProperties()
    {
        // Arrange & Act
        var edge = new Edge<TestOrder>
        {
            Node = new TestOrder(1, "Test Order", 99.99m),
            Cursor = "test_cursor"
        };

        // Assert
        edge.Node.Id.ShouldBe(1);
        edge.Node.Name.ShouldBe("Test Order");
        edge.Cursor.ShouldBe("test_cursor");
    }

    #endregion

    #region RelayPageInfo Tests

    [Fact]
    public void RelayPageInfo_Empty_ReturnsCorrectDefaults()
    {
        // Act
        var pageInfo = RelayPageInfo.Empty();

        // Assert
        pageInfo.HasPreviousPage.ShouldBeFalse();
        pageInfo.HasNextPage.ShouldBeFalse();
        pageInfo.StartCursor.ShouldBeNull();
        pageInfo.EndCursor.ShouldBeNull();
    }

    [Fact]
    public void RelayPageInfo_HasAllProperties()
    {
        // Arrange & Act
        var pageInfo = new RelayPageInfo
        {
            HasPreviousPage = true,
            HasNextPage = true,
            StartCursor = "start",
            EndCursor = "end"
        };

        // Assert
        pageInfo.HasPreviousPage.ShouldBeTrue();
        pageInfo.HasNextPage.ShouldBeTrue();
        pageInfo.StartCursor.ShouldBe("start");
        pageInfo.EndCursor.ShouldBe("end");
    }

    #endregion

    #region Test Types

    private sealed record TestOrder(int Id, string Name, decimal Total);

    #endregion
}
