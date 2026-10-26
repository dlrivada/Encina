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
        return OnlyTypedFailureOrNeverTheValue(new EncryptedField { Value = value }, value);
    }

    [Property(MaxTest = 300)]
    public bool CorruptedValidEnvelope_NeverLeaksAndOnlyThrowsTheTypedFailure(NonNegativeInt position, byte replacement)
    {
        var good = EncryptedField.Encrypt("plain-secret", Key, "kid-1").Value!;
        var chars = good.ToCharArray();
        var index = position.Get % chars.Length;
        chars[index] = (char)replacement;
        var value = new string(chars);

        return OnlyTypedFailureOrNeverTheValue(new EncryptedField { Value = value }, value);
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

    private static bool OnlyTypedFailureOrNeverTheValue(EncryptedField field, string value)
    {
        try
        {
            var result = field.Decrypt(Key);
            return result != value;
        }
        catch (EncryptedFieldDecryptionException ex)
        {
            // The message is fixed for every cause, so it can never carry any part of the stored value.
            return ex.Message == FixedMessage;
        }
    }
}
