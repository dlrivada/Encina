<!--
title: [TEST] Tighten the crap-gate self-test assertions to exact output lines
labels: area-testing
milestone: 
kind: test
-->

## Test Category

None of the listed categories applies: this tightens the assertions of an existing self-test of a repository script (`.github/scripts/crap-gate-selftest.ps1`).

## Description

The crap-gate self-test invokes `dotnet run --file` (`.github/scripts/crap-gate-selftest.ps1:48`) with no configuration, so it builds and runs the script in the default (Debug) configuration. Its assertions match the output with loose regexes (`'VIOLATION.*RiskyMethod'` at `:69`, `'VIOLATION.*Enclosing'` at `:140`, `'Enclosing\b'` at `:117`) that would also match any line that merely mentions those names.

This is not a defect today: the fixtures are hand-computed and deterministic, and the exit-code assertions are exact. But the negative check of assertion 4 (`-match 'Enclosing\b'`, `:117`) would also pass if the output format changed so that the enclosing method appears under a different heading that still contains the word.

Prefer asserting the exact `Changed methods analyzed: 1` line plus an anchored `VIOLATION` line format. Low priority: keep this only if the file is touched for the other self-test changes (exit 2 on missing input, and running the self-test when the gate's own files change).

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

- [ ] Assertion 4 checks the exact `Changed methods analyzed: 1` line and that no line attributes the enclosing method, with an anchored match
- [ ] The `VIOLATION` assertions (1 and 5) use an anchored line format instead of `VIOLATION.*Name`
- [ ] Optionally pass `-c Release` to `dotnet run` so the self-test runs in the same configuration as the gate

### Success Criteria

- [ ] All new tests pass
- [ ] Each tightened assertion still fails when the behaviour it covers is reverted
- [ ] No flaky tests introduced
- [ ] Tests run within acceptable time limits

## Collection Fixture (Integration Tests Only)

Not applicable: no integration tests are involved.

## Related Issues

- #19 (This issue)
