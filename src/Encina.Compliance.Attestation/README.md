# Encina.Compliance.Attestation

[![NuGet](https://img.shields.io/nuget/v/Encina.Compliance.Attestation.svg)](https://www.nuget.org/packages/Encina.Compliance.Attestation/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](../../LICENSE)

Provider-agnostic tamper-evident audit attestation for Encina. Creates cryptographic proof that an audit record existed at a specific point in time, and detects retroactive modifications. Supports EU AI Act (Art. 13), GDPR accountability (Art. 5.2), and NIS2 incident reporting evidence requirements.

## Features

- **`IAuditAttestationProvider`** -- Single interface for all attestation backends: `AttestAsync` and `VerifyAsync` returning `Either<EncinaError, T>`
- **InMemory Provider** -- Thread-safe `ConcurrentDictionary`-based provider for testing and development, with idempotent attestation
- **HashChain Provider** -- Self-hosted, zero-dependency append-only chain. Each entry's HMAC-SHA256 signature incorporates the previous entry's signature, detecting retroactive tampering. Ephemeral or persistent HMAC key supported
- **Http Provider** -- Delegates to external endpoints (Sigstore/Rekor, custom APIs) with configurable authentication and payload mapping
- **`[AttestDecision]` Attribute** -- Declarative marker for commands requiring attestation of their outcome
- **Immutable Domain Model** -- `AuditRecord`, `AttestationReceipt`, `AttestationVerification` as sealed records with `required` properties
- **Content Hashing** -- Deterministic hashing via `AttestationHasher` (SHA-256, SHA-384, SHA-512) for audit record fingerprinting
- **Full Observability** -- OpenTelemetry `ActivitySource` with 2 activity types, `Meter` with 5 instruments (4 counters, 1 histogram), 9 structured log events (EventId 9600-9608)
- **Health Check** -- Opt-in `AttestationHealthCheck` verifying the registered provider is resolvable
- **Railway Oriented Programming** -- All operations return `Either<EncinaError, T>`, no exceptions for business logic
- **.NET 10 Compatible** -- Built with C# 14, nullable reference types enabled

## Installation

```bash
dotnet add package Encina.Compliance.Attestation
```

## Quick Start

### 1. Register Services

```csharp
// In-memory (testing/development)
services.AddEncinaAttestation(options =>
{
    options.UseInMemory();
    options.AddHealthCheck = true;
});

// Hash chain (self-hosted production)
services.AddEncinaAttestation(options =>
{
    options.UseHashChain(hc =>
    {
        hc.StoragePath = "/var/data/attestation-chain";
        hc.HashAlgorithm = HashAlgorithmName.SHA256;
    });
});

// HTTP (external endpoint)
services.AddEncinaAttestation(options =>
{
    options.UseHttp(http =>
    {
        http.AttestEndpointUrl = new Uri("https://rekor.sigstore.dev/api/v1/log/entries");
        http.VerifyEndpointUrl = new Uri("https://rekor.sigstore.dev/api/v1/log/entries/retrieve");
        http.AuthHeader = "Bearer <token>";
    });
});
```

### 2. Mark Operations for Attestation

```csharp
[AttestDecision(Reason = "EU AI Act Art. 13 transparency", RecordType = "ModelDecision")]
public record ClassifyRiskLevel(Guid ModelId, string InputData) : IRequest<Either<EncinaError, RiskClassification>>;
```

### 3. Attest and Verify

```csharp
public class AuditService(IAuditAttestationProvider provider)
{
    public async Task<AttestationReceipt> AttestDecisionAsync(Guid decisionId, string serializedContent)
    {
        var record = new AuditRecord
        {
            RecordId = decisionId,
            RecordType = "ModelDecision",
            OccurredAtUtc = DateTimeOffset.UtcNow,
            SerializedContent = serializedContent
        };

        var result = await provider.AttestAsync(record);

        return result.Match(
            Right: receipt => receipt,
            Left: error => throw new InvalidOperationException(error.Message));
    }

    public async Task<bool> VerifyAsync(AttestationReceipt receipt)
    {
        var result = await provider.VerifyAsync(receipt);

        return result.Match(
            Right: v => v.IsValid,
            Left: _ => false); // Left means the answer is unknown (see HTTP Provider Details); treated as not verified here
    }
}
```

## Providers

| Provider | Use Case | External Dependencies | Thread-Safe |
|----------|----------|----------------------|-------------|
| `InMemoryAttestationProvider` | Testing, development | None | Yes (`ConcurrentDictionary`) |
| `HashChainAttestationProvider` | Self-hosted production | None | Yes (`Lock` + chain lock) |
| `HttpAttestationProvider` | Cloud attestation services | HTTP endpoint | Yes (`HttpClient`) |

### HashChain Provider Details

The hash chain creates a tamper-evident, append-only sequence using HMAC:

```text
Entry[0]: signature = HMAC(key, contentHash + ":genesis:" + 0)
Entry[1]: signature = HMAC(key, contentHash + ":" + Entry[0].signature + ":" + 1)
Entry[N]: signature = HMAC(key, contentHash + ":" + Entry[N-1].signature + ":" + N)
```

Modifying any entry breaks the chain from that point forward. Call `VerifyChainIntegrity()` to validate the full chain.

**Key management:**

| Scenario | Configuration |
|----------|--------------|
| Development / testing | Use `InMemoryAttestationProvider` — no key needed |
| Single-process production | Omit `HmacKey` — ephemeral key (warning logged at startup); chain is in-memory only |
| Multi-process / persistent | Provide stable `HmacKey` via secrets manager; note: chain data is still in-memory — use `HttpAttestationProvider` for cross-restart verification |
| Regulatory / external audit | Use `HttpAttestationProvider` with external transparency log |

### HTTP Provider Details

`HttpAttestationProvider` separates a verdict the endpoint gave from a response it could not use.

| Operation | Situation | Result |
|-----------|-----------|--------|
| `AttestAsync` | 2xx with a valid JSON object body | `Right(AttestationReceipt)` |
| `AttestAsync` | 2xx that is malformed: empty body, non-JSON, undecodable or unknown charset, JSON root not an object, `attestation_id` or `signature` absent, null or of the wrong type, or `attestation_id` not a valid GUID | `Left` with `attestation.provider_unavailable` |
| `AttestAsync` | Non-2xx | `Left` with `attestation.http_endpoint_error` |
| `VerifyAsync` | 2xx with `"is_valid": true` | `Right` with `IsValid = true` |
| `VerifyAsync` | Non-2xx, 2xx without `is_valid`, explicit `"is_valid": false`, or no `VerifyEndpointUrl` | `Right` with `IsValid = false` |
| `VerifyAsync` | 2xx whose verdict cannot be read: empty, non-JSON or non-object body, or `is_valid` not a JSON boolean (`null`, `"true"` and `1` all qualify) | `Left` with `attestation.provider_unavailable` |
| `VerifyAsync` | 2xx whose Content-Length exceeds 1 MB | `Left` with `attestation.http_response_too_large` |

On a negative verdict, a `failure_reason` of the wrong type is ignored and the default reason `Remote verification failed.` is used. The error text for an unusable response names only the exception type, never the exception message or the payload.

## Configuration Options

| Class | Property | Type | Description |
|-------|----------|------|-------------|
| `AttestationOptions` | `AddHealthCheck` | `bool` | Register health check (default: `false`) |
| `HashChainOptions` | `StoragePath` | `string?` | Persistence path (null = in-memory only) |
| `HashChainOptions` | `HashAlgorithm` | `HashAlgorithmName` | Hash algorithm (default: SHA-256). Only SHA-256, SHA-384 and SHA-512 are supported; any other value is rejected. With `AddEncinaAttestation`, `OptionsValidationException` is thrown at host start (`ValidateOnStart`) or on the first resolution of `IOptions<HashChainOptions>.Value`. A provider built by hand (`new HashChainAttestationProvider(...)`) throws `ArgumentException` at construction, also when `HmacKey` is supplied |
| `HashChainOptions` | `HmacKey` | `byte[]?` | HMAC signing key (null = ephemeral, warning logged). Excluded from JSON serialization; `ToString()` prints `HmacKey = [REDACTED]`, never its content or length |
| `HttpAttestationOptions` | `AttestEndpointUrl` | `Uri` | POST endpoint for attestation (required) |
| `HttpAttestationOptions` | `VerifyEndpointUrl` | `Uri?` | POST endpoint for verification (optional; when null, `VerifyAsync` returns a negative verdict) |
| `HttpAttestationOptions` | `AuthHeader` | `string?` | Authorization header value |

## Error Codes

| Code | Description |
|------|-------------|
| `attestation.verification_failed` | Receipt failed verification |
| `attestation.duplicate_record` | Duplicate RecordId (idempotent return) |
| `attestation.provider_unavailable` | Provider unreachable or its response unusable |
| `attestation.content_hash_mismatch` | Content hash does not match |
| `attestation.chain_integrity_broken` | Hash chain link broken |
| `attestation.http_endpoint_error` | HTTP endpoint returned error |
| `attestation.http_response_too_large` | HTTP response body exceeds the 1 MB cap |

## Observability

### Tracing

ActivitySource: `Encina.Compliance.Attestation`

| Activity | Tags |
|----------|------|
| `Attestation.Attest` | `attestation.provider`, `attestation.record_type`, `attestation.outcome` |
| `Attestation.Verify` | `attestation.provider`, `attestation.outcome` |

### Metrics

Meter: `Encina.Compliance.Attestation`

| Instrument | Type | Description |
|------------|------|-------------|
| `attestation.attest.total` | Counter | Total attestation operations |
| `attestation.attest.succeeded` | Counter | Successful attestations |
| `attestation.attest.failed` | Counter | Failed attestations |
| `attestation.verify.total` | Counter | Total verifications |
| `attestation.attest.duration` | Histogram (ms) | Attestation latency |

### Structured Logging

EventId range: 9600-9608 (registered in `EventIdRanges.ComplianceAttestation`)

| EventId | Level | Message |
|---------|-------|---------|
| 9600 | Information | Attestation created |
| 9601 | Information | Verification completed |
| 9602 | Debug | Idempotent attestation returned |
| 9603 | Error | Hash chain integrity broken |
| 9604 | Error | HTTP endpoint error |
| 9605 | Debug | Health check completed |
| 9606 | Warning | Attestation enforcement blocked pipeline |
| 9607 | Warning | Attestation failed in LogOnly mode |
| 9608 | Warning | Ephemeral HMAC key in use |

## Related Packages

- [`Encina`](../Encina/) -- Core CQRS pipeline, `EncinaError`, Railway Oriented Programming
- [`Encina.Compliance.GDPR`](../Encina.Compliance.GDPR/) -- GDPR compliance (Art. 5.2 accountability)
- [`Encina.Compliance.AIAct`](../Encina.Compliance.AIAct/) -- EU AI Act compliance (Art. 13 transparency)
- [`Encina.Compliance.NIS2`](../Encina.Compliance.NIS2/) -- NIS2 directive compliance (incident evidence)

## License

[MIT](../../LICENSE)
