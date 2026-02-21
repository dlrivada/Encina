using Encina.Security.PII;
using Encina.Security.PII.Strategies;

namespace Encina.UnitTests.Security.PII.Strategies;

public sealed class IPAddressMaskingStrategyTests
{
    private readonly IPAddressMaskingStrategy _sut = new();

    private static MaskingOptions DefaultPartial() => new()
    {
        Mode = MaskingMode.Partial,
        MaskCharacter = '*',
        PreserveLength = false,
        VisibleCharactersStart = 0,
        VisibleCharactersEnd = 0
    };

    private static MaskingOptions WithMode(MaskingMode mode) => new()
    {
        Mode = mode,
        MaskCharacter = '*',
        PreserveLength = false,
        VisibleCharactersStart = 0,
        VisibleCharactersEnd = 0
    };

    [Fact]
    public void Apply_IPv4_PreservesFirstTwoOctets()
    {
        var result = _sut.Apply("192.168.1.100", DefaultPartial());

        result.ShouldBe("192.168.***.***");
    }

    [Fact]
    public void Apply_IPv4_DifferentAddress_PreservesFirstTwoOctets()
    {
        var result = _sut.Apply("10.0.0.1", DefaultPartial());

        result.ShouldBe("10.0.***.***");
    }

    [Fact]
    public void Apply_IPv6_PreservesFirstTwoGroups()
    {
        var result = _sut.Apply("2001:0db8:85a3:0000:0000:8a2e:0370:7334", DefaultPartial());

        result.ShouldBe("2001:0db8:****:****:****:****:****:****");
    }

    [Fact]
    public void Apply_IPv6_ShortFormat_PreservesFirstTwoGroups()
    {
        var result = _sut.Apply("fe80:1234:abcd:ef01:0000:0000:0000:0001", DefaultPartial());

        result.ShouldBe("fe80:1234:****:****:****:****:****:****");
    }

    [Fact]
    public void Apply_FullMode_MasksEntireValue()
    {
        var result = _sut.Apply("192.168.1.100", WithMode(MaskingMode.Full));

        result.ShouldBe("*************");
    }

    [Fact]
    public void Apply_RedactMode_ReturnsRedactedPlaceholder()
    {
        var result = _sut.Apply("192.168.1.100", WithMode(MaskingMode.Redact));

        result.ShouldBe("[REDACTED]");
    }

    [Fact]
    public void Apply_RedactMode_UsesCustomPlaceholder()
    {
        var options = new MaskingOptions
        {
            Mode = MaskingMode.Redact,
            MaskCharacter = '*',
            PreserveLength = false,
            RedactedPlaceholder = "[IP REMOVED]"
        };

        var result = _sut.Apply("192.168.1.100", options);

        result.ShouldBe("[IP REMOVED]");
    }

    [Fact]
    public void Apply_HashMode_ReturnsDeterministicHash()
    {
        var options = WithMode(MaskingMode.Hash);

        var result1 = _sut.Apply("192.168.1.100", options);
        var result2 = _sut.Apply("192.168.1.100", options);

        result1.ShouldBe(result2);
        result1.ShouldNotBe("192.168.1.100");
        result1.Length.ShouldBe(64);
    }

    [Fact]
    public void Apply_TokenizeMode_ReturnsOriginalValue()
    {
        var result = _sut.Apply("192.168.1.100", WithMode(MaskingMode.Tokenize));

        result.ShouldBe("192.168.1.100");
    }

    [Fact]
    public void Apply_InvalidIPv4_MasksEntireValue()
    {
        // Only 3 octets - not valid IPv4
        var result = _sut.Apply("192.168.1", DefaultPartial());

        result.ShouldBe("*********");
    }

    [Fact]
    public void Apply_IPv6_FullMode_MasksEntireValue()
    {
        var input = "2001:0db8:85a3:0000:0000:8a2e:0370:7334";
        var result = _sut.Apply(input, WithMode(MaskingMode.Full));

        result.Length.ShouldBe(input.Length);
        result.ShouldAllBe(c => c == '*');
    }

    [Theory]
    [InlineData("192.168.1.100", "192.168.***.***")]
    [InlineData("10.0.0.1", "10.0.***.***")]
    [InlineData("172.16.254.1", "172.16.***.***")]
    public void Apply_VariousIPv4_MasksCorrectly(string input, string expected)
    {
        var result = _sut.Apply(input, DefaultPartial());

        result.ShouldBe(expected);
    }

    [Fact]
    public void Apply_HashMode_WithSalt_ProducesDifferentHash()
    {
        var optionsNoSalt = WithMode(MaskingMode.Hash);
        var optionsWithSalt = new MaskingOptions
        {
            Mode = MaskingMode.Hash,
            MaskCharacter = '*',
            PreserveLength = false,
            HashSalt = "ip-salt"
        };

        var resultNoSalt = _sut.Apply("192.168.1.100", optionsNoSalt);
        var resultWithSalt = _sut.Apply("192.168.1.100", optionsWithSalt);

        resultNoSalt.ShouldNotBe(resultWithSalt);
    }

    [Fact]
    public void Apply_Localhost_PreservesFirstTwoOctets()
    {
        var result = _sut.Apply("127.0.0.1", DefaultPartial());

        result.ShouldBe("127.0.***.***");
    }
}
