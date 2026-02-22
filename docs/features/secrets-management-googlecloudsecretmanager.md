# Secrets Management -- Google Cloud Secret Manager

This guide covers the Google Cloud Secret Manager integration in Encina via the `Encina.Security.Secrets.GoogleCloudSecretManager` package. It explains prerequisites, IAM roles, credential resolution, configuration, error mapping, and best practices specific to Google Cloud Secret Manager.

## Table of Contents

1. [Overview](#overview)
2. [Prerequisites](#prerequisites)
3. [IAM Roles](#iam-roles)
4. [Credential Chain](#credential-chain)
5. [Configuration](#configuration)
6. [Error Mapping](#error-mapping)
7. [SetSecretAsync Behavior](#setsecretasync-behavior)
8. [RotateSecretAsync Behavior](#rotatesecretasync-behavior)
9. [Quick Start](#quick-start)
10. [Best Practices](#best-practices)

---

## Overview

`Encina.Security.Secrets.GoogleCloudSecretManager` provides an ISP-compliant implementation backed by the Google Cloud SDK for .NET (`Google.Cloud.SecretManager.V1` v2.7.0). It wraps `SecretManagerServiceClient` to expose a unified, Railway Oriented Programming (ROP) API that returns `Either<EncinaError, T>` for all operations.

| Component | Description |
|-----------|-------------|
| **`GoogleCloudSecretManagerProvider`** | Implements `ISecretReader`, `ISecretWriter`, `ISecretRotator` via `SecretManagerServiceClient` |
| **`GoogleCloudSecretManagerOptions`** | Configuration: `ProjectId` (string, **required**) |
| **`AddGoogleCloudSecretManager`** | Extension method to register all services with decorator chain |

### NuGet Package

```
Encina.Security.Secrets.GoogleCloudSecretManager
```

**Dependencies**: `Google.Cloud.SecretManager.V1` (v2.7.0), `Encina.Security.Secrets` (core abstractions).

---

## Prerequisites

1. **GCP Project** -- An active Google Cloud Platform project with billing enabled.
2. **Secret Manager API Enabled** -- The Secret Manager API must be enabled in the project.
3. **IAM Permissions** -- The identity (service account, user account, or workload identity) must have the required Secret Manager permissions.

### Enable the Secret Manager API

```bash
gcloud services enable secretmanager.googleapis.com --project=my-gcp-project
```

---

## IAM Roles

The following IAM roles are relevant to `GoogleCloudSecretManagerProvider`:

| IAM Role | Permissions Included | Used By |
|----------|---------------------|---------|
| `roles/secretmanager.secretAccessor` | `secretmanager.versions.access` | `GetSecretAsync`, `RotateSecretAsync` (read) |
| `roles/secretmanager.secretVersionAdder` | `secretmanager.versions.add` | `SetSecretAsync`, `RotateSecretAsync` (write) |
| `roles/secretmanager.admin` | All Secret Manager permissions | Full read/write/create/delete |

### Minimum Required Permissions

| Permission | Used By | Purpose |
|------------|---------|---------|
| `secretmanager.versions.access` | `GetSecretAsync`, `GetSecretAsync<T>`, `RotateSecretAsync` | Read secret values |
| `secretmanager.versions.add` | `SetSecretAsync`, `RotateSecretAsync` | Add new secret versions |
| `secretmanager.secrets.create` | `SetSecretAsync` (fallback) | Create new secret containers |

### Example IAM Policy Binding

```bash
# Grant read access
gcloud secrets add-iam-policy-binding my-secret \
  --member="serviceAccount:my-app@my-project.iam.gserviceaccount.com" \
  --role="roles/secretmanager.secretAccessor"

# Grant write access
gcloud secrets add-iam-policy-binding my-secret \
  --member="serviceAccount:my-app@my-project.iam.gserviceaccount.com" \
  --role="roles/secretmanager.secretVersionAdder"
```

---

## Credential Chain

When no custom `SecretManagerServiceClient` is pre-registered in DI, the provider creates one using `SecretManagerServiceClient.Create()`, which resolves credentials through Application Default Credentials (ADC):

| Order | Source | Environment |
|-------|--------|-------------|
| 1 | `GOOGLE_APPLICATION_CREDENTIALS` env var | CI/CD pipelines |
| 2 | gcloud CLI credentials | Local development (`gcloud auth application-default login`) |
| 3 | Attached service account | GCE, GKE, Cloud Run, Cloud Functions |
| 4 | Workload Identity | GKE with Workload Identity Federation |

### Custom Credentials

Pre-register a configured client before calling `AddGoogleCloudSecretManager`:

```csharp
// With explicit service account key
var client = new SecretManagerServiceClientBuilder
{
    CredentialsPath = "/path/to/service-account.json"
}.Build();

services.AddSingleton(client);
services.AddGoogleCloudSecretManager(
    gcp => gcp.ProjectId = "my-gcp-project");
```

The extension method uses `TryAddSingleton`, so a pre-registered client takes precedence over the default.

---

## Configuration

`GoogleCloudSecretManagerOptions` exposes the following settings:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ProjectId` | `string` | `""` | GCP project ID. **Required** -- throws `InvalidOperationException` at startup if empty or whitespace. |

### Why Only ProjectId?

Unlike other cloud providers that expose SDK-specific configuration (region, credentials, client config), the Google Cloud SDK resolves all connection details through Application Default Credentials. The `ProjectId` is the only mandatory setting because it determines which GCP project's secrets to access.

For advanced client configuration (custom credentials, gRPC channel options, etc.), pre-register a configured `SecretManagerServiceClient` in DI before calling `AddGoogleCloudSecretManager`.

---

## Error Mapping

`GoogleCloudSecretManagerProvider` translates gRPC exceptions into `EncinaError` using `SecretsErrors`:

| gRPC Status Code | Encina Error Code | Description |
|------------------|-------------------|-------------|
| `NotFound` | `secrets.not_found` | Secret does not exist |
| `PermissionDenied` | `secrets.access_denied` | Insufficient IAM permissions |
| Other `RpcException` | `secrets.provider_unavailable` | Network error, quota, or other GCP failure |
| Any exception during rotation | `secrets.rotation_failed` | Rotation operation failed |
| `JsonException` during typed deserialization | `secrets.deserialization_failed` | Secret value is not valid JSON for the target type |

### Example Error Handling

```csharp
var result = await secretReader.GetSecretAsync("db-password", cancellationToken);

result.Match(
    Right: value => logger.LogInformation("Secret value retrieved"),
    Left: error => error.GetCode().IfNone("unknown") switch
    {
        "secrets.not_found" => logger.LogWarning("Secret not found"),
        "secrets.access_denied" => logger.LogError("IAM permission denied"),
        _ => logger.LogError("GCP error: {Message}", error.Message)
    });
```

---

## SetSecretAsync Behavior

`SetSecretAsync` implements a create-or-update pattern:

1. **Try `AddSecretVersion`** -- Attempts to add a new version to an existing secret.
2. **On `StatusCode.NotFound`** -- The secret container does not exist. Falls back to:
   1. `CreateSecret` with automatic replication to create the secret container.
   2. `AddSecretVersion` to add the first version.

This means `SetSecretAsync` is idempotent: calling it for a secret that does not yet exist will create it automatically.

### Replication Policy

When creating a new secret, the provider uses **automatic replication** (`Replication.Types.Automatic`), which lets Google Cloud manage where secret data is stored. For user-managed replication, create the secret via `gcloud` or the GCP console first.

---

## RotateSecretAsync Behavior

`RotateSecretAsync` performs a read-then-write rotation:

1. **Read** -- Retrieves the current secret value via `AccessSecretVersion` (using the `latest` version alias).
2. **Write** -- Writes the raw data back as a new version via `AddSecretVersion`.

In practice, the `SecretRotationCoordinator` (from the core package) generates the new value via `ISecretRotationHandler` and writes it through `ISecretWriter`.

> **Note**: For GCP-native rotation (using Cloud Functions or Cloud Run triggers), configure rotation schedules and topics directly in the GCP console or via `gcloud secrets update`.

---

## Quick Start

### 1. Install the Package

```bash
dotnet add package Encina.Security.Secrets.GoogleCloudSecretManager
```

### 2. Register Services

```csharp
using Encina.Security.Secrets.GoogleCloudSecretManager;

var builder = WebApplication.CreateBuilder(args);

// Basic setup with Application Default Credentials
builder.Services.AddGoogleCloudSecretManager(
    gcp => gcp.ProjectId = "my-gcp-project");

// With caching configuration
builder.Services.AddGoogleCloudSecretManager(
    gcp => gcp.ProjectId = "my-gcp-project",
    secrets =>
    {
        secrets.EnableCaching = true;
        secrets.DefaultCacheDuration = TimeSpan.FromMinutes(10);
    });
```

### 3. Use the Provider

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

// Typed deserialization
public class ConfigService(ISecretReader secretReader)
{
    public async Task<DbConfig> GetDatabaseConfigAsync(CancellationToken ct)
    {
        var result = await secretReader.GetSecretAsync<DbConfig>("db-config", ct);

        return result.Match(
            Right: config => config,
            Left: error => throw new InvalidOperationException(error.Message));
    }
}

public class DbConfig
{
    public string Host { get; set; } = "";
    public int Port { get; set; }
    public string Database { get; set; } = "";
}
```

---

## Best Practices

| Practice | Rationale |
|----------|-----------|
| **Use Workload Identity in GKE** | Eliminates service account key files; pods authenticate via Kubernetes service accounts mapped to GCP service accounts |
| **Use attached service accounts** | GCE, Cloud Run, and Cloud Functions automatically provide credentials via the metadata server |
| **Apply least-privilege IAM** | Grant `secretAccessor` for read-only, `secretVersionAdder` for write; avoid `admin` role in production |
| **Enable audit logging** | Cloud Audit Logs record every Secret Manager API call; essential for compliance |
| **Use secret labels** | Apply labels for cost allocation, environment tracking, and automated lifecycle management |
| **Enable caching** | Use `EnableCaching = true` to reduce API calls and latency |
| **Organize with naming conventions** | Name secrets descriptively (e.g., `db-password`, `api-key-stripe`) for clarity |
| **Set automatic replication** | Use automatic replication (the default) unless regulatory requirements mandate specific regions |
| **Monitor quota usage** | Secret Manager has per-project quotas for API calls; monitor via Cloud Monitoring |
| **Destroy unused versions** | Secret Manager retains all versions; periodically destroy old versions to reduce storage costs |

---

## Related Documentation

- [Secrets Management Overview](secrets-management.md) -- Core abstractions and architecture
- [Azure Key Vault Provider](secrets-management-azurekeyvault.md) -- Azure Key Vault integration
- [AWS Secrets Manager Provider](secrets-management-awssecretsmanager.md) -- AWS Secrets Manager integration
- [HashiCorp Vault Provider](secrets-management-hashicorpvault.md) -- HashiCorp Vault integration
- [Encina.Security.Secrets](../../src/Encina.Security.Secrets/) -- `ISecretReader`, `ISecretWriter`, `ISecretRotator`, `SecretsErrors`
- [Google Cloud Secret Manager Documentation](https://cloud.google.com/secret-manager/docs)
- [Google.Cloud.SecretManager.V1 NuGet](https://www.nuget.org/packages/Google.Cloud.SecretManager.V1/)
