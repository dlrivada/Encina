## Scope

Issue #58, closed COMPLETED 2025-12-27, no PR; closed by commit 3bccb573680153a3691ba44c63a37f5241d75b3e (`git show --stat`, 17 files). Source files, all still present in `src/Encina/Modules/` (verified):
- `IModulePipelineBehavior.cs`, `ModuleBehaviorAdapter.cs` (touched later by #2184 Phase 2 commit 5ca0b8a1f)
- `IModuleHandlerRegistry.cs`, `ModuleHandlerRegistry.cs` (later Sonar/assembly-scanning fixes)
- `RequestContextModuleExtensions.cs`, `ModuleBehaviorServiceCollectionExtensions.cs`
- Also touched by the commit: `src/Encina/Core/ServiceCollectionExtensions.cs`, `src/Encina/Dispatchers/MediatorAssemblyScanner.cs`, `src/Encina/Modules/ModuleServiceCollectionExtensions.cs`, `src/Encina/PublicAPI.Unshipped.txt`.

Tests: the four test files added by the commit (`tests/Encina.Tests/Modules/ModuleBehaviorAdapterTests.cs`, `ModuleBehaviorServiceCollectionExtensionsTests.cs`, `ModuleHandlerRegistryTests.cs`, `RequestContextModuleExtensionsTests.cs`) were deleted in commit 658263027 ("Consolidate tests"). Today only `tests/Encina.GuardTests/Core/Modules/ModuleHandlerRegistryGuardTests.cs`, `tests/Encina.UnitTests/Core/Modules/NullModuleHandlerRegistryTests.cs` and `tests/Encina.UnitTests/Pipeline/Ordering/ModuleBehaviorAdapterStepTests.cs` (plus `PipelineOrderResolverTests.cs`, `PipelineOrderingGuardTests.cs`) reference these types. No current test references `RequestContextModuleExtensions` or `AddEncinaModuleBehavior` by grep. Whether the 100 tests were migrated is for the test auditor to establish (not proven either way).

## Destinations

- Decisions (behavior interface, adapter filtering, registry, request-context extensions, DI extension): `CHANGELOG.md` line 8889 ("Module-scoped Pipeline Behaviors (Issue #58)") present; `ROADMAP.md` line 329 present; `docs/releases/pre-v0.10.0/README.md` mentions it (line 2368/2394). No ADR names these types (searched `IModulePipelineBehavior`, `AddEncinaModuleBehavior` under docs/: only plan 2184, release pages and audit-57 files). No guide page documents `AddEncinaModuleBehavior` (missing as user documentation).
- Acceptance items "override mechanism" and "no performance impact on non-module handlers": the closing comment does not claim them; no destination found. Recorded as an unknown pending item.
- Pre-draft corrections: closed_at normalised to ISO; the pre-draft's spec-invariant and regression-test destinations were not kept (no SPEC invariant exists, the cited 100 tests are gone); "rules" about no-performance-impact were an acceptance criterion, not a delivered fact.

## Successor and duplicate issues

Not a rejected/duplicate issue. Prerequisite #57 (IModule interface and registry) is CLOSED (`gh issue view 57`). Later related work: #2184 (pipeline execution order) builds on `ModuleBehaviorAdapter` (not verified open/closed here; not needed for the outcome).

## Lessons for the pipeline

- The original tests named in a commit message ("100 module-related unit tests") may have been deleted by a later consolidation; check `git log --diff-filter=D` for each test file in the commit stat and list what exists today.
- The pre-draft lives only in the main checkout's artifacts folder; the worktree has none.

## Re-run note (verifier correction 4)

Record decision 2 was split: ownership by assembly through IModuleHandlerRegistry stays current: yes; the claim that the module name travels in the request context (GetModuleName, WithModuleName, IsInModule) is now its own item with current: no, because a search of src finds no caller of WithModuleName (the key is never set). knowledge-records --check passes.
- A statement that bundles a live part and a never-wired part must be split; check callers of each API named in a decision before marking current: yes.
