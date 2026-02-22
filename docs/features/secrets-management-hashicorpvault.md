# Secrets Management -- HashiCorp Vault

This guide covers the HashiCorp Vault integration in Encina via the `Encina.Security.Secrets.HashiCorpVault` package. It explains prerequisites, authentication methods, KV v2 engine specifics, configuration, error mapping, and best practices specific to HashiCorp Vault.

## Table of Contents

1. [Overview](#overview)
2. [Prerequisites](#prerequisites)
3. [Authentication Methods](#authentication-methods)
4. [Configuration](#configuration)
5. [KV v2 Engine Specifics](#kv-v2-engine-specifics)
6. [Secret Value Extraction](#secret-value-extraction)
7. [Error Mapping](#error-mapping)
8. [Quick Start](#quick-start)
9. [Docker Setup](#docker-setup)
10. [Best Practices](#best-practices)

---

## Overview

`Encina.Security.Secrets.HashiCorpVault` provides ISP-compliant secret management backed by VaultSharp, targeting the **KV v2** (Key-Value version 2) secrets engine. It wraps `IVaultClient` to expose a unified, Railway Oriented Programming (ROP) API that returns `Either<EncinaError, T>` for all operations.

| Component | Description |
|-----------|-------------|
| **`HashiCorpVaultSecretProvider`** | Implements `ISecretReader`, `ISecretWriter`, and `ISecretRotator` using VaultSharp's KV v2 API |
| **`HashiCorpVaultOptions`** | Configuration: `VaultAddress`, `MountPoint`, `AuthMethod` |
| **`AddHashiCorpVaultSecrets`** | Extension method to register all services |

### NuGet Package

```
Encina.Security.Secrets.HashiCorpVault
```

**Dependencies**: `VaultSharp`, `Encina.Security.Secrets` (core abstractions).

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

services.AddHashiCorpVaultSecrets(options =>
{
    options.VaultAddress = "https://vault.example.com:8200";
    options.AuthMethod = new TokenAuthMethodInfo("hvs.your-vault-token");
});
```

### AppRole Authentication

Recommended for machine-to-machine authentication in production:

```csharp
using VaultSharp.V1.AuthMethods.AppRole;

services.AddHashiCorpVaultSecrets(options =>
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

services.AddHashiCorpVaultSecrets(options =>
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
| `GetSecretAsync(string)` | `ReadSecretAsync` | `GET secret/data/<name>` |
| `GetSecretAsync<T>(string)` | `ReadSecretAsync` + JSON deserialization | `GET secret/data/<name>` |
| `SetSecretAsync(string, string)` | `WriteSecretAsync` | `POST secret/data/<name>` |
| `RotateSecretAsync(string)` | `ReadSecretAsync` + `WriteSecretAsync` | Read then write to create new version |

> **Note on versioning**: KV v2 automatically creates a new version on every write. `SetSecretAsync` and `RotateSecretAsync` both leverage this behavior.

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

---

## Error Mapping

`HashiCorpVaultSecretProvider` translates `VaultApiException` into `EncinaError` using `SecretsErrors`:

| Vault HTTP Status | Encina Error Code | Description |
|-------------------|-------------------|-------------|
| `404 NotFound` | `secrets.not_found` | Secret path does not exist |
| `403 Forbidden` | `secrets.access_denied` | Insufficient Vault policy permissions |
| Any other status | `secrets.provider_unavailable` | Network error, sealed vault, or other Vault failure |

For rotation failures (`RotateSecretAsync`), any `VaultApiException` maps to `secrets.rotation_failed`.

For typed deserialization (`GetSecretAsync<T>`), invalid JSON maps to `secrets.deserialization_failed`.

### Example Error Handling

```csharp
var result = await secretReader.GetSecretAsync("app/config", cancellationToken);

result.Match(
    Right: value => logger.LogInformation("Secret value retrieved"),
    Left: error => error.Code switch
    {
        "secrets.not_found" => logger.LogWarning("Secret not found in Vault"),
        "secrets.access_denied" => logger.LogError("Vault policy denied access"),
        _ => logger.LogError("Vault error: {Message}", error.Message)
    });
```

---

## Quick Start

### 1. Install the Package

```bash
dotnet add package Encina.Security.Secrets.HashiCorpVault
```

### 2. Register Services

#### With Token Auth (Development)

```csharp
using Encina.Security.Secrets.HashiCorpVault;
using VaultSharp.V1.AuthMethods.Token;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHashiCorpVaultSecrets(options =>
{
    options.VaultAddress = "http://localhost:8200";
    options.MountPoint = "secret"; // default
    options.AuthMethod = new TokenAuthMethodInfo("hvs.dev-root-token");
});
```

#### With AppRole Auth (Production)

```csharp
using Encina.Security.Secrets.HashiCorpVault;
using VaultSharp.V1.AuthMethods.AppRole;

builder.Services.AddHashiCorpVaultSecrets(options =>
{
    options.VaultAddress = "https://vault.production.internal:8200";
    options.AuthMethod = new AppRoleAuthMethodInfo(
        roleId: Environment.GetEnvironmentVariable("VAULT_ROLE_ID")!,
        secretId: new SecretIdInfo(Environment.GetEnvironmentVariable("VAULT_SECRET_ID")!));
});
```

#### With Kubernetes Auth

```csharp
using Encina.Security.Secrets.HashiCorpVault;
using VaultSharp.V1.AuthMethods.Kubernetes;

builder.Services.AddHashiCorpVaultSecrets(options =>
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
    private readonly ISecretReader _secretReader;
    private readonly ISecretWriter _secretWriter;

    public MyService(ISecretReader secretReader, ISecretWriter secretWriter)
    {
        _secretReader = secretReader;
        _secretWriter = secretWriter;
    }

    public async Task DoWorkAsync(CancellationToken ct)
    {
        // Read a secret (returns the string value)
        var result = await _secretReader.GetSecretAsync("app/db-password", ct);

        result.Match(
            Right: value => Console.WriteLine($"Value: {value}"),
            Left: error => Console.WriteLine($"Error [{error.Code}]: {error.Message}"));

        // Read and deserialize a typed secret
        var typed = await _secretReader.GetSecretAsync<DatabaseConfig>("app/db-config", ct);

        // Write a secret (creates a new version in KV v2)
        var setResult = await _secretWriter.SetSecretAsync(
            "app/api-key",
            "sk-new-key-value",
            ct);
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
| **Use auto-unseal** | Configure auto-unseal with a cloud KMS (AWS KMS, Azure Key Vault, GCP Cloud KMS) to avoid manual unseal operations |
| **Set max versions** | Configure `max_versions` on the KV v2 engine to prevent unbounded version growth |
| **Use the `secret` mount point** | Stick with the default `"secret"` mount point unless you have a specific organizational reason to change it |

---

## Related Documentation

- [Secrets Management Overview](../features/secrets-management.md)
- [Encina.Security.Secrets Core Abstractions](../../src/Encina.Security.Secrets/) -- `ISecretReader`, `ISecretWriter`, `ISecretRotator`, `SecretsErrors`
- [HashiCorp Vault Documentation](https://developer.hashicorp.com/vault/docs)
- [VaultSharp GitHub](https://github.com/rajanadar/VaultSharp)
- [KV v2 Secrets Engine](https://developer.hashicorp.com/vault/docs/secrets/kv/kv-v2)
