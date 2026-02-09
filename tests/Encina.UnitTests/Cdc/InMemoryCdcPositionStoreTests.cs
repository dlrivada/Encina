using Encina.Cdc.Processing;
using Shouldly;

namespace Encina.UnitTests.Cdc;

/// <summary>
/// Unit tests for <see cref="InMemoryCdcPositionStore"/>.
/// </summary>
public sealed class InMemoryCdcPositionStoreTests
{
    [Fact]
    public async Task GetPositionAsync_NoSavedPosition_ReturnsNone()
    {
        var store = new InMemoryCdcPositionStore();

        var result = await store.GetPositionAsync("connector-1");

        result.IsRight.ShouldBeTrue();
        var option = result.Match(Right: o => o, Left: _ => default);
        option.IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task SavePositionAsync_ThenGetPositionAsync_ReturnsSavedPosition()
    {
        var store = new InMemoryCdcPositionStore();
        var position = new TestCdcPosition(100);

        await store.SavePositionAsync("connector-1", position);
        var result = await store.GetPositionAsync("connector-1");

        result.IsRight.ShouldBeTrue();
        var option = result.Match(Right: o => o, Left: _ => default);
        option.IsSome.ShouldBeTrue();
        option.IfSome(p => ((TestCdcPosition)p).Value.ShouldBe(100));
    }

    [Fact]
    public async Task SavePositionAsync_OverwritesPreviousPosition()
    {
        var store = new InMemoryCdcPositionStore();

        await store.SavePositionAsync("connector-1", new TestCdcPosition(10));
        await store.SavePositionAsync("connector-1", new TestCdcPosition(20));

        var result = await store.GetPositionAsync("connector-1");
        result.IsRight.ShouldBeTrue();
        var option = result.Match(Right: o => o, Left: _ => default);
        option.IfSome(p => ((TestCdcPosition)p).Value.ShouldBe(20));
    }

    [Fact]
    public async Task DeletePositionAsync_RemovesPosition()
    {
        var store = new InMemoryCdcPositionStore();
        await store.SavePositionAsync("connector-1", new TestCdcPosition(10));

        var deleteResult = await store.DeletePositionAsync("connector-1");

        deleteResult.IsRight.ShouldBeTrue();
        var getResult = await store.GetPositionAsync("connector-1");
        getResult.IsRight.ShouldBeTrue();
        var option = getResult.Match(Right: o => o, Left: _ => default);
        option.IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task DeletePositionAsync_NonExistentConnector_ReturnsSuccess()
    {
        var store = new InMemoryCdcPositionStore();

        var result = await store.DeletePositionAsync("non-existent");

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task GetPositionAsync_CaseInsensitive()
    {
        var store = new InMemoryCdcPositionStore();
        await store.SavePositionAsync("MyConnector", new TestCdcPosition(42));

        var result = await store.GetPositionAsync("myconnector");

        result.IsRight.ShouldBeTrue();
        var option = result.Match(Right: o => o, Left: _ => default);
        option.IsSome.ShouldBeTrue();
    }

    [Fact]
    public async Task MultipleConnectors_IndependentPositions()
    {
        var store = new InMemoryCdcPositionStore();

        await store.SavePositionAsync("connector-a", new TestCdcPosition(100));
        await store.SavePositionAsync("connector-b", new TestCdcPosition(200));

        var resultA = await store.GetPositionAsync("connector-a");
        var resultB = await store.GetPositionAsync("connector-b");

        resultA.IsRight.ShouldBeTrue();
        resultB.IsRight.ShouldBeTrue();

        var optionA = resultA.Match(Right: o => o, Left: _ => default);
        optionA.IfSome(p => ((TestCdcPosition)p).Value.ShouldBe(100));

        var optionB = resultB.Match(Right: o => o, Left: _ => default);
        optionB.IfSome(p => ((TestCdcPosition)p).Value.ShouldBe(200));
    }

    [Fact]
    public async Task GetPositionAsync_NullConnectorId_ThrowsArgumentException()
    {
        var store = new InMemoryCdcPositionStore();

        await Should.ThrowAsync<ArgumentException>(
            () => store.GetPositionAsync(null!));
    }

    [Fact]
    public async Task SavePositionAsync_NullConnectorId_ThrowsArgumentException()
    {
        var store = new InMemoryCdcPositionStore();

        await Should.ThrowAsync<ArgumentException>(
            () => store.SavePositionAsync(null!, new TestCdcPosition(1)));
    }

    [Fact]
    public async Task SavePositionAsync_NullPosition_ThrowsArgumentNullException()
    {
        var store = new InMemoryCdcPositionStore();

        await Should.ThrowAsync<ArgumentNullException>(
            () => store.SavePositionAsync("connector", null!));
    }

    [Fact]
    public async Task DeletePositionAsync_NullConnectorId_ThrowsArgumentException()
    {
        var store = new InMemoryCdcPositionStore();

        await Should.ThrowAsync<ArgumentException>(
            () => store.DeletePositionAsync(null!));
    }
}
