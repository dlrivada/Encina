using Encina.Messaging.ContractTests.Health;
using Encina.Messaging.Health;
using Encina.RabbitMQ.Health;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;

namespace Encina.RabbitMQ.Tests.Health;

/// <summary>
/// Contract tests for <see cref="RabbitMQHealthCheck"/> ensuring it follows the IEncinaHealthCheck contract.
/// </summary>
public sealed class RabbitMQHealthCheckContractTests : IEncinaHealthCheckContractTests
{
    protected override IEncinaHealthCheck CreateHealthCheck()
    {
        var serviceProvider = CreateMockServiceProvider();
        return new RabbitMQHealthCheck(serviceProvider, null);
    }

    protected override IEncinaHealthCheck CreateHealthCheckWithCustomName(string name)
    {
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Name = name };
        return new RabbitMQHealthCheck(serviceProvider, options);
    }

    protected override IEncinaHealthCheck CreateHealthCheckWithCustomTags(IReadOnlyCollection<string> tags)
    {
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Tags = tags.ToArray() };
        return new RabbitMQHealthCheck(serviceProvider, options);
    }

    [Fact]
    public void DefaultName_ShouldBeEncinaRabbitmq()
    {
        RabbitMQHealthCheck.DefaultName.ShouldBe("encina-rabbitmq");
    }

    [Fact]
    public void Tags_WithDefaultOptions_ShouldContainMessagingTag()
    {
        var healthCheck = CreateHealthCheck();
        healthCheck.Tags.ShouldContain("messaging");
    }

    [Fact]
    public void Tags_WithDefaultOptions_ShouldContainRabbitmqTag()
    {
        var healthCheck = CreateHealthCheck();
        healthCheck.Tags.ShouldContain("rabbitmq");
    }

    private static IServiceProvider CreateMockServiceProvider()
    {
        return Substitute.For<IServiceProvider>();
    }
}
