namespace Encina.Compliance.GDPR;

/// <summary>
/// Represents the Data Protection Officer (DPO) contact information as required by GDPR Articles 37-39.
/// </summary>
/// <remarks>
/// <para>
/// Under Article 37, certain organizations must designate a DPO. The DPO's contact details
/// must be published and communicated to the supervisory authority (Article 37(7)).
/// </para>
/// <para>
/// The DPO's minimum tasks (Article 39) include:
/// </para>
/// <list type="bullet">
/// <item>Informing and advising on GDPR obligations</item>
/// <item>Monitoring compliance with GDPR and internal policies</item>
/// <item>Providing advice on Data Protection Impact Assessments (DPIA)</item>
/// <item>Cooperating with the supervisory authority</item>
/// <item>Acting as the contact point for the supervisory authority</item>
/// </list>
/// <para>
/// This interface provides the contact information that is included in RoPA exports
/// and made available to data subjects and supervisory authorities.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Configure DPO in GDPR options
/// services.AddEncinaGDPR(options =>
/// {
///     options.DPO = new DataProtectionOfficer("Jane Smith", "dpo@company.com", "+34 600 000 000");
/// });
/// </code>
/// </example>
public interface IDataProtectionOfficer
{
    /// <summary>
    /// Full name of the Data Protection Officer.
    /// </summary>
    /// <remarks>
    /// Required by Article 37(7) to be communicated to the supervisory authority.
    /// </remarks>
    string Name { get; }

    /// <summary>
    /// Email address of the Data Protection Officer.
    /// </summary>
    /// <remarks>
    /// Primary contact channel for data subjects and supervisory authorities.
    /// Required by Article 37(7).
    /// </remarks>
    string Email { get; }

    /// <summary>
    /// Phone number of the Data Protection Officer.
    /// </summary>
    /// <remarks>
    /// Optional additional contact channel. While not strictly required by the GDPR,
    /// it is recommended by most supervisory authorities for accessibility.
    /// </remarks>
    string? Phone { get; }
}
