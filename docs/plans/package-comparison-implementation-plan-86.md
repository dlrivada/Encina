# Implementation Plan: Package Comparison Tables — reference page, provider-selection guide and entry points

> **Issue**: [#86](https://github.com/dlrivada/Encina/issues/86)
> **Type**: Feature (documentation)
> **Complexity**: Medium (4 phases, 6 files, no production code)
> **Estimated Scope**: about 600-800 changed lines of Markdown, no tests

---

## Summary

Give a reader choosing packages one place to compare them: a Reference page with the comparison tables for the four categories of the issue (database providers, caching providers, messaging transports, validation libraries) and a How-to page with the Mermaid decision flowcharts. Every cell is read from `src/` or cited from a page that already holds the fact; nothing is typed from memory, and the "Good" and "Excellent" performance ratings of the original issue are replaced by citations of the benchmark pages or dropped.

What the code and the docs say today (read on 2026-11-11):

- `docs/features/database-providers.md` already holds the database feature-by-provider matrix, written from store types in `src/` ("a store type living in `src/` is the only evidence this table accepts"), plus the package and registration table. The comparison page therefore links and summarises it for databases and adds only the dimensions it lacks (abstraction level, LINQ support, performance citations); it does not copy the matrix.
- The original table's "Migrations" row (EF Core yes, others no) has no Encina feature behind it for the data-access packages: `src/Encina.EntityFrameworkCore` has no migrations folder, and the only migration code in `src/` is `src/Encina/Sharding/Migrations/` (schema migration coordination for sharding). The row is dropped unless the worker finds code that supports it.
- The original MongoDB "Transactions: single doc" cell and the "Unit of Work" row have code to check: `src/Encina.MongoDB` has `Transactions/` and `UnitOfWork/` folders; `src/Encina.ADO.SqlServer` has `UnitOfWork/` and no `Transactions/` folder. The worker reads the types before writing any cell.
- Caching: Memory, Hybrid, Redis, Valkey, Dragonfly, Garnet and KeyDB exist under `src/Encina.Caching.*`; Memcached is planned. Performance is cited from `docs/benchmarks/caching-performance.md` and `caching-memory-`, `caching-hybrid-` and `caching-redis-benchmark-results.md`; the other four have no published benchmark page, so their cell says "no published benchmark".
- Transports: RabbitMQ, AzureServiceBus, AmazonSQS, Kafka, NATS, Redis.PubSub, MQTT, InMemory, gRPC and GraphQL exist as packages; GoogleCloudPubSub, AmazonEventBridge, Pulsar, Redis.Streams, ActiveMQ and Dapr are planned for 1.0 and have no package yet (AGENTS.md section 5, SPEC-000 DEC-009).
- Validation: `Encina.FluentValidation`, `Encina.DataAnnotations`, `Encina.MiniValidator`, all through `ValidationOrchestrator` and `ValidationPipelineBehavior` (AGENTS.md section 3). The original cells "Async validation" and "Custom rules" are checked against each provider's code.
- No comparison page exists in `docs/` today, and `README.md` has no link to one. The closed duplicate #4 held the transport table and four comparison dimensions that were not copied here; the SPEC-003 audit comment on #86 carries them in.

**Provider categories**: database (10), caching (8), transports (10 plus 6 planned), validation (3). Planned items are marked planned.

**Affected packages**: none changed.

**Size for the worker brief**: 6 files, about 700 lines. Within the 15-file and 1,500-line limit of one pull request.

---

## Design Choices

<details>
<summary><strong>1. Page shape — a Reference page for the tables and a separate How-to for the flowcharts</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `docs/features/package-comparison.md` (Reference) and `docs/guides/how-to-choose-a-provider.md` (How-to with the Mermaid flowcharts), linked sideways** | One quadrant per page; the tables stay scannable; the flowcharts can change without touching the facts | Two files |
| **B) One page with tables and flowcharts, as the issue words it** | One file, one place to look | Mixes reference and how-to; breaks the one-quadrant rule |

### Chosen Option: **A — Reference page plus How-to page (recommended, pending the maintainer)**

### Rationale

