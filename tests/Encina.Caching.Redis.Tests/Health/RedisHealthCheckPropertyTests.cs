using Encina.Caching.Redis.Health;
using Encina.Messaging.Health;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Encina.Caching.Redis.Tests.Health;

/// <summary>
/// Property-based tests for <see cref="RedisHealthCheck"/> invariants.
/// </summary>
public sealed class RedisHealthCheckPropertyTests
{
    [Fact]
    public void DefaultName_IsAlwaysEncinaRedis()
    {
        Assert.Equal("encina-redis", RedisHealthCheck.DefaultName);
    }

    [Property(MaxTest = 50)]
    public bool Constructor_WithNullOptions_UsesDefaultName(int seed)
    {
        var serviceProvider = CreateMockServiceProvider();
        var healthCheck = new RedisHealthCheck(serviceProvider, null);
        return healthCheck.Name == RedisHealthCheck.DefaultName;
    }

    [Property(MaxTest = 100)]
    public bool Constructor_WithCustomName_UsesCustomName(PositiveInt seed)
    {
        var customName = $"custom-name-{seed.Get}";
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Name = customName };
        var healthCheck = new RedisHealthCheck(serviceProvider, options);
        return healthCheck.Name == customName;
    }

    [Property(MaxTest = 50)]
    public bool Tags_WithDefaultOptions_ContainsEncina(int seed)
    {
        var serviceProvider = CreateMockServiceProvider();
        var healthCheck = new RedisHealthCheck(serviceProvider, null);
        return healthCheck.Tags.Contains("encina");
    }

    [Property(MaxTest = 50)]
    public bool Tags_WithDefaultOptions_ContainsCache(int seed)
    {
        var serviceProvider = CreateMockServiceProvider();
        var healthCheck = new RedisHealthCheck(serviceProvider, null);
        return healthCheck.Tags.Contains("cache");
    }

    [Property(MaxTest = 50)]
    public bool Tags_WithDefaultOptions_ContainsRedis(int seed)
    {
        var serviceProvider = CreateMockServiceProvider();
        var healthCheck = new RedisHealthCheck(serviceProvider, null);
        return healthCheck.Tags.Contains("redis");
    }

    [Property(MaxTest = 50)]
    public bool Tags_WithCustomTags_UsesCustomTags(PositiveInt count)
    {
        var tagCount = (count.Get % 5) + 1;
        var tagArray = Enumerable.Range(1, tagCount).Select(i => $"tag-{i}").ToArray();
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Tags = tagArray };
        var healthCheck = new RedisHealthCheck(serviceProvider, options);
        return tagArray.All(tag => healthCheck.Tags.Contains(tag));
    }

    [Property(MaxTest = 20)]
    public bool CheckHealthAsync_ReturnsValidHealthStatus(int seed)
    {
        var serviceProvider = CreateMockServiceProvider();
        var healthCheck = new RedisHealthCheck(serviceProvider, null);
        var result = healthCheck.CheckHealthAsync().GetAwaiter().GetResult();
        return result.Status == HealthStatus.Healthy
            || result.Status == HealthStatus.Unhealthy
            || result.Status == HealthStatus.Degraded;
    }

    private static IServiceProvider CreateMockServiceProvider()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        return serviceProvider;
    }
}
