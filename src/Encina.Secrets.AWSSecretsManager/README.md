# Encina.Secrets.AWSSecretsManager

[![NuGet](https://img.shields.io/nuget/v/Encina.Secrets.AWSSecretsManager.svg)](https://www.nuget.org/packages/Encina.Secrets.AWSSecretsManager/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](../../LICENSE)

AWS Secrets Manager provider for Encina secrets management. Implements `ISecretProvider` using the AWS SDK `IAmazonSecretsManager`, with full Railway Oriented Programming support and optional health checks.

## Prerequisites

- AWS account with Secrets Manager access
- IAM permissions for `secretsmanager:GetSecretValue`, `secretsmanager:PutSecretValue`, `secretsmanager:DeleteSecret`, `secretsmanager:ListSecrets`, `secretsmanager:DescribeSecret`
- AWS credentials configured via environment variables, credentials file, or IAM role

## Installation

```bash
dotnet add package Encina.Secrets.AWSSecretsManager
```

## Quick Start

### 1. Register the Provider

```csharp
services.AddEncinaAWSSecretsManager(options =>
{
    options.Region = "us-east-1";
});
```

### 2. Use ISecretProvider

```csharp
public class MyService(ISecretProvider secrets)
{
    public async Task DoWorkAsync(CancellationToken ct)
    {
        var result = await secrets.GetSecretAsync("prod/database-password", ct);

        result.Match(
            Right: secret => UsePassword(secret.Value),
            Left: error => logger.LogError("Failed: {Code}", error.Code));
    }
}
```

## Configuration Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Region` | `string?` | `null` | AWS region (e.g., `us-east-1`). When `null`, the default region from environment/credentials is used |
| `Credentials` | `AWSCredentials?` | `null` | AWS credentials provider. When `null`, the default credential chain is used |
| `ProviderHealthCheck` | `ProviderHealthCheckOptions` | Disabled | Health check configuration |

### Custom Credentials

```csharp
services.AddEncinaAWSSecretsManager(options =>
{
    options.Region = "eu-west-1";
    options.Credentials = new BasicAWSCredentials("accessKey", "secretKey");
});
```

### Enable Health Check

```csharp
services.AddEncinaAWSSecretsManager(options =>
{
    options.Region = "us-east-1";
    options.ProviderHealthCheck = new ProviderHealthCheckOptions
    {
        Enabled = true
    };
});
```

## Health Check

- **Class**: `AWSSecretsManagerHealthCheck`
- **Default Name**: `encina-secrets-aws`
- **Default Tags**: `encina`, `secrets`, `aws`, `ready`
- **Mechanism**: Lists secrets with max 1 result to verify connectivity

## Documentation

- [Encina.Secrets](../Encina.Secrets/README.md) - Core abstractions and caching
- [API Reference](https://docs.encina.dev/api/Encina.Secrets.AWSSecretsManager) - Full API documentation

## Related Packages

| Package | Description |
|---------|-------------|
| `Encina.Secrets` | Core abstractions, caching decorator, IConfiguration integration |
| `Encina.Secrets.AzureKeyVault` | Azure Key Vault provider |
| `Encina.Secrets.HashiCorpVault` | HashiCorp Vault KV v2 provider |
| `Encina.Secrets.GoogleSecretManager` | Google Cloud Secret Manager provider |

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.
