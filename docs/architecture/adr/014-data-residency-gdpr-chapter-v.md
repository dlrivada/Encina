# ADR-014: Data Residency — GDPR Chapter V (International Transfers)

## Status

**Accepted** — March 2026

## Context

GDPR Chapter V (Articles 44-49) governs international transfers of personal data. Organizations that process personal data across multiple geographic regions must ensure that every transfer complies with the Chapter V hierarchy: adequacy decisions, appropriate safeguards, or specific derogations.

### Problem Statement

| Challenge | Description |
|-----------|-------------|
| **Cross-border transfers** | Personal data may only be transferred to third countries or international organisations if the conditions in Chapter V are met. Without framework-level enforcement, compliance depends entirely on manual review. |
| **Legal basis tracking** | Each international transfer requires an identifiable legal basis (adequacy decision, SCCs, BCRs, or derogation). The application must record which basis was used for audit purposes. |
| **Enforcement consistency** | Data residency rules must apply uniformly regardless of transport — HTTP, message queue, gRPC, or serverless triggers. Database or network-level restrictions alone leave gaps for application-initiated transfers. |
| **Auditability** | Supervisory authorities (Art. 51-59) may request evidence that transfers comply with Chapter V. The system must produce an auditable trail of residency decisions. |
| **Region metadata** | Determining whether a region is within the EU, the EEA, or has an adequacy decision requires accurate, up-to-date metadata for dozens of jurisdictions. |

### Real-World Example

A healthcare platform processes patient records across EU and non-EU regions:

```csharp
// This command MUST be restricted to EU/EEA regions
[DataResidency("DE", "FR", "NL", DataCategory = "healthcare-data")]
public record CreatePatientCommand(string PatientId) : ICommand<PatientId>;

// This command MUST NOT leave the current region at all
[NoCrossBorderTransfer(DataCategory = "classified-data")]
public record ProcessClassifiedCommand(string DocumentId) : ICommand;
```

Without framework enforcement, the developer must manually verify region constraints in every handler. This is error-prone, inconsistent, and unauditable.

### Design Constraints

1. **Pipeline-level enforcement** — Must intercept all requests at the CQRS pipeline, not at the database or network layer
2. **Provider coherence** — Same interfaces across all 13 database providers (ADO.NET x 4, Dapper x 4, EF Core x 4, MongoDB x 1)
3. **Opt-in** — No overhead when data residency is not configured (pay-for-what-you-use)
4. **GDPR-accurate hierarchy** — Must follow the Chapter V preference order exactly
5. **ROP compliance** — All operations return `Either<EncinaError, T>`
6. **Zero per-request reflection** — Attribute lookups must be cached statically

## Decision

### Architecture: Pipeline-Level Residency Enforcement

We chose to enforce data residency at the CQRS pipeline level via `DataResidencyPipelineBehavior<TRequest, TResponse>` rather than at the database or network level. The pipeline behavior intercepts requests before handler execution, validates residency constraints, and optionally records audit trails and data locations after successful processing.

### Key Design Decisions

#### 1. Pipeline-Level Enforcement (Not Database/Network)

We enforce residency in the CQRS pipeline rather than via database-level restrictions (row-level security, connection routing) or network-level controls (firewalls, VPNs).

**Rationale**:

- Applies uniformly regardless of transport (HTTP, message queue, gRPC, serverless triggers)
- Works with all 13 database providers without provider-specific configuration
- Allows enforcement mode switching (Block, Warn, Disabled) without infrastructure changes
- Enables audit trail recording at the application level where request context is available

**Trade-off**: Pipeline enforcement assumes all data access goes through the CQRS pipeline. Direct database queries or raw SQL bypass it. This is acceptable for Encina's architecture where the pipeline is the primary entry point.

#### 2. Five-Step GDPR Transfer Hierarchy

The `DefaultCrossBorderTransferValidator` implements the GDPR Chapter V preference order:

