namespace Encina.Compliance.DPIA.Model;

/// <summary>
/// Encapsulates the outcome of a Data Protection Impact Assessment evaluation.
/// </summary>
/// <remarks>
/// <para>
/// Per GDPR Article 35(7), a DPIA must contain at minimum:
/// </para>
/// <list type="bullet">
/// <item><description>(a) A systematic description of the envisaged processing operations and purposes.</description></item>
/// <item><description>(b) An assessment of the necessity and proportionality of the processing.</description></item>
/// <item><description>(c) An assessment of the risks to the rights and freedoms of data subjects.</description></item>
/// <item><description>(d) The measures envisaged to address the risks.</description></item>
/// </list>
/// <para>
/// This record captures items (c) and (d) as structured data: identified risks with their
/// severity levels, and proposed mitigations with implementation tracking.
/// </para>
/// <para>
/// When <see cref="RequiresPriorConsultation"/> is <see langword="true"/>, the controller
/// must consult the supervisory authority under Article 36(1) before proceeding with processing.
/// </para>
/// </remarks>
public sealed record DPIAResult
{
    /// <summary>
    /// The overall risk level determined by the assessment.
    /// </summary>
    /// <remarks>
    /// This represents the aggregate risk after considering all identified risks
    /// and proposed mitigations. It drives enforcement decisions in the pipeline behavior.
    /// </remarks>
    public required RiskLevel OverallRisk { get; init; }

    /// <summary>
    /// Individual risks identified during the assessment.
    /// </summary>
    /// <remarks>
    /// Per Article 35(7)(c), the assessment must identify specific risks to the rights
    /// and freedoms of data subjects. Each risk includes a category, severity level,
    /// and optional mitigation suggestion.
    /// </remarks>
    public required IReadOnlyList<RiskItem> IdentifiedRisks { get; init; }

    /// <summary>
    /// Mitigation measures proposed or implemented to address identified risks.
    /// </summary>
    /// <remarks>
    /// Per Article 35(7)(d), the assessment must include "measures envisaged to address
    /// the risks, including safeguards, security measures and mechanisms."
    /// </remarks>
    public required IReadOnlyList<Mitigation> ProposedMitigations { get; init; }

    /// <summary>
    /// Whether the assessment outcome requires prior consultation with the supervisory authority.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Per Article 36(1), the controller must consult the supervisory authority prior
    /// to processing when the DPIA indicates that the processing would result in a
    /// high risk in the absence of measures taken by the controller to mitigate the risk.
    /// </para>
    /// <para>
    /// This flag is typically set when <see cref="OverallRisk"/> is <see cref="RiskLevel.VeryHigh"/>
    /// or when residual risk after mitigations remains at <see cref="RiskLevel.High"/>.
    /// </para>
    /// </remarks>
    public required bool RequiresPriorConsultation { get; init; }

    /// <summary>
    /// The UTC timestamp when this assessment result was produced.
    /// </summary>
    public required DateTimeOffset AssessedAtUtc { get; init; }

    /// <summary>
    /// The identity of the person or system that performed the assessment, if available.
    /// </summary>
    public string? AssessedBy { get; init; }

    /// <summary>
    /// Determines whether the overall risk is acceptable without additional intervention.
    /// </summary>
    /// <remarks>
    /// An acceptable risk level is <see cref="RiskLevel.Low"/> or <see cref="RiskLevel.Medium"/>.
    /// Processing operations with <see cref="RiskLevel.High"/> or <see cref="RiskLevel.VeryHigh"/>
    /// require additional mitigations or prior consultation before proceeding.
    /// </remarks>
    public bool IsAcceptable => OverallRisk <= RiskLevel.Medium;
}
