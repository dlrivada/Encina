# Issue #112 - ROP Assertion Extensions Migration Tracking

## Objective
Migrate ALL tests from verbose patterns to ROP assertion extensions:
- `result.IsRight.Should().BeTrue()` → `result.ShouldBeSuccess()`
- `result.IsLeft.Should().BeTrue()` → `result.ShouldBeError()`
- `result.IsRight.ShouldBeTrue()` → `result.ShouldBeSuccess()`
- `result.IsLeft.ShouldBeTrue()` → `result.ShouldBeError()`
- `Assert.True(result.IsRight)` → `result.ShouldBeSuccess()`
- `Assert.True(result.IsLeft)` → `result.ShouldBeError()`
- Collection patterns like `OnlyContain(r => r.IsRight)` → `AllShouldBeSuccess()`

## Status Legend
- ⬜ Pending
- 🔄 In Progress
- ✅ Migrated
- ⏭️ Skip (with reason)
- ❌ Cannot migrate (with reason)

---

## Files to Migrate

### Encina.Tests (Core)

| File | Status | Patterns Found | Notes |
|------|--------|----------------|-------|
| `EncinaTests.cs` | ⬜ | `result.IsRight.ShouldBeTrue()`, `result.IsLeft.ShouldBeTrue()` (lines 1901, 1910) | |
| `ParallelNotificationDispatchTests.cs` | ⬜ | `result.IsRight.ShouldBeTrue()` (7 occurrences), `result.IsLeft.ShouldBeTrue()` (3 occurrences) | |
| `StreamRequestTests.cs` | ⬜ | `r => r.IsRight`, `r => r.IsLeft` in Count (lines 136-137) | Used for counting, not assertion |
| `Guards/StreamRequestGuardsTests.cs` | ⬜ | `error.IsLeft.Should().BeFalse()`, `error.IsRight.Should().BeFalse()` (lines 24-25, 62-63, 115-116) | Special case: testing default Either |
| `PropertyTests/StreamRequestPropertyTests.cs` | ⬜ | Check for patterns | |
| `Integration/StreamRequestIntegrationTests.cs` | ⬜ | `OnlyContain(r => r.IsRight)` (line 180) | |
| `Contracts/StreamPipelineBehaviorContractTests.cs` | ⬜ | `OnlyContain(r => r.IsRight)` (line 39) | |
| `Contracts/StreamRequestHandlerContractTests.cs` | ⬜ | `OnlyContain(r => r.IsRight)`, `Contain(r => r.IsRight)`, `Contain(r => r.IsLeft)`, `Count(r => r.IsLeft)` (lines 35, 109, 110, 111, 170) | |

### Encina.Testing.Tests

| File | Status | Patterns Found | Notes |
|------|--------|----------------|-------|
| `EncinaFixtureTests.cs` | ⏭️ | `result.IsRight.Should().BeTrue()` (lines 25, 47) | Skip: Part of new Testing package, uses old patterns intentionally to test the fixture |

### Encina.AspNetCore Tests

| File | Status | Patterns Found | Notes |
|------|--------|----------------|-------|
| `ContractTests/AuthorizationPipelineBehaviorContractTests.cs` | ⬜ | `result.IsRight.ShouldBeTrue()` (line 47), `result.IsLeft.ShouldBeTrue()` (lines 150, 200) | |
| `PropertyTests/AuthorizationPipelineBehaviorPropertyTests.cs` | ⬜ | `result.IsLeft.ShouldBeTrue()` (lines 61, 89, 142, 196) | |
| `LoadTests/AuthorizationPipelineBehaviorLoadTests.cs` | ⬜ | `result.IsRight ? Response.Ok()`, `result.IsLeft ? Response.Ok()` (lines 43, 77, 112, 149, 166) | Ternary operators - may need different approach |

### Encina.DataAnnotations Tests

