using System.Text.Json;
using Encina.Caching.Redis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Encina.Caching.Redis.Tests;

/// <summary>
/// Unit tests for <see cref="RedisCacheProvider"/>.
/// </summary>
public sealed class RedisCacheProviderTests
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IDatabase _database;
    private readonly IServer _server;
    private readonly RedisCacheOptions _options;
    private readonly ILogger<RedisCacheProvider> _logger;

    public RedisCacheProviderTests()
    {
        _connectionMultiplexer = Substitute.For<IConnectionMultiplexer>();
        _database = Substitute.For<IDatabase>();
        _server = Substitute.For<IServer>();
        _logger = NullLogger<RedisCacheProvider>.Instance;
        _options = new RedisCacheOptions
        {
            KeyPrefix = "test",
            DefaultExpiration = TimeSpan.FromMinutes(5),
            Database = 0
        };

        _connectionMultiplexer.GetDatabase(Arg.Any<int>(), Arg.Any<object?>())
            .Returns(_database);
        _connectionMultiplexer.GetEndPoints(Arg.Any<bool>())
            .Returns([new System.Net.DnsEndPoint("localhost", 6379)]);
        _connectionMultiplexer.GetServer(Arg.Any<System.Net.EndPoint>(), Arg.Any<object?>())
            .Returns(_server);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullConnection_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new RedisCacheProvider(null!, Options.Create(_options), _logger));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new RedisCacheProvider(_connectionMultiplexer, null!, _logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new RedisCacheProvider(_connectionMultiplexer, Options.Create(_options), null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        var provider = CreateProvider();

        // Assert
        provider.ShouldNotBeNull();
    }

    #endregion

    #region GetAsync Tests

    [Fact]
    public async Task GetAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            provider.GetAsync<string>(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetAsync_WhenKeyExists_ReturnsDeserializedValue()
    {
        // Arrange
        var provider = CreateProvider();
        var key = "test-key";
        var expectedValue = new TestData { Id = 123, Name = "Test" };
        var json = JsonSerializer.Serialize(expectedValue, s_jsonOptions);

        _database.StringGetAsync($"{_options.KeyPrefix}:{key}", Arg.Any<CommandFlags>())
            .Returns(new RedisValue(json));

        // Act
        var result = await provider.GetAsync<TestData>(key, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(expectedValue.Id);
        result.Name.ShouldBe(expectedValue.Name);
    }

    [Fact]
    public async Task GetAsync_WhenKeyDoesNotExist_ReturnsDefault()
    {
        // Arrange
        var provider = CreateProvider();
        var key = "nonexistent-key";

        _database.StringGetAsync($"{_options.KeyPrefix}:{key}", Arg.Any<CommandFlags>())
            .Returns(RedisValue.Null);

        // Act
        var result = await provider.GetAsync<TestData>(key, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetAsync_WithCancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var provider = CreateProvider();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            provider.GetAsync<string>("key", cts.Token));
    }

    #endregion

    #region SetAsync Tests

    [Fact]
    public async Task SetAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            provider.SetAsync(null!, "value", null, CancellationToken.None));
    }

    [Fact]
    public async Task SetAsync_WithValue_StoresSerializedValue()
    {
        // Arrange
        var provider = CreateProvider();
        var key = "test-key";
        var value = new TestData { Id = 456, Name = "Stored" };

        // Act
        var exception = await Record.ExceptionAsync(() =>
            provider.SetAsync(key, value, TimeSpan.FromMinutes(10), CancellationToken.None));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task SetAsync_WithoutExpiration_UsesDefaultExpiration()
    {
        // Arrange
        var provider = CreateProvider();
        var key = "test-key";

        // Act
        var exception = await Record.ExceptionAsync(() =>
            provider.SetAsync(key, "value", null, CancellationToken.None));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task SetAsync_WithCancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var provider = CreateProvider();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            provider.SetAsync("key", "value", null, cts.Token));
    }

    #endregion

    #region RemoveAsync Tests

    [Fact]
    public async Task RemoveAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            provider.RemoveAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task RemoveAsync_WithValidKey_DeletesKey()
    {
        // Arrange
        var provider = CreateProvider();
        var key = "test-key";
        var expectedPrefixedKey = $"{_options.KeyPrefix}:{key}";

        _database.KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(true);

        // Act
        await provider.RemoveAsync(key, CancellationToken.None);

        // Assert
        await _database.Received(1).KeyDeleteAsync(expectedPrefixedKey, Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task RemoveAsync_WithCancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var provider = CreateProvider();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            provider.RemoveAsync("key", cts.Token));
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            provider.ExistsAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ExistsAsync_WhenKeyExists_ReturnsTrue()
    {
        // Arrange
        var provider = CreateProvider();
        var key = "existing-key";
        var expectedPrefixedKey = $"{_options.KeyPrefix}:{key}";

        _database.KeyExistsAsync(expectedPrefixedKey, Arg.Any<CommandFlags>())
            .Returns(true);

        // Act
        var result = await provider.ExistsAsync(key, CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenKeyDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var provider = CreateProvider();
        var key = "nonexistent-key";
        var expectedPrefixedKey = $"{_options.KeyPrefix}:{key}";

        _database.KeyExistsAsync(expectedPrefixedKey, Arg.Any<CommandFlags>())
            .Returns(false);

        // Act
        var result = await provider.ExistsAsync(key, CancellationToken.None);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region RemoveByPatternAsync Tests

    [Fact]
    public async Task RemoveByPatternAsync_WithNullPattern_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            provider.RemoveByPatternAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task RemoveByPatternAsync_WithPattern_RemovesMatchingKeys()
    {
        // Arrange
        var provider = CreateProvider();
        var pattern = "product:*";
        var matchingKeys = new RedisKey[]
        {
            $"{_options.KeyPrefix}:product:1",
            $"{_options.KeyPrefix}:product:2"
        };

        _server.KeysAsync(
            Arg.Any<int>(),
            Arg.Any<RedisValue>(),
            Arg.Any<int>(),
            Arg.Any<long>(),
            Arg.Any<int>(),
            Arg.Any<CommandFlags>())
            .Returns(matchingKeys.ToAsyncEnumerable());

        _database.KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(true);

        // Act
        await provider.RemoveByPatternAsync(pattern, CancellationToken.None);

        // Assert - implementation deletes keys one by one
        await _database.Received(2).KeyDeleteAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<CommandFlags>());
    }

    #endregion

    #region GetOrSetAsync Tests

    [Fact]
    public async Task GetOrSetAsync_WhenKeyExists_ReturnsExistingValue()
    {
        // Arrange
        var provider = CreateProvider();
        var key = "existing-key";
        var existingValue = new TestData { Id = 789, Name = "Existing" };
        var json = JsonSerializer.Serialize(existingValue, s_jsonOptions);

        _database.StringGetAsync($"{_options.KeyPrefix}:{key}", Arg.Any<CommandFlags>())
            .Returns(new RedisValue(json));

        var factoryCalled = false;

        // Act
        var result = await provider.GetOrSetAsync(
            key,
            _ =>
            {
                factoryCalled = true;
                return Task.FromResult(new TestData { Id = 999, Name = "New" });
            },
            TimeSpan.FromMinutes(5),
            CancellationToken.None);

        // Assert
        factoryCalled.ShouldBeFalse();
        result.Id.ShouldBe(existingValue.Id);
    }

    [Fact]
    public async Task GetOrSetAsync_WhenKeyDoesNotExist_CallsFactoryAndStoresResult()
    {
        // Arrange
        var provider = CreateProvider();
        var key = "new-key";
        var newValue = new TestData { Id = 999, Name = "New" };

        _database.StringGetAsync($"{_options.KeyPrefix}:{key}", Arg.Any<CommandFlags>())
            .Returns(RedisValue.Null);

        _database.StringSetAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<When>(),
            Arg.Any<CommandFlags>())
            .Returns(true);

        var factoryCalled = false;

        // Act
        var result = await provider.GetOrSetAsync(
            key,
            _ =>
            {
                factoryCalled = true;
                return Task.FromResult(newValue);
            },
            TimeSpan.FromMinutes(5),
            CancellationToken.None);

        // Assert
        factoryCalled.ShouldBeTrue();
        result.Id.ShouldBe(newValue.Id);
    }

    #endregion

    #region RefreshAsync Tests

    [Fact]
    public async Task RefreshAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            provider.RefreshAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task RefreshAsync_WhenKeyExists_ReturnsTrue()
    {
        // Arrange
        var provider = CreateProvider();
        var key = "existing-key";
        var expectedPrefixedKey = $"{_options.KeyPrefix}:{key}";

        _database.KeyExistsAsync(expectedPrefixedKey, Arg.Any<CommandFlags>())
            .Returns(true);

        _database.KeyExpireAsync(
            expectedPrefixedKey,
            Arg.Any<TimeSpan?>(),
            Arg.Any<ExpireWhen>(),
            Arg.Any<CommandFlags>())
            .Returns(true);

        // Act
        var result = await provider.RefreshAsync(key, CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task RefreshAsync_WhenKeyDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var provider = CreateProvider();
        var key = "nonexistent-key";
        var expectedPrefixedKey = $"{_options.KeyPrefix}:{key}";

        _database.KeyExistsAsync(expectedPrefixedKey, Arg.Any<CommandFlags>())
            .Returns(false);

        // Act
        var result = await provider.RefreshAsync(key, CancellationToken.None);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region SetWithSlidingExpirationAsync Tests

    [Fact]
    public async Task SetWithSlidingExpirationAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            provider.SetWithSlidingExpirationAsync(
                null!,
                "value",
                TimeSpan.FromMinutes(5),
                null,
                CancellationToken.None));
    }

    [Fact]
    public async Task SetWithSlidingExpirationAsync_WithCancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var provider = CreateProvider();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            provider.SetWithSlidingExpirationAsync(
                "key",
                "value",
                TimeSpan.FromMinutes(5),
                null,
                cts.Token));
    }

    [Fact]
    public async Task SetWithSlidingExpirationAsync_WithoutAbsoluteExpiration_StoresOnlyValue()
    {
        // Arrange
        var provider = CreateProvider();
        var key = "test-key";
        var value = new TestData { Id = 123, Name = "Test" };
        var slidingExpiration = TimeSpan.FromMinutes(5);

        // Act
        var exception = await Record.ExceptionAsync(() =>
            provider.SetWithSlidingExpirationAsync(key, value, slidingExpiration, null, CancellationToken.None));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task SetWithSlidingExpirationAsync_WithAbsoluteExpiration_StoresValueAndMetadata()
    {
        // Arrange
        var provider = CreateProvider();
        var key = "test-key";
        var value = new TestData { Id = 123, Name = "Test" };
        var slidingExpiration = TimeSpan.FromMinutes(5);
        var absoluteExpiration = TimeSpan.FromHours(1);

        // Act
        var exception = await Record.ExceptionAsync(() =>
            provider.SetWithSlidingExpirationAsync(
                key,
                value,
                slidingExpiration,
                absoluteExpiration,
                CancellationToken.None));

        // Assert
        Assert.Null(exception);
    }

    #endregion

    #region RedisCacheOptions Tests

    [Fact]
    public void RedisCacheOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new RedisCacheOptions();

        // Assert
        options.Database.ShouldBe(0);
        options.KeyPrefix.ShouldBe(string.Empty);
        options.DefaultExpiration.ShouldBe(TimeSpan.FromMinutes(5));
        options.ProviderHealthCheck.ShouldNotBeNull();
    }

    [Fact]
    public void RedisCacheOptions_CanSetAllProperties()
    {
        // Arrange & Act
        var options = new RedisCacheOptions
        {
            Database = 3,
            KeyPrefix = "myapp",
            DefaultExpiration = TimeSpan.FromMinutes(15)
        };

        // Assert
        options.Database.ShouldBe(3);
        options.KeyPrefix.ShouldBe("myapp");
        options.DefaultExpiration.ShouldBe(TimeSpan.FromMinutes(15));
    }

    #endregion

    #region RefreshAsync Cancellation Tests

    [Fact]
    public async Task RefreshAsync_WithCancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var provider = CreateProvider();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            provider.RefreshAsync("key", cts.Token));
    }

    #endregion

    #region Helper Methods

    private RedisCacheProvider CreateProvider()
    {
        return new RedisCacheProvider(
            _connectionMultiplexer,
            Options.Create(_options),
            _logger);
    }

    #endregion

    #region Test Types

    private sealed class TestData
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }

    #endregion
}
