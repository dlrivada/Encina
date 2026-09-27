# Stryker.NET and xUnit v3: status for Encina 1.0

**Status note.** First written 2026-09-06 as a chat answer; rewritten 2026-09-21 after verifying the upstream references and Encina's actual configuration; updated 2026-09-27 with the measured results of the #1087 spike (Stryker 5.0.0, both runners). Supersedes the earlier versions entirely.

## 1. Question

Has the Stryker.NET / xUnit v3 problem that limits Encina's mutation testing (issue #1026) been resolved upstream, and what does that mean for the 1.0 quality gate?

## 2. What Encina actually runs today

Verified on 2026-09-21 against the repository:

| Item | Value | Source |
|---|---|---|
| Stryker.NET version | **4.14.0**, pinned with `rollForward: false` | `.config/dotnet-tools.json` |
| Test runner | VsTest (default). `--test-runner mtp` is **not** used | `.github/workflows/mutation-tests.yml` |
| Coverage analysis | `off` (AllTests mode) | `.github/stryker-config.json` |
| Cost control | Per-folder `test-case-filter` patched into the config before each run (#1027), 17-shard GitHub Actions matrix with `fail-fast: false` (#1028) | `mutation-tests.yml` |
| Thresholds | `high: 80`, `low: 60`, **`break: 0`** — no run fails on score | `.github/stryker-config.json` |
| Project-wide target | None; results accumulate per file via `mutation-history.cs --merge-from` and are published at <https://dlrivada.github.io/Encina/mutations/> | `AGENTS.md` §9, `docs/testing/mutation-measurement-methodology.md` |

So the two concerns raised in the original note, "running Stryker over ~23,000 tests would be dangerous" and "a possibly wrong mutation score must not be a gate", are already addressed by the existing setup: sharding keeps each mutant to ~20–500 tests, and the score is informational only.

## 3. Upstream status (verified 2026-09-21)

| Reference | State | Relevance to Encina today |
|---|---|---|
| stryker-mutator/stryker-net#3117 — "Stryker.NET doesn't handle xUnit v3 properly" (`perTest` coverage broken, score collapses) | **Open** | **Yes, and confirmed by measurement.** The #1087 spike shows the VsTest runner kills 0 of 64 mutants on the pilot shard in both 4.14.0 and 5.0.0, and CI run 36304862790 shows 0 killed on all 17 shards of the current workflow. |
| stryker-mutator/stryker-net#3563 — immortal equality mutants with MTP + xUnit v3 (opened 2026-04-28) | Open upstream, **not reproduced** on Encina | The #1087 spike put 10 equality mutants in scope under the MTP runner with coverage off; all 10 were killed, 9 of them by a related test. No immortal equality mutant was observed (spot-checked two kills by hand). |
| stryker-mutator/stryker-net#3692 — hardcoded 3-minute MTP timeout kills discovery (opened 2026-07-06) | Open upstream, **not reproduced** on Encina in project mode | The #1087 spike measured discovery of 21,091 tests in 3-5 s and initial test runs of 3:56-5:30 (over 3 minutes) that were not killed by a timeout, when Stryker ran from `tests/Encina.UnitTests` (project mode). Solution mode is a different failure (see #3757 row and the spike's Finding 2), not this timeout. |
| stryker-mutator/stryker-net#3757 — MTP runner ignores `test-case-filter` | **Confirmed by measurement.** | The #1087 spike found 34 tests under VsTest with a given filter versus 21,091 (project mode) or 34,978 (solution mode) tests under MTP with the identical filter value; `Stryker.TestRunner.MicrosoftTestPlatform.dll` 5.0.0 contains a test-UID filter for per-test runs and no test-case-filter handling. Every shard would run the full test project per mutant, which the current sharding design depends on avoiding. |
| stryker-mutator/stryker-net#3727, #3754 (concurrency race; MTP coverage in multi-project solutions) | Reported in the 2026-09-06 note, **not re-verified** by the #1087 spike | Not measured; the spike focused on the five decision criteria in the issue, which did not require reproducing these two. |
| Stryker.NET releases 4.14.1 (2026-04-10), 4.14.2 (2026-05-17), 4.15.0 (2026-06-22), 4.16.0 (2026-07-03), **5.0.0 (2026-09-11)** | Released, **evaluated** | 5.0.0's `perTest` coverage analysis for the MTP runner does not complete on Encina's pilot shard: per-test coverage capture ran at about 150 tests/min with repeated 10-second relay-ack timeouts and was projected to take about 2 h 20 min per shard before any mutant is tested (stopped at the time box). No release note mentions an xUnit v3 fix, and none is needed for the VsTest runner outcome above. |

Conclusions, updated with the #1087 measurements:

1. #3117 is not just a documented open issue: it reproduces on Encina's exact test stack (xUnit v3 3.2.2, `xunit.runner.visualstudio` 3.1.5) under the VsTest runner, in both 4.14.0 and 5.0.0. Every mutation score the current workflow publishes is 0 % by construction, not a measure of test quality.
2. Stryker 5.0.0's MTP runner with `coverage-analysis: off` (not `perTest`) is the only configuration on the pilot shard that activates mutants at all — but it is 3.8x slower than the current VsTest run, ignores `test-case-filter` (so sharding by folder loses its purpose), and part of its measured kill count comes from Verify snapshot tests failing for reasons unrelated to the mutated code. It is not ready to adopt as-is.
3. `perTest` coverage — the mode that would make MTP both correct and fast — did not complete within the spike's time box on Encina's real test count (21,091 unit tests).

## 4. What this changes

The original note ended with "check which version Encina uses and how it is configured". That is now done (§2). The actionable experiment, tracked as **#1087**, is now done too: the full measurement — all 8 runs, the 7 findings and the decision-criteria table — is at [`stryker-5-mtp-spike-1087.md`](stryker-5-mtp-spike-1087.md). Its recommendation: none of options A (5.0.0 + MTP + `perTest`), B (5.0.0 + VsTest) or C (stay on 4.14.0) yields a valid mutation measurement today; #1026 stays open, and the workflow needs a runner switch (to MTP with coverage off, redesigning sharding) plus [a fix for the Verify snapshot tests](https://github.com/dlrivada/Encina/issues/1442) before mutation testing produces a trustworthy signal again.

Until that follow-up work lands, nothing about the current configuration should change: it is provably not measuring anything, but switching runners without the filter and [Verify fixes](https://github.com/dlrivada/Encina/issues/1442) would make the workflow slower without making it correct.

## 5. Mutation testing as a 1.0 requirement

The conclusion of `ENCINA-1.0-RECONCILIATION.md` §6.1 stands: **do not wait for upstream to "fix xUnit v3" before declaring 1.0.** The requirement, with its current status:

| Requirement | Status (2026-09-27) |
|---|---|
| A defined mutation-testing methodology | ✅ `docs/testing/mutation-measurement-methodology.md` (now with a caveat about the VsTest runner's zero-kill result) |
| Reproducible execution | ✅ Weekly matrix workflow + `run-stryker.cs` for local runs |
| Exact Stryker / .NET / xUnit versions recorded | ✅ Tool manifest pins 4.14.0; .NET/xUnit pinned via CPM; both 4.14.0 and 5.0.0 measured in #1087 |
| Results verified as plausible | ❌ Verified **implausible**: the #1087 spike and CI run 36304862790 show 0 killed on every shard under the current VsTest runner. No mutation score published today reflects test quality. |
| Known upstream limitations documented | ✅ `AGENTS.md` §9, methodology doc, #1026, this note, [`stryker-5-mtp-spike-1087.md`](stryker-5-mtp-spike-1087.md) |
| No potentially incorrect mutation score used as a quality gate | ✅ `break: 0`, no project-wide target |
| Re-evaluate the gate when upstream is stable | ✅ Done: #1087 measured 5.0.0's VsTest and MTP runners; see the spike page for the decision. #1026 stays open. |

For `SPEC-000` the wording should be:

> Mutation testing is part of the quality process and is reproducible, but its published scores are not currently a signal of test quality: the VsTest runner kills 0 mutants under xUnit v3 in both Stryker 4.14.0 and 5.0.0 (measured in #1087 and in CI run 36304862790, 0 killed on all 17 shards). No mutation-score threshold is a release blocker while this holds. The workflow needs a runner migration (to the MTP runner with coverage off, per the #1087 spike) and [a fix for the Verify snapshot tests](https://github.com/dlrivada/Encina/issues/1442) before scores become trustworthy again.

## 6. Result of the 5.0.0 evaluation (#1087)

The full measurement is at [`stryker-5-mtp-spike-1087.md`](stryker-5-mtp-spike-1087.md). Summary of what the earlier "risks if we migrate" section anticipated, now replaced by measured outcomes:

- `rollForward: false` still means any bump is explicit and reversible; keep it that way. No bump has been made to the tracked `.config/dotnet-tools.json`.
- #3692 (3-minute MTP discovery timeout): **did not reproduce** in project mode on `Encina.UnitTests` — discovery took 3-5 s and initial test runs of 3:56-5:30 completed without being killed by a timeout.
- #3563 (immortal equality mutants): **did not reproduce** — 10/10 equality mutants in scope were killed under the MTP runner with coverage off, 9 of them by a related test.
- #3757 (MTP runner ignores `test-case-filter`): **confirmed**. Every shard would run the full `Encina.UnitTests` project (21,091 tests) per mutant instead of the ~20-500 the current filter selects, making the matrix unaffordable as designed.
- New finding, not anticipated: `coverage-analysis: perTest` — the mode that would make the MTP runner both correct and fast — did not complete a shard within the spike's time box; capture alone was projected at about 2 h 20 min per shard.
- New finding, not anticipated: the MTP runner with coverage off does kill mutants, but 21 of 63 kills on the pilot shard came only from Verify snapshot tests failing in the reused test server, unrelated to the mutated code, inflating the reported score from a credible 65.6 % to 98.44 %.
