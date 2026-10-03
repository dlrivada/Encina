<!--
title: [TEST] The crap-gate self-test does not run when only the gate's own files change
labels: area-testing
milestone: 
kind: test
-->

## Test Category

None of the listed categories applies: this concerns when a CI job runs the existing self-test of a repository script (`.github/scripts/crap-gate-selftest.ps1`), plus a workflow-text check.

## Description

The CRAP gate's self-test does not run when the gate's own files change. The `crap-gate` job runs only when `needs.changes.outputs.src == 'true'` (`.github/workflows/ci.yml:499-504`), and the `src` filter lists `src/**`, `Directory.*.props` and `*.slnx` (`ci.yml:39-42`), not `.github/scripts/crap-gate.cs`, `.github/scripts/crap-gate-selftest.ps1` or `.github/scripts/testdata/**`.

A PR that edits only the gate (the area of #1506 and #1508) merges with its self-test never executed. That is exactly the regression the self-test was written for (#1354: "so a broken gate fails loudly").

Missing: a path trigger for the self-test, either a separate small job on `.github/scripts/crap-gate*` and `.github/scripts/testdata/crap-gate/**`, or those paths added to the filter. A workflow-text test can assert that the paths are present, which is deterministic.

## Packages / Providers Affected

- **Package(s)**: none (repository tooling, not an Encina package)
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

- [ ] A workflow-text test on `.github/workflows/ci.yml` asserting that a job running `crap-gate-selftest.ps1` is triggered by `.github/scripts/crap-gate*` and `.github/scripts/testdata/crap-gate/**`
- [ ] The job (or filter entry) itself that runs the self-test for those paths

### Success Criteria

- [ ] All new tests pass
- [ ] A PR that edits only `crap-gate.cs` runs the self-test
- [ ] No flaky tests introduced
- [ ] Tests run within acceptable time limits

## Collection Fixture (Integration Tests Only)

Not applicable: no integration tests are involved.

## Related Issues

- #19 (This issue)
- #1506 - area of gate edits that merge without the self-test
- #1508 - area of gate edits that merge without the self-test
- #1354 - the self-test was written so that a broken gate fails loudly
