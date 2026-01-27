using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using MQTTnet;
using MQTTnet.Protocol;

namespace Encina.NBomber.Scenarios.Brokers.Providers;

/// <summary>
/// Factory for creating MQTT broker providers for load testing.
/// </summary>
public sealed class MQTTProviderFactory : BrokerProviderFactoryBase
{
    private const int MqttPort = 1883;
    private IContainer? _container;
    private IMqttClient? _client;
    private string? _host;
    private int _port;
    private bool _containerInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="MQTTProviderFactory"/> class.
    /// </summary>
    /// <param name="configureOptions">Optional delegate to configure options.</param>
    public MQTTProviderFactory(Action<BrokerProviderOptions>? configureOptions = null)
        : base(configureOptions)
    {
    }

    /// <inheritdoc/>
    public override string ProviderName => "mqtt";

    /// <inheritdoc/>
    public override BrokerProviderCategory Category => BrokerProviderCategory.MQTT;

    /// <inheritdoc/>
    public override bool IsAvailable => base.IsAvailable && _containerInitialized;

    /// <summary>
    /// Gets the MQTT host.
    /// </summary>
    public string? Host => _host;

    /// <summary>
    /// Gets the MQTT port.
    /// </summary>
    public int Port => _port;

    /// <summary>
    /// Gets the shared MQTT client.
    /// </summary>
    public IMqttClient? Client => _client;

    /// <summary>
    /// Creates a new MQTT client and connects it.
    /// </summary>
    /// <param name="clientId">Optional client ID.</param>
    /// <returns>A connected MQTT client.</returns>
    public async Task<IMqttClient> CreateClientAsync(string? clientId = null)
    {
        EnsureInitialized();

        var factory = new MqttClientFactory();
        var client = factory.CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(_host, _port)
            .WithClientId(clientId ?? $"nbomber-{Guid.NewGuid():N}")
            .WithCleanSession()
            .Build();

        await client.ConnectAsync(options).ConfigureAwait(false);
        return client;
    }

    /// <summary>
    /// Gets the QoS level from options.
    /// </summary>
    /// <returns>The MQTT QoS level.</returns>
    public MqttQualityOfServiceLevel GetQoSLevel()
    {
        return Options.MQTTQoS switch
        {
            0 => MqttQualityOfServiceLevel.AtMostOnce,
            1 => MqttQualityOfServiceLevel.AtLeastOnce,
            2 => MqttQualityOfServiceLevel.ExactlyOnce,
            _ => MqttQualityOfServiceLevel.AtMostOnce
        };
    }

    /// <inheritdoc/>
    protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!string.IsNullOrEmpty(Options.MQTTHost) && Options.MQTTPort.HasValue)
            {
                _host = Options.MQTTHost;
                _port = Options.MQTTPort.Value;
            }
            else
            {
                _container = new ContainerBuilder(Options.MQTTImage)
                    .WithPortBinding(MqttPort, true)
                    .WithCommand("mosquitto", "-c", "/mosquitto-no-auth.conf")
                    .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("mosquitto"))
                    .WithCleanUp(true)
                    .Build();

                await _container.StartAsync(cancellationToken).ConfigureAwait(false);
                _host = _container.Hostname;
                _port = _container.GetMappedPublicPort(MqttPort);
            }

            // Create and connect shared client
            var factory = new MqttClientFactory();
            _client = factory.CreateMqttClient();

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(_host, _port)
                .WithClientId($"nbomber-shared-{Guid.NewGuid():N}")
                .WithCleanSession()
                .Build();

            // Retry connection with exponential backoff
            var maxRetries = 5;
            for (var i = 0; i < maxRetries; i++)
            {
                try
                {
                    await _client.ConnectAsync(options, cancellationToken).ConfigureAwait(false);
                    break;
                }
                catch when (i < maxRetries - 1)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(100 * Math.Pow(2, i)), cancellationToken)
                        .ConfigureAwait(false);
                }
            }

            _containerInitialized = _client.IsConnected;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize MQTT: {ex.Message}");
            _containerInitialized = false;
        }
    }

    /// <inheritdoc/>
    protected override async Task DisposeCoreAsync()
    {
        if (_client is not null)
        {
            if (_client.IsConnected)
            {
                await _client.DisconnectAsync().ConfigureAwait(false);
            }

            _client.Dispose();
        }

        if (_container is not null)
        {
            await _container.StopAsync().ConfigureAwait(false);
            await _container.DisposeAsync().ConfigureAwait(false);
        }
    }
}
