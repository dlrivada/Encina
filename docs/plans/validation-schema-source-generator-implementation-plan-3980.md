# Implementation Plan: Compile-Time (Source-Generated) Form of Encina.Validation.Schema Schemas

> **Issue**: [#3980](https://github.com/dlrivada/Encina/issues/3980)
> **Type**: Feature (validation, source generators)
> **Related**: #227 (owns the one Roslyn generator project `Encina.Validation.Generators`, the `ENCVAL001` to `ENCVAL004` diagnostics, `GeneratorTestHost` and the per-assembly registration extension; a blocking prerequisite of S2), #235 (owns the runtime schemas: `ISchema<T>`, `SchemaBase<T>`, `Schema.Object<T>`, the schema codes, limits, telemetry and `SchemaRequestValidator<T>`; a blocking prerequisite of S1), #230 (owns `IRequestValidator<T>` and the runner the schemas run under), #233 (owns `ValidationError.ErrorCode` and the message keys), #726 (central validation instrumentation), ADR-054, ADR-062 (merged: error category contract)
> **Complexity**: High (a second generator over the project of #227, a public seam in the runtime package, equivalence with the runtime schemas)
> **Estimated Scope**: ~1,900 lines of production code + ~4,000 lines of tests (about 900 of them snapshots) + ~450 lines of documentation (~6,350 lines in total, as the Estimated File Count table), in seven pull requests of at most 15 files and 1,500 lines each
> **Note on #227 and #235**: every statement about their types follows their plans (plan pull requests, not code on `main`); each is re-verified against `main` when the pull request that uses it starts

---

## Summary

### What exists today (read on 2026-11-24, main `278c164f1`)

- Neither package exists in `src`: `git grep -n -E "Encina.Validation.Schema|Encina.Validation.Generators" -- src` is empty; the Roslyn component in `src` is `Encina.Security.ABAC.Analyzers` (netstandard2.0, analyzer-packaged, listed in `Encina.slnx:48`) and `Microsoft.CodeAnalysis.CSharp` 5.0.0 and `Verify` 31.15.0 are central package versions (`Directory.Packages.props:169,206`).
- #227 plans `Encina.Validation.Generators`: an incremental generator (`IIncrementalGenerator`, `ForAttributeWithMetadataName`, equatable value models), a parser, four diagnostics `ENCVAL001` to `ENCVAL004`, snapshot tests with Verify, a generated per-assembly registration extension `Add<AssemblyName>GeneratedValidators`, and `GeneratorTestHost` in `tests/Encina.TestInfrastructure/Validation/`.
- #235 plans `Encina.Validation.Schema`: immutable schemas (`Schema.Object<T>(o => o.Property(x => x.Name, p => p.String().Min(1)))`), `ISchema<T>` with `Validate(T)` (a `ValidationResult`) and `Parse(JsonElement | string)` (`Either<EncinaError, T>`, code `validation.schema_failed`), a public abstract `SchemaBase<T>` with an `internal` constructor and internal abstract hooks (`Normalize`, `ValidateCore`), limits (depth, nodes, input bytes, issues, regex time), telemetry (`Encina.Validation.Schema` source and meter) and EventIds 620-622 in the range `ValidationSchema` 620-629. Its Design Choice 6 records that the compile-time form is a follow-up on #227's project, which is this issue.
- The runtime schema reads property selectors from expression trees and uses reflection over `T` for names and for the deserialization of `Parse`, so it is not AOT- or trimming-safe (the #235 plan records it as a known limit). Type-changing composition (`Extend`, `Pick`, `Omit` yielding a new output type) cannot be written with C# generics; the runtime schemas compose over the same `T`.
- An internal member of the runtime package is invisible to generated code: a generated class lives in the user's assembly, and `InternalsVisibleTo` cannot name every user assembly, so anything generated code needs from the runtime package must be a public seam (register row C3).

### What this plan delivers

A source generator, in the project of #227 (no second Roslyn project), that turns a schema marked `[GenerateSchema]` into a reflection-free partial class deriving from a new public base of the runtime package, with the same issue codes, arguments, limits and telemetry as the runtime schema:

1. **A public seam in `Encina.Validation.Schema` (S1)**: `GenerateSchemaAttribute`, `GeneratedSchemaBase<T>` (public abstract, protected constructor, protected abstract hooks that generated code overrides) and `GeneratedSchemaContext` (issue collection with the runtime's codes and limits). `SchemaBase<T>` and `GeneratedSchemaBase<T>` share one internal core, so `Parse` limits, telemetry and the failure mapping are implemented once; the reflective parts of the runtime (selector expression trees, `JsonSerializer` default resolution) stay in `SchemaBase<T>` only, so the generated path never reaches them (register row C12).
2. **The generator front end (S2)**: a second `IIncrementalGenerator` in `Encina.Validation.Generators`, a definition parser and new diagnostics `ENCVAL100` to `ENCVAL10x` for a shape it cannot generate (a compile error, never a silent difference).
3. **Emission (S3, S4)**: primitives (string, numbers, boolean, date), then objects, arrays and discriminated unions, `Normalize` over `JsonNode` and registration through the per-assembly extension of #227.
4. **Proof (S5)**: an equivalence contract test (generated versus runtime for the same definition), a no-reflection test over the generated syntax, integration through `IEncina.Send` under the three validation providers, and a benchmark.
5. **Composed types (S6, conditional on Design Choice 2 option A)**: generated output types for `Extend`, `Pick` and `Omit`.
6. **Documentation (S7)**.

Nothing changes for an application that does not mark a schema for generation, and the runtime schemas stay as the reflection-based default.

- Affected packages: `Encina.Validation.Schema` (the public seam), `Encina.Validation.Generators` (the generator), `Encina.TestInfrastructure` (reuses `GeneratorTestHost`). Core `Encina`, the three provider packages and the EventId registry are untouched (no new EventIds, register row C6).
- Provider category: Validation (3) (`AGENTS.md` section 5): a generated schema runs through the `IRequestValidator<T>` adapter of #235 and the runner of #230, so FluentValidation, DataAnnotations and MiniValidator behave the same; S5 proves it per provider.
- Shared pieces: owned by #227 and #235 (consumed here). Owned by this plan: `GeneratedSchemaBase<T>`, `GeneratedSchemaContext`, `GenerateSchemaAttribute` and the diagnostics `ENCVAL100` and up.

---

## Design Choices

<details>
<summary><strong>1. The authoring surface (the issue's own Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Attribute-driven on the target type: `[GenerateSchema]` on a type whose properties carry the DataAnnotations and Encina attributes of #227, and the generator emits the schema from them** | No new syntax; a smaller generator (the attribute model of #227 exists); no expression interpretation | The fluent definition and the attributes can drift if both exist; attributes cannot express object-level `Refine`, unions or every fluent rule |
| **B) The generator reads a schema definition written with the fluent API in a non-static `[GenerateSchema] public sealed partial class UserSchema` that holds one `private static ISchema<User> Define() => Schema.Object<User>(...)` method; the generator emits the other part of the class (the base type `GeneratedSchemaBase<User>`, the constructor and a static `Instance`). The issue's sketch `static partial class` cannot be used: C# does not allow a base class on a static class (CS0713), and `Define()` stays valid runtime code, so the equivalence tests can call it** | One source of truth, the fluent API of #235 is the product; every fluent shape is available | The generator interprets C# expressions (selectors, lambdas), which is complex and fragile; every unsupported shape needs a diagnostic |

### Chosen Option: **B — Fluent definition read by the generator, common shapes only, a diagnostic for the rest** (recommended, pending the maintainer)

### Rationale

- The issue recommends B: the schema is already written once for the runtime; generating from the same text keeps one source of truth. The supported subset is the common shapes of #235 (primitives and their rules, optional, nullable, default, objects with property selectors, arrays, unions); a lambda body the generator cannot interpret (a `Refine` delegate with arbitrary code) is copied verbatim into the generated class where possible and otherwise reported by `ENCVAL10x`.
- A wins when the maintainer wants a small first version and accepts that schemas with `Refine`, unions or composition stay runtime-only.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Whether this issue also emits composed output types (`Extend`, `Pick`, `Omit`)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Yes, as the last two pull requests (S6): the generator emits a new record or class for the result of `Pick`, `Omit` and `Extend`, with the generated schema for it** | Delivers what only generation can: type-changing composition (the issue's second motivation) | The most complex emitter; names, nullability and constructors of the new types; it must wait for the rest and can be dropped without harming S1 to S5 |
| **B) No: the composed types are not in 1.0; generated schemas compose over the same `T` like the runtime ones** | Smaller; S1 to S5 are a complete feature | The type-changing motivation of the issue stays unmet |

### Chosen Option: **A — Planned as the last pull requests, independent of S1 to S5** (recommended, pending the maintainer)

### Rationale

- The issue lists the composed types as something the generator "can also emit"; a plan that omitted them would leave a stated motivation without work, and S6 is separable: if the maintainer chooses B, S6 is not opened and nothing else changes.
- B wins when 1.0 needs only the reflection-free validation and the composed-type feature is judged low value.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. The shape of generated code: a public base with hooks, or direct `ISchema<T>` with public helpers</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Generated classes derive from the public `GeneratedSchemaBase<T>`, whose `Validate` and `Parse` are implemented once (limits, telemetry, failure mapping) and call protected hooks the generator overrides (`ValidateCore`, `Normalize`)** | Equivalence of limits, codes, telemetry and the never-throws rule by construction; the generator emits only the per-type rules | A public abstract surface (marked `EditorBrowsable(Never)`) that is part of the package's API; `SchemaBase<T>` is refactored to share the core |
| **B) Generated classes implement `ISchema<T>` directly and call public static helpers (`GeneratedSchemaSupport`) for limits and issue collection** | No base class in the generated type's hierarchy; the generated class can derive from something else | Every generated class repeats the `Validate`/`Parse` plumbing; equivalence of the plumbing rests on tests |
| **C) Inline everything in the generated class, no runtime dependency beyond `ISchema<T>`** | Smallest runtime surface | Duplicates the semantics of every rule and of the limits; drift is likely; telemetry cannot be shared |

### Chosen Option: **A — A public base with hooks** (recommended, pending the maintainer)

### Rationale

- The acceptance criterion is "same `ValidationResult`, codes and categories as the runtime schema"; A makes the plumbing identical by construction and leaves the generator to emit rules only. Generated code cannot use `internal` members of another assembly, so the seam is public by necessity (register row C3).
- B wins when generated classes must derive from an application base class; C wins when the runtime package must expose no new public type.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. How `Parse` produces a `T` without reflection</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The generated `Parse` normalizes the input as a `JsonNode` (reflection-free) and deserializes with the `JsonTypeInfo` of `T` that the application's `System.Text.Json` source-generation context provides through the existing `SchemaParseOptions.JsonSerializerOptions.TypeInfoResolver` (register row C8)** | Reuses the maintained System.Text.Json generator; AOT-safe; the generator stays small | The application needs a `JsonSerializerContext` for each parsed type; a missing `JsonTypeInfo<T>` is a defined `Left` (register row C8), not a fallback to reflection |
| **B) The generator also emits the construction of `T` from the `JsonElement` (explicit property reads and a constructor or object-initializer call)** | No serializer context needed; fully self-contained | A deserializer inside the generator: constructors, init-only properties, nullability, enums, collections; large and a second source of truth for naming policies |

### Chosen Option: **A — Type info from the application's serializer context** (recommended, pending the maintainer)

### Rationale

- The acceptance criterion is "reflection-free and AOT-compatible"; A meets it with the platform's own generator and keeps S4 within one pull request. `Validate(T)` (the path a bound request uses) needs no serializer at all.
- B wins when applications refuse a `JsonSerializerContext` and the maintainer accepts the emitter cost.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Where the schema generator lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A second generator class in the project of #227 (`Encina.Validation.Generators`)** | One Roslyn toolchain to build, test and ship; shared harness, models and registration emitter | The project grows by about six files |
| **B) A new project `Encina.Validation.Schema.Generators`** | Separate release unit | A second analyzer package and test harness; duplicated parser plumbing |

