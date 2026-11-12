# Implementation Plan: `Encina.Messaging` stable persisted message type names and payload versioning

> **Issue**: [#1221](https://github.com/dlrivada/Encina/issues/1221)
> **Type**: Feature
> **Verified against**: origin/main `c49b987e9` (2026-11-12)
> **Complexity**: Medium-High (8 phases, no new schema, tests on all 10 database providers, ~70 files in 6 pull requests)
> **Estimated Scope**: ~900-1,200 lines of production code + ~1,500-1,900 lines of tests

---

## Summary

The assembly-qualified CLR type name is the persisted wire format of outbox, inbox and scheduled messages (and of dead letters and delayed retries). It is written with `typeof(T).AssemblyQualifiedName` (`OutboxOrchestrator`, `OutboxWriter`, `SchedulerOrchestrator` twice, `InboxPipelineBehavior`, `DeadLetterOrchestrator`, `DelayedRetryScheduler`, `RecoverabilityContext`) and read back with `Type.GetType(...)` (`OutboxBatchProcessor`, `SchedulerOrchestrator`, `DeadLetterManager`, `DelayedRetryProcessor` twice, `OutboxCdcHandler`). Renaming or moving a type, or changing its assembly version, strands every stored message. SPEC-002 REQ-032 and AC-032 require stable registry names with an assembly-qualified fallback, and payloads that can be versioned and upcast; the issue takes the minimal slice of #134.

What it implements:

- `[MessageType("practice.appointment-booked.v1")]`, a registry of stable names built at start-up from the assemblies the application lists (opt-in), with a startup failure on a duplicate or malformed name.
- One writer (`IMessageTypeNames.GetName(Type)`) used by every write site: a registered type is stored under its registry name, an unregistered one under its assembly-qualified name; the warning and the fallback counter appear only once an application has configured a registry (Design Choice 7, a Dilemma).
- One reader (`IMessagePayloadReader.Read(storedName, content)`, synchronous: pure CPU) used by every read site: it resolves the stored name (registry first, `Type.GetType` fallback for rows written before adoption), deserializes into the stored version's type and upcasts through `IMessageUpcaster<TFrom, TTo>` to the current type.
- The version is part of the name (`...v1`, `...v2`); the existing type columns (`NotificationType`, `RequestType`, `NVARCHAR(500)`) hold it, so no schema change is needed (SPEC-002 section 13: "registry names and versions in the type columns").

Standards: SPEC-002 REQ-032, AC-032, P-19; ADR-001 (ROP); ADR-021 (EventIds); #134 (message versioning, minimal slice here, remainder stays open).

Estimated scope: about 70 files (core 18, call-site edits 13, tests 28, docs and metadata 11), shipped as 6 pull requests. Affected packages: `Encina` (the attribute), `Encina.Messaging` (registry, resolver, upcasting, call sites), `Encina.Cdc` (one read site). `Encina.Messaging.Encryption` is not affected: its `MessageEncryptionContext.MessageType` (`EncryptingMessageSerializer.cs` line 292) is not a persisted type name (verified: it is read only by `DataProtectionMessageEncryptionProvider` for log text and error construction, never stored in the envelope), so it stays unchanged.

Provider category: Database (10) by test only. The type columns already exist in every provider script and store, so there is no provider-specific code; AGENTS.md section 5 is met by running the round-trip scenario on all 10 providers.

---

## Design Choices

<details>
<summary><strong>1. Where the version lives — inside the registry name</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The version is the `.vN` suffix of the name, stored in the existing type column** | No schema change on 10 providers; the name alone identifies the payload shape; matches the REQ-032 example | Name and version cannot be queried separately without parsing |
| **B) A new `PayloadVersion` column next to the name** | Queryable | Schema change in 6 SQL scripts, EF configuration and the Mongo documents; two fields that must agree |

### Chosen Option: **A — Version inside the name**

### Rationale

- SPEC-002 section 13 describes REQ-032's store impact as "registry names and versions in the type columns", and the issue's own example is `name.vN`. Option B (a `PayloadVersion` column) would also satisfy the maintainer's "version stored with the row"; Option A is chosen for a technical reason: no schema change on 10 providers (pay-for-what-you-use) and one field that cannot disagree with the name.
- **Class:** Trivial

