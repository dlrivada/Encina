using Encina.Caching.Hybrid;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Caching.Hybrid;

/// <summary>
/// Tag tracking, tag removal and glob removal of <see cref="HybridCacheProvider"/>.
/// </summary>
public sealed class HybridCacheProviderTaggingTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly FakeLogger<HybridCacheProvider> _logger = new();
    private readonly HybridCacheProvider _sut;

    public HybridCacheProviderTaggingTests()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        services.AddDistributedMemoryCache();
        _serviceProvider = services.BuildServiceProvider();
        _sut = new HybridCacheProvider(
            _serviceProvider.GetRequiredService<HybridCache>(),
            Options.Create(new HybridCacheProviderOptions()),
            _logger);
    }

    public void Dispose() => _serviceProvider.Dispose();

    private Task<string> Tagged(string key, params string[] tags) =>
        _sut.GetOrSetAsync(key, _ => Task.FromResult("v"), TimeSpan.FromMinutes(1), tags, default);

    private int LastRemovedByPattern() =>
        int.Parse(
            System.Text.RegularExpressions.Regex.Match(
                _logger.Collector.GetSnapshot().Last(r => r.Message.Contains("remove by pattern", StringComparison.Ordinal)).Message,
                @"removed (\d+) keys").Groups[1].Value,
            System.Globalization.CultureInfo.InvariantCulture);

    [Fact]
    public async Task GlobPattern_RemovesEveryTrackedMatchingKey()
    {
        await Tagged("orders:1", "orders");
        await Tagged("orders:2", "orders");
        await Tagged("users:1", "users");

        await _sut.RemoveByPatternAsync("orders:*", default);

        LastRemovedByPattern().ShouldBe(2);
    }

    [Theory]
    [InlineData("tag:orders")]
    [InlineData("#orders")]
    public async Task TagPattern_RemovesByTag_AndForgetsTheTrackedKeys(string pattern)
    {
        await Tagged("orders:1", "orders");

        await _sut.RemoveByPatternAsync(pattern, default);
        await _sut.RemoveByPatternAsync("orders:*", default);

        LastRemovedByPattern().ShouldBe(0);
    }

    [Fact]
    public async Task RemoveByTag_KeepsKeysThatStillHaveAnotherTag()
    {
        await Tagged("orders:1", "orders", "recent");

        await _sut.RemoveByTagAsync("orders", default);
        await _sut.RemoveByPatternAsync("orders:*", default);

        LastRemovedByPattern().ShouldBe(1);
    }

    [Fact]
    public async Task RemoveByTag_ForTheLastTagOfAKey_ForgetsTheKey()
    {
        await Tagged("orders:1", "orders");

        await _sut.RemoveByTagAsync("orders", default);
        await _sut.RemoveByPatternAsync("orders:*", default);

        LastRemovedByPattern().ShouldBe(0);
    }

    [Fact]
    public async Task RemoveAsync_ForgetsTheTagsOfTheKey()
    {
        await Tagged("orders:1", "orders");

        await _sut.RemoveAsync("orders:1", default);
        await _sut.RemoveByPatternAsync("orders:*", default);

        LastRemovedByPattern().ShouldBe(0);
    }
}
