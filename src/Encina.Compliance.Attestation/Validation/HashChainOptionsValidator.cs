using Microsoft.Extensions.Options;

namespace Encina.Compliance.Attestation.Validation;

/// <summary>
/// Validates <see cref="HashChainOptions"/> at startup so that an unsupported
/// <see cref="HashChainOptions.HashAlgorithm"/> fails fast instead of on the first attestation.
/// </summary>
/// <remarks>
/// Supported algorithms are SHA-256, SHA-384 and SHA-512, the same set accepted by
/// <see cref="ContentHasher"/>.
/// </remarks>
internal sealed class HashChainOptionsValidator : IValidateOptions<HashChainOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, HashChainOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        try
        {
            _ = ContentHasher.GetRecommendedKeySize(options.HashAlgorithm);
            return ValidateOptionsResult.Success;
        }
        catch (ArgumentException)
        {
            return ValidateOptionsResult.Fail(
                $"{nameof(HashChainOptions)}.{nameof(HashChainOptions.HashAlgorithm)} " +
                $"'{options.HashAlgorithm.Name ?? "(none)"}' is not supported. Use SHA256, SHA384, or SHA512.");
        }
    }
}
