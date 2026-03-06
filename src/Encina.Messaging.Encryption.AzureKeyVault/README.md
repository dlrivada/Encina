# Encina.Messaging.Encryption.AzureKeyVault

[![NuGet](https://img.shields.io/nuget/v/Encina.Messaging.Encryption.AzureKeyVault.svg)](https://www.nuget.org/packages/Encina.Messaging.Encryption.AzureKeyVault)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)

**Azure Key Vault key management provider for Encina message encryption.**

Integrates Azure Key Vault with Encina's message encryption infrastructure, providing HSM-backed key management, automatic key rotation, and managed identity support for outbox/inbox message payload encryption.

## Key Features

- **Azure Key Vault** integration via `Azure.Security.KeyVault.Keys` SDK
- **DefaultAzureCredential** — managed identities, Azure CLI, environment variables
- **HSM-backed keys** — FIPS 140-2 Level 2 certified key storage
- **Automatic key rotation** — `RotateKeyAsync` creates new key versions
- **Railway Oriented Programming** — all operations return `Either<EncinaError, T>`

## Quick Start

```csharp
services.AddEncinaMessageEncryptionAzureKeyVault(
    azure =>
    {
        azure.VaultUri = new Uri("https://my-vault.vault.azure.net/");
        azure.KeyName = "msg-encryption-key";
        // Optional: specific credential
        // azure.Credential = new ManagedIdentityCredential();
    },
    encryption =>
    {
        encryption.EncryptAllMessages = true;
        encryption.AddHealthCheck = true;
    });
```

## Configuration

| Property | Type | Description |
|----------|------|-------------|
| `VaultUri` | `Uri?` | Azure Key Vault URI (**required**) |
| `KeyName` | `string?` | Key name in the vault |
| `KeyVersion` | `string?` | Specific key version (null = latest) |
| `Credential` | `TokenCredential?` | Azure credential (null = `DefaultAzureCredential`) |
| `ClientOptions` | `KeyClientOptions?` | Custom `KeyClient` options |

## Dependencies

- `Encina.Messaging.Encryption` (core encryption)
- `Azure.Security.KeyVault.Keys`
- `Azure.Identity`

## License

MIT License. See [LICENSE](../../LICENSE) for details.
