# Encina.Marten.GDPR

[![NuGet](https://img.shields.io/nuget/v/Encina.Marten.GDPR.svg)](https://www.nuget.org/packages/Encina.Marten.GDPR/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](../../LICENSE)

Crypto-shredding for GDPR compliance in Marten event-sourced systems. Encrypts PII fields with per-subject keys and enables GDPR Article 17 "Right to be Forgotten" by deleting encryption keys, rendering data permanently unreadable without modifying event history.

## Features

- **`[CryptoShredded]` Attribute** — Declarative PII marking on domain event properties with subject binding
- **Transparent Serializer Interception** — Automatic encrypt on save, decrypt on load via Marten `ISerializer` wrapping
- **Reuses `IFieldEncryptor` (AES-256-GCM)** — Zero crypto code duplication from `Encina.Security.Encryption`
- **Per-Subject Key Management** — InMemory (testing) and PostgreSQL (production) key providers
- **Key Rotation** — Forward-only encryption with versioned keys; old events decrypt with original key version
- **Right to be Forgotten** — Delete all subject keys to crypto-shred PII permanently
- **DSR Integration** — `CryptoShredErasureStrategy` plugs into `Encina.Compliance.DataSubjectRights` erasure workflow
- **PII Discovery** — `MartenEventPersonalDataLocator` discovers PII in Marten event streams
- **Configurable Placeholder** — Forgotten data replaced with `[REDACTED]` (customizable)
- **Auto-Registration** — Scan assemblies at startup to validate `[CryptoShredded]` configurations
- **Full Observability** — OpenTelemetry tracing, structured logging, metrics
- **Health Check** — Verifies encryption services, key provider, and configuration
- **Railway Oriented Programming** — All operations return `Either<EncinaError, T>`, no exceptions
- **.NET 10 Compatible** — Built with latest C# features

## Installation

```bash
dotnet add package Encina.Marten.GDPR
```

## Prerequisites

This package requires `Encina.Security.Encryption` to be configured first:

```csharp
// Required: provides IFieldEncryptor (AES-256-GCM) and IKeyProvider
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
- A `SubjectIdProperty` pointing to the sibling property containing the data subject's ID

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
    Left:  e => Console.WriteLine($"Error: {e.Message}"));

// After forgetting: Email fields show "[REDACTED]" instead of encrypted data
```

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `UsePostgreSqlKeyStore` | `bool` | `false` | Use PostgreSQL-backed key storage (required for production) |
| `AnonymizedPlaceholder` | `string` | `"[REDACTED]"` | Placeholder for forgotten subjects' PII |
| `KeyRotationDays` | `int` | `90` | Recommended key rotation interval (informational) |
| `AutoRegisterFromAttributes` | `bool` | `true` | Scan assemblies at startup to validate configurations |
| `AddHealthCheck` | `bool` | `false` | Register `encina-crypto-shredding` health check |
| `PublishEvents` | `bool` | `true` | Publish domain events for key lifecycle operations |
| `AssembliesToScan` | `List<Assembly>` | `[]` | Assemblies for auto-registration scanning |

## Key Provider Selection

| Provider | Scope | Persistence | Use Case |
|----------|-------|-------------|----------|
| `InMemorySubjectKeyProvider` | Singleton | Process lifetime | Testing, development |
| `PostgreSqlSubjectKeyProvider` | Scoped | Marten document store | Production |

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
    Left:  e => Console.WriteLine($"Error: {e.Message}"));
```

After rotation:
- **New events** are encrypted with the latest key version
- **Existing events** are decrypted with the key version stored in their `kid` field
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
var erasureService = serviceProvider.GetRequiredService<IDataErasureService>();
var scope = new ErasureScope { Reason = ErasureReason.ConsentWithdrawn };
await erasureService.EraseSubjectDataAsync("user-123", scope);
```

`MartenEventPersonalDataLocator` discovers all PII locations in the Marten event stream for the specified subject.

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

The serializer automatically substitutes the `AnonymizedPlaceholder` during deserialization when a subject's keys have been deleted.

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

| Metric | Type | Description |
|--------|------|-------------|
| `crypto.encryption.total` | Counter | Total PII field encryptions |
| `crypto.decryption.total` | Counter | Total PII field decryptions |
| `crypto.encryption.failed` | Counter | Encryption failures |
| `crypto.decryption.failed` | Counter | Decryption failures |
| `crypto.forgotten_access.total` | Counter | Decrypt attempts on forgotten subjects |
| `crypto.key_rotation.total` | Counter | Subject key rotations |
| `crypto.forget.total` | Counter | Subject forget (crypto-shred) operations |
| `crypto.encryption.duration` | Histogram (ms) | Encryption duration |
| `crypto.decryption.duration` | Histogram (ms) | Decryption duration |
| `crypto.forget.duration` | Histogram (ms) | Forget operation duration |

### Health Check

When `AddHealthCheck = true`, the `encina-crypto-shredding` health check verifies:
- Encryption services (`IFieldEncryptor`) are registered
- Key provider is accessible
- Options are valid
- Auto-registration has discovered event types (if enabled)

## Custom Implementations

Register custom implementations before `AddEncinaMartenGdpr()` to override defaults (TryAdd semantics):

```csharp
// Custom key provider
services.AddSingleton<ISubjectKeyProvider, MyVaultKeyProvider>();

// Custom forgotten subject handler
services.AddSingleton<IForgottenSubjectHandler, CustomForgottenSubjectHandler>();

// Custom erasure strategy
services.AddScoped<IDataErasureStrategy, CustomErasureStrategy>();

services.AddEncinaMartenGdpr(); // Won't override your registrations
```

## Domain Events

When `PublishEvents = true` (default), the following events are published:

| Event | Trigger |
|-------|---------|
| `SubjectForgottenEvent` | After successful key deletion |
| `SubjectKeyRotatedEvent` | After successful key rotation |
| `PiiEncryptionFailedEvent` | When PII encryption fails during serialization |

## Related Packages

| Package | Description |
|---------|-------------|
| `Encina.Marten` | Core Marten event sourcing integration |
| `Encina.Security.Encryption` | Field-level encryption with AES-256-GCM (prerequisite) |
| `Encina.Compliance.DataSubjectRights` | DSR workflows, erasure strategies, PII discovery (optional) |
| `Encina.Compliance.GDPR` | Processing activity tracking, RoPA, lawful basis validation |

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.
