namespace Encina.Security.BreakTheGlass;

/// <summary>
/// Options of the break-the-glass service and gate.
/// </summary>
/// <remarks>
/// The limits are enforced by <see cref="BreakTheGlassRequestValidator"/>. Registration helpers and
/// option validation arrive with the registration extension.
/// </remarks>
public sealed class BreakTheGlassOptions
{
    /// <summary>The largest <see cref="MaxDuration"/> the validator accepts.</summary>
    public static TimeSpan MaxAllowedDuration => TimeSpan.FromHours(24);

    /// <summary>The largest justification length accepted.</summary>
    public const int MaxJustificationLength = 2000;

    /// <summary>Gets or sets a value indicating whether the gate honours elevations. Default <see langword="true"/>.</summary>
    public bool Enabled { get; set; } = true;

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

    /// <summary>Gets or sets a value indicating whether a tenant is required to open, review and query. Default <see langword="false"/>.</summary>
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
