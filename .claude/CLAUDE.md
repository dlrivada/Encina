# Claude Code - Encina Guidelines

## Project Philosophy

### Pre-1.0 Development Status

- **Current Phase**: Pre-1.0 - Initial Design & Architecture
- **No Backward Compatibility Required**: We are NOT maintaining backward compatibility
- **Breaking Changes**: Fully acceptable and encouraged if they improve the design
- **Migration Support**: NOT needed - no existing users to migrate

### Design Principles

1. **Best Solution First**: Always choose the best technical solution, never compromise for compatibility
2. **Clean Architecture**: No legacy code, no deprecated features, no obsolete properties
3. **Pay-for-What-You-Use**: All features are opt-in, never forced on users
4. **Provider-Agnostic**: Use abstractions to support multiple implementations (EF Core, Dapper, ADO.NET)
5. **.NET 10 Only**: We use .NET 10 exclusively (very recent, stable release)

### Technology Stack

- **.NET Version**: .NET 10.0 (mandatory, no support for older versions)
- **Language Features**: Use latest C# features without hesitation
- **Breaking Changes**: Expected and acceptable in .NET 10 APIs
- **Nullable Reference Types**: Enabled everywhere

### Code Quality Standards

- **No Obsolete Attributes**: Never mark code as `[Obsolete]` for backward compatibility
- **No Legacy Code**: If we need to change something, we change it completely
- **No Migration Paths**: Don't implement migration helpers or compatibility layers
- **Clean Codebase**: Every line of code should serve a current purpose

---

## Architecture Decisions

### Railway Oriented Programming (ROP)

- Core pattern: `Either<EncinaError, T>`
- Explicit error handling, no exceptions for business logic
- Validation returns `Either` with detailed errors

### Messaging Patterns (All Optional)

1. **Outbox Pattern**: Reliable event publishing (at-least-once delivery)
2. **Inbox Pattern**: Idempotent message processing (exactly-once semantics)
3. **Saga Pattern**: Distributed transactions with compensation (orchestration-based)
4. **Scheduled Messages**: Delayed/recurring command execution
5. **Transactions**: Automatic database transaction management

### Provider Coherence

- **Encina.Messaging**: Shared abstractions (IOutboxStore, IInboxStore, etc.)
- **Encina.EntityFrameworkCore**: EF Core implementation
- **Encina.Dapper.{Database}**: Dapper implementations (SqlServer, PostgreSQL, MySQL, Sqlite, Oracle)
- **Encina.ADO.{Database}**: ADO.NET implementations
- Same interfaces, different implementations - easy to switch providers

### Opt-In Configuration

All messaging patterns are disabled by default:

```csharp
// Simple app - only what you need
config.UseTransactions = true;

// Complex distributed system - all patterns
config.UseTransactions = true;
config.UseOutbox = true;
config.UseInbox = true;
config.UseSagas = true;
config.UseScheduling = true;
```

### Validation Architecture (Orchestrator Pattern)

```
Encina (core)
├── Encina.Validation.IValidationProvider (interface)
├── Encina.Validation.ValidationOrchestrator (domain logic)
├── Encina.Validation.ValidationPipelineBehavior<,> (centralized behavior)
├── Encina.Validation.ValidationResult (immutable result)
└── Encina.Validation.ValidationError (record)

Encina.FluentValidation / DataAnnotations / MiniValidator
├── *ValidationProvider (implements IValidationProvider)
└── ServiceCollectionExtensions (registers orchestrator + provider)
```

---

## Naming Conventions

### Messaging Entities

- **Outbox**: `OutboxMessage` (not Message)
- **Inbox**: `InboxMessage` (not Message)
- **Saga**: `SagaState` (not Saga)
- **Scheduling**: `ScheduledMessage` (not ScheduledCommand)

### Property Names (Standardized)

- **Type Information**: `RequestType` or `NotificationType` (not MessageType)
- **Error Information**: `ErrorMessage` (not Error - avoids CA1716 keyword conflict)
- **Timestamps**: Always UTC with `AtUtc` suffix
  - `CreatedAtUtc`, `ProcessedAtUtc`, `ScheduledAtUtc`, etc.
  - **Saga timestamps**: `StartedAtUtc`, `LastUpdatedAtUtc`, `CompletedAtUtc`
- **Retry Logic**: `RetryCount`, `NextRetryAtUtc` (not AttemptCount)
- **Identifiers**: Descriptive names (`SagaId` not `Id` when implementing interface)

### Store Implementations

- Pattern: `{Pattern}Store{Provider}`
- Examples: `OutboxStoreEF`, `InboxStoreDapper`, `SagaStoreADO`
- Never just `Store` or `Repository`

---

## Testing Standards

### Coverage Targets

