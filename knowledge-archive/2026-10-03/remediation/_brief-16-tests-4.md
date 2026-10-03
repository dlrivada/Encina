Draft ONE remediation issue file for a finding from the SPEC-003 audit of closed GitHub issue #16 of
the Encina .NET library (tests stage, finding 4, severity Major). Use
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
  the same problem (a same-problem duplicate must never reach this step): #1134: [DEBT] 115 store error paths build EncinaError from ex.Message and drop the exception; #1322: [BUG] Encina.ADO.PostgreSQL audit and anonymization stores interpolate raw exception messages into EncinaError; #1343: [DEBT] Error-message leak static scan misses multi-line calls and ex.Message; leaks in SagaRunner and CDC cache invalidation; #1301: [DEBT] About 30 health checks put raw exception messages into their unhealthy results; #185: [FEATURE] Observability: Implement Error Recording According to OpenTelemetry Spec; #1435: [BUG] LawfulBasisValidationPipelineBehavior logs the raw data subject id and EncinaError.Message on every consent check; #1454: [BUG] EncinaError.Message still reaches OpenTelemetry activity tags/status in DataSubjectRights, DPIA and Retention; #1433: [DEBT] Duplicate evidence excludes every AGENTS.md/CLAUDE.md backticked token, not only house-rule quotes; #1348: [DEBT] Encina.Marten: InlineProjectionRelay logs EncinaError.Message, and AddEncinaMarten/AddSnapshotableAggregate have no ValidateOnBuild DI test; #1396: [DEBT] EF Core and GraphQL package READMEs still show error.Message in log/error examples
