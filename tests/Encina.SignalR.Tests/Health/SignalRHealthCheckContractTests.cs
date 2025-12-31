using Encina.Messaging.ContractTests.Health;
using Encina.Messaging.Health;
using Encina.SignalR.Health;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Encina.SignalR.Tests.Health;

/// <summary>
/// Contract tests for <see cref="SignalRHealthCheck"/> ensuring it follows the IEncinaHealthCheck contract.
/// </summary>
public sealed class SignalRHealthCheckContractTests : IEncinaHealthCheckContractTests
{
    protected override IEncinaHealthCheck CreateHealthCheck()
    {
        var serviceProvider = CreateMockServiceProvider();
        return new SignalRHealthCheck(serviceProvider, null);
    }

    protected override IEncinaHealthCheck CreateHealthCheckWithCustomName(string name)
    {
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Name = name };
        return new SignalRHealthCheck(serviceProvider, options);
    }

    protected override IEncinaHealthCheck CreateHealthCheckWithCustomTags(IReadOnlyCollection<string> tags)
    {
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Tags = tags.ToArray() };
        return new SignalRHealthCheck(serviceProvider, options);
    }

    [Fact]
    public void DefaultName_ShouldBeEncinaSignalr()
    {
        SignalRHealthCheck.DefaultName.ShouldBe("encina-signalr");
    }

    [Fact]
    public void Tags_WithDefaultOptions_ShouldContainMessagingTag()
    {
        var healthCheck = CreateHealthCheck();
        healthCheck.Tags.ShouldContain("messaging");
    }

    [Fact]
    public void Tags_WithDefaultOptions_ShouldContainSignalrTag()
    {
        var healthCheck = CreateHealthCheck();
        healthCheck.Tags.ShouldContain("signalr");
    }

    private static IServiceProvider CreateMockServiceProvider()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        return serviceProvider;
    }
}
