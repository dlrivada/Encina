# Secrets Management -- AWS Secrets Manager

This guide covers the AWS Secrets Manager integration in Encina via the `Encina.Security.Secrets.AwsSecretsManager` package. It explains prerequisites, IAM policies, credential resolution, configuration, error mapping, and best practices specific to AWS Secrets Manager.

## Table of Contents

1. [Overview](#overview)
2. [Prerequisites](#prerequisites)
3. [IAM Policy](#iam-policy)
4. [Credential Chain](#credential-chain)
5. [Configuration](#configuration)
6. [Error Mapping](#error-mapping)
7. [SetSecretAsync Behavior](#setsecretasync-behavior)
8. [RotateSecretAsync Behavior](#rotatesecretasync-behavior)
9. [Quick Start](#quick-start)
10. [Best Practices](#best-practices)

---

## Overview

`Encina.Security.Secrets.AwsSecretsManager` provides an ISP-compliant implementation backed by the AWS SDK for .NET (`AWSSDK.SecretsManager` v4.0.4.6). It wraps `IAmazonSecretsManager` to expose a unified, Railway Oriented Programming (ROP) API that returns `Either<EncinaError, T>` for all operations.

| Component | Description |
|-----------|-------------|
| **`AwsSecretsManagerProvider`** | Implements `ISecretReader`, `ISecretWriter`, `ISecretRotator` via `IAmazonSecretsManager` |
| **`AwsSecretsManagerOptions`** | Configuration: `Region` (`RegionEndpoint?`), `Credentials` (`AWSCredentials?`), `ClientConfig` (`AmazonSecretsManagerConfig?`) |
| **`AddAwsSecretsManager`** | Extension method to register all services with decorator chain |

### NuGet Package

```
Encina.Security.Secrets.AwsSecretsManager
```

**Dependencies**: `AWSSDK.SecretsManager` (v4.0.4.6), `Encina.Security.Secrets` (core abstractions).

---

## Prerequisites

1. **AWS Account** -- An active AWS account.
2. **IAM Permissions** -- The identity (IAM user, role, or instance profile) must have the required Secrets Manager permissions.
3. **Region** -- Know which AWS region your secrets reside in, or rely on the default region from your credential chain.

---

## IAM Policy

The following IAM actions are used by `AwsSecretsManagerProvider`:

| IAM Action | Used By | Purpose |
|------------|---------|---------|
| `secretsmanager:GetSecretValue` | `GetSecretAsync`, `GetSecretAsync<T>`, `RotateSecretAsync` | Read secret values |
| `secretsmanager:PutSecretValue` | `SetSecretAsync`, `RotateSecretAsync` | Update existing secret values |
| `secretsmanager:CreateSecret` | `SetSecretAsync` (fallback) | Create new secrets when they do not exist |

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
        "secretsmanager:CreateSecret"
      ],
      "Resource": "arn:aws:secretsmanager:<region>:<account-id>:secret:*"
    }
  ]
}
```

For read-only access, remove `PutSecretValue` and `CreateSecret`.

---

## Credential Chain

When `AwsSecretsManagerOptions.Credentials` is `null` (the default), the AWS SDK resolves credentials through its default chain:

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
using Amazon;
using Amazon.Runtime;

services.AddAwsSecretsManager(aws =>
{
    aws.Region = RegionEndpoint.USEast1;
    aws.Credentials = new BasicAWSCredentials("AKIA...", "secret...");
});
```

> **Warning**: Never hard-code access keys in source code. Use environment variables or IAM roles instead.

---

## Configuration

`AwsSecretsManagerOptions` exposes the following settings:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Region` | `RegionEndpoint?` | `null` | AWS region endpoint. When `null`, the default region from the credential chain is used. |
| `Credentials` | `AWSCredentials?` | `null` | Explicit AWS credentials. When `null`, the default credential chain is used. |
| `ClientConfig` | `AmazonSecretsManagerConfig?` | `null` | Client configuration (retry policies, timeouts, etc.). When `null`, AWS SDK defaults are used. |

### Region Resolution

If `Region` is set, the provider creates an `AmazonSecretsManagerClient` with that `RegionEndpoint`. Otherwise, the SDK resolves the region from the environment or credentials profile.

---

## Error Mapping

`AwsSecretsManagerProvider` translates AWS SDK exceptions into `EncinaError` using `SecretsErrors`:

| AWS Exception | Encina Error Code | Description |
|---------------|-------------------|-------------|
| `ResourceNotFoundException` | `secrets.not_found` | Secret does not exist |
| `AmazonSecretsManagerException` with `ErrorCode == "AccessDeniedException"` | `secrets.access_denied` | Insufficient IAM permissions |
| Other `AmazonSecretsManagerException` | `secrets.provider_unavailable` | Network error, throttling, or other AWS failure |
| Any exception during rotation | `secrets.rotation_failed` | Rotation operation failed |
| `JsonException` during typed deserialization | `secrets.deserialization_failed` | Secret value is not valid JSON for the target type |

### Example Error Handling

```csharp
var result = await secretReader.GetSecretAsync("db-password", cancellationToken);

result.Match(
    Right: value => logger.LogInformation("Secret value retrieved"),
    Left: error => logger.LogError("Failed: [{Code}] {Message}",
        error.GetCode().IfNone("unknown"), error.Message));
```

---

## SetSecretAsync Behavior

`SetSecretAsync` implements a create-or-update pattern:

1. **Try `PutSecretValue`** -- Attempts to add a new version to an existing secret.
2. **On `ResourceNotFoundException`** -- Falls back to `CreateSecret` to create the secret and its first version.

This means `SetSecretAsync` is idempotent: calling it for a secret that does not yet exist will create it automatically.

---

## RotateSecretAsync Behavior

`RotateSecretAsync` performs a read-then-write rotation:

1. **Read** -- Retrieves the current secret value via `GetSecretValue`.
2. **Write** -- Writes the value back via `PutSecretValue`, creating a new version.

In practice, the `SecretRotationCoordinator` (from the core package) generates the new value via `ISecretRotationHandler` and writes it through `ISecretWriter`.

> **Note**: This is a simple version-based rotation. For AWS Lambda-based rotation (multi-step with pending/current stages), use the AWS Secrets Manager rotation configuration directly.

---

## Quick Start

### 1. Install the Package

```bash
dotnet add package Encina.Security.Secrets.AwsSecretsManager
```

### 2. Register Services

```csharp
using Encina.Security.Secrets.AwsSecretsManager;

var builder = WebApplication.CreateBuilder(args);

// Basic setup with default credential chain
builder.Services.AddAwsSecretsManager();

// With explicit region and caching
builder.Services.AddAwsSecretsManager(
    aws => aws.Region = RegionEndpoint.USEast1,
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
        var result = await secretReader.GetSecretAsync("prod/api-key", ct);

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
        var result = await secretReader.GetSecretAsync<DbConfig>("prod/db-config", ct);

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
| **Use IAM roles, not access keys** | Instance profiles (EC2), task roles (ECS), and Pod Identity (EKS) eliminate credential rotation burden |
| **Enable automatic rotation** | AWS Secrets Manager supports automatic rotation with Lambda functions for databases and other services |
| **Use resource-based policies** | Scope IAM policies to specific secret ARNs rather than `*` |
| **Organize with path prefixes** | Name secrets like `prod/db-password`, `staging/api-key` for clear hierarchy |
| **Enable caching** | Use `EnableCaching = true` to reduce API calls and latency |
| **Monitor with CloudWatch** | Enable AWS CloudTrail logging for Secrets Manager API calls for auditing |
| **Tag secrets consistently** | Use tags for cost allocation, ownership tracking, and automated policies |
| **Avoid storing large payloads** | AWS Secrets Manager has a 64 KB limit per secret value; for larger data, store a reference to S3 |
| **Use VPC endpoints** | Deploy a VPC endpoint for Secrets Manager to keep traffic off the public internet |

---

## Related Documentation

- [Secrets Management Overview](secrets-management.md) -- Core abstractions and architecture
- [Azure Key Vault Provider](secrets-management-azurekeyvault.md) -- Azure Key Vault integration
- [Encina.Security.Secrets](../../src/Encina.Security.Secrets/) -- `ISecretReader`, `ISecretWriter`, `ISecretRotator`, `SecretsErrors`
- [AWS Secrets Manager Documentation](https://docs.aws.amazon.com/secretsmanager/)
- [AWSSDK.SecretsManager NuGet](https://www.nuget.org/packages/AWSSDK.SecretsManager/)
