# Encina Roadmap

**Last Updated**: 2025-12-23
**Version**: Pre-1.0 (breaking changes allowed)
**Current Name**: Encina

---

## Vision

**Build resilient .NET applications with Railway Oriented Programming.**

Encina is a comprehensive toolkit for building robust .NET applications with explicit error handling, CQRS patterns, messaging infrastructure, and composable pipeline behaviors.

### Design Principles

- **Functional First**: Pure ROP with `Either<EncinaError, T>` as first-class citizen
- **Explicit over Implicit**: Code should be clear and predictable
- **Performance Conscious**: Zero-allocation hot paths, Expression tree compilation
- **Composable**: Behaviors are small, composable units
- **Pay-for-What-You-Use**: All features are opt-in

---

## Project Status: 90% to Pre-1.0

| Category | Packages | Status |
|----------|----------|--------|
| Core & Validation | 5 | ✅ Production |
| Web Integration | 3 | ✅ Production |
| Database Providers | 12 | ✅ Production |
| Messaging Transports | 10 | ✅ Production |
| Caching | 8 | ✅ Production |
| Job Scheduling | 2 | ✅ Production |
| Resilience | 3 | ✅ Production |
| Event Sourcing | 2 | ✅ Production |
| Observability | 1 | ✅ Production |

### Quality Metrics

| Metric | Current | Target | Status |
|--------|---------|--------|--------|
| Line Coverage | 67.1% | ≥85% | 🟡 Phase 3 |
| Branch Coverage | 70.9% | ≥80% | 🟡 Phase 3 |
| Mutation Score | 79.75% | ≥80% | ✅ Achieved |
| Build Warnings | 0 | 0 | ✅ Perfect |
| Tests | 3,803 | ~5,000+ | 🟡 Phase 3 |

---

## Development Phases

The roadmap is organized in **6 sequential phases**, each building upon the previous:

### Phase Overview

| Phase | Name | Focus | Goal |
|-------|------|-------|------|
| **1** | Stability | Compilation & Tests | Green CI, all tests passing |
| **2** | Functionality | Features | Expand Encina capabilities |
| **3** | Testing & Quality | Tests & Issues | Stability and reliability |
| **4** | Code Quality | Static Analysis | SonarCloud, maintainability |
| **5** | Documentation | Docs & Examples | User-facing content |
| **6** | Release Prep | 1.0 Launch | Security, NuGet, branding |

---

## Phase 1: Stability 🔴

