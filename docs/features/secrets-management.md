# Secrets Management in Encina

This guide explains how to manage application secrets using Encina's provider-agnostic secrets management system. Secrets operations use Railway Oriented Programming (ROP) throughout, returning `Either<EncinaError, T>` instead of throwing exceptions.

## Table of Contents

1. [Overview](#overview)
2. [The Problem](#the-problem)
3. [The Solution](#the-solution)
4. [Quick Start](#quick-start)
5. [Architecture](#architecture)
6. [Provider Support](#provider-support)
7. [Error Handling](#error-handling)
8. [Configuration Integration](#configuration-integration)
9. [Caching](#caching)
10. [Health Checks](#health-checks)
11. [Observability](#observability)
12. [Best Practices](#best-practices)
13. [Testing](#testing)
14. [FAQ](#faq)

---

## Overview

Encina.Secrets provides a unified abstraction for secret management across cloud providers:

| Component | Description |
|-----------|-------------|
| **`ISecretProvider`** | Core interface with 6 ROP-based operations |
| **`CachedSecretProvider`** | Decorator that adds in-memory caching with configurable TTL |
| **`InstrumentedSecretProvider`** | Decorator that adds OpenTelemetry tracing and metrics |
| **`SecretConfigurationProvider`** | Bridge to Microsoft `IConfiguration` system |
| **Health Checks** | Core + per-provider health checks for production readiness |

### Key Design Principles

| Principle | Implementation |
|-----------|---------------|
| **ROP everywhere** | All operations return `Either<EncinaError, T>` -- no exceptions for business logic |
| **Provider-agnostic** | Same `ISecretProvider` interface across Azure, AWS, GCP, HashiCorp |
| **Opt-in decorators** | Caching and instrumentation are applied via decorator pattern, not baked in |
| **Security by default** | Secret names not recorded in telemetry unless explicitly enabled |

---

## The Problem

Applications typically couple secret access to a specific provider SDK, making it difficult to switch providers or test in isolation:

```csharp
// Problem 1: Tightly coupled to Azure Key Vault SDK
var client = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());
var secret = await client.GetSecretAsync("my-secret");
// What if you need to switch to AWS? Rewrite every call site.

// Problem 2: No consistent error handling
try
{
    var secret = await client.GetSecretAsync("my-secret");
}
catch (RequestFailedException ex) when (ex.Status == 404)
{
    // Azure-specific exception handling
}
catch (RequestFailedException ex) when (ex.Status == 403)
{
    // Different exception for different error
}

// Problem 3: No caching or observability out of the box
// Every call goes to the network, no metrics, no tracing
```

---

## The Solution

With Encina.Secrets, secret access is provider-agnostic with consistent ROP error handling:

```csharp
// One interface, any provider
public class OrderService(ISecretProvider secrets)
{
    public async Task<Either<EncinaError, string>> GetApiKey(CancellationToken ct)
    {
        var result = await secrets.GetSecretAsync("payment-api-key", ct);

        return result.Map(secret => secret.Value);
    }
}
```

Switch providers by changing a single DI registration -- no code changes needed.

---

## Quick Start

### 1. Install the Packages

```bash
# Core abstractions (required)
dotnet add package Encina.Secrets

# Choose your provider
dotnet add package Encina.Secrets.AzureKeyVault
# or
dotnet add package Encina.Secrets.AWSSecretsManager
# or
dotnet add package Encina.Secrets.HashiCorpVault
# or
dotnet add package Encina.Secrets.GoogleSecretManager
```

### 2. Register a Provider

```csharp
// Azure Key Vault
services.AddEncinaKeyVaultSecrets(options =>
{
    options.VaultUri = "https://my-vault.vault.azure.net/";
    options.ProviderHealthCheck.Enabled = true;
});

// Optional: Add caching
services.AddEncinaSecretsCaching(options =>
{
    options.DefaultTtl = TimeSpan.FromMinutes(10);
});

// Optional: Add instrumentation
services.AddEncinaSecretsInstrumentation(options =>
{
    options.RecordSecretNames = false; // default, for security
});
```

### 3. Use the Provider

```csharp
public class StartupService(ISecretProvider secrets)
{
    public async Task InitializeAsync(CancellationToken ct)
    {
        // Get a secret
        var result = await secrets.GetSecretAsync("database-password", ct);

        result.Match(
            Right: secret => ConfigureDatabase(secret.Value),
            Left: error => Log.Error("Failed to load secret: {Code}", error.GetCode()));

        // Store a secret with expiration
        var metadata = await secrets.SetSecretAsync(
            "api-token",
            "new-token-value",
            new SecretOptions(ExpiresAtUtc: DateTime.UtcNow.AddDays(90)),
            ct);

        metadata.IfRight(m =>
            Log.Information("Secret stored: version {Version}", m.Version));
    }
}
```

---

## Architecture

### Core Types

```
Encina.Secrets (core package)
├── ISecretProvider                    — 6-method interface, all returning Either<EncinaError, T>
├── Secret                             — Record(Name, Value, Version?, ExpiresAtUtc?)
├── SecretMetadata                     — Record(Name, Version, CreatedAtUtc, ExpiresAtUtc?)
├── SecretOptions                      — Record(ExpiresAtUtc?, Tags?)
├── SecretsErrorCodes                  — 6 error codes + factory methods
├── CachedSecretProvider               — Decorator: in-memory caching
├── Configuration/
│   ├── SecretConfigurationProvider    — IConfiguration bridge
│   ├── SecretConfigurationSource      — IConfigurationSource
│   ├── SecretConfigurationOptions     — Prefix, delimiter, reload
│   └── ConfigurationBuilderExtensions — AddEncinaSecrets()
├── Diagnostics/
│   ├── InstrumentedSecretProvider     — Decorator: tracing + metrics
│   ├── SecretsActivitySource          — ActivitySource "Encina.Secrets"
│   ├── SecretsMetrics                 — Meter "Encina.Secrets"
│   └── SecretsInstrumentationOptions  — RecordSecretNames, EnableTracing, EnableMetrics
└── Health/
    └── SecretsHealthCheck             — Core health check
```

### ISecretProvider Interface

The interface defines 6 operations, all returning `ValueTask<Either<EncinaError, T>>`:

| Method | Returns | Description |
|--------|---------|-------------|
| `GetSecretAsync(name)` | `Either<EncinaError, Secret>` | Get the latest version of a secret |
| `GetSecretVersionAsync(name, version)` | `Either<EncinaError, Secret>` | Get a specific version of a secret |
| `SetSecretAsync(name, value, options?)` | `Either<EncinaError, SecretMetadata>` | Create or update a secret |
| `DeleteSecretAsync(name)` | `Either<EncinaError, Unit>` | Delete a secret |
| `ListSecretsAsync()` | `Either<EncinaError, IEnumerable<string>>` | List all secret names |
| `ExistsAsync(name)` | `Either<EncinaError, bool>` | Check if a secret exists |

### Decorator Pipeline

Decorators are applied in registration order. The recommended order is:

```
Application Code
    │
    ▼
InstrumentedSecretProvider  ← AddEncinaSecretsInstrumentation()
    │
    ▼
CachedSecretProvider         ← AddEncinaSecretsCaching()
    │
    ▼
Cloud Provider               ← AddEncinaKeyVaultSecrets(), etc.
```

Register the cloud provider first, then caching, then instrumentation:

```csharp
// 1. Cloud provider (innermost)
services.AddEncinaKeyVaultSecrets(options => { ... });

// 2. Caching decorator
services.AddEncinaSecretsCaching(options =>
{
    options.DefaultTtl = TimeSpan.FromMinutes(5);
});

// 3. Instrumentation decorator (outermost)
services.AddEncinaSecretsInstrumentation(options =>
{
    options.RecordSecretNames = true;
});
```

---

## Provider Support

### Azure Key Vault

**Package**: `Encina.Secrets.AzureKeyVault`

```csharp
services.AddEncinaKeyVaultSecrets(options =>
{
    options.VaultUri = "https://my-vault.vault.azure.net/";
    options.Credential = new DefaultAzureCredential(); // default when null
    options.ProviderHealthCheck.Enabled = true;
});
```

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `VaultUri` | `string` | `""` | Azure Key Vault URI |
| `Credential` | `TokenCredential?` | `null` (DefaultAzureCredential) | Authentication credential |
| `ProviderHealthCheck.Enabled` | `bool` | `false` | Register health check |

### AWS Secrets Manager

**Package**: `Encina.Secrets.AWSSecretsManager`

```csharp
services.AddEncinaAWSSecretsManager(options =>
{
    options.Region = "us-east-1";
    options.ProviderHealthCheck.Enabled = true;
});
```

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Region` | `string?` | `null` (default chain) | AWS region |
| `Credentials` | `AWSCredentials?` | `null` (default chain) | AWS credentials |
| `ProviderHealthCheck.Enabled` | `bool` | `false` | Register health check |

### HashiCorp Vault

**Package**: `Encina.Secrets.HashiCorpVault`

Uses the KV v2 secrets engine.

```csharp
services.AddEncinaHashiCorpVault(options =>
{
    options.VaultAddress = "https://vault.example.com:8200";
    options.MountPoint = "secret"; // default KV v2 mount
    options.AuthMethod = new TokenAuthMethodInfo("s.mytoken");
    options.ProviderHealthCheck.Enabled = true;
});
```

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `VaultAddress` | `string` | `""` | Vault server address |
| `MountPoint` | `string` | `"secret"` | KV v2 mount point |
| `AuthMethod` | `IAuthMethodInfo?` | `null` (required) | Auth method (Token, AppRole, Kubernetes) |
| `ProviderHealthCheck.Enabled` | `bool` | `false` | Register health check |

**Supported auth methods**: `TokenAuthMethodInfo`, `AppRoleAuthMethodInfo`, `KubernetesAuthMethodInfo`.

### Google Secret Manager

**Package**: `Encina.Secrets.GoogleSecretManager`

```csharp
services.AddEncinaGoogleSecretManager(options =>
{
    options.ProjectId = "my-gcp-project";
    options.ProviderHealthCheck.Enabled = true;
});
```

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ProjectId` | `string` | `""` | GCP project ID |
| `ProviderHealthCheck.Enabled` | `bool` | `false` | Register health check |

Uses Application Default Credentials (ADC) automatically.

### Provider Comparison

| Feature | Azure Key Vault | AWS Secrets Manager | HashiCorp Vault | Google Secret Manager |
|---------|:-:|:-:|:-:|:-:|
| Native versioning | Yes | Yes | Yes (KV v2) | Yes |
| Expiration support | Yes | No (manual rotation) | Yes (TTL leases) | No (manual rotation) |
| Tags/Labels | Yes | Yes | Yes (metadata) | Yes (labels) |
| Auth options | DefaultAzureCredential, MI, SP | IAM, STS, profiles | Token, AppRole, K8s | ADC, Service Account |
| Health check | `KeyVaultHealthCheck` | `AWSSecretsManagerHealthCheck` | `HashiCorpVaultHealthCheck` | `GoogleSecretManagerHealthCheck` |

---

## Error Handling

All `ISecretProvider` methods return `Either<EncinaError, T>`, following Encina's Railway Oriented Programming pattern. Errors are never thrown as exceptions for expected failure conditions.

### Error Codes

| Code | Constant | Meaning |
|------|----------|---------|
| `encina.secrets.not_found` | `SecretsErrorCodes.NotFoundCode` | Secret does not exist |
| `encina.secrets.access_denied` | `SecretsErrorCodes.AccessDeniedCode` | Permission denied |
| `encina.secrets.invalid_name` | `SecretsErrorCodes.InvalidNameCode` | Secret name is invalid |
| `encina.secrets.provider_unavailable` | `SecretsErrorCodes.ProviderUnavailableCode` | Provider is unreachable |
| `encina.secrets.version_not_found` | `SecretsErrorCodes.VersionNotFoundCode` | Specific version not found |
| `encina.secrets.operation_failed` | `SecretsErrorCodes.OperationFailedCode` | General operation failure |

Each error includes structured metadata (e.g., `secretName`, `version`, `providerName`, `reason`, `stage`).

### Match() for Branching

The most common pattern -- branch on success or failure:

```csharp
var result = await secrets.GetSecretAsync("api-key", ct);

result.Match(
    Right: secret =>
    {
        Console.WriteLine($"Got secret: {secret.Name}, version: {secret.Version}");
    },
    Left: error =>
    {
        Console.WriteLine($"Error [{error.GetCode().IfNone("unknown")}]: {error.Message}");
    });
```

### Map() for Transforming Success

Transform the Right value without touching errors:

```csharp
// Extract just the value from a Secret
var valueResult = await secrets.GetSecretAsync("connection-string", ct);
Either<EncinaError, string> connectionString = valueResult.Map(s => s.Value);

// Check expiration
Either<EncinaError, bool> isExpired = valueResult.Map(s =>
    s.ExpiresAtUtc.HasValue && s.ExpiresAtUtc.Value < DateTime.UtcNow);
```

### Bind() for Chaining Operations

Chain multiple operations where each can fail independently:

```csharp
// Get a secret, then use it to get another
var result = await secrets.GetSecretAsync("encryption-key-name", ct)
    .BindAsync(async nameSecret =>
        await secrets.GetSecretAsync(nameSecret.Value, ct));
```

### Pattern Matching on Error Codes

Use error codes to handle specific failure cases:

```csharp
var result = await secrets.GetSecretAsync("my-secret", ct);

result.Match(
    Right: secret => UseSecret(secret),
    Left: error =>
    {
        var code = error.GetCode().IfNone("unknown");

        switch (code)
        {
            case SecretsErrorCodes.NotFoundCode:
                Log.Warning("Secret not found, using fallback");
                UseFallback();
                break;

            case SecretsErrorCodes.AccessDeniedCode:
                Log.Error("Access denied -- check IAM permissions");
                throw new UnauthorizedAccessException(error.Message);

            case SecretsErrorCodes.ProviderUnavailableCode:
                Log.Error("Provider down, retrying...");
                // Schedule retry
                break;

            default:
                Log.Error("Unexpected error: {Code} - {Message}", code, error.Message);
                break;
        }
    });
```

### Accessing Error Metadata

Error instances include structured details via `EncinaError`:

```csharp
var result = await secrets.GetSecretAsync("my-secret", ct);

result.IfLeft(error =>
{
    // Error code
    var code = error.GetCode().IfNone("unknown");

    // Human-readable message
    var message = error.Message;

    // Structured metadata (secretName, stage, reason, etc.)
    // Available through EncinaError details
});
```

### Common Error Handling Patterns

**Default value on not found**:

```csharp
var result = await secrets.GetSecretAsync("optional-config", ct);
var value = result.Match(
    Right: s => s.Value,
    Left: _ => "default-value");
```

**Propagate errors through the pipeline**:

```csharp
public async ValueTask<Either<EncinaError, OrderResult>> ProcessOrder(
    OrderCommand command, CancellationToken ct)
{
    // If GetSecretAsync fails, the error propagates automatically
    return await secrets.GetSecretAsync("payment-api-key", ct)
        .MapAsync(secret => CallPaymentApi(secret.Value, command));
}
```

**Aggregate multiple secrets**:

```csharp
var dbPassword = await secrets.GetSecretAsync("db-password", ct);
var apiKey = await secrets.GetSecretAsync("api-key", ct);

// Both must succeed
var combined = from db in dbPassword
               from api in apiKey
               select new { DbPassword = db.Value, ApiKey = api.Value };

combined.Match(
    Right: config => Initialize(config.DbPassword, config.ApiKey),
    Left: error => Log.Error("Missing secret: {Error}", error.Message));
```

---

## Configuration Integration

Bridge secrets into the standard .NET `IConfiguration` system using `AddEncinaSecrets()`:

### Basic Setup

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// 1. Register the secret provider
builder.Services.AddEncinaKeyVaultSecrets(options =>
{
    options.VaultUri = "https://my-vault.vault.azure.net/";
});

// 2. Build an intermediate service provider
var sp = builder.Services.BuildServiceProvider();

// 3. Add secrets as a configuration source
builder.Configuration.AddEncinaSecrets(sp);
```

Secrets are now accessible via `IConfiguration`:

```csharp
var dbPassword = configuration["DatabasePassword"];
var apiKey = configuration["ExternalApi:Key"];
```

### Configuration Options

```csharp
builder.Configuration.AddEncinaSecrets(sp, options =>
{
    // Only load secrets starting with "myapp/"
    options.SecretPrefix = "myapp/";

    // Strip the prefix from configuration keys (default: true)
    // "myapp/DatabasePassword" becomes "DatabasePassword"
    options.StripPrefix = true;

    // Map secret name delimiters to configuration sections
    // "Database--Password" becomes Configuration["Database:Password"]
    options.KeyDelimiter = "--"; // default

    // Periodically reload secrets (null = load once at startup)
    options.ReloadInterval = TimeSpan.FromMinutes(5);
});
```

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `SecretPrefix` | `string?` | `null` | Filter secrets by name prefix |
| `StripPrefix` | `bool` | `true` | Remove prefix from configuration keys |
| `KeyDelimiter` | `string` | `"--"` | Delimiter mapped to `:` in configuration hierarchy |
| `ReloadInterval` | `TimeSpan?` | `null` | Periodic reload interval (null = load once) |

### Key Delimiter Mapping

Secret names in vaults often cannot contain `:` (the standard configuration separator). The `KeyDelimiter` maps an alternative character sequence to `:`:

| Secret Name | KeyDelimiter | Configuration Key |
|-------------|:---:|-------------------|
| `Database--ConnectionString` | `--` | `Database:ConnectionString` |
| `App/Settings/Timeout` | `/` | `App:Settings:Timeout` |
| `Redis.Connection.Host` | `.` | `Redis:Connection:Host` |

---

## Caching

The `CachedSecretProvider` decorator adds in-memory caching using `IMemoryCache`:

### Registration

```csharp
// Register provider first
services.AddEncinaKeyVaultSecrets(options => { ... });

// Then wrap with caching
services.AddEncinaSecretsCaching(options =>
{
    options.DefaultTtl = TimeSpan.FromMinutes(10);
    options.Enabled = true; // default
});
```

### Cache Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `DefaultTtl` | `TimeSpan` | 5 minutes | Time-to-live for cached entries |
| `Enabled` | `bool` | `true` | Enable/disable caching (pass-through when disabled) |

### Caching Behavior

| Operation | Cached? | Notes |
|-----------|:---:|-------|
| `GetSecretAsync` | Yes | Cache key: `encina:secrets:{name}` |
| `GetSecretVersionAsync` | Yes | Cache key: `encina:secrets:v:{name}:{version}` |
| `ExistsAsync` | Yes | Cache key: `encina:secrets:exists:{name}` |
| `SetSecretAsync` | No | Invalidates `Get` and `Exists` cache entries on success |
| `DeleteSecretAsync` | No | Invalidates `Get` and `Exists` cache entries on success |
| `ListSecretsAsync` | No | Result set changes too frequently for reliable caching |

### ROP-Aware Caching

The cache decorator is fully ROP-aware:

- **Only `Right` (success) results are cached.** If a provider returns an error (Left), it is not cached, allowing subsequent calls to retry.
- **Cache invalidation only on successful writes.** `SetSecretAsync` and `DeleteSecretAsync` only invalidate cache entries when the inner provider returns `Right`.
- **Versioned entries expire via TTL.** Individual version cache entries cannot be enumerated, so they are not explicitly invalidated on write -- they expire naturally.

---

## Health Checks

### Core Health Check

The `SecretsHealthCheck` verifies that `ISecretProvider` is registered and resolvable from the DI container:

```csharp
// Registered automatically by provider packages when enabled
services.AddEncinaKeyVaultSecrets(options =>
{
    options.ProviderHealthCheck.Enabled = true;
});
```

Health check name: `encina-secrets`
Tags: `["encina", "secrets", "ready"]`

### Per-Provider Health Checks

Each provider package includes its own health check that verifies connectivity to the external vault:

| Provider | Health Check Class | Default Name |
|----------|-------------------|--------------|
| Azure Key Vault | `KeyVaultHealthCheck` | `encina-secrets-keyvault` |
| AWS Secrets Manager | `AWSSecretsManagerHealthCheck` | `encina-secrets-aws` |
| HashiCorp Vault | `HashiCorpVaultHealthCheck` | `encina-secrets-hashicorp` |
| Google Secret Manager | `GoogleSecretManagerHealthCheck` | `encina-secrets-google` |

### Health Check Configuration

```csharp
services.AddEncinaKeyVaultSecrets(options =>
{
    options.VaultUri = "https://my-vault.vault.azure.net/";

    options.ProviderHealthCheck.Enabled = true;
    options.ProviderHealthCheck.Tags = ["encina", "secrets", "ready", "azure"];
});
```

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Enabled` | `bool` | `false` | Register the health check |
| `Tags` | `IReadOnlyList<string>` | `["encina", "secrets", "ready"]` | Health check tags |

### Expected Health Response

```json
{
  "status": "Healthy",
  "checks": {
    "encina-secrets": {
      "status": "Healthy",
      "description": "Secret provider is registered and resolvable.",
      "tags": ["encina", "secrets", "ready"]
    }
  }
}
```

---

## Observability

### OpenTelemetry Tracing

Activity source: `Encina.Secrets`

Each secret operation creates a span with the following tags:

| Tag | Description |
|-----|-------------|
| `secrets.operation` | Operation name (`get`, `get_version`, `set`, `delete`, `list`, `exists`) |
| `secrets.name` | Secret name (only when `RecordSecretNames = true`) |
| `secrets.success` | `true` or `false` |
| `secrets.error_code` | Error code from `SecretsErrorCodes` (on failure) |

Activity naming convention: `encina.secrets.{operation}` (e.g., `encina.secrets.get`).

Activities use `ActivityKind.Client` since secret operations are outbound calls to external services.

### Metrics

Meter: `Encina.Secrets`

| Instrument | Type | Unit | Description |
|------------|------|------|-------------|
| `encina.secrets.operations` | Counter | `{operations}` | Total secret operations |
| `encina.secrets.duration` | Histogram | `ms` | Duration of operations |
| `encina.secrets.errors` | Counter | `{errors}` | Failed operations |

Metric tags:

| Tag | Applied To | Description |
|-----|-----------|-------------|
| `secrets.operation` | All instruments | Operation name |
| `secrets.name` | All instruments (if enabled) | Secret name |
| `secrets.error_code` | Errors counter | Error code |

### Instrumentation Options

```csharp
services.AddEncinaSecretsInstrumentation(options =>
{
    options.EnableTracing = true;       // default
    options.EnableMetrics = true;       // default
    options.RecordSecretNames = false;  // default (for security)
});
```

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `EnableTracing` | `bool` | `true` | Enable distributed tracing via ActivitySource |
| `EnableMetrics` | `bool` | `true` | Enable metrics via Meter |
| `RecordSecretNames` | `bool` | `false` | Include secret names in telemetry |

### Subscribing to Telemetry

```csharp
// OpenTelemetry tracing
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddSource("Encina.Secrets");
    })
    .WithMetrics(metrics =>
    {
        metrics.AddMeter("Encina.Secrets");
    });
```

When no OpenTelemetry listeners are registered, activities are not created and there is zero overhead.

### Structured Logging

The `InstrumentedSecretProvider` emits warning logs on failures:

```
Secret operation 'get' failed with code 'encina.secrets.not_found': Secret 'my-secret' was not found.
```

The `CachedSecretProvider` emits debug logs for cache hits, misses, and invalidations:

```
Cache hit for secret 'my-secret'.
Cache miss for secret 'my-secret'.
Cache invalidated for secret 'my-secret' after set.
```

---

## Best Practices

### 1. Never Store Secrets in Source Code

Use environment variables or configuration files to point to the vault, never to store the secret value:

```csharp
// Good: Vault URI from configuration, secrets fetched at runtime
var vaultUri = configuration["Azure:KeyVault:Uri"];
services.AddEncinaKeyVaultSecrets(options => options.VaultUri = vaultUri);

// Bad: Secret value in appsettings.json or code
var apiKey = "hardcoded-secret-value"; // NEVER do this
```

### 2. Use Short-Lived Credentials

Set expiration when storing secrets to enforce rotation:

```csharp
await secrets.SetSecretAsync("api-token", newToken,
    new SecretOptions(ExpiresAtUtc: DateTime.UtcNow.AddDays(30)));
```

### 3. Enable Caching for Read-Heavy Workloads

Avoid hitting the vault on every request:

```csharp
services.AddEncinaSecretsCaching(options =>
{
    options.DefaultTtl = TimeSpan.FromMinutes(5);
});
```

Cache TTL should balance freshness against vault API rate limits.

### 4. Enable Health Checks in Production

Detect vault connectivity issues before they impact requests:

```csharp
services.AddEncinaKeyVaultSecrets(options =>
{
    options.ProviderHealthCheck.Enabled = true;
});
```

### 5. Enable Instrumentation for Monitoring

Track operation latency, error rates, and usage patterns:

```csharp
services.AddEncinaSecretsInstrumentation(options =>
{
    options.RecordSecretNames = true; // only in non-sensitive environments
});
```

### 6. Handle All Error Cases Explicitly

Never ignore the Left side of Either results:

```csharp
// Good: Handle both sides
var result = await secrets.GetSecretAsync("key", ct);
result.Match(
    Right: secret => Use(secret),
    Left: error => HandleError(error));

// Bad: Assume success
var secret = (await secrets.GetSecretAsync("key", ct))
    .Match(Right: s => s, Left: _ => throw new Exception()); // Defeats ROP purpose
```

### 7. Register Decorators in the Correct Order

Provider first, then caching, then instrumentation:

```csharp
services.AddEncinaKeyVaultSecrets(...);      // innermost
services.AddEncinaSecretsCaching(...);        // wraps provider
services.AddEncinaSecretsInstrumentation(...); // outermost (observes everything)
```

---

## Testing

### Unit Testing with a Fake Provider

Create a simple in-memory implementation for tests:

```csharp
public sealed class FakeSecretProvider : ISecretProvider
{
    private readonly Dictionary<string, Secret> _secrets = new();

    public void AddSecret(string name, string value) =>
        _secrets[name] = new Secret(name, value, "1", null);

    public ValueTask<Either<EncinaError, Secret>> GetSecretAsync(
        string name, CancellationToken ct = default) =>
        _secrets.TryGetValue(name, out var secret)
            ? ValueTask.FromResult<Either<EncinaError, Secret>>(secret)
            : ValueTask.FromResult<Either<EncinaError, Secret>>(
                SecretsErrorCodes.NotFound(name));

    // ... implement remaining methods
}
```

### Testing Error Handling

```csharp
[Fact]
public async Task Should_Handle_NotFound_Error()
{
    // Arrange
    var provider = Substitute.For<ISecretProvider>();
    provider.GetSecretAsync("missing", Arg.Any<CancellationToken>())
        .Returns(SecretsErrorCodes.NotFound("missing"));

    var service = new MyService(provider);

    // Act
    var result = await service.DoWork(CancellationToken.None);

    // Assert
    result.IsLeft.Should().BeTrue();
    result.Match(
        Right: _ => Assert.Fail("Expected Left"),
        Left: error => error.GetCode().IfNone("").Should().Be(SecretsErrorCodes.NotFoundCode));
}
```

### Testing the Cache Decorator

```csharp
[Fact]
public async Task CachedProvider_Should_Return_Cached_Value_On_Second_Call()
{
    // Arrange
    var inner = Substitute.For<ISecretProvider>();
    var secret = new Secret("key", "value", "1", null);
    inner.GetSecretAsync("key", Arg.Any<CancellationToken>())
        .Returns(secret);

    var cache = new MemoryCache(new MemoryCacheOptions());
    var options = Options.Create(new SecretCacheOptions { DefaultTtl = TimeSpan.FromMinutes(5) });
    var cached = new CachedSecretProvider(inner, cache, options, NullLogger<CachedSecretProvider>.Instance);

    // Act
    await cached.GetSecretAsync("key");
    await cached.GetSecretAsync("key");

    // Assert: inner provider called only once
    await inner.Received(1).GetSecretAsync("key", Arg.Any<CancellationToken>());
}
```

---

## FAQ

### Can I use multiple providers simultaneously?

No. `ISecretProvider` is registered as a single service. If you need secrets from multiple vaults, create a composite provider that routes based on secret name prefix or implement a custom provider that delegates to multiple underlying providers.

### What happens if the provider is unavailable?

The provider returns `Left(EncinaError)` with code `encina.secrets.provider_unavailable`. If caching is enabled, previously cached secrets continue to be served until their TTL expires. New requests for uncached secrets will fail until the provider recovers.

### Are secret values ever logged or recorded in telemetry?

No. Secret values are never logged, traced, or recorded in metrics. Secret names are only recorded in telemetry when `RecordSecretNames = true` (disabled by default).

### How does caching interact with secret rotation?

The `CachedSecretProvider` has a configurable TTL (default 5 minutes). After rotation, the old value is served from cache until the TTL expires. For immediate propagation, call `SetSecretAsync` (which invalidates the cache) or reduce the TTL.

### Can I use Encina.Secrets without a cloud provider?

Yes. You can implement `ISecretProvider` for any backend -- environment variables, encrypted files, or any key-value store. The core package (`Encina.Secrets`) has no cloud provider dependencies.

### How do I switch providers?

Change the DI registration. No code changes are needed in consumers:

```csharp
// Before: Azure Key Vault
services.AddEncinaKeyVaultSecrets(options => { ... });

// After: AWS Secrets Manager
services.AddEncinaAWSSecretsManager(options => { ... });
```

All consumers continue to depend on `ISecretProvider` and work unchanged.

### Does the IConfiguration bridge support IOptionsSnapshot?

Yes. When `ReloadInterval` is set, the `SecretConfigurationProvider` periodically reloads secrets from the provider. Standard .NET configuration change tokens propagate these updates to `IOptionsSnapshot<T>` and `IOptionsMonitor<T>`.

---

## Source Files

| File | Purpose |
|------|---------|
| `src/Encina.Secrets/ISecretProvider.cs` | Core 6-method interface |
| `src/Encina.Secrets/Secret.cs` | Secret record |
| `src/Encina.Secrets/SecretMetadata.cs` | SecretMetadata record |
| `src/Encina.Secrets/SecretOptions.cs` | Write operation options |
| `src/Encina.Secrets/SecretsErrorCodes.cs` | Error codes and factory methods |
| `src/Encina.Secrets/CachedSecretProvider.cs` | Caching decorator |
| `src/Encina.Secrets/SecretCacheOptions.cs` | Cache configuration |
| `src/Encina.Secrets/ServiceCollectionExtensions.cs` | DI registration (caching + instrumentation) |
| `src/Encina.Secrets/Configuration/ConfigurationBuilderExtensions.cs` | `AddEncinaSecrets()` extension |
| `src/Encina.Secrets/Configuration/SecretConfigurationOptions.cs` | Configuration bridge options |
| `src/Encina.Secrets/Configuration/SecretConfigurationProvider.cs` | IConfigurationProvider implementation |
| `src/Encina.Secrets/Configuration/SecretConfigurationSource.cs` | IConfigurationSource implementation |
| `src/Encina.Secrets/Diagnostics/InstrumentedSecretProvider.cs` | Instrumentation decorator |
| `src/Encina.Secrets/Diagnostics/SecretsActivitySource.cs` | ActivitySource "Encina.Secrets" |
| `src/Encina.Secrets/Diagnostics/SecretsMetrics.cs` | Meter "Encina.Secrets" |
| `src/Encina.Secrets/Diagnostics/SecretsInstrumentationOptions.cs` | Instrumentation options |
| `src/Encina.Secrets/Health/SecretsHealthCheck.cs` | Core health check |
| `src/Encina.Secrets/SecretProviderOptions.cs` | Base provider + health check options |
| `src/Encina.Secrets.AzureKeyVault/` | Azure Key Vault provider |
| `src/Encina.Secrets.AWSSecretsManager/` | AWS Secrets Manager provider |
| `src/Encina.Secrets.HashiCorpVault/` | HashiCorp Vault provider (KV v2) |
| `src/Encina.Secrets.GoogleSecretManager/` | Google Secret Manager provider |
