using Encina.Messaging.ContractTests.Health;
using Encina.Messaging.Health;
using Encina.MQTT.Health;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;

namespace Encina.MQTT.Tests.Health;

/// <summary>
/// Contract tests for <see cref="MQTTHealthCheck"/> ensuring it follows the IEncinaHealthCheck contract.
/// </summary>
public sealed class MQTTHealthCheckContractTests : IEncinaHealthCheckContractTests
{
    protected override IEncinaHealthCheck CreateHealthCheck()
    {
        var serviceProvider = CreateMockServiceProvider();
        return new MQTTHealthCheck(serviceProvider, null);
    }

    protected override IEncinaHealthCheck CreateHealthCheckWithCustomName(string name)
    {
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Name = name };
        return new MQTTHealthCheck(serviceProvider, options);
    }

    protected override IEncinaHealthCheck CreateHealthCheckWithCustomTags(IReadOnlyCollection<string> tags)
    {
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Tags = tags.ToArray() };
        return new MQTTHealthCheck(serviceProvider, options);
    }

    [Fact]
    public void DefaultName_ShouldBeEncinaMqtt()
    {
        MQTTHealthCheck.DefaultName.ShouldBe("encina-mqtt");
    }

    [Fact]
    public void Tags_WithDefaultOptions_ShouldContainMessagingTag()
    {
        var healthCheck = CreateHealthCheck();
        healthCheck.Tags.ShouldContain("messaging");
    }

    [Fact]
    public void Tags_WithDefaultOptions_ShouldContainMqttTag()
    {
        var healthCheck = CreateHealthCheck();
        healthCheck.Tags.ShouldContain("mqtt");
    }

    private static IServiceProvider CreateMockServiceProvider()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        return serviceProvider;
    }
}
