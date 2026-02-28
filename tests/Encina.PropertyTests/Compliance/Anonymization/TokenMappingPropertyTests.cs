using Encina.Compliance.Anonymization.Model;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.Anonymization;

/// <summary>
/// Property-based tests for <see cref="TokenMapping"/> verifying domain invariants
/// using FsCheck random data generation.
/// </summary>
public class TokenMappingPropertyTests
{
    #region Create Factory Invariants

    /// <summary>
    /// Invariant: TokenMapping.Create always generates a non-empty Id (GUID format without hyphens).
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Create_AlwaysGeneratesNonEmptyId(
        NonEmptyString token,
        NonEmptyString hash,
        NonEmptyString keyId)
    {
        var mapping = TokenMapping.Create(
            token: token.Get,
            originalValueHash: hash.Get,
            encryptedOriginalValue: new byte[] { 1, 2, 3, 4 },
            keyId: keyId.Get);

        return !string.IsNullOrWhiteSpace(mapping.Id);
    }

    /// <summary>
    /// Invariant: The Token property always matches the token parameter passed to Create.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Create_PreservesToken(
        NonEmptyString token,
        NonEmptyString hash,
        NonEmptyString keyId)
    {
        var mapping = TokenMapping.Create(
            token: token.Get,
            originalValueHash: hash.Get,
            encryptedOriginalValue: new byte[] { 5, 6, 7 },
            keyId: keyId.Get);

        return mapping.Token == token.Get;
    }

    /// <summary>
    /// Invariant: The OriginalValueHash property always matches the hash parameter passed to Create.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Create_PreservesOriginalValueHash(
        NonEmptyString token,
        NonEmptyString hash,
        NonEmptyString keyId)
    {
        var mapping = TokenMapping.Create(
            token: token.Get,
            originalValueHash: hash.Get,
            encryptedOriginalValue: new byte[] { 10, 20 },
            keyId: keyId.Get);

        return mapping.OriginalValueHash == hash.Get;
    }

    /// <summary>
    /// Invariant: The KeyId property always matches the keyId parameter passed to Create.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Create_PreservesKeyId(
        NonEmptyString token,
        NonEmptyString hash,
        NonEmptyString keyId)
    {
        var mapping = TokenMapping.Create(
            token: token.Get,
            originalValueHash: hash.Get,
            encryptedOriginalValue: new byte[] { 99 },
            keyId: keyId.Get);

        return mapping.KeyId == keyId.Get;
    }

    /// <summary>
    /// Invariant: CreatedAtUtc is always set to approximately the current UTC time
    /// (within a 5-second tolerance to account for test execution time).
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Create_SetsCreatedAtUtcToCurrentTime(
        NonEmptyString token,
        NonEmptyString hash,
        NonEmptyString keyId)
    {
        var before = DateTimeOffset.UtcNow;

        var mapping = TokenMapping.Create(
            token: token.Get,
            originalValueHash: hash.Get,
            encryptedOriginalValue: new byte[] { 1 },
            keyId: keyId.Get);

        var after = DateTimeOffset.UtcNow;

        return mapping.CreatedAtUtc >= before && mapping.CreatedAtUtc <= after;
    }

    /// <summary>
    /// Invariant: ExpiresAtUtc defaults to null when not provided to Create.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Create_ExpiresAtUtcDefaultsToNull(
        NonEmptyString token,
        NonEmptyString hash,
        NonEmptyString keyId)
    {
        var mapping = TokenMapping.Create(
            token: token.Get,
            originalValueHash: hash.Get,
            encryptedOriginalValue: new byte[] { 1, 2 },
            keyId: keyId.Get);

        return mapping.ExpiresAtUtc is null;
    }

    #endregion
}
