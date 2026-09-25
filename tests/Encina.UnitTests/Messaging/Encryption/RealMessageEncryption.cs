using System.Security.Cryptography;
using Encina.Messaging.Encryption;
using Encina.Messaging.Encryption.Serialization;
using Encina.Messaging.Serialization;
using Encina.Security.Encryption;
using Encina.Security.Encryption.Abstractions;
using Encina.Security.Encryption.Algorithms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Messaging.Encryption;

/// <summary>
/// Builds a real <see cref="EncryptingMessageSerializer"/> (AES-256-GCM through
/// <see cref="DefaultMessageEncryptionProvider"/> and <see cref="AesGcmFieldEncryptor"/>) over an
/// <see cref="InMemoryKeyProvider"/>, so store tests exercise the same encryption path as production.
/// </summary>
internal static class RealMessageEncryption
{
    public const string KeyId = "unit-test-key";

    /// <summary>Creates an encrypting serializer that encrypts every message.</summary>
    public static EncryptingMessageSerializer CreateSerializer()
    {
        var keyProvider = CreateKeyProvider();

        return new EncryptingMessageSerializer(
            new JsonMessageSerializer(),
            new DefaultMessageEncryptionProvider(new AesGcmFieldEncryptor(keyProvider), keyProvider),
            Options.Create(new MessageEncryptionOptions { Enabled = true, EncryptAllMessages = true }),
            NullLogger<EncryptingMessageSerializer>.Instance);
    }

    /// <summary>
    /// Registers the key material <c>AddEncinaMessageEncryption</c> needs
    /// (<see cref="IKeyProvider"/> and <see cref="IFieldEncryptor"/>).
    /// </summary>
    public static IServiceCollection AddKeyMaterial(this IServiceCollection services)
    {
        var keyProvider = CreateKeyProvider();
        services.AddSingleton<IKeyProvider>(keyProvider);
        services.AddSingleton<IFieldEncryptor>(new AesGcmFieldEncryptor(keyProvider));
        return services;
    }

    private static InMemoryKeyProvider CreateKeyProvider()
    {
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);

        var keyProvider = new InMemoryKeyProvider();
        keyProvider.AddKey(KeyId, key);
        keyProvider.SetCurrentKey(KeyId);
        return keyProvider;
    }
}
