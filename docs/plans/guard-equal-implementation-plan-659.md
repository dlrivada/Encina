# Implementation Plan: `Guards.TryValidateEqual<T>` in Encina.GuardClauses

> **Issue**: [#659](https://github.com/dlrivada/Encina/issues/659)
> **Type**: Feature
> **Complexity**: Low (3 phases, no provider category, 8 files, see Estimated File Count)
> **Estimated Scope**: ~45 lines of production code + ~220 lines of tests (about 320 changed lines in total)

---

## Summary

`Encina.GuardClauses` cannot reject one forbidden value ("the order must not be `Deleted`", "the setting must not be the known-bad default", "the sentinel must not reach production"). This plan adds `Guards.TryValidateEqual<T>(T value, string paramName, T other, out EncinaError error, string? message = null)`. Following the .NET convention written in the issue, the guard **fails when `value` equals `other`** and passes when they differ; the name states the condition that triggers the error, like `ArgumentOutOfRangeException.ThrowIfEqual<T>` (.NET 8).

This is the second plan of the family guards-2 (#658, #659, #660). It is the first guard of the family that compares for equality instead of order, so it owns the pieces #660 reuses: the generic constraint, how arrays are compared and what the details carry. Its inverse is #660 (`TryValidateNotEqual`).

- **Standards**: none; .NET parity with `ArgumentOutOfRangeException.ThrowIfEqual<T>` (since .NET 8, no generic constraint, checked by reflection on 2026-11-17) through the Try-pattern of `Guards` (ADR-001 and ADR-006).
- **Affected packages**: `Encina.GuardClauses` only (`Guards.cs`, `PublicAPI.Unshipped.txt`, `README.md`).
- **Provider category**: none. `Guards` is a pure static class, so the matrices of AGENTS.md section 5 do not apply.
- **Scope**: 1 new public method and 1 private helper, plus tests, README, changelog fragment, `PublicAPI.Unshipped.txt` and the manifest entry: 8 files. Fits one worker PR (at most 15 files, 1500 lines).
- **Prerequisites**: #653 and #654 merged first (helper `CreateComparisonError`, category constant, naming rule, `IsUnusable<T>`); #658 lands before this plan so the three guards-2 PRs rebase in order; the implementing worker stops and reports if the helper is missing from `Guards.cs`. Relation to #654: the plan uses `IsUnusable<T>` (null or NaN fails closed) on both operands only if choice 5 is answered with option B, as recommended; with option A it deliberately does not. The ADR-062 category of the error is `ErrorCategory.Validation` (recommended, pending the maintainer; #653 choice 4), passed through the named argument `category:` by the helper of #653. The 12 existing `EncinaErrors.Create(GuardValidationFailed, ...)` calls of `Guards.cs` (`:124` to `:506`) carry no `category:` today (pending line, #3737); the new guard relies on #653's helper.

### Shared Pieces and Ownership (family guards-1 and guards-2)

| Shared piece | Owner | Used by |
|--------------|-------|---------|
| `CreateComparisonError`, category constant, error code, no rejected value in the details, `ErrorCategory` | #653 | all |
| Naming rule and its seven-name mapping (D1); `IsUnusable<T>` | #654 | this plan, #658, #660 |
| Per-file targets of the `Guards.cs` manifest entry | #653 | all |
| Equality semantics: generic constraint (choice 2), arrays (choice 3), compared value out of the details (choice 4), private `AreEqual<T>` | #659 (this plan) | #660 |

Implementation PRs land in the order #653 to #657, #658, #659, #660, one at a time.

---

## Design Choices

<details>
<summary><strong>1. Method name: the failing condition or the required condition</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `TryValidateEqual`, fails when `value == other` (the issue text)** | One-to-one with `ThrowIfEqual`; the acceptance criteria of #659 stay valid | The 12 existing guards name the required condition, so `if (!Guards.TryValidateEqual(...))` reads as "fail unless equal" while it means the opposite |
| **B) `TryValidateNotEqual`, same behavior** | Consistent with every existing guard: `true` means "the named condition holds" | Not the name of the issue; it is the name that #660 has under option A, so the names of #659 and #660 swap (see the mapping in #654) |

### Chosen Option: **A — `TryValidateEqual` (recommended, pending the maintainer)**

### Rationale

- The decision belongs to the whole family and is owned by #654 (choice 1, with the full mapping of the seven renamed methods); it is answered once and applies here. Option B wins for readers who meet the guard next to `TryValidatePositive`; option A wins where parity with the .NET names is the goal.
- This pair is where the inversion is easiest to misread, so the XML summary starts with "Fails when `value` equals `other`" and the README table lists both names with their failing condition. When the answer is recorded, #659 and #660 are re-read together: a worker who implements one under option A and the other under option B ships two guards that do the same.

- **Class:** Dilemma

</details>

<details>
<summary><strong>2. Generic constraint and how two values are compared</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `where T : IComparable<T>` and `Comparer<T>.Default.Compare(value, other) == 0` (the issue's signature)** | Same signature as the seven order guards; one constraint in the README table | The issue's own example, `OrderStatus.Deleted`, does not compile: an enum implements neither `IComparable<T>` nor `IEquatable<T>` (checked on `DayOfWeek`, 2026-11-17); `Guid`-less records and `byte[]` are excluded too; "equal" is asked of an order |
| **B) `where T : IEquatable<T>` and `EqualityComparer<T>.Default.Equals`** | Equality is the question asked | Enums are still excluded |
| **C) No constraint, `EqualityComparer<T>.Default.Equals(value, other)`** | The issue's enum example compiles; nulls are handled; same as .NET's `ThrowIfEqual` (unconstrained); works for `string`, `Guid`, enums, records and numbers | The signature differs from the issue; any type is accepted, including one with reference equality |

