# Encina.Security.Secrets.GoogleCloudSecretManager

Google Cloud Secret Manager provider for the Encina secrets management system.

## Quick Start

```csharp
// Basic setup with Application Default Credentials
services.AddGoogleCloudSecretManager(
    gcp => gcp.ProjectId = "my-gcp-project");

// With caching configuration
services.AddGoogleCloudSecretManager(
    gcp => gcp.ProjectId = "my-gcp-project",
    secrets =>
    {
        secrets.EnableCaching = true;
        secrets.DefaultCacheDuration = TimeSpan.FromMinutes(10);
    });
```

## Features

- **ISP-compliant**: Implements `ISecretReader`, `ISecretWriter`, and `ISecretRotator` separately
- **ROP pattern**: All operations return `Either<EncinaError, T>` -- no exceptions for business logic
- **Create-or-update**: `SetSecretAsync` uses `AddSecretVersion` with automatic fallback to `CreateSecret`
- **Caching**: Automatic in-memory caching via core decorator (enabled by default)
- **Observability**: Tracing, metrics, health checks, and auditing inherited from the core package
- **Thread-safe**: `SecretManagerServiceClient` is safe for concurrent use

## Authentication

When no custom `SecretManagerServiceClient` is pre-registered, Application Default Credentials (ADC)
are used automatically:

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

## Error Mapping

| gRPC Status Code | Encina Error Code | Description |
|------------------|-------------------|-------------|
| `NotFound` | `secrets.not_found` | Secret does not exist |
| `PermissionDenied` | `secrets.access_denied` | Insufficient IAM permissions |
| Other `RpcException` | `secrets.provider_unavailable` | Network, quota, or other GCP failure |

## Dependencies

- `Google.Cloud.SecretManager.V1` (v2.7.0)
- `Encina.Security.Secrets` (core abstractions)
