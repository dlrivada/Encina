using Encina.Compliance.DataSubjectRights.Events;
using Encina.DomainModeling;

namespace Encina.Compliance.DataSubjectRights.Aggregates;

/// <summary>
/// Event-sourced aggregate representing the full lifecycle of a Data Subject Rights request
/// under GDPR Articles 15-22.
/// </summary>
/// <remarks>
/// <para>
/// Each aggregate instance represents a single DSR request identified by a unique <see cref="AggregateBase.Id"/>.
/// The request progresses through a defined lifecycle:
/// <see cref="DSRRequestStatus.Received"/> →
/// <see cref="DSRRequestStatus.IdentityVerified"/> →
/// <see cref="DSRRequestStatus.InProgress"/> →
/// <see cref="DSRRequestStatus.Completed"/> | <see cref="DSRRequestStatus.Rejected"/>.
/// </para>
/// <para>
/// For complex requests, the deadline may be extended by up to 2 additional months
/// (Article 12(3)), transitioning to <see cref="DSRRequestStatus.Extended"/>.
/// If the deadline passes without completion, the request transitions to
/// <see cref="DSRRequestStatus.Expired"/>, indicating a potential compliance violation.
/// </para>
/// <para>
/// All state changes are captured as immutable events, providing a complete audit trail
/// for GDPR Article 5(2) accountability requirements. Events implement <see cref="INotification"/>
/// and are automatically published by <c>EventPublishingPipelineBehavior</c> after successful
/// Marten commit, enabling downstream handlers to react to DSR lifecycle changes.
/// </para>
/// </remarks>
public sealed class DSRRequestAggregate : AggregateBase
{
    /// <summary>
    /// Identifier of the data subject who submitted the request.
    /// </summary>
    /// <remarks>
    /// Stable identifier for the data subject (e.g., user ID, customer number).
    /// The controller may need to verify identity before processing (Article 12(6)).
    /// </remarks>
    public string SubjectId { get; private set; } = string.Empty;

    /// <summary>
    /// The specific GDPR right being exercised (Articles 15-22).
    /// </summary>
    public DataSubjectRight RightType { get; private set; }

    /// <summary>
    /// Current lifecycle status of this DSR request.
    /// </summary>
    public DSRRequestStatus Status { get; private set; }

    /// <summary>
    /// Timestamp when the request was received (UTC).
    /// </summary>
    /// <remarks>
    /// The 30-day response deadline is calculated from this date (Article 12(3)).
    /// </remarks>
    public DateTimeOffset ReceivedAtUtc { get; private set; }

    /// <summary>
    /// Initial deadline by which the request must be completed (UTC).
    /// </summary>
    /// <remarks>
    /// Calculated as <see cref="ReceivedAtUtc"/> plus 30 days per Article 12(3).
    /// If extended, the effective deadline becomes <see cref="ExtendedDeadlineAtUtc"/>.
    /// </remarks>
    public DateTimeOffset DeadlineAtUtc { get; private set; }

    /// <summary>
    /// Timestamp when the request was completed or denied (UTC).
    /// </summary>
    /// <remarks>
    /// <c>null</c> while the request is still pending or in progress.
    /// </remarks>
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    /// <summary>
    /// Timestamp when the data subject's identity was verified (UTC).
    /// </summary>
    /// <remarks>
    /// <c>null</c> if identity verification has not yet been completed.
    /// The controller may request additional information to confirm identity (Article 12(6)).
    /// </remarks>
    public DateTimeOffset? VerifiedAtUtc { get; private set; }

    /// <summary>
    /// Reason for extending the response deadline.
    /// </summary>
    /// <remarks>
    /// Required when <see cref="Status"/> is <see cref="DSRRequestStatus.Extended"/>.
    /// The controller must inform the data subject of the reasons for the delay
    /// within one month of receipt (Article 12(3)).
    /// </remarks>
    public string? ExtensionReason { get; private set; }

    /// <summary>
    /// Extended deadline when additional time is granted (UTC).
    /// </summary>
    /// <remarks>
    /// Maximum extension is 2 additional months beyond the original 30-day deadline (Article 12(3)).
    /// <c>null</c> if no extension has been granted.
    /// </remarks>
    public DateTimeOffset? ExtendedDeadlineAtUtc { get; private set; }

    /// <summary>
    /// Standardised reason code for denying the request.
    /// </summary>
    /// <remarks>
    /// Set when <see cref="Status"/> is <see cref="DSRRequestStatus.Rejected"/>; <c>null</c> otherwise.
    /// </remarks>
    public DSRRejectionReason? RejectionReason { get; private set; }

