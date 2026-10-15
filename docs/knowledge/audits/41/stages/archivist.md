## Scope
Issue #41 (created 2025-12-24T13:25:04Z, closed 2025-12-26T15:05:22Z, COMPLETED, no PR; closed by commit c13aee3c, `Fixes #41`, verified with `git show --stat`). All paths verified to exist today in `Encina.Messaging`:
- `src/Encina.Messaging/Sagas/LowCeremony/SagaDefinition.cs`, `SagaStepBuilder.cs`, `ISagaRunner.cs`, `SagaRunner.cs` (SagaRunner later changed by #1259, #1579 `a13d19be`, #2185 `328c5df2`, #2202 `576abb4c`: encryption, code-only exception logging, Compensated/Failed end state, DLQ capture).
- `src/Encina.Messaging/MessagingServiceCollectionExtensions.cs` (ISagaRunner registration, lines ~201 and ~313) and `src/Encina.Messaging/PublicAPI.Unshipped.txt` entries from the commit.
- Tests today: `tests/Encina.UnitTests/Messaging/Sagas/SagaRunnerTests.cs`, `.../Sagas/LowCeremony/SagaDefinitionTests.cs`, `tests/Encina.GuardTests/Messaging/Sagas/SagaRunnerGuardTests.cs`, `tests/Encina.IntegrationTests/Infrastructure/Sagas/SagaRunnerOutcomeScenarios.cs`. `SagaStepBuilderTests.cs`, `LowCeremonySagaPropertyTests.cs` and `ISagaRunnerContractTests.cs` from the commit do not exist at those paths; no property or contract test now references SagaDefinition/ISagaRunner (grep of tests/), so the claimed 11 property + 8 contract tests are gone or moved (for the test auditor).
- Docs: `docs/sharding/cross-shard-operations.md` uses `SagaDefinition.Create`; no `docs/messaging` or `docs/en` page documents the low-ceremony API.
- Pre-draft listed `docs/history/2025-12.md`; it was deleted (commit d098e542, repo-wide refactor), not a regression of this issue.

## Destinations
- Fluent definition/runner decision: present in code (four LowCeremony files) and CHANGELOG.md lines ~9396-9414 ("Low-Ceremony Sagas (Issue #41)"). No ADR: no ADR in docs/architecture/adr is named saga/messaging for this API (file-name check only).
- Reverse-order compensation rule: present in `SagaRunnerTests.cs` / `SagaDefinitionTests.cs`.
- Pre-draft "spec-invariant" candidate dropped: no source states it.
- Issue-body items `[Saga]` attribute, `SagaStep` static builder, convention-based routing, inline state: missing from src/ (no `[Saga]` attribute in LowCeremony, none in the commit file list); the closing comment does not list them. Recorded as `current: no`. Docs for the API: missing (only the sharding page uses it).
- Pre-draft open questions: 85% coverage per-flag is a test-auditor item; integration with `SagaOrchestrator` (separate class, not shared) left for the code auditor.

## Successor and duplicate issues
None. The issue is delivered, not rejected or duplicate; no open successor for convention-based routing was found in the timeline (only the closing commit reference).

## Lessons for the pipeline
- The wia-41 worktree had no `artifacts/knowledge/predraft/41.md`; read from the main checkout (same as earlier lessons).
- A PowerShell `.Replace(...) | Set-Content` on the record is blocked by `block-main-checkout-writes`; use the Edit tool for record fixes.
- The validator rejects `area` values outside its list (`messaging`, not `messaging-saga`), destination kind `code`, and a paraphrase source without a `#issue` or URL link; check these before the first run.
