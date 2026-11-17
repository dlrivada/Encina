# Implementation Plan: Always-Valid Domain Models (`Encina.Validation.Domain`)

> **Issue**: [#228](https://github.com/dlrivada/Encina/issues/228)
> **Type**: Feature
> **Related**: #227 (owns the generator project that S6 extends), #230 (owns the validation contract; not used here), #236 (two-phase validation, planned apart), #234 (planned apart), #726 (central validation instrumentation), ADR-001, ADR-006, ADR-054, ADR-062 (merged: error category contract)
> **Complexity**: Medium (a new package, a small contract in `Encina.DomainModeling`, one EF Core converter and a generator extension)
> **Estimated Scope**: ~1,100 lines of production code + ~3,000 lines of tests (~450 of them integration) + ~400 lines of documentation (~4,500 lines in total, as the Estimated File Count table), in seven pull requests of at most 15 files and 1,500 lines each

---

## Summary

### What exists today (read on 2026-11-15, main `db1e7f688`; the merge into `origin/main` `4ed1abf2f` on 2026-11-16 changed none of the files cited here)

- `Encina.DomainModeling` has `ValueObject` (structural equality through `GetEqualityComponents`) and `SingleValueObject<TValue>` (`Value`, `CompareTo`, `ToString`, an implicit conversion to `TValue`), both in `src/Encina.DomainModeling/ValueObject.cs`. The XML examples already show the smart-constructor shape: `public static Either<EncinaError, Email> Create(string value)`. The issue's `ValueObject<string>` is `SingleValueObject<string>` today. Six files reference them (`git grep -l -E 'SingleValueObject|: ValueObject\b' -- src tests`: `ValueObject.cs`, `GlobalSuppressions.cs`, `PublicAPI.Unshipped.txt`, `ValueObjectTests.cs`, `ValueObjectGuardTests.cs`, `BogusEnhancedGuardTests.cs`), and no derived value object exists in `src`.
- Not in `src`: `EmailAddress`, `PhoneNumber`, `Money`, any factory contract, a `DomainError` type, a `ValidationError.Required(...)` helper (`ValidationError` is a plain record `(PropertyName, ErrorMessage)` in `src/Encina/Validation/ValidationResult.cs:97`), any EF Core converter for value objects, any package named `Encina.Validation.Domain`.
- `Encina.DomainModeling` already has two other error idioms: `BusinessRuleError(ErrorCode, ErrorMessage)` with `Either<BusinessRuleError, Unit>` (`BusinessRule.cs:112,163`) and the repository and unit-of-work errors built as `EncinaError`. The error-category slice of ADR-062 for this package is still pending (`tests/Encina.UnitTests/Core/ErrorCategoryPending.txt`: `src/Encina.DomainModeling/ => P-DomainModeling`); the codes this plan creates are categorized from the first line.
- EF Core: `Encina.EntityFrameworkCore` references `Encina.DomainModeling` but not any validation package (`Encina.EntityFrameworkCore.csproj`), and has the converter precedent `ConfigureIdGenerationConventions` (`Converters/IdGenerationModelConfigurationExtensions.cs:30`, `Properties<UuidV7Id>().HaveConversion<UuidV7IdConverter>()`). It is one implementation for the three EF providers (SqlServer, PostgreSQL, MySQL).

### What this plan delivers

An opt-in always-valid value object layer: a factory contract `IValueObject<TSelf, TValue>` in `Encina.DomainModeling` (a static abstract `Create` returning `Either<EncinaError, TSelf>`) and categorized error builders; a new package `Encina.Validation.Domain` with `EmailAddress`, `PhoneNumber` and `Money`, none of which can be constructed in an invalid state; a generic EF Core converter and convention that persist a value object as its primitive and rebuild it through `Create`; a creation-failure counter; and, in the last implementation pull request, a generator that writes the boilerplate (gated on #227). Nothing changes for an application that never references the package.

- Affected packages: `Encina.DomainModeling`, `Encina.Validation.Domain` (new), `Encina.EntityFrameworkCore`, `Encina.Validation.Generators` (S6, from #227).
- Provider category: Database (10) for persistence of the converter (`AGENTS.md` section 5): the converter is one implementation for the 3 EF Core providers and is integration-tested on all three; Dapper (3), ADO.NET (3) and MongoDB need their own mapping and are a follow-up (draft: `artifacts/issues/value-object-persistence-dapper-ado-mongodb.md`).
- No `IValidationProvider` change: a value object validates itself at construction, which is the purpose; the request-level pipeline is unaffected.

---

## Design Choices

<details>
<summary><strong>1. Where do the common value objects live</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A new package `Encina.Validation.Domain` (the name in the issue title) with `EmailAddress`, `PhoneNumber`, `Money`; the contract stays in `Encina.DomainModeling`** | `Encina.DomainModeling` stays abstractions; pay-for-what-you-use; the catalog can grow without touching DomainModeling | One more package and csproj; two places to look |
| **B) Inside `Encina.DomainModeling`** | One package; fewer files | DomainModeling (78 files) takes a catalog and its tests; an application that wants only the base classes carries the catalog |

### Chosen Option: **A — New package `Encina.Validation.Domain`** (recommended, pending the maintainer)

### Rationale

- The issue title names the package and its "Affected Packages" ticks core; the only part that must be reachable from `Encina.EntityFrameworkCore` is the contract, which therefore stays in DomainModeling (Design Choice 2). B wins when the maintainer prefers a single package for DDD building blocks.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Where the factory contract lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IValueObject<TSelf, TValue>` in `Encina.DomainModeling`** | `Encina.EntityFrameworkCore` already references DomainModeling; the generic converter needs only the contract | None |
| **B) The contract and the converter inside `Encina.Validation.Domain`** | Everything in one package | The validation package would reference EF Core (or EF would reference the validation package): a dependency a non-EF application must not pay for |
| **C) The contract in `Encina` core, next to `Either` and `EncinaError`** | Reachable from every package with no new reference | Core grows with a domain-modeling concept |
| **D) The contract in `Encina.Validation.Domain` and the converter in a third package `Encina.Validation.Domain.EntityFrameworkCore`** | Each package has one dependency set | A fourth package for one converter |

### Chosen Option: **A — Contract in `Encina.DomainModeling`** (recommended, pending the maintainer)

### Rationale

- Today's references: `Encina.EntityFrameworkCore.csproj` references `Encina.DomainModeling` and `Encina.Validation.Domain` would reference it too, so A needs no new reference and keeps pay-for-what-you-use (`AGENTS.md` section 1); B fails that rule. C and D also satisfy it, so the choice is open: C wins when other packages beyond DomainModeling need the contract, D wins when the maintainer wants the catalog and its persistence versioned together.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. The error type returned by a smart constructor</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Either<EncinaError, TSelf>` with codes under `validation.domain.*` created with `ErrorCategory.Validation` (and the value object name and rule in `details`)** | One error model (ADR-001, ADR-006); the category flows to the HTTP, gRPC and GraphQL mapping (ADR-062); the existing `ValueObject.cs` examples already use it | One error per failed construction; no property-level list |
| **B) `Either<ValidationError, TSelf>` (the issue's sketch)** | Carries the property name; matches `ValidationResult` | `ValidationError` is a record, not an `EncinaError`: it has no code and no category, so a host cannot map it; a second error model |
| **C) `Either<BusinessRuleError, TSelf>` (the DomainModeling precedent)** | Already in the package | No category; the domain-rule error is meant for aggregate rules |

### Chosen Option: **A — `EncinaError` with `validation.domain.*` codes** (recommended, pending the maintainer)

### Rationale

- AGENTS.md section 3 says operations return `Either<EncinaError, T>`, and ADR-062 R3 makes field rules `Validation`; B wins when callers need to collect several property errors in one result (use `Combine` of `EitherExtensions` over the primitives instead, or a later aggregation, #234).

**Class:** Dilemma

</details>

<details>
<summary><strong>4. A way around validation: <code>CreateUnsafe</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No public bypass; the EF converter rebuilds through `Create` and throws `InvalidOperationException` (value object type only, never the value) when stored data is invalid** | The invariant holds everywhere: an invalid object cannot exist; corrupt data fails loudly at the infrastructure edge | A database row written by hand that violates a later-tightened rule fails to load |
| **B) A public `CreateUnsafe(value)` (the issue's sketch) used by the converter and by tests** | Loads legacy rows; trivial tests | Any caller can build an invalid object, which defeats "always valid"; cannot be audited |
| **C) An `internal` rehydration constructor exposed to the converter through `InternalsVisibleTo`** | No public bypass | The converter lives in another package: it would need `InternalsVisibleTo` from every value object assembly, including applications' own |

### Chosen Option: **A — No bypass; the converter validates** (recommended, pending the maintainer)

### Rationale

- The motivation of the issue is that "domain objects should be impossible to create in an invalid state". B wins when an existing database holds rows the new rules reject and a migration is not possible.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. How the EF Core mapping is configured</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A generic `ValueObjectConverter<TValueObject, TValue>` and `ConfigureEncinaValueObjectConventions(this ModelConfigurationBuilder, params Type[] valueObjectTypes)`, one `Properties<T>().HaveConversion<...>()` per type, in the style of `ConfigureIdGenerationConventions`** | Explicit, no assembly scan; follows the precedent; works for the three EF providers | The application lists its value object types once |
| **B) Scan assemblies for `IValueObject<,>` implementers** | Zero listing | Reflection over assemblies at model build; hides which types are mapped |

