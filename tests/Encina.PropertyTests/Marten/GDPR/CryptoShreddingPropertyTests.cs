using System.Collections.Immutable;
using System.Security.Cryptography;

using Encina.Marten.GDPR;
using Encina.Security.Encryption;

using FsCheck;
using FsCheck.Xunit;

using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

namespace Encina.PropertyTests.Marten.GDPR;

/// <summary>
/// Property-based tests for crypto-shredding invariants using FsCheck.
/// Tests fundamental properties that must hold for all inputs.
/// </summary>
[Trait("Category", "Property")]
[Trait("Provider", "Marten")]
public sealed class CryptoShreddingPropertyTests : IDisposable
{
    private readonly InMemorySubjectKeyProvider _keyProvider;

    public CryptoShreddingPropertyTests()
    {
        _keyProvider = new InMemorySubjectKeyProvider(
            TimeProvider.System,
            NullLogger<InMemorySubjectKeyProvider>.Instance);
    }

    public void Dispose()
    {
        _keyProvider.Clear();
    }

    #region Key Roundtrip Invariants

    [Property(MaxTest = 50)]
    public bool GetOrCreate_ThenGet_ReturnsSameKey(NonEmptyString subjectId)
    {
        var id = subjectId.Get.Trim();
        if (string.IsNullOrWhiteSpace(id)) return true; // skip invalid inputs

        var createResult = _keyProvider.GetOrCreateSubjectKeyAsync(id)
            .AsTask().GetAwaiter().GetResult();
        var getResult = _keyProvider.GetSubjectKeyAsync(id)
            .AsTask().GetAwaiter().GetResult();

        if (createResult.IsLeft || getResult.IsLeft) return false;

        byte[] createdKey = null!;
        byte[] gottenKey = null!;
        createResult.IfRight(k => createdKey = k.KeyMaterial);
        getResult.IfRight(k => gottenKey = k);

        return createdKey.SequenceEqual(gottenKey);
    }

    [Property(MaxTest = 50)]
    public bool GetOrCreate_CalledTwice_ReturnsSameKey(NonEmptyString subjectId)
    {
        var id = subjectId.Get.Trim();
        if (string.IsNullOrWhiteSpace(id)) return true;

        var first = _keyProvider.GetOrCreateSubjectKeyAsync(id)
            .AsTask().GetAwaiter().GetResult();
        var second = _keyProvider.GetOrCreateSubjectKeyAsync(id)
            .AsTask().GetAwaiter().GetResult();

        if (first.IsLeft || second.IsLeft) return false;

        byte[] firstKey = null!;
        byte[] secondKey = null!;
        first.IfRight(k => firstKey = k.KeyMaterial);
        second.IfRight(k => secondKey = k.KeyMaterial);

        return firstKey.SequenceEqual(secondKey);
    }

    [Property(MaxTest = 50)]
    public bool CreatedKey_HasCorrectLength(NonEmptyString subjectId)
    {
        var id = subjectId.Get.Trim();
        if (string.IsNullOrWhiteSpace(id)) return true;

        var result = _keyProvider.GetOrCreateSubjectKeyAsync(id)
            .AsTask().GetAwaiter().GetResult();

        if (result.IsLeft) return false;

        byte[] key = null!;
        result.IfRight(k => key = k.KeyMaterial);

        return key.Length == 32; // AES-256 = 32 bytes
    }

    #endregion

    #region Forget Invariants

    [Property(MaxTest = 50)]
    public bool ForgetSubject_ThenIsForgotten_ReturnsTrue(NonEmptyString subjectId)
    {
        var id = subjectId.Get.Trim();
        if (string.IsNullOrWhiteSpace(id)) return true;

        // Create a key first
        _keyProvider.GetOrCreateSubjectKeyAsync(id)
            .AsTask().GetAwaiter().GetResult();

        // Forget the subject
        _keyProvider.DeleteSubjectKeysAsync(id)
            .AsTask().GetAwaiter().GetResult();

        // Check forgotten
        var result = _keyProvider.IsSubjectForgottenAsync(id)
            .AsTask().GetAwaiter().GetResult();

        if (result.IsLeft) return false;

        bool isForgotten = false;
        result.IfRight(f => isForgotten = f);

        return isForgotten;
    }

    [Property(MaxTest = 50)]
    public bool ForgetSubject_ThenGetKey_ReturnsError(NonEmptyString subjectId)
    {
        var id = subjectId.Get.Trim();
        if (string.IsNullOrWhiteSpace(id)) return true;

        // Create then forget
        _keyProvider.GetOrCreateSubjectKeyAsync(id)
            .AsTask().GetAwaiter().GetResult();
        _keyProvider.DeleteSubjectKeysAsync(id)
            .AsTask().GetAwaiter().GetResult();

        // Attempt to get key
        var result = _keyProvider.GetSubjectKeyAsync(id)
            .AsTask().GetAwaiter().GetResult();

        return result.IsLeft; // Should be error (subject forgotten)
    }

    [Property(MaxTest = 50)]
    public bool ForgetSubject_ThenGetOrCreate_ReturnsError(NonEmptyString subjectId)
    {
        var id = subjectId.Get.Trim();
        if (string.IsNullOrWhiteSpace(id)) return true;

        // Create then forget
        _keyProvider.GetOrCreateSubjectKeyAsync(id)
            .AsTask().GetAwaiter().GetResult();
        _keyProvider.DeleteSubjectKeysAsync(id)
            .AsTask().GetAwaiter().GetResult();

        // Attempt to create new key
        var result = _keyProvider.GetOrCreateSubjectKeyAsync(id)
            .AsTask().GetAwaiter().GetResult();

        return result.IsLeft; // Should be error (cannot re-create for forgotten subject)
    }

