# Implementation Plan: `Encina.Messaging.Encryption` — Message Encryption & Security

> **Issue**: [#129](https://github.com/dlrivada/Encina/issues/129)
> **Type**: Feature
> **Complexity**: High (7 phases, 4 new packages, ~55 files)
> **Estimated Scope**: ~3,000-4,000 lines of production code + ~2,000-3,000 lines of tests

---

## Summary

Implement **transparent message encryption/decryption** for Outbox/Inbox message payloads, enabling regulatory compliance (GDPR, HIPAA, PCI-DSS) by encrypting the serialized `Content` field before persistence. This complements the existing `Encina.Security.Encryption` field-level encryption (which operates on request/response properties in the CQRS pipeline) by adding **payload-level encryption** at the messaging storage layer.

The feature introduces:

1. **`Encina.Messaging.Encryption`** — Core abstractions for message payload encryption (`IMessageEncryptionProvider`, `[EncryptedMessage]` attribute, `MessageEncryptionPipelineBehavior`)
2. **`Encina.Messaging.Encryption.AzureKeyVault`** — Azure Key Vault KMS integration for message encryption keys
3. **`Encina.Messaging.Encryption.AwsKms`** — AWS KMS integration for message encryption keys
4. **`Encina.Messaging.Encryption.DataProtection`** — ASP.NET Core Data Protection integration (simplest option for single-deployment scenarios)

**Provider category**: None (provider-independent). Message encryption wraps the serialized `Content` string *before* it reaches any database provider's `IOutboxStore.AddAsync()`, so all 13 database providers benefit automatically without per-provider implementations.

**Affected packages**: `Encina.Messaging` (minor: new hook in `OutboxOrchestrator` / `InboxOrchestrator`), plus 4 new satellite packages.

### Key Architectural Insight

Encina already has **field-level encryption** (`Encina.Security.Encryption`) that encrypts individual properties decorated with `[Encrypt]` before/after handler execution. Issue #129 requests a *different* layer: encrypting the **entire serialized message payload** (`OutboxMessage.Content`) at rest in the database and in transit through message brokers. These two layers are complementary:

| Layer | Package | Scope | When |
|-------|---------|-------|------|
| **Field-level** | `Encina.Security.Encryption` | Individual properties with `[Encrypt]` | Before/after handler execution |
| **Payload-level** | `Encina.Messaging.Encryption` (this feature) | Entire `OutboxMessage.Content` / `InboxMessage.Response` | Before/after database persistence |

---

## Design Choices

<details>
<summary><strong>1. Package Placement — New <code>Encina.Messaging.Encryption</code> package family</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New `Encina.Messaging.Encryption` package** | Clean separation, own observability, complements existing `Encina.Security.Encryption`, KMS providers as satellite packages | New NuGet packages to maintain |
| **B) Extend `Encina.Security.Encryption`** | Single encryption package | Mixes field-level and payload-level concerns, creates circular dependency with `Encina.Messaging` |
| **C) Add encryption directly into `Encina.Messaging`** | No new packages | Forces KMS dependencies on all messaging users, violates pay-for-what-you-use |
| **D) Create `Encina.Encryption` as proposed in the issue** | Matches issue naming | Conflicts with existing `Encina.Security.Encryption`, unclear relationship |

### Chosen Option: **A — New `Encina.Messaging.Encryption` package family**

### Rationale

- The issue proposes `Encina.Encryption.*` but that name conflicts with the existing `Encina.Security.Encryption` package (field-level encryption, already shipped)
- `Encina.Messaging.Encryption` clearly scopes this to message payload encryption at the messaging layer
- References `Encina.Messaging` for `IOutboxMessage`, `OutboxOptions`, etc.
- References `Encina.Security.Encryption` for `IKeyProvider`, `IFieldEncryptor`, `EncryptedValue` (reuse crypto primitives)
- KMS satellite packages (`AzureKeyVault`, `AwsKms`, `DataProtection`) follow the same pattern as `Encina.Security.Secrets.*`
- Users who don't need payload encryption have zero overhead — pay-for-what-you-use

</details>

<details>
<summary><strong>2. Encryption Integration Point — Orchestrator-level decorator with <code>IMessageSerializer</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Introduce `IMessageSerializer` abstraction, wrap with encryption** | Clean SRP, testable, interceptable, future-proof for other serialization concerns | New abstraction, minor refactor to `OutboxOrchestrator` |
| **B) Pipeline behavior that encrypts before outbox persistence** | Uses existing pipeline pattern | Too early — notification isn't serialized yet at behavior stage |
| **C) Modify `OutboxOrchestrator` directly with conditional encryption** | Simple, minimal changes | Violates OCP, hard to test, no extensibility |
| **D) Store-level encryption (encrypt in each IOutboxStore implementation)** | Transparent to orchestrator | Must implement in all 13 providers, violates DRY |

### Chosen Option: **A — `IMessageSerializer` abstraction with encryption decorator**

### Rationale

- Currently `OutboxOrchestrator` uses hardcoded `JsonSerializer.Serialize()` — extracting this into `IMessageSerializer` is a natural improvement
- `EncryptingMessageSerializer` decorates the default `JsonMessageSerializer`:
  1. Serializes to JSON (delegates to inner serializer)
  2. Encrypts the JSON string using `IMessageEncryptionProvider`
  3. Returns encrypted payload with metadata prefix (e.g., `ENC:v1:{keyId}:{algorithm}:{base64}`)
- Decryption is the reverse: detect `ENC:` prefix → decrypt → deserialize
- All 13 database providers benefit automatically (encryption happens before `IOutboxStore.AddAsync`)
- The `IMessageSerializer` abstraction also enables future concerns: compression, schema versioning, custom serializers

</details>

<details>
<summary><strong>3. Message Encryption Provider Model — Reuse existing <code>IKeyProvider</code> + <code>IFieldEncryptor</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reuse `IKeyProvider` + `IFieldEncryptor` from `Encina.Security.Encryption`** | DRY, consistent crypto, already supports key rotation, AES-256-GCM | Couples to Security.Encryption package |
| **B) New `IMessageEncryptionProvider` with full key management** | Self-contained, matches issue spec exactly | Duplicates crypto code, two key management systems |
| **C) Delegate to cloud KMS directly (envelope encryption)** | Stronger security (keys never leave KMS) | Cloud-specific, no local/testing option |

### Chosen Option: **A — Reuse existing crypto primitives, new `IMessageEncryptionProvider` facade**

### Rationale

- `IMessageEncryptionProvider` acts as a **facade** over `IFieldEncryptor` + `IKeyProvider`, adapted for byte-stream encryption (not field-by-field)
- Preserves the issue's proposed `IMessageEncryptionProvider` interface while avoiding crypto duplication
- `DefaultMessageEncryptionProvider` delegates to `IFieldEncryptor.EncryptBytesAsync()` / `DecryptBytesAsync()`
- KMS satellite packages (`AzureKeyVault`, `AwsKms`) implement `IKeyProvider` adapters that delegate to their respective SDKs for key management
- `DataProtection` satellite uses ASP.NET Core's `IDataProtector` — different mechanism, implements `IMessageEncryptionProvider` directly
- Local/testing: `InMemoryKeyProvider` (already exists) provides development keys

</details>

<details>
<summary><strong>4. Attribute Design — <code>[EncryptedMessage]</code> for message types</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `[EncryptedMessage]` attribute on notification/command types** | Declarative, opt-in per message type, consistent with `[Encrypt]` pattern | Attribute discovery overhead (mitigated by caching) |
| **B) Global configuration only (`config.Encryption.Enabled = true`)** | All-or-nothing, simple | Can't selectively encrypt — some messages may not need encryption |
| **C) Both attribute + global config** | Maximum flexibility | Complexity in resolution (which wins?) |

### Chosen Option: **C — Both attribute and global config with clear precedence**

### Rationale

