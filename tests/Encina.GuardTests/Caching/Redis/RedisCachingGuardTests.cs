using Encina.Caching.Redis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using StackExchange.Redis;
using RedisDistributedLockProvider = Encina.Caching.Redis.RedisDistributedLockProvider;
using RedisLockOptions = Encina.Caching.Redis.RedisLockOptions;

namespace Encina.GuardTests.Caching.Redis;

/// <summary>
/// Guard tests for Encina.Caching.Redis covering constructor null guards for
/// providers (RedisCacheProvider, RedisDistributedLockProvider, RedisPubSubProvider)
/// and argument guards on ServiceCollectionExtensions overloads.
/// </summary>
[Trait("Category", "Guard")]
public sealed class RedisCachingGuardTests
{
    private static readonly IConnectionMultiplexer Mux = Substitute.For<IConnectionMultiplexer>();

    // ─── RedisCacheProvider constructor guards ───

    [Fact]
    public void CacheProvider_NullConnection_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new RedisCacheProvider(null!,
                Options.Create(new RedisCacheOptions()),
                NullLogger<RedisCacheProvider>.Instance));
    }

    [Fact]
    public void CacheProvider_NullOptions_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new RedisCacheProvider(Mux, null!,
                NullLogger<RedisCacheProvider>.Instance));
    }

    [Fact]
    public void CacheProvider_NullLogger_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new RedisCacheProvider(Mux,
                Options.Create(new RedisCacheOptions()), null!));
    }

    [Fact]
    public void CacheProvider_ValidArgs_Constructs()
    {
        var sut = new RedisCacheProvider(Mux,
            Options.Create(new RedisCacheOptions()),
            NullLogger<RedisCacheProvider>.Instance);
        sut.ShouldNotBeNull();
    }

    // ─── RedisDistributedLockProvider constructor guards ───

    [Fact]
    public void LockProvider_NullConnection_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new RedisDistributedLockProvider(null!,
                Options.Create(new RedisLockOptions()),
                NullLogger<RedisDistributedLockProvider>.Instance));
    }

    [Fact]
    public void LockProvider_NullOptions_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new RedisDistributedLockProvider(Mux, null!,
                NullLogger<RedisDistributedLockProvider>.Instance));
    }

    [Fact]
    public void LockProvider_NullLogger_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new RedisDistributedLockProvider(Mux,
                Options.Create(new RedisLockOptions()), null!));
    }

    [Fact]
    public void LockProvider_ValidArgs_Constructs()
    {
        var sut = new RedisDistributedLockProvider(Mux,
            Options.Create(new RedisLockOptions()),
            NullLogger<RedisDistributedLockProvider>.Instance);
        sut.ShouldNotBeNull();
    }

    // ─── RedisPubSubProvider constructor guards ───

    [Fact]
    public void PubSubProvider_NullConnection_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new RedisPubSubProvider(null!,
                NullLogger<RedisPubSubProvider>.Instance));
    }

    [Fact]
    public void PubSubProvider_NullLogger_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new RedisPubSubProvider(Mux, null!));
    }

    [Fact]
    public void PubSubProvider_ValidArgs_Constructs()
    {
        var sut = new RedisPubSubProvider(Mux,
            NullLogger<RedisPubSubProvider>.Instance);
        sut.ShouldNotBeNull();
    }

    // ─── ServiceCollectionExtensions overload 1: (services, connectionString) ───

    [Fact]
    public void AddEncinaRedisCache_String_NullServices_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaRedisCache("localhost:6379"));
    }

    [Fact]
    public void AddEncinaRedisCache_String_NullConnectionString_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ServiceCollection().AddEncinaRedisCache((string)null!));
    }

    [Fact]
    public void AddEncinaRedisCache_String_EmptyConnectionString_Throws()
    {
        Should.Throw<ArgumentException>(() =>
            new ServiceCollection().AddEncinaRedisCache(string.Empty));
    }

    // ─── ServiceCollectionExtensions overload 2: (services, connectionString, configure, configureLock) ───

    [Fact]
    public void AddEncinaRedisCache_StringWithOptions_NullServices_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaRedisCache("localhost:6379", _ => { }, _ => { }));
    }

    [Fact]
    public void AddEncinaRedisCache_StringWithOptions_NullCacheOptions_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ServiceCollection().AddEncinaRedisCache("localhost:6379", null!, _ => { }));
    }

    [Fact]
    public void AddEncinaRedisCache_StringWithOptions_NullLockOptions_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ServiceCollection().AddEncinaRedisCache("localhost:6379", _ => { }, null!));
    }

    // ─── ServiceCollectionExtensions overload 3: (services, multiplexer) ───

    [Fact]
    public void AddEncinaRedisCache_Multiplexer_NullServices_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaRedisCache(Mux));
    }

    [Fact]
    public void AddEncinaRedisCache_Multiplexer_NullMultiplexer_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ServiceCollection().AddEncinaRedisCache((IConnectionMultiplexer)null!));
    }

    // ─── ServiceCollectionExtensions overload 4: (services, multiplexer, configure, configureLock) ───

    [Fact]
    public void AddEncinaRedisCache_MultiplexerWithOptions_NullServices_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaRedisCache(Mux, _ => { }, _ => { }));
    }

    [Fact]
    public void AddEncinaRedisCache_MultiplexerWithOptions_NullMultiplexer_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ServiceCollection().AddEncinaRedisCache((IConnectionMultiplexer)null!, _ => { }, _ => { }));
    }

    [Fact]
    public void AddEncinaRedisCache_MultiplexerWithOptions_NullCacheOptions_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ServiceCollection().AddEncinaRedisCache(Mux, null!, _ => { }));
    }

    [Fact]
    public void AddEncinaRedisCache_MultiplexerWithOptions_NullLockOptions_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ServiceCollection().AddEncinaRedisCache(Mux, _ => { }, null!));
    }

    // ─── Options defaults ───

    [Fact]
    public void RedisCacheOptions_Defaults()
    {
        var options = new RedisCacheOptions();

        options.ShouldNotBeNull();
        options.DefaultExpiration.ShouldBe(TimeSpan.FromMinutes(5));
        options.Database.ShouldBe(0);
        options.KeyPrefix.ShouldBe(string.Empty);
        options.ProviderHealthCheck.ShouldNotBeNull();
    }

    [Fact]
    public void RedisLockOptions_Defaults()
    {
        var options = new RedisLockOptions();

        options.ShouldNotBeNull();
        options.Database.ShouldBe(0);
        options.KeyPrefix.ShouldBe(string.Empty);
    }
}
