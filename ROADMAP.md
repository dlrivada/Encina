# Encina Roadmap

**Version**: Pre-1.0 (breaking changes allowed)

---

## Vision

**Encina** is a functional mediation library for .NET that enables building modern applications with **Railway Oriented Programming** as the core philosophy.

### Design Principles

- **Functional First**: Pure ROP with `Either<EncinaError, T>` as first-class citizen
- **Explicit over Implicit**: Code should be clear and predictable
- **Performance Conscious**: Zero-allocation hot paths, Expression tree compilation
- **Composable**: Behaviors are small, composable units
- **Pay-for-What-You-Use**: All features are opt-in

---

## Milestones

### Milestone 1.0 - Core Stable

The first stable release with production-ready core functionality.

**Exit Criteria**:
- All tests passing (CI green)
- Line coverage ≥85%
- SonarCloud configured with no critical issues
- Documentation site live
- NuGet packages published

### Milestone 1.1 - Advanced Patterns

Event-driven architecture enhancements.

**Planned Features**:
- Projections/Read Models (CQRS read side)
- Event Versioning (upcasting, schema evolution)
- Snapshotting (for large aggregates)
- Dead Letter Queue enhancements

### Milestone 1.2 - Developer Experience

Tooling and testing improvements.

**Planned Features**:
- Encina.Testing package (fluent assertions, test fixtures)
- Source Generators (zero-reflection, NativeAOT support)
- Encina.Cli (scaffolding tool)

### Future Considerations

Features that may be implemented based on community feedback:

- Modular Monolith support (IModule, IModuleRegistry)
- Serverless integration (Azure Functions, AWS Lambda)
- Enterprise Integration Patterns (Routing Slip, Scatter-Gather)

---

## Current Packages

> For detailed implementation history, see [docs/history/](docs/history/)

### Core (5 packages)
- Encina, FluentValidation, DataAnnotations, MiniValidator, GuardClauses

### Web (3 packages)
- AspNetCore, SignalR, OpenTelemetry

### Database (12 packages)
- EntityFrameworkCore, MongoDB
- Dapper: SqlServer, PostgreSQL, MySQL, Sqlite, Oracle
- ADO: SqlServer, PostgreSQL, MySQL, Sqlite, Oracle

### Messaging (10 packages)
- RabbitMQ, AzureServiceBus, AmazonSQS, Kafka, Redis.PubSub
- InMemory, NATS, MQTT, gRPC, GraphQL

### Caching (8 packages)
- Caching (core), Memory, Hybrid
- Redis, Valkey, KeyDB, Dragonfly, Garnet

### Resilience (3 packages)
- Extensions.Resilience, Polly, Refit

### Event Sourcing (1 package)
- Marten

### Job Scheduling (2 packages)
- Hangfire, Quartz

---

## Not Implementing

| Feature | Reason |
|---------|--------|
| Generic Variance | Goes against "explicit over implicit" |
| EncinaResult<T> Wrapper | Either<L,R> from LanguageExt is sufficient |
| Encina.Dapr | Dapr competes with Encina's value proposition |
| Encina.NServiceBus | Enterprise licensing conflicts |
| Encina.MassTransit | Overlapping patterns |
| Encina.Wolverine | Competing message bus |
| Encina.EventStoreDB | Marten provides better .NET integration |

> Deprecated code preserved in `.backup/deprecated-packages/`

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

## Tracking

- **Issues**: https://github.com/dlrivada/Encina/issues
- **Changelog**: [CHANGELOG.md](CHANGELOG.md)
- **History**: [docs/history/](docs/history/)
