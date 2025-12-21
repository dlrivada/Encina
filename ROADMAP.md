# SimpleMediator Roadmap

**Last Updated**: 2025-12-21
**Version**: Pre-1.0 (breaking changes allowed)
**Future Name**: Encina (to be renamed before 1.0)

---

## Vision

SimpleMediator (future: **Encina**) is a functional mediation library for .NET that enables building modern applications with **Railway Oriented Programming** as the core philosophy.

### Design Principles

- **Functional First**: Pure ROP with `Either<MediatorError, T>` as first-class citizen
- **Explicit over Implicit**: Code should be clear and predictable
- **Performance Conscious**: Zero-allocation hot paths, Expression tree compilation
- **Composable**: Behaviors are small, composable units
- **Pay-for-What-You-Use**: All features are opt-in

---

## Project Status: 90% to Pre-1.0

| Category | Packages | Status |
|----------|----------|--------|
| Core & Validation | 5 | ✅ Production |
| Web Integration | 2 | ✅ Production |
| Database Providers | 11 | ✅ Production |
| Messaging Transports | 12 | ✅ Production |
| Caching | 8 | ✅ Production |
| Job Scheduling | 2 | ✅ Production |
| Resilience | 3 | ✅ Production |
| Event Sourcing | 2 | ✅ Production |
| **Developer Tooling** | 0/3 | 📋 Pending |

### Quality Metrics

| Metric | Current | Target | Status |
|--------|---------|--------|--------|
| Line Coverage | 67.1% | ≥85% | 🟡 Needs work |
| Branch Coverage | 70.9% | ≥80% | 🟡 Needs work |
| Mutation Score | 79.75% | ≥80% | ✅ Achieved |
| Build Warnings | 0 | 0 | ✅ Perfect |
| Tests | 3,803 | ~5,000+ | 🟡 In progress |

### Test Coverage

- **Core Tests**: 265 passing
- **Database Provider Tests**: 1,763 passing (10 providers)
- **Caching Tests**: 367 passing
- **Total**: 3,000+ tests

---

## Completed Features

> Detailed implementation history: [docs/history/2025-12.md](docs/history/2025-12.md)
> Version history: [CHANGELOG.md](CHANGELOG.md)

### Core (5 packages)

- SimpleMediator Core - ROP, pipelines, CQRS
- FluentValidation, DataAnnotations, MiniValidator, GuardClauses

### Web (2 packages)

- AspNetCore - Middleware, authorization, Problem Details
- SignalR - Real-time notifications

### Database (11 packages)

- EntityFrameworkCore, MongoDB
- Dapper: SqlServer, PostgreSQL, MySQL, Sqlite, Oracle
- ADO: SqlServer, PostgreSQL, MySQL, Sqlite, Oracle

### Messaging (12 packages)

- Wolverine, NServiceBus, MassTransit
- RabbitMQ, AzureServiceBus, AmazonSQS, Kafka
- Redis.PubSub, InMemory, NATS, MQTT
- gRPC, GraphQL

### Caching (8 packages)

- Core, Memory, Hybrid
- Redis, Valkey, KeyDB, Dragonfly, Garnet

### Resilience (3 packages)

- Extensions.Resilience, Refit, Dapr

### Event Sourcing (2 packages)

- Marten, EventStoreDB

### Other

- Hangfire, Quartz (job scheduling)
- OpenTelemetry (observability)
- Stream Requests (IAsyncEnumerable)
- Parallel Notification Dispatch
- Choreography Sagas

---

## In Progress

### Test Architecture Refactoring

**Status**: 🔄 In Progress

Restructuring all test projects to use Testcontainers for real database integration testing.

**Completed**:

- ✅ SimpleMediator.TestInfrastructure with shared fixtures
- ✅ Dapper.Sqlite tests refactored (187 tests, 4 projects)
- ✅ Architecture established (1 project per test type)

**Pending**:

- ⏳ Testcontainers fixtures for SQL Server, PostgreSQL, MySQL, Oracle
- ⏳ Remaining provider tests (9 databases × 4 test types)

---

## Pending Features (Pre-1.0)

### Developer Tooling (0% complete)

| Package | Purpose | Priority |
|---------|---------|----------|
| SimpleMediator.Cli | Command-line scaffolding & analysis | ⭐⭐⭐⭐ |
| SimpleMediator.Testing | MediatorFixture fluent API | ⭐⭐⭐⭐ |
| SimpleMediator.OpenApi | Auto-generation from handlers | ⭐⭐⭐ |

### Core Improvements

| Task | Priority | Complexity |
|------|----------|------------|
| Refactor `SimpleMediator.Publish` with guards | ⭐⭐⭐ | Low |
| Optimize delegate caches (minimize reflection) | ⭐⭐⭐ | Medium |
| Replace `object? Details` with `ImmutableDictionary` | ⭐⭐⭐ | Medium |

### Testing Excellence

| Task | Current | Target |
|------|---------|--------|
| Line Coverage | 67.1% | ≥85% |
| Mutation Score | 79.75% | ≥95% |
| Property-based tests | Partial | Complete |
| Load tests | Partial | All providers |

### Additional Providers

| Package | Priority | Notes |
|---------|----------|-------|
| SimpleMediator.Polly | ⭐⭐⭐⭐ | Retry + circuit breaker |
| SimpleMediator.ODBC | ⭐⭐⭐ | Legacy databases |

---

## Strategic Initiatives (Just Before 1.0)

### Renaming: Encina

**Current Name**: SimpleMediator → **New Name**: Encina

**Why Encina?** Spanish word for holm oak - symbolizing strength, resilience, and longevity.

**Checklist**:

- [ ] Rename GitHub repository
- [ ] Update all namespaces
- [ ] Register new NuGet packages
- [ ] Update documentation

**Timeline**: Complete before 1.0 release

---

## Quality & Security

### Implemented

- ✅ CodeQL scanning on every PR
- ✅ SBOM generation workflow
- ✅ Dependabot enabled
- ✅ TreatWarningsAsErrors=true
- ✅ PublicAPI Analyzers
- ✅ LoggerMessage source generators (CA1848 compliance)

### Planned

- [ ] SLSA Level 2 compliance
- [ ] SonarCloud integration
- [ ] Supply chain security (Sigstore/cosign)

---

## Not Implementing

| Feature | Reason |
|---------|--------|
| Generic Variance | Goes against "explicit over implicit" |
| MediatorResult<T> Wrapper | Either<L,R> from LanguageExt is sufficient |
| Source Generators for Registration | Current Expression trees are fast enough |

See ADR-004 and ADR-005 for detailed rationale.

---

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

### Pre-1.0 Policy

Any feature can be added/modified/removed without restrictions.

### Post-1.0 Policy

Breaking changes only in major versions.

---

## References

### Inspiration

- [MediatR](https://github.com/jbogard/MediatR)
- [Wolverine](https://wolverine.netlify.app/)
- [LanguageExt](https://github.com/louthy/language-ext)

### Concepts

- [Railway Oriented Programming](https://fsharpforfunandprofit.com/rop/)
- [CQRS Pattern](https://martinfowler.com/bliki/CQRS.html)

---

**Maintained by**: @dlrivada
**History**: See [docs/history/](docs/history/) for detailed implementation records
**Changelog**: See [CHANGELOG.md](CHANGELOG.md) for version history
