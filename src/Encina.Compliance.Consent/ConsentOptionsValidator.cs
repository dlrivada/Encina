using Microsoft.Extensions.Options;

namespace Encina.Compliance.Consent;

/// <summary>
/// Validates <see cref="ConsentOptions"/> at startup to catch configuration errors early.
/// </summary>
/// <remarks>
/// <para>
/// Validates that consent configuration is consistent and complete.
/// This validator runs during the first <c>IOptions&lt;ConsentOptions&gt;.Value</c> access,
/// providing fail-fast behavior for misconfigured consent compliance.
/// </para>
/// <para>
/// Validates:
/// <list type="bullet">
/// <item><description><see cref="ConsentOptions.DefaultExpirationDays"/> is positive when set</description></item>
/// <item><description>All purpose definitions have valid identifiers</description></item>
/// <item><description><see cref="ConsentOptions.PurposeDefinitions"/> are non-empty when enforcement is <see cref="ConsentEnforcementMode.Block"/></description></item>
/// <item><description>Purpose-specific <see cref="ConsentOptions.PurposeDefinitionEntry.DefaultExpirationDays"/> are positive when set</description></item>
/// </list>
/// </para>
/// </remarks>
internal sealed class ConsentOptionsValidator : IValidateOptions<ConsentOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, ConsentOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        // Validate DefaultExpirationDays
        if (options.DefaultExpirationDays is not null and <= 0)
        {
            failures.Add(
                "ConsentOptions.DefaultExpirationDays must be a positive number when set. "
                + "Consent records need a meaningful expiration period.");
        }

        // Validate enforcement mode + purpose definitions
        if (options.EnforcementMode == ConsentEnforcementMode.Block
            && options.PurposeDefinitions.Count == 0
            && options.DetailedPurposeDefinitions.Count == 0)
        {
            failures.Add(
                "ConsentOptions.PurposeDefinitions is empty while EnforcementMode is Block. "
                + "Either define at least one purpose via PurposeDefinitions or DefinePurpose(), "
                + "or set EnforcementMode to Warn or Disabled.");
        }

        // Validate purpose definition entries
        foreach (var (purpose, definition) in options.DetailedPurposeDefinitions)
        {
            if (string.IsNullOrWhiteSpace(purpose))
            {
                failures.Add(
                    "A PurposeDefinition has an empty purpose identifier. "
                    + "Purpose identifiers must be non-empty strings.");
            }

            if (definition.DefaultExpirationDays is not null and <= 0)
            {
                failures.Add(
                    $"PurposeDefinition '{purpose}' has DefaultExpirationDays={definition.DefaultExpirationDays}. "
                    + "Expiration days must be a positive number when set.");
            }
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
