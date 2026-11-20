# Implementation Plan: Encina.AmazonEventBridge, an Amazon EventBridge publishing transport with CloudEvents

> **Issue**: [#238](https://github.com/dlrivada/Encina/issues/238)
> **Type**: Feature
> **Verified against**: origin/main `0336d31c7` (2026-11-20)
> **Complexity**: High (7 phases, 7 pull requests of about 13, 14, 8, 9, 6, 3 and 8 files)
> **Estimated Scope**: about 850 lines of production code and about 1,700 lines of tests
> **Family**: transport-new-1 (#237, #238, #239, #240, #241). Sibling family transport-new-2 (#242, #610, #611, #612, #310) runs later; "Shared pieces" below says which plan owns what.

---

## Summary

The issue adds Amazon EventBridge as a transport: publishing events to an event bus in CloudEvents format, event rules with event patterns and targets, schema registry integration, archive and replay, partner event sources, a health check, OpenTelemetry and integration tests with LocalStack. State of `origin/main`:

- Scope: SPEC-000 section 6.1 keeps cloud items with a free local emulator inside 1.0 and names transport #238 (EventBridge on LocalStack) (SPEC-000 line 171, DEC-009). The issue is planned, not skipped.
- Transports are publisher-only except MQTT and Redis.PubSub; #1609 tracks the gap. There is no `IMessageTransport` in `src/` (verified by grep). `src/Encina.AmazonSQS` is the template: `EncinaAmazonSQSOptions` (a region string, `AllowInsecureHttp` and `AllowLocalEndpoints` for emulators, ambient credentials, no secret), an internal validator, `AddEncinaAmazonSQS`, a health check, and `LocalStackFixture` (`localstack/localstack:4.14.0`, collection `Cloud-LocalStack`) with SQS and SNS clients.
- EventBridge has no receive API: events leave the bus through rules to targets (SQS, Lambda, ...). The issue's own motivation is the AWS `IEventBridgePublisher`, a publisher. This transport is therefore publish-only, and "subscription management" is rule and target management (Design Choice 3).
- Verified on 2026-11-20 by downloading the package from NuGet: **AWSSDK.EventBridge 4.1.0.2** (Apache-2.0; net10.0; requires AWSSDK.Core 4.1.1.1 or later; its assembly has `PutEventsRequestEntry`, `PutEventsAsync`, `PutRuleAsync`, `PutTargetsAsync`, `CreateArchive`, `StartReplay`, `DescribeEventBusAsync` and `TraceHeader`). The repository pins `AWSSDK.SQS` 4.0.2.17 and `AWSSDK.SimpleNotificationService` 4.0.2.19, so pull request 1 confirms by restore that the higher Core floor produces no downgrade warning. `CloudNative.CloudEvents` 2.9.0 exists on NuGet (license and formatter package are confirmed in pull request 2).

Delivered: package `Encina.AmazonEventBridge` with `IAmazonEventBridgeMessagePublisher` (publish only), `EncinaAmazonEventBridgeOptions`, `AddEncinaAmazonEventBridge`, a CloudEvents formatter, an opt-in provisioner for rules, targets, dead-letter queues and archives, a health check, an `ActivitySource` and `Meter`, log messages in the block `AmazonEventBridge` 1090-1099, LocalStack integration tests, unit, guard and contract tests, and documentation. A put that EventBridge rejects entry by entry is a `Left`, never success.

**Provider category**: Messaging transports (10 existing, 6 planned; AGENTS.md section 5). The cloud-tied ones are tested with local emulators.

**Shared pieces (family)**: the shared transport conformance test base is owned by the Redis.Streams plan (#240, its Design Choice 8); this plan subclasses its publish scenarios in its sixth pull request. The family follow-up issue (adoption of the batch #252, priority #251 and outbox-adapter #2385 contracts, tenant and module headers) is `artifacts/issues/new-transports-tenant-and-shared-contracts.md`, written once by the #240 plan.

---

## Design Choices

<details>
<summary><strong>1. Package placement and name</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New package `Encina.AmazonEventBridge` with `IAmazonEventBridgeMessagePublisher`** | Pay-for-what-you-use; the name follows `IAmazonSQSMessagePublisher` of package `Encina.AmazonSQS` | The issue sketches `IEventBridgeMessagePublisher` |
| **B) The issue's interface name** | Matches the issue | Breaks the `I<Package suffix>MessagePublisher` convention of the other transports |

### Chosen Option: **A, a new package with the convention name**

### Rationale

The issue names the package; the interface name follows the convention of every existing transport.

**Class:** Trivial
**Settled by:** AGENTS.md section 5 (transports table) and `src/Encina.AmazonSQS/IAmazonSQSMessagePublisher.cs`

</details>

<details>
<summary><strong>2. Publish-only interface</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IAmazonEventBridgeMessagePublisher` with `PublishAsync` only** | Matches the service: no receive API exists | The transport rule "subscription management" is met by rules (Design Choice 3), not by a handler |
| **B) A `SubscribeAsync` that creates an SQS target and polls it** | Same call shape as MQTT and Redis.PubSub | Hides a second service (SQS) and its polling inside the EventBridge package; that is the SQS transport's consume work (#1609) |

### Chosen Option: **A, publish only**

### Rationale

EventBridge delivers to targets; the issue lists no consumer in its tasks or acceptance criteria and cites the AWS `IEventBridgePublisher`. Consuming an SQS target belongs to the SQS transport.

**Class:** Settled
**Settled by:** #238 (summary and acceptance criteria: publisher only; motivation cites the AWS `IEventBridgePublisher`)

</details>

<details>
<summary><strong>3. Rules, targets, dead letters and archives: provisioner or infrastructure as code (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The transport never touches rules; infrastructure as code owns them** | Least privilege; no admin permissions at runtime | The "event pattern matching support" criterion and the integration tests need their own provisioning |
| **B) An opt-in provisioner (`AutoCreateResources`, default off) that creates the bus, rules with an event pattern, targets with `DeadLetterConfig` and `RetryPolicy`, and an archive, idempotently** | Meets the criterion; the dead-letter queue of a target exists by construction; one-line setup for development and tests | Needs `events:*` admin permissions when on; one more component |

### Chosen Option: **B, opt-in provisioner** (recommended, pending the maintainer)

### Rationale

A wins in production under least privilege; B wins for development, tests and small deployments, and it composes with A because it is off unless asked for. The event pattern is validated as a JSON object locally and matched by the service; the dead-letter queue is an SQS ARN set on the target (the transport rule's DLQ requirement).

**Class:** Dilemma

</details>

<details>
<summary><strong>4. CloudEvents format (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `CloudNative.CloudEvents` (CNCF library) behind an internal formatter; the CloudEvent in structured JSON mode is the `Detail`** | Spec compliance by a maintained library | One more dependency; #260 will define Encina's own `ICloudEvent` abstraction |
| **B) An internal minimal CloudEvents 1.0 record (specversion, id, source, type, time, datacontenttype, data, subject)** | No dependency; trivial to replace when #260 lands | Spec details (extension attribute rules, time format) are hand written |
| **C) Wait for #260** | One CloudEvents design | #260 has no plan on `origin/main`; the criterion would stay open |

### Chosen Option: **A, the CNCF library behind an internal formatter** (recommended, pending the maintainer)

### Rationale

A wins when spec compliance matters more than dependency count; B wins if the maintainer wants no new package before #260 decides. The attribute mapping is the one of #260's table (id from the message id, type from the notification type name, source from the option, time from `TimeProvider`, subject from the correlation id when present); only the formatter file changes when #260 lands.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Features beyond the acceptance criteria (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Archive creation is a provisioner setting; replay stays an operator action; a partner bus is targeted through `EventBusName`; schema registry integration is deferred to an issue** | Everything the API makes cheap is in; the schema registry is a separate service with its own client | Replay and schema registry are not wrapped |
| **B) Also wrap `StartReplay` and the schema registry (discovery, code bindings)** | Complete against the task list | A second AWS client for schemas, replay state handling, and LocalStack coverage that is uncertain |
| **C) Only the acceptance criteria** | Smallest | Archive and partner sources are listed by the issue |

### Chosen Option: **A, archive and partner bus as settings; replay and schemas as follow-ups** (recommended, pending the maintainer)

### Rationale

Schema registry, archive and replay, and partner event sources appear under Features and tasks but not in the acceptance criteria. A wins for a first release; B wins for teams that replay from code. The integration pull request starts with a probe that records what LocalStack supports (archives, patterns); what it lacks is covered by unit tests and named in the tests, never skipped silently.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Credentials and endpoint</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Ambient credentials, no secret in options; `Region`, `EventBusName`, `Source`, an optional `ServiceUrl` validated with `EndpointValidator` and the `AllowInsecureHttp` and `AllowLocalEndpoints` flags with a startup warning** | The SQS precedent; nothing to leak | No explicit key in options |
| **B) Access keys in options** | Explicit | A secret in options |

### Chosen Option: **A, ambient credentials and a validated endpoint**

### Rationale

`EncinaAmazonSQSOptions` and its validator are the template for the emulator flags.

**Class:** Trivial
**Settled by:** `src/Encina.AmazonSQS/EncinaAmazonSQSOptions.cs` and `src/Encina.AmazonSQS/EncinaAmazonSQSOptionsValidator.cs`

</details>

<details>
<summary><strong>7. Partial failure of a put</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A rejected entry (`FailedEntryCount` greater than zero) is a `Left` with the entry's AWS error code in the details; never success** | The outbox retries; nothing is swallowed | A single publish has one entry, so the code is simple |
| **B) Log and return success** | Simple | Violates AGENTS.md section 3 |

### Chosen Option: **A, a `Left`**

### Rationale

AGENTS.md section 3 requires that a failure is never reported as success. Codes (ADR-062): `EVENTBRIDGE_PUBLISH_FAILED` `Internal` (R4, provider failure), `EVENTBRIDGE_ENTRY_REJECTED` `Internal`, `EVENTBRIDGE_EVENT_BUS_REQUIRED` `InvalidRequest` (R3); messages are constants; cancellation propagates.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (errors are never swallowed) and ADR-062 R4

</details>

<details>
<summary><strong>8. OpenTelemetry: per-package source or the shared design of #725 (Dilemma D8)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ActivitySource` and `Meter` named `Encina.AmazonEventBridge` in the package, registered by name in `Encina.OpenTelemetry`; the trace header travels in the put's `TraceHeader`** | Meets the acceptance criterion now; pattern of `Encina.Messaging.Outbox` | The name list in `Encina.OpenTelemetry` is edited by every transport plan |
| **B) Wait for #725** | One design | #725 has no plan on `origin/main` |

### Chosen Option: **A, per-package instrumentation** (recommended, pending the maintainer)

### Rationale

A wins because the criterion is in this issue; B wins if #725 should rename every source at once. Spans link to the consumer through the trace header (ADR-054 D2, no baggage); the tenant tag of ADR-054 D4 arrives with the multi-tenancy follow-up.

**Class:** Dilemma

</details>

<details>
<summary><strong>9. Multi-tenancy and module headers</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Defer to an issue: tenant and module CloudEvents extension attributes need #1164 and the route metadata of #2385** | One design for all transports | Not in this plan |
| **B) Stamp a tenant attribute now** | Early | Needs the reserved `default` tenant segment and an injective encoding no transport defines yet |

