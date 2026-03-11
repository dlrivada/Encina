namespace Encina.Compliance.DPIA.Model;

/// <summary>
/// Defines a reusable template for conducting Data Protection Impact Assessments.
/// </summary>
/// <remarks>
/// <para>
/// Templates provide a structured framework for assessors, ensuring that assessments
/// are comprehensive, consistent, and cover all elements required by GDPR Article 35(7).
/// Different processing types may require different templates (e.g., a template for
/// automated decision-making vs. one for large-scale processing of special categories).
/// </para>
/// <para>
/// Templates include pre-defined risk categories and suggested mitigations relevant
/// to the processing type, accelerating the assessment process while maintaining rigor.
/// </para>
/// </remarks>
public sealed record DPIATemplate
{
    /// <summary>
    /// The display name of this template.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// A description of the template's purpose and the type of processing it targets.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// The type of processing this template is designed for (e.g., "AutomatedDecisionMaking", "LargeScaleProcessing").
    /// </summary>
    public required string ProcessingType { get; init; }

    /// <summary>
    /// The ordered sections that compose this template.
    /// </summary>
    /// <remarks>
    /// Sections should cover the mandatory DPIA content per Article 35(7):
    /// description of processing, necessity/proportionality assessment,
    /// risk assessment, and envisaged measures.
    /// </remarks>
    public required IReadOnlyList<DPIASection> Sections { get; init; }

    /// <summary>
    /// Pre-defined risk categories relevant to this template's processing type.
    /// </summary>
    /// <remarks>
    /// Provides a starting point for risk identification. Assessors may add
    /// additional categories as needed.
    /// </remarks>
    public required IReadOnlyList<string> RiskCategories { get; init; }

    /// <summary>
    /// Suggested mitigation measures commonly applicable to this processing type.
    /// </summary>
    /// <remarks>
    /// Derived from supervisory authority guidance and industry best practices.
    /// Assessors should evaluate each suggestion's applicability to their specific context.
    /// </remarks>
    public required IReadOnlyList<string> SuggestedMitigations { get; init; }
}