</details>

<details>
<summary><strong>2. Registry source, name format and failure modes</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Attribute scan of the assemblies passed to `AddEncinaMessageTypes(params Assembly[])`; name pattern `<segment>(.<segment>)*.v<N>`; duplicate or malformed names fail at start-up** | Opt-in; bad names fail early; the assembly-list registration shape is the convention of the validation packages | Reflection at start-up (once) |
| **B) Explicit `Register<T>("name")` calls only** | No reflection | Boilerplate; names drift from the type |
| **C) Source generator** | No reflection | A new generator project for a start-up scan |

### Chosen Option: **A — Opt-in assembly scan with validation**

### Rationale

- Unregistered types fall back to the assembly-qualified name, which keeps the registry opt-in (pay-for-what-you-use), as the maintainer's comment on the issue states; explicit registration is also exposed for types the scan cannot reach.
- Registered decisions (public behavior): the name pattern is `^[a-z][a-z0-9-]*(\.[a-z][a-z0-9-]*)*\.v[1-9][0-9]*$` (lower-case dot-separated segments and a `.vN` version suffix, N without leading zeros), at most 200 characters (the type columns are `NVARCHAR(500)` and the assembly-qualified fallback is longer); the name is validated by the options validator (`IValidateOptions<MessageTypeRegistryOptions>` with `ValidateOnStart`, which throws `OptionsValidationException` at host start), not on a rule path; the registry's runtime failures are `Left`.
- **Class:** Trivial
- **Settled by:** AGENTS.md section 3 (every messaging feature is optional) and #1221 (maintainer comment of 2026-10-17)

</details>

<details>
<summary><strong>3. Placement of the attribute — core package or messaging package</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `MessageTypeAttribute` in `Encina` (core), next to `INotification` and `IRequest`** | Domain assemblies that define notifications already reference the core, not `Encina.Messaging`; no new dependency for them | One more public type in the core package |
| **B) `MessageTypeAttribute` in `Encina.Messaging`** | The core stays free of persistence concerns | Domain assemblies must reference the messaging package only to name a type |

### Chosen Option: **A — In the core package (recommended, pending the maintainer)**

### Rationale

- B wins if the maintainer wants the core free of persistence vocabulary; A wins for applications whose message types live in a domain assembly that references only the core. The registry, resolver and upcasting stay in `Encina.Messaging` either way.
- **Class:** Dilemma

</details>

<details>
<summary><strong>4. Upcasting — chained through registered steps, or a direct upcaster per old version</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Compose registered `IMessageUpcaster<TFrom,TTo>` steps (v1 to v2 to v3) until the current type** | Each version adds one small upcaster; old payloads keep working | Resolution of the chain at start-up; a missing link is a startup error |
| **B) Require a direct upcaster from every old version to the current one** | Simple resolution | N upcasters to maintain per version (quadratic growth) |

### Chosen Option: **A — Chained upcasters (recommended, pending the maintainer)**

### Rationale

- B wins for a message type with one or two old versions where the chain adds no value; A wins as soon as a third version exists. The registry validates the chains at start-up so a gap fails fast, not on the first old row.
- **Class:** Dilemma

</details>

<details>
<summary><strong>5. Which persisted type names are covered — the three named patterns or every persisted type name</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Every writer and reader of a persisted type name: outbox, inbox, scheduled, dead letter, delayed retry and the CDC outbox handler** | One wire format; a renamed type does not strand dead letters or delayed retries | Six more call sites (two small edits each) |
| **B) Only outbox, inbox and scheduled messages, as the issue lists** | Smaller diff | Dead-letter replay and delayed retry of a renamed type still break, and a registered type is stored by two different conventions |

### Chosen Option: **A — Every persisted type name (recommended, pending the maintainer)**

### Rationale

- B wins if the maintainer wants this issue limited to its three named patterns and a separate issue for recoverability; A wins because the writer and the reader are single shared services, so the extra sites are one-line calls and leaving them out makes the wire format inconsistent.
- **Class:** Dilemma

</details>

