# Implementation Plan: `Encina.Messaging` stable persisted message type names and payload versioning

> **Issue**: [#1221](https://github.com/dlrivada/Encina/issues/1221)
> **Type**: Feature
> **Complexity**: Medium-High (8 phases, no new schema, tests on all 10 database providers, ~45 files in 3 pull requests)
> **Estimated Scope**: ~900-1,200 lines of production code + ~1,500-1,900 lines of tests

---

## Summary

The assembly-qualified CLR type name is the persisted wire format of outbox, inbox and scheduled messages (and of dead letters and delayed retries). It is written with `typeof(T).AssemblyQualifiedName` (`OutboxOrchestrator`, `OutboxWriter`, `SchedulerOrchestrator` twice, `InboxPipelineBehavior`, `DeadLetterOrchestrator`, `DelayedRetryScheduler`, `RecoverabilityContext`) and read back with `Type.GetType(...)` (`OutboxBatchProcessor`, `SchedulerOrchestrator`, `DeadLetterManager`, `DelayedRetryProcessor` twice, `OutboxCdcHandler`). Renaming or moving a type, or changing its assembly version, strands every stored message. SPEC-002 REQ-032 and AC-032 require stable registry names with an assembly-qualified fallback, and payloads that can be versioned and upcast; the issue takes the minimal slice of #134.

What it implements:

- `[MessageType("practice.appointment-booked.v1")]`, a registry of stable names built at start-up from the assemblies the application lists (opt-in), with a startup failure on a duplicate or malformed name.
- One writer (`IMessageTypeNames.GetName(Type)`) used by every write site: a registered type is stored under its registry name, an unregistered one under its assembly-qualified name with a logged warning (the registry stays opt-in).
- One reader (`IMessagePayloadReader.ReadAsync(storedName, content)`) used by every read site: it resolves the stored name (registry first, `Type.GetType` fallback for rows written before adoption), deserializes into the stored version's type and upcasts through `IMessageUpcaster<TFrom, TTo>` to the current type.
- The version is part of the name (`...v1`, `...v2`); the existing type columns (`NotificationType`, `RequestType`, `NVARCHAR(500)`) hold it, so no schema change is needed (SPEC-002 section 13: "registry names and versions in the type columns").

Standards: SPEC-002 REQ-032, AC-032, P-19; ADR-001 (ROP); ADR-021 (EventIds); #134 (message versioning, minimal slice here, remainder stays open).

Estimated scope: about 45 files (core 16, call-site edits 13, tests 11, docs and metadata 5), shipped as 3 pull requests. Affected packages: `Encina` (the attribute), `Encina.Messaging` (registry, resolver, upcasting, call sites), `Encina.Cdc` (one read site), `Encina.Messaging.Encryption` (reads `AssemblyQualifiedName` at one place, reviewed in Phase 2).

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

- SPEC-002 section 13 describes REQ-032's store impact as "registry names and versions in the type columns", and the issue's own example is `name.vN`.
- **Class:** Settled
- **Settled by:** SPEC-002 REQ-032 (and its section 13 note on type columns) and #1221 (maintainer comment of 2026-10-17: implement as written)

</details>

<details>
<summary><strong>2. Registry source, name format and failure modes</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Attribute scan of the assemblies passed to `AddEncinaMessageTypes(params Assembly[])`; name pattern `<segment>(.<segment>)*.v<N>`; duplicate or malformed names fail at start-up** | Same shape as `AddEncinaFluentValidation(assembly)`; opt-in; bad names fail early | Reflection at start-up (once) |
| **B) Explicit `Register<T>("name")` calls only** | No reflection | Boilerplate; names drift from the type |
| **C) Source generator** | No reflection | A new generator project for a start-up scan |

### Chosen Option: **A — Opt-in assembly scan with validation**

### Rationale

- Unregistered types fall back to the assembly-qualified name with a logged warning, which keeps the registry opt-in (pay-for-what-you-use), as the maintainer's comment on the issue states; explicit registration is also exposed for types the scan cannot reach.
- **Class:** Trivial
- **Settled by:** AGENTS.md section 3 (Validation uses `AddEncinaFluentValidation(assembly)` style registration; every messaging feature is optional) and #1221 (maintainer comment of 2026-10-17)

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

---

## Implementation Phases

