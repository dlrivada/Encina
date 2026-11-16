# Implementation Plan: Schema-First Validation (`Encina.Validation.Schema`)

> **Issue**: [#235](https://github.com/dlrivada/Encina/issues/235)
> **Type**: Feature
> **Related**: #230 (owns `IRequestValidator<T>`, the runner and EventIds 550-559; the adapter of S6 builds on it), #233 (owns `ValidationError.ErrorCode`, the message keys and the localizer this package emits through), #227 (owns the source-generator project; the compile-time form of schemas is a follow-up), #232 (security-focused validation, a sibling that shares no code), #234 and #236 (planned apart), #726 (central validation instrumentation), #3415 (a code-less validation `Left` never becomes a 400), ADR-001, ADR-006, ADR-054, ADR-062 (merged: error category contract)
> **Complexity**: High (a new package with a fluent type-safe API, a JSON normalization pass, composition, a telemetry layer and an integration with the validation runner)
> **Estimated Scope**: ~2,450 lines of production code + ~3,800 lines of tests + ~450 lines of documentation (about 6,700 lines in total, the table in "Estimated File Count"), in eight pull requests of at most 15 files and 1,500 lines each

---

## Summary

### What exists today (read on 2026-11-16, main `69a71e093`)

- All validation in Encina is rule-attached-to-a-type: FluentValidation `AbstractValidator<T>` classes, DataAnnotations attributes and MiniValidation (which reads the same attributes), run by the one registered `IValidationProvider` after the request is bound (`ValidationPipelineBehavior`, `ValidationOrchestrator`; `src/Encina/Validation/`). The only provider-neutral result is `ValidationResult` with `ValidationError(PropertyName, ErrorMessage)` (`ValidationResult.cs:97`).
- There is no schema concept, no package `Encina.Validation.Schema`, no `Schema` type, no parser from raw input, no type coercion, no composition, no discriminated union and no limits on untrusted input (`git grep -n "Encina.Validation.Schema" -- src` is empty). The only Roslyn component in `src` is `Encina.Security.ABAC.Analyzers`; the generator project of #227 does not exist yet.
- The issue's `Parse` returns `Either<ValidationErrors, User>`; the rule of `AGENTS.md` section 3 is `Either<EncinaError, T>`, and `ValidationErrors` is not a type that exists.
- A new satellite package costs a fixed set of touch points, all seen in `Encina.MiniValidator`: a `.csproj` with `PublicAPI` analyzers and `InternalsVisibleTo`, an entry in `Encina.slnx`, a `ProjectReference` in each test project that uses it (`tests/Encina.UnitTests`, `GuardTests`, `PropertyTests`, `ContractTests`, `IntegrationTests`, `BenchmarkTests`), an entry in `.github/scripts/coverage-weights.json`, a coverage manifest, `PublicAPI.Shipped.txt` and `Unshipped.txt`, a README (`Encina.MiniValidator.csproj` packs the root `README.md` with `Link="README.md"`, lines 8 and 25), and an entry in `docs/INVENTORY.md`.
- Observability convention (ADR-054 D3, D4): one `ActivitySource` and one `Meter` per package named `Encina.<Package>`, zero cost without a listener, a tenant tag only when multi-tenancy is on; the model in the repository is `src/Encina.Cdc/Caching/Diagnostics/CacheInvalidationActivitySource.cs` and `CacheInvalidationMetrics.cs`.
- The ambient request context exists: `IRequestContextAccessor` (`src/Encina/Abstractions/IRequestContextAccessor.cs:54`, registered with `TryAddSingleton` in `RequestIdentityServiceCollectionExtensions.cs:49`) exposes `IRequestContext? RequestContext`, and `IRequestContext.TenantId` is `string?` (`IRequestContext.cs:125`).

### What this plan delivers

A new opt-in package `Encina.Validation.Schema`: a fluent, type-safe schema API in the spirit of Zod, built from immutable schema objects.

- **Primitives**: `Schema.String()` (`Min`, `Max`, `Length`, `Email`, `Url`, `Guid`, `Regex`, `Trim`), `Schema.Int()`/`Number<T>()` (generic over `INumber<T>`: `Min`, `Max`, `Positive`, `Integer`), `Schema.Boolean()`, `Schema.Date()`, plus `Optional()`, `Nullable()` and `Default(value)` on any schema, and a `Coerce` form for string-to-number/bool/date.
- **Composites**: `Schema.Object<T>(...)` with typed property rules, `Schema.Array(item)` with `Min`/`Max`/`NonEmpty`, `Schema.DiscriminatedUnion<TBase>(...)`, composition over the same `T` (`Merge`, `Extend`, `Pick`, `Omit`), `Refine` and `Transform`.
- **Two entry points**: `Validate(T value)` returns a provider-neutral `ValidationResult` (what a bound request needs); `Parse(JsonElement)` or `Parse(string json)` validates untrusted raw input, normalizes it and returns `Either<EncinaError, T>` with code `validation.schema_failed` (category `Validation`) carrying JSON-pointer paths, .NET property paths and rule codes, never values.
- **Integration**: a schema becomes an `IRequestValidator<TRequest>` (the contract of #230), so it runs inside `ValidationOrchestrator` under any of the three providers and emits `ValidationError`s with the code and arguments of #233, which makes the messages localizable.
- **Safety and telemetry**: bounded depth, input size, node count, issue count and regular-expression time; every failure of user code or of a regex is a defined, fail-closed result (register rows C13 and C14); `Encina.Validation.Schema` activity source and meter with a tenant tag when `TenantId` is present, and an `ILogger` and tenant value carried by `SchemaParseOptions` (register row C24); EventIds 620-622.

Nothing changes for an application that does not reference the package. The compile-time (source-generated) form named in the issue's task list is a follow-up on the generator project of #227 (Design Choice 6).

- Affected packages: `Encina.Validation.Schema` (new). `Encina` (core) receives only the EventId range registration; the three provider packages are untouched.
- Provider category: Validation (3) (`AGENTS.md` section 5): schemas run under FluentValidation, DataAnnotations and MiniValidator through the neutral contract of #230, and an equivalence test proves the same rule gives the same result under each. It does not add a fourth `IValidationProvider` (Design Choice 1).
- Shared pieces: consumes `IRequestValidator<T>` and `RequestValidatorRunner` (#230), `ValidationError.ErrorCode`/`Arguments` and the message keys (#233). Owns `ISchema<T>`, `SchemaBase<T>`, `SchemaIssue` and the schema error codes.

### How the pieces compile in order

The compile order of the eight pull requests is a design constraint, not a detail:

- S1 creates `ISchema<T>`, the public abstract `SchemaBase<T>` (an `internal` constructor, so only this package derives from it) and the internal `SchemaContext`. `SchemaBase<T>` implements the public `Validate` and `Parse` once: `Parse` enforces the limits of register rows C2 and C14, parses to a mutable `JsonNode`, calls the internal abstract `Normalize(JsonNode?, SchemaContext)` of the concrete schema, then deserializes the normalized node; `Validate` calls the internal abstract `ValidateCore(T, SchemaContext)`. Every schema of S2 to S5 therefore compiles and parses from the day it exists, and S5 adds no parse infrastructure.
- S2 creates the wrappers `Optional()`, `Nullable()` and `Default(value)` together with the first schemas that use them. S3 and S4 reuse them.
- `SchemaFactory` (the `p` of `p => p.String().Min(1)` in `Property(...)`) is created in S4, after every schema it exposes exists (S2 and S3).
- Each pull request lists its own tests and its own per-file manifest targets; no pull request uses a type of a later one.

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

- Request types in Encina are usually records with constructors, which only C and B can build; C does it without per-schema boilerplate and reuses the serializer the application already configures (`SchemaParseOptions.JsonSerializerOptions`, camelCase by default, register row C23).
- A wins for mutable DTOs with many transforms; B wins when a type has invariants enforced in its constructor that must run at parse time (an always-valid type, #228).

**Class:** Dilemma

</details>

<details>
<summary><strong>3. The error a failed parse returns</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Either<EncinaError, T>` with code `validation.schema_failed`, `ErrorCategory.Validation`, details holding the issue list (JSON pointer, .NET property path, rule code, arguments) and never an input value** | Follows ROP; the category drives the HTTP status through ADR-062; issues are machine-readable | The details dictionary holds a list of small records |
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
| **A) A new package `Encina.Validation.Schema`, as the issue names it** | Opt-in (pay for what you use); core stays small; matches the issue's "New Package" | The fixed touch points of a new package (csproj, solution, six test project references, weights, manifest, README, inventory) |
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
| C1 | Error codes and categories (ADR-062), created with `EncinaErrors.Create(code, category, message, ...)`: `validation.schema_failed` (`Validation`, R3), `validation.schema_limit_exceeded` (`InvalidRequest`, R3), `validation.schema_refinement_failed` (`Internal`, R4: a user delegate threw; the `Left` carries the code only) | Dilemma (recommended, pending the maintainer) | public codes; `InvalidRequest` versus a dedicated payload-too-large category is the open choice (ADR-062 has no such category) |
| C2 | Untrusted input is bounded and fails closed: `MaxDepth = 32`, `MaxInputBytes = 1 MiB` for `Parse(string)`, `MaxNodes = 10,000` (C14), `MaxIssues = 100` (C21), `RegexTimeout = 1 s`; a limit violation returns the `Left` of C1 and never a partial success | Dilemma (recommended, pending the maintainer) | numeric defaults; the validation of options rejects zero and negative values with `ArgumentOutOfRangeException` at construction |
| C3 | Regular expressions use `RegexOptions.NonBacktracking` with the match timeout of C2; a pattern the engine cannot compile as non-backtracking (back-references, lookarounds) is rejected when the schema is built, not at parse time | Dilemma (recommended, pending the maintainer) | OWASP "Regular expression Denial of Service (ReDoS)" guidance and Microsoft "Best practices for regular expressions in .NET" also allow the backtracking engine with a match timeout. NonBacktracking runs in linear time but rejects back-references and lookarounds, a user-visible restriction; backtracking with a timeout accepts every pattern but bounds the cost only by the timeout |
| C4 | Messages are templates with named arguments (the schema parameters, for example `Min`); an input value is never part of a message, an issue, a log line, an activity tag or `EncinaError.Message` | Settled | `AGENTS.md` section 3 (`EncinaError.Message` never reaches logs; #1168, #1173, #1259, #1274) and ADR-054 settled point 2 |
| C5 | Every issue carries `ErrorCode` and `Arguments` (the fields of #233) with the key convention `validation.<rule>`; the English default text is in the package, translation goes through the localizer of #233 when registered | Dilemma (recommended, pending the maintainer) | follows #233 (a pending plan); the alternative is plain English messages with no keys until #233 is implemented |
| C6 | Schemas are immutable; every fluent call returns a new schema, so a schema built once at start-up is safe to share across threads and requests | Trivial | The fluent immutable-builder idiom (`System.Collections.Immutable`, `string` operations) and `IValidationProvider`'s own contract ("implementations should be thread-safe and reusable", `IValidationProvider.cs:12`) |
| C7 | `Refine` and `Transform` take synchronous delegates; asynchronous or I/O-bound rules are written as `IRequestValidator<T>` (#230), which has timeout and parallelism control | Dilemma (recommended, pending the maintainer) | the rule of `AGENTS.md` section 3 on asynchronous database calls does not apply to an in-memory delegate. Synchronous keeps `Validate(T)` synchronous and total; async delegates (`Func<T, CancellationToken, ValueTask<bool>>`) would let one schema hold I/O rules at the cost of an async `ISchema<T>` surface |
| C8a | Telemetry shape: one `ActivitySource` and one `Meter` named `Encina.Validation.Schema`; every call guarded by `HasListeners`; the tenant tag is set on activities and metrics only when the tenant value is not null (multi-tenancy on, ADR-054 D4); no input value, only bounded names and codes | Settled | ADR-054 D3 and D4 and settled point 7; `src/Encina.Cdc/Caching/Diagnostics/CacheInvalidationActivitySource.cs` and `CacheInvalidationMetrics.cs` (the convention in code) |
| C8b | Instrument and tag names (public conventions): counter `encina.validation.schema.operations` (tags `operation` = `parse`/`validate`, `result` = `valid`/`invalid`/`error`, `schema` = the developer-chosen schema name, a bounded constant, never request data), histogram `encina.validation.schema.duration` in seconds | Dilemma (recommended, pending the maintainer) | ADR-054 fixes the `Encina.<Package>` pattern, not these names; the alternative is names under the #726 central validation instrumentation namespace so one dashboard covers the orchestrator and the schemas |
| C9 | No tenant id is part of a key, a cache or a schema registration, so no tenant-qualified id needs to be injective and the reserved `default` segment is not involved; tenant-specific schemas are a follow-up | Settled | The package holds no tenant-keyed state; ADR-054 D4 |
| C10 | EventIds 620-622 are packed from the start of the sub-block 620-629 of the block 600-649 assigned to this family; the range `ValidationSchema = (620, 629)` is registered first and the new assembly is mapped in `AssemblyRanges` | Trivial | `AGENTS.md` section 7; ADR-021; `artifacts/decisions/plan-batch/eventid-blocks.md` |
| C11 | `SchemaIssue` carries both paths: `Path` (the JSON pointer of the input, `/items/0/quantity`) and `PropertyName` (the .NET property path, `Items[0].Quantity`). The object schema builds both from the selector chain and the serializer naming, so a `[JsonPropertyName]` or naming policy cannot make them diverge; `ValidationError.PropertyName` receives `PropertyName`, the machine-readable `Path` stays in `EncinaError` details | Dilemma (recommended, pending the maintainer) | public behavior; the cost of two paths is one more field per issue and a naming map built from the selectors; the alternative is the JSON pointer alone in both places, simpler but different from the other three providers' property paths |
| C12 | No AOT or trimming claim is made: selectors are read as expression trees, property names come from reflection over `T`, and `JsonSerializer.Deserialize<T>` is reflection-based; whether any of it is trim-safe is not verified here and is a spike inside the generator follow-up (Design Choice 6) | Trivial | #235 has no AOT acceptance criterion (the issue's Acceptance Criteria list); no claim is made, so nothing needs verifying |
| C13 | `Validate(T)` returns `ValidationResult` and has no error channel, so it never throws and never reads valid on a failure of user code: a `Refine`/`Transform` delegate that throws, or a regex match timeout, yields an invalid result with one issue carrying the reserved code `validation.schema_refinement_failed` (delegate) or `validation.schema_limit_exceeded` with argument `limit = regex_timeout` (regex); the exception is logged by type through `ForLogging()` (S7), never its message. `Parse` returns the matching `Left` of C1 (`Internal` and `InvalidRequest`) | Dilemma (recommended, pending the maintainer) | fail-closed gate rule (`AGENTS.md` section 3). Propagating the exception to the runner of #230 (which maps a failure under its own policy) also fails closed but makes the standalone `Validate` throw; the recommended option keeps `Validate` total |
| C14 | `Parse(JsonElement)` is bounded before it is copied: the element is walked with its enumerators, counting nodes (`MaxNodes`) and depth (`MaxDepth`), and nothing is copied to a `JsonNode` until both limits pass; `MaxInputBytes` bounds only `Parse(string)` because an element is already parsed | Dilemma (recommended, pending the maintainer) | public limits and the numeric default `MaxNodes = 10,000`; the alternative is `element.GetRawText().Length` against `MaxInputBytes`, which allocates the whole text first |
| C15 | Unknown members: `Strict` (an unknown JSON member is an issue) is the default of an object schema; `Strip()` ignores them | Dilemma (recommended, pending the maintainer) | public behavior; `Strip` by default is friendlier to evolving clients, `Strict` fails closed on an unexpected member |
| C16 | `Optional()` means the member may be absent (an absent member is valid and binds `default`); `Nullable()` means an explicit `null` is valid; neither implies the other. `Default(value)` applies to an absent member only, not to an explicit `null` | Dilemma (recommended, pending the maintainer) | public semantics; the alternative applies the default to `null` as well, which hides a client that sent `null` on purpose |
| C17 | `Coerce` is off by default and opt-in per schema (`Coerce()`) or through `SchemaParseOptions.Coerce`; the string `"42"` is a type issue for an `Int` schema unless coerced | Dilemma (recommended, pending the maintainer) | public behavior; coercion on by default matches form posts, off by default matches JSON APIs |
| C18 | `Email`: one `@` with a non-empty local part and a domain with at least one dot, at most 254 characters, matched with a non-backtracking pattern; `Url`: an absolute URI with scheme `http` or `https` through `Uri.TryCreate`; `Guid`: `Guid.TryParseExact` with format `D` | Dilemma (recommended, pending the maintainer) | public acceptance sets; RFC-complete email or any URI scheme would accept values the application cannot use |
| C19 | `Unique` compares items with `EqualityComparer<TItem>.Default` over the typed values (strings ordinal) | Dilemma (recommended, pending the maintainer) | public behavior; case-insensitive or a caller-supplied comparer is the alternative |
| C20 | A JSON object with a duplicate key is rejected as malformed input (`validation.schema_failed`), never resolved by last-one-wins | Dilemma (recommended, pending the maintainer) | public behavior and a fail-closed choice; last-wins accepts ambiguous input that two parsers could read differently |
| C21 | When `MaxIssues` is reached, collection stops and the `Left` is `validation.schema_failed` with the issues collected so far and a `truncated = true` detail; it is not a limit error | Dilemma (recommended, pending the maintainer) | public behavior; the alternative returns `validation.schema_limit_exceeded` and drops the issue list, which tells the client less |
| C22 | When a caller's `JsonSerializerOptions.MaxDepth` is larger than the schema's `MaxDepth`, the smaller of the two applies | Dilemma (recommended, pending the maintainer) | fail-closed choice; the alternative lets the serializer option override the schema limit |
| C23 | `SchemaParseOptions.JsonSerializerOptions` defaults to `JsonSerializerDefaults.Web` (camelCase, case-insensitive reading) | Dilemma (recommended, pending the maintainer) | public default; the alternative is exact-name matching with no naming policy |
| C24 | Logger and tenant source: schemas are built once and shared, so neither is stored in a schema. `SchemaParseOptions` gains `ILogger? Logger` and `string? TenantId` (S7); a caller that wants logs and a tenant tag sets them per call, and the adapter of S6 fills them from `ILogger<SchemaRequestValidator<T>>` and from `IRequestContextAccessor?.RequestContext?.TenantId` (the accessor is registered with `TryAddSingleton`, so a singleton validator may hold it). With no logger nothing is logged; with no tenant no tenant tag is set (telemetry, not a gate, so absence does not deny) | Dilemma (recommended, pending the maintainer) | public API shape; the alternative is for the adapter alone to instrument, which leaves the standalone `Parse` and `Validate` unobserved |
| C25 | Programmer errors fail at build time, not at validation time: a selector that is not a plain property access, a duplicate property, `Pick`/`Omit`/`Merge` naming an unknown property, a regex that cannot compile non-backtracking, `Min` greater than `Max` throw `ArgumentException` (or `ArgumentOutOfRangeException`) when the schema is built; a `DiscriminatedUnion.Validate(T)` whose runtime type has no registered variant returns an invalid result with `validation.schema_failed` and argument `rule = union_variant` | Trivial | `AGENTS.md` section 9 (guard tests for every validated parameter); fail-closed rule of section 3 for the union case |

---

## Implementation Phases

### Phase 1: Package Skeleton, Contracts and Parse Core (pull request S1)

> **Goal**: The new package builds, is part of the solution and the unit test project, and holds the contracts and the one parse and validate implementation every later schema reuses; no concrete schema exists yet.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Validation.Schema/Encina.Validation.Schema.csproj` (modeled on `Encina.MiniValidator.csproj`: `PackageId`, description, `PackageReadmeFile` with the root `README.md` linked as `Encina.MiniValidator.csproj` does, which S8 replaces with the package README, `GenerateDocumentationFile`, a reference to `Encina` and `Microsoft.Extensions.DependencyInjection.Abstractions`/`Microsoft.Extensions.Logging.Abstractions`, the `PublicApiAnalyzers`, and an `InternalsVisibleTo` entry for `Encina.UnitTests` (guard tests reach only public members, so none is added for `Encina.GuardTests`)), `PublicAPI.Shipped.txt` (`#nullable enable`) and `PublicAPI.Unshipped.txt`.
2. **Create** the contracts: `ISchema.cs` (`public interface ISchema<T>`: `string Name { get; }`, `ValidationResult Validate(T value, SchemaParseOptions? options = null)`, `Either<EncinaError, T> Parse(JsonElement input, SchemaParseOptions? options = null)`, `Either<EncinaError, T> Parse(string json, SchemaParseOptions? options = null)`), `SchemaIssue.cs` (`record SchemaIssue(string Path, string PropertyName, string Code, IReadOnlyDictionary<string, object?>? Arguments)`, register row C11), `SchemaParseOptions.cs` (limits of register rows C2 and C14, `RegexTimeout`, `TimeProvider`, `JsonSerializerOptions`, `Coerce`), `SchemaErrorCodes.cs` (the three codes of row C1 and their categories).
3. **Create** `SchemaBase.cs` (`public abstract class SchemaBase<T> : ISchema<T>` with an `internal` constructor; `Parse(string)` checks `MaxInputBytes`, `Parse(JsonElement)` walks the element for `MaxNodes` and `MaxDepth` (register row C14), both then call the internal abstract `Normalize(JsonNode?, SchemaContext)` and deserialize the normalized node; `Validate` calls the internal abstract `ValidateCore(T, SchemaContext)`; a delegate that throws and a regex timeout are mapped as register row C13 says) and `SchemaContext.cs` (internal: options, the JSON-pointer and .NET path stacks, issue collection up to `MaxIssues`, the one place that builds an issue with the key `validation.<rule>`).
4. **Modify** `Encina.slnx`, `tests/Encina.UnitTests/Encina.UnitTests.csproj` (a `ProjectReference`) and `.github/scripts/coverage-weights.json` (the package in the category that lists `Encina.FluentValidation`, `Encina.DataAnnotations`, `Encina.MiniValidator`); **create** `.github/coverage-manifest/Encina.Validation.Schema.json`; `PublicAPI.Unshipped.txt` lines; all through `mechanical-fixer`.
5. Tests (new): `SchemaFoundationTests` (unit: options defaults, the error-code constants and their categories through `EncinaErrors.Create`, `SchemaIssue` equality, and the parse core through a test-only `SchemaBase<T>` subclass in the test file: size, depth, node and issue limits each return the `Left` of row C1 and never a partial success, a throwing `ValidateCore` gives an invalid result and not an exception, duplicate keys and trailing data give a `Left`). The guard tests of `SchemaBase<T>` and `SchemaParseOptions` live in S2's guard file (`SchemaPrimitivesGuardTests`) because the guard project reference is added there; their guard targets are entered in the manifest by S2.

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
2. Create SchemaBase<T> (public abstract, internal constructor) and the internal SchemaContext: Parse and Validate are
   implemented once there; concrete schemas override the internal Normalize and ValidateCore.
3. Add the package to Encina.slnx, the UnitTests project reference, coverage-weights.json and a new coverage manifest.

KEY RULES:
- Parse returns Either<EncinaError, T>; error codes are created with the categorized EncinaErrors.Create (ADR-062).
- Never put an input value in an issue, message or log. XML docs on every public member.
- Parse and Validate never throw: register rows C13 and C14 define the failure of user code and the bound of JsonElement.
- Solution, csproj references, weights and manifest edits go through mechanical-fixer; edit code with Edit/Write only.

REFERENCE FILES:
- src/Encina.MiniValidator/Encina.MiniValidator.csproj, ServiceCollectionExtensions.cs
- .github/coverage-manifest/Encina.MiniValidator.json
- docs/plans/validation-schema-builder-implementation-plan-235.md (register rows C1, C2, C11, C13, C14)
```

</details>

---

### Phase 2: Primitive and Wrapper Schemas (pull request S2)

> **Goal**: String and number schemas with the shared fluent chain (`Optional`, `Nullable`, `Default`, `Coerce`), testable alone; the wrappers they use are created here.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `Schema.cs` (`public static partial class Schema`: `String()`, `Int()`, `Number<T>() where T : struct, INumber<T>`), `StringSchema.cs` (`Min`, `Max`, `Length`, `Email`, `Url`, `Guid`, `Regex(pattern)` per register rows C3 and C18, `Trim`, `OneOf(values)`), `NumberSchema.cs` (`NumberSchema<T>`: `Min`, `Max`, `Positive`, `Negative`, `Integer`, `Coerce`), and the three wrapper decorators `OptionalSchema.cs`, `NullableSchema.cs`, `DefaultSchema.cs` behind `Optional()`, `Nullable()` and `Default(value)` (register row C16).
2. **Modify** `tests/Encina.GuardTests/Encina.GuardTests.csproj` and `tests/Encina.PropertyTests/Encina.PropertyTests.csproj` (a `ProjectReference` each), `PublicAPI.Unshipped.txt` and the manifest (this PR also enters the guard targets of `SchemaBase.cs` and `SchemaParseOptions.cs`) through `mechanical-fixer`.
3. Tests (new): `StringSchemaTests` (including a regex timeout through a tiny `RegexTimeout`, register row C13), `NumberSchemaTests` (including `int`, `long`, `decimal`, `double` boundaries and `Coerce`), `WrapperSchemaTests` (optional, nullable and default semantics of register row C16) (unit); `SchemaPrimitivesGuardTests` (guard: null/empty pattern, negative length, `Min` greater than `Max`, null `OneOf`, null `json` and null `options` members of `SchemaBase<T>`, non-positive limits of `SchemaParseOptions`; `Optional()` on an optional schema is a valid case); `PrimitiveSchemaProperties` (FsCheck: a value inside `[Min, Max]` is valid and one outside is not for any bounds; `Validate` never throws for any string).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #235 plan docs/plans/validation-schema-builder-implementation-plan-235.md, phase 2 of 8. Depends on S1.

TASK:
1. Implement Schema.String(), Schema.Int(), Schema.Number<T>() (INumber<T>) as immutable schema objects deriving from
   SchemaBase<T>: every fluent call returns a new instance. Issues carry a JSON-pointer path, a .NET property path,
   a code and named arguments.
2. Implement the Optional, Nullable and Default wrappers (register row C16) here, so the chain compiles in this PR.
3. Regex rules use RegexOptions.NonBacktracking and the RegexTimeout of the options; reject patterns that cannot compile so.

KEY RULES:
- No input value in any issue or message. Immutable schemas, safe to share across threads.
- Tests execute real schema code; guard tests for every validated parameter.

REFERENCE FILES:
- docs/plans/validation-schema-builder-implementation-plan-235.md (register rows C3, C4, C5, C6, C16, C18)
- src/Encina/Validation/ValidationResult.cs
```

</details>

---

### Phase 3: Boolean, Date and Array Schemas (pull request S3)

> **Goal**: The remaining leaf and collection schemas.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `BooleanSchema.cs`, `DateSchema.cs` (`DateOnly` and `DateTimeOffset`; `Min`, `Max`, `Past`, `Future` through the `TimeProvider` of `SchemaParseOptions`, never `DateTime.UtcNow`) and `ArraySchema.cs` (`ArraySchema<TItem>`: `Min`, `Max`, `NonEmpty`, `Unique` per register row C19; item issues get indexed paths); **modify** `Schema.cs` (the `Schema.Boolean()`, `Schema.Date()` and `Schema.Array(item)` factory members).
2. **Modify** `PublicAPI.Unshipped.txt` and the manifest through `mechanical-fixer`.
3. Tests (new): `BooleanSchemaTests`, `DateSchemaTests` (with `FakeTimeProvider`), `ArraySchemaTests` (indexed paths, nested issues, `Unique`) (unit); `CollectionSchemaGuardTests` (guard); `DateSchemaProperties` (FsCheck: `Past` and `Future` are complementary around the fake clock).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #235 plan docs/plans/validation-schema-builder-implementation-plan-235.md, phase 3 of 8. Depends on S2.

TASK:
1. Implement BooleanSchema, DateSchema (DateOnly, DateTimeOffset) and ArraySchema<TItem>; reuse the wrappers of S2.
2. Time comes from the TimeProvider in SchemaParseOptions (default TimeProvider.System); never DateTime.UtcNow.

KEY RULES:
- Immutable schemas; item issues carry indexed JSON-pointer and .NET paths; no input value in any issue.
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

1. **Create** `ObjectSchema.cs` (`ObjectSchema<T>`: `ValidateCore` reads each property through a getter compiled once from the selector expression; `Strict()` (the default, register row C15) rejects unknown JSON members, `Strip()` ignores them), `SchemaFactory.cs` (the `p` of `p => p.String().Min(1)`: exposes `String()`, `Int()`, `Number<T>()`, `Boolean()`, `Date()` and `Array(item)` of S2 and S3), `ObjectSchemaBuilder.cs` (`Property<TProp>(Expression<Func<T, TProp>> selector, Func<SchemaFactory, ISchema<TProp>> rule)`; the JSON name resolves from the serializer options and `[JsonPropertyName]`, the .NET name from the selector chain, so each issue gets both paths, register row C11), `RefinedSchema.cs` (`Refine(Func<T, bool> predicate, string code)` with a rule code and arguments; a delegate that throws is handled by register row C13) and `TransformedSchema.cs` (`Transform<TOut>(Func<T, TOut>)` on typed results), and the composition members `Merge`, `Extend`, `Pick`, `Omit` over the same `T` (Design Choice 5; programmer errors per register row C25); **modify** `Schema.cs` (`Schema.Object<T>(...)`).
2. **Modify** `PublicAPI.Unshipped.txt` and the manifest through `mechanical-fixer`.
3. Tests (new): `ObjectSchemaTests` (the issue's `User` example, nested objects, both paths of an issue under a naming policy and `[JsonPropertyName]`, unknown members under `Strict`/`Strip`), `SchemaCompositionTests` (`Merge`, `Extend`, `Pick`, `Omit` and their effect on a fixture type), `RefineTransformTests` (predicate, transform result, a throwing delegate gives an invalid `Validate` result and the `Internal` `Left` of `Parse`, register row C13), `ObjectSchemaGuardTests` (null selector, a selector that is not a plain property access, a duplicate property, `Pick`, `Omit` and `Merge` of an unknown property), `ObjectSchemaProperties` (FsCheck: composition is associative for disjoint rule sets and `Validate(Parse(x))` agrees with `Parse(x)`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Issue #235 plan docs/plans/validation-schema-builder-implementation-plan-235.md, phase 4 of 8. Depends on S2 and S3.

TASK:
1. ObjectSchema<T> with typed property rules from selector expressions (read the expression tree once at build time,
   compile getters once), Strict/Strip, Refine and Transform, and Merge/Extend/Pick/Omit over the same T; SchemaFactory
   is created here.
2. A refinement delegate that throws: Validate returns an invalid result with validation.schema_refinement_failed, Parse
   returns the Left of that code (Internal) carrying the code only; the exception is logged later by type and stack trace.

KEY RULES:
- Immutable; composition returns new schemas; thread-safe after construction.
- Property names resolve through the JsonSerializerOptions naming policy and [JsonPropertyName]; every issue carries both
  the JSON pointer and the .NET property path.

REFERENCE FILES:
- docs/plans/validation-schema-builder-implementation-plan-235.md (Design Choices 2 and 5, register rows C11, C13, C15, C25)
- src/Encina/Results/EncinaErrors.cs
```

</details>

---

### Phase 5: Discriminated Unions, End-to-End Parse and Contract Tests (pull request S5)

> **Goal**: `DiscriminatedUnionSchema`, the end-to-end proof that `Parse` is bounded and hostile-input-safe across every schema, and the contract tests of the public `ISchema<T>`.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `DiscriminatedUnionSchema.cs` (`Schema.DiscriminatedUnion<TBase>("type", (value, schema)...)`: selects by the discriminator, an unknown or missing discriminator is an issue, never an exception; `Validate(T)` selects by the runtime type and an unregistered runtime type is an invalid result, register row C25); **modify** `Schema.cs` (the union factory).
2. **Modify** `PublicAPI.Unshipped.txt`, `tests/Encina.ContractTests/Encina.ContractTests.csproj` (a `ProjectReference`) and the manifest through `mechanical-fixer`.
3. Tests (new): `DiscriminatedUnionSchemaTests` (including an unregistered runtime type under `Validate`), `SchemaParseTests` (valid input, coercion, defaults, positional-record binding, unknown members, trailing data, duplicate keys, malformed JSON gives a `Left`, never an exception), `SchemaHostileInputTests` (a corpus of depth bombs, 100,000-element arrays, huge strings, repeated keys, numbers beyond `decimal` range, an oversized `JsonElement`; each returns the `validation.schema_limit_exceeded` or `validation.schema_failed` `Left` within the limits and with no value echoed), `SchemaParseProperties` (FsCheck: `Parse` never throws for any string input) and `tests/Encina.ContractTests/Validation/SchemaContractTests.cs` (contract: real `ISchema<T>` implementations of every schema kind keep the contract: `Validate` and `Parse` never throw, a `Left` carries a code and an `ErrorCategory`, the same input is deterministic, a schema is reusable and thread-safe).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
Issue #235 plan docs/plans/validation-schema-builder-implementation-plan-235.md, phase 5 of 8. Depends on S4.

TASK:
1. Implement DiscriminatedUnionSchema<TBase> on the SchemaBase<T> of S1 (Normalize and ValidateCore overrides).
2. Prove the Parse limits end to end (register rows C2, C14, C20, C21, C22) with a hostile-input corpus, and add the
   ISchema<T> contract tests with real implementations.

KEY RULES:
- Parse(string) and Parse(JsonElement) never throw for any input: every failure is a Left with a code and a category.
- Fail closed: a limit violation is a Left, never a partial success. No input value in any issue.
- Tests include a hostile-input corpus; no Thread.Sleep.

REFERENCE FILES:
- docs/plans/validation-schema-builder-implementation-plan-235.md (register rows C1 to C4, C13, C14, C25)
- src/Encina/Results/EncinaErrors.cs
```

</details>

---

### Phase 6: Integration with the Validation Runner, DI and Benchmark (pull request S6)

> **Goal**: A schema runs inside the validation pipeline under all three providers. Depends on the merged runner of #230 and the coded error of #233.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `SchemaRequestValidator.cs` (`SchemaRequestValidator<TRequest> : IRequestValidator<TRequest>`; maps `SchemaIssue` to `ValidationError(PropertyName, ErrorMessage, ErrorCode, Arguments)` per register rows C5 and C11) and `SchemaServiceCollectionExtensions.cs` (`AddEncinaSchema<TRequest>(this IServiceCollection, ISchema<TRequest> schema)`: `TryAddEnumerable` of a singleton validator, which is safe because schemas are immutable and hold no scoped dependency; it resolves `ILogger<>` and the optional singleton `IRequestContextAccessor`, so the registration test adds logging).
2. **Modify** `tests/Encina.IntegrationTests/Encina.IntegrationTests.csproj` and `tests/Encina.BenchmarkTests/Encina.Benchmarks/Encina.Benchmarks.csproj` (a `ProjectReference` each), `PublicAPI.Unshipped.txt` and the manifest through `mechanical-fixer`.
3. **Create** `tests/Encina.BenchmarkTests/Encina.Benchmarks/SchemaValidationBenchmarks.cs` (the `User` schema against the same request validated by FluentValidation and DataAnnotations).
4. Tests (new): `SchemaRequestValidatorTests` (unit), `SchemaRequestValidatorGuardTests`, `SchemaRegistrationTests` (DI with `ValidateOnBuild` and `ValidateScopes`, the runner resolves the schema validator), `SchemaProviderEquivalenceTests` in `tests/Encina.UnitTests/Validation/` (the same rule as a schema, as a FluentValidation rule, as a DataAnnotations attribute and as a MiniValidator attribute gives equal `ValidationResult`s under each provider), and `tests/Encina.IntegrationTests/Validation/{DataAnnotations,FluentValidation,MiniValidator}/SchemaValidationPipelineTests.cs` (through `IEncina.Send`, no container).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
Issue #235 plan docs/plans/validation-schema-builder-implementation-plan-235.md, phase 6 of 8. Requires the merged
IRequestValidator<T> and runner (issue #230) and ValidationError.ErrorCode/Arguments (issue #233).

TASK:
1. SchemaRequestValidator<TRequest> adapts ISchema<TRequest>.Validate to IRequestValidator<TRequest>, mapping each
   SchemaIssue to a ValidationError with code, arguments and the .NET property path.
2. AddEncinaSchema<TRequest>(schema) registers it as a singleton with TryAddEnumerable; prove it with a ValidateOnBuild
   and ValidateScopes DI test; add the equivalence test and three integration tests through IEncina.Send.

KEY RULES:
- A schema is immutable and is registered as a singleton; no scoped dependency enters it.
- Existing providers are not modified; PublicAPI.Unshipped.txt and the manifest through mechanical-fixer.

REFERENCE FILES:
- issue #230 (the contract, IRequestValidator<T>) and issue #233 (the coded error), by number
- src/Encina/Abstractions/IRequestContextAccessor.cs
```

</details>

---

### Phase 7: Observability (pull request S7)

> **Goal**: Traces, metrics and logs of schema operations by code and counts only; the logger and the tenant reach the schema through `SchemaParseOptions` (register row C24).

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina/Diagnostics/EventIdRanges.cs`: register `ValidationSchema = (620, 629)`; **modify** `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (map the assembly `Encina.Validation.Schema` to the new range) and the core `PublicAPI.Unshipped.txt`.
2. **Create** `src/Encina.Validation.Schema/Diagnostics/SchemaDiagnostics.cs` (internal static: the `ActivitySource` and `Meter` of register rows C8a and C8b, `HasListeners` guards) and `SchemaLog.cs` (`[LoggerMessage]`, packed): 620 a parse rejected the input (debug: schema name, issue count), 621 a limit was exceeded (warning: schema name, limit name), 622 a refinement threw (error: schema name, exception through `ForLogging()`). 623-629 stay free. XML docs name the range.
3. **Modify** `SchemaParseOptions.cs` (and the package `PublicAPI.Unshipped.txt`): `ILogger? Logger` and `string? TenantId` (register row C24). **Modify** `SchemaBase.cs`: the root `Parse` and `Validate`, which every schema inherits, call one internal measuring helper (activity, counter, histogram) and log by code; the tenant tag is set only when `TenantId` is not null.
4. **Modify** both manifests (`Encina.json`, `Encina.Validation.Schema.json`) through `mechanical-fixer`.
5. Tests (new): `SchemaDiagnosticsTests` (a listener observes the activity and the counter with the expected tags, no listener allocates nothing observable, the tenant tag appears only with a `TenantId`), `SchemaLogTests` (EventIds and no values in rendered messages), `SchemaInstrumentationTests` (a refinement exception produces id 622 and a `Left`; a limit violation produces id 621).

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
   Parse/Validate in SchemaBase<T>; add Logger and TenantId to SchemaParseOptions.

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

1. **Create** `docs/features/validation-schema.md` and `src/Encina.Validation.Schema/README.md` through `docs-writer` (the API by example, `Validate` versus `Parse`, composition, limits and security notes, the three provider paths, telemetry); **modify** `Encina.Validation.Schema.csproj` so `PackageReadmeFile` packs the package README instead of the root one.
2. **Modify** `docs/INVENTORY.md` and `docs/features/index.md` through `docs-writer`.
3. **Create** `changelog.d/235-validation-schema.added.md` through `mechanical-fixer`, and the Load-test justification file `tests/Encina.LoadTests/Validation/SchemaValidation.md` (`AGENTS.md` section 9 template: `# Load Tests - Validation Schema`, `## Status: Not Implemented`, numbered reasons: schemas are immutable and stateless, so there is no concurrency to test beyond the contract test's thread-safety case; adequate coverage from other test types; recommended alternative; related files; date; issue). A contract-test justification is never allowed, and none is written: the contract tests are in S5.
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
| ADR-054 D3, D4 and settled points 2, 7 | One meter and source per package; tenant tag when multi-tenancy is on; codes only; no cost without a listener | Register rows C8a, C8b, C24 |
| OWASP ReDoS guidance; Microsoft "Best practices for regular expressions in .NET" | Non-backtracking engine and match timeouts | Register row C3 |
| `System.Text.Json` (`JsonNode`, `JsonDocumentOptions.MaxDepth`, `JsonSerializerOptions`) | Mutable node tree, depth limit, naming policy | Normalization and limits (S1, S5) |
| .NET generic math (`INumber<T>`) | One numeric schema for every numeric type | `NumberSchema<T>` |
| RFC 6901 (JSON Pointer) | Path syntax for issue locations | Register row C11 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `ValidationResult`, `ValidationError` | `src/Encina/Validation/ValidationResult.cs` | Output of `Validate` and of the adapter |
| `IRequestValidator<T>`, `RequestValidatorRunner` (planned) | issue #230 | The integration contract (Design Choice 1) |
| `ValidationError.ErrorCode`/`Arguments`, the localizer (planned) | issue #233 | Coded, localizable issues (register row C5) |
| `EncinaErrors.Create` (categorized overload) | `src/Encina/Results/EncinaErrors.cs` | The three error codes |
| `IRequestContextAccessor`, `IRequestContext.TenantId` | `src/Encina/Abstractions/` | Tenant value the adapter hands to `SchemaParseOptions` (register row C24) |
| `CacheInvalidationActivitySource`, `CacheInvalidationMetrics` | `src/Encina.Cdc/Caching/Diagnostics/` | Model of the per-package source and meter |
| `Encina.MiniValidator` package shape | `src/Encina.MiniValidator/` | csproj, PublicAPI and registration conventions |
| `FakeTimeProvider` | `Microsoft.Extensions.TimeProvider.Testing` (central package version) | Date schema tests |
| `ValidationBenchmarks` | `tests/Encina.BenchmarkTests/Encina.Benchmarks/ValidationBenchmarks.cs` | Model for the schema benchmark |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Validation.Schema` (`ValidationSchema`, new, inside the block 600-649 assigned to the validation-2 family; the range is registered in core `EventIdRanges.cs`) | 620-629 | 620-622 used (three messages); 623-629 free; sibling plans in the same block: #233 600-609, #234 610-619, #236 630-639 |

### Estimated File Count

Every count includes tests, coverage manifest edits, PublicAPI files, project and solution edits, the README and the changelog fragment. No pull request uses a type created by a later one; each enters its per-file manifest targets with its own tests (S2 also enters the guard targets of the two S1 files its guard tests reach).

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| S1 | Skeleton, contracts, parse core | 9 (csproj, 2 PublicAPI, `ISchema`, `SchemaIssue`, `SchemaParseOptions`, `SchemaErrorCodes`, `SchemaBase`, `SchemaContext`) | 0 | 1 (`SchemaFoundationTests`) | 1 (`Encina.UnitTests.csproj`) | 1 (new manifest) | 2 (`Encina.slnx`, `coverage-weights.json`) | 14 | ~700 |
| S2 | Primitive schemas and wrappers | 6 (`Schema`, `StringSchema`, `NumberSchema`, `OptionalSchema`, `NullableSchema`, `DefaultSchema`) | 1 (PublicAPI) | 5 (string, number, wrappers, guard, property) | 2 (`GuardTests.csproj`, `PropertyTests.csproj`) | 1 | 0 | 15 | ~1,100 |
| S3 | Boolean, date, array | 3 (`BooleanSchema`, `DateSchema`, `ArraySchema`) | 2 (`Schema.cs`, PublicAPI) | 5 (boolean, date, array, guard, property) | 0 | 1 | 0 | 11 | ~800 |
| S4 | Object schemas and composition | 5 (`ObjectSchema`, `SchemaFactory`, `ObjectSchemaBuilder`, `RefinedSchema`, `TransformedSchema`) | 2 (`Schema.cs`, PublicAPI) | 5 (object, composition, refine/transform, guard, property) | 0 | 1 | 0 | 13 | ~1,100 |
| S5 | Unions, end-to-end parse, contract tests | 1 (`DiscriminatedUnionSchema`) | 2 (`Schema.cs`, PublicAPI) | 5 (union, parse, hostile corpus, property, contract) | 1 (`ContractTests.csproj`) | 1 | 0 | 10 | ~750 |
| S6 | Runner integration, DI, benchmark | 2 (`SchemaRequestValidator`, `SchemaServiceCollectionExtensions`) | 1 (PublicAPI) | 7 (adapter, guard, DI, equivalence, 3 integration) | 2 (`IntegrationTests.csproj`, `Benchmarks.csproj`) | 1 | 1 (the benchmark class) | 14 | ~1,000 |
| S7 | Observability | 2 (`SchemaDiagnostics`, `SchemaLog`) | 5 (`EventIdRanges.cs`, core PublicAPI, `SchemaBase.cs`, `SchemaParseOptions.cs`, package PublicAPI) | 3 (diagnostics, log, instrumentation) | 1 (`EncinaEventIdAllocationTests.cs`) | 2 (`Encina.json`, `Encina.Validation.Schema.json`) | 0 | 13 | ~800 |
| S8 | Documentation | 0 | 1 (csproj README link) | 0 | 0 | 0 | 6 (feature page, package README, `docs/INVENTORY.md`, `docs/features/index.md`, changelog fragment, load justification `.md`) | 7 | ~450 |
| **Total** | | **28** | **14** | **31** | **7** | **8** | **9** | **97** | **~6,700** |

The columns sum to the total (28 + 14 + 31 + 7 + 8 + 9 = 97) and the pull-request rows sum to 97 files (14 + 15 + 11 + 13 + 10 + 14 + 13 + 7); the line estimates sum to about 6,700 (700 + 1,100 + 800 + 1,100 + 750 + 1,000 + 800 + 450 = 6,700), which is the sum of the header's production, test and documentation estimates (about 2,450 + 3,800 + 450). S2 is at the 15-file limit and S4 and S6 near it: if a test file grows, split it to its own pull request. S1 first; S2 needs S1; S3 and S4 need S2 (S4 also needs S3 for `SchemaFactory`); S5 needs S4; S6 needs S5 and the merged plans' implementations of #230 (runner and provider changes) and #233 (`ErrorCode`); S7 needs S5; S8 needs all. If #230 or #233 is not implemented when S6 is due, S6 waits and S7 and S8 go first. Verify caller counts with `git grep` before each pull request.

Per-file coverage targets (per flag, with a one-sentence justification), written into `.github/coverage-manifest/Encina.Validation.Schema.json` by the pull request that adds the file (the Contract column is entered by S5, whose contract tests reach those files):

| File | Unit | Guard | Property | Contract | Justification |
|------|------|-------|----------|----------|---------------|
| `StringSchema.cs`, `NumberSchema.cs`, `BooleanSchema.cs`, `DateSchema.cs`, `ArraySchema.cs` | 95 | 90 | 60 | 40 | Every rule and its boundary is reachable with plain values and `FakeTimeProvider`; the guard tests reach every argument check; the property tests drive bounds and the clock; the contract tests reach only the never-throws path of each kind |
| `NullableSchema.cs`, `OptionalSchema.cs`, `DefaultSchema.cs` | 95 | 85 | 0 | 40 | Small decorators with a few branches each; a property adds nothing beyond the unit cases; the contract tests run each wrapper once |
| `ObjectSchema.cs`, `ObjectSchemaBuilder.cs`, `SchemaFactory.cs`, `RefinedSchema.cs`, `TransformedSchema.cs` | 90 | 90 | 60 | 50 | Selector validation, composition and delegate failures are reachable with fixture types; the property test covers composition and parse/validate agreement; the contract tests run real object schemas |
| `DiscriminatedUnionSchema.cs` | 90 | 85 | 60 | 50 | The hostile-input corpus and the union cases reach every branch; the property test covers "never throws" |
| `SchemaBase.cs` | 90 | 85 | 60 | 60 | The parse core and the limits are reachable through the test subclass and every schema; guard tests reach the null checks; the contract tests are written against this contract |
| `SchemaRequestValidator.cs`, `SchemaServiceCollectionExtensions.cs` | 90 | 90 | 0 | 0 | Mapping and registration are exercised by unit, DI and integration tests; the null checks are guard-tested; the contract tests cover `ISchema<T>`, not the adapter |
| `SchemaDiagnostics.cs` | 90 | 0 | 0 | 0 | Internal static type with no argument validation; guard tests cannot reach it (internal, no `InternalsVisibleTo` for `Encina.GuardTests`); a listener test exercises it |
| `SchemaLog.cs` | 90 | 0 | 0 | 0 | Source-generated methods exercised by the logging test; no parameters to guard |
| `SchemaParseOptions.cs` | 90 | 80 | 0 | 0 | Option defaults are exercised by unit tests and the validated limits by guard tests (entered by S2) |
| `SchemaContext.cs`, `SchemaIssue.cs`, `SchemaErrorCodes.cs` | 90 | 0 | 0 | 0 | Internal context and value types are exercised by unit tests; no executable validation beyond what the unit tests reach, so guard tests add nothing |
| `ISchema.cs` | 0 | 0 | 0 | 0 | An interface with no executable code: nothing to cover in any flag (its contract is tested through the real implementations above) |

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
1. New package skeleton, contracts (ISchema<T>, SchemaIssue, SchemaParseOptions, error codes) and the parse core
   SchemaBase<T> / SchemaContext.
2. Immutable schemas: string/number (with the Optional, Nullable, Default wrappers), boolean/date/array, object with Refine,
   Transform, Merge/Extend/Pick/Omit over the same T, and discriminated unions.
3. Parse(JsonElement|string): bounded normalize-then-deserialize, never throws; Validate(T) returns ValidationResult and
   never reads valid on a failure of user code.
4. SchemaRequestValidator<T> adapts a schema to IRequestValidator<T> (issues #230 and #233) so it runs under all three
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
| 2 | OpenTelemetry | ✅ Phase 7 | One `ActivitySource` and `Meter` `Encina.Validation.Schema`, duration and outcome by operation, tenant tag only when `TenantId` is present (ADR-054 D3, D4; source of the tenant value: register row C24); the orchestrator-level view is #726's |
| 3 | Structured Logging | ✅ Phase 7 | `SchemaLog` EventIds 620-622 in the new range 620-629, by schema name, code and counts only; the logger comes from `SchemaParseOptions` (register row C24) |
| 4 | Health Checks | ❌ N/A | In-process, no checkable dependency |
| 5 | Validation | ✅ All phases | This is the validation feature; it joins the three providers through the neutral contract of #230 and an equivalence test pins the result |
| 6 | Resilience | ❌ N/A | No external call; regex time and parse size limits are the bounded-work protection (register rows C2, C3, C14) |
| 7 | Distributed Locks | ❌ N/A | No shared state to coordinate |
| 8 | Transactions | ❌ N/A | Validation runs before the handler |
| 9 | Idempotency | ❌ N/A | Schema validation is deterministic and stateless |
| 10 | Multi-Tenancy | ⏭️ Follow-up | Tenant-specific schemas (the issue defers them): follow-up issue to be opened (draft: `artifacts/issues/validation-tenant-module-scoped-resources.md`); no tenant id is stored or keyed here (register row C9), so there is no tenant-qualified id to make injective |
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
| Generator project for compile-time schemas | #227 | The follow-up of Design Choice 6: follow-up issue to be opened (draft: `artifacts/issues/validation-schema-source-generator.md`) |
| Central validation instrumentation | #726 | Orchestrator-level metrics; this package instruments its own operations |
| Code-less validation `Left` never becomes a 400 | #3415 | Independent: `Parse`'s `Left` carries a code |

---

## Next Steps

1. Link this plan from #235 and ask the maintainer to answer Design Choices 1, 2 and 5 and register rows C1, C2, C3, C5, C7, C8b, C11 and C13 to C24 when work starts.
2. Open the follow-up issues when the first pull request opens: the source-generated form of schemas (follow-up issue to be opened, draft: `artifacts/issues/validation-schema-source-generator.md`) and tenant and module scoped schemas (follow-up issue to be opened, draft: `artifacts/issues/validation-tenant-module-scoped-resources.md`).
3. Implement S1 to S8 as the pull requests of the Estimated File Count split, `Fixes #235` on S8.