### Chosen Option: **A, defer**

### Rationale

The issue itself defers it ("Tenant context in headers"); the follow-up is an issue file.

**Class:** Settled
**Settled by:** #238 (cross-cutting table: multi-tenancy and module isolation deferred) and #1164

</details>

---

## Implementation Phases

### Phase 1: Package, options and DI (pull request 1)

<details>
<summary>Tasks</summary>

1. `src/Encina.AmazonEventBridge/Encina.AmazonEventBridge.csproj` (`AWSSDK.EventBridge`, `Microsoft.Extensions.*.Abstractions`, `LanguageExt.Core`, references to `Encina` and `Encina.Messaging`; `InternalsVisibleTo Encina.UnitTests`), `Encina.slnx` entry, `Directory.Packages.props` entry `AWSSDK.EventBridge` 4.1.0.2; the restore must show no downgrade warning for `AWSSDK.Core`.
2. `EncinaAmazonEventBridgeOptions`: `Region`, `EventBusName` (default `default`), `Source` (default `encina`), `DetailTypePrefix`, `ServiceUrl`, `AllowInsecureHttp`, `AllowLocalEndpoints`, `AutoCreateResources`, `EventPattern`, `TargetArn`, `DeadLetterQueueArn`, `EnableArchive`, `ProviderHealthCheck`; `ToString()` prints the bus name only; internal validator (as the SQS one, plus a pattern that must be a JSON object and ARNs that must be well formed).
3. `AddEncinaAmazonEventBridge(this IServiceCollection, Action<EncinaAmazonEventBridgeOptions>? configure = null)` with eager validation and `ValidateOnStart`; `IAmazonEventBridge` with `TryAddSingleton`.
4. Tests: options, validator and DI unit tests (DI with `ValidateOnBuild` and `ValidateScopes`), `AmazonEventBridgeGuardTests`; manifest; `UnitTests.csproj` and `GuardTests.csproj` references.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina.AmazonSQS is the cloud template. AWSSDK.EventBridge 4.1.0.2 is not yet in Directory.Packages.props and raises the AWSSDK.Core floor.
TASK: Create the Encina.AmazonEventBridge skeleton with options, validator and DI, plus unit and guard tests.
KEY RULES: No secret in options; EndpointValidator with the SQS emulator flags (warning once); TryAdd registrations only; DI test with ValidateOnBuild and ValidateScopes; no Obsolete.
REFERENCE FILES: src/Encina.AmazonSQS/EncinaAmazonSQSOptions.cs, src/Encina.AmazonSQS/EncinaAmazonSQSOptionsValidator.cs, src/Encina.AmazonSQS/ServiceCollectionExtensions.cs.
```

</details>

### Phase 2: Publishing and CloudEvents (pull request 2)

<details>
<summary>Tasks</summary>

1. `IAmazonEventBridgeMessagePublisher.PublishAsync<TMessage>(TMessage message, string? eventBusName = null, string? detailType = null, string? messageId = null, string? subject = null, CancellationToken cancellationToken = default) where TMessage : class` returning `ValueTask<Either<EncinaError, string>>` (the event id); `AmazonEventBridgeMessagePublisher` (scoped; `IAmazonEventBridge`, options, logger, `TimeProvider`): one `PutEvents` entry whose `Detail` is the CloudEvent (Design Choice 4), `Source` and `DetailType` from options and type, `TraceHeader` from the current activity, a rejected entry is a `Left` (Design Choice 7).
2. Internal `CloudEventFormatter` (id from the message id, time from `TimeProvider`).
3. `Log.cs` 1090-1093 (publishing, published, failed, entry rejected); `EventIdRanges.AmazonEventBridge = (1090, 1099)`; `AssemblyRanges` entry; `docs/engineering/eventid-block-registry.md` (through `docs-writer`): the row `| AmazonEventBridge | 1090-1099 | Transports | historical |` in the registered ranges table and the split of the reserved block 1050-1099 so that 1090-1099 is `registered` and no reserved block overlaps a registered range, as `EventIdRegistryAgreementTests` requires (the other four plans edit neighbouring rows of the same table, so the pull requests rebase on each other).
4. `Directory.Packages.props` entry for `CloudNative.CloudEvents` (and its System.Text.Json formatter) and the csproj edit; tests `AmazonEventBridgeMessagePublisherTests`, `CloudEventFormatterTests`, guard edit, manifest edit.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: The package skeleton exists. Publishing follows the messageId convention of the other publishers.
TASK: Implement PublishAsync with a CloudEvents detail, the trace header and entry-level failure handling, with unit and guard tests.
KEY RULES: Named category: argument on every EncinaErrors call; constant messages, never ex.Message or the AWS error text; ids and payloads never logged; TimeProvider for the event time; EventIds only 1090-1099 registered first in EventIdRanges.cs; cancellation propagates; the AWS client is substituted in unit tests.
REFERENCE FILES: src/Encina.AmazonSQS/AmazonSQSMessagePublisher.cs, src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs.
```