<details>
<summary><strong>6. Rows written before adoption</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The reader falls back to `Type.GetType` for a stored name the registry does not know** | Existing rows keep working; no migration | The fallback stays as a permanent path (it is the documented fallback anyway) |
| **B) A migration tool rewriting stored names** | One format | Migration helper, forbidden pre-1.0 (AGENTS.md section 3); rows are short-lived |

### Chosen Option: **A — Fallback reader, no migration**

### Rationale

- The assembly-qualified fallback is part of REQ-032 itself, so the same reader path serves unregistered types and old rows.
- **Class:** Settled
- **Settled by:** SPEC-002 REQ-032 (assembly-qualified fallback) and AGENTS.md section 3 (no migration helpers or compatibility layers)

</details>

<details>
<summary><strong>7. Behavior of an application that does not configure a registry</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) With no registry configured the writer and the reader are pass-through: the same name written and the same `Type.GetType` read as today; no warning, no fallback counter** | An application that never opts in sees no new log line, metric or error path | The fallback warning and counter exist only for applications that adopted the registry |
| **B) Always warn once per unregistered type and count every fallback write** | Surfaces types that could be registered | A new default log and metric for every user of outbox, inbox and scheduling who did not ask for the feature |

### Chosen Option: **A — Pass-through until a registry is configured (recommended, pending the maintainer)**

### Rationale

- Every messaging feature is opt-in (AGENTS.md section 3); B changes the default observable behavior of every write site. B wins if the maintainer wants the warning as an adoption nudge. Under A the sentence "no behavior change for applications that do not opt in" is true of logs, counters and errors; the only new thing on the default path is one extra service call per write and read.
- **Class:** Dilemma

</details>

<details>
<summary><strong>8. A stored name the configured registry does not know</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Fall back to `Type.GetType(storedName)` as today, also when a registry is configured** | Rows written before adoption keep working; no new refusal path | An attacker-controlled stored name still reaches `Type.GetType` (existing behavior; the registry-first lookup does not widen it) |
| **B) An option `RequireRegisteredNames` (default `false`): when `true` and a registry is configured, an unknown stored name returns `Left(message_type.unknown)` and the fallback is never used** | Fail closed for applications that migrated every row | One more option; old rows must be gone before it is turned on |

### Chosen Option: **A — Fallback always, B available as an option (recommended, pending the maintainer)**

### Rationale

