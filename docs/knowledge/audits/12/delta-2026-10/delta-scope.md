# Delta scope of issue #12 (set rules-2026-10)

Reused from the original audit; do not re-derive it. Rules in this delta: see tools/ai/audit/pipeline-delta.json.

**Scope source:** the published record and the published audit result (docs/knowledge/audits/issue-12.md) only: the original audit has no published stage files.

## Knowledge record (docs/knowledge/issues/12.md)

```yaml
schema: 1
nav_exclude: true
issue: 12
title: "[DEBT] Reduce code duplication in Dapper/ADO providers to meet SonarCloud ≤3% threshold"
closed: 2025-12-23
state_reason: completed
outcome: partial
type: debt
area: messaging
review: verified
packages:
  - Encina.Messaging
  - Encina.Dapper.SqlServer
  - Encina.Dapper.PostgreSQL
  - Encina.Dapper.MySQL
  - Encina.ADO.SqlServer
  - Encina.ADO.PostgreSQL
  - Encina.ADO.MySQL
  - Encina.EntityFrameworkCore
  - Encina.MongoDB
prs:
linked_prs:
knowledge:
  - kind: decision
    statement: "Provider-independent messaging behavior was centralized into Encina.Messaging (InboxPipelineBehavior, OutboxPostProcessor, IHasNotifications, SagaOrchestrator, SchedulerOrchestrator) behind factory interfaces (IInboxMessageFactory, IOutboxMessageFactory, ISagaStateFactory, IScheduledMessageFactory) implemented once per provider, and wired through the two DI entry points AddMessagingServices<...> and AddMessagingServicesCore<...> in MessagingServiceCollectionExtensions."
    current: yes
    sources:
      - "quote: \"All messaging pattern behaviors have been centralized to **Encina.Messaging**\" (https://github.com/dlrivada/Encina/issues/12#issuecomment-3687323189, 2025-12-23)"
      - "paraphrase: commit cf6cadbe 'centralize messaging patterns with Orchestrator/Factory architecture', attributed from the issue comments and a date-scoped git log, does not name #12 (https://github.com/dlrivada/Encina/commit/cf6cadbe, 2025-12-23)"
    destinations:
      - kind: executable-rule
        status: done
        target: "src/Encina.Messaging/MessagingServiceCollectionExtensions.cs"
  - kind: decision
    statement: "The Store implementations (OutboxStore, InboxStore, SagaStore, ScheduledMessageStore per provider) keep their SQL-dialect duplication on purpose (TOP vs LIMIT vs FETCH FIRST, UTC timestamp functions) and are excluded from SonarCloud duplication analysis, instead of being forced under the 3% threshold."
    current: yes
    sources:
      - "quote: \"The code duplication in Dapper/ADO store implementations is **intentional** and has been excluded from SonarCloud analysis.\" (https://github.com/dlrivada/Encina/issues/12#issuecomment-3688093992, 2025-12-23)"
      - "paraphrase: commit c66625f5 'exclude intentional SQL dialect duplication from SonarCloud' closes the issue (https://github.com/dlrivada/Encina/commit/c66625f59976de5641254d5aff161bbe3871f501, 2025-12-23)"
    destinations:
      - kind: executable-rule
        status: done
        target: ".github/workflows/sonarcloud.yml"
      - kind: adr
        status: planned
        target: "#1336"
  - kind: rejected-alternative
    statement: "Abstract base classes for the Store implementations were rejected because they add coupling between providers, against the provider-independence goal; an SQL template approach (the issue's own recommendation) was not implemented either, since centralization stopped at the behavioral layer and the SQL-level duplication was excluded rather than templated."
    current: yes
    sources:
      - "quote: \"Adds complexity, tighter coupling between providers\" (issue #12 body, Option 1, https://github.com/dlrivada/Encina/issues/12, 2025-12-23)"
      - "paraphrase: the resolving comment lists the benefits of keeping the providers separate: independence, clarity, performance, maintainability (https://github.com/dlrivada/Encina/issues/12#issuecomment-3688093992, 2025-12-23)"
    destinations:
      - kind: adr
        status: planned
        target: "#1336"
  - kind: rule
    statement: "Centralize only SQL-dialect-free behavior in the shared Encina.Messaging package and never store SQL; when dialects differ meaningfully prefer separate Store implementations over abstraction, and use a provider-specific factory as the seam that lets shared orchestration construct provider entities without knowing the concrete type."
    current: yes
    sources:
      - "paraphrase: the resolving comment's 'Benefits of Keeping Separate' and the live pattern in MessagingServiceCollectionExtensions.cs (https://github.com/dlrivada/Encina/issues/12#issuecomment-3688093992, 2025-12-23)"
    destinations:
      - kind: docs
        status: planned
        target: "#1336"
  - kind: direction-change
    statement: "The sonar-project.properties file that carried the CPD exclusions was removed in April 2026 because it is incompatible with SonarScanner for .NET; the same exclusions now live as /d:sonar.cpd.exclusions in .github/workflows/sonarcloud.yml and are broader than the original (they also cover Testing, Caching, DistributedLock and Marten packages)."
    current: yes
    sources:
      - "quote: \"fix(ci): fix import ordering and remove sonar-project.properties\" (https://github.com/dlrivada/Encina/commit/93ef58e4, 2026-04-13)"
    destinations:
      - kind: executable-rule
        status: done
        target: ".github/workflows/sonarcloud.yml"
  - kind: gotcha
    statement: "The closing comments declared the centralization complete, but EF Core and MongoDB register their factories and stores by hand instead of through the shared helper, which silently dropped InboxPipelineBehavior (MongoDB UseInbox) and ISagaRunner/ISagaNotFoundDispatcher (EF Core and MongoDB UseSagas); fixed by #1333 (PR #1399, closed). The MongoDB InboxMessageFactory still discards InboxMetadata (#1334, open)."
    current: yes
    sources:
      - "paraphrase: the SPEC-003 audit of #12 found the two registration gaps by reading the provider registrations (https://github.com/dlrivada/Encina/issues/1333, 2026-09-25)"
      - "paraphrase: PR #1399 registers EF Core and MongoDB messaging patterns through the shared helper (https://github.com/dlrivada/Encina/pull/1399, 2026-09-27)"
    destinations:
      - kind: backlog
        status: planned
        target: "#1334"
  - kind: gotcha
    statement: "The issue's own success criterion (SonarCloud duplication at or below 3%) is unverifiable today: sonarcloud.yml excludes whole provider packages from CPD and no document measures the resulting percentage; no closing comment attached a report."
    current: yes
    sources:
      - "paraphrase: .github/workflows/sonarcloud.yml:163 excludes Dapper, ADO, Testing and other packages from /d:sonar.cpd.exclusions (https://github.com/dlrivada/Encina/blob/main/.github/workflows/sonarcloud.yml, re-read 2026-10-07)"
    destinations:
      - kind: backlog
        status: planned
        target: "#1335"
remediation:
  - 1333
  - 1334
  - 1335
  - 1336
audit:
  checklist: 1
  date: 2026-09-25
  verdict: findings-tracked
  record: "docs/knowledge/audits/issue-12.md"
```