</details>

### Phase 3: Provisioner for rules, targets, dead letters and archives (pull request 3)

<details>
<summary>Tasks</summary>

1. Internal `AmazonEventBridgeResourceProvisioner` (only when `AutoCreateResources`; run once at start by an `IHostedService` registered with `TryAdd`): `CreateEventBus` when the bus is not `default`, `PutRule` with `EventPattern`, `PutTargets` with `DeadLetterConfig` and a `RetryPolicy`, `CreateArchive` when `EnableArchive`; an already-exists answer is success.
2. Codes: `EVENTBRIDGE_PROVISION_FAILED` `Internal` (R4); a failed provisioning stops host start (fail closed on missing resources).
3. `Log.cs` 1094-1097 (provisioning started, rule created, provisioned, provisioning failed); options and DI edits.
4. Tests: `AmazonEventBridgeProvisionerTests`, `AmazonEventBridgeProvisionerHostedServiceTests`, guard edit, manifest edit.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Publishing exists. Rules, targets, dead-letter queues and archives are the transport's subscription management and DLQ.
TASK: Implement the opt-in provisioner and its hosted service.
KEY RULES: Off by default; idempotent; a failure stops start rather than reporting success; the hosted service resolves no scoped service; constant error messages; no exception message in logs.
REFERENCE FILES: src/Encina.AmazonSQS/ServiceCollectionExtensions.cs, AGENTS.md section 3 (registration completeness).
```

</details>

### Phase 4: Health check and observability (pull request 4)

<details>
<summary>Tasks</summary>

1. `Health/AmazonEventBridgeHealthCheck : EncinaHealthCheck` (`DefaultName = "encina-amazoneventbridge"`, tags `encina`, `messaging`, `eventbridge`, `ready`; `DescribeEventBus` within `ProviderHealthCheck.Timeout`; text names the exception type only), registered when enabled.
2. `Diagnostics/AmazonEventBridgeActivitySource` and `AmazonEventBridgeMetrics`: spans publish and provision; counters `amazoneventbridge.published`, `.rejected` with tags `bus`, `outcome`; `Encina.OpenTelemetry` gains `AddSource`/`AddMeter` lines.
3. Tests: `AmazonEventBridgeHealthCheckTests`, `AmazonEventBridgeDiagnosticsTests`; DI edit; manifest edit.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: Publish and provisioning exist. The issue requires a health check and traces and metrics.
TASK: Add the health check and instrumentation and register the names in Encina.OpenTelemetry.
KEY RULES: No exception message in health results or tags; links not baggage (ADR-054 D2); zero cost without a listener.
REFERENCE FILES: src/Encina.AmazonSQS/Health/AmazonSQSHealthCheck.cs, src/Encina.Messaging/Diagnostics/OutboxActivitySource.cs, src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs.
```

