namespace Encina.Compliance.AIAct.Model;

/// <summary>
/// Represents the registration of an AI system in the organisation's AI system inventory,
/// capturing its risk classification and regulatory metadata.
/// </summary>
/// <remarks>
/// <para>
/// Article 49 requires providers of high-risk AI systems to register them in the EU database
/// before placing them on the market or putting them into service. The registration includes
/// the system's intended purpose, risk level, and deployment context.
/// </para>
/// <para>
/// This record is used by <c>IAISystemRegistry</c> to maintain an in-memory catalogue of
/// AI systems and their classifications for pipeline enforcement.
/// </para>
/// </remarks>
public sealed record AISystemRegistration
{
    /// <summary>
    /// Unique identifier for this AI system within the organisation.
    /// </summary>
    /// <example>"recruitment-cv-screener-v2"</example>
    public required string SystemId { get; init; }

    /// <summary>
    /// Human-readable name of the AI system.
    /// </summary>
    /// <example>"CV Screening Assistant"</example>
    public required string Name { get; init; }

    /// <summary>
    /// The Annex III category that this AI system falls into, used to determine
    /// high-risk classification under Article 6(2).
    /// </summary>
    public required AISystemCategory Category { get; init; }

    /// <summary>
    /// The assessed risk level of this AI system under the EU AI Act risk pyramid.
    /// </summary>
    public required AIRiskLevel RiskLevel { get; init; }

    /// <summary>
    /// Name of the provider (developer/manufacturer) of the AI system.
    /// </summary>
    /// <remarks>
    /// Article 3(3) defines "provider" as the natural or legal person that develops
    /// an AI system or has it developed with a view to placing it on the market or
    /// putting it into service under its own name or trademark.
    /// </remarks>
    public string? Provider { get; init; }

    /// <summary>
    /// Version identifier of the AI system or model.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Free-text description of the AI system's intended purpose.
    /// </summary>
    /// <remarks>
    /// Article 3(12) defines "intended purpose" as the use for which the AI system
    /// is intended by the provider, including the specific context and conditions of use.
    /// </remarks>
    public string? Description { get; init; }

    /// <summary>
    /// Timestamp when this AI system was registered in the inventory (UTC).
    /// </summary>
    public required DateTimeOffset RegisteredAtUtc { get; init; }

    /// <summary>
    /// Description of the deployment context (e.g., "HR department", "customer support chatbot").
    /// </summary>
    /// <remarks>
    /// The deployment context is relevant for determining whether certain prohibitions
    /// apply (e.g., emotion recognition in the workplace under Art. 5(1)(f)).
    /// </remarks>
    public string? DeploymentContext { get; init; }

    /// <summary>
    /// List of prohibited practices (Art. 5) that have been evaluated and identified
    /// as applicable to this AI system.
    /// </summary>
    /// <remarks>
    /// An empty list indicates that no prohibited practices apply. If any practice
    /// is present, the system should be classified as <see cref="AIRiskLevel.Prohibited"/>.
    /// </remarks>
    public IReadOnlyList<ProhibitedPractice> ProhibitedPractices { get; init; } = [];
}
