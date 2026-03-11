namespace Encina.Compliance.DPIA.Model;

/// <summary>
/// Represents a specific risk identified during a Data Protection Impact Assessment.
/// </summary>
/// <remarks>
/// <para>
/// Per GDPR Article 35(7)(c), a DPIA must include "the measures envisaged to address
/// the risks, including safeguards, security measures and mechanisms to ensure the protection
/// of personal data." Each <see cref="RiskItem"/> captures one identified risk along with
/// its severity classification and an optional mitigation suggestion.
/// </para>
/// <para>
/// Risk items are immutable value objects that form part of the <see cref="DPIAResult"/>.
/// </para>
/// </remarks>
/// <param name="Category">
/// The risk category (e.g., "Data Minimization", "Purpose Limitation", "Security").
/// Used for grouping and reporting across assessments.
/// </param>
/// <param name="Level">The assessed severity level of this risk.</param>
/// <param name="Description">A human-readable description of the identified risk.</param>
/// <param name="MitigationSuggestion">
/// An optional suggested mitigation measure for this specific risk.
/// When provided, this should describe concrete technical or organizational measures.
/// </param>
public sealed record RiskItem(
    string Category,
    RiskLevel Level,
    string Description,
    string? MitigationSuggestion);
