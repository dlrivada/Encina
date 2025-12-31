using Encina.Messaging.ContractTests.Health;
using Encina.Messaging.Health;
using Encina.Quartz.Health;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Encina.Quartz.ContractTests.Health;

/// <summary>
/// Contract tests for <see cref="QuartzHealthCheck"/> ensuring it follows the IEncinaHealthCheck contract.
/// </summary>
public sealed class QuartzHealthCheckContractTests : IEncinaHealthCheckContractTests
{
    protected override IEncinaHealthCheck CreateHealthCheck()
    {
        var serviceProvider = CreateMockServiceProvider();
        return new QuartzHealthCheck(serviceProvider, null);
    }

    protected override IEncinaHealthCheck CreateHealthCheckWithCustomName(string name)
    {
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Name = name };
        return new QuartzHealthCheck(serviceProvider, options);
    }

    protected override IEncinaHealthCheck CreateHealthCheckWithCustomTags(IReadOnlyCollection<string> tags)
    {
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Tags = tags.ToArray() };
        return new QuartzHealthCheck(serviceProvider, options);
    }

    [Fact]
    public void DefaultName_ShouldBeEncinaQuartz()
    {
        QuartzHealthCheck.DefaultName.ShouldBe("encina-quartz");
    }

    [Fact]
    public void Tags_WithDefaultOptions_ShouldContainSchedulingTag()
    {
        var healthCheck = CreateHealthCheck();
        healthCheck.Tags.ShouldContain("scheduling");
    }

    [Fact]
    public void Tags_WithDefaultOptions_ShouldContainQuartzTag()
    {
        var healthCheck = CreateHealthCheck();
        healthCheck.Tags.ShouldContain("quartz");
    }

    private static IServiceProvider CreateMockServiceProvider()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        return serviceProvider;
    }
}
