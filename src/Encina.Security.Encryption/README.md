# Encina.Security.Encryption

[![NuGet](https://img.shields.io/nuget/v/Encina.Security.Encryption.svg)](https://www.nuget.org/packages/Encina.Security.Encryption)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)

**Attribute-based, field-level encryption for Encina CQRS pipelines using AES-256-GCM.**

Encina.Security.Encryption provides automatic encryption and decryption of sensitive properties (emails, SSNs, card numbers) at the CQRS pipeline level. Decorate properties with `[Encrypt]` and let the pipeline handle key management, encryption, and decryption transparently — no cryptographic code in your business logic.

## Key Features

- **AES-256-GCM** (NIST SP 800-38D) with per-operation random nonces
- **Attribute-based** — `[Encrypt]`, `[EncryptedResponse]`, `[DecryptOnReceive]`
- **Key rotation** — embedded key version in ciphertext enables seamless rotation
- **Multi-tenant isolation** — per-tenant key derivation via `IRequestContext.TenantId`
- **Railway Oriented Programming** — all operations return `Either<EncinaError, T>`
- **Zero overhead** for requests without encryption attributes
- **OpenTelemetry** tracing and metrics (opt-in)
- **Health check** with roundtrip verification (opt-in)
- **Pluggable key providers** — Azure Key Vault, AWS KMS, HashiCorp Vault

## Quick Start

```csharp
// 1. Register services
services.AddLogging();
services.AddEncinaEncryption();

// 2. Decorate properties
public sealed record CreateUserCommand(
    [property: Encrypt(Purpose = "User.Email")] string Email,
    [property: Encrypt(Purpose = "User.SSN")] string SSN,
    string DisplayName  // Not encrypted
) : ICommand<UserId>;

// 3. Handler receives encrypted data — zero crypto awareness needed
public class CreateUserHandler : ICommandHandler<CreateUserCommand, UserId>
{
    public async ValueTask<Either<EncinaError, UserId>> Handle(
        CreateUserCommand command, IRequestContext context, CancellationToken ct)
    {
        // command.Email = "ENC:v1:0:key-v1:..." (already encrypted)
        var user = new User(command.Email, command.SSN, command.DisplayName);
        await _repository.AddAsync(user, ct);
        return Right(user.Id);
    }
}
```

## Production Key Provider

Register your cloud KMS provider before `AddEncinaEncryption()`:

```csharp
services.AddSingleton<IKeyProvider, AzureKeyVaultKeyProvider>();
services.AddEncinaEncryption(options =>
{
    options.AddHealthCheck = true;
    options.EnableTracing = true;
    options.EnableMetrics = true;
});
```

## Documentation

- [Field-Level Encryption Guide](../../docs/features/field-level-encryption.md) — comprehensive documentation with examples
- [CHANGELOG](../../CHANGELOG.md) — version history and release notes
- [Architecture Decision Records](../../docs/architecture/adr/) — design decisions

## Dependencies

- `Encina` (core abstractions)
- `Microsoft.Extensions.DependencyInjection.Abstractions`
- `Microsoft.Extensions.Logging.Abstractions`
- `Microsoft.Extensions.Diagnostics.HealthChecks`
- `Microsoft.Extensions.Options`

## License

MIT License. See [LICENSE](../../LICENSE) for details.
