namespace Encina.Compliance.DataSubjectRights.Events;

// Event-sourced events implement INotification so they are automatically published
// by Encina.Marten's EventPublishingPipelineBehavior after successful command execution.
// This allows handlers to react to DSR lifecycle changes without a separate notification layer.

/// <summary>
/// Raised when a data subject submits a request to exercise one of their GDPR rights (Articles 15-22).
/// </summary>
/// <remarks>
/// <para>
/// Initiates the DSR request lifecycle. The aggregate transitions to <see cref="DSRRequestStatus.Received"/> status.
/// The 30-day response deadline is calculated from <paramref name="ReceivedAtUtc"/> as required by
/// GDPR Article 12(3).
/// </para>
/// <para>
/// The controller must respond without undue delay and in any event within one month of receipt.
/// The <paramref name="DeadlineAtUtc"/> field captures this calculated deadline for compliance monitoring.
/// </para>
/// </remarks>
/// <param name="RequestId">Unique identifier for this DSR request.</param>
/// <param name="SubjectId">Stable identifier of the data subject who submitted the request (e.g., user ID, customer number).</param>
/// <param name="RightType">The specific GDPR right being exercised (Articles 15-22).</param>
/// <param name="ReceivedAtUtc">Timestamp when the request was received (UTC). The 30-day clock starts here.</param>
/// <param name="DeadlineAtUtc">Calculated response deadline (ReceivedAtUtc + 30 days) per Article 12(3).</param>
/// <param name="RequestDetails">Additional context or details provided by the data subject, or <c>null</c>.</param>
/// <param name="TenantId">Tenant identifier for multi-tenancy scoping.</param>
/// <param name="ModuleId">Module identifier for modular monolith scoping.</param>
public sealed record DSRRequestSubmitted(
    Guid RequestId,
    string SubjectId,
    DataSubjectRight RightType,
    DateTimeOffset ReceivedAtUtc,
    DateTimeOffset DeadlineAtUtc,
    string? RequestDetails,
    string? TenantId,
    string? ModuleId) : INotification;

/// <summary>
/// Raised when the identity of the data subject has been verified for a DSR request.
/// </summary>
/// <remarks>
/// <para>
/// Transitions the request from <see cref="DSRRequestStatus.Received"/> to
/// <see cref="DSRRequestStatus.IdentityVerified"/>. Per GDPR Article 12(6), the controller
/// may request additional information necessary to confirm the identity of the data subject
/// before processing the request.
/// </para>
/// </remarks>
/// <param name="RequestId">The DSR request identifier.</param>
/// <param name="VerifiedBy">Identifier of the person or system that verified the identity.</param>
/// <param name="VerifiedAtUtc">Timestamp when identity verification was completed (UTC).</param>
/// <param name="TenantId">Tenant identifier for multi-tenancy scoping.</param>
/// <param name="ModuleId">Module identifier for modular monolith scoping.</param>
public sealed record DSRRequestVerified(
    Guid RequestId,
    string VerifiedBy,
    DateTimeOffset VerifiedAtUtc,
    string? TenantId,
    string? ModuleId) : INotification;

/// <summary>
/// Raised when processing of a DSR request begins.
/// </summary>
/// <remarks>
/// <para>
/// Transitions the request from <see cref="DSRRequestStatus.IdentityVerified"/> to
/// <see cref="DSRRequestStatus.InProgress"/>. This indicates the controller has started
/// executing the requested right (access, erasure, portability, etc.).
/// </para>
/// </remarks>
/// <param name="RequestId">The DSR request identifier.</param>
/// <param name="ProcessedByUserId">Identifier of the user or system processing the request, or <c>null</c> for automated processing.</param>
/// <param name="StartedAtUtc">Timestamp when processing started (UTC).</param>
/// <param name="TenantId">Tenant identifier for multi-tenancy scoping.</param>
/// <param name="ModuleId">Module identifier for modular monolith scoping.</param>
public sealed record DSRRequestProcessing(
    Guid RequestId,
    string? ProcessedByUserId,
    DateTimeOffset StartedAtUtc,
    string? TenantId,
    string? ModuleId) : INotification;

