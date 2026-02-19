# Secrets Management -- AWS Secrets Manager

This guide covers the AWS Secrets Manager integration in Encina via the `Encina.Secrets.AWSSecretsManager` package. It explains prerequisites, IAM policies, credential resolution, configuration, version handling, error mapping, health checks, and best practices specific to AWS Secrets Manager.

## Table of Contents

1. [Overview](#overview)
2. [Prerequisites](#prerequisites)
3. [IAM Policy](#iam-policy)
4. [Credential Chain](#credential-chain)
5. [Configuration](#configuration)
6. [Version Handling](#version-handling)
7. [Error Mapping](#error-mapping)
8. [SetSecretAsync Behavior](#setsecretasync-behavior)
9. [Health Check](#health-check)
10. [Quick Start](#quick-start)
11. [Best Practices](#best-practices)

---

## Overview

`Encina.Secrets.AWSSecretsManager` provides an `ISecretProvider` implementation backed by the AWS SDK for .NET (`AWSSDK.SecretsManager`). It wraps `IAmazonSecretsManager` to expose a unified, Railway Oriented Programming (ROP) API that returns `Either<EncinaError, T>` for all operations.

| Component | Description |
|-----------|-------------|
| **`AWSSecretsManagerProvider`** | `ISecretProvider` implementation that delegates to `IAmazonSecretsManager` |
| **`AWSSecretsManagerOptions`** | Configuration: `Region`, `Credentials`, `ProviderHealthCheck` |
| **`AWSSecretsManagerHealthCheck`** | ASP.NET Core health check verifying Secrets Manager connectivity |
| **`AddEncinaAWSSecretsManager`** | Extension method to register all services |

### NuGet Package

```
Encina.Secrets.AWSSecretsManager
```

**Dependencies**: `AWSSDK.SecretsManager`, `Encina.Secrets` (core abstractions).

---

## Prerequisites

1. **AWS Account** -- An active AWS account.
2. **IAM Permissions** -- The identity (IAM user, role, or instance profile) must have the required Secrets Manager permissions.
3. **Region** -- Know which AWS region your secrets reside in, or rely on the default region from your credential chain.

---

## IAM Policy

The following IAM actions are used by `AWSSecretsManagerProvider`:

| IAM Action | Used By | Purpose |
|------------|---------|---------|
| `secretsmanager:GetSecretValue` | `GetSecretAsync`, `GetSecretVersionAsync` | Read secret values |
| `secretsmanager:PutSecretValue` | `SetSecretAsync` | Update existing secret values |
| `secretsmanager:CreateSecret` | `SetSecretAsync` (fallback) | Create new secrets when they do not exist |
| `secretsmanager:DeleteSecret` | `DeleteSecretAsync` | Schedule secret deletion |
| `secretsmanager:ListSecrets` | `ListSecretsAsync`, health check | Enumerate secrets |
| `secretsmanager:DescribeSecret` | `ExistsAsync` | Check secret existence without reading the value |

### Minimum IAM Policy Document

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "secretsmanager:GetSecretValue",
        "secretsmanager:PutSecretValue",
        "secretsmanager:CreateSecret",
        "secretsmanager:DeleteSecret",
        "secretsmanager:ListSecrets",
        "secretsmanager:DescribeSecret"
      ],
      "Resource": "arn:aws:secretsmanager:<region>:<account-id>:secret:*"
    }
  ]
}
```

For read-only access, remove `PutSecretValue`, `CreateSecret`, and `DeleteSecret`.

---

## Credential Chain

When `AWSSecretsManagerOptions.Credentials` is `null` (the default), the AWS SDK resolves credentials through its default chain:

| Order | Source | Environment |
|-------|--------|-------------|
| 1 | Environment variables (`AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`) | CI/CD pipelines, containers |
| 2 | Shared credentials file (`~/.aws/credentials`) | Local development |
| 3 | AWS config file (`~/.aws/config`) with SSO or profiles | Local development |
| 4 | EC2 instance profile (IMDS) | EC2 instances |
| 5 | ECS task role | ECS containers (Fargate or EC2 launch type) |
| 6 | EKS Pod Identity / IRSA | Kubernetes on EKS |

### Explicit Credentials

```csharp
using Amazon.Runtime;

services.AddEncinaAWSSecretsManager(options =>
{
    options.Region = "us-east-1";
    options.Credentials = new BasicAWSCredentials("AKIA...", "secret...");
});
```

> **Warning**: Never hard-code access keys in source code. Use environment variables or IAM roles instead.

---

## Configuration

`AWSSecretsManagerOptions` exposes the following settings:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Region` | `string?` | `null` | AWS region (e.g., `us-east-1`). When `null`, the default region from the credential chain is used. |
| `Credentials` | `AWSCredentials?` | `null` | Explicit AWS credentials. When `null`, the default credential chain is used. |
| `ProviderHealthCheck` | `ProviderHealthCheckOptions` | Disabled | Health check configuration (see [Health Check](#health-check)). |

### Region Resolution

If `Region` is set, the provider creates an `AmazonSecretsManagerClient` with `RegionEndpoint.GetBySystemName(options.Region)`. Otherwise, the SDK resolves the region from the environment or credentials profile.

---

## Version Handling

AWS Secrets Manager uses two versioning mechanisms:

| Mechanism | Description | Example |
|-----------|-------------|---------|
| **VersionId** | Unique UUID assigned to each version | `a1b2c3d4-5678-90ab-cdef-EXAMPLE11111` |
| **VersionStage** | Label attached to versions | `AWSCURRENT` (latest), `AWSPREVIOUS` (previous), `AWSPENDING` (rotation in progress) |

### How Encina Uses Versions

- **`GetSecretAsync`**: Retrieves the `AWSCURRENT` version (default behavior when no version is specified).
- **`GetSecretVersionAsync`**: Passes the `version` parameter as `VersionId` in the `GetSecretValueRequest`.
- **`SetSecretAsync`**: Returns the `VersionId` of the newly created version in `SecretMetadata.Version`.

### Accessing a Specific Version

```csharp
// Get the current version
var current = await provider.GetSecretAsync("my-secret", ct);

// Get a specific version by VersionId
var specific = await provider.GetSecretVersionAsync("my-secret", "a1b2c3d4-...", ct);
```

---

## Error Mapping

`AWSSecretsManagerProvider` translates AWS SDK exceptions into `EncinaError` using `SecretsErrorCodes`:

| AWS Exception | Encina Error Code | Description |
|---------------|-------------------|-------------|
| `ResourceNotFoundException` | `encina.secrets.not_found` | Secret does not exist |
| `AmazonSecretsManagerException` with `ErrorCode == "AccessDeniedException"` | `encina.secrets.access_denied` | Insufficient IAM permissions |
| Other `AmazonSecretsManagerException` | `encina.secrets.provider_unavailable` | Network error, throttling, or other AWS failure |

For versioned access (`GetSecretVersionAsync`), a `ResourceNotFoundException` maps to `encina.secrets.version_not_found`.

### Example Error Handling

```csharp
var result = await provider.GetSecretAsync("db-password", cancellationToken);

result.Match(
    Right: secret => logger.LogInformation("Secret version: {Version}", secret.Version),
    Left: error => error.Code switch
    {
        SecretsErrorCodes.NotFoundCode => logger.LogWarning("Secret not found"),
        SecretsErrorCodes.AccessDeniedCode => logger.LogError("IAM permission denied"),
        _ => logger.LogError("AWS error: {Message}", error.Message)
    });
```

---

## SetSecretAsync Behavior

`SetSecretAsync` implements a create-or-update pattern:

1. **Try `PutSecretValue`** -- Attempts to add a new version to an existing secret.
2. **On `ResourceNotFoundException`** -- Falls back to `CreateSecret` to create the secret and its first version.

This means `SetSecretAsync` is idempotent: calling it for a secret that does not yet exist will create it automatically.

### Tags on Creation

Tags (via `SecretOptions.Tags`) are only applied when the secret is **created** (the `CreateSecretRequest`). Updating tags on an existing secret requires a separate AWS API call not covered by `SetSecretAsync`.

### Delete Behavior

`DeleteSecretAsync` calls `DeleteSecretRequest` with `ForceDeleteWithoutRecovery = false`, which schedules the secret for deletion (default 30-day recovery window). It does **not** permanently destroy the secret immediately.

---

## Health Check

`AWSSecretsManagerHealthCheck` verifies connectivity by calling `ListSecrets` with `MaxResults = 1`.

| Property | Value |
|----------|-------|
| **Name** | `encina-secrets-aws` |
| **Tags** | `["encina", "secrets", "aws", "ready"]` |
| **Healthy** | `ListSecrets` call succeeds |
| **Unhealthy** | `AmazonSecretsManagerException` thrown (includes `ErrorCode` in description) |

### Enabling the Health Check

```csharp
services.AddEncinaAWSSecretsManager(options =>
{
    options.Region = "us-east-1";
    options.ProviderHealthCheck = new ProviderHealthCheckOptions
    {
        Enabled = true,
        Tags = ["encina", "secrets", "aws", "ready"]
    };
});
```

> **Permission requirement**: The health check calls `ListSecrets`, which requires the `secretsmanager:ListSecrets` IAM action.

---

## Quick Start

### 1. Install the Package

```bash
dotnet add package Encina.Secrets.AWSSecretsManager
```

### 2. Register Services

```csharp
using Encina.Secrets.AWSSecretsManager;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEncinaAWSSecretsManager(options =>
{
    options.Region = "us-east-1";
    // Default credential chain is used automatically
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
        // Read a secret (returns AWSCURRENT version)
        var result = await _secrets.GetSecretAsync("prod/db-password", ct);

        result.Match(
            Right: secret => Console.WriteLine($"Value: {secret.Value}, VersionId: {secret.Version}"),
            Left: error => Console.WriteLine($"Error [{error.Code}]: {error.Message}"));

        // Write a secret (creates if not exists, updates if exists)
        var setResult = await _secrets.SetSecretAsync(
            "prod/api-key",
            "sk-new-value-here",
            new SecretOptions(
                Tags: new Dictionary<string, string>
                {
                    ["Environment"] = "Production",
                    ["Team"] = "Backend"
                }),
            ct);

        // Delete a secret (schedules for deletion with 30-day recovery window)
        var deleteResult = await _secrets.DeleteSecretAsync("prod/old-key", ct);

        // List all secret names
        var listResult = await _secrets.ListSecretsAsync(ct);

        // Check existence (uses DescribeSecret, does not read the value)
        var existsResult = await _secrets.ExistsAsync("prod/db-password", ct);
    }
}
```

---

## Best Practices

| Practice | Rationale |
|----------|-----------|
| **Use IAM roles, not access keys** | Instance profiles (EC2), task roles (ECS), and Pod Identity (EKS) eliminate credential rotation burden |
| **Enable automatic rotation** | AWS Secrets Manager supports automatic rotation with Lambda functions for databases and other services |
| **Use resource-based policies** | Scope IAM policies to specific secret ARNs rather than `*` |
| **Organize with path prefixes** | Name secrets like `prod/db-password`, `staging/api-key` for clear hierarchy |
| **Enable health checks in production** | Detect connectivity or permission issues before they impact users |
| **Monitor with CloudWatch** | Enable AWS CloudTrail logging for Secrets Manager API calls for auditing |
| **Use `ForceDeleteWithoutRecovery = false`** | The default in Encina; allows recovery within 30 days of accidental deletion |
| **Tag secrets consistently** | Use tags for cost allocation, ownership tracking, and automated policies |
| **Avoid storing large payloads** | AWS Secrets Manager has a 64 KB limit per secret value; for larger data, store a reference to S3 |
| **Use VPC endpoints** | Deploy a VPC endpoint for Secrets Manager to keep traffic off the public internet |

---

## Related Documentation

- [Secrets Management Overview](../features/secrets-management.md) (if available)
- [Encina.Secrets Core Abstractions](../../src/Encina.Secrets/) -- `ISecretProvider`, `Secret`, `SecretMetadata`, `SecretsErrorCodes`
- [AWS Secrets Manager Documentation](https://docs.aws.amazon.com/secretsmanager/)
- [AWSSDK.SecretsManager NuGet](https://www.nuget.org/packages/AWSSDK.SecretsManager/)
