namespace SimpleMediator.Messaging.Sagas;

/// <summary>
/// Represents the persisted state of a saga.
/// </summary>
/// <remarks>
/// <para>
/// Sagas are long-running business processes that coordinate multiple steps.
/// This interface defines the state that must be persisted to recover from failures.
/// </para>
/// <para>
/// <b>Saga Lifecycle</b>:
/// <list type="number">
/// <item><description><b>Running</b>: Currently executing steps</description></item>
/// <item><description><b>Completed</b>: All steps finished successfully</description></item>
/// <item><description><b>Compensating</b>: Rolling back completed steps</description></item>
/// <item><description><b>Failed</b>: Compensation failed, manual intervention needed</description></item>
/// </list>
/// </para>
/// </remarks>
public interface ISagaState
{
    /// <summary>
    /// Gets or sets the unique saga identifier.
    /// </summary>
    Guid SagaId { get; set; }

    /// <summary>
    /// Gets or sets the saga type name.
    /// </summary>
    string SagaType { get; set; }

    /// <summary>
    /// Gets or sets the serialized saga data.
    /// </summary>
    /// <remarks>
    /// Contains the business data and state for the saga.
    /// </remarks>
    string Data { get; set; }

    /// <summary>
    /// Gets or sets the current status of the saga.
    /// </summary>
    string Status { get; set; }

    /// <summary>
    /// Gets or sets the current step index.
    /// </summary>
    /// <remarks>
    /// Indicates which step is currently being executed or was last executed.
    /// </remarks>
    int CurrentStep { get; set; }

    /// <summary>
    /// Gets or sets when the saga started.
    /// </summary>
    DateTime StartedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets when the saga completed (successfully or with failure).
    /// </summary>
    DateTime? CompletedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the error message if the saga failed.
    /// </summary>
    string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets when the saga was last updated.
    /// </summary>
    DateTime LastUpdatedAtUtc { get; set; }
}
