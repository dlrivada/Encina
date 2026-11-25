# Implementation Plan: Architecture Rule — Aggregates Reference Other Aggregates by Id, Not by Navigation

> **Issue**: [#4025](https://github.com/dlrivada/Encina/issues/4025)
> **Type**: Feature
> **Complexity**: Low-Medium (5 phases, no database provider, one custom ArchUnitNET condition, 11 files in 1 pull request)
> **Estimated Scope**: ~260 lines of production code + ~520 lines of tests, README and manifest
> **Family**: DDD discipline family (EPIC #1037). It is the sibling of #1038 (repository versus query-handler read boundary) and reads the base types planned and shipped by `Encina.DomainModeling`. Siblings are referred to by issue number only.

---

## Summary

An aggregate that holds another aggregate as a property (`Order.Customer`) compiles, maps in EF Core without complaint and passes every test, yet the consistency boundary is gone: loading `Order` drags `Customer` in, one transaction spans two boundaries and a change to one aggregate can mutate the other. #4025 adds an opt-in rule to `Encina.Testing.Architecture` that fails when an aggregate has a property or field whose type is another aggregate, or an array or generic collection of one, so aggregates reference each other by id.

**What exists today** (`origin/main` at `278c164f1`, 2026-11-24):

- `src/Encina.Testing.Architecture/EncinaArchitectureRules.cs` has 23 static rule methods; the only aggregate rule is `AggregatesShouldFollowPattern(string aggregateNamespace)` (line 276), which checks that classes named `*Aggregate` are sealed. No rule inspects the member types of a class. The remark above that method already records the limit of the fluent API for generic base classes.
- `EncinaArchitectureRulesBuilder` exposes `EnforceSealedAggregates(string)` (line 196), `AddCustomRule(IArchRule)` (line 174), `ApplyAllStandardRules()` (line 240) and `ApplyAllSagaRules()` (line 263, documented as opt-in). `EventIdUniquenessRule` is the precedent of a rule that does not use the fluent API (reflection, wired into the builder through a flag).
- The aggregate base types exist in `Encina.DomainModeling`: `AggregateRoot<TId>` (`AggregateRoot.cs:55`, with the derived `AuditableAggregateRoot<>`, `SoftDeletableAggregateRoot<>`, `AuditedAggregateRoot<>` and `FullyAuditedAggregateRoot<>`), `AggregateBase` and `AggregateBase<TId>` (`AggregateBase.cs:64,137`), `IAggregateRoot` and `IAggregateRoot<out TId>` (`IAggregateRoot.cs:12,51`), `IAggregate` and `IAggregate<out TId>` (`IAggregate.cs:23,74`). All are in the namespace `Encina.DomainModeling`. `Encina.Testing.Architecture` references only `Encina` (not `Encina.DomainModeling`), and the user's own architecture always contains the aggregates, so the default identification must work on names, not on `typeof`.
- ArchUnitNET 0.13.3 (`Directory.Packages.props`) exposes what the rule needs on `PropertyMember` and `FieldMember`: `Type`, `GenericArguments`, `IsArray`, `IsCompilerGenerated`, `IsStatic`; and on `Class`: `BaseClass`, `ImplementedInterfaces`, `Members` (checked on the 0.13.3 assembly, 2026-11-24). It does not expose reference-type nullability, so `Other?` and `Other` are the same member type and one check covers both.

**Standards and references**: "reference other aggregates by identity only" (Vernon, *Effective Aggregate Design*; Evans, *Domain-Driven Design*, aggregates chapter). No law applies.

**Affected packages**: `Encina.Testing.Architecture` only. The issue also describes `Encina.DomainModeling` as the source of the base types; it does not change.

**Provider category**: none. The rule is a test-time check over loaded assemblies; the 10-provider matrix, the cache, transport and lock categories do not apply.

**Shared pieces and who owns them**: this plan owns the three `AggregatesShouldNotReferenceOtherAggregates` overloads, the three `EnforceAggregateReferencesById` overloads and the internal `AggregateReferenceCondition`. #1038 owns the repository versus query-handler rule; the two rules share no code. This plan registers no EventId range and no error code.

---

## Design Choices

<details>
<summary><strong>1. Static members of an aggregate (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Only instance properties and instance fields are inspected; static members are skipped** | A `public static readonly Order Empty` null object is not a navigation: EF Core does not map statics and the boundary is untouched; no false positives on null-object and registry patterns | A static field that holds a live aggregate (a cache) passes; that is a different smell |
| **B) Every property and field is inspected, static or not** | Simplest rule, matches the issue sentence "every property and every field" literally | Flags `static readonly` sentinels and `Lazy<T>` caches that are not part of the object graph |

### Chosen Option: **A — Instance members only** (recommended, pending the maintainer)

### Rationale

- A wins in any code base that uses a null object or a static default (`Money.Zero` style on an aggregate); B wins if the maintainer wants the rule to also catch process-wide caches of aggregates, which is a different concern from the navigation the issue targets. The wording of the issue (a navigation "property or field") describes instance state; the issue does not discuss statics.
- **Class:** Dilemma

</details>

<details>
<summary><strong>2. Navigations hidden in entities owned by the aggregate (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Only the members of the aggregate types themselves are inspected, as the issue states** | Exactly the issue's scope; simple, no graph walk; the message always names an aggregate that the developer owns | `OrderLine.Product` (a child entity holding another aggregate) is not found |
| **B) Also inspect the entity types reachable from the aggregate's own members (owned children), transitively** | Catches the most common real mistake: the child entity that carries the navigation | A graph walk with cycle protection; "owned" must be defined (any `Entity<TId>` that is not itself an aggregate); a larger message and a higher chance of false positives on shared entities |

### Chosen Option: **A — Aggregate members only** (recommended, pending the maintainer)

### Rationale

- A wins for this release because it is the issue's stated scope and keeps the rule decidable by type name. B wins when a team keeps most navigations inside child entities, which is likely in practice; it is recorded as a deferred step inside this plan (Prerequisites & Dependencies, "Deferred") and needs its own acceptance criteria, not a new issue.
- **Class:** Dilemma

</details>

<details>
<summary><strong>3. How ArchUnitNET inspects the member types (Trivial)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A custom condition over `Class.Members` (`PropertyMember`, `FieldMember`) that unwraps `IsArray` and `GenericArguments` recursively, attached with `Should().FollowCustomCondition(...)`** | Stays inside the `IArchRule` model: `rule.Check(architecture)`, `AddCustomRule` and `VerifyWithResult` work unchanged; the loaded architecture already holds the types | Depends on ArchUnitNET exposing generic arguments (it does in 0.13.3) |
| **B) Reflection over the loaded assemblies, in the style of `EventIdUniquenessRule`** | Full control of nullable and generic handling | Not an `IArchRule`; needs a new flag in the builder and a second result path |
| **C) `DoNotDependOnAny(aggregates)`** | One fluent line | Flags method bodies, parameters and return types that legitimately mention other aggregates (domain services, factories): wrong |

