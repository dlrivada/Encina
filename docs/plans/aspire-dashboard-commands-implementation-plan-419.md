# Implementation Plan: Aspire Dashboard commands for Encina (`WithEncinaCommands`, dead letter retry first)

> **Issue**: [#419](https://github.com/dlrivada/Encina/issues/419)
> **Type**: Feature
> **Complexity**: Medium (6 phases, 3 pull requests of 15, 11 and 6 files)
> **Estimated Scope**: ~550 lines of production code + ~1,100 lines of tests
> **Family**: Aspire (aspire-1): #416 hosting package (this plan adds a folder to it), #417 service defaults (telemetry the dashboard shows), #421 deployment, #440 umbrella; app-side endpoints the commands call: #2229 (dead letter admin API), #2013 (audit of operator replay); the family continues in #424, #425, #382, #387 and #391. **This plan owns the command infrastructure, the failure-code table and the `AspireHosting` EventId range.**

---

## Summary

The issue asks for dashboard "commands and metrics visualization" for the messaging patterns. State of `origin/main` (`ec129cae5`), verified by reading, grep and the Aspire package:

| Item of the issue | Today | Owner of what remains |
|-------------------|-------|-----------------------|
| Custom commands infrastructure | Missing | **This plan**, in `Encina.Aspire.Hosting` (#416), folder `Commands/` (Design Choice 1) |
| `retry-dead-letters` | `IDeadLetterManager.ReplayAllAsync(filter, maxMessages = 100)` exists in `src/Encina.Messaging/DeadLetter/IDeadLetterManager.cs`; no HTTP surface yet: `MapEncinaDeadLetterEndpoints` is #2229 (open, planned) | **This plan** (the command), #2229 (the endpoint) |
| `process-outbox`, `clear-outbox` | `OutboxOrchestrator.ProcessPendingMessagesAsync` needs a publish callback owned by the processor; `RequeueExhaustedAsync` exists; there is no operation to clear processed rows and no HTTP surface | Follow-up issue file (Design Choice 2) |
| `cancel-saga`, `compensate-saga` | `SagaOrchestrator.StartCompensationAsync` and `FailAsync` exist; no cancel operation and no HTTP surface | Follow-up issue file |
| `trigger-scheduled` | `SchedulerOrchestrator.CancelAsync` exists; no run-now operation and no HTTP surface | Follow-up issue file |
| `purge-inbox` | `MessagePurgeService` purges on a timer; no on-demand operation and no HTTP surface | Follow-up issue file |
| Metrics `encina_outbox_pending_total` and siblings | Do not exist. The real instruments are named with dots, for example `encina.outbox.pending_count`, `encina.outbox.messages_processed_total`, `encina.saga.active_count`, `encina.inbox.duplicates_detected_total` (`src/Encina.Messaging/Diagnostics/MessagingStoreMetrics.cs`); the dashboard receives them through OTLP | #417 wires the telemetry; no endpoint is needed (Design Choice 5) |
| "Custom panel" of #440 and "metrics endpoint" | `Aspire.Hosting` 13.6.5 offers commands, URLs, resource snapshot properties, relationships and health checks; no type in its XML documentation adds a panel to the dashboard | Design Choice 5 |
| Documentation with examples | Missing | **This plan** (README section) |

Differences from the issue text, found while reading the Aspire package: `WithCommand(name, displayName, executeCommand, updateState, iconName, iconVariant)` is replaced in 13.6.5 by `WithCommand(name, displayName, executeCommand, CommandOptions)` (`CommandOptions` carries `UpdateState`, `IconName`, `IconVariant`, `ConfirmationMessage`, `Arguments`, `ValidateArguments`, `Visibility`) and by `WithHttpCommand(path, displayName, endpointName, commandName, HttpCommandOptions)` whose `PrepareRequest` and `GetCommandResult` callbacks fit this feature. `ResourceCommandVisibility` "controls discovery and display, not authorization", and API clients (the CLI, agents) can invoke a command the UI hides, so the application endpoint a command calls must authorize every request on its own.

**Delivered**: `WithEncinaCommands<T>(this IResourceBuilder<T>, Action<EncinaCommandsBuilder> configure)` for `T : IResourceWithEndpoints`; `EncinaCommandsBuilder.AddRetryDeadLetters()`; `EncinaCommandsOptions` (route prefix, credential parameter and header, tenant header); the HTTP-status to failure-code mapper; the request preparer (credential, optional tenant); log messages 820-824. The six other commands are not delivered here because the application has no operation to call (Design Choice 2).

**Not in scope**: the application-side operations (#2229 for dead letters; the follow-up issue file for the rest), the dashboard module rows (#423), publishers (#421), an Encina UI.

**Affected packages**: `Encina.Aspire.Hosting` (a `Commands/` folder, created by #416); `Encina` (core: the `AspireHosting` range and its PublicAPI line).

**Provider category**: none; the command calls HTTP endpoints above `IDeadLetterStore`, which all 10 database providers implement.

---

## Design Choices

<details>
<summary><strong>1. Where the commands live (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A folder `Commands/` in `Encina.Aspire.Hosting`** | One AppHost package; the commands use the same `IResourceBuilder` conventions as `WithEncina`; no extra pin or manifest | A solution that wants no commands still has the code in the package (an extension method costs nothing when not called) |
| **B) A separate package `Encina.Aspire.Dashboard` (the issue's name)** | An operations control surface is opt-in at package level | A package for one extension class; its own pin, manifest and README |

### Chosen Option: **A — the hosting package** (recommended, pending the maintainer)

### Rationale

A wins because the issue itself allows it ("or merge into `Encina.Aspire.Hosting`") and #423 already adds module extensions to the same package. B wins when commands that can replay and delete data should be absent from an AppHost's dependency graph entirely, for a security review. The same answer applies to the file layout of the plan.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Which commands this plan delivers (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The infrastructure and `retry-dead-letters` (over #2229's endpoint); the other six commands wait for their application operations, which a follow-up issue file proposes** | Every delivered command calls an operation that exists or is planned; no unauthenticated or invented endpoint | The issue's acceptance items for the outbox, saga and scheduled commands are not met by this plan |
| **B) All seven commands, adding an operations endpoint group for outbox, saga, scheduled and inbox to `Encina.AspNetCore` in this plan** | The issue is met in one go | Six new operations (several have no domain operation yet: "clear outbox", "run a scheduled message now", "cancel a saga") and a new administrative HTTP surface, each needing authorization, tenant rules and audit; it is a feature of its own in another package |

### Chosen Option: **A — infrastructure plus `retry-dead-letters`** (recommended, pending the maintainer)

### Rationale

A wins because a command is only as safe as the endpoint behind it, and #2229 already specifies that endpoint with authorization, tenant rules and confirmation. B wins if the maintainer wants the whole operator surface in one release and accepts a larger plan for the application side (the follow-up issue file is that plan's seed). Each missing operation is listed in the Summary table with the existing method it would call.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. How the AppHost authenticates to the endpoint (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The AppHost holds a secret `ParameterResource` and `PrepareRequest` sends it as `Authorization: Bearer <value>` (header and scheme configurable); the application authorizes it with its own policy through `MapEncinaDeadLetterEndpoints(authorizationPolicy)`** | No key is invented by Encina; the application keeps full control of authentication; the secret never appears in a log | The solution must configure an authentication scheme for the credential |
| **B) Encina generates a random development key, passes it to the application as an environment variable and sends it in a header; the application side validates it** | Works with no configuration | A new authentication handler in the application packages; a shared secret in an environment variable that every process of the resource can read; a fixed pattern that leaks into non-development use |
| **C) No credential** | Simplest | The endpoint would run anonymous; the gate fails closed (AGENTS.md section 3), so #2229 never maps an unauthorized group |

### Chosen Option: **A — a secret parameter and a header** (recommended, pending the maintainer)

### Rationale

A wins because it adds no authentication code to the application packages and reuses the Aspire parameter mechanism (`ParameterResource.GetValueAsync`, present in 13.6.5). B wins for a demo that must work after one `dotnet run`. C is not valid: when no credential parameter is given, the command is added in the Disabled state and a click fails with `credential_missing` (it never sends an anonymous request).

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Tenant context of a command (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An optional input `tenant` (an `InteractionInput`, validated with `ValidateArguments`); a non-blank value is sent verbatim in the header named by `EncinaCommandsOptions.TenantHeader` (default none: the application's resolver decides); a blank value sends no header; the command never invents `default`** | A multi-tenant application can act for one tenant at a time; the manager's fail-closed `dlq_tenant_required` still applies without it | The header name must match the application's tenant resolver |
| **B) No tenant input: a multi-tenant application gets the failure code `tenant_required`** | Smallest | The command is useless for a multi-tenant application |

### Chosen Option: **A — optional tenant input** (recommended, pending the maintainer)

### Rationale

A wins for an application with multi-tenancy on, which is the common case for the dead letter queue (the manager is tenant-aware, `DeadLetterManager.cs:133-151`, and #2229 never lets a client name another tenant through the filter, so the header only feeds the application's resolver). B wins for single-tenant solutions. The reserved `default` tenant segment is never sent for a blank input, and the value is never logged (the log carries only whether a tenant was supplied).

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Metrics and "panel": through telemetry, not through a new endpoint</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The dashboard shows the Encina metrics because the application exports them over OTLP (#417); no endpoint and no panel are added** | Nothing to build; follows the platform; the real instrument names are used | The metrics are in the dashboard's metrics page, not in a custom Encina panel |
| **B) A scrape endpoint (Prometheus-style) of the Encina metrics for the dashboard** | Looks like what the issue's "metrics endpoint integration" says | The Aspire dashboard ingests telemetry through OTLP; it does not scrape a metrics endpoint |
| **C) A custom panel** | The issue's wish | `Aspire.Hosting` 13.6.5 exposes no panel extension (commands, URLs, snapshot properties, relationships, health checks only) |

### Chosen Option: **A — OTLP, no endpoint, no panel**

### Rationale

Only A is possible with the platform: B and C need an API that is not there, and the application's metrics reach the dashboard once the service defaults of #417 subscribe the `Encina` meter.

**Class:** Settled
**Settled by:** `Aspire.Hosting` 13.6.5 `Aspire.Hosting.xml` (no panel type; commands, URLs, `CustomResourceSnapshot.Properties`, relationships and health checks only); ADR-054 settled point 1 (the application chooses the exporters, so telemetry leaves by OTLP); `src/Encina.Messaging/Diagnostics/MessagingStoreMetrics.cs` (the instrument names).

</details>

<details>
<summary><strong>6. Confirmation and the safe-to-repeat contract</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Every command that changes data carries `CommandOptions.ConfirmationMessage` (the replay names the batch size cap), is enabled only while the resource is running, and never retries on its own; a repeated click is safe because the manager rejects an already replayed row with `dlq.already_replayed`** | The issue's "idempotent and safe, consider confirmation dialogs"; no surprise re-sends | One more click |
| **B) No confirmation** | Fewer clicks | A replay re-sends business messages |

### Chosen Option: **A — confirmation, state-gated, no automatic retry**

### Rationale

The issue's technical considerations ask for exactly this: "commands should be idempotent and safe; consider confirmation dialogs for destructive operations".

**Class:** Trivial
**Settled by:** issue #419, Technical Considerations (idempotent and safe, confirmation dialogs); `CommandOptions.ConfirmationMessage` in `Aspire.Hosting.xml` 13.6.5; `src/Encina.Messaging/DeadLetter/DeadLetterManager.cs` (`dlq.already_replayed`).

</details>

---

## Implementation Phases

### Phase 1: Command infrastructure (pull request 1)

> **Goal**: `WithEncinaCommands` exists, builds an HTTP command from a definition, and maps results to failure codes.

<details>
<summary>Tasks</summary>

1. `src/Encina.Aspire.Hosting/Commands/EncinaCommandsOptions.cs` (public sealed): `DeadLetterPrefix` (default `"/api/dead-letters"`, the default of the #2229 plan, validated like a path: starts with `/`, no whitespace), `Credential` (`IResourceBuilder<ParameterResource>?`), `CredentialHeader` (default `"Authorization"`), `CredentialScheme` (default `"Bearer"`, null for a raw value), `TenantHeader` (`string?`, a valid header token). It holds a reference to a parameter, never a secret value, and `ToString()` is overridden to print no property that could hold one.
2. `Commands/EncinaCommandFailure.cs` (internal static, pure): `Map(HttpStatusCode)` to the codes of the table in Phase 3 and `Cancelled`/`Timeout` helpers.
3. `Commands/EncinaCommandsBuilder.cs` (public sealed) and `Commands/EncinaCommandBuilderExtensions.cs`: `WithEncinaCommands<T>(this IResourceBuilder<T> builder, Action<EncinaCommandsBuilder> configure)` with `where T : IResourceWithEndpoints`; the action runs first and the commands are then emitted once; a second call on the same resource throws `InvalidOperationException`; guards. Each command is added with `WithHttpCommand(path, displayName, endpointName: "http", commandName, new HttpCommandOptions { ... })` using `PrepareRequest`, `GetCommandResult`, `UpdateState`, `ConfirmationMessage`, `IconName`, `Arguments` and `ValidateArguments`, and `ResultMode` left at `None` so the response body is never returned to the dashboard.
4. `Commands/Diagnostics/EncinaCommandLog.cs` (`[LoggerMessage]`): 820 command invoked (Information: command and resource names, whether a tenant was supplied), 821 command failed (Warning: command name, failure code, HTTP status as an integer), 822 command cancelled (Information), 823 tenant input rejected (Warning: no value), 824 credential parameter has no value (Error: parameter name); `EventIdRanges.AspireHosting = (820, 839)` registered first in `src/Encina/Diagnostics/EventIdRanges.cs`, the `Encina` PublicAPI line, and the `AssemblyRanges` entry of `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`.
5. Tests: unit (the mapper for every status class, options validation, the builder emits one annotation per command, a second call throws, `UpdateState` disabled while the resource is not running, the confirmation message is set), guard, and the `Encina.json` and hosting manifest entries and `PublicAPI.Unshipped.txt` through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
- Issue #419; plan docs/plans/aspire-dashboard-commands-implementation-plan-419.md. D1 to D4 are recommended, pending the maintainer: follow the Maintainer Decisions section if it exists. The package Encina.Aspire.Hosting exists (#416).
- Aspire.Hosting 13.6.5 offers WithHttpCommand and WithCommand with CommandOptions; confirm each member you use in Aspire.Hosting.xml before coding (HttpCommandOptions.PrepareRequest, GetCommandResult, ResultMode, CommandOptions.UpdateState, ConfirmationMessage, Arguments, ValidateArguments).

TASK:
Add the options, the failure mapper, WithEncinaCommands, the builder skeleton, the log class and the EventId range with their tests.

KEY RULES:
- Never return the response body to the dashboard and never log it, an exception message or a header value; log codes, names and integers only.
- Register EventIdRanges.AspireHosting first; ids 820-824 packed.
- Visibility is not authorization: the command never sends an anonymous request.
- No reference to any Encina package from the hosting project.

REFERENCE FILES:
- src/Encina.Aspire.Hosting (created by #416)
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 2: The `retry-dead-letters` command (pull request 2)

> **Goal**: A dashboard click replays the dead letters of the resource's ambient tenant through the #2229 endpoint.

<details>
<summary>Tasks</summary>

1. `EncinaCommandsBuilder.AddRetryDeadLetters(int maxMessages = 100)` (validated 1 to the manager default cap of 100, the cap `POST /replay` uses in the #2229 plan): command name `retry-dead-letters`, display name "Retry dead letters", `POST {DeadLetterPrefix}/replay` with a JSON body naming `maxMessages` only (no filter fields, so the client can never name a tenant or a type), confirmation text that states the cap, icon, enabled only while the resource is running.
2. `Commands/EncinaCommandRequestPreparer.cs` (internal): the credential header from `Credential.Resource.GetValueAsync(ct)` (an empty value logs 824 and fails the command with `credential_missing` before any request), and the optional tenant header (D4) from the `tenant` input (validated: non-whitespace-only, at most 128 characters, no control characters; never trimmed into `default`).
3. `GetCommandResult` returns success for 2xx and a failure carrying only the code and the HTTP status number otherwise (never the body); a `409` from the endpoint maps to the `conflict` code.
4. Tests: unit for the preparer and the mapper paths, with a real minimal API on a loopback port that stands in for #2229's endpoint (200, 401, 403, 404, 409, 500, a timeout); the test invokes the real `ResourceCommandAnnotation.ExecuteCommand` after assigning an allocated endpoint to the model (the technique is confirmed at work start; when it is not possible the delegates of `HttpCommandOptions` are invoked directly); guard; the entries of the manifests and `PublicAPI.Unshipped.txt` through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT:
- Issue #419, Phase 1 is merged. D3 (secret parameter and header) and D4 (optional tenant input) apply. The endpoint is MapEncinaDeadLetterEndpoints of #2229 (plan docs/plans/deadletter-admin-api-implementation-plan-2229.md); its prefix and body are recommended there, pending the maintainer.

TASK:
Add AddRetryDeadLetters, the request preparer and the result mapping with their tests.

KEY RULES:
- The command body names maxMessages only; no tenant, type or message id is accepted from the dashboard input except the optional tenant header.
- A blank tenant sends no header; the reserved default tenant segment is never invented; the tenant value is never logged.
- No automatic retry; the failure carries a code and a status number only.

REFERENCE FILES:
- src/Encina.Messaging/DeadLetter/IDeadLetterManager.cs
- docs/plans/deadletter-admin-api-implementation-plan-2229.md
- src/Encina.Aspire.Hosting/Commands (Phase 1)
```

</details>

---

### Phase 3: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. Structured logging (function 3): Phase 1. Validation (function 5): the options, the `maxMessages` cap and the tenant input. Idempotency (function 9): Design Choice 6. Multi-tenancy (function 10): Design Choice 4. Audit (function 12): the replay is audited by the manager once #2013 lands; the actor is the principal of the credential the application authenticates.
2. Failure codes and their ADR-062 categories (the package creates no `EncinaError`; the codes are shown by the dashboard and logged):

   | Code | `ErrorCategory` | Cause |
   |------|-----------------|-------|
   | `encina.aspire.command.unauthenticated` | `Unauthenticated` | HTTP 401 |
   | `encina.aspire.command.forbidden` | `Forbidden` | HTTP 403 |
   | `encina.aspire.command.endpoint_not_found` | `NotFound` | HTTP 404: the application did not map the operation or the prefix differs |
   | `encina.aspire.command.conflict` | `Conflict` | HTTP 409 |
   | `encina.aspire.command.rate_limited` | `RateLimited` | HTTP 429 |
   | `encina.aspire.command.rejected` | `InvalidRequest` | Any other 4xx |
   | `encina.aspire.command.upstream_failed` | `UpstreamFailure` | HTTP 5xx or a connection failure |
   | `encina.aspire.command.timeout` | `Timeout` | The request timed out |
   | `encina.aspire.command.canceled` | `Cancelled` | The caller cancelled |
   | `encina.aspire.command.credential_missing` | `FailedPrecondition` | The credential parameter has no value |
   | `encina.aspire.command.tenant_invalid` | `Validation` | The tenant input failed validation |

3. Tenancy and telemetry (ADR-054 D4): the package emits no span or metric, so there is no tenant tag to add; the tenant of a command travels only as a request header.
4. Deferred, as issue files in this plan's worktree: `artifacts/issues/aspire-operations-endpoints-outbox-saga-scheduled-inbox.md` (the application operations behind the six commands that are not delivered). Dead letter endpoints are #2229 and the audit of operator replay is #2013 (both open and planned).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
- Issue #419; the cross-cutting matrix is the contract.

TASK:
Verify each included row exists in code and tests and that the issue file is written.

KEY RULES:
- Never log EncinaError.Message, a response body, a header value or a tenant value.
- The reserved default tenant segment is never sent for a blank input; ids are passed verbatim, never composed.

REFERENCE FILES:
- AGENTS.md sections 3 and 6
- docs/architecture/adr/062-error-category-contract.md
```

</details>

---

### Phase 4: Observability

<details>
<summary>Tasks</summary>

1. `EncinaCommandLog` with EventIds 820-824 inside `AspireHosting = (820, 839)` (15 ids spare), packed with no gaps, XML docs naming the range; the allocation architecture test passes.
2. No `ActivitySource` or `Meter` in an AppHost package. The application's metrics reach the dashboard through #417 (Design Choice 5).

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
- Issue #419. EventIds 800-849 are the block of the Aspire family; this plan takes 820-824.

TASK:
Confirm the log class, the registered range and the architecture test.

KEY RULES:
- Use the [LoggerMessage] source generator; EventIds only inside the registered range; ExecuteCommandContext.Logger or an ILoggerFactory from the service provider is the logger, verified at work start.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

### Phase 5: Testing

<details>
<summary>Tasks</summary>

1. **Unit, Guard** as in Phases 1 and 2. **Contract**: `tests/Encina.ContractTests/Aspire/EncinaCommandFailureContractTests.cs` pins the table of failure codes and their categories (the codes are a public contract of the dashboard). **Property**: `EncinaCommandFailurePropertyTests` (every HTTP status maps to exactly one code and 2xx maps to none). **Integration**: `tests/Encina.IntegrationTests/Aspire/RetryDeadLettersCommandTests.cs` runs the command against a real in-process ASP.NET Core server with `MapEncinaDeadLetterEndpoints` once #2229 is merged (before that, the stand-in minimal API of Phase 2); no database fixture, because the stand-in answers over a substituted manager. **Load** and **Benchmark**: justification files.
2. **Per-file targets** (measured at work start; new files have no baseline; edited `EventIdRanges.cs` is measured then):

   | File | Unit | Guard | Contract | Property | Integration | Justification |
   |------|:---:|:---:|:---:|:---:|:---:|---------------|
   | `Commands/EncinaCommandFailure.cs` | 100 | 0 | 100 | 90 | 0 | A pure function with no reference parameter; the contract test pins the table and the property test covers every status |
   | `Commands/EncinaCommandsOptions.cs` | 95 | 0 | 0 | 0 | 0 | Validation paths run in unit tests; no parameterized public member |
   | `Commands/EncinaCommandsBuilder.cs` | 95 | 85 | 0 | 0 | 80 | Real model in unit tests; the integration test runs the command |
   | `Commands/EncinaCommandBuilderExtensions.cs` | 95 | 90 | 0 | 0 | 0 | Run over the real `DistributedApplication.CreateBuilder()` model |
   | `Commands/EncinaCommandRequestPreparer.cs` | 95 | 70 | 0 | 0 | 80 | Real `HttpRequestMessage`, stand-in server; guard reaches the argument checks of an internal type through `InternalsVisibleTo` |
   | `Commands/Diagnostics/EncinaCommandLog.cs` | 90 | 0 | 0 | 0 | 0 | Generated log methods, reached through command tests; no parameterized public member |

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT:
- Issue #419. Per-flag coverage per AGENTS.md section 9; targets live in .github/coverage-manifest/Encina.Aspire.Hosting.json.

TASK:
Write the tests, the manifest entries and the two justification files (load, benchmark).

KEY RULES:
- Tests execute the real code; the stand-in endpoint is a real minimal API, never a mock of HttpClient; no test swallows an exception.
- A justification file is allowed for load and benchmark only here.
- Shouldly through Encina.Testing.Shouldly; targets never go down; CRAP <= 10 on every changed method.

REFERENCE FILES:
- tests/Encina.UnitTests/Aspire
- tests/Encina.IntegrationTests/Observability/OpenTelemetry/OpenTelemetryIntegrationTests.cs
- .github/coverage-manifest/Encina.Aspire.Testing.json
```

</details>

---

### Phase 6: Documentation & Finalization (pull request 3)

<details>
<summary>Tasks</summary>

1. XML docs with examples on all public APIs; `PublicAPI.Unshipped.txt`.
2. A "Dashboard commands" section of `src/Encina.Aspire.Hosting/README.md` and a usage page (through `docs-writer`: the AppHost snippet with the secret parameter, the application side with `MapEncinaDeadLetterEndpoints`, the failure-code table, the six commands that are not delivered and their owner, the warning that visibility is not authorization); `docs/INVENTORY.md` line.
3. `changelog.d/419-aspire-dashboard-commands.added.md` (through `mechanical-fixer`).
4. `dotnet build --configuration Release` with 0 warnings; `dotnet test` with every flag at its target.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT:
- Issue #419 is implemented and tested.

TASK:
Write the documentation set of the Phase 6 tasks through the specialists that own them.

KEY RULES:
- Pages through docs-writer; fragments, PublicAPI lines and manifests through mechanical-fixer; never edit CHANGELOG.md; coverage figures are cited with covref markers, never typed; the reference application is named only as "the reference application".

REFERENCE FILES:
- changelog.d/README.md
- .claude/skills/encina-docs/SKILL.md
```

</details>

---

### Proposed split into pull requests (limit 15 files and 1500 lines each)

| PR | Content | Files | Approx. lines |
|----|---------|------:|--------------:|
| 1 | `EncinaCommandsOptions`, `EncinaCommandFailure`, `EncinaCommandsBuilder`, `EncinaCommandBuilderExtensions`, `EncinaCommandLog`, `EventIdRanges.cs` edit, `Encina` `PublicAPI.Unshipped.txt` edit, allocation test map edit, hosting `PublicAPI.Unshipped.txt` edit, hosting manifest edit, `Encina.json` manifest edit, unit tests for the mapper, the options and the builder (3 files), guard test file | 15 | 1,100 |
| 2 | `EncinaCommandRequestPreparer`, `AddRetryDeadLetters` edit, hosting `PublicAPI.Unshipped.txt` and manifest edits, unit, guard, contract, property and integration test files, `Encina.ContractTests.csproj` and `Encina.IntegrationTests.csproj` edits (references to the hosting project; a reference already added by #416 is not repeated) | 11 | 1,000 |
| 3 | README section, usage page, `docs/INVENTORY.md` line, changelog fragment, two justification files (load, benchmark) | 6 | 330 |

PR 1 needs #416 pull request 1; PR 2 needs PR 1 (its integration test uses the stand-in endpoint, so it does not wait for #2229); PR 3 is last. No interface changes, so no implementer is touched. A pull request that measures over 15 files or 1,500 lines when it is briefed is split again.

---

## Research

### Relevant Standards & Specifications

| Reference | What it defines | Use in this feature |
|-----------|-----------------|---------------------|
| `AGENTS.md` sections 3 and 6 | Fail closed, codes only in logs, 12 functions | D3, D4, Phase 3 |
| ADR-021 | EventId ranges registered first | `AspireHosting` 820-839 |
| ADR-054 | Observability conventions; exporters outside the library | D5 |
| ADR-062 | Error category contract | Phase 3 table of codes |
| ADR-046 | Persistent dead letter queue | What replay does |
| SPEC-000 section 6 and DEC-009 | Aspire integration that runs locally is in 1.0 | Scope |
| `Aspire.Hosting` 13.6.5 (`WithHttpCommand`, `WithCommand`, `CommandOptions`, `HttpCommandOptions`, `ResourceCommandVisibility`) | The command API | Phase 1 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IDeadLetterManager` (`ReplayAllAsync`, `dlq.already_replayed`, tenant rules) | `src/Encina.Messaging/DeadLetter/` | Behind the endpoint the command calls |
| Dead letter admin API | #2229 | The endpoint (`POST {prefix}/replay`) |
| Replay audit | #2013 | The audit of the operator's replay |
| `OutboxOrchestrator`, `SagaOrchestrator`, `SchedulerOrchestrator`, `MessagePurgeService` | `src/Encina.Messaging/` | The existing methods the six follow-up operations would call |
| `MessagingStoreMetrics` instrument names | `src/Encina.Messaging/Diagnostics/MessagingStoreMetrics.cs` | What the dashboard shows through OTLP |
| `Encina.Aspire.Hosting` | #416 | The package that receives the folder |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Aspire.Hosting` (`AspireHosting`, new) | 820-839, ids 820-824 used | From the assigned block 800-849 (registry `artifacts/decisions/plan-batch/eventid-blocks.md`); 825-839 free; `AspireServiceDefaults = (800, 819)` is #417's; 840-849 free; registered in `EventIdRanges.cs` and the allocation test map before use |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 7 | Options, mapper, builder, extension, preparer, log, `EventIdRanges.cs` edit |
| Public API and manifests | 4 | Hosting `PublicAPI.Unshipped.txt` and hosting manifest (each edited in two pull requests), `Encina` PublicAPI, `Encina.json` |
| Tests and project files | 12 | Nine test files (mapper, options and builder unit, guard; preparer unit and guard; contract; property; integration), allocation map edit, 2 csproj edits |
| Justification files | 2 | Load, benchmark |
| Documentation | 4 | README section, usage page, inventory line, changelog fragment |
| **Total** | **29 distinct files; 32 file touches across 3 pull requests** | The sum of the split table (15 + 11 + 6) is the authority (32) |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no [Obsolete], no compatibility layers.
- Issue #419: Aspire Dashboard commands, first the dead letter retry. Plan: docs/plans/aspire-dashboard-commands-implementation-plan-419.md; D1-D4 are recommended, pending the maintainer.

IMPLEMENTATION OVERVIEW (3 pull requests, each builds alone):
1. WithEncinaCommands, options, the failure mapper, the log 820-824 and the EventId range (Phase 1).
2. retry-dead-letters, the request preparer (credential, tenant) and result mapping (Phase 2).
3. README, usage page, inventory, changelog fragment, justification files (Phase 6).

KEY PATTERNS:
- WithHttpCommand with HttpCommandOptions; ResultMode None; the response body is never shown or logged; the failure is a code and a status number.
- The credential is a secret parameter sent in a header; without it the command never sends a request; visibility is not authorization.
- The tenant is an optional input sent verbatim in the configured header; a blank input sends nothing; default is never invented.
- Only commands whose application operation exists or is planned are delivered; the other six are an issue file.

REFERENCE FILES:
- src/Encina.Messaging/DeadLetter/IDeadLetterManager.cs
- docs/plans/deadletter-admin-api-implementation-plan-2229.md
- src/Encina/Diagnostics/EventIdRanges.cs
- .github/coverage-manifest/Encina.Aspire.Testing.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: commands read nothing repeatedly |
| 2 | OpenTelemetry | ⏭️ | Deferred as in the issue: the metrics the dashboard shows are exported by the application through the service defaults of #417 (open, planned); no metrics endpoint exists or is needed (D5) |
| 3 | Structured Logging | ✅ | Phase 1: `EncinaCommandLog`, EventIds 820-824 of `AspireHosting` |
| 4 | Health Checks | ❌ | Not applicable: the resource health check is #416; a command is enabled only while the resource is running |
| 5 | Validation | ✅ | Options, the replay cap and the tenant input are validated; the endpoint validates again |
| 6 | Resilience | ❌ | Not applicable: one request with a timeout and no automatic retry, because a replay re-sends business messages; the manager rejects a repeated row |
| 7 | Distributed Locks | ❌ | Not applicable: the lock of a replay is the manager's claim on the row (#2229, `ReplayInProgress`) |
| 8 | Transactions | ❌ | Not applicable: one HTTP call; atomicity is the endpoint's and manager's |
| 9 | Idempotency | ✅ | D6: a repeated click is safe (`dlq.already_replayed` becomes the `conflict` code) and nothing retries |
| 10 | Multi-Tenancy | ✅ | D4: an optional tenant input sent verbatim; a blank input sends no header; the reserved `default` segment is never invented; the manager stays fail closed without a tenant |
| 11 | Module Isolation | ❌ | Not applicable: dashboard module rows are #423 |
| 12 | Audit Trail | ⏭️ | Deferred as in the issue: the audit of operator replay is #2013 (open, planned), done by the manager, with the credential's principal as actor |

---

## Prerequisites & Dependencies

- **#416 pull request 1** (the hosting package) before pull request 1. #2229 is not needed to build or test (the stand-in endpoint), but an end-to-end click needs it merged and mapped by the application. #2013 adds the audit of operator replay.
- #417 supplies the telemetry the dashboard shows; #423 adds module rows; #421 is independent.
- The follow-up issue file for the six application operations is opened by the orchestrator with this plan.

---

## Next Steps

1. The maintainer answers D1, D2, D3 and D4 when work on #419 starts; the answers go into `## Maintainer Decisions` and the markers are removed.
2. Pull request 1 after #416 pull request 1; pull request 2; pull request 3 last.
