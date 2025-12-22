namespace Encina.Marten;

/// <summary>
/// Configuration options for Encina Marten integration.
/// </summary>
public sealed class EncinaMartenOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to automatically publish domain events
    /// from aggregates after command execution. Default is true.
    /// </summary>
    public bool AutoPublishDomainEvents { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to use optimistic concurrency
    /// when saving aggregates. Default is true.
    /// </summary>
    public bool UseOptimisticConcurrency { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to throw on concurrency conflicts.
    /// When false, returns a EncinaError instead. Default is false.
    /// </summary>
    public bool ThrowOnConcurrencyConflict { get; set; }

    /// <summary>
    /// Gets or sets the default stream prefix for event streams.
    /// Default is empty (no prefix).
    /// </summary>
    public string StreamPrefix { get; set; } = string.Empty;
}