### Chosen Option: **A — A custom condition on the loaded architecture**

### Rationale

The issue chose A with B as the fallback and rejected C; the first unit test of Phase 1 pins how 0.13.3 reports `Foo[]` and `List<Foo>` members, and the fallback applies only if that test shows the generic arguments missing.

- **Class:** Trivial
- **Settled by:** issue #4025 ("ArchUnitNET limitation and the chosen approach") and the existing precedent of `EventIdUniquenessRule` for the fallback.

</details>

<details>
<summary><strong>4. Default identification of an aggregate, by name (Trivial)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A class whose base-class chain contains `Encina.DomainModeling.AggregateRoot<>` or `AggregateBase`/`AggregateBase<>`, or that implements `Encina.DomainModeling.IAggregateRoot` or `IAggregate`; matched by `FullName` because the package does not reference `Encina.DomainModeling`; predicate and namespace overloads replace it** | Covers the five aggregate base classes and both interfaces; no new package reference; `Entity<TId>` alone is not an aggregate | A user-defined aggregate base that implements neither interface needs the predicate overload |
| **B) Add a ProjectReference to `Encina.DomainModeling` and use `typeof`** | Compile-time checked names | A new dependency edge from a testing package to a domain package for a name check |

### Chosen Option: **A — Full-name matching with predicate and namespace overloads**

