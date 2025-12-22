namespace Encina.Messaging.Choreography;

/// <summary>
/// Represents the persisted state of a choreography saga.
/// </summary>
public interface IChoreographyState
{
    /// <summary>
    /// Gets or sets the unique correlation ID for the saga.
    /// </summary>
    string CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the saga type name.
    /// </summary>
    string SagaType { get; set; }

    /// <summary>
    /// Gets or sets the serialized saga state data.
    /// </summary>
    string? StateData { get; set; }

    /// <summary>
    /// Gets or sets the current status of the saga.
    /// </summary>
    ChoreographyStatus Status { get; set; }

    /// <summary>
    /// Gets or sets when the saga was started.
    /// </summary>
    DateTime StartedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets when the saga was last updated.
    /// </summary>
    DateTime LastUpdatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets when the saga completed (if completed).
    /// </summary>
    DateTime? CompletedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the error message if the saga failed.
    /// </summary>
    string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the number of events processed in this saga.
    /// </summary>
    int EventCount { get; set; }
}

/// <summary>
/// Status of a choreography saga.
/// </summary>
public enum ChoreographyStatus
{
    /// <summary>
    /// Saga is currently running.
    /// </summary>
    Running = 0,

    /// <summary>
    /// Saga completed successfully.
    /// </summary>
    Completed = 1,

    /// <summary>
    /// Saga failed and is being compensated.
    /// </summary>
    Compensating = 2,

    /// <summary>
    /// Saga compensation completed.
    /// </summary>
    Compensated = 3,

    /// <summary>
    /// Saga failed and compensation also failed.
    /// </summary>
    Failed = 4
}
