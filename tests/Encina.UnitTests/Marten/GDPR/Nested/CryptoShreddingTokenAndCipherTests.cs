using System.Security.Cryptography;

using Encina.Marten.GDPR;

namespace Encina.UnitTests.Marten.GDPR.Nested;

[Trait("Category", "Unit")]
public sealed class CryptoShreddingTokenAndCipherTests
{
    private static readonly byte[] Key = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();

    [Fact]
    public void Token_FormatAndParse_RoundTrip()
    {
        var token = CryptoShreddingToken.Format(3, new byte[12], new byte[] { 1, 2, 3 }, new byte[16]);

        token.ShouldStartWith("cs2:3:");
        CryptoShreddingToken.TryParse(token, out var parsed).ShouldBeTrue();
        parsed.Version.ShouldBe(3);
        parsed.Ciphertext.ToArray().ShouldBe(new byte[] { 1, 2, 3 });
        parsed.Nonce.Length.ShouldBe(12);
        parsed.Tag.Length.ShouldBe(16);
    }

    [Theory]
    [InlineData("plain")]
    [InlineData("cs1:1:AAAA")]
    [InlineData("cs2:0:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    [InlineData("cs2:-1:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    [InlineData("cs2:x:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    [InlineData("cs2::AAAA")]
    [InlineData("cs2:1:not base64 !!")]
    [InlineData("cs2:1:AAAA")]
    [InlineData("cs2:erased")]
    public void Token_StrictParsing_RejectsAnythingElse(string value) =>
        CryptoShreddingToken.TryParse(value, out _).ShouldBeFalse();

    [Fact]
    public void Tombstone_IsRecognisedOrdinally()
    {
        CryptoShreddingToken.IsTombstone("cs2:erased").ShouldBeTrue();
        CryptoShreddingToken.IsTombstone("CS2:ERASED").ShouldBeFalse();
    }

    [Fact]
    public void Cipher_RoundTrip_WithAFreshNoncePerCall()
    {
        using var aes = CryptoShreddingFieldCipher.CreateAes(Key)!;

        var first = CryptoShreddingFieldCipher.Encrypt(aes, "s", 1, "value");
        var second = CryptoShreddingFieldCipher.Encrypt(aes, "s", 1, "value");

        first.ShouldNotBe(second);
        CryptoShreddingToken.TryParse(first, out var token).ShouldBeTrue();
        CryptoShreddingFieldCipher.Decrypt(aes, "s", token).ShouldBe("value");
    }

    [Fact]
    public void Cipher_SwappedSubjectOrVersion_FailsTheTag()
    {
        using var aes = CryptoShreddingFieldCipher.CreateAes(Key)!;
        var nonce = new byte[12];
        CryptoShreddingToken.TryParse(CryptoShreddingFieldCipher.Encrypt(aes, "s", 1, "value", nonce), out var token).ShouldBeTrue();
        CryptoShreddingToken.TryParse(CryptoShreddingToken.Format(2, token.Nonce, token.Ciphertext, token.Tag), out var otherVersion).ShouldBeTrue();

        Should.Throw<AuthenticationTagMismatchException>(() => CryptoShreddingFieldCipher.Decrypt(aes, "other", token));
        Should.Throw<AuthenticationTagMismatchException>(() => CryptoShreddingFieldCipher.Decrypt(aes, "s", otherVersion));
    }

    [Theory]
    [InlineData(16)]
    [InlineData(24)]
    [InlineData(31)]
    [InlineData(0)]
    public void Cipher_OnlyThirtyTwoByteKeysAreAccepted(int length) =>
        CryptoShreddingFieldCipher.CreateAes(new byte[length]).ShouldBeNull();

    [Fact]
    public void Cipher_NullKey_IsRejected() => CryptoShreddingFieldCipher.CreateAes(null).ShouldBeNull();
}
