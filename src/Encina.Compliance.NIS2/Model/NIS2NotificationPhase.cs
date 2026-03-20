namespace Encina.Compliance.NIS2.Model;

/// <summary>
/// Phases of the NIS2 incident notification timeline as defined in Article 23(4).
/// </summary>
/// <remarks>
/// <para>
/// NIS2 Article 23(4) establishes a phased notification process for significant incidents:
/// </para>
/// <list type="number">
/// <item><description><see cref="EarlyWarning"/>: Within 24 hours of becoming aware (Art. 23(4)(a)).</description></item>
/// <item><description><see cref="IncidentNotification"/>: Within 72 hours of becoming aware (Art. 23(4)(b)).</description></item>
/// <item><description><see cref="IntermediateReport"/>: Upon request of CSIRT or competent authority (Art. 23(4)(c)).</description></item>
/// <item><description><see cref="FinalReport"/>: Within one month of the incident notification (Art. 23(4)(d)).</description></item>
/// </list>
/// <para>
/// For essential entities in the digital infrastructure or ICT service management sectors,
/// the early warning deadline is shortened to 24 hours for significant incidents.
/// </para>
/// </remarks>
public enum NIS2NotificationPhase
{
    /// <summary>
    /// Early warning — due within 24 hours of becoming aware of a significant incident.
    /// </summary>
    /// <remarks>
    /// Per Art. 23(4)(a), the early warning must indicate whether the significant incident
    /// is suspected of being caused by unlawful or malicious acts or could have a cross-border
    /// impact. This is a preliminary notification to alert the CSIRT or competent authority.
    /// </remarks>
    EarlyWarning = 0,

    /// <summary>
    /// Incident notification — due within 72 hours of becoming aware of a significant incident.
    /// </summary>
    /// <remarks>
    /// Per Art. 23(4)(b), the incident notification must update the early warning information
    /// and provide an initial assessment of the significant incident, including its severity
    /// and impact, as well as, where available, the indicators of compromise.
    /// </remarks>
    IncidentNotification = 1,

    /// <summary>
    /// Intermediate report — provided upon request of the CSIRT or competent authority.
    /// </summary>
    /// <remarks>
    /// Per Art. 23(4)(c), the CSIRT or competent authority may request relevant status
    /// updates on the incident handling at any time during the incident lifecycle.
    /// </remarks>
    IntermediateReport = 2,

    /// <summary>
    /// Final report — due within one month of the incident notification submission.
    /// </summary>
    /// <remarks>
    /// Per Art. 23(4)(d), the final report must include: a detailed description of the incident
    /// including its severity and impact; the type of threat or root cause that likely triggered
    /// the incident; applied and ongoing mitigation measures; and, where applicable, the
    /// cross-border impact of the incident.
    /// </remarks>
    FinalReport = 3
}