| Step | Check | GDPR Article | Result |
|------|-------|--------------|--------|
| 1 | Same region | N/A | Allowed (no cross-border transfer) |
| 2 | Both within EEA | Art. 1(3) | Allowed (free movement under GDPR single market) |
| 3 | Adequacy decision | Art. 45 | Allowed (no additional safeguards) |
| 4 | Appropriate safeguards (SCCs/BCRs) | Art. 46 | Allowed with safeguards noted |
| 5 | No valid mechanism | Art. 44 | Denied |

**Rationale**:

- Mirrors the GDPR hierarchy exactly, making compliance audits straightforward
- Each step short-circuits — if an earlier basis applies, later checks are skipped
- The `TransferValidationResult` captures the legal basis, required safeguards, and any warnings for each decision
- Step 4 uses `DataProtectionLevel <= Medium` as a simplified SCC eligibility check

**Alternative considered**: Flat allow/deny list without hierarchy. Rejected because it does not capture the nuance of GDPR Chapter V (e.g., SCCs require supplementary measures that should be surfaced in the result).

#### 3. Region as a Sealed Record with Value Equality

`Region` is a `sealed record` with case-insensitive `Code` equality:

```csharp
public sealed record Region : IEquatable<Region>
{
    public required string Code { get; init; }
    public required string Country { get; init; }
    public required bool IsEU { get; init; }
    public required bool IsEEA { get; init; }
    public required bool HasAdequacyDecision { get; init; }
    public required DataProtectionLevel ProtectionLevel { get; init; }

    public bool Equals(Region? other) =>
        string.Equals(Code, other?.Code, StringComparison.OrdinalIgnoreCase);

    public override int GetHashCode() =>
        StringComparer.OrdinalIgnoreCase.GetHashCode(Code);
}
```

**Rationale**:

- Case-insensitive code equality enables safe use in dictionaries and sets (`"DE"` equals `"de"`)
- `sealed` prevents inheritance — region identity is based solely on `Code`
- Custom `Equals`/`GetHashCode` override the record's default structural equality to use only `Code`
- `RegionRegistry` provides 50+ pre-defined regions with accurate EU/EEA/adequacy metadata

**Alternative considered**: Using a simple `string` for region codes. Rejected because it loses the structured metadata (IsEU, IsEEA, HasAdequacyDecision, ProtectionLevel) that the transfer validator needs.

#### 4. Static Per-Type Attribute Caching in Pipeline Behavior

The pipeline behavior uses `static readonly` fields (not a `static ConcurrentDictionary`) to cache attribute lookups:

```csharp
public sealed class DataResidencyPipelineBehavior<TRequest, TResponse>
{
    private static readonly DataResidencyAttributeInfo? CachedResidencyInfo = ResolveResidencyAttribute();
    private static readonly NoCrossBorderTransferInfo? CachedNoCrossInfo = ResolveNoCrossAttribute();
    private static readonly PropertyInfo? CachedEntityIdProperty = ResolveEntityIdProperty();
}
```

**Rationale**:

- Each closed generic type (e.g., `DataResidencyPipelineBehavior<CreatePatientCommand, PatientId>`) gets its own static fields via the CLR's generic type specialization
- The CLR guarantees one-time initialization per closed generic type — no locking needed
- Zero reflection overhead after the first resolution for any given request/response pair
- No `ConcurrentDictionary` lookup cost — direct field access

#### 5. DataProtectionLevel Enum Ordering (High=0, Low=2, Unknown=3)

```csharp
public enum DataProtectionLevel
{
    High = 0,
    Medium = 1,
    Low = 2,
    Unknown = 3
}
```

**Rationale**:

- Lower numeric values represent better protection, enabling intuitive comparisons: `ProtectionLevel <= Medium` means "at least Medium protection"
- The `DefaultCrossBorderTransferValidator` uses `destination.ProtectionLevel <= DataProtectionLevel.Medium` to determine SCC eligibility
- `Unknown` (3) is treated as highest risk — any comparison `<= Medium` excludes Unknown regions, requiring explicit evaluation

