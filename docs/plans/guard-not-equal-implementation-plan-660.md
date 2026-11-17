# Implementation Plan: `Guards.TryValidateNotEqual<T>` in Encina.GuardClauses

> **Issue**: [#660](https://github.com/dlrivada/Encina/issues/660)
> **Type**: Feature
> **Complexity**: Low (3 phases, no provider category, 8 files, see Estimated File Count)
> **Estimated Scope**: ~25 lines of production code + ~200 lines of tests (about 280 changed lines in total)

---

## Summary

`Encina.GuardClauses` cannot require that a value equals an expected one ("the entity version must be the version the caller loaded", "the computed checksum must match", "the protocol version must be the supported one"). This plan adds `Guards.TryValidateNotEqual<T>(T value, string paramName, T expected, out EncinaError error, string? message = null)`. Following the .NET convention written in the issue, the guard **fails when `value` differs from `expected`** and passes only when they are equal; the name states the condition that triggers the error, like `ArgumentOutOfRangeException.ThrowIfNotEqual<T>` (.NET 8, no generic constraint, checked by reflection on 2026-11-17).

This is the third and last guard plan of the family guards-2 (#658, #659, #660). It is the inverse of #659 and reuses everything #659 owns: the generic constraint, the private `AreEqual<T>` helper and the rule that the compared value stays out of the details.

- **Standards**: none; .NET parity with `ArgumentOutOfRangeException.ThrowIfNotEqual<T>` (since .NET 8) through the Try-pattern of `Guards` (ADR-001 and ADR-006).
- **Affected packages**: `Encina.GuardClauses` only (`Guards.cs`, `PublicAPI.Unshipped.txt`, `README.md`).
- **Provider category**: none. `Guards` is a pure static class, so the matrices of AGENTS.md section 5 do not apply.
- **Scope**: 1 new public method (no new helper), plus tests, README, changelog fragment, `PublicAPI.Unshipped.txt` and the manifest entry: 8 files. Fits one worker PR (at most 15 files, 1500 lines).
- **Prerequisites**: #653, #654 and #659 merged first (helper, category constant, `IsUnusable<T>`, `AreEqual<T>`); the implementing worker stops and reports if `AreEqual<T>` is missing from `Guards.cs`. Relation to #654: `IsUnusable<T>` is applied on both operands if choice 5 is answered with option B (recommended) and deliberately not applied with option A, which keeps `Equals` semantics for an expected null or NaN. The ADR-062 category of the error is `ErrorCategory.Validation` (recommended, pending the maintainer; #653 choice 4), passed through the named argument `category:` by the helper of #653. The 12 existing `EncinaErrors.Create(GuardValidationFailed, ...)` calls of `Guards.cs` (`:124` to `:506`) carry no `category:` today (pending line, #3737); the new guard relies on #653's helper.

### Shared Pieces and Ownership (family guards-1 and guards-2)

| Shared piece | Owner | Used by |
|--------------|-------|---------|
| `CreateComparisonError`, category constant, error code, no rejected value in the details, `ErrorCategory` | #653 | all |
| Naming rule and its seven-name mapping (D1); `IsUnusable<T>` | #654 | this plan, #658, #659 |
| Per-file targets of the `Guards.cs` manifest entry | #653 | all |
| Equality semantics: generic constraint, arrays, compared value out of the details, private `AreEqual<T>` | #659 | this plan |
| This plan adds no shared piece | #660 | none |

Implementation PRs land in the order #653 to #657, #658, #659, #660, one at a time.

---

## Design Choices

<details>
<summary><strong>1. Method name: the failing condition or the required condition</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `TryValidateNotEqual`, fails when `value != expected` (the issue text)** | One-to-one with `ThrowIfNotEqual`; the acceptance criteria of #660 stay valid | The name reads as "valid when not equal" while the guard passes only when the values ARE equal: the opposite of every existing guard name |
| **B) `TryValidateEqual`, same behavior** | Consistent with every existing guard: `true` means "the named condition holds" | Not the name of the issue; it is the name that #659 has under option A, so the names of #659 and #660 swap (see the mapping in #654) |

### Chosen Option: **A — `TryValidateNotEqual` (recommended, pending the maintainer)**

### Rationale

- The decision belongs to the whole family and is owned by #654 (choice 1, with the full mapping of the seven renamed methods); it is answered once and applies here. Option B wins for readers who meet the guard next to `TryValidatePositive` (`if (!Guards.TryValidateEqual(version, ..., expected, ...))` reads as "fail unless equal", which is what it does); option A wins where parity with the .NET names is the goal.
- This is the most counter-intuitive name of the family, so the XML summary starts with "Fails when `value` differs from `expected`".
- When the answer is recorded, #659 and #660 are read together so that both are implemented under the same rule.

- **Class:** Dilemma

</details>

<details>
<summary><strong>2. Generic constraint, comparison and arrays</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `where T : IComparable<T>` as in the issue** | Same signature as the order guards | The enum and `byte[]` cases do not compile (see #659 choice 2); `AggregateRoot<TId>.RowVersion` is a `byte[]?` (`src/Encina.DomainModeling/AggregateRoot.cs:75`) |
| **B) No constraint, `AreEqual<T>` of #659 (structural for arrays, `EqualityComparer<T>.Default` otherwise)** | The optimistic-concurrency use case works with `int`, `long`, `Guid`, `string` and the `byte[]` row version; same as .NET's unconstrained helper for everything but arrays | The signature differs from the issue; arrays are compared by content, unlike the .NET helper |

### Chosen Option: **B — as #659 (recommended, pending the maintainer)**

### Rationale

- The decision is owned by #659 (choices 2 and 3) and answered once. Here the cost of a wrong answer is higher than in #659: with plain `EqualityComparer<T>.Default` on a `byte[]`, `TryValidateNotEqual(entity.RowVersion, ..., expectedRowVersion, ...)` would report a conflict for two equal row versions every time, the exact use case the issue names ("version matches expected version (optimistic concurrency)").
- Option A wins when one signature for the whole family matters more than the enum and array cases.

- **Class:** Dilemma

</details>

<details>
<summary><strong>3. Does the error carry the expected value</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Details hold `parameter`, `type`, `stage`, `guard` only; the default message names the parameter, not the values** | The expected value of a match check is often a checksum, a token or a version of a record of a person; error details are serialized into snapshots (open bug #2108) | The issue lists "expected value" among the metadata; the diagnostic value is lost unless the caller adds it to `message` |
| **B) Details add `expected`** | Matches the acceptance criteria; a version conflict shows both versions | Exposes the compared value in every serialization of the error |

### Chosen Option: **A — no expected value in the details or the default message (recommended, pending the maintainer)**

### Rationale

- The decision is owned by #659 (choice 4) and answered once. Option B wins for protocol and version numbers that are plain configuration; option A wins when the same method compares a hash or a token.
- The issue's own example puts both versions in the caller's custom message (`$"Concurrency conflict: expected version {expectedVersion}, got {entity.Version}"`), which is the caller's text and stays possible. The default message is `"{paramName} does not match the expected value."`.

- **Class:** Dilemma

</details>

<details>
<summary><strong>4. The guard is not a secret comparison</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Ordinary comparison; the remarks and the README say that tokens, MACs and passwords are compared with `CryptographicOperations.FixedTimeEquals`** | The guard checks a domain invariant (the README: "defensive programming against bugs"), not authentication; the helper stays one line; .NET's helper is not constant-time either | A caller can still pass a secret and leak timing information |
| **B) Constant-time comparison for `byte[]` and `string`** | Safe for secrets by accident | A different algorithm for two types, more branches and a false sense of security (the error still says which parameter failed, and a `string` comparison of different lengths cannot be constant-time) |