| File | Status | Patterns Found | Notes |
|------|--------|----------------|-------|
| `DataAnnotationsValidationBehaviorTests.cs` | ⬜ | `result.IsRight.ShouldBeTrue()` (lines 45, 165), `result.IsLeft.ShouldBeTrue()` (lines 71, 104, 134, 184) | |
| `Guards/DataAnnotationsValidationBehaviorGuardsTests.cs` | ⬜ | `result.IsRight.ShouldBeTrue()` (lines 81, 164), `result.IsLeft.ShouldBeTrue()` (lines 101, 124, 182), `Assert.True(result.IsRight \|\| result.IsLeft)` (line 207) | |
| `ContractTests/DataAnnotationsValidationBehaviorContractTests.cs` | ⬜ | `result.IsLeft.ShouldBeTrue()` (lines 93, 111), `result.IsRight.ShouldBeTrue()` (lines 147, 306) | |
| `PropertyTests/DataAnnotationsValidationBehaviorPropertyTests.cs` | ⬜ | `result.IsRight.ShouldBeTrue()` (lines 58, 125, 407), `result.IsLeft.ShouldBeTrue()` (lines 92, 285, 327), `tasks.All(t => t.Result.IsRight).ShouldBeTrue()` (line 350) | |
| `IntegrationTests/DataAnnotationsValidationBehaviorIntegrationTests.cs` | ⬜ | `result.IsRight.ShouldBeTrue()` (lines 53, 139, 160), `result.IsLeft.ShouldBeTrue()` (lines 75, 105, 168, 194) | |
| `LoadTests/DataAnnotationsValidationBehaviorLoadTests.cs` | ⬜ | `result.IsRight ? Response.Ok()` (multiple), `result.IsLeft ? Response.Ok()` (line 99) | Ternary operators |

### Encina.Dapper.Sqlite Tests

| File | Status | Patterns Found | Notes |
|------|--------|----------------|-------|
| `IntegrationTests/TransactionPipelineBehaviorTests.cs` | ⬜ | `Assert.True(result.IsRight)` (lines 57, 164, 203), `Assert.True(result.IsLeft)` (lines 98, 254) | |
| `PropertyTests/TransactionPipelineBehaviorPropertyTests.cs` | ⬜ | `Assert.True(result.IsRight)` (lines 78, 231, 276), `Assert.Equal(shouldSucceed, result.IsRight)` (line 193), `Assert.True(result.IsLeft)` (line 112) | |

### Encina.Extensions.Resilience Tests

| File | Status | Patterns Found | Notes |
|------|--------|----------------|-------|
| `IntegrationTests/StandardResilienceEndToEndTests.cs` | ⬜ | `OnlyContain(r => r.IsLeft)` (line 103) | |
| `PropertyTests/StandardResiliencePipelineBehaviorPropertyTests.cs` | ⬜ | `return result.IsRight &&`, `return result.IsLeft &&` (lines 43, 77, 146) | Used in FsCheck property returns |
| `LoadTests/StandardResilienceLoadTests.cs` | ⬜ | `return result.IsRight \|\| result.IsLeft`, `OnlyContain(r => r.IsLeft)` (lines 50, 88, 115) | |

### Encina.FluentValidation Tests

| File | Status | Patterns Found | Notes |
|------|--------|----------------|-------|
| `ValidationPipelineBehaviorTests.cs` | ⬜ | `result.IsRight.ShouldBeTrue()` (lines 51, 150), `result.IsLeft.ShouldBeTrue()` (lines 78, 115, 172, 222) | |
| `Guards/ValidationPipelineBehaviorGuardsTests.cs` | ⬜ | `result.IsRight.ShouldBeTrue()` (lines 104, 123, 188, 207), `result.IsLeft.ShouldBeTrue()` (lines 142, 163) | |
| `ContractTests/ValidationPipelineBehaviorContractTests.cs` | ⬜ | `result.IsRight.ShouldBeTrue()` (line 177), `result.IsLeft.ShouldBeTrue()` (lines 120, 139) | |
| `PropertyTests/ValidationPipelineBehaviorPropertyTests.cs` | ⬜ | `result.IsRight.ShouldBeTrue()` (lines 58, 127, 459), `result.IsLeft.ShouldBeTrue()` (lines 93, 300, 342) | |
| `IntegrationTests/ValidationPipelineBehaviorIntegrationTests.cs` | ⬜ | `result.IsRight.ShouldBeTrue()` (lines 55, 131), `result.IsLeft.ShouldBeTrue()` (lines 78, 109, 168) | |
| `LoadTests/ValidationPipelineBehaviorLoadTests.cs` | ⬜ | `result.IsRight ? Response.Ok()`, `result.IsLeft ? Response.Ok()` (multiple) | Ternary operators |

