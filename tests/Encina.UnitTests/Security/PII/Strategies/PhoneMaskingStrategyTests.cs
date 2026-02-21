using Encina.Security.PII;
using Encina.Security.PII.Strategies;

namespace Encina.UnitTests.Security.PII.Strategies;

public sealed class PhoneMaskingStrategyTests
{
    private readonly PhoneMaskingStrategy _sut = new();

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
    public void Apply_StandardPhone_MasksAllButLastFourDigits()
    {
        var result = _sut.Apply("555-123-4567", DefaultPartial());

        result.ShouldBe("***-***-4567");
    }

    [Fact]
    public void Apply_InternationalPhone_MasksAllButLastFourDigits()
    {
        var result = _sut.Apply("+1-555-123-4567", DefaultPartial());

        result.ShouldBe("+*-***-***-4567");
    }

    [Fact]
    public void Apply_PhoneWithParentheses_PreservesFormatting()
    {
        var result = _sut.Apply("(555) 123-4567", DefaultPartial());

        result.ShouldBe("(***) ***-4567");
    }

    [Theory]
    [InlineData("5551234567", "******4567")]
    [InlineData("123-456-7890", "***-***-7890")]
    [InlineData("+44 20 7946 0958", "+** ** **** 0958")]
    public void Apply_VariousFormats_MasksCorrectly(string input, string expected)
    {
        var result = _sut.Apply(input, DefaultPartial());

        result.ShouldBe(expected);
    }

    [Fact]
    public void Apply_FullMode_MasksEntireValue()
    {
        var result = _sut.Apply("555-123-4567", WithMode(MaskingMode.Full));

        result.ShouldBe("************");
    }

    [Fact]
    public void Apply_RedactMode_ReturnsRedactedPlaceholder()
    {
        var result = _sut.Apply("555-123-4567", WithMode(MaskingMode.Redact));

        result.ShouldBe("[REDACTED]");
    }

    [Fact]
    public void Apply_HashMode_ReturnsDeterministicHash()
    {
        var options = WithMode(MaskingMode.Hash);

        var result1 = _sut.Apply("555-123-4567", options);
        var result2 = _sut.Apply("555-123-4567", options);

        result1.ShouldBe(result2);
        result1.ShouldNotBe("555-123-4567");
        result1.Length.ShouldBe(64);
    }

    [Fact]
    public void Apply_TokenizeMode_ReturnsOriginalValue()
    {
        var result = _sut.Apply("555-123-4567", WithMode(MaskingMode.Tokenize));

        result.ShouldBe("555-123-4567");
    }

    [Fact]
    public void Apply_ShortNumber_ReturnsOriginal()
    {
        // Only 4 digits - same as visible end count, too short to mask
        var result = _sut.Apply("1234", DefaultPartial());

        result.ShouldBe("1234");
    }

    [Fact]
    public void Apply_PhoneWithSpaces_PreservesSpaces()
    {
        var result = _sut.Apply("555 123 4567", DefaultPartial());

        result.ShouldBe("*** *** 4567");
    }

    [Fact]
    public void Apply_PhoneWithDots_PreservesDots()
    {
        var result = _sut.Apply("555.123.4567", DefaultPartial());

        result.ShouldBe("***.***.4567");
    }

    [Fact]
    public void Apply_RedactMode_UsesCustomPlaceholder()
    {
        var options = new MaskingOptions
        {
            Mode = MaskingMode.Redact,
            MaskCharacter = '*',
            PreserveLength = true,
            RedactedPlaceholder = "[PHONE REMOVED]"
        };

        var result = _sut.Apply("555-123-4567", options);

        result.ShouldBe("[PHONE REMOVED]");
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
            HashSalt = "phone-salt"
        };

        var resultNoSalt = _sut.Apply("555-123-4567", optionsNoSalt);
        var resultWithSalt = _sut.Apply("555-123-4567", optionsWithSalt);

        resultNoSalt.ShouldNotBe(resultWithSalt);
    }
}
