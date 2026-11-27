# Implementation Plan: Visual Event Stream Explorer (`encina events` and `encina projections` in `Encina.Cli`)

> **Issue**: [#329](https://github.com/dlrivada/Encina/issues/329)
> **Type**: Feature (tooling)
> **Related**: #328 (event stream visualization, open), #314 (temporal queries, the in-application time-travel API), #326 (Marten multi-tenancy), #322 (crypto-shredding), #576 and #585 (`Encina.EventStoreDB`), #201 (CLI templates, shares `Program.cs`), ADR-062
> **Complexity**: Medium (6 phases, 6 pull requests)
> **Estimated Scope**: ~1,300 lines of production code + ~1,900 lines of tests and documentation, in six pull requests of at most 15 files

---

## Summary

### What exists today (read on 2026-11-27, `origin/main` `b5114e7e5`)

- `src/Encina.Cli` is a .NET tool (`PackAsTool`, command `encina`) with `System.CommandLine` 2.0.7 and `Spectre.Console` 0.57.2 (both pinned), the commands `new`, `generate` and `add` (`src/Encina.Cli/Commands/*.cs`, `internal static class` with `Create()`), registered in `Program.cs`. It references no Encina package, no Marten and no database client. Its `InternalsVisibleTo` list names UnitTests, IntegrationTests, ContractTests and PropertyTests but **not** GuardTests; UnitTests, GuardTests, IntegrationTests and PropertyTests reference the project, ContractTests does not. The manifest `.github/coverage-manifest/Encina.Cli.json` has targets guard 15, unit 60, integration 75.
- Marten 9.38.0 and Npgsql 10.0.2 are pinned; `EventStore.Client.Grpc.Streams` 23.3.9 is pinned, but there is **no `Encina.EventStoreDB` package** and no `Testcontainers` EventStoreDB pin (the package is planned by #576 and #585).
- The data the issue wants to show is already available inside an application: `IEventMetadataQuery` (`GetCausalChainAsync`, `GetEventByIdAsync`, correlation and causation queries; `src/Encina.Marten/IEventMetadataQuery.cs`), `IProjectionManager.GetAllStatusesAsync` (`src/Encina.Marten/Projections/IProjectionManager.cs`), `ISnapshotStore<T>`, and `IAggregateRepository<T>.LoadAsync(id, version)`. A command-line tool has no application and no DI container, so none of it is reachable from the CLI today.
- Marten stores events in its own PostgreSQL schema (`mt_events`, `mt_streams`, `mt_event_progression`); that schema is Marten's, not an Encina contract (Design Choice 1 states the consequence and the test that guards it).

### What this plan delivers

- **`encina events list|show|stats|trace|replay`** and **`encina projections status`**, with `--output table|json` (Spectre.Console tables and trees; plain JSON for scripts), reading a Marten (PostgreSQL) store directly with the operator's own credentials, read-only.
- **Fail-closed behavior**: no tenant means no data when the store holds more than the default tenant (`--tenant` or an explicit `--all-tenants`); the connection string never appears in output, logs or `ToString()`.
- **A provider-neutral reader** (`IEventStreamReader`, internal to the CLI) so the same commands can read EventStoreDB; the EventStoreDB reader is the last code pull request (Design Choice 3).
- **Not delivered**: web UI and IDE extensions (the issue's rejected alternatives); a new package outside `Encina.Cli`; structured logging and OpenTelemetry (a console tool; register row C5).
- Affected packages: `Encina.Cli` (code, `Encina.Cli.csproj`, manifest) and test projects. Provider category: event sourcing (Marten primary, `AGENTS.md` section 5); the 10-database rule does not apply. Integration tests run Marten on PostgreSQL through Testcontainers (`AGENTS.md` section 3).

---

## Design Choices

<details>
<summary><strong>1. How the CLI reaches the events</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Direct, read-only access to the store with the operator's connection string: Npgsql over Marten's tables for Marten, the gRPC client for EventStoreDB; payloads are raw JSON** | Works with no application running, matches the issue ("connection string configuration", "multiple providers"); no Marten reference in the tool, so the tool stays small; raw JSON never decrypts crypto-shredded fields | Relies on Marten's internal table and column names (guarded by an integration test against the pinned version); production database credentials on the operator's machine; bypasses the application's tenant and audit gates |
| **B) App-hosted explorer endpoints (`MapEncinaEventExplorer`) and a thin HTTP client in the CLI** | Runs inside the application: its tenant gates, authorization and read audit apply; no database credentials on laptops; type-aware payloads | A new server package and an authentication story; needs a running application; roughly two more pull requests |
| **C) Load the application's assembly and host (the `dotnet ef` pattern) and call its `IEventMetadataQuery` and `IProjectionManager`** | Full fidelity, any serializer, real tenancy | Runs application startup code from a tool; version coupling between tool and application; a host-resolution helper to build and pin |

### Chosen Option: **A - Direct read-only access** (recommended, pending the maintainer)

### Rationale

- A wins for development and incident debugging on a store the operator may already read; B wins when production credentials must not leave the servers or when reads of personal data must be audited by the application (the reference application's health data is the scenario); C wins when payloads must be shown as typed objects for any serializer. A matches the issue's task list and keeps `Encina.Cli` free of Encina and Marten references. The Marten schema coupling is contained in one class (`MartenSqlEventStreamReader`) and one integration test that creates the schema through a real Marten store and reads it back, so a Marten upgrade that renames a column fails that test, not an operator. The connection is opened with `default_transaction_read_only=on`, so the tool cannot mutate the store even by mistake.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. State reconstruction (`events replay`)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `--assembly <path>` and `--aggregate <type>`: load the application's aggregate assembly in a collectible `AssemblyLoadContext` (with `AssemblyDependencyResolver`), deserialize the stored JSON with `System.Text.Json` into the event CLR types named by `mt_dotnet_type`, and fold them through `IAggregate.LoadFromHistory` up to `--version` or `--at`** | Delivers the issue's `replay` with no running application; state shown as the aggregate's real JSON | Needs `Encina.DomainModeling` in the tool for `IAggregate`; assumes events were written with the default System.Text.Json settings (a custom serializer fails with a clear error) |
| **B) Show a JSON fold only: the list of events up to the version, no aggregate state** | No assembly loading, no coupling | Not "replay to state"; the issue's acceptance item is not met |
| **C) Defer `replay` until Design Choice 1 option B or C exists** | No fidelity risk now | One of six commands missing in the first release |

