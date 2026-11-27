# Implementation Plan: Amazon EventBridge schema registry validation and archive replay

> **Issue**: [#4545](https://github.com/dlrivada/Encina/issues/4545)
> **Type**: Feature
> **Verified against**: origin/main `f9063538e` (2026-11-27)
> **Complexity**: Medium (5 phases, 5 pull requests of about 12, 13, 5, 5 and 4 files)
> **Estimated Scope**: about 700 lines of production code and about 1,500 lines of tests
> **Family**: transport-new-1 follow-ups (#4543, #4544, #4545). Builds on #238 (the EventBridge transport).

---

## Summary

#238 publishes events to an event bus in CloudEvents format and, when `AutoCreateResources` is on, creates an archive (`EnableArchive`); its Design Choice 5 recommends leaving replay as an operator action and the schema registry as a follow-up. This plan adds both: a replay wrapper over `StartReplay`, `DescribeReplay` and `CancelReplay` that returns `Either<EncinaError, ...>`, and an opt-in validation of the event against a schema of the EventBridge schema registry before publishing. State of `origin/main` (`f9063538e`):

- `Encina.AmazonEventBridge` is not on `main` (only its plan); this plan edits what #238 creates: `EncinaAmazonEventBridgeOptions`, its validator, `AmazonEventBridgeMessagePublisher`, `AmazonEventBridgeErrorCodes.cs`, `Log.cs` and `ServiceCollectionExtensions.cs` (`IAmazonEventBridge` is a `TryAddSingleton` there). The package carries no `PublicAPI.*.txt`, like `Encina.AmazonSQS`, `Encina.NATS` and `Encina.Kafka` (searched).
- AWS SDK facts, verified on 2026-11-27 by downloading the packages: **AWSSDK.EventBridge 4.1.0.2** has `StartReplay` (request `ReplayName`, `EventSourceArn`, `Destination`, `EventStartTime`, `EventEndTime`, `Description`), `DescribeReplay` (state, `StateReason`, `EventLastReplayedTime`), `CancelReplay`, the states `STARTING`, `RUNNING`, `CANCELLING`, `COMPLETED`, `CANCELLED` and `FAILED`, and the exceptions `ResourceNotFoundException`, `ResourceAlreadyExistsException`, `LimitExceededException`, `ThrottlingException`; **AWSSDK.Schemas 4.1.0.2** (Apache-2.0, net10.0) has `DescribeSchema` (request `RegistryName`, `SchemaName`, `SchemaVersion`; response `Content` and `Type`), `GetDiscoveredSchema` and `SearchSchemas`. A replay re-sends the archived events of a time range to the bus, so every matching rule delivers again: consumers deduplicate on the message id (the inbox).
- The repository has no JSON Schema validator: `Directory.Packages.props` pins `Newtonsoft.Json` 13.0.4 and no schema library. **NJsonSchema 11.6.2** (MIT; net8.0 and netstandard2.0; depends on Newtonsoft.Json) has `JsonSchema.FromJsonAsync` and `JsonSchema.Validate(string, SchemaType, settings)` with `SchemaType.JsonSchema` and `SchemaType.OpenApi3`; **JsonSchema.Net 9.7.0** carries the OSMF EULA license (a maintenance fee) and supports draft 6 and later only.
- The reusable precedent for an operator action behind a tenancy gate is `DeadLetterManager` (`src/Encina.Messaging/DeadLetter/DeadLetterManager.cs:22-27`): with `MultiTenancyMarker` registered and no tenant, an operation that does not opt out with `DeadLetterFilter.AllTenants` is denied. `ITenantContext` does not exist (searched). The audit abstraction is `IOperationAuditStore` and `OperationAuditEntry` (`src/Encina.Security.Audit/Abstractions/`).

Delivered: `IAmazonEventBridgeReplay` with `StartReplayAsync`, `GetReplayAsync` and `CancelReplayAsync`; `ValidateAgainstSchemaRegistry` and its options; an internal schema cache and validator; log messages 40698-40699 (the spare ids of #238's block); unit, guard and integration tests; documentation. Delivery semantics of replay are at-least-once by design.

**Provider category**: Messaging transports (10 existing, 6 planned; AGENTS.md section 5); one cloud transport tested with LocalStack for what it supports (SPEC-000 section 6.1, DEC-009).

**Shared pieces**: #238 owns the package, `LocalStackFixture` (`Cloud-LocalStack`) and the spare ids 40698-40699; #240 owns the conformance base (EventBridge subclasses its publish scenarios only, replay and schemas have none); #4543 owns the tenant and module header contract (CloudEvents extension attributes), unrelated to this plan's gate. This plan assumes #238 Design Choice 5 is answered A (replay and schemas leave #238); under its option B the same tasks move into #238 and this plan is closed.

---

## Design Choices

<details>
<summary><strong>1. Placement of the schema client (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AWSSDK.Schemas` and NJsonSchema inside `Encina.AmazonEventBridge`** | One package; the validation sits next to the publisher it guards | Every EventBridge user restores two more packages whether they validate or not |
| **B) A separate package `Encina.AmazonEventBridge.Schemas` with an `IEventSchemaValidator` hook that the transport calls when present** | The transport keeps its small dependency set; pay-for-what-you-use | A public hook in the transport, a second package, manifest and README |

### Chosen Option: **A, inside the transport package** (recommended, pending the maintainer)

### Rationale

A wins because the replay wrapper needs no extra package and the validation is one small internal class; B wins if the maintainer weighs the dependency set of the transport above the cost of a second package. The cost of A is the two `PackageReference` entries and the same two `Directory.Packages.props` entries.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Replay as code or as operator action (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A code wrapper `IAmazonEventBridgeReplay`: start, read state, cancel, as `Either` values** | Applications that replay programmatically (a recovery job after a consumer defect) get typed errors and the audit and tenancy gates of D6 and D7 | A surface to maintain and a permission (`events:StartReplay`) the application role must hold |
| **B) Nothing in code; operators use the console, the CLI or infrastructure as code** | No runtime permission; no gate to build | No audit or tenancy gate on the action; no typed state |

### Chosen Option: **A, a code wrapper** (recommended, pending the maintainer)

### Rationale

A wins for programmatic recovery and for the gates (a console replay has neither); B wins where least privilege forbids the application role from starting replays. The wrapper is registered only through `AddEncinaAmazonEventBridge`, never starts anything by itself, and a replay needs an existing archive (created by the #238 provisioner or by infrastructure as code).

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Local validation library (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) NJsonSchema (MIT)** | Validates `OpenApi3` and `JsonSchema` schema types (`SchemaType`, verified in the 11.6.2 XML documentation) which are the two types the registry stores; depends only on Newtonsoft.Json, already pinned | Newtonsoft-based; draft-4 support of the `JSONSchemaDraft4` type is confirmed by a probe test in pull request 2 |
| **B) JsonSchema.Net (OSMF EULA license)** | System.Text.Json native | A license with a maintenance fee; no draft 4, no OpenAPI 3 dialect |
| **C) No local validation: the registry is used for discovery only** | No dependency | The acceptance criterion (a violating detail is refused before publishing) is not met |

### Chosen Option: **A, NJsonSchema** (recommended, pending the maintainer)

### Rationale

A wins because it covers the schema types the registry holds and is MIT; B wins if the maintainer prefers a System.Text.Json library and accepts the fee and the dialect gap; C is excluded by the issue's acceptance criterion. A schema of a type other than `OpenApi3` or `JSONSchemaDraft4` is refused (Design Choice 8).

**Class:** Dilemma

</details>

<details>
<summary><strong>4. What is validated: the detail or the whole event (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The `Detail` JSON Encina sends (the CloudEvent of #238 D4), against the schema configured for its detail type** | Matches custom schemas written for the application's payload; nothing the service sets is invented locally | Schemas generated by discovery describe the whole event envelope and do not fit |
| **B) The whole event as EventBridge delivers it (`source`, `detail-type`, `time`, `detail`, and `account`, `region` and `id` set by the service)** | Fits discovered schemas | Encina must build service-assigned fields locally; a mismatch risk |

### Chosen Option: **A, the detail only** (recommended, pending the maintainer)

### Rationale

A wins for schemas the team registers for its own events; B wins for teams that rely on schema discovery. The mapping is explicit: `SchemaNames` maps a detail type to a schema name; a detail type with no entry is refused while validation is on (Design Choice 8). Error details carry property paths only, never values.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Schema version and lookup failure (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A pinned `SchemaVersion` per schema name; the content is cached per (registry, schema, version) for the life of the process; any lookup failure refuses the publish** | Deterministic; no TTL; fails closed | A new version needs a configuration change |
| **B) The latest version, resolved with a TTL (`SchemaCacheDuration` through `TimeProvider`); while the registry is unreachable the last cached schema is served, and a detail type never cached is refused** | New versions are picked up | A stale schema can admit an event the new version would refuse |

### Chosen Option: **A, pinned versions** (recommended, pending the maintainer)

### Rationale

A wins when the schema is a reviewed contract; B wins when teams publish schema versions faster than they deploy. Both refuse a publish when no schema can be applied (Design Choice 8). The cache is internal (a `ConcurrentDictionary`), not `ICacheProvider`, so the package takes no dependency on `Encina.Caching`.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Replay under multi-tenancy (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) With `MultiTenancyMarker` registered, a replay request must set `AllTenants = true`, else `Left` `EVENTBRIDGE_REPLAY_ALL_TENANTS_REQUIRED` (`InvalidRequest`, ADR-062 R3); the choice is logged (40698) and audited (D7); with tenancy off there is no gate** | A replay cannot filter by tenant (`StartReplay` has no event filter), so the operator must acknowledge it replays every tenant's events; the `DeadLetterFilter.AllTenants` precedent | An extra flag to set |
| **B) Require `FilterRuleArns` (the rules the replay targets) when tenancy is on, assuming one rule per tenant** | Narrows the blast radius | Assumes a per-tenant rule layout Encina does not create |
| **C) No gate** | Simplest | An operator action that re-delivers every tenant's events with no acknowledgement |

### Chosen Option: **A, an explicit `AllTenants` acknowledgement** (recommended, pending the maintainer)

### Rationale

A and B both fail closed on missing tenant context, so the rule does not decide between them; A wins because it matches an existing precedent and needs no layout assumption, B wins for deployments with a rule per tenant. C is excluded by AGENTS.md section 3 (a gate with missing context denies, the only opt-out is explicit and logged). No tenant-qualified name or id is built here, so the reserved `default` segment does not apply.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Auditing a replay (Dilemma D7)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An optional `IOperationAuditStore`: when registered, `StartReplayAsync` records an `OperationAuditEntry` (`Action` `eventbridge.replay.start`, `EntityType` `EventBridgeReplay`, `EntityId` the replay name, the outcome, the error code never the message) and refuses to start when recording fails (`EVENTBRIDGE_AUDIT_FAILED`, `Internal`, R4)** | The issue's "worth auditing"; an unaudited replay cannot happen when a store is present | A project reference to `Encina.Security.Audit` |
| **B) Log and span only; the application audits through its own pipeline** | No dependency | The action is not audited by the package |

### Chosen Option: **A, optional audit store** (recommended, pending the maintainer)

### Rationale

A wins because the issue marks audit included; B wins if the maintainer wants the transport free of the audit package. The store is an optional constructor parameter (`IOperationAuditStore? audit = null`): absent means not audited, which the documentation states.

**Class:** Dilemma

</details>

<details>
<summary><strong>8. A schema that cannot be applied refuses the publish</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) With validation on, a detail type with no schema mapping, a schema that is missing, unreadable or of an unsupported type, and a detail that violates the schema each return a `Left` and nothing is published** | Matches the acceptance criterion; never reports success on a failure | A misconfigured mapping blocks publishing |
| **B) Publish unvalidated with a warning when the schema cannot be applied** | Availability | Reports success for an event nobody validated |

### Chosen Option: **A, refuse**

### Rationale

Errors are never swallowed (AGENTS.md section 3) and the issue's acceptance criterion says a violating detail "is refused before publishing when validation is on". Codes (ADR-062): a violating detail `EVENTBRIDGE_SCHEMA_VALIDATION_FAILED` `Validation` (R3); a missing mapping or an unsupported schema type `EVENTBRIDGE_SCHEMA_NOT_CONFIGURED` `Internal` (R1, a configuration entry); a registry failure `EVENTBRIDGE_SCHEMA_LOOKUP_FAILED` `Internal` (R4). Cancellation propagates and is never a `Left`.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (errors are never swallowed) and #4545 acceptance criterion 2

</details>

<details>
<summary><strong>9. LocalStack coverage and replay request rules</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A probe test records what LocalStack supports (archives, replays, schemas); what it lacks is covered at the unit level and named in the tests; replay requests are validated locally before any call (end after start, archive ARN present, replay name well formed)** | The #4545 Settled entry 3 and SPEC-000 section 6.1; nothing is skipped silently | Integration targets depend on the probe |
| **B) Skip the unsupported features in integration tests** | Smaller | Silent skips |

### Chosen Option: **A, probe and name**

### Rationale

SPEC-000 section 6.1 keeps cloud items inside 1.0 when a free local emulator exists and says what is unsupported is tested otherwise; the issue records the same. Codes: `EVENTBRIDGE_REPLAY_INVALID` `Validation` (R3, field rules), `EVENTBRIDGE_REPLAY_ARCHIVE_REQUIRED` `InvalidRequest` (R3, a missing id), `EVENTBRIDGE_REPLAY_ALREADY_EXISTS` `AlreadyExists` (R2), `EVENTBRIDGE_REPLAY_NOT_FOUND` `NotFound` (R1, the replay name is supplied by the caller), `EVENTBRIDGE_REPLAY_THROTTLED` `Unavailable` (R7), `EVENTBRIDGE_REPLAY_FAILED` `Internal` (R4).

**Class:** Settled
**Settled by:** SPEC-000 section 6.1 and DEC-009, #4545 Alternatives entry 3, ADR-062 R1 to R4 and R7

</details>

<details>
<summary><strong>10. Telemetry</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Spans `replay` and `validate` on the package `ActivitySource` of #238 and a counter `amazoneventbridge.replay.started` with tag `outcome`; the tenant tag `encina.tenant_id` only when multi-tenancy is on and a tenant is known** | Follows ADR-054 D4; no tag when tenancy is off | None |
| **B) No telemetry** | None | Violates the issue's Included row |

### Chosen Option: **A, spans and one counter**

### Rationale

ADR-054 D4 requires the tenant attribute and tag only when multi-tenancy is enabled; a replay has no single tenant (D6), so its tag is absent and its span carries `all_tenants` as a boolean attribute.

**Class:** Settled
**Settled by:** ADR-054 D4 and #4545 Cross-Cutting row 2

</details>

---

## Implementation Phases

### Phase 1: Replay wrapper (pull request 1)

<details>
<summary>Tasks</summary>

1. `IAmazonEventBridgeReplay` and `AmazonEventBridgeReplay` (public sealed, scoped; public constructor `(IAmazonEventBridge client, IOptions<EncinaAmazonEventBridgeOptions> options, ILogger<AmazonEventBridgeReplay> logger, TimeProvider? timeProvider = null, MultiTenancyMarker? tenancy = null, IOperationAuditStore? audit = null)`): `StartReplayAsync(EventBridgeReplayRequest, CancellationToken)` returning `ValueTask<Either<EncinaError, EventBridgeReplayStatus>>`, `GetReplayAsync(string replayName, CancellationToken)`, `CancelReplayAsync(string replayName, CancellationToken)`. `EventBridgeReplayRequest` (record: `ReplayName`, `ArchiveArn`, `DestinationBusArn`, `FilterRuleArns`, `EventStartTime`, `EventEndTime`, `AllTenants`, `Description`) with `Validate()` (Design Choice 9) and `EventBridgeReplayStatus` (the state as an enum mapped from the service strings, `StateReason` as a code only, never the service text). The gates run in the order: validate, tenancy (D6), audit (D7), call.
2. Errors added to `AmazonEventBridgeErrorCodes.cs` (categories in Design Choices 6, 7 and 9); a service exception is mapped by type, never by its message; the exception goes to `ForLogging()`.
3. `Log.cs` 40698 (replay requested: outcome code, `all_tenants`; no replay name). `Encina.AmazonEventBridge.csproj` gains the `Encina.Security.Audit` project reference (D7 A). DI: `AddEncinaAmazonEventBridge` gains `TryAddScoped<IAmazonEventBridgeReplay, AmazonEventBridgeReplay>()`, with the resolution test under `ValidateOnBuild` and `ValidateScopes` (the optional `tenancy` and `audit` parameters resolve to null when unregistered).
4. Tests: `AmazonEventBridgeReplayTests` (unit over a substituted client: every state, every code, gate order, a failing audit store refuses, cancellation propagates), `EventBridgeReplayRequestTests`, DI test edit, guard edit (null `client`, `options`, `logger`; the three nullable parameters accepted), manifest edit. 12 files: interface, implementation, models, error codes edit, `Log` edit, DI edit, csproj edit, 2 unit tests, DI test edit, guard edit, manifest.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: The EventBridge transport (#238) exists with a scoped publisher and IAmazonEventBridge registered; StartReplay needs an archive.
TASK: Add IAmazonEventBridgeReplay with start, get and cancel, request validation, the tenancy gate, the optional audit and the log message, with unit and guard tests.
KEY RULES: named ADR-062 category on every EncinaErrors call; constant messages, never the AWS text; the replay name and ARNs never logged; with MultiTenancyMarker registered and no AllTenants the request is denied; an audit failure refuses the start; cancellation propagates; TimeProvider for any timestamp.
REFERENCE FILES: src/Encina.Messaging/DeadLetter/DeadLetterManager.cs, src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs, docs/plans for #238, docs/architecture/adr/062-error-category-contract.md.
```

</details>

### Phase 2: Schema validator (pull requests 2 and 3)

<details>
<summary>Tasks</summary>

1. Pull request 2, the internal pieces: `AmazonEventBridge.csproj` gains `AWSSDK.Schemas` and `NJsonSchema`, with their entries in `Directory.Packages.props` (4.1.0.2 and 11.6.2; the restore must show no downgrade warning for `AWSSDK.Core`, as in #238 pull request 1); options `ValidateAgainstSchemaRegistry` (default false), `SchemaRegistryName`, `SchemaNames` (detail type to schema name), `SchemaVersions` (schema name to version, Design Choice 5 A) and the validator rules (registry name required and mappings non-empty when on, versions required); `IAmazonSchemas` as `TryAddSingleton` with the same endpoint rules as the EventBridge client; internal `IEventSchemaValidator`, `EventSchemaValidator` (reads the schema through `DescribeSchema`, parses it by its `Type` with NJsonSchema, validates the detail and returns property paths only) and `SchemaCache`; `EVENTBRIDGE_SCHEMA_*` codes. A pinned-version cache never expires; under D5 B an expiry through `TimeProvider` is added instead.
2. Pull request 3, the wiring: `AmazonEventBridgeMessagePublisher` gains the optional trailing constructor parameter `IEventSchemaValidator? validator = null` (the `new AmazonEventBridgeMessagePublisher(` sites of #238, found by search at pick-up, keep compiling); validation runs before the put, on the detail produced by the CloudEvents formatter, and a `Left` returns before any call; `Log.cs` 40699 (event refused by the schema: error code only). DI registers the validator only when `ValidateAgainstSchemaRegistry` is on, proven by two DI tests (on and off) with `ValidateOnBuild` and `ValidateScopes`.
3. Tests: `EventSchemaValidatorTests` (an OpenAPI 3 schema, a JSON Schema draft 4 schema, a violating detail, a missing mapping, an unsupported type, a registry failure), `SchemaCacheTests` (hit, miss, never refetched for a pinned version), options and validator test edits, publisher test edit, guard edit (constructor with the nullable validator accepted), manifest edits. Pull request 2 has 13 files and pull request 3 has 5.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: AWSSDK.Schemas DescribeSchema returns the schema content and its type (OpenApi3 or JSONSchemaDraft4); NJsonSchema validates with SchemaType.OpenApi3 or SchemaType.JsonSchema.
TASK: Add the internal schema validator and cache and the options, then wire the validator into the publisher before the put.
KEY RULES: with validation on, any schema that cannot be applied refuses the publish; error details carry property paths only, never values; a pinned version is cached for the process lifetime; cancellation propagates; the validator is registered only when the option is on.
REFERENCE FILES: docs/plans for #238, src/Encina.AmazonSQS/EncinaAmazonSQSOptionsValidator.cs, AGENTS.md section 3.
```

</details>

### Phase 3: Cross-cutting integration and observability

<details>
<summary>Tasks</summary>

1. Telemetry (Design Choice 10): the spans `replay` and `validate` and the counter `amazoneventbridge.replay.started` on the source and meter registered by #238, with the tenant tag only when tenancy is on and a tenant is known; the edits sit in pull requests 1 and 3.
2. Verify the matrix below against the code; confirm every code has its ADR-062 category and that no log call, tag or error detail carries a replay name, an ARN, a payload value or `EncinaError.Message`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Replay and schema validation exist.
TASK: Add the spans and counter, verify the cross-cutting matrix against the code and fix any gap.
KEY RULES: AGENTS.md sections 3, 6, 7; ADR-054 D4; ADR-062 categories.
REFERENCE FILES: AGENTS.md section 6, docs/plans for #238 (Phase 4 diagnostics).
```

</details>

### Phase 4: LocalStack probe and integration tests (pull request 4)

<details>
<summary>Tasks</summary>

1. `AmazonEventBridgeReplayProbeTests` (first task): records which of `CreateArchive`, `StartReplay`, `DescribeReplay` and the Schemas API the pinned LocalStack image of `LocalStackFixture` accepts and enforces; the integration targets below hold when it does, and a feature it lacks is asserted at the unit level and named in the test, never skipped silently.
2. `AmazonEventBridgeReplayIntegrationTests` and `AmazonEventBridgeSchemaIntegrationTests` in the `Cloud-LocalStack` collection (`[Trait("Category","Integration")]`, `[Trait("Transport","AmazonEventBridge")]`): a replay is started and read back; a violating event is refused and a conforming one reaches the bus. `LocalStackFixture` gains the `schemas` service name when the probe shows it is available. 5 files with the manifest edit.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: SPEC-000 section 6.1 requires cloud items to be tested with a local emulator; LocalStack may not implement archives, replays or schemas.
TASK: Add the probe and the integration tests against the shared LocalStack collection.
KEY RULES: one shared container, never per class; no Thread.Sleep (bounded waits); Shouldly through Encina.Testing.Shouldly; unsupported features are named, never skipped silently.
REFERENCE FILES: tests/Encina.TestInfrastructure/Fixtures/LocalStackFixture.cs, tests/Encina.IntegrationTests/MessageBrokers/AmazonSQS/.
```

</details>

### Phase 5: Documentation and finalization (pull request 5)

<details>
<summary>Tasks</summary>

Per-file targets, entered in `Encina.AmazonEventBridge.json` by the pull request that adds the tests (a target needs its test file in the same pull request; the file rows of the package are created by #238, so "unchanged" quotes that plan's planned values):

| Pull request | File | unit | guard | contract | property | integration | Justification |
|---|---|:---:|:---:|:---:|:---:|:---:|---|
| 1 | `AmazonEventBridgeReplay.cs` | 90 | 20 | 0 | 0 | 40 | A substituted client reaches every state and code; guard tests reach the constructor checks; LocalStack covers start and read only if the probe shows it. |
| 1 | `EventBridgeReplayModels.cs` | 100 | 0 | 0 | 0 | 0 | Records and `Validate()` are pure; guard tests have no argument to check. |
| 2 | `EventSchemaValidator.cs` | 90 | 0 | 0 | 0 | 40 | An internal type: unit tests reach each schema type and failure; guard tests cannot reach it; integration depends on LocalStack Schemas. |
| 2 | `SchemaCache.cs` | 95 | 0 | 0 | 0 | 0 | A small internal class with a fake-clock test per branch. |
| 2 | options and validator files | 100 | 0 | 0 | 0 | 0 | Pure data and validation, as #238 plans. |
| 3 | `AmazonEventBridgeMessagePublisher.cs` | 85 | 20 | 50 | 0 | 65 | The unit, guard, contract and integration values of #238's publisher row are kept, never lowered; the edit adds validator cases to the unit tests. |
| 1 to 3 | `ServiceCollectionExtensions.cs` | 85 | 10 | 0 | 0 | 0 | The #238 row values; the DI tests add three registrations. |
| 1 to 3 | error codes, `Log.cs` | 0 | 0 | 0 | 0 | 0 | Constants and generated delegates have no executable lines. |

1. `src/Encina.AmazonEventBridge/README.md` (Replay and Schema registry sections) and the EventBridge feature page of #238's documentation pull request (or the README alone while that page is not on `main`) through `docs-writer`; `changelog.d/4545-eventbridge-schema-registry-replay.added.md` through `mechanical-fixer`; the load and benchmark justification files of #238 already cover the package. 4 files.
2. `dotnet build --configuration Release` with zero warnings; every flag at its target; CRAP of every new method at most 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: Replay and schema validation are implemented and tested.
TASK: Hand the README sections, the feature page and the changelog fragment to the owning specialists; re-measure the per-flag targets.
KEY RULES: targets never go down; no hand-typed coverage figures in docs; the docs state that a replay re-delivers to every matching rule and that consumers deduplicate on the message id, and that without an audit store a replay is not audited.
REFERENCE FILES: changelog.d/README.md, docs/plans for #238 (documentation phase).
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / Spec | Relevance | Notes |
|-----------------|-----------|-------|
| `AGENTS.md` sections 3, 5, 6, 7, 9 | Fail-closed gates, errors never swallowed, transport category, EventIds, coverage | All phases |
| ADR-062 R1 to R4, R7 | Categories of every code | Design Choices 8, 9 |
| ADR-054 D4 | Tenant tag only when tenancy is on | Design Choice 10 |
| SPEC-000 section 6.1, DEC-009 | EventBridge in 1.0, tested with LocalStack | Phase 4 |
| EventBridge archive and replay, Schema registry (named product documentation) | `StartReplay` re-sends the archive range to the bus; schemas of type OpenApi3 or JSONSchemaDraft4 | Design Choices 2 to 5 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `DeadLetterManager`, `DeadLetterFilter.AllTenants` | `src/Encina.Messaging/DeadLetter/DeadLetterManager.cs` | Tenancy gate precedent |
| `MultiTenancyMarker` | `src/Encina/Abstractions/MultiTenancyMarker.cs` | Tenancy-on signal |
| `IOperationAuditStore`, `OperationAuditEntry` | `src/Encina.Security.Audit/Abstractions/` | Optional audit of a replay |
| `EncinaAmazonSQSOptionsValidator` | `src/Encina.AmazonSQS/` | Endpoint rules reused for the schemas client |
| Plan of #238 | `docs/plans/` | Package shape, fixture, spare ids |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `AmazonEventBridge` (the range registered by #238, 40690-40699) | 40698-40699 | 40698 replay requested, 40699 event refused by the schema; the two spare ids of #238 (40690-40697 are claimed); if #238's implementation used them first the ids are to be assigned by the orchestrator |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 10 | replay interface and class, models, schema validator interface and class, cache, 3 edits to options, validator and publisher |
| Tests | 11 | replay, request, validator, cache, options edits, publisher edit, probe, 2 integration, guard, DI |
| Shared edits | 5 | csproj, `Directory.Packages.props`, DI, error codes, `Log`, fixture |
| Documentation and manifests | 4 | README, feature page, changelog, manifest |
| **Pull request 1** | **12** | about 700 lines |
| **Pull request 2** | **13** | about 800 lines |
| **Pull request 3** | **5** | about 250 lines |
| **Pull request 4** | **5** | about 350 lines |
| **Pull request 5** | **4** | about 150 lines |

Implementers and callers: `IAmazonEventBridgeReplay` is new; the publisher constructor gains an optional trailing parameter, so the `new AmazonEventBridgeMessagePublisher(` sites of #238 keep compiling (listed by search at pick-up). Each pull request builds alone in order.

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library; no backward compatibility, zero warnings, ROP (Either<EncinaError, T>). #238 provides the EventBridge publishing transport.

IMPLEMENTATION OVERVIEW: PR1 replay wrapper with tenancy gate, optional audit and log 40698; PR2 schema validator, cache, options and packages; PR3 wiring into the publisher and log 40699; PR4 LocalStack probe and integration tests; PR5 documentation. Dilemmas D1 to D7 are implemented as the maintainer answers, the recommended options until then.

KEY PATTERNS: a schema that cannot be applied refuses the publish; replay under tenancy needs AllTenants; no replay name, ARN, payload value or exception message in logs; named ADR-062 category on every error; constant messages; TimeProvider; cancellation never a Left; targets never go down; CRAP <= 10.

REFERENCE FILES: docs/plans for #238, src/Encina.Messaging/DeadLetter/DeadLetterManager.cs, src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ | An internal schema cache per (registry, schema, version) (Design Choice 5); not `ICacheProvider`. |
| 2 | OpenTelemetry | ✅ | Spans `replay` and `validate`, one counter, tenant tag only when tenancy is on (Design Choice 10). |
| 3 | Structured Logging | ✅ | 40698-40699, error code and outcome only. |
| 4 | Health Checks | ❌ | No new dependency of the bus check of #238. |
| 5 | Validation | ✅ | Schema validation is the feature; replay requests are validated locally. |
| 6 | Resilience | ⏭️ | Publisher decorators are #4393 and #742 (open); the AWS SDK retries by itself (#4393's per-provider table); replay and schema calls are not wrapped here. |
| 7 | Distributed Locks | ❌ | The service enforces a unique replay name (`ResourceAlreadyExistsException`). |
| 8 | Transactions | ❌ | None. |
| 9 | Idempotency | ❌ | Replay re-delivers by design; consumers deduplicate on the message id (inbox). |
| 10 | Multi-Tenancy | ✅ | Replay is denied without `AllTenants` when tenancy is on (D6); tenant header stamping is #4543 (open). |
| 11 | Module Isolation | ⏭️ | Module header is #4543 Phase 5. |
| 12 | Audit Trail | ✅ | A replay start is recorded through the optional `IOperationAuditStore` (D7). |

---

## Prerequisites & Dependencies

- **Blocking**: #238 pull requests 1 and 2 (options, validator, publisher, DI), pull request 4 (the `ActivitySource` and `Meter`) for the telemetry edits, and pull request 5 (the LocalStack fixture extension) for Phase 4; #238 Design Choice 5 answered A.
- **Related, not blocking**: #4543 (header contract), #4393 and #742 (resilience), #260 (CloudEvents abstraction: only the formatter file changes), #1164.
- **Sibling plans**: #4544 is independent.

---

## Next Steps

1. Ask the maintainer D1 to D7 when work on #4545 starts; record the answers in `## Maintainer Decisions`.
2. Pull request 1: replay wrapper (12 files).
3. Pull requests 2 and 3: schema validator and cache (13), wiring into the publisher (5).
4. Pull requests 4 and 5: LocalStack probe and integration tests (5), documentation (4).
