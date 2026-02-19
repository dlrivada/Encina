# Secrets Management -- Error Handling with ROP

This guide covers how to handle errors from Encina Secrets using the Railway Oriented Programming (ROP) pattern. Every `ISecretProvider` method returns `Either<EncinaError, T>`, and this guide shows how to branch, transform, chain, and inspect errors using `Match()`, `Map()`, `Bind()`, and the error metadata API.

## The ROP Pattern in Encina Secrets

Every operation returns one of two tracks:

- **Right track** (`T`): The operation succeeded. Contains the result (`Secret`, `SecretMetadata`, `bool`, `IEnumerable<string>`, or `Unit`).
- **Left track** (`EncinaError`): The operation failed. Contains a structured error with a code, message, and metadata dictionary.

This pattern eliminates the need for try/catch around secret operations. Errors are values, not exceptions.

## Match: Branching on Success or Failure

`Match()` is the primary way to handle both tracks. It forces you to handle both cases explicitly.

### Basic Match

```csharp
var result = await provider.GetSecretAsync("api-key", ct);

result.Match(
    Right: secret => Console.WriteLine($"Got {secret.Name} v{secret.Version}"),
    Left: error => Console.WriteLine($"Failed: {error.Message}")
);
```

### Match with Return Value

```csharp
var result = await provider.GetSecretAsync("api-key", ct);

var output = result.Match(
    Right: secret => $"Using API key v{secret.Version}",
    Left: error => $"Failed: {error.Message}"
);

Console.WriteLine(output);
```

### Match in an API Endpoint

```csharp
app.MapGet("/config", async (ISecretProvider provider) =>
{
    var result = await provider.GetSecretAsync("app-config");

    return result.Match(
        Right: secret => Results.Ok(new { secret.Name, secret.Version }),
        Left: error => Results.Problem(
            detail: error.Message,
            statusCode: StatusCodes.Status503ServiceUnavailable)
    );
});
```

## Map: Transforming Success Values

`Map()` transforms the `Right` value without affecting the `Left` track. If the result is an error, `Map()` short-circuits and returns the error unchanged.

```csharp
var result = await provider.GetSecretAsync("db-conn", ct);

// Extract just the connection string value
Either<EncinaError, string> connectionString = result.Map(secret => secret.Value);

// Chain multiple transformations
Either<EncinaError, int> valueLength = result
    .Map(secret => secret.Value)
    .Map(value => value.Length);
```

### Practical Example: Extract and Use a Value

```csharp
var result = await provider.GetSecretAsync("jwt-secret", ct);

var signingKey = result.Map(secret =>
    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret.Value)));

signingKey.Match(
    Right: key => ConfigureJwtAuth(key),
    Left: error => logger.LogError("Cannot configure JWT: {Message}", error.Message)
);
```

## Bind: Chaining Dependent Operations

`Bind()` (also known as `FlatMap`) chains operations where the next step also returns `Either`. If the first operation fails, the chain short-circuits without executing the second operation.

### Chaining Two Secret Lookups

```csharp
var result = await provider.GetSecretAsync("primary-key", ct);

// If primary-key fails, this never executes
var chained = result.Bind(primary =>
{
    // Use the primary key to derive which secondary key to fetch
    var secondaryName = $"secondary-{primary.Version}";
    return provider.GetSecretAsync(secondaryName, ct).AsTask().GetAwaiter().GetResult();
});
```

### Async Chaining Pattern

For fully async chains, use `MatchAsync` or await each step:

```csharp
var primaryResult = await provider.GetSecretAsync("primary-key", ct);

var finalResult = await primaryResult.MatchAsync(
    RightAsync: async primary =>
    {
        var secondary = await provider.GetSecretAsync($"backup-{primary.Version}", ct);
        return secondary;
    },
    Left: error => error
);
```

## IfRight / IfLeft: Side Effects

When you only need to act on one track without transforming the result:

