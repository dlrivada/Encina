using System.Text;
using System.Text.Json;
using EventStore.Client;

namespace Encina.EventStoreDB;

/// <summary>
/// Default JSON-based event serializer using System.Text.Json.
/// </summary>
public sealed class JsonEventSerializer : IEventSerializer
{
    private readonly JsonSerializerOptions _options;
    private readonly IEventTypeResolver _eventTypeResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonEventSerializer"/> class.
    /// </summary>
    /// <param name="eventTypeResolver">The event type resolver.</param>
    /// <param name="options">Optional JSON serializer options.</param>
    public JsonEventSerializer(
        IEventTypeResolver eventTypeResolver,
        JsonSerializerOptions? options = null)
    {
        _eventTypeResolver = eventTypeResolver ?? throw new ArgumentNullException(nameof(eventTypeResolver));
        _options = options ?? new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    /// <inheritdoc />
    public EventData Serialize(object domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var eventType = domainEvent.GetType();
        var eventTypeName = _eventTypeResolver.GetTypeName(eventType);
        var jsonData = JsonSerializer.SerializeToUtf8Bytes(domainEvent, eventType, _options);

        // Create metadata with type information
        var metadata = new EventMetadata
        {
            ClrType = eventType.AssemblyQualifiedName ?? eventType.FullName ?? eventType.Name
        };
        var metadataBytes = JsonSerializer.SerializeToUtf8Bytes(metadata, _options);

        return new EventData(
            Uuid.NewUuid(),
            eventTypeName,
            jsonData,
            metadataBytes);
    }

    /// <inheritdoc />
    public object? Deserialize(ResolvedEvent resolvedEvent)
    {
        if (resolvedEvent.Event.Data.IsEmpty)
        {
            return null;
        }

        // Try to get type from metadata first
        var clrType = GetClrTypeFromMetadata(resolvedEvent.Event.Metadata);

        // Fall back to event type name resolution
        clrType ??= _eventTypeResolver.ResolveType(resolvedEvent.Event.EventType);

        if (clrType is null)
        {
            return null;
        }

        return JsonSerializer.Deserialize(resolvedEvent.Event.Data.Span, clrType, _options);
    }

    private Type? GetClrTypeFromMetadata(ReadOnlyMemory<byte> metadata)
    {
        if (metadata.IsEmpty)
        {
            return null;
        }

        try
        {
            var eventMetadata = JsonSerializer.Deserialize<EventMetadata>(metadata.Span, _options);
            if (!string.IsNullOrEmpty(eventMetadata?.ClrType))
            {
                return Type.GetType(eventMetadata.ClrType);
            }
        }
        catch
        {
            // Ignore deserialization errors for metadata
        }

        return null;
    }

    private sealed class EventMetadata
    {
        public string? ClrType { get; set; }
    }
}
