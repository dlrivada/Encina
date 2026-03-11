namespace Encina.Compliance.DPIA.Model;

/// <summary>
/// Defines a section within a DPIA template, containing guiding questions for assessors.
/// </summary>
/// <remarks>
/// <para>
/// Per GDPR Article 35(7), a DPIA must include specific content areas. Sections provide
/// a structured framework for assessors to address each required aspect systematically.
/// Templates composed of sections ensure assessments are comprehensive and consistent.
/// </para>
/// <para>
/// Sections are immutable value objects that form part of a <see cref="DPIATemplate"/>.
/// </para>
/// </remarks>
/// <param name="Name">The display name of this section (e.g., "Processing Description", "Necessity Assessment").</param>
/// <param name="Description">A brief explanation of what this section covers and why it is needed.</param>
/// <param name="IsRequired">
/// Whether this section must be completed for the assessment to be considered valid.
/// Required sections correspond to the mandatory elements in Article 35(7)(a)-(d).
/// </param>
/// <param name="Questions">
/// Guiding questions for the assessor to address within this section.
/// These help ensure comprehensive coverage of the section's topic area.
/// </param>
public sealed record DPIASection(
    string Name,
    string Description,
    bool IsRequired,
    IReadOnlyList<string> Questions);
