# Encina.Security.Secrets.AzureKeyVault

Azure Key Vault provider for Encina's secrets management system. Provides enterprise-grade vault integration with managed identity support, automatic secret versioning, and rotation capabilities.

## Features

- **ISP-compliant** — Implements `ISecretReader`, `ISecretWriter`, and `ISecretRotator`
- **DefaultAzureCredential** — Automatic credential discovery (managed identity, environment, Azure CLI)
- **Railway Oriented Programming** — All operations return `Either<EncinaError, T>`, no exceptions
- **Automatic versioning** — Azure Key Vault versions secrets on every write
- **Decorator chain** — Integrates with caching, auditing, and failover decorators
- **Zero-allocation logging** — `LoggerMessage` source generators
- **Thread-safe** — `SecretClient` is designed for concurrent use

## Quick Start

```csharp
// Register Azure Key Vault as the secrets provider
services.AddAzureKeyVaultSecrets(
    new Uri("https://my-vault.vault.azure.net/"));

// Inject and use
public class PaymentService(ISecretReader secretReader)
{
    public async Task<string> GetStripeKeyAsync(CancellationToken ct)
    {
        var result = await secretReader.GetSecretAsync("stripe-api-key", ct);
        return result.Match(
            Right: value => value,
            Left: error => throw new InvalidOperationException(error.Message));
    }
}
```

## Authentication

### DefaultAzureCredential (Recommended)

The default behavior uses `DefaultAzureCredential`, which automatically discovers credentials:

```csharp
services.AddAzureKeyVaultSecrets(
    new Uri("https://my-vault.vault.azure.net/"));
```

This works with managed identities (Azure), environment variables, Azure CLI, and Visual Studio credentials.

### Custom Credential

```csharp
services.AddAzureKeyVaultSecrets(
    new Uri("https://my-vault.vault.azure.net/"),
    kvOptions => kvOptions.Credential = new ManagedIdentityCredential());
```

### Client Options

```csharp
services.AddAzureKeyVaultSecrets(
    new Uri("https://my-vault.vault.azure.net/"),
    kvOptions => kvOptions.ClientOptions = new SecretClientOptions
    {
        Retry = { MaxRetries = 5, Delay = TimeSpan.FromSeconds(1) }
    });
```

## Caching Integration

Enable in-memory caching to reduce Key Vault API calls:

```csharp
services.AddAzureKeyVaultSecrets(
    new Uri("https://my-vault.vault.azure.net/"),
    configureSecrets: options =>
    {
        options.EnableCaching = true;
        options.DefaultCacheDuration = TimeSpan.FromMinutes(10);
    });
```

## Writing Secrets

```csharp
public class SecretManager(ISecretWriter secretWriter)
{
    public async Task StoreConnectionStringAsync(string value, CancellationToken ct)
    {
        var result = await secretWriter.SetSecretAsync("db-connection", value, ct);
        result.IfLeft(error => logger.LogError("Failed: {Code}", error.GetCode()));
    }
}
```

## Error Handling

All Azure SDK exceptions are mapped to Encina error codes:

| HTTP Status | Error Code | Description |
|-------------|------------|-------------|
| 404 | `secrets.not_found` | Secret does not exist |
| 401, 403 | `secrets.access_denied` | Insufficient permissions |
| Other | `secrets.provider_unavailable` | Network or service errors |

```csharp
var result = await secretReader.GetSecretAsync("my-secret", ct);
result.Match(
    Right: value => Console.WriteLine(value),
    Left: error =>
    {
        var code = error.GetCode().IfNone("");
        if (code == SecretsErrors.NotFoundCode)
            Console.WriteLine("Secret not found");
        else if (code == SecretsErrors.AccessDeniedCode)
            Console.WriteLine("Access denied - check RBAC permissions");
    });
```

## Health Check

```csharp
services.AddAzureKeyVaultSecrets(
    new Uri("https://my-vault.vault.azure.net/"),
    configureSecrets: options =>
    {
        options.ProviderHealthCheck = true;
        options.HealthCheckSecretName = "health-probe";
    });
```

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `Azure.Security.KeyVault.Secrets` | 4.8.0 | Azure Key Vault SDK |
| `Azure.Identity` | 1.17.1 | Authentication |
| `Encina.Security.Secrets` | Latest | Core abstractions |

## Requirements

- .NET 10.0
- Azure Key Vault instance
- Appropriate RBAC permissions (Key Vault Secrets User/Officer)
