using Encina.Compliance.ProcessorAgreements.Model;

using Microsoft.Extensions.Options;

namespace Encina.Compliance.ProcessorAgreements;

/// <summary>
/// Validates <see cref="ProcessorAgreementOptions"/> at startup to catch configuration errors early.
/// </summary>
/// <remarks>
/// <para>
/// Validates that processor agreement configuration is consistent and complete.
/// This validator runs during the first <c>IOptions&lt;ProcessorAgreementOptions&gt;.Value</c> access,
/// providing fail-fast behavior for misconfigured processor agreement compliance.
/// </para>
/// <para>
/// Validates:
/// <list type="bullet">
/// <item><description><see cref="ProcessorAgreementOptions.EnforcementMode"/> is a defined enum value</description></item>
/// <item><description><see cref="ProcessorAgreementOptions.MaxSubProcessorDepth"/> is between 1 and 10 (inclusive)</description></item>
/// <item><description><see cref="ProcessorAgreementOptions.ExpirationCheckInterval"/> is positive when monitoring is enabled</description></item>
/// <item><description><see cref="ProcessorAgreementOptions.ExpirationWarningDays"/> is positive</description></item>
/// </list>
/// </para>
/// </remarks>
internal sealed class ProcessorAgreementOptionsValidator : IValidateOptions<ProcessorAgreementOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, ProcessorAgreementOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        // Validate EnforcementMode is a defined enum value
        if (!Enum.IsDefined(options.EnforcementMode))
        {
            failures.Add(
                $"ProcessorAgreementOptions.EnforcementMode has an invalid value '{(int)options.EnforcementMode}'. "
                + "Valid values are Block (0), Warn (1), or Disabled (2).");
        }

        // Validate MaxSubProcessorDepth is between 1 and 10
        if (options.MaxSubProcessorDepth < 1 || options.MaxSubProcessorDepth > 10)
        {
            failures.Add(
                $"ProcessorAgreementOptions.MaxSubProcessorDepth must be between 1 and 10 (inclusive). "
                + $"Current value: {options.MaxSubProcessorDepth}.");
        }

        // Validate ExpirationCheckInterval is positive when monitoring is enabled
        if (options.EnableExpirationMonitoring && options.ExpirationCheckInterval <= TimeSpan.Zero)
        {
            failures.Add(
                $"ProcessorAgreementOptions.ExpirationCheckInterval must be positive when EnableExpirationMonitoring is true. "
                + $"Current value: {options.ExpirationCheckInterval}.");
        }

        // Validate ExpirationWarningDays is positive
        if (options.ExpirationWarningDays <= 0)
        {
            failures.Add(
                $"ProcessorAgreementOptions.ExpirationWarningDays must be positive. "
                + $"Current value: {options.ExpirationWarningDays}.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
