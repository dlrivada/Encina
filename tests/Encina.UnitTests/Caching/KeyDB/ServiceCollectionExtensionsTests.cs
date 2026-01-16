using Encina.Caching.KeyDB;
using StackExchange.Redis;

namespace Encina.UnitTests.Caching.KeyDB;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public ServiceCollectionExtensionsTests()
    {
        _connectionMultiplexer = Substitute.For<IConnectionMultiplexer>();
    }

    #region AddEncinaKeyDBCache(string connectionString) Tests

    [Fact]
    public void AddEncinaKeyDBCache_WithConnectionString_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddEncinaKeyDBCache("localhost:6379"));
    }

    [Fact]
    public void AddEncinaKeyDBCache_WithConnectionString_NullConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaKeyDBCache((string)null!));
    }

    [Fact]
    public void AddEncinaKeyDBCache_WithConnectionString_EmptyConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            services.AddEncinaKeyDBCache(string.Empty));
    }

    #endregion

    #region AddEncinaKeyDBCache(string, Action, Action) Tests

    [Fact]
    public void AddEncinaKeyDBCache_WithOptions_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddEncinaKeyDBCache(
                "localhost:6379",
                _ => { },
                _ => { }));
    }

    [Fact]
    public void AddEncinaKeyDBCache_WithOptions_NullConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaKeyDBCache(
                (string)null!,
                _ => { },
                _ => { }));
    }

    [Fact]
    public void AddEncinaKeyDBCache_WithOptions_EmptyConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            services.AddEncinaKeyDBCache(
                string.Empty,
                _ => { },
                _ => { }));
    }

    [Fact]
    public void AddEncinaKeyDBCache_WithOptions_NullCacheOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaKeyDBCache(
                "localhost:6379",
                null!,
                _ => { }));
    }

    [Fact]
    public void AddEncinaKeyDBCache_WithOptions_NullLockOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaKeyDBCache(
                "localhost:6379",
                _ => { },
                null!));
    }

    #endregion

    #region AddEncinaKeyDBCache(IConnectionMultiplexer) Tests

    [Fact]
    public void AddEncinaKeyDBCache_WithMultiplexer_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddEncinaKeyDBCache(_connectionMultiplexer));
    }

    [Fact]
    public void AddEncinaKeyDBCache_WithMultiplexer_NullMultiplexer_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaKeyDBCache((IConnectionMultiplexer)null!));
    }

    [Fact]
    public void AddEncinaKeyDBCache_WithMultiplexer_ValidParameters_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaKeyDBCache(_connectionMultiplexer);

        // Assert
        result.ShouldBe(services);
        services.ShouldContain(sd => sd.ServiceType == typeof(IConnectionMultiplexer));
    }

    #endregion

    #region AddEncinaKeyDBCache(IConnectionMultiplexer, Action, Action) Tests

    [Fact]
    public void AddEncinaKeyDBCache_WithMultiplexerAndOptions_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddEncinaKeyDBCache(
                _connectionMultiplexer,
                _ => { },
                _ => { }));
    }

    [Fact]
    public void AddEncinaKeyDBCache_WithMultiplexerAndOptions_NullMultiplexer_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaKeyDBCache(
                (IConnectionMultiplexer)null!,
                _ => { },
                _ => { }));
    }

    [Fact]
    public void AddEncinaKeyDBCache_WithMultiplexerAndOptions_NullCacheOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaKeyDBCache(
                _connectionMultiplexer,
                null!,
                _ => { }));
    }

    [Fact]
    public void AddEncinaKeyDBCache_WithMultiplexerAndOptions_NullLockOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaKeyDBCache(
                _connectionMultiplexer,
                _ => { },
                null!));
    }

    [Fact]
    public void AddEncinaKeyDBCache_WithMultiplexerAndOptions_ValidParameters_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaKeyDBCache(
            _connectionMultiplexer,
            _ => { },
            _ => { });

        // Assert
        result.ShouldBe(services);
        services.ShouldContain(sd => sd.ServiceType == typeof(IConnectionMultiplexer));
    }

    #endregion
}
