## Scope
Issue #75 (created 2025-12-24T13:35:55Z, closed 2026-09-21T21:24:52Z by comment, no PR, no closing commit; timeline `closed` event has no commit_id). Surface:
- `.github/workflows/sonarcloud.yml` (exists today; triggers push to main, pull_request opened/synchronize/reopened, workflow_dispatch; job `if` skips `dependabot[bot]` and fork PRs; uses `secrets.SONAR_TOKEN`; `sonar.issue.ignore.multicriteria` e1-e46). History (`git log --follow`): created before April 2026, Dependabot/fork skip added 2026-04-13 (30ee1b5fd, 5ac2b5f82), last touched b3cc8a925 (2026-11-10, Codecov retirement).
- Repository secret `SONAR_TOKEN`: `gh api .../actions/secrets` lists it, created 2025-12-22T13:15:06Z (before the issue was opened).
- README.md SonarCloud badges (project `dlrivada_Encina`), lines 5 and 27-33.
- No `src/` or `tests/` scope. The pre-draft's "packages: []" is correct; "All packages" in the issue is a template statement, not shipped code.

## Destinations
- Workflow + secret decision: present in `.github/workflows/sonarcloud.yml` (verified by reading triggers and token usage).
- Findings handling: lives in #76, OPEN ("[DEBT] Address SonarCloud findings"); recorded as `backlog planned`.
- Stale claim found: `docs/engineering/PHASE0-BASELINE.md:112` still says `sonarcloud.yml` is "skipped on PRs: SONAR_TOKEN not configured (#75)". The secret exists since 2025-12-22, so this row is inaccurate or a snapshot (Phase 0 diagnostic dated 2026-09-21). Left for the docs stage to judge.
- Pre-draft "Rules and lessons" (SonarCloud as Phase 4 prerequisite) came from the issue body's description, not a decision; dropped. No ADR/AGENTS destination exists or is needed.

## Successor and duplicate issues
- No successor or duplicate. Related: #76 OPEN (findings backlog, verified with `gh issue view`), #1088 CLOSED (build/NuGet audit break that made the quality gate stale), #1090 CLOSED (README/ROADMAP claims incl. "0 Sonar issues"; cross-referenced 2026-09-21).

## Lessons for the pipeline
- Pre-draft had `closed_at: 09/21/2026` and `linked_prs: []` in non-schema form; normalised.
- The issue's premise ("SONAR_TOKEN is not configured") was already false when written: the secret is dated 2 days before the issue. Check secret creation dates via `gh api repos/.../actions/secrets` for configuration issues.
- A doc (PHASE0-BASELINE) cites the closed issue as a live blocker; grep docs for `#<n>` of closed config issues.
- The worktree lacks the pre-draft; it was read from the main checkout (same as earlier lessons).
