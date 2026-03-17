using Encina.Compliance.Retention.Events;
using Encina.Compliance.Retention.Model;
using Encina.DomainModeling;

namespace Encina.Compliance.Retention.Aggregates;

/// <summary>
/// Event-sourced aggregate representing a data retention policy definition for a specific data category.
/// </summary>
/// <remarks>
/// <para>
/// Per GDPR Article 5(1)(e), personal data shall be "kept in a form which permits identification
/// of data subjects for no longer than is necessary for the purposes for which the personal data
/// are processed." Each policy defines the retention period and enforcement behavior for a
/// specific data category (e.g., "customer-data", "financial-records", "marketing-consent").
/// </para>
/// <para>
/// The policy lifecycle is simple: <c>Active → Deactivated</c>. An active policy can be updated
/// with new parameters (retention period, auto-delete behavior, legal basis). Deactivation prevents
/// new retention records from being tracked under this policy, but existing records continue their
/// lifecycle unaffected.
/// </para>
/// <para>
/// All state changes are captured as immutable events, providing a complete audit trail
/// for GDPR Article 5(2) accountability requirements. Events implement <see cref="INotification"/>
/// and are automatically published by <c>EventPublishingPipelineBehavior</c> after successful
/// Marten commit.
/// </para>
/// </remarks>
public sealed class RetentionPolicyAggregate : AggregateBase
{
    /// <summary>
    /// The data category this policy applies to (e.g., "customer-data", "financial-records").
    /// </summary>
    public string DataCategory { get; private set; } = string.Empty;

    /// <summary>
    /// How long data in this category should be retained before becoming eligible for deletion.
    /// </summary>
    public TimeSpan RetentionPeriod { get; private set; }

    /// <summary>
    /// Whether the enforcement service should automatically delete expired data.
    /// </summary>
    /// <remarks>
    /// When <see langword="false"/>, expiration alerts are raised but deletion must be
    /// performed manually.
    /// </remarks>
    public bool AutoDelete { get; private set; }

    /// <summary>
    /// The trigger mechanism for the retention period.
    /// </summary>
    public RetentionPolicyType PolicyType { get; private set; }

    /// <summary>
    /// Optional reason or justification for this retention period.
    /// </summary>
    public string? Reason { get; private set; }

    /// <summary>
    /// Optional legal basis for the retention requirement (e.g., "Tax Code §147").
    /// </summary>
    public string? LegalBasis { get; private set; }

    /// <summary>
    /// Whether this policy is currently active and accepting new retention records.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Tenant identifier for multi-tenancy scoping.
    /// </summary>
    public string? TenantId { get; private set; }

    /// <summary>
    /// Module identifier for modular monolith scoping.
    /// </summary>
    public string? ModuleId { get; private set; }

    /// <summary>
    /// The UTC timestamp when this policy was created.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>
    /// The UTC timestamp when this policy was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedAtUtc { get; private set; }

    /// <summary>
    /// Creates a new data retention policy for a specific data category.
    /// </summary>
    /// <param name="id">Unique identifier for the new policy aggregate.</param>
    /// <param name="dataCategory">The data category this policy applies to.</param>
    /// <param name="retentionPeriod">How long data should be retained.</param>
    /// <param name="autoDelete">Whether to automatically delete expired data.</param>
    /// <param name="policyType">The trigger mechanism for the retention period.</param>
    /// <param name="reason">Optional reason or justification for this retention period.</param>
    /// <param name="legalBasis">Optional legal basis for the retention requirement.</param>
    /// <param name="occurredAtUtc">Timestamp when the policy was created (UTC).</param>
    /// <param name="tenantId">Optional tenant identifier for multi-tenancy scoping.</param>
    /// <param name="moduleId">Optional module identifier for modular monolith scoping.</param>
    /// <returns>A new <see cref="RetentionPolicyAggregate"/> in active status.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="dataCategory"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="retentionPeriod"/> is zero or negative.</exception>
    public static RetentionPolicyAggregate Create(
        Guid id,
        string dataCategory,
        TimeSpan retentionPeriod,
        bool autoDelete,
        RetentionPolicyType policyType,
        string? reason,
        string? legalBasis,
        DateTimeOffset occurredAtUtc,
        string? tenantId = null,
        string? moduleId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataCategory);

