# Secrets Management -- IConfiguration Integration

This guide explains how to bridge Encina Secrets into the standard .NET `IConfiguration` system, so secrets stored in Azure Key Vault, AWS Secrets Manager, HashiCorp Vault, or Google Secret Manager become accessible through `IConfiguration["Key"]` and `IOptions<T>` bindings.

## When to Use This

Use the configuration bridge when you want to:

- Bind secrets to strongly-typed `IOptions<T>` classes
- Pass secrets to libraries that read from `IConfiguration` (e.g., EF Core connection strings)
- Unify secret access with other configuration sources (appsettings.json, environment variables)
- Enable periodic secret refresh without restarting the application

For direct programmatic access to secrets (with full ROP error handling), inject `ISecretProvider` directly instead. See [Basic Setup](secrets-basic-setup.md).

## Registration

Register a secret provider first, then add the configuration source:

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. Register the secret provider
builder.Services.AddEncinaKeyVaultSecrets(options =>
{
    options.VaultUri = "https://my-vault.vault.azure.net/";
});

// 2. Build a service provider to resolve ISecretProvider
var sp = builder.Services.BuildServiceProvider();

// 3. Add secrets as a configuration source
builder.Configuration.AddEncinaSecrets(sp, options =>
{
    options.SecretPrefix = "MyApp";
    options.StripPrefix = true;
    options.KeyDelimiter = "--";
    options.ReloadInterval = TimeSpan.FromMinutes(5);
});
```

The configuration source calls `ListSecretsAsync()` to discover available secrets and `GetSecretAsync()` to load each value during the `Load()` phase.

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `SecretPrefix` | `string?` | `null` | When set, only secrets whose names start with this prefix are loaded |
| `StripPrefix` | `bool` | `true` | When `true`, removes the prefix from the configuration key |
| `KeyDelimiter` | `string` | `"--"` | Delimiter in secret names that maps to the `:` section separator |
| `ReloadInterval` | `TimeSpan?` | `null` | When set, secrets are reloaded periodically on a background timer |

## Key Mapping

Secret names are converted to configuration keys through two transformations:

1. **Prefix stripping** (when `StripPrefix = true`)
2. **Delimiter replacement** (the `KeyDelimiter` is replaced with `:`)

### Example Mapping

Given this configuration:

```csharp
options.SecretPrefix = "MyApp";
options.StripPrefix = true;
options.KeyDelimiter = "--";
```

| Secret Name in Vault | Configuration Key | Access Pattern |
|----------------------|-------------------|----------------|
| `MyApp--Database--ConnectionString` | `Database:ConnectionString` | `config["Database:ConnectionString"]` |
| `MyApp--Jwt--Secret` | `Jwt:Secret` | `config["Jwt:Secret"]` |
| `MyApp--ApiKey` | `ApiKey` | `config["ApiKey"]` |
| `OtherApp--Something` | *(not loaded)* | Filtered out by prefix |

### Without Prefix

When `SecretPrefix` is `null` (default), all secrets are loaded and only the delimiter replacement applies:

```csharp
builder.Configuration.AddEncinaSecrets(sp, options =>
{
    options.KeyDelimiter = "--";
});
```

| Secret Name | Configuration Key |
|-------------|-------------------|
| `Database--ConnectionString` | `Database:ConnectionString` |
| `ApiKey` | `ApiKey` |

### Custom Delimiters

Different vaults use different naming conventions. Adjust the delimiter to match:

```csharp
// AWS style: forward slash delimiter
options.KeyDelimiter = "/";
// Secret "myapp/database/password" -> config["myapp:database:password"]

// Azure style: double-dash delimiter (default)
options.KeyDelimiter = "--";
// Secret "myapp--database--password" -> config["myapp:database:password"]
```

## Accessing Mapped Secrets

Once loaded, secrets are accessible through all standard `IConfiguration` patterns:

### Direct Access

```csharp
app.MapGet("/test", (IConfiguration config) =>
{
    var connString = config["Database:ConnectionString"];
    return connString is not null ? "Connected" : "No connection string";
});
```

### Options Binding

```csharp
public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; }
}

