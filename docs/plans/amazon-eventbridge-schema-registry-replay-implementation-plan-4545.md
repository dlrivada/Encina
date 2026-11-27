# Implementation Plan: Amazon EventBridge schema registry validation and archive replay

> **Issue**: [#4545](https://github.com/dlrivada/Encina/issues/4545)
> **Type**: Feature
> **Verified against**: origin/main `f9063538e` (2026-11-27)
> **Complexity**: Medium (5 phases, 6 pull requests of about 10, 8, 14, 7, 6 and 4 files)
> **Estimated Scope**: about 800 lines of production code and about 1,600 lines of tests
> **Family**: transport-new-1 follow-ups (#4543, #4544, #4545). Builds on #238 (the EventBridge transport).

---

## Summary

#238 publishes events to an event bus in CloudEvents format and, when `AutoCreateResources` is on, creates an archive (`EnableArchive`); its Design Choice 5 recommends leaving replay as an operator action and the schema registry as a follow-up. This plan adds both: a replay wrapper over `StartReplay`, `DescribeReplay` and `CancelReplay` that returns `Either<EncinaError, ...>`, and an opt-in validation of the event against a schema of the EventBridge schema registry before publishing. State of `origin/main` (`f9063538e`):

- `Encina.AmazonEventBridge` is not on `main` (only its plan); this plan edits what #238 creates: `EncinaAmazonEventBridgeOptions`, its validator, `AmazonEventBridgeMessagePublisher`, `AmazonEventBridgeErrorCodes.cs`, `Log.cs`, `ServiceCollectionExtensions.cs`, and the `AmazonEventBridgeActivitySource` and `AmazonEventBridgeMetrics` of its pull request 4. The package carries no `PublicAPI.*.txt`, like `Encina.AmazonSQS`, `Encina.NATS` and `Encina.Kafka` (searched).
- AWS SDK facts, verified on 2026-11-27 by downloading the packages: **AWSSDK.EventBridge 4.1.0.2** has `StartReplay` (request `ReplayName`, `EventSourceArn`, `Destination`, `EventStartTime`, `EventEndTime`, `Description`), `DescribeReplay` (state, `StateReason`, `EventLastReplayedTime`), `CancelReplay`, the states `STARTING`, `RUNNING`, `CANCELLING`, `COMPLETED`, `CANCELLED` and `FAILED`, and the exceptions `ResourceNotFoundException`, `ResourceAlreadyExistsException`, `LimitExceededException`, `ThrottlingException`, `AccessDeniedException`, `InvalidStateException` and `InternalException`; **AWSSDK.Schemas 4.1.0.2** (Apache-2.0, net10.0) has `DescribeSchema` (request `RegistryName`, `SchemaName`, `SchemaVersion`; response `Content` and `Type`), `GetDiscoveredSchema` and `SearchSchemas`. A replay re-sends the archived events of a time range to the bus, so every matching rule delivers again: consumers deduplicate on the message id (the inbox).
- The repository has no JSON Schema validator: `Directory.Packages.props` pins `Newtonsoft.Json` 13.0.4 and no schema library. **NJsonSchema 11.6.2** (MIT; net8.0 and netstandard2.0; depends on Newtonsoft.Json) has `JsonSchema.FromJsonAsync` and `JsonSchema.Validate(string, SchemaType, settings)` with `SchemaType.JsonSchema` and `SchemaType.OpenApi3` (verified in its XML documentation); **JsonSchema.Net 9.7.0** carries the OSMF EULA license (a maintenance fee) and supports draft 6 and later only. The registry stores schemas of type `OpenApi3` or `JSONSchemaDraft4`; an `OpenApi3` schema is a whole OpenAPI document (its types sit under `components.schemas`), not a bare JSON schema.
- Tenancy: `MultiTenancyMarker` (`src/Encina/Abstractions/MultiTenancyMarker.cs`) is the tenancy-on signal; `ITenantContext` does not exist (searched). `DeadLetterManager` (`src/Encina.Messaging/DeadLetter/DeadLetterManager.cs:22-27`) scopes by the ambient tenant and denies when tenancy is on and none is resolved, with `DeadLetterFilter.AllTenants` as the explicit operator opt-out. The audit abstraction is `IOperationAuditStore` and `OperationAuditEntry` (`src/Encina.Security.Audit/Abstractions/`): `Id`, `CorrelationId`, `Action`, `EntityType`, `Outcome` (`AuditOutcome`: `Success`, `Failure`, `Denied`, `Error`), `TimestampUtc`, `StartedAtUtc` and `CompletedAtUtc` are required; `UserId`, `TenantId`, `EntityId` and `Metadata` are optional.

Delivered: `IAmazonEventBridgeReplay` with `StartReplayAsync`, `GetReplayAsync` and `CancelReplayAsync`; `ValidateAgainstSchemaRegistry` and its options; a public `IEventSchemaValidator` with an internal implementation and cache; log messages (two ids, see Research); unit, guard and integration tests; documentation. Replay delivery is at-least-once by design.

**Provider category**: Messaging transports (10 existing, 6 planned; AGENTS.md section 5); one cloud transport tested with LocalStack for what it supports (SPEC-000 section 6.1, DEC-009).

**Shared pieces**: #238 owns the package, `LocalStackFixture` (`Cloud-LocalStack`) and its range; #240 owns the conformance base (EventBridge subclasses its publish scenarios only); #4543 owns the tenant and module header contract and raises the publisher contract target; this plan keeps the publisher row at the values #238 and #4543 enter (unit 85, guard 20, contract 50, integration 65). This plan assumes #238 Design Choice 5 is answered A (replay and schemas leave #238); under its option B the same tasks move into #238 and this plan is closed.

---

## Design Choices

<details>
<summary><strong>1. Placement of the schema client (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AWSSDK.Schemas` and NJsonSchema inside `Encina.AmazonEventBridge`** | One package; the validation sits next to the publisher it guards | Every EventBridge user restores two more packages whether they validate or not |
| **B) A separate package `Encina.AmazonEventBridge.Schemas` that registers the `IEventSchemaValidator` the transport already accepts** | The transport keeps its small dependency set; pay-for-what-you-use | A second package, manifest and README |

### Chosen Option: **A, inside the transport package** (recommended, pending the maintainer)

### Rationale

A wins because the replay wrapper needs no extra package and the validation is one small class; B wins if the maintainer weighs the dependency set of the transport above a second package. `IEventSchemaValidator` is public in both options (it is a constructor parameter of the public publisher), so B needs no further change to the transport.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Replay as code or as operator action (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A code wrapper `IAmazonEventBridgeReplay`: start, read state, cancel, as `Either` values** | Applications that replay programmatically (a recovery job after a consumer defect) get typed errors and the gates of D6 and D7 | A surface to maintain and a permission (`events:StartReplay`) the application role must hold |
| **B) Nothing in code; operators use the console, the CLI or infrastructure as code** | No runtime permission; no gate to build | No audit or tenancy gate on the action; no typed state |

### Chosen Option: **A, a code wrapper** (recommended, pending the maintainer)

### Rationale

A wins for programmatic recovery and for the gates (a console replay has neither); B wins where least privilege forbids the application role from starting replays. The wrapper never starts anything by itself, and a replay needs an existing archive (created by the #238 provisioner or by infrastructure as code).

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Local validation library (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) NJsonSchema (MIT)** | Validates the `OpenApi3` and `JsonSchema` schema types of its `SchemaType`; depends only on Newtonsoft.Json, already pinned | Newtonsoft-based; its draft-4 support is a stated prerequisite (below) |
| **B) JsonSchema.Net (OSMF EULA license)** | System.Text.Json native | A license with a maintenance fee; no draft 4, no OpenAPI 3 dialect |
| **C) No local validation: the registry is used for discovery only** | No dependency | Acceptance criterion 2 (a violating detail is refused) is not met |

### Chosen Option: **A, NJsonSchema** (recommended, pending the maintainer)

### Rationale

A wins because it covers the schema types the registry holds and is MIT; B wins if the maintainer prefers a System.Text.Json library and accepts the fee and the dialect gap; C is excluded by the acceptance criterion. Prerequisite, not a deferred check: the first test of pull request 3 validates a draft-4 document and an OpenAPI 3 document with NJsonSchema, and the pull request does not proceed (the `JSONSchemaDraft4` type is then refused as `EVENTBRIDGE_SCHEMA_NOT_CONFIGURED` and D3 is asked again) unless both pass.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. What is validated and against which part of the schema (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The `Detail` JSON Encina sends (the CloudEvent of #238 D4). A mapping entry names the schema and, for an `OpenApi3` schema, the component under `components.schemas` that describes the detail (required for `OpenApi3`; resolved with its `$ref`s inside the same document); a `JSONSchemaDraft4` schema is applied as a whole** | Matches schemas the team registers for its own events; the rule is explicit | Schemas generated by discovery describe the whole event and need a component that is the detail |
| **B) The whole event as EventBridge delivers it (`source`, `detail-type`, `time`, `detail`, plus `account`, `region` and `id` that the service sets), against the `AWSEvent` component** | Fits discovered schemas | Encina builds service-assigned fields locally; a mismatch risk |

### Chosen Option: **A, the detail against a named component** (recommended, pending the maintainer)

### Rationale

A wins for schemas written for the application's own events; B wins for teams that rely on schema discovery. `SchemaNames` maps a detail type to `(schema name, component name)`; error details carry property paths only, never values.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Schema version and lookup failure (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A pinned `SchemaVersion` per schema name; the content is cached per (registry, schema, version) for the life of the process; no TTL and no clock** | Deterministic; simple | A new version needs a configuration change |
| **B) The latest version, resolved with a TTL (`SchemaCacheDuration` through `TimeProvider`); while the registry is unreachable the last cached schema is served** | New versions are picked up | A stale schema can admit an event the new version would refuse |

### Chosen Option: **A, pinned versions** (recommended, pending the maintainer)

### Rationale

A wins when the schema is a reviewed contract; B wins when teams publish schema versions faster than they deploy. The cache is internal (a `ConcurrentDictionary`), not `ICacheProvider`, so the package takes no dependency on `Encina.Caching`. What happens when the lookup fails is Design Choice 8.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Replay under multi-tenancy (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) With `MultiTenancyMarker` registered, a replay request must set `AllTenants = true`, else `Left` `EVENTBRIDGE_REPLAY_ALL_TENANTS_REQUIRED` (`InvalidRequest`, ADR-062 R3); with tenancy off there is no gate; the span carries `all_tenants`** | `StartReplay` has no event filter, so the operator must acknowledge that every tenant's events are replayed | An extra flag to set; stricter than `DeadLetterManager`, which scopes by an ambient tenant when one exists and denies only without one |
| **B) Require `FilterRuleArns` (the rules the replay targets) when tenancy is on, assuming one rule per tenant** | Narrows the blast radius | Assumes a per-tenant rule layout Encina does not create |
| **C) No gate** | Simplest | An operator action that re-delivers every tenant's events with no acknowledgement |

### Chosen Option: **A, an explicit `AllTenants` acknowledgement** (recommended, pending the maintainer)

### Rationale

A and B both fail closed on missing tenant context; A wins because it needs no layout assumption and echoes the `AllTenants` opt-out of the dead letter manager (the replay cannot scope by tenant, so no ambient tenant narrows it), B wins for deployments with a rule per tenant. C is excluded by AGENTS.md section 3 (a gate with missing context denies, the only opt-out is explicit and logged). No tenant-qualified name or id is built here, so the reserved `default` segment does not apply.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Auditing a replay (Dilemma D7)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An optional `IOperationAuditStore`, recorded after the call so the outcome is known: `Action` `eventbridge.replay.start`, `EntityType` `EventBridgeReplay`, `EntityId` the replay name, `Outcome` `Success`, `Error` (a failed call) or `Denied` (a gate refusal), `StartedAtUtc` and `CompletedAtUtc` from `TimeProvider`, `TimestampUtc` the completion time, `CorrelationId` the request's `CorrelationId` (default the replay name), `UserId` the request's `RequestedBy` (required when a store is registered, else the request is `Validation`), `TenantId` null, `Metadata` `all_tenants`; the error code, never a message, in `ErrorMessage`. A failed recording after a started replay does not undo it: the replay result is returned and the failure is counted and logged** | Fits `OperationAuditEntry`; the actor is explicit; no entry claims an outcome before it exists | An unaudited replay is possible when the store fails after the start (visible in the counter and the log) |
| **B) Log and span only; the application audits through its own pipeline** | No dependency on `Encina.Security.Audit` | The package does not audit the action |

### Chosen Option: **A, optional audit store after the call** (recommended, pending the maintainer)

### Rationale

A wins because the issue marks audit included; B wins if the maintainer wants the transport free of the audit package. A recording before the call is not offered: `AuditOutcome` has no "started" value and an entry must carry the outcome. Absent store means not audited, which the documentation states.

**Class:** Dilemma

</details>

<details>
<summary><strong>8. A schema that cannot be applied: refuse or publish unvalidated (Dilemma D8)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) With validation on, a detail type with no mapping, a schema that is missing, unreadable or of an unsupported type, and a registry failure each return a `Left` and nothing is published (`EVENTBRIDGE_SCHEMA_NOT_CONFIGURED` `Internal` R1, `EVENTBRIDGE_SCHEMA_LOOKUP_FAILED` `Internal` R4)** | Never reports success for an event nobody validated | A misconfigured mapping or a registry outage blocks publishing |
| **B) Publish unvalidated with a warning and a counter when the schema cannot be applied** | Availability | Reports success for an unvalidated event |

### Chosen Option: **A, refuse** (recommended, pending the maintainer)

### Rationale

A wins when the schema is a contract the consumers rely on; B wins for a producer whose availability matters more than strict validation. Cancellation propagates and is never a `Left`.

**Class:** Dilemma

</details>

<details>
<summary><strong>9. A detail that violates its schema is refused</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) With validation on, a detail that violates the applied schema returns `EVENTBRIDGE_SCHEMA_VALIDATION_FAILED` (`Validation`, ADR-062 R3) and nothing is published** | Meets the acceptance criterion | None |
| **B) Publish it anyway** | None | Contradicts the criterion |

### Chosen Option: **A, refuse**

### Rationale

Acceptance criterion 2 of #4545 says a detail that violates the registered schema is refused before publishing when validation is on; field rules are `Validation` (R3).

**Class:** Settled
**Settled by:** #4545 acceptance criterion 2 and ADR-062 R3

</details>

<details>
<summary><strong>10. LocalStack coverage</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A probe test records what LocalStack supports (archives, replays, schemas); what it lacks is covered at the unit level and named in the tests** | Nothing is skipped silently | Integration targets depend on the probe |
| **B) Skip unsupported features in integration tests** | Smaller | Silent skips |

### Chosen Option: **A, probe and name**

### Rationale

SPEC-000 section 6.1 keeps cloud items inside 1.0 when a free local emulator exists, and the issue's Settled entry 3 says features LocalStack does not implement are tested at the unit level and named in the tests.

**Class:** Settled
**Settled by:** SPEC-000 section 6.1 and DEC-009, #4545 Alternatives entry 3

</details>

<details>
<summary><strong>11. Replay request rules and the AWS exception table</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The request is validated locally before any call and every service exception maps to one named code with a category** | Typed errors; no AWS text reaches callers | A table to keep |
| **B) Pass the service exception through** | Less code | Exposes provider text; violates the `EncinaError.Message` rule |

### Chosen Option: **A, local validation and a table**

### Rationale

ADR-062 R1 to R7 give the categories. Local rules: end after start, archive ARN present, replay name well formed. Table: `EVENTBRIDGE_REPLAY_INVALID` `Validation` (R3); `EVENTBRIDGE_REPLAY_ARCHIVE_REQUIRED` `InvalidRequest` (R3); `ResourceAlreadyExistsException` `EVENTBRIDGE_REPLAY_ALREADY_EXISTS` `AlreadyExists` (R2); `ResourceNotFoundException` on `DescribeReplay` or `CancelReplay` `EVENTBRIDGE_REPLAY_NOT_FOUND` `NotFound` (R1, the name is caller supplied) but on `StartReplay` (archive or bus missing) `EVENTBRIDGE_REPLAY_FAILED` `Internal` (R1, a configuration entry); `InvalidStateException` (cancel of a finished replay) `EVENTBRIDGE_REPLAY_STATE_CONFLICT` `Conflict` (R2); `ThrottlingException` and `LimitExceededException` `EVENTBRIDGE_REPLAY_THROTTLED` `Unavailable` (R7); `AccessDeniedException` (a missing permission of the application's role is a provider-side configuration failure) and `InternalException` `EVENTBRIDGE_REPLAY_FAILED` `Internal` (R4). Cancellation propagates.

**Class:** Settled
**Settled by:** ADR-062 R1 to R4 and R7, AGENTS.md section 3 (`EncinaError.Message` never carries provider text)

</details>

<details>
<summary><strong>12. Tenant tag on telemetry</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Spans `replay` and `validate` and the counter `amazoneventbridge.replay.started` (tag `outcome`) on the source and meter of #238; the tag `encina.tenant_id` only when multi-tenancy is on and a tenant is known** | Follows ADR-054 D4; no tag when tenancy is off | None |
| **B) A tenant tag always** | Uniform | Emits a tenant tag with tenancy off |

### Chosen Option: **A, tag only when tenancy is on**

### Rationale

ADR-054 D4 requires the tenant attribute and tag only when multi-tenancy is enabled.

**Class:** Settled
**Settled by:** ADR-054 D4 and #4545 Cross-Cutting row 2

</details>

---

## Implementation Phases

### Phase 1: Replay wrapper (pull requests 1 and 2)

<details>
<summary>Tasks</summary>

1. Pull request 1: `IAmazonEventBridgeReplay` and `AmazonEventBridgeReplay` (public sealed, scoped; public constructor `(IAmazonEventBridge client, IOptions<EncinaAmazonEventBridgeOptions> options, ILogger<AmazonEventBridgeReplay> logger, TimeProvider? timeProvider = null, MultiTenancyMarker? tenancy = null, IOperationAuditStore? audit = null)`): `StartReplayAsync(EventBridgeReplayRequest, CancellationToken)` returning `ValueTask<Either<EncinaError, EventBridgeReplayStatus>>`, `GetReplayAsync(string replayName, CancellationToken)`, `CancelReplayAsync(string replayName, CancellationToken)`. `EventBridgeReplayRequest` (record: `ReplayName`, `ArchiveArn`, `DestinationBusArn`, `FilterRuleArns`, `EventStartTime`, `EventEndTime`, `AllTenants`, `RequestedBy`, `CorrelationId`, `Description`) with `Validate()` and `EventBridgeReplayStatus` (the state as an enum mapped from the service strings, the reason as a code only). Gate order: validate, tenancy (D6), call. The error table of Design Choice 11 is added to `AmazonEventBridgeErrorCodes.cs`; the exception goes to `ForLogging()`. DI: `TryAddScoped<IAmazonEventBridgeReplay, AmazonEventBridgeReplay>()` with the resolution test under `ValidateOnBuild` and `ValidateScopes`. The `audit` parameter exists but pull request 1 does not use it (it is unreferenced until pull request 2, which is why the `Encina.Security.Audit` reference lands there). 10 files, by name: interface, implementation, models, error codes edit, DI edit, DI test edit, replay test, request test, guard edit, manifest.
2. Pull request 2 (audit and telemetry): the csproj gains the `Encina.Security.Audit` project reference; the implementation records the audit entry of D7 after the call and the spans and counter of Design Choice 12 (edits to #238's `AmazonEventBridgeActivitySource` and `AmazonEventBridgeMetrics`); `Log.cs` 40698 (replay requested: outcome code and `all_tenants`; no replay name or ARN). 8 files, by name: csproj edit, implementation edit, `AmazonEventBridgeActivitySource` edit, `AmazonEventBridgeMetrics` edit, `Log` edit, replay test edit, diagnostics test edit, manifest.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: The EventBridge transport (#238) exists with a scoped publisher and IAmazonEventBridge registered; StartReplay needs an archive.
TASK: Add IAmazonEventBridgeReplay with start, get and cancel, request validation, the tenancy gate, then the optional audit after the call, the telemetry and the log message, with unit and guard tests.
KEY RULES: named ADR-062 category on every EncinaErrors call; constant messages, never the AWS text; the replay name and ARNs never logged; with MultiTenancyMarker registered and no AllTenants the request is denied; the audit entry is written after the call with the real outcome; cancellation propagates; TimeProvider for every timestamp.
REFERENCE FILES: src/Encina.Messaging/DeadLetter/DeadLetterManager.cs, src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs, docs/plans for #238, docs/architecture/adr/062-error-category-contract.md.
```

</details>

### Phase 2: Schema validator (pull requests 3 and 4)

<details>
<summary>Tasks</summary>

1. Pull request 3, the pieces (14 files, by name): `Encina.AmazonEventBridge.csproj` (`AWSSDK.Schemas`, `NJsonSchema`), `Directory.Packages.props` (4.1.0.2 and 11.6.2; the restore must show no downgrade warning for `AWSSDK.Core`, as in #238 pull request 1), options edit (`ValidateAgainstSchemaRegistry` default false, `SchemaRegistryName`, `SchemaNames`, `SchemaVersions`), options validator edit (registry name required, mappings non-empty and a component name for each `OpenApi3` mapping, versions required when on), public `IEventSchemaValidator` (XML documented; a public type because it is a parameter of the public publisher constructor), internal `EventSchemaValidator` (reads the schema through `DescribeSchema`, selects the part by Design Choice 4, parses it by `Type`, validates the detail and returns property paths only), internal `SchemaCache`, error codes edit (`EVENTBRIDGE_SCHEMA_*` of Design Choices 8 and 9), DI edit, and tests: `EventSchemaValidatorTests` (an OpenAPI 3 component, a draft-4 document, a violating detail, an unmapped type, an unsupported type, a registry failure), `SchemaCacheTests`, options test edit, options validator test edit, DI test edit, manifest. The first test is the NJsonSchema prerequisite of D3.
2. DI: `IAmazonSchemas` (`TryAddSingleton`, with the endpoint rules of the EventBridge client) and `IEventSchemaValidator` are registered only when `ValidateAgainstSchemaRegistry` is on in the eagerly built options instance that `AddEncinaAmazonEventBridge` already validates (the `Encina.AmazonSQS` precedent), so a user who does not validate resolves neither; options bound later from configuration are not seen, which the documentation states.
3. Pull request 4, the wiring (7 files, by name): `AmazonEventBridgeMessagePublisher` gains the optional trailing constructor parameter `IEventSchemaValidator? validator = null` (the `new AmazonEventBridgeMessagePublisher(` sites of #238, found by search at pick-up, keep compiling); validation runs before the put, on the detail produced by the CloudEvents formatter, and a `Left` returns before any call; `Log.cs` 40699 (event refused by the schema: error code only); the `validate` span in `AmazonEventBridgeActivitySource` (edit); tests: publisher test edit, diagnostics test edit, guard edit (constructor with the nullable validator accepted), manifest.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: AWSSDK.Schemas DescribeSchema returns the schema content and its type (OpenApi3 or JSONSchemaDraft4); an OpenApi3 schema is a whole OpenAPI document; NJsonSchema validates with SchemaType.OpenApi3 or SchemaType.JsonSchema.
TASK: Add the public IEventSchemaValidator with its internal implementation and cache and the options, then wire the validator into the publisher before the put.
KEY RULES: a violating detail is always refused; the unmapped and lookup-failure cases follow the maintainer's answer to D8; error details carry property paths only, never values; a pinned version is cached for the process lifetime; cancellation propagates; the validator and IAmazonSchemas are registered only when the option is on.
REFERENCE FILES: docs/plans for #238, src/Encina.AmazonSQS/ServiceCollectionExtensions.cs, AGENTS.md section 3.
```

</details>

### Phase 3: Cross-cutting integration and observability

<details>
<summary>Tasks</summary>

1. Telemetry is delivered in pull requests 2 and 4 (Design Choice 12).
2. Verify the matrix below against the code; confirm every code has its ADR-062 category and that no log call, tag or error detail carries a replay name, an ARN, a payload value or `EncinaError.Message`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Replay and schema validation exist.
TASK: Verify the cross-cutting matrix against the code and fix any gap.
KEY RULES: AGENTS.md sections 3, 6, 7; ADR-054 D4; ADR-062 categories.
REFERENCE FILES: AGENTS.md section 6, docs/plans for #238 (Phase 4 diagnostics).
```

</details>

### Phase 4: LocalStack probe and integration tests (pull request 5)

<details>
<summary>Tasks</summary>

1. `AmazonEventBridgeReplayProbeTests` (first task): records which of `CreateArchive`, `StartReplay`, `DescribeReplay` and the Schemas API the pinned LocalStack image of `LocalStackFixture` accepts and enforces; the integration targets hold when it does, and a feature it lacks is asserted at the unit level and named in the test, never skipped silently.
2. `AmazonEventBridgeReplayIntegrationTests` and `AmazonEventBridgeSchemaIntegrationTests` in the `Cloud-LocalStack` collection (`[Trait("Category","Integration")]`, `[Trait("Transport","AmazonEventBridge")]`): a replay is started and read back; a violating event is refused and a conforming one reaches the bus. 6 files, by name: `LocalStackFixture` edit (the `schemas` service when the probe shows it), `TestInfrastructure.csproj` (`AWSSDK.Schemas`), probe, replay integration test, schema integration test, manifest (the integration targets of the replay, validator and publisher rows are entered here, where their tests land).

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

### Phase 5: Documentation and finalization (pull request 6)

<details>
<summary>Tasks</summary>

Per-file targets, entered in `Encina.AmazonEventBridge.json` by the pull request that adds the tests of the flag (a target needs its test file in the same pull request; the package's rows are created by #238):

| Pull request | File | unit | guard | contract | property | integration | Justification |
|---|---|:---:|:---:|:---:|:---:|:---:|---|
| 1, 2 | `AmazonEventBridgeReplay.cs` | 90 | 20 | 0 | 0 | 0 | A substituted client reaches every state and code; guard tests reach the constructor checks; integration is entered in pull request 5. |
| 1 | `EventBridgeReplayModels.cs` | 100 | 0 | 0 | 0 | 0 | Records and `Validate()` are pure; guard tests have no argument to check. |
| 3 | `EventSchemaValidator.cs` | 90 | 0 | 0 | 0 | 0 | An internal type: unit tests reach each schema type and failure; guard tests cannot reach it. |
| 3 | `SchemaCache.cs` | 95 | 0 | 0 | 0 | 0 | A small internal class with hit, miss and first-load tests; a pinned version has no TTL, so no clock is involved. |
| 3 | options and validator files | 100 | 0 | 0 | 0 | 0 | Pure data and validation, as #238 plans. |
| 4 | `AmazonEventBridgeMessagePublisher.cs` | 85 | 20 | 50 | 0 | 65 | The values #238 enters for the publisher (and #4543 keeps at contract 50), never lowered; this pull request adds validator cases to the unit tests. |
| 5 | `AmazonEventBridgeReplay.cs` | 90 | 20 | 0 | 0 | 40 | Real start and read on LocalStack only if the probe shows it supports replays. |
| 5 | `EventSchemaValidator.cs` | 90 | 0 | 0 | 0 | 40 | A real registry read and validation only if the probe shows LocalStack supports schemas. |
| 1 to 4 | `ServiceCollectionExtensions.cs` | 85 | 15 | 0 | 0 | 0 | The #238 row values (guard raised to the 15 floor); the DI tests add the registrations. |
| 1 to 4 | error codes, `Log.cs` | 0 | 0 | 0 | 0 | 0 | Constants and generated delegates have no executable lines. |

1. `src/Encina.AmazonEventBridge/README.md` (Replay and Schema registry sections) and the EventBridge feature page of #238's documentation pull request (or the README alone while that page is not on `main`) through `docs-writer`; `changelog.d/4545-eventbridge-schema-registry-replay.added.md` through `mechanical-fixer`. 4 files with the manifest.
2. `dotnet build --configuration Release` with zero warnings; every flag at its target; CRAP of every new method at most 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: Replay and schema validation are implemented and tested.
TASK: Hand the README sections, the feature page and the changelog fragment to the owning specialists; re-measure the per-flag targets.
KEY RULES: targets never go down; no hand-typed coverage figures in docs; the docs state that a replay re-delivers to every matching rule and that consumers deduplicate on the message id, that without an audit store a replay is not audited, and that a failed audit write after a started replay is counted and logged.
REFERENCE FILES: changelog.d/README.md, docs/plans for #238 (documentation phase).
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / Spec | Relevance | Notes |
|-----------------|-----------|-------|
| `AGENTS.md` sections 3, 5, 6, 7, 9 | Fail-closed gates, errors never swallowed, transport category, EventIds, coverage | All phases |
| ADR-062 R1 to R4, R7 | Categories of every code | Design Choices 8, 9, 11 |
| ADR-054 D4 | Tenant tag only when tenancy is on | Design Choice 12 |
| SPEC-000 section 6.1, DEC-009 | EventBridge in 1.0, tested with LocalStack | Phase 4 |
| EventBridge archive and replay, Schema registry (named product documentation) | `StartReplay` re-sends the archive range to the bus; schemas of type OpenApi3 or JSONSchemaDraft4 | Design Choices 2 to 5 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `DeadLetterManager`, `DeadLetterFilter.AllTenants` | `src/Encina.Messaging/DeadLetter/DeadLetterManager.cs` | Tenancy gate precedent |
| `MultiTenancyMarker` | `src/Encina/Abstractions/MultiTenancyMarker.cs` | Tenancy-on signal |
| `IOperationAuditStore`, `OperationAuditEntry`, `AuditOutcome` | `src/Encina.Security.Audit/Abstractions/` | Optional audit of a replay |
| `EncinaAmazonSQSOptionsValidator`, `ServiceCollectionExtensions` | `src/Encina.AmazonSQS/` | Endpoint rules and the eager options instance |
| Plan of #238 | `docs/plans/` | Package shape, fixture, diagnostics |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `AmazonEventBridge` (the range registered by #238, 40690-40699) | 40698-40699 (proposed) | 40698 replay requested, 40699 event refused by the schema; #238's plan keeps these two for its own health and faults, so they are proposed only if that plan is amended, otherwise to be assigned by the orchestrator |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 12 | replay interface and class, models, schema validator interface, class and cache, options and validator edits, publisher edit, diagnostics edits (2) |
| Tests | 14 | replay, request, validator, cache, options and validator test edits, publisher edit, diagnostics edit, probe, 2 integration, guard, DI |
| Shared edits | 7 | csproj, `Directory.Packages.props`, `TestInfrastructure.csproj`, DI, error codes, `Log`, fixture |
| Documentation and manifests | 4 | README, feature page, changelog, manifest |
| **Pull request 1** | **10** | about 600 lines |
| **Pull request 2** | **8** | about 350 lines |
| **Pull request 3** | **14** | about 800 lines |
| **Pull request 4** | **7** | about 300 lines |
| **Pull request 5** | **6** | about 350 lines |
| **Pull request 6** | **4** | about 150 lines |

Implementers and callers: `IAmazonEventBridgeReplay` and `IEventSchemaValidator` are new; the publisher constructor gains an optional trailing parameter, so the `new AmazonEventBridgeMessagePublisher(` sites of #238 keep compiling (listed by search at pick-up). Each pull request builds alone in order.

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library; no backward compatibility, zero warnings, ROP (Either<EncinaError, T>). #238 provides the EventBridge publishing transport.

IMPLEMENTATION OVERVIEW: PR1 replay wrapper with request validation and tenancy gate; PR2 audit after the call, telemetry and log 40698; PR3 schema validator, cache, options and packages; PR4 wiring into the publisher and log 40699; PR5 LocalStack probe and integration tests; PR6 documentation. Dilemmas D1 to D8 are implemented as the maintainer answers, the recommended options until then.

KEY PATTERNS: a violating detail is always refused; replay under tenancy needs AllTenants; the audit entry is written after the call; no replay name, ARN, payload value or exception message in logs; named ADR-062 category on every error; constant messages; TimeProvider; cancellation never a Left; targets never go down; CRAP <= 10.

REFERENCE FILES: docs/plans for #238, src/Encina.Messaging/DeadLetter/DeadLetterManager.cs, src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ | An internal schema cache per (registry, schema, version) (Design Choice 5); not `ICacheProvider`. |
| 2 | OpenTelemetry | ✅ | Spans `replay` and `validate`, one counter, tenant tag only when tenancy is on (Design Choice 12). |
| 3 | Structured Logging | ✅ | Two ids (Research), error code and outcome only. |
| 4 | Health Checks | ❌ | No new dependency of the bus check of #238. |
| 5 | Validation | ✅ | Schema validation is the feature; replay requests are validated locally. |
| 6 | Resilience | ⏭️ | Publisher decorators are #4393 and #742 (open); the AWS SDK retries by itself (#4393's per-provider table); replay and schema calls are not wrapped here. |
| 7 | Distributed Locks | ❌ | The service enforces a unique replay name (`ResourceAlreadyExistsException`). |
| 8 | Transactions | ❌ | None. |
| 9 | Idempotency | ❌ | Replay re-delivers by design; consumers deduplicate on the message id (inbox). |
| 10 | Multi-Tenancy | ✅ | Replay is denied without `AllTenants` when tenancy is on (D6); tenant header stamping is #4543 (open). |
| 11 | Module Isolation | ⏭️ | Module header is #4543 Phase 5. |
| 12 | Audit Trail | ✅ | A replay start is recorded through the optional `IOperationAuditStore` after the call (D7). |

---

## Prerequisites & Dependencies

- **Blocking**: #238 pull requests 1 and 2 (options, validator, publisher, DI), pull request 4 (the `ActivitySource`, `Meter` and diagnostics) for pull requests 2 and 4 here, and pull request 5 (the LocalStack fixture extension) for pull request 5 here; #238 Design Choice 5 answered A.
- **Related, not blocking**: #4543 (header contract, publisher contract target), #4393 and #742 (resilience), #260 (CloudEvents abstraction: only the formatter file changes), #1164.
- **Sibling plans**: #4544 is independent.

---

## Next Steps

1. Ask the maintainer D1 to D8 when work on #4545 starts; record the answers in `## Maintainer Decisions`.
2. Pull requests 1 and 2: replay wrapper (10), audit and telemetry (8).
3. Pull requests 3 and 4: schema validator and cache (14), wiring into the publisher (7).
4. Pull requests 5 and 6: LocalStack probe and integration tests (6), documentation (4).
