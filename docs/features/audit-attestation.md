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
2. **Chain link**: `signature = HMAC-SHA256(key, contentHash + ":" + previousSignature + ":" + chainIndex)`
3. **Verification**: Recompute the HMAC and compare using constant-time comparison — any mismatch indicates tampering
4. **Full chain audit**: `VerifyChainIntegrity()` walks the entire chain from genesis

Properties:
- **Append-only**: New entries can only be added at the end
- **No gaps**: Each entry references its predecessor
- **Tamper-evident**: Modifying any entry breaks the chain from that point forward
- **Idempotent**: Attesting the same `RecordId` twice returns the original receipt
- **HMAC-signed**: Signatures require knowledge of the secret key, preventing forgery by database-level attackers

### HMAC Key Management

| Scenario | Configuration | Trade-off |
|----------|--------------|-----------|
| Development / testing | Use `InMemoryAttestationProvider` | No key management needed |
| Single-process production | Omit `HmacKey` (ephemeral) | Chain integrity within process lifetime only; a warning is logged at startup |
| Multi-process / persistent | Provide stable `HmacKey` via secrets manager (Azure Key Vault, AWS Secrets Manager) | Full cross-restart verification |
| Regulatory / external audit | Use `HttpAttestationProvider` with transparency log | External trust anchor independent of key management |

**Important**: When using an ephemeral key (default), all previously attested receipts become unverifiable after a process restart. The provider logs a `Warning` (EventId 9608) at startup to make this visible to operators. For compliance-critical deployments, always provide a persistent key.

**Key rotation**: The current implementation does not support key rotation within a chain. Rotating the key starts a new chain. If key rotation is required, use the HTTP provider with an external service that manages key lifecycle independently.

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

## HTTP Attestation Provider — REST Contract

The `HttpAttestationProvider` works with any service implementing the following REST contract. This enables interoperability with third-party attestation backends.

**Attest (create receipt):**

```
POST /attest
Content-Type: application/json
Authorization: Bearer <token>

{
    "recordId": "guid",
    "recordType": "string",
    "occurredAtUtc": "datetime",
    "contentHash": "string"
}

→ 200 OK
{
    "attestationId": "guid",
    "auditRecordId": "guid",
    "signature": "string",
    "attestedAtUtc": "datetime",
    "proofMetadata": { ... }
}
```

**Verify (retrieve receipt):**

```
GET /receipt/{attestationId}
Authorization: Bearer <token>

→ 200 OK
{
    "attestationId": "guid",
    "auditRecordId": "guid",
    "contentHash": "string",
    "signature": "string",
    "attestedAtUtc": "datetime",
    "proofMetadata": { ... }
}
```

### Security Recommendations

The following are recommended practices when implementing an HTTP-based attestation provider:

- Use HTTPS by default; require an explicit opt-in flag (e.g., `AllowInsecureHttp`) for HTTP endpoints in local or development scenarios
- Avoid logging authorization headers or secrets; sensitive fields such as `AuthHeader` should be excluded from serialization (e.g., via `[JsonIgnore]`) and redacted from `ToString()` output
- Truncate error response bodies in user-facing logs (e.g., to 500 characters); emit full bodies only at Debug level
- Apply reasonable response size limits (e.g., via `MaxResponseContentBufferSize`) to reduce the risk of memory exhaustion
- Apply SSRF safeguards in configuration and validation (e.g., validating base URLs against an allowlist)

## Compatible Third-Party Attestation Services

The `HttpAttestationProvider` works with any service implementing the expected
REST contract (`POST /attest` returning a receipt, `GET /receipt/{attestationId}` returning
a stored receipt). This includes but is not limited to:

| Service | Backend | Notes |
|---------|---------|-------|
| [Trust Layer](https://trust.arkforge.tech) | Rekor transparency log | Publicly auditable, Ed25519 signatures |

> Encina does not endorse or guarantee any third-party service.
> This list documents known-compatible implementations contributed
> by the community. To add your service, open a PR updating this table.

## Observability

The module instruments all operations with OpenTelemetry:

- **Activities**: `Attestation.Attest` and `Attestation.Verify` with semantic tags
- **Metrics**: 5 instruments tracking attestation volume, success/failure rates, and latency
- **Logging**: 9 structured events in the 9600-9608 EventId range using `[LoggerMessage]` source generator

## Testing

Property-based tests (FsCheck) verify the core hash chain invariants:
- Append-only: N records produce N receipts with sequential chain indices
- No gaps: Each receipt's HMAC chain link matches its predecessor
- Tamper detection: Modified content hashes fail verification
- Idempotency: Same RecordId returns identical receipts
- Determinism: Same content always produces the same hash
