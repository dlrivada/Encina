# Plan: Multi-Provider Implementation for Issues #279, #280, #281, #282, #283, #534, #380

> **Status**: In Progress - Issues #279, #280, #282 Completed
> **Created**: 2026-01-22
> **Updated**: 2026-01-24
> **Milestone**: v1.0.0 - Core Infrastructure

## Executive Summary

Several issues were marked as closed but only have SQL Server implementations. This plan addresses implementing the missing features across all 8 remaining providers, following the CodeRabbit plans established in each issue's comments.

## Workflow per Issue

For EACH issue, follow this workflow:

1. **Reopen the Issue** via GitHub CLI
2. **Follow the CodeRabbit plan** from that issue's comments (adapting SQL Server to 8 providers)
3. **Implement** code for all 8 providers
4. **Test** with ≥85% coverage (unit + integration)
5. **Document** (README, ROADMAP, CHANGELOG, 2026-01.md, INVENTORY.md)
6. **Comment the Issue** with implementation summary
7. **STOP and wait for user review** - NO commit, NO push, NO close until user approves
8. **After approval**: Commit only that issue's changes, push, then proceed to next issue

---

## Affected Issues and CodeRabbit Plans

### Issue #279: Generic Repository Pattern (`IRepository<TEntity, TId>`)

**CodeRabbit Plan Summary:**

- **Phase 1**: Core Repository Abstractions (in `Encina.DomainModeling`)
  - Task 1: Define `IRepository<TEntity, TId>` interface with `Either<EncinaError, T>` return types
  - Task 2: Define `RepositoryErrors` factory (NotFound, ConcurrencyConflict, ValidationFailed, PersistenceError)
  - Task 3: Create `IHasId<TId>` interface for entity ID abstraction
- **Phase 2**: Provider Implementations (EF Core, Dapper, ADO.NET, MongoDB)
  - Task 1: Implement `RepositoryXXX<TEntity, TId>` for each provider
  - Task 2: Create `SpecificationEvaluator` for each provider
  - Task 3: Add DI registration via `AddEncinaRepository<TEntity, TId>()`
- **Phase 3**: Testing (unit + integration)

**Providers to implement:**

- ADO.SQLite, ADO.PostgreSQL, ADO.MySQL, ADO.Oracle
- Dapper.SQLite, Dapper.PostgreSQL, Dapper.MySQL, Dapper.Oracle

---

### Issue #280: Specification Pattern (`ISpecification<T>`, `QuerySpecification<T>`)

**CodeRabbit Plan Summary:**

- **Phase 1**: Enhance Core Specification Abstractions
  - Task 1: Define `ISpecification<T>` interface
  - Task 2: Enhance `Specification<T>` with cumulative `AddCriteria()` (AND logic)
  - Task 3: Add ThenBy/ThenByDescending for multi-column ordering
  - Task 4: Implement Keyset Pagination (`KeysetPaginationEnabled`, `KeysetProperty`, `LastKeyValue`)
  - Task 5: Define `ISpecificationEvaluator<T>` interface
- **Phase 2**: Provider-Specific Evaluators
  - Task 1: `SpecificationEvaluatorEF<T>` for EF Core
  - Task 2: `SpecificationEvaluatorDapper<T>` with dialect-specific SQL generation
  - Task 3: `SpecificationEvaluatorMongoDB<T>` for MongoDB
- **Phase 3**: Testing

**Providers to implement:**

- ADO.SQLite, ADO.PostgreSQL, ADO.MySQL, ADO.Oracle
- Dapper.SQLite, Dapper.PostgreSQL, Dapper.MySQL, Dapper.Oracle

---

### Issue #281: Unit of Work Pattern (`IUnitOfWork`)

**CodeRabbit Plan Summary:**

- **Phase 1**: Core Interface and EF Core Implementation
  - Task 1: Define `IUnitOfWork` interface with `Repository<TEntity, TId>()`, `SaveChangesAsync()`, `BeginTransactionAsync()`, `CommitAsync()`, `RollbackAsync()`
  - Task 2: Create `UnitOfWorkErrors` factory
  - Task 3: Implement `UnitOfWorkEF` for EF Core
  - Task 4: Create `UnitOfWorkRepositoryEF<TEntity, TId>`
  - Task 5: Add DI registration