    #endregion

    #region Key Rotation Invariants

    [Property(MaxTest = 30)]
    public bool RotateKey_ProducesDifferentKey(NonEmptyString subjectId)
    {
        var id = subjectId.Get.Trim();
        if (string.IsNullOrWhiteSpace(id)) return true;

        var originalResult = _keyProvider.GetOrCreateSubjectKeyAsync(id)
            .AsTask().GetAwaiter().GetResult();
        if (originalResult.IsLeft) return false;

        byte[] originalKey = null!;
        originalResult.IfRight(k => originalKey = k.KeyMaterial);

        var rotateResult = _keyProvider.RotateSubjectKeyAsync(id)
            .AsTask().GetAwaiter().GetResult();
        if (rotateResult.IsLeft) return false;

        var newKeyResult = _keyProvider.GetSubjectKeyAsync(id)
            .AsTask().GetAwaiter().GetResult();
        if (newKeyResult.IsLeft) return false;

        byte[] newKey = null!;
        newKeyResult.IfRight(k => newKey = k);

        return !originalKey.SequenceEqual(newKey);
    }

    [Property(MaxTest = 30)]
    public bool RotateKey_IncrementsVersion(NonEmptyString subjectId)
    {
        var id = subjectId.Get.Trim();
        if (string.IsNullOrWhiteSpace(id)) return true;

        _keyProvider.GetOrCreateSubjectKeyAsync(id)
            .AsTask().GetAwaiter().GetResult();

        // The generator may produce the same id more than once within a run and the provider
        // keeps state across iterations, so the invariant is "rotation adds exactly one version"
        // relative to the version active before rotating, not "the new version is 2".
        var infoBefore = _keyProvider.GetSubjectInfoAsync(id)
            .AsTask().GetAwaiter().GetResult();
        if (infoBefore.IsLeft) return false;

        int versionBefore = 0;
        infoBefore.IfRight(i => versionBefore = i.ActiveKeyVersion);

        var rotateResult = _keyProvider.RotateSubjectKeyAsync(id)
            .AsTask().GetAwaiter().GetResult();

        if (rotateResult.IsLeft) return false;

        int newVersion = 0;
        rotateResult.IfRight(r => newVersion = r.NewVersion);

        return newVersion == versionBefore + 1;
    }

    #endregion

    #region v2 Token and Cipher Invariants

    [Property(MaxTest = 100)]
    public bool Serialize_ThenParse_RestoresAllFields(NonEmptyString keyId, byte[] ciphertextRaw)
    {
        var kid = keyId.Get;
        var ct = ciphertextRaw ?? [];
        if (ct.Length == 0) ct = [1, 2, 3]; // Need at least some ciphertext

        var nonce = new byte[12];
        RandomNumberGenerator.Fill(nonce);
        var tag = new byte[16];
        RandomNumberGenerator.Fill(tag);

        var token = CryptoShreddingToken.Format(1, nonce, ct, tag);

        // The v2 token carries only the version and the payload: no subject id or key id (#1698).
        _ = kid;
        return CryptoShreddingToken.TryParse(token, out var parsed)
            && parsed.Version == 1
            && parsed.Ciphertext.SequenceEqual(ct)
            && parsed.Nonce.SequenceEqual(nonce)
            && parsed.Tag.SequenceEqual(tag);
    }

    [Property(MaxTest = 50)]
    public bool Cipher_RoundTripsAnyPlaintext_AndBindsTheSubject(NonEmptyString plaintext, NonEmptyString subject)
    {
        using var aes = CryptoShreddingFieldCipher.CreateAes(RandomNumberGenerator.GetBytes(32))!;
        var token = CryptoShreddingFieldCipher.Encrypt(aes, subject.Get, 1, plaintext.Get);
        CryptoShreddingToken.TryParse(token, out var parsed).ShouldBeTrue();

        Should.Throw<AuthenticationTagMismatchException>(() => CryptoShreddingFieldCipher.Decrypt(aes, subject.Get + "x", parsed));
        return CryptoShreddingFieldCipher.Decrypt(aes, subject.Get, parsed) == plaintext.Get;
    }

    [Property(MaxTest = 100)]
    public bool RegularString_IsNeverAToken(NonEmptyString input) =>
        input.Get.StartsWith(CryptoShreddingToken.Prefix, StringComparison.Ordinal) || !CryptoShreddingToken.TryParse(input.Get, out _);

    #endregion

    #region Subject Info Invariants

    [Property(MaxTest = 30)]
    public bool SubjectInfo_ActiveSubject_HasVersion1(NonEmptyString subjectId)
    {
        var id = subjectId.Get.Trim();
        if (string.IsNullOrWhiteSpace(id)) return true;

        _keyProvider.GetOrCreateSubjectKeyAsync(id)
            .AsTask().GetAwaiter().GetResult();

        var infoResult = _keyProvider.GetSubjectInfoAsync(id)
            .AsTask().GetAwaiter().GetResult();
        if (infoResult.IsLeft) return false;

        int version = 0;
        SubjectStatus status = default;
        infoResult.IfRight(info =>
        {
            version = info.ActiveKeyVersion;
            status = info.Status;
        });

        return version == 1 && status == SubjectStatus.Active;
    }

    #endregion
}
