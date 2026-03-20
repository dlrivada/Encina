using Encina.Compliance.AIAct.Model;

using Microsoft.Extensions.Options;

namespace Encina.Compliance.AIAct;

/// <summary>
/// Validates <see cref="AIActOptions"/> at startup to catch configuration errors early.
/// </summary>
/// <remarks>
/// <para>
/// Validates that required configuration is populated when enforcement is enabled.
/// This validator runs during the first <c>IOptions&lt;AIActOptions&gt;.Value</c> access,
/// providing fail-fast behavior for misconfigured AI Act compliance.
/// </para>
/// </remarks>
internal sealed class AIActOptionsValidator : IValidateOptions<AIActOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, AIActOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (options.EnforcementMode == AIActEnforcementMode.Block &&
            options.AutoRegisterFromAttributes &&
            options.AssembliesToScan.Count == 0)
        {
            // This is a warning-level validation; the system will fall back to entry assembly.
            // No hard failure here — the hosted service handles the fallback.
        }

        if (options.EnforcementMode != AIActEnforcementMode.Disabled &&
            !options.AutoRegisterFromAttributes &&
            options.AssembliesToScan.Count > 0)
        {
            failures.Add(
                "AIActOptions.AssembliesToScan has entries but AutoRegisterFromAttributes is false. "
                + "Either enable AutoRegisterFromAttributes or remove the assembly entries.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
