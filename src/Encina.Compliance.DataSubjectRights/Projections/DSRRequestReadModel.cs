using Encina.Marten.Projections;

namespace Encina.Compliance.DataSubjectRights.Projections;

/// <summary>
/// Query-optimized projected view of a DSR request, built from
/// <see cref="Aggregates.DSRRequestAggregate"/> events.
/// </summary>
/// <remarks>
/// <para>
/// This read model is materialized from the DSR request aggregate event stream by
/// <see cref="DSRRequestProjection"/>. It provides an efficient query view without
/// replaying events, while the underlying event stream maintains the full audit trail
/// for GDPR Article 5(2) accountability requirements.
/// </para>
/// <para>
/// Properties have mutable setters because projections update them incrementally
/// as events are applied. The <see cref="LastModifiedAtUtc"/> timestamp is updated
/// on every event, enabling efficient change detection.
/// </para>
/// <para>
/// Used by <c>IDSRService</c> query methods to return DSR request state to consumers.
/// Replaces the old entity-based <c>DSRRequest</c> for query purposes in the event-sourced model.
/// </para>
/// </remarks>
public sealed class DSRRequestReadModel : IReadModel
{
    /// <summary>
    /// Unique identifier for this DSR request (matches the aggregate stream ID).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Identifier of the data subject who submitted the request.
    /// </summary>
    /// <remarks>
    /// Stable identifier for the data subject (e.g., user ID, customer number).
    /// The controller may need to verify identity before processing (Article 12(6)).
    /// </remarks>
    public string SubjectId { get; set; } = string.Empty;

    /// <summary>
    /// The specific GDPR right being exercised (Articles 15-22).
    /// </summary>
    public DataSubjectRight RightType { get; set; }

    /// <summary>
    /// Current lifecycle status of this DSR request.
    /// </summary>
    public DSRRequestStatus Status { get; set; }

    /// <summary>
    /// Timestamp when the request was received (UTC).
    /// </summary>
    /// <remarks>
    /// The 30-day response deadline is calculated from this date (Article 12(3)).
    /// </remarks>
    public DateTimeOffset ReceivedAtUtc { get; set; }

    /// <summary>
    /// Initial deadline by which the request must be completed (UTC).
    /// </summary>
    /// <remarks>
    /// Calculated as <see cref="ReceivedAtUtc"/> plus 30 days per Article 12(3).
    /// If extended, the effective deadline becomes <see cref="ExtendedDeadlineAtUtc"/>.
    /// </remarks>
    public DateTimeOffset DeadlineAtUtc { get; set; }

    /// <summary>
    /// Timestamp when the request was completed or denied (UTC).
    /// </summary>
    /// <remarks>
    /// <c>null</c> while the request is still pending or in progress.
    /// </remarks>
    public DateTimeOffset? CompletedAtUtc { get; set; }

    /// <summary>
    /// Timestamp when the data subject's identity was verified (UTC).
    /// </summary>
    /// <remarks>
    /// <c>null</c> if identity verification has not yet been completed.
    /// The controller may request additional information to confirm identity (Article 12(6)).
    /// </remarks>
    public DateTimeOffset? VerifiedAtUtc { get; set; }

    /// <summary>
    /// Reason for extending the response deadline.
    /// </summary>
    /// <remarks>
    /// Required when <see cref="Status"/> is <see cref="DSRRequestStatus.Extended"/>.
    /// The controller must inform the data subject of the reasons for the delay
    /// within one month of receipt (Article 12(3)).
    /// </remarks>
    public string? ExtensionReason { get; set; }

    /// <summary>
    /// Extended deadline when additional time is granted (UTC).
    /// </summary>
    /// <remarks>
    /// Maximum extension is 2 additional months beyond the original 30-day deadline (Article 12(3)).
    /// <c>null</c> if no extension has been granted.
    /// </remarks>
    public DateTimeOffset? ExtendedDeadlineAtUtc { get; set; }

    /// <summary>
    /// Reason for denying the request.
    /// </summary>
    /// <remarks>
    /// Required when <see cref="Status"/> is <see cref="DSRRequestStatus.Rejected"/>.
    /// The controller must provide reasons, inform about complaint rights (Article 77),
    /// and judicial remedy rights (Article 79) as required by Article 12(4).
    /// </remarks>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Additional details or context provided by the data subject with the request.
    /// </summary>
    public string? RequestDetails { get; set; }

    /// <summary>
    /// Identifier of the user or system processing the request.
    /// </summary>
    /// <remarks>
    /// Tracks accountability for who handled the request. May be <c>null</c>
    /// for automated processing or if not yet assigned.
    /// </remarks>
    public string? ProcessedByUserId { get; set; }

    /// <summary>
    /// Tenant identifier for multi-tenancy scoping.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Module identifier for modular monolith scoping.
    /// </summary>
    public string? ModuleId { get; set; }

    /// <summary>
    /// Timestamp of the last modification to this DSR request (UTC).
    /// </summary>
    /// <remarks>
    /// Updated on every event applied to the DSR request aggregate.
    /// Enables efficient change detection and cache invalidation.
    /// </remarks>
    public DateTimeOffset LastModifiedAtUtc { get; set; }

    /// <summary>
    /// Event stream version for optimistic concurrency.
    /// </summary>
    /// <remarks>
    /// Incremented on every event. Matches the aggregate's <see cref="DomainModeling.AggregateBase.Version"/>.
    /// </remarks>
    public int Version { get; set; }

    /// <summary>
    /// Determines whether this request is overdue based on the current time.
    /// </summary>
    /// <remarks>
    /// A request is overdue when it is still active (not completed, rejected, or expired)
    /// and the effective deadline has passed. Overdue requests indicate a potential
    /// GDPR compliance violation.
    /// </remarks>
    /// <param name="nowUtc">The current UTC time for deadline evaluation.</param>
    /// <returns><c>true</c> if the request is still active and past its effective deadline; otherwise <c>false</c>.</returns>
    public bool IsOverdue(DateTimeOffset nowUtc)
    {
        if (Status is DSRRequestStatus.Completed or DSRRequestStatus.Rejected or DSRRequestStatus.Expired)
        {
            return false;
        }

        var effectiveDeadline = ExtendedDeadlineAtUtc ?? DeadlineAtUtc;
        return nowUtc > effectiveDeadline;
    }

    /// <summary>
    /// Determines whether this request represents an active processing restriction.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A restriction is considered active when the request exercises the
    /// <see cref="DataSubjectRight.Restriction"/> right and is still being processed
    /// (not yet completed, rejected, or expired). This is used by
    /// <c>ProcessingRestrictionPipelineBehavior</c> to enforce Article 18(2).
    /// </para>
    /// <para>
    /// Per GDPR Article 18(2), when processing is restricted, personal data may only be
    /// stored — not processed — unless the data subject consents or for specific purposes.
    /// </para>
    /// </remarks>
    public bool HasActiveRestriction =>
        RightType == DataSubjectRight.Restriction &&
        Status is DSRRequestStatus.Received or DSRRequestStatus.IdentityVerified or DSRRequestStatus.InProgress;
}