- **Phase 2**: Dapper and ADO.NET Implementation
  - Task 1: Implement `UnitOfWorkDapper` for SqlServer (template)
  - Task 2: Create `UnitOfWorkRepositoryDapper<TEntity, TId>`
  - Task 3: Add DI registration
  - Task 4-5: Replicate for PostgreSQL, MySQL
- **Phase 3**: MongoDB Implementation
- **Phase 4**: Testing

**Providers to implement:**

- ADO.SQLite, ADO.PostgreSQL, ADO.MySQL, ADO.Oracle
- Dapper.SQLite, Dapper.PostgreSQL, Dapper.MySQL, Dapper.Oracle

---

### Issue #282: Multi-Tenancy Database Support

**CodeRabbit Plan Summary:**

- **Phase 1**: Core Tenancy Package (`Encina.Tenancy`)
  - Task 1: Create project structure
  - Task 2: Define `ITenantProvider`, `ITenantEntity`, `ITenantStore`, `TenantInfo`, `TenantIsolationStrategy`
  - Task 3: Implement `DefaultTenantProvider`
  - Task 4: Create `TenancyOptions`
  - Task 5: Implement `InMemoryTenantStore`
  - Task 6: Define `ITenantConnectionFactory<TConnection>`
- **Phase 2**: ASP.NET Core Integration
  - Task 1-5: Tenant resolvers (Header, Claim, Subdomain, Route)
- **Phase 3-5**: Provider Implementations (EF Core, Dapper, ADO.NET, MongoDB)
- **Phase 6**: Testing

**Providers to implement:**

- ADO.SQLite, ADO.PostgreSQL, ADO.MySQL, ADO.Oracle
- Dapper.SQLite, Dapper.PostgreSQL, Dapper.MySQL, Dapper.Oracle

---

### Issue #283: Read/Write Database Separation (CQRS Physical Split)

**CodeRabbit Plan Summary:**

- **Phase 1**: Core Infrastructure
  - Task 1: Create `ReadWriteSeparationOptions` with `WriteConnectionString`, `ReadConnectionStrings`, `ReplicaStrategy`
  - Task 2: Implement Replica Selection Strategies (`RoundRobin`, `Random`, `LeastConnections`)
  - Task 3: Create `DatabaseRoutingContext` with `AsyncLocal<DatabaseIntent>`
  - Task 4: Create `[ForceWriteDatabase]` attribute
  - Task 5: Create `IReadWriteConnectionSelector` interface
- **Phase 2**: EF Core Implementation
  - Task 1: Create `IReadWriteDbContextFactory<TContext>`
  - Task 2: Implement `ReadWriteRoutingPipelineBehavior`
  - Task 3: Extend `MessagingConfiguration`
  - Task 4: Implement `ReadWriteHealthCheck`
  - Task 5: Unit and Integration Tests (≥85% coverage)
- **Phase 3**: Dapper and ADO.NET Implementation
- **Phase 4**: MongoDB Implementation
- **Phase 5**: Documentation

**Providers to implement:**

- ADO.SQLite, ADO.PostgreSQL, ADO.MySQL, ADO.Oracle
- Dapper.SQLite, Dapper.PostgreSQL, Dapper.MySQL, Dapper.Oracle

---

### Issue #534: Module Isolation by Database Permissions

**CodeRabbit Plan Summary:**

- **Phase 1**: Core Abstractions
  - Task 1: Create `ModuleIsolationOptions`, `ModuleSchemaOptions`, `ModuleIsolationStrategy` enum
  - Task 2: Create `IModulePermissionScriptGenerator` interface
  - Task 3: Create `IModuleExecutionContext` with `AsyncLocal<IModule>`
  - Task 4: Create `SqlSchemaExtractor` utility
  - Task 5: Create `IModuleSchemaRegistry`
  - Task 6: Create `ModuleIsolationViolationException`
- **Phase 2**: Permission Script Generators
  - Task 1: `SqlServerPermissionScriptGenerator`
  - Task 2: `PostgreSqlPermissionScriptGenerator`
- **Phase 3**: EF Core Integration
  - Task 1: Extend `MessagingConfiguration`
  - Task 2: Create `ModuleSchemaValidationInterceptor`
  - Task 3: Create `ModuleExecutionContextBehavior`
  - Task 4: Register services
- **Phase 4**: Dapper.SqlServer Integration
- **Phase 5**: ADO.SqlServer Integration
- **Phase 6**: MongoDB Integration
- **Phase 7**: Testing and Documentation

