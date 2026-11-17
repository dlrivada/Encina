# Implementation Plan: `Guards.TryValidateLessThan<T>` in Encina.GuardClauses

> **Issue**: [#654](https://github.com/dlrivada/Encina/issues/654)
> **Type**: Feature
> **Complexity**: Low (3 phases, no provider category, 8 files, see Estimated File Count)
> **Estimated Scope**: ~80 lines of production code + ~220 lines of tests (about 350 changed lines in total)

---

## Summary

`Encina.GuardClauses` has `TryValidateInRange` for bounded checks but no guard for an open-ended lower bound ("the order amount must not be below the minimum", "the customer must be at least 18"). This plan adds `Guards.TryValidateLessThan<T>(T value, string paramName, T threshold, out EncinaError error, string? message = null) where T : IComparable<T>`. Following the .NET convention written in the issue, the guard **fails when `value < threshold`** (it returns `false` with an `EncinaError`) and passes when `value >= threshold`; the name states the condition that triggers the error, like `ArgumentOutOfRangeException.ThrowIfLessThan<T>` (.NET 8).

This is the second plan of the family guards-1 (#653, #654, #655, #656, #657). It depends on the private helper and the category constant that #653 introduces, and it owns the pieces that #655, #657 and the guards-2 plans (#658, #659, #660) reuse: the naming rule, the `threshold` details key and the NaN rule.

- **Standards**: none; .NET parity with `ArgumentOutOfRangeException.ThrowIfLessThan<T>` (since .NET 8) through the Try-pattern of `Guards` (ADR-001 and ADR-006).
- **Affected packages**: `Encina.GuardClauses` only (`Guards.cs`, `PublicAPI.Unshipped.txt`, `README.md`).
- **Provider category**: none. `Guards` is a pure static class: no database, cache, transport, lock or cloud provider is involved, so the matrices of AGENTS.md section 5 do not apply.
- **Scope**: 1 new public method and 2 private helpers, plus tests, README, changelog fragment, `PublicAPI.Unshipped.txt` and the manifest entry: 8 files. Fits one worker PR (at most 15 files, 1500 lines).
- **Prerequisites**: #653 merged first (private `CreateComparisonError`, the category constant, the manifest per-file entry); the implementing worker stops and reports if the helper is missing. Coordinates with #3737, #2108, #2123, #2043 (same as #653).

### Shared Pieces and Ownership (family guards-1 and guards-2)

| Shared piece | Owner | Used by |
|--------------|-------|---------|
| `CreateComparisonError` helper, category constant, error code, no rejected value in the details, `ErrorCategory` (the choices 2 to 4 of the plan of #653) | #653 | all |
| Naming rule (choice 1 below), the `threshold` key of the details (the helper gains an optional `threshold` argument here), `IsNaN<T>` and the NaN rule (choice 3 below) | #654 (this plan) | #655, #657, #658, #659, #660 |
| Zero source of the zero-based guards | #653 | #656 |

Implementation PRs land in the order #653, #654, #655, #656, #657, then guards-2, one at a time.

---

## Design Choices

<details>
<summary><strong>1. Naming rule of the family: the name states the failing condition, or the required condition</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The name states the condition that triggers the error (the issue text): `TryValidateLessThan` fails when `value < threshold`** | Exactly what #654, #655, #657, #658, #659, #660 say and what their .NET equivalents (`ThrowIfLessThan` and siblings) mean; one rule that a developer who knows the .NET helpers already knows; acceptance criteria and test matrices of the six issues stay valid | Opposite of the 12 existing guards, whose names state the required condition (`TryValidatePositive`, `TryValidateNotNull`, `TryValidateInRange` return `true` when the condition holds); `TryValidateNotEqual` would pass when the values ARE equal |
| **B) The name states the required condition** (see the mapping below) | Consistent with every existing guard: `true` always means "the named condition holds" | The six issues change their method names (their acceptance criteria are rewritten); loses the one-to-one link with the .NET helper names |

Mapping of option B (the behavior never changes, only the name): #654 `TryValidateLessThan` becomes `TryValidateGreaterThanOrEqual`; #655 `TryValidateGreaterThan` becomes `TryValidateLessThanOrEqual`; #656 `TryValidateZero` becomes `TryValidateNotZero`; #657 `TryValidateLessThanOrEqual` becomes `TryValidateGreaterThan`; #658 `TryValidateGreaterThanOrEqual` becomes `TryValidateLessThan`; #659 `TryValidateEqual` becomes `TryValidateNotEqual`; #660 `TryValidateNotEqual` becomes `TryValidateEqual`. #653 `TryValidateNonNegative` is the same under both.

### Chosen Option: **A — the name states the failing condition, as the issues say (recommended, pending the maintainer)**

### Rationale

- The family exists to give the .NET 8 throw helpers a ROP form ("Part of ROP guard clause parity with .NET 8-10 built-in throw helpers"); A keeps the names one-to-one with them, and each XML summary starts with "Fails when ..." so the first line of IntelliSense shows the rule.
- Option B wins for a codebase whose readers meet these guards next to `TryValidatePositive` and read `if (!Guards.TryValidateEqual(...))` as "fail unless equal"; option A wins where the parity with the .NET names is the goal of the family, which is the issues' stated goal.
- Because this is a public API name, the answer changes #654 to #660 and the PublicAPI lines; it must be given before the first of them is implemented. This plan and the plans of #655 to #657 are written for option A.

- **Class:** Dilemma

</details>

<details>
<summary><strong>2. How the two values are compared</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `value.CompareTo(threshold)` (as `TryValidateInRange` does, `Guards.cs:315`)** | Same idiom as the file | A `null` value of a reference type (`string`) throws `NullReferenceException`: a guard that throws instead of returning `Left` |
| **B) `Comparer<T>.Default.Compare(value, threshold)`** | Null-safe (null sorts before any non-null value); honors `IComparable<T>`; no boxing for value types | A different idiom from the three existing numeric guards |

### Chosen Option: **B — `Comparer<T>.Default.Compare`**

### Rationale

- The accepted constraint stays `where T : IComparable<T>`, so `DateTime`, `TimeSpan`, `string`, `decimal` and numbers all work.
- A null value compared with a non-null threshold is "less than" it, so `TryValidateLessThan(null, "name", "a", ...)` returns `false` with an error (fail closed) and never throws.

- **Class:** Trivial
- **Settled by:** AGENTS.md section 3 (Railway Oriented Programming: never exceptions for business logic; compliance and security gates fail closed)

</details>

<details>
<summary><strong>3. NaN and the floating-point order</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Pure `IComparable<T>` order** | No extra code; follows the .NET helpers | `double.NaN.CompareTo(x)` is `-1`, so NaN fails the lower bound check but PASSES every upper-bound check (`TryValidateGreaterThan(double.NaN, ..., 100d)` returns `true`): a silent hole in a guard meant to protect a maximum discount or a quota |
| **B) Reject a NaN value or a NaN threshold in every comparison guard** (private `IsNaN<T>` covering `double`, `float` and `Half`) | Fail closed: a NaN never satisfies a bound; one rule for the whole family, and the unit tests state it | Three type checks per call (`value is double` on a generic value type costs nothing after JIT, no boxing); a NaN threshold is a caller bug reported as a guard failure |