### Chosen Option: **A - Assembly-based replay** (recommended, pending the maintainer)

### Rationale

- A wins when the operator has the application build output at hand (the debugging scenario of the issue); C wins when the maintainer prefers the explorer to be type-agnostic only; B is not a replay. The decision is isolated in PR E4, so E1 to E3 ship without it. Replay uses the aggregate's own `Apply` logic, so its output is exactly what `IAggregateRepository.LoadAsync(id, version)` would return in the application.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. EventStoreDB support</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Marten only now; the `IEventStreamReader` seam exists, the EventStoreDB reader is left to #576 and #585** | Smaller; no new container pin | The issue's acceptance item "Works with Marten and EventStoreDB" is not met; the seam is unproven by a second implementation |
| **B) Marten first; the EventStoreDB reader as the last pull request (E5) over the pinned `EventStore.Client.Grpc.Streams`, with the integration test on an EventStoreDB container** | Meets the acceptance item; proves the seam | A new `Testcontainers` EventStoreDB pin (exact-version rule, clean restore audit); no `Encina.EventStoreDB` dependency is needed because the reader uses the client directly |

### Chosen Option: **B - Reader in the last code pull request** (recommended, pending the maintainer)

### Rationale

- B wins because the issue names both stores in its acceptance criteria; A wins if the maintainer does not want a new container pin before #576 lands. E5 is last, so dropping it later changes nothing else. `trace` and `projections status` are Marten-only in the first cut because EventStoreDB has no causation index and projections are subscription checkpoints; the command prints a clear "not supported for this store" code (`encina.cli.events.unsupported_store`, `InvalidRequest`).

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Tenant behavior of a read-only operator tool</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Fail closed: the tool finds the distinct `tenant_id` values (at most two rows fetched); when anything besides Marten's default tenant exists, a command needs `--tenant <id>` (reads that tenant only) or an explicit `--all-tenants`; otherwise it is denied** | Matches the gate rule; a single-tenant store works with no flag; the choice to read every tenant is explicit and printed in the output header | One extra query; the operator learns the flag |
| **B) Always read every tenant (the tool is an operator tool)** | Simplest | A mistyped command leaks another tenant's data into a terminal and a shell log |

