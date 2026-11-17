# Implementation Plan: `Guards.TryValidateGreaterThanOrEqual<T>` in Encina.GuardClauses

> **Issue**: [#658](https://github.com/dlrivada/Encina/issues/658)
> **Type**: Feature
> **Complexity**: Low (3 phases, no provider category, 8 files, see Estimated File Count)
> **Estimated Scope**: ~30 lines of production code + ~200 lines of tests (about 280 changed lines in total)

---

## Summary

`Encina.GuardClauses` cannot express an exclusive upper bound ("the index must be below the length", "the percentage must stay below 100", "the timestamp must be before the deadline"). This plan adds `Guards.TryValidateGreaterThanOrEqual<T>(T value, string paramName, T threshold, out EncinaError error, string? message = null) where T : IComparable<T>`. Following the .NET convention written in the issue, the guard **fails when `value >= threshold`** and passes only when `value < threshold`; the name states the condition that triggers the error, like `ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual<T>` (.NET 8, constraint `IComparable<T>`, checked by reflection on 2026-11-17).

This is the first plan of the family guards-2 (#658, #659, #660; #1039 of the same batch is EF Core work and shares nothing). It adds one public method on top of the pieces owned by the family guards-1: #653 (error helper, category constant, no rejected value in the details) and #654 (naming rule, `threshold` key, NaN rule).

- **Standards**: none; .NET parity with `ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual<T>` (since .NET 8) through the Try-pattern of `Guards` (ADR-001 and ADR-006).
- **Affected packages**: `Encina.GuardClauses` only (`Guards.cs`, `PublicAPI.Unshipped.txt`, `README.md`).
- **Provider category**: none. `Guards` is a pure static class, so the matrices of AGENTS.md section 5 do not apply.
- **Scope**: 1 new public method (no new helper), plus tests, README, changelog fragment, `PublicAPI.Unshipped.txt` and the manifest entry: 8 files. Fits one worker PR (at most 15 files, 1500 lines).
- **Prerequisites**: #653, #654, #655, #656 and #657 merged first (helper, category constant, `threshold` argument, `IsNaN<T>`, README family table); the implementing worker stops and reports if the helper or `IsNaN<T>` is missing from `Guards.cs`.

### Shared Pieces and Ownership (family guards-1 and guards-2)

| Shared piece | Owner | Used by |
|--------------|-------|---------|
| `CreateComparisonError`, category constant, error code, no rejected value in the details, `ErrorCategory` | #653 | all |
| Naming rule, `threshold` key, `IsNaN<T>` and the NaN rule | #654 | this plan, #655, #657 |
| Per-file targets of the `Guards.cs` manifest entry | #653 | all |
| Equality semantics of #659 and #660 (constraint, arrays, details) | #659 | #660 |
| This plan adds no shared piece | #658 | none |

Implementation PRs land in the order #653, #654, #655, #656, #657, then #658, #659, #660, one at a time (they edit the same files, so they rebase and never run in parallel).

---

## Design Choices

<details>
<summary><strong>1. Method name: the failing condition or the required condition</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `TryValidateGreaterThanOrEqual`, fails when `value >= threshold` (the issue text)** | One-to-one with `ThrowIfGreaterThanOrEqual`; the acceptance criteria of #658 stay valid | The 12 existing guards name the required condition, so a reader may expect `true` when the value IS greater than or equal |
| **B) `TryValidateLessThan`, same behavior** | Consistent with `TryValidatePositive` and the other existing guards | Not the name of the issue; equal to the name of #654 under option A (see the mapping in #654) |

### Chosen Option: **A — `TryValidateGreaterThanOrEqual` (recommended, pending the maintainer)**

### Rationale

- The decision belongs to the whole family and is owned by #654 (choice 1, with the full mapping of the seven renamed methods); it is answered once and applies here. Option B wins for readers who meet the guard next to `TryValidatePositive`; option A wins where parity with the .NET names is the goal.
- The XML summary starts with "Fails when `value` is greater than or equal to `threshold`".

- **Class:** Dilemma

</details>

<details>
<summary><strong>2. Boundary: is a value equal to the threshold accepted</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Equal fails; only `value < threshold` passes** | Exactly `ThrowIfGreaterThanOrEqual`; "index < length" and "percentage < 100" are the use cases; #655 (`GreaterThan`) is the inclusive sibling | None |
| **B) Equal passes** | Would duplicate #655 | Contradicts the issue and the .NET helper |