- The assembly-qualified fallback is part of REQ-032 (Design Choice 6). A compliance-minded deployment may want B once its rows are migrated (AGENTS.md section 3, gates fail closed); the choice of whether B is shipped in this issue is the maintainer's.
- **Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Core Models — attribute, registry contracts, upcaster

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina/Abstractions/MessageTypeAttribute.cs` (new): `[AttributeUsage(Class | Struct, Inherited = false)] public sealed class MessageTypeAttribute(string name)` with the validated name.
2. `src/Encina.Messaging/TypeNames/IMessageTypeNames.cs` (new): `string GetName(Type type)`.
3. `src/Encina.Messaging/TypeNames/IMessagePayloadReader.cs` (new): `Either<EncinaError, object> Read(string storedName, string content)` (synchronous, pure CPU).
4. `src/Encina.Messaging/TypeNames/IMessageUpcaster.cs` (new): `IMessageUpcaster<TFrom, TTo>` with `Either<EncinaError, TTo> Upcast(TFrom message)`.
5. `src/Encina.Messaging/TypeNames/MessageTypeErrorCodes.cs` (new): each code created with an `ErrorCategory` (ADR-062, `EncinaErrors.Create(code, ErrorCategory, message, ...)`): `message_type.unknown` `NotFound`, `message_type.deserialization_failed` `Validation`, `message_type.upcast_failed` `Internal`, `message_type.duplicate_name` `Validation`, `message_type.invalid_name` `Validation`, `message_type.upcast_gap` `Validation`; a unit test asserts `GetCategory()` for each. The call sites map the new codes to their existing codes (Phase 4) and keep the category those codes carry today, pinned by a test per site.
6. `PublicAPI.Unshipped.txt` of `Encina` and `Encina.Messaging` (via `mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Encina pre-1.0, .NET 10, no compatibility layers. Issue #1221 gives persisted message types stable
registry names. The type columns of the outbox, inbox and scheduled tables are NVARCHAR(500) and already exist.
TASK: Create MessageTypeAttribute, IMessageTypeNames, IMessagePayloadReader, IMessageUpcaster<TFrom,TTo> and the
error code constants.
KEY RULES: Either<EncinaError,T> for operations that can fail; XML docs with examples; PublicAPI.Unshipped.txt;
error text never contains payloads or personal data.
REFERENCE FILES: src/Encina/Abstractions/INotification.cs, src/Encina.Messaging/Serialization/IMessageSerializer.cs,
src/Encina.Messaging/Outbox/OutboxOrchestrator.cs (OutboxErrorCodes)
```

</details>

### Phase 2: Default Implementation — registry, reader, upcast chains

<details>
<summary><strong>Tasks</strong></summary>

1. `MessageTypeRegistry` (internal): name to type and type to name maps; construction validates the pattern, duplicates and upcast chains (a chain from every registered old version to the current one, no cycles, `message_type.upcast_gap` otherwise).
2. `MessageTypeNames`: registry name when registered, otherwise `type.AssemblyQualifiedName`; with a registry configured, a one-time warning per type and the fallback counter (`MessageTypeLog`, Design Choice 7).
3. `MessagePayloadReader`: resolves the stored name (registry, then `Type.GetType` fallback; with `RequireRegisteredNames` on, if Design Choice 8 B is shipped, no fallback), deserializes through `IMessageSerializer.Deserialize(content, storedType)`, applies the upcast chain, returns `Left(message_type.unknown | deserialization_failed | upcast_failed)`. The reader is synchronous and takes no `CancellationToken`, so it has no cancellation to handle; a serializer exception becomes a `Left` carrying the exception type only (never its message).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: Phase 1 created the contracts. Reads today use Type.GetType(stored name) and fail with
unknown_notification_type / unknown_request_type when it returns null.
TASK: Implement the registry with start-up validation, the name writer with fallback and warning, and the payload
reader with upcast chaining.
KEY RULES: a Left is never swallowed; no payload or message text in error codes or logs; reflection only at
start-up (cache maps); CRAP <= 10 per method (split validation steps).
REFERENCE FILES: src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs (ProcessMessageAsync), src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs,
src/Encina.Messaging/Serialization/IMessageSerializer.cs
```

</details>

### Phase 3: Configuration, DI & Registration

<details>
<summary><strong>Tasks</strong></summary>