### Chosen Option: **A - Deny without an explicit tenant choice**

### Rationale

- The tenant id is passed as a bound SQL parameter, never concatenated; stream and event ids are `Guid`s read together with the tenant column, so no two tenant/key pairs address one row. The id is never written to a log (there is none) and is shown only in the output header the operator asked for. This is the CLI counterpart of the fail-closed gates.

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (compliance and security gates fail closed when context is missing); SPEC-002 DEC-006

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | Connection string: read from `ENCINA_EVENTS_CONNECTION` or `--connection-string` (the option help warns that shell history keeps arguments); the options class marks it `[JsonIgnore]` and overrides `ToString()`; it never appears in output or in an error | Settled | `AGENTS.md` section 3 (options with a connection string) |
| C2 | Every database call is asynchronous with the command's `CancellationToken` (`OpenAsync`, `ExecuteReaderAsync`); `Console.CancelKeyPress` cancels the token | Settled | `AGENTS.md` section 3 (database calls) |
| C3 | Error codes (prefix `encina.cli.events.`, `EncinaError` carrying an `ErrorCategory`): `invalid_argument` (`Validation`, bad id, version or timestamp), `tenant_required` (`InvalidRequest`), `unsupported_store` (`InvalidRequest`), `stream_not_found` and `event_not_found` (`NotFound`: the id is supplied by the operator), `store_unavailable` (`Unavailable`), `store_failed` (`Internal`), `replay_failed` (`Internal`). Process exit code: 0 success, 1 a `Left`, 2 a usage error | Settled | ADR-062 rules R1, R3, R4; no `FailedPrecondition` before #3420 |
| C4 | Payloads are shown as stored: the tool references no key store, so crypto-shredded fields stay encrypted JSON and an erased subject stays unreadable; `--no-payload` suppresses payloads in `list` | Trivial | Crypto-shredding plan #322 and `src/Encina.Marten.GDPR`: encryption is applied by the Marten serializer, which the raw read bypasses |
| C5 | No `[LoggerMessage]`, no EventId, no telemetry: the console output is the product and the tool has no logging provider configuration; the issue's "structured logging included" is amended (Next Steps) | Settled | Plan #201 register row C8 (CLI commands print to the console and claim no EventId) |
| C6 | Output: `table` (Spectre.Console) by default, `json` for scripts (one JSON document on stdout, diagnostics on stderr, no colors when redirected) | Trivial | Issue task "Add `--output json` option for scripting" |
| C7 | Time is displayed as UTC from the stored timestamp; the tool reads no clock (`--from`, `--to`, `--at` are explicit `DateTimeOffset` values) | Settled | `AGENTS.md` section 3 (time comes from `TimeProvider`) |
| C8 | Projection lag: `last_seq_id` of each `mt_event_progression` row against the maximum event sequence; status `UpToDate` (lag 0), `Behind` (lag below `--stale-after`, default 1,000) or `Stale` | Trivial | Issue example output; Marten async daemon progression table |

---

## Implementation Phases

### Delivery order and size

Six pull requests; each builds and passes alone with its own tests (counts include csproj, `Program.cs`, manifest, test-project edits and the fragment).

| PR | Phase | Content | Files | Lines (est.) |
|----|-------|---------|-------|--------------|
| E1 | 1 | `events` and `projections` command groups, options, output renderer, error codes, csproj | 11 | ~850 |
| E2 | 2 | `IEventStreamReader`, `MartenSqlEventStreamReader`, tenant gate, `list`, `show`, `stats` | 8 | ~1,350 |
| E3 | 3 | `trace` and `projections status` | 7 | ~700 |
| E4 | 4 | `replay` with assembly loading | 7 | ~850 |
| E5 | 5 | EventStoreDB reader and container fixture | 8 | ~900 |
| E6 | 6 | Guide, README, INVENTORY, ROADMAP, changelog | 5 | ~450 |

