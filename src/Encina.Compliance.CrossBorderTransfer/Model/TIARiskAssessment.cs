namespace Encina.Compliance.CrossBorderTransfer.Model;

/// <summary>
/// Represents the result of a pluggable risk assessment for a destination country.
/// </summary>
/// <remarks>
/// <para>
/// Produced by <c>ITIARiskAssessor</c> implementations. Contains the risk score,
/// contributing factors, and recommended supplementary measures based on the
/// destination country's legal framework analysis.
/// </para>
/// <para>
/// The risk assessment follows EDPB Recommendations 01/2020 for evaluating
/// the level of protection in the destination country.
/// </para>
/// </remarks>
/// <param name="Score">Risk score between 0.0 (no risk) and 1.0 (maximum risk).</param>
/// <param name="Factors">Contributing risk factors identified in the assessment.</param>
/// <param name="Recommendations">Recommended supplementary measures to mitigate identified risks.</param>
public sealed record TIARiskAssessment(
    double Score,
    IReadOnlyList<string> Factors,
    IReadOnlyList<string> Recommendations);
