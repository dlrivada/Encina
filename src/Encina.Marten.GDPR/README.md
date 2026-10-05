# Encina.Marten.GDPR

[![NuGet](https://img.shields.io/nuget/v/Encina.Marten.GDPR.svg)](https://www.nuget.org/packages/Encina.Marten.GDPR/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](../../LICENSE)

Crypto-shredding for GDPR compliance in Marten event-sourced systems. Encrypts PII fields with per-subject keys and enables GDPR Article 17 "Right to be Forgotten" by deleting encryption keys, rendering data permanently unreadable without modifying event history.

## Features

- **`[CryptoShredded]` Attribute** — Declarative PII marking with subject binding, on events, snapshots, read models and the objects nested in them (nested objects, collection elements, dictionary values)
- **System.Text.Json contract modifier** — Encrypts on save and decrypts on load inside Marten's System.Text.Json serializer (`CryptoShredderSerializer`); the caller's objects are not mutated. System.Text.Json only
- **AES-256-GCM v2 token** — `cs2:{version}:{payload}`; no subject id is stored, the subject is bound as associated data
- **Per-Subject Key Management** — InMemory (testing) and PostgreSQL (production) key providers, reached through `IServiceScopeFactory`
- **Key Rotation** — Forward-only encryption with versioned keys; old events decrypt with the version in their token
- **Right to be Forgotten** — Delete all subject keys to crypto-shred PII permanently
- **Fail-closed reads** — Only a forgotten subject reads as the placeholder; every other failure throws
- **DSR Integration** — `CryptoShredErasureStrategy` plugs into the `Encina.Compliance.DataSubjectRights` erasure workflow through a routing strategy
- **PII Discovery** — `MartenEventPersonalDataLocator` discovers PII in Marten event streams and reports nested paths
- **Configurable Placeholder** — Forgotten data replaced with `[REDACTED]` (customizable)
- **Startup validation** — Classifies every `[CryptoShredded]` type and checks the installation and the wiring; the host does not start when anything is wrong
- **Full Observability** — OpenTelemetry tracing, structured logging, metrics
- **Health Check** — Verifies the serializer installation and the key provider
- **.NET 10 Compatible** — Built with latest C# features

## Installation

```bash
dotnet add package Encina.Marten.GDPR
```

## Prerequisites

This package requires `Encina.Security.Encryption` to be configured first:

```csharp
// Required: Encina.Security.Encryption infrastructure
services.AddEncinaEncryption();

// Optional: enables DSR erasure workflows and PII discovery
services.AddEncinaDataSubjectRights();
```

## Quick Start

### 1. Register Services

```csharp
services.AddEncinaEncryption();
services.AddEncinaMartenGdpr(options =>
{
    options.UsePostgreSqlKeyStore = true;  // Production: persist keys in PostgreSQL
    options.AddHealthCheck = true;
    options.AssembliesToScan.Add(typeof(Program).Assembly);
});
```

### 2. Decorate Event Properties

```csharp
public sealed record UserEmailChangedEvent
{
    public string UserId { get; init; } = string.Empty;

    [PersonalData(Category = PersonalDataCategory.Contact, Erasable = true)]
    [CryptoShredded(SubjectIdProperty = nameof(UserId))]
    public string Email { get; init; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; init; }
}
```

The `[CryptoShredded]` attribute requires:
- A co-located `[PersonalData]` attribute (from `Encina.Compliance.DataSubjectRights`)
- A `SubjectIdProperty` pointing to the sibling property **on the same declaring object** (at every depth; a nested value object carries its own subject-id property) containing the data subject's ID: `string`, `Guid`, an integer type, or a strongly-typed id (an `IFormattable` type or a wrapper with a public `Value` of those types). Any other type is rejected by the startup validation when its type is in the scanned assemblies (`ValidateOnStartup`, `AssembliesToScan`), and by the contract modifier on first use otherwise. Erase with the id's string form (`Guid` as `ToString("D")`)

Call Marten's `UseTypeInfoResolver(context)` (a source-generated `JsonSerializerContext`) **before** `AddEncinaMartenGdpr`: a context added later is placed ahead of the modifier and its types would be written unencrypted. Startup detects this and stops the host.

### 3. Events Are Encrypted Transparently

```csharp
// Serialization: Email is encrypted with the subject's key before Marten stores it
session.Events.Append(streamId, new UserEmailChangedEvent
{
    UserId = "user-123",
    Email = "alice@example.com",
    OccurredAtUtc = DateTimeOffset.UtcNow
});
await session.SaveChangesAsync();

// Deserialization: Email is decrypted automatically when loading events
var events = await session.Events.FetchStreamAsync(streamId);
```

### 4. Forget a Subject (GDPR Article 17)

```csharp
var keyProvider = serviceProvider.GetRequiredService<ISubjectKeyProvider>();

// Delete all encryption keys for the subject
var result = await keyProvider.DeleteSubjectKeysAsync("user-123");

result.Match(
    Right: r => Console.WriteLine($"Forgotten: {r.KeysDeleted} keys deleted"),
    Left:  e => Console.WriteLine($"Error: {e.GetCode().IfNone("encina.unknown")}"));

// After forgetting: Email fields read as "[REDACTED]" instead of the encrypted token
```

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `UsePostgreSqlKeyStore` | `bool` | `false` | Use PostgreSQL-backed key storage (required for production) |
| `AnonymizedPlaceholder` | `string` | `"[REDACTED]"` | Placeholder for forgotten subjects' PII |
| `KeyRotationDays` | `int` | `90` | Recommended key rotation interval (informational) |
| `ValidateOnStartup` | `bool` | `true` | Validate `[CryptoShredded]` types and the wiring at startup; `false` is a logged opt-out (EventId 8462) |
| `AddHealthCheck` | `bool` | `false` | Register `encina-crypto-shredding` health check |
| `PublishEvents` | `bool` | `true` | Reserved: the key lifecycle events are not published yet |
| `AssembliesToScan` | `List<Assembly>` | `[]` | Assemblies for the startup validation (the entry assembly when empty) |

## Key Provider Selection

| Provider | Scope | Persistence | Use Case |
|----------|-------|-------------|----------|
| `InMemorySubjectKeyProvider` | Singleton | Process lifetime (every key is lost on restart and older data then fails to read; warning 8484) | Testing, development |
| `PostgreSqlSubjectKeyProvider` | Scoped | Marten document store | Production |

- `PostgreSqlSubjectKeyProvider` serializes key creation, rotation and erasure of one subject with `pg_advisory_xact_lock` (keyed by the Marten tenant and the subject id) inside the transaction that checks the forgotten marker and writes, so concurrent first writers receive the one stored key and no key is created after an erasure commits.
- Each operation opens its own Marten session and never flushes the caller's.
- `DeleteSubjectKeysAsync` is idempotent and returns `crypto.subject_forgotten` when the subject was already forgotten.
- `InMemorySubjectKeyProvider` returns copies of key material, so erasure never alters a key a caller holds.
- Log EventIds: 8467 (initial key created, Debug), 8468 (concurrent key write resolved to the stored key, Warning; fields `Operation`, `Version`), 8469 (leftover keys erased on a repeated erasure, Warning; field `KeysDeleted`).

```csharp
// Development (default)
services.AddEncinaMartenGdpr();

// Production
services.AddEncinaMartenGdpr(options =>
{
    options.UsePostgreSqlKeyStore = true;
});
```

## Key Rotation

Key rotation creates a new active key version. Old versions remain available for decrypting existing events:

```csharp
var keyProvider = serviceProvider.GetRequiredService<ISubjectKeyProvider>();

var result = await keyProvider.RotateSubjectKeyAsync("user-123");

result.Match(
    Right: r => Console.WriteLine($"Rotated to version {r.NewVersion}"),
    Left:  e => Console.WriteLine($"Error: {e.GetCode().IfNone("encina.unknown")}"));
```

After rotation:
- **New events** are encrypted with the latest key version
- **Existing events** are decrypted with the key version stored in their token
- Old key versions transition to `Rotated` status but remain retrievable

## DSR Integration

When `Encina.Compliance.DataSubjectRights` is registered, `CryptoShredErasureStrategy` automatically participates in erasure workflows:

```csharp
services.AddEncinaEncryption();
services.AddEncinaDataSubjectRights();
services.AddEncinaMartenGdpr(options =>
{
    options.UsePostgreSqlKeyStore = true;
});

// Erasure workflow triggers crypto-shredding automatically
var executor = serviceProvider.GetRequiredService<IDataErasureExecutor>();
var scope = new ErasureScope { Reason = ErasureReason.ConsentWithdrawn };
await executor.EraseAsync("user-123", scope, cancellationToken);
```

`MartenEventPersonalDataLocator` discovers all PII locations in the Marten event stream for the specified subject. `PersonalDataLocation.FieldName` is the path of the field (`Email`, `Contact.Email`, `Items[].Note`). Erasure is subject-wide: it shreds every crypto-shredded field of the subject whatever `FieldName` or `ErasureScope.SpecificFields` say (#1144).

`AddEncinaMartenGdpr` registers a routing `IDataErasureStrategy`: Marten locations always reach `CryptoShredErasureStrategy`, every other location goes to the strategy registered **before** `AddEncinaMartenGdpr`. A strategy registered after it, or an `IPersonalDataLocator` that is not a `CompositePersonalDataLocator` containing the Marten locator, stops the host at startup.

## Projection Handling

Projections should check for forgotten subjects before rendering PII:

```csharp
public class UserProjection : SingleStreamProjection<UserView>
{
    public void Apply(UserEmailChangedEvent e, UserView view)
    {
        // If the subject has been forgotten, Email will be "[REDACTED]"
        view.Email = e.Email;
    }
}
```

The serializer automatically substitutes the `AnonymizedPlaceholder` during deserialization when a subject's keys have been deleted. Any other read failure (key-store outage, tampering) throws `CryptoShreddingDecryptionException`; the configurator turns off Marten's `SkipSerializationErrors` so the async daemon pauses the shard instead of dead-lettering the event.

## Error Codes

| Code | Meaning |
|------|---------|
| `crypto.subject_forgotten` | Subject has been cryptographically forgotten; PII is permanently unreadable |
| `crypto.encryption_failed` | PII field encryption failed during serialization |
| `crypto.decryption_failed` | PII field decryption failed during deserialization |
| `crypto.key_rotation_failed` | Key rotation failed for a subject |
| `crypto.key_store_error` | Key store infrastructure error |
| `crypto.invalid_subject_id` | Invalid or empty subject identifier |
| `crypto.key_already_exists` | Active key already exists (use rotation instead) |
| `crypto.serialization_error` | Crypto-shredding serialization/deserialization error |
| `crypto.attribute_misconfigured` | `[CryptoShredded]` attribute is misconfigured |
| `crypto.envelope_malformed` | A stored value is neither a `cs2` token nor the tombstone |
| `crypto.integrity_check_failed` | Authentication tag or associated data did not match |
| `crypto.serializer_unsupported` | The Marten store serializer is not the crypto-shredding serializer |
| `crypto.erasure_strategy_missing` | A non-Marten location reached the erasure router and no other strategy is registered |

## Fail-Closed Serialization and Reads

The event store is append-only, so personal data written in plaintext can never be crypto-shredded. The package throws three exceptions, all deriving from `InvalidOperationException`, whose messages carry type names, property names, reasons and error codes only (never a subject id, a value or an inner exception message; compliance gates fail closed, [SPEC-002](../../docs/specifications/SPEC-002-eu-regulatory-readiness.md) DEC-006). There is no opt-out. A null value is left null without a key lookup.

| Exception | Thrown when | Key members |
|-----------|-------------|-------------|
| `CryptoShreddingConfigurationException` | A type or the wiring is wrong: at startup (the host does not start) or on the first use of a misconfigured type, before any byte is written | `Problem` (`CryptoShreddingConfigurationProblem`), `Issues` (per property, with `CryptoShreddedPropertyProblems` flags), `ComponentType` |
| `CryptoShreddingEncryptionException` | A non-null value cannot be encrypted while serializing; Marten's append or `SaveChangesAsync` fails and nothing is stored | `DocumentTypeName`, `DeclaringTypeName`, `PropertyName`, `Reason` (`SubjectIdMissing`, `SubjectIdInvalid`, `KeyUnavailable`), `ErrorCode` |
| `CryptoShreddingDecryptionException` | A stored value cannot be read and the subject is not forgotten | `DocumentTypeName`, `DeclaringTypeName`, `PropertyName`, `Reason` (`SubjectIdMissing`, `SubjectIdInvalid`, `KeyUnavailable`, `IntegrityCheckFailed`, `EnvelopeMalformed`, `PropertyNotWritable`), `ErrorCode` |

- `SubjectIdMissing`: the subject id is `null`, `Guid.Empty`, or an empty or whitespace string (logged as EventId 8466).
- `KeyUnavailable` on write: `GetOrCreateSubjectKeyAsync` returned `Left` (a forgotten subject included), threw, or returned an unusable key (version below 1, or key material that is not 32 bytes) (logged as EventId 8455). On read: any `Left` other than `crypto.subject_forgotten`, or a provider that threw (EventId 8456).
- A tombstone (`cs2:erased`, written when the placeholder of a forgotten subject is saved again) is accepted on read only after `IsSubjectForgottenAsync` confirms the subject is forgotten.
- Misconfigured properties are logged at error level as EventId 8459, once per issue, and the startup summary as 8470.

Reference for the flags, the configuration problems, the read semantics and the limits (Marten `Patch`, duplicated fields, raw `JsonDocument` upcasters, string collections, personal data on interfaces): [crypto-shredding feature page](../../docs/features/crypto-shredding.md#validation). Design reasons: [ADR-034](../../docs/architecture/adr/034-crypto-shredding-through-the-stj-contract.md).

## Observability

### Tracing

`ActivitySource`: `Encina.Marten.GDPR`

| Activity | Kind | Tags |
|----------|------|------|
| `CryptoShredding.Encrypt` | Internal | `crypto.event_type`, `crypto.outcome` |
| `CryptoShredding.Decrypt` | Internal | `crypto.event_type`, `crypto.outcome` |
| `CryptoShredding.Forget` | Internal | `crypto.outcome`, `crypto.failure_reason` (on failure) |
| `CryptoShredding.KeyRotation` | Internal | `crypto.outcome`, `crypto.failure_reason` (on failure) |
| `CryptoShredding.Erasure` | Internal | `crypto.outcome`, `crypto.failure_reason` (on failure) |

### Metrics

`Meter`: `Encina.Marten.GDPR`

| Metric | Type | Description |
|--------|------|-------------|
| `crypto.encryption.total` | Counter | Total PII field encryptions |
| `crypto.decryption.total` | Counter | Total PII field decryptions |
| `crypto.encryption.failed` | Counter | Encryption failures |
| `crypto.decryption.failed` | Counter | Decryption failures |
| `crypto.forgotten_access.total` | Counter | Decrypt attempts on forgotten subjects |
| `crypto.key_rotation.total` | Counter | Subject key rotations |
| `crypto.forget.total` | Counter | Subject forget (crypto-shred) operations |
| `crypto.configuration.misconfigured.total` | Counter | Misconfiguration findings |
| `crypto.encryption.duration` | Histogram (ms) | Encryption duration |
| `crypto.decryption.duration` | Histogram (ms) | Decryption duration |
| `crypto.forget.duration` | Histogram (ms) | Forget operation duration |

### Health Check

When `AddHealthCheck = true`, the `encina-crypto-shredding` health check is Unhealthy when the Marten serializer is not the crypto-shredding serializer, when the installation check fails (resolver replaced or bypassed, nested canary not encrypted) or when `ISubjectKeyProvider` cannot be resolved. Its data: `keyProviderType`, `cryptoContractCount`, `misconfiguredTypeCount`.

## Custom Implementations

Register custom implementations before `AddEncinaMartenGdpr()` to override defaults (TryAdd semantics):

```csharp
// Custom key provider
services.AddSingleton<ISubjectKeyProvider, MyVaultKeyProvider>();

// Custom forgotten subject handler
services.AddSingleton<IForgottenSubjectHandler, CustomForgottenSubjectHandler>();

// Custom erasure strategy: becomes the inner strategy of the crypto-shredding router
services.AddScoped<IDataErasureStrategy, CustomErasureStrategy>();

services.AddEncinaMartenGdpr(); // Won't override your registrations
```

A custom `ISubjectKeyProvider` implements `GetOrCreateSubjectKeyAsync` with the return type `ValueTask<Either<EncinaError, SubjectEncryptionKey>>`. `SubjectEncryptionKey` carries `Version` and `KeyMaterial`, which must come from one read of the key store so the version in the token names the key that encrypted the value. Return `Left` (for example `crypto.subject_forgotten`) when no key can be provided; the serializer then refuses to store the event.

## Domain Events

`SubjectForgottenEvent` and `SubjectKeyRotatedEvent` exist as types, but nothing publishes them yet, whatever `PublishEvents` says. Wiring them is a follow-up.

## Performance

<!-- docref-table: bench:gdpr/* -->
<!-- /docref-table -->

*Auto-generated from benchmark data. See [methodology](../../docs/testing/performance-measurement-methodology.md).*

## Related Packages

| Package | Description |
|---------|-------------|
| `Encina.Marten` | Core Marten event sourcing integration |
| `Encina.Security.Encryption` | Field-level encryption with AES-256-GCM (prerequisite) |
| `Encina.Compliance.DataSubjectRights` | DSR workflows, erasure strategies, PII discovery (optional) |
| `Encina.Compliance.GDPR` | Processing activity tracking, RoPA, lawful basis validation |

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.
