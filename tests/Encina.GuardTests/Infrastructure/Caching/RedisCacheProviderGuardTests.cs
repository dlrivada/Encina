using Encina.Caching.Redis;
using StackExchange.Redis;

namespace Encina.GuardTests.Infrastructure.Caching;

/// <summary>
/// Guard tests for <see cref="RedisCacheProvider"/> (also the provider behind Valkey, Dragonfly, Garnet and KeyDB).
/// </summary>
public class RedisCacheProviderGuardTests
{
    private static readonly IOptions<RedisCacheOptions> Options_ = Options.Create(new RedisCacheOptions());

    private static RedisCacheProvider Create() =>
        new(Substitute.For<IConnectionMultiplexer>(), Options_, NullLogger<RedisCacheProvider>.Instance);

    [Fact]
    public void Constructor_NullConnection_Throws() =>
        Should.Throw<ArgumentNullException>(() => new RedisCacheProvider(null!, Options_, NullLogger<RedisCacheProvider>.Instance))
            .ParamName.ShouldBe("connection");

    [Fact]
    public void Constructor_NullOptions_Throws() =>
        Should.Throw<ArgumentNullException>(() => new RedisCacheProvider(Substitute.For<IConnectionMultiplexer>(), null!, NullLogger<RedisCacheProvider>.Instance))
            .ParamName.ShouldBe("options");

    [Fact]
    public void Constructor_NullLogger_Throws() =>
        Should.Throw<ArgumentNullException>(() => new RedisCacheProvider(Substitute.For<IConnectionMultiplexer>(), Options_, null!))
            .ParamName.ShouldBe("logger");

    [Fact]
    public async Task GetAsync_NullKey_Throws() =>
        (await Should.ThrowAsync<ArgumentNullException>(() => Create().GetAsync<string>(null!, default))).ParamName.ShouldBe("key");

    [Fact]
    public async Task SetAsync_NullKey_Throws() =>
        (await Should.ThrowAsync<ArgumentNullException>(() => Create().SetAsync(null!, "v", null, default))).ParamName.ShouldBe("key");

    [Fact]
    public async Task RemoveAsync_NullKey_Throws() =>
        (await Should.ThrowAsync<ArgumentNullException>(() => Create().RemoveAsync(null!, default))).ParamName.ShouldBe("key");

    [Fact]
    public async Task RemoveByPatternAsync_NullPattern_Throws() =>
        (await Should.ThrowAsync<ArgumentNullException>(() => Create().RemoveByPatternAsync(null!, default))).ParamName.ShouldBe("pattern");

    [Fact]
    public async Task ExistsAsync_NullKey_Throws() =>
        (await Should.ThrowAsync<ArgumentNullException>(() => Create().ExistsAsync(null!, default))).ParamName.ShouldBe("key");

    [Fact]
    public async Task GetOrSetAsync_NullKey_Throws() =>
        (await Should.ThrowAsync<ArgumentNullException>(() => Create().GetOrSetAsync<string>(null!, _ => Task.FromResult("v"), null, default))).ParamName.ShouldBe("key");

    [Fact]
    public async Task GetOrSetAsync_NullFactory_Throws() =>
        (await Should.ThrowAsync<ArgumentNullException>(() => Create().GetOrSetAsync<string>("k", null!, null, default))).ParamName.ShouldBe("factory");

    [Fact]
    public async Task SetWithSlidingExpirationAsync_NullKey_Throws() =>
        (await Should.ThrowAsync<ArgumentNullException>(() => Create().SetWithSlidingExpirationAsync(null!, "v", TimeSpan.FromMinutes(1), null, default))).ParamName.ShouldBe("key");

    [Fact]
    public async Task RefreshAsync_NullKey_Throws() =>
        (await Should.ThrowAsync<ArgumentNullException>(() => Create().RefreshAsync(null!, default))).ParamName.ShouldBe("key");
}
