using System.Collections.Immutable;
using Encina.Messaging.Encryption.DataProtection;
using Encina.Messaging.Encryption.Model;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.PropertyTests.Messaging.Encryption.DataProtection;

/// <summary>
/// Property-based tests for <see cref="DataProtectionMessageEncryptionProvider"/> invariants.
/// Uses EphemeralDataProtectionProvider for in-memory testing without external dependencies.
/// </summary>
[Trait("Category", "Property")]
public sealed class DataProtectionPropertyTests
{
    private readonly DataProtectionMessageEncryptionProvider _provider;

    public DataProtectionPropertyTests()
    {
        var dpProvider = new EphemeralDataProtectionProvider();
        var options = Options.Create(new DataProtectionEncryptionOptions());
        var logger = NullLogger<DataProtectionMessageEncryptionProvider>.Instance;
        _provider = new DataProtectionMessageEncryptionProvider(dpProvider, options, logger);
    }

    #region Encrypt/Decrypt Round-Trip

    /// <summary>
    /// Property: For any non-empty byte array, Encrypt then Decrypt returns the original bytes.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Property_EncryptDecrypt_RoundTrip_ReturnsOriginalBytes()
    {
        return Prop.ForAll(
            Arb.From(GenNonEmptyByteArray()),
            plaintext =>
            {
                var context = new MessageEncryptionContext
                {
                    KeyId = "test-key",
                    MessageType = "TestMessage",
                    MessageId = Guid.NewGuid()
                };

                var encryptResult = _provider.EncryptAsync(
                    new ReadOnlyMemory<byte>(plaintext), context).AsTask().GetAwaiter().GetResult();

                return encryptResult.Match(
                    Left: _ => false,
                    Right: encrypted =>
                    {
                        var decryptResult = _provider.DecryptAsync(
                            encrypted, context).AsTask().GetAwaiter().GetResult();

                        return decryptResult.Match(
                            Left: _ => false,
                            Right: decrypted => decrypted.SequenceEqual(plaintext.ToImmutableArray()));
                    });
            });
    }

    /// <summary>
    /// Property: Encrypt always returns a Right result for valid input.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Property_Encrypt_AlwaysSucceeds_ForValidInput()
    {
        return Prop.ForAll(
            Arb.From(GenNonEmptyByteArray()),
            plaintext =>
            {
                var context = new MessageEncryptionContext
                {
                    KeyId = "test-key",
                    MessageType = "TestMessage",
                    MessageId = Guid.NewGuid()
                };

                var result = _provider.EncryptAsync(
                    new ReadOnlyMemory<byte>(plaintext), context).AsTask().GetAwaiter().GetResult();

                return result.IsRight;
            });
    }

    #endregion

    #region Ciphertext Randomness

    /// <summary>
    /// Property: Two encryptions of the same data produce different ciphertexts (randomness).
    /// </summary>
    [Property(MaxTest = 50)]
    public Property Property_TwoEncryptions_ProduceDifferentCiphertexts()
    {
        return Prop.ForAll(
            Arb.From(GenNonEmptyByteArray()),
            plaintext =>
            {
                var context = new MessageEncryptionContext
                {
                    KeyId = "test-key",
                    MessageType = "TestMessage",
                    MessageId = Guid.NewGuid()
                };

                var result1 = _provider.EncryptAsync(
                    new ReadOnlyMemory<byte>(plaintext), context).AsTask().GetAwaiter().GetResult();
                var result2 = _provider.EncryptAsync(
                    new ReadOnlyMemory<byte>(plaintext), context).AsTask().GetAwaiter().GetResult();

                return result1.Match(
                    Left: _ => false,
                    Right: encrypted1 => result2.Match(
                        Left: _ => false,
                        Right: encrypted2 =>
                            !encrypted1.Ciphertext.SequenceEqual(encrypted2.Ciphertext)));
            });
    }

    #endregion

    #region Encrypted Payload Metadata

    /// <summary>
    /// Property: Encrypted payload always has the "data-protection" KeyId.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property Property_EncryptedPayload_HasDataProtectionKeyId()
    {
        return Prop.ForAll(
            Arb.From(GenNonEmptyByteArray()),
            plaintext =>
            {
                var context = new MessageEncryptionContext
                {
                    KeyId = "test-key",
                    MessageType = "TestMessage",
                    MessageId = Guid.NewGuid()
                };

                var result = _provider.EncryptAsync(
                    new ReadOnlyMemory<byte>(plaintext), context).AsTask().GetAwaiter().GetResult();

                return result.Match(
                    Left: _ => false,
                    Right: encrypted => encrypted.KeyId == "data-protection"
                                        && encrypted.Algorithm == "DataProtection"
                                        && encrypted.Version == 1);
            });
    }

    #endregion

    #region Generators

    /// <summary>
    /// Generates non-empty byte arrays of varying lengths.
    /// </summary>
    private static Gen<byte[]> GenNonEmptyByteArray()
    {
        return Gen.Choose(1, 256)
            .SelectMany(length =>
                Gen.ArrayOf<byte>(Gen.Choose(0, 255).Select(i => (byte)i), length));
    }

    #endregion
}
