using Microsoft.Extensions.Options;

namespace Encina.Compliance.DataResidency;

/// <summary>
/// Validates <see cref="DataResidencyOptions"/> at startup to catch configuration errors early.
/// </summary>
/// <remarks>
/// <para>
/// Validates that data residency configuration is consistent and complete.
/// This validator runs during the first <c>IOptions&lt;DataResidencyOptions&gt;.Value</c> access,
/// providing fail-fast behavior for misconfigured residency compliance.
/// </para>
/// <para>
/// Validates:
/// <list type="bullet">
/// <item><description><see cref="DataResidencyOptions.EnforcementMode"/> is a defined enum value</description></item>
/// <item><description><see cref="DataResidencyOptions.DefaultRegion"/> is set when enforcement is Block</description></item>
/// <item><description>Fluent-configured policies have at least one allowed region</description></item>
/// </list>
/// </para>
/// </remarks>
internal sealed class DataResidencyOptionsValidator : IValidateOptions<DataResidencyOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, DataResidencyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        // Validate EnforcementMode is a defined enum value
        if (!Enum.IsDefined(options.EnforcementMode))
        {
            failures.Add(
                $"DataResidencyOptions.EnforcementMode has an invalid value '{(int)options.EnforcementMode}'. "
                + "Valid values are Block (0), Warn (1), or Disabled (2).");
        }

        // Validate DefaultRegion is set when enforcement is Block
        if (options.EnforcementMode == DataResidencyEnforcementMode.Block && options.DefaultRegion is null)
        {
            failures.Add(
                "DataResidencyOptions.DefaultRegion must be set when EnforcementMode is Block. "
                + "This ensures region resolution always succeeds for strict enforcement.");
        }

        // Validate fluent-configured policies have at least one allowed region
        foreach (var policy in options.ConfiguredPolicies)
        {
            if (policy.AllowedRegions.Count == 0)
            {
                failures.Add(
                    $"Data residency policy for category '{policy.DataCategory}' has no allowed regions. "
                    + "Use AllowRegions(), AllowEU(), AllowEEA(), or AllowAdequate() to specify at least one region.");
            }
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