### Rationale

The namespace overload follows the `EnforceSealedAggregates(string aggregateNamespace)` convention (`EncinaArchitectureRulesBuilder.cs:196`); the predicate overload covers custom bases.

- **Class:** Trivial
- **Settled by:** issue #4025 ("How aggregates are identified") and the `EnforceSealedAggregates` convention.

</details>

<details>
<summary><strong>5. Opt-in, outside the standard rule set (Settled)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A separate `EnforceAggregateReferencesById` method; `ApplyAllStandardRules` is unchanged** | Pay-for-what-you-use; a project with no aggregates gets no rule that has nothing to check | The developer must call it |
| **B) Add it to `ApplyAllStandardRules`** | Applied without a decision | Changes the standard set for every existing user and fails projects that never opted into DDD |

### Chosen Option: **A — Opt-in**

### Rationale

The standard set is the rules that apply to every Encina application; aggregates are optional (`Encina.DomainModeling` is a separate package).

- **Class:** Settled
- **Settled by:** `AGENTS.md` section 1 (pay-for-what-you-use: every feature opt-in) and the precedent of `ApplyAllSagaRules` (`EncinaArchitectureRulesBuilder.cs:255-267`), which the source documents as "opt-in and not included in `ApplyAllStandardRules`".

</details>

<details>
<summary><strong>6. A self-reference is a violation (Trivial)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Category.Parent` of type `Category` is flagged; the fix is `CategoryId? ParentId`** | Another instance of an aggregate is another consistency boundary; same fix as a reference to another type | A tree aggregate that really embeds its own nodes must model them as an owned entity |
| **B) Allow self-references behind an option** | Fewer false positives on trees | An option nobody needs pre-1.0; a rule with a hole |

### Chosen Option: **A — Flag self-references**

### Rationale

No option is added: the rule reports the aggregate, the member and the referenced type, and the message suggests the id type.

- **Class:** Trivial
- **Settled by:** issue #4025 ("Self-references ... flagged") and `AGENTS.md` section 1 (pre-1.0, best solution first, no compatibility option).

</details>

---

## Implementation Phases

### Phase 1: Core API Design — Identification and the Condition

> **Goal**: The condition that decides, for one class, which members reference an aggregate.

<details>
<summary>Tasks</summary>

1. **`AggregateReferenceCondition`** (new, `internal static class`, `src/Encina.Testing.Architecture/AggregateReferenceCondition.cs`). Methods, each small enough for CRAP at most 10: `Evaluate(Class aggregate, Func<IType, bool> isAggregate)` returns the ArchUnitNET condition result with one failure text per offending member; `ReferencedAggregate(ITypeInstance<IType> instance, Func<IType, bool> isAggregate)` unwraps the instance (`Type`, `IsArray`, `GenericArguments`) recursively and returns the first aggregate type found or null; `IsDefaultAggregate(IType type)` walks `Class.BaseClass` and `ImplementedInterfaces` comparing `FullName` to `Encina.DomainModeling.AggregateRoot`1`, `AggregateBase`, `AggregateBase`1`, `IAggregateRoot`, `IAggregateRoot`1`, `IAggregate`, `IAggregate`1`.
2. Member selection (D1 recommended): instance `PropertyMember` and `FieldMember`; compiler-generated backing fields of auto-properties are skipped because the property is counted (`IsCompilerGenerated`); a private backing field of a collection (`private readonly List<Other> _others`) is a real field and is counted. Dictionary keys and values are both unwrapped because both are generic arguments.
3. The failure text is `{aggregate} member {member} references aggregate {other}; reference it by id` (type and member names only; no value, no `EncinaError`).
4. Nothing logs, throws a domain exception or touches `EncinaError`: the result is an `IArchRule` failure like every other rule in the package.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```
CONTEXT:
- Encina is a .NET 10 / C# 14 library. Issue #4025; plan docs/plans/aggregate-references-by-id-implementation-plan-4025.md. Dilemmas D1 and D2 are recommended, pending the maintainer: read the Maintainer Decisions section if it exists and follow the answers.
- src/Encina.Testing.Architecture uses ArchUnitNET 0.13.3 and does not reference Encina.DomainModeling. PropertyMember and FieldMember expose Type, GenericArguments, IsArray, IsCompilerGenerated and IsStatic.

