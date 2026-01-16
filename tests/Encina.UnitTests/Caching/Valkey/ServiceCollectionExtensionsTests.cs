using Encina.Caching.Valkey;
using StackExchange.Redis;

namespace Encina.UnitTests.Caching.Valkey;

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

    #region AddEncinaValkeyCache(string connectionString) Tests

    [Fact]
    public void AddEncinaValkeyCache_WithConnectionString_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddEncinaValkeyCache("localhost:6379"));
    }

    [Fact]
    public void AddEncinaValkeyCache_WithConnectionString_NullConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaValkeyCache((string)null!));
    }

    [Fact]
    public void AddEncinaValkeyCache_WithConnectionString_EmptyConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            services.AddEncinaValkeyCache(string.Empty));
    }

    #endregion

    #region AddEncinaValkeyCache(string, Action, Action) Tests

    [Fact]
    public void AddEncinaValkeyCache_WithOptions_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddEncinaValkeyCache(
                "localhost:6379",
                _ => { },
                _ => { }));
    }

    [Fact]
    public void AddEncinaValkeyCache_WithOptions_NullConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaValkeyCache(
                (string)null!,
                _ => { },
                _ => { }));
    }

    [Fact]
    public void AddEncinaValkeyCache_WithOptions_EmptyConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            services.AddEncinaValkeyCache(
                string.Empty,
                _ => { },
                _ => { }));
    }

    [Fact]
    public void AddEncinaValkeyCache_WithOptions_NullCacheOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaValkeyCache(
                "localhost:6379",
                null!,
                _ => { }));
    }

    [Fact]
    public void AddEncinaValkeyCache_WithOptions_NullLockOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaValkeyCache(
                "localhost:6379",
                _ => { },
                null!));
    }

    #endregion

    #region AddEncinaValkeyCache(IConnectionMultiplexer) Tests

    [Fact]
    public void AddEncinaValkeyCache_WithMultiplexer_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddEncinaValkeyCache(_connectionMultiplexer));
    }

    [Fact]
    public void AddEncinaValkeyCache_WithMultiplexer_NullMultiplexer_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaValkeyCache((IConnectionMultiplexer)null!));
    }

    [Fact]
    public void AddEncinaValkeyCache_WithMultiplexer_ValidParameters_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaValkeyCache(_connectionMultiplexer);

        // Assert
        result.ShouldBe(services);
        services.ShouldContain(sd => sd.ServiceType == typeof(IConnectionMultiplexer));
    }

    #endregion

    #region AddEncinaValkeyCache(IConnectionMultiplexer, Action, Action) Tests

    [Fact]
    public void AddEncinaValkeyCache_WithMultiplexerAndOptions_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddEncinaValkeyCache(
                _connectionMultiplexer,
                _ => { },
                _ => { }));
    }

    [Fact]
    public void AddEncinaValkeyCache_WithMultiplexerAndOptions_NullMultiplexer_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaValkeyCache(
                (IConnectionMultiplexer)null!,
                _ => { },
                _ => { }));
    }

    [Fact]
    public void AddEncinaValkeyCache_WithMultiplexerAndOptions_NullCacheOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaValkeyCache(
                _connectionMultiplexer,
                null!,
                _ => { }));
    }

    [Fact]
    public void AddEncinaValkeyCache_WithMultiplexerAndOptions_NullLockOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaValkeyCache(
                _connectionMultiplexer,
                _ => { },
                null!));
    }

    [Fact]
    public void AddEncinaValkeyCache_WithMultiplexerAndOptions_ValidParameters_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaValkeyCache(
            _connectionMultiplexer,
            _ => { },
            _ => { });

        // Assert
        result.ShouldBe(services);
        services.ShouldContain(sd => sd.ServiceType == typeof(IConnectionMultiplexer));
    }

    #endregion
}
