namespace Encina.Compliance.AIAct.Model;

/// <summary>
/// Risk levels for AI systems as defined by the EU AI Act risk-based classification framework.
/// </summary>
/// <remarks>
/// <para>
/// The EU AI Act (Regulation EU 2024/1689) classifies AI systems into four risk tiers,
/// each with different regulatory obligations. The classification determines which
/// requirements from Title III (Arts. 8-15) apply.
/// </para>
/// <para>
/// Risk classification is primarily determined by the system's intended use and the
/// sector in which it operates (Art. 6 and Annex III). The <c>IAIActClassifier</c>
/// resolves the appropriate level for a given AI system.
/// </para>
/// </remarks>
public enum AIRiskLevel
{
    /// <summary>
    /// Unacceptable risk — the AI system is prohibited from being placed on the market or put into service.
    /// </summary>
    /// <remarks>
    /// Art. 5. Includes social scoring by public authorities, real-time remote biometric
    /// identification in publicly accessible spaces (with narrow exceptions), subliminal
    /// manipulation, exploitation of vulnerabilities, and untargeted facial image scraping.
    /// </remarks>
    Prohibited = 0,

    /// <summary>
    /// High risk — the AI system is subject to strict requirements before market placement.
    /// </summary>
    /// <remarks>
    /// Art. 6 and Annex III. High-risk systems must comply with requirements for risk management (Art. 9),
    /// data governance (Art. 10), technical documentation (Art. 11), record-keeping (Art. 12),
    /// transparency (Art. 13), human oversight (Art. 14), and accuracy/robustness (Art. 15).
    /// Examples: biometric identification, critical infrastructure management, employment screening,
    /// credit scoring, law enforcement, and justice administration.
    /// </remarks>
    HighRisk = 1,

    /// <summary>
    /// Limited risk — the AI system has transparency obligations only.
    /// </summary>
    /// <remarks>
    /// Art. 50. Systems that interact with natural persons (chatbots), generate synthetic content
    /// (deepfakes, AI-generated text/images), perform emotion recognition, or carry out biometric
    /// categorisation must disclose that fact to the user. No further compliance requirements apply.
    /// </remarks>
    LimitedRisk = 2,

    /// <summary>
    /// Minimal risk — the AI system has no specific regulatory requirements under the AI Act.
    /// </summary>
    /// <remarks>
    /// No specific article. AI systems that do not fall into the prohibited, high-risk, or
    /// limited-risk categories are considered minimal risk. Examples: AI-enabled video games,
    /// spam filters, and inventory management systems. Voluntary codes of conduct may apply (Art. 95).
    /// </remarks>
    MinimalRisk = 3
}