### Phase 1: Command Groups, Options and Rendering (pull request E1)

<details>
<summary>Tasks</summary>

1. **Modify** `src/Encina.Cli/Encina.Cli.csproj` (`PackageReference Npgsql`; `InternalsVisibleTo` for `Encina.GuardTests`), `src/Encina.Cli/Program.cs` (register the two groups).
2. **Create** `src/Encina.Cli/Commands/EventsCommand.cs` (`internal static class` with `Create()`: group `events` with the shared options `--connection-string`, `--schema` (default `public`), `--tenant`, `--all-tenants`, `--output`), `src/Encina.Cli/Commands/ProjectionsCommand.cs` (group `projections`), `src/Encina.Cli/Services/Events/EventsConnectionOptions.cs` (C1), `EventsErrorCodes.cs` (C3), `EventsOutputRenderer.cs` (table and JSON of a neutral `EventRow` record).
3. **Modify** `.github/coverage-manifest/Encina.Cli.json` (per-file targets below; through `mechanical-fixer`).
4. Tests: new `tests/Encina.UnitTests/Cli/Events/EventsCommandTests.cs` (option parsing, missing connection string is `Validation`, exit codes), `EventsOutputRendererTests.cs` (table and JSON golden output), `tests/Encina.GuardTests/CLI/EventsConnectionOptionsGuardTests.cs` (`ToString` hides the string; null and whitespace arguments).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #329, plan docs/plans/event-stream-explorer-implementation-plan-329.md, phase 1.
Encina.Cli is an internal-command tool: commands are internal static classes with Create(); Program.cs registers them.

TASK:
1. Add Npgsql and the GuardTests InternalsVisibleTo entry to Encina.Cli.csproj; register the events and projections groups.
2. Create the command groups with the shared options, the options class, the error codes and the output renderer.
3. Unit and guard tests; manifest targets through mechanical-fixer.

KEY RULES:
- The connection string is [JsonIgnore], ToString never prints it, and it never appears in output or errors.
- Errors are EncinaError with ErrorCategory per ADR-062 (Validation, InvalidRequest, NotFound, Unavailable, Internal); exit codes 0, 1, 2.
- No logging provider and no EventId (register row C5). English only; zero warnings; XML docs on public members.

REFERENCE FILES:
- src/Encina.Cli/Commands/AddCommand.cs, src/Encina.Cli/Program.cs, src/Encina.Cli/Encina.Cli.csproj
- tests/Encina.GuardTests/CLI/PackageManagerGuardTests.cs
```

</details>

### Phase 2: Marten Reader, Tenant Gate, `list`, `show`, `stats` (pull request E2)

<details>
<summary>Tasks</summary>

1. **Create** `src/Encina.Cli/Services/Events/IEventStreamReader.cs` (`internal interface`: `ListAsync(streamId, EventFilter, ct)`, `GetAsync(streamId, version, ct)`, `GetStatsAsync(streamId, ct)`, `GetTenantsAsync(ct)`, all returning `Task<Either<EncinaError, T>>` over neutral records `EventRow`, `StreamStats`), `MartenSqlEventStreamReader.cs` (Npgsql, `default_transaction_read_only=on`, schema name validated against `^[A-Za-z_][A-Za-z0-9_]*$` and quoted, every value a bound parameter, tenant filter always applied), `TenantGate.cs` (Design Choice 4).
2. **Modify** `EventsCommand.cs` (`list` with `--limit` default 50 and at most 1,000, `--from`, `--to`, `--no-payload`; `show --version`; `stats`; summaries show the event type and payload keys only up to 60 characters), `Encina.Cli.json`.
3. Tests: new `tests/Encina.IntegrationTests/Cli/Events/MartenSqlEventStreamReaderIntegrationTests.cs` (shared `[Collection(MartenCollection.Name)]` fixture: a real Marten store appends events for two tenants; list, show, stats, a renamed-column guard, tenant denial without a flag, a read-only connection rejects an `INSERT`), `tests/Encina.UnitTests/Cli/Events/TenantGateTests.cs`, guard tests for the reader constructor and the schema-name check.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT:
Issue #329 plan docs/plans/event-stream-explorer-implementation-plan-329.md, phase 2. E1 merged.

TASK:
1. IEventStreamReader, MartenSqlEventStreamReader, TenantGate; wire list, show and stats.
2. Integration tests on Marten PostgreSQL through the shared MartenFixture collection; unit and guard tests.

KEY RULES:
- Read-only (default_transaction_read_only=on), asynchronous with the CancellationToken, parameters bound, schema identifier validated and quoted.
- More than the default tenant in the store and no --tenant or --all-tenants: deny with encina.cli.events.tenant_required.
- Never reference a key store; payloads stay as stored. Never print the connection string.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Infrastructure/Marten/Fixtures/MartenFixture.cs
- src/Encina.Marten/EventMetadataEnrichmentService.cs (correlation, causation and TenantId metadata)
```

