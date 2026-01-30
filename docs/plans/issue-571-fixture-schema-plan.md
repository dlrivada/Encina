# Issue #571: EF Core Test Fixtures Schema Creation Plan

## Coding Plan

Create a robust solution for EF Core integration tests that need custom DbContext types with their own entity definitions, beyond the pre-defined messaging entities.

## Problem Statement

The current EF Core test fixture architecture has two levels of schema creation:

1. **Level 1 (Raw SQL)**: `DatabaseFixture.InitializeAsync()` creates tables using raw SQL via `PostgreSqlSchema`, `SqlServerSchema`, etc.
2. **Level 2 (EF Core)**: Tests call `context.Database.EnsureCreatedAsync()` expecting it to create any missing tables.

**The Issue**: When a custom `DbContext` (like `ImmutableTestDbContext`) defines entities NOT in the pre-created SQL schema (like `TestImmutableOrder`), the `EnsureCreatedAsync()` call fails silently or the INSERT fails with "table does not exist".

**Root Cause Analysis**:
- `EnsureCreatedAsync()` checks if the database exists (it does - Testcontainers created it)
- Since the database exists, it does **NOT** run `OnModelCreating()` to create tables
- This is EF Core's expected behavior: `EnsureCreatedAsync()` only creates schema if the database is completely empty

## Observations

1. **SQLite works** because our tests create a fresh in-memory database per test class with a shared connection
2. **PostgreSQL/SqlServer/MySQL fail** because they share a Testcontainers database that already has the messaging tables
3. The `TestEFDbContext` works because its entities (`OutboxMessage`, `InboxMessage`, etc.) are pre-created by raw SQL
4. Custom DbContext types with new entities have no mechanism to register their schema requirements

## Assumptions

### Assumption 1: How should custom entities be added to the schema?

**Options Considered**:

| Option | Description | Pros | Cons |
|--------|-------------|------|------|
| A. Extend raw SQL schemas | Add new tables to `PostgreSqlSchema`, `SqlServerSchema`, etc. | Consistent with current pattern | Requires SQL per provider, pollutes shared schema |
| B. EF Migration per test | Run `context.Database.Migrate()` | Uses EF Core's migration system | Requires migration files, complex setup |
| C. `EnsureDeleted` + `EnsureCreated` | Delete and recreate DB per test | Guarantees clean schema | Slow, loses shared fixture benefit |
| D. Dynamic schema creation in fixture | Add method to create schema from DbContext model | Clean API, leverages EF Core | Must handle existing tables |
| E. Test-specific schema callback | Allow tests to register schema creation | Flexible | Each test must handle it |
| F. Hybrid: EF for test entities only | Use EF Core to create only NEW tables not in raw SQL | Best of both worlds | Complexity in detecting "new" entities |

**Chosen Option**: **D. Dynamic schema creation in fixture**

**Rationale**:
- Keeps the shared fixture pattern for performance
- Leverages EF Core's model builder for provider-specific SQL generation
- Doesn't require manual SQL for new entity types
- Can be added as an enhancement to `IEFCoreFixture`

### Assumption 2: Should this be opt-in or automatic?

**Options Considered**:

| Option | Description |
|--------|-------------|
| A. Automatic | Fixture detects and creates all missing tables |
| B. Explicit method call | Test must call `EnsureSchemaCreatedAsync<TContext>()` |
| C. Attribute-based | Mark test classes that need custom schema |

**Chosen Option**: **B. Explicit method call**

**Rationale**:
- Clear intent in test code
- No magic behavior that might cause confusion
- Consistent with existing `EnsureSchemaCreatedAsync<TContext>()` pattern

### Assumption 3: How to handle table conflicts with existing schema?

**Options Considered**:

| Option | Description |
|--------|-------------|
| A. Drop and recreate | Always recreate if exists |
| B. Skip if exists | Only create missing tables |
| C. Fail on conflict | Throw if table exists with different schema |

**Chosen Option**: **B. Skip if exists** with warning

**Rationale**:
- Non-destructive behavior
- Allows sharing tables between contexts
- Raw SQL tables continue to work

### Assumption 4: What about the #569 tests - are they correct?

**Analysis of current #569 implementation**:

| Component | Assessment |
|-----------|------------|
| SQLite tests | ✅ Correct - uses own connection/schema |
| PostgreSQL/SqlServer/MySQL tests | ❌ Incorrect - uses fixture incorrectly |
| Test entities (`ImmutableTestDbContext`) | ⚠️ Review needed - entity design is fine, but location is wrong |

**Issue with #569 tests**:
The tests define `ImmutableTestDbContext` inline in the SQLite test file, then reuse it from other provider tests. This creates coupling and makes the entities test-specific rather than shared.

## Implementation Steps

### Phase 1: Enhance IEFCoreFixture Interface

**Tasks**:
1. Add `EnsureDynamicSchemaAsync<TContext>()` method to `IEFCoreFixture`
2. Implement in all four EF Core fixtures (SQLite, PostgreSQL, SqlServer, MySQL)
3. Handle existing tables gracefully (CREATE TABLE IF NOT EXISTS equivalent via EF)

### Phase 2: Refactor Test Infrastructure