### Chosen Option: **A — Explicit conventions in the style of the id converters**

### Rationale

- The existing precedent maps each id type explicitly (`IdGenerationModelConfigurationExtensions.cs:30-45`); the same API shape keeps one idiom in the package.

**Class:** Trivial
**Settled by:** `src/Encina.EntityFrameworkCore/Converters/IdGenerationModelConfigurationExtensions.cs:30-45`

</details>

<details>
<summary><strong>6. Is the boilerplate generator in this issue</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Yes, as the last implementation pull request (S6), opened only after #227 S2 (the generator project) is on `main`: `[ValueObject]` plus a user `Validate` partial method generate `Create`, the private constructor and the `IValueObject` implementation** | Meets the acceptance criterion "source generator for boilerplate"; reuses the project | Waits for #227; adds a generator type and an attribute |
| **B) A follow-up issue after #227 lands** | This issue closes without the generator | The acceptance criterion stays unmet; the maintainer must accept the change |

### Chosen Option: **A — Include the generator, gated on #227** (recommended, pending the maintainer)

### Rationale

- The acceptance criteria list the generator; S1 to S5 and S7 do not depend on it. B wins when #227 slips and the maintainer wants this issue closed first.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Observability of creation failures</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A static `Counter<long>` on the package meter (`Encina.Validation.Domain`), tags value object type and error code; no `ActivitySource`, no logging in the factory** | Cheap (no cost without a listener); the factory stays pure and has no `ILogger`; no value ever reaches a log (value objects hold personal data) | No trace per construction; the caller logs by code |
| **B) Counter plus an `ActivitySource` span per `Create`** | A span per construction | A span for a few microseconds of work dwarfs the work; thousands of spans per request on a collection |
| **C) An injectable diagnostics hook (`IValueObjectDiagnostics`)** | Pluggable | A static factory cannot receive injected services; needs an ambient accessor |

