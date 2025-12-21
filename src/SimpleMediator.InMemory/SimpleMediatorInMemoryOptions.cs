namespace SimpleMediator.InMemory;

/// <summary>
/// Configuration options for SimpleMediator In-Memory message bus.
/// </summary>
public sealed class SimpleMediatorInMemoryOptions
{
    /// <summary>
    /// Gets or sets the bounded capacity for the message channel.
    /// </summary>
    public int BoundedCapacity { get; set; } = 1000;

    /// <summary>
    /// Gets or sets a value indicating whether to use unbounded channels.
    /// </summary>
    public bool UseUnboundedChannel { get; set; }

    /// <summary>
    /// Gets or sets the full mode behavior when the channel is full.
    /// </summary>
    public InMemoryFullMode FullMode { get; set; } = InMemoryFullMode.Wait;

    /// <summary>
    /// Gets or sets the number of worker tasks for processing messages.
    /// </summary>
    public int WorkerCount { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// Gets or sets a value indicating whether to allow synchronous continuations.
    /// </summary>
    public bool AllowSynchronousContinuations { get; set; }
}

/// <summary>
/// Defines the behavior when the in-memory channel is full.
/// </summary>
public enum InMemoryFullMode
{
    /// <summary>
    /// Wait for space to become available.
    /// </summary>
    Wait,

    /// <summary>
    /// Drop the oldest item to make room.
    /// </summary>
    DropOldest,

    /// <summary>
    /// Drop the newest item (the one being written).
    /// </summary>
    DropNewest
}
