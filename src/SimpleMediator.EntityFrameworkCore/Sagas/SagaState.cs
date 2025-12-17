using SimpleMediator.Messaging.Sagas;

namespace SimpleMediator.EntityFrameworkCore.Sagas;

/// <summary>
/// Entity Framework Core implementation of <see cref="ISagaState"/>.
/// Represents the persisted state of a saga (distributed transaction).
/// </summary>
/// <remarks>
/// <para>
/// A Saga is a sequence of local transactions where each transaction updates data within a
/// single service/aggregate. If a step fails, compensating transactions are executed to
/// undo the changes made by preceding steps.
/// </para>
/// <para>
/// <b>Saga Pattern Types</b>:
/// <list type="bullet">
/// <item><description><b>Choreography</b>: Services publish events, others listen and react</description></item>
/// <item><description><b>Orchestration</b>: Central coordinator directs the saga (this implementation)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Example Saga: Order Processing</b>
/// <list type="number">
/// <item><description>Reserve inventory</description></item>
/// <item><description>Charge customer</description></item>
/// <item><description>Ship order</description></item>
/// <item><description>If any step fails, compensate previous steps</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class SagaState : ISagaState
{
    /// <summary>
    /// Gets or sets the unique identifier for the saga instance.
    /// </summary>
    public Guid SagaId { get; set; }

    /// <summary>
    /// Gets or sets the fully qualified type name of the saga.
    /// </summary>
    /// <remarks>
    /// Used to deserialize and resume the saga.
    /// Format: "Namespace.SagaTypeName, AssemblyName"
    /// </remarks>
    public required string SagaType { get; set; }

    /// <summary>
    /// Gets or sets the JSON-serialized saga data.
    /// </summary>
    /// <remarks>
    /// Contains the saga's state (accumulated data from all steps).
    /// </remarks>
    public required string Data { get; set; }

    /// <summary>
    /// Gets or sets the current step index in the saga.
    /// </summary>
    /// <remarks>
    /// Zero-based index indicating which step is currently executing.
    /// </remarks>
    public int CurrentStep { get; set; }

    /// <summary>
    /// Gets or sets the current status of the saga as a string.
    /// </summary>
    string ISagaState.Status
    {
        get => Status.ToString();
        set => Status = Enum.Parse<SagaStatus>(value);
    }

    /// <summary>
    /// Gets or sets the current status of the saga.
    /// </summary>
    public SagaStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the saga was created.
    /// </summary>
    public DateTime StartedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the saga was last updated.
    /// </summary>
    public DateTime LastUpdatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the saga completed or was compensated.
    /// </summary>
    public DateTime? CompletedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the error message if a step failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID for tracing.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the saga should timeout and be compensated.
    /// </summary>
    public DateTime? TimeoutAtUtc { get; set; }

    /// <summary>
    /// Gets or sets optional metadata about the saga.
    /// </summary>
    /// <remarks>
    /// Can store user ID, tenant ID, origin system, etc.
    /// Stored as JSON.
    /// </remarks>
    public string? Metadata { get; set; }
}

/// <summary>
/// Represents the current status of a saga.
/// </summary>
public enum SagaStatus
{
    /// <summary>
    /// Saga is currently executing forward steps.
    /// </summary>
    Running = 0,

    /// <summary>
    /// All forward steps completed successfully.
    /// </summary>
    Completed = 1,

    /// <summary>
    /// A step failed and compensation is in progress.
    /// </summary>
    Compensating = 2,

    /// <summary>
    /// All compensating actions completed successfully (rollback complete).
    /// </summary>
    Compensated = 3,

    /// <summary>
    /// Compensation failed - manual intervention required.
    /// </summary>
    Failed = 4,

    /// <summary>
    /// Saga exceeded timeout and is being compensated.
    /// </summary>
    TimedOut = 5
}