</details>

### Phase 3: `trace` and `projections status` (pull request E3)

<details>
<summary>Tasks</summary>

1. **Modify** `IEventStreamReader.cs` (`GetCausalChainAsync(eventId, maxDepth, ct)` using a recursive query on the causation column with a depth cap of 100, the same cap as `IEventMetadataQuery.GetCausalChainAsync`; `GetProjectionProgressAsync(ct)`), `MartenSqlEventStreamReader.cs`, `EventsCommand.cs` (`trace <event-id>` rendered as a Spectre.Console tree, the queried event marked), `ProjectionsCommand.cs` (`status` with `--stale-after`), `Encina.Cli.json`.
2. Tests: edit the integration test class (chain of four events across streams, a cycle stops at the cap, lag and status values), new `tests/Encina.UnitTests/Cli/Events/ProjectionStatusClassifierTests.cs` (UpToDate, Behind, Stale boundaries).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
Issue #329 plan docs/plans/event-stream-explorer-implementation-plan-329.md, phase 3. E1 and E2 merged.

TASK:
1. trace over the causation chain and projections status over mt_event_progression.
2. Integration and unit tests.

KEY RULES:
- Depth cap 100; the tenant gate applies to every query, including the chain walk (a chain never crosses into another tenant).
- Lag classification per register row C8; unsupported store returns encina.cli.events.unsupported_store.

REFERENCE FILES:
- src/Encina.Marten/IEventMetadataQuery.cs, src/Encina.Marten/Projections/ProjectionStatus.cs
```

</details>

### Phase 4: `replay` (pull request E4)

<details>
<summary>Tasks</summary>

1. **Modify** `Encina.Cli.csproj` (`ProjectReference` to `Encina.DomainModeling`), **create** `src/Encina.Cli/Services/Events/AggregateReplayer.cs` (collectible `AssemblyLoadContext`, `AssemblyDependencyResolver`, event types resolved by the stored .NET type name, `System.Text.Json`, `LoadFromHistory`, state serialized as indented JSON; failures are `encina.cli.events.replay_failed` with the exception type only), `EventsCommand.cs` (`replay <id> --assembly --aggregate [--version | --at]`), `Encina.Cli.json`.
2. Tests: new unit tests with a small aggregate compiled into the test assembly (version and instant cut-offs, unknown event type, assembly not found), integration test over the Marten reader, guard tests for the replayer.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
Issue #329 plan docs/plans/event-stream-explorer-implementation-plan-329.md, phase 4. E1-E3 merged.

TASK:
1. AggregateReplayer and the replay command; add the DomainModeling reference.
2. Tests with a real aggregate type and real stored JSON.

KEY RULES:
- Unload the load context after the run; never execute application startup code; only deserialize and fold events.
- A serializer other than the default System.Text.Json fails with replay_failed (exception type only); EncinaError.Message is not printed from a caught exception.

REFERENCE FILES:
- src/Encina.DomainModeling/IAggregate.cs, src/Encina.Marten/MartenAggregateRepository.cs (Replay)
```

</details>

### Phase 5: EventStoreDB Reader (pull request E5)

<details>
<summary>Tasks</summary>