        if (retentionPeriod <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(retentionPeriod), retentionPeriod,
                "Retention period must be a positive duration.");
        }

        var aggregate = new RetentionPolicyAggregate();
        aggregate.RaiseEvent(new RetentionPolicyCreated(
            id, dataCategory, retentionPeriod, autoDelete, policyType,
            reason, legalBasis, occurredAtUtc, tenantId, moduleId));
        return aggregate;
    }

    /// <summary>
    /// Updates the retention policy with new parameters.
    /// </summary>
    /// <param name="retentionPeriod">The updated retention period.</param>
    /// <param name="autoDelete">The updated auto-deletion setting.</param>
    /// <param name="reason">Updated reason or justification.</param>
    /// <param name="legalBasis">Updated legal basis.</param>
    /// <param name="occurredAtUtc">Timestamp when the update occurred (UTC).</param>
    /// <exception cref="InvalidOperationException">Thrown when the policy is deactivated.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="retentionPeriod"/> is zero or negative.</exception>
    public void Update(
        TimeSpan retentionPeriod,
        bool autoDelete,
        string? reason,
        string? legalBasis,
        DateTimeOffset occurredAtUtc)
    {
        if (!IsActive)
        {
            throw new InvalidOperationException(
                $"Cannot update policy '{Id}' because it has been deactivated.");
        }

        if (retentionPeriod <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(retentionPeriod), retentionPeriod,
                "Retention period must be a positive duration.");
        }

        RaiseEvent(new RetentionPolicyUpdated(Id, retentionPeriod, autoDelete, reason, legalBasis, occurredAtUtc));
    }

    /// <summary>
    /// Deactivates the policy, preventing new retention records from being tracked under it.
    /// </summary>
    /// <remarks>
    /// Existing retention records tracked under this policy continue their lifecycle (expiration,
    /// deletion) unaffected. Deactivation is preferred over deletion to maintain a complete
    /// audit trail per GDPR Article 5(2) accountability.
    /// </remarks>
    /// <param name="reason">The reason for deactivating this policy.</param>
    /// <param name="occurredAtUtc">Timestamp when the deactivation occurred (UTC).</param>
    /// <exception cref="InvalidOperationException">Thrown when the policy is already deactivated.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="reason"/> is null or whitespace.</exception>
    public void Deactivate(string reason, DateTimeOffset occurredAtUtc)
    {
        if (!IsActive)
        {
            throw new InvalidOperationException(
                $"Cannot deactivate policy '{Id}' because it is already deactivated.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        RaiseEvent(new RetentionPolicyDeactivated(Id, reason, occurredAtUtc));
    }

    /// <inheritdoc />
    protected override void Apply(object domainEvent)
    {
        switch (domainEvent)
        {
            case RetentionPolicyCreated e:
                Id = e.PolicyId;
                DataCategory = e.DataCategory;
                RetentionPeriod = e.RetentionPeriod;
                AutoDelete = e.AutoDelete;
                PolicyType = e.PolicyType;
                Reason = e.Reason;
                LegalBasis = e.LegalBasis;
                IsActive = true;
                TenantId = e.TenantId;
                ModuleId = e.ModuleId;
                CreatedAtUtc = e.OccurredAtUtc;
                LastUpdatedAtUtc = e.OccurredAtUtc;
                break;

            case RetentionPolicyUpdated e:
                RetentionPeriod = e.RetentionPeriod;
                AutoDelete = e.AutoDelete;
                Reason = e.Reason;
                LegalBasis = e.LegalBasis;
                LastUpdatedAtUtc = e.OccurredAtUtc;
                break;

            case RetentionPolicyDeactivated e:
                IsActive = false;
                LastUpdatedAtUtc = e.OccurredAtUtc;
                break;
        }
    }
}
