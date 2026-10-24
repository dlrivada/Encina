- The pre-draft listed code touched as "unknown" and `closed_at: 12/27/2025 11:31:05`, `linked_prs: []`, and packages straight from the issue; resolved with the timeline `closed` commit and `git show --stat`.
Applied: role:issue-archivist When the pre-draft lists code touched as unknown, resolve it from the timeline closed commit with git show --stat.
- The pre-draft's "Rules and lessons" and "spec-invariant"/"adr" destinations had no quotable source and were dropped.
Applied: not applied: one-off cleanup of this pre-draft; the archivist already drops unsourced destinations by its definition.
- The issue title names an interface (`IDistributedLock`) that never shipped; compare title symbols with src/ before choosing `delivered` (outcome set to `partial` because of the unbuilt Redlock and renamed API).
Applied: role:issue-archivist Compare the symbols in the issue title with src/ before choosing outcome delivered; a renamed or unbuilt API means partial.
- The pre-draft was read from the main checkout (the worktree has none, `artifacts/` is gitignored).
Applied: not applied: expected behaviour (artifacts/ is gitignored, so pre-drafts live only in the main checkout); no change needed.
- The archivist's scope stopped at the four `Encina.DistributedLock*` packages; the closing commit also touched `Encina.Caching`, and the two cache packages implement the same interface. For a feature that moved an abstraction out of a package, the scope list should include every remaining implementer of the interface (search `: IInterfaceName` over `src`).
Applied: role:issue-archivist When a feature moved an abstraction out of a package, list every remaining implementer of the interface (search ": IInterfaceName" over src) in the scope.
- Run the open-issue dump first: #2405 and #1799 already owned the most obvious defects (Redis owner check, SQL Server expiry, exceptions versus Either), so the effort went to the SQL Server command timeout, the provider-level `ExtendAsync` contract, the cache-package duplicates and the dead configuration path.
Applied: role:issue-auditor Dump the open issues of the scope first so effort goes to defects no open issue owns yet.
- A configuration example in a README is the cheapest pointer to a dead option: check that the options type the registration configures is the type the service resolves (finding 4).
Applied: role:issue-auditor A README configuration example is the cheapest pointer to a dead option: check the configured options type is the type the service resolves.
- A `?? default` on a property that is non-nullable with a default is dead code; check the declaring options class before trusting a fallback list (finding 10).
Applied: role:issue-auditor A "?? default" on a non-nullable property with a default is dead code; read the options class before trusting a fallback.
- A package with a 133-line provider that builds its own `SqlConnection` cannot reach a package unit target of 60 or guard target of 20 by null-check tests; for packages whose main file has no seam, the delta/rule (b) stage should compute the package ceiling first (lines the flag can reach divided by package coverable lines) before the per-file proposals.
Applied: role:test-auditor For packages whose main file has no seam, compute the package ceiling per flag before proposing per-file targets.
- The union of the unit and integration missed-line lists (not each list alone) is the quickest way to find a method that no test runs: here every coverable line of `SqlServerDistributedLockProvider.AcquireAsync` is in both lists, which neither flag's percentage shows.
Applied: role:test-auditor Intersect the unit and integration missed-line lists to find methods no test runs; percentages hide them.
- A manifest `reason` that names the tests that exercise a file ("Integration tests exercise with real Redis") needs the integration report line for that file; the Redis health check says so and runs 0 of 24 lines.
Applied: role:test-auditor A manifest reason that names the tests exercising a file needs that file's line in the flag report to back it.
- A whole-project integration-style run with one container per test class takes minutes: give the test stage `timeout: 600000` for integration filters and run integration last.
Applied: role:test-auditor Use timeout 600000 for integration filters and run the integration flag last.
- A consolidation commit deleted a contract, a property and a load project of a delivered feature without trace again (`658263027`); the tests stage should run `git show --stat <closing commit> -- tests` and `git log --diff-filter=D` for those paths whenever contract or property tests are absent today.
Applied: role:test-auditor When contract or property tests are absent today, run git show --stat <closing commit> -- tests and git log --diff-filter=D to see whether a consolidation deleted them.
- A README written for a C# feature can use a language keyword as a variable name (`lock`); an identifier-existence check passes it, so the docs stage should scan sample variable names against the C# keyword list.
Applied: role:docs-reviewer Scan sample variable names against the C# keyword list (e.g. lock); identifier-existence checks pass them.
- When the code stage reports a dead option path, the docs stage should map it to the README section that advertises it (here "Base Options") and write it as a README finding tied to the code finding.
Applied: role:docs-reviewer Map each dead option path the code stage reports to the README section that advertises it and file it as a README finding tied to the code finding.
- (code 8, tests 8) The extend semantics (add versus replace) was left open by the code stage; the code 8 draft holds it as a Dilemma and its labels are exactly `bug`: the orchestrator adds `needs-decision` after opening. Tests 8 waits for that decision for its discriminating pair.
Applied: not applied here: orchestrator adds needs-decision to the code 8 issue after open-remediation (done in this audit's step 5).
- (tests 4, tests 6) The consolidation commit hashes and dates are the tests stage's account from the git history; this agent has no shell and could not re-read them.
Applied: not applied: the verifier re-read the commit hashes with git in passes 1-5; nothing to change.
- (docs 4) The docs stage says "tutorials and learning paths"; no learning-path page exists under docs/, so the draft names the tutorials index, the guides and the features folders. Count-mode Grep: 0 hits in `docs/tutorials` and `docs/guides`; `docs/features` has 2 hits (`scheduling.md:137`, `time-based-sharding.md:576`), neither a feature page. The draft is independent of #908.
Applied: not applied: superseded by the pass 2-4 re-runs (docs 4 now cites #908 and lists the passing mentions as examples).
- (code 9) The error form for rejected arguments follows the open spike #1799; the draft does not pre-decide it.
Applied: not applied: superseded by the pass 2 re-run (code 9 takes the error form from #2667).
- (tests 3) The finding cites #1389 without a title in the manifest; the draft names it without a title claim.
Applied: not applied: informational; #1389 cited without a title claim is correct.
- code 5: recorded as duplicate of #2654 by manual override
Applied: not applied: logged override, verified by audit-verifier pass 2.
- code 6: recorded as duplicate of #2654 by manual override
Applied: not applied: logged override, verified by audit-verifier pass 2.
- tests 1: recorded as not a duplicate and kept out of location merges by manual override
Applied: not applied: logged override, verified by audit-verifier pass 2.
- (tests 1) The existing `SqlServerDistributedLockIntegrationTests` builds its own `MsSqlContainer` per class (`:19-20`), not a shared collection; the tests 1 draft names `ADO-SqlServer` with `SqlServerFixture` for the new tests. The tests 2 draft says the lock integration tests "already use" a shared SQL Server collection, which is not so; the verifier may want that sentence corrected.
Applied: not applied: corrected in the tests 2 re-run (next lesson).
- (tests 2) The sentence above about the tests 2 draft was corrected in the rewritten draft: it now names `ADO-SqlServer` with `SqlServerFixture` and says the existing class uses its own container.
Applied: not applied: informational; correction already in the draft.
- (tests 5) #2653 already asks for ValidateOnBuild tests in both registration orders, so the tests 5 draft keeps only the ValidateOnBuild and ValidateScopes proof of the three lock registrations and the base-options key test, and cites #2653 as partially related.
Applied: not applied: informational; draft narrowed and verified in pass 3.
- (docs 7) The README line "Lock auto-expires after 5 minutes" (`README.md:55`) is not true for SQL Server (the session lock ignores `expiry`); the docs stage did not report it, so the docs 7 draft adds it. #2654 is cited as partially related (only its Redlock part overlaps).
Applied: not applied: informational; draft verified in pass 3.
- tests 4: recorded as duplicate of #2667 by manual override
Applied: not applied: logged override, verified by audit-verifier pass 3.
- (pass 2 re-run) The docs 4 lesson above ("independent of #908") and the code 9 lesson above ("follows the open spike #1799") are superseded: the docs 4 draft now cites #908 as partially related (it covers only the feature page) and the code 9 draft takes the error form from #2667 (the maintainer decision recorded on #1799).
Applied: not applied: informational; supersession recorded.
- (tests 1) Line `:274` (`IsReleased`) is executed by the unit run (`SqlServerLockHandleReleaseTests.cs:34`), so the draft lists only 272, 278, 280, 284-285 and 292 of the handle; it cites #2453 as partially related for moving the existing integration class to the shared collection and says only the new tests use `ADO-SqlServer`.
Applied: not applied: informational; draft verified in pass 3.
- (tests 1) Finalize stripped the #2453 line from the tests 1 draft because #2453 is not a manifest partially related candidate (it is only a possibly-related hit); the verifier asked for it, so it is cited as an orchestrator comment instead of in the draft.
Applied: role:remediation-drafter When Finalize strips a reference the verifier asked for, record it as an orchestrator comment in the lessons instead of fighting the sanitizer.
- (code 7) Narrowed to the `IsLockedAsync` probe (`:140-150`); the collapse of negative `sp_getapplock` codes in `TryAcquireAsync` is #2667 and is cited as partially related. (tests 10) `LockAcquisitionException.cs` is left out because #2667 deletes it.
Applied: not applied: informational; draft verified in pass 3.
- (docs 4) The feature page `docs/features/distributed-locking.md` was removed from Expected Behavior and Proposed Fix because it is the deliverable of #908; this agent had to rewrite all five drafts of this run because none was on disk at the start.
Applied: role:remediation-drafter Before a -Only re-run, read the previous draft's lesson lines and input: the draft file is deleted by Prepare, so its shape survives only there.
- (pass 3 re-run, docs 4) The docs 4 draft was not on disk at the start and was rewritten: its Description lists every docs-site page that mentions locks in passing (the pages named in docs.md finding 4) and says none teaches the feature, instead of "the only mention". The three missing provider READMEs were checked with a count-mode Grep (0 `.md` files in each provider directory, with the core README as the positive control); `docs/tutorials` has 0 hits for "lock". The manifest carries `partiallyRelated: null` for docs 4, so #908 (the feature page) is cited by the instruction of the re-run and Finalize may strip it.
Applied: not applied: informational; draft verified in pass 4.
- (pass 3 re-run, code 9) The code 9 draft is a `[BUG]` and keeps only bug_report.md headers (the `## Proposed Fix` header is gone; its text is in Additional Context). Expected Behavior says "the error form decided for the lock providers" with no issue number; the manifest carries `partiallyRelated: null` for code 9, so no related issue other than #55 is cited. Redis and SQL Server behaviour for non-positive `expiry`/`wait` was not read from a run and the draft says so.
Applied: not applied: informational; draft verified in pass 4.
- (pass 4 re-run, docs 4) The docs 4 draft was not on disk at the start and was rewritten: the Description presents the pages as examples ("pages that mention locks in passing include ...") with no claim of being exhaustive, adds `docs/architecture/module-dependency-matrix.md:65` and `:92` and `docs/testing/testcontainers-direct-usage.md:22` and `:110-140` (all re-read in the worktree), and Current Behavior says "The pages listed in the Description are examples of passing mentions; none of them teaches locking." Absent-path claims were rechecked with Grep (0 hits for "lock" in `docs/tutorials` and `docs/features/index.md`; `docs/guides` hits are clock-drift text only; the only `README.md` under `src` that names DistributedLock is the core one).
Applied: not applied: informational; draft verified in pass 5.
- A negative sentence with a count ("the only guide hits are X") needs the same whole-word scan the stage ran for its other claims: a case-insensitive "lock" also matches "clock", and the one real "Lock" token (`AcquireLockAndClaimId`) is easy to lose behind them. Say "no guide teaches locking" instead of characterising every hit.
Applied: role:docs-reviewer Never characterise every hit with a count ("the only mentions are X"); say no page teaches the feature, or state the search scope.
- After a docs-only exhaustiveness fix, the closing set was: the one-line `--unified=0` diff of `docs.md`, the empty source diff, every cited line printed (including the unchanged ones), a whole-docs Grep for the feature token, the old-phrase grep over stages and drafts, the header/tick/milestone command over all 27 drafts, draft write-times versus the previous verification, and the created-since search; about ten calls.
Applied: role:audit-verifier After a docs-only exhaustiveness fix, the closing check set is about ten calls: one-line diff, empty source diff, every cited line, whole-docs grep, old-phrase grep, header/tick/milestone check, write times, created-since search.
