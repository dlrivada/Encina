using Encina.Security.PII;
using Encina.Security.PII.Strategies;

namespace Encina.UnitTests.Security.PII.Strategies;

public sealed class FullMaskingStrategyTests
{
    private readonly FullMaskingStrategy _sut = new();

    private static MaskingOptions DefaultPartial() => new()
    {
        Mode = MaskingMode.Partial,
        MaskCharacter = '*',
        PreserveLength = true,
        VisibleCharactersStart = 0,
        VisibleCharactersEnd = 0
    };

    private static MaskingOptions WithMode(MaskingMode mode) => new()
    {
        Mode = mode,
        MaskCharacter = '*',
        PreserveLength = true,
        VisibleCharactersStart = 0,
        VisibleCharactersEnd = 0
    };

    [Fact]
    public void Apply_PartialDefault_MasksEntireValueForLongStrings()
    {
        var result = _sut.Apply("sensitive-data", DefaultPartial());

        result.ShouldBe("**************");
    }

    [Fact]
    public void Apply_PartialWithVisibleStart_PreservesStartCharacters()
    {
        var options = new MaskingOptions
        {
            Mode = MaskingMode.Partial,
            MaskCharacter = '*',
            PreserveLength = true,
            VisibleCharactersStart = 2,
            VisibleCharactersEnd = 0
        };

        var result = _sut.Apply("sensitive-data", options);

        result.ShouldBe("se************");
    }

    [Fact]
    public void Apply_PartialWithVisibleEnd_PreservesEndCharacters()
    {
        var options = new MaskingOptions
        {
            Mode = MaskingMode.Partial,
            MaskCharacter = '*',
            PreserveLength = true,
            VisibleCharactersStart = 0,
            VisibleCharactersEnd = 3
        };

        var result = _sut.Apply("sensitive-data", options);

        result.ShouldBe("***********ata");
    }

    [Fact]
    public void Apply_PartialWithVisibleStartAndEnd_PreservesBothEnds()
    {
        var options = new MaskingOptions
        {
            Mode = MaskingMode.Partial,
            MaskCharacter = '*',
            PreserveLength = true,
            VisibleCharactersStart = 2,
            VisibleCharactersEnd = 3
        };

        var result = _sut.Apply("sensitive-data", options);

        result.ShouldBe("se*********ata");
    }

    [Fact]
    public void Apply_FullMode_WithPreserveLength_MasksEntireValueSameLength()
    {
        var options = new MaskingOptions
        {
            Mode = MaskingMode.Full,
            MaskCharacter = '*',
            PreserveLength = true
        };

        var result = _sut.Apply("sensitive-data", options);

        result.ShouldBe("**************");
        result.Length.ShouldBe(14);
    }

    [Fact]
    public void Apply_FullMode_WithoutPreserveLength_ReturnsFixedLengthMask()
    {
        var options = new MaskingOptions
        {
            Mode = MaskingMode.Full,
            MaskCharacter = '*',
            PreserveLength = false
        };

        var result = _sut.Apply("sensitive-data", options);

        result.ShouldBe("***");
    }

    [Fact]
    public void Apply_RedactMode_ReturnsRedactedPlaceholder()
    {
        var result = _sut.Apply("sensitive-data", WithMode(MaskingMode.Redact));

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
            RedactedPlaceholder = "[HIDDEN]"
        };

        var result = _sut.Apply("sensitive-data", options);

        result.ShouldBe("[HIDDEN]");
    }

    [Fact]
    public void Apply_HashMode_ReturnsDeterministicHash()
    {
        var options = WithMode(MaskingMode.Hash);

        var result1 = _sut.Apply("sensitive-data", options);
        var result2 = _sut.Apply("sensitive-data", options);

        result1.ShouldBe(result2);
        result1.ShouldNotBe("sensitive-data");
        result1.Length.ShouldBe(64);
    }

    [Fact]
    public void Apply_HashMode_DifferentInputs_ProduceDifferentHashes()
    {
        var options = WithMode(MaskingMode.Hash);

        var result1 = _sut.Apply("data-one", options);
        var result2 = _sut.Apply("data-two", options);

        result1.ShouldNotBe(result2);
    }

    [Fact]
    public void Apply_TokenizeMode_ReturnsOriginalValue()
    {
        var result = _sut.Apply("sensitive-data", WithMode(MaskingMode.Tokenize));

        result.ShouldBe("sensitive-data");
    }

    [Fact]
    public void Apply_ShortValue_SingleChar_MasksFully()
    {
        var result = _sut.Apply("A", DefaultPartial());

        result.ShouldBe("*");
    }

    [Fact]
    public void Apply_ShortValue_TwoChars_MasksFully()
    {
        var result = _sut.Apply("AB", DefaultPartial());

        result.ShouldBe("**");
    }

    [Fact]
    public void Apply_VisibleStartAndEndExceedLength_ReturnsOriginal()
    {
        var options = new MaskingOptions
        {
            Mode = MaskingMode.Partial,
            MaskCharacter = '*',
            PreserveLength = true,
            VisibleCharactersStart = 5,
            VisibleCharactersEnd = 5
        };

        // 6 chars, visible start (5) + visible end (1 clamped) >= length
        var result = _sut.Apply("short", options);

        // visibleStart=5 is clamped to length (5), visibleEnd clamped to 0
        // visibleStart + visibleEnd = 5 >= 5, so returns original
        result.ShouldBe("short");
    }

    [Fact]
    public void Apply_PartialWithoutPreserveLength_UsesFixedLengthMask()
    {
        var options = new MaskingOptions
        {
            Mode = MaskingMode.Partial,
            MaskCharacter = '*',
            PreserveLength = false,
            VisibleCharactersStart = 2,
            VisibleCharactersEnd = 2
        };

        var result = _sut.Apply("sensitive-data", options);

        result.ShouldBe("se***ta");
    }

    [Fact]
    public void Apply_HashMode_WithSalt_ProducesDifferentHash()
    {
        var optionsNoSalt = WithMode(MaskingMode.Hash);
        var optionsWithSalt = new MaskingOptions
        {
            Mode = MaskingMode.Hash,
            MaskCharacter = '*',
            PreserveLength = true,
            HashSalt = "full-salt"
        };

        var resultNoSalt = _sut.Apply("sensitive-data", optionsNoSalt);
        var resultWithSalt = _sut.Apply("sensitive-data", optionsWithSalt);

        resultNoSalt.ShouldNotBe(resultWithSalt);
    }

    [Fact]
    public void Apply_CustomMaskCharacter_UsesSpecifiedCharacter()
    {
        var options = new MaskingOptions
        {
            Mode = MaskingMode.Full,
            MaskCharacter = '#',
            PreserveLength = true
        };

        var result = _sut.Apply("sensitive", options);

        result.ShouldBe("#########");
    }
}
