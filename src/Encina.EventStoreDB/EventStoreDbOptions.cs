namespace Encina.EventStoreDB;

/// <summary>
/// Configuration options for EventStoreDB integration.
/// </summary>
public sealed class EventStoreDbOptions
{
    /// <summary>
    /// Gets or sets the connection string for EventStoreDB.
    /// </summary>
    /// <example>esdb://localhost:2113?tls=false</example>
    public string ConnectionString { get; set; } = "esdb://localhost:2113?tls=false";

    /// <summary>
    /// Gets or sets whether to throw exceptions on concurrency conflicts.
    /// When false, returns an error result instead.
    /// </summary>
    public bool ThrowOnConcurrencyConflict { get; set; }

    /// <summary>
    /// Gets or sets the stream name prefix for aggregate streams.
    /// </summary>
    /// <remarks>
    /// The full stream name will be: {Prefix}{AggregateTypeName}-{Id}
    /// Example: "Order-550e8400-e29b-41d4-a716-446655440000"
    /// </remarks>
    public string StreamNamePrefix { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the batch size for reading events from a stream.
    /// </summary>
    public int ReadBatchSize { get; set; } = 500;

    /// <summary>
    /// Gets or sets whether to use JSON serialization for events.
    /// When true, uses System.Text.Json; when false, you must provide a custom serializer.
    /// </summary>
    public bool UseJsonSerialization { get; set; } = true;
}
