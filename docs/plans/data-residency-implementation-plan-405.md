# Implementation Plan: `Encina.Compliance.DataResidency` — Data Sovereignty & Residency Enforcement

> **Issue**: [#405](https://github.com/dlrivada/Encina/issues/405)
> **Type**: Feature
> **Complexity**: Very High (10 phases, 13 database providers, ~130 files)
> **Estimated Scope**: ~5,000-7,000 lines of production code + ~3,500-5,000 lines of tests

---

## Summary

Implement data residency and sovereignty enforcement for distributed architectures. This package ensures data stays within geographic boundaries by validating residency policies before processing, tracking physical data locations across regions, validating cross-border transfers against adequacy decisions, and routing requests to compliant regions.

The feature enforces GDPR Chapter V (Arts. 44-49) on international data transfers, supports 120+ country data protection laws, and provides built-in knowledge of EU adequacy decisions. The implementation uses a `DataResidencyPipelineBehavior` that intercepts requests decorated with `[DataResidency]` or `[NoCrossBorderTransfer]` attributes to enforce region constraints automatically.

**Provider category**: Database (13 providers) — for policy, location tracking, and audit stores.

**Affected packages**:
- `Encina.Compliance.DataResidency` (new — core abstractions, models, pipeline, diagnostics)
- `Encina.AspNetCore` (region context extraction from HTTP headers)
- `Encina.Messaging` (add `UseDataResidency` flag to `MessagingConfiguration`)
- `Encina.ADO.Sqlite`, `Encina.ADO.SqlServer`, `Encina.ADO.PostgreSQL`, `Encina.ADO.MySQL` (ADO stores)
- `Encina.Dapper.Sqlite`, `Encina.Dapper.SqlServer`, `Encina.Dapper.PostgreSQL`, `Encina.Dapper.MySQL` (Dapper stores)
- `Encina.EntityFrameworkCore` (EF Core stores)
- `Encina.MongoDB` (MongoDB stores)

---

## Design Choices

<details>
<summary><strong>1. Package Placement — New <code>Encina.Compliance.DataResidency</code> package</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New `Encina.Compliance.DataResidency` package** | Clean separation, own pipeline behavior, own observability, independent versioning, clear domain boundary | New NuGet package, additional project to maintain |
| **B) Extend `Encina.Compliance.GDPR`** | Single package, shared config | Bloats GDPR core (~60+ files), residency is broader than GDPR (120+ countries), violates SRP |
| **C) Add to `Encina` core** | No new package | Residency is opt-in compliance, not core framework concern |

### Chosen Option: **A — New `Encina.Compliance.DataResidency` package**

### Rationale

- Data residency spans multiple regulations beyond GDPR (CCPA, LGPD, PIPL, PDPA, etc.) — it deserves its own domain
- Follows the established compliance family pattern: `Encina.Compliance.Consent`, `Encina.Compliance.DataSubjectRights`, `Encina.Compliance.Retention`, `Encina.Compliance.Anonymization`
- Has its own pipeline behavior, store interfaces, and observability
- References nothing from other compliance packages — completely independent domain
- Satellite providers add stores in a `DataResidency/` subfolder (same pattern as `Retention/`, `Consent/`)

</details>

<details>
<summary><strong>2. Region Model — Immutable record with static registry</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Immutable `Region` record with static well-known instances** | Type-safe, compile-time validation for common regions, extensible | Static registry, more code upfront |
| **B) Simple string codes** | Minimal code, flexible | No validation, typo-prone, no adequacy metadata |
| **C) Enum-based regions** | Compile-time safety | Not extensible, can't add new countries without code change |

### Chosen Option: **A — Immutable record with static registry**

### Rationale

- `Region` is a sealed record with: `Code` (ISO 3166-1 or region code), `Country` (ISO 3166-1 alpha-2), `IsEU`, `IsEEA`, `HasAdequacyDecision`, `ProtectionLevel`
- `RegionRegistry` provides well-known instances: `Region.EU`, `Region.US`, `Region.UK`, `Region.JP`, etc.
- Adequacy decisions are codified as static data (current as of 2025) — easily updatable
- Custom regions created via `Region.Create("MY-REGION", "MY", ...)` for private cloud zones
- `RegionGroup` record for grouping: `RegionGroup.EU` contains all 27 EU member states
- Integration with existing `GeoRegion` from `Encina.Sharding.Routing` via optional adapter

</details>

<details>
<summary><strong>3. Data Location Tracking — Entity-based with store persistence</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Dedicated `DataLocation` entity stored via `IDataLocationStore`** | Full audit trail, queryable, fits 13-provider pattern | Storage overhead, write on every tracked operation |
| **B) In-memory tracking only** | Fast, no storage | Lost on restart, no audit compliance |
| **C) Event-sourced location log** | Immutable history | Requires event store, incompatible with CRUD providers |

### Chosen Option: **A — Entity-based with store persistence**

### Rationale

- `DataLocation` record tracks: `EntityId`, `DataCategory`, `Region`, `StoredAtUtc`, `LastVerifiedAtUtc`, `StorageType` (Primary/Replica/Cache/Backup)
- `IDataLocationStore` provides CRUD + query-by-entity + query-by-region
- Enables compliance audits: "show me all data stored outside the EU"
- Integrates with retention (auto-delete location records when data is deleted)
- Same persistence pattern as `IDSRRequestStore`, `IRetentionRecordStore`
- Pipeline behavior records location after successful processing

</details>

<details>
<summary><strong>4. Cross-Border Transfer Validation — Rule-based with adequacy check</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Rule-based validator with built-in adequacy database** | Accurate, standards-compliant, ready to use | Adequacy decisions change (annual updates needed) |
| **B) External service call** | Always up-to-date | Network dependency, latency, availability risk |
| **C) User-configurable rules only** | Flexible | Burden on user, compliance risk if misconfigured |

### Chosen Option: **A — Rule-based validator with built-in adequacy database**

### Rationale

- `ICrossBorderTransferValidator` validates transfers between source and destination regions
- Built-in `AdequacyDecisionRegistry` with current EU adequacy decisions (15 countries as of 2025)
- `TransferValidationResult` includes: `IsAllowed`, `LegalBasis` (Adequacy/SCCs/BCRs/Derogation), `RequiredSafeguards`, `Warnings`
- Transfer legal bases: `Adequacy` (Art. 45), `StandardContractualClauses` (Art. 46(2)(c)), `BindingCorporateRules` (Art. 47), `ExplicitConsent` (Art. 49(1)(a)), `Derogation` (Art. 49)
- Users can override/extend adequacy database via `DataResidencyOptions.ConfigureAdequacy()`
- Validator is composable: users register additional `ITransferRule` implementations

</details>

<details>
<summary><strong>5. Pipeline Behavior — Pre-execution residency enforcement</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Pre-execution check with attribute caching** | Blocks non-compliant before handler runs, zero reflection after warmup | Needs region context resolution per request |
| **B) Post-execution location recording only** | No blocking, just audit | Non-compliant data may be processed |
| **C) Middleware-only (ASP.NET Core level)** | Early rejection, HTTP-level | Doesn't work for non-HTTP scenarios (background jobs, messaging) |

### Chosen Option: **A — Pre-execution check with attribute caching**

### Rationale

- `DataResidencyPipelineBehavior<TRequest, TResponse>` checks residency before handler execution
- Static attribute caching per closed generic type (same pattern as `RetentionValidationPipelineBehavior`)
- Resolves current region from `IRegionContextProvider` (HTTP header, tenant config, or explicit setting)
- For `[DataResidency("EU")]`: validates current region is in allowed list
- For `[NoCrossBorderTransfer]`: validates source and target regions are the same
- Three enforcement modes: `Block` (reject), `Warn` (log + continue), `Disabled` (skip)
- After successful execution: records data location via `IDataLocationStore` (if tracking enabled)
- Cross-border transfers validated via `ICrossBorderTransferValidator`

</details>

<details>
<summary><strong>6. Region Context Resolution — Layered provider with HTTP and tenant support</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Layered `IRegionContextProvider` with HTTP + tenant + config fallback** | Works in HTTP and non-HTTP scenarios, respects multi-tenancy | Multiple resolution paths |
| **B) HTTP header only** | Simple | Doesn't work for background jobs, messaging consumers |
| **C) Configuration only** | Deployment-time decision | Can't support multi-region in same deployment |

### Chosen Option: **A — Layered provider with fallback chain**

### Rationale

- `IRegionContextProvider` resolves current processing region with fallback chain:
  1. Explicit `IRequestContext.Region` (set by caller)
  2. HTTP header `X-Data-Region` (via ASP.NET Core middleware)
  3. Tenant configuration (tenant → region mapping)
  4. `DataResidencyOptions.DefaultRegion` (deployment default)
