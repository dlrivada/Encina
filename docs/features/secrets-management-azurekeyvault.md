# Secrets Management -- Azure Key Vault

This guide covers the Azure Key Vault integration in Encina via the `Encina.Secrets.AzureKeyVault` package. It explains prerequisites, authentication strategies, configuration, error handling, health checks, and best practices specific to Azure Key Vault.

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

`Encina.Secrets.AzureKeyVault` provides an `ISecretProvider` implementation backed by the Azure Key Vault Secrets SDK (`Azure.Security.KeyVault.Secrets`). It wraps the `SecretClient` to expose a unified, Railway Oriented Programming (ROP) API that returns `Either<EncinaError, T>` for all operations.

| Component | Description |
|-----------|-------------|
| **`KeyVaultSecretProvider`** | `ISecretProvider` implementation that delegates to `SecretClient` |
| **`KeyVaultSecretProviderOptions`** | Configuration: `VaultUri`, `Credential`, `ProviderHealthCheck` |
| **`KeyVaultHealthCheck`** | ASP.NET Core health check verifying Key Vault connectivity |
| **`AddEncinaKeyVaultSecrets`** | Extension method to register all services |

### NuGet Package

```
Encina.Secrets.AzureKeyVault
```

**Dependencies**: `Azure.Security.KeyVault.Secrets`, `Azure.Identity`, `Encina.Secrets` (core abstractions).

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

