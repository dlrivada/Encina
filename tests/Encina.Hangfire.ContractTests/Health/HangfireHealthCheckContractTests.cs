using Encina.Hangfire.Health;
using Encina.Messaging.ContractTests.Health;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;

namespace Encina.Hangfire.ContractTests.Health;

/// <summary>
/// Contract tests for <see cref="HangfireHealthCheck"/> ensuring it follows the IEncinaHealthCheck contract.
/// </summary>
public sealed class HangfireHealthCheckContractTests : IEncinaHealthCheckContractTests
{
    protected override IEncinaHealthCheck CreateHealthCheck()
    {
        var serviceProvider = CreateMockServiceProvider();
        return new HangfireHealthCheck(serviceProvider, null);
    }

    protected override IEncinaHealthCheck CreateHealthCheckWithCustomName(string name)
    {
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Name = name };
        return new HangfireHealthCheck(serviceProvider, options);
    }

    protected override IEncinaHealthCheck CreateHealthCheckWithCustomTags(IReadOnlyCollection<string> tags)
    {
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Tags = tags.ToArray() };
        return new HangfireHealthCheck(serviceProvider, options);
    }

    [Fact]
    public void DefaultName_ShouldBeEncinaHangfire()
    {
        HangfireHealthCheck.DefaultName.ShouldBe("encina-hangfire");
    }

    [Fact]
    public void Tags_WithDefaultOptions_ShouldContainSchedulingTag()
    {
        var healthCheck = CreateHealthCheck();
        healthCheck.Tags.ShouldContain("scheduling");
    }

    [Fact]
    public void Tags_WithDefaultOptions_ShouldContainHangfireTag()
    {
        var healthCheck = CreateHealthCheck();
        healthCheck.Tags.ShouldContain("hangfire");
    }

    private static IServiceProvider CreateMockServiceProvider()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        return serviceProvider;
    }
}
