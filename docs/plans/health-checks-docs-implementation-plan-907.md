# Implementation Plan: Health Checks Documentation — guide, concept page, reference page and entry points

> **Issue**: [#907](https://github.com/dlrivada/Encina/issues/907)
> **Type**: Feature (documentation)
> **Complexity**: Low (5 phases, 11 files, no production code, no provider code)
> **Estimated Scope**: about 900-1,100 changed lines of Markdown, no tests

---

## Summary

Document Encina's health check system end to end: what every package registers, how to enable, rename, tag and time-box a check, how the ASP.NET Core integration aggregates them, how to write a custom check and how to wire readiness and liveness probes in Kubernetes and Docker Compose. The existing how-to guide `docs/guides/health-checks.md` (529 lines, indexed from `docs/guides/index.md`) is reviewed, corrected and expanded; two pages are added so that each page has one Diátaxis quadrant (a concept page `docs/features/health-checks.md` and a reference page `docs/configuration/health-checks-reference.md`); and the entry points named by the issue and by the SPEC-003 delta audit of #24 are linked (`README.md`, `docs/features/index.md`, `docs/tutorials/index.md`, `docs/tutorials/quickstart.md`, `docs/index.md`, `docs/introduction.md`, `src/Encina.Messaging/README.md`).

What the code says today and the plan corrects:

- The guide's samples call `.AddEncinaHealthChecks(sp)` and `.AddEncinaHealthChecks(builder.Services.BuildServiceProvider())`. The only overload is `AddEncinaHealthChecks(this IHealthChecksBuilder builder, IEnumerable<string>? tags = null, HealthStatus? failureStatus = null)` (`src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs:70`), so those samples do not compile.
- `CompositeEncinaHealthCheck` is `internal sealed` (`src/Encina.AspNetCore/Health/CompositeEncinaHealthCheck.cs`): the pages explain its aggregation rule (Healthy, Degraded when any check is degraded and none unhealthy, Unhealthy when any is unhealthy) and never tell the reader to instantiate it.
- The issue text says `ProviderHealthCheck.Enabled`; the type is `ProviderHealthCheckOptions` (`src/Encina.Messaging/Health/ProviderHealthCheckOptions.cs`: `Enabled = true`, `Timeout = 5 s`, `Name`, `Tags = ["encina", "database", "ready"]`, `FailureStatus = Unhealthy`).
- The issue asked for a `docs/toc.yml` entry; that file does not exist. Navigation comes from just-the-docs front matter (`docs/_config.yml`).
- The tag table in the root README (`README.md` lines 424-432) and in the Messaging README ("Available Health Checks", `src/Encina.Messaging/README.md` line 680) lists a provider tag that DI-registered checks do not carry (bug [#1959](https://github.com/dlrivada/Encina/issues/1959), open, milestone v0.14.0). Dilemma 2 below decides how the pages treat that.
- Provider coverage is uneven in the code (found by file-name search of `src/` on 2026-11-11): the pages state it instead of implying parity. There is no `*HealthCheck` class in `Encina.Caching.Memory`, `Encina.Caching.Hybrid`, `Encina.Caching.Valkey`, `Encina.Caching.Dragonfly`, `Encina.Caching.Garnet`, `Encina.Caching.KeyDB`, `Encina.Redis.PubSub`, `Encina.InMemory`, `Encina.GraphQL` or `Encina.DistributedLock.InMemory`. AGENTS.md section 5 requires a documentation page about a provider-dependent feature to cover all providers of the category or state which it covers and link the issue for the rest; an issue file for the gap is written with this plan (see Next Steps).

**Provider category**: none for the documentation itself (it is provider-independent prose), but the reference page covers the database family (ADO.NET x3, Dapper x3, EF Core, MongoDB), caching, transports, locks, scheduling, event sourcing, compliance and security, each as the code registers it today.

**Affected packages**: none changed. Documented: `Encina.Messaging` (`IEncinaHealthCheck`, `EncinaHealthCheck`, `ProviderHealthCheckOptions`), `Encina.AspNetCore` (`HealthCheckBuilderExtensions`, the adapter, the composite), `Encina.Tenancy.AspNetCore` (`AddEncinaTenancy`) and every package that ships a health check.

**Size for the worker brief**: 11 files (3 pages, 8 edits), about 1,000 lines. Within the 15-file and 1,500-line limit of one pull request.

---

## Design Choices

<details>
<summary><strong>1. Page set and quadrants — extend the guide, add one concept page and one reference page</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep the guide as the single page and grow it** | One file, no new navigation | Mixes how-to, concept and a long reference table (its row count is measured in Phase 2); breaks the one-quadrant rule; the table dominates the page |
| **B) Guide (how-to) + `docs/features/health-checks.md` (concept) + `docs/configuration/health-checks-reference.md` (reference)** | One quadrant per page, pages link sideways, reference is scannable and can be re-verified alone | Three files to keep in step |
| **C) Only a reference page inside `docs/features/health-checks.md`** | Fewer files | The issue and the #24 audit ask for separate concept and reference pages; the page would again mix moods |

### Chosen Option: **B — Guide, concept page and reference page**

### Rationale

- The encina-docs skill, section 4 step 3: a feature that needs several moods becomes several pages that link to each other. Section 1 places how-to guides in `docs/guides/`, hand-written reference in `docs/configuration/` or option tables inside `docs/features/<feature>.md`, and explanation in `docs/features/`, `docs/architecture/` and ADRs.
- The concept page explains why Encina has its own `IEncinaHealthCheck` next to ASP.NET Core's `IHealthCheck`, what the composite and the adapter do, and how tags select probes. It links ADR-018 for the cross-cutting function and holds no option tables.
- The reference page holds only lookup material: the signatures of the `AddEncina*` health check extensions, `ProviderHealthCheckOptions`, the default name and tags per package.
- The guide keeps the tasks: register, configure, filter by tag, write a custom check, probes for Kubernetes and Docker Compose.

**Class:** Settled
**Settled by:** `.claude/skills/encina-docs/SKILL.md` section 1 (placement per quadrant) and section 4 step 3 (one page per mood); maintainer audit comment of 2026-10-06 on #907 (SPEC-003 delta audit of #24, finding 9).

</details>

<details>
<summary><strong>2. Documenting tags while bug #1959 is open — describe today's behaviour, or wait for the fix</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Document the behaviour of `main` today and add a visible "known limitation (#1959)" note on the tag columns** | The pages ship in the documentation milestone (v0.21.0) regardless of the bug; the note is honest; the issue acceptance criterion "names and tags read from the code, not from the README" is met literally | If #1959 is fixed afterwards, the tag column and the note change (one more docs edit); readers see a limitation in the docs |
| **B) Sequence the work: implement this plan after #1959 merges, document the fixed behaviour** | Pages are written once; no limitation note | The docs milestone is blocked by a bug that has no plan or owner in this batch; the guide stays with non-compiling samples meanwhile |
| **C) Document only names and the `ProviderHealthCheckOptions.Tags` property, leave the effective tags out until #1959 is fixed** | No page becomes wrong when #1959 changes | A reader cannot filter probes by tag, which is the main reason to read the page |

