namespace Encina.Compliance.Retention;

/// <summary>
/// Specifies the retention period for data decorated with this attribute.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="RetentionPeriodAttribute"/> enables declarative retention period configuration
/// on response types or properties. When applied, the <c>RetentionValidationPipelineBehavior</c>
/// automatically creates <see cref="ReadModels.RetentionRecordReadModel"/> entries for data entities
/// returned from command handlers, tracking their creation and expiration dates.
/// </para>
/// <para>
/// Per GDPR Article 5(1)(e) (storage limitation), personal data shall be kept for no longer
/// than is necessary for the purposes for which it is processed. This attribute formalizes
/// retention requirements directly in the domain model, ensuring that retention periods
/// are discoverable and enforceable at the code level.
/// </para>
/// <para>
/// <see cref="Days"/> and <see cref="Years"/> are mutually exclusive. Set one or the other,
/// but not both. The computed <see cref="RetentionPeriod"/> property resolves the appropriate
/// <see cref="TimeSpan"/> based on whichever is set.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Apply to a class — all instances retain for 7 years
/// [RetentionPeriod(Years = 7, DataCategory = "financial-records",
///     Reason = "German tax law (AO section 147)")]
/// public sealed record Invoice(string Id, decimal Amount, DateTimeOffset CreatedAtUtc);
///
/// // Apply to a property — specific field retention
/// public sealed record CustomerProfile
/// {
///     [RetentionPeriod(Days = 365, DataCategory = "marketing-consent",
///         Reason = "Consent validity period", AutoDelete = true)]
///     public string? MarketingPreferences { get; init; }
/// }
///
/// // Minimal usage with days only
/// [RetentionPeriod(Days = 90)]
/// public sealed record SessionLog(string SessionId, DateTimeOffset StartedAtUtc);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class RetentionPeriodAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the retention period in days.
    /// </summary>
    /// <remarks>
    /// Mutually exclusive with <see cref="Years"/>. Set one or the other, but not both.
    /// When set, the <see cref="RetentionPeriod"/> is computed as <c>TimeSpan.FromDays(Days)</c>.
    /// </remarks>
    public int Days { get; set; }

    /// <summary>
    /// Gets or sets the retention period in years.
    /// </summary>
    /// <remarks>
    /// Mutually exclusive with <see cref="Days"/>. Set one or the other, but not both.
    /// When set, the <see cref="RetentionPeriod"/> is computed as <c>TimeSpan.FromDays(Years * 365)</c>.
    /// A year is approximated as 365 days for consistency.
    /// </remarks>
    public int Years { get; set; }

    /// <summary>
    /// Gets or sets the reason for the retention period.
    /// </summary>
    /// <remarks>
    /// Documents the legal or business justification for the retention period.
    /// For example: "German tax law (AO section 147)" or "Consent validity period".
    /// Per GDPR Article 5(2) (accountability principle), controllers should document
    /// the rationale behind retention decisions.
    /// </remarks>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether data should be automatically deleted
    /// when the retention period expires.
    /// </summary>
    /// <remarks>
    /// When <c>true</c> (default), the <c>RetentionEnforcementService</c> will automatically
    /// delete the data after the retention period expires. When <c>false</c>, the data
    /// will be flagged as expired but not automatically deleted, requiring manual action.
    /// </remarks>
    public bool AutoDelete { get; set; } = true;

    /// <summary>
    /// Gets or sets the data category for retention policy resolution.
    /// </summary>
    /// <remarks>
    /// Maps this data to a specific retention policy category. When set, the pipeline
    /// behavior uses this value to look up the <see cref="ReadModels.RetentionPolicyReadModel"/>
    /// via <see cref="Abstractions.IRetentionPolicyService.GetPolicyByCategoryAsync"/>. If not set,
    /// the pipeline behavior derives the category from the type name.
    /// </remarks>
    public string? DataCategory { get; set; }

    /// <summary>
    /// Gets the computed retention period as a <see cref="TimeSpan"/>.
    /// </summary>
    /// <value>
    /// The retention period resolved from <see cref="Days"/> or <see cref="Years"/>,
    /// or <see cref="TimeSpan.Zero"/> if neither is set.
    /// </value>
    /// <remarks>
    /// <para>
    /// If both <see cref="Days"/> and <see cref="Years"/> are set (which is invalid),
    /// <see cref="Days"/> takes precedence. If neither is set, returns <see cref="TimeSpan.Zero"/>.
    /// </para>
    /// <para>
    /// Validation of mutual exclusivity should be performed at configuration time
    /// by the pipeline behavior or policy validation logic.
    /// </para>
    /// </remarks>
    public TimeSpan RetentionPeriod
    {
        get
        {
            if (Days > 0)
                return TimeSpan.FromDays(Days);

            if (Years > 0)
                return TimeSpan.FromDays(Years * 365);

            return TimeSpan.Zero;
        }
    }
}
