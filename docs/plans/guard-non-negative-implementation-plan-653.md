# Implementation Plan: `Guards.TryValidateNonNegative<T>` in Encina.GuardClauses

> **Issue**: [#653](https://github.com/dlrivada/Encina/issues/653)
> **Type**: Feature
> **Complexity**: Low (3 phases, no provider category, 8 files, see Estimated File Count)
> **Estimated Scope**: ~60 lines of production code + ~200 lines of tests (about 300 changed lines in total)

---

## Summary

`Encina.GuardClauses` has `TryValidatePositive` (value > 0) and `TryValidateNegative` (value < 0) but no guard for "zero is valid, negative is not" (account balance, discount percentage, stock after a decrement). This plan adds `Guards.TryValidateNonNegative<T>(T value, string paramName, out EncinaError error, string? message = null)`: it returns `true` when `value >= 0`, otherwise `false` with an `EncinaError` that carries the guard kind `NonNegative`, the parameter name and the value type. It is the ROP equivalent of `ArgumentOutOfRangeException.ThrowIfNegative<T>` (.NET 8).

This is the first plan of the family guards-1 (#653, #654, #655, #656, #657: ROP equivalents of the .NET 8 comparison throw helpers) and it owns the pieces the others reuse (see Shared Pieces and Ownership).

- **Standards**: none; .NET parity with `ArgumentOutOfRangeException.ThrowIfNegative<T>` (since .NET 8), exposed through the Try-pattern of `Guards` (ADR-001 and ADR-006, Railway Oriented Programming).
- **Affected packages**: `Encina.GuardClauses` only (`Guards.cs`, `PublicAPI.Unshipped.txt`, `README.md`).
- **Provider category**: none. `Guards` is a pure static class: no database, cache, transport, lock or cloud provider is involved, so the 10-database matrix and the specialized categories of AGENTS.md section 5 do not apply.
- **Scope**: 1 new public method, 1 private helper, plus tests, README, changelog fragment, `PublicAPI.Unshipped.txt` and the manifest entry: 8 files (the exact table is under Estimated File Count). Fits one worker PR (at most 15 files, 1500 lines).
- **Prerequisites**: none blocking. Open issues this plan coordinates with: #3737 (slice P-Misc of the ADR-062 migration declares the `ErrorCategory` of the existing guards), #2108 (bug: the existing guards copy the rejected input into the error details), #2123 and #2043 (per-file coverage targets of `Guards.cs`).

### Shared Pieces and Ownership (family guards-1 and guards-2)

The five plans of this family and the three of guards-2 that add guards (#658, #659, #660; #1039 is EF Core work and shares nothing) edit the same files: `Guards.cs`, `PublicAPI.Unshipped.txt`, `README.md`, the three `GuardClauses` test files and the manifest entry. They must land one after another (rebase, never in parallel) in this order: #653, #654, #655, #656, #657, then guards-2.

| Shared piece | Owner | Used by |
|--------------|-------|---------|
| Private helper `CreateComparisonError` (builds the error with the keys `parameter`, `type`, `stage`, `guard`) and the private constant that holds the error category | #653 (this plan) | all eight guards of both families |
| Error code `Guards.GuardValidationFailed` (`Encina.guard.validation_failed`), reused unchanged | existing (`Guards.cs:78`) | all |
| Zero source and generic constraint of the zero-based guards (choice 2) | #653 | #656 |
| Whether the rejected value goes into the details (choice 3) | #653 | all |
| `ErrorCategory` of the guard error (choice 4) | #653, coordinated with the slice #3737 | all |
| Naming rule "the name states the failing condition" (D1), the `threshold` metadata key, the NaN rule and the helpers that implement them | #654 | #655, #657 and guards-2 |
| Manifest entry `Guards.cs` per-file `targets` (unit 100, guard 100) when still absent | #653 (or whichever of #653, #2123 lands first) | all |

---

## Design Choices

<details>
<summary><strong>1. Method name and semantics</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `TryValidateNonNegative<T>`, returns `true` when `value >= 0`** | Matches the issue title and signature; the name states the required condition exactly as `TryValidatePositive`, `TryValidateNegative`, `TryValidateNotNull` and `TryValidateInRange` do (`Guards.cs:108-312`); equal to the .NET helper's accepted set | None |
| **B) `TryValidateNotNegative`** | Reads like `TryValidateNotNull` | Not the name of the issue; "non-negative" is the standard mathematical term |

### Chosen Option: **A — `TryValidateNonNegative`**

### Rationale

- The issue fixes the name and the contract ("returns `true` for values >= 0, `false` for negative values"). Under both naming rules discussed in #654 (D1) this guard keeps the same name, because its name already states the required condition.
- The guard kind written in the error details is `NonNegative`, as the acceptance criteria say.

- **Class:** Trivial
- **Settled by:** #653 acceptance criteria and the existing naming of `TryValidatePositive` and `TryValidateNegative` (`src/Encina.GuardClauses/Guards.cs:240`, `:272`)

</details>

<details>
<summary><strong>2. Zero source and generic constraint</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `where T : IComparable<T>` and `Convert.ChangeType(0, typeof(T))`, as `TryValidatePositive` and `TryValidateNegative` do (`Guards.cs:243`, `:275`)** | Same signature as the issue and as the two siblings; one idiom in the file | `Convert.ChangeType` throws `InvalidCastException` for `TimeSpan`, `DateTime`, `Guid` and any non-`IConvertible` type, and a guard must not throw (AGENTS.md section 3, ROP); runtime failure instead of compile error |
| **B) `where T : IComparable<T>` and `default(T)` as zero** | No exception; right for every numeric primitive, `decimal` and `TimeSpan` | Silently meaningless for `string`, `DateTime`, `char`: compiles and gives an answer that is not a zero test |
| **C) `where T : INumber<T>` and `T.Zero`** | Compile-time safety: only numbers are accepted; zero is exact (`double`, `decimal`, `int`, `BigInteger`, `Half`); `INumber<T>` includes `IComparable<T>`, so call sites with numeric arguments compile unchanged; `T.IsNaN` is available | Changes the constraint written in the issue; `TimeSpan` is rejected at compile time (the issue's own example for a sibling guard already converts a `TimeSpan` to `double`); differs from `TryValidatePositive` until a follow-up aligns it |

### Chosen Option: **C — `where T : INumber<T>`, zero is `T.Zero` (recommended, pending the maintainer)**

### Rationale

- Pre-1.0 rule (AGENTS.md section 1): the best solution, not the compatible one. C is the only option that cannot throw and cannot answer a meaningless question.
- Option A wins when the maintainer wants all zero-based guards to keep one idiom and the same constraint, accepting the exception for `TimeSpan`. Option B wins when `TimeSpan` and other `IComparable<T>` structs must stay valid arguments. Option C wins for a library whose callers pass numbers, which is every use case of the issue (balance, percentage, quantity).
- The same choice applies to #656 (`TryValidateZero`), the only other guard that compares with zero. Aligning the two existing guards (`TryValidatePositive`, `TryValidateNegative`) is a separate follow-up (issue file `guards-numeric-zero-source` written with this plan).

- **Class:** Dilemma

</details>

<details>
<summary><strong>3. Does the error details carry the rejected value?</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Details hold `parameter`, `type`, `stage`, `guard` only** | The value of a balance, an age or a quantity can be personal or financial data; error details are serialized by `EncinaErrorConverter` into snapshots (open bug #2108 says no rejected input belongs in guard details) | The issue's acceptance criterion lists "actual value" among the metadata; a developer debugging sees the value only in the caller's own variables |
| **B) Details also hold `value`, as the existing numeric guards do today (`Guards.cs:255`, `:287`, `:326`)** | Matches the issue text and the siblings | Repeats the defect #2108 reports; the fix of #2108 would have to touch the new guard too |

### Chosen Option: **A — no rejected value in the details (recommended, pending the maintainer)**

### Rationale

- Bounds that the check used (`threshold` in #654, #655, #657) are not input and stay; the input itself does not.
- Option B wins when the guards run only in non-production diagnostics where the value is needed to reproduce a failure; option A wins everywhere the error can reach a log, a snapshot or a client, which is the default (AGENTS.md section 3: `EncinaError.Message` and similar never reach logs; the same care applies to details).
- The helper takes no value parameter, so a later decision for B is a one-line change in one place.

- **Class:** Dilemma

</details>

<details>
<summary><strong>4. Error code and ErrorCategory</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reuse `Guards.GuardValidationFailed` and the category `Validation`** | One code for every guard (all 12 existing methods create it, `Guards.cs:124`, `:158`, ...); a code is never created with two categories, which the completeness scan of ADR-062 enforces; `Validation` maps to HTTP 400 and gRPC `INVALID_ARGUMENT` (R3: field rules are `Validation`) | A guard that signals a programming error is reported as a client error |
| **B) Reuse the code with the category `Internal`** | Guards protect domain invariants and detect bugs (README: "defensive programming against bugs"); maps to 500 | A guard that rejects a caller-supplied value answers 500 |
| **C) New code per guard (`guard.non_negative`)** | Distinguishable by code | Breaks the single-code convention; any consumer that matches the code must learn eight more; #233 owns the error-code contract and decides whether guards get codes of their own |

