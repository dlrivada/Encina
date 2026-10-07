## Pages reviewed

Delta `rules-2026-10`, rule (a) only. Scope from `artifacts/knowledge/delta-scope.md` (issue #28 delivered no page; its destination is `.github/workflows/ci.yml`). Pages that describe what #28 asked about (ContractTests and PropertyTests excluded from SonarCloud, then run in CI and SonarCloud kept as static analysis):

- `docs/architecture/adr/023-coverage-strategy-codecov-sonarcloud.md` (124 lines)
- `docs/plans/testing-dogfooding-plan.md` (2521 lines)
- `docs/contributing/README.md` (267 lines; :212 and :216)
- `docs/engineering/crap-gate-design.md` (66 lines)
- `docs/engineering/REPLICATION-GUIDE.md` (260 lines; no finding)

Counts, each by a command over the page: Mermaid blocks `Select-String '^\s*```mermaid'`; emoji lines by Unicode range (U+2600-U+27BF, U+23F3, U+2B50, surrogate pairs U+1F300-U+1FAFF); front matter by first line.

| Page | Mermaid | Emoji lines | First line | Table lines |
| --- | --- | --- | --- | --- |
| ADR-023 | 0 | 0 | front matter | 16 |
| testing-dogfooding-plan | 1 (:2099) | 32 | `#` heading | n/a |
| contributing/README | 0 | 0 | `#` heading | 65 |
| crap-gate-design | 0 | 0 | front matter (`nav_exclude`) | 0 |
| REPLICATION-GUIDE | 0 | 0 | `#` heading | n/a |

## Findings

1. **Minor** — `docs/architecture/adr/023-coverage-strategy-codecov-sonarcloud.md`, "Decision" (:33-64) and "Consequences" (:85-106): rule (a) point 1. The ADR decides a flow (CI runs the five test types and uploads each flag to Codecov, SonarCloud only builds and analyses) in five numbered prose items and has 0 Mermaid, UML or C4 blocks (0 fenced blocks of any kind). The only visual is the 14-row category table (:68-83), which is itself the subject of open #1665. One flowchart of "test job -> Codecov flag" and "build -> SonarCloud" would show what #28 asked about (which workflow runs ContractTests/PropertyTests, which one does not). Neighbouring open issues cover the content (#1665 supersede the ADR, #1667 figures), not the missing diagram; if #1665 supersedes the ADR, put the diagram in the new ADR.

2. **Minor** — `docs/architecture/adr/023-coverage-strategy-codecov-sonarcloud.md:18` ("core business logic can achieve 85%+") and `:105` ("SonarCloud dashboard shows 0% coverage"): rule (a) point 3, figures typed by hand with no `covref` and no date or command. Search `Select-String '\d+(\.\d+)?\s?%'` returns lines 18, 20, 28, 29, 62, 70-83, 90, 100, 105; lines 28-29 are third-party Codecov settings, not Encina figures. Open #1665 names :66-83, :90 and :100 and open #1667 names :16, :20, :22, :49 and :91; neither names :18 or :105, so these two are the residue. Extend #1667 with the two lines, or fix them in the superseding ADR of #1665.

3. **Minor** — `docs/contributing/README.md:216` ("What CI runs where"): rule (a) point 1. One 683-character paragraph lists five triggers (pull request, push to `main`, daily, weekly, docs-only) and which jobs each runs, including the unit/guard/contract/property/integration shards that #28 was about; `:212` adds the required checks (`ci-result`, CodeQL) in another dense paragraph. The page has tables elsewhere (65 table lines) but none for this content. A table "trigger / workflow / jobs / blocks merge" and, optionally, a Mermaid flow would make it scannable. No open issue names `docs/contributing/README.md:212` or `:216` (searched open issue bodies for the path: #1103, #1102, #1373, #1847 mention the file, none this paragraph; #1847 covers the page's missing front matter, which is why that is not repeated here).

4. **Minor** — `docs/plans/testing-dogfooding-plan.md` (2521 lines): rule (a) point 1, no emojis. 32 lines carry a pictograph: :72-75, :141, :146, :888, :904, :2140, :2142, :2143 and :2293-:2313 (the status and checklist lists, for example check-mark and cross marks). The page has 1 Mermaid block (:2099) in 2521 lines and 132 fenced lines. It carries a Historical Note at :7, which does not excuse the style in a page that is still linked from the plans. Open #1723 covers only the SonarCloud sentence at :132-134 (verified: its body names `:132-134` only), not the emojis; no open issue mentions emojis in this plan (searched `gh issue list --search "dogfooding-plan emoji"`: no result).

5. **Minor** — `docs/engineering/crap-gate-design.md:32`, `:34`, `:36`, `:42`, `:48`, `:56`: rule (a) point 1. The note compares three CI wiring options and describes the job's dependencies and guards, entirely in paragraphs of 579, 784, 1232, 700, 919 and 800 characters (measured per physical line), with 0 tables and 0 diagrams. The `needs:` list of the six flag jobs (:34, :58), the `paths-filter` behaviour (:32, :36) and the three-option tradeoff (:19-48) are structure a table and a small flowchart would show. Open #1360 concerns CRAP data attribution in `coverage-report.cs`, not this page.

## Informational (not findings)

- Point 2 (C# samples): none of the five pages carries a `csharp` block for this issue's subject; `contributing/README.md` has 8 fence lines (4 blocks) and `REPLICATION-GUIDE.md` has 10 fence lines, none C# about the CI subject. Nothing to compare against `src/`.
- Point 3 beyond finding 2: `contributing/README.md:211` says Codecov patch coverage "target 60%"; this is the same literal the previous stage left unverified and is not about #28. `crap-gate-design.md` and `REPLICATION-GUIDE.md` have no `%` literals.
- Point 4 (placement): ADR-023 is linked from `docs/architecture/adr/index.md:35` and has front matter (`parent: ADRs`, `grand_parent: Architecture`). `crap-gate-design.md` is `nav_exclude: true`, a design note by intent. `contributing/README.md` and `testing-dogfooding-plan.md` start with a `#` heading and no front matter; the first is tracked in #1847, the second sits under `docs/plans/` (not nav). Not re-reported.
- Point 5 (feature adequacy): #28 delivered no feature (a CI test-gating change, closed in error and duplicate of #29), so no concept page, reference or tutorial entry is owed. The surrounding CI behaviour is described in `docs/contributing/README.md:212-216` and `docs/engineering/crap-gate-design.md:25-26` (names `test-contract` and `test-property`); finding 3 concerns its form only. `docs/tutorials` and `docs/index.md` have no CI-gating content to update.
- The content defect already found (`testing-dogfooding-plan.md:132-134` says SonarCloud excludes the Encina.Testing packages) is open as #1723 and is not repeated.
- Search of `artifacts/` was not needed; every cited line was read from the page in this worktree. The page counts above exclude this stage's own artifact.
- Not run in delta mode: lychee, markdownlint, the coverage-citation command (rule (a) only).

## Lessons for the pipeline

- For a CI-configuration issue closed in error, the docs stage finds its rule (a) work in the pages that describe the CI flow (ADR, contributing page, design note), not in a feature page; run the emoji, Mermaid and long-paragraph measurements per page and map the content defects to the open issues by file and line before writing (#1665, #1667, #1723 here), so only the residue lines (:18, :105) and the visual gaps are new.