- Scenario where A wins: readers either look a fact up (tables) or decide what to use (flowcharts); the skill treats these as different moods.
- Scenario where B wins: the maintainer wants a single "compare and choose" page for discoverability and accepts the mixed quadrant; the issue text asks for tables plus flowcharts in the same deliverable.
- The skill says a feature that needs several moods becomes several pages; the issue does not say one page.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Database section — link and summarise docs/features/database-providers.md instead of copying its matrix</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A short table of dimensions the existing page lacks (abstraction level, LINQ support, benchmark citations) for the 10 providers, plus a link to the feature-by-provider matrix** | One owner for the store-type matrix; no drift | The reader follows a link for the store names |
| **B) Re-create the matrix in the comparison page** | Self-contained | Two matrices to keep in step; the existing one is the evidence-based table |

### Chosen Option: **A — Link and add only what is missing**

### Rationale

- The existing page is already the verified source of "which store exists for which provider".
- The original table's families (EF Core, Dapper, ADO.NET, MongoDB) are expanded to the 10 providers of AGENTS.md section 5.

**Class:** Settled
**Settled by:** `.claude/skills/encina-docs/SKILL.md` section 3 rule 4 (decisions and facts are linked, not restated) and section 3 rule 5 (provider matrix complete or explicit); AGENTS.md section 5 (10 database providers).

</details>

<details>
<summary><strong>3. Performance — citations of benchmark pages, never ratings</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Cite `docs/benchmarks/` pages per provider; "no published benchmark" where none exists** | Evidence-backed; honest gaps | Several cells read "no published benchmark" |
| **B) Keep "Good" and "Excellent" labels** | Short | Hand-typed claims with no source, which the house rules forbid |

### Chosen Option: **A — Citations only**

### Rationale

- Benchmark pages that exist: `dapper-vs-ado-comparison.md`, `ado-*-benchmark-results.md`, `dapper-*-benchmark-results.md`, `mongodb-benchmark-results.md`, `caching-performance.md`, `caching-memory-`, `caching-hybrid-` and `caching-redis-benchmark-results.md`, `id-generation-comparison.md`. The page cites them; it types no figure.

**Class:** Settled
**Settled by:** `.claude/skills/encina-docs/SKILL.md` section 3 rule 2 (no hand-typed figures); SPEC-001 (citations); SPEC-003 audit comment on #86.

</details>

<details>
<summary><strong>4. Transport "Best for" column — drop it or keep it with a source per row</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep "Type" (factual, from each broker's own documentation) and drop "Best for"** | No unsourced opinion; nothing to go stale | The reader gets less guidance from the table; the flowchart carries the guidance |
| **B) Keep "Best for" with a link to the vendor documentation on each row** | More guidance in the table | Opinions need a source each; ten rows to maintain; a vendor can change its positioning |

### Chosen Option: **A — Keep "Type", drop "Best for" (recommended, pending the maintainer)**

### Rationale

- Scenario where A wins: the maintainer wants the page to hold only what a reviewer can verify.
- Scenario where B wins: the maintainer wants the table to answer "which one for my case" without opening the flowchart, and accepts the maintenance.
- The how-to page (Design Choice 1) carries the "which one for" guidance with reasons.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Every cell verified in src/, and planned providers marked planned</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A cell is a mark only when a type or package in `src/` backs it; planned providers have a "planned" column or row with the issue** | Accurate; the page cannot promise what is absent | More reading per cell |
| **B) Copy the original ticks and warnings** | Fast | The original table named features with no code (Migrations) and omitted providers |

### Chosen Option: **A — Verified cells, planned marked**

### Rationale

- Lists come from AGENTS.md section 5: 10 database providers, 8 caches (Memcached planned), 10 transports plus 6 planned for 1.0, 3 validation libraries.
- Where a provider lacks a feature the table says "not supported" with the reason "no such type in the package", as `docs/features/database-providers.md` does.

**Class:** Settled
**Settled by:** AGENTS.md section 5 (provider lists and the rule that a provider-dependent page covers all providers or states which); `.claude/skills/encina-docs/SKILL.md` section 3 rules 3 and 5.

</details>

<details>
<summary><strong>6. Flowcharts are Mermaid</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Mermaid `flowchart` blocks** | Rendered by the site (`mermaid.version: 11`) and by GitHub; reviewable as text | Layout is automatic |
| **B) Images** | Full control | Not diffable, not accessible |

### Chosen Option: **A — Mermaid**

### Rationale

- One flowchart per category (database, caching, transport, validation), each decision node phrased as a requirement the reader can answer.

**Class:** Trivial
**Settled by:** `.claude/skills/encina-docs/SKILL.md` section 2 (Mermaid is available).

</details>

---

## Implementation Phases

### Phase 1: Reference page — `docs/features/package-comparison.md`

<details>
<summary>Tasks</summary>