TASK:
Create AggregateReferenceCondition as an internal static class with Evaluate, ReferencedAggregate and IsDefaultAggregate (Phase 1 Tasks). Write the first unit test that pins how 0.13.3 reports Foo[] and List<Foo> members before building on it.

KEY RULES:
- Match base types and interfaces by FullName, never by typeof.
- Each method has cyclomatic complexity low enough that CRAP stays at most 10 with the unit tests.
- The failure text names type and member only.
- No [Obsolete], XML docs on every member that is visible, zero warnings.

REFERENCE FILES:
- src/Encina.Testing.Architecture/EncinaArchitectureRules.cs (rule style, line 276)
- src/Encina.Testing.Architecture/EventIdUniquenessRule.cs (fallback precedent)
- src/Encina.DomainModeling/AggregateRoot.cs, AggregateBase.cs, IAggregateRoot.cs, IAggregate.cs
```

</details>

---

### Phase 2: Implementation — The Rule and the Builder

> **Goal**: The three rule overloads and the three builder methods.

<details>
<summary>Tasks</summary>

1. **`EncinaArchitectureRules`** (edit): `AggregatesShouldNotReferenceOtherAggregates()` (default identification), `AggregatesShouldNotReferenceOtherAggregates(Func<IType, bool> isAggregate)` (`ArgumentNullException.ThrowIfNull`) and `AggregatesShouldNotReferenceOtherAggregates(string aggregateNamespace)` (`ArgumentException.ThrowIfNullOrWhiteSpace`, `ResideInNamespaceMatching` as `AggregatesShouldFollowPattern`). Each is `Classes().That()...Should().FollowCustomCondition(...)` with `.Because("Aggregates reference other aggregates by id, not by navigation")`.
2. **`EncinaArchitectureRulesBuilder`** (edit): `EnforceAggregateReferencesById()`, `EnforceAggregateReferencesById(Func<IType, bool>)`, `EnforceAggregateReferencesById(string)`, each adding the rule to `_rules` and returning `this`. `ApplyAllStandardRules` is not changed.
3. **`PublicAPI.Unshipped.txt`**: six lines, `Encina.Testing.Architecture.EncinaArchitectureRules.AggregatesShouldNotReferenceOtherAggregates() -> ArchUnitNET.Fluent.IArchRule!`, the `Func<ArchUnitNET.Domain.IType!, bool>!` and `string!` overloads, and the three builder methods returning `Encina.Testing.Architecture.EncinaArchitectureRulesBuilder!`.
4. Constructor and signature count: no constructor is added; the three rule overloads and three builder overloads are the only new public symbols (6 PublicAPI lines); the optional-parameter order rule does not apply because the overloads are distinct signatures, not optional parameters.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```
CONTEXT:
- Issue #4025, Phase 2. AggregateReferenceCondition exists from Phase 1.

TASK:
Add the three rule overloads to EncinaArchitectureRules and the three EnforceAggregateReferencesById overloads to EncinaArchitectureRulesBuilder, with XML docs (summary, remarks naming the opt-in status, param, returns, an example) and the six PublicAPI.Unshipped.txt lines.

