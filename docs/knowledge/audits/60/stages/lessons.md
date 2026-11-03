- Pre-draft carried `closed_at: 12/27/2025 00:34:56`, `linked_prs: []`, "specific files unknown" and invented rules/destinations; re-derived from the timeline close commit and `git show --stat`.
Applied: not applied: same lesson already recorded for issue-archivist from audit #59 (2026-10-28); repeat occurrence only.
- `gh issue view --comments` printed nothing again; `--json title,body,comments` worked.
Applied: not applied: already a rule in issue-archivist's memory (#59 lesson, PR #2977).
- The commit-stat test projects (`tests/Encina.AwsLambda.*`) are all deleted by a later consolidation; the commit's "21 contract, 10 property" tests have no obvious successor, check with the test auditor.
Applied: not applied: handed to the tests stage in this audit (tests 5 and tests 6 drafts); test-auditor already has the consolidation-commit check from #59.
- A host-adapter package that mirrors one already audited (Lambda after Azure Functions) needs its sibling's open issues read first (#2945-#2949, #2960): four of the ten findings are the Lambda counterparts of Azure bugs that have no Lambda issue, and two of those issues explicitly defer the Lambda copy only in a sentence ("same gap in its copy"), which the remediation stage should not treat as coverage.
Applied: role:issue-auditor For a package that mirrors an audited sibling, read the sibling's open remediation issues first; a sentence like "same gap in its copy" in a sibling issue is not coverage, so the counterpart finding still gets its own issue.
- A file-based replica that references only NuGet packages (`#:package`, no `#:project`) written with the Write tool to a literal scratchpad path and run with `dotnet run <literal path>` was not blocked and turned two derived claims (case-sensitive header dictionary, v2 authorizer shape) into executed evidence; the stage text can say so for event-shape questions.
Applied: role:issue-auditor For event-shape or serializer questions, write a NuGet-only file-based replica with the Write tool to a literal scratchpad path and run it with `dotnet run <path>`; report the result as executed evidence.
- Hook note: the first `gh issue list ... > $f` redirect through a variable produced a `block-main-checkout-writes` warning (not a block, the scratchpad path was correct); later commands used literal absolute paths and raised none.
Applied: role:issue-auditor Redirect output only to literal absolute scratchpad paths, never through a variable, so the main-checkout write hook can recognise the target.
- For options classes, a one-line search per property for readers outside the health check found five dead options; make "options read only by the health check or its own tests" a standing check for every `Encina*Options`.
Applied: role:issue-auditor For every `Encina*Options` class, search each property for readers outside the health check and its own tests; a property with none is a dead option finding.
- Coverage of a file whose `defaultTests` include `guard` must be judged from its `ThrowIfNull` lines: `ApiGatewayResponseExtensions.cs` has one, so a copied guard target of 25 is unreachable by construction; the manifest check should compare each per-file guard target with the file's `ThrowIfNull` count.
Applied: role:test-auditor Compare every per-file and package guard target with the number of argument-check lines (`ThrowIfNull` and siblings) in scope; a target above what those lines can cover is a finding with a proposed reachable target.
- A consolidation commit deleted a property test that tested a private copy of the mapping; check a deleted test for what it executed before asking for it back (restoring it would add no coverage).
Applied: role:test-auditor Before asking for a deleted test back, check what it executed; a test of a private copy of production logic covers zero package lines.
- Tests that assert "the buggy outcome" (`WithSomeFailures`, the dead-option setters) are found fastest by reading each code-stage defect against the test list; the stage definition could ask for that table for every audit with a Blocker or Major code finding.
Applied: role:test-auditor For every Blocker or Major code finding, list the existing tests that assert the defective behaviour (pinning tests) in one table.
- A package-level guard target should be checked against the count of `ThrowIfNull` lines before a family issue (#1389) tells a worker to "reach the target".
Applied: not applied: covered by the guard-target lesson above (same rule, package level).
- A README for a package that mirrors an audited sibling (AwsLambda after AzureFunctions) has most of its docs gaps already owned by the sibling's open issues (#2951 names the shared page, #2280 lists the empty Performance marker for nine READMEs, #2958 the missing links); read those bodies first and spend the time on what the code stage proved false in the text (dead options, cold-start advice, swallowed `Left`).
Applied: role:docs-reviewer For a package that mirrors an audited sibling, read the sibling's open docs issues first, record the shared gaps as duplicates, and spend the review on README claims the code stage proved false.
- The strongest README defects here came from reading each documented option and each "automatic" claim against the code stage's dead-option list, not from symbol checks: every identifier in the samples exists, while the behaviour the page promises does not.
Applied: role:docs-reviewer Check each documented option and each "automatic" claim against the code stage's findings, not only symbol existence; a sample can compile while the behaviour it promises does not exist.
- `markdownlint-cli2` reports MD060 (compact table style) on separator rows of READMEs written before the rule existed; run it on every README in scope and count by rule.
Applied: role:docs-reviewer MD060 errors on table separators are tracked repo-wide by #1371: record them as a duplicate of #1371, not a new finding.
- docs 5: recorded as duplicate of #2951 by manual override
Applied: not applied: per-audit override record, confirmed by the verifier (#2951 covers the shared AzureFunctions/AwsLambda page); comment added to #2951 at opening.
- docs 11: recorded as duplicate of #1299 by manual override
Applied: not applied: per-audit override record, confirmed by the verifier; comment added to #1299 at opening.
- docs 12: recorded as duplicate of #1371 by manual override
Applied: not applied: per-audit override record, confirmed by the verifier; comment added to #1371 at opening.
- docs 13: recorded as duplicate of #2280 by manual override
Applied: not applied: per-audit override record, confirmed by the verifier; comment added to #2280 at opening.
- docs 6: kept out of location merges by manual override
Applied: not applied: per-audit override record; the verifier required uncompilable README samples to be their own [DEBT], not inside a [BUG].
- docs 1: merged into code 2 by manual override
Applied: not applied: per-audit override record; the README documents the dead options of code 2.
- docs 2: merged into code 3 by manual override
Applied: not applied: per-audit override record; the README text of the SQS FIFO defect.
- docs 3: merged into code 4 by manual override
Applied: not applied: per-audit override record; the README samples of the swallowed Left defect.
- docs 4: merged into code 5 by manual override
Applied: not applied: per-audit override record; the README cold-start advice of the AOT defect.
- tests 2: merged into tests 3 by manual override
Applied: not applied: per-audit override record; one coverage-manifest issue.
- tests 4: merged into tests 3 by manual override
Applied: not applied: per-audit override record; one coverage-manifest issue.
- tests 10: merged into tests 3 by manual override
Applied: not applied: per-audit override record; one coverage-manifest issue.
- tests 7: merged into tests 8 by manual override
Applied: not applied: per-audit override record; two small coverage gaps in one issue.
- (2026-10-30, #60) (code 2, code 3, code 5, code 7, tests 6) Findings with two valid options are written as neutral Dilemmas with a route-only labels line; the orchestrator adds `needs-decision` at opening. The AWS behaviours (FIFO partial-batch contract, HTTP API v2 lower-case headers) and the consolidation commit `658263027` are stated as the stages recorded them: this agent cannot fetch AWS documentation or read git history. Bug drafts carry Related Issues inside Additional Context because the bug template has no such header. (state unfetched external facts as recorded; keep Related Issues inside Additional Context for the bug template)
Applied: role:remediation-drafter State external facts you cannot fetch (vendor documentation, git history) as the stage recorded them; in bug drafts put Related Issues inside Additional Context, since the bug template has no such header.
- (2026-10-30, #60) (tests 3) The tests stage's package guard figure (32 covered of 393 lines) was recomputed to 8.14% for the Current Coverage table; proposed targets stay labelled derived until a measured run. Issue titles for #727, #1389 and #2948 come from the findings or the manifest where present; those without a manifest title were cited by number with the finding's description. (keep derived targets labelled; cite untitled issues with the finding's description)
Applied: role:remediation-drafter Keep proposed coverage targets labelled "derived" until measured; cite an issue without a manifest title by number with the finding's own description.
- (2026-10-30, #60) (code 2, docs 1) Re-run after the verifier FAIL: the earlier draft pre-decided its Dilemma by saying the page-side fix is to list only the options that are read. The README defect is now stated as a fact only; what the README says is written inside Option A (lists only what is read) and Option B (keeps the options, now true), and the CA1848 false suppression is Part 1, independent of the Dilemma. The draft file was not on disk when this run started, so it was rewritten and its facts re-read in the audit worktree (`EncinaAwsLambdaOptions.cs`, `Log.cs`, `GlobalSuppressions.cs`, `AwsLambdaHealthCheck.cs:42-44`, `README.md:7-11, 112-140`, `ServiceCollectionExtensions.cs:44,46`). The Description also notes that `IncludeExceptionDetailsInResponse` is not even echoed by the health check, which the code stage did not say. (keep a README fix inside the options of a Dilemma; state a fact before the options)
Applied: role:remediation-drafter In a Dilemma whose fix also changes documentation, state the documentation defect as a fact and put what the page says afterwards inside each option; a "the fix is..." sentence outside the options pre-decides it.
- When `gh issue list` hits the GraphQL rate limit, `gh api -X GET search/issues -f q=...` and `gh api repos/dlrivada/Encina/issues/<n>` still work (REST core and search buckets are separate); use them for the duplicate search and issue states.
Applied: role:audit-verifier When `gh issue list` hits the GraphQL rate limit, use `gh api -X GET search/issues -f q=...` and `gh api repos/dlrivada/Encina/issues/<n>` (separate REST buckets) for duplicate searches and issue states.
- (orchestrator) The docs-stage re-run edited docs.md with a PowerShell .NET string replace instead of the Edit tool; the ownership check accepted it because the previous writer was the same agent.
Applied: role:docs-reviewer Write and correct stage files only with the Edit or Write tools, never with a PowerShell or .NET file write, even for a one-line replace.