### Chosen Option: **A — Counter only** (recommended, pending the maintainer)

### Rationale

- The issue asks for "log domain validation failures with context" and "trace creation failures and rates"; a static factory has no logger and the error message is never logged (AGENTS.md section 3), so the logged facts are the code and the type, carried by the counter and by the caller's log of the `Left`. B wins when the maintainer wants per-call traces in a debugging session.

**Class:** Dilemma

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | A failed construction is a `Left`; no exception for invalid input | Settled | AGENTS.md section 3 (ROP; never exceptions for business logic); ADR-001, ADR-006 |
| C2 | Error messages and `details` never contain the rejected value (an email or a phone number is personal data) | Settled | AGENTS.md section 3 (`EncinaError.Message` never reaches logs or plaintext); ADR-062; holds under every option of Design Choice 3 (the error type changes, the rule that no value is carried does not) |
| C3 | Error codes and categories (ADR-062), created with the category overload: `validation.domain.required`, `validation.domain.invalid_format`, `validation.domain.out_of_range`, `validation.domain.too_long`, `validation.domain.currency_mismatch` (all `ErrorCategory.Validation`, R3) | Dilemma (recommended, pending the maintainer) | public codes; follows Design Choice 3 |
| C4 | Value objects are immutable, sealed and compare structurally through the existing `GetEqualityComponents` | Settled | `src/Encina.DomainModeling/ValueObject.cs` (the base contract) |
| C5 | Whatever telemetry Design Choice 7 lands on (counter or span) carries no tenant tag: a static factory has no tenant context, and ADR-054 D4 adds the tag only where the tenant is known | Settled | ADR-054 D4; a static `Create` receives no request context under any option of Design Choice 7 |
| C6 | No new EventIds and no new range while Design Choice 7 option A stands (the factory has no logger); the block 570-579 reserved for this issue stays unused. If the maintainer picks another option, the range is registered first (`AGENTS.md` section 7) | Dilemma (recommended, pending the maintainer) | follows Design Choice 7; a range is registered only when a log class exists |
| C12 | `Money.Add` and `Subtract` use checked arithmetic: an overflow of `decimal` returns `validation.domain.out_of_range` (`ErrorCategory.Validation`) as a `Left`, never an `OverflowException` (C1). Option B: let the exception propagate as `decimal` does | Dilemma (recommended, pending the maintainer): Left | C1 forbids exceptions for invalid input but an overflow can also be read as a programming error, which is what B assumes. `OutOfRange` is returned only by `Money` |
| C13 | `Money` rules: negative and zero amounts are valid (a refund, a balance); no rounding or scale change (the amount keeps the scale it was given and equality compares amount and currency, so `1.0` and `1.00` are equal as `decimal` equality is); `Subtract` may produce a negative amount; the currency code is exactly three ASCII letters, accepted in any case and stored uppercase; two `Money` are equal when amount and normalized currency are equal. Option B: reject lowercase currency codes; Option C: reject negative amounts | Dilemma (recommended, pending the maintainer): as stated | business semantics; B wins when data must never be altered on construction, C wins for a price-only domain |
| C14 | Stored `Value` is the normalized form: `EmailAddress` trimmed with the domain lowercased and the local part unchanged; `PhoneNumber` as `+` and digits only (separators stripped); the converter persists that form. Option B: store the text as given and normalize only for comparison | Dilemma (recommended, pending the maintainer): normalized | the normalized form is what is persisted and indexed; B wins when an application must reproduce the user's original input |
| C15 | The model convention sets no column constraint (no `HasMaxLength`): the application configures column length and the validation rules (254, 16) already bound what is stored. Option B: the convention sets `HasMaxLength(254)` for `EmailAddress` and `HasMaxLength(16)` for `PhoneNumber` | Dilemma (recommended, pending the maintainer): none | one convention method works for any value object type; B wins when schema tooling should get the limit without application code |
| C16 | Mask format: `EmailAddress.ToString()` is the first character of the local part, `***`, `@` and the domain (`a***@example.com`); a one-character local part gives `*@example.com`; `PhoneNumber.ToString()` is `+`, the country prefix of the first two digits, asterisks, and the last three digits (the minimum length of eight digits guarantees at least three masked). Option B: a fixed `***` with no characters of the value | Dilemma (recommended, pending the maintainer): partial mask | extends C10; B wins when even a partial value is personal data in the target regulation |
| C7 | `EmailAddress`: trimmed, at most 254 characters, local part at most 64, one `@`, parsed with `System.Net.Mail.MailAddress` and compared case-insensitively on the domain; no deliverability check | Dilemma (recommended, pending the maintainer) | validation depth |
| C8 | `PhoneNumber`: E.164 only (`+` and 8 to 15 digits, separators stripped); no national numbering plan library | Dilemma (recommended, pending the maintainer) | validation depth |
| C9 | `Money`: `decimal` amount and a three-letter uppercase currency code (no ISO 4217 table); arithmetic between two `Money` of different currencies returns `validation.domain.currency_mismatch`, never converts | Dilemma (recommended, pending the maintainer) | semantics |
| C10 | `ToString()` of `EmailAddress` and `PhoneNumber` returns a masked form (`a***@example.com`, `+34*****678`); `Value` stays the full value | Dilemma (recommended, pending the maintainer) | logs and structured logging print `ToString()`; the alternative is the base `SingleValueObject` behavior |
| C11 | The generator hook (S6): the user writes `private static partial Either<EncinaError, TValue> Validate(TValue value)`; the generator writes the private constructor, `Create` and the `IValueObject` implementation | Dilemma (recommended, pending the maintainer) | public shape |

