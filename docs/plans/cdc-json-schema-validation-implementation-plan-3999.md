# Implementation Plan: JSON Schema Validation of Untyped CDC Change Event Payloads

> **Issue**: [#3999](https://github.com/dlrivada/Encina/issues/3999)
> **Type**: Feature (the issue carries `needs-decision`; every open alternative is a Dilemma below, recommended and pending the maintainer)
> **Related**: #730 (typed CDC validation: owns `ICdcEventValidator<TEntity>`, the dispatcher stage and the code `encina.cdc.validation_failed`; a hard prerequisite of S2), #2343 (open bug: a `Left` from the dispatcher is retried and dead-lettered), #3611 (open bug: the dispatcher keeps one handler per entity type), #231 (OpenAPI request validation: the same JSON Schema engine question, see Design Choice 2), #235 (a schema builder, a different purpose), #233 (owns `ValidationError.ErrorCode`), #726 (central validation instrumentation), ADR-050, ADR-054, ADR-062 (merged: error category contract)
> **Complexity**: Medium (one new satellite package, one core hook, one third-party dependency)
> **Estimated Scope**: ~1,900 lines in total (~420 production, ~1,150 tests and manifests, ~330 documentation), in four pull requests of at most 13 files and 900 lines each

---

## Summary

### What exists today (read on 2026-11-24, main `278c164f1`)

- `CdcDispatcher.DispatchAsync` looks up the entity type of the table (`_tableToEntityMap`, `StringComparer.OrdinalIgnoreCase`) and its single handler (`_entityToHandlerMap`, a `ToFrozenDictionary` keyed by entity type), converts `ChangeEvent.Before` and `After` (`object?`) to the entity by serializing and deserializing them (`CdcDispatcher.cs`, `DeserializeEntity`: a `JsonElement` is deserialized as is, anything else goes through `JsonSerializer`), and calls `IChangeEventHandler<TEntity>`. The dispatcher is a singleton built from the `CdcConfiguration` (`ServiceCollectionExtensions.cs:80`).
- A table "handled as `JsonElement`" is a table mapped with `WithTableMapping<JsonElement>(table)` and handled by a `IChangeEventHandler<JsonElement>`. `UseOutboxCdc()` already maps the outbox table to `JsonElement` with `OutboxCdcHandler` (`CdcConfiguration.cs:257-258`), and `WithCacheInvalidation()` registers `QueryCacheInvalidationCdcHandler` as `IChangeEventHandler<JsonElement>` (`ServiceCollectionExtensions.cs:193`). Because the handler map has one entry per entity type, a second application handler for `JsonElement` fails when the dispatcher is created; that limit is the open bug #3611 and is not decided here, so a raw-JSON deployment has one `JsonElement` handler that routes by `ChangeContext.TableName`.
- `ChangeEvent(TableName, Operation, Before, After, Metadata)` and `ChangeMetadata(Position, CapturedAtUtc, TransactionId, SourceDatabase, SourceSchema)` carry no tenant and no module.
- #730 (planned) adds `ICdcEventValidator<TEntity>` (unconstrained, so `TEntity = JsonElement` is valid), the context `CdcEventValidationContext<TEntity>(TableName, Operation, Before, After, Metadata, RequestContext)`, a dispatcher stage between deserialization and the handler, and the code `encina.cdc.validation_failed` (`ErrorCategory.Validation`) whose result is dead-lettered at once. Its adapter validates `After` for Insert, Snapshot and Update and not Delete (its register row C7).
- Not in `src`: any JSON Schema library (`Directory.Packages.props` has `FluentValidation` and `MiniValidation` for validation and nothing for schemas), any package `Encina.Cdc.JsonSchema`, any hook that lets a satellite package add services from the fluent `CdcConfiguration` call. `coverage-weights.json` already matches the glob `Encina.Cdc.*`, so a new `Encina.Cdc.*` package needs no entry there.
- #231's plan reports that the candidate engine `JsonSchema.Net` 9.7.0 ships under `OSMFEULA.txt`, not an SPDX licence, and that AOT and trimming support are unverified; that question is shared with this plan.

### What this plan delivers

A per-table JSON Schema check for payloads that have no entity type:

1. **A new package `Encina.Cdc.JsonSchema`** with `JsonSchemaCdcEventValidator : ICdcEventValidator<JsonElement>`. It holds a frozen map from table name (ordinal, ignoring case) to a schema compiled once at startup, validates the `After` image of Insert, Snapshot and Update events of the tables that have a schema, and returns a `ValidationResult` whose errors carry a JSON Pointer and a fixed text per keyword, never a value. The #730 stage turns it into `encina.cdc.validation_failed` and the dead-letter outcome.
2. **Registration in the fluent call of the issue**: `config.AddJsonSchemaValidation("Orders", orderSchemaJson)` (and a file form) through one new hook on `CdcConfiguration`, `ConfigureServices(Action<IServiceCollection>)`, applied by `AddEncinaCdc` (Design Choice 3).
3. **Startup validation**: every schema is parsed and compiled when the host starts; invalid JSON, an unsupported draft, a `$ref` to a remote document or an unresolvable `$ref` fails startup with an `OptionsValidationException`, never the first event.
4. **Limits**: payload size and nesting depth bounded before evaluation, so an oversized change event is rejected, not evaluated (Design Choice 5).

Nothing changes for an application that does not call `AddJsonSchemaValidation`: a table without a schema is not touched.

- Affected packages: `Encina.Cdc` (one public method, `ConfigureServices`), `Encina.Cdc.JsonSchema` (new). The five connectors (SqlServer, PostgreSql, MySql, MongoDb, Debezium) are untouched: the check sits in the dispatcher, after the connector.
- Provider category: none of the database, cache or transport categories. The three validation providers are not involved (a schema validator is a CDC validator, not an `IValidationProvider`); the CDC connectors are one equivalence class because validation sits after them (`AGENTS.md` section 5: no provider-specific code).
- Shared pieces: consumes `ICdcEventValidator<TEntity>`, `CdcEventValidationContext<TEntity>` and `encina.cdc.validation_failed` (owned by #730) and optionally `ValidationError.ErrorCode` (owned by #233); shares the engine decision with #231.

---

## Design Choices

<details>
<summary><strong>1. Where schemas come from (the issue's Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Schema documents registered per table in configuration (JSON text or a file)** | Explicit and versionable with the application; needs no entity type, which is exactly what untyped payloads lack; the schema is the contract with the upstream database | The schema can drift from the database it describes; two places to change when a column is added |
| **B) Schemas derived from registered entity types or validators (a builder such as #235)** | One source of truth when a typed entity exists | Untyped payloads have no entity type, so B cannot serve the stated case; a derived schema only checks what the entity already deserializes, which misses a dropped column |

### Chosen Option: **A - schema documents registered per table** (recommended, pending the maintainer)

### Rationale

- The feature exists for tables with no entity type and for connectors that forward raw JSON (Debezium); only A serves them. B wins for a table that has a typed entity and where a schema form is wanted only as documentation; #235 can emit a document that A then registers.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Package placement and the JSON Schema engine (the issue's Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A new package `Encina.Cdc.JsonSchema` that carries the engine dependency; `Encina.Cdc` stays free of it; the engine is the one #231 picks, behind a small internal adapter in each package** | Pay-for-what-you-use: no CDC user carries the library; a licence or AOT problem touches only this package; the adapter keeps the engine swappable | A new package to ship, pack and document (the touch points of register row C13) |
| **B) The dependency and the validator inside `Encina.Cdc`** | One package, no new csproj | Every CDC deployment carries the library and its licence terms, and most deployments will not use schemas |
| **C) A shared package `Encina.Validation.JsonSchema` holding the engine adapter and limits, used by this package and by #231's `Encina.Validation.OpenApi`** | One adapter, one set of limits | A third package for two consumers; a shared package decides the engine for both plans |

### Chosen Option: **A - new package, the engine of #231, an adapter in each** (recommended, pending the maintainer)

### Rationale

- A keeps the dependency out of every CDC user. The engine is not chosen here: the first of #231 and this plan to start implementation runs the spike of #231's Design Choice 2 (licence reading of the candidate's `OSMFEULA.txt`, AOT and trimming, keyword coverage, evaluation of a `JsonElement` without copying) and records the result for both; if the maintainer rejects that licence, the minimal-evaluator option of #231 applies to both. C wins if a third consumer of JSON Schema appears and the two adapters would diverge; B wins only if the maintainer expects schemas in most CDC deployments.
- Whatever the engine, remote `$ref` resolution is disabled (register row C8).

**Class:** Dilemma

</details>

<details>
<summary><strong>3. How the fluent call reaches the container</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One new public method on `CdcConfiguration`, `ConfigureServices(Action<IServiceCollection>)`, applied at the end of `AddEncinaCdc`; the satellite's `AddJsonSchemaValidation` extension uses it** | Keeps the issue's single fluent call; the same hook serves future satellites (a pay-for-what-you-use extension point) | A public method on a core class; one more thing to document |
| **B) Two registrations: `services.AddEncinaCdcJsonSchema(options => options.Add("Orders", schema))` and `config.AddEventValidator<JsonElement, JsonSchemaCdcEventValidator>()` of #730** | No change to `Encina.Cdc` | Two calls that must agree; forgetting the second silently disables validation (a fail-open gate); the issue's sample shape is lost |