1. **Create** `src/Encina.Cli/Services/Events/EventStoreDbEventStreamReader.cs` (`EventStoreClient` from the pinned `EventStore.Client.Grpc.Streams`, read-only `ReadStreamAsync`; metadata JSON for correlation and causation), **modify** `Encina.Cli.csproj` (`PackageReference EventStore.Client.Grpc.Streams`), `Directory.Packages.props` (the EventStoreDB `Testcontainers` pin, exact version, clean restore audit), `EventsCommand.cs` (`--store marten|eventstoredb`), `Encina.Cli.json`.
2. Tests: new `tests/Encina.IntegrationTests/Cli/Events/EventStoreDbEventStreamReaderIntegrationTests.cs` with its own `[Collection("EventStoreDB")]` fixture, unit tests for the mapping, guard tests.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT:
Issue #329 plan docs/plans/event-stream-explorer-implementation-plan-329.md, phase 5. E1-E4 merged.

TASK:
1. EventStoreDbEventStreamReader behind IEventStreamReader and the --store option.
2. Container fixture and integration tests; pin the Testcontainers package.

KEY RULES:
- Exact package version, no floating range; run a clean restore audit after the pin.
- Operations the store cannot answer (trace, projections) return unsupported_store; tenant semantics: EventStoreDB has no tenant column, so --tenant maps to a stream-name prefix only when the application declares one; otherwise the tenant gate treats the store as single-tenant and prints that fact.

