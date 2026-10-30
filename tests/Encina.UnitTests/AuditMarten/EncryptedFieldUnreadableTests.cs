using System.Security.Cryptography;
using System.Text.Json;

using Encina.Audit.Marten.Events;

using Shouldly;

namespace Encina.UnitTests.AuditMarten;

/// <summary>
/// Issue #2770 (D14): every stored value that cannot be read, except a legitimately destroyed key, fails with
/// <see cref="EncryptedFieldDecryptionException"/> carrying a fixed message; nothing is returned as plaintext
/// or reported as erased.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Provider", "Marten")]
public sealed class EncryptedFieldUnreadableTests
{
    private const string KeyId = "temporal:2026-03:v1-secret-kid";
    private const string Plaintext = "plaintext-user-4711";

    private static byte[] NewKey(int size = 32)
    {
        var key = new byte[size];
        RandomNumberGenerator.Fill(key);
        return key;
    }

    private static (byte[] Key, byte[] Ct, byte[] Nonce, byte[] Tag) Parts()
    {
        var key = NewKey();
        var field = EncryptedField.Encrypt(Plaintext, key, KeyId);
        using var doc = JsonDocument.Parse(field.Value!);
        var root = doc.RootElement;
        return (key, root.GetProperty("ct").GetBytesFromBase64(), root.GetProperty("n").GetBytesFromBase64(), root.GetProperty("t").GetBytesFromBase64());
    }

    private static EncryptedField Envelope(byte[] ct, byte[] nonce, byte[] tag) =>
        new() { Value = $"{{\"__enc\":true,\"v\":1,\"kid\":\"{KeyId}\",\"ct\":\"{Convert.ToBase64String(ct)}\",\"n\":\"{Convert.ToBase64String(nonce)}\",\"t\":\"{Convert.ToBase64String(tag)}\"}}", KeyId = KeyId };

    private static EncryptedFieldDecryptionException AssertFails(EncryptedField field, byte[] key, EncryptedFieldDecryptionFailureReason reason)
    {
        var decrypt = Should.Throw<EncryptedFieldDecryptionException>(() => field.Decrypt(key));
        var projected = Should.Throw<EncryptedFieldDecryptionException>(() => field.DecryptOrPlaceholder(key));

        foreach (var ex in new[] { decrypt, projected })
        {
            ex.Reason.ShouldBe(reason);
            ex.InnerException.ShouldBeNull();
            ex.Message.ShouldContain(EncryptedFieldDecryptionException.Code);
            ex.Message.ShouldNotContain(KeyId);
            ex.Message.ShouldNotContain(Plaintext);
            ex.Message.ShouldNotContain("__enc");
            ex.ToString().ShouldNotContain(Plaintext);
        }

        decrypt.Message.ShouldBe(projected.Message);
        return decrypt;
    }

    [Fact]
    public void FlippedTag_Fails()
    {
        var (key, ct, nonce, tag) = Parts();
        tag[0] ^= 0xFF;
        AssertFails(Envelope(ct, nonce, tag), key, EncryptedFieldDecryptionFailureReason.IntegrityCheckFailed);
    }

    [Fact]
    public void FlippedCiphertext_Fails()
    {
        var (key, ct, nonce, tag) = Parts();
        ct[0] ^= 0xFF;
        AssertFails(Envelope(ct, nonce, tag), key, EncryptedFieldDecryptionFailureReason.IntegrityCheckFailed);
    }

    [Fact]
    public void WrongNonNullKey_FailsAndIsNotReportedAsShredded()
    {
        var (_, ct, nonce, tag) = Parts();
        var field = Envelope(ct, nonce, tag);
        AssertFails(field, NewKey(), EncryptedFieldDecryptionFailureReason.IntegrityCheckFailed);
        field.DecryptOrPlaceholder(null).ShouldBe("[SHREDDED]");
    }

    [Theory]
    [InlineData(5)]
    [InlineData(11)]
    [InlineData(13)]
    public void WrongNonceLength_Fails(int length)
    {
        var (key, ct, _, tag) = Parts();
        AssertFails(Envelope(ct, new byte[length], tag), key, EncryptedFieldDecryptionFailureReason.InvalidKeyOrParameters);
    }

    [Theory]
    [InlineData(15)]
    [InlineData(17)]
    public void WrongTagLength_Fails(int length)
    {
        var (key, ct, nonce, _) = Parts();
        AssertFails(Envelope(ct, nonce, new byte[length]), key, EncryptedFieldDecryptionFailureReason.InvalidKeyOrParameters);
    }

