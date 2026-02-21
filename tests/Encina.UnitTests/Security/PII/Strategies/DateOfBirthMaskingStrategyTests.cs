using Encina.Security.PII;
using Encina.Security.PII.Strategies;

namespace Encina.UnitTests.Security.PII.Strategies;

public sealed class DateOfBirthMaskingStrategyTests
{
    private readonly DateOfBirthMaskingStrategy _sut = new();

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
    public void Apply_SlashFormat_MasksMonthAndDayPreservesYear()
    {
        var result = _sut.Apply("03/15/1990", DefaultPartial());

        result.ShouldBe("**/**/1990");
    }

    [Fact]
    public void Apply_DashFormat_MasksMonthAndDayPreservesYear()
    {
        var result = _sut.Apply("03-15-1990", DefaultPartial());

        result.ShouldBe("**-**-1990");
    }

    [Fact]
    public void Apply_DotFormat_MasksMonthAndDayPreservesYear()
    {
        var result = _sut.Apply("03.15.1990", DefaultPartial());

        result.ShouldBe("**.**.1990");
    }

    [Fact]
    public void Apply_ISOFormat_PreservesYearMasksMonthAndDay()
    {
        var result = _sut.Apply("1990-03-15", DefaultPartial());

        result.ShouldBe("1990-**-**");
    }

    [Fact]
    public void Apply_FullMode_MasksEntireValue()
    {
        var result = _sut.Apply("03/15/1990", WithMode(MaskingMode.Full));

        result.ShouldBe("**********");
    }

    [Fact]
    public void Apply_RedactMode_ReturnsRedactedPlaceholder()
    {
        var result = _sut.Apply("03/15/1990", WithMode(MaskingMode.Redact));

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
            RedactedPlaceholder = "[DOB REMOVED]"
        };

        var result = _sut.Apply("03/15/1990", options);

        result.ShouldBe("[DOB REMOVED]");
    }

    [Fact]
    public void Apply_HashMode_ReturnsDeterministicHash()
    {
        var options = WithMode(MaskingMode.Hash);

        var result1 = _sut.Apply("03/15/1990", options);
        var result2 = _sut.Apply("03/15/1990", options);

        result1.ShouldBe(result2);
        result1.ShouldNotBe("03/15/1990");
        result1.Length.ShouldBe(64);
    }

    [Fact]
    public void Apply_TokenizeMode_ReturnsOriginalValue()
    {
        var result = _sut.Apply("03/15/1990", WithMode(MaskingMode.Tokenize));

        result.ShouldBe("03/15/1990");
    }

    [Theory]
    [InlineData("01/01/2000", "**/**/2000")]
    [InlineData("12/31/1985", "**/**/1985")]
    [InlineData("06-25-1975", "**-**-1975")]
    public void Apply_VariousDates_MasksCorrectly(string input, string expected)
    {
        var result = _sut.Apply(input, DefaultPartial());

        result.ShouldBe(expected);
    }

    [Fact]
    public void Apply_FullMode_PreservesLength()
    {
        var input = "03/15/1990";
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
            HashSalt = "dob-salt"
        };

        var resultNoSalt = _sut.Apply("03/15/1990", optionsNoSalt);
        var resultWithSalt = _sut.Apply("03/15/1990", optionsWithSalt);

        resultNoSalt.ShouldNotBe(resultWithSalt);
    }

    [Fact]
    public void Apply_PreservesSeparatorCharacters()
    {
        var slashResult = _sut.Apply("03/15/1990", DefaultPartial());
        var dashResult = _sut.Apply("03-15-1990", DefaultPartial());

        slashResult.ShouldContain("/");
        dashResult.ShouldContain("-");
    }

    [Fact]
    public void Apply_TwoPartDate_MasksAllDigits()
    {
        // Fewer than 3 parts: cannot determine year, masks all digits
        var result = _sut.Apply("03/1990", DefaultPartial());

        result.ShouldBe("**/****");
    }
}
