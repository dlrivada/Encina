# Delta scope of issue #16 (set rules-2026-10)

Reused from the original audit; do not re-derive it. Rules in this delta: see tools/ai/audit/pipeline-delta.json.

**Scope source:** the published record and the published stage files of the original audit (docs/knowledge/audits/16/stages/).

## Knowledge record (docs/knowledge/issues/16.md)

```yaml
schema: 1
nav_exclude: true
issue: 16
title: "[SPIKE] Implement Strategy pattern for Sagas (Choreography vs Orchestration)"
closed: 2025-12-23
state_reason: completed
outcome: delivered
type: spike
area: messaging
review: verified
packages:
prs:
linked_prs:
knowledge:
  - kind: decision
    statement: "The maintainer rejected a Strategy pattern that would unify Saga Orchestration and Choreography behind one interface; both remain separate, distinct systems in Encina.Messaging."
    current: yes
    sources:
      - "paraphrase: the maintainer decided not to implement a Strategy pattern unifying Sagas; Orchestration and Choreography remain separate, distinct systems (issue #16, closing comment by dlrivada, 2025-12-23)"
    destinations:
      - kind: docs
        status: done
        target: "docs/messaging/sagas.md"
      - kind: adr
        status: planned
        target: "artifacts/knowledge/remediation/16-docs-5-docs-messaging-sagas-md-the-decision-that-orchestration.md"
  - kind: decision
    statement: "The spike's documentation deliverable was fulfilled by commit 8301a07c, which added docs/messaging/sagas.md and docs/messaging/index.md; there was no linked pull request, only a direct commit referenced from the issue's timeline."
    current: yes
    sources:
      - "quote: \"docs: add comprehensive Saga patterns documentation\" (commit 8301a07c, 2025-12-23, referenced from issue #16)"
    destinations:
      - kind: docs
        status: done
        target: "docs/messaging/sagas.md"
  - kind: rule
    statement: "Strategy selection between Orchestration and Choreography is application-wide: users pick one approach for the whole application, and mixing both is discouraged except across separate bounded contexts."
    current: yes
    sources:
      - "paraphrase: application-wide selection, no mixing except via separate bounded contexts (issue #16, closing comment by dlrivada, 2025-12-23)"
    destinations:
      - kind: docs
        status: done
        target: "docs/messaging/sagas.md"
  - kind: rule
    statement: "No automatic migration exists between Orchestration and Choreography state models (CurrentStep/Data vs Events[]/Compensations[]); switching requires completing or compensating pending sagas, rewriting saga definitions, and changing configuration."
    current: yes
    sources:
      - "paraphrase: migration policy — no automatic migration between strategies (issue #16, closing comment by dlrivada, 2025-12-23)"
    destinations:
      - kind: docs
        status: done
        target: "docs/messaging/sagas.md"
  - kind: rejected-alternative
    statement: "Option A (a Strategy + Orchestrator pattern with both strategies behind one common interface) was rejected as artificial, given the incompatible state models of Orchestration and Choreography."
    current: yes
    sources:
      - "paraphrase: Option A rejected as artificial given incompatible state models (issue #16 body 'Options to Evaluate' and closing comment, 2025-12-23)"
    destinations:
      - kind: docs
        status: done
        target: "docs/messaging/sagas.md"
  - kind: rejected-alternative
    statement: "Option B (ship one strategy, add the second later) was moot: both Orchestration and Choreography already existed in the codebase at spike time, so there was no 'ship one, add later' path."
    current: yes
    sources:
      - "paraphrase: Option B is moot since both patterns already existed at spike time (issue #16 body and closing comment, 2025-12-23)"
    destinations:
      - kind: docs
        status: done
        target: "docs/messaging/sagas.md"
  - kind: rejected-alternative
    statement: "Option C (keep the current single implementation as-is) was refined rather than simply accepted: the decision added an explicit documentation commitment (decision flowchart, comparison table, FAQ) and an explicit application-wide, no-mixing rule, not merely leaving the code untouched."
    current: yes
    sources:
      - "paraphrase: Option C refined with an explicit documentation commitment and app-wide rule, not silent acceptance (issue #16, closing comment, 2025-12-23)"
    destinations:
      - kind: docs
        status: done
        target: "docs/messaging/sagas.md"
  - kind: gotcha
    statement: "docs/messaging/sagas.md (line 297) shows services.AddEncinaChoreography(options => ...) as a Choreography registration example, but no AddEncinaChoreography (or AddEncinaSagas) extension method exists anywhere in src/; the only registration surface today is the generic AddMessagingServices<...> in MessagingServiceCollectionExtensions.cs, which has no Choreography type parameters."
    current: yes
    sources:
      - "paraphrase: AddEncinaChoreography and StuckSagaTimeout referenced in docs/messaging/sagas.md:291-300 do not exist in src/ (audit #16 docs stage, finding 1, 2026-09-27)"
    destinations:
      - kind: docs
        status: planned
        target: "artifacts/knowledge/remediation/16-docs-1-docs-messaging-sagas-md-291-300-choreography-configuration.md"
  - kind: pending-work
    statement: "No reviewer-checklist entry asks reviewers to confirm new Saga code keeps Orchestration and Choreography separate, or that Choreography's documented registration surface matches its real API."
    current: yes
    sources:
      - "paraphrase: no reviewer-checklist entry found for this decision in .coderabbit.yaml, PR templates or CONTRIBUTING (issue #16 archivist stage, 2026-09-25)"
    destinations:
      - kind: review-checklist
        status: planned
        target: "orchestrator: candidate reviewer-checklist line for Saga Orchestration/Choreography separation and Choreography DI-registration accuracy (issue #16, not yet drafted)"
audit:
  checklist: 1
  date: 2026-09-27
  verdict: findings-tracked
  record: "docs/knowledge/audits/issue-16.md"
remediation:
```

