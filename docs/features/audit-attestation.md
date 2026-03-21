# Audit Attestation

## Overview

The `Encina.Compliance.Attestation` module provides tamper-evident audit attestation — cryptographic proof that an audit record existed at a specific point in time, and that it has not been modified since. This addresses compliance requirements across multiple regulations:

- **EU AI Act (Art. 13)**: Transparency and traceability of high-risk AI system decisions
- **GDPR (Art. 5.2)**: Accountability principle — demonstrating compliance with data protection rules
- **NIS2 (Art. 23)**: Incident reporting evidence with verifiable timestamps

## Architecture

```
Application Code
    │
    ▼
[AttestDecision] attribute (declarative)
    │
    ▼
IAuditAttestationProvider
    ├── InMemoryAttestationProvider  (testing)
    ├── HashChainAttestationProvider (self-hosted production)
    └── HttpAttestationProvider      (external: Sigstore/Rekor, custom)
```

All providers implement the same `IAuditAttestationProvider` interface, returning `Either<EncinaError, T>` for Railway Oriented error handling.

## Provider Selection Guide

| Scenario | Provider | Rationale |
|----------|----------|-----------|
| Unit/integration tests | `InMemory` | Fast, no external dependencies |
| Self-hosted compliance | `HashChain` | Zero cost, tamper-evident, no network calls |
| Regulatory audit trail | `Http` (Sigstore/Rekor) | Third-party verifiable, immutable public ledger |
| Enterprise compliance | `Http` (custom API) | Integrate with existing GRC tooling |

## Hash Chain Mechanics

The `HashChainAttestationProvider` implements a cryptographic hash chain:

1. **Genesis**: First entry uses `"genesis"` as the previous signature
2. **Chain link**: `signature = SHA256(contentHash + ":" + previousSignature + ":" + chainIndex)`
3. **Verification**: Recompute the signature and compare — any mismatch indicates tampering
4. **Full chain audit**: `VerifyChainIntegrity()` walks the entire chain from genesis

Properties:
- **Append-only**: New entries can only be added at the end
- **No gaps**: Each entry references its predecessor
- **Tamper-evident**: Modifying any entry breaks the chain from that point forward
- **Idempotent**: Attesting the same `RecordId` twice returns the original receipt

## Configuration

### InMemory (Testing)

```csharp
services.AddEncinaAttestation(options => options.UseInMemory());
```

### HashChain (Production)

```csharp
services.AddEncinaAttestation(options =>
{
    options.UseHashChain(hc =>
    {
        hc.StoragePath = "/var/data/attestation";
        hc.HashAlgorithm = HashAlgorithmName.SHA256;
    });
    options.AddHealthCheck = true;
});
```

### HTTP (External)

```csharp
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

## Observability

The module instruments all operations with OpenTelemetry:

- **Activities**: `Attestation.Attest` and `Attestation.Verify` with semantic tags
- **Metrics**: 5 instruments tracking attestation volume, success/failure rates, and latency
- **Logging**: 6 structured events in the 9600-9605 EventId range using `[LoggerMessage]` source generator

## Testing

Property-based tests (FsCheck) verify the core hash chain invariants:
- Append-only: N records produce N receipts with sequential chain indices
- No gaps: Each receipt's `previous_signature` matches its predecessor
- Tamper detection: Modified content hashes fail verification
- Idempotency: Same RecordId returns identical receipts
- Determinism: Same content always produces the same hash
