<!--
title: [TEST] Send_ComposesPipelineAcrossOutcomes is skipped with no issue and still asserts the pre-fail-fast contract
labels: area-testing
milestone: 
kind: test
-->

## Test Category

- [ ] Unit Tests
- [ ] Integration Tests (Docker/Testcontainers)
- [x] Property-Based Tests (FsCheck)
- [ ] Contract Tests
- [ ] Guard Clause Tests
- [ ] Load Tests (NBomber)
- [ ] Benchmark Tests (BenchmarkDotNet)
- [ ] Coverage Gap (below 85% target)

## Description

`Send_ComposesPipelineAcrossOutcomes` in `tests/Encina.PropertyTests/ConfigurationProperties.cs:301-302` is declared `[Property(MaxTest = 150, Skip = "Pure ROP: exceptions now propagate (fail-fast)")]`. It is the only test with a `Skip` in `tests/Encina.PropertyTests` and `tests/Encina.ContractTests`, and no issue is referenced, which breaks AGENTS.md section 9 ("NEVER skip a test without justification", "ignore a flaky test: fix or delete it").

The body (`ConfigurationProperties.cs:316-330`) still asserts the old contract: a handler exception must come back as `Left` whose `err.Exception` is an `InvalidOperationException`, a handler cancellation as `Left` whose `err.Exception` is an `OperationCanceledException`, and in both cases the full pre-processor, behavior and handler event timeline must match `BuildExpectedTimeline`. The dispatcher no longer behaves that way for exceptions: `RequestDispatcher.ExecuteAsync` (`src/Encina/Dispatchers/Encina.RequestDispatcher.cs:146-162`) catches only `OperationCanceledException` when the token is cancelled and returns a `Left` with `EncinaErrorCodes.RequestCancelled`; its comment at `:163-165` states that any other exception propagates (fail-fast).

Only the `Success` outcome is still checked, by `Send_ComposesPipelineDeterministically` (`ConfigurationProperties.cs:282-299`). Composition of pre-processors, pipeline behaviors and post-processors under a throwing or cancelled handler has no property coverage, and `ThrowAsFault` and `ThrowAsCancellation` (`ConfigurationProperties.cs:429-430`) and the `Exception` and `Cancellation` arms of `MapExecution` (`ConfigurationProperties.cs:227-228`) are reachable only from the skipped test.

## Packages / Providers Affected

- **Package(s)**: None (test code only: `Encina.PropertyTests`)
- **Provider(s)**: Not applicable (no provider-specific code)

## Current Coverage

No coverage figure applies to this item. The gap is behavioral: the `Exception` and `Cancellation` outcomes of `Encina.Send` have no property test while the skipped test stays in place.

## Infrastructure Required

- [ ] Docker / Testcontainers
- [ ] Real database (specify: SQL Server / PostgreSQL / MySQL / MongoDB)
- [ ] Message broker (specify: RabbitMQ / Kafka / NATS / MQTT)
- [ ] NBomber load testing framework
- [ ] BenchmarkDotNet
- [x] None (pure unit tests)

## Test Plan

### Tests to Implement

- [ ] Rewrite the `Exception` arm of `Send_ComposesPipelineAcrossOutcomes` to the current fail-fast contract: the exception thrown by the handler escapes `Encina.Send`, and the recorded events up to the failure are the expected prefix of the timeline (pre-processors, behavior enter events, handler).
- [ ] Rewrite the `Cancellation` arm to what `RequestDispatcher` returns today (`Left` with `EncinaErrorCodes.RequestCancelled`, `src/Encina/Dispatchers/Encina.RequestDispatcher.cs:146-162`), and confirm by running it whether a pre-cancelled token reaches the handler, since the test cancels the token before `Send` (`ConfigurationProperties.cs:384-385`).
- [ ] Remove the `Skip` argument from the attribute.
- [ ] Alternative if the property is judged not worth keeping: delete the test together with `ThrowAsFault`, `ThrowAsCancellation` and the unused `MapExecution` arms, and record where exception propagation of `Encina.Send` is covered.

### Success Criteria

- [x] All new tests pass
- [ ] Coverage meets ≥85% target (if coverage gap)
- [x] No flaky tests introduced
- [x] Tests run within acceptable time limits

## Collection Fixture (Integration Tests Only)

Not applicable: this is a property test with no database or container.

## Related Issues

- #28 (This issue)