### Phase 1: Core Models — attribute, registry contracts, upcaster

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina/Abstractions/MessageTypeAttribute.cs` (new): `[AttributeUsage(Class | Struct, Inherited = false)] public sealed class MessageTypeAttribute(string name)` with the validated name.
2. `src/Encina.Messaging/TypeNames/IMessageTypeNames.cs` (new): `string GetName(Type type)`.
3. `src/Encina.Messaging/TypeNames/IMessagePayloadReader.cs` (new): `Either<EncinaError, object> Read(string storedName, string content)` (synchronous, pure CPU).
4. `src/Encina.Messaging/TypeNames/IMessageUpcaster.cs` (new): `IMessageUpcaster<TFrom, TTo>` with `Either<EncinaError, TTo> Upcast(TFrom message)`.
5. `src/Encina.Messaging/TypeNames/MessageTypeErrorCodes.cs` (new): `message_type.unknown`, `message_type.deserialization_failed`, `message_type.upcast_failed`, `message_type.duplicate_name`, `message_type.invalid_name`, `message_type.upcast_gap`.
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
2. `MessageTypeNames`: registry name when registered, otherwise `type.AssemblyQualifiedName` plus a one-time warning per type (`MessageTypeLog`).
3. `MessagePayloadReader`: resolves the stored name (registry, then `Type.GetType` fallback), deserializes through `IMessageSerializer.Deserialize(content, storedType)`, applies the upcast chain, returns `Left(message_type.unknown | deserialization_failed | upcast_failed)`. Cancellation and exceptions are caught per call and turned into `Left` with the exception type only.
4. Review `Encina.Messaging.Encryption/Serialization/EncryptingMessageSerializer.cs:292` (an `AssemblyQualifiedName` use) and keep it compatible.

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
src/Encina.Messaging.Encryption/Serialization/EncryptingMessageSerializer.cs
```

</details>

### Phase 3: Configuration, DI & Registration

<details>
<summary><strong>Tasks</strong></summary>

1. `AddEncinaMessageTypes(this IServiceCollection, params Assembly[] assemblies)` and a `MessagingConfiguration` helper `config.MessageTypes.AddAssembly(assembly)` that feed one `MessageTypeRegistryOptions`; registers `IMessageTypeNames`, `IMessagePayloadReader`, the registry and every `IMessageUpcaster<,>` found in the assemblies, all with `TryAdd*`.
2. When the registry is not configured, `IMessageTypeNames` and the reader are still registered with an empty registry so every write and read site works through the fallback (no behavior change for applications that do not opt in).
3. DI test with `ValidateOnBuild` and `ValidateScopes`, configured and unconfigured, in both registration orders.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: AGENTS.md section 3, registration completeness; messaging patterns are optional and the registry is opt-in.
TASK: Add the registration extension, wire it into AddMessagingServices with an empty registry by default, and
test the container.
KEY RULES: TryAdd*; startup validation throws a clear InvalidOperationException listing the offending names
(codes, not payloads); the application's own registration wins.
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