---

## Implementation Phases

### Phase 1: Contract and Error Builders in `Encina.DomainModeling`

> **Goal**: The factory contract and the categorized errors (pull request S1).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.DomainModeling/IValueObject.cs`: `public interface IValueObject<TSelf, TValue> where TSelf : IValueObject<TSelf, TValue> where TValue : notnull { static abstract Either<EncinaError, TSelf> Create(TValue value); TValue Value { get; } }`. `SingleValueObject<TValue>` is unchanged (it already has `Value`); a derived class opts in by implementing the interface.
2. **Create** `src/Encina.DomainModeling/ValueObjectErrors.cs`: the five codes of register row C3 as constants and builders `Required(string valueObject)`, `InvalidFormat(string valueObject, string expected)`, `OutOfRange(...)`, `TooLong(...)`, `CurrencyMismatch(...)`, each `EncinaErrors.Create(code, ErrorCategory.Validation, message, details: { ["valueObject"] = name })` with no rejected value anywhere.
3. **Modify** `src/Encina.DomainModeling/PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.DomainModeling.json` (through `mechanical-fixer`).
4. Tests of this phase ship in S1: unit (`ValueObjectErrorsTests`: code, category through `GetCategory`, no value in message or details), guard (`ValueObjectErrorsGuardTests`: null or whitespace names), contract (`ValueObjectContractTests`: a sample implementer of `IValueObject` keeps equality, `Value` and `Create` semantics).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0, no backward compatibility). Issue #228 adds always-valid value objects. Read
docs/plans/domain-value-object-validation-implementation-plan-228.md, Design Choices 2 and 3 and register rows C1-C3.

TASK:
1. Create IValueObject<TSelf, TValue> (static abstract Create returning Either<EncinaError, TSelf>) and ValueObjectErrors
   in src/Encina.DomainModeling/.
2. Every error is created with EncinaErrors.Create(code, ErrorCategory.Validation, message, details) (ADR-062).

KEY RULES:
- The rejected value never appears in a message or in details.
- XML docs on every public member; PublicAPI and manifest through mechanical-fixer; no [Obsolete].

REFERENCE FILES:
- src/Encina.DomainModeling/ValueObject.cs, BusinessRule.cs, RepositoryErrors.cs
- src/Encina/Results/EncinaErrors.cs, src/Encina/Errors/ErrorCategory.cs
```

</details>

---

### Phase 2: New Package and `EmailAddress`

> **Goal**: The package skeleton and the first value object, with its tests (pull request S2).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Validation.Domain/Encina.Validation.Domain.csproj` (references `Encina.DomainModeling`; `Microsoft.CodeAnalysis.PublicApiAnalyzers`; `PackageId`, `Description` and `PackageTags`; the README is packed, so it is created in this same pull request), `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`, **`src/Encina.Validation.Domain/README.md`** (a first version through `docs-writer`: what the package is and `EmailAddress`; S7 extends it), a new row in `docs/INVENTORY.md` (through `docs-writer`), and add the project to `Encina.slnx`. No `Directory.Packages.props` entry is needed (no new third-party package). `EmailAddress.cs` is modified again in S5 (the counter call); its per-file targets are entered here and S5 does not lower them.
2. **Create** `EmailAddress.cs`: `public sealed class EmailAddress : SingleValueObject<string>, IValueObject<EmailAddress, string>` with a private constructor, `Create` following register row C7 (`Required` for null or whitespace, `TooLong`, `InvalidFormat`), normalized value, masked `ToString` (register row C10).
3. **Modify** the test projects that need the new package: `tests/Encina.UnitTests/Encina.UnitTests.csproj`, `tests/Encina.GuardTests/Encina.GuardTests.csproj`, `tests/Encina.PropertyTests/Encina.PropertyTests.csproj` (one `ProjectReference` each); **create** `.github/coverage-manifest/Encina.Validation.Domain.json` (through `mechanical-fixer`).
4. Tests (S2): `EmailAddressTests` (unit), `EmailAddressGuardTests`, `EmailAddressProperties` (FsCheck: a value accepted by `Create` is accepted again by `Create(Value)`, equality is case-insensitive on the domain, no accepted value exceeds 254 characters).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #228, plan docs/plans/domain-value-object-validation-implementation-plan-228.md, phase 2 of 7. Depends on S1.

TASK:
1. Create the Encina.Validation.Domain project, add it to Encina.slnx and write EmailAddress.
2. Add the ProjectReference to the unit, guard and property test projects and write the three test files.

KEY RULES:
- No public bypass of Create (Design Choice 4); the rejected value never appears in an error; ToString masks.
- Tests execute the real factory; Shouldly through Encina.Testing.Shouldly; csproj, slnx and manifest edits through
  mechanical-fixer where they are not source code.

REFERENCE FILES:
- src/Encina.DomainModeling/ValueObject.cs (SingleValueObject), src/Encina.DomainModeling/Encina.DomainModeling.csproj
- tests/Encina.UnitTests/DomainModeling/ValueObjectTests.cs
```

