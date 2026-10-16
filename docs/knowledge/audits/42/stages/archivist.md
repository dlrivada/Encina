## Scope
Issue #42, created 2025-12-24T13:25:23Z, closed 2025-12-26T18:31:20Z (COMPLETED) by commit d5c60cab9 (Fixes #42; no PR, no comments). Files it added, all still present today:
- `src/Encina.Messaging/DeadLetter/`: DeadLetterCleanupProcessor, ErrorCodes, Filter, Log, Manager, Options, Orchestrator, ServiceCollectionExtensions, SourcePatterns, IDeadLetterManager, IDeadLetterMessage, IDeadLetterMessageFactory, IDeadLetterStore (all have been modified later; Options and Orchestrator by #583/PR #2082 and PR #2202).
- `src/Encina.Messaging/Health/DeadLetterHealthCheck.cs`.
- Edits to `MessagingConfiguration.cs`, `MessagingServiceCollectionExtensions.cs`.
- Tests were in `tests/Encina.Tests/DeadLetter/` (deleted by the consolidation commit 658263027, 2026-01-16); today they live in `tests/Encina.{Unit,Guard,Contract,Property,Integration,Load}Tests/Messaging/DeadLetter` and per-provider `...IntegrationTests/**/DeadLetter`.
Later work on the same area (not #42): provider stores for the 10 database providers, `DeadLetterMetrics`, `InstrumentedDeadLetterStore` (#583, PR #2082, 2026-10-10), source capture (PR #2202). No code was removed on purpose.

## Destinations
- Abstraction/store/manager decision: ADR-046 `docs/architecture/adr/046-persistent-dead-letter-queue.md` present; `docs/features/dead-letter-queue.md` present (references AddEncinaDeadLetterQueue); regression tests present in `tests/Encina.UnitTests/Messaging/DeadLetter`. AGENTS.md mentions the DLQ only in the transports row (line 65), no rule for the abstraction: missing, not needed.
- Proposed API `MaxRetries`, `EnableAutoReplay`: missing in `DeadLetterOptions` (grep of `src/Encina.Messaging/DeadLetter`); no open issue asks for them by name (searched "auto replay", "automatic replay dead letter").
- Poison detection: missing in all of `src/`; tracked by #135 (OPEN, body reads as the same request plus an `IPoisonMessageDetector` design).
- Transport DLQ integration: missing; #1609 (OPEN) records that RabbitMQ, Kafka, NATS, Azure Service Bus and Amazon SQS have no dead-letter support.
- Metrics: not delivered by #42 (added 2026-10-10 by PR #2082).
- Inspection/admin surface beyond the manager: #2229 (OPEN) adds an admin API; adjacent, #42 asked for none.

## Successor and duplicate issues
- #26 (CLOSED, COMPLETED, "Reverted - issue created in error") is the earlier duplicate of #42; its record already says `duplicate_of: 42` (docs/knowledge/issues/26.md).
- #42 itself has no successor. Open related, verified OPEN today: #135, #1609, #149, #2229, #771.
- Pre-draft errors: `closed_at` as 12/26/2025 (normalised), `linked_prs: [unknown]` (empty: no PR), "Code touched: unknown" (resolved by git show --stat), invented spec-invariant/docs destinations (docs kept only because the feature page exists).

## Lessons for the pipeline
- `gh issue view <n> --repo ... --comments` printed nothing in this session; `--json title,body,comments --jq` worked.
- A close commit that matches the title is not delivery: compare each Features bullet (poison detection, auto replay, transports, metrics) with `src/` and with `git log --diff-filter=A` of the symbol before choosing the outcome.
