# Delta scope of issue #15 (set rules-2026-10)

Reused from the original audit; do not re-derive it. Rules in this delta: see tools/ai/audit/pipeline-delta.json.

**Scope source:** the published record and the published audit result (docs/knowledge/audits/issue-15.md) only: the original audit has no published stage files.

## Knowledge record (docs/knowledge/issues/15.md)

```yaml
schema: 1
nav_exclude: true
issue: 15
title: "[REFACTOR] Apply Orchestrator pattern to Event Sourcing (2 packages)"
closed: 2025-12-23
state_reason: completed
outcome: rejected-reasoned
type: refactor
area: eventsourcing
review: verified
packages:
  - Encina.EventStoreDB
  - Encina.Marten
prs:
linked_prs:
knowledge:
  - kind: rejected-alternative
    statement: "A single Orchestrator plus Provider abstraction (a new Encina.EventSourcing package with AggregateOrchestrator, IAggregateRepository and IEventStore shared by Encina.EventStoreDB and Encina.Marten, mirroring the pattern applied to Validation (#14); for Caching the pattern was proposed and rejected as unnecessary, #13) was rejected because the two stores are different architectural approaches (catch-up subscriptions and server-side projections versus inline projections, the async daemon and PostgreSQL-native features), not interchangeable providers."
    current: yes
    sources:
      - "quote: \"These aren't interchangeable implementations - they're **different architectural approaches**.\" (closing comment by dlrivada, https://github.com/dlrivada/Encina/issues/15#issuecomment-3687436427, 2025-12-23)"
    destinations:
      - kind: adr
        status: done
        target: "docs/architecture/adr/027-marten-as-the-event-sourcing-provider.md"
  - kind: decision
    statement: "The closing comment replaced #15 with three Strategy-pattern spikes (Event Sourcing #17, Sagas #16, Messaging #18, all opened the same day and all closed). None produced a Strategy-to-Orchestrator-to-Provider abstraction; the event-sourcing question was resolved by dropping one side of the choice: ADR-027 makes Encina.Marten the sole event-sourcing provider and declares Encina.EventStoreDB deprecated."
    current: yes
    sources:
      - "quote: \"This issue will be replaced by:\" followed by the Event Sourcing, Sagas and Messaging Strategy Pattern issues (https://github.com/dlrivada/Encina/issues/15#issuecomment-3687436427, 2025-12-23)"
      - "paraphrase: ADR-027 records the #17 and #321 decision nine months after the fact (https://github.com/dlrivada/Encina/blob/main/docs/architecture/adr/027-marten-as-the-event-sourcing-provider.md, read 2026-10-07)"
    destinations:
      - kind: adr
        status: done
        target: "docs/architecture/adr/027-marten-as-the-event-sourcing-provider.md"
  - kind: rule
    statement: "When evaluating patterns for subsystems with heterogeneous backends, first ask whether the backends are interchangeable providers (Validation, where the orchestrator applies; Caching also has interchangeable providers but needed no orchestrator because its behaviors were already centralized, #13) or fundamentally different strategies (Event Sourcing, and by the same reasoning Sagas and Messaging Transports); a spike opened to design the strategy selection API may end by eliminating one side of the choice instead."
    current: yes
    sources:
      - "paraphrase: the closing comment's 'Key Insight' contrasting Caching and Validation with Event Sourcing, Sagas and Messaging Transports (https://github.com/dlrivada/Encina/issues/15#issuecomment-3687436427, 2025-12-23)"
    destinations:
      - kind: adr
        status: done
        target: "docs/architecture/adr/027-marten-as-the-event-sourcing-provider.md"
  - kind: gotcha
    statement: "The Strategy-versus-Provider question of the Sagas (#16) and Messaging Transports (#18) spikes was not formally resolved in any ADR; ADR-027 covers only event sourcing."
    current: unknown
    sources:
      - "paraphrase: #16 and #18 are closed with no ADR beyond ADR-027 and no IEventSourcingStrategy-style marker interfaces in src/ (https://github.com/dlrivada/Encina/issues/18, re-checked 2026-10-07)"
    destinations:
      - kind: none
        status: done
  - kind: gotcha
    statement: "The orchestration duplication #15 worried about recurs inside Encina.Marten itself: MartenAggregateRepository and SnapshotAwareAggregateRepository duplicate the concurrency and stream-collision classification and the save/create orchestration."
    current: yes
    sources:
      - "paraphrase: the SPEC-003 audit of #15 compared the two repositories, which duplicate IsConcurrencyException and the collision check (https://github.com/dlrivada/Encina/issues/1349, 2026-09-25)"
    destinations:
      - kind: backlog
        status: planned
        target: "#1349"
  - kind: gotcha
    statement: "AGENTS.md section 5 still describes event sourcing as 'Marten primary, EventStoreDB future' (and the older CLAUDE.md table said the same), contradicting ADR-027, which says EventStoreDB is deprecated and was deleted from src/ in commit 87d92a39."
    current: yes
    sources:
      - "paraphrase: AGENTS.md section 5 'Other categories' line on event sourcing against ADR-027 (https://github.com/dlrivada/Encina/issues/1347, 2026-09-25, re-read 2026-10-07)"
    destinations:
      - kind: docs
        status: planned
        target: "#1347"
remediation:
  - 1328
  - 1347
  - 1348
  - 1349
audit:
  checklist: 1
  date: 2026-09-25
  verdict: findings-tracked
  record: "docs/knowledge/audits/issue-15.md"
```

