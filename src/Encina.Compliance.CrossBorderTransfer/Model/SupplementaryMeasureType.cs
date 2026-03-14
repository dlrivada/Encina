namespace Encina.Compliance.CrossBorderTransfer.Model;

/// <summary>
/// Categorizes supplementary measures that may be required for international data transfers
/// following the Schrems II judgment (CJEU C-311/18).
/// </summary>
/// <remarks>
/// <para>
/// The European Data Protection Board (EDPB) Recommendations 01/2020 on supplementary measures
/// identify three categories of measures that data exporters can implement to ensure
/// "essentially equivalent" protection when transferring personal data outside the EU/EEA.
/// </para>
/// <para>
/// A Transfer Impact Assessment (TIA) evaluates whether the legal framework of the
/// destination country provides adequate protection, and determines which supplementary
/// measures are necessary to bridge any identified gaps.
/// </para>
/// </remarks>
public enum SupplementaryMeasureType
{
    /// <summary>
    /// Technical measures that prevent access to personal data by unauthorized parties.
    /// </summary>
    /// <remarks>
    /// Examples include end-to-end encryption, pseudonymization, split processing,
    /// and transport layer security. Technical measures are generally the most effective
    /// as they do not rely on compliance by third parties or legal enforcement.
    /// </remarks>
    Technical = 0,

    /// <summary>
    /// Contractual measures that supplement the Standard Contractual Clauses.
    /// </summary>
    /// <remarks>
    /// Examples include obligations to challenge government access requests, transparency
    /// reporting commitments, data subject notification requirements, and audit rights.
    /// Contractual measures bind the data importer but their effectiveness depends on
    /// enforceability in the destination country's legal system.
    /// </remarks>
    Contractual = 1,

    /// <summary>
    /// Organizational measures that establish internal policies and procedures.
    /// </summary>
    /// <remarks>
    /// Examples include internal access control policies, data minimization procedures,
    /// staff training on government access requests, security certifications (ISO 27001),
    /// and appointment of a Data Protection Officer. Organizational measures complement
    /// technical and contractual measures.
    /// </remarks>
    Organizational = 2
}