</details>

---

### Phase 3: `PhoneNumber` and `Money`

> **Goal**: The other two common value objects (pull request S3).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `PhoneNumber.cs` (register row C8) and `Money.cs` (register row C9: `Create(decimal amount, string currency)`, `Add` and `Subtract` returning `Either<EncinaError, Money>`, `CurrencyMismatch` on different currencies; `Money` derives from `ValueObject` with `Amount` and `Currency` as equality components, so it has two components and does not implement the single-value `IValueObject` contract; the application maps it with EF Core complex properties, which the feature page of S7 documents, and the converter of S4 does not cover it).
2. **Modify** `PublicAPI.Unshipped.txt` and the manifest (through `mechanical-fixer`).
3. Tests (S3): unit, guard and property tests for each (E.164 round trip; for any two amounts with the same currency `Add` is commutative and associative; different currencies never add; overflow gives the register row C12 result), plus `ValueObjectCatalogContractTests` in `tests/Encina.ContractTests/DomainModeling/`: the `IValueObject` contract assertions of S1 (equality, `Value`, `Create(Value)` returns an equal object) run against the real `EmailAddress` and `PhoneNumber`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #228, plan docs/plans/domain-value-object-validation-implementation-plan-228.md, phase 3 of 7. Depends on S2.

TASK:
1. Write PhoneNumber (E.164) and Money (decimal plus currency code) in src/Encina.Validation.Domain/.
2. Money arithmetic returns Either<EncinaError, Money>; a currency mismatch is validation.domain.currency_mismatch.

KEY RULES:
- No exceptions for invalid input; no value in any message; PhoneNumber.ToString masks.
- Property tests for the arithmetic laws; guard tests for every public parameter.

REFERENCE FILES:
- src/Encina.Validation.Domain/EmailAddress.cs (phase 2), src/Encina.DomainModeling/EitherExtensions.cs
```

</details>

---

### Phase 4: EF Core Converter and Convention

> **Goal**: Persist a value object as its primitive and rebuild it through `Create`, on the three EF providers (pull request S4).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.EntityFrameworkCore/Converters/ValueObjectConverter.cs`: `public sealed class ValueObjectConverter<TValueObject, TValue> : ValueConverter<TValueObject, TValue> where TValueObject : IValueObject<TValueObject, TValue>`; the from-provider expression calls `TValueObject.Create` and, on a `Left`, throws `InvalidOperationException` naming the type only (Design Choice 4).
2. **Create** `Converters/ValueObjectModelConfigurationExtensions.cs`: `ConfigureEncinaValueObjectConventions(this ModelConfigurationBuilder, params Type[] valueObjectTypes)` (Design Choice 5).
3. **Modify** the `Encina.EntityFrameworkCore` `PublicAPI.Unshipped.txt` and manifest (through `mechanical-fixer`); **modify** `tests/Encina.IntegrationTests/Encina.IntegrationTests.csproj` (one `ProjectReference`).
4. Tests (S4): unit (converter round trip with the in-memory `Create`), guard, property (any value accepted by `Create` survives the round trip), and integration on the three EF providers in their shared collections: `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/{SqlServer,PostgreSQL,MySQL}/ValueObjects/ValueObjectPersistenceEFTests.cs` (`[Collection("EFCore-SqlServer")]` and its siblings, `ClearAllDataAsync` in `InitializeAsync`) with a shared test entity helper: save, query by value object, load, and a row with a value the rules reject fails with `InvalidOperationException`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Issue #228, plan docs/plans/domain-value-object-validation-implementation-plan-228.md, phase 4 of 7. Depends on S1-S3.
AGENTS.md section 5: the converter is one implementation for the three EF Core providers; section 9: real databases.

TASK:
1. Write ValueObjectConverter<TValueObject, TValue> and ConfigureEncinaValueObjectConventions in
   src/Encina.EntityFrameworkCore/Converters/ in the style of IdGenerationModelConfigurationExtensions.
2. Write unit, guard, property and three integration tests in the shared [Collection("EFCore-...")] fixtures.

KEY RULES:
- Never IClassFixture or new fixtures; never dispose the collection fixture; ClearAllDataAsync in InitializeAsync.
- The exception names the value object type, never the stored value.