### Chosen Option: **A — ordinary comparison, documented**

### Rationale

- Authentication comparisons belong to the security packages; a guard that returns a `Left` for the caller to pass on is the wrong place for them.
- A remark in the XML docs and a sentence in the README carry the warning; no test can pin timing.

- **Class:** Trivial
- **Settled by:** AGENTS.md section 3 (every line serves a current purpose: a second, constant-time algorithm serves no use case of #660, which lists a version, a checksum and a protocol version) and the .NET API `CryptographicOperations.FixedTimeEquals` for the case it is meant for

</details>

<details>
<summary><strong>5. NaN, nulls and the documented example</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Equals` semantics as in the Dilemma of #659 (two NaNs are equal, two nulls are equal, a NaN differs from every finite value)** | The expected value can itself be null or NaN; same as the .NET helper; the tests execute the documented example (`long` and the `byte[]` row version) | Fail-open for a version gate: an unloaded (null) version equals a null expected version and `(NaN, NaN)` passes, contrary to the family rule of #654 choice 2 |
| **B) The family rule of #654: `IsUnusable<T>` (null or NaN) on the value or on `expected` returns `false` with an error** | A match gate never passes on a missing or NaN value; one fail-closed rule for the family (#654 names #659 and #660 as users) | A match against an expected null or NaN cannot be expressed (use `TryValidateNotNull` or `TryValidate(condition, ...)`); differs from the .NET helper |

### Chosen Option: **B — apply `IsUnusable<T>` (recommended, pending the maintainer)**

### Rationale

- The decision is owned by #659 (choice 5) and answered once; it is repeated here because a match check is where the fail-open reading hurts most (a token the caller never loaded must not match). Option A wins when the expected value can legitimately be null or NaN; option B wins for a gate. This plan relates to #654 by using `IsUnusable<T>` on both operands if the answer is B, and deliberately not using it if the answer is A.
- Tests under B: `(NaN, NaN)`, `(NaN, 1.0)`, `(null, null)`, `(null, "a")` and `("a", null)` return `false`; two equal `byte[]` copies return `true` (#659 choice 3, option B); the tests flip for the null and NaN pairs under A (`true`, `false`, `true`, `false`, `false`).

- **Class:** Dilemma

</details>

<details>
<summary><strong>6. Error code of the guard</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reuse `Guards.GuardValidationFailed` (`Encina.guard.validation_failed`), the one code every existing guard returns** | All 12 existing methods create it (`Guards.cs:78`, `:124`, `:158`, ...); a consumer matches one code | A caller cannot tell a version conflict from another guard failure by code (the details carry `guard = "NotEqual"`) |
| **B) A dedicated code such as `Encina.guard.concurrency_conflict` for the version use case** | A caller can tell a conflict from another guard failure | Breaks the single-code convention; #233 owns the error-code contract; a conflict is a domain error the caller maps itself |

### Chosen Option: **A — the existing code**

### Rationale

- The error is built through the helper of #653 and carries its `ErrorCategory` (`ErrorCategory.Validation`, recommended, pending the maintainer; #653 choice 4); those are #653's decisions, not decided here. The 12 existing `EncinaErrors.Create(GuardValidationFailed, ...)` calls (`Guards.cs:124` to `:506`) carry no `category:` today (pending line, #3737); the new guard does not copy them and relies on #653's helper. A unit test asserts `error.GetCategory()` against the constant of #653.
- A caller that needs a typed conflict builds its own `EncinaError` from the `Left` or uses `TryValidate(condition, ...)`.

- **Class:** Trivial
- **Settled by:** `src/Encina.GuardClauses/Guards.cs:78` and the 12 existing `EncinaErrors.Create(GuardValidationFailed, ...)` calls (`Guards.cs:124` to `:506`)

</details>

---

## Implementation Phases

### Phase 1: `TryValidateNotEqual`

<details>
<summary>Tasks</summary>

1. `src/Encina.GuardClauses/Guards.cs`: add `public static bool TryValidateNotEqual<T>(T value, string paramName, T expected, out EncinaError error, string? message = null)` (with `where T : IComparable<T>` only if #659 choice 2 is answered with option A): when `!IsUnusable(value) && !IsUnusable(expected) && AreEqual(value, expected)` (the `IsUnusable` terms only if choice 5 is answered with option B) then `error = default; return true;`, otherwise `error = CreateComparisonError("NotEqual", paramName, typeof(T), message ?? $"{paramName} does not match the expected value.", threshold: null)` and `return false`. XML docs: summary starting "Fails when `value` differs from `expected`", `typeparam`, every `param`, `returns`, `remarks` (NaN, null and array behavior; not for secrets, use `CryptographicOperations.FixedTimeEquals`), `example` with the optimistic-concurrency version.
2. `src/Encina.GuardClauses/PublicAPI.Unshipped.txt`: add `static Encina.GuardClauses.Guards.TryValidateNotEqual<T>(T value, string! paramName, T expected, out Encina.EncinaError error, string? message = null) -> bool` (via `mechanical-fixer`, sorted).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina.GuardClauses (src/Encina.GuardClauses/Guards.cs), .NET 10 / C# 14. Issue #660 (plan docs/plans/guard-not-equal-implementation-plan-660.md). #653, #654 and #659 are merged: Guards.cs has the category constant, CreateComparisonError(guard, paramName, valueType, message, threshold = null) and the private AreEqual<T>.
TASK: Add the public method TryValidateNotEqual<T> described in the plan, Phase 1, following the Chosen Option of every choice as the maintainer answered it (Dilemmas are recorded as recommended, pending the maintainer: re-read the Maintainer Decisions section if it exists; if the naming was answered with option B, the method is TryValidateEqual and #659 takes the name TryValidateNotEqual: implement the behavior of THIS plan, which passes only when the values are equal, under the new name).
KEY RULES: never throw; ROP; no compared value in the details or the default message unless #659 choice 4 was answered with option B; the error code is Guards.GuardValidationFailed; XML docs with example and the "not for secrets" remark; one PublicAPI.Unshipped.txt line; zero warnings; a unit test must reach every line you add.
REFERENCE FILES: src/Encina.GuardClauses/Guards.cs (the helper added by #653 and AreEqual<T> added by #659), src/Encina.GuardClauses/PublicAPI.Unshipped.txt
```

</details>

### Phase 2: Tests

<details>
<summary>Tasks</summary>

1. Unit (`tests/Encina.UnitTests/GuardClauses/GuardsTests.cs`, `#region TryValidateNotEqual Tests`): equal and different values for `int`, `long` (the version case), `string`, `Guid`, a test `enum` and a `record`; two `byte[]` copies with equal content return `true`, with different content return `false` (the row-version case, #659 choice 3 option B); `(NaN, NaN)`, `(NaN, 1.0)`, `(null, null)`, `(null, "a")` and `("a", null)` return `false` (choice 5, option B); custom message; default message names the parameter and not the values; error not `default`, code `Guards.GuardValidationFailed`, `GetCategory()` equal to the constant; details hold `guard = "NotEqual"`, `parameter`, `type`, `stage` and neither `value` nor `expected`; success leaves `error` as `default`.
2. Guard (`tests/Encina.GuardTests/Validation/GuardClauses/GuardsGuardTests.cs`): `TryValidateNotEqual_NullValueAndNullExpected_ReturnsFalseWithoutThrowing`, `TryValidateNotEqual_NonNullValueNullExpected_ReturnsFalseWithoutThrowing`, `TryValidateNotEqual_NullValueNonNullExpected_ReturnsFalseWithoutThrowing`, `TryValidateNotEqual_NullMessage_UsesDefaultMessage` (valid result), `TryValidateNotEqual_EmptyParamName_StillReturnsError`, `TryValidateNotEqual_WhitespaceParamName_StillReturnsError`. No `ArgumentNullException` test: guards never validate `paramName` and never throw.
3. Property (`tests/Encina.PropertyTests/Validation/GuardClauses/GuardsPropertyTests.cs`): FsCheck `[Property(MaxTest = 300)]`: for `int`, non-null `string` and `Guid` pairs the result is `false` iff `value != expected`; for `byte[]` pairs the result is `true` iff the sequences are equal (`SequenceEqual` is the oracle, not #659's method, which is not on `main` when this plan is written); for any pair with a null or NaN operand the result is `false` (option B); the error is `default` exactly when the result is `true`.
4. No contract, integration, load or benchmark test (pure comparison; justification in the PR description, as in #653).
5. Per-file targets: the entry `Guards.cs` is created by #653 (unit 100, guard 100, no property value); the worker re-measures and reports.

| File | Unit | Guard | Contract | Property | Integration | One-sentence justification |
|------|------|-------|----------|----------|-------------|----------------------------|
| `Guards.cs` | 100 | 100 | 0 | not set here (owned by #2043) | 0 | The new method has one success and one failure branch on top of the helper tested by #659, both reached by value-driven unit and guard tests; no per-file property target is written because the file's measured property coverage is 111/164 = 67.68% (#2043), so 87 is unreachable by this PR; contract and integration are 0 because the class has no interface and no external dependency. |

   CRAP of `TryValidateNotEqual`: complexity 3, fully covered, below 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1 is done. Tests for Encina.GuardClauses live in tests/Encina.UnitTests/GuardClauses/GuardsTests.cs, tests/Encina.GuardTests/Validation/GuardClauses/GuardsGuardTests.cs and tests/Encina.PropertyTests/Validation/GuardClauses/GuardsPropertyTests.cs.
TASK: Write the unit, guard and property tests of Phase 2 for TryValidateNotEqual with Shouldly through Encina.Testing.Shouldly; confirm the manifest targets of Guards.cs via mechanical-fixer.
KEY RULES: tests execute the real Guards class; AAA; one behavior per test; no Thread.Sleep; the guard tests include the null-message case as a valid result; assert details through error.GetDetails() and the category through error.GetCategory(); the array tests follow #659 choice 3 as answered.
REFERENCE FILES: tests/Encina.UnitTests/GuardClauses/GuardsTests.cs, tests/Encina.GuardTests/Validation/GuardClauses/GuardsGuardTests.cs, tests/Encina.PropertyTests/Validation/GuardClauses/GuardsPropertyTests.cs, .github/coverage-manifest/Encina.GuardClauses.json
```

</details>

### Phase 3: Documentation and finalization

<details>
<summary>Tasks</summary>

1. `src/Encina.GuardClauses/README.md` (via `docs-writer`): add the guard to the equality block started by #659 and complete the family table, with the optimistic-concurrency example and the "not for secrets" sentence.
2. `changelog.d/660-guards-try-validate-not-equal.added.md` (via `mechanical-fixer`).
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
| `ArgumentOutOfRangeException.ThrowIfNotEqual<T>` (.NET 8) | Throws when `value` differs from `other`; no generic constraint; this guard returns `false` in the same case |
| `EqualityComparer<T>.Default` and `StructuralComparisons` (.NET) | Equality semantics of choices 2 and 5 (owned by #659) |
| `CryptographicOperations.FixedTimeEquals` (.NET) | The comparison for secrets named in choice 4 |
| AGENTS.md section 3 | ROP, fail closed; secrets and personal data stay out of diagnostics |
| ADR-062 | Error category per code (#653 choice 4) |
| #2108, #3737, #233 | Rejected-input details, category slice, error-code contract |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `Guards` | `src/Encina.GuardClauses/Guards.cs` | Hosts the method |
| `CreateComparisonError`, category constant | added by #653 in `Guards.cs` | Error construction |
| `AreEqual<T>` | added by #659 in `Guards.cs` | Equality decision |
| `Guards.GuardValidationFailed` | `src/Encina.GuardClauses/Guards.cs:78` | The error code |
| `AggregateRoot<TId>.RowVersion` (`byte[]?`) | `src/Encina.DomainModeling/AggregateRoot.cs:75` | The array case of the concurrency example |
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
| Docs and metadata | 3 | `README.md`, `changelog.d/660-guards-try-validate-not-equal.added.md`, `.github/coverage-manifest/Encina.GuardClauses.json` (only if a measured value changes) |
| **Total** | **8** | About 280 changed lines; no `ProjectReference`, DI, contract class or test double changes |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina (pre-1.0, .NET 10, C# 14). Issue #660: Guards.TryValidateNotEqual<T> in Encina.GuardClauses (fails when value differs from expected). AGENTS.md rules apply (ROP, no exceptions, zero warnings, per-flag coverage, CRAP <= 10). #653, #654 and #659 are merged first and own the helper, category, naming rule and AreEqual<T>.

IMPLEMENTATION OVERVIEW: (1) public TryValidateNotEqual<T> using AreEqual<T> and CreateComparisonError without a compared value; (2) unit, guard and FsCheck property tests; (3) README, changelog fragment, PublicAPI line.

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
| 7 | Distributed Locks | ❌ | No shared state; an optimistic-concurrency check needs none |
| 8 | Transactions | ❌ | No state change |
| 9 | Idempotency | ❌ | Pure function |
| 10 | Multi-Tenancy | ❌ | Tenant-independent: no tenant id and no tenant-qualified key, so the reserved `default` segment does not apply |
| 11 | Module Isolation | ❌ | Module-independent |
| 12 | Audit Trail | ❌ | Too granular for audit; the error carries no compared value |

---

## Next Steps

1. Commit this plan, open its PR and link it from #660.
2. When #660 is picked up, re-check the choices against today's code (whether #653, #654 and #659 have landed) and ask the maintainer about the Dilemmas (choice 1 answered once in #654, choices 2 and 3 answered once in #659).
3. Brief a worker for Phases 1-3 (one PR, 8 files), after #659 is merged.