### Chosen Option: **A — equal fails**

### Rationale

- The issue states "returns `false` when `value >= threshold`" and the property "returns false iff value >= threshold"; a unit test for `value == threshold` pins it.

- **Class:** Settled
- **Settled by:** #658 acceptance criteria and `ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual<T>` (throws when `value.CompareTo(other) >= 0`)

</details>

<details>
<summary><strong>3. NaN passes an exclusive upper bound unless the guard rejects it</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Pure `IComparable<T>` order** | Same as the .NET helper | `double.NaN.CompareTo(100d)` is `-1` (checked on 2026-11-17), so `TryValidateGreaterThanOrEqual(double.NaN, ..., 100d)` returns `true`: the "percentage below 100" guard accepts NaN. A NaN threshold fails every value (`x.CompareTo(NaN)` is `1`) |
| **B) A NaN value or a NaN threshold fails the guard (`IsNaN<T>` of #654)** | Fail closed in both positions; one rule for the whole family | Differs from the .NET helper for NaN |

### Chosen Option: **B — reject NaN (recommended, pending the maintainer)**

### Rationale

- The decision is owned by #654 (choice 3) and answered once. Option A wins when exact .NET behavior is the contract; option B wins when the bound protects a limit (a percentage, a quota, a deadline) that must never silently disappear, which is every use case of this issue.
- Tests: NaN as value returns `false`; NaN as threshold returns `false`; `NegativeInfinity` passes against a finite threshold and `PositiveInfinity` fails.

- **Class:** Dilemma

</details>

<details>
<summary><strong>4. A null reference value against an upper bound</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Comparer<T>.Default.Compare` as in #654 and #655: a null `string` sorts before any non-null threshold, so it passes `value < threshold`** | Never throws; one comparison path for the family; the behavior is pinned by a test and written in the remarks | A null value passes an upper bound; callers who need presence combine `TryValidateNotNull` |
| **B) A null reference value fails the guard** | Fail closed | A second rule and an extra branch only for reference types; differs from #655, where the same null passes `value <= threshold` |

### Chosen Option: **A — Comparer-based, null passes (as #655)**

### Rationale

- A null value is a missing value, not an exceeded limit; presence is `TryValidateNotNull`'s job (README "Null Checks"). The guard test `TryValidateGreaterThanOrEqual_NullStringValue_ReturnsTrueWithoutThrowing` pins it and the XML remark says it.
- The same rule is already chosen for the sibling upper bound in #655 (choice 5); both guards behave alike.

- **Class:** Trivial
- **Settled by:** AGENTS.md section 3 (ROP: a guard never throws for business logic) and #655 choice 5 (the same comparison, already classed Trivial there)

</details>

<details>
<summary><strong>5. The documented examples: an index needs a lower bound too</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Show the deadline and the percentage cases in the XML example, and show the index case together with `TryValidateNonNegative` (#653) in the README** | `index < Count` alone also accepts `-1` (a value below the threshold passes), so the issue's own example is incomplete; the README shows the correct pair | Two examples instead of one |
| **B) Show only the issue's index example** | Matches the issue text | Teaches a guard that lets `-1` through; `TryValidateInRange(index, 0, count - 1)` is the single-call alternative and would not be mentioned |

### Chosen Option: **A — deadline and percentage in the XML, index plus `TryValidateNonNegative` in the README**

### Rationale

- A unit test calls the guard with `DateTime` values (before and at the deadline) so the documented example is executed code.
- The caller's custom message in the issue interpolates the index; a message is the caller's text and is never logged by the guard (AGENTS.md section 3), so it stays allowed; the details still carry no value (#653 choice 3).

- **Class:** Trivial
- **Settled by:** the use cases listed in #658 ("array index < length", "percentage strictly less than 100", "timestamp before a deadline") and the existing `TryValidateInRange` example style (`Guards.cs:306`)

</details>

<details>
<summary><strong>6. Comparison, details and error</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Comparer<T>.Default.Compare`, details `guard`, `parameter`, `type`, `stage`, `threshold` and no value, code `Guards.GuardValidationFailed`, category constant of #653** | Same as #654: null-safe, one code and one category; the threshold is a bound, not input | None |
| **B) `value.CompareTo(threshold)` and the value in the details** | Same idiom as the file; matches the issue's "actual value" wording | A null reference value throws; repeats the defect of #2108 |

