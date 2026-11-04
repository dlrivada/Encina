using Encina.Audit.Marten.Events;
using Encina.Audit.Marten.Projections;

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

    [Fact]
    public void OperationAuditEntryReadModel_UnreadableFlag_DefaultsToFalseAndIsSettable()
    {
        var model = new OperationAuditEntryReadModel();
        model.IsExpired.ShouldBeFalse();
        model.IsUnreadable.ShouldBeFalse();
        model.IsUnreadable = true;
        model.IsUnreadable.ShouldBeTrue();
    }

    [Fact]
    public void ReadAuditEntryReadModel_UnreadableFlag_DefaultsToFalseAndIsSettable()
    {
        var model = new ReadAuditEntryReadModel();
        model.IsUnreadable.ShouldBeFalse();
        model.IsUnreadable = true;
        model.IsUnreadable.ShouldBeTrue();
    }

    [Fact]
    public void Decrypt_UnmarkedValue_ThrowsTypedFailure()
    {
        var field = new EncryptedField { Value = "not-an-envelope" };

        Should.Throw<EncryptedFieldDecryptionException>(() => field.Decrypt(new byte[32]))
            .Reason.ShouldBe(EncryptedFieldDecryptionFailureReason.NotEncrypted);
    }

    [Fact]
    public void Decrypt_MalformedEnvelope_ThrowsTypedFailure()
    {
        var field = new EncryptedField { Value = "{\"__enc\":true,\"ct\":" };

        Should.Throw<EncryptedFieldDecryptionException>(() => field.Decrypt(new byte[32]))
            .Reason.ShouldBe(EncryptedFieldDecryptionFailureReason.EnvelopeMalformed);
    }

    [Fact]
    public void Decrypt_WrongKeyLength_ThrowsTypedFailure()
    {
        var key = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(key);
        var field = EncryptedField.Encrypt("test", key, "key-1");

        Should.Throw<EncryptedFieldDecryptionException>(() => field.Decrypt(new byte[16]))
            .Reason.ShouldBe(EncryptedFieldDecryptionFailureReason.InvalidKeyOrParameters);
    }

    [Fact]
    public void Decrypt_WrongKey_ThrowsIntegrityFailure()
    {
        var key = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(key);
        var field = EncryptedField.Encrypt("test", key, "key-1");

        Should.Throw<EncryptedFieldDecryptionException>(() => field.Decrypt(new byte[32]))
            .Reason.ShouldBe(EncryptedFieldDecryptionFailureReason.IntegrityCheckFailed);
    }
}
