# Delta scope of issue #19 (set rules-2026-10)

Reused from the original audit; do not re-derive it. Rules in this delta: see tools/ai/audit/pipeline-delta.json.

**Scope source:** the published record and the published stage files of the original audit (docs/knowledge/audits/19/stages/).

## Knowledge record (docs/knowledge/issues/19.md)

```yaml
schema: 1
nav_exclude: true
issue: 19
title: "[DEBT] Increase code coverage threshold to 85%"
closed: 2026-09-24
state_reason: not-planned
outcome: rejected-reasoned
type: debt
area: testing-quality
review: verified
packages:
prs:
linked_prs:
  - 1300
knowledge:
  - kind: rejected-alternative
    statement: "A single project-wide line coverage threshold (raise the CI gate from 45% to 85%, with 80% per package and 90% for Core and Messaging) was rejected: it belongs to the old single-percentage coverage model."
    current: yes
    sources:
      - "quote: \"Closing: this target comes from the old single-percentage coverage model.\" (issue #19, comment by dlrivada, 2026-09-24)"
    destinations:
      - kind: quality-method
        status: done
        target: "docs/testing/coverage-measurement-methodology.md"
  - kind: decision
    statement: "Coverage is measured per flag (unit, guard, contract, property, integration) against per-package targets in .github/coverage-manifest/, computed by .github/scripts/coverage-report.cs and shown on the coverage dashboard; there is no project-wide percentage."
    current: yes
    sources:
      - "quote: \"Encina now measures coverage per flag (unit, guard, contract, property, integration) against the per-package targets in .github/coverage-manifest/, computed by .github/scripts/coverage-report.cs and shown on the coverage dashboard.\" (issue #19, comment by dlrivada, 2026-09-24)"
    destinations:
      - kind: rule
        status: done
        target: "AGENTS.md"
      - kind: quality-method
        status: done
        target: "docs/testing/coverage-measurement-methodology.md"
  - kind: decision
    statement: "Mutation score is tracked per file with no project-wide target (the 95% mutation target of #67 is not adopted)."
    current: yes
    sources:
      - "quote: \"Mutation score is tracked per file with no project-wide target (docs/testing/mutation-measurement-methodology.md).\" (issue #19, comment by dlrivada, 2026-09-24)"
    destinations:
      - kind: quality-method
        status: done
        target: "docs/testing/mutation-measurement-methodology.md"
  - kind: decision
    statement: "Coverage gaps against the per-package targets are tracked as one [TEST] issue per package, not as a broad technical-debt threshold issue."
    current: yes
    sources:
      - "quote: \"Gaps against those targets are tracked as [TEST] issues per package.\" (issue #19, comment by dlrivada, 2026-09-24)"
    destinations:
      - kind: backlog
        status: planned
        target: "https://github.com/dlrivada/Encina/issues/1627"
  - kind: rule
    statement: "Documentation never hand-types coverage or inventory figures (including an 85% coverage target); it links the dashboards or cites SPEC-001 markers."
    current: yes
    sources:
      - "paraphrase: PR #1300 removed the hand-typed '85%+' coverage target, package and test counts from README.md, ROADMAP.md and two architecture pages (PR #1300, 2026-09-24)"
    destinations:
      - kind: rule
        status: done
        target: "AGENTS.md"
audit:
  checklist: 1
  date: 2026-10-02
  verdict: not-audited
  record: "docs/knowledge/audits/issue-19.md"
remediation:
```

## From the original archivist.md (docs/knowledge/audits/19/stages/archivist.md)

The issue touched one file: `.github/workflows/dotnet-ci.yml` (threshold 45% to 85%). That file no longer exists. Commit 59a5a867 ("fix(ci): consolidate CI workflows") replaced it with `.github/workflows/ci.yml` and `ci-full.yml`. A search of `ci.yml` and `ci-full.yml` finds no 45 or 85 coverage threshold; `ci-full.yml` runs `coverage-report.cs` and fails only when it produces no index or every entry is `noData`; `ci.yml` has the blocking `crap-gate` job (AGENTS.md section 9).

Current homes of the replacement model, all verified present in the worktree:
- `.github/coverage-manifest/*.json` (`defaults.json` plus one per package; e.g. `Encina.json` has `targets` contract 15, guard 20, unit 70).
- `.github/scripts/coverage-report.cs` (last touched by 365a7ace).
- `docs/testing/coverage-measurement-methodology.md` and `docs/testing/mutation-measurement-methodology.md` (the latter describes per-file data carried across runs).
- ADR-023 `docs/architecture/adr/023-coverage-strategy-codecov-sonarcloud.md`.
- PR #1300 (merged 2026-09-24) changed README.md, ROADMAP.md, `docs/architecture/component-diagram.md`, `docs/architecture/patterns-guide.md`. A search of README, ROADMAP, CLAUDE.md and AGENTS.md for "85" finds no coverage target.

Code scope for the auditor: none under `src/`. The issue is CI configuration and a coverage target, so there is no code to audit. The pre-draft's open question about CLAUDE.md still saying "85% line coverage" is answered: it does not. The pre-draft's `packages: [unknown]` is corrected to empty.

## From the original code.md (docs/knowledge/audits/19/stages/code.md)

The archivist scope list names no `src/` code: issue #19 (single 85% line-coverage threshold, closed not-planned) was replaced by the per-flag model. Scope was corrected to today's implementation of that replacement and its leftovers, reviewed as if it were this issue's pull request: `.github/scripts/coverage-report.cs` (read in full), `.github/workflows/ci.yml` (test jobs, `crap-gate`, `coverage-citations`, `ci-result` needs), `.github/workflows/ci-full.yml` (`coverage` job), `.github/workflows/publish-coverage.yml`, `codecov.yml`, `.github/coverage-manifest/*.json` (106 package manifests: every one has `targets`, none has a unit/guard/contract/integration target of 85), `.github/scripts/coverage-weights.json`, `check-sonarcloud-coverage.cs`, `coverage-recalculate.cs` (header), `crap-gate.cs` (no-data handling), `.github/workflows/templates/encina-test.yml` and `encina-full-ci.yml`, `.github/ISSUE_TEMPLATE/*.md`, `.github/pull_request_template.md`, `docs/coverage/app.js`, `docs/architecture/adr/023-coverage-strategy-codecov-sonarcloud.md`, `docs/testing/coverage-measurement-methodology.md` (for what the code is claimed to do), `.opencode/skills/{test-workflow,release-checklist}/SKILL.md`, `.opencode/agents/encina-test.md`, and the open issues #901, #910, #521, #1627 (via `gh issue view`/`gh issue list`, read only). Test files and prose docs were not reviewed (test-auditor and docs-reviewer stages); where a leftover lives in a test or doc it is cited only as evidence.


