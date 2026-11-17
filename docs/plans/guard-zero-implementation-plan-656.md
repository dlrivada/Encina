# Implementation Plan: `Guards.TryValidateZero<T>` in Encina.GuardClauses

> **Issue**: [#656](https://github.com/dlrivada/Encina/issues/656)
> **Type**: Feature
> **Complexity**: Low (3 phases, no provider category, 8 files, see Estimated File Count)
> **Estimated Scope**: ~25 lines of production code + ~180 lines of tests (about 260 changed lines in total)

---

## Summary

`Encina.GuardClauses` cannot reject a zero divisor, a zero pricing multiplier or a zero scaling factor with semantic metadata. This plan adds `Guards.TryValidateZero<T>(T value, string paramName, out EncinaError error, string? message = null)`. Following the .NET convention written in the issue, the guard **fails when `value` equals zero** (it returns `false` with an `EncinaError`) and passes for every non-zero value, positive or negative; it is the ROP equivalent of `ArgumentOutOfRangeException.ThrowIfZero<T>` (.NET 8).

This is the fourth plan of the family guards-1 (#653, #654, #655, #656, #657). It depends on the helper, the category constant and the zero source owned by #653, and on the naming rule and the NaN rule owned by #654.

- **Standards**: none; .NET parity with `ArgumentOutOfRangeException.ThrowIfZero<T>` (since .NET 8) through the Try-pattern of `Guards` (ADR-001 and ADR-006).
- **Affected packages**: `Encina.GuardClauses` only (`Guards.cs`, `PublicAPI.Unshipped.txt`, `README.md`).
- **Provider category**: none. `Guards` is a pure static class, so the matrices of AGENTS.md section 5 do not apply.
- **Scope**: 1 new public method (no new helper), plus tests, README, changelog fragment, `PublicAPI.Unshipped.txt` and the manifest entry: 8 files. Fits one worker PR (at most 15 files, 1500 lines).
- **Prerequisites**: #653 merged first (helper, category constant, constraint decision); #654 merged first when the NaN rule is wanted in the same shape (the NaN test below). The implementing worker stops and reports if they are missing.

### Shared Pieces and Ownership (family guards-1 and guards-2)

| Shared piece | Owner | Used by |
|--------------|-------|---------|
| `CreateComparisonError`, category constant, error code, no rejected value in the details, `ErrorCategory`, zero source and generic constraint | #653 | this plan and all other guards |
| Naming rule, `threshold` key, `IsNaN<T>` and the NaN rule | #654 | this plan (NaN only), #655, #657, guards-2 |
| This plan adds no shared piece | #656 | none |

Implementation PRs land in the order #653, #654, #655, #656, #657, then guards-2, one at a time.

---

## Design Choices

<details>
<summary><strong>1. Method name: the failing condition or the required condition</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `TryValidateZero`, fails when the value IS zero (the issue text)** | One-to-one with `ThrowIfZero`; the acceptance criteria of #656 stay valid | The most counterintuitive name of the family: `TryValidateZero(x)` returning `false` for `0` reads as "validate that x is zero" |
| **B) `TryValidateNotZero`, same behavior** | States the required condition, like `TryValidateNotNull` and `TryValidateNotEmpty` | Not the name of the issue |

### Chosen Option: **A — `TryValidateZero` (recommended, pending the maintainer)**

### Rationale

- The naming rule is owned by #654 (choice 1, with the full mapping) and answered once for the family. Option B wins for readers who skim `if (!Guards.TryValidateZero(divisor, ...))` and expect the opposite; option A wins where parity with the .NET names is the goal, as the issues state.
- This guard is the strongest argument for option B in the family; the XML summary starts with "Fails when `value` is zero" to limit the risk under option A.

- **Class:** Dilemma

</details>

<details>
<summary><strong>2. Zero source and generic constraint</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `where T : IComparable<T>` and `Convert.ChangeType(0, typeof(T))`** | Signature of the issue; same idiom as `TryValidatePositive` | `Convert.ChangeType` throws for `TimeSpan` and other non-`IConvertible` types |
| **B) `where T : IComparable<T>` and `default(T)`** | No exception | Meaningless answer for `string`, `DateTime`, `char` |
| **C) `where T : INumber<T>` and `T.Zero`** | Compile-time safety; exact zero for `double`, `decimal`, `int`, `BigInteger`, `Half`; `T.IsNaN` available | Changes the constraint written in the issue; `TimeSpan` is rejected at compile time |

### Chosen Option: **C — `where T : INumber<T>`, zero is `T.Zero` (recommended, pending the maintainer)**

### Rationale

- The decision is owned by #653 (choice 2) and answered once; this guard and `TryValidateNonNegative` are the only two that compare with zero. Option A wins when all zero-based guards must keep one idiom; option B wins when `TimeSpan` must stay valid; option C wins for numeric use cases, which are all the use cases of the issue (divisor, multiplier, scaling factor).

- **Class:** Dilemma

</details>

