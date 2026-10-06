- A README sample that combines two packages must be checked against both registration methods: the `TryAdd*` calls make the second registration a silent no-op, and no symbol-existence check shows it. Read the registration bodies of every package a "combine" sample names.
Applied: role:docs-reviewer Check a README sample that combines two packages against both registration methods; TryAdd makes the second a silent no-op.
- Before writing a finding, search the open umbrella issue for the same README by file path and line range; here four of five findings were already in #1850 and #1330, so the stage's value was the one new defect and the duplicate mapping.
Applied: role:docs-reviewer Before writing a finding, search the open umbrella issues for the same file path and line range.
- Before turning a "raise the target" proposal into a finding for a file with very few coverable lines, check whether the current target already demands the same: with 5 coverable lines, property 90 and property 100 are the same gate (4/5 = 80%), so the proposal is cosmetic and says so.
Applied: role:test-auditor Before proposing a higher target on a file with very few coverable lines, check whether the current target already demands the same lines.
- When a package-level target is a hand-typed aggregate, compute its ceiling from the lines the flag can reach (here 3 of 27 for guard = 11.11%) before accepting that a remediation issue's success criterion ("coverage meets the guard target") is attainable; the per-file proposals of #1850 and the package target 25 were never reconciled.
Applied: role:test-auditor Compute a package-level aggregate target's ceiling from the lines the flag can reach before accepting it.
- A delta brief that names an already-open consolidated issue for the same files (#1850) saves a duplicate analysis, but the issue was written while PR #1826 was still open; the stage has to re-read the merged manifest and restate which of the issue's numbers are now stale (here none are wrong, the manifest holds 90/90/0 and the issue proposes 100/100/9).
Applied: role:test-auditor Re-read an umbrella issue written before a schema change (#1826) against the current manifest before marking a finding duplicate.
- A test whose name says it checks propagation can execute the lines it names and still assert nothing about them; for a property target the stage should say "executed, not asserted" next to the 100% when the assertion survives deletion of the line (lines 36 and 41 of the provider).
Applied: role:test-auditor Say "executed, not asserted" next to a coverage figure when the tests that execute the lines do not assert on them.
- `Encina.ContractTests` does not reference the validation provider packages, so the contract flag is not measurable for them at all; the stage definition could list per-package which test projects reference the package before the coverage runs.
Applied: not applied: tooling knowledge (which test projects reference which packages) belongs to the coverage manifests, noted for #1825.
- docs 2: recorded as duplicate of #1330 by manual override
Applied: not applied: record of a manual duplicate decision (#1330).
- docs 3: recorded as duplicate of #1850 by manual override
Applied: not applied: record of a manual duplicate decision (#1850).
- docs 4: recorded as duplicate of #1850 by manual override
Applied: not applied: record of a manual duplicate decision (#1850).
- docs 5: recorded as duplicate of #1850 by manual override
Applied: not applied: record of a manual duplicate decision (#1850).
- tests 1: recorded as duplicate of #1850 by manual override
Applied: not applied: record of a manual duplicate decision (#1850).
- tests 2: recorded as duplicate of #1850 by manual override
Applied: not applied: record of a manual duplicate decision (#1850).
- The manifest's `routes.docs` carries the label `technical-debt` only, while the role definition says a docs-stage draft uses `area-documentation`; the docs 1 draft follows the manifest. (Align the route's labels with the routing section of the drafter definition.)
Applied: not applied: label routing mismatch between the manifest and the role definition; added to #1863.
- The tests stage lists the 27 guard-coverable lines and the three reachable lines (provider `:27`, `:28`, registration `:69`) without naming them; the drafts verified the three `ThrowIfNull` lines in `src/` but the 27-line total is the stage's measured figure (22 + 5), not re-measured here. (State the line numbers behind a coverable-lines figure.)
Applied: role:test-auditor Name each reachable line (file:line) when stating a flag ceiling, not only the count.
- Docs 1 says the README line is repeated in the FluentValidation README only if it carries the same sample and reports 0 hits; the draft states the verified result (no "Combining" or `AddDataAnnotationsValidation` there). (Phrase a scope check as its result, not as a conditional.)
Applied: role:remediation-drafter State the verified result of a cross-package check (hits or 0 hits with the command), not the conditional wording of the finding.
- When `-Finalize` strips an issue number from the middle of a sentence it leaves "of: 8 findings"; the verifier can only report it (open #1863), so the orchestrator should fix the wording when opening the issue; the drafter could name the audited issue in a form `-Finalize` keeps.
Applied: not applied: tooling defect #1863; the orchestrator fixed the wording before opening the issue.
- A "ceiling" claim for a flag target (here guard 3/27) holds for null-check guard tests; a guard test that calls the method with valid arguments would reach more lines, so such a draft should say "null-check guard tests" rather than "a guard test", which the tests 3 draft only partly does. The proposed floor of 11 is still correct.
Applied: role:test-auditor Qualify a guard ceiling as "for null-check guard tests"; a guard test with valid arguments reaches more lines.
- For a README "combine two packages" sample, the verifier should read both registration bodies (here `TryAdd*` at `:72-74` and `:62-64`) and the consumer's constructor, not only the symbol names; that is the only way to see that the combination is a silent no-op.
Applied: role:audit-verifier For a "combine two packages" README claim, read both registration bodies and the consumer's constructor, not only the symbol names.
- Re-measuring three filtered flag runs (unit, property, guard) on an already-built worktree took about four minutes in the foreground; the hook blocks `run_in_background` for verifiers, so run each with a 10-minute timeout.
Applied: not applied: timing note, no rule; verifiers already run measurements in the foreground.