</details>

### Phase 5: Cross-cutting integration

<details>
<summary>Tasks</summary>

1. Verify each row of the matrix below against the code; deferred rows point to the family issue file and to the follow-up issue files of Design Choice 5.
2. Confirm every code has its ADR-062 category and no log call carries a message id, payload or `EncinaError.Message`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: EventBridge publishes, provisions, reports health and emits telemetry.
TASK: Verify the cross-cutting matrix against the code.
KEY RULES: AGENTS.md sections 3, 6 and 7; ADR-062 categories.
REFERENCE FILES: AGENTS.md section 6, docs/architecture/adr/062-error-category-contract.md.
```

</details>

### Phase 6: LocalStack integration and contract tests (pull requests 5 and 6)

<details>
<summary>Tasks</summary>

1. Pull request 5 (6 files): `LocalStackFixture` gains an `IAmazonEventBridge` client (the image stays `localstack/localstack:4.14.0`, pinned by #2699), `TestInfrastructure.csproj` references `AWSSDK.EventBridge`, `IntegrationTests.csproj` references the package, `tests/Encina.IntegrationTests/MessageBrokers/AmazonEventBridge/AmazonEventBridgePublisherIntegrationTests.cs` and `AmazonEventBridgeFeatureProbeTests.cs` in the existing `Cloud-LocalStack` collection (`[Trait("Category","Integration")]`, `[Trait("Transport","AmazonEventBridge")]`): the provisioner creates a rule and an SQS target, a publish arrives in the queue as a CloudEvent with the message id, a rejected entry is a `Left`, the probe records archive and pattern support; manifest.
2. Pull request 6 (3 files): `tests/Encina.ContractTests/MessageBrokers/AmazonEventBridge/AmazonEventBridgeConformanceTests.cs` subclassing the publish scenarios of the shared base (owned by #240; EventBridge has no subscribe scenario), `ContractTests.csproj` reference, manifest edit; it lands after #240's contract pull request.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: SPEC-000 requires EventBridge to be tested against LocalStack through the shared collection.
TASK: Extend the LocalStack fixture, add the integration and probe tests and the conformance subclass.
KEY RULES: Reuse the shared Cloud-LocalStack collection and its pinned image, never a per-class container; ClearAllDataAsync-style cleanup in InitializeAsync; no Thread.Sleep (bounded waits); Shouldly through Encina.Testing.Shouldly; a feature LocalStack does not support is asserted at unit level and named in the test.
REFERENCE FILES: tests/Encina.TestInfrastructure/Fixtures/LocalStackFixture.cs, tests/Encina.IntegrationTests/MessageBrokers/AmazonSQS/AmazonSQSPublisherIntegrationTests.cs, tests/Encina.IntegrationTests/MessageBrokers/AmazonSQS/LocalStackCollection.cs.
```