### Chosen Option: **A - the `ConfigureServices` hook** (recommended, pending the maintainer)

### Rationale

- Forgetting a registration must not turn a gate off; with A one call registers the schema and the validator together. B wins if the maintainer wants no new public surface on `CdcConfiguration` and accepts the second registration being validated at startup (a start-up check that fails when schemas exist and the validator is not registered).

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Which events and which payload shapes are validated</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `JsonElement` entities only: the `After` image of Insert, Snapshot and Update; Delete is not validated; a table without a schema is not validated** | Matches the issue (payloads with no entity type) and the rule of #730 (register row C7); needs no change to #730's contract; a delete image often holds only key columns, so a full schema would reject every delete | A typed entity with a schema is not served: its payload is already deserialized, so a dropped column became a default value before the validator sees it |
| **B) A raw-image stage for every table, typed or not: the validator receives the connector's `ChangeEvent` before deserialization** | Detects upstream schema drift on typed tables too, which the issue names as the failure to catch | Changes #730's context and dispatcher stage (the raw event added to `CdcEventValidationContext`); a second schema per operation for deletes |

### Chosen Option: **A - `JsonElement` entities, After image of Insert, Snapshot and Update** (recommended, pending the maintainer)

### Rationale

- Under A a schema registered for a table that is not mapped to `JsonElement` would never run; startup fails for it (register row C14), so A never leaves a schema silently inactive.
- A delivers the issue's stated scope without reopening #730. B wins when schema-drift detection on typed tables is wanted; it is a follow-on that extends #730's context with the raw image and reuses this package's validator (listed in Next Steps).
- The validated document is the JSON the dispatcher builds with its `JsonSerializerOptions`: dates are strings, binary is base64, so a schema describes that JSON, not the database column types; the documentation page lists the mapping.

