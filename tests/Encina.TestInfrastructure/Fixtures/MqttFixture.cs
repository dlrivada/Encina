using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using MQTTnet;
using Xunit;

namespace Encina.TestInfrastructure.Fixtures;

/// <summary>
/// MQTT fixture using Testcontainers with Eclipse Mosquitto.
/// Provides a throwaway MQTT broker instance for integration tests.
/// </summary>
public sealed class MqttFixture : IAsyncLifetime
{
    private IContainer? _container;
    private IMqttClient? _client;

    /// <summary>
    /// The default MQTT port.
    /// </summary>
    public const int MqttPort = 1883;

    /// <summary>
    /// Gets the MQTT client connected to the broker.
    /// </summary>
    public IMqttClient? Client => _client;

    /// <summary>
    /// Gets the host of the MQTT broker.
    /// </summary>
    public string Host => _container?.Hostname ?? "localhost";

    /// <summary>
    /// Gets the mapped port for MQTT connections.
    /// </summary>
    public int Port => _container?.GetMappedPublicPort(MqttPort) ?? MqttPort;

    /// <summary>
    /// Gets a value indicating whether the MQTT broker is available.
    /// </summary>
    public bool IsAvailable => _container is not null && _client is not null && _client.IsConnected;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        _container = new ContainerBuilder()
            .WithImage("eclipse-mosquitto:2")
            .WithPortBinding(MqttPort, true)
            .WithCommand("mosquitto", "-c", "/mosquitto-no-auth.conf")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("mosquitto"))
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();

        // Create and connect MQTT client with retry logic
        var factory = new MqttClientFactory();
        _client = factory.CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(Host, Port)
            .WithClientId($"test-client-{Guid.NewGuid():N}")
            .Build();

        // Retry connection a few times in case the broker isn't ready
        const int maxRetries = 5;
        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                await _client.ConnectAsync(options);
                break;
            }
            catch when (i < maxRetries - 1)
            {
                await Task.Delay(500);
            }
        }
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        if (_client is not null)
        {
            if (_client.IsConnected)
            {
                await _client.DisconnectAsync();
            }
            _client.Dispose();
        }

        if (_container is not null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }
}

/// <summary>
/// Collection definition for MQTT integration tests.
/// </summary>
[CollectionDefinition(Name)]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public class MqttCollection : ICollectionFixture<MqttFixture>
#pragma warning restore CA1711
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public const string Name = "MQTT";
}
