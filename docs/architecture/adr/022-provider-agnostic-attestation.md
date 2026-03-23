# ADR-022: Provider-Agnostic Attestation via `IAuditAttestationProvider`

## Status

Accepted

## Date

2026-03-21

## Context

Issue #803 introduces tamper-evident audit attestation as a compliance primitive for the Encina framework. The requirement is to produce cryptographically verifiable records that a specific audit event occurred at a specific time — supporting EU AI Act Art. 12 (record-keeping), Art. 17 (logging for high-risk AI), and general audit integrity for regulated deployments.

### The Core Design Question

Should attestation be implemented as a direct integration with a specific backend, or abstracted as a provider?

**Candidate backends considered:**

| Backend | Approach | Properties |
|---------|----------|------------|
| Rekor (Sigstore) | HTTP to public transparency log | Public, append-only, cryptographic proofs |
| RFC 3161 TSA | HTTP to trusted timestamp authority | Standard, widely accepted, centralized |
| Local hash chain | SHA-256 append-only linked list | Air-gapped, self-contained, no external dependency |
| Blockchain | On-chain transaction | Immutable, decentralized, high cost |
| None (in-memory) | Runtime-only map | Dev/test only, no persistence |

### Relevant Constraints

1. **Deployment diversity**: Encina targets enterprise deployments ranging from air-gapped on-premise environments (where Rekor or any external TSA is unreachable) to cloud-native deployments where public transparency logs are acceptable.

2. **Regulatory evolution**: EU AI Act implementing acts are still being finalized. Prescribing a specific attestation backend today risks non-compliance if the acceptable methods list changes in future delegated acts.

3. **Existing framework pattern**: Encina already abstracts pluggable concerns via provider interfaces — `ICacheProvider` (ADR-003), `IAuditStore` (audit trail), `ISubjectKeyProvider` (crypto-shredding). Consumers integrate these without coupling to a specific implementation.

4. **Testability**: Integration with external transparency logs is not suitable for unit or contract tests. Any design must support a deterministic, in-process implementation for the test tier.

5. **Separation from `IAuditStore`**: `IAuditStore` records *what happened* (audit trail). `IAuditAttestationProvider` proves *that the record has not been tampered with* (integrity proof). These are distinct concerns — a record can be audited without being attested, and an attestation receipt is meaningful only in conjunction with the original record.

## Decision

Introduce `IAuditAttestationProvider` as a provider interface following Encina's standard extensibility pattern (ADR-007).

### Interface Contract

```csharp
public interface IAuditAttestationProvider
{
    Task<AttestationReceipt> AttestAsync(AuditRecord record, CancellationToken ct = default);
    Task<AttestationVerification> VerifyAsync(AuditRecord record, AttestationReceipt receipt, CancellationToken ct = default);
    Task<AttestationReceipt?> GetReceiptAsync(string receiptId, CancellationToken ct = default);
}
```

`AuditRecord` is content-addressed: the provider computes a SHA-256 hash of the record payload before attesting. This ensures the receipt is bound to the record content, not just its identifier.

### Bundled Providers

Three providers ship with `Encina.Compliance.Attestation`:

| Provider | Class | Use Case |
|----------|-------|----------|
| In-memory | `InMemoryAttestationProvider` | Development, unit tests, contract tests |
| Hash chain | `HashChainAttestationProvider` | Air-gapped, self-hosted, or offline deployments |
| HTTP | `HttpAttestationProvider` | External attestation services (Rekor, custom TSA) |

The `HttpAttestationProvider` is deliberately generic — it speaks a simple `POST /attest` / `GET /receipt/{id}` contract, not Rekor's specific API. Rekor-specific support (including transparency log index extraction) can be layered via a derived provider or adapter without modifying the core abstraction.

### Deliberate Exclusions

The following were evaluated and deferred:

