using System.Security.Cryptography;

namespace Encina.Audit.Marten.Benchmarks;

/// <summary>
/// Micro-benchmarks for <see cref="EncryptedField"/> AES-256-GCM encryption and decryption.
/// Measures the per-field overhead that audit recording adds to every command.
/// </summary>
/// <remarks>
/// <para>
/// Since audit is on the hot path of every command via <c>AuditPipelineBehavior</c>,
/// the encryption overhead is paid per audited field per request. Typical audit entries
/// have 5-6 PII fields, so multiply these numbers by ~6 for real-world per-entry cost.
/// </para>
/// <para>
/// Run:
/// <code>
/// dotnet run -c Release -- --filter "*EncryptedField*" --job short
/// </code>
/// </para>
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
public class EncryptedFieldBenchmarks
{
    private byte[] _key = null!;
    private const string KeyId = "temporal:2026-03:v1";

    private string _shortText = null!;   // ~16 chars (UserId)
    private string _mediumText = null!;   // ~256 chars (IpAddress + UserAgent)
    private string _longText = null!;     // ~4096 chars (RequestPayload)
    private string _veryLongText = null!; // ~65536 chars (max payload)

    private EncryptedField _encryptedShort = null!;
    private EncryptedField _encryptedMedium = null!;
    private EncryptedField _encryptedLong = null!;
    private EncryptedField _encryptedVeryLong = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _key = new byte[32];
        RandomNumberGenerator.Fill(_key);

        _shortText = "user-12345678";
        _mediumText = new string('A', 256);
        _longText = new string('B', 4096);
        _veryLongText = new string('C', 65536);

        // Pre-encrypt for decryption benchmarks
        _encryptedShort = EncryptedField.Encrypt(_shortText, _key, KeyId);
        _encryptedMedium = EncryptedField.Encrypt(_mediumText, _key, KeyId);
        _encryptedLong = EncryptedField.Encrypt(_longText, _key, KeyId);
        _encryptedVeryLong = EncryptedField.Encrypt(_veryLongText, _key, KeyId);
    }

    // ── Encryption benchmarks ────────────────────────────────────────────

    [BenchmarkCategory("DocRef:bench:audit-marten/encrypt-short-16b")]
    [Benchmark(Baseline = true)]
    public EncryptedField Encrypt_Short_16B()
        => EncryptedField.Encrypt(_shortText, _key, KeyId);

    [Benchmark]
    public EncryptedField Encrypt_Medium_256B()
        => EncryptedField.Encrypt(_mediumText, _key, KeyId);

    [BenchmarkCategory("DocRef:bench:audit-marten/encrypt-long-4kb")]
    [Benchmark]
    public EncryptedField Encrypt_Long_4KB()
        => EncryptedField.Encrypt(_longText, _key, KeyId);

    [Benchmark]
    public EncryptedField Encrypt_VeryLong_64KB()
        => EncryptedField.Encrypt(_veryLongText, _key, KeyId);

    // ── Decryption benchmarks ────────────────────────────────────────────

    [BenchmarkCategory("DocRef:bench:audit-marten/decrypt-short-16b")]
    [Benchmark]
    public string? Decrypt_Short_16B()
        => _encryptedShort.Decrypt(_key);

    [Benchmark]
    public string? Decrypt_Medium_256B()
        => _encryptedMedium.Decrypt(_key);

    [Benchmark]
    public string? Decrypt_Long_4KB()
        => _encryptedLong.Decrypt(_key);

    [BenchmarkCategory("DocRef:bench:audit-marten/decrypt-verylong-64kb")]
    [Benchmark]
    public string? Decrypt_VeryLong_64KB()
        => _encryptedVeryLong.Decrypt(_key);

    // ── Shredded path (null key = no decryption) ─────────────────────────

    [Benchmark]
    public string? DecryptOrPlaceholder_NullKey()
        => _encryptedShort.DecryptOrPlaceholder(null, "[SHREDDED]");
}
