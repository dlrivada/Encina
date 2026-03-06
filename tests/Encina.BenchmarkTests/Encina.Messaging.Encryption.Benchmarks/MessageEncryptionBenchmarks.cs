using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Encina.Messaging.Encryption;
using Encina.Messaging.Encryption.Model;
using Encina.Security.Encryption;
using Encina.Security.Encryption.Abstractions;
using Encina.Security.Encryption.Algorithms;
using LanguageExt;

namespace Encina.Messaging.Encryption.Benchmarks;

/// <summary>
/// Benchmarks for <see cref="DefaultMessageEncryptionProvider"/> encrypt/decrypt operations.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class EncryptDecryptBenchmarks
{
    private DefaultMessageEncryptionProvider _provider = null!;
    private MessageEncryptionContext _context = null!;
    private byte[] _shortPayload = null!;
    private byte[] _mediumPayload = null!;
    private byte[] _largePayload = null!;
    private EncryptedPayload _encryptedShort = null!;
    private EncryptedPayload _encryptedMedium = null!;
    private EncryptedPayload _encryptedLarge = null!;

    [GlobalSetup]
    public void Setup()
    {
        var keyProvider = new InMemoryKeyProvider();
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        keyProvider.AddKey("bench-key", key);
        keyProvider.SetCurrentKey("bench-key");

        var fieldEncryptor = new AesGcmFieldEncryptor(keyProvider);
        _provider = new DefaultMessageEncryptionProvider(fieldEncryptor, keyProvider);

        _context = new MessageEncryptionContext { KeyId = "bench-key" };

        // Generate payloads of different sizes
        _shortPayload = Encoding.UTF8.GetBytes("""{"orderId":"abc-123","total":99.99}""");
        _mediumPayload = Encoding.UTF8.GetBytes(new string('x', 1024));
        _largePayload = Encoding.UTF8.GetBytes(new string('x', 64 * 1024));

        // Pre-encrypt for decrypt benchmarks
        _encryptedShort = Encrypt(_shortPayload);
        _encryptedMedium = Encrypt(_mediumPayload);
        _encryptedLarge = Encrypt(_largePayload);
    }

    [Benchmark(Baseline = true)]
    public EncryptedPayload Encrypt_Short() => Encrypt(_shortPayload);

    [Benchmark]
    public EncryptedPayload Encrypt_Medium() => Encrypt(_mediumPayload);

    [Benchmark]
    public EncryptedPayload Encrypt_Large() => Encrypt(_largePayload);

    [Benchmark]
    public ImmutableArray<byte> Decrypt_Short() => Decrypt(_encryptedShort);

    [Benchmark]
    public ImmutableArray<byte> Decrypt_Medium() => Decrypt(_encryptedMedium);

    [Benchmark]
    public ImmutableArray<byte> Decrypt_Large() => Decrypt(_encryptedLarge);

    [Benchmark]
    public ImmutableArray<byte> Roundtrip_Short()
    {
        var encrypted = Encrypt(_shortPayload);
        return Decrypt(encrypted);
    }

    private EncryptedPayload Encrypt(byte[] plaintext)
    {
        var result = _provider.EncryptAsync(plaintext, _context).AsTask().Result;
        return result.Match(Right: p => p, Left: _ => null!);
    }

    private ImmutableArray<byte> Decrypt(EncryptedPayload payload)
    {
        var result = _provider.DecryptAsync(payload, _context).AsTask().Result;
        return result.Match(Right: b => b, Left: _ => ImmutableArray<byte>.Empty);
    }
}

/// <summary>
/// Benchmarks for <see cref="EncryptedPayloadFormatter"/> format/parse operations.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class PayloadFormatterBenchmarks
{
    private EncryptedPayload _payload = null!;
    private string _formatted = null!;

    [GlobalSetup]
    public void Setup()
    {
        var nonce = new byte[12];
        var tag = new byte[16];
        var ciphertext = new byte[256];
        RandomNumberGenerator.Fill(nonce);
        RandomNumberGenerator.Fill(tag);
        RandomNumberGenerator.Fill(ciphertext);

        _payload = new EncryptedPayload
        {
            Ciphertext = [.. ciphertext],
            KeyId = "msg-key-2024-v1",
            Algorithm = "AES-256-GCM",
            Nonce = [.. nonce],
            Tag = [.. tag],
            Version = 1
        };

        _formatted = EncryptedPayloadFormatter.Format(_payload);
    }

    [Benchmark(Baseline = true)]
    public string Format() => EncryptedPayloadFormatter.Format(_payload);

    [Benchmark]
    public EncryptedPayload? TryParse()
    {
        _ = EncryptedPayloadFormatter.TryParse(_formatted, out var result);
        return result;
    }

    [Benchmark]
    public bool IsEncrypted() => EncryptedPayloadFormatter.IsEncrypted(_formatted);

    [Benchmark]
    public EncryptedPayload? Roundtrip()
    {
        var formatted = EncryptedPayloadFormatter.Format(_payload);
        _ = EncryptedPayloadFormatter.TryParse(formatted, out var result);
        return result;
    }
}
