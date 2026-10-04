<!--
title: [DEBT] Hand-typed contract test counts on six feature pages carry no date, command or citation
labels: technical-debt
milestone:
kind: docs
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

Six feature pages under `docs/features/` state a contract-test count typed by hand, with no date, no command and no citation:

- `docs/features/cdc-debezium.md:372`: "Contract Tests | ~24"
- `docs/features/cdc-sharding.md:389`: "Contract Tests | 14"
- `docs/features/cdc.md:781`: "~47 contract tests"
- `docs/features/gdpr-compliance.md:300`: "ContractTests | 16"
- `docs/features/id-generation.md:279`: "Contract Tests | 43"
- `docs/features/lawful-basis-validation.md:489`: "Contract Tests | 26"

The documentation rule that fails is `.claude/skills/encina-docs/SKILL.md` section 3, rule 2: "Counts that no dashboard measures (number of packages, providers, tests) go in a sentence with the date and the command that produced them, or are left out."

## Location

- **File(s)**: `docs/features/cdc-debezium.md`, `docs/features/cdc-sharding.md`, `docs/features/cdc.md`, `docs/features/gdpr-compliance.md`, `docs/features/id-generation.md`, `docs/features/lawful-basis-validation.md`
- **Package(s)**: none (documentation only)

## Current Behavior

The six figures drift from the test source. `id-generation.md:279` says 43, while `Select-String '^\s*\[(Fact|Theory)'` over `tests/Encina.ContractTests/IdGeneration` finds 19 attributes, all in `IdGeneratorContractTests.cs` (theory expansion and tests elsewhere were not measured, so the true count is not established). `cdc-sharding.md:389` says 14 and the same search over `tests/Encina.ContractTests/Cdc/Sharding` finds 14 attributes today, in `IShardedCdcPositionStoreContractTests.cs`; that figure is right now but equally unsourced. The `~` figures (`cdc-debezium.md:372`, `cdc.md:781`), `gdpr-compliance.md:300` and `lawful-basis-validation.md:489` were not measured.

## Expected Behavior

No page carries an unsourced test count. Each line is either removed, or replaced by a sentence that names the date and the command that produced the figure, so a reader can tell how old it is and reproduce it.

## Root Cause

The counts are typed literals and nothing ties them to the test source, so they stay unchanged while tests are added or removed. No dashboard measures test counts for these pages, which is why the documentation rule requires a date and a command instead of a citation.

## Proposed Fix

For each of the six lines, either delete the count (and the row if it holds nothing else) or rewrite it as a dated sentence such as "N `[Fact]`/`[Theory]` attributes under `tests/Encina.ContractTests/<folder>` on <date>, counted with `Select-String '^\s*\[(Fact|Theory)' -Path ... -Recurse`", with the figure measured at the time of the edit. Do not measure only the folder named in the current row: decide per page which test folders the count is meant to cover, and say it in the sentence.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [x] **Medium** - Should be fixed before 1.0 release
- [ ] **Low** - Nice to have, can be deferred

## Effort Estimate

- [x] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

- #29 (This issue)
