using Encina.Security.Audit;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.Audit.Marten.Benchmarks;

/// <summary>
/// End-to-end benchmarks for <see cref="AuditEventEncryptor"/> — the full mapping +
/// encryption pipeline that runs on every audited command.
/// </summary>
/// <remarks>
/// <para>
/// This is the most representative benchmark for real-world impact because it measures
/// the complete flow: AuditEntry → temporal key lookup → encrypt 5-6 PII fields →
/// produce AuditEntryRecordedEvent.
/// </para>
/// <para>
/// Run:
/// <code>
/// dotnet run -c Release -- --filter "*AuditEventEncryptor*" --job short
/// </code>
/// </para>
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
public class AuditEventEncryptorBenchmarks
{
    private AuditEventEncryptor _encryptor = null!;
    private InMemoryTemporalKeyProvider _keyProvider = null!;
    private AuditEntry _minimalEntry = null!;
    private AuditEntry _fullEntry = null!;
    private ReadAuditEntry _readEntry = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _keyProvider = new InMemoryTemporalKeyProvider(
            TimeProvider.System,
            NullLogger<InMemoryTemporalKeyProvider>.Instance);

        var options = Options.Create(new MartenAuditOptions
        {
            TemporalGranularity = TemporalKeyGranularity.Monthly
        });

        _encryptor = new AuditEventEncryptor(
            _keyProvider,
            options,
            NullLogger<AuditEventEncryptor>.Instance);

        var now = DateTimeOffset.UtcNow;

        // Minimal entry: only required fields, no PII
        _minimalEntry = new AuditEntry
        {
            Id = Guid.NewGuid(),
            CorrelationId = "bench-corr-1",
            Action = "Create",
            EntityType = "Order",
            Outcome = AuditOutcome.Success,
            TimestampUtc = now.UtcDateTime,
            StartedAtUtc = now.AddMilliseconds(-10),
            CompletedAtUtc = now,
        };

        // Full entry: all PII fields populated (worst case for encryption)
        _fullEntry = new AuditEntry
        {
            Id = Guid.NewGuid(),
            CorrelationId = "bench-corr-2",
            UserId = "user-12345678-abcd-efgh",
            TenantId = "tenant-xyz",
            Action = "Update",
            EntityType = "Patient",
            EntityId = "PAT-999",
            Outcome = AuditOutcome.Success,
            TimestampUtc = now.UtcDateTime,
            StartedAtUtc = now.AddMilliseconds(-50),
            CompletedAtUtc = now,
            IpAddress = "192.168.1.100",
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
            RequestPayloadHash = "sha256:a1b2c3d4e5f6...",
            RequestPayload = new string('R', 2048),   // 2KB request payload
            ResponsePayload = new string('S', 1024),   // 1KB response payload
            Metadata = new Dictionary<string, object?>
            {
                ["workflow"] = "approval",
                ["step"] = 3,
                ["department"] = "finance",
                ["approver"] = "manager@example.com"
            }
        };

        _readEntry = new ReadAuditEntry
        {
            Id = Guid.NewGuid(),
            EntityType = "Patient",
            EntityId = "PAT-123",
            UserId = "user-456",
            TenantId = "tenant-xyz",
            AccessedAtUtc = now,
            CorrelationId = "bench-corr-3",
            Purpose = "Patient care review under GDPR Art. 15",
            AccessMethod = ReadAccessMethod.Repository,
            EntityCount = 1,
            Metadata = new Dictionary<string, object?>
            {
                ["source"] = "EHR",
                ["ward"] = "cardiology"
            }
        };
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _keyProvider.Clear();
    }

    /// <summary>
    /// Best case: minimal entry with no PII fields (only structural data).
    /// No encryption needed — only temporal key lookup.
    /// </summary>
    [BenchmarkCategory("DocRef:bench:audit-marten/encryptor-minimal-nopii")]
    [Benchmark(Baseline = true)]
    public async Task<object?> EncryptAuditEntry_Minimal_NoPii()
    {
        var result = await _encryptor.EncryptAuditEntryAsync(_minimalEntry);
        object? evt = null;
        result.IfRight(e => evt = e);
        return evt;
    }

    /// <summary>
    /// Worst case: full entry with all PII fields populated.
    /// Encrypts 6 fields (UserId, IpAddress, UserAgent, RequestPayload, ResponsePayload, Metadata).
    /// </summary>
    [BenchmarkCategory("DocRef:bench:audit-marten/encryptor-full-allpii")]
    [Benchmark]
    public async Task<object?> EncryptAuditEntry_Full_AllPii()
    {
        var result = await _encryptor.EncryptAuditEntryAsync(_fullEntry);
        object? evt = null;
        result.IfRight(e => evt = e);
        return evt;
    }

    /// <summary>
    /// Read audit entry encryption (UserId, Purpose, Metadata — 3 fields).
    /// </summary>
    [Benchmark]
    public async Task<object?> EncryptReadAuditEntry()
    {
        var result = await _encryptor.EncryptReadAuditEntryAsync(_readEntry);
        object? evt = null;
        result.IfRight(e => evt = e);
        return evt;
    }
}
