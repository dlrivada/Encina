using System.Buffers;
using System.Text.Json;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using static LanguageExt.Prelude;

namespace SimpleMediator.MQTT;

/// <summary>
/// MQTT-based implementation of the message publisher.
/// </summary>
#pragma warning disable CA1848 // Use LoggerMessage delegates
public sealed class MQTTMessagePublisher : IMQTTMessagePublisher, IAsyncDisposable
{
    private readonly MqttClient _client;
    private readonly ILogger<MQTTMessagePublisher> _logger;
    private readonly SimpleMediatorMQTTOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="MQTTMessagePublisher"/> class.
    /// </summary>
    /// <param name="client">The MQTT client.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The configuration options.</param>
    public MQTTMessagePublisher(
        MqttClient client,
        ILogger<MQTTMessagePublisher> logger,
        IOptions<SimpleMediatorMQTTOptions> options)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _client = client;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public bool IsConnected => _client.IsConnected;

    /// <inheritdoc />
    public async ValueTask<Either<MediatorError, Unit>> PublishAsync<TMessage>(
        TMessage message,
        string? topic = null,
        MqttQualityOfService? qos = null,
        bool retain = false,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);

        var effectiveTopic = topic ?? $"{_options.TopicPrefix}/{typeof(TMessage).Name}";
        var effectiveQos = qos ?? _options.QualityOfService;

        try
        {
            _logger.LogDebug(
                "Publishing message of type {MessageType} to topic {Topic} with QoS {QoS}",
                typeof(TMessage).Name,
                effectiveTopic,
                effectiveQos);

            var payload = JsonSerializer.SerializeToUtf8Bytes(message);

            var mqttQos = effectiveQos switch
            {
                MqttQualityOfService.AtMostOnce => MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce,
                MqttQualityOfService.AtLeastOnce => MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce,
                MqttQualityOfService.ExactlyOnce => MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce,
                _ => MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce
            };

            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(effectiveTopic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(mqttQos)
                .WithRetainFlag(retain)
                .Build();

            await _client.PublishAsync(mqttMessage, cancellationToken).ConfigureAwait(false);

            _logger.LogDebug(
                "Successfully published message to topic {Topic}",
                effectiveTopic);

            return Right<MediatorError, Unit>(Unit.Default);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish message of type {MessageType} to topic {Topic}",
                typeof(TMessage).Name,
                effectiveTopic);

            return Left<MediatorError, Unit>(
                MediatorErrors.FromException(
                    "MQTT_PUBLISH_FAILED",
                    ex,
                    $"Failed to publish message of type {typeof(TMessage).Name} to topic {effectiveTopic}."));
        }
    }

    /// <inheritdoc />
    public async ValueTask<IAsyncDisposable> SubscribeAsync<TMessage>(
        Func<TMessage, ValueTask> handler,
        string topic,
        MqttQualityOfService? qos = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(topic);

        var effectiveQos = qos ?? _options.QualityOfService;

        var mqttQos = effectiveQos switch
        {
            MqttQualityOfService.AtMostOnce => MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce,
            MqttQualityOfService.AtLeastOnce => MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce,
            MqttQualityOfService.ExactlyOnce => MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce,
            _ => MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce
        };

        _logger.LogDebug(
            "Subscribing to topic {Topic} for messages of type {MessageType}",
            topic,
            typeof(TMessage).Name);

        var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(f => f.WithTopic(topic).WithQualityOfServiceLevel(mqttQos))
            .Build();

        await _client.SubscribeAsync(subscribeOptions, cancellationToken).ConfigureAwait(false);

        var subscription = new MqttSubscription<TMessage>(_client, topic, handler, _logger);

        return subscription;
    }

    /// <inheritdoc />
    public async ValueTask<IAsyncDisposable> SubscribePatternAsync<TMessage>(
        Func<string, TMessage, ValueTask> handler,
        string topicFilter,
        MqttQualityOfService? qos = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(topicFilter);

        var effectiveQos = qos ?? _options.QualityOfService;

        var mqttQos = effectiveQos switch
        {
            MqttQualityOfService.AtMostOnce => MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce,
            MqttQualityOfService.AtLeastOnce => MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce,
            MqttQualityOfService.ExactlyOnce => MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce,
            _ => MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce
        };

        _logger.LogDebug(
            "Subscribing to topic pattern {TopicFilter} for messages of type {MessageType}",
            topicFilter,
            typeof(TMessage).Name);

        var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(f => f.WithTopic(topicFilter).WithQualityOfServiceLevel(mqttQos))
            .Build();

        await _client.SubscribeAsync(subscribeOptions, cancellationToken).ConfigureAwait(false);

        var subscription = new MqttPatternSubscription<TMessage>(_client, topicFilter, handler, _logger);

        return subscription;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_client.IsConnected)
        {
            await _client.DisconnectAsync().ConfigureAwait(false);
        }

        _client.Dispose();
    }
}

