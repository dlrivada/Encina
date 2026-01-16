using Encina.Caching.Garnet;
using StackExchange.Redis;

namespace Encina.UnitTests.Caching.Garnet;

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

    #region AddEncinaGarnetCache(string connectionString) Tests

    [Fact]
    public void AddEncinaGarnetCache_WithConnectionString_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddEncinaGarnetCache("localhost:3278"));
    }

    [Fact]
    public void AddEncinaGarnetCache_WithConnectionString_NullConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaGarnetCache((string)null!));
    }

    [Fact]
    public void AddEncinaGarnetCache_WithConnectionString_EmptyConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            services.AddEncinaGarnetCache(string.Empty));
    }

    #endregion

    #region AddEncinaGarnetCache(string, Action, Action) Tests

    [Fact]
    public void AddEncinaGarnetCache_WithOptions_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddEncinaGarnetCache(
                "localhost:3278",
                _ => { },
                _ => { }));
    }

    [Fact]
    public void AddEncinaGarnetCache_WithOptions_NullConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaGarnetCache(
                (string)null!,
                _ => { },
                _ => { }));
    }

    [Fact]
    public void AddEncinaGarnetCache_WithOptions_EmptyConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            services.AddEncinaGarnetCache(
                string.Empty,
                _ => { },
                _ => { }));
    }

    [Fact]
    public void AddEncinaGarnetCache_WithOptions_NullCacheOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaGarnetCache(
                "localhost:3278",
                null!,
                _ => { }));
    }

    [Fact]
    public void AddEncinaGarnetCache_WithOptions_NullLockOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaGarnetCache(
                "localhost:3278",
                _ => { },
                null!));
    }

    #endregion

    #region AddEncinaGarnetCache(IConnectionMultiplexer) Tests

    [Fact]
    public void AddEncinaGarnetCache_WithMultiplexer_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddEncinaGarnetCache(_connectionMultiplexer));
    }

    [Fact]
    public void AddEncinaGarnetCache_WithMultiplexer_NullMultiplexer_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaGarnetCache((IConnectionMultiplexer)null!));
    }

    [Fact]
    public void AddEncinaGarnetCache_WithMultiplexer_ValidParameters_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaGarnetCache(_connectionMultiplexer);

        // Assert
        result.ShouldBe(services);
        services.ShouldContain(sd => sd.ServiceType == typeof(IConnectionMultiplexer));
    }

    #endregion

    #region AddEncinaGarnetCache(IConnectionMultiplexer, Action, Action) Tests

    [Fact]
    public void AddEncinaGarnetCache_WithMultiplexerAndOptions_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddEncinaGarnetCache(
                _connectionMultiplexer,
                _ => { },
                _ => { }));
    }

    [Fact]
    public void AddEncinaGarnetCache_WithMultiplexerAndOptions_NullMultiplexer_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaGarnetCache(
                (IConnectionMultiplexer)null!,
                _ => { },
                _ => { }));
    }

    [Fact]
    public void AddEncinaGarnetCache_WithMultiplexerAndOptions_NullCacheOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaGarnetCache(
                _connectionMultiplexer,
                null!,
                _ => { }));
    }

    [Fact]
    public void AddEncinaGarnetCache_WithMultiplexerAndOptions_NullLockOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaGarnetCache(
                _connectionMultiplexer,
                _ => { },
                null!));
    }

    [Fact]
    public void AddEncinaGarnetCache_WithMultiplexerAndOptions_ValidParameters_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaGarnetCache(
            _connectionMultiplexer,
            _ => { },
            _ => { });

        // Assert
        result.ShouldBe(services);
        services.ShouldContain(sd => sd.ServiceType == typeof(IConnectionMultiplexer));
    }

    #endregion
}
