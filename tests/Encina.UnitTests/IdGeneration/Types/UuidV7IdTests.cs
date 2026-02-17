using Encina.IdGeneration;

namespace Encina.UnitTests.IdGeneration.Types;

/// <summary>
/// Unit tests for <see cref="UuidV7Id"/>.
/// </summary>
public sealed class UuidV7IdTests
{
    // ────────────────────────────────────────────────────────────
    //  Construction
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Default_IsEmpty()
    {
        var id = default(UuidV7Id);
        id.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void Empty_ReturnsDefaultValue()
    {
        UuidV7Id.Empty.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void NewUuidV7_ReturnsNonEmptyId()
    {
        var id = UuidV7Id.NewUuidV7();
        id.IsEmpty.ShouldBeFalse();
    }

    // ────────────────────────────────────────────────────────────
    //  RFC 9562 compliance
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void NewUuidV7_SetsVersionTo7()
    {
        var id = UuidV7Id.NewUuidV7();
        Span<byte> bytes = stackalloc byte[16];
        id.Value.TryWriteBytes(bytes, bigEndian: true, out _);

        (bytes[6] & 0xF0).ShouldBe(0x70);
    }

    [Fact]
    public void NewUuidV7_SetsRFC4122Variant()
    {
        var id = UuidV7Id.NewUuidV7();
        Span<byte> bytes = stackalloc byte[16];
        id.Value.TryWriteBytes(bytes, bigEndian: true, out _);

        (bytes[8] & 0xC0).ShouldBe(0x80);
    }

    // ────────────────────────────────────────────────────────────
    //  Timestamp encoding
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void NewUuidV7_WithTimestamp_EncodesCorrectly()
    {
        var timestamp = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var id = UuidV7Id.NewUuidV7(timestamp);

        var extracted = id.GetTimestamp();
        extracted.ToUnixTimeMilliseconds().ShouldBe(timestamp.ToUnixTimeMilliseconds());
    }

    // ────────────────────────────────────────────────────────────
    //  Parse / TryParse
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_ValidGuidString_ReturnsUuidV7Id()
    {
        var guid = Guid.NewGuid();
        var id = UuidV7Id.Parse(guid.ToString());
        id.Value.ShouldBe(guid);
    }

    [Fact]
    public void Parse_InvalidString_ThrowsFormatException()
    {
        Should.Throw<FormatException>(() => UuidV7Id.Parse("not-a-guid"));
    }

    [Fact]
    public void TryParse_ValidString_ReturnsTrue()
    {
        var guid = Guid.NewGuid();
        UuidV7Id.TryParse(guid.ToString(), out var result).ShouldBeTrue();
        result.Value.ShouldBe(guid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("invalid")]
    [InlineData("")]
    public void TryParse_InvalidString_ReturnsFalse(string? input)
    {
        UuidV7Id.TryParse(input, out _).ShouldBeFalse();
    }

    [Fact]
    public void TryParseEither_ValidString_ReturnsRight()
    {
        var id = UuidV7Id.NewUuidV7();
        var result = UuidV7Id.TryParseEither(id.ToString());
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void TryParseEither_InvalidString_ReturnsLeft()
    {
        var result = UuidV7Id.TryParseEither("invalid");
        result.IsLeft.ShouldBeTrue();
    }

    // ────────────────────────────────────────────────────────────
    //  Guid conversion
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void ImplicitConversion_ToGuid()
    {
        var original = UuidV7Id.NewUuidV7();
        Guid guid = original;
        guid.ShouldBe(original.Value);
    }

    [Fact]
    public void ImplicitConversion_FromGuid()
    {
        var guid = Guid.NewGuid();
        UuidV7Id id = guid;
        id.Value.ShouldBe(guid);
    }

    // ────────────────────────────────────────────────────────────
    //  Equality
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Equals_SameValue_ReturnsTrue()
    {
        var guid = Guid.NewGuid();
        var a = new UuidV7Id(guid);
        var b = new UuidV7Id(guid);
        a.ShouldBe(b);
    }

    [Fact]
    public void Equals_DifferentValue_ReturnsFalse()
    {
        var a = UuidV7Id.NewUuidV7();
        var b = UuidV7Id.NewUuidV7();
        a.ShouldNotBe(b);
    }

    // ────────────────────────────────────────────────────────────
    //  Comparison
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void CompareTo_LaterTimestamp_IsGreater()
    {
        var t1 = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var t2 = t1.AddSeconds(10);
        var id1 = UuidV7Id.NewUuidV7(t1);
        var id2 = UuidV7Id.NewUuidV7(t2);

        id2.CompareTo(id1).ShouldBeGreaterThan(0);
    }

    [Fact]
    public void CompareTo_Null_Returns1()
    {
        var id = UuidV7Id.NewUuidV7();
        id.CompareTo(null).ShouldBe(1);
    }

    [Fact]
    public void CompareTo_WrongType_ThrowsArgumentException()
    {
        var id = UuidV7Id.NewUuidV7();
        Should.Throw<ArgumentException>(() => id.CompareTo("not-a-uuid"));
    }

    // ────────────────────────────────────────────────────────────
    //  ToString
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void ToString_ReturnsGuidFormat()
    {
        var id = UuidV7Id.NewUuidV7();
        Guid.TryParse(id.ToString(), out _).ShouldBeTrue();
    }
}
