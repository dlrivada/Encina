using Encina.MQTT;
using MQTTnet;

namespace Encina.UnitTests.MQTT.Publishing;

/// <summary>
/// Regression tests for #1608: ending a scope that resolved the scoped publisher must not
/// disconnect or dispose the DI-singleton MQTT client.
/// </summary>
public sealed class MQTTPublisherLifetimeTests
{
    [Fact]
    public void ResolvingAndDisposingPublisherScope_LeavesSingletonClientUntouched()
    {
        // Arrange
        var client = Substitute.For<IMqttClient>();
        client.IsConnected.Returns(true);
        using var provider = BuildProvider(client);

        // Act
        using (var scope = provider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IMQTTMessagePublisher>().ShouldNotBeNull();
        }

        // Assert
        client.Received(0).Dispose();
        _ = client.Received(0).DisconnectAsync(
            Arg.Any<MqttClientDisconnectOptions>(),
            Arg.Any<CancellationToken>());
        using var secondScope = provider.CreateScope();
        secondScope.ServiceProvider.GetRequiredService<IMqttClient>().ShouldBeSameAs(client);
        secondScope.ServiceProvider.GetRequiredService<IMQTTMessagePublisher>().ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaMQTT_BuildsWithScopeAndBuildValidation()
    {
        // Arrange
        var client = Substitute.For<IMqttClient>();

        // Act
        var act = () => BuildProvider(client).Dispose();

        // Assert
        Should.NotThrow(act);
    }

    private static ServiceProvider BuildProvider(IMqttClient client)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(client);
        services.AddEncinaMQTT();

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
    }
}