</details>

### Phase 7: Documentation and finalization (pull request 7)

<details>
<summary>Tasks</summary>

Per-flag targets (entered in `.github/coverage-manifest/Encina.AmazonEventBridge.json` in the pull request that adds each file):

| File | unit | guard | contract | property | integration |
|------|:---:|:---:|:---:|:---:|:---:|
| `AmazonEventBridgeMessagePublisher.cs` | 85 | 20 | 50 | 0 | 65 |
| `CloudEventFormatter.cs` | 95 | 0 | 0 | 0 | 0 |
| `AmazonEventBridgeResourceProvisioner.cs` | 85 | 0 | 0 | 0 | 60 |
| `EncinaAmazonEventBridgeOptions.cs`, `...OptionsValidator.cs` | 100 | 0 | 0 | 0 | 0 |
| `ServiceCollectionExtensions.cs` | 85 | 55 | 0 | 0 | 0 |
| `Health/AmazonEventBridgeHealthCheck.cs` | 90 | 40 | 0 | 0 | 50 |
| `Diagnostics/*.cs` | 90 | 0 | 0 | 0 | 0 |
| `IAmazonEventBridgeMessagePublisher.cs`, `Log.cs` | 0 | 0 | 0 | 0 | 0 |

| File and flag | Justification |
|---------------|---------------|
| Publisher unit 85, integration 65 | Unit tests substitute the AWS client for each branch; the integration class publishes through a real rule to an SQS target on LocalStack, leaving connection-failure branches. |
| Formatter unit 95 | Pure mapping code with no external service. |
| Provisioner integration 60 | LocalStack accepts creation calls; features it ignores are unit tested only. |
| Publisher guard 20, contract 50 | Guard tests reach only argument checks; the conformance base runs the publish scenarios only. |
| Options and validator unit 100 | Pure data and validation. |
| Health check integration 50 | One real bus read; the failing branch needs a broken service. |
| Interface and `Log.cs` 0 | No executable lines or generated delegates. |

