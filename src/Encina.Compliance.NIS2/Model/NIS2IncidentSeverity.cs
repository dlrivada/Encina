namespace Encina.Compliance.NIS2.Model;

/// <summary>
/// Severity level of a cybersecurity incident under the NIS2 Directive (EU 2022/2555).
/// </summary>
/// <remarks>
/// <para>
/// Per NIS2 Article 23(3), an incident is considered "significant" if it has caused or is
/// capable of causing severe operational disruption of the services or financial loss for
/// the entity concerned, or has affected or is capable of affecting other natural or legal
/// persons by causing considerable material or non-material damage.
/// </para>
/// <para>
/// The severity level determines the urgency of notification obligations and the scope
/// of the response. Essential entities (<see cref="NIS2EntityType.Essential"/>) face
/// stricter reporting requirements regardless of severity.
/// </para>
/// </remarks>
public enum NIS2IncidentSeverity
{
    /// <summary>
    /// Minor incident with limited operational impact.
    /// </summary>
    /// <remarks>
    /// Unlikely to meet the "significant incident" threshold under Art. 23(3).
    /// Should still be documented internally per the entity's incident handling
    /// policy (Art. 21(2)(b)).
    /// </remarks>
    Low = 0,

    /// <summary>
    /// Moderate incident that may affect service availability or integrity.
    /// </summary>
    /// <remarks>
    /// May meet the "significant incident" threshold depending on the duration
    /// and scope of impact. Requires assessment against Art. 23(3) criteria.
    /// </remarks>
    Medium = 1,

    /// <summary>
    /// Serious incident causing significant operational disruption or financial loss.
    /// </summary>
    /// <remarks>
    /// Meets the "significant incident" threshold under Art. 23(3). Triggers mandatory
    /// notification obligations: 24-hour early warning (Art. 23(4)(a)), 72-hour incident
    /// notification (Art. 23(4)(b)), and 1-month final report (Art. 23(4)(d)).
    /// </remarks>
    High = 2,

    /// <summary>
    /// Critical incident with severe, widespread impact on operations and potentially on third parties.
    /// </summary>
    /// <remarks>
    /// Clearly significant under Art. 23(3) — causes or is capable of causing severe
    /// operational disruption and considerable material or non-material damage to
    /// other natural or legal persons. Requires immediate response and may trigger
    /// CSIRT involvement per Art. 23(5).
    /// </remarks>
    Critical = 3
}
