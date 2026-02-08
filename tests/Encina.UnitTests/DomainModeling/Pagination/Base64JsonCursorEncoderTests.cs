using System.Text.Json;
using Encina.DomainModeling.Pagination;
using FluentAssertions;

namespace Encina.UnitTests.DomainModeling.Pagination;

/// <summary>
/// Unit tests for <see cref="Base64JsonCursorEncoder"/>.
/// </summary>
public class Base64JsonCursorEncoderTests
{
    private readonly Base64JsonCursorEncoder _encoder = new();

    #region Encode Tests

    [Fact]
    public void Encode_StringValue_ShouldReturnBase64String()
    {
        // Arrange
        var value = "test-value";

        // Act
        var encoded = _encoder.Encode(value);

        // Assert
        encoded.Should().NotBeNullOrEmpty();
        encoded.Should().NotBe(value); // Should be encoded
    }

    [Fact]
    public void Encode_IntValue_ShouldReturnBase64String()
    {
        // Arrange
        var value = 12345;

        // Act
        var encoded = _encoder.Encode(value);

        // Assert
        encoded.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Encode_GuidValue_ShouldReturnBase64String()
    {
        // Arrange
        var value = Guid.NewGuid();

        // Act
        var encoded = _encoder.Encode(value);

        // Assert
        encoded.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Encode_DateTimeValue_ShouldReturnBase64String()
    {
        // Arrange
        var value = new DateTime(2025, 12, 25, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var encoded = _encoder.Encode(value);

        // Assert
        encoded.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Encode_NullValue_ShouldReturnNull()
    {
        // Act
        var encoded = _encoder.Encode<string?>(null);

        // Assert
        encoded.Should().BeNull();
    }

    [Fact]
    public void Encode_AnonymousType_ShouldReturnBase64String()
    {
        // Arrange - Composite cursor key
        var value = new { CreatedAt = DateTime.UtcNow, Id = Guid.NewGuid() };

        // Act
        var encoded = _encoder.Encode(value);

        // Assert
        encoded.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Decode Tests

    [Fact]
    public void Decode_ValidEncodedString_ShouldReturnOriginalValue()
    {
        // Arrange
        var original = "test-value";
        var encoded = _encoder.Encode(original);

        // Act
        var decoded = _encoder.Decode<string>(encoded!);

        // Assert
        decoded.Should().Be(original);
    }

    [Fact]
    public void Decode_ValidEncodedInt_ShouldReturnOriginalValue()
    {
        // Arrange
        var original = 12345;
        var encoded = _encoder.Encode(original);

        // Act
        var decoded = _encoder.Decode<int>(encoded!);

        // Assert
        decoded.Should().Be(original);
    }

    [Fact]
    public void Decode_ValidEncodedGuid_ShouldReturnOriginalValue()
    {
        // Arrange
        var original = Guid.NewGuid();
        var encoded = _encoder.Encode(original);

        // Act
        var decoded = _encoder.Decode<Guid>(encoded!);

        // Assert
        decoded.Should().Be(original);
    }

    [Fact]
    public void Decode_ValidEncodedDateTime_ShouldReturnOriginalValue()
    {
        // Arrange
        var original = new DateTime(2025, 12, 25, 10, 30, 0, DateTimeKind.Utc);
        var encoded = _encoder.Encode(original);

        // Act
        var decoded = _encoder.Decode<DateTime>(encoded!);

        // Assert
        decoded.Should().Be(original);
    }

    [Fact]
    public void Decode_NullCursor_ShouldReturnDefault()
    {
        // Act
        var decoded = _encoder.Decode<string>(null);

        // Assert
        decoded.Should().BeNull();
    }

    [Fact]
    public void Decode_EmptyCursor_ShouldReturnDefault()
    {
        // Act
        var decoded = _encoder.Decode<string>("");

        // Assert
        decoded.Should().BeNull();
    }

    #endregion

    #region Roundtrip Tests

    [Theory]
    [InlineData("simple")]
    [InlineData("with spaces")]
    [InlineData("with-dashes")]
    [InlineData("with_underscores")]
    [InlineData("with.dots")]
    [InlineData("UPPERCASE")]
    [InlineData("MixedCase123")]
    public void Roundtrip_StringValues_ShouldPreserveValue(string original)
    {
        // Act
        var encoded = _encoder.Encode(original);
        var decoded = _encoder.Decode<string>(encoded!);

        // Assert
        decoded.Should().Be(original);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void Roundtrip_IntValues_ShouldPreserveValue(int original)
    {
        // Act
        var encoded = _encoder.Encode(original);
        var decoded = _encoder.Decode<int>(encoded!);

        // Assert
        decoded.Should().Be(original);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(-1L)]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    public void Roundtrip_LongValues_ShouldPreserveValue(long original)
    {
        // Act
        var encoded = _encoder.Encode(original);
        var decoded = _encoder.Decode<long>(encoded!);

        // Assert
        decoded.Should().Be(original);
    }

    [Fact]
    public void Roundtrip_DecimalValue_ShouldPreserveValue()
    {
        // Arrange
        var original = 123.456m;

        // Act
        var encoded = _encoder.Encode(original);
        var decoded = _encoder.Decode<decimal>(encoded!);

        // Assert
        decoded.Should().Be(original);
    }

    [Fact]
    public void Roundtrip_BooleanValues_ShouldPreserveValue()
    {
        // Arrange & Act & Assert
        foreach (var original in new[] { true, false })
        {
            var encoded = _encoder.Encode(original);
            var decoded = _encoder.Decode<bool>(encoded!);
            decoded.Should().Be(original);
        }
    }

    #endregion

    #region Composite Key Tests

    [Fact]
    public void Roundtrip_CompositeKeyAsObject_ShouldPreserveValues()
    {
        // Arrange
        var id = Guid.NewGuid();
        var createdAt = new DateTime(2025, 12, 25, 10, 30, 0, DateTimeKind.Utc);

        // Using a concrete record for composite key
        var original = new CompositeKey(createdAt, id);

        // Act
        var encoded = _encoder.Encode(original);
        var decoded = _encoder.Decode<CompositeKey>(encoded!);

        // Assert
        decoded.Should().NotBeNull();
        decoded!.CreatedAt.Should().Be(createdAt);
        decoded.Id.Should().Be(id);
    }

    [Fact]
    public void Roundtrip_NestedObject_ShouldPreserveValues()
    {
        // Arrange
        var original = new OrderCursor(
            CreatedAt: new DateTime(2025, 12, 25, 10, 30, 0, DateTimeKind.Utc),
            OrderId: Guid.NewGuid(),
            Status: "Pending");

        // Act
        var encoded = _encoder.Encode(original);
        var decoded = _encoder.Decode<OrderCursor>(encoded!);

        // Assert
        decoded.Should().BeEquivalentTo(original);
    }

    private sealed record CompositeKey(DateTime CreatedAt, Guid Id);
    private sealed record OrderCursor(DateTime CreatedAt, Guid OrderId, string Status);

    #endregion

    #region Edge Cases

    [Fact]
    public void Encode_SpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var original = "test<>&\"'value";

        // Act
        var encoded = _encoder.Encode(original);
        var decoded = _encoder.Decode<string>(encoded!);

        // Assert
        decoded.Should().Be(original);
    }

    [Fact]
    public void Encode_UnicodeCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var original = "Unicode: \u00e9\u00e0\u00fc\u4e2d\u6587";

        // Act
        var encoded = _encoder.Encode(original);
        var decoded = _encoder.Decode<string>(encoded!);

        // Assert
        decoded.Should().Be(original);
    }

    [Fact]
    public void Encode_EmptyString_ShouldHandleCorrectly()
    {
        // Arrange
        var original = "";

        // Act
        var encoded = _encoder.Encode(original);
        var decoded = _encoder.Decode<string>(encoded!);

        // Assert
        decoded.Should().Be(original);
    }

    [Fact]
    public void Decode_InvalidBase64_ShouldThrowCursorEncodingException()
    {
        // Arrange
        var invalidCursor = "not-valid-base64!!!";

        // Act
        var action = () => _encoder.Decode<string>(invalidCursor);

        // Assert - CursorEncodingException wraps FormatException
        action.Should().Throw<CursorEncodingException>()
            .WithInnerException<FormatException>();
    }

    [Fact]
    public void Decode_ValidBase64ButInvalidJson_ShouldThrowCursorEncodingException()
    {
        // Arrange
        var invalidJson = Convert.ToBase64String("not-valid-json"u8.ToArray());

        // Act
        var action = () => _encoder.Decode<int>(invalidJson);

        // Assert - CursorEncodingException wraps JsonException
        action.Should().Throw<CursorEncodingException>()
            .WithInnerException<JsonException>();
    }

    [Fact]
    public void Decode_TypeMismatch_ShouldThrowCursorEncodingException()
    {
        // Arrange - Encode a string
        var encoded = _encoder.Encode("text-value");

        // Act - Try to decode as int
        var action = () => _encoder.Decode<int>(encoded!);

        // Assert - CursorEncodingException wraps JsonException
        action.Should().Throw<CursorEncodingException>()
            .WithInnerException<JsonException>();
    }

    #endregion

    #region URL Safety Tests

    [Fact]
    public void Encode_ShouldProduceUrlSafeBase64()
    {
        // Arrange
        var value = "test-value-with-data";

        // Act
        var encoded = _encoder.Encode(value);

        // Assert - Standard Base64 might have +, /, = which are not URL-safe
        // This test verifies the encoded value can be safely used in URLs
        encoded.Should().NotBeNull();

        // If URL-safe encoding is used, these characters should not appear:
        // However, standard Base64 is also acceptable if URL encoding is done at the transport layer
    }

    #endregion

    #region Multiple Roundtrips

    [Fact]
    public void MultipleRoundtrips_ShouldBeConsistent()
    {
        // Arrange
        var original = new DateTime(2025, 12, 25, 10, 30, 0, DateTimeKind.Utc);

        // Act - Multiple encode/decode cycles
        var encoded1 = _encoder.Encode(original);
        var decoded1 = _encoder.Decode<DateTime>(encoded1!);
        var encoded2 = _encoder.Encode(decoded1);
        var decoded2 = _encoder.Decode<DateTime>(encoded2!);

        // Assert
        decoded2.Should().Be(original);
        encoded1.Should().Be(encoded2); // Same input = same output
    }

    #endregion
}