### Chosen Option: **A — code `Encina.guard.validation_failed`, category `Validation` (recommended, pending the maintainer)**

### Rationale

- The code is Trivial (the convention of the file). The category is the Dilemma: the migration slice #3737 (P-Misc) decides the category of the code `Encina.guard.validation_failed` for the existing guards, and the new guards must carry the same category (one code, one category). This plan therefore keeps the category in one private constant read by `CreateComparisonError`, so the outcome of #3737 changes one line.
- Until #3737 lands, the pending line `src/Encina.GuardClauses/ => P-GuardClauses` of `tests/Encina.UnitTests/Core/ErrorCategoryPending.txt` covers the new call; if #3737 lands first, the helper's `EncinaErrors.Create(code, category, ...)` call already satisfies the scan (the named `category:` argument is used).
- Option B wins when guards are used for invariants of already-validated state (the README says guards are not input validation); option A wins when the result of a guard is returned to the caller of a handler (the README examples return the guard's `Left`).
- A unit test asserts `error.GetCategory()` against the constant for the new guard.

- **Class:** Dilemma

</details>

<details>
<summary><strong>5. Where the shared error construction lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Private static `CreateComparisonError` in `Guards.cs`, used by the new guards only** | One place for the keys and the category of the eight new guards; no behavior change for the 12 existing methods | The existing methods keep their copy-pasted dictionaries until #3737 and #2108 touch them |
| **B) Refactor all existing methods onto the helper now** | One construction path | Larger diff; mixes a refactor with the feature; touches the lines #2108 and #3737 change (merge conflicts) |
| **C) Inline the dictionary in each new method (the file's current style)** | Zero new abstraction | Eight more copies of the dictionary and of the category decision |

### Chosen Option: **A — private helper for the new guards**

### Rationale

- The helper has one branch-free body, so CRAP stays under 10 for it and for the method.
- #3737 and #2108 can then fix the existing methods without touching the new ones.

- **Class:** Trivial
- **Settled by:** AGENTS.md section 3 ("Every line serves a current purpose"; no duplicated compatibility layers) and the existing pattern of `private const MetadataKey*` fields in `Guards.cs:81-84`

</details>

---

## Implementation Phases

### Phase 1: Helper and `TryValidateNonNegative`

<details>
<summary>Tasks</summary>

1. `src/Encina.GuardClauses/Guards.cs`:
   - Add `private const ErrorCategory GuardErrorCategory = ErrorCategory.Validation;` (the value follows the maintainer's answer to choice 4) with a comment naming #3737.
   - Add `private static EncinaError CreateComparisonError(string guard, string paramName, Type valueType, string message)`: builds `Dictionary<string, object?>` with `parameter`, `type` (`valueType.FullName`), `stage` (`guard_validation`) and `guard`, and returns `EncinaErrors.Create(GuardValidationFailed, GuardErrorCategory, message, details: metadata)`. It takes no value (choice 3).
   - Add `public static bool TryValidateNonNegative<T>(T value, string paramName, out EncinaError error, string? message = null) where T : INumber<T>`: if `!T.IsNaN(value) && value >= T.Zero` then `error = default; return true;`, otherwise `error = CreateComparisonError("NonNegative", paramName, typeof(T), message ?? $"{paramName} must be non-negative (zero or greater).")` and `return false`. `-0.0` is non-negative (it equals zero); `NaN` is rejected (fail closed, see #654 choice on NaN).
   - XML docs: `summary`, `typeparam`, every `param`, `returns`, `example` with the balance example of the issue, and a `remarks` line "Fails when the value is below zero or is NaN".
   - `using System.Numerics;` at the top of the file.
2. `src/Encina.GuardClauses/PublicAPI.Unshipped.txt`: add `static Encina.GuardClauses.Guards.TryValidateNonNegative<T>(T value, string! paramName, out Encina.EncinaError error, string? message = null) -> bool` (via `mechanical-fixer` when implementing; keep the file sorted).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina.GuardClauses (src/Encina.GuardClauses/Guards.cs), .NET 10 / C# 14. Issue #653 (plan docs/plans/guard-non-negative-implementation-plan-653.md). The class is a static Try-pattern guard: returns bool, error through `out EncinaError`.
TASK: Add the private constant GuardErrorCategory, the private helper CreateComparisonError and the public method TryValidateNonNegative<T> described in the plan, Phase 1, following the Chosen Option of choices 2, 3 and 4 as the maintainer answered them (the plan records them as recommended, pending the maintainer: re-read the plan's Maintainer Decisions section if it exists).
KEY RULES: never throw; ROP; no rejected value in the details (choice 3); the error code is Guards.GuardValidationFailed; XML docs with example on every public member; one PublicAPI.Unshipped.txt line; zero warnings; no [Obsolete]; a unit test must reach every line you add.
REFERENCE FILES: src/Encina.GuardClauses/Guards.cs (TryValidatePositive at 240, TryValidateInRange at 312), src/Encina.GuardClauses/PublicAPI.Unshipped.txt, src/Encina/Errors/ErrorCategory.cs, docs/architecture/adr/062-error-category-contract.md
```

</details>

### Phase 2: Tests

<details>
<summary>Tasks</summary>

1. Unit (`tests/Encina.UnitTests/GuardClauses/GuardsTests.cs`, new `#region TryValidateNonNegative Tests`): positive, zero, `-0.0`, negative for `int`, `long`, `decimal`, `double`, `float`; `int.MinValue`, `double.NegativeInfinity`, `double.NaN` return `false`; a custom message replaces the default; the default message names the parameter; the error is not `default` and has code `Guards.GuardValidationFailed`; `error.GetCategory()` equals the category constant; details contain `guard = "NonNegative"`, `parameter`, `type` and `stage`, and contain no key `value` (choice 3); success path leaves `error` as `default`.
2. Guard (`tests/Encina.GuardTests/Validation/GuardClauses/GuardsGuardTests.cs`): `TryValidateNonNegative_NullMessage_UsesDefaultMessage` (a nullable parameter that accepts null gets the valid result, not an exception), `TryValidateNonNegative_EmptyParamName_StillReturnsError`, `TryValidateNonNegative_WhitespaceParamName_StillReturnsError`, `TryValidateNonNegative_NaN_ReturnsFalse`, `TryValidateNonNegative_NegativeInfinity_ReturnsFalse`. `Guards` does not validate `paramName` (no existing guard does, and a guard must not throw), so there is no `ArgumentNullException` test: the plan states it so a reviewer does not look for one.
3. Property (`tests/Encina.PropertyTests/Validation/GuardClauses/GuardsPropertyTests.cs`): FsCheck `[Property(MaxTest = 300)]` with `int`, `decimal` and `double` generators: the result is `true` iff `value >= 0` (and not NaN), the error is `default` on `true` and not `default` on `false`, repeated calls are idempotent.
4. No contract test (a static class, no interface), no integration test (pure function; `GuardsIntegrationTests` already drives the guards through a real handler and DI, and gains no new branch), no load or benchmark test (a comparison and a dictionary allocation, no concurrency, no shared state): the PR description records the three justifications; no `.md` folder is added because no load or benchmark folder for `GuardClauses` exists.
5. Per-file coverage targets (manifest `.github/coverage-manifest/Encina.GuardClauses.json`, entry `Guards.cs`, via `mechanical-fixer`). The entry has no per-file `targets` today (open issues #2123 and #2043 add them); the implementing worker measures the baseline with the commands of AGENTS.md section 9 and these numbers are the floor:

| File | Unit | Guard | Contract | Property | Integration | One-sentence justification |
|------|------|-------|----------|----------|-------------|----------------------------|
| `Guards.cs` | 100 | 100 | 0 | 87 (value owned by #2043, kept as is) | 0 | A pure static class whose every line (the new helper and method included) is reached by value-driven unit and guard tests; property stays at the package key that #2043 raises with generated cases; contract is 0 because the class implements no interface; integration is 0 because `GuardsIntegrationTests` only re-drives existing guards. |

   If #2123 has landed, the worker keeps its values (targets never go down). `dotnet run --file .github/scripts/coverage-report.cs -- --check-justifications` must pass, and the PR reports the measured per-flag values of `Guards.cs` against this table. CRAP of `TryValidateNonNegative` and `CreateComparisonError`: complexity 2 and 1, fully covered, far below 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1 is done. Tests for Encina.GuardClauses live in tests/Encina.UnitTests/GuardClauses/GuardsTests.cs, tests/Encina.GuardTests/Validation/GuardClauses/GuardsGuardTests.cs and tests/Encina.PropertyTests/Validation/GuardClauses/GuardsPropertyTests.cs (FsCheck is available in the property project).
TASK: Write the unit, guard and property tests of Phase 2 for TryValidateNonNegative, using Shouldly through Encina.Testing.Shouldly; add the per-file targets of Guards.cs to the manifest via mechanical-fixer.
KEY RULES: tests execute the real Guards class; AAA; descriptive names; one behavior per test; no Thread.Sleep; the guard tests include the null-message case as a valid result; assert details keys through error.GetDetails(); assert the category through error.GetCategory().
REFERENCE FILES: tests/Encina.UnitTests/GuardClauses/GuardsTests.cs, tests/Encina.GuardTests/Validation/GuardClauses/GuardsGuardTests.cs, tests/Encina.PropertyTests/Validation/GuardClauses/GuardsPropertyTests.cs, tests/Encina.PropertyTests/AuditMartenEncryptedFieldProperties.cs (a [Property] example), .github/coverage-manifest/Encina.GuardClauses.json
```

</details>

### Phase 3: Documentation and finalization

<details>
<summary>Tasks</summary>

1. `src/Encina.GuardClauses/README.md` (via `docs-writer`): add the guard to the "Numeric Validation" block and a usage example with the balance case; correct the stale "68 tests" claim only if the writer can cite a source, otherwise remove the figure (documentation never types coverage or test figures by hand, AGENTS.md section 9). Show the Try-pattern example and the `Ensure` equivalent from the issue, written with the constant `Guards.GuardValidationFailed` instead of the literal `"guard.validation_failed"` of the issue, which is not the real code.
2. `changelog.d/653-guards-try-validate-non-negative.added.md` (via `mechanical-fixer`): one bullet naming `Guards.TryValidateNonNegative<T>`.
3. `ROADMAP.md`, `docs/INVENTORY.md` and `docs/releases/` need no change (no new package or file kind, no phase finished); no ADR (no architectural decision beyond the cited ones).
4. Build `dotnet build --configuration Release` (0 warnings), `dotnet test` of `Encina.UnitTests`, `Encina.GuardTests` and `Encina.PropertyTests` filtered to `GuardClauses`; the three architecture checks `ErrorCategoryCompletenessStaticScanTests`, `PublicAPI` analyzers and `--check-justifications` pass.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Phases 1 and 2 are done and green.
TASK: Document the guard in the Encina.GuardClauses README and add the changelog fragment; run the build and the tests listed in the plan.
KEY RULES: never edit CHANGELOG.md; fragment name <issue>-<slug>.<section>.md; README by docs-writer, fragment and PublicAPI by mechanical-fixer; no hand-typed coverage figures.
REFERENCE FILES: src/Encina.GuardClauses/README.md, changelog.d/README.md, src/Encina.GuardClauses/PublicAPI.Unshipped.txt
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Relevance |
|--------|-----------|
| `ArgumentOutOfRangeException.ThrowIfNegative<T>` (.NET 8) | The exception-based behavior this guard mirrors: accepts zero and positive values |
| AGENTS.md section 3 | ROP, no exceptions for business logic, fail closed |
| ADR-001, ADR-006 | `Either<EncinaError, T>` and error handling |
| ADR-062 | Every error declares an `ErrorCategory`; one code, one category |
| ADR-021 | EventIds: not used here (guards do not log) |
| #2108, #3737, #233 | Rejected-input details, category slice, error-code contract |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `Guards` | `src/Encina.GuardClauses/Guards.cs` | Hosts the new method and helper |
| `Guards.GuardValidationFailed` | `src/Encina.GuardClauses/Guards.cs:78` | The error code, reused |
| `EncinaErrors.Create(code, category, message, ..., details)` | `src/Encina/Results/EncinaErrors.cs:59` | Builds the error with a category |
| `ErrorCategory` | `src/Encina/Errors/ErrorCategory.cs` | The category of choice 4 |
| `ErrorCategoryPending.txt` | `tests/Encina.UnitTests/Core/ErrorCategoryPending.txt:87` | Pending line of the slice #3737 |
| `Encina.GuardClauses.json` | `.github/coverage-manifest/Encina.GuardClauses.json` | Per-file targets |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| Encina.GuardClauses | none | Guards do not log (the caller decides). The block 700-749 assigned to the family guards-1 stays unused and can be returned to the registry |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production | 2 | `Guards.cs`, `PublicAPI.Unshipped.txt` |
| Tests | 3 | `GuardsTests.cs`, `GuardsGuardTests.cs`, `GuardsPropertyTests.cs` (all modified) |
| Docs and metadata | 3 | `README.md`, `changelog.d/653-guards-try-validate-non-negative.added.md`, `.github/coverage-manifest/Encina.GuardClauses.json` |
| **Total** | **8** | About 300 changed lines. No other file: no `ProjectReference` change (the three test projects already reference `Encina.GuardClauses`), no DI, no contract class, no fake or test double (no interface changes) |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina (pre-1.0, .NET 10, C# 14). Issue #653: Guards.TryValidateNonNegative<T> in Encina.GuardClauses. AGENTS.md rules apply (ROP, no exceptions, zero warnings, per-flag coverage, CRAP <= 10). The eight comparison guards of the families guards-1 and guards-2 land one after another; this plan owns the private helper and the category constant.

IMPLEMENTATION OVERVIEW: (1) private CreateComparisonError and GuardErrorCategory in Guards.cs, public TryValidateNonNegative<T> where T : INumber<T> (choice 2), no value in the details (choice 3), category per choice 4; (2) unit, guard and FsCheck property tests; (3) README, changelog fragment, PublicAPI line, manifest per-file targets.

KEY PATTERNS: bool Try-pattern with out EncinaError; EncinaErrors.Create(code, category, ...) with the named category; details keys parameter/type/stage/guard; specialists for PublicAPI, changelog and manifest lines; docs-writer for the README.

REFERENCE FILES: src/Encina.GuardClauses/Guards.cs, src/Encina.GuardClauses/PublicAPI.Unshipped.txt, tests/Encina.UnitTests/GuardClauses/GuardsTests.cs, tests/Encina.GuardTests/Validation/GuardClauses/GuardsGuardTests.cs, tests/Encina.PropertyTests/Validation/GuardClauses/GuardsPropertyTests.cs, .github/coverage-manifest/Encina.GuardClauses.json
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
| 5 | Validation | ✅ | The feature is a validation guard; it feeds the same `Left` path as the other guards (the guard is not a replacement of the validation orchestrator) |
| 6 | Resilience | ❌ | No external call |
| 7 | Distributed Locks | ❌ | No shared state |
| 8 | Transactions | ❌ | No state change |
| 9 | Idempotency | ❌ | Pure function; same input, same result |
| 10 | Multi-Tenancy | ❌ | Tenant-independent: no tenant id, no tenant-qualified key, so the reserved `default` tenant segment does not apply |
| 11 | Module Isolation | ❌ | Module-independent |
| 12 | Audit Trail | ❌ | Too granular for audit; the error carries no value, so no personal data leaves the guard |

---

## Next Steps

1. Commit this plan, open its PR and link it from #653.
2. Open the plans of the family in order #653, #654, #655, #656, #657: they edit the same files and their implementation PRs must merge one after another.
3. When #653 is picked up, re-check the choices against today's code (notably whether #3737, #2108, #2123 and #2043 have landed) and ask the maintainer about the Dilemmas (choices 2, 3 and 4).
4. Brief a worker for Phases 1-3 (one PR, 8 files).
