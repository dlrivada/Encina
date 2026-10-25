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

    private static void ValidateBounds(BreakTheGlassOptions options, List<string> failures)
    {
        if (options.MaxDuration <= TimeSpan.Zero || options.MaxDuration > BreakTheGlassOptions.MaxAllowedDuration)
        {
            failures.Add($"BreakTheGlassOptions.MaxDuration must be greater than zero and at most {BreakTheGlassOptions.MaxAllowedDuration}.");
        }

        if (options.MinimumJustificationLength < 1 || options.MinimumJustificationLength > BreakTheGlassOptions.MaxJustificationLength)
        {
            failures.Add($"BreakTheGlassOptions.MinimumJustificationLength must be between 1 and {BreakTheGlassOptions.MaxJustificationLength}.");
        }

        if (options.ExpirySweepInterval < BreakTheGlassOptions.MinExpirySweepInterval)
        {
            failures.Add($"BreakTheGlassOptions.ExpirySweepInterval must be at least {BreakTheGlassOptions.MinExpirySweepInterval}.");
        }

        if (options.ExpirySweepBatchSize < 1 || options.ExpirySweepBatchSize > BreakTheGlassOptions.MaxExpirySweepBatchSize)
        {
            failures.Add($"BreakTheGlassOptions.ExpirySweepBatchSize must be between 1 and {BreakTheGlassOptions.MaxExpirySweepBatchSize}.");
        }

        if (options.RetentionDays < 1)
        {
            failures.Add("BreakTheGlassOptions.RetentionDays must be positive.");
        }

        if (options.PurgeIntervalHours < 1)
        {
            failures.Add("BreakTheGlassOptions.PurgeIntervalHours must be positive.");
        }
    }

    private static void ValidateFeature(BreakTheGlassOptions options, List<string> failures)
    {
        if (options.Scopes.Count == 0)
        {
            failures.Add("BreakTheGlassOptions.Scopes must declare at least one scope.");
        }

        foreach (var (scopeName, scope) in options.Scopes)
        {
            ValidateScope(scopeName, scope, options.MaxDuration, failures);
        }

        if (options.ReviewerRoles.Count == 0)
        {
            failures.Add("BreakTheGlassOptions.ReviewerRoles must name at least one reviewer role.");
        }
    }

    private static void ValidateScope(string scopeName, BreakTheGlassScopeOptions scope, TimeSpan maxDuration, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(scopeName))
        {
            failures.Add("A break-the-glass scope name must not be blank.");
        }

        if (scope.OpenerRoles.Count == 0 && scope.OpenerPermissions.Count == 0)
        {
            failures.Add($"Break-the-glass scope '{scopeName}' must name an opener role or an opener permission.");
        }

        if (scope.MaxDuration is { } limit && (limit <= TimeSpan.Zero || limit > maxDuration))
        {
            failures.Add($"Break-the-glass scope '{scopeName}' MaxDuration must be greater than zero and at most BreakTheGlassOptions.MaxDuration.");
        }
    }
}
