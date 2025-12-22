using EventStore.Client;

namespace Encina.EventStoreDB;

/// <summary>
/// Interface for serializing and deserializing domain events.
/// </summary>
public interface IEventSerializer
{
    /// <summary>
    /// Serializes a domain event to EventStore event data.
    /// </summary>
    /// <param name="domainEvent">The domain event to serialize.</param>
    /// <returns>The serialized event data.</returns>
    EventData Serialize(object domainEvent);

    /// <summary>
    /// Deserializes a resolved event to a domain event.
    /// </summary>
    /// <param name="resolvedEvent">The resolved event from EventStoreDB.</param>
    /// <returns>The deserialized domain event, or null if deserialization fails.</returns>
    object? Deserialize(ResolvedEvent resolvedEvent);
}
