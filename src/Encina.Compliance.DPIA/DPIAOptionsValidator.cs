using Encina.Compliance.DPIA.Model;

using Microsoft.Extensions.Options;

namespace Encina.Compliance.DPIA;

/// <summary>
/// Validates <see cref="DPIAOptions"/> at startup to catch configuration errors early.
/// </summary>
/// <remarks>
/// <para>
/// Validates that DPIA configuration is consistent and complete.
/// This validator runs during the first <c>IOptions&lt;DPIAOptions&gt;.Value</c> access,
/// providing fail-fast behavior for misconfigured DPIA compliance.
/// </para>
/// <para>
/// Validates:
/// <list type="bullet">
/// <item><description><see cref="DPIAOptions.EnforcementMode"/> is a defined enum value</description></item>
/// <item><description><see cref="DPIAOptions.DefaultReviewPeriod"/> is positive</description></item>
/// <item><description><see cref="DPIAOptions.ExpirationCheckInterval"/> is positive when monitoring is enabled</description></item>
/// <item><description><see cref="DPIAOptions.DPOEmail"/> format is valid when provided</description></item>
/// </list>
/// </para>
/// </remarks>
internal sealed class DPIAOptionsValidator : IValidateOptions<DPIAOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, DPIAOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        // Validate EnforcementMode is a defined enum value
        if (!Enum.IsDefined(options.EnforcementMode))
        {
            failures.Add(
                $"DPIAOptions.EnforcementMode has an invalid value '{(int)options.EnforcementMode}'. "
                + "Valid values are Block (0), Warn (1), or Disabled (2).");
        }

        // Validate DefaultReviewPeriod is positive
        if (options.DefaultReviewPeriod <= TimeSpan.Zero)
        {
            failures.Add(
                $"DPIAOptions.DefaultReviewPeriod must be positive. "
                + $"Current value: {options.DefaultReviewPeriod}.");
        }

        // Validate ExpirationCheckInterval is positive when monitoring is enabled
        if (options.EnableExpirationMonitoring && options.ExpirationCheckInterval <= TimeSpan.Zero)
        {
            failures.Add(
                $"DPIAOptions.ExpirationCheckInterval must be positive when EnableExpirationMonitoring is true. "
                + $"Current value: {options.ExpirationCheckInterval}.");
        }

        // Validate DPOEmail format when provided
        if (options.DPOEmail is { Length: > 0 }
            && !options.DPOEmail.Contains('@'))
        {
            failures.Add(
                $"DPIAOptions.DPOEmail has an invalid format '{options.DPOEmail}'. "
                + "The email must contain an '@' character.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