/// <summary>
/// Raised when a DSR request has been fulfilled successfully.
/// </summary>
/// <remarks>
/// <para>
/// Transitions the request from <see cref="DSRRequestStatus.InProgress"/> to
/// <see cref="DSRRequestStatus.Completed"/>. The controller has provided the information
/// or taken the action requested by the data subject within the applicable deadline.
/// </para>
/// </remarks>
/// <param name="RequestId">The DSR request identifier.</param>
/// <param name="CompletedAtUtc">Timestamp when the request was completed (UTC).</param>
/// <param name="TenantId">Tenant identifier for multi-tenancy scoping.</param>
/// <param name="ModuleId">Module identifier for modular monolith scoping.</param>
public sealed record DSRRequestCompleted(
    Guid RequestId,
    DateTimeOffset CompletedAtUtc,
    string? TenantId,
    string? ModuleId) : INotification;

/// <summary>
/// Raised when a DSR request is denied with a stated reason.
/// </summary>
/// <remarks>
/// <para>
/// Transitions the request to <see cref="DSRRequestStatus.Rejected"/>. Per GDPR Article 12(4),
/// the controller must inform the data subject of the reasons for not taking action,
/// the possibility of lodging a complaint with a supervisory authority (Article 77),
/// and the right to seek a judicial remedy (Article 79).
/// </para>
/// </remarks>
/// <param name="RequestId">The DSR request identifier.</param>
/// <param name="RejectionReason">Explanation of why the request was denied (required for Article 12(4) compliance).</param>
/// <param name="DeniedAtUtc">Timestamp when the request was denied (UTC).</param>
/// <param name="TenantId">Tenant identifier for multi-tenancy scoping.</param>
/// <param name="ModuleId">Module identifier for modular monolith scoping.</param>
public sealed record DSRRequestDenied(
    Guid RequestId,
    string RejectionReason,
    DateTimeOffset DeniedAtUtc,
    string? TenantId,
    string? ModuleId) : INotification;

/// <summary>
/// Raised when the deadline for a DSR request is extended by up to 2 additional months.
/// </summary>
/// <remarks>
/// <para>
/// Transitions the request to <see cref="DSRRequestStatus.Extended"/>. Per GDPR Article 12(3),
/// the controller may extend the response period by up to two further months, taking into account
/// the complexity and number of requests. The controller must inform the data subject of the
/// extension and the reasons for the delay within one month of receipt.
/// </para>
/// </remarks>
/// <param name="RequestId">The DSR request identifier.</param>
/// <param name="ExtensionReason">Explanation of why additional time is needed (required for Article 12(3) compliance).</param>
/// <param name="ExtendedDeadlineAtUtc">The new extended deadline (original deadline + up to 2 months).</param>
/// <param name="ExtendedAtUtc">Timestamp when the extension was granted (UTC).</param>
/// <param name="TenantId">Tenant identifier for multi-tenancy scoping.</param>
/// <param name="ModuleId">Module identifier for modular monolith scoping.</param>
public sealed record DSRRequestExtended(
    Guid RequestId,
    string ExtensionReason,
    DateTimeOffset ExtendedDeadlineAtUtc,
    DateTimeOffset ExtendedAtUtc,
    string? TenantId,
    string? ModuleId) : INotification;

/// <summary>
/// Raised when a DSR request deadline passes without the request being completed.
/// </summary>
/// <remarks>
/// <para>
/// Transitions the request to <see cref="DSRRequestStatus.Expired"/>. An expired request indicates
/// a potential compliance violation. The data subject may lodge a complaint with a supervisory
/// authority (Article 77) or seek a judicial remedy (Article 79).
/// </para>
/// <para>
/// This event is typically published by background processors or deadline monitoring services
/// that detect overdue requests.
/// </para>
/// </remarks>
/// <param name="RequestId">The DSR request identifier.</param>
/// <param name="ExpiredAtUtc">Timestamp when the expiration was detected (UTC).</param>
/// <param name="TenantId">Tenant identifier for multi-tenancy scoping.</param>
/// <param name="ModuleId">Module identifier for modular monolith scoping.</param>
public sealed record DSRRequestExpired(
    Guid RequestId,
    DateTimeOffset ExpiredAtUtc,
    string? TenantId,
    string? ModuleId) : INotification;
