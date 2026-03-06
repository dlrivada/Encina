# IntegrationTests - Message Encryption

## Status: Not Implemented

## Justification

Message encryption (`Encina.Messaging.Encryption`) is a **non-database feature**. It operates as an in-memory decorator around `IMessageSerializer`, delegating cryptographic operations to `IFieldEncryptor` and `IKeyProvider` from `Encina.Security.Encryption`.

### 1. No Database Interaction

The encryption module does not interact with any database directly:

- `DefaultMessageEncryptionProvider` delegates to `IFieldEncryptor` (in-memory crypto)
- `EncryptingMessageSerializer` wraps an inner `IMessageSerializer` (decorator pattern)
- `EncryptedPayloadFormatter` performs string formatting/parsing (pure functions)
- `DefaultTenantKeyResolver` applies a naming pattern (pure function)

Integration tests for real database encryption are covered by the **outbox/inbox store integration tests** in each provider, where the `EncryptingMessageSerializer` is naturally exercised.

### 2. KMS Provider Integration Tests

The cloud KMS providers (Azure Key Vault, AWS KMS) would benefit from integration tests against real services, but these require cloud credentials and are not suitable for local CI/CD. They will be added as **manual integration tests** with `[Trait("Category", "CloudIntegration")]` in a future milestone.

### 3. Adequate Coverage from Other Test Types

- **Unit Tests** (62 tests): Full coverage of encrypt/decrypt flows, serializer decorator, formatter, health check, options, errors, tenant key resolver, attribute cache
- **Guard Tests** (13 tests): All public constructors and methods validated for null arguments
- **Contract Tests** (27 tests): Interface shapes, data type structures, error code conventions, attribute contracts, options defaults
- **Property Tests** (14 properties): Roundtrip invariants, value semantics, error code consistency, tenant key resolver determinism
- **Benchmark Tests**: Encrypt/decrypt performance at different payload sizes, formatter throughput, attribute cache cold/warm comparison

### 4. Recommended Alternative

If integration tests become necessary:

- Test the full pipeline with `EncryptingMessageSerializer` wrapping a real `SystemTextJsonMessageSerializer`, encrypting with `AesGcmFieldEncryptor` and `InMemoryKeyProvider`
- Test round-trip through outbox store → serialize → encrypt → persist → read → decrypt → deserialize
- These would be added to existing provider integration tests (e.g., `OutboxStoreEFSqlServerIntegrationTests`)

## Related Files

- `src/Encina.Messaging.Encryption/` - Source package
- `tests/Encina.UnitTests/Messaging/Encryption/` - Unit tests
- `tests/Encina.GuardTests/Messaging/Encryption/` - Guard tests
- `tests/Encina.ContractTests/Messaging/Encryption/` - Contract tests
- `tests/Encina.PropertyTests/Messaging/Encryption/` - Property tests

## Date: 2026-03-06
## Issue: #129
