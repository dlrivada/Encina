using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Encina.Caching.Redis.Tests;

/// <summary>
/// Unit tests for <see cref="RedisDistributedLockProvider"/>.
/// </summary>
public sealed class RedisDistributedLockProviderTests
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IDatabase _database;
    private readonly RedisLockOptions _options;
    private readonly ILogger<RedisDistributedLockProvider> _logger;

    public RedisDistributedLockProviderTests()
    {
        _connectionMultiplexer = Substitute.For<IConnectionMultiplexer>();
        _database = Substitute.For<IDatabase>();
        _logger = NullLogger<RedisDistributedLockProvider>.Instance;
        _options = new RedisLockOptions
        {
            KeyPrefix = "test",
            Database = 0
        };

        _connectionMultiplexer.GetDatabase(Arg.Any<int>(), Arg.Any<object?>())
            .Returns(_database);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullConnection_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new RedisDistributedLockProvider(null!, Options.Create(_options), _logger));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new RedisDistributedLockProvider(_connectionMultiplexer, null!, _logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new RedisDistributedLockProvider(_connectionMultiplexer, Options.Create(_options), null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        var provider = CreateProvider();
        provider.ShouldNotBeNull();
    }

    #endregion

    #region TryAcquireAsync Tests

    [Fact]
    public async Task TryAcquireAsync_WithNullResource_ThrowsArgumentNullException()
    {
        var provider = CreateProvider();

        await Should.ThrowAsync<ArgumentNullException>(() =>
            provider.TryAcquireAsync(null!, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100), CancellationToken.None));
    }

    [Fact]
    public async Task TryAcquireAsync_WithCancellationRequested_ThrowsOperationCanceledException()
    {
        var provider = CreateProvider();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            provider.TryAcquireAsync("resource", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100), cts.Token));
    }

    #endregion

    #region AcquireAsync Tests

    [Fact]
    public async Task AcquireAsync_WithNullResource_ThrowsArgumentNullException()
    {
        var provider = CreateProvider();

        await Should.ThrowAsync<ArgumentNullException>(() =>
            provider.AcquireAsync(null!, TimeSpan.FromSeconds(10), CancellationToken.None));
    }

    [Fact]
    public async Task AcquireAsync_WithCancellationRequested_ThrowsOperationCanceledException()
    {
        var provider = CreateProvider();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            provider.AcquireAsync("resource", TimeSpan.FromSeconds(10), cts.Token));
    }

    #endregion

    #region IsLockedAsync Tests

    [Fact]
    public async Task IsLockedAsync_WithNullResource_ThrowsArgumentNullException()
    {
        var provider = CreateProvider();

        await Should.ThrowAsync<ArgumentNullException>(() =>
            provider.IsLockedAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task IsLockedAsync_WithCancellationRequested_ThrowsOperationCanceledException()
    {
        var provider = CreateProvider();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            provider.IsLockedAsync("resource", cts.Token));
    }

    [Fact]
    public async Task IsLockedAsync_WhenResourceIsLocked_ReturnsTrue()
    {
        var provider = CreateProvider();

        _database.KeyExistsAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(true);

        var result = await provider.IsLockedAsync("test-resource", CancellationToken.None);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IsLockedAsync_WhenResourceIsNotLocked_ReturnsFalse()
    {
        var provider = CreateProvider();

        _database.KeyExistsAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(false);

        var result = await provider.IsLockedAsync("test-resource", CancellationToken.None);

        result.ShouldBeFalse();
    }

    #endregion

    #region ExtendAsync Tests

    [Fact]
    public async Task ExtendAsync_WithNullResource_ThrowsArgumentNullException()
    {
        var provider = CreateProvider();

        await Should.ThrowAsync<ArgumentNullException>(() =>
            provider.ExtendAsync(null!, TimeSpan.FromSeconds(30), CancellationToken.None));
    }

    [Fact]
    public async Task ExtendAsync_WithCancellationRequested_ThrowsOperationCanceledException()
    {
        var provider = CreateProvider();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            provider.ExtendAsync("resource", TimeSpan.FromSeconds(30), cts.Token));
    }

    #endregion

    #region RedisLockOptions Tests

    [Fact]
    public void RedisLockOptions_DefaultValues_AreCorrect()
    {
        var options = new RedisLockOptions();

        options.Database.ShouldBe(0);
        options.KeyPrefix.ShouldBe(string.Empty);
    }

    [Fact]
    public void RedisLockOptions_CanSetAllProperties()
    {
        var options = new RedisLockOptions
        {
            Database = 5,
            KeyPrefix = "myapp"
        };

        options.Database.ShouldBe(5);
        options.KeyPrefix.ShouldBe("myapp");
    }

    #endregion

    #region Helper Methods

    private RedisDistributedLockProvider CreateProvider()
    {
        return new RedisDistributedLockProvider(
            _connectionMultiplexer,
            Options.Create(_options),
            _logger);
    }

    #endregion
}