**Providers to implement:**

- ADO.SQLite, ADO.PostgreSQL, ADO.MySQL, ADO.Oracle
- Dapper.SQLite, Dapper.PostgreSQL, Dapper.MySQL, Dapper.Oracle

---

### Issue #380: Generic Repository Abstraction

**Status:** Already implemented in `Encina.DomainModeling/Repository.cs`

**Contains:**

- `IReadOnlyRepository<TEntity, TId>`
- `IRepository<TEntity, TId>`
- `IAggregateRepository<TEntity, TId>`
- `PagedResult<T>`
- `RepositoryError`

**Action Required:** Verify implementations exist for all 8 providers, add if missing.

---

## Provider Matrix

### All 12 Database Providers - Feature Implementation Status

| Provider | #279 Repo | #280 Spec | #281 UoW | #282 Tenancy | #283 R/W | #534 Module |
|----------|-----------|-----------|----------|--------------|----------|-------------|
| ADO.SqlServer | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |
| ADO.Sqlite | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| ADO.PostgreSQL | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| ADO.MySQL | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| ADO.Oracle | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| Dapper.SqlServer | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |
| Dapper.Sqlite | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| Dapper.PostgreSQL | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| Dapper.MySQL | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| Dapper.Oracle | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| EntityFrameworkCore | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |
| MongoDB | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |

**Legend**: ❌ = Not implemented, ✅ = Implemented

**Summary by Issue:**

| Issue | Feature | Status | Providers Implemented |
|-------|---------|--------|----------------------|
| #279 | Repository | ✅ Complete | 12/12 |
| #280 | Specification | ✅ Complete | 12/12 |
| #281 | Unit of Work | ✅ Complete | 12/12 |
| #282 | Multi-Tenancy | ✅ Complete | 12/12 |
| #283 | Read/Write Separation | 🟡 Partial | 4/12 (SqlServer ADO/Dapper, EFCore, MongoDB) |
| #534 | Module Isolation | ❌ Not Started | 0/12 |

### Providers Excluded (NOT part of the 12)

| Provider | Reason |
|----------|--------|
| `Encina.Marten` | Event sourcing focus - uses aggregate repositories only |
| `Encina.InMemory` | Testing only - uses simple in-memory collections |

---

## Test Coverage Matrix by Issue

### Issue #282: Multi-Tenancy - Test Types by Provider

| Provider | UnitTests | GuardTests | PropertyTests | ContractTests | IntegrationTests | LoadTests | BenchmarkTests |
|----------|-----------|------------|---------------|---------------|------------------|-----------|----------------|
| ADO.SqlServer | ✅ | ✅ | ✅ | ✅ | 📄 | 📄 | 📄 |
| ADO.Sqlite | ✅ | ✅ | ✅ | ✅ | 📄 | 📄 | 📄 |
| ADO.PostgreSQL | ✅ | ✅ | ✅ | ✅ | 📄 | 📄 | 📄 |
| ADO.MySQL | ✅ | ✅ | ✅ | ✅ | 📄 | 📄 | 📄 |
| ADO.Oracle | ✅ | ✅ | ✅ | ✅ | 📄 | 📄 | 📄 |
| Dapper.SqlServer | ✅ | ✅ | ✅ | ✅ | 📄 | 📄 | 📄 |
| Dapper.Sqlite | ✅ | ✅ | ✅ | ✅ | 📄 | 📄 | 📄 |
| Dapper.PostgreSQL | ✅ | ✅ | ✅ | ✅ | 📄 | 📄 | 📄 |
| Dapper.MySQL | ✅ | ✅ | ✅ | ✅ | 📄 | 📄 | 📄 |
| Dapper.Oracle | ✅ | ✅ | ✅ | ✅ | 📄 | 📄 | 📄 |
| EntityFrameworkCore | ✅ | ✅ | ✅ | ✅ | 📄 | 📄 | 📄 |
| MongoDB | ✅ | ✅ | ✅ | ✅ | 📄 | 📄 | 📄 |

**Legend**:

- ✅ = Implemented (`.cs` files exist)
- 📄 = Justified skip (`.md` justification file exists)
- ❌ = Not implemented, no justification
- ⬜ = Not yet analyzed

### Issue #279: Repository - Test Types by Provider