KEY RULES:
- Do not add the rule to ApplyAllStandardRules.
- Validate arguments exactly like AggregatesShouldFollowPattern.
- The predicate overload receives IType; the namespace overload keeps the identification of the default and narrows by namespace.

REFERENCE FILES:
- src/Encina.Testing.Architecture/EncinaArchitectureRules.cs (line 276)
- src/Encina.Testing.Architecture/EncinaArchitectureRulesBuilder.cs (lines 174-260)
- src/Encina.Testing.Architecture/PublicAPI.Unshipped.txt
```

</details>

---

### Phase 3: Cross-Cutting Integration and Observability

> **Goal**: Record that no runtime concern applies and prove the rule stays out of the default set.

<details>
<summary>Tasks</summary>

1. No ActivitySource, Meter, health check, store or DI registration: the rule runs inside a test process. There is no EventId, no `Log.cs` and no new range in `src/Encina/Diagnostics/EventIdRanges.cs`.
2. No error code: nothing returns `Either<EncinaError, T>`; a violation is an `IArchRule` failure (so no ADR-062 `ErrorCategory` and no tenant tag apply).
3. A unit test asserts that `ApplyAllStandardRules()` does not add the rule (the builder's rule count is unchanged).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```
CONTEXT:
- Issue #4025, Phase 3. The feature is a test-time architecture rule.

TASK:
Confirm no telemetry, logging or registration is added, and add the unit test that ApplyAllStandardRules does not include EnforceAggregateReferencesById.

KEY RULES:
- Do not register an EventId range and do not touch DI.

REFERENCE FILES:
- src/Encina.Testing.Architecture/EncinaArchitectureRulesBuilder.cs (ApplyAllStandardRules, ApplyAllSagaRules)
```

</details>

---

### Phase 4: Testing

> **Goal**: Every applicable flag at its target; real fixture types in a real loaded architecture.

<details>
<summary>Tasks</summary>

1. **Fixtures** (new, `tests/Encina.UnitTests/Testing/Architecture/AggregateReferenceFixtures.cs`, `Encina.UnitTests` already references `Encina.DomainModeling` and `Encina.Testing.Architecture`): two namespaces, `...Fixtures.Violating` and `...Fixtures.Compliant`, so a namespace-scoped rule isolates each. Violating: a single navigation, `ICollection<Other>`, `IReadOnlyList<Other>`, `Other[]`, `Other?`, `private readonly List<Other> _others`, a self-reference, `List<List<Other>>`, `Dictionary<string, Other>`. Compliant: `OtherId` strongly typed id, `Guid` id, a value object, an owned child entity (`Entity<TId>`), `IEnumerable<OwnedEntity>`, an aggregate that names another aggregate only in a method parameter and return type, a `static readonly` sentinel of an aggregate type (D1 recommended), and one `AggregateBase` and one `IAggregateRoot` implementer to cover the identification.
2. **`AggregateReferenceRuleTests`** (new): failing cases assert a non-empty failure whose text names aggregate, member and referenced type; passing cases assert `rule.Check(architecture)` does not throw; the default overload is checked against the whole test assembly and must fail naming only the violating fixtures; the predicate and namespace overloads are checked on their own.
3. **`EncinaArchitectureRulesBuilderTests`** (edit): each `EnforceAggregateReferencesById` overload adds a rule; `Verify()` throws `ArchitectureRuleException` for the violating namespace; `ApplyAllStandardRules()` does not include the rule.
4. **`ArchitectureGuardTests`** (edit, `tests/Encina.GuardTests/Testing/Architecture/`): null for the predicate overloads of the rule and the builder, null and whitespace for the namespace overloads of both.

| Flag | Scope | Per-file target (justification) |
|------|-------|---------------------------------|
| Unit | the condition, the three rule overloads, the three builder overloads | `AggregateReferenceCondition.cs` 95 (every branch reachable with in-memory fixtures: property, field, array, generic, dictionary, self, static, base-type and interface identification); `EncinaArchitectureRules.cs` 80 and `EncinaArchitectureRulesBuilder.cs` 80 (baseline measured before the change with the command below and never lowered) |
| Guard | argument checks of the six public overloads | `EncinaArchitectureRules.cs` 40 and `EncinaArchitectureRulesBuilder.cs` 40 (the guard flag reaches only the argument checks of the two files); `AggregateReferenceCondition.cs` 0 (an internal type, and `Encina.Testing.Architecture` grants `InternalsVisibleTo` to `Encina.UnitTests`, `Encina.IntegrationTests`, `Encina.ContractTests` and `Encina.PropertyTests` but not to `Encina.GuardTests`; its arguments are checked by the public overloads, which the guard tests reach) |
| Contract | none: the public surface is the rule factory and the builder; the PublicAPI file is the contract | justification in the PR |
| Property | none: a pure function over a fixed fixture set | justification in the PR |
| Integration | none: no database, transport or container | justification in the PR |

Load and benchmark: not applicable (test-time rule, not concurrent, not a hot path); a justification `.md` each under the test projects when the PR checklist asks for it.

Measure the baseline and the result from the repository root: `dotnet test tests\Encina.UnitTests --collect "XPlat Code Coverage" --results-directory artifacts\coverage\UnitTests --filter "FullyQualifiedName~Testing.Architecture"`, the same for `tests\Encina.GuardTests`, then `dotnet run --file .github/scripts/coverage-report.cs -- --input artifacts/coverage --output artifacts/coverage-report`.

5. **`.github/coverage-manifest/Encina.Testing.Architecture.json`** (edit): per-file `targets` and `justifications` for `AggregateReferenceCondition.cs` (new entry) and the two touched files (they have only `defaultTests` today), then `dotnet run --file .github/scripts/coverage-report.cs -- --check-justifications`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```
CONTEXT:
- Issue #4025, Phase 4. Shouldly through Encina.Testing.Shouldly; ArchUnitNET ArchLoader loads the test assembly as in EncinaArchitectureRulesTests.

