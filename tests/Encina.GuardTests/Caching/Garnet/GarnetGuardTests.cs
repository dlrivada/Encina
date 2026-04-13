using Encina.Caching.Garnet;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

namespace Encina.GuardTests.Caching.Garnet;

/// <summary>
/// Guard tests for Encina.Caching.Garnet covering ThrowIfNull guards on all 4 overloads.
/// </summary>
[Trait("Category", "Guard")]
public sealed class GarnetGuardTests
{
    // ─── Overload 1: (services, connectionString) ───

    [Fact]
    public void AddEncinaGarnetCache_String_NullServices_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaGarnetCache("localhost:3278"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void AddEncinaGarnetCache_String_InvalidConnectionString_Throws(string? cs)
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentException>(() =>
            services.AddEncinaGarnetCache(cs!));
    }

    // ─── Overload 2: (services, connectionString, cacheOptions, lockOptions) ───

    [Fact]
    public void AddEncinaGarnetCache_StringWithOptions_NullServices_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaGarnetCache("localhost:3278", _ => { }, _ => { }));
    }

    [Fact]
    public void AddEncinaGarnetCache_StringWithOptions_NullCacheOptions_Throws()
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaGarnetCache("localhost:3278", null!, _ => { }));
    }

    [Fact]
    public void AddEncinaGarnetCache_StringWithOptions_NullLockOptions_Throws()
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaGarnetCache("localhost:3278", _ => { }, null!));
    }

    // ─── Overload 3: (services, connectionMultiplexer) ───

    [Fact]
    public void AddEncinaGarnetCache_Multiplexer_NullServices_Throws()
    {
        var mux = Substitute.For<IConnectionMultiplexer>();
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaGarnetCache(mux));
    }

    [Fact]
    public void AddEncinaGarnetCache_Multiplexer_NullMultiplexer_Throws()
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaGarnetCache((IConnectionMultiplexer)null!));
    }

    // ─── Overload 4: (services, connectionMultiplexer, cacheOptions, lockOptions) ───

    [Fact]
    public void AddEncinaGarnetCache_MultiplexerWithOptions_NullServices_Throws()
    {
        var mux = Substitute.For<IConnectionMultiplexer>();
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaGarnetCache(mux, _ => { }, _ => { }));
    }

    [Fact]
    public void AddEncinaGarnetCache_MultiplexerWithOptions_NullMultiplexer_Throws()
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaGarnetCache((IConnectionMultiplexer)null!, _ => { }, _ => { }));
    }

    [Fact]
    public void AddEncinaGarnetCache_MultiplexerWithOptions_NullCacheOptions_Throws()
    {
        var mux = Substitute.For<IConnectionMultiplexer>();
        var services = new ServiceCollection();
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaGarnetCache(mux, null!, _ => { }));
    }

    [Fact]
    public void AddEncinaGarnetCache_MultiplexerWithOptions_NullLockOptions_Throws()
    {
        var mux = Substitute.For<IConnectionMultiplexer>();
        var services = new ServiceCollection();
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaGarnetCache(mux, _ => { }, null!));
    }
}