1. `AddEncinaMessageTypes(this IServiceCollection, params Assembly[] assemblies)` and a `MessagingConfiguration` helper `config.MessageTypes.AddAssembly(assembly)` that feed one `MessageTypeRegistryOptions`; registers `IMessageTypeNames`, `IMessagePayloadReader`, the registry and every `IMessageUpcaster<,>` found in the assemblies, all with `TryAdd*`.
2. When the registry is not configured, `IMessageTypeNames` and the reader are still registered with an empty registry in pass-through mode so every write and read site works through the fallback; under Design Choice 7 A that mode logs and counts nothing, so the observable behavior of an application that does not opt in is unchanged.
3. DI test with `ValidateOnBuild` and `ValidateScopes`, configured and unconfigured, in both registration orders.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: AGENTS.md section 3, registration completeness; messaging patterns are optional and the registry is opt-in.
TASK: Add the registration extension, wire it into AddMessagingServices with an empty registry by default, and
test the container.
KEY RULES: TryAdd*; startup validation is an options validator (IValidateOptions with ValidateOnStart) that
fails the host start listing the offending names (names and codes, not payloads), not a rule path; the
application's own registration wins.
REFERENCE FILES: src/Encina.Messaging/MessagingServiceCollectionExtensions.cs, src/Encina.Messaging/MessagingConfiguration.cs
```

</details>

### Phase 4: Call Sites — writers and readers

<details>
<summary><strong>Tasks</strong></summary>

Writers (replace `typeof(T).AssemblyQualifiedName ?? ...` with `IMessageTypeNames.GetName`): `OutboxOrchestrator.cs:92`, `OutboxWriter.cs:56`, `SchedulerOrchestrator.cs:127` and `:212`, `InboxPipelineBehavior.cs:94`, `DeadLetterOrchestrator.cs:143`, `DelayedRetryScheduler.cs:74`, `RecoverabilityContext.cs:207`.

Readers (replace `Type.GetType` plus `Deserialize` with `IMessagePayloadReader.Read`): `OutboxBatchProcessor.cs:173`, `SchedulerOrchestrator.cs:439`, `DeadLetterManager.cs:354`, `DelayedRetryProcessor.cs:199` and `:482`, `Encina.Cdc/Messaging/OutboxCdcHandler.cs:124`. The unknown-type and deserialization failures keep their current error codes mapped from the new ones, so retry and dead-letter behavior is unchanged.

Constructors gain the new services; the registration of the processors is updated; unit tests for each site.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT: The 7 writers and 6 readers listed in the Tasks block are the complete set of persisted type-name uses
found with grep on 2026-11-11; re-run the search (AssemblyQualifiedName, Type.GetType) before editing.
TASK: Route every writer through IMessageTypeNames and every reader through IMessagePayloadReader.
KEY RULES: the failure paths stay Left and keep their retry/dead-letter semantics; no behavior change when the
registry is empty; constructor changes updated in DI and tests; one pull request per group.
REFERENCE FILES: src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs, src/Encina.Messaging/Recoverability/DelayedRetryProcessor.cs,
src/Encina.Cdc/Messaging/OutboxCdcHandler.cs, src/Encina.Messaging/DeadLetter/DeadLetterManager.cs
```

</details>

### Phase 5: Cross-Cutting Integration

<details>
<summary><strong>Tasks</strong></summary>

1. Multi-tenancy (function 10): the registry is global and carries no tenant data, and the reader has no tenant input; the tenant-aware requirement of the issue (REQ-061, AC-043) is met by the existing tenant column of the stores, which this feature neither reads nor writes. Test: after the outbox and scheduler read paths resolve a stored name through the reader, the row's `TenantId` is unchanged and was not consulted (a store double that records access to the tenant column).
2. Validation: duplicate or malformed names and upcast gaps fail at start-up (Phase 2).
3. Deferred rows keep their reason in the matrix.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT: SPEC-002 REQ-061 and AC-043: every record this issue adds or changes carries the tenant id; the registry
adds no record.
TASK: Add the tenant-column-untouched test and confirm the validation paths are covered.
KEY RULES: AGENTS.md section 6; no scope growth.
REFERENCE FILES: AGENTS.md section 6, docs/specifications (REQ-061, AC-043)
```

</details>

### Phase 6: Observability

<details>
<summary><strong>Tasks</strong></summary>

1. Counters on the `Encina` meter: `encina.messaging.type_name.fallback_total` (writes that used the assembly-qualified fallback) and `encina.messaging.payload.upcast_total` (tags `message_type`, bounded by registered names), with the `tenant` tag **only when multi-tenancy is enabled and an ambient tenant resolves (ADR-054 D4)**; the read sites (processors) run without a request context, so no tenant tag is emitted there, and none is emitted at all when multi-tenancy is off. Counters are emitted only once a registry is configured (Design Choice 7).
2. `MessageTypeLog` (`[LoggerMessage]`) with EventIds 5811-5816 inside the family block 5800-5849 (`EventIdRanges.MessagingInboxIdempotency`, registered by plan #3440 or by the first plan of the family to land), packed after #1220's 5805-5810: 5811 fallback warning (once per type), 5812 unknown stored name, 5813 upcast applied (Debug), 5814 upcast failed, 5815 registry built, 5816 registry validation failed.
3. No payload or message text in tags or logs (AC-044 test with an in-memory exporter).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT: ADR-021; the family block 5800-5849 is registered once. The family packs ids in the order #3440
(5800-5804), #1220 (5805-5810), this plan (5811-5816), #2385 (5817-5822), with no gaps.
TASK: Add the counters and MessageTypeLog; re-check on the branch that 5811-5816 are free.
KEY RULES: bounded tag dimensions; EncinaError.Message never in logs; exceptions through ForLogging(); packed ids.
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, src/Encina.Messaging/Diagnostics/MessagePurgeMetrics.cs
```

