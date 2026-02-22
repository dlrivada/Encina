# Encina.Security.Secrets

[![NuGet](https://img.shields.io/nuget/v/Encina.Security.Secrets.svg)](https://www.nuget.org/packages/Encina.Security.Secrets)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)

**Secrets management abstractions for Encina — ISP-compliant interfaces with DI-first pattern, in-memory caching, and IConfiguration integration.**

Encina.Security.Secrets provides a unified API for reading, writing, and rotating secrets across any vault provider (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault, GCP Secret Manager). It ships with development-ready providers (environment variables, `IConfiguration`) and a transparent caching decorator. All operations follow Encina's Railway Oriented Programming pattern with `Either<EncinaError, T>`.

## Key Features

- **Interface Segregation** — `ISecretReader`, `ISecretWriter`, `ISecretRotator` (inject only what you need)
- **DI-first** — register with `AddEncinaSecrets()`, inject `ISecretReader` via constructor
- **Railway Oriented Programming** — all operations return `Either<EncinaError, T>`
- **In-memory caching** — transparent `CachedSecretReaderDecorator` (enabled by default)
- **Typed secrets** — `GetSecretAsync<T>()` with JSON deserialization
- **IConfiguration bridge** — `SecretsConfigurationSource` exposes secrets as configuration keys
- **Multi-provider failover** — `FailoverSecretReader` tries providers in order
- **Audit trail** — `AuditedSecretReaderDecorator`, `AuditedSecretWriterDecorator`, `AuditedSecretRotatorDecorator`
- **Rotation coordinator** — `SecretRotationCoordinator` orchestrates generate → rotate → notify
- **Development providers** — `EnvironmentSecretProvider` and `ConfigurationSecretProvider`
- **Secret rotation** — `ISecretRotationHandler` for custom rotation logic
- **Health check** for provider connectivity (opt-in)

## Quick Start

```csharp
// 1. Register services (defaults: EnvironmentSecretProvider + caching enabled)
services.AddLogging();
services.AddMemoryCache();
services.AddEncinaSecrets();

// 2. Inject and use
public class PaymentService(ISecretReader secretReader)
{
    public async Task<string> GetStripeKeyAsync(CancellationToken ct)
    {
        var result = await secretReader.GetSecretAsync("stripe-api-key", ct);
        return result.Match(
            Right: value => value,
            Left: error => throw new InvalidOperationException(error.Message));
    }
}
```

## Typed Secrets

Deserialize JSON secrets directly into strongly-typed objects:

```csharp
public sealed class DatabaseConfig
{
    public string Host { get; set; } = "";
    public int Port { get; set; }
    public string Password { get; set; } = "";
}

var result = await secretReader.GetSecretAsync<DatabaseConfig>("db-config", ct);
result.IfRight(config =>
{
    // config.Host, config.Port, config.Password are available
});
```

## Configuration Options

```csharp
services.AddEncinaSecrets(options =>
{
    options.EnableCaching = true;                           // Default: true
    options.DefaultCacheDuration = TimeSpan.FromMinutes(5); // Default: 5 min
    options.ProviderHealthCheck = true;                     // Default: false
    options.EnableAutoRotation = false;                     // Default: false
    options.EnableFailover = false;                         // Default: false
    options.EnableAccessAuditing = false;                   // Default: false
    options.KeyPrefix = "myapp/";                           // Default: null
});
```

## Custom Provider

Register a specific reader (e.g., `ConfigurationSecretProvider` for appsettings.json):

```csharp
services.AddEncinaSecrets<ConfigurationSecretProvider>(options =>
{
    options.EnableCaching = true;
});
```

## Production Vault Provider

Cloud vault providers will be available as separate packages:

```csharp
// Future: Encina.Security.Secrets.AzureKeyVault
services.AddEncinaSecrets<AzureKeyVaultSecretProvider>(options =>
{
    options.ProviderHealthCheck = true;
});
```

## Secret Rotation

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

## Multi-Provider Failover

Build a failover chain so secret retrieval falls through to secondary providers when the primary fails:

```csharp
// Using the extension method
var reader = primaryReader.WithFailover(logger, secondaryReader, tertiaryReader);

// Or construct directly
var reader = new FailoverSecretReader(
    [primaryReader, secondaryReader, tertiaryReader],
    logger);

// Returns the first Right result; if all fail, returns SecretsErrors.FailoverExhausted
var result = await reader.GetSecretAsync("api-key", ct);
```

## Audit Trail

When `EnableAccessAuditing` is set, read/write/rotation operations are automatically recorded via `IAuditStore`:

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

## Rotation Coordinator

`SecretRotationCoordinator` orchestrates the full rotation workflow — generate → rotate → notify:

```csharp
services.AddSecretRotationHandler<DatabasePasswordRotationHandler>();

// Resolve and use
var coordinator = sp.GetRequiredService<SecretRotationCoordinator>();
var result = await coordinator.RotateWithCallbacksAsync("db-password", ct);
```

## IConfiguration Bridge

Expose secrets as configuration keys (useful for libraries expecting `IConfiguration`):

```csharp
var config = new ConfigurationBuilder()
    .AddEncinaSecrets(secretReader, ["ConnectionStrings:Default", "ApiKeys:Stripe"])
    .Build();

// Access secrets via standard IConfiguration
var connectionString = config["ConnectionStrings:Default"];
```

## Attribute-Based Secret Injection

Automatically inject secrets into pipeline request properties using `[InjectSecret]`. The pipeline behavior resolves secrets from `ISecretReader` before the handler executes — no manual `GetSecretAsync` calls needed.

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

When `ProcessPaymentCommand` flows through the pipeline, `StripeApiKey`, `WebhookSecret`, and `EncryptionKey` are resolved from `ISecretReader` and set automatically.

### Attribute Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `SecretName` | `string` | (required) | The secret name to resolve |
| `Version` | `string?` | `null` | Appends `/{version}` to the secret name |
| `FailOnError` | `bool` | `true` | If `false`, missing secrets are silently skipped |

### Observability

Enable tracing and metrics for injection operations:

```csharp
services.AddEncinaSecrets(options =>
{
    options.EnableSecretInjection = true;
    options.EnableTracing = true;   // OpenTelemetry activities
    options.EnableMetrics = true;   // secrets.injections, secrets.properties_injected
});
```

## Documentation

- [CHANGELOG](../../CHANGELOG.md) — version history and release notes
- [Architecture Decision Records](../../docs/architecture/adr/) — design decisions

## Dependencies

- `Encina` (core abstractions)
- `Encina.Security.Audit` (audit trail integration)
- `Microsoft.Extensions.Caching.Memory`
- `Microsoft.Extensions.Configuration`
- `Microsoft.Extensions.Configuration.Abstractions`
- `Microsoft.Extensions.Configuration.Binder`
- `Microsoft.Extensions.DependencyInjection.Abstractions`
- `Microsoft.Extensions.Diagnostics.HealthChecks`
- `Microsoft.Extensions.Logging.Abstractions`
- `Microsoft.Extensions.Options`

## License

MIT License. See [LICENSE](../../LICENSE) for details.