1. `src/Encina.AmazonEventBridge/README.md`, `docs/features/amazon-eventbridge.md`, the EventBridge section of `docs/messaging/transports.md` and `docs/INVENTORY.md` through `docs-writer`; changelog fragment `changelog.d/238-amazon-eventbridge.added.md` through `mechanical-fixer`. Transport packages carry no `PublicAPI.*.txt` today, so none is added.
2. Load and benchmark justification files under `tests/Encina.LoadTests/Messaging/AmazonEventBridge/` and `tests/Encina.BenchmarkTests/Messaging/AmazonEventBridge/` (throughput belongs to the service).
3. Issue files `artifacts/issues/amazon-eventbridge-schema-registry-and-replay.md` (D5 option A follow-ups).
4. `dotnet build --configuration Release` with zero warnings; every flag at its target; CRAP of every new method at most 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT: The package is implemented and tested; documentation remains.
TASK: Enter the manifest targets with one-sentence justifications, write the two justification files, hand README, feature page, transports page, INVENTORY and the changelog fragment to the owning specialists.
KEY RULES: Targets never go down; no hand-typed coverage figures in docs; never edit CHANGELOG.md.
REFERENCE FILES: tests/Encina.LoadTests/Messaging/RedisPubSub/RedisPubSub.md, .github/coverage-manifest/Encina.AmazonSQS.json, changelog.d/README.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / Spec | Relevance | Notes |
|-----------------|-----------|-------|
| `AGENTS.md` sections 3, 5, 6, 7, 9 | Errors never swallowed, transport category, 12 functions, EventIds, coverage | All phases |
| SPEC-000 section 6.1 and DEC-009 | The EventBridge transport is in 1.0 and is tested with LocalStack | Scope |
| CloudEvents 1.0 (CNCF); #260 | Event envelope | Design Choice 4 |
| ADR-062, ADR-054 D2 and D4 | Error categories; trace links and tenant tags | Phases 2 to 4 |
| EventBridge `PutEvents`, rules and event patterns, targets with dead-letter queues, archives (named by the issue) | Native mechanisms | Design Choices 3 and 7 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `Encina.AmazonSQS` | `src/Encina.AmazonSQS/` | Template for cloud options, validator, DI, health check |
| `EndpointValidator` | `src/Encina/Validation/EndpointValidator.cs` | Service URL validation |
| `EncinaHealthCheck`, `ProviderHealthCheckOptions` | `src/Encina.Messaging/Health/` | Health check base |
| `LocalStackFixture`, `LocalStackCollection` | `tests/Encina.TestInfrastructure/Fixtures/LocalStackFixture.cs`, `tests/Encina.IntegrationTests/MessageBrokers/AmazonSQS/LocalStackCollection.cs` | Shared emulator, SQS client for the target queue |
| `EventIdRanges` | `src/Encina/Diagnostics/EventIdRanges.cs` | New field `AmazonEventBridge` |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `AmazonEventBridge` (new) | 1090-1099 | Fifth of five packages of block 1050-1099 (`reserved-pre-zone`, transport-new-1); 1090-1093 publish, 1094-1097 provisioning, 1098-1099 left for health and faults; verified free by repo-wide grep |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 13 | csproj, options, validator, interface, publisher, formatter, provisioner, hosted service, `Log`, DI, health check, 2 diagnostics |
| Tests | 16 | options, validator, DI, publisher, formatter, provisioner, hosted service, health, diagnostics, guard, 2 integration, conformance subclass, manifest |
| Shared edits | 9 | `Encina.slnx`, `Directory.Packages.props` (twice), 4 test `.csproj`, `EventIdRanges.cs`, allocation test, `Encina.OpenTelemetry`, `LocalStackFixture` |
| Documentation | 9 | README, feature page, transports page, INVENTORY, registry, changelog, 2 justification files, follow-up issue file (git-ignored) |
| **Pull request 1** | **13** | csproj, options, validator, DI, slnx, `Directory.Packages.props`, 2 test csproj, manifest, 3 unit tests, guard; about 550 lines |
| **Pull request 2** | **14** | interface, publisher, formatter, `Log`, `EventIdRanges.cs`, allocation test, registry, `Directory.Packages.props`, csproj edit, 2 unit tests, guard edit, manifest edit, DI edit; about 700 lines |
| **Pull request 3** | **8** | provisioner, hosted service, `Log` edit, options edit, DI edit, 2 unit tests, manifest edit; about 600 lines |
| **Pull request 4** | **9** | health check, 2 diagnostics, DI edit, `Encina.OpenTelemetry` edit, 2 unit tests, guard edit, manifest edit; about 500 lines |
| **Pull request 5** | **6** | `LocalStackFixture` edit, `TestInfrastructure.csproj`, `IntegrationTests.csproj`, 2 integration classes, manifest; about 500 lines |
| **Pull request 6** | **3** | conformance subclass, `ContractTests.csproj`, manifest; about 100 lines |
| **Pull request 7** | **8** | README, feature page, transports page, INVENTORY, changelog fragment, 2 justification files, manifest justifications; about 350 lines |