### Chosen Option: **A — Document today's behaviour with a limitation note (recommended, pending the maintainer)**

### Rationale

- Scenario where A wins: #1959 stays open for weeks (it is p0 in v0.14.0 but has no worker yet) and the docs milestone is due; the corrected samples and the Kubernetes section are valuable now.
- Scenario where B wins: #1959 is picked up in the next few days, so the tag columns would be rewritten almost at once.
- The reference page reads the effective tags from the code at implementation time (the `base(...)` call and `ProviderTags` of each class) and records the date of the reading, so the table is re-checkable. If #1959 has merged when the worker starts, the worker applies B for free and drops the note.
- Open question for the maintainer: is the docs milestone allowed to land before #1959?

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Source of the per-package names and tags — read from the code, listed by hand with a date and the command</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Hand-written table, each row read from the class (`DefaultName`, `base(...)` tags) and dated, with the command that listed the classes** | No new tooling; matches the rule for counts that no dashboard measures; reviewable | Can drift when a package adds a check |
| **B) Copy the README tag table** | Fast | The README table is wrong for DI-registered checks (#1959) and omits the compliance, security, CDC and tenancy checks |
| **C) Generate the table from the source with a script and a CI check** | Never drifts | New tooling and a CI job for one page; out of scope of a documentation issue |

### Chosen Option: **A — Hand-written, read from the code, dated**

### Rationale

- The page states the date and the command that produced the inventory (a file search for `*HealthCheck*.cs` under `src/`), as the skill requires for counts no dashboard measures.
- Package READMEs keep a one-line pointer to the reference page instead of a second copy of the table, so the table has one owner.

**Class:** Settled
**Settled by:** `.claude/skills/encina-docs/SKILL.md` section 3 rules 2 and 3 (no hand-typed figures; every example names real API read from `src/`); bug #1959 (README table wrong).

</details>

