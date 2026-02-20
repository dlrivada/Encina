using System.Security.Cryptography;
using System.Text;
using Encina.Security.Encryption;
using Encina.Security.Encryption.Abstractions;
using Encina.Security.Encryption.Algorithms;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.Security.Encryption.Benchmarks;

/// <summary>
/// Benchmarks for AES-256-GCM field-level encryption operations.
/// Measures throughput and memory allocation for encrypt/decrypt cycles.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class AesGcmFieldEncryptorBenchmarks
{
    private AesGcmFieldEncryptor _encryptor = null!;
    private InMemoryKeyProvider _keyProvider = null!;
    private EncryptionContext _context = new() { Purpose = "Benchmark" };
    private EncryptedValue _preEncryptedValue;
    private string _shortText = null!;
    private string _mediumText = null!;
    private string _longText = null!;
    private byte[] _shortBytes = null!;
    private byte[] _mediumBytes = null!;

    [GlobalSetup]
    public void Setup()
    {
        _keyProvider = new InMemoryKeyProvider();
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        _keyProvider.AddKey("bench-key", key);
        _keyProvider.SetCurrentKey("bench-key");

        _encryptor = new AesGcmFieldEncryptor(_keyProvider);
        _context = new EncryptionContext { Purpose = "Benchmark" };

        // Prepare test data
        _shortText = "user@example.com";
        _mediumText = new string('x', 256);
        _longText = new string('x', 4096);
        _shortBytes = Encoding.UTF8.GetBytes(_shortText);
        _mediumBytes = Encoding.UTF8.GetBytes(_mediumText);

        // Pre-encrypt for decrypt benchmarks
        var result = _encryptor.EncryptStringAsync(_shortText, _context).AsTask().GetAwaiter().GetResult();
        _preEncryptedValue = result.Match(Right: v => v, Left: _ => default);
    }

    [Benchmark(Baseline = true)]
    public async Task<EncryptedValue> EncryptString_Short()
    {
        var result = await _encryptor.EncryptStringAsync(_shortText, _context);
        return result.Match(Right: v => v, Left: _ => default);
    }

    [Benchmark]
    public async Task<EncryptedValue> EncryptString_Medium()
    {
        var result = await _encryptor.EncryptStringAsync(_mediumText, _context);
        return result.Match(Right: v => v, Left: _ => default);
    }

    [Benchmark]
    public async Task<EncryptedValue> EncryptString_Long()
    {
        var result = await _encryptor.EncryptStringAsync(_longText, _context);
        return result.Match(Right: v => v, Left: _ => default);
    }

    [Benchmark]
    public async Task<string> DecryptString_Short()
    {
        var result = await _encryptor.DecryptStringAsync(_preEncryptedValue, _context);
        return result.Match(Right: v => v, Left: _ => string.Empty);
    }

    [Benchmark]
    public async Task<string> EncryptDecryptRoundtrip()
    {
        var encryptResult = await _encryptor.EncryptStringAsync(_shortText, _context);
        var encrypted = encryptResult.Match(Right: v => v, Left: _ => default);
        var decryptResult = await _encryptor.DecryptStringAsync(encrypted, _context);
        return decryptResult.Match(Right: v => v, Left: _ => string.Empty);
    }

    [Benchmark]
    public async Task<EncryptedValue> EncryptBytes_Short()
    {
        var result = await _encryptor.EncryptBytesAsync(_shortBytes, _context);
        return result.Match(Right: v => v, Left: _ => default);
    }

    [Benchmark]
    public async Task<EncryptedValue> EncryptBytes_Medium()
    {
        var result = await _encryptor.EncryptBytesAsync(_mediumBytes, _context);
        return result.Match(Right: v => v, Left: _ => default);
    }
}

