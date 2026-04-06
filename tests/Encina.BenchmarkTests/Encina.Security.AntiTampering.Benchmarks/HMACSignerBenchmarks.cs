using System.Security.Cryptography;
using Encina.Security.AntiTampering;
using Encina.Security.AntiTampering.Abstractions;
using Encina.Security.AntiTampering.HMAC;
using Microsoft.Extensions.Options;

namespace Encina.Security.AntiTampering.Benchmarks;

/// <summary>
/// Benchmarks for <see cref="HMACSigner"/> signing and verification operations.
/// Measures throughput and memory allocation for different payload sizes and HMAC algorithms.
/// Tracing and metrics are disabled to measure pure cryptographic performance.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class HMACSignerBenchmarks
{
    private HMACSigner _signerSha256 = null!;
    private HMACSigner _signerSha384 = null!;
    private HMACSigner _signerSha512 = null!;
    private SigningContext _context = null!;
    private byte[] _smallPayload = null!;
    private byte[] _mediumPayload = null!;
    private byte[] _largePayload = null!;
    private string _preComputedSignatureSmall = null!;

    [GlobalSetup]
    public void Setup()
    {
        var options = new AntiTamperingOptions
        {
            EnableTracing = false,
            EnableMetrics = false
        };
        var keyProvider = new InMemoryKeyProvider(Options.Create(options));

        // Generate a 256-bit (32-byte) cryptographically strong key
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        keyProvider.AddKey("bench-key", key);

        var optionsSha256 = Options.Create(new AntiTamperingOptions
        {
            Algorithm = HMACAlgorithm.SHA256,
            EnableTracing = false,
            EnableMetrics = false
        });
        var optionsSha384 = Options.Create(new AntiTamperingOptions
        {
            Algorithm = HMACAlgorithm.SHA384,
            EnableTracing = false,
            EnableMetrics = false
        });
        var optionsSha512 = Options.Create(new AntiTamperingOptions
        {
            Algorithm = HMACAlgorithm.SHA512,
            EnableTracing = false,
            EnableMetrics = false
        });

        _signerSha256 = new HMACSigner(keyProvider, optionsSha256);
        _signerSha384 = new HMACSigner(keyProvider, optionsSha384);
        _signerSha512 = new HMACSigner(keyProvider, optionsSha512);

        _context = new SigningContext
        {
            KeyId = "bench-key",
            Nonce = Guid.NewGuid().ToString("N"),
            Timestamp = DateTimeOffset.UtcNow,
            HttpMethod = "POST",
            RequestPath = "/api/orders"
        };

        // Generate payload data: 64 bytes, 1 KB, 64 KB
        _smallPayload = new byte[64];
        _mediumPayload = new byte[1024];
        _largePayload = new byte[64 * 1024];
        RandomNumberGenerator.Fill(_smallPayload);
        RandomNumberGenerator.Fill(_mediumPayload);
        RandomNumberGenerator.Fill(_largePayload);

        // Pre-compute signature for verification benchmark
        _preComputedSignatureSmall = _signerSha256.SignAsync(_smallPayload, _context)
            .AsTask().GetAwaiter().GetResult()
            .Match(Right: s => s, Left: _ => string.Empty);
    }

    #region SignAsync Benchmarks

    [BenchmarkCategory("DocRef:bench:security/sign-sha256-small")]
    [Benchmark(Baseline = true)]
    public async Task<string> Sign_SHA256_SmallPayload()
    {
        var result = await _signerSha256.SignAsync(_smallPayload, _context);
        return result.Match(Right: s => s, Left: _ => string.Empty);
    }

    [Benchmark]
    public async Task<string> Sign_SHA256_MediumPayload()
    {
        var result = await _signerSha256.SignAsync(_mediumPayload, _context);
        return result.Match(Right: s => s, Left: _ => string.Empty);
    }

    [BenchmarkCategory("DocRef:bench:security/sign-sha256-large")]
    [Benchmark]
    public async Task<string> Sign_SHA256_LargePayload()
    {
        var result = await _signerSha256.SignAsync(_largePayload, _context);
        return result.Match(Right: s => s, Left: _ => string.Empty);
    }

    [Benchmark]
    public async Task<string> Sign_SHA384_SmallPayload()
    {
        var result = await _signerSha384.SignAsync(_smallPayload, _context);
        return result.Match(Right: s => s, Left: _ => string.Empty);
    }

    [Benchmark]
    public async Task<string> Sign_SHA512_SmallPayload()
    {
        var result = await _signerSha512.SignAsync(_smallPayload, _context);
        return result.Match(Right: s => s, Left: _ => string.Empty);
    }

    #endregion

    #region VerifyAsync Benchmarks

    [BenchmarkCategory("DocRef:bench:security/verify-sha256-small")]
    [Benchmark]
    public async Task<bool> Verify_SHA256_SmallPayload()
    {
        var result = await _signerSha256.VerifyAsync(_smallPayload, _preComputedSignatureSmall, _context);
        return result.Match(Right: v => v, Left: _ => false);
    }

    #endregion

    #region Sign+Verify Roundtrip

    [BenchmarkCategory("DocRef:bench:security/sign-verify-roundtrip")]
    [Benchmark]
    public async Task<bool> SignAndVerify_Roundtrip()
    {
        var signResult = await _signerSha256.SignAsync(_smallPayload, _context);
        var signature = signResult.Match(Right: s => s, Left: _ => string.Empty);

        var verifyResult = await _signerSha256.VerifyAsync(_smallPayload, signature, _context);
        return verifyResult.Match(Right: v => v, Left: _ => false);
    }

    #endregion
}
