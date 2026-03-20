namespace Encina.Compliance.NIS2.Model;

/// <summary>
/// Result of a supply chain security assessment for a specific supplier (Art. 21(2)(d)).
/// </summary>
/// <remarks>
/// <para>
/// Produced by <c>ISupplyChainSecurityValidator.AssessSupplierAsync()</c>. Contains the
/// overall risk rating, individual risk findings, and the assessment timestamp.
/// </para>
/// <para>
/// Per NIS2 Article 21(2)(d), entities must address supply chain security by maintaining
/// an understanding of the security posture of their direct suppliers and service providers.
/// Regular assessments ensure that supplier risks are identified and mitigated in a timely manner.
/// </para>
/// </remarks>
public sealed record SupplyChainAssessment
{
    /// <summary>
    /// Identifier of the assessed supplier.
    /// </summary>
    public required string SupplierId { get; init; }

    /// <summary>
    /// Overall risk level determined by the assessment.
    /// </summary>
    /// <remarks>
    /// Typically the highest <see cref="SupplierRiskLevel"/> among all individual
    /// <see cref="Risks"/>, but may be adjusted based on mitigation measures in place.
    /// </remarks>
    public required SupplierRiskLevel OverallRisk { get; init; }

    /// <summary>
    /// Individual risk findings from the assessment.
    /// </summary>
    public required IReadOnlyList<SupplierRisk> Risks { get; init; }

    /// <summary>
    /// Timestamp when the assessment was performed (UTC).
    /// </summary>
    public required DateTimeOffset AssessedAtUtc { get; init; }

    /// <summary>
    /// Recommended date for the next reassessment (UTC).
    /// </summary>
    /// <remarks>
    /// Higher-risk suppliers should be reassessed more frequently. Typical intervals:
    /// <see cref="SupplierRiskLevel.Low"/> — annually,
    /// <see cref="SupplierRiskLevel.Medium"/> — semi-annually,
    /// <see cref="SupplierRiskLevel.High"/> — quarterly,
    /// <see cref="SupplierRiskLevel.Critical"/> — monthly.
    /// </remarks>
    public required DateTimeOffset NextAssessmentDueAtUtc { get; init; }

    /// <summary>
    /// Creates a supply chain assessment result.
    /// </summary>
    /// <param name="supplierId">Identifier of the assessed supplier.</param>
    /// <param name="overallRisk">Overall risk level.</param>
    /// <param name="risks">Individual risk findings.</param>
    /// <param name="assessedAtUtc">Timestamp of the assessment.</param>
    /// <param name="nextAssessmentDueAtUtc">Recommended next reassessment date.</param>
    /// <returns>A new <see cref="SupplyChainAssessment"/>.</returns>
    public static SupplyChainAssessment Create(
        string supplierId,
        SupplierRiskLevel overallRisk,
        IReadOnlyList<SupplierRisk> risks,
        DateTimeOffset assessedAtUtc,
        DateTimeOffset nextAssessmentDueAtUtc) =>
        new()
        {
            SupplierId = supplierId,
            OverallRisk = overallRisk,
            Risks = risks,
            AssessedAtUtc = assessedAtUtc,
            NextAssessmentDueAtUtc = nextAssessmentDueAtUtc
        };
}
