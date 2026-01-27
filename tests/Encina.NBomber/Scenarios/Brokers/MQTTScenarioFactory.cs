using System.Collections.Concurrent;
using Encina.NBomber.Scenarios.Brokers.Providers;
using MQTTnet;
using MQTTnet.Protocol;
using NBomber.Contracts;
using NBomber.CSharp;

namespace Encina.NBomber.Scenarios.Brokers;

/// <summary>
/// Factory for creating MQTT load test scenarios.
/// Tests QoS 0 and QoS 1 publishing, and subscription throughput.
/// </summary>
public sealed class MQTTScenarioFactory
{
    private readonly BrokerScenarioContext _context;
    private MQTTProviderFactory? _mqttFactory;
    private IMqttClient? _publishClient;
    private readonly ConcurrentDictionary<string, long> _metrics = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MQTTScenarioFactory"/> class.
    /// </summary>
    /// <param name="context">The broker scenario context.</param>
    public MQTTScenarioFactory(BrokerScenarioContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _mqttFactory = context.ProviderFactory as MQTTProviderFactory;
    }

    /// <summary>
    /// Creates all MQTT scenarios.
    /// </summary>
    /// <returns>A collection of load test scenarios.</returns>
    public IEnumerable<ScenarioProps> CreateScenarios()
    {
        yield return CreatePublishQoS0Scenario();
        yield return CreatePublishQoS1Scenario();
        yield return CreateSubscribeThroughputScenario();
    }

    /// <summary>
    /// Creates the QoS 0 publish scenario.
    /// Tests fire-and-forget delivery with maximum throughput.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreatePublishQoS0Scenario()
    {
        var topic = $"{_context.Options.TopicPrefix}/qos0/throughput";

        return Scenario.Create(
            name: "mqtt-publish-qos0",
            run: async scenarioContext =>
            {
                try
                {
                    if (_publishClient is null || !_publishClient.IsConnected)
                    {
                        return Response.Fail("Client not connected", statusCode: "no_client");
                    }

                    var message = _context.CreateTestMessage();

                    var mqttMessage = new MqttApplicationMessageBuilder()
                        .WithTopic(topic)
                        .WithPayload(message)
                        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
                        .Build();

                    await _publishClient.PublishAsync(mqttMessage).ConfigureAwait(false);

                    _metrics.AddOrUpdate("published_qos0", 1, (_, c) => c + 1);
                    return Response.Ok(statusCode: "qos0");
                }
                catch (Exception ex)
                {
                    return Response.Fail(ex.Message, statusCode: "exception");
                }
            })
            .WithInit(async _ =>
            {
                _publishClient = await _mqttFactory!.CreateClientAsync("nbomber-qos0-publisher")
                    .ConfigureAwait(false);
            })
            .WithClean(async _ =>
            {
                var published = _metrics.GetValueOrDefault("published_qos0", 0);
                Console.WriteLine($"MQTT QoS 0 publish - Published: {published}");
                _metrics.Clear();

                if (_publishClient is not null)
                {
                    if (_publishClient.IsConnected)
                    {
                        await _publishClient.DisconnectAsync().ConfigureAwait(false);
                    }

                    _publishClient.Dispose();
                }
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: 500,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromMinutes(1)));
    }

