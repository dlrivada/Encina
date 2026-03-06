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
        CryptoShreddedPropertyCache.ClearCache();
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
        createResult.IfRight(k => createdKey = k);
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
        first.IfRight(k => firstKey = k);
        second.IfRight(k => secondKey = k);

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
        result.IfRight(k => key = k);

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
        originalResult.IfRight(k => originalKey = k);

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

        var rotateResult = _keyProvider.RotateSubjectKeyAsync(id)
            .AsTask().GetAwaiter().GetResult();

        if (rotateResult.IsLeft) return false;

        int newVersion = 0;
        rotateResult.IfRight(r => newVersion = r.NewVersion);

        return newVersion == 2; // First key is v1, rotated key is v2
    }

    #endregion

    #region EncryptedFieldJsonConverter Roundtrip Invariants

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

        var original = new EncryptedValue
        {
            KeyId = kid,
            Ciphertext = ImmutableArray.Create(ct),
            Nonce = ImmutableArray.Create(nonce),
            Tag = ImmutableArray.Create(tag),
            Algorithm = EncryptionAlgorithm.Aes256Gcm
        };

        var json = EncryptedFieldJsonConverter.Serialize(original);
        var parsed = EncryptedFieldJsonConverter.TryParse(json);

        if (parsed is null) return false;

        return parsed.Value.KeyId == original.KeyId
            && parsed.Value.Ciphertext.SequenceEqual(original.Ciphertext)
            && parsed.Value.Nonce.SequenceEqual(original.Nonce)
            && parsed.Value.Tag.SequenceEqual(original.Tag)
            && parsed.Value.Algorithm == original.Algorithm;
    }

    [Property(MaxTest = 100)]
    public bool EncryptedOutput_AlwaysStartsWithMarker(NonEmptyString keyId)
    {
        var value = new EncryptedValue
        {
            KeyId = keyId.Get,
            Ciphertext = ImmutableArray.Create<byte>(1, 2, 3),
            Nonce = ImmutableArray.Create(new byte[12]),
            Tag = ImmutableArray.Create(new byte[16]),
            Algorithm = EncryptionAlgorithm.Aes256Gcm
        };

        var json = EncryptedFieldJsonConverter.Serialize(value);
        return json.StartsWith("{\"__enc\":true", StringComparison.Ordinal);
    }

    [Property(MaxTest = 100)]
    public bool IsEncryptedField_Matches_Serialize(NonEmptyString keyId)
    {
        var value = new EncryptedValue
        {
            KeyId = keyId.Get,
            Ciphertext = ImmutableArray.Create<byte>(42),
            Nonce = ImmutableArray.Create(new byte[12]),
            Tag = ImmutableArray.Create(new byte[16]),
            Algorithm = EncryptionAlgorithm.Aes256Gcm
        };

        var json = EncryptedFieldJsonConverter.Serialize(value);
        return EncryptedFieldJsonConverter.IsEncryptedField(json);
    }

    [Property(MaxTest = 100)]
    public bool IsEncryptedField_RegularString_ReturnsFalse(NonEmptyString input)
    {
        var str = input.Get;
        // Skip strings that happen to start with the marker
        if (str.StartsWith("{\"__enc\":true", StringComparison.Ordinal)) return true;
        return !EncryptedFieldJsonConverter.IsEncryptedField(str);
    }

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