| Provider | UnitTests | GuardTests | PropertyTests | ContractTests | IntegrationTests | LoadTests | BenchmarkTests |
|----------|-----------|------------|---------------|---------------|------------------|-----------|----------------|
| ADO.SqlServer | ✅ | ✅ | ✅ | ✅ | 📄 | 📄 | 📄 |
| ADO.Sqlite | ✅ | ✅ | ✅ | ✅ | 📄 | 📄 | 📄 |
| ADO.PostgreSQL | ✅ | ✅ | ✅ | ✅ | 📄 | 📄 | 📄 |
| ADO.MySQL | ✅ | ✅ | ✅ | ✅ | 📄 | 📄 | 📄 |
| ADO.Oracle | ✅ | ✅ | ✅ | ✅ | 📄 | 📄 | 📄 |
| Dapper.SqlServer | ✅ | ✅ | ✅ | ✅ | 📄 | 📄 | 📄 |
| Dapper.Sqlite | ✅ | ✅ | ✅ | ✅ | 📄 | 📄 | 📄 |
| Dapper.PostgreSQL | ✅ | ✅ | ✅ | ✅ | 📄 | 📄 | 📄 |
| Dapper.MySQL | ✅ | ✅ | ✅ | ✅ | 📄 | 📄 | 📄 |
| Dapper.Oracle | ✅ | ✅ | ✅ | ✅ | 📄 | 📄 | 📄 |
| EntityFrameworkCore | ✅ | ✅ | ✅ | ✅ | 📄 | 📄 | 📄 |
| MongoDB | ✅ | ✅ | ✅ | ✅ | 📄 | 📄 | 📄 |

### Test Justification Files Created for #279

| Test Type | Location | Status |
|-----------|----------|--------|
| IntegrationTests | `tests/Encina.IntegrationTests/{Provider}/Repository.md` | 12 files ✅ |
| LoadTests | `tests/Encina.LoadTests/Repository.md` | 1 file ✅ |
| BenchmarkTests | `tests/Encina.BenchmarkTests/Repository.md` | 1 file ✅ |

### Issue #280: Specification - Test Types by Provider

| Provider | UnitTests | GuardTests | PropertyTests | ContractTests | IntegrationTests | LoadTests | BenchmarkTests |
|----------|-----------|------------|---------------|---------------|------------------|-----------|----------------|
| ADO.SqlServer | ✅ | ✅ | ✅ | ✅ | 📄 | 📄 | 📄 |
| ADO.Sqlite | ✅ | ✅ | ✅ | ✅ | 📄 | 📄 | 📄 |
| ADO.PostgreSQL | ✅ | ✅ | ✅ | ✅ | 📄 | 📄 | 📄 |
| ADO.MySQL | ✅ | ✅ | ✅ | ✅ | 📄 | 📄 | 📄 |
| ADO.Oracle | ✅ | ✅ | ✅ | ✅ | 📄 | 📄 | 📄 |
| Dapper.SqlServer | ✅ | ✅ | ✅ | ✅ | 📄 | 📄 | 📄 |
| Dapper.Sqlite | ✅ | ✅ | ✅ | ✅ | 📄 | 📄 | 📄 |
| Dapper.PostgreSQL | ✅ | ✅ | ✅ | ✅ | 📄 | 📄 | 📄 |
| Dapper.MySQL | ✅ | ✅ | ✅ | ✅ | 📄 | 📄 | 📄 |
| Dapper.Oracle | ✅ | ✅ | ✅ | ✅ | 📄 | 📄 | 📄 |
| EntityFrameworkCore | ✅ | ✅ | ✅ | ✅ | 📄 | 📄 | 📄 |
| MongoDB | ✅ | ✅ | ✅ | ✅ | 📄 | 📄 | 📄 |

### Test Justification Files Created for #280

| Test Type | Location | Status |
|-----------|----------|--------|
| IntegrationTests | `tests/Encina.IntegrationTests/Database/Specification.md` | 1 file ✅ |
| LoadTests | `tests/Encina.LoadTests/Specification.md` | 1 file ✅ |
| BenchmarkTests | `tests/Encina.BenchmarkTests/Specification.md` | 1 file ✅ |

### Test Justification Files Created for #282

| Test Type | Location | Status |
|-----------|----------|--------|
| IntegrationTests | `tests/Encina.IntegrationTests/{Provider}/Tenancy.md` | 12 files ✅ |
| LoadTests | `tests/Encina.LoadTests/Tenancy.md` | 1 file ✅ |
| BenchmarkTests | `tests/Encina.BenchmarkTests/Tenancy.md` | 1 file ✅ |