    /// <summary>
    /// Explanation of the denial given to the data subject.
    /// </summary>
    /// <remarks>
    /// Set when <see cref="Status"/> is <see cref="DSRRequestStatus.Rejected"/>.
    /// The controller must provide reasons, inform about complaint rights (Article 77),
    /// and judicial remedy rights (Article 79) as required by Article 12(4), and bears the
    /// burden of demonstrating the ground (Article 12(5)).
    /// </remarks>
    public string? RejectionExplanation { get; private set; }

    /// <summary>
    /// Additional details or context provided by the data subject with the request.
    /// </summary>
    /// <remarks>
    /// May include specific data categories, fields, or processing activities the
    /// data subject is referring to in their request.
    /// </remarks>
    public string? RequestDetails { get; private set; }

    /// <summary>
    /// Identifier of the user or system processing the request.
    /// </summary>
    /// <remarks>
    /// Tracks accountability for who handled the request. May be <c>null</c>
    /// for automated processing or if not yet assigned.
    /// </remarks>
    public string? ProcessedByUserId { get; private set; }

    /// <summary>
    /// Tenant identifier for multi-tenancy scoping.
    /// </summary>
    public string? TenantId { get; private set; }

    /// <summary>
    /// Module identifier for modular monolith scoping.
    /// </summary>
    public string? ModuleId { get; private set; }

    /// <summary>
    /// Submits a new Data Subject Rights request, starting the GDPR Article 12(3) response clock.
    /// </summary>
    /// <remarks>
    /// Creates a new aggregate in <see cref="DSRRequestStatus.Received"/> status with a calculated
    /// 30-day deadline. The deadline is computed as <paramref name="receivedAtUtc"/> + 30 days
    /// per GDPR Article 12(3).
    /// </remarks>
    /// <param name="id">Unique identifier for the new DSR request.</param>
    /// <param name="subjectId">Stable identifier of the data subject submitting the request.</param>
    /// <param name="rightType">The GDPR right being exercised (Articles 15-22).</param>
    /// <param name="receivedAtUtc">Timestamp when the request was received (UTC).</param>
    /// <param name="requestDetails">Additional context provided by the data subject, or <c>null</c>.</param>
    /// <param name="tenantId">Optional tenant identifier for multi-tenancy scoping.</param>
    /// <param name="moduleId">Optional module identifier for modular monolith scoping.</param>
    /// <returns>A new <see cref="DSRRequestAggregate"/> in <see cref="DSRRequestStatus.Received"/> status.</returns>
    public static DSRRequestAggregate Submit(
        Guid id,
        string subjectId,
        DataSubjectRight rightType,
        DateTimeOffset receivedAtUtc,
        string? requestDetails = null,
        string? tenantId = null,
        string? moduleId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);

        var deadlineAtUtc = receivedAtUtc.AddDays(30);

