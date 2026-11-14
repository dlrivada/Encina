# Implementation Plan: Interactive Documentation Site (Playground, Dark Mode, Versioning)

> **Issue**: [#448](https://github.com/dlrivada/Encina/issues/448)
> **Type**: Feature
> **Complexity**: Low to Medium (6 phases; no `src/` code unless Dilemma D1 is option B)
> **Estimated Scope**: ~150 lines of site configuration and script + ~250 lines of example code and pages + ~60 lines of workflow and monitor changes

---

## Summary

Add to the documentation site the interactive parts it still lacks: runnable examples (a playground), dark mode, copy buttons for code blocks and a versioning policy, on top of the site that already exists. The issue was written before the site; most of its original acceptance items are met today and this plan verifies them instead of rebuilding them.

State of the site (checked on `origin/main`, 2026-11-11):

| Original acceptance item | State |
|--------------------------|-------|
| Documentation site | Exists: Jekyll with `just-the-docs/just-the-docs@v0.10.1` (`docs/_config.yml`) plus DocFX (`docs/docfx.json`, `dest: api`), built and deployed by `.github/workflows/docs.yml` (hybrid architecture of #91) |
| Getting started, concepts, patterns with diagrams | Exist under `docs/` (`introduction.md`, `tutorials/quickstart.md`, `guides/`, `features/`, `messaging/`, `architecture/`); Mermaid is configured in `docs/_includes/mermaid_config.js` |
| Generated API reference | Exists at `/api/` |
| Search | Exists: `search_enabled: true`; Algolia DocSearch is out of scope |
| GitHub Pages deployment | Exists: `docs.yml` is the only Pages deployer (#1381) |
| Playground | Missing |
| Dark mode | Missing: `color_scheme: light`; note `mermaid_config.js` already forces `theme: "dark"` for diagrams, so diagrams and page disagree today |
| Versioning | Missing: one version, `main` |
| Copy buttons | Missing: `docs/_config.yml` has no `enable_copy_code_button` |
| Migration guides (MediatR, MassTransit) | Missing: `docs/migrations/` holds only `compound-shard-keys.md`; MediatR appears only in `introduction.md` and architecture notes. They are not in the acceptance criteria of the rewritten issue: the MediatR guide is tracked by #85 and the MassTransit guide by #3626 (open, "[FEATURE] Documentation: MassTransit migration guide", checked with `gh api`) |

- **Estimated scope**: 14 files (15 if the conditional `ROADMAP.md` edit is needed), about 460 lines (see Research, Estimated File Count); one worker pull request, with a fallback split if the count passes 15.
- **Affected packages**: none of `src/`; `docs/`, `.github/workflows/docs.yml`, `tools/ai/sites.json`. A playground host project is needed only if D1 is option B.
- **Provider category**: none. Static documentation.
- **Prerequisites**: #447 provides the Codespace that option C of D1 links to (the dev container is owned by plan #447); #1381 (single Pages deploy workflow) is already merged.

---

## Design Choices

<details>
<summary><strong>1. How examples become runnable (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Embed .NET Fiddle or a similar hosted runner** | No code or infrastructure of our own; meets "at least a .NET Fiddle embed" fast | Depends on a third-party service and on Encina and LanguageExt being available as packages there (Encina is not on nuget.org before 1.0); embeds third-party scripts in the site |
| **B) A Blazor WebAssembly (Try .NET style) runner hosted in the Pages site** | Full control, offline from the published site, can reference Encina | A new project to build, test and maintain; large download; compiling a LanguageExt-based API in the browser is not proven |
| **C) No in-browser execution: each example is a file-based app (`dotnet run file.cs`) with a copy button and a link to open a Codespace (#447)** | Nothing to host; the example is exactly what the user runs; matches the scripting policy; CI can compile every example | Not "try it now" in the browser |

### Chosen Option: **C — file-based examples with copy button and Codespace link** (recommended, pending the maintainer)

### Rationale

- Option C wins while Encina is not published to nuget.org (the quickstart builds a local feed) and the cost of a hosted runner is not justified before 1.0; B wins when the playground becomes a strategic adoption feature after 1.0; A wins when only a few short examples need to run and a hosted runner can reference the packages.
- If the maintainer picks B, it is a separate plan and pull-request set (host project, build, tests, hosting), because it adds `src/` code that this plan does not size; this plan then keeps only Phases 1, 2, 4 and 5.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. How the documentation is versioned (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No versioning until 1.0; after it, one folder per released minor (`/v1.0/`) built by the same workflow** | No cost while the API changes without compatibility (`AGENTS.md` section 1); no duplicated content | Pre-1.0 readers see only `main` |
| **B) Version from now on, one snapshot per milestone release (`docs/releases/vX.Y.Z/` exists)** | Readers of an older package see matching docs | Duplicated content and longer builds for versions that are not supported |

### Chosen Option: **A — no versioning until 1.0** (recommended, pending the maintainer)

### Rationale

- A wins while there is no released API to preserve; B wins if external users depend on tagged pre-releases.
- Under A this plan delivers the policy page and a deferred issue for the post-1.0 folder layout, not a build change.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Dark mode mechanism</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Theme toggle (light, dark) through the theme's own `jtd.setTheme`, default from `prefers-color-scheme`, stored in local storage; Mermaid follows the selection** | Native to just-the-docs, no new dependency, respects the reader's system setting | A small script in `head_custom.html` and a Mermaid re-render on switch |
| **B) `color_scheme: dark` fixed** | One line | Takes the choice away from readers; light readers lose their setting |
| **C) A third-party theme-switch plugin** | Less own code | A new dependency for a few lines |

### Chosen Option: **A — toggle with system default** (recommended, pending the maintainer)

### Rationale

- A wins for readers who want both schemes without a dependency; B wins for the simplest change (one line, one scheme to test); C wins when the maintainer prefers a maintained plugin over own script. The evidence that `jtd.setTheme` exists in v0.10.1 is the theme documentation, which the implementer has to re-read against that version; until then the mechanism is unverified, which is why this is a Dilemma and not Trivial.
- Unregistered sub-choices, proposals under A: the default scheme is `prefers-color-scheme` and falls back to light; the local-storage key is `encina-docs-color-scheme`; the toggle sits in `aux_links` of the header (a header button is the alternative).
- Mermaid reads the active scheme at initialization only. A runtime switch therefore reloads the page (the simple proposal); re-rendering every diagram in place is the alternative and needs the diagram source kept in the page.
- The Mermaid mismatch found above (diagrams are always dark) is fixed by the same reading of the scheme.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. How the examples are kept correct</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Each example is a file-based app under `docs/playground/examples/`; `docs.yml` compiles every file in a step** | An example that no longer compiles fails the documentation build; reuses the scripting policy | Adds a short step to the documentation job |
| **B) Examples are only code fences in Markdown** | Nothing to build | They rot silently |
| **C) A separate test project that includes the examples** | Standard test tooling | Needs a project and a place in `Encina.slnx` for what are scripts |

