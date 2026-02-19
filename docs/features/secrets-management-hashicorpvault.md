# Secrets Management -- HashiCorp Vault

This guide covers the HashiCorp Vault integration in Encina via the `Encina.Secrets.HashiCorpVault` package. It explains prerequisites, authentication methods, KV v2 engine specifics, configuration, error mapping, health checks, and best practices specific to HashiCorp Vault.

## Table of Contents

1. [Overview](#overview)
2. [Prerequisites](#prerequisites)
3. [Authentication Methods](#authentication-methods)
4. [Configuration](#configuration)
5. [KV v2 Engine Specifics](#kv-v2-engine-specifics)
6. [Secret Value Extraction](#secret-value-extraction)
7. [Error Mapping](#error-mapping)
8. [ListSecretsAsync Behavior](#listsecretsasync-behavior)
9. [Version Handling](#version-handling)
10. [Health Check](#health-check)
11. [Quick Start](#quick-start)
12. [Docker Setup](#docker-setup)
13. [Best Practices](#best-practices)

---

## Overview

`Encina.Secrets.HashiCorpVault` provides an `ISecretProvider` implementation backed by VaultSharp, targeting the **KV v2** (Key-Value version 2) secrets engine. It wraps `IVaultClient` to expose a unified, Railway Oriented Programming (ROP) API that returns `Either<EncinaError, T>` for all operations.

| Component | Description |
|-----------|-------------|
| **`HashiCorpVaultProvider`** | `ISecretProvider` implementation using VaultSharp's KV v2 API |
| **`HashiCorpVaultOptions`** | Configuration: `VaultAddress`, `MountPoint`, `AuthMethod`, `ProviderHealthCheck` |
| **`HashiCorpVaultHealthCheck`** | ASP.NET Core health check verifying Vault connectivity, initialization, and seal status |
| **`AddEncinaHashiCorpVault`** | Extension method to register all services |

### NuGet Package

```
Encina.Secrets.HashiCorpVault
```

**Dependencies**: `VaultSharp`, `Encina.Secrets` (core abstractions).

---

## Prerequisites

1. **Vault Server** -- A running HashiCorp Vault instance, either:
   - **Self-hosted**: Vault OSS or Enterprise on your infrastructure.
   - **HCP Vault** (HashiCorp Cloud Platform): Managed Vault as a service.
2. **KV v2 Engine Enabled** -- The KV v2 secrets engine must be enabled at a known mount point (default: `secret`).
3. **Authentication Credentials** -- A valid auth method configured (token, AppRole, Kubernetes, etc.).

### Verify KV v2 Is Enabled

```bash
vault secrets list
# Should show:
# Path          Type     Description
# secret/       kv       key-value v2 secrets engine
```

If not enabled:

```bash
vault secrets enable -path=secret kv-v2
```

---

## Authentication Methods

`HashiCorpVaultOptions.AuthMethod` accepts any `IAuthMethodInfo` implementation from VaultSharp. This property is **required** -- the provider will throw `InvalidOperationException` at startup if it is not set.

### Token Authentication

The simplest method; suitable for development and CI/CD:

```csharp
using VaultSharp.V1.AuthMethods.Token;

services.AddEncinaHashiCorpVault(options =>
{
    options.VaultAddress = "https://vault.example.com:8200";
    options.AuthMethod = new TokenAuthMethodInfo("hvs.your-vault-token");
});
```

### AppRole Authentication

Recommended for machine-to-machine authentication in production:

```csharp
using VaultSharp.V1.AuthMethods.AppRole;

services.AddEncinaHashiCorpVault(options =>
{
    options.VaultAddress = "https://vault.example.com:8200";
    options.AuthMethod = new AppRoleAuthMethodInfo(
        roleId: "your-role-id",
        secretId: new SecretIdInfo("your-secret-id"));
});
```

### Kubernetes Authentication

For workloads running in Kubernetes:

```csharp
using VaultSharp.V1.AuthMethods.Kubernetes;

services.AddEncinaHashiCorpVault(options =>
{
    options.VaultAddress = "https://vault.example.com:8200";
    options.AuthMethod = new KubernetesAuthMethodInfo(
        roleName: "my-app-role",
        jwt: File.ReadAllText("/var/run/secrets/kubernetes.io/serviceaccount/token"));
});
```

### Other Supported Methods

VaultSharp supports many additional auth methods. Any `IAuthMethodInfo` implementation works:

| Method | VaultSharp Class | Use Case |
|--------|-----------------|----------|
| LDAP | `LDAPAuthMethodInfo` | Active Directory / LDAP integration |
| Userpass | `UserPassAuthMethodInfo` | Username/password (development) |
| AWS IAM | `IAMAWSAuthMethodInfo` | AWS workloads authenticating via IAM |
| Azure | `AzureAuthMethodInfo` | Azure workloads |
| GitHub | `GitHubAuthMethodInfo` | CI/CD pipelines |
| TLS Certificate | `CertAuthMethodInfo` | Mutual TLS authentication |

---

## Configuration

`HashiCorpVaultOptions` exposes the following settings:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `VaultAddress` | `string` | `""` | Vault server address (e.g., `https://vault.example.com:8200`). **Required.** |
| `MountPoint` | `string` | `"secret"` | Mount point of the KV v2 secrets engine. |
| `AuthMethod` | `IAuthMethodInfo?` | `null` | Authentication method. **Required** -- throws `InvalidOperationException` if null at startup. |
| `ProviderHealthCheck` | `ProviderHealthCheckOptions` | Disabled | Health check configuration (see [Health Check](#health-check)). |

---

## KV v2 Engine Specifics

The KV v2 engine is a versioned key-value store. Key differences from KV v1:

| Feature | KV v1 | KV v2 |
|---------|-------|-------|
| Versioning | No versioning | Automatic versioning (configurable max versions) |
| Soft delete | No | Yes (`DeleteVersions` vs `DestroyVersions`) |
| Metadata | No | Yes (creation time, version number, custom metadata) |
| Check-and-set | No | Yes (CAS for optimistic concurrency) |
| API path | `secret/data/<path>` | `secret/data/<path>` (data), `secret/metadata/<path>` (metadata) |

### How Encina Maps to KV v2

| Encina Operation | VaultSharp Method | KV v2 Path |
|------------------|-------------------|------------|
| `GetSecretAsync` | `ReadSecretAsync` | `GET secret/data/<name>` |
| `GetSecretVersionAsync` | `ReadSecretAsync(version: N)` | `GET secret/data/<name>?version=N` |
| `SetSecretAsync` | `WriteSecretAsync` | `POST secret/data/<name>` |
| `DeleteSecretAsync` | `DeleteMetadataAsync` | `DELETE secret/metadata/<name>` (permanent) |
| `ListSecretsAsync` | `ReadSecretPathsAsync` | `LIST secret/metadata/` |
| `ExistsAsync` | `ReadSecretMetadataAsync` | `GET secret/metadata/<name>` |

> **Note on deletion**: `DeleteSecretAsync` calls `DeleteMetadataAsync`, which permanently deletes all versions and metadata. This is different from `DeleteSecretVersionsAsync` which only soft-deletes specific versions.

---

## Secret Value Extraction

KV v2 stores secrets as a `Dictionary<string, object>`. The Encina provider extracts a single string value using this logic:

1. **Look for a `"data"` key** -- If the dictionary contains a key named `"data"` with a string value, that value is returned.
2. **Fallback to JSON** -- If no `"data"` key exists (or the value is not a string), the entire dictionary is serialized to JSON and returned.

### Writing Secrets

When you call `SetSecretAsync(name, value)`, the provider writes:

```json
{
  "data": "<your-value>"
}
```

If you also provide tags via `SecretOptions.Tags`, they are stored as additional keys in the same data dictionary:

```json
{
  "data": "<your-value>",
  "environment": "production",
  "team": "backend"
}
```

---

## Error Mapping

`HashiCorpVaultProvider` translates `VaultApiException` into `EncinaError` using `SecretsErrorCodes`:

| Vault HTTP Status | Encina Error Code | Description |
|-------------------|-------------------|-------------|
| `404 NotFound` | `encina.secrets.not_found` | Secret path does not exist |
| `403 Forbidden` | `encina.secrets.access_denied` | Insufficient Vault policy permissions |
| Any other status | `encina.secrets.provider_unavailable` | Network error, sealed vault, or other Vault failure |

For versioned access (`GetSecretVersionAsync`):
- `404` maps to `encina.secrets.version_not_found`.
- Non-integer version strings return `encina.secrets.version_not_found` immediately (VaultSharp requires integer versions).

### Example Error Handling

```csharp
var result = await provider.GetSecretAsync("app/config", cancellationToken);

result.Match(
    Right: secret => logger.LogInformation("Secret version: {Version}", secret.Version),
    Left: error => error.Code switch
    {
        SecretsErrorCodes.NotFoundCode => logger.LogWarning("Secret not found in Vault"),
        SecretsErrorCodes.AccessDeniedCode => logger.LogError("Vault policy denied access"),
        _ => logger.LogError("Vault error: {Message}", error.Message)
    });
```

---

## ListSecretsAsync Behavior

When `ListSecretsAsync` is called and the KV v2 engine has no secrets (or the path does not exist), Vault returns a `404` response. The Encina provider handles this intentionally:

- **`404` returns an empty list** (not an error).
- This is by design -- an empty vault is a valid state, not a failure.

All other errors (403, 5xx) are mapped to their respective `EncinaError` codes.

---

## Version Handling

KV v2 versions are **integers** (1, 2, 3, ...). The `ISecretProvider` interface uses **strings** for version identifiers. The provider handles conversion:

1. The `version` parameter from `GetSecretVersionAsync` is parsed as `int` using `int.TryParse`.
2. If parsing fails (e.g., the version is `"abc"` or a UUID), the provider returns `encina.secrets.version_not_found` immediately without making a Vault API call.
3. Version numbers returned in `Secret.Version` and `SecretMetadata.Version` are formatted as strings using `CultureInfo.InvariantCulture`.

```csharp
// Valid: integer version as string
var v2 = await provider.GetSecretVersionAsync("my-secret", "2", ct);

// Invalid: non-integer returns VersionNotFound immediately
var bad = await provider.GetSecretVersionAsync("my-secret", "abc", ct);
// bad is Left(encina.secrets.version_not_found)
```

---

## Health Check

`HashiCorpVaultHealthCheck` verifies Vault health by calling `GetHealthStatusAsync()` (the `/sys/health` endpoint). It evaluates both initialization and seal status:

| Condition | Result | Description |
|-----------|--------|-------------|
| Initialized and unsealed | **Healthy** | Vault is fully operational |
| Sealed | **Degraded** | Vault is sealed and cannot serve requests |
| Not initialized | **Degraded** | Vault has not been initialized |
| Exception thrown | **Unhealthy** | Vault is not accessible (network error, wrong address) |

| Property | Value |
|----------|-------|
| **Name** | `encina-secrets-vault` |
| **Tags** | `["encina", "secrets", "vault", "ready"]` |

### Enabling the Health Check

```csharp
services.AddEncinaHashiCorpVault(options =>
{
    options.VaultAddress = "https://vault.example.com:8200";
    options.AuthMethod = new TokenAuthMethodInfo("hvs.my-token");
    options.ProviderHealthCheck = new ProviderHealthCheckOptions
    {
        Enabled = true,
        Tags = ["encina", "secrets", "vault", "ready"]
    };
});
```

> **Note**: The `/sys/health` endpoint may be accessible even without authentication on some Vault configurations. The health check does not require secret-level permissions.

---

## Quick Start

### 1. Install the Package

```bash
dotnet add package Encina.Secrets.HashiCorpVault
```

### 2. Register Services

#### With Token Auth (Development)

```csharp
using Encina.Secrets.HashiCorpVault;
using VaultSharp.V1.AuthMethods.Token;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEncinaHashiCorpVault(options =>
{
    options.VaultAddress = "http://localhost:8200";
    options.MountPoint = "secret"; // default
    options.AuthMethod = new TokenAuthMethodInfo("hvs.dev-root-token");
    options.ProviderHealthCheck = new ProviderHealthCheckOptions { Enabled = true };
});
```

#### With AppRole Auth (Production)

```csharp
using Encina.Secrets.HashiCorpVault;
using VaultSharp.V1.AuthMethods.AppRole;

builder.Services.AddEncinaHashiCorpVault(options =>
{
    options.VaultAddress = "https://vault.production.internal:8200";
    options.AuthMethod = new AppRoleAuthMethodInfo(
        roleId: Environment.GetEnvironmentVariable("VAULT_ROLE_ID")!,
        secretId: new SecretIdInfo(Environment.GetEnvironmentVariable("VAULT_SECRET_ID")!));
});
```

#### With Kubernetes Auth

```csharp
using Encina.Secrets.HashiCorpVault;
using VaultSharp.V1.AuthMethods.Kubernetes;

builder.Services.AddEncinaHashiCorpVault(options =>
{
    options.VaultAddress = "https://vault.vault.svc.cluster.local:8200";
    options.AuthMethod = new KubernetesAuthMethodInfo(
        roleName: "my-app",
        jwt: File.ReadAllText("/var/run/secrets/kubernetes.io/serviceaccount/token"));
});
```

### 3. Use the Provider

```csharp
public class MyService
{
    private readonly ISecretProvider _secrets;

    public MyService(ISecretProvider secrets) => _secrets = secrets;

    public async Task DoWorkAsync(CancellationToken ct)
    {
        // Read a secret (latest version)
        var result = await _secrets.GetSecretAsync("app/db-password", ct);

        result.Match(
            Right: secret => Console.WriteLine($"Value: {secret.Value}, Version: {secret.Version}"),
            Left: error => Console.WriteLine($"Error [{error.Code}]: {error.Message}"));

        // Read a specific version
        var v1 = await _secrets.GetSecretVersionAsync("app/db-password", "1", ct);

        // Write a secret (creates new version in KV v2)
        var setResult = await _secrets.SetSecretAsync(
            "app/api-key",
            "sk-new-key-value",
            new SecretOptions(
                Tags: new Dictionary<string, string>
                {
                    ["environment"] = "production"
                }),
            ct);

        // Delete a secret (permanent: removes all versions and metadata)
        var deleteResult = await _secrets.DeleteSecretAsync("app/old-secret", ct);

        // List all secret paths at the root
        var listResult = await _secrets.ListSecretsAsync(ct);

        // Check existence
        var existsResult = await _secrets.ExistsAsync("app/db-password", ct);
    }
}
```

---

## Docker Setup

For local development and testing, run Vault in dev mode:

```bash
docker run -d \
  --name vault \
  -p 8200:8200 \
  -e 'VAULT_DEV_ROOT_TOKEN_ID=dev-root-token' \
  -e 'VAULT_DEV_LISTEN_ADDRESS=0.0.0.0:8200' \
  hashicorp/vault:latest server -dev
```

The dev server:
- Starts unsealed and initialized.
- Has a KV v2 engine enabled at `secret/` by default.
- Uses the specified root token for authentication.
- Stores data in memory (lost on restart).

### Verify the Dev Server

```bash
export VAULT_ADDR='http://127.0.0.1:8200'
export VAULT_TOKEN='dev-root-token'

# Write a test secret
vault kv put secret/test-secret data="hello-world"

# Read it back
vault kv get secret/test-secret
```

---

## Best Practices

| Practice | Rationale |
|----------|-----------|
| **Use AppRole or Kubernetes auth in production** | Avoid long-lived tokens; AppRole supports secret ID rotation and Kubernetes auth leverages pod identity |
| **Never use the root token in production** | Root tokens have unlimited access; generate scoped tokens or use machine auth methods |
| **Apply least-privilege Vault policies** | Create policies that grant only the specific paths your application needs |
| **Enable audit logging** | Vault audit logs record every request/response; essential for compliance and security investigations |
| **Use namespaces (Enterprise)** | Vault Enterprise namespaces provide multi-tenancy and delegation |
| **Rotate SecretIDs regularly** | When using AppRole, configure `secret_id_ttl` and `secret_id_num_uses` for automatic expiration |
| **Enable health checks in production** | Detect sealed vault, network issues, or uninitialized state before they impact users |
| **Use auto-unseal** | Configure auto-unseal with a cloud KMS (AWS KMS, Azure Key Vault, GCP Cloud KMS) to avoid manual unseal operations |
| **Set max versions** | Configure `max_versions` on the KV v2 engine to prevent unbounded version growth |
| **Use the `secret` mount point** | Stick with the default `"secret"` mount point unless you have a specific organizational reason to change it |

---

## Related Documentation

- [Secrets Management Overview](../features/secrets-management.md) (if available)
- [Encina.Secrets Core Abstractions](../../src/Encina.Secrets/) -- `ISecretProvider`, `Secret`, `SecretMetadata`, `SecretsErrorCodes`
- [HashiCorp Vault Documentation](https://developer.hashicorp.com/vault/docs)
- [VaultSharp GitHub](https://github.com/rajanadar/VaultSharp)
- [KV v2 Secrets Engine](https://developer.hashicorp.com/vault/docs/secrets/kv/kv-v2)