The `KeyVaultSecretProviderOptions.Credential` property accepts any `Azure.Core.TokenCredential` implementation. When `null` (the default), `DefaultAzureCredential` is used automatically.

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
services.AddEncinaKeyVaultSecrets(options =>
{
    options.VaultUri = "https://my-vault.vault.azure.net/";
    // options.Credential = null; // DefaultAzureCredential used automatically
});
```

### ManagedIdentityCredential

For production workloads running on Azure infrastructure:

```csharp
services.AddEncinaKeyVaultSecrets(options =>
{
    options.VaultUri = "https://my-vault.vault.azure.net/";
    options.Credential = new ManagedIdentityCredential(); // System-assigned
    // or for user-assigned identity:
    // options.Credential = new ManagedIdentityCredential("client-id-of-user-assigned-identity");
});
```

### ClientSecretCredential

For service-to-service scenarios outside Azure (not recommended for production):

```csharp
services.AddEncinaKeyVaultSecrets(options =>
{
    options.VaultUri = "https://my-vault.vault.azure.net/";
    options.Credential = new ClientSecretCredential(
        tenantId: "your-tenant-id",
        clientId: "your-client-id",
        clientSecret: "your-client-secret");
});
```

> **Warning**: Avoid storing client secrets in code or configuration files. Use environment variables or a secure configuration provider.

---

## Configuration

`KeyVaultSecretProviderOptions` exposes the following settings:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `VaultUri` | `string` | `""` | The Key Vault URI (e.g., `https://my-vault.vault.azure.net/`). **Required.** |
| `Credential` | `TokenCredential?` | `null` | Token credential for authentication. When `null`, `DefaultAzureCredential` is used. |
| `ProviderHealthCheck` | `ProviderHealthCheckOptions` | Disabled | Health check configuration (see [Health Check](#health-check)). |

### ProviderHealthCheckOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Enabled` | `bool` | `false` | Whether to register the health check. |
| `Tags` | `IReadOnlyList<string>` | `["encina", "secrets", "ready"]` | Tags applied to the health check registration. |

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

`KeyVaultSecretProvider` translates `RequestFailedException` from the Azure SDK into `EncinaError` using `SecretsErrorCodes`:

| Azure HTTP Status | Encina Error Code | Description |
|-------------------|-------------------|-------------|
| `404` | `encina.secrets.not_found` | Secret does not exist in the vault |
| `401` or `403` | `encina.secrets.access_denied` | Insufficient permissions or invalid credentials |
| Any other status | `encina.secrets.provider_unavailable` | Network error, throttling, or other Azure failure |

For versioned access (`GetSecretVersionAsync`), a `404` maps to `encina.secrets.version_not_found` instead of `not_found`.

### Example Error Handling

```csharp
var result = await provider.GetSecretAsync("my-secret", cancellationToken);

result.Match(
    Right: secret => logger.LogInformation("Retrieved secret version {Version}", secret.Version),
    Left: error => error.Code switch
    {
        SecretsErrorCodes.NotFoundCode => logger.LogWarning("Secret not found"),
        SecretsErrorCodes.AccessDeniedCode => logger.LogError("Access denied to secret"),
        _ => logger.LogError("Provider error: {Message}", error.Message)
    });
```

---

## Health Check

`KeyVaultHealthCheck` verifies connectivity by calling `GetPropertiesOfSecretsAsync` (a lightweight list operation that breaks after the first result).

| Property | Value |
|----------|-------|
| **Name** | `encina-secrets-keyvault` |
| **Tags** | `["encina", "secrets", "keyvault", "ready"]` |
| **Healthy** | Key Vault is accessible |
| **Unhealthy** | `RequestFailedException` thrown (includes HTTP status in description) |

### Enabling the Health Check

```csharp
services.AddEncinaKeyVaultSecrets(options =>
{
    options.VaultUri = "https://my-vault.vault.azure.net/";
    options.ProviderHealthCheck = new ProviderHealthCheckOptions
    {
        Enabled = true,
        Tags = ["encina", "secrets", "keyvault", "ready"]
    };
});
```

> **Permission requirement**: The health check calls `GetPropertiesOfSecrets`, which requires the **Key Vault Secrets User** role at minimum.

---

## Quick Start

### 1. Install the Package

```bash
dotnet add package Encina.Secrets.AzureKeyVault
```

### 2. Register Services

```csharp
using Encina.Secrets.AzureKeyVault;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEncinaKeyVaultSecrets(options =>
{
    options.VaultUri = "https://my-vault.vault.azure.net/";
    // DefaultAzureCredential is used automatically
    options.ProviderHealthCheck = new ProviderHealthCheckOptions { Enabled = true };
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
        // Read a secret
        var result = await _secrets.GetSecretAsync("api-key", ct);

        result.Match(
            Right: secret => Console.WriteLine($"Key: {secret.Value}, Version: {secret.Version}"),
            Left: error => Console.WriteLine($"Error [{error.Code}]: {error.Message}"));

        // Write a secret with expiration
        var setResult = await _secrets.SetSecretAsync(
            "db-connection-string",
            "Server=...;Password=...",
            new SecretOptions(ExpiresAtUtc: DateTime.UtcNow.AddDays(90)),
            ct);

        // Delete a secret (starts soft-delete via StartDeleteSecretAsync)
        var deleteResult = await _secrets.DeleteSecretAsync("old-key", ct);

        // List all secret names
        var listResult = await _secrets.ListSecretsAsync(ct);

        // Check existence
        var existsResult = await _secrets.ExistsAsync("api-key", ct);
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
| **Enable soft-delete and purge protection** | Prevents accidental permanent deletion; `DeleteSecretAsync` triggers soft-delete via `StartDeleteSecretAsync` |
| **Enable health checks in production** | Detect connectivity issues before they impact users |
| **Keep secrets close to consumers** | Deploy Key Vault in the same region as your application to minimize latency |
| **Use tags for organization** | Pass tags via `SecretOptions.Tags` to categorize secrets by team, environment, or application |
| **Monitor with Azure Diagnostics** | Enable Key Vault diagnostics logging for audit and troubleshooting |

---

## Related Documentation

- [Secrets Management Overview](../features/secrets-management.md) (if available)
- [Encina.Secrets Core Abstractions](../../src/Encina.Secrets/) -- `ISecretProvider`, `Secret`, `SecretMetadata`, `SecretsErrorCodes`
- [Azure Key Vault Documentation](https://learn.microsoft.com/en-us/azure/key-vault/)
- [Azure.Identity Documentation](https://learn.microsoft.com/en-us/dotnet/api/azure.identity)
