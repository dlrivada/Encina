using Microsoft.Extensions.Options;

namespace Encina.Compliance.Retention;

/// <summary>
/// Validates <see cref="RetentionOptions"/> at startup to catch configuration errors early.
/// </summary>
/// <remarks>
/// <para>
/// Validates that retention configuration is consistent and complete.
/// This validator runs during the first <c>IOptions&lt;RetentionOptions&gt;.Value</c> access,
/// providing fail-fast behavior for misconfigured retention compliance.
/// </para>
/// <para>
/// Validates:
/// <list type="bullet">
/// <item><description><see cref="RetentionOptions.EnforcementMode"/> is a defined enum value</description></item>
/// <item><description><see cref="RetentionOptions.EnforcementInterval"/> is positive</description></item>
/// <item><description><see cref="RetentionOptions.AlertBeforeExpirationDays"/> is non-negative</description></item>
/// <item><description><see cref="RetentionOptions.DefaultRetentionPeriod"/> is positive when set</description></item>
/// </list>
/// </para>
/// </remarks>
internal sealed class RetentionOptionsValidator : IValidateOptions<RetentionOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, RetentionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        // Validate EnforcementMode is a defined enum value
        if (!Enum.IsDefined(options.EnforcementMode))
        {
            failures.Add(
                $"RetentionOptions.EnforcementMode has an invalid value '{(int)options.EnforcementMode}'. "
                + "Valid values are Block (0), Warn (1), or Disabled (2).");
        }

        // Validate EnforcementInterval is positive
        if (options.EnforcementInterval <= TimeSpan.Zero)
        {
            failures.Add(
                $"RetentionOptions.EnforcementInterval must be positive. "
                + $"Current value: {options.EnforcementInterval}.");
        }

        // Validate AlertBeforeExpirationDays is non-negative
        if (options.AlertBeforeExpirationDays < 0)
        {
            failures.Add(
                $"RetentionOptions.AlertBeforeExpirationDays must be non-negative. "
                + $"Current value: {options.AlertBeforeExpirationDays}.");
        }

        // Validate DefaultRetentionPeriod is positive when set
        if (options.DefaultRetentionPeriod is { } period && period <= TimeSpan.Zero)
        {
            failures.Add(
                $"RetentionOptions.DefaultRetentionPeriod must be positive when set. "
                + $"Current value: {period}.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