REFERENCE FILES:
- Directory.Packages.props, tests/Encina.IntegrationTests/Infrastructure/Marten/Fixtures/MartenFixture.cs
```

</details>

### Phase 6: Documentation and Finalization (pull request E6)

<details>
<summary>Tasks</summary>

1. Through `docs-writer`: `docs/features/event-stream-explorer.md` (commands with the issue's examples, connection and tenant options, what is and is not shown, security notes), the CLI README section, `docs/INVENTORY.md`, `ROADMAP.md` line for #329.
2. `changelog.d/329-event-stream-explorer.added.md` through `mechanical-fixer`.
3. Build with zero warnings; every flag reaches its target; the PR reports measured per-file coverage.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT:
Issue #329 plan docs/plans/event-stream-explorer-implementation-plan-329.md, phase 6. All code merged.

TASK:
1. Ask docs-writer for the guide, README section, INVENTORY and ROADMAP lines; the fragment through mechanical-fixer.
2. Report measured per-file coverage against the plan.

KEY RULES:
- Documentation never types coverage figures by hand; public text never names a private client application.
- The guide states the read-only guarantee, the tenant rule and that payloads stay encrypted for erased subjects.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md, docs/features/
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| EventCatalog and Spectre.Console documentation (issue references) | Visualization and terminal rendering | Output design |
| `AGENTS.md` section 3 | Fail-closed gates, async database calls, connection-string options, time source | Register rows C1, C2, C7; Design Choice 4 |
| SPEC-002 DEC-006 | Gates fail closed | Tenant gate |
| ADR-062 | `ErrorCategory` rules | Register row C3 |
| Marten documentation | Event tables and async daemon progression | Reader queries (guarded by the integration test) |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `System.CommandLine`, `Spectre.Console` | `src/Encina.Cli/Encina.Cli.csproj` | Commands and rendering |
| Command pattern (`internal static` with `Create()`) | `src/Encina.Cli/Commands/` | New groups |
| `IEventMetadataQuery.GetCausalChainAsync` (depth cap 100) | `src/Encina.Marten/IEventMetadataQuery.cs` | Semantics mirrored by `trace` |
| `ProjectionStatus` | `src/Encina.Marten/Projections/ProjectionStatus.cs` | Status vocabulary |
| `IAggregate.LoadFromHistory` | `src/Encina.DomainModeling/IAggregate.cs` | `replay` |
| `MartenFixture` | `tests/Encina.IntegrationTests/Infrastructure/Marten/Fixtures/MartenFixture.cs` | Real Marten schema |
| CLI test layout | `tests/Encina.UnitTests/Cli/`, `tests/Encina.GuardTests/CLI/` | Test placement |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Cli` | none | Console tool, no `[LoggerMessage]` (register row C5, as plan #201 C8) |

### Estimated File Count

| PR | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| E1 | 5 (`EventsCommand`, `ProjectionsCommand`, `EventsConnectionOptions`, `EventsErrorCodes`, `EventsOutputRenderer`) | 2 (`Encina.Cli.csproj`, `Program.cs`) | 3 | 0 | 1 (`Encina.Cli.json`) | 0 | 11 | ~850 |
| E2 | 3 (`IEventStreamReader`, `MartenSqlEventStreamReader`, `TenantGate`) | 1 (`EventsCommand.cs`) | 3 (integration, gate unit, guard) | 0 | 1 | 0 | 8 | ~1,350 |
| E3 | 0 | 4 (`IEventStreamReader.cs`, `MartenSqlEventStreamReader.cs`, `EventsCommand.cs`, `ProjectionsCommand.cs`) | 1 (`ProjectionStatusClassifierTests`) | 1 (the E2 integration test class) | 1 | 0 | 7 | ~700 |
| E4 | 1 (`AggregateReplayer`) | 2 (`Encina.Cli.csproj`, `EventsCommand.cs`) | 2 (unit, guard) | 1 (the E2 integration test class) | 1 | 0 | 7 | ~850 |
| E5 | 1 (`EventStoreDbEventStreamReader`) | 3 (`Encina.Cli.csproj`, `EventsCommand.cs`, `Directory.Packages.props`) | 3 (integration with its fixture, unit, guard) | 0 | 1 | 0 | 8 | ~900 |
| E6 | 0 | 0 | 0 | 0 | 0 | 5 (guide, README, `INVENTORY.md`, `ROADMAP.md`, fragment) | 5 | ~450 |
| **Total** | **10** | **12** | **12** | **2** | **5** | **5** | **46** | **~5,100** |

Column check: 10 + 12 + 12 + 2 + 5 + 5 = 46 = 11 + 8 + 7 + 7 + 8 + 5. `MartenFixture` is reused unchanged. The implementing brief recounts with a grep on the branch.

No existing interface changes: `IEventStreamReader` is new and internal, so no implementers or fakes exist outside this plan. `Encina.Cli` has no `PublicAPI` files (internal tool), so no PublicAPI lines. New constructors: `MartenSqlEventStreamReader`, `EventStoreDbEventStreamReader` and `AggregateReplayer` are `internal`; their guard tests rely on the `InternalsVisibleTo` entry for GuardTests added in E1 (csproj edit counted in E1).

Per-file coverage targets (baseline: `Encina.Cli.json` targets guard 15, unit 60, integration 75, measured 76.2 % integration on 2026-03-27; targets for the new files are added with the PR that creates them and never lower the package values):

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `Commands/EventsCommand.cs` | 70 | 20 | 0 | 0 | 40 | Option parsing and exit codes are unit tested; actions run end to end in the reader integration tests; no contract test project reference exists for the CLI |
| `Commands/ProjectionsCommand.cs` | 70 | 20 | 0 | 0 | 40 | Same pattern as above |
| `Services/Events/EventsConnectionOptions.cs` | 90 | 90 | 0 | 0 | 0 | Small class; guard covers arguments and the `ToString` secret rule |
| `Services/Events/EventsErrorCodes.cs` | 0 | 0 | 0 | 0 | 0 | Constants only |
| `Services/Events/EventsOutputRenderer.cs` | 90 | 0 | 0 | 0 | 0 | Pure formatting, golden-output tests; no validated reference parameter |
| `Services/Events/TenantGate.cs` | 95 | 60 | 0 | 0 | 70 | Every branch is a unit test; integration proves the real tenant query |
| `Services/Events/MartenSqlEventStreamReader.cs` | 20 | 60 | 0 | 0 | 85 | SQL behavior needs a real schema; guard covers arguments and schema-name validation |
| `Services/Events/AggregateReplayer.cs` | 80 | 60 | 0 | 0 | 50 | Replay logic and failure paths unit tested with a compiled aggregate |
| `Services/Events/EventStoreDbEventStreamReader.cs` | 40 | 60 | 0 | 0 | 80 | Mapping unit tested; the gRPC read needs the container |
| `Services/Events/IEventStreamReader.cs` | 0 | 0 | 0 | 0 | 0 | Interface, no executable code |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #329 - the event stream explorer in Encina.Cli - in six small pull requests (E1 to E6), in order.
Plan: docs/plans/event-stream-explorer-implementation-plan-329.md.

PROJECT CONTEXT:
- Encina .NET 10 / C# 14, pre-1.0. Edit with Edit/Write only; manifests and changelog through mechanical-fixer; docs through docs-writer.
- Encina.Cli is a dotnet tool with internal static command classes; it references no Encina package until E4 (DomainModeling).
- The CLI reads the store directly, read-only, with the operator's connection string; it never decrypts and never prints the connection string.

IMPLEMENTATION OVERVIEW:
1. E1: events and projections groups, options, error codes, renderer, csproj (Npgsql, GuardTests InternalsVisibleTo).
2. E2: IEventStreamReader, Marten SQL reader, tenant gate, list, show, stats.
3. E3: trace and projections status. 4. E4: replay by assembly loading. 5. E5: EventStoreDB reader. 6. E6: documentation.

KEY PATTERNS:
- ROP: every service method returns Either<EncinaError, T> with an ADR-062 category; the command maps a Left to exit code 1.
- Fail closed on tenants (Design Choice 4); async database calls with the CancellationToken; no logging, no EventId.
- Guard tests for every public or internal-visible constructor and method parameter; integration tests on real Marten PostgreSQL.

REFERENCE FILES:
- src/Encina.Cli/Commands/AddCommand.cs, src/Encina.Marten/IEventMetadataQuery.cs, src/Encina.Marten/Projections/IProjectionManager.cs
- tests/Encina.IntegrationTests/Infrastructure/Marten/Fixtures/MartenFixture.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Interactive, one-shot reads; nothing to cache |
| 2 | OpenTelemetry | ❌ N/A | A short-lived tool with no exporter; the issue deferred it ("display trace context"): `trace` and `show` already print the stored correlation and causation ids, so no deferral issue is needed |
| 3 | Structured Logging | ❌ N/A | Console output is the product (register row C5); the issue marked it included and is amended |
| 4 | Health Checks | ❌ N/A | Not a long-running service |
| 5 | Validation | ✅ Phase 1 | Every argument validated, `Validation` errors as `Left` |
| 6 | Resilience | ❌ N/A | Interactive: the operator retries; the connection has a fixed timeout |
| 7 | Distributed Locks | ❌ N/A | Read-only |
| 8 | Transactions | ❌ N/A | Read-only connection (`default_transaction_read_only=on`) |
| 9 | Idempotency | ❌ N/A | Reads are idempotent |
| 10 | Multi-Tenancy | ✅ Phase 2 | Tenant gate (Design Choice 4); the issue deferred it, the plan integrates it because the tool reads tenant data |
| 11 | Module Isolation | ❌ N/A | Marten events carry no module identifier today; nothing to filter by |
| 12 | Audit Trail | ❌ N/A | The tool uses the operator's own database credentials, so the database engine's audit applies; an Encina audit entry needs an application context a tool does not have. Design Choice 1 option B is the audited path if the maintainer needs it |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| Marten fixture for integration tests | Merged | `MartenFixture` |
| `Npgsql` and `EventStore.Client.Grpc.Streams` pins | Merged | `Directory.Packages.props` |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Marten multi-tenancy modes (schema or database per tenant) | #326 | The tool then needs `--schema` per tenant; `--schema` already exists |
| `Encina.EventStoreDB` | #576, #585 | Not required: the reader uses the client directly |
| CLI template command | #201 | Shares `Program.cs`; whichever merges second rebases |

---

## Next Steps

1. Link this plan from #329 and ask the maintainer to answer Design Choices 1, 2 and 3 when work starts.
2. Implement E1 to E6 in order (E4 and E5 are independent after E2).
3. Maintainer action: amend the acceptance criteria of #329 that name structured logging, OpenTelemetry deferral, multi-tenancy deferral and "Works with Marten and EventStoreDB" to the outcome of this plan (this plan never comments on an issue).