### Chosen Option: **B — NaN value or NaN threshold fails the guard (recommended, pending the maintainer)**

### Rationale

- Guards protect domain invariants; a NaN that crosses a "must not exceed" guard defeats it. Option A wins when callers want the exact .NET behavior of `ArgumentOutOfRangeException.ThrowIfGreaterThan` (which also lets NaN through); option B wins everywhere a bound protects money, quantities or limits.
- #653 and #656 reject NaN through `T.IsNaN` (their `INumber<T>` constraint offers it); the helper `IsNaN<T>` of this plan serves the guards that keep `IComparable<T>` (this one, #655, #657, #658) and the equality guards of guards-2 if they adopt it.
- The helper is `private static bool IsNaN<T>(T value)`: `value is double d && double.IsNaN(d) || value is float f && float.IsNaN(f) || value is Half h && Half.IsNaN(h)`.

- **Class:** Dilemma

</details>

<details>
<summary><strong>4. The threshold in the error details</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Details carry the `threshold` the check used (under the key `threshold`)** | A bound is configuration, not input; the issue's acceptance criteria list it; #2108's expected behavior keeps `min` and `max` for `TryValidateInRange` | A threshold computed from personal data would be exposed (none of the issue's examples does that) |
| **B) No threshold in the details** | Minimal metadata | Loses the "semantic metadata" the issue gives as the reason for the guard over `TryValidate(value >= x, ...)` |

