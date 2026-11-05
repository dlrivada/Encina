using Encina.IdGeneration;

namespace Encina.GuardTests.IdGeneration;

/// <summary>
/// Guard tests for the parsing, construction and comparison entry points of the four id types
/// (<see cref="SnowflakeId"/>, <see cref="UuidV7Id"/>, <see cref="UlidId"/>, <see cref="ShardPrefixedId"/>).
/// </summary>
public sealed class IdTypeParsingGuardTests
{
    // ---- SnowflakeId ----

    [Fact]
    public void SnowflakeId_Parse_Null_ThrowsArgumentNullException()
    {
        Action act = () => _ = SnowflakeId.Parse(null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("s");
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-number")]
    public void SnowflakeId_Parse_InvalidText_ThrowsFormatException(string text)
    {
        Action act = () => _ = SnowflakeId.Parse(text);

        Should.Throw<FormatException>(act);
    }

    [Fact]
    public void SnowflakeId_Parse_OutOfRange_ThrowsOverflowException()
    {
        Action act = () => _ = SnowflakeId.Parse("99999999999999999999");

        Should.Throw<OverflowException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    public void SnowflakeId_TryParse_InvalidInput_ReturnsFalseAndDefault(string? text)
    {
        var parsed = SnowflakeId.TryParse(text, out var result);

        parsed.ShouldBeFalse();
        result.ShouldBe(default);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("abc")]
    public void SnowflakeId_TryParseEither_InvalidInput_ReturnsLeft(string? text)
    {
        SnowflakeId.TryParseEither(text).IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void SnowflakeId_CompareToObject_WrongType_ThrowsArgumentException()
    {
        var id = new SnowflakeId(1);

        Action act = () => _ = id.CompareTo("not an id");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("obj");
    }

    [Fact]
    public void SnowflakeId_CompareToObject_Null_IsGreaterThanNull()
    {
        new SnowflakeId(1).CompareTo((object?)null).ShouldBe(1);
    }

    // ---- UuidV7Id ----

    [Fact]
    public void UuidV7Id_Parse_Null_ThrowsArgumentNullException()
    {
        Action act = () => _ = UuidV7Id.Parse(null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("input");
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-guid")]
    public void UuidV7Id_Parse_InvalidText_ThrowsFormatException(string text)
    {
        Action act = () => _ = UuidV7Id.Parse(text);

        Should.Throw<FormatException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-guid")]
    public void UuidV7Id_TryParse_InvalidInput_ReturnsFalseAndDefault(string? text)
    {
        var parsed = UuidV7Id.TryParse(text, out var result);

        parsed.ShouldBeFalse();
        result.ShouldBe(default);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-guid")]
    public void UuidV7Id_TryParseEither_InvalidInput_ReturnsLeft(string? text)
    {
        UuidV7Id.TryParseEither(text).IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void UuidV7Id_CompareToObject_WrongType_ThrowsArgumentException()
    {
        var id = UuidV7Id.NewUuidV7();

        Action act = () => _ = id.CompareTo(42);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("obj");
    }

    // ---- UlidId ----

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(17)]
    public void UlidId_Constructor_WrongByteLength_ThrowsArgumentException(int length)
    {
        var bytes = new byte[length];

        Action act = () => _ = new UlidId(bytes);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("bytes");
    }

    [Fact]
    public void UlidId_Parse_Null_ThrowsFormatException()
    {
        Action act = () => _ = UlidId.Parse(null!);

        Should.Throw<FormatException>(act);
    }

    [Theory]
    [InlineData("")]
    [InlineData("TOO-SHORT")]
    [InlineData("01ARZ3NDEKTSV4RRFFQ69G5FAVXX")]
    [InlineData("!!ARZ3NDEKTSV4RRFFQ69G5FAV")]
    public void UlidId_Parse_InvalidText_ThrowsFormatException(string text)
    {
        Action act = () => _ = UlidId.Parse(text);

        Should.Throw<FormatException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("TOO-SHORT")]
    [InlineData("!!ARZ3NDEKTSV4RRFFQ69G5FAV")]
    public void UlidId_TryParse_InvalidInput_ReturnsFalseAndDefault(string? text)
    {
        var parsed = UlidId.TryParse(text, out var result);

        parsed.ShouldBeFalse();
        result.ShouldBe(default(UlidId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("TOO-SHORT")]
    public void UlidId_TryParseEither_InvalidInput_ReturnsLeft(string? text)
    {
        UlidId.TryParseEither(text).IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void UlidId_CompareToObject_WrongType_ThrowsArgumentException()
    {
        var id = UlidId.NewUlid();

        Action act = () => _ = id.CompareTo((object)Guid.NewGuid());

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("obj");
    }

    // ---- ShardPrefixedId ----

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ShardPrefixedId_Constructor_BlankShardId_ThrowsArgumentException(string? shardId)
    {
        Action act = () => _ = new ShardPrefixedId(shardId!, "seq");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("shardId");
    }

    [Fact]
    public void ShardPrefixedId_Constructor_ShardIdContainingDelimiter_ThrowsArgumentException()
    {
        Action act = () => _ = new ShardPrefixedId("shard:01", "seq");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("shardId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ShardPrefixedId_Constructor_BlankSequence_ThrowsArgumentException(string? sequence)
    {
        Action act = () => _ = new ShardPrefixedId("shard-01", sequence!);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("sequence");
    }

    [Fact]
    public void ShardPrefixedId_Parse_Null_ThrowsFormatException()
    {
        Action act = () => _ = ShardPrefixedId.Parse(null!);

        Should.Throw<FormatException>(act);
    }

    [Theory]
    [InlineData("")]
    [InlineData("no-delimiter")]
    [InlineData(":missing-shard")]
    [InlineData("missing-sequence:")]
    [InlineData("two:de:limiters")]
    [InlineData(" :seq")]
    public void ShardPrefixedId_Parse_InvalidText_ThrowsFormatException(string text)
    {
        Action act = () => _ = ShardPrefixedId.Parse(text);

        Should.Throw<FormatException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("no-delimiter")]
    [InlineData(":missing-shard")]
    [InlineData("missing-sequence:")]
    [InlineData("two:de:limiters")]
    [InlineData(" :seq")]
    public void ShardPrefixedId_TryParse_InvalidInput_ReturnsFalseAndEmpty(string? text)
    {
        var parsed = ShardPrefixedId.TryParse(text, out var result);

        parsed.ShouldBeFalse();
        result.IsEmpty.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("no-delimiter")]
    public void ShardPrefixedId_TryParseEither_InvalidInput_ReturnsLeft(string? text)
    {
        ShardPrefixedId.TryParseEither(text).IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void ShardPrefixedId_CompareToObject_WrongType_ThrowsArgumentException()
    {
        var id = new ShardPrefixedId("shard-01", "seq");

        Action act = () => _ = id.CompareTo("shard-01:seq");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("obj");
    }
}