**Class:** Dilemma

</details>

### Register of defaults and semantics

| Id | Choice | Class | Settled by |
|----|--------|-------|------------|
| C5 | The schema validator is one more `ICdcEventValidator<JsonElement>`: the dispatcher stage, the code `encina.cdc.validation_failed` (`ErrorCategory.Validation`, ADR-062 R3), the non-retryable dead-letter outcome and the never-log-the-payload rule are inherited from #730, not redefined | Settled | The plan of #730 (owner of the stage and the code); ADR-062 R3; AGENTS.md section 3 (errors are never swallowed in background infrastructure) |
| C6 | The validator returns `ValidationError(PropertyName = JSON Pointer, ErrorMessage = fixed text of the keyword)`; the instance value, the schema's `const` and `enum` members and the library's own message never enter the result, a log or a dead-letter reason; `ErrorCode` is filled with the keyword name once #233 exists. Pointers and logged names are capped at 64 characters and 20 entries | Settled | AGENTS.md section 3 (`EncinaError.Message` and payload values never reach logs or plaintext storage); the requirement of #3999 ("schema paths and counts, never payload values") |
| C7 | A table that has no schema is not validated (schemas are opt-in per table); the table-name comparison is ordinal, ignoring case, as the dispatcher's own map (`CdcDispatcher` constructor) | Trivial | `src/Encina.Cdc/Processing/CdcDispatcher.cs` (`StringComparer.OrdinalIgnoreCase`) |
| C8 | A schema is parsed and compiled at startup (`ValidateOnStart`): invalid JSON, an unsupported `$schema` draft, a `$ref` that is not a local fragment (`#/...`) and an unresolvable `$ref` fail startup; no `$ref` is ever fetched over the network (server-side request forgery) | Settled | The requirement of #3999 ("an invalid schema fails at startup"); OWASP ASVS 4.0 V12.6.1 (the server only requests resources from an allow-list) |
| C9 | Numeric defaults: `MaxPayloadBytes` 1,048,576 and `MaxDepth` 64, both validated above zero; an event over a limit is rejected with the same `encina.cdc.validation_failed` and the second of this plan's two EventIds (Warning, table and limit kind; register row C11); the payload size is read from the serialized `JsonElement` raw text, not by re-serializing | Dilemma (recommended, pending the maintainer) | Numeric defaults, the same kind as the limits of #231's rows C3 and C5; wins differently for a wide-row table (raise the size) or a hostile upstream (lower both) |
| C10 | Evaluation is synchronous, allocation-light and holds no state per event; the compiled schemas are immutable, so the validator is safe in the singleton dispatcher's scoped validator resolution (#730 registers validators scoped) | Settled | AGENTS.md section 3 (never a scoped dependency in a singleton; `ValidateScopes`) |
| C11 | EventIds: the next two ids of #730's `ValidationCdc` range, after #730's last id (the first: schemas compiled at startup, Information, table count; the second: payload over a limit, Warning, table and limit kind). No numeric literal is fixed here: #730's plan registers `ValidationCdc` below 10000, which AGENTS.md section 7 and `docs/engineering/eventid-block-registry.md` no longer allow for a new range, so #730 must be re-slotted into the 10000+ zone before its first pull request; this plan takes the two ids that follow #730's last one in whatever slot it ends with, and if that slot is still unfixed when S2 opens the ids are to be assigned by the orchestrator. The failure of an event is already logged by #730's error id (table, operation, position, error count, pointers), so this plan adds no second failure log | Trivial | AGENTS.md section 7 (ADR-021); the plan of #730 (its range) |
| C14 | A schema registered for a table that is not mapped to `JsonElement` (not mapped at all, or mapped to a typed entity) fails startup: the options validator checks every schema table against `CdcConfiguration.IsTableMappedTo(table, typeof(JsonElement))` (S0; ordinal, ignoring case) and reports the code `encina.cdc.schema_table_not_json` (`ErrorCategory.Internal`, ADR-062 R1: a configuration entry; it surfaces as an `OptionsValidationException`, never reaches a client). Otherwise the schema would silently never run, a gate failing open, because the validator is only invoked for `JsonElement` entities. The check runs at host start, after the whole fluent call, so the order of `WithTableMapping` and `AddJsonSchemaValidation` does not matter | Settled | AGENTS.md section 3 (gates fail closed; registration completeness); `src/Encina.Cdc/CdcConfiguration.cs:95-98` (mapping per table) |
| C15 | `AddJsonSchemaValidation` is idempotent per configuration: the options, registry, options validator and the `AddEventValidator<JsonElement, JsonSchemaCdcEventValidator>()` registration of #730 happen once however many tables are added (the first call registers them, later calls only add a schema to the options); otherwise the validator would run once per table registered and report duplicate errors at N times the cost, since #730 merges several validators per entity | Settled | The plan of #730 (several validators per entity run in sequence); AGENTS.md section 3 (registration completeness) |
| C12 | Multi-tenancy and modules do not apply: `ChangeEvent` and `ChangeMetadata` carry neither, so there is nothing to select by and no tenant tag to emit | Settled | `src/Encina.Cdc/ChangeEvent.cs` and `ChangeMetadata.cs` (no tenant member); ADR-054 D4 (no tag when there is no tenant) |
| C13 | A new satellite package costs a fixed set of touch points, counted in the split: the csproj (with `InternalsVisibleTo` for `Encina.UnitTests`, `Encina.GuardTests`, `Encina.IntegrationTests`, `Encina.PropertyTests`, `Encina.ContractTests`), an `Encina.slnx` entry, two PublicAPI files, a README, a `Directory.Packages.props` entry, a coverage manifest and one `ProjectReference` per test project that uses it | Trivial | The new-package precedent of `src/Encina.Cdc.Debezium/Encina.Cdc.Debezium.csproj` |

