using Encina.Caching.Dragonfly;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

namespace Encina.GuardTests.Caching.Dragonfly;

/// <summary>
/// Guard tests for Encina.Caching.Dragonfly covering ThrowIfNull guards on all 4 overloads.
/// </summary>
[Trait("Category", "Guard")]
public sealed class DragonflyGuardTests
{
    [Fact]
    public void AddEncinaDragonflyCache_String_NullServices_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaDragonflyCache("localhost:6379"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void AddEncinaDragonflyCache_String_InvalidConnectionString_Throws(string? cs)
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentException>(() => services.AddEncinaDragonflyCache(cs!));
    }

    [Fact]
    public void AddEncinaDragonflyCache_StringWithOptions_NullServices_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaDragonflyCache("localhost:6379", _ => { }, _ => { }));
    }

    [Fact]
    public void AddEncinaDragonflyCache_StringWithOptions_NullCacheOptions_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ServiceCollection().AddEncinaDragonflyCache("localhost:6379", null!, _ => { }));
    }

    [Fact]
    public void AddEncinaDragonflyCache_StringWithOptions_NullLockOptions_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ServiceCollection().AddEncinaDragonflyCache("localhost:6379", _ => { }, null!));
    }

    [Fact]
    public void AddEncinaDragonflyCache_Multiplexer_NullServices_Throws()
    {
        var mux = Substitute.For<IConnectionMultiplexer>();
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaDragonflyCache(mux));
    }

    [Fact]
    public void AddEncinaDragonflyCache_Multiplexer_NullMultiplexer_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ServiceCollection().AddEncinaDragonflyCache((IConnectionMultiplexer)null!));
    }

    [Fact]
    public void AddEncinaDragonflyCache_MultiplexerWithOptions_NullServices_Throws()
    {
        var mux = Substitute.For<IConnectionMultiplexer>();
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaDragonflyCache(mux, _ => { }, _ => { }));
    }

    [Fact]
    public void AddEncinaDragonflyCache_MultiplexerWithOptions_NullMultiplexer_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ServiceCollection().AddEncinaDragonflyCache((IConnectionMultiplexer)null!, _ => { }, _ => { }));
    }

    [Fact]
    public void AddEncinaDragonflyCache_MultiplexerWithOptions_NullCacheOptions_Throws()
    {
        var mux = Substitute.For<IConnectionMultiplexer>();
        Should.Throw<ArgumentNullException>(() =>
            new ServiceCollection().AddEncinaDragonflyCache(mux, null!, _ => { }));
    }

    [Fact]
    public void AddEncinaDragonflyCache_MultiplexerWithOptions_NullLockOptions_Throws()
    {
        var mux = Substitute.For<IConnectionMultiplexer>();
        Should.Throw<ArgumentNullException>(() =>
            new ServiceCollection().AddEncinaDragonflyCache(mux, _ => { }, null!));
    }
}
