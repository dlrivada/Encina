# Stryker.NET and xUnit v3: status for Encina 1.0

**Status note.** First written 2026-09-06 as a chat answer; rewritten 2026-09-21 after verifying the upstream references and Encina's actual configuration. Supersedes the earlier version entirely.

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
| Project-wide target | None; results accumulate per file via `mutation-history.cs --merge-from` and are published at <https://dlrivada.github.io/Encina/mutations/> | `CLAUDE.md`, `docs/testing/mutation-measurement-methodology.md` |

So the two concerns raised in the original note, "running Stryker over ~23,000 tests would be dangerous" and "a possibly wrong mutation score must not be a gate", are already addressed by the existing setup: sharding keeps each mutant to ~20–500 tests, and the score is informational only.

## 3. Upstream status (verified 2026-09-21)

| Reference | State | Relevance to Encina today |
|---|---|---|
| stryker-mutator/stryker-net#3117 — "Stryker.NET doesn't handle xUnit v3 properly" (`perTest` coverage broken, score collapses) | **Open** | **Yes.** This is the constraint behind #1026 and the reason `coverage-analysis` is `off`. |
| stryker-mutator/stryker-net#3563 — immortal equality mutants with MTP + xUnit v3 (opened 2026-04-28) | Open | Not today: only affects `--test-runner mtp`. |
| stryker-mutator/stryker-net#3692 — hardcoded 3-minute MTP timeout kills discovery (opened 2026-07-06) | Open | Not today: MTP-only. Would matter for large shards after a migration. |
| stryker-mutator/stryker-net#3727, #3754, #3757 (concurrency race; MTP coverage in multi-project solutions; test selection silently ignored by MTP runner) | Reported in the 2026-09-06 note, **not re-verified** on 2026-09-21 | #3757 would matter after migration because the sharding depends on `test-case-filter` being honoured. |
| Stryker.NET releases 4.14.1 (2026-04-10), 4.14.2 (2026-05-17), 4.15.0 (2026-06-22), 4.16.0 (2026-07-03), **5.0.0 (2026-09-11)** | Released | 5.0.0 notes include **"Support `perTest` and `perTestInIsolation` coverage analysis"** for the MTP runner. No release note mentions an xUnit v3 fix. |

Two conclusions follow:

1. The MTP-runner bugs collected in the original note **do not apply to Encina's current configuration**. Only #3117 does.
2. Stryker 5.0.0 is the first upstream change that could lift the #1026 constraint, because `perTest` coverage is exactly what Encina had to switch off. Whether it works with xUnit v3 is unknown until tried; #3117 is still open.

## 4. What this changes

The original note ended with "check which version Encina uses and how it is configured". That is now done (§2). The actionable next step is an experiment, tracked as **#1087**: bump to 5.0.0 on a branch, run **one** small shard with `--test-runner mtp` and `coverage-analysis: perTest`, and compare mutant counts, killed/survived and wall time against the same shard on 4.14.0. Decision criteria (plausible kill counts, no discovery timeouts, `test-case-filter` honoured, report shape still accepted by `mutation-history.cs`) are in the issue.

Until that experiment produces evidence, nothing about the current configuration should change.

## 5. Mutation testing as a 1.0 requirement

The conclusion of `ENCINA-1.0-RECONCILIATION.md` §6.1 stands: **do not wait for upstream to "fix xUnit v3" before declaring 1.0.** The requirement, with its current status:

| Requirement | Status (2026-09-21) |
|---|---|
| A defined mutation-testing methodology | ✅ `docs/testing/mutation-measurement-methodology.md` |
| Reproducible execution | ✅ Weekly matrix workflow + `run-stryker.cs` for local runs |
| Exact Stryker / .NET / xUnit versions recorded | ✅ Tool manifest pins 4.14.0; .NET/xUnit pinned via CPM |
| Results verified as plausible | 🟡 Per-file accumulation exists; no explicit plausibility check (e.g. flagging shards with 0 killed) |
| Known upstream limitations documented | ✅ `CLAUDE.md`, methodology doc, #1026, this note |
| No potentially incorrect mutation score used as a quality gate | ✅ `break: 0`, no project-wide target |
| Re-evaluate the gate when upstream is stable | 🟡 Opened as #1087 |

For `SPEC-000` the wording should be:

> Mutation testing is part of the quality process, is reproducible, and its results are published per file with the exact tool versions recorded. No mutation-score threshold is a release blocker while Stryker.NET / MTP / xUnit v3 integration produces unreliable results. The gate is re-evaluated when #1087 provides evidence.

## 6. Risks if we migrate to 5.0.0

- `rollForward: false` means the bump is explicit and reversible, which is good; keep it that way.
- #3692: a shard whose filtered test set takes more than 3 minutes to discover/run will abort. The largest folders may need to be split further or stay on VsTest.
- #3563: immortal mutants would deflate scores silently. The plausibility check in §5 (row 4) becomes mandatory, not optional, before trusting any 5.0.0 result.
- #3757: if the MTP runner ignores `test-case-filter`, every mutant runs the full ~23,000 tests again and the matrix becomes unaffordable. Verify on the pilot shard by inspecting the executed-test count in the log.
