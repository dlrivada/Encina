namespace Encina.Security.BreakTheGlass;

/// <summary>
/// Options of the break-the-glass service and gate.
/// </summary>
/// <remarks>
/// <para>
/// The request limits are enforced by <see cref="BreakTheGlassRequestValidator"/>; the options themselves
/// are validated at start-up by <c>AddEncinaBreakTheGlass</c>. The safety bounds (<see cref="MaxDuration"/>,
/// the sweep settings) are always validated. With <see cref="Enabled"/> <see langword="false"/> the
/// validation of scopes, opener roles and reviewer roles and the start-up checks are skipped, so an
/// application that references the package without configuring scopes starts cleanly, while the expiry
/// sweeper keeps closing and auditing the elevations that are already open.
/// </para>
/// <para>
/// Every setting is read from the built options at run time, so each can come from the <c>configure</c>
/// delegate or from configuration binding. <see cref="EnableAutoPurge"/> is read when the purge service starts.
/// </para>
/// </remarks>
public sealed class BreakTheGlassOptions
{
    /// <summary>The largest <see cref="MaxDuration"/> the validator accepts.</summary>
    public static TimeSpan MaxAllowedDuration => TimeSpan.FromHours(24);

    /// <summary>The largest justification length accepted.</summary>
    public const int MaxJustificationLength = 2000;

    /// <summary>The shortest <see cref="ExpirySweepInterval"/> the validator accepts.</summary>
    public static TimeSpan MinExpirySweepInterval => TimeSpan.FromSeconds(10);

    /// <summary>The largest <see cref="ExpirySweepBatchSize"/> the validator accepts.</summary>
    public const int MaxExpirySweepBatchSize = 500;

    /// <summary>Gets or sets a value indicating whether the gate honours elevations. Default <see langword="true"/>.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Gets or sets how often the expiry sweeper closes and audits expired elevations. Default 1 minute; the validator accepts 10 seconds to 1 day.</summary>
    public TimeSpan ExpirySweepInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>Gets or sets the most elevations one sweep cycle handles. Default 100; the validator accepts 1 to 500.</summary>
    public int ExpirySweepBatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets how many days a closed and reviewed elevation is kept before the purge deletes it.
    /// Default 2555 (about 7 years, aligned with the operation audit). Used only when <see cref="EnableAutoPurge"/> is set.
    /// </summary>
    public int RetentionDays { get; set; } = 2555;

    /// <summary>
    /// Gets or sets a value indicating whether closed and reviewed elevations older than
    /// <see cref="RetentionDays"/> are deleted automatically. Default <see langword="false"/>: deleting review
    /// evidence is irreversible, so the application enables it once its retention policy is fixed (the
    /// justification can hold health data; GDPR art. 5(1)(e)). A required production setting.
    /// </summary>
    public bool EnableAutoPurge { get; set; }

    /// <summary>Gets or sets the hours between two purge runs. Default 24; the validator accepts 1 to 720; used only when <see cref="EnableAutoPurge"/> is set.</summary>
    public int PurgeIntervalHours { get; set; } = 24;

    /// <summary>
    /// Gets or sets a value indicating whether the in-memory elevation store may run outside the
    /// <c>Development</c> environment. Default <see langword="false"/>: start-up fails; when set, the use is logged.
    /// </summary>
    public bool AllowInMemoryStore { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the application accepts <see cref="RequireTenant"/> being
    /// <see langword="false"/> although tenancy is registered (<c>ITenantProvider</c>). Default
    /// <see langword="false"/>: start-up fails. When set, the opt-out is logged at every start.
    /// </summary>
    public bool AllowMissingTenantRequirement { get; set; }

    /// <summary>Gets the declared scopes by name (case-sensitive).</summary>
    public IDictionary<string, BreakTheGlassScopeOptions> Scopes { get; } =
        new Dictionary<string, BreakTheGlassScopeOptions>(StringComparer.Ordinal);

    /// <summary>Gets the roles allowed to review elevations and to read the review queue.</summary>
    public ISet<string> ReviewerRoles { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets or sets the longest elevation. Default 4 hours; the validator rejects a value above 24 hours.</summary>
    public TimeSpan MaxDuration { get; set; } = TimeSpan.FromHours(4);

    /// <summary>Gets or sets the minimum length of the trimmed justification. Default 20.</summary>
    public int MinimumJustificationLength { get; set; } = 20;

    /// <summary>Gets or sets a value indicating whether opening and reviewing publish notifications. Default <see langword="true"/>.</summary>
    public bool PublishNotifications { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether a tenant is required by every service operation (open, revoke, review, review queue and get). Default <see langword="false"/>.</summary>
    public bool RequireTenant { get; set; }

    /// <summary>
    /// Declares a scope.
    /// </summary>
    /// <param name="name">The scope name (case-sensitive).</param>
    /// <param name="configure">Configures the scope.</param>
    /// <returns>This instance.</returns>
    public BreakTheGlassOptions DeclareScope(string name, Action<BreakTheGlassScopeOptions> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(configure);

        var scope = new BreakTheGlassScopeOptions();
        configure(scope);
        Scopes[name] = scope;
        return this;
    }
}

/// <summary>
/// Options of one break-the-glass scope.
/// </summary>
public sealed class BreakTheGlassScopeOptions
{
    /// <summary>Gets the roles allowed to open an elevation for the scope.</summary>
    public ISet<string> OpenerRoles { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets the permissions allowed to open an elevation for the scope.</summary>
    public ISet<string> OpenerPermissions { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets or sets a per-scope duration limit that tightens <see cref="BreakTheGlassOptions.MaxDuration"/>, or <see langword="null"/>.</summary>
    public TimeSpan? MaxDuration { get; set; }
}
