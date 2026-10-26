using System.Security.Cryptography;

using Encina.Compliance.Attestation;
using Encina.Compliance.Attestation.Validation;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.Attestation;

/// <summary>
/// Property-based tests for <see cref="HashChainOptionsValidator"/>: only SHA-256, SHA-384 and
/// SHA-512 are accepted, whatever the other options contain.
/// </summary>
[Trait("Category", "Property")]
[Trait("Feature", "Attestation")]
public sealed class HashChainOptionsValidatorPropertyTests
{
    private static readonly string[] Supported = ["SHA256", "SHA384", "SHA512"];

    [Property(MaxTest = 50)]
    public bool Validate_SupportedAlgorithm_AlwaysSucceeds(byte pick, byte[]? key, string? path)
    {
        var options = new HashChainOptions
        {
            HashAlgorithm = new HashAlgorithmName(Supported[pick % Supported.Length]),
            HmacKey = key,
            StoragePath = path
        };

        return new HashChainOptionsValidator().Validate(null, options).Succeeded;
    }

    [Property(MaxTest = 100)]
    public bool Validate_AnyOtherAlgorithmName_FailsNamingTheRejectedValue(NonEmptyString name)
    {
        var rejected = name.Get;
        if (Supported.Contains(rejected))
        {
            return true;
        }

        var result = new HashChainOptionsValidator().Validate(
            null, new HashChainOptions { HashAlgorithm = new HashAlgorithmName(rejected) });

        return result.Failed
            && result.FailureMessage.Contains(rejected, StringComparison.Ordinal)
            && result.FailureMessage.Contains("HashAlgorithm", StringComparison.Ordinal);
    }
}
