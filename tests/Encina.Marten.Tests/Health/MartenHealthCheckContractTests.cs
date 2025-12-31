using Encina.Marten.Health;
using Encina.Messaging.ContractTests.Health;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Encina.Marten.Tests.Health;

/// <summary>
/// Contract tests for <see cref="MartenHealthCheck"/> ensuring it follows the IEncinaHealthCheck contract.
/// </summary>
public sealed class MartenHealthCheckContractTests : IEncinaHealthCheckContractTests
{
    protected override IEncinaHealthCheck CreateHealthCheck()
    {
        var serviceProvider = CreateMockServiceProvider();
        return new MartenHealthCheck(serviceProvider, null);
    }

    protected override IEncinaHealthCheck CreateHealthCheckWithCustomName(string name)
    {
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Name = name };
        return new MartenHealthCheck(serviceProvider, options);
    }

    protected override IEncinaHealthCheck CreateHealthCheckWithCustomTags(IReadOnlyCollection<string> tags)
    {
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Tags = tags.ToArray() };
        return new MartenHealthCheck(serviceProvider, options);
    }

    [Fact]
    public void DefaultName_ShouldBeEncinaMarten()
    {
        MartenHealthCheck.DefaultName.ShouldBe("encina-marten");
    }

    [Fact]
    public void Tags_WithDefaultOptions_ShouldContainDatabaseTag()
    {
        var healthCheck = CreateHealthCheck();
        healthCheck.Tags.ShouldContain("database");
    }

    [Fact]
    public void Tags_WithDefaultOptions_ShouldContainMartenTag()
    {
        var healthCheck = CreateHealthCheck();
        healthCheck.Tags.ShouldContain("marten");
    }

    private static IServiceProvider CreateMockServiceProvider()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        return serviceProvider;
    }
}
