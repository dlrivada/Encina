<!--
title: [DEBT] Six feature pages state contract-test counts typed by hand, with no date, command or citation
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

Six feature pages state how many contract tests exist as a bare number typed by hand:

| Page | Line | Text |
|---|---|---|
| `docs/features/cdc-debezium.md` | `:372` | `Contract Tests \| ~24` |
| `docs/features/cdc-sharding.md` | `:389` | `Contract Tests \| 14` |
| `docs/features/cdc.md` | `:781` | `~47 contract tests` |
| `docs/features/gdpr-compliance.md` | `:300` | `ContractTests \| 16` |
| `docs/features/id-generation.md` | `:279` | `Contract Tests \| 43` |
| `docs/features/lawful-basis-validation.md` | `:489` | `Contract Tests \| 26` |

The documentation rule that fails is `.claude/skills/encina-docs/SKILL.md` section 3, rule 2: counts that no dashboard measures (number of packages, providers, tests) go in a sentence with the date and the command that produced them, or are left out. None of the six lines carries a date, a command or a citation.

The figures drift. `id-generation.md:279` says 43, while `Select-String '^\s*\[(Fact|Theory)'` over `tests/Encina.ContractTests/IdGeneration` finds 19 `[Fact]`/`[Theory]` attributes, all in one file, `IdGeneratorContractTests.cs` (theory expansion and tests outside that folder were not measured, so the true count is not established). `cdc-sharding.md:389` says 14 and the same search over `tests/Encina.ContractTests/Cdc/Sharding` finds 14 attributes today, in `IShardedCdcPositionStoreContractTests.cs`: that one is right now but equally unsourced. The `~` figures at `cdc-debezium.md:372` and `cdc.md:781` were not measured.

The tables that hold these rows type the neighbouring counts by hand as well: `id-generation.md:277-281` (`Unit Tests | 199`, `Guard Tests | 4`, `Property Tests | 19`, `Integration Tests | 14 files`), `lawful-basis-validation.md:485-488`, `cdc-debezium.md:370-374` and `cdc-sharding.md:387-390`.

## Location

- **File(s)**: `docs/features/cdc-debezium.md:372`, `docs/features/cdc-sharding.md:389`, `docs/features/cdc.md:781`, `docs/features/gdpr-compliance.md:300`, `docs/features/id-generation.md:279`, `docs/features/lawful-basis-validation.md:489`
- **Package(s)**: None (documentation only)

## Current Behavior

Each page presents a test count as a fact, with nothing that tells the reader when or how it was measured. At least one of them (`id-generation.md:279`) disagrees with what a search of the contract test folder finds today.

## Expected Behavior

No page types a test count that no dashboard measures. A count that is kept appears in a sentence with the date and the command that produced it; otherwise the row or sentence is removed.

## Root Cause

Not established. No date is verified for when each count was written or for when the tests it counted changed.

## Proposed Fix

For each of the six lines, either delete the count (keep the row's description and the path of the test folder, as `cdc-sharding.md:389` already does), or replace it with a sentence that carries the date and the command, for example the `Select-String` search above over the folder named in the row. Apply the same choice to the neighbouring rows of the same tables so no table mixes sourced and unsourced counts. Do not type any coverage figure by hand.

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
