namespace Encina.Compliance.DataResidency.Model;

/// <summary>
/// Classification of the data protection level provided by a jurisdiction.
/// </summary>
/// <remarks>
/// <para>
/// Data protection levels are used to assess whether a region provides adequate
/// safeguards for personal data processing. The classification influences cross-border
/// transfer decisions — data may flow freely to regions with <see cref="High"/> protection,
/// while transfers to <see cref="Low"/> or <see cref="Unknown"/> regions require additional
/// legal bases such as Standard Contractual Clauses (Art. 46) or Binding Corporate Rules (Art. 47).
/// </para>
/// <para>
/// Per GDPR Article 45, the European Commission assesses the adequacy of protection
/// in third countries based on rule of law, data protection legislation, independent
/// supervisory authorities, and international commitments.
/// </para>
/// </remarks>
public enum DataProtectionLevel
{
    /// <summary>
    /// The jurisdiction provides a high level of data protection.
    /// </summary>
    /// <remarks>
    /// Typically applies to EU/EEA member states and countries with an adequacy decision
    /// from the European Commission (Art. 45). Data can flow freely to these regions.
    /// </remarks>
    High = 0,

    /// <summary>
    /// The jurisdiction provides a medium level of data protection.
    /// </summary>
    /// <remarks>
    /// Applies to countries with partial data protection frameworks or sector-specific
    /// adequacy (e.g., Canada's PIPEDA for commercial organizations). Transfers may
    /// require supplementary measures.
    /// </remarks>
    Medium = 1,

    /// <summary>
    /// The jurisdiction provides a low level of data protection.
    /// </summary>
    /// <remarks>
    /// Applies to countries without comprehensive data protection legislation or
    /// with significant surveillance practices. Transfers require strong legal bases
    /// and supplementary technical measures (e.g., encryption, pseudonymization).
    /// </remarks>
    Low = 2,

    /// <summary>
    /// The data protection level of the jurisdiction is unknown or not assessed.
    /// </summary>
    /// <remarks>
    /// Default value for regions that have not been evaluated. Treated as high-risk
    /// for transfer validation purposes — the most restrictive safeguards apply.
    /// </remarks>
    Unknown = 3
}