internal sealed class MqttSubscription<TMessage> : IAsyncDisposable
    where TMessage : class
{
    private readonly MqttClient _client;
    private readonly string _topic;
    private readonly Func<TMessage, ValueTask> _handler;
    private readonly ILogger _logger;

    public MqttSubscription(
        MqttClient client,
        string topic,
        Func<TMessage, ValueTask> handler,
        ILogger logger)
    {
        _client = client;
        _topic = topic;
        _handler = handler;
        _logger = logger;

        _client.ApplicationMessageReceivedAsync += OnMessageReceived;
    }

    private async Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs args)
    {
        if (args.ApplicationMessage.Topic == _topic)
        {
            try
            {
                var message = JsonSerializer.Deserialize<TMessage>(args.ApplicationMessage.Payload.ToArray());
                if (message is not null)
                {
                    await _handler(message).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MQTT message from topic {Topic}", _topic);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _client.ApplicationMessageReceivedAsync -= OnMessageReceived;

        var unsubscribeOptions = new MqttClientUnsubscribeOptionsBuilder()
            .WithTopicFilter(_topic)
            .Build();

        await _client.UnsubscribeAsync(unsubscribeOptions).ConfigureAwait(false);
    }
}

internal sealed class MqttPatternSubscription<TMessage> : IAsyncDisposable
    where TMessage : class
{
    private readonly MqttClient _client;
    private readonly string _topicFilter;
    private readonly Func<string, TMessage, ValueTask> _handler;
    private readonly ILogger _logger;

    public MqttPatternSubscription(
        MqttClient client,
        string topicFilter,
        Func<string, TMessage, ValueTask> handler,
        ILogger logger)
    {
        _client = client;
        _topicFilter = topicFilter;
        _handler = handler;
        _logger = logger;

        _client.ApplicationMessageReceivedAsync += OnMessageReceived;
    }

    private async Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs args)
    {
        // Simple pattern matching (could be improved with proper MQTT topic matching)
        if (MatchesTopic(args.ApplicationMessage.Topic, _topicFilter))
        {
            try
            {
                var message = JsonSerializer.Deserialize<TMessage>(args.ApplicationMessage.Payload.ToArray());
                if (message is not null)
                {
                    await _handler(args.ApplicationMessage.Topic, message).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MQTT message from topic {Topic}", args.ApplicationMessage.Topic);
            }
        }
    }

    private static bool MatchesTopic(string topic, string filter)
    {
        // Simple implementation - in production, use proper MQTT topic matching
        if (filter.EndsWith('#'))
        {
            var prefix = filter[..^1];
            return topic.StartsWith(prefix, StringComparison.Ordinal);
        }

        if (filter.Contains('+'))
        {
            var filterParts = filter.Split('/');
            var topicParts = topic.Split('/');

            if (filterParts.Length != topicParts.Length)
            {
                return false;
            }

            for (var i = 0; i < filterParts.Length; i++)
            {
                if (filterParts[i] != "+" && filterParts[i] != topicParts[i])
                {
                    return false;
                }
            }

            return true;
        }

        return topic == filter;
    }

    public async ValueTask DisposeAsync()
    {
        _client.ApplicationMessageReceivedAsync -= OnMessageReceived;

        var unsubscribeOptions = new MqttClientUnsubscribeOptionsBuilder()
            .WithTopicFilter(_topicFilter)
            .Build();

        await _client.UnsubscribeAsync(unsubscribeOptions).ConfigureAwait(false);
    }
}
