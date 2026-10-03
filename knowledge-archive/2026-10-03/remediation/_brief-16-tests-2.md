Draft ONE remediation issue file for a finding from the SPEC-003 audit of closed GitHub issue #16 of
the Encina .NET library (tests stage, finding 2, severity Major). Use
ONLY the input finding text; never invent facts. Output EXACTLY the header comment block below followed by the
template body below it, keeping every '## ' header of the template body verbatim and in the same order, and
ticking a checkbox only from the options the template body itself lists:

<!--
title: [TEST] <specific title drawn from the finding>
labels: area-testing
milestone: 
-->

## Test Category

- [ ] Unit Tests
- [ ] Integration Tests (Docker/Testcontainers)
- [ ] Property-Based Tests (FsCheck)
- [ ] Contract Tests
- [ ] Guard Clause Tests
- [ ] Load Tests (NBomber)
- [ ] Benchmark Tests (BenchmarkDotNet)
- [ ] Coverage Gap (below 85% target)

## Description

A clear description of the testing work needed.

## Packages / Providers Affected

- **Package(s)**: [e.g., Encina.Dapper.SqlServer, Encina.ADO.PostgreSQL]
- **Provider(s)**: [e.g., ADO-SqlServer, Dapper-PostgreSQL, EFCore-MySQL, MongoDB]

## Current Coverage

> Fill in if this is a coverage gap issue.

| Package | Line Coverage | Target | Gap |
|---------|:------------:|:------:|:---:|
| Example.Package | 62.3% | 85% | -22.7% |

## Infrastructure Required

- [ ] Docker / Testcontainers
- [ ] Real database (specify: SQL Server / PostgreSQL / MySQL / MongoDB)
- [ ] Message broker (specify: RabbitMQ / Kafka / NATS / MQTT)
- [ ] NBomber load testing framework
- [ ] BenchmarkDotNet
- [ ] None (pure unit tests)

## Test Plan

### Tests to Implement

- [ ] Test 1: Description
- [ ] Test 2: Description

### Success Criteria

- [ ] All new tests pass
- [ ] Coverage meets ≥85% target (if coverage gap)
- [ ] No flaky tests introduced
- [ ] Tests run within acceptable time limits

## Collection Fixture (Integration Tests Only)

> Per `AGENTS.md` §9 (Testing obligations): Integration tests MUST use shared `[Collection]` fixtures.

- **Collection**: [e.g., `ADO-PostgreSQL`, `Dapper-SqlServer`, `EFCore-MySQL`]
- **Fixture**: [e.g., `PostgreSqlFixture`, `SqlServerFixture`]

## Related Issues

- #___ - Description

Guidance:
- Put the finding's file:line evidence in the Location (or Steps to Reproduce) section.
- Related Issues: include #16 and any of these candidate open issues that are related but are NOT
  the same problem (a same-problem duplicate must never reach this step): #910: [TEST] Increase coverage for 8 mejorable modules (60-79%) to reach 85%; #853: [DEBT] ~174 Options classes missing IValidateOptions<T> validators (only 15/189 have them); #1389: [TEST] Guard flag below target in 42 of 99 packages: close the gap per package family; #634: [FEATURE] Saga State Transition Events via CDC; #128: [FEATURE] Enhanced Saga Visibility & Process Manager; #1221: [FEATURE] Stable persisted message type names and payload versioning for outbox, inbox and scheduled messages; #262: [FEATURE] MassTransit interoperability; #134: [FEATURE] Message Versioning & Upcasting for Outbox/Inbox; #617: [FEATURE] Encina.Sentry - Sentry Error Monitoring Integration
