namespace Encina.Compliance.AIAct.Model;

/// <summary>
/// Represents the technical documentation required for high-risk AI systems
/// under Article 11 and Annex IV of the EU AI Act.
/// </summary>
/// <remarks>
/// <para>
/// Article 11(1) requires that the technical documentation of a high-risk AI system
/// be drawn up before that system is placed on the market or put into service and shall
/// be kept up to date. It shall demonstrate that the system complies with the requirements
/// set out in Chapter III, Section 2.
/// </para>
/// <para>
/// Annex IV specifies the content of the technical documentation, including: a general
/// description, design specifications, data governance practices, risk management measures,
/// accuracy and robustness metrics, and human oversight mechanisms.
/// </para>
/// <para>
/// In the core package, this record provides the structure. Full documentation generation
/// is implemented in the child issue "AI Act Technical Documentation Generation" (#840).
/// </para>
/// </remarks>
public sealed record TechnicalDocumentation
{
    /// <summary>
    /// Identifier of the AI system this documentation describes.
    /// </summary>
    public required string SystemId { get; init; }

    /// <summary>
    /// General description of the AI system, including its intended purpose,
    /// as required by Annex IV, point 1.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Design specifications and development process description,
    /// as required by Annex IV, point 2.
    /// </summary>
    /// <remarks>
    /// Includes the general logic of the AI system, key design choices, and
    /// computational resources used.
    /// </remarks>
    public string? DesignSpecifications { get; init; }

    /// <summary>
    /// Description of data governance practices applied to training, validation,
    /// and testing data sets, as required by Annex IV, point 2(d).
    /// </summary>
    /// <remarks>
    /// Should cover data collection, origin, scope, characteristics, and suitability.
    /// </remarks>
    public string? DataGovernancePractices { get; init; }

    /// <summary>
    /// Description of risk management measures implemented per Article 9.
    /// </summary>
    public string? RiskManagementMeasures { get; init; }

    /// <summary>
    /// Accuracy metrics and validation results, as required by Annex IV, point 3.
    /// </summary>
    public string? AccuracyMetrics { get; init; }

    /// <summary>
    /// Robustness and cybersecurity metrics, as required by Article 15.
    /// </summary>
    public string? RobustnessMetrics { get; init; }

    /// <summary>
    /// Description of human oversight mechanisms, as required by Article 14.
    /// </summary>
    public string? HumanOversightMechanisms { get; init; }

    /// <summary>
    /// Timestamp when this technical documentation was generated or last updated (UTC).
    /// </summary>
    public required DateTimeOffset GeneratedAtUtc { get; init; }
}