- `[EncryptedMessage]` on a notification/command type opts that specific message type into encryption
- `MessageEncryptionOptions.EncryptAllMessages = true` enables encryption globally (every outbox message)
- Precedence: attribute overrides global config. `[EncryptedMessage(Enabled = false)]` can disable encryption for a specific type even when global is on
- `[EncryptedMessage(KeyId = "payment-key")]` allows per-type key selection
- `[EncryptedMessage(UseTenantKey = true)]` enables multi-tenant key isolation
- Attribute cache uses `ConcurrentDictionary<Type, EncryptedMessageInfo?>` (same pattern as `EncryptedPropertyCache`)

</details>

<details>
<summary><strong>5. Encryption Audit Logging — Extend existing audit infrastructure</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New `IEncryptionAuditLogger` interface** | Dedicated audit, matches issue spec | New interface, possible overlap with existing audit |
| **B) Emit domain notifications (`MessageEncryptedNotification`)** | Uses existing notification pipeline, decoupled | Overhead for high-throughput encryption |
| **C) Structured logging only (via `[LoggerMessage]`)** | Zero overhead, uses existing logging infrastructure | Less structured than dedicated audit |

### Chosen Option: **C — Structured logging with optional notification for sensitive operations**

### Rationale

- Encryption/decryption of outbox messages is a high-frequency operation — dedicated audit store would add significant latency
- `[LoggerMessage]` source generator provides zero-allocation structured logging with EventIds for filtering
- Key rotation and decryption failures emit domain notifications (`KeyRotatedNotification`, `DecryptionFailedNotification`) for compliance alerting
- OpenTelemetry metrics capture counts, latency, and key usage patterns
- Compliance teams can filter logs by EventId range (2400-2499) for encryption-specific audit trails
- Future: `IEncryptionAuditLogger` can be added later if customers require dedicated audit persistence

</details>

<details>
<summary><strong>6. Key Rotation Strategy — Versioned keys with forward compatibility</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Versioned key IDs in encrypted payload header** | Decrypt with correct key version, no re-encryption needed | Slightly larger payload header |
| **B) Re-encrypt all messages on key rotation** | All messages use latest key | Impractical for large outbox tables, requires downtime |
| **C) Key rotation via KMS provider (transparent)** | KMS handles versioning | Only works with cloud KMS, not local keys |

### Chosen Option: **A — Versioned key IDs embedded in payload header**

### Rationale

- Encrypted payload format: `ENC:v1:{keyId}:{algorithm}:{base64(nonce)}:{base64(tag)}:{base64(ciphertext)}`
- The `keyId` identifies which key version was used for encryption
- On decryption, `IKeyProvider.GetKeyAsync(keyId)` retrieves the specific key version
- Key rotation: new messages use the latest key, old messages remain decryptable with their original key
- `IKeyProvider.RotateKeyAsync()` creates a new key version without invalidating old ones
- No re-encryption needed — messages naturally age out as they're processed from the outbox
- Compatible with both local keys and cloud KMS versioning (Azure Key Vault versions, AWS KMS key aliases)

</details>

<details>
<summary><strong>7. Multi-Tenant Key Isolation — Tenant-aware key resolution</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Tenant-specific key IDs via `ITenantKeyResolver`** | Clean isolation, composable with `Encina.Tenancy` | Requires tenant context in encryption path |
| **B) Single key for all tenants with tenant ID in AAD** | Simpler key management | Single key compromise affects all tenants |
| **C) Per-tenant encryption providers** | Maximum isolation | Complex DI, provider explosion |

### Chosen Option: **A — `ITenantKeyResolver` for tenant-specific key IDs**

### Rationale

- `ITenantKeyResolver` interface: `string ResolveKeyId(string tenantId)` — maps tenant to key ID
- Default implementation: `DefaultTenantKeyResolver` uses pattern `tenant-{tenantId}-key`
- Integrates with `Encina.Tenancy` via `ITenantContext` (if registered)
- If `ITenantContext` is not registered, falls back to default key ID
- `[EncryptedMessage(UseTenantKey = true)]` activates tenant-aware key resolution
- Each tenant's messages encrypted with isolated keys — key compromise affects only one tenant
- Compatible with cloud KMS: each tenant key can map to a different KMS key/alias

</details>

---

## Implementation Phases

### Phase 1: Core Abstractions & Models

> **Goal**: Define the public API surface — interfaces, records, attributes, and error codes for `Encina.Messaging.Encryption`.

<details>
<summary><strong>Tasks</strong></summary>

#### New project: `src/Encina.Messaging.Encryption/`

1. **Create project file** `Encina.Messaging.Encryption.csproj`
   - Target: `net10.0`
   - Dependencies: `Encina.Messaging`, `Encina.Security.Encryption`, `LanguageExt.Core`, `Microsoft.Extensions.Logging.Abstractions`, `Microsoft.Extensions.Options`, `Microsoft.Extensions.DependencyInjection.Abstractions`
   - Enable nullable, implicit usings, XML doc

2. **Core interfaces** (`Abstractions/` folder):
   - `IMessageEncryptionProvider` — facade for message-level encryption
     ```csharp
     public interface IMessageEncryptionProvider
     {
         ValueTask<Either<EncinaError, EncryptedPayload>> EncryptAsync(
             byte[] plaintext, MessageEncryptionContext context, CancellationToken ct = default);
         ValueTask<Either<EncinaError, byte[]>> DecryptAsync(
             EncryptedPayload ciphertext, MessageEncryptionContext context, CancellationToken ct = default);
     }
     ```
   - `IMessageSerializer` — serialization abstraction for outbox/inbox messages
     ```csharp
     public interface IMessageSerializer
     {
         string Serialize<T>(T message);
         T? Deserialize<T>(string content);
         object? Deserialize(string content, Type type);
     }
     ```
   - `ITenantKeyResolver` — tenant-to-key-ID mapping
     ```csharp
     public interface ITenantKeyResolver
     {
         string ResolveKeyId(string tenantId);
     }
     ```

3. **Data types** (`Model/` folder):
   - `EncryptedPayload` — sealed record: `Ciphertext (ImmutableArray<byte>)`, `KeyId (string)`, `Algorithm (string)`, `Nonce (ImmutableArray<byte>)`, `Tag (ImmutableArray<byte>)`, `Version (int, default 1)`
   - `MessageEncryptionContext` — sealed record: `KeyId (string?)`, `TenantId (string?)`, `MessageType (string?)`, `MessageId (Guid?)`, `AssociatedData (ImmutableArray<byte>)`

4. **Attributes** (`Attributes/` folder):
   - `EncryptedMessageAttribute` — `[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]`
     - Properties: `Enabled (bool, default true)`, `KeyId (string?)`, `UseTenantKey (bool, default false)`
   - `EncryptedFieldAttribute` — `[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]`
     - Marker for selective field encryption within a message (delegates to existing `[Encrypt]`)

5. **Error codes** (`MessageEncryptionErrors.cs`):
   - Error code prefix: `msg_encryption.`
   - Codes: `msg_encryption.encryption_failed`, `msg_encryption.decryption_failed`, `msg_encryption.key_not_found`, `msg_encryption.invalid_payload`, `msg_encryption.unsupported_version`, `msg_encryption.tenant_key_resolution_failed`, `msg_encryption.serialization_failed`, `msg_encryption.deserialization_failed`, `msg_encryption.provider_unavailable`
   - Follow `EncryptionErrors.cs` pattern: `public static class MessageEncryptionErrors` with factory methods

