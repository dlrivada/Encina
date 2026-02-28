using Encina.Compliance.Anonymization.Model;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.Anonymization;

/// <summary>
/// Unit tests for <see cref="TokenMapping"/> factory method and record behavior.
/// </summary>
public class TokenMappingTests
{
    #region Create Factory Method Tests

    [Fact]
    public void Create_ShouldGenerateNonEmptyId()
    {
        // Act
        var mapping = TokenMapping.Create(
            token: "tok_abc123",
            originalValueHash: "hash_value",
            encryptedOriginalValue: [0x01, 0x02, 0x03],
            keyId: "key-001");

        // Assert
        mapping.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Create_ShouldSetToken()
    {
        // Act
        var mapping = TokenMapping.Create(
            token: "tok_abc123",
            originalValueHash: "hash_value",
            encryptedOriginalValue: [0x01, 0x02, 0x03],
            keyId: "key-001");

        // Assert
        mapping.Token.Should().Be("tok_abc123");
    }

    [Fact]
    public void Create_ShouldSetOriginalValueHash()
    {
        // Act
        var mapping = TokenMapping.Create(
            token: "tok_abc123",
            originalValueHash: "hmac_sha256_base64_hash",
            encryptedOriginalValue: [0x01, 0x02, 0x03],
            keyId: "key-001");

        // Assert
        mapping.OriginalValueHash.Should().Be("hmac_sha256_base64_hash");
    }

    [Fact]
    public void Create_ShouldSetEncryptedOriginalValue()
    {
        // Arrange
        byte[] encrypted = [0xAA, 0xBB, 0xCC, 0xDD];

        // Act
        var mapping = TokenMapping.Create(
            token: "tok_abc123",
            originalValueHash: "hash_value",
            encryptedOriginalValue: encrypted,
            keyId: "key-001");

        // Assert
        mapping.EncryptedOriginalValue.Should().BeEquivalentTo(encrypted);
    }

    [Fact]
    public void Create_ShouldSetKeyId()
    {
        // Act
        var mapping = TokenMapping.Create(
            token: "tok_abc123",
            originalValueHash: "hash_value",
            encryptedOriginalValue: [0x01, 0x02, 0x03],
            keyId: "key-2025-01");

        // Assert
        mapping.KeyId.Should().Be("key-2025-01");
    }

    [Fact]
    public void Create_ShouldSetCreatedAtUtcToNow()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var mapping = TokenMapping.Create(
            token: "tok_abc123",
            originalValueHash: "hash_value",
            encryptedOriginalValue: [0x01, 0x02, 0x03],
            keyId: "key-001");

        var after = DateTimeOffset.UtcNow;

        // Assert
        mapping.CreatedAtUtc.Should().BeOnOrAfter(before);
        mapping.CreatedAtUtc.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Create_WithExpiresAtUtc_ShouldSetExpiration()
    {
        // Arrange
        var expiration = new DateTimeOffset(2027, 6, 15, 0, 0, 0, TimeSpan.Zero);

        // Act
        var mapping = TokenMapping.Create(
            token: "tok_abc123",
            originalValueHash: "hash_value",
            encryptedOriginalValue: [0x01, 0x02, 0x03],
            keyId: "key-001",
            expiresAtUtc: expiration);

        // Assert
        mapping.ExpiresAtUtc.Should().Be(expiration);
    }

    [Fact]
    public void Create_WithoutExpiresAtUtc_ShouldBeNull()
    {
        // Act
        var mapping = TokenMapping.Create(
            token: "tok_abc123",
            originalValueHash: "hash_value",
            encryptedOriginalValue: [0x01, 0x02, 0x03],
            keyId: "key-001");

        // Assert
        mapping.ExpiresAtUtc.Should().BeNull();
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        byte[] encrypted = [0x01, 0x02, 0x03];

        var mapping1 = new TokenMapping
        {
            Id = "id-001",
            Token = "tok_abc123",
            OriginalValueHash = "hash_value",
            EncryptedOriginalValue = encrypted,
            KeyId = "key-001",
            CreatedAtUtc = now
        };

        var mapping2 = new TokenMapping
        {
            Id = "id-001",
            Token = "tok_abc123",
            OriginalValueHash = "hash_value",
            EncryptedOriginalValue = encrypted,
            KeyId = "key-001",
            CreatedAtUtc = now
        };

        // Assert
        mapping1.Should().Be(mapping2);
    }

    #endregion
}
