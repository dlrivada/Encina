using System.Collections.Immutable;
using Encina.Messaging.Encryption;
using Encina.Messaging.Encryption.Model;
using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Messaging.Encryption;

/// <summary>
/// Property-based tests for message encryption invariants.
/// Uses FsCheck to verify behavioral properties that must hold for all valid inputs.
/// </summary>
public sealed class MessageEncryptionPropertyTests
{
    #region EncryptedPayloadFormatter Roundtrip

    [Property(MaxTest = 100)]
    public bool FormatThenTryParse_AlwaysRoundTrips(
        NonEmptyString keyId,
        NonEmptyString algorithm,
        byte[] nonce,
        byte[] tag,
        byte[] ciphertext)
    {
        if (nonce is null || nonce.Length == 0) return true;
        if (tag is null || tag.Length == 0) return true;
        if (ciphertext is null || ciphertext.Length == 0) return true;

        // Filter out strings with colons (they break the format separator)
        if (keyId.Get.Contains(':') || algorithm.Get.Contains(':')) return true;

        var original = new EncryptedPayload
        {
            Ciphertext = [.. ciphertext],
            KeyId = keyId.Get,
            Algorithm = algorithm.Get,
            Nonce = [.. nonce],
            Tag = [.. tag],
            Version = 1
        };

        var formatted = EncryptedPayloadFormatter.Format(original);
        if (!EncryptedPayloadFormatter.TryParse(formatted, out var parsed))
            return false;

        return parsed!.KeyId == original.KeyId
               && parsed.Algorithm == original.Algorithm
               && parsed.Version == original.Version
               && parsed.Nonce.SequenceEqual(original.Nonce)
               && parsed.Tag.SequenceEqual(original.Tag)
               && parsed.Ciphertext.SequenceEqual(original.Ciphertext);
    }

    [Property(MaxTest = 100)]
    public bool IsEncrypted_TrueForFormattedPayloads(
        NonEmptyString keyId,
        NonEmptyString algorithm)
    {
        // Filter out strings with colons
        if (keyId.Get.Contains(':') || algorithm.Get.Contains(':')) return true;

        var payload = new EncryptedPayload
        {
            Ciphertext = [.. new byte[] { 1, 2, 3 }],
            KeyId = keyId.Get,
            Algorithm = algorithm.Get,
            Nonce = [.. new byte[] { 4, 5, 6 }],
            Tag = [.. new byte[] { 7, 8, 9 }],
            Version = 1
        };

        var formatted = EncryptedPayloadFormatter.Format(payload);
        return EncryptedPayloadFormatter.IsEncrypted(formatted);
    }

    [Property(MaxTest = 50)]
    public bool IsEncrypted_FalseForArbitraryStrings(NonEmptyString value)
    {
        // Skip strings that happen to start with "ENC:v"
        if (value.Get.StartsWith("ENC:v", StringComparison.Ordinal)) return true;

        return !EncryptedPayloadFormatter.IsEncrypted(value.Get);
    }

    #endregion

    // Note: EncryptedPayload record equality is not structural for ImmutableArray<byte> properties.
    // ImmutableArray<T> uses reference equality, so two EncryptedPayload instances
    // with the same byte content are NOT equal via record == operator.

    #region MessageEncryptionContext Value Semantics

    [Property(MaxTest = 50)]
    public bool MessageEncryptionContext_RecordEquality_WhenIdentical(NonEmptyString tenantId)
    {
        var id = Guid.NewGuid();
        var a = new MessageEncryptionContext
        {
            KeyId = "k",
            TenantId = tenantId.Get,
            MessageType = "Test",
            MessageId = id
        };

        var b = new MessageEncryptionContext
        {
            KeyId = "k",
            TenantId = tenantId.Get,
            MessageType = "Test",
            MessageId = id
        };

        return a == b && a.GetHashCode() == b.GetHashCode();
    }

    [Property(MaxTest = 50)]
    public bool MessageEncryptionContext_WithMutation_ProducesNewInstance(NonEmptyString original)
    {
        var ctx = new MessageEncryptionContext { TenantId = original.Get };
        var mutated = ctx with { TenantId = "different" };

        return ctx.TenantId == original.Get
               && mutated.TenantId == "different"
               && ctx != mutated;
    }

