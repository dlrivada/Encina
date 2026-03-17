using Encina.Compliance.Retention.Events;
using Encina.Marten.Projections;

namespace Encina.Compliance.Retention.ReadModels;

/// <summary>
/// Marten inline projection that transforms retention policy aggregate events into <see cref="RetentionPolicyReadModel"/> state.
/// </summary>
/// <remarks>
/// <para>
/// This projection implements the "Query" side of CQRS for retention policy management.
/// It handles all 3 retention policy event types, creating or updating the
/// <see cref="RetentionPolicyReadModel"/> as events are applied.
/// </para>
/// <para>
/// <b>Event Handling</b>:
/// <list type="bullet">
///   <item><description><see cref="RetentionPolicyCreated"/> — Creates a new read model in active status (first event in stream)</description></item>
///   <item><description><see cref="RetentionPolicyUpdated"/> — Updates retention period, auto-delete, reason, and legal basis</description></item>
///   <item><description><see cref="RetentionPolicyDeactivated"/> — Records deactivation reason; marks policy as inactive</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Idempotency</b>: The projection is idempotent — applying the same event multiple times
/// produces the same result. This enables safe replay and rebuild of the read model.
/// </para>
/// </remarks>
public sealed class RetentionPolicyProjection :
    IProjection<RetentionPolicyReadModel>,
    IProjectionCreator<RetentionPolicyCreated, RetentionPolicyReadModel>,
    IProjectionHandler<RetentionPolicyUpdated, RetentionPolicyReadModel>,
    IProjectionHandler<RetentionPolicyDeactivated, RetentionPolicyReadModel>
{
    /// <inheritdoc />
    public string ProjectionName => "RetentionPolicyProjection";

    /// <summary>
    /// Creates a new <see cref="RetentionPolicyReadModel"/> from a <see cref="RetentionPolicyCreated"/> event.
    /// </summary>
    /// <remarks>
    /// This is the first event in a retention policy aggregate stream. It initializes all fields
    /// including the retention period, auto-delete setting, and optional legal basis per
    /// GDPR Article 5(1)(e) storage limitation.
    /// </remarks>
    /// <param name="domainEvent">The retention policy created event.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>A new <see cref="RetentionPolicyReadModel"/> in active status.</returns>
    public RetentionPolicyReadModel Create(RetentionPolicyCreated domainEvent, ProjectionContext context)
    {
        return new RetentionPolicyReadModel
        {
            Id = domainEvent.PolicyId,
            DataCategory = domainEvent.DataCategory,
            RetentionPeriod = domainEvent.RetentionPeriod,
            AutoDelete = domainEvent.AutoDelete,
            PolicyType = domainEvent.PolicyType,
            Reason = domainEvent.Reason,
            LegalBasis = domainEvent.LegalBasis,
            IsActive = true,
            TenantId = domainEvent.TenantId,
            ModuleId = domainEvent.ModuleId,
            CreatedAtUtc = domainEvent.OccurredAtUtc,
            LastModifiedAtUtc = domainEvent.OccurredAtUtc,
            Version = 1
        };
    }

    /// <summary>
    /// Updates the read model when a retention policy is updated with new parameters.
    /// </summary>
    /// <remarks>
    /// Per GDPR Article 5(2) accountability, this event provides an immutable record of all
    /// policy changes, enabling organizations to demonstrate that retention periods were
    /// reviewed and adjusted as necessary.
    /// </remarks>
    /// <param name="domainEvent">The retention policy updated event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model.</returns>
    public RetentionPolicyReadModel Apply(RetentionPolicyUpdated domainEvent, RetentionPolicyReadModel current, ProjectionContext context)
    {
        current.RetentionPeriod = domainEvent.RetentionPeriod;
        current.AutoDelete = domainEvent.AutoDelete;
        current.Reason = domainEvent.Reason;
        current.LegalBasis = domainEvent.LegalBasis;
        current.LastModifiedAtUtc = domainEvent.OccurredAtUtc;
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when a retention policy is deactivated.
    /// </summary>
    /// <remarks>
    /// Deactivation prevents new retention records from being tracked under this policy.
    /// Existing records continue their lifecycle unaffected. Deactivation is preferred over
    /// deletion to maintain a complete audit trail per GDPR Article 5(2) accountability.
    /// </remarks>
    /// <param name="domainEvent">The retention policy deactivated event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model with <see cref="RetentionPolicyReadModel.IsActive"/> set to <see langword="false"/>.</returns>
    public RetentionPolicyReadModel Apply(RetentionPolicyDeactivated domainEvent, RetentionPolicyReadModel current, ProjectionContext context)
    {
        current.IsActive = false;
        current.DeactivationReason = domainEvent.Reason;
        current.LastModifiedAtUtc = domainEvent.OccurredAtUtc;
        current.Version++;
        return current;
    }
}
