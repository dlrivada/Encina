using Encina.Compliance.DataResidency.Events;
using Encina.Marten.Projections;

namespace Encina.Compliance.DataResidency.ReadModels;

/// <summary>
/// Marten inline projection that transforms residency policy aggregate events into
/// <see cref="ResidencyPolicyReadModel"/> state.
/// </summary>
/// <remarks>
/// <para>
/// This projection implements the "Query" side of CQRS for residency policy management.
/// It handles all 3 residency policy event types, creating or updating the
/// <see cref="ResidencyPolicyReadModel"/> as events are applied.
/// </para>
/// <para>
/// <b>Event Handling</b>:
/// <list type="bullet">
///   <item><description><see cref="ResidencyPolicyCreated"/> — Creates a new read model in active status (first event in stream)</description></item>
///   <item><description><see cref="ResidencyPolicyUpdated"/> — Updates allowed regions, adequacy requirement, and transfer bases</description></item>
///   <item><description><see cref="ResidencyPolicyDeleted"/> — Records deletion reason; marks policy as inactive</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Idempotency</b>: The projection is idempotent — applying the same event multiple times
/// produces the same result. This enables safe replay and rebuild of the read model.
/// </para>
/// </remarks>
public sealed class ResidencyPolicyProjection :
    IProjection<ResidencyPolicyReadModel>,
    IProjectionCreator<ResidencyPolicyCreated, ResidencyPolicyReadModel>,
    IProjectionHandler<ResidencyPolicyUpdated, ResidencyPolicyReadModel>,
    IProjectionHandler<ResidencyPolicyDeleted, ResidencyPolicyReadModel>
{
    /// <inheritdoc />
    public string ProjectionName => "ResidencyPolicyProjection";

    /// <summary>
    /// Creates a new <see cref="ResidencyPolicyReadModel"/> from a <see cref="ResidencyPolicyCreated"/> event.
    /// </summary>
    /// <remarks>
    /// This is the first event in a residency policy aggregate stream. It initializes all fields
    /// including allowed regions, adequacy decision requirement, and transfer legal bases per
    /// GDPR Chapter V (Articles 44–49) international data transfer requirements.
    /// </remarks>
    /// <param name="domainEvent">The residency policy created event.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>A new <see cref="ResidencyPolicyReadModel"/> in active status.</returns>
    public ResidencyPolicyReadModel Create(ResidencyPolicyCreated domainEvent, ProjectionContext context)
    {
        return new ResidencyPolicyReadModel
        {
            Id = domainEvent.PolicyId,
            DataCategory = domainEvent.DataCategory,
            AllowedRegionCodes = domainEvent.AllowedRegionCodes,
            RequireAdequacyDecision = domainEvent.RequireAdequacyDecision,
            AllowedTransferBases = domainEvent.AllowedTransferBases,
            IsActive = true,
            TenantId = domainEvent.TenantId,
            ModuleId = domainEvent.ModuleId,
            CreatedAtUtc = new DateTimeOffset(context.Timestamp, TimeSpan.Zero),
            LastModifiedAtUtc = new DateTimeOffset(context.Timestamp, TimeSpan.Zero),
            Version = 1
        };
    }

    /// <summary>
    /// Updates the read model when a residency policy is updated with new parameters.
    /// </summary>
    /// <remarks>
    /// Per GDPR Article 5(2) accountability, this event provides an immutable record of all
    /// policy changes, enabling organizations to demonstrate that data residency rules were
    /// reviewed and adjusted as necessary in response to regulatory changes (e.g., new adequacy
    /// decisions, Schrems II implications).
    /// </remarks>
    /// <param name="domainEvent">The residency policy updated event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model.</returns>
    public ResidencyPolicyReadModel Apply(ResidencyPolicyUpdated domainEvent, ResidencyPolicyReadModel current, ProjectionContext context)
    {
        current.AllowedRegionCodes = domainEvent.AllowedRegionCodes;
        current.RequireAdequacyDecision = domainEvent.RequireAdequacyDecision;
        current.AllowedTransferBases = domainEvent.AllowedTransferBases;
        current.LastModifiedAtUtc = new DateTimeOffset(context.Timestamp, TimeSpan.Zero);
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when a residency policy is deleted.
    /// </summary>
    /// <remarks>
    /// Deletion prevents further enforcement of data residency rules for this data category.
    /// Existing data locations are not affected — they remain in their current regions but are
    /// no longer validated against the policy. The event stream preserves the full policy history
    /// for GDPR Article 5(2) accountability.
    /// </remarks>
    /// <param name="domainEvent">The residency policy deleted event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model with <see cref="ResidencyPolicyReadModel.IsActive"/> set to <see langword="false"/>.</returns>
    public ResidencyPolicyReadModel Apply(ResidencyPolicyDeleted domainEvent, ResidencyPolicyReadModel current, ProjectionContext context)
    {
        current.IsActive = false;
        current.DeletionReason = domainEvent.Reason;
        current.LastModifiedAtUtc = new DateTimeOffset(context.Timestamp, TimeSpan.Zero);
        current.Version++;
        return current;
    }
}
