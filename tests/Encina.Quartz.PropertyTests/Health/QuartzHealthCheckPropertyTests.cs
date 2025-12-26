using Encina.Messaging.Health;
using Encina.Quartz.Health;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Quartz;

namespace Encina.Quartz.PropertyTests.Health;

/// <summary>
/// Property-based tests for <see cref="QuartzHealthCheck"/> invariants.
/// </summary>
public sealed class QuartzHealthCheckPropertyTests
{
    [Fact]
    public void DefaultName_IsAlwaysEncinaQuartz()
    {
        Assert.Equal("encina-quartz", QuartzHealthCheck.DefaultName);
    }

    [Property(MaxTest = 50)]
    public bool Constructor_WithNullOptions_UsesDefaultName(int seed)
    {
        var serviceProvider = CreateMockServiceProvider();
        var healthCheck = new QuartzHealthCheck(serviceProvider, null);
        return healthCheck.Name == QuartzHealthCheck.DefaultName;
    }

    [Property(MaxTest = 100)]
    public bool Constructor_WithCustomName_UsesCustomName(PositiveInt seed)
    {
        var customName = $"custom-name-{seed.Get}";
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Name = customName };
        var healthCheck = new QuartzHealthCheck(serviceProvider, options);
        return healthCheck.Name == customName;
    }

    [Property(MaxTest = 50)]
    public bool Tags_WithDefaultOptions_ContainsEncina(int seed)
    {
        var serviceProvider = CreateMockServiceProvider();
        var healthCheck = new QuartzHealthCheck(serviceProvider, null);
        return healthCheck.Tags.Contains("encina");
    }

    [Property(MaxTest = 50)]
    public bool Tags_WithDefaultOptions_ContainsScheduling(int seed)
    {
        var serviceProvider = CreateMockServiceProvider();
        var healthCheck = new QuartzHealthCheck(serviceProvider, null);
        return healthCheck.Tags.Contains("scheduling");
    }

    [Property(MaxTest = 50)]
    public bool Tags_WithDefaultOptions_ContainsQuartz(int seed)
    {
        var serviceProvider = CreateMockServiceProvider();
        var healthCheck = new QuartzHealthCheck(serviceProvider, null);
        return healthCheck.Tags.Contains("quartz");
    }

    [Property(MaxTest = 50)]
    public bool Tags_WithCustomTags_UsesCustomTags(PositiveInt count)
    {
        var tagCount = (count.Get % 5) + 1;
        var tagArray = Enumerable.Range(1, tagCount).Select(i => $"tag-{i}").ToArray();
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Tags = tagArray };
        var healthCheck = new QuartzHealthCheck(serviceProvider, options);
        return tagArray.All(tag => healthCheck.Tags.Contains(tag));
    }

    [Property(MaxTest = 20)]
    public bool CheckHealthAsync_ReturnsValidHealthStatus(int seed)
    {
        var serviceProvider = CreateMockServiceProvider();
        var healthCheck = new QuartzHealthCheck(serviceProvider, null);
        var result = healthCheck.CheckHealthAsync().GetAwaiter().GetResult();
        return result.Status == HealthStatus.Healthy
            || result.Status == HealthStatus.Unhealthy
            || result.Status == HealthStatus.Degraded;
    }

    private static IServiceProvider CreateMockServiceProvider()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        var schedulerFactory = Substitute.For<ISchedulerFactory>();
        serviceProvider.GetService(typeof(ISchedulerFactory)).Returns(schedulerFactory);
        return serviceProvider;
    }
}