1. Create the page, front matter `title: "Package comparison"`, `layout: default`, `parent: "Features"`. Opening sentence: for a developer choosing between Encina's packages of one category; they get a fact table per category.
2. Database: table of the 10 providers with abstraction level and LINQ support (read from the packages), links to the benchmark pages per family, and a link to `docs/features/database-providers.md` for the store matrix and registration (Design Choice 2). Decide each of the original rows (Outbox, Inbox, Sagas, Scheduling, Transactions, Unit of Work) by reading the store types; drop "Migrations" unless code backs it.
3. Caching: the 8 caches (Memory L1, Hybrid L1+L2, Redis, Valkey, Dragonfly, Garnet, KeyDB present; Memcached planned) with distributed, persistence and multi-tier cells read from the packages, and benchmark citations or "no published benchmark".
4. Transports: the 10 existing transports with type (Design Choice 4), and the 6 planned for 1.0 (GoogleCloudPubSub, AmazonEventBridge, Pulsar, Redis.Streams, ActiveMQ, Dapr) in a "Planned" table.
5. Validation: the 3 libraries with custom rules, async validation and attribute-based cells read from each provider.
6. A "Not in the list" note: Oracle and SQLite are out (ADR-009, ADR-024); post-1.0 items are named as such.
7. Opening and closing links: to the how-to page of Phase 2 and to the data-access explanation `docs/architecture/data-access-providers.md`.
8. Each category states its date of reading and how the packages were listed (a directory listing of `src/`), as the skill requires for counts.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT
Encina is a pre-1.0 .NET 10 library. Worktree: <worktree> (absolute paths only). You write one Reference page of the Diataxis model. Plan: docs/plans/package-comparison-implementation-plan-86.md.

TASK
Create <worktree>\docs\features\package-comparison.md as listed in Phase 1. Read first: docs/features/database-providers.md (do not copy its matrix), the package folders under src/ for caching, transports and validation, docs/benchmarks/README.md and the benchmark pages it lists, AGENTS.md section 5.

KEY RULES
- A cell is a mark only when a type or package in src/ backs it; otherwise write "not supported" or "planned" with the issue or reason. Drop the original "Migrations" row unless code backs it.
- No hand-typed performance ratings: cite benchmark pages or write "no published benchmark".
- Cover every provider of each category or state which it omits and link the issue (10 database, 8 caching with Memcached planned, 10 transports plus 6 planned, 3 validation).
- Reference only: no recommendations, no flowcharts (they go to the how-to page).
- English only, no emojis, markdownlint and link check clean; commit locally, never push.

REFERENCE FILES
.claude/skills/encina-docs/SKILL.md, .claude/skills/encina-docs/diataxis.md, docs/features/database-providers.md, docs/architecture/data-access-providers.md, docs/benchmarks/, AGENTS.md section 5
```

</details>

### Phase 2: How-to page — `docs/guides/how-to-choose-a-provider.md`

<details>
<summary>Tasks</summary>

1. Create the page, front matter `title: "How to choose a provider"`, `layout: default`, `parent: "Guides"`. If the maintainer answers Design Choice 1 with one page, fold these flowcharts into the Phase 1 page and skip this phase.
2. One Mermaid flowchart per category, each node a requirement ("Need persistence across restarts?", "Already running RabbitMQ?") with the outcomes naming a package as written in the reference page.
3. A short paragraph per category giving the reason behind each outcome, each reason pointing to a fact in the reference page or to an ADR (for example ADR-009, ADR-024).
4. Link sideways: the reference page for the facts, `docs/architecture/data-access-providers.md` for the explanation.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT
Encina is a pre-1.0 .NET 10 library. Worktree: <worktree> (absolute paths only). Phase 1 created docs/features/package-comparison.md. You write one How-to page. Plan: docs/plans/package-comparison-implementation-plan-86.md.

TASK
Create <worktree>\docs\guides\how-to-choose-a-provider.md as in Phase 2: four Mermaid flowcharts (database, caching, transport, validation) and the reason for each outcome, linked to the reference page.

KEY RULES
- Every outcome names a package that exists in src/ or is marked planned; no invented provider.
- Every reason is a fact from the reference page or an ADR link; no unsourced performance claim.
- Mermaid syntax compatible with GitHub and the site; no ASCII art.
- English only, no emojis; commit locally, never push.

REFERENCE FILES
.claude/skills/encina-docs/SKILL.md, docs/features/package-comparison.md, docs/architecture/data-access-providers.md, docs/architecture/adr/009-remove-oracle-provider-pre-1.0.md, docs/architecture/adr/024-remove-sqlite-provider-pre-1.0.md
```

