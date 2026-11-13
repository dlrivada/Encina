# Implementation Plan: How to migrate from MediatR to Encina — how-to guide and entry points

> **Issue**: [#85](https://github.com/dlrivada/Encina/issues/85)
> **Type**: Feature (documentation)
> **Complexity**: Medium (4 phases, 6 files, no production code)
> **Estimated Scope**: about 500-700 changed lines of Markdown, no tests

---

## Summary

Write `docs/guides/how-to-migrate-from-mediatr.md`, a How-to page for a developer who already runs MediatR and wants to move a codebase to Encina: a mapping from each MediatR concept to the Encina type that exists today, the steps in an order that keeps the build green, before and after samples that compile, and the traps. Link it from `docs/guides/index.md`, `README.md`, `docs/index.md`, `docs/tutorials/index.md` and from the MediatR comparison in `docs/introduction.md`, which stays the only place with the comparison table.

What the code says today (read from `src/` on 2026-11-11), which replaces the identifiers the original issue text used (`SendAsync`, `HandleAsync`, `IStreamQuery` do not exist):

| MediatR | Encina (file) |
|---------|---------------|
| `IMediator.Send` | `IEncina.Send<TResponse>(IRequest<TResponse>, CancellationToken)` returning `ValueTask<Either<EncinaError, TResponse>>` (`src/Encina/Abstractions/IEncina.cs` line 46; overload with `IRequestContext` at 77) |
| `IMediator.Publish` | `IEncina.Publish<TNotification>` returning `ValueTask<Either<EncinaError, Unit>>` (line 89) |
| `IMediator.CreateStream` | `IEncina.Stream<TItem>(IStreamRequest<TItem>, ...)` returning `IAsyncEnumerable<Either<EncinaError, TItem>>` (line 144) |
| `IRequestHandler<,>` | `IRequestHandler<in TRequest, TResponse>` with `Task<Either<EncinaError, TResponse>> Handle(TRequest, CancellationToken)` (`IRequestHandler.cs` line 57), or `ICommandHandler<,>` / `IQueryHandler<,>` (`CqrsContracts.cs`) |
| `IPipelineBehavior<,>` | `IPipelineBehavior<TRequest, TResponse>` with a `ValueTask<Either<EncinaError, TResponse>> Handle(...)` (`IPipelineBehavior.cs` line 51) |
| `INotificationHandler<>` | `INotificationHandler<in TNotification>` with `Task<Either<EncinaError, Unit>> Handle(...)` (`INotificationHandler.cs` line 52) |
| `IStreamRequestHandler<,>` | `IStreamRequestHandler<in TRequest, TItem>` (`IStreamRequestHandler.cs` line 61) |
| `AddMediatR(...)` | `AddEncina(this IServiceCollection, params Assembly[])` and `AddEncina(services, Action<EncinaConfiguration>?, params Assembly[])` (`src/Encina/Core/ServiceCollectionExtensions.cs`) |

Facts that shape the guide:

- Encina is pre-1.0 and not published to nuget.org: packages ship only to GitHub Packages, so the step "update package references" cannot say `dotnet add package Encina`. It links the quickstart (`docs/tutorials/quickstart.md`), which builds a local feed from a clone.
- `docs/introduction.md` already has a "Comparison with MediatR" section (line 48) and a competitive-landscape section; the guide links to them and does not copy the table.
- `docs/guides/how-to-write-a-pipeline-behavior.md` exists and is linked from the behavior step.
- There is no `docs/migration/` folder and no skill rule for one; the skill puts how-to pages in `docs/guides/`, title starting "How to" (SKILL.md section 1).
- The closed duplicate #4 and the SPEC-003 audit comment on this issue require the README, guides index, docs index and tutorials index links, the introduction cross-links and "every Encina identifier found in `src/`".

**Provider category**: none (core request/handler API only).

**Affected packages**: none changed. Documented: `Encina` (abstractions and `AddEncina`).

**Size for the worker brief**: 6 files, about 600 lines. Within the 15-file and 1,500-line limit of one pull request.

---

## Design Choices

<details>
<summary><strong>1. Page type and placement — a How-to in docs/guides/, titled "How to migrate from MediatR"</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `docs/guides/how-to-migrate-from-mediatr.md`** | Skill-defined home for how-to pages; indexed from `docs/guides/index.md` | None |
| **B) `docs/migration/` folder (the issue's original text)** | A visible name | The skill defines no such location; it needs a new navigation entry in `_config.yml` |
| **C) A section of `docs/introduction.md`** | No new file | Mixes Explanation and How-to; the comparison table already makes that page long |

