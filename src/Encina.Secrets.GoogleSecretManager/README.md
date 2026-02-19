# Encina.Secrets.GoogleSecretManager

[![NuGet](https://img.shields.io/nuget/v/Encina.Secrets.GoogleSecretManager.svg)](https://www.nuget.org/packages/Encina.Secrets.GoogleSecretManager/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](../../LICENSE)

Google Cloud Secret Manager provider for Encina secrets management. Implements `ISecretProvider` using the Google Cloud SDK `SecretManagerServiceClient`, with full Railway Oriented Programming support and optional health checks.

## Prerequisites

- Google Cloud project with the Secret Manager API enabled
- Authentication via Application Default Credentials (ADC), service account key, or Workload Identity
- IAM roles: `roles/secretmanager.secretAccessor` (read), `roles/secretmanager.admin` (read/write)

## Installation

```bash
dotnet add package Encina.Secrets.GoogleSecretManager
```

## Quick Start

### 1. Register the Provider

```csharp
services.AddEncinaGoogleSecretManager(options =>
{
    options.ProjectId = "my-gcp-project";
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
| `ProjectId` | `string` | `""` | Google Cloud project ID (required). Used to construct resource names (`projects/{projectId}/secrets/{secretName}`) |
| `ProviderHealthCheck` | `ProviderHealthCheckOptions` | Disabled | Health check configuration |

### Enable Health Check

```csharp
services.AddEncinaGoogleSecretManager(options =>
{
    options.ProjectId = "my-gcp-project";
    options.ProviderHealthCheck = new ProviderHealthCheckOptions
    {
        Enabled = true
    };
});
```

## Health Check

- **Class**: `GoogleSecretManagerHealthCheck`
- **Default Name**: `encina-secrets-gcp`
- **Default Tags**: `encina`, `secrets`, `gcp`, `ready`
- **Mechanism**: Lists secrets with page size 1 to verify connectivity; catches gRPC `RpcException` for failure detection

## Documentation

- [Encina.Secrets](../Encina.Secrets/README.md) - Core abstractions and caching
- [API Reference](https://docs.encina.dev/api/Encina.Secrets.GoogleSecretManager) - Full API documentation

## Related Packages

| Package | Description |
|---------|-------------|
| `Encina.Secrets` | Core abstractions, caching decorator, IConfiguration integration |
| `Encina.Secrets.AzureKeyVault` | Azure Key Vault provider |
| `Encina.Secrets.AWSSecretsManager` | AWS Secrets Manager provider |
| `Encina.Secrets.HashiCorpVault` | HashiCorp Vault KV v2 provider |

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.
