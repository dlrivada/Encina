using Encina.Audit.Marten.Events;

namespace Encina.GuardTests.AuditMarten;

public class EncryptedFieldGuardTests
{
    [Fact]
    public void Encrypt_NullKeyMaterial_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            EncryptedField.Encrypt("plaintext", null!, "key-1"));
    }

    [Fact]
    public void Encrypt_NullKeyId_Throws()
    {
        var key = new byte[32];
        Should.Throw<ArgumentException>(() =>
            EncryptedField.Encrypt("plaintext", key, null!));
    }

    [Fact]
    public void Encrypt_EmptyKeyId_Throws()
    {
        var key = new byte[32];
        Should.Throw<ArgumentException>(() =>
            EncryptedField.Encrypt("plaintext", key, ""));
    }

    [Fact]
    public void Encrypt_WhitespaceKeyId_Throws()
    {
        var key = new byte[32];
        Should.Throw<ArgumentException>(() =>
            EncryptedField.Encrypt("plaintext", key, "  "));
    }

    [Fact]
    public void Decrypt_NullKeyMaterial_Throws()
    {
        var key = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(key);
        var field = EncryptedField.Encrypt("test", key, "key-1");

        Should.Throw<ArgumentNullException>(() => field.Decrypt(null!));
    }
}
