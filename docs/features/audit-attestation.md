# Audit Attestation in Encina

Encina.Compliance.Attestation provides a provider-agnostic, tamper-evident audit attestation framework at the CQRS pipeline level. It enables externally verifiable proof that audit records have not been modified retroactively.

## Table of Contents

1. [Overview](#overview)
2. [The Problem](#the-problem)
3. [The Solution](#the-solution)
4. [Quick Start](#quick-start)
5. [Core Types](#core-types)
6. [Providers](#providers)
7. [Pipeline Integration](#pipeline-integration)
8. [Configuration Reference](#configuration-reference)
9. [HTTP Attestation Provider](#http-attestation-provider)
10. [Compatible Third-Party Attestation Services](#compatible-third-party-attestation-services)
11. [Observability](#observability)
12. [Health Check](#health-check)
13. [Testing](#testing)

---

## Overview

Encina.Compliance.Attestation adds an external proof layer to audit records. While internal audit stores (Marten event sourcing, `IAuditStore`) prove events were recorded in order, external attestation proves events existed at a specific time and have not been altered — satisfying requirements from EU AI Act (Art. 13), GDPR accountability (Art. 5(2)), and NIS2 incident reporting.

| Layer | What it proves | Who trusts it |
|-------|---------------|---------------|
| **Internal audit** (Marten ES, IAuditStore) | Events were recorded in order | Development and ops teams |
| **External attestation** (this feature) | Events existed at a specific time and have not been altered | Regulators, auditors, insurers |

## The Problem

A DBA with database access could alter internal audit records. For compliance-critical operations — high-risk AI decisions, data breach timelines, GDPR data subject requests — regulators may require proof that is independently verifiable, outside the system's own trust boundary.

## The Solution

`IAuditAttestationProvider` creates and verifies tamper-evident attestations for audit records. Implementations range from local hash chains (free, self-hosted) to cloud immutable ledgers and third-party services. All patterns are **opt-in** — attestation is only activated when explicitly configured.

## Quick Start

```csharp
// Development / testing
services.AddEncinaAttestation(options =>
{
    options.UseInMemory();
});

// Self-hosted production (zero cost)
services.AddEncinaAttestation(options =>
{
    options.UseHashChain(chain =>
    {
        chain.StoragePath = "/var/encina/attestations";
        chain.HashAlgorithm = HashAlgorithmName.SHA256;
    });
});

// External HTTP attestation endpoint
services.AddEncinaAttestation(options =>
{
    options.UseHttp(http =>
    {
        http.EndpointUrl = new Uri("https://attestation.example.com/api/attest");
        http.AuthHeader = "Bearer <token>";
    });
});
```

## Core Types

### IAuditAttestationProvider

```csharp
public interface IAuditAttestationProvider
{
    ValueTask<Either<EncinaError, AttestationReceipt>> AttestAsync(
        AuditRecord record, CancellationToken ct = default);

    ValueTask<Either<EncinaError, AttestationVerification>> VerifyAsync(
        AttestationReceipt receipt, CancellationToken ct = default);
}
```

### AttestationReceipt

Immutable receipt proving an audit record was attested at a specific time. Includes `AttestationId`, `AuditRecordId`, `ContentHash`, `AttestedAtUtc`, `ProviderName`, `Signature`, and optional `ProofMetadata`.

### AttestationVerification

Result of verifying an attestation receipt. Contains `IsValid`, `VerifiedAtUtc`, `AttestationId`, `ProviderName`, and optional `FailureReason`.

### AuditRecord

Represents an audit record to be attested. Contains `RecordId`, `RecordType`, `OccurredAtUtc`, `SerializedContent`, and optional fields for `CorrelationId`, `ActorId`, `TenantId`, `ModuleId`, and `Metadata`.

## Providers

| Provider | Package | Purpose | Mechanism |
|----------|---------|---------|-----------|
| `InMemoryAttestationProvider` | `Encina.Compliance.Attestation` | Testing and development | In-memory hash map |
| `HashChainAttestationProvider` | `Encina.Compliance.Attestation` | Self-hosted, no cloud dependency | SHA-256 hash chain with append-only storage |
| `HttpAttestationProvider` | `Encina.Compliance.Attestation` | External HTTP endpoints | POST + receipt storage, configurable auth/payload |

### Future / Community Providers

| Provider | Package | Backend |
|----------|---------|---------|
| `AzureLedgerAttestationProvider` | `Encina.Compliance.Attestation.Azure` | Azure Confidential Ledger |
| `AwsQldbAttestationProvider` | `Encina.Compliance.Attestation.Aws` | Amazon QLDB |

## Pipeline Integration

Attestation integrates as an optional decorator on the audit pipeline via the `[AttestDecision]` attribute:

```csharp
[RequireHumanOversight(Reason = "High-risk AI decision")]
[AttestDecision(FailureMode = AttestationFailureMode.Enforce)]
public record ApproveLoanCommand : ICommand<LoanDecision>;
```

The `AttestationPipelineBehavior<TRequest, TResponse>` processes commands marked with `[AttestDecision]`. The `FailureMode` property controls behavior when attestation fails:

- `Enforce` — the command fails if attestation fails
- `LogOnly` — the command proceeds, but attestation failure is logged

## Configuration Reference

### AttestationOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ProviderType` | `AttestationProviderType` | `InMemory` | Which provider to use |

### HttpAttestationOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `EndpointUrl` | `Uri` | — | Attestation service URL |
| `AuthHeader` | `string?` | `null` | Authorization header value |
| `AllowInsecureHttp` | `bool` | `false` | Allow HTTP (not HTTPS) endpoints |
| `MaxResponseContentBufferSize` | `long` | `1048576` (1 MB) | Max response size |

## HTTP Attestation Provider

The `HttpAttestationProvider` works with any service implementing the following REST contract:

### Expected REST Contract

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

### Security Considerations

- HTTPS is enforced by default; `AllowInsecureHttp` must be explicitly opted in
- `AuthHeader` is marked `[JsonIgnore]` and redacted from `ToString()` output
- Error response bodies are truncated to 500 characters; full body logged at Debug level only
- Response size is capped at 1 MB by default via `MaxResponseContentBufferSize`
- SSRF protection is applied via `HttpAttestationOptionsValidator`

## Compatible Third-Party Attestation Services

The `HttpAttestationProvider` works with any service implementing the expected
REST contract (`POST /attest` returning a receipt, `GET /receipt/{id}` returning
a stored receipt). This includes but is not limited to:

| Service | Backend | Notes |
|---------|---------|-------|
| [Trust Layer](https://trust.arkforge.tech) | Rekor transparency log | Publicly auditable, Ed25519 signatures |

> Encina does not endorse or guarantee any third-party service.
> This list documents known-compatible implementations contributed
> by the community. To add your service, open a PR updating this table.

## Observability

### OpenTelemetry

Attestation operations emit traces and metrics:

- **Traces**: `encina.attestation.attest`, `encina.attestation.verify`
- **Counters**: `encina.attestation.verification.succeeded`, `encina.attestation.verification.failed`
- **Histogram**: `encina.attestation.verification.duration`

### Structured Logging

All logging uses `[LoggerMessage]` source generator with EventIds registered in `EventIdRanges.cs`.

## Health Check

The attestation health check verifies provider availability without contaminating the hash chain — it verifies an existing receipt rather than creating a new one.

```csharp
services.AddHealthChecks()
    .AddEncinaAttestationHealthCheck();
```

## Testing

### Using InMemoryAttestationProvider

```csharp
services.AddEncinaAttestation(options =>
{
    options.UseInMemory();
});
```

The in-memory provider supports idempotent attestation (attesting the same record twice returns the existing receipt) and all verification operations, making it suitable for unit and integration tests.

### Test Coverage

| Test Type | Scope |
|-----------|-------|
| **UnitTests** | All providers, receipt creation, hash chain integrity |
| **GuardTests** | All public methods — null checks, invalid arguments |
| **ContractTests** | `IAuditAttestationProvider` contract across all providers |
| **PropertyTests** | Hash chain invariants: append-only, no gaps, tamper detection |

## Related Features

- [Anti-Tampering](anti-tampering.md) — HMAC-based request signing and integrity verification
- [Read Auditing](read-auditing.md) — Internal audit record creation
- [Audit Tracking](audit-tracking.md) — Change tracking and audit trail
- [GDPR Compliance](gdpr-compliance.md) — Data protection compliance patterns
- [NIS2 Compliance](nis2-compliance.md) — Network and information security directive
