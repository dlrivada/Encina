using Encina.DistributedLock;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Encina.Caching.Redis.Tests;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public ServiceCollectionExtensionsTests()
    {
        _connectionMultiplexer = Substitute.For<IConnectionMultiplexer>();
        _connectionMultiplexer.GetDatabase(Arg.Any<int>(), Arg.Any<object?>())
            .Returns(Substitute.For<IDatabase>());
        _connectionMultiplexer.GetSubscriber(Arg.Any<object?>())
            .Returns(Substitute.For<ISubscriber>());
    }

    #region AddEncinaRedisCache(IConnectionMultiplexer) Tests

    [Fact]
    public void AddEncinaRedisCache_WithMultiplexer_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddEncinaRedisCache(_connectionMultiplexer));
    }

    [Fact]
    public void AddEncinaRedisCache_WithMultiplexer_NullMultiplexer_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaRedisCache((IConnectionMultiplexer)null!));
    }

    [Fact]
    public void AddEncinaRedisCache_WithMultiplexer_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaRedisCache(_connectionMultiplexer);

        // Assert
        result.ShouldBe(services);
        services.ShouldContain(sd => sd.ServiceType == typeof(IConnectionMultiplexer));
        services.ShouldContain(sd => sd.ServiceType == typeof(ICacheProvider));
        services.ShouldContain(sd => sd.ServiceType == typeof(IPubSubProvider));
        services.ShouldContain(sd => sd.ServiceType == typeof(IDistributedLockProvider));
    }

    #endregion

    #region AddEncinaRedisCache(IConnectionMultiplexer, Action, Action) Tests

    [Fact]
    public void AddEncinaRedisCache_WithMultiplexerAndOptions_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddEncinaRedisCache(
                _connectionMultiplexer,
                _ => { },
                _ => { }));
    }

    [Fact]
    public void AddEncinaRedisCache_WithMultiplexerAndOptions_NullMultiplexer_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaRedisCache(
                (IConnectionMultiplexer)null!,
                _ => { },
                _ => { }));
    }

    [Fact]
    public void AddEncinaRedisCache_WithMultiplexerAndOptions_NullCacheOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaRedisCache(
                _connectionMultiplexer,
                null!,
                _ => { }));
    }

    [Fact]
    public void AddEncinaRedisCache_WithMultiplexerAndOptions_NullLockOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaRedisCache(
                _connectionMultiplexer,
                _ => { },
                null!));
    }

    [Fact]
    public void AddEncinaRedisCache_WithMultiplexerAndOptions_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaRedisCache(
            _connectionMultiplexer,
            _ => { },
            _ => { });

        // Assert
        result.ShouldBe(services);
        services.ShouldContain(sd => sd.ServiceType == typeof(IConnectionMultiplexer));
        services.ShouldContain(sd => sd.ServiceType == typeof(ICacheProvider));
        services.ShouldContain(sd => sd.ServiceType == typeof(IPubSubProvider));
        services.ShouldContain(sd => sd.ServiceType == typeof(IDistributedLockProvider));
    }

    #endregion

    #region AddEncinaRedisCache(string) Tests

    [Fact]
    public void AddEncinaRedisCache_WithConnectionString_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddEncinaRedisCache("localhost:6379"));
    }

    [Fact]
    public void AddEncinaRedisCache_WithConnectionString_NullConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaRedisCache((string)null!));
    }

    [Fact]
    public void AddEncinaRedisCache_WithConnectionString_EmptyConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            services.AddEncinaRedisCache(string.Empty));
    }

    #endregion

    #region AddEncinaRedisCache(string, Action, Action) Tests

    [Fact]
    public void AddEncinaRedisCache_WithConnectionStringAndOptions_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddEncinaRedisCache(
                "localhost:6379",
                _ => { },
                _ => { }));
    }

    [Fact]
    public void AddEncinaRedisCache_WithConnectionStringAndOptions_NullConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaRedisCache(
                (string)null!,
                _ => { },
                _ => { }));
    }

    [Fact]
    public void AddEncinaRedisCache_WithConnectionStringAndOptions_EmptyConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            services.AddEncinaRedisCache(
                string.Empty,
                _ => { },
                _ => { }));
    }

    [Fact]
    public void AddEncinaRedisCache_WithConnectionStringAndOptions_NullCacheOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaRedisCache(
                "localhost:6379",
                null!,
                _ => { }));
    }

    [Fact]
    public void AddEncinaRedisCache_WithConnectionStringAndOptions_NullLockOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaRedisCache(
                "localhost:6379",
                _ => { },
                null!));
    }

    #endregion
}
