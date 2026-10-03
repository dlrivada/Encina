<!--
title: [TEST] Deterministic text check that no hand-typed 85% coverage target remains in templates, agent instructions or the dashboard
labels: area-testing
milestone: 
kind: test
-->

## Test Category

None of the listed categories applies: this is a repository-wide text check, not a test of an Encina package.

## Description

The rejected single global coverage target of #19 survived in many places that no test watches. One deterministic text check, "no hand-typed 85% coverage target in `.github/ISSUE_TEMPLATE`, `.opencode`, `docs/coverage/app.js`", would have caught the stale targets in:

- the issue and PR templates (`.github/ISSUE_TEMPLATE/test_implementation.md:18`, `:35`, `:56`, `epic.md:65`, `feature_request.md:182`),
- the agent and skill instructions (`.opencode/skills/test-workflow/SKILL.md:28-32`, `.opencode/agents/encina-test.md:19-23`, `.opencode/skills/release-checklist/SKILL.md:32-34`),
- the dashboard (`docs/coverage/app.js:817-831`),
- the legacy single-threshold artifacts and the workflow templates that carry a global threshold.

The fixes for each of these places are tracked in their own issues; this issue adds the check that keeps the figure from coming back.

## Packages / Providers Affected

- **Package(s)**: none (repository files, not an Encina package)
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

- [ ] A text check over `.github/ISSUE_TEMPLATE`, `.github/pull_request_template.md`, `.opencode` and `docs/coverage/app.js` that fails when a hand-typed 85% coverage target appears
- [ ] The check names the offending file and line in its failure message
- [ ] The allowed contexts (for example arithmetic examples that are not targets) are listed explicitly so the check stays deterministic

### Success Criteria

- [ ] All new tests pass
- [ ] The check fails on the current stale text and passes once the fixes are merged
- [ ] No flaky tests introduced
- [ ] Tests run within acceptable time limits

## Collection Fixture (Integration Tests Only)

Not applicable: no integration tests are involved.

## Related Issues

- #19 (This issue)
