namespace Encina.Compliance.Consent;

/// <summary>
/// Represents a record of consent given by a data subject for a specific processing purpose.
/// </summary>
/// <remarks>
/// <para>
/// Each consent record documents when, how, and for what purpose a data subject gave consent,
/// as required by GDPR Article 7(1) to demonstrate that the data subject has consented to
/// the processing of their personal data.
/// </para>
/// <para>
/// Consent records are immutable once created. Status changes (withdrawal, expiration) are
/// tracked through <see cref="Status"/>, <see cref="WithdrawnAtUtc"/>, and <see cref="ExpiresAtUtc"/>.
/// The full consent history is maintained for audit and demonstrability purposes.
/// </para>
/// <para>
/// The <see cref="ConsentVersionId"/> links this record to the specific version of the consent
/// terms that the data subject agreed to, enabling reconsent tracking when terms change.
/// </para>
/// </remarks>
public sealed record ConsentRecord
{
    /// <summary>
    /// Unique identifier for this consent record.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Identifier of the data subject who gave consent.
    /// </summary>
    /// <remarks>
    /// This should be a stable identifier for the data subject (e.g., user ID, customer number).
    /// It is used to look up all consents for a given individual.
    /// </remarks>
    public required string SubjectId { get; init; }

    /// <summary>
    /// The specific processing purpose for which consent was given.
    /// </summary>
    /// <remarks>
    /// Purposes should be granular and specific as required by Article 6(1)(a).
    /// Use constants from <see cref="ConsentPurposes"/> for standard purposes,
    /// or define custom purpose strings for domain-specific needs.
    /// </remarks>
    /// <example>"marketing", "analytics", "personalization"</example>
    public required string Purpose { get; init; }

    /// <summary>
    /// The current status of this consent record.
    /// </summary>
    public required ConsentStatus Status { get; init; }

    /// <summary>
    /// Identifier of the consent version the data subject agreed to.
    /// </summary>
    /// <remarks>
    /// Links this consent to a specific <see cref="ConsentVersion"/>, enabling
    /// detection of when consent terms change and reconsent is required.
    /// </remarks>
    public required string ConsentVersionId { get; init; }

    /// <summary>
    /// Timestamp when the data subject gave consent (UTC).
    /// </summary>
    public required DateTimeOffset GivenAtUtc { get; init; }

    /// <summary>
    /// Timestamp when the data subject withdrew consent (UTC).
    /// </summary>
    /// <remarks>
    /// <c>null</c> if consent has not been withdrawn. Set when <see cref="Status"/>
    /// transitions to <see cref="ConsentStatus.Withdrawn"/>.
    /// </remarks>
    public DateTimeOffset? WithdrawnAtUtc { get; init; }

    /// <summary>
    /// Timestamp when this consent expires (UTC).
    /// </summary>
    /// <remarks>
    /// <c>null</c> if no expiration is set. When the current time exceeds this value,
    /// the consent should be treated as <see cref="ConsentStatus.Expired"/>.
    /// </remarks>
    public DateTimeOffset? ExpiresAtUtc { get; init; }

    /// <summary>
    /// The source or channel through which consent was collected.
    /// </summary>
    /// <example>"web-form", "api", "mobile-app", "in-person", "email"</example>
    public required string Source { get; init; }

    /// <summary>
    /// The IP address of the data subject at the time consent was given.
    /// </summary>
    /// <remarks>
    /// Optional. May be <c>null</c> when consent is collected through channels
    /// where IP address is not available or not applicable (e.g., in-person, phone).
    /// When available, it provides additional evidence for demonstrating consent.
    /// </remarks>
    public string? IpAddress { get; init; }

    /// <summary>
    /// Hash or reference to the consent form shown to the data subject.
    /// </summary>
    /// <remarks>
    /// Optional proof of what the data subject was presented with when they gave consent.
    /// This could be a hash of the consent text, a reference to a stored consent form version,
    /// or a URL to the consent page.
    /// </remarks>
    public string? ProofOfConsent { get; init; }

    /// <summary>
    /// Additional metadata associated with this consent record.
    /// </summary>
    /// <remarks>
    /// Extensible key-value pairs for storing domain-specific information such as
    /// the user agent, consent form version, A/B test variant, or any other contextual
    /// data relevant to the consent collection.
    /// </remarks>
    public required IReadOnlyDictionary<string, object?> Metadata { get; init; }
}