REFERENCE FILES:
- src/Encina.EntityFrameworkCore/Converters/UuidV7IdConverter.cs, IdGenerationModelConfigurationExtensions.cs
- tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/SqlServer/DeadLetter/DeadLetterEndToEndEFTests.cs
```

</details>

---

### Phase 5: Creation-Failure Counter

> **Goal**: The observability of Design Choice 7 option A (pull request S5).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Validation.Domain/Diagnostics/ValueObjectMetrics.cs` (internal static; `Meter` named `Encina.Validation.Domain` per ADR-054 D3; counter `encina.validation.domain.failures` with tags `value_object` and `error_code`, no tenant tag, register row C5).
2. **Modify** `EmailAddress.cs`, `PhoneNumber.cs` and `Money.cs`: record the counter on every `Left`.
3. **Modify** the manifest (through `mechanical-fixer`). Tests (S5): `ValueObjectMetricsTests` with a `MeterListener` (one increment per failure, tags carry the type and the code, never the value).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
Issue #228, plan docs/plans/domain-value-object-validation-implementation-plan-228.md, phase 5 of 7. Depends on S2-S3.

TASK:
1. Add the internal ValueObjectMetrics with one counter and call it from the three factories on every Left.
2. Test with a MeterListener.

KEY RULES:
- Counter only (Design Choice 7); no ActivitySource, no logging in the factories; never put the value in a tag.

