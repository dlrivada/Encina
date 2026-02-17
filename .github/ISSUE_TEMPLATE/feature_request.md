---
name: Feature Request
about: Suggest a new feature or enhancement
title: "[FEATURE] "
labels: enhancement
assignees: ''
---

## Summary

A clear and concise description of the feature you'd like (1-2 sentences).

## Motivation

Why is this feature needed? What problem does it solve?

- Use cases and scenarios
- Industry comparison (how do other frameworks/tools solve this?)
- Strategic context (why now, why this approach?)

## Proposed Solution

Describe how you envision this feature working. Include:

- Interface definitions with XML doc comments
- Configuration / DI registration examples
- Key types and their relationships

```csharp
// Example API usage (if applicable)
services.AddEncina(config => {
    config.UseNewFeature = true;
});
```

## Alternatives Considered

Describe any alternative solutions or features you've considered, with trade-offs for each:

1. **Alternative A**: Description. Pros/cons.
2. **Alternative B**: Description. Pros/cons.

## Affected Packages

Which Encina packages would this feature affect?

- [ ] Encina (core)
- [ ] Encina.EntityFrameworkCore
- [ ] Encina.Dapper.*
- [ ] Encina.ADO.*
- [ ] Encina.AspNetCore
- [ ] Encina.MongoDB
- [ ] Encina.OpenTelemetry
- [ ] Encina.Caching.*
- [ ] Encina.Messaging
- [ ] Encina.Polly
- [ ] Encina.Cdc
- [ ] Other: ___

## Provider Implementation Matrix

> Per CLAUDE.md Multi-Provider Implementation Rule: All provider-dependent features MUST be implemented for ALL applicable providers.
> **Note**: Omit this section if the feature does not touch provider-specific code.

Use the "Provider Applicability Matrix" in CLAUDE.md to determine which provider categories apply to this issue:

- Database features (stores, repositories, UoW, persistence): 13 database providers required
- Caching features: 8 caching providers
- Messaging/transport features: 10+ messaging transport providers
- Distributed locking features: 4+ lock providers
- Validation features: 3 validation providers
- Cloud-specific features: AWS/Azure/GCP triangle rule
- Event sourcing features: Marten, EventStoreDB
- Resilience features: Polly, Microsoft.Extensions.Resilience
- Observability features: OpenTelemetry + exporters
- Scheduling features: Built-in, Hangfire, Quartz adapters

Example of 13 database providers matrix:

| Provider | Feature A | Feature B | Notes |
|----------|:-:|:-:|-------|
| ADO-SQLite | | | |
| ADO-SqlServer | | | |
| ADO-PostgreSQL | | | |
| ADO-MySQL | | | |
| Dapper-SQLite | | | |
| Dapper-SqlServer | | | |
| Dapper-PostgreSQL | | | |
| Dapper-MySQL | | | |
| EFCore-SQLite | | | |
| EFCore-SqlServer | | | |
| EFCore-PostgreSQL | | | |
| EFCore-MySQL | | | |
| MongoDB | | | |

## Observability

> Define the observability strategy for this feature. All non-trivial features should have OpenTelemetry instrumentation.

### Metrics (OpenTelemetry)

| Metric Name | Type | Description |
|-------------|------|-------------|
| `encina.feature.operation_count` | Counter | Example metric |

### Traces / Spans

| Span Name | Attributes | Description |
|-----------|------------|-------------|
| `encina.feature.operation` | `feature.key` | Example span |

### Health Checks

- Describe any health check endpoints or probes this feature adds.

### Structured Logging

- Key log events and their severity levels.

## Test Matrix

> Per CLAUDE.md Testing Standards: All applicable test types must be implemented or justified.

| Test Type | Required? | Scope | Notes |
|-----------|:---------:|-------|-------|
| **UnitTests** | Required | | |
| **GuardTests** | Required | | |
| **ContractTests** | | | |
| **PropertyTests** | | | |
| **IntegrationTests** | | | |
| **LoadTests** | | | |
| **BenchmarkTests** | | | |

## Implementation Tasks

### Core Abstractions

- [ ] Task 1
- [ ] Task 2

### Provider Implementations (if applicable)

- [ ] ADO.NET providers (4): ...
- [ ] Dapper providers (4): ...
- [ ] EF Core providers (4): ...
- [ ] MongoDB provider (1): ...

### Observability

- [ ] OpenTelemetry metrics
- [ ] Traces / spans
- [ ] Health checks (if applicable)

### Testing

- [ ] UnitTests
- [ ] GuardTests
- [ ] ContractTests
- [ ] PropertyTests
- [ ] IntegrationTests (Docker)
- [ ] LoadTests (if applicable)
- [ ] BenchmarkTests (if applicable)

### Documentation

- [ ] XML doc comments on all public APIs
- [ ] README updates
- [ ] Configuration guide
- [ ] Architecture Decision Record (if applicable)

## Acceptance Criteria

- [ ] Core abstractions implemented and tested
- [ ] All applicable providers have implementations
- [ ] All test types per Test Matrix implemented (or justified with `.md`)
- [ ] OpenTelemetry instrumentation in place
- [ ] Documentation complete
- [ ] Zero build warnings
- [ ] Code coverage >= 85%

## Documentation

What documentation artifacts are needed?

- [ ] Package README
- [ ] Configuration guide in `docs/`
- [ ] ADR in `docs/architecture/adr/` (for architectural decisions)
- [ ] Scaling/usage guidance
- [ ] CHANGELOG.md update
- [ ] ROADMAP.md update (if applicable)

## Related Issues

- #___ - Description
