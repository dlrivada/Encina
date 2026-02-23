namespace Encina.Compliance.Consent;

/// <summary>
/// Represents the type of action recorded in the consent audit trail.
/// </summary>
/// <remarks>
/// Audit actions track every significant change to a consent record, supporting
/// GDPR Article 7(1) which requires controllers to be able to demonstrate that
/// the data subject has consented to the processing of their personal data.
/// </remarks>
public enum ConsentAuditAction
{
    /// <summary>
    /// The data subject granted consent for a processing purpose.
    /// </summary>
    Granted,

    /// <summary>
    /// The data subject withdrew their previously given consent.
    /// </summary>
    /// <remarks>Article 7(3). Withdrawal must be as easy as giving consent.</remarks>
    Withdrawn,

    /// <summary>
    /// The consent expired due to reaching its expiration date.
    /// </summary>
    Expired,

    /// <summary>
    /// The consent terms changed and the data subject's consent status was
    /// updated to require reconsent under the new version.
    /// </summary>
    VersionChanged
}
