# Anti-Tampering in Encina

Encina.Security.AntiTampering provides HMAC-based request signing, integrity verification, and replay attack protection at the CQRS pipeline level.

## Table of Contents

1. [Overview](#overview)
2. [The Problem](#the-problem)
3. [The Solution](#the-solution)
4. [Quick Start](#quick-start)
5. [Configuration Reference](#configuration-reference)
6. [Client-Side Signing](#client-side-signing)
7. [Server-Side Validation](#server-side-validation)
8. [Nonce Stores](#nonce-stores)
9. [Key Management](#key-management)
10. [Pipeline Order](#pipeline-order)
11. [Observability](#observability)
12. [Health Check](#health-check)
13. [Error Handling](#error-handling)
14. [Testing](#testing)
15. [Troubleshooting](#troubleshooting)

---

## Overview

Encina.Security.AntiTampering protects API endpoints against three categories of attacks:

| Attack | Protection | Mechanism |
|--------|------------|-----------|
| **Tampering** | HMAC signature | Cryptographic hash of payload + context |
| **Replay** | Nonce deduplication | One-time-use request identifiers |
| **Stale requests** | Timestamp tolerance | Configurable time window validation |

**Key characteristics**:

- **HMAC-SHA256/384/512** with configurable algorithm selection
- **Railway Oriented Programming**: All operations return `Either<EncinaError, T>`
- **Zero overhead** for requests without `[RequireSignature]` attribute
- **Canonical signature format**: `HMAC(SecretKey, "Method|Path|PayloadHash|Timestamp|Nonce")`
- **Thread-safe**: All implementations are concurrent-safe

| Component | Description |
|-----------|-------------|
| **`IRequestSigner`** | Signs and verifies payloads using HMAC |
| **`INonceStore`** | Nonce deduplication for replay protection |
| **`IKeyProvider`** | Key management abstraction (pluggable) |
| **`IRequestSigningClient`** | Signs outgoing HTTP requests with all headers |
| **`HMACValidationPipelineBehavior`** | Automatic validation in the CQRS pipeline |
| **`AntiTamperingHealthCheck`** | Roundtrip verification of the signing pipeline |

---

## The Problem

API requests can be intercepted and modified in transit. Without integrity verification, an attacker can:

```csharp
// Without Encina — no request integrity verification
public class ProcessPaymentHandler : ICommandHandler<ProcessPaymentCommand, PaymentResult>
{
    public async ValueTask<Either<EncinaError, PaymentResult>> Handle(
        ProcessPaymentCommand command, IRequestContext context, CancellationToken ct)
    {
        // How do we know this request wasn't tampered with?
        // How do we know it's not a replay of a previous request?
        // How do we know the caller is who they claim to be?
        await _paymentGateway.ChargeAsync(command.Amount, command.Currency);
        // ...
    }
}
```

Developers must implement signature validation, timestamp checks, and nonce tracking in every handler — violating Single Responsibility.

---

## The Solution

With Encina, declare signature requirements on commands and let the pipeline handle validation:

```csharp
// With Encina — declarative integrity verification
[RequireSignature]
public sealed record ProcessPaymentCommand(
    decimal Amount,
    string Currency
) : ICommand<PaymentResult>;

// Handler has zero security awareness
public class ProcessPaymentHandler : ICommandHandler<ProcessPaymentCommand, PaymentResult>
{
    public async ValueTask<Either<EncinaError, PaymentResult>> Handle(
        ProcessPaymentCommand command, IRequestContext context, CancellationToken ct)
    {
        // Request is guaranteed to be:
        // 1. Signed by a valid key holder
        // 2. Not tampered with (HMAC verified)
        // 3. Not a replay (nonce checked)
        // 4. Recent (timestamp validated)
        await _paymentGateway.ChargeAsync(command.Amount, command.Currency);
        // ...
    }
}
```

---

## Quick Start

### 1. Register Services

```csharp
services.AddEncinaAntiTampering(options =>
{
    options.Algorithm = HMACAlgorithm.SHA256;
    options.TimestampToleranceMinutes = 5;
    options.RequireNonce = true;
    options.AddKey("api-key-v1", "your-secret-value-at-least-32-chars!");
});
```

### 2. Decorate Commands

```csharp
[RequireSignature]
public sealed record CreateOrderCommand(string ProductId, int Quantity) : ICommand<OrderId>;
```

### 3. Sign Outgoing Requests (Client Side)

```csharp
var client = serviceProvider.GetRequiredService<IRequestSigningClient>();

var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
{
    Content = JsonContent.Create(new { ProductId = "SKU-001", Quantity = 3 })
};

var result = await client.SignRequestAsync(request, "api-key-v1");
// Headers X-Signature, X-Timestamp, X-Nonce, X-Key-Id are now set
```

---

## Configuration Reference

All options are configured via `AntiTamperingOptions`:

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Algorithm` | `HMACAlgorithm` | `SHA256` | HMAC algorithm (SHA256, SHA384, SHA512) |
| `TimestampToleranceMinutes` | `int` | `5` | Maximum age of a request in minutes |
| `RequireNonce` | `bool` | `true` | Enable nonce-based replay protection |
| `NonceExpiryMinutes` | `int` | `10` | How long nonces are retained |
| `SignatureHeader` | `string` | `"X-Signature"` | HTTP header for the HMAC signature |
| `TimestampHeader` | `string` | `"X-Timestamp"` | HTTP header for the timestamp |
| `NonceHeader` | `string` | `"X-Nonce"` | HTTP header for the nonce |
| `KeyIdHeader` | `string` | `"X-Key-Id"` | HTTP header for the key identifier |
| `AddHealthCheck` | `bool` | `false` | Register health check |
| `EnableTracing` | `bool` | `true` | Enable OpenTelemetry tracing |
| `EnableMetrics` | `bool` | `true` | Enable OpenTelemetry metrics |

```csharp
services.AddEncinaAntiTampering(options =>
{
    options.Algorithm = HMACAlgorithm.SHA512;
    options.TimestampToleranceMinutes = 2;  // Strict 2-minute window
    options.NonceExpiryMinutes = 15;
    options.AddHealthCheck = true;
    options.EnableTracing = true;
    options.EnableMetrics = true;

    // Register test keys (development/testing only)
    options.AddKey("api-key-v1", "your-secret-value");
    options.AddKey("api-key-v2", "your-rotated-secret-value");
});
```

---

## Client-Side Signing

### Using IRequestSigningClient

The `IRequestSigningClient` signs outgoing HTTP requests by computing the HMAC signature and attaching all required headers:

```csharp
public class OrderApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IRequestSigningClient _signingClient;

    public OrderApiClient(HttpClient httpClient, IRequestSigningClient signingClient)
    {
        _httpClient = httpClient;
        _signingClient = signingClient;
    }

    public async Task<OrderResult> CreateOrderAsync(CreateOrderDto order, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
        {
            Content = JsonContent.Create(order)
        };

        var signResult = await _signingClient.SignRequestAsync(request, "api-key-v1", ct);

        return await signResult.MatchAsync(
            Right: async signedRequest =>
            {
                var response = await _httpClient.SendAsync(signedRequest, ct);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<OrderResult>(ct);
            },
            Left: error => throw new InvalidOperationException($"Signing failed: {error.Message}")
        );
    }
}
```

### Signature Format

The canonical string used for HMAC computation:

```
Method|Path|PayloadHash|Timestamp|Nonce
```

Example:

```
POST|/api/orders|a1b2c3d4e5f6...(SHA-256 hex)|2026-02-21T10:30:00.0000000+00:00|abc123def456
```

### HTTP Headers Attached

| Header | Example Value | Description |
|--------|---------------|-------------|
| `X-Signature` | `dGhpcyBpcyBhIGJhc2U2NCBzaWdu...` | Base64-encoded HMAC |
| `X-Timestamp` | `2026-02-21T10:30:00.0000000+00:00` | ISO 8601 UTC timestamp |
| `X-Nonce` | `a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4` | 32-char hex GUID |
| `X-Key-Id` | `api-key-v1` | Signing key identifier |

---

## Server-Side Validation

### Pipeline Behavior

The `HMACValidationPipelineBehavior` automatically validates incoming requests decorated with `[RequireSignature]`:

```
Incoming HTTP Request
        │
        ▼
┌──────────────────────────────────┐
│  HMACValidationPipelineBehavior  │
│                                  │
│  1. Check [RequireSignature]     │
│  2. Extract HTTP headers         │
│  3. Validate timestamp tolerance │
│  4. Validate nonce uniqueness    │
│  5. Verify HMAC signature        │
│                                  │
│  ✅ Pass → next pipeline step    │
│  ❌ Fail → return EncinaError    │
└──────────────────────────────────┘
        │
        ▼
   Command Handler
```

### RequireSignature Attribute Options

```csharp
// Basic — validate signature with any registered key
[RequireSignature]
public sealed record CreateOrderCommand(...) : ICommand<OrderId>;

// Restrict to specific key
[RequireSignature(KeyId = "partner-api-key")]
public sealed record WebhookCommand(...) : ICommand;

// Skip nonce check (for idempotent operations)
[RequireSignature(SkipReplayProtection = true)]
public sealed record IdempotentUpdateCommand(...) : ICommand;
```

### No HttpContext Passthrough

When there is no `HttpContext` (e.g., background jobs, message handlers), the pipeline behavior passes through without validation, allowing the same commands to work in both HTTP and non-HTTP contexts.

---

## Nonce Stores

### InMemoryNonceStore (Default)

Suitable for single-instance deployments:

- Thread-safe `ConcurrentDictionary` with TTL expiration
- Background cleanup timer every 5 minutes
- Implements `IDisposable` for cleanup timer disposal

### DistributedCacheNonceStore

For multi-instance deployments, replace with distributed cache:

```csharp
// Register a cache provider first (e.g., Redis)
services.AddEncinaRedisCache(options => { ... });

// Then replace the nonce store
services.AddEncinaAntiTampering(options => { ... });
services.AddDistributedNonceStore();
```

The distributed store uses `ICacheProvider` from `Encina.Caching` with key prefix `"encina:nonce:"` and automatic TTL based on `NonceExpiryMinutes`.

---

## Key Management

### Development/Testing Keys

Use `AntiTamperingOptions.AddKey()` for development and testing:

```csharp
services.AddEncinaAntiTampering(options =>
{
    options.AddKey("api-key-v1", "my-secret-value-for-testing");
    options.AddKey("api-key-v2", "another-secret-for-rotation");
});
```

### Production Key Provider

Register a custom `IKeyProvider` before calling `AddEncinaAntiTampering()`:

```csharp
// Custom key provider backed by Azure Key Vault, AWS KMS, etc.
services.AddSingleton<IKeyProvider, AzureKeyVaultKeyProvider>();
services.AddEncinaAntiTampering(options =>
{
    options.Algorithm = HMACAlgorithm.SHA256;
    options.AddHealthCheck = true;
});
```

The `TryAdd` semantics ensure your custom provider is not overridden by the default `InMemoryKeyProvider`.

### Key Rotation

Support concurrent keys by including `KeyId` in requests:

1. Add new key to the provider
2. Start signing new requests with the new key ID
3. Both old and new keys validate during transition
4. Remove old key after transition period

---

## Pipeline Order

When combined with other Encina pipeline behaviors, the recommended order is:

```
1. SecurityPipelineBehavior      (authentication/authorization)
2. GDPRCompliancePipelineBehavior (GDPR validation)
3. HMACValidationPipelineBehavior (signature verification) ← THIS
4. ValidationPipelineBehavior    (input validation)
5. InputSanitizationPipelineBehavior (sanitization)
6. EncryptionPipelineBehavior    (field encryption)
7. AuditPipelineBehavior         (audit logging)
8. TransactionPipelineBehavior   (database transaction)
```

---

## Observability

### OpenTelemetry Tracing

Activities are created under the `Encina.Security.AntiTampering` ActivitySource:

| Tag | Description |
|-----|-------------|
| `antitampering.request_type` | Full type name of the request |
| `antitampering.key_id` | Key identifier used for signing/verification |
| `antitampering.algorithm` | HMAC algorithm (SHA256/SHA384/SHA512) |
| `antitampering.outcome` | `success`, `failed`, `skipped` |

### Metrics

4 instruments under the `Encina.Security.AntiTampering` Meter:

| Instrument | Type | Description |
|------------|------|-------------|
| `antitampering.sign.total` | Counter | Total signing operations |
| `antitampering.verify.total` | Counter | Total verification operations |
| `antitampering.verify.failures` | Counter | Failed verifications |
| `antitampering.operation.duration` | Histogram | Operation duration in ms |

### Structured Logging

Zero-allocation logging via `LoggerMessage` source generation with structured event IDs for sign, verify, failure, and skip operations.

---

## Health Check

Enable the health check to verify the signing pipeline is functional:

```csharp
services.AddEncinaAntiTampering(options =>
{
    options.AddHealthCheck = true;
    options.AddKey("health-key", "health-check-secret");
});
```

The health check verifies:

1. `IKeyProvider` is resolvable from DI
2. `IRequestSigner` is resolvable from DI
3. `INonceStore` is resolvable from DI
4. Roundtrip nonce write/read probe succeeds

Response data includes service availability status for each component.

---

## Error Handling

All errors follow the Railway Oriented Programming pattern with `Either<EncinaError, T>`:

| Error Code | Cause | Resolution |
|------------|-------|------------|
| `antitampering.key_not_found` | Key ID not registered in provider | Register the key or check key ID spelling |
| `antitampering.signature_invalid` | HMAC mismatch (tampered payload or wrong key) | Verify payload hasn't been modified and key matches |
| `antitampering.signature_missing` | X-Signature header not present | Ensure client sends the signature header |
| `antitampering.timestamp_expired` | Request age exceeds tolerance | Check clock synchronization or increase tolerance |
| `antitampering.nonce_reused` | Duplicate nonce detected (replay attack) | Generate a new unique nonce per request |
| `antitampering.nonce_missing` | X-Nonce header not present when required | Ensure client sends the nonce header |

---

## Testing

### Unit Testing with Mocks

```csharp
var signer = Substitute.For<IRequestSigner>();
signer.SignAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<SigningContext>(), Arg.Any<CancellationToken>())
    .Returns(ValueTask.FromResult<Either<EncinaError, string>>(Right("mock-signature")));
```

### Integration Testing

```csharp
var services = new ServiceCollection();
services.AddLogging();
services.AddSingleton(TimeProvider.System);
services.AddEncinaAntiTampering(options =>
{
    options.AddKey("test-key", "test-secret-value-32-chars-min!");
});
var provider = services.BuildServiceProvider();

var signer = provider.GetRequiredService<IRequestSigner>();
// Sign and verify in test...
```

---

## Troubleshooting

### Common Issues

**"Unable to resolve service for type 'System.TimeProvider'"**

The `RequestSigningClient` depends on `TimeProvider`. In ASP.NET Core apps, this is automatically registered. For standalone DI (tests, console apps), register it manually:

```csharp
services.AddSingleton(TimeProvider.System);
```

**Signature verification fails after payload modification**

The canonical string includes the SHA-256 hash of the full payload. Any modification (even whitespace changes in JSON) invalidates the signature. Ensure the exact same bytes are used for signing and verification.

**Nonce rejected on retry**

Each request must have a unique nonce. If you retry a failed request, generate a new nonce. For idempotent operations where retries are expected, use `[RequireSignature(SkipReplayProtection = true)]`.

**Clock skew between client and server**

Increase `TimestampToleranceMinutes` or ensure NTP synchronization between client and server machines. The default 5-minute tolerance handles most network delays.