## Where the knowledge lives (record)

- ADR-027 (`docs/architecture/adr/027-marten-as-the-event-sourcing-provider.md`) records the #15, #17 and #321 reasoning and cites #15 by number; that destination is complete.
- `AGENTS.md` section 5 is inconsistent with it (EventStoreDB "future" instead of deprecated); tracked by #1347 (open).
- The concern #15 raised, duplicated orchestration, now lives inside `Encina.Marten` (#1349, open).

## Audit result (docs/knowledge/audits/issue-15.md)

# Audit — issue #15 (SPEC-003)

Scope: `src/Encina.Marten/**` (aggregate repositories, snapshots, projections, service registration), the ADR-027 decision destination, CLAUDE.md's Event Sourcing table, and the siblings `src/Encina.Audit.Marten`, `src/Encina.Marten.GDPR` (checked for the same duplication pattern; not present there — they don't implement `IAggregateRepository<T>` themselves, they build on the Marten projections/document-session APIs directly).

Outcome of #15 itself: rejected-reasoned / superseded (no code shipped from #15; ADR-027 is the true resolution of its concern, one provider survives).

| AUD item | Result | Evidence |
|---|---|---|
| Decision destination present (ADR) | PASS | `docs/architecture/adr/027-marten-as-the-event-sourcing-provider.md`, Accepted, cites #15/#17/#321 correctly |
| Decision destination present (CLAUDE.md) | FAIL | `CLAUDE.md:331` still lists `Encina.EventStoreDB` as "(future)"; ADR-027 says it is deprecated with no new capabilities. Stale since at least 2026-09-22 (ADR-027's date) |
| Decision destination present (ROADMAP) | NOT MEASURED | Out of scope for this pass; flagged as open question in the knowledge record |
| Orchestration duplication (the literal concern of #15) | FAIL | `MartenAggregateRepository.cs` and `Snapshots/SnapshotAwareAggregateRepository.cs` duplicate `IsConcurrencyException` (:361-368 / :475-481), `IsStreamCollisionException` (:373-379 / :486-492), the concurrency-conflict `Left`/`conflictDetails` construction (:230-254 / :200-224), and the `SaveAsync`/`CreateAsync` bodies almost verbatim. `SnapshotAwareAggregateRepository.LoadWithSnapshotAsync:336-345` re-implements the "stream does not belong to aggregate" check a third time instead of reusing `MartenAggregateRepository.StreamDoesNotBelongToAggregate:348-356`. Verified by adversarial-reviewer (foreground pass, same findings independently confirmed with matching line numbers) |
| Provider coherence (single provider, Marten) | PASS (n/a for multi-provider matrix) | ADR-027 makes Marten the only event-sourcing backend; the 10-database-provider matrix and the 8-caching-provider matrix do not apply to this package |
| `EncinaError.Message` never reaches logs | PARTIAL | `EventPublishingPipelineBehavior.cs:87` already tracked by open issue #1328 (not re-reported). New finding: `Projections/InlineProjectionRelay.cs:134-137` passes `error.Message` from a failed inline-projection dispatch into `ProjectionLog.InlineProjectionFailedAfterSave` (EventId 2700-range) — violates the same CLAUDE.md rule, not previously tracked |
| Registration completeness (DI test with `ValidateOnBuild`/`ValidateScopes`) | FAIL | `ServiceCollectionExtensionsTests.cs` and the Guard/Contract tests for Marten build `IServiceProvider` without `ValidateOnBuild: true, ValidateScopes: true` (grep for that string under `tests/**/*Marten*` returned zero matches). `AddSnapshotableAggregate<TAggregate>` and `AddEncinaMarten` are not proven correct by the CLAUDE.md-mandated DI-validation test |
| TimeProvider injection | PASS | `SnapshotAwareAggregateRepository.cs:49,64` takes `TimeProvider?` defaulting to `TimeProvider.System`; `MartenAggregateRepository` has no time-dependent logic |
| Async DB calls with CancellationToken | PASS | All Marten calls (`FetchStreamAsync`, `SaveChangesAsync`, snapshot store calls) forward the token; the one `CancellationToken.None` use is the documented fire-and-forget async-snapshot background write |
| Errors never swallowed (background/store paths) | PASS (documented exception) | `TryCreateSnapshotAsync`'s catch-all (`SnapshotAwareAggregateRepository.cs:461-469`) swallows a failed snapshot save/prune, but this is a deliberate, commented trade-off: snapshots are a read-optimization derived from already-durable events, not the primary `Send`/`Publish`/store operation this CLAUDE.md rule targets. Not flagged as a violation |
| EventId range discipline | PASS | `Log.cs` and `Snapshots/SnapshotLog.cs` EventIds fall inside the registered Marten range (2600-2799 per `EventIdRanges.cs`/CLAUDE.md); no overlap found by inspection with `Projections/ProjectionLog.cs` |
| Docs/README accuracy | FAIL (gap, not inaccuracy) | No `src/Encina.Marten/README.md` exists (unlike `Encina.Marten.GDPR`, which has one). No dedicated Diátaxis feature page for the aggregate-repository/snapshot pattern exists under `docs/features/`; only a narrower `docs/features/event-metadata-tracking.md` covers one sub-feature and is itself accurate against `ServiceCollectionExtensions.cs`. Did the docs-reviewer check myself (brief §"docs-reviewer... do this check yourself"): no page misdescribes existing behavior, the gap is absence, not error |
| Tests for orchestration pieces | PASS | Both repositories have Unit, Guard, Contract and Integration tests (`tests/Encina.UnitTests/Marten/MartenAggregateRepositoryTests.cs`, `Snapshots/SnapshotAwareAggregateRepositoryTests.cs`, `Encina.GuardTests/Marten/...`, `Encina.ContractTests/Marten/Core/...`, `Encina.IntegrationTests/Infrastructure/Marten/Core/MartenAggregateRepositoryIntegrationTests.cs`); coverage-per-flag was not separately re-run in this pass (existing test presence confirmed by file listing, not a fresh `dotnet test` coverage run) — recorded as NOT MEASURED for the numeric coverage percentage, PASS for test-type presence |
| Siblings (`Encina.Audit.Marten`, `Encina.Marten.GDPR`) | PASS (no duplication of this pattern) | Neither package re-implements `IAggregateRepository<T>` or the concurrency/collision classification; they consume Marten's document/session API directly for their own projections and crypto-shredding concerns, so the #15 duplication concern does not recur there |

## Specialist passes
- **adversarial-reviewer** (foreground, Sonnet): ran on the full file set above; confirmed and extended my own by-inspection finding (added the third `StreamDoesNotBelongToAggregate` duplication and the `InlineProjectionRelay` message-leak instance). Full findings folded into this table.
- **docs-reviewer**: not spawned (issue-worker/coordinator restriction noted in brief — "until the spawn allowlist lets issue-worker spawn docs-reviewer, do this check yourself"); performed the check directly above (docs/README accuracy row).
- **Second adversarial-reviewer for tests**: skipped — scope is one package with existing test coverage across all flags already present; not a large/complex scope requiring a second focused pass. Coverage percentages were not re-measured (see NOT MEASURED note above); this is a gap in rigor but a full `dotnet test --collect "XPlat Code Coverage"` run for Encina.Marten was judged out of proportion for a refactor-pattern audit whose primary code-level finding is structural duplication, not a coverage shortfall — flagged in "Lessons for the pipeline".


