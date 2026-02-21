using Encina.Security.PII;
using Encina.Security.PII.Strategies;

namespace Encina.UnitTests.Security.PII.Strategies;

public sealed class AddressMaskingStrategyTests
{
    private readonly AddressMaskingStrategy _sut = new();

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
    public void Apply_FullAddress_MasksStreetPreservesCityState()
    {
        var result = _sut.Apply("123 Main St, Springfield, IL", DefaultPartial());

        result.ShouldBe("*** **** **, Springfield, IL");
    }

    [Fact]
    public void Apply_AddressWithoutComma_MasksEntireValue()
    {
        var result = _sut.Apply("123 Main Street", DefaultPartial());

        result.ShouldBe("*** **** ******");
    }

    [Fact]
    public void Apply_FullMode_MasksEntireValue()
    {
        var result = _sut.Apply("123 Main St, Springfield, IL", WithMode(MaskingMode.Full));

        result.ShouldBe("****************************");
    }

    [Fact]
    public void Apply_RedactMode_ReturnsRedactedPlaceholder()
    {
        var result = _sut.Apply("123 Main St, Springfield, IL", WithMode(MaskingMode.Redact));

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
            RedactedPlaceholder = "[ADDRESS REMOVED]"
        };

        var result = _sut.Apply("123 Main St, Springfield, IL", options);

        result.ShouldBe("[ADDRESS REMOVED]");
    }

    [Fact]
    public void Apply_HashMode_ReturnsDeterministicHash()
    {
        var options = WithMode(MaskingMode.Hash);

        var result1 = _sut.Apply("123 Main St, Springfield, IL", options);
        var result2 = _sut.Apply("123 Main St, Springfield, IL", options);

        result1.ShouldBe(result2);
        result1.ShouldNotBe("123 Main St, Springfield, IL");
        result1.Length.ShouldBe(64);
    }

    [Fact]
    public void Apply_TokenizeMode_ReturnsOriginalValue()
    {
        var result = _sut.Apply("123 Main St, Springfield, IL", WithMode(MaskingMode.Tokenize));

        result.ShouldBe("123 Main St, Springfield, IL");
    }

    [Fact]
    public void Apply_PreservesSpacesInStreetPortion()
    {
        var result = _sut.Apply("456 Oak Ave, Portland, OR", DefaultPartial());

        // Spaces preserved, letters/digits masked
        result.ShouldBe("*** *** ***, Portland, OR");
    }

    [Fact]
    public void Apply_MultipleCommas_OnlyFirstCommaIsDelimiter()
    {
        var result = _sut.Apply("123 Main St, Springfield, IL, 62701", DefaultPartial());

        result.ShouldBe("*** **** **, Springfield, IL, 62701");
    }

    [Fact]
    public void Apply_FullMode_PreservesLength()
    {
        var input = "123 Main St, Springfield, IL";
        var result = _sut.Apply(input, WithMode(MaskingMode.Full));

        result.Length.ShouldBe(input.Length);
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
            HashSalt = "addr-salt"
        };

        var resultNoSalt = _sut.Apply("123 Main St, Springfield, IL", optionsNoSalt);
        var resultWithSalt = _sut.Apply("123 Main St, Springfield, IL", optionsWithSalt);

        resultNoSalt.ShouldNotBe(resultWithSalt);
    }

    [Fact]
    public void Apply_AddressWithApartment_MasksStreetAndApt()
    {
        var result = _sut.Apply("123 Main St Apt 4B, Springfield, IL", DefaultPartial());

        result.ShouldBe("*** **** ** *** **, Springfield, IL");
    }

    [Fact]
    public void Apply_OnlyStreetNoCity_MasksEverything()
    {
        var result = _sut.Apply("789 Elm Blvd", DefaultPartial());

        result.ShouldBe("*** *** ****");
    }
}