### Chosen Option: **A — compile every example in `docs.yml`** (recommended, pending the maintainer)

### Rationale

- A wins when examples must not rot and the cost is one build step; B wins for the least machinery (no build step, no scripts to maintain); C wins when the examples should run under the standard test tooling and count as tests. None of `AGENTS.md` section 2 (scripting form) or section 10 (per-job `permissions:`) mandates compiling documentation examples in `docs.yml`, so they do not make A the only option.
- The `encina-docs` skill rule that every identifier exists in `src/` is satisfied cheaply by compiling.
- The step declares `permissions:` per job, never at workflow level.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. How the examples reference Encina and how CI compiles them</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `#:project ../../../src/Encina/Encina.csproj` (a relative path into the repository) and `dotnet build <file>.cs` in the job** | Restores with no feed and no network access to a package source; always compiles against the code of the commit | A copied snippet needs its directive changed to a package reference |
| **B) `#:package Encina@<dev version>` and a local feed packed in the job (`dotnet pack src/Encina/Encina.csproj -c Release -o feed`, as the quickstart does) with the feed as the restore source** | The snippet is what the reader runs after following the quickstart | A pack step and a feed configuration in the job; the version changes per commit |

Encina is not on nuget.org (plan Summary, quickstart), so a plain `#:package Encina` cannot restore; this is why the choice exists.

### Chosen Option: **A — `#:project` into `src/` in CI** (recommended, pending the maintainer)

### Rationale

- A wins for the cheapest and most reliable CI step; B wins for fidelity with the quickstart reader. The implementer confirms the command form `dotnet build <file>.cs` against the SDK pinned in `global.json` before writing the step.
- Under A the playground page states the one-line change a reader makes to run an example outside a clone.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Core Design — verify the baseline and fix the page structure

> **Goal**: confirm every item of the state table on the day of implementation and create the playground landing page.

