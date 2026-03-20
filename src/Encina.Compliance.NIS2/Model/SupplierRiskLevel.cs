namespace Encina.Compliance.NIS2.Model;

/// <summary>
/// Risk level assigned to a supplier in the context of NIS2 supply chain security (Art. 21(2)(d)).
/// </summary>
/// <remarks>
/// <para>
/// Per NIS2 Article 21(2)(d), entities must address supply chain security by evaluating
/// the security-related aspects of relationships with direct suppliers and service providers.
/// </para>
/// <para>
/// Per Art. 21(3), when assessing supply chain measures, entities shall take into account:
/// </para>
/// <list type="bullet">
/// <item><description>The vulnerabilities specific to each direct supplier and service provider.</description></item>
/// <item><description>The overall quality of products and cybersecurity practices of suppliers,
/// including their secure development procedures.</description></item>
/// <item><description>The results of coordinated security risk assessments of critical supply chains
/// carried out in accordance with Art. 22(1).</description></item>
/// </list>
/// </remarks>
public enum SupplierRiskLevel
{
    /// <summary>
    /// Low risk — supplier has adequate security controls and certifications.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Medium risk — supplier has basic security controls but may lack certifications or recent assessments.
    /// </summary>
    Medium = 1,

    /// <summary>
    /// High risk — supplier has known security gaps or operates in a high-risk jurisdiction.
    /// </summary>
    /// <remarks>
    /// Operations involving high-risk suppliers should be subject to additional controls
    /// and may be blocked by the <c>NIS2CompliancePipelineBehavior</c> when enforcement
    /// mode is set to <see cref="NIS2EnforcementMode.Block"/>.
    /// </remarks>
    High = 2,

    /// <summary>
    /// Critical risk — supplier has significant security deficiencies or has experienced recent breaches.
    /// </summary>
    /// <remarks>
    /// Critical-risk suppliers should be immediately reviewed for continued engagement.
    /// Operations are blocked by default when enforcement mode is
    /// <see cref="NIS2EnforcementMode.Block"/> or <see cref="NIS2EnforcementMode.Warn"/>.
    /// </remarks>
    Critical = 3
}
