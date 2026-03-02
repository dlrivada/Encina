namespace Encina.Compliance.Retention.Model;

/// <summary>
/// Defines a data retention policy for a specific data category.
/// </summary>
/// <remarks>
/// <para>
/// A retention policy specifies how long data of a given category should be retained
/// and whether automatic deletion is enabled upon expiration. Each policy is linked
/// to a data category (e.g., "financial-records", "session-logs", "marketing-consent")
/// and optionally references the legal basis requiring the retention period.
/// </para>
/// <para>
/// Per GDPR Article 5(1)(e) (storage limitation), personal data shall be kept in a form
/// which permits identification of data subjects for no longer than is necessary for the
/// purposes for which the personal data are processed. Retention policies formalize and
/// enforce this principle by defining explicit retention periods per data category.
/// </para>
/// <para>
/// Recital 39 states that time limits should be established by the controller for erasure
/// or for a periodic review, and appropriate measures should be taken to ensure that
/// personal data are not kept longer than necessary.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var policy = RetentionPolicy.Create(
///     dataCategory: "financial-records",
///     retentionPeriod: TimeSpan.FromDays(365 * 7),
///     autoDelete: true,
///     reason: "German tax law (AO section 147)",
///     legalBasis: "Legal obligation (Art. 6(1)(c))",
///     policyType: RetentionPolicyType.TimeBased);
/// </code>
/// </example>
public sealed record RetentionPolicy
{
    /// <summary>
    /// Unique identifier for this retention policy.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The data category this policy applies to.
    /// </summary>
    /// <remarks>
    /// Examples: "financial-records", "session-logs", "marketing-consent",
    /// "healthcare-records", "employment-records". Each category should have
    /// at most one retention policy.
    /// </remarks>
    public required string DataCategory { get; init; }

    /// <summary>
    /// The duration for which data in this category must be retained.
    /// </summary>
    /// <remarks>
    /// After this period elapses (measured from the retention record's creation date),
    /// the data is considered expired and eligible for deletion. Use helper methods
    /// <see cref="FromDays"/>, <see cref="FromYears"/> for convenient construction.
    /// </remarks>
    public required TimeSpan RetentionPeriod { get; init; }

    /// <summary>
    /// Whether expired data should be automatically deleted by the enforcement service.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, the <c>RetentionEnforcementService</c> will automatically
    /// delete expired data during enforcement cycles. When <c>false</c>, expired data
    /// is flagged but requires manual deletion.
    /// </remarks>
    public required bool AutoDelete { get; init; }

    /// <summary>
    /// Human-readable reason for this retention period.
    /// </summary>
    /// <remarks>
    /// Documents the business or legal justification for the retention period.
    /// Examples: "German tax law (AO section 147)", "UK Limitation Act 1980",
    /// "Internal data governance policy".
    /// </remarks>
    public string? Reason { get; init; }

    /// <summary>
    /// The GDPR lawful basis or legal reference requiring this retention period.
    /// </summary>
    /// <remarks>
    /// References the applicable legal framework. Examples: "Legal obligation (Art. 6(1)(c))",
    /// "Legitimate interest (Art. 6(1)(f))", "Consent (Art. 6(1)(a))".
    /// </remarks>
    public string? LegalBasis { get; init; }

    /// <summary>
    /// The type of trigger mechanism for this retention policy.
    /// </summary>
    public required RetentionPolicyType PolicyType { get; init; }

    /// <summary>
    /// Timestamp when this policy was created (UTC).
    /// </summary>
    public required DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>
    /// Timestamp when this policy was last modified (UTC).
    /// </summary>
    /// <remarks>
    /// <c>null</c> if the policy has never been modified since creation.
    /// </remarks>
    public DateTimeOffset? LastModifiedAtUtc { get; init; }

    /// <summary>
    /// Creates a new retention policy with a generated unique identifier and the current UTC timestamp.
    /// </summary>
    /// <param name="dataCategory">The data category this policy applies to.</param>
    /// <param name="retentionPeriod">The duration for which data must be retained.</param>
    /// <param name="autoDelete">Whether expired data should be automatically deleted.</param>
    /// <param name="reason">Human-readable reason for this retention period.</param>
    /// <param name="legalBasis">The lawful basis or legal reference.</param>
    /// <param name="policyType">The trigger mechanism type.</param>
    /// <returns>A new <see cref="RetentionPolicy"/> with a generated GUID identifier.</returns>
    public static RetentionPolicy Create(
        string dataCategory,
        TimeSpan retentionPeriod,
        bool autoDelete = true,
        string? reason = null,
        string? legalBasis = null,
        RetentionPolicyType policyType = RetentionPolicyType.TimeBased) =>
        new()
        {
            Id = Guid.NewGuid().ToString("N"),
            DataCategory = dataCategory,
            RetentionPeriod = retentionPeriod,
            AutoDelete = autoDelete,
            Reason = reason,
            LegalBasis = legalBasis,
            PolicyType = policyType,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

    /// <summary>
    /// Creates a <see cref="TimeSpan"/> representing the specified number of days.
    /// </summary>
    /// <param name="days">Number of days for the retention period.</param>
    /// <returns>A <see cref="TimeSpan"/> representing the specified days.</returns>
    public static TimeSpan FromDays(int days) => TimeSpan.FromDays(days);

    /// <summary>
    /// Creates a <see cref="TimeSpan"/> representing the specified number of years
    /// (approximated as 365 days per year).
    /// </summary>
    /// <param name="years">Number of years for the retention period.</param>
    /// <returns>A <see cref="TimeSpan"/> representing the specified years.</returns>
    public static TimeSpan FromYears(int years) => TimeSpan.FromDays(years * 365);
}
