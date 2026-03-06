# Encina.Messaging.Encryption.DataProtection

[![NuGet](https://img.shields.io/nuget/v/Encina.Messaging.Encryption.DataProtection.svg)](https://www.nuget.org/packages/Encina.Messaging.Encryption.DataProtection)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)

**ASP.NET Core Data Protection provider for Encina message encryption.**

Uses ASP.NET Core's built-in Data Protection framework to encrypt outbox/inbox message payloads. Handles both key management and encryption, leveraging DPAPI-compatible key storage with automatic key rotation.

## Key Features

- **ASP.NET Core Data Protection** — built-in key management, no external KMS required
- **Automatic key rotation** — Data Protection rotates keys every 90 days by default
- **DPAPI-compatible** — works with Azure Blob Storage, Redis, file system key rings
- **Self-contained** — implements `IMessageEncryptionProvider` directly (no `IKeyProvider` needed)
- **Railway Oriented Programming** — all operations return `Either<EncinaError, T>`

## Quick Start

```csharp
// Ensure Data Protection is registered
services.AddDataProtection()
    .PersistKeysToAzureBlobStorage(connectionString, containerName, blobName)
    .ProtectKeysWithAzureKeyVault(keyIdentifier, tokenCredential);

// Register message encryption with Data Protection
services.AddEncinaMessageEncryptionDataProtection(
    dp =>
    {
        dp.Purpose = "Encina.Messaging.Payload";
    },
    encryption =>
    {
        encryption.EncryptAllMessages = true;
        encryption.AddHealthCheck = true;
    });
```

## Configuration

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Purpose` | `string` | `"Encina.Messaging.Encryption"` | Data Protection purpose string for key isolation |

## When to Use

| Scenario | Recommended Provider |
|----------|---------------------|
| On-premises, no cloud KMS | **Data Protection** |
| Already using ASP.NET Core Data Protection | **Data Protection** |
| Azure-native, HSM-backed keys | Azure Key Vault |
| AWS-native, IAM-based access | AWS KMS |
| Multi-cloud, need cloud-agnostic | Data Protection with Azure/Redis key storage |

## Dependencies

- `Encina.Messaging.Encryption` (core encryption)
- `Microsoft.AspNetCore.DataProtection`

## License

MIT License. See [LICENSE](../../LICENSE) for details.
