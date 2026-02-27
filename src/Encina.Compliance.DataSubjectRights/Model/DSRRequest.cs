namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Represents a Data Subject Rights request tracking its full lifecycle.
/// </summary>
/// <remarks>
/// <para>
/// Each DSR request documents who made the request, what right is being exercised,
/// and the current status of the request. The 30-day deadline is calculated from
/// the receipt date as required by GDPR Article 12(3).
/// </para>
/// <para>
/// The request progresses through statuses:
/// <see cref="DSRRequestStatus.Received"/> ->
/// <see cref="DSRRequestStatus.IdentityVerified"/> ->
/// <see cref="DSRRequestStatus.InProgress"/> ->
/// <see cref="DSRRequestStatus.Completed"/> | <see cref="DSRRequestStatus.Rejected"/>.
/// </para>
/// <para>
/// For complex requests, the deadline may be extended by up to 2 additional months
/// (Article 12(3)). The extension reason and new deadline are tracked via
/// <see cref="ExtensionReason"/> and <see cref="ExtendedDeadlineAtUtc"/>.
/// </para>
/// </remarks>
public sealed record DSRRequest
{
    /// <summary>
    /// Unique identifier for this DSR request.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Identifier of the data subject who submitted the request.
    /// </summary>
    /// <remarks>
    /// This should be a stable identifier for the data subject (e.g., user ID, customer number).
    /// The controller may need to verify the identity before processing (Article 12(6)).
    /// </remarks>
    public required string SubjectId { get; init; }

    /// <summary>
    /// The specific right being exercised by the data subject.
    /// </summary>
    public required DataSubjectRight RightType { get; init; }

    /// <summary>
    /// The current status of this request in the processing lifecycle.
    /// </summary>
    public required DSRRequestStatus Status { get; init; }

    /// <summary>
    /// Timestamp when the request was received (UTC).
    /// </summary>
    /// <remarks>
    /// The 30-day response deadline is calculated from this date (Article 12(3)).
    /// </remarks>
    public required DateTimeOffset ReceivedAtUtc { get; init; }

    /// <summary>
    /// Deadline by which the request must be completed (UTC).
    /// </summary>
    /// <remarks>
    /// Initially set to <see cref="ReceivedAtUtc"/> plus 30 days.
    /// If extended, the effective deadline becomes <see cref="ExtendedDeadlineAtUtc"/>.
    /// </remarks>
    public required DateTimeOffset DeadlineAtUtc { get; init; }

    /// <summary>
    /// Timestamp when the request was completed or rejected (UTC).
    /// </summary>
    /// <remarks>
    /// <c>null</c> while the request is still pending or in progress.
    /// </remarks>
    public DateTimeOffset? CompletedAtUtc { get; init; }

    /// <summary>
    /// Reason for extending the response deadline.
    /// </summary>
    /// <remarks>
    /// Required when <see cref="Status"/> is <see cref="DSRRequestStatus.Extended"/>.
    /// The controller must inform the data subject of the reasons for the delay
    /// within one month of receipt (Article 12(3)).
    /// </remarks>
    public string? ExtensionReason { get; init; }

    /// <summary>
    /// Extended deadline when additional time is granted (UTC).
    /// </summary>
    /// <remarks>
    /// Maximum extension is 2 additional months beyond the original 30-day deadline (Article 12(3)).
    /// </remarks>
    public DateTimeOffset? ExtendedDeadlineAtUtc { get; init; }

    /// <summary>
    /// Reason for rejecting the request.
    /// </summary>
    /// <remarks>
    /// Required when <see cref="Status"/> is <see cref="DSRRequestStatus.Rejected"/>.
    /// The controller must provide reasons, inform about complaint rights (Article 77),
    /// and judicial remedy rights (Article 79) as required by Article 12(4).
    /// </remarks>
    public string? RejectionReason { get; init; }

    /// <summary>
    /// Additional details or context provided with the request.
    /// </summary>
    /// <remarks>
    /// May include specific data categories, fields, or processing activities the
    /// data subject is referring to in their request.
    /// </remarks>
    public string? RequestDetails { get; init; }

    /// <summary>
    /// Timestamp when the data subject's identity was verified (UTC).
    /// </summary>
    /// <remarks>
    /// <c>null</c> if identity verification has not yet been completed.
    /// The controller may request additional information to confirm identity (Article 12(6)).
    /// </remarks>
    public DateTimeOffset? VerifiedAtUtc { get; init; }

    /// <summary>
    /// Identifier of the user or system that processed the request.
    /// </summary>
    /// <remarks>
    /// Tracks accountability for who handled the request. May be <c>null</c>
    /// for automated processing or if not yet assigned.
    /// </remarks>
    public string? ProcessedByUserId { get; init; }

    /// <summary>
    /// Creates a new DSR request with a calculated 30-day deadline.
    /// </summary>
    /// <param name="id">Unique identifier for the request.</param>
    /// <param name="subjectId">Identifier of the data subject.</param>
    /// <param name="rightType">The right being exercised.</param>
    /// <param name="receivedAtUtc">When the request was received.</param>
    /// <param name="requestDetails">Optional additional request context.</param>
    /// <returns>A new <see cref="DSRRequest"/> with <see cref="DSRRequestStatus.Received"/> status and a 30-day deadline.</returns>
    public static DSRRequest Create(
        string id,
        string subjectId,
        DataSubjectRight rightType,
        DateTimeOffset receivedAtUtc,
        string? requestDetails = null) =>
        new()
        {
            Id = id,
            SubjectId = subjectId,
            RightType = rightType,
            Status = DSRRequestStatus.Received,
            ReceivedAtUtc = receivedAtUtc,
            DeadlineAtUtc = receivedAtUtc.AddDays(30),
            RequestDetails = requestDetails
        };
}
