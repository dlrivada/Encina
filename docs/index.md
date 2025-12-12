---
_layout: landing
---

# SimpleMediator

**Railway Oriented Mediator for .NET 10**

SimpleMediator is a lightweight, functional mediator abstraction for .NET applications that embraces Railway Oriented Programming (ROP) principles. Built on top of [LanguageExt](https://github.com/louthy/language-ext), it provides explicit request/response contracts, composable pipeline behaviors, and rich observability features for building maintainable CQRS-style applications.

[![.NET Quality Gate](https://github.com/dlrivada/SimpleMediator/actions/workflows/dotnet-ci.yml/badge.svg)](https://github.com/dlrivada/SimpleMediator/actions/workflows/dotnet-ci.yml)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=dlrivada_SimpleMediator&metric=coverage)](https://sonarcloud.io/summary/new_code?id=dlrivada_SimpleMediator)
![Mutation](https://img.shields.io/badge/mutation-93.74%25-4C934C.svg)

## Key Features

- **Functional Error Handling**: All operations return `Either<MediatorError, TValue>` for explicit, type-safe error handling
- **Zero Exceptions Policy**: Operational failures travel through functional rails instead of exceptions
- **Pipeline Composition**: Ordered behaviors, pre-processors, and post-processors for cross-cutting concerns
- **Rich Observability**: Built-in OpenTelemetry support with activities, metrics, and structured logging
- **CQRS Contracts**: Explicit `ICommand<T>`, `IQuery<T>`, and `INotification` interfaces
- **Assembly Scanning**: Automatic discovery and registration of handlers, behaviors, and processors
- **Functional Failure Detection**: Translate domain envelopes into consistent mediator errors

## Getting Started

### Installation

```bash
dotnet add package SimpleMediator
```

### Basic Configuration

```csharp
using SimpleMediator;

services.AddSimpleMediator(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<ApplicationMarker>()
       .AddPipelineBehavior(typeof(CommandActivityPipelineBehavior<,>))
       .AddPipelineBehavior(typeof(QueryMetricsPipelineBehavior<,>));
});
```

### Send a Command

```csharp
public sealed record RegisterUser(string Email, string Password) : ICommand<Unit>;

var result = await mediator.Send(new RegisterUser("user@example.com", "Pass@123"), ct);

result.Match(
    Left: error => logger.LogWarning("Registration failed: {Code}", error.GetMediatorCode()),
    Right: _ => logger.LogInformation("User registered successfully"));
```

## Documentation

- [API Reference](api/SimpleMediator.html) - Complete API documentation
- [Getting Started](docs/getting-started.html) - Quick start guide
- [Introduction](docs/introduction.html) - Core concepts and architecture

## Quality Metrics

| Metric | Current | Target | Status |
|--------|---------|--------|--------|
| Line Coverage | 92.5% | ≥90% | ✅ Exceeded |
| Branch Coverage | 83.3% | ≥85% | 🟡 Near |
| Mutation Score | 93.74% | ≥95% | 🟡 Near |
| Build Warnings | 0 | 0 | ✅ Perfect |
| XML Documentation | 100% | 100% | ✅ Perfect |
| Tests Passing | 229/229 | 100% | ✅ Perfect |

## Architecture Highlights

- **Railway Oriented Programming**: Explicit success/failure paths through `Either<L, R>`
- **Pipeline Pattern**: Composable behaviors for validation, logging, metrics, and more
- **Dependency Injection**: First-class support for Microsoft.Extensions.DependencyInjection
- **OpenTelemetry Ready**: Built-in ActivitySource and Metrics support
- **Immutable Messages**: Commands, queries, and notifications are record types

## Resources

- [GitHub Repository](https://github.com/dlrivada/SimpleMediator)
- [Quality & Security Roadmap](https://github.com/dlrivada/SimpleMediator/blob/main/QUALITY_SECURITY_ROADMAP.md)
- [Contributing Guide](https://github.com/dlrivada/SimpleMediator/blob/main/CONTRIBUTING.md)

---

Built with ❤️ for the .NET community
