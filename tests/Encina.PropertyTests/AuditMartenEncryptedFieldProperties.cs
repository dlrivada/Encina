using System.Security.Cryptography;

using Encina.Audit.Marten.Events;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests;

/// <summary>
/// Issue #2770: whatever is stored in an <see cref="EncryptedField"/>, reading it either returns the
/// original plaintext or fails with <see cref="EncryptedFieldDecryptionException"/>; arbitrary garbage is never
/// returned as plaintext and never raises another exception type.
/// </summary>
[Trait("Category", "Property")]
[Trait("Provider", "Marten")]
public sealed class AuditMartenEncryptedFieldProperties
{
    private static readonly byte[] Key = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();

    private static readonly string FixedMessage =
        new EncryptedFieldDecryptionException(EncryptedFieldDecryptionFailureReason.NotEncrypted).Message;

    [Property(MaxTest = 300)]
    public bool Garbage_NeverReturnsTheEnvelopeAndOnlyThrowsTheTypedFailure(string? garbage, bool withMarker)
    {
        var value = (withMarker ? "{\"__enc\":true" : string.Empty) + (garbage ?? string.Empty);
        return OnlyTypedFailure(new EncryptedField { Value = value });
    }

    [Property(MaxTest = 300)]
    public bool CorruptedValidEnvelope_NeverLeaksAndOnlyThrowsTheTypedFailure(NonNegativeInt position, byte replacement)
    {
        var good = EncryptedField.Encrypt("plain-secret", Key, "kid-1").Value!;
        var chars = good.ToCharArray();
        var index = position.Get % chars.Length;
        chars[index] = (char)replacement;
        var value = new string(chars);

        // A replacement can leave the envelope byte-identical or change only ignorable base64 padding bits;
        // then the only acceptable plaintext is the original one. Anything else must be the typed failure.
        return OnlyTypedFailureOrExactly(new EncryptedField { Value = value }, "plain-secret");
    }

    [Property(MaxTest = 300)]
    public bool NonThrowingRead_AgreesWithTheThrowingRead(string? garbage, bool withMarker, bool corruptValid, NonNegativeInt position, byte replacement)
    {
        string value;
        if (corruptValid)
        {
            var chars = EncryptedField.Encrypt("plain-secret", Key, "kid-1").Value!.ToCharArray();
            chars[position.Get % chars.Length] = (char)replacement;
            value = new string(chars);
        }
        else
        {
            value = (withMarker ? "{\"__enc\":true" : string.Empty) + (garbage ?? string.Empty);
        }

        var field = new EncryptedField { Value = value };
        var ok = field.TryDecrypt(Key, out var plaintext, out var reason);

        try
        {
            return field.Decrypt(Key) == plaintext && ok;
        }
        catch (EncryptedFieldDecryptionException ex)
        {
            return !ok && plaintext is null && ex.Reason == reason;
        }
    }

    [Property(MaxTest = 50)]
    public bool NullPlaintext_StaysNullInEveryRead(NonNegativeInt unused)
    {
        var field = EncryptedField.Encrypt(null, Key, "kid-1");

        return field.TryDecrypt(Key, out var plaintext, out _)
            && plaintext is null
            && field.Decrypt(Key) is null
            && field.DecryptOrPlaceholder(null) is null;
    }

    [Property(MaxTest = 200)]
    public bool LoneSurrogateInMarkedValue_IsMalformedInBothReads(NonNegativeInt offset)
    {
        var surrogate = (char)(0xD800 + (offset.Get % 0x400));
        var field = new EncryptedField { Value = "{\"__enc\":true,\"kid\":\"" + surrogate + "\"}" };

        return !field.TryDecrypt(Key, out _, out var reason)
            && reason == EncryptedFieldDecryptionFailureReason.EnvelopeMalformed
            && Throws(field, EncryptedFieldDecryptionFailureReason.EnvelopeMalformed);
    }

    [Property(MaxTest = 100)]
    public bool DestroyedKey_ReadsAsPlaceholderWithoutTouchingTheEnvelope(string? placeholder)
    {
        var text = string.IsNullOrWhiteSpace(placeholder) ? "[SHREDDED]" : placeholder;
        var field = EncryptedField.Encrypt("plain-secret", Key, "kid-1");

        return field.DecryptOrPlaceholder(null, text) == text;
    }

    private static bool Throws(EncryptedField field, EncryptedFieldDecryptionFailureReason reason)
    {
        try
        {
            field.Decrypt(Key);
            return false;
        }
        catch (EncryptedFieldDecryptionException ex)
        {
            return ex.Reason == reason;
        }
    }

    [Property(MaxTest = 200)]
    public bool RoundTrip_ReturnsThePlaintext(string? plaintext)
    {
        if (plaintext is null || plaintext.Any(char.IsSurrogate))
        {
            return true;
        }

        return EncryptedField.Encrypt(plaintext, Key, "kid-1").Decrypt(Key) == plaintext;
    }

    [Property(MaxTest = 200)]
    public bool WrongKeyLength_AlwaysTypedFailure(PositiveInt size)
    {
        var length = size.Get % 80;
        if (length == 32)
        {
            return true;
        }

        var field = EncryptedField.Encrypt("x", Key, "kid-1");
        var key = new byte[length];
        RandomNumberGenerator.Fill(key);

        try
        {
            field.Decrypt(key);
            return false;
        }
        catch (EncryptedFieldDecryptionException)
        {
            return true;
        }
    }

    private static bool OnlyTypedFailure(EncryptedField field) => OnlyTypedFailureOrExactly(field, null);

    private static bool OnlyTypedFailureOrExactly(EncryptedField field, string? expectedPlaintext)
    {
        try
        {
            var result = field.Decrypt(Key);
            return expectedPlaintext is not null && result == expectedPlaintext;
        }
        catch (EncryptedFieldDecryptionException ex)
        {
            // The message is fixed for every cause, so it can never carry any part of the stored value.
            return ex.Message == FixedMessage;
        }
    }
}
