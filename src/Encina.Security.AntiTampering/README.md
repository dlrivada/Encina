# Encina.Security.AntiTampering

[![NuGet](https://img.shields.io/nuget/v/Encina.Security.AntiTampering.svg)](https://www.nuget.org/packages/Encina.Security.AntiTampering)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)

**HMAC-based request signing, integrity verification, and replay attack protection for Encina CQRS pipelines.**

Encina.Security.AntiTampering provides automatic HMAC signature validation, timestamp tolerance, and nonce-based replay protection at the CQRS pipeline level. Decorate commands with `[RequireSignature]` and let the pipeline handle cryptographic verification transparently — no security code in your business logic.

## Key Features

- **HMAC-SHA256/384/512** with configurable algorithm selection
- **Attribute-based** — `[RequireSignature]` on commands/queries for declarative validation
- **Replay attack protection** — nonce-based deduplication with configurable expiry
- **Timestamp tolerance** — configurable time window to reject stale requests
- **Key rotation** — support for multiple concurrent keys via key ID
- **Client-side signing** — `IRequestSigningClient` for signing outgoing HTTP requests
- **Railway Oriented Programming** — all operations return `Either<EncinaError, T>`
- **Zero overhead** for requests without `[RequireSignature]` attribute
- **OpenTelemetry** tracing and metrics (opt-in)
- **Health check** with roundtrip nonce probe (opt-in)
- **Distributed nonce store** — pluggable via `ICacheProvider` for multi-instance deployments

## Quick Start

```csharp
// 1. Register services
services.AddEncinaAntiTampering(options =>
{
    options.Algorithm = HMACAlgorithm.SHA256;
    options.TimestampToleranceMinutes = 5;
    options.RequireNonce = true;
    options.AddKey("api-key-v1", "your-secret-value");
});

// 2. Decorate commands requiring signature verification
[RequireSignature]
public sealed record ProcessPaymentCommand(decimal Amount, string Currency) : ICommand<PaymentResult>;

// 3. Handler receives verified data — zero security awareness needed
public class ProcessPaymentHandler : ICommandHandler<ProcessPaymentCommand, PaymentResult>
{
    public async ValueTask<Either<EncinaError, PaymentResult>> Handle(
        ProcessPaymentCommand command, IRequestContext context, CancellationToken ct)
    {
        // Request is guaranteed: signed, not tampered, not replayed, recent
        await _paymentGateway.ChargeAsync(command.Amount, command.Currency);
        return Right(new PaymentResult(/* ... */));
    }
}
```

## Client-Side Signing

Sign outgoing HTTP requests with all required headers:

```csharp
var client = serviceProvider.GetRequiredService<IRequestSigningClient>();

var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
{
    Content = JsonContent.Create(new { ProductId = "SKU-001", Quantity = 3 })
};

var result = await client.SignRequestAsync(request, "api-key-v1");
// Headers X-Signature, X-Timestamp, X-Nonce, X-Key-Id are now set
```

## Distributed Nonce Store

For multi-instance deployments, replace the in-memory store with a distributed cache:

```csharp
services.AddEncinaRedisCache(options => { /* ... */ });
services.AddEncinaAntiTampering(options => { /* ... */ });
services.AddDistributedNonceStore();
```

## Custom Key Provider

Register your cloud KMS provider before `AddEncinaAntiTampering()`:

```csharp
services.AddSingleton<IKeyProvider, AzureKeyVaultKeyProvider>();
services.AddEncinaAntiTampering(options =>
{
    options.AddHealthCheck = true;
    options.EnableTracing = true;
    options.EnableMetrics = true;
});
```

## Signature Format

```
HMAC-SHA256(SecretKey, "Method|Path|PayloadHash|Timestamp|Nonce")
```

## HTTP Headers

| Header | Default | Purpose |
|--------|---------|---------|
| `X-Signature` | Signature | Base64-encoded HMAC |
| `X-Timestamp` | Timestamp | ISO 8601 UTC |
| `X-Nonce` | Nonce | Unique request ID |
| `X-Key-Id` | Key ID | Signing key identifier |

## Error Codes

| Code | Description |
|------|-------------|
| `antitampering.key_not_found` | Key ID not registered |
| `antitampering.signature_invalid` | HMAC mismatch |
| `antitampering.signature_missing` | Signature header missing |
| `antitampering.timestamp_expired` | Request too old |
| `antitampering.nonce_reused` | Replay attack detected |
| `antitampering.nonce_missing` | Nonce header missing |

## Documentation

- [Anti-Tampering Guide](../../docs/features/anti-tampering.md) — comprehensive documentation with examples
- [CHANGELOG](../../CHANGELOG.md) — version history and release notes
- [Architecture Decision Records](../../docs/architecture/adr/) — design decisions

## Dependencies

- `Encina` (core abstractions)
- `Encina.Caching` (for distributed nonce store)
- `Microsoft.AspNetCore.Http.Abstractions`
- `Microsoft.Extensions.DependencyInjection.Abstractions`
- `Microsoft.Extensions.Logging.Abstractions`
- `Microsoft.Extensions.Diagnostics.HealthChecks`
- `Microsoft.Extensions.Options`

## License

MIT License. See [LICENSE](../../LICENSE) for details.
