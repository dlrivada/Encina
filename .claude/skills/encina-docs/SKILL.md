---
name: encina-docs
description: Write, restructure or review Encina documentation in the house style - one Diátaxis quadrant per page, junior-readable English, evidence-cited figures (covref/mutref), just-the-docs front matter, DocFX for API reference, links to ADRs and SPECs. Use for any task that creates or changes a page under docs/, a package README or a CONTRIBUTING file, and for the documentation milestone issues.
---

# Encina documentation

The audience is a junior .NET developer who has never seen Encina, and a contributor who needs to find the rule that governs a change. Every page is written for exactly one of them, in exactly one Diátaxis quadrant. Read `diataxis.md` in this folder before the first page of a session; it is short.

## 1. Where things live

The site is Jekyll with the just-the-docs theme (`docs/_config.yml`) for narrative pages, plus DocFX (`docs/docfx.json`) for the generated API reference at `/api/`. `.github/workflows/docs.yml` builds both on every PR that touches `docs/` or `src/`, and `link-check.yml` runs lychee offline on every PR.

| Quadrant | Where it goes | Notes |
|---|---|---|
| Tutorial | `docs/tutorials/` | Create the folder with the first tutorial (the quickstart, #81); never create it empty. One linear path, tested from a clean checkout. |
| How-to guide | `docs/guides/` | Title starts with "How to". Indexed from `docs/guides/index.md`. |
| Reference | Generated: `/api/` from XML docs. Hand-written: `docs/configuration/`, option tables and error catalogues inside `docs/features/<feature>.md` | Structure mirrors the package: namespace, type, option. |
| Explanation | `docs/architecture/adr/` (decisions), `docs/architecture/*.md`, `docs/engineering/`, `docs/specifications/` (SPEC-NNN) | ADRs keep their sequence and their `Decision / Context / Consequences / Status / Date` sections. |
| Mixed today | `docs/features/*.md` (85 pages) | Historically each feature page mixes all four. Do not rewrite them wholesale; apply the working method in §4 one page at a time. |

Internal working documents (`docs/plans/`, `docs/reports/`, `docs/engineering/`) carry `nav_exclude: true` in the front matter so they do not appear in the public navigation.

## 2. Front matter and file conventions

Every page under `docs/` starts with just-the-docs front matter:

```yaml
---
title: "How to run the outbox processor against SQL Server"
layout: default
parent: "Guides"        # the index page's title; omit on top-level pages
nav_order: 12           # optional; alphabetical when omitted
---
```

- File names are lower-case kebab-case (`outbox-sql-server.md`); the exceptions in `docs/en/guides/` are contributor documents and stay as they are.
- The first heading repeats the title as `# …`. The next line says in one sentence who the page is for and what they will get.
- Every page links sideways to its neighbours: a how-to links to the reference for the options it uses and to the explanation for the reason; a tutorial links to the how-to that generalises it; reference links to nothing but other reference and, at most, one explanation.
- `.markdownlint.json` applies: MD013 (line length) is off, sibling headings may repeat, list indentation is free. Keep fenced blocks balanced and typed (` ```csharp `, ` ```bash `, ` ```text `).
- Mermaid is available (`mermaid.version: 11` in `_config.yml`); prefer a diagram to a paragraph for any flow with more than three steps.

## 3. House rules (mandatory)

1. **English only** in every file under `docs/` and every README; translate any Spanish you meet while editing. `docs/roadmap-documentacion.md` and `docs/comparacion-nestjs.md` are historical and stay.
2. **No hand-typed figures.** A coverage, mutation or performance number is a citation, never a literal. Coverage, inline: `<!-- covref: cov:Encina.Marten/MartenAggregateRepository.cs:unit -->(generated)<!-- /covref -->`, where the field is `coverage`, `obligations`, one flag name (`unit`, `guard`, `contract`, `property`, `integration`) or another scalar of the DocRef schema; as a table: `<!-- covref-table: cov:Encina.Marten/Projections/* -->` … `<!-- /covref-table -->`. Mutation uses the same shape with `mutref` and `mut:<package>/<path>` ids; benchmarks use the performance dashboard's docref. Conventions: `docs/testing/coverage-measurement-methodology.md` (DocRef convention) and `docs/testing/mutation-measurement-methodology.md`. The `coverage-citations` job of `ci.yml` fails a PR that cites an id or a field that does not exist. Counts that no dashboard measures (number of packages, providers, tests) go in a sentence with the date and the command that produced them, or are left out.
3. **Every code example names real API.** Before writing `services.AddEncinaOutbox(...)`, find the method in `src/` and copy its exact name, parameters and namespace. Prefer copying from an existing test or sample over writing from memory. Examples longer than a few lines belong in a compiling project the page links to, not in the page alone.
4. **Decisions are linked, not restated.** When a page says "Encina does X because Y", link the ADR (`docs/architecture/adr/NNN-…md`) or the SPEC (`docs/specifications/SPEC-NNN-…md`) and keep the restatement to one sentence. If no ADR exists for a decision the page needs, say so in the report; do not invent a rationale.
5. **Provider matrix is complete or explicit.** A page about a provider-dependent feature either covers all providers of that category (`AGENTS.md` §5, Providers) or states which ones it covers and links the issue for the rest.
6. **Names follow the code**: `OutboxMessage`, `InboxMessage`, `SagaState`, `ScheduledMessage`, `…AtUtc` timestamps, `ErrorMessage`, `Either<EncinaError, T>`. Never invent friendlier aliases in prose.
7. **Nothing pre-1.0 is promised as stable.** Say "pre-1.0" where a reader could mistake an API for final; never document migration paths, `[Obsolete]` members or compatibility layers (there are none by policy).
8. **Package READMEs** follow the same rules and are reference plus one short how-to; philosophy goes to the site.
9. **Changelog**: never edit `CHANGELOG.md`; add a fragment in `changelog.d/` (`<issue>-<slug>.<section>.md`) only when the change is user-visible.

## 4. Working method

Diátaxis' cycle, applied to Encina:

1. **Choose** the page from the issue (documentation milestone `v0.21.0 — Documentation`: #80 to #86, #903 to #908, #90, #1032, #661; onboarding: #1102, #1103, #1104, #1107).
2. **Assess** with the compass (`diataxis.md` §2). Write the verdict at the top of your working notes: quadrant, intended reader, what the page must let them do or know. For an existing page, mark each section with its quadrant; every section outside the page's quadrant is either moved, linked or deleted.
3. **Decide** the single shape of the page. A feature that needs a tutorial, a how-to and an explanation becomes three pages that link to each other, not one page with three moods.
4. **Do it.** Write, then verify (§5), then commit.

Never create empty sections, placeholder pages or "coming soon" links. A page ships complete for its quadrant or it does not ship.

## 5. Verification before commit

Run from the repository root of the worktree:

```powershell
# Links and anchors, offline, same as the PR gate (lychee is a standalone binary: winget/scoop/cargo)
lychee --offline --config .github/lychee.toml 'docs/**/*.md' 'README.md'
# Markdown lint with the repository config (Node is present; npx fetches the tool)
npx --yes markdownlint-cli2 "docs/**/*.md" "#docs/INVENTORY.md"
# Coverage citations resolve against this checkout's manifests (the exact command of the coverage-citations CI job)
dotnet run .github/scripts/cov-docs-render.cs -- --check-dangling --docs-root docs --scan-roots src --manifest-dir .github/coverage-manifest --src-root src
```

Neither `lychee` nor `markdownlint-cli2` is installed on the maintainer's machine by default. If a tool is unavailable, say so in the report and check every relative link with `Test-Path` from the page's folder; do not skip the step silently. Mutation citations are checked against `docs/mutations/data/docref-index.json`.

For every code block: confirm each type and member exists in `src/` (Grep for the identifier); for a tutorial, run the whole sequence from a clean clone and paste the actual output into the page where it says "you should see". For a page under `docs/`, a Jekyll build is not required locally; the `docs.yml` workflow builds it on the PR.

## 6. Reviewing a page

The `docs-reviewer` agent applies this checklist; a writer applies it to their own page before reporting.

| Check | Fails when |
|---|---|
| One quadrant | Any section answers a different question than the page's title (compass on each `##`). |
| Reader named | The opening does not say who the page is for and what they get. |
| Real API | A type, member, option or package name in a code block or in prose does not exist in `src/`. |
| Figures cited | A percentage, count or timing appears as a literal instead of a covref/mutref/perf citation, or a citation id does not exist. |
| Decisions linked | A "because" about a design choice has no ADR or SPEC link. |
| Providers | A provider-dependent feature page covers a subset with no statement and no issue. |
| Links and lint | lychee offline or markdownlint reports an error. |
| Language | Any Spanish, or an em-dash-heavy essay tone in a how-to or reference page. |
| Neighbours | The page has no link to at least one adjacent quadrant (tutorial to how-to, how-to to reference, reference or how-to to explanation). |
| Tutorial reliability | A tutorial step's promised output was not reproduced from a clean state. |

Report findings with file and heading, the check that fails and the evidence, ranked blocker (wrong API, wrong facts, hand-typed figures) then major (quadrant leak, missing provider statement) then minor (style, links between neighbours).

## 7. Delegation

- Bulk first drafts of reference tables (option names and defaults read from an options class), inventories and section classifications can go to the local model with the `local-ai-task` skill; the writer verifies every identifier afterwards.
- Already-decided edits (moving a section, fixing front matter across pages) go to `mechanical-fixer`.
- The compass verdict, the page shape and the final check stay with the writer; the independent review stays with `docs-reviewer`.