### Chosen Option: **A — `docs/guides/how-to-migrate-from-mediatr.md`**

### Rationale

- One quadrant per page: the comparison stays Explanation in the introduction, the steps are How-to here.
- The page is a how-to for a task ("move my MediatR code"), so its title starts with "How to" as the skill requires.

**Class:** Settled
**Settled by:** `.claude/skills/encina-docs/SKILL.md` section 1 (how-to guides in `docs/guides/`, title "How to") and the SPEC-003 audit comment on #85 (placement).

</details>

<details>
<summary><strong>2. Which MediatR version the "before" samples target</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Show only constructs identical across the MediatR versions in use (`IRequest<T>`, `IRequestHandler<,>`, `INotification`, `INotificationHandler<>`, `IPipelineBehavior<,>`) and state the version the samples were compiled against** | The guide stays valid for readers on 12.x and on later versions; fewer claims about MediatR to maintain | Version-specific features (for example exception handlers) get a link, not a sample |
| **B) Target the latest MediatR only and compile the "before" samples against it** | Exactly what a new reader installs | Later versions are dual-licensed (the comparison in `docs/introduction.md` cites the licence terms), so compiling needs the licence mechanism; readers still on 12.x get a mismatch |
| **C) Target 12.x only (the last version without the commercial edition)** | Easy to compile without a licence key | Readers on current versions find differences with no warning |

### Chosen Option: **A — Shared constructs, with the compiled-against version stated (recommended, pending the maintainer)**

### Rationale

- Scenario where A wins: the audience mixes teams on 12.x and on current versions, as the migration audience does.
- Scenario where B wins: the maintainer wants the guide to track the current MediatR exactly and accepts the licence-key handling in the scratch project.
- Scenario where C wins: the maintainer wants samples any reader can compile without a licence.
- The question for the maintainer: which MediatR version does the guide state as its baseline? The plan cannot read MediatR's versions from this repository; the worker states the one it compiled against.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Comparison content — link to docs/introduction.md, never copy</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Link to the "Comparison with MediatR" section and add the reverse link** | One owner for the table; cross-links both ways | The reader leaves the page for the "why" |
| **B) Repeat a short table in the guide** | Self-contained | Two tables to keep in step; the introduction's rows are source-cited and a copy goes stale |

### Chosen Option: **A — Link both ways**

### Rationale

- The audit comment on #85 requires that the guide not duplicate the table and that both pages link to each other.
- The guide keeps one sentence of "why" per step and links ADR-001 and ADR-004 for the decisions.

**Class:** Settled
**Settled by:** SPEC-003 audit comment on #85 (2026-10-10); `.claude/skills/encina-docs/SKILL.md` section 3 rule 4 (decisions are linked, not restated).

</details>

<details>
<summary><strong>4. Identifiers — copied from src/, with a compiled before-and-after pair per concept</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Every Encina identifier copied from `src/`; each sample compiled in a scratch project outside the repository** | The guide cannot show code that does not compile (the original issue's mapping did) | A little extra work per sample |
| **B) Samples written from memory and reviewed by reading** | Faster | The original text proves this fails: `SendAsync`, `HandleAsync` and `IStreamQuery` do not exist |

### Chosen Option: **A — Copied and compiled**

### Rationale

- The mapping table in this plan was built by reading the signatures; the worker re-reads them when writing, and the PR description pastes the compile result.

**Class:** Settled
**Settled by:** `.claude/skills/encina-docs/SKILL.md` section 3 rule 3 and section 5; the audit comment on #85 (correction of the mapping).

