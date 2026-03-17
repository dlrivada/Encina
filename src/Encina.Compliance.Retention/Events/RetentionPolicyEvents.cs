using Encina.Compliance.Retention.Model;

namespace Encina.Compliance.Retention.Events;

// Event-sourced events implement INotification so they are automatically published
// by Encina.Marten's EventPublishingPipelineBehavior after successful command execution.
// This allows handlers to react to aggregate state changes without a separate notification layer.

/// <summary>
/// Raised when a new data retention policy is created for a specific data category.
/// </summary>
/// <remarks>
/// <para>
/// Initiates the retention policy lifecycle. The policy defines how long data in the specified
/// <paramref name="DataCategory"/> should be retained before becoming eligible for deletion,
/// per GDPR Article 5(1)(e) storage limitation principle: "kept in a form which permits
/// identification of data subjects for no longer than is necessary."
/// </para>
/// <para>
/// The <paramref name="PolicyType"/> determines when the retention period starts counting:
/// <see cref="RetentionPolicyType.TimeBased"/> from data creation,
/// <see cref="RetentionPolicyType.EventBased"/> from a specific business event, or
/// <see cref="RetentionPolicyType.ConsentBased"/> until consent withdrawal.
/// </para>
/// <para>
/// When <paramref name="AutoDelete"/> is <see langword="true"/>, the enforcement service
/// will automatically delete expired data. Otherwise, expiration alerts are raised but
/// deletion must be performed manually.
/// </para>
/// </remarks>
/// <param name="PolicyId">Unique identifier for this retention policy aggregate.</param>
/// <param name="DataCategory">The data category this policy applies to (e.g., "customer-data", "financial-records").</param>
/// <param name="RetentionPeriod">How long data in this category should be retained.</param>
/// <param name="AutoDelete">Whether the enforcement service should automatically delete expired data.</param>
/// <param name="PolicyType">The trigger mechanism for the retention period.</param>
/// <param name="Reason">Optional reason or justification for this retention period.</param>
/// <param name="LegalBasis">Optional legal basis for the retention requirement (e.g., "Tax Code §147").</param>
/// <param name="OccurredAtUtc">Timestamp when the policy was created (UTC).</param>
/// <param name="TenantId">Tenant identifier for multi-tenancy scoping.</param>
/// <param name="ModuleId">Module identifier for modular monolith scoping.</param>
public sealed record RetentionPolicyCreated(
    Guid PolicyId,
    string DataCategory,
    TimeSpan RetentionPeriod,
    bool AutoDelete,
    RetentionPolicyType PolicyType,
    string? Reason,
    string? LegalBasis,
    DateTimeOffset OccurredAtUtc,
    string? TenantId,
    string? ModuleId) : INotification;

/// <summary>
/// Raised when an existing retention policy is updated with new parameters.
/// </summary>
/// <remarks>
/// <para>
/// Updates the policy's retention period, auto-deletion behavior, reason, or legal basis.
/// The <paramref name="PolicyId"/> and data category remain unchanged — to change the category,
/// deactivate the existing policy and create a new one.
/// </para>
/// <para>
/// Per GDPR Article 5(2) accountability, this event provides an immutable record of all
/// policy changes, enabling organizations to demonstrate that retention periods were
/// reviewed and adjusted as necessary.
/// </para>
/// </remarks>
/// <param name="PolicyId">The retention policy aggregate identifier.</param>
/// <param name="RetentionPeriod">The updated retention period.</param>
/// <param name="AutoDelete">The updated auto-deletion setting.</param>
/// <param name="Reason">Updated reason or justification for the retention period.</param>
/// <param name="LegalBasis">Updated legal basis for the retention requirement.</param>
/// <param name="OccurredAtUtc">Timestamp when the policy was updated (UTC).</param>
public sealed record RetentionPolicyUpdated(
    Guid PolicyId,
    TimeSpan RetentionPeriod,
    bool AutoDelete,
    string? Reason,
    string? LegalBasis,
    DateTimeOffset OccurredAtUtc) : INotification;

/// <summary>
/// Raised when a retention policy is deactivated, preventing new records from being tracked under it.
/// </summary>
/// <remarks>
/// <para>
/// A deactivated policy no longer accepts new retention records. Existing records tracked
/// under this policy continue their lifecycle (expiration, deletion) unaffected.
/// </para>
/// <para>
/// Deactivation is preferred over deletion to maintain a complete audit trail per
/// GDPR Article 5(2) accountability — the event stream preserves the full history
/// of policy changes for regulatory review.
/// </para>
/// </remarks>
/// <param name="PolicyId">The retention policy aggregate identifier.</param>
/// <param name="Reason">The reason for deactivating this policy.</param>
/// <param name="OccurredAtUtc">Timestamp when the policy was deactivated (UTC).</param>
public sealed record RetentionPolicyDeactivated(
    Guid PolicyId,
    string Reason,
    DateTimeOffset OccurredAtUtc) : INotification;
