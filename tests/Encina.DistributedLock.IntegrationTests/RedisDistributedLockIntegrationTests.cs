using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace Encina.DistributedLock.IntegrationTests;

/// <summary>
/// Integration tests for RedisDistributedLockProvider using Testcontainers.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "Redis")]
public class RedisDistributedLockIntegrationTests : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer;
    private ConnectionMultiplexer? _connection;
    private RedisDistributedLockProvider? _provider;

    public RedisDistributedLockIntegrationTests()
    {
        _redisContainer = new RedisBuilder("redis:7-alpine")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _redisContainer.StartAsync();
        _connection = await ConnectionMultiplexer.ConnectAsync(_redisContainer.GetConnectionString());
        _provider = new RedisDistributedLockProvider(
            _connection,
            Options.Create(new RedisLockOptions()),
            NullLogger<RedisDistributedLockProvider>.Instance);
    }

    public async Task DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        await _redisContainer.DisposeAsync();
    }

    [Fact]
    public async Task TryAcquireAsync_ShouldAcquireLock()
    {
        // Arrange
        var resource = $"test-{Guid.NewGuid()}";

        // Act
        var lockHandle = await _provider!.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        // Assert
        lockHandle.ShouldNotBeNull();
        await lockHandle!.DisposeAsync();
    }

    [Fact]
    public async Task TryAcquireAsync_WhenResourceLocked_ShouldReturnNull()
    {
        // Arrange
        var resource = $"test-{Guid.NewGuid()}";

        await using var firstLock = await _provider!.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(50),
            CancellationToken.None);

        // Act
        var secondLock = await _provider.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMilliseconds(200),
            TimeSpan.FromMilliseconds(50),
            CancellationToken.None);

        // Assert
        firstLock.ShouldNotBeNull();
        secondLock.ShouldBeNull();
    }

    [Fact]
    public async Task IsLockedAsync_WhenResourceLocked_ShouldReturnTrue()
    {
        // Arrange
        var resource = $"test-{Guid.NewGuid()}";

        await using var lockHandle = await _provider!.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(50),
            CancellationToken.None);

        // Act
        var isLocked = await _provider.IsLockedAsync(resource, CancellationToken.None);

        // Assert
        isLocked.ShouldBeTrue();
    }

    [Fact]
    public async Task Lock_WhenReleased_ShouldBeAcquirableAgain()
    {
        // Arrange
        var resource = $"test-{Guid.NewGuid()}";

        var firstLock = await _provider!.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(50),
            CancellationToken.None);

        await firstLock!.DisposeAsync();

        // Act
        var secondLock = await _provider.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(50),
            CancellationToken.None);

        // Assert
        secondLock.ShouldNotBeNull();
        await secondLock!.DisposeAsync();
    }

    [Fact]
    public async Task ConcurrentAcquisitions_OnlyOneSucceeds()
    {
        // Arrange
        var resource = $"test-concurrent-{Guid.NewGuid()}";
        const int numAttempts = 10;

        // Act
        var tasks = Enumerable.Range(0, numAttempts)
            .Select(_ => _provider!.TryAcquireAsync(
                resource,
                TimeSpan.FromMinutes(1),
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(20),
                CancellationToken.None))
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert
        var successCount = results.Count(r => r is not null);
        successCount.ShouldBe(1);

        // Clean up
        foreach (var result in results.Where(r => r is not null))
        {
            await result!.DisposeAsync();
        }
    }

    [Fact]
    public async Task LockHandle_ExtendAsync_ShouldExtendLock()
    {
        // Arrange
        var resource = $"test-extend-{Guid.NewGuid()}";

        var lockHandle = await _provider!.TryAcquireAsync(
            resource,
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(50),
            CancellationToken.None) as ILockHandle;

        // Act
        var extended = await lockHandle!.ExtendAsync(TimeSpan.FromMinutes(5));

        // Assert
        extended.ShouldBeTrue();

        await lockHandle.DisposeAsync();
    }
}