</details>

### Phase 3: Entry points

<details>
<summary>Tasks</summary>

1. `README.md`: one link to the comparison page (find the section first, for example near "Packages" or "Documentation").
2. `docs/features/index.md`: one entry under the group whose neighbours share the topic (the "Database & Sharding" or "Other" group).
3. `docs/guides/index.md`: one entry for the how-to page.
4. `docs/features/database-providers.md`: one sentence linking back to the comparison page.
5. No `docs/toc.yml`; no link to a page that is not in the same PR.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT
Encina is a pre-1.0 .NET 10 library; the site is Jekyll with just-the-docs. Worktree: <worktree> (absolute paths only). Phases 1 and 2 created docs/features/package-comparison.md and docs/guides/how-to-choose-a-provider.md.

TASK
Add the links of Phase 3 in docs/plans/package-comparison-implementation-plan-86.md to README.md, docs/features/index.md, docs/guides/index.md and docs/features/database-providers.md. One sentence and one link each.

KEY RULES
- Do not copy content; link.
- Relative links resolve (Test-Path from each file's folder).
- English only, no emojis; commit locally, never push.

REFERENCE FILES
README.md, docs/features/index.md, docs/guides/index.md, docs/features/database-providers.md
```

</details>

### Phase 4: Verification and finalization

<details>
<summary>Tasks</summary>

1. Run the skill's section 5 checks (lychee offline, markdownlint-cli2, `cov-docs-render.cs --check-dangling`); report a missing tool and check relative links with `Test-Path`.
2. For every table cell, list the type or package that backs it in the PR description (a short evidence table); a cell without evidence is removed.
3. Validate each Mermaid block renders (GitHub preview or the site build in `docs.yml`).
4. Run `docs-reviewer` on the two pages.
5. `docs/INVENTORY.md`: add the pages if it lists them. No changelog fragment unless the maintainer says documentation is user-visible. No `src/` change.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT
Encina is a pre-1.0 .NET 10 library. Worktree: <worktree> (absolute paths only). Phases 1 to 3 are committed.

TASK
Run the verification of Phase 4 in docs/plans/package-comparison-implementation-plan-86.md, fix every finding and report the exact output of each command plus the cell-evidence table.

KEY RULES
- Do not skip a tool silently: report a missing tool and check relative links with Test-Path.
- A table cell with no backing type or package in src/ is removed or changed to "not supported".
- No changes under src/, no PublicAPI, EventId or coverage-manifest edits.
- English only, no emojis, no AI attribution; never push.

REFERENCE FILES
.claude/skills/encina-docs/SKILL.md sections 5 and 6, .github/lychee.toml, .markdownlint.json, .github/scripts/cov-docs-render.cs, docs/INVENTORY.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard or specification | Relevance | Where it applies |
|---------------------------|-----------|------------------|
| AGENTS.md section 5 | Provider lists of each category and the planned items | All tables |
| SPEC-000 DEC-009 | The planned transports and the exact lock set are in 1.0 scope | Transport and planned tables |
| [ADR-009](../architecture/adr/009-remove-oracle-provider-pre-1.0.md) and [ADR-024](../architecture/adr/024-remove-sqlite-provider-pre-1.0.md) | Oracle and SQLite are out of the matrix | Database note |
| SPEC-001 | Citations instead of typed figures | Performance cells |
| Diátaxis | Reference and How-to are separate pages | `.claude/skills/encina-docs/diataxis.md` |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| Database providers reference | docs/features/database-providers.md | existing page with packages and stores per provider; linked and reused, not copied |
| Data access explanation | docs/architecture/data-access-providers.md | linked from the database comparison |
| Benchmark pages | docs/benchmarks/ | cited for performance (dapper-vs-ado-comparison.md, caching-performance.md and the per-provider result pages) |
| Caching packages | src/Encina.Caching.Memory (and Hybrid, Redis, Valkey, Dragonfly, Garnet, KeyDB) | features read from each package |
| Transport packages | src/Encina.RabbitMQ (and AzureServiceBus, AmazonSQS, Kafka, NATS, Redis.PubSub, MQTT, InMemory, gRPC, GraphQL) | features read from each package |
| Validation packages | src/Encina.FluentValidation (and DataAnnotations, MiniValidator) | features read from each package |
| Messaging stores | src/Encina.Messaging | provider-agnostic abstractions the comparison is checked against |
| AGENTS.md section 5 | AGENTS.md | provider lists of each category |
| Features index | docs/features/index.md | lists the new page |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| None | None | Documentation only: no `[LoggerMessage]`, so nothing to register in `src/Encina/Diagnostics/EventIdRanges.cs` |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Reference page (docs/features/package-comparison.md) | 1 | new |
| How-to page (docs/guides/how-to-choose-a-provider.md) | 1 | new; dropped if Design Choice 1 is answered with one page |
| README.md | 1 | one link |
| docs/features/index.md | 1 | one entry |
| docs/guides/index.md | 1 | one entry |
| docs/features/database-providers.md | 1 | one back-link |
| Changelog fragment | 0 | not applicable |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt — all phases</summary>

```text
PROJECT CONTEXT
Encina is a pre-1.0 .NET 10 / C# 14 library (no backward compatibility, no [Obsolete]). You are a docs-writer in the git worktree <worktree> (absolute paths only; never touch the main checkout). Rules: AGENTS.md and .claude/skills/encina-docs/SKILL.md. Issue: https://github.com/dlrivada/Encina/issues/86. No production code changes.

IMPLEMENTATION OVERVIEW
1. Create docs/features/package-comparison.md (Reference): database (10 providers), caching (8, Memcached planned), transports (10 plus 6 planned), validation (3); every cell backed by a type or package in src/; performance cited from docs/benchmarks/.
2. Create docs/guides/how-to-choose-a-provider.md (How-to): four Mermaid flowcharts with the reason for each outcome (or fold them into step 1 if the maintainer chose one page).
3. Link from README.md, docs/features/index.md, docs/guides/index.md and docs/features/database-providers.md.
4. Verify (lychee, markdownlint-cli2, cov-docs-render check, cell-evidence table, Mermaid render), run docs-reviewer, commit locally.

KEY PATTERNS
- Do not copy the store matrix of docs/features/database-providers.md; link it.
- The original "Migrations" row has no code behind it in the data-access packages: drop it unless the worker finds code.
- Planned providers are marked planned with the issue; no hand-typed performance ratings; "no published benchmark" is a valid cell.
- One Diataxis quadrant per page, every identifier verified in src/, English only, no emojis.

REFERENCE FILES
docs/plans/package-comparison-implementation-plan-86.md, .claude/skills/encina-docs/SKILL.md, docs/features/database-providers.md, docs/benchmarks/README.md, docs/architecture/data-access-providers.md, AGENTS.md section 5
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Documentation only; the caching comparison documents the providers, no runtime change. |
| 2 | OpenTelemetry | ❌ | Documentation only, no runtime code, no metrics or spans. |
| 3 | Structured Logging | ❌ | Documentation only; no `[LoggerMessage]`, so no EventId range is reserved. |
| 4 | Health Checks | ❌ | Documentation only; health check coverage per provider is the subject of #907, not repeated here. |
| 5 | Validation | ❌ | Documentation only; the validation comparison documents the three libraries, no runtime change. |
| 6 | Resilience | ❌ | Documentation only, no runtime code. |
| 7 | Distributed Locks | ❌ | Documentation only; the lock providers are the subject of #908 and are not part of the four categories of this issue. |
| 8 | Transactions | ❌ | Documentation only; the Transactions row is checked against the code, no change. |
| 9 | Idempotency | ❌ | Documentation only, no runtime code. |
| 10 | Multi-Tenancy | ❌ | Documentation only, no runtime code. |
| 11 | Module Isolation | ❌ | Documentation only, no runtime code. |
| 12 | Audit Trail | ❌ | Documentation only, no runtime code. |

---

## Prerequisites & Dependencies

- None blocking. Related: #85 (MediatR migration guide, plan `mediatr-migration-guide-implementation-plan-85.md`; link each other only after both exist), #4 (closed duplicate whose transport table is carried here), #907 and #908 (health checks and lock pages, linked when merged).
- The planned transports (GoogleCloudPubSub, AmazonEventBridge, Pulsar, Redis.Streams, ActiveMQ, Dapr) have issues of their own; the worker finds the numbers with `gh api repos/dlrivada/Encina/issues` before writing the "Planned" table and does not invent them.

---

## Next Steps

1. The orchestrator links this plan from #86 and asks the maintainer about Design Choices 1 and 4 when work on the issue starts.
2. Brief a `docs-writer` for the two pages and four link edits (6 files, about 700 lines).
3. If #85 and #86 merge in either order, add the sideways link in a follow-up; never link an unmerged page.
