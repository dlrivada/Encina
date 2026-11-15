# Implementation Plan: Schema-First Validation (`Encina.Validation.Schema`)

> **Issue**: [#235](https://github.com/dlrivada/Encina/issues/235)
> **Type**: Feature
> **Related**: #230 (owns `IRequestValidator<T>`, the runner and EventIds 550-559; the adapter of S6 builds on it), #233 (owns `ValidationError.ErrorCode`, the message keys and the localizer this package emits through), #227 (owns the source-generator project; the compile-time form of schemas is a follow-up), #232 (security-focused validation, a sibling that shares no code), #234 and #236 (planned apart), #726 (central validation instrumentation), #3415 (a code-less validation `Left` never becomes a 400), ADR-001, ADR-006, ADR-054, ADR-062 (merged: error category contract)
> **Complexity**: High (a new package with a fluent type-safe API, a JSON normalization pass, composition, a telemetry layer and an integration with the validation runner)
> **Estimated Scope**: ~2,300 lines of production code + ~3,400 lines of tests + ~450 lines of documentation, in eight pull requests of at most 15 files and 1,000 lines each

---

## Summary

### What exists today (read on 2026-11-15, main `db1e7f688`)

- All validation in Encina is rule-attached-to-a-type: FluentValidation `AbstractValidator<T>` classes, DataAnnotations attributes and MiniValidation (which reads the same attributes), run by the one registered `IValidationProvider` after the request is bound (`ValidationPipelineBehavior`, `ValidationOrchestrator`; `src/Encina/Validation/`). The only provider-neutral result is `ValidationResult` with `ValidationError(PropertyName, ErrorMessage)` (`ValidationResult.cs:97`).
- There is no schema concept, no package `Encina.Validation.Schema`, no `Schema` type, no parser from raw input, no type coercion, no composition, no discriminated union and no limits on untrusted input (`git grep -n "Encina.Validation.Schema" -- src` is empty). The only Roslyn component in `src` is `Encina.Security.ABAC.Analyzers`; the generator project of #227 does not exist yet.
- The issue's `Parse` returns `Either<ValidationErrors, User>`; the rule of `AGENTS.md` section 3 is `Either<EncinaError, T>`, and `ValidationErrors` is not a type that exists.
- A new satellite package costs a fixed set of touch points, all seen in `Encina.MiniValidator`: a `.csproj` with `PublicAPI` analyzers and `InternalsVisibleTo`, an entry in `Encina.slnx`, a `ProjectReference` in each test project that uses it (`tests/Encina.UnitTests`, `GuardTests`, `PropertyTests`, `IntegrationTests`, `BenchmarkTests`), an entry in `.github/scripts/coverage-weights.json`, a coverage manifest, `PublicAPI.Shipped.txt` and `Unshipped.txt`, a README, and an entry in `docs/INVENTORY.md`.
- Observability convention (ADR-054 D3, D4): one `ActivitySource` and one `Meter` per package named `Encina.<Package>`, zero cost without a listener, a tenant tag only when multi-tenancy is on; the model in the repository is `src/Encina.Cdc/Caching/Diagnostics/CacheInvalidationActivitySource.cs` and `CacheInvalidationMetrics.cs`.

### What this plan delivers

A new opt-in package `Encina.Validation.Schema`: a fluent, type-safe schema API in the spirit of Zod, built from immutable schema objects.

- **Primitives**: `Schema.String()` (`Min`, `Max`, `Length`, `Email`, `Url`, `Guid`, `Regex`, `Trim`), `Schema.Int()`/`Number<T>()` (generic over `INumber<T>`: `Min`, `Max`, `Positive`, `Integer`), `Schema.Boolean()`, `Schema.Date()`, plus `Optional()`, `Nullable()` and `Default(value)` on any schema, and a `Coerce` form for string-to-number/bool/date.
- **Composites**: `Schema.Object<T>(...)` with typed property rules, `Schema.Array(item)` with `Min`/`Max`/`NonEmpty`, `Schema.DiscriminatedUnion<TBase>(...)`, composition over the same `T` (`Merge`, `Extend`, `Pick`, `Omit`), `Refine` and `Transform`.
- **Two entry points**: `Validate(T value)` returns a provider-neutral `ValidationResult` (what a bound request needs); `Parse(JsonElement)` or `Parse(string json)` validates untrusted raw input, normalizes it and returns `Either<EncinaError, T>` with code `validation.schema_failed` (category `Validation`) carrying JSON-pointer paths and rule codes, never values.
- **Integration**: a schema becomes an `IRequestValidator<TRequest>` (the contract of #230), so it runs inside `ValidationOrchestrator` under any of the three providers and emits `ValidationError`s with the code and arguments of #233, which makes the messages localizable.
- **Safety and telemetry**: bounded depth, input size, issue count and regular-expression time; `Encina.Validation.Schema` activity source and meter with a tenant tag when `TenantId` is present; EventIds 620-622.

Nothing changes for an application that does not reference the package. The compile-time (source-generated) form named in the issue's task list is a follow-up on the generator project of #227 (Design Choice 6).

- Affected packages: `Encina.Validation.Schema` (new). `Encina` (core) receives only the EventId range registration; the three provider packages are untouched.
- Provider category: Validation (3) (`AGENTS.md` section 5): schemas run under FluentValidation, DataAnnotations and MiniValidator through the neutral contract of #230, and an equivalence test proves the same rule gives the same result under each. It does not add a fourth `IValidationProvider` (Design Choice 1).
- Shared pieces: consumes `IRequestValidator<T>` and `RequestValidatorRunner` (#230), `ValidationError.ErrorCode`/`Arguments` and the message keys (#233). Owns `ISchema<T>`, `SchemaIssue` and the schema error codes.

---

## Design Choices

<details>
<summary><strong>1. How a schema takes part in request validation</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Adapter: `SchemaRequestValidator<T> : IRequestValidator<T>`, registered with `AddEncinaSchema<TRequest>(schema)`; the schema runs inside whichever provider is registered, through the runner of #230** | Works under all three providers at once and next to existing validators (the provider-consistency rule); no second validation pipeline; reuses the timeout, parallelism and failure mapping of the runner | Needs #230 (S1 and the provider changes) to be merged first; the schema result passes through `ValidationError` |
| **B) A new `SchemaValidationProvider : IValidationProvider`** | Independent of #230 | Only one `IValidationProvider` is registered (the first `TryAdd` wins), so schemas would exclude FluentValidation, DataAnnotations and MiniValidator instead of joining them; a fourth provider outside the "all 3 consistent" rule |
| **C) Both** | Covers the standalone application | Two integration paths to test and document |

### Chosen Option: **A — `IRequestValidator<T>` adapter** (recommended, pending the maintainer)

### Rationale

- The issue asks for "integration with validation orchestrator"; the neutral contract is the integration that composes with the existing providers instead of replacing them. The standalone `Validate` and `Parse` calls remain for code that does not use the pipeline.
- B wins when an application wants schemas as its only validation mechanism and cannot wait for #230; C wins when both kinds of application are in scope for 1.0.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. How `Parse` turns raw input into a typed value</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Compiled property setters: build `T` by assigning each property through setters compiled from the selector expressions** | Transformations apply per property; no second serializer | Needs a parameterless constructor and settable or `init` properties: a positional record or a type with a constructor cannot be built |
| **B) An explicit factory per schema (`.Construct(values => new User(...))`)** | Works with any constructor | Every schema repeats its constructor call; an easy place for a mismatch the compiler cannot catch |
| **C) Normalize then deserialize: the schema walks the input as a mutable `JsonNode`, collects issues, applies coercion, defaults and per-value transforms in place, then `System.Text.Json` deserializes the normalized node into `T`** | Works with positional records, constructors and `[JsonPropertyName]`; one place applies naming; coercion and trimming happen before binding | Two passes over the data; whole-object `Transform` that changes the type needs `Refine`/`Transform` on the typed result, applied after deserialization |

