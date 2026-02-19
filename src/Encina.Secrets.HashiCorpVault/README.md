# Encina.Secrets.HashiCorpVault

[![NuGet](https://img.shields.io/nuget/v/Encina.Secrets.HashiCorpVault.svg)](https://www.nuget.org/packages/Encina.Secrets.HashiCorpVault/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](../../LICENSE)

HashiCorp Vault KV v2 provider for Encina secrets management. Implements `ISecretProvider` using VaultSharp, with full Railway Oriented Programming support, multiple authentication methods, and optional health checks.

## Prerequisites

- HashiCorp Vault server (self-hosted or HCP Vault)
- KV v2 secrets engine enabled at the configured mount point
- Authentication method configured (Token, AppRole, or Kubernetes)

## Installation

```bash
dotnet add package Encina.Secrets.HashiCorpVault
```

## Quick Start

### 1. Register the Provider

```csharp
services.AddEncinaHashiCorpVault(options =>
{
    options.VaultAddress = "https://vault.example.com:8200";
    options.MountPoint = "secret";
    options.AuthMethod = new TokenAuthMethodInfo("hvs.CAESIG...");
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
| `VaultAddress` | `string` | `""` | Vault server address (e.g., `https://vault.example.com:8200`) |
| `MountPoint` | `string` | `"secret"` | Mount point for the KV v2 secrets engine |
| `AuthMethod` | `IAuthMethodInfo?` | `null` | Authentication method (required). Throws `InvalidOperationException` if not set |
| `ProviderHealthCheck` | `ProviderHealthCheckOptions` | Disabled | Health check configuration |

### Authentication Methods

```csharp
// Token authentication
options.AuthMethod = new TokenAuthMethodInfo("hvs.CAESIG...");

// AppRole authentication
options.AuthMethod = new AppRoleAuthMethodInfo("role-id", "secret-id");

// Kubernetes authentication
options.AuthMethod = new KubernetesAuthMethodInfo("my-role", jwt);
```

### Enable Health Check

```csharp
services.AddEncinaHashiCorpVault(options =>
{
    options.VaultAddress = "https://vault.example.com:8200";
    options.AuthMethod = new TokenAuthMethodInfo("hvs.CAESIG...");
    options.ProviderHealthCheck = new ProviderHealthCheckOptions
    {
        Enabled = true
    };
});
```

## Health Check

- **Class**: `HashiCorpVaultHealthCheck`
- **Default Name**: `encina-secrets-vault`
- **Default Tags**: `encina`, `secrets`, `vault`, `ready`
- **Mechanism**: Calls `/sys/health` endpoint; reports Healthy if initialized and unsealed, Degraded if sealed or uninitialized

## Documentation

- [Encina.Secrets](../Encina.Secrets/README.md) - Core abstractions and caching
- [API Reference](https://docs.encina.dev/api/Encina.Secrets.HashiCorpVault) - Full API documentation

## Related Packages

| Package | Description |
|---------|-------------|
| `Encina.Secrets` | Core abstractions, caching decorator, IConfiguration integration |
| `Encina.Secrets.AzureKeyVault` | Azure Key Vault provider |
| `Encina.Secrets.AWSSecretsManager` | AWS Secrets Manager provider |
| `Encina.Secrets.GoogleSecretManager` | Google Cloud Secret Manager provider |

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.