### Encina.GuardClauses Tests

| File | Status | Patterns Found | Notes |
|------|--------|----------------|-------|
| `GuardsTests.cs` | ⬜ | `result1.IsLeft.ShouldBeTrue()`, `result2.IsRight.ShouldBeTrue()` (lines 719, 723) | Only 2 ROP assertions |
| `IntegrationTests/GuardsIntegrationTests.cs` | ⬜ | `result.IsRight.ShouldBeTrue()` (line 54), `result.IsLeft.ShouldBeTrue()` (lines 73, 99) | |
| `LoadTests/GuardsLoadTests.cs` | ⬜ | `result.IsRight ? Response.Ok()`, `result.IsLeft ? Response.Ok()` (lines 59, 90, 123) | Ternary operators |

### Encina.Hangfire Tests

| File | Status | Patterns Found | Notes |
|------|--------|----------------|-------|
| `IntegrationTests/HangfireRequestJobAdapterIntegrationTests.cs` | ⬜ | `result.IsRight.ShouldBeTrue()` (line 35), `result.IsLeft.ShouldBeTrue()` (lines 60, 86) | |
| `PropertyTests/HangfireRequestJobAdapterPropertyTests.cs` | ⬜ | `result.IsRight.ShouldBeTrue()` (line 40), `result.IsLeft.ShouldBeTrue()` (line 74), `tasks.All(t => t.Result.IsRight).ShouldBeTrue()` (line 139) | |
| `LoadTests/HangfireJobAdapterLoadTests.cs` | ⬜ | `result.IsRight ? Response.Ok()` (lines 45, 121) | Ternary operators |

### Encina.MiniValidator Tests

| File | Status | Patterns Found | Notes |
|------|--------|----------------|-------|
| `MiniValidationBehaviorTests.cs` | ⬜ | `result.IsRight.ShouldBeTrue()` (line 45), `result.IsLeft.ShouldBeTrue()` (lines 71, 97, 122, 147, 171) | |
| `Guards/MiniValidationBehaviorGuardsTests.cs` | ⬜ | `result.IsRight.ShouldBeTrue()` (lines 81, 164), `result.IsLeft.ShouldBeTrue()` (lines 101, 124, 182), `Assert.True(result.IsRight \|\| result.IsLeft)` (line 207) | |
| `ContractTests/MiniValidationBehaviorContractTests.cs` | ⬜ | `result.IsLeft.ShouldBeTrue()` (lines 93, 112), `result.IsRight.ShouldBeTrue()` (lines 145, 299) | |
| `PropertyTests/MiniValidationBehaviorPropertyTests.cs` | ⬜ | `result.IsRight.ShouldBeTrue()` (lines 58, 125, 399), `result.IsLeft.ShouldBeTrue()` (lines 92, 286, 323), `tasks.All(t => t.Result.IsRight).ShouldBeTrue()` (line 346) | |
| `IntegrationTests/MiniValidationBehaviorIntegrationTests.cs` | ⬜ | `result.IsRight.ShouldBeTrue()` (lines 53, 139, 160), `result.IsLeft.ShouldBeTrue()` (lines 75, 105, 168, 194) | |
| `LoadTests/MiniValidationBehaviorLoadTests.cs` | ⬜ | `result.IsRight ? Response.Ok()`, `result.IsLeft ? Response.Ok()` (multiple) | Ternary operators |

### Encina.OpenTelemetry Tests

