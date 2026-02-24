using Microsoft.Extensions.Options;

namespace Encina.Compliance.GDPR;

/// <summary>
/// Validates <see cref="LawfulBasisOptions"/> at startup to catch configuration errors early.
/// </summary>
/// <remarks>
/// <para>
/// Validates that required fields are populated when auto-registration is enabled.
/// This validator runs during the first <c>IOptions&lt;LawfulBasisOptions&gt;.Value</c> access,
/// providing fail-fast behavior for misconfigured lawful basis validation.
/// </para>
/// </remarks>
internal sealed class LawfulBasisOptionsValidator : IValidateOptions<LawfulBasisOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, LawfulBasisOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        // Validate enforcement mode is a defined enum value
        if (!Enum.IsDefined(options.EnforcementMode))
        {
            failures.Add(
                $"LawfulBasisOptions.EnforcementMode has an invalid value: {(int)options.EnforcementMode}. "
                + "Use Block, Warn, or Disabled.");
        }

        // Validate that DefaultBases does not contain null keys
        foreach (var kvp in options.DefaultBases)
        {
            if (kvp.Key is null)
            {
                failures.Add("LawfulBasisOptions.DefaultBases contains a null key.");
                break;
            }

            if (!Enum.IsDefined(kvp.Value))
            {
                failures.Add(
                    $"LawfulBasisOptions.DefaultBases contains an invalid LawfulBasis value "
                    + $"for type '{kvp.Key.Name}': {(int)kvp.Value}.");
            }
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
