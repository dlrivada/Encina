using Encina.Compliance.DataSubjectRights.Events;
using Encina.Marten.Projections;

namespace Encina.Compliance.DataSubjectRights.Projections;

/// <summary>
/// Marten inline projection that transforms DSR request aggregate events into <see cref="DSRRequestReadModel"/> state.
/// </summary>
/// <remarks>
/// <para>
/// This projection implements the "Query" side of CQRS for DSR request management. It handles all 7
/// DSR event types, creating or updating the <see cref="DSRRequestReadModel"/> as events are applied.
/// </para>
/// <para>
/// <b>Event Handling</b>:
/// <list type="bullet">
///   <item><description><see cref="DSRRequestSubmitted"/> — Creates a new read model (first event in stream)</description></item>
///   <item><description><see cref="DSRRequestVerified"/> — Updates status to <see cref="DSRRequestStatus.IdentityVerified"/></description></item>
///   <item><description><see cref="DSRRequestProcessing"/> — Updates status to <see cref="DSRRequestStatus.InProgress"/></description></item>
///   <item><description><see cref="DSRRequestCompleted"/> — Updates status to <see cref="DSRRequestStatus.Completed"/></description></item>
///   <item><description><see cref="DSRRequestDenied"/> — Updates status to <see cref="DSRRequestStatus.Rejected"/></description></item>
///   <item><description><see cref="DSRRequestExtended"/> — Updates status to <see cref="DSRRequestStatus.Extended"/> with new deadline</description></item>
///   <item><description><see cref="DSRRequestExpired"/> — Updates status to <see cref="DSRRequestStatus.Expired"/></description></item>
/// </list>
/// </para>
/// <para>
/// <b>Idempotency</b>: The projection is idempotent — applying the same event multiple times
/// produces the same result. This enables safe replay and rebuild of the read model.
/// </para>
/// </remarks>
public sealed class DSRRequestProjection :
    IProjection<DSRRequestReadModel>,
    IProjectionCreator<DSRRequestSubmitted, DSRRequestReadModel>,
    IProjectionHandler<DSRRequestVerified, DSRRequestReadModel>,
    IProjectionHandler<DSRRequestProcessing, DSRRequestReadModel>,
    IProjectionHandler<DSRRequestCompleted, DSRRequestReadModel>,
    IProjectionHandler<DSRRequestDenied, DSRRequestReadModel>,
    IProjectionHandler<DSRRequestExtended, DSRRequestReadModel>,
    IProjectionHandler<DSRRequestExpired, DSRRequestReadModel>
{
    /// <inheritdoc />
    public string ProjectionName => "DSRRequestProjection";

    /// <summary>
    /// Creates a new <see cref="DSRRequestReadModel"/> from a <see cref="DSRRequestSubmitted"/> event.
    /// </summary>
    /// <remarks>
    /// This is the first event in a DSR request aggregate stream. It initializes all fields
    /// including the 30-day deadline calculated per GDPR Article 12(3).
    /// </remarks>
    /// <param name="domainEvent">The DSR request submitted event.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>A new <see cref="DSRRequestReadModel"/> in <see cref="DSRRequestStatus.Received"/> status.</returns>
    public DSRRequestReadModel Create(DSRRequestSubmitted domainEvent, ProjectionContext context)
    {
        return new DSRRequestReadModel
        {
            Id = domainEvent.RequestId,
            SubjectId = domainEvent.SubjectId,
            RightType = domainEvent.RightType,
            Status = DSRRequestStatus.Received,
            ReceivedAtUtc = domainEvent.ReceivedAtUtc,
            DeadlineAtUtc = domainEvent.DeadlineAtUtc,
            RequestDetails = domainEvent.RequestDetails,
            TenantId = domainEvent.TenantId,
            ModuleId = domainEvent.ModuleId,
            LastModifiedAtUtc = domainEvent.ReceivedAtUtc,
            Version = 1
        };
    }

    /// <summary>
    /// Updates the read model when the data subject's identity is verified.
    /// </summary>
    /// <remarks>
    /// Sets status to <see cref="DSRRequestStatus.IdentityVerified"/> and records who performed
    /// the verification. Per GDPR Article 12(6), the controller may request additional information
    /// to confirm the identity of the data subject.
    /// </remarks>
    /// <param name="domainEvent">The identity verified event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model.</returns>
    public DSRRequestReadModel Apply(DSRRequestVerified domainEvent, DSRRequestReadModel current, ProjectionContext context)
    {
        current.Status = DSRRequestStatus.IdentityVerified;
        current.VerifiedAtUtc = domainEvent.VerifiedAtUtc;
        current.LastModifiedAtUtc = domainEvent.VerifiedAtUtc;
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when processing of the DSR request begins.
    /// </summary>
    /// <remarks>
    /// Sets status to <see cref="DSRRequestStatus.InProgress"/> and records the user or system
    /// responsible for processing the request.
    /// </remarks>
    /// <param name="domainEvent">The processing started event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model.</returns>
    public DSRRequestReadModel Apply(DSRRequestProcessing domainEvent, DSRRequestReadModel current, ProjectionContext context)
    {
        current.Status = DSRRequestStatus.InProgress;
        current.ProcessedByUserId = domainEvent.ProcessedByUserId;
        current.LastModifiedAtUtc = domainEvent.StartedAtUtc;
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when the DSR request is fulfilled successfully.
    /// </summary>
    /// <remarks>
    /// Sets status to <see cref="DSRRequestStatus.Completed"/> and records the completion timestamp.
    /// The controller has provided the information or taken the action requested by the data subject.
    /// </remarks>
    /// <param name="domainEvent">The request completed event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model.</returns>
    public DSRRequestReadModel Apply(DSRRequestCompleted domainEvent, DSRRequestReadModel current, ProjectionContext context)
    {
        current.Status = DSRRequestStatus.Completed;
        current.CompletedAtUtc = domainEvent.CompletedAtUtc;
        current.LastModifiedAtUtc = domainEvent.CompletedAtUtc;
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when the DSR request is denied.
    /// </summary>
    /// <remarks>
    /// Sets status to <see cref="DSRRequestStatus.Rejected"/> and records the denial reason.
    /// Per GDPR Article 12(4), the controller must inform the data subject of the reasons,
    /// their right to lodge a complaint (Article 77), and judicial remedy rights (Article 79).
    /// </remarks>
    /// <param name="domainEvent">The request denied event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model.</returns>
    public DSRRequestReadModel Apply(DSRRequestDenied domainEvent, DSRRequestReadModel current, ProjectionContext context)
    {
        current.Status = DSRRequestStatus.Rejected;
        current.RejectionReason = domainEvent.RejectionReason;
        current.CompletedAtUtc = domainEvent.DeniedAtUtc;
        current.LastModifiedAtUtc = domainEvent.DeniedAtUtc;
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when the response deadline is extended.
    /// </summary>
    /// <remarks>
    /// Sets status to <see cref="DSRRequestStatus.Extended"/> and records the new deadline and reason.
    /// Per GDPR Article 12(3), the extension may be up to 2 additional months, and the controller
    /// must inform the data subject of the reasons for the delay within one month of receipt.
    /// </remarks>
    /// <param name="domainEvent">The deadline extended event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model.</returns>
    public DSRRequestReadModel Apply(DSRRequestExtended domainEvent, DSRRequestReadModel current, ProjectionContext context)
    {
        current.Status = DSRRequestStatus.Extended;
        current.ExtensionReason = domainEvent.ExtensionReason;
        current.ExtendedDeadlineAtUtc = domainEvent.ExtendedDeadlineAtUtc;
        current.LastModifiedAtUtc = domainEvent.ExtendedAtUtc;
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when the request deadline expires without completion.
    /// </summary>
    /// <remarks>
    /// Sets status to <see cref="DSRRequestStatus.Expired"/>. An expired request indicates a
    /// potential GDPR compliance violation. The data subject may lodge a complaint with a
    /// supervisory authority (Article 77) or seek a judicial remedy (Article 79).
    /// </remarks>
    /// <param name="domainEvent">The request expired event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model.</returns>
    public DSRRequestReadModel Apply(DSRRequestExpired domainEvent, DSRRequestReadModel current, ProjectionContext context)
    {
        current.Status = DSRRequestStatus.Expired;
        current.LastModifiedAtUtc = domainEvent.ExpiredAtUtc;
        current.Version++;
        return current;
    }
}
