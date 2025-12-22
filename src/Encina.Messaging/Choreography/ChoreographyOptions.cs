namespace Encina.Messaging.Choreography;

/// <summary>
/// Configuration options for choreography-based sagas.
/// </summary>
public sealed class ChoreographyOptions
{
    /// <summary>
    /// Gets or sets whether to automatically compensate on failure.
    /// </summary>
    /// <remarks>
    /// When true, registered compensation actions will be executed automatically
    /// if an event reaction fails.
    /// </remarks>
    public bool AutoCompensateOnFailure { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum time to wait for saga completion.
    /// </summary>
    /// <remarks>
    /// If the saga doesn't complete within this time, it will be marked as stuck.
    /// </remarks>
    public TimeSpan SagaTimeout { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Gets or sets whether to persist saga state between events.
    /// </summary>
    /// <remarks>
    /// When true, saga state is persisted after each event, allowing recovery
    /// from failures.
    /// </remarks>
    public bool PersistState { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of compensation retries.
    /// </summary>
    public int MaxCompensationRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the delay between compensation retries.
    /// </summary>
    public TimeSpan CompensationRetryDelay { get; set; } = TimeSpan.FromSeconds(5);
}
