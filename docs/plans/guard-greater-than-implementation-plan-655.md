# Implementation Plan: `Guards.TryValidateGreaterThan<T>` in Encina.GuardClauses

> **Issue**: [#655](https://github.com/dlrivada/Encina/issues/655)
> **Type**: Feature
> **Complexity**: Low (3 phases, no provider category, 8 files, see Estimated File Count)
> **Estimated Scope**: ~30 lines of production code + ~200 lines of tests (about 280 changed lines in total)

---

## Summary

`Encina.GuardClauses` has no guard for an open-ended upper bound ("the discount must not exceed 100", "the quantity must not exceed the maximum allowed", "the upload must not exceed the size limit"). This plan adds `Guards.TryValidateGreaterThan<T>(T value, string paramName, T threshold, out EncinaError error, string? message = null) where T : IComparable<T>`. Following the .NET convention written in the issue, the guard **fails when `value > threshold`** and passes when `value <= threshold`; the name states the condition that triggers the error, like `ArgumentOutOfRangeException.ThrowIfGreaterThan<T>` (.NET 8).

This is the third plan of the family guards-1 (#653, #654, #655, #656, #657). It adds one public method on top of the pieces owned by #653 (error helper, category, code) and #654 (naming rule, `threshold` key, NaN rule).

- **Standards**: none; .NET parity with `ArgumentOutOfRangeException.ThrowIfGreaterThan<T>` (since .NET 8) through the Try-pattern of `Guards` (ADR-001 and ADR-006).
- **Affected packages**: `Encina.GuardClauses` only (`Guards.cs`, `PublicAPI.Unshipped.txt`, `README.md`).
- **Provider category**: none. `Guards` is a pure static class, so the matrices of AGENTS.md section 5 do not apply.
- **Scope**: 1 new public method (no new helper), plus tests, README, changelog fragment, `PublicAPI.Unshipped.txt` and the manifest entry: 8 files. Fits one worker PR (at most 15 files, 1500 lines).
- **Prerequisites**: #653 and #654 merged first (helper, category constant, `threshold` argument, `IsNaN<T>`); the implementing worker stops and reports if they are missing.

### Shared Pieces and Ownership (family guards-1 and guards-2)

| Shared piece | Owner | Used by |
|--------------|-------|---------|
| `CreateComparisonError`, category constant, error code, no rejected value in the details, `ErrorCategory` | #653 | all |
| Naming rule, `threshold` key, `IsNaN<T>` and the NaN rule | #654 | this plan, #657, #658, #659, #660 |
| This plan adds no shared piece | #655 | none |

Implementation PRs land in the order #653, #654, #655, #656, #657, then guards-2, one at a time.

---

## Design Choices

<details>
<summary><strong>1. Method name: the failing condition or the required condition</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `TryValidateGreaterThan`, fails when `value > threshold` (the issue text)** | One-to-one with `ThrowIfGreaterThan`; the acceptance criteria of #655 stay valid | The 12 existing guards name the required condition, so a reader may expect `true` when the value IS greater |
| **B) `TryValidateLessThanOrEqual`, same behavior** | Consistent with `TryValidatePositive` and the other existing guards | Not the name of the issue; collides with the name of #657 under option A (see the mapping in #654) |

### Chosen Option: **A — `TryValidateGreaterThan` (recommended, pending the maintainer)**

### Rationale

- The decision belongs to the whole family and is owned by #654 (choice 1, with the full mapping of the seven renamed methods); it is answered once and applies here. Option B wins for readers who meet the guard next to `TryValidatePositive`; option A wins where parity with the .NET helper names is the goal, as the issues state.
- The XML summary starts with "Fails when `value` is greater than `threshold`".

- **Class:** Dilemma

</details>

<details>
<summary><strong>2. Boundary: is a value equal to the threshold accepted</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Equal passes; only `value > threshold` fails** | Exactly `ThrowIfGreaterThan`; "discount <= 100" is the use case; the strict-versus-inclusive sibling is #658 (`GreaterThanOrEqual`) | None |
| **B) Equal fails** | Would duplicate #658 | Contradicts the issue and the .NET helper |

### Chosen Option: **A — equal passes**

### Rationale

- The issue states "returns `false` when `value > threshold`" and the property "returns false iff value > threshold"; a unit test for `value == threshold` pins it.

- **Class:** Settled
- **Settled by:** #655 acceptance criteria and `ArgumentOutOfRangeException.ThrowIfGreaterThan<T>` (throws only when `value.CompareTo(other) > 0`)

