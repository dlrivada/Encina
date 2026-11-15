- Pre-draft had `closed_at: 09/21/2026` and `linked_prs: []` in non-schema form; normalised.
Applied: not applied: specific to audit #75, recorded in its stage files
- The issue's premise ("SONAR_TOKEN is not configured") was already false when written: the secret is dated 2 days before the issue. Check secret creation dates via `gh api repos/.../actions/secrets` for configuration issues.
Applied: role:issue-archivist check secret created_at and updated_at via gh api actions/secrets for configuration issues
- A doc (PHASE0-BASELINE) cites the closed issue as a live blocker; grep docs for `#<n>` of closed config issues.
Applied: role:issue-archivist grep docs for the number of a closed configuration issue cited as a live blocker
- The worktree lacks the pre-draft; it was read from the main checkout (same as earlier lessons).
Applied: not applied: specific to audit #75, recorded in its stage files
- For a CI-scanner issue ("run the first complete scan"), compare the file set the scanner holds with the file set of the worktree (public SonarCloud `components/tree` against `Get-ChildItem src -Recurse`) and read the scanner's own `exclude flag` / `skipped` lines in the job log: the one package the scanner dropped is invisible in the YAML and in the dashboard totals, and neither earlier audit of this workflow (#30, #32) looked at coverage of the scan.
Applied: role:issue-auditor for a scanner issue compare the scanner file set with the worktree file set and read its own exclude/skipped lines
- A qualifier matters when listing SonarCloud components: test files are `UTS`, main files `FIL`; query both before concluding a path is missing.
Applied: role:issue-auditor query both test (UTS) and main (FIL) qualifiers when listing SonarCloud components
- When two earlier audits already cover a workflow (#30 and #32 here), search the open-issue dump for each of their findings first; all of them were still open, so the new effort belonged to what the workflow does, not to what its suppressions say.
Applied: role:issue-auditor when earlier audits already cover a workflow, search their findings in the open-issue dump first
- For a CI-workflow issue with no `src/` scope, the tests stage cannot measure a coverage flag; the useful output is a table of script, self-test, CI wiring and trigger plus one finding per check that would have caught a code-stage defect (here the silently excluded package and the redundant suppressions), as in audits #19, #31 and #67.
Applied: role:test-auditor for a CI workflow issue without src scope produce the script/self-test/CI-wiring/trigger table and one finding per missing check
- The delta/original brief for a scanner-workflow issue should tell the tests stage to look for a completeness check (scanner file set vs `src/` file set); the three audits of `sonarcloud.yml` (#30, #32, #75) each found a different gap that no script guards.
Applied: role:test-auditor look for a completeness check (scanner file set vs src file set) for scanner workflow issues
- When a page says a CI job is skipped or never ran, check the workflow `if:` condition, `gh secret list` and `gh run list` for that workflow, not only the closing commit or an older snapshot page.
Applied: role:docs-reviewer for "skipped or never ran" claims check the workflow if: condition, secrets and run list
- Before calling a snapshot page stale, read its banner and its date against the date the fix landed (secret replaced 2026-09-22T09:21:28Z, snapshot 2026-09-21), and take the secret's created_at and updated_at from `gh api repos/<repo>/actions/secrets`, since `gh secret list` shows only the updated date.
Applied: role:docs-reviewer read a snapshot page banner and date against the date the fix landed, with created_at and updated_at
- Before citing a run id as evidence, open it with `gh api repos/<repo>/actions/runs/<id>/jobs` (conclusion and steps in one call); a skipped Dependabot run has no steps and proves nothing. For "has never run", group `gh api .../workflows/<file>/runs?created=<range>` by conclusion instead of relying on the snapshot page.
Applied: role:docs-reviewer open a run with gh api runs/<id>/jobs before citing it; group runs by conclusion for "never ran"
- (2026-11-12, #75) This agent has no shell, so the SonarCloud and `gh` facts (run 43217793385 log lines, file counts 3041/2961, `SONAR_TOKEN` date, run times, closed state of the issue) are stated as the stages recorded them; only repository facts were re-read in wia-75 (`sonarcloud.yml:3-8, 25-28, 51, 60-68, 107-128, 161-162, 176`, `.config/dotnet-tools.json`, `Directory.Packages.props:44`, `Directory.Build.props:34, 70-73`, `Encina.Testing.Fakes.csproj:5`, `ROADMAP.md:42, 159`, `PHASE0-BASELINE.md:15, 148`). Unverified by this agent: the live SonarCloud state.
Applied: role:remediation-drafter a drafter without a shell must say which external facts come from stage texts so the verifier re-checks them
- (2026-11-12, #75) docs 2 cites PHASE0-BASELINE as `:15` (item 4); the same claim is also at `:148`, which the draft adds. The line is in the v0.12.0 section, which the docs stage did not name.
Applied: not applied: specific to audit #75, recorded in its stage files
- tests 2: recorded as not a duplicate and kept out of location merges by manual override
Applied: not applied: specific to audit #75, recorded in its stage files
- (verifier, #75) The pass-2 correction closed in one round because the stage opened each run with `gh api .../runs/<id>/jobs`, re-quoted the log line with its timestamp, and replaced "never run" with the grouped conclusion counts and the request that produced them (`per_page=100&created=...`, 596 in range, 100 returned). A count taken from a paged API should always state `total_count` and how many were returned.
Applied: role:audit-verifier open each cited run with gh api jobs and re-quote the log line with its timestamp
- (verifier, #75) A narrow pass 3 after a one-bullet docs fix: the diff against the previous verification commit (two files), the empty `src tests docs .github CHANGELOG.md ROADMAP.md` diff, `--unified=0` of `docs.md`, each cited run/job/secret/log re-queried, the banner and the three snapshot lines printed, an old-phrase grep over stages and drafts, and draft write-times; about ten calls.
Applied: role:audit-verifier narrow pass after a one-bullet fix: diff to the previous verification commit and re-query each cited run
- (verifier, #75) Note for the orchestrator (unchanged from pass 2): `-Finalize` strips issue numbers that are not in the finding, so the tests 2 draft lacks #76; when opening, add #76 to its Related Issues (the rationale requirement overlaps #76's "All suppressions documented with justification" task).
Applied: not applied: handled when opening the issues (#76 added to the tests 2 draft by the orchestrator)