<details>
<summary><strong>4. Docker Compose section — a short how-to section in the guide using the repository's own convention</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A section of the guide with a `healthcheck:` stanza for the application container and `depends_on: condition: service_healthy` for its database** | Same page as the Kubernetes section, same probe endpoints | Needs a compose file that was run |
| **B) A separate page under `docs/infrastructure/`** | Close to the other Docker pages | Splits the probes story across two places for one paragraph of content |

### Chosen Option: **A — Section of the guide**

### Rationale

- `docs/infrastructure/docker-infrastructure.md` already shows `healthcheck:` stanzas for the infrastructure services; the new section reuses that style and links to it.
- The docs-writer runs the compose snippet against a minimal sample before keeping it; a snippet that was not run is dropped (skill section 5: examples are verified).

**Class:** Trivial
**Settled by:** `docs/infrastructure/docker-infrastructure.md` (convention for compose health checks) and `.claude/skills/encina-docs/SKILL.md` section 5.

</details>

<details>
<summary><strong>5. Entry points — one link from each page the audit named, no duplicated content</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A one-sentence mention and link in `docs/tutorials/index.md`, `docs/tutorials/quickstart.md`, `docs/index.md`, `docs/introduction.md`, `docs/features/index.md`, plus a retarget of the README and Messaging README links to the new pages** | Meets the audit finding; the quickstart stays one linear path | Seven small edits |
| **B) A new tutorial "Add health checks to the quickstart"** | A tutorial quadrant page | A tutorial must be tested from a clean checkout and is new scope; the issue asks for an entry in the learning path, not a new lesson |

### Chosen Option: **A — Links only**

### Rationale

- The #24 delta audit (finding 9, verified 2026-10-06) says the tutorials index, the quickstart, `docs/index.md` and `docs/introduction.md` never mention health checks; the fix is an entry, not a lesson.
- The quickstart mention is a "next steps" sentence after the final step; it does not add a step to the linear path (skill: a tutorial is one linear path).
- A real tutorial can be a separate `[FEATURE]` later if the maintainer wants one; nothing here blocks it.

**Class:** Settled
**Settled by:** maintainer audit comment of 2026-10-06 on #907 (SPEC-003 delta audit of #24, finding 9) and `.claude/skills/encina-docs/SKILL.md` section 1 (tutorial rows).

</details>

---

## Implementation Phases

### Phase 1: Concept page — `docs/features/health-checks.md`

<details>
<summary>Tasks</summary>

1. Create `docs/features/health-checks.md` (Explanation quadrant), front matter `title: "Health checks"`, `layout: default`, `parent: "Features"`.
2. Opening sentence: who the page is for (a developer deciding how Encina health checks fit an ASP.NET Core application) and what they get (the model, not the steps).
3. Sections, each answering "why/what", none answering "how do I":
   - Why Encina has `IEncinaHealthCheck` (`Name`, `IReadOnlyCollection<string> Tags`, `Task<HealthCheckResult> CheckHealthAsync(CancellationToken)`) in `Encina.Messaging.Health`, independent of ASP.NET Core (`src/Encina.Messaging/Health/IEncinaHealthCheck.cs`).
   - `EncinaHealthCheck` base class: exceptions become Unhealthy; cancellation by the caller propagates (`src/Encina.Messaging/Health/EncinaHealthCheck.cs`); `MergeTags` keeps `encina` in the tag set.
   - The three registration routes, with a Mermaid flow: (a) a package registers `IEncinaHealthCheck` in DI when `ProviderHealthCheck.Enabled`; (b) `AddEncinaHealthChecks` wraps all `IEncinaHealthCheck` services in one internal composite named `encina`; (c) `AddEncinaOutbox`, `AddEncinaInbox`, `AddEncinaSaga`, `AddEncinaScheduling` and the other `AddEncina*` builder methods register one adapter per check.
   - Aggregation rule of the composite, taken from `CompositeEncinaHealthCheck` (Healthy, Degraded, Unhealthy); the type is internal.
   - Why `outbox` is not tagged `ready` by default (`DefaultTagsWithoutReady` in `HealthCheckBuilderExtensions`): backlog must not take an instance out of rotation.
   - What a result may contain: result descriptions and data never carry `EncinaError.Message` or exception messages (AGENTS.md section 3; `EncinaHealthCheckAdapter` comment); link the rule, do not restate it beyond one sentence.
   - Where health checks sit among the 12 cross-cutting functions: link [ADR-018](../architecture/adr/018-cross-cutting-integration-principle.md).
   - The tag limitation of #1959 with a link (per Dilemma 2).
4. End with "See also": the guide (how), the reference page (lookup). No tutorial link here.
5. Mark the page "pre-1.0" where an API name could be mistaken for final.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT
You are documenting Encina, a pre-1.0 .NET 10 library. Worktree: <worktree> (use absolute paths only). The page is an Explanation page of the Diataxis model. The reader is a junior .NET developer who has used ASP.NET Core health checks but has never seen Encina.

