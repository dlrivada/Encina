# Field-Level Encryption in Encina

Encina.Security.Encryption provides automatic, attribute-based field-level encryption and decryption at the CQRS pipeline level, using AES-256-GCM with key rotation and multi-tenant isolation.

## Table of Contents

1. [Overview](#overview)
2. [The Problem](#the-problem)
3. [The Solution](#the-solution)
4. [Quick Start](#quick-start)
5. [Attributes Reference](#attributes-reference)
6. [Key Providers](#key-providers)
7. [Key Rotation](#key-rotation)
8. [Multi-Tenancy](#multi-tenancy)
9. [Pipeline Order](#pipeline-order)
10. [Observability](#observability)
11. [Health Check](#health-check)
12. [Error Handling](#error-handling)
13. [Configuration Reference](#configuration-reference)
14. [Testing](#testing)
15. [Troubleshooting](#troubleshooting)

---

## Overview

Encina.Security.Encryption enables transparent encryption of sensitive properties (emails, SSNs, card numbers) within CQRS commands and queries, without polluting business logic with cryptographic concerns.

| Component | Description |
|-----------|-------------|
| **`IFieldEncryptor`** | Low-level encrypt/decrypt interface for strings and byte arrays |
| **`IKeyProvider`** | Key management abstraction (pluggable for cloud KMS) |
| **`IEncryptionOrchestrator`** | Discovers `[Encrypt]` properties and orchestrates operations |
| **`EncryptionPipelineBehavior`** | Auto-encrypts before handler, auto-decrypts after handler |
| **`EncryptionHealthCheck`** | Roundtrip verification of the full crypto pipeline |

**Key characteristics**:

- **AES-256-GCM** (NIST SP 800-38D) with 12-byte random nonce and 16-byte authentication tag
- **Railway Oriented Programming**: All operations return `Either<EncinaError, T>` — no exceptions for business logic
- **Zero overhead** for requests without encryption attributes
- **Compact serialization**: `ENC:v1:{Algorithm}:{KeyId}:{Nonce}:{Tag}:{Ciphertext}` — self-describing, key-versioned
- **Thread-safe**: Stateless encryptor, concurrent-safe key provider

---

## The Problem

Sensitive data must be encrypted at rest for regulatory compliance (GDPR Art. 32, HIPAA, PCI-DSS), but encryption logic pollutes business code:

```csharp
// Without Encina — encryption scattered across handlers
public class CreateUserHandler : ICommandHandler<CreateUserCommand, UserId>
{
    private readonly IAesEncryptor _encryptor;
    private readonly IKeyVault _keyVault;

    public async ValueTask<Either<EncinaError, UserId>> Handle(
        CreateUserCommand command, IRequestContext context, CancellationToken ct)
    {
        // Encryption logic mixed with business logic
        var key = await _keyVault.GetCurrentKeyAsync();
        var encryptedEmail = await _encryptor.EncryptAsync(command.Email, key);
        var encryptedPhone = await _encryptor.EncryptAsync(command.Phone, key);

        var user = new User(encryptedEmail, encryptedPhone, command.Name);
        // ... save user
    }
}
```

Every handler must know about encryption, key management, and error handling — violating Single Responsibility.

---

## The Solution

With Encina, declare encryption requirements on properties and let the pipeline handle everything:

```csharp
// With Encina — clean separation of concerns
public sealed record CreateUserCommand(
    [property: Encrypt(Purpose = "User.Email")] string Email,
    [property: Encrypt(Purpose = "User.Phone")] string Phone,
    string Name
) : ICommand<UserId>;

// Handler has zero encryption awareness
public class CreateUserHandler : ICommandHandler<CreateUserCommand, UserId>
{
    public async ValueTask<Either<EncinaError, UserId>> Handle(
        CreateUserCommand command, IRequestContext context, CancellationToken ct)
    {
        // command.Email is already encrypted ("ENC:v1:0:key-v1:...")
        var user = new User(command.Email, command.Phone, command.Name);
        // ... save user
    }
}
```

The `EncryptionPipelineBehavior` automatically encrypts `Email` and `Phone` before the handler executes, and decrypts response properties (if decorated with `[EncryptedResponse]`) after.

---

## Quick Start

### 1. Install the package

```bash
dotnet add package Encina.Security.Encryption
```

### 2. Register services

```csharp
services.AddLogging();
services.AddEncinaEncryption();
```

### 3. Configure a key provider

For testing, `InMemoryKeyProvider` is registered by default. Add a key:

```csharp
var keyProvider = serviceProvider.GetRequiredService<IKeyProvider>() as InMemoryKeyProvider;
var key = new byte[32];
RandomNumberGenerator.Fill(key);
keyProvider!.AddKey("my-key-v1", key);
keyProvider.SetCurrentKey("my-key-v1");
```

For production, register a cloud KMS provider before `AddEncinaEncryption()`:

```csharp
services.AddSingleton<IKeyProvider, AzureKeyVaultKeyProvider>();
services.AddEncinaEncryption(options => options.AddHealthCheck = true);
```

### 4. Decorate properties

```csharp
public sealed record CreateUserCommand(
    [property: Encrypt(Purpose = "User.Email")] string Email,
    [property: Encrypt(Purpose = "User.SSN")] string SocialSecurityNumber,
    string DisplayName  // Not encrypted
) : ICommand<UserId>;
```

### 5. Use the orchestrator (optional — pipeline does this automatically)

```csharp
var orchestrator = scope.ServiceProvider.GetRequiredService<IEncryptionOrchestrator>();
var context = RequestContext.Create();

// Encrypt
var result = await orchestrator.EncryptAsync(command, context);
// result.IsRight == true => command.Email is now "ENC:v1:0:my-key-v1:..."

// Decrypt
var decryptResult = await orchestrator.DecryptAsync(command, context);
// result.IsRight == true => command.Email is back to original plaintext
```

---

## Attributes Reference

### `[Encrypt]` — Field-Level Encryption

Marks a string property for automatic encryption/decryption.

```csharp
public sealed record PaymentCommand(
    [property: Encrypt(Purpose = "Payment.CardNumber", KeyId = "pci-key-v2")]
    string CardNumber,

    [property: Encrypt(Purpose = "Payment.CVV", FailOnError = false)]
    string CVV
) : ICommand;
```

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Purpose` | `string?` | `null` | Purpose string for key derivation (recommended: `"Entity.Property"`) |
| `KeyId` | `string?` | `null` | Explicit key version (default: current active key) |
| `Algorithm` | `EncryptionAlgorithm` | `Aes256Gcm` | Encryption algorithm (inherited from base) |
| `FailOnError` | `bool` | `true` | Whether errors propagate or are silently logged (inherited from base) |

### `[EncryptedResponse]` — Response Encryption

Marks a response class or property for encryption before returning to the caller.

```csharp
[EncryptedResponse]
public sealed class UserProfileResponse
{
    [Encrypt(Purpose = "User.Email")]
    public string Email { get; set; }
    public string Name { get; set; } // Not encrypted
}
```

### `[DecryptOnReceive]` — Incoming Data Decryption

Marks incoming data for decryption before handler execution.

```csharp
public sealed record ProcessWebhookCommand(
    [property: DecryptOnReceive] string EncryptedPayload
) : ICommand;
```

---

## Key Providers

### `InMemoryKeyProvider` (Testing/Development)

Thread-safe, in-memory key store. **Not for production** — keys are lost on process restart.

```csharp
var keyProvider = new InMemoryKeyProvider();

// Add keys manually
keyProvider.AddKey("key-v1", keyMaterial);
keyProvider.SetCurrentKey("key-v1");

// Or rotate automatically
var result = await keyProvider.RotateKeyAsync();
var newKeyId = result.Match(Right: id => id, Left: _ => throw new Exception());
```

### Custom Key Provider (Production)

Implement `IKeyProvider` for your cloud KMS:

```csharp
public sealed class AzureKeyVaultKeyProvider : IKeyProvider
{
    private readonly SecretClient _client;

    public async ValueTask<Either<EncinaError, byte[]>> GetKeyAsync(
        string keyId, CancellationToken ct = default)
    {
        try
        {
            var secret = await _client.GetSecretAsync(keyId, cancellationToken: ct);
            return Right(Convert.FromBase64String(secret.Value.Value));
        }
        catch (RequestFailedException ex)
        {
            return Left(EncryptionErrors.KeyNotFound(keyId));
        }
    }

    // ... implement GetCurrentKeyIdAsync and RotateKeyAsync
}

// Register before AddEncinaEncryption (TryAdd semantics)
services.AddSingleton<IKeyProvider, AzureKeyVaultKeyProvider>();
services.AddEncinaEncryption();
```

---

## Key Rotation

The serialized format `ENC:v1:{Algorithm}:{KeyId}:...` embeds the key version used for encryption. During decryption, the embedded `KeyId` is used to retrieve the correct key, enabling seamless key rotation:

```csharp
// 1. Encrypt with key-v1
keyProvider.SetCurrentKey("key-v1");
await orchestrator.EncryptAsync(command, context);
// command.Email = "ENC:v1:0:key-v1:..."

// 2. Rotate to key-v2
var key2 = new byte[32];
RandomNumberGenerator.Fill(key2);
keyProvider.AddKey("key-v2", key2);
keyProvider.SetCurrentKey("key-v2");

// 3. Old data still decrypts (uses embedded key-v1)
await orchestrator.DecryptAsync(command, context);
// command.Email = "user@example.com" (decrypted with key-v1)

// 4. New encryptions use key-v2
await orchestrator.EncryptAsync(newCommand, context);
// newCommand.Email = "ENC:v1:0:key-v2:..."
```

**Crypto-shredding for GDPR**: Delete the encryption key to render all data encrypted with that key permanently unreadable — without touching the database.

---

## Multi-Tenancy

The `EncryptionContext.TenantId` is automatically populated from `IRequestContext.TenantId`, enabling per-tenant key isolation:

```csharp
// The orchestrator automatically builds context with tenant info
// BuildEncryptionContext(attribute, requestContext) =>
//   new EncryptionContext { TenantId = requestContext.TenantId, ... }
```

For tenant-specific keys, implement `IKeyProvider` with tenant-aware key lookup:

```csharp
public sealed class TenantAwareKeyProvider : IKeyProvider
{
    public async ValueTask<Either<EncinaError, byte[]>> GetKeyAsync(
        string keyId, CancellationToken ct = default)
    {
        // keyId format: "tenant-A:key-v1"
        var parts = keyId.Split(':');
        var tenantId = parts[0];
        var keyVersion = parts[1];

        return await _kmsClient.GetKeyAsync(tenantId, keyVersion, ct);
    }
}
```

---

## Pipeline Order

The recommended pipeline order when combining encryption with other behaviors:

```text
Request Flow (top → bottom):
┌─────────────────────────────────┐
│ 1. Validation                   │  ← Validate plaintext BEFORE encryption
│ 2. Security (RBAC/ABAC)         │  ← Authorize BEFORE encryption
│ 3. Encryption (encrypt request) │  ← Encrypt sensitive fields
│ 4. Audit Trail                  │  ← Log encrypted values (safe)
│ 5. Handler                      │  ← Business logic with encrypted data
│ 6. Encryption (decrypt response)│  ← Decrypt response fields
└─────────────────────────────────┘
```

This ensures:

- Validation sees plaintext values (can validate email format, etc.)
- Security checks run on the authenticated user before any crypto work
- Audit trail logs encrypted values (no plaintext in logs)
- Handler receives encrypted data for persistence

---

## Observability

### OpenTelemetry Tracing

Activity source: `Encina.Security.Encryption`

| Tag | Description |
|-----|-------------|
| `encryption.request_type` | The request type being processed |
| `encryption.operation` | `encrypt` or `decrypt` |
| `encryption.property_count` | Number of properties processed |
| `encryption.outcome` | `success` or `failure` |

### Metrics

Meter: `Encina.Security.Encryption`

| Instrument | Type | Unit | Description |
|------------|------|------|-------------|
| `encryption.operations.total` | Counter | `{operations}` | Total encrypt/decrypt operations |
| `encryption.failures.total` | Counter | `{failures}` | Total failed operations |
| `encryption.operation.duration` | Histogram | `ms` | Operation duration |

### Enable observability

```csharp
services.AddEncinaEncryption(options =>
{
    options.EnableTracing = true;
    options.EnableMetrics = true;
});
```

---

## Health Check

The `EncryptionHealthCheck` performs a roundtrip verification: encrypts test data with the current key and decrypts it, verifying the entire crypto pipeline works end-to-end.

```csharp
services.AddEncinaEncryption(options => options.AddHealthCheck = true);
```

Health check name: `encina-encryption`
Tags: `encina`, `encryption`, `ready`

**Healthy**: Key provider has a current key, roundtrip encrypt/decrypt succeeds.
**Unhealthy**: No key configured, or roundtrip verification fails.

---

## Error Handling

All operations return `Either<EncinaError, T>` following Railway Oriented Programming:

| Error Code | When | Metadata |
|------------|------|----------|
| `encryption.key_not_found` | Key ID not found in provider | `keyId` |
| `encryption.decryption_failed` | Cryptographic decryption fails (wrong key, tampered data) | `keyId`, `propertyName` |
| `encryption.invalid_ciphertext` | Serialized value cannot be parsed | `propertyName` |
| `encryption.algorithm_not_supported` | Unknown encryption algorithm | `algorithm` |
| `encryption.key_rotation_failed` | Key rotation operation fails | (exception details) |

```csharp
var result = await orchestrator.EncryptAsync(command, context);

result.Match(
    Right: encrypted => { /* success — command properties are now encrypted */ },
    Left: error =>
    {
        logger.LogError("Encryption failed: {Code} - {Message}", error.Code, error.Message);
    }
);
```

---

## Configuration Reference

### `EncryptionOptions`

```csharp
services.AddEncinaEncryption(options =>
{
    // Default encryption algorithm (default: Aes256Gcm)
    options.DefaultAlgorithm = EncryptionAlgorithm.Aes256Gcm;

    // Whether decryption errors propagate or are logged (default: true)
    options.FailOnDecryptionError = true;

    // Register ASP.NET Core health check (default: false)
    options.AddHealthCheck = true;

    // Enable OpenTelemetry tracing (default: false)
    options.EnableTracing = true;

    // Enable OpenTelemetry metrics (default: false)
    options.EnableMetrics = true;
});
```

### Service Lifetimes

| Service | Lifetime | Notes |
|---------|----------|-------|
| `IFieldEncryptor` | Singleton | Stateless, thread-safe |
| `IKeyProvider` | Singleton | Thread-safe (ConcurrentDictionary for InMemory) |
| `IEncryptionOrchestrator` | Scoped | Per-request lifecycle |
| `EncryptionPipelineBehavior` | Transient | New instance per pipeline invocation |

---

## Testing

### Unit Testing with InMemoryKeyProvider

```csharp
[Fact]
public async Task Should_Encrypt_And_Decrypt_Email()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddEncinaEncryption();
    var provider = services.BuildServiceProvider();

    var keyProvider = provider.GetRequiredService<IKeyProvider>() as InMemoryKeyProvider;
    var key = new byte[32];
    RandomNumberGenerator.Fill(key);
    keyProvider!.AddKey("test-key", key);
    keyProvider.SetCurrentKey("test-key");

    using var scope = provider.CreateScope();
    var orchestrator = scope.ServiceProvider
        .GetRequiredService<IEncryptionOrchestrator>();
    var context = RequestContext.CreateForTest();

    var command = new CreateUserCommand("user@test.com", "+1234567890", "John");

    // Act
    var encryptResult = await orchestrator.EncryptAsync(command, context);

    // Assert
    encryptResult.IsRight.Should().BeTrue();
    command.Email.Should().StartWith("ENC:v1:");

    // Verify roundtrip
    var decryptResult = await orchestrator.DecryptAsync(command, context);
    decryptResult.IsRight.Should().BeTrue();
    command.Email.Should().Be("user@test.com");
}
```

### Mocking IKeyProvider

```csharp
var mockKeyProvider = Substitute.For<IKeyProvider>();
mockKeyProvider.GetCurrentKeyIdAsync(Arg.Any<CancellationToken>())
    .Returns(ValueTask.FromResult<Either<EncinaError, string>>(
        Right("test-key")));
mockKeyProvider.GetKeyAsync("test-key", Arg.Any<CancellationToken>())
    .Returns(ValueTask.FromResult<Either<EncinaError, byte[]>>(
        Right(new byte[32])));
```

---

## Troubleshooting

### "Key not found" error

**Cause**: The key ID embedded in the encrypted value does not exist in the key provider.

**Solutions**:

1. Ensure the key was added before encryption: `keyProvider.AddKey("key-id", keyMaterial)`
2. Verify `SetCurrentKey()` was called after adding the key
3. For key rotation, keep old keys in the provider until all data is re-encrypted

### "Invalid ciphertext" error

**Cause**: The serialized string doesn't match the expected format `ENC:v1:{Algorithm}:{KeyId}:{Nonce}:{Tag}:{Ciphertext}`.

**Solutions**:

1. Verify the property value starts with `ENC:v1:`
2. Check that no string manipulation (trimming, encoding) modified the encrypted value
3. Ensure the value wasn't partially overwritten

### "Decryption failed" error

**Cause**: The ciphertext was encrypted with a different key than the one being used for decryption, or the data was tampered with.

**Solutions**:

1. Verify the key material matches the key that was used for encryption
2. Check for data corruption in transit or storage
3. The AES-GCM authentication tag detects any tampering — this is by design

### NSubstitute "Could not create proxy" error in tests

**Cause**: `EncryptionOrchestrator` is `internal` — NSubstitute/Castle DynamicProxy cannot proxy `ILogger<EncryptionOrchestrator>`.

**Solution**: Use `NullLogger<EncryptionOrchestrator>.Instance` instead of `Substitute.For<ILogger<EncryptionOrchestrator>>()`.

### Empty string encryption edge case

**Cause**: Encrypting an empty string produces a valid encrypted value with zero-length ciphertext. Decryption rejects empty ciphertext as invalid.

**Solution**: Ensure all `[Encrypt]`-decorated properties have non-empty values, or set `FailOnError = false` to skip empty values gracefully.
