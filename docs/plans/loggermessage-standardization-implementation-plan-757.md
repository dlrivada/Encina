# Implementation Plan: Standardize the `[LoggerMessage]` Pattern (Remove Every Direct `ILogger.Log*()` Call)

> **Issue**: [#757](https://github.com/dlrivada/Encina/issues/757)
> **Type**: Feature (technical-debt program)
> **Complexity**: High by volume, low per change (21 pull requests, each a mechanical conversion)
> **Estimated Scope**: 616 call conversions in 89 files of 33 packages, 21 `#pragma warning disable CA1848` sites to remove, about 33 new `EventIdRanges` entries in the 10000+ zone; about 6,000-8,000 changed lines in total, at most about 1,300 per pull request
> **Specification**: [ADR-021](../architecture/adr/021-eventid-uniqueness-enforcement.md) (EventId ranges and the 10000+ zone); `AGENTS.md` sections 7 and 8; [EventId block registry](../engineering/eventid-block-registry.md)
> **Related**: [#4007](https://github.com/dlrivada/Encina/issues/4007) (migration of the ranges below 10000 into the zone), [#1120](https://github.com/dlrivada/Encina/issues/1120) (the EventId allocation architecture test), [#1788](https://github.com/dlrivada/Encina/issues/1788) (error text in spans and logs)

---

## Summary

The issue says that newer caching, distributed-lock and transport packages log through `ILogger.Log*()` directly. Measured on `origin/main` today, that is not true: those packages already use `[LoggerMessage]` (the caching providers with a logger, the lock providers, the 10 transports). The real remaining set is different and larger: **616 direct calls in 89 files of 33 packages**, found with a search for the `LogTrace|LogDebug|LogInformation|LogWarning|LogError|LogCritical` extension calls and `Log(LogLevel...)` outside comments. Another 21 files hide further sites behind `#pragma warning disable CA1848` or a `SuppressMessage`, so the analyzer alone cannot list them (Phase 1). This plan converts all of them and then makes the build refuse new ones.

| Family | Packages (calls) |
|--------|------------------|
| Sharding data layer | ADO.SqlServer, ADO.PostgreSQL, ADO.MySQL, Dapper.SqlServer, Dapper.PostgreSQL, Dapper.MySQL, EntityFrameworkCore (22 each), MongoDB (32) |
| Core and cross-cutting | `Encina` (82: sharding 70, modules 12), Caching (18), Cdc (2), AspNetCore (4), AwsLambda (8), DomainModeling (7) |
| Compliance modules | DataResidency (53), LawfulBasis (38), BreachNotification (37), Retention (27), CrossBorderTransfer (24), ProcessorAgreements (20), DPIA (14), DataSubjectRights (14), Consent (9), PrivacyByDesign (3), AIAct (3), NIS2 (2), Attestation (1) |
| Security and audit | Audit.Marten (29), Security.ABAC (19), Security.Encryption (9), Security.Sanitization (4), Security.PII (2), Security.Secrets (1) |

1. **Audit first (Phase 1).** A local build with `CA1848` as a warning for `src/` and every `#pragma warning disable CA1848` and `SuppressMessage` for it stripped lists every site; the counts above come from a text search.
2. **Convert package by package (Phases 2 to 5).** Each direct call becomes a `[LoggerMessage]` method in the package's log class with the same level, template and parameters, with ids from a new range of the 10000+ zone registered in the pull request that first uses it (Design Choice 4); the pragma of each converted file is deleted in the same pull request.
3. **Enforce (Phase 6).** After the last conversion, `CA1848` becomes a warning for `src/` so a new direct call fails the zero-warnings build (Design Choice 2).
4. **Document (Phase 8).** A developer guide page on EventId allocation closes the last acceptance criterion.

**Standards**: ADR-021; `AGENTS.md` section 7 (EventIds, the registry), section 8 (zero warnings, CA1848), section 3 (an exception passed to a logger goes through `ForLogging()`).

**Affected packages**: the 33 above; no new package.

**Provider category**: Database (10) for the sharding data layer: the three ADO and the three Dapper repositories, the one EF Core repository class (it serves the three EF Core databases) and MongoDB are converted in pull requests 1 to 3 so the provider coherence rule holds. Other families are provider-independent.

**What the code does today** (`origin/main` at `039cd3524`):

- `.editorconfig` sets `dotnet_diagnostic.CA1848.severity = suggestion` ("Future optimization"). `GlobalSuppressions.cs` of `Encina.AwsLambda`, `Encina.AzureFunctions`, `Encina.DistributedLock` and `Encina.DomainModeling` suppress `CA1848`, and 17 source files carry `#pragma warning disable CA1848`: `Encina/Sharding/Resharding/**` (six phases and the orchestrator), `ShardedQueryExecutor`, `ShardedMigrationCoordinator`, `ModuleLifecycleHostedService`, `ShardRoutingMetrics`, `ShardedReadWriteMetrics`, the four `Encina.Caching/Sharding` classes and `ShardedMongoDbDatabaseHealthMonitor` (three of those files, `ShardRoutingMetrics`, `ShardedReadWriteMetrics` and the Mongo monitor, hold no counted call and are touched only to delete the pragma).
- Caching providers (`Memory`, `Redis`, `Hybrid`), lock providers (`InMemory`, `Redis`, `SqlServer`) and the transports already declare `[LoggerMessage]` methods; `Encina.Caching.Dragonfly`, `Garnet`, `KeyDB`, `Valkey` and `Encina.DistributedLock` have no logger (they reuse the Redis provider); `Encina.Caching` itself has 18 direct calls in its sharding classes.
- Messages carry parameters such as request ids and `VerifiedBy` (the DSR service); only 2 of the 616 calls pass an exception, and those must go through `ForLogging()` (`src/Encina/Diagnostics/RedactedException.cs`).
- Free ids inside the existing ranges cannot be trusted: sibling plans on `main` already claim ids in nearly every range this work touches (`MongoDB` 3100-3199 has only 3199 left after #609, #594, #590 and #583; `Caching` 3500-3549 is claimed by the cache plans #266, #272, #274 and #275), ten ranges have fewer free ids than calls (Core 23 vs 82, DataResidency 7 vs 53, BreachNotification 8 vs 37, LawfulBasis 13 vs 38, Retention 3 vs 27, Audit.Marten 7 vs 29, ProcessorAgreements 12 vs 20, ABAC 0 vs 19, DPIA 3 vs 14, DataSubjectRights 0 vs 14), and `Encina.Security.Encryption` has no range. The registry page `docs/engineering/eventid-block-registry.md` has no block in the 10000+ zone yet.
- `EventIdUniquenessRule` (`src/Encina.Testing.Architecture/EventIdUniquenessRule.cs`) asserts that every `[LoggerMessage]` has an EventId inside a range mapped to its assembly; `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (`AssemblyRanges`) and `EventIdRegistryAgreementTests.cs` (rule 5: nothing below 10000 unless historical) keep the registry and `EventIdRanges.cs` in agreement. No test or analyzer forbids a direct call.
- Almost every touched call-site file has only a `defaultRule` in its package manifest (no explicit `targets`), so Phase 7 gives it numeric targets in the same pull request; `EventIdRanges.cs` has an entry in `.github/coverage-manifest/Encina.json`.
- `Encina.AwsLambda` has `Log.cs` but no `InternalsVisibleTo` for `Encina.UnitTests`; the other packages of this plan have it.

---

## Design Choices

<details>
<summary><strong>1. Scope: which direct calls are converted</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Convert every direct call found by the audit (616 in 33 packages today) and then enforce** | Meets the acceptance criterion "no direct `ILogger.Log*()` calls remain"; the package list of the issue is not what the code shows, the measured list is | 21 pull requests; touches compliance code that logs identifiers (Design Choice 3) |
| **B) Convert only the packages the issue names (caching providers, lock providers, newer transports)** | Matches the issue text literally | Those packages have 0 direct calls, so there is nothing to convert; the criterion stays false for 33 packages |
| **C) Enforce for new code only: `CA1848` as a warning with a baseline of the existing files** | One small pull request stops new direct calls | Leaves 616 calls and a baseline list that is a compatibility layer (`AGENTS.md` section 1) |

### Chosen Option: **A — convert all, then enforce** (recommended, pending the maintainer)

### Rationale

- A wins because the acceptance criteria are about the result in the code, and the measured gap is outside the packages the issue lists; C wins only if the maintainer wants the enforcement before the 1.0 push and accepts the debt as a tracked baseline; B closes the issue without effect. The pull request table of Implementation Phases is the plan for option A; under C only its last pull request changes.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. How a new direct call is prevented</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `.editorconfig`: `dotnet_diagnostic.CA1848.severity = warning` in a `[src/**.cs]` section (tests stay at suggestion), and delete every local suppression and pragma** | Zero new code; the zero-warnings build already enforces it for every contributor and every agent; the analyzer knows every logging overload | Changes the meaning of `AGENTS.md` section 8 ("CA1848 may be suppressed when the optimisation is future work"), a text change that belongs to the maintainer |
| **B) A new rule in `Encina.Testing.Architecture` that scans assemblies for calls to the `LoggerExtensions.Log*` methods** | Works inside the architecture test suite; reusable by applications; sees through pragmas | New public API and an IL scan to write, test and maintain, for what the analyzer already does |
| **C) Documentation only** | No change | Nothing stops the next direct call |

### Chosen Option: **A — analyzer severity** (recommended, pending the maintainer)

### Rationale

- A wins for a repository that already treats warnings as build failures; B wins if a consumer application should be able to run the same check on its own assemblies. A is complete only because every pragma and `SuppressMessage` of `CA1848` is deleted in the pull request that converts its file (the last pull request deletes the remaining `GlobalSuppressions.cs` lines); a pragma left behind would hide a new call. The proposed `AGENTS.md` section 8 edit is listed for the maintainer in Next Steps; the worker does not edit `AGENTS.md`.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Content of the converted messages: preserve, or fix levels and identifiers in the same pull request</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Preserve each call exactly: same level, same template text, same parameters (names and order), exceptions through `ForLogging()`** | A pure refactor, reviewable line by line; no behavior change to explain | The acceptance criterion "consistent log levels across subsystems" is not met by this plan; messages that carry identifiers (the DSR request id and `VerifiedBy`, tenant or subject values in some compliance services) keep carrying them |
| **B) Convert and, in the same pull request, normalize levels and drop identifiers** | Meets the criterion and the identifier concern in one pass | Mixes behavior changes into 21 mechanical pull requests; each level change needs a justification a reviewer cannot check from the diff alone |

### Chosen Option: **A — preserve** (recommended, pending the maintainer)

### Rationale

- A wins because a mechanical conversion is the only kind of change a reviewer can verify at this volume; B wins if the maintainer wants the level policy delivered with the conversion and accepts slower reviews. Under A the audit of Phase 1 also lists every message whose parameters are identifiers, and the follow-up issue file `loggermessage-level-and-identifier-policy` (Next Steps) carries the level policy and the identifier review as their own change.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Where the new EventIds come from</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Every package that receives converted messages gets a new range in the 10000+ zone, registered in the pull request that first uses it, whatever its old range has left** | No dependence on claims of other plans in the old ranges (the sibling plans overbook MongoDB, Caching and others); one rule for every package; ids never collide | About 33 registrations (a 50-id slot each, 100 for the larger ones); the old range keeps unused ids |
| **B) Use the old range where free ids remain and a new zone range only where they do not** | Fewer registrations | The free count is not reliable: other plans claim the same ids, and a claim that merges first breaks the conversion later; the audit of free ids would repeat per pull request |
| **C) Borrow ids from another package's range, or reuse an id for two messages** | No registration | Forbidden by `AGENTS.md` section 7 and by `EventIdUniquenessRule` |

### Chosen Option: **A — a new zone range per package** (recommended, pending the maintainer)

### Rationale

- A wins because the id budget of the old ranges is contested by plans that have not merged; B wins if the maintainer prefers fewer ranges and accepts re-checking free ids at each pull request. C is not valid.
- A registration is `src/Encina/Diagnostics/EventIdRanges.cs`, the `PublicAPI.Unshipped.txt` of `Encina` for each new field, the "Registered ranges" and "Blocks" rows of the registry page (Zone `zone`, block status `registered`, the registered row never `reserved-pre-zone`) and the `AssemblyRanges` map of `EncinaEventIdAllocationTests`; the four files are shared by every range of one pull request. Slots are 50 ids, 100 for `Core2` and DataResidency, read from the registry page when the pull request opens: Database area (base 20000) for ADO, Dapper, EF Core and MongoDB; Caching (30000) for `Encina.Caching`; Core and messaging (10000) for `Encina`, Cdc, DomainModeling and Audit.Marten; Observability and platform (60000) for AspNetCore; Cloud and integration (80000) for AwsLambda; Security and compliance (50000) for the compliance and security packages. Names follow `<ExistingName>2` (`<ExistingName>Extra` when the name ends in a digit).

**Class:** Dilemma

</details>

<details>
<summary><strong>5. File and class naming of the converted messages</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Put the new methods into the package's existing log class and file (`Diagnostics/*LogMessages.cs`, `Log.cs` or whatever the package uses); a package with none gets `Diagnostics/<Package>LogMessages.cs`** | Smallest diff; follows `AGENTS.md` section 7 (`Diagnostics/*LogMessages.cs`); no renames | The packages do not share one file name, so the issue's criterion "all packages use `Log.cs`" is not met literally |
| **B) Rename every log class to `Log.cs`** | Meets the criterion literally | About 40 file renames and class renames with no behavior gain, against the `*LogMessages.cs` convention of `AGENTS.md` section 7 and of the existing `ReshardingLogMessages`, `DPIALogMessages` and `Encina.MongoDB/Log.cs` mix |

### Chosen Option: **A — keep each package's convention** (recommended, pending the maintainer)

### Rationale

- A wins because the criterion's intent (one structured pattern with registered ranges) is met without churn; B wins if the maintainer wants a literal file name for tooling or search.

**Class:** Dilemma

</details>

### Register of defaults and semantics

| Id | Choice | Class | Settled by |
|----|--------|-------|------------|
| C1 | No `EncinaError` is created, so there is no ADR-062 `ErrorCategory` to assign | Settled | ADR-062 (a category is chosen where an error is created) |
| C2 | The converted message uses the call's own level and template; the `Exception` parameter, where there is one, receives `exception.ForLogging()` under every option of Design Choice 3 (under option B the level and parameters may also change) | Settled | `AGENTS.md` section 3 (an exception passed to a logger goes through `ForLogging()`) |
| C3 | `LoggerMessage.Define` code is left as it is (one literal `new EventId(...)` per call, scanned by the test); only direct `ILogger.Log*()` calls are converted | Settled | `AGENTS.md` section 7 (existing `LoggerMessage.Define` calls stay; new code uses the generator) |
| C4 | Each pull request is at most 15 files and 1500 changed lines, builds and passes alone, and a range-registering pull request lands before the ones that use the range; the number of pull requests in the table is the plan under Design Choice 1 option A | Settled | `AGENTS.md` section 11 (at most 15 files and 1500 changed lines per pull request) |
| C5 | No metric, span or tag is added or changed by any pull request under any option; a converted message never adds a tenant or subject value that the call did not already log | Settled | ADR-054 D4 (tenant tags exist only where tenant data exists); `AGENTS.md` section 3 |
| C6 | A touched method must keep CRAP at or below 10; the pull request measures the touched methods first and adds tests in the same pull request where a touched method is below the bar | Settled | `AGENTS.md` section 9 (CRAP gate on changed methods) |
| C7 | Each converted file's `#pragma warning disable/restore CA1848` pair is deleted in the pull request that converts it; the `SuppressMessage` lines of `GlobalSuppressions.cs` are deleted in the pull request that converts that package (AwsLambda, DomainModeling) or in the last one (AzureFunctions, DistributedLock) | Trivial | Design Choice 2 option A is incomplete otherwise; each deletion sits next to its own conversion |

---

## Implementation Phases

**Pull request table.** Files are counted by name: call-site files, pragma-only files, log classes (modified, or new where noted), one coverage manifest per package touched (`.github/coverage-manifest/<Package>.json`, numeric targets for the touched files per Phase 7), `GlobalSuppressions.cs` where its lines go, one unit test class, and for every pull request that registers ranges: `src/Encina/Diagnostics/EventIdRanges.cs`, `src/Encina/PublicAPI.Unshipped.txt`, `docs/engineering/eventid-block-registry.md`, `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` and the `.github/coverage-manifest/Encina.json` entry of `EventIdRanges.cs` (the last one is the package manifest itself in the pull requests that touch `Encina` files). Every row is at most 15 files; the line estimate is about 14 changed lines per conversion (attribute, signature, call site, test), so rows 1 and 2 are about 1,100-1,300 lines and the others under 900: if a row passes 1,500 lines it splits by provider.

| PR | Packages (call files + pragma-only files; calls) | Ranges registered (zone) | Files |
|----|--------------------------------------------------|--------------------------|------:|
| 1 | ADO.SqlServer, ADO.PostgreSQL, ADO.MySQL (3; 66) | `ADOSqlServer2`, `ADOPostgreSQL2`, `ADOMySQL2` | 15 |
| 2 | Dapper.SqlServer, Dapper.PostgreSQL, Dapper.MySQL (3; 66) | `DapperSqlServer2`, `DapperPostgreSQL2`, `DapperMySQL2` | 15 |
| 3 | EntityFrameworkCore (1; 22), MongoDB (1 + the monitor pragma; 32) | `EntityFrameworkCore2`, `MongoDB2` | 13 |
| 4 | Caching (4; 18), Cdc (1; 2) | `Caching2`, `Cdc2` | 15 |
| 5 | AwsLambda (2; 8, plus `GlobalSuppressions.cs` and the `InternalsVisibleTo` for `Encina.UnitTests` in its csproj), AspNetCore (1; 4) | `AwsLambda2`, `AspNetCore2` | 15 |
| 6 | `Encina` sharding: Execution, Migrations, ReferenceTables x2 (4) and the pragma-only `ShardRoutingMetrics`, `ShardedReadWriteMetrics` (31 calls; new `Diagnostics/ShardingLogMessages.cs`) | `Core2` (100 ids) | 13 |
| 7 | `Encina` sharding: the six resharding phases and the orchestrator (7; 39) | uses PR 6 | 10 |
| 8 | `Encina` modules (1; 12; new `Diagnostics/ModuleLogMessages.cs`), DomainModeling (1; 7, plus `GlobalSuppressions.cs`) | `DomainModeling2` | 12 |
| 9 | DataResidency, first 5 files (24) | `ComplianceDataResidency2` (100 ids) | 13 |
| 10 | BreachNotification, first 5 files (32) | `ComplianceBreachNotification2` | 13 |
| 11 | DataResidency, last 5 files (29) and BreachNotification, last 5 files (5) | uses PRs 9 and 10 | 15 |
| 12 | LawfulBasis (1; 38), Retention (3; 27) | `ComplianceLawfulBasis2`, `ComplianceRetention2` | 14 |
| 13 | CrossBorderTransfer (4; 24), Consent (1; 9) | `ComplianceCrossBorderTransfer2`, `ComplianceConsent2` | 15 |
| 14 | ProcessorAgreements (3; 20), PrivacyByDesign (1; 3) | `ComplianceProcessorAgreements2`, `CompliancePrivacyByDesign2` | 14 |
| 15 | DPIA (2; 14), DataSubjectRights (3; 14) | `ComplianceDPIA2`, `ComplianceDSR2` | 15 |
| 16 | AIAct (1; 3), NIS2 (1; 2), Attestation (1; 1) | `ComplianceAIAct2`, `ComplianceNIS2Extra`, `ComplianceAttestation2` | 15 |
| 17 | Audit.Marten (7; 29) | `AuditMarten2` | 15 |
| 18 | Security.ABAC (4; 19), Security.Secrets (1; 1) | `SecurityABAC2`, `SecuritySecrets2` | 15 |
| 19 | Security.Encryption (2; 9; new `Diagnostics/EncryptionLogMessages.cs`), Security.PII (2; 2) | `SecurityEncryption`, `SecurityPII2` | 14 |
| 20 | Security.Sanitization (3; 4) | `Sanitization2` | 11 |
| 21 | Enforcement and guide: `.editorconfig`, `GlobalSuppressions.cs` of AzureFunctions and DistributedLock, guide page, changelog fragment | none | 6 |

Order: PR 6 before 7 and 8; PRs 9 and 10 before 11; every other pull request is independent and can open in any order, in batches of three or four. PRs 1 to 20 each also remove the pragmas of their files (C7).

### Phase 1: Audit and Baseline

> **Goal**: The exact list of sites, per package, with the pragmas stripped.

<details>
<summary><strong>Tasks</strong></summary>

1. `git grep -n "CA1848" -- src` for `#pragma warning disable` and `SuppressMessage` (21 files today, listed in the Summary) and the same search for `CA2254` and `CA1873` pragmas that hide logging calls
2. Build `Encina.slnx` once with a temporary local copy of the files with their `CA1848` pragmas removed and `dotnet_diagnostic.CA1848.severity = warning` under `src/`, and write the warnings per file to `artifacts/logging-audit/` (not committed)
3. Compare with the table of this plan; where a package differs by more than 10 calls, update the pull request table before starting
4. List every message whose parameters are identifiers (request, subject, user, tenant values) into the follow-up issue file of Design Choice 3 (a worker writes the list; the local model drafts the body)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #757: audit the direct ILogger.Log*() calls.

CONTEXT:
- .editorconfig sets CA1848 to suggestion. The plan counts 616 direct calls in 89 files with a text search and 21 files with a CA1848 pragma or suppression;
  the analyzer cannot see sites inside a pragma, so the pragmas must be stripped for the audit.
- Nothing is committed in this phase except an updated pull request table in the plan when the counts differ.

TASK:
Find every pragma and SuppressMessage of CA1848, build with CA1848 as a warning for src/ and the pragmas removed in a temporary copy, group the warnings by
package and file, and list the messages with identifier parameters.

KEY RULES:
- Outputs under artifacts/ only; no code change; never lower or raise any other analyzer.

REFERENCE FILES:
- .editorconfig
- docs/engineering/eventid-block-registry.md
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

### Phase 2: Database Providers, Caching, Cdc, Cloud and AspNetCore (pull requests 1 to 5)

> **Goal**: The sharding data layer and the cross-cutting packages log through the generator.

<details>
<summary><strong>Tasks</strong></summary>

1. **PRs 1 and 2**: `Sharding/FunctionalShardedRepositoryADO.cs` and `FunctionalShardedRepositoryDapper.cs` of the six packages; the same 22 messages in every provider keep the same templates so the 10-provider behavior stays coherent; each package registers its second range in the Database area of the zone
2. **PR 3**: `FunctionalShardedRepositoryEF.cs` and `FunctionalShardedRepositoryMongoDB.cs` (32 messages) plus the pragma of `ShardedMongoDbDatabaseHealthMonitor.cs`; ranges `EntityFrameworkCore2` and `MongoDB2`
3. **PR 4**: the four `Encina.Caching/Sharding` classes (range `Caching2`, Caching area) and `Encina.Cdc/Sharding/CdcDrivenRefreshHandler.cs` (`Cdc2`, Core and messaging area)
4. **PR 5**: `EventBridgeHandler` and `SqsMessageHandler` (`AwsLambda2`, Cloud area), `HttpRegionContextProvider` (`AspNetCore2`, Observability and platform area); add `<InternalsVisibleTo Include="Encina.UnitTests" />` to `Encina.AwsLambda.csproj` so the unit test can call the internal `Log` methods (the other packages already have it); delete the CA1848 line of `Encina.AwsLambda/GlobalSuppressions.cs`
5. Per pull request: each converted call uses the next id of the package's new range, packed from its first id; one unit test class (Phase 7); the package manifests get numeric targets for the touched files (Phase 7); the pragma pairs of the converted files are deleted

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #757: convert the sharding data layer, Caching, Cdc, AwsLambda and AspNetCore.

CONTEXT:
- Each file has direct _logger.LogXxx(...) calls and, in some of them (the four Caching classes and the Mongo monitor here), a #pragma warning disable CA1848 pair. The package's log class is named in the plan.
- Design Choice 3 option A: same level, same template, same parameters; exceptions through ForLogging(). Design Choice 4 option A: ids come from a NEW zone range
  registered in this pull request (EventIdRanges.cs, PublicAPI.Unshipped.txt of Encina, the registry page, AssemblyRanges, the Encina.json entry), packed from its first id.

TASK:
For each file of the pull request, replace every direct call with a [LoggerMessage] extension method in the package's log class and call it from the same place, delete the pragma pair,
register the ranges, add the numeric manifest targets and the unit test class. Run the EventId architecture tests (EncinaEventIdAllocationTests, EventIdRegistryAgreementTests) and the package tests.

KEY RULES:
- No behavior change; the new ranges are in the 10000+ zone; the new registered row has Zone zone and the block row status registered; PublicAPI and manifest lines through mechanical-fixer.
- Measure CRAP of every touched method locally; add tests in the same pull request if one is above 10.
- Edit files with the Edit tool only.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/Sharding/FunctionalShardedRepositoryADO.cs
- src/Encina.MongoDB/Log.cs
- docs/engineering/eventid-block-registry.md
```

</details>

### Phase 3: Core Sharding, Modules and DomainModeling (pull requests 6 to 8)

> **Goal**: `Encina` core and DomainModeling log through the generator.

<details>
<summary><strong>Tasks</strong></summary>

1. **PR 6**: register `Core2` (100 ids, Core and messaging area of the zone); create `src/Encina/Diagnostics/ShardingLogMessages.cs` (internal static partial); convert `ShardedQueryExecutor`, `ShardedMigrationCoordinator`, `ReferenceTableReplicationService` and `ReferenceTableReplicator`; delete the pragmas of those and of the pragma-only `ShardRoutingMetrics` and `ShardedReadWriteMetrics`; the `Encina.json` entry gets numeric targets for `EventIdRanges.cs` and the touched files
2. **PR 7**: convert the six resharding phases and `ReshardingOrchestrator` (39 calls) into the same class with the next ids of `Core2`; delete their pragmas
3. **PR 8**: `ModuleLifecycleHostedService` (12 calls, new `Diagnostics/ModuleLogMessages.cs`, ids from `Core2`, pragma deleted) and `PortsAndAdapters` of DomainModeling (7 calls, its own range `DomainModeling2`, the CA1848 line of its `GlobalSuppressions.cs` deleted)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #757: convert Encina core (sharding, modules) and DomainModeling.

CONTEXT:
- The ranges of Core 100-199 are nearly full and contested, so the messages take the new range Core2 in the 10000+ zone (registered in PR 6, registry rules in AGENTS.md section 7).
- The registry page docs/engineering/eventid-block-registry.md and EventIdRegistryAgreementTests must agree with EventIdRanges.cs.

TASK:
Register the range, add the log classes, convert the files of the pull request in order (PR 6 before PR 7 and PR 8), delete the pragmas, add the unit test class and run the architecture tests.

KEY RULES:
- The new registered row has Zone zone; the block row has status registered; PublicAPI line for each new field through mechanical-fixer.
- Same level, template and parameters as the current call; exceptions through ForLogging(); no new metric or tag.

REFERENCE FILES:
- src/Encina/Sharding/Resharding/ReshardingOrchestrator.cs
- src/Encina.OpenTelemetry/Resharding/ReshardingLogMessages.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

### Phase 4: Compliance Modules (pull requests 9 to 16)

> **Goal**: The 13 compliance packages log through the generator.

<details>
<summary><strong>Tasks</strong></summary>

1. **PRs 9 to 11**: DataResidency and BreachNotification, 10 files each, split in halves (the large services `DefaultDataLocationService` 13 calls and `DefaultBreachNotificationService` 12 calls come with their tests); the first half of each registers the package's range, PR 11 uses both
2. **PRs 12 to 16**: LawfulBasis with Retention, CrossBorderTransfer with Consent, ProcessorAgreements with PrivacyByDesign, DPIA with DataSubjectRights, AIAct with NIS2 and Attestation; each package registers its own range as in the table
3. In `Encina.Compliance.DPIA` the plan of #763 takes EventIds 6350-6351 from another range, which this plan does not touch
4. One unit test class per pull request; the DSR, Consent and LawfulBasis services already have unit tests that drive the call sites

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #757: convert the compliance packages.

CONTEXT:
- Each package gets a new zone range in the Security and compliance area (base 50000), registered in the first pull request that uses it; the table names the ranges.
- Messages that carry identifiers are preserved as they are (Design Choice 3 option A) and listed in the follow-up issue file.

TASK:
For each pull request, register the ranges the table names, convert the files, delete the pragmas, add the numeric manifest targets and the unit test class, run the architecture tests and the package tests.

KEY RULES:
- Compliance gates fail closed: a conversion must not change which branch logs or whether an operation returns Left.
- Never log EncinaError.Message; keep the current parameters; exceptions through ForLogging().
- Edit files with the Edit tool only; registration files, manifests and PublicAPI lines are described to mechanical-fixer.

REFERENCE FILES:
- src/Encina.Compliance.DataResidency/Services/DefaultDataLocationService.cs
- src/Encina.Compliance.DataSubjectRights/Services/DefaultDSRService.cs
- docs/engineering/eventid-block-registry.md
```

</details>

### Phase 5: Audit and Security (pull requests 17 to 20)

> **Goal**: Audit.Marten and the security packages log through the generator.

<details>
<summary><strong>Tasks</strong></summary>

1. **PR 17**: Audit.Marten, 7 files (encryptor, stores, two key providers, two projections), range `AuditMarten2` (Core and messaging area)
2. **PR 18**: Security.ABAC (four files, one of them `ServiceCollectionExtensions.cs`) and Security.Secrets
3. **PR 19**: Security.Encryption (new `Diagnostics/EncryptionLogMessages.cs` and its manifest entry; key-handling messages keep their text and never include key material) and Security.PII
4. **PR 20**: Security.Sanitization (three files)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #757: convert Audit.Marten and the security packages.

CONTEXT:
- Event-sourced and security code: tests mock IAggregateRepository or the stores (NSubstitute); no InMemory store is introduced.
- Each package registers a new zone range in this pull request (Core and messaging area for Audit.Marten, Security and compliance for the others).

TASK:
Convert the files, delete the pragmas, register the ranges the table names, add the numeric manifest targets and the unit test class, and run the architecture tests.

KEY RULES:
- Never log key material, plaintext or EncinaError.Message; preserve the current parameters; exceptions through ForLogging().
- Fail-closed behavior of the security gates is untouched.

REFERENCE FILES:
- src/Encina.Audit.Marten/MartenOperationAuditStore.cs
- src/Encina.Security.Encryption/EncryptionOrchestrator.cs
- src/Encina.Security.ABAC/ABACPolicySeedingHostedService.cs
```

</details>

### Phase 6: Enforcement (pull request 21)

> **Goal**: A new direct call fails the build.

<details>
<summary><strong>Tasks</strong></summary>

1. **`.editorconfig`** (modify) — a section `[src/**.cs]` with `dotnet_diagnostic.CA1848.severity = warning`; the global `suggestion` line stays for tests and samples
2. **`GlobalSuppressions.cs`** of `Encina.AzureFunctions` and `Encina.DistributedLock` (modify) — delete the `CA1848` suppressions (those of AwsLambda and DomainModeling went with PRs 5 and 8); any site they hid is converted in this pull request if it is new, or reported
3. A full `dotnet build Encina.slnx --configuration Release` with zero warnings is the acceptance proof; a search for `#pragma warning disable CA1848` and `SuppressMessage` for `CA1848` under `src/` returns nothing
4. Proposed text for `AGENTS.md` section 8 is left in Next Steps (the maintainer edits `AGENTS.md`)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of issue #757: enforce [LoggerMessage] with the analyzer.

CONTEXT:
- Phases 2 to 5 converted every site and deleted every pragma; CA1848 is currently a suggestion.

TASK:
Make CA1848 a warning for src/ only, remove the two remaining local suppressions, build the whole solution and fix any remaining site.

KEY RULES:
- Zero warnings in the final build; tests keep the suggestion severity; do not edit AGENTS.md.

REFERENCE FILES:
- .editorconfig
- src/Encina.AzureFunctions/GlobalSuppressions.cs
- src/Encina.DistributedLock/GlobalSuppressions.cs
```

</details>

### Phase 7: Testing

> **Goal**: Every converted message is proved, every flag at its target.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit**: per pull request, one class `<Family>LogMessagesTests` calling each new log method with a `FakeLogger` and asserting the EventId, level, template and parameter names; the existing service and repository tests keep driving the converted call sites; for AwsLambda the `InternalsVisibleTo` of PR 5 makes the internal `Log` methods reachable
2. **Guard**: no new public method or constructor, so no new guard test; the log classes are internal
3. **Contract, property, integration**: not applicable (no public interface, no invariant, no infrastructure); justification: the sharding repositories have integration tests of their own that run the converted call sites; no DI registration is added, so there is no DI test
4. **Architecture**: `EncinaEventIdAllocationTests`, `EventIdRegistryAgreementTests` and `EventIdUniquenessRule` pass in every pull request
5. **Targets**: each touched call-site file has only a `defaultRule` in its manifest today, so each pull request writes numeric unit and guard targets for it in that package's manifest, equal to the figure measured before the change (with a one-sentence justification; never lowered afterwards, never zero while the file has coverable lines); the entry of `EventIdRanges.cs` in `Encina.json` gets numeric targets in every range-registering pull request (the file is a list of constants, so the target is the measured figure of the unit flag, justified as "constants covered by the architecture tests"); the log classes carry no targets (source-generated), as `ReshardingLogMessages.cs` does, and the three new log classes (`ShardingLogMessages.cs`, `ModuleLogMessages.cs`, `EncryptionLogMessages.cs`) get manifest entries with no flags

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of issue #757: tests of the converted messages.

CONTEXT:
- Tests execute real log methods; assertions use Shouldly through Encina.Testing.Shouldly; the logger is a FakeLogger.
- Coverage is per flag against .github/coverage-manifest/<Package>.json; targets never go down.

TASK:
Write the unit test class of each pull request, measure the per-file table of the touched files before and after the change with
`dotnet test tests\Encina.UnitTests --collect "XPlat Code Coverage" --results-directory artifacts\coverage\UnitTests`, write the numeric targets and report the table.

KEY RULES:
- No Thread.Sleep; a test never swallows an exception of the code under test; no justification file for unit or guard.

REFERENCE FILES:
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
- .github/coverage-manifest/Encina.json
```

</details>

### Phase 8: Documentation and Finalization

> **Goal**: Contributors know how to log and allocate ids.

<details>
<summary><strong>Tasks</strong></summary>

1. A developer guide page on structured logging and EventId allocation (how to add a `[LoggerMessage]`, how to find free ids on the registry page, when to open a new range in the 10000+ zone, why a direct `ILogger.Log*()` call fails the build), written by `docs-writer` under the `encina-docs` skill; the page location is decided by `docs-writer`
2. `changelog.d/757-loggermessage-standardization.changed.md` (written by `mechanical-fixer`): all packages log through source-generated messages; new EventId ranges
3. `dotnet build --configuration Release` with 0 warnings; `dotnet test`; every flag at its target; CRAP <= 10 on changed methods

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are finishing issue #757: documentation and verification.

CONTEXT:
- Documentation goes through docs-writer and the encina-docs skill; the changelog fragment through mechanical-fixer.

TASK:
Complete the Phase 8 tasks and report the per-flag figures of the touched files against their targets.

KEY RULES:
- English only; no private client application named; no hand-typed coverage figures.
- Zero warnings; no [Obsolete].

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- changelog.d/README.md
- docs/engineering/eventid-block-registry.md
```

</details>

---

## Research

### Relevant Standards and Specifications

| Source | Provision | Relevance |
|--------|-----------|-----------|
| ADR-021 | EventId uniqueness, ranges, the 10000+ zone | Design Choice 4 |
| `AGENTS.md` section 7 | Register a range first, pack sequentially, generator for new code, new ranges only in the zone | Every pull request |
| `AGENTS.md` section 8 | Zero warnings; CA1848 may be suppressed when the optimisation is future work | Design Choice 2 |
| `AGENTS.md` section 3 | Exceptions through `ForLogging()`; `EncinaError.Message` never reaches logs | Register C2 |
| `AGENTS.md` section 11 | At most 15 files and 1500 changed lines per pull request | The 21-pull-request split |
| CA1848 | Use the `LoggerMessage` delegates | The analyzer of Design Choice 2 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `EventIdRanges` | `src/Encina/Diagnostics/EventIdRanges.cs` | Registered ranges; new fields for the zone ranges |
| Registry page | `docs/engineering/eventid-block-registry.md` | Free space and the rows each registration edits |
| `EventIdUniquenessRule` | `src/Encina.Testing.Architecture/EventIdUniquenessRule.cs` | Asserts the converted ids |
| `EncinaEventIdAllocationTests`, `EventIdRegistryAgreementTests` | `tests/Encina.UnitTests/Testing/Architecture/` | Per-pull-request proof |
| `RedactedException.ForLogging()` | `src/Encina/Diagnostics/RedactedException.cs` | Exception parameter of converted messages |
| `ReshardingLogMessages`, `Encina.MongoDB/Log.cs` | `src/Encina.OpenTelemetry/Resharding/`, `src/Encina.MongoDB/` | Style of a log class and of `Log.cs` |
| `.editorconfig` | repository root | CA1848 severity |
| Coverage manifests | `.github/coverage-manifest/<Package>.json`, `Encina.json` | Numeric targets of the touched files |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| ADO.SqlServer, ADO.PostgreSQL, ADO.MySQL, Dapper x3, EntityFrameworkCore, MongoDB | new `<Name>2` ranges, 50 ids each, Database area (base 20000) | 22, 22, 22, 22, 22, 22, 22 and 32 messages; slot read from the registry at PR time |
| `Encina` (sharding and modules) | new `Core2`, 100 ids, Core and messaging area (10000) | 70 + 12 = 82 messages |
| Cdc, DomainModeling, Audit.Marten | new `Cdc2`, `DomainModeling2`, `AuditMarten2`, 50 ids each, Core and messaging area | 2, 7 and 29 messages |
| Caching | new `Caching2`, 50 ids, Caching area (30000) | 18 messages |
| AspNetCore | new `AspNetCore2`, 50 ids, Observability and platform area (60000) | 4 messages |
| AwsLambda | new `AwsLambda2`, 50 ids, Cloud and integration area (80000) | 8 messages |
| 13 compliance packages and 6 security packages | new `<Name>2` ranges (`ComplianceNIS2Extra`, `SecurityEncryption`), 50 ids each, 100 for DataResidency, Security and compliance area (50000) | 53, 37, 38, 27, 24, 20, 14, 14, 9, 3, 3, 2, 1 and 19, 9, 4, 2, 1 messages |

Every range is registered by the pull request that first uses it; the old ranges are untouched. The block 1000-1049 reserved for the family of this plan is not used and can be released.

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Call-site files modified | 89 | 616 conversions |
| Pragma-only files modified | 3 | `ShardRoutingMetrics.cs`, `ShardedReadWriteMetrics.cs`, `ShardedMongoDbDatabaseHealthMonitor.cs` |
| Log classes modified or created | about 40 | 3 new (`ShardingLogMessages.cs`, `ModuleLogMessages.cs`, `EncryptionLogMessages.cs`) |
| Coverage manifests | about 35 | One per package touched plus `Encina.json`, touched again by each range-registering pull request |
| Range registration | 80 | 20 range-registering pull requests of 4 shared files (`EventIdRanges.cs`, `PublicAPI.Unshipped.txt`, registry page, `AssemblyRanges` test) |
| Unit tests | 20 | One class per pull request 1 to 20 |
| Suppressions and enforcement | 6 | `.editorconfig` and 4 `GlobalSuppressions.cs` (AwsLambda and DomainModeling in PRs 5 and 8, AzureFunctions and DistributedLock in PR 21), plus 1 csproj (`InternalsVisibleTo` of AwsLambda) |
| Documentation and changelog | 2 | Guide, fragment |
| **Total** | **about 275** | 21 pull requests of 6 to 15 files each, never above 15 |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing issue #757: remove every direct ILogger.Log*() call and enforce [LoggerMessage].

PROJECT CONTEXT:
- .NET 10 / C# 14, nullable enabled, pre-1.0: best solution first, no compatibility layers, no [Obsolete].
- 616 direct calls in 89 files of 33 packages (measured on origin/main) and 21 files that hide sites behind a CA1848 pragma or suppression; the audit strips them.
- EventIds: AGENTS.md section 7 and docs/engineering/eventid-block-registry.md; every package gets a new range in the 10000+ zone.

IMPLEMENTATION OVERVIEW:
1. Audit with the pragmas stripped and CA1848 as a warning; update the pull request table if counts differ.
2. 20 conversion pull requests in the order of the table: each direct call becomes a [LoggerMessage] method in the package's log class with the same level, template and parameters;
   exceptions through ForLogging(); ids from a new zone range registered in the first pull request that uses it (EventIdRanges.cs, PublicAPI of Encina, registry page, AssemblyRanges, Encina.json);
   the file's pragma pair is deleted; numeric manifest targets for the touched files.
3. One unit test class per pull request (FakeLogger; EventId, level, template) plus the EventId architecture tests.
4. Pull request 21: CA1848 as a warning for src/, delete the last two local suppressions, guide through docs-writer, fragment through mechanical-fixer.

KEY PATTERNS:
- No behavior change: never alter which branch logs; never log EncinaError.Message; preserve parameters.
- At most 15 files per pull request, counted by name; every pull request builds and passes alone; targets never go down; CRAP <= 10 on touched methods.

REFERENCE FILES:
- docs/plans/loggermessage-standardization-implementation-plan-757.md
- src/Encina/Diagnostics/EventIdRanges.cs
- docs/engineering/eventid-block-registry.md
- src/Encina.Testing.Architecture/EventIdUniquenessRule.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | The change touches log statements only |
| 2 | OpenTelemetry | ❌ | No span or metric changes; structured messages also benefit the log signal of #1048 |
| 3 | Structured Logging | ✅ | This plan is the feature (all phases) |
| 4 | Health Checks | ❌ | Health check classes are converted, not changed; no new check |
| 5 | Validation | ❌ | No input |
| 6 | Resilience | ❌ | No external call is added |
| 7 | Distributed Locks | ❌ | None |
| 8 | Transactions | ❌ | None |
| 9 | Idempotency | ❌ | None |
| 10 | Multi-Tenancy | ❌ | Parameters are preserved, so no tenant value enters a log that was not there; identifier review is a follow-up |
| 11 | Module Isolation | ❌ | None |
| 12 | Audit Trail | ❌ | Audit stores are converted (PR 17) with their messages unchanged; no audit event changes |

---

## Prerequisites & Dependencies

| Item | State | Effect on this plan |
|------|-------|---------------------|
| [#4007](https://github.com/dlrivada/Encina/issues/4007) — migrate ranges below 10000 into the zone | Open | The ranges here do not wait for it; the registry rows say `zone` |
| [#1120](https://github.com/dlrivada/Encina/issues/1120) — EventId allocation architecture test | Merged | Proof run in every pull request |
| [#1048](https://github.com/dlrivada/Encina/issues/1048) — logs wiring in `WithEncina` | Open; plan written | Independent; makes the converted messages reach an OpenTelemetry pipeline |
| [#763](https://github.com/dlrivada/Encina/issues/763) — DPIA reminder lock | Open; plan on `main` | Adds messages to `DPIALogMessages.cs` in range 6350-6359; PR 15 rebases on whichever merges first |

---

## Next Steps

1. Link this plan from issue #757 and open the issue comment that the packages the issue names are already compliant and the real gap is the 616 calls listed here.
2. When work on the issue starts, ask the maintainer to answer Dilemmas 1 to 5 (scope; enforcement mechanism; content of the converted messages; source of the new ids; file naming), record the answers in a `## Maintainer Decisions` section placed last, and have the adversarial-reviewer verify the Settled and Trivial choices. If Dilemma 2 is answered A, the maintainer edits `AGENTS.md` section 8 so that `CA1848` is fixed rather than suppressed in `src/`.
3. Follow-up issue file `loggermessage-level-and-identifier-policy` (a log-level policy across subsystems and a review of messages that carry identifiers), written with this plan and opened by the orchestrator.
4. One pull request per row of the table; the last pull request references `Fixes #757`.
