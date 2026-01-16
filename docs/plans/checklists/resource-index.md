# Testing Dogfooding Resource Index

A complete index of all resources for the testing dogfooding initiative.

---

## Quick Links

| Resource | Location |
|----------|----------|
| 📋 Epic Issue | [#498](https://github.com/dlrivada/Encina/issues/498) |
| 📖 Main Plan | [testing-dogfooding-plan.md](../testing-dogfooding-plan.md) |
| 🎯 Priority Guide | [migration-priority-guide.md](../migration-priority-guide.md) |
| 📊 Progress Tracking | [progress-tracking.md](./progress-tracking.md) |

---

## Documentation

### Planning Documents

| Document | Purpose |
|----------|---------|
| [testing-dogfooding-plan.md](../testing-dogfooding-plan.md) | Comprehensive migration plan |
| [migration-priority-guide.md](../migration-priority-guide.md) | Priority matrix and quick patterns |

### Checklists

| Checklist | Phase |
|-----------|-------|
| [phase1-foundation.md](./phase1-foundation.md) | Add package references |
| [phase2-either-assertions.md](./phase2-either-assertions.md) | Migrate Either assertions |
| [phase3-test-data.md](./phase3-test-data.md) | Migrate to EncinaFaker |
| [phase4-messaging-fakes.md](./phase4-messaging-fakes.md) | Use FakeOutboxStore/FakeInboxStore |
| [common-patterns-and-edge-cases.md](./common-patterns-and-edge-cases.md) | Patterns and troubleshooting |

### Tracking & Communication

| Document | Purpose |
|----------|---------|
| [progress-tracking.md](./progress-tracking.md) | Current migration status |
| [communication-plan.md](./communication-plan.md) | How to communicate |
| [time-estimation-matrix.md](./time-estimation-matrix.md) | Effort estimates |

---

## Reference Implementations

### Encina.Testing.Examples Project

**Location**: `tests/Encina.Testing.Examples/`

| Example File | Demonstrates |
|--------------|--------------|
| `Unit/HandlerTestExamples.cs` | EncinaTestFixture patterns |
| `Unit/HandlerSpecificationExamples.cs` | BDD Given/When/Then |
| `Unit/EitherAssertionExamples.cs` | Shouldly Either extensions |
| `Integration/ModuleIntegrationExamples.cs` | ModuleTestFixture patterns |
| `Fixtures/WireMockFixtureExamples.cs` | HTTP API mocking |
| `TestData/BogusExamples.cs` | EncinaFaker patterns |
| `TestData/MessagingFakerExamples.cs` | FakeOutboxStore, OutboxTestHelper |
| `PropertyBased/PropertyTestExamples.cs` | FsCheck with EncinaProperty |
| `ContractTests/PactConsumerExamples.cs` | EncinaPactConsumerBuilder |
| `Architecture/ArchitectureRulesExamples.cs` | EncinaArchitectureRulesBuilder |

---

## Scripts & Automation

| Script | Purpose |
|--------|---------|
| `scripts/audit-test-dependencies.ps1` | Analyze current dependencies |
| `scripts/update-test-dependencies.ps1` | Add Encina.Testing.* references |

### Usage Examples

```powershell
# Audit all test projects
.\scripts\audit-test-dependencies.ps1 -Path . -OutputFormat Markdown

# Update a specific project
.\scripts\update-test-dependencies.ps1 -ProjectPath tests\Encina.UnitTests\Encina.UnitTests.csproj -DryRun

# Add all packages to a project
.\scripts\update-test-dependencies.ps1 -ProjectPath tests\Encina.UnitTests\Encina.UnitTests.csproj -AddAll
```

---

## CI/CD

| Workflow | Purpose |
|----------|---------|
| `.github/workflows/testing-dogfooding-validation.yml` | Validate examples and track progress |

### Workflow Jobs

- `validate-examples` - Build and run reference examples
- `validate-testing-packages` - Test all Encina.Testing.* packages
- `audit-dependencies` - Generate dependency audit report
- `migration-progress` - Track migration metrics

---

## Encina.Testing.* Packages

| Package | Purpose | Key APIs |
|---------|---------|----------|
| `Encina.Testing` | Core testing utilities | `EncinaTestFixture`, test helpers |
| `Encina.Testing.Shouldly` | Either assertions | `ShouldBeSuccess()`, `ShouldBeError()` |
| `Encina.Testing.Bogus` | Test data generation | `EncinaFaker<T>`, extensions |
| `Encina.Testing.Fakes` | In-memory test doubles | `FakeOutboxStore`, `FakeInboxStore` |
| `Encina.Testing.Handlers` | Handler testing | `HandlerSpecification<,>` |
| `Encina.Testing.WireMock` | HTTP mocking | `EncinaWireMockFixture` |
| `Encina.Testing.Architecture` | Architecture tests | `EncinaArchitectureRulesBuilder` |
| `Encina.Testing.FsCheck` | Property-based testing | `PropertyTestBase`, `EncinaProperty` |
| `Encina.Testing.Pact` | Contract testing | `EncinaPactConsumerBuilder` |
| `Encina.Testing.Verify` | Snapshot testing | `EncinaVerify` |
| `Encina.Testing.Respawn` | Database cleanup | `EncinaRespawner` |

---

## Issue Templates

| Template | Use For |
|----------|---------|
| `.github/ISSUE_TEMPLATE/testing-dogfooding-feedback.md` | Migration feedback and issues |

---

## External Resources

### Official Documentation

| Resource | Link |
|----------|------|
| Shouldly | https://docs.shouldly.org/ |
| Bogus | https://github.com/bchavez/Bogus |
| FsCheck | https://fscheck.github.io/FsCheck/ |
| PactNet | https://github.com/pact-foundation/pact-net |
| WireMock.Net | https://github.com/WireMock-Net/WireMock.Net |
| ArchUnitNET | https://archunitnet.readthedocs.io/ |
| Verify | https://github.com/VerifyTests/Verify |
| Respawn | https://github.com/jbogard/Respawn |

---

## Getting Help

1. **Check documentation** - Start with this index
2. **Check examples** - Look at reference implementations
3. **Search issues** - Someone may have had the same question
4. **Create feedback issue** - Use the template
5. **Comment on #498** - For broader discussions

---

## Document Versions

| Document | Last Updated | Version |
|----------|--------------|---------|
| testing-dogfooding-plan.md | 2026-01-04 | 1.1 |
| migration-priority-guide.md | 2026-01-04 | 1.0 |
| All checklists | 2026-01-04 | 1.0 |
| This index | 2026-01-04 | 1.0 |