TASK
Create <worktree>\docs\features\health-checks.md as described in Phase 1 of docs/plans/health-checks-docs-implementation-plan-907.md. Read these files first and quote only what they say: src/Encina.Messaging/Health/IEncinaHealthCheck.cs, EncinaHealthCheck.cs, ProviderHealthCheckOptions.cs, src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs, CompositeEncinaHealthCheck.cs, EncinaHealthCheckAdapter.cs. Include one Mermaid diagram of the three registration routes.

KEY RULES
- Explanation only: no step-by-step tasks, no option tables (link the guide and the reference page).
- Every identifier exists in src/; grep each before writing it. CompositeEncinaHealthCheck is internal: never show `new CompositeEncinaHealthCheck`.
- English only, no emojis, no hand-typed coverage or mutation figures.
- Say "pre-1.0" where relevant; no [Obsolete], no migration or compatibility notes.
- Do not restate decisions: link ADR-018 and AGENTS.md rules in one sentence.
- Run markdownlint and the link check from the encina-docs skill; commit locally, never push.

REFERENCE FILES
.claude/skills/encina-docs/SKILL.md, .claude/skills/encina-docs/diataxis.md, src/Encina.Messaging/Health/*.cs, src/Encina.AspNetCore/Health/*.cs, docs/features/index.md, docs/architecture/adr/index.md
```

</details>

### Phase 2: Reference page — `docs/configuration/health-checks-reference.md`

<details>
<summary>Tasks</summary>

1. Create `docs/configuration/health-checks-reference.md` (Reference quadrant), front matter `title: "Health checks reference"`, `layout: default`. Check `docs/configuration/` for a parent index first; if none exists, create the page top-level with `nav_order` and say so in the report (do not create an empty index).
2. Table 1 — registration methods, from `HealthCheckBuilderExtensions` (namespace `Encina.AspNetCore.Health`) and `Encina.Tenancy.AspNetCore.Health.HealthCheckBuilderExtensions`: `AddEncinaHealthChecks`, `AddEncinaOutbox`, `AddEncinaInbox`, `AddEncinaSaga`, `AddEncinaScheduling`, `AddEncinaDatabasePool`, `AddEncinaHealthCheck<THealthCheck>`, `AddEncinaModuleHealthChecks`, `AddEncinaIdGenerationHealthCheck`, `AddEncinaReferenceTableReplication`, `AddEncinaTierTransition`, `AddEncinaShardCreation`, `AddEncinaAudit`, `AddEncinaQueryCache`, `AddEncinaTenancy`. Columns: method, default registration name, default tags, parameters (exact signature from the source).
3. Table 2 — `ProviderHealthCheckOptions`: `Enabled` (true), `Timeout` (5 s), `Name`, `Tags` (default `encina`, `database`, `ready`), `FailureStatus` (`Unhealthy`); where `ProviderHealthCheck` is set (`MessagingConfiguration.ProviderHealthCheck`, and the per-package options such as Kafka's `options.ProviderHealthCheck`).
4. Table 3 — per-package inventory of `IEncinaHealthCheck` implementations: package, class, default name, tags as the code declares them, how it is registered (`services.AddSingleton<IEncinaHealthCheck, T>()` when `ProviderHealthCheck.Enabled`, or a builder method). Families, from the scan of 2026-11-11: databases (ADO.NET and Dapper for SqlServer, PostgreSQL and MySQL; EF Core; MongoDB; Marten), caching (Redis, query cache), transports (RabbitMQ, Kafka, NATS, MQTT, AzureServiceBus, AmazonSQS, gRPC, SignalR), locks (Redis, SqlServer), scheduling (Hangfire, Quartz), CDC, compliance, security, tenancy, ID generation, sharding and read/write separation.
5. A "Not covered today" table: the packages without a health check (Memory, Hybrid, Valkey, Dragonfly, Garnet, KeyDB, Redis.PubSub, InMemory, GraphQL, DistributedLock.InMemory), with the link to the issue opened for them. Do not invent a reason.
6. Above Table 3 state the date of the reading and the command that listed the classes; mark the tag column with the #1959 note per Dilemma 2.
7. Link sideways only to the guide and the concept page (reference links to other reference and at most one explanation).

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT
Encina is a pre-1.0 .NET 10 library. Worktree: <worktree> (absolute paths only). You write a Reference page of the Diataxis model: facts to look up, no instructions, no rationale.

TASK
Create <worktree>\docs\configuration\health-checks-reference.md per Phase 2 of docs/plans/health-checks-docs-implementation-plan-907.md. For every row open the class and copy: the DefaultName constant, the tags passed to base(...) (or ProviderTags and how DatabaseHealthCheck combines them), and the registration line in the package's ServiceCollectionExtensions. Copy builder method signatures exactly from src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs and src/Encina.Tenancy.AspNetCore/Health/HealthCheckBuilderExtensions.cs.

KEY RULES
- Names and tags come from the code, never from README tables (they are wrong for DI-registered checks, issue 1959).
- State the date of the reading and the command (a file search for *HealthCheck*.cs under src/) in a sentence; type no other counts.
- A package without a health check is listed under "Not covered today" with the issue link, not described as covered. All 10 database providers and all 8 caching providers must each appear either in Table 3 or in the "Not covered" table.
- Health check results never carry EncinaError.Message or exception messages: do not show sample output that does.
- English only, no emojis; markdownlint and link check clean; commit locally, never push.

REFERENCE FILES
.claude/skills/encina-docs/SKILL.md, src/Encina.Messaging/Health/ProviderHealthCheckOptions.cs, src/Encina.Messaging/Health/DatabaseHealthCheck.cs, src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs, every src/*/Health/*HealthCheck.cs, AGENTS.md section 5
```

</details>

### Phase 3: Review and expand the how-to guide — `docs/guides/health-checks.md`

<details>
<summary>Tasks</summary>

1. Mark each `##` section of the existing guide with its quadrant and move, link or delete everything outside "how-to" (the "Overview" comparison table and the "Architecture by Application Type" prose are explanation: shorten and link the concept page).
2. Fix every non-compiling sample: `.AddEncinaHealthChecks(sp)` (line 28) and `.AddEncinaHealthChecks(builder.Services.BuildServiceProvider())` (lines 63, 138, 441) become `.AddEncinaHealthChecks()` or `.AddEncinaHealthChecks(tags: [...])`. Remove `BuildServiceProvider()` calls inside registration (they build a second container).
3. Verify every other call in every code block with a grep in `src/` (for example `AddNpgSql`, `AddRedis` belong to the `AspNetCore.HealthChecks.*` packages, which the guide already names; check the package names in the "Recommended NuGet Packages" section against NuGet before keeping them).
4. Add the missing how-to sections:
   - Enable, disable or rename a provider check (`ProviderHealthCheck.Enabled`, `Name`, `Timeout`, `FailureStatus`), using `MessagingConfiguration` as the entry point.
   - Filter by tag for liveness and readiness endpoints (`MapHealthChecks` with `HealthCheckOptions.Predicate`), with the #1959 note.
   - Write a custom check: subclass `EncinaHealthCheck` (constructor `(string name, IReadOnlyCollection<string>? tags = null)`, override `CheckHealthCoreAsync`), register with `AddEncinaHealthCheck<THealthCheck>(string name, IEnumerable<string>? tags, HealthStatus? failureStatus)`. The sample must compile and must not put exception text into the result description.
   - Docker Compose (`healthcheck:` and `depends_on: condition: service_healthy`) per Design Choice 4.
5. Keep the Kubernetes section (probes, endpoint configuration, timeouts, best practices); verify each YAML example and each `MapHealthChecks` call; move the "Understanding Probes" paragraph to the concept page if it is explanation.
6. Update front matter if needed and the opening sentence (who, what they get). Keep the file indexed from `docs/guides/index.md`; adjust its one-line description if the page scope changed.
7. Add "See also": concept page and reference page.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT
Encina is a pre-1.0 .NET 10 library. Worktree: <worktree> (absolute paths only). The page <worktree>\docs\guides\health-checks.md exists (529 lines). It must become a pure How-to page of the Diataxis model, with every sample compiling against src/.

TASK
Review and expand the guide per Phase 3 of docs/plans/health-checks-docs-implementation-plan-907.md: classify each section, fix the samples that call AddEncinaHealthChecks(sp) or AddEncinaHealthChecks(BuildServiceProvider()), add the sections for provider options, tag filtering, custom checks and Docker Compose, keep and verify the Kubernetes section.

KEY RULES
- AddEncinaHealthChecks has one overload: (IEnumerable<string>? tags = null, HealthStatus? failureStatus = null). AddEncinaHealthCheck<T> takes (string name, IEnumerable<string>? tags = null, HealthStatus? failureStatus = null) and T : class, IEncinaHealthCheck.
- Never call BuildServiceProvider() inside registration code.
- Custom-check samples and result descriptions never include exception messages or EncinaError.Message (AGENTS.md section 3).
- Every identifier is found in src/ first; a Docker Compose or Kubernetes example that was not run is removed rather than kept.
- Leave explanation to the concept page (link it); no coverage or performance figures typed by hand; English only, no emojis.
- markdownlint and link check clean; commit locally, never push.

REFERENCE FILES
.claude/skills/encina-docs/SKILL.md, docs/guides/health-checks.md, docs/guides/index.md, docs/infrastructure/docker-infrastructure.md, src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs, src/Encina.Messaging/Health/EncinaHealthCheck.cs, src/Encina.Messaging/MessagingConfiguration.cs
```

</details>

### Phase 4: Entry points and navigation

<details>
<summary>Tasks</summary>

1. `README.md` (Health Checks section, line 402): link the guide and the reference page; correct or remove the "Default Tags" column of the table at lines 424-432 per Dilemma 2 (the table must not contradict the reference page).
2. `src/Encina.Messaging/README.md` ("Available Health Checks", line 680): replace the tag column by a one-line pointer to the reference page, or correct it from the code; one owner for the table (Design Choice 3).
3. `docs/features/index.md`: add an entry for the new concept page (the index has no health entry today).
4. `docs/guides/index.md`: keep the entry and align its description with the page.
5. `docs/tutorials/index.md`, `docs/tutorials/quickstart.md`, `docs/index.md`, `docs/introduction.md`: add one sentence and one link each, pointing to the guide; in the quickstart it is a "next steps" sentence after the last step, not a new step.
6. No `docs/toc.yml` (it does not exist); navigation is front matter.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT
Encina is a pre-1.0 .NET 10 library; the site is Jekyll with just-the-docs. Worktree: <worktree> (absolute paths only). Phases 1 to 3 created docs/features/health-checks.md, docs/configuration/health-checks-reference.md and rewrote docs/guides/health-checks.md.

TASK
Add the entry points listed in Phase 4 of docs/plans/health-checks-docs-implementation-plan-907.md: README.md Health Checks section, src/Encina.Messaging/README.md, docs/features/index.md, docs/guides/index.md, docs/tutorials/index.md, docs/tutorials/quickstart.md, docs/index.md, docs/introduction.md. One sentence and one link each, except the two READMEs where the tag table is corrected or replaced by a pointer.

KEY RULES
- Do not copy content between pages; link.
- The README and Messaging README tag tables must not contradict docs/configuration/health-checks-reference.md.
- The quickstart stays one linear path: add the mention after the last step.
- Relative links must resolve (Test-Path from the page folder) and pass lychee when available.
- English only, no emojis; commit locally, never push.

REFERENCE FILES
README.md, src/Encina.Messaging/README.md, docs/features/index.md, docs/guides/index.md, docs/tutorials/index.md, docs/tutorials/quickstart.md, docs/index.md, docs/introduction.md, .claude/skills/encina-docs/SKILL.md
```

</details>

### Phase 5: Verification and finalization

<details>
<summary>Tasks</summary>

1. Run the checks of the encina-docs skill, section 5, from the worktree root: `lychee --offline --config .github/lychee.toml 'docs/**/*.md' 'README.md'` and `npx --yes markdownlint-cli2 "docs/**/*.md" "#docs/INVENTORY.md"`; when a tool is not installed, say so and check every relative link with `Test-Path`.
2. Run the coverage-citation checker (`dotnet run .github/scripts/cov-docs-render.cs -- --check-dangling ...`); the pages cite no coverage, so it must stay green.
3. Compile every C# sample from the guide and the concept page in a scratch project against the repository's packages (not committed); record the result in the PR description.
4. Run `docs-reviewer` on the three pages before the PR.
5. `docs/INVENTORY.md`: add the two new pages if the file lists pages (check; the markdownlint command excludes it, so it is maintained separately).
6. Changelog: documentation changes are not user-visible product changes; no fragment unless the maintainer says otherwise (issue Documentation checklist).
7. No coverage manifest, PublicAPI or EventId work: no `src/` file changes.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT
Encina is a pre-1.0 .NET 10 library. Worktree: <worktree> (absolute paths only). Phases 1 to 4 are committed. This phase proves the pages are correct and complete the pull request.

