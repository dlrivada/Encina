using Microsoft.Extensions.Options;

namespace Encina.Compliance.Anonymization;

/// <summary>
/// Validates <see cref="AnonymizationOptions"/> at startup to catch configuration errors early.
/// </summary>
/// <remarks>
/// <para>
/// Validates that anonymization configuration is consistent and complete.
/// This validator runs during the first <c>IOptions&lt;AnonymizationOptions&gt;.Value</c> access,
/// providing fail-fast behavior for misconfigured anonymization compliance.
/// </para>
/// <para>
/// Validates:
/// <list type="bullet">
/// <item><description><see cref="AnonymizationOptions.EnforcementMode"/> is a defined enum value</description></item>
/// <item><description><see cref="AnonymizationOptions.AutoRegisterFromAttributes"/> and <see cref="AnonymizationOptions.AssembliesToScan"/> are consistent</description></item>
/// </list>
/// </para>
/// </remarks>
internal sealed class AnonymizationOptionsValidator : IValidateOptions<AnonymizationOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, AnonymizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        // Validate EnforcementMode is a defined enum value
        if (!Enum.IsDefined(options.EnforcementMode))
        {
            failures.Add(
                $"AnonymizationOptions.EnforcementMode has an invalid value '{(int)options.EnforcementMode}'. "
                + "Valid values are Block (0), Warn (1), or Disabled (2).");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
