using Encina.Caching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Caching;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    #region AddEncinaCaching Tests

    [Fact]
    public void AddEncinaCaching_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => services!.AddEncinaCaching());
    }

    [Fact]
    public void AddEncinaCaching_RegistersDefaultOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaCaching();

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<CachingOptions>>();
        options.Value.ShouldNotBeNull();
        options.Value.EnableQueryCaching.ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaCaching_WithConfiguration_AppliesOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaCaching(options =>
        {
            options.EnableQueryCaching = false;
            options.EnableCacheInvalidation = false;
            options.EnableDistributedIdempotency = false;
            options.DefaultDuration = TimeSpan.FromMinutes(15);
            options.KeyPrefix = "test";
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<CachingOptions>>();
        options.Value.EnableQueryCaching.ShouldBeFalse();
        options.Value.EnableCacheInvalidation.ShouldBeFalse();
        options.Value.EnableDistributedIdempotency.ShouldBeFalse();
        options.Value.DefaultDuration.ShouldBe(TimeSpan.FromMinutes(15));
        options.Value.KeyPrefix.ShouldBe("test");
    }

    [Fact]
    public void AddEncinaCaching_RegistersDefaultCacheKeyGenerator()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaCaching();

        // Assert
        var provider = services.BuildServiceProvider();
        var generator = provider.GetService<ICacheKeyGenerator>();
        generator.ShouldNotBeNull();
        generator.ShouldBeOfType<DefaultCacheKeyGenerator>();
    }

    [Fact]
    public void AddEncinaCaching_WhenQueryCachingEnabled_RegistersQueryCachingBehavior()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaCaching(options =>
        {
            options.EnableQueryCaching = true;
            options.EnableCacheInvalidation = false;
            options.EnableDistributedIdempotency = false;
        });

        // Assert
        var behaviorRegistrations = services.Where(s =>
            s.ServiceType == typeof(IPipelineBehavior<,>) &&
            s.ImplementationType?.Name.Contains("QueryCaching") == true);
        behaviorRegistrations.ShouldNotBeEmpty();
    }

    [Fact]
    public void AddEncinaCaching_WhenCacheInvalidationEnabled_RegistersInvalidationBehavior()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaCaching(options =>
        {
            options.EnableQueryCaching = false;
            options.EnableCacheInvalidation = true;
            options.EnableDistributedIdempotency = false;
        });

        // Assert
        var behaviorRegistrations = services.Where(s =>
            s.ServiceType == typeof(IPipelineBehavior<,>) &&
            s.ImplementationType?.Name.Contains("CacheInvalidation") == true);
        behaviorRegistrations.ShouldNotBeEmpty();
    }

    [Fact]
    public void AddEncinaCaching_WhenDistributedIdempotencyEnabled_RegistersIdempotencyBehavior()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaCaching(options =>
        {
            options.EnableQueryCaching = false;
            options.EnableCacheInvalidation = false;
            options.EnableDistributedIdempotency = true;
        });

        // Assert
        var behaviorRegistrations = services.Where(s =>
            s.ServiceType == typeof(IPipelineBehavior<,>) &&
            s.ImplementationType?.Name.Contains("DistributedIdempotency") == true);
        behaviorRegistrations.ShouldNotBeEmpty();
    }

    [Fact]
    public void AddEncinaCaching_WhenAllBehaviorsDisabled_RegistersNoBehaviors()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaCaching(options =>
        {
            options.EnableQueryCaching = false;
            options.EnableCacheInvalidation = false;
            options.EnableDistributedIdempotency = false;
        });

        // Assert
        var behaviorRegistrations = services.Where(s =>
            s.ServiceType == typeof(IPipelineBehavior<,>));
        behaviorRegistrations.ShouldBeEmpty();
    }

    [Fact]
    public void AddEncinaCaching_CopiesAllOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaCaching(options =>
        {
            options.EnableQueryCaching = false;
            options.EnableCacheInvalidation = false;
            options.EnableDistributedIdempotency = false;
            options.EnableDistributedLocks = false;
            options.EnablePubSubInvalidation = false;
            options.DefaultDuration = TimeSpan.FromMinutes(20);
            options.DefaultPriority = CachePriority.High;
            options.KeyPrefix = "myapp";
            options.InvalidationChannel = "myapp:invalidate";
            options.IdempotencyKeyPrefix = "myapp:idem";
            options.IdempotencyTtl = TimeSpan.FromHours(48);
            options.LockKeyPrefix = "myapp:lock";
            options.DefaultLockExpiry = TimeSpan.FromMinutes(2);
            options.DefaultLockWait = TimeSpan.FromSeconds(20);
            options.DefaultLockRetry = TimeSpan.FromMilliseconds(500);
            options.ThrowOnCacheErrors = true;
            options.SerializerOptions = new CacheSerializerOptions
            {
                SerializerType = CacheSerializerType.MessagePack,
                EnableCompression = true,
                CompressionThreshold = 2048
            };
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<CachingOptions>>().Value;

        options.EnableQueryCaching.ShouldBeFalse();
        options.EnableCacheInvalidation.ShouldBeFalse();
        options.EnableDistributedIdempotency.ShouldBeFalse();
        options.EnableDistributedLocks.ShouldBeFalse();
        options.EnablePubSubInvalidation.ShouldBeFalse();
        options.DefaultDuration.ShouldBe(TimeSpan.FromMinutes(20));
        options.DefaultPriority.ShouldBe(CachePriority.High);
        options.KeyPrefix.ShouldBe("myapp");
        options.InvalidationChannel.ShouldBe("myapp:invalidate");
        options.IdempotencyKeyPrefix.ShouldBe("myapp:idem");
        options.IdempotencyTtl.ShouldBe(TimeSpan.FromHours(48));
        options.LockKeyPrefix.ShouldBe("myapp:lock");
        options.DefaultLockExpiry.ShouldBe(TimeSpan.FromMinutes(2));
        options.DefaultLockWait.ShouldBe(TimeSpan.FromSeconds(20));
        options.DefaultLockRetry.ShouldBe(TimeSpan.FromMilliseconds(500));
        options.ThrowOnCacheErrors.ShouldBeTrue();
        options.SerializerOptions.SerializerType.ShouldBe(CacheSerializerType.MessagePack);
        options.SerializerOptions.EnableCompression.ShouldBeTrue();
        options.SerializerOptions.CompressionThreshold.ShouldBe(2048);
    }

    #endregion

    #region AddCacheKeyGenerator Tests

    [Fact]
    public void AddCacheKeyGenerator_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddCacheKeyGenerator<TestCacheKeyGenerator>());
    }

    [Fact]
    public void AddCacheKeyGenerator_ReplacesDefaultGenerator()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncinaCaching();

        // Act
        services.AddCacheKeyGenerator<TestCacheKeyGenerator>();

        // Assert
        var provider = services.BuildServiceProvider();
        var generator = provider.GetRequiredService<ICacheKeyGenerator>();
        generator.ShouldBeOfType<TestCacheKeyGenerator>();
    }

    #endregion

    #region AddCacheConfiguration Tests

    [Fact]
    public void AddCacheConfiguration_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddCacheConfiguration<TestQuery>(config => { }));
    }

    [Fact]
    public void AddCacheConfiguration_WithNullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddCacheConfiguration<TestQuery>(null!));
    }

    [Fact]
    public void AddCacheConfiguration_RegistersConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        // Note: configure action is called but CacheConfiguration uses init-only properties
        // so the action can only inspect the default values, not modify them.
        // This tests that the registration mechanism works.
        services.AddCacheConfiguration<TestQuery>(_ =>
        {
            // Configuration inspection only (init properties can't be set via action)
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var config = provider.GetRequiredService<ICacheConfiguration<TestQuery>>();
        config.ShouldNotBeNull();
        // Verify default values are registered
        config.Duration.ShouldBe(TimeSpan.FromMinutes(5));
    }

    #endregion

    #region Test Types

    private sealed record TestQuery(Guid Id) : IRequest<string>;

    private sealed class TestCacheKeyGenerator : ICacheKeyGenerator
    {
        public string GenerateKey<TRequest, TResponse>(TRequest request, IRequestContext context)
            where TRequest : IRequest<TResponse>
        {
            return $"test:{request?.GetType().Name}";
        }

        public string GeneratePattern<TRequest>(IRequestContext context)
        {
            return $"test:{typeof(TRequest).Name}:*";
        }

        public string GeneratePatternFromTemplate<TRequest>(string keyTemplate, TRequest request, IRequestContext context)
        {
            return $"test:{keyTemplate}";
        }
    }

    #endregion
}