TASK
Run the verification of Phase 5 in docs/plans/health-checks-docs-implementation-plan-907.md, fix every finding in the pages, and report the exact output of each command.

KEY RULES
- Do not skip a tool silently: if lychee or markdownlint-cli2 is not installed, say so and check relative links with Test-Path.
- Every C# sample in the pages compiles in a scratch project outside the repository tree; paste the result, not a summary.
- No changes under src/, no PublicAPI, EventId or coverage-manifest edits.
- English only, no emojis, no AI attribution in commits; never push.

REFERENCE FILES
.claude/skills/encina-docs/SKILL.md section 5 and 6, .github/lychee.toml, .markdownlint.json, .github/scripts/cov-docs-render.cs, docs/INVENTORY.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard or specification | Relevance | Where it applies |
|---------------------------|-----------|------------------|
| ASP.NET Core health checks (`Microsoft.Extensions.Diagnostics.HealthChecks`, `MapHealthChecks`, `HealthCheckOptions.Predicate`) | The model `IEncinaHealthCheck` is adapted to; tags and predicates select probes | Guide, concept page, reference page |
| Kubernetes liveness, readiness and startup probes | The probe vocabulary of the guide's Kubernetes section | Guide (section kept and verified) |
| Docker Compose `healthcheck` and `depends_on.condition: service_healthy` | Compose section | Guide |
| Diátaxis (tutorial, how-to, reference, explanation) | One quadrant per page | All pages; `.claude/skills/encina-docs/diataxis.md` |
| [ADR-018](../architecture/adr/018-cross-cutting-integration-principle.md) | Health checks are function 4 of the 12 cross-cutting functions | Concept page |
| SPEC-001 citations (covref, mutref) | No hand-typed figures | Skill house rule 2; not used here because the pages cite none |
| AGENTS.md section 3 (`EncinaError.Message` never reaches health-check results) | Custom-check sample must not leak messages | Guide, concept page |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| IEncinaHealthCheck | src/Encina.Messaging/Health/IEncinaHealthCheck.cs | the abstraction every health check implements (Name, Tags, CheckHealthAsync) |
| EncinaHealthCheck | src/Encina.Messaging/Health/EncinaHealthCheck.cs | base class that turns exceptions into Unhealthy results |
| ProviderHealthCheckOptions | src/Encina.Messaging/Health/ProviderHealthCheckOptions.cs | Enabled, Timeout, Name, Tags, FailureStatus of provider checks |
| DatabaseHealthCheck | src/Encina.Messaging/Health/DatabaseHealthCheck.cs | base of the ADO.NET and Dapper provider checks |
| HealthCheckBuilderExtensions | src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs | AddEncinaHealthChecks, AddEncinaHealthCheck<T>, AddEncinaOutbox and siblings |
| CompositeEncinaHealthCheck | src/Encina.AspNetCore/Health/CompositeEncinaHealthCheck.cs | internal aggregation behind AddEncinaHealthChecks |
| EncinaHealthCheckAdapter | src/Encina.AspNetCore/Health/EncinaHealthCheckAdapter.cs | adapts IEncinaHealthCheck to ASP.NET Core IHealthCheck |
| IModuleWithHealthChecks | src/Encina.AspNetCore/Modules/IModuleWithHealthChecks.cs | per-module health checks |
| Existing guide | docs/guides/health-checks.md | page to review and expand (529 lines) |
| Docker guide | docs/infrastructure/docker-infrastructure.md | convention for the Docker Compose section |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| None | None | Documentation only: no `[LoggerMessage]`, so nothing to register in `src/Encina/Diagnostics/EventIdRanges.cs` |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Concept page (docs/features/health-checks.md) | 1 | new |
| Reference page (docs/configuration/health-checks-reference.md) | 1 | new |
| How-to guide (docs/guides/health-checks.md) | 1 | modified |
| Navigation and entry points (README.md, docs/features/index.md, docs/guides/index.md, docs/tutorials/index.md, docs/tutorials/quickstart.md, docs/index.md, docs/introduction.md) | 7 | modified, one link each |
| Total | 11 | 3 pages (2 new, 1 modified), 7 entry points, 1 Messaging README |
| Messaging README tag table (src/Encina.Messaging/README.md) | 1 | modified |
| Changelog fragment | 0 | not applicable |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt — all phases</summary>