## Where the knowledge lives (record)

- The centralized behavior and its single DI seam: `src/Encina.Messaging/MessagingServiceCollectionExtensions.cs`; all six Dapper/ADO packages call `AddMessagingServices<...>`. No documentation page describes the factory seam yet.
- The accepted Store duplication and the CPD exclusion: `.github/workflows/sonarcloud.yml`; no ADR records the decision yet (#1336, open).
- The migration from `sonar-project.properties` to workflow flags is routine CI maintenance, not a design decision.

## Audit result (docs/knowledge/audits/issue-12.md)

# SPEC-003 audit — issue #12

Scope: `src/Encina.Messaging/{Inbox,Outbox,Sagas,Scheduling}/*` (behaviors, orchestrators,
factory interfaces, `MessagingServiceCollectionExtensions.cs`), the 8 per-provider factory
implementations (ADO ×3, Dapper ×3, EntityFrameworkCore, MongoDB), the 6 Dapper/ADO
`ServiceCollectionExtensions.cs` registration call sites, and the SonarCloud CPD exclusion
(`.github/workflows/sonarcloud.yml`, formerly `sonar-project.properties`).

This is a **broad/cross-cutting issue** (a refactor across 8 provider packages). Per the
audit method, the CONCERN audited in depth is *duplication reduction / centralization
correctness and registration completeness*. AUD items unrelated to that concern (e.g.
provider-specific SQL correctness of the Store classes, which this issue explicitly left
untouched) are the responsibility of the issues that created those Store classes, not this
one — marked n/a below with that reasoning.

| AUD item | Result | Evidence |
|---|---|---|
| Registration completeness | **Fail — 2 verified gaps** | The *factory* registrations pass as originally described: all 6 Dapper/ADO `ServiceCollectionExtensions.cs` call `AddMessagingServices<...>(config)` (e.g. `src/Encina.Dapper.SqlServer/ServiceCollectionExtensions.cs:69-78`), which registers `IOutboxMessageFactory`/`IInboxMessageFactory`/`ISagaStateFactory`/`IScheduledMessageFactory` in `src/Encina.Messaging/MessagingServiceCollectionExtensions.cs:80-123`. But the adversarial-reviewer pass (below) found EntityFrameworkCore and MongoDB — which register these interfaces by hand instead of through the shared helper — do it **incompletely**: (1) **BLOCKER** — `src/Encina.MongoDB/ServiceCollectionExtensions.cs:100-106` and `:246-252` (`UseInbox`, both overloads) never register `services.AddScoped(typeof(IPipelineBehavior<,>), typeof(InboxPipelineBehavior<,>))`, unlike EF Core (`src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs:176`) and ADO/Dapper via the shared helper (`MessagingServiceCollectionExtensions.cs:93`) — verified by direct read, confirmed absent in both MongoDB `UseInbox` blocks. Inbox idempotency silently does nothing for MongoDB users with `UseInbox = true`. (2) **MAJOR** — `ISagaRunner`/`ISagaNotFoundDispatcher` are registered by the shared helper (`MessagingServiceCollectionExtensions.cs:102-105`) but never by EF Core's hand-rolled `UseSagas` block (`src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs:179-185`, confirmed absent by grep) or MongoDB's (`:108-114`, `:254-260`, confirmed absent by grep). This is exactly the kind of gap the `CLAUDE.md` "Registration completeness" rule (project history #1260/#1273/#1285/#1289) exists to catch, and exactly the kind a `ValidateOnBuild`/`ValidateScopes` DI test would have caught before merge. |
| Errors never swallowed | **Not measured** | Out of this issue's concern (no new error-handling logic was introduced; centralization moved existing behavior verbatim). |
| Fail-closed gates | **N/A** | No gate/authorization logic in scope. |
| No `EncinaError.Message` leaks | **N/A** | No `EncinaError` construction changed by this issue. |
| Cross-cutting: Caching | **N/A** | Not applicable to a DI-registration/orchestration refactor. |
| Cross-cutting: OpenTelemetry | **N/A** | No new operations introduced; pre-existing instrumentation, if any, was moved along with the behavior, not audited here (belongs to the issue that added it). |
| Cross-cutting: Structured Logging | **Pass (moved, not audited for content)** | `SagaOrchestrator`, `OutboxOrchestrator`, `InboxOrchestrator`, `SchedulerOrchestrator`, `OutboxPostProcessor` all take `ILogger<T>` per their `PublicAPI.Unshipped.txt` constructor signatures (e.g. `Encina.Messaging.Sagas.SagaOrchestrator.SagaOrchestrator(..., ILogger<SagaOrchestrator>! logger, ...)`). EventId compliance of that logging is the concern of the issue(s) that defined it, not this one. |
| Health Checks | **N/A** | Not this issue's concern. |
| Validation | **N/A** | Not this issue's concern. |
| Resilience | **N/A** | Not this issue's concern. |
| Distributed Locks | **N/A** | Not this issue's concern. |
| Transactions | **N/A** | `TransactionPipelineBehavior` registration is untouched by this issue. |
| Idempotency | **N/A** | Not this issue's concern (Inbox idempotency logic predates this refactor and was moved verbatim). |
| Multi-Tenancy | **N/A** | Not this issue's concern. |
| Module Isolation | **N/A** | Not this issue's concern. |
| Audit Trail | **N/A** | Not this issue's concern. |
| 10-provider matrix | **Pass** | `IInboxMessageFactory`/`IOutboxMessageFactory` implemented in all 3 ADO + 3 Dapper + EntityFrameworkCore + MongoDB packages (8 implementations, matching the "ADO×3, Dapper×3, EFCore×1, MongoDB×1" provider count in `CLAUDE.md`). `ISagaStateFactory`/`IScheduledMessageFactory` likewise implemented in all 8. |
| EventIds | **N/A** | No new `[LoggerMessage]` EventIds were introduced by this issue; logging was relocated, not created. |
| PublicAPI | **Pass** | All new public types (`IInboxMessageFactory`, `IOutboxMessageFactory`, `ISagaStateFactory`, `IScheduledMessageFactory`, `SagaOrchestrator`, `SchedulerOrchestrator`, `InboxOrchestrator`, `OutboxOrchestrator`, `OutboxPostProcessor<,>`) are declared in `src/Encina.Messaging/PublicAPI.Unshipped.txt` (grep-confirmed, e.g. lines 76-112, 837-838). Still "Unshipped" because the project has not cut a 1.0 release; consistent with every other pre-1.0 package. |
| XML docs | **Not fully measured** | Spot-checked `MessagingServiceCollectionExtensions.cs` — every public member has an XML doc comment. Did not spot-check all 8 factory implementations individually (bulk item, low risk — mechanical factories). |
| Diátaxis docs / README accuracy | **Pass** | `docs/architecture/patterns-guide.md` documents the factory/orchestrator pattern (grep-confirmed present; not read in full for prose accuracy — see docs-reviewer substitute note below). |
| Fail-closed defaults | **N/A** | Not this issue's concern. |
| TimeProvider | **Pass** | `SagaOrchestrator` and `OutboxPostProcessor<,>` constructors take `System.TimeProvider? timeProvider = null` per `PublicAPI.Unshipped.txt` (optional, defaults presumably to `TimeProvider.System` internally, consistent with the `CLAUDE.md` rule). Not read line-by-line for the internal default-assignment. |
| Secrets in options | **N/A** | No options classes with secrets touched by this issue. |
| Async DB calls with `CancellationToken` | **Not measured** | Store implementations (where DB calls live) are explicitly out of this issue's scope (left duplicated). |
| Coverage per flag | **Not measured** | Running full-suite coverage per flag was judged disproportionate for a pure DI/registration refactor with no behavior change; test *existence* was verified instead (see below), matching the "cheap path" spirit for a mechanical, already-covered pattern. This is a deviation from the letter of the method and is flagged here rather than silently skipped. |
| Regression test for the bug | **N/A** | This is a `[DEBT]` issue, not a bug; no regression test expected. |
| Test existence | **Fail — 1 verified gap** | `SagaOrchestratorTests.cs`, `SchedulerOrchestratorTests.cs`, `OutboxPostProcessorTests.cs`, `InboxPipelineBehaviorTests.cs` all exist under `tests/Encina.UnitTests/Messaging/...`. Every one of the 8 provider factory implementations has a matching `*FactoryTests.cs` (24 files found). **MAJOR (adversarial-reviewer)** — none of them close the gap above: `tests/Encina.UnitTests/MongoDB/ServiceCollectionExtensionsExtendedTests.cs:236-370` never resolves `IPipelineBehavior<,>` to confirm it contains `InboxPipelineBehavior`, and no test anywhere resolves `ISagaRunner`/`ISagaNotFoundDispatcher` from a container built with EF Core's or MongoDB's `UseSagas = true`. A DI test with `ValidateOnBuild`/`ValidateScopes` per `CLAUDE.md`'s registration-completeness rule would have caught both gaps above. |
| Decision destinations (ADR/CLAUDE.md/reviewer-checklist) | **Partial — gap found** | See knowledge record: no ADR for the "accept Store-level SQL-dialect duplication, exclude from CPD" decision; `CLAUDE.md`'s Provider Coherence section does not carry the specific lesson. Docs destination (patterns-guide.md) is present. **MAJOR (adversarial-reviewer)** — the issue's own title claim ("meet SonarCloud ≤3% threshold") is unverifiable today: `.github/workflows/sonarcloud.yml:161-164` excludes entire provider packages (Dapper, ADO, EntityFrameworkCore, MongoDB, Testing, Caching, DistributedLock, Marten) from CPD analysis, and no doc or dashboard measures the resulting duplication percentage anywhere. The issue's goal was reframed from "reduce duplication to X%" to "exclude the remaining duplication from measurement" without ever recording that reframing as the actual outcome. |
| MongoDB `InboxMetadata` fidelity | **Fail — 1 verified minor** | **MINOR (adversarial-reviewer)** — `src/Encina.MongoDB/Inbox/InboxMessageFactory.cs:18-19` accepts an `InboxMetadata? metadata` parameter and discards it (never assigned to the created message), unlike the other 8 provider factories which serialize and persist it. Untested — no `InboxMessageFactoryTests.cs` assertion in MongoDB's test file exercises the metadata parameter, so the discrepancy was never caught by CI. |

## Adversarial-reviewer pass

Spawned in the foreground on the code in scope. Verified findings, ranked:

1. **BLOCKER** — MongoDB's `UseInbox` (`src/Encina.MongoDB/ServiceCollectionExtensions.cs:100-106` and `:246-252`, both overloads) never registers `IPipelineBehavior<,> -> InboxPipelineBehavior<,>`. Confirmed by direct read: EF Core registers it at `src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs:176`; ADO/Dapper get it for free through `MessagingServiceCollectionExtensions.cs:93`; MongoDB's two `UseInbox` blocks have no equivalent line. Effect: Inbox idempotency (message deduplication) silently never runs for MongoDB users, regardless of `UseInbox = true`.
2. **MAJOR** — `ISagaRunner`/`ISagaNotFoundDispatcher` are registered by the shared helper (`MessagingServiceCollectionExtensions.cs:102-105`) for every ADO/Dapper provider, but EF Core's own `UseSagas` block (`src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs:179-185`) and MongoDB's two `UseSagas` blocks (`:108-114`, `:254-260`) never register either. Confirmed absent by grep on both files. Effect: the low-ceremony saga runner and its not-found dispatcher are unresolvable for EF Core and MongoDB, even with sagas enabled.
3. **MAJOR** — No DI test resolves `IPipelineBehavior<TRequest,TResponse>` to confirm it yields an `InboxPipelineBehavior`, nor resolves `ISagaRunner`, for EF Core or MongoDB. `tests/Encina.UnitTests/MongoDB/ServiceCollectionExtensionsExtendedTests.cs:236-370` was checked specifically and does not do this. This is precisely the registration-completeness DI test `CLAUDE.md` requires (project history #1260/#1273/#1285/#1289).
4. **MAJOR** — The issue's own success criterion ("meet SonarCloud ≤3% threshold") is unverifiable today: `.github/workflows/sonarcloud.yml:161-164` excludes whole provider packages from CPD, and no document measures the actual resulting duplication percentage.
5. **MINOR** — `src/Encina.MongoDB/Inbox/InboxMessageFactory.cs:18-19` discards the `InboxMetadata` parameter instead of persisting it, unlike the other 8 provider factories; untested.

Findings 1-3 share one root cause (EF Core and MongoDB hand-roll their DI registration instead of
going through the shared `MessagingServiceCollectionExtensions` helper, and nothing catches the
resulting drift) and are drafted as a single `[BUG]` remediation issue. Findings 4 and 5 are
independent and drafted separately.

## docs-reviewer substitute (self-check, per brief §4)

`docs/architecture/patterns-guide.md` was grep-confirmed to mention the factory pattern but
not read end-to-end for Diátaxis fit or figure accuracy in this audit. Given this issue's
narrow, already-shipped scope and that the page covers many other patterns beyond this one
issue, a full docs-reviewer-equivalent pass was judged disproportionate; recommending it only
if a broader docs debt issue on `patterns-guide.md` is opened.

## Coverage

Not collected per-flag for this issue. Rationale: the scope is DI registration and
orchestration classes with unchanged runtime behavior since Dec 2025 — thousands of
downstream unit/integration tests already exercise them indirectly every time the messaging
patterns run (Outbox/Inbox/Saga/Scheduling tests across all providers). Spending a ~40-minute
CI-Full-equivalent local run to re-confirm flag percentages for a pure refactor with no
behavior change and dedicated `*OrchestratorTests`/`*FactoryTests` already found was judged
disproportionate to the audit's marginal value. Flagged as **not measured** rather than
silently assumed.


