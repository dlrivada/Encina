# Encina.Secrets.AzureKeyVault

[![NuGet](https://img.shields.io/nuget/v/Encina.Secrets.AzureKeyVault.svg)](https://www.nuget.org/packages/Encina.Secrets.AzureKeyVault/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](../../LICENSE)

Azure Key Vault provider for Encina secrets management. Implements `ISecretProvider` using the Azure SDK `SecretClient`, with full Railway Oriented Programming support and optional health checks.

## Prerequisites

- Azure subscription with a Key Vault instance
- `Azure.Identity` for authentication (included as dependency)
- Appropriate Key Vault access policies or RBAC roles assigned to the application identity

## Installation

```bash
dotnet add package Encina.Secrets.AzureKeyVault
```

## Quick Start

### 1. Register the Provider

```csharp
services.AddEncinaKeyVaultSecrets(options =>
{
    options.VaultUri = "https://my-vault.vault.azure.net/";
});
```

### 2. Use ISecretProvider

```csharp
public class MyService(ISecretProvider secrets)
{
    public async Task DoWorkAsync(CancellationToken ct)
    {
        var result = await secrets.GetSecretAsync("database-password", ct);

        result.Match(
            Right: secret => UsePassword(secret.Value),
            Left: error => logger.LogError("Failed: {Code}", error.Code));
    }
}
```

## Configuration Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `VaultUri` | `string` | `""` | URI of the Azure Key Vault (e.g., `https://my-vault.vault.azure.net/`) |
| `Credential` | `TokenCredential?` | `null` | Token credential for authentication. When `null`, `DefaultAzureCredential` is used |
| `ProviderHealthCheck` | `ProviderHealthCheckOptions` | Disabled | Health check configuration |

### Custom Credential

```csharp
services.AddEncinaKeyVaultSecrets(options =>
{
    options.VaultUri = "https://my-vault.vault.azure.net/";
    options.Credential = new ManagedIdentityCredential();
});
```

### Enable Health Check

```csharp
services.AddEncinaKeyVaultSecrets(options =>
{
    options.VaultUri = "https://my-vault.vault.azure.net/";
    options.ProviderHealthCheck = new ProviderHealthCheckOptions
    {
        Enabled = true
    };
});
```

## Health Check

- **Class**: `KeyVaultHealthCheck`
- **Default Name**: `encina-secrets-keyvault`
- **Default Tags**: `encina`, `secrets`, `keyvault`, `ready`
- **Mechanism**: Lists secrets with max 1 result to verify connectivity

## Documentation

- [Encina.Secrets](../Encina.Secrets/README.md) - Core abstractions and caching
- [API Reference](https://docs.encina.dev/api/Encina.Secrets.AzureKeyVault) - Full API documentation

## Related Packages

| Package | Description |
|---------|-------------|
| `Encina.Secrets` | Core abstractions, caching decorator, IConfiguration integration |
| `Encina.Secrets.AWSSecretsManager` | AWS Secrets Manager provider |
| `Encina.Secrets.HashiCorpVault` | HashiCorp Vault KV v2 provider |
| `Encina.Secrets.GoogleSecretManager` | Google Cloud Secret Manager provider |

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.
