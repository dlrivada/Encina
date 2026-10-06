# Delta scope of issue #17 (set rules-2026-10)

Reused from the original audit; do not re-derive it. Rules in this delta: see tools/ai/audit/pipeline-delta.json.

**Scope source:** the published record and the published stage files of the original audit (docs/knowledge/audits/17/stages/).

## Knowledge record (docs/knowledge/issues/17.md)

```yaml
schema: 1
nav_exclude: true
issue: 17
title: "[SPIKE] Implement Strategy pattern for Event Sourcing (EventStoreDB vs Marten)"
closed: 2025-12-23
state_reason: completed
outcome: delivered
type: spike
area: eventsourcing
review: verified
packages:
  - Encina.EventStoreDB
  - Encina.Marten
prs:
linked_prs:
  - 1113
knowledge:
  - kind: decision
    statement: "Deprecate Encina.EventStoreDB and retain only Encina.Marten as the sole event-sourcing provider, rejecting a Strategy-pattern abstraction over both backends."
    current: yes
    sources:
      - "paraphrase: closing comment decided to deprecate EventStoreDB support and keep only Marten for event sourcing (issue #17, 2025-12-23)"
      - "quote: \"refactor: deprecate EventStoreDB, Wolverine, NServiceBus, MassTransit, Dapr\" (commit 87d92a394cccbcb4445e2ac349123f290548742d, Closes #17, 2025-12-23)"
    destinations:
      - kind: adr
        status: done
        target: "docs/architecture/adr/027-marten-as-the-event-sourcing-provider.md"
      - kind: docs
        status: done
        target: "ROADMAP.md"
      - kind: docs
        status: planned
        target: "AGENTS.md (line 70 still reads 'EventStoreDB future', contradicts ADR-027)"
  - kind: decision
    statement: "Encina.Marten remains the sole event-sourcing backend; no Encina.EventSourcing shared-abstractions package was created, since a single provider leaves nothing to abstract."
    current: yes
    sources:
      - "paraphrase: closing comment answer 'No' to creating a shared abstractions package (issue #17, 2025-12-23)"
    destinations:
      - kind: adr
        status: done
        target: "docs/architecture/adr/027-marten-as-the-event-sourcing-provider.md"
  - kind: rejected-alternative
    statement: "Option A (Strategy + Orchestrator pattern, both EventStoreDB and Marten as interchangeable strategies) was rejected: once EventStoreDB was dropped, only one strategy remained, making the abstraction unnecessary."
    current: yes
    sources:
      - "paraphrase: issue body Option A plus closing comment rejection (issue #17, 2025-12-23)"
    destinations:
      - kind: adr
        status: done
        target: "docs/architecture/adr/027-marten-as-the-event-sourcing-provider.md"
  - kind: rejected-alternative
    statement: "Option B (keep Encina.EventStoreDB and Encina.Marten as two independent, uncoordinated packages) was rejected because Encina.EventStoreDB was deprecated outright rather than kept."
    current: yes
    sources:
      - "paraphrase: issue body Option B plus closing comment rejection (issue #17, 2025-12-23)"
    destinations:
      - kind: adr
        status: done
        target: "docs/architecture/adr/027-marten-as-the-event-sourcing-provider.md"
  - kind: rejected-alternative
    statement: "Option C (a shared Encina.EventSourcing abstractions package over both providers) was rejected: with a single provider, shared interfaces are redundant."
    current: yes
    sources:
      - "paraphrase: issue body Option C plus closing comment rejection (issue #17, 2025-12-23)"
    destinations:
      - kind: adr
        status: done
        target: "docs/architecture/adr/027-marten-as-the-event-sourcing-provider.md"
  - kind: rule
    statement: "Prefer single-vendor simplicity over premature abstraction: when only one implementation is supported, do not add an abstraction layer over it."
    current: yes
    sources:
      - "paraphrase: closing comment rationale (issue #17, 2025-12-23)"
    destinations:
      - kind: adr
        status: done
        target: "docs/architecture/adr/027-marten-as-the-event-sourcing-provider.md"
  - kind: rule
    statement: "Infrastructure choice drives architecture: choosing Marten (PostgreSQL-based) over EventStoreDB (a dedicated server) avoided adding a second piece of operational infrastructure."
    current: yes
    sources:
      - "paraphrase: closing comment rationale, consistent with ADR-027 (issue #17, 2025-12-23)"
    destinations:
      - kind: adr
        status: done
        target: "docs/architecture/adr/027-marten-as-the-event-sourcing-provider.md"
  - kind: gotcha
    statement: "Deprecation here meant physical removal of the package's files from src/ and Encina.slnx in one commit, not an in-repo [Obsolete] marker; the removal commit's claim that files were 'moved to .backup/' should not be read as 'still present in the repo' — .backup/ is gitignored and absent from a fresh checkout."
    current: yes
    sources:
      - "paraphrase: commit 87d92a394cccbcb4445e2ac349123f290548742d message (#17) vs. verified absence of src/Encina.EventStoreDB/ and .backup/ in the current checkout (2025-12-23; verified 2026-09-27)"
    destinations:
      - kind: rule
        status: done
        target: "AGENTS.md"
  - kind: gotcha
    statement: "ADR-027's Consequences section states EventStoreDB 'remains in the repository as deprecated code until a removal decision', which is factually wrong: the package was deleted from git tracking in commit 87d92a39, not left in place."
    current: yes
    sources:
      - "paraphrase: docs/architecture/adr/027-marten-as-the-event-sourcing-provider.md Consequences section (#17) vs. verified absence of src/Encina.EventStoreDB/ (2026-09-22; verified 2026-09-27)"
    destinations:
      - kind: adr
        status: planned
        target: "docs/architecture/adr/027-marten-as-the-event-sourcing-provider.md"
  - kind: gotcha
    statement: "AGENTS.md line 70 and docs/INVENTORY.md line 89 both still describe EventStoreDB as a 'future' event-sourcing provider, contradicting ADR-027 and the actual 2025-12-23 deletion."
    current: yes
    sources:
      - "paraphrase: AGENTS.md line 70 and docs/INVENTORY.md line 89 read against ADR-027 and the deletion commit for #17 (verified 2026-09-27)"
    destinations:
      - kind: docs
        status: planned
        target: "AGENTS.md"
      - kind: docs
        status: planned
        target: "docs/INVENTORY.md"
  - kind: pending-work
    statement: "No EventStoreDB-to-Marten migration guide was ever created; low priority pre-1.0 (no existing users per AGENTS.md §1), but the documentation gap is real."
    current: yes
    sources:
      - "paraphrase: docs/ search found no migration guide (issue #17; verified 2026-09-27)"
    destinations:
      - kind: docs
        status: planned
        target: "docs/ (an EventStoreDB-to-Marten migration guide, if still judged worth writing pre-1.0)"
remediation:
audit:
  checklist: 1
  date: 2026-09-27
  verdict: not-audited
  record: "docs/knowledge/audits/issue-17.md"
```

