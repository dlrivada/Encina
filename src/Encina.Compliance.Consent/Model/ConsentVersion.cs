namespace Encina.Compliance.Consent;

/// <summary>
/// Represents a specific version of consent terms for a processing purpose.
/// </summary>
/// <remarks>
/// <para>
/// Consent versions track changes to the terms and conditions that data subjects agree to.
/// When consent terms are updated (e.g., new data categories added, purpose scope changed),
/// a new version is published and existing consents may require reconsent.
/// </para>
/// <para>
/// This supports GDPR Article 7 requirements by ensuring that consent is always linked
/// to the specific terms the data subject was presented with, and that changes to those
/// terms trigger appropriate reconsent flows.
/// </para>
/// </remarks>
public sealed record ConsentVersion
{
    /// <summary>
    /// Unique identifier for this consent version.
    /// </summary>
    /// <remarks>
    /// Referenced by <see cref="ConsentRecord.ConsentVersionId"/> to link a consent
    /// record to the specific version of terms the data subject agreed to.
    /// </remarks>
    public required string VersionId { get; init; }

    /// <summary>
    /// The processing purpose this version applies to.
    /// </summary>
    /// <remarks>
    /// Must match the <see cref="ConsentRecord.Purpose"/> values.
    /// Use constants from <see cref="ConsentPurposes"/> for standard purposes.
    /// </remarks>
    public required string Purpose { get; init; }

    /// <summary>
    /// Timestamp from which this version of the consent terms is effective (UTC).
    /// </summary>
    /// <remarks>
    /// Consent records created before this date under previous versions may require
    /// reconsent if <see cref="RequiresExplicitReconsent"/> is <c>true</c>.
    /// </remarks>
    public required DateTimeOffset EffectiveFromUtc { get; init; }

    /// <summary>
    /// Human-readable description of what changed in this version.
    /// </summary>
    /// <remarks>
    /// Should clearly describe the consent terms or what changed from the previous version.
    /// This information may be presented to data subjects during reconsent flows.
    /// </remarks>
    /// <example>"Added third-party analytics provider (Mixpanel) to data sharing scope"</example>
    public required string Description { get; init; }

    /// <summary>
    /// Whether existing consents under previous versions must be explicitly renewed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c>, data subjects who consented under a previous version will have their
    /// consent status changed to <see cref="ConsentStatus.RequiresReconsent"/> and must
    /// provide fresh consent under the new terms before processing can continue.
    /// </para>
    /// <para>
    /// When <c>false</c>, existing consents remain valid under the new version (e.g., for
    /// minor clarifications or formatting changes that do not affect the scope of processing).
    /// </para>
    /// </remarks>
    public required bool RequiresExplicitReconsent { get; init; }
}
