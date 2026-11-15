- (issue-archivist) The pre-draft had `closed_at: 12/27/2025 13:01:38`, `linked_prs: [63]` (the issue number itself), `packages` straight from the issue and invented "Rules and lessons" and destinations; all normalised or dropped. A close comment's EventId range (600-615) was again wrong (actual 2915-2930); read `EventId =` values in the code.
Applied: role:issue-archivist verify a close comment's EventId range and linked_prs against the code and PRs, never trust the pre-draft
- (issue-archivist) Same-named files can belong to a different feature (sharding `ScatterGather*` vs Messaging); scope by namespace/folder, and name the collision so the code stage ignores it.
Applied: role:issue-archivist scope same-named files by namespace and folder and name the collision for the code stage
- A configuration option that the runner reads into a local variable and then does not pass on (`resultsToGather`, `ScatterGatherRunner.cs:440-448`) passes a "does the option have a reader" grep; the check has to follow the value to the call it was meant to reach, and the options test that only sets and gets the property (`ScatterGatherOptionsTests.cs:88-95`) is the pointer.
Applied: role:issue-auditor follow an option value to the call it should reach, not only to a reader
- For a runner that has its own cancellation source for strategy completion (`linkedCts.CancelAsync()`), trace which token every later phase receives: the gather phase got the source the runner itself had cancelled. Hand the verifier a reproduction with an asynchronous delegate that uses its token, because every test helper in the folder ignored it.
Applied: role:issue-auditor trace which token every later phase receives when the runner owns a cancellation source
- For a family of copied runners, the sibling issues usually cite only their own file in Location (here #3307, #3305, #3311, #3309 each name `RoutingSlipRunner`), and a sentence such as "the same gap exists in ScatterGatherRunner" in one of them (#3311) is not coverage; the counterpart still gets its own finding, with the sibling issue as the precedent for the fix.
Applied: role:issue-auditor a sibling issue naming only its own file does not cover the copied counterpart
- A returned `Right` with zero successes under a "partial results allowed" strategy deserves a search for the selectors that follow it: `FirstOrDefault()` plus `is null` on an unconstrained generic is never null for a value type, which turns an empty gather into a successful default.
Applied: role:issue-auditor search the selectors after a Right with zero successes for default-on-value-type results
- `PublicAPI.Unshipped.txt` with 0 lines for a whole folder is a quick check for a package that does not reference the analyzer; it is owned by #1171 and should be recorded as Informational instead of re-filed per audit.
Applied: not applied: PublicAPI coverage of Messaging is owned by #1171
- A timeout test whose assertion accepts every outcome (`result.IsLeft.ShouldBeTrue()` with a comment "either X or Y", `ScatterGatherRunnerTests.cs:311`) is how a never-produced error code (`scattergather.timeout`) goes unnoticed; the unit-missed list pointed at it at once (`ScatterGatherRunner.cs:110-116`). Read the missed lines for each error code the code declares before judging the tests.
Applied: role:test-auditor read the missed lines for each declared error code before judging tests
- A test that pins a bug (`:316-341` asserts `Right` with zero successes) must be named in the findings as "will flip", otherwise the fix PR looks like a regression to a reviewer.
Applied: role:test-auditor name tests that pin a bug as "will flip" in the findings
- Same-named sharding tests match a `~ScatterGather` name filter; the `Encina.UnitTests.Messaging.ScatterGather` namespace filter isolated the feature in one pass, and the wider name filter was needed only to prove that the `ErrorMessageLeakTests` class reaches no extra line.
Applied: role:test-auditor isolate a feature by namespace filter when same-named tests exist elsewhere
- A sibling audit (#62) with docs issues already open makes the new work two things: extend the open issues for the same files (Messaging README, tutorials index) and find the pattern-specific artifacts the sibling did not have, here a "compilable" example in a `nav_exclude` page that CHANGELOG links.
Applied: role:docs-reviewer with a sibling audit open on the same docs, extend its issues and look for pattern-specific artifacts
- A page that calls its own sample "compilable" and "functional" while a comment says "hypothetical API" is checked by reading the claim against the comments in the same block, not only the identifiers; the runner type, the handler lambda return type and the `Either` unwrap each fail independently.
Applied: role:docs-reviewer read a "compilable" claim against comments in the same block, each identifier can fail independently
- Search the feature name over `src/**/*.md` and root files as well as `docs/`: the only README mention was in the EF Core package README, and the sharding scatter-gather pages must be excluded by path before counting hits.
Applied: role:docs-reviewer search the feature name over src/**/*.md and root files as well as docs/
- tests 4: merged into code 5 by manual override
Applied: not applied: specific to the findings of audit #63, recorded in its stage files and drafts
- code 11 is written with one fix (failures exclude cancelled handlers): the property documentation already defines two disjoint counts, so the code stage's second shape (cancelled as a subset of failed) contradicts it.
Applied: not applied: specific to the findings of audit #63, recorded in its stage files and drafts
- code 10 (observability) is settled by ADR-054 decision D3, so the draft has no design choice; the code stage found that the routing slip observability issue does not cover this file.
Applied: not applied: specific to the findings of audit #63, recorded in its stage files and drafts
- The Glob tool returned no files for `docs/architecture/adr/054*` and `src/Encina.Messaging/PublicAPI*.txt` although the ADR exists (a Grep found it); absence claims (no PublicAPI line, no Scatter hit in the Messaging README, no tutorial hit) were checked with count-mode or content Grep.
Applied: not applied: specific to the findings of audit #63, recorded in its stage files and drafts
- The Glob tool returned no files for `docs/architecture/adr/054*` and `src/Encina.Messaging/PublicAPI*.txt` although both exist (a Grep found them); `src/Encina.Messaging` has `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt`, and neither holds a ScatterGather line (count-mode Grep, with a positive control of 934 lines). Absence claims (no PublicAPI line, no Scatter hit in the Messaging README, no tutorial hit) were checked with count-mode or content Grep.
Applied: role:remediation-drafter an empty Glob is not proof of absence, use count-mode Grep with a positive control
- The tests stage cites `ScatterGatherRunnerTests.cs:404` for the WaitForFirst test; the draft cites the stage's line without re-reading that region (lines 255-343 and 600-629 of the test file were read).
Applied: role:remediation-drafter re-read the region of a test a stage cites before citing its line
- docs 4: recorded as duplicate of #1299 by manual override
Applied: not applied: specific to the findings of audit #63, recorded in its stage files and drafts
- tests 6: merged into code 6 by manual override
Applied: not applied: specific to the findings of audit #63, recorded in its stage files and drafts
- code 2 is written as a Dilemma (does IncludeFailedResultsInGather govern WaitForAllAllowPartial, whose GatherStrategy documentation says it receives failures), now with a Pro and a Con for each option; the draft carries no needs-decision label, the orchestrator adds it with the board gate.
Applied: not applied: specific to the findings of audit #63, recorded in its stage files and drafts
- code 6 is written as a neutral Dilemma on the three dead options (remove them, or wire them as defaults the way the runner already does for DefaultTimeout and MaxDegreeOfParallelism at ScatterGatherRunner.cs:55 and :134); the three dead error codes are removed outside the choice because Build() throws. A verified absence of readers did not settle the options: wiring complies too. The draft carries no needs-decision label, the orchestrator adds it with the board gate.
Applied: not applied: specific to the findings of audit #63, recorded in its stage files and drafts
- code 4 cites #3235 as filed for the sharded executor, open and not yet decided, in a `Related Issues:` label inside Additional Context (bug_report.md has no Related Issues header); the interface citation is `IScatterGatherRunner.cs:16`. Drafts code 4 and code 6 were not on disk when this re-run started, so both were written again and re-verified in the audit worktree.
Applied: not applied: specific to the findings of audit #63, recorded in its stage files and drafts
- code 6 cites open [SPIKE] #3412 (confirms the 63 option properties nothing reads, plans a child per package family) as the parent and states that the draft is its child for the Messaging scatter-gather family; #3480 (open [INFRA] gate: every public option has a reader) is cited under Related Issues. The Dilemma (A remove, B wire) is kept with no needs-decision label.
Applied: role:remediation-drafter every absence claim carries its positive control in the same sentence
- A literal sentence like "X has no `PublicAPI*.txt`" came from a Glob that returned nothing; the re-run replaced it with count-mode Grep numbers (Shipped 1 line, Unshipped 933, 0 ScatterGather, positive control 934). For any absence claim, require the positive control in the same sentence.
Applied: role:audit-verifier when -Finalize strips a parent spike number, accept words naming the relation and hand the numbers to the orchestrator
- When `-Finalize` strips the number of the parent spike, a draft stays acceptable if it says in words that it is the child of "the open spike" and for which family; the verifier then checks that no sentence contradicts the relation and hands the numbers to the orchestrator as a comment.
Applied: role:audit-verifier check a dead-option draft against umbrella spikes and gate issues by property name
- A dead-option draft should be checked against the umbrella spike and the gate issue by property name (#3412 names all three properties); the spike decides nothing for the options, so the Dilemma stays open and the draft is the child, not a duplicate.
Applied: role:audit-verifier compare bug-draft header lists printed side by side, not by an equality flag
- Bug-draft header comparison: print both lists with `## Root Cause` dropped from both; the extra `## Related Issues` heading of pass 2 would have been hidden by an equality flag alone.
Applied: role:audit-verifier after a narrow FAIL, diff the previous verification commit to HEAD and re-check only what changed plus the global counts
- After a narrow FAIL whose diff over `src tests docs .github CHANGELOG.md ROADMAP.md` is empty, the closing set was: the diff between the previous verification commit and HEAD (two artifact files), `--unified=0` of `remediation.md`, every citation of the two redrafted files printed, the old-phrase grep over stages, drafts and manifest, the header/tick/milestone/emoji command over the 19 drafts, the write-time listing, REST state of the cited issues and a created-since search; measurements carried forward.
Applied: 
