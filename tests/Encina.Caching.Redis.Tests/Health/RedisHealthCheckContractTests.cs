using Encina.Caching.Redis.Health;
using Encina.Messaging.ContractTests.Health;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Encina.Caching.Redis.Tests.Health;

/// <summary>
/// Contract tests for <see cref="RedisHealthCheck"/> ensuring it follows the IEncinaHealthCheck contract.
/// </summary>
public sealed class RedisHealthCheckContractTests : IEncinaHealthCheckContractTests
{
    protected override IEncinaHealthCheck CreateHealthCheck()
    {
        var serviceProvider = CreateMockServiceProvider();
        return new RedisHealthCheck(serviceProvider, null);
    }

    protected override IEncinaHealthCheck CreateHealthCheckWithCustomName(string name)
    {
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Name = name };
        return new RedisHealthCheck(serviceProvider, options);
    }

    protected override IEncinaHealthCheck CreateHealthCheckWithCustomTags(IReadOnlyCollection<string> tags)
    {
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Tags = tags.ToArray() };
        return new RedisHealthCheck(serviceProvider, options);
    }

    [Fact]
    public void DefaultName_ShouldBeEncinaRedis()
    {
        RedisHealthCheck.DefaultName.ShouldBe("encina-redis");
    }

    [Fact]
    public void Tags_WithDefaultOptions_ShouldContainCacheTag()
    {
        var healthCheck = CreateHealthCheck();
        healthCheck.Tags.ShouldContain("cache");
    }

    [Fact]
    public void Tags_WithDefaultOptions_ShouldContainRedisTag()
    {
        var healthCheck = CreateHealthCheck();
        healthCheck.Tags.ShouldContain("redis");
    }

    private static IServiceProvider CreateMockServiceProvider()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        return serviceProvider;
    }
}
