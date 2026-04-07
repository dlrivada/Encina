# Encina.Messaging.Encryption

[![NuGet](https://img.shields.io/nuget/v/Encina.Messaging.Encryption.svg)](https://www.nuget.org/packages/Encina.Messaging.Encryption)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)

**Transparent payload-level encryption for Encina Outbox/Inbox messaging patterns using AES-256-GCM.**

Encina.Messaging.Encryption decorates the existing `IMessageSerializer` with `EncryptingMessageSerializer` to encrypt serialized message content before database persistence and decrypt upon retrieval. Supports key rotation, multi-tenant key isolation, and pluggable KMS providers.

## Key Features

- **AES-256-GCM** (NIST SP 800-38D) via `Encina.Security.Encryption` infrastructure
- **Serializer decorator** — wraps `IMessageSerializer` transparently, zero changes to existing code
- **Attribute-based** — `[EncryptedMessage]` for per-type opt-in, `EncryptAllMessages` for global mode
- **Key rotation** — key ID embedded in payload enables seamless rotation
- **Multi-tenant isolation** — per-tenant key derivation via `ITenantKeyResolver`
- **Railway Oriented Programming** — all operations return `Either<EncinaError, T>`
- **Compact payload format** — `ENC:v{Version}:{KeyId}:{Algorithm}:{Nonce}:{Tag}:{Ciphertext}`
- **OpenTelemetry** tracing and metrics (opt-in)
- **Health check** with roundtrip verification (opt-in)
- **Pluggable providers** — Azure Key Vault, AWS KMS, ASP.NET Core Data Protection

## Quick Start

```csharp
// 1. Register messaging (any provider)
services.AddEncinaEntityFrameworkCore<AppDbContext>(config =>
{
    config.UseOutbox = true;
    config.UseInbox = true;
});

// 2. Enable message encryption
services.AddEncinaMessageEncryption(options =>
{
    options.EncryptAllMessages = true;
    options.DefaultKeyId = "msg-key-2024";
    options.AddHealthCheck = true;
});

// 3. Decorate message types (if not using EncryptAllMessages)
[EncryptedMessage(KeyId = "orders-key")]
public sealed record OrderPlacedEvent(Guid OrderId, decimal Total);

// 4. Messages are encrypted/decrypted transparently — zero handler changes
```

## Configuration

```csharp
services.AddEncinaMessageEncryption(options =>
{
    // Global encryption toggle
    options.Enabled = true;

    // Encrypt all messages by default (opt-out via [EncryptedMessage(Enabled = false)])
    options.EncryptAllMessages = false;

    // Default key (null = resolve from IKeyProvider.GetCurrentKeyIdAsync())
    options.DefaultKeyId = "msg-key-2024";

    // Multi-tenant key isolation
    options.UseTenantKeys = true;
    options.TenantKeyPattern = "tenant-{0}-key";

    // Compliance auditing
    options.AuditDecryption = true;

    // Observability
    options.EnableTracing = true;
    options.EnableMetrics = true;
    options.AddHealthCheck = true;
});
```

## KMS Providers

Install the satellite package for your cloud provider:

```csharp
// Azure Key Vault
services.AddEncinaMessageEncryptionAzureKeyVault(options =>
{
    options.VaultUri = new Uri("https://my-vault.vault.azure.net/");
    options.KeyName = "msg-encryption-key";
});

// AWS KMS
services.AddEncinaMessageEncryptionAwsKms(options =>
{
    options.KeyId = "arn:aws:kms:us-east-1:123456789:key/abc-123";
    options.Region = "us-east-1";
});

// ASP.NET Core Data Protection
services.AddDataProtection();
services.AddEncinaMessageEncryptionDataProtection(options =>
{
    options.Purpose = "Encina.Messaging";
});
```

## Performance

<!-- docref-table: bench:messaging/* -->
<!-- /docref-table -->

*Auto-generated from benchmark data. See [methodology](../../docs/testing/performance-measurement-methodology.md).*

## Documentation

- [Message Encryption Guide](../../docs/features/message-encryption.md) — architecture, compliance, and examples
- [Field-Level Encryption](../../docs/features/field-level-encryption.md) — property-level encryption (complementary)
- [CHANGELOG](../../CHANGELOG.md) — version history and release notes

## Dependencies

- `Encina.Messaging` (messaging abstractions)
- `Encina.Security.Encryption` (AES-256-GCM, key management)
- `Microsoft.Extensions.DependencyInjection.Abstractions`
- `Microsoft.Extensions.Logging.Abstractions`
- `Microsoft.Extensions.Diagnostics.HealthChecks`
- `Microsoft.Extensions.Options`

## License

MIT License. See [LICENSE](../../LICENSE) for details.
