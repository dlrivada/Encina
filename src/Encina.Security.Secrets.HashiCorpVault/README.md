# Encina.Security.Secrets.HashiCorpVault

HashiCorp Vault provider for [Encina](https://github.com/dlrivada/Encina) secrets management, targeting the KV v2 secrets engine.

## Features

- **ISP-compliant**: Implements `ISecretReader`, `ISecretWriter`, `ISecretRotator` separately
- **Multiple auth methods**: Token, AppRole, Kubernetes, LDAP, and more via VaultSharp
- **KV v2 engine**: Automatic versioning, soft delete, and metadata support
- **Railway Oriented Programming**: All operations return `Either<EncinaError, T>`
- **Decorator chain**: Caching, auditing, tracing, and metrics applied transparently

## Quick Start

```csharp
using Encina.Security.Secrets.HashiCorpVault;
using VaultSharp.V1.AuthMethods.Token;

// Register with token auth (development)
services.AddHashiCorpVaultSecrets(vault =>
{
    vault.VaultAddress = "http://localhost:8200";
    vault.AuthMethod = new TokenAuthMethodInfo("hvs.dev-root-token");
});

// Register with AppRole auth (production)
services.AddHashiCorpVaultSecrets(
    vault =>
    {
        vault.VaultAddress = "https://vault.example.com:8200";
        vault.AuthMethod = new AppRoleAuthMethodInfo(roleId, new SecretIdInfo(secretId));
    },
    secrets =>
    {
        secrets.EnableCaching = true;
        secrets.DefaultCacheDuration = TimeSpan.FromMinutes(5);
    });

// Use via ISP interfaces
public class MyService(ISecretReader secretReader)
{
    public async Task<string> GetApiKeyAsync(CancellationToken ct)
    {
        var result = await secretReader.GetSecretAsync("app/api-key", ct);
        return result.Match(
            Right: value => value,
            Left: error => throw new InvalidOperationException(error.Message));
    }
}
```

## Configuration

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `VaultAddress` | `string` | `""` | Vault server URL (required) |
| `AuthMethod` | `IAuthMethodInfo?` | `null` | Authentication method (required) |
| `MountPoint` | `string` | `"secret"` | KV v2 mount point |

## Error Mapping

| Vault HTTP Status | Encina Error Code |
|-------------------|-------------------|
| 404 Not Found | `secrets.not_found` |
| 403 Forbidden | `secrets.access_denied` |
| Other errors | `secrets.provider_unavailable` |

## Documentation

- [HashiCorp Vault Provider Guide](../../docs/features/secrets-management-hashicorpvault.md)
- [Secrets Management Overview](../../docs/features/secrets-management.md)