</details>

### Phase 7: Testing — 6 Test Types

<details>
<summary><strong>Tasks</strong></summary>

1. Unit: registry validation (duplicate, malformed, gap, cycle), writer fallback and warning once, reader (registered, renamed CLR type with the same name still resolves, old row via fallback, upcast v1 to v2 to v3, unknown name), each call site.
2. Guard: every new public member.
3. Contract: `IMessagePayloadReader` and `IMessageTypeNames` behave the same when used by outbox, scheduler, dead letter and delayed retry harnesses.
4. Property: for any chain of upcasters, reading a v-k payload yields the current type; names round-trip through the registry.
5. Integration: all 10 providers through the shared `[Collection]` fixtures: an outbox message and a scheduled message written under a registry name, read after the CLR type was replaced by a renamed one with the same name (AC-032), and a v1 payload upcast to v2.
6. Load and Benchmark: `.md` justifications (one dictionary lookup per message).
7. Manifest targets per new or touched `src/` file (floors for the worker, never lowered; `.github/coverage-manifest/{Package}.json`):

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|-----:|------:|---------:|---------:|------------:|---------------|
| `TypeNames/MessageTypeRegistry.cs` | 95 | 100 | 0 | 90 | 0 | Pure construction and validation logic; every rejection (duplicate, malformed, gap, cycle) is a unit case and the name round trip is a property. |
| `TypeNames/MessageTypeNames.cs` | 95 | 100 | 100 | 0 | 0 | Small writer; pass-through, registered and fallback paths are all unit-testable, and the contract suite pins the shared behavior. |
| `TypeNames/MessagePayloadReader.cs` | 95 | 100 | 100 | 90 | 90 | Reader with upcast chains: chain composition is a property, and the integration scenario proves it on a real row. |
| `Encina/Abstractions/MessageTypeAttribute.cs` | 100 | 100 | 0 | 0 | 0 | One constructor with validation. |
| Call-site files (13) | 90 | 100 | 0 | 0 | 90 | Each edit is a one-line service call; the failure mapping is unit-tested per site and the round trip is integration-tested on 10 providers. |