    #endregion

    #region MessageEncryptionErrors Invariants

    [Property(MaxTest = 50)]
    public bool EncryptionFailed_AlwaysUsesCorrectCode(NonEmptyString messageType)
    {
        var error = MessageEncryptionErrors.EncryptionFailed(messageType.Get);
        var code = error.GetCode().IfNone(string.Empty);
        return code == MessageEncryptionErrors.EncryptionFailedCode;
    }

    [Property(MaxTest = 50)]
    public bool DecryptionFailed_AlwaysUsesCorrectCode(NonEmptyString keyId)
    {
        var error = MessageEncryptionErrors.DecryptionFailed(keyId.Get);
        var code = error.GetCode().IfNone(string.Empty);
        return code == MessageEncryptionErrors.DecryptionFailedCode;
    }

    [Property(MaxTest = 50)]
    public bool KeyNotFound_AlwaysUsesCorrectCode(NonEmptyString keyId)
    {
        var error = MessageEncryptionErrors.KeyNotFound(keyId.Get);
        var code = error.GetCode().IfNone(string.Empty);
        return code == MessageEncryptionErrors.KeyNotFoundCode;
    }

    [Property(MaxTest = 50)]
    public bool AllErrorCodes_HaveConsistentPrefix()
    {
        const string prefix = "msg_encryption.";

        var errors = new[]
        {
            MessageEncryptionErrors.EncryptionFailed(),
            MessageEncryptionErrors.DecryptionFailed("k"),
            MessageEncryptionErrors.KeyNotFound("k"),
            MessageEncryptionErrors.InvalidPayload(),
            MessageEncryptionErrors.UnsupportedVersion(99),
            MessageEncryptionErrors.TenantKeyResolutionFailed("t"),
            MessageEncryptionErrors.SerializationFailed(),
            MessageEncryptionErrors.DeserializationFailed(),
            MessageEncryptionErrors.ProviderUnavailable()
        };

        return errors.All(e =>
        {
            var code = e.GetCode().IfNone(string.Empty);
            return code.StartsWith(prefix, StringComparison.Ordinal);
        });
    }

    #endregion

    #region DefaultTenantKeyResolver Properties

    [Property(MaxTest = 50)]
    public bool DefaultTenantKeyResolver_ConsistentOutput_ForSameInput(NonEmptyString tenantId)
    {
        // Filter whitespace-only strings
        if (string.IsNullOrWhiteSpace(tenantId.Get)) return true;

        var resolver = new DefaultTenantKeyResolver(
            Microsoft.Extensions.Options.Options.Create(new MessageEncryptionOptions()));

        var result1 = resolver.ResolveKeyId(tenantId.Get);
        var result2 = resolver.ResolveKeyId(tenantId.Get);

        return result1 == result2;
    }

    [Property(MaxTest = 50)]
    public bool DefaultTenantKeyResolver_OutputContainsTenantId(NonEmptyString tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId.Get)) return true;

        var resolver = new DefaultTenantKeyResolver(
            Microsoft.Extensions.Options.Options.Create(new MessageEncryptionOptions()));

        var result = resolver.ResolveKeyId(tenantId.Get);
        return result.Contains(tenantId.Get, StringComparison.Ordinal);
    }

    [Property(MaxTest = 30)]
    public bool DefaultTenantKeyResolver_DifferentTenants_DifferentKeys(
        NonEmptyString tenant1,
        NonEmptyString tenant2)
    {
        if (string.IsNullOrWhiteSpace(tenant1.Get) || string.IsNullOrWhiteSpace(tenant2.Get)) return true;
        if (tenant1.Get == tenant2.Get) return true;

        var resolver = new DefaultTenantKeyResolver(
            Microsoft.Extensions.Options.Options.Create(new MessageEncryptionOptions()));

        return resolver.ResolveKeyId(tenant1.Get) != resolver.ResolveKeyId(tenant2.Get);
    }

    #endregion
}