/// <summary>
/// Benchmarks for <see cref="EncryptedPropertyCache"/> property discovery and compiled delegates.
/// Measures the overhead of reflection caching versus uncached access.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class PropertyCacheBenchmarks
{
#pragma warning disable CA1822 // Mark members as static - BenchmarkDotNet requires instance methods
    [IterationSetup]
    public void IterationSetup()
    {
        EncryptedPropertyCache.ClearCache();
    }
#pragma warning restore CA1822

    [Benchmark(Baseline = true)]
    public int GetProperties_ColdCache()
    {
        EncryptedPropertyCache.ClearCache();
        var props = EncryptedPropertyCache.GetProperties(typeof(BenchmarkEntity));
        return props.Length;
    }

    [Benchmark]
    public int GetProperties_WarmCache()
    {
        // Warm up
        EncryptedPropertyCache.GetProperties(typeof(BenchmarkEntity));
        // Measure warm hit
        var props = EncryptedPropertyCache.GetProperties(typeof(BenchmarkEntity));
        return props.Length;
    }

    [Benchmark]
    public int GetProperties_MultipleTypes()
    {
        var count = 0;
        count += EncryptedPropertyCache.GetProperties(typeof(BenchmarkEntity)).Length;
        count += EncryptedPropertyCache.GetProperties(typeof(BenchmarkEntity2)).Length;
        count += EncryptedPropertyCache.GetProperties(typeof(BenchmarkEntity3)).Length;
        return count;
    }

    [Benchmark]
    public string SetValue_CompiledSetter()
    {
        var props = EncryptedPropertyCache.GetProperties(typeof(BenchmarkEntity));
        var entity = new BenchmarkEntity();
        props[0].SetValue(entity, "encrypted-value");
        return entity.Email;
    }

    [Benchmark]
    public object? GetValue_CompiledGetter()
    {
        var props = EncryptedPropertyCache.GetProperties(typeof(BenchmarkEntity));
        var entity = new BenchmarkEntity { Email = "test@example.com" };
        return props[0].GetValue(entity);
    }

    private sealed class BenchmarkEntity
    {
        [Encrypt(Purpose = "Email")]
        public string Email { get; set; } = string.Empty;

        [Encrypt(Purpose = "SSN")]
        public string SSN { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }

    private sealed class BenchmarkEntity2
    {
        [Encrypt(Purpose = "Phone")]
        public string Phone { get; set; } = string.Empty;
    }

    private sealed class BenchmarkEntity3
    {
        [Encrypt(Purpose = "Address")]
        public string Address { get; set; } = string.Empty;

        [Encrypt(Purpose = "City")]
        public string City { get; set; } = string.Empty;
    }
}

/// <summary>
/// Benchmarks for the full encryption pipeline via <see cref="EncryptionOrchestrator"/>.
/// Measures end-to-end latency including property discovery, key retrieval, and cryptographic operations.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class OrchestratorBenchmarks : IDisposable
{
    private EncryptionOrchestrator _orchestrator = null!;
    private InMemoryKeyProvider _keyProvider = null!;
    private IRequestContext _context = null!;

    [GlobalSetup]
    public void Setup()
    {
        _keyProvider = new InMemoryKeyProvider();
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        _keyProvider.AddKey("bench-key", key);
        _keyProvider.SetCurrentKey("bench-key");

        var encryptor = new AesGcmFieldEncryptor(_keyProvider);
        _orchestrator = new EncryptionOrchestrator(encryptor, NullLogger<EncryptionOrchestrator>.Instance);
        _context = RequestContext.CreateForTest(tenantId: "bench-tenant");

        EncryptedPropertyCache.ClearCache();
    }

    public void Dispose()
    {
        _keyProvider?.Clear();
        EncryptedPropertyCache.ClearCache();
    }

    [Benchmark(Baseline = true)]
    public async Task<bool> Encrypt_SingleProperty()
    {
        var entity = new SingleFieldEntity { Email = "user@example.com" };
        var result = await _orchestrator.EncryptAsync(entity, _context);
        return result.IsRight;
    }

    [Benchmark]
    public async Task<bool> Encrypt_ThreeProperties()
    {
        var entity = new MultiFieldEntity
        {
            Email = "user@example.com",
            Phone = "+1234567890",
            SSN = "123-45-6789",
            Name = "John"
        };
        var result = await _orchestrator.EncryptAsync(entity, _context);
        return result.IsRight;
    }

    [Benchmark]
    public async Task<bool> EncryptDecrypt_Roundtrip()
    {
        var entity = new SingleFieldEntity { Email = "user@example.com" };
        var enc = await _orchestrator.EncryptAsync(entity, _context);
        if (enc.IsLeft) return false;
        var dec = await _orchestrator.DecryptAsync(entity, _context);
        return dec.IsRight;
    }

    [Benchmark]
    public async Task<bool> NoEncryptedProperties_Passthrough()
    {
        var entity = new PlainEntity { Name = "John", Age = 30 };
        var result = await _orchestrator.EncryptAsync(entity, _context);
        return result.IsRight;
    }

    private sealed class SingleFieldEntity
    {
        [Encrypt(Purpose = "Email")]
        public string Email { get; set; } = string.Empty;
    }

    private sealed class MultiFieldEntity
    {
        [Encrypt(Purpose = "Email")]
        public string Email { get; set; } = string.Empty;

        [Encrypt(Purpose = "Phone")]
        public string Phone { get; set; } = string.Empty;

        [Encrypt(Purpose = "SSN")]
        public string SSN { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }

    private sealed class PlainEntity
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }
}
