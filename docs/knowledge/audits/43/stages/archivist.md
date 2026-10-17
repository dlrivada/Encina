## Scope
Closing commit ea980e755 (timeline `referenced` event; no PR). Files today:
- src/Encina.Messaging/Sagas/IHandleSagaNotFound.cs (HandleAsync with CancellationToken), ISagaNotFoundDispatcher.cs, SagaNotFoundContext.cs, SagaNotFoundDispatcher.cs, SagaErrorCodes.cs (HandlerCancelled/HandlerFailed; the original commit added them inside SagaOrchestrator.cs, they now live in SagaErrorCodes.cs).
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs line 310 (`AddScoped<ISagaNotFoundDispatcher, SagaNotFoundDispatcher>()` inside RegisterSagas, only when UseSagas).
- Original commit also touched src/Encina.MongoDB/Sagas/SagaStoreMongoDB.cs (4 lines) and SagaOrchestrator.cs (constants only).
- Tests: the commit's tests/Encina.Tests/Sagas/SagaNotFoundContextTests.cs and SagaNotFoundDispatcherTests.cs were deleted/moved in consolidation commit 658263027. Today: tests/Encina.UnitTests/Messaging/Sagas/SagaNotFoundContextTests.cs, tests/Encina.GuardTests/Messaging/Sagas/SagaNotFoundContextGuardTests.cs, and incidental dispatcher use in tests/Encina.UnitTests/Messaging/DeadLetter/DeadLetterRuntimeCaptureTests.cs and tests/Encina.IntegrationTests/Messaging/DeadLetter/DeadLetterSourcesEndToEndScenario.cs. No SagaNotFoundDispatcherTests file exists.
- Later changes: 576abb4c8 (#2202, merged 2026-10-10) reworked the context and dispatcher for dead letter capture (#2203).
- Coverage manifest: .github/coverage-manifest/Encina.Messaging.json.

## Destinations
- Decision "optional handler + dispatcher + error codes": present. docs/features/dead-letter-queue.md (saga-not-found section with usage) and ADR-046 decision 15; CHANGELOG.md lines ~9550 list the types.
- No dedicated docs page for IHandleSagaNotFound outside the DLQ page (issue task "Add documentation"): grep of docs/ finds only dead-letter-queue.md and ADR-046; sagas have no guide (#1885 open).
- Regression test for the dispatcher: no dedicated file, but 13 direct unit tests sit in DeadLetterRuntimeCaptureTests.cs (lines 212-382, 49/49 lines under the unit flag); guard coverage is 0 because the type is internal. #1480, #1481 and #1886 are stale trackers. Context tests present.
- Pipeline integration: missing. A grep of src/ for `ISagaNotFoundDispatcher` and `DispatchAsync` shows no caller in SagaOrchestrator or SagaRunner; the dispatcher is registered and called only by user code.
- StartNewSaga option: absent from src/.

## Successor and duplicate issues
- No successor or duplicate. Related: #1480 (OPEN, [TEST] unit and guard tests for SagaNotFoundDispatcher and IHandleSagaNotFound); #2203 (CLOSED 2026-10-10, DLQ SourceMessageId spike that reshaped the context); #1886 (OPEN, delta re-audit of #16, mentions the dispatcher). No open issue tracks wiring the dispatcher into the saga pipeline or StartNewSaga (searched "saga not found" and "SagaNotFoundDispatcher" over all states).

## Lessons for the pipeline
- The pre-draft read the usage example as the whole delivery; compare the body's task list ("Integrate with saga pipeline") with callers in src/ (grep for the dispatcher's method), which showed the integration was never wired.
- The pre-draft had `closed_at: 12/25/2025`, `linked_prs: []` and invented a "rules" item (auto-register dispatchers); both were normalised/dropped.
- The wia-43 worktree had no predraft; read from the main checkout.
- A shell command containing a stray `sed` was blocked by the prohibited-commands hook; reran without it.