        var aggregate = new DSRRequestAggregate();
        aggregate.RaiseEvent(new DSRRequestSubmitted(
            id, subjectId, rightType, receivedAtUtc, deadlineAtUtc, requestDetails, tenantId, moduleId));
        return aggregate;
    }

    /// <summary>
    /// Records that the data subject's identity has been verified.
    /// </summary>
    /// <remarks>
    /// Per GDPR Article 12(6), the controller may request additional information necessary
    /// to confirm the identity of the data subject before processing the request.
    /// Transitions from <see cref="DSRRequestStatus.Received"/> to <see cref="DSRRequestStatus.IdentityVerified"/>.
    /// </remarks>
    /// <param name="verifiedBy">Identifier of the person or system that verified the identity.</param>
    /// <param name="verifiedAtUtc">Timestamp when identity verification was completed (UTC).</param>
    /// <exception cref="InvalidOperationException">Thrown when the request is not in <see cref="DSRRequestStatus.Received"/> status.</exception>
    public void Verify(string verifiedBy, DateTimeOffset verifiedAtUtc)
    {
        if (Status != DSRRequestStatus.Received)
        {
            throw new InvalidOperationException(
                $"Cannot verify identity when the request is in '{Status}' status. Verification is only allowed from Received status.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(verifiedBy);

        RaiseEvent(new DSRRequestVerified(Id, verifiedBy, verifiedAtUtc, TenantId, ModuleId));
    }

    /// <summary>
    /// Starts processing the DSR request.
    /// </summary>
    /// <remarks>
    /// Transitions from <see cref="DSRRequestStatus.IdentityVerified"/> to
    /// <see cref="DSRRequestStatus.InProgress"/>. This indicates the controller has begun
    /// executing the requested right.
    /// </remarks>
    /// <param name="processedByUserId">Identifier of the user or system processing the request, or <c>null</c> for automated processing.</param>
    /// <param name="startedAtUtc">Timestamp when processing started (UTC).</param>
    /// <exception cref="InvalidOperationException">Thrown when the request is not in <see cref="DSRRequestStatus.IdentityVerified"/> or <see cref="DSRRequestStatus.Extended"/> status.</exception>
    public void StartProcessing(string? processedByUserId, DateTimeOffset startedAtUtc)
    {
        if (Status is not (DSRRequestStatus.IdentityVerified or DSRRequestStatus.Extended))
        {
            throw new InvalidOperationException(
                $"Cannot start processing when the request is in '{Status}' status. Processing can only start from IdentityVerified or Extended status.");
        }

        RaiseEvent(new DSRRequestProcessing(Id, processedByUserId, startedAtUtc, TenantId, ModuleId));
    }

    /// <summary>
    /// Marks the DSR request as completed successfully.
    /// </summary>
    /// <remarks>
    /// Transitions from <see cref="DSRRequestStatus.InProgress"/> to
    /// <see cref="DSRRequestStatus.Completed"/>. The controller has fulfilled the data subject's request.
    /// </remarks>
    /// <param name="completedAtUtc">Timestamp when the request was completed (UTC).</param>
    /// <exception cref="InvalidOperationException">Thrown when the request is not in <see cref="DSRRequestStatus.InProgress"/> status.</exception>
    public void Complete(DateTimeOffset completedAtUtc)
    {
        if (Status != DSRRequestStatus.InProgress)
        {
            throw new InvalidOperationException(
                $"Cannot complete a request that is in '{Status}' status. Completion is only allowed from InProgress status.");
        }

        RaiseEvent(new DSRRequestCompleted(Id, completedAtUtc, TenantId, ModuleId));
    }

    /// <summary>
    /// Denies the DSR request with a standardised reason code and an explanation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Per GDPR Article 12(4), the controller must inform the data subject of the reasons for
    /// not taking action, the possibility of lodging a complaint with a supervisory authority
    /// (Article 77), and the right to seek a judicial remedy (Article 79).
    /// </para>
    /// <para>
    /// A request may be denied from any active status (Received, IdentityVerified, InProgress, or Extended).
    /// </para>
    /// </remarks>
    /// <param name="rejectionReason">Standardised reason code of the refusal.</param>
    /// <param name="rejectionExplanation">Explanation given to the data subject (required for Article 12(4) and 12(5)).</param>
    /// <param name="deniedAtUtc">Timestamp when the request was denied (UTC).</param>
    /// <exception cref="InvalidOperationException">Thrown when the request is already in a terminal status (Completed, Rejected, or Expired).</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="rejectionReason"/> is not a defined <see cref="DSRRejectionReason"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="rejectionExplanation"/> is null, empty or whitespace.</exception>
    public void Deny(DSRRejectionReason rejectionReason, string rejectionExplanation, DateTimeOffset deniedAtUtc)
    {
        if (Status is DSRRequestStatus.Completed or DSRRequestStatus.Rejected or DSRRequestStatus.Expired)
        {
            throw new InvalidOperationException(
                $"Cannot deny a request that is in '{Status}' status. Denial is not allowed from terminal statuses.");
        }

        if (!Enum.IsDefined(rejectionReason))
        {
            throw new ArgumentOutOfRangeException(
                nameof(rejectionReason), rejectionReason, "The rejection reason is not a defined DSRRejectionReason value.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(rejectionExplanation);

        RaiseEvent(new DSRRequestDenied(Id, rejectionReason, rejectionExplanation, deniedAtUtc, TenantId, ModuleId));
    }

    /// <summary>
    /// Extends the response deadline by up to 2 additional months.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Per GDPR Article 12(3), the controller may extend the response period by up to two
    /// further months, taking into account the complexity and number of requests. The extended
    /// deadline is calculated as the original <see cref="DeadlineAtUtc"/> plus 2 months.
    /// </para>
    /// <para>
    /// The controller must inform the data subject of the extension and the reasons for
    /// the delay within one month of receipt.
    /// </para>
    /// </remarks>
    /// <param name="extensionReason">Explanation of why additional time is needed (required for Article 12(3)).</param>
    /// <param name="extendedAtUtc">Timestamp when the extension was granted (UTC).</param>
    /// <exception cref="InvalidOperationException">Thrown when the request is not in an extendable status.</exception>
    public void Extend(string extensionReason, DateTimeOffset extendedAtUtc)
    {
        if (Status is DSRRequestStatus.Completed or DSRRequestStatus.Rejected or DSRRequestStatus.Expired or DSRRequestStatus.Extended)
        {
            throw new InvalidOperationException(
                $"Cannot extend a request that is in '{Status}' status. Extension is only allowed from Received, IdentityVerified, or InProgress status.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(extensionReason);

        var extendedDeadlineAtUtc = DeadlineAtUtc.AddMonths(2);

        RaiseEvent(new DSRRequestExtended(Id, extensionReason, extendedDeadlineAtUtc, extendedAtUtc, TenantId, ModuleId));
    }

    /// <summary>
    /// Marks the DSR request as expired because the deadline passed without completion.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An expired request indicates a potential GDPR compliance violation. The data subject
    /// may lodge a complaint with a supervisory authority (Article 77) or seek a judicial
    /// remedy (Article 79).
    /// </para>
    /// <para>
    /// This method is typically called by background processors or deadline monitoring services.
    /// </para>
    /// </remarks>
    /// <param name="expiredAtUtc">Timestamp when the expiration was detected (UTC).</param>
    /// <exception cref="InvalidOperationException">Thrown when the request is already in a terminal status (Completed, Rejected, or Expired).</exception>
    public void Expire(DateTimeOffset expiredAtUtc)
    {
        if (Status is DSRRequestStatus.Completed or DSRRequestStatus.Rejected or DSRRequestStatus.Expired)
        {
            throw new InvalidOperationException(
                $"Cannot expire a request that is in '{Status}' status. Expiration is not allowed from terminal statuses.");
        }

        RaiseEvent(new DSRRequestExpired(Id, expiredAtUtc, TenantId, ModuleId));
    }

    /// <summary>
    /// Returns the effective deadline for this request, considering any extension.
    /// </summary>
    /// <returns>The <see cref="ExtendedDeadlineAtUtc"/> if the deadline was extended; otherwise <see cref="DeadlineAtUtc"/>.</returns>
    public DateTimeOffset GetEffectiveDeadline() =>
        ExtendedDeadlineAtUtc ?? DeadlineAtUtc;

    /// <summary>
    /// Determines whether this request is overdue based on the current time.
    /// </summary>
    /// <param name="nowUtc">The current UTC time for deadline evaluation.</param>
    /// <returns><c>true</c> if the request is still active and past its effective deadline; otherwise <c>false</c>.</returns>
    public bool IsOverdue(DateTimeOffset nowUtc)
    {
        if (Status is DSRRequestStatus.Completed or DSRRequestStatus.Rejected or DSRRequestStatus.Expired)
        {
            return false;
        }

        return nowUtc > GetEffectiveDeadline();
    }

    /// <inheritdoc />
    protected override void Apply(object domainEvent)
    {
        if (EventHandlers.TryGetValue(domainEvent.GetType(), out var handler))
        {
            handler(this, domainEvent);
        }
    }

    // One state change per event type, looked up by the exact (sealed) event type.
    private static readonly Dictionary<Type, Action<DSRRequestAggregate, object>> EventHandlers = new()
    {
        [typeof(DSRRequestSubmitted)] = (a, e) => a.When((DSRRequestSubmitted)e),
        [typeof(DSRRequestVerified)] = (a, e) => a.When((DSRRequestVerified)e),
        [typeof(DSRRequestProcessing)] = (a, e) => a.When((DSRRequestProcessing)e),
        [typeof(DSRRequestCompleted)] = (a, e) => a.When((DSRRequestCompleted)e),
        [typeof(DSRRequestDenied)] = (a, e) => a.When((DSRRequestDenied)e),
        [typeof(DSRRequestExtended)] = (a, e) => a.When((DSRRequestExtended)e),
        [typeof(DSRRequestExpired)] = (a, _) => a.Status = DSRRequestStatus.Expired
    };

    private void When(DSRRequestSubmitted e)
    {
        Id = e.RequestId;
        SubjectId = e.SubjectId;
        RightType = e.RightType;
        Status = DSRRequestStatus.Received;
        ReceivedAtUtc = e.ReceivedAtUtc;
        DeadlineAtUtc = e.DeadlineAtUtc;
        RequestDetails = e.RequestDetails;
        TenantId = e.TenantId;
        ModuleId = e.ModuleId;
    }

    private void When(DSRRequestVerified e)
    {
        Status = DSRRequestStatus.IdentityVerified;
        VerifiedAtUtc = e.VerifiedAtUtc;
    }

    private void When(DSRRequestProcessing e)
    {
        Status = DSRRequestStatus.InProgress;
        ProcessedByUserId = e.ProcessedByUserId;
    }

    private void When(DSRRequestCompleted e)
    {
        Status = DSRRequestStatus.Completed;
        CompletedAtUtc = e.CompletedAtUtc;
    }

    private void When(DSRRequestDenied e)
    {
        Status = DSRRequestStatus.Rejected;
        RejectionReason = e.RejectionReason;
        RejectionExplanation = e.RejectionExplanation;
        CompletedAtUtc = e.DeniedAtUtc;
    }

    private void When(DSRRequestExtended e)
    {
        Status = DSRRequestStatus.Extended;
        ExtensionReason = e.ExtensionReason;
        ExtendedDeadlineAtUtc = e.ExtendedDeadlineAtUtc;
    }
}