## From the original archivist.md (docs/knowledge/audits/16/stages/archivist.md)

- `docs/messaging/sagas.md`, `docs/messaging/index.md` — created by commit `8301a07c` (verified via `git show --stat`), still present today at the same paths (verified via `Get-ChildItem`/`git log --follow`).
- `docs/messaging/toc.yml`, `docs/toc.yml` — created/updated by the same commit; both removed by `5d8b6d32` ("docs: migrate to just-the-docs theme with proper navigation", #916), a deliberate project-wide replacement of the toc.yml navigation mechanism with just-the-docs front matter. Nothing further to scope for these two files.
- `src/Encina.Messaging/Sagas/` (Orchestration: `SagaOrchestrator.cs`, `SagaNotFoundDispatcher.cs`, `IHandleSagaNotFound.cs`, `LowCeremony/SagaRunner.cs`) and `src/Encina.Messaging/Choreography/` (`IChoreographyStateStore.cs`, `IChoreographyEventBus.cs`, `IChoreographySaga.cs`, `IChoreographyState.cs`, `ChoreographyOptions.cs`, `ChoreographyErrorCodes.cs`) — the issue named no production code changes; these are the folders the decision is *about* (keep them separate). Confirmed both still exist as separate folders today with no shared interface/base class between them.
- `src/Encina.Messaging/MessagingServiceCollectionExtensions.cs` (`AddMessagingServices<...>`) — the actual DI registration surface for Saga/Outbox/Inbox/Scheduled stores today; scoped in because the issue's decision implied a documented `AddEncinaSagas`/`AddEncinaChoreography` pair that does not exist as such in code (see Destinations below).
- No ADR file exists for this decision (`docs/architecture/adr/` has no saga-strategy ADR); scope for that gap is a documentation/reviewer-checklist decision, not code.

## From the original code.md (docs/knowledge/audits/16/stages/code.md)

- `src/Encina.Messaging/Sagas/` (orchestration): `ISagaState.cs`, `ISagaStore.cs`, `ISagaStateFactory` (in `SagaOrchestrator.cs`), `SagaOrchestrator.cs`, `SagaNotFoundDispatcher.cs`, `ISagaNotFoundDispatcher.cs`, `IHandleSagaNotFound.cs`, `SagaNotFoundContext.cs`, `SagaErrorCodes.cs`, `LowCeremony/SagaRunner.cs`, `LowCeremony/ISagaRunner.cs`, `LowCeremony/SagaDefinition.cs`, `LowCeremony/SagaStepBuilder.cs` — all read in full or in the relevant part.
- `src/Encina.Messaging/Choreography/` (all 8 files: `IChoreographyStateStore.cs`, `IChoreographyEventBus.cs`, `IChoreographySaga.cs`, `IChoreographyState.cs`, `ChoreographyOptions.cs`, `ChoreographyErrorCodes.cs`, `IEventHandlerScope.cs`, `IEventReaction.cs`) — read in full.
- `src/Encina.Messaging/MessagingServiceCollectionExtensions.cs` — read in full (`AddMessagingServices`, `AddMessagingServicesCore`, `TryAddDefaultMessageSerializer`, `RegisterSoftDeleteServices`).
- **Scope correction / mandatory siblings** (AGENTS.md §5 provider coherence + the auditor brief's "siblings are mandatory"): the archivist's scope list stopped at the abstraction layer. Since `ISagaStore` is a provider-dependent feature, I additionally reviewed all of its implementations and the one decorator: `src/Encina.ADO.{SqlServer,PostgreSQL,MySQL}/Sagas/SagaStoreADO.cs`, `src/Encina.Dapper.{SqlServer,PostgreSQL,MySQL}/Sagas/SagaStoreDapper.cs` (SqlServer variant read in full; PostgreSQL/MySQL confirmed structurally identical via the earlier `git grep`-style listing, not re-read line by line), `src/Encina.EntityFrameworkCore/Sagas/SagaStoreEF.cs`, `src/Encina.MongoDB/Sagas/SagaStoreMongoDB.cs`, `src/Encina.OpenTelemetry/MessagingStores/InstrumentedSagaStore.cs` (decorator), and grepped its three siblings `InstrumentedOutboxStore.cs`/`InstrumentedInboxStore.cs`/`InstrumentedScheduledMessageStore.cs` for the same defect pattern. Also checked `MessagingConfiguration.cs` (grep for `UseChoreography`/`UseSagas`) and the ADO.NET connection-registration path (`Encina.ADO.SqlServer/ServiceCollectionExtensions.cs`) that feeds `SagaStoreADO`, plus the test fixture that stands in for it in integration tests (`tests/Encina.TestInfrastructure/Fixtures/SqlServerFixture.cs`).