</details>

<details>
<summary><strong>5. Scope — no converter tool and no compatibility layer</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Manual steps with a checklist; no converter and no `MediatR` shim package** | Matches the pre-1.0 policy; the guide is the deliverable | Larger codebases migrate by hand |
| **B) Document a Roslyn converter or an adapter that lets MediatR handlers run on Encina** | Faster migration | A compatibility layer is forbidden; a converter is new code, outside a documentation issue |

### Chosen Option: **A — Manual steps**

### Rationale

- The guide may suggest finding the call sites with the IDE's "find usages" and shows the mechanical edits; it never presents a bridge as a feature.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (no compatibility layers, migration helpers) and section 1 (pre-1.0, best solution first); `.claude/skills/encina-docs/SKILL.md` section 3 rule 7.

</details>

---

## Implementation Phases

### Phase 1: The guide — `docs/guides/how-to-migrate-from-mediatr.md`

<details>
<summary>Tasks</summary>

1. Create the page with front matter `title: "How to migrate from MediatR"`, `layout: default`, `parent: "Guides"`. Opening sentence: for a developer with a working MediatR codebase; they get a mapping, an ordered set of steps and the traps.
2. Prerequisites: Encina is pre-1.0 and not on nuget.org; link `docs/tutorials/quickstart.md` for the local feed. State the MediatR version the samples were compiled against (Design Choice 2).
3. Concept mapping table: the eight rows of the Summary of this plan, re-read from `src/` when writing, with file and line only if they are still correct.
4. Steps, each a heading with a before-and-after sample:
   - Replace the `MediatR` package reference with the single `Encina` package (`src/Encina/Encina.csproj`; `Encina.Messaging` is not needed to migrate) and `AddMediatR(...)` with `AddEncina(...)`; the scratch project of Phase 3 step 2 proves that one package is enough.
   - Context: `Send`, `Publish` and `Stream` have overloads that take an `IRequestContext` (`IEncina.cs` lines 77, 108, 172); show one in the Send and Publish samples.
   - Pre- and post-processing: MediatR pre/post processors map to `IRequestPreProcessor<TRequest>.Process` and `IRequestPostProcessor<TRequest, TResponse>.Process` (both return `ValueTask<Either<EncinaError, Unit>>`; `src/Encina/Abstractions/IRequestPreProcessor.cs` line 41, `IRequestPostProcessor.cs` line 40) and MediatR exception handlers to `IRequestErrorProcessor<TRequest, TResponse>` (`IRequestErrorProcessor.cs` line 43); re-read the three files when writing and state the mapping only as far as they back it.
   - Requests: `IRequest<T>` stays; choose `ICommand<T>` or `IQuery<T>` where the intent is clear.
   - Handlers: `Handle` returns `Task<Either<EncinaError, TResponse>>`; replace thrown exceptions with `Left` values.
   - Call sites: `IMediator.Send` becomes `IEncina.Send` and the caller handles an `Either` (match, `IfLeft`, or bind).
   - Notifications: `INotificationHandler<>` returns `Task<Either<EncinaError, Unit>>`; `Publish` returns an `Either`.
   - Pipeline behaviors: signature with `Either`; link `docs/guides/how-to-write-a-pipeline-behavior.md`.
   - Streams: `IStreamRequest<TItem>`, `IStreamRequestHandler<,>`, `Stream` returning `IAsyncEnumerable<Either<EncinaError, TItem>>`.
5. Gotchas: errors are values, an uncaught exception is a bug (ADR-001, ADR-006); handler lifetime and registration differences only if the code shows them (read `AddEncina` first, say nothing it does not back); opt-in extras (Outbox, Inbox, Saga) are not needed to migrate and are off by default.
6. "After the migration": links to the features the reader may add (pipeline behaviors page, Outbox page); one sentence each.
7. "See also": the introduction's MediatR comparison, ADR-001, ADR-004.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT
Encina is a pre-1.0 .NET 10 library, not published to nuget.org. Worktree: <worktree> (absolute paths only). You write one How-to page of the Diataxis model for a developer with a working MediatR codebase. Plan: docs/plans/mediatr-migration-guide-implementation-plan-85.md.

