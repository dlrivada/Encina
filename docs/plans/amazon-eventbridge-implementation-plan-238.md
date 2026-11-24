# Implementation Plan: Encina.AmazonEventBridge, an Amazon EventBridge publishing transport with CloudEvents

> **Issue**: [#238](https://github.com/dlrivada/Encina/issues/238)
> **Type**: Feature
> **Verified against**: origin/main `0336d31c7` (2026-11-20)
> **Complexity**: High (7 phases, 7 pull requests of about 13, 15, 9, 14, 7, 3 and 7 files)
> **Estimated Scope**: about 850 lines of production code and about 1,700 lines of tests
> **Family**: transport-new-1 (#237, #238, #239, #240, #241). Sibling family transport-new-2 (#242, #610, #611, #612, #310) runs later; "Shared pieces" below says which plan owns what.

---

## Summary

The issue adds Amazon EventBridge as a transport: publishing events to an event bus in CloudEvents format, event rules with event patterns and targets, schema registry integration, archive and replay, partner event sources, a health check, OpenTelemetry and integration tests with LocalStack. State of `origin/main`:

- Scope: SPEC-000 section 6.1 keeps cloud items with a free local emulator inside 1.0 and names transport #238 (EventBridge on LocalStack) (SPEC-000 line 171, DEC-009). The issue is planned, not skipped.
- Transports are publisher-only except MQTT and Redis.PubSub; #1609 tracks the gap. There is no `IMessageTransport` in `src/` (verified by grep). `src/Encina.AmazonSQS` is the template: `EncinaAmazonSQSOptions` (a region string, `AllowInsecureHttp` and `AllowLocalEndpoints` for emulators, ambient credentials, no secret), an internal validator, `AddEncinaAmazonSQS`, a health check, and `LocalStackFixture` (`localstack/localstack:4.14.0`, collection `Cloud-LocalStack`) with SQS and SNS clients.
- EventBridge has no receive API: events leave the bus through rules to targets (SQS, Lambda, ...). The issue's own motivation is the AWS `IEventBridgePublisher`, a publisher. This transport is therefore publish-only, and "subscription management" is rule and target management (Design Choice 3).
- Verified on 2026-11-20 by downloading the package from NuGet: **AWSSDK.EventBridge 4.1.0.2** (Apache-2.0; net10.0; requires AWSSDK.Core 4.1.1.1 or later; its assembly has `PutEventsRequestEntry`, `PutEventsAsync`, `PutRuleAsync`, `PutTargetsAsync`, `CreateArchive`, `StartReplay`, `DescribeEventBusAsync` and `TraceHeader`). The repository pins `AWSSDK.SQS` 4.0.2.17 and `AWSSDK.SimpleNotificationService` 4.0.2.19, so pull request 1 confirms by restore that the higher Core floor produces no downgrade warning. `CloudNative.CloudEvents` 2.9.0 exists on NuGet (license and formatter package are confirmed in pull request 2).

Delivered: package `Encina.AmazonEventBridge` with `IAmazonEventBridgeMessagePublisher` (publish only), `EncinaAmazonEventBridgeOptions`, `AddEncinaAmazonEventBridge`, a CloudEvents formatter, an opt-in provisioner for rules, targets, dead-letter queues and archives, a health check, an `ActivitySource` and `Meter`, log messages in the block `AmazonEventBridge` 40690-40699, LocalStack integration tests, unit, guard and contract tests, and documentation. A put that EventBridge rejects entry by entry is a `Left`, never success.

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
<summary><strong>2. Publish-only interface (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IAmazonEventBridgeMessagePublisher` with `PublishAsync` only** | Matches the service: no receive API exists | The transport rule "subscription management" is met by rules (Design Choice 3), not by a handler |
| **B) A `SubscribeAsync` that creates an SQS target and polls it** | Same call shape as MQTT and Redis.PubSub | Hides a second service (SQS) and its polling inside the EventBridge package; that is the SQS transport's consume work (#1609) |

### Chosen Option: **A, publish only** (recommended, pending the maintainer)

### Rationale

A wins because EventBridge delivers to targets, the issue lists no consumer in its tasks or acceptance criteria and cites the AWS `IEventBridgePublisher`, and consuming an SQS target belongs to the SQS transport (#1609); B wins for applications that want the same publish-and-subscribe call shape on every transport and accept an SQS dependency inside this package.

**Class:** Dilemma

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
<summary><strong>6. Credentials and endpoint (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Ambient credentials, no secret in options; `Region`, `EventBusName`, `Source`, an optional `ServiceUrl` validated with `EndpointValidator` and the `AllowInsecureHttp` and `AllowLocalEndpoints` flags with a startup warning** | The SQS precedent; nothing to leak | No explicit key in options |
| **B) Access keys in options** | Explicit | A secret in options |

### Chosen Option: **A, ambient credentials and a validated endpoint** (recommended, pending the maintainer)

### Rationale

A wins on hosts with an AWS credential chain (roles, instance profiles) where nothing needs protecting; B wins for a host outside AWS that must carry access keys in options (which would then need `[JsonIgnore]` and a `ToString()` override). `EncinaAmazonSQSOptions` and its validator are the precedent for the emulator flags, not a decision for EventBridge.

**Class:** Dilemma

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

AGENTS.md section 3 requires that a failure is never reported as success; this entry settles only that (a rejected entry is a `Left`). The codes follow ADR-062: `EVENTBRIDGE_PUBLISH_FAILED` `Internal` and `EVENTBRIDGE_ENTRY_REJECTED` `Internal` (R4: provider failures), `EVENTBRIDGE_EVENT_BUS_REQUIRED` `InvalidRequest` (R3). The AWS entry error code (a short code such as `ThrottlingException`, not text) is stored in the error details under `aws.error_code`, never in `EncinaError.Message`, which stays a constant and never reaches logs; cancellation propagates.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (errors are never swallowed; option B is excluded) and ADR-062 R4 (provider failures are `Internal`)

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

A wins because the criterion is in this issue; B wins if #725 should rename every source at once. Spans link to the consumer through the CloudEvents `traceparent` attribute (ADR-054 D2, no baggage; the put's X-Ray `TraceHeader` is set as well for X-Ray users); the tenant tag of ADR-054 D4 arrives with the multi-tenancy follow-up.

**Class:** Dilemma

</details>

<details>
<summary><strong>9. Multi-tenancy and module headers (Dilemma D9)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Defer to an issue: tenant and module CloudEvents extension attributes need #1164 and the route metadata of #2385** | One design for all transports | Not in this plan |
| **B) Stamp a tenant attribute now** | Early | Needs the reserved `default` tenant segment and an injective encoding no transport defines yet |

### Chosen Option: **A, defer** (recommended, pending the maintainer)

### Rationale

A wins because one design for all transports needs the persisted context of #1164 and the route metadata of #2385, and the issue's own table defers it ("Tenant context in headers"); B wins for a tenant-isolated deployment that needs the extension attribute before those land. The follow-up is an issue file.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Package, options and DI (pull request 1)

<details>
<summary>Tasks</summary>

1. `src/Encina.AmazonEventBridge/Encina.AmazonEventBridge.csproj` (`AWSSDK.EventBridge`, `Microsoft.Extensions.*.Abstractions`, `LanguageExt.Core`, references to `Encina` and `Encina.Messaging`; `InternalsVisibleTo Encina.UnitTests`), `Encina.slnx` entry, `Directory.Packages.props` entries `AWSSDK.EventBridge` 4.1.0.2, `CloudNative.CloudEvents` 2.9.0 and `CloudNative.CloudEvents.SystemTextJson` 2.9.0 (both Apache-2.0, both with a net10.0 target, checked on NuGet on 2026-11-20; they are referenced here and first used in pull request 2, so pull request 2 stays inside the file limit). `AWSSDK.Core` is not pinned in `Directory.Packages.props` (it arrives transitively with SQS and SNS 4.0.x); `AWSSDK.EventBridge` needs Core 4.1.1.1 or later, so the restore of this pull request must show no downgrade warning, and if it does an explicit `AWSSDK.Core` 4.1.1.1 entry goes in the same file.
2. `EncinaAmazonEventBridgeOptions`: `Region`, `EventBusName` (default `default`), `Source` (default `encina`), `DetailTypePrefix`, `ServiceUrl`, `AllowInsecureHttp`, `AllowLocalEndpoints`, `AutoCreateResources`, `EventPattern`, `TargetArn`, `DeadLetterQueueArn`, `EnableArchive`, `ProviderHealthCheck`; `ToString()` prints the bus name only; internal validator (as the SQS one, plus a pattern that must be a JSON object and ARNs that must be well formed).
3. `AddEncinaAmazonEventBridge(this IServiceCollection, Action<EncinaAmazonEventBridgeOptions>? configure = null)` with eager validation and `ValidateOnStart`; `IAmazonEventBridge` with `TryAddSingleton`.
4. Tests: options, validator and DI unit tests (DI with `ValidateOnBuild` and `ValidateScopes`), `AmazonEventBridgeGuardTests`; manifest with the package-level `targets` for unit and guard and the per-file targets and one-sentence justifications of the files added (table of Phase 7, rows of pull request 1; `coverage-report.cs --check-justifications` in `ci.yml` fails a manifest without them); `UnitTests.csproj` and `GuardTests.csproj` references. This pull request has 13 files: csproj, options, validator, DI, slnx, `Directory.Packages.props`, 2 test csproj, manifest, 3 unit tests, guard test.

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

1. `IAmazonEventBridgeMessagePublisher.PublishAsync<TMessage>(TMessage message, string? eventBusName = null, string? detailType = null, string? messageId = null, string? subject = null, CancellationToken cancellationToken = default) where TMessage : class` returning `ValueTask<Either<EncinaError, string>>` (the event id); `AmazonEventBridgeMessagePublisher` (scoped; `IAmazonEventBridge`, options, logger, `TimeProvider`): one `PutEvents` entry whose `Detail` is the CloudEvent (Design Choice 4), `Source` and `DetailType` from options and type, a rejected entry is a `Left` (Design Choice 7). The trace context travels twice: as the CloudEvents `traceparent` extension attribute in the detail (the W3C value of the current activity, which the consumer links to, Design Choice 8), and as the put's `TraceHeader`, which is an X-Ray value (`Root=1-<8 hex>-<24 hex>;Parent=<16 hex>;Sampled=<0|1>`) built from the same trace and span ids, not a W3C `traceparent`.
2. Internal `CloudEventFormatter` (id from the message id, time from `TimeProvider`) and, in the same file, the internal X-Ray header conversion (so it has no file of its own and is covered by `CloudEventFormatterTests`).
3. `Log.cs` 40690-40693 (publishing, published, failed, entry rejected); `EventIdRanges.AmazonEventBridge = (40690, 40699)`; `AssemblyRanges` entry; `docs/engineering/eventid-block-registry.md` (through `docs-writer`): the registered-ranges row `| AmazonEventBridge | 40690-40699 | Transports | zone |` and the Blocks row `| 40690-40699 | Transports | transport-new-1 (#238) | registered |` (see "EventId allocation" in Research; no code range below 10000 is added, as `EventIdRegistryAgreementTests` rule 5 requires); and `src/Encina/PublicAPI.Unshipped.txt` (through `mechanical-fixer`) gets the line `static readonly Encina.Diagnostics.EventIdRanges.AmazonEventBridge -> (int Min, int Max)`, because the range is a public field of `Encina`. The other four transport plans take the neighbouring slices, so their pull requests are merged one at a time (a new commit on a branch when a slot moves, never a rebase of a pushed branch).
4. Error codes in a new `AmazonEventBridgeErrorCodes.cs`. DI: `AddEncinaAmazonEventBridge` gains `TryAddScoped<IAmazonEventBridgeMessagePublisher, AmazonEventBridgeMessagePublisher>` here (registration completeness, AGENTS.md section 3), with the resolution test under `ValidateOnBuild` and `ValidateScopes` in `ServiceCollectionExtensionsTests`.
5. Tests: `AmazonEventBridgeMessagePublisherTests`, `CloudEventFormatterTests` (unit), guard edit, manifest edit with the targets and justifications of the files added. This pull request has 15 files: interface, publisher, formatter, error codes, `Log`, `EventIdRanges.cs`, `src/Encina/PublicAPI.Unshipped.txt`, allocation test, registry doc, DI edit, DI test edit, 2 unit tests, guard edit, manifest.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: The package skeleton exists. Publishing follows the messageId convention of the other publishers.
TASK: Implement PublishAsync with a CloudEvents detail, the trace header and entry-level failure handling, with unit and guard tests.
KEY RULES: Named category: argument on every EncinaErrors call; constant messages, never ex.Message or the AWS error text; ids and payloads never logged; TimeProvider for the event time; EventIds only 40690-40699 registered first in EventIdRanges.cs; cancellation propagates; the AWS client is substituted in unit tests.
REFERENCE FILES: src/Encina.AmazonSQS/AmazonSQSMessagePublisher.cs, src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs.
```

</details>

### Phase 3: Provisioner for rules, targets, dead letters and archives (pull request 3)

<details>
<summary>Tasks</summary>

1. Internal `AmazonEventBridgeResourceProvisioner` and `AmazonEventBridgeProvisionerHostedService` (only when `AutoCreateResources`; the hosted service runs the provisioner once at start). The hosted service is registered with `services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, AmazonEventBridgeProvisionerHostedService>())`, because `TryAddSingleton<IHostedService, X>` is skipped whenever another hosted service is already registered and the provisioner would silently never run; a DI test resolves `IEnumerable<IHostedService>` and asserts the provisioner service is present when the option is on and absent when it is off. The provisioner: `CreateEventBus` when the bus is not `default`, `PutRule` with `EventPattern`, `PutTargets` with `DeadLetterConfig` and a `RetryPolicy`, `CreateArchive` when `EnableArchive`; an already-exists answer is success.
2. Codes, added to `AmazonEventBridgeErrorCodes.cs`: `EVENTBRIDGE_PROVISION_FAILED` `Internal` (R4); a failed provisioning stops host start (fail closed on missing resources).
3. Acceptance mapping: the criterion "event pattern matching support" and the dead-letter queue of a target are met only when `AutoCreateResources` is on or infrastructure as code creates the rule and target; with the defaults the transport only publishes, and the feature page says so.
4. `Log.cs` 40694-40697 (provisioning started, rule created, provisioned, provisioning failed); DI edit (the hosted service and the provisioner). The options are not edited in this pull request: every setting the provisioner reads (`AutoCreateResources`, `EventPattern`, `TargetArn`, `DeadLetterQueueArn`, `EnableArchive`) already exists in pull request 1.
5. Tests: `AmazonEventBridgeProvisionerTests`, `AmazonEventBridgeProvisionerHostedServiceTests`, the hosted service resolution test in `ServiceCollectionExtensionsTests`, manifest edit with the targets and justifications of the files added (both new types are internal, so their guard target is 0 and no guard edit is needed). This pull request has 9 files: provisioner, hosted service, error codes edit, `Log` edit, DI edit, DI test edit, 2 unit tests, manifest.

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
3. The publisher and the provisioner (pull requests 2 and 3) are the emitters, so the publisher file and the provisioner file are edited here to start and stop the spans and increment the counters. Tests: `AmazonEventBridgeHealthCheckTests`, `AmazonEventBridgeDiagnosticsTests`, edits of `AmazonEventBridgeMessagePublisherTests` and `AmazonEventBridgeProvisionerTests`, guard edit (health check constructor); DI edit (health check only); manifest edit with the targets and justifications of the files added or changed. The `Encina.OpenTelemetry` edit (the new `AddSource`/`AddMeter` lines in `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs`) is tested by a new unit test `AmazonEventBridgeOpenTelemetryRegistrationTests` that builds the tracer and meter providers with `AddEncinaOpenTelemetry` and asserts the package source and meter names are subscribed; that file's existing per-file targets in `.github/coverage-manifest/Encina.OpenTelemetry.json` (unit 90, guard 35) are not lowered and need no new entry. This pull request has 14 files: health check, 2 diagnostics, publisher edit, provisioner edit, DI edit, `Encina.OpenTelemetry` edit, 5 unit tests (health, diagnostics, publisher edit, provisioner edit, OpenTelemetry registration), guard edit, manifest.

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

1. Pull request 5 (7 files, with a health check scenario): `LocalStackFixture` gains an `IAmazonEventBridge` client (the image stays `localstack/localstack:4.14.0`, pinned by #2699), `TestInfrastructure.csproj` references `AWSSDK.EventBridge`, `IntegrationTests.csproj` references the package, `tests/Encina.IntegrationTests/MessageBrokers/AmazonEventBridge/AmazonEventBridgePublisherIntegrationTests.cs`, `AmazonEventBridgeFeatureProbeTests.cs` and `AmazonEventBridgeHealthCheckIntegrationTests.cs` (one healthy bus description) in the existing `Cloud-LocalStack` collection (`[Trait("Category","Integration")]`, `[Trait("Transport","AmazonEventBridge")]`): the provisioner creates a rule and an SQS target, a publish arrives in the queue as a CloudEvent with the message id and the `traceparent` attribute, a rejected entry is a `Left`, the probe records archive and pattern support; manifest with the integration targets and justifications of the publisher, the provisioner, the hosted service and the health check (a feature LocalStack does not support lowers the provisioner target to 40 and is asserted at unit level).
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

Per-flag targets are entered in `.github/coverage-manifest/Encina.AmazonEventBridge.json` in the pull request that adds the tests of each flag, each with its one-sentence justification and with the package-level `targets` of that flag in the same pull request (`coverage-report.cs --check-justifications`, run by `ci.yml`, fails a manifest without them). The numbers are planned ceilings; each pull request replaces one with the measured value floored to a multiple of 5 when that is lower (never lowering a target already entered).

| Pull request | File | unit | guard | contract | property | integration | Justification |
|---|---|:---:|:---:|:---:|:---:|:---:|---|
| 1 | `EncinaAmazonEventBridgeOptions.cs`, `EncinaAmazonEventBridgeOptionsValidator.cs` | 100 | 0 | 0 | 0 | 0 | Pure data and validation; guard tests have no argument to check. |
| 1 | `ServiceCollectionExtensions.cs` | 85 | 10 | 0 | 0 | 0 | Unit tests build the provider for each option combination; guard tests reach only the null check of `services`. |
| 2 | `AmazonEventBridgeMessagePublisher.cs` | 85 | 20 | 0 | 0 | 0 | Unit tests substitute the AWS client for each publish branch; guard tests reach only argument checks. |
| 2 | `CloudEventFormatter.cs` | 95 | 0 | 0 | 0 | 0 | Pure mapping code with no external service. |
| 2 | `AmazonEventBridgeErrorCodes.cs`, `IAmazonEventBridgeMessagePublisher.cs`, `Log.cs` | 0 | 0 | 0 | 0 | 0 | Constants, an interface and generated delegates have no executable lines. |
| 3 | `AmazonEventBridgeResourceProvisioner.cs`, `AmazonEventBridgeProvisionerHostedService.cs` | 85 | 0 | 0 | 0 | 0 | A substituted AWS client reaches every creation and already-exists branch; the hosted service only delegates. |
| 4 | `Health/AmazonEventBridgeHealthCheck.cs` | 90 | 40 | 0 | 0 | 0 | A substituted client reaches the healthy and unhealthy branches; guard tests reach the constructor checks. |
| 4 | `Diagnostics/*.cs` | 80 | 0 | 0 | 0 | 0 | Counters and spans are exercised through publisher and provisioner unit tests; the static initializers are the remainder. |
| 5 | `AmazonEventBridgeMessagePublisher.cs` | 85 | 20 | 0 | 0 | 65 | Integration tests publish through a real rule to an SQS target on LocalStack, leaving connection-failure branches. |
| 5 | `AmazonEventBridgeResourceProvisioner.cs`, `AmazonEventBridgeProvisionerHostedService.cs` | 85 | 0 | 0 | 0 | 60 | LocalStack accepts creation calls; 40 when the probe shows it ignores archives or patterns, which are then unit tested only. |
| 5 | `Health/AmazonEventBridgeHealthCheck.cs` | 90 | 40 | 0 | 0 | 50 | One real bus description; the failing branch needs a broken service. |
| 6 | `AmazonEventBridgeMessagePublisher.cs` | 85 | 20 | 50 | 0 | 65 | The conformance base runs the publish scenarios only. |

1. `src/Encina.AmazonEventBridge/README.md`, `docs/features/amazon-eventbridge.md`, the EventBridge section of `docs/messaging/transports.md` and `docs/INVENTORY.md` through `docs-writer`; changelog fragment `changelog.d/238-amazon-eventbridge.added.md` through `mechanical-fixer`. Transport packages carry no `PublicAPI.*.txt` today, so none is added.
2. Load and benchmark justification files under `tests/Encina.LoadTests/Messaging/AmazonEventBridge/` and `tests/Encina.BenchmarkTests/Messaging/AmazonEventBridge/` (throughput belongs to the service).
3. Issue files `artifacts/issues/amazon-eventbridge-schema-registry-and-replay.md` (D5 option A follow-ups).
4. `dotnet build --configuration Release` with zero warnings; every flag at its target; CRAP of every new method at most 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT: The package is implemented and tested; documentation remains.
TASK: Write the two justification files (the manifest targets and their justifications were entered in pull requests 1 to 6), hand README, feature page, transports page, INVENTORY and the changelog fragment to the owning specialists.
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
| `AmazonEventBridge` (new) | 40690-40699 | 40690-40693 publish, 40694-40697 provisioning, 40698-40699 left for health and faults; fifth slice of the family |

**EventId allocation.** AGENTS.md section 7 allows new blocks only in the 10000+ zone and `EventIdRegistryAgreementTests` rule 5 fails any new range below 10000, so the family does not use the old 1050-1099 reservation. The Transports blocks 40000-40549 are reserved by #4007 for the existing transports (RabbitMQ 40000-40049 through SignalR 40500-40549) and 40550-40649 are claimed by the transport-new-2 plans (#242, #610, #611, #612, #310, 20 ids each, in their open pull requests), so the five plans of transport-new-1 take contiguous 10-id slices from 40650: `RedisStreams` 40650-40659 (#240), `Pulsar` 40660-40669 (#239), `ActiveMQ` 40670-40679 (#241), `GoogleCloudPubSub` 40680-40689 (#237), `AmazonEventBridge` 40690-40699 (this plan). Each plan's registering pull request adds its own registered-ranges row (Zone `zone`) and its own Blocks row with status `registered`, so no `reserved` row overlaps a registered range. If a slot has been taken by then, the registering pull request takes the next free slot and the ids of the plan shift by the same offset.

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 13 | csproj, options, validator, interface, publisher, formatter, provisioner, hosted service, `Log`, DI, health check, 2 diagnostics |
| Tests | 16 | options, validator, DI, publisher, formatter, provisioner, hosted service, health, diagnostics, guard, 2 integration, conformance subclass, manifest |
| Shared edits | 9 | `Encina.slnx`, `Directory.Packages.props` (twice), 4 test `.csproj`, `EventIdRanges.cs`, allocation test, `Encina.OpenTelemetry`, `LocalStackFixture` |
| Documentation | 9 | README, feature page, transports page, INVENTORY, registry, changelog, 2 justification files, follow-up issue file (git-ignored) |
| **Pull request 1** | **13** | csproj, options, validator, DI, slnx, `Directory.Packages.props`, 2 test csproj, manifest, 3 unit tests, guard; about 550 lines |
| **Pull request 2** | **15** | interface, publisher, formatter, `AmazonEventBridgeErrorCodes`, `Log`, `EventIdRanges.cs`, `PublicAPI.Unshipped.txt`, allocation test, registry (registered row and Blocks row), DI edit (scoped publisher), DI test edit, 2 unit tests, guard edit, manifest edit; about 750 lines |
| **Pull request 3** | **9** | provisioner, hosted service, error codes edit, `Log` edit, DI edit (`TryAddEnumerable`), DI test edit, 2 unit tests, manifest edit; about 600 lines |
| **Pull request 4** | **14** | health check, 2 diagnostics, publisher edit and provisioner edit (spans and counters), DI edit (health check only), `Encina.OpenTelemetry` edit, 5 unit tests (health, diagnostics, publisher edit, provisioner edit, OpenTelemetry registration), guard edit, manifest edit; about 750 lines |
| **Pull request 5** | **7** | `LocalStackFixture` edit, `TestInfrastructure.csproj`, `IntegrationTests.csproj`, 3 integration classes, manifest; about 600 lines |
| **Pull request 6** | **3** | conformance subclass, `ContractTests.csproj`, manifest; about 100 lines |
| **Pull request 7** | **7** | README, feature page, transports page, INVENTORY, changelog fragment, 2 justification files; about 350 lines |

`IAmazonEventBridgeMessagePublisher` is new, so there are no implementers or `new X(` sites to update; `LocalStackFixture` is edited additively (callers use its existing members). Pull requests 1 to 5 and 7 build alone in order (each needs the previous one); pull request 6 needs the conformance base of #240.

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library; no backward compatibility, zero warnings, ROP (Either<EncinaError, T>). Transports are publisher-only except MQTT and Redis.PubSub; Encina.AmazonEventBridge adds an EventBridge publisher tested with LocalStack (SPEC-000 DEC-009).

IMPLEMENTATION OVERVIEW: PR1 package, options, DI, AWS and CloudEvents package entries; PR2 publish with a CloudEvents detail, traceparent attribute and X-Ray trace header, entry-level failure handling and the EventId block 40690-40699; PR3 opt-in provisioner for rules, targets with dead-letter queues and archives; PR4 health check and telemetry; PR5 LocalStack fixture extension and integration tests; PR6 conformance subclass (publish scenarios); PR7 documentation. Dilemmas D2, D3, D4, D5, D6, D8 and D9 are implemented as the maintainer answers, the recommended options until then.

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
| 3 | Structured Logging | ✅ | `[LoggerMessage]` in `Log.cs`, EventIds 40690-40699; error codes and exception types only. |
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

1. Ask the maintainer D2, D3, D4, D5, D6, D8 and D9 when work on #238 starts; record the answers in `## Maintainer Decisions`.
2. Pull request 1: package skeleton (13 files).
3. Pull requests 2 to 4: publish (15), provisioner (9), health and telemetry (14).
4. Pull requests 5 to 7: LocalStack integration tests (7), conformance subclass (3), documentation (7).
