# Implementation Plan: Standardize the `[LoggerMessage]` Pattern (Remove Every Direct `ILogger.Log*()` Call)

> **Issue**: [#757](https://github.com/dlrivada/Encina/issues/757)
> **Type**: Feature (technical-debt program)
> **Complexity**: High by volume, low per change (18 pull requests, each a mechanical conversion)
> **Estimated Scope**: 616 call conversions in 89 files of 33 packages; about 12 new `EventIdRanges` entries; about 1,800-2,400 changed lines in total (about 100-250 per pull request)
> **Specification**: [ADR-021](../architecture/adr/021-eventid-uniqueness-enforcement.md) (EventId ranges and the 10000+ zone); `AGENTS.md` sections 7 and 8; [EventId block registry](../engineering/eventid-block-registry.md)
> **Related**: [#4007](https://github.com/dlrivada/Encina/issues/4007) (migration of the ranges below 10000 into the zone), [#1120](https://github.com/dlrivada/Encina/issues/1120) (the EventId allocation architecture test), [#1788](https://github.com/dlrivada/Encina/issues/1788) (error text in spans and logs)

---

## Summary

The issue says that newer caching, distributed-lock and transport packages log through `ILogger.Log*()` directly. Measured on `origin/main` today, that is not true: those packages already use `[LoggerMessage]` (all 8 caching providers with a logger, the lock providers, the 10 transports). The real remaining set is different and larger: **616 direct calls in 89 files of 33 packages**, found with a search for the `LogTrace|LogDebug|LogInformation|LogWarning|LogError|LogCritical` extension calls and `Log(LogLevel...)` outside comments. This plan converts all of them and then makes the build refuse new ones.

| Family | Packages (calls) |
|--------|------------------|
| Sharding data layer | ADO.SqlServer, ADO.PostgreSQL, ADO.MySQL, Dapper.SqlServer, Dapper.PostgreSQL, Dapper.MySQL, EntityFrameworkCore (22 each), MongoDB (32) |
| Core and cross-cutting | `Encina` (82: sharding 70, modules 12), Caching (18), Cdc (2), AspNetCore (4), AwsLambda (8), DomainModeling (7) |
| Compliance modules | DataResidency (53), LawfulBasis (38), BreachNotification (37), Retention (27), CrossBorderTransfer (24), ProcessorAgreements (20), DPIA (14), DataSubjectRights (14), Consent (9), PrivacyByDesign (3), AIAct (3), NIS2 (2), Attestation (1) |
| Security and audit | Audit.Marten (29), Security.ABAC (19), Security.Encryption (9), Security.Sanitization (4), Security.PII (2), Security.Secrets (1) |

1. **Audit first (Phase 1).** The analyzer is the source of truth: a local build with `dotnet_diagnostic.CA1848.severity = warning` under `src/` lists every site (the count above comes from a text search and can miss a multi-line call).
2. **Convert package by package (Phases 2 to 5).** Each direct call becomes a `[LoggerMessage]` method in the package's log class with the same level, the same template and the same parameters; ids come from the package's registered range or, when the range has too few free ids, from a new range of the 10000+ zone registered in the pull request that first uses it (Design Choice 4).
3. **Enforce (Phase 6).** After the last conversion, `CA1848` becomes a warning for `src/` so a new direct call fails the zero-warnings build (Design Choice 2).
4. **Document (Phase 8).** A developer guide page on EventId allocation closes the last acceptance criterion.

**Standards**: ADR-021; `AGENTS.md` section 7 (EventIds, the registry), section 8 (zero warnings, CA1848), section 3 (an exception passed to a logger goes through `ForLogging()`).

**Estimated scope**: about 89 modified call-site files, about 40 modified log classes, 2 new log classes, 12 range registrations (4 files each), 18 unit test files, 1 `.editorconfig`, up to 4 `GlobalSuppressions.cs`, the guide and a changelog fragment; 18 pull requests of 7 to 14 files each (see Implementation Phases).

**Affected packages**: the 33 above; no new package.

**Provider category**: Database (10) for the sharding data layer: the 7 ADO, Dapper and EF Core repositories and MongoDB are converted together so the 10-provider coherence rule holds (ADO 3, Dapper 3, EF Core 1 as the single EF repository class, MongoDB 1; the EF Core family has one repository class for all three databases). Other families are provider-independent.

**What the code does today** (`origin/main` at `da9dd5e3e`):

- `.editorconfig` sets `dotnet_diagnostic.CA1848.severity = suggestion` ("Future optimization"); four `GlobalSuppressions.cs` files (`Encina.AwsLambda`, `Encina.AzureFunctions`, `Encina.DistributedLock`, `Encina.DomainModeling`) suppress `CA1848` locally.
- Caching providers (`Memory`, `Redis`, `Hybrid`), lock providers (`InMemory`, `Redis`, `SqlServer`) and the transports already declare `[LoggerMessage]` methods; `Encina.Caching.Dragonfly`, `Garnet`, `KeyDB`, `Valkey` and `Encina.DistributedLock` have no logger at all (they reuse the Redis provider); `Encina.Caching` itself has 18 direct calls in its sharding classes.
- Messages carry parameters such as request ids and `VerifiedBy` (the DSR service); only 2 of the 616 calls pass an exception, and those must go through `ForLogging()` (`src/Encina/Diagnostics/RedactedException.cs`).
- Ranges are tight: of the ranges that hold call sites, ten have fewer free ids than calls (Core 100-199 with 23 free vs 82 calls, DataResidency 7 vs 53, BreachNotification 8 vs 37, LawfulBasis 13 vs 38, Retention 3 vs 27, Audit.Marten 7 vs 29, ProcessorAgreements 12 vs 20, ABAC 0 vs 19, DPIA 3 vs 14, DataSubjectRights 0 vs 14) and `Encina.Security.Encryption` has no range at all. The registry page `docs/engineering/eventid-block-registry.md` has no block in the 10000+ zone yet.
- `EventIdUniquenessRule` (`src/Encina.Testing.Architecture/EventIdUniquenessRule.cs`) asserts that every `[LoggerMessage]` has an EventId inside a range mapped to its assembly; `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (`AssemblyRanges`) and `EventIdRegistryAgreementTests.cs` keep the registry and `EventIdRanges.cs` in agreement. No test or analyzer forbids a direct call.
- The 12 sharding repositories (`Sharding/FunctionalShardedRepository*.cs`) hold 22 calls each; the 11 files of `src/Encina/Sharding/` hold 70; `ModuleLifecycleHostedService.cs` holds 12.

---

## Design Choices

<details>
<summary><strong>1. Scope: which direct calls are converted</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Convert every direct call found by the analyzer (616 in 33 packages today) and then enforce** | Meets the acceptance criterion "no direct `ILogger.Log*()` calls remain"; the package list of the issue is not what the code shows, the measured list is | 18 pull requests; touches compliance code that logs identifiers (Design Choice 3) |
| **B) Convert only the packages the issue names (caching providers, lock providers, newer transports)** | Matches the issue text literally | Those packages have 0 direct calls, so there is nothing to convert; the criterion stays false for 33 packages |
| **C) Enforce for new code only: `CA1848` as a warning with a baseline of the existing files** | One small pull request stops new direct calls | Leaves 616 calls and a baseline list that is a compatibility layer (`AGENTS.md` section 1) |

### Chosen Option: **A — convert all, then enforce** (recommended, pending the maintainer)

### Rationale

- A wins because the acceptance criteria are about the result in the code, and the measured gap is outside the packages the issue lists; C wins only if the maintainer wants the enforcement before the 1.0 push and accepts the debt as a tracked baseline; B closes the issue without effect.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. How a new direct call is prevented</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `.editorconfig`: `dotnet_diagnostic.CA1848.severity = warning` in a `[src/**.cs]` section (tests stay at suggestion), and delete the four local suppressions** | Zero new code; the zero-warnings build already enforces it for every contributor and every agent; the analyzer knows every logging overload | Changes the meaning of `AGENTS.md` section 8 ("CA1848 may be suppressed when the optimisation is future work"), a text change that belongs to the maintainer |
| **B) A new rule in `Encina.Testing.Architecture` that scans assemblies for calls to the `LoggerExtensions.Log*` methods** | Works inside the architecture test suite; reusable by applications | New public API and an IL scan to write, test and maintain, for what the analyzer already does |
| **C) Documentation only** | No change | Nothing stops the next direct call |

### Chosen Option: **A — analyzer severity** (recommended, pending the maintainer)

### Rationale

- A wins for a repository that already treats warnings as build failures; B wins if a consumer application should be able to run the same check on its own assemblies. The proposed `AGENTS.md` section 8 edit (CA1848 is fixed, not suppressed, in `src/`) is listed for the maintainer in Next Steps; the worker does not edit `AGENTS.md`.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Content of the converted messages: preserve, or fix levels and identifiers in the same pull request</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Preserve each call exactly: same level, same template text, same parameters (names and order), exceptions through `ForLogging()`** | A pure refactor, reviewable line by line; no behavior change to explain | The acceptance criterion "consistent log levels across subsystems" is not met by this plan; messages that carry identifiers (the DSR request id and `VerifiedBy`, tenant or subject values in some compliance services) keep carrying them |
| **B) Convert and, in the same pull request, normalize levels and drop identifiers** | Meets the criterion and the identifier concern in one pass | Mixes behavior changes into 18 mechanical pull requests; each level change needs a justification a reviewer cannot check from the diff alone |

### Chosen Option: **A — preserve** (recommended, pending the maintainer)

### Rationale

- A wins because a mechanical conversion is the only kind of change a reviewer can verify at this volume; B wins if the maintainer wants the level policy delivered with the conversion and accepts slower reviews. Under A the audit of Phase 1 also lists every message whose parameters are identifiers, and the follow-up issue file `loggermessage-level-and-identifier-policy` (Next Steps) carries the level policy and the identifier review as their own change.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Where new EventIds come from</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Use the package's registered range while it has free ids; otherwise register a second range for the package in the 10000+ zone, in the pull request that first uses it** | Follows the ADR-021 rule exactly; ids stay inside the package's ranges | Ten packages need a second range (a 4-file registration each) |
| **B) Borrow free ids from another package's range** | No registration | Forbidden: an id outside the package's range fails `EventIdUniquenessRule` |
| **C) Reuse an EventId for two messages with the same text** | Fewer ids | Two messages with one id break uniqueness and the log filter by id |

### Chosen Option: **A — own range, second range from the zone**

### Rationale

- Each registration is `EventIdRanges.cs`, the `PublicAPI.Unshipped.txt` of `Encina` for the new field, the "Registered ranges" and "Blocks" rows of the registry page and the `AssemblyRanges` map of `EncinaEventIdAllocationTests`; the new row has Zone `zone` and the block row status `registered`. The slot for each new range is the next free 50 or 100 ids in the area's current 1000 block, read from the registry page when the pull request opens; this plan names only the area and the size.

**Class:** Settled
**Settled by:** `AGENTS.md` section 7 (never assign an EventId outside the package's range; register a range first; new blocks only in the 10000+ zone; pack sequentially); the registry page and `EventIdRegistryAgreementTests`

</details>

### Register of defaults and semantics

| Id | Choice | Class | Settled by |
|----|--------|-------|------------|
| C1 | No `EncinaError` is created, so there is no ADR-062 `ErrorCategory` to assign | Settled | ADR-062 (a category is chosen where an error is created) |
| C2 | The converted message uses the call's own level and template; the `Exception` parameter, where there is one, receives `exception.ForLogging()` | Settled | `AGENTS.md` section 3 (an exception passed to a logger goes through `ForLogging()`) |
| C3 | `LoggerMessage.Define` code is left as it is (one literal `new EventId(...)` per call, scanned by the test); only direct `ILogger.Log*()` calls are converted | Settled | `AGENTS.md` section 7 (existing `LoggerMessage.Define` calls stay; new code uses the generator) |
| C4 | The new method goes into the package's existing log class (`Diagnostics/*LogMessages.cs`, `Log.cs` or the file the package already uses); a package with none gets `Diagnostics/<Package>LogMessages.cs` | Trivial | `AGENTS.md` section 7 and the existing convention of each package |
| C5 | The split into 18 pull requests of at most 15 files follows the size rule; a range-registering pull request lands before the ones that use the range | Settled | `AGENTS.md` section 11 (at most 15 files and 1500 changed lines per pull request) |
| C6 | No metric, span or tenant tag is added or changed; messages keep their parameters, so no new tenant data enters a log | Settled | Design Choice 3 option A is a preserving refactor; ADR-054 D4 applies to tags, which this plan does not touch |
| C7 | A touched method must keep CRAP at or below 10; the pull request measures the touched methods first and adds tests in the same pull request where a touched method is below the bar | Settled | `AGENTS.md` section 9 (CRAP gate on changed methods) |

---

## Implementation Phases

**Pull request table** (counts include the unit test file, the log classes, the registration files and any manifest entry; every row is at most 15 files and about 250 changed lines; rows marked "range" register their ranges and must merge before the rows that name them):

| PR | Packages (call files / calls) | New range | Files |
|----|-------------------------------|-----------|------:|
| 1 | ADO.SqlServer, ADO.PostgreSQL, ADO.MySQL (3 / 66) | none (45, 45 and 49 free) | 7 |
| 2 | Dapper.SqlServer, Dapper.PostgreSQL, Dapper.MySQL, EntityFrameworkCore (4 / 88) | none | 9 |
| 3 | MongoDB (1 / 32), Caching (4 / 18), Cdc (1 / 2) | none (33, 36 and 46 free) | 10 |
| 4 | `Encina` sharding: Execution, Migrations, ReferenceTables x2 (4 / 31) | range `CoreSharding`, about 70 ids, area Core and messaging | 11 |
| 5 | `Encina` sharding: the six resharding phases and the orchestrator (7 / 39) | uses PR 4 | 9 |
| 6 | `Encina` modules (1 / 12), AspNetCore (1 / 4), AwsLambda (2 / 8), DomainModeling (1 / 7) | none | 10 |
| 7 | DataResidency, first 5 files (5 / 24) | range `ComplianceDataResidency2`, 50 ids | 11 |
| 8 | DataResidency, last 5 files (5 / 29) | uses PR 7 | 7 |
| 9 | BreachNotification, first 5 files (5 / 32) | range `ComplianceBreachNotification2`, 50 ids | 11 |
| 10 | BreachNotification, last 5 files (5 / 5) | uses PR 9 | 7 |
| 11 | Consent, PrivacyByDesign, AIAct, NIS2, Attestation, Security.Secrets (6 / 19) | none | 13 |
| 12 | LawfulBasis (1 / 38), Retention (3 / 27) | ranges `ComplianceLawfulBasis2`, `ComplianceRetention2`, 50 ids each | 11 |
| 13 | CrossBorderTransfer (4 / 24), ProcessorAgreements (3 / 20) | range `ComplianceProcessorAgreements2`, 50 ids | 14 |
| 14 | DPIA (2 / 14), DataSubjectRights (3 / 14) | ranges `ComplianceDPIA2`, `ComplianceDSR2`, 50 ids each | 12 |
| 15 | Audit.Marten (7 / 29) | range `AuditMarten2`, 50 ids, area Core and messaging | 13 |
| 16 | Security.ABAC (4 / 19), Security.PII (2 / 2) | range `SecurityABAC2`, 50 ids | 13 |
| 17 | Security.Encryption (2 / 9), Security.Sanitization (3 / 4) | range `SecurityEncryption`, 50 ids; Sanitization uses its free range 1-99 | 13 |
| 18 | Enforcement and guide (Phases 6 and 8) | none | 7 |

The new ranges of the security and compliance packages come from the Security and compliance area (base 50000); the `Encina` core and Audit.Marten ranges from Core and messaging (base 10000); each range is 50 or 100 ids and the exact slot is read from the registry page when the pull request opens. PR 4 to PR 5, PR 7 to PR 8 and PR 9 to PR 10 are ordered; every other pull request is independent and can open in any order, in batches of three or four.

### Phase 1: Audit and Baseline

> **Goal**: The exact list of sites, per package, from the analyzer.

<details>
<summary><strong>Tasks</strong></summary>

1. Build `Encina.slnx` once with `-p:` overrides or a temporary local `.editorconfig` that sets `dotnet_diagnostic.CA1848.severity = warning` under `src/`, and write the warnings per file to `artifacts/logging-audit/` (not committed)
2. Compare with the table of this plan; where a package differs by more than 10 calls, update the pull request table before starting
3. List every message whose parameters are identifiers (request, subject, user, tenant values) into the follow-up issue file of Design Choice 3 (a worker writes the list; the local model drafts the body)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #757: audit the direct ILogger.Log*() calls.

CONTEXT:
- .editorconfig sets CA1848 to suggestion. The plan counts 616 direct calls in 89 files with a text search; the analyzer is the source of truth.
- Nothing is committed in this phase except an updated pull request table in the plan when the counts differ.

TASK:
Run the build with CA1848 as a warning for src/, group the warnings by package and file, and list the messages with identifier parameters.

KEY RULES:
- Outputs under artifacts/ only; no code change; never lower or raise any other analyzer.

REFERENCE FILES:
- .editorconfig
- docs/engineering/eventid-block-registry.md
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

### Phase 2: Database Providers and Caching (pull requests 1 to 3)

> **Goal**: The 8 sharding repositories, Caching and Cdc log through the generator.

<details>
<summary><strong>Tasks</strong></summary>

1. **PR 1**: `Sharding/FunctionalShardedRepositoryADO.cs` of the three ADO packages: each call becomes a method of the package's log class with ids from its range (3200-3249, 3250-3299, 3300-3349), packed after the highest id in use
2. **PR 2**: `Sharding/FunctionalShardedRepositoryDapper.cs` of the three Dapper packages (3350-3399, 3400-3449, 3450-3499) and `FunctionalShardedRepositoryEF.cs` (3000-3099); the same 22 messages in every provider keep the same templates so the 10-provider behavior stays coherent
3. **PR 3**: `FunctionalShardedRepositoryMongoDB.cs` (32 messages inside 3100-3199, which has 33 free ids), the four `Encina.Caching/Sharding` classes (3500-3549) and `Encina.Cdc/Sharding/CdcDrivenRefreshHandler.cs` (4800-4899)
4. One unit test class per pull request: for each converted message, the real log method is called with a `FakeLogger` and the EventId, level and template are asserted; the existing tests of the repositories continue to drive the call sites

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #757: convert the sharding data layer, Caching and Cdc.

CONTEXT:
- Each file has direct _logger.LogXxx(...) calls. The package's log class and range are named in the plan; ids are packed after the highest id in use.
- Design Choice 3 option A: same level, same template, same parameters; exceptions through ForLogging().

TASK:
For each file of the pull request, replace every direct call with a [LoggerMessage] extension method in the package's log class and call it from the same place.
Add the unit test class. Run the EventId architecture tests (EncinaEventIdAllocationTests, EventIdRegistryAgreementTests) and the package tests.

KEY RULES:
- No behavior change; no new range in these pull requests; EventIds inside the registered range, no gaps.
- Measure CRAP of every touched method locally; add tests in the same pull request if one is above 10.
- Edit files with the Edit tool only; PublicAPI unchanged (log classes are internal).

REFERENCE FILES:
- src/Encina.ADO.SqlServer/Sharding/FunctionalShardedRepositoryADO.cs
- src/Encina.MongoDB/Log.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

### Phase 3: Core, Modules and Cross-Cutting (pull requests 4 to 6)

> **Goal**: `Encina` core, AspNetCore, AwsLambda and DomainModeling log through the generator.

<details>
<summary><strong>Tasks</strong></summary>

1. **PR 4**: register `CoreSharding` (about 70 ids from the Core and messaging area of the zone; `EventIdRanges.cs`, `PublicAPI.Unshipped.txt` of `Encina`, the registry page, `AssemblyRanges`); create `src/Encina/Diagnostics/ShardingLogMessages.cs` (internal static partial) and its manifest entry (no flags, source-generated as `ReshardingLogMessages.cs`); convert `ShardedQueryExecutor`, `ShardedMigrationCoordinator`, `ReferenceTableReplicationService` and `ReferenceTableReplicator`
2. **PR 5**: convert the six resharding phases and `ReshardingOrchestrator` (39 calls) into the same class
3. **PR 6**: `ModuleLifecycleHostedService` (12 calls, ids inside `Core` 100-199, which has 23 free ids; confirm which are free, since the range is used up to 176 with gaps), `HttpRegionContextProvider` (AspNetCore 200-249), `EventBridgeHandler` and `SqsMessageHandler` (AwsLambda 4700-4749), `PortsAndAdapters` (DomainModeling, in the range of its own log class); remove the `GlobalSuppressions.cs` CA1848 lines of AwsLambda and DomainModeling

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #757: convert Encina core, modules and the cloud and domain packages.

CONTEXT:
- Core 100-199 is nearly full, so the sharding messages need the new range CoreSharding in the 10000+ zone (registered in PR 4, registry rules in AGENTS.md section 7).
- The registry page docs/engineering/eventid-block-registry.md and EventIdRegistryAgreementTests must agree with EventIdRanges.cs.

TASK:
Register the range, add the log class, convert the files of the pull request in order (PR 4 before PR 5), add the unit test class and run the architecture tests.

KEY RULES:
- The new registered row has Zone zone; the block row has status registered; PublicAPI line for the new field through mechanical-fixer.
- Same level, template and parameters as the current call; exceptions through ForLogging(); no new metric or tag.

REFERENCE FILES:
- src/Encina/Sharding/Resharding/ReshardingOrchestrator.cs
- src/Encina.OpenTelemetry/Resharding/ReshardingLogMessages.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

### Phase 4: Compliance Modules (pull requests 7 to 14)

> **Goal**: The 13 compliance packages log through the generator.

<details>
<summary><strong>Tasks</strong></summary>

1. **PRs 7 and 8** DataResidency, **PRs 9 and 10** BreachNotification: register the second range in the first pull request, convert the files in the order of the table; the large services (`DefaultDataLocationService` 13 calls, `DefaultBreachNotificationService` 12) are converted with their tests in the same pull request
2. **PR 11**: Consent, PrivacyByDesign, AIAct, NIS2, Attestation and Security.Secrets inside their registered ranges (9200-9299, 8900-8949, 9500-9599, 9200-9299, 9600-9699, 5100-5199 respectively; read the highest id in use first)
3. **PR 12** LawfulBasis and Retention, **PR 13** CrossBorderTransfer (inside 9300-9399) and ProcessorAgreements, **PR 14** DPIA and DataSubjectRights: ranges registered where the table says; in `Encina.Compliance.DPIA` the plan of #763 takes EventIds 6350-6351 from another range, which this plan does not touch
4. One unit test class per pull request as in Phase 2; the DSR, Consent and LawfulBasis services already have unit tests that drive the call sites

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #757: convert the compliance packages.

CONTEXT:
- Ranges: DataResidency 8600-8699 (7 free), BreachNotification 8700-8799 (8 free), LawfulBasis 8350-8399 (13 free), Retention 8500-8599 (3 free),
  ProcessorAgreements 9400-9499 (12 free), DPIA 8800-8899 (3 free), DSR 8300-8349 (0 free): the second ranges come from the 50000 zone.
- Messages that carry identifiers are preserved as they are (Design Choice 3 option A) and listed in the follow-up issue file.

TASK:
For each pull request, register the ranges the table names, convert the files, add the unit test class, run the architecture tests and the package tests.

KEY RULES:
- Compliance gates fail closed: a conversion must not change which branch logs or whether an operation returns Left.
- Never log EncinaError.Message; keep the current parameters; exceptions through ForLogging().
- Edit files with the Edit tool only; registration files and PublicAPI lines are described to mechanical-fixer.

REFERENCE FILES:
- src/Encina.Compliance.DataResidency/Services/DefaultDataLocationService.cs
- src/Encina.Compliance.DataSubjectRights/Services/DefaultDSRService.cs
- docs/engineering/eventid-block-registry.md
```

</details>

### Phase 5: Security and Audit (pull requests 15 to 17)

> **Goal**: Audit.Marten and the security packages log through the generator.

<details>
<summary><strong>Tasks</strong></summary>

1. **PR 15**: Audit.Marten (7 files; the range 2550-2599 has 7 free ids; second range `AuditMarten2` from the Core and messaging area); the encryptor, stores, key providers and projections
2. **PR 16**: Security.ABAC (range 9000-9099 is full; `SecurityABAC2`) and Security.PII (8010-8029, 5 free ids)
3. **PR 17**: Security.Encryption (no range: `SecurityEncryption`, new `Diagnostics/EncryptionLogMessages.cs` and its manifest entry) and Security.Sanitization (range 1-99, free); the key-handling messages keep their text and parameters and never include key material
4. One unit test class per pull request

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #757: convert Audit.Marten and the security packages.

CONTEXT:
- Event-sourced and security code: tests mock IAggregateRepository or the stores (NSubstitute); no InMemory store is introduced.
- Security.Encryption has no EventId range yet; the plan registers SecurityEncryption (50 ids, Security and compliance area) in PR 17.

TASK:
Convert the files, register the ranges the table names, add the unit test class and run the architecture tests.

KEY RULES:
- Never log key material, plaintext or EncinaError.Message; preserve the current parameters; exceptions through ForLogging().
- Fail-closed behavior of the security gates is untouched.

REFERENCE FILES:
- src/Encina.Audit.Marten/MartenOperationAuditStore.cs
- src/Encina.Security.Encryption/EncryptionOrchestrator.cs
- src/Encina.Security.ABAC/ABACPolicySeedingHostedService.cs
```

</details>

### Phase 6: Enforcement (pull request 18)

> **Goal**: A new direct call fails the build.

<details>
<summary><strong>Tasks</strong></summary>

1. **`.editorconfig`** (modify) — a section `[src/**.cs]` with `dotnet_diagnostic.CA1848.severity = warning`; the global `suggestion` line stays for tests and samples
2. **`GlobalSuppressions.cs`** of `Encina.AwsLambda`, `Encina.AzureFunctions`, `Encina.DistributedLock`, `Encina.DomainModeling` (modify) — delete the `CA1848` suppressions; any site the suppression hid is converted in this pull request if it is new, or reported
3. A full `dotnet build Encina.slnx --configuration Release` with zero warnings is the acceptance proof
4. Proposed text for `AGENTS.md` section 8 is left in the follow-up note of Next Steps (the maintainer edits `AGENTS.md`)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of issue #757: enforce [LoggerMessage] with the analyzer.

CONTEXT:
- Phases 2 to 5 converted every site; CA1848 is currently a suggestion.

TASK:
Make CA1848 a warning for src/ only, remove the four local suppressions, build the whole solution and fix any remaining site.

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

1. **Unit**: per pull request, one class `<Family>LogMessagesTests` calling each new log method with a `FakeLogger` and asserting the EventId, level, template and parameter names; the existing service and repository tests keep driving the converted call sites
2. **Guard**: no new public method or constructor, so no new guard test; the log classes are internal
3. **Contract, property, integration**: not applicable (no public interface, no invariant, no infrastructure); justification: the 10-provider repositories have integration tests of their own that run the converted call sites
4. **Architecture**: `EncinaEventIdAllocationTests`, `EventIdRegistryAgreementTests` and `EventIdUniquenessRule` pass in every pull request
5. **Targets**: every touched call-site file keeps its current numeric target in `.github/coverage-manifest/<Package>.json` (never lowered); a touched file whose manifest entry has only default flags (no number) gets numeric unit and guard targets in the same pull request, equal to the value measured before the change, each with a one-sentence justification; log classes carry no targets (source-generated) as `ReshardingLogMessages.cs` does; the two new log classes get manifest entries with no flags

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
`dotnet test tests\Encina.UnitTests --collect "XPlat Code Coverage" --results-directory artifacts\coverage\UnitTests` and report it against the targets.

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
| `AGENTS.md` section 7 | Register a range first, pack sequentially, generator for new code | Every pull request |
| `AGENTS.md` section 8 | Zero warnings; CA1848 may be suppressed when the optimisation is future work | Design Choice 2 |
| `AGENTS.md` section 3 | Exceptions through `ForLogging()`; `EncinaError.Message` never reaches logs | Register C2 |
| `AGENTS.md` section 11 | At most 15 files and 1500 changed lines per pull request | The 18-pull-request split |
| CA1848 | Use the `LoggerMessage` delegates | The analyzer of Design Choice 2 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `EventIdRanges` | `src/Encina/Diagnostics/EventIdRanges.cs` | Registered ranges; new fields for the second ranges |
| Registry page | `docs/engineering/eventid-block-registry.md` | Free space and the rows each registration edits |
| `EventIdUniquenessRule` | `src/Encina.Testing.Architecture/EventIdUniquenessRule.cs` | Asserts the converted ids |
| `EncinaEventIdAllocationTests`, `EventIdRegistryAgreementTests` | `tests/Encina.UnitTests/Testing/Architecture/` | Per-pull-request proof |
| `RedactedException.ForLogging()` | `src/Encina/Diagnostics/RedactedException.cs` | Exception parameter of converted messages |
| `ReshardingLogMessages`, `Encina.MongoDB/Log.cs` | `src/Encina.OpenTelemetry/Resharding/`, `src/Encina.MongoDB/` | Style of a log class and of `Log.cs` |
| `.editorconfig` | repository root | CA1848 severity |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| ADO.SqlServer, ADO.PostgreSQL, ADO.MySQL | 3200-3249, 3250-3299, 3300-3349 (existing) | 22 messages each; 45, 45 and 49 free |
| Dapper.SqlServer, Dapper.PostgreSQL, Dapper.MySQL, EntityFrameworkCore | 3350-3399, 3400-3449, 3450-3499, 3000-3099 (existing) | 22 messages each |
| MongoDB, Caching, Cdc, AspNetCore, AwsLambda | 3100-3199, 3500-3549, 4800-4899, 200-249, 4700-4749 (existing) | 32, 18, 2, 4 and 8 messages; within free ids |
| `Encina` sharding | new `CoreSharding`, about 70 ids, Core and messaging area of the 10000+ zone | 70 messages; slot read from the registry page |
| `Encina` modules | `Core` 100-199 (existing) | 12 messages; 23 free ids with gaps |
| DataResidency, BreachNotification, LawfulBasis, Retention, ProcessorAgreements, DPIA, DataSubjectRights | second ranges of 50 ids each, Security and compliance area | 53, 37, 38, 27, 20, 14, 14 messages against 7, 8, 13, 3, 12, 3, 0 free ids |
| Audit.Marten | new `AuditMarten2`, 50 ids, Core and messaging area | 29 messages against 7 free |
| Security.ABAC | new `SecurityABAC2`, 50 ids | 19 messages against 0 free |
| Security.Encryption | new `SecurityEncryption`, 50 ids | 9 messages; no range today |
| Other packages (Consent, CrossBorderTransfer, PrivacyByDesign, AIAct, NIS2, Attestation, Secrets, PII, Sanitization, DomainModeling) | existing ranges | Within free ids |

The block 1000-1049 reserved for the family of this plan is not used: every range this plan needs comes from the 10000+ zone or an existing range.

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Call-site files modified | 89 | 616 conversions |
| Log classes modified or created | about 42 | 2 new (`ShardingLogMessages.cs`, `EncryptionLogMessages.cs`) |
| Range registration | 40 | 10 registrations of 4 files (`EventIdRanges.cs`, `PublicAPI.Unshipped.txt`, registry page, `AssemblyRanges` test) |
| Unit tests | 17 | One class per pull request 1 to 17 |
| Manifests | 3 | Two new log classes; the numeric targets of touched files with default flags |
| Enforcement | 5 | `.editorconfig` and four `GlobalSuppressions.cs` |
| Documentation and changelog | 2 | Guide, fragment |
| **Total** | **about 200** | 18 pull requests of 7 to 14 files each, never above 15 |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing issue #757: remove every direct ILogger.Log*() call and enforce [LoggerMessage].

PROJECT CONTEXT:
- .NET 10 / C# 14, nullable enabled, pre-1.0: best solution first, no compatibility layers, no [Obsolete].
- 616 direct calls in 89 files of 33 packages (measured on origin/main da9dd5e3e); the analyzer CA1848 is the source of truth.
- EventIds: AGENTS.md section 7 and docs/engineering/eventid-block-registry.md; new ranges only in the 10000+ zone.

IMPLEMENTATION OVERVIEW:
1. Audit with CA1848 as a warning; update the pull request table if counts differ.
2. 17 conversion pull requests in the order of the table: each direct call becomes a [LoggerMessage] method in the package's log class with the same
   level, template and parameters; exceptions through ForLogging(); ids inside the package's range or a second range registered in the first pull
   request that needs it (EventIdRanges.cs, PublicAPI of Encina, registry page, AssemblyRanges).
3. One unit test class per pull request (FakeLogger; EventId, level, template) plus the EventId architecture tests.
4. Pull request 18: CA1848 as a warning for src/, delete the four local suppressions, guide through docs-writer, fragment through mechanical-fixer.

KEY PATTERNS:
- No behavior change: never alter which branch logs; never log EncinaError.Message; preserve parameters.
- At most 15 files per pull request; every pull request builds and passes alone; targets never go down; CRAP <= 10 on touched methods.

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
| 12 | Audit Trail | ❌ | Audit stores are converted (PR 15) with their messages unchanged; no audit event changes |

---

## Prerequisites & Dependencies

| Item | State | Effect on this plan |
|------|-------|---------------------|
| [#4007](https://github.com/dlrivada/Encina/issues/4007) — migrate ranges below 10000 into the zone | Open | The second ranges here do not wait for it; the registry rows say `zone` |
| [#1120](https://github.com/dlrivada/Encina/issues/1120) — EventId allocation architecture test | Merged | Proof run in every pull request |
| [#1048](https://github.com/dlrivada/Encina/issues/1048) — logs wiring in `WithEncina` | Open; plan written | Independent; makes the converted messages reach an OpenTelemetry pipeline |
| [#763](https://github.com/dlrivada/Encina/issues/763) — DPIA reminder lock | Open; plan on `main` | Adds messages to `DPIALogMessages.cs` in range 6350-6359; PR 14 rebases on whichever merges first |

---

## Next Steps

1. Link this plan from issue #757 and open the issue comment that the packages the issue names are already compliant and the real gap is the 616 calls listed here.
2. When work on the issue starts, ask the maintainer to answer Dilemmas 1 to 3 (scope; enforcement mechanism; content of the converted messages), record the answers in a `## Maintainer Decisions` section placed last, and have the adversarial-reviewer verify the Settled and Trivial choices. If Dilemma 2 is answered A, the maintainer edits `AGENTS.md` section 8 so that `CA1848` is fixed rather than suppressed in `src/`.
3. Follow-up issue file `loggermessage-level-and-identifier-policy` (a log-level policy across subsystems and a review of messages that carry identifiers), written with this plan and opened by the orchestrator.
4. One pull request per row of the table; the last pull request references `Fixes #757`.
