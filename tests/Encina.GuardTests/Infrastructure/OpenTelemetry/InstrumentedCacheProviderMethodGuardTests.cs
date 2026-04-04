using Encina.OpenTelemetry.QueryCache;

namespace Encina.GuardTests.Infrastructure.OpenTelemetry;

/// <summary>
/// Guard tests that exercise ALL public methods of <see cref="InstrumentedCacheProvider"/>.
/// Each method delegates to the inner cache provider wrapped in Activity tracing.
/// </summary>
public sealed class InstrumentedCacheProviderMethodGuardTests
{
    [Fact]
    public async Task GetAsync_DelegatesToInner()
    {
        var inner = Substitute.For<ICacheProvider>();
        inner.GetAsync<string>("key-1", Arg.Any<CancellationToken>())
            .Returns("value-1");

        var sut = new InstrumentedCacheProvider(inner);

        var result = await sut.GetAsync<string>("key-1", CancellationToken.None);

        result.ShouldBe("value-1");
        await inner.Received(1).GetAsync<string>("key-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_WhenMiss_ReturnsNull()
    {
        var inner = Substitute.For<ICacheProvider>();
        inner.GetAsync<string>("key-miss", Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var sut = new InstrumentedCacheProvider(inner);

        var result = await sut.GetAsync<string>("key-miss", CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetAsync_WhenInnerThrows_PropagatesException()
    {
        var inner = Substitute.For<ICacheProvider>();
        inner.GetAsync<string>("key-err", Arg.Any<CancellationToken>())
            .Returns<string?>(x => throw new InvalidOperationException("cache down"));

        var sut = new InstrumentedCacheProvider(inner);

        await Should.ThrowAsync<InvalidOperationException>(
            () => sut.GetAsync<string>("key-err", CancellationToken.None));
    }

    [Fact]
    public async Task SetAsync_DelegatesToInner()
    {
        var inner = Substitute.For<ICacheProvider>();
        var sut = new InstrumentedCacheProvider(inner);

        await sut.SetAsync("key-1", "value-1", TimeSpan.FromMinutes(5), CancellationToken.None);

        await inner.Received(1).SetAsync("key-1", "value-1", TimeSpan.FromMinutes(5), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetAsync_WhenInnerThrows_PropagatesException()
    {
        var inner = Substitute.For<ICacheProvider>();
        inner.When(x => x.SetAsync("key-err", Arg.Any<string>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new InvalidOperationException("cache down"));

        var sut = new InstrumentedCacheProvider(inner);

        await Should.ThrowAsync<InvalidOperationException>(
            () => sut.SetAsync("key-err", "value", TimeSpan.FromMinutes(5), CancellationToken.None));
    }

    [Fact]
    public async Task RemoveAsync_DelegatesToInner()
    {
        var inner = Substitute.For<ICacheProvider>();
        var sut = new InstrumentedCacheProvider(inner);

        await sut.RemoveAsync("key-1", CancellationToken.None);

        await inner.Received(1).RemoveAsync("key-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveAsync_WhenInnerThrows_PropagatesException()
    {
        var inner = Substitute.For<ICacheProvider>();
        inner.When(x => x.RemoveAsync("key-err", Arg.Any<CancellationToken>()))
            .Do(_ => throw new InvalidOperationException("cache down"));

        var sut = new InstrumentedCacheProvider(inner);

        await Should.ThrowAsync<InvalidOperationException>(
            () => sut.RemoveAsync("key-err", CancellationToken.None));
    }

    [Fact]
    public async Task RemoveByPatternAsync_DelegatesToInner()
    {
        var inner = Substitute.For<ICacheProvider>();
        var sut = new InstrumentedCacheProvider(inner);

        await sut.RemoveByPatternAsync("key-*", CancellationToken.None);

        await inner.Received(1).RemoveByPatternAsync("key-*", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveByPatternAsync_WhenInnerThrows_PropagatesException()
    {
        var inner = Substitute.For<ICacheProvider>();
        inner.When(x => x.RemoveByPatternAsync("key-*", Arg.Any<CancellationToken>()))
            .Do(_ => throw new InvalidOperationException("cache down"));

        var sut = new InstrumentedCacheProvider(inner);

        await Should.ThrowAsync<InvalidOperationException>(
            () => sut.RemoveByPatternAsync("key-*", CancellationToken.None));
    }

    [Fact]
    public async Task ExistsAsync_WhenKeyExists_ReturnsTrue()
    {
        var inner = Substitute.For<ICacheProvider>();
        inner.ExistsAsync("key-1", Arg.Any<CancellationToken>())
            .Returns(true);

        var sut = new InstrumentedCacheProvider(inner);

        var result = await sut.ExistsAsync("key-1", CancellationToken.None);

        result.ShouldBeTrue();
        await inner.Received(1).ExistsAsync("key-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExistsAsync_WhenKeyDoesNotExist_ReturnsFalse()
    {
        var inner = Substitute.For<ICacheProvider>();
        inner.ExistsAsync("key-miss", Arg.Any<CancellationToken>())
            .Returns(false);

        var sut = new InstrumentedCacheProvider(inner);

        var result = await sut.ExistsAsync("key-miss", CancellationToken.None);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task ExistsAsync_WhenInnerThrows_PropagatesException()
    {
        var inner = Substitute.For<ICacheProvider>();
        inner.ExistsAsync("key-err", Arg.Any<CancellationToken>())
            .Returns<bool>(x => throw new InvalidOperationException("cache down"));

        var sut = new InstrumentedCacheProvider(inner);

        await Should.ThrowAsync<InvalidOperationException>(
            () => sut.ExistsAsync("key-err", CancellationToken.None));
    }

    [Fact]
    public async Task GetOrSetAsync_DelegatesToInner()
    {
        var inner = Substitute.For<ICacheProvider>();
        inner.GetOrSetAsync("key-1", Arg.Any<Func<CancellationToken, Task<string>>>(), TimeSpan.FromMinutes(5), Arg.Any<CancellationToken>())
            .Returns("value-1");

        var sut = new InstrumentedCacheProvider(inner);

        var result = await sut.GetOrSetAsync(
            "key-1",
            _ => Task.FromResult("value-1"),
            TimeSpan.FromMinutes(5),
            CancellationToken.None);

        result.ShouldBe("value-1");
    }

    [Fact]
    public async Task GetOrSetAsync_WhenInnerThrows_PropagatesException()
    {
        var inner = Substitute.For<ICacheProvider>();
        inner.GetOrSetAsync("key-err", Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>())
            .Returns<string>(x => throw new InvalidOperationException("cache down"));

        var sut = new InstrumentedCacheProvider(inner);

        await Should.ThrowAsync<InvalidOperationException>(
            () => sut.GetOrSetAsync(
                "key-err",
                _ => Task.FromResult("value"),
                TimeSpan.FromMinutes(5),
                CancellationToken.None));
    }

    [Fact]
    public async Task SetWithSlidingExpirationAsync_DelegatesToInner()
    {
        var inner = Substitute.For<ICacheProvider>();
        var sut = new InstrumentedCacheProvider(inner);

        await sut.SetWithSlidingExpirationAsync(
            "key-1", "value-1", TimeSpan.FromMinutes(5), TimeSpan.FromHours(1), CancellationToken.None);

        await inner.Received(1).SetWithSlidingExpirationAsync(
            "key-1", "value-1", TimeSpan.FromMinutes(5), TimeSpan.FromHours(1), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetWithSlidingExpirationAsync_WhenInnerThrows_PropagatesException()
    {
        var inner = Substitute.For<ICacheProvider>();
        inner.When(x => x.SetWithSlidingExpirationAsync(
                "key-err", Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new InvalidOperationException("cache down"));

        var sut = new InstrumentedCacheProvider(inner);

        await Should.ThrowAsync<InvalidOperationException>(
            () => sut.SetWithSlidingExpirationAsync(
                "key-err", "value", TimeSpan.FromMinutes(5), null, CancellationToken.None));
    }

    [Fact]
    public async Task RefreshAsync_WhenRefreshed_ReturnsTrue()
    {
        var inner = Substitute.For<ICacheProvider>();
        inner.RefreshAsync("key-1", Arg.Any<CancellationToken>())
            .Returns(true);

        var sut = new InstrumentedCacheProvider(inner);

        var result = await sut.RefreshAsync("key-1", CancellationToken.None);

        result.ShouldBeTrue();
        await inner.Received(1).RefreshAsync("key-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_WhenNotFound_ReturnsFalse()
    {
        var inner = Substitute.For<ICacheProvider>();
        inner.RefreshAsync("key-miss", Arg.Any<CancellationToken>())
            .Returns(false);

        var sut = new InstrumentedCacheProvider(inner);

        var result = await sut.RefreshAsync("key-miss", CancellationToken.None);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task RefreshAsync_WhenInnerThrows_PropagatesException()
    {
        var inner = Substitute.For<ICacheProvider>();
        inner.RefreshAsync("key-err", Arg.Any<CancellationToken>())
            .Returns<bool>(x => throw new InvalidOperationException("cache down"));

        var sut = new InstrumentedCacheProvider(inner);

        await Should.ThrowAsync<InvalidOperationException>(
            () => sut.RefreshAsync("key-err", CancellationToken.None));
    }
}
