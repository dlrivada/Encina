## Scope
Issue #49 (REFACTOR, closed 2025-12-26, no PR). Closing commit bcd86ef7c (verified `git show --stat`; 11:14:45Z, 18 s before closedAt 11:15:03Z). Today:
- `src/Encina/Dispatchers/Encina.RequestDispatcher.cs`: `RequestHandlerCache` TryGetValue then GetOrAdd (l.65-67), `RequestKindCache` ConcurrentDictionary in `GetRequestKind` (l.39, 186-199).
- `src/Encina/Dispatchers/Encina.NotificationDispatcher.cs`: TryGetValue before GetOrAdd on `NotificationHandlerInvokerCache` (l.214-249). Both files have later commits (#1319, #1147, #1557 etc.) that code-stage must account for.
- `tests/Encina.BenchmarkTests/Encina.Benchmarks/CacheOptimizationBenchmarks.cs`: moved from `benchmarks/Encina.Benchmarks/` (commit 658263027 consolidation). `MediatorBenchmarks.cs` also touched by the commit.
- No `CollectionsMarshal` or `FrozenDictionary` in `src/Encina/Dispatchers`. No unit test references `GetRequestKind`/`RequestKindCache` by name (only the benchmark) - test-auditor to check dispatcher behavior coverage.
- Commit 25d23fb8 (docs) is mostly unrelated Recoverability code; its #49 content is CHANGELOG.md (l.9556, 9611), ROADMAP.md (l.326) and `docs/history/2025-12.md` (deleted later; not a regression of this issue).
Outcome `partial`: body tasks "profile baseline", "boxing hotspots", "CollectionsMarshal.AsSpan" were not ticked and have no trace in code; the "Measurable improvement" acceptance criterion has no stored before/after result.

## Destinations
- TryGetValue-before-GetOrAdd: code present in both dispatchers; benchmark present; CHANGELOG.md present. No reviewer-checklist/rule exists (pre-draft's "reviewer-checklist" candidate dropped: no source states it).
- RequestKind cache: present in code and benchmark.
- FrozenDictionary rejection: no destination (current unknown).
- Undelivered tasks: no tracking issue found; #50 is the source-generator successor, not a tracker for these.
- Pre-draft corrections: closed_at normalised to 2025-12-26, `linked_prs` empty (no PR), "Rules and lessons" invented rule on micro-benchmarks dropped, "reviewer-checklist" destination dropped.

## Successor and duplicate issues
- #50 "[FEATURE] Source Generators - zero-reflection, NativeAOT ready": state CLOSED today (verified `gh issue view 50`); the close comment's "next step". Its delivery was not examined here.
- #1716 (OPEN, DEBT) says ROADMAP lists #50 and #51 under completed items; related to ROADMAP line 326 accuracy, not to #49's own delivery.

## Lessons for the pipeline
- The pre-draft read the close comment's three optimizations as full delivery; comparing the body's six Tasks with the commit file list and `src/` (no CollectionsMarshal, no profiling) showed `partial`.
- The worktree had no `artifacts/knowledge/predraft/49.md`; it was read from the main checkout.
