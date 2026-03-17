using System.Reflection;

namespace Encina.Compliance.Retention;

/// <summary>
/// Configuration options for the Retention module.
/// </summary>
/// <remarks>
/// <para>
/// These options control the behavior of the retention enforcement service,
/// including default retention periods, enforcement scheduling, and notification
/// preferences. All options have sensible defaults aligned with GDPR
/// Article 5(1)(e) storage limitation requirements.
/// </para>
/// <para>
/// Register via <c>AddEncinaRetention(options => { ... })</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaRetention(options =>
/// {
///     options.DefaultRetentionPeriod = TimeSpan.FromDays(365);
///     options.AlertBeforeExpirationDays = 30;
///     options.PublishNotifications = true;
/// });
/// </code>
/// </example>
public sealed class RetentionOptions
{
    /// <summary>
    /// Gets or sets the default retention period applied when no category-specific policy exists.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When a data entity's category has no explicit <see cref="Aggregates.RetentionPolicyAggregate"/>,
    /// this default period is used as a fallback. If <c>null</c>, the system requires
    /// an explicit policy for every data category and returns an error when none is found.
    /// </para>
    /// <para>
    /// Per GDPR Article 5(1)(e), controllers should establish explicit retention periods.
    /// Using a default is a convenience for development and testing; production systems
    /// should define category-specific policies.
    /// </para>
    /// </remarks>
    public TimeSpan? DefaultRetentionPeriod { get; set; }

    /// <summary>
    /// Gets or sets the number of days before expiration to generate alerts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When the retention enforcer identifies records expiring within this window,
    /// it publishes <see cref="DataExpiringNotification"/> events to allow controllers
    /// to review upcoming deletions.
    /// </para>
    /// <para>
    /// Default is <c>30</c> days.
    /// </para>
    /// </remarks>
    public int AlertBeforeExpirationDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether to publish domain notifications for retention lifecycle events.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c>, the retention enforcer and legal hold manager publish notifications
    /// at key lifecycle points (e.g., data deleted, legal hold applied, enforcement completed).
    /// These can be used for audit logging, event sourcing, or integration with external systems.
    /// </para>
    /// <para>
    /// Default is <c>true</c>.
    /// </para>
    /// </remarks>
    public bool PublishNotifications { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to register a retention health check with <c>IHealthChecksBuilder</c>.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, a health check is registered that verifies the retention stores
    /// are reachable and the enforcement service is operational.
    /// Default is <c>false</c>.
    /// </remarks>
    public bool AddHealthCheck { get; set; }

    /// <summary>
    /// Gets or sets whether automatic retention enforcement is enabled via the hosted service.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c>, the <c>RetentionEnforcementService</c> background service runs
    /// enforcement cycles at the interval specified by <see cref="EnforcementInterval"/>.
    /// When <c>false</c>, enforcement must be triggered manually via
    /// the <see cref="RetentionEnforcementService"/>.
    /// </para>
    /// <para>
    /// Default is <c>true</c>. Disable this for applications that manage enforcement
    /// externally (e.g., via Hangfire, Quartz, or manual triggers).
    /// </para>
    /// </remarks>
    public bool EnableAutomaticEnforcement { get; set; } = true;

    /// <summary>
    /// Gets or sets the interval between automatic retention enforcement cycles.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Controls how frequently the <c>RetentionEnforcementService</c> evaluates expired
    /// records and triggers deletion. Only applies when <see cref="EnableAutomaticEnforcement"/>
    /// is <c>true</c>.
    /// </para>
    /// <para>
    /// Default is <c>60 minutes</c>. Shorter intervals increase enforcement responsiveness
    /// but may add database load. Per Recital 39, the controller should establish appropriate
    /// time limits for erasure or periodic review.
    /// </para>
    /// </remarks>
    public TimeSpan EnforcementInterval { get; set; } = TimeSpan.FromMinutes(60);

    /// <summary>
    /// Gets or sets the enforcement mode for the retention pipeline behavior.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Controls how the <c>RetentionValidationPipelineBehavior</c> responds when
    /// retention record creation fails or when no matching retention policy exists:
    /// <list type="bullet">
    /// <item><description><see cref="RetentionEnforcementMode.Block"/>: Returns an error, blocking the response.</description></item>
    /// <item><description><see cref="RetentionEnforcementMode.Warn"/>: Logs a warning but allows the response through.</description></item>
    /// <item><description><see cref="RetentionEnforcementMode.Disabled"/>: Skips all retention tracking entirely.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Default is <see cref="RetentionEnforcementMode.Warn"/> to support gradual adoption.
    /// Production systems should use <see cref="RetentionEnforcementMode.Block"/>.
    /// </para>
    /// </remarks>
    public RetentionEnforcementMode EnforcementMode { get; set; } = RetentionEnforcementMode.Warn;

    // --- Auto-Registration ---

    /// <summary>
    /// Gets or sets whether to automatically scan assemblies for <see cref="RetentionPeriodAttribute"/>
    /// decorations at startup and create matching <see cref="Aggregates.RetentionPolicyAggregate"/> entries.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c>, the <c>RetentionAutoRegistrationHostedService</c> scans the assemblies
    /// in <see cref="AssembliesToScan"/> for types and properties decorated with
    /// <see cref="RetentionPeriodAttribute"/>. For each discovered data category without an
    /// existing policy, a new <see cref="Aggregates.RetentionPolicyAggregate"/> is created in the store.
    /// </para>
    /// <para>
    /// Default is <c>true</c>.
    /// </para>
    /// </remarks>
    public bool AutoRegisterFromAttributes { get; set; } = true;

    /// <summary>
    /// Gets the assemblies to scan for <see cref="RetentionPeriodAttribute"/> decorations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only used when <see cref="AutoRegisterFromAttributes"/> is <c>true</c>.
    /// Add assemblies that contain your types with properties or classes decorated
    /// with <see cref="RetentionPeriodAttribute"/>.
    /// </para>
    /// <para>
    /// If empty and <see cref="AutoRegisterFromAttributes"/> is <c>true</c>,
    /// the entry assembly is used as a fallback.
    /// </para>
    /// </remarks>
    public List<Assembly> AssembliesToScan { get; } = [];

    // --- Fluent Policy Configuration ---

    /// <summary>
    /// Gets the retention policies configured via the fluent API.
    /// </summary>
    /// <remarks>
    /// Policies added via <see cref="AddPolicy"/> are created in the store at startup
    /// alongside attribute-discovered policies.
    /// </remarks>
    internal List<RetentionPolicyDescriptor> ConfiguredPolicies { get; } = [];

    /// <summary>
    /// Adds a retention policy for a specific data category using the fluent builder API.
    /// </summary>
    /// <param name="dataCategory">The data category to create a policy for.</param>
    /// <param name="configure">An action to configure the policy via <see cref="RetentionPolicyBuilder"/>.</param>
    /// <returns>This options instance for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Provides a fluent alternative to attribute-based policy declaration.
    /// Policies configured here are created in the <see cref="Abstractions.IRetentionPolicyService"/>
    /// at startup if a policy for the same category doesn't already exist.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaRetention(options =>
    /// {
    ///     options.AddPolicy("user-profiles", policy =>
    ///     {
    ///         policy.RetainForDays(365);
    ///         policy.WithAutoDelete();
    ///         policy.WithReason("GDPR Article 5(1)(e) - storage limitation");
    ///     });
    ///
    ///     options.AddPolicy("audit-logs", policy =>
    ///     {
    ///         policy.RetainForYears(7);
    ///         policy.WithAutoDelete(false);
    ///         policy.WithLegalBasis("Legal retention requirement");
    ///     });
    /// });
    /// </code>
    /// </example>
    public RetentionOptions AddPolicy(string dataCategory, Action<RetentionPolicyBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(dataCategory);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new RetentionPolicyBuilder(dataCategory);
        configure(builder);
        ConfiguredPolicies.Add(builder.Build());

        return this;
    }
}

/// <summary>
/// Fluent builder for configuring retention policies within <see cref="RetentionOptions.AddPolicy"/>.
/// </summary>
public sealed class RetentionPolicyBuilder
{
    private readonly string _dataCategory;
    private TimeSpan _retentionPeriod = TimeSpan.FromDays(365);
    private bool _autoDelete = true;
    private string? _reason;
    private string? _legalBasis;