<details>
<summary><strong>3. Equality test and NaN</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Fail when `value == T.Zero`, and also when `T.IsNaN(value)`** | `-0.0`, `0.0` and `0.00m` are all zero; a NaN divisor is as bad as a zero divisor and would pass a pure equality test; fail closed | Differs from `ThrowIfZero` for NaN |
| **B) Fail only when `value == T.Zero`** | Exactly the .NET helper | `TryValidateZero(double.NaN, ...)` returns `true`, so a NaN divisor gets through the guard whose purpose is to protect a division |

### Chosen Option: **A — zero and NaN fail (recommended, pending the maintainer)**

### Rationale

- The NaN rule is owned by #654 (choice 3) and answered once; this plan uses `T.IsNaN`, available through `INumber<T>`, instead of the `IsNaN<T>` helper (no dependency on #654's code, only on its answer). Option B wins when exact .NET behavior is the contract; option A wins for divisors and multipliers.
- Tests: `0`, `0L`, `0m`, `0.00m`, `0d`, `-0d` fail; `1`, `-1`, `double.Epsilon`, `double.PositiveInfinity`, `double.NegativeInfinity` pass; `double.NaN` fails.

- **Class:** Dilemma

</details>

<details>
<summary><strong>4. Details, error code and category</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `CreateComparisonError("Zero", paramName, typeof(T), message)`: details `parameter`, `type`, `stage`, `guard`, no value and no threshold; code `Guards.GuardValidationFailed`; category constant of #653** | One construction path and one category for every guard; no rejected value (open bug #2108); the scan of ADR-062 never sees one code with two categories | The issue's "actual value" metadata is not provided |
| **B) A dedicated code `guard.zero`** | Distinguishable | #233 owns the error-code contract; one code per guard breaks the file's convention |

### Chosen Option: **A — as #653**

### Rationale