`IAmazonEventBridgeMessagePublisher` is new, so there are no implementers or `new X(` sites to update; `LocalStackFixture` is edited additively (callers use its existing members). Pull requests 1 to 5 and 7 build alone in order (each needs the previous one); pull request 6 needs the conformance base of #240.

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library; no backward compatibility, zero warnings, ROP (Either<EncinaError, T>). Transports are publisher-only except MQTT and Redis.PubSub; Encina.AmazonEventBridge adds an EventBridge publisher tested with LocalStack (SPEC-000 DEC-009).

IMPLEMENTATION OVERVIEW: PR1 package, options, DI, AWS package entry; PR2 publish with a CloudEvents detail, trace header, entry-level failure handling and the EventId block 1090-1099; PR3 opt-in provisioner for rules, targets with dead-letter queues and archives; PR4 health check and telemetry; PR5 LocalStack fixture extension and integration tests; PR6 conformance subclass (publish scenarios); PR7 documentation. Dilemmas D3, D4, D5 and D8 are implemented as the maintainer answers, the recommended options until then.

KEY PATTERNS: a rejected entry is a Left, never success; named ADR-062 category on every error; constant error messages; ids and payloads never logged; no secret in options; TimeProvider; targets never go down; CRAP <= 10.

REFERENCE FILES: src/Encina.AmazonSQS, src/Encina.Messaging/Health, tests/Encina.TestInfrastructure/Fixtures/LocalStackFixture.cs, src/Encina/Diagnostics/EventIdRanges.cs.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | A transport does not cache messages. |
| 2 | OpenTelemetry | ✅ | Per-package `ActivitySource` and `Meter` (Design Choice 8); the trace header travels in the put. |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` in `Log.cs`, EventIds 1090-1099; error codes and exception types only. |
| 4 | Health Checks | ✅ | `AmazonEventBridgeHealthCheck` describes the event bus. |
| 5 | Validation | ✅ | Options, ARNs and the event pattern are validated at startup; payload validation belongs to the producer's pipeline. |
| 6 | Resilience | ⏭️ | Retry and circuit breaking for transports is #742 (open); the AWS client's retry applies and targets carry a retry policy. |
| 7 | Distributed Locks | ❌ | Publishing needs no lock. |
| 8 | Transactions | ❌ | Acknowledgement based; a put is one call. |
| 9 | Idempotency | ✅ | The message id is the CloudEvent id; processing idempotency stays with the inbox. |
| 10 | Multi-Tenancy | ⏭️ | Tenant extension attributes need #1164 and #2385; family issue file. |
| 11 | Module Isolation | ⏭️ | Same issue file. |
| 12 | Audit Trail | ❌ | Transport is infrastructure; audit belongs to the outbox and inbox. |

---

## Prerequisites & Dependencies

- None blocking pull request 1. Pull requests 2 to 5 and 7 follow in order; pull request 6 waits for the conformance base of #240.
- **Related, not blocking**: #260 (CloudEvents abstraction; the formatter is the only file it replaces), #1609 (consuming an SQS target is the SQS transport's work), #251, #252 and #2385 (contracts adopted through the family issue file), #725, #742, #1164.
- **Sibling plans**: #237, #239, #240 and #241; transport-new-2 reuses the same conformance base.

---

## Next Steps

1. Ask the maintainer D3, D4, D5 and D8 when work on #238 starts; record the answers in `## Maintainer Decisions`.
2. Pull request 1: package skeleton (13 files).
3. Pull requests 2 to 4: publish (14), provisioner (8), health and telemetry (9).
4. Pull requests 5 to 7: LocalStack integration tests (6), conformance subclass (3), documentation (8).
