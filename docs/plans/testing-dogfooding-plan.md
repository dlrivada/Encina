# Testing Dogfooding Plan - Use Encina.Testing Infrastructure

> **Objective**: Refactor Encina tests to use its own testing infrastructure (`Encina.Testing.*`), demonstrating its real utility and serving as implementation examples.

## Research Summary

### .NET Aspire vs Testcontainers

According to the [official Aspire documentation](https://aspire.dev/testing/write-your-first-test/) and [recent analysis](https://endjin.com/blog/2025/06/dotnet-aspire-db-testing-integration-tests):

| Aspect | Aspire Testing | Testcontainers |
|--------|---------------|----------------|
| **Package** | `Aspire.Hosting.Testing` | `Testcontainers.*` |
| **Approach** | AppHost orchestration | Container management |
| **Integration** | Type-safe resource access | Manual connection strings |
| **Service Discovery** | Built-in | Manual configuration |
| **Ideal for** | New Aspire applications | Existing apps, granular control |
| **Databases** | AddPostgres, AddSqlServer, AddRedis | PostgreSqlContainer, etc. |

**Recommendation**: Aspire Testing for new integration tests, Testcontainers where granular control is needed or for complex existing tests.

### Current Inventory

| Metric | Value |
|--------|-------|
| Source packages (`src/`) | 64 |
| Test projects (`tests/`) | 189 |
| Files using Testcontainers | 85 |
| Files using Encina.Testing | 6 (own tests only) |

### Available Encina.Testing Packages

| Package | Purpose | Status |
|---------|---------|--------|
| `Encina.Testing` | EncinaTestFixture, EncinaTestContext, EitherAssertions | ✅ |
| `Encina.Testing.Fakes` | FakeEncina, FakeOutboxStore, FakeInboxStore, etc. | ✅ |
| `Encina.Testing.Shouldly` | Shouldly-style assertions for Either | ✅ |
| `Encina.Testing.TUnit` | TUnit framework support | ✅ |
| `Encina.Testing.Bogus` | Test data generation | ✅ |
| `Encina.Testing.WireMock` | HTTP API mocking | ✅ |
| `Encina.Testing.Verify` | Snapshot testing | ✅ |
| `Encina.Testing.Architecture` | ArchUnitNET rules | ✅ |
| `Encina.Testing.Respawn` | Database reset | ✅ |
| `Encina.Testing.Testcontainers` | Docker fixtures | ✅ |
| `Encina.Aspire.Testing` | Aspire integration testing | ✅ |

---

## Refactoring Plan

### Phase 1: Core Package (`Encina`)

**Priority**: High (it's the foundation, must demonstrate usage)

| Test Type | Action | Encina.Testing Tools |
|-----------|--------|---------------------|
| Unit Tests | Use `EncinaTestFixture`, `EitherAssertions` | `Encina.Testing` |
| Property Tests | Add with `Encina.Testing.Bogus` for generation | `Encina.Testing.Bogus` |
| Guard Tests | Already exists, verify assertion usage | `Encina.Testing.Shouldly` |
| Contract Tests | Add if missing | `Encina.Testing` |

**Files to refactor**:
- `tests/Encina.Tests/*.cs`
- `tests/Encina.PropertyTests/*.cs` (if exists)
- `tests/Encina.GuardTests/*.cs` (if exists)

### Phase 2: Domain Modeling (`Encina.DomainModeling`)

**Priority**: High

| Test Type | Action | Tools |
|-----------|--------|-------|
| Unit Tests | `EitherAssertions` for Result types | `Encina.Testing.Shouldly` |
| Property Tests | Value Objects invariants | `Encina.Testing.Bogus` |
| Guard Tests | Verify constructors | `Encina.Testing` |
| Contract Tests | Public interfaces | `Encina.Testing` |

### Phase 3: Messaging (`Encina.Messaging`)

**Priority**: High

| Test Type | Action | Tools |
|-----------|--------|-------|
| Unit Tests | Use `FakeOutboxStore`, `FakeInboxStore` | `Encina.Testing.Fakes` |
| Property Tests | Message invariants | `Encina.Testing.Bogus` |
| Contract Tests | Store interfaces | `Encina.Testing` |

### Phase 4: Database Providers (ADO, Dapper, EF Core)

**Priority**: Medium-High (85 files use Testcontainers)

**Migration strategy**:

1. **Keep Testcontainers** for existing tests that work
2. **New tests** with `Encina.Aspire.Testing` where possible
3. **Use `Encina.Testing.Respawn`** for reset between tests

| Provider | Integration Tests | Contract Tests | Property Tests |
|----------|------------------|----------------|----------------|
| ADO.MySQL | Aspire/Testcontainers | `Encina.Testing` | `Encina.Testing.Bogus` |
| ADO.PostgreSQL | Aspire/Testcontainers | `Encina.Testing` | `Encina.Testing.Bogus` |
| ADO.SqlServer | Aspire/Testcontainers | `Encina.Testing` | `Encina.Testing.Bogus` |
| ADO.Oracle | Testcontainers (no Aspire) | `Encina.Testing` | `Encina.Testing.Bogus` |
| ADO.Sqlite | In-memory | `Encina.Testing` | `Encina.Testing.Bogus` |
| Dapper.* | Similar to ADO | Similar | Similar |
| EF Core | Aspire preferred | `Encina.Testing` | `Encina.Testing.Bogus` |

### Phase 5: Caching Providers

**Priority**: Medium

| Provider | Tests | Tools |
|----------|-------|-------|
| Memory | Unit tests | `Encina.Testing`, `Encina.Testing.Fakes` |
| Redis | Integration | `Encina.Aspire.Testing` (AddRedis) |
| Hybrid | Unit + Integration | Combination |

### Phase 6: Message Transports

**Priority**: Medium

| Transport | Tests | Tools |
|-----------|-------|-------|
| RabbitMQ | Integration | `Encina.Aspire.Testing` (AddRabbitMQ) |
| Kafka | Integration | `Encina.Aspire.Testing` (AddKafka) |
| Azure Service Bus | Mock/Emulator | `Encina.Testing.WireMock` |
| Amazon SQS | Mock/LocalStack | `Encina.Testing.WireMock` |

### Phase 7: Web Integration (AspNetCore, Refit, gRPC, SignalR)

**Priority**: Medium

| Package | Tests | Tools |
|---------|-------|-------|
| AspNetCore | Integration | `Encina.Aspire.Testing`, `WebApplicationFactory` |
| Refit | HTTP mocking | `Encina.Testing.WireMock` |
| gRPC | Integration | `Encina.Aspire.Testing` |
| SignalR | Integration | `Encina.Aspire.Testing` |

### Phase 8: Resilience & Observability

**Priority**: Low

| Package | Tests | Tools |
|---------|-------|-------|
| Polly | Unit + chaos | `Encina.Testing`, `Encina.Testing.Bogus` |
| OpenTelemetry | Integration | `Encina.Aspire.Testing` |

### Phase 9: Validation Providers

**Priority**: Low

| Package | Tests | Tools |
|---------|-------|-------|
| FluentValidation | Unit | `Encina.Testing`, `EitherAssertions` |
| DataAnnotations | Unit | `Encina.Testing` |
| MiniValidator | Unit | `Encina.Testing` |

### Phase 10: Serverless & Scheduling

**Priority**: Low

| Package | Tests | Tools |
|---------|-------|-------|
| AwsLambda | Unit + Integration | `Encina.Testing`, mocks |
| AzureFunctions | Unit + Integration | `Encina.Testing`, mocks |
| Hangfire | Unit | `Encina.Testing.Fakes` |
| Quartz | Unit | `Encina.Testing.Fakes` |

---

## Exclusions (Do Not Refactor)

The `Encina.Testing.*` packages **cannot use their own infrastructure** (circular dependency):

- ❌ `Encina.Testing`
- ❌ `Encina.Testing.Fakes`
- ❌ `Encina.Testing.Shouldly`
- ❌ `Encina.Testing.TUnit`
- ❌ `Encina.Testing.Bogus`
- ❌ `Encina.Testing.WireMock`
- ❌ `Encina.Testing.Verify`
- ❌ `Encina.Testing.Architecture`
- ❌ `Encina.Testing.Respawn`
- ❌ `Encina.Testing.Testcontainers`
- ❌ `Encina.Aspire.Testing`

---

## Coverage Standards

According to CLAUDE.md and Sonar standards:

| Metric | Target |
|--------|--------|
| Line Coverage | ≥85% |
| Branch Coverage | ≥80% |
| Method Coverage | ≥90% |
| Mutation Score | ≥80% |

### Required Test Types per Package

| Type | When Applicable | Primary Tool |
|------|----------------|--------------|
| **Unit** | Always | `Encina.Testing`, `EitherAssertions` |
| **Integration** | DB, HTTP, messaging | `Encina.Aspire.Testing` or `Testcontainers` |
| **Contract** | Public APIs | `Encina.Testing` |
| **Property** | Logic with invariants | `Encina.Testing.Bogus` |
| **Guard** | Public methods | `Encina.Testing.Shouldly` |
| **Load** | Performance-critical | NBomber (optional) |

---

## Issues Proposal

### Main Issue (Epic)

**#XXX - [EPIC] Dogfooding: Refactor tests to use Encina.Testing**

Epic that groups all test refactoring tasks.

### Issues by Phase

| Issue | Title | Labels | Milestone |
|-------|-------|--------|-----------|
| #YYY | [TESTING] Dogfood Phase 1: Core package tests | `area-testing`, `dogfooding`, `priority-high` | v0.11.0 |
| #YYY | [TESTING] Dogfood Phase 2: DomainModeling tests | `area-testing`, `dogfooding`, `priority-high` | v0.11.0 |
| #YYY | [TESTING] Dogfood Phase 3: Messaging tests | `area-testing`, `dogfooding`, `priority-high` | v0.11.0 |
| #YYY | [TESTING] Dogfood Phase 4: Database provider tests | `area-testing`, `dogfooding`, `priority-medium` | v0.11.0 |
| #YYY | [TESTING] Dogfood Phase 5-10: Remaining packages | `area-testing`, `dogfooding`, `priority-low` | v0.11.0 |

### Specific Issue: Aspire Migration

| Issue | Title | Labels |
|-------|-------|--------|
| #YYY | [TESTING] Evaluate Aspire Testing vs Testcontainers migration | `area-testing`, `investigation`, `aspire` |

---

## Success Metrics

1. **100% of non-Testing packages** use `Encina.Testing.*` where applicable
2. **Coverage** meets targets (85%+ lines, 80%+ branches)
3. **Tests serve as documentation** - clear usage examples
4. **Green CI/CD** - all tests pass
5. **Boilerplate reduction** - less repetitive code in tests

---

## Suggested Timeline

| Phase | Estimated Duration | Dependencies |
|-------|-------------------|--------------|
| Phase 1 (Core) | 1-2 sessions | None |
| Phase 2 (DomainModeling) | 1 session | Phase 1 |
| Phase 3 (Messaging) | 1 session | Phase 1 |
| Phase 4 (DB Providers) | 3-4 sessions | Phases 1-3 |
| Phases 5-10 | 4-6 sessions | Phase 4 |

**Total estimated**: 10-15 work sessions

---

## Additional Notes

1. **Prioritize core packages** that are most used and visible
2. **Don't break existing tests** - gradual migration
3. **Document patterns** in each phase to facilitate the following ones
4. **Update examples** in README of each Testing package
5. **⚠️ .NET 10 JIT Bug Warning**: Load tests using `IAsyncEnumerable<Either<EncinaError, T>>` may fail in Release builds due to a JIT optimization bug affecting conditional stack allocation escape analysis.
   - **Workaround**: Set environment variable `DOTNET_JitObjectStackAllocationConditionalEscape=0` before running load tests
   - **Re-evaluate**: Remove this workaround once a .NET 10.0.x patch addresses the issue