```text
PROJECT CONTEXT
Encina is a pre-1.0 .NET 10 / C# 14 library (no backward compatibility, no [Obsolete]). You are a docs-writer working in the git worktree <worktree> (absolute paths only; never touch the main checkout). Rules: AGENTS.md and .claude/skills/encina-docs/SKILL.md. Issue: https://github.com/dlrivada/Encina/issues/907. No production code changes.

IMPLEMENTATION OVERVIEW
1. Create docs/features/health-checks.md (Explanation): why IEncinaHealthCheck exists, the three registration routes (Mermaid), the composite aggregation rule, tags, the probe model, the #1959 limitation.
2. Create docs/configuration/health-checks-reference.md (Reference): registration methods with exact signatures, ProviderHealthCheckOptions, per-package default names and tags read from the code with the date and command, and a "Not covered today" table.
3. Rewrite docs/guides/health-checks.md (How-to): fix AddEncinaHealthChecks(sp) samples, add provider options, tag filtering, custom check, Docker Compose, verify the Kubernetes section.
4. Add one sentence and one link to README.md, src/Encina.Messaging/README.md, docs/features/index.md, docs/guides/index.md, docs/tutorials/index.md, docs/tutorials/quickstart.md, docs/index.md, docs/introduction.md.
5. Verify (lychee, markdownlint-cli2, cov-docs-render check, compile every C# sample in a scratch project), run docs-reviewer, commit locally.

KEY PATTERNS
- AddEncinaHealthChecks(this IHealthChecksBuilder, IEnumerable<string>? tags = null, HealthStatus? failureStatus = null); AddEncinaHealthCheck<T>(string name, IEnumerable<string>? tags = null, HealthStatus? failureStatus = null) where T : class, IEncinaHealthCheck.
- CompositeEncinaHealthCheck is internal sealed: explain it, never instantiate it in a sample.
- Provider checks register as services.AddSingleton<IEncinaHealthCheck, T>() when ProviderHealthCheck.Enabled; defaults come from ProviderHealthCheckOptions.
- Health check results never carry EncinaError.Message or exception messages.
- One Diataxis quadrant per page; pages link sideways; no hand-typed figures; every identifier verified in src/; English only; no emojis.

REFERENCE FILES
docs/plans/health-checks-docs-implementation-plan-907.md, .claude/skills/encina-docs/SKILL.md, src/Encina.Messaging/Health/*.cs, src/Encina.AspNetCore/Health/*.cs, docs/guides/health-checks.md, docs/infrastructure/docker-infrastructure.md, README.md, src/Encina.Messaging/README.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Documentation only, no runtime code; the query cache health check (`AddEncinaQueryCache`) is documented, not changed. |
| 2 | OpenTelemetry | ❌ | Documentation only, no runtime code, no metrics or spans. |
| 3 | Structured Logging | ❌ | Documentation only; no `[LoggerMessage]`, so no EventId range is reserved. |
| 4 | Health Checks | ❌ | This feature documents function 4; no new check is created. Providers without a check are recorded in a follow-up issue (see Next Steps). |
| 5 | Validation | ❌ | Documentation only; the pages receive no input. |
| 6 | Resilience | ❌ | Documentation only; probe timeouts (`ProviderHealthCheckOptions.Timeout`) are documented, not changed. |
| 7 | Distributed Locks | ❌ | Documentation only; the lock providers' health checks are listed in the reference page. |
| 8 | Transactions | ❌ | Documentation only, no state change. |
| 9 | Idempotency | ❌ | Documentation only, nothing can receive a duplicate. |
| 10 | Multi-Tenancy | ❌ | Documentation only; `AddEncinaTenancy` is listed in the reference page. |
| 11 | Module Isolation | ❌ | Documentation only; `AddEncinaModuleHealthChecks` and `IModuleWithHealthChecks` are documented. |
| 12 | Audit Trail | ❌ | Documentation only; the audit store health checks are listed in the reference page. |

---

## Prerequisites & Dependencies

- Bug [#1959](https://github.com/dlrivada/Encina/issues/1959) (tags): not a hard prerequisite; see Design Choice 2 for how the pages treat it.
- The documentation milestone `v0.21.0 — Documentation` (#907 belongs to it); no code issue blocks the plan.
- Follow-up issue for the providers with no health check (file `artifacts/issues/health-checks-missing-providers.md`, a local artifact): the orchestrator opens it and its number replaces `#<number to be filled>` here before the brief is written; Phase 2 step 5 (the "Not covered today" table) links it and is blocked until then. Issue: #<number to be filled>.
- Tools for the verification step: `lychee`, `npx markdownlint-cli2`, the .NET 10 SDK for the scratch compile.

---

## Next Steps

1. The orchestrator links this plan from #907 and asks the maintainer about Design Choice 2 when work on the issue starts.
2. Brief a `docs-writer` for the three pages and the eight edits (11 files, about 1,000 lines). If the guide rewrite grows past the 1,500-line limit, split the pull request in two: (a) the three pages, (b) the entry points.
3. A follow-up issue file is written with this plan: `artifacts/issues/health-checks-missing-providers.md`, `[DEBT]` for the packages with no `IEncinaHealthCheck` (the caching providers Memory, Hybrid, Valkey, Dragonfly, Garnet and KeyDB, the transports Redis.PubSub, InMemory and GraphQL, and the in-memory lock provider). Open it before the reference page's "Not covered today" table is written, so the table can link it.
