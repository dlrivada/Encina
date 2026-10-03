<!--
title: [DEBT] Docs Sagas Configuration section omits explicit subset statement and issue link for 8 missing providers
labels: technical-debt
milestone: 
-->

## Type

- [ ] Failing tests
- [ ] Missing tests
- [ ] Code quality (warnings, analyzers)
- [ ] Performance optimization
- [ ] Refactoring needed
- [x] Documentation gap
- [ ] Incorrect implementation
- [ ] Other

## Description

The documentation for Sagas in `docs/messaging/sagas.md` is a provider-dependent database feature page under the AGENTS.md §5 database matrix, which covers all 10 providers (ADO.NET/Dapper/EF Core × SqlServer/PostgreSQL/MySQL, plus MongoDB). However, the "Configuration" section for Orchestration only lists two providers: `AddEncinaEntityFrameworkCore` and the non-existent `AddEncinaDapperSqlServer`. The page does not include an explicit statement regarding the subset of supported providers nor does it link to an issue tracking the missing providers. This fails SKILL.md §3 house rule 5 and the checklist's "Providers" row requirement for provider-dependent feature pages.

## Location

- **File(s)**: `docs/messaging/sagas.md`
- **Package(s)**: [Encina]

## Current Behavior

The "Configuration" section for Orchestration in `docs/messaging/sagas.md` displays exactly two providers (`AddEncinaEntityFrameworkCore`, and the non-existent `AddEncinaDapperSqlServer`) and states no explicit subset statement or link to an issue for the other 8 providers in the database matrix.

## Expected Behavior

The "Configuration" section should either list all supported providers, or include an explicit statement clarifying the current subset of supported providers and link to a relevant issue for the missing 8 providers, in compliance with SKILL.md §3 house rule 5 and the checklist's "Providers" row.

## Root Cause

The documentation was not updated to reflect the full provider matrix defined in AGENTS.md §5, and the specific rule requiring explicit subset statements or issue links for provider-dependent features was not applied during the docs stage.

## Proposed Fix

Update the "Configuration" section in `docs/messaging/sagas.md` to either add the missing provider configurations or insert a clear statement indicating that Sagas support is currently limited to the listed providers, accompanied by a link to the appropriate tracking issue for the remaining 8 providers.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [x] **Medium** - Should be fixed before 1.0 release
- [ ] **Low** - Nice to have, can be deferred

## Effort Estimate

- [x] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

#16