---

## Implementation Phases

### Phase 1: The `ConfigureServices` Hook in `Encina.Cdc` (pull request S0)

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina.Cdc/CdcConfiguration.cs`: `public CdcConfiguration ConfigureServices(Action<IServiceCollection> configure)` (null checked) storing the actions in an `internal IReadOnlyList<Action<IServiceCollection>> ServiceConfigurations`; and `public bool IsTableMappedTo(string tableName, Type entityType)` (null and blank checked; ordinal, ignoring case, over the internal `TableMappings`, `CdcConfiguration.cs:44`), which the satellite needs for register row C14 because `TableMappings` is internal.
2. **Modify** `src/Encina.Cdc/ServiceCollectionExtensions.cs`: at the end of `AddEncinaCdc`, apply each stored action to `services` in registration order.
3. **Modify** `src/Encina.Cdc/PublicAPI.Unshipped.txt` (`mechanical-fixer`).
4. **Tests**: edit `tests/Encina.UnitTests/Cdc/CdcConfigurationTests.cs` or add `CdcConfigurationServiceHookTests.cs` (actions run in order, once, after the core registrations; a registration made by the hook is resolvable) and edit `tests/Encina.GuardTests/Cdc/` (a null action throws `ArgumentNullException`; a null or blank table or a null entity type throws in `IsTableMappedTo`); the unit tests also cover `IsTableMappedTo` (a mapped table, an unmapped one, a table mapped to another type, a case difference).
5. **Manifest** `.github/coverage-manifest/Encina.Cdc.json` through `mechanical-fixer`. Both entries (`CdcConfiguration.cs`, `ServiceCollectionExtensions.cs`) have no numeric `targets` today (only `defaultTests`), so this pull request adds them: `CdcConfiguration.cs` unit 90 guard 60 (a fluent builder, most methods are one-line registrations reached by unit tests; guard tests reach the argument checks of its public methods, which are a minority of its lines), `ServiceCollectionExtensions.cs` unit 90 guard 50 (registration code reached by DI tests; guard tests reach only the null checks). Each justification is one sentence in the file; the PR reports the measured values against them and the numbers are set to the measured value when it is lower than the estimate above, never to 0.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Encina issue #3999, phase 1. Encina.Cdc has CdcConfiguration (a fluent builder of table mappings and handler registrations) and AddEncinaCdc(services, configure). A satellite package needs to add services from the fluent call.
TASK: Add CdcConfiguration.ConfigureServices(Action<IServiceCollection>) and CdcConfiguration.IsTableMappedTo(string, Type), and apply the stored actions at the end of AddEncinaCdc; write unit and guard tests; update PublicAPI.Unshipped.txt and the manifest through mechanical-fixer.
KEY RULES: Null-check the argument. Actions run once, in order, after the core registrations, so a satellite can TryAdd over defaults. No change to any existing behavior when no action is registered. Tests drive the real AddEncinaCdc, never reflection-only checks.
REFERENCE FILES: src/Encina.Cdc/CdcConfiguration.cs, src/Encina.Cdc/ServiceCollectionExtensions.cs, tests/Encina.UnitTests/Cdc/, tests/Encina.GuardTests/Cdc/CdcDispatcherGuardTests.cs.
```

</details>

