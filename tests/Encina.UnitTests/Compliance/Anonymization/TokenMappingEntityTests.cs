using Encina.Compliance.Anonymization;

using Shouldly;

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
        entity.Id.ShouldBe(id);
        entity.Token.ShouldBe(token);
        entity.OriginalValueHash.ShouldBe(hash);
        entity.EncryptedOriginalValue.ShouldBe(encrypted);
        entity.KeyId.ShouldBe(keyId);
        entity.CreatedAtUtc.ShouldBe(created);
        entity.ExpiresAtUtc.ShouldBe(expires);
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
        entity.CreatedAtUtc.ShouldBe(default(DateTimeOffset));
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
        entity.ExpiresAtUtc.ShouldBeNull();
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
        entity.ExpiresAtUtc.ShouldBe(expires);
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
        entity.Id.ShouldBe("required-id");
        entity.Token.ShouldBe("required-token");
        entity.OriginalValueHash.ShouldBe("required-hash");
        entity.EncryptedOriginalValue.ShouldBe<byte[]>([0xAA, 0xBB]);
        entity.KeyId.ShouldBe("required-key");
    }
}
