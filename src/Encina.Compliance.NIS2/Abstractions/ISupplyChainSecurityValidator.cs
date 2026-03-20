using Encina.Compliance.NIS2.Model;

using LanguageExt;

namespace Encina.Compliance.NIS2.Abstractions;

/// <summary>
/// Validates supply chain security posture for NIS2 compliance (Art. 21(2)(d)).
/// </summary>
/// <remarks>
/// <para>
/// Per NIS2 Article 21(2)(d), entities must address "supply chain security, including
/// security-related aspects concerning the relationships between each entity and its
/// direct suppliers or service providers."
/// </para>
/// <para>
/// Per Art. 21(3), entities shall take into account:
/// </para>
/// <list type="bullet">
/// <item><description>The vulnerabilities specific to each direct supplier and service provider.</description></item>
/// <item><description>The overall quality of products and cybersecurity practices of suppliers,
/// including their secure development procedures.</description></item>
/// <item><description>The results of coordinated security risk assessments of critical supply chains
/// carried out in accordance with Art. 22(1).</description></item>
/// </list>
/// <para>
/// Suppliers are registered via <c>NIS2Options.AddSupplier()</c>. The default implementation
/// evaluates suppliers against their configured risk level, last assessment date, and
/// certification status.
/// </para>
/// </remarks>
public interface ISupplyChainSecurityValidator
{
    /// <summary>
    /// Performs a security assessment of the specified supplier.
    /// </summary>
    /// <param name="supplierId">Unique identifier of the supplier to assess.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="SupplyChainAssessment"/> with the overall risk, individual risk findings,
    /// and next assessment date; or an <see cref="EncinaError"/> if the supplier is unknown
    /// or assessment failed.
    /// </returns>
    ValueTask<Either<EncinaError, SupplyChainAssessment>> AssessSupplierAsync(
        string supplierId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all identified risks across all registered suppliers.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A list of <see cref="SupplierRisk"/> findings for all registered suppliers;
    /// or an <see cref="EncinaError"/> if the assessment could not be performed.
    /// An empty list indicates no risks were identified.
    /// </returns>
    ValueTask<Either<EncinaError, IReadOnlyList<SupplierRisk>>> GetSupplierRisksAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates whether a supplier is acceptable for a specific operation based on its risk level.
    /// </summary>
    /// <param name="supplierId">Unique identifier of the supplier to validate.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <c>true</c> if the supplier's risk level permits the operation;
    /// <c>false</c> if the supplier is too risky (e.g., <see cref="SupplierRiskLevel.Critical"/>
    /// or <see cref="SupplierRiskLevel.High"/> depending on enforcement mode);
    /// or an <see cref="EncinaError"/> if the supplier is unknown.
    /// </returns>
    /// <remarks>
    /// Used by the <c>NIS2CompliancePipelineBehavior</c> to enforce supply chain checks
    /// on requests decorated with the <c>[NIS2SupplyChainCheck]</c> attribute.
    /// </remarks>
    ValueTask<Either<EncinaError, bool>> ValidateSupplierForOperationAsync(
        string supplierId,
        CancellationToken cancellationToken = default);
}
