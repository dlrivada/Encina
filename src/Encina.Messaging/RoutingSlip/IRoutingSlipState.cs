namespace Encina.Messaging.RoutingSlip;

/// <summary>
/// Represents the persisted state of a routing slip.
/// </summary>
/// <remarks>
/// <para>
/// A routing slip is a dynamic message routing pattern where a message carries
/// its own itinerary. Unlike sagas where steps are predefined, routing slips
/// can have their route modified during execution.
/// </para>
/// <para>
/// <b>Key differences from Saga</b>:
/// <list type="bullet">
/// <item><description>Steps can be added/removed during execution</description></item>
/// <item><description>Each step can modify the remaining itinerary</description></item>
/// <item><description>Route is attached to the message, not the orchestrator</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IRoutingSlipState
{
    /// <summary>
    /// Gets or sets the unique routing slip identifier.
    /// </summary>
    Guid RoutingSlipId { get; set; }

    /// <summary>
    /// Gets or sets the routing slip type name.
    /// </summary>
    string SlipType { get; set; }

    /// <summary>
    /// Gets or sets the serialized message/data being routed.
    /// </summary>
    string Data { get; set; }

    /// <summary>
    /// Gets or sets the serialized itinerary (remaining steps).
    /// </summary>
    /// <remarks>
    /// Contains the list of steps yet to be executed. Modified as steps
    /// complete or when steps are dynamically added/removed.
    /// </remarks>
    string Itinerary { get; set; }

    /// <summary>
    /// Gets or sets the serialized activity log (completed steps).
    /// </summary>
    /// <remarks>
    /// Contains the history of executed steps for compensation purposes.
    /// </remarks>
    string ActivityLog { get; set; }

    /// <summary>
    /// Gets or sets the current status of the routing slip.
    /// </summary>
    string Status { get; set; }

    /// <summary>
    /// Gets or sets the current step index (0-based).
    /// </summary>
    int CurrentStep { get; set; }

    /// <summary>
    /// Gets or sets when the routing slip was created.
    /// </summary>
    DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets when the routing slip was last updated.
    /// </summary>
    DateTime LastUpdatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets when the routing slip completed.
    /// </summary>
    DateTime? CompletedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the routing slip should timeout.
    /// </summary>
    DateTime? TimeoutAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the error message if the routing slip failed.
    /// </summary>
    string? ErrorMessage { get; set; }
}