## From the original archivist.md (docs/knowledge/audits/17/stages/archivist.md)

- **`Encina.EventStoreDB`** (the strategy that was dropped): removed from `src/` entirely. Commit `87d92a394cccbcb4445e2ac349123f290548742d` ("refactor: deprecate EventStoreDB, Wolverine, NServiceBus, MassTransit, Dapr", 2025-12-23, on `main`, `Closes #17`) deleted every file under `src/Encina.EventStoreDB/` (`AggregateBase.cs`, `EventStoreDbAggregateRepository.cs`, `IAggregateRepository.cs`, `ServiceCollectionExtensions.cs`, etc.) and the matching entries in `Encina.slnx`. The commit message says the package was "moved to `.backup/deprecated-packages/`", but `.backup/` is a gitignored path (`.gitignore:16`) that does not exist in this checkout — the package is untracked local state, not something present in the repository today. Verified: `Test-Path src/Encina.EventStoreDB` → `False`; `Test-Path .backup` → `False`; `git show --stat 87d92a39` shows only deletions (`69 files changed, 18 insertions(+), 4689 deletions(-)`), no `.backup/` path in the diff.
- **`Encina.Marten`** (the strategy that was kept): still lives at `src/Encina.Marten/`, with `IAggregateRepository.cs`, `MartenAggregateRepository.cs` and `Snapshots/SnapshotAwareAggregateRepository.cs` — the aggregate-repository implementation the issue's decision made the sole event-sourcing provider. Verified by `Get-ChildItem`.
- **`Encina.EventSourcing` abstractions package**: never created, matching the "No" answer in the decision comment. Verified: no such directory anywhere under `src/`.
- Nothing else in scope: the issue was a SPIKE (investigation + decision), not an implementation; the only code change was the deletion above, captured entirely in commit `87d92a39`.

## From the original code.md (docs/knowledge/audits/17/stages/code.md)

- `src/Encina.EventStoreDB/` — confirmed absent (`Test-Path` → `False`); the archivist's scope list.
- `src/Encina.Marten/IAggregateRepository.cs`, `MartenAggregateRepository.cs` — read in full as the "kept" provider the decision made sole; no `Encina.EventSourcing` abstractions package exists, confirmed.
- Scope correction: the archivist's scope list stops at "the code change was the deletion, nothing else in scope." Since #17's actual diff (commit `87d92a394cccbcb4445e2ac349123f290548742d`) is a pure deletion, the code-review question for a PR submitted today is not "is `Encina.Marten`'s 48-file implementation correct" (that belongs to whichever later issues actually wrote it, each auditable on its own in this same pipeline) but "is the deletion complete and free of orphaned/misleading artifacts." I widened the check accordingly: grepped the whole repository (`src/`, `tests/`, `.github/`, `.vscode/`, `Directory.Packages.props`, `Encina.slnx`, `docker-compose*`, coverage manifests) for `EventStoreDB`/`EventStore.Client`/`KurrentDB` to find what the commit's cleanup missed, rather than re-auditing `Encina.Marten`'s implementation logic (out of scope for #17, would duplicate the audits of the issues that actually wrote it).


