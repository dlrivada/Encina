<!--
title: [TEST] Run generate-coverage-manifest.cs --self-test in CI
labels: area-testing
milestone: 
kind: test
-->

## Test Category

None of the listed categories applies: this wires an existing self-test of a repository script (`.github/scripts/generate-coverage-manifest.cs --self-test`) into CI.

## Description

`generate-coverage-manifest.cs --self-test` (`.github/scripts/generate-coverage-manifest.cs:348-484`, `RunSelfTest` at `:350`, temporary fixtures, exit 1 on failure) is never invoked by CI, a hook test or any script. A repository-wide search of the `*.yml`, `*.ps1` and `*.json` files finds the option only in the script's own header and argument parsing (`:9`, `:28`, `:47`, `:66`).

The generator produces the manifests that define every flag target. #1542 added `--append-only` plus this self-test so that key preservation stays proven; without a CI step the assertions can rot.

Missing: a step in the `coverage-citations` job (`.github/workflows/ci.yml:548-577`; no build needed, same setup as the two existing `coverage-report.cs --check-*` steps at `:572-577`) running:

```pwsh
dotnet run .github/scripts/generate-coverage-manifest.cs -- --self-test
```

The test is deterministic: it uses a temporary directory with a GUID name, with no network or clock.

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

- [ ] A `Self-test generate-coverage-manifest.cs` step in the `coverage-citations` job that runs `--self-test` and fails the job on a non-zero exit
- [ ] Confirm the self-test passes against the current script before enabling the step

### Success Criteria

- [ ] All new tests pass
- [ ] A change that breaks key preservation in the generator turns the `coverage-citations` job red
- [ ] No flaky tests introduced
- [ ] Tests run within acceptable time limits

## Collection Fixture (Integration Tests Only)

Not applicable: no integration tests are involved.

## Related Issues

- #19 (This issue)
- #1542 - added `--append-only` and the self-test