**Alternative considered**: `High=3, Low=0` (higher = better). Rejected because the comparison operators become counterintuitive (`ProtectionLevel >= Medium` reads awkwardly for a risk assessment).

#### 6. Fluent Policy Builder with Builder Pattern

The `ResidencyPolicyBuilder` provides method chaining for configuring policies:

```csharp
options.AddPolicy("healthcare-data", policy => policy
    .AllowEU()
    .AllowEEA()
    .AllowRegions(RegionRegistry.Switzerland)
    .RequireAdequacyDecision()
    .AllowTransferBasis(TransferLegalBasis.StandardContractualClauses));
```

**Rationale**:

- Fluent API makes policy configuration readable and self-documenting
- `AllowEU()`, `AllowEEA()`, and `AllowAdequate()` are convenience methods that expand to the full set of regions from `RegionRegistry`
- Policies are stored via `IResidencyPolicyStore` for runtime resolution, enabling dynamic policy changes without redeployment
- `RequireAdequacyDecision()` and `AllowTransferBasis()` capture the GDPR transfer mechanisms explicitly

#### 7. Separation of Concerns: Four Abstractions

| Abstraction | Responsibility | GDPR Mapping |
|-------------|----------------|--------------|
| `IDataResidencyPolicy` | Checks if a region is allowed for a data category | Policy enforcement |
| `ICrossBorderTransferValidator` | Evaluates cross-border transfers per Chapter V hierarchy | Art. 44-49 |
| `IRegionRouter` | Determines the appropriate region for a request | Geographic routing |
| `IRegionContextProvider` | Resolves the current region from ambient context | Source region detection |

**Rationale**:

- Each abstraction has a single responsibility — policy evaluation is separate from transfer validation, which is separate from routing
- `IRegionContextProvider` supports multiple resolution strategies (static config, HTTP headers, cloud metadata, geo-IP) without coupling to the pipeline
- `IRegionRouter` enables automatic geographic routing based on policy, distinct from the enforcement check in the pipeline
- All four interfaces return `Either<EncinaError, T>` for ROP compliance

#### 8. 13-Provider Database Support

Three stores support data residency persistence:

| Store | Purpose | Tables |
|-------|---------|--------|
| `IResidencyPolicyStore` | Policy CRUD | `residency_policies` |
| `IDataLocationStore` | Data location tracking | `data_locations` |
| `IResidencyAuditStore` | Audit trail | `residency_audit_entries` |

All three stores have implementations for all 13 database providers:

| Category | Providers |
|----------|-----------|
| ADO.NET | Sqlite, SqlServer, PostgreSQL, MySQL |
| Dapper | Sqlite, SqlServer, PostgreSQL, MySQL |
| EF Core | Sqlite, SqlServer, PostgreSQL, MySQL |
| MongoDB | MongoDB |

Registered via the standard `config.UseDataResidency = true` option.

#### 9. Three-Mode Enforcement

The `DataResidencyEnforcementMode` enum controls behavior:

| Mode | Behavior |
|------|----------|
| `Block` | Returns `Left<EncinaError>` if residency check fails — request is rejected |
| `Warn` | Logs a warning but allows the request through — for gradual rollout |
| `Disabled` | Skips all residency checks entirely — zero overhead |

**Rationale**: Enables gradual adoption. Organizations can start with `Warn` to identify violations without disrupting production, then switch to `Block` once policies are validated.

## Alternatives Considered

### 1. Database-Level Enforcement (Row-Level Security)

Enforce residency via database-level row-level security or connection routing.

| Aspect | Evaluation |
|--------|------------|
| Pros | Enforced at the lowest level, cannot be bypassed by application code |
| Cons | Provider-specific (SQL Server RLS differs from PostgreSQL), does not work for MongoDB, no audit trail at application level, complex to configure per data category |
| Verdict | **Rejected** — not portable across 13 providers, loses application-level context |

### 2. Network-Level Enforcement

Use network policies, VPNs, or cloud provider region restrictions.

