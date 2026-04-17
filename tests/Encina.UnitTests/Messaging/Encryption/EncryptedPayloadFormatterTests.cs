using System.Collections.Immutable;
using Encina.Messaging.Encryption;
using Encina.Messaging.Encryption.Model;
using Shouldly;

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
        result.ShouldStartWith("ENC:v1:test-key:AES-256-GCM:");
        result.Split(':').Length.ShouldBe(7);
    }

    [Fact]
    public void Format_NullPayload_ThrowsArgumentNullException()
    {
        Action act = () => EncryptedPayloadFormatter.Format(null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("payload");
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
        result.ShouldBeTrue();
        parsed.ShouldNotBeNull();
        parsed!.KeyId.ShouldBe("my-key");
        parsed.Algorithm.ShouldBe("AES-256-GCM");
        parsed.Nonce.ToArray().ShouldBe([10, 20]);
        parsed.Tag.ToArray().ShouldBe([30, 40]);
        parsed.Ciphertext.ToArray().ShouldBe([50, 60]);
        parsed.Version.ShouldBe(1);
    }

    [Fact]
    public void TryParse_NullContent_ReturnsFalse()
    {
        EncryptedPayloadFormatter.TryParse(null, out var payload).ShouldBeFalse();
        payload.ShouldBeNull();
    }

    [Fact]
    public void TryParse_EmptyString_ReturnsFalse()
    {
        EncryptedPayloadFormatter.TryParse(string.Empty, out var payload).ShouldBeFalse();
        payload.ShouldBeNull();
    }

    [Fact]
    public void TryParse_PlainJson_ReturnsFalse()
    {
        EncryptedPayloadFormatter.TryParse("{\"hello\":\"world\"}", out var payload).ShouldBeFalse();
        payload.ShouldBeNull();
    }

    [Fact]
    public void TryParse_UnsupportedVersion_ReturnsFalse()
    {
        EncryptedPayloadFormatter.TryParse("ENC:v99:key:algo:a:b:c", out var payload).ShouldBeFalse();
        payload.ShouldBeNull();
    }

    [Fact]
    public void TryParse_MissingParts_ReturnsFalse()
    {
        EncryptedPayloadFormatter.TryParse("ENC:v1:key:algo", out var payload).ShouldBeFalse();
        payload.ShouldBeNull();
    }

    [Fact]
    public void TryParse_InvalidBase64_ReturnsFalse()
    {
        EncryptedPayloadFormatter.TryParse("ENC:v1:key:algo:!!!:!!!:!!!", out var payload).ShouldBeFalse();
        payload.ShouldBeNull();
    }

    [Fact]
    public void TryParse_EmptyKeyId_ReturnsFalse()
    {
        EncryptedPayloadFormatter.TryParse("ENC:v1::algo:YQ==:Yg==:Yw==", out var payload).ShouldBeFalse();
        payload.ShouldBeNull();
    }

    [Fact]
    public void TryParse_EmptyAlgorithm_ReturnsFalse()
    {
        EncryptedPayloadFormatter.TryParse("ENC:v1:key::YQ==:Yg==:Yw==", out var payload).ShouldBeFalse();
        payload.ShouldBeNull();
    }

    [Fact]
    public void IsEncrypted_EncryptedContent_ReturnsTrue()
    {
        EncryptedPayloadFormatter.IsEncrypted("ENC:v1:key:algo:a:b:c").ShouldBeTrue();
    }

    [Fact]
    public void IsEncrypted_PlainContent_ReturnsFalse()
    {
        EncryptedPayloadFormatter.IsEncrypted("{\"hello\":\"world\"}").ShouldBeFalse();
    }

    [Fact]
    public void IsEncrypted_NullContent_ReturnsFalse()
    {
        EncryptedPayloadFormatter.IsEncrypted(null).ShouldBeFalse();
    }

    [Fact]
    public void IsEncrypted_EmptyContent_ReturnsFalse()
    {
        EncryptedPayloadFormatter.IsEncrypted(string.Empty).ShouldBeFalse();
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
        parsed.ShouldBeTrue();
        result!.KeyId.ShouldBe(original.KeyId);
        result.Algorithm.ShouldBe(original.Algorithm);
        result.Nonce.ToArray().ShouldBe(nonce);
        result.Tag.ToArray().ShouldBe(tag);
        result.Ciphertext.ToArray().ShouldBe(ciphertext);
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
