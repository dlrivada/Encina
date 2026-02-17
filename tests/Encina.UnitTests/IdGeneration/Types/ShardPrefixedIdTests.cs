using Encina.IdGeneration;

namespace Encina.UnitTests.IdGeneration.Types;

/// <summary>
/// Unit tests for <see cref="ShardPrefixedId"/>.
/// </summary>
public sealed class ShardPrefixedIdTests
{
    // ────────────────────────────────────────────────────────────
    //  Construction
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_SetsShardIdAndSequence()
    {
        var id = new ShardPrefixedId("shard-01", "seq-001");
        id.ShardId.ShouldBe("shard-01");
        id.Sequence.ShouldBe("seq-001");
    }

    [Fact]
    public void Default_IsEmpty()
    {
        var id = default(ShardPrefixedId);
        id.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void Empty_ReturnsDefaultValue()
    {
        ShardPrefixedId.Empty.IsEmpty.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Constructor_NullOrWhitespaceShardId_ThrowsArgumentException(string? shardId)
    {
        Should.Throw<ArgumentException>(() => new ShardPrefixedId(shardId!, "seq"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Constructor_NullOrWhitespaceSequence_ThrowsArgumentException(string? sequence)
    {
        Should.Throw<ArgumentException>(() => new ShardPrefixedId("shard", sequence!));
    }

    [Fact]
    public void Constructor_ShardIdContainingDelimiter_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => new ShardPrefixedId("shard:01", "seq"));
    }

    // ────────────────────────────────────────────────────────────
    //  Delimiter
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Delimiter_IsColon()
    {
        ShardPrefixedId.Delimiter.ShouldBe(':');
    }

    // ────────────────────────────────────────────────────────────
    //  ToString
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void ToString_ReturnsShardDelimiterSequence()
    {
        var id = new ShardPrefixedId("shard-01", "seq-001");
        id.ToString().ShouldBe("shard-01:seq-001");
    }

    [Fact]
    public void ToString_Empty_ReturnsEmptyString()
    {
        ShardPrefixedId.Empty.ToString().ShouldBe(string.Empty);
    }

    // ────────────────────────────────────────────────────────────
    //  Implicit conversion to string
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void ImplicitConversion_ToString()
    {
        var id = new ShardPrefixedId("shard-01", "seq-001");
        string value = id;
        value.ShouldBe("shard-01:seq-001");
    }

    // ────────────────────────────────────────────────────────────
    //  Parse / TryParse
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_ValidString_ReturnsShardPrefixedId()
    {
        var id = ShardPrefixedId.Parse("shard-01:seq-001");
        id.ShardId.ShouldBe("shard-01");
        id.Sequence.ShouldBe("seq-001");
    }

    [Fact]
    public void Parse_InvalidString_ThrowsFormatException()
    {
        Should.Throw<FormatException>(() => ShardPrefixedId.Parse("no-delimiter"));
    }

    [Fact]
    public void Parse_MultipleDelimiters_ThrowsFormatException()
    {
        Should.Throw<FormatException>(() => ShardPrefixedId.Parse("a:b:c"));
    }

    [Fact]
    public void TryParse_ValidString_ReturnsTrue()
    {
        ShardPrefixedId.TryParse("shard:seq", out var result).ShouldBeTrue();
        result.ShardId.ShouldBe("shard");
        result.Sequence.ShouldBe("seq");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("no-delimiter")]
    [InlineData(":seq")]
    [InlineData("shard:")]
    [InlineData("a:b:c")]
    public void TryParse_InvalidString_ReturnsFalse(string? input)
    {
        ShardPrefixedId.TryParse(input, out _).ShouldBeFalse();
    }

    [Fact]
    public void TryParseEither_ValidString_ReturnsRight()
    {
        var result = ShardPrefixedId.TryParseEither("shard:seq");
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void TryParseEither_InvalidString_ReturnsLeft()
    {
        var result = ShardPrefixedId.TryParseEither("invalid");
        result.IsLeft.ShouldBeTrue();
    }

    // ────────────────────────────────────────────────────────────
    //  Equality
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        var a = new ShardPrefixedId("shard", "seq");
        var b = new ShardPrefixedId("shard", "seq");
        a.ShouldBe(b);
    }

    [Fact]
    public void Equals_DifferentShard_ReturnsFalse()
    {
        var a = new ShardPrefixedId("shard-1", "seq");
        var b = new ShardPrefixedId("shard-2", "seq");
        a.ShouldNotBe(b);
    }

    [Fact]
    public void Equals_DifferentSequence_ReturnsFalse()
    {
        var a = new ShardPrefixedId("shard", "seq-1");
        var b = new ShardPrefixedId("shard", "seq-2");
        a.ShouldNotBe(b);
    }

    [Fact]
    public void GetHashCode_SameValues_SameHash()
    {
        var a = new ShardPrefixedId("shard", "seq");
        var b = new ShardPrefixedId("shard", "seq");
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    // ────────────────────────────────────────────────────────────
    //  Comparison
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void CompareTo_DifferentShardId_ComparesShardFirst()
    {
        var a = new ShardPrefixedId("aaa", "zzz");
        var b = new ShardPrefixedId("bbb", "aaa");
        (a < b).ShouldBeTrue();
    }

    [Fact]
    public void CompareTo_SameShardId_ComparesSequence()
    {
        var a = new ShardPrefixedId("shard", "aaa");
        var b = new ShardPrefixedId("shard", "bbb");
        (a < b).ShouldBeTrue();
    }

    [Fact]
    public void CompareTo_Null_Returns1()
    {
        var id = new ShardPrefixedId("shard", "seq");
        id.CompareTo(null).ShouldBe(1);
    }

    [Fact]
    public void CompareTo_WrongType_ThrowsArgumentException()
    {
        var id = new ShardPrefixedId("shard", "seq");
        Should.Throw<ArgumentException>(() => id.CompareTo("not-valid"));
    }
}