    /// <summary>
    /// Creates the QoS 1 publish scenario.
    /// Tests at-least-once delivery with acknowledgments.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreatePublishQoS1Scenario()
    {
        var topic = $"{_context.Options.TopicPrefix}/qos1/throughput";

        return Scenario.Create(
            name: "mqtt-publish-qos1",
            run: async scenarioContext =>
            {
                try
                {
                    if (_publishClient is null || !_publishClient.IsConnected)
                    {
                        return Response.Fail("Client not connected", statusCode: "no_client");
                    }

                    var message = _context.CreateTestMessage();

                    var mqttMessage = new MqttApplicationMessageBuilder()
                        .WithTopic(topic)
                        .WithPayload(message)
                        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                        .Build();

                    var result = await _publishClient.PublishAsync(mqttMessage).ConfigureAwait(false);

                    if (result.ReasonCode == MqttClientPublishReasonCode.Success)
                    {
                        _metrics.AddOrUpdate("published_qos1", 1, (_, c) => c + 1);
                        return Response.Ok(statusCode: "qos1_acked");
                    }

                    _metrics.AddOrUpdate("publish_failed", 1, (_, c) => c + 1);
                    return Response.Ok(statusCode: $"failed_{result.ReasonCode}");
                }
                catch (Exception ex)
                {
                    return Response.Fail(ex.Message, statusCode: "exception");
                }
            })
            .WithInit(async _ =>
            {
                _publishClient = await _mqttFactory!.CreateClientAsync("nbomber-qos1-publisher")
                    .ConfigureAwait(false);
            })
            .WithClean(async _ =>
            {
                var published = _metrics.GetValueOrDefault("published_qos1", 0);
                var failed = _metrics.GetValueOrDefault("publish_failed", 0);
                Console.WriteLine($"MQTT QoS 1 publish - Published: {published}, Failed: {failed}");
                _metrics.Clear();

                if (_publishClient is not null)
                {
                    if (_publishClient.IsConnected)
                    {
                        await _publishClient.DisconnectAsync().ConfigureAwait(false);
                    }

                    _publishClient.Dispose();
                }
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: 200,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromMinutes(1)));
    }

    /// <summary>
    /// Creates the subscribe throughput scenario.
    /// Tests message reception rate for subscribers.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreateSubscribeThroughputScenario()
    {
        var topic = $"{_context.Options.TopicPrefix}/subscribe/throughput";
        IMqttClient? subscriberClient = null;

        return Scenario.Create(
            name: "mqtt-subscribe-throughput",
            run: async scenarioContext =>
            {
                try
                {
                    if (_publishClient is null || !_publishClient.IsConnected)
                    {
                        return Response.Fail("Publisher not connected", statusCode: "no_publisher");
                    }

                    var message = _context.CreateTestMessage();

                    var mqttMessage = new MqttApplicationMessageBuilder()
                        .WithTopic(topic)
                        .WithPayload(message)
                        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                        .Build();

                    await _publishClient.PublishAsync(mqttMessage).ConfigureAwait(false);
                    _metrics.AddOrUpdate("published", 1, (_, c) => c + 1);

                    return Response.Ok(statusCode: "published");
                }
                catch (Exception ex)
                {
                    return Response.Fail(ex.Message, statusCode: "exception");
                }
            })
            .WithInit(async _ =>
            {
                _publishClient = await _mqttFactory!.CreateClientAsync("nbomber-sub-publisher")
                    .ConfigureAwait(false);

                subscriberClient = await _mqttFactory.CreateClientAsync("nbomber-subscriber")
                    .ConfigureAwait(false);

                subscriberClient.ApplicationMessageReceivedAsync += e =>
                {
                    _metrics.AddOrUpdate("received", 1, (_, c) => c + 1);
                    return Task.CompletedTask;
                };

                var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter(f => f.WithTopic(topic).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce))
                    .Build();

                await subscriberClient.SubscribeAsync(subscribeOptions).ConfigureAwait(false);
            })
            .WithClean(async _ =>
            {
                // Give time for remaining messages to be received
                await Task.Delay(500).ConfigureAwait(false);

                var published = _metrics.GetValueOrDefault("published", 0);
                var received = _metrics.GetValueOrDefault("received", 0);
                var lossRate = published > 0 ? (double)(published - received) / published * 100 : 0;

                Console.WriteLine($"MQTT subscribe throughput - Published: {published}, Received: {received}, " +
                    $"Loss rate: {lossRate:F1}%");
                _metrics.Clear();

                if (_publishClient is not null)
                {
                    if (_publishClient.IsConnected)
                    {
                        await _publishClient.DisconnectAsync().ConfigureAwait(false);
                    }

                    _publishClient.Dispose();
                }

                if (subscriberClient is not null)
                {
                    if (subscriberClient.IsConnected)
                    {
                        await subscriberClient.DisconnectAsync().ConfigureAwait(false);
                    }

                    subscriberClient.Dispose();
                }
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: 100,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromMinutes(1)));
    }
}
