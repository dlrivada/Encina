namespace Encina.Messaging.Serialization;

/// <summary>
/// Provides serialization and deserialization of message payloads for outbox/inbox storage.
/// </summary>
/// <remarks>
/// <para>
/// This abstraction replaces the hardcoded <c>JsonSerializer.Serialize()</c> calls in
/// <c>OutboxOrchestrator</c> and <c>InboxOrchestrator</c>, enabling pluggable serialization
/// strategies including encryption, compression, and schema versioning via the decorator pattern.
/// </para>
/// <para>
/// The default implementation (<c>JsonMessageSerializer</c>) uses <c>System.Text.Json</c>
/// with camelCase naming. The <c>Encina.Messaging.Encryption</c> package provides
/// <c>EncryptingMessageSerializer</c>, a decorator that adds transparent payload encryption.
/// </para>
/// <para>
/// Implementations must be thread-safe and suitable for singleton registration.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Serialize a notification for outbox storage
/// var json = serializer.Serialize(new OrderPlacedNotification(orderId));
///
/// // Deserialize back to the original type
/// var notification = serializer.Deserialize&lt;OrderPlacedNotification&gt;(json);
///
/// // Deserialize with runtime type resolution
/// var message = serializer.Deserialize(json, typeof(OrderPlacedNotification));
/// </code>
/// </example>
public interface IMessageSerializer
{
    /// <summary>
    /// Serializes a message to its string representation for storage.
    /// </summary>
    /// <typeparam name="T">The type of message to serialize.</typeparam>
    /// <param name="message">The message instance to serialize.</param>
    /// <returns>The serialized string representation (typically JSON, optionally encrypted).</returns>
    string Serialize<T>(T message);

    /// <summary>
    /// Deserializes a string representation back to a strongly-typed message.
    /// </summary>
    /// <typeparam name="T">The expected type of the deserialized message.</typeparam>
    /// <param name="content">The serialized string content (may be JSON or encrypted payload).</param>
    /// <returns>
    /// The deserialized message instance, or <c>null</c> if deserialization fails
    /// or the content represents a null value.
    /// </returns>
    T? Deserialize<T>(string content);

    /// <summary>
    /// Deserializes a string representation back to an object using a runtime type.
    /// </summary>
    /// <param name="content">The serialized string content (may be JSON or encrypted payload).</param>
    /// <param name="type">The runtime type to deserialize into.</param>
    /// <returns>
    /// The deserialized object instance, or <c>null</c> if deserialization fails
    /// or the content represents a null value.
    /// </returns>
    object? Deserialize(string content, Type type);
}