### Chosen Option: **C — no constraint, `EqualityComparer<T>.Default` (recommended, pending the maintainer)**

### Rationale

- Option A wins when the maintainer wants one signature for every guard of the family and accepts that enum statuses cannot be passed; option C wins for a library whose first use case is a status enum, which is the case the issue shows. Pre-1.0 (AGENTS.md section 1): the best solution, not the compatible one.
- `AreEqual<T>` (choice 3) is the single place that decides equality, so a later change of the rule is one line.
- #660 reuses the same constraint and helper.

- **Class:** Dilemma

</details>

<details>
<summary><strong>3. Arrays and other structurally comparable values</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Plain `EqualityComparer<T>.Default.Equals`: two arrays are equal only when they are the same instance (`byte[]` with equal content is not equal, checked on 2026-11-17)** | Same as the .NET helper; no extra branch | The guard silently passes a forbidden `byte[]` (a row version, a hash, a key) because a copy is never equal; and `TryValidateNotEqual` of #660 reports a conflict for equal row versions, the type `AggregateRoot<TId>.RowVersion` has (`src/Encina.DomainModeling/AggregateRoot.cs:75`) |
| **B) `AreEqual<T>`: an `Array` is compared element by element with `StructuralComparisons.StructuralEqualityComparer`, every other value with `EqualityComparer<T>.Default`** | The row-version and checksum use cases of #660 work; no silent pass for a forbidden array | One extra branch; differs from the .NET helper for arrays; the comparison is not constant-time (see #660 remarks) |
| **C) Only document: callers compare arrays with `TryValidate(a.AsSpan().SequenceEqual(b), ...)`** | No code | The silent pass of option A stays the default behavior |

### Chosen Option: **B — structural comparison for arrays (recommended, pending the maintainer)**

### Rationale

- The failure of option A is silent in this guard (a forbidden array passes) and loud only in #660, so the safe default is B. Option A wins when exact .NET behavior is the contract; option C wins when the guard must stay free of type tests.
- The private helper is `private static bool AreEqual<T>(T left, T right) => left is Array ? StructuralComparisons.StructuralEqualityComparer.Equals(left, right) : EqualityComparer<T>.Default.Equals(left, right);`. The array branch boxes nothing for value types: a value type is never an `Array`.
- If choice 2 is answered with option A or B, an array cannot be passed (arrays implement neither constraint) and this choice has no effect.

- **Class:** Dilemma

</details>

<details>
<summary><strong>4. Does the error carry the forbidden value</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Details hold `parameter`, `type`, `stage`, `guard` only; the default message names the parameter, not the value** | The compared value of an equality check can be a checksum, a token or a status that is personal data; error details are serialized into snapshots (open bug #2108: no rejected input in guard details); the helper of #653 already supports "no threshold" | The issue lists "forbidden value" among the metadata; a developer sees it only in the caller's own variables or in the custom message |
| **B) Details add `forbidden = other`** | Matches the acceptance criteria and the diagnostic value of seeing the sentinel | The value is exposed in every serialization of the error; #654's `threshold` key was justified by "a bound is configuration", which does not hold for an arbitrary equality operand |

