using System.Collections.Immutable;

using Encina.Marten.GDPR;
using Encina.Security.Encryption;

using Shouldly;

namespace Encina.UnitTests.Marten.GDPR;

public sealed class EncryptedFieldJsonConverterTests
{
    [Fact]
    public void Serialize_ProducesValidJson()
    {
        // Arrange
        var encryptedValue = new EncryptedValue
        {
            KeyId = "subject:user-1:v1",
            Ciphertext = [1, 2, 3, 4, 5],
            Nonce = [10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21],
            Tag = [30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45],
            Algorithm = EncryptionAlgorithm.Aes256Gcm
        };

        // Act
        var json = EncryptedFieldJsonConverter.Serialize(encryptedValue);

        // Assert
        json.ShouldStartWith("{\"__enc\":true");
        json.ShouldContain("\"kid\":\"subject:user-1:v1\"");
        json.ShouldContain("\"v\":1");
        json.ShouldContain("\"alg\":0");
    }

    [Fact]
    public void Roundtrip_Serialize_ThenParse_RestoresValue()
    {
        // Arrange
        var original = new EncryptedValue
        {
            KeyId = "subject:user-42:v3",
            Ciphertext = [100, 200, 150, 50],
            Nonce = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12],
            Tag = [20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35],
            Algorithm = EncryptionAlgorithm.Aes256Gcm
        };

        // Act
        var json = EncryptedFieldJsonConverter.Serialize(original);
        var parsed = EncryptedFieldJsonConverter.TryParse(json);

        // Assert
        parsed.ShouldNotBeNull();
        parsed.Value.KeyId.ShouldBe(original.KeyId);
        parsed.Value.Ciphertext.SequenceEqual(original.Ciphertext).ShouldBeTrue();
        parsed.Value.Nonce.SequenceEqual(original.Nonce).ShouldBeTrue();
        parsed.Value.Tag.SequenceEqual(original.Tag).ShouldBeTrue();
        parsed.Value.Algorithm.ShouldBe(original.Algorithm);
    }

    [Fact]
    public void TryParse_NullInput_ReturnsNull()
    {
        // Act
        var result = EncryptedFieldJsonConverter.TryParse(null);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void TryParse_EmptyString_ReturnsNull()
    {
        // Act
        var result = EncryptedFieldJsonConverter.TryParse("");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void TryParse_RegularString_ReturnsNull()
    {
        // Act
        var result = EncryptedFieldJsonConverter.TryParse("hello world");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void TryParse_RegularJson_ReturnsNull()
    {
        // Act
        var result = EncryptedFieldJsonConverter.TryParse("{\"name\":\"John\"}");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void TryParse_MalformedEncryptedJson_ReturnsNull()
    {
        // Act — has marker but missing required fields
        var result = EncryptedFieldJsonConverter.TryParse("{\"__enc\":true}");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void TryParse_InvalidBase64_ReturnsNull()
    {
        // Act
        var result = EncryptedFieldJsonConverter.TryParse(
            "{\"__enc\":true,\"v\":1,\"kid\":\"key\",\"ct\":\"not-valid-base64!!!\",\"n\":\"also-bad\"}");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void IsEncryptedField_WithEncryptedValue_ReturnsTrue()
    {
        // Act
        var result = EncryptedFieldJsonConverter.IsEncryptedField(
            "{\"__enc\":true,\"v\":1,\"kid\":\"key\",\"ct\":\"YQ==\",\"n\":\"YQ==\"}");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsEncryptedField_WithNull_ReturnsFalse()
    {
        // Act
        var result = EncryptedFieldJsonConverter.IsEncryptedField(null);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsEncryptedField_WithRegularString_ReturnsFalse()
    {
        // Act
        var result = EncryptedFieldJsonConverter.IsEncryptedField("plain text");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Serialize_CompactFormat_ContainsAllFields()
    {
        // Arrange
        var value = new EncryptedValue
        {
            KeyId = "k1",
            Ciphertext = [1],
            Nonce = [2],
            Tag = [3],
            Algorithm = EncryptionAlgorithm.Aes256Gcm
        };

        // Act
        var json = EncryptedFieldJsonConverter.Serialize(value);

        // Assert
        json.ShouldContain("\"__enc\":true");
        json.ShouldContain("\"v\":");
        json.ShouldContain("\"kid\":");
        json.ShouldContain("\"ct\":");
        json.ShouldContain("\"n\":");
        json.ShouldContain("\"t\":");
        json.ShouldContain("\"alg\":");
    }

    [Fact]
    public void TryParse_MissingTag_StillParsesWithEmptyTag()
    {
        // Arrange — valid encrypted JSON without 't' field
        var json = "{\"__enc\":true,\"v\":1,\"kid\":\"k1\",\"ct\":\"AQ==\",\"n\":\"Ag==\"}";

        // Act
        var result = EncryptedFieldJsonConverter.TryParse(json);

        // Assert
        result.ShouldNotBeNull();
        result.Value.Tag.Length.ShouldBe(0);
    }
}
