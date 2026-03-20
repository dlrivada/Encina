using Encina.Compliance.AIAct.Model;

namespace Encina.Compliance.AIAct.Attributes;

/// <summary>
/// Marks a request type as associated with a high-risk AI system under
/// Article 6 and Annex III of the EU AI Act.
/// </summary>
/// <remarks>
/// <para>
/// When <c>AIActOptions.AutoRegisterFromAttributes</c> is enabled, request types decorated
/// with this attribute are automatically discovered at startup and registered in the
/// <see cref="Abstractions.IAISystemRegistry"/>.
/// </para>
/// <para>
/// The <c>AIActCompliancePipelineBehavior</c> uses this attribute to:
/// </para>
/// <list type="number">
/// <item>Identify the AI system category (Annex III)</item>
/// <item>Determine the risk classification (Art. 6)</item>
/// <item>Enforce applicable requirements (Art. 8-15 for high-risk systems)</item>
/// <item>Trigger human oversight checks if the system is high-risk (Art. 14)</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// [HighRiskAI(
///     Category = AISystemCategory.EmploymentWorkersManagement,
///     SystemId = "cv-screener-v2",
///     Provider = "Contoso HR",
///     Description = "Automated CV screening for recruitment")]
/// public sealed record ScreenCandidateCommand(string CandidateId) : ICommand&lt;ScreeningResult&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class HighRiskAIAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the Annex III category of the AI system.
    /// </summary>
    /// <remarks>
    /// Article 6(2) states that an AI system is considered high-risk if it falls into
    /// one of the areas listed in Annex III. This property determines which category applies.
    /// </remarks>
    public required AISystemCategory Category { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the AI system.
    /// </summary>
    /// <remarks>
    /// When <c>null</c>, the system ID is derived from the request type's full name
    /// during auto-registration.
    /// </remarks>
    public string? SystemId { get; set; }

    /// <summary>
    /// Gets or sets the name of the provider (developer/manufacturer) of the AI system.
    /// </summary>
    /// <remarks>
    /// Article 3(3) defines "provider" as the natural or legal person that develops
    /// an AI system with a view to placing it on the market.
    /// </remarks>
    public string? Provider { get; set; }

    /// <summary>
    /// Gets or sets the version identifier of the AI system or model.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets a description of the AI system's intended purpose.
    /// </summary>
    /// <remarks>
    /// Article 3(12) defines "intended purpose" as the use for which the AI system
    /// is intended by the provider, including the specific context and conditions of use.
    /// </remarks>
    public string? Description { get; set; }
}
