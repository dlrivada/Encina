# Docs stage: delta rules-2026-10 (rule (a) only), issue #23

## Pages reviewed

Issue #23 was closed in error; the feature (`Encina.Testing`: `EncinaFixture`, `Either` assertions, `AggregateTestBase`) was delivered by #44. Rule (a) point 5 is run on the delivered feature. Every path below was checked with `Test-Path` or `Get-ChildItem`.

| Page | Result |
| --- | --- |
| `src/Encina.Testing/README.md` | does not exist (`Test-Path` False); `src/Encina.Testing/` has 32 `.cs` files and no `.md`. Tracked by #1689 |
| `docs/en/guides/TESTING.md` (112 physical lines) | exists; 0 hits for `Encina.Testing`; 0 Mermaid blocks, 1 table, 5 `pwsh` fenced blocks (lines 21, 27, 72, 78, 94), 0 emoji lines (Unicode ranges U+2600-U+27BF, U+23F3, U+2B50, surrogates); first line is `# Testing Encina`, so no front matter |
| `docs/tutorials/index.md` (12 lines), `docs/tutorials/quickstart.md` | `Select-String -Pattern test` over `docs/tutorials/*.md`, `docs/index.md`, `docs/introduction.md`: no tutorial hit; the only index entry is the quickstart (line 12) |
| `docs/index.md:71`, `docs/features/index.md` | `docs/index.md:71` is a table row ("Testing", 12 packages); `docs/features/index.md` has 0 hits for `Encina.Testing` or `TESTING.md`; `docs/features/` has no testing page |
| `docs/en/guides/MUTATION_TESTING.md:110-117` | `Encina.Testing.Mutations` is a real namespace (`src/Encina.Testing/Mutations/MutationKillerAttribute.cs`, `NeedsMutationCoverageAttribute.cs`); OK |

Search used for "no page documents the feature": `Get-ChildItem docs -Recurse -Filter *.md` excluding `knowledge|history|plans|releases`, pattern `Encina\.Testing\b|ShouldBeSuccess|EncinaFixture|AggregateTestBase`. Hits are only `docs/INVENTORY.md` (Spanish status tables and history), `docs/engineering/*` (handbook table, assessment), `docs/ci-cd-templates.md:331` and `docs/_api-landing/index.md:28` (one-line package lists), `docs/contributing/README.md:78,98`, `docs/comparacion-nestjs.md` and package lists. None teaches a reader to use the package.

## Findings

1. **Major** — Rule (a) point 5, no concept or guide page and no reference for `Encina.Testing`. The package a user installs to test handlers, sagas, aggregates, messaging and modules has no page under `docs/`: `docs/features/` has no testing page (the only `*test*` name there is `audit-attestation.md`); `docs/en/guides/TESTING.md` is about running Encina's own suites and has 0 hits for `Encina.Testing`, `EncinaFixture`, `ShouldBeSuccess` or `AggregateTestBase`; `docs/index.md:71` only lists the package family in a table row. The package README is already tracked by #1689 (it names `EncinaFixture`, `AggregateTestBase` and the rest), but #1689 covers `src/Encina.Testing/README.md` only, not a docs-site concept or guide page. Extend #1689 to add the docs-site page (explanation plus reference), placed under `docs/features/` or `docs/guides/` with front matter and linked from `docs/features/index.md`.
2. **Major** — Rule (a) point 5, the feature appears in no tutorial or learning path. `docs/tutorials/index.md:12` lists only the quickstart; `docs/tutorials/` holds `index.md` and `quickstart.md`; no tutorial shows how to test a handler with `EncinaFixture` and `ShouldBeSuccess`. #1864 asks for a tutorial on a provider integration test with `Encina.TestInfrastructure` (files `docs/tutorials/index.md`, `quickstart.md`, `docs/index.md`), which is a different feature (the repository's own test infrastructure, not the shipped `Encina.Testing` package). Extend #1864 with a "test your first handler with `Encina.Testing`" tutorial, or open a sibling issue; do not treat it as covered.
3. **Minor** — Rule (a) point 4 and point 1, `docs/en/guides/TESTING.md` placement and shape. It is the only guide-level testing page and it has no front matter (first line `# Testing Encina`), is not linked from `docs/index.md`, `docs/features/index.md` or `docs/introduction.md` (0 hits for `TESTING` in each); inbound links come only from `docs/en/guides/REQUIREMENTS.md:34` and engineering pages. It has 0 Mermaid blocks (a layer-to-project diagram would show the structure of the seven suites) and one table. Open issues #1663, #1666 and #1711 cover its coverage and mutation content; none names front matter, navigation or the missing user-facing testing section (bodies searched for `front matter`, `diagram`, `Encina.Testing`: 0 hits). Extend #1663 or fold into the page created for finding 1.

## Informational (not findings)

- `docs/comparacion-nestjs.md` (2475 lines, `nav_exclude: true`, no inbound links) lists "EncinaFixture Builder" as planned (lines 2180-2203, 2395, 2460). `EncinaFixture` exists (`src/Encina.Testing/EncinaFixture.cs:42`) but has no `Create` or `WithMockedHandler` member (0 hits in `src/Encina.Testing/*.cs`), so the "fluent builder not implemented" statement is accurate. The page is Spanish and has 494 emoji lines; Oracle and SQLite on it are tracked by #1882. Not scoped to this feature.
- `docs/index.md:18` states "112 packages", "13,000+ tests" and "13 database providers" as typed literals (rule (a) point 3), site-wide and not specific to this feature.
- `docs/INVENTORY.md:3102-3112,3155` document the package in Spanish with emoji status marks; not reviewed further.
- Open umbrellas #1847 #1856 #1864 #1850 #1894 #1896 #1901 were read for `Encina.Testing`, tutorial and learning-path text; #1866 #1877 #1882 #1886 #1887 #1897 #1908 #1915 matched `Encina.Testing|EncinaFixture` 0 times except #1882 (once, the Oracle/SQLite item).

## Lessons for the pipeline

- For a closed-in-error issue whose feature has an open README issue from the original audit (#1689), the delta docs stage still adds value by separating the README from the docs-site page and the tutorial entry, and by saying which open issue to extend.
- A guide named after the feature (`TESTING.md`) can describe the repository's own test suites and not the shipped package; check the page for the package name before counting it as the feature's page.
