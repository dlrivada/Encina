using SimpleMediator.Messaging.Sagas;

namespace SimpleMediator.Dapper.PostgreSQL.Sagas;

/// <summary>
/// Dapper implementation of saga state for distributed transaction orchestration.
/// Tracks saga progress and supports compensation on failure.
/// </summary>
public sealed class SagaState : ISagaState
{
    /// <summary>
    /// Gets or sets the unique identifier for the saga.
    /// </summary>
    public Guid SagaId { get; set; }

    /// <summary>
    /// Gets or sets the full type name of the saga.
    /// </summary>
    public string SagaType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JSON serialized saga data.
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the saga status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the saga was started (UTC).
    /// </summary>
    public DateTime StartedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets when the saga was last updated (UTC).
    /// </summary>
    public DateTime LastUpdatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets when the saga completed (UTC).
    /// Null if not yet completed.
    /// </summary>
    public DateTime? CompletedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the error message if the saga failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the current step number in the saga execution.
    /// </summary>
    public int CurrentStep { get; set; }
}