### Chosen Option: **A — as #654 and #653**

### Rationale

- The default message is `"{paramName} must be less than {threshold}."`. The `ErrorCategory` is the one constant of #653 (ADR-062; the guard code keeps one category, decided in #653 choice 4 and the slice #3737).

- **Class:** Trivial
- **Settled by:** AGENTS.md section 3 (ROP, no exceptions for business logic) and #654 choices 2 and 4 (same code path)

</details>

---

## Implementation Phases

### Phase 1: `TryValidateGreaterThanOrEqual`

<details>
<summary>Tasks</summary>

1. `src/Encina.GuardClauses/Guards.cs`: add `public static bool TryValidateGreaterThanOrEqual<T>(T value, string paramName, T threshold, out EncinaError error, string? message = null) where T : IComparable<T>`: when `!IsNaN(value) && !IsNaN(threshold) && Comparer<T>.Default.Compare(value, threshold) < 0` then `error = default; return true;`, otherwise `error = CreateComparisonError("GreaterThanOrEqual", paramName, typeof(T), message ?? $"{paramName} must be less than {threshold}.", threshold)` and `return false`. XML docs: summary starting "Fails when `value` is greater than or equal to `threshold`", `typeparam`, every `param`, `returns`, `remarks` (null and NaN behavior), `example` with the deadline and percentage cases.
2. `src/Encina.GuardClauses/PublicAPI.Unshipped.txt`: add `static Encina.GuardClauses.Guards.TryValidateGreaterThanOrEqual<T>(T value, string! paramName, T threshold, out Encina.EncinaError error, string? message = null) -> bool` (via `mechanical-fixer`, sorted).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina.GuardClauses (src/Encina.GuardClauses/Guards.cs), .NET 10 / C# 14. Issue #658 (plan docs/plans/guard-greater-than-or-equal-implementation-plan-658.md). #653 to #657 are merged: Guards.cs has the category constant, CreateComparisonError(guard, paramName, valueType, message, threshold) and IsNaN<T>.
TASK: Add the public method TryValidateGreaterThanOrEqual<T> described in the plan, Phase 1, following the Chosen Option of every choice as the maintainer answered it (Dilemmas are recorded as recommended, pending the maintainer: re-read the Maintainer Decisions section if it exists; if the naming was answered with option B, use the renamed method TryValidateLessThan of the mapping in #654).
KEY RULES: never throw; ROP; no rejected value in the details; the error code is Guards.GuardValidationFailed; XML docs with example; one PublicAPI.Unshipped.txt line; zero warnings; a unit test must reach every line you add.
REFERENCE FILES: src/Encina.GuardClauses/Guards.cs (the methods added by #653 to #657), src/Encina.GuardClauses/PublicAPI.Unshipped.txt
```

</details>

### Phase 2: Tests

<details>
<summary>Tasks</summary>

1. Unit (`tests/Encina.UnitTests/GuardClauses/GuardsTests.cs`, `#region TryValidateGreaterThanOrEqual Tests`): value below, equal to and above the threshold for `int`, `decimal`, `double`, `DateTime` (the deadline case) and `string` (below returns `true`; equal and above return `false`); NaN as value and as threshold returns `false`; both infinities against a finite threshold; custom message; default message names the parameter and the threshold; error not `default`, code `Guards.GuardValidationFailed`, `GetCategory()` equal to the constant; details hold `guard = "GreaterThanOrEqual"`, `parameter`, `type`, `stage`, `threshold` and no `value`; success leaves `error` as `default`; a negative index passes (documents why the README pairs it with `TryValidateNonNegative`).
2. Guard (`tests/Encina.GuardTests/Validation/GuardClauses/GuardsGuardTests.cs`): `TryValidateGreaterThanOrEqual_NullStringValue_ReturnsTrueWithoutThrowing`, `TryValidateGreaterThanOrEqual_NullStringThreshold_ReturnsFalseWithoutThrowing`, `TryValidateGreaterThanOrEqual_NullMessage_UsesDefaultMessage` (valid result), `TryValidateGreaterThanOrEqual_EmptyParamName_StillReturnsError`, `TryValidateGreaterThanOrEqual_WhitespaceParamName_StillReturnsError`. No `ArgumentNullException` test: guards never validate `paramName` and never throw.
3. Property (`tests/Encina.PropertyTests/Validation/GuardClauses/GuardsPropertyTests.cs`): FsCheck `[Property(MaxTest = 300)]`: for `int` and `decimal` pairs the result is `false` iff `value >= threshold`; for `double` pairs the result is `false` whenever either is NaN; the error is `default` exactly when the result is `true`.
4. No contract, integration, load or benchmark test (pure comparison; justification in the PR description, as in #653).
5. Per-file targets: the entry `Guards.cs` keeps the values owned by #653 (unit 100, guard 100, property per #2043); the worker re-measures and reports.

| File | Unit | Guard | Contract | Property | Integration | One-sentence justification |
|------|------|-------|----------|----------|-------------|----------------------------|
| `Guards.cs` | 100 | 100 | 0 | 87 (owned by #2043) | 0 | The new method has one success and one failure branch, both reached by value-driven unit and guard tests; the property target stays with #2043; contract and integration are 0 because the class has no interface and no external dependency. |

   CRAP of `TryValidateGreaterThanOrEqual`: complexity 4, fully covered, below 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1 is done. Tests for Encina.GuardClauses live in tests/Encina.UnitTests/GuardClauses/GuardsTests.cs, tests/Encina.GuardTests/Validation/GuardClauses/GuardsGuardTests.cs and tests/Encina.PropertyTests/Validation/GuardClauses/GuardsPropertyTests.cs.
TASK: Write the unit, guard and property tests of Phase 2 for TryValidateGreaterThanOrEqual with Shouldly through Encina.Testing.Shouldly; confirm the manifest targets of Guards.cs via mechanical-fixer.
KEY RULES: tests execute the real Guards class; AAA; one behavior per test; no Thread.Sleep; the guard tests include the null-message case as a valid result; assert details through error.GetDetails() and the category through error.GetCategory().
REFERENCE FILES: tests/Encina.UnitTests/GuardClauses/GuardsTests.cs, tests/Encina.GuardTests/Validation/GuardClauses/GuardsGuardTests.cs, tests/Encina.PropertyTests/Validation/GuardClauses/GuardsPropertyTests.cs, .github/coverage-manifest/Encina.GuardClauses.json
```

</details>

### Phase 3: Documentation and finalization

<details>
<summary>Tasks</summary>

1. `src/Encina.GuardClauses/README.md` (via `docs-writer`): add the guard to the "Numeric Validation" block and complete the family table started by #654, with the deadline example and the index example paired with `TryValidateNonNegative`.
2. `changelog.d/658-guards-try-validate-greater-than-or-equal.added.md` (via `mechanical-fixer`).
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
| `ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual<T>` (.NET 8) | Throws when `value >= other`; this guard returns `false` in the same case |
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
| Encina.GuardClauses | none | Guards do not log. The block 750-799 assigned to this family stays unused |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production | 2 | `Guards.cs`, `PublicAPI.Unshipped.txt` |
| Tests | 3 | `GuardsTests.cs`, `GuardsGuardTests.cs`, `GuardsPropertyTests.cs` (modified) |
| Docs and metadata | 3 | `README.md`, `changelog.d/658-guards-try-validate-greater-than-or-equal.added.md`, `.github/coverage-manifest/Encina.GuardClauses.json` (only if a measured value changes) |
| **Total** | **8** | About 280 changed lines; no `ProjectReference`, DI, contract class or test double changes |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina (pre-1.0, .NET 10, C# 14). Issue #658: Guards.TryValidateGreaterThanOrEqual<T> in Encina.GuardClauses (fails when value >= threshold). AGENTS.md rules apply (ROP, no exceptions, zero warnings, per-flag coverage, CRAP <= 10). #653 to #657 are merged first and own the helper, category, threshold key and NaN rule.

IMPLEMENTATION OVERVIEW: (1) public TryValidateGreaterThanOrEqual<T> where T : IComparable<T> using Comparer<T>.Default, IsNaN<T> and CreateComparisonError; (2) unit, guard and FsCheck property tests; (3) README, changelog fragment, PublicAPI line.

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

1. Commit this plan, open its PR and link it from #658.
2. When #658 is picked up, re-check the choices against today's code (whether #653 to #657 have landed) and ask the maintainer about the Dilemmas (choices 1 and 3 here, answered once in #654, plus choices 2 to 4 of #653).
3. Brief a worker for Phases 1-3 (one PR, 8 files), after #657 is merged.
