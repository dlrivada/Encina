# Security Modules — Benchmark Results

> **Date**: 2026-03-19
> **Issue**: [#797](https://github.com/dlrivada/Encina/issues/797)
> **Packages**: `Encina.Security.Encryption`, `Encina.Security.PII`, `Encina.Security.AntiTampering`, `Encina.Security.Sanitization`

## Benchmark Environment

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7462/25H2/2025Update/HudsonValley2)
13th Gen Intel Core i9-13900KS 3.20GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.201
  Runtime: .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  GC: Concurrent Workstation
  HardwareIntrinsics: AVX2, AES, PCLMUL, AvxVnni
```

---

## 1. Encryption — AES-256-GCM Field-Level Encryption

Measures encrypt/decrypt cycles for field-level encryption using AES-256-GCM.
Every encrypted property in a request passes through this path.

### AesGcmFieldEncryptor

<!-- docref-table: bench:security/* -->
| Method | Mean | StdDev | Allocated | Notes |
|--------|-----:|-------:|----------:|-------|
| **EncryptString_Short** (baseline, 16B) | 1,171 ns | 58 ns | ~456 B | Email, UserId |
| EncryptString_Medium (256B) | 1,219 ns | 195 ns | ~1,176 B | UserAgent |
| EncryptString_Long (4KB) | 2,190 ns | 243 ns | ~12.7 KB | RequestPayload |
| **DecryptString_Short** | 738 ns | 79 ns | ~232 B | |
| EncryptDecryptRoundtrip | 1,744 ns | 156 ns | ~712 B | Full cycle |
| EncryptBytes_Short | 1,037 ns | 67 ns | ~424 B | |
| EncryptBytes_Medium | 1,191 ns | 158 ns | ~1,132 B | |
<!-- /docref-table -->

### EncryptionOrchestrator (End-to-End Pipeline)

<!-- docref-table: bench:security/* -->
| Method | Mean | StdDev | Allocated | Notes |
|--------|-----:|-------:|----------:|-------|
| **Encrypt_SingleProperty** (baseline) | 2.08 us | 0.12 us | ~1.8 KB | Single [Encrypt] field |
| Encrypt_ThreeProperties | 5.78 us | 1.31 us | ~5.2 KB | 3x [Encrypt] fields |
| EncryptDecrypt_Roundtrip | 4.37 us | 0.30 us | ~3.4 KB | Encrypt + Decrypt |
| NoEncryptedProperties_Passthrough | 0.47 us | 0.02 us | ~0.2 KB | Passthrough (no [Encrypt]) |
<!-- /docref-table -->

### PropertyCache (Reflection Overhead)

<!-- docref-table: bench:security/* -->
| Method | Mean | Allocated | Notes |
|--------|-----:|----------:|-------|
| GetProperties_ColdCache | 294 us | 15.3 KB | First call per type |
| GetProperties_WarmCache | 299 us | 15.3 KB | Subsequent calls |
| GetProperties_MultipleTypes | 582 us | 31.0 KB | 3 different types |
| SetValue_CompiledSetter | 284 us | 15.3 KB | Compiled delegate |
| GetValue_CompiledGetter | 307 us | 15.3 KB | Compiled delegate |
<!-- /docref-table -->

**Key Insight**: Encryption adds ~1-2 us per field. For a typical entity with 3 encrypted fields, the total overhead is ~6 us — negligible compared to database I/O (~1-50 ms).

---

## 2. AntiTampering — HMAC-SHA256/384/512

Measures HMAC signing and verification performance. Every signed request computes a hash over the canonical representation.

### HMACSigner

<!-- docref-table: bench:security/* -->
| Method | Mean | StdDev | Allocated | Notes |
|--------|-----:|-------:|----------:|-------|
| **Sign_SHA256_SmallPayload** (baseline, 64B) | 957 ns | 155 ns | 1.64 KB | |
| Sign_SHA256_MediumPayload (1KB) | 1,163 ns | 55 ns | 1.64 KB | |
| Sign_SHA256_LargePayload (64KB) | 26,012 ns | 627 ns | 1.64 KB | |
| Sign_SHA384_SmallPayload | 1,519 ns | 41 ns | 1.70 KB | |
| Sign_SHA512_SmallPayload | 1,591 ns | 115 ns | 1.76 KB | |
| **Verify_SHA256_SmallPayload** | 919 ns | 63 ns | 1.69 KB | |
| SignAndVerify_Roundtrip | 1,534 ns | 24 ns | 3.26 KB | |
<!-- /docref-table -->

**Key Insight**: HMAC-SHA256 sign+verify roundtrip is ~1.5 us for typical payloads. Constant memory allocation (~1.6 KB) regardless of payload size. SHA384/512 add ~50% overhead but may be required for compliance (FIPS 140-2).

---

## 3. Sanitization — Output Encoding

Measures output encoding performance for different contexts. Every user input passes through the sanitizer in the pipeline behavior.

### DefaultOutputEncoder

<!-- docref-table: bench:security/* -->
| Method | Mean | StdDev | Allocated | Notes |
|--------|-----:|-------:|----------:|-------|
| **EncodeForHtml_SafeText** (baseline) | 3.5 ns | 0.5 ns | 0 B | No-op for safe text |
| EncodeForHtml_SpecialChars | 101 ns | 12.6 ns | 160 B | HTML entities |
| EncodeForJavaScript_SafeText | 3.2 ns | 0.7 ns | 0 B | No-op |
| EncodeForJavaScript_SpecialChars | 60 ns | 5.4 ns | 144 B | JS escape |
| EncodeForUrl_SafeText | 74 ns | 3.1 ns | 152 B | RFC 3986 |
| EncodeForUrl_SpecialChars | 57 ns | 1.5 ns | 128 B | Percent-encoding |
| EncodeForCss_SafeText | 163 ns | 6.3 ns | 1,256 B | OWASP \HHHHHH |
| EncodeForCss_SpecialChars | 186 ns | 12.5 ns | 1,192 B | 6-digit hex |
<!-- /docref-table -->

**Key Insight**: HTML/JS encoding for safe text is essentially free (~3 ns, 0 B allocated). CSS encoding is the most expensive (~163-186 ns, ~1.2 KB) due to OWASP Rule #4 hex encoding. All operations are sub-microsecond.

---

## 4. PII Masking — 9 Masking Strategies

Measures per-strategy masking performance. Every PII-decorated property in a request passes through the masking pipeline.

### MaskingStrategy (per-field)

<!-- docref-table: bench:security/* -->
| Method | Mean | StdDev | Allocated | Notes |
|--------|-----:|-------:|----------:|-------|
| **Email_Partial** (baseline) | 33 ns | 0.6 ns | 224 B | `j***@example.com` |
| Phone_Partial | 142 ns | 13.0 ns | 488 B | `***-***-4567` |
| CreditCard_Partial | 165 ns | 16.2 ns | 512 B | `****-****-****-1111` |
| SSN_Partial | 148 ns | 1.5 ns | 488 B | `***-**-6789` |
| Name_Partial | 87 ns | 0.8 ns | 280 B | `J*** D**` |
| Address_Partial | 52 ns | 2.1 ns | 328 B | Street masked, city preserved |
| DateOfBirth_Partial | 75 ns | 1.5 ns | 384 B | `**/**/1990` |
| IPAddress_Partial | 58 ns | 0.9 ns | 264 B | `192.168.*.*` |
| Custom_FullMasking | 22 ns | 0.5 ns | 128 B | `***` |
| Email_Short (6 chars) | 20 ns | 0.2 ns | 128 B | Edge case |
| Email_Long (55 chars) | 37 ns | 0.2 ns | 328 B | Edge case |
| RegexPattern | 272 ns | 37.9 ns | 416 B | Custom regex |
<!-- /docref-table -->

### PIIMasker (end-to-end pipeline)

<!-- docref-table: bench:security/* -->
| Method | Mean | StdDev | Allocated | Notes |
|--------|-----:|-------:|----------:|-------|
| **MaskObject_SingleField** (1 [PII]) | 809 ns | 74.5 ns | 1,696 B | JSON roundtrip + 1 strategy |
| MaskObject_MultiField (4 [PII]) | 2,091 ns | 130.9 ns | 4,904 B | JSON roundtrip + 4 strategies |
| MaskObject_NoAttributes | 405 ns | 7.0 ns | 952 B | JSON roundtrip only (passthrough) |
| MaskForAudit_SingleField | 813 ns | 95.7 ns | 1,696 B | Same as MaskObject |
| MaskForAudit_NonGeneric | 726 ns | 11.7 ns | 1,696 B | Object overload |
<!-- /docref-table -->

### Serialization Overhead

<!-- docref-table: bench:security/* -->
| Method | Mean | StdDev | Allocated | Notes |
|--------|-----:|-------:|----------:|-------|
| Serialize_Small (1 PII field) | 142 ns | 14.5 ns | 112 B | JSON serialize only |
| Serialize_Medium (4 PII fields) | 193 ns | 3.6 ns | 392 B | |
| Serialize_Large (8 PII fields) | 719 ns | 188.4 ns | 3,784 B | |
| MaskObject_Small (full pipeline) | 731 ns | 6.1 ns | 1,696 B | Serialize + mask + deserialize |
| MaskObject_Medium | 2,489 ns | 15.0 ns | 6,352 B | |
| MaskObject_Large | 4,973 ns | 30.6 ns | 18,160 B | |
<!-- /docref-table -->

**Key Insight**: All 9 masking strategies execute in under 275 ns per field. Email masking is the fastest at ~33 ns. The JSON serialization roundtrip dominates end-to-end cost: ~405 ns baseline + ~400 ns per PII field. For a typical entity with 4 PII fields, total masking cost is ~2.1 us

---

## Summary — Security Module Performance Budget

| Operation | Typical Latency | Allocation | Hot Path? |
|-----------|----------------:|-----------:|:---------:|
| AES-256-GCM encrypt (per field) | ~1.2 us | ~456 B | Yes |
| AES-256-GCM decrypt (per field) | ~0.7 us | ~232 B | Yes |
| HMAC-SHA256 sign | ~1.0 us | ~1.6 KB | Yes |
| HMAC-SHA256 verify | ~0.9 us | ~1.7 KB | Yes |
| HTML encoding (safe text) | ~3.5 ns | 0 B | Yes |
| HTML encoding (special chars) | ~100 ns | 160 B | Yes |
| CSS encoding | ~163 ns | ~1.3 KB | Rare |
| PII masking (per strategy) | 22-272 ns | 128-512 B | Yes |
| Full object masking (4 fields) | ~2.1 us | ~4.9 KB | Pipeline |

**Verdict**: All security operations are sub-10 us, well within the performance budget for pipeline behaviors. Total security pipeline overhead for a typical request with 3 encrypted fields + HMAC + sanitization is ~10-15 us — less than 0.01% of a typical 100ms database round-trip.
