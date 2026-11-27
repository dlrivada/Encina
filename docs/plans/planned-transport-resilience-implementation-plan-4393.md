# Implementation Plan: Resilience decorators for the six planned transports, and the `IMessageTransport` that ADR-055 R7 assumes

> **Issue**: [#4393](https://github.com/dlrivada/Encina/issues/4393)
> **Type**: Feature (follow-up of #742)
> **Complexity**: High (7 phases, 6 planned transports, 7 pull requests)
> **Estimated Scope**: ~700 lines of production code + ~1,900 lines of tests (about 4,500 changed lines in total, thirteen pull requests of about 350 lines each)

---

## Summary

Extend the transport resilience of #742 to the six transports planned for 1.0 (GoogleCloudPubSub, AmazonEventBridge, Pulsar, Redis.Streams, ActiveMQ, Dapr) when their packages land, and record the fact that the `IMessageTransport` interface named by [ADR-055](../architecture/adr/055-resilience-family.md) R7 does not exist. Each planned transport gets a `Resilient<Transport>MessagePublisher` decorator over its own publisher interface, an opt-in registration, and a row in the per-provider SDK retry table, all using the provider-keyed executor `IProviderResilience` that the #742 plan defines. The decision on `IMessageTransport` is a Dilemma asked once for the family with choice 2 of the #742 plan; this plan carries it to its outcome (an amendment of ADR-055 R7 that also covers consumption). Ownership of consumer-side resilience is settled here and in #4392 in the same words: #4392 owns the `IMessageConsumer` abstraction, the `ResilientMessageConsumer` decorator and the consumers of the eight transports on `main`; this plan owns, for each of the six planned transports, both the publisher decorator (outbound) and the adoption of `IMessageConsumer` with its `AddEncina<Transport>ConsumerResilience()` registration (inbound), as a second pull request per transport (choice 6).

What exists today (read in `origin/main` at `0a7ce2ae1`):

- None of the six packages is on `main`: `ls src` has no `Encina.GoogleCloudPubSub`, `Encina.AmazonEventBridge`, `Encina.Pulsar`, `Encina.Redis.Streams`, `Encina.ActiveMQ` or `Encina.Dapr.PubSub`. Their plans are on `main` (#237, #238, #239, #240, #241, #242) and each defers resilience to #742 (its cross-cutting row 6).
- The publisher interfaces those plans name: `IGoogleCloudPubSubMessagePublisher`, `IAmazonEventBridgeMessagePublisher` (publish only), `IPulsarMessagePublisher` (with `IPulsarMessageConsumer` as the issue's alternative), `IRedisStreamsMessagePublisher` (with `IRedisStreamsMessageConsumer` as the issue's alternative), `IActiveMQMessagePublisher` (with `IActiveMQMessageConsumer` as the issue's alternative) and `IDaprMessagePublisher`, in the package `Encina.Dapr.PubSub` (registration `AddEncinaDaprPubSub`), not in `Encina.Dapr`. These are plan names: the provider PR reads the merged code.
- `git grep "interface IMessageTransport" -- src` returns nothing; the eight existing transports each have their own publisher interface (#742 plan, "What exists today").
- The executor, options and registration are the plan of #742 (on `main`, not implemented): `IProviderResilience.ExecuteAsync<T>(string provider, string operation, Func<CancellationToken, ValueTask<Either<EncinaError, T>>> action, CancellationToken)`, `ProviderResilienceOptions.ForProvider(name, ...)`, `AddEncinaProviderResilience()`, the abstract contract suite in `tests/Encina.ContractTests/Messaging/` and EventIds 970-984.
- The issue #4363 (open) also lists "resilience decorators" for the new transports Dapr, ZeroMQ, Event Hubs and Kinesis (its item 3): Dapr is named by both issues (choice 3).

What this plan delivers: per planned transport, the decorator, its registration extension, the guard, unit, DI, contract and integration tests, its PublicAPI lines, manifest rows, README line and changelog fragment; and once, the ADR-055 R7 amendment and the transport resilience page rows.

- **Affected packages**: `Encina.Resilience` (planned, no change), the six planned packages (as each lands), `Encina.Messaging` (no code: choice 1 option A). The ADR amendment is documentation.
- **Provider category**: Transports (AGENTS.md section 5), the planned six of SPEC-000 DEC-009; the cloud-tied ones are tested with local emulators.
- **Estimated size**: 13 pull requests of at most 15 files and 1,500 lines (split in "Implementation Phases").
- **Prerequisites**: #742 implementation, #4380, #4382 and the package of each transport, described in "Prerequisites & Dependencies".

### Shared Pieces and Ownership (family resilience follow-ups: #4391, #4392, #4393, #4394, #4395)

| Shared piece | Owner | Used by |
|--------------|-------|---------|
| `Encina.Resilience` project, registry, options front-ends | #4380 (plan, by number) | #742 and this plan |
| `IProviderResilience`, `ProviderResilienceOptions`, the abstract contract suite, EventIds 970-984, `AddEncinaProviderResilience` | #742 (plan on `main`, by number) | this plan |
| The six packages and their publisher interfaces | #237, #238, #239, #240, #241, #242 (plans on `main`, by number) | this plan wraps them |
| The transport conformance base `TransportConformanceContractBase` | #240 (plan on `main`, by number) | the contract classes of this plan may derive from the #742 suite or the conformance base |
| One decorator per planned transport and its registration | **This plan (#4393)** | none |
| The `IMessageTransport` decision record (ADR-055 R7 amendment) | **This plan (#4393)**, answered together with #742 choice 2 | #742 |
| `IMessageConsumer`, `IConsumerSubscription`, `ResilientMessageConsumer`, the abstract consumer contract suite | #4392 (plan, by number) | the consumer adoption PRs of this plan |
| Consumer adoption and consumer resilience registration for the six planned transports | **This plan (#4393)** | none |

---

## Design Choices

<details>
<summary><strong>1. What the decorators wrap: each package's publisher interface, or a new `IMessageTransport`</strong></summary>

ADR-055 R7 says the decorators "wrap `IMessageTransport`". The type does not exist; every transport has its own interface with its own operations. This is the same question as choice 2 of the #742 plan, asked once for the family.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One decorator per publisher interface, delegating to `IProviderResilience.ExecuteAsync`; ADR-055 R7 is amended to read "the publisher interface of each transport"** | No change to any public interface; operations such as `JetStreamPublishAsync` or `ScheduleAsync` stay; matches the #742 plan | One decorator class per transport (14 in total with #742); the contract suite needs an adapter per transport |
| **B) Introduce `IMessageTransport` in `Encina.Messaging`, make the 8 existing and the 6 planned publishers implement it, decorate once** | One decorator; the category rule (send and publish on every transport) becomes a type | A breaking redesign of 8 public interfaces and of 6 plans; no lowest-common interface fits operations such as scheduling, request/reply or a Pulsar key-shared publish; a design for its own issue |

### Chosen Option: **A - Decorators over each interface** (recommended, pending the maintainer)

### Rationale

- A wins when transport-specific operations must stay; B wins if the maintainer wants the transport category to be one type, pre-1.0 and at the cost of reopening the six plans.
- Under B this plan is replaced by the issue of the common interface; the ADR amendment would then record the new type instead.

- **Class:** Dilemma

</details>

<details>
<summary><strong>2. When a decorator is built: a separate pull request after its package lands, or inside the package's own pull requests</strong></summary>

The six packages are planned in 6 or 7 pull requests each; their plans defer resilience to #742.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A separate decorator pull request per package, opened after that package's pull requests and after the #742 executor are merged** | Each plan keeps its scope; the decorator follows the final publisher interface; no ordering constraint between the package and #742 | One more pull request per package |
| **B) The package's own last pull request adds its decorator** | No extra pull request when #742 has merged first | Changes six merged plans; the package pull request grows beyond its limits when the decorator, six tests and an emulator class are added |

### Chosen Option: **A - Separate pull request per package** (recommended, pending the maintainer)

### Rationale

- A wins when the packages and #742 land in unknown order, which is the case today (none is implemented); B wins if #742 is merged before the packages and the maintainer wants fewer pull requests.

- **Class:** Dilemma

</details>

<details>
<summary><strong>3. Who owns the Dapr decorator: this issue or #4363</strong></summary>

#4393 lists Dapr among the six; #4363 (open) lists "the four new transports Dapr, ZeroMQ, Event Hubs and Kinesis" and puts the resilience decorators in its third item with tenant and module metadata and contract adoption.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) This plan owns every decorator of the six, including Dapr; #4363 item 3 keeps ZeroMQ, Event Hubs and Kinesis** | One conformance suite and one provider-key convention for the planned transports; a single owner per decorator | #4363's text must drop Dapr from its item 3 |
| **B) #4363 owns the Dapr decorator** | The Dapr package gets its tenant metadata, contracts and decorator in one family of pull requests | Two owners of the same category rule; this plan covers five, not six, so its acceptance criterion changes |

### Chosen Option: **A - This plan owns the six** (recommended, pending the maintainer)

### Rationale

- A wins when the category rule (every transport has the decorator) is verified in one place; B wins when the Dapr work is sequenced as one unit by #4363. The overlap is reported to the orchestrator, who decides whether to edit #4363; this plan edits no issue.

- **Class:** Dilemma

</details>

<details>
<summary><strong>4. Tests per transport: the shared contract suite and an emulator or container</strong></summary>

SPEC-000 DEC-009 puts the six in 1.0 and says the cloud-tied ones are tested with local emulators; AGENTS.md section 5 asks the same behavior of every transport and section 9 asks integration classes to use shared `[Collection]` fixtures, never a per-class fixture. Each package's plan already names its fixture: `PubSubEmulatorFixture` (#237), `LocalStackFixture` (#238, shared with SQS), `PulsarFixture` (#239), `RedisFixture` (#240, exists), `ArtemisFixture` (#241) and `DaprSidecarFixture` (#242).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Each provider pull request adds a derived class of the #742 abstract contract suite (a fake inner publisher that fails transiently) and one integration class on the package's own collection fixture (publish succeeds after a restart; the breaker opens while the container is stopped)** | Proves the category rule and the real SDK behavior | Containers and emulators in CI |
| **B) Contract class for all six; an integration class only for the two cloud-tied transports (GoogleCloudPubSub and AmazonEventBridge), which SPEC-000 DEC-009 requires to be tested with local emulators; for Pulsar, Redis.Streams, ActiveMQ and Dapr the integration class is skipped with a justification file** | Fewer containers in CI; the DEC-009 requirement is met | AGENTS.md section 9 allows skipping integration tests of non-database features only with a justification, so each of the four needs a written one, and the SDK facts of those four stay unproven by a test |

### Chosen Option: **A - Contract and integration per transport** (recommended, pending the maintainer)

### Rationale

- For the two cloud-tied transports an emulator-based integration class is required by SPEC-000 DEC-009 under both options. For the other four, A wins when the SDK retry and closed-broker facts of the checklist must be proven by a test; B wins when the contract suite is judged enough and CI time matters. Integration classes use shared `[Collection]` fixtures, never a per-class fixture (AGENTS.md section 9).

- **Class:** Dilemma

</details>

<details>
<summary><strong>5. The provider key of each decorator</strong></summary>

ADR-055 settled points: the circuit-breaker state lives in a per-container singleton; R7: the pipeline is keyed by provider name.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One fixed lowercase constant per transport (`googlecloudpubsub`, `amazoneventbridge`, `pulsar`, `redisstreams`, `activemq`, `dapr`), declared in the decorator's own package and never derived from caller input** | Bounded, injective keys; one breaker per provider per container | One broker outage opens the breaker for every topic of the provider |
| **B) One breaker per provider and destination** | A bad topic does not stop the others | Keys from caller input grow the registry without a bound and break the injectivity of keys |

### Chosen Option: **A - Fixed key per provider**

### Rationale

- B keys by caller-supplied values, so it is not "keyed by provider name"; A is the only option that satisfies R7 (one constant per decorator). It is the same rule as choice 10 of the #742 plan. A breaker is not tenant-keyed (broker connectivity is shared), so the reserved `default` tenant segment of the sibling plans does not apply. It holds under every option of choices 1 to 4 and 6.
- **Class:** Settled
- **Settled by:** ADR-055 settled points ("circuit-breaker state lives in a per-container singleton, never in a static field") and R7 ("keyed by provider name or shard id")

</details>

<details>
<summary><strong>6. Who owns consumer resilience for the six planned transports</strong></summary>

#4392 builds `IMessageConsumer`, the `ResilientMessageConsumer` decorator and the consumers of the eight transports on `main`. The six planned transports have subscription surfaces in their own plans (the publisher interface with `SubscribeAsync`, or a separate consumer or subscriber interface). Without an owner their consumption would stay unwrapped.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) This plan owns it: each of the six gets, after #4392 and its package have merged, a second pull request with an `IMessageConsumer` adapter over its subscription surface, `AddEncina<Transport>ConsumerResilience()` and the consumer tests** | #4392 stays at its 12 pull requests; one owner per transport for both directions | This plan grows to 13 pull requests |
| **B) #4392 extends its scope to the six** | One plan for every consumer | #4392 becomes the 12 pull requests plus 6 more that depend on packages that are not on `main` |

### Chosen Option: **A - This plan owns the six consumer adoptions** (recommended, pending the maintainer)

### Rationale

- A wins because every planned-transport pull request waits on its package, which this plan already tracks; B wins if the maintainer wants the whole consumer story in one plan. The same text appears in #4392.

- **Class:** Dilemma

</details>

---

## Implementation Phases

Thirteen pull requests: **PR-1** Phase 1 (the decision record: the ADR-055 R7 amendment, covering publishers and consumers, and the page rows; it follows the maintainer's answers to choices 1 and 6 and may merge before any package); then, for each of the six planned transports, **a publisher decorator pull request** (Phases 2 and 3: PR-2, PR-4, PR-6, PR-8, PR-10, PR-12) and **a consumer adoption pull request** (Phase 2b: PR-3, PR-5, PR-7, PR-9, PR-11, PR-13, each after the decorator PR of the same transport and after #4392 PR-1 to PR-4), opened in the order the packages and #742 land; the first candidate is Redis.Streams, because its `RedisFixture` already exists in `tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures/`; the others follow in the order their packages merge. A provider pull request needs its package merged, the #742 executor and its abstract contract suite merged, and #4380 and #4382.

### Phase 1: Decision Record

<details>
<summary>Tasks</summary>

1. `docs/architecture/adr/055-resilience-family.md` (modified, `docs-writer`): a dated amendment under R7 stating the outcome of choice 1 and of #4392 choice 1 in one text: with the recommended options, "outbound operations are wrapped by a decorator over the publisher interface of each transport and inbound consumption by `ResilientMessageConsumer` over `IMessageConsumer`; there is no `IMessageTransport`"; with other answers, the text names the type that exists. No new ADR number is taken: the amendment is a dated blockquote note at the top of ADR-055, in the form of the dated amendments already at the top of ADR-050 (for example "Amendment 2026-10-24").
2. `docs/features/transport-resilience.md` (page of #742, modified, `docs-writer`): rows for the six planned transports with the status "decorator when the package lands".
3. `changelog.d` fragment: none in this PR (documentation only, no user-visible change).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina issue #4393, plan docs/plans/planned-transport-resilience-implementation-plan-4393.md. The maintainer has answered choice 1.
TASK: Amend ADR-055 R7 with the outcome and add the six rows to the transport resilience page.
KEY RULES: documentation through docs-writer only; no new ADR number; English; no hand-typed coverage figures.
REFERENCE FILES: docs/architecture/adr/055-resilience-family.md, docs/architecture/adr/050-modules-may-require-modules.md (the form of a dated amendment), .claude/skills/encina-docs/SKILL.md
```

</details>

### Phase 2: Provider Implementations (the Decorators)

<details>
<summary>Tasks</summary>

For each planned transport (the decorator PR, then task 7 as its consumer PR), in its package (paths use the package name from its plan; Dapr is `Encina.Dapr.PubSub`):

1. `Resilient<Transport>MessagePublisher.cs` (public sealed, implements the package's publisher interface; constructor `(I<Transport>MessagePublisher inner, IProviderResilience resilience)`, both public types, both null-guarded): every operation method calls `resilience.ExecuteAsync("<provider key>", "<operation>", ct => inner.Method(..., messageId ?? generated, ct), cancellationToken)`; the message id is assigned once before the first attempt (#742 choice 5) and each operation kind follows the rule of #742 choices 11 and 12 as answered; subscription methods and pure accessors pass through unwrapped here (consumption is Phase 2b); a caller's cancellation is handled as #742 choice 9 is answered (propagate, never counted against the breaker).
2. `ServiceCollectionExtensions.cs` (modified): `AddEncina<Transport>Resilience()`, order-independent with `AddEncina<Transport>()` (the inner registration becomes the concrete class plus a factory that wraps it when a marker singleton is present, as #742 choice 8), `TryAdd*`, null-guarded; the package's registration sets the provider's retry default from the SDK finding (below) before the application's own configuration.
3. `Encina.<Package>.csproj`: `ProjectReference` to `Encina.Resilience` (every transport package that offers the extension takes Polly transitively, the shape #742 choice 8 option A accepts).
4. SDK facts are a prerequisite of the provider pull request, not a part of this plan: before the pull request is opened its author reads the SDK and records, in the options default, the README and the page of #742, whether the SDK retries by default, how to turn that off, and which exception a closed broker raises (the checklist in "Research"). The rule for who owns the retry per provider is the one of #742 choice 6 as the maintainer answers it; the SDK's own retry is never left on where Encina retries (ADR-050, one retry layer).
5. `tests/Encina.ContractTests/Messaging/<Transport>FakePublisher.cs`: the fake inner publisher that fails transiently, one per transport because each publisher interface has its own operations (the cons of #742 choice 2 option A); counted in the file list.
6. Tests in the same PR: the decorator unit test (forwarding, id assigned once and constant across attempts, last `Left` returned), the guard test (constructor nulls), the DI test (`ValidateOnBuild`, `ValidateScopes`, any registration order), a derived class of the #742 abstract contract suite using the fake, and the integration class on the package's collection fixture (choice 4).

7. Consumer adoption (Phase 2b, the second pull request of each transport, after its decorator PR and after the consumer abstraction and decorator of #4392 have merged): `<Transport>MessageConsumer.cs` (public sealed, implements `IMessageConsumer`; `Provider` is the same fixed key as the publisher decorator), an adapter over the package's own subscription surface (the publisher interface's `SubscribeAsync` or the separate consumer or subscriber interface its plan settled); Right acknowledges and Left asks for the native negative acknowledgement; codes `<TRANSPORT>_SUBSCRIBE_FAILED` (`Unavailable`) and `<TRANSPORT>_SUBSCRIBE_REJECTED` (`Internal`) as defined in #4392 Phase 1 task 5; `ServiceCollectionExtensions.cs` (modified) gains keyed `AddEncina<Transport>Consumer()` and order-independent `AddEncina<Transport>ConsumerResilience()` as in #4392; tests in the same PR: the consumer unit, guard and DI tests, a class derived from the #4392 abstract consumer contract suite and the integration class on the package's collection fixture (choice 4); the pause and negative-acknowledgement facts of the SDK are verified by the author before the pull request, as in task 4.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1 decided; the #742 executor and abstract contract suite are merged; the package <Package> is merged. Transport in this PR: <name>.
TASK: Add the decorator, its registration and tests for the transport, and verify its SDK retry behavior.
KEY RULES: the decorator has a public constructor taking only public types; every operation method of the interface is wrapped except subscriptions and pure accessors; the provider key is a fixed lowercase constant; the message id is assigned once before the first attempt; a caller's cancellation propagates and is never a Left or a breaker failure; registration is order-independent and proven with ValidateOnBuild and ValidateScopes; nothing changes for an application that does not call the resilience extension; record only SDK facts the PR verified.
REFERENCE FILES: docs/plans/transport-resilience-implementation-plan-742.md (Phase 3), src/Encina.Kafka/ServiceCollectionExtensions.cs once #742 has landed, docs/plans/<package plan>.md
```

</details>

### Phase 3: Configuration and DI

<details>
<summary>Tasks</summary>

1. Covered by Phase 2 task 2 for each transport; the shared `AddEncinaProviderResilience()` and `AddEncinaResilience(o => o.ForProvider("<key>", ...))` calls are the #742 and #4380 entry points and are not changed.
2. A DI test per transport builds the provider with `ValidateOnBuild` and `ValidateScopes` for `AddEncina<Transport>()` plus `AddEncina<Transport>Resilience()` in both orders, and with the resilience call alone absent (the inner publisher resolves unwrapped).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Phase 2 of this transport done.
TASK: Complete the registration tests.
KEY RULES: AGENTS.md section 3 registration completeness: every option type and dependency the extension resolves is registered; a database or other store of the application is never overridden.
REFERENCE FILES: AGENTS.md section 3, tests/Encina.UnitTests/Resilience/Providers/ once #742 has landed
```

</details>

### Phase 4: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. Multi-tenancy (function 10): the decorator takes no tenant or module and keys nothing by them (choice 5); the tenant tag on telemetry only with multi-tenancy (ADR-054 D4); tenant and module metadata of the new transports belongs to #4363.
2. Idempotency (function 9): the id assigned once and constant across attempts lets the consumer deduplicate through the Inbox.
3. Health checks: owned by each transport package; no breaker-state check is planned (the breaker is per process).

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: Phases 1 to 3 done.
TASK: Verify the cross-cutting rows and add only what they need.
KEY RULES: no tenant or module key; no new health check.
REFERENCE FILES: docs/architecture/adr/054-observability-conventions-and-trace-propagation.md
```

</details>

### Phase 5: Observability

<details>
<summary>Tasks</summary>

1. No new EventId and no new range: the executor of #742 logs retry, breaker and timeout events (ids 970-975 of its range `ResilienceProvider`) with the provider key as a property, and the decorators log nothing. Nothing is assigned by the orchestrator for this plan.
2. Spans and Polly's instruments follow R5 and #728; the provider key is a tag; no instrument of our own.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: Phases 1 to 4 done.
TASK: Confirm the decorator emits no log message of its own and that the provider key reaches the #742 telemetry.
KEY RULES: no message bodies, ids or EncinaError.Message in logs.
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

### Phase 6: Testing

<details>
<summary>Tasks</summary>

1. Unit (`tests/Encina.UnitTests/<Transport>/`): the decorator and the registration per transport.
2. Guard (`tests/Encina.GuardTests/<Transport>/`): the decorator constructor with a null of each parameter (no nullable parameter exists).
3. Contract (`tests/Encina.ContractTests/Messaging/<Transport>ResilienceContractTests.cs`): a class derived from the #742 abstract suite; runs real code against a fake inner that fails transiently.
4. Integration (`tests/Encina.IntegrationTests/MessageBrokers/<Transport>/`, `[Collection]` fixtures of the package's plan, `[Trait("Category", "Integration")]`): a publish succeeds after a broker restart and the breaker opens while the container is stopped.
5. Property, load, benchmark: not applicable; the properties of the executor are in #742's property tests; justification files are the #742 ones (the decorator adds one delegate call).

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Phases 1 to 5 done for this transport.
TASK: Write the Phase 6 tests.
KEY RULES: tests execute real package code; FakeTimeProvider, never Thread.Sleep; collection fixtures only (never IClassFixture, never a per-class container); Shouldly through Encina.Testing.Shouldly; a test never swallows an exception thrown by the code it drives.
REFERENCE FILES: tests/Encina.IntegrationTests/MessageBrokers/, docs/plans/transport-resilience-implementation-plan-742.md (Phase 6)
```

</details>

### Phase 7: Documentation and Finalization

<details>
<summary>Tasks</summary>

1. In each provider pull request: the package README line and the row of the SDK retry table in `docs/features/transport-resilience.md` (`docs-writer`), the changelog fragment `changelog.d/4393-<transport>-resilience.added.md`, `PublicAPI.Unshipped.txt` and the manifest rows (`mechanical-fixer`).
2. Verification per pull request: `dotnet build` 0 warnings; tests filtered to the transport; `--check-justifications`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT: Phases 1 to 6 green for this transport.
TASK: Documentation and verification.
KEY RULES: never edit CHANGELOG.md; documentation by docs-writer, metadata by mechanical-fixer; the SDK facts recorded contain only what the PR author verified; no hand-typed coverage figures (cite covref markers).
REFERENCE FILES: changelog.d/README.md, .claude/skills/encina-docs/SKILL.md
```

</details>

---

## Research

### Relevant Standards and Specifications

| Source | Relevance |
|--------|-----------|
| ADR-055 R2, R5, R7 and settled points | Provider-keyed pipelines, telemetry, decorators, singleton breaker state |
| ADR-050 (resilience row), ADR-029 | One retry layer; only retryable errors |
| SPEC-000 DEC-009 | The six planned transports are in 1.0; cloud-tied ones use local emulators |
| AGENTS.md section 5 (transports) | Consistency across every provider of the category |
| MassTransit and Wolverine | One resilience pipeline over all transports (comparable products named by the issue) |

### SDK facts: a stated prerequisite of each provider pull request

No SDK fact is claimed by this plan: the six SDKs are not referenced anywhere on `origin/main`, so there is nothing to read today. The author of each provider pull request answers these three questions from the SDK version that package pins, before opening the pull request, and records the answers in the options default, the package README and the page of #742:

| Transport | Questions answered before its pull request is opened |
|-----------|-------------------------------------------------------|
| GoogleCloudPubSub, AmazonEventBridge, Pulsar, Redis.Streams, ActiveMQ, Dapr.PubSub | Does the SDK retry by default? How is that retry turned off? Which exception does a closed broker raise? For Phase 2b also: how does the SDK pause delivery, and what is its negative acknowledgement? |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| `ResilientSecretReaderDecorator` | `src/Encina.Security.Secrets/` | The R7 precedent |
| `IProviderResilience`, `ProviderResilienceOptions`, the abstract contract suite | #742 plan, PR-1 and PR-3 | Called and derived |
| `TransportConformanceContractBase` | #240 plan | Shared conformance of the new transports |
| Package plans and fixtures | #237-#242 plans | Publisher interfaces and collection fixtures |
| `IErrorClassifier`, `OutboundRetryRules` | `src/Encina/Recoverability/` | Classification inside the executor |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| the six planned packages | none | The decorators log nothing; the executor of #742 logs with the provider key (970-975 of `ResilienceProvider`) |

### Estimated File Count

| PR | Files | Content |
|----|-------|---------|
| PR-1 | 2 | `docs/architecture/adr/055-resilience-family.md` (amendment), `docs/features/transport-resilience.md` (modified) |
| Publisher decorator PR of each transport (six) | 13 | `Resilient<Transport>MessagePublisher.cs`, `ServiceCollectionExtensions.cs` (modified), `Encina.<Package>.csproj` (modified), `<Transport>FakePublisher.cs` (test double), five tests (decorator unit, guard, DI, contract class, integration class), `PublicAPI.Unshipped.txt`, the package manifest, the README line, the changelog fragment |
| Consumer adoption PR of each transport (six) | 12 | `<Transport>MessageConsumer.cs`, `ServiceCollectionExtensions.cs` (modified), five tests (consumer unit, guard, DI, contract class derived from the #4392 suite, integration class), `PublicAPI.Unshipped.txt`, the package manifest, the README line, the changelog fragment, `Encina.<Package>.csproj` only if the ProjectReference to `Encina.Messaging` is absent (13) |

Drafted with the local model and reviewed against the code and the plans: the draft counted 11 files per package and used `Encina.Dapr`; the review added the changelog fragment and corrected the Dapr package to `Encina.Dapr.PubSub`, and the later review added the fake publisher and the consumer adoption PRs. Each PR is within 15 files and 1,500 lines (about 400 lines each). If a test project lacks the `ProjectReference` to the package (the package's own pull requests normally add it), the missing line is one more file in that project's edit and the PR stays at 14 or 15. No test double or implementer changes: the decorators are new and implement interfaces that are new in their packages.

Per-file targets (new files; baseline: none exists on `origin/main`, the six package manifests are created by the package plans):

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `Resilient<Transport>MessagePublisher.cs` | 90 | 20 | 80 | 0 | 50 (0 for a transport whose integration class is skipped under choice 4 option B) | Every wrapped operation by unit and contract tests with a fake inner; guard reaches the constructor nulls (two of about ten lines); integration reaches the same operations against the real broker; all rows are written in the same PR as their tests |
| `<Transport>MessageConsumer.cs` | 90 | 20 | 70 | 0 | 50 (same rule) | Callbacks driven by fakes in unit tests, the #4392 suite and the broker; guard reaches the constructor nulls |
| `ServiceCollectionExtensions.cs` of each package (modified) | 85 | 35 | 0 | 0 | 0 | The files are created by the package plans and have no row on `origin/main`, so no number can be quoted: if the package plan's row is higher the higher value applies (targets never go down), otherwise this PR writes these numbers; the DI test reaches the new registrations |

CRAP of each added method is at most 10 (each operation is one delegating call).

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina (pre-1.0, .NET 10, C# 14). Issue #4393: resilience decorators for the six planned transports and the IMessageTransport decision (ADR-055 R7). AGENTS.md rules apply. The #742 executor and abstract contract suite, #4380, #4382 and the transport package are merged first; if missing, stop and report.

IMPLEMENTATION OVERVIEW: (1) the ADR-055 R7 amendment and page rows once; (2) per transport, a decorator over its publisher interface and its opt-in registration; (3) unit, guard, DI, contract and integration tests; (4) SDK retry facts verified in each provider PR; (5) README, fragment, PublicAPI and manifest rows.

KEY PATTERNS: one retry layer; message id assigned once; fixed provider key; cancellation propagates; no tenant key; no new EventId; opt-in registration in any order; specialists for PublicAPI, changelog, manifest and docs.

REFERENCE FILES: docs/plans/transport-resilience-implementation-plan-742.md, docs/architecture/adr/055-resilience-family.md, the plan of each package
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable |
| 2 | OpenTelemetry | ✅ | ADR-055 R5 instruments through the #742 executor; provider key as a tag; tenant tag only with multi-tenancy |
| 3 | Structured Logging | ✅ | Reuses the #742 events; no new id |
| 4 | Health Checks | ⏭️ | Owned by each transport package (its own plan); no breaker-state check planned |
| 5 | Validation | ✅ | Options validated at startup through the #742 validator |
| 6 | Resilience | ✅ | The feature |
| 7 | Distributed Locks | ❌ | Not applicable |
| 8 | Transactions | ❌ | Not applicable |
| 9 | Idempotency | ✅ | Message id assigned once before the first attempt (#742 choice 5) |
| 10 | Multi-Tenancy | ✅ | No tenant key; tenant tag only with multi-tenancy; tenant metadata of the new transports is #4363 |
| 11 | Module Isolation | ❌ | Not applicable |
| 12 | Audit Trail | ❌ | Not applicable |

---

## Prerequisites & Dependencies

| Prerequisite | State | Effect |
|--------------|-------|--------|
| #742 implementation (executor, options, abstract contract suite) | Plan on `main`; not implemented | Blocks every provider PR (PR-2 to PR-13) |
| #4380 (`Encina.Resilience` foundation) and #4382 (token and deadline) | Plans on `main`; not implemented | Block #742, hence this plan |
| The six packages (#237 to #242) | Plans on `main`; none implemented | Each provider PR needs its package merged |
| #4363 (open) | Lists a Dapr decorator among its items | Choice 3; the orchestrator decides whether to edit it |
| #4392 (consumer-side resilience) | Plan, by number; its `IMessageConsumer`, decorator and abstract consumer suite (its PR-1 to PR-4) | Block the consumer adoption PRs (PR-3, PR-5, PR-7, PR-9, PR-11, PR-13); the publisher decorator PRs do not wait for it |
| The maintainer's answer to choice 1 (shared with #742 choice 2) | Pending | Blocks PR-1 |

---

## Next Steps

1. Commit this plan, open its PR and link it from #4393.
2. When #4393 is picked up, re-check the choices against today's code and ask the maintainer about the Dilemmas (choices 1 to 4 and 6).
3. Brief workers for PR-1, then for PR-2 to PR-13 as each package, the #742 executor and the #4392 abstraction land.
