<!-- issue
title: [TEST] Stream/StreamDispatcher load-test justification, and unverified .NET 10 JIT bug citations left in CLAUDE.md and docs
labels: area-testing, area-streaming, ai:claude-required
milestone: v0.21.0 — Documentation
-->

Found by the SPEC-003 audit of #5.

## Test Category

- [ ] Unit Tests
- [ ] Integration Tests (Docker/Testcontainers)
- [ ] Property-Based Tests (FsCheck)
- [ ] Contract Tests
- [ ] Guard Clause Tests
- [x] Load Tests (NBomber)
- [ ] Benchmark Tests (BenchmarkDotNet)
- [ ] Coverage Gap (below 85% target)

## Description

Closed issue #5 ("[DEBT] Stream load tests cause CLR crash on .NET 10") skipped 8 load tests in `tests/Encina.Tests/LoadTests/StreamRequestLoadTests.cs` in 2025-12 because of a suspected .NET 10 JIT bug (Conditional Escape Analysis + `IAsyncEnumerable`/NBomber), and was closed as `COMPLETED` on 2025-12-30 citing `dotnet/runtime` issue #121736 and PR #121771 as the root cause and fix. CodeRabbit's bot reply on the issue ran three rounds of live web search and could not find either number publicly, and asked the author twice to confirm them; the closing comment repeated the same citation without addressing the flag.

Separately, the Jan 2026 test-consolidation refactor (commit `658263027956ba0614cc5e5daeb30cc3735975af`) deleted `tests/Encina.Tests/LoadTests/StreamRequestLoadTests.cs` entirely (480 lines, 0 replacement) while migrating most other `LoadTests` content into `tests/Encina.LoadTests/`. Unlike sibling features, which got a justification `.md` file when no `.cs` test exists (`Specification.md`, `Repository.md`, `ModuleIsolation.md`), Stream/`StreamDispatcher` load testing has neither a test file nor a justification file today. `src/Encina/Core/StreamDispatcher.cs` (the code originally under test) is unchanged in shape and still uses `Either.Map` inside `IAsyncEnumerable` — the same pattern the original crash stack trace named.

`CLAUDE.md:1386` currently reads: "Load tests are excluded from the standard CI pipeline because an upstream .NET 10 JIT bug (conditional escape analysis with complex `IAsyncEnumerable` code) crashes the runtime; the workaround `DOTNET_JitObjectStackAllocationConditionalEscape=0` applies when running them locally (project history: #5, #496)." This overgeneralizes: `*LoadTests*` projects are excluded from the standard CI pipeline as a blanket policy (long-running/perf tests), not specifically because of #5's JIT bug or #496's (unrelated, verified) MSBuild parallel-build crash. `docs/testing/load-tests-known-issues.md` documents the same workaround but lists only `Encina.FluentValidation.LoadTests` and `Encina.GuardClauses.LoadTests` as affected — the Stream case that originated this narrative for the repo is not mentioned anywhere on that page, and no script under `.github/` or `tools/` actually sets `DOTNET_JitObjectStackAllocationConditionalEscape=0` in CI.

## Packages / Providers Affected

- **Package(s)**: `Encina` (core, `StreamDispatcher`/stream request dispatching)
- **Provider(s)**: N/A (provider-agnostic core feature)

## Current Coverage

Not a percentage gap: there is currently zero load-test evidence (no `.cs`, no `.md`) for the Stream/`StreamDispatcher` feature under `tests/Encina.LoadTests/`.

## Infrastructure Required

- [ ] Docker / Testcontainers
- [ ] Real database
- [ ] Message broker
- [x] NBomber load testing framework
- [ ] BenchmarkDotNet
- [ ] None (pure unit tests)

## Test Plan

### Tests to Implement

- [ ] Decide and record whether Stream/`StreamDispatcher` load testing is reinstated (guarded behind a documented, CI-wired `DOTNET_JitObjectStackAllocationConditionalEscape=0` workaround if the JIT-bug class is confirmed to still apply on the pinned .NET 10 SDK) or formally waived with a `tests/Encina.LoadTests/Stream.md` (or `Core.md`) justification file following the required format in `CLAUDE.md`, "Test Justification Documents (.md)".
- [ ] If reinstated: recreate load tests equivalent to the deleted `StreamRequestLoadTests.cs` (8 scenarios) under `tests/Encina.LoadTests/`, following the current `[Collection]`/fixture conventions, and confirm they pass on the currently pinned .NET 10 SDK without the workaround before deciding whether the workaround is still needed.
- [ ] Strip or replace the unverified `dotnet/runtime` issue #121736 / PR #121771 citations wherever they are asserted as fact (repository history in `ROADMAP.md`'s past entries is not editable, but any current doc reasserting them must cite a verified source or drop the specific numbers and describe the symptom class only).
- [ ] Correct `CLAUDE.md:1386` so the LoadTests-excluded-from-CI sentence does not read as if #5's JIT bug (or #496's MSBuild crash) is the general reason `*LoadTests*` projects are excluded from CI; state the blanket exclusion policy and the narrow JIT-workaround note as two separate facts.
- [ ] Add the Stream case (or its resolution) to `docs/testing/load-tests-known-issues.md`'s "Affected Load Test Projects" list, or explicitly note it no longer applies if the tests are not reinstated.

### Success Criteria

- [ ] All new/updated tests pass
- [ ] Coverage meets the applicable per-flag target if load tests are reinstated (n/a otherwise, with the justification file in place)
- [ ] No flaky tests introduced
- [ ] Tests run within acceptable time limits
- [ ] `CLAUDE.md` and `docs/testing/load-tests-known-issues.md` no longer assert unverified third-party issue/PR numbers as fact, and no longer overgeneralize the CI-exclusion reason

## Collection Fixture (Integration Tests Only)

N/A — this is a load-test / documentation-accuracy gap, not a database integration test.

## Related Issues

- #5 - `[DEBT] Stream load tests cause CLR crash on .NET 10` (closed; this issue is the SPEC-003 audit follow-up)
- #496 - `[BUG] MSBuild CLR crashes (0x80131506) with parallel builds` (separate, verified root cause; miscited alongside #5 in `CLAUDE.md:1386`)