TASK
Create <worktree>\docs\guides\how-to-migrate-from-mediatr.md as in Phase 1 of the plan. Read first: src/Encina/Abstractions/IEncina.cs, IRequestHandler.cs, IPipelineBehavior.cs, INotificationHandler.cs, IStreamRequestHandler.cs, CqrsContracts.cs, src/Encina/Core/ServiceCollectionExtensions.cs, docs/introduction.md (comparison), docs/guides/how-to-write-a-pipeline-behavior.md, docs/tutorials/quickstart.md.

KEY RULES
- The identifiers are Send, Handle, Publish, Stream, IStreamRequest<TItem>, IStreamRequestHandler<,>: never SendAsync, HandleAsync or IStreamQuery.
- Compile every Encina sample and every MediatR sample in a scratch project outside the repository; state the MediatR version compiled against.
- Do not copy the comparison table of docs/introduction.md: link it, and link back from it (Phase 2).
- Never describe a migration helper, converter or compatibility shim as a feature; none exists.
- Do not claim anything about MediatR that its own public sources do not state; when unsure, link its documentation.
- English only, no emojis, one Diataxis quadrant, markdownlint and link check clean; commit locally, never push.

REFERENCE FILES
.claude/skills/encina-docs/SKILL.md, .claude/skills/encina-docs/diataxis.md, docs/guides/how-to-write-a-pipeline-behavior.md, docs/architecture/adr/001-railway-oriented-programming.md, docs/architecture/adr/004-reject-mediator-result.md, docs/architecture/adr/006-pure-rop-exception-handling.md
```

</details>

### Phase 2: Entry points and cross-links

<details>
<summary>Tasks</summary>

1. `docs/guides/index.md`: add `- [How to migrate from MediatR](how-to-migrate-from-mediatr.md) — ...` in the style of the existing lines.
2. `docs/introduction.md`: in the "Comparison with MediatR" section add one sentence linking the guide; the table stays as it is.
3. `README.md`: one link (find the fitting section first; the README has a Quick Start and a Documentation section at its end).
4. `docs/index.md`: one entry in the Resources list or the quick-start area where a guide fits.
5. `docs/tutorials/index.md`: one line "Coming from MediatR?" pointing to the guide, because the page fits the learning path as a next step; no new tutorial.
6. Do not edit `docs/introduction.md` before the guide exists in the same PR (a link to a missing page breaks the link check).

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT
Encina is a pre-1.0 .NET 10 library; the site is Jekyll with just-the-docs. Worktree: <worktree> (absolute paths only). Phase 1 created docs/guides/how-to-migrate-from-mediatr.md.

TASK
Add the links of Phase 2 in docs/plans/mediatr-migration-guide-implementation-plan-85.md to docs/guides/index.md, docs/introduction.md, README.md, docs/index.md and docs/tutorials/index.md. One sentence and one link each.

KEY RULES
- Do not copy content or the comparison table; link.
- Relative links resolve (Test-Path from each file's folder).
- English only, no emojis; commit locally, never push.

REFERENCE FILES
docs/guides/index.md, docs/introduction.md, README.md, docs/index.md, docs/tutorials/index.md
```

</details>

### Phase 3: Verification

<details>
<summary>Tasks</summary>

1. Run the skill's section 5 checks: lychee offline, markdownlint-cli2, `cov-docs-render.cs --check-dangling`; when a tool is missing, say so and check relative links with `Test-Path`.
2. Compile all C# samples (Encina and MediatR "before") in a scratch project; paste the result in the PR.
3. Search the guide for the three forbidden identifiers (`SendAsync`, `HandleAsync`, `IStreamQuery`); the result must be empty.
4. Run `docs-reviewer` on the page.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT
Encina is a pre-1.0 .NET 10 library. Worktree: <worktree> (absolute paths only). Phases 1 and 2 are committed.