CRAP table of every changed method before each pull request.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT: Per-flag coverage; tests run real package code; Shouldly via Encina.Testing.Shouldly; shared [Collection]
fixtures with ClearAllDataAsync; FsCheck via Encina.Testing.FsCheck.
TASK: Implement the listed tests and the two justification files; set manifest targets.
KEY RULES: no Thread.Sleep; all 10 providers in the integration scenario; targets never go down.
REFERENCE FILES: tests/Encina.IntegrationTests/Messaging/InboxPipelineHarnesses.cs, tests/Encina.UnitTests/Messaging,
.github/coverage-manifest/Encina.Messaging.json
```

</details>

### Phase 8: Documentation & Finalization

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs with examples; `changelog.d/1221-stable-message-type-names.added.md` (via `mechanical-fixer`).
2. A guide page `docs/messaging/message-types.md` and a link from `docs/messaging/index.md` (by `docs-writer`): naming rules, registration, upcasters, fallback and warning.
3. ADR for the decided Dilemmas (3, 4, 5, 7 and 8) once decided (ADR number assigned when the ADR pull request opens); `docs/INVENTORY.md`; PublicAPI files.
4. Build 0 warnings; tests at manifest targets.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```text
CONTEXT: Documentation follows the encina-docs skill; coverage figures are cited, never typed.
TASK: Finish XML docs, changelog, the message-types guide, ADR and INVENTORY.
KEY RULES: English only; do not name a private client application (write "the reference application" if needed);
PublicAPI files complete.
REFERENCE FILES: docs/messaging/index.md, .claude/skills/encina-docs/SKILL.md, changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Requirement | How this feature meets it |
|----------|-------------|---------------------------|
| SPEC-002 REQ-032 / AC-032 | Stable names with assembly-qualified fallback; versioning and upcasting | Phases 1-4 |
| SPEC-002 REQ-061 / AC-043 | Tenant-aware records | No new record; tenant-column-untouched test (Phase 5) |
| SPEC-002 REQ-062 / AC-044 | Instrumented, no payloads | Phase 6 |
| #134 | Message versioning and upcasting | Minimal slice only; the remainder stays open |
| ADR-021 | Registered EventId ranges | Family block 5800-5849 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| OutboxOrchestrator / OutboxWriter | src/Encina.Messaging/Outbox/ | writers of NotificationType |
| OutboxBatchProcessor | src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs | reader of NotificationType |
| SchedulerOrchestrator | src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs | writer (twice) and reader of RequestType |
| InboxPipelineBehavior | src/Encina.Messaging/Inbox/InboxPipelineBehavior.cs | writer of the inbox RequestType (informational) |
| DeadLetterOrchestrator / DeadLetterManager | src/Encina.Messaging/DeadLetter/ | writer and reader of RequestType |
| DelayedRetryScheduler / DelayedRetryProcessor / RecoverabilityContext | src/Encina.Messaging/Recoverability/ | writers and readers |
| OutboxCdcHandler | src/Encina.Cdc/Messaging/OutboxCdcHandler.cs | reader of the outbox type name |
| IMessageSerializer | src/Encina.Messaging/Serialization/IMessageSerializer.cs | deserializes into the stored version's type |
| Type columns | src/Encina.{ADO,Dapper}.*/Scripts/001, 002, 004 (NVARCHAR(500)) | hold the registry name; no schema change |
| AddEncinaFluentValidation(assembly) | src/Encina.FluentValidation | registration convention for the assembly scan |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` | 5811-5816 inside `MessagingInboxIdempotency` (5800-5849) | Family block assigned 2026-11-11; packed order #3440 (5800-5804), #1220 (5805-5810), this plan (5811-5816), #2385 (5817-5822), no gaps; the first plan of the family to land registers the block |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Core abstractions and implementation | 18 | attribute, 3 contracts, error codes, registry, options and validator, names, reader, registration, metrics, log, config helper, upcast chain, EventIdRanges edit |
| Call-site edits | 13 | 7 writers and 6 readers |
| Tests | 28 | unit, guard, contract, property, integration scenario, DI tests, 2 justification files, EventId map, existing tests that construct the changed classes (counted at pick-up) |
| Documentation and metadata | 11 | changelog, guide, ADR, INVENTORY, manifests (Encina, Messaging, Cdc), PublicAPI (Encina, Messaging, Cdc) |
| **Total** | **70** | Six pull requests |

### Pull request split

Counts include tests, coverage manifests, `PublicAPI.Unshipped.txt` and the existing tests that change with each constructor. Each pull request builds alone.

| PR | Content | Files (approx.) |
|----|---------|-----------------|
| A1 | Contracts and registry: attribute, three contracts, error codes, options and validator, registry; unit, guard and property tests; PublicAPI of Encina and Messaging; 2 manifests | 14 |
| A2 | Names and reader: `MessageTypeNames`, `MessagePayloadReader`, upcast chain, registration extension, config helper, metrics, `MessageTypeLog`, `EventIdRanges` and `EncinaEventIdAllocationTests` edits; unit and DI tests; PublicAPI; manifest | 14 |
| B1 | The 7 writers, their DI registration updates and unit tests (existing tests that construct them included), PublicAPI, manifest | 14 |
| B2 | The 6 readers (5 in Messaging, `OutboxCdcHandler` in Cdc), unit tests, PublicAPI and manifests of both packages | 14 |
| C | Contract tests and the 10-provider integration scenario (the shared `[Collection]` fixtures), 2 justification files | 8 |
| D | Documentation: guide page, index link, ADR, INVENTORY, changelog | 6 |

A1 first; A2 depends on A1; B1 and B2 depend on A2 and may land in either order; C depends on B1 and B2; D last.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT:
Encina is pre-1.0, .NET 10 / C# 14, nullable enabled, no backward compatibility. Issue #1221 (SPEC-002 REQ-032,
AC-032): persisted message types use stable registry names with an assembly-qualified fallback, and payloads can
be upcast. The type columns already exist (NVARCHAR(500)); no schema change; the version is the .vN suffix.

IMPLEMENTATION OVERVIEW:
1. MessageTypeAttribute (core), IMessageTypeNames, IMessagePayloadReader, IMessageUpcaster<TFrom,TTo>, codes.
2. Registry with start-up validation, writer with fallback and warning, reader with chained upcasting.
3. AddEncinaMessageTypes(assemblies) with an empty default registry; DI tests.
4. 7 writers and 6 readers routed through the two services; failure semantics unchanged.
5. Counters and MessageTypeLog EventIds 5811-5816 in the family block 5800-5849.
6. Tests on all flags with the 10-provider integration matrix; docs, ADR and changelog.

KEY PATTERNS:
- ROP; reflection only at start-up; no payloads, message text or personal data in codes, logs or tags.
- Opt-in registry; with no registry configured the path behaves exactly as today (Design Choice 7, pending).
- Registered EventId range first; bounded tag dimensions; CRAP <= 10; targets never go down; PRs of at most 15 files.

REFERENCE FILES:
docs/plans/stable-message-type-names-implementation-plan-1221.md, src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs,
src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs, src/Encina.Messaging/Recoverability/DelayedRetryProcessor.cs,
src/Encina.Messaging/DeadLetter/DeadLetterManager.cs, src/Encina.Cdc/Messaging/OutboxCdcHandler.cs,
src/Encina.Messaging/Serialization/IMessageSerializer.cs, src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | The registry is built once at start-up; there is no read path to cache |
| 2 | OpenTelemetry | ✅ | Fallback and upcast counters with bounded tags, tenant tag only when multi-tenancy is on and a tenant resolves (ADR-054 D4, Phase 6) |
| 3 | Structured Logging | ✅ | `MessageTypeLog` EventIds 5811-5816 in the family block (Phase 6) |
| 4 | Health Checks | ❌ | No new checkable dependency; invalid registry fails at start-up |
| 5 | Validation | ✅ | Duplicate names, malformed names and upcast gaps fail at start-up (Phase 2) |
| 6 | Resilience | ❌ | No call to an external system |
| 7 | Distributed Locks | ❌ | No shared mutable state: the registry is immutable after start-up |
| 8 | Transactions | ❌ | Single-operation reads and writes of an existing column |
| 9 | Idempotency | ❌ | Not a message or request entry point |
| 10 | Multi-Tenancy | ✅ | Registry global and tenant-free; the stores' tenant column is untouched by the reader, proven by a test (Phase 5) |
| 11 | Module Isolation | ❌ | SPEC-002 requires no module scoping; `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ❌ | No compliance-relevant operation beyond those already audited |