### Phase 2: Package Skeleton, Options and Engine Adapter (pull request S1)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** the package: `src/Encina.Cdc.JsonSchema/Encina.Cdc.JsonSchema.csproj` (project references `Encina.Cdc`; the engine `PackageReference` chosen by the spike of Design Choice 2; the PublicAPI analyzer; `InternalsVisibleTo` of register row C13), `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`, `README.md`; **modify** `Encina.slnx` (one entry), `Directory.Packages.props` (the engine version), `tests/Encina.UnitTests/Encina.UnitTests.csproj` and `tests/Encina.GuardTests/Encina.GuardTests.csproj` (one `ProjectReference` each); **create** `.github/coverage-manifest/Encina.Cdc.JsonSchema.json` (`mechanical-fixer`).
2. **Create** `src/Encina.Cdc.JsonSchema/CdcJsonSchemaOptions.cs`: `public sealed class CdcJsonSchemaOptions { MaxPayloadBytes (1_048_576), MaxDepth (64), internal table-to-schema-text entries; public CdcJsonSchemaOptions AddSchema(string tableName, string schemaJson); AddSchemaFile(string tableName, string path) }` (blank table or text throws `ArgumentException`; a second schema for one table, compared ordinal ignoring case, is refused).
3. **Create** `src/Encina.Cdc.JsonSchema/Engine/JsonSchemaEngine.cs` (`internal`): `Either<EncinaError, CompiledSchema> Compile(string schemaJson)` and `IReadOnlyList<SchemaViolation> Evaluate(CompiledSchema schema, JsonElement instance)`; no remote `$ref` (register row C8); violations carry the JSON Pointer and the keyword only (register row C6).
4. **Tests**: new `tests/Encina.UnitTests/Cdc/JsonSchema/JsonSchemaEngineTests.cs` (valid and invalid documents, missing and extra properties, type mismatch, `required`, `enum`, a `$ref` to a remote URL refused, an unsupported draft refused, no instance value in any violation) and `tests/Encina.GuardTests/Cdc/JsonSchema/CdcJsonSchemaOptionsGuardTests.cs` (null, blank and duplicate tables, a missing file, limits at or below zero).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: Encina issue #3999, phase 2 (after phase 1). A new satellite package Encina.Cdc.JsonSchema validates untyped CDC payloads against JSON Schema. The engine is the one the shared spike with #231 picks (licence reading, AOT and trimming, keyword coverage); keep it behind the internal JsonSchemaEngine adapter.
TASK: Create the package skeleton with every touch point of the plan, CdcJsonSchemaOptions and the internal engine adapter, with unit and guard tests.
KEY RULES: Never fetch a $ref over the network; accept only fragments of the same document. A violation carries a JSON Pointer and the keyword, never the instance value, a const or enum member, or the library's message. Table names compare ordinal ignoring case. Update PublicAPI.Shipped.txt, PublicAPI.Unshipped.txt, Encina.slnx, Directory.Packages.props, the manifest and two test csproj ProjectReferences; the PublicAPI, manifest and changelog lines go through mechanical-fixer.
REFERENCE FILES: src/Encina.Cdc.Debezium/Encina.Cdc.Debezium.csproj, src/Encina.Cdc/CdcConfiguration.cs, docs/plans for #231 (by issue number, the engine spike), Directory.Packages.props.
```

</details>

### Phase 3: Validator, Registry, Registration and Log (pull request S2, after #730)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Cdc.JsonSchema/CdcJsonSchemaRegistry.cs` (`internal sealed`): built from the options at startup, a `FrozenDictionary<string, CompiledSchema>` with `StringComparer.OrdinalIgnoreCase`; `TryGet(string tableName, out CompiledSchema schema)`.
2. **Create** `src/Encina.Cdc.JsonSchema/JsonSchemaCdcEventValidator.cs`: `internal sealed class JsonSchemaCdcEventValidator : ICdcEventValidator<JsonElement>` (internal, like the registry its constructor takes: a public class with a constructor parameter of an internal type is CS0051; it is registered by type through `AddEventValidator<JsonElement, JsonSchemaCdcEventValidator>()` of #730, whose constraint is `where TValidator : class`, and the extension that calls it is in the same assembly; the package's only public API in S2 is therefore `CdcJsonSchemaConfigurationExtensions`, and the validator has no PublicAPI line), constructor `(CdcJsonSchemaRegistry registry, IOptions<CdcJsonSchemaOptions> options, ILogger<JsonSchemaCdcEventValidator> logger)`; `ValidateAsync(CdcEventValidationContext<JsonElement> context, CancellationToken)`: Delete or no schema for the table returns `ValidationResult.Success`; a missing `After` for Insert, Snapshot or Update is a failure (`ValidationError("/", fixed text)`); the payload is checked against the limits of register row C9 before evaluation; the violations become `ValidationError`s (register row C6).
3. **Create** `src/Encina.Cdc.JsonSchema/CdcJsonSchemaOptionsValidator.cs` (`IValidateOptions<CdcJsonSchemaOptions>`, constructed with a `Func<string, bool>` over `CdcConfiguration.IsTableMappedTo(table, typeof(JsonElement))` of S0: it compiles every schema and fails when a schema's table is not mapped to `JsonElement` with `encina.cdc.schema_table_not_json` (register row C14), so a bad schema or an inactive one fails `ValidateOnStart`) and `CdcJsonSchemaConfigurationExtensions.cs`: `AddJsonSchemaValidation(this CdcConfiguration configuration, string tableName, string schemaJson)` and `AddJsonSchemaValidationFromFile(...)`, each calling `configuration.ConfigureServices` once to register the options, the registry (`TryAddSingleton`), the validator as `ICdcEventValidator<JsonElement>` through #730's registration, `ValidateOnStart`; and `AddEventValidator<JsonElement, JsonSchemaCdcEventValidator>()` of #730 so the stage exists. The registration is idempotent per configuration (register row C15): a `ConditionalWeakTable<CdcConfiguration, ...>` marker in the satellite records that the first call registered the options validator, registry and validator, and later calls only add a schema to the options.
4. **Create** `src/Encina.Cdc.JsonSchema/Diagnostics/CdcJsonSchemaLog.cs`: `internal static partial class`, `[LoggerMessage]` with the next two ids of #730's `ValidationCdc` range (register row C11: Information, schemas compiled, count; Warning, over a limit, table and limit kind), inside that range; **modify** `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (assembly `Encina.Cdc.JsonSchema` maps to `ValidationCdc`) and `PublicAPI.Unshipped.txt`; the manifest through `mechanical-fixer`.
5. **Tests**: new unit tests of the validator through the real dispatcher stage of #730 (a valid and an invalid payload per operation, Delete skipped, no schema skipped, an over-limit payload, a table-name case difference), of the registry and the options validator (an invalid schema fails the host start; a schema for a table not mapped, or mapped to a typed entity, fails the host start with `encina.cdc.schema_table_not_json`, whatever the order of `WithTableMapping` and `AddJsonSchemaValidation`), a registration test that builds the provider with `ValidateOnBuild` and `ValidateScopes` and proves `AddJsonSchemaValidation` registers the validator and the stage together and, called for three tables, registers one validator (the dispatcher stage runs it once per event, no duplicate errors), and a guard test for the extension and the constructor.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: Encina issue #3999, phase 3. #730's ICdcEventValidator<TEntity>, CdcEventValidationContext<TEntity>, AddEventValidator and the dispatcher stage exist; phases 1 and 2 are merged.
TASK: Implement the registry, JsonSchemaCdcEventValidator, the options validator, the AddJsonSchemaValidation extensions through CdcConfiguration.ConfigureServices, the two log messages (ids per register row C11) and their tests.
KEY RULES: A schema failure is a ValidationResult, never an exception; the #730 stage owns the Left and the dead letter. No instance value in any result, log or exception text. Compile schemas at startup (ValidateOnStart). One call must register the schema and the validator together, so forgetting the second registration can never disable the gate. The two EventIds are the next two ids of #730's ValidationCdc range after its last one (never hard-code 662 or 663; if #730's slot is unfixed, ask the orchestrator); map the assembly in EncinaEventIdAllocationTests. The validator and registry are internal (CS0051). The options validator fails startup for a schema whose table is not mapped to JsonElement (encina.cdc.schema_table_not_json, Internal). AddJsonSchemaValidation is idempotent per configuration. Tests use Shouldly, the real dispatcher and real engine.
REFERENCE FILES: docs/plans for #730 (by issue number), src/Encina.Cdc/Processing/CdcDispatcher.cs, src/Encina.Cdc.JsonSchema/ (phase 2), tests/Encina.UnitTests/Cdc/CdcDispatcherTests.cs.
```

</details>

### Phase 4: Integration, Property, Benchmark and Documentation (pull request S3)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `tests/Encina.IntegrationTests/Cdc/JsonSchema/CdcJsonSchemaIntegrationTests.cs` (no container: the real `CdcProcessor`, dispatcher and in-memory dead-letter store; an event that violates the schema is dead-lettered once with `encina.cdc.validation_failed` and never reaches the `JsonElement` handler; a valid event reaches it; a schema that fails to compile stops the host); **modify** `tests/Encina.IntegrationTests/Encina.IntegrationTests.csproj`, `tests/Encina.PropertyTests/Encina.PropertyTests.csproj` and `tests/Encina.BenchmarkTests/Encina.BenchmarkTests.csproj` (one `ProjectReference` each).
2. **Create** `tests/Encina.PropertyTests/Cdc/JsonSchema/CdcJsonSchemaPropertyTests.cs` (FsCheck: any document generated from a schema validates, and stays valid after a serialize and parse round trip) and `tests/Encina.BenchmarkTests/Encina.Cdc.JsonSchema.Benchmarks/` (evaluation of a typical 20-column row; `BenchmarkSwitcher`), or a justification file `tests/Encina.BenchmarkTests/Cdc/JsonSchema/CdcJsonSchema.md` if the measured cost is negligible.
3. **Create** `docs/features/cdc-json-schema-validation.md` through `docs-writer` (registration, the JSON mapping of database types, the images validated, limits, startup failures, what a rejected event looks like); **modify** `src/Encina.Cdc.JsonSchema/README.md` and `docs/INVENTORY.md` (`docs-writer`).
4. **Create** `changelog.d/3999-cdc-json-schema-validation.added.md` through `mechanical-fixer`; Load tests: a justification file (`.md`: no shared mutable state, a pure function of the event).
5. Build verification: `dotnet build --configuration Release` with 0 warnings; every flag reaches the targets of "Estimated File Count".

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT: Encina issue #3999, phase 4. The package, the validator and the registration exist.
TASK: Write the integration, property and benchmark tests (or the justification), then hand the documentation page, the README, the INVENTORY row and the changelog fragment to docs-writer and mechanical-fixer; run build, tests and coverage commands.
KEY RULES: Integration tests need no container; they drive the real CdcProcessor, CdcDispatcher and dead-letter store. Never swallow an exception thrown by the code under test. BenchmarkSwitcher, never BenchmarkRunner.Run. Documentation never types coverage figures by hand. Never edit CHANGELOG.md.
REFERENCE FILES: tests/Encina.IntegrationTests/Cdc/CdcDispatcherIntegrationTests.cs, docs/features/cdc.md, changelog.d/README.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Requirement | How the plan meets it |
|----------|-------------|-----------------------|
| JSON Schema (draft 2020-12 and the draft the engine supports) | The schema language | The engine evaluates the registered documents; an unsupported draft fails startup |
| RFC 6901 | JSON Pointer | A violation's location is a pointer |
| OWASP ASVS 4.0 V12.6.1 | The server requests only allow-listed resources | No `$ref` is fetched remotely |
| `AGENTS.md` section 3 | No payload value in logs or storage; errors are never swallowed | Register rows C5 and C6 |
| ADR-050 | CDC requires a reachable dead letter queue | The rejected event goes to it through #730 and #2343 |
| ADR-062 R3 | Field rules are `Validation` | `encina.cdc.validation_failed` keeps `ErrorCategory.Validation` |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `CdcDispatcher`, `CdcConfiguration` | `src/Encina.Cdc/Processing/CdcDispatcher.cs`, `src/Encina.Cdc/CdcConfiguration.cs` | The stage and the fluent registration |
| `ICdcEventValidator<TEntity>`, `CdcEventValidationContext<TEntity>` | #730 (planned) | The contract the validator implements |
| `ValidationResult`, `ValidationError` | `src/Encina/Validation/ValidationResult.cs` | The result of a failed check |
| `CdcDeadLetterEntry`, `InMemoryCdcDeadLetterStore`, `CdcDeadLetterHealthCheck` | `src/Encina.Cdc/` | The dead-letter outcome and its health check |
| `Encina.Cdc.Debezium.csproj` | `src/Encina.Cdc.Debezium/` | The package precedent |
| The engine spike | #231 (planned) | The engine decision shared with this plan |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Cdc.JsonSchema` | the next two ids of #730's `ValidationCdc` range | #730's plan registers `ValidationCdc` below 10000, which AGENTS.md section 7 and `docs/engineering/eventid-block-registry.md` no longer allow for a new range, so #730 must be re-slotted into the 10000+ zone before its first pull request. This plan claims no literal id: it takes the two ids that follow #730's last one in whatever slot #730 ends with (register row C11); if that slot is still unfixed when S2 opens, the ids are to be assigned by the orchestrator |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| S0 (core hook) | 7 (`CdcConfiguration.cs`, `ServiceCollectionExtensions.cs`, `PublicAPI.Unshipped.txt`, `Encina.Cdc.json`, 2 unit tests, 1 guard test) | Targets (new numeric entries, Phase 1 task 5): `CdcConfiguration.cs` unit 90 guard 60; `ServiceCollectionExtensions.cs` unit 90 guard 50 |
| S1 (skeleton, options, adapter) | 13 (csproj, 2 PublicAPI, README, `Encina.slnx`, `Directory.Packages.props`, manifest, 2 test csproj, options, engine adapter, 2 tests) | Targets: `CdcJsonSchemaOptions.cs` unit 95 guard 90 (every setter and duplicate branch); `Engine/JsonSchemaEngine.cs` unit 95 guard 0 (internal, no parameter validated beyond the options, evaluation driven by real documents) |
| S2 (validator, registry, registration, log) | 12 (4 production, log, `PublicAPI.Unshipped.txt`, `EncinaEventIdAllocationTests.cs`, manifest, 3 unit tests, 1 guard test) | Targets: `JsonSchemaCdcEventValidator.cs` (internal, reached by guard tests through `InternalsVisibleTo`; no PublicAPI line) unit 95 guard 85 integration 80; `CdcJsonSchemaRegistry.cs` unit 100 guard 0 (internal, a pure lookup with no validated parameter); `CdcJsonSchemaOptionsValidator.cs` unit 95 guard 0 (internal; its inputs are reached by the unit tests of the startup failures); `CdcJsonSchemaConfigurationExtensions.cs` unit 95 guard 90; `CdcJsonSchemaLog.cs` unit 90 guard 0 (generated bodies reached through the validator tests) |
| S3 (integration, property, benchmark, docs) | 11 (integration test, property test, benchmark project or justification, load justification, 3 test csproj, feature page, README, `INVENTORY.md`, changelog fragment) | `JsonSchemaCdcEventValidator.cs` integration 80, property 80 |
| Total | 43 files over four pull requests (7, 13, 12, 11) | At most 15 files and 900 lines each |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT: Encina (.NET 10, C# 14, pre-1.0). Issue #3999: JSON Schema validation of untyped CDC change event payloads, reusing the validation stage, the contract ICdcEventValidator<TEntity> and the code encina.cdc.validation_failed of #730. CdcDispatcher keeps one handler per entity type (open bug #3611) and UseOutboxCdc already uses the JsonElement slot.
IMPLEMENTATION OVERVIEW: (1) CdcConfiguration.ConfigureServices hook in Encina.Cdc. (2) New package Encina.Cdc.JsonSchema: options, an internal engine adapter (engine chosen by the spike shared with #231), JsonSchemaCdcEventValidator : ICdcEventValidator<JsonElement>, a frozen table-to-schema registry, an options validator that compiles every schema at startup, AddJsonSchemaValidation extensions, two log ids that follow #730's last one in its ValidationCdc range (no literal ids), and a startup check that every schema table is mapped to JsonElement. (3) Integration (no container), property and benchmark tests, documentation, changelog.
KEY PATTERNS: Opt-in per table. Validate the After image of Insert, Snapshot and Update; skip Delete and tables with no schema. A violation is a JSON Pointer plus a fixed keyword text; no instance value, const, enum member or library message ever reaches a result, log or dead-letter reason. Never fetch a $ref remotely. Startup fails on an invalid schema. One registration call registers the schema and the validator together. Design Choices 1 to 4 are Dilemmas recommended and pending the maintainer: configured schema documents, a new package with the engine of #231, the ConfigureServices hook, JsonElement entities only.
REFERENCE FILES: src/Encina.Cdc/Processing/CdcDispatcher.cs, src/Encina.Cdc/CdcConfiguration.cs, src/Encina.Cdc/ServiceCollectionExtensions.cs, src/Encina.Cdc.Debezium/Encina.Cdc.Debezium.csproj, docs/plans for #730 and #231 (by issue number), tests/Encina.IntegrationTests/Cdc/CdcDispatcherIntegrationTests.cs.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ | Compiled schemas are built once at startup and held in a frozen table-to-schema map; no `ICacheProvider`. |
| 2 | OpenTelemetry | ⏭️ | #726 owns central validation instrumentation; a schema failure is counted there with the rule kind `schema`. |
| 3 | Structured Logging | ✅ | Two `[LoggerMessage]` ids (schemas compiled, payload over a limit) that follow #730's last id in its CDC validation range; the failure of an event is logged by #730's error id; pointers and counts only, never values. |
| 4 | Health Checks | ❌ | Dead letters are already reported by `CdcDeadLetterHealthCheck`; the schema map is static configuration. |
| 5 | Validation | ✅ | This is a validation feature: a JSON Schema validator behind `ICdcEventValidator<JsonElement>` of #730. |
| 6 | Resilience | ❌ | No external call; evaluation is deterministic and bounded by the limits of register row C9. |
| 7 | Distributed Locks | ❌ | No shared mutable state; the compiled schemas are immutable. |
| 8 | Transactions | ❌ | Validation runs in the dispatcher before the handler; the dead-letter path is owned by #2343 and #730. |
| 9 | Idempotency | ❌ | Repeatable and side-effect free. |
| 10 | Multi-Tenancy | ❌ | `ChangeEvent` and `ChangeMetadata` carry no tenant, so there is nothing to select by. |
| 11 | Module Isolation | ❌ | CDC events are table-scoped and carry no module. |
| 12 | Audit Trail | ❌ | The dead-letter entry is the record of a rejected event; no operation audit applies. |

---

## Prerequisites & Dependencies

| Item | Kind | Why |
|------|------|-----|
| None for S0 | - | The hook stands alone |
| #231 engine spike (licence reading of `JsonSchema.Net` / `OSMFEULA.txt`, AOT and trimming) | Hard for S1 | S1 adds the engine `PackageReference` and adapter, so it cannot start before the spike has recorded the engine |
| #730 re-slotted into the 10000+ EventId zone | Hard for S2 | The two ids of this plan follow #730's last one in its range (register row C11) |
| #730 (validator contract, stage, `encina.cdc.validation_failed`) | Hard for S2 | The validator implements its contract and relies on its stage |
| #2343 (a dispatcher `Left` is retried and dead-lettered) | Hard for S3's integration test and for #730 | Without it a `Left` is not dead-lettered |
| #3611 (one handler per entity type) | Soft | Until it is decided, a deployment has one `JsonElement` handler, and `UseOutboxCdc()` takes that slot |
| #233 (`ValidationError.ErrorCode`) | Soft | The keyword name becomes the error code once it exists |
| #726 | Soft | Central validation instrumentation |

---

## Next Steps

1. Open S0 and S1 once the engine spike (shared with #231) has recorded the engine, then S2 after #730's stage merged, then S3.
2. A raw-image stage for typed entities (Design Choice 4, option B) is a follow-on that adds the raw event to #730's context and reuses this package's validator; it starts only when schema-drift detection on typed tables is requested.
3. A second `JsonElement` handler (outbox CDC plus an application raw handler) is the open bug #3611; this plan does not change the handler map.