<details>
<summary><strong>Tasks</strong></summary>

1. Re-run the checks of the Summary table (search, DocFX, copy button, migration folder) and record any change in the pull request.
2. Create `docs/playground/index.md` (how-to quadrant, `parent` and `nav_order` front matter) listing the examples and how to run them (per D1).
3. The migration guides are not written here: the MediatR guide is #85 and the MassTransit guide is #3626.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Encina docs are Jekyll (just-the-docs v0.10.1) plus DocFX, deployed only by .github/workflows/docs.yml.
TASK: Verify the current state of search, copy buttons and migration guides, then create docs/playground/index.md as a how-to page.
KEY RULES: follow .claude/skills/encina-docs/SKILL.md (one Diataxis quadrant per page, no hand-typed coverage figures, no emojis); English only; the page names the reference application generically when needed.
REFERENCE FILES: docs/_config.yml, docs/tutorials/quickstart.md, docs/index.md.
```

</details>

### Phase 2: Implementation — dark mode and copy buttons

> **Goal**: a toggle that respects the system setting, readable diagrams in both schemes, and a copy button on every code block.

<details>
<summary><strong>Tasks</strong></summary>

1. `docs/_config.yml`: add `enable_copy_code_button: true`; keep `color_scheme: light` as the server default.
2. `docs/_includes/head_custom.html`: a small script that sets the scheme from local storage or `prefers-color-scheme` and adds a toggle link (`aux_links` or a header button), using `jtd.setTheme`.
3. `docs/_sass/custom/custom.scss`: overrides for the elements the stock dark scheme leaves unreadable (to be found by viewing the built site).
4. `docs/_includes/mermaid_config.js`: read the active scheme (`light` selects the Mermaid `default` theme, `dark` the `dark` theme) instead of the fixed `theme: "dark"`; the scheme is read at initialization, so a toggle reloads the page (decision 3).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: docs/_config.yml sets color_scheme: light; docs/_includes/mermaid_config.js forces the dark Mermaid theme.
TASK: Add the copy button setting, a light/dark toggle with system default and local storage, and a Mermaid theme that follows the scheme.
KEY RULES: no new dependency; do not break the existing layout overrides in head_custom.html; check the built site in both schemes (tables, code, callouts, Mermaid, search box); keep the page weight small.
REFERENCE FILES: docs/_config.yml, docs/_includes/head_custom.html, docs/_includes/mermaid_config.js, docs/_sass/custom/custom.scss.
```

</details>

### Phase 3: Playground per D1 (recommended option C)

> **Goal**: the Getting Started examples are exactly what the reader runs.

<details>
<summary><strong>Tasks</strong></summary>

