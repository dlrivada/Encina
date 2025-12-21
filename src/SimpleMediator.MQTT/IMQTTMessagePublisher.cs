using LanguageExt;

namespace SimpleMediator.MQTT;

/// <summary>
/// Interface for publishing messages through MQTT.
/// </summary>
public interface IMQTTMessagePublisher
{
    /// <summary>
    /// Publishes a message to a topic.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="topic">The topic. If null, derived from message type.</param>
    /// <param name="qos">The quality of service level. If null, uses the default QoS.</param>
    /// <param name="retain">Whether to retain the message on the broker.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Either a MediatorError or Unit on success.</returns>
    ValueTask<Either<MediatorError, Unit>> PublishAsync<TMessage>(
        TMessage message,
        string? topic = null,
        MqttQualityOfService? qos = null,
        bool retain = false,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    /// Subscribes to a topic.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="handler">The message handler.</param>
    /// <param name="topic">The topic to subscribe to.</param>
    /// <param name="qos">The quality of service level for the subscription.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A subscription that can be disposed to unsubscribe.</returns>
    ValueTask<IAsyncDisposable> SubscribeAsync<TMessage>(
        Func<TMessage, ValueTask> handler,
        string topic,
        MqttQualityOfService? qos = null,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    /// Subscribes to topics matching a wildcard pattern.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="handler">The message handler with topic name.</param>
    /// <param name="topicFilter">The topic filter (e.g., "sensors/+/temperature" or "events/#").</param>
    /// <param name="qos">The quality of service level for the subscription.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A subscription that can be disposed to unsubscribe.</returns>
    ValueTask<IAsyncDisposable> SubscribePatternAsync<TMessage>(
        Func<string, TMessage, ValueTask> handler,
        string topicFilter,
        MqttQualityOfService? qos = null,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    /// Gets a value indicating whether the client is connected.
    /// </summary>
    bool IsConnected { get; }
}