```csharp
var result = await provider.GetSecretAsync("api-key", ct);

// Log on success (does nothing if Left)
result.IfRight(secret =>
    logger.LogInformation("Loaded secret {Name} v{Version}", secret.Name, secret.Version));

// Log on failure (does nothing if Right)
result.IfLeft(error =>
    logger.LogWarning("Secret retrieval failed: {Message}", error.Message));
```

## Accessing Error Metadata

`EncinaError` carries structured metadata. Use the extension methods `GetCode()` and `GetDetails()` to extract it.

### GetCode()

Returns `Option<string>` containing the error code. Use `IfSome()`/`Match()` to extract the value:

```csharp
result.IfLeft(error =>
{
    // GetCode() returns Option<string>
    error.GetCode().IfSome(code =>
        Console.WriteLine($"Error code: {code}"));

    // Or extract with a default
    var code = error.GetCode().IfNone("unknown");
    Console.WriteLine($"Code: {code}");
});
```

### GetDetails()

Returns `IReadOnlyDictionary<string, object?>` with error-specific metadata:

```csharp
result.IfLeft(error =>
{
    var details = error.GetDetails();

    if (details.TryGetValue("secretName", out var name))
        Console.WriteLine($"Secret: {name}");

    if (details.TryGetValue("reason", out var reason))
        Console.WriteLine($"Reason: {reason}");

    if (details.TryGetValue("stage", out var stage))
        Console.WriteLine($"Stage: {stage}");
});
```

### Pattern Matching on Error Codes

```csharp
var result = await provider.GetSecretAsync("api-key", ct);

result.Match(
    Right: secret =>
    {
        // Use the secret
        ConfigureApiClient(secret.Value);
    },
    Left: error =>
    {
        var code = error.GetCode().IfNone("unknown");

        switch (code)
        {
            case SecretsErrorCodes.NotFoundCode:
                logger.LogWarning("Secret not found, using fallback configuration");
                UseFallbackConfig();
                break;

            case SecretsErrorCodes.AccessDeniedCode:
                logger.LogError("Access denied to secret. Check IAM/RBAC permissions.");
                throw new UnauthorizedAccessException(error.Message);

            case SecretsErrorCodes.ProviderUnavailableCode:
                logger.LogError("Vault is unreachable. Retrying in 30 seconds...");
                ScheduleRetry(TimeSpan.FromSeconds(30));
                break;

            case SecretsErrorCodes.InvalidNameCode:
                logger.LogError("Bug: invalid secret name passed to provider");
                throw new ArgumentException(error.Message);

            case SecretsErrorCodes.VersionNotFoundCode:
                logger.LogWarning("Requested version not found, falling back to latest");
                FetchLatestVersion();
                break;

            case SecretsErrorCodes.OperationFailedCode:
            default:
                logger.LogError("Unexpected secret error [{Code}]: {Message}", code, error.Message);
                break;
        }
    }
);
```

## Error Codes Reference

All error codes are defined as constants in `SecretsErrorCodes`:

| Constant | Code String | Description | Common Causes |
|----------|-------------|-------------|---------------|
| `NotFoundCode` | `encina.secrets.not_found` | The requested secret does not exist | Typo in secret name; secret was deleted; wrong vault |
| `AccessDeniedCode` | `encina.secrets.access_denied` | Insufficient permissions to access the secret | Missing IAM role; expired token; wrong RBAC policy |
| `InvalidNameCode` | `encina.secrets.invalid_name` | The secret name is empty, too long, or contains forbidden characters | Empty string passed; unsupported characters for the provider |
| `ProviderUnavailableCode` | `encina.secrets.provider_unavailable` | The vault service is unreachable | Network error; DNS failure; vault sealed (HashiCorp); authentication failure |
| `VersionNotFoundCode` | `encina.secrets.version_not_found` | The requested version of the secret does not exist | Wrong version ID; version was purged |
| `OperationFailedCode` | `encina.secrets.operation_failed` | Generic failure not covered by a more specific code | Serialization error; unexpected provider response; timeout |

### Error Metadata Fields

Each error carries a `details` dictionary with context-specific metadata:

