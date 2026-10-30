# Implementation Plan: Compliance Applicability Resolver — Conditional Enforcement by Jurisdiction, Tenant and Subject

> **Issue**: [#686](https://github.com/dlrivada/Encina/issues/686)
> **Type**: Feature
> **Complexity**: High (12 phases, 18 packages touched, ~120 source and test files)
> **Estimated Scope**: ~2,600-3,200 lines of production code + ~3,000-3,700 lines of tests
> **Milestone**: v0.17.0 — Compliance Lifecycle
> **Parent EPIC**: [#668](https://github.com/dlrivada/Encina/issues/668) — v0.13.0 Security & Compliance (the issue's parent epic)
> **Specification**: [SPEC-002](../specifications/SPEC-002-eu-regulatory-readiness.md) REQ-011 (line 268), AC-011 (line 462), REQ-019 (line 295), INV-003 and INV-005 (lines 512, 514), DEC-006 (line 547), REQ-061 and REQ-062 (lines 384-395); [SPEC-000](../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) DEC-009 addendum of 2026-10-23 (line 281)
> **Order in the milestone** (maintainer, [comment of 2026-10-23](https://github.com/dlrivada/Encina/issues/686)): **#686 → [#687](https://github.com/dlrivada/Encina/issues/687) → S15 rules [#2700](https://github.com/dlrivada/Encina/issues/2700) → [#688](https://github.com/dlrivada/Encina/issues/688)**. This plan is the first of the four and fixes the contract the other three implement or consume.
> **Related**: [#345](https://github.com/dlrivada/Encina/issues/345) (feature flags, complementary), [#1705](https://github.com/dlrivada/Encina/issues/1705) (one request identity), [#1193](https://github.com/dlrivada/Encina/issues/1193) (evidential read audit), [#798](https://github.com/dlrivada/Encina/issues/798) (tenant filtering in Security.Audit), [#2184](https://github.com/dlrivada/Encina/issues/2184) (pipeline execution order), [#751](https://github.com/dlrivada/Encina/issues/751) (ABAC decision audit, the precedent for decision recording)

---

## Summary

Every compliance module of Encina enforces statically today: a request type that carries `[RequireConsent]`, `[DataResidency]` or `[RestrictProcessing]` is checked for every caller, every tenant and every data subject. A SaaS application that serves a Spanish clinic and a Brazilian retailer from one deployment cannot say "GDPR and LOPDGDD apply to the first, not to the second", and a subject outside the territorial scope of GDPR is checked as if he were inside it. This plan adds one decision point, `IComplianceApplicabilityResolver`, that answers "which regulations and which compliance capabilities apply to this request, tenant and subject?", and makes every compliance module consult it before it enforces. The default answer is **everything applies**, so an application that does not opt in behaves exactly as today, and an unknown or missing context never switches a check off (SPEC-002 DEC-006, INV-005).

The contract defined here is the one #687 (fluent DSL, `RuleBasedComplianceResolver`), #2700 (the S15 rules of SPEC-002 REQ-011, "built on the Compliance Applicability Resolver (#686) and the rule-based compliance DSL (#687)", SPEC-002 line 268) and #688 (`OpaComplianceResolver`) implement or consume. OPA runs locally as a Docker image, so #688 is testable without a cloud account (maintainer comment of 2026-10-23); nothing in this plan depends on it.

**Principle (maintainer, 2026-10-30).** Compliance with EU and Spanish law is never optional or commercial. The only valid reason not to apply a control is that the law does not apply (territorial scope: the tenant and the subject are outside the EU), and even then only when the skip is justified, traced and audited and opens no gap that lacks an alternative; otherwise the skip is not allowed, whatever the user wants. A commercial plan or an operator preference never decides compliance. Encina is designed for Spain and the EU; use outside that frame is not foreseen and is at the application's own risk, and the documentation says so. Non-EU regulation identifiers are identifiers only: Encina claims nothing for them (SPEC-002 INV-002).

**State of the code (verified on `0bc84c32`).** Nothing of the feature exists: a search of `src/` for `ComplianceProfile`, `ComplianceContext`, `Applicability`, `IComplianceApplicabilityResolver`, `AddEncinaCompliance(` and `ComplianceEnforcementMode` finds nothing. The enforcement points it will gate do exist:

| Module | Enforcement point (pipeline behavior unless stated) | Mode enum and default | Context it reads today |
|---|---|---|---|
| GDPR | `src/Encina.Compliance.GDPR/GDPRCompliancePipelineBehavior.cs:60` (`Handle` :90, attribute lookup :102-113, Left :141/:155/:172/:186) | `GDPREnforcementMode` (`Enforce`, `WarnOnly`; no `Disabled`), `GDPROptions.cs:89`, `Enforce` | None (context passed to the validator only, :164) |
| Consent | `src/Encina.Compliance.Consent/ConsentRequiredPipelineBehavior.cs:54` (`Handle` :85, `Disabled` skip :99-103, missing subject → Left in every mode :123-127) | `ConsentEnforcementMode`, `ConsentOptions.cs:52`, `Block` | Subject from `SubjectIdProperty` or `context.UserId` (:224) |
| DataSubjectRights | `src/Encina.Compliance.DataSubjectRights/ProcessingRestrictionPipelineBehavior.cs:87` (`Handle` :127, tenant missing → Left :203-210, subject missing → Left :235-244) | `DSREnforcementMode`, `DataSubjectRightsOptions.RestrictionEnforcementMode` :51, `Block` | `IDataSubjectIdExtractor` (:111) |
| LawfulBasis | `src/Encina.Compliance.LawfulBasis/Pipeline/LawfulBasisValidationPipelineBehavior.cs:51` (`Handle` :97, Left :163/:217) | `LawfulBasisEnforcementMode`, `LawfulBasisOptions.cs:47`, `Block` | `ILawfulBasisSubjectIdExtractor` (:79) |
| DataResidency | `src/Encina.Compliance.DataResidency/DataResidencyPipelineBehavior.cs:58` (`Handle` :144, `ShouldSkipEnforcement` :177-189, region failure → Left in Block :248-250) | `DataResidencyEnforcementMode`, `DataResidencyOptions.cs:73`, `Warn` | `IRegionContextProvider` (:112) |
| CrossBorderTransfer | `src/Encina.Compliance.CrossBorderTransfer/Pipeline/TransferBlockingPipelineBehavior.cs:66` (`Handle` :112, unresolved destination → Left in every mode :150-156) | `CrossBorderTransferEnforcementMode`, `CrossBorderTransferOptions.cs:26`, `Block` | `context.TenantId` (:174), `IModuleExecutionContext` (:108) |
| Retention | `src/Encina.Compliance.Retention/RetentionValidationPipelineBehavior.cs:71` (after the handler, :153); `RetentionEnforcementService : BackgroundService` (`RetentionEnforcementService.cs:61`, cycle :126) | `RetentionEnforcementMode`, `RetentionOptions.cs:140`, `Warn` | `context.TenantId`, module name (:298-299) |
| Anonymization | `src/Encina.Compliance.Anonymization/AnonymizationPipelineBehavior.cs:71` (after the handler, :149) | `AnonymizationEnforcementMode`, `AnonymizationOptions.cs:50`, `Block` | None |
| BreachNotification | `src/Encina.Compliance.BreachNotification/BreachDetectionPipelineBehavior.cs:61` (after the handler, :133) | `BreachDetectionEnforcementMode`, `BreachNotificationOptions.cs:56`, `Warn` | `SecurityEventFactory.Create(request, context)` (:137) |
| DPIA | `src/Encina.Compliance.DPIA/DPIARequiredPipelineBehavior.cs:68` (`Handle` :103) | `DPIAEnforcementMode`, `DPIAOptions.cs:42`, `Warn` | `context.TenantId` as a trace tag (:195-197) |
| ProcessorAgreements | `src/Encina.Compliance.ProcessorAgreements/ProcessorValidationPipelineBehavior.cs:74` (`Handle` :104) | `ProcessorAgreementEnforcementMode`, `ProcessorAgreementOptions.cs:40`, `Warn` | Trace tag only (:197-199) |
| PrivacyByDesign | `src/Encina.Compliance.PrivacyByDesign/DataMinimizationPipelineBehavior.cs:92` (`ShouldSkipEnforcement` :185-210) | `PrivacyByDesignEnforcementMode` (`Disabled=0`, `Warn=1`, `Block=2`), `PrivacyByDesignOptions.cs:67`, `Warn` | `context.TenantId` (:163, :169) |
| NIS2 | `src/Encina.Compliance.NIS2/NIS2CompliancePipelineBehavior.cs:56` (`Handle` :95) | `NIS2EnforcementMode`, `NIS2Options.cs:64`, `Warn` | `context.UserId`, `context.TenantId` (:405-406) |
| AIAct | `src/Encina.Compliance.AIAct/AIActCompliancePipelineBehavior.cs:58` (`Handle` :88; prohibited practice blocks in Warn too, :188-193) | `AIActEnforcementMode`, `AIActOptions.cs:40`, `Block` | None |
| Attestation | `src/Encina.Compliance.Attestation/Behaviors/AttestationPipelineBehavior.cs:49` (after the handler) | Per attribute: `AttestationFailureMode` (`AttestDecisionAttribute.cs:27`) | `context.CorrelationId` (:145) |
| Crypto-shredding | `src/Encina.Marten.GDPR/Serialization/CryptoShredderSerializer.cs:52` (synchronous `ToJson` :123, `WriteTo` :136), decision per `[CryptoShredded]` property in `CryptoShreddingContractModifier.cs:89-95`, subject from the document in `CryptoShreddingEngine.cs:106-111` | None (always encrypts) | Fresh DI scope, no request scope (`CryptoShreddingCallScope.cs:145`) |
| Read auditing | `src/Encina.Security.Audit/AuditedReadOnlyRepository.cs:37` (`ShouldAudit` :211) | `ReadAuditOptions.Enabled` (`ReadAuditOptions.cs:57`) plus per-type `AuditReadsFor<T>()` (:158) | `Identity.Kind` (:214) |

The building blocks the resolver reads also exist:

| Building block | Where | State |
|---|---|---|
| Request context | `src/Encina/Abstractions/IRequestContext.cs:54` (`Identity` :94, `TenantId` :125, `Metadata` :152); `UserId` is a C# 14 extension property (`src/Encina/Identity/RequestContextIdentityExtensions.cs:25`); `GetModuleName()` (`src/Encina/Modules/RequestContextModuleExtensions.cs:37`) | Merged (#1705 phases 1-3) |
| Request identity | `src/Encina/Identity/RequestIdentity.cs:47` (`Kind` :115, `UserId` :121, `Principal` :149, `HasClaim` :184); no region or jurisdiction claim | Merged |
| Tenant model | `src/Encina.Tenancy/Abstractions/TenantInfo.cs:56-61`: positional `sealed record TenantInfo(TenantId, Name, Strategy, ConnectionString, SchemaName)`; no country or jurisdiction | Shipped |
| Tenant store | `ITenantStore` (`src/Encina.Tenancy/Abstractions/ITenantStore.cs:55`, `GetTenantAsync` :68); the only implementation is `InMemoryTenantStore` (`src/Encina.Tenancy/Stores/InMemoryTenantStore.cs:43`); no database-backed store exists (`: ITenantStore` matches one class) | Shipped |
| Current tenant | `ITenantProvider` (`src/Encina.Tenancy/Abstractions/ITenantProvider.cs:57`, `GetCurrentTenantAsync` :104), `DefaultTenantProvider` (`src/Encina.Tenancy/Core/DefaultTenantProvider.cs:20`); `TenancyOptions.RequireTenant` default `true` (`TenancyOptions.cs:55`); `MultiTenancyMarker` in core (`src/Encina/Abstractions/MultiTenancyMarker.cs:25`) | Shipped |
| Region | `IRegionContextProvider.GetCurrentRegionAsync` (`src/Encina.Compliance.DataResidency/Abstractions/IRegionContextProvider.cs:62-63`); `Region` record with `Code`, `Country`, `IsEU`, `IsEEA` (`Model/Region.cs:39-77`); `RegionRegistry` with EU-27 and EEA lists (`Model/RegionRegistry.cs:55-146`); no region-to-regulation mapping | Shipped |
| Data categories | `PersonalDataCategory` in DataSubjectRights (`src/Encina.Compliance.DataSubjectRights/Model/PersonalDataCategory.cs:17`, `Health` :38) | Shipped |
| Policy evaluation (ABAC) | `IPolicyDecisionPoint.EvaluateAsync` (`src/Encina.Security.ABAC/Abstractions/IPolicyDecisionPoint.cs:58-60`), `XACMLPolicyDecisionPoint` (`Evaluation/XACMLPolicyDecisionPoint.cs:43`); callable outside the pipeline through `AttributeContextBuilder.Build` (`AttributeContextBuilder.cs:53`), but `PolicyEvaluationContext.RequestType` is required (`Model/PolicyEvaluationContext.cs:94`) and EEL conditions compile with Roslyn on first use (`EEL/EELCompiler.cs:105`) | Shipped |
| Decision audit precedent | `AuditStoreABACDecisionRecorder` writes an `OperationAuditEntry` per decision (`src/Encina.Security.ABAC/DecisionAudit/AuditStoreABACDecisionRecorder.cs:54`, scope at :103) | Shipped (#751 phase) |
| Operation audit | `IOperationAuditStore.RecordAsync` (`src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs:50`), `OperationAuditEntry` (`OperationAuditEntry.cs:50`, `Metadata` :252) on the 10 providers, Marten and InMemory | Shipped |
| Policy engines | No OPA, Rego or external policy engine in `src/` (the only hits are doc comments, `src/Encina.Security/Abstractions/IPermissionEvaluator.cs:14`) | #688 adds OPA |

**What this plan delivers**

1. A core contract in `src/Encina/Compliance/`: `IComplianceApplicabilityResolver`, `ComplianceProfile`, `ComplianceContext`, `ComplianceCapability`, `ComplianceRegulation`, `ComplianceOperation`, `IComplianceContextContributor`, `IComplianceApplicability` (the question every module asks), `IComplianceDecisionRecorder`, and a zero-cost default that answers "applies" without resolving anything.
2. A new package `Encina.Compliance.Applicability` (references only `Encina`): `AddEncinaComplianceApplicability`, the four built-in resolvers of the issue (`AlwaysEnforceComplianceResolver` in core, `RegionBasedComplianceResolver`, `TenantAwareComplianceResolver`, `CompositeComplianceResolver`), a claim contributor, the per-request profile memo, `ComplianceOrchestrationBehavior`, options with validation, observability and a health check.
3. Context contributors in the packages that own the data: `Encina.Tenancy` (tenant country and regulations), `Encina.Compliance.DataResidency` (current region), `Encina.Compliance.DataSubjectRights` (subject id); `TenantInfo` gains `Country` and `ApplicableRegulations`.
4. Every compliance gate in the table above consults `IComplianceApplicability` after it has found its attribute and before it enforces, except Attestation and crypto-shredding (the module list is Design Choice 8; crypto-shredding always encrypts, Design Choice 16).
5. Every skip is logged, counted and recorded as an `OperationAuditEntry` through `Encina.Security.Audit`; a skip that cannot be recorded does not happen (Design Choice 15).

**Deviations from the issue text**, each explained in the Design Choice named: the profile is a capability set and a regulation set instead of typed booleans, and `FullEnforcement()` is `Full()` (3); no `ComplianceEnforcementMode` enum (9); `ComplianceOrchestrationBehavior` owns the decision and its telemetry while each module keeps its own behavior, instead of replacing the module behaviors (7); the DSR and breach fulfilment services are not gated and the issue task "Update DSR handler to check `DataSubjectRightsApply`" is superseded (8); no per-capability modes (9) and no `FirstDecisive` merge (11); crypto-shredding is not gated (16); `TenantInfo.Jurisdiction` is folded into `ApplicableRegulations` and `SubscriptionTier` is not added, because a commercial tier never decides compliance (12); registration is `AddEncinaComplianceApplicability` and coverage is per flag instead of a single 85% (18); the span attribute `subject.id` is dropped (REQ-062 forbids direct subject identifiers); the log messages are named `ProfileResolved`, `CapabilitySkipped` and `ResolutionFailed`; Resilience is N/A instead of Deferred because the built-in resolvers call nothing external (matrix row 6). The capability names (`ProcessingRestriction`, `Dpia`) differ from the sketches in #687 and #688; Next Steps updates those sketches.

**Standards**: GDPR Art. 2, 3 (territorial scope), 5(2) (accountability), 24, 25 and 32; LOPDGDD (Ley Orgánica 3/2018) art. 1-2; SPEC-002 REQ-011, REQ-019, REQ-061, REQ-062, DEC-006, INV-003, INV-005; ISO 3166-1 alpha-2 for country codes. CCPA (Cal. Civ. Code §1798.100 et seq.) and LGPD (Lei 13.709/2018) are configuration targets, not built-in mappings (Design Choice 14).

**Affected packages**: `Encina` (core contract), new `Encina.Compliance.Applicability`, `Encina.Tenancy`, the 14 gated compliance packages (`GDPR`, `Consent`, `DataSubjectRights`, `LawfulBasis`, `DataResidency`, `CrossBorderTransfer`, `Retention`, `Anonymization`, `BreachNotification`, `DPIA`, `ProcessorAgreements`, `PrivacyByDesign`, `NIS2`, `AIAct`), `Encina.Security.Audit`. `Encina.Marten.GDPR` is not changed: `[CryptoShredded]` data is always encrypted (Design Choice 16). `Encina.AspNetCore` needs no change: the "HTTP-aware context builder" of the issue is the claim contributor of Phase 3, which reads `IRequestContext.Identity` and works for every host. `Encina.Compliance.Attestation` is never gated (Design Choice 8).

**Provider category**: none. The resolver, its contributors and the profile memo are in-memory and stateless; no database, caching, transport or lock code changes (Design Choice 13). The skip records go to the existing `IOperationAuditStore`, already on the 10 database providers.

---

## Design Choices

Twenty choices. Five were Dilemmas and the maintainer decided them on 2026-10-30 (3, 8, 9, 11, 16; see Maintainer Decisions); fifteen are Settled or Trivial with a citation, each verified by an independent `adversarial-reviewer` screen of 2026-10-29. The rejected options stay in the tables as history; no later phase depends on them.

<details>
<summary><strong>1. Package Placement — core contract, new <code>Encina.Compliance.Applicability</code> package, contributors in the packages that own the data</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Core contract in `src/Encina/Compliance/` (interfaces, records, enums, zero-cost default); a new `Encina.Compliance.Applicability` package that references only `Encina` (resolvers, options, DI, orchestration behavior, observability); `IComplianceContextContributor` implementations in `Encina.Tenancy`, `Encina.Compliance.DataResidency` and `Encina.Compliance.DataSubjectRights`** | The 14 compliance packages and `Encina.Security.Audit` consult the contract without a new project reference (GDPR, AIAct, Anonymization and PrivacyByDesign reference only `Encina`); the resolver package pulls no Marten, Messaging or Caching; each contributor sits next to the type it reads (`TenantInfo`, `Region`, `IDataSubjectIdExtractor`) | The feature lives in five places; the region-based resolver needs its own EU/EEA table because `RegionRegistry` is in DataResidency |
| **B) Everything in core `Encina`** | One place; no new package | Core grows options, a behavior, a meter, a health check and resolvers that most applications never use (against pay-for-what-you-use, AGENTS.md §1); core cannot read `TenantInfo` or `Region` anyway, so contributors still live elsewhere |
| **C) Contract and resolvers in a new package that references `Encina.Tenancy` and `Encina.Compliance.DataResidency`** | Resolvers read `TenantInfo` and `Region` directly; one package | Every compliance package must reference the new one; DataResidency references `Encina.Marten`, `Encina.Caching` and `Encina.DomainModeling` (`Encina.Compliance.DataResidency.csproj`), and Tenancy references `Encina.Messaging`, so an application that wants only the tenant-aware resolver pays for Marten |

### Chosen Option: **A — Core contract, resolver package on core only, contributors with their data**

### Rationale

- A is the only placement in which every enforcement point can ask the question without a new reference, and the resolver package stays as light as core. It follows the precedent of `IAccessElevationGate` in core with the implementation in `Encina.Security.BreakTheGlass` ([#1244 Design Choice 1](break-the-glass-implementation-plan-1244.md), maintainer decision of 2026-10-17).
- The built-in resolvers read primitive context fields (`TenantCountry`, `SubjectCountry`, `RegionCountry`, `RegionInEea`), and the packages that own `TenantInfo` and `Region` fill them through contributors registered by their own `AddEncina*` methods. Nothing is resolved unless `Encina.Compliance.Applicability` is registered.
- B puts options, a behavior, a meter and resolvers in core, against pay-for-what-you-use (AGENTS.md §1). C is impossible: the new package would reference `Encina.Compliance.DataResidency` (which references Marten, Caching and DomainModeling) while DataResidency itself must consult the contract, a project cycle.

**Class:** Settled
**Settled by:** Same elimination as the maintainer's #1244 Design Choice 1 (`break-the-glass-implementation-plan-1244.md`, Maintainer Decisions item 1, 2026-10-17; contract in core `IAccessElevationGate`, `src/Encina/Authorization/IAccessElevationGate.cs:27`); C is a project cycle through the DataResidency csproj; B violates AGENTS.md §1 (pay-for-what-you-use); `src/Encina.Compliance.{GDPR,AIAct,Anonymization,PrivacyByDesign}/*.csproj` reference only `Encina`
**Verified:** adversarial-reviewer 2026-10-29

</details>

<details>
<summary><strong>2. The Resolver Contract — asynchronous, Railway-oriented, over a context of core types</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ValueTask<Either<EncinaError, ComplianceProfile>> ResolveAsync(ComplianceContext context, CancellationToken cancellationToken)`; `ComplianceContext` holds only core types (strings, `Type`, `IRequestContext`, core enums)** | The exact signature #687 and #688 already build on; an OPA or database resolver can await I/O and report failure without exceptions | Contributors must flatten `TenantInfo` and `Region` into strings |
| **B) Synchronous `ComplianceProfile Resolve(ComplianceContext)`** | Cheaper for in-memory resolvers; fits the synchronous Marten serializer | #688 calls OPA over HTTP; a synchronous contract forces sync-over-async in every remote resolver |
| **C) Context typed with `TenantInfo`, `Region` and `PersonalDataCategory` as in the issue sketch** | Rich typed context | Core cannot reference Tenancy, DataResidency or DataSubjectRights; the contract would have to leave core (contradicts Design Choice 1 and the package graph) |

### Chosen Option: **A — Asynchronous ROP contract over core types**

### Rationale

- ROP is the project rule for every operation that can fail (AGENTS.md §3, ADR-001, ADR-006), and the dependent issues fix the signature: #687's "Current state" shows `ValueTask<Either<EncinaError, ComplianceProfile>> ResolveAsync(ComplianceContext context, CancellationToken ct)` and #688 is "simply another `IComplianceApplicabilityResolver`".
- The context carries `TenantId`, `TenantCountry`, `TenantRegulations`, `SubjectId`, `SubjectCountry` (the data subject's country, from an explicit source only), `CallerCountry` (the authenticated caller's country claim, never used for the GDPR Art. 3(2) test), `RegionCountry` and `RegionInEea` (from `Region.Country` and `Region.IsEEA`), `ModuleName`, `RequestType`, `Operation`, `DataCategories` (strings, so `PersonalDataCategory` names and application categories both fit), `RequestContext` and `Metadata`.
- Option C is impossible once Design Choice 1 is A: core cannot reference Tenancy, DataResidency or DSR. The #687 and #688 sketches use typed members (`ctx.Region`, `ctx.Tenant`); Next Steps updates them.

**Class:** Settled
**Settled by:** AGENTS.md §3 (Railway Oriented Programming), ADR-001, ADR-006; the signature consumed by #687 and #688 (issue bodies)
**Verified:** adversarial-reviewer 2026-10-29

</details>

<details>
<summary><strong>3. Profile Shape — a set of capabilities and a set of regulation identifiers</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The issue's typed booleans: `GDPRApplies`, `CCPAApplies`, `LGPDApplies`, `ConsentRequired`, ... `DPIARequired`** | Reads well at the call site; matches the issue text | Adding a regulation (LOPDGDD, NIS2, AI Act) or a capability changes the public record; CCPA and LGPD become first-class types although Encina ships no CCPA or LGPD logic; a DSL (#687) and OPA (#688) must map to named properties one by one |
| **B) `IReadOnlySet<ComplianceCapability> Capabilities` (closed enum, one value per gated enforcement point) plus `IReadOnlySet<ComplianceRegulation> Regulations` (a `readonly record struct` over a normalised string id, with well-known values `Gdpr`, `Lopdgdd`, `Nis2`, `AiAct` only; non-EU ids are identifiers); `Applies(ComplianceCapability)`; factories `Full()` and `None()`** | A module asks one question (`Applies(Consent)`); regulations stay open for applications and OPA; the capability enum is exhaustive, so tests can assert every value is consulted somewhere | Regulations are strings at the boundary; a typo in configuration is caught only by validation |
| **C) Regulations only; each module derives its capability from the regulations** | Smallest profile | A module asks about a capability, not about a regulation; deriving it on every call hides the question |

### Chosen Option: **B — Capability set plus open regulation identifiers** (decided by the maintainer, 2026-10-30)

### Rationale

- The question a module asks is about a capability, and the list of regulations is open-ended; B keeps the record stable when #687 adds rules and #688 maps Rego output.
- Non-EU regulation ids (CCPA, LGPD, ...) are identifiers only; Encina claims nothing for them (SPEC-002 INV-002). An application may use such an id in its own configuration, but no built-in logic exists behind it.
- `ComplianceCapability` values (closed enum): `ProcessingActivity` (GDPR behavior), `Consent`, `LawfulBasis`, `ProcessingRestriction` (DSR behavior), `DataResidency`, `CrossBorderTransfer`, `Retention`, `Anonymization`, `BreachDetection`, `Dpia`, `ProcessorAgreements`, `DataMinimization`, `Nis2`, `AiAct`, `ReadAuditing` (15 values). `Attestation` and crypto-shredding have no capability: Attestation never consults the resolver (Design Choice 8) and `[CryptoShredded]` data is always encrypted (Design Choice 16).

**Class:** Dilemma (decided by the maintainer, 2026-10-30; see Maintainer Decisions)

</details>

<details>
<summary><strong>4. The Default — full enforcement when no resolver is configured</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Core registers an `IComplianceApplicability` that answers "applies" for every capability without resolving; `AddEncinaComplianceApplicability` defaults to `AlwaysEnforceComplianceResolver`** | An application that does not opt in behaves as today; zero allocations on the hot path | None for safety |
| **B) Default to "nothing applies" until a resolver is configured** | Cheapest | Silently disables every compliance check |

### Chosen Option: **A — Full enforcement by default**

### Rationale

- Security- and compliance-relevant behaviours fail closed unless an explicit, logged opt-out says otherwise (SPEC-002 INV-005, DEC-006 (a), REQ-019; AGENTS.md §3). B is a silent opt-out and does not comply.
- The issue says "backward-compatible"; pre-1.0 that is not a reason (AGENTS.md §1), but the fail-closed rule gives the same answer.

**Class:** Settled
**Settled by:** SPEC-002 INV-005, DEC-006, REQ-019; AGENTS.md §3 (compliance and security gates fail closed)
**Verified:** adversarial-reviewer 2026-10-29

</details>

<details>
<summary><strong>5. Missing Context — an unknown tenant, jurisdiction, country or subject never removes a capability</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Every built-in resolver treats a missing or unparseable field as "unknown", and unknown maps to the full profile; a contributor failure leaves its fields unknown; with tenancy registered (`MultiTenancyMarker`) and no tenant, the context is unknown** | A gap in the data can only add checks, never remove them; the modules' own fail-closed rules (missing subject in Consent :123-127, missing tenant in DSR :203-210) still run | A request without a country is checked fully, which can surprise an application that forgot a contributor |
| **B) Unknown maps to the application's configured "default jurisdiction" profile** | Fewer surprises for a single-country application | A missing tenant or claim silently applies a weaker profile |

### Chosen Option: **A — Unknown means full enforcement**

### Rationale

- "When context is missing (no `HttpContext`, tenant or principal) or a lookup fails, the gate denies; the only opt-out is explicit and logged" (AGENTS.md §3); SPEC-002 REQ-019 names subject resolution as a case that must fail closed. B is a silent weakening; a single-country application gets the same result explicitly with `RegionBasedResolverOptions.AssumedSubjectCountry`, which is logged at start-up as an opt-out.
- **When a resolver may narrow.** `RegionBasedComplianceResolver` is decisive only when the tenant country is known **and** the subject country or the region country is known (or `AssumedSubjectCountry` is set); otherwise it returns `Full`. `TenantAwareComplianceResolver` is decisive only when the tenant is known and declares regulations. The composite unions decisive answers, so a capability is removed only when every resolver that evaluated it agrees that the law does not apply; this is intended.
- **No "abstain" result.** A resolver that lacks its input returns `Full`; it never abstains. If a resolver could abstain, the composite would union only the others, and a tenant resolver would narrow a jurisdiction that nobody evaluated, which is the silent weakening AGENTS.md §3 forbids.
- The caller's own country claim is `CallerCountry`, never `SubjectCountry`: staff acting on a patient's record are not the data subject, so a claim-derived value would look "known" and drop GDPR for an EU subject.
- The health check (Phase 8) asserts the invariant: the configured resolver must return the full profile for an empty context.

**Class:** Settled
**Settled by:** AGENTS.md §3 (fail closed on missing context); SPEC-002 REQ-019, INV-005, DEC-006
**Verified:** adversarial-reviewer 2026-10-29

</details>

<details>
<summary><strong>6. Resolution Failure — what a <code>Left</code> or an exception from the resolver does</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Fall back to the full profile, log `ComplianceResolutionFailed` (Warning, error code only), count it; the request continues and every module enforces** | No capability is lost; an outage of a remote resolver (#688 OPA) does not take the application down; matches the issue's logging table ("falling back to full enforcement") | A misconfigured resolver can stay unnoticed if nobody watches the metric |
| **B) Deny the request with `compliance.applicability_failed`** | The failure is impossible to miss | A resolver outage blocks every request, including those whose checks would pass |

### Chosen Option: **A — Fall back to full enforcement**

### Rationale

- Both options keep every check active, so INV-005 does not choose; the issue text does: its logging table says `ComplianceResolutionFailed` "falling back to full enforcement", and #688 defaults `FallbackProfile` to the full profile. A also keeps availability (the modules still deny what they must deny). The health check reports `Degraded` on repeated failures.
- Background paths (the Retention service) also log and count a resolution failure and never report success (AGENTS.md §3).
- `IComplianceApplicability.AppliesAsync` returns `Either<EncinaError, bool>` so that a deny-on-failure policy stays possible without a contract change.

**Class:** Settled
**Settled by:** #686 issue body (Observability logging table: `ComplianceResolutionFailed` "falling back to full enforcement"); #688 issue body (`FallbackProfile = ComplianceProfile.FullEnforcement()` as default); maintainer precedent #1244 decision D6 of 2026-10-24 (a lookup failure keeps the stricter state after a log and a counter); SPEC-002 INV-005
**Verified:** adversarial-reviewer 2026-10-29

</details>

<details>
<summary><strong>7. Integration Mechanism — one per-request answer that every module asks for, plus a thin orchestration behavior</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Each module asks `IComplianceApplicability.AppliesAsync(request, context, capability, ct)` after it has found its attribute; the package's implementation resolves once per request instance (scoped memo, key in Design Choice 19) and records skips; `ComplianceOrchestrationBehavior` runs early, warms the memo, opens the `encina.compliance.orchestrate` span and tags the profile** | Works whatever the behavior order is (no dependency on #2184); requests without compliance attributes never resolve; modules stay independent packages | 14 behaviors change; each needs a skip test |
| **B) One `ComplianceOrchestrationBehavior` that replaces the module behaviors and calls each module's check** | One place; one span | The orchestrator must reference all 14 packages, so installing it installs every module (pay-for-what-you-use, AGENTS.md §1); module behaviors lose their own registration and options |
| **C) An early behavior resolves and stamps the profile on `IRequestContext.Metadata`; modules read the stamp** | No service call in modules | Depends on behavior order (#2184 open); metadata is application-writable, so a stamp could be forged |

### Chosen Option: **A — Per-request answer asked by each module, thin orchestration behavior**

### Rationale

- Each module keeps its attribute, options and enforcement mode, and no package has to reference all 14 modules. This is a documented **deviation** from the literal acceptance criterion "`ComplianceOrchestrationBehavior` coordinates ALL compliance modules" and its class doc "Replaces individual compliance behaviors": under A the behavior coordinates the *decision* (one resolution, one span, the skip records) but does not call the modules; the modules call the decision. The wording is superseded by AGENTS.md §1, and a comment on #686 and the ADR record it so that a review does not flag a missed acceptance criterion. The memo pattern is the one `BreakTheGlassRequestGrants` uses for one grant per request (#1244 Phase 2).
- B would make the orchestrator reference all 14 modules, so installing it installs every module. C depends on #2184, which is open; moving to C later is ordering, not a decision.

**Class:** Settled
**Settled by:** AGENTS.md §1 (pay-for-what-you-use: an orchestrator that calls 14 modules must reference them); in-repo pattern `src/Encina.Security.BreakTheGlass/Gate/BreakTheGlassRequestGrants.cs:13` and #1244 Design Choice 3 (each gate consults inside itself); #2184 is open
**Verified:** adversarial-reviewer 2026-10-29

</details>

<details>
<summary><strong>8. Which Enforcement Points Consult the Resolver</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The issue's list: GDPR, Consent, DSR (processing restriction), DataResidency, Retention (behavior and `RetentionEnforcementService`), Anonymization, BreachNotification (detection behavior), LawfulBasis, DPIA, crypto-shredding, read auditing** | Exactly the acceptance criteria | CrossBorderTransfer (GDPR Chapter V) still blocks a transfer for a subject outside GDPR; NIS2 and AI Act cannot be scoped although they are regulations with their own territorial scope |
| **B) A plus CrossBorderTransfer, ProcessorAgreements, PrivacyByDesign, NIS2 and AIAct; Attestation never; obligation-fulfilment services never (DSR service, breach notification service and deadline monitor, DPIA review reminders, consent recording)** | One rule for every compliance gate; each regulation module can be scoped | Five more behaviors and tests |
| **C) Gates and fulfilment services alike (a DSR request is refused when DSR does not apply)** | Literal reading of "every module" | Refusing a rights request or skipping a notified breach's deadline is never needed for compliance and can itself be a violation; the application can refuse explicitly if it wants |

### Chosen Option: **B — Every compliance gate except Attestation; fulfilment services untouched** (decided by the maintainer, 2026-10-30)

### Rationale

- The resolver answers "does this regulation's check apply here", and every compliance gate is such a check, including CrossBorderTransfer, ProcessorAgreements, PrivacyByDesign, NIS2 and AIAct. Attestation never consults it: it is integrity evidence of a decision, not a regulation, and must not depend on the decision it attests.
- Obligation-fulfilment services never consult it (DSR service, breach notification service and deadline monitor, DPIA reminders, consent recording): honouring a data subject request or tracking a recorded breach is never wrong.
- The issue task "Update DSR handler to check `DataSubjectRightsApply`" is superseded: the DSR and breach rows are met by their enforcement behaviors (`ProcessingRestrictionPipelineBehavior`, `BreachDetectionPipelineBehavior`).

**Class:** Dilemma (decided by the maintainer, 2026-10-30; see Maintainer Decisions)

</details>

<details>
<summary><strong>9. What "Not Applicable" Does — a binary skip; the module's own mode governs when it applies</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Binary: a capability applies (the module enforces with its configured `EnforcementMode`) or does not apply by territory (the module skips as if the attribute were absent, after the skip is recorded)** | A resolver can only remove a check, never weaken one silently; easy to audit ("skipped capability X") | None for compliance |
| **B) The profile carries a per-capability mode (`Enforce`, `Warn`, `Skip`) that overrides the module's mode (the issue's `ComplianceEnforcementMode EnforcementMode`)** | Supports "observe before enforcing" per tenant | Rejected: a resolver could turn Block into Warn for a subject in scope, a weakening of a legal duty that is not a territorial skip |

### Chosen Option: **A — Binary applicability** (decided by the maintainer, 2026-10-30)

### Rationale

- A capability applies, or it does not apply by territory and every skip is recorded (Design Choice 15). There are no `WarnOnly` or per-capability modes for in-scope subjects; the issue's `ComplianceEnforcementMode` enum is not created. Each module keeps its own configured enforcement mode, which the resolver never changes.

**Class:** Dilemma (decided by the maintainer, 2026-10-30; see Maintainer Decisions)

</details>

<details>
<summary><strong>10. Where the Module Asks — after the attribute check, before any enforcement work</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) After the `Disabled` check and the cached attribute lookup, before subject extraction or any store call; for the after-handler behaviors (Retention, Anonymization, BreachDetection) before `nextStep()` is awaited** | A request without the attribute pays nothing; a skipped capability never runs the module's own fail-closed paths (missing subject) that exist only to enforce | Each behavior's step list changes at one point |
| **B) First thing in `Handle`** | Uniform | Resolves a profile for every request in the application, including those no module cares about |

### Chosen Option: **A — After the attribute check**

### Rationale

- The convention of every compliance behavior is "`Disabled` skip, then cached attribute lookup, then work" (Consent :99-118, DSR :138-151, DPIA :125-144); the applicability question is part of "should this module act", so it belongs at the end of that sequence. Pipeline behaviors use static per-generic-type attribute caching (implementation-plan prompt, rule 3).

**Class:** Trivial
**Settled by:** existing behavior convention, `src/Encina.Compliance.Consent/ConsentRequiredPipelineBehavior.cs:99-118`; implementation-plan prompt rule 3 (pipeline behaviors, static per-generic-type attribute caching); #1244 Design Choice 3 (consult only where the gate already decided)
**Verified:** adversarial-reviewer 2026-10-29

</details>

<details>
<summary><strong>11. Composite Merge Strategy</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Union only (`MostRestrictive`): a capability or regulation applies if any child resolver says so; a child `Left` makes the composite `Left`** | The legal floor is always kept: a child resolver can never remove what another requires | A reduction needs a custom resolver that wraps the others, and #687's engine owns its own overrides |
| **B) Union by default; opt-in `FirstDecisive` (ordered, the first child that returns a non-full profile wins)** | Covers "this subject was manually exempted" without code | Rejected: a manual exemption is not a territorial non-applicability, so it is not allowed |
| **C) Configurable, including `LeastRestrictive` (intersection)** | Maximum flexibility | Any child can silently remove what another requires |

### Chosen Option: **A — Union only** (decided by the maintainer, 2026-10-30)

### Rationale

- The union keeps the legal floor (a tenant processing EU subjects is still bound by GDPR, because the jurisdiction resolver's answer is part of the union). There is no `FirstDecisive` and no manual exemption: compliance is never optional, and only territorial non-applicability may remove a control.

**Class:** Dilemma (decided by the maintainer, 2026-10-30; see Maintainer Decisions)

</details>

<details>
<summary><strong>12. TenantInfo Extension</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Typed `init` properties on `TenantInfo`: `Country` (ISO 3166-1 alpha-2) and `ApplicableRegulations` (`IReadOnlySet<string>?`); the issue's `Jurisdiction` is folded into `ApplicableRegulations`; no `SubscriptionTier` (a commercial tier never decides compliance)** | Discoverable; validated at start-up; meets "TenantInfo extended with jurisdiction metadata" | Each new tenant attribute is a public-API change |
| **B) An open `IReadOnlyDictionary<string, string> Properties` bag on `TenantInfo`** | Any attribute without an API change | No validation; the tenant-aware resolver reads magic keys |
| **C) No change to `TenantInfo`; a `TenantComplianceOptions` map from tenant id to country and regulations** | Tenancy stays compliance-free | Two sources of tenant truth; a database tenant store added later cannot carry the data |

### Chosen Option: **A — Typed properties `Country` and `ApplicableRegulations`, `Jurisdiction` folded into `ApplicableRegulations`**

### Rationale

- The issue's "TenantInfo Extension" section, its task list and its acceptance criterion specify typed fields, and typed fields can be validated (country code format, regulation ids); B and C contradict it. A separate `Jurisdiction` string duplicates `ApplicableRegulations` (the issue's own example values are regulation ids), and a second field is a second source of truth (AGENTS.md §1).
- `SubscriptionTier` is not a resolver input and not a `TenantInfo` field (maintainer correction, 2026-10-30): a commercial tier never decides compliance. Only `Country` and `ApplicableRegulations` feed the resolver.

**Class:** Settled
**Settled by:** #686 issue body ("TenantInfo Extension" section, task list and acceptance criterion); AGENTS.md §1 (every line serves a purpose); `src/Encina.Tenancy/Abstractions/TenantInfo.cs:56-61` (positional record, `init` properties fit)
**Verified:** adversarial-reviewer 2026-10-29

</details>

<details>
<summary><strong>13. Provider Scope — no store, so no provider implementations</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No new store: the new `TenantInfo` fields live in `InMemoryTenantStore` and `TenancyOptions.Tenants`; subject-level overrides are a custom resolver or contributor; the provider matrix does not apply** | Nothing to implement on the 10 providers because no database `ITenantStore` exists (`: ITenantStore` matches only `InMemoryTenantStore`) | A database tenant store added later must carry the fields |
| **B) Add a database `ITenantStore` and a subject-override store on the 10 providers in this issue** | Persisted overrides | Out of the issue's scope ("No store is proposed for subject-level overrides"); a database tenant store is its own feature |

### Chosen Option: **A — No store in this issue**

### Rationale

- AGENTS.md §5 requires all 10 providers for provider-dependent features; this feature has none. The issue's own Provider Implementation Matrix leaves the decision to the plan and records that "No database `ITenantStore` exists today"; any database tenant store added later follows the 10-provider rule and carries the three fields.

**Class:** Settled
**Settled by:** AGENTS.md §5 (Providers); the issue body's Provider Implementation Matrix (#686)
**Verified:** adversarial-reviewer 2026-10-29

</details>

<details>
<summary><strong>14. Built-in Jurisdiction Mapping — GDPR territorial scope and LOPDGDD; other regulations by configuration</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `RegionBasedComplianceResolver` ships one mapping: GDPR applies when the tenant (controller) country or the configured `OperatorCountry` (the application operator, a processor when it hosts other controllers) is in the EU/EEA (Art. 3(1)), or the subject or region is in the EU/EEA (Art. 3(2)); LOPDGDD is added when any of those countries is `ES`; NIS2 and AI Act capabilities are never removed by a country mapping; unknown → full profile; every other regulation id comes from configuration and is an identifier only (`MapCountry("BR", ComplianceRegulation.Parse("LGPD"), capabilities...)`; Encina claims nothing for it, SPEC-002 INV-002)** | Encodes only law the project has researched (SPEC-002 covers EU and Spain); CCPA's California scope cannot be expressed with country codes anyway | Use outside the EU and Spain is at the application's own risk; `OperatorCountry` must be configured to be decisive for an EU-hosted operator |
| **B) Ship mappings for GDPR, CCPA and LGPD as in the issue's examples** | Works out of the box for the three examples | CCPA needs a US state (the `RegionRegistry` has one `US` region, `RegionRegistry.cs:205`); shipping legal mappings for laws outside SPEC-002 is unverified legal content, against INV-003 (defaults follow the applicable law on the verification date) |

### Chosen Option: **A — GDPR and LOPDGDD built in, everything else by configuration**

### Rationale

- GDPR Art. 3(1) applies to processing "in the context of the activities of an establishment of a controller or a processor in the Union, regardless of whether the processing takes place in the Union or not"; Art. 3(2) to controllers or processors not established in the Union that offer goods or services to, or monitor the behaviour of, data subjects in the Union; Art. 3(3) to places where Member State law applies by virtue of public international law. The union of controller establishment, processor (operator) establishment and subject or region location is therefore the only reading that does not under-apply it (the issue's "Non-EU tenant processing EU subject data → GDPR still applies"). Art. 3(3) is not mapped (no country code expresses it); an application that needs it configures `MapCountry`.
- `OperatorCountry` is an option, not a context field: a Spanish SaaS operator that hosts Brazilian tenants is an EU processor, so GDPR applies to its processing whatever the tenant's country. When `OperatorCountry` is not set, the operator is unknown and the resolver returns the full profile (fail closed, AGENTS.md §3; Design Choice 5): a missing configuration value never removes GDPR for an EU-established processor. The resolver narrows only when `OperatorCountry` is set to a non-EEA country and the tenant and the subject (or region) are both known and non-EEA.
- NIS2 (Art. 2-3, entity type and size) and the AI Act (Art. 2, including providers outside the Union) do not follow the data subject's country, so a country mapping never removes the `Nis2` or `AiAct` capability; `OutOfScopeProfile` defaults to `{ Nis2, AiAct }`, not `None` (INV-003).
- The capabilities each regulation brings are one table in core (`ComplianceRegulationCapabilities`, Phase 1): `Gdpr` and `Lopdgdd` → every GDPR capability (`ProcessingActivity`, `Consent`, `LawfulBasis`, `ProcessingRestriction`, `DataResidency`, `CrossBorderTransfer`, `Retention`, `Anonymization`, `BreachDetection`, `Dpia`, `ProcessorAgreements`, `DataMinimization`, `ReadAuditing`); `Nis2` → `Nis2`; `AiAct` → `AiAct`; a configured regulation brings the capabilities its `MapCountry` call lists. Both built-in resolvers use the same table.
- SPEC-002 §3 is the regulatory table the project has verified; INV-003 forbids defaults that are not grounded in the applicable law.
- The region contributor passes `Region.IsEEA` (`Region.cs:77`) as `RegionInEea`; tenant, operator and subject country codes still need an EU/EEA list, which the package copies from `RegionRegistry` (EU-27 and IS, LI, NO); the duplication is recorded in the follow-up file `plan-686-single-source-eea-country-list.md`.

**Class:** Settled
**Settled by:** GDPR Art. 3(1)-(3); NIS2 Directive (EU) 2022/2555 Art. 2-3; AI Act Regulation (EU) 2024/1689 Art. 2; SPEC-002 INV-003 and §3 (regulatory table); LOPDGDD art. 1-2
**Verified:** adversarial-reviewer 2026-10-29

</details>

<details>
<summary><strong>15. Evidence of Skips — record every skip, and a skip that cannot be recorded does not happen</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Record every resolution as an `OperationAuditEntry`** | Full history of decisions | One audit write per request with a compliance attribute, also when nothing is skipped; volume and latency on the hot path |
| **B) Record only skips: when a module asks about a capability that does not apply, `IComplianceDecisionRecorder.RecordSkipAsync` writes `Action = "compliance.capability_skipped"` (capability, regulations, resolver, request type; no subject id) before the module skips; with no recorder registered, or on a recorder `Left`, the capability is enforced instead; an unrecorded skip is not allowed** | Evidence exists for every weakening and only for weakenings; the skip itself fails closed | Needs `Encina.Security.Audit` (or a custom recorder) for any skip to happen |
| **C) Log and metric only** | Cheapest | A log line is not evidence; the issue's matrix says "Compliance decisions MUST be audited" |

### Chosen Option: **B — Record skips write-ahead; no record, no skip**

### Rationale

- REQ-019 asks that "any skip is an explicit, logged opt-out" and GDPR Art. 5(2) asks the controller to demonstrate compliance; the record of a skip is that demonstration. Making the record a precondition of the skip is the write-ahead, fail-closed pattern of #1244 Design Choice 5. There is no `AllowUnrecordedSkips` option: an unrecorded skip is not allowed (maintainer correction, 2026-10-30).
- The recorder implementation lives in `Encina.Security.Audit` (`OperationAuditComplianceDecisionRecorder`, registered with `TryAddEnumerable` by `AddEncinaAudit`), as `AuditStoreABACDecisionRecorder` does for ABAC. Duplicate entries for the same request are suppressed by the per-request memo.
- A has no rule behind it and records non-weakenings that the modules' own evidence already covers; C is excluded by the matrix row "Audit Trail - Compliance decisions MUST be audited".

**Class:** Settled
**Settled by:** #686 issue body (cross-cutting matrix row 12: "Compliance decisions MUST be audited"); SPEC-002 REQ-019, INV-005, DEC-006; GDPR Art. 5(2); maintainer precedent #1244 Design Choice 5 (write-ahead, fail-closed audit); recorder precedent `src/Encina.Security.ABAC/DecisionAudit/AuditStoreABACDecisionRecorder.cs:54`
**Verified:** adversarial-reviewer 2026-10-29

</details>

<details>
<summary><strong>16. Crypto-Shredding — consult per write, encrypt when undecided</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The engine consults the resolver per write and writes a clear-text envelope for a subject outside territorial scope** | Meets the issue's acceptance criterion literally | Rejected: a clear-text value in an append-only store is irreversible and cannot be shredded if the subject later falls under GDPR; it contradicts ADR-034 ("plaintext written once can never be shredded") |
| **B) Always encrypt `[CryptoShredded]` data; the profile is not consulted by the serializer** | Forgetting stays possible for every subject; no blocking call, no new envelope; ADR-034 unchanged | The `CryptoShredding` acceptance criterion of the issue is superseded |
| **C) Decide per document type at registration (`CryptoShreddingOptions.ApplicabilityScope`)** | No per-write cost | Not per subject or tenant, which is the point of the feature |

### Chosen Option: **B — Always encrypt `[CryptoShredded]` data** (decided by the maintainer, 2026-10-30)

### Rationale

- No clear-text envelope exists and ADR-034 is unchanged: the serializer never consults the resolver, and `[CryptoShredded]` data is always encrypted, so forgetting stays possible for every subject (compliance is never optional, and a clear-text value cannot be undone). There is no `ComplianceCapability.CryptoShredding` value.
- The read path is unchanged: it rejects any stored value that is not a `cs2:` token (`CryptoShreddingEngine.Read.cs:161-164`, `EnvelopeMalformed`), which stays the tamper and downgrade detection.

**Class:** Dilemma (decided by the maintainer, 2026-10-30; see Maintainer Decisions)

</details>

<details>
<summary><strong>17. Reuse of the ABAC Policy Decision Point</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Built-in resolvers are plain C#; ABAC is not referenced; an ABAC-backed resolver is left to #687's design (the DSL may compile to XACML or not)** | No Roslyn compilation, no `RequestType` requirement, no `Encina.Security.ABAC` dependency for applicability | An application that already writes XACML policies has two policy languages |
| **B) The built-in resolvers evaluate XACML policies through `IPolicyDecisionPoint.EvaluateAsync`** | One policy language with ABAC | `PolicyEvaluationContext.RequestType` is required (`PolicyEvaluationContext.cs:94`), so background contexts have no natural value; the first EEL evaluation compiles with Roslyn (`EELCompiler.cs:105`); ABAC references Caching, Security and Security.Audit |

### Chosen Option: **A — Plain C# resolvers; ABAC reuse decided in #687**

### Rationale

- The four built-in resolvers are table lookups, and the declarative layer is #687's scope; #687 owns whether its DSL targets XACML (nothing in its body mentions XACML or ABAC). The contract of Design Choice 2 lets anyone write an `AbacComplianceResolver` later.
- B needs `Encina.Compliance.Applicability` to reference `Encina.Security.ABAC` (which references Caching, Security and Security.Audit), contradicting Design Choice 1 and AGENTS.md §1; `PolicyEvaluationContext.RequestType` is required and has no value in background contexts.

**Class:** Settled
**Settled by:** AGENTS.md §1 (pay-for-what-you-use) and Design Choice 1 (resolver package on core only); `src/Encina.Security.ABAC/Model/PolicyEvaluationContext.cs:94` (`RequestType` required); issue ownership: #687 scope
**Verified:** adversarial-reviewer 2026-10-29

</details>

<details>
<summary><strong>18. Registration Name, EventId Range and Coverage Targets</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddEncinaComplianceApplicability(Action<ComplianceApplicabilityOptions>)`; EventIds in a new range `ComplianceApplicability = (8950, 8999)`; per-flag targets in `.github/coverage-manifest/Encina.Compliance.Applicability.json`** | Follows `AddEncina{Package}` (`AddEncinaConsent`, `AddEncinaBreakTheGlass`); 8950-8999 is the free tail of the compliance block (header "Compliance packages (8100-8999)", `EventIdRanges.cs:319`); per-flag coverage is the project rule | Differs from the issue's `AddEncinaCompliance` and its "Code coverage >= 85%" |
| **B) The issue's `AddEncinaCompliance` and a single 85% target** | Literal | `AddEncinaCompliance` names no package and suggests it registers every compliance module; a single percentage is not how coverage is measured |

### Chosen Option: **A — Package-named registration, free compliance range, per-flag targets**

### Rationale

- Registration methods are named after their package (implementation-plan prompt rule 3, "Satellite DI: AddEncina{Feature}"); EventIds use a range registered first in `EventIdRanges.cs` and pick the next free range in the right area (AGENTS.md §7, ADR-021); coverage is per flag against the manifest (AGENTS.md §9).

**Class:** Trivial
**Settled by:** AGENTS.md §7 (EventIds, ADR-021), AGENTS.md §9 (per-flag coverage), implementation-plan prompt rule 3; the two existing methods `AddEncinaConsent` (`src/Encina.Compliance.Consent/ServiceCollectionExtensions.cs:74`) and `AddEncinaBreakTheGlass` (`src/Encina.Security.BreakTheGlass/ServiceCollectionExtensions.cs:53`); `EventIdRanges.cs:353` (last range 8900-8949, so 8950-8999 is free)
**Verified:** adversarial-reviewer 2026-10-29

</details>

<details>
<summary><strong>19. Memo Key — one answer per request instance and full context, never per correlation id</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Key the request-path memo on the request instance (reference identity, `ConditionalWeakTable<object, ComplianceProfile>` held by the scoped memo); key skip records on the built `ComplianceContext` value (tenant, subject, countries, request type, operation) plus the capability** | Two `Send` calls in one scope for two subjects never share an answer; the same request asked by several modules resolves once | A request object reused for two subjects (an anti-pattern) resolves once |
| **B) Key on (correlation id, request type)** | Simple | One HTTP request, saga step or handler loop shares one correlation id (`RequestContext.cs:194`, copied at :93 and :235): the second subject reuses the first subject's "not applicable" and is skipped without a record |

### Chosen Option: **A — Request instance for the answer, full context value for the skip record**

### Rationale

- B fails open: a handler that sends `RecordConsentCommand` for a Brazilian subject and then for a Spanish one in the same scope would skip Consent for the Spanish subject. AGENTS.md §3 and SPEC-002 INV-005 leave only A.

**Class:** Settled
**Settled by:** AGENTS.md §3 (compliance gates fail closed); SPEC-002 INV-005; `src/Encina/Core/RequestContext.cs:194` (correlation id shared across a scope)
**Verified:** adversarial-reviewer 2026-10-29

</details>

<details>
<summary><strong>20. Skip Records Outside a Request — de-duplication of background skips</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The background path takes a `skipKey` (the retention record id) and records a skip at most once per key and capability per `SkipRecordWindow` (default 24 hours, in-memory per process)** | Bounded audit volume: retention keeps returning the same expired, non-applicable records every cycle | After a restart or on another host the same skip is recorded again (over-reports, never under-reports) |
| **B) De-duplicate through the audit store (query before write)** | Exactly one record per key across hosts | A query per skip on the hot path; `OperationAuditQuery` has no metadata filter for the key |
| **C) Record every consultation** | Simplest | One audit entry per record per cycle, forever |

### Chosen Option: **A — In-memory window per key and capability**

### Rationale

- Duplicates only over-report, which the write-ahead pattern of #1244 Design Choice 6 already accepts, while C grows the audit store without bound. B has no cheap form: `OperationAuditQuery` has no metadata filter and it would add a query per skip. The 24-hour window is a trivial default.

**Class:** Settled
**Settled by:** maintainer precedent #1244 Design Choice 6 (decision D29: a duplicate entry after a crash is possible and over-reports); `src/Encina.Security.Audit/OperationAuditQuery.cs:41-151` (no metadata filter)
**Verified:** adversarial-reviewer 2026-10-29

</details>

---

## Implementation Phases

### Phase 1: Core Contract and Models

> **Goal**: The question, the answer and the zero-cost default live in core, so every package can ask without a new reference.

<details>
<summary><strong>Tasks</strong></summary>

All files in `src/Encina/Compliance/`, namespace `Encina.Compliance`.

1. `ComplianceCapability.cs` (public enum): the 15 values of Design Choice 3, explicit numbers, XML docs naming the module and the enforcement point each gates; no `CryptoShredding` value (Design Choice 16).
2. `ComplianceRegulation.cs` (public `readonly record struct`): `string Id` normalised to upper invariant and trimmed; `static Gdpr`, `Lopdgdd`, `Nis2`, `AiAct` (non-EU ids are identifiers only, created through `Parse`; Encina claims nothing for them, SPEC-002 INV-002); `static Either<EncinaError, ComplianceRegulation> Parse(string id)` (non-blank, `[A-Z0-9_-]{2,32}`); `ToString()` returns `Id`.
3. `ComplianceOperation.cs` (public enum): `Unknown`, `Read`, `Write`, `Delete`, `Background`.
4. `ComplianceProfile.cs` (public sealed record): `IReadOnlySet<ComplianceCapability> Capabilities`, `IReadOnlySet<ComplianceRegulation> Regulations`, `string ResolvedBy`; `bool Applies(ComplianceCapability capability)`; `bool IsFull`; `static ComplianceProfile Full(string resolvedBy)` (all enum values, cached frozen set), `static ComplianceProfile None(string resolvedBy)`; `ComplianceProfile Union(ComplianceProfile other)`; value equality over set contents.
5. `ComplianceContext.cs` (public sealed record): `Type? RequestType`, `ComplianceOperation Operation`, `string? TenantId`, `string? TenantCountry`, `IReadOnlySet<string>? TenantRegulations`, `string? SubjectId`, `string? SubjectCountry` (data subject only, from an explicit source), `string? CallerCountry` (caller's claim; never used for GDPR Art. 3(2)), `string? RegionCountry`, `bool? RegionInEea`, `string? ModuleName`, `IReadOnlySet<string>? DataCategories`, `IRequestContext? RequestContext`, `IReadOnlyDictionary<string, object?> Metadata` (empty by default); `ToString()` omits `SubjectId` and `Metadata`; `static ComplianceContext Empty`.
6. `ComplianceContextBuilder.cs` (public sealed class): mutable builder the contributors fill; exposes the input `object? Request` and `Type? RequestType` read-only, so contributors can call extractors that need the request instance (`IDataSubjectIdExtractor.ExtractSubjectId<TRequest>(request, context)`, `IDataSubjectIdExtractor.cs:65`); `Build()`; country codes normalised to ISO 3166-1 alpha-2 upper case, invalid values left `null` (unknown, Design Choice 5).
7. `IComplianceApplicabilityResolver.cs`: `ValueTask<Either<EncinaError, ComplianceProfile>> ResolveAsync(ComplianceContext context, CancellationToken cancellationToken = default)`; XML docs state the invariants: unknown context returns the full profile; a resolver never abstains; never throws for business outcomes.
8. `IComplianceContextContributor.cs`: `int Order { get; }`, `ValueTask<Either<EncinaError, Unit>> ContributeAsync(ComplianceContextBuilder builder, IRequestContext? requestContext, CancellationToken cancellationToken)`; the request instance and type come from the builder (task 6).
9. `IComplianceApplicability.cs`: `ValueTask<Either<EncinaError, bool>> AppliesAsync<TRequest>(TRequest request, IRequestContext context, ComplianceCapability capability, CancellationToken cancellationToken) where TRequest : notnull` (request path) and `ValueTask<Either<EncinaError, bool>> AppliesAsync(ComplianceContext context, ComplianceCapability capability, string skipKey, CancellationToken cancellationToken)` (background path; `skipKey` de-duplicates skip records per Design Choice 20). Under Design Choice 6 A the result is never `Left`.
9a. `ComplianceRegulationCapabilities.cs` (public static): the regulation → capability table of Design Choice 14, used by both built-in resolvers and by #687.
9b. `ComplianceAttributeRegistry.cs` (public sealed, singleton, `TryAddSingleton` in the defaults of task 13): `RegisterConsultPoint(ComplianceCapability capability, string pointName, Type? requestAttributeType = null)`; `bool IsGoverned(Type requestType)` cached per type. `IsGoverned` is an optimisation only (the orchestration behavior warms the memo for governed types); modules work the same whether or not it returns true. It lives in core so modules that reference only `Encina` can register.
9c. `DataSubjectCountryAttribute.cs` (public, `AttributeTargets.Property`): marks the request property that holds the data subject's country (ISO 3166-1 alpha-2), the explicit source of `SubjectCountry` (Design Choice 5).
10. `IComplianceDecisionRecorder.cs` and `ComplianceSkipRecord.cs` (sealed record: `ComplianceCapability Capability`, `ComplianceProfile Profile`, `string? RequestTypeName`, `string? TenantId`, `string CorrelationId`, `DateTimeOffset SkippedAtUtc`; no subject id): `ValueTask<Either<EncinaError, Unit>> RecordSkipAsync(ComplianceSkipRecord record, CancellationToken cancellationToken)`.
11. `AlwaysEnforceComplianceResolver.cs` (public sealed): returns `Right(ComplianceProfile.Full(nameof(...)))`.
12. `AlwaysAppliesComplianceApplicability.cs` (internal sealed): returns a cached completed `Right(true)` task; no resolution, no allocation.
13. `ComplianceApplicabilityDefaults.cs` (public static): `IServiceCollection TryAddEncinaComplianceApplicabilityDefaults(this IServiceCollection services)` → `TryAddSingleton<IComplianceApplicability, AlwaysAppliesComplianceApplicability>()`; called from `AddEncina` (`src/Encina/Core/ServiceCollectionExtensions.cs:43`) and, in Phases 4-6, from every module's `AddEncina*` (registration completeness, AGENTS.md §3).
14. `ComplianceApplicabilityErrors.cs` (public static, `EncinaErrors.Create`): `compliance.applicability_failed`, `compliance.invalid_regulation`, `compliance.skip_not_recorded`; messages are fixed strings without identifiers.
15. `src/Encina/PublicAPI.Unshipped.txt`: every public symbol.
16. `src/Encina/Encina.csproj`: `InternalsVisibleTo` for `Encina.Compliance.Applicability`, so Phase 2 can remove the internal default descriptor by implementation type (the BreakTheGlass precedent relies on the same grant).
17. Tests: `tests/Encina.UnitTests/Core/Compliance/` (profile set semantics, `Union`, `Full`/`None`, regulation parsing, builder normalisation, regulation table, registry, default applicability), `tests/Encina.GuardTests/Core/Compliance/`, `tests/Encina.PropertyTests/Core/Compliance/` (union commutative, associative, idempotent, monotonic).
18. `.github/coverage-manifest/Encina.json`: per-file, per-flag targets with justifications for every new file (AGENTS.md §9, #1762).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
You are implementing Phase 1 of issue #686 (Compliance Applicability Resolver): the core contract.

CONTEXT:
- Encina is a .NET 10 / C# 14 library; Railway Oriented Programming with Either<EncinaError, T>; pre-1.0, no compatibility layers.
- Plan: docs/plans/compliance-applicability-resolver-implementation-plan-686.md, Design Choices 2-5 and 9 fix the names and semantics
  (read the Maintainer Decisions section first if it exists: it overrides the recommendations).
- Request context: src/Encina/Abstractions/IRequestContext.cs; error factory EncinaErrors.Create.
- Core registration: src/Encina/Core/ServiceCollectionExtensions.cs (AddEncina at line 43).

TASK:
Create the files of the Phase 1 tasks in src/Encina/Compliance/ (namespace Encina.Compliance), call
TryAddEncinaComplianceApplicabilityDefaults from AddEncina, add the InternalsVisibleTo grant, update
src/Encina/PublicAPI.Unshipped.txt and .github/coverage-manifest/Encina.json, and write the unit, guard and
property tests listed.

KEY RULES:
- Sealed records with value equality over set contents (ComplianceProfile compares capability and regulation sets).
- ComplianceProfile.Full is cached and frozen (FrozenSet); AlwaysAppliesComplianceApplicability allocates nothing per call.
- An unknown or invalid field means "unknown", never a narrower profile; document it in the XML docs of the resolver interface.
- No subject id or Metadata in ToString, error messages or exception messages.
- XML docs on every public member (<summary>, <remarks>, <example> where useful); RS0016 must not fire.

REFERENCE FILES:
- src/Encina/Authorization/IAccessElevationGate.cs (core contract + no-op default precedent)
- src/Encina/Abstractions/IRequestContext.cs
- src/Encina/Errors/EncinaErrorCodes.cs
- src/Encina/Core/ServiceCollectionExtensions.cs
```

</details>

---

### Phase 2: The `Encina.Compliance.Applicability` Package — Options, Resolvers and Registration

> **Goal**: An application can choose a resolver; the four built-in resolvers of the issue work on a context.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Compliance.Applicability/Encina.Compliance.Applicability.csproj` (`net10.0`, nullable, XML docs, PublicAPI analyzers; references only `Encina`; packages `Microsoft.Extensions.Options`, `Microsoft.Extensions.Logging.Abstractions`, `Microsoft.Extensions.Diagnostics.HealthChecks`); add to `Encina.slnx`; `PublicAPI.Shipped.txt` (empty) and `PublicAPI.Unshipped.txt`.
2. `ComplianceApplicabilityOptions.cs`: resolver selection (`UseResolver<TResolver>()`, `UseRegionBasedResolver(Action<RegionBasedResolverOptions>?)`, `UseTenantAwareResolver(Action<TenantAwareResolverOptions>)`, `UseCompositeResolver(Action<CompositeResolverBuilder>)`), `bool AddOrchestrationBehavior = true`, `bool AddHealthCheck = true`, `int FailureDegradedThreshold = 5`, `TimeSpan SkipRecordWindow = 24 h` (Design Choice 20).
3. `ComplianceApplicabilityOptionsValidator : IValidateOptions<ComplianceApplicabilityOptions>` with `ValidateOnStart`: exactly one resolver selected; a composite has at least one child; every configured country code is ISO 3166-1 alpha-2; every regulation id parses.
4. `Resolvers/RegionBasedComplianceResolver.cs` and `Resolvers/RegionBasedResolverOptions.cs`: Design Choice 14 mapping over tenant, operator, subject and region countries (`RegionInEea` when present); `OperatorCountry`; `AssumedSubjectCountry` (an explicit territorial assertion that every subject lives in that country, logged at start-up; it can only name a country, never switch a control off by itself); `MapCountry(string countryCode, ComplianceRegulation regulation, params ComplianceCapability[] capabilities)`; `OutOfScopeCapabilities` (capabilities kept for a known country with no mapped regulation, default `{ Nis2, AiAct }`); decisive only under the rule of Design Choice 5, otherwise `Full`; capabilities from `ComplianceRegulationCapabilities`.
5. `Resolvers/EuropeanEconomicAreaCountries.cs` (internal): EU-27 plus IS, LI, NO (copy of `RegionRegistry.cs:55-146`, see follow-up file).
6. `Resolvers/TenantAwareComplianceResolver.cs` and `TenantAwareResolverOptions.cs`: reads `TenantRegulations` only (never a commercial tier); the regulations the tenant declares add their capability sets from `ComplianceRegulationCapabilities` (Phase 1); no tenant or no declared regulations → `Full`.
7. `Resolvers/CompositeComplianceResolver.cs`, `CompositeResolverBuilder.cs`, children resolved in order; the result is the union (Design Choice 11, union only: no merge-strategy option) and any child `Left` makes the composite `Left`.
8. `Contributors/RequestComplianceContextContributor.cs`: fills `RequestType`, `Operation` from the generic `ICommand<TResponse>` / `IQuery<TResponse>` interfaces (`src/Encina/Abstractions/CqrsContracts.cs:29,60`), `TenantId` and `ModuleName` from `GetModuleName()`, and `SubjectCountry` from the request property marked `[DataSubjectCountry]` (compiled accessor cached per request type).
8a. `Contributors/ClaimComplianceContextContributor.cs`: reads a configurable claim type (`CallerCountryClaimType`, default `"country"`) from `IRequestContext.Identity.Principal` into `CallerCountry` only; XML docs say the claim is the caller's, not the data subject's (Design Choice 5).
9. `ServiceCollectionExtensions.cs`: `AddEncinaComplianceApplicability(this IServiceCollection, Action<ComplianceApplicabilityOptions>?)`: options and validator; `TryAddSingleton(TimeProvider.System)`; the selected resolver (singleton); `TryAddEnumerable` the request and claim contributors; replaces the core `AlwaysAppliesComplianceApplicability` descriptor (by implementation type, never an application's own registration) with the Phase 3 service.
10. `src/Encina/Diagnostics/EventIdRanges.cs`: register `ComplianceApplicability = (8950, 8999)` in the compliance block; add the field to `src/Encina/PublicAPI.Unshipped.txt`; add `Encina.Compliance.Applicability` to `AssemblyRanges` in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`.
11. `Diagnostics/ComplianceApplicabilityLogMessages.cs` (`[LoggerMessage]`, XML doc `Event IDs: 8950-8999 (see EventIdRanges.ComplianceApplicability)`): 8950 `ProfileResolved` (Debug), 8951 `UnknownCountryCode` (Debug), 8952 `AssumedSubjectCountryConfigured` (Warning, start-up). Phases 3 and 8 continue from 8953.
12. Tests: unit tests per resolver (EU tenant, non-EU tenant with EU subject, EU operator with non-EU tenant and subject, `ES` adds LOPDGDD, non-EEA known countries keep `Nis2` and `AiAct`, caller claim never decides GDPR, unknown → full, unset `OperatorCountry` → full, a tenant declaring regulations, composite union, child `Left`), options validator, DI test with `ValidateOnBuild` and `ValidateScopes`; guard tests for every public member of the package.
13. `.github/coverage-manifest/Encina.Compliance.Applicability.json` (new) with per-file, per-flag targets and justifications for the files of this phase.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
You are implementing Phase 2 of issue #686: the new package Encina.Compliance.Applicability with options, the
region-based, tenant-aware and composite resolvers, the request and claim contributors and the registration.

CONTEXT:
- Phase 1 contract exists in src/Encina/Compliance/.
- Plan Design Choices 1, 5, 11 and 14 fix the package graph (references only Encina), the unknown-means-full rule,
  the union-only merge and the GDPR/LOPDGDD mapping. Maintainer Decisions, if present, override them.
- EU/EEA country list source: src/Encina.Compliance.DataResidency/Model/RegionRegistry.cs lines 55-146.

TASK:
Create the package, its options and validator, the three resolvers, the two contributors, the registration and
the EventId range ComplianceApplicability = (8950, 8999), with the tests listed in the Phase 2 tasks.

KEY RULES:
- The package references only Encina (no Tenancy, DataResidency, Marten, Messaging or Caching).
- Any unknown, missing or invalid input returns ComplianceProfile.Full; never a narrower profile.
- GDPR applies when the tenant, operator, subject or region country is in the EU/EEA (GDPR Art. 3(1) and 3(2)).
- The caller's country claim fills CallerCountry only; SubjectCountry comes only from [DataSubjectCountry] or a
  custom contributor. A country mapping never removes Nis2 or AiAct.
- Register the EventId range first; EventIds packed from 8950; [LoggerMessage] source generator only.
- TryAdd/TryAddEnumerable; the DI test builds with ValidateOnBuild and ValidateScopes.
- No subject id in logs, tags or exceptions.

REFERENCE FILES:
- src/Encina.Security.BreakTheGlass/ServiceCollectionExtensions.cs (replacing a core default by implementation type)
- src/Encina.Compliance.Consent/ConsentOptionsValidator.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 3: Resolution Service, Context Contributors, Orchestration Behavior and `TenantInfo`

> **Goal**: A request gets one profile; tenancy, residency and DSR feed the context; skips are recorded or refused.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Compliance.Applicability/Services/ComplianceApplicabilityService.cs : IComplianceApplicability` (scoped). Ctor: `IComplianceApplicabilityResolver`, `IEnumerable<IComplianceContextContributor>`, `IEnumerable<IComplianceDecisionRecorder>`, `ComplianceProfileMemo`, `IOptions<ComplianceApplicabilityOptions>`, `TimeProvider`, `ILogger<ComplianceApplicabilityService>`.
   - Request path: memo hit for this request instance → answer; miss → build context (contributors by `Order`; a contributor `Left` or exception leaves its fields unknown and logs 8953 `ContributorFailed` with the error code only) → resolve (`Left`/exception → `Full`, log 8954 `ResolutionFailed`, per Design Choice 6) → store in memo.
   - When `Applies(capability)` is false: record the skip through every recorder (Design Choice 15); no recorder registered → return `true` and log 8955 `SkipNotRecordedEnforcing` (an unrecorded skip is not allowed); a recorder `Left` → return `true` and log 8956 `SkipRecordFailedEnforcing`; recorded → log 8957 `CapabilitySkipped` (Information: request type, capability, tenant; no subject) and return `false`. A skip is recorded once per context value and capability (Design Choice 19).
   - Background path: no memo; skip records de-duplicated per `skipKey` and capability within `SkipRecordWindow` (Design Choice 20, `IMemoryCache`-free bounded dictionary with `TimeProvider` expiry).
2. `Services/ComplianceProfileMemo.cs` (internal, scoped): `ConditionalWeakTable<object, ComplianceProfile>` keyed by the request instance, plus the set of recorded `(ComplianceContext, ComplianceCapability)` skips (Design Choice 19); a test sends two requests for two subjects in one scope and expects two resolutions.
3. `Pipeline/ComplianceOrchestrationBehavior.cs : IPipelineBehavior<TRequest, TResponse>`: static per-type flag from the core `ComplianceAttributeRegistry.IsGoverned` (an optimisation only, Phase 1 task 9b); when true, resolves through the service (warms the memo), opens `encina.compliance.orchestrate`; registered with `TryAddEnumerable` when `AddOrchestrationBehavior` is true. Never returns `Left` for a resolution failure under Design Choice 6 A.
4. `Services/SkipRecordWindow.cs` (internal, singleton): the per-process de-duplication of Design Choice 20.
5. `src/Encina.Tenancy/Abstractions/TenantInfo.cs`: `Country` and `ApplicableRegulations` as `init` properties (Design Choice 12); `TenancyOptions` binding unchanged (properties are settable through `with`); `PublicAPI.Unshipped.txt`.
6. `src/Encina.Tenancy/Compliance/TenantComplianceContextContributor.cs` (internal): tenant id = `builder.TenantId` (set by the caller, for example the retention enforcer) or else the ambient `ITenantProvider.GetCurrentTenantId()`; then `ITenantStore.GetTenantAsync(tenantId)` → `TenantCountry`, `TenantRegulations`. `GetTenantAsync` returns `TenantInfo?` and has no `Left` (`ITenantStore.cs:68`), so an exception is caught and returned as `Left` (fields stay unknown) and a `null` tenant leaves them unknown; registered by `AddEncinaTenancy` with `TryAddEnumerable`. This lets background contexts, which have no ambient request (`DefaultTenantProvider.cs:44`), still carry the tenant's country.
7. `src/Encina.Tenancy/Configuration/TenancyOptionsValidator` (existing or new): country code format and regulation ids of `TenancyOptions.Tenants`.
8. `src/Encina.Compliance.DataResidency/Compliance/RegionComplianceContextContributor.cs` (internal): `IRegionContextProvider.GetCurrentRegionAsync` → `RegionCountry = region.Country`, `RegionInEea = region.IsEEA` (`Region.cs:58,77`; a `Left` leaves both unknown); registered by `AddEncinaDataResidency`.
9. `src/Encina.Compliance.DataSubjectRights/Compliance/SubjectComplianceContextContributor.cs` (internal): `IDataSubjectIdExtractor.ExtractSubjectId(builder.Request, requestContext)` → `SubjectId`, and `DataCategories` from `[PersonalData]` (`Attributes/PersonalDataAttribute.cs:45`); registered by `AddEncinaDataSubjectRights`.
10. Tests: service (memo per request instance, two subjects in one scope, every failure path of Design Choices 5, 6 and 15 including no recorder registered, the skip window with `FakeTimeProvider`), orchestration behavior (governed and ungoverned types), the three contributors (tenant id from the builder without an ambient request; throwing store), `TenantInfo` equality with the new properties, DI tests for Tenancy, DataResidency and DSR without the Applicability package (the core default resolves); guard tests for the new public members.
11. Coverage manifests: `Encina.Compliance.Applicability.json`, `Encina.Tenancy.json`, `Encina.Compliance.DataResidency.json`, `Encina.Compliance.DataSubjectRights.json` for every file of this phase.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
You are implementing Phase 3 of issue #686: the resolution service with its per-request memo and skip recording,
the orchestration behavior, the TenantInfo extension and the three context contributors.

CONTEXT:
- Phases 1-2 are merged: core contract in src/Encina/Compliance/, package src/Encina.Compliance.Applicability/.
- Tenancy: src/Encina.Tenancy/Abstractions/TenantInfo.cs (positional record, lines 56-61), ITenantProvider.cs:104.
- Residency: src/Encina.Compliance.DataResidency/Abstractions/IRegionContextProvider.cs:62.
- DSR: src/Encina.Compliance.DataSubjectRights/Abstractions/IDataSubjectIdExtractor.cs.
- Plan Design Choices 5, 6, 7, 12 and 15 fix the failure semantics; Maintainer Decisions override them.

TASK:
Implement ComplianceApplicabilityService, ComplianceProfileMemo, SkipRecordWindow, ComplianceOrchestrationBehavior,
the TenantInfo properties and the contributors in Tenancy, DataResidency and DSR, the coverage manifest entries,
and the tests listed in the Phase 3 tasks.

KEY RULES:
- A skip happens only after a successful record; an unrecorded skip is not allowed.
- Contributor and resolver failures widen the profile to Full; they never narrow it.
- The memo is keyed by the request instance, never by correlation id (Design Choice 19): two subjects in one scope
  resolve twice.
- The tenant contributor takes the tenant id from the builder first and reads ITenantStore, so background contexts work.
- Time from TimeProvider only; CancellationToken on every async call; exceptions to loggers through ForLogging().
- No subject id, EncinaError.Message or Metadata values in logs, tags or audit metadata.
- Each AddEncina* still registers every dependency it resolves (ValidateOnBuild + ValidateScopes tests).

REFERENCE FILES:
- src/Encina.Security.BreakTheGlass/Gate/BreakTheGlassRequestGrants.cs (per-request holder)
- src/Encina.Tenancy/ServiceCollectionExtensions.cs
- src/Encina.Compliance.DataResidency/ServiceCollectionExtensions.cs
- src/Encina.Compliance.DataSubjectRights/ServiceCollectionExtensions.cs
```

</details>

---

### Phase 4a: Module Integration — Request Gates of the GDPR Core (GDPR, Consent, LawfulBasis, DSR)

> **Goal**: The four GDPR-core behaviors that decide before the handler consult the resolver.

<details>
<summary><strong>Tasks</strong></summary>

For each behavior: inject `IComplianceApplicability`; after the `Disabled` check and the attribute lookup (Design Choice 10), call `AppliesAsync(request, context, <capability>, cancellationToken)`; `Right(false)` → `return await nextStep()` (the service already logged and recorded); `Right(true)` → current code unchanged; `Left` (not produced under Design Choice 6 A; kept for a custom service) → return it. Each module's `AddEncina*` calls `TryAddEncinaComplianceApplicabilityDefaults()` and registers its consult point in the core `ComplianceAttributeRegistry` (Phase 1 task 9b). Behaviors whose dispatcher is a non-async `ValueTask` method returning `nextStep()` directly become async at that point; the phase line estimate includes it.

1. `src/Encina.Compliance.GDPR/GDPRCompliancePipelineBehavior.cs` (after :116-124) → `ProcessingActivity`; `ServiceCollectionExtensions.cs:68`.
2. `src/Encina.Compliance.Consent/ConsentRequiredPipelineBehavior.cs` (after :110-118, before subject extraction :121) → `Consent`; `ServiceCollectionExtensions.cs:74`.
3. `src/Encina.Compliance.LawfulBasis/Pipeline/LawfulBasisValidationPipelineBehavior.cs` (after :115-119) → `LawfulBasis`; `ServiceCollectionExtensions.cs:78`.
4. `src/Encina.Compliance.DataSubjectRights/ProcessingRestrictionPipelineBehavior.cs` (after :147-151) → `ProcessingRestriction`; `ServiceCollectionExtensions.cs:81`.
5. `PublicAPI.Unshipped.txt` of each package if a public constructor changes.
6. Unit and guard tests per behavior (4 files extended): capability not applicable → handler called, module service `DidNotReceive`; applicable → current behavior; the module's missing-subject or missing-tenant `Left` is not reached when skipped.
7. Coverage manifests of the four packages: targets and justifications for every touched file.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4a</strong></summary>

```text
You are implementing Phase 4a of issue #686: GDPR, Consent, LawfulBasis and DSR behaviors consult IComplianceApplicability.

CONTEXT:
- Phases 1-3 are merged. IComplianceApplicability (src/Encina/Compliance/) answers per request; the core default
  always answers true; the Applicability package records skips before answering false.
- Behaviors and lines are listed in the Phase 4a tasks of docs/plans/compliance-applicability-resolver-implementation-plan-686.md.

TASK:
Add the applicability check to the four behaviors at the point named in each task, register each module's consult
point in ComplianceAttributeRegistry, call TryAddEncinaComplianceApplicabilityDefaults from each AddEncina*, update
the coverage manifests and extend the tests as listed.

KEY RULES:
- The check goes after the Disabled check and the cached attribute lookup, before any subject extraction or store call.
- A skipped capability calls nextStep() and nothing else; an applicable capability runs today's code unchanged.
- Do not change enforcement modes or their defaults.
- Behaviors keep their current registration call (two use AddTransient today; the follow-up debt issue covers it).
- No new log messages in the modules: the Applicability service logs the skip.
- CRAP <= 10 on every changed Handle method: if the check pushes complexity up, extract the skip branch.

REFERENCE FILES:
- src/Encina.Compliance.Consent/ConsentRequiredPipelineBehavior.cs
- tests/Encina.UnitTests/Compliance/Consent/ (existing behavior tests)
- docs/engineering/crap-gate-design.md
```

</details>

---

### Phase 4b: Module Integration — Residency, Transfer, DPIA and Processor Gates

> **Goal**: The other four pre-handler behaviors consult the resolver.

<details>
<summary><strong>Tasks</strong></summary>

Same pattern as Phase 4a.

1. `src/Encina.Compliance.DataResidency/DataResidencyPipelineBehavior.cs` (`ShouldSkipEnforcement` :177-189 stays synchronous; the async check follows it) → `DataResidency`; `ServiceCollectionExtensions.cs:75`.
2. `src/Encina.Compliance.CrossBorderTransfer/Pipeline/TransferBlockingPipelineBehavior.cs` (after :137-144) → `CrossBorderTransfer` (Design Choice 8); `ServiceCollectionExtensions.cs:71`.
3. `src/Encina.Compliance.DPIA/DPIARequiredPipelineBehavior.cs` (after :136-144; `DispatchAsync` :116-146 becomes async) → `Dpia`; `ServiceCollectionExtensions.cs:100`.
4. `src/Encina.Compliance.ProcessorAgreements/ProcessorValidationPipelineBehavior.cs` (after :141-149; dispatcher becomes async) → `ProcessorAgreements` (Design Choice 8); `ServiceCollectionExtensions.cs:75`.
5. `PublicAPI.Unshipped.txt` of each package if a public constructor changes.
6. Unit and guard tests per behavior (4 files extended), as in Phase 4a.
7. Coverage manifests of the four packages.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4b</strong></summary>

```text
You are implementing Phase 4b of issue #686: DataResidency, CrossBorderTransfer, DPIA and ProcessorAgreements
behaviors consult IComplianceApplicability.

CONTEXT:
- Phases 1-4a are merged; Phase 4a shows the pattern.
- Behaviors and lines are listed in the Phase 4b tasks of docs/plans/compliance-applicability-resolver-implementation-plan-686.md.

TASK:
Add the applicability check to the four behaviors, register their consult points, call
TryAddEncinaComplianceApplicabilityDefaults from each AddEncina*, update the coverage manifests and extend the tests.

KEY RULES:
- Same rules as Phase 4a: check after the Disabled check and the attribute lookup; skip means nextStep() only.
- Making a dispatcher async must not change its behavior when the capability applies.
- CRAP <= 10 on every changed method.

REFERENCE FILES:
- Phase 4a changes (same pattern)
- src/Encina.Compliance.DPIA/DPIARequiredPipelineBehavior.cs
- docs/engineering/crap-gate-design.md
```

</details>

---

### Phase 5a: Module Integration — Retention, Anonymization and Breach Detection

> **Goal**: The after-handler behaviors and the retention background service consult the resolver.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Compliance.Retention/RetentionValidationPipelineBehavior.cs` (after :144-147, before `nextStep()` at :153) → `Retention`.
2. `src/Encina.Compliance.Retention/RetentionEnforcementService.cs` (`ExecuteEnforcementCycleAsync` :126): per expired record, `AppliesAsync(ComplianceContext { TenantId, SubjectId?, Operation = Background, DataCategories }, Retention, skipKey: record id)` inside a scope (the tenant contributor reads the tenant's country from the store, Phase 3 task 6); not applicable → the record is left in place and counted; `Left` (not produced by the built-in service under Design Choice 6 A; a custom service may return it) → the cycle fails for that record, never reported as success (AGENTS.md §3).
3. `src/Encina.Compliance.Anonymization/AnonymizationPipelineBehavior.cs` (after :142-146, before :149) → `Anonymization`.
4. `src/Encina.Compliance.BreachNotification/BreachDetectionPipelineBehavior.cs` (after :127-130, before :133) → `BreachDetection`. `DefaultBreachNotificationService` and `BreachDeadlineMonitorService` are not touched (Design Choice 8).
5. Each module's `AddEncina*`: `TryAddEncinaComplianceApplicabilityDefaults()` and consult-point registration (Retention registers two points: behavior and enforcement service).
6. Unit and guard tests per behavior (3 files extended) and for the retention cycle with `FakeTimeProvider`: applicable deleted; not applicable kept; across two cycles inside `SkipRecordWindow` one skip record per record; after the window a new one.
7. Coverage manifests of the three packages.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5a</strong></summary>

```text
You are implementing Phase 5a of issue #686: Retention (behavior and enforcement service), Anonymization and
BreachDetection consult IComplianceApplicability.

CONTEXT:
- Phases 1-4b are merged; Phase 4a shows the pattern.
- Three behaviors act after the handler (Retention :153, Anonymization :149, BreachDetection :133): the check must
  run before nextStep() so a skipped capability does no post-processing.
- RetentionEnforcementService.cs:126 is a BackgroundService: use the ComplianceContext overload with the record id
  as skipKey inside a DI scope.

TASK:
Add the check to the three behaviors and the retention cycle as listed in the Phase 5a tasks, with their tests and
manifest entries.

KEY RULES:
- Fulfilment services (DSR service, breach notification service, deadline monitors, DPIA reminders) are not changed.
- In background work a Left fails the operation for that record; it never reports success (AGENTS.md section 3).
- No subject id in logs or tags; time from TimeProvider.
- CRAP <= 10 on changed methods.

REFERENCE FILES:
- src/Encina.Compliance.Retention/RetentionEnforcementService.cs
- src/Encina.Compliance.Anonymization/AnonymizationPipelineBehavior.cs
- Phase 4a changes (same pattern)
```

</details>

---

### Phase 5b: Module Integration — PrivacyByDesign, NIS2 and AIAct

> **Goal**: The data-minimisation and the two non-GDPR regulation modules consult the resolver.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Compliance.PrivacyByDesign/DataMinimizationPipelineBehavior.cs` (after `ShouldSkipEnforcement` :185-210) → `DataMinimization`.
2. `src/Encina.Compliance.NIS2/NIS2CompliancePipelineBehavior.cs` (after :112-116) → `Nis2`.
3. `src/Encina.Compliance.AIAct/AIActCompliancePipelineBehavior.cs` (after :118-126; dispatcher becomes async) → `AiAct`. The prohibited-practice block (:188-193) is part of the AI Act capability and is skipped with it; XML docs say so.
4. Each module's `AddEncina*`: `TryAddEncinaComplianceApplicabilityDefaults()` and consult-point registration.
5. Unit and guard tests per behavior (3 files extended).
6. Coverage manifests of the three packages.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5b</strong></summary>

```text
You are implementing Phase 5b of issue #686: PrivacyByDesign, NIS2 and AIAct consult IComplianceApplicability.

CONTEXT:
- Phases 1-5a are merged; Phase 4a shows the pattern.
- The region-based resolver never removes Nis2 or AiAct (plan Design Choice 14); only a tenant-aware or custom
  resolver can, and only for territorial non-applicability.

TASK:
Add the check to the three behaviors as listed in the Phase 5b tasks, with their tests and manifest entries.

KEY RULES:
- Check after the Disabled check and the attribute lookup; skip means nextStep() only.
- Do not change enforcement modes or their defaults.
- CRAP <= 10 on changed methods.

REFERENCE FILES:
- src/Encina.Compliance.AIAct/AIActCompliancePipelineBehavior.cs
- src/Encina.Compliance.NIS2/NIS2CompliancePipelineBehavior.cs
- Phase 4a changes (same pattern)
```

</details>

---

### Phase 6: Read Auditing

> **Goal**: The non-pipeline read-audit enforcement point consults the resolver. Crypto-shredding is not gated: `[CryptoShredded]` data is always encrypted and `Encina.Marten.GDPR` is not changed (Design Choice 16).

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Security.Audit/AuditedReadOnlyRepository.cs`: `ShouldAudit` (:211) becomes `ShouldAuditAsync`; after the existing checks, `AppliesAsync(ComplianceContext { TenantId, RequestType = typeof(TEntity), Operation = Read, RequestContext }, ReadAuditing, skipKey: entity type + tenant)` using the `IRequestContext` the decorator already injects (:43); not applicable → no read-audit entry. The read-audit write stays fire-and-forget (:205), but the applicability question (and the skip record it may write) is awaited inline, which adds one memo or resolver call to the read path; the benchmark of Phase 9 measures it. The #1193 decorator, when merged, gets the same check.
2. `src/Encina.Security.Audit/ServiceCollectionExtensions.cs` (`AddEncinaReadAuditing` :179): `TryAddEncinaComplianceApplicabilityDefaults()`.
3. Tests: read-audit decorator (applicable, not applicable, failure → audit), guard tests.
4. Coverage manifest `Encina.Security.Audit.json`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
You are implementing Phase 6 of issue #686: read auditing consults IComplianceApplicability.

CONTEXT:
- Phases 1-5 are merged.
- Read audit: src/Encina.Security.Audit/AuditedReadOnlyRepository.cs (ShouldAudit at line 211).
- Crypto-shredding is NOT gated: [CryptoShredded] data is always encrypted and Encina.Marten.GDPR is not changed
  (plan Design Choice 16, decided by the maintainer).
- Plan Design Choice 20 (Maintainer Decisions override the recommendations).

TASK:
Gate read-audit entries on the ReadAuditing capability as listed in the Phase 6 tasks and write the tests.

KEY RULES:
- Any failure to decide means audit (fail closed).
- Do not touch Encina.Marten.GDPR.
- EventIds only inside the package's registered range.

REFERENCE FILES:
- src/Encina.Security.Audit/ReadAuditOptions.cs
- docs/plans/evidential-read-audit-implementation-plan-1193.md
```

</details>

---

### Phase 7: Cross-Cutting Integration — Skip Records, Tenancy and Module Scope

> **Goal**: Skips become operation-audit evidence; tenant and module flow into every decision.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Security.Audit/Compliance/OperationAuditComplianceDecisionRecorder.cs : IComplianceDecisionRecorder` (scoped): writes `OperationAuditEntry { Action = "compliance.capability_skipped", EntityType = "ComplianceCapability", EntityId = capability name, TenantId, CorrelationId, Outcome = Success, Metadata = { "compliance.capability", "compliance.regulations", "compliance.resolver", "compliance.request_type" } }`; returns the store's `Either`; writes through its own scope (`IServiceScopeFactory`) inside `TransactionScope(TransactionScopeOption.Suppress)`, as `AuditStoreABACDecisionRecorder.cs:100-103` does, so a rolled-back unit of work never erases the evidence; registered by `AddEncinaAudit` (:79) with `TryAddEnumerable`.
2. `ComplianceApplicabilityStartupValidator` (hosted, runs once, in the Applicability package): fails start-up when no `IComplianceDecisionRecorder` is registered while the configured resolver is not `AlwaysEnforceComplianceResolver` (so the misconfiguration is visible before the first request); there is no opt-out, because an unrecorded skip is not allowed (Design Choice 15).
3. Tenancy: the tenant id is on every skip record, metric and span; with tenancy registered and no tenant the context is unknown (Design Choice 5).
4. Module isolation: `ModuleName` in the context (Phase 2 contributor) and in the skip metadata.
5. Tests: recorder (entry shape, store `Left` propagated, record survives a rolled-back ambient transaction), start-up validator, guard tests, a two-tenant test (tenant A in `ES` enforces; tenant B in `BR` with a request carrying `[DataSubjectCountry] = "BR"` and `OperatorCountry = "BR"` skips with a record per skip; the same request with `OperatorCountry` unset enforces fully; SPEC-002 AC-043).
6. Coverage manifests `Encina.Security.Audit.json` and `Encina.Compliance.Applicability.json`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
You are implementing Phase 7 of issue #686: the operation-audit skip recorder, the start-up validator and the
tenant and module propagation.

CONTEXT:
- Phases 1-6 are merged. IComplianceDecisionRecorder and ComplianceSkipRecord are in src/Encina/Compliance/.
- Audit sink: src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs (RecordAsync line 50),
  OperationAuditEntry.cs (Metadata line 252). Precedent: src/Encina.Security.ABAC/DecisionAudit/AuditStoreABACDecisionRecorder.cs.

TASK:
Implement OperationAuditComplianceDecisionRecorder, register it from AddEncinaAudit, add
ComplianceApplicabilityStartupValidator to the Applicability package, and write the tests in the Phase 7 tasks.

KEY RULES:
- The record carries no subject id and no EncinaError.Message.
- A recorder Left makes the service enforce the capability (Design Choice 15); never swallow it.
- Start-up fails closed on a missing recorder; there is no opt-out.

REFERENCE FILES:
- src/Encina.Security.ABAC/DecisionAudit/AuditStoreABACDecisionRecorder.cs
- src/Encina.Security.Audit/ServiceCollectionExtensions.cs
- src/Encina.Security.BreakTheGlass/Services/BreakTheGlassStartupValidator.cs (start-up validator precedent)
```

</details>

---

### Phase 8: Observability and Health Check

> **Goal**: Every resolution and skip is traced, counted and logged without identifiers; the resolver is checked.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Compliance.Applicability/Diagnostics/ComplianceApplicabilityDiagnostics.cs`: `ActivitySource` and `Meter` named `Encina.Compliance.Applicability`; spans `encina.compliance.resolve` (tags `tenant.id`, `compliance.region`, `compliance.operation`, `compliance.resolver`, `compliance.profile.full`) and `encina.compliance.orchestrate` (`compliance.regulations`, `compliance.capabilities.count`); no subject id (REQ-062).
2. Metrics (the issue's names): `encina.compliance.resolution_count` (Counter, tags `resolver`, `outcome`), `encina.compliance.resolution_duration` (Histogram, ms), `encina.compliance.capability_applied` and `encina.compliance.enforcement_skipped` (Counter, tag `capability`), `encina.compliance.regulation_matched` (Counter, tag `regulation`); tenant id as a tag on all (REQ-062).
3. `Health/ComplianceApplicabilityHealthCheck.cs` (`DefaultName = "encina-compliance-applicability"`, `Tags = ["encina", "compliance", "applicability", "ready"]`, scoped resolution through `IServiceProvider.CreateScope()`): resolves `ComplianceContext.Empty`; `Unhealthy` if the result is not the full profile (Design Choice 5 invariant); `Degraded` when the failure counter passed `FailureDegradedThreshold` in the last window or no recorder is registered; registered when `AddHealthCheck` is true.
4. Log messages 8958 `HealthProbeFailed` (Warning) and any message added since Phase 3, packed without gaps.
5. Tests: activity and metric tests with `MeterListener`/`ActivityListener` (tags present, no subject id), health check states, guard tests.
6. Coverage manifest `Encina.Compliance.Applicability.json` for the new files.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```text
You are implementing Phase 8 of issue #686: tracing, metrics, logging and the health check of the Applicability package.

CONTEXT:
- Phases 1-7 are merged; EventIds 8950-8957 are used in ComplianceApplicability = (8950, 8999).
- SPEC-002 REQ-062: ActivitySource, Meter, [LoggerMessage] in a registered range, a health check, no direct subject
  identifiers, the tenant id as an attribute.

TASK:
Add ComplianceApplicabilityDiagnostics with the spans and the five metrics of the Phase 8 tasks, wire them into the
service and the orchestration behavior, add the health check, and test them.

KEY RULES:
- Health checks: DefaultName const, Tags static array, scoped resolution via IServiceProvider.CreateScope().
- No subject id, user id or EncinaError.Message in tags or logs.
- EventIds packed sequentially from 8958; no sparse allocation.

REFERENCE FILES:
- src/Encina.Compliance.Consent/Diagnostics/ (ConsentDiagnostics)
- src/Encina.Security.BreakTheGlass/Health/BreakTheGlassHealthCheck.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

### Phase 9: Testing

> **Goal**: Every flag reaches its target in the coverage manifests; the acceptance criteria are proven end to end.

<details>
<summary><strong>Tasks</strong></summary>

1. **Coverage manifests**: every phase already added its files' targets (AGENTS.md §9, #1762); this phase re-checks them with `--check-justifications` and raises a target where the new integration and contract tests reach more lines.
2. **UnitTests** (`tests/Encina.UnitTests/Compliance/Applicability/` and the module folders): completed in Phases 1-8; add the exhaustiveness test "every `ComplianceCapability` value has at least one consult point registered in `ComplianceAttributeRegistry`" after registering every module (Retention registers two; read auditing registers an attribute-less point).
3. **GuardTests** (`tests/Encina.GuardTests/Compliance/Applicability/`): completed per phase; this phase fills any gap the guard flag measurement shows.
4. **ContractTests** (`tests/Encina.ContractTests/Compliance/Applicability/`): abstract `ComplianceApplicabilityResolverContract` run against `AlwaysEnforce`, `RegionBased`, `TenantAware` and `Composite` (and reused by #687 and #688): empty context → full profile; never throws for any context; deterministic for equal contexts; a profile's capabilities are a subset of the enum. `IComplianceDecisionRecorder` contract against the operation-audit recorder with the InMemory store.
5. **PropertyTests** (`tests/Encina.PropertyTests/Compliance/Applicability/`, FsCheck through `Encina.Testing.FsCheck`): union is commutative, associative, idempotent and monotonic (a composite never applies fewer capabilities than any child); for any context with an EU/EEA tenant or subject country, `Gdpr` is in the regulations; for any context with any unknown field the region-based resolver returns `Full`.
6. **IntegrationTests** (`tests/Encina.IntegrationTests/Compliance/Applicability/`, `[Trait("Category", "Integration")]`): one end-to-end pipeline test with **every gated module** of Design Choice 8 registered (one request type per module), two tenants (`ES`, `BR` with `OperatorCountry = "BR"`), the composite resolver and the operation-audit recorder on PostgreSQL through the shared `[Collection("ADO-PostgreSQL")]` fixture, asserting enforcement for `ES`, one skip record per module for `BR`, full enforcement when the tenant store throws, and two subjects in one scope resolved separately. The gate itself has no provider-specific code, so one provider proves the recorder path (the recorder's store is already tested on the 10 providers).
7. **LoadTests**: `tests/Encina.LoadTests/Compliance/Applicability/ComplianceApplicability.md` justification (stateless in-memory resolution, scoped memo, no shared mutable state; concurrency covered by the memo unit tests).
8. **BenchmarkTests** (`tests/Encina.BenchmarkTests/Compliance/`): the hot path per the issue: core default (`AlwaysApplies`), memo hit, full resolution with the composite of region and tenant resolvers and two contributors; `BenchmarkSwitcher`, results under `artifacts/performance/`.
9. **CRAP ≤ 10** on every changed method, measured locally with the crap-gate table before review.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```text
You are implementing Phase 9 of issue #686: the tests across all types and the coverage manifests.

CONTEXT:
- Phases 1-8 are merged. Test projects: tests/Encina.{Unit,Guard,Contract,Property,Integration}Tests,
  tests/Encina.BenchmarkTests, and a .md justification for load tests (AGENTS.md section 9).
- Integration fixtures: shared [Collection("ADO-PostgreSQL")]; Marten tests use the existing Marten PostgreSQL fixture.

TASK:
Write the manifests and the tests listed in the Phase 9 tasks, then measure each flag:
dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage" --results-directory artifacts\coverage\<Flag>Tests,
dotnet run --file .github/scripts/coverage-report.cs -- --input artifacts/coverage --output artifacts/coverage-report,
dotnet run --file .github/scripts/coverage-report.cs -- --check-justifications.

KEY RULES:
- Tests execute real package code; no reflection-only tests.
- Shouldly through Encina.Testing.Shouldly; FsCheck through Encina.Testing.FsCheck; FakeTimeProvider for time.
- Never IClassFixture or a new database fixture; ClearAllDataAsync in InitializeAsync; never dispose the fixture.
- Benchmarks: BenchmarkSwitcher.FromAssembly, verify the filter with --list flat first.
- Every applicable flag reaches its manifest target; CRAP <= 10 on changed methods.

REFERENCE FILES:
- tests/Encina.ContractTests (an existing abstract contract suite)
- docs/testing/coverage-measurement-methodology.md
- docs/testing/integration-tests.md
- tests/Encina.BenchmarkTests/Compliance/
```

</details>

---

### Phase 10: Documentation and Finalization

> **Goal**: Every document the prompt requires; build and tests green.

<details>
<summary><strong>Tasks</strong></summary>

1. XML documentation on every new public API (`<summary>`, `<remarks>`, `<param>`, `<returns>`, `<example>`), citing GDPR Art. 3 and SPEC-002 REQ-019 where the semantics come from them.
2. `src/Encina.Compliance.Applicability/README.md` (what it is, registration, each resolver, the composite, contributors, the skip record, the territorial-scope principle and that use outside Spain and the EU is at the application's own risk); README notes in `Encina.Tenancy`, `Encina.Security.Audit` and the 14 gated compliance packages ("this module consults the applicability resolver for capability X").
3. `docs/features/compliance-applicability-resolver.md` (how-to, one Diátaxis quadrant, per the `encina-docs` skill) with a configuration example for each criterion type that is a territorial-scope test: region, tenant, subject, composite.
4. ADR: "Compliance applicability: modules ask one resolver; unknown means full; skips are recorded" with the next number that is neither used nor reserved in `docs/architecture/adr/index.md` (058 on 2026-10-28).
5. `changelog.d/686-compliance-applicability-resolver.added.md`.
6. `docs/INVENTORY.md` (new package), `ROADMAP.md` (v0.17.0), `docs/releases/` notes if the milestone has a release page.
7. `PublicAPI.Unshipped.txt` complete in every touched package.
8. `dotnet build Encina.slnx --configuration Release` → 0 errors, 0 warnings; `dotnet test` → all pass; every coverage flag at its manifest target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 10</strong></summary>

```text
You are implementing Phase 10 of issue #686: documentation and finalization of the compliance applicability resolver.

CONTEXT:
- Phases 1-9 are merged and green. Documentation rules: .claude/skills/encina-docs/SKILL.md (Diátaxis, cited
  figures, no hand-typed coverage numbers).

TASK:
Write the XML docs review, the package README and the README notes, the feature page, the ADR, the changelog
fragment and the INVENTORY/ROADMAP updates listed in the Phase 10 tasks; then run the Release build and the tests.

KEY RULES:
- English only; no emojis; no AI attribution.
- Never edit the [Unreleased] section of CHANGELOG.md; add changelog.d/686-compliance-applicability-resolver.added.md.
- Coverage figures are cited with covref markers, never typed.
- The ADR takes the next number that is neither used nor reserved in docs/architecture/adr/index.md.
- Build: zero warnings; tests: all pass; flags at target.

REFERENCE FILES:
- src/Encina.Security.ABAC/README.md
- docs/architecture/adr/index.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | What it asks of this feature |
|--------|-----------|------------------------------|
| GDPR Art. 2, 3 | Material and territorial scope; Art. 3(1) establishment, Art. 3(2) targeting or monitoring of subjects in the Union | The built-in mapping: GDPR applies on EU/EEA establishment or EU/EEA subject |
| GDPR Art. 5(2), 24 | Accountability; controller responsibility | Evidence of every skipped check (skip records) |
| GDPR Art. 25, 32 | Data protection by design and by default; security of processing | Default is full enforcement; `[CryptoShredded]` data is always encrypted (Design Choice 16) |
| GDPR Chapter V (Art. 44-49) | International transfers | CrossBorderTransfer is a GDPR capability (Design Choice 8) |
| LOPDGDD (LO 3/2018) art. 1-2 | Spanish adaptation of GDPR | Added when the tenant or subject country is `ES` |
| SPEC-002 REQ-011, AC-011 | S15 rules built on #686 and #687, deny when context is missing | The contract #2700 builds on; unknown context must not narrow |
| SPEC-002 REQ-019, INV-005, DEC-006 | Fail closed; any skip is an explicit, logged opt-out | Unknown → full; skip recorded or not taken; territorial assertions logged at start-up |
| SPEC-002 INV-002, INV-003 | Encina claims nothing for non-EU law; defaults follow the applicable law | No built-in CCPA or LGPD mapping; non-EU regulation ids are identifiers only |
| SPEC-002 REQ-061, AC-043 | Tenant-aware everywhere; fixed default tenant when tenancy is off | Tenant on every record, metric and span; two-tenant test |
| SPEC-002 REQ-062, AC-044 | Instrumentation without identifiers | ActivitySource, Meter, LoggerMessage in a registered range, health check, tenant tag, no subject id |
| SPEC-000 DEC-009 addendum (2026-10-23) | #686, #687, S15 rules, #688 in 1.0, v0.17.0, in that order | This plan fixes the contract the next three use |
| ISO 3166-1 alpha-2 | Country codes | Normalisation and validation of every country field |
| CCPA (Cal. Civ. Code §1798.100 et seq.), LGPD (Lei 13.709/2018) | Non-EU privacy laws named in the issue | Identifiers only (`MapCountry`); Encina claims nothing for them |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `IRequestContext`, `RequestIdentity`, `GetModuleName()` | `src/Encina/Abstractions/IRequestContext.cs:54`, `src/Encina/Identity/RequestIdentity.cs:47`, `src/Encina/Modules/RequestContextModuleExtensions.cs:37` | Tenant, correlation id, claims (subject country), module name |
| `ICommand` / `IQuery` markers | `src/Encina/Abstractions/CqrsContracts.cs:29,60` | `ComplianceOperation` of a request |
| Core contract + no-op default precedent | `src/Encina/Authorization/IAccessElevationGate.cs` and `Encina.Security.BreakTheGlass` | Same split for `IComplianceApplicability` |
| `TenantInfo`, `ITenantProvider`, `MultiTenancyMarker` | `src/Encina.Tenancy/Abstractions/TenantInfo.cs:56`, `ITenantProvider.cs:104`, `src/Encina/Abstractions/MultiTenancyMarker.cs:25` | Tenant contributor; tenancy-on detection |
| `IRegionContextProvider`, `RegionRegistry` | `src/Encina.Compliance.DataResidency/Abstractions/IRegionContextProvider.cs:62`, `Model/RegionRegistry.cs:55-146` | Region contributor; source of the EU/EEA list |
| `IDataSubjectIdExtractor`, `PersonalDataCategory` | `src/Encina.Compliance.DataSubjectRights/Abstractions/IDataSubjectIdExtractor.cs`, `Model/PersonalDataCategory.cs:17` | Subject contributor; data categories |
| 14 compliance pipeline behaviors | Summary table | Enforcement points that consult the resolver |
| `RetentionEnforcementService` | `src/Encina.Compliance.Retention/RetentionEnforcementService.cs:61,126` | Background consultation per record |
| `AuditedReadOnlyRepository` | `src/Encina.Security.Audit/AuditedReadOnlyRepository.cs:37,211` | Read-audit gating |
| `IOperationAuditStore`, decision-recorder precedent | `src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs:50`, `src/Encina.Security.ABAC/DecisionAudit/AuditStoreABACDecisionRecorder.cs:54` | Skip records |
| `IPolicyDecisionPoint` | `src/Encina.Security.ABAC/Abstractions/IPolicyDecisionPoint.cs:58` | Not used by the built-in resolvers (Design Choice 17); available for a custom resolver |
| `EventIdRanges` | `src/Encina/Diagnostics/EventIdRanges.cs:319` (compliance block) | New range 8950-8999 |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| **`Encina.Compliance.Applicability`** | **8950-8999** | **New (`ComplianceApplicability`), the free tail of the compliance block 8100-8999; 8950-8958 planned, packed. It is the last free slot of that block, so #687 and #688 take ranges from the "future compliance modules" block 9800-9999 (header `EventIdRanges.cs:385`)** |
| `Encina.Tenancy` | 1800-1899 | Existing; no new message (the contributor's failure is logged by the Applicability service) |
| `Encina.Security.Audit` | 5000-5099 | Existing; no new message planned |
| 14 compliance packages | Their existing ranges (8100-8949, 9200-9599) | No new message: skips are logged centrally |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Core contract (Phase 1) | ~19 | `src/Encina/Compliance/` (17), `AddEncina`, csproj grant, PublicAPI |
| New package (Phases 2, 3, 7, 8) | ~26 | csproj, options, validator, 3 resolvers, EEA list, builder, 2 contributors, service, memo, skip window, orchestration behavior, start-up validator, diagnostics, log messages, health check, DI, PublicAPI ×2 |
| Contributors and `TenantInfo` (Phase 3) | 6 | Tenancy (3), DataResidency (1), DSR (1), DI edits |
| Module integration (Phases 4a-5b) | ~30 modified | 14 behaviors, retention service, 14 registrations |
| Read audit (Phase 6) | ~3 modified | Repository decorator, DI, tests |
| Skip recorder (Phase 7) | 2 | Recorder, registration |
| Tests and manifests (all phases) | ~35 | Unit, guard, contract, property, integration (2), benchmark, load `.md`, manifest edits (19) |
| Documentation (Phase 10) | ~22 | Package README, 17 README notes, feature page, ADR, changelog fragment, INVENTORY/ROADMAP |
| **Total** | **~124 source, test and manifest files, plus ~22 documents** | Each phase stays near 10-25 files; Phases 2 and 3 are the largest |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #686: the Compliance Applicability Resolver (conditional enforcement of compliance
capabilities by jurisdiction, tenant and subject), the first of #686 -> #687 -> #2700 -> #688 in v0.17.0.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; ROP with Either<EncinaError, T>; pre-1.0 (no compatibility layers).
- 15 compliance packages enforce statically through pipeline behaviors; read auditing (Security.Audit) enforces
  outside the pipeline. Crypto-shredding (Marten.GDPR) always encrypts and is not gated.
- Fail closed (SPEC-002 DEC-006, INV-005, REQ-019; AGENTS.md section 3): unknown context never removes a check;
  any skip is explicit and recorded.
- Maintainer principle (2026-10-30): compliance with EU and Spanish law is never optional or commercial; only
  territorial non-applicability, justified, traced and audited, may disable a control. Designed for Spain and the EU.
- Provider category: none (no store).

IMPLEMENTATION OVERVIEW:
Phase 1: core contract in src/Encina/Compliance/ (resolver, profile, context, capability, regulation, operation,
         contributor, IComplianceApplicability, decision recorder, AlwaysEnforce resolver, zero-cost default).
Phase 2: new package Encina.Compliance.Applicability (references only Encina): options, validator, region-based,
         tenant-aware and composite resolvers, request and claim contributors, AddEncinaComplianceApplicability, EventIds 8950-8999.
Phase 3: resolution service with per-request memo and skip recording, ComplianceOrchestrationBehavior,
         TenantInfo (Country, ApplicableRegulations), contributors in Tenancy, DataResidency, DSR.
Phase 4a: GDPR, Consent, LawfulBasis, DSR consult it.
Phase 4b: DataResidency, CrossBorderTransfer, DPIA, ProcessorAgreements.
Phase 5a: Retention (behavior + enforcement service), Anonymization, BreachDetection.
Phase 5b: PrivacyByDesign, NIS2, AIAct.
Phase 6: read auditing.
Phase 7: operation-audit skip recorder, start-up validator, tenant and module propagation.
Phase 8: ActivitySource and Meter "Encina.Compliance.Applicability", five metrics, health check.
Phase 9: unit, guard, contract (reusable by #687/#688), property, integration (PostgreSQL), benchmark,
         load .md, coverage manifests.
Phase 10: XML docs, READMEs, feature page, ADR, changelog fragment, INVENTORY/ROADMAP; Release build clean.

KEY PATTERNS:
- Modules ask IComplianceApplicability.AppliesAsync after their Disabled check and attribute lookup, before any work.
- One resolution per request instance (scoped memo, never keyed by correlation id); requests without a governed
  attribute never resolve.
- Unknown, missing or failing context or resolver -> full profile; never a narrower one; resolvers never abstain.
- SubjectCountry only from [DataSubjectCountry] or a custom contributor; the caller's claim is CallerCountry.
- Every phase adds its files' coverage-manifest targets and guard tests.
- A skip is recorded (operation audit) before it happens; no recorder or a recorder Left -> enforce.
- The composite is the union only; no manual exemption, no per-capability modes, no unrecorded skip.
- No subject id, user id or EncinaError.Message in logs, tags or audit metadata; tenant id as a tag.
- TryAdd/TryAddEnumerable; every AddEncina* registers what it resolves (ValidateOnBuild + ValidateScopes tests).
- Read the plan's Maintainer Decisions section first: it records the decided options and the corrections.

REFERENCE FILES:
- docs/plans/compliance-applicability-resolver-implementation-plan-686.md (this plan)
- src/Encina/Abstractions/IRequestContext.cs, src/Encina/Authorization/IAccessElevationGate.cs
- src/Encina.Compliance.Consent/ConsentRequiredPipelineBehavior.cs (behavior pattern)
- src/Encina.Tenancy/Abstractions/TenantInfo.cs, src/Encina.Compliance.DataResidency/Model/RegionRegistry.cs
- src/Encina.Security.ABAC/DecisionAudit/AuditStoreABACDecisionRecorder.cs
- docs/specifications/SPEC-002-eu-regulatory-readiness.md (REQ-011, REQ-019, REQ-061, REQ-062)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ⏭️ Deferred | Per-request memo is included (Phase 3); a cross-request profile cache (by tenant and country, invalidated on tenant change) is deferred, as the issue says: follow-up file `artifacts/issues/plan-686-cross-request-profile-cache.md` |
| 2 | OpenTelemetry | ✅ Phase 8 | `ActivitySource` and `Meter` "Encina.Compliance.Applicability"; spans `encina.compliance.resolve` and `encina.compliance.orchestrate`; the five metrics of the issue with tenant, capability, regulation and resolver tags; no subject id (REQ-062) |
| 3 | Structured Logging | ✅ Phases 2-3, 8 | `[LoggerMessage]` EventIds 8950-8958 in the new `ComplianceApplicability` range 8950-8999, registered in `EventIdRanges.cs` first (ADR-021); skip, failure and start-up messages |
| 4 | Health Checks | ✅ Phase 8 | `ComplianceApplicabilityHealthCheck` resolves an empty context (must be full), reports repeated failures and a missing recorder |
| 5 | Validation | ✅ Phases 1-3 | Options validator with `ValidateOnStart` (one resolver, country codes, regulation ids); context builder normalises ISO 3166-1 codes; `TenancyOptions` tenant fields validated |
| 6 | Resilience | ❌ N/A | The built-in resolvers and contributors are in-memory; a failure widens to full enforcement (Design Choice 6). A remote resolver owns its retry and circuit breaker: OPA in #688 |
| 7 | Distributed Locks | ❌ N/A | Resolution reads only; no shared state is written except audit entries, which are appends |
| 8 | Transactions | ❌ N/A | No state change of its own; the skip record precedes the skip (write-ahead), and the operation-audit store may live in another database, so a shared transaction cannot be assumed |
| 9 | Idempotency | ❌ N/A | Resolution is deterministic and stateless; the memo guarantees one skip record per request and capability; no message entry point |
| 10 | Multi-Tenancy | ✅ Phases 3, 7 | Tenant contributor, tenant-aware resolver, tenant on every record, span and metric; tenancy on and no tenant → unknown → full (Design Choice 5); two-tenant test (AC-043) |
| 11 | Module Isolation | ✅ Phases 2, 7 | `ModuleName` from `GetModuleName()` in the context and the skip record, so a resolver can scope by module; module-scoped resolver configuration is left to #687's DSL |
| 12 | Audit Trail | ✅ Phase 7 | `compliance.capability_skipped` operation-audit entries, write-ahead, required before a skip (Design Choice 15) |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| `IRequestContext.Identity` and `TenantId` | Done (#1705 phases 1-3) | Claim contributor and tenant id; #1705 remains open for its later phases, which this plan does not need |
| `IOperationAuditStore` on the 10 providers | Done ([#1633](https://github.com/dlrivada/Encina/issues/1633) closed) | Sink of the skip records |
| The maintainer's answers to the Dilemmas | Done (2026-10-30) | Recorded in Maintainer Decisions |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Pipeline execution-order contract | [#2184](https://github.com/dlrivada/Encina/issues/2184) (open) | Design Choice 7 A is order-independent; once #2184 lands, the orchestration behavior takes the first compliance stage |
| Evidential read audit | [#1193](https://github.com/dlrivada/Encina/issues/1193) (open) | Its decorator gets the same `ReadAuditing` check (Phase 6) |
| Tenant filtering for Security.Audit | [#798](https://github.com/dlrivada/Encina/issues/798) (open) | Lets a tenant read its own skip records without cross-tenant leakage |
| Rule-based DSL, S15 rules, OPA resolver | [#687](https://github.com/dlrivada/Encina/issues/687), [#2700](https://github.com/dlrivada/Encina/issues/2700), [#688](https://github.com/dlrivada/Encina/issues/688) (open) | Consumers of this contract, in that order; the Phase 9 contract suite is reusable by #687 and #688; OPA runs locally (Docker image), so #688 needs no cloud account |
| Feature flags | [#345](https://github.com/dlrivada/Encina/issues/345) (open) | Complementary; a flag-backed resolver can be a custom resolver |
| Cross-request profile cache | `artifacts/issues/plan-686-cross-request-profile-cache.md` (draft) | Deferred caching integration |
| Enforcement mode and registration consistency | `artifacts/issues/plan-686-enforcement-mode-registration-consistency.md` (draft) | GDPR has no `Disabled`, PrivacyByDesign numbers its modes in reverse, LawfulBasis and CrossBorderTransfer register with `AddTransient` instead of `TryAddEnumerable` |
| Single source for the EU/EEA country list | `artifacts/issues/plan-686-single-source-eea-country-list.md` (draft) | The Applicability package copies `RegionRegistry`'s list (Design Choice 14) |

---

## Next Steps

1. The register was screened independently on 2026-10-29 and the five open Dilemmas (3, 8, 9, 11, 16) were decided on 2026-10-30; the answers are in `Maintainer Decisions`. When #686 is picked up, the orchestrator re-checks each Design Choice against the code once more.
2. Open the three follow-up issues drafted under `artifacts/issues/` and link them from this plan.
3. Link this plan from #686 and from #687, #2700 and #688, whose plans build on the contract of Phase 1; update the sketches in the #687 and #688 bodies (they use `ComplianceCapability.DataSubjectRights`, `ComplianceCapability.DPIA`, `ComplianceProfile.FullEnforcement()` and boolean profile properties) to the names this plan fixes (Design Choice 3 is decided), and remove their per-capability modes and `FirstDecisive`-style overrides, which Design Choices 9 and 11 exclude. Record in a comment on #686 that the acceptance criteria "orchestration behavior coordinates ALL modules / replaces the individual behaviors", "Update DSR handler to check `DataSubjectRightsApply`" and the crypto-shredding gating are superseded (Design Choices 7, 8 and 16), and that the `ctx.Region` and `ctx.Tenant` typed members of the #687 and #688 sketches become the primitive context fields of Design Choice 2.
4. Phases 1-3 can ship before the module integration; each phase is one PR referencing #686, and the last one carries `Fixes #686`.
5. Take the next free ADR number at Phase 10 (058 on 2026-10-28) unless it is reserved for this plan earlier.

---

## Maintainer Decisions

Decisions of the maintainer on the Design Choices, recorded 2026-10-30 in [the comment on #686](https://github.com/dlrivada/Encina/issues/686#issuecomment-6521534413) and confirmed in [its follow-up](https://github.com/dlrivada/Encina/issues/686#issuecomment-6522970662). The independent screen of 2026-10-29 settled Design Choices 1, 6, 7, 12, 15, 17 and 20 and verified the other eight Settled or Trivial entries; five Dilemmas remained and are decided here. The principle behind them: compliance with EU and Spanish law is never optional or commercial; only territorial non-applicability may disable a control, and only when the skip is justified, traced and audited and opens no gap that lacks an alternative. Encina is designed for Spain and the EU; use outside that frame is not foreseen and is at the application's own risk.

- **Design Choice 3** (2026-10-30): B. A capability set (closed enum) plus open regulation identifiers. Non-EU regulation ids are identifiers only; Encina claims nothing for them (SPEC-002 INV-002). Source: [comment 6521534413](https://github.com/dlrivada/Encina/issues/686#issuecomment-6521534413).
- **Design Choice 8** (2026-10-30): B. Every compliance gate consults the resolver, including CrossBorderTransfer, ProcessorAgreements, PrivacyByDesign, NIS2 and AIAct; Attestation never; obligation-fulfilment services never (DSR service, breach notification service and deadline monitor, DPIA reminders, consent recording). The issue task "Update DSR handler to check `DataSubjectRightsApply`" is superseded: the DSR and breach rows are met by their enforcement behaviors. Source: [comment 6521534413](https://github.com/dlrivada/Encina/issues/686#issuecomment-6521534413).
- **Design Choice 9** (2026-10-30): binary. A capability applies, or it does not apply by territory and every skip is recorded; no `WarnOnly` and no per-capability modes for in-scope subjects. Source: [comment 6521534413](https://github.com/dlrivada/Encina/issues/686#issuecomment-6521534413).
- **Design Choice 11** (2026-10-30): union only. The legal floor is always kept; no `FirstDecisive` and no manual exemption. Source: [comment 6521534413](https://github.com/dlrivada/Encina/issues/686#issuecomment-6521534413).
- **Design Choice 16** (2026-10-30): always encrypt `[CryptoShredded]` data. No clear-text envelope (`csc1:`), no gating of crypto-shredding, ADR-034 unchanged. Source: [comment 6521534413](https://github.com/dlrivada/Encina/issues/686#issuecomment-6521534413).
- **Design Choice 12** (2026-10-30), correction: `SubscriptionTier` is dropped as a resolver input and as a `TenantInfo` field (a commercial tier never decides compliance); `Country` and `ApplicableRegulations` stay. Source: [comment 6521534413](https://github.com/dlrivada/Encina/issues/686#issuecomment-6521534413).
- **Design Choice 14** (2026-10-30), correction: an unset `OperatorCountry` means the full profile (fail closed, AGENTS.md §3); the "narrows" behaviour for that case is removed. The issue's free-tier scenarios ("no consent tracking or DSR", "retention only") are removed from the plan; `None()` is used only for "law not applicable" (territorial scope). Source: [comment 6521534413](https://github.com/dlrivada/Encina/issues/686#issuecomment-6521534413).
- **Design Choice 15** (2026-10-30), correction: `AllowUnrecordedSkips` is dropped; an unrecorded skip is not allowed, so no recorder or a recorder `Left` means the capability is enforced and start-up fails without a recorder. Source: [comment 6521534413](https://github.com/dlrivada/Encina/issues/686#issuecomment-6521534413).
