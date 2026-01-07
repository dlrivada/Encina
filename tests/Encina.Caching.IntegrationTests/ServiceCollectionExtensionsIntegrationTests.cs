using Encina.Caching.IntegrationTests.Fixtures;
using Encina.Caching.Redis;
using Encina.DistributedLock;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;

namespace Encina.Caching.IntegrationTests;

/// <summary>
/// Integration tests for <see cref="ServiceCollectionExtensions"/> using real Redis connections.
/// These tests cover the connection string overloads that require an actual Redis server.
/// </summary>
[Collection(RedisCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "Redis")]
public sealed class ServiceCollectionExtensionsIntegrationTests
{
    private readonly RedisFixture _fixture;

    public ServiceCollectionExtensionsIntegrationTests(RedisFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public void AddEncinaRedisCache_WithConnectionString_RegistersAllServices()
    {
        Skip.IfNot(_fixture.IsAvailable, "Redis container not available");

        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        // Act
        services.AddEncinaRedisCache(_fixture.ConnectionString);
        using var provider = services.BuildServiceProvider();

        // Assert
        var cacheProvider = provider.GetService<ICacheProvider>();
        var pubSubProvider = provider.GetService<IPubSubProvider>();
        var lockProvider = provider.GetService<IDistributedLockProvider>();
        var multiplexer = provider.GetService<IConnectionMultiplexer>();

        cacheProvider.ShouldNotBeNull();
        pubSubProvider.ShouldNotBeNull();
        lockProvider.ShouldNotBeNull();
        multiplexer.ShouldNotBeNull();
    }

    [SkippableFact]
    public void AddEncinaRedisCache_WithConnectionStringAndOptions_RegistersAllServices()
    {
        Skip.IfNot(_fixture.IsAvailable, "Redis container not available");

        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        var cacheOptionsConfigured = false;

        // Act
        services.AddEncinaRedisCache(
            _fixture.ConnectionString,
            options =>
            {
                options.DefaultExpiration = TimeSpan.FromMinutes(10);
                cacheOptionsConfigured = true;
            },
            options =>
            {
                options.Database = 1;
            });
        using var provider = services.BuildServiceProvider();

        // Assert
        cacheOptionsConfigured.ShouldBeTrue();

        var cacheProvider = provider.GetService<ICacheProvider>();
        var pubSubProvider = provider.GetService<IPubSubProvider>();
        var lockProvider = provider.GetService<IDistributedLockProvider>();

        cacheProvider.ShouldNotBeNull();
        pubSubProvider.ShouldNotBeNull();
        lockProvider.ShouldNotBeNull();
    }

    [SkippableFact]
    public async Task AddEncinaRedisCache_WithConnectionString_CanPerformCacheOperations()
    {
        Skip.IfNot(_fixture.IsAvailable, "Redis container not available");

        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddEncinaRedisCache(_fixture.ConnectionString);
        await using var provider = services.BuildServiceProvider();

        var cacheProvider = provider.GetRequiredService<ICacheProvider>();
        var key = $"test-key-{Guid.NewGuid()}";
        var value = "test-value";

        // Act
        await cacheProvider.SetAsync(key, value, TimeSpan.FromMinutes(1), CancellationToken.None);
        var result = await cacheProvider.GetAsync<string>(key, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(value);

        // Cleanup
        await cacheProvider.RemoveAsync(key, CancellationToken.None);
    }

    [SkippableFact]
    public async Task AddEncinaRedisCache_WithConnectionStringAndOptions_CanAcquireLock()
    {
        Skip.IfNot(_fixture.IsAvailable, "Redis container not available");

        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddEncinaRedisCache(
            _fixture.ConnectionString,
            _ => { },
            options => options.KeyPrefix = "test-lock");
        await using var provider = services.BuildServiceProvider();

        var lockProvider = provider.GetRequiredService<IDistributedLockProvider>();
        var lockKey = $"lock-key-{Guid.NewGuid()}";

        // Act
        await using var lockHandle = await lockProvider.AcquireAsync(lockKey, TimeSpan.FromSeconds(10), CancellationToken.None);

        // Assert - if we get here without exception, the lock was acquired
        lockHandle.ShouldNotBeNull();
    }
}
