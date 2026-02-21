using Encina.Security.PII;
using Encina.Security.PII.Strategies;

namespace Encina.UnitTests.Security.PII.Strategies;

public sealed class CreditCardMaskingStrategyTests
{
    private readonly CreditCardMaskingStrategy _sut = new();

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
    public void Apply_CardWithDashes_MasksAllButLastFourDigits()
    {
        var result = _sut.Apply("4532-1234-5678-9012", DefaultPartial());

        result.ShouldBe("****-****-****-9012");
    }

    [Fact]
    public void Apply_CardWithoutDashes_MasksAllButLastFourDigits()
    {
        var result = _sut.Apply("4532123456789012", DefaultPartial());

        result.ShouldBe("************9012");
    }

    [Fact]
    public void Apply_CardWithSpaces_PreservesSpaces()
    {
        var result = _sut.Apply("4532 1234 5678 9012", DefaultPartial());

        result.ShouldBe("**** **** **** 9012");
    }

    [Fact]
    public void Apply_AmexCard15Digits_MasksAllButLastFour()
    {
        var result = _sut.Apply("3782-822463-10005", DefaultPartial());

        result.ShouldBe("****-******-*0005");
    }

    [Fact]
    public void Apply_Visa16Digits_MasksAllButLastFour()
    {
        var result = _sut.Apply("4111111111111111", DefaultPartial());

        result.ShouldBe("************1111");
    }

    [Fact]
    public void Apply_FullMode_MasksEntireValue()
    {
        var result = _sut.Apply("4532-1234-5678-9012", WithMode(MaskingMode.Full));

        result.ShouldBe("*******************");
    }

    [Fact]
    public void Apply_RedactMode_ReturnsRedactedPlaceholder()
    {
        var result = _sut.Apply("4532-1234-5678-9012", WithMode(MaskingMode.Redact));

        result.ShouldBe("[REDACTED]");
    }

    [Fact]
    public void Apply_HashMode_ReturnsDeterministicHash()
    {
        var options = WithMode(MaskingMode.Hash);

        var result1 = _sut.Apply("4532-1234-5678-9012", options);
        var result2 = _sut.Apply("4532-1234-5678-9012", options);

        result1.ShouldBe(result2);
        result1.ShouldNotBe("4532-1234-5678-9012");
        result1.Length.ShouldBe(64);
    }

    [Fact]
    public void Apply_TokenizeMode_ReturnsOriginalValue()
    {
        var result = _sut.Apply("4532-1234-5678-9012", WithMode(MaskingMode.Tokenize));

        result.ShouldBe("4532-1234-5678-9012");
    }

    [Fact]
    public void Apply_ShortNumber_ReturnsOriginal()
    {
        // Only 4 digits - same as visible end count, too short to mask
        var result = _sut.Apply("1234", DefaultPartial());

        result.ShouldBe("1234");
    }

    [Fact]
    public void Apply_RedactMode_UsesCustomPlaceholder()
    {
        var options = new MaskingOptions
        {
            Mode = MaskingMode.Redact,
            MaskCharacter = '*',
            PreserveLength = true,
            RedactedPlaceholder = "[CC REMOVED]"
        };

        var result = _sut.Apply("4532-1234-5678-9012", options);

        result.ShouldBe("[CC REMOVED]");
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
            HashSalt = "cc-salt"
        };

        var resultNoSalt = _sut.Apply("4532123456789012", optionsNoSalt);
        var resultWithSalt = _sut.Apply("4532123456789012", optionsWithSalt);

        resultNoSalt.ShouldNotBe(resultWithSalt);
    }

    [Fact]
    public void Apply_FullMode_PreservesLength()
    {
        var input = "4532-1234-5678-9012";
        var result = _sut.Apply(input, WithMode(MaskingMode.Full));

        result.Length.ShouldBe(input.Length);
    }
}
