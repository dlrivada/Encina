#pragma warning disable CA2012 // ValueTask should not be awaited multiple times - used via .AsTask().Result in sync property tests

using System.Security.Cryptography;
using System.Text;
using Encina.Security.AntiTampering;
using Encina.Security.AntiTampering.HMAC;
using FsCheck;
using FsCheck.Xunit;
using LanguageExt;
using Microsoft.Extensions.Options;

namespace Encina.PropertyTests.Security.AntiTampering;

/// <summary>
/// Property-based tests for HMAC signing and verification invariants.
/// Uses FsCheck to verify behavioral properties that must hold for all valid inputs.
/// </summary>
public sealed class SignaturePropertyTests
{
    #region Roundtrip Invariant

    [Property(MaxTest = 50)]
    public bool SignThenVerify_WithSameKey_AlwaysSucceeds(NonEmptyString payloadStr)
    {
        var (signer, _) = CreateSigner();
        var context = CreateContext("prop-key");
        var payload = Encoding.UTF8.GetBytes(payloadStr.Get);

        var signResult = signer.SignAsync(payload.AsMemory(), context).AsTask().Result;
        if (!signResult.IsRight) return false;

        var signature = (string)signResult;
        var verifyResult = signer.VerifyAsync(payload.AsMemory(), signature, context).AsTask().Result;
        if (!verifyResult.IsRight) return false;

        return (bool)verifyResult;
    }

    #endregion

    #region Cross-Key Failure

    [Property(MaxTest = 30)]
    public bool SignWithKeyA_VerifyWithKeyB_AlwaysFails(NonEmptyString payloadStr)
    {
        var keyA = new byte[32];
        var keyB = new byte[32];
        RandomNumberGenerator.Fill(keyA);
        RandomNumberGenerator.Fill(keyB);

        var signerA = CreateSignerWithKey("key-a", keyA);
        var signerB = CreateSignerWithKey("key-b", keyB);
        var payload = Encoding.UTF8.GetBytes(payloadStr.Get);
        var context = CreateContext("key-a");

        var signResult = signerA.SignAsync(payload.AsMemory(), context).AsTask().Result;
        if (!signResult.IsRight) return false;

        var signature = (string)signResult;
        var contextB = CreateContext("key-b");
        var verifyResult = signerB.VerifyAsync(payload.AsMemory(), signature, contextB).AsTask().Result;
        if (!verifyResult.IsRight) return false;

        return !(bool)verifyResult;
    }

    #endregion

    #region Determinism

    [Property(MaxTest = 50)]
    public bool Signature_IsDeterministic_SameInputSameOutput(NonEmptyString payloadStr)
    {
        var (signer, _) = CreateSigner();
        var context = CreateContext("prop-key");
        var payload = Encoding.UTF8.GetBytes(payloadStr.Get);

        var result1 = signer.SignAsync(payload.AsMemory(), context).AsTask().Result;
        var result2 = signer.SignAsync(payload.AsMemory(), context).AsTask().Result;

        if (!result1.IsRight || !result2.IsRight) return false;

        return (string)result1 == (string)result2;
    }

    #endregion

    #region Helpers

    private static (HMACSigner signer, InMemoryKeyProvider keyProvider) CreateSigner()
    {
        var options = new AntiTamperingOptions { Algorithm = HMACAlgorithm.SHA256 };
        var optionsWrapper = Options.Create(options);
        var keyProvider = new InMemoryKeyProvider(optionsWrapper);

        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        keyProvider.AddKey("prop-key", key);

        var signer = new HMACSigner(keyProvider, optionsWrapper);
        return (signer, keyProvider);
    }

    private static HMACSigner CreateSignerWithKey(string keyId, byte[] key)
    {
        var options = new AntiTamperingOptions { Algorithm = HMACAlgorithm.SHA256 };
        var optionsWrapper = Options.Create(options);
        var keyProvider = new InMemoryKeyProvider(optionsWrapper);
        keyProvider.AddKey(keyId, key);

        return new HMACSigner(keyProvider, optionsWrapper);
    }

    private static SigningContext CreateContext(string keyId) => new()
    {
        KeyId = keyId,
        HttpMethod = "POST",
        RequestPath = "/api/test",
        Timestamp = DateTimeOffset.UtcNow,
        Nonce = Guid.NewGuid().ToString("N")
    };

    #endregion
}