### Chosen Option: **A — no compared value in the details or the default message (recommended, pending the maintainer)**

### Rationale

- Option B wins for sentinels and statuses that are plain configuration (`Deleted`, a known-bad default), which is every example of the issue; option A wins as soon as the same method is used to compare a hash, a version token or an identifier, which a public method cannot rule out. A caller who wants the value passes it in `message`, which the guard never logs (AGENTS.md section 3).
- The default message is `"{paramName} must not be equal to the forbidden value."`; no new helper argument is needed, `threshold` is passed as `null`.

- **Class:** Dilemma

</details>

<details>
<summary><strong>5. NaN, nulls and floating-point equality</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No special case: `EqualityComparer<T>.Default` semantics; `double.NaN` equals `double.NaN` (checked on 2026-11-17), a NaN differs from every finite value, two nulls are equal, `(null, "a")` passes** | An equality guard has no ordering hole; a forbidden sentinel can itself be NaN or null; null-safe, never throws; same as the .NET helper | Deviates from the family rule of #654 (choice 2: a null or NaN value or bound fails closed): a NaN or null value passes "must not equal 0"; `NaN == NaN` is `false` under the C# operator but `true` under `Equals` |
| **B) The family rule of #654: `IsUnusable<T>` (null or NaN) on the value or on `other` returns `false` with an error** | One fail-closed rule for every comparison guard of the family (#654 names #659 and #660 as users of `IsUnusable<T>`); a missing or NaN value never passes a gate | A forbidden NaN or null cannot be expressed (use `TryValidateNotNull` or `double.IsNaN` through `TryValidate`); differs from the .NET helper |

### Chosen Option: **B — apply `IsUnusable<T>` (recommended, pending the maintainer)**

### Rationale

- Option A wins when the guard must express a forbidden NaN or null sentinel, or when exact .NET behavior is the contract; option B wins when the guard is a gate and a missing or NaN value must never pass it, which is the rule the family adopts in #654 choice 2. This plan relates to #654 by using `IsUnusable<T>` on both operands.
- Tests under B: `(NaN, NaN)`, `(NaN, 1.0)`, `(1.0, NaN)`, `(null, null)`, `(null, "a")` and `("a", null)` all return `false`; under A the tests flip to `false`, `true`, `true`, `false`, `true`, `true`.
- Validity of the value beyond that (a negative quantity) stays with the ordering guards.

- **Class:** Dilemma

</details>

<details>
<summary><strong>6. Error code of the guard</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reuse `Guards.GuardValidationFailed` (`Encina.guard.validation_failed`), the one code every existing guard returns** | All 12 existing methods create it (`Guards.cs:78`, `:124`, `:158`, ...); a consumer matches one code | A consumer cannot tell this guard from another by code (the details carry `guard = "Equal"`) |
| **B) A new code per guard** | Distinguishable by code | Breaks the single-code convention of the file; the error-code contract is #233's |

### Chosen Option: **A — the existing code**

### Rationale

- The error is built through the helper of #653 and carries its `ErrorCategory` (`ErrorCategory.Validation`, recommended, pending the maintainer; #653 choice 4); those are #653's decisions, not decided here. The 12 existing `EncinaErrors.Create(GuardValidationFailed, ...)` calls (`Guards.cs:124` to `:506`) carry no `category:` today (pending line, #3737); the new guard does not copy them and relies on #653's helper. A unit test asserts `error.GetCategory()` against the constant of #653.

- **Class:** Trivial
- **Settled by:** `src/Encina.GuardClauses/Guards.cs:78` and the 12 existing `EncinaErrors.Create(GuardValidationFailed, ...)` calls (`Guards.cs:124` to `:506`)

</details>

---

## Implementation Phases

### Phase 1: `AreEqual<T>` and `TryValidateEqual`

<details>
<summary>Tasks</summary>

1. `src/Encina.GuardClauses/Guards.cs`:
   - Add `private static bool AreEqual<T>(T left, T right)` (choices 2 and 3).
   - Add `public static bool TryValidateEqual<T>(T value, string paramName, T other, out EncinaError error, string? message = null)` (with `where T : IComparable<T>` only if choice 2 is answered with option A): when `!IsUnusable(value) && !IsUnusable(other) && !AreEqual(value, other)` (the `IsUnusable` terms only if choice 5 is answered with option B) then `error = default; return true;`, otherwise `error = CreateComparisonError("Equal", paramName, typeof(T), message ?? $"{paramName} must not be equal to the forbidden value.", threshold: null)` and `return false`.
   - XML docs: summary starting "Fails when `value` equals `other`", `typeparam`, every `param`, `returns`, `remarks` (NaN, null and array behavior; not for secrets), `example` with the `OrderStatus.Deleted` case of the issue.