- **Line Coverage**: ≥85% (target for overall codebase)
- **Branch Coverage**: ≥80% (target for overall codebase)
- **Method Coverage**: ≥90% (target for overall codebase)
- **Mutation Score**: ≥80% (Stryker mutation testing)

### Test Types

1. **Unit Tests** ✅ (Required) - Location: `tests/{Package}.Tests/`
2. **Integration Tests** 🟡 (Critical paths) - Location: `tests/{Package}.IntegrationTests/`
3. **Contract Tests** 🟡 (Public APIs) - Location: `tests/{Package}.ContractTests/`
4. **Property-Based Tests** 🟡 (Complex logic) - Location: `tests/{Package}.PropertyTests/`
5. **Guard Clause Tests** 🟡 (Public methods) - Location: `tests/{Package}.GuardTests/`
6. **Load Tests** 🟡 (Performance) - Location: `load/Encina.LoadTests/`
7. **Benchmarks** 🟡 (Hot paths) - Location: `benchmarks/Encina.Benchmarks/`

### Test Quality Standards

**Good tests should**:
- Have a clear, descriptive name (no `Test1`, `Test2`)
- Follow AAA pattern (Arrange, Act, Assert)
- Test ONE thing (single responsibility)
- Be independent (no shared state between tests)
- Be deterministic (same input = same output, always)

**Avoid**:
- Skipping tests without justification
- Testing implementation details (test behavior, not internals)
- Using `Thread.Sleep` (prefer proper synchronization)

---

## Code Analysis

- **Zero Warnings**: All CA warnings must be addressed (fix or suppress with justification)
- **Suppression Rules**:
  - CA1848 (LoggerMessage delegates): Suppress if performance optimization is future work
  - CA2263 (Generic overload): Suppress when dynamic serialization is needed
  - CA1716 (Keyword conflicts): Fix by renaming (e.g., `Error` → `ErrorMessage`)

---

## Documentation

- **XML Comments**: Required on all public APIs
- **Examples**: Provide code examples in XML docs when helpful
- **README Files**: Each satellite package has its own comprehensive README

---

## Git Workflow

- **No Force Push to main/master**: Never use `--force` on main branches
- **Commit Messages**: Clear, descriptive, in English
- **No AI Attribution**: Do NOT include any AI signatures, co-author tags, or Claude references in commits
  - ❌ Never add `Co-Authored-By: Claude...`
  - ❌ Never add `🤖 Generated with Claude Code`
  - ❌ Never add any reference to AI assistance in commit messages
- **Author**: All commits should appear as authored solely by the repository owner

---

## Language Rules

- User communicates in Spanish
- Code, comments, documentation: **English only**
- Commit messages: English
- User-facing messages: Spanish when responding to user
- **Translation Rule**: If you encounter any Spanish comments in code while editing, translate them to English

---

## Issue Tracking

**All bugs, features, and technical debt MUST be tracked via GitHub Issues.**

Location: https://github.com/dlrivada/Encina/issues

### Issue Templates

| Template | Use Case | Label |
|----------|----------|-------|
| `bug_report.md` | Report bugs or unexpected behavior | `bug` |
| `feature_request.md` | Suggest new features or enhancements | `enhancement` |
| `technical_debt.md` | Track internal code quality issues | `technical-debt` |

### Project Documentation Files

| File | Purpose | Update Frequency |
|------|---------|------------------|
| `ROADMAP.md` | High-level vision by milestone | Occasionally |
| `CHANGELOG.md` | Released changes (Keep a Changelog format) | Each release |
| `docs/history/YYYY-MM.md` | Detailed monthly implementation history | During development |
| GitHub Issues | Actionable work items | Constantly |

---

## MSBuild Stability

⚠️ **Building the full solution can cause MSBuild crashes** due to parallel execution overload with the large test suite (70+ projects).

**Mitigations** (ALWAYS use one of these):

1. **Use `-maxcpucount:1` flag** for single-process builds
2. **Use Solution Filters (.slnf)** to build only what you need (preferred)

```bash
dotnet build Encina.Caching.slnf   # 17 projects
dotnet build Encina.Core.slnf      # 7 projects
dotnet build Encina.Database.slnf  # 21 projects
```

---

## Common Errors to Avoid

1. ❌ Don't add `[Obsolete]` attributes for backward compatibility
2. ❌ Don't create migration helpers or compatibility layers
3. ❌ Don't use .NET 9 or older - only .NET 10
4. ❌ Don't name properties `Error` (use `ErrorMessage` to avoid CA1716)
5. ❌ Don't make patterns mandatory - everything is opt-in
6. ❌ Don't mix provider-specific code with abstractions
7. ❌ Don't compromise design for non-existent legacy users

---

## Remember

> "We're in Pre-1.0. Choose the best solution, not the compatible one."
