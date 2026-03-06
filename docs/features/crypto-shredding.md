# Crypto-Shredding in Encina

This guide explains how to implement GDPR Article 17 "Right to be Forgotten" compliance in Marten event-sourced systems using the `Encina.Marten.GDPR` package. Crypto-shredding encrypts PII at the field level with per-subject keys and enables data erasure by deleting keys — rendering PII permanently unreadable without modifying the immutable event log.

## Table of Contents

1. [Overview](#overview)
2. [The Problem](#the-problem)
3. [The Solution](#the-solution)
4. [Architecture](#architecture)
5. [Quick Start](#quick-start)
6. [The `[CryptoShredded]` Attribute](#the-cryptoshredded-attribute)
7. [Key Management](#key-management)
8. [Forgetting a Subject](#forgetting-a-subject)
9. [Key Rotation](#key-rotation)
10. [DSR Integration](#dsr-integration)
11. [Configuration Options](#configuration-options)
12. [Projection Handling](#projection-handling)
13. [Observability](#observability)
14. [Health Check](#health-check)
15. [Error Handling](#error-handling)
16. [Best Practices](#best-practices)
17. [Testing](#testing)
18. [FAQ](#faq)

---

## Overview

Encina.Marten.GDPR provides transparent, attribute-based crypto-shredding at the Marten serializer level:

| Component | Description |
|-----------|-------------|
| **`[CryptoShredded]` Attribute** | Marks PII properties on domain events with subject binding |
| **`CryptoShredderSerializer`** | Marten serializer decorator that encrypts/decrypts PII transparently |
| **`ISubjectKeyProvider`** | Per-subject key lifecycle: create, retrieve, rotate, delete |
| **`CryptoShredErasureStrategy`** | Bridges DSR erasure workflows with crypto-shredding |
| **`MartenEventPersonalDataLocator`** | Discovers PII in Marten event streams |
| **`CryptoShreddingOptions`** | Configuration for key store, placeholder, rotation, health check |

### Why Crypto-Shredding?

| Benefit | Description |
|---------|-------------|
| **Immutable event log** | Events are never modified or deleted — only keys are destroyed |
| **Per-subject isolation** | Each subject has independent encryption keys |
| **Forward-only encryption** | Key rotation encrypts new events; old events retain their key version |
| **Transparent** | Application code does not change — encryption is handled at the serializer level |
| **Auditable** | Key lifecycle events are published for compliance evidence |

---

## The Problem

Event-sourced systems store an immutable log of domain events. When a user exercises their GDPR Article 17 right to erasure, traditional approaches face a fundamental conflict:

```csharp
// Problem: Events are immutable — we cannot delete or modify them
session.Events.Append(streamId, new UserRegisteredEvent
{
    UserId = "user-123",
    Email = "alice@example.com",  // PII stored in plaintext
    Name = "Alice Smith"           // PII stored in plaintext
});

// GDPR Article 17 request: "Delete all my data"
// But we CANNOT modify or delete events in the event store!
```

---

## The Solution

Crypto-shredding encrypts PII at write time with a per-subject key. To "forget" a subject, delete the key — the encrypted ciphertext becomes permanently unreadable:

```csharp
// PII is encrypted transparently before storage
[PersonalData(Category = PersonalDataCategory.Contact, Erasable = true)]
[CryptoShredded(SubjectIdProperty = nameof(UserId))]
public string Email { get; init; } = string.Empty;

// In the event store, Email is stored as:
// {"__enc":true,"kid":"subject:user-123:v1","ct":"base64...","nonce":"base64..."}

// To forget the subject: delete the key
await keyProvider.DeleteSubjectKeysAsync("user-123");

// Now Email decrypts to "[REDACTED]" — permanently unreadable
```

---

## Architecture

### Dependency Diagram

```
Encina.Marten.GDPR
├── Encina.Marten                         (Marten event sourcing integration)
├── Encina.Security.Encryption            (IFieldEncryptor, AES-256-GCM)
└── Encina.Compliance.DataSubjectRights   ([PersonalData], DSR workflows)
```

### Encryption Flow (Event Serialization)

```
Domain Event → CryptoShredderSerializer
                 │
                 ├── CryptoShreddedPropertyCache.HasCryptoShreddedFields(type)?
                 │     └── No → delegate to inner ISerializer (zero overhead)
                 │
                 ├── Yes → for each [CryptoShredded] property:
                 │     ├── Extract SubjectId from SubjectIdProperty
                 │     ├── ISubjectKeyProvider.GetOrCreateSubjectKeyAsync(subjectId)
                 │     ├── IFieldEncryptor.Encrypt(plaintext, key) → ciphertext
                 │     ├── Replace property value with encrypted JSON envelope
                 │     └── Restore original value after serialization
                 │
                 └── Delegate to inner ISerializer → JSON output
```

### Forget Flow (Key Deletion)

```
GDPR Art. 17 Request → ISubjectKeyProvider.DeleteSubjectKeysAsync(subjectId)
                          │
                          ├── Delete ALL key versions for the subject
                          ├── Mark subject as "Forgotten" (SubjectForgottenMarker)
                          ├── Publish SubjectForgottenEvent
                          └── Return CryptoShreddingResult { KeysDeleted, ... }

Subsequent Deserialization:
  CryptoShredderSerializer
    ├── Detect encrypted field with kid = "subject:{subjectId}:v{N}"
    ├── ISubjectKeyProvider.GetSubjectKeyAsync(subjectId, version)
    │     └── Returns Left(crypto.subject_forgotten)
    ├── IForgottenSubjectHandler handles the error
    └── Replace field value with AnonymizedPlaceholder ("[REDACTED]")
```

### DSR Integration Flow

```
IDataErasureService.EraseSubjectDataAsync("user-123", scope)
  │
  ├── MartenEventPersonalDataLocator.LocateAllDataAsync("user-123")
  │     └── Scans Marten event stream for [CryptoShredded] properties
  │
  ├── CryptoShredErasureStrategy.EraseFieldAsync(location)
  │     └── ISubjectKeyProvider.DeleteSubjectKeysAsync(subjectId)
  │
  └── Returns ErasureResult with per-field status
```

---

## Quick Start

### 1. Install Packages

```bash
dotnet add package Encina.Security.Encryption
dotnet add package Encina.Marten.GDPR
```

### 2. Register Services

```csharp
// Required: encryption infrastructure
services.AddEncinaEncryption();

// Crypto-shredding for Marten
services.AddEncinaMartenGdpr(options =>
{
    options.UsePostgreSqlKeyStore = true;  // Production
    options.AddHealthCheck = true;
    options.AssembliesToScan.Add(typeof(Program).Assembly);
});
```

### 3. Decorate Event Properties

```csharp
public sealed record UserRegisteredEvent
{
    public string UserId { get; init; } = string.Empty;

    [PersonalData(Category = PersonalDataCategory.Contact, Erasable = true)]
    [CryptoShredded(SubjectIdProperty = nameof(UserId))]
    public string Email { get; init; } = string.Empty;

    [PersonalData(Category = PersonalDataCategory.Identity, Erasable = true)]
    [CryptoShredded(SubjectIdProperty = nameof(UserId))]
    public string FullName { get; init; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; init; }
}
```

### 4. Use Normally — Encryption is Transparent

```csharp
// Write: Email and FullName are encrypted before storage
session.Events.Append(streamId, new UserRegisteredEvent
{
    UserId = "user-123",
    Email = "alice@example.com",
    FullName = "Alice Smith",
    OccurredAtUtc = DateTimeOffset.UtcNow
});
await session.SaveChangesAsync();

// Read: Email and FullName are decrypted automatically
var events = await session.Events.FetchStreamAsync(streamId);
var registered = events[0].Data as UserRegisteredEvent;
// registered.Email == "alice@example.com"
```

---

## The `[CryptoShredded]` Attribute

### Requirements

| Requirement | Description |
|-------------|-------------|
| **`[PersonalData]` co-located** | Must be on the same property (governs DSR participation) |
| **`SubjectIdProperty`** | Must reference a readable `string` property on the same type |
| **Property type** | Must be `string` (only strings are encrypted) |
| **Use `nameof()`** | Compile-time safety for `SubjectIdProperty` |

### Validation

At startup (when `AutoRegisterFromAttributes = true`), the auto-registration hosted service validates:
1. `[CryptoShredded]` has co-located `[PersonalData]`
2. `SubjectIdProperty` references a valid, readable property
3. The property type is `string`

Misconfigured properties are excluded with a warning log.

---

## Key Management

### Key Lifecycle

| Operation | Method | Description |
|-----------|--------|-------------|
| **Create** | `GetOrCreateSubjectKeyAsync` | Creates AES-256 key on first use (idempotent) |
| **Retrieve** | `GetSubjectKeyAsync` | Gets key material for a specific version |
| **Rotate** | `RotateSubjectKeyAsync` | Creates new key version; old versions remain |
| **Delete** | `DeleteSubjectKeysAsync` | Deletes ALL versions (crypto-shredding) |
| **Query** | `GetSubjectInfoAsync` | Returns `SubjectEncryptionInfo` with status and version history |
| **Check** | `IsSubjectForgottenAsync` | Checks if subject has been forgotten |

### Key Storage

| Provider | Registration | Persistence |
|----------|-------------|-------------|
| `InMemorySubjectKeyProvider` | `UsePostgreSqlKeyStore = false` (default) | Process lifetime |
| `PostgreSqlSubjectKeyProvider` | `UsePostgreSqlKeyStore = true` | Marten document store |

---

## Forgetting a Subject

```csharp
var keyProvider = serviceProvider.GetRequiredService<ISubjectKeyProvider>();

// Delete all encryption keys
var result = await keyProvider.DeleteSubjectKeysAsync("user-123");

result.Match(
    Right: r => Console.WriteLine($"Subject forgotten: {r.KeysDeleted} keys deleted"),
    Left:  e => Console.WriteLine($"Error: {e.Message}"));

// Verify
var isForgotten = await keyProvider.IsSubjectForgottenAsync("user-123");
// isForgotten == Right(true)
```

After forgetting:
- All encrypted PII for the subject becomes permanently unreadable
- Deserialization returns the `AnonymizedPlaceholder` value (default: `[REDACTED]`)
- `SubjectForgottenEvent` is published for audit trail

---

## Key Rotation

```csharp
var result = await keyProvider.RotateSubjectKeyAsync("user-123");

result.Match(
    Right: r => Console.WriteLine($"Rotated: v{r.PreviousVersion} → v{r.NewVersion}"),
    Left:  e => Console.WriteLine($"Error: {e.Message}"));
```

Key rotation behavior:
- Creates a new key version (monotonically increasing)
- Previous key transitions to `Rotated` status
- **New events** are encrypted with the latest version
- **Existing events** are decrypted with the version stored in their `kid` field

---

## DSR Integration

When `Encina.Compliance.DataSubjectRights` is registered, crypto-shredding integrates automatically:

```csharp
services.AddEncinaEncryption();
services.AddEncinaDataSubjectRights();
services.AddEncinaMartenGdpr(options =>
{
    options.UsePostgreSqlKeyStore = true;
});
```

- **`CryptoShredErasureStrategy`**: Registered as `IDataErasureStrategy` — triggers key deletion during erasure workflows
- **`MartenEventPersonalDataLocator`**: Registered as `IPersonalDataLocator` — discovers PII locations in the Marten event stream

---

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `UsePostgreSqlKeyStore` | `bool` | `false` | Use PostgreSQL-backed key storage |
| `AnonymizedPlaceholder` | `string` | `"[REDACTED]"` | Placeholder for forgotten data |
| `KeyRotationDays` | `int` | `90` | Recommended rotation interval (informational) |
| `AutoRegisterFromAttributes` | `bool` | `true` | Validate `[CryptoShredded]` at startup |
| `AddHealthCheck` | `bool` | `false` | Register health check |
| `PublishEvents` | `bool` | `true` | Publish domain events for key lifecycle |
| `AssembliesToScan` | `List<Assembly>` | `[]` | Assemblies for auto-registration |

---

## Projection Handling

The serializer handles forgotten subjects automatically. Projections receive the `AnonymizedPlaceholder` value:

```csharp
public class UserProjection : SingleStreamProjection<UserView>
{
    public void Apply(UserRegisteredEvent e, UserView view)
    {
        view.UserId = e.UserId;
        view.Email = e.Email;     // "[REDACTED]" if subject forgotten
        view.FullName = e.FullName; // "[REDACTED]" if subject forgotten
    }
}
```

For projections that need explicit forgotten-subject handling:

```csharp
var isForgotten = await keyProvider.IsSubjectForgottenAsync(e.UserId);
isForgotten.IfRight(forgotten =>
{
    if (forgotten)
    {
        view.DisplayName = "Deleted User";
    }
});
```

---

## Observability

### Tracing

`ActivitySource`: `Encina.Marten.GDPR`

| Activity | Kind | Tags |
|----------|------|------|
| `CryptoShredding.Encrypt` | Internal | `crypto.event_type`, `crypto.outcome` |
| `CryptoShredding.Decrypt` | Internal | `crypto.event_type`, `crypto.outcome` |
| `CryptoShredding.Forget` | Internal | `crypto.subject_id`, `crypto.outcome` |
| `CryptoShredding.KeyRotation` | Internal | `crypto.subject_id`, `crypto.outcome` |
| `CryptoShredding.Erasure` | Internal | `crypto.subject_id`, `crypto.outcome` |

### Metrics

`Meter`: `Encina.Marten.GDPR`

| Metric | Type | Unit | Description |
|--------|------|------|-------------|
| `crypto.encryption.total` | Counter | — | Total PII field encryptions |
| `crypto.decryption.total` | Counter | — | Total PII field decryptions |
| `crypto.encryption.failed` | Counter | — | Encryption failures |
| `crypto.decryption.failed` | Counter | — | Decryption failures |
| `crypto.forgotten_access.total` | Counter | — | Decrypt attempts on forgotten subjects |
| `crypto.key_rotation.total` | Counter | — | Subject key rotations |
| `crypto.forget.total` | Counter | — | Subject forget operations |
| `crypto.encryption.duration` | Histogram | ms | Encryption duration |
| `crypto.decryption.duration` | Histogram | ms | Decryption duration |
| `crypto.forget.duration` | Histogram | ms | Forget operation duration |

### Structured Logging

Zero-allocation logging via `LoggerMessage.Define` for all operations.

---

## Health Check

When `AddHealthCheck = true`, the `encina-crypto-shredding` health check verifies:

| Check | Unhealthy If |
|-------|-------------|
| Encryption services | `IFieldEncryptor` not registered |
| Key provider | `ISubjectKeyProvider` not accessible |
| Options | Configuration validation fails |
| Auto-registration | No event types discovered (when enabled) |

Tags: `encina`, `crypto-shredding`, `ready`

---

## Error Handling

All errors follow the Railway Oriented Programming pattern (`Either<EncinaError, T>`):

| Error Code | Meaning |
|------------|---------|
| `crypto.subject_forgotten` | Subject has been cryptographically forgotten |
| `crypto.encryption_failed` | PII field encryption failed |
| `crypto.decryption_failed` | PII field decryption failed |
| `crypto.key_rotation_failed` | Key rotation failed |
| `crypto.key_store_error` | Key store infrastructure error |
| `crypto.invalid_subject_id` | Invalid or empty subject ID |
| `crypto.key_already_exists` | Active key exists (use rotation) |
| `crypto.serialization_error` | Serialization/deserialization error |
| `crypto.attribute_misconfigured` | `[CryptoShredded]` attribute misconfigured |

---

## Best Practices

1. **Use `nameof()` for `SubjectIdProperty`** — Compile-time safety prevents misconfiguration
2. **Always co-locate `[PersonalData]` with `[CryptoShredded]`** — Required for validation and DSR integration
3. **Use PostgreSQL key store in production** — `InMemorySubjectKeyProvider` loses keys on restart
4. **Rotate keys periodically** — Configure `KeyRotationDays` and implement a rotation schedule
5. **Enable auto-registration** — Catches misconfigured attributes at startup
6. **Enable health check in production** — Monitor encryption infrastructure health
7. **Keep non-PII fields unencrypted** — Crypto-shredding only applies to `[CryptoShredded]` properties
8. **Handle forgotten subjects in projections** — Check for `[REDACTED]` or use `IsSubjectForgottenAsync`

---

## Testing

The package includes tests across 6 test projects:

| Project | Coverage |
|---------|----------|
| UnitTests | Serializer, property cache, errors, options, DI registration |
| GuardTests | Null checks on all public methods |
| PropertyTests | FsCheck invariants for key provider and serializer |
| ContractTests | `ISubjectKeyProvider` contract verification |
| IntegrationTests | Full lifecycle with PostgreSQL (serialize → forget → verify redaction) |
| BenchmarkTests | Serializer overhead comparison (plain vs crypto-shredded) |

---

## FAQ

### Does crypto-shredding modify the event store?

No. Encrypted ciphertext remains in the event store. Only the encryption keys are deleted, making the ciphertext permanently unreadable.

### What happens to projections after a subject is forgotten?

Projections that rebuild from the event stream will receive `[REDACTED]` (or your configured placeholder) for all crypto-shredded fields of forgotten subjects.

### Can I use crypto-shredding with existing events?

Crypto-shredding applies to events written after enabling it. Existing plaintext events are not retroactively encrypted. Consider a migration strategy for pre-existing PII.

### What encryption algorithm is used?

AES-256-GCM via `Encina.Security.Encryption`. The same `IFieldEncryptor` used throughout the Encina security infrastructure.

### How do I test crypto-shredding?

Use `InMemorySubjectKeyProvider` (the default) for unit and integration tests. It provides the same API as the production PostgreSQL provider without requiring a database.

### What if the key provider is unavailable during serialization?

The serializer logs the error and delegates to the inner serializer without encryption. This ensures event persistence is never blocked by key provider failures.

---

## Related

- [GDPR Compliance](gdpr-compliance.md) — Processing activities and RoPA at the pipeline level
- [Security Authorization](security-authorization.md) — Transport-agnostic security
- [Encina.Marten.GDPR README](../../src/Encina.Marten.GDPR/README.md) — Package quick reference