2. `src/Encina.GuardClauses/PublicAPI.Unshipped.txt`: add `static Encina.GuardClauses.Guards.TryValidateEqual<T>(T value, string! paramName, T other, out Encina.EncinaError error, string? message = null) -> bool` (via `mechanical-fixer`, sorted). The PublicAPI format does not list generic constraints, so the line is the same under every answer to choice 2.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina.GuardClauses (src/Encina.GuardClauses/Guards.cs), .NET 10 / C# 14. Issue #659 (plan docs/plans/guard-equal-implementation-plan-659.md). #653 and #654 are merged: Guards.cs has the category constant, IsUnusable<T> and CreateComparisonError(guard, paramName, valueType, message, threshold = null).
TASK: Add the private AreEqual<T> and the public method TryValidateEqual<T> described in the plan, Phase 1, following the Chosen Option of every choice as the maintainer answered it (Dilemmas are recorded as recommended, pending the maintainer: re-read the Maintainer Decisions section if it exists; if the naming was answered with option B, the method is TryValidateNotEqual and #660 takes the name TryValidateEqual: implement the behavior of THIS plan under the new name).
KEY RULES: never throw; ROP; no compared value in the details or the default message unless choice 4 was answered with option B; the error code is Guards.GuardValidationFailed; XML docs with example; one PublicAPI.Unshipped.txt line; zero warnings; a unit test must reach every line you add.
REFERENCE FILES: src/Encina.GuardClauses/Guards.cs (the helper added by #653), src/Encina.GuardClauses/PublicAPI.Unshipped.txt
```

</details>

### Phase 2: Tests

<details>
<summary>Tasks</summary>

1. Unit (`tests/Encina.UnitTests/GuardClauses/GuardsTests.cs`, `#region TryValidateEqual Tests`): different and equal values for `int`, `string`, `Guid`, a test `enum` (the `OrderStatus` case), a `record` and `DateTime`; `(NaN, NaN)`, `(NaN, 1.0)`, `(1.0, NaN)`, `(null, null)`, `(null, "a")` and `("a", null)` return `false` (choice 5, option B); two `byte[]` copies with equal content return `false`, with different content return `true` (choice 3, option B); custom message; default message names the parameter and not the value; error not `default`, code `Guards.GuardValidationFailed`, `GetCategory()` equal to the constant; details hold `guard = "Equal"`, `parameter`, `type`, `stage` and neither `value` nor `forbidden`; success leaves `error` as `default`.
2. Guard (`tests/Encina.GuardTests/Validation/GuardClauses/GuardsGuardTests.cs`): `TryValidateEqual_NullValueAndNullOther_ReturnsFalseWithoutThrowing`, `TryValidateEqual_NullValueNonNullOther_ReturnsFalseWithoutThrowing`, `TryValidateEqual_NonNullValueNullOther_ReturnsFalseWithoutThrowing`, `TryValidateEqual_NullMessage_UsesDefaultMessage` (valid result), `TryValidateEqual_EmptyParamName_StillReturnsError`, `TryValidateEqual_WhitespaceParamName_StillReturnsError`. No `ArgumentNullException` test: guards never validate `paramName` and never throw.
3. Property (`tests/Encina.PropertyTests/Validation/GuardClauses/GuardsPropertyTests.cs`): FsCheck `[Property(MaxTest = 300)]`: for `int`, non-null `string` and `Guid` pairs the result is `false` iff `value == other`, and for any pair with a null or NaN operand it is `false` (option B); for `byte[]` pairs the result is `false` iff the sequences are equal; the error is `default` exactly when the result is `true`.
4. No contract, integration, load or benchmark test (pure comparison; justification in the PR description, as in #653).
5. Per-file targets: the entry `Guards.cs` is created by #653 (unit 100, guard 100, no property value); the worker re-measures and reports.

| File | Unit | Guard | Contract | Property | Integration | One-sentence justification |
|------|------|-------|----------|----------|-------------|----------------------------|
| `Guards.cs` | 100 | 100 | 0 | not set here (owned by #2043) | 0 | The new method and `AreEqual<T>` have two branches each, all reached by value-driven unit and guard tests; no per-file property target is written because the file's measured property coverage is 111/164 = 67.68% (#2043), so 87 is unreachable by this PR, and the new methods' own lines are covered by their FsCheck properties; contract and integration are 0 because the class has no interface and no external dependency. |

   CRAP: `TryValidateEqual` complexity 3 and `AreEqual<T>` complexity 3 (the `is Array` test plus the conditional), fully covered, both below 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1 is done. Tests for Encina.GuardClauses live in tests/Encina.UnitTests/GuardClauses/GuardsTests.cs, tests/Encina.GuardTests/Validation/GuardClauses/GuardsGuardTests.cs and tests/Encina.PropertyTests/Validation/GuardClauses/GuardsPropertyTests.cs.
TASK: Write the unit, guard and property tests of Phase 2 for TryValidateEqual with Shouldly through Encina.Testing.Shouldly; confirm the manifest targets of Guards.cs via mechanical-fixer.
KEY RULES: tests execute the real Guards class; AAA; one behavior per test; no Thread.Sleep; the guard tests include the null-message case as a valid result; assert details through error.GetDetails() and the category through error.GetCategory(); the array tests follow choice 3 as answered.
REFERENCE FILES: tests/Encina.UnitTests/GuardClauses/GuardsTests.cs, tests/Encina.GuardTests/Validation/GuardClauses/GuardsGuardTests.cs, tests/Encina.PropertyTests/Validation/GuardClauses/GuardsPropertyTests.cs, .github/coverage-manifest/Encina.GuardClauses.json
```

</details>

### Phase 3: Documentation and finalization

<details>
<summary>Tasks</summary>

1. `src/Encina.GuardClauses/README.md` (via `docs-writer`): add the guard to the "Numeric Validation" block (or a new "Equality" block) and complete the family table started by #654, with the status enum example and a sentence that the guard is not for comparing secrets.
2. `changelog.d/659-guards-try-validate-equal.added.md` (via `mechanical-fixer`).
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
| `ArgumentOutOfRangeException.ThrowIfEqual<T>` (.NET 8) | Throws when `value` equals `other`; no generic constraint; this guard returns `false` in the same case |
| `EqualityComparer<T>.Default` and `StructuralComparisons` (.NET) | Equality semantics of choices 2, 3 and 5 |
| AGENTS.md section 3 | ROP, fail closed; secrets and personal data stay out of diagnostics |
| ADR-062 | Error category per code (#653 choice 4) |
| #2108, #3737 | Rejected-input details, category slice |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `Guards` | `src/Encina.GuardClauses/Guards.cs` | Hosts the method and `AreEqual<T>` |
| `CreateComparisonError`, category constant | added by #653 in `Guards.cs` | Error construction |
| `Guards.GuardValidationFailed` | `src/Encina.GuardClauses/Guards.cs:78` | The error code |
| `AggregateRoot<TId>.RowVersion` (`byte[]?`) | `src/Encina.DomainModeling/AggregateRoot.cs:75` | The array case of choice 3 |
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
| Docs and metadata | 3 | `README.md`, `changelog.d/659-guards-try-validate-equal.added.md`, `.github/coverage-manifest/Encina.GuardClauses.json` (only if a measured value changes) |
| **Total** | **8** | About 320 changed lines; no `ProjectReference`, DI, contract class or test double changes |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina (pre-1.0, .NET 10, C# 14). Issue #659: Guards.TryValidateEqual<T> in Encina.GuardClauses (fails when value equals other). AGENTS.md rules apply (ROP, no exceptions, zero warnings, per-flag coverage, CRAP <= 10). #653 and #654 are merged first and own the helper, category and naming rule; #658 lands before this plan.

IMPLEMENTATION OVERVIEW: (1) private AreEqual<T> (EqualityComparer, structural for arrays) and public TryValidateEqual<T> using CreateComparisonError without a compared value; (2) unit, guard and FsCheck property tests; (3) README, changelog fragment, PublicAPI line.

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
| 12 | Audit Trail | ❌ | Too granular for audit; the error carries no compared value |

---

## Next Steps

1. Commit this plan, open its PR and link it from #659.
2. When #659 is picked up, re-check the choices against today's code (whether #653, #654 and #658 have landed) and ask the maintainer about the Dilemmas (choice 1 answered once in #654, choices 2 to 4 here; they also decide #660).
3. Brief a worker for Phases 1-3 (one PR, 8 files), after #658 is merged.
