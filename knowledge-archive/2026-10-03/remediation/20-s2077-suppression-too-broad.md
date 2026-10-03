<!-- issue
title: [DEBT] SonarCloud S2077 suppression for Dapper/ADO/EF Core is package-wide instead of file-scoped
labels: technical-debt, area-ci-cd
milestone: v0.14.0 — Hardening
-->

Found by the SPEC-003 audit of #20.

## Type

- [ ] Failing tests
- [ ] Missing tests
- [x] Code quality (warnings, analyzers)
- [ ] Performance optimization
- [ ] Refactoring needed
- [ ] Documentation gap
- [ ] Incorrect implementation
- [ ] Other

## Description

Commit e1c68db (closing issue #20) added a SonarCloud suppression for rule `csharpsquid:S2077` (SQL injection) justified as: "Store files use `SqlIdentifierValidator.ValidateTableName()` which validates table names against a safe regex pattern... All data values use parameterized queries." The suppression as written in `.github/workflows/sonarcloud.yml:69-72` is a package-wide wildcard (`resourceKey="src/Encina.Dapper.*/**/*.cs"` and `"src/Encina.ADO.*/**/*.cs"`), not scoped to the store/builder files that actually call the validator, and a second wildcard at lines 135-136 covers all of `src/Encina.EntityFrameworkCore/**/*.cs`. A sampled audit of `Encina.Dapper.SqlServer` and `Encina.ADO.SqlServer` found no currently exploitable unvalidated identifier, but the suppression's breadth means any new file added to these ~7 packages that builds SQL from an actually-unvalidated runtime string would silently inherit the exemption with no additional review step.

## Location

- **File(s)**: `.github/workflows/sonarcloud.yml` (lines 69-72, 135-136)
- **Package(s)**: Encina.Dapper.SqlServer, Encina.Dapper.PostgreSQL, Encina.Dapper.MySQL, Encina.ADO.SqlServer, Encina.ADO.PostgreSQL, Encina.ADO.MySQL, Encina.EntityFrameworkCore

## Current Behavior

The S2077 SonarCloud rule is disabled for every `.cs` file in the listed packages (~65 files per package), regardless of whether that file builds SQL from a runtime-supplied, unvalidated identifier.

## Expected Behavior

The S2077 suppression covers only the specific files that legitimately build SQL from a value already validated by `SqlIdentifierValidator.ValidateTableName()` (or an equivalent constant/attribute-sourced value). A new file added anywhere else in these packages is not silently exempted.

## Root Cause

The suppression was written at package granularity for expediency when issue #20 closed, rather than being scoped to the specific store/builder files the justification actually describes.

## Proposed Fix

Inventory the files in each of the 7 packages that build SQL from a runtime identifier and call `SqlIdentifierValidator` (directly, or indirectly through an already-validated `TableName`/`ColumnName` cached at construction time, as `EntityMappingBuilder.cs` does). Replace the package-wide wildcard `resourceKey` with per-folder or per-file globs limited to those files (e.g. `src/Encina.Dapper.SqlServer/{Outbox,Inbox,Sagas,Scheduling,Auditing,Anonymization,Repository,SoftDelete,Tenancy,Pagination}/*.cs`), repeated for each of the 7 packages.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [ ] **Medium** - Should be fixed before 1.0 release
- [x] **Low** - Nice to have, can be deferred

## Effort Estimate

- [ ] Small (< 1 hour)
- [x] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

Found by the SPEC-003 audit of #20. Related pattern (over-broad SonarCloud exclusions from the same era): #1335 (CPD exclusions unverifiable, from the audit of #12).
