using Encina.Caching.Valkey;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

namespace Encina.GuardTests.Caching.Valkey;

/// <summary>
/// Guard tests for Encina.Caching.Valkey covering ThrowIfNull guards on all 4 overloads.
/// </summary>
[Trait("Category", "Guard")]
public sealed class ValkeyGuardTests
{
    // ─── Overload 1: (services, connectionString) ───

    [Fact]
    public void AddEncinaValkeyCache_String_NullServices_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaValkeyCache("localhost:6379"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void AddEncinaValkeyCache_String_InvalidConnectionString_Throws(string? cs)
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentException>(() =>
            services.AddEncinaValkeyCache(cs!));
    }

    // ─── Overload 2: (services, connectionString, cacheOptions, lockOptions) ───

    [Fact]
    public void AddEncinaValkeyCache_StringWithOptions_NullServices_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaValkeyCache("localhost:6379", _ => { }, _ => { }));
    }

    [Fact]
    public void AddEncinaValkeyCache_StringWithOptions_NullCacheOptions_Throws()
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaValkeyCache("localhost:6379", null!, _ => { }));
    }

    [Fact]
    public void AddEncinaValkeyCache_StringWithOptions_NullLockOptions_Throws()
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaValkeyCache("localhost:6379", _ => { }, null!));
    }

    // ─── Overload 3: (services, connectionMultiplexer) ───

    [Fact]
    public void AddEncinaValkeyCache_Multiplexer_NullServices_Throws()
    {
        var mux = Substitute.For<IConnectionMultiplexer>();
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaValkeyCache(mux));
    }

    [Fact]
    public void AddEncinaValkeyCache_Multiplexer_NullMultiplexer_Throws()
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaValkeyCache((IConnectionMultiplexer)null!));
    }

    // ─── Overload 4: (services, connectionMultiplexer, cacheOptions, lockOptions) ───

    [Fact]
    public void AddEncinaValkeyCache_MultiplexerWithOptions_NullServices_Throws()
    {
        var mux = Substitute.For<IConnectionMultiplexer>();
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaValkeyCache(mux, _ => { }, _ => { }));
    }

    [Fact]
    public void AddEncinaValkeyCache_MultiplexerWithOptions_NullMultiplexer_Throws()
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaValkeyCache((IConnectionMultiplexer)null!, _ => { }, _ => { }));
    }

    [Fact]
    public void AddEncinaValkeyCache_MultiplexerWithOptions_NullCacheOptions_Throws()
    {
        var mux = Substitute.For<IConnectionMultiplexer>();
        var services = new ServiceCollection();
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaValkeyCache(mux, null!, _ => { }));
    }

    [Fact]
    public void AddEncinaValkeyCache_MultiplexerWithOptions_NullLockOptions_Throws()
    {
        var mux = Substitute.For<IConnectionMultiplexer>();
        var services = new ServiceCollection();
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaValkeyCache(mux, _ => { }, null!));
    }
}
