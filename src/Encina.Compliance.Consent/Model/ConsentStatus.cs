namespace Encina.Compliance.Consent;

/// <summary>
/// Represents the current state of a consent record.
/// </summary>
/// <remarks>
/// <para>
/// Consent status tracks the lifecycle of a data subject's consent from when it is
/// actively given through withdrawal, expiration, or the need for reconsent.
/// </para>
/// <para>
/// Status transitions follow GDPR Article 7 requirements: consent can be withdrawn
/// at any time, and must be as easy to withdraw as to give. Expired consent must
/// not be treated as valid authorization for processing.
/// </para>
/// </remarks>
public enum ConsentStatus
{
    /// <summary>
    /// The data subject has actively given consent and it remains valid.
    /// </summary>
    /// <remarks>
    /// Article 6(1)(a). The consent is current, has not been withdrawn,
    /// and has not passed its expiration date.
    /// </remarks>
    Active,

    /// <summary>
    /// The data subject has exercised their right to withdraw consent.
    /// </summary>
    /// <remarks>
    /// Article 7(3). Withdrawal must be as easy as giving consent.
    /// Processing based on this consent must cease upon withdrawal.
    /// </remarks>
    Withdrawn,

    /// <summary>
    /// The consent has passed its expiration date and is no longer valid.
    /// </summary>
    /// <remarks>
    /// Consent should not be treated as lasting indefinitely. Expired consent
    /// requires the data subject to provide fresh consent before processing can resume.
    /// </remarks>
    Expired,

    /// <summary>
    /// The consent terms or purpose have changed and the data subject must provide fresh consent.
    /// </summary>
    /// <remarks>
    /// When processing purposes change or consent terms are updated, existing consent
    /// may no longer cover the new conditions. The data subject must be informed and
    /// asked to provide consent under the updated terms.
    /// </remarks>
    RequiresReconsent
}
