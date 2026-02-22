# Secrets Management in Encina

This guide explains how to manage application secrets using Encina's ISP-compliant, provider-agnostic secrets management system. All operations follow Railway Oriented Programming (ROP), returning `Either<EncinaError, T>` instead of throwing exceptions.

## Table of Contents

1. [Overview](#overview)
2. [The Problem](#the-problem)
3. [The Solution](#the-solution)
4. [Quick Start](#quick-start)
5. [Architecture](#architecture)
6. [Providers](#providers)
7. [Failover](#failover)
8. [Caching](#caching)
9. [Access Auditing](#access-auditing)
10. [Secret Injection](#secret-injection)
11. [Secret Rotation](#secret-rotation)
12. [Configuration Integration](#configuration-integration)
13. [Health Check](#health-check)
14. [Observability](#observability)
15. [Error Handling](#error-handling)
16. [Best Practices](#best-practices)
17. [Testing](#testing)
18. [FAQ](#faq)
19. [Source Files](#source-files)

---

## Overview

`Encina.Security.Secrets` provides a unified abstraction for secret management:

| Component | Description |
|-----------|-------------|
| **`ISecretReader`** | Read secrets (string or typed `T` via JSON) |
| **`ISecretWriter`** | Write/update secrets (separate interface — ISP) |
| **`ISecretRotator`** | Rotate secrets (optional) |
| **`ISecretRotationHandler`** | Custom rotation logic with callbacks |
| **`CachedSecretReaderDecorator`** | Transparent in-memory caching with TTL |
| **`AuditedSecret*Decorator`** | Automatic audit trail for read/write/rotate |
| **`FailoverSecretReader`** | Multi-provider chain with fallback |
| **`[InjectSecret]`** | Attribute-based secret injection into pipeline requests |
| **`SecretsConfigurationSource`** | Bridge secrets to `IConfiguration` |
| **`SecretsHealthCheck`** | Provider connectivity health check |

### Key Design Principles

| Principle | Implementation |
|-----------|---------------|
| **Interface Segregation** | `ISecretReader`, `ISecretWriter`, `ISecretRotator` — inject only what you need |
| **ROP everywhere** | All operations return `Either<EncinaError, T>` — no exceptions for business logic |
| **DI-first** | `AddEncinaSecrets()` registers everything; inject `ISecretReader` via constructor |
| **Pay-for-what-you-use** | Caching, auditing, injection, tracing — all opt-in via `SecretsOptions` |
| **Decorator pattern** | Cross-cutting concerns (caching, auditing) applied transparently |

---

## The Problem

Applications typically couple secret access to a specific provider SDK or environment, making it difficult to switch, test, or add cross-cutting concerns:

```csharp
// Problem 1: Tightly coupled to a specific SDK
var client = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());
var secret = await client.GetSecretAsync("my-secret");
// Switching to AWS requires rewriting every call site.

// Problem 2: No consistent error handling
try { var s = await client.GetSecretAsync("my-secret"); }
catch (RequestFailedException ex) when (ex.Status == 404) { /* Azure-specific */ }

// Problem 3: No caching, auditing, failover, or telemetry out of the box
```

---

## The Solution

With `Encina.Security.Secrets`, secret access is provider-agnostic with consistent ROP error handling:

```csharp
public class OrderService(ISecretReader secretReader)
{
    public async Task<Either<EncinaError, string>> GetApiKey(CancellationToken ct)
    {
        return await secretReader.GetSecretAsync("payment-api-key", ct);
    }
}
```

Switch providers by changing a single DI registration — no code changes needed.

---

## Quick Start

### 1. Install

```bash
dotnet add package Encina.Security.Secrets
```

### 2. Register

```csharp
services.AddLogging();
services.AddMemoryCache();
services.AddEncinaSecrets(); // Default: EnvironmentSecretProvider + caching
```

### 3. Use

```csharp
public class PaymentService(ISecretReader secretReader)
{
    public async Task<string> GetStripeKeyAsync(CancellationToken ct)
    {
        var result = await secretReader.GetSecretAsync("STRIPE_API_KEY", ct);
        return result.Match(
            Right: value => value,
            Left: error => throw new InvalidOperationException(error.Message));
    }
}
```

---

## Architecture

### Core Types

```
Encina.Security.Secrets
├── Abstractions/
│   ├── ISecretReader           — GetSecretAsync(name), GetSecretAsync<T>(name)
│   ├── ISecretWriter           — SetSecretAsync(name, value)
│   ├── ISecretRotator          — RotateSecretAsync(name)
│   └── ISecretRotationHandler  — GenerateNewSecretAsync + OnRotationAsync
├── Providers/
│   ├── EnvironmentSecretProvider    — Reads from environment variables
│   ├── ConfigurationSecretProvider  — Reads from IConfiguration sections
│   └── FailoverSecretReader         — Multi-provider chain
├── Caching/
│   └── CachedSecretReaderDecorator  — IMemoryCache decorator with TTL
├── Auditing/
│   ├── AuditedSecretReaderDecorator  — Read audit trail
│   ├── AuditedSecretWriterDecorator  — Write audit trail
│   └── AuditedSecretRotatorDecorator — Rotate audit trail
├── Injection/
│   ├── InjectSecretAttribute             — [InjectSecret("name")] attribute
│   ├── SecretInjectionPipelineBehavior   — Pipeline behavior
│   ├── SecretInjectionOrchestrator       — Discovers and injects secrets
│   └── SecretPropertyCache              — Reflection cache
├── Rotation/
│   └── SecretRotationCoordinator — generate → rotate → notify
├── Configuration/
│   ├── SecretsConfigurationSource    — IConfigurationSource bridge
│   ├── SecretsConfigurationProvider  — IConfigurationProvider implementation
│   └── SecretsConfigurationOptions   — Prefix, delimiter, reload
├── Diagnostics/
│   ├── SecretsActivitySource   — OpenTelemetry activities
│   ├── SecretsMetrics          — Counters and histograms
│   └── SecretsDiagnostics      — Internal facade
├── Health/
│   └── SecretsHealthCheck      — IHealthCheck implementation
├── SecretsOptions              — Configuration for all features
├── SecretsErrors               — 9 error codes + factory methods
├── SecretReference             — Immutable secret descriptor
└── ServiceCollectionExtensions — DI registration
```

### Decorator Pipeline

Decorators are applied transparently by `AddEncinaSecrets()` based on `SecretsOptions`:

```
Application Code (ISecretReader)
    │
    ▼
AuditedSecretReaderDecorator   ← EnableAccessAuditing = true
    │
    ▼
CachedSecretReaderDecorator    ← EnableCaching = true (default)
    │
    ▼
EnvironmentSecretProvider      ← Default provider (or custom TReader)
```

---

## Providers

### EnvironmentSecretProvider (Default)

Reads secrets from environment variables. Ideal for local development, Docker, and CI/CD:

```csharp
// Default: uses EnvironmentSecretProvider
services.AddEncinaSecrets();

// Secret name maps directly to environment variable name
// STRIPE_API_KEY → reader.GetSecretAsync("STRIPE_API_KEY")
```

### ConfigurationSecretProvider

Reads secrets from `IConfiguration` (appsettings.json, user secrets, etc.):

```csharp
services.AddEncinaSecrets<ConfigurationSecretProvider>();
```

Secrets are read from a configurable section (default: `"Secrets"`):

```json
{
  "Secrets": {
    "stripe-api-key": "sk_test_...",
    "db-password": "my-password"
  }
}
```

### Custom Provider

Implement `ISecretReader` for any backend:

```csharp
public class VaultSecretReader : ISecretReader
{
    public async ValueTask<Either<EncinaError, string>> GetSecretAsync(
        string secretName, CancellationToken ct = default)
    {
        // Call your vault SDK here
    }

    public async ValueTask<Either<EncinaError, T>> GetSecretAsync<T>(
        string secretName, CancellationToken ct = default)
    {
        // Get string, then deserialize to T
    }
}

services.AddEncinaSecrets<VaultSecretReader>();
```

Cloud vault providers will be available as separate satellite packages:
- `Encina.Security.Secrets.AzureKeyVault`
- `Encina.Security.Secrets.AWSSecretsManager`
- `Encina.Security.Secrets.HashiCorpVault`
- `Encina.Security.Secrets.GoogleSecretManager`

---

## Failover

`FailoverSecretReader` chains multiple readers — the first `Right` result wins:

```csharp
// Using the extension method
var reader = primaryReader.WithFailover(logger, secondaryReader, tertiaryReader);

// Or construct directly
var reader = new FailoverSecretReader(
    [primaryReader, secondaryReader, tertiaryReader],
    logger);

// Returns first Right result; if all fail, returns SecretsErrors.FailoverExhausted
var result = await reader.GetSecretAsync("api-key", ct);
```

---

## Caching

The `CachedSecretReaderDecorator` provides transparent in-memory caching via `IMemoryCache`:

```csharp
services.AddEncinaSecrets(options =>
{
    options.EnableCaching = true;                           // Default: true
    options.DefaultCacheDuration = TimeSpan.FromMinutes(5); // Default: 5 min
});
```

### Caching Behavior

| Behavior | Details |
|----------|---------|
| **Only Right cached** | Errors (Left) are never cached — subsequent calls retry |
| **TTL-based expiration** | Entries expire after `DefaultCacheDuration` |
| **Manual invalidation** | Call `Invalidate(secretName)` to evict a specific entry |
| **Cache key** | `secrets:{secretName}` |

---

## Access Auditing

When `EnableAccessAuditing` is set, read/write/rotate operations are automatically recorded via `IAuditStore`:

```csharp
services.AddEncinaSecrets(options =>
{
    options.EnableAccessAuditing = true;
});

// Register audit infrastructure (from Encina.Security.Audit)
services.AddSingleton<IAuditStore, YourAuditStore>();
services.AddSingleton<IRequestContext, YourRequestContext>();
```

Each audit entry captures: action (`SecretAccess`, `SecretWrite`, `SecretRotation`), entity, user, tenant, timing, and outcome. Audit failures are logged but **never** block secret operations.

---

## Secret Injection

Automatically inject secrets into pipeline request properties — no manual `GetSecretAsync` calls needed.

### Setup

```csharp
services.AddEncinaSecrets(options =>
{
    options.EnableSecretInjection = true; // Opt-in (default: false)
});
```

### Usage

Decorate request properties with `[InjectSecret("secret-name")]`:

```csharp
public sealed class ProcessPaymentCommand : IRequest<Unit>
{
    [InjectSecret("stripe-api-key")]
    public string StripeApiKey { get; set; } = "";

    [InjectSecret("stripe-webhook-secret", FailOnError = false)]
    public string WebhookSecret { get; set; } = "";

    [InjectSecret("encryption-key", Version = "v2")]
    public string EncryptionKey { get; set; } = "";

    public decimal Amount { get; set; }
}
```

When `ProcessPaymentCommand` flows through the pipeline, `SecretInjectionPipelineBehavior` resolves each `[InjectSecret]` property from `ISecretReader` and sets the value automatically before the handler executes.

### Attribute Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `SecretName` | `string` | (required) | The secret name to resolve |
| `Version` | `string?` | `null` | Appends `/{version}` to the secret name |
| `FailOnError` | `bool` | `true` | If `false`, missing secrets are silently skipped |

---

## Secret Rotation

### Rotation Handler

Register custom rotation handlers:

```csharp
services.AddSecretRotationHandler<DatabasePasswordRotationHandler>();

public class DatabasePasswordRotationHandler : ISecretRotationHandler
{
    public async ValueTask<Either<EncinaError, string>> GenerateNewSecretAsync(
        string secretName, CancellationToken ct)
    {
        return Right(GenerateStrongPassword());
    }

    public async ValueTask<Either<EncinaError, Unit>> OnRotationAsync(
        string secretName, string oldValue, string newValue, CancellationToken ct)
    {
        await UpdateDatabasePasswordAsync(newValue, ct);
        return Right(Unit.Default);
    }
}
```

### Rotation Coordinator

`SecretRotationCoordinator` orchestrates the full rotation workflow — generate → rotate → notify:

```csharp
var coordinator = sp.GetRequiredService<SecretRotationCoordinator>();
var result = await coordinator.RotateWithCallbacksAsync("db-password", ct);
```

---

## Configuration Integration

Bridge secrets into the standard .NET `IConfiguration` system:

```csharp
var config = new ConfigurationBuilder()
    .AddEncinaSecrets(serviceProvider, options =>
    {
        options.SecretNames = ["ConnectionStrings--Default", "ApiKeys--Stripe"];
        options.SecretPrefix = "myapp/";
        options.StripPrefix = true;        // "myapp/key" → "key"
        options.KeyDelimiter = "--";        // "a--b" → "a:b" in config
        options.ReloadInterval = TimeSpan.FromMinutes(5); // Periodic reload
    })
    .Build();

// Access secrets via standard IConfiguration
var connectionString = config["ConnectionStrings:Default"];
```

### Key Delimiter Mapping

Secret names in vaults often cannot contain `:` (the standard configuration separator). The `KeyDelimiter` maps an alternative character sequence to `:`:

| Secret Name | KeyDelimiter | Configuration Key |
|-------------|:---:|-------------------|
| `Database--ConnectionString` | `--` | `Database:ConnectionString` |
| `App__Settings__Timeout` | `__` | `App:Settings:Timeout` |

### Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `SecretNames` | `IReadOnlyList<string>` | `[]` | Secrets to load |
| `SecretPrefix` | `string?` | `null` | Filter secrets by name prefix |
| `StripPrefix` | `bool` | `true` | Remove prefix from configuration keys |
| `KeyDelimiter` | `string` | `"--"` | Delimiter mapped to `:` in hierarchy |
| `ReloadInterval` | `TimeSpan?` | `null` | Periodic reload interval (null = load once) |

---

## Health Check

`SecretsHealthCheck` verifies that `ISecretReader` is registered and resolvable. Optionally probes a specific secret:

```csharp
services.AddEncinaSecrets(options =>
{
    options.ProviderHealthCheck = true;
    options.HealthCheckSecretName = "health-probe-key"; // Optional probe
});
```

Health check name: `encina-secrets`
Tags: `["encina", "secrets", "ready"]`

---

## Observability

### OpenTelemetry Tracing

Activity source: `Encina.Security.Secrets`

| Activity | Description |
|----------|-------------|
| `Secrets.GetSecret` | Reading a secret |
| `Secrets.SetSecret` | Writing a secret |
| `Secrets.RotateSecret` | Rotating a secret |
| `Secrets.InjectSecrets` | Injecting secrets into a pipeline request |

### Metrics

Meter: `Encina.Security.Secrets`

| Instrument | Type | Description |
|------------|------|-------------|
| `secrets.operations` | Counter | Total secret operations |
| `secrets.errors` | Counter | Failed operations |
| `secrets.cache_hits` | Counter | Cache hits |
| `secrets.cache_misses` | Counter | Cache misses |
| `secrets.operation_duration` | Histogram | Duration in ms |

### Enable

```csharp
services.AddEncinaSecrets(options =>
{
    options.EnableTracing = true;
    options.EnableMetrics = true;
});

// Subscribe in OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(t => t.AddSource("Encina.Security.Secrets"))
    .WithMetrics(m => m.AddMeter("Encina.Security.Secrets"));
```

---

## Error Handling

### Error Codes

| Code | Constant | Meaning |
|------|----------|---------|
| `secrets.not_found` | `SecretsErrors.NotFoundCode` | Secret does not exist |
| `secrets.access_denied` | `SecretsErrors.AccessDeniedCode` | Permission denied |
| `secrets.provider_unavailable` | `SecretsErrors.ProviderUnavailableCode` | Provider unreachable |
| `secrets.deserialization_failed` | `SecretsErrors.DeserializationFailedCode` | JSON deserialization failed |
| `secrets.failover_exhausted` | `SecretsErrors.FailoverExhaustedCode` | All failover providers failed |
| `secrets.injection_failed` | `SecretsErrors.InjectionFailedCode` | Secret injection error |
| `secrets.rotation_failed` | `SecretsErrors.RotationFailedCode` | Rotation error |
| `secrets.audit_failed` | `SecretsErrors.AuditFailedCode` | Audit recording error |
| `secrets.cache_failure` | `SecretsErrors.CacheFailureCode` | Cache operation error |

### Common Patterns

**Match for branching**:

```csharp
var result = await reader.GetSecretAsync("api-key", ct);
result.Match(
    Right: value => UseSecret(value),
    Left: error => Log.Error("Error: {Code}", error.GetCode().IfNone("unknown")));
```

**Default value on not found**:

```csharp
var value = result.Match(Right: v => v, Left: _ => "default-value");
```

**Propagate errors through the pipeline**:

```csharp
return await reader.GetSecretAsync("payment-key", ct)
    .MapAsync(value => CallPaymentApi(value, command));
```

---

## Best Practices

1. **Never store secrets in source code** — use environment variables or vault references
2. **Enable caching for read-heavy workloads** — balance TTL against freshness needs
3. **Use `FailOnError = false`** on `[InjectSecret]` for optional secrets
4. **Enable health checks in production** — detect vault issues before they impact requests
5. **Enable tracing and metrics** — track operation latency and error rates
6. **Handle all error cases explicitly** — never ignore the Left side of Either results
7. **Use typed secrets** (`GetSecretAsync<T>`) for complex configuration objects
8. **Register audit infrastructure** when `EnableAccessAuditing = true` — otherwise audit decorators silently skip

---

## Testing

### Unit Testing with NSubstitute

```csharp
var reader = Substitute.For<ISecretReader>();
reader.GetSecretAsync("api-key", Arg.Any<CancellationToken>())
    .Returns(_ => new ValueTask<Either<EncinaError, string>>("test-value"));

var service = new MyService(reader);
var result = await service.DoWork(CancellationToken.None);

result.IsRight.Should().BeTrue();
```

### Testing Error Handling

```csharp
reader.GetSecretAsync("missing", Arg.Any<CancellationToken>())
    .Returns(_ => new ValueTask<Either<EncinaError, string>>(
        SecretsErrors.NotFound("missing")));

var result = await service.DoWork(CancellationToken.None);
result.IsLeft.Should().BeTrue();
```

### Testing the Cache Decorator

```csharp
var decorator = new CachedSecretReaderDecorator(inner, cache, options, logger);

await decorator.GetSecretAsync("key");
await decorator.GetSecretAsync("key"); // Cache hit

await inner.Received(1).GetSecretAsync("key", Arg.Any<CancellationToken>());
```

---

## FAQ

### Can I use multiple providers simultaneously?

Yes — use `FailoverSecretReader` to chain multiple providers. The first `Right` result wins:

```csharp
var reader = primaryReader.WithFailover(logger, secondaryReader, tertiaryReader);
```

### What happens if the provider is unavailable?

The provider returns `Left(EncinaError)` with code `secrets.provider_unavailable`. If caching is enabled, previously cached secrets continue to be served until their TTL expires.

### Are secret values ever logged?

No. Secret values are never logged, traced, or recorded in metrics. Only operation names and error codes appear in telemetry.

### How does `[InjectSecret]` differ from constructor injection of `ISecretReader`?

`[InjectSecret]` is a secondary pattern — convenient for pipeline requests that need multiple secrets resolved before the handler executes. Constructor injection of `ISecretReader` is the primary pattern for general-purpose secret access.

### Can I use Encina.Security.Secrets without a cloud provider?

Yes. The package ships with `EnvironmentSecretProvider` and `ConfigurationSecretProvider` for development. Implement `ISecretReader` for any backend.

---

## Source Files

| File | Purpose |
|------|---------|
| `src/Encina.Security.Secrets/Abstractions/ISecretReader.cs` | Read interface (string + typed) |
| `src/Encina.Security.Secrets/Abstractions/ISecretWriter.cs` | Write interface |
| `src/Encina.Security.Secrets/Abstractions/ISecretRotator.cs` | Rotate interface |
| `src/Encina.Security.Secrets/Abstractions/ISecretRotationHandler.cs` | Rotation callbacks |
| `src/Encina.Security.Secrets/Providers/EnvironmentSecretProvider.cs` | Environment variable reader |
| `src/Encina.Security.Secrets/Providers/ConfigurationSecretProvider.cs` | IConfiguration reader |
| `src/Encina.Security.Secrets/Providers/FailoverSecretReader.cs` | Multi-provider chain |
| `src/Encina.Security.Secrets/Caching/CachedSecretReaderDecorator.cs` | IMemoryCache caching |
| `src/Encina.Security.Secrets/Auditing/AuditedSecretReaderDecorator.cs` | Read audit trail |
| `src/Encina.Security.Secrets/Auditing/AuditedSecretWriterDecorator.cs` | Write audit trail |
| `src/Encina.Security.Secrets/Auditing/AuditedSecretRotatorDecorator.cs` | Rotate audit trail |
| `src/Encina.Security.Secrets/Attributes/InjectSecretAttribute.cs` | `[InjectSecret]` attribute |
| `src/Encina.Security.Secrets/Injection/SecretInjectionPipelineBehavior.cs` | Pipeline behavior |
| `src/Encina.Security.Secrets/Injection/SecretInjectionOrchestrator.cs` | Injection orchestrator |
| `src/Encina.Security.Secrets/Injection/SecretPropertyCache.cs` | Reflection cache |
| `src/Encina.Security.Secrets/Rotation/SecretRotationCoordinator.cs` | Rotation orchestrator |
| `src/Encina.Security.Secrets/Configuration/SecretsConfigurationSource.cs` | IConfigurationSource |
| `src/Encina.Security.Secrets/Configuration/SecretsConfigurationProvider.cs` | IConfigurationProvider |
| `src/Encina.Security.Secrets/Configuration/SecretsConfigurationOptions.cs` | Configuration bridge options |
| `src/Encina.Security.Secrets/Diagnostics/SecretsActivitySource.cs` | OpenTelemetry activities |
| `src/Encina.Security.Secrets/Diagnostics/SecretsMetrics.cs` | Meter + instruments |
| `src/Encina.Security.Secrets/Diagnostics/SecretsDiagnostics.cs` | Diagnostics facade |
| `src/Encina.Security.Secrets/Health/SecretsHealthCheck.cs` | Health check |
| `src/Encina.Security.Secrets/SecretsOptions.cs` | Configuration options |
| `src/Encina.Security.Secrets/SecretsErrors.cs` | Error codes + factories |
| `src/Encina.Security.Secrets/SecretReference.cs` | Secret descriptor |
| `src/Encina.Security.Secrets/ServiceCollectionExtensions.cs` | DI registration |
