using System.Text.Json;

namespace Encina.Messaging.Serialization;

/// <summary>
/// Default <see cref="IMessageSerializer"/> implementation using <see cref="System.Text.Json"/>.
/// </summary>
/// <remarks>
/// <para>
/// Uses camelCase property naming and no indentation, matching the serialization behavior
/// used by <c>OutboxOrchestrator</c> and <c>InboxOrchestrator</c>.
/// </para>
/// <para>
/// This class is thread-safe and suitable for singleton registration.
/// The <see cref="JsonSerializerOptions"/> instance is shared and immutable after initialization.
/// </para>
/// <para>
/// When message encryption is enabled, <c>EncryptingMessageSerializer</c> from
/// <c>Encina.Messaging.Encryption</c> decorates this serializer to add transparent
/// encryption/decryption of the serialized JSON payload.
/// </para>
/// </remarks>
public sealed class JsonMessageSerializer : IMessageSerializer
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonMessageSerializer"/> class
    /// with default serialization options (camelCase, no indentation).
    /// </summary>
    public JsonMessageSerializer()
        : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonMessageSerializer"/> class
    /// with custom serialization options.
    /// </summary>
    /// <param name="options">
    /// Custom JSON serializer options. When <c>null</c>, defaults to camelCase naming
    /// with no indentation.
    /// </param>
    public JsonMessageSerializer(JsonSerializerOptions? options)
    {
        _options = options ?? DefaultOptions;
    }

    /// <inheritdoc />
    public string Serialize<T>(T message)
    {
        return JsonSerializer.Serialize(message, _options);
    }

    /// <inheritdoc />
    public T? Deserialize<T>(string content)
    {
        return JsonSerializer.Deserialize<T>(content, _options);
    }

    /// <inheritdoc />
    public object? Deserialize(string content, Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return JsonSerializer.Deserialize(content, type, _options);
    }
}
