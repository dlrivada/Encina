using System.Security.Cryptography;
using Encina.Security.AntiTampering;
using Encina.Security.AntiTampering.Abstractions;
using Encina.Security.AntiTampering.HMAC;
using Microsoft.Extensions.Options;

namespace Encina.Security.AntiTampering.Benchmarks;

/// <summary>
/// Benchmarks for <see cref="HMACSigner"/> signing and verification operations.
/// Measures throughput and memory allocation for different payload sizes and algorithms.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class HMACSignerBenchmarks
{
    private HMACSigner _signerSha256 = null!;
    private HMACSigner _signerSha384 = null!;
    private HMACSigner _signerSha512 = null!;
    private SigningContext _context = null!;
    private byte[] _payload1KB = null!;
    private byte[] _payload10KB = null!;
    private byte[] _payload100KB = null!;
    private string _preComputedSignature1KB = null!;
    private string _preComputedSignature10KB = null!;
    private string _preComputedSignature100KB = null!;

    [GlobalSetup]
    public void Setup()
    {
        var keyProvider = new InMemoryKeyProvider(Options.Create(new AntiTamperingOptions()));

        // Generate a cryptographically strong key
        var key = new byte[64];
        RandomNumberGenerator.Fill(key);
        keyProvider.AddKey("bench-key", key);

        var optionsSha256 = Options.Create(new AntiTamperingOptions { Algorithm = HMACAlgorithm.SHA256 });
        var optionsSha384 = Options.Create(new AntiTamperingOptions { Algorithm = HMACAlgorithm.SHA384 });
        var optionsSha512 = Options.Create(new AntiTamperingOptions { Algorithm = HMACAlgorithm.SHA512 });

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

        // Generate payload data
        _payload1KB = new byte[1024];
        _payload10KB = new byte[10 * 1024];
        _payload100KB = new byte[100 * 1024];
        RandomNumberGenerator.Fill(_payload1KB);
        RandomNumberGenerator.Fill(_payload10KB);
        RandomNumberGenerator.Fill(_payload100KB);

        // Pre-compute signatures for verification benchmarks
        _preComputedSignature1KB = _signerSha256.SignAsync(_payload1KB, _context)
            .AsTask().GetAwaiter().GetResult()
            .Match(Right: s => s, Left: _ => string.Empty);

        _preComputedSignature10KB = _signerSha256.SignAsync(_payload10KB, _context)
            .AsTask().GetAwaiter().GetResult()
            .Match(Right: s => s, Left: _ => string.Empty);

        _preComputedSignature100KB = _signerSha256.SignAsync(_payload100KB, _context)
            .AsTask().GetAwaiter().GetResult()
            .Match(Right: s => s, Left: _ => string.Empty);
    }

    #region SignAsync Benchmarks

    [Benchmark(Baseline = true)]
    public async Task<string> Sign_SHA256_1KB()
    {
        var result = await _signerSha256.SignAsync(_payload1KB, _context);
        return result.Match(Right: s => s, Left: _ => string.Empty);
    }

    [Benchmark]
    public async Task<string> Sign_SHA256_10KB()
    {
        var result = await _signerSha256.SignAsync(_payload10KB, _context);
        return result.Match(Right: s => s, Left: _ => string.Empty);
    }

    [Benchmark]
    public async Task<string> Sign_SHA256_100KB()
    {
        var result = await _signerSha256.SignAsync(_payload100KB, _context);
        return result.Match(Right: s => s, Left: _ => string.Empty);
    }

    [Benchmark]
    public async Task<string> Sign_SHA384_1KB()
    {
        var result = await _signerSha384.SignAsync(_payload1KB, _context);
        return result.Match(Right: s => s, Left: _ => string.Empty);
    }

    [Benchmark]
    public async Task<string> Sign_SHA512_1KB()
    {
        var result = await _signerSha512.SignAsync(_payload1KB, _context);
        return result.Match(Right: s => s, Left: _ => string.Empty);
    }

    #endregion

    #region VerifyAsync Benchmarks

    [Benchmark]
    public async Task<bool> Verify_SHA256_1KB()
    {
        var result = await _signerSha256.VerifyAsync(_payload1KB, _preComputedSignature1KB, _context);
        return result.Match(Right: v => v, Left: _ => false);
    }

    [Benchmark]
    public async Task<bool> Verify_SHA256_10KB()
    {
        var result = await _signerSha256.VerifyAsync(_payload10KB, _preComputedSignature10KB, _context);
        return result.Match(Right: v => v, Left: _ => false);
    }

    [Benchmark]
    public async Task<bool> Verify_SHA256_100KB()
    {
        var result = await _signerSha256.VerifyAsync(_payload100KB, _preComputedSignature100KB, _context);
        return result.Match(Right: v => v, Left: _ => false);
    }

    #endregion

    #region Sign+Verify Roundtrip

    [Benchmark]
    public async Task<bool> SignAndVerify_Roundtrip_1KB()
    {
        var signResult = await _signerSha256.SignAsync(_payload1KB, _context);
        var signature = signResult.Match(Right: s => s, Left: _ => string.Empty);

        var verifyResult = await _signerSha256.VerifyAsync(_payload1KB, signature, _context);
        return verifyResult.Match(Right: v => v, Left: _ => false);
    }

    #endregion
}