REFERENCE FILES:
- src/Encina.Security.Sanitization/Diagnostics/SanitizationDiagnostics.cs (meter and counter pattern)
```

</details>

---

### Phase 6: Boilerplate Generator

> **Goal**: `[ValueObject]` generates `Create` and the contract implementation (pull request S6; opens only after #227 S2 is on `main`).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.DomainModeling/ValueObjectAttribute.cs` (public marker; the generator finds it by metadata name).
2. **Create** in `src/Encina.Validation.Generators/`: `ValueObjectGenerator.cs` (a second `IIncrementalGenerator` in the project of #227) and `Emit/ValueObjectEmitter.cs`: for a `partial` class deriving `SingleValueObject<T>` with a user `Validate` partial method (register row C11), emit the private constructor, `Create` (calls `Validate`, wraps the result) and the `IValueObject<TSelf, TValue>` implementation.
3. **Modify** `PublicAPI.Unshipped.txt` of `Encina.DomainModeling` and the manifests of `Encina.DomainModeling` and `Encina.Validation.Generators` (through `mechanical-fixer`).
4. Tests (S6), in `tests/Encina.UnitTests/Validation/Generators/` using the shared `GeneratorTestHost` that #227 S2 places in `tests/Encina.TestInfrastructure` (no further test csproj edit): one snapshot test class and its two `.verified.txt` files, a unit test of the diagnostics (not partial, no `Validate` method) and an equivalence test that compiles a generated value object in memory and compares it with a hand-written one. A generator that declares diagnostics follows the packaging conventions of #227 (its register row C15).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
Issue #228, plan docs/plans/domain-value-object-validation-implementation-plan-228.md, phase 6 of 7. Requires the
generator project of issue #227 (S2) on main; follow its harness and diagnostic conventions.

TASK:
1. Add [ValueObject] to Encina.DomainModeling and a ValueObjectGenerator plus emitter to Encina.Validation.Generators.
2. Snapshots, diagnostics test and an in-memory equivalence test.

KEY RULES:
- Reflection-free output; a shape the generator cannot support is a compile error, never a silent skip.

REFERENCE FILES:
- src/Encina.Validation.Generators/ (issue #227), docs/plans/validation-source-generators-implementation-plan-227.md
```

</details>

---

### Phase 7: Documentation and Finalization

> **Goal**: Usage, READMEs, changelog and the justification files (pull request S7).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** through `docs-writer`: `docs/features/always-valid-value-objects.md`; **modify** `src/Encina.Validation.Domain/README.md` (created in S2; extended with `PhoneNumber`, `Money`, the EF Core mapping and the generator) and `src/Encina.DomainModeling/README.md`.
2. **Create** `changelog.d/228-domain-value-object-validation.added.md` (through `mechanical-fixer`).
3. **Create** the justification files (`AGENTS.md` section 9): load and benchmark are not implemented because `Create` is a pure in-memory function with no shared state (`.md` under the existing Load and Benchmark projects).
4. Build verification: `dotnet build --configuration Release` with zero warnings; per-flag coverage of the touched files against the manifest targets below.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT:
Issue #228, plan docs/plans/domain-value-object-validation-implementation-plan-228.md, phase 7 of 7.

TASK:
1. Document the contract, the three value objects, the EF Core mapping and the generator; update the two READMEs.
2. Add the changelog fragment and the justification files; run the build and the per-flag coverage measurement.

KEY RULES:
- Never type coverage figures by hand; cite cov: markers (SPEC-001). Pages through docs-writer; fragments through
  mechanical-fixer. Sibling plans are referenced by issue number only.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md, changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| Always-Valid Domain Model (Enterprise Craftsmanship, linked in the issue) | Validate at construction; an invalid object must not exist | Design Choices 3 and 4 |
| Martin Fowler, Value Object | Equality by value, immutability | Register row C4 |
| E.164 (ITU-T) | International telephone number format, at most 15 digits | Register row C8 |
| ISO 4217 | Alphabetic currency codes of three letters | Register row C9 (shape only, no table) |
| RFC 5321 | Email path limits (254 characters, local part 64) | Register row C7 |
| EF Core value conversions | `ValueConverter`, `ConfigureConventions` | Phase 4 |
| ADR-054 D3, D4 | Meter per package; tenant tag only when known | Phase 5, register row C5 |
| ADR-062 | Error category contract; R3 field rules are `Validation` | Register row C3 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `ValueObject`, `SingleValueObject<T>` | `src/Encina.DomainModeling/ValueObject.cs` | Base classes of the three value objects |
| `EitherExtensions.Combine` | `src/Encina.DomainModeling/EitherExtensions.cs` | Combining several smart constructors in an aggregate factory |
| `EncinaErrors.Create` with a category | `src/Encina/Results/EncinaErrors.cs` | Error builders |
| `ConfigureIdGenerationConventions`, `UuidV7IdConverter` | `src/Encina.EntityFrameworkCore/Converters/` | Style of the converter and the convention |
| EF Core shared fixtures | `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/` | Integration tests on the three providers |
| `SanitizationDiagnostics` | `src/Encina.Security.Sanitization/Diagnostics/` | Meter and counter pattern |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| none | none | No logger exists in the static factories (Design Choice 7, register row C6); the slot 570-579 of the family block 550-599 is not used |

### Estimated File Count

Every count includes tests, coverage manifest edits, PublicAPI files, test project references, the README and the changelog fragment. No pull request uses a type created by a later one.

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| S1 | Contract and error builders | 2 (`IValueObject`, `ValueObjectErrors`) | 1 (PublicAPI) | 3 (unit, guard, contract) | 0 | 1 | 0 | 7 | ~450 |
| S2 | New package, metadata, README and `EmailAddress` | 2 (csproj, `EmailAddress`) | 2 (PublicAPI Shipped and Unshipped) | 3 (unit, guard, property) | 3 (the three test csproj) | 1 | 3 (`Encina.slnx`, package README, `docs/INVENTORY.md` row) | 14 | ~900 |
| S3 | `PhoneNumber` and `Money` | 2 | 1 (PublicAPI) | 7 (unit, guard, property for each, catalog contract) | 0 | 1 | 0 | 11 | ~950 |
| S4 | EF Core converter and convention | 2 | 1 (PublicAPI of `Encina.EntityFrameworkCore`) | 7 (unit, guard, property, 3 integration, 1 entity helper) | 1 (`Encina.IntegrationTests.csproj`) | 1 | 0 | 12 | ~900 |
| S5 | Creation-failure counter | 1 (`ValueObjectMetrics`) | 3 (the three value objects) | 1 (metrics test) | 0 | 1 | 0 | 6 | ~300 |
| S6 | Boilerplate generator (needs #227 S2 and its shared harness) | 3 (`ValueObjectAttribute`, `ValueObjectGenerator`, `ValueObjectEmitter`) | 1 (PublicAPI) | 5 (snapshot test class, 2 `.verified.txt` snapshots, diagnostics unit, equivalence) | 0 | 2 | 0 | 11 | ~700 |
| S7 | Documentation | 0 | 2 (`Encina.DomainModeling` README, the package README created in S2) | 2 (load and benchmark justification `.md`) | 0 | 0 | 2 (feature page, changelog fragment) | 6 | ~300 |
| **Total** | | **12** | **11** | **28** | **4** | **7** | **5** | **67** | **~4,500** |

The columns sum to the total (12 + 11 + 28 + 4 + 7 + 5 = 67) and every statement of the total in this plan is 67 files and about 4,500 lines (450 + 900 + 950 + 900 + 300 + 700 + 300). The largest pull request is S2 at 14 files; none exceeds 1,500 lines. S6 waits for #227 S2; S1 to S5 and S7 do not. The existing value object tests (`ValueObjectTests`, `ValueObjectGuardTests`, `BogusEnhancedGuardTests`) are not edited: `SingleValueObject<T>` does not change.

Per-file coverage targets (per flag, with a one-sentence justification), written into the package manifests by the pull request that adds the file:

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `ValueObjectErrors.cs` (S1) | 95 | 90 | 0 | 0 | 0 | Pure builders: every branch and null or whitespace argument is reachable; the Contract flag is 0 because it is a static builder class with no interface or abstract member to keep, and the contract test of `IValueObject` does not call it; Property is 0 because the builders have no input space beyond a name string, covered by the unit cases |
| `IValueObject.cs` | 100 | 0 | 80 | 0 | 0 | An interface with a static abstract member: only the contract test exercises it through a sample implementer |
| `EmailAddress.cs` (S2: Unit, Guard, Property), `PhoneNumber.cs` (S3) | 95 | 90 | 0 in S2 | 70 | 0 | Each branch of the validation has a unit case; the property test covers the round trip and the length bounds |
| `EmailAddress.cs`, `PhoneNumber.cs` (S3: Contract) | 95 | 90 | 40 | 70 | 0 | The Contract target is entered by S3, whose `ValueObjectCatalogContractTests` runs the `IValueObject` contract against both real classes |
| `Money.cs` (S3) | 95 | 90 | 0 | 80 | 0 | Arithmetic laws are property-testable; currency mismatch, overflow and null guards have direct cases; Contract is 0 because `Money` derives from `ValueObject` with two components and implements no contract interface, so there is no contract surface beyond the structural equality the unit and property tests cover |
| `ValueObjectConverter.cs` | 90 | 85 | 0 | 60 | 70 | The round trip is unit- and property-tested in memory; the real providers run it in the integration tests |
| `ValueObjectModelConfigurationExtensions.cs` | 90 | 90 | 0 | 0 | 60 | The convention registration is unit-tested; the integration tests prove the model builds on each provider |
| `Diagnostics/ValueObjectMetrics.cs` | 90 | 0 | 0 | 0 | 0 | Internal static class with no parameters; the `MeterListener` test covers it |
| `ValueObjectGenerator.cs`, `Emit/ValueObjectEmitter.cs` | 85 | 0 | 0 | 0 | 0 | Driven through the generator harness of #227; internal types and Roslyn structs have no guard surface |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #228 - always-valid domain models.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always.
- Railway Oriented Programming: Either<EncinaError, T>; every new code carries an ErrorCategory (ADR-062).
- The maintainer's answers to Design Choices 1 to 4, 6 and 7 of
  docs/plans/domain-value-object-validation-implementation-plan-228.md decide the package, the error type, the
  bypass, the generator scope and the observability.

IMPLEMENTATION OVERVIEW:
1. Encina.DomainModeling: IValueObject<TSelf, TValue> and ValueObjectErrors.
2. Encina.Validation.Domain: EmailAddress, PhoneNumber, Money (no public bypass, masked ToString).
3. Encina.EntityFrameworkCore: ValueObjectConverter and ConfigureEncinaValueObjectConventions, integration-tested on
   the three EF providers.
4. A creation-failure counter; no logging, no span.
5. The [ValueObject] generator (after #227), documentation and changelog.
Seven pull requests (S1 to S7), see the Estimated File Count table.

KEY PATTERNS:
- Rejected values never appear in errors, tags or logs; tests execute real code; shared [Collection] fixtures.
- Zero warnings; PublicAPI and manifests through mechanical-fixer.

REFERENCE FILES:
- src/Encina.DomainModeling/ValueObject.cs, src/Encina.EntityFrameworkCore/Converters/
- docs/plans/domain-value-object-validation-implementation-plan-228.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Construction is a pure function of its input |
| 2 | OpenTelemetry | ✅ Phase 5 | Counter `encina.validation.domain.failures` on the `Encina.Validation.Domain` meter; no tenant tag (register row C5) |
| 3 | Structured Logging | ❌ N/A | A static factory has no logger and its error message never reaches a log (AGENTS.md section 3); the caller logs the `Left` by code (Design Choice 7, register row C6). The issue marks this "Integrate"; the deviation is recorded as Design Choice 7 for the maintainer |
| 4 | Health Checks | ❌ N/A | In-process, no dependency |
| 5 | Validation | ✅ All phases | This is validation at construction; the request-level providers are unchanged |
| 6 | Resilience | ❌ N/A | No external call |
| 7 | Distributed Locks | ❌ N/A | No shared state |
| 8 | Transactions | ❌ N/A | Construction happens before and outside any transaction |
| 9 | Idempotency | ❌ N/A | Pure and repeatable |
| 10 | Multi-Tenancy | ⏭️ | Tenant-specific invariants need a tenant at construction, which a static factory does not have: a follow-up shared with the family (draft: `artifacts/issues/validation-tenant-module-scoped-selection.md`) |
| 11 | Module Isolation | ⏭️ | The same follow-up issue |
| 12 | Audit Trail | ⏭️ | Auditing of domain validation failures: a follow-up (draft: `artifacts/issues/validation-failure-audit.md`); personal data in value objects makes the audit record carry the code only |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| `SingleValueObject<T>` and `EitherExtensions` | ✅ On `main` | Base classes |
| ADR-062 error category contract | ✅ Merged | Codes of register row C3 |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Generator project | #227 | Blocking for S6 only |
| Persistence for Dapper (3), ADO.NET (3) and MongoDB | follow-up (draft: `artifacts/issues/value-object-persistence-dapper-ado-mongodb.md`) | The provider coherence gap of the converter |
| `DomainModeling` error categories | P-DomainModeling slice of ADR-062 | The existing codes of the package are categorized there, not here |

---

## Next Steps

1. Link this plan from #228 and ask the maintainer to answer Design Choices 1 to 4, 6 and 7 and the Dilemma rows C3, C6 to C16 of the register when work starts.
2. Open the follow-up issues when the first pull request opens (all three drafts exist in the planning worktree's `artifacts/issues/`): persistence for Dapper, ADO.NET and MongoDB (draft: `artifacts/issues/value-object-persistence-dapper-ado-mongodb.md`), tenant and module scoped invariants (draft: `artifacts/issues/validation-tenant-module-scoped-selection.md`) and auditing of validation failures (draft: `artifacts/issues/validation-failure-audit.md`).
3. Implement S1 to S7 as the pull requests of the Estimated File Count split, `Fixes #228` on S7.
