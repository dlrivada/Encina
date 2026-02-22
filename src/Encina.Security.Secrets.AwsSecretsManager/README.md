# Encina.Security.Secrets.AwsSecretsManager

AWS Secrets Manager provider for the Encina secrets management system.

## Quick Start

```csharp
// Basic setup with default credential chain and region
services.AddAwsSecretsManager();

// With explicit region
services.AddAwsSecretsManager(
    aws => aws.Region = RegionEndpoint.USEast1);

// With explicit region and credentials
services.AddAwsSecretsManager(
    aws =>
    {
        aws.Region = RegionEndpoint.USEast1;
        aws.Credentials = new EnvironmentVariablesAWSCredentials();
    },
    secrets =>
    {
        secrets.EnableCaching = true;
        secrets.DefaultCacheDuration = TimeSpan.FromMinutes(10);
    });
```

## Features

- **ISP-compliant**: Implements `ISecretReader`, `ISecretWriter`, and `ISecretRotator` separately
- **ROP pattern**: All operations return `Either<EncinaError, T>` — no exceptions for business logic
- **Create-or-update**: `SetSecretAsync` uses `PutSecretValue` with automatic fallback to `CreateSecret`
- **Caching**: Automatic in-memory caching via core decorator (enabled by default)
- **Observability**: Tracing, metrics, health checks, and auditing inherited from the core package
- **Thread-safe**: `IAmazonSecretsManager` client is safe for concurrent use

## Authentication

When no credentials are configured, the AWS SDK default credential chain resolves credentials automatically:

| Order | Source | Environment |
|-------|--------|-------------|
| 1 | Environment variables | CI/CD pipelines |
| 2 | Shared credentials file | Local development |
| 3 | EC2 instance profile | EC2 instances |
| 4 | ECS task role | ECS/Fargate |
| 5 | EKS Pod Identity / IRSA | Kubernetes on EKS |

## Error Mapping

| AWS Exception | Encina Error Code | Description |
|---------------|-------------------|-------------|
| `ResourceNotFoundException` | `secrets.not_found` | Secret does not exist |
| `AccessDeniedException` error code | `secrets.access_denied` | Insufficient IAM permissions |
| Other `AmazonSecretsManagerException` | `secrets.provider_unavailable` | Network, throttling, or other AWS failure |

## Health Check

Health checks are provided by the core `Encina.Security.Secrets` package:

```csharp
services.AddAwsSecretsManager(
    configureSecrets: o => o.ProviderHealthCheck = true);
```

## Observability

All observability features are inherited from `Encina.Security.Secrets`:

| Feature | Source | Opt-In |
|---------|--------|--------|
| Tracing | `SecretsActivitySource` | `EnableTracing = true` |
| Metrics | `SecretsMetrics` | `EnableMetrics = true` |
| Health checks | `SecretsHealthCheck` | `ProviderHealthCheck = true` |
| Access auditing | `AuditedSecretReaderDecorator` | `EnableAccessAuditing = true` |
| Caching | `CachedSecretReaderDecorator` | `EnableCaching = true` (default) |
| Provider logging | `Log.cs` (EventIds 210-218) | Always active |

## Dependencies

- `AWSSDK.SecretsManager` (v4.0.4.6)
- `Encina.Security.Secrets` (core abstractions)