TASK:
Write the fixtures, the rule tests, the builder tests, the guard tests and the manifest entries of the Phase 4 Tasks.

KEY RULES:
- Tests execute real rules against a real loaded architecture; no reflection-only asserts.
- Violating and compliant fixtures live in separate namespaces so each rule run is isolated.
- No Thread.Sleep, no FluentAssertions.
- Targets never go down; justify every number in one sentence.

REFERENCE FILES:
- tests/Encina.UnitTests/Testing/Architecture/EncinaArchitectureRulesTests.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaArchitectureRulesBuilderTests.cs
- tests/Encina.GuardTests/Testing/Architecture/ArchitectureGuardTests.cs
- .github/coverage-manifest/Encina.Testing.Architecture.json
```

</details>

---

### Phase 5: Documentation & Finalization

> **Goal**: The mandatory documentation set.

<details>
<summary>Tasks</summary>

1. XML docs on every new public API (`<summary>`, `<remarks>` stating "opt-in, not part of `ApplyAllStandardRules`", `<param>`, `<returns>`, `<example>` with `new EncinaArchitectureRulesBuilder(typeof(Order).Assembly).EnforceAggregateReferencesById().Verify()`).
2. `src/Encina.Testing.Architecture/README.md` (edit): a rule table row, the builder example and the opt-in note, stated as the recommended rule for every project that uses `Encina.DomainModeling`.
3. `changelog.d/4025-aggregate-references-by-id.added.md` (new): one bullet describing the rule and the builder method.
4. `PublicAPI.Unshipped.txt` (Phase 2). No ROADMAP, `docs/features`, `docs/INVENTORY.md`, ADR or release-notes change: no new package, file kind or architectural decision (the issue records "no new architecture decision").
5. Verification: `dotnet build --configuration Release` with 0 warnings; `dotnet test tests\Encina.UnitTests --filter "FullyQualifiedName~Testing.Architecture"` and the guard project pass; each flag reaches its target.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```
CONTEXT:
- Issue #4025, Phase 5. The code and tests exist.

