using Encina.IdGeneration;

namespace Encina.UnitTests.IdGeneration.Types;

/// <summary>
/// Unit tests for <see cref="SnowflakeId"/>.
/// </summary>
public sealed class SnowflakeIdTests
{
    // ────────────────────────────────────────────────────────────
    //  Construction and Value
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_SetsValue()
    {
        var id = new SnowflakeId(42L);
        id.Value.ShouldBe(42L);
    }

    [Fact]
    public void Default_IsEmpty()
    {
        var id = default(SnowflakeId);
        id.IsEmpty.ShouldBeTrue();
        id.Value.ShouldBe(0L);
    }

    [Fact]
    public void Empty_ReturnsDefaultValue()
    {
        SnowflakeId.Empty.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void NonZeroValue_IsNotEmpty()
    {
        var id = new SnowflakeId(1L);
        id.IsEmpty.ShouldBeFalse();
    }

    // ────────────────────────────────────────────────────────────
    //  Implicit conversions
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void ImplicitConversion_ToLong()
    {
        SnowflakeId id = new(123L);
        long value = id;
        value.ShouldBe(123L);
    }

    [Fact]
    public void ImplicitConversion_FromLong()
    {
        SnowflakeId id = 456L;
        id.Value.ShouldBe(456L);
    }

    // ────────────────────────────────────────────────────────────
    //  Parse / TryParse
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_ValidString_ReturnsSnowflakeId()
    {
        var id = SnowflakeId.Parse("123456789");
        id.Value.ShouldBe(123456789L);
    }

    [Fact]
    public void Parse_InvalidString_ThrowsFormatException()
    {
        Should.Throw<FormatException>(() => SnowflakeId.Parse("not-a-number"));
    }

    [Fact]
    public void TryParse_ValidString_ReturnsTrue()
    {
        SnowflakeId.TryParse("123", out var result).ShouldBeTrue();
        result.Value.ShouldBe(123L);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("abc")]
    [InlineData("")]
    public void TryParse_InvalidString_ReturnsFalse(string? input)
    {
        SnowflakeId.TryParse(input, out _).ShouldBeFalse();
    }

    [Fact]
    public void TryParseEither_ValidString_ReturnsRight()
    {
        var result = SnowflakeId.TryParseEither("999");
        result.IsRight.ShouldBeTrue();
        result.Match(id => id.Value.ShouldBe(999L), _ => { });
    }

    [Fact]
    public void TryParseEither_InvalidString_ReturnsLeft()
    {
        var result = SnowflakeId.TryParseEither("invalid");
        result.IsLeft.ShouldBeTrue();
    }

    // ────────────────────────────────────────────────────────────
    //  Equality
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Equals_SameValue_ReturnsTrue()
    {
        var a = new SnowflakeId(42L);
        var b = new SnowflakeId(42L);
        a.ShouldBe(b);
    }

    [Fact]
    public void Equals_DifferentValue_ReturnsFalse()
    {
        var a = new SnowflakeId(1L);
        var b = new SnowflakeId(2L);
        a.ShouldNotBe(b);
    }

    [Fact]
    public void GetHashCode_SameValue_SameHash()
    {
        var a = new SnowflakeId(42L);
        var b = new SnowflakeId(42L);
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    // ────────────────────────────────────────────────────────────
    //  Comparison
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void CompareTo_LessThan()
    {
        var a = new SnowflakeId(1L);
        var b = new SnowflakeId(2L);
        (a < b).ShouldBeTrue();
        (a <= b).ShouldBeTrue();
    }

    [Fact]
    public void CompareTo_GreaterThan()
    {
        var a = new SnowflakeId(2L);
        var b = new SnowflakeId(1L);
        (a > b).ShouldBeTrue();
        (a >= b).ShouldBeTrue();
    }

    [Fact]
    public void CompareTo_Equal()
    {
        var a = new SnowflakeId(5L);
        var b = new SnowflakeId(5L);
        a.CompareTo(b).ShouldBe(0);
        (a <= b).ShouldBeTrue();
        (a >= b).ShouldBeTrue();
    }

    [Fact]
    public void CompareTo_Null_Returns1()
    {
        var id = new SnowflakeId(1L);
        id.CompareTo(null).ShouldBe(1);
    }

    [Fact]
    public void CompareTo_WrongType_ThrowsArgumentException()
    {
        var id = new SnowflakeId(1L);
        Should.Throw<ArgumentException>(() => id.CompareTo("not-a-snowflake"));
    }

    // ────────────────────────────────────────────────────────────
    //  ToString
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void ToString_ReturnsInvariantCultureString()
    {
        var id = new SnowflakeId(123456789L);
        id.ToString().ShouldBe("123456789");
    }
}