| Factory Method | Available Fields |
|---------------|------------------|
| `NotFound(name)` | `secretName`, `stage` |
| `AccessDenied(name, reason)` | `secretName`, `reason`, `stage` |
| `InvalidName(name, reason)` | `secretName`, `reason`, `stage` |
| `ProviderUnavailable(providerName, ex)` | `providerName`, `stage` |
| `VersionNotFound(name, version)` | `secretName`, `version`, `stage` |
| `OperationFailed(operation, reason, ex)` | `operation`, `reason`, `stage` |

The `stage` field is always `"secrets"` for all error types, which helps distinguish secrets errors from other Encina error categories in centralized error handlers.

## Best Practices

### 1. Always Handle Both Tracks

Never assume a secret call will succeed. Use `Match()` to handle both `Right` and `Left`:

```csharp
// Good: explicit handling of both cases
result.Match(
    Right: secret => UseSecret(secret),
    Left: error => HandleError(error)
);

// Avoid: accessing .Value without checking (throws on Left)
```

### 2. Use Error Codes for Branching, Not Message Strings

Error messages are human-readable and may change between versions. Error codes are stable:

```csharp
// Good: branch on code
var code = error.GetCode().IfNone("unknown");
if (code == SecretsErrorCodes.NotFoundCode) { ... }

// Avoid: branch on message text
if (error.Message.Contains("not found")) { ... }
```

### 3. Differentiate Retriable vs. Terminal Errors

| Error Code | Retriable? | Action |
|------------|-----------|--------|
| `not_found` | No | Use fallback or fail |
| `access_denied` | No | Fix permissions, then retry |
| `invalid_name` | No | Fix the code (bug) |
| `provider_unavailable` | Yes | Retry with backoff |
| `version_not_found` | No | Use latest version |
| `operation_failed` | Maybe | Inspect details, then decide |

### 4. Log Errors with Structure

```csharp
result.IfLeft(error =>
{
    var code = error.GetCode().IfNone("unknown");
    var details = error.GetDetails();

    logger.LogWarning(
        "Secret operation failed. Code={Code}, Message={Message}, Details={@Details}",
        code, error.Message, details);
});
```

### 5. Use Map for Simple Transformations, Bind for Dependent Operations

```csharp
// Map: transform the value if present
var value = result.Map(s => s.Value);

// Bind: chain another operation that can also fail
var chained = result.Bind(s => ValidateSecret(s));
```

## Full Example: Resilient Secret Loading

```csharp
public class SecretLoader
{
    private readonly ISecretProvider _provider;
    private readonly ILogger<SecretLoader> _logger;

    public SecretLoader(ISecretProvider provider, ILogger<SecretLoader> logger)
    {
        _provider = provider;
        _logger = logger;
    }

    public async Task<string> GetConnectionStringAsync(CancellationToken ct)
    {
        var result = await _provider.GetSecretAsync("db-connection-string", ct);

        return result.Match(
            Right: secret =>
            {
                _logger.LogInformation(
                    "Loaded connection string v{Version}", secret.Version);
                return secret.Value;
            },
            Left: error =>
            {
                var code = error.GetCode().IfNone("unknown");

                _logger.LogWarning(
                    "Failed to load connection string [{Code}]: {Message}",
                    code, error.Message);

                // Fall back to configuration for non-critical environments
                return code switch
                {
                    SecretsErrorCodes.NotFoundCode =>
                        throw new InvalidOperationException(
                            "Connection string secret not found. Ensure it exists in the vault."),
                    SecretsErrorCodes.ProviderUnavailableCode =>
                        throw new InvalidOperationException(
                            $"Secret vault is unavailable: {error.Message}"),
                    _ => throw new InvalidOperationException(
                            $"Unexpected error loading secret: {error.Message}")
                };
            }
        );
    }
}
```

## Related

- [Basic Setup](secrets-basic-setup.md)
- [IConfiguration Integration](secrets-configuration-integration.md)
- [Caching Strategy](secrets-caching-strategy.md)
