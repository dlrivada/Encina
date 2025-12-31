using Encina.AzureServiceBus.Health;
using Encina.Messaging.ContractTests.Health;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Encina.AzureServiceBus.Tests.Health;

/// <summary>
/// Contract tests for <see cref="AzureServiceBusHealthCheck"/> ensuring it follows the IEncinaHealthCheck contract.
/// </summary>
public sealed class AzureServiceBusHealthCheckContractTests : IEncinaHealthCheckContractTests
{
    protected override IEncinaHealthCheck CreateHealthCheck()
    {
        var serviceProvider = CreateMockServiceProvider();
        return new AzureServiceBusHealthCheck(serviceProvider, null);
    }

    protected override IEncinaHealthCheck CreateHealthCheckWithCustomName(string name)
    {
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Name = name };
        return new AzureServiceBusHealthCheck(serviceProvider, options);
    }

    protected override IEncinaHealthCheck CreateHealthCheckWithCustomTags(IReadOnlyCollection<string> tags)
    {
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Tags = tags.ToArray() };
        return new AzureServiceBusHealthCheck(serviceProvider, options);
    }

    [Fact]
    public void DefaultName_ShouldBeEncinaAzureServicebus()
    {
        AzureServiceBusHealthCheck.DefaultName.ShouldBe("encina-azure-servicebus");
    }

    [Theory]
    [InlineData("messaging")]
    [InlineData("azure-servicebus")]
    public void Tags_WithDefaultOptions_ShouldContainExpectedTag(string tag)
    {
        var healthCheck = CreateHealthCheck();
        healthCheck.Tags.ShouldContain(tag);
    }

    private static IServiceProvider CreateMockServiceProvider()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        return serviceProvider;
    }
}
