## Scope

Issue #38: created 2025-12-24T13:24:09Z, closed 2025-12-25T08:29:03Z, COMPLETED; one comment ("Implemented saga timeout support in commit 3cc4d57"). No linked PR. The only shipped change is commit 3cc4d57b (`git show --stat`, 32 files, authored 2025-12-25T09:28:43+01:00 = 08:28:43Z, 20 seconds before closed_at). Milestone today: v0.04.0 (re-milestoned 2026-03-23).

Mapped to today (`git log --follow` run per file; all still exist unless stated):
- `src/Encina.Messaging/Sagas/ISagaState.cs` (`TimeoutAtUtc`), `ISagaStore.cs` (`GetExpiredSagasAsync`), `SagaOrchestrator.cs` (`StartAsync(..., timeout)`, `TimeoutAsync`, `GetExpiredSagasAsync`, `SagaStatus.TimedOut`, `SagaOptions.DefaultSagaTimeout`, `ExpiredSagaBatchSize`; later commits: TimeProvider #543, ROP #670, EventIds #1120, encryption #1168, SagaRunner #1469), `SagaErrorCodes.cs` (`saga.timeout`).
- Stores: `src/Encina.EntityFrameworkCore/Sagas/SagaStoreEF.cs`; `src/Encina.Dapper.{SqlServer,PostgreSQL,MySQL}/Sagas/SagaStoreDapper.cs` and `SagaStateFactory.cs`; `src/Encina.ADO.{SqlServer,PostgreSQL,MySQL}/Sagas/SagaState.cs` and `SagaStoreADO.cs`; `src/Encina.MongoDB/Sagas/SagaStoreMongoDB.cs`, `SagaStateFactory.cs`.
- Consumers of the new API today: `src/Encina.Messaging/Health/SagaHealthCheck.cs:56`, `src/Encina.OpenTelemetry/MessagingStores/InstrumentedSagaStore.cs`, `src/Encina.Testing.Fakes/Stores/FakeSagaStore.cs`.
- Removed on purpose, scope nothing further: `src/Encina.ADO.Oracle`, `Encina.Dapper.Oracle` (6121713c, Oracle out of scope, ADR-009) and `Encina.ADO.Sqlite`, `Encina.Dapper.Sqlite` (22494a97, ADR-024). The commit changed them; they are not in `src/` today.
- Tests: the commit's two test files moved with the test consolidation. Today's equivalents: `tests/Encina.UnitTests/EntityFrameworkCore/Sagas/SagaStoreEFTests.cs`, `SagaStateFactoryTests.cs` under `tests/Encina.UnitTests/{ADO,Dapper}/{SqlServer,PostgreSQL,MySQL}/Sagas`, `EntityFrameworkCore/Sagas`, `MongoDB/Sagas`; further coverage in `tests/Encina.UnitTests/Messaging/Sagas/SagaOrchestratorTests.cs`, `tests/Encina.GuardTests/Messaging/Sagas/SagaOrchestratorGuardTests.cs`, integration tests `SagaStoreADOTests` (3 providers), `SagaStoreEFSqlServerTests`, `SagaStoreMongoDBIntegrationTests`.
- Not shipped (verified by grep of src/): no `RequestTimeout` symbol anywhere in `src/` (only unrelated Resilience hits for the word "Timeout"); `SagaOrchestrator.TimeoutAsync` has no caller in `src/` (only its definition at `SagaOrchestrator.cs:435`); `GetExpiredSagasAsync` is called only by `SagaHealthCheck`. No background processor, scheduler (Hangfire/Quartz) integration or timeout handler exists. The code stage should check whether an expired saga is ever timed out or compensated automatically, and which tests exercise `TimeoutAsync`.
- The pre-draft's "Encina.Messaging" only package list was extended to the provider packages the commit really changed.

## Destinations

- Primitives (TimeoutAtUtc, StartAsync overload, options, GetExpiredSagasAsync, TimedOut, saga.timeout): present in code (above); documented in `CHANGELOG.md` "Saga timeout support (Issue #38)" (present, found by grep). Not present in `docs/messaging/sagas.md` (grep for `Timeout|GetExpired` there finds only `StuckSagaTimeout` and `GetStuckSagasAsync`, a different, choreography/stuck-saga feature) nor in the in-repo docs except `docs/releases/pre-v0.10.0/README.md` and `docs/INVENTORY.md` hits that I did not review in depth: docs gap for the docs stage.
- `RequestTimeout<T>()`, per-step timeouts, timeout handler, automatic dispatch, scheduler integration, retry on timeout: missing (no code, no ADR, no backlog issue found: `gh issue list --state all --search "saga timeout in:title"` returns only #38). Recorded as pending-work, `current: unknown`, destination none.
- `docs/engineering/PROJECT-HISTORY.md:96` says "Saga timeouts are implemented via a RequestTimeout<T>() pattern": inaccurate against the code; recorded as a gotcha with a planned backlog destination for the remediation batch.
- No ADR: no decision of #38 is architectural; the issue's "Alternatives Considered" says "_Not evaluated yet._", so there are no rejected alternatives.
- The pre-draft's "Candidate destinations" (docs, regression tests, spec-invariant) and "85%+ coverage" came from the template's acceptance criteria and test matrix, not from a decision; dropped. Its `closed_at: 12/25/2025` and `linked_prs: []` were normalised to ISO and the block format.

## Successor and duplicate issues

None. No duplicate or successor: the title search above returns only #38; the issue was not closed as rejected or duplicate. (Not a rejected issue; the open question is delivery gap, not a successor.)

## Lessons for the pipeline

- (issue-archivist) When the closing commit's message lists primitives while the issue title promises a named API (`RequestTimeout<T>()`), grep `src/` for the title's symbol and for callers of each new method; here the symbol does not exist and `TimeoutAsync` has no caller, which makes the outcome `partial`, not `delivered`. (grep src/ for the issue title's API symbol and for callers of each new method before choosing `delivered`)
- (issue-archivist) A project-history page can repeat an issue title as if shipped (PROJECT-HISTORY.md line 96); compare such lines with the code before treating them as evidence of delivery. (compare PROJECT-HISTORY lines with the code before using them as delivery evidence)
- (issue-archivist) The record's `audit.checklist` is the number `1` in the worked example; the checker accepts other values, but follow the example. (use `checklist: 1`, as in docs/knowledge/issues/1345.md)
