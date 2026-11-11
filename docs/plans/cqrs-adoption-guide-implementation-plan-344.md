# Implementation Plan: Progressive CQRS Adoption Guide — a how-to, an explanation and a tutorial

> **Issue**: [#344](https://github.com/dlrivada/Encina/issues/344) (family: #87, #88, #89, #188; links to the finished examples when they exist)
> **Type**: Feature (documentation only; no change under `src/`)
> **Research**: `.claude/skills/encina-docs/SKILL.md`, `src/Encina/Abstractions/CqrsContracts.cs`, `src/Encina.DomainModeling/AggregateBase.cs`, `src/Encina.Marten/IAggregateRepository.cs`, `docs/features/read-write-separation.md`, `docs/architecture/request-pipeline.md`, `docs/tutorials/quickstart.md`
> **ADR**: none (the guide links existing ADRs and records no new decision)
> **Related**: #365 (Vertical Slice Architecture Support, open), #352 (Modular Monolith Support, open), #87 and #88 (examples the tutorial links), `docs/tutorials/quickstart.md` (#81)
> **Complexity**: Low to medium (three pages, three index edits, one README section)
> **Estimated Scope**: 7 files, about 900 lines of documentation

---

## Summary

Encina offers explicit `ICommand<T>` and `IQuery<T>` contracts, and the community's loudest criticism of mediator libraries is "CQRS everywhere": applying the pattern where plain CRUD would do. This plan writes the documentation that tells a developer when to adopt CQRS, in which order, and when not to, so Encina guides as well as provides tools. The target readers are developers new to CQRS, teams evaluating Encina, architects choosing patterns and developers coming from MediatR.

**The deliverable is three pages, one per Diátaxis quadrant, plus the links.** The issue's original paths (`docs/patterns/`, `docs/guides/progressive-cqrs-adoption.md`) are replaced because `docs/patterns/` does not exist and the `encina-docs` skill places a page by quadrant (skill section 1):

| Page | Quadrant and path | Content |
|------|-------------------|---------|
| How to choose and move between CQRS adoption levels | How-to, `docs/guides/how-to-adopt-cqrs-progressively.md` (indexed from `docs/guides/index.md`) | Mermaid decision tree, the five adoption levels, one code sample per level, how to move up one level |
| About when not to use CQRS | Explanation, `docs/architecture/about-cqrs-adoption.md` (parent "Architecture", linked from `docs/architecture/index.md`) | Anti-patterns, cost-benefit table, alternatives, vertical slice integration |
| From CRUD to CQRS | Tutorial, `docs/tutorials/from-crud-to-cqrs.md` (indexed from `docs/tutorials/index.md`) | The e-commerce walkthrough from plain CRUD to commands and queries, with real output |
| `README.md` | Main README | A short "when to use" section linking the three pages |

**What the code samples use, checked in `src/`:** level 1 `ICommand<TResponse>` with `ICommandHandler<,>`; level 2 `IQuery<TResponse>` with `IQueryHandler<,>` and a separate read model (`src/Encina/Abstractions/CqrsContracts.cs`); level 3 an aggregate deriving from `AggregateBase<TId>` (`src/Encina.DomainModeling/AggregateBase.cs`) saved through `IAggregateRepository<T>` (`src/Encina.Marten/IAggregateRepository.cs`); level 4 read/write separation (`MessagingConfiguration.UseReadWriteSeparation`, `docs/features/read-write-separation.md`). **A correction to the original sample:** the aggregate raises events with `protected void RaiseEvent<TEvent>(TEvent @event)` and updates its state in `protected abstract void Apply(object domainEvent)`; there is no `Apply(new OrderPlaced(...))` call that raises an event. The `RaiseEvent` method itself calls `Apply` (`AggregateBase.cs`).

**Scope of the tutorial.** The tutorial runs from a clean checkout without Docker, so it covers levels 0 to 2 (CRUD, commands, queries) and ends by pointing to the how-to for levels 3 and 4. Event sourcing needs Marten on PostgreSQL, which would make the tutorial unreliable to reproduce (the skill requires every "you should see" output to be reproduced from a clean state).

**Provider category**: none. The pages describe database providers only in passing and link to `docs/architecture/data-access-providers.md`; they promise nothing about a provider (skill house rule 5).

**Affected packages**: none; documentation (`docs/guides/`, `docs/architecture/`, `docs/tutorials/`, `README.md`).

**Size**: 1 plan file; the implementation is one worker pull request of 7 files and about 900 lines, within the 15-file and 1500-line limit.

---

## Design Choices

No open design choices. Every choice below is settled by a repository rule; the table lists them for the register.

| Id | Choice | Class | Settled by | Verified |
|----|--------|-------|------------|----------|
| 1 | Pages are placed by Diátaxis quadrant (how-to in `docs/guides/`, explanation in `docs/architecture/`, tutorial in `docs/tutorials/`), not under a new `docs/patterns/` | Settled | `.claude/skills/encina-docs/SKILL.md` section 1 (where things live) | pending |
| 2 | CQRS is presented as optional, with level 0 (no CQRS) as a valid end state | Settled | AGENTS.md section 1 (pay-for-what-you-use; every feature opt-in) | pending |
| 3 | No separate `Encina.Simple` and `Encina.CQRS` packages; one core package serves every level | Settled | AGENTS.md section 1 (pay-for-what-you-use) and the single core package (`src/Encina`) | pending |
| 4 | The tutorial covers levels 0 to 2 and runs without Docker; levels 3 and 4 live in the how-to | Settled | `.claude/skills/encina-docs/SKILL.md` section 5 (every tutorial step's output is reproduced from a clean state) | pending |
| 5 | The cost-benefit table is qualitative (low, medium, high) and the "5+ handlers" threshold is worded as the author's rule of thumb, not a measured figure | Settled | `.claude/skills/encina-docs/SKILL.md` house rule 2 (no hand-typed figures), SPEC-001 | pending |
| 6 | The vertical slice section describes what Encina does today (handlers grouped by feature are ordinary classes) and links #365 as future work; it promises no API | Settled | `.claude/skills/encina-docs/SKILL.md` house rule 7 (nothing pre-1.0 is promised as stable) | pending |
| 7 | Event sourcing guidance links [ADR-019](../architecture/adr/019-compliance-event-sourcing-marten.md) and [ADR-027](../architecture/adr/027-marten-as-the-event-sourcing-provider.md) instead of restating why Marten | Settled | `.claude/skills/encina-docs/SKILL.md` house rule 4 (decisions are linked, not restated) | pending |

---

## Implementation Phases

All pages are written by `docs-writer`, which runs `docs-reviewer` itself (skill section 7). The worker verifies each identifier in `src/` before it appears in a code block.

### Phase 1: How-to page `how-to-adopt-cqrs-progressively.md`

> **Goal**: A reader decides their level and moves up one level with working code.

<details>
<summary>Tasks</summary>

1. `docs/guides/how-to-adopt-cqrs-progressively.md`, front matter `title: "How to adopt CQRS progressively"`, `layout: default`, `parent: "Guides"`. The opening sentence names the reader (a developer deciding how much CQRS to use) and the result (a chosen level and the code to reach it).
2. A Mermaid decision tree: different read and write scaling, then command complexity, then audit needs (event sourcing plus CQRS), else plain CRUD.
3. The adoption-level table (0 no CQRS, 1 commands only, 2 commands and queries, 3 plus event sourcing, 4 plus separate databases) with the use case of each.
4. One code sample per level with real types: level 1 `ICommand<Guid>` and `ICommandHandler<,>` returning `Either<EncinaError, Guid>`; level 2 `IQuery<OrderSummary>` and a read model; level 3 `AggregateBase<Guid>` with `RaiseEvent` and `Apply(object)` and the `IAggregateRepository<T>` call (names verified in `src/Encina.Marten`); level 4 `config.UseReadWriteSeparation = true` with a link to `docs/features/read-write-separation.md`.
5. "Move up one level" steps and a short "when to stop" per level, linking the explanation page and the tutorial.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina is a pre-1.0 .NET 10 mediator library; CQRS contracts are in src/Encina/Abstractions/CqrsContracts.cs. The encina-docs skill (.claude/skills/encina-docs/SKILL.md) governs pages under docs/: one Diataxis quadrant per page, real API names, no hand-typed figures, just-the-docs front matter.
TASK: Spawn docs-writer (foreground, naming the worktree path) to write docs/guides/how-to-adopt-cqrs-progressively.md as in the plan's Phase 1 tasks 1-5. Verify every type in the code blocks with Grep in src/ (ICommand<T>, IQuery<T>, handlers, AggregateBase<TId>, RaiseEvent, Apply, IAggregateRepository<T>, UseReadWriteSeparation).
KEY RULES: How-to quadrant only (no concept essays); aggregate samples raise events with RaiseEvent and update state in Apply(object); say "pre-1.0"; link ADR-019 and ADR-027 instead of restating them; English only; no private client application names.
REFERENCE FILES: src/Encina.DomainModeling/AggregateBase.cs, src/Encina.Marten/IAggregateRepository.cs, docs/features/read-write-separation.md, docs/guides/how-to-write-a-pipeline-behavior.md.
```

</details>

### Phase 2: Explanation page `about-cqrs-adoption.md`

> **Goal**: A reader understands why CQRS everywhere is an anti-pattern and what each level costs.

<details>
<summary>Tasks</summary>

1. `docs/architecture/about-cqrs-adoption.md`, front matter `title: "About when not to use CQRS"`, `layout: default`, `parent: "Architecture"`, a free `nav_order`.
2. Anti-patterns: CQRS for simple CRUD; commands returning the full entity (return an id or success); queries that modify state; one handler per property change; event sourcing for static reference data; eventual consistency where strong consistency is required.
3. The qualitative cost-benefit table (basic mediator, commands only, full CQRS, event sourcing) per Design Choice 5.
4. "When not to use CQRS" section and alternatives (a plain service class, an EF Core `DbContext` as the unit of work, per AGENTS.md section 3 repository guidance).
5. Vertical slice integration per Design Choice 6: each slice decides its complexity (orders full CQRS plus events, settings plain CRUD, reports query-only), with links to #365 and, for module boundaries, #352 and `docs/features/module-isolation.md`.
6. External references as plain links (CQRS Journey, Martin Fowler, Greg Young, Jimmy Bogard, "Don't use MediatR by default").

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: The how-to page exists (Phase 1). This page is the explanation quadrant: it answers "why" and "what are the trade-offs", not "how".
TASK: Spawn docs-writer (foreground, naming the worktree path) to write docs/architecture/about-cqrs-adoption.md as in the plan's Phase 2 tasks 1-6.
KEY RULES: Explanation quadrant only (no step lists); no hand-typed figures (the cost-benefit table is qualitative); do not promise the vertical slice or modular monolith features, link #365 and #352 as open; link the how-to and the tutorial as neighbours; English only; do not restate ADR-019 or ADR-027.
REFERENCE FILES: docs/architecture/request-pipeline.md, docs/architecture/patterns-guide.md, docs/features/module-isolation.md, docs/architecture/data-access-providers.md.
```

</details>

### Phase 3: Tutorial `from-crud-to-cqrs.md`

> **Goal**: A reader turns a plain CRUD service into commands and queries and sees both tracks of the result.

<details>
<summary>Tasks</summary>

1. `docs/tutorials/from-crud-to-cqrs.md`, front matter `title: "From CRUD to CQRS"`, `layout: default`, `parent: "Tutorials"`. It continues from the quickstart project (`docs/tutorials/quickstart.md`) and states that prerequisite in the opening.
2. A linear path for the e-commerce example in memory (no Docker): start with a CRUD `ProductService`; move the write to a command and handler; move the read to a query and a read model; keep the settings page as plain CRUD to show where not to apply CQRS.
3. Each step ends with the real output of `dotnet run`, pasted from a run on a clean checkout (skill section 5); the error track is shown with `error.GetCode()`, not `EncinaError.Message`.
4. "Rolling back a step": keep the old service class beside the handler and switch the endpoint back, with no compatibility layer left in the final code.
5. The closing section points to the how-to for levels 3 and 4 and to the explanation page.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Tutorials are linear paths tested from a clean checkout; the quickstart (docs/tutorials/quickstart.md) builds a local package feed because Encina is not on nuget.org yet.
TASK: Spawn docs-writer (foreground, naming the worktree path) to write docs/tutorials/from-crud-to-cqrs.md as in Phase 3 tasks 1-5. Run the whole sequence from a clean clone and paste the actual output where the page says "you should see".
KEY RULES: Tutorial quadrant only (do what, see this); real API only; no Docker; error output prints the code, never EncinaError.Message; say "pre-1.0"; English only; do not leave a compatibility layer in the final code.
REFERENCE FILES: docs/tutorials/quickstart.md, src/Encina/Abstractions/CqrsContracts.cs, src/Encina/Results/EncinaErrors.cs.
```

</details>

### Phase 4: Indexes, README and links

> **Goal**: The three pages are findable.

<details>
<summary>Tasks</summary>

1. `docs/guides/index.md`: one bullet for the how-to, in the existing format.
2. `docs/architecture/index.md`: one bullet under "Key Documents" for the explanation.
3. `docs/tutorials/index.md`: one bullet for the tutorial.
4. `README.md`: a short "When to use CQRS" section linking the three pages, placed near "Key Features"; no coverage or performance figures.
5. Sideways links per skill section 2: how-to to explanation and tutorial; tutorial to how-to; explanation to how-to. When #87 and #88 have merged, add a link to their example folders (a follow-up edit, not a blocker).

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: The three pages exist (Phases 1-3).
TASK: Spawn docs-writer (foreground, naming the worktree path) for the three index bullets and the README section as in Phase 4 tasks 1-5.
KEY RULES: Keep each index in its existing format; the README section is short and links, it does not repeat the guide; English only.
REFERENCE FILES: docs/guides/index.md, docs/architecture/index.md, docs/tutorials/index.md, README.md.
```

</details>

### Phase 5: Verification and finalization

> **Goal**: The pages pass the documentation gates.

<details>
<summary>Tasks</summary>

1. `lychee --offline --config .github/lychee.toml 'docs/**/*.md' 'README.md'`; if `lychee` is unavailable, check every relative link with `Test-Path` from the page's folder and say so in the report.
2. `npx --yes markdownlint-cli2 "docs/**/*.md" "#docs/INVENTORY.md"`.
3. `dotnet run .github/scripts/cov-docs-render.cs -- --check-dangling --docs-root docs --scan-roots src --manifest-dir .github/coverage-manifest --src-root src` (no coverage citation is expected; the check must stay green).
4. Every identifier in every code block exists in `src/`; the tutorial output is the real output.
5. No changelog fragment (documentation only, the issue says "not needed"), no ADR, no ROADMAP change, no `PublicAPI` change, no `docs/INVENTORY.md` change unless the inventory lists documentation pages (check when writing).

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: All pages are written (Phases 1-4).
TASK: Run the verification of Phase 5 tasks 1-4 from the worktree root and report the real output; spawn docs-reviewer if docs-writer did not.
KEY RULES: Never skip a gate silently; if a tool is missing, say so and use Test-Path for relative links; report findings by file and heading, ranked blocker, major, minor.
REFERENCE FILES: .claude/skills/encina-docs/SKILL.md section 5 and 6, .github/lychee.toml, .markdownlint.json.
```

</details>

---

## Research

### Relevant Standards and Prior Art

| Source | Relevance to this plan |
|--------|------------------------|
| Diátaxis (`.claude/skills/encina-docs/diataxis.md`) | One quadrant per page: how-to, explanation, tutorial |
| [CQRS Journey (Microsoft)](https://learn.microsoft.com/en-us/previous-versions/msp-n-p/jj554200(v=pandp.10)) | Reference for when CQRS pays off |
| [Martin Fowler, CQRS](https://www.martinfowler.com/bliki/CQRS.html) | "Be careful about using CQRS" in most systems |
| [Greg Young, CQRS Documents](https://cqrs.files.wordpress.com/2010/11/cqrs_documents.pdf) | Origin of the separation and its link to event sourcing |
| [Jimmy Bogard, Vertical Slice Architecture](https://www.jimmybogard.com/vertical-slice-architecture/) | Each slice chooses its own complexity |
| [Don't use MediatR by default](https://goatreview.com/dont-use-mediatr-by-default-net-projects/) | The criticism the guide answers |
| ADR-019 and ADR-027 (`docs/architecture/adr/`) | Why event sourcing in Encina is Marten-based, linked not restated |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `ICommand<T>`, `IQuery<T>`, `ICommandHandler<,>`, `IQueryHandler<,>` | `src/Encina/Abstractions/CqrsContracts.cs` | Samples for levels 1 and 2 |
| `AggregateBase<TId>`, `RaiseEvent`, `Apply` | `src/Encina.DomainModeling/AggregateBase.cs` | Sample for level 3 |
| `IAggregateRepository<T>` | `src/Encina.Marten/IAggregateRepository.cs` | Persistence of the level 3 sample |
| `UseReadWriteSeparation` and the feature page | `src/Encina.Messaging/MessagingConfiguration.cs`, `docs/features/read-write-separation.md` | Level 4 |
| Existing pages to link | `docs/tutorials/quickstart.md`, `docs/architecture/request-pipeline.md`, `docs/guides/how-to-write-a-pipeline-behavior.md` | Neighbours of the new pages |
| Page conventions and gates | `.claude/skills/encina-docs/SKILL.md` | Placement, front matter, verification |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| None | None | Documentation only; no `[LoggerMessage]` and no range registered in `EventIdRanges.cs` |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| New pages | 3 | how-to (about 250 lines), explanation (about 250), tutorial (about 330) |
| Index edits | 3 | `docs/guides/index.md`, `docs/architecture/index.md`, `docs/tutorials/index.md` (a few lines each) |
| README | 1 | The "When to use CQRS" section (about 20 lines) |
| **Total** | **7** | About 900 lines; one worker pull request |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 mediator library with Railway Oriented Programming (Either<EncinaError, T>) and opt-in features (AGENTS.md). Issue #344 adds guidance on progressive CQRS adoption (plan docs/plans/cqrs-adoption-guide-implementation-plan-344.md). Documentation follows the encina-docs skill: one Diataxis quadrant per page, junior-readable English, real API names, no hand-typed figures, just-the-docs front matter.

IMPLEMENTATION OVERVIEW:
1. How-to docs/guides/how-to-adopt-cqrs-progressively.md: Mermaid decision tree, five adoption levels, one real-type sample per level, moving up a level.
2. Explanation docs/architecture/about-cqrs-adoption.md: anti-patterns, qualitative cost-benefit table, alternatives, vertical slice integration (links #365 and #352 as open work).
3. Tutorial docs/tutorials/from-crud-to-cqrs.md: levels 0 to 2 in memory without Docker, real pasted output, error code (not message) on the Left track.
4. Links: bullets in the three index pages, a short README section, sideways links between the three pages.
5. Verification: lychee offline (or Test-Path), markdownlint-cli2, cov-docs-render dangling check, every identifier exists in src/.

KEY PATTERNS: aggregates raise events with RaiseEvent and update state in Apply(object) (AggregateBase.cs); link ADRs instead of restating them; qualitative cost-benefit, no hand-typed figures; say pre-1.0 and promise no unreleased API; no migration paths or compatibility layers; provider claims limited to what exists; English only; no AI attribution; no private client application names.

REFERENCE FILES: .claude/skills/encina-docs/SKILL.md, src/Encina/Abstractions/CqrsContracts.cs, src/Encina.DomainModeling/AggregateBase.cs, src/Encina.Marten/IAggregateRepository.cs, docs/features/read-write-separation.md, docs/tutorials/quickstart.md, docs/guides/index.md, docs/architecture/index.md, docs/tutorials/index.md, README.md.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Documentation only; the reports-slice sample mentions caching in one sentence and links the caching feature page |
| 2 | OpenTelemetry | ❌ | Documentation only |
| 3 | Structured Logging | ❌ | Documentation only |
| 4 | Health Checks | ❌ | Documentation only |
| 5 | Validation | ❌ | Documentation only |
| 6 | Resilience | ❌ | Documentation only |
| 7 | Distributed Locks | ❌ | Documentation only |
| 8 | Transactions | ❌ | Documentation only |
| 9 | Idempotency | ❌ | Documentation only |
| 10 | Multi-Tenancy | ❌ | Documentation only |
| 11 | Module Isolation | ❌ | Documentation only; the modular-monolith link goes to #352 and `docs/features/module-isolation.md` |
| 12 | Audit Trail | ❌ | Documentation only; level 3 is justified by audit and replay needs in prose |

No function is deferred, so no follow-up issue is needed for the matrix.

---

## Prerequisites & Dependencies

| Item | State | Relation |
|------|-------|----------|
| `docs/tutorials/quickstart.md` (#81) | merged | The tutorial continues from it |
| #365 Vertical Slice Architecture Support | open | Linked as future work; no API promised |
| #352 Modular Monolith Support | open | Linked as future work |
| #87 and #88 examples | open (plans in this family) | Links are added after they merge (Phase 4 task 5); not a blocker |
| `docs-writer` and `docs-reviewer` agents | available | Write and review the pages |

**Risks**: the tutorial depends on the local package feed method of the quickstart and must be re-run if that page changes; the aggregate sample depends on `AggregateBase` signatures (checked in this plan, re-checked when writing).

---

## Next Steps

1. Merge this plan.
2. The orchestrator writes one docs-writer brief for Phases 1 to 5 (7 files, about 900 lines).
3. `docs-writer` writes the pages and runs `docs-reviewer`; the orchestrator runs the PR-level review.
4. After #87 and #88 merge, add the links to the example folders in a small follow-up edit.
5. Close #344 with the implementation pull request.
