using Dapr.Client;

namespace Encina.Dapr;

/// <summary>
/// Marker interface for requests that publish events via Dapr Pub/Sub.
/// </summary>
/// <remarks>
/// Dapr Pub/Sub provides:
/// - Cloud-agnostic event publishing (Redis, RabbitMQ, Azure Service Bus, Kafka, etc.)
/// - At-least-once delivery guarantees
/// - Message routing and filtering
/// - Dead letter queues
/// - CloudEvents specification compliance
///
/// Returns <see cref="LanguageExt.Unit"/> from LanguageExt for void operations in Railway Oriented Programming.
/// </remarks>
/// <example>
/// <code>
/// // Define an event to publish
/// public record OrderPlacedEvent(string OrderId, decimal Total)
///     : IRequest&lt;Unit&gt;, IDaprPubSubRequest
/// {
///     public string PubSubName => "pubsub";
///     public string TopicName => "orders";
///
///     public async Task PublishAsync(
///         DaprClient daprClient,
///         CancellationToken cancellationToken)
///     {
///         await daprClient.PublishEventAsync(
///             PubSubName,
///             TopicName,
///             new { OrderId, Total },
///             cancellationToken);
///     }
/// }
///
/// // Use through Encina
/// var result = await Encina.Send(new OrderPlacedEvent("12345", 99.99m));
/// result.Match(
///     Right: _ => Console.WriteLine("Event published successfully"),
///     Left: error => Console.WriteLine($"Error: {error.Message}")
/// );
/// </code>
/// </example>
public interface IDaprPubSubRequest : IRequest<LanguageExt.Unit>
{
    /// <summary>
    /// Gets the name of the Dapr Pub/Sub component.
    /// </summary>
    string PubSubName { get; }

    /// <summary>
    /// Gets the topic name to publish to.
    /// </summary>
    string TopicName { get; }

    /// <summary>
    /// Publishes the event using the provided Dapr client.
    /// </summary>
    /// <param name="daprClient">The Dapr client instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishAsync(DaprClient daprClient, CancellationToken cancellationToken);
}