1. `docs/playground/examples/hello-command.cs`, `docs/playground/examples/error-track.cs`, `docs/playground/examples/pipeline-behavior.cs`: three file-based apps taken from the quickstart (send a command, read the error track, add a pipeline behavior), each starting with the `#:` directives it needs (`#:project` into `src/` under the recommended decision 5).
2. `docs/playground/index.md` shows each example in a code fence with the copy button and the command `dotnet run <file>.cs`, plus a link that opens the repository in a Codespace (the dev container of #447).
3. `docs/tutorials/quickstart.md` links to the playground page.
4. If the maintainer picks A or B, replace this phase as described in the choice 1 rationale.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: Decision D1 is option C (or the recorded answer). Encina is not on nuget.org before 1.0; the quickstart builds a local feed.
TASK: Write three file-based example apps and the playground page that presents them.
KEY RULES: C# 14, .NET 10; every identifier must exist in src/ (read the code); examples return Either<EncinaError, T> and show both tracks; no secrets; comments in English.
REFERENCE FILES: docs/tutorials/quickstart.md, docs/guides/how-to-write-a-pipeline-behavior.md, src/Encina/ (IEncina).
```

</details>

### Phase 4: Versioning policy (recommended option A of D2)

> **Goal**: record the versioning rule and what happens at 1.0.

<details>
<summary><strong>Tasks</strong></summary>

1. A short reference page `docs/` section "Documentation versions" stating: until 1.0 the site is `main`; at 1.0 each released minor gets `/v<major>.<minor>/` built by `docs.yml`.
2. The post-1.0 folder layout and workflow change is a follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/docs-versioned-layout-post-1-0.md); it is not a file of this pull request.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT: D2 is option A (no versioning until 1.0).
TASK: Write the versioning policy page and the deferred issue file for the post-1.0 layout.
KEY RULES: documentation through the docs-writer; issue file follows the feature_request template headers verbatim; do not change docs.yml for versioning in this issue.
REFERENCE FILES: .github/workflows/docs.yml, docs/releases/, .github/ISSUE_TEMPLATE/feature_request.md.
```

</details>

### Phase 5: Cross-Cutting Integration — CI check and site monitor

> **Goal**: the examples compile in CI and the new path is monitored.

<details>
<summary><strong>Tasks</strong></summary>

1. `.github/workflows/docs.yml` (orchestrator-owned file): a step in the build job that runs `dotnet build <file>.cs` on every `docs/playground/examples/*.cs` (file-based apps; each file references `src/` through `#:project`, decision 5), with `permissions:` unchanged at job level. The orchestrator applies this edit.
2. `tools/ai/sites.json`: an entry `playground` with `path: playground/`, `kind: docs`, `expectHtml: true`, `publisher: docs.yml`.
3. The link check of `.github/workflows/link-check.yml` covers the new pages without change; verify on the pull request.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT: docs.yml is the only workflow that deploys Pages. tools/ai/sites.json lists the monitored sites.
TASK: Add the example compile step and the sites.json entry.
KEY RULES: workflow edits belong to the orchestrator (shared hot spot); permissions declared per job; the sites.json entry follows the schema of the existing "docs" entry.
REFERENCE FILES: .github/workflows/docs.yml, tools/ai/sites.json, .claude/skills/site-health/SKILL.md.
```

</details>

### Phase 6: Testing, Documentation & Finalization

> **Goal**: prove the build and finish the metadata.

<details>
<summary><strong>Tasks</strong></summary>

1. Verification: the Jekyll and DocFX build is green; the link check passes; the three examples compile; both schemes render the main page types.
2. Test types, answering the issue criterion "all applicable test types implemented or justified with `.md`": unit, guard, contract and property tests do not apply (no `src/` code); no `.md` justification file is written because no test project folder is touched (a static site has no test project); the equivalent of the integration test is the site build plus the link check plus the example compile (decision 4).
3. `changelog.d/448-docs-site-playground.added.md`, `ROADMAP.md` entry only if the milestone text changes (conditional file).
4. A configuration guide section for the theme options, in the how-to page for contributors named in the file count (through the docs-writer).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT: The site changes are in place.
TASK: Run the documentation build and the link check, add the changelog fragment and the configuration guide section.
KEY RULES: changelog fragments through the mechanical-fixer; documentation pages through the docs-writer; no coverage figures typed by hand; report the exact commands run.
REFERENCE FILES: changelog.d/README.md, .github/workflows/link-check.yml, .claude/skills/encina-docs/SKILL.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance | Use in this feature |
|----------|-----------|---------------------|
| just-the-docs theme (v0.10.1) | Color schemes, `jtd.setTheme`, `enable_copy_code_button`, search | Dark mode and copy buttons |
| Mermaid | Diagram themes (`default`, `dark`) | Follow the selected scheme |
| `AGENTS.md` section 2 | C# 14 file-based apps as the scripting form (ADR-058 is the `Encina.Cli` command architecture and is not cited for this) | Runnable examples |
| #91 and #1381 | Hybrid Jekyll plus DocFX site; single Pages deploy workflow | Framework and deployment stay |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
| :--- | :--- | :--- |
| Jekyll configuration | `docs/_config.yml` | `remote_theme: just-the-docs/just-the-docs@v0.10.1`, `color_scheme: light`, `search_enabled: true`; gets the dark mode and copy button settings |
| Head include | `docs/_includes/head_custom.html` | Layout overrides; gets the theme toggle script |
| Mermaid include | `docs/_includes/mermaid_config.js` | Currently `theme: "dark"` fixed; must follow the selected scheme |
| Custom styles | `docs/_sass/custom/custom.scss` | Place for the dark color scheme overrides |
| Pages deployment | `.github/workflows/docs.yml` | The only workflow that deploys Pages (#1381); builds Jekyll plus DocFX; gets the example compile step |
| Link check | `.github/workflows/link-check.yml` | Checks the internal links of the new pages |
| DocFX | `docs/docfx.json` | API reference published at `/api/`, unchanged |
| Site monitor | `tools/ai/sites.json` | Gets an entry for the playground path (site-health skill) |
| Quickstart | `docs/tutorials/quickstart.md` | The Getting Started example that becomes runnable |
| Documentation skill | `.claude/skills/encina-docs/SKILL.md` | Style rules for the new pages |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| none | none | No `[LoggerMessage]` code; no EventId range is needed. If D1 were option B, the host project would get its own range from the orchestrator |

### Estimated File Count

| Category | Files | Notes |
| :--- | :--- | :--- |
| Theme configuration | 4 | `docs/_config.yml`, `docs/_includes/head_custom.html`, `docs/_includes/mermaid_config.js`, `docs/_sass/custom/custom.scss` (Phase 2 tasks 1 to 4) |
| Playground content | 4 | `docs/playground/index.md` and three example files (file-based apps) |
| Workflow and monitor | 2 | `.github/workflows/docs.yml` compile step, `tools/ai/sites.json` entry |
| Documentation | 3 | Versioning policy page (new), the how-to page for contributors that receives the theme-options section of Phase 6 task 4 (Phase 6 "configuration guide section"; an edit of an existing page), the link in `docs/tutorials/quickstart.md` (edit) |
| Metadata | 1 | `changelog.d/448-docs-site-playground.added.md` |
| **Total** | 14 | One worker pull request (limit 15 files, 1500 lines) |
| Conditional | +1 | `ROADMAP.md`, only if the milestone text changes (Phase 6 task 3); the total is then 15, the limit |

If the count passes 15 (for example the implementer finds one more page to edit), the pull request is split in two: pull request 1 is the theme configuration (4), the contributor how-to section (1) and the changelog fragment (1); pull request 2 is the playground content (4), the versioning page (1), the quickstart link (1), the workflow and monitor (2) and the ROADMAP edit if needed. Pull request 2 uses the toggle of pull request 1 only through the site, so no file of one uses a file created by the other.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing the documentation site additions of Issue #448.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; the site is Jekyll (just-the-docs v0.10.1) plus DocFX, deployed only by .github/workflows/docs.yml.
- Pre-1.0: best solution, no compatibility layers.

IMPLEMENTATION OVERVIEW:
1. Verify the baseline and create docs/playground/index.md.
2. Dark mode toggle (system default), copy buttons, Mermaid following the scheme.
3. Three file-based example apps and the playground page (decision D1, recommended option C).
4. Versioning policy page (decision D2, recommended option A) and a deferred issue file.
5. docs.yml compile step (orchestrator applies), sites.json entry.
6. Build, link check, changelog fragment, configuration guide.

KEY PATTERNS:
- One Diataxis quadrant per page; every identifier in an example exists in src/; no hand-typed coverage figures.
- Workflow permissions per job; shared hot spots (.github/workflows/*) stay with the orchestrator.

REFERENCE FILES:
- docs/_config.yml, docs/_includes/head_custom.html, docs/_includes/mermaid_config.js, docs/_sass/custom/custom.scss
- docs/tutorials/quickstart.md, .github/workflows/docs.yml, tools/ai/sites.json, .claude/skills/encina-docs/SKILL.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Static site |
| 2 | OpenTelemetry | ❌ | Static site; no runtime |
| 3 | Structured Logging | ❌ | Documentation platform |
| 4 | Health Checks | ✅ | The playground path is added to `tools/ai/sites.json` so the site-health check covers it (Phase 5) |
| 5 | Validation | ✅ | The link check runs on the new pages and every example compiles in `docs.yml` (Phase 5) |
| 6 | Resilience | ❌ | No external call at run time; if D1 were option A, an outage of the third-party runner would be a documented degradation |
| 7 | Distributed Locks | ❌ | Static content |
| 8 | Transactions | ❌ | Static content |
| 9 | Idempotency | ❌ | Static content |
| 10 | Multi-Tenancy | ❌ | Public documentation |
| 11 | Module Isolation | ❌ | Documentation organization |
| 12 | Audit Trail | ❌ | Public documentation; no audit-relevant data. Deferred items outside this matrix: the migration guides (MediatR is #85, MassTransit is #3626) and the post-1.0 versioned layout, a follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/docs-versioned-layout-post-1-0.md) |

---

## Next Steps

1. Review and approve this plan.
2. The maintainer answers the Dilemmas (decisions 1, 2, 3, 4 and 5) when work on the issue starts; the answers go into a new `## Maintainer Decisions` section added as the last section of this plan at that time (it does not exist yet).
3. Implement Phases 1 to 6 as one worker pull request (14 files, 15 with the conditional ROADMAP edit, about 460 lines); the `docs.yml` edit is applied by the orchestrator.
4. The final commit references `Fixes #448`.
