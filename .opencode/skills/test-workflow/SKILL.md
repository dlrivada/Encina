---
name: test-workflow
description: Writes tests covering all encina test types: unit, guard, contract, property, integration. Enforces per-flag obligations model, AAA pattern, real code execution, Docker fixtures, and multi-provider coverage. Triggers on test creation or when asked to write tests.
---

# Test Workflow Skill

Guides the complete test-writing workflow for Encina. Ensures tests follow the project's rigorous standards.

## Test Types (Select Appropriate)

| Type | When Required | Location | Key Rules |
|------|---------------|----------|-----------|
| **Unit** | ✅ All code | `{Package}.Tests/` | Fast (<1ms), mocks deps, isolated |
| **Guard** | ✅ Public methods | `{Package}.GuardTests/` | Null checks → `ArgumentNullException` |
| **Contract** | ✅ Public APIs | `{Package}.ContractTests/` | Interfaces, abstract classes |
| **Property** | 🟡 Complex logic | `{Package}.PropertyTests/` | FsCheck, invariants |
| **Integration** | **🟡 DB ops** | `{Package}.IntegrationTests/ | Docker/Testcontainers, `[Collection]` |
| **Load** | 📄 Concurrency | `Encina.LoadTests/` | Justify if skip |
| **Benchmarks** | 📄 Hot paths | `Encina.BenchmarkTests/` | BenchmarkDotNet, `--list flat` |

## Per-Flag Obligations (CRITICAL)

Each test type is an **independent flag**. Coverage measured per-flag:

| Flag | Project | Target |
|------|---------|--------|
| Unit | `Encina.UnitTests` | ≥85% line |
| Guard | `Encina.GuardTests` | ≥85% line |
| Contract | `Encina.ContractTests` | ≥85% line |
| Property | `Encina.PropertyTests` | ≥80% branch |
| Integration | `Encina.IntegrationTests` | ≥85% line |

**Code execution MUST be real (not reflection):**

| Approach | Coverage |
|----------|----------|
| `new Detector().Detect(x)` | ✅ Covers lines |
| `validator.Validate(x, opts)` | ✅ Covers lines |
| `typeof(IValidator).GetMethod(...)` | ❌ ZERO lines |
| `typeof(T).IsInterface` | ❌ ZERO lines |

## Integration Test Fixtures

```csharp
// ✅ CORRECT - inject fixture via constructor
[Collection("ADO-PostgreSQL")]
public class MyTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;
    public MyTests(PostgreSqlFixture fixture) => _fixture = fixture;
    public async Task InitializeAsync() => await _fixture.ClearAllDataAsync();
}

// ❌ WRONG - per-class fixture
// ❌ WRONG - calling _fixture.DisposeAsync()
// ❌ WRONG - using new PostgreSqlFixture()
```

### Available Collections

`ADO-SqlServer`, `ADO-PostgreSQL`, `ADO-MySQL`, `Dapper-SqlServer`, `Dapper-PostgreSQL`, `Dapper-MySQL`, `EFCore-SqlServer`, `EFCore-PostgreSQL`, `EFCore-MySQL`.

## Test Writing Workflow

### Step 1: Write Unit Tests
- Arrange (setup) → Act (call) → Assert (verify)
- ONE thing per test
- Descriptive names: `AddAsync_ValidMessage_ShouldSucceed`
- Mock dependencies, no shared state

### Step 2: Add Guard Tests
- Null params → `ArgumentNullException`
- Empty/invalid params → `ArgumentException`
- Use GuardClauses.xUnit

### Step 3: Add Contract Tests
- All providers return same shapes
- Use real implementations (not reflection)

### Step 4: Add Property Tests (if complex)
- FsCheck for invariants
- Boundary values, edge cases

### Step 5: Add Integration Tests (if DB)
- Docker/Testcontainers
- Shared `[Collection]` fixtures

### Step 6: Verify
- `dotnet test` locally
- Check manifest for required types
- CI won't trigger prematurely

## Test Data Builders

Use builders for test data:
```csharp
var message = new OutboxMessageBuilder()
    .WithPayload("{\"test\":true}")
    .Build();
```

## Before Commit

- [ ] All required test types present (check manifest)
- [ ] Per-flag coverage targets met
- [ ] Multi-provider coverage (10 DB providers)
- [ ] Tests use real code (not reflection-only)
- [ ] Integration tests use `[Collection]` fixtures
- [ ] No flaky tests
- [ ] `dotnet test` passes
