using Encina.IdGeneration;

namespace Encina.UnitTests.IdGeneration.Types;

/// <summary>
/// Unit tests for <see cref="UlidId"/>.
/// </summary>
public sealed class UlidIdTests
{
    // ────────────────────────────────────────────────────────────
    //  Construction
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Default_IsDefaultValue()
    {
        var id = default(UlidId);
        id.ShouldBe(default(UlidId));
    }

    [Fact]
    public void Empty_EqualsDefault()
    {
        UlidId.Empty.ShouldBe(default(UlidId));
    }

    [Fact]
    public void NewUlid_IsEmpty_ReturnsFalse()
    {
        var id = UlidId.NewUlid();
        id.IsEmpty.ShouldBeFalse();
    }

    [Fact]
    public void ConstructedWithAllZeros_IsEmpty_ReturnsTrue()
    {
        var id = new UlidId(new byte[16]);
        id.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_InvalidByteLength_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => new UlidId(new byte[8].AsSpan()));
    }

    // ────────────────────────────────────────────────────────────
    //  Timestamp encoding
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void NewUlid_WithTimestamp_EncodesCorrectly()
    {
        var timestamp = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var id = UlidId.NewUlid(timestamp);

        var extracted = id.GetTimestamp();
        extracted.ToUnixTimeMilliseconds().ShouldBe(timestamp.ToUnixTimeMilliseconds());
    }

    // ────────────────────────────────────────────────────────────
    //  String representation
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void ToString_Returns26CharCrockfordBase32()
    {
        var id = UlidId.NewUlid();
        var str = id.ToString();
        str.Length.ShouldBe(26);
        str.ShouldMatch("^[0-9A-HJKMNP-TV-Z]{26}$");
    }

    [Fact]
    public void ToString_Roundtrip_ParseReturnsEqualId()
    {
        var original = UlidId.NewUlid();
        var str = original.ToString();
        var parsed = UlidId.Parse(str);
        parsed.ShouldBe(original);
    }

    // ────────────────────────────────────────────────────────────
    //  Parse / TryParse
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_InvalidString_ThrowsFormatException()
    {
        Should.Throw<FormatException>(() => UlidId.Parse("invalid"));
    }

    [Fact]
    public void TryParse_ValidString_ReturnsTrue()
    {
        var original = UlidId.NewUlid();
        UlidId.TryParse(original.ToString(), out var result).ShouldBeTrue();
        result.ShouldBe(original);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("too-short")]
    [InlineData("!!!invalid!!chars!!invalid")]
    public void TryParse_InvalidString_ReturnsFalse(string? input)
    {
        UlidId.TryParse(input, out _).ShouldBeFalse();
    }

    [Fact]
    public void TryParse_CaseInsensitive()
    {
        var original = UlidId.NewUlid();
        var lower = original.ToString().ToLowerInvariant();
        UlidId.TryParse(lower, out var result).ShouldBeTrue();
        result.ShouldBe(original);
    }

    [Fact]
    public void TryParseEither_ValidString_ReturnsRight()
    {
        var original = UlidId.NewUlid();
        var result = UlidId.TryParseEither(original.ToString());
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void TryParseEither_InvalidString_ReturnsLeft()
    {
        var result = UlidId.TryParseEither("invalid");
        result.IsLeft.ShouldBeTrue();
    }

    // ────────────────────────────────────────────────────────────
    //  Guid conversion
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void ImplicitConversion_ToGuid_Roundtrip()
    {
        var original = UlidId.NewUlid();
        Guid guid = original;
        UlidId back = guid;

        // The bytes should match when round-tripped through Guid
        guid.ShouldNotBe(Guid.Empty);
        back.ToGuid().ShouldBe(guid);
    }

    // ────────────────────────────────────────────────────────────
    //  Equality
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Equals_SameBytes_ReturnsTrue()
    {
        var id = UlidId.NewUlid();
        var clone = UlidId.Parse(id.ToString());
        id.ShouldBe(clone);
    }

    [Fact]
    public void Equals_DifferentIds_ReturnsFalse()
    {
        var a = UlidId.NewUlid();
        var b = UlidId.NewUlid();
        a.ShouldNotBe(b);
    }

    [Fact]
    public void GetHashCode_SameValue_SameHash()
    {
        var id = UlidId.NewUlid();
        var clone = UlidId.Parse(id.ToString());
        id.GetHashCode().ShouldBe(clone.GetHashCode());
    }

    // ────────────────────────────────────────────────────────────
    //  Comparison
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void CompareTo_LaterTimestamp_IsGreater()
    {
        var t1 = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var t2 = t1.AddSeconds(10);
        var id1 = UlidId.NewUlid(t1);
        var id2 = UlidId.NewUlid(t2);

        (id2 > id1).ShouldBeTrue();
    }

    [Fact]
    public void CompareTo_Null_Returns1()
    {
        var id = UlidId.NewUlid();
        id.CompareTo(null).ShouldBe(1);
    }

    [Fact]
    public void CompareTo_WrongType_ThrowsArgumentException()
    {
        var id = UlidId.NewUlid();
        Should.Throw<ArgumentException>(() => id.CompareTo("not-a-ulid"));
    }
}