TASK:
Finish the XML docs, the README section, the changelog fragment and run the verification commands of the Phase 5 Tasks.

KEY RULES:
- Never edit CHANGELOG.md; add the fragment under changelog.d/.
- Documentation never types coverage figures by hand.

REFERENCE FILES:
- src/Encina.Testing.Architecture/README.md
- changelog.d/README.md
```

</details>

---

### Proposed split into pull requests (limit 15 files and 1500 lines each)

One pull request. File count, verified against the paths on `origin/main` (`git ls-tree`): 11 files, 4 of them new.

| Category | Files | Notes |
|----------|-------|-------|
| Production code | 3 | `EncinaArchitectureRules.cs`, `EncinaArchitectureRulesBuilder.cs`, `AggregateReferenceCondition.cs` (new) |
| Tests | 4 | `AggregateReferenceFixtures.cs` (new), `AggregateReferenceRuleTests.cs` (new), `EncinaArchitectureRulesBuilderTests.cs`, `ArchitectureGuardTests.cs` |
| Coverage manifest | 1 | `Encina.Testing.Architecture.json` |
| PublicAPI | 1 | `PublicAPI.Unshipped.txt` |
| Documentation (README) | 1 | `README.md` |
| Changelog fragment | 1 | `4025-aggregate-references-by-id.added.md` (new) |
| Total | 11 | no csproj edit: the new condition is internal and its tests live in `Encina.UnitTests`, which already has `InternalsVisibleTo`; no ProjectReference edit |

About 780 changed lines in total, below the limit.

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| Vernon, *Effective Aggregate Design* (rule "reference other aggregates by identity") | The rule the feature enforces |
| Evans, *Domain-Driven Design*, aggregates | Consistency boundary and the root as the only entry point |
| ArchUnitNET 0.13.3 | Loaded architecture, `Class.Members`, `PropertyMember`, `FieldMember`, `FollowCustomCondition` |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `EncinaArchitectureRules`, `AggregatesShouldFollowPattern` | `src/Encina.Testing.Architecture/EncinaArchitectureRules.cs:276` | Style, argument validation and the namespace matching convention |
| `EncinaArchitectureRulesBuilder`, `EnforceSealedAggregates`, `ApplyAllSagaRules` | `src/Encina.Testing.Architecture/EncinaArchitectureRulesBuilder.cs:196,255-267` | Builder convention and the opt-in precedent |
| `EventIdUniquenessRule` | `src/Encina.Testing.Architecture/EventIdUniquenessRule.cs` | Fallback precedent for a reflection-based rule |
| `AggregateRoot<TId>`, `AggregateBase`, `IAggregateRoot`, `IAggregate` | `src/Encina.DomainModeling/` | The names matched by the default identification and the fixture bases |
| `EncinaArchitectureRulesTests` | `tests/Encina.UnitTests/Testing/Architecture/` | Test style and architecture loading |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Testing.Architecture` | none | A test-time rule logs nothing; no `EventIdRanges.cs` field, no registry row and no `PublicAPI` field line |

### Provider Matrix

| Category | Applies | Notes |
|----------|---------|-------|
| Database (10), caching (8), transports, locks, validation, cloud | No | The rule reads loaded assemblies only (`AGENTS.md` section 5, applicability matrix) |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production code | 3 | 1 new |
| Tests | 4 | 2 new |
| Coverage manifest | 1 | edit |
| PublicAPI | 1 | edit |
| Documentation (README) | 1 | edit |
| Changelog fragment | 1 | new |
| Total | 11 | 4 new (local model draft of the table, reviewed against the file list) |

<!-- local-draft: D:\Proyectos\Encina\.claude\worktrees\pb-e\artifacts\local-ai\out\p4025-filecount.md -->



