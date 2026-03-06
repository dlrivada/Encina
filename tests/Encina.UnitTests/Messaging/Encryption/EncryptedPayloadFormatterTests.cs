using System.Collections.Immutable;
using Encina.Messaging.Encryption;
using Encina.Messaging.Encryption.Model;
using FluentAssertions;

namespace Encina.UnitTests.Messaging.Encryption;

public class EncryptedPayloadFormatterTests
{
    [Fact]
    public void Format_ValidPayload_ReturnsExpectedString()
    {
        // Arrange
        var payload = CreatePayload("test-key", "AES-256-GCM", [1, 2, 3], [4, 5, 6], [7, 8, 9]);

        // Act
        var result = EncryptedPayloadFormatter.Format(payload);

        // Assert
        result.Should().StartWith("ENC:v1:test-key:AES-256-GCM:");
        result.Split(':').Should().HaveCount(7);
    }

    [Fact]
    public void Format_NullPayload_ThrowsArgumentNullException()
    {
        var act = () => EncryptedPayloadFormatter.Format(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("payload");
    }

    [Fact]
    public void TryParse_ValidV1String_ReturnsTrueWithPayload()
    {
        // Arrange
        var original = CreatePayload("my-key", "AES-256-GCM", [10, 20], [30, 40], [50, 60]);
        var formatted = EncryptedPayloadFormatter.Format(original);

        // Act
        var result = EncryptedPayloadFormatter.TryParse(formatted, out var parsed);

        // Assert
        result.Should().BeTrue();
        parsed.Should().NotBeNull();
        parsed!.KeyId.Should().Be("my-key");
        parsed.Algorithm.Should().Be("AES-256-GCM");
        parsed.Nonce.ToArray().Should().Equal(10, 20);
        parsed.Tag.ToArray().Should().Equal(30, 40);
        parsed.Ciphertext.ToArray().Should().Equal(50, 60);
        parsed.Version.Should().Be(1);
    }

    [Fact]
    public void TryParse_NullContent_ReturnsFalse()
    {
        EncryptedPayloadFormatter.TryParse(null, out var payload).Should().BeFalse();
        payload.Should().BeNull();
    }

    [Fact]
    public void TryParse_EmptyString_ReturnsFalse()
    {
        EncryptedPayloadFormatter.TryParse(string.Empty, out var payload).Should().BeFalse();
        payload.Should().BeNull();
    }

    [Fact]
    public void TryParse_PlainJson_ReturnsFalse()
    {
        EncryptedPayloadFormatter.TryParse("{\"hello\":\"world\"}", out var payload).Should().BeFalse();
        payload.Should().BeNull();
    }

    [Fact]
    public void TryParse_UnsupportedVersion_ReturnsFalse()
    {
        EncryptedPayloadFormatter.TryParse("ENC:v99:key:algo:a:b:c", out var payload).Should().BeFalse();
        payload.Should().BeNull();
    }

    [Fact]
    public void TryParse_MissingParts_ReturnsFalse()
    {
        EncryptedPayloadFormatter.TryParse("ENC:v1:key:algo", out var payload).Should().BeFalse();
        payload.Should().BeNull();
    }

    [Fact]
    public void TryParse_InvalidBase64_ReturnsFalse()
    {
        EncryptedPayloadFormatter.TryParse("ENC:v1:key:algo:!!!:!!!:!!!", out var payload).Should().BeFalse();
        payload.Should().BeNull();
    }

    [Fact]
    public void TryParse_EmptyKeyId_ReturnsFalse()
    {
        EncryptedPayloadFormatter.TryParse("ENC:v1::algo:YQ==:Yg==:Yw==", out var payload).Should().BeFalse();
        payload.Should().BeNull();
    }

    [Fact]
    public void TryParse_EmptyAlgorithm_ReturnsFalse()
    {
        EncryptedPayloadFormatter.TryParse("ENC:v1:key::YQ==:Yg==:Yw==", out var payload).Should().BeFalse();
        payload.Should().BeNull();
    }

    [Fact]
    public void IsEncrypted_EncryptedContent_ReturnsTrue()
    {
        EncryptedPayloadFormatter.IsEncrypted("ENC:v1:key:algo:a:b:c").Should().BeTrue();
    }

    [Fact]
    public void IsEncrypted_PlainContent_ReturnsFalse()
    {
        EncryptedPayloadFormatter.IsEncrypted("{\"hello\":\"world\"}").Should().BeFalse();
    }

    [Fact]
    public void IsEncrypted_NullContent_ReturnsFalse()
    {
        EncryptedPayloadFormatter.IsEncrypted(null).Should().BeFalse();
    }

    [Fact]
    public void IsEncrypted_EmptyContent_ReturnsFalse()
    {
        EncryptedPayloadFormatter.IsEncrypted(string.Empty).Should().BeFalse();
    }

    [Fact]
    public void FormatThenParse_RoundTrip_PreservesAllFields()
    {
        // Arrange
        var nonce = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
        var tag = new byte[] { 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115 };
        var ciphertext = new byte[] { 200, 201, 202, 203, 204 };
        var original = CreatePayload("roundtrip-key", "AES-256-GCM", nonce, tag, ciphertext);

        // Act
        var formatted = EncryptedPayloadFormatter.Format(original);
        var parsed = EncryptedPayloadFormatter.TryParse(formatted, out var result);

        // Assert
        parsed.Should().BeTrue();
        result!.KeyId.Should().Be(original.KeyId);
        result.Algorithm.Should().Be(original.Algorithm);
        result.Nonce.ToArray().Should().Equal(nonce);
        result.Tag.ToArray().Should().Equal(tag);
        result.Ciphertext.ToArray().Should().Equal(ciphertext);
    }

    private static EncryptedPayload CreatePayload(
        string keyId, string algorithm, byte[] nonce, byte[] tag, byte[] ciphertext) => new()
        {
            KeyId = keyId,
            Algorithm = algorithm,
            Nonce = [.. nonce],
            Tag = [.. tag],
            Ciphertext = [.. ciphertext],
            Version = 1
        };
}
