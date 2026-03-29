using Encina.GraphQL.Pagination;

namespace Encina.UnitTests.GraphQL.Pagination;

/// <summary>
/// Unit tests for <see cref="Edge{T}"/>.
/// </summary>
public sealed class EdgeTests
{
    [Fact]
    public void Edge_ShouldStoreNodeAndCursor()
    {
        var edge = new Edge<string> { Node = "item-1", Cursor = "abc123" };
        edge.Node.ShouldBe("item-1");
        edge.Cursor.ShouldBe("abc123");
    }

    [Fact]
    public void Edge_WithComplexType_ShouldWork()
    {
        var order = new TestOrder { Id = 42, Name = "Test" };
        var edge = new Edge<TestOrder> { Node = order, Cursor = "cursor-42" };

        edge.Node.Id.ShouldBe(42);
        edge.Node.Name.ShouldBe("Test");
        edge.Cursor.ShouldBe("cursor-42");
    }

    private sealed class TestOrder
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}