| Aspect | Evaluation |
|--------|------------|
| Pros | Infrastructure-level, transparent to application |
| Cons | Coarse-grained (per-service, not per-data-category), no legal basis tracking, no audit trail, requires infrastructure team coordination |
| Verdict | **Rejected as primary mechanism** — complementary but insufficient for GDPR Chapter V compliance |

### 3. Middleware-Level Enforcement (ASP.NET Core Only)

Enforce residency via ASP.NET Core middleware instead of CQRS pipeline.

| Aspect | Evaluation |
|--------|------------|
| Pros | Simple implementation, early rejection before routing |
| Cons | Only applies to HTTP requests — misses message queue, gRPC, and serverless triggers; couples residency to ASP.NET Core |
| Verdict | **Rejected** — pipeline-level enforcement is transport-agnostic |

## Consequences

### Positive

1. **Declarative enforcement**: `[DataResidency]` and `[NoCrossBorderTransfer]` attributes make residency constraints discoverable and self-documenting
2. **Transport-agnostic**: Pipeline-level enforcement applies uniformly to HTTP, message queue, gRPC, and serverless triggers
3. **GDPR-accurate hierarchy**: The five-step validation mirrors Chapter V exactly, simplifying compliance audits
4. **Zero per-request reflection**: Static per-type caching eliminates attribute resolution overhead after first use
5. **Auditable**: Optional audit trail records every residency decision with legal basis, outcome, and timestamp
6. **Fluent configuration**: `ResidencyPolicyBuilder` makes policy definition readable and maintainable
7. **Gradual adoption**: Block/Warn/Disabled enforcement modes enable incremental rollout
8. **13-provider support**: Full provider coherence across ADO.NET, Dapper, EF Core, and MongoDB

### Negative

1. **Pipeline dependency**: Enforcement assumes all data access goes through the CQRS pipeline — direct database queries bypass it
2. **Hardcoded region metadata**: `RegionRegistry` embeds EU/EEA membership and adequacy decisions — requires code changes when the European Commission issues new adequacy decisions or revokes existing ones
3. **Simplified SCC check**: The `ProtectionLevel <= Medium` heuristic for SCC eligibility is a simplification — complex scenarios (e.g., Schrems II supplementary measures) may need custom `ICrossBorderTransferValidator` implementations
4. **No Art. 49 derogations**: The current implementation does not evaluate specific derogations (explicit consent, public interest, vital interests) — step 5 is a blanket denial

### Risks

| Risk | Mitigation |
|------|------------|
| EU adequacy decisions change (e.g., Privacy Shield invalidation in Schrems II) | `RegionRegistry` must be updated; `IAdequacyDecisionProvider` can be replaced with a runtime-configurable implementation |
| Direct database queries bypass pipeline enforcement | Document this limitation; recommend using the CQRS pipeline for all data operations |
| Art. 49 derogations needed in future | Extend `DefaultCrossBorderTransferValidator` with a step 5 derogation check; the interface already supports this via `TransferValidationResult` |
| Region context resolution fails in serverless environments | `IRegionContextProvider` supports multiple strategies; provide cloud-specific implementations (Azure, AWS, GCP) |

## Related

- [GitHub Issue #405](https://github.com/dlrivada/Encina/issues/405) — Data Residency / Data Sovereignty Enforcement
- GDPR Chapter V — Articles 44-49 (International Transfers)
- EDPB Recommendations 01/2020 — Supplementary measures for international transfers
- Schrems II (Case C-311/18) — Invalidation of EU-US Privacy Shield, impact on SCCs
- `Encina.Compliance.DataSubjectRights` — DSR integration (data portability requires residency awareness)
- `Encina.Compliance.Retention` — Retention integration (retention policies may differ by region)
- `Encina.Compliance.Consent` — Consent as a transfer legal basis (Art. 49(1)(a) derogation)
- `Encina.Compliance.Anonymization` — Anonymized data is outside GDPR scope and exempt from Chapter V