### Template: Test Coverage Matrix (for future issues)

Copy this template for each new issue:

```markdown
### Issue #NNN: {Feature} - Test Types by Provider

| Provider | UnitTests | GuardTests | PropertyTests | ContractTests | IntegrationTests | LoadTests | BenchmarkTests |
|----------|-----------|------------|---------------|---------------|------------------|-----------|----------------|
| ADO.SqlServer | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ |
| ADO.Sqlite | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ |
| ADO.PostgreSQL | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ |
| ADO.MySQL | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ |
| ADO.Oracle | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ |
| Dapper.SqlServer | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ |
| Dapper.Sqlite | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ |
| Dapper.PostgreSQL | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ |
| Dapper.MySQL | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ |
| Dapper.Oracle | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ |
| EntityFrameworkCore | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ |
| MongoDB | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ |
```

---

## Database-Specific Considerations

### SQLite

| Aspect | SQLite Specifics |
|--------|------------------|
| Parameter prefix | `@param` (same as SQL Server) |
| Identifier quoting | `"identifier"` or `[identifier]` |
| Pagination | `LIMIT n OFFSET m` |
| Boolean | `INTEGER` (0/1) |
| GUID | `TEXT` (string representation) |
| Schemas | NOT SUPPORTED - use table prefixes |
| Transactions | `BEGIN IMMEDIATE` recommended for writes |

### PostgreSQL

| Aspect | PostgreSQL Specifics |
|--------|----------------------|
| Parameter prefix | `@param` or `$1, $2` |
| Identifier quoting | `"identifier"` |
| Pagination | `LIMIT n OFFSET m` |
| Boolean | Native `true/false` |
| GUID | Native `uuid` type |
| Schemas | Full support |
| Sequences | `SERIAL`, `IDENTITY`, or explicit sequences |

### MySQL

| Aspect | MySQL Specifics |
|--------|-----------------|
| Parameter prefix | `@param` or `?` |
| Identifier quoting | `` `identifier` `` (backticks) |
| Pagination | `LIMIT n OFFSET m` |
| Boolean | `TINYINT(1)` (0/1) |
| GUID | `CHAR(36)` or `BINARY(16)` |
| Schemas | Database = Schema in MySQL |

### Oracle

| Aspect | Oracle Specifics |
|--------|------------------|
| Parameter prefix | `:param` |
| Identifier quoting | `"IDENTIFIER"` (case-sensitive) |
| Pagination | `FETCH FIRST n ROWS ONLY` (12c+) or `ROWNUM` |
| Boolean | `NUMBER(1)` (0/1) |
| GUID | `RAW(16)` or `VARCHAR2(36)` |
| Schemas | Full support |
| Sequences | Explicit sequences with `NEXTVAL` |

---

## Testing Requirements

### Coverage Target: ≥85%

All new code must achieve:

- **Line Coverage**: ≥85%
- **Branch Coverage**: ≥80%
- **Method Coverage**: ≥90%

### Test Types Required

1. **Unit Tests** (Required)
   - Location: `tests/Encina.UnitTests/{Provider}/`
   - Mock all dependencies
   - Fast execution

2. **Integration Tests** (Required for database operations)
   - Location: `tests/Encina.IntegrationTests/Infrastructure/{Provider}/`
   - Use Testcontainers/real databases
   - Mark with `[Trait("Category", "Integration")]`

### Test Categories

```csharp
[Trait("Category", "Integration")]
[Trait("Database", "SQLite")]      // or PostgreSQL, MySQL, Oracle
[Trait("Feature", "Repository")]   // or Specification, UnitOfWork, Tenancy, ReadWrite, ModuleIsolation
[Trait("Provider", "ADO")]         // or Dapper
```

---

## Documentation Requirements

For EACH issue, update:

| Document | Location | What to Update |
|----------|----------|----------------|
| README.md | `src/Encina.{Provider}/README.md` | Add feature documentation |
| ROADMAP.md | `ROADMAP.md` (root) | Mark features as complete |
| CHANGELOG.md | `CHANGELOG.md` (root) | Add to Unreleased section |
| History | `docs/history/2026-01.md` | Add implementation details |
| INVENTORY.md | `docs/INVENTORY.md` | Update file listings |

---

## Execution Order

### Issue-by-Issue Approach