    [Theory]
    [InlineData(31)]
    [InlineData(33)]
    [InlineData(0)]
    public void WrongKeyLength_Fails(int length)
    {
        var field = EncryptedField.Encrypt(Plaintext, NewKey(), KeyId);
        AssertFails(field, NewKey(length), EncryptedFieldDecryptionFailureReason.InvalidKeyOrParameters);
    }

    [Theory]
    [InlineData("{\"__enc\":true,\"v\":1,\"n\":\"AAAAAAAAAAAAAAAA\",\"t\":\"AAAAAAAAAAAAAAAAAAAAAA==\"}")]
    [InlineData("{\"__enc\":true,\"v\":1,\"ct\":\"AAAA\",\"t\":\"AAAAAAAAAAAAAAAAAAAAAA==\"}")]
    [InlineData("{\"__enc\":true,\"v\":1,\"ct\":\"AAAA\",\"n\":\"AAAAAAAAAAAAAAAA\"}")]
    [InlineData("{\"__enc\":true,\"v\":1,\"ct\":\"###\",\"n\":\"AAAAAAAAAAAAAAAA\",\"t\":\"AAAAAAAAAAAAAAAAAAAAAA==\"}")]
    [InlineData("{\"__enc\":true,\"v\":1,\"ct\":5,\"n\":\"AAAAAAAAAAAAAAAA\",\"t\":\"AAAAAAAAAAAAAAAAAAAAAA==\"}")]
    [InlineData("{\"__enc\":true,\"v\":1,\"ct\":\"AAAA\",\"n\":null,\"t\":\"AAAAAAAAAAAAAAAAAAAAAA==\"}")]
    [InlineData("{\"__enc\":true,\"v\":1,\"ct\":\"AAAA\",\"n\":[],\"t\":{}}")]
    [InlineData("{\"__enc\":true,\"ct\":")]
    [InlineData("{\"__enc\":true}trailing")]
    public void MalformedEnvelope_Fails(string value)
    {
        AssertFails(new EncryptedField { Value = value, KeyId = KeyId }, NewKey(), EncryptedFieldDecryptionFailureReason.EnvelopeMalformed);
    }

    [Theory]
    [InlineData("plain text")]
    [InlineData("")]
    [InlineData("{\"v\":1}")]
    [InlineData("###")]
    public void ValueWithoutMarker_FailsInsteadOfBeingReturnedAsPlaintext(string value)
    {
        AssertFails(new EncryptedField { Value = value }, NewKey(), EncryptedFieldDecryptionFailureReason.NotEncrypted);
    }

    [Fact]
    public void InvalidUtf16Surrogate_IsMalformedNotAnUnexpectedException()
    {
        var field = new EncryptedField { Value = "{\"__enc\":true,\"kid\":\"\ud800\"}" };
        AssertFails(field, NewKey(), EncryptedFieldDecryptionFailureReason.EnvelopeMalformed);
    }

    [Fact]
    public void NullKey_ReturnsPlaceholder_NullValueStaysNull()
    {
        var field = EncryptedField.Encrypt(Plaintext, NewKey(), KeyId);

        field.DecryptOrPlaceholder(null).ShouldBe("[SHREDDED]");
        field.DecryptOrPlaceholder(null, "<erased>").ShouldBe("<erased>");
        new EncryptedField().DecryptOrPlaceholder(null).ShouldBeNull();
        new EncryptedField().DecryptOrPlaceholder(NewKey()).ShouldBeNull();
    }

    [Fact]
    public void NullKeyWithUnmarkedValue_StillReturnsPlaceholder()
    {
        // A destroyed key means there is nothing to read; the placeholder is the legitimate answer (D15).
        new EncryptedField { Value = "garbage" }.DecryptOrPlaceholder(null).ShouldBe("[SHREDDED]");
    }

    [Fact]
    public void Exception_ExposesCodeAndReason()
    {
        var ex = new EncryptedFieldDecryptionException(EncryptedFieldDecryptionFailureReason.NotEncrypted);

        EncryptedFieldDecryptionException.Code.ShouldBe("audit.marten.encrypted_field_unreadable");
        ex.Reason.ShouldBe(EncryptedFieldDecryptionFailureReason.NotEncrypted);
        ex.ShouldBeAssignableTo<CryptographicException>();
    }
}
