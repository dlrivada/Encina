# Secrets Management -- Azure Key Vault

This guide covers the Azure Key Vault integration in Encina via the `Encina.Security.Secrets.AzureKeyVault` package. It explains prerequisites, authentication strategies, configuration, error handling, health checks, and best practices specific to Azure Key Vault.

## Table of Contents

1. [Overview](#overview)
2. [Prerequisites](#prerequisites)
3. [Authentication](#authentication)
4. [Configuration](#configuration)
5. [Managed Identity Setup](#managed-identity-setup)
6. [RBAC Setup Guide](#rbac-setup-guide)
7. [Error Mapping](#error-mapping)
8. [Health Check](#health-check)
9. [Quick Start](#quick-start)
10. [Best Practices](#best-practices)

---

## Overview

`Encina.Security.Secrets.AzureKeyVault` provides ISP-compliant implementations of `ISecretReader`, `ISecretWriter`, and `ISecretRotator` backed by the Azure Key Vault Secrets SDK (`Azure.Security.KeyVault.Secrets`). It wraps the `SecretClient` to expose a unified, Railway Oriented Programming (ROP) API that returns `Either<EncinaError, T>` for all operations.

| Component | Description |
|-----------|-------------|
| **`AzureKeyVaultSecretProvider`** | Implements `ISecretReader`, `ISecretWriter`, `ISecretRotator` — delegates to `SecretClient` |
| **`AzureKeyVaultOptions`** | Configuration: `VaultUri`, `Credential`, `ClientOptions` |
| **`AddAzureKeyVaultSecrets`** | Extension method to register all services and plug into the core decorator chain |

### NuGet Package

```
Encina.Security.Secrets.AzureKeyVault
```

**Dependencies**: `Azure.Security.KeyVault.Secrets` (v4.8.0), `Azure.Identity` (v1.17.1), `Encina.Security.Secrets` (core abstractions).

---

## Prerequisites

1. **Azure Subscription** -- An active Azure subscription.
2. **Azure Key Vault resource** -- A Key Vault instance provisioned in your subscription.
3. **RBAC permissions** -- The identity running your application must have one of the following built-in roles:

| Role | Permissions | Use Case |
|------|-------------|----------|
| **Key Vault Secrets Officer** | Full CRUD on secrets | Applications that create, update, and delete secrets |
| **Key Vault Secrets User** | Read-only access to secrets | Applications that only read secrets |
| **Key Vault Administrator** | Full management including access policies | Infrastructure/DevOps tooling |

> **Note**: RBAC (Role-Based Access Control) is the recommended authorization model. Vault access policies are a legacy mechanism.

---

## Authentication

The `AzureKeyVaultOptions.Credential` property accepts any `Azure.Core.TokenCredential` implementation. When `null` (the default), `DefaultAzureCredential` is used automatically.

### DefaultAzureCredential (Recommended)

`DefaultAzureCredential` attempts authentication through a chain of mechanisms in order:

| Order | Mechanism | Environment |
|-------|-----------|-------------|
| 1 | Environment variables | CI/CD pipelines |
| 2 | Workload Identity | Kubernetes (AKS) |
| 3 | Managed Identity | Azure App Service, Functions, AKS, VMs |
| 4 | Azure CLI | Local development (`az login`) |
| 5 | Azure PowerShell | Local development (`Connect-AzAccount`) |
| 6 | Visual Studio | Local development (signed-in account) |
| 7 | Azure Developer CLI | Local development (`azd auth login`) |

```csharp
// DefaultAzureCredential is used when Credential is null (default)
services.AddAzureKeyVaultSecrets(
    new Uri("https://my-vault.vault.azure.net/"));
```

### ManagedIdentityCredential

For production workloads running on Azure infrastructure:

```csharp
services.AddAzureKeyVaultSecrets(
    new Uri("https://my-vault.vault.azure.net/"),
    kv => kv.Credential = new ManagedIdentityCredential());

// For user-assigned identity:
services.AddAzureKeyVaultSecrets(
    new Uri("https://my-vault.vault.azure.net/"),
    kv => kv.Credential = new ManagedIdentityCredential("client-id-of-user-assigned-identity"));
```

### ClientSecretCredential

For service-to-service scenarios outside Azure (not recommended for production):

```csharp
services.AddAzureKeyVaultSecrets(
    new Uri("https://my-vault.vault.azure.net/"),
    kv => kv.Credential = new ClientSecretCredential(
        tenantId: "your-tenant-id",
        clientId: "your-client-id",
        clientSecret: "your-client-secret"));
```

> **Warning**: Avoid storing client secrets in code or configuration files. Use environment variables or a secure configuration provider.

---

## Configuration

### AzureKeyVaultOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `VaultUri` | `Uri?` | `null` | The Key Vault URI (e.g., `https://my-vault.vault.azure.net/`). Set by the extension method. |
| `Credential` | `TokenCredential?` | `null` | Token credential for authentication. When `null`, `DefaultAzureCredential` is used. |
| `ClientOptions` | `SecretClientOptions?` | `null` | Custom options for the underlying `SecretClient` (retry policy, diagnostics, etc.). |

### SecretsOptions (from core package)

The `configureSecrets` parameter controls core decorator behavior:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `EnableCaching` | `bool` | `true` | Wrap `ISecretReader` with `CachedSecretReaderDecorator` |
| `DefaultCacheDuration` | `TimeSpan` | `5 minutes` | Default cache TTL for secrets |
| `EnableTracing` | `bool` | `false` | Enable OpenTelemetry tracing via `SecretsActivitySource` |
| `EnableMetrics` | `bool` | `false` | Enable OpenTelemetry metrics via `SecretsMetrics` |
| `EnableAccessAuditing` | `bool` | `false` | Wrap `ISecretReader` with `AuditedSecretReaderDecorator` |
| `ProviderHealthCheck` | `bool` | `false` | Register `SecretsHealthCheck` for ASP.NET Core health endpoints |

---

## Managed Identity Setup

### Azure App Service

1. Navigate to your App Service in the Azure Portal.
2. Go to **Identity** > **System assigned** > set Status to **On**.
3. Copy the Object (principal) ID.
4. Assign the Key Vault Secrets User role (see [RBAC Setup Guide](#rbac-setup-guide)).

### Azure Kubernetes Service (AKS)

1. Enable workload identity on your AKS cluster.
2. Create a user-assigned managed identity.
3. Create a federated identity credential linking the Kubernetes service account.
4. Assign the Key Vault Secrets User role to the managed identity.

### Azure Functions

1. Navigate to your Function App in the Azure Portal.
2. Go to **Identity** > **System assigned** > set Status to **On**.
3. Copy the Object (principal) ID.
4. Assign the Key Vault Secrets User role.

---

## RBAC Setup Guide

Grant the necessary role to your application's identity using the Azure CLI:

```bash
# Assign "Key Vault Secrets User" for read-only access
az role assignment create \
  --role "Key Vault Secrets User" \
  --assignee <principal-id> \
  --scope /subscriptions/<subscription-id>/resourceGroups/<rg>/providers/Microsoft.KeyVault/vaults/<vault-name>

# Assign "Key Vault Secrets Officer" for full CRUD access
az role assignment create \
  --role "Key Vault Secrets Officer" \
  --assignee <principal-id> \
  --scope /subscriptions/<subscription-id>/resourceGroups/<rg>/providers/Microsoft.KeyVault/vaults/<vault-name>
```

Replace `<principal-id>` with the Object ID of the managed identity, service principal, or user.

> **Important**: After assigning a role, it can take up to 5 minutes for the assignment to propagate.

---

## Error Mapping

`AzureKeyVaultSecretProvider` translates `RequestFailedException` from the Azure SDK into `EncinaError` using `SecretsErrors` factory methods:

| Azure HTTP Status | SecretsErrors Method | Error Code | Description |
|-------------------|---------------------|------------|-------------|
| `404` | `NotFound(secretName)` | `secrets.not_found` | Secret does not exist in the vault |
| `401` or `403` | `AccessDenied(secretName, message)` | `secrets.access_denied` | Insufficient permissions or invalid credentials |
| Any other status | `ProviderUnavailable("AzureKeyVault", ex)` | `secrets.provider_unavailable` | Network error, throttling, or other Azure failure |
| Rotation failure | `RotationFailed(secretName, message, ex)` | `secrets.rotation_failed` | Error during secret rotation |

Additionally, `GetSecretAsync<T>` can return:

| Scenario | SecretsErrors Method | Error Code |
|----------|---------------------|------------|
| JSON deserialization failure | `DeserializationFailed(secretName, typeof(T))` | `secrets.deserialization_failed` |

### Example Error Handling

```csharp
var result = await secretReader.GetSecretAsync("my-secret", cancellationToken);

result.Match(
    Right: value => logger.LogInformation("Retrieved secret: {Length} chars", value.Length),
    Left: error => error.Code switch
    {
        SecretsErrors.NotFoundCode => logger.LogWarning("Secret not found"),
        SecretsErrors.AccessDeniedCode => logger.LogError("Access denied to secret"),
        _ => logger.LogError("Provider error: {Message}", error.Message)
    });
```

---

## Health Check

Health checks are provided by the core `Encina.Security.Secrets` package via `SecretsHealthCheck`. When enabled, the health check verifies that `ISecretReader` can resolve and operate correctly.

### Enabling the Health Check

```csharp
services.AddAzureKeyVaultSecrets(
    new Uri("https://my-vault.vault.azure.net/"),
    configureSecrets: o => o.ProviderHealthCheck = true);
```

The health check is registered automatically by the core `AddEncinaSecrets<T>()` infrastructure.

---

## Quick Start

### 1. Install the Package

```bash
dotnet add package Encina.Security.Secrets.AzureKeyVault
```

### 2. Register Services

```csharp
using Encina.Security.Secrets.AzureKeyVault;

var builder = WebApplication.CreateBuilder(args);

// Basic setup with DefaultAzureCredential
builder.Services.AddAzureKeyVaultSecrets(
    new Uri("https://my-vault.vault.azure.net/"));

// With caching and custom credential
builder.Services.AddAzureKeyVaultSecrets(
    new Uri("https://my-vault.vault.azure.net/"),
    kv => kv.Credential = new ManagedIdentityCredential(),
    secrets =>
    {
        secrets.EnableCaching = true;
        secrets.DefaultCacheDuration = TimeSpan.FromMinutes(10);
    });
```

### 3. Read Secrets

```csharp
public class MyService(ISecretReader secretReader)
{
    public async Task<string> GetApiKeyAsync(CancellationToken ct)
    {
        var result = await secretReader.GetSecretAsync("api-key", ct);
        return result.Match(
            Right: value => value,
            Left: error => throw new InvalidOperationException(error.Message));
    }
}
```

### 4. Write Secrets

```csharp
public class SecretManager(ISecretWriter secretWriter)
{
    public async Task StoreConnectionStringAsync(string value, CancellationToken ct)
    {
        var result = await secretWriter.SetSecretAsync("db-connection-string", value, ct);
        result.Match(
            Right: _ => Console.WriteLine("Secret stored successfully"),
            Left: error => Console.WriteLine($"Error: {error.Message}"));
    }
}
```

### 5. Rotate Secrets

```csharp
public class RotationService(ISecretRotator secretRotator)
{
    public async Task RotateAsync(string secretName, CancellationToken ct)
    {
        var result = await secretRotator.RotateSecretAsync(secretName, ct);
        result.Match(
            Right: _ => Console.WriteLine("Secret rotated (new version created in Key Vault)"),
            Left: error => Console.WriteLine($"Rotation failed: {error.Message}"));
    }
}
```

### 6. Typed Secrets

```csharp
public record DatabaseConfig(string Host, int Port, string Password);

public class ConfigService(ISecretReader secretReader)
{
    public async Task<DatabaseConfig> GetDbConfigAsync(CancellationToken ct)
    {
        var result = await secretReader.GetSecretAsync<DatabaseConfig>("db-config", ct);
        return result.Match(
            Right: config => config,
            Left: error => throw new InvalidOperationException(error.Message));
    }
}
```

---

## Best Practices

| Practice | Rationale |
|----------|-----------|
| **Use Managed Identity in production** | Eliminates credential management; automatic token rotation |
| **Use RBAC over access policies** | RBAC is the modern authorization model; access policies are legacy |
| **Use `DefaultAzureCredential` for local development** | Supports `az login`, Visual Studio, and other local credential sources seamlessly |
| **Apply least-privilege roles** | Use Secrets User for read-only workloads; Secrets Officer only when writes are needed |
| **Enable soft-delete and purge protection** | Prevents accidental permanent deletion |
| **Enable caching for frequently accessed secrets** | Reduces latency and Key Vault API calls (enabled by default) |
| **Keep secrets close to consumers** | Deploy Key Vault in the same region as your application to minimize latency |
| **Monitor with Azure Diagnostics** | Enable Key Vault diagnostics logging for audit and troubleshooting |

---

## Observability

All observability features are inherited from the core `Encina.Security.Secrets` package through the decorator chain:

| Feature | Source | Opt-In |
|---------|--------|--------|
| **Tracing** | `SecretsActivitySource` (4 activities) | `EnableTracing = true` |
| **Metrics** | `SecretsMetrics` (5 instruments) | `EnableMetrics = true` |
| **Health checks** | `SecretsHealthCheck` | `ProviderHealthCheck = true` |
| **Access auditing** | `AuditedSecretReaderDecorator` | `EnableAccessAuditing = true` |
| **Caching** | `CachedSecretReaderDecorator` | `EnableCaching = true` (default) |
| **Provider logging** | `Log.cs` (EventIds 200-208) | Always active |

The satellite package provides its own structured logging via `LoggerMessage` source generators (EventIds 200-208) covering all provider-specific operations.

---

## Related Documentation

- [Secrets Management Overview](secrets-management.md)
- [Encina.Security.Secrets Core Abstractions](../../src/Encina.Security.Secrets/)
- [Azure Key Vault Documentation](https://learn.microsoft.com/en-us/azure/key-vault/)
- [Azure.Identity Documentation](https://learn.microsoft.com/en-us/dotnet/api/azure.identity)
