# Encina.Security.PII

[![NuGet](https://img.shields.io/nuget/v/Encina.Security.PII.svg)](https://www.nuget.org/packages/Encina.Security.PII)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)

**Attribute-based PII masking and data protection for Encina CQRS pipelines.**

Encina.Security.PII provides automatic masking of personally identifiable information (emails, phone numbers, SSNs, credit cards) at the CQRS pipeline level. Decorate properties with `[PII]` and let the pipeline handle masking in responses, logs, and audit trails — no manual redaction in your business logic.

## Key Features

- **9 built-in strategies** — Email, Phone, CreditCard, SSN, Name, Address, DateOfBirth, IPAddress, Custom
- **5 masking modes** — Partial, Full, Hash, Tokenize, Redact
- **Attribute-based** — `[PII]`, `[SensitiveData]`, `[MaskInLogs]`
- **Audit trail integration** — implements `IPiiMasker` from `Encina.Security.Audit`
- **Railway Oriented Programming** — consistent `Either<EncinaError, T>` error handling
- **Zero overhead** for requests without PII attributes
- **OpenTelemetry** tracing and metrics (opt-in)
- **Health check** with masking probe verification (opt-in)
- **GDPR/HIPAA/PCI-DSS** compliance support

## Quick Start

```csharp
// 1. Register services
services.AddEncinaPII(options =>
{
    options.MaskInResponses = true;
    options.MaskInLogs = true;
    options.MaskInAuditTrails = true;
    options.DefaultMode = MaskingMode.Partial;
});

// 2. Decorate properties
public sealed record CreateUserCommand(
    [property: PII(PIIType.Email)] string Email,
    [property: PII(PIIType.Phone)] string PhoneNumber,
    [property: PII(PIIType.Name)] string FullName,
    [property: SensitiveData(MaskingMode.Redact)] string Password
) : ICommand<UserId>;

// 3. Responses are masked automatically
// Email: "u***@example.com", Phone: "***-***-4567", Name: "J*** D**"
// Password: "[REDACTED]"
```

## Custom Masking Strategies

Register a custom strategy for any `PIIType`:

```csharp
public class MyEmailMasker : IMaskingStrategy
{
    public string Apply(string value, MaskingOptions options)
        => value.Split('@') is [var local, var domain]
            ? $"{local[0]}***@{domain}"
            : new string(options.MaskCharacter, value.Length);
}

services.AddEncinaPII(options =>
{
    options.AddStrategy<MyEmailMasker>(PIIType.Email);
});
```

## Documentation

- [PII Masking Guide](../../docs/features/pii-masking.md) — comprehensive documentation with examples
- [CHANGELOG](../../CHANGELOG.md) — version history and release notes
- [Architecture Decision Records](../../docs/architecture/adr/) — design decisions

## Dependencies

- `Encina` (core abstractions)
- `Encina.Security.Audit` (audit trail integration)
- `Microsoft.Extensions.DependencyInjection.Abstractions`
- `Microsoft.Extensions.Logging.Abstractions`
- `Microsoft.Extensions.Diagnostics.HealthChecks`
- `Microsoft.Extensions.Options`

## License

MIT License. See [LICENSE](../../LICENSE) for details.