### Chosen Option: **A — The project of #227**

### Rationale

- The issue states the placement and the reason (one generator project avoids two Roslyn toolchains).

**Class:** Trivial
**Settled by:** the issue's Alternatives Considered row 1 ("the generator lives in the project of #227, not a second Roslyn project") and the title of #227 ("Source-Generated Validation (Encina.Validation.Generators)")

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | Requirement, not a mechanism: a generated schema returns the same `ValidationResult`, issue codes (`validation.<rule>`), arguments, categories (ADR-062: `validation.schema_failed` is `Validation`, `validation.schema_limit_exceeded` is `InvalidRequest`, a failing user delegate is `Internal`) and the same limits as the runtime schema for the same definition; the S5 equivalence test enforces it under every option of Design Choice 3 (the mechanism that makes it hold, the base or helpers or inlined rules, is that Design Choice's) | Settled | `AGENTS.md` section 5 (the validation category stays consistent across providers and implementations); ADR-062 (categories); the issue's acceptance criterion 1 |
| C2 | A shape the generator cannot generate in a type marked `[GenerateSchema]` is a compile error `ENCVAL100` to `ENCVAL10x` (unsupported expression, unknown rule, a lambda it cannot copy, an output-type name clash), never a silent skip. Option B: a warning and the type keeps working through the runtime path | Dilemma (recommended, pending the maintainer): error | the issue's acceptance criterion 3 ("a compiler diagnostic, never a silent difference"); #227 register row C2 chose the same severity question; a warning wins when a build must never break on a marked type |
| C3 | Whatever generated code uses from the runtime package is public (the attribute under every option of Design Choice 3, and under option A or B the base, the context or the helpers), marked `[EditorBrowsable(EditorBrowsableState.Never)]` where it is not authoring surface and listed in `PublicAPI.Unshipped.txt`; no `InternalsVisibleTo` is used for generated code | Settled | generated code compiles in the user's assembly, which an `InternalsVisibleTo` list cannot name; `src/Encina.Validation.Schema` (#235 Design Choice: `SchemaBase<T>` has an `internal` constructor) is the limit this overcomes |
| C4 | Generated schemas are stateless and immutable (static readonly rule tables, no instance state), safe to share across threads and requests | Trivial | `IValidationProvider`'s own contract ("implementations should be thread-safe and reusable", `src/Encina/Validation/IValidationProvider.cs:12`) |
| C5 | Regex rules emit a `static readonly Regex` built with the same options and match timeout as the runtime schema (the non-backtracking engine and the timeout of #235 register rows C2 and C3), not `[GeneratedRegex]`. Option B: `[GeneratedRegex]` for speed and compile-time validation of patterns | Dilemma (recommended, pending the maintainer) | identical engine semantics keep the equivalence of C1 by construction; `[GeneratedRegex]` wins when the pattern set is fixed and start-up cost matters, at the cost of verifying engine equivalence by tests |
| C6 | No new EventId and no new range: generated schemas log through the package's schema logging (`SchemaLog` EventIds 620-622 of #235, range `ValidationSchema` 620-629); compile-time diagnostics are Roslyn ids, not EventIds | Trivial | `AGENTS.md` section 7 (no range is allocated when no log message is added); the issue's Cross-Cutting row 3 ("Reuses the EventIds 620-629") |
| C7 | Requirement: telemetry of a generated schema is the package's (under option C of Design Choice 3 the inlined code must call public telemetry helpers to meet it, which makes C converge on B): the `Encina.Validation.Schema` activity source and meter of #235, tenant tag only when a tenant is present on the parse options (ADR-054 D4), schema name a bounded constant chosen at generation (the type name), never request data | Settled | ADR-054 D3 and D4 (one source and meter per package, tenant tag only when present); a generated schema is part of the same package's surface whichever shape Design Choice 3 gives it |
| C8 | The carrier of the serializer metadata is the existing `SchemaParseOptions.JsonSerializerOptions` (a member of #235, no new member): the generated `Parse` asks `options.JsonSerializerOptions.TryGetTypeInfo(typeof(T), out var info)` and requires a non-null `TypeInfoResolver`; with no type info for `T` it returns a `Left` `validation.schema_failed` (code only, no input) and never calls the reflection-based default; `Validate(T)` needs none | Dilemma (recommended, pending the maintainer) | follows Design Choice 4 option A; under option B the row does not arise; the fail-closed reading (a configuration gap denies) is why it is a `Left` and not a pass |
| C9 | A generator diagnostic never contains user data: the id, the type name and the member name only | Trivial | Roslyn diagnostics are compile-time messages of identifiers; `AGENTS.md` section 3 (no data in diagnostics) |
| C10 | The generator project keeps the build rules of #227 (netstandard2.0, `IsRoslynComponent`, `EnforceExtendedAnalyzerRules`, `IncludeBuildOutput=false`, `NoWarn` RS1038 and NU5128 as `src/Encina.Security.ABAC.Analyzers` does), and the diagnostics of this plan follow whatever #227 register row C15 decides about `AnalyzerReleases.*.md` (option A: not shipped; option B: two files, and S2 grows by two files, still within 15) | Dilemma (recommended, pending the maintainer): follow #227 C15 | the build rules are the model in `src/Encina.Security.ABAC.Analyzers/Encina.Security.ABAC.Analyzers.csproj`; the release-tracking choice is #227's pending Dilemma and this plan does not decide it separately |
| C12 | The reflection-free and AOT claim is proven by a build, not by a syntax scan alone: the S5 sample project sets `IsAotCompatible` (trim and AOT analyzers) and `TreatWarningsAsErrors`, compiles the generated schema, the generated registration and a call of `Validate` and `Parse` with a source-generated `JsonSerializerContext`, so any IL2026, IL3050 or IL2070-family warning on the generated path fails the build; the syntax test of S3 remains a fast first check. Option B: a Native AOT publish and run of the sample in CI | Dilemma (recommended, pending the maintainer): analyzer-clean build | the analyzer build needs no native toolchain and fails on the same warnings the AOT publish would report; the publish wins when a runtime failure that analyzers cannot see (a missing serializer metadata path) must be caught, at the cost of a native toolchain on the runner |
| C13 | Registration: the schema generator emits its own per-assembly extension `Add<SanitizedAssemblyName>GeneratedSchemas(this IServiceCollection)` (a distinct class and hint name from the `...GeneratedValidators` extension of #227), calling `AddEncinaSchema` registrations with `TryAddEnumerable`. Option B: one combined generator that collects both marker attributes and owns one extension | Dilemma (recommended, pending the maintainer): distinct extensions | two generators in one project cannot read each other's output and a shared class name would collide (duplicate type); distinct names need the application to call two methods, which the combined generator would avoid at the cost of merging #227's generator and this one |
| C11 | Tenancy and modules: generation is build-time and composes no tenant-qualified id; a schema-backed validator is scoped to a tenant or module by registering an `IScopedRequestValidator<T>` (#3982) that delegates to the generated schema's `Validate`, so no generator change is needed and the reserved `default` segment does not arise | Trivial | `AGENTS.md` section 6 rows 10 and 11 (evaluated: build-time feature); #3982's contract is a plain interface over `IRequestValidator<T>` |

---

## Implementation Phases

### Phase 1: The Public Seam in the Runtime Package

> **Goal**: Generated code can derive from the runtime machinery without internals; tested with a hand-written derived schema (pull request S1; needs #235 S1 to S7: the shared core carries the telemetry calls that #235 creates in its S7).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** in `src/Encina.Validation.Schema/`: `GenerateSchemaAttribute.cs` (`[AttributeUsage(Class)] public sealed class GenerateSchemaAttribute : Attribute`; the generator finds it by metadata name `Encina.Validation.Schema.GenerateSchemaAttribute`), `GeneratedSchemaContext.cs` (`public sealed class`; `void AddIssue(string path, string propertyName, string code, params object?[] arguments)` honoring `MaxIssues` and the truncation of #235 register row C21; `bool IsFull`; no input value is ever stored) and `GeneratedSchemaBase.cs` (`public abstract class GeneratedSchemaBase<T> : ISchema<T>` with a `protected` constructor taking the bounded schema name, `Validate` and `Parse` implemented once, and `protected abstract void ValidateCore(T value, GeneratedSchemaContext context)` and `protected virtual JsonNode? Normalize(JsonNode? input, GeneratedSchemaContext context) => input` (a virtual identity default, so the class emitted by S3, which overrides only `ValidateCore`, compiles and builds alone; S4 emits the real override); all three types carry `[EditorBrowsable(EditorBrowsableState.Never)]` and full XML docs).
2. **Modify** `SchemaBase.cs` (#235): move the shared core (limit enforcement, the never-throws wrapper, failure mapping, telemetry calls) into an internal helper both bases use, so the behavior exists once (register row C1). **Modify** `PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.Validation.Schema.json` (through `mechanical-fixer`).
3. Tests (S1): `GeneratedSchemaBaseTests` (a hand-written `UserSchema : GeneratedSchemaBase<User>` gives the same `Validate` and `Parse` results, limits and truncation as the equivalent runtime `Schema.Object<User>` for a corpus of valid, invalid and hostile inputs; a throwing hook gives an invalid result with the reserved code, never an exception), `GeneratedSchemaContextTests` (issue cap, truncation flag, no value stored), `GeneratedSchemaGuardTests` (null and blank arguments of the context and the base constructor; the attribute needs none), `GeneratedSchemaContractTests` (the generated base and `SchemaBase<T>` satisfy the same `ISchema<T>` contract over the same fixtures).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0, no backward compatibility). Issue #3980 adds a source-generated form of
Encina.Validation.Schema schemas. Read docs/plans/validation-schema-source-generator-implementation-plan-3980.md (Design
Choices 1-5, register rows C1-C11) and docs/plans/validation-schema-builder-implementation-plan-235.md (SchemaBase<T>, limits).

TASK:
1. Create GenerateSchemaAttribute, GeneratedSchemaContext and GeneratedSchemaBase<T> (public, EditorBrowsable Never) in
   src/Encina.Validation.Schema/. Refactor SchemaBase<T> so both bases share one internal core.
2. Prove with a hand-written derived schema that Validate and Parse equal the runtime schema's results.

KEY RULES:
- Generated code compiles in user assemblies: everything it uses must be public; no InternalsVisibleTo.
- Never store or log an input value; codes, paths and counts only. XML docs on every public member.
- PublicAPI.Unshipped.txt and the manifest through mechanical-fixer; tests execute real code.

REFERENCE FILES:
- src/Encina.Validation.Schema/SchemaBase.cs and ISchema.cs (from #235)
```

</details>

---

### Phase 2: Generator Front End and Diagnostics

> **Goal**: The second generator finds marked schemas, parses their definitions and reports unsupported shapes (pull request S2; needs #227 S2).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** in `src/Encina.Validation.Generators/`: `SchemaGenerator.cs` (`[Generator] public sealed class SchemaGenerator : IIncrementalGenerator`, `ForAttributeWithMetadataName("Encina.Validation.Schema.GenerateSchemaAttribute")`, equatable value models, no `Compilation` captured), `Models/SchemaDefinitionModel.cs`, `Models/SchemaRuleModel.cs`, `Parsing/SchemaDefinitionParser.cs` (interprets the fluent definition of Design Choice 1 option B over the syntax tree and semantic model: `Schema.Object<T>(...)`, `Property(selector, p => ...)`, the primitive rule calls, `Optional`, `Nullable`, `Default`, `Array`, `DiscriminatedUnion`; a shape outside the subset yields a diagnostic, never a partial model).
2. **Modify** `DiagnosticDescriptors.cs` (#227): `ENCVAL100` unsupported expression, `ENCVAL101` unknown or unsupported rule, `ENCVAL102` a lambda that cannot be generated, `ENCVAL103` generated name clash (register row C2; messages carry identifiers only, row C9).
3. **Modify** `.github/coverage-manifest/Encina.Validation.Generators.json` (through `mechanical-fixer`).
4. Tests (S2): `SchemaDefinitionParserTests` (each supported shape yields the expected model; each unsupported shape yields exactly its diagnostic), `SchemaDiagnosticsTests` (ids, severity error, messages carry no literal values), `SchemaGeneratorIncrementalityTests` (an unrelated edit does not re-run the model step; the same input yields equal models), driven by the shared `GeneratorTestHost` of #227.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #3980, plan docs/plans/validation-schema-source-generator-implementation-plan-3980.md, phase 2 of 7. Depends on S1 and
on #227 S2 (the generator project, DiagnosticDescriptors, GeneratorTestHost).

TASK:
1. Add SchemaGenerator (incremental), the two models and SchemaDefinitionParser to src/Encina.Validation.Generators/.
2. Add diagnostics ENCVAL100-103 and the three test classes.

KEY RULES:
- Incremental: equatable models, no Compilation or ISymbol captured in a model. Diagnostics carry identifiers only.
- A shape the parser does not support is a compile error, never a silent skip.

REFERENCE FILES:
- docs/plans/validation-source-generators-implementation-plan-227.md, src/Encina.Security.ABAC.Analyzers/
```

</details>

---

### Phase 3: Emission of Primitive Rules

> **Goal**: Generated schemas for string, number, boolean and date rules (pull request S3; needs S2).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `Emit/SchemaEmitter.cs` (the second part of the marker's `partial class`, legal C#: `partial class UserSchema : GeneratedSchemaBase<User>` with a private constructor, a `public static UserSchema Instance` and an `override ValidateCore`; a user member named `Instance` is `ENCVAL103`; the marker is never `static`) and `Emit/PrimitiveRuleEmitter.cs` (one rule per call: `Min`, `Max`, `Length`, `Email`, `Url`, `Guid`, `Regex` as in register row C5, `Positive`, `Integer`, `Past`, `Future` with the `TimeProvider` of the parse options, `OneOf`), each emitting the issue code and arguments of #235's `validation.<rule>` key convention.
2. **Modify** `SchemaGenerator.cs` to call the emitter. **Modify** the manifest (through `mechanical-fixer`).
3. Tests (S3): `SchemaEmissionSnapshotTests` with Verify snapshots for one schema per rule family (three snapshot files), `PrimitiveEquivalenceProperties` (FsCheck: for any value the generated and the runtime schema built from the same definition give the same `ValidationResult`), and `SchemaEmissionNoReflectionTests` (the generated syntax references no `System.Reflection`, `Expression<>`, `Activator` or `typeof` member lookup, checked with the semantic model).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #3980, plan docs/plans/validation-schema-source-generator-implementation-plan-3980.md, phase 3 of 7. Depends on S2.

TASK:
1. Emit the partial schema class for primitive rules, deriving from GeneratedSchemaBase<T> and overriding ValidateCore.
2. Write the snapshots, the equivalence property and the no-reflection test.

KEY RULES:
- Same codes, arguments, bounds and clock behavior as the runtime rule; Regex built with the runtime's options and timeout.
- Generated code is deterministic (stable ordering) so snapshots do not flap.

REFERENCE FILES:
- docs/plans/validation-schema-builder-implementation-plan-235.md (rule semantics), src/Encina.Validation.Schema/
```

</details>

---

### Phase 4: Objects, Arrays, Unions, Normalize and Registration

> **Goal**: Complete generated schemas, `Parse` through `JsonNode`, and registration (pull request S4; needs S3).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `Emit/ObjectRuleEmitter.cs` (property reads by direct access, nested schemas, `Strict` and `Strip`), `Emit/CollectionRuleEmitter.cs` (arrays with indexed paths, unions by discriminator) and `Emit/NormalizeEmitter.cs` (the `Normalize` override over `JsonNode`: unknown members, defaults, coercion when enabled, duplicate keys rejected as in #235 register row C20; deserialization goes through the type info resolved from the parse options, Design Choice 4 option A, register row C8).
2. **Create** `Emit/SchemaRegistrationEmitter.cs` (the same `Emit/` folder #227 uses for its emitters; the S3 and S4 emitter paths of this plan are `Emit/` too): the per-assembly extension `Add<SanitizedAssemblyName>GeneratedSchemas` of register row C13, registering each generated schema as `SchemaRequestValidator<T>` through the adapter of #235 with `TryAddEnumerable`, so a generated schema is picked up by the runner of #230; #227's `RegistrationEmitter.cs` is not touched. **Modify** `SchemaGenerator.cs` and the manifest (through `mechanical-fixer`).
3. Tests (S4): snapshot tests for an object with nested object and array, a discriminated union and a `Strict` and a `Strip` object (four snapshot files), `ObjectEquivalenceProperties` (generated and runtime agree for random objects, including unknown members and duplicate keys), `GeneratedParseTests` (limits enforced through the base; a missing `JsonTypeInfo<T>` is the `Left` of register row C8), `GeneratedSchemaRegistrationTests` (a compiled sample resolves its generated validator from a provider built with `ValidateOnBuild` and `ValidateScopes`, and registering twice runs it once).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Issue #3980, plan docs/plans/validation-schema-source-generator-implementation-plan-3980.md, phase 4 of 7. Depends on S3 and on
#227 S5 (RegistrationEmitter) and #235 S6 (SchemaRequestValidator).

TASK:
1. Emit object, array and union schemas, the Normalize hook over JsonNode and the registration of each generated schema.
2. Write the snapshots, the equivalence property, the parse tests and the registration DI test.

KEY RULES:
- Reflection-free: direct property access only; deserialization through the JsonTypeInfo<T> of the parse options.
- Registration with TryAddEnumerable; a registration DI test with ValidateOnBuild and ValidateScopes.

REFERENCE FILES:
- docs/plans/validation-source-generators-implementation-plan-227.md (RegistrationEmitter), docs/plans/validation-schema-builder-implementation-plan-235.md
```

</details>

---

### Phase 5: Equivalence, Integration and Benchmark

> **Goal**: The generated and runtime schemas are proven equal end to end under the three providers (pull request S5; needs S4).

<details>
<summary><strong>Tasks</strong></summary>

0. **Create** the sample project `tests/Encina.Validation.Generated.Sample/` (a `.csproj` with `IsAotCompatible` and `TreatWarningsAsErrors`, the generator referenced as an analyzer, one source file with the issue's `User`, a `UserSchema` marker and a source-generated `JsonSerializerContext`) and add it to `Encina.slnx`; **modify** `tests/Encina.ContractTests/Encina.ContractTests.csproj`, `tests/Encina.IntegrationTests/Encina.IntegrationTests.csproj` and `tests/Encina.BenchmarkTests/Encina.Benchmarks/Encina.Benchmarks.csproj` (a ProjectReference to the sample; the proof of register row C12 is that the sample builds without a warning).
1. **Create** `tests/Encina.ContractTests/Core/Validation/GeneratedSchemaEquivalenceContractTests.cs`: using the sample project and the issue's `User` definition, the same inputs through the generated and the runtime schema, the same `ValidationResult`, codes, categories and `Parse` results.
2. **Create** `tests/Encina.IntegrationTests/Validation/{DataAnnotations,FluentValidation,MiniValidator}/GeneratedSchemaPipelineTests.cs`: a request through `IEncina.Send` with the real pipeline behavior and a registered generated schema; no container.
3. **Create** `tests/Encina.BenchmarkTests/Encina.Benchmarks/GeneratedSchemaBenchmarks.cs` (generated versus runtime `Validate`, `BenchmarkSwitcher` conventions of `AGENTS.md` section 9); the filter is verified with `--list flat`.
4. Tests execute the real generator (the sample project is compiled with it by MSBuild) and the S3 and S4 tests through `GeneratorTestHost`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
Issue #3980, plan docs/plans/validation-schema-source-generator-implementation-plan-3980.md, phase 5 of 7. Depends on S4.

TASK:
1. Write the equivalence contract test, the three integration tests through IEncina.Send and the benchmark.

KEY RULES:
- Real generator and real providers; no reflection-only tests; Shouldly through Encina.Testing.Shouldly.
- Benchmarks use BenchmarkSwitcher.FromAssembly, never BenchmarkRunner.Run<T>().

REFERENCE FILES:
- tests/Encina.IntegrationTests/Validation/ (#230 S5), tests/Encina.BenchmarkTests/Encina.Benchmarks/ValidationBenchmarks.cs
```

</details>

---

### Phase 6: Composed Output Types (conditional on Design Choice 2 option A)

> **Goal**: `Extend`, `Pick` and `Omit` produce new generated types (pull request S6).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `Emit/CompositionEmitter.cs`: for a definition ending in `Pick(...)`, `Omit(...)` or `Extend(...)` the generator emits a new `partial` record named by the marker (`[GenerateSchema(OutputType = "UserSummary")]` is added to the attribute in S6, a public member listed in `PublicAPI`), with the selected properties, and its generated schema; a name clash is `ENCVAL103`, an unknown property `ENCVAL104`.
2. **Modify** `GenerateSchemaAttribute.cs` (`OutputType`), `SchemaGenerator.cs`, `DiagnosticDescriptors.cs`, `PublicAPI.Unshipped.txt` and the two manifests (through `mechanical-fixer`).
3. Tests (S6): snapshot tests for `Pick`, `Omit` and `Extend`, `CompositionEmissionTests` (nullability and property order preserved, diagnostics), `CompositionEquivalenceProperties` (the output schema equals the runtime composition over the same `T` for the shared properties).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
Issue #3980, plan docs/plans/validation-schema-source-generator-implementation-plan-3980.md, phase 6 of 7. Depends on S4; skip
when the maintainer chose option B of Design Choice 2.

TASK:
1. Emit the new output types of Pick, Omit and Extend and their generated schemas.

KEY RULES:
- Names, nullability and property order are deterministic; diagnostics carry identifiers only.

REFERENCE FILES:
- docs/plans/validation-schema-builder-implementation-plan-235.md (composition over the same T)
```

</details>

---

### Phase 7: Documentation and Finalization

> **Goal**: Usage, READMEs, changelog and justification (pull request S7).

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** the feature page of schemas created by #235 (`docs/features/` page, name verified when S7 starts; through `docs-writer`): "Generated schemas": the marker, the supported subset, the diagnostics table, the serializer-context requirement, AOT notes, equivalence guarantees.
2. **Modify** the two package READMEs (`src/Encina.Validation.Schema/README.md`, `src/Encina.Validation.Generators/README.md`; through `docs-writer`).
3. **Create** `changelog.d/3980-validation-schema-source-generator.added.md` (through `mechanical-fixer`).
4. **Create** the load justification `tests/Encina.LoadTests/Validation/GeneratedSchema.md` (`AGENTS.md` section 9 format: stateless generated code, no concurrency of its own; the shared runner is covered by #230).
5. Build verification: `dotnet build --configuration Release` with zero warnings; `dotnet test` of the touched projects; per-flag coverage of the touched files against the targets of the Estimated File Count section.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT:
Issue #3980, plan docs/plans/validation-schema-source-generator-implementation-plan-3980.md, phase 7 of 7.

TASK:
1. Document the feature (docs-writer), add the changelog fragment (mechanical-fixer) and the load justification file.

KEY RULES:
- Never type coverage figures by hand: cite cov: markers (SPEC-001). Link ADRs and sibling plans by number only.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md, changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| Roslyn incremental generators cookbook | `IIncrementalGenerator`, `ForAttributeWithMetadataName`, equatable models, no `Compilation` in a model | S2 pipeline and incrementality test |
| .NET trimming and Native AOT guidance | Reflection, `Expression<>` and `Activator` are not AOT-safe; `System.Text.Json` source generation is | Design Choice 4, the no-reflection test |
| `AGENTS.md` section 5 | Validation category consistency | Register row C1 |
| ADR-062 | Error category contract | Categories of the reused codes (row C1) |
| ADR-054 D3, D4 | One source and meter per package; tenant tag only when present | Register row C7 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `Encina.Security.ABAC.Analyzers` (the Roslyn packaging model) | `src/Encina.Security.ABAC.Analyzers/Encina.Security.ABAC.Analyzers.csproj` | Build rules of the generator project (register row C10) |
| `Microsoft.CodeAnalysis.CSharp` 5.0.0 and `Verify` 31.15.0 | `Directory.Packages.props:206,169` | Generator and snapshots |
| `ISchema<T>`, `SchemaBase<T>`, `Schema.Object<T>`, `SchemaRequestValidator<T>`, `SchemaLog` (#235) | `src/Encina.Validation.Schema/` (after #235) | Runtime reference and the base refactored in S1 |
| `ValidatorGenerator`, `DiagnosticDescriptors`, `RegistrationEmitter`, `GeneratorTestHost` (#227) | `src/Encina.Validation.Generators/`, `tests/Encina.TestInfrastructure/Validation/` (after #227) | Reused by S2 to S4 |
| `IRequestValidator<T>`, `RequestValidatorRunner` (#230) | `src/Encina/Validation/` (after #230) | How a generated schema runs under the three providers |
| `ValidationBenchmarks` | `tests/Encina.BenchmarkTests/Encina.Benchmarks/ValidationBenchmarks.cs` | Benchmark conventions |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Validation.Schema` (`ValidationSchema`, registered by #235 S7) | none new | Generated schemas log through the base with 620-622; compile-time diagnostics are `ENCVAL100` and up, not EventIds (register row C6) |

### Estimated File Count

Every count includes tests, snapshots, coverage manifest edits, PublicAPI files, READMEs and the changelog fragment. No pull request uses a type created by a later one; each enters its per-file manifest targets with its own tests.

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| S1 | Public seam in the runtime package | 3 (`GenerateSchemaAttribute`, `GeneratedSchemaContext`, `GeneratedSchemaBase`) | 2 (`SchemaBase`, PublicAPI) | 4 (base, context, guard, contract) | 0 | 1 | 0 | 10 | ~700 |
| S2 | Generator front end, diagnostics | 4 (`SchemaGenerator`, 2 models, `SchemaDefinitionParser`) | 1 (`DiagnosticDescriptors`) | 3 (parser, diagnostics, incrementality) | 0 | 1 | 0 | 9 | ~900 |
| S3 | Primitive emission | 2 (`SchemaEmitter`, `PrimitiveRuleEmitter`) | 1 (`SchemaGenerator`) | 6 (snapshot class, 3 snapshots, equivalence property, no-reflection) | 0 | 1 | 0 | 10 | ~1,100 |
| S4 | Objects, arrays, unions, normalize, registration | 4 (`ObjectRuleEmitter`, `CollectionRuleEmitter`, `NormalizeEmitter`, `SchemaRegistrationEmitter`) | 1 (`SchemaGenerator`) | 8 (snapshot class, 4 snapshots, object property, parse, registration) | 0 | 1 | 0 | 14 | ~1,350 |
| S5 | Equivalence, integration, benchmark, AOT-analyzer sample | 0 | 0 | 5 (contract, 3 integration, benchmark) | 3 (`ContractTests.csproj`, `IntegrationTests.csproj`, `Encina.Benchmarks.csproj`: ProjectReference to the sample) | 0 | 3 (sample `.csproj`, sample source, `Encina.slnx`) | 11 | ~950 |
| S6 | Composed types (conditional) | 1 (`CompositionEmitter`) | 4 (`GenerateSchemaAttribute`, `SchemaGenerator`, `DiagnosticDescriptors`, PublicAPI) | 5 (3 snapshots, composition emission, composition property) | 0 | 2 | 0 | 12 | ~900 |
| S7 | Documentation | 0 | 2 (the two READMEs) | 0 | 0 | 0 | 3 (feature page, fragment, load `.md`) | 5 | ~450 |
| **Total** | | **14** | **11** | **31** | **3** | **6** | **6** | **71** | **~6,350** |

The columns sum to the total (14 + 11 + 31 + 3 + 6 + 6 = 71) and the rows sum to 71 files (10 + 9 + 10 + 14 + 11 + 12 + 5) and about 6,350 lines (700 + 900 + 1,100 + 1,350 + 950 + 900 + 450). S6 needs `GeneratorTestHost` and the snapshots only, which `Encina.UnitTests` already references through #227 S2. The largest pull request is S4 at 14 files; none exceeds 1,500 lines (the snapshot files are generated text and counted in the line estimates). S1 and S2 can proceed in parallel after their prerequisites; S3 needs S2; S4 needs S3; S5 needs S4; S6 needs S4; S7 needs S5.

Per-file coverage targets (per flag, with a one-sentence justification), written into the package manifests by the pull request that adds the file:

| File (entered by) | Unit | Guard | Property | Contract | Justification |
|------|------|-------|----------|----------|---------------|
| `GeneratedSchemaBase.cs` (S1) | 90 | 85 | 0 | 40 | Every branch of `Validate` and `Parse` is reachable with a hand-written derived schema and hostile inputs; the guard tests reach the constructor checks; the S1 contract test runs the main path |
| `GeneratedSchemaContext.cs` (S1) | 95 | 85 | 0 | 0 | Small collector reached by the context tests; the guard tests reach the argument checks |
| `GenerateSchemaAttribute.cs` (S1, S6) | 0 | 0 | 0 | 0 | An attribute with no executable code beyond a property in S6, read by the generator; behavior is covered through the snapshots |
| `SchemaBase.cs` (S1, changed core) | 90 | 85 | 0 | 0 | The extracted core is exercised by the existing #235 tests and the S1 equivalence cases |
| `SchemaGenerator.cs`, `SchemaDefinitionParser.cs`, models (S2 to S4) | 90 | 0 | 0 | 0 | Parser and pipeline branches are reachable with the driver and source strings; guard 0 because the generator types have no guarded public parameters (Roslyn passes the contexts) and the helpers are internal without `InternalsVisibleTo` for `Encina.GuardTests` |
| `SchemaEmitter.cs`, `PrimitiveRuleEmitter.cs`, `ObjectRuleEmitter.cs`, `CollectionRuleEmitter.cs`, `NormalizeEmitter.cs` (S3, S4) | 85 | 0 | 50 | 0 | Each rule family has a snapshot and the equivalence properties drive generated code against the runtime schema; guard 0 for the same reason |
| `SchemaRegistrationEmitter.cs` (S4) | 85 | 0 | 0 | 0 | The registration snapshot and the compiled registration test reach every emitted branch; guard 0 for the same reason as the other emitters |
| `CompositionEmitter.cs` (S6) | 85 | 0 | 50 | 0 | Snapshots plus the composition property; guard 0 for the same reason |

Integration targets are not entered per file: the S5 tests run a compiled sample through `IEncina.Send` and the package default for the integration flag stays.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #3980 - the compile-time (source-generated) form of Encina.Validation.Schema schemas.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always.
- Validation follows the orchestrator pattern (AGENTS.md sections 3 and 5); a generated schema runs through the
  IRequestValidator<T> adapter of #235 and the runner of #230, so the three providers behave the same.
- The generator lives in the one Roslyn project of #227 (Encina.Validation.Generators); the runtime schemas are #235.

IMPLEMENTATION OVERVIEW:
1. A public seam in Encina.Validation.Schema: GenerateSchemaAttribute, GeneratedSchemaBase<T>, GeneratedSchemaContext (S1).
2. A second incremental generator, the definition parser and the ENCVAL100+ diagnostics (S2).
3. Emission: primitives (S3); objects, arrays, unions, Normalize over JsonNode and registration (S4).
4. Equivalence contract test, three provider integration tests, benchmark (S5).
5. Composed output types Extend/Pick/Omit (S6, conditional); docs and changelog (S7).
Seven pull requests, see the Estimated File Count table.

KEY PATTERNS:
- Generated code compiles in user assemblies: only public members of the runtime package; no InternalsVisibleTo.
- Reflection-free output: direct property access; JsonTypeInfo<T> from the application's serializer context for Parse.
- Incremental generator: equatable models, no Compilation captured; diagnostics carry identifiers only.
- Same codes, arguments, categories and limits as the runtime schema; proven by property and contract tests.

REFERENCE FILES:
- docs/plans/validation-schema-source-generator-implementation-plan-3980.md
- docs/plans/validation-source-generators-implementation-plan-227.md, docs/plans/validation-schema-builder-implementation-plan-235.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Build-time feature; generated schemas are static singletons, not cached data |
| 2 | OpenTelemetry | ✅ Phase 1 | Generated schemas inherit the `Encina.Validation.Schema` source and meter of #235 through the base; tenant tag only when present (ADR-054 D4, register row C7) |
| 3 | Structured Logging | ✅ Phase 1 | The base logs with EventIds 620-622 of #235; no new range (register row C6) |
| 4 | Health Checks | ❌ N/A | No runtime dependency |
| 5 | Validation | ✅ All phases | This is a validation feature; the three providers behave the same through the runner (S5) |
| 6 | Resilience | ❌ N/A | No external call |
| 7 | Distributed Locks | ❌ N/A | No shared state |
| 8 | Transactions | ❌ N/A | Validation runs before the handler |
| 9 | Idempotency | ❌ N/A | Generated validation is deterministic |
| 10 | Multi-Tenancy | ❌ N/A | Build-time; a tenant-scoped schema-backed validator is an `IScopedRequestValidator<T>` (#3982) delegating to the generated schema, so no generator change (register row C11) |
| 11 | Module Isolation | ❌ N/A | Build-time; module scoping is the same #3982 mechanism |
| 12 | Audit Trail | ❌ N/A | No state change; validation failures are results, not audit events |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| The runtime schemas of #235, S1 to S7 (S7 creates the telemetry that the shared core calls) | Open (plan only) | Blocking for S1 |
| The generator project, diagnostics, `RegistrationEmitter` and `GeneratorTestHost` of #227 (S2 to S5) | Open (plan only) | Blocking for S2 and S4 |
| `IRequestValidator<T>` and the runner (#230) | Open (plan only) | Needed by the S4 registration and the S5 pipeline tests |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Tenant- and module-scoped selection | #3982 (open) | How a schema-backed validator is scoped (register row C11) |
| Central validation instrumentation | #726 (open) | Not needed: the base carries the schema telemetry |

---

## Next Steps

1. Link this plan from #3980 and ask the maintainer to answer Design Choices 1 to 4 and the Dilemma rows C2, C5 and C8 when work starts; remove `needs-decision` from the issue after the answers are recorded.
2. Implement S1 to S7 as the pull requests of the Estimated File Count split once the prerequisites of each are merged; S6 only when Design Choice 2 option A stands; `Fixes #3980` on S7.
3. Re-verify against `main` when S1 starts: `SchemaBase<T>` and its hooks (#235), the diagnostics ids already taken by #227, #228 and #231 (the block `ENCVAL100` and up must be free), and the name of the schema feature page.
