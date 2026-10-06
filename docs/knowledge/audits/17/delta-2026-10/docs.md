# Docs stage: delta rules-2026-10 (rule (a) only), issue #17

## Pages reviewed

Scope source: `artifacts/knowledge/delta-scope.md`. #17 shipped no page of its own (a deprecation SPIKE whose only code change was a deletion), so the review covers the page that records the decision and the places a reader would look for the feature it kept.

- `docs/architecture/adr/027-marten-as-the-event-sourcing-provider.md` (34 lines): read in full; the destination of every knowledge item.
- `docs/architecture/adr/index.md` (ADR 027 row at :38; front matter at :1-7).
- `docs/index.md:69` (the only row that mentions event sourcing).
- `docs/tutorials/` (`index.md`, `quickstart.md`), `docs/features/index.md`, `docs/guides/*.md`: searched for `aggregate`, `event sourc`, `Marten` (case-insensitive); the only hits are `docs/guides/health-checks.md:21` and `:140`, which are not about event sourcing.
- `src/Encina.Marten/` (`Get-ChildItem -Filter *.md` and `Get-ChildItem -Recurse -Filter README.md`: 0 files).
- Neighbour docs that restate the same fact, for context only: `AGENTS.md:70`, `docs/INVENTORY.md:89`, `ROADMAP.md:803`.

Rule (a) point results for ADR-027: point 1 (visual): no diagram, no table; point 2 (C# samples): the page has no `csharp` block, so nothing to compile; point 3 (figures): 0 hand-typed coverage, mutation or performance figures; point 4 (placement): see finding 1; point 5 (adequate docs): see finding 3.

## Findings

1. **Minor** — `docs/architecture/adr/027-marten-as-the-event-sourcing-provider.md:1` (first line is `# ADR-027: ...`; no `---` front matter). Rule (a) point 4, placement: `.claude/skills/encina-docs/SKILL.md:24-26` requires just-the-docs front matter on every page under `docs/`, and the sibling ADR-019 has it (`docs/architecture/adr/019-compliance-event-sourcing-marten.md:1-6`: `title`, `layout: default`, `parent: ADRs`, `grand_parent: Architecture`). Without `parent: ADRs` the page is not placed under the ADRs section in the site navigation; it is reachable only through the index row at `docs/architecture/adr/index.md:38` and the cross-reference at `docs/architecture/adr/036-three-audit-stores.md:137`. `nav_order` is optional and is not part of this finding. Repo-wide, 8 of 34 files under `docs/architecture/adr/*.md` start without `---` (024, 027, 028, 029, 030, 031, 034, 036; command: `Get-ChildItem docs/architecture/adr/*.md | ? { (Get-Content $_ -TotalCount 1) -ne '---' }`); this finding is scoped to ADR-027.

2. **Minor** — `docs/architecture/adr/027-marten-as-the-event-sourcing-provider.md:13` (section `## Decision`). Rule (a) point 1, no emojis: the quoted text `#321: "❌ Encina.EventStoreDB - Excluded"` carries U+274C. It is the only emoji on the page (Unicode-range search over the file, including U+23F3 and U+2B50: 1 line, :13). Cite `.claude/agents/docs-reviewer.md` point 1 for the rule. Fix: quote the decision without the symbol.

3. **Minor** — `docs/architecture/adr/027-marten-as-the-event-sourcing-provider.md:15-19` (`## Rationale`) and `:21-24` (`## Alternatives rejected`). Rule (a) point 1, visual and scannable: the page compares two backends (infrastructure footprint, projection language, number of implementations to verify) in prose bullets, with no comparison table and no diagram of the before and after (two providers behind one abstraction versus `Encina.Marten` alone). The page is short (34 lines) and not a wall of text, which is why this is minor and not major. A three-row table (Marten, EventStoreDB, criterion) would let a reader scan the decision. The `.backup`-style factual drift on the same page (line :30) is a different finding and is already tracked as #1513 (see Informational).

4. **Major** — Rule (a) point 5, adequate docs for a feature in scope: the issue's outcome is that `Encina.Marten` is the sole event-sourcing provider, yet there is no concept or guide page for event sourcing on Marten, no package README, and no entry in the tutorials or learning paths. Evidence: `src/Encina.Marten/` contains no `.md` file (searched recursively, 0 hits); `docs/tutorials/index.md`, `docs/tutorials/quickstart.md`, `docs/features/index.md` and `docs/guides/*.md` have no hit for `Marten`, `event sourc` or `aggregate` that concerns event sourcing; `docs/index.md:69` is a one-line table row ("Marten event store with projections, snapshots, GDPR crypto-shredding") with no link; the `docs/features/` pages that mention Marten (`audit-marten.md`, `crypto-shredding.md`, `event-metadata-tracking.md`, `optimistic-concurrency.md`) each describe a single capability, not how to adopt aggregates, repositories, snapshots and projections. AGENTS.md section 8 also requires each satellite package to have its own README. The planned destination in the knowledge record (an EventStoreDB-to-Marten migration guide, pending-work item) is separate and optional pre-1.0; this finding asks for the primary page the decision implies: a feature page under `docs/features/` (reference or concept), a how-to for registering `Encina.Marten`, and a link from `docs/tutorials/index.md`. Related issues: none open that asks for a Marten event-sourcing page (open EventStoreDB-docs issues #1510, #1513, #1515, #1517, #1518 are about stale mentions, not this page; the same-day delta umbrellas #1847, #1850, #1856, #1864, #1866, #1877, #1886, #1887 concern other packages).

## Informational (not findings)

- `docs/architecture/adr/027-marten-as-the-event-sourcing-provider.md:30` still says `Encina.EventStoreDB` "remains in the repository as deprecated code"; `Test-Path src/Encina.EventStoreDB` is `False` and `Test-Path .backup` is `False`. This is an accuracy defect, not a rule (a) point, and open issue #1513 already tracks it; the same family is tracked as #1510, #1515 (`docs/INVENTORY.md:89`), #1517 (`docs/architecture/extensibility-analysis.md`) and #1518 (ADR-019 :38). `AGENTS.md:70` ("EventStoreDB future") is the remaining mention in the binding rules file; `ROADMAP.md:803` is correct ("Deprecated"). Delta mode checks only rule (a), so no new finding is written for them.
- ADR-027 has a `## References` section and is linked from the ADR index (`docs/architecture/adr/index.md:38`), so the neighbour check for the decision itself passes.
- No `csharp` blocks, coverage figures, mutation figures or performance figures exist on ADR-027, so points 2 and 3 pass trivially.
- Search method for the front matter and emoji checks: `Get-Content -TotalCount 1` and `Select-String` run from the worktree root with relative paths; the `artifacts/` tree was not part of any count.

## Lessons for the pipeline

- When an issue's only destination is an ADR and the ADR lacks front matter, check the sibling ADR on the same topic (here ADR-019) to show the expected block; the contrast makes the finding checkable.
- For a deprecation SPIKE, rule (a) point 5 is about the surviving provider, not the removed one: check for a concept page, a package README and a tutorial entry for the kept package.
