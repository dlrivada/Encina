namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Lifecycle status of a Data Subject Rights request.
/// </summary>
/// <remarks>
/// <para>
/// Each DSR request progresses through a defined lifecycle:
/// <c>Received</c> -> <c>IdentityVerified</c> -> <c>InProgress</c> -> <c>Completed</c> | <c>Rejected</c>.
/// </para>
/// <para>
/// The <c>Extended</c> status applies when the controller needs additional time
/// (up to 2 months) for complex or numerous requests, as permitted by Article 12(3).
/// The <c>Expired</c> status is set when the 30-day (or extended) deadline passes
/// without completion.
/// </para>
/// </remarks>
public enum DSRRequestStatus
{
    /// <summary>
    /// The request has been received but not yet processed.
    /// </summary>
    /// <remarks>Initial status. The 30-day clock starts from this point (Article 12(3)).</remarks>
    Received,

    /// <summary>
    /// The identity of the data subject has been verified.
    /// </summary>
    /// <remarks>
    /// The controller may request additional information to confirm the identity
    /// of the data subject before processing the request (Article 12(6)).
    /// </remarks>
    IdentityVerified,

    /// <summary>
    /// The request is actively being processed.
    /// </summary>
    InProgress,

    /// <summary>
    /// The request has been fulfilled successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// The request has been rejected with a stated reason.
    /// </summary>
    /// <remarks>
    /// The controller must inform the data subject of the reasons for not taking action,
    /// their right to lodge a complaint, and their right to a judicial remedy (Article 12(4)).
    /// </remarks>
    Rejected,

    /// <summary>
    /// The deadline has been extended by up to 2 additional months.
    /// </summary>
    /// <remarks>
    /// Article 12(3) permits extension taking into account the complexity and number of requests.
    /// The controller must inform the data subject within one month of receipt, together
    /// with the reasons for the delay.
    /// </remarks>
    Extended,

    /// <summary>
    /// The deadline has passed without the request being completed.
    /// </summary>
    /// <remarks>
    /// An expired request indicates a potential compliance violation.
    /// The data subject may lodge a complaint with a supervisory authority (Article 77)
    /// or seek a judicial remedy (Article 79).
    /// </remarks>
    Expired
}
