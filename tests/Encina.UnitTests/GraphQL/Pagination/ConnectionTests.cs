using Encina.DomainModeling.Pagination;
using Encina.GraphQL.Pagination;
using FluentAssertions;

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
        connection.Edges.Should().HaveCount(3);
        connection.Edges[0].Node.Id.Should().Be(1);
        connection.Edges[0].Cursor.Should().Be("cursor_1");
        connection.Edges[1].Node.Id.Should().Be(2);
        connection.Edges[1].Cursor.Should().Be("cursor_2");
        connection.Edges[2].Node.Id.Should().Be(3);
        connection.Edges[2].Cursor.Should().Be("cursor_3");
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
        connection.PageInfo.HasPreviousPage.Should().BeTrue();
        connection.PageInfo.HasNextPage.Should().BeTrue();
        connection.PageInfo.StartCursor.Should().Be("start_cursor");
        connection.PageInfo.EndCursor.Should().Be("end_cursor");
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
        connection.TotalCount.Should().Be(42);
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
        connection.TotalCount.Should().BeNull();
    }

    [Fact]
    public void FromPagedData_NullData_ThrowsArgumentNullException()
    {
        // Act
        var act = () => Connection<TestOrder>.FromPagedData(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("data");
    }

    #endregion

    #region Connection.Empty Tests

    [Fact]
    public void Empty_ReturnsEmptyConnection()
    {
        // Act
        var connection = Connection<TestOrder>.Empty();

        // Assert
        connection.Edges.Should().BeEmpty();
        connection.Nodes.Should().BeEmpty();
        connection.TotalCount.Should().Be(0);
    }

    [Fact]
    public void Empty_HasCorrectPageInfo()
    {
        // Act
        var connection = Connection<TestOrder>.Empty();

        // Assert
        connection.PageInfo.HasPreviousPage.Should().BeFalse();
        connection.PageInfo.HasNextPage.Should().BeFalse();
        connection.PageInfo.StartCursor.Should().BeNull();
        connection.PageInfo.EndCursor.Should().BeNull();
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
        connection.Nodes.Should().HaveCount(2);
        connection.Nodes[0].Id.Should().Be(1);
        connection.Nodes[1].Id.Should().Be(2);
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
        edge.Node.Id.Should().Be(1);
        edge.Node.Name.Should().Be("Test Order");
        edge.Cursor.Should().Be("test_cursor");
    }

    #endregion

    #region RelayPageInfo Tests

    [Fact]
    public void RelayPageInfo_Empty_ReturnsCorrectDefaults()
    {
        // Act
        var pageInfo = RelayPageInfo.Empty();

        // Assert
        pageInfo.HasPreviousPage.Should().BeFalse();
        pageInfo.HasNextPage.Should().BeFalse();
        pageInfo.StartCursor.Should().BeNull();
        pageInfo.EndCursor.Should().BeNull();
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
        pageInfo.HasPreviousPage.Should().BeTrue();
        pageInfo.HasNextPage.Should().BeTrue();
        pageInfo.StartCursor.Should().Be("start");
        pageInfo.EndCursor.Should().Be("end");
    }

    #endregion

    #region Test Types

    private sealed record TestOrder(int Id, string Name, decimal Total);

    #endregion
}