### Chosen Option: **C — Normalize, then deserialize** (recommended, pending the maintainer)

### Rationale

- Request types in Encina are usually records with constructors, which only C and B can build; C does it without per-schema boilerplate and reuses the serializer the application already configures (`SchemaParseOptions.JsonSerializerOptions`, camelCase by default).
- A wins for mutable DTOs with many transforms; B wins when a type has invariants enforced in its constructor that must run at parse time (an always-valid type, #228).

**Class:** Dilemma

</details>

<details>
<summary><strong>3. The error a failed parse returns</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Either<EncinaError, T>` with code `validation.schema_failed`, `ErrorCategory.Validation`, details holding the issue list (JSON pointer, rule code, arguments) and never an input value** | Follows ROP; the category drives the HTTP status through ADR-062; issues are machine-readable | The details dictionary holds a list of small records |
| **B) `Either<ValidationErrors, T>` as in the issue** | Typed left side | A new error type nothing in Encina can map to a host response; violates the ROP rule |

### Chosen Option: **A — `EncinaError` with issues in the details**

### Rationale

- Operations return `Either<EncinaError, T>`; a rejected input is an `EncinaError` with a code and a category (ADR-001, ADR-006, ADR-062 rule R3 field rules are `Validation`). Limit violations (depth, size) are `InvalidRequest` (the nearest rule is R3, which gives a missing or malformed request part that category; register row C1 records the choice), and an exception thrown by a user-supplied refinement is `Internal` (R4).

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (Railway Oriented Programming: operations return `Either<EncinaError, T>`), ADR-001, ADR-006 and ADR-062 rules R3 and R4; option B violates the first, so A is the only compliant option

</details>

<details>
<summary><strong>4. Package placement</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A new package `Encina.Validation.Schema`, as the issue names it** | Opt-in (pay for what you use); core stays small; matches the issue's "New Package" | The fixed touch points of a new package (csproj, solution, five test project references, weights, manifest, README, inventory) |
| **B) Inside core `Encina.Validation`** | No new package | About 20 types in core for an optional style of validation |

### Chosen Option: **A — New package**

### Rationale

- The issue names the package and labels itself `new-package`; the package depends on `Encina` core only, and every provider package stays free of it.

**Class:** Trivial
**Settled by:** the issue ("New Package: `Encina.Validation.Schema`", label `new-package`) and `AGENTS.md` section 1 (pay-for-what-you-use, every feature opt-in)

</details>

<details>
<summary><strong>5. How schemas compose when C# has no structural types</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Composition over the same `T`: `Merge` combines the rules of two schemas of `T`, `Extend` adds rules, `Pick` and `Omit` keep or drop the rules of named properties** | Buildable with the type system as it is; the schema of a base DTO is reused for derived or partial views | The result type is always `T`: Zod's `extend`, which changes the output type, has no equivalent |
| **B) Type-changing composition (a new `T2` per `extend`/`pick`)** | Matches Zod | Needs a generator to emit the new types: the generator project of #227 does not exist yet |

### Chosen Option: **A — Same-`T` composition** (recommended, pending the maintainer)

### Rationale

- A delivers the issue's four operations with the language as it is. B becomes possible when the generator exists and is the natural first use of it.
- B wins when the maintainer wants type-changing composition as part of the same release as the generator.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. The source generator named in the issue's task list</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Out of scope here: a follow-up issue on the generator project of #227 emits compile-time schema code (draft: `artifacts/issues/validation-schema-source-generator.md`)** | The generator is one project owned by one plan; the runtime package ships and is usable without it; the acceptance criteria do not list it | The task-list item is not delivered by this issue |
| **B) A generator inside this package** | One issue delivers everything | A second Roslyn component, duplicating the project of #227 |

### Chosen Option: **A — Follow-up on #227's generator**

### Rationale

- The issue's Acceptance Criteria list the fluent API, parsing, composition, refinements, unions, orchestrator integration, tests and documentation; the generator appears only in the task list. One Roslyn project avoids two toolchains to maintain.

**Class:** Trivial
**Settled by:** the issue's Acceptance Criteria (the generator is not among them) and the plan of #227 (owns the one generator project of the family)

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | Error codes and categories (ADR-062), created with `EncinaErrors.Create(code, category, message, ...)`: `validation.schema_failed` (`Validation`, R3), `validation.schema_limit_exceeded` (`InvalidRequest`, R3), `validation.schema_refinement_failed` (`Internal`, R4: a user delegate threw; the `Left` carries the code only) | Dilemma (recommended, pending the maintainer) | public codes; `InvalidRequest` versus a dedicated payload-too-large category is the open point (ADR-062 has no such category) |
| C2 | Untrusted input is bounded and fails closed: `MaxDepth = 32`, `MaxInputBytes = 1 MiB` for `Parse(string)`, `MaxIssues = 100`; a limit violation returns the `Left` of C1 and never a partial success | Dilemma (recommended, pending the maintainer) | numeric defaults; the validation of options at start-up rejects zero and negative values |
| C3 | Regular expressions use `RegexOptions.NonBacktracking` with a match timeout of one second; a pattern the engine cannot compile as non-backtracking (back-references, lookarounds) is rejected when the schema is built, not at parse time | Settled | OWASP "Regular expression Denial of Service (ReDoS)" guidance and Microsoft's "Best practices for regular expressions in .NET" (non-backtracking engine runs in linear time; match timeout) |
| C4 | Messages are templates with named arguments (the schema parameters, for example `Min`); an input value is never part of a message, an issue, a log line, an activity tag or `EncinaError.Message` | Settled | `AGENTS.md` section 3 (`EncinaError.Message` never reaches logs; #1168, #1173, #1259, #1274) and ADR-054 settled point 2 |
| C5 | Every issue carries `ErrorCode` and `Arguments` (the fields of #233) with the key convention `validation.<rule>`; the English default text is in the package, translation goes through the localizer of #233 when registered | Dilemma (recommended, pending the maintainer) | follows #233 (a pending plan); the alternative is plain English messages with no keys until #233 is implemented |
| C6 | Schemas are immutable; every fluent call returns a new schema, so a schema built once at start-up is safe to share across threads and requests | Trivial | The fluent immutable-builder idiom (`System.Collections.Immutable`, `string` operations) and `IValidationProvider`'s own contract ("implementations should be thread-safe and reusable", `IValidationProvider.cs:12`) |
| C7 | `Refine` and `Transform` take synchronous delegates; asynchronous or I/O-bound rules are written as `IRequestValidator<T>` (#230), which has timeout and parallelism control | Settled | #230 plan (the runner owns async validation); `AGENTS.md` section 3 (database calls are asynchronous with a `CancellationToken`) |
| C8 | Telemetry: one `ActivitySource` and one `Meter` named `Encina.Validation.Schema`; counter `encina.validation.schema.operations` (tags `operation` = `parse`/`validate`, `result` = `valid`/`invalid`/`error`, `schema` = the developer-chosen schema name) and histogram `encina.validation.schema.duration` in seconds; every call is guarded by `HasListeners`; `encina.tenant_id` is set on activities and metrics only when `IRequestContext.TenantId` is not null (multi-tenancy on, ADR-054 D4); the schema name is a bounded constant, never request data | Settled | ADR-054 D3 and D4 and settled point 7; `src/Encina.Cdc/Caching/Diagnostics/CacheInvalidationActivitySource.cs` and `CacheInvalidationMetrics.cs` (the convention in code) |
| C9 | No tenant id is part of a key, a cache or a schema registration, so no tenant-qualified id needs to be injective and the reserved `default` segment is not involved; tenant-specific schemas are a follow-up | Settled | The package holds no tenant-keyed state; ADR-054 D4 |
| C10 | EventIds 620-622 are packed from the start of the sub-block 620-629 of the block 600-649 assigned to this family; the range `ValidationSchema = (620, 629)` is registered first and the new assembly is mapped in `AssemblyRanges` | Trivial | `AGENTS.md` section 7; ADR-021; `artifacts/decisions/plan-batch/eventid-blocks.md` |
| C11 | Property paths are JSON pointers (`/items/0/quantity`) in issues; `ValidationError.PropertyName` receives the .NET property path (`Items[0].Quantity`) so the three providers' error shape is equal | Dilemma (recommended, pending the maintainer) | public behavior; the JSON pointer for `PropertyName` is the alternative when the schema serves a JSON API only |
| C12 | AOT and trimming are not guaranteed by this package: selectors are read as expression trees and property names come from reflection over `T`; the generator follow-up (Design Choice 6) is the AOT path | Trivial | The plan of #227 owns reflection-free validation; `AGENTS.md` section 1 (best solution first, no compromise for compatibility) |

---

## Implementation Phases

### Phase 1: Package Skeleton and Contracts (pull request S1)

> **Goal**: The new package builds, is part of the solution and the test projects, and holds the contracts; no schema exists yet.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Validation.Schema/Encina.Validation.Schema.csproj` (modeled on `Encina.MiniValidator.csproj`: `PackageId`, description, `PackageReadmeFile`, `GenerateDocumentationFile`, a reference to `Encina` and `Microsoft.Extensions.DependencyInjection.Abstractions`/`Microsoft.Extensions.Logging.Abstractions`/`Microsoft.Extensions.Options`, the `PublicApiAnalyzers`, and an `InternalsVisibleTo` entry for `Encina.UnitTests` (guard tests reach only public members, so none is added for `Encina.GuardTests`)), `PublicAPI.Shipped.txt` (`#nullable enable`) and `PublicAPI.Unshipped.txt`.
2. **Create** the contracts: `ISchema.cs` (`public interface ISchema<T>`: `string Name { get; }`, `ValidationResult Validate(T value)`, `Either<EncinaError, T> Parse(JsonElement input, SchemaParseOptions? options = null)`, `Either<EncinaError, T> Parse(string json, SchemaParseOptions? options = null)`), `SchemaIssue.cs` (`record SchemaIssue(string Path, string Code, IReadOnlyDictionary<string, object?>? Arguments)`), `SchemaParseOptions.cs` (limits of register row C2, `JsonSerializerOptions`, `Coerce`), `SchemaErrorCodes.cs` (the three codes of row C1 and their categories).
3. **Modify** `Encina.slnx`, `tests/Encina.UnitTests/Encina.UnitTests.csproj` (a `ProjectReference`) and `.github/scripts/coverage-weights.json` (the package in the category that lists `Encina.FluentValidation`, `Encina.DataAnnotations`, `Encina.MiniValidator`); **create** `.github/coverage-manifest/Encina.Validation.Schema.json`; `PublicAPI.Unshipped.txt` lines; all through `mechanical-fixer`.
4. Tests (new): `SchemaContractsTests` (unit: options defaults and validation of limits, the error-code constants and their categories through `EncinaErrors.Create`, `SchemaIssue` equality).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #235 plan docs/plans/validation-schema-builder-implementation-plan-235.md,
phase 1 of 8. A new satellite package Encina.Validation.Schema; model it on src/Encina.MiniValidator.

TASK:
1. Create the csproj (PublicAPI analyzers, InternalsVisibleTo Encina.UnitTests), the two PublicAPI files, and the
   contracts ISchema<T>, SchemaIssue, SchemaParseOptions, SchemaErrorCodes.
2. Add the package to Encina.slnx, the UnitTests project reference, coverage-weights.json and a new coverage manifest.

KEY RULES:
- Parse returns Either<EncinaError, T>; error codes are created with the categorized EncinaErrors.Create (ADR-062).
- Never put an input value in an issue, message or log. XML docs on every public member.
- Solution, csproj references, weights and manifest edits go through mechanical-fixer; edit code with Edit/Write only.

REFERENCE FILES:
- src/Encina.MiniValidator/Encina.MiniValidator.csproj, ServiceCollectionExtensions.cs
- .github/coverage-manifest/Encina.MiniValidator.json
- docs/plans/validation-schema-builder-implementation-plan-235.md (register rows C1, C2)
```

</details>

---

### Phase 2: Primitive Schemas (pull request S2)

> **Goal**: String, number and boolean schemas with the shared fluent chain (`Optional`, `Nullable`, `Default`, `Coerce`), testable alone.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `Schema.cs` (`public static partial class Schema`: `String()`, `Int()`, `Number<T>() where T : struct, INumber<T>`, `Boolean()`), `StringSchema.cs` (`Min`, `Max`, `Length`, `Email`, `Url`, `Guid`, `Regex(pattern)` per register row C3, `Trim`, `OneOf(values)`), `NumberSchema.cs` (`NumberSchema<T>`: `Min`, `Max`, `Positive`, `Negative`, `Integer`, `Coerce`), `BooleanSchema.cs`, and an internal `SchemaIssueFactory.cs` (path, code, arguments in one place; keys `validation.<rule>` per register row C5).
2. **Modify** `tests/Encina.GuardTests/Encina.GuardTests.csproj` and `tests/Encina.PropertyTests/Encina.PropertyTests.csproj` (a `ProjectReference` each), `PublicAPI.Unshipped.txt` and the manifest through `mechanical-fixer`.
3. Tests (new): `StringSchemaTests`, `NumberSchemaTests` (including `int`, `long`, `decimal`, `double` boundaries and `Coerce`), `BooleanSchemaTests` (unit); `PrimitiveSchemaGuardTests` (guard: null/empty pattern, negative length, `Min` greater than `Max`, null `OneOf`; `Optional()` on an optional schema is a valid case); `PrimitiveSchemaProperties` (FsCheck: a value inside `[Min, Max]` is valid and one outside is not for any bounds; `Validate` never throws for any string).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #235 plan docs/plans/validation-schema-builder-implementation-plan-235.md, phase 2 of 8. Depends on S1.

TASK:
1. Implement Schema.String(), Schema.Int(), Schema.Number<T>() (INumber<T>), Schema.Boolean() as immutable schema
   objects: every fluent call returns a new instance. Issues carry a JSON-pointer path, a code and named arguments.
2. Regex rules use RegexOptions.NonBacktracking and a one-second match timeout; reject patterns that cannot compile so.

KEY RULES:
- No input value in any issue or message. Immutable schemas, safe to share across threads.
- Tests execute real schema code; guard tests for every validated parameter.

REFERENCE FILES:
- docs/plans/validation-schema-builder-implementation-plan-235.md (register rows C3, C4, C5, C6)
- src/Encina/Validation/ValidationResult.cs
```

</details>

---

### Phase 3: Date, Array and Wrapper Schemas (pull request S3)

> **Goal**: The remaining leaf and collection schemas.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `DateSchema.cs` (`DateOnly` and `DateTimeOffset`; `Min`, `Max`, `Past`, `Future` through the injected `TimeProvider` of `SchemaParseOptions`, never `DateTime.UtcNow`), `ArraySchema.cs` (`ArraySchema<TItem>`: `Min`, `Max`, `NonEmpty`, `Unique`; item issues get indexed paths), `NullableSchema.cs`/`OptionalSchema.cs`/`DefaultSchema.cs` (the wrapper decorators behind `Optional()`, `Nullable()` and `Default(value)`), and the `Schema.Date()`/`Schema.Array(item)` factory members.
2. **Modify** `PublicAPI.Unshipped.txt` and the manifest through `mechanical-fixer`.
3. Tests (new): `DateSchemaTests` (with `FakeTimeProvider`), `ArraySchemaTests` (indexed paths, nested issues, `Unique`), `WrapperSchemaTests` (optional, nullable, default semantics), `CollectionSchemaGuardTests` (guard), `DateSchemaProperties` (FsCheck: `Past` and `Future` are complementary around the fake clock).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #235 plan docs/plans/validation-schema-builder-implementation-plan-235.md, phase 3 of 8. Depends on S2.

TASK:
1. Implement DateSchema (DateOnly, DateTimeOffset), ArraySchema<TItem> and the Optional/Nullable/Default wrappers.
2. Time comes from the TimeProvider in SchemaParseOptions (default TimeProvider.System); never DateTime.UtcNow.

KEY RULES:
- Immutable schemas; item issues carry indexed JSON-pointer paths; no input value in any issue.
- Tests use FakeTimeProvider (Microsoft.Extensions.TimeProvider.Testing); no Thread.Sleep.

REFERENCE FILES:
- src/Encina.MiniValidator/ (package shape), docs/plans/validation-schema-builder-implementation-plan-235.md
```

</details>

---

### Phase 4: Object Schemas, Composition, Refine and Transform (pull request S4)

> **Goal**: The typed object schema of the issue's example and its composition operations.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `ObjectSchema.cs` (`ObjectSchema<T>`: `Validate(T)` reads each property through a getter compiled once from the selector expression; `Strict()` rejects unknown JSON members, `Strip()` ignores them), `ObjectSchemaBuilder.cs` (`Property<TProp>(Expression<Func<T, TProp>> selector, Func<SchemaFactory, ISchema<TProp>> rule)`: the `p => p.String().Min(1)` form of the issue; the property name resolves to the JSON name from the serializer options and `[JsonPropertyName]`), `RefinedSchema.cs` (`Refine(Func<T, bool> predicate, string code)` with a rule code and arguments; a delegate that throws maps to `validation.schema_refinement_failed`) and `TransformedSchema.cs` (`Transform<TOut>(Func<T, TOut>)` on typed results), and the composition members `Merge`, `Extend`, `Pick`, `Omit` over the same `T` (Design Choice 5).
2. **Modify** `PublicAPI.Unshipped.txt` and the manifest through `mechanical-fixer`.
3. Tests (new): `ObjectSchemaTests` (the issue's `User` example, nested objects, unknown members under `Strict`/`Strip`), `SchemaCompositionTests` (`Merge`, `Extend`, `Pick`, `Omit` and their effect on a fixture type), `RefineTransformTests` (predicate, throwing delegate gives the `Internal` code, transform result), `ObjectSchemaGuardTests` (null selector, a selector that is not a plain property access, a duplicate property, `Pick` of an unknown property), `ObjectSchemaProperties` (FsCheck: composition is associative for disjoint rule sets and `Validate(Parse(x))` agrees with `Parse(x)`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Issue #235 plan docs/plans/validation-schema-builder-implementation-plan-235.md, phase 4 of 8. Depends on S2 and S3.

TASK:
1. ObjectSchema<T> with typed property rules from selector expressions (read the expression tree once at build time,
   compile getters once), Strict/Strip, Refine and Transform, and Merge/Extend/Pick/Omit over the same T.
2. A refinement delegate that throws becomes a Left with validation.schema_refinement_failed (Internal) carrying the code
   only; the exception is logged later by type and stack trace, never its message.

KEY RULES:
- Immutable; composition returns new schemas; thread-safe after construction.
- Property names resolve through the JsonSerializerOptions naming policy and [JsonPropertyName].

REFERENCE FILES:
- docs/plans/validation-schema-builder-implementation-plan-235.md (Design Choices 2 and 5)
- src/Encina/Results/EncinaErrors.cs
```

</details>

---

### Phase 5: Raw Input Parsing, Unions and Limits (pull request S5)

> **Goal**: `Parse(JsonElement)`/`Parse(string)` with bounded, hostile-input-safe normalization, and discriminated unions.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `DiscriminatedUnionSchema.cs` (`Schema.DiscriminatedUnion<TBase>("type", (value, schema)...)`: selects by the discriminator, an unknown or missing discriminator is an issue, never an exception; `Validate(T)` selects by the runtime type), `SchemaJsonNormalizer.cs` (internal: walks a mutable `JsonNode` applying coercion, defaults and per-value transforms and collecting issues up to `MaxIssues`, checking `MaxDepth` and `MaxInputBytes`, then `JsonSerializer.Deserialize<T>` of the normalized node) and wire `Parse` of `ObjectSchema<T>` and `ArraySchema<T>` to it.
2. **Modify** `PublicAPI.Unshipped.txt` and the manifest through `mechanical-fixer`.
3. Tests (new): `DiscriminatedUnionSchemaTests`, `SchemaParseTests` (valid input, coercion, defaults, positional-record binding, unknown members, trailing data, malformed JSON gives a `Left`, never an exception), `SchemaHostileInputTests` (a corpus of depth bombs, 100,000-element arrays, huge strings, repeated keys, numbers beyond `decimal` range; each returns the `validation.schema_limit_exceeded` or `validation.schema_failed` `Left` within the limits and with no value echoed), `SchemaParseProperties` (FsCheck: `Parse` never throws for any string input).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
Issue #235 plan docs/plans/validation-schema-builder-implementation-plan-235.md, phase 5 of 8. Depends on S4.

TASK:
1. Implement the normalize-then-deserialize parse of Design Choice 2 option C with the limits of register row C2, and
   DiscriminatedUnionSchema<TBase>.
2. Parse(string) and Parse(JsonElement) never throw for any input: every failure is a Left with a code and a category.

KEY RULES:
- Fail closed: a limit violation is a Left, never a partial success. No input value in any issue.
- Tests include a hostile-input corpus; no Thread.Sleep.

REFERENCE FILES:
- docs/plans/validation-schema-builder-implementation-plan-235.md (register rows C1 to C4)
- src/Encina/Results/EncinaErrors.cs
```

</details>

---

### Phase 6: Integration with the Validation Runner, DI and Benchmark (pull request S6)

> **Goal**: A schema runs inside the validation pipeline under all three providers. Depends on the merged runner of #230 and the coded error of #233.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `SchemaRequestValidator.cs` (`SchemaRequestValidator<TRequest> : IRequestValidator<TRequest>`; maps `SchemaIssue` to `ValidationError(PropertyName, ErrorMessage, ErrorCode, Arguments)` per register rows C5 and C11) and `SchemaServiceCollectionExtensions.cs` (`AddEncinaSchema<TRequest>(this IServiceCollection, ISchema<TRequest> schema)`: `TryAddEnumerable` of a singleton validator, which is safe because schemas are immutable and hold no scoped dependency).
2. **Modify** `tests/Encina.IntegrationTests/Encina.IntegrationTests.csproj` and `tests/Encina.BenchmarkTests/Encina.Benchmarks/Encina.Benchmarks.csproj` (a `ProjectReference` each), `PublicAPI.Unshipped.txt` and the manifest through `mechanical-fixer`.
3. **Create** `tests/Encina.BenchmarkTests/Encina.Benchmarks/SchemaValidationBenchmarks.cs` (the `User` schema against the same request validated by FluentValidation and DataAnnotations).
4. Tests (new): `SchemaRequestValidatorTests` (unit), `SchemaRequestValidatorGuardTests`, `SchemaRegistrationTests` (DI with `ValidateOnBuild` and `ValidateScopes`, the runner resolves the schema validator), `SchemaProviderEquivalenceTests` in `tests/Encina.UnitTests/Validation/` (the same rule as a schema, as a FluentValidation rule, as a DataAnnotations attribute and as a MiniValidator attribute gives equal `ValidationResult`s under each provider), and `tests/Encina.IntegrationTests/Validation/{DataAnnotations,FluentValidation,MiniValidator}/SchemaValidationPipelineTests.cs` (through `IEncina.Send`, no container).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
Issue #235 plan docs/plans/validation-schema-builder-implementation-plan-235.md, phase 6 of 8. Requires the merged
IRequestValidator<T> and runner (plan of #230) and ValidationError.ErrorCode/Arguments (plan of #233).

TASK:
1. SchemaRequestValidator<TRequest> adapts ISchema<TRequest>.Validate to IRequestValidator<TRequest>, mapping each
   SchemaIssue to a ValidationError with code and arguments.
2. AddEncinaSchema<TRequest>(schema) registers it as a singleton with TryAddEnumerable; prove it with a ValidateOnBuild
   and ValidateScopes DI test; add the equivalence test and three integration tests through IEncina.Send.

KEY RULES:
- A schema is immutable and is registered as a singleton; no scoped dependency enters it.
- Existing providers are not modified; PublicAPI.Unshipped.txt and the manifest through mechanical-fixer.

REFERENCE FILES:
- docs/plans/async-cross-field-validation-implementation-plan-230.md (issue #230: the contract)
- docs/plans/validation-localization-implementation-plan-233.md (issue #233: the coded error)
```

</details>

---

### Phase 7: Observability (pull request S7)

> **Goal**: Traces, metrics and logs of schema operations by code and counts only.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina/Diagnostics/EventIdRanges.cs`: register `ValidationSchema = (620, 629)`; **modify** `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (map the assembly `Encina.Validation.Schema` to the new range) and the core `PublicAPI.Unshipped.txt`.
2. **Create** `src/Encina.Validation.Schema/Diagnostics/SchemaDiagnostics.cs` (internal static: the `ActivitySource` and `Meter` of register row C8, `HasListeners` guards) and `SchemaLog.cs` (`[LoggerMessage]`, packed): 620 a parse rejected the input (debug: schema name, issue count), 621 a limit was exceeded (warning: schema name, limit name), 622 a refinement threw (error: schema name, exception through `ForLogging()`). 623-629 stay free. XML docs name the range.
3. **Modify** `ObjectSchema.cs`, `ArraySchema.cs` and `DiscriminatedUnionSchema.cs`: the root `Parse` and `Validate` call one internal measuring helper (activity, counter, histogram) and log by code.
4. **Modify** both manifests (`Encina.json`, `Encina.Validation.Schema.json`) through `mechanical-fixer`.
5. Tests (new): `SchemaDiagnosticsTests` (a listener observes the activity and the counter with the expected tags, no listener allocates nothing observable, the tenant tag appears only with a `TenantId`), `SchemaLogTests` (EventIds and no values in rendered messages), `SchemaInstrumentationTests` (a refinement exception produces id 622 and a `Left`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT:
Issue #235 plan docs/plans/validation-schema-builder-implementation-plan-235.md, phase 7 of 8. Depends on S5.
ADR-021: register the EventId range in src/Encina/Diagnostics/EventIdRanges.cs before using it. ADR-054 D3, D4.

TASK:
1. Register ValidationSchema (620, 629) and map the new assembly in EncinaEventIdAllocationTests.
2. SchemaDiagnostics (Encina.Validation.Schema ActivitySource and Meter) and SchemaLog 620-622; instrument the root
   Parse/Validate of the object, array and union schemas.

KEY RULES:
- HasListeners guards; tenant tag only when TenantId is not null; schema name is a bounded constant.
- Logs and tags carry codes, counts and names only; exceptions go through ForLogging().

REFERENCE FILES:
- src/Encina.Cdc/Caching/Diagnostics/CacheInvalidationActivitySource.cs, CacheInvalidationMetrics.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 8: Documentation and Finalization (pull request S8)

> **Goal**: The package page, README, inventory, changelog and justification file.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `docs/features/validation-schema.md` and `src/Encina.Validation.Schema/README.md` through `docs-writer` (the API by example, `Validate` versus `Parse`, composition, limits and security notes, the three provider paths, telemetry).
2. **Modify** `docs/INVENTORY.md` and `docs/features/index.md` through `docs-writer`.
3. **Create** `changelog.d/235-validation-schema.added.md` through `mechanical-fixer`, and the justification file for Load tests (`AGENTS.md` section 9): schemas are immutable and stateless, so there is no concurrency to test beyond the unit-level thread-safety test.
4. Verification: `dotnet build --configuration Release` with zero warnings, `dotnet test` of the touched projects, per-flag coverage of every new file against the table below.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```text
CONTEXT:
Issue #235 plan docs/plans/validation-schema-builder-implementation-plan-235.md, phase 8 of 8.

TASK:
1. Document the package and add it to the inventory; add the changelog fragment and the load-test justification.
2. Build and measure per-flag coverage against the manifest targets.

KEY RULES:
- Pages go through docs-writer; fragments through mechanical-fixer. Never type coverage figures by hand: cite cov: markers.
- Link ADRs by number; link sibling plans by issue number only.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| Zod (TypeScript schema library) | Fluent schemas, `parse`/`safeParse`, `coerce`, `refine`, `transform`, discriminated unions | The API the issue asks for; mapped to the C# type system in Design Choices 2 and 5 |
| ADR-001, ADR-006, `AGENTS.md` section 3 | Operations return `Either<EncinaError, T>` | Result of `Parse` (Design Choice 3) |
| ADR-062 | Category contract; R3 field rules `Validation` and malformed request `InvalidRequest`; R4 provider/delegate failures `Internal` | Register row C1 |
| ADR-054 D3, D4 and settled points 2, 7 | One meter and source per package; tenant tag when multi-tenancy is on; codes only; no cost without a listener | Register row C8 |
| OWASP ReDoS guidance; Microsoft "Best practices for regular expressions in .NET" | Non-backtracking engine and match timeouts | Register row C3 |
| `System.Text.Json` (`JsonNode`, `JsonDocumentOptions.MaxDepth`, `JsonSerializerOptions`) | Mutable node tree, depth limit, naming policy | Normalization and limits (Phase 5) |
| .NET generic math (`INumber<T>`) | One numeric schema for every numeric type | `NumberSchema<T>` |
| RFC 6901 (JSON Pointer) | Path syntax for issue locations | Register row C11 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `ValidationResult`, `ValidationError` | `src/Encina/Validation/ValidationResult.cs` | Output of `Validate` and of the adapter |
| `IRequestValidator<T>`, `RequestValidatorRunner` (planned) | plan of #230 | The integration contract (Design Choice 1) |
| `ValidationError.ErrorCode`/`Arguments`, the localizer (planned) | plan of #233 | Coded, localizable issues (register row C5) |
| `EncinaErrors.Create` (categorized overload) | `src/Encina/Results/EncinaErrors.cs` | The three error codes |
| `CacheInvalidationActivitySource`, `CacheInvalidationMetrics` | `src/Encina.Cdc/Caching/Diagnostics/` | Model of the per-package source and meter |
| `Encina.MiniValidator` package shape | `src/Encina.MiniValidator/` | csproj, PublicAPI and registration conventions |
| `FakeTimeProvider` | `Microsoft.Extensions.TimeProvider.Testing` (central package version) | Date schema tests |
| `ValidationBenchmarks` | `tests/Encina.BenchmarkTests/Encina.Benchmarks/ValidationBenchmarks.cs` | Model for the schema benchmark |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Validation.Schema` (`ValidationSchema`, new, inside the block 600-649 assigned to the validation-2 family; the range is registered in core `EventIdRanges.cs`) | 620-629 | 620-622 used (three messages); 623-629 free; sibling plans in the same block: #233 600-609, #234 610-619, #236 630-639 |

### Estimated File Count

Every count includes tests, coverage manifest edits, PublicAPI files, project and solution edits, the README and the changelog fragment. No pull request uses a type created by a later one; each enters its per-file manifest targets with its own tests.

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| S1 | Skeleton and contracts | 7 (csproj, 2 PublicAPI, `ISchema`, `SchemaIssue`, `SchemaParseOptions`, `SchemaErrorCodes`) | 0 | 1 (contracts unit) | 1 (`Encina.UnitTests.csproj`) | 1 (new manifest) | 2 (`Encina.slnx`, `coverage-weights.json`) | 12 | ~450 |
| S2 | Primitive schemas | 5 (`Schema`, `StringSchema`, `NumberSchema`, `BooleanSchema`, `SchemaIssueFactory`) | 1 (PublicAPI) | 5 (string, number, boolean, guard, property) | 2 (`GuardTests.csproj`, `PropertyTests.csproj`) | 1 | 0 | 14 | ~1,000 |
| S3 | Date, array, wrappers | 5 (`DateSchema`, `ArraySchema`, `NullableSchema`, `OptionalSchema`, `DefaultSchema`) | 2 (`Schema.cs`, PublicAPI) | 5 (date, array, wrappers, guard, property) | 0 | 1 | 0 | 13 | ~950 |
| S4 | Object schemas and composition | 4 (`ObjectSchema`, `ObjectSchemaBuilder`, `RefinedSchema`, `TransformedSchema`) | 2 (`Schema.cs`, PublicAPI) | 5 (object, composition, refine/transform, guard, property) | 0 | 1 | 0 | 12 | ~1,000 |
| S5 | Parse, unions, limits | 2 (`DiscriminatedUnionSchema`, `SchemaJsonNormalizer`) | 3 (`ObjectSchema.cs`, `ArraySchema.cs`, PublicAPI) | 4 (union, parse, hostile corpus, property) | 0 | 1 | 0 | 10 | ~850 |
| S6 | Runner integration, DI, benchmark | 2 (`SchemaRequestValidator`, `SchemaServiceCollectionExtensions`) | 1 (PublicAPI) | 7 (adapter, guard, DI, equivalence, 3 integration) | 2 (`IntegrationTests.csproj`, `Benchmarks.csproj`) | 1 | 1 (the benchmark class) | 14 | ~1,000 |
| S7 | Observability | 2 (`SchemaDiagnostics`, `SchemaLog`) | 5 (`EventIdRanges.cs`, core PublicAPI, `ObjectSchema.cs`, `ArraySchema.cs`, `DiscriminatedUnionSchema.cs`) | 3 (diagnostics, log, instrumentation) | 1 (`EncinaEventIdAllocationTests.cs`) | 2 (`Encina.json`, `Encina.Validation.Schema.json`) | 0 | 13 | ~800 |
| S8 | Documentation | 0 | 0 | 0 | 0 | 0 | 6 (feature page, package README, `docs/INVENTORY.md`, `docs/features/index.md`, changelog fragment, load justification `.md`) | 6 | ~450 |
| **Total** | | **27** | **14** | **30** | **6** | **8** | **9** | **94** | **~6,500** |

The columns sum to the total (27 + 14 + 30 + 6 + 8 + 9 = 94) and the pull-request rows sum to 94 files (12 + 14 + 13 + 12 + 10 + 14 + 13 + 6). The largest are S2 and S6 with 14 files and about 1,000 lines, at the limit: if a test file grows, split it to its own pull request. S1 first; S2 needs S1; S3 and S4 need S2; S5 needs S4; S6 needs S5 and the merged plans' implementations of #230 (runner and provider changes) and #233 (`ErrorCode`); S7 needs S5; S8 needs all. If #230 or #233 is not implemented when S6 is due, S6 waits and S7 and S8 go first. Verify caller counts with `git grep` before each pull request.

Per-file coverage targets (per flag, with a one-sentence justification), written into `.github/coverage-manifest/Encina.Validation.Schema.json` by the pull request that adds the file:

| File | Unit | Guard | Property | Justification |
|------|------|-------|----------|---------------|
| `StringSchema.cs`, `NumberSchema.cs`, `BooleanSchema.cs`, `DateSchema.cs`, `ArraySchema.cs` | 95 | 90 | 60 | Every rule and its boundary is reachable with plain values and `FakeTimeProvider`; the guard tests reach every argument check; the property tests drive bounds and the clock |
| `NullableSchema.cs`, `OptionalSchema.cs`, `DefaultSchema.cs` | 95 | 85 | 0 | Small decorators with a few branches each; a property adds nothing beyond the unit cases |
| `ObjectSchema.cs`, `ObjectSchemaBuilder.cs`, `RefinedSchema.cs`, `TransformedSchema.cs` | 90 | 90 | 60 | Selector validation, composition and delegate failures are reachable with fixture types; the property test covers composition and parse/validate agreement |
| `DiscriminatedUnionSchema.cs`, `SchemaJsonNormalizer.cs` | 90 | 85 | 60 | The hostile-input corpus and the union cases reach every branch; the property test covers "never throws" |
| `SchemaRequestValidator.cs`, `SchemaServiceCollectionExtensions.cs` | 90 | 90 | 0 | Mapping and registration are exercised by unit, DI and integration tests; the null checks are guard-tested |
| `SchemaDiagnostics.cs` | 90 | 0 | 0 | Internal static type with no argument validation; guard tests cannot reach it (internal, no `InternalsVisibleTo` for `Encina.GuardTests`); a listener test exercises it |
| `SchemaLog.cs` | 90 | 0 | 0 | Source-generated methods exercised by the logging test; no parameters to guard |
| `SchemaParseOptions.cs`, `SchemaIssue.cs`, `SchemaErrorCodes.cs`, `ISchema.cs`, `SchemaIssueFactory.cs` | 90 | 0 | 0 | Option defaults and value types are exercised by unit tests; no executable validation beyond what the unit tests reach, so guard tests add nothing; `ISchema.cs` is an interface with no executable code |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #235 - the Zod-style schema validation package Encina.Validation.Schema.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always.
- Validation follows the orchestrator pattern (AGENTS.md sections 3 and 5): IValidationProvider, ValidationOrchestrator,
  ValidationPipelineBehavior live in core; FluentValidation, DataAnnotations and MiniValidator stay consistent.
- Railway Oriented Programming: Either<EncinaError, T>; every new error code carries an ErrorCategory (ADR-062).

IMPLEMENTATION OVERVIEW:
1. New package skeleton and contracts (ISchema<T>, SchemaIssue, SchemaParseOptions, error codes).
2. Immutable schemas: string/number/boolean/date/array/object with Optional, Nullable, Default, Coerce, Refine,
   Transform, Merge/Extend/Pick/Omit over the same T, and discriminated unions.
3. Parse(JsonElement|string): bounded normalize-then-deserialize, never throws; Validate(T) returns ValidationResult.
4. SchemaRequestValidator<T> adapts a schema to IRequestValidator<T> (plans of #230 and #233) so it runs under all three
   providers; EventIds 620-622 in a range registered first; per-package ActivitySource and Meter with a tenant tag.
Eight pull requests (S1 to S8), see the Estimated File Count table.

KEY PATTERNS:
- No input value in any issue, message, log or tag; codes, counts and bounded names only.
- Regexes are non-backtracking with a timeout; every limit fails closed.
- New package touch points: csproj, Encina.slnx, test project references, coverage-weights.json, manifest, PublicAPI.
- PublicAPI.Unshipped.txt, manifests and changelog through mechanical-fixer; documentation through docs-writer.

REFERENCE FILES:
- src/Encina.MiniValidator/ (package shape), src/Encina/Validation/*.cs
- src/Encina.Cdc/Caching/Diagnostics/CacheInvalidationActivitySource.cs
- docs/plans/validation-schema-builder-implementation-plan-235.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Schema validation is per request, as the issue says; schemas themselves are built once and shared |
| 2 | OpenTelemetry | ✅ Phase 7 | One `ActivitySource` and `Meter` `Encina.Validation.Schema`, duration and outcome by operation, tenant tag only when `TenantId` is present (ADR-054 D3, D4); the orchestrator-level view is #726's |
| 3 | Structured Logging | ✅ Phase 7 | `SchemaLog` EventIds 620-622 in the new range 620-629, by schema name, code and counts only |
| 4 | Health Checks | ❌ N/A | In-process, no checkable dependency |
| 5 | Validation | ✅ All phases | This is the validation feature; it joins the three providers through the neutral contract of #230 and an equivalence test pins the result |
| 6 | Resilience | ❌ N/A | No external call; regex time and parse size limits are the bounded-work protection (register rows C2, C3) |
| 7 | Distributed Locks | ❌ N/A | No shared state to coordinate |
| 8 | Transactions | ❌ N/A | Validation runs before the handler |
| 9 | Idempotency | ❌ N/A | Schema validation is deterministic and stateless |
| 10 | Multi-Tenancy | ⏭️ Follow-up | Tenant-specific schemas (the issue defers them): draft `artifacts/issues/validation-schema-tenant-module-scoping.md`; no tenant id is stored or keyed here (register row C9), so there is no tenant-qualified id to make injective |
| 11 | Module Isolation | ⏭️ Follow-up | Module-scoped schema definitions: the same draft |
| 12 | Audit Trail | ⏭️ Follow-up | Auditing of rejected requests is a follow-up of the family (accompanies the plan of #230); the schema issues carry no values |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| Validation orchestrator pattern (`IValidationProvider`, `ValidationOrchestrator`) and `ValidationResult` | ✅ On `main` | S1 to S5 and S7 need only these |
| ADR-062 error category contract (`EncinaErrors.Create` with a category) | ✅ Merged | Codes of register row C1 |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| `IRequestValidator<T>` and the runner | #230 | Required for S6 only (Design Choice 1) |
| `ValidationError.ErrorCode`/`Arguments` and the localizer | #233 | Required for S6's coded issues; S1 to S5 are independent |
| Generator project for compile-time schemas | #227 | The follow-up of Design Choice 6 (draft: `artifacts/issues/validation-schema-source-generator.md`) |
| Central validation instrumentation | #726 | Orchestrator-level metrics; this package instruments its own operations |
| Code-less validation `Left` never becomes a 400 | #3415 | Independent: `Parse`'s `Left` carries a code |

---

## Next Steps

1. Link this plan from #235 and ask the maintainer to answer Design Choices 1, 2 and 5 and register rows C1, C2, C5 and C11 when work starts.
2. Open the follow-up issues when the first pull request opens: the source-generated form of schemas (draft: `artifacts/issues/validation-schema-source-generator.md`) and tenant and module scoped schemas (draft: `artifacts/issues/validation-schema-tenant-module-scoping.md`).
3. Implement S1 to S8 as the pull requests of the Estimated File Count split, `Fixes #235` on S8.