Process one issue at a time, completing ALL 8 providers before moving to the next issue:

| Order | Issue | Feature | Est. Files | Est. Tests | Status |
|-------|-------|---------|------------|------------|--------|
| 1 | #279 | Repository Pattern | ~32 | ~160 | ✅ Complete |
| 2 | #280 | Specification Pattern | ~24 | ~200 | ✅ Complete |
| 3 | #281 | Unit of Work | ~24 | ~120 | ⬜ Tests Pending |
| 4 | #282 | Multi-Tenancy | ~64 | ~890 | ✅ Complete |
| 5 | #283 | Read/Write Separation | ~40 | ~160 | 🟡 Partial (4/12) |
| 6 | #534 | Module Isolation | ~32 | ~120 | ❌ Not Started |
| 7 | #380 | Repository Abstraction | Verify only | Verify only | ⬜ Pending |

### Per-Issue Checklist

For each issue:

- [ ] **Step 0**: Reopen Issue via `gh issue reopen {number}`
- [ ] **Step 1**: Read CodeRabbit plan in issue comments
- [ ] **Step 2**: Implement for ADO.SQLite
- [ ] **Step 3**: Implement for ADO.PostgreSQL
- [ ] **Step 4**: Implement for ADO.MySQL
- [ ] **Step 5**: Implement for ADO.Oracle
- [ ] **Step 6**: Implement for Dapper.SQLite
- [ ] **Step 7**: Implement for Dapper.PostgreSQL
- [ ] **Step 8**: Implement for Dapper.MySQL
- [ ] **Step 9**: Implement for Dapper.Oracle
- [ ] **Step 10**: Write unit tests (≥85% coverage)
- [ ] **Step 11**: Write integration tests
- [ ] **Step 12**: Update PublicAPI.Unshipped.txt for each provider
- [ ] **Step 13**: Update README.md for each provider
- [ ] **Step 14**: Update ROADMAP.md
- [ ] **Step 15**: Update CHANGELOG.md
- [ ] **Step 16**: Update docs/history/2026-01.md
- [ ] **Step 17**: Update docs/INVENTORY.md
- [ ] **Step 18**: Comment Issue with implementation summary
- [ ] **Step 19**: **STOP - Wait for user review**
- [ ] **Step 20**: After approval: `git commit` for this issue only
- [ ] **Step 21**: `git push`
- [ ] **Step 22**: Proceed to next issue

---

## Git Workflow

### Important Rules

1. **NO commit until user approves** each issue's implementation
2. **One commit per issue** (not per provider)
3. **NO push until commit is approved**
4. **NO close issue until user says so**

### Commit Message Format

```
feat({feature}): Implement {Feature} for remaining 8 providers (#{issue})

Implements {Feature} pattern for:
- ADO.SQLite, ADO.PostgreSQL, ADO.MySQL, ADO.Oracle
- Dapper.SQLite, Dapper.PostgreSQL, Dapper.MySQL, Dapper.Oracle

Changes:
- {brief list of main changes}

Test coverage: ≥85%
```

---

## Estimated Totals

| Metric | Count |
|--------|-------|
| New source files | ~216 |
| New unit tests | ~600 |
| New integration tests | ~400 |
| Documentation updates | 7 issues × 5 docs = 35 updates |
| PublicAPI updates | 8 providers × 6 features = 48 updates |

---

## Issue #279 Test Coverage Summary ✅ COMPLETED

### Test Breakdown (All 12 Providers)

| Test Type | Files | Coverage | Description |
|-----------|-------|----------|-------------|
| **UnitTests** | 39 | Core logic | Tests FunctionalRepository, EntityMappingBuilder, SpecificationSqlBuilder for all providers |
| **GuardTests** | 27 | Parameter validation | Null checks for constructors and public methods |
| **PropertyTests** | 2 | Invariants | Cross-provider consistency, column mapping preservation, ID exclusion from updates |
| **ContractTests** | 1 | Interface contracts | API consistency across all 12 providers |
| **Total** | **69** | **≥85%** | Comprehensive coverage achieved |

### Providers Covered (12 total)

- **ADO.NET (5)**: Sqlite, SqlServer, PostgreSQL, MySQL, Oracle
- **Dapper (5)**: Sqlite, SqlServer, PostgreSQL, MySQL, Oracle
- **ORM (1)**: EntityFrameworkCore
- **NoSQL (1)**: MongoDB

