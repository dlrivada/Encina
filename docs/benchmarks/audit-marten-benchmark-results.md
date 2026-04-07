# Encina.Audit.Marten — Benchmark Results

> **Date**: 2026-03-19
> **Issue**: [#800](https://github.com/dlrivada/Encina/issues/800)
> **Package**: `Encina.Audit.Marten`

## Benchmark Environment

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7462/25H2)
13th Gen Intel Core i9-13900KS 3.20GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.201
  Runtime: .NET 10.0.5, X64 RyuJIT x86-64-v3
  GC: Concurrent Workstation
  HardwareIntrinsics: AVX2, AES, PCLMUL, AvxVnni
```

## 1. EncryptedField — AES-256-GCM Per-Field Cost

Measures the per-field encryption/decryption cost. Typical audit entries have 5-6 PII fields.

<!-- docref-table: bench:audit-marten/* -->
| Method | Mean | Allocated | Notes |
|--------|-----:|----------:|-------|
| **Encrypt_Short_16B** (baseline) | 899 ns | 1,504 B | UserId, IpAddress |
| Encrypt_Medium_256B | 1,099 ns | 7,576 B | UserAgent |
| Encrypt_Long_4KB | 3,027 ns | 37,120 B | RequestPayload |
| Encrypt_VeryLong_64KB | 64,523 ns | 569,677 B | Max payload size |
| **Decrypt_Short_16B** | 879 ns | 320 B | |
| Decrypt_Medium_256B | 1,077 ns | 1,288 B | |
| Decrypt_Long_4KB | 2,853 ns | 16,648 B | |
| Decrypt_VeryLong_64KB | 54,210 ns | 262,459 B | |
| DecryptOrPlaceholder_NullKey | **0.37 ns** | 0 B | Shredded path (zero cost) |
<!-- /docref-table -->

**Key findings**:
- Short field (UserId/IpAddress): **~900 ns** per encrypt/decrypt
- Medium field (UserAgent): **~1.1 us** per encrypt/decrypt
- Shredded path (null key): **~0.4 ns** — effectively free (no crypto operations)
- AES-NI hardware acceleration active on this CPU

## 2. TemporalKeyProvider — Key Lookup Overhead

Measures the per-entry key management cost (called once per `RecordAsync`).

<!-- docref-table: bench:audit-marten/* -->
| Method | PeriodCount | Mean | Allocated |
|--------|:-----------:|-----:|----------:|
| **GetExistingKey** (baseline) | 12 | 60 ns | 288 B |
| GetOrCreateExistingKey | 12 | 65 ns | 264 B |
| CreateNewKey | 12 | 1,747 ns | 784 B |
| IsKeyDestroyed | 12 | 38 ns | 112 B |
| GetActiveKeysCount | 12 | 589 ns | 2,336 B |
| **GetExistingKey** | 84 | 57 ns | 288 B |
| GetOrCreateExistingKey | 84 | 63 ns | 264 B |
| CreateNewKey | 84 | 1,782 ns | 784 B |
| IsKeyDestroyed | 84 | 37 ns | 112 B |
| GetActiveKeysCount | 84 | 4,631 ns | 15,144 B |
<!-- /docref-table -->

**Key findings**:
- Hot path (`GetExistingKey`): **~60 ns** — negligible
- Key count doesn't affect lookup: 12 vs 84 periods same latency (O(1) ConcurrentDictionary)
- New key creation: **~1.7 us** — only happens once per period (monthly/quarterly/yearly)

## 3. AuditEventEncryptor — End-to-End Pipeline Cost

Measures the complete flow: key lookup + encrypt N PII fields + produce encrypted event.

<!-- docref-table: bench:audit-marten/* -->
| Method | Mean | Allocated | Notes |
|--------|-----:|----------:|-------|
| **EncryptAuditEntry_Minimal_NoPii** (baseline) | 190 ns | 672 B | No PII fields — only key lookup overhead |
| EncryptAuditEntry_Full_AllPii | 8,555 ns | 41,440 B | 6 PII fields + 2KB payload |
| EncryptReadAuditEntry | 3,032 ns | 5,400 B | 3 PII fields |
<!-- /docref-table -->

**Key findings**:
- **Minimal entry (no PII)**: ~190 ns overhead — practically zero
- **Full entry (6 PII fields + payload)**: **~8.6 us** — the realistic hot-path cost
- **Read audit (3 PII fields)**: ~3 us

## 4. Real-World Impact Assessment

For a typical command audit entry with all PII fields populated:

| Component | Overhead |
|-----------|----------|
| Key lookup | ~60 ns |
| Encrypt 6 PII fields | ~8.5 us |
| **Total encryption overhead** | **~8.6 us** |
| Typical PostgreSQL write (SaveChangesAsync) | 1-5 ms |
| **Encryption as % of total** | **< 0.9%** |

The temporal encryption overhead is **< 1% of the total audit recording cost** when Marten/PostgreSQL I/O dominates.
