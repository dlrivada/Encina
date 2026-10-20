Verdict: PASS
## Verified claims
Method (pass 2, narrow): `git diff --stat bc1de80a HEAD -- src tests docs .github` is empty (HEAD d4ce23c8 on `audit/31`), so every pass-1 measurement and every pass-1 confirmed claim carries forward (21 findings: 10 code + 4 tests + 7 docs; 21 disposition lines in `stages/remediation.md`; 17 draft files `31-*.md` counted by command in `D:\Proyectos\Encina\artifacts\knowledge\remediation`; 4 duplicate lines code 9, code 10, tests 4, docs 7). Only the corrected sentences and the five regenerated drafts were re-read, each at its cited source line.

Old-phrase grep (`ci.yml:276`, `exactly these four`, `== 'all' can never`, `after the close`, `cannot even`, `#2000`) over the stage files and all 17 drafts:
- Hits remain only in `verification.md` (the pass-1 FAIL text, now replaced by this file) and in `remediation.md` lessons that quote the old wording as history (lines 26-29). No stage finding and no draft body carries any of them.
- One residue, not blocking: `docs.md:34` (a lesson) still says "four hits in two files" for `.github/ci/CD`. Finding 5 in `docs.md:20` states the corrected count and says the four-hit count came from a glob list; the lesson line is history.

Archivist (correction 1): `archivist.md` Scope now reads "2025-12-24 18:42:25 +0100 = 17:42:25Z, 14 minutes before the close at 17:56:25Z and the owner comment at 17:56:24Z". Matches my pass-1 `git log` and `gh issue view` check.

Code (corrections 2a, 2b, 2c), each re-read at the source:
- Finding 1: `ci.yml:194, 268, 307, 346, 385, 495` all print "# Test failures MUST fail the CI — no continue-on-error" (6 of 6); `:276` is no longer cited.
- Finding 4: `load-tests.yml:650` is `if: matrix.provider == 'kafka' || matrix.provider == 'all'`, `:653` is the `echo`; the stage now says the step runs for Kafka and only the `'all'` half is dead (`:590` expands `all` into concrete providers). The line list now includes `:396-405` (caching Redis service) and `:413` (`REDIS_CONNECTION_STRING`), both confirmed.
- Finding 5: options at `:37-39`, `:47-49`, `:57-59`, `:67-71`; matrix fallbacks at `:165, :395, :486, :590`; "3 of 10 selectable (all EF Core), other 7 (ADO.NET x3, Dapper x3, MongoDB)" is true; `DatabaseProviderRegistry.cs:46-61` registers ado x3, dapper x3, efcore x3 and mongodb; harness provider factories exist for exactly 3 caches (Memory, Hybrid, Redis), 3 locks (InMemory, Redis, SqlServer) and 4 brokers (Kafka, MQTT, NATS, RabbitMQ). `check-load-metrics.cs:117` is `Environment.Exit(1)`; `Program.cs:173` writes `artifacts/load-metrics/nbomber-<ts>`.

Docs (corrections 3a, 3b):
- Finding 5: Grep over the whole worktree for `\.github/ci/CD` in `*.md` returns 8 lines in 3 files: `load-test-baselines.md:175, 449, 664`; `tests/Encina.NBomber/README.md:370`; `docs/releases/v0.11.0/README.md:1056, 1064, 3164, 3179`. Matches the finding and the draft. `v0.11.0/README.md:9` is the Historical Note on file paths.
- Finding 2 now carries `README.md:284-301` (the export block; line 284 "Alternatively, set environment variables to use existing services") and `:309` ("Results are written to `artifacts/nbomber/`"), against `Program.cs:173`. Confirmed.

Regenerated drafts (code 1, code 4, code 5, docs 2, docs 5; write-times 17:30): prose re-read in full against the sources above. Headers compared by command with `bug_report.md` (code 1) and `technical_debt.md` (the other four): verbatim and in order, printed side by side. Every ticked box (11 across the four debt drafts) equals a real template option (`-ceq` on the option text); code 1 keeps milestone `v0.14.0 — Hardening`, the other four have an empty milestone; no emoji. The other 12 drafts are older than the regeneration and unchanged (write-times 17:03-17:12).

Duplicates and issue states (re-run `gh issue view` today): #1682, #1228, #2000, #1863, #1760 all OPEN. No new issue created since 2026-10-09 (#2016-#2045 listed) names `load-tests.yml`, `benchmarks.yml`, `continue-on-error`, `packages.lock.json` or the NBomber README; no open issue titled for the benchmarks.yml dead step or the load-tests `continue-on-error` exists, so code 1-8 stay as drafts. The overrides (code 7/8 not duplicates; code 9, tests 4 -> #1682; code 10, docs 7 -> #1228) were confirmed from the issue bodies in pass 1 and nothing changed.

Disposition of correction 4 (the false "#2000 - partially related" line, `31-code-7-...:66` and `31-code-8-...:68`): acceptable. The line is a manifest artifact, `-Finalize` has no override to drop it (gap #1863, whose title covers `-Finalize` leaving text when it strips a reference), and the two drafts are not yet opened as issues (no issue exists). The line is the only inaccurate item in those drafts (the rest of their Related Issues I checked in pass 1). Conditions the orchestrator must meet: remove that line from the code 7 and code 8 bodies before or when opening them (a leftover would tell the maintainer that #2000 covers part of the dead step or the injection shape, which it does not), and post the two comments below.

Comments handed to the orchestrator (not corrections, per the agreed channel): on #1682, the merge-step regression test (extraction of `mutation-tests.yml:516-575` with a `--self-test`) with cases (d) duplicate files across shards and (e) merged-count mismatch, which #1682 does not name; on #1228, the NOASSERTION evidence from run 35736044294 (14 of 14 packages, 0 checksums) and that `docs/RELEASE-PROCESS.md` (`:168, :176`) gets the SBOM check once #1228 changes what the SBOM step produces.

## Corrections
(empty)

## Lessons for the pipeline
- Correction 4 of this audit shows a gap: `-Finalize` prints a "partially related" line from the manifest even when the orchestrator rejected the relation, and no override removes it (#1863). Until then the orchestrator strips the line when opening the issue; a note in the opening checklist for drafts with a rejected relation would make that step reliable.
- A stage's lessons are not rewritten by a re-run, so an old count survives in them (`docs.md:34` "four hits in two files") next to the corrected finding; a verifier greps the lessons too and treats them as history when the finding is right.
- Pass 2 after a regeneration with `-Only` took: the old-phrase grep over stages and drafts, a source print for each changed citation (ci.yml comment lines, the `if:` condition, the options lists, the registry, the factory file names), a whole-worktree Grep for the repeated token, and a side-by-side header and ticked-box command; measurements carried forward because the diff over `src tests docs .github` was empty.
