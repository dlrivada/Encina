using Encina.Compliance.Anonymization;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.Anonymization;

/// <summary>
/// Unit tests for <see cref="TokenMappingEntity"/> persistence entity.
/// </summary>
public class TokenMappingEntityTests
{
    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var id = "entity-001";
        var token = "tok-abc-123";
        var hash = "hash-xyz";
        var encrypted = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var keyId = "key-001";
        var created = new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);
        var expires = new DateTimeOffset(2027, 6, 15, 10, 0, 0, TimeSpan.Zero);

        // Act
        var entity = new TokenMappingEntity
        {
            Id = id,
            Token = token,
            OriginalValueHash = hash,
            EncryptedOriginalValue = encrypted,
            KeyId = keyId,
            CreatedAtUtc = created,
            ExpiresAtUtc = expires
        };

        // Assert
        entity.Id.Should().Be(id);
        entity.Token.Should().Be(token);
        entity.OriginalValueHash.Should().Be(hash);
        entity.EncryptedOriginalValue.Should().BeEquivalentTo(encrypted);
        entity.KeyId.Should().Be(keyId);
        entity.CreatedAtUtc.Should().Be(created);
        entity.ExpiresAtUtc.Should().Be(expires);
    }

    [Fact]
    public void CreatedAtUtc_DefaultValue_ShouldBeDefault()
    {
        // Arrange & Act
        var entity = new TokenMappingEntity
        {
            Id = "id",
            Token = "token",
            OriginalValueHash = "hash",
            EncryptedOriginalValue = [0x01],
            KeyId = "key"
        };

        // Assert
        entity.CreatedAtUtc.Should().Be(default(DateTimeOffset));
    }

    [Fact]
    public void ExpiresAtUtc_DefaultValue_ShouldBeNull()
    {
        // Arrange & Act
        var entity = new TokenMappingEntity
        {
            Id = "id",
            Token = "token",
            OriginalValueHash = "hash",
            EncryptedOriginalValue = [0x01],
            KeyId = "key"
        };

        // Assert
        entity.ExpiresAtUtc.Should().BeNull();
    }

    [Fact]
    public void ExpiresAtUtc_WhenSet_ShouldReturnValue()
    {
        // Arrange
        var expires = new DateTimeOffset(2028, 12, 31, 23, 59, 59, TimeSpan.Zero);

        // Act
        var entity = new TokenMappingEntity
        {
            Id = "id",
            Token = "token",
            OriginalValueHash = "hash",
            EncryptedOriginalValue = [0x01],
            KeyId = "key",
            ExpiresAtUtc = expires
        };

        // Assert
        entity.ExpiresAtUtc.Should().Be(expires);
    }

    [Fact]
    public void RequiredProperties_ShouldBeAccessible()
    {
        // Arrange
        var entity = new TokenMappingEntity
        {
            Id = "required-id",
            Token = "required-token",
            OriginalValueHash = "required-hash",
            EncryptedOriginalValue = [0xAA, 0xBB],
            KeyId = "required-key"
        };

        // Act & Assert
        entity.Id.Should().Be("required-id");
        entity.Token.Should().Be("required-token");
        entity.OriginalValueHash.Should().Be("required-hash");
        entity.EncryptedOriginalValue.Should().Equal(0xAA, 0xBB);
        entity.KeyId.Should().Be("required-key");
    }
}