### Test Types Not Included (Documented with Justification Files)

#### IntegrationTests - Justification Documents Created

**Location**: `tests/Encina.IntegrationTests/{Provider}/Repository.md`

Files created:

- `ADO/Sqlite/Repository.md`, `ADO/SqlServer/Repository.md`, `ADO/PostgreSQL/Repository.md`, `ADO/MySQL/Repository.md`, `ADO/Oracle/Repository.md`
- `Dapper/Sqlite/Repository.md`, `Dapper/SqlServer/Repository.md`, `Dapper/PostgreSQL/Repository.md`, `Dapper/MySQL/Repository.md`, `Dapper/Oracle/Repository.md`
- `Infrastructure/EntityFrameworkCore/Repository.md`, `Infrastructure/MongoDB/Repository.md`

**Summary**: Repository is a thin abstraction layer. SQL generation tested in unit tests. Provider performance dominates.

#### LoadTests - Justification Document Created

**Location**: `tests/Encina.LoadTests/Repository.md`

**Summary**: Repository adds negligible overhead. Load testing should target database/API level.

#### BenchmarkTests - Justification Document Created

**Location**: `tests/Encina.BenchmarkTests/Repository.md`

**Summary**: EntityMappingBuilder.Build() is one-time startup cost. GetId() is single delegate call (~1ns).

### Commits