**Goal**: Ensure the project compiles and all tests pass (except known CLR crash issue #5)

### Current Blockers

| Issue | Description | Priority | Complexity | Status |
|-------|-------------|----------|------------|--------|
| ~~**CI Benchmark Failure**~~ | ~~`InboxEfCoreBenchmarks.IterationSetup()` has wrong return type~~ | ~~⭐⭐⭐⭐⭐~~ | ~~Low~~ | ✅ **Fixed** |
| **SonarCloud Exclusions** | ContractTests/PropertyTests excluded due to 57 failures | ⭐⭐⭐⭐⭐ | Medium | ⏳ Pending |

### Blocked Upstream (Cannot Fix)

| Issue | Description | Upstream Bug | Status |
|-------|-------------|--------------|--------|
| **[#5] Stream Load Tests** | CLR crash on .NET 10 | [dotnet/runtime#121736](https://github.com/dotnet/runtime/issues/121736) | Fixed in .NET 11, awaiting .NET 10.x backport |

> **Workaround**: Set `DOTNET_JitObjectStackAllocationConditionalEscape=0` to disable the faulty JIT optimization.
> Tests already skipped in CI with `[Trait("Category", "Load")]`.

### Tasks

| Task | Priority | Complexity | Notes |
|------|----------|------------|-------|
| ~~Fix `InboxEfCoreBenchmarks.IterationSetup()` return type~~ | ~~⭐⭐⭐⭐⭐~~ | ~~Low~~ | ✅ Fixed - async → sync |
| Re-enable ContractTests in SonarCloud workflow | ⭐⭐⭐⭐⭐ | Medium | Fix 57 failing tests first |
| Re-enable PropertyTests in SonarCloud workflow | ⭐⭐⭐⭐⭐ | Medium | Fix failing tests first |
| Verify all workflows run green | ⭐⭐⭐⭐⭐ | Low | CI, CodeQL, SBOM, docs |
| Configure badges correctly in README | ⭐⭐⭐⭐ | Low | Show build status |

### Exit Criteria

- [ ] CI workflow passes consistently
- [ ] All unit, contract, and property tests pass
- [ ] Benchmarks run without errors
- [ ] Mutation tests complete successfully
- [ ] All workflow badges show green

---

## Phase 2: Functionality 🟡

**Goal**: Implement functional features to expand Encina capabilities

Features are prioritized: **cross-cutting first** (affect multiple areas), then **isolated features**.

### Cross-Cutting Features (High Impact)

These features affect multiple packages or enable other features:

| Feature | Package | Priority | Complexity | Impact |
|---------|---------|----------|------------|--------|
| **Refactor `Encina.Publish` with guards** | Core | ⭐⭐⭐⭐⭐ | Low | Consistency with Send |
| **Replace `object? Details` with `ImmutableDictionary`** | Core | ⭐⭐⭐⭐ | Medium | Type safety across all errors |
| **Health Check Abstractions** | Core / AspNetCore | ⭐⭐⭐⭐ | Medium | Microservices readiness |
| **Projections/Read Models** | Encina.Projections | ⭐⭐⭐⭐ | High | CQRS read side for all ES providers |
| **Event Versioning** | EventStoreDB, Marten | ⭐⭐⭐⭐ | High | Upcasting, schema evolution |

### Messaging & Enterprise Features

| Feature | Package | Priority | Complexity | Notes |
|---------|---------|----------|------------|-------|
| **Saga Timeouts** | Messaging | ⭐⭐⭐⭐⭐ | Medium | `RequestTimeout<T>()` pattern |
| **Recoverability Pipeline** | Messaging.Enterprise | ⭐⭐⭐⭐ | Medium | Immediate + delayed retries |
| **Automatic Rate Limiting** | Messaging.Enterprise | ⭐⭐⭐⭐ | Medium | Detect outages, auto-throttle |
| **Low-Ceremony Sagas** | Messaging | ⭐⭐⭐⭐ | Medium | Wolverine-style minimal syntax |
| **Dead Letter Queue** | Messaging providers | ⭐⭐⭐ | Medium | Enhanced DLQ handling |
| **Saga Not Found Handler** | Messaging | ⭐⭐⭐ | Low | `IHandleSagaNotFound` |

### Developer Tooling

| Feature | Package | Priority | Complexity | Notes |
|---------|---------|----------|------------|-------|
| **Encina.Testing** | New Package | ⭐⭐⭐⭐⭐ | Medium | EncinaFixture fluent API |
| **ROP Assertion Extensions** | Encina.Testing | ⭐⭐⭐⭐⭐ | Low | `ShouldBeSuccess()`, `ShouldBeError()` |
| **AggregateTestBase** | Encina.Testing | ⭐⭐⭐⭐ | Medium | Given/When/Then for ES |
| **Encina.Cli** | New Package | ⭐⭐⭐ | High | Command-line scaffolding |
| **Encina.OpenApi** | New Package | ⭐⭐⭐ | Medium | Auto-generation from handlers |

### Performance Features

| Feature | Package | Priority | Complexity | Notes |
|---------|---------|----------|------------|-------|
| **Optimize delegate caches** | Core | ⭐⭐⭐⭐ | Medium | Minimize reflection/boxing |
| **Source Generators** | Encina.SourceGenerator | ⭐⭐⭐ | Very High | Zero-reflection, NativeAOT |
| **Switch-based dispatch** | SourceGenerator | ⭐⭐⭐ | High | No dictionary lookup |

### Isolated Features (Lower Priority)

| Feature | Package | Priority | Complexity | Notes |
|---------|---------|----------|------------|-------|
| **Snapshotting** | EventStoreDB, Marten | ⭐⭐⭐ | Medium | For large aggregates |
| **Bulkhead Isolation** | Polly | ⭐⭐⭐ | Medium | Parallel execution isolation |
| **API Versioning Helpers** | AspNetCore | ⭐⭐ | Medium | Contract evolution support |
| **Distributed Lock Abstractions** | Encina.DistributedLock | ⭐⭐ | Medium | IDistributedLock interface |
| **ODBC provider** | Encina.ODBC | ⭐⭐ | Medium | Legacy databases |

### Modular Monolith Support (Future)

| Feature | Priority | Complexity | Notes |
|---------|----------|------------|-------|
| `IModule` interface | ⭐⭐⭐ | Low | Module definition |
| `IModuleRegistry` | ⭐⭐⭐ | Low | Runtime discovery |
| Module lifecycle hooks | ⭐⭐⭐ | Low | OnStart/OnStop |
| Module-scoped behaviors | ⭐⭐ | Medium | Selective pipeline |

### Serverless Integration (Future)

| Feature | Package | Priority | Complexity |
|---------|---------|----------|------------|
| Azure Functions | Encina.AzureFunctions | ⭐⭐⭐ | Medium |
| AWS Lambda | Encina.AwsLambda | ⭐⭐⭐ | Medium |
| Durable Functions | Encina.AzureFunctions | ⭐⭐ | High |

### Enterprise Integration Patterns (Future)

| Pattern | Priority | Complexity |
|---------|----------|------------|
| Routing Slip | ⭐⭐⭐ | Medium |
| Scatter-Gather | ⭐⭐ | High |
| Content-Based Router | ⭐⭐ | Medium |

### Exit Criteria

- [ ] Cross-cutting features implemented
- [ ] Encina.Testing package available
- [ ] Saga enhancements complete
- [ ] All new features have basic tests

---

## Phase 3: Testing & Quality 🟡

**Goal**: Improve test coverage, resolve open issues, ensure reliability

### Test Coverage Improvements

| Task | Current | Target | Priority |
|------|---------|--------|----------|
| Line Coverage | 67.1% | ≥85% | ⭐⭐⭐⭐⭐ |
| Branch Coverage | 70.9% | ≥80% | ⭐⭐⭐⭐⭐ |
| Mutation Score | 79.75% | ≥95% | ⭐⭐⭐⭐ |
| Property-based tests | Partial | Complete | ⭐⭐⭐⭐ |

### Test Architecture

| Task | Priority | Complexity | Notes |
|------|----------|------------|-------|
| Complete Testcontainers fixtures | ⭐⭐⭐⭐⭐ | Medium | SQL Server, PostgreSQL, MySQL, Oracle |
| Remaining provider tests | ⭐⭐⭐⭐ | High | 9 databases × 4 test types |
| Load tests for all providers | ⭐⭐⭐ | Medium | Stress testing |
| Telemetry exhaustive tests | ⭐⭐⭐ | Medium | OpenTelemetry coverage |

### Issue Resolution

| Issue | Priority | Notes |
|-------|----------|-------|
| Resolve any issues from Phase 1 & 2 | ⭐⭐⭐⭐⭐ | Track in GitHub Issues |
| [#5] Stream load tests CLR crash | ⭐⭐⭐ | .NET 10 specific |
| Fix flaky tests if any | ⭐⭐⭐⭐ | Deterministic tests |

### Workflow & Badges

| Task | Priority | Notes |
|------|----------|-------|
| All workflows green | ⭐⭐⭐⭐⭐ | CI, CodeQL, SonarCloud |
| README badges accurate | ⭐⭐⭐⭐ | Build, coverage, version |
| GitHub Actions optimized | ⭐⭐⭐ | Caching, parallel jobs |

### Exit Criteria

- [ ] Line coverage ≥85%
- [ ] All GitHub issues resolved or documented
- [ ] All workflows consistently green
- [ ] Badges display correctly

---

## Phase 4: Code Quality 🟡

**Goal**: Achieve high code quality standards via static analysis

### Static Analysis Tools

| Tool | Status | Priority | Notes |
|------|--------|----------|-------|
| **SonarCloud** | ⚠️ SONAR_TOKEN needed | ⭐⭐⭐⭐⭐ | First scan pending |
| **CodeQL** | ✅ Enabled | ⭐⭐⭐⭐ | Security scanning |
| **PublicAPI Analyzers** | ✅ Enabled | ⭐⭐⭐⭐ | API compatibility |
| **LoggerMessage generators** | ✅ Implemented | ⭐⭐⭐⭐ | CA1848 compliance |

### Quality Targets

| Metric | Target | Priority |
|--------|--------|----------|
| Code Duplication | <3% | ⭐⭐⭐⭐ |
| Cyclomatic Complexity | ≤10/method | ⭐⭐⭐⭐ |
| Technical Debt Ratio | <5% | ⭐⭐⭐ |
| Maintainability Rating | A | ⭐⭐⭐⭐ |

### Tasks

| Task | Priority | Complexity |
|------|----------|------------|
| Configure SONAR_TOKEN secret | ⭐⭐⭐⭐⭐ | Low |
| Run first SonarCloud scan | ⭐⭐⭐⭐⭐ | Low |
| Address SonarCloud findings | ⭐⭐⭐⭐ | Variable |
| Review cyclomatic complexity hotspots | ⭐⭐⭐ | Medium |
| Eliminate code duplication | ⭐⭐⭐ | Medium |

### Exit Criteria

- [ ] SonarCloud configured and scanning
- [ ] No critical/blocker issues
- [ ] Maintainability rating A
- [ ] Code duplication <3%

---

## Phase 5: Documentation 🟡

**Goal**: Create documentation, examples, and developer resources (no budget required)

### Documentation Structure

```
docs/
├── introduction.md
├── quickstart.md
├── fundamentals/
│   ├── encina-pattern.md
│   ├── handlers.md
│   ├── pipeline-behaviors.md
│   └── notifications.md
├── database/
│   ├── overview.md
│   └── [provider].md (12 providers)
├── caching/
│   ├── overview.md
│   └── [provider].md (8 providers)
├── messaging/
│   ├── overview.md
│   └── [transport].md
├── testing/
│   ├── unit-testing.md
│   └── integration-testing.md
└── migration/
    └── from-mediatr.md
```

### Documentation Tasks (No Budget)

| Task | Priority | Complexity | Notes |
|------|----------|------------|-------|
| **Introduction & Philosophy** | ⭐⭐⭐⭐⭐ | Low | Why Encina? |
| **Quickstart Guide (5 min)** | ⭐⭐⭐⭐⭐ | Low | First request in 5 min |
| **Fundamentals (Handlers, Behaviors)** | ⭐⭐⭐⭐⭐ | Medium | Core concepts |
| **Database Providers Overview** | ⭐⭐⭐⭐ | Medium | All 12 providers |
| **Caching Overview** | ⭐⭐⭐⭐ | Medium | All 8 providers |
| **MediatR Migration Guide** | ⭐⭐⭐⭐ | Medium | [#4] GitHub Issue |
| **Package Comparison Tables** | ⭐⭐⭐⭐ | Medium | Feature matrices |

### Examples (No Budget)

| Example | Priority | Complexity |
|---------|----------|------------|
| 01-basic-encina | ⭐⭐⭐⭐⭐ | Low |
| 02-cqrs-complete | ⭐⭐⭐⭐⭐ | Medium |
| 03-entity-framework | ⭐⭐⭐⭐ | Medium |
| 04-dapper-integration | ⭐⭐⭐⭐ | Medium |
| 05-redis-caching | ⭐⭐⭐ | Medium |

### Documentation Site (No Budget)

| Task | Priority | Notes |
|------|----------|-------|
| DocFX configuration | ⭐⭐⭐⭐⭐ | Already configured |
| GitHub Pages deployment | ⭐⭐⭐⭐⭐ | Free hosting |
| API reference generation | ⭐⭐⭐⭐ | From XML docs |
| Search (local or Algolia free) | ⭐⭐⭐ | DocSearch is free for OSS |

### Deferred (Requires Budget)

The following are valuable but require investment:

- ❌ Video courses (equipment, editing)
- ❌ Conference talks (travel, fees)
- ❌ Enterprise support contracts (time)
- ❌ Paid promotion (ads)
- ❌ Custom domain (optional - GitHub Pages works)
- ❌ Swag (stickers, t-shirts)

### Exit Criteria

- [ ] Documentation site live on GitHub Pages
- [ ] Quickstart and fundamentals complete
- [ ] All providers documented
- [ ] MediatR migration guide published
- [ ] At least 5 runnable examples

---

## Phase 6: Release Preparation 🟡

**Goal**: Prepare for 1.0 release - security, publishing, and branding

### Security & Supply Chain

| Task | Priority | Complexity | Notes |
|------|----------|------------|-------|
| SLSA Level 2 compliance | ⭐⭐⭐⭐ | Medium | Build provenance |
| SBOM generation | ✅ Done | - | Workflow exists |
| Sign packages (optional) | ⭐⭐⭐ | Medium | Sigstore/cosign |
| Security policy (SECURITY.md) | ⭐⭐⭐⭐ | Low | Vulnerability reporting |
| Dependabot config review | ⭐⭐⭐⭐ | Low | Already enabled |

### Repository Preparation

| Task | Priority | Complexity | Notes |
|------|----------|------------|-------|
| CONTRIBUTING.md finalized | ⭐⭐⭐⭐⭐ | Low | How to contribute |
| CODE_OF_CONDUCT.md | ⭐⭐⭐⭐ | Low | Community standards |
| Issue templates finalized | ⭐⭐⭐⭐ | Low | Bug, feature, debt |
| PR template | ⭐⭐⭐⭐ | Low | Standard PR format |
| Branch protection rules | ⭐⭐⭐⭐ | Low | Require reviews |
| GitHub Discussions enabled | ⭐⭐⭐ | Low | Q&A, ideas |

### NuGet Publishing

| Task | Priority | Complexity | Notes |
|------|----------|------------|-------|
| Register NuGet packages | ⭐⭐⭐⭐⭐ | Low | Reserve names |
| Configure NuGet API key | ⭐⭐⭐⭐⭐ | Low | Secret management |
| Package metadata review | ⭐⭐⭐⭐ | Low | Icons, descriptions |
| Publish workflow | ⭐⭐⭐⭐ | Medium | Tag-triggered release |
| Version strategy (SemVer) | ⭐⭐⭐⭐ | Low | 1.0.0 and beyond |

### Branding & Visual Identity

| Task | Priority | Complexity | Notes |
|------|----------|------------|-------|
| Logo design | ⭐⭐⭐⭐ | Medium | Encina (holm oak) theme |
| Package icon (128x128 PNG) | ⭐⭐⭐⭐ | Low | NuGet requirement |
| README banner | ⭐⭐⭐ | Low | Visual header |
| Social preview image | ⭐⭐⭐ | Low | GitHub social |
| Color palette | ⭐⭐ | Low | Brand consistency |

### Final Checklist

| Task | Priority |
|------|----------|
| README comprehensive | ⭐⭐⭐⭐⭐ |
| CHANGELOG up to date | ⭐⭐⭐⭐⭐ |
| All links working | ⭐⭐⭐⭐ |
| License correct (MIT) | ⭐⭐⭐⭐⭐ |
| No secrets in code | ⭐⭐⭐⭐⭐ |
| API stable | ⭐⭐⭐⭐⭐ |

### Exit Criteria

- [ ] Security measures in place
- [ ] Repository ready for contributors
- [ ] NuGet publishing automated
- [ ] Visual identity complete
- [ ] 1.0.0 tag created and published

---

## Completed Features

> Detailed implementation history: [docs/history/2025-12.md](docs/history/2025-12.md)
> Version history: [CHANGELOG.md](CHANGELOG.md)

### Core (5 packages)

- Encina Core - ROP, pipelines, CQRS
- FluentValidation, DataAnnotations, MiniValidator, GuardClauses

### Web (3 packages)

- AspNetCore - Middleware, authorization, Problem Details
- SignalR - Real-time notifications
- ~~MassTransit~~ (deprecated)

### Database (12 packages)

- EntityFrameworkCore, MongoDB
- Dapper: SqlServer, PostgreSQL, MySQL, Sqlite, Oracle
- ADO: SqlServer, PostgreSQL, MySQL, Sqlite, Oracle
- Messaging abstractions (Outbox, Inbox, Sagas, Choreography)

### Messaging (10 packages)

- RabbitMQ, AzureServiceBus, AmazonSQS, Kafka
- Redis.PubSub, InMemory, NATS, MQTT
- gRPC, GraphQL

### Caching (8 packages)

- Core, Memory, Hybrid
- Redis, Valkey, KeyDB, Dragonfly, Garnet

### Resilience (3 packages)

- Extensions.Resilience, Polly, Refit

### Event Sourcing (2 packages)

- Marten, EventStoreDB

### Observability (1 package)

- OpenTelemetry - Distributed tracing and metrics

### Other Features (in Core)

- Stream Requests (IAsyncEnumerable)
- Parallel Notification Dispatch strategies
- Choreography Sagas abstractions (in Messaging)

---

## Not Implementing / Deprecated

| Feature | Reason |
|---------|--------|
| Generic Variance | Goes against "explicit over implicit" |
| EncinaResult<T> Wrapper | Either<L,R> from LanguageExt is sufficient |
| **Encina.Dapr** | Dapr competes with Encina's value proposition |
| **Encina.NServiceBus** | Enterprise licensing conflicts |
| **Encina.MassTransit** | Overlapping patterns |
| **Encina.Wolverine** | Competing message bus |

> Deprecated code preserved in `.backup/deprecated-packages/`

---

## GitHub Issues

Track all bugs, features, and technical debt via GitHub Issues:
<https://github.com/dlrivada/Encina/issues>

### Open Issues

| # | Title | Type | Phase | Status |
|---|-------|------|-------|--------|
| 5 | Stream load tests cause CLR crash on .NET 10 | technical-debt | 1 | ⏸️ Blocked upstream ([dotnet/runtime#121736](https://github.com/dotnet/runtime/issues/121736)) |
| 4 | Documentation: MediatR migration guide | documentation | 5 | 📝 Pending |

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
