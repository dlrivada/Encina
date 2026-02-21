using Encina.Security.PII;
using Encina.Security.PII.Strategies;

namespace Encina.UnitTests.Security.PII.Strategies;

public sealed class EmailMaskingStrategyTests
{
    private readonly EmailMaskingStrategy _sut = new();

    private static MaskingOptions DefaultPartial() => new()
    {
        Mode = MaskingMode.Partial,
        MaskCharacter = '*',
        PreserveLength = false,
        VisibleCharactersStart = 1,
        VisibleCharactersEnd = 0
    };

    private static MaskingOptions WithMode(MaskingMode mode) => new()
    {
        Mode = mode,
        MaskCharacter = '*',
        PreserveLength = false,
        VisibleCharactersStart = 1,
        VisibleCharactersEnd = 0
    };

    [Fact]
    public void Apply_ValidEmail_MasksLocalPartPreservingFirstCharAndDomain()
    {
        var result = _sut.Apply("john.doe@example.com", DefaultPartial());

        result.ShouldBe("j***@example.com");
    }

    [Theory]
    [InlineData("alice@example.com", "a***@example.com")]
    [InlineData("bob@test.org", "b***@test.org")]
    [InlineData("charlie.brown@company.co.uk", "c***@company.co.uk")]
    public void Apply_VariousValidEmails_MasksCorrectly(string input, string expected)
    {
        var result = _sut.Apply(input, DefaultPartial());

        result.ShouldBe(expected);
    }

    [Fact]
    public void Apply_FullMode_MasksEntireLocalPart()
    {
        var result = _sut.Apply("john.doe@example.com", WithMode(MaskingMode.Full));

        result.ShouldBe("***@example.com");
    }

    [Fact]
    public void Apply_RedactMode_ReturnsRedactedPlaceholder()
    {
        var result = _sut.Apply("john@example.com", WithMode(MaskingMode.Redact));

        result.ShouldBe("[REDACTED]");
    }

    [Fact]
    public void Apply_RedactMode_UsesCustomPlaceholder()
    {
        var options = new MaskingOptions
        {
            Mode = MaskingMode.Redact,
            MaskCharacter = '*',
            RedactedPlaceholder = "[REMOVED]"
        };

        var result = _sut.Apply("john@example.com", options);

        result.ShouldBe("[REMOVED]");
    }

    [Fact]
    public void Apply_HashMode_ReturnsDeterministicHash()
    {
        var options = WithMode(MaskingMode.Hash);

        var result1 = _sut.Apply("john@example.com", options);
        var result2 = _sut.Apply("john@example.com", options);

        result1.ShouldBe(result2);
        result1.ShouldNotBe("john@example.com");
        result1.Length.ShouldBe(64); // SHA-256 hex string
    }

    [Fact]
    public void Apply_HashMode_WithSalt_ProducesDifferentHash()
    {
        var optionsNoSalt = WithMode(MaskingMode.Hash);
        var optionsWithSalt = new MaskingOptions
        {
            Mode = MaskingMode.Hash,
            MaskCharacter = '*',
            HashSalt = "my-salt"
        };

        var resultNoSalt = _sut.Apply("john@example.com", optionsNoSalt);
        var resultWithSalt = _sut.Apply("john@example.com", optionsWithSalt);

        resultNoSalt.ShouldNotBe(resultWithSalt);
    }

    [Fact]
    public void Apply_TokenizeMode_ReturnsOriginalValue()
    {
        var result = _sut.Apply("john@example.com", WithMode(MaskingMode.Tokenize));

        result.ShouldBe("john@example.com");
    }

    [Fact]
    public void Apply_NoAtSign_AppliesGenericMask()
    {
        var result = _sut.Apply("notanemail", DefaultPartial());

        result.ShouldBe("n*********");
    }

    [Fact]
    public void Apply_SingleCharLocalPart_MasksEntireLocalPart()
    {
        var result = _sut.Apply("j@example.com", DefaultPartial());

        result.ShouldBe("***@example.com");
    }

    [Fact]
    public void Apply_EmptyLocalPart_MasksEntireLocalPart()
    {
        // Edge case: starts with @ (atIndex == 0, localPart is empty)
        var result = _sut.Apply("@example.com", DefaultPartial());

        result.ShouldBe("***@example.com");
    }

    [Fact]
    public void Apply_SubdomainEmail_PreservesFullDomain()
    {
        var result = _sut.Apply("user@mail.sub.example.com", DefaultPartial());

        result.ShouldBe("u***@mail.sub.example.com");
    }

    [Theory]
    [InlineData("ab", "**")]
    [InlineData("x", "*")]
    public void Apply_ShortValueWithoutAtSign_MasksEntireValue(string input, string expected)
    {
        var result = _sut.Apply(input, DefaultPartial());

        result.ShouldBe(expected);
    }

    [Fact]
    public void Apply_UnicodeLocalPart_MasksCorrectly()
    {
        var result = _sut.Apply("usuario@dominio.com", DefaultPartial());

        result.ShouldBe("u***@dominio.com");
    }

    [Fact]
    public void Apply_FullMode_SingleCharLocal_MasksWithThreeStars()
    {
        var result = _sut.Apply("a@example.com", WithMode(MaskingMode.Full));

        result.ShouldBe("***@example.com");
    }
}