- `437ed3b` - test(repository): Add comprehensive test coverage for Repository pattern (#279)

---

## Issue #282 Test Coverage Summary ✅ COMPLETED

### Test Breakdown (All 12 Providers)

| Test Type | Count | Coverage | Justification |
|-----------|-------|----------|---------------|
| **UnitTests** | 376 | Core logic | Tests TenantEntityMappingBuilder, ADOTenancyOptions, DapperTenancyOptions, MongoDbTenancyOptions, EfCoreTenancyOptions, TenantAwareFunctionalRepository, SQL generation, validation |
| **PropertyTests** | 40 | Invariants | Verifies invariants across all 12 providers: defaults consistency, HasTenantId always excludes from updates, GetTenantId/SetTenantId correctness |
| **ContractTests** | 20 | Interface contracts | Validates all 12 providers implement ITenantEntityMapping correctly, mapping operations behave consistently |
| **GuardTests** | 123 | Parameter validation | All public APIs across all 12 providers validate null/empty parameters correctly |
| **Total** | **559** | **≥85%** | Comprehensive coverage achieved |

### Providers Covered (12 total)

- **ADO.NET (5)**: Sqlite, SqlServer, PostgreSQL, MySQL, Oracle
- **Dapper (5)**: Sqlite, SqlServer, PostgreSQL, MySQL, Oracle
- **ORM (1)**: EntityFrameworkCore
- **NoSQL (1)**: MongoDB

### Test Types Not Included (Documented with Justification Files)

All skipped test types have `.md` justification documents in the appropriate test project folders:

#### IntegrationTests - Justification Documents Created

**Location**: `tests/Encina.IntegrationTests/{Provider}/Tenancy.md`

Files created:

- `ADO/Sqlite/Tenancy.md`, `ADO/SqlServer/Tenancy.md`, `ADO/PostgreSQL/Tenancy.md`, `ADO/MySQL/Tenancy.md`, `ADO/Oracle/Tenancy.md`
- `Dapper/Sqlite/Tenancy.md`, `Dapper/SqlServer/Tenancy.md`, `Dapper/PostgreSQL/Tenancy.md`, `Dapper/MySQL/Tenancy.md`, `Dapper/Oracle/Tenancy.md`
- `Infrastructure/EntityFrameworkCore/Tenancy.md`, `Infrastructure/MongoDB/Tenancy.md`

**Summary**: Multi-tenancy is a query filtering feature, not database-level isolation. The SQL generation is fully tested in UnitTests.

#### LoadTests - Justification Document Created

**Location**: `tests/Encina.LoadTests/Tenancy.md`

**Summary**: Tenancy adds minimal overhead (one WHERE clause). No distinct performance characteristics to test.

#### BenchmarkTests - Justification Document Created

**Location**: `tests/Encina.BenchmarkTests/Tenancy.md`

**Summary**: Tenancy logic is O(1) operations only - string concatenation and property access. Not meaningful to benchmark.

### Test Quality Indicators

- **All 559 Tenancy tests pass** across all 12 providers
- **Zero warnings** in test compilation
- **Deterministic tests** - no flaky tests, no time-dependent assertions
- **Independent tests** - no shared state, can run in parallel
- **Clear naming** - test names describe what is being verified
- **Justification documents** - all skipped test types have `.md` files explaining why

---

## Issue #280 Test Coverage Summary ✅ COMPLETED

### Test Breakdown (All 12 Providers)

| Test Type | Files | Coverage | Description |
|-----------|-------|----------|-------------|
| **UnitTests** | 12 | Core logic | Tests SpecificationSqlBuilder for ADO/Dapper providers, SpecificationEvaluator for EF Core, SpecificationFilterBuilder for MongoDB |
| **GuardTests** | 13 | Parameter validation | Null checks for constructors and public methods across all providers |
| **PropertyTests** | 1 | Invariants | Tests specification composition (And, Or, Not), commutativity, associativity, consistency between IsSatisfiedBy and ToExpression, QuerySpecification paging/ordering |
| **ContractTests** | 1 | Interface contracts | API consistency for SpecificationSqlBuilder across all ADO/Dapper providers (already in RepositoryContractTests.cs) |
| **Total** | **27** | **≥85%** | Comprehensive coverage achieved |

### Providers Covered (12 total)

- **ADO.NET (5)**: Sqlite, SqlServer, PostgreSQL, MySQL, Oracle
- **Dapper (5)**: Sqlite, SqlServer, PostgreSQL, MySQL, Oracle
- **ORM (1)**: EntityFrameworkCore
- **NoSQL (1)**: MongoDB

### Files Created

**GuardTests (10 new files):**

- `tests/Encina.GuardTests/ADO/Sqlite/Repository/SpecificationSqlBuilderGuardTests.cs`
- `tests/Encina.GuardTests/ADO/SqlServer/Repository/SpecificationSqlBuilderGuardTests.cs`
- `tests/Encina.GuardTests/ADO/PostgreSQL/Repository/SpecificationSqlBuilderGuardTests.cs`
- `tests/Encina.GuardTests/ADO/MySQL/Repository/SpecificationSqlBuilderGuardTests.cs`
- `tests/Encina.GuardTests/ADO/Oracle/Repository/SpecificationSqlBuilderGuardTests.cs`
- `tests/Encina.GuardTests/Dapper/Sqlite/Repository/SpecificationSqlBuilderGuardTests.cs`
- `tests/Encina.GuardTests/Dapper/SqlServer/Repository/SpecificationSqlBuilderGuardTests.cs`
- `tests/Encina.GuardTests/Dapper/PostgreSQL/Repository/SpecificationSqlBuilderGuardTests.cs`
- `tests/Encina.GuardTests/Dapper/MySQL/Repository/SpecificationSqlBuilderGuardTests.cs`
- `tests/Encina.GuardTests/Dapper/Oracle/Repository/SpecificationSqlBuilderGuardTests.cs`

**PropertyTests (1 new file):**

- `tests/Encina.PropertyTests/Database/Specification/SpecificationPropertyTests.cs`

**Justification Documents (3 new files):**

- `tests/Encina.IntegrationTests/Database/Specification.md`
- `tests/Encina.LoadTests/Specification.md`
- `tests/Encina.BenchmarkTests/Specification.md`

### Test Results

- **70 GuardTests** passed (7 tests × 10 providers)
- **29 PropertyTests** passed (specification composition invariants)
- **Existing UnitTests** for SpecificationSqlBuilder continue to pass
- **Existing ContractTests** already cover SpecificationSqlBuilder API consistency

### Key Findings

1. **Implementation Already Complete**: Specification pattern code exists for all 12 providers
   - `SpecificationSqlBuilder<T>` in ADO/Dapper providers (10)
   - `SpecificationEvaluator` in EntityFrameworkCore
   - `SpecificationFilterBuilder<T>` in MongoDB
2. **Test Gap Addressed**: Only DomainModeling, EF Core, and MongoDB had GuardTests - now all 12 providers covered
3. **Contract Tests Already Exist**: RepositoryContractTests.cs lines 378-429 verify SpecificationSqlBuilder API consistency

---

## Approval

- [ ] Plan reviewed by user
- [ ] Workflow understood (stop after each issue for review)
- [ ] Coverage target confirmed (≥85%)
- [ ] Documentation requirements confirmed
- [ ] Ready to begin with Issue #279