1. Multi-tenancy (function 10): the registry is global and carries no tenant data; the tenant-aware requirement of the issue (REQ-061, AC-043) is met by the existing tenant column of the stores, and a two-tenant test shows tenant A's rows never resolve through tenant B's reads (the reader is stateless).
2. Validation: duplicate or malformed names and upcast gaps fail at start-up (Phase 2).
3. Deferred rows keep their reason in the matrix.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT: SPEC-002 REQ-061 and AC-043: every record this issue adds or changes carries the tenant id; the registry
adds no record.
TASK: Add the two-tenant read test and confirm the validation paths are covered.
KEY RULES: AGENTS.md section 6; no scope growth.
REFERENCE FILES: AGENTS.md section 6, docs/specifications (REQ-061, AC-043)
```

</details>

### Phase 6: Observability

<details>
<summary><strong>Tasks</strong></summary>

1. Counters on the `Encina` meter: `encina.messaging.type_name.fallback_total` (writes that used the assembly-qualified fallback) and `encina.messaging.payload.upcast_total` (tags `message_type`, bounded by registered names), with the tenant as an attribute where a context exists (REQ-062).
2. `MessageTypeLog` (`[LoggerMessage]`) with EventIds 5820-5825 inside the family block 5800-5849 (`EventIdRanges.MessagingInboxIdempotency`, registered by plan #3440 or by the first plan of the family to land): 5820 fallback warning (once per type), 5821 unknown stored name, 5822 upcast applied (Debug), 5823 upcast failed, 5824 registry built, 5825 registry validation failed.
3. No payload or message text in tags or logs (AC-044 test with an in-memory exporter).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT: ADR-021; the family block 5800-5849 is registered once. #3440 uses 5800-5804, #1220 uses 5805-5810,
this plan uses 5820-5825.
TASK: Add the counters and MessageTypeLog; re-check on the branch that 5820-5825 are free.
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
7. Manifest targets with justifications; CRAP table.

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
3. ADR for Design Choices 3 to 5 once decided (next free number at implementation time); `docs/INVENTORY.md`; PublicAPI files.
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
| SPEC-002 REQ-061 / AC-043 | Tenant-aware records | No new record; two-tenant read test (Phase 5) |
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
| `Encina.Messaging` | 5820-5825 inside `MessagingInboxIdempotency` (5800-5849) | Family block assigned 2026-11-11; #3440 uses 5800-5804 and #1220 uses 5805-5810; the first plan of the family to land registers the block |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Core abstractions and implementation | 16 | attribute, 3 contracts, error codes, registry, names, reader, options, registration, metrics, log, config helper, upcast chain, fake and test helper |
| Call-site edits | 13 | 7 writers and 6 readers |
| Tests | 11 | unit, guard, contract, property, integration scenario, DI tests, 2 justification files, EventId map |
| Documentation and metadata | 5 | changelog, guide, ADR, INVENTORY, manifests (PublicAPI per package edited with the code) |
| **Total** | **45** | Three pull requests |

### Pull request split

| PR | Content | Files (approx.) |
|----|---------|-----------------|
| A | Phases 1, 2, 3, 6: attribute, contracts, registry, reader, registration, observability, unit, guard and property tests | 15 |
| B | Phase 4 call sites with their unit tests and the contract tests | 15 |
| C | Phase 7 integration on 10 providers, justifications, Phase 8 documentation | 8 |

A also needs `Encina` core (attribute) merged first; B depends on A; C depends on B.

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
5. Counters and MessageTypeLog EventIds 5820-5825 in the family block 5800-5849.
6. Tests on all flags with the 10-provider integration matrix; docs, ADR and changelog.

KEY PATTERNS:
- ROP; reflection only at start-up; no payloads, message text or personal data in codes, logs or tags.
- Opt-in registry; the unregistered path behaves exactly as today plus a warning.
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
| 2 | OpenTelemetry | ✅ | Fallback and upcast counters with bounded tags (Phase 6) |
| 3 | Structured Logging | ✅ | `MessageTypeLog` EventIds 5820-5825 in the family block (Phase 6) |
| 4 | Health Checks | ❌ | No new checkable dependency; invalid registry fails at start-up |
| 5 | Validation | ✅ | Duplicate names, malformed names and upcast gaps fail at start-up (Phase 2) |
| 6 | Resilience | ❌ | No call to an external system |
| 7 | Distributed Locks | ❌ | No shared mutable state: the registry is immutable after start-up |
| 8 | Transactions | ❌ | Single-operation reads and writes of an existing column |
| 9 | Idempotency | ❌ | Not a message or request entry point |
| 10 | Multi-Tenancy | ✅ | Tenant-aware per REQ-061 through the existing store columns; two-tenant read test (Phase 5) |
| 11 | Module Isolation | ❌ | SPEC-002 requires no module scoping; `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ❌ | No compliance-relevant operation beyond those already audited |

---

## Prerequisites & Dependencies

| Issue | Relation | Notes |
|-------|----------|-------|
| [#134](https://github.com/dlrivada/Encina/issues/134) | Remainder stays open | Full message versioning; this plan is its minimal slice |
| [#1220](https://github.com/dlrivada/Encina/issues/1220) | Shares call sites | Both edit `OutboxProcessorBase`/`OutboxBatchProcessor` neighbors; the later pull request rebases |
| [#2385](https://github.com/dlrivada/Encina/issues/2385) | Shares the outbox type name | The transport publisher reads the stored name through the same reader |
| [#3440](https://github.com/dlrivada/Encina/issues/3440) | Shares the EventId block | Family block 5800-5849 |
| [#1186](https://github.com/dlrivada/Encina/issues/1186) | Parent EPIC | SPEC-002 readiness |

---

## Next Steps

1. At pick-up, re-check the Design Choices against today's code, ask the maintainer Dilemmas 3, 4 and 5 and record the answers in `## Maintainer Decisions`; run `check-plan.ps1` without `-Draft`.
2. Re-run the search for `AssemblyQualifiedName` and `Type.GetType` to refresh the call-site list before PR B.
3. Re-check that 5820-5825 are free and the family block is registered before PR A.
4. Measure per-flag coverage and the CRAP table for every touched file before each pull request.
