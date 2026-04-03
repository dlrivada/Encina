using Encina.Caching;
using Encina.OpenTelemetry.QueryCache;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.OpenTelemetry.QueryCache;

/// <summary>
/// Unit tests for <see cref="InstrumentedCacheProvider"/>.
/// </summary>
public sealed class InstrumentedCacheProviderTests
{
    private readonly ICacheProvider _inner;
    private readonly InstrumentedCacheProvider _sut;

    public InstrumentedCacheProviderTests()
    {
        _inner = Substitute.For<ICacheProvider>();
        _sut = new InstrumentedCacheProvider(_inner);
    }

    [Fact]
    public async Task GetAsync_DelegatesToInner()
    {
        _inner.GetAsync<string>("key-1", Arg.Any<CancellationToken>())
            .Returns("cached-value");

        var result = await _sut.GetAsync<string>("key-1", CancellationToken.None);

        result.ShouldBe("cached-value");
    }

    [Fact]
    public async Task GetAsync_WhenMiss_ReturnsNull()
    {
        _inner.GetAsync<string>("key-miss", Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var result = await _sut.GetAsync<string>("key-miss", CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task SetAsync_DelegatesToInner()
    {
        await _sut.SetAsync("key-1", "value", TimeSpan.FromMinutes(5), CancellationToken.None);

        await _inner.Received(1).SetAsync("key-1", "value", TimeSpan.FromMinutes(5), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveAsync_DelegatesToInner()
    {
        await _sut.RemoveAsync("key-1", CancellationToken.None);

        await _inner.Received(1).RemoveAsync("key-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveByPatternAsync_DelegatesToInner()
    {
        await _sut.RemoveByPatternAsync("prefix:*", CancellationToken.None);

        await _inner.Received(1).RemoveByPatternAsync("prefix:*", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExistsAsync_DelegatesToInner()
    {
        _inner.ExistsAsync("key-1", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.ExistsAsync("key-1", CancellationToken.None);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task GetOrSetAsync_DelegatesToInner()
    {
        _inner.GetOrSetAsync("key-1", Arg.Any<Func<CancellationToken, Task<string>>>(), TimeSpan.FromMinutes(5), Arg.Any<CancellationToken>())
            .Returns("factory-value");

        var result = await _sut.GetOrSetAsync("key-1", _ => Task.FromResult("factory-value"), TimeSpan.FromMinutes(5), CancellationToken.None);

        result.ShouldBe("factory-value");
    }

    [Fact]
    public async Task SetWithSlidingExpirationAsync_DelegatesToInner()
    {
        await _sut.SetWithSlidingExpirationAsync("key-1", "value", TimeSpan.FromMinutes(5), TimeSpan.FromHours(1), CancellationToken.None);

        await _inner.Received(1).SetWithSlidingExpirationAsync("key-1", "value", TimeSpan.FromMinutes(5), TimeSpan.FromHours(1), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_DelegatesToInner()
    {
        _inner.RefreshAsync("key-1", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.RefreshAsync("key-1", CancellationToken.None);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task GetAsync_WhenInnerThrows_PropagatesException()
    {
        _inner.GetAsync<string>("key-1", Arg.Any<CancellationToken>())
            .Returns<string?>(_ => throw new InvalidOperationException("cache down"));

        await Should.ThrowAsync<InvalidOperationException>(
            async () => await _sut.GetAsync<string>("key-1", CancellationToken.None));
    }
}
