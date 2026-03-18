using Encina.Compliance.DataResidency.Events;
using Encina.Compliance.DataResidency.Model;
using Encina.DomainModeling;

namespace Encina.Compliance.DataResidency.Aggregates;

/// <summary>
/// Event-sourced aggregate representing a data residency policy for a specific data category.
/// </summary>
/// <remarks>
/// <para>
/// Per GDPR Chapter V (Articles 44–49), personal data transferred outside the EU/EEA requires
/// a valid legal basis and appropriate safeguards. A residency policy defines which geographic
/// regions are allowed for storing and processing data of a given category, whether an EU
/// adequacy decision (Art. 45) is required, and which transfer legal bases are acceptable.
/// </para>
/// <para>
/// The policy lifecycle is: <c>Active → Deleted</c>. An active policy can be updated with new
/// allowed regions, adequacy requirements, or transfer bases. Deletion prevents further enforcement
/// but the event stream preserves the full history for GDPR Article 5(2) accountability.
/// </para>
/// <para>
/// All state changes are captured as immutable events, providing a complete audit trail for
/// GDPR Article 5(2) accountability and Article 58 supervisory authority inquiries. Events
/// implement <see cref="INotification"/> and are automatically published by
/// <c>EventPublishingPipelineBehavior</c> after successful Marten commit.
/// </para>
/// </remarks>
public sealed class ResidencyPolicyAggregate : AggregateBase
{
    /// <summary>
    /// The data category this policy applies to (e.g., "personal-data", "healthcare-data").
    /// </summary>
    public string DataCategory { get; private set; } = string.Empty;

    /// <summary>
    /// Region codes where data of this category is allowed to be stored.
    /// </summary>
    /// <remarks>
    /// An empty list means no geographic restrictions are applied. Region codes are
    /// case-insensitive identifiers (ISO 3166-1 alpha-2, regional, or custom).
    /// </remarks>
    public IReadOnlyList<string> AllowedRegionCodes { get; private set; } = [];

    /// <summary>
    /// Whether the region must have an EU adequacy decision under GDPR Article 45.
    /// </summary>
    /// <remarks>
    /// When <see langword="true"/>, data can only be stored in regions with an adequacy decision
    /// from the European Commission. This is the strictest transfer mechanism and eliminates
    /// the need for supplementary measures.
    /// </remarks>
    public bool RequireAdequacyDecision { get; private set; }

    /// <summary>
    /// Legal bases acceptable for cross-border transfers involving this data category.
    /// </summary>
    /// <remarks>
    /// Defines which GDPR Chapter V mechanisms are permitted: adequacy decisions (Art. 45),
    /// Standard Contractual Clauses (Art. 46(2)(c)), Binding Corporate Rules (Art. 47),
    /// explicit consent (Art. 49(1)(a)), or other derogations (Art. 49).
    /// </remarks>
    public IReadOnlyList<TransferLegalBasis> AllowedTransferBases { get; private set; } = [];

    /// <summary>
    /// Whether this policy is currently active and enforcing data residency rules.
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
    /// Creates a new data residency policy for a specific data category.
    /// </summary>
    /// <param name="id">Unique identifier for the new policy aggregate.</param>
    /// <param name="dataCategory">The data category this policy applies to.</param>
    /// <param name="allowedRegionCodes">Region codes where data is allowed to be stored. Empty means no restrictions.</param>
    /// <param name="requireAdequacyDecision">Whether an EU adequacy decision is required (Art. 45).</param>
    /// <param name="allowedTransferBases">Legal bases acceptable for cross-border transfers.</param>
    /// <param name="tenantId">Optional tenant identifier for multi-tenancy scoping.</param>
    /// <param name="moduleId">Optional module identifier for modular monolith scoping.</param>
    /// <returns>A new <see cref="ResidencyPolicyAggregate"/> in active status.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="dataCategory"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="allowedRegionCodes"/> or <paramref name="allowedTransferBases"/> is null.</exception>
    public static ResidencyPolicyAggregate Create(
        Guid id,
        string dataCategory,
        IReadOnlyList<string> allowedRegionCodes,
        bool requireAdequacyDecision,
        IReadOnlyList<TransferLegalBasis> allowedTransferBases,
        string? tenantId = null,
        string? moduleId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataCategory);
        ArgumentNullException.ThrowIfNull(allowedRegionCodes);
        ArgumentNullException.ThrowIfNull(allowedTransferBases);

        var aggregate = new ResidencyPolicyAggregate();
        aggregate.RaiseEvent(new ResidencyPolicyCreated(
            id, dataCategory, allowedRegionCodes, requireAdequacyDecision,
            allowedTransferBases, tenantId, moduleId));
        return aggregate;
    }

    /// <summary>
    /// Updates the residency policy with new parameters.
    /// </summary>
    /// <param name="allowedRegionCodes">Updated list of allowed region codes.</param>
    /// <param name="requireAdequacyDecision">Updated adequacy decision requirement.</param>
    /// <param name="allowedTransferBases">Updated list of allowed transfer legal bases.</param>
    /// <exception cref="InvalidOperationException">Thrown when the policy has been deleted.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="allowedRegionCodes"/> or <paramref name="allowedTransferBases"/> is null.</exception>
    public void Update(
        IReadOnlyList<string> allowedRegionCodes,
        bool requireAdequacyDecision,
        IReadOnlyList<TransferLegalBasis> allowedTransferBases)
    {
        if (!IsActive)
        {
            throw new InvalidOperationException(
                $"Cannot update policy '{Id}' because it has been deleted.");
        }

        ArgumentNullException.ThrowIfNull(allowedRegionCodes);
        ArgumentNullException.ThrowIfNull(allowedTransferBases);

        RaiseEvent(new ResidencyPolicyUpdated(
            Id, allowedRegionCodes, requireAdequacyDecision, allowedTransferBases));
    }

    /// <summary>
    /// Deletes the policy, preventing further data residency enforcement for this data category.
    /// </summary>
    /// <remarks>
    /// Existing data locations are not affected — they remain in their current regions but are
    /// no longer validated against the policy. The event stream preserves the full policy history
    /// for GDPR Article 5(2) accountability.
    /// </remarks>
    /// <param name="reason">The reason for deleting this policy.</param>
    /// <exception cref="InvalidOperationException">Thrown when the policy is already deleted.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="reason"/> is null or whitespace.</exception>
    public void Delete(string reason)
    {
        if (!IsActive)
        {
            throw new InvalidOperationException(
                $"Cannot delete policy '{Id}' because it is already deleted.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        RaiseEvent(new ResidencyPolicyDeleted(Id, reason));
    }

    /// <inheritdoc />
    protected override void Apply(object domainEvent)
    {
        switch (domainEvent)
        {
            case ResidencyPolicyCreated e:
                Id = e.PolicyId;
                DataCategory = e.DataCategory;
                AllowedRegionCodes = e.AllowedRegionCodes;
                RequireAdequacyDecision = e.RequireAdequacyDecision;
                AllowedTransferBases = e.AllowedTransferBases;
                IsActive = true;
                TenantId = e.TenantId;
                ModuleId = e.ModuleId;
                break;

            case ResidencyPolicyUpdated e:
                AllowedRegionCodes = e.AllowedRegionCodes;
                RequireAdequacyDecision = e.RequireAdequacyDecision;
                AllowedTransferBases = e.AllowedTransferBases;
                break;

            case ResidencyPolicyDeleted:
                IsActive = false;
                break;
        }
    }
}
