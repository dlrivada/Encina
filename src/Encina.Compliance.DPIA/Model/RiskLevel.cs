namespace Encina.Compliance.DPIA.Model;

/// <summary>
/// Risk level classification for Data Protection Impact Assessment outcomes.
/// </summary>
/// <remarks>
/// <para>
/// GDPR Article 35(1) requires a DPIA when processing is "likely to result in a high risk
/// to the rights and freedoms of natural persons." This enumeration provides a graduated
/// scale matching ICO (Information Commissioner's Office) guidance for risk classification.
/// </para>
/// <para>
/// The <see cref="High"/> and <see cref="VeryHigh"/> levels indicate that the processing
/// may require prior consultation with the supervisory authority under Article 36(1),
/// particularly when the controller cannot sufficiently mitigate the identified risks.
/// </para>
/// </remarks>
public enum RiskLevel
{
    /// <summary>
    /// Processing poses minimal risk to data subjects' rights and freedoms.
    /// </summary>
    /// <remarks>
    /// No additional safeguards beyond standard data protection measures are required.
    /// </remarks>
    Low = 0,

    /// <summary>
    /// Processing poses moderate risk that can be adequately mitigated with standard measures.
    /// </summary>
    /// <remarks>
    /// Additional safeguards may be recommended but the processing can proceed
    /// once standard mitigations are documented.
    /// </remarks>
    Medium = 1,

    /// <summary>
    /// Processing poses high risk to data subjects' rights and freedoms.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Per Article 35(1), a DPIA is mandatory at this level. The controller must implement
    /// specific mitigations to reduce risk before processing can begin.
    /// </para>
    /// <para>
    /// If risk cannot be mitigated to an acceptable level, prior consultation with the
    /// supervisory authority is required under Article 36(1).
    /// </para>
    /// </remarks>
    High = 2,

    /// <summary>
    /// Processing poses very high risk requiring prior consultation with the supervisory authority.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Per Article 36(1), the controller must consult the supervisory authority prior to
    /// processing when the DPIA indicates that processing would result in a high risk
    /// in the absence of measures taken by the controller to mitigate the risk.
    /// </para>
    /// <para>
    /// This level typically applies when multiple high-risk triggers are present simultaneously
    /// (e.g., systematic profiling combined with automated decision-making affecting vulnerable subjects).
    /// </para>
    /// </remarks>
    VeryHigh = 3
}
