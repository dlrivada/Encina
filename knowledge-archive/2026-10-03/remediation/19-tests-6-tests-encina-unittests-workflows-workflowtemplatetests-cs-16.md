<!--
title: [TEST] WorkflowTemplateTests locks in the global coverage-threshold input and no test protects the blocking ci-result set
labels: area-testing
milestone: 
kind: test
-->

## Test Category

- [x] Unit Tests
- [ ] Integration Tests (Docker/Testcontainers)
- [ ] Property-Based Tests (FsCheck)
- [ ] Contract Tests
- [ ] Guard Clause Tests
- [ ] Load Tests (NBomber)
- [ ] Benchmark Tests (BenchmarkDotNet)
- [ ] Coverage Gap (below the per-flag target in the manifest)

## Description

`tests/Encina.UnitTests/Workflows/WorkflowTemplateTests.cs:160` asserts that the template input `coverage-threshold` exists (`[InlineData("encina-test.yml", "coverage-threshold")]`). That locks in the single global threshold the repository rejected in #19, and every test in the class targets `.github/workflows/templates/*.yml`, which no workflow calls.

The class checks structure only (names, permissions, `runs-on`, steps, cache action). It asserts nothing about the live workflows `ci.yml` and `ci-full.yml`, for example that the `ci-result` job's `needs` contains `crap-gate` and `coverage-citations` (`.github/workflows/ci.yml:635`), which is the whole enforcement of the CRAP rule in AGENTS.md section 9.

Work to do:

- Fix this together with the removal of the global threshold from the templates: delete the `coverage-threshold` row with the input, or delete the templates and the whole fixture.
- Add one workflow test over `ci.yml` asserting that `ci-result.needs` contains `crap-gate`, so removing the gate from the blocking set is a red test and not a silent change.

The test is deterministic: it parses YAML as the existing fixture does. The class uses `IClassFixture<WorkflowTemplateFixture>`, which is allowed here because it is not a database fixture, and `AppContext.BaseDirectory` path discovery, which is deterministic.

## Packages / Providers Affected

- **Package(s)**: none (the tests cover repository workflow files, not an Encina package)
- **Provider(s)**: none

## Current Coverage

Not a coverage-gap issue; no coverage figures are measured here.

## Infrastructure Required

- [ ] Docker / Testcontainers
- [ ] Real database (specify: SQL Server / PostgreSQL / MySQL / MongoDB)
- [ ] Message broker (specify: RabbitMQ / Kafka / NATS / MQTT)
- [ ] NBomber load testing framework
- [ ] BenchmarkDotNet
- [x] None (pure unit tests)

## Test Plan

### Tests to Implement

- [ ] Remove the `[InlineData("encina-test.yml", "coverage-threshold")]` row at `WorkflowTemplateTests.cs:160` (or the whole fixture if the templates are deleted)
- [ ] `ci.yml` `ci-result` job `needs` contains `crap-gate`
- [ ] `ci.yml` `ci-result` job `needs` contains `coverage-citations`

### Success Criteria

- [ ] All new tests pass
- [ ] Removing `crap-gate` from the `ci-result` `needs` list turns a test red
- [ ] No flaky tests introduced
- [ ] Tests run within acceptable time limits

## Collection Fixture (Integration Tests Only)

Not applicable: no integration tests are involved.

## Related Issues

- #19 (This issue)