**Tasks**:
1. Move `TestImmutableOrder`, `TestOrderEvent`, `ImmutableTestDbContext` to `Encina.TestInfrastructure`
2. Add `Orders` table creation to all raw SQL schema classes (PostgreSQL, SqlServer, MySQL, SQLite)
3. Add `Orders` DbSet to `TestEFDbContext` for consistency

### Phase 3: Fix #569 Integration Tests

**Tasks**:
1. Remove inline entity definitions from SQLite test file
2. Update all four provider tests to use shared `TestEFDbContext` with `Orders` entity
3. Remove Skip attributes since tests will now work
4. Verify all tests pass

### Phase 4: Documentation and Cleanup

**Tasks**:
1. Update test infrastructure documentation
2. Add examples of how to add new test entities
3. Close Issue #571

---

## Alternative Approach: Simpler Solution

After further analysis, there's a **simpler approach** that aligns better with existing patterns:

### Approach B: Add Orders to Existing Schema

Instead of creating a generic mechanism, simply add the `Orders` table (and `TestImmutableOrder` entity) to the existing test infrastructure:

**Why this is better**:
1. Follows the established pattern (raw SQL + matching DbContext)
2. No new API to design and maintain
3. Immediate fix for #569
4. Other future test entities can follow the same pattern

**Implementation**:
1. Add `Orders` table SQL to all schema files (`PostgreSqlSchema`, `SqlServerSchema`, `MySqlSchema`, `SqliteSchema`)
2. Add `TestImmutableOrder` entity to `Encina.TestInfrastructure`
3. Add `Orders` DbSet to `TestEFDbContext`
4. Update #569 tests to use `TestEFDbContext`

---

## Recommended Approach

**Go with Approach B (Simpler Solution)** for these reasons:

1. **YAGNI**: We don't need a generic dynamic schema mechanism yet
2. **Consistency**: Follows the existing pattern that works
3. **Speed**: Can be implemented quickly
4. **Risk**: Lower risk than adding new fixture API

If in the future we need many test-specific entities, we can revisit Approach A (Phase 1).

---

## Implementation Plan (Approach B)

### Phase 1: Add Orders Schema to All Providers

**Files to modify**:
- `tests/Encina.TestInfrastructure/Schemas/PostgreSqlSchema.cs`
- `tests/Encina.TestInfrastructure/Schemas/SqlServerSchema.cs`
- `tests/Encina.TestInfrastructure/Schemas/MySqlSchema.cs`
- `tests/Encina.TestInfrastructure/Schemas/SqliteSchema.cs`

**Add**:
```sql
CREATE TABLE IF NOT EXISTS Orders (
    Id UUID PRIMARY KEY,
    CustomerName VARCHAR(200) NOT NULL,
    Status VARCHAR(50) NOT NULL
);
```

### Phase 2: Add Test Entity to Test Infrastructure

**Files to create/modify**:
- `tests/Encina.TestInfrastructure/Entities/TestImmutableOrder.cs` (new)
- `tests/Encina.TestInfrastructure/Entities/TestOrderEvent.cs` (new)
- `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/TestEFDbContext.cs` (add Orders DbSet)

### Phase 3: Update #569 Tests

**Files to modify**:
- `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Sqlite/ImmutableUpdates/ImmutableUpdatesSqliteTests.cs`
- `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/PostgreSQL/ImmutableUpdates/ImmutableUpdatesPostgreSqlTests.cs`
- `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/SqlServer/ImmutableUpdates/ImmutableUpdatesSqlServerTests.cs`
- `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/MySQL/ImmutableUpdates/ImmutableUpdatesMySqlTests.cs`

**Changes**:
1. Remove inline `ImmutableTestDbContext` definition
2. Use `TestEFDbContext` with shared `TestImmutableOrder` entity
3. Remove Skip attributes
4. Update imports

### Phase 4: Verify and Close

**Tasks**:
1. Run all integration tests for all 4 providers
2. Verify 17 ImmutableUpdate tests pass (5 SQLite + 4×3 other providers)
3. Close Issue #571 with reference to PR

---

## Prompt for AI Agents

```
Implement the solution for Issue #571 following the plan in docs/plans/issue-571-fixture-schema-plan.md.

Summary of changes needed:

1. Add `CreateOrdersSchemaAsync()` method to all four schema classes:
   - PostgreSqlSchema.cs
   - SqlServerSchema.cs
   - MySqlSchema.cs
   - SqliteSchema.cs

2. Call the new schema method from each fixture's `CreateSchemaAsync()`:
   - PostgreSqlFixture.cs
   - SqlServerFixture.cs
   - MySqlFixture.cs
   - SqliteFixture.cs

3. Create shared test entities in Encina.TestInfrastructure:
   - TestImmutableOrder (AggregateRoot<Guid> with CustomerName, Status properties)
   - TestOrderEvent (IDomainEvent record)

4. Add Orders configuration to TestEFDbContext.OnModelCreating()

5. Update all four ImmutableUpdate test files:
   - Remove inline entity definitions (keep using statements to new location)
   - Use TestEFDbContext instead of ImmutableTestDbContext
   - Remove Skip.If() calls
   - Update test logic to work with TestEFDbContext

6. Update schema ClearAllDataAsync and DropAllSchemasAsync to include Orders table

7. Run all tests and verify they pass
```

---

## Date: 2026-01-30
## Issue: #571
## Related: #569
