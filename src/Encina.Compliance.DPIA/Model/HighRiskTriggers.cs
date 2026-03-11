namespace Encina.Compliance.DPIA.Model;

/// <summary>
/// Well-known high-risk triggers that indicate a DPIA is likely required.
/// </summary>
/// <remarks>
/// <para>
/// GDPR Article 35(3) provides a non-exhaustive list of processing operations that require
/// a DPIA. The Article 29 Working Party (now EDPB) further elaborated in WP 248 rev.01
/// that the presence of two or more of these criteria generally triggers the need for a DPIA.
/// </para>
/// <para>
/// These constants are defined as strings rather than an enum to support extensibility:
/// organizations can define custom triggers specific to their processing activities
/// without modifying the Encina library. Custom triggers can be combined with these
/// well-known values in the <see cref="DPIAContext.HighRiskTriggers"/> collection.
/// </para>
/// <para>
/// The triggers align with the nine criteria identified by the EDPB:
/// evaluation/scoring, automated decision-making with legal effect, systematic monitoring,
/// sensitive data or data of a highly personal nature, data processed on a large scale,
/// matching or combining datasets, data concerning vulnerable subjects, innovative use of
/// technology, and transfer across borders.
/// </para>
/// </remarks>
public static class HighRiskTriggers
{
    /// <summary>
    /// Processing of biometric data for the purpose of uniquely identifying a natural person.
    /// </summary>
    /// <remarks>
    /// Biometric data is classified as a special category under Article 9(1).
    /// Processing requires explicit consent or another Article 9(2) legal basis.
    /// </remarks>
    public const string BiometricData = "BiometricData";

    /// <summary>
    /// Processing of data concerning health, including physical or mental health conditions.
    /// </summary>
    /// <remarks>
    /// Health data is classified as a special category under Article 9(1).
    /// Commonly triggers DPIA requirements in healthcare, insurance, and employee wellness contexts.
    /// </remarks>
    public const string HealthData = "HealthData";

    /// <summary>
    /// Automated individual decision-making, including profiling, which produces legal
    /// effects or similarly significant effects on the data subject.
    /// </summary>
    /// <remarks>
    /// Per Article 35(3)(a), a DPIA is required for "a systematic and extensive evaluation
    /// of personal aspects relating to natural persons which is based on automated processing,
    /// including profiling, and on which decisions are based that produce legal effects."
    /// See also Article 22 (automated individual decision-making).
    /// </remarks>
    public const string AutomatedDecisionMaking = "AutomatedDecisionMaking";

    /// <summary>
    /// Systematic and extensive evaluation of personal aspects based on automated processing,
    /// including profiling.
    /// </summary>
    /// <remarks>
    /// Explicitly referenced in Article 35(3)(a). Includes credit scoring, behavioral
    /// advertising, and algorithmic content personalization.
    /// </remarks>
    public const string SystematicProfiling = "SystematicProfiling";

    /// <summary>
    /// Systematic monitoring of a publicly accessible area on a large scale.
    /// </summary>
    /// <remarks>
    /// Per Article 35(3)(c), a DPIA is required for systematic monitoring of publicly
    /// accessible areas. Includes CCTV, Wi-Fi tracking, and location-based analytics.
    /// </remarks>
    public const string PublicMonitoring = "PublicMonitoring";

    /// <summary>
    /// Processing of special categories of data as defined in Article 9(1).
    /// </summary>
    /// <remarks>
    /// Includes racial/ethnic origin, political opinions, religious/philosophical beliefs,
    /// trade union membership, genetic data, biometric data, health data, sex life,
    /// and sexual orientation. Also covers criminal convictions under Article 10.
    /// </remarks>
    public const string SpecialCategoryData = "SpecialCategoryData";

    /// <summary>
    /// Processing of personal data on a large scale.
    /// </summary>
    /// <remarks>
    /// Per Article 35(3)(b), large-scale processing of special categories or criminal
    /// conviction data requires a DPIA. The EDPB considers factors such as the number
    /// of data subjects, volume of data, duration of processing, and geographical extent.
    /// </remarks>
    public const string LargeScaleProcessing = "LargeScaleProcessing";

    /// <summary>
    /// Processing of data concerning vulnerable subjects (children, employees, patients, elderly).
    /// </summary>
    /// <remarks>
    /// Vulnerable data subjects may be unable to freely consent or oppose processing.
    /// Recital 75 specifically mentions children and the EDPB identifies this as a
    /// high-risk criterion in WP 248 rev.01.
    /// </remarks>
    public const string VulnerableSubjects = "VulnerableSubjects";

    /// <summary>
    /// Innovative use or application of new technological or organizational solutions.
    /// </summary>
    /// <remarks>
    /// Novel technologies (AI/ML, IoT, blockchain for personal data) may involve new forms
    /// of data collection and usage with unknown risks. The EDPB identifies this as a
    /// high-risk criterion that often combines with other triggers.
    /// </remarks>
    public const string NovelTechnology = "NovelTechnology";

    /// <summary>
    /// Transfer of personal data across borders, particularly outside the EEA.
    /// </summary>
    /// <remarks>
    /// Chapter V of the GDPR governs international transfers. Cross-border transfers
    /// increase risk due to varying legal frameworks and enforcement capabilities.
    /// This trigger is particularly relevant post-Schrems II for transfers to
    /// countries without adequacy decisions.
    /// </remarks>
    public const string CrossBorderTransfer = "CrossBorderTransfer";
}