- **RFC 3161 TSA provider**: Deferred to a future issue. The HTTP provider can proxy a TSA endpoint; a first-class TSA provider would add ASN.1 parsing and certificate chain validation — out of scope for this iteration.
- **Blockchain provider**: Deferred. Cost and throughput characteristics are not suitable for high-frequency audit events. A future provider could batch receipts and submit a Merkle root.
- **`IAuditStore` integration**: Attesting on every `IAuditStore.AppendAsync` call is not automatic. Consumers opt in via the `[AttestDecision]` attribute or explicit `IAuditAttestationProvider` injection. This keeps the audit trail path unconditionally fast.

### DI Registration

```csharp
services.AddEncinaAttestation(options =>
{
    options.HashAlgorithm = HashAlgorithmName.SHA256; // reserved, SHA-256 fixed for now
})
.UseInMemoryAttestation()         // dev/test
// or .UseHashChainAttestation()  // file-backed chain
// or .UseHttpAttestation(o => { o.BaseAddress = new Uri("https://attestation.example.com"); });
```

Only one provider is registered at a time. The choice is a deployment-time configuration decision, not a compile-time one.

## Alternatives Considered

### A. Direct Rekor Integration

Implement `AttestAsync` as a direct call to `https://rekor.sigstore.dev/api/v1/log/entries`.

**Rejected because:**
- Couples the compliance module to a single vendor's availability SLA
- Breaks air-gapped deployments unconditionally
- The Rekor API returns a `logIndex` — a meaningful proof only if the verifier also trusts the Sigstore root of trust. This is an acceptable assumption for open-source projects but not for regulated enterprise contexts where the trust anchor must be configurable.

### B. Extend `IAuditStore` with an `AttestAsync` Overload

Add `AttestAsync(AuditEntry entry)` directly to `IAuditStore`.

**Rejected because:**
- `IAuditStore` is already implemented by 13 database providers. Adding attestation would require each to implement or stub attestation logic.
- Audit storage and attestation have different failure semantics: a storage write failure should surface immediately; an attestation failure may be acceptable to log-and-continue depending on the compliance posture.
- Violates the single-responsibility principle at the interface level.

### C. Timestamp-Only (RFC 3161)

Issue a trusted timestamp for each audit record without a full attestation receipt.

**Rejected as the primary design**, but not excluded as an implementation strategy. RFC 3161 timestamps prove *when* a record existed but do not prove it has not been modified since. The `AttestationReceipt` model is a superset of a timestamp — it includes a content hash, receipt identifier, and an optional provider-specific payload that can carry a TSA response.

## Consequences

### Positive

- **Deployment flexibility**: InMemory for tests, HashChain for air-gapped production, Http for cloud or Rekor-backed deployments — same application code, different DI configuration.
- **Testability**: `AttestationProviderContractTests` validates all three bundled providers against a shared contract. No external services required in CI.
- **Future-proof**: When EU AI Act implementing acts specify acceptable attestation methods more precisely, a compliant provider can be added without touching the interface or existing consumers.
- **Consistent with framework patterns**: Consumers already familiar with `ICacheProvider` or `IDistributedLockProvider` registration will find `IAuditAttestationProvider` registration immediately recognizable.

### Negative

- **Compliance-grade ambiguity**: The provider pattern does not prevent a consumer from registering `InMemoryAttestationProvider` in production. Documentation and health checks (`AttestationHealthCheck`) mitigate this but cannot enforce it.
- **No automatic cross-provider receipt portability**: A receipt issued by `HashChainAttestationProvider` cannot be verified by `HttpAttestationProvider` — the verifier must use the same provider that issued the receipt. This is by design (receipts are provider-scoped) but must be documented clearly to avoid operational confusion.

## References

- Issue #803: [FEATURE] Encina.Compliance.Attestation - Provider-Agnostic Tamper-Evident Audit Attestation
- PR #849: feat(attestation): provider-agnostic tamper-evident audit attestation
- ADR-002: Dependency Injection Strategy
- ADR-007: Extensibility Strategy v2 (provider pattern)
- ADR-018: Cross-Cutting Integration Principle
- ADR-020: Temporal Crypto-Shredding for Marten-Based Audit Store
