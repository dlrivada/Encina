# Encina.Secrets

[![NuGet](https://img.shields.io/nuget/v/Encina.Secrets.svg)](https://www.nuget.org/packages/Encina.Secrets/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](../../LICENSE)

Provider-agnostic secrets management abstractions with Railway Oriented Programming (`Either<EncinaError, T>`). Supports Azure Key Vault, AWS Secrets Manager, HashiCorp Vault, and Google Cloud Secret Manager through a unified interface.

## Features

- **ISecretProvider Interface** - 6 operations: `GetSecretAsync`, `GetSecretVersionAsync`, `SetSecretAsync`, `DeleteSecretAsync`, `ListSecretsAsync`, `ExistsAsync`
- **CachedSecretProvider Decorator** - In-memory caching with configurable TTL, ROP-aware (only caches `Right` results)
- **IConfiguration Integration** - Load secrets as configuration values via `AddEncinaSecrets()`, with prefix filtering and periodic reload
- **OpenTelemetry Instrumentation** - Distributed tracing via `Encina.Secrets` ActivitySource and 3 metric instruments (operations, duration, errors)
- **Health Checks** - DI verification health check (`SecretsHealthCheck`) plus provider-specific health checks
- **6 Structured Error Codes** - Typed errors for precise error handling in ROP pipelines
- **.NET 10 Compatible** - Built with latest C# features

## Installation

```bash
dotnet add package Encina.Secrets
```

## Quick Start

### 1. Register a Provider and Add Caching

```csharp
// Register a provider (e.g., Azure Key Vault)
services.AddEncinaKeyVaultSecrets(options =>
{
    options.VaultUri = "https://my-vault.vault.azure.net/";
});

// Add caching decorator (wraps the registered ISecretProvider)
services.AddEncinaSecretsCaching(options =>
{
    options.DefaultTtl = TimeSpan.FromMinutes(10);
    options.Enabled = true;
});

// Add OpenTelemetry instrumentation
services.AddEncinaSecretsInstrumentation(options =>
{
    options.EnableTracing = true;
    options.EnableMetrics = true;
    options.RecordSecretNames = false; // Security: don't record names in telemetry
});
```

### 2. Use ISecretProvider with ROP

```csharp
public class MyService(ISecretProvider secrets)
{
    public async Task DoWorkAsync(CancellationToken ct)
    {
        var result = await secrets.GetSecretAsync("my-api-key", ct);

        result.Match(
            Right: secret => Console.WriteLine($"Secret retrieved, version: {secret.Version}"),
            Left: error => Console.WriteLine($"Error [{error.Code}]: {error.Message}"));
    }
}
```

### 3. Load Secrets as IConfiguration

```csharp
// In Program.cs
builder.Services.AddEncinaKeyVaultSecrets(options => { ... });
var sp = builder.Services.BuildServiceProvider();
builder.Configuration.AddEncinaSecrets(sp, options =>
{
    options.SecretPrefix = "myapp/";
    options.KeyDelimiter = "/";
    options.ReloadInterval = TimeSpan.FromMinutes(5);
});
```

## Error Codes

| Code | Constant | Meaning |
|------|----------|---------|
| `encina.secrets.not_found` | `NotFoundCode` | Requested secret does not exist |
| `encina.secrets.access_denied` | `AccessDeniedCode` | Insufficient permissions to access the secret |
| `encina.secrets.invalid_name` | `InvalidNameCode` | Secret name is empty, too long, or contains forbidden characters |
| `encina.secrets.provider_unavailable` | `ProviderUnavailableCode` | Provider unreachable (network error, auth failure) |
| `encina.secrets.version_not_found` | `VersionNotFoundCode` | Specific version of the secret does not exist |
| `encina.secrets.operation_failed` | `OperationFailedCode` | Generic operation failure not covered by other codes |

## Observability

- **Tracing**: `Encina.Secrets` ActivitySource with `secrets.operation`, `secrets.name`, and `secrets.success` tags
- **Metrics**: `encina.secrets.operations` (counter), `encina.secrets.duration` (histogram, ms), `encina.secrets.errors` (counter)
- **Logging**: Structured log events for cache hits/misses and cache invalidation

## Documentation

- [API Reference](https://docs.encina.dev/api/Encina.Secrets) - Full API documentation

## Related Packages

| Package | Description |
|---------|-------------|
| `Encina.Secrets.AzureKeyVault` | Azure Key Vault provider implementation |
| `Encina.Secrets.AWSSecretsManager` | AWS Secrets Manager provider implementation |
| `Encina.Secrets.HashiCorpVault` | HashiCorp Vault KV v2 provider implementation |
| `Encina.Secrets.GoogleSecretManager` | Google Cloud Secret Manager provider implementation |

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.
