Verdict: PASS
## Verified claims

Delta `rules-2026-10`, rules (a) and (b), verification pass 3 after the pass-2 FAIL (one correction). Base for this pass: pass-2 commit `8ce5f4fe` (an ancestor of HEAD, checked with `git merge-base --is-ancestor`); HEAD `7d23b46a` ("audit #18: tests stage").

Scope of the re-run, measured by command:
- `git diff --stat 8ce5f4fe HEAD`: only `artifacts/knowledge/stages/tests.md` (1 line) and `artifacts/knowledge/stages/.authors.json` (the tests stage's timestamp). `git diff --stat 8ce5f4fe HEAD -- src tests docs .github` is empty.
- `git diff --unified=0`: the only change in `tests.md` is line 225, "tolerates losing five of the 102 covered lines" became "tolerates losing seven of the 102 covered lines". Nothing else changed in that file; the diff holds no appended lesson in `tests.md` (the lesson goes to the agent lessons file, outside this worktree), which is not a defect.
- Arithmetic: 146 x 0.65 = 94.9, ceil 95 (95/146 = 65.07%); 102 - 95 = 7. The sentence "(95 of 146 suffice)" now agrees with "seven".
- Grep for `losing five` over all stage files and the 16 drafts `18-delta-2026-10-*.md` in the main checkout: 0 hits. Grep for `losing seven`: hit at `tests.md:225`. The other 13 "tolerates losing" sentences (lines 125, 142, 159, 173, 174, 189, 202, 203, 240, 241, 255, 256, 289) are unchanged in the diff and were recomputed correctly in pass 2.
- Drafts untouched: 16 `18-delta-2026-10-*.md` files (7 docs, 9 tests) in the main checkout, write times 13:41:26 to 13:46:59, all before the pass-2 verification commit (15:11 +02:00 = 13:11 UTC is the commit instant of the tests-stage timestamp; the drafts were regenerated before pass 2 and the diff shows no remediation change since). No draft repeats the corrected sentence.

Carried forward unchanged from pass 2 (and through it pass 1), because only a stage artifact changed and `src/`, `tests/`, `docs/`, `.github/` did not: every measurement (unit 630, guard 129, contract 16, property 6, integration 13 passed; per-file covered/coverable and missed lists for the ten packages), every `file:line` citation and range printed against its file in docs.md and tests.md, the symbol checks, the issue states (#1479, #1611, #1615, #1856, #1847, #1886, #1882, #1619, #1622, #1627 OPEN; #1887 not cited as a duplicate), the #1847 amendment comment against the tests.md tables, the #1622 and #1627 retarget comments, the duplicate search over the 16 drafts including the issues created since 2026-10-06, the docs rule (a) findings, the rule (b) findings and proposed targets against the manifests, the remediation checks (20 lines: 16 drafts plus docs 3, tests 8, docs 9, docs 10 as duplicates; kinds and titles; template headers and ticked options compared by command; empty milestones; each finding exactly once).

Not re-verified (as in passes 1 and 2): the code-reading ceilings labelled "derived"; CRAP is pending #1346.

## Corrections

None.

## Lessons for the pipeline
- A narrow FAIL on one arithmetic word closed with four checks: `git diff --unified=0` between the previous verification commit and HEAD showing a single changed line, the recomputed arithmetic, a grep of the old phrase over the stage files and drafts, and the draft write-times; carrying the measurements forward was safe because `src`, `tests`, `docs` and `.github` were empty in the diff.