- Each layer is optional — minimal deployments just configure `DefaultRegion`
- Multi-region deployments use HTTP header or tenant-based resolution
- `RegionContextMiddleware` in `Encina.AspNetCore` extracts `X-Data-Region` header
- Non-HTTP scenarios (hosted services, message consumers) use explicit context or default

</details>

<details>
<summary><strong>7. Region Router — Strategy-based with pluggable routing</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IRegionRouter` with pluggable strategy** | Extensible, supports any routing backend | Interface overhead |
| **B) Built-in HTTP routing only** | Simple | Only works for HTTP-based services |
| **C) Integration with Sharding `GeoShardRouter` only** | Reuses existing code | Tight coupling, sharding is optional |

### Chosen Option: **A — Strategy-based `IRegionRouter`**

### Rationale

- `IRegionRouter` determines target region and routes requests
- `DefaultRegionRouter` uses `IDataResidencyPolicy` to find the closest compliant region
- `DetermineTargetRegionAsync` returns the best region based on: allowed regions → proximity → availability
- `RouteToRegionAsync` delegates to region-specific handler (useful for multi-region deployments with service mesh)
- Optional integration with `GeoShardRouter` via `ShardingRegionRouter` adapter
- Users implement custom `IRegionRouter` for their infrastructure (Kubernetes, service mesh, cloud load balancer)

</details>

<details>
<summary><strong>8. Audit Trail — Dedicated store with transfer logging</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Dedicated `IResidencyAuditStore` with structured entries** | Compliance-ready, queryable, same pattern as other compliance modules | Additional store |
| **B) Reuse Security AuditStore** | No new store | Different domain, different retention, conflated concerns |
| **C) Logging only** | Simple | Not queryable, not auditable for regulators |

### Chosen Option: **A — Dedicated `IResidencyAuditStore`**

### Rationale

- `ResidencyAuditEntry` record: `Id`, `EntityId`, `DataCategory`, `SourceRegion`, `TargetRegion`, `Action` (PolicyCheck/Transfer/LocationRecord/Violation), `Outcome` (Allowed/Blocked/Warning), `LegalBasis`, `Timestamp`, `RequestType`, `UserId`
- Same pattern as `IDSRAuditStore`, `IRetentionAuditStore`, `IConsentAuditStore`
- Queryable for compliance reports: "all cross-border transfers in last 90 days"
- Required for GDPR Art. 30 records of processing activities
- Integrates with existing security audit trail for comprehensive compliance view

</details>

---

## Implementation Phases

### Phase 1: Core Models & Enums

<details>
<summary>Tasks</summary>

1. **Create project** `src/Encina.Compliance.DataResidency/Encina.Compliance.DataResidency.csproj`
   - Target `net10.0`, nullable enabled, reference `Encina` core
   - PublicAPI analyzers, XML doc generation

2. **`Model/Region.cs`** — `Encina.Compliance.DataResidency.Model`
   - Sealed record: `Code` (string), `Country` (string), `IsEU` (bool), `IsEEA` (bool), `HasAdequacyDecision` (bool), `ProtectionLevel` (DataProtectionLevel)
   - Static factory: `Region.Create(code, country, isEU, isEEA, hasAdequacy, level)`
   - Equality by `Code` (case-insensitive)

3. **`Model/DataProtectionLevel.cs`** — Enum: `High`, `Medium`, `Low`, `Unknown`

4. **`Model/RegionRegistry.cs`** — Static class with well-known regions
   - `Region.EU`, `Region.US`, `Region.UK`, `Region.CH`, `Region.JP`, `Region.KR`, `Region.CA`, `Region.AU`, `Region.NZ`, `Region.IL`, `Region.AR`, `Region.UY`, `Region.IN`, `Region.BR`, `Region.CN`
   - `RegionRegistry.EUMemberStates` — `IReadOnlyList<Region>` (27 countries)
   - `RegionRegistry.EEACountries` — EU + IS, LI, NO
   - `RegionRegistry.AdequacyCountries` — Countries with EU adequacy decisions
   - `RegionRegistry.GetByCode(string code)` → `Option<Region>`

5. **`Model/RegionGroup.cs`** — Sealed record: `Name` (string), `Regions` (IReadOnlySet<Region>)
   - Static instances: `RegionGroup.EU`, `RegionGroup.EEA`, `RegionGroup.Adequate`
   - `Contains(Region)` check

6. **`Model/DataLocation.cs`** — Sealed record
   - `Id` (string), `EntityId` (string), `DataCategory` (string), `Region` (Region), `StorageType` (StorageType), `StoredAtUtc` (DateTimeOffset), `LastVerifiedAtUtc` (DateTimeOffset?), `Metadata` (IReadOnlyDictionary<string, string>?)

7. **`Model/StorageType.cs`** — Enum: `Primary`, `Replica`, `Cache`, `Backup`, `Archive`

8. **`Model/TransferValidationResult.cs`** — Sealed record
   - `IsAllowed` (bool), `LegalBasis` (TransferLegalBasis?), `RequiredSafeguards` (IReadOnlyList<string>), `Warnings` (IReadOnlyList<string>), `DenialReason` (string?)

9. **`Model/TransferLegalBasis.cs`** — Enum: `AdequacyDecision`, `StandardContractualClauses`, `BindingCorporateRules`, `ExplicitConsent`, `PublicInterest`, `LegalClaims`, `VitalInterests`, `Derogation`

10. **`Model/ResidencyAuditEntry.cs`** — Sealed record
    - `Id` (string), `EntityId` (string?), `DataCategory` (string), `SourceRegion` (string), `TargetRegion` (string?), `Action` (ResidencyAction), `Outcome` (ResidencyOutcome), `LegalBasis` (string?), `RequestType` (string?), `UserId` (string?), `TimestampUtc` (DateTimeOffset), `Details` (string?)

11. **`Model/ResidencyAction.cs`** — Enum: `PolicyCheck`, `CrossBorderTransfer`, `LocationRecord`, `Violation`, `RegionRouting`

12. **`Model/ResidencyOutcome.cs`** — Enum: `Allowed`, `Blocked`, `Warning`, `Skipped`

13. **`Model/ResidencyPolicyDescriptor.cs`** — Sealed record (for fluent config)
    - `DataCategory` (string), `AllowedRegions` (IReadOnlyList<Region>), `RequireAdequacyDecision` (bool), `AllowedTransferBases` (IReadOnlyList<TransferLegalBasis>)

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```
CONTEXT:
You are implementing Phase 1 of Encina.Compliance.DataResidency (#405).
This is a new compliance package in the Encina framework (.NET 10, C# 14, nullable enabled).

TASK:
1. Create the project file: src/Encina.Compliance.DataResidency/Encina.Compliance.DataResidency.csproj
   - Copy structure from src/Encina.Compliance.Retention/Encina.Compliance.Retention.csproj
   - Target net10.0, enable nullable, reference Encina core
   - Include PublicAPI analyzers

2. Create all Model/ files as described in Phase 1 tasks (13 files)
3. All types are sealed records or enums in namespace Encina.Compliance.DataResidency.Model
4. Region record must have case-insensitive equality by Code
5. RegionRegistry must include all 27 EU member states, 3 EEA-only, and 15 adequacy countries
6. All public types need XML documentation (<summary>, <remarks>, <param>)

KEY RULES:
- .NET 10 / C# 14 features, nullable enabled
- Sealed records for immutable domain models
- No [Obsolete], no backward compatibility
- EncinaError via factory methods (not exceptions) for domain errors
- Follow naming conventions from CLAUDE.md

REFERENCE FILES:
- src/Encina.Compliance.Retention/Model/ (all model files)
- src/Encina.Compliance.DataSubjectRights/Model/ (DSRRequest.cs, DSRAuditEntry.cs)
- src/Encina.Compliance.Retention/Encina.Compliance.Retention.csproj (project structure)
```

</details>

---

### Phase 2: Core Interfaces & Abstractions

<details>
<summary>Tasks</summary>

1. **`Abstractions/IDataResidencyPolicy.cs`** — `Encina.Compliance.DataResidency.Abstractions`
   - `ValueTask<Either<EncinaError, bool>> IsAllowedAsync(string dataCategory, Region targetRegion, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, IReadOnlyList<Region>>> GetAllowedRegionsAsync(string dataCategory, CancellationToken ct)`

2. **`Abstractions/IDataLocationStore.cs`**
   - `ValueTask<Either<EncinaError, Unit>> RecordAsync(DataLocation location, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, IReadOnlyList<DataLocation>>> GetByEntityAsync(string entityId, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, IReadOnlyList<DataLocation>>> GetByRegionAsync(Region region, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, IReadOnlyList<DataLocation>>> GetByCategoryAsync(string dataCategory, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, Unit>> DeleteByEntityAsync(string entityId, CancellationToken ct)`

3. **`Abstractions/IResidencyPolicyStore.cs`**
   - `ValueTask<Either<EncinaError, Unit>> CreateAsync(ResidencyPolicyDescriptor policy, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, Option<ResidencyPolicyDescriptor>>> GetByCategoryAsync(string dataCategory, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, IReadOnlyList<ResidencyPolicyDescriptor>>> GetAllAsync(CancellationToken ct)`
   - `ValueTask<Either<EncinaError, Unit>> UpdateAsync(ResidencyPolicyDescriptor policy, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, Unit>> DeleteAsync(string dataCategory, CancellationToken ct)`

4. **`Abstractions/IResidencyAuditStore.cs`**
   - `ValueTask<Either<EncinaError, Unit>> RecordAsync(ResidencyAuditEntry entry, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, IReadOnlyList<ResidencyAuditEntry>>> GetByEntityAsync(string entityId, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, IReadOnlyList<ResidencyAuditEntry>>> GetByDateRangeAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, IReadOnlyList<ResidencyAuditEntry>>> GetViolationsAsync(CancellationToken ct)`

5. **`Abstractions/ICrossBorderTransferValidator.cs`**
   - `ValueTask<Either<EncinaError, TransferValidationResult>> ValidateTransferAsync(Region source, Region destination, string dataCategory, CancellationToken ct)`

6. **`Abstractions/IRegionRouter.cs`**
   - `ValueTask<Either<EncinaError, Region>> DetermineTargetRegionAsync<TRequest>(TRequest request, CancellationToken ct)`

7. **`Abstractions/IRegionContextProvider.cs`**
   - `ValueTask<Either<EncinaError, Region>> GetCurrentRegionAsync(CancellationToken ct)`

8. **`Abstractions/IAdequacyDecisionProvider.cs`**
   - `bool HasAdequacy(Region region)`
   - `IReadOnlyList<Region> GetAdequateRegions()`

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```
CONTEXT:
You are implementing Phase 2 of Encina.Compliance.DataResidency (#405).
Phase 1 is complete — all Model/ types exist.

TASK:
1. Create 8 interface files in Abstractions/ folder
2. All store methods return ValueTask<Either<EncinaError, T>> following ROP pattern
3. Use Option<T> for single-item retrieval that may not exist
4. All methods accept CancellationToken with default value
5. Each interface needs full XML documentation

KEY RULES:
- Follow the same interface patterns as IRetentionPolicyStore, IDSRRequestStore
- All parameters validated with ArgumentNullException.ThrowIfNull at implementation level
- Option<T> from LanguageExt for optional returns
- Unit from LanguageExt for void-equivalent returns

REFERENCE FILES:
- src/Encina.Compliance.Retention/Abstractions/IRetentionPolicyStore.cs
- src/Encina.Compliance.Retention/Abstractions/IRetentionRecordStore.cs
- src/Encina.Compliance.DataSubjectRights/Abstractions/IDSRRequestStore.cs
```

</details>

---

### Phase 3: Default Implementations & In-Memory Stores

<details>
<summary>Tasks</summary>

1. **`DefaultDataResidencyPolicy.cs`** — Implements `IDataResidencyPolicy`
   - Constructor: `IResidencyPolicyStore`, `DataResidencyOptions`, `ILogger`
   - `IsAllowedAsync`: checks policy store for category → validates target region is in allowed list
   - Respects `DataResidencyOptions.DefaultRegion` as fallback when no specific policy exists

2. **`DefaultCrossBorderTransferValidator.cs`** — Implements `ICrossBorderTransferValidator`
   - Constructor: `IAdequacyDecisionProvider`, `DataResidencyOptions`, `ILogger`
   - Logic:
     - Same region → always allowed
     - Both EU/EEA → always allowed (free movement within EU)
     - Destination has adequacy → allowed with `AdequacyDecision` basis
     - Policy allows SCCs/BCRs → allowed with safeguards
     - Otherwise → denied with reason

3. **`DefaultAdequacyDecisionProvider.cs`** — Implements `IAdequacyDecisionProvider`
   - Built-in list of 15 countries with EU adequacy decisions
   - `HasAdequacy(Region)` → checks IsEU || IsEEA || is in adequacy list
   - Extensible via `DataResidencyOptions.AdditionalAdequateRegions`

4. **`DefaultRegionContextProvider.cs`** — Implements `IRegionContextProvider`
   - Constructor: `IRequestContext`, `DataResidencyOptions`, `ILogger`
   - Fallback chain: request context → tenant config → `DataResidencyOptions.DefaultRegion`

5. **`DefaultRegionRouter.cs`** — Implements `IRegionRouter`
   - Constructor: `IDataResidencyPolicy`, `IRegionContextProvider`, `ILogger`
   - `DetermineTargetRegionAsync`: uses policy to find closest compliant region

6. **`InMemory/InMemoryDataLocationStore.cs`** — Implements `IDataLocationStore`
   - `ConcurrentDictionary<string, List<DataLocation>>` keyed by entity ID
   - Thread-safe operations

7. **`InMemory/InMemoryResidencyPolicyStore.cs`** — Implements `IResidencyPolicyStore`
   - `ConcurrentDictionary<string, ResidencyPolicyDescriptor>` keyed by data category

8. **`InMemory/InMemoryResidencyAuditStore.cs`** — Implements `IResidencyAuditStore`
   - `ConcurrentBag<ResidencyAuditEntry>` for append-only audit trail

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```
CONTEXT:
You are implementing Phase 3 of Encina.Compliance.DataResidency (#405).
Phases 1-2 are complete — all models and interfaces exist.

TASK:
1. Create 5 default service implementations at the root of the package
2. Create 3 InMemory store implementations in InMemory/ folder
3. All services use constructor injection with ArgumentNullException.ThrowIfNull
4. InMemory stores use ConcurrentDictionary for thread safety
5. Default implementations follow the same pattern as DefaultRetentionPolicy, DefaultRetentionEnforcer

KEY RULES:
- InMemory stores are the default registrations (TryAddSingleton in DI)
- All methods return Either<EncinaError, T> — catch exceptions and wrap in Left
- OperationCanceledException must be re-thrown (never wrapped)
- InMemory stores should have testing helpers: GetAllRecords(), Clear(), Count
- DefaultCrossBorderTransferValidator must implement EU free movement rules correctly
- TimeProvider injection for deterministic testing

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/InMemory/InMemoryDSRRequestStore.cs
- src/Encina.Compliance.Retention/DefaultRetentionPolicy.cs
- src/Encina.Compliance.Retention/DefaultRetentionEnforcer.cs
- src/Encina.Compliance.Retention/InMemory/InMemoryRetentionPolicyStore.cs
```

</details>

---

### Phase 4: Attributes & Pipeline Behavior

<details>
<summary>Tasks</summary>

1. **`Attributes/DataResidencyAttribute.cs`** — `Encina.Compliance.DataResidency.Attributes`
   - `[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]`
   - Constructor: `params string[] allowedRegionCodes`
   - Properties: `AllowedRegionCodes` (string[]), `DataCategory` (string?), `RequireAdequacyDecision` (bool)

2. **`Attributes/NoCrossBorderTransferAttribute.cs`**
   - `[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]`
   - Properties: `DataCategory` (string?), `Reason` (string?)
   - Semantics: data must stay in the current processing region

3. **`DataResidencyPipelineBehavior.cs`** — Implements `IPipelineBehavior<TRequest, TResponse>`
   - Static attribute caching: `private static readonly DataResidencyAttributeInfo? CachedResidencyInfo`
   - Static attribute caching: `private static readonly NoCrossBorderTransferInfo? CachedNoCrossInfo`
   - Constructor: `IRegionContextProvider`, `IDataResidencyPolicy`, `ICrossBorderTransferValidator`, `IDataLocationStore`, `IResidencyAuditStore`, `DataResidencyOptions`, `TimeProvider`, `ILogger`
   - Handle logic:
     1. If enforcement disabled → call next
     2. Resolve cached attribute info (skip if none)
     3. Get current region from `IRegionContextProvider`
     4. For `[DataResidency]`: validate current region is in allowed list via `IDataResidencyPolicy`
     5. For `[NoCrossBorderTransfer]`: validate no cross-border movement
     6. Block or Warn based on enforcement mode
     7. Call next handler
     8. On success: record location via `IDataLocationStore` (if tracking enabled)
     9. Record audit entry via `IResidencyAuditStore`

4. **`DataResidencyAttributeInfo.cs`** — Internal record
   - `AllowedRegionCodes` (string[]), `DataCategory` (string?), `RequireAdequacyDecision` (bool)

5. **`NoCrossBorderTransferInfo.cs`** — Internal record
   - `DataCategory` (string?), `Reason` (string?)

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```
CONTEXT:
You are implementing Phase 4 of Encina.Compliance.DataResidency (#405).
Phases 1-3 are complete — models, interfaces, and default implementations exist.

TASK:
1. Create 2 attribute files in Attributes/ folder
2. Create the DataResidencyPipelineBehavior with static attribute caching
3. Create 2 internal record types for cached attribute info

KEY RULES:
- Static per-generic-type attribute caching — CRITICAL for performance
  private static readonly DataResidencyAttributeInfo? CachedResidencyInfo = ResolveResidencyAttribute();
- ResolveResidencyAttribute() checks typeof(TRequest).GetCustomAttribute<DataResidencyAttribute>()
- Three enforcement modes: Block (return Left error), Warn (log + continue), Disabled (skip entirely)
- Pipeline behavior is registered as open generic: typeof(IPipelineBehavior<,>)
- Audit entries recorded for BOTH success and failure outcomes
- OperationCanceledException must be re-thrown, never wrapped

REFERENCE FILES:
- src/Encina.Compliance.Retention/RetentionValidationPipelineBehavior.cs (MAIN reference)
- src/Encina.Compliance.Retention/Attributes/RetentionPeriodAttribute.cs
- src/Encina.Compliance.DataSubjectRights/ProcessingRestrictionPipelineBehavior.cs
```

</details>

---

### Phase 5: Configuration, DI & Error Constants

<details>
<summary>Tasks</summary>

1. **`DataResidencyOptions.cs`** — Configuration options
   - `DefaultRegion` (Region?) — deployment default region
   - `EnforcementMode` (DataResidencyEnforcementMode) — default: `Warn`
   - `TrackDataLocations` (bool) — default: `true`
   - `TrackAuditTrail` (bool) — default: `true`
   - `BlockNonCompliantTransfers` (bool) — default: `true`
   - `AddHealthCheck` (bool) — default: `false`
   - `AutoRegisterFromAttributes` (bool) — default: `true`
   - `AssembliesToScan` (List<Assembly>) — assemblies for attribute scanning
   - `AdditionalAdequateRegions` (List<Region>) — user-added adequacy regions
   - `ConfiguredPolicies` (internal List<ResidencyPolicyDescriptor>)
   - Fluent method: `AddPolicy(string dataCategory, Action<ResidencyPolicyBuilder> configure)`

2. **`DataResidencyOptionsValidator.cs`** — `IValidateOptions<DataResidencyOptions>`
   - Validates: `DefaultRegion` is set if enforcement is Block, policies have at least one allowed region

3. **`ResidencyPolicyBuilder.cs`** — Fluent builder
   - `AllowRegions(params Region[] regions)` → adds to allowed list
   - `AllowEU()` → adds all EU member states
   - `AllowEEA()` → adds all EEA countries
   - `AllowAdequate()` → adds all adequacy-decision countries
   - `RequireAdequacyDecision(bool require = true)`
   - `AllowTransferBasis(params TransferLegalBasis[] bases)`

4. **`DataResidencyEnforcementMode.cs`** — Enum: `Block`, `Warn`, `Disabled`

5. **`DataResidencyErrors.cs`** — Static factory methods
   - `RegionNotAllowed(string dataCategory, string regionCode)`
   - `CrossBorderTransferDenied(string source, string destination, string reason)`
   - `RegionNotResolved(string reason)`
   - `PolicyNotFound(string dataCategory)`
   - `StoreError(string operation, string message)`
   - `TransferValidationFailed(string reason)`
   - Error codes: `"residency.region_not_allowed"`, `"residency.cross_border_denied"`, etc.

6. **`ServiceCollectionExtensions.cs`** — DI registration
   - `AddEncinaDataResidency(this IServiceCollection, Action<DataResidencyOptions>? configure)`
   - TryAdd defaults: InMemory stores, DefaultPolicy, DefaultValidator, DefaultProvider, DefaultRouter
   - Register pipeline behavior: `typeof(IPipelineBehavior<,>)` → `typeof(DataResidencyPipelineBehavior<,>)`
   - Conditional: health check, auto-registration hosted service
   - `TryAddSingleton(TimeProvider.System)`

7. **`DataResidencyAutoRegistrationDescriptor.cs`** — Internal record with assemblies list

8. **`DataResidencyAutoRegistrationHostedService.cs`** — `BackgroundService`
   - Scans assemblies for `[DataResidency]` attributes at startup
   - Creates `ResidencyPolicyDescriptor` for each discovered category
   - Registers fluent-configured policies from options

9. **`DataResidencyFluentPolicyDescriptor.cs`** — Internal record for DI
   - Holds policies from fluent configuration

10. **`DataResidencyFluentPolicyHostedService.cs`** — `BackgroundService`
    - Registers fluent-configured policies into `IResidencyPolicyStore` at startup

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```
CONTEXT:
You are implementing Phase 5 of Encina.Compliance.DataResidency (#405).
Phases 1-4 are complete — models, interfaces, implementations, attributes, pipeline behavior exist.

TASK:
1. Create DataResidencyOptions with fluent policy configuration
2. Create options validator
3. Create ResidencyPolicyBuilder for fluent API
4. Create enforcement mode enum (Block/Warn/Disabled)
5. Create DataResidencyErrors static factory
6. Create ServiceCollectionExtensions with AddEncinaDataResidency()
7. Create auto-registration descriptor and hosted service
8. Create fluent policy descriptor and hosted service

KEY RULES:
- TryAdd* for all default registrations (user can override)
- TryAddSingleton for InMemory stores, DefaultPolicy, DefaultValidator, DefaultProvider
- TryAddTransient for pipeline behavior (open generic)
- Conditional registrations for health check and auto-registration
- Error codes follow "residency.{category}" pattern
- Options validator uses IValidateOptions<T>
- Auto-registration scans for [DataResidency] attributes on command/query types

REFERENCE FILES:
- src/Encina.Compliance.Retention/RetentionOptions.cs
- src/Encina.Compliance.Retention/ServiceCollectionExtensions.cs
- src/Encina.Compliance.Retention/RetentionErrors.cs
- src/Encina.Compliance.Retention/RetentionAutoRegistrationHostedService.cs
- src/Encina.Compliance.Retention/RetentionFluentPolicyHostedService.cs
```

</details>

---

### Phase 6: Persistence Entities, Mappers & SQL Scripts

<details>
<summary>Tasks</summary>

1. **`DataLocationEntity.cs`** — Persistence entity
   - `Id` (string), `EntityId` (string), `DataCategory` (string), `RegionCode` (string), `StorageTypeValue` (int), `StoredAtUtc` (DateTimeOffset), `LastVerifiedAtUtc` (DateTimeOffset?), `Metadata` (string?)

2. **`DataLocationMapper.cs`** — Static bidirectional mapper
   - `ToEntity(DataLocation)` → `DataLocationEntity`
   - `ToDomain(DataLocationEntity)` → `DataLocation?` (null if invalid enum)

3. **`ResidencyPolicyEntity.cs`** — Persistence entity
   - `DataCategory` (string PK), `AllowedRegionCodes` (string — comma-separated), `RequireAdequacyDecision` (bool), `AllowedTransferBasesValue` (string — comma-separated ints), `CreatedAtUtc` (DateTimeOffset), `LastModifiedAtUtc` (DateTimeOffset?)

4. **`ResidencyPolicyMapper.cs`** — Static bidirectional mapper
   - `ToEntity(ResidencyPolicyDescriptor)` → `ResidencyPolicyEntity`
   - `ToDomain(ResidencyPolicyEntity)` → `ResidencyPolicyDescriptor?`

5. **`ResidencyAuditEntryEntity.cs`** — Persistence entity
   - `Id` (string), `EntityId` (string?), `DataCategory` (string), `SourceRegion` (string), `TargetRegion` (string?), `ActionValue` (int), `OutcomeValue` (int), `LegalBasis` (string?), `RequestType` (string?), `UserId` (string?), `TimestampUtc` (DateTimeOffset), `Details` (string?)

6. **`ResidencyAuditEntryMapper.cs`** — Static bidirectional mapper

7. **SQL Scripts** — For each provider (PostgreSQL, SqlServer, MySQL, SQLite):

   **`CreateDataLocationsTable`**:
   ```sql
   CREATE TABLE datalocations (
       id VARCHAR(36) NOT NULL PRIMARY KEY,
       entityid VARCHAR(256) NOT NULL,
       datacategory VARCHAR(256) NOT NULL,
       regioncode VARCHAR(32) NOT NULL,
       storagetypevalue INT NOT NULL,
       storedatutc TIMESTAMPTZ NOT NULL,
       lastverifiedatutc TIMESTAMPTZ NULL,
       metadata TEXT NULL
   );
   CREATE INDEX ix_datalocations_entityid ON datalocations (entityid);
   CREATE INDEX ix_datalocations_regioncode ON datalocations (regioncode);
   CREATE INDEX ix_datalocations_datacategory ON datalocations (datacategory);
   ```

   **`CreateResidencyPoliciesTable`**:
   ```sql
   CREATE TABLE residencypolicies (
       datacategory VARCHAR(256) NOT NULL PRIMARY KEY,
       allowedregioncodes VARCHAR(1024) NOT NULL,
       requireadequacydecision BOOLEAN NOT NULL,
       allowedtransferbasesvalue VARCHAR(256) NULL,
       createdatutc TIMESTAMPTZ NOT NULL,
       lastmodifiedatutc TIMESTAMPTZ NULL
   );
   ```

   **`CreateResidencyAuditTable`**:
   ```sql
   CREATE TABLE residencyauditentries (
       id VARCHAR(36) NOT NULL PRIMARY KEY,
       entityid VARCHAR(256) NULL,
       datacategory VARCHAR(256) NOT NULL,
       sourceregion VARCHAR(32) NOT NULL,
       targetregion VARCHAR(32) NULL,
       actionvalue INT NOT NULL,
       outcomevalue INT NOT NULL,
       legalbasis VARCHAR(256) NULL,
       requesttype VARCHAR(512) NULL,
       userid VARCHAR(256) NULL,
       timestamputc TIMESTAMPTZ NOT NULL,
       details TEXT NULL
   );
   CREATE INDEX ix_residencyaudit_entityid ON residencyauditentries (entityid);
   CREATE INDEX ix_residencyaudit_timestamputc ON residencyauditentries (timestamputc);
   CREATE INDEX ix_residencyaudit_outcomevalue ON residencyauditentries (outcomevalue);
   ```

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```
CONTEXT:
You are implementing Phase 6 of Encina.Compliance.DataResidency (#405).
Phases 1-5 are complete. Now creating persistence entities, mappers, and SQL scripts.

TASK:
1. Create 3 entity classes and 3 mapper classes in the core package
2. Create SQL scripts for all 4 database variants (PostgreSQL, SqlServer, MySQL, SQLite)

KEY RULES:
- Entities use primitive types only (string, int, bool, DateTimeOffset)
- Enums stored as int (StorageType, ResidencyAction, ResidencyOutcome)
- Lists stored as comma-separated strings (AllowedRegionCodes, AllowedTransferBases)
- Mappers are static classes with ToEntity/ToDomain methods
- ToDomain returns null for invalid enum values (defensive)
- SQL column names are lowercase
- PostgreSQL: TIMESTAMPTZ, BOOLEAN
- SQL Server: DATETIME2, BIT, NVARCHAR
- MySQL: DATETIME, TINYINT(1), VARCHAR with backticks
- SQLite: TEXT for dates (ISO 8601 "O" format), INTEGER for booleans (0/1)

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/DSRRequestEntity.cs
- src/Encina.Compliance.DataSubjectRights/DSRRequestMapper.cs
- src/Encina.ADO.PostgreSQL/Scripts/ (existing SQL scripts)
- src/Encina.ADO.SqlServer/Scripts/ (SQL Server scripts)
```

</details>

---

### Phase 7: Multi-Provider Store Implementations (All 13 Providers)

<details>
<summary>Tasks</summary>

For each of the 3 store interfaces (`IDataLocationStore`, `IResidencyPolicyStore`, `IResidencyAuditStore`), implement in all 13 providers:

**ADO.NET (4 providers × 3 stores = 12 files)**:

| Provider | Files | Namespace |
|----------|-------|-----------|
| `Encina.ADO.Sqlite` | `DataResidency/DataLocationStoreADO.cs`, `DataResidency/ResidencyPolicyStoreADO.cs`, `DataResidency/ResidencyAuditStoreADO.cs` | `Encina.ADO.Sqlite.DataResidency` |
| `Encina.ADO.SqlServer` | Same 3 files | `Encina.ADO.SqlServer.DataResidency` |
| `Encina.ADO.PostgreSQL` | Same 3 files | `Encina.ADO.PostgreSQL.DataResidency` |
| `Encina.ADO.MySQL` | Same 3 files | `Encina.ADO.MySQL.DataResidency` |

**Dapper (4 providers × 3 stores = 12 files)**:

| Provider | Files | Namespace |
|----------|-------|-----------|
| `Encina.Dapper.Sqlite` | `DataResidency/DataLocationStoreDapper.cs`, `DataResidency/ResidencyPolicyStoreDapper.cs`, `DataResidency/ResidencyAuditStoreDapper.cs` | `Encina.Dapper.Sqlite.DataResidency` |
| `Encina.Dapper.SqlServer` | Same 3 files | `Encina.Dapper.SqlServer.DataResidency` |
| `Encina.Dapper.PostgreSQL` | Same 3 files | `Encina.Dapper.PostgreSQL.DataResidency` |
| `Encina.Dapper.MySQL` | Same 3 files | `Encina.Dapper.MySQL.DataResidency` |

**EF Core (4 providers × 3 stores = 12 files)**:

| Provider | Files | Namespace |
|----------|-------|-----------|
| `Encina.EntityFrameworkCore` | `DataResidency/DataLocationStoreEF.cs`, `DataResidency/ResidencyPolicyStoreEF.cs`, `DataResidency/ResidencyAuditStoreEF.cs` | `Encina.EntityFrameworkCore.DataResidency` |

> Note: EF Core has a single project with database-agnostic implementation. The 4 EF Core "providers" (Sqlite, SqlServer, PostgreSQL, MySQL) are configured at the `DbContext` level.

**MongoDB (1 provider × 3 stores = 3 files)**:

| Provider | Files | Namespace |
|----------|-------|-----------|
| `Encina.MongoDB` | `DataResidency/DataLocationStoreMongoDB.cs`, `DataResidency/ResidencyPolicyStoreMongoDB.cs`, `DataResidency/ResidencyAuditStoreMongoDB.cs` | `Encina.MongoDB.DataResidency` |

**DI Registration updates** (add `UseDataResidency` flag):

1. Update `Encina.Messaging/MessagingConfiguration.cs` — Add `UseDataResidency` property
2. Update `Encina.ADO.Sqlite/ServiceCollectionExtensions.cs` — Add `if (config.UseDataResidency)` block
3. Update `Encina.ADO.SqlServer/ServiceCollectionExtensions.cs` — Same
4. Update `Encina.ADO.PostgreSQL/ServiceCollectionExtensions.cs` — Same
5. Update `Encina.ADO.MySQL/ServiceCollectionExtensions.cs` — Same
6. Update `Encina.Dapper.Sqlite/ServiceCollectionExtensions.cs` — Same
7. Update `Encina.Dapper.SqlServer/ServiceCollectionExtensions.cs` — Same
8. Update `Encina.Dapper.PostgreSQL/ServiceCollectionExtensions.cs` — Same
9. Update `Encina.Dapper.MySQL/ServiceCollectionExtensions.cs` — Same
10. Update `Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs` — Same
11. Update `Encina.MongoDB/ServiceCollectionExtensions.cs` — Same

**Total**: 39 store files + 12 DI updates = 51 files

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```
CONTEXT:
You are implementing Phase 7 of Encina.Compliance.DataResidency (#405).
Phases 1-6 are complete — entities, mappers, and SQL scripts exist.
This is the largest phase: 39 store implementations + 12 DI updates.

TASK:
1. Create 3 ADO store classes for EACH of 4 databases (12 files total)
2. Create 3 Dapper store classes for EACH of 4 databases (12 files total)
3. Create 3 EF Core store classes in Encina.EntityFrameworkCore (3 files)
4. Create 3 MongoDB store classes in Encina.MongoDB (3 files)
5. Add UseDataResidency to MessagingConfiguration
6. Update all 11 satellite ServiceCollectionExtensions to register stores

KEY RULES (provider-specific):
- ADO: IDbCommand + manual parameters, provider-specific async (NpgsqlCommand, SqlCommand, etc.)
- Dapper: connection.ExecuteAsync/QueryAsync with anonymous objects
- EF Core: DbContext.Set<T>(), SaveChangesAsync, catch DbUpdateException
- MongoDB: IMongoCollection<T>, Find/ReplaceOneAsync/DeleteManyAsync
- SQLite: dates as ISO 8601 string format ("O"), booleans as 0/1, never datetime('now')
- SQL Server: @param, TOP(@n), NVARCHAR, DATETIME2, BIT
- PostgreSQL: @param, LIMIT @n, TIMESTAMPTZ, BOOLEAN
- MySQL: @param, LIMIT @n, backtick identifiers, TINYINT(1) for bool
- All stores: constructor takes IDbConnection (ADO/Dapper), DbContext (EF), IMongoClient + options (MongoDB)
- All stores: table name parameter with SqlIdentifierValidator.ValidateTableName()
- All stores: TimeProvider injection for time-based operations
- DI registration: TryAddScoped under if (config.UseDataResidency) block
- OperationCanceledException must be re-thrown, never wrapped

REFERENCE FILES:
- src/Encina.ADO.PostgreSQL/Retention/ (RetentionPolicyStoreADO.cs, etc.)
- src/Encina.Dapper.PostgreSQL/Retention/ (RetentionPolicyStoreDapper.cs, etc.)
- src/Encina.EntityFrameworkCore/Retention/ (RetentionPolicyStoreEF.cs, etc.)
- src/Encina.MongoDB/DataSubjectRights/ (DSRRequestStoreMongoDB.cs)
- src/Encina.ADO.PostgreSQL/ServiceCollectionExtensions.cs (DI pattern)
- src/Encina.Messaging/MessagingConfiguration.cs (feature flags)
```

</details>

---

### Phase 8: ASP.NET Core Integration & Health Check

<details>
<summary>Tasks</summary>

1. **`Health/DataResidencyHealthCheck.cs`** — In core package
   - `public const string DefaultName = "encina-data-residency"`
   - `private static readonly string[] DefaultTags = ["encina", "gdpr", "residency", "compliance", "ready"]`
   - Constructor: `IServiceProvider`, `ILogger`
   - Uses scoped resolution via `_serviceProvider.CreateScope()`
   - Checks: options configured, stores registered, default region set
   - Returns: Healthy, Degraded (optional stores missing), or Unhealthy (critical stores missing)

2. **ASP.NET Core middleware update** — Update `Encina.AspNetCore`
   - Update `EncinaContextMiddleware.cs` to extract `X-Data-Region` header
   - Set region on `IRequestContext` if header present
   - Update `EncinaAspNetCoreOptions.cs` — add `DataRegionHeaderName` property (default: `"X-Data-Region"`)

3. **`HttpRegionContextProvider.cs`** — In `Encina.AspNetCore`
   - Implements `IRegionContextProvider`
   - Resolves region from HTTP context header → tenant mapping → default
   - Registered as scoped service (HTTP request lifetime)

</details>

<details>
<summary>Prompt for AI Agents — Phase 8</summary>

```
CONTEXT:
You are implementing Phase 8 of Encina.Compliance.DataResidency (#405).
Phases 1-7 are complete — all stores and core services exist.

TASK:
1. Create DataResidencyHealthCheck in the core compliance package
2. Update EncinaContextMiddleware to extract X-Data-Region header
3. Create HttpRegionContextProvider in Encina.AspNetCore

KEY RULES:
- Health check uses IServiceProvider.CreateScope() for scoped dependencies
- Health check has DefaultName const and Tags static array
- Middleware extracts header ONLY if present (no exception if missing)
- HttpRegionContextProvider fallback chain: header → tenant → default
- Don't break existing middleware behavior — only add region extraction

REFERENCE FILES:
- src/Encina.Compliance.Retention/Health/RetentionHealthCheck.cs
- src/Encina.AspNetCore/EncinaContextMiddleware.cs
- src/Encina.AspNetCore/EncinaAspNetCoreOptions.cs
```

</details>

---

### Phase 9: Observability

<details>
<summary>Tasks</summary>

1. **`Diagnostics/DataResidencyDiagnostics.cs`**
   - `internal const string SourceName = "Encina.Compliance.DataResidency"`
   - `internal const string SourceVersion = "1.0"`
   - `internal static readonly ActivitySource ActivitySource = new(SourceName, SourceVersion)`
   - `internal static readonly Meter Meter = new(SourceName, SourceVersion)`

   **Tag constants**:
   - `TagOutcome = "residency.outcome"` — allowed, blocked, skipped, warning
   - `TagRequestType = "residency.request_type"`
   - `TagResponseType = "residency.response_type"`
   - `TagDataCategory = "residency.data_category"`
   - `TagSourceRegion = "residency.source_region"`
   - `TagTargetRegion = "residency.target_region"`
   - `TagEnforcementMode = "residency.enforcement_mode"`
   - `TagLegalBasis = "residency.legal_basis"`
   - `TagAction = "residency.action"`
   - `TagFailureReason = "residency.failure_reason"`

   **Counters**:
   - `PipelineExecutionsTotal` — `"residency.pipeline.executions.total"`
   - `PolicyChecksTotal` — `"residency.policy.checks.total"`
   - `CrossBorderTransfersTotal` — `"residency.transfers.total"`
   - `TransfersBlockedTotal` — `"residency.transfers.blocked.total"`
   - `LocationRecordsTotal` — `"residency.locations.recorded.total"`
   - `ViolationsTotal` — `"residency.violations.total"`
   - `AuditEntriesTotal` — `"residency.audit.entries.total"`

   **Histograms**:
   - `PipelineDuration` — `"residency.pipeline.duration"` (ms)
   - `TransferValidationDuration` — `"residency.transfer.validation.duration"` (ms)

   **Activity helpers**:
   - `StartPipelineExecution(requestType, responseType)` → Activity?
   - `StartTransferValidation(source, destination)` → Activity?
   - `StartLocationRecord(entityId, regionCode)` → Activity?
   - `RecordCompleted(activity)`, `RecordBlocked(activity, reason)`, `RecordSkipped(activity)`

2. **`Diagnostics/DataResidencyLogMessages.cs`**
   - Uses `[LoggerMessage]` source generator
   - **Event ID range: 8600-8699**

   | Range | Feature Area |
   |-------|-------------|
   | 8600-8609 | Pipeline behavior |
   | 8610-8619 | Transfer validation |
   | 8620-8629 | Auto-registration |
   | 8630-8639 | Health check |
   | 8640-8649 | Policy management |
   | 8650-8659 | Location tracking |
   | 8660-8669 | Audit trail |
   | 8670-8679 | Region resolution |
   | 8680-8699 | Reserved |

   **Key messages**:
   - 8600: `PipelineDisabled` (Trace) — enforcement disabled, skip
   - 8601: `PipelineSkippedNoAttribute` (Trace) — no residency attribute
   - 8602: `ResidencyCheckPassed` (Debug) — region allowed
   - 8603: `ResidencyCheckBlocked` (Warning) — region not allowed, blocked
   - 8604: `ResidencyCheckWarning` (Warning) — region not allowed, warn-only mode
   - 8605: `RegionResolutionFailed` (Error) — could not determine current region
   - 8610: `TransferAllowed` (Debug) — cross-border transfer validated
   - 8611: `TransferDenied` (Warning) — cross-border transfer blocked
   - 8612: `TransferAdequacyCheck` (Debug) — adequacy decision check result
   - 8620: `AutoRegistrationStarted` (Information) — scanning assemblies
   - 8621: `AutoRegistrationCompleted` (Information) — policies discovered
   - 8622: `AutoRegistrationSkipped` (Debug) — no assemblies to scan
   - 8623: `PolicyDiscovered` (Debug) — attribute found on type
   - 8630: `HealthCheckCompleted` (Debug) — health check result
   - 8640: `PolicyCreated` (Information) — new policy registered
   - 8641: `PolicyNotFound` (Debug) — no policy for category
   - 8650: `LocationRecorded` (Debug) — data location tracked
   - 8651: `LocationRecordFailed` (Warning) — failed to record location
   - 8660: `AuditEntryRecorded` (Debug) — audit entry created
   - 8670: `RegionResolvedFromHeader` (Debug) — region from X-Data-Region
   - 8671: `RegionResolvedFromTenant` (Debug) — region from tenant config
   - 8672: `RegionResolvedFromDefault` (Debug) — region from default config

</details>

<details>
<summary>Prompt for AI Agents — Phase 9</summary>

```
CONTEXT:
You are implementing Phase 9 of Encina.Compliance.DataResidency (#405).
Phases 1-8 are complete. Now adding observability: ActivitySource, Meter, LoggerMessage.

TASK:
1. Create DataResidencyDiagnostics.cs with ActivitySource, Meter, tag constants, counters, histograms, activity helpers
2. Create DataResidencyLogMessages.cs with [LoggerMessage] source generator methods
3. Event ID range: 8600-8699 (verify no collisions — 8500-8599 is Retention, next available is 8600)

KEY RULES:
- All fields are internal static readonly
- Tag names use "residency." prefix
- Counters are Counter<long>, histograms are Histogram<double>
- [LoggerMessage] uses partial methods with the source generator
- Activity helpers check HasListeners() before creating activities
- RecordCompleted/RecordBlocked set activity status codes
- Each log message has descriptive, structured parameters

REFERENCE FILES:
- src/Encina.Compliance.Retention/Diagnostics/RetentionDiagnostics.cs
- src/Encina.Compliance.Retention/Diagnostics/RetentionLogMessages.cs
- src/Encina.Compliance.DataSubjectRights/Diagnostics/DataSubjectRightsDiagnostics.cs
```

</details>

---

### Phase 10: Testing

<details>
<summary>Tasks</summary>

**Unit Tests** (`tests/Encina.UnitTests/Compliance/DataResidency/`):

1. `RegionTests.cs` — Region equality, static instances, Create factory
2. `RegionRegistryTests.cs` — EU members count, EEA countries, adequacy list, GetByCode
3. `RegionGroupTests.cs` — Contains checks, static instances
4. `DataResidencyOptionsTests.cs` — Default values, AddPolicy fluent API, validation
5. `DataResidencyErrorsTests.cs` — Error factory methods return correct codes
6. `DataResidencyPipelineBehaviorTests.cs` — Attribute caching, enforcement modes, region validation
7. `DefaultDataResidencyPolicyTests.cs` — Allowed/denied regions, fallback behavior
8. `DefaultCrossBorderTransferValidatorTests.cs` — EU free movement, adequacy, SCCs, denial
9. `DefaultAdequacyDecisionProviderTests.cs` — Known adequate countries, unknown regions
10. `DefaultRegionContextProviderTests.cs` — Fallback chain resolution
11. `DefaultRegionRouterTests.cs` — Target region determination
12. `DataResidencyAutoRegistrationTests.cs` — Assembly scanning, attribute discovery
13. `InMemoryDataLocationStoreTests.cs` — CRUD operations, concurrency
14. `InMemoryResidencyPolicyStoreTests.cs` — CRUD operations
15. `InMemoryResidencyAuditStoreTests.cs` — Record and query
16. `DataLocationMapperTests.cs` — ToEntity/ToDomain round-trip
17. `ResidencyPolicyMapperTests.cs` — ToEntity/ToDomain, comma-separated parsing
18. `ResidencyAuditEntryMapperTests.cs` — ToEntity/ToDomain
19. `DataResidencyHealthCheckTests.cs` — Healthy, Degraded, Unhealthy states

**Guard Tests** (`tests/Encina.GuardTests/Compliance/DataResidency/`):

20. `DataResidencyGuardTests.cs` — ArgumentNullException for all public constructors and methods

**Contract Tests** (`tests/Encina.ContractTests/Compliance/DataResidency/`):

21. `DataLocationStoreContractTests.cs` — All providers follow same contract
22. `ResidencyPolicyStoreContractTests.cs` — All providers follow same contract
23. `ResidencyAuditStoreContractTests.cs` — All providers follow same contract

**Property Tests** (`tests/Encina.PropertyTests/Compliance/DataResidency/`):

24. `RegionPropertyTests.cs` — Equality invariants, round-trip properties
25. `TransferValidationPropertyTests.cs` — EU free movement always allowed, same-region always allowed
26. `MapperPropertyTests.cs` — ToEntity/ToDomain round-trip for random inputs

**Integration Tests** (`tests/Encina.IntegrationTests/Compliance/DataResidency/`):

27-39. One integration test class per provider (13 classes):
   - `ADO/Sqlite/DataResidencyStoreTests.cs` — `[Collection("ADO-Sqlite")]`
   - `ADO/SqlServer/DataResidencyStoreTests.cs` — `[Collection("ADO-SqlServer")]`
   - `ADO/PostgreSQL/DataResidencyStoreTests.cs` — `[Collection("ADO-PostgreSQL")]`
   - `ADO/MySQL/DataResidencyStoreTests.cs` — `[Collection("ADO-MySQL")]`
   - `Dapper/Sqlite/DataResidencyStoreTests.cs` — `[Collection("Dapper-Sqlite")]`
   - `Dapper/SqlServer/DataResidencyStoreTests.cs` — `[Collection("Dapper-SqlServer")]`
   - `Dapper/PostgreSQL/DataResidencyStoreTests.cs` — `[Collection("Dapper-PostgreSQL")]`
   - `Dapper/MySQL/DataResidencyStoreTests.cs` — `[Collection("Dapper-MySQL")]`
   - `EFCore/Sqlite/DataResidencyStoreTests.cs` — `[Collection("EFCore-Sqlite")]`
   - `EFCore/SqlServer/DataResidencyStoreTests.cs` — `[Collection("EFCore-SqlServer")]`
   - `EFCore/PostgreSQL/DataResidencyStoreTests.cs` — `[Collection("EFCore-PostgreSQL")]`
   - `EFCore/MySQL/DataResidencyStoreTests.cs` — `[Collection("EFCore-MySQL")]`
   - `MongoDB/DataResidencyStoreTests.cs` (if MongoDB collection fixture exists)

**Load Tests** — `.md` justification (no concurrent behavior):

40. `tests/Encina.LoadTests/Compliance/DataResidency/DataResidency.md` — Pipeline is thin validation, no concurrency concern

**Benchmark Tests** — `.md` justification (not a hot path for most apps):

41. `tests/Encina.BenchmarkTests/Encina.Benchmarks/Compliance/DataResidency/DataResidency.md`

</details>

<details>
<summary>Prompt for AI Agents — Phase 10</summary>

```
CONTEXT:
You are implementing Phase 10 of Encina.Compliance.DataResidency (#405).
Phases 1-9 are complete — all production code exists. Now creating tests.

TASK:
1. Create ~19 unit test files in tests/Encina.UnitTests/Compliance/DataResidency/
2. Create guard tests in tests/Encina.GuardTests/Compliance/DataResidency/
3. Create 3 contract test files in tests/Encina.ContractTests/Compliance/DataResidency/
4. Create 3 property test files in tests/Encina.PropertyTests/Compliance/DataResidency/
5. Create 13 integration test files (one per database provider)
6. Create .md justification files for LoadTests and BenchmarkTests

KEY RULES:
- Unit tests: AAA pattern, mock all dependencies, fast execution (<1ms per test)
- Guard tests: verify ArgumentNullException for all public parameters
- Contract tests: ensure all provider implementations return same results for same inputs
- Property tests: FsCheck, test invariants (EU free movement, round-trip mappers)
- Integration tests: [Collection("Provider-DB")] shared fixtures, ClearAllDataAsync in InitializeAsync
- SQLite integration: NEVER dispose shared connection from fixture
- All test methods have descriptive names: MethodName_Scenario_ExpectedResult
- Use xUnit [Fact] and [Theory] with [InlineData] where appropriate
- Mock ILogger with NSubstitute or Moq
- Mock stores for unit tests, use real stores for integration tests

REFERENCE FILES:
- tests/Encina.UnitTests/Compliance/Retention/ (unit test patterns)
- tests/Encina.IntegrationTests/Compliance/Retention/ (integration test patterns)
- tests/Encina.GuardTests/Compliance/Retention/ (guard test patterns)
- tests/Encina.ContractTests/Compliance/Retention/ (contract test patterns)
- tests/Encina.PropertyTests/Compliance/Retention/ (property test patterns)
```

</details>

---

### Phase 11: Documentation & Finalization

<details>
<summary>Tasks</summary>

1. **XML documentation** — Verify all public APIs have `<summary>`, `<remarks>`, `<param>`, `<returns>`, `<example>` where appropriate

2. **`CHANGELOG.md`** — Add entry under `## [Unreleased]`:
   ```markdown
   ### Added
   - `Encina.Compliance.DataResidency` — Data sovereignty and residency enforcement (GDPR Chapter V, Arts. 44-49) with region policies, cross-border transfer validation, location tracking, and audit trail across all 13 database providers (Fixes #405)
   ```

3. **`ROADMAP.md`** — Update if milestone v0.13.0 is affected

4. **`src/Encina.Compliance.DataResidency/README.md`** — Package README with:
   - Overview and motivation
   - Quick start
   - Region model and registry
   - Policy configuration
   - Cross-border transfer rules
   - Pipeline behavior
   - Attributes usage
   - Health check
   - Observability

5. **`docs/INVENTORY.md`** — Add new package and files

6. **`PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt`** — Ensure all public symbols tracked

7. **Build verification**: `dotnet build --configuration Release` → 0 errors, 0 warnings

8. **Test verification**: `dotnet test` → all pass

</details>

<details>
<summary>Prompt for AI Agents — Phase 11</summary>

```
CONTEXT:
You are implementing Phase 11 (final) of Encina.Compliance.DataResidency (#405).
All production code and tests are complete. Now finalizing documentation.

TASK:
1. Verify XML documentation on all public APIs
2. Update CHANGELOG.md with new feature entry
3. Update ROADMAP.md if applicable
4. Create package README.md
5. Update docs/INVENTORY.md
6. Ensure PublicAPI.Unshipped.txt is complete
7. Run dotnet build --configuration Release — fix any warnings
8. Run dotnet test — fix any failures
9. Commit with message: feat: add Encina.Compliance.DataResidency - data sovereignty and residency enforcement with 13 database providers (Fixes #405)

KEY RULES:
- No [Obsolete], no backward compatibility notes
- No Co-Authored-By or AI attribution in commits
- CHANGELOG follows Keep a Changelog format
- README should show code examples for quick start
- All 0 warnings in Release build

REFERENCE FILES:
- src/Encina.Compliance.Retention/README.md (package README pattern)
- CHANGELOG.md (existing entries)
- docs/INVENTORY.md (existing inventory)
```

</details>

---

## Research

### Relevant Standards & Regulations

| Standard | Article(s) | Relevance |
|----------|-----------|-----------|
| GDPR Chapter V | Arts. 44-49 | International data transfers, adequacy decisions, safeguards |
| GDPR Art. 30 | Records of processing | Audit trail for data location tracking |
| GDPR Art. 5(1)(f) | Integrity & Confidentiality | Data must be processed securely in transit |
| Schrems II (CJEU C-311/18) | — | Invalidated EU-US Privacy Shield, requires additional safeguards |
| EU-US Data Privacy Framework | — | Replaced Privacy Shield (2023), provides adequacy for certified US companies |
| CCPA/CPRA | Cal. Civ. Code §1798 | California data residency requirements |
| LGPD (Brazil) | Arts. 33-36 | Brazilian cross-border transfer rules |
| PIPL (China) | Arts. 38-43 | Chinese data localization requirements |
| PDPA (Singapore) | Part VIA | Transfer limitation obligation |
| POPIA (South Africa) | §72 | Cross-border data transfer conditions |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in DataResidency |
|-----------|----------|----------------------|
| `GeoRegion` | `src/Encina/Sharding/Routing/GeoRegion.cs` | Optional adapter for region-to-shard mapping |
| `GeoShardRouter` | `src/Encina/Sharding/Routing/GeoShardRouter.cs` | Optional integration for region-aware routing |
| `IRequestContext` | `src/Encina/IRequestContext.cs` | Carries region context through pipeline |
| `EncinaContextMiddleware` | `src/Encina.AspNetCore/EncinaContextMiddleware.cs` | HTTP header extraction for region |
| `IPipelineBehavior<,>` | `src/Encina/IPipelineBehavior.cs` | Pipeline behavior registration |
| `Either<EncinaError, T>` | `src/Encina/Either.cs` | ROP pattern for all store methods |
| `MessagingConfiguration` | `src/Encina.Messaging/MessagingConfiguration.cs` | Feature flag registration |
| `SqlIdentifierValidator` | Provider packages | Table name validation |
| `TimeProvider` | .NET 10 built-in | Deterministic time for testing |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Security` | 8000-8099 | Authorization |
| `Encina.Compliance.GDPR` | 8100-8199 | Core GDPR compliance |
| `Encina.Compliance.Consent` | 8200-8299 | Consent management |
| `Encina.Compliance.DataSubjectRights` | 8300-8399 | DSR (Arts. 15-22) |
| `Encina.Compliance.Anonymization` | 8400-8499 | Anonymization/Pseudonymization |
| `Encina.Compliance.Retention` | 8500-8599 | Data retention & deletion |
| **`Encina.Compliance.DataResidency`** | **8600-8699** | **Data sovereignty (this feature)** |
| *(Available)* | 8700-8999 | Future compliance modules |

### Estimated File Count by Category

| Category | Files | Notes |
|----------|-------|-------|
| Core models & enums | 13 | Region, DataLocation, TransferResult, etc. |
| Abstractions (interfaces) | 8 | IDataResidencyPolicy, IDataLocationStore, etc. |
| Default implementations | 5 | DefaultPolicy, DefaultValidator, etc. |
| InMemory stores | 3 | Default store implementations |
| Attributes | 2 | [DataResidency], [NoCrossBorderTransfer] |
| Pipeline behavior | 3 | Behavior + 2 cached info records |
| Configuration & DI | 10 | Options, validator, builder, errors, extensions, hosted services |
| Persistence entities & mappers | 6 | 3 entities + 3 mappers |
| SQL scripts | ~12 | 3 tables × 4 providers |
| Diagnostics | 2 | Diagnostics + LogMessages |
| Health check | 1 | DataResidencyHealthCheck |
| ASP.NET Core integration | 2 | HttpRegionContextProvider + middleware update |
| Provider stores (ADO) | 12 | 3 stores × 4 databases |
| Provider stores (Dapper) | 12 | 3 stores × 4 databases |
| Provider stores (EF Core) | 3 | 3 stores × 1 project |
| Provider stores (MongoDB) | 3 | 3 stores × 1 project |
| DI updates (satellites) | 12 | 11 providers + MessagingConfiguration |
| Unit tests | ~19 | Core logic tests |
| Guard tests | 1 | Parameter validation |
| Contract tests | 3 | Provider consistency |
| Property tests | 3 | Invariant verification |
| Integration tests | 13 | One per provider |
| Load/Benchmark justifications | 2 | .md files |
| Documentation | 5 | README, CHANGELOG, INVENTORY, PublicAPI |
| **Total** | **~153** | |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined Prompt for All Phases</strong></summary>

```
PROJECT CONTEXT:
You are implementing Encina.Compliance.DataResidency for issue #405.
Encina is a .NET 10 / C# 14 framework for building distributed applications.
This is a compliance module that enforces data sovereignty and residency rules.

IMPLEMENTATION OVERVIEW:
- New package: src/Encina.Compliance.DataResidency/
- 3 store interfaces: IDataLocationStore, IResidencyPolicyStore, IResidencyAuditStore
- All 13 database providers: ADO (4), Dapper (4), EF Core (4), MongoDB (1)
- Pipeline behavior: DataResidencyPipelineBehavior with [DataResidency] and [NoCrossBorderTransfer] attributes
- Region model with EU adequacy decisions
- Cross-border transfer validation
- ASP.NET Core integration for HTTP region context
- Observability: ActivitySource + Meter + LoggerMessage (Event IDs 8600-8699)
- Health check with degraded status support

KEY PATTERNS:
1. ROP: All store methods return ValueTask<Either<EncinaError, T>>
2. Static attribute caching per closed generic type in pipeline behaviors
3. ConcurrentDictionary for InMemory stores
4. TryAdd* for all default DI registrations
5. Enforcement modes: Block/Warn/Disabled
6. Entity + Mapper pattern for persistence (enums as int, lists as comma-separated)
7. SQL scripts per provider (PostgreSQL=TIMESTAMPTZ, SqlServer=DATETIME2, MySQL=backticks, SQLite=TEXT)
8. [Collection("Provider-DB")] for integration test fixtures
9. Health check with DefaultName const, Tags array, scoped resolution

REFERENCE FILES:
- Core compliance patterns:
  src/Encina.Compliance.Retention/ (full package reference)
  src/Encina.Compliance.DataSubjectRights/ (pipeline, stores, auto-registration)
  src/Encina.Compliance.Anonymization/ (attribute, mapper patterns)

- Satellite providers:
  src/Encina.ADO.PostgreSQL/Retention/ (ADO store pattern)
  src/Encina.Dapper.PostgreSQL/Retention/ (Dapper store pattern)
  src/Encina.EntityFrameworkCore/Retention/ (EF Core store pattern)
  src/Encina.MongoDB/DataSubjectRights/ (MongoDB store pattern)
  src/Encina.ADO.PostgreSQL/ServiceCollectionExtensions.cs (DI registration)
  src/Encina.Messaging/MessagingConfiguration.cs (feature flags)

- ASP.NET Core:
  src/Encina.AspNetCore/EncinaContextMiddleware.cs (header extraction)
  src/Encina.AspNetCore/EncinaAspNetCoreOptions.cs (options)

- Testing:
  tests/Encina.UnitTests/Compliance/Retention/ (unit test patterns)
  tests/Encina.IntegrationTests/Compliance/Retention/ (integration test patterns)

PHASES:
1. Core Models & Enums (13 files)
2. Core Interfaces & Abstractions (8 files)
3. Default Implementations & InMemory Stores (8 files)
4. Attributes & Pipeline Behavior (5 files)
5. Configuration, DI & Errors (10 files)
6. Persistence Entities, Mappers & SQL Scripts (6 + ~12 files)
7. Multi-Provider Store Implementations (39 stores + 12 DI updates)
8. ASP.NET Core Integration & Health Check (3 files)
9. Observability (2 files)
10. Testing (~41 files)
11. Documentation & Finalization (5 files)

CRITICAL RULES:
- .NET 10, C# 14, nullable enabled
- No [Obsolete], no backward compatibility
- All public APIs need XML documentation
- SQLite: dates as ISO 8601 "O" format, never datetime('now'), never dispose shared connection
- OperationCanceledException must be re-thrown, never wrapped
- Pre-1.0: choose the best solution
```

</details>

---

## Next Steps

1. **Review** — Review this plan for completeness and alignment with issue #405
2. **Publish** — Post as a comment on issue #405 for team visibility
3. **Implement** — Execute phases 1-11 sequentially, committing after each phase
4. **Final commit** — `feat: add Encina.Compliance.DataResidency - data sovereignty and residency enforcement with 13 database providers (Fixes #405)`
