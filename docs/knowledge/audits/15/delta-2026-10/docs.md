## Pages reviewed

Delta `rules-2026-10`, rule (a) only. Issue #15 shipped no page; its only docs destination is ADR-027, so the rule (a) checks ran on it and on the feature's surviving-provider surface (concept page, README, tutorial entry).

- `docs/architecture/adr/027-marten-as-the-event-sourcing-provider.md` (34 physical lines, `(Get-Content).Count`; 0 fenced blocks, 0 Mermaid, 0 `csharp`, 0 `%`/`ms`/`ns` literals, 1 emoji line, :13).
- `docs/architecture/adr/index.md:38` (index row for ADR-027).
- `src/Encina.Marten/` (`Test-Path src\Encina.Marten\README.md` is False), `docs/tutorials/` (`index.md`, `quickstart.md`; 0 hits for `Marten`/`event sourc`), `docs/index.md:69` (the only Event Sourcing hit among `docs/index.md` and `docs/tutorials/*.md`).
- Open issues checked: #1894 (delta of #17, same page and same feature), #1513, #1347, #1349, #1975/#1976 (delta of #25, snapshots).

## Findings

1. **Minor** — `docs/architecture/adr/027-marten-as-the-event-sourcing-provider.md:1` (first line is `# ADR-027: ...`, no `---` front matter) fails rule (a) point 4 and encina-docs SKILL section 2. The sibling ADR on the same topic has it (`docs/architecture/adr/019-compliance-event-sourcing-marten.md:1-6`, `parent: ADRs`, `grand_parent: Architecture`). Related open issue: duplicate of #1894 (checklist item "ADR-027 has no just-the-docs front matter ...").
2. **Minor** — `docs/architecture/adr/027-marten-as-the-event-sourcing-provider.md:13` (`## Decision`) carries an emoji (U+274C, in the quoted `"❌ Encina.EventStoreDB - Excluded"`), which fails rule (a) point 1 (no emojis; `.claude/agents/docs-reviewer.md` point 1). Related open issue: duplicate of #1894 (item "ADR-027 quotes a source decision with an emoji").
3. **Minor** — `docs/architecture/adr/027-marten-as-the-event-sourcing-provider.md:15-19` (`## Rationale` at :15, its three bullets at :17-19) compares Marten and EventStoreDB (infrastructure, projection language, implementations to verify) in three prose bullets; the page has 0 tables and 0 diagrams (command: `Select-String '^\s*```'` returned no hits, no `|` table rows). Rule (a) point 1 asks for a table where a comparison is shown. Related open issue: duplicate of #1894 (item "compares Marten and EventStoreDB in prose bullets").
4. **Major** — Rule (a) point 5: `Encina.Marten` is the sole event-sourcing provider (ADR-027) and `src/Encina.Marten/README.md` does not exist, `docs/features/` has no event-sourcing or aggregate-repository page, and `docs/tutorials/` (`index.md`, `quickstart.md`) has 0 hits for `Marten` and `event sourc`; `docs/index.md:69` is a one-row mention only. AGENTS.md section 8 also requires a README per satellite package. The issue's own audit (`docs/knowledge/audits/issue-15.md`, "Docs/README accuracy" row) recorded the same gap as a FAIL but no destination was opened for it. Related open issue: duplicate of #1894 (item "No concept or how-to page, package README or tutorial entry for event sourcing on Encina.Marten"); #1975/#1976 cover only the snapshot sub-feature.

## Informational (not findings)

- Points 2 and 3 of rule (a) do not apply to the ADR: it has no `csharp` block and no coverage, mutation or performance figure. Searched with `Select-String` for `\d+(\.\d+)?\s?(%|ms|ns)`: 0 hits.
- ADR-027 `## Consequences` says `Encina.EventStoreDB` "remains in the repository as deprecated code"; `Test-Path src\Encina.EventStoreDB` is False and `Encina.slnx` lists no EventStoreDB project. Already tracked by #1513 (open), so not a new finding.
- `AGENTS.md:70` ("EventStoreDB future") and `docs/INVENTORY.md:89` ("EventStoreDB (future)") still contradict ADR-027; this is a content-accuracy item (not rule (a)) covered for `CLAUDE.md`/`AGENTS.md` by #1347 (open). `docs/INVENTORY.md:89` is not named in #1347's title; the implementer of #1347 may add it.
- The orchestration-duplication gotcha (#1349) and the `error.Message` leak are code items, outside this delta.
- Tooling: `lychee`, `markdownlint-cli2` and the coverage-citations check were not run; the delta is rule (a) only and the ADR has no relative links beyond the ones in `docs/architecture/adr/index.md:38`.

## Lessons for the pipeline

- When the delta scope is a closed-in-error issue whose only destination is an ADR already reviewed by the delta of a sibling issue (#17 here, #1894), all four rule (a) findings are duplicates; the stage's value is the duplicate map plus one check that no other page needs work. Read the sibling umbrella issue's checklist first and map before measuring.
