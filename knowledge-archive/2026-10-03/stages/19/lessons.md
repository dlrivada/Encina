- The pre-draft listed a CI workflow file as scope without checking it still exists; verify renames with `git log --follow` before listing scope. dotnet-ci.yml was consolidated away.
Applied: role:issue-archivist Check every scope path exists today (git log --follow for renames and consolidations) and list the successor file, never a path that is gone.
- A fresh wia worktree has no `artifacts/knowledge/predraft`; read it from the main checkout and create `artifacts/knowledge/issues` in the worktree.
Applied: role:issue-archivist Read the pre-draft from the main checkout's artifacts/knowledge/predraft and create artifacts/knowledge/issues in the wia worktree when it is missing.
- When the archivist finds no `src/` scope for a rejected-by-replacement issue, the code stage's real surface is the replacement's own enforcement path (`.github/scripts`, workflows, templates, dashboards, agent skill files). The scope list should name those files so the code stage does not start from nothing.
Applied: role:issue-archivist For an issue rejected because something replaced it, list the replacement's enforcement path (scripts, workflows, templates, dashboards, skill files) as scope instead of an empty src/ scope.
- For "rejected number" issues, grep the whole tree (templates, skills under `.opencode/` and `.claude/`, dashboard JS, open issue titles and bodies via `gh issue list --search`) for the literal figure; the archivist's search of README/ROADMAP/CLAUDE.md/AGENTS.md found none and missed every real leftover.
Applied: role:issue-archivist For a rejected figure, search the whole tree (templates, .opencode and .claude skills, dashboard JS) and open issues for the literal value, not only README/ROADMAP/CLAUDE.md/AGENTS.md.
- A rule stated as MUST in AGENTS.md needs an enforcing job; the audit should always check "who fails when this is violated?" and record "nobody" as a finding, not accept a documented intent.
Applied: role:issue-auditor For every MUST rule in scope ask which job fails when it is violated; "nobody" is a finding (audit #19 code 1).
- The `guard-orchestrator-writes` hook blocked both `pwsh -File .github\scripts\crap-gate-selftest.ps1` and `dotnet run .github\scripts\generate-coverage-manifest.cs -- --self-test` (run from inside the worktree after `Set-Location`) with: "Blocked: A test-auditor subagent (not a governed writing agent) does not edit src/ or tests/; brief an issue-worker (worker-brief skill), which also resolves rebase conflicts there. runs '.github\scripts\crap-gate-selftest.ps1' (pwsh -File), whose script path the hook cannot resolve, so it cannot rule out writes to src/ or tests/; pass its literal path (#1181; .claude/agents/README.md, Delegation)." The hook asks for a literal path, but a test-stage agent is told never to rephrase a blocked command, so the two self-tests were not run. Likely false positive: a read-only audit of CI tooling cannot run its own self-tests. The orchestrator should decide whether the hook accepts relative paths under the worktree (`.github/scripts/*.ps1|cs`) or whether the test-auditor definition should state that literal absolute script paths are the expected form.
Applied: role:test-auditor Run .github/scripts self-tests with their literal absolute path inside the wia worktree from the start (D:\Proyectos\Encina\.claude\worktrees\wia-<n>\.github\scripts\...); audit-verifier ran both self-tests that way unblocked, so the hook is right and needs no change.
- For tooling-only audits the test stage has no coverage flag to measure: the stage definition and the `tests.md` template should say "not measured: no src/ scope" and ask for self-test runs and presence of CI wiring instead.
Applied: role:test-auditor When the scope has no src/ code, write "not measured: no src/ scope" for the coverage flags and measure self-test runs and their CI wiring instead.
- Check not only that a self-test exists but that CI runs it and on which path filters: two of the three self-tests here (`generate-coverage-manifest.cs --self-test` never, `crap-gate-selftest.ps1` only on `src/**` changes) do not guard changes to their own tool.
Applied: role:test-auditor For every self-test, check that CI runs it and that its path filters include the tool's own files (audit #19 drafts 19-tests-3 and 19-tests-4).
- A rejected-number issue needs a literal-figure search over the whole repository, not only README/ROADMAP/CLAUDE/AGENTS: here the figure survived in CONTRIBUTING (as 90, a different number, so a search for 85 alone misses it), the issue and PR templates, the `.opencode` skills, the dashboard JS and the ADR. The docs stage should search for every coverage-like `%` literal in the pages it reviews, not only the issue's number.
Applied: role:docs-reviewer In an audit about a rejected figure, search the reviewed pages for every literal of the same kind (every coverage-like %), not only the issue's own number.
- An ADR that a methodology page cites as "what we use" must be checked against the config it describes (`codecov.yml` says Components are unavailable); when code and ADR disagree the finding is a missing superseding ADR, not an edit of the old one.
Applied: role:docs-reviewer Check an ADR a page cites as current against the config it describes; when they disagree, the finding is a missing superseding ADR, never an edit of the accepted one.
- Doc claims of enforcement ("the real enforcement happens at ...") must be checked against a job that fails; two pages here claimed a gate that the code stage proved absent.
Applied: role:docs-reviewer Verify every enforcement claim in a page against a CI job that actually fails; a claimed gate with no failing job is a finding.
- The guard hook asks for literal absolute script paths; stating that in the stage briefs (as this brief did) avoids the block the tests stage hit.
Applied: not applied: same lesson as the guard-hook one above, recorded once as role:test-auditor (stage agents get no brief beyond the issue and worktree, so their own memory is where it belongs).
- When one phrase is cited at several line numbers, open every line and quote what each says; a grouped citation here attributed "no single project-wide percentage" to four lines when only two say it. Measure every figure about a page (a length, a count) before stating it; the 1,700-character paragraph was 1,229.
Applied: role:docs-reviewer Open every line of a grouped citation and quote what each one says; measure every figure about a page (length, count) with a command before stating it (audit #19 FAIL).
- docs 6: merged into code 2 by manual override
Applied: not applied: record of the orchestrator's -MergeInto override (#1632), verified by audit-verifier; nothing to change.
- docs 5: merged into code 3 by manual override
Applied: not applied: record of the orchestrator's -MergeInto override (#1632), verified by audit-verifier; nothing to change.
- docs 4: merged into code 4 by manual override
Applied: not applied: record of the orchestrator's -MergeInto override (#1632), verified by audit-verifier; nothing to change.
- docs 2: merged into code 8 by manual override (the merged group's primary is docs 2)
Applied: not applied: record of the orchestrator's -MergeInto override (#1632); the docs 2 draft also covers code 8's .github/scripts artifacts, as briefed.
- docs 9: merged into code 8 by manual override (the merged group's primary is docs 2)
Applied: not applied: record of the orchestrator's -MergeInto override (#1632), verified by audit-verifier; nothing to change.
- tests 5: merged into code 7 by manual override
Applied: not applied: record of the orchestrator's -MergeInto override (#1632), verified by audit-verifier; nothing to change.
- tests stage (tests 5): named the report output `coverage.json`; the script writes `encina-coverage-summary.json`, `encina-coverage-report.md`, `docref-index.json`, `badge.json` and `badge.svg` (`coverage-report.cs:739`, `:795`, `:803`, `:818`, `:824`); the code 7 draft uses the real names.
Applied: role:test-auditor Quote output file names from the code that writes them (file:line), never from memory.
- tests stage (tests 4): placed `generate-coverage-manifest.cs --self-test` at `:348-482`; the self-test section spans `:348-484` (`RunSelfTest` at `:350`).
Applied: role:test-auditor Re-open the last line of every cited range before writing it.
- code stage (code 4): called the 85% line the overall trend chart's; `renderTrendChart` (`docs/coverage/app.js:744`) draws it for the combined series and for every per-flag series alike.
Applied: role:issue-auditor Trace a rendering or reporting function through its callers before describing what it draws or for which series.
- code stage (code 9): missed `docs/ci-cd-templates.md`, which documents the `coverage-threshold` input and recommends values (`:49`, `:87`, `:111`, `:132`, `:207`, `:255`, `:293`); the code 9 draft covers it.
Applied: role:issue-auditor When a finding names an input, option or flag, search docs/ for it too, so the pages that document it are in the finding.
- code stage (code 8): did not notice that `.github/scripts/coverage-weights.json` points `$schema` at a `coverage-weights-schema.json` that does not exist.
Applied: role:issue-auditor For a JSON or config file in a finding, check that its $schema and other references resolve.
- possible duplicate the manifest did not catch (for audit-verifier): tests 8 (text check for a hand-typed 85% target) overlaps the optional text check named in the code 2 and code 4 drafts; the tests 8 draft stays separate because it asks for the check itself.
Applied: not applied: audit-verifier re-checked it and kept tests 8 separate (it asks for the check itself; code 2 and code 4 only mention it as optional).
- Re-running the docs stage with `-Only 'docs 11'` and keeping the six manual merges left every other draft and `stages/remediation.md` untouched, which let a verifier limit the re-check to the changed draft plus the cross-stage count; keep using `-Only` for single-finding corrections.
Applied: not applied: already the documented procedure (issue-audit SKILL.md, -Only, #1492); this audit confirmed that merges now persist across -Only runs (#1632, PR #1644).
- A verifier's own counts need measuring too: the previous verification wrote "22 drafts" and "code 1-11 (9 drafts)" from memory of the manifest; counting the files and the `title:` lines gave 23 drafts. Count with a command before writing a number.
Applied: role:audit-verifier Count drafts, findings and lines with a command before writing any number in the verdict.
