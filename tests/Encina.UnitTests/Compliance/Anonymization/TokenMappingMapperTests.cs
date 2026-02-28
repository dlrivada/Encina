using Encina.Compliance.Anonymization;
using Encina.Compliance.Anonymization.Model;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.Anonymization;

/// <summary>
/// Unit tests for <see cref="TokenMappingMapper"/> static mapping methods.
/// </summary>
public class TokenMappingMapperTests
{
    [Fact]
    public void ToEntity_ValidMapping_ShouldMapAllProperties()
    {
        // Arrange
        var mapping = CreateMapping();

        // Act
        var entity = TokenMappingMapper.ToEntity(mapping);

        // Assert
        entity.Id.Should().Be(mapping.Id);
        entity.Token.Should().Be(mapping.Token);
        entity.OriginalValueHash.Should().Be(mapping.OriginalValueHash);
        entity.EncryptedOriginalValue.Should().BeEquivalentTo(mapping.EncryptedOriginalValue);
        entity.KeyId.Should().Be(mapping.KeyId);
        entity.CreatedAtUtc.Should().Be(mapping.CreatedAtUtc);
        entity.ExpiresAtUtc.Should().Be(mapping.ExpiresAtUtc);
    }

    [Fact]
    public void ToEntity_ShouldPreserveId()
    {
        // Arrange
        var mapping = CreateMapping();

        // Act
        var entity = TokenMappingMapper.ToEntity(mapping);

        // Assert
        entity.Id.Should().Be(mapping.Id);
    }

    [Fact]
    public void ToEntity_ShouldPreserveEncryptedOriginalValue()
    {
        // Arrange
        var mapping = CreateMapping();

        // Act
        var entity = TokenMappingMapper.ToEntity(mapping);

        // Assert
        entity.EncryptedOriginalValue.Should().Equal(mapping.EncryptedOriginalValue);
    }

    [Fact]
    public void ToEntity_WithExpiresAtUtc_ShouldMap()
    {
        // Arrange
        var mapping = TokenMapping.Create(
            token: "tok-test-456",
            originalValueHash: "hash-def",
            encryptedOriginalValue: [0x05, 0x06],
            keyId: "key-002",
            expiresAtUtc: new DateTimeOffset(2028, 3, 1, 0, 0, 0, TimeSpan.Zero));

        // Act
        var entity = TokenMappingMapper.ToEntity(mapping);

        // Assert
        entity.ExpiresAtUtc.Should().Be(new DateTimeOffset(2028, 3, 1, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void ToEntity_WithNullExpiresAtUtc_ShouldMapNull()
    {
        // Arrange
        var mapping = TokenMapping.Create(
            token: "tok-no-expire",
            originalValueHash: "hash-no-expire",
            encryptedOriginalValue: [0x07],
            keyId: "key-003");

        // Act
        var entity = TokenMappingMapper.ToEntity(mapping);

        // Assert
        entity.ExpiresAtUtc.Should().BeNull();
    }

    [Fact]
    public void ToDomain_ValidEntity_ShouldMapAllProperties()
    {
        // Arrange
        var entity = CreateEntity();

        // Act
        var domain = TokenMappingMapper.ToDomain(entity);

        // Assert
        domain.Id.Should().Be(entity.Id);
        domain.Token.Should().Be(entity.Token);
        domain.OriginalValueHash.Should().Be(entity.OriginalValueHash);
        domain.EncryptedOriginalValue.Should().BeEquivalentTo(entity.EncryptedOriginalValue);
        domain.KeyId.Should().Be(entity.KeyId);
        domain.CreatedAtUtc.Should().Be(entity.CreatedAtUtc);
        domain.ExpiresAtUtc.Should().Be(entity.ExpiresAtUtc);
    }

    [Fact]
    public void ToDomain_ShouldPreserveEncryptedOriginalValue()
    {
        // Arrange
        var entity = CreateEntity();

        // Act
        var domain = TokenMappingMapper.ToDomain(entity);

        // Assert
        domain.EncryptedOriginalValue.Should().Equal(entity.EncryptedOriginalValue);
    }

    [Fact]
    public void Roundtrip_ToEntityThenToDomain_ShouldPreserveAllFields()
    {
        // Arrange
        var original = CreateMapping();

        // Act
        var entity = TokenMappingMapper.ToEntity(original);
        var roundtripped = TokenMappingMapper.ToDomain(entity);

        // Assert
        roundtripped.Id.Should().Be(original.Id);
        roundtripped.Token.Should().Be(original.Token);
        roundtripped.OriginalValueHash.Should().Be(original.OriginalValueHash);
        roundtripped.EncryptedOriginalValue.Should().Equal(original.EncryptedOriginalValue);
        roundtripped.KeyId.Should().Be(original.KeyId);
        roundtripped.CreatedAtUtc.Should().Be(original.CreatedAtUtc);
        roundtripped.ExpiresAtUtc.Should().Be(original.ExpiresAtUtc);
    }

    private static TokenMapping CreateMapping() =>
        TokenMapping.Create(
            token: "tok-test-123",
            originalValueHash: "hash-abc",
            encryptedOriginalValue: [0x01, 0x02, 0x03, 0x04],
            keyId: "key-001",
            expiresAtUtc: new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero));

    private static TokenMappingEntity CreateEntity() => new()
    {
        Id = "entity-001",
        Token = "tok-entity-123",
        OriginalValueHash = "hash-xyz",
        EncryptedOriginalValue = [0x0A, 0x0B, 0x0C],
        KeyId = "key-002",
        CreatedAtUtc = new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.Zero),
        ExpiresAtUtc = new DateTimeOffset(2027, 6, 15, 10, 0, 0, TimeSpan.Zero)
    };
}