### Chosen Option: **A — `threshold` in the details**

### Rationale

- The helper of #653 gains an optional `object? threshold = null`; the key `threshold` is added only when it is not null. Tests of #653 cover the no-threshold branch, the tests of this plan cover the threshold branch.
- The rejected value is not added (choice 3 of #653).

- **Class:** Trivial
- **Settled by:** #654 acceptance criteria ("Metadata includes guard type (`LessThan`), parameter name, actual value, threshold") for the threshold, and #2108 expected behavior ("`min` and `max` for `TryValidateInRange` (the bounds the check used, never the input)")

</details>

---

## Implementation Phases

### Phase 1: Helper extension and `TryValidateLessThan`

<details>
<summary>Tasks</summary>

1. `src/Encina.GuardClauses/Guards.cs`:
   - Extend the helper of #653: `CreateComparisonError(string guard, string paramName, Type valueType, string message, object? threshold = null)`; add `["threshold"] = threshold` when `threshold is not null`.
   - Add `private static bool IsNaN<T>(T value)` (choice 3).
   - Add `public static bool TryValidateLessThan<T>(T value, string paramName, T threshold, out EncinaError error, string? message = null) where T : IComparable<T>`: when `!IsNaN(value) && !IsNaN(threshold) && Comparer<T>.Default.Compare(value, threshold) >= 0` then `error = default; return true;`, otherwise `error = CreateComparisonError("LessThan", paramName, typeof(T), message ?? $"{paramName} must not be less than {threshold}.", threshold)` and `return false`.
   - XML docs: summary starting "Fails when `value` is less than `threshold`", `typeparam`, every `param`, `returns`, `example` with the age case of the issue, and a remark that string comparison is culture-sensitive (`IComparable<string>`).
2. `src/Encina.GuardClauses/PublicAPI.Unshipped.txt`: add `static Encina.GuardClauses.Guards.TryValidateLessThan<T>(T value, string! paramName, T threshold, out Encina.EncinaError error, string? message = null) -> bool` (via `mechanical-fixer`, sorted).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina.GuardClauses (src/Encina.GuardClauses/Guards.cs), .NET 10 / C# 14. Issue #654 (plan docs/plans/guard-less-than-implementation-plan-654.md). The plan of #653 is merged: Guards.cs has the private constant GuardErrorCategory and the private helper CreateComparisonError.
TASK: Extend the helper with the optional threshold argument, add IsNaN<T> and the public method TryValidateLessThan<T> described in the plan, Phase 1, following the Chosen Option of every choice as the maintainer answered it (the plan records Dilemmas as recommended, pending the maintainer: re-read the Maintainer Decisions section if it exists; if choice 1 was answered with option B, use the renamed method of the mapping).
KEY RULES: never throw (null values compare through Comparer<T>.Default); ROP; no rejected value in the details; the error code is Guards.GuardValidationFailed; XML docs with example on every public member; one PublicAPI.Unshipped.txt line; zero warnings; no [Obsolete]; a unit test must reach every line you add.
REFERENCE FILES: src/Encina.GuardClauses/Guards.cs (TryValidateInRange at 312), src/Encina.GuardClauses/PublicAPI.Unshipped.txt, docs/plans/guard-non-negative-implementation-plan-653.md (shared pieces)
```

</details>

### Phase 2: Tests

<details>
<summary>Tasks</summary>

1. Unit (`tests/Encina.UnitTests/GuardClauses/GuardsTests.cs`, `#region TryValidateLessThan Tests`): value below, equal to and above the threshold for `int`, `decimal`, `double`, `DateTime`, `TimeSpan` and `string` (below returns `false`, equal and above return `true`); `double.NaN` as value and as threshold returns `false`; `double.NegativeInfinity` and `double.PositiveInfinity` against a finite threshold; a custom message replaces the default; the default message names the parameter and the threshold; the error is not `default`, has code `Guards.GuardValidationFailed` and `GetCategory()` equal to the constant; details hold `guard = "LessThan"`, `parameter`, `type`, `stage`, `threshold` and no `value`; the success path leaves `error` as `default`.
2. Guard (`tests/Encina.GuardTests/Validation/GuardClauses/GuardsGuardTests.cs`): `TryValidateLessThan_NullStringValue_ReturnsFalseWithoutThrowing`, `TryValidateLessThan_NullStringThreshold_DoesNotThrow` (a null threshold sorts first, so every non-null value passes), `TryValidateLessThan_NullMessage_UsesDefaultMessage` (valid result, not an exception), `TryValidateLessThan_EmptyParamName_StillReturnsError`, `TryValidateLessThan_WhitespaceParamName_StillReturnsError`. There is no `ArgumentNullException` test: guards never validate `paramName` and never throw.
3. Property (`tests/Encina.PropertyTests/Validation/GuardClauses/GuardsPropertyTests.cs`): FsCheck `[Property(MaxTest = 300)]` over `int` and `decimal` pairs: the result is `false` iff `value < threshold`; over `double` pairs generated with NaN: the result is `false` whenever either is NaN; the error is `default` exactly when the result is `true`.
4. No contract, integration, load or benchmark test (a pure comparison on a static class; the PR description records the justification, as in #653).
5. Per-file coverage targets: the entry `Guards.cs` of `.github/coverage-manifest/Encina.GuardClauses.json` keeps the values of #653 (unit 100, guard 100, property per #2043); the worker re-measures and, if the file grew, confirms the same numbers (targets never go down). The implementing worker reports the measured per-flag values.

| File | Unit | Guard | Contract | Property | Integration | One-sentence justification |
|------|------|-------|----------|----------|-------------|----------------------------|
| `Guards.cs` | 100 | 100 | 0 | 87 (owned by #2043) | 0 | Every line of the new method and the two helpers (the NaN branches, the threshold branch and the failure branch) is reached by value-driven unit and guard tests; the property target stays with #2043; contract is 0 because the class implements no interface; integration is 0 because the class has no external dependency. |

   CRAP: `TryValidateLessThan` complexity 4, `IsNaN<T>` complexity 6, both fully covered, below 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1 is done. Tests for Encina.GuardClauses live in tests/Encina.UnitTests/GuardClauses/GuardsTests.cs, tests/Encina.GuardTests/Validation/GuardClauses/GuardsGuardTests.cs and tests/Encina.PropertyTests/Validation/GuardClauses/GuardsPropertyTests.cs.
TASK: Write the unit, guard and property tests of Phase 2 for TryValidateLessThan with Shouldly through Encina.Testing.Shouldly; confirm the manifest targets of Guards.cs via mechanical-fixer.
KEY RULES: tests execute the real Guards class; AAA; one behavior per test; no Thread.Sleep; the guard tests include the null-message case as a valid result and the null string value as a result, not an exception; assert details through error.GetDetails() and the category through error.GetCategory().
REFERENCE FILES: tests/Encina.UnitTests/GuardClauses/GuardsTests.cs, tests/Encina.GuardTests/Validation/GuardClauses/GuardsGuardTests.cs, tests/Encina.PropertyTests/Validation/GuardClauses/GuardsPropertyTests.cs, .github/coverage-manifest/Encina.GuardClauses.json
```

</details>

### Phase 3: Documentation and finalization

<details>
<summary>Tasks</summary>

1. `src/Encina.GuardClauses/README.md` (via `docs-writer`): add the guard to the "Numeric Validation" block with the age example; add (once, here, because this plan owns the rule) a short "Naming of the comparison guards" paragraph stating that `TryValidateLessThan`, `TryValidateGreaterThan` and their siblings fail when their named condition holds, with a table of the eight guards of both families as they land; use the constant `Guards.GuardValidationFailed` in the `Ensure` equivalent, not the literal of the issue.
2. `changelog.d/654-guards-try-validate-less-than.added.md` (via `mechanical-fixer`).
3. `ROADMAP.md`, `docs/INVENTORY.md`, `docs/releases/` and ADRs need no change.
4. Build `dotnet build --configuration Release` (0 warnings), `dotnet test` of the three test projects filtered to `GuardClauses`, `ErrorCategoryCompletenessStaticScanTests`, `--check-justifications`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Phases 1 and 2 are done and green.
TASK: Document the guard and the family naming rule in the Encina.GuardClauses README; add the changelog fragment; run the build and the tests listed in the plan.
KEY RULES: never edit CHANGELOG.md; fragment name <issue>-<slug>.<section>.md; README by docs-writer; fragment and PublicAPI by mechanical-fixer; no hand-typed coverage figures.
REFERENCE FILES: src/Encina.GuardClauses/README.md, changelog.d/README.md, src/Encina.GuardClauses/PublicAPI.Unshipped.txt
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Relevance |
|--------|-----------|
| `ArgumentOutOfRangeException.ThrowIfLessThan<T>` (.NET 8) | Throws when `value < other`; this guard returns `false` in the same case |
| IEEE 754 and `double.CompareTo` | NaN sorts before every number in `CompareTo`; the reason for choice 3 |
| AGENTS.md section 3 | ROP, fail closed |
| ADR-062 | Error category per code (#653 choice 4) |
| #2108, #3737, #233 | Rejected-input details, category slice, error-code contract |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `Guards` | `src/Encina.GuardClauses/Guards.cs` | Hosts the method and helpers |
| `CreateComparisonError`, category constant | added by #653 in `Guards.cs` | Error construction |
| `Guards.GuardValidationFailed` | `src/Encina.GuardClauses/Guards.cs:78` | The error code |
| `TryValidateInRange` | `src/Encina.GuardClauses/Guards.cs:312` | Closest existing guard (idiom and test shape) |
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
| Docs and metadata | 3 | `README.md`, `changelog.d/654-guards-try-validate-less-than.added.md`, `.github/coverage-manifest/Encina.GuardClauses.json` (only if a measured value changes) |
| **Total** | **8** | About 350 changed lines; no `ProjectReference`, DI, contract class or test double changes |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina (pre-1.0, .NET 10, C# 14). Issue #654: Guards.TryValidateLessThan<T> in Encina.GuardClauses (fails when value < threshold). AGENTS.md rules apply (ROP, no exceptions, zero warnings, per-flag coverage, CRAP <= 10). #653 is merged first and owns CreateComparisonError and the category constant; this plan owns the naming rule, the threshold key and the NaN rule.

IMPLEMENTATION OVERVIEW: (1) optional threshold argument on CreateComparisonError, private IsNaN<T>, public TryValidateLessThan<T> where T : IComparable<T> using Comparer<T>.Default; (2) unit, guard and FsCheck property tests; (3) README (with the family naming paragraph), changelog fragment, PublicAPI line.

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

1. Commit this plan, open its PR and link it from #654.
2. When #654 is picked up, re-check the choices against today's code (whether #653 has landed and whether #3737, #2108, #2123 and #2043 have landed) and ask the maintainer about the Dilemmas (choices 1 and 3, plus choices 2 to 4 of #653).
3. Brief a worker for Phases 1-3 (one PR, 8 files), after #653 is merged.
