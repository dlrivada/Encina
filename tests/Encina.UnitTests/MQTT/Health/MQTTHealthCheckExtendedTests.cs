using Encina.MQTT.Health;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using NSubstitute;
using HealthCheckResult = Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult;
using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Encina.UnitTests.MQTT.Health;

public class MQTTHealthCheckExtendedTests
{
    [Fact]
    public void DefaultName_ShouldBeEncinaMqtt()
    {
        MQTTHealthCheck.DefaultName.ShouldBe("encina-mqtt");
    }

    [Fact]
    public void Constructor_NullOptions_UsesDefaults()
    {
        var sp = Substitute.For<IServiceProvider>();
        var hc = new MQTTHealthCheck(sp, null);
        hc.Name.ShouldBe("encina-mqtt");
    }

    [Fact]
    public void Constructor_CustomOptions_UsesCustomName()
    {
        var sp = Substitute.For<IServiceProvider>();
        var options = new global::Encina.Messaging.Health.ProviderHealthCheckOptions { Name = "custom-mqtt" };
        var hc = new MQTTHealthCheck(sp, options);
        hc.Name.ShouldBe("custom-mqtt");
    }

    [Fact]
    public async Task CheckHealthAsync_ConnectedClient_ReturnsHealthy()
    {
        var client = Substitute.For<IMqttClient>();
        client.IsConnected.Returns(true);
        var services = new ServiceCollection();
        services.AddSingleton(client);
        var sp = services.BuildServiceProvider();

        var hc = new MQTTHealthCheck(sp, null);
        var result = await hc.CheckHealthAsync();

        ((int)result.Status).ShouldBe((int)HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_DisconnectedClient_ReturnsUnhealthy()
    {
        var client = Substitute.For<IMqttClient>();
        client.IsConnected.Returns(false);
        var services = new ServiceCollection();
        services.AddSingleton(client);
        var sp = services.BuildServiceProvider();

        var hc = new MQTTHealthCheck(sp, null);
        var result = await hc.CheckHealthAsync();

        ((int)result.Status).ShouldBe((int)HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_NoClient_ReturnsUnhealthy()
    {
        var sp = new ServiceCollection().BuildServiceProvider();
        var hc = new MQTTHealthCheck(sp, null);

        var result = await hc.CheckHealthAsync();
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public void Tags_Default_ContainsExpected()
    {
        var sp = Substitute.For<IServiceProvider>();
        var hc = new MQTTHealthCheck(sp, null);
        var tags = hc.Tags.ToList();
        tags.ShouldContain("encina");
        tags.ShouldContain("mqtt");
    }
}
