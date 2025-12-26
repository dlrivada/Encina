namespace Encina.Marten.Projections;

/// <summary>
/// Represents the current status of a projection.
/// </summary>
/// <remarks>
/// <para>
/// Projection status tracks how far a projection has processed the event stream.
/// This enables:
/// <list type="bullet">
/// <item><description>Catch-up processing after restart</description></item>
/// <item><description>Progress monitoring during rebuilds</description></item>
/// <item><description>Health checks and diagnostics</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class ProjectionStatus
{
    /// <summary>
    /// Gets or sets the name of the projection.
    /// </summary>
    public string ProjectionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the last processed global position.
    /// </summary>
    /// <remarks>
    /// This is the position of the last event that was successfully processed.
    /// On restart, processing resumes from this position.
    /// </remarks>
    public long LastProcessedPosition { get; set; }

    /// <summary>
    /// Gets or sets when the projection last processed an event.
    /// </summary>
    public DateTime? LastProcessedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the current state of the projection.
    /// </summary>
    public ProjectionState State { get; set; } = ProjectionState.Stopped;

    /// <summary>
    /// Gets or sets the number of events processed since startup.
    /// </summary>
    public long EventsProcessed { get; set; }

    /// <summary>
    /// Gets or sets the error message if the projection has failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets when the projection was last started.
    /// </summary>
    public DateTime? StartedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the estimated lag in events behind the current position.
    /// </summary>
    public long? EventLag { get; set; }

    /// <summary>
    /// Gets or sets whether this projection is a rebuild in progress.
    /// </summary>
    public bool IsRebuilding { get; set; }

    /// <summary>
    /// Gets or sets the rebuild progress percentage (0-100).
    /// </summary>
    public int? RebuildProgressPercent { get; set; }
}

/// <summary>
/// The possible states of a projection.
/// </summary>
public enum ProjectionState
{
    /// <summary>
    /// The projection is not running.
    /// </summary>
    Stopped = 0,

    /// <summary>
    /// The projection is starting up.
    /// </summary>
    Starting = 1,

    /// <summary>
    /// The projection is actively processing events.
    /// </summary>
    Running = 2,

    /// <summary>
    /// The projection is catching up with the event stream.
    /// </summary>
    CatchingUp = 3,

    /// <summary>
    /// The projection is being rebuilt from scratch.
    /// </summary>
    Rebuilding = 4,

    /// <summary>
    /// The projection is paused.
    /// </summary>
    Paused = 5,

    /// <summary>
    /// The projection has encountered an error.
    /// </summary>
    Faulted = 6,

    /// <summary>
    /// The projection is shutting down.
    /// </summary>
    Stopping = 7
}
