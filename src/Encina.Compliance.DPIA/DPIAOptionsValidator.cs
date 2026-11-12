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
/// <item><description><see cref="DPIAOptions.ExpirationCheckInterval"/> is between 1 millisecond and <c>uint.MaxValue - 1</c> milliseconds (about 49.7 days), the range <see cref="PeriodicTimer"/> accepts</description></item>
/// <item><description><see cref="DPIAOptions.DPOEmail"/> format is valid when provided</description></item>
/// </list>
/// </para>
/// </remarks>
internal sealed class DPIAOptionsValidator : IValidateOptions<DPIAOptions>
{
    /// <summary>The shortest period <see cref="PeriodicTimer"/> accepts.</summary>
    internal static readonly TimeSpan MinExpirationCheckInterval = TimeSpan.FromMilliseconds(1);

    /// <summary>The longest period <see cref="PeriodicTimer"/> accepts: <c>uint.MaxValue - 1</c> milliseconds.</summary>
    internal static readonly TimeSpan MaxExpirationCheckInterval = TimeSpan.FromMilliseconds(uint.MaxValue - 1);

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, DPIAOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        ValidateEnforcementMode(options, failures);
        ValidateDefaultReviewPeriod(options, failures);
        ValidateExpirationCheckInterval(options, failures);
        ValidateDPOEmail(options, failures);

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }

    private static void ValidateEnforcementMode(DPIAOptions options, List<string> failures)
    {
        if (!Enum.IsDefined(options.EnforcementMode))
        {
            failures.Add(
                $"DPIAOptions.EnforcementMode has an invalid value '{(int)options.EnforcementMode}'. "
                + "Valid values are Block (0), Warn (1), or Disabled (2).");
        }
    }

    private static void ValidateDefaultReviewPeriod(DPIAOptions options, List<string> failures)
    {
        if (options.DefaultReviewPeriod <= TimeSpan.Zero)
        {
            failures.Add(
                $"DPIAOptions.DefaultReviewPeriod must be positive. "
                + $"Current value: {options.DefaultReviewPeriod}.");
        }
    }

    // The review-reminder service always runs, so the interval is always checked against
    // the range PeriodicTimer accepts.
    private static void ValidateExpirationCheckInterval(DPIAOptions options, List<string> failures)
    {
        if (options.ExpirationCheckInterval < MinExpirationCheckInterval
            || options.ExpirationCheckInterval > MaxExpirationCheckInterval)
        {
            failures.Add(
                $"DPIAOptions.ExpirationCheckInterval must be between {MinExpirationCheckInterval} "
                + $"and {MaxExpirationCheckInterval} (the range PeriodicTimer supports). "
                + $"Current value: {options.ExpirationCheckInterval}.");
        }
    }

    private static void ValidateDPOEmail(DPIAOptions options, List<string> failures)
    {
        if (options.DPOEmail is { Length: > 0 }
            && !options.DPOEmail.Contains('@'))
        {
            failures.Add(
                $"DPIAOptions.DPOEmail has an invalid format '{options.DPOEmail}'. "
                + "The email must contain an '@' character.");
        }
    }
}