TASK
Run the verification of Phase 3 in docs/plans/mediatr-migration-guide-implementation-plan-85.md, fix every finding and report the exact output of each command.

KEY RULES
- Do not skip a tool silently: report a missing tool and check relative links with Test-Path.
- Samples compile in a scratch project outside the repository; paste the result.
- No changes under src/, no PublicAPI, EventId or coverage-manifest edits.
- English only, no emojis, no AI attribution; never push.

REFERENCE FILES
.claude/skills/encina-docs/SKILL.md sections 5 and 6, .github/lychee.toml, .markdownlint.json, .github/scripts/cov-docs-render.cs
```

</details>

### Phase 4: Finalization

<details>
<summary>Tasks</summary>

1. `docs/INVENTORY.md`: add the page if it lists guides.
2. Changelog: not applicable unless the maintainer treats documentation as user-visible; no fragment.
3. Report to the orchestrator the MediatR version used (Design Choice 2) so the answer is recorded in the plan's `## Maintainer Decisions` section when the work starts.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT
Encina is a pre-1.0 .NET 10 library. Worktree: <worktree> (absolute paths only). Phases 1 to 3 are committed.

TASK
Finish the pull request of docs/plans/mediatr-migration-guide-implementation-plan-85.md: update docs/INVENTORY.md if it lists guides and write the report (files, command outputs, MediatR version compiled against).

KEY RULES
- No fragment in changelog.d unless the orchestrator says so.
- Do not edit CHANGELOG.md, plans or ADRs.
- English only, no emojis; commit locally, never push.

REFERENCE FILES
docs/INVENTORY.md, .claude/skills/encina-docs/SKILL.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard or specification | Relevance | Where it applies |
|---------------------------|-----------|------------------|
| [ADR-001](../architecture/adr/001-railway-oriented-programming.md) | Handlers return `Either<EncinaError, T>`; errors are values | Handler and call-site steps |
| [ADR-004](../architecture/adr/004-reject-mediator-result.md) | Why `Either` is exposed directly instead of a MediatR-style wrapper | Linked from "See also" |
| [ADR-006](../architecture/adr/006-pure-rop-exception-handling.md) | An uncaught exception is a bug | Gotchas |
| MediatR public documentation | The "before" side; claims about MediatR are limited to what its sources state | Mapping table, samples |
| Diátaxis | One quadrant per page | `.claude/skills/encina-docs/diataxis.md` |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| IEncina | src/Encina/Abstractions/IEncina.cs | Send, Publish and Stream signatures copied into the mapping |
| IRequestHandler | src/Encina/Abstractions/IRequestHandler.cs | Handle returning Task<Either<EncinaError, TResponse>> |
| IPipelineBehavior | src/Encina/Abstractions/IPipelineBehavior.cs | Handle returning ValueTask<Either<EncinaError, TResponse>> |
| INotificationHandler | src/Encina/Abstractions/INotificationHandler.cs | Handle returning Task<Either<EncinaError, Unit>> |
| IStreamRequestHandler | src/Encina/Abstractions/IStreamRequestHandler.cs | streaming handler mapping |
| CQRS contracts | src/Encina/Abstractions/CqrsContracts.cs | ICommand, IQuery and their handlers |
| AddEncina | src/Encina/Core/ServiceCollectionExtensions.cs | assembly scanning registration that replaces AddMediatR |
| Comparison table | docs/introduction.md | existing MediatR comparison; linked, not duplicated |
| Pipeline behavior guide | docs/guides/how-to-write-a-pipeline-behavior.md | linked from the behavior step |
| Guides index | docs/guides/index.md | lists the new guide |
| ADR-001 | docs/architecture/adr/001-railway-oriented-programming.md | linked for the Either decision |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| None | None | Documentation only: no `[LoggerMessage]`, so nothing to register in `src/Encina/Diagnostics/EventIdRanges.cs` |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Migration guide (docs/guides/how-to-migrate-from-mediatr.md) | 1 | new |
| docs/guides/index.md | 1 | one entry |
| docs/introduction.md | 1 | link from the MediatR comparison to the guide |
| README.md | 1 | one link |
| docs/index.md | 1 | one entry |
| docs/tutorials/index.md | 1 | one entry where the page fits |
| Changelog fragment | 0 | not applicable |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt — all phases</summary>

