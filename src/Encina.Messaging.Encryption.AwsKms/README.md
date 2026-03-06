# Encina.Messaging.Encryption.AwsKms

[![NuGet](https://img.shields.io/nuget/v/Encina.Messaging.Encryption.AwsKms.svg)](https://www.nuget.org/packages/Encina.Messaging.Encryption.AwsKms)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)

**AWS KMS key management provider for Encina message encryption.**

Integrates AWS Key Management Service with Encina's message encryption infrastructure, providing IAM-based access control, envelope encryption, and automatic key rotation for outbox/inbox message payload encryption.

## Key Features

- **AWS KMS** integration via `AWSSDK.KeyManagementService` SDK
- **Default credential chain** — IAM roles, environment variables, EC2/ECS/EKS profiles
- **Envelope encryption** — data keys encrypted with KMS master key
- **Automatic key rotation** — `RotateKeyAsync` leverages AWS KMS key rotation
- **Railway Oriented Programming** — all operations return `Either<EncinaError, T>`

## Quick Start

```csharp
services.AddEncinaMessageEncryptionAwsKms(
    aws =>
    {
        aws.KeyId = "arn:aws:kms:us-east-1:123456789:key/abc-123";
        aws.Region = "us-east-1";
    },
    encryption =>
    {
        encryption.EncryptAllMessages = true;
        encryption.AddHealthCheck = true;
    });
```

## Configuration

| Property | Type | Description |
|----------|------|-------------|
| `KeyId` | `string?` | KMS key ARN, alias, or alias ARN |
| `Region` | `string?` | AWS region (null = default SDK chain) |
| `EncryptionAlgorithm` | `string` | Algorithm (default: `"SYMMETRIC_DEFAULT"`) |
| `ClientConfig` | `AmazonKeyManagementServiceConfig?` | Custom client config |

## Dependencies

- `Encina.Messaging.Encryption` (core encryption)
- `AWSSDK.KeyManagementService`

## License

MIT License. See [LICENSE](../../LICENSE) for details.
