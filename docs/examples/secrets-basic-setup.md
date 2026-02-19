# Secrets Management -- Basic Setup

This guide shows the minimal setup to start using Encina Secrets with each supported cloud provider. Every provider implements the same `ISecretProvider` interface, so the consuming code stays identical regardless of backend.

## Prerequisites

Install the core package plus the provider package you need:

```bash
# Core (always required)
dotnet add package Encina.Secrets

# Pick ONE provider:
dotnet add package Encina.Secrets.AzureKeyVault
dotnet add package Encina.Secrets.AWSSecretsManager
dotnet add package Encina.Secrets.HashiCorpVault
dotnet add package Encina.Secrets.GoogleSecretManager
```

## Provider Registration

### Azure Key Vault

```csharp
using Encina.Secrets.AzureKeyVault;

builder.Services.AddEncinaKeyVaultSecrets(options =>
{
    options.VaultUri = "https://my-vault.vault.azure.net/";
    // Uses DefaultAzureCredential when Credential is null (default).
    // Override for specific scenarios:
    // options.Credential = new ManagedIdentityCredential();
});
```

### AWS Secrets Manager

```csharp
using Encina.Secrets.AWSSecretsManager;

builder.Services.AddEncinaAWSSecretsManager(options =>
{
    options.Region = "us-east-1";
    // Uses the default AWS credential chain when Credentials is null.
    // Override for explicit credentials:
    // options.Credentials = new BasicAWSCredentials("key", "secret");
});
```

### HashiCorp Vault

```csharp
using Encina.Secrets.HashiCorpVault;
using VaultSharp.V1.AuthMethods.Token;

builder.Services.AddEncinaHashiCorpVault(options =>
{
    options.VaultAddress = "https://vault.example.com:8200";
    options.MountPoint = "secret";             // KV v2 mount point (default: "secret")
    options.AuthMethod = new TokenAuthMethodInfo("hvs.my-vault-token");
    // Other auth methods:
    // options.AuthMethod = new AppRoleAuthMethodInfo("role-id", "secret-id");
    // options.AuthMethod = new KubernetesAuthMethodInfo("my-role", jwt);
});
```

### Google Cloud Secret Manager

```csharp
using Encina.Secrets.GoogleSecretManager;

builder.Services.AddEncinaGoogleSecretManager(options =>
{
    options.ProjectId = "my-gcp-project";
    // Uses Application Default Credentials (ADC) automatically.
    // Set GOOGLE_APPLICATION_CREDENTIALS or use gcloud auth for local dev.
});
```

## Using ISecretProvider

All providers implement the same `ISecretProvider` interface. Inject it and call any method. Every operation returns `Either<EncinaError, T>` following Encina's Railway Oriented Programming pattern.

### Retrieve a Secret

```csharp
using Encina.Secrets;

public class MyService
{
    private readonly ISecretProvider _secretProvider;

    public MyService(ISecretProvider secretProvider)
        => _secretProvider = secretProvider;

    public async Task ConnectToDatabaseAsync(CancellationToken ct)
    {
        var result = await _secretProvider.GetSecretAsync("database-connection", ct);

        result.Match(
            Right: secret => Console.WriteLine(
                $"Got secret: {secret.Name} v{secret.Version}"),
            Left: error => Console.WriteLine(
                $"Error: {error.Message} (Code: {error.GetCode()})")
        );
    }
}
```

### Retrieve a Specific Version

```csharp
var result = await secretProvider.GetSecretVersionAsync("api-key", "3", ct);

result.Match(
    Right: secret => Console.WriteLine($"Value: {secret.Value}, Expires: {secret.ExpiresAtUtc}"),
    Left: error => Console.WriteLine($"Version not found: {error.Message}")
);
```

### Create or Update a Secret

```csharp
var result = await secretProvider.SetSecretAsync(
    "api-key",
    "sk-new-value-here",
    new SecretOptions(
        ExpiresAtUtc: DateTime.UtcNow.AddDays(90),
        Tags: new Dictionary<string, string> { ["env"] = "production" }),
    ct);

result.Match(
    Right: metadata => Console.WriteLine(
        $"Created {metadata.Name} v{metadata.Version} at {metadata.CreatedAtUtc}"),
    Left: error => Console.WriteLine($"Failed: {error.Message}")
);
```

### Check Existence and Delete

```csharp
// Check if a secret exists
var exists = await secretProvider.ExistsAsync("old-api-key", ct);
exists.Match(
    Right: found => Console.WriteLine(found ? "Secret exists" : "Secret not found"),
    Left: error => Console.WriteLine($"Check failed: {error.Message}")
);

// Delete a secret
var deleted = await secretProvider.DeleteSecretAsync("old-api-key", ct);
deleted.Match(
    Right: _ => Console.WriteLine("Secret deleted"),
    Left: error => Console.WriteLine($"Delete failed: {error.Message}")
);
```

### List All Secrets

```csharp
var list = await secretProvider.ListSecretsAsync(ct);
list.Match(
    Right: names =>
    {
        foreach (var name in names)
            Console.WriteLine($"  - {name}");
    },
    Left: error => Console.WriteLine($"List failed: {error.Message}")
);
```

## Adding Caching and Instrumentation

Decorators are registered **after** the provider. They wrap the existing `ISecretProvider` transparently.

```csharp
// 1. Register the provider
builder.Services.AddEncinaKeyVaultSecrets(options =>
{
    options.VaultUri = "https://my-vault.vault.azure.net/";
});

// 2. Add caching (wraps the provider with CachedSecretProvider)
builder.Services.AddEncinaSecretsCaching(options =>
{
    options.DefaultTtl = TimeSpan.FromMinutes(10);
    options.Enabled = true;
});

// 3. Add instrumentation (wraps with InstrumentedSecretProvider)
builder.Services.AddEncinaSecretsInstrumentation(options =>
{
    options.EnableTracing = true;
    options.EnableMetrics = true;
    options.RecordSecretNames = false; // Default: false for security
});
```

The decoration order matters. With the registration above, the call chain is:

```
Caller --> InstrumentedSecretProvider --> CachedSecretProvider --> KeyVaultSecretProvider
```

Instrumentation captures metrics for both cache hits and actual provider calls.

## Enabling Health Checks

Each provider supports optional health checks:

```csharp
builder.Services.AddEncinaKeyVaultSecrets(options =>
{
    options.VaultUri = "https://my-vault.vault.azure.net/";
    options.ProviderHealthCheck.Enabled = true;
    options.ProviderHealthCheck.Tags = ["encina", "secrets", "ready"];
});
```

Health checks integrate with the standard ASP.NET Core health check system and can be exposed via `/health` endpoints.

## The Secret Record

The `Secret` record returned by read operations contains:

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | The secret name |
| `Value` | `string` | The secret value |
| `Version` | `string?` | Version identifier (null if versioning not supported) |
| `ExpiresAtUtc` | `DateTime?` | UTC expiration time (null if no expiration) |

The `SecretMetadata` record returned by write operations contains:

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | The secret name |
| `Version` | `string` | Version identifier assigned to the new version |
| `CreatedAtUtc` | `DateTime` | UTC timestamp of creation |
| `ExpiresAtUtc` | `DateTime?` | UTC expiration time (null if no expiration) |

## Related

- [IConfiguration Integration](secrets-configuration-integration.md)
- [Caching Strategy](secrets-caching-strategy.md)
- [Error Handling with ROP](secrets-error-handling.md)