</details>

<details>
<summary><strong>3. NaN passes an upper bound unless the guard rejects it</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Pure `IComparable<T>` order** | Same as the .NET helper | `double.NaN.CompareTo(100d)` is `-1`, so `TryValidateGreaterThan(double.NaN, ..., 100d)` returns `true`: the "maximum discount" guard accepts NaN |
| **B) A NaN value or a NaN threshold fails the guard (`IsNaN<T>` of #654)** | Fail closed; the use cases of this issue (discount, quantity, quota) are exactly where a NaN must not pass | Differs from the .NET helper for NaN |

### Chosen Option: **B — reject NaN (recommended, pending the maintainer)**

### Rationale

- This is the guard where choice 3 of #654 matters most, because the upper bound is the check NaN would silently pass; the decision is owned by #654 and answered once. Option A wins when exact .NET behavior is the contract; option B wins when the bound protects money or limits.
- Tests: `TryValidateGreaterThan(double.NaN, "p", 100d)` returns `false`; `TryValidateGreaterThan(5d, "p", double.NaN)` returns `false`; infinities compare normally (`PositiveInfinity` fails against a finite threshold).

- **Class:** Dilemma

</details>

<details>
<summary><strong>4. Comparison, constraint and details</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Comparer<T>.Default.Compare`, `where T : IComparable<T>`, details with `guard`, `parameter`, `type`, `stage`, `threshold` and no value** | Same as #654: null-safe, accepts `DateTime`, `TimeSpan`, `string` and numbers; threshold is a bound, not input | None |
| **B) `value.CompareTo(threshold)`, details with the value** | Same idiom as the file; matches the issue's "actual value" wording | A null reference value throws; repeats the defect of #2108 |

### Chosen Option: **A — as #654**

### Rationale

- A null `string` value is "less than" any non-null threshold, so it passes an upper bound and does not throw; the guard test pins this behavior (a null value is a missing value, not an exceeded limit: callers who need presence use `TryValidateNotNull`).

- **Class:** Trivial
- **Settled by:** AGENTS.md section 3 (ROP, no exceptions for business logic) and #654 choices 2 and 4 (same code path)

</details>

---

## Implementation Phases

### Phase 1: `TryValidateGreaterThan`

<details>
<summary>Tasks</summary>

1. `src/Encina.GuardClauses/Guards.cs`: add `public static bool TryValidateGreaterThan<T>(T value, string paramName, T threshold, out EncinaError error, string? message = null) where T : IComparable<T>`: when `!IsNaN(value) && !IsNaN(threshold) && Comparer<T>.Default.Compare(value, threshold) <= 0` then `error = default; return true;`, otherwise `error = CreateComparisonError("GreaterThan", paramName, typeof(T), message ?? $"{paramName} must not be greater than {threshold}.", threshold)` and `return false`. XML docs: summary starting "Fails when `value` is greater than `threshold`", `typeparam`, every `param`, `returns`, `example` with the discount case of the issue.
2. `src/Encina.GuardClauses/PublicAPI.Unshipped.txt`: add `static Encina.GuardClauses.Guards.TryValidateGreaterThan<T>(T value, string! paramName, T threshold, out Encina.EncinaError error, string? message = null) -> bool` (via `mechanical-fixer`, sorted).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina.GuardClauses (src/Encina.GuardClauses/Guards.cs), .NET 10 / C# 14. Issue #655 (plan docs/plans/guard-greater-than-implementation-plan-655.md). #653 and #654 are merged: Guards.cs has GuardErrorCategory, CreateComparisonError(guard, paramName, valueType, message, threshold) and IsNaN<T>.
TASK: Add the public method TryValidateGreaterThan<T> described in the plan, Phase 1, following the Chosen Option of every choice as the maintainer answered it (Dilemmas are recorded as recommended, pending the maintainer: re-read the Maintainer Decisions section if it exists; if the naming was answered with option B, use the renamed method of the mapping in #654).
KEY RULES: never throw; ROP; no rejected value in the details; the error code is Guards.GuardValidationFailed; XML docs with example; one PublicAPI.Unshipped.txt line; zero warnings; a unit test must reach every line you add.
REFERENCE FILES: src/Encina.GuardClauses/Guards.cs (the methods added by #653 and #654), src/Encina.GuardClauses/PublicAPI.Unshipped.txt
```

</details>

### Phase 2: Tests

<details>
<summary>Tasks</summary>

1. Unit (`tests/Encina.UnitTests/GuardClauses/GuardsTests.cs`, `#region TryValidateGreaterThan Tests`): value below, equal to and above the threshold for `int`, `decimal`, `double`, `DateTime`, `TimeSpan` and `string` (above returns `false`; equal and below return `true`); NaN as value and as threshold returns `false`; `double.PositiveInfinity` fails and `double.NegativeInfinity` passes against a finite threshold; custom message; default message names the parameter and the threshold; error not `default`, code `Guards.GuardValidationFailed`, `GetCategory()` equal to the constant; details hold `guard = "GreaterThan"`, `parameter`, `type`, `stage`, `threshold` and no `value`; success leaves `error` as `default`.
2. Guard (`tests/Encina.GuardTests/Validation/GuardClauses/GuardsGuardTests.cs`): `TryValidateGreaterThan_NullStringValue_ReturnsTrueWithoutThrowing`, `TryValidateGreaterThan_NullStringThreshold_ReturnsFalseWithoutThrowing`, `TryValidateGreaterThan_NullMessage_UsesDefaultMessage` (valid result), `TryValidateGreaterThan_EmptyParamName_StillReturnsError`, `TryValidateGreaterThan_WhitespaceParamName_StillReturnsError`. No `ArgumentNullException` test: guards never validate `paramName` and never throw.
3. Property (`tests/Encina.PropertyTests/Validation/GuardClauses/GuardsPropertyTests.cs`): FsCheck `[Property(MaxTest = 300)]`: for `int` and `decimal` pairs the result is `false` iff `value > threshold`; for `double` pairs the result is `false` whenever either is NaN; the error is `default` exactly when the result is `true`.
4. No contract, integration, load or benchmark test (pure comparison; justification in the PR description, as in #653).
5. Per-file targets: the entry `Guards.cs` keeps the values owned by #653 (unit 100, guard 100, property per #2043); the worker re-measures and reports.

| File | Unit | Guard | Contract | Property | Integration | One-sentence justification |
|------|------|-------|----------|----------|-------------|----------------------------|
| `Guards.cs` | 100 | 100 | 0 | 87 (owned by #2043) | 0 | The new method has one success and one failure branch, both reached by value-driven unit and guard tests; the property target stays with #2043; contract and integration are 0 because the class has no interface and no external dependency. |

   CRAP of `TryValidateGreaterThan`: complexity 4, fully covered, below 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1 is done. Tests for Encina.GuardClauses live in tests/Encina.UnitTests/GuardClauses/GuardsTests.cs, tests/Encina.GuardTests/Validation/GuardClauses/GuardsGuardTests.cs and tests/Encina.PropertyTests/Validation/GuardClauses/GuardsPropertyTests.cs.
TASK: Write the unit, guard and property tests of Phase 2 for TryValidateGreaterThan with Shouldly through Encina.Testing.Shouldly; confirm the manifest targets of Guards.cs via mechanical-fixer.
KEY RULES: tests execute the real Guards class; AAA; one behavior per test; no Thread.Sleep; the guard tests include the null-message case as a valid result; assert details through error.GetDetails() and the category through error.GetCategory().
REFERENCE FILES: tests/Encina.UnitTests/GuardClauses/GuardsTests.cs, tests/Encina.GuardTests/Validation/GuardClauses/GuardsGuardTests.cs, tests/Encina.PropertyTests/Validation/GuardClauses/GuardsPropertyTests.cs, .github/coverage-manifest/Encina.GuardClauses.json
```

</details>

### Phase 3: Documentation and finalization

<details>
<summary>Tasks</summary>

1. `src/Encina.GuardClauses/README.md` (via `docs-writer`): add the guard to the "Numeric Validation" block and the table of the family guards that #654 started, with the discount example (constant `Guards.GuardValidationFailed` in the `Ensure` equivalent).
2. `changelog.d/655-guards-try-validate-greater-than.added.md` (via `mechanical-fixer`).
3. `ROADMAP.md`, `docs/INVENTORY.md`, `docs/releases/` and ADRs need no change.
4. Build `dotnet build --configuration Release` (0 warnings), `dotnet test` of the three test projects filtered to `GuardClauses`, `ErrorCategoryCompletenessStaticScanTests`, `--check-justifications`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Phases 1 and 2 are done and green.
TASK: Document the guard in the Encina.GuardClauses README and add the changelog fragment; run the build and the tests listed in the plan.
KEY RULES: never edit CHANGELOG.md; fragment name <issue>-<slug>.<section>.md; README by docs-writer; fragment and PublicAPI by mechanical-fixer; no hand-typed coverage figures.
REFERENCE FILES: src/Encina.GuardClauses/README.md, changelog.d/README.md, src/Encina.GuardClauses/PublicAPI.Unshipped.txt
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Relevance |
|--------|-----------|
| `ArgumentOutOfRangeException.ThrowIfGreaterThan<T>` (.NET 8) | Throws when `value > other`; this guard returns `false` in the same case |
| IEEE 754 and `double.CompareTo` | NaN sorts before every number: the reason for choice 3 |
| AGENTS.md section 3 | ROP, fail closed |
| ADR-062 | Error category per code (#653 choice 4) |
| #2108, #3737, #233 | Rejected-input details, category slice, error-code contract |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `Guards` | `src/Encina.GuardClauses/Guards.cs` | Hosts the method |
| `CreateComparisonError`, category constant | added by #653 and #654 in `Guards.cs` | Error construction |
| `IsNaN<T>` | added by #654 in `Guards.cs` | NaN rule |
| `Guards.GuardValidationFailed` | `src/Encina.GuardClauses/Guards.cs:78` | The error code |
| `Encina.GuardClauses.json` | `.github/coverage-manifest/Encina.GuardClauses.json` | Per-file targets |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| Encina.GuardClauses | none | Guards do not log. The block 700-749 stays unused |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production | 2 | `Guards.cs`, `PublicAPI.Unshipped.txt` |
| Tests | 3 | `GuardsTests.cs`, `GuardsGuardTests.cs`, `GuardsPropertyTests.cs` (modified) |
| Docs and metadata | 3 | `README.md`, `changelog.d/655-guards-try-validate-greater-than.added.md`, `.github/coverage-manifest/Encina.GuardClauses.json` (only if a measured value changes) |
| **Total** | **8** | About 280 changed lines; no `ProjectReference`, DI, contract class or test double changes |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina (pre-1.0, .NET 10, C# 14). Issue #655: Guards.TryValidateGreaterThan<T> in Encina.GuardClauses (fails when value > threshold). AGENTS.md rules apply (ROP, no exceptions, zero warnings, per-flag coverage, CRAP <= 10). #653 and #654 are merged first and own the helper, category, threshold key and NaN rule.

IMPLEMENTATION OVERVIEW: (1) public TryValidateGreaterThan<T> where T : IComparable<T> using Comparer<T>.Default, IsNaN<T> and CreateComparisonError; (2) unit, guard and FsCheck property tests; (3) README, changelog fragment, PublicAPI line.

KEY PATTERNS: bool Try-pattern with out EncinaError; details keys parameter/type/stage/guard/threshold, never the value; specialists for PublicAPI, changelog and manifest lines; docs-writer for the README.

REFERENCE FILES: src/Encina.GuardClauses/Guards.cs, src/Encina.GuardClauses/PublicAPI.Unshipped.txt, tests/Encina.UnitTests/GuardClauses/GuardsTests.cs, tests/Encina.GuardTests/Validation/GuardClauses/GuardsGuardTests.cs, tests/Encina.PropertyTests/Validation/GuardClauses/GuardsPropertyTests.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Pure comparison; nothing to cache |
| 2 | OpenTelemetry | ❌ | Too low-level for tracing; no activity, so ADR-054 D4 tenant tags do not apply |
| 3 | Structured Logging | ❌ | Guards never log; the caller decides. No EventId is allocated |
| 4 | Health Checks | ❌ | No dependency to check |
| 5 | Validation | ✅ | The feature is a validation guard on the same `Left` path as the other guards |
| 6 | Resilience | ❌ | No external call |
| 7 | Distributed Locks | ❌ | No shared state |
| 8 | Transactions | ❌ | No state change |
| 9 | Idempotency | ❌ | Pure function |
| 10 | Multi-Tenancy | ❌ | Tenant-independent: no tenant id and no tenant-qualified key, so the reserved `default` segment does not apply |
| 11 | Module Isolation | ❌ | Module-independent |
| 12 | Audit Trail | ❌ | Too granular for audit; the error carries no value |

---

## Next Steps

1. Commit this plan, open its PR and link it from #655.
2. When #655 is picked up, re-check the choices against today's code (whether #653 and #654 have landed) and ask the maintainer about the Dilemmas (choices 1 and 3 here, answered once in #654, plus choices 2 to 4 of #653).
3. Brief a worker for Phases 1-3 (one PR, 8 files), after #654 is merged.