// In Program.cs:
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("Jwt"));

// In a service:
public class AuthService
{
    private readonly JwtSettings _jwt;

    public AuthService(IOptions<JwtSettings> options)
        => _jwt = options.Value;
}
```

This works because secrets named `MyApp--Jwt--Secret`, `MyApp--Jwt--Issuer`, and `MyApp--Jwt--ExpirationMinutes` map to the `Jwt:Secret`, `Jwt:Issuer`, and `Jwt:ExpirationMinutes` keys, which `GetSection("Jwt")` resolves correctly.

### Connection Strings

```csharp
// Secret: "MyApp--ConnectionStrings--Default"
// Maps to: "ConnectionStrings:Default"
var connString = builder.Configuration.GetConnectionString("Default");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connString));
```

## Auto-Reload Behavior

When `ReloadInterval` is set, a background timer periodically calls `Load()` to refresh all secrets from the provider:

```csharp
options.ReloadInterval = TimeSpan.FromMinutes(5);
```

**How it works:**

1. The `SecretConfigurationProvider` sets up a `Timer` after the initial load
2. Every `ReloadInterval`, the timer fires and calls `Load()` again
3. After loading, `OnReload()` is called, which triggers `IOptionsMonitor<T>` change notifications

**Consuming reload notifications:**

```csharp
public class MyService
{
    private readonly IOptionsMonitor<JwtSettings> _jwtMonitor;

    public MyService(IOptionsMonitor<JwtSettings> jwtMonitor)
    {
        _jwtMonitor = jwtMonitor;

        // React to secret changes
        _jwtMonitor.OnChange(settings =>
        {
            Console.WriteLine($"JWT secret was updated. New issuer: {settings.Issuer}");
        });
    }

    public JwtSettings CurrentSettings => _jwtMonitor.CurrentValue;
}
```

Use `IOptionsMonitor<T>` (not `IOptions<T>`) to receive updated values after a reload.

## Limitations and Considerations

### Synchronous Load

The `ConfigurationProvider.Load()` method is synchronous by design in .NET. The Encina implementation uses `GetAwaiter().GetResult()` to bridge the async `ISecretProvider` calls. This is safe at application startup but means:

- Startup time includes the round-trip to the vault
- Network errors during startup will be logged as warnings (the application still starts)

### Cold Start Delay

Each secret requires one network call. If your vault contains many secrets, consider:

- Using `SecretPrefix` to load only the secrets your application needs
- Combining related values into a single secret (JSON) and parsing them after load

### Error Handling

Errors during load are logged as warnings but do **not** prevent the application from starting. If `ListSecretsAsync()` fails, no secrets are loaded. If an individual `GetSecretAsync()` fails, that secret is skipped and the rest continue loading.

### Configuration Source Ordering

The configuration source added last wins when keys conflict. Place `AddEncinaSecrets()` after `AddJsonFile()` to ensure vault secrets override local appsettings:

```csharp
builder.Configuration
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{env}.json", optional: true)
    .AddEncinaSecrets(sp, options => { ... }); // Vault values win
```

## Full Example

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register the provider
builder.Services.AddEncinaKeyVaultSecrets(options =>
{
    options.VaultUri = builder.Configuration["KeyVault:Uri"]!;
});

// Add caching to reduce vault calls
builder.Services.AddEncinaSecretsCaching(options =>
{
    options.DefaultTtl = TimeSpan.FromMinutes(10);
});

// Bridge into IConfiguration
var sp = builder.Services.BuildServiceProvider();
builder.Configuration.AddEncinaSecrets(sp, options =>
{
    options.SecretPrefix = "MyApp";
    options.StripPrefix = true;
    options.KeyDelimiter = "--";
    options.ReloadInterval = TimeSpan.FromMinutes(5);
});

// Bind options from secrets
builder.Services.Configure<DatabaseSettings>(
    builder.Configuration.GetSection("Database"));

var app = builder.Build();
app.Run();
```

## Related

- [Basic Setup](secrets-basic-setup.md)
- [Caching Strategy](secrets-caching-strategy.md)
- [Error Handling with ROP](secrets-error-handling.md)