---

## Prerequisites & Dependencies

| Issue | Relation | Notes |
|-------|----------|-------|
| [#134](https://github.com/dlrivada/Encina/issues/134) | Remainder stays open | Full message versioning; this plan is its minimal slice |
| [#1220](https://github.com/dlrivada/Encina/issues/1220), [#2385](https://github.com/dlrivada/Encina/issues/2385), [#1251](https://github.com/dlrivada/Encina/issues/1251) | Share `OutboxProcessorBase` and `OutboxBatchProcessor` | All edit the outbox processing classes (#1220 passes the idempotency context, #2385 routes integration events to a transport and reads the stored name through the same reader, #1251 changes row claiming); the later pull request rebases |
| [#3440](https://github.com/dlrivada/Encina/issues/3440) | Shares the EventId block | Family block 5800-5849 (packed order in Event ID Allocation) |
| [#1186](https://github.com/dlrivada/Encina/issues/1186) | Parent EPIC | SPEC-002 readiness |

---

## Next Steps

1. At pick-up, re-check the Design Choices against today's code, ask the maintainer Dilemmas 3, 4, 5, 7 and 8 and record the answers in `## Maintainer Decisions`; run `check-plan.ps1` without `-Draft`.
2. Re-run the search for `AssemblyQualifiedName` and `Type.GetType` to refresh the call-site list before PR B1.
3. Re-check that 5811-5816 are free and the family block is registered before PR A2.
4. Measure per-flag coverage and the CRAP table for every touched file before each pull request.
