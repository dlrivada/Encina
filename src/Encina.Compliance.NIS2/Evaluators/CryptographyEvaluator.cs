using Encina.Compliance.NIS2.Abstractions;
using Encina.Compliance.NIS2.Model;
using Encina.Security.Encryption.Abstractions;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;

using static LanguageExt.Prelude;

namespace Encina.Compliance.NIS2.Evaluators;

/// <summary>
/// Evaluates Art. 21(2)(h): Policies and procedures regarding the use of cryptography and encryption.
/// </summary>
/// <remarks>
/// <para>
/// Beyond configuration flags, this evaluator checks whether
/// <c>Encina.Security.Encryption</c>'s <c>IKeyProvider</c> is registered, providing evidence
/// that cryptographic key management infrastructure is in place.
/// </para>
/// </remarks>
internal sealed class CryptographyEvaluator : INIS2MeasureEvaluator
{
    public NIS2Measure Measure => NIS2Measure.Cryptography;

    public ValueTask<Either<EncinaError, NIS2MeasureResult>> EvaluateAsync(
        NIS2MeasureContext context,
        CancellationToken cancellationToken = default)
    {
        var hasAtRest = context.Options.EncryptedDataCategories.Count > 0;
        var hasInTransit = context.Options.EncryptedEndpoints.Count > 0;
        var enforced = context.Options.EnforceEncryption;

        // Check if real key management infrastructure is available
        var hasKeyProvider = context.ServiceProvider
            .GetService<IKeyProvider>() is not null;

        if (hasAtRest && hasInTransit && enforced)
        {
            var details = $"Cryptography policies are in place: {context.Options.EncryptedDataCategories.Count} data category(ies) encrypted at rest, "
                + $"{context.Options.EncryptedEndpoints.Count} endpoint(s) encrypted in transit.";

            if (hasKeyProvider)
            {
                details += " Cryptographic key management infrastructure (IKeyProvider) is active.";
            }

            return ValueTask.FromResult(Right<EncinaError, NIS2MeasureResult>(
                NIS2MeasureResult.Satisfied(Measure, details)));
        }

        var recommendations = new List<string>();
        if (!hasAtRest) recommendations.Add("Register encrypted data categories via NIS2Options.EncryptedDataCategories");
        if (!hasInTransit) recommendations.Add("Register encrypted endpoints via NIS2Options.EncryptedEndpoints");
        if (!enforced) recommendations.Add("Enable encryption enforcement via NIS2Options.EnforceEncryption = true");
        if (!hasKeyProvider) recommendations.Add("Register Encina.Security.Encryption for cryptographic key management (IKeyProvider)");

        return ValueTask.FromResult(Right<EncinaError, NIS2MeasureResult>(
            NIS2MeasureResult.NotSatisfied(Measure,
                "Cryptography and encryption policies are not fully configured.",
                recommendations)));
    }
}
