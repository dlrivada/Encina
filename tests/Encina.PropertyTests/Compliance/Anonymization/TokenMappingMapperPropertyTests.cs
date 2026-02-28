using Encina.Compliance.Anonymization;
using Encina.Compliance.Anonymization.Model;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.Anonymization;

/// <summary>
/// Property-based tests for <see cref="TokenMappingMapper"/> verifying roundtrip
/// invariants between <see cref="TokenMapping"/> and <see cref="TokenMappingEntity"/>.
/// </summary>
public class TokenMappingMapperPropertyTests
{
    #region Roundtrip Invariants

    /// <summary>
    /// Invariant: Converting domain -> entity -> domain preserves all fields.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool ToEntity_ToDomain_Roundtrip_PreservesAllFields(
        NonEmptyString token,
        NonEmptyString hash,
        NonEmptyString keyId)
    {
        var mapping = TokenMapping.Create(
            token: token.Get,
            originalValueHash: hash.Get,
            encryptedOriginalValue: new byte[] { 1, 2, 3 },
            keyId: keyId.Get);

        var entity = TokenMappingMapper.ToEntity(mapping);
        var roundtripped = TokenMappingMapper.ToDomain(entity);

        return roundtripped.Id == mapping.Id
            && roundtripped.Token == mapping.Token
            && roundtripped.OriginalValueHash == mapping.OriginalValueHash
            && roundtripped.KeyId == mapping.KeyId
            && roundtripped.CreatedAtUtc == mapping.CreatedAtUtc
            && roundtripped.ExpiresAtUtc == mapping.ExpiresAtUtc;
    }

    /// <summary>
    /// Invariant: ToEntity preserves CreatedAtUtc with full DateTimeOffset precision.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool ToEntity_PreservesCreatedAtUtc(
        NonEmptyString token,
        NonEmptyString hash,
        NonEmptyString keyId)
    {
        var mapping = TokenMapping.Create(
            token: token.Get,
            originalValueHash: hash.Get,
            encryptedOriginalValue: new byte[] { 10, 20, 30 },
            keyId: keyId.Get);

        var entity = TokenMappingMapper.ToEntity(mapping);

        return entity.CreatedAtUtc == mapping.CreatedAtUtc;
    }

    /// <summary>
    /// Invariant: Converting entity -> domain -> entity preserves all fields (reverse direction).
    /// </summary>
    [Property(MaxTest = 50)]
    public bool ToDomain_ToEntity_Roundtrip_PreservesAllFields(
        NonEmptyString token,
        NonEmptyString hash,
        NonEmptyString keyId)
    {
        var entity = new TokenMappingEntity
        {
            Id = Guid.NewGuid().ToString("N"),
            Token = token.Get,
            OriginalValueHash = hash.Get,
            EncryptedOriginalValue = new byte[] { 7, 8, 9 },
            KeyId = keyId.Get,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = null
        };

        var domain = TokenMappingMapper.ToDomain(entity);
        var roundtripped = TokenMappingMapper.ToEntity(domain);

        return roundtripped.Id == entity.Id
            && roundtripped.Token == entity.Token
            && roundtripped.OriginalValueHash == entity.OriginalValueHash
            && roundtripped.KeyId == entity.KeyId
            && roundtripped.CreatedAtUtc == entity.CreatedAtUtc
            && roundtripped.ExpiresAtUtc == entity.ExpiresAtUtc;
    }

    /// <summary>
    /// Invariant: EncryptedOriginalValue byte array is preserved exactly through roundtrip.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool ToEntity_EncryptedOriginalValue_PreservedExactly(
        NonEmptyString token,
        NonEmptyString hash,
        NonEmptyString keyId,
        byte[] encryptedValue)
    {
        // Skip null or empty byte arrays since EncryptedOriginalValue is required
        var bytes = encryptedValue is null || encryptedValue.Length == 0
            ? new byte[] { 1 }
            : encryptedValue;

        var mapping = TokenMapping.Create(
            token: token.Get,
            originalValueHash: hash.Get,
            encryptedOriginalValue: bytes,
            keyId: keyId.Get);

        var entity = TokenMappingMapper.ToEntity(mapping);
        var roundtripped = TokenMappingMapper.ToDomain(entity);

        return roundtripped.EncryptedOriginalValue.SequenceEqual(mapping.EncryptedOriginalValue);
    }

    #endregion
}