---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; no [Obsolete]; XML docs on every public API; zero warnings.
- Issue #4025: an opt-in architecture rule in src/Encina.Testing.Architecture that fails when an aggregate has a property or field whose type is another aggregate, an array or a generic collection of one. Plan: docs/plans/aggregate-references-by-id-implementation-plan-4025.md. Dilemmas D1 (static members) and D2 (entities owned by the aggregate) are recommended, pending the maintainer; read the Maintainer Decisions section first if it exists.

IMPLEMENTATION OVERVIEW:
1. Phase 1: internal AggregateReferenceCondition (Evaluate, ReferencedAggregate, IsDefaultAggregate), matching Encina.DomainModeling base types by FullName.
2. Phase 2: three AggregatesShouldNotReferenceOtherAggregates overloads, three EnforceAggregateReferencesById overloads, six PublicAPI.Unshipped.txt lines.
3. Phase 3: no telemetry, logging, DI or EventIds; the rule is not in ApplyAllStandardRules.
4. Phase 4: fixtures in two namespaces, rule tests, builder tests, guard tests, manifest targets.
5. Phase 5: XML docs, README, changelog fragment, verification.

KEY PATTERNS:
- Same style and argument validation as AggregatesShouldFollowPattern (EncinaArchitectureRules.cs:276).
- Violation text names aggregate, member and referenced type; never a value.
- Tests execute real rules against a real loaded architecture; per-flag targets from the manifest; CRAP at most 10.

REFERENCE FILES:
- src/Encina.Testing.Architecture/EncinaArchitectureRules.cs
- src/Encina.Testing.Architecture/EncinaArchitectureRulesBuilder.cs
- src/Encina.Testing.Architecture/EventIdUniquenessRule.cs
- src/Encina.DomainModeling/AggregateRoot.cs, AggregateBase.cs, IAggregateRoot.cs, IAggregate.cs
- docs/plans/dsr-implementation-plan-404.md (style)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: a test-time rule reads no data worth caching |
| 2 | OpenTelemetry | ❌ | Not applicable: runs inside a test process; no tenant tag applies (ADR-054 D4 concerns runtime telemetry) |
| 3 | Structured Logging | ❌ | Not applicable: no logging; no EventId range is registered |
| 4 | Health Checks | ❌ | Not applicable: no runtime dependency |
| 5 | Validation | ❌ | Not applicable: the only inputs are the overload arguments, checked with `ArgumentException` guards like every other rule of the package |
| 6 | Resilience | ❌ | Not applicable: no external call |
| 7 | Distributed Locks | ❌ | Not applicable: no shared state |
| 8 | Transactions | ❌ | Not applicable: no persistence |
| 9 | Idempotency | ❌ | Not applicable: a pure function of the loaded assemblies |
| 10 | Multi-Tenancy | ❌ | Not applicable: no tenant data; no id is built, so the reserved `default` tenant segment does not apply |
| 11 | Module Isolation | ❌ | Not applicable: the rule input is the assemblies the caller passes in |
| 12 | Audit Trail | ❌ | Not applicable: no compliance event; the rule fails closed in the sense that a violation always fails the test |

---

## Prerequisites & Dependencies

- No blocking issue: `AggregateRoot<TId>`, `AggregateBase`, `IAggregateRoot` and `IAggregate` exist on `origin/main`; ArchUnitNET 0.13.3 is already referenced.
- Siblings: #1038 (repository versus query-handler rule) is independent and shares no code; EPIC #1037 is the parent.
- Deferred inside this plan (not a new issue): if the maintainer answers D2 with option B, the rule also walks the entity types owned by the aggregate; that adds a graph walk with cycle protection to `AggregateReferenceCondition` and its own fixtures (`OrderLine.Product`), about 120 lines and 2 test files, still one pull request.

---

## Next Steps

1. The maintainer answers Dilemmas D1 and D2 when work on #4025 starts; the answers go into `## Maintainer Decisions` at the end of this plan.
2. Brief the single pull request: 11 files, about 780 lines.
3. After the merge, the README of `Encina.Testing.Architecture` names the rule as recommended for every project that uses `Encina.DomainModeling`.
