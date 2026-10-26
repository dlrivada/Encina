using Microsoft.Extensions.Options;

namespace Encina.Security.BreakTheGlass;

/// <summary>
/// Validates <see cref="BreakTheGlassOptions"/> when the host starts and reports every failure at once.
/// </summary>
/// <remarks>
/// The safety bounds (duration, sweep interval and batch size, retention, purge interval) are always
/// checked. With <see cref="BreakTheGlassOptions.Enabled"/> <see langword="false"/> the rules of the
/// feature itself (at least one scope, an opener per scope, reviewer roles, per-scope limits) are
/// skipped, so an application that references the package without configuring scopes starts cleanly.
/// </remarks>
internal sealed class BreakTheGlassOptionsValidator : IValidateOptions<BreakTheGlassOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, BreakTheGlassOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();
        ValidateBounds(options, failures);
        if (options.Enabled)
        {
            ValidateFeature(options, failures);
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    // Adds the message when the rule does not hold.
    private static void Require(bool holds, string message, List<string> failures)
    {
        if (!holds)
        {
            failures.Add(message);
        }
    }

    private static bool Within<T>(T value, T min, T max)
        where T : IComparable<T> =>
        value.CompareTo(min) >= 0 && value.CompareTo(max) <= 0;

    private static void ValidateBounds(BreakTheGlassOptions options, List<string> failures)
    {
        var maxDuration = BreakTheGlassOptions.MaxAllowedDuration;
        Require(
            options.MaxDuration > TimeSpan.Zero && Within(options.MaxDuration, TimeSpan.Zero, maxDuration),
            $"BreakTheGlassOptions.MaxDuration must be greater than zero and at most {maxDuration}.",
            failures);
        Require(
            Within(options.MinimumJustificationLength, 1, BreakTheGlassOptions.MaxJustificationLength),
            $"BreakTheGlassOptions.MinimumJustificationLength must be between 1 and {BreakTheGlassOptions.MaxJustificationLength}.",
            failures);
        Require(
            Within(options.ExpirySweepInterval, BreakTheGlassOptions.MinExpirySweepInterval, TimeSpan.FromDays(1)),
            $"BreakTheGlassOptions.ExpirySweepInterval must be between {BreakTheGlassOptions.MinExpirySweepInterval} and 1 day.",
            failures);
        Require(
            Within(options.ExpirySweepBatchSize, 1, BreakTheGlassOptions.MaxExpirySweepBatchSize),
            $"BreakTheGlassOptions.ExpirySweepBatchSize must be between 1 and {BreakTheGlassOptions.MaxExpirySweepBatchSize}.",
            failures);
        Require(options.RetentionDays >= 1, "BreakTheGlassOptions.RetentionDays must be positive.", failures);
        Require(Within(options.PurgeIntervalHours, 1, 720), "BreakTheGlassOptions.PurgeIntervalHours must be between 1 and 720.", failures);
    }

    private static void ValidateFeature(BreakTheGlassOptions options, List<string> failures)
    {
        Require(options.Scopes.Count > 0, "BreakTheGlassOptions.Scopes must declare at least one scope.", failures);
        Require(options.ReviewerRoles.Count > 0, "BreakTheGlassOptions.ReviewerRoles must name at least one reviewer role.", failures);

        foreach (var (scopeName, scope) in options.Scopes)
        {
            ValidateScope(scopeName, scope, options.MaxDuration, failures);
        }
    }

    private static void ValidateScope(string scopeName, BreakTheGlassScopeOptions scope, TimeSpan maxDuration, List<string> failures)
    {
        Require(!string.IsNullOrWhiteSpace(scopeName), "A break-the-glass scope name must not be blank.", failures);
        Require(
            scope.OpenerRoles.Count + scope.OpenerPermissions.Count > 0,
            $"Break-the-glass scope '{scopeName}' must name an opener role or an opener permission.",
            failures);
        Require(
            scope.MaxDuration is null || (scope.MaxDuration > TimeSpan.Zero && scope.MaxDuration <= maxDuration),
            $"Break-the-glass scope '{scopeName}' MaxDuration must be greater than zero and at most BreakTheGlassOptions.MaxDuration.",
            failures);
    }
}
