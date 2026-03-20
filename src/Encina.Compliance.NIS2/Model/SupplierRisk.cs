namespace Encina.Compliance.NIS2.Model;

/// <summary>
/// An identified risk associated with a supplier in the supply chain security context (Art. 21(2)(d)).
/// </summary>
/// <remarks>
/// <para>
/// Part of the supply chain risk assessment output from <c>ISupplyChainSecurityValidator</c>.
/// Each <see cref="SupplierRisk"/> identifies a specific risk and provides actionable
/// recommendations for mitigation.
/// </para>
/// <para>
/// Per NIS2 Article 21(3), risk assessment should consider vulnerabilities specific to
/// each supplier, the quality of products and cybersecurity practices, and the results
/// of coordinated risk assessments.
/// </para>
/// </remarks>
public sealed record SupplierRisk
{
    /// <summary>
    /// Identifier of the supplier associated with this risk.
    /// </summary>
    public required string SupplierId { get; init; }

    /// <summary>
    /// Risk level of this specific risk.
    /// </summary>
    public required SupplierRiskLevel RiskLevel { get; init; }

    /// <summary>
    /// Description of the identified risk.
    /// </summary>
    public required string RiskDescription { get; init; }

    /// <summary>
    /// Recommended actions to mitigate this risk.
    /// </summary>
    public required IReadOnlyList<string> RecommendedActions { get; init; }
}