6. **`PublicAPI.Unshipped.txt`** — Add all public types

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of Encina.Messaging.Encryption (Issue #129).

CONTEXT:
- Encina is a .NET 10 / C# 14 library using Railway Oriented Programming (Either<EncinaError, T>)
- This is a NEW project: src/Encina.Messaging.Encryption/
- Encina already has field-level encryption in src/Encina.Security.Encryption/ — this feature adds PAYLOAD-level encryption for outbox/inbox messages
- Reference existing patterns in src/Encina.Security.Encryption/Abstractions/ for interface style
- All domain models are sealed records with XML documentation
- Use LanguageExt for Option<T>, Either<L, R>, Unit
- Use ImmutableArray<byte> for byte buffers (same as EncryptedValue in Security.Encryption)

TASK:
Create the project file and all abstractions, models, attributes, and error codes listed in Phase 1 Tasks.

KEY RULES:
- Target net10.0, enable nullable, enable implicit usings
- All public types need XML documentation with <summary>, <remarks>, and compliance references
- IMessageEncryptionProvider mirrors the issue's proposed interface but adapted for Encina's ROP pattern
- IMessageSerializer is a NEW abstraction — currently OutboxOrchestrator uses hardcoded JsonSerializer
- EncryptedPayload uses ImmutableArray<byte> for Ciphertext, Nonce, Tag (consistent with EncryptedValue)
- MessageEncryptionContext includes TenantId for multi-tenant key isolation
- EncryptedMessageAttribute targets classes (notification/command types), not properties
- Error codes follow msg_encryption.* prefix to avoid collision with encryption.* (field-level)
- Add PublicAPI.Unshipped.txt tracking all public symbols

REFERENCE FILES:
- src/Encina.Security.Encryption/Abstractions/IFieldEncryptor.cs (interface pattern with Either)
- src/Encina.Security.Encryption/Abstractions/IKeyProvider.cs (key management interface)
- src/Encina.Security.Encryption/EncryptionErrors.cs (error factory pattern)
- src/Encina.Security.Encryption/EncryptedValue.cs (ImmutableArray<byte> pattern)
- src/Encina.Security.Encryption/Attributes/EncryptAttribute.cs (attribute pattern)
- src/Encina.Messaging/Outbox/OutboxOrchestrator.cs (current serialization in AddAsync)
```

</details>

---

### Phase 2: Default Implementations

> **Goal**: Implement the default `JsonMessageSerializer`, `DefaultMessageEncryptionProvider`, `DefaultTenantKeyResolver`, and the `EncryptingMessageSerializer` decorator.

<details>
<summary><strong>Tasks</strong></summary>

#### In project: `src/Encina.Messaging.Encryption/`

1. **`JsonMessageSerializer`** (`Serialization/JsonMessageSerializer.cs`)
   - Implements `IMessageSerializer`
   - Uses `System.Text.Json` with `JsonSerializerOptions` (camelCase, no indentation — same as current `OutboxOrchestrator`)
   - Thread-safe, singleton-compatible
   - XML doc on all public methods

2. **`EncryptingMessageSerializer`** (`Serialization/EncryptingMessageSerializer.cs`)
   - Implements `IMessageSerializer` as a decorator pattern
   - Constructor: `IMessageSerializer inner`, `IMessageEncryptionProvider provider`, `MessageEncryptionOptions options`, `ILogger<EncryptingMessageSerializer> logger`
   - `Serialize<T>()`:
     1. Delegates to `inner.Serialize(message)` → gets JSON string
     2. Checks if encryption is required (global config or `[EncryptedMessage]` attribute)
     3. If required: encrypt JSON bytes → format as `ENC:v1:{keyId}:{algorithm}:{base64Nonce}:{base64Tag}:{base64Ciphertext}`
     4. Return encrypted string
   - `Deserialize<T>()`:
     1. Checks if content starts with `ENC:v1:` prefix
     2. If encrypted: parse header → decrypt → get JSON string
     3. Delegates to `inner.Deserialize<T>(json)` → return typed object
   - Uses `EncryptedMessageAttributeCache` for attribute discovery

3. **`DefaultMessageEncryptionProvider`** (`DefaultMessageEncryptionProvider.cs`)
   - Implements `IMessageEncryptionProvider`
   - Constructor: `IFieldEncryptor fieldEncryptor`, `IKeyProvider keyProvider`
   - Delegates to `IFieldEncryptor.EncryptBytesAsync()` / `DecryptBytesAsync()`
   - Builds `EncryptionContext` from `MessageEncryptionContext` (maps KeyId, adds AssociatedData)
   - If `MessageEncryptionContext.KeyId` is null, uses `IKeyProvider.GetCurrentKeyIdAsync()` to get default key
   - Returns `EncryptedPayload` with algorithm, key ID, nonce, and tag

4. **`DefaultTenantKeyResolver`** (`DefaultTenantKeyResolver.cs`)
   - Implements `ITenantKeyResolver`
   - Pattern-based: `tenantId => $"tenant-{tenantId}-key"` (configurable via `MessageEncryptionOptions.TenantKeyPattern`)
   - Thread-safe, singleton-compatible

5. **`EncryptedMessageAttributeCache`** (`EncryptedMessageAttributeCache.cs`)
   - Static `ConcurrentDictionary<Type, EncryptedMessageInfo?>` cache
   - `GetEncryptionInfo(Type messageType)` — discovers `[EncryptedMessage]` attribute
   - `EncryptedMessageInfo` internal record: `Enabled`, `KeyId`, `UseTenantKey`
   - Same pattern as `EncryptedPropertyCache` in `Encina.Security.Encryption`

6. **`EncryptedPayloadFormatter`** (`EncryptedPayloadFormatter.cs`)
   - Static helper for parsing/formatting the `ENC:v1:...` string format
   - `Format(EncryptedPayload payload)` → `string`
   - `TryParse(string content, out EncryptedPayload? payload)` → `bool`
   - Format: `ENC:v1:{keyId}:{algorithm}:{base64Nonce}:{base64Tag}:{base64Ciphertext}`
   - Extensible version field for future format changes

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of Encina.Messaging.Encryption (Issue #129).

CONTEXT:
- Phase 1 abstractions are already implemented in src/Encina.Messaging.Encryption/
- The goal is to provide default implementations that encrypt/decrypt outbox message payloads
- The EncryptingMessageSerializer is a DECORATOR over JsonMessageSerializer
- DefaultMessageEncryptionProvider delegates to existing IFieldEncryptor + IKeyProvider from Encina.Security.Encryption
- Encrypted payloads use a string format: ENC:v1:{keyId}:{algorithm}:{base64Nonce}:{base64Tag}:{base64Ciphertext}

TASK:
Create all implementation classes listed in Phase 2 Tasks.

KEY RULES:
- JsonMessageSerializer uses System.Text.Json with camelCase policy (same as current OutboxOrchestrator)
- EncryptingMessageSerializer checks BOTH global config (EncryptAllMessages) and [EncryptedMessage] attribute
- Attribute precedence: [EncryptedMessage(Enabled = false)] overrides global EncryptAllMessages = true
- DefaultMessageEncryptionProvider builds EncryptionContext from MessageEncryptionContext
- EncryptedPayloadFormatter.TryParse must be fault-tolerant — malformed payloads return false, never throw
- EncryptedMessageAttributeCache uses ConcurrentDictionary for thread-safe per-type caching
- All classes must be thread-safe (singleton or scoped as appropriate)
- All public methods need XML documentation

REFERENCE FILES:
- src/Encina.Security.Encryption/EncryptionOrchestrator.cs (orchestrator pattern, decorator usage)
- src/Encina.Security.Encryption/EncryptedPropertyCache.cs (ConcurrentDictionary caching pattern)
- src/Encina.Security.Encryption/Algorithms/AesGcmFieldEncryptor.cs (IFieldEncryptor impl)
- src/Encina.Messaging/Outbox/OutboxOrchestrator.cs (current JsonSerializer usage — lines 74-96)
```

</details>

---

### Phase 3: Messaging Integration & Configuration

> **Goal**: Integrate `IMessageSerializer` into `Encina.Messaging` (Outbox/Inbox orchestrators), add `MessageEncryptionOptions`, and wire up DI registration.

<details>
<summary><strong>Tasks</strong></summary>

#### Modify project: `src/Encina.Messaging/`

1. **Register `IMessageSerializer` abstraction in `Encina.Messaging`**
   - Add `IMessageSerializer` interface to `Encina.Messaging` (or reference `Encina.Messaging.Encryption`)
   - Option: define `IMessageSerializer` in `Encina.Messaging` itself so it's always available, then `Encina.Messaging.Encryption` provides the encrypting decorator
   - **Decision**: `IMessageSerializer` lives in `Encina.Messaging` (base serialization is a messaging concern). `Encina.Messaging.Encryption` provides the encrypting decorator.

2. **Modify `OutboxOrchestrator`** to use `IMessageSerializer`
   - Replace `JsonSerializer.Serialize(notification, JsonOptions)` with `_messageSerializer.Serialize(notification)`
   - Inject `IMessageSerializer` via constructor
   - Default registration: `JsonMessageSerializer` (preserves current behavior for non-encryption users)

3. **Modify `InboxOrchestrator`** to use `IMessageSerializer`
   - Replace deserialization logic with `_messageSerializer.Deserialize(content, type)`
   - Same injection pattern as OutboxOrchestrator

4. **Register default `IMessageSerializer` in `Encina.Messaging`'s DI**
   - `services.TryAddSingleton<IMessageSerializer, JsonMessageSerializer>()`
   - Users who add `Encina.Messaging.Encryption` get the encrypting decorator via decoration

#### In project: `src/Encina.Messaging.Encryption/`

5. **`MessageEncryptionOptions`** (`MessageEncryptionOptions.cs`)
   ```csharp
   public sealed class MessageEncryptionOptions
   {
       public bool Enabled { get; set; } = true;
       public bool EncryptAllMessages { get; set; } = false;
       public string? DefaultKeyId { get; set; }
       public bool UseTenantKeys { get; set; } = false;
       public string TenantKeyPattern { get; set; } = "tenant-{0}-key";
       public bool AuditDecryption { get; set; } = false;
       public bool AddHealthCheck { get; set; } = false;
       public bool EnableTracing { get; set; } = false;
       public bool EnableMetrics { get; set; } = false;
   }
   ```

6. **`ServiceCollectionExtensions`** (`ServiceCollectionExtensions.cs`)
   - `AddEncinaMessageEncryption(Action<MessageEncryptionOptions>? configure = null)`
     - Registers `MessageEncryptionOptions`
     - Registers `IMessageEncryptionProvider` → `DefaultMessageEncryptionProvider` (TryAdd)
     - Registers `ITenantKeyResolver` → `DefaultTenantKeyResolver` (TryAdd)
     - Decorates `IMessageSerializer`: replaces existing registration with `EncryptingMessageSerializer` wrapping the original
     - Registers health check if `AddHealthCheck = true`
   - `AddEncinaMessageEncryption<TProvider>(Action<MessageEncryptionOptions>? configure = null)` where `TProvider : class, IMessageEncryptionProvider`
     - Same as above but uses custom provider

7. **`MessageEncryptionHealthCheck`** (`Health/MessageEncryptionHealthCheck.cs`)
   - Verifies `IMessageEncryptionProvider` is registered
   - Performs roundtrip encrypt/decrypt with test payload `"encina-message-health-probe"`
   - Default name: `"encina-message-encryption"`
   - Tags: `["encina", "messaging", "encryption", "ready"]`
   - Uses `IServiceProvider.CreateScope()` pattern

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of Encina.Messaging.Encryption (Issue #129).

CONTEXT:
- Phase 1 (abstractions) and Phase 2 (default implementations) are done
- Now we need to integrate IMessageSerializer into the existing Encina.Messaging orchestrators
- IMessageSerializer lives in Encina.Messaging (serialization is a messaging concern)
- Encina.Messaging.Encryption provides the EncryptingMessageSerializer decorator
- The DI registration pattern uses decorator approach: AddEncinaMessageEncryption() wraps existing IMessageSerializer

TASK:
1. Add IMessageSerializer to Encina.Messaging and register JsonMessageSerializer as default
2. Modify OutboxOrchestrator and InboxOrchestrator to use IMessageSerializer instead of hardcoded JsonSerializer
3. Create MessageEncryptionOptions and ServiceCollectionExtensions in Encina.Messaging.Encryption
4. Create MessageEncryptionHealthCheck

KEY RULES:
- IMessageSerializer in Encina.Messaging — not in Encina.Messaging.Encryption (avoids forcing encryption dependency)
- JsonMessageSerializer is the DEFAULT registered by Encina.Messaging's existing DI
- AddEncinaMessageEncryption() DECORATES the existing IMessageSerializer with EncryptingMessageSerializer
- Use the TryAdd pattern for IMessageEncryptionProvider and ITenantKeyResolver
- For IMessageSerializer decoration: resolve existing registration, wrap it with EncryptingMessageSerializer
- MessageEncryptionHealthCheck follows EncryptionHealthCheck pattern (roundtrip test, scoped resolution)
- DO NOT break existing behavior — users without Encina.Messaging.Encryption get plain JSON (no encryption)

REFERENCE FILES:
- src/Encina.Messaging/Outbox/OutboxOrchestrator.cs (lines 74-96 — current serialization)
- src/Encina.Messaging/Inbox/InboxOrchestrator.cs (deserialization logic)
- src/Encina.Security.Encryption/ServiceCollectionExtensions.cs (DI registration pattern)
- src/Encina.Security.Encryption/Health/EncryptionHealthCheck.cs (health check pattern)
- src/Encina.Messaging/ServiceCollectionExtensions.cs (existing Encina.Messaging DI)
```

</details>

---

### Phase 4: KMS Provider Satellites

> **Goal**: Implement cloud KMS integrations (Azure Key Vault, AWS KMS) and ASP.NET Core Data Protection provider.

<details>
<summary><strong>Tasks</strong></summary>

#### New project: `src/Encina.Messaging.Encryption.AzureKeyVault/`

1. **Create project file** `Encina.Messaging.Encryption.AzureKeyVault.csproj`
   - Dependencies: `Encina.Messaging.Encryption`, `Azure.Security.KeyVault.Keys`
   - Target: `net10.0`

2. **`AzureKeyVaultKeyProvider`** (`AzureKeyVaultKeyProvider.cs`)
   - Implements `IKeyProvider` (from `Encina.Security.Encryption`)
   - Constructor: `KeyClient keyClient`, `AzureKeyVaultOptions options`
   - Uses Azure Key Vault's key encryption/wrapping for envelope encryption
   - `GetKeyAsync()` retrieves key bytes via Key Vault API (for symmetric keys) or wraps/unwraps data keys (for RSA/EC)
   - `RotateKeyAsync()` creates new key version in Key Vault
   - Error mapping: Azure SDK exceptions → `EncinaError`

3. **`AzureKeyVaultOptions`** (`AzureKeyVaultOptions.cs`)
   - `VaultUri (Uri)`, `KeyName (string)`, `KeyVersion (string?)`, `UseEnvelopeEncryption (bool, default true)`

4. **`ServiceCollectionExtensions`** (`ServiceCollectionExtensions.cs`)
   - `AddEncinaMessageEncryptionAzureKeyVault(Action<AzureKeyVaultOptions> configure)`
   - Registers `IKeyProvider` → `AzureKeyVaultKeyProvider`
   - Calls `AddEncinaMessageEncryption()` if not already registered

#### New project: `src/Encina.Messaging.Encryption.AwsKms/`

5. **Create project file** `Encina.Messaging.Encryption.AwsKms.csproj`
   - Dependencies: `Encina.Messaging.Encryption`, `AWSSDK.KeyManagementService`
   - Target: `net10.0`

6. **`AwsKmsKeyProvider`** (`AwsKmsKeyProvider.cs`)
   - Implements `IKeyProvider`
   - Constructor: `IAmazonKeyManagementService kmsClient`, `AwsKmsOptions options`
   - Uses AWS KMS `GenerateDataKey` / `Decrypt` for envelope encryption
   - `GetKeyAsync()` calls KMS to decrypt the data key (cached locally)
   - `RotateKeyAsync()` generates new data key, stores encrypted version
   - Error mapping: AWS SDK exceptions → `EncinaError`

7. **`AwsKmsOptions`** (`AwsKmsOptions.cs`)
   - `KeyId (string)` — AWS KMS key ARN or alias
   - `EncryptionAlgorithm (string, default "SYMMETRIC_DEFAULT")`
   - `Region (string?)`

8. **`ServiceCollectionExtensions`** (`ServiceCollectionExtensions.cs`)
   - `AddEncinaMessageEncryptionAwsKms(Action<AwsKmsOptions> configure)`
   - Registers `IKeyProvider` → `AwsKmsKeyProvider`
   - Calls `AddEncinaMessageEncryption()` if not already registered

#### New project: `src/Encina.Messaging.Encryption.DataProtection/`

9. **Create project file** `Encina.Messaging.Encryption.DataProtection.csproj`
   - Dependencies: `Encina.Messaging.Encryption`, `Microsoft.AspNetCore.DataProtection`
   - Target: `net10.0`

10. **`DataProtectionMessageEncryptionProvider`** (`DataProtectionMessageEncryptionProvider.cs`)
    - Implements `IMessageEncryptionProvider` directly (Data Protection has its own key management)
    - Constructor: `IDataProtectionProvider dataProtectionProvider`, `DataProtectionEncryptionOptions options`
    - Creates `IDataProtector` with purpose `"Encina.Messaging.Encryption"`
    - `EncryptAsync()`: `protector.Protect(plaintext)` → wrap in `EncryptedPayload`
    - `DecryptAsync()`: `protector.Unprotect(ciphertext)` → return bytes
    - Note: Data Protection handles key rotation automatically

11. **`DataProtectionEncryptionOptions`** (`DataProtectionEncryptionOptions.cs`)
    - `Purpose (string, default "Encina.Messaging.Encryption")` — Data Protection purpose string

12. **`ServiceCollectionExtensions`** (`ServiceCollectionExtensions.cs`)
    - `AddEncinaMessageEncryptionDataProtection(Action<DataProtectionEncryptionOptions>? configure = null)`
    - Registers `IMessageEncryptionProvider` → `DataProtectionMessageEncryptionProvider`
    - Calls `AddEncinaMessageEncryption()` if not already registered

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of Encina.Messaging.Encryption (Issue #129).

CONTEXT:
- Phases 1-3 are done. Core abstractions, default implementations, and DI integration are complete.
- Now we need 3 satellite packages for cloud KMS integrations:
  1. Encina.Messaging.Encryption.AzureKeyVault — Azure Key Vault key management
  2. Encina.Messaging.Encryption.AwsKms — AWS KMS key management
  3. Encina.Messaging.Encryption.DataProtection — ASP.NET Core Data Protection
- Azure and AWS satellites implement IKeyProvider (from Encina.Security.Encryption)
- DataProtection implements IMessageEncryptionProvider directly (different mechanism)

TASK:
Create all 3 satellite packages with their implementations, options, and DI registrations.

KEY RULES:
- Each satellite has its own .csproj, ServiceCollectionExtensions, options class
- Azure/AWS implement IKeyProvider — they provide keys, DefaultMessageEncryptionProvider does the actual encryption
- DataProtection implements IMessageEncryptionProvider directly — it handles both key management AND encryption
- Error mapping: cloud SDK exceptions → EncinaError (same pattern as Encina.Security.Secrets.AzureKeyVault)
- Each ServiceCollectionExtensions calls AddEncinaMessageEncryption() if not already registered
- Use TryAdd for all registrations (allows user override)
- All public types need XML documentation

REFERENCE FILES:
- src/Encina.Security.Secrets.AzureKeyVault/ (Azure SDK integration pattern)
- src/Encina.Security.Secrets.AwsSecretsManager/ (AWS SDK integration pattern)
- src/Encina.Security.Encryption/InMemoryKeyProvider.cs (IKeyProvider implementation)
- src/Encina.Security.Encryption/ServiceCollectionExtensions.cs (satellite DI pattern)
```

</details>

---

### Phase 5: Observability

> **Goal**: Add OpenTelemetry tracing, metrics, and structured logging for message encryption operations.

<details>
<summary><strong>Tasks</strong></summary>

#### In project: `src/Encina.Messaging.Encryption/`

1. **`MessageEncryptionDiagnostics`** (`Diagnostics/MessageEncryptionDiagnostics.cs`)
   - `ActivitySource`: `"Encina.Messaging.Encryption"`, version `"1.0"`
   - `Meter`: `"Encina.Messaging.Encryption"`, version `"1.0"`
   - Counters:
     - `msg_encryption.operations` (Counter<long>) — tags: `operation` (encrypt/decrypt), `outcome` (success/failure), `message_type`
     - `msg_encryption.key_operations` (Counter<long>) — tags: `operation` (get_key/rotate), `key_id`, `outcome`
   - Histograms:
     - `msg_encryption.duration` (Histogram<double>, unit: `"ms"`) — tags: `operation`, `message_type`
     - `msg_encryption.payload_size` (Histogram<long>, unit: `"By"`) — tags: `operation` (original vs encrypted)
   - Activity helpers:
     - `StartEncrypt(string messageType, string? keyId)` → `Activity?`
     - `StartDecrypt(string messageType, string? keyId)` → `Activity?`
     - `RecordSuccess(Activity?, string messageType)`
     - `RecordFailure(Activity?, string messageType, string error)`
   - Tag constants: `TagOperation`, `TagOutcome`, `TagMessageType`, `TagKeyId`, `TagAlgorithm`

2. **`MessageEncryptionLogMessages`** (`Diagnostics/MessageEncryptionLogMessages.cs`)
   - EventId range: **2400-2499** (100 events, non-colliding)
   - Messages:
     - 2400: `MessageEncrypted` (Debug) — `"Message {MessageType} encrypted with key {KeyId} using {Algorithm}"`
     - 2401: `MessageDecrypted` (Debug) — `"Message {MessageType} decrypted with key {KeyId}"`
     - 2402: `EncryptionSkipped` (Debug) — `"Encryption skipped for {MessageType} (not configured)"`
     - 2403: `EncryptionFailed` (Error) — `"Failed to encrypt message {MessageType}: {ErrorMessage}"`
     - 2404: `DecryptionFailed` (Error) — `"Failed to decrypt message {MessageType}: {ErrorMessage}"`
     - 2405: `InvalidEncryptedPayload` (Warning) — `"Invalid encrypted payload format for {MessageType}"`
     - 2406: `KeyResolutionFailed` (Error) — `"Failed to resolve encryption key {KeyId}: {ErrorMessage}"`
     - 2410: `TenantKeyResolved` (Debug) — `"Tenant {TenantId} key resolved to {KeyId}"`
     - 2411: `TenantKeyResolutionFailed` (Warning) — `"Failed to resolve key for tenant {TenantId}"`
     - 2420: `KeyRotationStarted` (Information) — `"Key rotation started for key {KeyId}"`
     - 2421: `KeyRotationCompleted` (Information) — `"Key rotation completed: new key {NewKeyId}"`
     - 2422: `KeyRotationFailed` (Error) — `"Key rotation failed for key {KeyId}: {ErrorMessage}"`
     - 2430: `HealthCheckPassed` (Debug) — `"Message encryption health check passed"`
     - 2431: `HealthCheckFailed` (Warning) — `"Message encryption health check failed: {ErrorMessage}"`
   - Use `[LoggerMessage]` source generator for zero-allocation logging

3. **Instrument `EncryptingMessageSerializer`** with diagnostics
   - Start/stop activities for encrypt/decrypt operations
   - Record counters and histograms
   - Emit log messages for all operations
   - Guard with `ActivitySource.HasListeners()` and `Meter.Enabled` for zero-cost when disabled

4. **Instrument `DefaultMessageEncryptionProvider`** with diagnostics
   - Record key resolution operations
   - Record encrypt/decrypt duration

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of Encina.Messaging.Encryption (Issue #129).

CONTEXT:
- Phases 1-4 are done. Core abstractions, implementations, DI, and KMS satellites are complete.
- Now we add OpenTelemetry observability: ActivitySource, Meter, and [LoggerMessage] structured logging.
- EventId range 2400-2499 is allocated for message encryption (non-colliding with existing ranges).

TASK:
Create MessageEncryptionDiagnostics and MessageEncryptionLogMessages, then instrument existing classes.

KEY RULES:
- ActivitySource and Meter named "Encina.Messaging.Encryption" version "1.0"
- Counters use Counter<long> with dimensional tags (operation, outcome, message_type, key_id)
- Histograms use Histogram<double> for duration (ms) and Histogram<long> for payload size (bytes)
- [LoggerMessage] source generator for zero-allocation structured logging
- EventIds 2400-2499 (15 messages defined, room for future additions)
- Guard all observability calls with HasListeners() / Enabled checks
- Follow the exact pattern in src/Encina.Security.Encryption/Diagnostics/EncryptionDiagnostics.cs
- Log messages use structured parameters ({MessageType}, {KeyId}, etc.) — not string interpolation

REFERENCE FILES:
- src/Encina.Security.Encryption/Diagnostics/EncryptionDiagnostics.cs (ActivitySource + Meter pattern)
- src/Encina.Compliance.BreachNotification/Diagnostics/BreachNotificationLogMessages.cs (LoggerMessage pattern with EventId ranges)
- src/Encina.Messaging/Diagnostics/OutboxStoreLog.cs (messaging diagnostics pattern)
- src/Encina.Security.Audit/Diagnostics/ReadAuditLog.cs (EventId grouping pattern)
```

</details>

---

### Phase 6: Testing

> **Goal**: Comprehensive test coverage across unit, guard, contract, and property tests.

<details>
<summary><strong>Tasks</strong></summary>

#### Unit Tests (`tests/Encina.UnitTests/Messaging/Encryption/`)

1. **`JsonMessageSerializerTests.cs`**
   - Serialize/Deserialize round-trip for various types
   - Null handling
   - Complex nested objects
   - Unicode content

2. **`EncryptingMessageSerializerTests.cs`**
   - Encrypt then decrypt round-trip (integration with mocked IMessageEncryptionProvider)
   - Global encryption enabled — all messages encrypted
   - `[EncryptedMessage]` attribute — only decorated messages encrypted
   - `[EncryptedMessage(Enabled = false)]` overrides global config
   - `[EncryptedMessage(KeyId = "custom")]` uses specific key
   - `[EncryptedMessage(UseTenantKey = true)]` resolves tenant key
   - Non-encrypted content passes through unchanged
   - Malformed encrypted payload handling (graceful error)
   - Concurrent serialization thread-safety

3. **`DefaultMessageEncryptionProviderTests.cs`**
   - Encrypt/Decrypt with mocked IFieldEncryptor and IKeyProvider
   - Key resolution (explicit KeyId vs default)
   - Error propagation (key not found, encryption failure)
   - AssociatedData forwarding

4. **`EncryptedPayloadFormatterTests.cs`**
   - Format → TryParse round-trip
   - TryParse with malformed inputs (missing fields, invalid base64, wrong version)
   - Version detection
   - Edge cases (empty strings, null)

5. **`EncryptedMessageAttributeCacheTests.cs`**
   - Cache hit/miss behavior
   - Types with [EncryptedMessage] attribute
   - Types without attribute (null result)
   - Thread-safety under concurrent access

6. **`DefaultTenantKeyResolverTests.cs`**
   - Default pattern resolution
   - Custom pattern resolution
   - Null/empty tenant ID handling

7. **`MessageEncryptionOptionsTests.cs`**
   - Default values verification
   - Configuration binding

8. **`MessageEncryptionHealthCheckTests.cs`**
   - Healthy when roundtrip succeeds
   - Unhealthy when provider fails
   - Unhealthy when key not available

9. **`MessageEncryptionDiagnosticsTests.cs`**
   - Activity creation for encrypt/decrypt
   - Counter increments
   - Log message emission with correct EventIds

10. **`ServiceCollectionExtensionsTests.cs`**
    - Default registrations
    - Custom provider registration
    - Decorator pattern verification (IMessageSerializer wrapping)
    - TryAdd behavior (no double registration)

#### KMS Satellite Tests (`tests/Encina.UnitTests/Messaging/Encryption/`)

11. **`DataProtectionMessageEncryptionProviderTests.cs`**
    - Encrypt/Decrypt with mocked IDataProtectionProvider
    - Purpose string configuration
    - Error handling

#### Guard Tests (`tests/Encina.GuardTests/`)

12. **`MessageEncryptionGuardTests.cs`**
    - ArgumentNullException for all public constructors and methods
    - All IMessageEncryptionProvider methods
    - All IMessageSerializer methods
    - ServiceCollectionExtensions null services parameter

#### Contract Tests (`tests/Encina.ContractTests/`)

13. **`IMessageEncryptionProviderContractTests.cs`**
    - All implementations follow the contract:
      - Encrypt then decrypt returns original plaintext
      - EncryptedPayload has non-empty KeyId and Algorithm
      - Null/empty plaintext handling

14. **`IMessageSerializerContractTests.cs`**
    - All implementations follow the contract:
      - Serialize then deserialize returns equivalent object
      - Null message handling

#### Property Tests (`tests/Encina.PropertyTests/`)

15. **`MessageEncryptionPropertyTests.cs`** (FsCheck)
    - Round-trip: for any byte array, `Decrypt(Encrypt(bytes)) == bytes`
    - Payload format: `Format(Parse(formatted)) == formatted`
    - Key isolation: messages encrypted with different keys cannot be cross-decrypted
    - Attribute cache idempotency: multiple reads return same result

#### Integration Tests — `.md` Justification

16. **`tests/Encina.IntegrationTests/Messaging/Encryption/MessageEncryption.md`**
    - Justification: Message encryption is provider-independent (wraps Content string before store). No database-specific behavior to test. Unit tests with mocked stores provide full coverage.

#### Load Tests — `.md` Justification

17. **`tests/Encina.LoadTests/Messaging/Encryption/MessageEncryption.md`**
    - Justification: Encryption operations are CPU-bound and thread-safe. BenchmarkDotNet provides better throughput measurement than load tests. Outbox processing under load is already covered by existing outbox load tests.

#### Benchmark Tests

18. **`tests/Encina.BenchmarkTests/Encina.Messaging.Encryption.Benchmarks/`**
    - `MessageEncryptionBenchmarks.cs`:
      - `EncryptSmallPayload` (100 bytes) — baseline
      - `EncryptMediumPayload` (1KB) — typical message
      - `EncryptLargePayload` (100KB) — large event
      - `DecryptSmallPayload`, `DecryptMediumPayload`, `DecryptLargePayload`
      - `SerializeWithEncryption` vs `SerializeWithoutEncryption` — overhead measurement
      - `AttributeCacheLookup` — cache performance
    - Use `BenchmarkSwitcher.FromAssembly().Run(args, config)` (NOT `BenchmarkRunner.Run<T>()`)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of Encina.Messaging.Encryption (Issue #129).

CONTEXT:
- Phases 1-5 are done. All production code is complete.
- Now we create comprehensive tests: unit, guard, contract, property, and benchmark.
- The codebase uses xUnit + Moq + FluentAssertions patterns
- Tests go in the consolidated test projects (Encina.UnitTests, Encina.GuardTests, etc.)
- Integration tests get a .md justification (encryption is provider-independent)

TASK:
Create all test files listed in Phase 6 Tasks. Target >90% coverage for the new packages.

KEY RULES:
- Unit tests: AAA pattern, one assert per test, descriptive names
- Guard tests: verify ArgumentNullException for ALL public parameters on ALL public methods/constructors
- Contract tests: verify all IMessageEncryptionProvider implementations follow same contract
- Property tests: FsCheck generators for byte arrays, encryption round-trip invariant
- Benchmark tests: use BenchmarkSwitcher (NOT BenchmarkRunner), materialize results
- .md justifications for integration tests and load tests (not database-dependent)
- All test classes in correct folders matching the pattern:
  - Unit: tests/Encina.UnitTests/Messaging/Encryption/
  - Guard: tests/Encina.GuardTests/Messaging/Encryption/
  - Contract: tests/Encina.ContractTests/Messaging/Encryption/
  - Property: tests/Encina.PropertyTests/Messaging/Encryption/
  - Benchmark: tests/Encina.BenchmarkTests/Encina.Messaging.Encryption.Benchmarks/

REFERENCE FILES:
- tests/Encina.UnitTests/Security/Encryption/ (existing encryption tests pattern)
- tests/Encina.GuardTests/ (guard test pattern)
- tests/Encina.ContractTests/ (contract test pattern)
- tests/Encina.PropertyTests/ (FsCheck property test pattern)
- tests/Encina.BenchmarkTests/ (BenchmarkDotNet project structure)
```

</details>

---

### Phase 7: Documentation & Finalization

> **Goal**: Complete XML documentation, update CHANGELOG, ROADMAP, and ensure build verification.

<details>
<summary><strong>Tasks</strong></summary>

1. **XML doc comments** — Verify all new public APIs have `<summary>`, `<remarks>`, `<param>`, `<returns>`, `<example>` where appropriate

2. **CHANGELOG.md** — Add under `## [Unreleased]` → `### Added`:
   ```markdown
   - `Encina.Messaging.Encryption` — Transparent message payload encryption for Outbox/Inbox patterns (#129)
     - `IMessageEncryptionProvider` interface for pluggable encryption
     - `IMessageSerializer` abstraction for message serialization (replaces hardcoded JsonSerializer)
     - `[EncryptedMessage]` attribute for declarative message encryption
     - `EncryptingMessageSerializer` decorator with versioned encrypted payload format
     - Multi-tenant key isolation via `ITenantKeyResolver`
     - Key rotation support without re-encryption
     - OpenTelemetry tracing and metrics (EventIds 2400-2499)
   - `Encina.Messaging.Encryption.AzureKeyVault` — Azure Key Vault KMS integration for message encryption keys
   - `Encina.Messaging.Encryption.AwsKms` — AWS KMS integration for message encryption keys
   - `Encina.Messaging.Encryption.DataProtection` — ASP.NET Core Data Protection integration
   ```

3. **ROADMAP.md** — Update if milestone v0.13.0 features list needs updating

4. **Package README.md** files:
   - `src/Encina.Messaging.Encryption/README.md` — Usage guide, configuration examples, attribute reference
   - `src/Encina.Messaging.Encryption.AzureKeyVault/README.md` — Azure Key Vault setup
   - `src/Encina.Messaging.Encryption.AwsKms/README.md` — AWS KMS setup
   - `src/Encina.Messaging.Encryption.DataProtection/README.md` — Data Protection setup

5. **Feature documentation** — `docs/features/message-encryption.md`:
   - Overview & motivation
   - Architecture diagram (field-level vs payload-level encryption layers)
   - Configuration options reference
   - Multi-tenant key isolation guide
   - Key rotation procedures
   - Provider comparison table (Azure Key Vault vs AWS KMS vs Data Protection)
   - Performance characteristics
   - Compliance mapping (GDPR Art. 32, HIPAA § 164.312, PCI-DSS Req. 3)

6. **docs/INVENTORY.md** — Update with new packages and files

7. **PublicAPI.Shipped.txt / PublicAPI.Unshipped.txt** — Ensure all public symbols tracked in all 4 packages

8. **Build verification**:
   - `dotnet build Encina.slnx --configuration Release` → 0 errors, 0 warnings
   - `dotnet test Encina.slnx --configuration Release` → all pass
   - Coverage target: ≥90% for new packages

9. **Final commit**: `Fixes #129`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 (final) of Encina.Messaging.Encryption (Issue #129).

CONTEXT:
- All production code and tests are complete (Phases 1-6)
- Now we finalize documentation, public API tracking, and build verification

TASK:
1. Verify XML doc comments on all public APIs
2. Update CHANGELOG.md with new features under Unreleased
3. Create README.md for each of the 4 new packages
4. Create docs/features/message-encryption.md feature documentation
5. Update docs/INVENTORY.md
6. Verify PublicAPI.Unshipped.txt files
7. Run build verification: dotnet build --configuration Release (0 warnings)
8. Run tests: dotnet test --configuration Release (all pass)

KEY RULES:
- CHANGELOG follows Keep a Changelog format
- README files include: installation, quick start, configuration, examples
- Feature doc includes architecture diagram, compliance mapping, performance notes
- All documentation in English
- No AI attribution in commits
- Final commit message references Fixes #129

REFERENCE FILES:
- CHANGELOG.md (existing format)
- ROADMAP.md (milestone structure)
- docs/INVENTORY.md (inventory format)
- src/Encina.Security.Encryption/README.md (satellite package README example)
- docs/features/ (existing feature docs)
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Reference | Relevance |
|----------|-----------|-----------|
| **GDPR Art. 32** | Encryption of personal data | Message encryption is a technical measure for data protection |
| **GDPR Art. 5(1)(f)** | Integrity and confidentiality | Protection against unauthorized access to personal data |
| **HIPAA § 164.312(a)(2)(iv)** | Encryption and decryption | Required for ePHI in transit and at rest |
| **HIPAA § 164.312(e)(2)(ii)** | Encryption mechanism | Technical safeguard for data in transmission |
| **PCI-DSS Req. 3.4** | Render PAN unreadable | Encryption of payment data at rest |
| **PCI-DSS Req. 4.1** | Strong cryptography in transit | Protection during transmission |
| **NIST SP 800-38D** | AES-GCM specification | Foundation algorithm for authenticated encryption |
| **NIST SP 800-57** | Key management guidelines | Key rotation, lifecycle management |
| **OWASP Cryptographic Failures** | A02:2021 | Prevention of sensitive data exposure |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `IFieldEncryptor` | `Encina.Security.Encryption/Abstractions/` | Reuse for AES-256-GCM encryption primitives |
| `IKeyProvider` | `Encina.Security.Encryption/Abstractions/` | Key management interface (reused by KMS satellites) |
| `AesGcmFieldEncryptor` | `Encina.Security.Encryption/Algorithms/` | Default AES-256-GCM implementation |
| `InMemoryKeyProvider` | `Encina.Security.Encryption/` | Testing/development key provider |
| `EncryptedValue` | `Encina.Security.Encryption/` | Pattern for `EncryptedPayload` record |
| `EncryptedPropertyCache` | `Encina.Security.Encryption/` | Pattern for `EncryptedMessageAttributeCache` |
| `EncryptionErrors` | `Encina.Security.Encryption/` | Pattern for `MessageEncryptionErrors` |
| `EncryptionDiagnostics` | `Encina.Security.Encryption/Diagnostics/` | Pattern for `MessageEncryptionDiagnostics` |
| `EncryptionHealthCheck` | `Encina.Security.Encryption/Health/` | Pattern for `MessageEncryptionHealthCheck` |
| `IOutboxMessage` | `Encina.Messaging/Outbox/` | Interface with `Content` property to encrypt |
| `OutboxOrchestrator` | `Encina.Messaging/Outbox/` | Integration point for `IMessageSerializer` |
| `InboxOrchestrator` | `Encina.Messaging/Inbox/` | Integration point for `IMessageSerializer` |
| `AzureKeyVaultSecretProvider` | `Encina.Security.Secrets.AzureKeyVault/` | Pattern for Azure SDK integration |
| `AwsSecretsManagerProvider` | `Encina.Security.Secrets.AwsSecretsManager/` | Pattern for AWS SDK integration |
| `ITenantContext` | `Encina.Tenancy/` | Multi-tenant context resolution |

### EventId Allocation

| Package | Range | Notes |
|---------|-------|-------|
| Encina.SignalR | 1-48 | Low-range, basic messaging |
| Encina.Security.Audit | 1-9 | Audit retention service |
| Encina.Security.Secrets | 1-45 | Secret reader/writer/cache |
| Encina.Security.Secrets.AzureKeyVault | 200-208 | Azure Key Vault operations |
| Encina.Security.Secrets.AwsSecretsManager | 210-218 | AWS Secrets Manager operations |
| Encina.Security.Secrets.HashiCorpVault | 220-227 | HashiCorp Vault operations |
| Encina.Security.Secrets.GoogleCloudSecretManager | 230-238 | GCP Secret Manager operations |
| Encina.DomainModeling (Write Audit) | 1600-1699 | Write audit trail |
| Encina.Security.Audit (Read Audit) | 1700-1799 | Read audit trail |
| Encina.Tenancy | 1800-1804 | Multi-tenancy operations |
| Encina.Messaging (Outbox) | 2000-2006 | Outbox store operations |
| Encina.Messaging (Inbox) | 2100-2106 | Inbox store operations |
| Encina.Messaging (Saga) | 2200-2299 | Saga store operations |
| Encina.Messaging (Scheduling) | 2300-2306 | Scheduled message operations |
| **Encina.Messaging.Encryption** | **2400-2499** | **Message encryption operations (NEW)** |
| Encina.Security (Authorization) | 8000-8004 | Security authorization |
| Encina.Compliance.GDPR (LawfulBasis) | 8200-8216 | Lawful basis validation |
| Encina.Compliance.Anonymization | 8400-8481 | Anonymization/pseudonymization |
| Encina.Compliance.DataResidency | 8600-8674 | Data sovereignty/residency |
| Encina.Compliance.BreachNotification | 8700-8771 | Breach notification |
| Encina.Security.AntiTampering | 9100-9105 | HMAC anti-tampering |

### Estimated File Count by Category

| Category | Count | Details |
|----------|-------|---------|
| Core abstractions & models | 10 | Interfaces, records, attributes, errors |
| Default implementations | 6 | Serializers, provider, resolver, cache, formatter |
| Configuration & DI | 3 | Options, ServiceCollectionExtensions, HealthCheck |
| KMS satellites | 9 | 3 packages × (provider + options + DI) |
| Observability | 2 | Diagnostics + LogMessages |
| Messaging integration | 2 | OutboxOrchestrator + InboxOrchestrator modifications |
| Unit tests | 11 | One per component |
| Guard tests | 1 | Consolidated guard tests |
| Contract tests | 2 | IMessageEncryptionProvider + IMessageSerializer |
| Property tests | 1 | FsCheck round-trip tests |
| Benchmark tests | 2 | Program.cs + benchmarks class |
| Documentation | 6 | READMEs + feature doc + CHANGELOG + INVENTORY |
| **Total** | **~55** | |

### Prerequisites Assessment

| Prerequisite | Status | Impact |
|--------------|--------|--------|
| `Encina.Security.Encryption` (field-level) | ✅ Completed (#396) | Provides `IKeyProvider`, `IFieldEncryptor`, `AesGcmFieldEncryptor` |
| `Encina.Security.Secrets` (vault integration) | ✅ Completed (#400, #603) | Provides patterns for Azure/AWS/GCP integration |
| `Encina.Messaging` (outbox/inbox) | ✅ Exists | Integration target — `OutboxOrchestrator`, `InboxOrchestrator` |
| `Encina.Tenancy` (multi-tenant context) | ✅ Exists | Optional integration for tenant key resolution |
| `IMessageSerializer` abstraction | ❌ Does not exist yet | Created in Phase 3 — minor refactor to Messaging |

No blocking prerequisites. The only dependency not yet available (`IMessageSerializer`) is created as part of this implementation.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt (All Phases)</strong></summary>

```
You are implementing Encina.Messaging.Encryption — Message Encryption & Security (Issue #129).

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 messaging library using Railway Oriented Programming (Either<EncinaError, T>)
- Pre-1.0: choose the best solution, not the compatible one
- Existing field-level encryption in Encina.Security.Encryption (IKeyProvider, IFieldEncryptor, AesGcmFieldEncryptor)
- Existing secrets management in Encina.Security.Secrets.* (Azure Key Vault, AWS, GCP, HashiCorp Vault)
- Outbox/Inbox orchestrators currently use hardcoded JsonSerializer.Serialize()
- This feature adds PAYLOAD-level encryption: encrypting the serialized OutboxMessage.Content before persistence

IMPLEMENTATION OVERVIEW:
4 new packages:
1. Encina.Messaging.Encryption — Core: IMessageEncryptionProvider, IMessageSerializer, [EncryptedMessage], EncryptingMessageSerializer decorator
2. Encina.Messaging.Encryption.AzureKeyVault — Azure Key Vault IKeyProvider adapter
3. Encina.Messaging.Encryption.AwsKms — AWS KMS IKeyProvider adapter
4. Encina.Messaging.Encryption.DataProtection — ASP.NET Core Data Protection IMessageEncryptionProvider

Plus modifications to Encina.Messaging:
- Add IMessageSerializer interface + JsonMessageSerializer default
- Modify OutboxOrchestrator and InboxOrchestrator to use IMessageSerializer

KEY PATTERNS:
- IMessageSerializer lives in Encina.Messaging (serialization is a messaging concern)
- EncryptingMessageSerializer DECORATES JsonMessageSerializer (transparent encryption)
- DefaultMessageEncryptionProvider delegates to IFieldEncryptor + IKeyProvider (reuses existing crypto)
- Encrypted payload format: ENC:v1:{keyId}:{algorithm}:{base64Nonce}:{base64Tag}:{base64Ciphertext}
- [EncryptedMessage] attribute with ConcurrentDictionary cache (same pattern as EncryptedPropertyCache)
- Multi-tenant: ITenantKeyResolver maps tenantId → keyId, integrates with ITenantContext
- Observability: ActivitySource "Encina.Messaging.Encryption", Meter, EventIds 2400-2499
- DI: AddEncinaMessageEncryption() decorates IMessageSerializer, TryAdd for all registrations

REFERENCE FILES:
- src/Encina.Security.Encryption/ — All files (crypto primitives, pipeline behavior, attributes, DI)
- src/Encina.Security.Secrets.AzureKeyVault/ — Azure SDK integration pattern
- src/Encina.Security.Secrets.AwsSecretsManager/ — AWS SDK integration pattern
- src/Encina.Messaging/Outbox/OutboxOrchestrator.cs — Current JsonSerializer usage
- src/Encina.Messaging/Inbox/InboxOrchestrator.cs — Current deserialization
- src/Encina.Messaging/ServiceCollectionExtensions.cs — Existing messaging DI
- src/Encina.Compliance.BreachNotification/Diagnostics/ — LoggerMessage pattern
- tests/Encina.UnitTests/Security/Encryption/ — Test patterns

PHASES:
Phase 1: Core abstractions, models, attributes, error codes (Encina.Messaging.Encryption)
Phase 2: Default implementations (JsonMessageSerializer, EncryptingMessageSerializer, DefaultMessageEncryptionProvider)
Phase 3: Messaging integration (IMessageSerializer in Encina.Messaging, modify orchestrators, DI, health check)
Phase 4: KMS satellites (AzureKeyVault, AwsKms, DataProtection)
Phase 5: Observability (ActivitySource, Meter, LoggerMessage EventIds 2400-2499)
Phase 6: Testing (unit, guard, contract, property, benchmark)
Phase 7: Documentation (CHANGELOG, READMEs, feature docs, build verification)

ENCINA RULES:
- .NET 10 / C# 14, nullable enabled everywhere
- ROP: Either<EncinaError, T> on all async operations
- All public types need XML documentation
- No [Obsolete], no backward compatibility layers
- Satellite DI: AddEncina*() with TryAdd*
- Health checks: DefaultName const, Tags static array, scoped resolution
- Diagnostics: ActivitySource + Meter + [LoggerMessage] source generator
- SQLite dates in ISO 8601 "O" format (if any date handling)
- Tests: AAA pattern, one thing per test, BenchmarkSwitcher not BenchmarkRunner
- No AI attribution in commits
```

</details>

---

## Next Steps

1. **Review** this plan for completeness and alignment with project goals
2. **Publish** as a comment on [Issue #129](https://github.com/dlrivada/Encina/issues/129)
3. **Implement** phase by phase:
   - Phase 1 → Core abstractions & models
   - Phase 2 → Default implementations
   - Phase 3 → Messaging integration & DI
   - Phase 4 → KMS satellite packages
   - Phase 5 → Observability
   - Phase 6 → Testing
   - Phase 7 → Documentation & finalization
