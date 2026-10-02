using Encina.Security.PII.Strategies;

namespace Encina.UnitTests.Security.PII.Strategies;

public sealed class HashHelperTests
{
    [Fact]
    public void ComputeHash_WithKey_MatchesRfc4231TestCase1()
    {
        // RFC 4231 test case 1: key = 20 bytes of 0x0b, data = "Hi There"
        var key = new string('\u000b', 20);

        var hash = HashHelper.ComputeHash("Hi There", key);

        hash.ShouldBe("b0344c61d8db38535ca8afceaf0bf12b881dc200c9833da726e9376c2e32cff7");
    }

    [Fact]
    public void ComputeHash_WithKey_MatchesRfc4231TestCase2()
    {
        // RFC 4231 test case 2: key = "Jefe", data = "what do ya want for nothing?"
        var hash = HashHelper.ComputeHash("what do ya want for nothing?", "Jefe");

        hash.ShouldBe("5bdcc146bf60754e6a042426089575c75a003f089d2739839dec58b964ec3843");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ComputeHash_WithoutKey_IsPlainSha256(string? key)
    {
        var hash = HashHelper.ComputeHash("abc", key);

        hash.ShouldBe("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");
    }

    [Fact]
    public void ComputeHash_KeyValueBoundary_ProducesDifferentHashes()
    {
        // A concatenating construction (key + value) would make these collide.
        var first = HashHelper.ComputeHash("c", "ab");
        var second = HashHelper.ComputeHash("bc", "a");

        first.ShouldNotBe(second);
    }

    [Fact]
    public void ComputeHash_SameInputs_AreDeterministicLowercaseHex()
    {
        var first = HashHelper.ComputeHash("123-45-6789", "secret");
        var second = HashHelper.ComputeHash("123-45-6789", "secret");

        first.ShouldBe(second);
        first.Length.ShouldBe(64);
        first.ShouldBe(first.ToLowerInvariant());
    }

    [Fact]
    public void ComputeHash_DifferentKeys_ProduceDifferentHashes()
    {
        HashHelper.ComputeHash("value", "key-one")
            .ShouldNotBe(HashHelper.ComputeHash("value", "key-two"));
    }
}
