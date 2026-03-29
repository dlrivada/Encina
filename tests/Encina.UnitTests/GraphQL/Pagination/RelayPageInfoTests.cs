using Encina.GraphQL.Pagination;

namespace Encina.UnitTests.GraphQL.Pagination;

/// <summary>
/// Unit tests for <see cref="RelayPageInfo"/>.
/// </summary>
public sealed class RelayPageInfoTests
{
    [Fact]
    public void Empty_ShouldReturnAllFalseAndNullCursors()
    {
        var info = RelayPageInfo.Empty();
        info.HasPreviousPage.ShouldBeFalse();
        info.HasNextPage.ShouldBeFalse();
        info.StartCursor.ShouldBeNull();
        info.EndCursor.ShouldBeNull();
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        var info = new RelayPageInfo
        {
            HasPreviousPage = true,
            HasNextPage = true,
            StartCursor = "cursor-1",
            EndCursor = "cursor-10"
        };

        info.HasPreviousPage.ShouldBeTrue();
        info.HasNextPage.ShouldBeTrue();
        info.StartCursor.ShouldBe("cursor-1");
        info.EndCursor.ShouldBe("cursor-10");
    }
}
