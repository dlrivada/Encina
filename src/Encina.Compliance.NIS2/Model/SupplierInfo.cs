namespace Encina.Compliance.NIS2.Model;

/// <summary>
/// Information about a supplier registered for NIS2 supply chain security assessment (Art. 21(2)(d)).
/// </summary>
/// <remarks>
/// <para>
/// Per NIS2 Article 21(2)(d), entities must address supply chain security including
/// security-related aspects concerning relationships with direct suppliers or service providers.
/// </para>
/// <para>
/// Per Art. 21(3), entities shall take into account the vulnerabilities specific to each
/// direct supplier and service provider, the overall quality of products and cybersecurity
/// practices of their suppliers, and the results of coordinated security risk assessments.
/// </para>
/// <para>
/// Suppliers are registered via <c>NIS2Options.AddSupplier()</c> and evaluated by the
/// <c>ISupplyChainSecurityValidator</c>.
/// </para>
/// </remarks>
public sealed record SupplierInfo
{
    /// <summary>
    /// Unique identifier for this supplier.
    /// </summary>
    /// <remarks>
    /// Used in the <c>[NIS2SupplyChainCheck("supplier-id")]</c> attribute to associate
    /// pipeline requests with specific suppliers.
    /// </remarks>
    public required string SupplierId { get; init; }

    /// <summary>
    /// Display name of the supplier.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Assessed risk level of the supplier.
    /// </summary>
    public required SupplierRiskLevel RiskLevel { get; init; }

    /// <summary>
    /// Timestamp of the most recent security assessment for this supplier (UTC).
    /// </summary>
    /// <remarks>
    /// Per Art. 21(3), entities should regularly assess the cybersecurity practices
    /// of their suppliers. A <c>null</c> value indicates the supplier has never been assessed.
    /// </remarks>
    public DateTimeOffset? LastAssessmentAtUtc { get; init; }

    /// <summary>
    /// Mitigation measures in place for this supplier's identified risks.
    /// </summary>
    /// <remarks>
    /// Examples: contractual security requirements, SLA terms, audit rights,
    /// alternative supplier arrangements, monitoring controls.
    /// </remarks>
    public IReadOnlyList<string> MitigationMeasures { get; init; } = [];

    /// <summary>
    /// Current certification status of the supplier (e.g., ISO 27001, SOC 2, CSA STAR).
    /// </summary>
    /// <remarks>
    /// <c>null</c> when no certification information is available.
    /// Certifications can reduce the assessed <see cref="RiskLevel"/> and satisfy
    /// part of the supply chain security requirements under Art. 21(2)(d).
    /// </remarks>
    public string? CertificationStatus { get; init; }

    /// <summary>
    /// Creates a new supplier information record.
    /// </summary>
    /// <param name="supplierId">Unique supplier identifier.</param>
    /// <param name="name">Display name of the supplier.</param>
    /// <param name="riskLevel">Assessed risk level.</param>
    /// <returns>A new <see cref="SupplierInfo"/> with default empty mitigation measures.</returns>
    public static SupplierInfo Create(
        string supplierId,
        string name,
        SupplierRiskLevel riskLevel) =>
        new()
        {
            SupplierId = supplierId,
            Name = name,
            RiskLevel = riskLevel
        };
}