    internal RetentionPolicyBuilder(string dataCategory)
    {
        _dataCategory = dataCategory;
    }

    /// <summary>
    /// Sets the retention period in days.
    /// </summary>
    /// <param name="days">The number of days to retain data.</param>
    /// <returns>This builder for chaining.</returns>
    public RetentionPolicyBuilder RetainForDays(int days)
    {
        _retentionPeriod = TimeSpan.FromDays(days);
        return this;
    }

    /// <summary>
    /// Sets the retention period in years.
    /// </summary>
    /// <param name="years">The number of years to retain data.</param>
    /// <returns>This builder for chaining.</returns>
    public RetentionPolicyBuilder RetainForYears(int years)
    {
        _retentionPeriod = TimeSpan.FromDays(years * 365);
        return this;
    }

    /// <summary>
    /// Sets the retention period to a custom <see cref="TimeSpan"/>.
    /// </summary>
    /// <param name="period">The retention period.</param>
    /// <returns>This builder for chaining.</returns>
    public RetentionPolicyBuilder RetainFor(TimeSpan period)
    {
        _retentionPeriod = period;
        return this;
    }

    /// <summary>
    /// Enables or disables automatic deletion when the retention period expires.
    /// </summary>
    /// <param name="autoDelete">Whether to enable auto-delete. Default is <c>true</c>.</param>
    /// <returns>This builder for chaining.</returns>
    public RetentionPolicyBuilder WithAutoDelete(bool autoDelete = true)
    {
        _autoDelete = autoDelete;
        return this;
    }

    /// <summary>
    /// Sets the reason for the retention policy.
    /// </summary>
    /// <param name="reason">A description of why this retention period was chosen.</param>
    /// <returns>This builder for chaining.</returns>
    public RetentionPolicyBuilder WithReason(string reason)
    {
        _reason = reason;
        return this;
    }

    /// <summary>
    /// Sets the legal basis for the retention policy.
    /// </summary>
    /// <param name="legalBasis">The legal basis for data retention (e.g., "GDPR Article 5(1)(e)").</param>
    /// <returns>This builder for chaining.</returns>
    public RetentionPolicyBuilder WithLegalBasis(string legalBasis)
    {
        _legalBasis = legalBasis;
        return this;
    }

    internal RetentionPolicyDescriptor Build() => new(
        _dataCategory,
        _retentionPeriod,
        _autoDelete,
        _reason,
        _legalBasis);
}

/// <summary>
/// Internal descriptor for a retention policy configured via the fluent API.
/// </summary>
/// <param name="DataCategory">The data category.</param>
/// <param name="RetentionPeriod">The retention period.</param>
/// <param name="AutoDelete">Whether auto-delete is enabled.</param>
/// <param name="Reason">The reason for the policy.</param>
/// <param name="LegalBasis">The legal basis for retention.</param>
internal sealed record RetentionPolicyDescriptor(
    string DataCategory,
    TimeSpan RetentionPeriod,
    bool AutoDelete,
    string? Reason,
    string? LegalBasis);
