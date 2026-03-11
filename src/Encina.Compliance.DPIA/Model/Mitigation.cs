namespace Encina.Compliance.DPIA.Model;

/// <summary>
/// Represents a proposed or implemented mitigation measure for risks identified in a DPIA.
/// </summary>
/// <remarks>
/// <para>
/// GDPR Article 35(7)(d) requires a DPIA to include "the measures envisaged to address
/// the risks, including safeguards, security measures and mechanisms to ensure the protection
/// of personal data and to demonstrate compliance with this Regulation."
/// </para>
/// <para>
/// Each mitigation tracks both proposed measures and their implementation status,
/// providing an auditable trail of risk management actions.
/// </para>
/// </remarks>
/// <param name="Description">A human-readable description of the mitigation measure.</param>
/// <param name="Category">
/// The category of mitigation (e.g., "Technical", "Organizational", "Legal").
/// Used for grouping and compliance reporting.
/// </param>
/// <param name="IsImplemented">
/// Whether this mitigation has been implemented.
/// <see langword="false"/> indicates a proposed but not yet applied measure.
/// </param>
/// <param name="ImplementedAtUtc">
/// The UTC timestamp when this mitigation was implemented, or <see langword="null"/>
/// if not yet implemented.
/// </param>
public sealed record Mitigation(
    string Description,
    string Category,
    bool IsImplemented,
    DateTimeOffset? ImplementedAtUtc);
