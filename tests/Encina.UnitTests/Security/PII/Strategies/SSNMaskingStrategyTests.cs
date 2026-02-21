using Encina.Security.PII;
using Encina.Security.PII.Strategies;

namespace Encina.UnitTests.Security.PII.Strategies;

public sealed class SSNMaskingStrategyTests
{
    private readonly SSNMaskingStrategy _sut = new();

    private static MaskingOptions DefaultPartial() => new()
    {
        Mode = MaskingMode.Partial,
        MaskCharacter = '*',
        PreserveLength = true,
        VisibleCharactersStart = 0,
        VisibleCharactersEnd = 4
    };

    private static MaskingOptions WithMode(MaskingMode mode) => new()
    {
        Mode = mode,
        MaskCharacter = '*',
        PreserveLength = true,
        VisibleCharactersStart = 0,
        VisibleCharactersEnd = 4
    };

    [Fact]
    public void Apply_StandardFormat_MasksAllButLastFourDigits()
    {
        var result = _sut.Apply("123-45-6789", DefaultPartial());

        result.ShouldBe("***-**-6789");
    }

    [Fact]
    public void Apply_WithoutDashes_MasksAllButLastFourDigits()
    {
        var result = _sut.Apply("123456789", DefaultPartial());

        result.ShouldBe("*****6789");
    }

    [Fact]
    public void Apply_FullMode_MasksEntireValue()
    {
        var result = _sut.Apply("123-45-6789", WithMode(MaskingMode.Full));

        result.ShouldBe("***********");
    }

    [Fact]
    public void Apply_RedactMode_ReturnsRedactedPlaceholder()
    {
        var result = _sut.Apply("123-45-6789", WithMode(MaskingMode.Redact));

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
            RedactedPlaceholder = "[SSN REMOVED]"
        };

        var result = _sut.Apply("123-45-6789", options);

        result.ShouldBe("[SSN REMOVED]");
    }

    [Fact]
    public void Apply_HashMode_ReturnsDeterministicHash()
    {
        var options = WithMode(MaskingMode.Hash);

        var result1 = _sut.Apply("123-45-6789", options);
        var result2 = _sut.Apply("123-45-6789", options);

        result1.ShouldBe(result2);
        result1.ShouldNotBe("123-45-6789");
        result1.Length.ShouldBe(64);
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
            HashSalt = "ssn-salt"
        };

        var resultNoSalt = _sut.Apply("123-45-6789", optionsNoSalt);
        var resultWithSalt = _sut.Apply("123-45-6789", optionsWithSalt);

        resultNoSalt.ShouldNotBe(resultWithSalt);
    }

    [Fact]
    public void Apply_TokenizeMode_ReturnsOriginalValue()
    {
        var result = _sut.Apply("123-45-6789", WithMode(MaskingMode.Tokenize));

        result.ShouldBe("123-45-6789");
    }

    [Fact]
    public void Apply_ShortValue_ReturnsOriginal()
    {
        // Only 4 digits - same as visible end count, too short to mask
        var result = _sut.Apply("6789", DefaultPartial());

        result.ShouldBe("6789");
    }

    [Fact]
    public void Apply_PreservesDashPositions()
    {
        var result = _sut.Apply("123-45-6789", DefaultPartial());

        result[3].ShouldBe('-');
        result[6].ShouldBe('-');
    }

    [Fact]
    public void Apply_FullMode_PreservesLength()
    {
        var input = "123-45-6789";
        var result = _sut.Apply(input, WithMode(MaskingMode.Full));

        result.Length.ShouldBe(input.Length);
    }

    [Theory]
    [InlineData("123-45-6789", "***-**-6789")]
    [InlineData("987-65-4321", "***-**-4321")]
    [InlineData("000-00-0000", "***-**-0000")]
    public void Apply_MultipleSSNs_MasksCorrectly(string input, string expected)
    {
        var result = _sut.Apply(input, DefaultPartial());

        result.ShouldBe(expected);
    }
}
