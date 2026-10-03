<!--
title: [TEST] Fixture-run self-test for coverage-report.cs and its per-flag target check
labels: area-testing
milestone: 
kind: test
-->

## Test Category

None of the listed categories applies: this is a self-test of a repository script (`.github/scripts/coverage-report.cs`), run from a script or a workflow step against file fixtures, next to the existing `crap-gate-selftest.ps1`.

## Description

`.github/scripts/coverage-report.cs` has no test of any kind. Its options `--input`, `--output` and `--manifest` are declared at `:28-35`. The script (manifest weighting, per-flag aggregation, `noData` handling at `:592-600`, the CRAP union at `:515-559`, badge colours, JSON shape) is validated only by its two `--check-stale-manifest` and `--check-missing-manifest` modes against the real repository tree, and by whatever the dashboard shows.

No test exists for a per-flag target check either, because the check itself does not exist yet.

Missing: a self-test mode of the script, or a `.ps1` self-test next to `crap-gate-selftest.ps1`, that runs the script against a hand-computed fixture under `.github/scripts/testdata/coverage-report/` (one tiny manifest with `targets`, one Cobertura file per flag). It asserts:

1. A flag below its target yields a non-zero exit, once the per-flag target check exists.
2. A flag with no Cobertura file is reported as a failure and not rendered as `-` (`FlagPct`, `:928-935`).
3. A Cobertura parse error fails the run and is not skipped (`:507-510`).

The test is deterministic: inputs are files, there is no network, clock or randomness, and `--manifest` and `--input` already redirect all reads. The same fixtures also prove that the per-package JSON consumed by `docs/coverage/app.js` keeps its shape. Write this test together with the check, not after it.

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

- [ ] Fixture manifest and one Cobertura file per flag under `.github/scripts/testdata/coverage-report/`, with hand-computed expected values
- [ ] A flag below its target yields a non-zero exit
- [ ] A flag with no Cobertura file is reported as a failure, not rendered as `-`
- [ ] A Cobertura parse error fails the run and is not skipped
- [ ] The per-package JSON keeps the shape that `docs/coverage/app.js` consumes

### Success Criteria

- [ ] All new tests pass
- [ ] Each assertion fails when the behaviour it covers is reverted
- [ ] No flaky tests introduced
- [ ] Tests run within acceptable time limits

## Collection Fixture (Integration Tests Only)

Not applicable: no integration tests are involved.

## Related Issues

- #19 (This issue)