```text
PROJECT CONTEXT
Encina is a pre-1.0 .NET 10 / C# 14 library, not published to nuget.org (no backward compatibility, no [Obsolete], no compatibility layers). You are a docs-writer in the git worktree <worktree> (absolute paths only; never touch the main checkout). Rules: AGENTS.md and .claude/skills/encina-docs/SKILL.md. Issue: https://github.com/dlrivada/Encina/issues/85. No production code changes.

IMPLEMENTATION OVERVIEW
1. Create docs/guides/how-to-migrate-from-mediatr.md: prerequisites (link the quickstart), concept mapping, ordered steps with compiled before-and-after samples, gotchas, see also.
2. Link the guide from docs/guides/index.md, README.md, docs/index.md, docs/tutorials/index.md and from the MediatR comparison in docs/introduction.md (and back); do not copy the table.
3. Verify (lychee, markdownlint-cli2, cov-docs-render check, compile every sample in a scratch project, search for SendAsync, HandleAsync, IStreamQuery), run docs-reviewer, commit locally.

KEY PATTERNS
- IEncina.Send/Publish/Stream return ValueTask<Either<EncinaError, ...>> / IAsyncEnumerable<Either<EncinaError, TItem>>; handlers' Handle returns Task<Either<...>>; behaviors' Handle returns ValueTask<Either<...>>.
- AddEncina(params Assembly[]) replaces AddMediatR.
- The MediatR version of the samples is stated (Design Choice 2, recommended: shared constructs, pending the maintainer).
- One Diataxis quadrant (How-to), links not restatements, every identifier verified in src/, English only, no emojis.

REFERENCE FILES
docs/plans/mediatr-migration-guide-implementation-plan-85.md, .claude/skills/encina-docs/SKILL.md, src/Encina/Abstractions/*.cs, src/Encina/Core/ServiceCollectionExtensions.cs, docs/introduction.md, docs/guides/index.md, docs/tutorials/quickstart.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Documentation only, no runtime code. |
| 2 | OpenTelemetry | ❌ | Documentation only, no runtime code, no metrics or spans. |
| 3 | Structured Logging | ❌ | Documentation only; no `[LoggerMessage]`, so no EventId range is reserved. |
| 4 | Health Checks | ❌ | Documentation only, no runtime code. |
| 5 | Validation | ❌ | Documentation only; the guide may mention that validation behaviors port like any behavior, with a link, nothing more. |
| 6 | Resilience | ❌ | Documentation only, no runtime code. |
| 7 | Distributed Locks | ❌ | Documentation only, no runtime code. |
| 8 | Transactions | ❌ | Documentation only, no runtime code. |
| 9 | Idempotency | ❌ | Documentation only; Inbox is mentioned as opt-in after the migration, no change. |
| 10 | Multi-Tenancy | ❌ | Documentation only, no runtime code. |
| 11 | Module Isolation | ❌ | Documentation only, no runtime code. |
| 12 | Audit Trail | ❌ | Documentation only, no runtime code. |

---

## Prerequisites & Dependencies

- Related: #86 (comparison tables, plan `package-comparison-implementation-plan-86.md`); the two pages link each other only after both exist. #4 (closed duplicate), #90 (README link of the site, no overlap).
- For the "before" samples: a scratch project with a MediatR package of the version Design Choice 2 settles; it is not committed.

---

## Next Steps

1. The orchestrator links this plan from #85 and asks the maintainer about Design Choice 2 when work on the issue starts (priority `p0-mandatory`, so this plan is among the first to be briefed).
2. Brief a `docs-writer` for the guide and the five link edits (6 files, about 600 lines).
3. If #86 merges first, add a "see also" line to the comparison page in the same PR or a follow-up; do not link a page that is not merged.
