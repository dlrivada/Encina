using Encina.Security.PII;
using Encina.Security.PII.Strategies;

namespace Encina.UnitTests.Security.PII.Strategies;

public sealed class NameMaskingStrategyTests
{
    private readonly NameMaskingStrategy _sut = new();

    private static MaskingOptions DefaultPartial() => new()
    {
        Mode = MaskingMode.Partial,
        MaskCharacter = '*',
        PreserveLength = true,
        VisibleCharactersStart = 1,
        VisibleCharactersEnd = 0
    };

    private static MaskingOptions WithMode(MaskingMode mode) => new()
    {
        Mode = mode,
        MaskCharacter = '*',
        PreserveLength = true,
        VisibleCharactersStart = 1,
        VisibleCharactersEnd = 0
    };

    [Fact]
    public void Apply_FirstAndLastName_MasksEachPartPreservingFirstChar()
    {
        var result = _sut.Apply("John Doe", DefaultPartial());

        result.ShouldBe("J*** D**");
    }

    [Fact]
    public void Apply_SingleName_MasksPreservingFirstChar()
    {
        var result = _sut.Apply("Alice", DefaultPartial());

        result.ShouldBe("A****");
    }

    [Fact]
    public void Apply_HyphenatedName_MasksEachPartSeparately()
    {
        var result = _sut.Apply("Mary-Jane Watson", DefaultPartial());

        result.ShouldBe("M***-J*** W*****");
    }

    [Fact]
    public void Apply_WithMiddleName_MasksAllParts()
    {
        var result = _sut.Apply("John Michael Doe", DefaultPartial());

        result.ShouldBe("J*** M****** D**");
    }

    [Fact]
    public void Apply_NameWithPeriodSeparator_MasksEachPart()
    {
        var result = _sut.Apply("J.R. Tolkien", DefaultPartial());

        result.ShouldBe("J.R. T******");
    }

    [Fact]
    public void Apply_SingleCharName_PreservesSingleChar()
    {
        var result = _sut.Apply("J", DefaultPartial());

        result.ShouldBe("J");
    }

    [Fact]
    public void Apply_FullMode_MasksEntireValue()
    {
        var result = _sut.Apply("John Doe", WithMode(MaskingMode.Full));

        result.ShouldBe("********");
    }

    [Fact]
    public void Apply_RedactMode_ReturnsRedactedPlaceholder()
    {
        var result = _sut.Apply("John Doe", WithMode(MaskingMode.Redact));

        result.ShouldBe("[REDACTED]");
    }

    [Fact]
    public void Apply_RedactMode_UsesCustomPlaceholder()
    {
        var options = new MaskingOptions
        {
            Mode = MaskingMode.Redact,
            MaskCharacter = '*',
            PreserveLength = true,
            RedactedPlaceholder = "[NAME REMOVED]"
        };

        var result = _sut.Apply("John Doe", options);

        result.ShouldBe("[NAME REMOVED]");
    }

    [Fact]
    public void Apply_HashMode_ReturnsDeterministicHash()
    {
        var options = WithMode(MaskingMode.Hash);

        var result1 = _sut.Apply("John Doe", options);
        var result2 = _sut.Apply("John Doe", options);

        result1.ShouldBe(result2);
        result1.ShouldNotBe("John Doe");
        result1.Length.ShouldBe(64);
    }

    [Fact]
    public void Apply_TokenizeMode_ReturnsOriginalValue()
    {
        var result = _sut.Apply("John Doe", WithMode(MaskingMode.Tokenize));

        result.ShouldBe("John Doe");
    }

    [Fact]
    public void Apply_FullMode_PreservesLength()
    {
        var input = "John Doe";
        var result = _sut.Apply(input, WithMode(MaskingMode.Full));

        result.Length.ShouldBe(input.Length);
    }

    [Theory]
    [InlineData("John Doe", "J*** D**")]
    [InlineData("Alice Bob Charlie", "A**** B** C******")]
    [InlineData("X", "X")]
    public void Apply_VariousNames_MasksCorrectly(string input, string expected)
    {
        var result = _sut.Apply(input, DefaultPartial());

        result.ShouldBe(expected);
    }
}
