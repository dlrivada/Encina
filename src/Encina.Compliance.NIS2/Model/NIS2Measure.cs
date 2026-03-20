namespace Encina.Compliance.NIS2.Model;

/// <summary>
/// The 10 mandatory cybersecurity risk-management measures required by Article 21(2)
/// of the NIS2 Directive (EU 2022/2555).
/// </summary>
/// <remarks>
/// <para>
/// Per Art. 21(1), essential and important entities must take appropriate and proportionate
/// technical, operational, and organisational measures to manage the risks posed to the
/// security of network and information systems used for their operations or services.
/// These measures shall be based on an all-hazards approach and shall include at least
/// the following 10 elements (Art. 21(2)).
/// </para>
/// <para>
/// The <c>INIS2MeasureEvaluator</c> interface provides pluggable evaluation
/// of each measure. Use <c>INIS2ComplianceValidator</c> to assess the overall
/// compliance posture across all 10 measures.
/// </para>
/// </remarks>
public enum NIS2Measure
{
    /// <summary>
    /// Policies on risk analysis and information system security.
    /// </summary>
    /// <remarks>
    /// Art. 21(2)(a). Entities must establish and maintain policies for risk analysis
    /// and information system security, including risk assessment methodologies,
    /// risk treatment plans, and regular security policy reviews.
    /// </remarks>
    RiskAnalysisAndSecurityPolicies = 0,

    /// <summary>
    /// Incident handling.
    /// </summary>
    /// <remarks>
    /// Art. 21(2)(b). Entities must implement incident detection, reporting, and response
    /// procedures. See also Art. 23 for the mandatory notification timeline
    /// (24h early warning, 72h notification, 1-month final report).
    /// </remarks>
    IncidentHandling = 1,

    /// <summary>
    /// Business continuity, such as backup management and disaster recovery, and crisis management.
    /// </summary>
    /// <remarks>
    /// Art. 21(2)(c). Entities must ensure continuity of critical services through
    /// backup strategies, disaster recovery plans, and crisis management procedures.
    /// </remarks>
    BusinessContinuity = 2,

    /// <summary>
    /// Supply chain security, including security-related aspects concerning relationships
    /// between each entity and its direct suppliers or service providers.
    /// </summary>
    /// <remarks>
    /// Art. 21(2)(d). Entities must assess and manage risks from their supply chain,
    /// including supplier risk assessments, contractual security requirements,
    /// and ongoing monitoring of supplier security posture.
    /// </remarks>
    SupplyChainSecurity = 3,

    /// <summary>
    /// Security in network and information systems acquisition, development, and maintenance,
    /// including vulnerability handling and disclosure.
    /// </summary>
    /// <remarks>
    /// Art. 21(2)(e). Entities must integrate security into the SDLC, implement
    /// vulnerability management processes, and support coordinated vulnerability
    /// disclosure (CVD).
    /// </remarks>
    NetworkAndSystemSecurity = 4,

    /// <summary>
    /// Policies and procedures to assess the effectiveness of cybersecurity
    /// risk-management measures.
    /// </summary>
    /// <remarks>
    /// Art. 21(2)(f). Entities must regularly test and evaluate the effectiveness
    /// of their cybersecurity measures through audits, penetration tests,
    /// and security assessments.
    /// </remarks>
    EffectivenessAssessment = 5,

    /// <summary>
    /// Basic cyber hygiene practices and cybersecurity training.
    /// </summary>
    /// <remarks>
    /// Art. 21(2)(g). Entities must implement cyber hygiene practices (patching,
    /// password policies, secure configuration) and provide regular cybersecurity
    /// awareness training to all staff, including management bodies (Art. 20(2)).
    /// </remarks>
    CyberHygiene = 6,

    /// <summary>
    /// Policies and procedures regarding the use of cryptography and, where appropriate, encryption.
    /// </summary>
    /// <remarks>
    /// Art. 21(2)(h). Entities must define cryptographic policies covering data at rest,
    /// data in transit, key management, and approved cryptographic algorithms.
    /// </remarks>
    Cryptography = 7,

    /// <summary>
    /// Human resources security, access control policies, and asset management.
    /// </summary>
    /// <remarks>
    /// Art. 21(2)(i). Entities must implement HR security measures (background checks,
    /// security clearances), access control policies (least privilege, role-based access),
    /// and asset management (inventory, classification, lifecycle).
    /// </remarks>
    HumanResourcesSecurity = 8,

    /// <summary>
    /// The use of multi-factor authentication or continuous authentication solutions,
    /// secured voice, video, and text communications, and secured emergency communication
    /// systems within the entity, where appropriate.
    /// </summary>
    /// <remarks>
    /// Art. 21(2)(j). Entities must deploy MFA or continuous authentication for
    /// critical systems and administrative access. Secured communications channels
    /// must be available for emergency situations.
    /// </remarks>
    MultiFactorAuthentication = 9
}
