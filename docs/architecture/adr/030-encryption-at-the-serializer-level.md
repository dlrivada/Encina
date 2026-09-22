# ADR-030: Message and Field Encryption Live at the Serializer Level with AES-256-GCM

## Status

**Accepted** - decided in issues #129 and #396 (2026); recorded as an ADR on 2026-09-22 from the project history.

## Context

Two needs arrived separately: encrypting whole messages at rest and in transit through the Outbox and transports, and encrypting individual PII fields inside documents and events (a prerequisite for crypto-shredding under GDPR, ADR-020). Doing either inside handlers or stores would have meant one implementation per provider and per transport, and a format nobody could read back outside Encina.

## Decision

- Encryption is applied **at the serializer level**, as a decorator over the configured serializer: messages are encrypted after serialization and decrypted before deserialization, so stores and transports carry opaque payloads and need no changes (#129: "AES-256-GCM encryption (NIST SP 800-38D)... Serializer decorator").
- The algorithm is **AES-256-GCM** (authenticated encryption); field-level values use the self-describing format `ENC:v1:{Algorithm}:{KeyId}:{Nonce}:{Tag}:{Ciphertext}` (#396), so a value carries its key identifier and can be rotated or shredded per key.
- **Key management is a separate package per backend**: `Encina.Messaging.Encryption.AzureKeyVault`, `Encina.Messaging.Encryption.AwsKms`, `Encina.Messaging.Encryption.DataProtection` (#129), sharing the `Encina.Messaging.Encryption` abstractions.

## Rationale

- One implementation covers the ten database providers and every transport; provider coherence is preserved without touching each store.
- Authenticated encryption detects tampering; the versioned format allows algorithm and key rotation without rewriting stored data.
- Separating key management keeps cloud SDK dependencies out of the core packages (pay for what you use).

## Alternatives rejected

- **Encrypting in each store or transport:** rejected; N implementations and no single place to audit.
- **Transport-level encryption only (TLS):** rejected; it protects the wire, not the stored payload or the PII inside a document.
- **A single key-management implementation in the core package:** rejected; it would force every consumer to depend on one cloud SDK.

## Consequences

- Crypto-shredding (ADR-020) and the PII package build on the `ENC:v1` format and its key ids.
- Any new serializer must be wrapped by the encryption decorator to stay in the guarantee; the cross-cutting evaluation of a feature that stores PII must say which fields are encrypted.
- Key rotation is a key-management operation, not a data migration.

## References

- Issues #129, #396; ADR-020 (temporal crypto-shredding audit store); `docs/engineering/PROJECT-HISTORY.md`, "Messaging patterns and transports" and "Security and regulatory compliance".
