# Encina

<!-- CI/CD Status -->
[![.NET CI](https://github.com/dlrivada/Encina/actions/workflows/dotnet-ci.yml/badge.svg)](https://github.com/dlrivada/Encina/actions/workflows/dotnet-ci.yml)
[![SonarCloud Analysis](https://github.com/dlrivada/Encina/actions/workflows/sonarcloud.yml/badge.svg)](https://github.com/dlrivada/Encina/actions/workflows/sonarcloud.yml)
[![CodeQL](https://github.com/dlrivada/Encina/actions/workflows/codeql.yml/badge.svg)](https://github.com/dlrivada/Encina/actions/workflows/codeql.yml)
[![SBOM](https://github.com/dlrivada/Encina/actions/workflows/sbom.yml/badge.svg)](https://github.com/dlrivada/Encina/actions/workflows/sbom.yml)
[![Benchmarks](https://github.com/dlrivada/Encina/actions/workflows/benchmarks.yml/badge.svg)](https://github.com/dlrivada/Encina/actions/workflows/benchmarks.yml)

<!-- Project Info -->
![.NET 10.0](https://img.shields.io/badge/.NET-10.0-512BD4.svg)
![Status](https://img.shields.io/badge/status-pre--1.0-blue.svg)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Dependabot](https://img.shields.io/badge/Dependabot-Enabled-025E8C?logo=dependabot&logoColor=white)](https://docs.github.com/code-security/dependabot)
[![Conventional Commits](https://img.shields.io/badge/Conventional%20Commits-1.0.0-FE5196)](https://www.conventionalcommits.org/)

<!-- Code Quality (SonarCloud) -->
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=dlrivada_Encina&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=dlrivada_Encina)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=dlrivada_Encina&metric=coverage)](https://sonarcloud.io/summary/new_code?id=dlrivada_Encina)
[![Bugs](https://sonarcloud.io/api/project_badges/measure?project=dlrivada_Encina&metric=bugs)](https://sonarcloud.io/summary/new_code?id=dlrivada_Encina)
[![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=dlrivada_Encina&metric=code_smells)](https://sonarcloud.io/summary/new_code?id=dlrivada_Encina)
[![Technical Debt](https://sonarcloud.io/api/project_badges/measure?project=dlrivada_Encina&metric=sqale_index)](https://sonarcloud.io/summary/new_code?id=dlrivada_Encina)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=dlrivada_Encina&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=dlrivada_Encina)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=dlrivada_Encina&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=dlrivada_Encina)
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=dlrivada_Encina&metric=reliability_rating)](https://sonarcloud.io/summary/new_code?id=dlrivada_Encina)
[![Mutation Score](https://img.shields.io/badge/mutation-79.75%25-4C934C.svg)](https://img.shields.io/badge/mutation-79.75%25-4C934C.svg)
<!-- AI -->
[![CodeRabbit Pull Request Reviews](https://img.shields.io/coderabbit/prs/github/dlrivada/Encina?logo=coderabbit)](https://img.shields.io/coderabbit/prs/github/dlrivada/Encina?logo=coderabbit) ![Claude](https://img.shields.io/badge/Claude-D97757?style=for-the-badge&logo=claude&logoColor=white) ![Dependabot](https://img.shields.io/badge/dependabot-025E8C?style=for-the-badge&logo=dependabot&logoColor=white) ![GitHub Copilot](https://img.shields.io/badge/github_copilot-8957E5?style=for-the-badge&logo=github-copilot&logoColor=white)

**Build resilient .NET applications with Railway Oriented Programming.**

Encina is a comprehensive toolkit for building robust .NET applications. Built on [LanguageExt](https://github.com/louthy/language-ext), it provides explicit error handling through `Either<EncinaError, T>`, CQRS patterns, messaging infrastructure, and composable pipeline behaviors.

## Key Features

- **Railway Oriented Programming**: All operations return `Either<EncinaError, T>` - no exceptions for business logic
- **CQRS Support**: Explicit `ICommand<T>` and `IQuery<T>` contracts with dedicated handlers
- **Pipeline Behaviors**: Composable middleware for validation, caching, transactions, and more
- **Audit Trail**: Automatic tracking of who created/modified entities and when (see [docs](docs/features/audit-tracking.md))
- **Streaming**: `IAsyncEnumerable` support via `IStreamRequest<TItem>`
- **Observability**: Built-in OpenTelemetry integration with ActivitySource and Metrics
- **Pay-for-What-You-Use**: All features are opt-in via satellite packages

## Architecture

```mermaid
flowchart TB
    subgraph Client
        A[Application Code]
    end

    subgraph Encina["Encina Core"]
        B[IEncina]
        C[Pipeline Behaviors]
        D[Request Handler]
    end

    subgraph Satellites["Satellite Packages"]
        E[Validation]
        F[Caching]
        G[Transactions]
        H[Observability]
    end

    A -->|Send/Publish| B
    B --> C
    C --> E & F & G & H
    C --> D
    D -->|Either of Error,T| B
    B -->|Either of Error,T| A
```

## Quick Start

### 1. Install the Package

```bash
dotnet add package Encina
```

### 2. Configure Services

```csharp
using Encina;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEncina(typeof(Program).Assembly);
```

### 3. Define a Command

```csharp
public sealed record CreateOrder(Guid CustomerId, List<OrderItem> Items) : ICommand<OrderId>;

public sealed class CreateOrderHandler : ICommandHandler<CreateOrder, OrderId>
{
    public async Task<OrderId> Handle(CreateOrder command, CancellationToken ct)
    {
        var order = Order.Create(command.CustomerId, command.Items);
        await _repository.SaveAsync(order, ct);
        return order.Id;
    }
}
```

### 4. Send the Command

```csharp
var result = await encina.Send(new CreateOrder(customerId, items), ct);

result.Match(
    Left: error => logger.LogError("Order failed: {Code}", error.Code),
    Right: orderId => logger.LogInformation("Order created: {Id}", orderId));
```

## Request Lifecycle

```mermaid
sequenceDiagram
    participant App as Application
    participant Enc as IEncina
    participant Pipe as Pipeline
    participant Handler

    App->>Enc: Send(command)
    Enc->>Pipe: Execute behaviors

    loop Each Behavior
        Pipe->>Pipe: Validation, Caching, etc.
    end

    Pipe->>Handler: Handle(command, ct)
    Handler-->>Pipe: TResponse
    Pipe-->>Enc: Either[EncinaError, T]
    Enc-->>App: Either[EncinaError, T]
```

## Packages (39 Active)

| Category | Packages | Description |
|----------|----------|-------------|
| **Core** | `Encina` | ROP, CQRS, pipeline behaviors |
| **Validation** | `FluentValidation`, `DataAnnotations`, `MiniValidator`, `GuardClauses` | Request validation with ROP integration |
| **Web** | `AspNetCore`, `SignalR` | Middleware, authorization, real-time notifications |
| **Database** | `EntityFrameworkCore`, `MongoDB`, `Dapper.{5}`, `ADO.{5}` | Persistence with messaging patterns |
| **Messaging** | `Messaging`, `RabbitMQ`, `Kafka`, `AzureServiceBus`, `AmazonSQS`, `NATS`, `MQTT`, `Redis.PubSub`, `InMemory`, `gRPC`, `GraphQL` | Message transports |
| **Caching** | `Caching`, `Caching.Memory`, `Caching.Hybrid`, `Caching.Redis`, `Caching.Valkey`, `Caching.KeyDB`, `Caching.Dragonfly`, `Caching.Garnet` | Multi-tier caching |
| **Scheduling** | `Hangfire`, `Quartz` | Job scheduling adapters |
| **Resilience** | `Extensions.Resilience`, `Polly`, `Refit` | Retry, circuit breaker, HTTP clients |
| **Event Sourcing** | `Marten` | Event store with projections |
| **Observability** | `OpenTelemetry` | Distributed tracing and metrics |

> **Note**: 3,800+ tests across all packages

## Validation

Encina provides three validation approaches, all using the centralized Orchestrator pattern:

```mermaid
flowchart LR
    subgraph Request Pipeline
        A[Request] --> B[ValidationPipelineBehavior]
    end

    subgraph Validation
        B --> C[ValidationOrchestrator]
        C --> D[IValidationProvider]
    end

    subgraph Providers
        D --> E[FluentValidation]
        D --> F[DataAnnotations]
        D --> G[MiniValidator]
    end

    E & F & G --> H{Valid?}
    H -->|Yes| I[Handler]
    H -->|No| J[EncinaError]
```

### FluentValidation

```csharp
// Registration
builder.Services.AddEncinaFluentValidation(typeof(Program).Assembly);

// Validator
public class CreateOrderValidator : AbstractValidator<CreateOrder>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();
    }
}
```

### DataAnnotations

```csharp
// Registration
builder.Services.AddDataAnnotationsValidation();

// Request with attributes
public sealed record CreateOrder(
    [Required] Guid CustomerId,
    [Required, MinLength(1)] List<OrderItem> Items) : ICommand<OrderId>;
```

### MiniValidator

```csharp
// Registration
builder.Services.AddMiniValidation();

// Uses DataAnnotations attributes - ultra-lightweight (~20KB)
```

## Messaging Patterns

Encina provides enterprise messaging patterns for distributed systems:

```mermaid
flowchart TB
    subgraph Outbox Pattern
        A1[Command Handler] --> B1[Save Entity]
        B1 --> C1[Save OutboxMessage]
        C1 --> D1[Commit Transaction]
        D1 --> E1[Background Processor]
        E1 --> F1[Publish to Transport]
    end

    subgraph Inbox Pattern
        A2[Message Received] --> B2[Check InboxMessage]
        B2 -->|New| C2[Process Message]
        B2 -->|Duplicate| D2[Return Cached Response]
        C2 --> E2[Save to Inbox]
    end
```

### Entity Framework Core

```csharp
builder.Services.AddEncinaEntityFrameworkCore<AppDbContext>(config =>
{
    config.UseTransactions = true;  // Automatic transaction management
    config.UseOutbox = true;        // Reliable event publishing
    config.UseInbox = true;         // Idempotent message processing
    config.UseSagas = true;         // Distributed transactions
    config.UseScheduling = true;    // Delayed/recurring execution
});
```

### Immutable Records Support

Encina supports immutable domain entities (C# records) with EF Core while preserving domain events:

```csharp
// Define an immutable aggregate root
public record Order : AggregateRoot<OrderId>
{
    public required string CustomerName { get; init; }
    public required OrderStatus Status { get; init; }

    public Order Ship()
    {
        AddDomainEvent(new OrderShippedEvent(Id));
        return this with { Status = OrderStatus.Shipped };
    }
}

// Update with event preservation (DbContext)
var order = await context.Orders.FindAsync(orderId);
var shippedOrder = order!.Ship().WithPreservedEvents(order);
var result = context.UpdateImmutable(shippedOrder);
await context.SaveChangesAsync(); // Events dispatched automatically

// Or with IUnitOfWork pattern
var orderResult = await unitOfWork.Repository<Order, Guid>().GetByIdAsync(orderId, ct);
var updateResult = orderResult.Bind(order =>
{
    var shipped = order.Ship().WithPreservedEvents(order);
    return unitOfWork.UpdateImmutable(shipped);
});
await unitOfWork.SaveChangesAsync(ct);
```

See [Immutable Domain Models](docs/features/immutable-domain-models.md) for full documentation.

### Dapper / ADO.NET / PostgreSQL / MySQL / SQLite / Oracle

```csharp
// SQL Server with Dapper
builder.Services.AddEncinaDapper(config =>
{
    config.UseOutbox = true;
    config.UseInbox = true;
}, connectionString);
```

## Caching

```csharp
// Configure caching
builder.Services.AddEncinaCaching(options =>
{
    options.EnableQueryCaching = true;
    options.DefaultDuration = TimeSpan.FromMinutes(10);
});

// Add a cache provider
builder.Services.AddEncinaRedisCache("localhost:6379");
// Or: AddEncinaMemoryCache(), AddEncinaHybridCache(), etc.

// Mark queries as cacheable
[Cache(Duration = 300)]
public sealed record GetProductById(Guid Id) : IQuery<Product>;
```

## Resilience

```csharp
builder.Services.AddEncinaStandardResilience(options =>
{
    options.Retry.MaxRetryAttempts = 3;
    options.CircuitBreaker.FailureRatio = 0.5;
    options.Timeout.Timeout = TimeSpan.FromSeconds(30);
});
```

## Streaming

```csharp
// Define a stream request
public sealed record StreamProducts(string Category) : IStreamRequest<Product>;

public sealed class StreamProductsHandler : IStreamRequestHandler<StreamProducts, Product>
{
    public async IAsyncEnumerable<Product> Handle(
        StreamProducts request,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var product in _repository.StreamByCategory(request.Category, ct))
        {
            yield return product;
        }
    }
}

// Consume the stream
await foreach (var result in encina.Stream(new StreamProducts("Electronics"), ct))
{
    result.Match(
        Left: error => logger.LogError("Stream error: {Error}", error.Message),
        Right: product => Console.WriteLine(product.Name));
}
```

## Real-time Notifications (SignalR)

```csharp
builder.Services.AddEncinaSignalR();

// Declarative broadcasting
[BroadcastToSignalR("OrderUpdated")]
public sealed record OrderStatusChanged(Guid OrderId, string Status) : INotification;
```

## OpenTelemetry Integration

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(b => b.AddSource("Encina"))
    .WithMetrics(b => b.AddMeter("Encina"));
```

## Health Checks

Encina provides automatic health check registration for all providers. When you configure a provider, health checks are registered automatically.

```csharp
// Health checks registered automatically
builder.Services.AddEncinaDapper<PostgreSqlConnection>(config =>
{
    config.UseOutbox = true;
    config.ProviderHealthCheck.Enabled = true; // Default
});

// Configure ASP.NET Core health endpoints
builder.Services.AddHealthChecks();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```

### Available Health Checks

| Category | Providers | Default Tags |
|----------|-----------|--------------|
| **Databases** | PostgreSQL, MySQL, SQL Server, SQLite, Oracle, MongoDB, Marten | `database`, `ready` |
| **Message Brokers** | RabbitMQ, Kafka, NATS, MQTT, Azure Service Bus, Amazon SQS | `messaging`, `ready` |
| **Caching** | Redis (and compatible: Valkey, KeyDB, Dragonfly, Garnet) | `caching`, `ready` |
| **Scheduling** | Hangfire, Quartz | `scheduling`, `ready` |
| **Other** | SignalR, gRPC, EF Core | `messaging`/`database`, `ready` |

See [Encina.Messaging README](src/Encina.Messaging/README.md#health-checks) for detailed configuration options.

## Pipeline Behavior Example

```csharp
public sealed class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        _logger.LogInformation("Handling {Request}", typeof(TRequest).Name);

        var result = await next();

        result.Match(
            Left: error => _logger.LogWarning("Failed: {Error}", error.Code),
            Right: _ => _logger.LogInformation("Completed successfully"));

        return result;
    }
}
```

## Project Structure

```text
src/
├── Encina/                    # Core library
├── Encina.Messaging/          # Messaging abstractions
├── Encina.AspNetCore/         # Web integration
├── Encina.EntityFrameworkCore/# EF Core provider
├── Encina.Dapper.*/           # Dapper providers (5 databases)
├── Encina.ADO.*/              # ADO.NET providers (5 databases)
├── Encina.Caching.*/          # Caching providers (8 packages)
├── Encina.FluentValidation/   # FluentValidation integration
└── ...                        # 39 packages total

tests/
├── Encina.UnitTests/          # Consolidated unit tests
├── Encina.IntegrationTests/   # Integration tests
├── Encina.PropertyTests/      # Property-based tests
├── Encina.ContractTests/      # API contract tests
├── Encina.GuardTests/         # Guard clause tests
├── Encina.LoadTests/          # Load testing harness
├── Encina.NBomber/            # NBomber scenarios
└── Encina.BenchmarkTests/     # BenchmarkDotNet benchmarks
```

## Testing

```bash
# Run all tests
dotnet test Encina.slnx --configuration Release

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run mutation testing
dotnet run --file scripts/run-stryker.cs
```

## Documentation

- [ROADMAP.md](ROADMAP.md) - Development phases and planned features
- [CHANGELOG.md](CHANGELOG.md) - Version history
- [docs/history/](docs/history/) - Detailed implementation records
- [docs/messaging/](docs/messaging/) - Saga patterns and transport guides

## Roadmap

Encina is in active development toward **1.0**. See [ROADMAP.md](ROADMAP.md) for details.

### Current Progress

| Phase | Status | Description |
|-------|--------|-------------|
| Phase 1 | ✅ Complete | Stability - All tests passing |
| **Phase 2** | **In Progress** | Functionality - 364 issues across 10 milestones |
| Phase 3 | Pending | Testing & Quality - Coverage targets (85%+) |
| Phase 4 | Pending | Code Quality - SonarCloud compliance |
| Phase 5 | Pending | Documentation - User guides and examples |
| Phase 6 | Pending | Release - NuGet publishing, branding |

### Phase 2 Milestones (v0.10.0 → v0.19.0)

| Version | Focus | Issues |
|---------|-------|--------|
| v0.10.0 | DDD Foundations | 31 |
| v0.11.0 | Testing Infrastructure | 25 |
| v0.12.0 | Database & Repository | 22 |
| v0.13.0 | Security & Compliance | 25 |
| v0.14.0 | Cloud-Native & Aspire | 23 |
| v0.15.0 | Messaging & EIP | 71 |
| v0.16.0 | Multi-Tenancy & Modular | 21 |
| v0.17.0 | AI/LLM Patterns | 16 |
| v0.18.0 | Developer Experience | 43 |
| v0.19.0 | Observability & Resilience | 87 |

→ [View all milestones](https://github.com/dlrivada/Encina/milestones)

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## License

This project is licensed under the [MIT License](LICENSE).

---

**Maintained by**: [@dlrivada](https://github.com/dlrivada)
