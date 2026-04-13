using Encina.Caching.KeyDB;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

namespace Encina.GuardTests.Caching.KeyDB;

/// <summary>
/// Guard tests for Encina.Caching.KeyDB covering ThrowIfNull guards on all 4 overloads.
/// </summary>
[Trait("Category", "Guard")]
public sealed class KeyDBGuardTests
{
    // ─── Overload 1: (services, connectionString) ───

    [Fact]
    public void AddEncinaKeyDBCache_String_NullServices_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaKeyDBCache("localhost:6379"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void AddEncinaKeyDBCache_String_InvalidConnectionString_Throws(string? cs)
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentException>(() =>
            services.AddEncinaKeyDBCache(cs!));
    }

    // ─── Overload 2: (services, connectionString, cacheOptions, lockOptions) ───

    [Fact]
    public void AddEncinaKeyDBCache_StringWithOptions_NullServices_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaKeyDBCache("localhost:6379", _ => { }, _ => { }));
    }

    [Fact]
    public void AddEncinaKeyDBCache_StringWithOptions_NullCacheOptions_Throws()
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaKeyDBCache("localhost:6379", null!, _ => { }));
    }

    [Fact]
    public void AddEncinaKeyDBCache_StringWithOptions_NullLockOptions_Throws()
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaKeyDBCache("localhost:6379", _ => { }, null!));
    }

    // ─── Overload 3: (services, connectionMultiplexer) ───

    [Fact]
    public void AddEncinaKeyDBCache_Multiplexer_NullServices_Throws()
    {
        var mux = Substitute.For<IConnectionMultiplexer>();
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaKeyDBCache(mux));
    }

    [Fact]
    public void AddEncinaKeyDBCache_Multiplexer_NullMultiplexer_Throws()
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaKeyDBCache((IConnectionMultiplexer)null!));
    }

    // ─── Overload 4: (services, connectionMultiplexer, cacheOptions, lockOptions) ───

    [Fact]
    public void AddEncinaKeyDBCache_MultiplexerWithOptions_NullServices_Throws()
    {
        var mux = Substitute.For<IConnectionMultiplexer>();
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaKeyDBCache(mux, _ => { }, _ => { }));
    }

    [Fact]
    public void AddEncinaKeyDBCache_MultiplexerWithOptions_NullMultiplexer_Throws()
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaKeyDBCache((IConnectionMultiplexer)null!, _ => { }, _ => { }));
    }

    [Fact]
    public void AddEncinaKeyDBCache_MultiplexerWithOptions_NullCacheOptions_Throws()
    {
        var mux = Substitute.For<IConnectionMultiplexer>();
        var services = new ServiceCollection();
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaKeyDBCache(mux, null!, _ => { }));
    }

    [Fact]
    public void AddEncinaKeyDBCache_MultiplexerWithOptions_NullLockOptions_Throws()
    {
        var mux = Substitute.For<IConnectionMultiplexer>();
        var services = new ServiceCollection();
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaKeyDBCache(mux, _ => { }, null!));
    }
}
