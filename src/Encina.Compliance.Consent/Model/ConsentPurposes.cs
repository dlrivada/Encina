namespace Encina.Compliance.Consent;

/// <summary>
/// Standard consent purpose identifiers for common processing activities.
/// </summary>
/// <remarks>
/// <para>
/// These constants provide well-known purpose identifiers that can be used with
/// <see cref="ConsentRecord.Purpose"/> and the <c>[RequireConsent]</c> attribute.
/// Using standard identifiers promotes consistency across applications and
/// simplifies consent management.
/// </para>
/// <para>
/// Applications may define additional custom purpose strings for domain-specific
/// processing activities. Purpose identifiers should be lowercase, hyphen-separated,
/// and descriptive of the processing activity they represent.
/// </para>
/// </remarks>
public static class ConsentPurposes
{
    /// <summary>
    /// Sending promotional emails, SMS, push notifications, and targeted advertisements.
    /// </summary>
    public const string Marketing = "marketing";

    /// <summary>
    /// Collecting and analyzing usage data for product improvement and reporting.
    /// </summary>
    public const string Analytics = "analytics";

    /// <summary>
    /// Tailoring content, recommendations, and user experience based on user behavior and preferences.
    /// </summary>
    public const string Personalization = "personalization";

    /// <summary>
    /// Sharing personal data with third-party organizations for their own processing purposes.
    /// </summary>
    public const string ThirdPartySharing = "third-party-sharing";

    /// <summary>
    /// Automated processing of personal data to evaluate certain personal aspects (Article 22).
    /// </summary>
    /// <remarks>
    /// Profiling includes analyzing or predicting aspects concerning work performance,
    /// economic situation, health, personal preferences, interests, reliability, behavior,
    /// location, or movements.
    /// </remarks>
    public const string Profiling = "profiling";

    /// <summary>
    /// Subscribing to periodic email communications such as newsletters or digests.
    /// </summary>
    public const string Newsletter = "newsletter";

    /// <summary>
    /// Collecting and processing geographic location data from the data subject's device.
    /// </summary>
    public const string LocationTracking = "location-tracking";

    /// <summary>
    /// Transferring personal data to countries outside the European Economic Area (EEA).
    /// </summary>
    /// <remarks>
    /// Cross-border transfers require appropriate safeguards under Articles 44-49,
    /// such as Standard Contractual Clauses (SCCs) or adequacy decisions.
    /// </remarks>
    public const string CrossBorderTransfer = "cross-border-transfer";
}