- The zero is implicit in the guard kind, so a threshold key adds nothing. The default message is `"{paramName} must not be zero."`. The category question (choice 4 of #653, coordinated with #3737) applies unchanged, and a unit test asserts `GetCategory()`.

- **Class:** Trivial
- **Settled by:** the convention of `src/Encina.GuardClauses/Guards.cs` (one code for all guards, `Guards.cs:78`) and #653 choices 3 and 4 (same code path)

</details>

---

## Implementation Phases

### Phase 1: `TryValidateZero`

<details>
<summary>Tasks</summary>

1. `src/Encina.GuardClauses/Guards.cs`: add `public static bool TryValidateZero<T>(T value, string paramName, out EncinaError error, string? message = null) where T : INumber<T>`: when `!T.IsNaN(value) && value != T.Zero` then `error = default; return true;`, otherwise `error = CreateComparisonError("Zero", paramName, typeof(T), message ?? $"{paramName} must not be zero.")` and `return false`. XML docs: summary starting "Fails when `value` is zero", `typeparam`, every `param`, `returns`, `example` with the divisor case of the issue.
2. `src/Encina.GuardClauses/PublicAPI.Unshipped.txt`: add `static Encina.GuardClauses.Guards.TryValidateZero<T>(T value, string! paramName, out Encina.EncinaError error, string? message = null) -> bool` (via `mechanical-fixer`, sorted).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina.GuardClauses (src/Encina.GuardClauses/Guards.cs), .NET 10 / C# 14. Issue #656 (plan docs/plans/guard-zero-implementation-plan-656.md). #653 is merged: Guards.cs has GuardErrorCategory and CreateComparisonError, and TryValidateNonNegative<T> uses where T : INumber<T> with T.Zero.
TASK: Add the public method TryValidateZero<T> described in the plan, Phase 1, following the Chosen Option of every choice as the maintainer answered it (Dilemmas are recorded as recommended, pending the maintainer: re-read the Maintainer Decisions section if it exists; if the naming was answered with option B, use TryValidateNotZero; if the constraint was answered with option A or B, use that zero source as in TryValidateNonNegative).
KEY RULES: never throw; ROP; no rejected value in the details; the error code is Guards.GuardValidationFailed; XML docs with example; one PublicAPI.Unshipped.txt line; zero warnings; a unit test must reach every line you add.
REFERENCE FILES: src/Encina.GuardClauses/Guards.cs (the method added by #653), src/Encina.GuardClauses/PublicAPI.Unshipped.txt
```

</details>

### Phase 2: Tests

<details>
<summary>Tasks</summary>

1. Unit (`tests/Encina.UnitTests/GuardClauses/GuardsTests.cs`, `#region TryValidateZero Tests`): zero for `int`, `long`, `decimal` (`0m` and `0.00m`), `double` (`0d` and `-0d`), `float` returns `false`; positive, negative, `double.Epsilon` and both infinities return `true`; `double.NaN` returns `false`; custom message; default message names the parameter; error not `default`, code `Guards.GuardValidationFailed`, `GetCategory()` equal to the constant; details hold `guard = "Zero"`, `parameter`, `type`, `stage` and neither `value` nor `threshold`; success leaves `error` as `default`.
2. Guard (`tests/Encina.GuardTests/Validation/GuardClauses/GuardsGuardTests.cs`): `TryValidateZero_NullMessage_UsesDefaultMessage` (valid result, not an exception), `TryValidateZero_EmptyParamName_StillReturnsError`, `TryValidateZero_WhitespaceParamName_StillReturnsError`, `TryValidateZero_NaN_ReturnsFalse`. No `ArgumentNullException` test: guards never validate `paramName` and never throw.
3. Property (`tests/Encina.PropertyTests/Validation/GuardClauses/GuardsPropertyTests.cs`): FsCheck `[Property(MaxTest = 300)]` over `int`, `decimal` and `double`: the result is `false` iff `value == 0` or the value is NaN; the error is `default` exactly when the result is `true`.
4. No contract, integration, load or benchmark test (pure comparison; justification in the PR description, as in #653).
5. Per-file targets: the entry `Guards.cs` keeps the values owned by #653 (unit 100, guard 100, property per #2043); the worker re-measures and reports.

| File | Unit | Guard | Contract | Property | Integration | One-sentence justification |
|------|------|-------|----------|----------|-------------|----------------------------|
| `Guards.cs` | 100 | 100 | 0 | 87 (owned by #2043) | 0 | The new method has one success and one failure branch, both reached by value-driven unit and guard tests; the property target stays with #2043; contract and integration are 0 because the class has no interface and no external dependency. |

   CRAP of `TryValidateZero`: complexity 3, fully covered, below 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1 is done. Tests for Encina.GuardClauses live in tests/Encina.UnitTests/GuardClauses/GuardsTests.cs, tests/Encina.GuardTests/Validation/GuardClauses/GuardsGuardTests.cs and tests/Encina.PropertyTests/Validation/GuardClauses/GuardsPropertyTests.cs.
TASK: Write the unit, guard and property tests of Phase 2 for TryValidateZero with Shouldly through Encina.Testing.Shouldly; confirm the manifest targets of Guards.cs via mechanical-fixer.
KEY RULES: tests execute the real Guards class; AAA; one behavior per test; no Thread.Sleep; the guard tests include the null-message case as a valid result; assert details through error.GetDetails() and the category through error.GetCategory().
REFERENCE FILES: tests/Encina.UnitTests/GuardClauses/GuardsTests.cs, tests/Encina.GuardTests/Validation/GuardClauses/GuardsGuardTests.cs, tests/Encina.PropertyTests/Validation/GuardClauses/GuardsPropertyTests.cs, .github/coverage-manifest/Encina.GuardClauses.json
```

</details>

### Phase 3: Documentation and finalization

<details>
<summary>Tasks</summary>

1. `src/Encina.GuardClauses/README.md` (via `docs-writer`): add the guard to the "Numeric Validation" block and the family table started by #654, with the divisor example and the note that the guard fails on zero (the one name of the family whose reading is the reverse of its behavior).
2. `changelog.d/656-guards-try-validate-zero.added.md` (via `mechanical-fixer`).
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
| `ArgumentOutOfRangeException.ThrowIfZero<T>` (.NET 8) | Throws when the value is zero; this guard returns `false` in the same case |
| IEEE 754 | `-0.0 == 0.0` and NaN is neither: the reason for choice 3 |
| AGENTS.md section 3 | ROP, fail closed |
| ADR-062 | Error category per code (#653 choice 4) |
| #2108, #3737, #233 | Rejected-input details, category slice, error-code contract |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `Guards` | `src/Encina.GuardClauses/Guards.cs` | Hosts the method |
| `CreateComparisonError`, category constant | added by #653 in `Guards.cs` | Error construction |
| `TryValidateNonNegative<T>` | added by #653 in `Guards.cs` | Same `INumber<T>` shape |
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
| Docs and metadata | 3 | `README.md`, `changelog.d/656-guards-try-validate-zero.added.md`, `.github/coverage-manifest/Encina.GuardClauses.json` (only if a measured value changes) |
| **Total** | **8** | About 260 changed lines; no `ProjectReference`, DI, contract class or test double changes |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina (pre-1.0, .NET 10, C# 14). Issue #656: Guards.TryValidateZero<T> in Encina.GuardClauses (fails when value is zero). AGENTS.md rules apply (ROP, no exceptions, zero warnings, per-flag coverage, CRAP <= 10). #653 is merged first and owns the helper, the category and the zero source.

IMPLEMENTATION OVERVIEW: (1) public TryValidateZero<T> where T : INumber<T> failing on T.Zero and NaN, using CreateComparisonError; (2) unit, guard and FsCheck property tests; (3) README, changelog fragment, PublicAPI line.

KEY PATTERNS: bool Try-pattern with out EncinaError; details keys parameter/type/stage/guard, never the value; specialists for PublicAPI, changelog and manifest lines; docs-writer for the README.

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

1. Commit this plan, open its PR and link it from #656.
2. When #656 is picked up, re-check the choices against today's code (whether #653 and #654 have landed) and ask the maintainer about the Dilemmas (choices 1 to 3 here, answered once in #653 and #654).
3. Brief a worker for Phases 1-3 (one PR, 8 files), after #653 is merged.
