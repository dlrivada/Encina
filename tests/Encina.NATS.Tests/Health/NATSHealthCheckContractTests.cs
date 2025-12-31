using Encina.Messaging.ContractTests.Health;
using Encina.Messaging.Health;
using Encina.NATS.Health;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Encina.NATS.Tests.Health;

/// <summary>
/// Contract tests for <see cref="NATSHealthCheck"/> ensuring it follows the IEncinaHealthCheck contract.
/// </summary>
public sealed class NATSHealthCheckContractTests : IEncinaHealthCheckContractTests
{
    protected override IEncinaHealthCheck CreateHealthCheck()
    {
        var serviceProvider = CreateMockServiceProvider();
        return new NATSHealthCheck(serviceProvider, null);
    }

    protected override IEncinaHealthCheck CreateHealthCheckWithCustomName(string name)
    {
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Name = name };
        return new NATSHealthCheck(serviceProvider, options);
    }

    protected override IEncinaHealthCheck CreateHealthCheckWithCustomTags(IReadOnlyCollection<string> tags)
    {
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Tags = tags.ToArray() };
        return new NATSHealthCheck(serviceProvider, options);
    }

    [Fact]
    public void DefaultName_ShouldBeEncinaNats()
    {
        NATSHealthCheck.DefaultName.ShouldBe("encina-nats");
    }

    [Fact]
    public void Tags_WithDefaultOptions_ShouldContainMessagingTag()
    {
        var healthCheck = CreateHealthCheck();
        healthCheck.Tags.ShouldContain("messaging");
    }

    [Fact]
    public void Tags_WithDefaultOptions_ShouldContainNatsTag()
    {
        var healthCheck = CreateHealthCheck();
        healthCheck.Tags.ShouldContain("nats");
    }

    private static IServiceProvider CreateMockServiceProvider()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        return serviceProvider;
    }
}
