namespace Encina.Compliance.BreachNotification.Model;

/// <summary>
/// Severity level of a data breach, used to determine notification obligations.
/// </summary>
/// <remarks>
/// <para>
/// Per GDPR Article 33(1), all breaches must be reported to the supervisory authority
/// unless the breach "is unlikely to result in a risk to the rights and freedoms
/// of natural persons." The severity level helps determine:
/// </para>
/// <list type="bullet">
/// <item><description>Whether supervisory authority notification is required (Art. 33).</description></item>
/// <item><description>Whether data subject notification is required (Art. 34 — "high risk" threshold).</description></item>
/// <item><description>The urgency of the response and notification timeline.</description></item>
/// </list>
/// <para>
/// Per EDPB Guidelines 9/2022, severity assessment should consider the nature of the breach,
/// the sensitivity and volume of data, the ease of identification of individuals, and the
/// severity of consequences for individuals.
/// </para>
/// </remarks>
public enum BreachSeverity
{
    /// <summary>
    /// Unlikely to result in a risk to the rights and freedoms of natural persons.
    /// </summary>
    /// <remarks>
    /// Per Art. 33(1), notification to the supervisory authority may not be required.
    /// The breach should still be documented per Art. 33(5).
    /// </remarks>
    Low = 0,

    /// <summary>
    /// May result in a risk to the rights and freedoms of natural persons.
    /// </summary>
    /// <remarks>
    /// Supervisory authority notification is required under Art. 33.
    /// Data subject notification typically not required unless risk escalates.
    /// </remarks>
    Medium = 1,

    /// <summary>
    /// Likely to result in a high risk to the rights and freedoms of natural persons.
    /// </summary>
    /// <remarks>
    /// Both supervisory authority notification (Art. 33) and data subject
    /// notification (Art. 34) are required. The controller must communicate
    /// the breach to affected individuals without undue delay.
    /// </remarks>
    High = 2,

    /// <summary>
    /// Severe breach with immediate and significant impact on data subjects.
    /// </summary>
    /// <remarks>
    /// Requires immediate notification to both the supervisory authority (Art. 33)
    /// and affected data subjects (Art. 34). May involve sensitive data categories
    /// (Art. 9), large-scale data exposure, or high risk of identity fraud.
    /// </remarks>
    Critical = 3
}
