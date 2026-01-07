namespace Encina.Caching.Dragonfly.Tests;

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

    #region AddEncinaDragonflyCache(string connectionString) Tests

    [Fact]
    public void AddEncinaDragonflyCache_WithConnectionString_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddEncinaDragonflyCache("localhost:6379"));
    }

    [Fact]
    public void AddEncinaDragonflyCache_WithConnectionString_NullConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaDragonflyCache((string)null!));
    }

    [Fact]
    public void AddEncinaDragonflyCache_WithConnectionString_EmptyConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            services.AddEncinaDragonflyCache(string.Empty));
    }

    #endregion

    #region AddEncinaDragonflyCache(string, Action, Action) Tests

    [Fact]
    public void AddEncinaDragonflyCache_WithOptions_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddEncinaDragonflyCache(
                "localhost:6379",
                _ => { },
                _ => { }));
    }

    [Fact]
    public void AddEncinaDragonflyCache_WithOptions_NullConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaDragonflyCache(
                (string)null!,
                _ => { },
                _ => { }));
    }

    [Fact]
    public void AddEncinaDragonflyCache_WithOptions_EmptyConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            services.AddEncinaDragonflyCache(
                string.Empty,
                _ => { },
                _ => { }));
    }

    [Fact]
    public void AddEncinaDragonflyCache_WithOptions_NullCacheOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaDragonflyCache(
                "localhost:6379",
                null!,
                _ => { }));
    }

    [Fact]
    public void AddEncinaDragonflyCache_WithOptions_NullLockOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaDragonflyCache(
                "localhost:6379",
                _ => { },
                null!));
    }

    #endregion

    #region AddEncinaDragonflyCache(IConnectionMultiplexer) Tests

    [Fact]
    public void AddEncinaDragonflyCache_WithMultiplexer_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddEncinaDragonflyCache(_connectionMultiplexer));
    }

    [Fact]
    public void AddEncinaDragonflyCache_WithMultiplexer_NullMultiplexer_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaDragonflyCache((IConnectionMultiplexer)null!));
    }

    [Fact]
    public void AddEncinaDragonflyCache_WithMultiplexer_ValidParameters_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaDragonflyCache(_connectionMultiplexer);

        // Assert
        result.ShouldBe(services);
        services.ShouldContain(sd => sd.ServiceType == typeof(IConnectionMultiplexer));
    }

    #endregion

    #region AddEncinaDragonflyCache(IConnectionMultiplexer, Action, Action) Tests

    [Fact]
    public void AddEncinaDragonflyCache_WithMultiplexerAndOptions_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddEncinaDragonflyCache(
                _connectionMultiplexer,
                _ => { },
                _ => { }));
    }

    [Fact]
    public void AddEncinaDragonflyCache_WithMultiplexerAndOptions_NullMultiplexer_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaDragonflyCache(
                (IConnectionMultiplexer)null!,
                _ => { },
                _ => { }));
    }

    [Fact]
    public void AddEncinaDragonflyCache_WithMultiplexerAndOptions_NullCacheOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaDragonflyCache(
                _connectionMultiplexer,
                null!,
                _ => { }));
    }

    [Fact]
    public void AddEncinaDragonflyCache_WithMultiplexerAndOptions_NullLockOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaDragonflyCache(
                _connectionMultiplexer,
                _ => { },
                null!));
    }

    [Fact]
    public void AddEncinaDragonflyCache_WithMultiplexerAndOptions_ValidParameters_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaDragonflyCache(
            _connectionMultiplexer,
            _ => { },
            _ => { });

        // Assert
        result.ShouldBe(services);
        services.ShouldContain(sd => sd.ServiceType == typeof(IConnectionMultiplexer));
    }

    #endregion
}