| File | Status | Patterns Found | Notes |
|------|--------|----------------|-------|
| `IntegrationTests/BasicInstrumentationTests.cs` | ⬜ | `Assert.True(result.IsRight)` (lines 38, 64, 88) | |
| `IntegrationTests/ConsoleExporterIntegrationTests.cs` | ⬜ | Check current state | May have been migrated in previous session |
| `IntegrationTests/OpenTelemetryIntegrationTests.cs` | ⬜ | Check current state | May have been migrated in previous session |
| `LoadTests/OpenTelemetryLoadTests.cs` | ⬜ | `result.IsRight ? Response.Ok()` (lines 47, 89) | Ternary operators |

### Encina.Polly Tests

| File | Status | Patterns Found | Notes |
|------|--------|----------------|-------|
| `IntegrationTests/EndToEndIntegrationTests.cs` | ⬜ | Check current state | May have been migrated in previous session |
| `LoadTests/ResilienceLoadTests.cs` | ⬜ | Check current state | May have been migrated in previous session |

### Encina.Refit Tests

| File | Status | Patterns Found | Notes |
|------|--------|----------------|-------|
| `IntegrationTests/RefitClientIntegrationTests.cs` | ⬜ | `OnlyContain(r => r.IsRight)` (line 73) | |
| `IntegrationTests/RestApiRequestHandlerIntegrationTests.cs` | ⬜ | Check current state | May have been migrated in previous session |
| `PropertyTests/RestApiRequestHandlerPropertyTests.cs` | ⬜ | `return result.IsRight.ToProperty()`, `return result.IsLeft.ToProperty()` (lines 30, 49, 138) | FsCheck property returns |
| `LoadTests/RestApiRequestLoadTests.cs` | ⬜ | `result.IsRight ? Response...`, `if (result.IsRight)` (multiple) | Ternary/conditional operators |

---

## Patterns That CANNOT Be Migrated (Remain As-Is)

### 1. Ternary Operators in Load Tests
Pattern: `result.IsRight ? Response.Ok() : Response.Fail<...>(...)`

These are NOT assertions - they are business logic for returning NBomber responses. They must remain unchanged.

**Files affected:**
- All `*LoadTests.cs` files

### 2. FsCheck Property Returns
Pattern: `return result.IsRight.ToProperty()` or `return result.IsRight && ...`

These are FsCheck property test returns, not assertions. They must remain unchanged.

**Files affected:**
- `Encina.Refit.PropertyTests/RestApiRequestHandlerPropertyTests.cs`
- `Encina.Extensions.Resilience.PropertyTests/StandardResiliencePipelineBehaviorPropertyTests.cs`

### 3. Count Operations
Pattern: `results.Count(r => r.IsRight)` or `results.Count(r => r.IsLeft)`

These are counting operations, not assertions. They must remain unchanged.

**Files affected:**
- `Encina.Tests/StreamRequestTests.cs` (lines 136-137)
- `Encina.Tests/Contracts/StreamRequestHandlerContractTests.cs` (line 111)

### 4. Default Either Checks
Pattern: `error.IsLeft.Should().BeFalse(); error.IsRight.Should().BeFalse()`

Testing that a default Either is neither Left nor Right. No ROP extension for this case.

**Files affected:**
- `Encina.Tests/Guards/StreamRequestGuardsTests.cs`

### 5. Conditional Checks in Tests
Pattern: `Assert.True(result.IsRight || result.IsLeft, "...")`

Testing that result is valid (either success or error). No single ROP extension for this.

**Files affected:**
- `Encina.DataAnnotations.Tests/Guards/DataAnnotationsValidationBehaviorGuardsTests.cs`
- `Encina.MiniValidator.Tests/Guards/MiniValidationBehaviorGuardsTests.cs`

---

## Summary Statistics

- **Total files with patterns**: ~55 files
- **Files to migrate**: ~45 files (assertion patterns only)
- **Files to skip**: ~10 files (non-assertion patterns: ternary, FsCheck, Count)
- **Total patterns to migrate**: ~200+ occurrences

---

## Migration Progress

Last updated: 2024-12-24

### Phase 1: Search Complete ✅
### Phase 2: Document Created ✅
### Phase 3: Migration ⬜ (0/45 files)
### Phase 4: Compile ⬜
### Phase 5: Test ⬜
### Phase 6: Commit ⬜
