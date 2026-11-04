## Scope

Closing commit c26725b (no PR; timeline `referenced` event, `closed` has no commit_id). Today:
- `src/Encina.AzureFunctions/Durable/`: ActivityResult.cs, DurableFunctionsHealthCheck.cs, DurableFunctionsOptions.cs, DurableSagaBuilder.cs, DurableServiceCollectionExtensions.cs (`AddEncinaDurableFunctions`), FanOutFanInExtensions.cs, OrchestrationContextExtensions.cs; plus DurableSagaErrorCodes.cs, added later. Retry defaults exist (`DurableFunctionsOptions.DefaultFirstRetryInterval`, `TaskOptions` in the saga builder).
- `src/Encina.AzureFunctions/README.md` (Durable section, line 317), `PublicAPI.Unshipped.txt`, `GlobalSuppressions.cs`, `.github/coverage-manifest/Encina.AzureFunctions.json`.
- Tests that exist: `tests/Encina.UnitTests/AzureFunctions/Durable/` (ActivityResult, HealthCheck, Options, SagaBuilder, ServiceCollectionExtensions, FanOutFanInExtensions, OrchestrationContextExtensions), `tests/Encina.GuardTests/Cloud/AzureFunctions/DurableGuardTests.cs`.
- Deleted by consolidation 658263027: `tests/Encina.AzureFunctions.{ContractTests,PropertyTests,GuardTests}/Durable/*` (contract, property, guard). Contract and property tests were not replaced.
- No code was removed on purpose. The package does not claim Oracle/SQLite.

## Destinations

- Package README Durable section: present (src/Encina.AzureFunctions/README.md:317).
- ActivityResult regression test: present (`ActivityResultTests.cs`).
- Integration tests with a Durable host (body task and Test Matrix row): missing; tracked by open #3114 and #523.
- Contract tests: missing; open #2961. Unit flag below target: open #2787.
- Not recorded: pre-draft "spec-invariant" destination and "Rules and lessons" (no quotable source).
- Packages: pre-draft said `Encina.AzureFunctions`; verified to exist.

## Successor and duplicate issues

No successor or duplicate. Related open work (states from `gh issue list --state all` search, #523 and #3114 and #2961 confirmed with `gh api` as open): #523 [DEBT] AzureFunctions integration tests for Durable Functions extensions, #3114 [TEST] Durable orchestrations on a local host with Azurite, #2961 [TEST] AzureFunctions contract tests deleted by consolidation, #2787 [TEST] unit flag far below target for the Durable helpers, #2950 [DEBT] metadata/README claim Queue and Timer triggers without code (about #59 scope, same package). All OPEN, so that work is pending.

## Lessons for the pipeline

- The pre-draft had `closed_at: 12/27/2025`, `linked_prs: []` and read the close comment's "228 tests" as current; the contract, property and old guard test files were deleted by consolidation 658263027. Count tests from the tree, never from the close comment.
- The wia worktree had no predraft; it was read from the main checkout.
