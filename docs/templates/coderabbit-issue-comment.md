# CodeRabbit Issue Comment Template

This template provides a standard format for requesting CodeRabbit analysis on existing issues.

## Usage

Copy and paste the appropriate template as a comment on any GitHub issue that needs CodeRabbit analysis.

---

## Template: Full Analysis Request

```markdown
@coderabbitai please analyze this issue and provide:

### Provider Implementation Status
For database-related issues, verify implementation across all 12 providers:

| Category | Providers | Expected |
|----------|-----------|----------|
| **ADO.NET** | Sqlite, SqlServer, PostgreSQL, MySQL, Oracle | 5 |
| **Dapper** | Sqlite, SqlServer, PostgreSQL, MySQL, Oracle | 5 |
| **ORM** | EntityFrameworkCore | 1 |
| **NoSQL** | MongoDB | 1 |

### Test Coverage Requirements
Check that appropriate test types exist for each provider:

| Test Type | Database Features | Non-DB Features |
|-----------|-------------------|-----------------|
| **UnitTests** | Required | Required |
| **GuardTests** | Required | Required |
| **PropertyTests** | Required | If complex logic |
| **ContractTests** | Required | If public API |
| **IntegrationTests** | **Required** (Docker/Testcontainers) | Justify if skip |
| **LoadTests** | Only for concurrent features* | Justify if skip |
| **BenchmarkTests** | Only for hot paths** | Justify if skip |

*Concurrent features: Unit of Work, Multi-Tenancy, Read/Write Separation
**Hot paths: Replica selection algorithms, high-frequency operations

### Files to Check
- [ ] Source implementations in `src/Encina.*/`
- [ ] Unit tests in `tests/Encina.UnitTests/`
- [ ] Guard tests in `tests/Encina.GuardTests/`
- [ ] Property tests in `tests/Encina.PropertyTests/`
- [ ] Contract tests in `tests/Encina.ContractTests/`
- [ ] Integration tests in `tests/Encina.IntegrationTests/`
- [ ] Load tests in `tests/Encina.LoadTests/` (if concurrent feature)
- [ ] Benchmarks in `tests/Encina.BenchmarkTests/` (if hot path)

### Requested Analysis
1. **Duplicates**: Find any existing issues that may be duplicates
2. **Related Issues/PRs**: Link similar issues for context
3. **Implementation Plan**: Generate step-by-step implementation guidance
4. **Missing Coverage**: Identify any gaps in provider or test coverage
```

---

## Template: Quick Analysis

```markdown
@coderabbitai please analyze this issue:
- Check for duplicate issues
- Find related issues and PRs
- Verify 12-provider coverage requirements if database-related
- Suggest implementation approach
```

---

## Template: Plan Generation Only

```markdown
@coderabbitai plan

Please generate a detailed implementation plan for this issue, considering:
- All 12 database providers (if applicable)
- Required test types per CLAUDE.md guidelines
- Integration with existing codebase patterns
```

---

## Notes

- CodeRabbit automatically enriches issues when they are **created or edited**
- For existing issues without CodeRabbit analysis, use these templates to trigger analysis
- The `@coderabbitai plan` command generates implementation steps that can be used with Claude Code
- See `.coderabbit.yaml` for project-specific configuration

## Related Documentation

- [CLAUDE.md](../../CLAUDE.md) - Testing guidelines and provider requirements
- [.coderabbit.yaml](../../.coderabbit.yaml) - CodeRabbit configuration
- [GitHub Issues](https://github.com/dlrivada/Encina/issues) - Issue tracker
