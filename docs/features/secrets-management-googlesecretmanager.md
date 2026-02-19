# Secrets Management -- Google Cloud Secret Manager

This guide covers the Google Cloud Secret Manager integration in Encina via the `Encina.Secrets.GoogleSecretManager` package. It explains prerequisites, authentication via Application Default Credentials, IAM roles, version handling, configuration, error mapping, health checks, and best practices specific to Google Cloud Secret Manager.

## Table of Contents

1. [Overview](#overview)
2. [Prerequisites](#prerequisites)
3. [Authentication](#authentication)
4. [IAM Roles](#iam-roles)
5. [Configuration](#configuration)
6. [Version Handling](#version-handling)
7. [SetSecretAsync Behavior](#setsecretasync-behavior)
8. [Error Mapping](#error-mapping)
9. [Health Check](#health-check)
10. [Quick Start](#quick-start)
11. [Testing and Emulators](#testing-and-emulators)
12. [Best Practices](#best-practices)

---

## Overview

`Encina.Secrets.GoogleSecretManager` provides an `ISecretProvider` implementation backed by the Google Cloud Secret Manager client library (`Google.Cloud.SecretManager.V1`). It wraps `SecretManagerServiceClient` to expose a unified, Railway Oriented Programming (ROP) API that returns `Either<EncinaError, T>` for all operations.

| Component | Description |
|-----------|-------------|
| **`GoogleSecretManagerProvider`** | `ISecretProvider` implementation that delegates to `SecretManagerServiceClient` |
| **`GoogleSecretManagerOptions`** | Configuration: `ProjectId`, `ProviderHealthCheck` |
| **`GoogleSecretManagerHealthCheck`** | ASP.NET Core health check verifying Secret Manager connectivity |
| **`AddEncinaGoogleSecretManager`** | Extension method to register all services |

### NuGet Package

```
Encina.Secrets.GoogleSecretManager
```

**Dependencies**: `Google.Cloud.SecretManager.V1`, `Encina.Secrets` (core abstractions).

---

## Prerequisites

1. **GCP Project** -- An active Google Cloud project with billing enabled.
2. **Secret Manager API Enabled** -- The Secret Manager API must be enabled for your project.
3. **IAM Permissions** -- The identity running your application must have the appropriate Secret Manager IAM roles.

### Enable the Secret Manager API

```bash
gcloud services enable secretmanager.googleapis.com --project=my-project
```

---

## Authentication

The Google Cloud client library uses **Application Default Credentials (ADC)** to authenticate. The `AddEncinaGoogleSecretManager` extension method calls `SecretManagerServiceClient.Create()`, which resolves credentials through the ADC chain.

### ADC Resolution Order

| Order | Source | Environment |
|-------|--------|-------------|
| 1 | `GOOGLE_APPLICATION_CREDENTIALS` environment variable | Points to a service account JSON key file |
| 2 | gcloud CLI credentials (`gcloud auth application-default login`) | Local development |
| 3 | Attached service account (Compute Engine, App Engine, Cloud Run) | GCP compute services |
| 4 | Workload Identity (GKE) | Kubernetes pods on GKE |
| 5 | Workload Identity Federation | Non-GCP environments (AWS, Azure, on-premises) |

### Local Development

```bash
# Authenticate with gcloud for local development
gcloud auth application-default login --project=my-project
```

### Service Account JSON Key

```bash
# Set the environment variable to a service account key file
export GOOGLE_APPLICATION_CREDENTIALS="/path/to/service-account.json"
```

> **Warning**: Avoid using service account keys in production. Use attached service accounts or Workload Identity instead.

### Workload Identity (GKE)

1. Create a GCP service account with Secret Manager permissions.
2. Create a Kubernetes service account.
3. Bind them with Workload Identity:

```bash
gcloud iam service-accounts add-iam-policy-binding \
  my-sa@my-project.iam.gserviceaccount.com \
  --role roles/iam.workloadIdentityUser \
  --member "serviceAccount:my-project.svc.id.goog[namespace/k8s-sa]"
```

4. Annotate the Kubernetes service account:

```yaml
apiVersion: v1
kind: ServiceAccount
metadata:
  name: k8s-sa
  annotations:
    iam.gke.io/gcp-service-account: my-sa@my-project.iam.gserviceaccount.com
```

---

## IAM Roles

The following predefined IAM roles are relevant for Secret Manager:

| Role | Permissions | Use Case |
|------|-------------|----------|
| `roles/secretmanager.admin` | Full control over secrets and their versions | Administrative tools, infrastructure automation |
| `roles/secretmanager.secretAccessor` | `secretmanager.versions.access` | Applications that only read secret values |
| `roles/secretmanager.secretVersionManager` | Create, disable, enable, destroy versions | Applications that write new secret versions |
| `roles/secretmanager.viewer` | List and view secret metadata (not values) | Monitoring, auditing |

### Required Permissions by Operation

| Encina Operation | Required IAM Permission |
|------------------|------------------------|
| `GetSecretAsync` | `secretmanager.versions.access` |
| `GetSecretVersionAsync` | `secretmanager.versions.access` |
| `SetSecretAsync` | `secretmanager.secrets.create`, `secretmanager.versions.add` |
| `DeleteSecretAsync` | `secretmanager.secrets.delete` |
| `ListSecretsAsync` | `secretmanager.secrets.list` |
| `ExistsAsync` | `secretmanager.secrets.get` |
| Health check | `secretmanager.secrets.list` |

### Grant Roles via gcloud

```bash
# Read-only access
gcloud projects add-iam-policy-binding my-project \
  --member="serviceAccount:my-sa@my-project.iam.gserviceaccount.com" \
  --role="roles/secretmanager.secretAccessor"

# Read and write access
gcloud projects add-iam-policy-binding my-project \
  --member="serviceAccount:my-sa@my-project.iam.gserviceaccount.com" \
  --role="roles/secretmanager.secretVersionManager"

# Full admin access
gcloud projects add-iam-policy-binding my-project \
  --member="serviceAccount:my-sa@my-project.iam.gserviceaccount.com" \
  --role="roles/secretmanager.admin"
```

---

## Configuration

`GoogleSecretManagerOptions` exposes the following settings:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ProjectId` | `string` | `""` | The GCP project ID (e.g., `my-gcp-project`). **Required.** Used to construct resource names. |
| `ProviderHealthCheck` | `ProviderHealthCheckOptions` | Disabled | Health check configuration (see [Health Check](#health-check)). |

### Resource Name Format

Google Cloud Secret Manager uses structured resource names:

| Resource | Format |
|----------|--------|
| Secret | `projects/{projectId}/secrets/{secretName}` |
| Secret version | `projects/{projectId}/secrets/{secretName}/versions/{version}` |

The `ProjectId` is used to construct these resource names internally. You only pass the secret name (e.g., `"my-secret"`) to the `ISecretProvider` methods.

---

## Version Handling

Google Cloud Secret Manager versions are sequential integers starting from `1`. Each call to `AddSecretVersion` creates a new version.

| Concept | Description |
|---------|-------------|
| **`latest`** | Special alias for the most recent enabled version |
| **Numeric version** | Specific version number (e.g., `"1"`, `"2"`, `"15"`) |
| **Version states** | `ENABLED`, `DISABLED`, `DESTROYED` |

### How Encina Uses Versions

- **`GetSecretAsync`**: Always retrieves the `latest` version by constructing resource name `projects/{project}/secrets/{name}/versions/latest`.
- **`GetSecretVersionAsync`**: Passes the `version` parameter directly into the resource name (`projects/{project}/secrets/{name}/versions/{version}`).
- **`SetSecretAsync`**: Returns the version number of the newly created version in `SecretMetadata.Version`, extracted from the response resource name.

### Version ID Extraction

The provider extracts the version ID from the full resource name using `SecretVersionName.Parse`:

```
projects/my-project/secrets/my-secret/versions/3  -->  "3"
```

### Example

```csharp
// Get the latest version
var latest = await provider.GetSecretAsync("my-secret", ct);

// Get a specific version
var v2 = await provider.GetSecretVersionAsync("my-secret", "2", ct);

// "latest" also works as a version string
var alsoLatest = await provider.GetSecretVersionAsync("my-secret", "latest", ct);
```

---

## SetSecretAsync Behavior

`SetSecretAsync` implements a create-or-update pattern:

1. **Check if the secret exists** -- Calls `GetSecret` to verify the secret resource exists.
2. **Create if `NotFound`** -- If the secret does not exist, calls `CreateSecret` with:
   - **Automatic replication** (replicates across all GCP regions).
   - **Labels** from `SecretOptions.Tags` (if provided).
3. **Add a new version** -- Calls `AddSecretVersion` with the secret value encoded as UTF-8 bytes.
4. **Return metadata** -- Returns a `SecretMetadata` with the new version ID.

### Replication Policy

Secrets created by Encina always use **Automatic** replication (Google manages replica placement). If you need user-managed replication (specific regions), create the secret manually before calling `SetSecretAsync`.

### Labels vs Tags

Google Cloud Secret Manager uses "labels" (key-value pairs on the secret resource). Encina maps `SecretOptions.Tags` to labels when creating a new secret. Labels are **not updated** on existing secrets.

---

## Error Mapping

`GoogleSecretManagerProvider` translates gRPC `RpcException` into `EncinaError` using `SecretsErrorCodes`:

| gRPC Status Code | Encina Error Code | Description |
|------------------|-------------------|-------------|
| `NotFound` | `encina.secrets.not_found` | Secret does not exist in the project |
| `PermissionDenied` | `encina.secrets.access_denied` | Insufficient IAM permissions |
| Any other code | `encina.secrets.provider_unavailable` | Network error, quota exceeded, or other GCP failure |

For versioned access (`GetSecretVersionAsync`), a `NotFound` maps to `encina.secrets.version_not_found`.

### Example Error Handling

```csharp
var result = await provider.GetSecretAsync("db-password", cancellationToken);

result.Match(
    Right: secret => logger.LogInformation("Secret version: {Version}", secret.Version),
    Left: error => error.Code switch
    {
        SecretsErrorCodes.NotFoundCode => logger.LogWarning("Secret not found in project"),
        SecretsErrorCodes.AccessDeniedCode => logger.LogError("IAM permission denied"),
        _ => logger.LogError("GCP error: {Message}", error.Message)
    });
```

---

## Health Check

`GoogleSecretManagerHealthCheck` verifies connectivity by calling `ListSecrets` with `PageSize = 1`. It reads one secret from the first page to confirm the API is accessible.

| Property | Value |
|----------|-------|
| **Name** | `encina-secrets-gcp` |
| **Tags** | `["encina", "secrets", "gcp", "ready"]` |
| **Healthy** | `ListSecrets` call succeeds (even if the project has no secrets) |
| **Unhealthy** | `RpcException` thrown (includes gRPC `StatusCode` in description) |

### Enabling the Health Check

```csharp
services.AddEncinaGoogleSecretManager(options =>
{
    options.ProjectId = "my-gcp-project";
    options.ProviderHealthCheck = new ProviderHealthCheckOptions
    {
        Enabled = true,
        Tags = ["encina", "secrets", "gcp", "ready"]
    };
});
```

> **Permission requirement**: The health check calls `ListSecrets`, which requires the `secretmanager.secrets.list` IAM permission.

---

## Quick Start

### 1. Install the Package

```bash
dotnet add package Encina.Secrets.GoogleSecretManager
```

### 2. Register Services

```csharp
using Encina.Secrets.GoogleSecretManager;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEncinaGoogleSecretManager(options =>
{
    options.ProjectId = "my-gcp-project";
    // Authentication via Application Default Credentials (automatic)
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
        // Read a secret (latest version)
        var result = await _secrets.GetSecretAsync("db-password", ct);

        result.Match(
            Right: secret => Console.WriteLine($"Value: {secret.Value}, Version: {secret.Version}"),
            Left: error => Console.WriteLine($"Error [{error.Code}]: {error.Message}"));

        // Read a specific version
        var v1 = await _secrets.GetSecretVersionAsync("db-password", "1", ct);

        // Write a secret (creates if not exists, adds new version if exists)
        var setResult = await _secrets.SetSecretAsync(
            "api-key",
            "sk-new-api-key-value",
            new SecretOptions(
                Tags: new Dictionary<string, string>
                {
                    ["environment"] = "production",
                    ["team"] = "backend"
                }),
            ct);

        // Delete a secret (permanent: removes all versions)
        var deleteResult = await _secrets.DeleteSecretAsync("old-secret", ct);

        // List all secret names in the project
        var listResult = await _secrets.ListSecretsAsync(ct);

        // Check existence
        var existsResult = await _secrets.ExistsAsync("db-password", ct);
    }
}
```

---

## Testing and Emulators

### No Official Emulator

As of this writing, Google Cloud Secret Manager does not provide an official local emulator. Testing strategies include:

| Strategy | Description | Best For |
|----------|-------------|----------|
| **Dedicated dev project** | Create a separate GCP project for development/testing | Integration tests |
| **Service account key** | Use a service account with limited permissions in a dev project | CI/CD pipelines |
| **Mock `ISecretProvider`** | Mock the Encina interface in unit tests | Unit tests |
| **Mock `SecretManagerServiceClient`** | Mock the Google client for integration-level tests | Contract tests |

### Unit Testing with Mocked Provider

```csharp
// Using any mocking framework
var mockProvider = Substitute.For<ISecretProvider>();
mockProvider.GetSecretAsync("test-secret", Arg.Any<CancellationToken>())
    .Returns(new Secret("test-secret", "test-value", "1", null));

var service = new MyService(mockProvider);
await service.DoWorkAsync(CancellationToken.None);
```

### Integration Testing with Dev Project

```bash
# Set up a dedicated test project
gcloud projects create my-test-project --name="Encina Test"
gcloud services enable secretmanager.googleapis.com --project=my-test-project

# Create a service account for CI
gcloud iam service-accounts create encina-ci \
  --project=my-test-project \
  --display-name="Encina CI"

gcloud projects add-iam-policy-binding my-test-project \
  --member="serviceAccount:encina-ci@my-test-project.iam.gserviceaccount.com" \
  --role="roles/secretmanager.admin"

# Export key for CI (store securely)
gcloud iam service-accounts keys create key.json \
  --iam-account=encina-ci@my-test-project.iam.gserviceaccount.com
```

---

## Best Practices

| Practice | Rationale |
|----------|-----------|
| **Use Workload Identity in GKE** | Eliminates service account keys; pods authenticate via Kubernetes service account binding |
| **Use attached service accounts on Compute Engine / Cloud Run** | Automatic credential resolution without key files |
| **Avoid service account key files in production** | Key files are long-lived credentials that require manual rotation |
| **Enable audit logging** | Cloud Audit Logs record all Secret Manager API calls; required for compliance |
| **Apply least-privilege IAM** | Use `secretAccessor` for read-only workloads; `admin` only for infrastructure tooling |
| **Use secret-level IAM bindings** | Grant access to individual secrets rather than project-wide when possible |
| **Enable health checks in production** | Detect API connectivity or permission issues before they impact users |
| **Set up secret rotation** | Use Cloud Functions or Pub/Sub notifications to automate secret rotation |
| **Use labels for organization** | Apply labels via `SecretOptions.Tags` to categorize secrets by environment, team, or application |
| **Monitor with Cloud Monitoring** | Set up alerts on Secret Manager API errors, latency, and quota usage |
| **Use Automatic replication** | Default in Encina; Google manages replication across regions for availability |
| **Destroy unused versions** | Disable and then destroy old secret versions to reduce storage and exposure |

---

## Related Documentation

- [Secrets Management Overview](../features/secrets-management.md) (if available)
- [Encina.Secrets Core Abstractions](../../src/Encina.Secrets/) -- `ISecretProvider`, `Secret`, `SecretMetadata`, `SecretsErrorCodes`
- [Google Cloud Secret Manager Documentation](https://cloud.google.com/secret-manager/docs)
- [Google.Cloud.SecretManager.V1 NuGet](https://www.nuget.org/packages/Google.Cloud.SecretManager.V1/)
- [Application Default Credentials](https://cloud.google.com/docs/authentication/application-default-credentials)
