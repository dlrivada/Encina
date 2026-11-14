## Pages reviewed

- `ROADMAP.md` (:34, :42, :159, :203, :752)
- `docs/engineering/PHASE0-BASELINE.md` (:4, :15, :73, :94, :112, :148, :201)
- `.github/workflows/sonarcloud.yml` (triggers and `if:` condition, :3-8 and :25-28)
- Repo secret and run history, checked with `gh secret list` and `gh run list`.

## Findings

1. **Major** — `ROADMAP.md:42`, the SonarCloud Issues row of the quality-metrics table, says "scans are currently skipped on PRs, see #75". The check that fails is accuracy against today's state. `gh api repos/dlrivada/Encina/actions/secrets` shows `SONAR_TOKEN` with created_at 2025-12-22T13:15:06Z and updated_at 2026-09-22T09:21:28Z (the secret was replaced on 2026-09-22; `gh secret list` shows only the updated date); issue #75 is CLOSED; `.github/workflows/sonarcloud.yml:3-8` runs on push to main, pull_request and workflow_dispatch, and `:25-28` skips only Dependabot and fork PRs; `gh run list` and `gh api .../workflows/sonarcloud.yml/runs` show the SonarCloud Analysis workflow failing on same-repo PRs until 2026-09-22T09:13:51Z and succeeding from 2026-09-22T09:20:35Z, and running on current same-repo PRs and successful push runs on main (2026-11-12 12:28Z, 2026-11-11 22:23Z and 22:04Z). The parenthetical is false and points readers to a closed issue. Fix: keep only the dashboard link, or state that scans run on PRs and on main.
2. **Minor** — `ROADMAP.md:159`, "Milestone Completed: 58 issues closed, 0 warnings, 0 SonarCloud issues", is a hand-typed quality figure with no date or command. The check that fails is "figures cited, never hand-typed". It is a historical milestone line, and it is the same unbacked claim that `docs/engineering/PHASE0-BASELINE.md:15` already records (finding #1090). Cite it or remove it.

## Informational (not findings)

- `docs/engineering/PHASE0-BASELINE.md:112` ("skipped on PRs", `SONAR_TOKEN` not configured (#75)), `:148` ("SonarCloud has never run (#75)") and `:201` (F-06) are not findings, because the page is a frozen snapshot (banner at `:4`: "historical, pre-remediation snapshot ... not updated to the current state", dated 2026-09-21); do not edit these lines. Re-checked with `gh api` (REST), the snapshot's wording is partly unsupported at its own date:
  - Secret: `SONAR_TOKEN` was not missing. It existed since 2025-12-22T13:15:06Z and was updated 2026-09-22T09:21:28Z, after the snapshot, so "`SONAR_TOKEN` not configured" (`:112`) is imprecise for 2026-09-21.
  - Failure: the run that shows the failure is 35709195162 (pull_request, 2026-09-22T09:13:51Z, conclusion failure). Its job `SonarCloud Analysis` has the step "Begin SonarCloud analysis" failing, with "Build for SonarCloud analysis" and "End SonarCloud analysis" skipped, and the job log contains `WARNING: Authentication with the server has failed.` (09:15:17Z). Same-repo PR runs failed until 09:13:51Z and succeeded from 09:20:35Z, after the secret was replaced.
  - Run 25069093865 (2026-04-28) is not evidence: its only job `SonarCloud Analysis` has conclusion skipped (a Dependabot run excluded by the `if:` condition, no steps), so it is not cited.
  - "Has never run" (`:148`) is not supported. The workflow has analysis runs that completed successfully since April 2026: in `gh api "repos/dlrivada/Encina/actions/workflows/sonarcloud.yml/runs?per_page=100&created=2026-04-01..2026-04-30"` (596 runs in the range, newest 100 returned) 44 have conclusion success, 48 cancelled, 7 skipped, 1 failure; for example push on main run 25068185520 (2026-04-28T17:35:10Z) has its job conclusion success. The logs of those April runs have expired, so what they uploaded to SonarCloud cannot be shown; only that the workflow completed.
  - This is the snapshot's own wording at its date, not today's state.
- `ROADMAP.md:34` correctly says there is no aggregate Sonar-issues dashboard figure. It is not stale.
- #75 changed no page. I reviewed only the pages that state its outcome. Placement and Diataxis were not reviewed.
- lychee and markdownlint were not run, because only a one-cell text fix is proposed.

## Lessons for the pipeline

- When a page says a CI job is skipped or never ran, check the workflow `if:` condition, `gh secret list` and `gh run list` for that workflow, not only the closing commit or an older snapshot page.
- Before calling a snapshot page stale, read its banner and its date against the date the fix landed (secret replaced 2026-09-22T09:21:28Z, snapshot 2026-09-21), and take the secret's created_at and updated_at from `gh api repos/<repo>/actions/secrets`, since `gh secret list` shows only the updated date.
- Before citing a run id as evidence, open it with `gh api repos/<repo>/actions/runs/<id>/jobs` (conclusion and steps in one call); a skipped Dependabot run has no steps and proves nothing. For "has never run", group `gh api .../workflows/<file>/runs?created=<range>` by conclusion instead of relying on the snapshot page.
