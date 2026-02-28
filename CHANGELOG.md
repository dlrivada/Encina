## [Unreleased] - v0.13.0 - Security & Compliance

### Added

#### Encina.Security — Core Security Abstractions and Pipeline Behavior (#394)

Added the `Encina.Security` package providing attribute-based, transport-agnostic security at the CQRS pipeline level. Operates independently of ASP.NET Core, ensuring consistent authorization enforcement across HTTP, messaging, gRPC, and serverless transports.

**Core Abstractions**:

- **`ISecurityContext`**: Immutable security context carrying identity, roles, permissions, and tenant info
- **`ISecurityContextAccessor`**: Request-scoped accessor using `AsyncLocal<T>` for async flow
- **`IPermissionEvaluator`**: Extensible permission evaluation (default: in-memory set lookup)
- **`IResourceOwnershipEvaluator`**: Resource ownership verification (default: cached reflection)
- **`SecurityContext`**: Claims-based implementation with configurable claim type extraction
- **`SecurityOptions`**: Configuration for claim types, default auth policy, health check opt-in

**Seven Security Attributes** (declarative, composable):

- **`[AllowAnonymous]`**: Bypasses all security checks (pipeline short-circuit)
- **`[DenyAnonymous]`**: Requires authenticated identity
- **`[RequireRole("Admin", "Manager")]`**: OR-based role check
- **`[RequireAllRoles("Admin", "Auditor")]`**: AND-based role check
- **`[RequirePermission("orders:read")]`**: Permission check via `IPermissionEvaluator` (supports OR/AND via `RequireAll`)
- **`[RequireClaim("department", "finance")]`**: Claim existence/value check
- **`[RequireOwnership("OwnerId")]`**: Resource ownership via `IResourceOwnershipEvaluator`

**Pipeline Behavior**:

- **`SecurityPipelineBehavior<TRequest, TResponse>`**: Evaluates security attributes in priority order, short-circuits on first failure with `EncinaError` (Railway Oriented Programming)
- Configurable evaluation order via `SecurityAttribute.Order` property
- `RequireAuthenticatedByDefault` option for global authentication enforcement
- `ThrowOnMissingSecurityContext` option for strict context validation

**Error Codes** (6 structured errors via `SecurityErrors`):

- `security.unauthenticated`, `security.insufficient_roles`, `security.permission_denied`
- `security.claim_missing`, `security.not_owner`, `security.missing_context`
- All errors include structured metadata (`requestType`, `stage`, `userId`, `requirement`)

**Observability**:

- OpenTelemetry tracing via `Encina.Security` ActivitySource with tags: `security.request_type`, `security.user_id`, `security.outcome`, `security.denial_reason`
- 4 metric instruments under `Encina.Security` Meter: `security.authorization.total`, `.allowed`, `.denied` (counters), `.duration` (histogram in ms)
- 5 structured log events (EventId 8000–8004) using `LoggerMessage.Define` for zero-allocation logging

**Health Check**:

- **`SecurityHealthCheck`**: Verifies all security services are registered and resolvable from DI
- Opt-in via `SecurityOptions.AddHealthCheck = true`
- Tags: `encina`, `security`, `ready`

**DI Registration**:

- `services.AddEncinaSecurity()` with `TryAdd` semantics — register custom evaluators before calling to override defaults
- Configurable via `Action<SecurityOptions>` delegate

**Testing**: 420 unit tests across 6 test classes covering all attributes, pipeline behavior, evaluators, DI registration, and observability (tracing, metrics, logging, health checks).

---

#### Encina.Compliance.GDPR — Core GDPR Abstractions and RoPA (#402)

Added the `Encina.Compliance.GDPR` package providing declarative, attribute-based GDPR compliance at the CQRS pipeline level. Implements Articles 5, 6, 30, 32, and 37-39 with automatic Record of Processing Activities (RoPA) generation.

**Core Abstractions**:

- **`IProcessingActivityRegistry`**: Central registry for GDPR Article 30 processing activities with `Option<ProcessingActivity>` return type
- **`IGDPRComplianceValidator`**: Extensible compliance validation (default: `DefaultGDPRComplianceValidator`)
- **`IDataProtectionOfficer`**: DPO contact information (Articles 37-39)
- **`ProcessingActivity`**: Immutable record with purpose, lawful basis, data categories, subjects, retention, security measures
- **`ComplianceResult`**: Compliant / CompliantWithWarnings / NonCompliant states
- **`LawfulBasis`**: Six GDPR Article 6(1) lawful bases enum

**Two Declarative Attributes**:

- **`[ProcessingActivity(...)]`**: Full Article 30 declaration with purpose, lawful basis, data categories, subjects, retention, security measures
- **`[ProcessesPersonalData]`**: Lightweight marker indicating personal data processing (requires registry entry)

**Pipeline Behavior**:

- **`GDPRCompliancePipelineBehavior<TRequest, TResponse>`**: Validates processing activities at pipeline level
- Two enforcement modes: `Enforce` (block non-compliant) and `WarnOnly` (log and proceed)
- `BlockUnregisteredProcessing` option for strict registry enforcement
- Requests without GDPR attributes bypass all checks (zero overhead)

**RoPA Export** (Article 30 compliance):

- **`JsonRoPAExporter`**: Export RoPA as structured JSON
- **`CsvRoPAExporter`**: Export RoPA as CSV for regulatory submission
- **`RoPAExportMetadata`**: Controller info, DPO, export timestamp
- **`RoPAExportResult`**: Export content with byte array, content type, activity count

**Auto-Registration**:

- Scan assemblies for `[ProcessingActivity]` attributes at startup via `IHostedService`
- Configurable via `GDPROptions.AssembliesToScan` and `GDPROptions.AutoRegisterFromAttributes`
- Idempotent: duplicate registrations are skipped

**Error Codes** (4 structured errors via `GDPRErrors`):

- `gdpr.unregistered_activity`, `gdpr.compliance_validation_failed`
- `gdpr.registry_lookup_failed`, `gdpr.ropa_export_serialization_failed`

**Observability**:

- OpenTelemetry tracing via `Encina.Compliance.GDPR` ActivitySource with lawful basis tags
- Structured log events using `LoggerMessage.Define` (zero-allocation)
- Optional health check verifying registry population and DI configuration

**DI Registration**:

- `services.AddEncinaGDPR()` with `TryAdd` semantics — register custom implementations before calling to override defaults

**Testing**: 135 tests across 4 test projects (100 unit, 11 guard, 8 property, 16 contract).

---

#### Encina.Compliance.Consent — Consent Management with GDPR Article 7 Compliance (#403)

Added the `Encina.Compliance.Consent` package providing declarative, attribute-based consent management at the CQRS pipeline level. Implements GDPR Articles 6(1)(a), 7, and 8 with full consent lifecycle management, version tracking, audit trail, and domain event publishing.

**Core Abstractions**:

- **`IConsentStore`**: Full consent lifecycle with `RecordConsentAsync`, `GetConsentAsync`, `GetAllConsentsAsync`, `WithdrawConsentAsync`, `HasValidConsentAsync`, `BulkRecordConsentAsync`, `BulkWithdrawConsentAsync`
- **`IConsentValidator`**: Consent validation with `ValidateAsync(subjectId, requiredPurposes)` returning `ConsentValidationResult`
- **`IConsentVersionManager`**: Version management with `GetCurrentVersionAsync`, `PublishNewVersionAsync`, `RequiresReconsentAsync`
- **`IConsentAuditStore`**: Immutable audit trail with `RecordAsync` and `GetAuditTrailAsync`
- **`ConsentRecord`**: Sealed record with Id, SubjectId, Purpose, Status, ConsentVersionId, GivenAtUtc, WithdrawnAtUtc, ExpiresAtUtc, Source, IpAddress, ProofOfConsent, Metadata
- **`ConsentVersion`**: Sealed record with VersionId, Purpose, EffectiveFromUtc, Description, RequiresExplicitReconsent
- **`ConsentValidationResult`**: Valid / ValidWithWarnings / Invalid states with missing purposes and errors

**Declarative Attribute**:

- **`[RequireConsent("marketing", SubjectIdProperty = "UserId")]`**: Attribute-based consent requirement with purpose(s), subject ID extraction via cached reflection, and custom error messages

**Pipeline Behavior**:

- **`ConsentRequiredPipelineBehavior<TRequest, TResponse>`**: Validates consent at pipeline level with attribute caching, property reflection caching, and OpenTelemetry tracing
- Three enforcement modes: `Block` (reject non-compliant), `Warn` (log and proceed), `Disabled` (no-op)
- Requests without `[RequireConsent]` bypass all checks (zero overhead)

**Domain Events** (4 notification types):

- **`ConsentGrantedEvent`**: Published when consent is recorded
- **`ConsentWithdrawnEvent`**: Published when consent is withdrawn (Article 7(3))
- **`ConsentExpiredEvent`**: Published when expired consent is detected
- **`ConsentVersionChangedEvent`**: Published when a new consent version is published

**In-Memory Implementations** (development/testing):

- **`InMemoryConsentStore`**: ConcurrentDictionary-based with automatic expiration detection
- **`InMemoryConsentAuditStore`**: Thread-safe audit trail with time-ordered entries
- **`InMemoryConsentVersionManager`**: ConcurrentDictionary-based version tracking

**Standard Consent Purposes** (8 pre-defined constants in `ConsentPurposes`):

- `Marketing`, `Analytics`, `Personalization`, `ThirdPartySharing`, `Profiling`, `Newsletter`, `LocationTracking`, `CrossBorderTransfer`

**Error Codes** (5 structured errors via `ConsentErrors`):

- `consent.missing`, `consent.withdrawn`, `consent.expired`, `consent.requires_reconsent`, `consent.version_mismatch`
- All errors include structured metadata (`subjectId`, `purpose`, timestamps)

**Configuration** (`ConsentOptions`):

- `EnforcementMode` (Block/Warn/Disabled), `DefaultExpirationDays`, `RequireExplicitConsent`
- `AutoRegisterFromAttributes` with assembly scanning
- `DefinePurpose()` fluent API with per-purpose `DefaultExpirationDays`, `RequiresExplicitOptIn`, `CanBeWithdrawnAnytime`
- `FailOnUnknownPurpose`, `AllowGranularWithdrawal`, `TrackConsentProof`
- Options validation via `IValidateOptions<ConsentOptions>`

**Auto-Registration**:

- Scan assemblies for `[RequireConsent]` attributes at startup via `IHostedService`
- Registers discovered purposes automatically into `ConsentOptions.PurposeDefinitions`

**Observability**:

- OpenTelemetry tracing via `Encina.Compliance.Consent` ActivitySource with consent-specific tags
- 6 structured log events using `LoggerMessage.Define` (zero-allocation)
- Optional health check (`ConsentHealthCheck`) verifying store connectivity and DI configuration

**Bulk Operations**:

- `BulkRecordConsentAsync`: Batch consent recording with per-item error tracking
- `BulkWithdrawConsentAsync`: Batch withdrawal for single subject across multiple purposes
- `BulkOperationResult`: Success/Partial results with `SuccessCount`, `FailureCount`, `Errors`

**DI Registration**:

- `services.AddEncinaConsent()` with `TryAdd` semantics — register custom stores before calling to override defaults
- Configurable via `Action<ConsentOptions>` delegate

**Database Provider Implementations** (13 providers):

- ADO.NET: SQLite, SQL Server, PostgreSQL, MySQL (`ConsentStoreADO`, `ConsentAuditStoreADO`, `ConsentVersionManagerADO`)
- Dapper: SQLite, SQL Server, PostgreSQL, MySQL (`ConsentStoreDapper`, `ConsentAuditStoreDapper`, `ConsentVersionManagerDapper`)
- EF Core: SQLite, SQL Server, PostgreSQL, MySQL (`ConsentStoreEF`, `ConsentAuditStoreEF`, `ConsentVersionManagerEF`)
- MongoDB: `ConsentStoreMongo`, `ConsentAuditStoreMongo`, `ConsentVersionManagerMongo`

**Testing**: 1,100+ tests across 7 test projects — unit tests, guard tests, property tests, contract tests, integration tests (178 passing across 13 providers), load tests (7 concurrent scenarios), benchmarks (13 BenchmarkDotNet scenarios).

---

#### Encina.Compliance.GDPR — Lawful Basis Tracking and Validation (Art. 6) (#413)

Added GDPR Article 6(1) lawful basis enforcement to the compliance pipeline. Provides declarative, attribute-based lawful basis declarations with runtime validation, Legitimate Interest Assessment (LIA) management, consent integration, and multi-provider persistence across all 13 database providers.

**Core Abstractions**:

- **`ILawfulBasisRegistry`**: Central registry linking request types to lawful bases with `RegisterAsync`, `GetByRequestTypeAsync`, `GetByRequestTypeNameAsync`, `GetAllAsync`
- **`ILawfulBasisProvider`**: Resolves and validates lawful bases with `GetBasisForRequestAsync`, `ValidateBasisAsync<TRequest>`
- **`ILIAStore`**: Legitimate Interest Assessment storage with `StoreAsync`, `GetByReferenceAsync`, `GetPendingReviewAsync`
- **`ILegitimateInterestAssessment`**: LIA validation via `ValidateAsync(liaReference)` returning `LIAValidationResult`
- **`IConsentStatusProvider`**: Bridge interface for consent-based processing validation
- **`ILawfulBasisSubjectIdExtractor`**: Subject ID extraction for consent validation

**Declarative Attributes**:

- **`[LawfulBasis(LawfulBasis.Consent, Purpose = "...")]`**: Declares the lawful basis for a request type with optional `Purpose`, `LIAReference`, `LegalReference`, `ContractReference`
- Works alongside `[ProcessingActivity]` with automatic conflict detection and LawfulBasisAttribute priority

**Pipeline Behavior**:

- **`LawfulBasisValidationPipelineBehavior<TRequest, TResponse>`**: Validates lawful basis at pipeline level
- Three enforcement modes: `Block` (reject), `Warn` (log and proceed), `Disabled` (no-op)
- Consent validation via `IConsentStatusProvider` for `LawfulBasis.Consent`
- LIA approval validation via `ILegitimateInterestAssessment` for `LawfulBasis.LegitimateInterests`
- Attribute-conflict detection when both `[LawfulBasis]` and `[ProcessingActivity]` declare different bases
- Static per-generic-type attribute caching (zero reflection after first access)
- Registry fallback for programmatically registered bases

**Legitimate Interest Assessment (LIA)**:

- **`LIARecord`**: Full EDPB three-part test model with Purpose, Necessity, and Balancing assessments
- 20 documented fields covering legitimate interest, benefits, necessity justification, alternatives, data minimisation, impact assessment, safeguards, DPO involvement
- **`LIAOutcome`**: Three states — `Approved`, `Rejected`, `RequiresReview`
- **`LIAValidationResult`**: Four factory methods — `Approved()`, `Rejected(reason)`, `PendingReview()`, `NotFound()`

**Multi-Provider Persistence** (13 providers):

- ADO.NET: `LawfulBasisRegistryADO{Sqlite,SqlServer,PostgreSQL,MySQL}`, `LIAStoreADO{...}`
- Dapper: `LawfulBasisRegistryDapper{Sqlite,SqlServer,PostgreSQL,MySQL}`, `LIAStoreDapper{...}`
- EF Core: `LawfulBasisRegistryEF`, `LIAStoreEF` (with entity configurations)
- MongoDB: `LawfulBasisRegistryMongo`, `LIAStoreMongo`
- All stores support upsert semantics for idempotent operations

**Error Codes** (7 structured errors via `GDPRErrors`):

- `gdpr.lawful_basis_not_declared`, `gdpr.consent_not_found`, `gdpr.lia_not_found`
- `gdpr.lia_not_approved`, `gdpr.consent_provider_not_registered`
- `gdpr.lawful_basis_store_error`, `gdpr.lia_store_error`

**Observability**:

- OpenTelemetry tracing via `Encina.Compliance.GDPR.LawfulBasis` ActivitySource with tags: `request.type`, `lawful_basis.declared`, `lawful_basis.valid`, `basis`, `outcome`, `failure_reason`
- 3 metric counters: `lawful_basis_validations_total`, `lawful_basis_consent_checks_total`, `lawful_basis_lia_checks_total`
- 17 structured log events (EventId 8200–8216)
- Optional `LawfulBasisHealthCheck` verifying registry and LIA store access

**DI Registration**:

- `services.AddEncinaLawfulBasis()` with `LawfulBasisOptions` configuration
- Provider-specific: `AddEncinaLawfulBasisADO{Sqlite,SqlServer,PostgreSQL,MySQL}(connectionString)`
- Provider-specific: `AddEncinaLawfulBasisDapper{...}(connectionString)`
- Provider-specific: `AddEncinaLawfulBasisEFCore()`
- Provider-specific: `AddEncinaLawfulBasisMongoDB(connectionString, databaseName)`

**Testing**: 284 tests across 7 test projects plus 18 benchmarks and 8 load test scenarios — 70 unit tests, 137 integration tests (13 providers), 19 guard tests, 17 property tests, 26 contract tests, 15 integration test justification documents, 18 BenchmarkDotNet benchmarks (11 store + 7 pipeline), 8 load test scenarios (50 concurrent workers × 10K operations each).

---

#### Encina.Compliance.GDPR — ProcessingActivity Registry for All 13 Database Providers (#681)

Added multi-provider persistence for `IProcessingActivityRegistry` (GDPR Article 30 Record of Processing Activities) across all 13 database providers. Previously only available via `InMemoryProcessingActivityRegistry`, the registry can now persist processing activities in production databases.

**Infrastructure**:

- **`ProcessingActivityEntity`**: Flat persistence entity with 14 primitive fields, JSON-serialized collections, and `RetentionPeriodTicks` (long) for lossless `TimeSpan` storage
- **`ProcessingActivityMapper`**: Static `ToEntity`/`ToDomain` mapping with `System.Text.Json` camelCase serialization, `Type.GetType` resolution for `RequestTypeName`, and null-safe round-trips
- **SQL migration scripts** (`011_CreateProcessingActivitiesTable.sql`): Provider-specific DDL for SQLite, SQL Server, PostgreSQL, and MySQL with UNIQUE constraint on `RequestTypeName`

**Multi-Provider Persistence** (13 providers):

- ADO.NET: `ProcessingActivityRegistryADO` for SQLite, SQL Server, PostgreSQL, MySQL — INSERT-only registration with provider-specific duplicate detection (SQLite error code 19, SQL Server 2627/2601, PostgreSQL "23505", MySQL 1062)
- Dapper: `ProcessingActivityRegistryDapper` for SQLite, SQL Server, PostgreSQL, MySQL — same semantics with Dapper parameterized queries
- EF Core: `ProcessingActivityRegistryEF` with `ProcessingActivityEntityConfiguration` and `ProcessingActivityModelBuilderExtensions` — shared implementation across SQLite, SQL Server, PostgreSQL, MySQL via `DbUpdateException` duplicate key detection
- MongoDB: `ProcessingActivityRegistryMongoDB` with `ProcessingActivityDocument` — `MongoWriteException` `DuplicateKey` detection, unique index on `RequestTypeName`

**Error Codes** (3 new structured errors via `GDPRErrors`):

- `gdpr.processing_activity_store_error` — general store operation failure
- `gdpr.processing_activity_duplicate` — duplicate `RequestType` registration attempt
- `gdpr.processing_activity_not_found` — update on non-existing activity

**DI Registration**:

- All ADO.NET providers: `AddEncinaProcessingActivityADO{Sqlite,SqlServer,PostgreSQL,MySQL}(connectionString)`
- All Dapper providers: `AddEncinaProcessingActivityDapper{Sqlite,SqlServer,PostgreSQL,MySQL}(connectionString)`
- EF Core: `AddEncinaProcessingActivityEFCore()`
- MongoDB: `AddEncinaProcessingActivityMongoDB(connectionString, databaseName)`

**Observability**:

- **`ProcessingActivityDiagnostics`**: Dedicated ActivitySource (`Encina.Compliance.GDPR.ProcessingActivity`) with 4 activity types: Register, Update, GetByRequestType, GetAll
- 2 counters via shared `GDPRDiagnostics.Meter`: `processing_activity_operations_total`, `processing_activity_operations_failed_total`
- Tag constants: `operation`, `outcome`, `request.type`, `activity.count`, `failure_reason`
- Helper methods: `RecordSuccess`/`RecordFailure` with null-safe activity handling

**Testing**: 42 new tests — 30 unit tests (16 mapper + 14 diagnostics), 2 guard tests, 4 property tests (FsCheck mapper round-trips), plus 91 integration tests across 13 providers (7 tests × 13 providers).

---

#### Encina.Compliance.DataSubjectRights — GDPR Data Subject Rights Management (Arts. 15-22) (#404)

Added the `Encina.Compliance.DataSubjectRights` package providing comprehensive GDPR Data Subject Rights management covering Articles 15-22. Includes full request lifecycle tracking with 30-day SLA compliance, automated data erasure with legal retention exemptions (Art. 17(3)), data portability export (Art. 20), processing restriction enforcement via pipeline behavior (Art. 18), and immutable audit trail for compliance evidence.

**Core Abstractions**:

- **`IDataSubjectRightsHandler`**: Full DSR lifecycle orchestration — submit, track, update, query pending/overdue requests
- **`IDSRRequestStore`**: Request persistence with CRUD, subject-based queries, and active restriction detection
- **`IDSRAuditStore`**: Immutable audit trail with chronological entry recording
- **`IPersonalDataLocator`**: Discovers all personal data fields for a subject across the application
- **`IDataErasureExecutor`**: Orchestrates field-level erasure with legal retention exemptions
- **`IDataErasureStrategy`**: Per-field erasure customization (anonymize, nullify, pseudonymize)
- **`IDataPortabilityExporter`**: Data export with portable-only field filtering
- **`IExportFormatWriter`**: Pluggable format writers for JSON, CSV, XML output

**Domain Model**:

- **`DSRRequest`**: Sealed record with 30-day deadline calculation, extension support, identity verification tracking
- **`DSRRequestStatus`**: 7-state lifecycle — Received, IdentityVerified, InProgress, Completed, Rejected, Extended, Expired
- **`DataSubjectRight`**: 8 GDPR rights — Access, Rectification, Erasure, Restriction, Portability, Objection, AutomatedDecisionMaking, Notification
- **`[PersonalData]`**: Property-level attribute with Category, IsErasable, IsPortable, HasLegalRetention, ErasureMethod
- **`PersonalDataCategory`**: 16 categories including Identity, Contact, Financial, Health, Biometric, Location, Genetic, Political
- **`ErasureScope`/`ErasureResult`**: Detailed per-field erasure tracking with exemption documentation

**Pipeline Behavior**:

- **`ProcessingRestrictionPipelineBehavior<TRequest, TResponse>`**: Enforces Art. 18 processing restrictions in the CQRS pipeline
- 3 enforcement modes: `Block` (reject with error), `Warn` (log + proceed), `Disabled` (no-op)
- Cached `[ProcessesPersonalData]` attribute detection per generic type

**Default Implementations**:

- `DefaultDataSubjectRightsHandler`: Full lifecycle orchestration with deadline enforcement and audit trail
- `DefaultDataErasureExecutor`: Erasure with legal retention exemptions and field-level result tracking
- `DefaultDataPortabilityExporter`: Portable-only field filtering with format writer resolution
- `InMemoryDSRRequestStore`: Thread-safe `ConcurrentDictionary` implementation
- `InMemoryDSRAuditStore`: Thread-safe `ConcurrentDictionary` implementation

**Observability**:

- OpenTelemetry tracing via `Encina.Compliance.DataSubjectRights` ActivitySource
- 3 counters: `dsr.requests.submitted`, `dsr.requests.completed`, `dsr.requests.overdue`
- 2 histograms: `dsr.request.duration`, `dsr.erasure.duration`
- 14 structured log events using `LoggerMessage.Define` (zero-allocation, event IDs 8300-8399)
- `DataSubjectRightsHealthCheck` with overdue request monitoring (Unhealthy/Degraded/Healthy)

**DI Registration**:

- `services.AddEncinaDataSubjectRights()` with `TryAdd` semantics and `Action<DataSubjectRightsOptions>` configuration

**Testing**: 228 tests across 6 test projects — 125 unit tests, 42 guard tests, 17 contract tests, 10 property tests (FsCheck invariants), 17 integration tests (DI registration, full lifecycle, audit trail, health check, concurrent access), plus 8 load test methods (50 concurrent workers × 10K operations each), and 15 BenchmarkDotNet benchmarks (12 request store, 3 audit store — with 10/100/1000 pre-seeded records).

**Note**: Database provider implementations (13 providers) are planned for future phases. Currently ships with InMemory stores only.

---

#### Encina.Compliance.Anonymization — Data Anonymization, Pseudonymization, and Tokenization (#407)

Added the `Encina.Compliance.Anonymization` package providing comprehensive data anonymization capabilities at the CQRS pipeline level. Implements GDPR Article 4(5) pseudonymization, five anonymization techniques, reversible tokenization with encrypted token mapping stores, and statistical risk assessment (k-anonymity, l-diversity, t-closeness).

**Core Abstractions**:

- **`IAnonymizer`**: Data anonymization with technique-based field processing, field-level anonymization rules, and anonymization detection heuristics
- **`IPseudonymizer`**: Reversible pseudonymization using AES-256-GCM encryption and HMAC-SHA256 deterministic hashing
- **`ITokenizer`**: Token-based data protection with encrypted original value storage, deduplication via HMAC hash, and three token formats (UUID, Prefixed, FormatPreserving)
- **`IRiskAssessor`**: Statistical re-identification risk assessment computing k-anonymity, l-diversity, and t-closeness metrics with configurable acceptance targets

**Five Anonymization Techniques** (implementing `IAnonymizationTechnique`):

- **`GeneralizationTechnique`**: Numeric range bucketing (e.g., age 25 → "20-29"), configurable bucket sizes
- **`SuppressionTechnique`**: Complete value removal (returns null/default for all types)
- **`PerturbationTechnique`**: Random noise injection with configurable percentage (default 10%)
- **`DataMaskingTechnique`**: Character masking preserving first character (e.g., "John" → "J***")
- **`SwappingTechnique`**: Value swapping within a dataset for relational anonymity

**Domain Model**:

- **`TokenMapping`**: Sealed record with Id, Token, OriginalValueHash, EncryptedOriginalValue, KeyId, CreatedAtUtc, ExpiresAtUtc
- **`AnonymizationProfile`**: Named profile with field-level anonymization rules
- **`AnonymizationAuditEntry`**: Audit trail for anonymization operations with subject ID and operation type
- **`RiskAssessmentResult`**: Statistical metrics with KAnonymityValue, LDiversityValue, TClosenessDistance, ReIdentificationProbability, IsAcceptable, Recommendations
- **`AnonymizationOptions`**: Configuration for enforcement mode, audit trail opt-in, health check, assembly scanning
- **`TokenizationOptions`**: Token format (UUID/Prefixed/FormatPreserving), prefix, preserve-length settings
- **`FieldAnonymizationRule`**: Per-field technique assignment with optional parameters
- **`AnonymizationTechnique`**: Enum with 8 technique types (Generalization, Suppression, Perturbation, Swapping, DataMasking, KAnonymity, LDiversity, TCloseness)

**Pipeline Behavior**:

- **`AnonymizationPipelineBehavior<TRequest, TResponse>`**: Applies anonymization at the CQRS pipeline level with configurable enforcement mode
- Constructor takes 6 parameters: techniques, pseudonymizer, tokenizer, keyProvider, options, logger

**In-Memory Implementations** (development/testing):

- **`InMemoryTokenMappingStore`**: ConcurrentDictionary-based with triple-index lookups (by ID, by token, by hash)
- **`InMemoryKeyProvider`**: Thread-safe key management with generation, rotation, and retrieval
- **`InMemoryAnonymizationAuditStore`**: Thread-safe audit trail with subject-based queries

**Error Codes** (14 factory methods via `AnonymizationErrors`):

- `anonymization.technique_not_found`, `anonymization.field_not_found`, `anonymization.encryption_failed`
- `anonymization.decryption_failed`, `anonymization.token_not_found`, `anonymization.key_not_found`
- `anonymization.invalid_configuration`, `anonymization.risk_assessment_failed`
- Plus 6 additional error codes for store operations, token format, and audit failures

**Observability**:

- OpenTelemetry tracing via `Encina.Compliance.Anonymization` ActivitySource
- Structured log events using `LoggerMessage.Define` (zero-allocation)
- Optional `AnonymizationHealthCheck` verifying service registration and store connectivity

**DI Registration**:

- `services.AddEncinaAnonymization()` with `TryAdd` semantics and `Action<AnonymizationOptions>` configuration

**Multi-Provider Token Mapping Persistence** (13 providers):

- **`TokenMappingEntity`**: Database-agnostic persistence entity with primitive types (strings, byte arrays, DateTimeOffset)
- **`TokenMappingMapper`**: Static `ToEntity`/`ToDomain` mapping with null safety
- ADO.NET: `TokenMappingStoreADO` for SQLite, SQL Server, PostgreSQL, MySQL — provider-specific SQL with parameterized queries
- Dapper: `TokenMappingStoreDapper` for SQLite, SQL Server, PostgreSQL, MySQL — Dapper parameterized queries
- EF Core: `TokenMappingStoreEF` with `TokenMappingEntityConfiguration` — shared implementation across all EF Core providers
- MongoDB: `TokenMappingStoreMongoDB` with `TokenMappingDocument` — BSON serialization with snake_case conventions, DateTimeOffset→DateTime UTC conversion

**Testing**: 351 tests across 5 test projects — 271 unit tests (23 files covering all services, techniques, models, errors, in-memory stores, mapper, entity), 40 guard tests (9 files covering all constructors and public method parameter validation), 16 property tests (3 files with FsCheck invariants for TokenMapping roundtrips, mapper preservation, store deduplication), 12 contract tests (abstract base + InMemory implementation for ITokenMappingStore), 12 integration tests (DI registration, options configuration, full lifecycle tokenize/detokenize and pseudonymize/depseudonymize roundtrips, concurrent access with 50 parallel operations).

**Note**: Database provider integration tests (13 providers) are planned for future phases. Currently ships with InMemory stores and all 13 provider store implementations.

---

#### Encina.Security.Secrets — Secrets Management and Vault Integration (#400)

Added the `Encina.Security.Secrets` package providing ISP-compliant, provider-agnostic secrets management with Railway Oriented Programming. Ships with development-ready providers (environment variables, `IConfiguration`) and a transparent caching decorator. Cloud vault providers (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault, GCP Secret Manager) will be available as separate satellite packages.

**Core Abstractions** (Interface Segregation Principle):

- **`ISecretReader`**: Read secrets — `GetSecretAsync(string)` and `GetSecretAsync<T>(string)` with JSON deserialization
- **`ISecretWriter`**: Write/update secrets — `SetSecretAsync(string, string)` (separate from reader)
- **`ISecretRotator`**: Rotate secrets — `RotateSecretAsync(string)` (optional)
- **`ISecretRotationHandler`**: Custom rotation logic — `GenerateNewSecretAsync` + `OnRotationAsync` callbacks
- **`SecretReference`**: Immutable record with `Name`, `Version?`, `CacheDuration?`, `AutoRotate`, `RotationInterval?`

**Development Providers**:

- **`EnvironmentSecretProvider`**: Reads from environment variables (default provider via `AddEncinaSecrets()`)
- **`ConfigurationSecretProvider`**: Reads from `IConfiguration` sections (`appsettings.json`, user secrets)
- **`FailoverSecretReader`**: Chain multiple providers — first `Right` result wins, returns `SecretsErrors.FailoverExhausted` if all fail

**Decorator Chain** (transparent, opt-in via `SecretsOptions`):

- **`CachedSecretReaderDecorator`**: In-memory caching via `IMemoryCache` with configurable TTL (default 5 min), cache-aside pattern (only `Right` results cached), `Invalidate(secretName)` for manual cache eviction
- **`AuditedSecretReaderDecorator`** / **`AuditedSecretWriterDecorator`** / **`AuditedSecretRotatorDecorator`**: Automatic audit trail via `IAuditStore` — records action, entity, user, tenant, timing, and outcome. Audit failures are logged but never block secret operations

**Secret Rotation**:

- **`SecretRotationCoordinator`**: Orchestrates full rotation workflow — generate new secret → rotate via `ISecretRotator` → notify handlers. Supports multiple `ISecretRotationHandler` registrations

**Attribute-Based Secret Injection**:

- **`[InjectSecret("secret-name")]`**: Decorate pipeline request properties for automatic resolution from `ISecretReader` before handler execution. Supports `Version` (appends `/{version}` to name), `FailOnError` (default `true`, set `false` for graceful skip)
- **`SecretInjectionPipelineBehavior<TRequest, TResponse>`**: Pipeline behavior that discovers `[InjectSecret]` properties via reflection caching (`SecretPropertyCache`) and delegates to `SecretInjectionOrchestrator`

**IConfiguration Bridge**:

- **`IConfigurationBuilder.AddEncinaSecrets()`**: Exposes secrets as `IConfiguration` keys with key delimiter mapping (`--` → `:`), optional prefix filtering with `StripPrefix`, and auto-reload via `ReloadInterval`
- Enables `IOptions<T>` binding for libraries expecting standard configuration

**Error Codes** (9 structured errors via `SecretsErrors`):

- `secrets.not_found`, `secrets.access_denied`, `secrets.provider_unavailable`
- `secrets.deserialization_failed`, `secrets.failover_exhausted`, `secrets.injection_failed`
- `secrets.rotation_failed`, `secrets.audit_failed`, `secrets.cache_failure`
- All errors include structured metadata via `EncinaError` with ROP pattern

**Observability**:

- OpenTelemetry tracing via `Encina.Security.Secrets` ActivitySource with 4 activity types: `Secrets.GetSecret`, `Secrets.SetSecret`, `Secrets.RotateSecret`, `Secrets.InjectSecrets`
- 5 metric instruments under `Encina.Security.Secrets` Meter: `secrets.operations` (counter), `secrets.errors` (counter), `secrets.cache_hits` / `secrets.cache_misses` (counters), `secrets.operation_duration` (histogram in ms)
- `SecretsDiagnostics` internal facade for unified tracing/metrics access
- Configurable via `SecretsOptions.EnableTracing` and `SecretsOptions.EnableMetrics`

**Health Check**:

- **`SecretsHealthCheck`**: Verifies `ISecretReader` is registered and resolvable; optionally probes a specific secret via `SecretsOptions.HealthCheckSecretName`
- Opt-in via `SecretsOptions.ProviderHealthCheck = true`
- Tags: `encina`, `secrets`, `ready`

**DI Registration**:

```csharp
// Default: EnvironmentSecretProvider + caching enabled
services.AddEncinaSecrets();

// With custom reader
services.AddEncinaSecrets<ConfigurationSecretProvider>(options =>
{
    options.EnableCaching = true;
    options.EnableSecretInjection = true;
    options.EnableAccessAuditing = true;
    options.EnableTracing = true;
    options.EnableMetrics = true;
});

// Rotation handler registration
services.AddSecretRotationHandler<DatabasePasswordRotationHandler>();
```

**Testing**: 335 tests (319 unit + 16 integration) covering all providers, decorators, injection, rotation, configuration bridge, health check, and observability.

---

#### Encina.Security.Secrets.AwsSecretsManager — AWS Secrets Manager Provider (#677)

Added the `Encina.Security.Secrets.AwsSecretsManager` satellite package — the second cloud provider for Encina's secrets management system. Integrates with AWS Secrets Manager using the `AWSSDK.SecretsManager` SDK (v4.0.4.6).

**Provider**:

- **`AwsSecretsManagerProvider`**: Implements all three ISP interfaces (`ISecretReader`, `ISecretWriter`, `ISecretRotator`) backed by `IAmazonSecretsManager`
- **Default credential chain** used by default — supports IAM roles, environment variables, shared credential files, EC2 instance profiles, ECS task roles, and EKS Pod Identity
- **Create-or-update semantics**: `SetSecretAsync` attempts `PutSecretValue` first; on `ResourceNotFoundException`, falls back to `CreateSecret` for idempotent writes
- **Thread-safe**: `IAmazonSecretsManager` client is designed for concurrent use across threads

**Error Mapping** (AWS SDK → Encina ROP):

- `ResourceNotFoundException` → `SecretsErrors.NotFound` (`secrets.not_found`)
- `ErrorCode == "AccessDeniedException"` → `SecretsErrors.AccessDenied` (`secrets.access_denied`)
- Other `AmazonSecretsManagerException` → `SecretsErrors.ProviderUnavailable` (`secrets.provider_unavailable`)
- Rotation failures → `SecretsErrors.RotationFailed` (`secrets.rotation_failed`)

**Configuration**:

- **`AwsSecretsManagerOptions`**: `Region` (`RegionEndpoint?`), `Credentials` (`AWSCredentials?`), `ClientConfig` (`AmazonSecretsManagerConfig?`)
- All parameters are optional — sensible defaults from the AWS SDK credential chain

**DI Registration**:

```csharp
services.AddAwsSecretsManager(
    aws =>
    {
        aws.Region = RegionEndpoint.USEast1;
        aws.Credentials = new EnvironmentVariablesAWSCredentials();
    },
    secrets =>
    {
        secrets.EnableCaching = true;
        secrets.DefaultCacheDuration = TimeSpan.FromMinutes(10);
    });
```

**Observability**: Inherits full observability from core package — caching decorator, auditing decorator, health check, OpenTelemetry tracing, and metrics are all applied transparently via `AddEncinaSecrets<AwsSecretsManagerProvider>()`. Provider-specific logging via `LoggerMessage` source generators (EventIds 210-218).

**Testing**: 58 tests (43 unit + 15 guard) covering provider operations, error mapping, create-or-update fallback, typed deserialization, DI registration, options configuration, and null/empty/whitespace argument validation.

---

#### Encina.Security.Secrets.GoogleCloudSecretManager — Google Cloud Secret Manager Provider (#679)

Added the `Encina.Security.Secrets.GoogleCloudSecretManager` satellite package — the fourth cloud provider for Encina's secrets management system. Integrates with Google Cloud Secret Manager using the `Google.Cloud.SecretManager.V1` SDK (v2.7.0).

**Provider**:

- **`GoogleCloudSecretManagerProvider`**: Implements all three ISP interfaces (`ISecretReader`, `ISecretWriter`, `ISecretRotator`) backed by `SecretManagerServiceClient`
- **Application Default Credentials (ADC)** used by default — supports service accounts, gcloud CLI, GCE metadata, GKE Workload Identity
- **Create-or-update semantics**: `SetSecretAsync` uses `AddSecretVersion` with automatic fallback to `CreateSecret` + `AddSecretVersion`
- **Thread-safe**: `SecretManagerServiceClient` is designed for concurrent use across threads

**Error Mapping** (gRPC `StatusCode` → Encina ROP):

| gRPC Status Code | Encina Error Code |
|------------------|-------------------|
| `NotFound` | `secrets.not_found` |
| `PermissionDenied` | `secrets.access_denied` |
| Other `RpcException` | `secrets.provider_unavailable` |
| Rotation failures | `secrets.rotation_failed` |

**Configuration**:

- **`GoogleCloudSecretManagerOptions`**: `ProjectId` (string, **required**)
- `ProjectId` is validated at startup; empty or whitespace values throw `InvalidOperationException`

**DI Registration**:

```csharp
services.AddGoogleCloudSecretManager(
    gcp => gcp.ProjectId = "my-gcp-project",
    secrets =>
    {
        secrets.EnableCaching = true;
        secrets.DefaultCacheDuration = TimeSpan.FromMinutes(10);
    });
```

**Observability**: Inherits full observability from core package — caching decorator, auditing decorator, health check, OpenTelemetry tracing, and metrics are all applied transparently via `AddEncinaSecrets<GoogleCloudSecretManagerProvider>()`. Provider-specific logging via `LoggerMessage` source generators (EventIds 230-238).

**Testing**: 56 tests (39 unit + 17 guard) covering provider operations, error mapping, create-or-update pattern, typed deserialization, DI registration, options configuration, ProjectId validation, and null/empty/whitespace argument validation.

---

#### Encina.Security.Secrets.HashiCorpVault — HashiCorp Vault Provider (#678)

Added the `Encina.Security.Secrets.HashiCorpVault` satellite package — the third cloud provider for Encina's secrets management system. Integrates with HashiCorp Vault's KV v2 secrets engine using the `VaultSharp` SDK (v1.17.5.1).

**Provider**:

- **`HashiCorpVaultSecretProvider`**: Implements all three ISP interfaces (`ISecretReader`, `ISecretWriter`, `ISecretRotator`) backed by `IVaultClient`
- **Multiple auth methods** supported via `IAuthMethodInfo`: Token, AppRole, Kubernetes, LDAP, Userpass, AWS IAM, Azure, GitHub, TLS
- **KV v2 value extraction**: Looks for a `"data"` key with string value; if not found, serializes the entire dictionary to JSON
- **Automatic versioning**: KV v2 creates a new version on every write; `RotateSecretAsync` reads current data and writes it back

**Error Mapping** (via `VaultApiException.HttpStatusCode`):

| HTTP Status | Encina Error Code |
|-------------|-------------------|
| 404 | `secrets.not_found` |
| 403 | `secrets.access_denied` |
| Other | `secrets.provider_unavailable` |
| Rotation failures | `secrets.rotation_failed` |

**Configuration**:

- **`HashiCorpVaultOptions`**: `VaultAddress` (string, **required**), `AuthMethod` (`IAuthMethodInfo?`, **required**), `MountPoint` (string, default `"secret"`)
- Both `VaultAddress` and `AuthMethod` are validated at startup; missing values throw `InvalidOperationException`

**DI Registration**:

```csharp
services.AddHashiCorpVaultSecrets(
    vault =>
    {
        vault.VaultAddress = "https://vault.example.com:8200";
        vault.AuthMethod = new TokenAuthMethodInfo("hvs.my-token");
        vault.MountPoint = "secret";
    },
    secrets =>
    {
        secrets.EnableCaching = true;
        secrets.DefaultCacheDuration = TimeSpan.FromMinutes(5);
    });
```

**Observability**: Inherits full observability from core package — caching decorator, auditing decorator, health check, OpenTelemetry tracing, and metrics are all applied transparently via `AddEncinaSecrets<HashiCorpVaultSecretProvider>()`. Provider-specific logging via `LoggerMessage` source generators (EventIds 220-227).

**Testing**: 65 tests (48 unit + 17 guard) covering provider operations, error mapping, KV v2 value extraction, typed deserialization, DI registration, options configuration, validation, and null/empty/whitespace argument validation.

---

#### Encina.Security.Secrets.AzureKeyVault — Azure Key Vault Provider (#676)

Added the `Encina.Security.Secrets.AzureKeyVault` satellite package — the first cloud provider for Encina's secrets management system. Integrates with Azure Key Vault using the `Azure.Security.KeyVault.Secrets` SDK (v4.8.0) and `Azure.Identity` (v1.17.1).

**Provider**:

- **`AzureKeyVaultSecretProvider`**: Implements all three ISP interfaces (`ISecretReader`, `ISecretWriter`, `ISecretRotator`) backed by `SecretClient`
- **`DefaultAzureCredential`** used by default — supports managed identities, environment variables, Azure CLI, and Visual Studio credentials
- **Automatic versioning**: Azure Key Vault creates a new version on every write; `RotateSecretAsync` leverages this behavior
- **Thread-safe**: `SecretClient` is designed for concurrent use across threads

**Error Mapping** (Azure SDK → Encina ROP):

- HTTP 404 → `SecretsErrors.NotFound` (`secrets.not_found`)
- HTTP 401/403 → `SecretsErrors.AccessDenied` (`secrets.access_denied`)
- Other HTTP errors → `SecretsErrors.ProviderUnavailable` (`secrets.provider_unavailable`)
- Rotation failures → `SecretsErrors.RotationFailed` (`secrets.rotation_failed`)

**Configuration**:

- **`AzureKeyVaultOptions`**: `VaultUri`, `Credential` (`TokenCredential?`), `ClientOptions` (`SecretClientOptions?`)
- All settings configurable via `Action<AzureKeyVaultOptions>` delegate

**DI Registration**:

```csharp
services.AddAzureKeyVaultSecrets(
    new Uri("https://my-vault.vault.azure.net/"),
    kvOptions => kvOptions.Credential = new ManagedIdentityCredential(),
    secretsOptions =>
    {
        secretsOptions.EnableCaching = true;
        secretsOptions.DefaultCacheDuration = TimeSpan.FromMinutes(10);
    });
```

**Observability**: Inherits full observability from core package — caching decorator, auditing decorator, health check, OpenTelemetry tracing, and metrics are all applied transparently via `AddEncinaSecrets<AzureKeyVaultSecretProvider>()`. Provider-specific logging via `LoggerMessage` source generators (EventIds 200-208).

**Testing**: 59 tests (43 unit + 16 guard) covering provider operations, error mapping, typed deserialization, DI registration, options configuration, and null/empty/whitespace argument validation.

---

#### Encina.Security.Encryption — Field-Level Encryption with AES-256-GCM (#396)

Added the `Encina.Security.Encryption` package providing attribute-based, automatic field-level encryption and decryption at the CQRS pipeline level. Uses AES-256-GCM (NIST SP 800-38D) with per-operation random nonces, key rotation support, and multi-tenant key isolation for GDPR, HIPAA, and PCI-DSS compliance.

**Core Abstractions**:

- **`IFieldEncryptor`**: Low-level encrypt/decrypt interface for strings and byte arrays, returning `Either<EncinaError, T>` (Railway Oriented Programming)
- **`IKeyProvider`**: Key management abstraction with `GetKeyAsync`, `GetCurrentKeyIdAsync`, and `RotateKeyAsync` — pluggable for Azure Key Vault, AWS KMS, or HashiCorp Vault
- **`IEncryptionOrchestrator`**: High-level orchestrator that discovers `[Encrypt]`-decorated properties via reflection caching and delegates to `IFieldEncryptor`
- **`EncryptionContext`**: Sealed record carrying `KeyId`, `Purpose` (for key derivation), `TenantId` (multi-tenant isolation), and `AssociatedData` (AEAD binding)
- **`EncryptedValue`**: Readonly record struct with `Ciphertext`, `Algorithm`, `KeyId`, `Nonce`, and `Tag` — zero-allocation storage for hot paths

**Three Declarative Attributes** (composable with other pipeline behaviors):

- **`[Encrypt(Purpose = "User.Email")]`**: Marks string properties for automatic encryption with optional purpose-based key derivation and explicit key version
- **`[EncryptedResponse]`**: Marks response types for encryption before returning to the caller
- **`[DecryptOnReceive]`**: Marks incoming data for decryption before handler execution

**Base Attribute** (`EncryptionAttribute`):

- `Algorithm` — configurable per-property (default: `Aes256Gcm`)
- `FailOnError` — when `true` (default), encryption/decryption failures propagate as `EncinaError`; when `false`, the value is left unchanged

**Pipeline Behavior**:

- **`EncryptionPipelineBehavior<TRequest, TResponse>`**: Automatic encrypt-before-handle and decrypt-after-handle with attribute detection
- Requests without encryption attributes bypass all checks (zero overhead)
- Configurable via `EncryptionOptions` for global defaults

**Serialization Format** (compact, self-describing):

- `ENC:v1:{Algorithm}:{KeyId}:{Base64Nonce}:{Base64Tag}:{Base64Ciphertext}` — embeds key version for seamless key rotation

**Error Codes** (5 structured errors via `EncryptionErrors`):

- `encryption.key_not_found`, `encryption.decryption_failed`, `encryption.invalid_ciphertext`
- `encryption.algorithm_not_supported`, `encryption.key_rotation_failed`
- All errors include structured metadata (`keyId`, `propertyName`, `algorithm`)

**Observability**:

- OpenTelemetry tracing via `Encina.Security.Encryption` ActivitySource with tags: `encryption.request_type`, `encryption.operation`, `encryption.property_count`, `encryption.outcome`
- 3 metric instruments under `Encina.Security.Encryption` Meter: `encryption.operations.total` (counter), `encryption.failures.total` (counter), `encryption.operation.duration` (histogram in ms)
- Structured logging with `ILogger<T>` for encrypt/decrypt operations and error conditions

**Health Check**:

- **`EncryptionHealthCheck`**: Roundtrip verification — encrypts test data and decrypts it to verify the full crypto pipeline
- Opt-in via `EncryptionOptions.AddHealthCheck = true`
- Tags: `encina`, `encryption`, `ready`

**Default Implementation**:

- **`InMemoryKeyProvider`**: Thread-safe in-memory key store for testing and development (not for production)
- **`AesGcmFieldEncryptor`**: AES-256-GCM with 12-byte nonce, 16-byte tag, 32-byte key — stateless, thread-safe

**DI Registration**:

```csharp
// Basic setup (InMemoryKeyProvider for testing)
services.AddEncinaEncryption();

// With custom key provider (e.g., Azure Key Vault)
services.AddSingleton<IKeyProvider, AzureKeyVaultKeyProvider>();
services.AddEncinaEncryption(options =>
{
    options.AddHealthCheck = true;
    options.EnableTracing = true;
});

// Or use the generic overload
services.AddEncinaEncryption<AzureKeyVaultKeyProvider>();
```

**Testing**: 181 tests across 5 test projects (109 unit, 22 guard, 12 property, 28 contract, 10 integration) plus BenchmarkDotNet benchmarks.

---

#### Encina.Security.Sanitization — Input Sanitization and Output Encoding (#399)

Added the `Encina.Security.Sanitization` package providing attribute-based, context-aware input sanitization and output encoding at the CQRS pipeline level. Prevents XSS, SQL injection, command injection, and other OWASP Top 10 injection attacks through declarative property annotations.

**Core Abstractions**:

- **`ISanitizer`**: Context-aware input sanitization interface with 6 methods — `SanitizeHtml`, `SanitizeForSql`, `SanitizeForShell`, `SanitizeForJson`, `SanitizeForXml`, `Custom`
- **`IOutputEncoder`**: Context-aware output encoding interface with 5 methods — `EncodeForHtml`, `EncodeForHtmlAttribute`, `EncodeForJavaScript`, `EncodeForUrl`, `EncodeForCss`
- **`ISanitizationProfile`**: Immutable profile defining allowed tags, attributes, protocols, and stripping behavior
- **`SanitizationOptions`**: Configuration for global sanitization mode, default profile, health check, tracing, and metrics

**Sanitization Contexts** (5 input sanitization types):

- **HTML** (`[SanitizeHtml]`): Strips dangerous tags/attributes while preserving safe HTML via HtmlSanitizer (Ganss.Xss)
- **SQL** (`[SanitizeSql]`): Escapes single quotes, removes comment markers, semicolons, and `xp_` extended procedures
- **Shell** (`[SanitizeForShell]`): OS-aware escaping — `^`-escaping on Windows, single-quote wrapping on Unix
- **JSON**: Escapes control characters and special JSON characters per RFC 8259
- **XML**: Escapes `<`, `>`, `&`, `"`, `'` and strips invalid XML 1.0 characters

**Output Encoding** (5 encoding contexts):

- **HTML** (`[EncodeForHtml]`): Entity-encodes `<`, `>`, `&`, `"`, `'`
- **JavaScript** (`[EncodeForJavaScript]`): `\uXXXX` encoding for non-alphanumeric characters
- **URL** (`[EncodeForUrl]`): RFC 3986 percent-encoding via `UrlEncoder.Default`
- **CSS**: `\XXXXXX` hex escaping per OWASP Rule #4
- **HTML Attribute**: Context-specific attribute encoding

**Built-in Sanitization Profiles** (5 profiles):

- **`SanitizationProfiles.None`**: Pass-through, no modifications
- **`SanitizationProfiles.StrictText`**: Strips all HTML tags (default for auto-sanitization)
- **`SanitizationProfiles.BasicFormatting`**: Allows `<b>`, `<i>`, `<em>`, `<strong>`, `<br>`, `<p>`
- **`SanitizationProfiles.RichText`**: Adds headings, links, images, lists, tables with `https`/`mailto` protocols
- **`SanitizationProfiles.Markdown`**: Comprehensive Markdown-rendered HTML including `<code>`, `<pre>`, `<blockquote>`

**Custom Profiles** (via fluent builder):

```csharp
options.AddProfile("BlogPost", profile =>
{
    profile.AllowTags("p", "h1", "h2", "a", "img");
    profile.AllowAttributes("href", "src", "alt");
    profile.AllowProtocols("https", "mailto");
    profile.WithStripScripts(true);
});
```

**Pipeline Behaviors**:

- **`InputSanitizationPipelineBehavior<TRequest, TResponse>`**: Pre-handler — sanitizes request properties before handler execution via attribute discovery and compiled delegate caching
- **`OutputEncodingPipelineBehavior<TRequest, TResponse>`**: Post-handler — encodes response properties after handler execution
- Requests without sanitization/encoding attributes bypass all checks (zero overhead)
- Configurable via `SanitizeAllStringInputs` (global auto-sanitize) and `EncodeAllOutputs` (global auto-encode)

**Error Codes** (2 structured errors via `SanitizationErrors`):

- `sanitization.profile_not_found` — requested profile not registered
- `sanitization.property_error` — sanitization of a property failed
- All errors include structured metadata (`profileName`/`propertyName`, `stage`)

**Observability**:

- OpenTelemetry tracing via `Encina.Security.Sanitization` ActivitySource with activities: `Sanitization.Input`, `Sanitization.Output`; tags: `sanitization.request_type`, `sanitization.operation`, `sanitization.type`, `sanitization.profile`, `sanitization.property_count`, `sanitization.outcome`
- 4 metric instruments under `Encina.Security.Sanitization` Meter: `sanitization.operations` (counter), `sanitization.properties.processed` (counter), `sanitization.failures` (counter), `sanitization.duration` (histogram in ms)
- 9 structured log events (EventId 1–9) using `LoggerMessage` source generation for zero-allocation logging

**Health Check**:

- **`SanitizationHealthCheck`**: Verifies `ISanitizer`, `IOutputEncoder`, and `SanitizationOrchestrator` are resolvable from DI
- Opt-in via `SanitizationOptions.AddHealthCheck = true`
- Tags: `encina`, `sanitization`, `ready`

**DI Registration**:

```csharp
services.AddEncinaSanitization(options =>
{
    options.SanitizeAllStringInputs = true;
    options.EncodeAllOutputs = true;
    options.AddHealthCheck = true;
    options.EnableTracing = true;
    options.EnableMetrics = true;
});
```

**Testing**: 283 tests across 3 test projects (235 unit, 21 guard, 27 property).

---

#### Encina.Security.PII — PII Masking and Data Protection (#397)

Added the `Encina.Security.PII` package providing attribute-based PII masking and data protection at the CQRS pipeline level. Automatically masks personally identifiable information (emails, phone numbers, SSNs, credit cards) in responses, logs, and audit trails with configurable masking strategies for GDPR/HIPAA/PCI-DSS compliance.

**Core Abstractions**:

- **`IPIIMasker`**: PII masking interface with 3 methods — `Mask(string, PIIType)`, `Mask(string, pattern)`, `MaskObject<T>(T)`
- **`IMaskingStrategy`**: Extensible masking strategy interface for custom PII handling
- **`PIIOptions`**: Configuration for default masking mode, response/log/audit masking, health check, tracing, and metrics
- **`PIIAttribute`**: Declarative attribute for marking PII properties with type, mode, pattern, and replacement
- **`MaskingOptions`**: Immutable record struct configuring mask character, visible characters, preserve length, and hash salt

**9 Built-in Masking Strategies** (one per `PIIType`):

- **Email** (`PIIType.Email`): `user@example.com` → `u***@example.com`
- **Phone** (`PIIType.Phone`): `555-123-4567` → `***-***-4567`
- **Credit Card** (`PIIType.CreditCard`): `4111-1111-1111-1111` → `****-****-****-1111`
- **SSN** (`PIIType.SSN`): `123-45-6789` → `***-**-6789`
- **Name** (`PIIType.Name`): `John Doe` → `J*** D**`
- **Address** (`PIIType.Address`): `123 Main St, Springfield, IL` → `*** **** **, Springfield, IL`
- **Date of Birth** (`PIIType.DateOfBirth`): `01/15/1990` → `**/**/1990`
- **IP Address** (`PIIType.IPAddress`): `192.168.1.100` → `192.168.***.***`
- **Custom** (`PIIType.Custom`): Full masking fallback for unclassified PII

**5 Masking Modes** (configurable per attribute or globally):

- **Partial**: Show selected characters, mask the rest (default)
- **Full**: Replace all characters with mask character
- **Hash**: SHA-256 deterministic hash with optional salt
- **Tokenize**: Passthrough for external tokenization systems
- **Redact**: Replace entire value with `[REDACTED]`

**3 Declarative Attributes** (composable):

- **`[PII(PIIType.Email)]`**: Mark property with specific PII type and optional mode/pattern/replacement
- **`[SensitiveData]`**: Lightweight marker for sensitive data (defaults to `PIIType.Custom` + `MaskingMode.Full`)
- **`[MaskInLogs]`**: Mark property for masking only in logging context (not in responses)

**Pipeline Behavior**:

- **`PIIMaskingPipelineBehavior<TRequest, TResponse>`**: Post-handler — masks PII-decorated properties on response objects using JSON deep-copy
- Requests without PII attributes bypass all checks (zero overhead)
- Property metadata cached via `ConcurrentDictionary` after first access per type
- Configurable sensitive field pattern detection for convention-based masking (password, secret, token, etc.)

**Audit Trail Integration**:

- Implements `IPiiMasker` from `Encina.Security.Audit` for automatic PII redaction in audit entries
- **`MaskForAudit<T>(T)`** and **`MaskForAudit(object)`**: Generic and non-generic audit masking
- Single DI registration serves both `IPIIMasker` and `IPiiMasker` interfaces

**Logging Extensions** (4 methods):

- **`LogMasked`**: Level-aware PII masking before structured logging with automatic fallback on failure
- **`LogInformationMasked`**, **`LogWarningMasked`**, **`LogErrorMasked`**: Convenience wrappers per log level

**Error Codes** (3 structured errors via `PIIErrors`):

- `pii.masking_failed` — masking operation encountered an error
- `pii.strategy_not_found` — no strategy registered for the requested PII type
- `pii.invalid_configuration` — invalid PII options or strategy configuration
- All errors include structured metadata (`piiType`, `propertyName`, `stage`)

**Observability**:

- OpenTelemetry tracing via `Encina.Security.PII` ActivitySource with activities: `PII.MaskObject`, `PII.MaskProperty`, `PII.ApplyStrategy`; tags: `pii.type_name`, `pii.property_count`, `pii.masked_count`, `pii.mode`, `pii.outcome`
- 5 metric instruments under `Encina.Security.PII` Meter: `pii.masking.operations` (counter), `pii.masking.properties` (counter), `pii.masking.errors` (counter), `pii.masking.duration` (histogram in ms), `pii.pipeline.operations` (counter)
- 9 structured log events (EventId 8000–8008) using `LoggerMessage` source generation for zero-allocation logging

**Health Check**:

- **`PIIHealthCheck`**: Verifies `IPIIMasker` resolution, strategy availability (9 built-in), and masking probe (email test)
- Opt-in via `PIIOptions.AddHealthCheck = true`
- Tags: `encina`, `pii`, `ready`

**DI Registration**:

```csharp
services.AddEncinaPII(options =>
{
    options.MaskInResponses = true;
    options.MaskInLogs = true;
    options.MaskInAuditTrails = true;
    options.DefaultMode = MaskingMode.Partial;
    options.AddHealthCheck = true;
    options.EnableTracing = true;
    options.EnableMetrics = true;
});
```

**Testing**: 319 tests across 5 test projects (242 unit, 13 guard, 6 property, 21 contract, 37 benchmarks).

---

#### Encina.Security.AntiTampering — HMAC Request Signing and Integrity Verification (#398)

Added the `Encina.Security.AntiTampering` package providing HMAC-based request signing, integrity verification, and replay attack protection at the CQRS pipeline level. Protects API endpoints against tampering, replay, and impersonation attacks using cryptographic signatures with timestamp validation and nonce-based deduplication.

**Core Abstractions**:

- **`IRequestSigner`**: Signs and verifies request payloads using HMAC, returning `Either<EncinaError, T>` (Railway Oriented Programming)
- **`INonceStore`**: Nonce deduplication interface with `TryAddAsync` and `ExistsAsync` for replay attack prevention
- **`IKeyProvider`**: Key management abstraction for HMAC secret keys, returning `Either<EncinaError, byte[]>` — pluggable for cloud KMS
- **`IRequestSigningClient`**: High-level client for signing outgoing `HttpRequestMessage` instances with all required headers

**Declarative Attribute**:

- **`[RequireSignature]`**: Marks commands/queries for automatic HMAC validation in the pipeline
- `KeyId` — optional key restriction for specific request types
- `SkipReplayProtection` — opt-out of nonce validation for idempotent operations

**HMAC Signing** (3 algorithms):

- **HMAC-SHA256** (default), **HMAC-SHA384**, **HMAC-SHA512**
- Canonical signature format: `HMAC(SecretKey, "Method|Path|PayloadHash|Timestamp|Nonce")`
- Payload hash computed using SHA-256 (hex-encoded lowercase)
- Key rotation support via `KeyId` header for concurrent key versions

**Signing Records**:

- **`SigningContext`**: Sealed record with `KeyId`, `Nonce`, `Timestamp`, `HttpMethod`, `RequestPath` — input for signing/verification
- **`SignatureComponents`**: Sealed record with `ToCanonicalString()` producing the pipe-delimited canonical form

**Pipeline Behavior**:

- **`HMACValidationPipelineBehavior<TRequest, TResponse>`**: Automatic HMAC validation on incoming requests
- Extracts 4 headers (X-Signature, X-Timestamp, X-Nonce, X-Key-Id) from `HttpContext`
- Validates timestamp tolerance (configurable window)
- Validates nonce uniqueness (replay protection)
- Verifies HMAC signature against recomputed hash
- Requests without `[RequireSignature]` bypass all checks (zero overhead)
- Reflection caching via `ConcurrentDictionary` for attribute lookups

**HTTP Headers** (4 headers, all customizable):

| Header | Default | Purpose |
|--------|---------|---------|
| `X-Signature` | Signature | Base64-encoded HMAC |
| `X-Timestamp` | Timestamp | ISO 8601 UTC |
| `X-Nonce` | Nonce | Unique request ID |
| `X-Key-Id` | Key ID | Signing key identifier |

**Nonce Stores** (2 implementations):

- **`InMemoryNonceStore`**: Thread-safe in-memory store with background cleanup timer (5-minute interval) — suitable for single-instance deployments
- **`DistributedCacheNonceStore`**: Distributed cache-backed store via `ICacheProvider` — suitable for multi-instance deployments (Redis, Valkey, etc.)

**Error Codes** (6 structured errors via `AntiTamperingErrors`):

- `antitampering.key_not_found`, `antitampering.signature_invalid`, `antitampering.signature_missing`
- `antitampering.timestamp_expired`, `antitampering.nonce_reused`, `antitampering.nonce_missing`
- All errors include structured metadata (`keyId`, `headerName`, `nonce`, `stage`)

**Observability**:

- OpenTelemetry tracing via `Encina.Security.AntiTampering` ActivitySource with tags: `antitampering.request_type`, `antitampering.key_id`, `antitampering.algorithm`, `antitampering.outcome`
- 4 metric instruments under `Encina.Security.AntiTampering` Meter: `antitampering.sign.total`, `antitampering.verify.total` (counters), `antitampering.verify.failures` (counter), `antitampering.operation.duration` (histogram in ms)
- Structured logging with `LoggerMessage` source generation for zero-allocation logging

**Health Check**:

- **`AntiTamperingHealthCheck`**: Verifies `IKeyProvider`, `IRequestSigner`, `INonceStore` are resolvable and functional (roundtrip nonce probe)
- Opt-in via `AntiTamperingOptions.AddHealthCheck = true`
- Tags: `encina`, `security`, `antitampering`

**DI Registration**:

```csharp
// Basic setup with test keys
services.AddEncinaAntiTampering(options =>
{
    options.Algorithm = HMACAlgorithm.SHA256;
    options.TimestampToleranceMinutes = 5;
    options.RequireNonce = true;
    options.AddKey("api-key-v1", "your-secret-value");
});

// Distributed nonce store (requires ICacheProvider)
services.AddDistributedNonceStore();

// Custom key provider (register before AddEncinaAntiTampering)
services.AddSingleton<IKeyProvider, AzureKeyVaultKeyProvider>();
services.AddEncinaAntiTampering();
```

**Testing**: 94 tests across 5 test projects (41 unit, 36 guard, 6 property, 11 integration) plus BenchmarkDotNet benchmarks (17 benchmarks for signing/verification throughput across payload sizes and algorithms).

#### Encina.AspNetCore — Policy-Based Authorization Enhancement (#356)

Enhanced the `AuthorizationPipelineBehavior` with CQRS-aware default policies, resource-based authorization, and a thin ROP facade over ASP.NET Core's `IAuthorizationService`. This extends — not replaces — ASP.NET Core's native authorization system.

**CQRS-Aware Default Policies**:

- `AuthorizationConfiguration` with `DefaultCommandPolicy` and `DefaultQueryPolicy` (both default to `"RequireAuthenticated"` — secure-by-default)
- `AutoApplyPolicies` option: when enabled, commands and queries without explicit `[Authorize]` attributes automatically receive their CQRS-type default policy
- `[AllowAnonymous]` bypasses all authorization checks, including auto-applied defaults

**Resource-Based Authorization**:

- `[ResourceAuthorize("PolicyName")]` attribute for request types — the request object is passed as the resource to ASP.NET Core's `IAuthorizationService.AuthorizeAsync(user, resource, policy)`
- Works with standard `AuthorizationHandler<TRequirement, TResource>` handlers
- Composable with `[Authorize]` attributes (both are checked, AND logic)

**IResourceAuthorizer Facade**:

- `IResourceAuthorizer` interface for handlers that need resource-based authorization after loading the resource from the database
- Thin wrapper over `IAuthorizationService` adding ROP semantics (`Either<EncinaError, bool>`)
- Registered as scoped via `AddEncinaAuthorization()`

**Policy Helper Extensions**:

- `AddRolePolicy()`, `AddClaimPolicy()`, `AddAuthenticatedPolicy()` — convenience wrappers over `AuthorizationOptions` (not a parallel system)

**Error Codes** (4 structured errors via `EncinaErrorCodes`):

- `encina.authorization.unauthorized`, `encina.authorization.forbidden`, `encina.authorization.policy_failed`, `encina.authorization.resource_denied`
- All errors include structured metadata (`requestType`, `stage`, `userId`, `policy`, `failureReasons`)

**DI Registration**:

- `services.AddEncinaAuthorization()` registers `AuthorizationConfiguration`, `IResourceAuthorizer`, and the `"RequireAuthenticated"` policy

**Testing**: 103 tests (90 unit + 13 integration) covering attribute validation, resource authorizer facade, pipeline behavior with CQRS auto-apply, resource-based authorization, claim policies, and full DI integration with real ASP.NET Core authorization infrastructure.

---

### Changed

#### Railway Oriented Programming — Full Either Enforcement (#670, #671, #672, #673)

- All orchestrator public methods now return `Either<EncinaError, T>` instead of throwing exceptions (#670)
- All mapping builder `Build()` methods now return `Either<EncinaError, T>` instead of throwing `InvalidOperationException` (#671)
- Removed unreachable `throw` statements from `Match`/`MatchAsync` callbacks where the branch was already determined by prior `IsLeft`/`IsSome` checks (#672)
- Replaced runtime `InvalidOperationException` throws with `Either<EncinaError, T>` returns across all config/connection infrastructure (#673):
  - `IReadWriteConnectionSelector` and `ReadWriteConnectionSelector` (3 methods)
  - 8 `IReadWriteConnectionFactory` + implementations (ADO.NET + Dapper, all 4 databases)
  - `IReadWriteDbContextFactory` + `ReadWriteDbContextFactory` (EF Core)
  - 8 `ITenantConnectionFactory` + implementations (ADO.NET + Dapper, all 4 databases)
  - Generic `ITenantConnectionFactory<T>` + `TenantConnectionFactoryBase<T>` (Tenancy)
  - `TenantDbContextFactory<TContext>` (EF Core Tenancy)
  - `SagaNotFoundContext.MoveToDeadLetterAsync` (Messaging)
- Removed 16 unreachable `throw new InvalidOperationException("Unexpected Right after Left check")` in Sharding `GetAllConnections`/`GetAllCollections` loops across all 13 database providers, replaced with direct `(EncinaError)` cast (#675)

### Fixed

- `SchedulerOrchestrator.HandleRecurringMessageAsync` crashed when cron parser returned `Left` (no more occurrences) because `Either.Match` with a `null` return violates LanguageExt's non-null contract. Replaced with `MatchAsync` using both branches returning `Unit.Default` (#674)

---

## [0.12.0] - 2026-02-16 - Database & Repository

### Added

#### Database Sharding (#289)

Added comprehensive database sharding with four routing strategies, support for all 13 database providers, and full observability integration.

**Core Architecture**:

- **`IShardRouter`**: Abstraction for shard key → shard ID mapping with four implementations
- **`ShardTopology`**: Immutable shard configuration with connection metadata per shard
- **`ShardKeyExtractor`**: Extracts shard keys via `IShardable` interface or `[ShardKey]` attribute (cached reflection)
- **`EntityShardRouter<TEntity>`**: Combines extraction and routing into a single pipeline step
- **`IShardedQueryExecutor`**: Scatter-gather engine for cross-shard queries with partial failure handling

**Four Routing Strategies**:

- **Hash** (`HashShardRouter`): xxHash64 + consistent hashing with 150 virtual nodes per shard, ~1/N data movement on rebalance via `IShardRebalancer`
- **Range** (`RangeShardRouter`): Sorted boundary binary search, overlap detection at construction time
- **Directory** (`DirectoryShardRouter`): Explicit key-to-shard mapping via `IShardDirectoryStore` (pluggable backends)
- **Geo** (`GeoShardRouter`): Region-based routing with fallback chains and cycle detection

**Provider Support** (13 providers):

- ADO.NET: `AddEncinaADOSharding<TEntity, TId>()` — SQLite, SqlServer, PostgreSQL, MySQL
- Dapper: `AddEncinaDapperSharding<TEntity, TId>()` — reuses ADO's `IShardedConnectionFactory`
- EF Core: `AddEncinaEFCoreSharding{Provider}<TContext, TEntity, TId>()` — SQLite, SqlServer, PostgreSQL, MySQL
- MongoDB: `AddEncinaMongoDBSharding<TEntity, TId>()` — dual-mode (native mongos + app-level routing)

**Observability**:

- 7 metric instruments under "Encina" meter (route decisions, route latency, scatter duration, partial failures, active queries, per-shard duration, active shards gauge)
- 3 trace activities under "Encina.Sharding" ActivitySource (Routing, ScatterGather, ShardQuery)
- 13 stable error codes prefixed `encina.sharding.*`

**Health Monitoring**:

- `ShardHealthResult` with three-state model (Healthy/Degraded/Unhealthy)
- `ShardedHealthSummary` with aggregate status calculation
- Configurable health check interval via `ShardingMetricsOptions`

**Documentation** (5 guides + 1 ADR):

- `docs/architecture/adr/010-database-sharding.md` — Architecture Decision Record
- `docs/sharding/configuration.md` — Complete configuration reference
- `docs/sharding/scaling-guidance.md` — Shard key selection, capacity planning, rebalancing
- `docs/sharding/mongodb.md` — MongoDB dual-mode (native vs app-level)
- `docs/sharding/cross-shard-operations.md` — Scatter-gather, Saga pattern, partial failures

**Testing**: ~680+ tests across unit, integration, guard, contract, property tests; 13 BenchmarkDotNet benchmarks

#### Compound Shard Keys (#641)

Added multi-field shard key support, enabling routing decisions based on combinations of entity properties (e.g., tenant + region, country + category).

**Core Abstractions**:

- **`CompoundShardKey`**: Immutable record holding ordered components with implicit conversion from `string` and pipe-delimited `ToString()`
- **`ICompoundShardable`**: Interface for entities that expose a compound shard key via `GetCompoundShardKey()`
- **`CompoundShardKeyExtractor`**: Static extractor with priority resolution: `ICompoundShardable` → multiple `[ShardKey]` attributes → `IShardable` → single `[ShardKey]`
- **`ShardKeyAttribute.Order`**: New property for specifying component order in compound keys (0-based)

**Routing Infrastructure**:

- **`CompoundShardRouter`**: Routes each key component through a dedicated strategy (hash, range, directory, geo) and combines results via configurable `ShardIdCombiner`
- **`CompoundShardRouterOptions`**: Configuration with per-component router dictionary and combiner function (default: hyphen-join)
- **`IShardRouter.GetShardId(CompoundShardKey)`**: New default interface method for compound routing (falls back to `ToString()` + single-key routing)
- **`IShardRouter.GetShardIds(CompoundShardKey)`**: Partial key routing for scatter-gather queries with prefix keys
- All four existing routers (Hash, Range, Directory, Geo) extended with compound key overloads

**Configuration**:

- **`CompoundRoutingBuilder`**: Fluent builder with `Component()`, `HashComponent()`, `RangeComponent()`, `DirectoryComponent()`, `GeoComponent()`, and `CombineWith()`
- **`ShardingOptions<TEntity>.UseCompoundRouting()`**: Entry point for configuring compound routing per entity type

**Error Codes** (4 new):

- `CompoundShardKeyEmpty`, `CompoundShardKeyComponentEmpty`, `DuplicateShardKeyOrder`, `PartialKeyRoutingFailed`

**Observability**:

- `ShardRoutingMetrics.RecordCompoundKeyExtraction()` for tracking compound key extraction with component count and router type

#### Time-Based Sharding & Archival (#650)

Added time-based shard partitioning with automatic tier lifecycle management (Hot → Warm → Cold → Archived), enabling data archival workflows for time-series, audit logs, and IoT data.

**Core Abstractions**:

- **`ITimeBasedShardRouter`**: Extends `IShardRouter` with timestamp routing (`RouteByTimestampAsync`), write routing with Hot-tier enforcement (`RouteWriteByTimestampAsync`), range queries (`GetShardsInRangeAsync`), and tier introspection
- **`ShardPeriod`**: Enum defining partitioning granularity (Daily, Weekly, Monthly, Quarterly, Yearly)
- **`ShardTier`**: Enum defining storage tiers (Hot, Warm, Cold, Archived) with ordered progression
- **`ShardTierInfo`**: Immutable record combining shard identity, tier, period boundaries `[start, end)`, read-only status, and connection string
- **`TierTransition`**: Record defining age-based rules for tier promotion with forward-only validation
- **`PeriodBoundaryCalculator`**: Static utility for computing period start/end, labels, and enumerating contiguous periods across all 5 granularities

**Tier Lifecycle Automation**:

- **`TierTransitionScheduler`**: `BackgroundService` that periodically checks for shards due for tier transition and auto-creates next-period Hot shards before they're needed
- **`IShardArchiver`** / **`ShardArchiver`**: Coordinates tier transitions, read-only enforcement, archival to external storage, and retention-based deletion via Railway Oriented Programming
- **`ITierStore`** / **`InMemoryTierStore`**: Persists and queries shard tier metadata; in-memory implementation with `ConcurrentDictionary` for single-process deployments
- **`IReadOnlyEnforcer`**: Provider-specific interface for database-level read-only enforcement (e.g., `ALTER DATABASE SET READ_ONLY`)
- **`IShardFallbackCreator`**: On-demand shard creation for resilience when scheduler misses its window

**Routing Implementation**:

- **`TimeBasedShardRouter`**: Binary search over sorted period ranges with `FrozenDictionary` shard lookup, co-location support, and fallback creation
- Write operations reject non-Hot shards with error code `encina.sharding.shard_read_only`
- Prefix-based scatter-gather via `GetShardIds(CompoundShardKey)` (e.g., `"2026"` matches all 2026 periods)

**Health Monitoring**:

- **`TierTransitionHealthCheck`**: Reports Healthy/Degraded/Unhealthy based on shard age vs configured per-tier thresholds
- **`ShardCreationHealthCheck`**: Reports Unhealthy if current-period shard is missing, Degraded if next-period shard is missing within warning window

**Observability** (`Encina.OpenTelemetry`):

- 6 metric instruments: `shards_per_tier` gauge, `oldest_hot_shard_age_days` gauge, `tier_transitions_total` counter, `auto_created_shards_total` counter, `queries_per_tier` counter, `archival_duration_ms` histogram

**Error Codes** (6 codes):

- `TimestampOutsideRange`, `ShardReadOnly`, `NoTimeBasedShards`, `TierTransitionFailed`, `ShardNotFound`, `RetentionPolicyFailed`

**Configuration**:

- **`TimeBasedShardingOptions`**: Controls scheduler interval, period, lead time, connection string template, and tier transition rules
- **`TimeBasedShardRouterOptions`**: Per-entity fluent configuration via `UseTimeBasedRouting()`

**Testing**: 227 tests across unit (161), guard (31), contract (23), and property (12) tests

#### Shadow Sharding (#649)

Added shadow sharding for testing new shard topologies under real production traffic with zero impact. Enables phased rollouts from dual-write validation to full cutover.

**Core Abstractions**:

- **`IShadowShardRouter`**: Extends `IShardRouter` with shadow-specific operations (`RouteShadowAsync`, `CompareAsync`, `IsShadowEnabled`, `ShadowTopology`)
- **`ShadowShardRouterDecorator`**: Wraps production router; all `IShardRouter` methods delegate to primary, shadow operations run against secondary topology with latency measurement
- **`ShadowComparisonResult`**: Immutable record capturing routing comparison data (shard keys, routing match, latency measurements, optional result match, timestamp)
- **`ShadowShardingOptions`**: Configuration with `ShadowTopology`, `DualWriteEnabled`, `ShadowReadPercentage` (0-100), `CompareResults`, `DiscrepancyHandler`, `ShadowWriteTimeout`, `ShadowRouterFactory`

**Pipeline Integration**:

- **`ShadowWritePipelineBehavior<TCommand, TResponse>`**: Fire-and-forget shadow write on production success with configurable timeout; shadow failures logged, never propagated
- **`ShadowReadPipelineBehavior<TQuery, TResponse>`**: Percentage-based shadow read sampling with hash-based result comparison; discrepancies forwarded to optional handler

**Configuration**:

- **`WithShadowSharding()`**: Fluent extension on `ShardingOptions<TEntity>` for shadow topology setup
- **`ShadowShardingServiceCollectionExtensions`**: DI registration for decorator, behaviors, and options

**Observability** (`Encina.OpenTelemetry`):

- **`ShadowShardingMetrics`**: 6 metric instruments — routing comparisons, routing mismatches, shadow writes (by outcome), write latency diff, read comparisons (by result match), read latency diff
- **`ShadowShardingActivityEnricher`**: Trace enrichment with production/shadow shard IDs, routing match, write outcome, read results match
- 5 trace tag constants under `ActivityTagNames.Shadow`
- Structured logging via `ShadowShardingLog` (source-generated LoggerMessage delegates)

**Error Codes** (1 code):

- `encina.sharding.shadow_routing_failed`

**Testing**: 90 tests across unit (55), guard (18), contract (9), property (8) tests; 5 BenchmarkDotNet benchmarks

#### Schema Migration Coordination for Shards (#651)

Added coordinated schema migration across sharded databases with four deployment strategies, automatic rollback, schema drift detection, and full observability integration.

**Core Types** (`Encina` package):

- **`MigrationScript`**: Immutable record with `Id`, `UpSql`, `DownSql`, `Description`, `Checksum` for DDL scripts
- **`MigrationResult`**: Coordination result with `PerShardStatus`, computed `AllSucceeded`, `SucceededCount`, `FailedCount`
- **`MigrationProgress`**: In-flight tracking with `TotalShards`, `CompletedShards`, `FailedShards`, `CurrentShard`, computed `RemainingShards`, `IsFinished`
- **`ShardMigrationStatus`**: Per-shard state with `ShardId`, `Outcome` (Success/Failed/RolledBack/Skipped), `Duration`, optional `ErrorMessage`
- **`MigrationOptions`**: Per-migration configuration (strategy, parallelism, timeout, stop-on-first-failure, validation)

**Coordination Engine**:

- **`IShardedMigrationCoordinator`**: Main interface — `ApplyToAllShardsAsync`, `RollbackAsync`, `DetectDriftAsync`, `GetProgressAsync`, `GetAppliedMigrationsAsync`
- **`ShardedMigrationCoordinator`**: Full implementation with in-memory progress tracking, history table initialization, and per-shard error isolation

**Four Migration Strategies**:

- **Sequential** (`SequentialMigrationStrategy`): One shard at a time — safest, slowest
- **Parallel** (`ParallelMigrationStrategy`): All shards simultaneously with semaphore-based throttling
- **RollingUpdate** (`RollingUpdateStrategy`): Configurable batch size, balanced approach
- **CanaryFirst** (`CanaryFirstStrategy`): Apply to canary shard first, then parallel to rest

**Provider Abstractions**:

- **`IMigrationExecutor`**: Provider-specific DDL execution (`ExecuteSqlAsync`)
- **`IMigrationHistoryStore`**: Migration history tracking (`GetAppliedAsync`, `RecordAppliedAsync`, `RecordRolledBackAsync`, `EnsureHistoryTableExistsAsync`, `ApplyHistoricalMigrationsAsync`)
- **`ISchemaIntrospector`**: Provider-specific schema inspection for drift detection

**Schema Drift Detection**:

- **`SchemaComparer`**: Core comparison logic with three depths (TablesOnly, TablesAndColumns, Full)
- **`SchemaDriftReport`**: Aggregated drift across all shards with computed `HasDrift`
- **`ShardSchemaDiff`**: Per-shard drift result with table diffs
- **`TableDiff`**: Individual table difference (Missing, Extra, Modified) with optional column diffs
- **`DriftDetectionOptions`**: Configuration with baseline shard, comparison depth, critical table tracking

**Builder & DI**:

- **`MigrationCoordinationBuilder`**: Fluent builder — `UseStrategy()`, `WithMaxParallelism()`, `StopOnFirstFailure()`, `WithPerShardTimeout()`, `ValidateBeforeApply()`, `OnShardMigrated()`, `WithDriftDetection()`
- **`AddEncinaShardMigrationCoordination()`**: DI registration extension method on `IServiceCollection`

**Observability** (`Encina.OpenTelemetry`):

- 6 metric instruments: `shards_migrated_total` counter, `shards_failed_total` counter, `duration_per_shard_ms` histogram, `total_duration_ms` histogram, `drift_detected_count` observable gauge, `rollbacks_total` counter
- 3 trace activities via `MigrationActivitySource`: `StartMigrationCoordination`, `StartShardMigration`, `Complete` enrichment
- 14 activity tags under `ActivityTagNames.Migration`
- **`SchemaDriftHealthCheck`**: Reports Unhealthy/Degraded/Healthy based on drift in critical vs non-critical tables
- **`MigrationMetricsInitializer`**: Hosted service for metrics initialization

**Error Codes** (constants in `MigrationErrorCodes`):

- `NoActiveShards`, `MigrationFailed`, `RollbackFailed`, `DriftDetectionFailed`, `HistoryQueryFailed`

**Testing**: 169 tests across unit (47), guard (54), contract (26), property (31), integration (11) tests — integration tests use real SQLite databases via `ShardedSqliteFixture`

#### Online Resharding Workflow (#648)

Added online resharding with a 6-phase workflow (Planning → Copying → Replicating → Verifying → CuttingOver → CleaningUp), crash recovery, automatic rollback, and full observability integration.

**Core Architecture**:

- **`IReshardingOrchestrator`**: Orchestrates the full lifecycle with `PlanAsync`, `ExecuteAsync`, `RollbackAsync`, `GetProgressAsync`
- **`IReshardingStateStore`**: Persistent state for crash recovery (`SaveStateAsync`, `GetStateAsync`, `GetActiveReshardingsAsync`, `DeleteStateAsync`)
- **`IReshardingServices`**: Application-level data operations (`CopyBatchAsync`, `ReplicateChangesAsync`, `GetReplicationLagAsync`, `VerifyDataConsistencyAsync`, `SwapTopologyAsync`, `CleanupSourceDataAsync`, `EstimateRowCountAsync`) plus 3 result records (`CopyBatchResult`, `ReplicationResult`, `VerificationResult`)
- **`ReshardingPhaseExecutor`**: Sequential phase pipeline with `ValidateTransition`, crash recovery from last checkpoint
- **`ReshardingState`**: Immutable state record persisted across phases with `ReshardingCheckpoint` support

**6-Phase Workflow**:

- **Planning**: Uses `IShardRebalancer.CalculateAffectedKeyRanges` to generate `ShardMigrationStep[]`, estimates resources via `IReshardingServices.EstimateRowCountAsync`
- **Copying**: Batch data copy with configurable `CopyBatchSize` (default: 10,000), checkpoint resume after crash via `ReshardingCheckpoint.LastBatchPosition`
- **Replicating**: CDC-based catch-up with configurable `CdcLagThreshold` (default: 5s), multi-pass replication tracking via `ReshardingCheckpoint.CdcPosition`
- **Verifying**: Consistency verification with `VerificationMode` (RowCount, CountAndChecksum, Full)
- **CuttingOver**: Atomic topology swap with `OnCutoverStarting` predicate gate and `CutoverTimeout` (default: 30s)
- **CleaningUp**: Source data deletion with `CleanupRetentionPeriod` (default: 24h), best-effort (failures don't fail the workflow)

**Configuration**:

- **`ReshardingOptions`**: `CopyBatchSize` (10,000), `CdcLagThreshold` (5s), `VerificationMode` (CountAndChecksum), `CutoverTimeout` (30s), `CleanupRetentionPeriod` (24h), `OnPhaseCompleted` callback, `OnCutoverStarting` predicate
- **`ReshardingBuilder`**: Fluent API — `CopyBatchSize`, `CdcLagThreshold`, `VerificationMode`, `CutoverTimeout`, `CleanupRetentionPeriod`, `OnPhaseCompleted()`, `OnCutoverStarting()`
- **`WithResharding()`**: Configuration entry point on `ShardingOptions<TEntity>`

**Observability** (`Encina.OpenTelemetry`):

- `ReshardingMetrics`: 7 instruments — `phase_duration_ms` histogram, `rows_copied_total` counter, `rows_per_second` observable gauge, `cdc_lag_ms` observable gauge, `verification_mismatches_total` counter, `cutover_duration_ms` histogram, `active_resharding_count` observable gauge (via `ReshardingMetricsCallbacks`)
- `ReshardingActivitySource` ("Encina.Resharding"): 2 activities (`StartReshardingExecution`, `StartPhaseExecution`) with `Complete` enrichment
- `ReshardingActivityEnricher`: Static methods for enriching activities with plan and phase details
- `ReshardingHealthCheck`: Three-state health (Healthy: no active, Degraded: in-progress, Unhealthy: failed/overdue/timeout/error) with `ReshardingHealthCheckOptions` (MaxReshardingDuration: 2h, Timeout: 30s)
- `ReshardingLogMessages`: 10 source-generated structured log events with unique EventIds

**Error Codes** (16 stable codes under `encina.sharding.resharding.*`):

- `TopologiesIdentical`, `EmptyPlan`, `PlanGenerationFailed`, `CopyFailed`, `ReplicationFailed`, `VerificationFailed`, `CutoverTimeout`, `CutoverAborted`, `CutoverFailed`, `CleanupFailed`, `RollbackFailed`, `RollbackNotAvailable`, `ReshardingNotFound`, `InvalidPhaseTransition`, `StateStoreFailed`, `ConcurrentReshardingNotAllowed`

**Testing**: 342 tests across unit (252), guard (48), contract (18), property (12), integration (12) tests

#### CDC Dead Letter Queue (#631)

Added dead letter queue (DLQ) for CDC failed events, enabling persistence and later replay of change events that fail after exhausting all retries.

**Core Abstractions** (`Encina.Cdc`):

- **`ICdcDeadLetterStore`**: Interface for adding, querying, and resolving dead letter entries (ROP with `Either<EncinaError, T>`)
- **`CdcDeadLetterEntry`**: Sealed record with `Id`, `OriginalEvent`, `ErrorMessage`, `StackTrace`, `RetryCount`, `FailedAtUtc`, `ConnectorId`, `Status`
- **`CdcDeadLetterStatus`**: Enum with `Pending`, `Replayed`, `Discarded`
- **`CdcDeadLetterResolution`**: Enum with `Replay`, `Discard`
- **`InMemoryCdcDeadLetterStore`**: Default in-memory implementation (providers can override via DI)

**Configuration** (opt-in):

- `UseDeadLetterQueue()` fluent method on `CdcConfiguration`
- Registers `ICdcDeadLetterStore` (default: in-memory) and health check via `ServiceCollectionExtensions`

**Observability**:

- 4 new error codes: `DeadLetterStoreFailed`, `DeadLetterNotFound`, `DeadLetterAlreadyResolved`, `DeadLetterInvalidResolution`
- `CdcDeadLetterMetrics`: OpenTelemetry metrics for DLQ operations
- `CdcDeadLetterHealthCheck`: Health check with configurable warning/critical thresholds (`CdcDeadLetterHealthCheckOptions`)
- Log EventIds 210–213 for DLQ operations

**Processor Integration**:

- `CdcProcessor` accepts optional `ICdcDeadLetterStore?` — failed events are persisted after retry exhaustion
- Graceful degradation: DLQ store failures are logged and do not crash the processor

**Testing** (`Encina.Testing.Fakes`):

- `FakeCdcDeadLetterStore`: Thread-safe fake with verification helpers (`WasEventDeadLettered`, `GetEntries`, `GetEntriesByConnector`, `GetResolvedEntries`)
- 8 contract tests, 5 property-based tests, 6 unit tests (19 total)

#### Distributed Aggregation Helpers for Sharding (#640)

Added two-phase distributed aggregation operations (Count, Sum, Avg, Min, Max) across sharded repositories with mathematically correct combine logic.

**Core Abstractions** (`Encina` package):

- **`IShardedAggregationSupport<TEntity, TId>`**: Interface for providers implementing distributed aggregation
- **`ShardAggregatePartial<TValue>`**: Immutable record for per-shard intermediate results (Sum, Count, Min, Max)
- **`AggregationResult<T>`**: Final result record with `Value`, `ShardsQueried`, `FailedShards`, `Duration`, and `IsPartial`
- **`AggregationCombiner`**: Static class with five combine methods using mathematically correct two-phase aggregation
- **`ShardedAggregationExtensions`**: Extension methods on `IFunctionalShardedRepository<TEntity, TId>` for Count, Sum, Avg, Min, Max

**Key Design Decision**: Average uses `totalSum / totalCount` (not average-of-averages) to prevent incorrect results when shards have unequal row counts.

**Provider Support** (13 providers):

- ADO.NET (4): `BuildAggregationSql` + `GetColumnNameFromSelector` on `SpecificationSqlBuilder`, 5 aggregation methods on `FunctionalShardedRepositoryADO`
- Dapper (4): Same pattern with `IDictionary<string, object?>` parameters and `QuerySingleAsync` execution
- EF Core (4): LINQ-based aggregation via `DbContext.Set<T>()` with `CountAsync`, `SumAsync`, etc.
- MongoDB (1): `AggregationPipelineBuilder<TEntity>` with `$match`, `$group`, `$count` stages

**Observability**:

- 2 new metric instruments: `encina.sharding.aggregation.duration` (histogram), `encina.sharding.aggregation.partial_results` (counter)
- 2 new trace activities: `Encina.Sharding.Aggregation` (parent), `Encina.Sharding.ShardAggregation` (per-shard)
- `ShardingMetricsOptions.EnableAggregationMetrics` (default: `true`)
- 2 new error codes: `AggregationFailed`, `AggregationPartialFailure`

**Testing**: 105+ new unit tests covering AggregationCombiner, AggregationResult, ShardedAggregationExtensions, and diagnostics

**Documentation**: `docs/features/distributed-aggregations.md` — comprehensive guide with architecture, edge cases, and provider-specific details

#### Specification-Based Scatter-Gather for Sharding (#652)

Added specification-based scatter-gather queries across shards, enabling reuse of domain specifications for cross-shard operations with per-shard metadata, pagination, and observability.

**Core Abstractions** (`Encina.DomainModeling` package):

- **`IShardedSpecificationSupport<TEntity, TId>`**: Interface for providers implementing specification-based scatter-gather (4 methods)
- **`ShardedSpecificationExtensions`**: Extension methods on `IFunctionalShardedRepository` for `QueryAllShardsAsync`, `QueryAllShardsPagedAsync`, `CountAllShardsAsync`, `QueryShardsAsync`
- **`ScatterGatherResultMerger`**: Static class for merging per-shard results with ordering from specifications

**Result Types** (`Encina` package):

- **`ShardedSpecificationResult<T>`**: Merged results with `ItemsPerShard`, `DurationPerShard`, `FailedShards`, `IsComplete`, `IsPartial`
- **`ShardedPagedResult<T>`**: Cross-shard paginated results with `TotalCount`, `TotalPages`, `HasNextPage`, `HasPreviousPage`, `CountPerShard`
- **`ShardedCountResult`**: Lightweight count-only result with per-shard breakdown
- **`ShardedPaginationOptions`**: Configuration with `Page`, `PageSize`, and `Strategy`
- **`ShardedPaginationStrategy`**: Two strategies — `OverfetchAndMerge` (simple, correct) and `EstimateAndDistribute` (efficient, approximate)

**Provider Support** (13 providers via 10 implementations):

- ADO.NET (4): `FunctionalShardedRepositoryADO` implements `IShardedSpecificationSupport` — SQLite, SqlServer, PostgreSQL, MySQL
- Dapper (4): `FunctionalShardedRepositoryDapper` implements `IShardedSpecificationSupport` — SQLite, SqlServer, PostgreSQL, MySQL
- EF Core (4 via 1 generic): `FunctionalShardedRepositoryEF` implements `IShardedSpecificationSupport` — SQLite, SqlServer, PostgreSQL, MySQL
- MongoDB (1): `FunctionalShardedRepositoryMongoDB` implements `IShardedSpecificationSupport`

**Observability**:

- 4 new metric instruments: `encina.sharding.specification.queries_total` (counter), `encina.sharding.specification.merge.duration_ms` (histogram), `encina.sharding.specification.items_per_shard` (histogram), `encina.sharding.specification.shard_fan_out` (histogram)
- 3 new activity methods on `ShardingActivitySource`: `StartSpecificationScatterGather`, `SetPaginationContext`, `CompleteSpecificationScatterGather`
- `ShardingMetricsOptions.EnableSpecificationMetrics` (default: `true`)
- Structured logging with specification type, operation kind, and merge duration in all providers

**Testing**: 109+ tests across unit (67), guard (15), contract (14), property (13); 5 BenchmarkDotNet benchmarks for merge/pagination overhead

**Documentation**: `docs/features/specification-scatter-gather.md` — comprehensive guide with architecture, pagination strategies, and provider examples

#### Entity Co-Location Groups for Sharding (#647)

Added co-location group support for sharded entities, ensuring related entities (e.g., Order + OrderItem) are always stored on the same shard for efficient local JOINs and shard-local transactions.

**Core Abstractions** (`Encina` package):

- **`IColocationGroup`**: Interface exposing `RootEntity`, `ColocatedEntities`, and `SharedShardKeyProperty`
- **`ColocationGroup`**: Immutable sealed record implementing `IColocationGroup`
- **`ColocationGroupBuilder`**: Fluent builder with `WithRootEntity<T>()`, `AddColocatedEntity<T>()`, `WithSharedShardKeyProperty()`, `Build()`
- **`ColocationGroupRegistry`**: Thread-safe singleton registry with O(1) bidirectional lookups (entity → group, root → group)
- **`[ColocatedWith(typeof(Root))]`**: Declarative attribute for child entities (`AllowMultiple = false`, `Inherited = false`)
- **`ColocationViolationException`**: Startup validation exception with `RootEntityType`, `FailedEntityType`, `Reason`, and `ToEncinaError()`

**Router Integration** (all 5 routers):

- `HashShardRouter`, `RangeShardRouter`, `DirectoryShardRouter`, `GeoShardRouter`, `CompoundShardRouter`: all accept optional `ColocationGroupRegistry` in constructor
- `IShardRouter.GetColocationGroup(Type)`: new default interface method returning `IColocationGroup?` (default: `null`)
- `ShardTopology`: extended with `ColocationGroupRegistry` support

**Error Codes** (4 new):

- `ColocationEntityNotShardable`, `ColocationShardKeyMismatch`, `ColocationDuplicateRegistration`, `ColocationSelfReference`

**Observability** (`Encina.OpenTelemetry` package):

- `ColocationMetrics`: 3 metric instruments — `encina.sharding.colocation.groups_registered` (gauge), `encina.sharding.colocation.validation_failures_total` (counter), `encina.sharding.colocation.local_joins_total` (counter)
- 3 trace attribute constants: `encina.sharding.colocation.group`, `encina.sharding.colocation.is_colocated`, `encina.sharding.colocation.root_entity`
- `ColocationLog`: 5 source-generated log events (EventIds 620-624)

**Testing**: 97 new tests across unit (44), guard (8), contract (28), property (18)

**Documentation**: `docs/features/sharding-colocation.md` — comprehensive guide with configuration, validation rules, and observability

#### Change Data Capture (CDC) Pattern (#308)

Added a provider-agnostic Change Data Capture infrastructure that streams database changes as typed events through a handler pipeline, with support for 5 database providers and messaging integration.

**Core Components**:

- **`ICdcConnector`**: Provider abstraction for streaming `ChangeEvent` instances via `IAsyncEnumerable<Either<EncinaError, ChangeEvent>>`
- **`IChangeEventHandler<TEntity>`**: Typed handler interface for reacting to Insert, Update, and Delete operations
- **`ICdcDispatcher`**: Routes change events to handlers based on table-to-entity type mappings
- **`ICdcPositionStore`**: Persists stream position for resume after restart
- **`ICdcEventInterceptor`**: Cross-cutting concern invoked after successful dispatch
- **`CdcProcessor`**: `BackgroundService` with poll-dispatch-save loop, exponential backoff retry

**Configuration**:

```csharp
services.AddEncinaCdc(config =>
{
    config.UseCdc()
          .AddHandler<Order, OrderChangeHandler>()
          .WithTableMapping<Order>("dbo.Orders")
          .WithMessagingBridge(opts =>
          {
              opts.TopicPattern = "cdc.{tableName}.{operation}";
              opts.IncludeTables = ["Orders"];
          })
          .UseOutboxCdc("OutboxMessages");
});

services.AddEncinaCdcSqlServer(opts =>
{
    opts.ConnectionString = connectionString;
    opts.TrackedTables = ["dbo.Orders"];
});
```

**Provider Packages**:

| Package | CDC Mechanism | Position Type |
|---------|---------------|---------------|
| `Encina.Cdc` | Core abstractions + processing | `CdcPosition` (abstract) |
| `Encina.Cdc.SqlServer` | Change Tracking | `SqlServerCdcPosition` (version) |
| `Encina.Cdc.PostgreSql` | Logical Replication (WAL) | `PostgresCdcPosition` (LSN) |
| `Encina.Cdc.MySql` | Binary Log Replication | `MySqlCdcPosition` (GTID/binlog) |
| `Encina.Cdc.MongoDb` | Change Streams | `MongoCdcPosition` (resume token) |
| `Encina.Cdc.Debezium` | HTTP Consumer + Kafka Consumer | `DebeziumCdcPosition` / `DebeziumKafkaPosition` |

**Debezium Dual-Mode Support**:

- **HTTP Mode** (`AddEncinaCdcDebezium`): Receives events from Debezium Server via HTTP POST with bounded channel backpressure
- **Kafka Mode** (`AddEncinaCdcDebeziumKafka`): Consumes Debezium change events from Kafka topics with consumer group scaling
- Shared `DebeziumEventMapper` for consistent CloudEvents/Flat format parsing across both modes
- SASL/SSL security configuration for Kafka connections
- Mutual exclusivity via `TryAddSingleton` — first registered mode wins

**Messaging Integration**:

- **`CdcMessagingBridge`**: `ICdcEventInterceptor` that publishes `CdcChangeNotification` via `IEncina.Publish()`
- **`OutboxCdcHandler`**: CDC-driven outbox processing replacing polling-based `OutboxProcessor`
- **`CdcChangeNotification`**: `INotification` wrapper with topic name from configurable pattern
- **`CdcMessagingOptions`**: Table/operation filtering and topic pattern configuration

**Test Coverage** (498+ tests):

- ~232 unit tests, ~60 integration tests, ~69 guard tests, ~71 contract tests, ~66 property tests

---

#### CDC Per-Shard Connector (#646)

Added sharded CDC support that aggregates change streams from multiple database shards into a unified event pipeline, with per-shard position tracking and topology-aware health checks.

**Core Abstractions** (`Encina.Cdc` package):

- **`IShardedCdcConnector`**: Aggregates per-shard `ICdcConnector` instances into a unified stream via `StreamAllShardsAsync()` or per-shard via `StreamShardAsync()`
- **`IShardedCdcPositionStore`**: Persists positions per `(shardId, connectorId)` composite key with `GetPositionAsync`, `SavePositionAsync`, `DeletePositionAsync`, `GetAllPositionsAsync`
- **`ShardedChangeEvent`**: Record wrapping `ChangeEvent` with `ShardId` and `ShardPosition` for per-shard tracking
- **`ShardedCaptureOptions`**: Configuration for auto-discovery, processing mode, lag threshold, topology callbacks
- **`ShardedProcessingMode`**: Enum with `Aggregated` (cross-shard ordering) and `PerShardParallel` (independent streams)

**Processing & Infrastructure**:

- **`ShardedCdcConnector`**: Internal implementation using `Channel<T>` for aggregated streaming with `AddConnector`/`RemoveConnector` for runtime topology changes
- **`ShardedCdcProcessor`**: `BackgroundService` with poll-dispatch-save loop, exponential backoff retry, batch size control
- **`InMemoryShardedCdcPositionStore`**: `ConcurrentDictionary`-based implementation with case-insensitive `ToUpperInvariant()` composite keys

**Configuration** (via `CdcConfiguration.WithShardedCapture()`):

```csharp
services.AddEncinaCdc(config =>
{
    config.UseCdc()
          .AddHandler<Order, OrderChangeHandler>()
          .WithShardedCapture(opts =>
          {
              opts.AutoDiscoverShards = true;
              opts.ProcessingMode = ShardedProcessingMode.Aggregated;
              opts.MaxLagThreshold = TimeSpan.FromMinutes(5);
              opts.ConnectorId = "orders-sharded-cdc";
          });
});
```

**Health Check**: `ShardedCdcHealthCheck` (extends `EncinaHealthCheck`) — reports Healthy/Degraded/Unhealthy based on shard lag and connector status

**Error Codes** (2 new): `encina.cdc.shard_not_found`, `encina.cdc.shard_stream_failed`

**Observability** (`Encina.OpenTelemetry` package):

- `ShardedCdcMetrics`: 5 metric instruments — `encina.cdc.sharded.events_total` (counter), `encina.cdc.sharded.position_saves_total` (counter), `encina.cdc.sharded.errors_total` (counter), `encina.cdc.sharded.active_connectors` (gauge), `encina.cdc.sharded.lag_ms` (gauge)
- 2 trace attribute constants: `encina.cdc.shard.id`, `encina.cdc.operation`

**Test Coverage** (132 tests): 86 unit, 23 guard, 14 contract, 9 property

**Documentation**: [`docs/features/cdc-sharding.md`](docs/features/cdc-sharding.md) — comprehensive guide with configuration, position tracking, and observability

---

#### CDC-Driven Query Cache Invalidation (#632)

Added CDC-driven query cache invalidation that detects database changes from any source (other app instances, direct SQL, migrations, external microservices) and invalidates matching cache entries across all application instances via pub/sub broadcast.

**Core Components** (`Encina.Cdc` package):

- **`QueryCacheInvalidationOptions`**: Configuration for cache key prefix, pub/sub channel, table filtering, and explicit table-to-entity-type mappings
- **`QueryCacheInvalidationCdcHandler`**: Internal `IChangeEventHandler<JsonElement>` that translates CDC events into `RemoveByPatternAsync` calls with pattern `{prefix}:*:{entityType}:*`
- **`CdcTableNameResolver`**: Internal resolver with explicit mapping precedence (case-insensitive) and automatic schema stripping fallback (`dbo.Orders` → `Orders`)
- **`CacheInvalidationSubscriberService`**: `IHostedService` that subscribes to the pub/sub channel and invalidates local cache entries when messages arrive from other instances

**Configuration** (via `CdcConfiguration.WithCacheInvalidation()`):

```csharp
services.AddEncinaCdc(config =>
{
    config.UseCdc()
          .WithCacheInvalidation(opts =>
          {
              opts.CacheKeyPrefix = "sm:qc";
              opts.UsePubSubBroadcast = true;
              opts.PubSubChannel = "sm:cache:invalidate";
              opts.Tables = ["Orders", "Products"];
              opts.TableToEntityTypeMappings = new Dictionary<string, string>
              {
                  ["dbo.Orders"] = "Order",
                  ["dbo.Products"] = "Product"
              };
          });
});
```

**Health Check**: `CacheInvalidationSubscriberHealthCheck` (extends `EncinaHealthCheck`) — verifies pub/sub connectivity with diagnostic data (channel, prefix)

**Observability**:

- `CacheInvalidationActivitySource`: 7 trace methods under `Encina.Cdc.CacheInvalidation` ActivitySource (StartInvalidation, SetResolution, InvalidationCompleted/Failed/Skipped, StartBroadcast, CompleteBroadcast)
- `CacheInvalidationMetrics`: 3 counter instruments — `encina.cdc.cache.invalidations`, `encina.cdc.cache.broadcasts`, `encina.cdc.cache.errors`
- `CdcCacheInvalidationLog`: 16 source-generated log events (EventIds 150-165) covering invalidation, broadcast, subscriber lifecycle, and error scenarios

**Test Coverage** (48 tests): 19 unit, 4 guard, 9 contract, 9 property, 7 integration

**Documentation**: [`docs/features/cdc-cache-invalidation.md`](docs/features/cdc-cache-invalidation.md) — comprehensive guide with architecture, configuration, troubleshooting

---

#### Sharded Read/Write Separation (#644)

Added per-shard read/write separation with five replica selection strategies, health-aware routing, and staleness tolerance across all 13 database providers.

**Core Architecture**:

- **`IShardedReadWriteConnectionFactory`**: Unified factory for shard-aware read/write connections (non-generic for Dapper, generic `<TConnection>` for ADO.NET)
- **`IShardedReadWriteDbContextFactory<TContext>`**: EF Core variant with context-aware, explicit read, and explicit write modes
- **`IReplicaHealthTracker`**: Thread-safe health tracking with recovery delay, replication lag filtering, and three-state model (Healthy/Degraded/Unhealthy)
- **`ShardReplicaSelectorFactory`**: Creates and caches per-shard replica selectors based on strategy configuration
- **`ShardedReadWriteOptions`**: Configuration with global defaults and per-shard overrides for strategy, staleness, and health parameters

**Five Replica Selection Strategies** (`ReplicaSelectionStrategy` enum):

- **RoundRobin** (default): `Interlocked.Increment` — lock-free, even distribution
- **Random**: `Random.Shared.Next` — thread-safe, stateless
- **LeastLatency**: EMA smoothing (alpha=0.3) over `ConcurrentDictionary` — routes to fastest replica
- **LeastConnections**: `Interlocked` counters per replica — adapts to variable query durations
- **WeightedRandom**: Cumulative weight array + binary search — heterogeneous replica capacity

**Health & Failover**:

- `ReplicaHealthTracker`: `ConcurrentDictionary`-based with configurable recovery delay (`UnhealthyReplicaRecoveryDelay`) and replication lag filtering (`MaxAcceptableReplicationLag`)
- `ShardReplicaHealthCheck`: Aggregate health evaluation across all shards for ASP.NET Core health check integration
- Configurable fallback to primary when no healthy replicas are available (`FallbackToPrimaryWhenNoReplicas`)

**Staleness Tolerance**:

- Global: `ShardedReadWriteOptions.DefaultMaxStaleness`
- Per-query: `[StalenessOptions]` attribute on request/query types
- Interacts with health tracking to filter replicas exceeding acceptable replication lag

**Provider Support** (13 providers):

- ADO.NET (4): `AddEncinaADOShardedReadWrite()` — SqlServer, PostgreSQL, MySQL, SQLite
- Dapper (4): `AddEncinaDapperShardedReadWrite()` — non-generic `IShardedReadWriteConnectionFactory`
- EF Core (4): `AddEncinaEFCoreShardedReadWrite{Provider}<TContext>()` — SqlServer, PostgreSQL, MySQL, SQLite
- MongoDB (1): `AddEncinaMongoDBShardedReadWrite()` — `IShardedReadWriteMongoCollectionFactory`

**Observability**:

- 6 metric instruments under "Encina" meter: read/write operation counters, replica selection duration, failover counter, unhealthy replica gauge, replication lag histogram
- Structured logging with shard ID, strategy, and health state in all providers

**Documentation** (2 guides + 1 ADR):

- `docs/architecture/adr/012-sharded-read-write-separation.md` — Architecture Decision Record
- `docs/sharding/read-write-separation.md` — Comprehensive usage guide with configuration, strategies, staleness, health, and provider-specific setup

**Testing**: 289+ tests across unit (222), guard (33), contract (27), property (7); 10 BenchmarkDotNet benchmarks (5 single-threaded + 5 concurrent with ThreadingDiagnoser); load tests for distribution fairness

---

#### Reference Tables / Global Data Replication (#639)

Added reference table (broadcast table) replication for sharded deployments, automatically synchronizing small lookup tables from a primary shard to all target shards for local JOINs without cross-shard traffic.

**Core Architecture**:

- **`IReferenceTableReplicator`**: Main orchestrator for replicating reference table data from primary to all target shards with per-shard result tracking
- **`IReferenceTableRegistry`**: Immutable frozen-dictionary registry of configured reference tables, O(1) lookup by entity type
- **`IReferenceTableStore`**: Provider-agnostic interface for bulk upsert, read-all, and content hash operations on a single shard
- **`IReferenceTableStoreFactory`**: Creates per-shard store instances from connection strings
- **`IReferenceTableStateStore`**: Persists content hashes and last replication timestamps for change detection
- **`ReferenceTableHashComputer`**: XxHash64-based deterministic content hashing with PK-ordered serialization
- **`EntityMetadataCache`**: Reflection-based discovery and caching of `[Table]`, `[Key]`, `[Column]` attributes
- **`[ReferenceTable]`**: Marker attribute for entity classes, optional when using explicit registration

**Three Refresh Strategies** (`RefreshStrategy` enum):

- **CdcDriven**: Near-real-time replication via Change Data Capture — requires configured `ICdcConnector`
- **Polling** (default): Periodic hash-based change detection with configurable interval
- **Manual**: Explicit replication via `IReferenceTableReplicator.ReplicateAsync<T>()`

**Configuration**:

```csharp
options.AddReferenceTable<Country>(rt =>
{
    rt.RefreshStrategy = RefreshStrategy.Polling;
    rt.PrimaryShardId = "shard-0";
    rt.PollingInterval = TimeSpan.FromMinutes(10);
    rt.BatchSize = 500;
    rt.SyncOnStartup = true;
});
```

**Provider Support** (13 providers):

- ADO.NET (4): `ReferenceTableStoreADO` — SQLite (`INSERT OR REPLACE`), SqlServer (`MERGE`), PostgreSQL (`ON CONFLICT DO UPDATE`), MySQL (`ON DUPLICATE KEY UPDATE`)
- Dapper (4): `ReferenceTableStoreDapper` — same SQL dialects via Dapper execution
- EF Core (4): `ReferenceTableStoreEF<TContext>` — generic implementation using `DbContext`
- MongoDB (1): `ReferenceTableStoreMongoDB` — `BulkWriteAsync` with `ReplaceOneModel`

**Observability**:

- 5 metric instruments under "Encina" meter: `encina.reference_table.replications_total`, `encina.reference_table.replication_duration_ms`, `encina.reference_table.rows_synced_total`, `encina.reference_table.errors_total`, `encina.reference_table.active_replications`
- Activity enrichment via `ReferenceTableActivityEnricher` with 6 tag constants
- 15 stable error codes prefixed `encina.reference_table.*`

**Health Monitoring**:

- `ReferenceTableHealthCheck` with three-state model (Healthy/Degraded/Unhealthy) based on replication lag thresholds
- Configurable via `ReferenceTableHealthCheckOptions` (degraded: 1min, unhealthy: 5min defaults)

**Documentation** (4 guides + 1 ADR):

- `docs/architecture/adr/013-reference-tables.md` — Architecture Decision Record
- `docs/features/reference-tables.md` — Comprehensive feature guide
- `docs/guides/reference-tables-scaling.md` — Scaling guidance and capacity planning
- `docs/configuration/reference-tables.md` — Configuration reference

**Testing**: 247+ tests across unit (154), contract (38), guard (13), property (12), integration (30 — ADO + Dapper across 4 databases); load tests and BenchmarkDotNet benchmarks

---

#### Query Cache Interceptor - EF Core Second-Level Cache (#291)

Added an EF Core query caching interceptor that acts as a transparent second-level cache, caching query results at the database command level and automatically invalidating them on `SaveChanges`.

**Core Components**:

- **`QueryCacheInterceptor`**: `DbCommandInterceptor` + `ISaveChangesInterceptor` that intercepts EF Core queries, serves cached results via `CachedDataReader`, and invalidates affected cache entries when entities are saved
- **`DefaultQueryCacheKeyGenerator`**: SHA256-based key generator that extracts table names from SQL, maps them to entity types via `DbContext.Model`, and produces deterministic cache keys with format `{prefix}:{entity}:{hash}` or `{prefix}:{tenant}:{entity}:{hash}`
- **`CachedDataReader`**: Full `DbDataReader` implementation that serves cached `CachedQueryResult` data with `JsonElement` conversion support for all CLR types
- **`SqlTableExtractor`**: Compiled `[GeneratedRegex]` for provider-agnostic SQL table extraction from FROM/JOIN clauses (bracket, double-quote, backtick quoting)

**Configuration**:

```csharp
// Step 1: Register query caching services
services.AddQueryCaching(options =>
{
    options.Enabled = true;
    options.DefaultExpiration = TimeSpan.FromMinutes(5);
    options.KeyPrefix = "sm:qc";
    options.ExcludeType<AuditLog>(); // Skip caching for specific entities
});

// Step 2: Add interceptor to DbContext
optionsBuilder.UseQueryCaching(serviceProvider);
```

**Features**:

- Automatic cache invalidation on `SaveChanges` based on entity types
- Multi-tenant cache isolation via `IRequestContext.TenantId`
- Entity type exclusion for high-churn tables
- Configurable error handling (`ThrowOnCacheErrors`)
- Works with any `ICacheProvider` (Memory, Redis, Hybrid, etc.)

**New Types**:

| Type | Package | Purpose |
|------|---------|---------|
| `IQueryCacheKeyGenerator` | `Encina.Caching` | Interface for SQL command key generation |
| `QueryCacheKey` | `Encina.Caching` | Cache key record with entity type metadata |
| `QueryCacheInterceptor` | `Encina.EntityFrameworkCore` | EF Core interceptor for query caching |
| `DefaultQueryCacheKeyGenerator` | `Encina.EntityFrameworkCore` | Default key generator implementation |
| `QueryCacheOptions` | `Encina.EntityFrameworkCore` | Configuration options |
| `CachedDataReader` | `Encina.EntityFrameworkCore` | DbDataReader for cached results |
| `CachedQueryResult` | `Encina.EntityFrameworkCore` | Serializable cached result model |
| `CachedColumnSchema` | `Encina.EntityFrameworkCore` | Column metadata record |
| `QueryCachingExtensions` | `Encina.EntityFrameworkCore` | DI registration extensions |

**Test Coverage** (256 tests):

- 184 unit tests, 19 guard tests, 29 contract tests, 16 property tests, 8 integration tests
- 7 BenchmarkDotNet benchmarks (key generation, cache lookup, CachedDataReader)

---

#### Soft Delete & Temporal Tables Support (#285)

Added comprehensive soft delete pattern and SQL Server temporal tables support across all database providers.

**Soft Delete Pattern**:

- **Domain Modeling**: `ISoftDeletable` (read-only) and `ISoftDeletableEntity` (mutable) interfaces
- **Base Classes**: `SoftDeletableEntity<TId>`, `FullyAuditedEntity<TId>`, `SoftDeletableAggregateRoot<TId>`
- **EF Core**: `SoftDeleteInterceptor` for automatic delete-to-update conversion
- **Repository**: `ISoftDeleteRepository<TEntity, TId>` with specialized operations
- **Global Query Filters**: Automatic exclusion of soft-deleted entities

```csharp
// Configure soft delete in EF Core
services.AddEncinaEntityFrameworkCore<AppDbContext>(config =>
{
    config.UseSoftDelete = true;
    config.SoftDeleteOptions.TrackDeletedAt = true;
    config.SoftDeleteOptions.TrackDeletedBy = true;
});

// Entity implementation
public class Order : SoftDeletableEntity<OrderId>
{
    // Inherits IsDeleted, DeletedAtUtc, DeletedBy
}

// Repository operations
await repository.GetByIdWithDeletedAsync(id);    // Include soft-deleted
await repository.ListWithDeletedAsync(spec);     // Include soft-deleted
await repository.RestoreAsync(id);               // Restore entity
await repository.HardDeleteAsync(id);            // Permanent delete
```

**Temporal Tables** (SQL Server):

- **Point-in-Time Queries**: `GetAsOfAsync(id, timestamp)` - Entity state at a specific time
- **Entity History**: `GetHistoryAsync(id)` - Complete history with all versions
- **Time Range Queries**: `GetChangedBetweenAsync(start, end)` - Changes in a time window
- **Filtered Historical Queries**: `ListAsOfAsync(spec, timestamp)` - Historical queries with specifications

```csharp
// Configure temporal table
modelBuilder.Entity<Order>().ConfigureTemporalTable();

// Query historical state
var orderAtLastWeek = await repository.GetAsOfAsync(orderId, lastWeek);
var history = await repository.GetHistoryAsync(orderId);
var changes = await repository.GetChangedBetweenAsync(startDate, endDate);
```

**Pipeline Behavior**:

- `SoftDeleteQueryFilterBehavior<TRequest, TResponse>`: Automatic filter context setup
- `IIncludeDeleted`: Marker interface to bypass soft delete filtering
- `ISoftDeleteFilterContext`: Scoped service for filter state communication

**Provider Support**:

| Provider | Soft Delete | Temporal Tables |
|----------|:-----------:|:---------------:|
| EF Core (4 DBs) | ✅ | ✅ (SQL Server) |
| Dapper (4 DBs) | ✅ | N/A |
| ADO.NET (4 DBs) | ✅ | N/A |
| MongoDB | ✅ | N/A |

**New Types**:

| Type | Purpose |
|------|---------|
| `ISoftDeletable` | Read-only soft delete interface |
| `ISoftDeletableEntity` | Mutable soft delete interface |
| `SoftDeletableEntity<TId>` | Base entity with soft delete |
| `FullyAuditedEntity<TId>` | Base entity with audit + soft delete |
| `SoftDeletableAggregateRoot<TId>` | Aggregate root with soft delete |
| `SoftDeleteInterceptor` | EF Core SaveChanges interceptor |
| `SoftDeleteInterceptorOptions` | Interceptor configuration |
| `SoftDeleteRepositoryEF<TEntity, TId>` | EF Core soft delete repository |
| `ISoftDeleteRepository<TEntity, TId>` | Repository interface |
| `ITemporalRepository<TEntity, TId>` | Temporal query repository interface |
| `TemporalRepositoryEF<TEntity, TId>` | EF Core temporal repository |
| `SoftDeleteQueryFilterBehavior<,>` | Pipeline behavior for filtering |
| `ISoftDeleteFilterContext` | Scoped filter state |
| `IIncludeDeleted` | Marker to bypass soft delete |

**Tests Added**:

| Test Type | Count | Description |
|-----------|-------|-------------|
| Unit Tests | 28+ | Interceptor, repository, domain classes |
| Guard Tests | 24+ | Null parameter validation |
| Integration Tests | 11+ | Real database operations with SQL Server |

**Documentation**: See `docs/features/soft-delete.md` and `docs/features/temporal-tables.md` for complete guides.

**Related Issue**: [#285 - Soft Delete & Temporal Tables Support](https://github.com/dlrivada/Encina/issues/285)

---

#### Pagination Abstractions (#293)

Added comprehensive pagination abstractions for data access, supporting offset-based pagination with sorting capabilities.

**Core Types** (`Encina.DomainModeling`):

- **`PaginationOptions`**: Immutable record with `PageNumber` (1-based), `PageSize`, and computed `Skip`
- **`SortedPaginationOptions`**: Extends `PaginationOptions` with `SortBy` and `SortDescending`
- **`PagedResult<T>`**: Result record with computed navigation properties
- **`IPagedSpecification<T>`**: Interface for specification-based pagination
- **`PagedQuerySpecification<T>`**: Abstract base class combining specification and pagination

```csharp
// Basic pagination
var options = PaginationOptions.Default
    .WithPage(2)
    .WithSize(25);

var result = await repository.GetPagedAsync(options);
// result: Either<EncinaError, PagedResult<Entity>>

// With sorting
var sortedOptions = SortedPaginationOptions.Default
    .WithSort("CreatedAtUtc", descending: true)
    .WithPage(1)
    .WithSize(50);

// Using specifications
public class ActiveOrdersSpec : PagedQuerySpecification<Order>
{
    public ActiveOrdersSpec(PaginationOptions pagination) : base(pagination)
    {
        AddCriteria(o => o.Status == OrderStatus.Active);
        ApplyOrderByDescending(o => o.CreatedAtUtc);
    }
}

var spec = new ActiveOrdersSpec(PaginationOptions.Default.WithSize(20));
var result = await repository.GetPagedAsync(spec);
```

**PagedResult Properties**:

| Property | Type | Description |
|----------|------|-------------|
| `Items` | `IReadOnlyList<T>` | Current page items |
| `PageNumber` | `int` | Current page (1-based) |
| `PageSize` | `int` | Items per page |
| `TotalCount` | `int` | Total items across all pages |
| `TotalPages` | `int` | Computed total pages |
| `HasPreviousPage` | `bool` | Navigation helper |
| `HasNextPage` | `bool` | Navigation helper |
| `FirstItemIndex` | `int` | First item index (1-based) |
| `LastItemIndex` | `int` | Last item index (1-based) |
| `IsFirstPage` | `bool` | Navigation helper |
| `IsLastPage` | `bool` | Navigation helper |

**EF Core Extensions** (`Encina.EntityFrameworkCore`):

- `ToPagedResultAsync<T>()` - Convert `IQueryable<T>` to paginated result
- `ToPagedResultAsync<T, TResult>()` - With projection expression

```csharp
// Direct IQueryable usage
var result = await dbContext.Orders
    .Where(o => o.IsActive)
    .OrderByDescending(o => o.CreatedAtUtc)
    .ToPagedResultAsync(new PaginationOptions(1, 25));

// With projection
var dtos = await dbContext.Orders
    .ToPagedResultAsync(
        o => new OrderDto(o.Id, o.Total),
        new PaginationOptions(1, 25));
```

**Repository Integration**:

- `GetPagedAsync(PaginationOptions)` - Basic pagination
- `GetPagedAsync(Specification, PaginationOptions)` - With filter
- `GetPagedAsync(IPagedSpecification)` - Specification-based

All methods return `Either<EncinaError, PagedResult<T>>` for ROP integration.

**New Types**:

| Type | Purpose |
|------|---------|
| `PaginationOptions` | Base pagination parameters |
| `SortedPaginationOptions` | Pagination with sorting |
| `PagedResult<T>` | Pagination result with navigation |
| `IPagedSpecification<T>` | Specification interface for pagination |
| `IPagedSpecification<T, TResult>` | With projection |
| `PagedQuerySpecification<T>` | Abstract base class |
| `PagedQuerySpecification<T, TResult>` | With projection |
| `QueryablePagedExtensions` | EF Core extension methods |

**Tests Added**:

| Test Type | Count | Description |
|-----------|-------|-------------|
| Unit Tests | 221 | PaginationOptions, PagedResult, specs, extensions |
| Guard Tests | 25 | Null and invalid parameter validation |

**Related Issue**: [#293 - Pagination Abstractions](https://github.com/dlrivada/Encina/issues/293)

---

#### Cursor-Based Pagination Helpers (#336)

Added cursor-based (keyset) pagination with O(1) performance, bidirectional navigation, and composite key support.

**Core Types** (`Encina.DomainModeling`):

- **`CursorPaginationOptions`**: Immutable record with `Cursor`, `PageSize`, and `Direction`
- **`CursorDirection`**: Enum for `Forward` (default) and `Backward` navigation
- **`CursorPaginatedResult<T>`**: Result with `NextCursor`, `PreviousCursor`, and navigation flags
- **`ICursorPaginatedQuery<T>`**: CQRS pattern interface for cursor-paginated queries
- **`ICursorEncoder`**: Interface for cursor encoding/decoding
- **`Base64JsonCursorEncoder`**: URL-safe Base64+JSON implementation
- **`CursorEncodingException`**: Dedicated exception for cursor encoding errors

```csharp
// Basic usage with EF Core
var query = dbContext.Orders.OrderByDescending(o => o.CreatedAtUtc);

var result = await query.ToCursorPaginatedAsync(
    cursor: null,          // First page
    pageSize: 25,
    keySelector: o => o.CreatedAtUtc,
    cursorEncoder: encoder);

// result.Items           - Current page items
// result.NextCursor      - Use for next page
// result.HasNextPage     - Navigation flag
// result.PreviousCursor  - Use for previous page
// result.HasPreviousPage - Navigation flag

// Navigate to next page
var nextPage = await query.ToCursorPaginatedAsync(
    cursor: result.NextCursor,
    pageSize: 25,
    keySelector: o => o.CreatedAtUtc,
    cursorEncoder: encoder);

// Bidirectional navigation (go back)
var options = new CursorPaginationOptions(
    Cursor: nextPage.PreviousCursor,
    PageSize: 25,
    Direction: CursorDirection.Backward);

var previousPage = await query.ToCursorPaginatedAsync(
    options: options,
    keySelector: o => o.CreatedAtUtc,
    cursorEncoder: encoder);
```

**CursorPaginatedResult Properties**:

| Property | Type | Description |
|----------|------|-------------|
| `Items` | `IReadOnlyList<T>` | Current page items |
| `NextCursor` | `string?` | Cursor for next page |
| `PreviousCursor` | `string?` | Cursor for previous page |
| `HasNextPage` | `bool` | More items available forward |
| `HasPreviousPage` | `bool` | More items available backward |
| `IsEmpty` | `bool` | No items returned |

**Performance Comparison**:

| Pagination Type | Page 1 | Page 100 | Page 10,000 |
|----------------|--------|----------|-------------|
| Offset-based | O(n) | O(100n) | O(10,000n) |
| Cursor-based | O(n) | O(n) | O(n) |

*Cursor pagination maintains constant performance regardless of page position.*

**EF Core Extensions** (`Encina.EntityFrameworkCore`):

- `ToCursorPaginatedAsync()` - Ascending sort with simple key
- `ToCursorPaginatedDescendingAsync()` - Descending sort with simple key
- `ToCursorPaginatedCompositeAsync()` - Composite keys (e.g., `CreatedAtUtc` + `Id`)
- All methods support optional projection via `selector` parameter

```csharp
// With projection (efficient SELECT)
var dtos = await query.ToCursorPaginatedAsync(
    selector: o => new OrderDto(o.Id, o.Total),
    cursor: null,
    pageSize: 25,
    keySelector: o => o.CreatedAtUtc,
    cursorEncoder: encoder);

// Composite key for tie-breaking
var result = await query.ToCursorPaginatedCompositeAsync(
    cursor: null,
    pageSize: 25,
    keySelector: o => new { o.CreatedAtUtc, o.Id },
    cursorEncoder: encoder,
    keyDescending: [true, false]); // CreatedAtUtc DESC, Id ASC
```

**CQRS Pattern Integration**:

```csharp
// Query record with cursor parameters
public record GetOrdersQuery(string? Cursor, int PageSize = 20)
    : IRequest<CursorPaginatedResult<OrderDto>>,
      ICursorPaginatedQuery<GetOrdersQuery>
{
    public string? GetCursor() => Cursor;
    public int GetPageSize() => PageSize;
    public CursorDirection GetDirection() => CursorDirection.Forward;
}

// Extension methods
var options = query.ToPaginationOptions();
var isFirst = query.IsFirstPage();
```

**Dependency Injection**:

```csharp
// Register default encoder
services.AddCursorPagination();

// With custom JSON options
services.AddCursorPagination(new JsonSerializerOptions { ... });

// With custom encoder
services.AddCursorPagination<MyCustomEncoder>();
```

**New Types**:

| Type | Purpose |
|------|---------|
| `CursorDirection` | Navigation direction enum |
| `CursorPaginationOptions` | Cursor pagination parameters |
| `CursorPaginatedResult<T>` | Pagination result with cursors |
| `ICursorPaginatedQuery<T>` | CQRS query interface |
| `ICursorEncoder` | Cursor encoding abstraction |
| `Base64JsonCursorEncoder` | URL-safe encoder implementation |
| `CursorEncodingException` | Encoding error exception |
| `CursorPaginatedQueryExtensions` | Extension methods for queries |
| `QueryableCursorExtensions` | EF Core IQueryable extensions |

**GraphQL Relay Connection Support** (`Encina.GraphQL`):

| Type | Purpose |
|------|---------|
| `Connection<T>` | Relay-compliant connection with edges and page info |
| `Edge<T>` | Single item with cursor |
| `RelayPageInfo` | Navigation information (hasNext, hasPrevious, cursors) |
| `ConnectionExtensions` | Conversion methods (`ToConnection()`, `Map()`) |

```csharp
// Convert cursor result to GraphQL Connection
var connection = cursorResult.ToConnection();

// Map to DTOs
var dtoConnection = connection.Map(order => new OrderDto(order.Id, order.Total));
```

**ADO.NET Helpers** (`Encina.ADO.*`):

Added `CursorPaginationHelper<TEntity>` for all 4 ADO.NET providers with raw SQL performance:

```csharp
// Create helper with entity mapper
var helper = new CursorPaginationHelper<Order>(
    connection,
    cursorEncoder,
    reader => new Order
    {
        Id = reader.GetGuid(reader.GetOrdinal("Id")),
        CreatedAtUtc = reader.GetDateTime(reader.GetOrdinal("CreatedAtUtc")),
        Total = reader.GetDecimal(reader.GetOrdinal("Total"))
    });

// Simple key pagination
var result = await helper.ExecuteAsync<DateTime>(
    tableName: "Orders",
    keyColumn: "CreatedAtUtc",
    cursor: null,
    pageSize: 25,
    isDescending: true);

// Composite key for tie-breaking
var result = await helper.ExecuteCompositeAsync(
    tableName: "Orders",
    keyColumns: ["CreatedAtUtc", "Id"],
    cursor: null,
    pageSize: 25,
    keyDescending: [true, false]);
```

**Provider-Specific SQL**:

| Provider | Column Quoting | Row Limiting |
|----------|----------------|--------------|
| SQL Server | `[column]` | `TOP (n)` |
| PostgreSQL | `"column"` | `LIMIT n` |
| MySQL | `` `column` `` | `LIMIT n` |
| SQLite | `"column"` | `LIMIT n` |

**Tests Added**:

| Test Type | Count | Description |
|-----------|-------|-------------|
| Unit Tests | 277 | Domain types, encoder, extensions |
| Guard Tests | 15 | Null and invalid parameter validation |
| EF Core Tests | 17 | InMemory database integration |
| GraphQL Tests | 25 | Connection types and extensions |
| ADO.NET Tests | 52 | Parameter validation for all 4 providers |

**Documentation**: See `docs/features/cursor-pagination.md` for complete guide.

**Related Issue**: [#336 - Cursor-Based Pagination Helpers](https://github.com/dlrivada/Encina/issues/336)

---

#### Database Resilience (#290)

Added connection pool monitoring, database-aware circuit breakers, transient error detection, and connection warm-up across all 13 database providers.

**Core Abstractions** (`Encina`):

- **`IDatabaseHealthMonitor`**: Core interface for pool monitoring, health checks, and circuit breaker state
- **`ConnectionPoolStats`**: Immutable record with pool utilization (clamped 0-1), active/idle/total connections
- **`DatabaseHealthResult`**: Health check result with `Healthy`/`Degraded`/`Unhealthy` status and factory methods
- **`DatabaseResilienceOptions`**: Opt-in configuration for pool monitoring, circuit breaker, warm-up, and health check intervals
- **`DatabaseCircuitBreakerOptions`**: Failure threshold, sampling duration, break duration, minimum throughput

```csharp
// All resilience features are opt-in
services.AddEncinaEntityFrameworkCore<AppDbContext>(config =>
{
    config.UseResilience(options =>
    {
        options.EnablePoolMonitoring = true;
        options.EnableCircuitBreaker = true;
        options.CircuitBreaker.FailureThreshold = 0.3;
        options.CircuitBreaker.BreakDuration = TimeSpan.FromMinutes(1);
        options.WarmUpConnections = 5;
        options.HealthCheckInterval = TimeSpan.FromSeconds(30);
    });
});

// Monitor pool health
var stats = monitor.GetPoolStatistics();
if (stats.PoolUtilization > 0.9)
    logger.LogWarning("Pool nearing capacity: {Util:P0}", stats.PoolUtilization);

// Health check with circuit breaker awareness
var health = await monitor.CheckHealthAsync(ct);
if (health.Status == DatabaseHealthStatus.Unhealthy)
    logger.LogError("Database unreachable: {Desc}", health.Description);
```

**Polly Integration** (`Encina.Polly`):

- **`DatabaseCircuitBreakerPipelineBehavior<TRequest, TResponse>`**: Pipeline behavior with fast-path circuit check and Polly `ResiliencePipeline` per provider
- **`DatabaseTransientErrorPredicate`**: Identifies transient errors by exception type name (no hard assembly references) across SQL Server, PostgreSQL, MySQL, MongoDB, and SQLite

```csharp
// Register circuit breaker as pipeline behavior
services.AddDatabaseCircuitBreaker(options =>
{
    options.FailureThreshold = 0.3;
    options.MinimumThroughput = 20;
    options.IncludeTimeouts = true;
    options.IncludeConnectionFailures = true;
});
```

**Provider Support**:

| Provider | Pool Stats | Health Check | Pool Clear | Circuit Breaker |
|----------|:----------:|:------------:|:----------:|:---------------:|
| ADO.NET SqlServer | ✅ Full | ✅ | ✅ | ✅ |
| ADO.NET PostgreSQL | ✅ MaxPool | ✅ | ✅ | ✅ |
| ADO.NET MySQL | ✅ MaxPool | ✅ | ✅ | ✅ |
| ADO.NET SQLite | Empty | ✅ | No-op | ✅ |
| Dapper (4 DBs) | Same as ADO | ✅ | Same as ADO | ✅ |
| EF Core (4 DBs) | Empty | ✅ | No-op | ✅ |
| MongoDB | Topology | ✅ Ping | No-op | ✅ |

**New Types**:

| Type | Package | Purpose |
|------|---------|---------|
| `IDatabaseHealthMonitor` | Encina | Core monitoring interface |
| `ConnectionPoolStats` | Encina | Pool statistics snapshot |
| `DatabaseHealthResult` | Encina | Health check result |
| `DatabaseHealthStatus` | Encina | Health status enum |
| `DatabaseResilienceOptions` | Encina | Resilience configuration |
| `DatabaseCircuitBreakerOptions` | Encina | Circuit breaker settings |
| `DatabasePoolMetrics` | Encina | OpenTelemetry metrics |
| `DatabaseHealthMonitorBase` | Encina.Messaging | Abstract base for relational providers |
| `DatabaseCircuitBreakerPipelineBehavior<,>` | Encina.Polly | Pipeline behavior |
| `DatabaseTransientErrorPredicate` | Encina.Polly | Transient error detection |
| `SqliteDatabaseHealthMonitor` | Encina.ADO.Sqlite | SQLite monitor |
| `SqlServerDatabaseHealthMonitor` | Encina.ADO.SqlServer | SQL Server monitor |
| `PostgreSqlDatabaseHealthMonitor` | Encina.ADO.PostgreSQL | PostgreSQL monitor |
| `MySqlDatabaseHealthMonitor` | Encina.ADO.MySQL | MySQL monitor |
| `DapperSqliteDatabaseHealthMonitor` | Encina.Dapper.Sqlite | Dapper SQLite monitor |
| `DapperSqlServerDatabaseHealthMonitor` | Encina.Dapper.SqlServer | Dapper SQL Server monitor |
| `DapperPostgreSqlDatabaseHealthMonitor` | Encina.Dapper.PostgreSQL | Dapper PostgreSQL monitor |
| `DapperMySqlDatabaseHealthMonitor` | Encina.Dapper.MySQL | Dapper MySQL monitor |
| `EfCoreDatabaseHealthMonitor` | Encina.EntityFrameworkCore | EF Core monitor |
| `ConnectionPoolMonitoringInterceptor` | Encina.EntityFrameworkCore | EF Core connection interceptor |
| `MongoDbDatabaseHealthMonitor` | Encina.MongoDB | MongoDB monitor |

**Tests Added**:

| Test Type | Count | Description |
|-----------|-------|-------------|
| Unit Tests | 113 | Circuit breaker state, pipeline behavior, cache invalidation |
| Guard Tests | 22 | Null validation for all 10 monitors + supporting types |
| Contract Tests | 20 | API consistency across all providers |
| Property Tests | 19 | ConnectionPoolStats invariants (FsCheck) |
| Integration Tests | 15 | Real SQLite health checks |

**Documentation**: See `docs/features/database-resilience.md` for complete guide.

**Related Issue**: [#290 - Connection Pool Resilience](https://github.com/dlrivada/Encina/issues/290)

---

#### Optimistic Concurrency Abstractions (#287)

Added comprehensive optimistic concurrency control using Railway Oriented Programming (ROP) patterns for conflict detection and resolution.

**Core Interfaces**:

- `IVersioned`: Read-only interface with `long Version` property for integer-based versioning
- `IVersionedEntity`: Combines `IVersioned` with `IEntity<Guid>` for versioned entities
- `IConcurrencyAwareEntity`: Interface for row versioning with `byte[] RowVersion` (EF Core `[Timestamp]`)

**Conflict Detection**:

```csharp
// ConcurrencyConflictInfo captures all states for conflict resolution
public record ConcurrencyConflictInfo<TEntity>(
    TEntity CurrentEntity,      // What we loaded initially
    TEntity ProposedEntity,     // What we want to save
    TEntity? DatabaseEntity     // What's currently in DB (null if deleted)
) where TEntity : class
{
    public bool WasDeleted => DatabaseEntity is null;
}
```

**Built-in Conflict Resolvers**:

```csharp
// Last write wins - proposed entity replaces database state
var resolver = new LastWriteWinsResolver<Order>();

// First write wins - database state is preserved
var resolver = new FirstWriteWinsResolver<Order>();

// Custom merge - implement your own merge logic
public class OrderMergeResolver : MergeResolver<Order>
{
    protected override async Task<Either<EncinaError, Order>> MergeAsync(
        Order current, Order proposed, Order database)
    {
        // Custom merge logic here
        return proposed with { Version = database.Version + 1 };
    }
}
```

**ROP Integration**:

```csharp
// Create concurrency error with conflict info
var conflictInfo = new ConcurrencyConflictInfo<Order>(current, proposed, database);
var error = RepositoryErrors.ConcurrencyConflict(conflictInfo);

// Error includes entity type and conflict details
error.GetCode();    // Option.Some("Repository.ConcurrencyConflict")
error.GetDetails(); // Dictionary with EntityType, CurrentEntity, ProposedEntity, etc.
```

**Provider Support**:

| Provider | Versioning | Row Version | Conflict Info |
|----------|:----------:|:-----------:|:-------------:|
| EF Core (4 DBs) | ✅ | ✅ `[Timestamp]` | ✅ |
| Dapper (4 DBs) | ✅ | Manual SQL | ✅ |
| ADO.NET (4 DBs) | ✅ | Manual SQL | ✅ |
| MongoDB | ✅ | ✅ | ✅ |
| Marten | ✅ Event Stream | N/A | ✅ |

**Marten Event Stream Versioning**:

Marten uses event stream versioning (aggregate version equals total event count) rather than entity-level versioning. Conflict info includes `ExpectedVersion`, `AggregateVersion`, and `UncommittedEventCount`.

**New Types**:

| Type | Purpose |
|------|---------|
| `IVersioned` | Integer version interface |
| `IVersionedEntity` | Versioned entity interface |
| `IConcurrencyAwareEntity` | Row version interface |
| `ConcurrencyConflictInfo<T>` | Conflict state capture |
| `IConcurrencyConflictResolver<T>` | Resolver interface |
| `LastWriteWinsResolver<T>` | Proposed wins resolver |
| `FirstWriteWinsResolver<T>` | Database wins resolver |
| `MergeResolver<T>` | Custom merge base class |
| `RepositoryErrors.ConcurrencyConflict()` | Error factory methods |

**Tests Added**:

| Test Type | Count | Description |
|-----------|-------|-------------|
| Unit Tests | 79+ | Resolvers, conflict info, error creation |
| Guard Tests | 10+ | Null parameter validation |
| Integration Tests | 13+ | Real database operations (EF Core, Dapper) |

**Documentation**: See `docs/features/optimistic-concurrency.md` for complete guide.

**Related Issue**: [#287 - Optimistic Concurrency Abstractions](https://github.com/dlrivada/Encina/issues/287)

---

#### Causation and Correlation ID Tracking in Event Metadata (#321)

Added comprehensive event metadata tracking for Marten event sourcing, enabling end-to-end distributed tracing and causal chain reconstruction.

**Event Metadata Options**:

```csharp
services.AddEncinaMarten(options =>
{
    // Core tracking (enabled by default)
    options.Metadata.CorrelationIdEnabled = true;
    options.Metadata.CausationIdEnabled = true;
    options.Metadata.CaptureUserId = true;
    options.Metadata.CaptureTenantId = true;
    options.Metadata.CaptureTimestamp = true;

    // Optional features
    options.Metadata.CaptureCommitSha = true;
    options.Metadata.CommitSha = "abc123";
    options.Metadata.CaptureSemanticVersion = true;
    options.Metadata.SemanticVersion = "1.0.0";

    // Custom headers
    options.Metadata.CustomHeaders["Environment"] = "Production";
});
```

**Automatic Metadata Propagation**:

- Correlation ID automatically propagated from `IRequestContext`
- Causation ID defaults to correlation ID or can be set explicitly via metadata
- Custom metadata enrichers via `IEventMetadataEnricher` interface

**Query Capabilities**:

| Method | Description |
|--------|-------------|
| `GetEventsByCorrelationIdAsync()` | Find all events from a workflow |
| `GetEventsByCausationIdAsync()` | Find events caused by a specific event |
| `GetCausalChainAsync()` | Reconstruct event causation chains |
| `GetEventByIdAsync()` | Get single event with full metadata |

**OpenTelemetry Integration**:

```csharp
// Generic activity enrichment
EventMetadataActivityEnricher.EnrichWithCorrelationIds(activity, correlationId, causationId);
EventMetadataActivityEnricher.EnrichWithEvent(activity, eventId, streamId, ...);

// Marten-specific enrichment
MartenActivityEnricher.EnrichWithEvent(activity, eventWithMetadata);
MartenActivityEnricher.EnrichWithQueryResult(activity, queryResult);
MartenActivityEnricher.EnrichWithCausalChain(activity, chain, direction);
```

**Activity Tag Names**:

| Tag | Description |
|-----|-------------|
| `event.message_id` | Unique event identifier |
| `event.correlation_id` | Links related events across a workflow |
| `event.causation_id` | Links cause-effect relationships |
| `event.stream_id` | Aggregate/stream identifier |
| `event.type_name` | Event type name |
| `event.version` | Version within the stream |
| `event.sequence` | Global sequence number |
| `event.timestamp` | Event timestamp (ISO 8601) |

**New Types**:

| Type | Purpose |
|------|---------|
| `EventMetadataOptions` | Configuration for metadata capture |
| `IEventMetadataQuery` | Query interface for events by metadata |
| `MartenEventMetadataQuery` | Marten implementation of query interface |
| `IEventMetadataEnricher` | Custom metadata enrichment hook |
| `EventWithMetadata` | Event data with all metadata fields |
| `EventQueryOptions` | Pagination and filtering options |
| `EventQueryResult` | Paginated query results |
| `CausalChainDirection` | Ancestors or Descendants traversal |
| `EventMetadataActivityEnricher` | OpenTelemetry activity enricher |
| `MartenActivityEnricher` | Marten-specific activity enricher |

**Tests Added**:

| Test Type | Count | Description |
|-----------|-------|-------------|
| Unit Tests | 128+ | Options, enrichment, query validation |
| Integration Tests | 15+ | Metadata persistence, query capabilities |
| OpenTelemetry Tests | 44 | Activity enrichment |

**Documentation**: See `docs/features/event-metadata-tracking.md` for complete guide.

**Related Issue**: [#321 - Causation and Correlation ID Tracking](https://github.com/dlrivada/Encina/issues/321)

---

#### Audit Trail Pattern (IAuditableEntity) (#286)

Added comprehensive audit trail tracking for entity creation, modification, and soft delete operations across all 13 database providers.

**Granular Interfaces**:

| Interface | Property | Type | Description |
|-----------|----------|------|-------------|
| `ICreatedAtUtc` | `CreatedAtUtc` | `DateTime` | Creation timestamp |
| `ICreatedBy` | `CreatedBy` | `string?` | Creator user ID |
| `IModifiedAtUtc` | `ModifiedAtUtc` | `DateTime?` | Last modification timestamp |
| `IModifiedBy` | `ModifiedBy` | `string?` | Last modifier user ID |
| `IAuditableEntity` | All above | - | Composite interface for EF Core |
| `IAuditable` | All above (read-only) | - | For immutable records |
| `ISoftDeletable` | `IsDeleted`, `DeletedAtUtc`, `DeletedBy` | - | Soft delete tracking |

**Base Classes**:

| Class | Description |
|-------|-------------|
| `AuditedEntity<TId>` | Entity with audit fields |
| `AuditedAggregateRoot<TId>` | Aggregate with audit + concurrency |
| `FullyAuditedAggregateRoot<TId>` | Audit + soft delete |
| `AuditableAggregateRoot<TId>` | Immutable audit pattern |
| `SoftDeletableAggregateRoot<TId>` | Immutable soft delete |

**EF Core Automatic Tracking**:

```csharp
services.AddEncinaEntityFrameworkCore<AppDbContext>(config =>
{
    config.UseAuditing = true;
    config.AuditingOptions.TrackCreatedBy = true;
    config.AuditingOptions.TrackModifiedBy = true;
});
```

**Non-EF Core Explicit Helpers**:

```csharp
// Static helper
AuditFieldPopulator.PopulateForCreate(entity, userId, timeProvider);
AuditFieldPopulator.PopulateForUpdate(entity, userId, timeProvider);
AuditFieldPopulator.PopulateForDelete(entity, userId, timeProvider);

// Extension methods (per provider)
entity.WithAuditCreate(userId, timeProvider);
entity.WithAuditUpdate(userId, timeProvider);
```

**Optional Audit Log Store**:

```csharp
// Enable detailed audit logging
config.AuditingOptions.LogChangesToStore = true;
services.AddSingleton<IAuditLogStore, InMemoryAuditLogStore>();
```

**Provider Support**:

| Provider | Automatic (Interceptor/Repository) | Manual (Helpers) |
|----------|-----------------------------------|------------------|
| EF Core (4 DBs) | ✅ via `AuditInterceptor` | ✅ |
| Dapper (4 DBs) | ✅ via Repository (v0.12.0+) | ✅ |
| ADO.NET (4 DBs) | ✅ via Repository (v0.12.0+) | ✅ |
| MongoDB | ✅ via Repository (v0.12.0+) | ✅ |

**Tests Added**:

| Test Type | Count | Description |
|-----------|-------|-------------|
| Unit Tests | 135 | Interfaces, base classes, interceptor, utilities |
| Guard Tests | 19 | Null parameter validation |

**Documentation**: See `docs/features/audit-tracking.md` for complete guide.

**Related Issue**: [#286 - Audit Trail Pattern (IAuditableEntity)](https://github.com/dlrivada/Encina/issues/286)

---

#### Persistent IAuditLogStore Implementations for All 13 Database Providers (#574)

Added database-backed `IAuditLogStore` implementations for all 13 database providers, enabling persistent audit trail storage for production use.

**New Implementations**:

| Store Class | Package | Database |
|-------------|---------|----------|
| `AuditLogStoreEF` | `Encina.EntityFrameworkCore` | SQLite, SQL Server, PostgreSQL, MySQL |
| `AuditLogStoreDapper` | `Encina.Dapper.*` | SQLite, SQL Server, PostgreSQL, MySQL |
| `AuditLogStoreADO` | `Encina.ADO.*` | SQLite, SQL Server, PostgreSQL, MySQL |
| `AuditLogStoreMongoDB` | `Encina.MongoDB` | MongoDB |

**MongoDB Support**:

- New `AuditLogDocument` with BSON serialization attributes
- Optimized indexes for entity history lookups
- Sparse indexes for UserId and CorrelationId fields

**Configuration**:

```csharp
// EF Core
services.AddEncinaEntityFrameworkCore<AppDbContext>(config =>
{
    config.UseAuditLogStore = true;  // Registers AuditLogStoreEF
});

// MongoDB
services.AddEncinaMongoDB(config =>
{
    config.UseAuditLogStore = true;  // Registers AuditLogStoreMongoDB
});

// Dapper (auto-registered via UseAuditLogStore in options)
// ADO.NET (auto-registered via UseAuditLogStore in options)
```

**Tests Added**:

| Test Type | Count | Description |
|-----------|-------|-------------|
| Unit Tests | 45+ | All store implementations |
| Guard Tests | 20+ | Constructor and method null checks |

**Documentation**: Updated `docs/features/audit-tracking.md` with production configuration examples.

**Related Issue**: [#574 - Persistent IAuditLogStore Implementations](https://github.com/dlrivada/Encina/issues/574)

---

#### Automatic IAuditableEntity Field Population for Dapper, ADO.NET, and MongoDB (#623)

Extended automatic audit field population to 9 additional providers (4 Dapper, 4 ADO.NET, 1 MongoDB), bringing feature parity with EF Core's `AuditInterceptor`.

**Affected Providers**:

| Provider Category | Databases | Status |
|-------------------|-----------|--------|
| Dapper | SQLite, SQL Server, PostgreSQL, MySQL | ✅ Added |
| ADO.NET | SQLite, SQL Server, PostgreSQL, MySQL | ✅ Added |
| MongoDB | MongoDB | ✅ Added |

**How It Works**:

Repository classes now accept optional `IRequestContext?` and `TimeProvider?` constructor parameters. When provided, the repository automatically populates audit fields:

- `AddAsync` / `AddRangeAsync` → calls `AuditFieldPopulator.PopulateForCreate()`
- `UpdateAsync` / `UpdateRangeAsync` → calls `AuditFieldPopulator.PopulateForUpdate()`

**Configuration**:

```csharp
// DI registration (TimeProvider and IRequestContext are auto-resolved)
services.AddScoped<IRequestContext, HttpRequestContext>();
services.TryAddSingleton(TimeProvider.System);

// Repository automatically uses audit context
services.AddEncinaDapper(config => { ... });
```

**Backward Compatibility**:

- Constructor parameters are optional with default values
- Existing code continues to work without changes
- When `IRequestContext` is null, timestamps are still set (via `TimeProvider.System`)

**Excluded Providers**:

- **Marten**: Event sourcing with immutable events - audit is in event metadata
- **InMemory**: Messaging-only, no entity persistence

**Tests Added**:

| Test Type | Count | Description |
|-----------|-------|-------------|
| Unit Tests | 12 | Constructor validation across providers |
| Integration Tests | 28 | Full CRUD audit verification |

**Updated Repository Classes**:

- `FunctionalRepositoryDapper<TEntity, TId>` (4 providers)
- `UnitOfWorkRepositoryDapper<TEntity, TId>` (4 providers)
- `TenantAwareFunctionalRepositoryDapper<TEntity, TId>` (4 providers)
- `FunctionalRepositoryADO<TEntity, TId>` (4 providers)
- `UnitOfWorkRepositoryADO<TEntity, TId>` (4 providers)
- `TenantAwareFunctionalRepositoryADO<TEntity, TId>` (4 providers)
- `FunctionalRepositoryMongoDB<TEntity, TId>`
- `UnitOfWorkRepositoryMongoDB<TEntity, TId>`
- `TenantAwareFunctionalRepositoryMongoDB<TEntity, TId>`
- `BulkOperationsMongoDB<TEntity, TId>`

**Documentation**: Updated `docs/features/audit-tracking.md` with automatic population examples.

**Related Issue**: [#623 - Auto-populate IAuditableEntity fields for Dapper, ADO.NET, and MongoDB](https://github.com/dlrivada/Encina/issues/623)

---

#### Immutable Records Support for IUnitOfWork and IFunctionalRepository (#572)

Extended immutable record support to `IUnitOfWork` and `IFunctionalRepository` interfaces, providing a consistent API for updating immutable aggregates across all data access patterns.

**New APIs**:

| Interface | Method | Description |
|-----------|--------|-------------|
| `IUnitOfWork` | `UpdateImmutable<TEntity>()` | Sync update returning `Either<EncinaError, Unit>` |
| `IUnitOfWork` | `UpdateImmutableAsync<TEntity>()` | Async overload |
| `IFunctionalRepository<TEntity, TId>` | `UpdateImmutableAsync()` | Repository-level immutable update |

**Provider Support**:

| Provider | Support | Notes |
|----------|---------|-------|
| EF Core | Full | Automatic change tracking and event preservation |
| Dapper | `OperationNotSupported` | Use `ImmutableAggregateHelper` instead |
| ADO.NET | `OperationNotSupported` | Use `ImmutableAggregateHelper` instead |
| MongoDB | `OperationNotSupported` | Use `ImmutableAggregateHelper` instead |

**Non-EF Core Workflow**:

For providers without change tracking, use `ImmutableAggregateHelper.PrepareForUpdate()`:

```csharp
// For Dapper/ADO.NET/MongoDB
var order = await repository.GetByIdAsync(orderId, ct);
var shipped = order.Ship();

// PrepareForUpdate: copies events + tracks aggregate
ImmutableAggregateHelper.PrepareForUpdate(shipped, order, eventCollector);

await repository.UpdateAsync(shipped, ct);
await dispatchHelper.DispatchCollectedEventsAsync(ct);
```

**Usage with IUnitOfWork (EF Core)**:

```csharp
var order = await unitOfWork.Repository<Order, Guid>().GetByIdAsync(orderId, ct);
var shipped = order.Ship().WithPreservedEvents(order);
var result = unitOfWork.UpdateImmutable(shipped);
await unitOfWork.SaveChangesAsync(ct);
```

**Tests Added**:

| Test Type | Count | Description |
|-----------|-------|-------------|
| Unit Tests | 64 | `ImmutableAggregateHelper`, `UnitOfWorkEF`, error types |
| Guard Tests | 10 | Null parameter validation |
| Contract Tests | 14 | API consistency across providers |
| Property Tests | 16 | Invariants for event copying and tracking |
| Integration Tests | 4 | EF Core with SQLite |

**Documentation**:

- Updated `docs/features/immutable-domain-models.md` with IUnitOfWork and non-EF sections
- Updated `README.md` with IUnitOfWork example

**Related Issue**: [#572 - Immutable Records Support for IUnitOfWork and IFunctionalRepository](https://github.com/dlrivada/Encina/issues/572)

---

#### Immutable Domain Models Support for EF Core (#569)

Added utilities for using immutable C# records as domain entities with EF Core while preserving domain events through state transitions.

**The Problem**: When using C# record with-expressions to create modified copies of entities, EF Core's change tracker loses track of the entity, and domain events raised during state transitions are lost on the new instance.

**The Solution**: Two complementary extension methods:

| Method | Description |
|--------|-------------|
| `WithPreservedEvents<TAggregateRoot>()` | Copies domain events from original to new instance |
| `UpdateImmutable<TEntity>()` | Handles detach/attach with automatic event preservation |

**Usage Example**:

```csharp
// Define immutable aggregate root
public record Order : AggregateRoot<Guid>
{
    public required OrderStatus Status { get; init; }

    public Order Ship()
    {
        AddDomainEvent(new OrderShippedEvent(Id));
        return this with { Status = OrderStatus.Shipped };
    }
}

// Update with event preservation
var order = await context.Orders.FindAsync(orderId);
var shippedOrder = order!.Ship().WithPreservedEvents(order);
var result = context.UpdateImmutable(shippedOrder);
await context.SaveChangesAsync(); // Events dispatched automatically
```

**API**:

- `ImmutableUpdateExtensions.UpdateImmutable<TEntity>()` - Sync update returning `Either<EncinaError, Unit>`
- `ImmutableUpdateExtensions.UpdateImmutableAsync<TEntity>()` - Async overload
- `ImmutableUpdateExtensions.WithPreservedEvents<TAggregateRoot>()` - Event preservation helper
- `IAggregateRoot.CopyEventsFrom()` - Low-level event copying interface method

**Tests Added**:

| Test Type | Count | Description |
|-----------|-------|-------------|
| Unit Tests | 11 | `UpdateImmutable`, `WithPreservedEvents`, `CopyEventsFrom` |
| Guard Tests | 6 | Null argument validation |
| Integration Tests | 17 | SQLite, PostgreSQL, SQL Server, MySQL (4 skipped) |

**Documentation**:

- Added immutable records section to `README.md`
- Created `docs/features/immutable-domain-models.md` feature guide

**Related Issue**: [#569 - Immutable Records Support for EF Core and Domain Events](https://github.com/dlrivada/Encina/issues/569)

#### Distributed ID Generation (#638)

Added a multi-strategy distributed ID generation package (`Encina.IdGeneration`) with four algorithms, shard-aware generation, and full integration across all 13 database providers.

**New Package**: `Encina.IdGeneration`

**Four ID Strategies**:

- **Snowflake** (`SnowflakeIdGenerator`): 64-bit time-ordered IDs with configurable bit allocation (41 timestamp + 10 shard + 12 sequence = 63 bits). Thread-safe with lock-based sequencing, clock drift tolerance, and shard embedding via `IShardedIdGenerator<SnowflakeId>`
- **ULID** (`UlidIdGenerator`): 128-bit Crockford Base32 identifiers (48-bit timestamp + 80-bit random). Cryptographically random via `RandomNumberGenerator`, inherently thread-safe
- **UUIDv7** (`UuidV7IdGenerator`): RFC 9562 time-ordered UUIDs wrapping `System.Guid`. Timestamp in MSBs for B-tree index locality, cryptographically random
- **ShardPrefixed** (`ShardPrefixedIdGenerator`): String-based `{shardId}:{sequence}` format with pluggable sequence generation (ULID, UUIDv7, TimestampRandom) via `IShardedIdGenerator<ShardPrefixedId>`

**Strongly-Typed ID Types** (4 `readonly record struct`):

- **`SnowflakeId`**: Wraps `long`, implicit conversion to/from `long`, `Parse`/`TryParse`/`TryParseEither`, comparison operators
- **`UlidId`**: Wraps Crockford Base32, `NewUlid()` factory, `GetTimestamp()`, `ToGuid()`, implicit Guid conversion
- **`UuidV7Id`**: Wraps `Guid`, `NewUuidV7()` factory, `GetTimestamp()`, implicit Guid conversion
- **`ShardPrefixedId`**: String-based with `ShardId`/`Sequence` properties, `Parse` with delimiter support, implicit string conversion

**Core Abstractions** (`Encina` package):

- **`IIdGenerator<TId>`**: Non-sharded generation returning `Either<EncinaError, TId>`
- **`IShardedIdGenerator<TId>`**: Extends with `Generate(string shardId)` and `ExtractShardId(TId)` for shard-aware generation and reverse routing
- **`IdGenerationErrorCodes`**: 4 stable error codes (`clock_drift_detected`, `sequence_exhausted`, `invalid_shard_id`, `id_parse_failure`)
- **`IdGenerationErrors`**: Factory methods with consistent error metadata

**Provider Type Mapping** (13 providers):

- ADO.NET (4): `IdParameterExtensions` with `AddSnowflakeId`, `AddUlidId`, `AddUuidV7Id`, `AddShardPrefixedId` for SQLite, SqlServer, PostgreSQL, MySQL
- Dapper (4): Type handlers (`SnowflakeIdTypeHandler`, `UlidIdTypeHandler`, `UuidV7IdTypeHandler`, `ShardPrefixedIdTypeHandler`) for each database
- EF Core (4): Value converters (`SnowflakeIdValueConverter`, `UlidIdValueConverter`, `UuidV7IdValueConverter`, `ShardPrefixedIdValueConverter`) registered via `ModelBuilderExtensions`
- MongoDB (1): BSON serializers (`SnowflakeIdBsonSerializer`, `UlidIdBsonSerializer`, `UuidV7IdBsonSerializer`, `ShardPrefixedIdBsonSerializer`)

**Observability**:

- 4 metric instruments via `IdGenerationMetrics`: `encina.idgen.generated` (counter), `encina.idgen.collisions` (counter), `encina.idgen.duration_ms` (histogram), `encina.idgen.sequence_exhausted` (counter)
- 5 trace activities via `IdGenerationActivitySource`: `StartIdGeneration`, `StartShardExtraction`, `Complete`, `CompleteShardExtraction`, `Failed`
- Source-generated structured logging via `IdGenerationLog`

**Health Check**:

- `IdGeneratorHealthCheck` extending `EncinaHealthCheck` with clock drift monitoring and Snowflake sequence validation
- `IdGeneratorHealthCheckOptions` with configurable `ClockDriftThresholdMs`

**Configuration**:

```csharp
services.AddEncinaIdGeneration(options =>
{
    options.UseSnowflake(sf => { sf.MachineId = 1; sf.EpochStart = new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero); });
    options.UseUlid();
    options.UseUuidV7();
    options.UseShardPrefixed(sp => { sp.Format = ShardPrefixedFormat.Ulid; });
});
```

**Testing**: 265+ unit/guard/contract/property tests; 14 integration test files across all providers; 7 NBomber load test scenarios; 20 BenchmarkDotNet benchmarks

**Documentation** (4 guides + 1 ADR):

- `docs/architecture/adr/011-id-generation-multi-strategy.md` — Architecture Decision Record
- `docs/features/id-generation.md` — Feature overview and architecture
- `docs/guides/id-generation-configuration.md` — Strategy comparison and configuration guide
- `docs/guides/id-generation-scaling.md` — Scaling, machine ID allocation, and migration

---

### Changed

#### Replace DateTime.UtcNow with TimeProvider Injection (#543)

Replaced all ~205 occurrences of `DateTime.UtcNow` across ~112 source files with `TimeProvider` injection, enabling deterministic time control in tests. This is a **breaking change** for classes with public constructors that now accept an optional `TimeProvider?` parameter.

**Pattern Applied**:

```csharp
// Before
var now = DateTime.UtcNow;

// After (constructor injection)
public SomeClass(..., TimeProvider? timeProvider = null)
{
    _timeProvider = timeProvider ?? TimeProvider.System;
}
var now = _timeProvider.GetUtcNow().UtcDateTime;
```

**Packages Affected** (all source packages):

| Category | Packages |
|----------|----------|
| **Encina.Messaging** | SagaOrchestrator, SchedulerOrchestrator, RecoverabilityPipelineBehavior, RecoverabilityContext, DeadLetterOrchestrator, InboxOrchestrator, OutboxOrchestrator, ContentRouter, OutboxPostProcessor, DelayedRetryScheduler, RoutingSlipRunner, health checks |
| **Encina.EntityFrameworkCore** | OutboxProcessor, SagaStoreEF, ScheduledMessageStoreEF, QueryCacheInterceptor, InboxStoreEF, OutboxStoreEF, TemporalRepositoryEF |
| **Encina.MongoDB** | SagaStoreMongoDB, ScheduledMessageStoreMongoDB, InboxStoreMongoDB, OutboxStoreMongoDB |
| **ADO.NET (4 providers)** | OutboxProcessor (retry time calculation) |
| **Dapper (4 providers)** | OutboxProcessor, SagaStoreDapper, ScheduledMessageStoreDapper |
| **Encina.Caching** | DistributedIdempotencyPipelineBehavior, QueryCachingPipelineBehavior |
| **Encina.Caching.Redis** | RedisCacheProvider, RedisDistributedLockProvider |
| **Encina.DistributedLock** | RedisDistributedLockProvider, SqlServerDistributedLockProvider, InMemoryDistributedLockProvider |
| **Encina.Cdc** | All 5 CDC connectors + DebeziumEventMapper |
| **Encina.DomainModeling** | DomainEventEnvelope, IDomainEvent, IIntegrationEvent, AuditLogEntry, InMemoryAuditLogStore, pagination cursors |
| **Encina.Marten** | MartenProjectionManager, SnapshotAwareAggregateRepository |
| **Encina.Redis.PubSub** | RedisPubSubMessagePublisher |
| **Encina.Security.Audit** | IAuditStore, AuditQuery |
| **Encina.Aspire.Testing** | FailureSimulationExtensions |
| **Encina.Testing.*** | Fakes (stores, models, providers), Bogus, FsCheck |

**DI Registration**: Added `services.TryAddSingleton(TimeProvider.System)` to all `ServiceCollectionExtensions` entry points.

**Model Classes**: ADO, Dapper, EF Core, and MongoDB model classes (`InboxMessage`, `ScheduledMessage`) use `TimeProvider.System` directly in `IsExpired()`/`IsDue()` methods (parameterless interface contract).

**Related Issue**: [#543 - Replace DateTime.UtcNow with TimeProvider injection](https://github.com/dlrivada/Encina/issues/543)

---

### Fixed

#### PostgreSQL EF Core Integration Tests Case-Sensitivity (#570)

Fixed PostgreSQL integration tests failing with "relation does not exist" errors due to PostgreSQL's case-sensitivity with table names.

**Root Cause**: PostgreSQL and MySQL schema files were missing `CreateTestRepositorySchemaAsync` methods, and SQL Server used an inconsistent table name (`TestEntities` instead of `TestRepositoryEntities`).

**Changes**:

- Added `CreateTestRepositorySchemaAsync` to `PostgreSqlSchema.cs` with quoted identifiers (`"TestRepositoryEntities"`)
- Added `CreateTestRepositorySchemaAsync` to `MySqlSchema.cs` with backtick quoting (`` `TestRepositoryEntities` ``)
- Renamed SQL Server table from `TestEntities` to `TestRepositoryEntities` for consistency
- Updated cleanup methods (`DropAllSchemasAsync`, `ClearAllDataAsync`) in all schema files
- Updated fixtures (`PostgreSqlFixture`, `MySqlFixture`) to call the new schema creation methods
- Re-enabled 5 PostgreSQL UnitOfWork integration tests that were previously skipped

**Related Issue**: [#570 - EF Core PostgreSQL integration tests fail due to table name case-sensitivity](https://github.com/dlrivada/Encina/issues/570)

---

### Added

#### Domain Entity Base Classes with Domain Events Support (#292)

Enhanced domain modeling capabilities with comprehensive domain event support and optimistic concurrency control for aggregate roots.

**Entity Hierarchy**:

| Class | Inherits From | Features |
|-------|---------------|----------|
| `Entity<TId>` | - | Identity, equality, domain events |
| `AggregateRoot<TId>` | `Entity<TId>` | Domain events + `IConcurrencyAware` (RowVersion) |
| `AuditableAggregateRoot<TId>` | `AggregateRoot<TId>` | + `IAuditable` (CreatedAt/By, ModifiedAt/By) |
| `SoftDeletableAggregateRoot<TId>` | `AuditableAggregateRoot<TId>` | + `ISoftDeletable` (IsDeleted, DeletedAt/By) |

**Domain Event Features**:

- `AddDomainEvent()` / `RaiseDomainEvent()` - Raise events from entities
- `DomainEvents` - Read-only collection of pending events
- `ClearDomainEvents()` - Clear events after dispatch
- `IDomainEvent` interface with `EventId` and `OccurredAtUtc`

**Optimistic Concurrency**:

- `IConcurrencyAware` interface with `RowVersion` property
- All `AggregateRoot<TId>` variants implement `IConcurrencyAware`
- EF Core configuration helper: `ConfigureConcurrencyToken()`

**Non-EF Core Provider Support**:

- `IDomainEventCollector` - Manual aggregate tracking for Dapper/ADO.NET/MongoDB
- `DomainEventCollector` - Default implementation (scoped lifetime)
- `DomainEventDispatchHelper` - Dispatch events via `IPublisher`
- `DomainEventDispatchErrors` - Structured error factory for dispatch failures

**EF Core Integration**:

- `DomainEventDispatcherInterceptor` - Auto-dispatch after SaveChanges
- `DomainEventDispatcherOptions` - Configure `Enabled`, `StopOnFirstError`, `RequireINotification`
- `AddDomainEventDispatcher()` - Service registration
- `UseDomainEventDispatcher()` - DbContext interceptor configuration
- Entity configuration helpers: `ConfigureAuditProperties()`, `ConfigureSoftDelete()`, `ConfigureAggregateRoot()`

**Tests Added**:

| Test Type | Count | Description |
|-----------|-------|-------------|
| Unit Tests | 97 | Entity events, concurrency, collector, dispatcher |
| Contract Tests | 20 | Aggregate root variants API contracts |

**Documentation**:

- Updated `Encina.DomainModeling/README.md` with domain events and concurrency sections
- Updated `Encina.EntityFrameworkCore/README.md` with dispatcher and configuration helpers

**Related Issue**: [#292 - Domain Entity Base Classes (Entity<TId>, AggregateRoot<TId>)](https://github.com/dlrivada/Encina/issues/292)

---

#### EntityFrameworkCore BenchmarkDotNet Micro-Benchmarks (#564)

Implemented comprehensive BenchmarkDotNet micro-benchmarks for Encina.EntityFrameworkCore data access components, measuring hot path performance and abstraction overhead.

**Benchmark Classes**:

| Class | Benchmarks | Focus |
|-------|------------|-------|
| `TransactionBehaviorBenchmarks` | 10 | Transaction detection and lifecycle |
| `SpecificationEvaluatorBenchmarks` | 14 | Query expression building |
| `FunctionalRepositoryBenchmarks` | 11 | CRUD operations |
| `UnitOfWorkBenchmarks` | 12 | Repository caching, transactions |
| `UnitOfWorkRepositoryBenchmarks` | 12 | Deferred vs immediate persistence |
| `BulkOperationsBenchmarks` | 14 | Factory overhead, provider detection |

**Performance Targets**:

| Category | Operation | Target |
|----------|-----------|--------|
| Transaction | Non-transactional passthrough | <1μs |
| Transaction | Interface detection | <100ns |
| Specification | Simple predicate | <100ns |
| Specification | Keyset pagination | 1-5μs |
| Repository | GetByIdAsync (cache hit) | <1μs |
| UnitOfWork | Repository cache hit | <100ns |

**Infrastructure**:
- `BenchmarkEntity` implementing `IEntity<Guid>`
- `EntityFrameworkBenchmarkDbContext` with InMemory/SQLite support
- `TestData` factory methods for test data generation

**Running Benchmarks**:
```bash
cd tests/Encina.BenchmarkTests/Encina.Benchmarks
dotnet run -c Release -- --filter "*EntityFrameworkCore*"
```

**Related Issue**: [#564 - Implement BenchmarkTests for Encina.EntityFrameworkCore data access](https://github.com/dlrivada/Encina/issues/564)

---

#### Dapper and ADO.NET BenchmarkDotNet Micro-Benchmarks (#568)

Implemented comprehensive BenchmarkDotNet micro-benchmarks for Encina.Dapper and Encina.ADO data access providers, measuring messaging store operations, repository pattern overhead, and direct provider comparison.

**Benchmark Projects**:

| Project | Benchmarks | Focus |
|---------|------------|-------|
| `Encina.Dapper.Benchmarks` | ~62 | Messaging stores, Repository, SQL Builder, Dapper vs ADO |
| `Encina.ADO.Benchmarks` | ~52 | Messaging stores, Repository, SQL Builder |

**Benchmark Classes**:

| Class | Provider | Description |
|-------|----------|-------------|
| `OutboxStoreBenchmarks` | Both | Outbox CRUD and batch operations |
| `InboxStoreBenchmarks` | Both | Inbox idempotency checks |
| `SagaStoreBenchmarks` | Both | Saga state persistence |
| `ScheduledMessageStoreBenchmarks` | Both | Due message queries |
| `RepositoryBenchmarks` | Both | Repository vs raw data access |
| `SpecificationSqlBuilderBenchmarks` | Both | SQL generation from specifications |
| `DapperVsAdoComparisonBenchmarks` | Dapper | Direct provider comparison |

**Performance Comparison (Dapper vs ADO.NET)**:

| Operation | Dapper | ADO.NET | Overhead |
|-----------|--------|---------|----------|
| Single write | 50-200 μs | 40-150 μs | ~5-15% |
| Batch read (100) | 200-500 μs | 150-400 μs | ~10-20% |
| Exists check | 20-100 μs | 15-80 μs | ~5-15% |

**Database Providers Supported**:
- SQLite, SQL Server, PostgreSQL, MySQL (8 total: 4 Dapper + 4 ADO.NET)

**Running Benchmarks**:
```bash
# Dapper benchmarks
cd tests/Encina.BenchmarkTests/Encina.Dapper.Benchmarks
dotnet run -c Release

# ADO.NET benchmarks
cd tests/Encina.BenchmarkTests/Encina.ADO.Benchmarks
dotnet run -c Release

# Direct comparison
dotnet run -c Release -- --filter "*DapperVsAdo*"
```

**Related Issue**: [#568 - Implement BenchmarkTests for Dapper and ADO.NET provider stores](https://github.com/dlrivada/Encina/issues/568)

---

#### Database Load Testing Infrastructure with NBomber (#538)

Implemented comprehensive load testing for database features using NBomber, enabling performance validation for concurrent database operations.

**Load Test Scenarios**:

| Feature | Scenarios | Description |
|---------|-----------|-------------|
| Unit of Work | 3 | Transaction management, rollback, connection pool pressure |
| Multi-Tenancy | 2 | Tenant isolation, context switching (100 tenants) |
| Read/Write Separation | 3 | Replica distribution, round-robin, least-connections |

**Provider Support**:
- All 13 database providers supported (ADO.NET x4, Dapper x4, EF Core x4, MongoDB)
- Provider-specific thresholds for realistic performance expectations
- Feature-specific scenarios with appropriate load simulations

**CI/CD Integration**:
- New `run-database-load-tests` job in GitHub Actions
- Matrix strategy for testing multiple providers
- Service containers for SQL Server, PostgreSQL, MySQL, MongoDB
- Runs on schedule (Saturday 2 AM UTC) or manual dispatch

**Configuration Files**:
- `tests/Encina.LoadTests/profiles/nbomber.database-*.json` - Profile configurations
- `ci/nbomber-database-thresholds.json` - Performance thresholds
- `docs/testing/load-test-baselines.md` - Expected performance documentation

**Related Issue**: [#538 - Implement LoadTests for concurrent database features](https://github.com/dlrivada/Encina/issues/538)

---

#### Comprehensive Integration Tests with Docker/Testcontainers (#537)

Replaced 23 justification `.md` files with comprehensive integration tests using Docker/Testcontainers for all database providers.

**Test Coverage by Provider**:

| Provider | Repository | Tenancy | Module Isolation | Read/Write Separation |
|----------|------------|---------|------------------|----------------------|
| ADO.NET (4 providers) | ✅ | ✅ | ✅ | ✅ |
| Dapper (4 providers) | ✅ | ✅ | ✅ | ✅ |
| EF Core (4 providers) | ✅ | ✅ | ✅ | ✅ |
| MongoDB | ✅ | ✅ | N/A | N/A |

**Total Tests Added**: ~479 integration tests across all providers

**Key Features Tested**:

- **Repository**: CRUD operations with real database transactions
- **Multi-Tenancy**: Tenant isolation, auto-assignment, cross-tenant access prevention
- **Module Isolation**: Schema/prefix isolation, development mode enforcement
- **Read/Write Separation**: Connection routing, replica selection, routing scopes

**Related Issue**: [#537 - Integration Tests Implementation](https://github.com/dlrivada/Encina/issues/537)

---

#### Multi-Tenancy Support for Remaining Providers (#282)

Extended Multi-Tenancy support to all 8 remaining database providers, enabling SaaS applications with tenant isolation capabilities.

**Providers Implemented**:

| Provider | Package | Tenant-Aware Repository | Connection Factory |
|----------|---------|------------------------|-------------------|
| ADO.SQLite | `Encina.ADO.Sqlite` | `TenantAwareFunctionalRepositoryADO` | `TenantConnectionFactory` |
| ADO.PostgreSQL | `Encina.ADO.PostgreSQL` | `TenantAwareFunctionalRepositoryADO` | `TenantConnectionFactory` |
| ADO.MySQL | `Encina.ADO.MySQL` | `TenantAwareFunctionalRepositoryADO` | `TenantConnectionFactory` |
| ADO.Oracle | `Encina.ADO.Oracle` | `TenantAwareFunctionalRepositoryADO` | `TenantConnectionFactory` |
| Dapper.SQLite | `Encina.Dapper.Sqlite` | `TenantAwareFunctionalRepositoryDapper` | `TenantConnectionFactory` |
| Dapper.PostgreSQL | `Encina.Dapper.PostgreSQL` | `TenantAwareFunctionalRepositoryDapper` | `TenantConnectionFactory` |
| Dapper.MySQL | `Encina.Dapper.MySQL` | `TenantAwareFunctionalRepositoryDapper` | `TenantConnectionFactory` |
| Dapper.Oracle | `Encina.Dapper.Oracle` | `TenantAwareFunctionalRepositoryDapper` | `TenantConnectionFactory` |

**Key Components per Provider**:

- `TenantAwareFunctionalRepositoryADO<TEntity, TId>` / `TenantAwareFunctionalRepositoryDapper<TEntity, TId>` - Auto-filtering repository
- `TenantEntityMappingBuilder<TEntity, TId>` - Fluent API for tenant entity mapping with `HasTenantId()`
- `ITenantEntityMapping<TEntity, TId>` - Extended mapping interface with tenant properties
- `TenantAwareSpecificationSqlBuilder<TEntity>` - SQL builder with automatic tenant filters
- `TenantConnectionFactory` - Tenant-aware connection routing
- `ADOTenancyOptions` / `DapperTenancyOptions` - Configuration options

**Multi-Tenancy Features**:

- **Auto-filtering**: All queries automatically include `WHERE TenantId = @TenantId`
- **Auto-assignment**: New entities automatically get tenant ID assigned on insert
- **Cross-tenant validation**: Updates/deletes verify tenant ownership
- **Database-per-tenant**: Route connections based on tenant isolation strategy

**Usage Example**:

```csharp
services.AddEncinaADOSqliteWithTenancy(
    config => { config.UseOutbox = true; },
    tenancy =>
    {
        tenancy.AutoFilterTenantQueries = true;
        tenancy.AutoAssignTenantId = true;
        tenancy.ValidateTenantOnModify = true;
    });

services.AddTenantAwareRepository<Order, Guid>(mapping =>
    mapping.ToTable("Orders")
           .HasId(o => o.Id)
           .HasTenantId(o => o.TenantId)
           .MapProperty(o => o.CustomerId)
           .MapProperty(o => o.Total));
```

**Test Coverage**: 485 unit tests for Multi-Tenancy across all providers

**Related Issue**: [#282 - Multi-Tenancy Support](https://github.com/dlrivada/Encina/issues/282)

---

#### Generic Repository Pattern for Remaining Providers (#279)

Implemented the Generic Repository Pattern (`IFunctionalRepository<TEntity, TId>`) across all 8 remaining database providers, completing the repository infrastructure.

**Providers Implemented**:

| Provider | Package | Identifier Quoting | Pagination |
|----------|---------|-------------------|------------|
| ADO.SQLite | `Encina.ADO.Sqlite` | `"column"` | LIMIT/OFFSET |
| ADO.PostgreSQL | `Encina.ADO.PostgreSQL` | `"column"` | LIMIT/OFFSET |
| ADO.MySQL | `Encina.ADO.MySQL` | `` `column` `` | LIMIT/OFFSET |
| ADO.Oracle | `Encina.ADO.Oracle` | `"column"` | OFFSET/FETCH |
| Dapper.SQLite | `Encina.Dapper.Sqlite` | `"column"` | LIMIT/OFFSET |
| Dapper.PostgreSQL | `Encina.Dapper.PostgreSQL` | `"column"` | LIMIT/OFFSET |
| Dapper.MySQL | `Encina.Dapper.MySQL` | `` `column` `` | LIMIT/OFFSET |
| Dapper.Oracle | `Encina.Dapper.Oracle` | `"column"` | OFFSET/FETCH |

**Key Components per Provider**:

- `FunctionalRepositoryADO<TEntity, TId>` / `FunctionalRepositoryDapper<TEntity, TId>` - Repository implementation
- `EntityMappingBuilder<TEntity, TId>` - Fluent API for entity-to-table mapping
- `IEntityMapping<TEntity, TId>` - Mapping configuration interface
- `SpecificationSqlBuilder<TEntity>` - Expression-to-SQL translation with database-specific syntax

**Database-Specific Features**:

- **SQLite**: GUID as TEXT, boolean as INTEGER (0/1)
- **PostgreSQL**: Native UUID and boolean types
- **MySQL**: GUID as CHAR(36), boolean as TINYINT(1), backtick quoting
- **Oracle**: Colon parameters (`:p0`), OFFSET/FETCH pagination

**Usage Example**:

```csharp
// Configuration (startup) — Build() returns Either<EncinaError, IEntityMapping>.
// Invalid configuration is a programmer error, so fail fast at startup.
var mapping = new EntityMappingBuilder<Order, Guid>()
    .ToTable("Orders")
    .HasId(o => o.Id)
    .MapProperty(o => o.CustomerId, "CustomerId")
    .MapProperty(o => o.Total, "Total")
    .ExcludeFromInsert(o => o.Id)
    .Build()
    .Match(
        Right: m => m,
        Left: error => throw new InvalidOperationException(error.Message));

var repository = new FunctionalRepositoryADO<Order, Guid>(connection, mapping);

// Domain operations — Railway Oriented Programming, no exceptions
var result = await repository.GetByIdAsync(orderId);
result.Match(
    order => Console.WriteLine($"Found: {order.Total}"),
    error => Console.WriteLine($"Error: {error.Message}")
);
```

**Test Coverage**: 1,153 unit tests for Repository pattern across all providers

**Related Issue**: [#279 - Generic Repository Pattern](https://github.com/dlrivada/Encina/issues/279)

---

#### Unit of Work Pattern - Remaining Providers (#281)

Extended the Unit of Work pattern (`IUnitOfWork`) to all 8 remaining database providers, completing the transactional infrastructure.

**Providers Implemented**:

| Provider | Package | Implementation |
|----------|---------|----------------|
| ADO.SQLite | `Encina.ADO.Sqlite` | `UnitOfWorkADO` |
| ADO.PostgreSQL | `Encina.ADO.PostgreSQL` | `UnitOfWorkADO` |
| ADO.MySQL | `Encina.ADO.MySQL` | `UnitOfWorkADO` |
| ADO.Oracle | `Encina.ADO.Oracle` | `UnitOfWorkADO` |
| Dapper.SQLite | `Encina.Dapper.Sqlite` | `UnitOfWorkDapper` |
| Dapper.PostgreSQL | `Encina.Dapper.PostgreSQL` | `UnitOfWorkDapper` |
| Dapper.MySQL | `Encina.Dapper.MySQL` | `UnitOfWorkDapper` |
| Dapper.Oracle | `Encina.Dapper.Oracle` | `UnitOfWorkDapper` |

**Key Components per Provider**:

- `UnitOfWorkADO` / `UnitOfWorkDapper` - Unit of Work implementation
- `UnitOfWorkRepositoryADO` / `UnitOfWorkRepositoryDapper` - Transaction-aware repository
- `AddEncinaUnitOfWork()` - DI registration extension method

---

#### Unit of Work Pattern (#281)

Implemented the Unit of Work pattern (`IUnitOfWork`) across all data access providers for coordinating transactional operations across multiple repositories.

**Core Interface** (`Encina.DomainModeling`):

```csharp
public interface IUnitOfWork : IAsyncDisposable
{
    bool HasActiveTransaction { get; }
    IFunctionalRepository<TEntity, TId> Repository<TEntity, TId>() where TEntity : class where TId : notnull;
    Task<Either<EncinaError, int>> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<Either<EncinaError, Unit>> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task<Either<EncinaError, Unit>> CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
```

**Provider Implementations**:

| Provider | Implementation | Transaction Support |
|----------|----------------|---------------------|
| `Encina.EntityFrameworkCore` | `UnitOfWorkEF` | EF Core `IDbContextTransaction` |
| `Encina.Dapper.SqlServer` | `UnitOfWorkDapper` | ADO.NET `IDbTransaction` |
| `Encina.ADO.SqlServer` | `UnitOfWorkADO` | ADO.NET `IDbTransaction` |
| `Encina.MongoDB` | `UnitOfWorkMongoDB` | MongoDB `IClientSessionHandle` |

**Key Features**:

- Railway Oriented Programming with `Either<EncinaError, T>` return types
- Repository caching (same entity type returns same instance)
- Auto-rollback on dispose for uncommitted transactions
- Error codes: `TransactionAlreadyActive`, `NoActiveTransaction`, `TransactionStartFailed`, `CommitFailed`, `SaveChangesFailed`

**Service Registration**:

```csharp
// EF Core
services.AddEncinaUnitOfWork<MyDbContext>();

// Dapper/ADO.NET
services.AddEncinaUnitOfWork();

// MongoDB (requires replica set for transactions)
services.AddEncinaUnitOfWork();
```

**Usage Example**:

```csharp
public async Task<Either<EncinaError, Unit>> TransferFunds(IUnitOfWork uow, TransferCommand cmd)
{
    var accounts = uow.Repository<Account, Guid>();

    var begin = await uow.BeginTransactionAsync();
    if (begin.IsLeft) return begin;

    var source = await accounts.GetByIdAsync(cmd.SourceId);
    var target = await accounts.GetByIdAsync(cmd.TargetId);

    // Modify accounts...

    var save = await uow.SaveChangesAsync();
    if (save.IsLeft) { await uow.RollbackAsync(); return save.Map(_ => Unit.Default); }

    return await uow.CommitAsync();
}
```

**Test Coverage**: 128 unit tests + 60 integration tests across all providers

**Related Issue**: [#281 - Unit of Work Pattern](https://github.com/dlrivada/Encina/issues/281)

---

#### Bulk Operations (#284)

Implemented high-performance bulk database operations across all data access providers (EF Core, Dapper, ADO.NET, MongoDB), achieving **up to 459x faster** performance compared to standard row-by-row operations.

**Performance Comparison (measured with Testcontainers, 1,000 entities)**:

| Provider | Database | Insert | Update | Delete |
|----------|----------|--------|--------|--------|
| **Dapper** | SQL Server 2022 | **30x** faster | **125x** faster | **370x** faster |
| **EF Core** | SQL Server 2022 | **112x** faster | **178x** faster | **200x** faster |
| **ADO.NET** | SQL Server 2022 | **104x** faster | **187x** faster | **459x** faster |
| **MongoDB** | MongoDB 7 | **130x** faster | **16x** faster | **21x** faster |

**Core Interface** (`Encina.DomainModeling`):

```csharp
public interface IBulkOperations<TEntity> where TEntity : class
{
    Task<Either<EncinaError, int>> BulkInsertAsync(IEnumerable<TEntity> entities, BulkConfig? config = null, CancellationToken ct = default);
    Task<Either<EncinaError, int>> BulkUpdateAsync(IEnumerable<TEntity> entities, BulkConfig? config = null, CancellationToken ct = default);
    Task<Either<EncinaError, int>> BulkDeleteAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);
    Task<Either<EncinaError, int>> BulkMergeAsync(IEnumerable<TEntity> entities, BulkConfig? config = null, CancellationToken ct = default);
    Task<Either<EncinaError, IReadOnlyList<TEntity>>> BulkReadAsync(IEnumerable<object> ids, CancellationToken ct = default);
}
```

**Provider Implementations**:

| Provider | BulkInsert | BulkUpdate | BulkDelete | BulkMerge |
|----------|:----------:|:----------:|:----------:|:---------:|
| `Encina.EntityFrameworkCore` | SqlBulkCopy | MERGE + TVP | DELETE + TVP | MERGE |
| `Encina.Dapper.SqlServer` | SqlBulkCopy | MERGE + TVP | DELETE + TVP | MERGE |
| `Encina.ADO.SqlServer` | SqlBulkCopy | MERGE + TVP | DELETE + TVP | MERGE |
| `Encina.MongoDB` | InsertMany | BulkWrite | BulkWrite | BulkWrite (upsert) |

**Configuration** (`BulkConfig` immutable record):

```csharp
var config = BulkConfig.Default with
{
    BatchSize = 5000,              // Entities per batch (default: 2000)
    BulkCopyTimeout = 300,         // Timeout in seconds
    SetOutputIdentity = true,      // Get generated IDs back
    PreserveInsertOrder = true,    // Maintain entity order
    UseTempDB = true,              // Use tempdb for staging (SQL Server)
    PropertiesToInclude = ["Status", "UpdatedAt"],  // Partial update
    PropertiesToExclude = ["CreatedAt"]             // Exclude columns
};
```

**Usage Example**:

```csharp
var bulkOps = unitOfWork.BulkOperations<Order>();
var orders = GenerateOrders(10_000);

var result = await bulkOps.BulkInsertAsync(orders, BulkConfig.Default with { BatchSize = 5000 });

result.Match(
    Right: count => _logger.LogInformation("Inserted {Count} orders", count),
    Left: error => _logger.LogError("Bulk insert failed: {Error}", error.Message)
);
```

**Error Codes**:

| Error Code | Description |
|------------|-------------|
| `Repository.BulkInsertFailed` | Bulk insert operation failed |
| `Repository.BulkUpdateFailed` | Bulk update operation failed |
| `Repository.BulkDeleteFailed` | Bulk delete operation failed |
| `Repository.BulkMergeFailed` | Bulk merge/upsert operation failed |
| `Repository.BulkReadFailed` | Bulk read operation failed |

**Test Coverage**: 65 unit tests + 11 integration tests + benchmarks

**Related Issue**: [#284 - Bulk Operations](https://github.com/dlrivada/Encina/issues/284)

---

#### Multi-Tenancy Database Support (#282)

Implemented comprehensive multi-tenant database support across all data access providers (EF Core, Dapper, ADO.NET, MongoDB) with three isolation strategies.

**Isolation Strategies**:

| Strategy | Isolation Level | Use Case |
|----------|-----------------|----------|
| `SharedSchema` | Row-level (TenantId column) | Cost-effective, many small tenants |
| `SchemaPerTenant` | Schema-level | Balance of isolation and cost |
| `DatabasePerTenant` | Database-level | Maximum isolation, compliance |

**Core Abstractions** (`Encina.Tenancy`):

```csharp
// Tenant metadata
public record TenantInfo(
    string TenantId,
    string Name,
    TenantIsolationStrategy Strategy,
    string? ConnectionString = null,
    string? SchemaName = null);

// Core interfaces
public interface ITenantProvider {
    string? GetCurrentTenantId();
    ValueTask<TenantInfo?> GetCurrentTenantAsync(CancellationToken ct);
}

public interface ITenantStore {
    ValueTask<TenantInfo?> GetTenantAsync(string tenantId, CancellationToken ct);
    ValueTask<IReadOnlyList<TenantInfo>> GetAllTenantsAsync(CancellationToken ct);
    ValueTask<bool> ExistsAsync(string tenantId, CancellationToken ct);
}
```

**ASP.NET Core Integration** (`Encina.Tenancy.AspNetCore`):

Four built-in tenant resolvers with configurable priority:

| Resolver | Priority | Source |
|----------|----------|--------|
| `HeaderTenantResolver` | 100 | HTTP Header (`X-Tenant-Id`) |
| `ClaimTenantResolver` | 110 | JWT Claim (`tenant_id`) |
| `RouteTenantResolver` | 120 | Route Parameter (`{tenant}`) |
| `SubdomainTenantResolver` | 130 | Subdomain (`acme.example.com`) |

**Provider Implementations**:

| Provider | Key Features |
|----------|--------------|
| `Encina.Dapper.SqlServer` | `TenantAwareSpecificationSqlBuilder`, auto tenant filtering |
| `Encina.ADO.SqlServer` | `TenantAwareSpecificationSqlBuilder`, auto tenant filtering |
| `Encina.MongoDB` | `TenantAwareSpecificationFilterBuilder`, database-per-tenant routing |

**Service Registration**:

```csharp
// Dapper with tenancy
services.AddEncinaDapperSqlServerWithTenancy(connectionString, tenancy => {
    tenancy.AutoFilterTenantQueries = true;
    tenancy.AutoAssignTenantId = true;
    tenancy.ValidateTenantOnModify = true;
});

// MongoDB with tenancy
services.AddEncinaMongoDBWithTenancy(config => { ... }, tenancy => {
    tenancy.EnableDatabasePerTenant = true;
    tenancy.DatabaseNamePattern = "{baseName}_{tenantId}";
});

// ASP.NET Core tenant resolution
services.AddEncinaTenancyAspNetCore(options => {
    options.HeaderResolver.Enabled = true;
    options.SubdomainResolver.BaseDomain = "example.com";
});
app.UseEncinaTenantResolution();
```

**Test Coverage**: 376 unit tests across all tenancy components

| Component | Tests |
|-----------|-------|
| Core Tenancy (`TenantInfo`, `InMemoryTenantStore`, etc.) | ~100 tests |
| ASP.NET Core (resolvers, middleware, chain) | ~80 tests |
| Dapper/ADO.NET Tenancy | ~80 tests |
| MongoDB Tenancy | 72 tests |

**Documentation**: Comprehensive guide at `docs/features/multi-tenancy.md` (849 lines)

**Related Issue**: [#282 - Multi-Tenancy Database Support](https://github.com/dlrivada/Encina/issues/282)

---

#### Module Isolation by Database Permissions (#534)

Implemented database-level module isolation for modular monolith architectures, ensuring bounded contexts cannot directly access each other's data.

**Isolation Strategies**:

| Strategy | Enforcement Level | Use Case |
|----------|-------------------|----------|
| `DevelopmentValidationOnly` | Runtime SQL validation | Development/testing |
| `SchemaWithPermissions` | Database users/permissions | Production |
| `ConnectionPerModule` | Separate connection strings | Microservice preparation |

**Core Abstractions** (`Encina`):

```csharp
// Configuration options
public class ModuleIsolationOptions {
    ModuleIsolationStrategy Strategy { get; set; }
    IReadOnlySet<string> SharedSchemas { get; }
    IReadOnlyList<ModuleSchemaOptions> ModuleSchemas { get; }
    bool GeneratePermissionScripts { get; set; }
    string? PermissionScriptsOutputPath { get; set; }
}

// Module schema configuration
public class ModuleSchemaOptions {
    string ModuleName { get; init; }
    string SchemaName { get; init; }
    string? DatabaseUser { get; init; }
    IReadOnlyList<string> AdditionalAllowedSchemas { get; init; }
}

// Schema registry for validation
public interface IModuleSchemaRegistry {
    IReadOnlySet<string> GetAllowedSchemas(string moduleName);
    bool CanAccessSchema(string moduleName, string schemaName);
    SchemaAccessValidationResult ValidateSqlAccess(string moduleName, string sql);
}
```

**Provider Implementations**:

| Provider | Key Features |
|----------|--------------|
| `Encina.EntityFrameworkCore` | EF Core interceptor, schema validation |
| `Encina.Dapper.SqlServer` | SQL interceptor, permission script generation |
| `Encina.ADO.SqlServer` | ADO.NET interceptor, SQL Server scripts |
| `Encina.MongoDB` | Collection prefix isolation, module-aware factory |

**Permission Script Generation**:

- `SqlServerPermissionScriptGenerator` - SQL Server GRANT/REVOKE scripts
- `PostgreSqlPermissionScriptGenerator` - PostgreSQL roles and ALTER DEFAULT PRIVILEGES

**Service Registration**:

```csharp
services.AddEncinaEntityFrameworkCore<AppDbContext>(config =>
{
    config.UseModuleIsolation = true;
    config.ModuleIsolation.Strategy = ModuleIsolationStrategy.SchemaWithPermissions;
    config.ModuleIsolation.AddSharedSchemas("shared", "lookup");
    config.ModuleIsolation.AddModuleSchema("Orders", "orders", b =>
        b.WithDatabaseUser("orders_user")
         .WithAdditionalAllowedSchemas("audit"));
});
```

**Test Coverage**: 172 unit tests covering all module isolation components

| Component | Tests |
|-----------|-------|
| Core (ModuleSchemaRegistry, SqlSchemaExtractor) | ~50 tests |
| Permission Script Generators (SQL Server, PostgreSQL) | ~80 tests |
| ModuleExecutionContext (AsyncLocal) | ~20 tests |
| PermissionScript record | ~22 tests |

**Documentation**: Comprehensive guide at `docs/features/module-isolation.md` (560+ lines)

**Related Issue**: [#534 - Module Isolation by Database Permissions](https://github.com/dlrivada/Encina/issues/534)

---

#### Read/Write Database Separation (CQRS Physical Split) (#283)

Implemented read/write database separation for CQRS physical split architectures across all data access providers (EF Core, Dapper, ADO.NET, MongoDB).

**Core Abstractions** (`Encina.Messaging`):

```csharp
// Database intent markers
public enum DatabaseIntent { Read, Write, ForceWrite }

// Context for routing decisions
public static class DatabaseRoutingContext
{
    public static DatabaseIntent CurrentIntent { get; }
    public static void SetIntent(DatabaseIntent intent);
    public static IDisposable BeginReadContext();
    public static IDisposable BeginWriteContext();
    public static IDisposable BeginForceWriteContext();
}

// Attribute for read-after-write consistency
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ForceWriteDatabaseAttribute : Attribute;
```

**Provider Implementations**:

| Provider | Read Routing | Configuration |
|----------|--------------|---------------|
| `Encina.EntityFrameworkCore` | `IDbContextFactory` + interceptors | `UseReadWriteSeparation = true` |
| `Encina.Dapper.SqlServer` | Connection factory with read preference | `ReadConnectionString` |
| `Encina.ADO.SqlServer` | Connection factory with read preference | `ReadConnectionString` |
| `Encina.MongoDB` | `IReadWriteMongoCollectionFactory` with read preferences | `UseReadWriteSeparation = true` |

**MongoDB-Specific Features**:

```csharp
// Read preferences for replica routing
public enum MongoReadPreference
{
    Primary,           // Always read from primary (strong consistency)
    PrimaryPreferred,  // Prefer primary, fallback to secondary
    Secondary,         // Only read from secondaries
    SecondaryPreferred,// Prefer secondary, fallback to primary
    Nearest            // Lowest latency node
}

// Read concerns for consistency levels
public enum MongoReadConcern
{
    Default,      // Driver default
    Local,        // Node's local data
    Majority,     // Majority-committed data
    Linearizable, // Linearizable reads (strongest)
    Available,    // Available data (sharded)
    Snapshot      // Snapshot isolation
}
```

**Service Registration**:

```csharp
// EF Core
services.AddEncinaEntityFrameworkCore<ReadDbContext, WriteDbContext>(config =>
{
    config.UseReadWriteSeparation = true;
});

// Dapper
services.AddEncinaDapperSqlServer(config =>
{
    config.WriteConnectionString = "Server=primary;...";
    config.ReadConnectionString = "Server=secondary;...";
});

// MongoDB
services.AddEncinaMongoDB(mongoClient, config =>
{
    config.DatabaseName = "mydb";
    config.UseReadWriteSeparation = true;
    config.ReadWriteSeparationOptions.ReadPreference = MongoReadPreference.SecondaryPreferred;
    config.ReadWriteSeparationOptions.ReadConcern = MongoReadConcern.Local;
    config.ReadWriteSeparationOptions.FallbackToPrimaryOnNoSecondaries = true;
    config.ReadWriteSeparationOptions.MaxStaleness = TimeSpan.FromMinutes(2);
});
```

**Pipeline Behavior (Automatic Routing)**:

The `ReadWriteRoutingPipelineBehavior<TRequest, TResponse>` automatically routes:

- `IQuery<T>` → Read database (unless marked with `[ForceWriteDatabase]`)
- `ICommand<T>` / `INotification` → Write database

```csharp
// Automatically routed to read database
public sealed record GetOrderByIdQuery(Guid Id) : IQuery<Order>;

// Force read-after-write consistency
[ForceWriteDatabase]
public sealed record GetOrderAfterCreationQuery(Guid Id) : IQuery<Order>;
```

**Health Checks**:

| Provider | Health Check | Default Name |
|----------|--------------|--------------|
| EF Core | `ReadWriteEfCoreHealthCheck` | `encina-read-write-separation-efcore` |
| MongoDB | `ReadWriteMongoHealthCheck` | `encina-read-write-separation-mongodb` |
| Dapper | Connection validation | `encina-read-write-separation-dapper` |
| ADO.NET | Connection validation | `encina-read-write-separation-ado` |

**Test Coverage**: 69 unit tests for MongoDB read/write separation

| Component | Tests |
|-----------|-------|
| `MongoReadWriteSeparationOptions` | 11 tests |
| `MongoReadPreference` enum | 8 tests |
| `MongoReadConcern` enum | 7 tests |
| `ReadWriteMongoCollectionFactory` | 14 tests |
| `ReadWriteRoutingPipelineBehavior` | 15 tests |
| `ReadWriteMongoHealthCheck` | 6 tests |
| `ServiceCollectionExtensions` | 8 tests |

**Documentation**: Comprehensive guide at `docs/features/read-write-separation.md` (500+ lines)

**Related Issue**: [#283 - Read/Write Database Separation (CQRS Physical Split)](https://github.com/dlrivada/Encina/issues/283)

---

#### Specification Pattern Enhancement (#280)

Enhanced the Specification Pattern implementation across all data access providers with comprehensive `QuerySpecification<T>` support for multi-column ordering, offset-based pagination, and keyset (cursor-based) pagination.

**QuerySpecification API**:

```csharp
public class ActiveOrdersQuerySpec : QuerySpecification<Order>
{
    public ActiveOrdersQuerySpec(Guid lastId, int pageSize)
    {
        AddCriteria(o => o.IsActive);
        ApplyOrderBy(o => o.Name);
        ApplyThenByDescending(o => o.CreatedAtUtc);
        ApplyKeysetPagination(o => o.Id, lastId, pageSize);
    }
}
```

**Provider Implementations**:

| Provider | Features Implemented |
|----------|---------------------|
| `Encina.EntityFrameworkCore` | `SpecificationEvaluator` with ThenBy/ThenByDescending, keyset pagination |
| `Encina.Dapper.SqlServer` | `SpecificationSqlBuilder` with ORDER BY, OFFSET/FETCH, keyset filter |
| `Encina.ADO.SqlServer` | `SpecificationSqlBuilder` with ORDER BY, OFFSET/FETCH, keyset filter |
| `Encina.MongoDB` | `SpecificationFilterBuilder` with SortDefinition, keyset pagination |

**New Methods Added**:

- `QuerySpecification<T>.ApplyOrderBy()` / `ApplyOrderByDescending()` - Primary ordering
- `QuerySpecification<T>.ApplyThenBy()` / `ApplyThenByDescending()` - Secondary ordering
- `QuerySpecification<T>.ApplyPaging(skip, take)` - Offset-based pagination
- `QuerySpecification<T>.ApplyKeysetPagination(keySelector, lastKey, pageSize)` - Cursor-based O(1) pagination

**SQL Generation Examples**:

```sql
-- Offset pagination (Dapper/ADO)
SELECT * FROM Orders WHERE [IsActive] = 1
ORDER BY [Name] ASC, [CreatedAtUtc] DESC
OFFSET 10 ROWS FETCH NEXT 20 ROWS ONLY

-- Keyset pagination (Dapper/ADO)
SELECT * FROM Orders WHERE [IsActive] = 1 AND [Id] > @p0
ORDER BY [Id] ASC
FETCH NEXT 10 ROWS ONLY
```

**Test Coverage**: 88 new unit tests across all providers

| Provider | New Tests |
|----------|-----------|
| EF Core `SpecificationEvaluatorTests` | 22 tests |
| Dapper `QuerySpecificationSqlBuilderTests` | 22 tests |
| ADO.SqlServer `QuerySpecificationSqlBuilderTests` | 22 tests |
| MongoDB `SpecificationFilterBuilderTests` | 22 tests |

**Related Issue**: [#280 - Specification Pattern](https://github.com/dlrivada/Encina/issues/280)

---

- `Encina.MongoDB`: Added `InternalsVisibleTo` for `Encina.UnitTests` and `Encina.IntegrationTests` to enable testing of internal methods
- Unit tests for `MongoDbRepositoryOptions.GetEffectiveCollectionName()` and `Validate()` internal methods (8 new tests)

---

## [0.11.0] - 2026-01-19 - Testing Infrastructure

> **Milestone**: [v0.11.0 - Testing Infrastructure](https://github.com/dlrivada/Encina/milestone/8) - **COMPLETED**
>
> This release delivers a comprehensive testing toolkit for Encina applications, including 10 new testing packages, property-based testing support, mutation testing helpers, and CI/CD workflow templates.

### Highlights

- **34 issues closed** in this milestone
- **92.3% code coverage** achieved (target: ≥85%)
- **0 SonarCloud issues** (all addressed or excluded with justification)
- **6,500+ tests** across all packages
- **10 new testing packages** delivered

### Table of Contents

- [Dogfooding Initiative](#dogfooding-initiative-issues-498-502)
- [Solution Filters Reorganization](#solution-filters-reorganization)
- [Encina.Testing.Pact](#encinatestingpact-new-package-issue-436)
- [Encina.Testing.FsCheck](#encinatestingfscheck-new-package-issue-435)
- [Encina.Testing.TUnit](#encinatestingtunit-new-package-issue-171)

- [Language Requirements](#language-requirements)
- [Added](#added)
  - [AI/LLM Patterns Issues](#aillm-patterns-issues-12-new-features-planned-based-on-december-29-2025-research)
  - [Hexagonal Architecture Patterns Issues](#hexagonal-architecture-patterns-issues-10-new-features-planned-based-on-december-29-2025-research)
  - [TDD Patterns Issues](#tdd-patterns-issues-12-new-features-planned-based-on-december-29-2025-research)
  - [Developer Tooling & DX Issues](#developer-tooling--dx-issues-11-new-features-planned-based-on-december-29-2025-research)
  - [.NET Aspire Integration Patterns Issues](#net-aspire-integration-patterns-issues-10-new-features-planned-based-on-december-29-2025-research)
  - [Cloud-Native Patterns Issues](#cloud-native-patterns-issues-11-new-features-planned-based-on-december-29-2025-research)
  - [Microservices Architecture Patterns Issues](#microservices-architecture-patterns-issues-12-new-features-planned-based-on-december-29-2025-research)
  - [Security Patterns Issues](#security-patterns-issues-8-new-features-planned-based-on-december-29-2025-research)
  - [Advanced Validation Patterns Issues](#advanced-validation-patterns-issues-10-new-features-planned-based-on-december-2025-research)
  - [Advanced Event Sourcing Patterns Issues](#advanced-event-sourcing-patterns-issues-13-new-features-planned-based-on-december-2025-research)
  - [Advanced CQRS Patterns Issues](#advanced-cqrs-patterns-issues-12-new-features-planned-based-on-december-2025-market-research)
  - [Domain Modeling Building Blocks Issues](#domain-modeling-building-blocks-issues-15-new-features-planned-based-on-december-29-2025-ddd-research)
  - [Vertical Slice Architecture Patterns Issues](#vertical-slice-architecture-patterns-issues-12-new-features-planned-based-on-december-29-2025-research)
  - [Modular Monolith Architecture Patterns Issues](#modular-monolith-architecture-patterns-issues-10-new-features-planned-based-on-december-29-2025-research)
  - [Advanced Messaging Patterns Issues](#advanced-messaging-patterns-issues-15-new-features-planned-based-on-market-research)
  - [Database Providers Patterns Issues](#database-providers-patterns-issues-16-new-features-planned-based-on-december-2025-research)
  - [Advanced DDD & Workflow Patterns Issues](#advanced-ddd--workflow-patterns-issues-13-new-features-planned-based-on-december-29-2025-research)
  - [Advanced EDA Patterns Issues](#advanced-eda-patterns-issues-12-new-features-planned-based-on-december-29-2025-research)
  - [Advanced Caching Patterns Issues](#advanced-caching-patterns-issues-13-new-features-planned-based-on-december-2025-research)
  - [Advanced Resilience Patterns Issues](#advanced-resilience-patterns-issues-9-new-features-planned-based-on-2025-research)
  - [Advanced Scheduling Patterns Issues](#advanced-scheduling-patterns-issues-15-new-features-planned-based-on-2025-research)
  - [Advanced Observability Patterns Issues](#advanced-observability-patterns-issues-15-new-features-planned-based-on-2025-research)
  - [Web/API Integration Patterns Issues](#webapi-integration-patterns-issues-18-new-features-planned-based-on-december-2025-research)
  - [Advanced Testing Patterns Issues](#advanced-testing-patterns-issues-13-new-features-planned-based-on-2025-research)
  - [Advanced Distributed Lock Patterns Issues](#advanced-distributed-lock-patterns-issues-20-new-features-planned-based-on-december-2025-research)
  - [Message Transport Patterns Issues](#message-transport-patterns-issues-29-new-features-planned-based-on-december-2025-research)
  - [Clean Architecture Patterns Issues](#clean-architecture-patterns-issues-2-new-features-planned-based-on-december-29-2025-research)
- [Changed](#changed)

---

### Language Requirements

> **Encina requires C# 14 / .NET 10 or later.** All packages in this framework use modern C# features including target-typed `new()`, `with` expressions (requires `record` types), and other .NET 10 enhancements.

---

### Added

#### Dogfooding Initiative (Issues #498-508) - COMPLETED

Epic initiative to refactor all Encina tests to use `Encina.Testing.*` infrastructure (dogfooding).

> **EPIC #498**: [Dogfooding Testing Infrastructure](https://github.com/dlrivada/Encina/issues/498) - **CLOSED** (2026-01-19)

**All Phases Completed**:

- **Phase 1** (#499): Core package tests - **CLOSED**
- **Phase 2** (#500): DomainModeling package tests - **CLOSED**
- **Phase 3** (#501): Messaging package tests - **CLOSED**
- **Phase 4** (#502): Database provider tests (ADO, Dapper, EF Core) - **CLOSED**
- **Phase 5** (#503): Caching provider tests - **CLOSED**
- **Phase 6** (#504): Transport provider tests - **CLOSED**
- **Phase 7** (#505): Web Integration tests (AspNetCore, Refit, gRPC, SignalR) - **CLOSED**
- **Phase 8** (#506): Resilience & Observability tests (Polly, OpenTelemetry) - **CLOSED**
- **Phase 9** (#507): Validation Provider tests (FluentValidation, DataAnnotations, MiniValidator) - **CLOSED**
- **Phase 10** (#508): Serverless & Scheduling tests (AwsLambda, AzureFunctions, Hangfire, Quartz) - **CLOSED**

**Phase 10 Results** (Serverless & Scheduling):

| Package | Tests | Improvements |
|---------|-------|--------------|
| Encina.AwsLambda.Tests | 97 | EitherAssertions, Bogus fakers |
| Encina.AzureFunctions.Tests | 124 | EitherAssertions, Bogus fakers |
| Encina.Hangfire.Tests | 29 | FakeLogger (7 skipped tests enabled), EitherAssertions |
| Encina.Hangfire.IntegrationTests | 10 | Bogus fakers |
| Encina.Quartz.Tests | 36 | Bogus fakers, EitherAssertions |
| Encina.Quartz.IntegrationTests | 9 | Bogus fakers |
| **Total** | **305** | Zero skipped, zero warnings |

**Key Improvements (Phase 10)**:

- Replaced `Substitute.For<ILogger<T>>()` with `FakeLogger<T>` for Hangfire tests (resolves Issue #6)
- Enabled 7 previously skipped Hangfire logging tests using Microsoft.Extensions.Diagnostics.Testing
- Applied `ShouldBeSuccess()` / `ShouldBeError()` EitherAssertions across all packages
- Created Bogus fakers for test data generation in all 4 serverless/scheduling packages
- Added global using for `Encina.TestInfrastructure.Extensions` in all test projects

**Phase 9 Coverage Results** (all ≥85% target met):

| Package | Line Coverage | Branch Coverage | Tests |
|---------|---------------|-----------------|-------|
| Encina.FluentValidation | 96.7% | 90.5% | 20 |
| Encina.DataAnnotations | 100% | 90% | 20 |
| Encina.MiniValidator | 100% | 100% | 17 |

**Test Categories Implemented (Phase 9)**:

- Null input invariants (ArgumentNullException handling)
- Validation idempotency (same request → same result)
- Error aggregation (multiple failures captured)
- PropertyName inclusion for field-level errors
- ValidationResult invariants (IsValid/IsInvalid mutually exclusive)
- ServiceCollection extension tests (DI registration)
- Context metadata propagation (UserId, TenantId)
- Cross-provider consistency tests (13 tests verifying all providers behave identically)

**Phase 8 Coverage Results** (all ≥85% target met):

| Package | Coverage | Tests |
|---------|----------|-------|
| Encina.Polly | 89.3% | 214 |
| Encina.OpenTelemetry | 92.0% | 71 |

**Test Fixes Applied (Phase 8)**:

- Fixed `BackoffTypeTests.cs`: Changed `values.Count` to `values.Length` (.NET 10 breaking change)
- Fixed `BulkheadManagerTests.cs`: Changed `Func<Task>` to async lambda with await for ValueTask
- Fixed `AdaptiveRateLimiterGuardsTests.cs`: Added async/await and fixed lambda types
- Fixed `BulkheadManagerGuardsTests.cs`: Fixed ValueTask handling in tests
- Fixed `RateLimitingPipelineBehaviorGuardsTests.cs`: Changed `System.Threading.RateLimiting` to `Encina.Polly` namespace
- Fixed `ServiceCollectionExtensionsTests.cs` (OpenTelemetry): Removed incorrect assertion about options registration

**Phase 7 Coverage Results** (all ≥85% target met):

| Package | Coverage | Tests |
|---------|----------|-------|
| Encina.AspNetCore | 89.3% | 101 |
| Encina.Refit | 92.1% | 46 |
| Encina.SignalR | 94.0% | 111 |
| Encina.gRPC | 97.7% | 78 |

**Bug Fix**: #520 - Fixed reflection bug in `GrpcEncinaService.SendAsync` that was searching for methods with wrong generic argument count.

**Test Infrastructure Improvements**:

- `BogusArbitrary<T>` class bridging Bogus Faker with FsCheck generators
- `MessageDataGenerators` with pre-built generators for messaging entities
- `TimeProvider` injection to Dapper stores for deterministic time control
- Fixed SQLite datetime format incompatibility (ISO 8601 vs `datetime('now')`)

**Test Fixes Applied**:

- ADO/Dapper GuardTests: Fixed parameter name "next" → "nextStep"
- EF Core ContractTests: Fixed invalid regex `ShouldMatch("*Type*")` → `ShouldContain("Type")`
- EF Core IntegrationTests: Added `HasConversion<string>()` for SagaStatus enum
- EF Core HealthCheck: Added `DefaultTags` with ["encina", "database", "efcore", "ready"]

**Test Results**:

- ADO.Sqlite: 209 tests ✅
- EF Core: 219 tests ✅

#### Solution Filters Reorganization

> **Note**: Solution filters were subsequently deprecated after the January 2026 test consolidation. Tests are now consolidated into 7 projects under `tests/`, and the full solution `Encina.slnx` builds without issues. The `.slnf` files were moved to `.backup/slnf-old/`.

Updated all solution filters (`.slnf` files) to include complete test project coverage.

**Updated Filters** (11 files):

- `Encina.Core.slnf` - Added DomainModeling and all its tests
- `Encina.Messaging.slnf` - Added TestInfrastructure
- `Encina.EventSourcing.slnf` - Added TestInfrastructure, Marten.IntegrationTests
- `Encina.Observability.slnf` - Added Messaging, TestInfrastructure
- `Encina.Validation.slnf` - Added GuardClauses.Tests
- `Encina.Web.slnf` - Added SignalR, gRPC and tests
- `Encina.Scheduling.slnf` - Reorganized TestInfrastructure
- `Encina.Testing.slnf` - Added FsCheck, Verify, Testcontainers, Architecture, Aspire (32 projects)
- `Encina.Database.slnf` - Full expansion with all 85 database provider test projects
- `Encina.Caching.slnf` - Added TestInfrastructure, Redis.Tests
- `Encina.Resilience.slnf` - Added TestInfrastructure

**New Filters Created** (5 files):

- `Encina.Transports.slnf` - RabbitMQ, Kafka, AzureServiceBus, AmazonSQS, NATS, MQTT
- `Encina.Serverless.slnf` - AwsLambda, AzureFunctions
- `Encina.DistributedLock.slnf` - DistributedLock, Redis, SqlServer
- `Encina.Cli.slnf` - CLI tool
- `Encina.Workflows.slnf` - Workflows

#### Encina.Testing.Pact (New Package, Issue #436)

PactNet integration for Consumer-Driven Contract Testing (CDC) with Encina framework.

- **`EncinaPactConsumerBuilder`** - Fluent builder for defining consumer-side Pact expectations:
  - `WithCommandExpectation<TCommand, TResponse>()` - Define command request/response contracts
  - `WithQueryExpectation<TQuery, TResponse>()` - Define query request/response contracts
  - `WithNotificationExpectation<TNotification>()` - Define notification contracts
  - `WithCommandFailureExpectation<TCommand, TResponse>()` - Define expected error responses for commands
  - `WithQueryFailureExpectation<TQuery, TResponse>()` - Define expected error responses for queries
  - `BuildAsync()` - Build the Pact and write to configured directory
  - `GetMockServerUri()` - Get mock server URI for testing

- **`EncinaPactProviderVerifier`** - Verifies Pact contracts against provider implementation:
  - `WithProviderName()` - Set the provider name for verification
  - `WithProviderState(stateName, Action)` - Register synchronous provider state setup
  - `WithProviderState(stateName, Func<Task>)` - Register async provider state setup
  - `WithProviderState(stateName, Func<IDictionary<string,object>, Task>)` - State setup with parameters
  - `VerifyAsync(pactFilePath)` - Verify a local Pact JSON file
  - `VerifyFromBrokerAsync(brokerUrl, providerName)` - Verify from Pact Broker

- **`EncinaPactFixture`** - xUnit test fixture for simplified test setup:
  - Implements `IAsyncLifetime` and `IDisposable` for lifecycle management
  - `CreateConsumer(consumerName, providerName)` - Create a consumer builder
  - `CreateVerifier(providerName)` - Create a provider verifier
  - `VerifyAsync(consumer, Action<Uri>)` - Verify with sync test action
  - `VerifyAsync(consumer, Func<Uri, Task>)` - Verify with async test action
  - `VerifyProviderAsync(providerName)` - Verify all Pact files for a provider
  - `WithEncina(encina, serviceProvider)` - Configure with Encina instance
  - `WithServices(configureServices)` - Configure with DI services

- **`PactExtensions`** - Extension methods for working with Pact:
  - `CreatePactHttpClient(Uri)` - Create HTTP client for mock server
  - `SendCommandAsync<TCommand, TResponse>()` - Send command to mock server
  - `SendQueryAsync<TQuery, TResponse>()` - Send query to mock server
  - `PublishNotificationAsync<TNotification>()` - Publish notification to mock server
  - `ReadAsEitherAsync<TResponse>()` - Deserialize response as Either result
  - `ToPactResponse<TResponse>()` - Convert Either to Pact-compatible response

- **Response Types**:
  - `PactSuccessResponse<T>` - Success response wrapper with `IsSuccess` and `Data`
  - `PactErrorResponse` - Error response with `IsSuccess`, `ErrorCode`, `ErrorMessage`
  - `PactVerificationResult` - Verification result with `Success`, `Errors`, `InteractionResults`
  - `InteractionVerificationResult` - Individual interaction result with `Description`, `Success`, `ErrorMessage`

- **Error Code Mapping** - Automatic HTTP status code mapping from Encina error codes:
  - `encina.validation.*` → 400 Bad Request
  - `encina.authorization.*` → 403 Forbidden
  - `encina.authentication.*` → 401 Unauthorized
  - `encina.notfound.*` → 404 Not Found
  - `encina.conflict.*` → 409 Conflict
  - `encina.timeout.*` → 408 Request Timeout
  - `encina.ratelimit.*` → 429 Too Many Requests
  - Other errors → 500 Internal Server Error

- **Tests**: 118 unit tests covering all public APIs
  - EncinaPactConsumerBuilderTests (17 tests)
  - EncinaPactProviderVerifierTests (24 tests)
  - EncinaPactFixtureTests (23 tests)
  - PactExtensionsTests (14 tests)
  - GuardClauseTests (40 tests)

#### Encina.Testing.FsCheck (New Package, Issue #435)

FsCheck property-based testing extensions for Encina framework, compatible with FsCheck 3.x.

- **`EncinaArbitraries`** - Pre-built arbitraries for generating Encina types:
  - Core types: `EncinaError()`, `EncinaErrorWithException()`, `RequestContext()`
  - Either types: `EitherOf<T>()`, `SuccessEither<T>()`, `FailureEither<T>()`
  - Messaging types: `OutboxMessage()`, `PendingOutboxMessage()`, `FailedOutboxMessage()`
  - `InboxMessage()`, `SagaState()`, `ScheduledMessage()`, `RecurringScheduledMessage()`

- **`EncinaProperties`** - Common property validators for invariants:
  - Either properties: `EitherIsExclusive()`, `MapPreservesRightState()`, `BindToFailureProducesLeft()`
  - Error properties: `ErrorHasNonEmptyMessage()`, `ErrorFromStringPreservesMessage()`
  - Context properties: `ContextHasCorrelationId()`, `WithMetadataIsImmutable()`, `WithUserIdCreatesNewContext()`
  - Outbox: `OutboxProcessedStateIsConsistent()`, `OutboxDeadLetterIsConsistent()`, `OutboxHasRequiredFields()`
  - Inbox: `InboxProcessedStateIsConsistent()`, `InboxHasRequiredFields()`
  - Saga: `SagaStatusIsValid()`, `SagaHasRequiredFields()`, `SagaCurrentStepIsNonNegative()`
  - Scheduled: `RecurringHasCronExpression()`, `ScheduledHasRequiredFields()`
  - Handler: `HandlerIsDeterministic()`, `AsyncHandlerIsDeterministic()`

- **`GenExtensions`** - Generator extension methods:
  - Either generators: `ToEither()`, `ToSuccess()`, `ToFailure<T>()`
  - Nullable generators: `OrNull()`, `OrNullValue()`
  - String generators: `NonEmptyString()`, `AlphaNumericString()`, `EmailAddress()`
  - Data generators: `JsonObject()`, `UtcDateTime()`, `PastUtcDateTime()`, `FutureUtcDateTime()`
  - Other: `CronExpression()`, `PositiveDecimal()`, `ListOf()`, `NonEmptyListOf()`

- **xUnit Integration**:
  - `PropertyTestBase` - Base class with auto-registered arbitraries
  - `EncinaArbitraryProvider` - Arbitrary provider for FsCheck type registration
  - Custom attributes: `[EncinaProperty]`, `[QuickProperty]`, `[ThoroughProperty]`
  - `PropertyTestConfig` - Configuration constants for test runs

- **Concrete Message Types** for testing:
  - `ArbitraryOutboxMessage` - Implements `IOutboxMessage`
  - `ArbitraryInboxMessage` - Implements `IInboxMessage`
  - `ArbitrarySagaState` - Implements `ISagaState`
  - `ArbitraryScheduledMessage` - Implements `IScheduledMessage`

- **Tests**: 75 unit tests covering all public APIs
  - EncinaArbitrariesTests (22 tests)
  - EncinaPropertiesTests (22 tests)
  - GenExtensionsTests (18 tests)
  - PropertyTestBaseTests (13 tests)

#### Encina.Testing.TUnit (New Package, Issue #171)

TUnit framework support for modern, source-generated testing with NativeAOT compatibility.

- **`EncinaTUnitFixture`** - TUnit-compatible test fixture with fluent builder pattern:
  - Implements TUnit's `IAsyncInitializer` and `IAsyncDisposable` for lifecycle management
  - `WithConfiguration(Action<EncinaConfiguration>)` - Custom Encina configuration
  - `WithServices(Action<IServiceCollection>)` - Register custom services for DI
  - `WithHandlersFromAssemblyContaining<T>()` - Scan assembly for handlers
  - `Encina` property - Get the configured Encina instance
  - `CreateScope()` - Create service scope for scoped services
  - `GetService<T>()`, `GetRequiredService<T>()` - Resolve services
  - Fluent builder pattern with chaining support
  - Proper `GC.SuppressFinalize` in `DisposeAsync()` per CA1816

- **`TUnitEitherAssertions`** - Async-first assertions for `Either<TLeft, TRight>`:
  - `ShouldBeSuccessAsync()` - Assert Right and return value
  - `ShouldBeSuccessAsync(expected)` - Assert Right with expected value
  - `ShouldBeSuccessAsync(Func<TRight, Task>)` - Assert with async validator
  - `ShouldBeErrorAsync()` - Assert Left and return error
  - `ShouldBeErrorAsync(Func<TLeft, Task>)` - Assert with async validator
  - `AndReturnAsync()` - Alias for `ShouldBeSuccessAsync()` for fluent chaining

- **EncinaError-Specific Assertions**:
  - `ShouldBeErrorWithCodeAsync(code)` - Assert error with specific code
  - `ShouldBeErrorContainingAsync(text)` - Assert error message contains text
  - `ShouldBeValidationErrorAsync()` - Assert code starts with "encina.validation"
  - `ShouldBeAuthorizationErrorAsync()` - Assert code starts with "encina.authorization"
  - `ShouldBeNotFoundErrorAsync()` - Assert code starts with "encina.notfound"
  - `ShouldBeConflictErrorAsync()` - Assert code starts with "encina.conflict"
  - `ShouldBeInternalErrorAsync()` - Assert code starts with "encina.internal"

- **Task Extension Methods** - All assertions work with `Task<Either<>>`:
  - `task.ShouldBeSuccessAsync()`, `task.ShouldBeErrorAsync()`
  - `task.ShouldBeValidationErrorAsync()`, `task.AndReturnAsync()`, etc.

- **`TUnitEitherCollectionAssertions`** - Collection assertions for `IEnumerable<Either<>>`:
  - `ShouldAllBeSuccessAsync()` - Assert all items are Right, return values
  - `ShouldAllBeErrorAsync()` - Assert all items are Left, return errors
  - `ShouldContainSuccessAsync()` - Assert at least one Right
  - `ShouldContainErrorAsync()` - Assert at least one Left
  - `ShouldHaveSuccessCountAsync(count)` - Assert exact success count
  - `ShouldHaveErrorCountAsync(count)` - Assert exact error count
  - `GetSuccesses()`, `GetErrors()` - Extract values/errors from collection

- **NativeAOT Compatibility**:
  - Package marked with `<IsAotCompatible>true</IsAotCompatible>`
  - No reflection-based patterns in package code
  - Compatible with TUnit's source-generated test discovery

- **Tests**: 56 unit tests covering all public APIs
  - EncinaTUnitFixtureTests (15 tests)
  - TUnitEitherAssertionsTests (21 tests)
  - TUnitEitherCollectionAssertionsTests (20 tests)

- **CI/CD Workflow Templates** (Issue #173) - Reusable GitHub Actions workflow templates for testing .NET 10 applications:
  - `encina-test.yml` - Basic test workflow with unit tests and coverage:
    - Cross-platform support (Windows, Linux, macOS)
    - Configurable coverage threshold enforcement
    - NuGet package caching for faster builds
    - Test filter expressions for selective testing
    - Integration test opt-in
  - `encina-matrix.yml` - Matrix testing across OS and database providers:
    - Multiple OS testing (Windows, Linux, macOS)
    - Database service containers (PostgreSQL, SQL Server, MySQL, Redis, MongoDB, SQLite)
    - Parallel execution with configurable max-parallel
    - Automatic connection string configuration
    - Summary report across all matrix combinations
  - `encina-full-ci.yml` - Complete CI pipeline with all stages:
    - Build & analyze (formatting, warnings-as-errors)
    - Unit tests with coverage threshold
    - Integration tests with Docker services (optional)
    - Architecture tests (optional)
    - Mutation tests with Stryker (optional)
    - NuGet package creation and publishing
    - CI summary report
  - Documentation: `docs/ci-cd-templates.md` with usage examples and best practices
  - Tests: `Encina.Workflows.Tests` project with 49 YAML validation tests

- **Encina.Testing Package** - Enhanced testing fixtures for fluent test setup (Issue #444):
  - `EncinaTestFixture` - Fluent builder pattern for test setup:
    - `WithMockedOutbox()`, `WithMockedInbox()`, `WithMockedSaga()` - Configure fake stores
    - `WithMockedScheduling()`, `WithMockedDeadLetter()` - Additional store mocking
    - `WithAllMockedStores()` - Enable all mocked stores at once
    - `WithHandler<THandler>()` - Register request/notification handlers
    - `WithService<TService>(instance)` - Register custom service instances
    - `WithService<TService, TImplementation>()` - Register service with implementation type
    - `WithConfiguration(Action<EncinaConfiguration>)` - Custom Encina configuration
    - `SendAsync<TResponse>(IRequest<TResponse>)` - Send request and get test context
    - `PublishAsync(INotification)` - Publish notification and get test context
    - Properties: `Outbox`, `Inbox`, `SagaStore`, `ScheduledMessageStore`, `DeadLetterStore`
    - `ClearStores()` - Reset all stores for test isolation
    - `IAsyncLifetime` support for xUnit integration
  - `EncinaTestContext<TResponse>` - Chainable assertion context:
    - `ShouldSucceed()`, `ShouldFail()` - Assert result state
    - `ShouldSucceedWith(Action<TResponse>)` - Assert and verify success value
    - `ShouldFailWith(Action<EncinaError>)` - Assert and verify error
    - `ShouldSatisfy(Action<TResponse>)` - Custom verification
    - `OutboxShouldContain<TNotification>()` - Verify outbox messages
    - `OutboxShouldBeEmpty()`, `OutboxShouldContainExactly(count)` - Outbox assertions
    - `SagaShouldBeStarted<TSaga>()` - Verify saga lifecycle
    - `SagaShouldHaveTimedOut<TSaga>()`, `SagaShouldHaveCompleted<TSaga>()` - Saga state assertions
    - `SagaShouldBeCompensating<TSaga>()`, `SagaShouldHaveFailed<TSaga>()` - Saga failure assertions
    - `GetSuccessValue()`, `GetErrorValue()` - Extract values
    - `And` property for fluent chaining
    - Implicit conversion to `Either<EncinaError, TResponse>`
  - **Time-Travel Testing** (Phase 3):
    - `WithFakeTimeProvider()`, `WithFakeTimeProvider(DateTimeOffset)` - Configure fake time
    - `AdvanceTimeBy(TimeSpan)`, `AdvanceTimeByMinutes(int)`, `AdvanceTimeByHours(int)` - Advance time
    - `AdvanceTimeByDays(int)`, `SetTimeTo(DateTimeOffset)`, `GetCurrentTime()` - Time control
    - `ThenAdvanceTimeBy()`, `ThenAdvanceTimeByHours()` - Context chaining for time-travel
    - `TimeProvider` property for direct FakeTimeProvider access
  - **Messaging Pattern Test Helpers** (Issue #169) - BDD Given/When/Then helpers for messaging patterns:
    - `OutboxTestHelper` - Fluent test helper for outbox pattern testing:
      - `GivenEmptyOutbox()`, `GivenMessages()`, `GivenPendingMessage()`, `GivenProcessedMessage()`, `GivenFailedMessage()` - Setup methods
      - `WhenMessageAdded()`, `WhenMessageProcessed()`, `WhenMessageFailed()`, `When()`, `WhenAsync()` - Action methods
      - `ThenMessageWasAdded<T>()`, `ThenOutboxContains<T>()`, `ThenMessageWasProcessed()`, `ThenNoMessagesWereAdded()`, `ThenNoException()`, `ThenThrows<T>()` - Assertion methods
      - Time-travel: `AdvanceTimeBy()`, `AdvanceTimeByMinutes()`, `AdvanceTimeByHours()`, `AdvanceTimeByDays()`, `GetCurrentTime()`
    - `InboxTestHelper` - Fluent test helper for inbox/idempotency testing:
      - `GivenEmptyInbox()`, `GivenNewMessage()`, `GivenProcessedMessage()`, `GivenFailedMessage()` - Setup methods
      - `WhenMessageReceived()`, `WhenMessageRegistered()`, `WhenMessageProcessed()`, `WhenMessageFailed()`, `When()`, `WhenAsync()` - Action methods
      - `ThenMessageWasRegistered()`, `ThenMessageIsProcessed()`, `ThenCachedResponseIs<T>()`, `ThenMessageWasDuplicate()`, `ThenNoException()`, `ThenThrows<T>()` - Assertion methods
    - `SagaTestHelper` - Fluent test helper for saga orchestration testing:
      - `GivenNoSagas()`, `GivenNewSaga<TSaga, TData>()`, `GivenRunningSaga<TSaga, TData>()`, `GivenCompletedSaga()`, `GivenFailedSaga()`, `GivenTimedOutSaga()` - Setup methods
      - `WhenSagaStarts<TSaga, TData>()`, `WhenSagaAdvancesToNextStep()`, `WhenSagaDataUpdated<TData>()`, `WhenSagaCompletes()`, `WhenSagaStartsCompensating()`, `WhenSagaFails()`, `WhenSagaTimesOut()` - Action methods
      - `ThenSagaWasStarted<TSaga>()`, `ThenSagaIsAtStep()`, `ThenSagaIsCompleted()`, `ThenSagaIsFailed()`, `ThenSagaIsCompensating()`, `ThenSagaData<TData>()`, `ThenNoException()`, `ThenThrows<T>()` - Assertion methods
    - `SchedulingTestHelper` - Fluent test helper for scheduled message testing:
      - `GivenNoScheduledMessages()`, `GivenScheduledMessage()`, `GivenRecurringMessage()`, `GivenDueMessage()`, `GivenProcessedMessage()`, `GivenFailedMessage()`, `GivenCancelledMessage()` - Setup methods
      - `WhenMessageScheduled()`, `WhenRecurringMessageScheduled()`, `WhenMessageProcessed()`, `WhenMessageFailed()`, `WhenMessageCancelled()`, `WhenMessageRescheduled()` - Action methods
      - `ThenMessageWasScheduled<T>()`, `ThenMessageIsDue<T>()`, `ThenMessageIsNotDue()`, `ThenMessageWasProcessed()`, `ThenMessageWasCancelled()`, `ThenMessageIsRecurring()`, `ThenMessageHasCron()`, `ThenScheduledMessageCount()` - Assertion methods
      - Time-travel: `AdvanceTimeBy()`, `AdvanceTimeByMinutes()`, `AdvanceTimeByHours()`, `AdvanceTimeByDays()`, `AdvanceTimeUntilDue()`, `GetCurrentTime()`, `GetDueMessagesAsync()`
  - **(NEW Issue #170)** Improved Assertions with Shouldly-like fluent chaining (xUnit-based):
    - `AndConstraint<T>` - Fluent chaining pattern for assertions:
      - `Value` property for accessing the asserted value
      - `And` property for continuing assertion chains
      - `ShouldSatisfy(Action<T>)` for custom assertions
      - Implicit conversion to underlying value type
    - `EitherAssertions` enhancements:
      - `ShouldBeSuccessAnd()`, `ShouldBeRightAnd()` returning `AndConstraint<TRight>`
      - `ShouldBeErrorAnd()`, `ShouldBeLeftAnd()` returning `AndConstraint<TLeft>`
      - `ShouldBeValidationErrorForProperty()`, `ShouldBeValidationErrorForPropertyAnd()` for property-specific validation
      - EncinaError `*And` variants: `ShouldBeErrorWithCodeAnd()`, `ShouldBeValidationErrorAnd()`, `ShouldBeAuthorizationErrorAnd()`, `ShouldBeNotFoundErrorAnd()`, `ShouldBeErrorContainingAnd()`
      - Async `*And` variants: `ShouldBeSuccessAndAsync()`, `ShouldBeErrorAndAsync()`, `ShouldBeValidationErrorAndAsync()`, etc.
    - `EitherCollectionAssertions` - Collection assertions for `IEnumerable<Either<TLeft, TRight>>`:
      - `ShouldAllBeSuccess()`, `ShouldAllBeSuccessAnd()` for all-success verification
      - `ShouldAllBeError()`, `ShouldAllBeErrorAnd()` for all-error verification
      - `ShouldContainSuccess()`, `ShouldContainSuccessAnd()`, `ShouldContainError()`, `ShouldContainErrorAnd()`
      - `ShouldHaveSuccessCount()`, `ShouldHaveSuccessCountAnd()`, `ShouldHaveErrorCount()`, `ShouldHaveErrorCountAnd()`
      - EncinaError-specific: `ShouldContainValidationErrorFor()`, `ShouldNotContainAuthorizationErrors()`, `ShouldContainAuthorizationError()`, `ShouldAllHaveErrorCode()`
      - Async variants: `ShouldAllBeSuccessAsync()`, `ShouldAllBeErrorAsync()`, `ShouldContainSuccessAsync()`, `ShouldContainErrorAsync()`
      - Helper methods: `GetSuccesses()`, `GetErrors()`
    - `StreamingAssertions` - `IAsyncEnumerable<Either<TLeft, TRight>>` assertions:
      - `ShouldAllBeSuccessAsync()`, `ShouldAllBeSuccessAndAsync()`, `ShouldAllBeErrorAsync()`, `ShouldAllBeErrorAndAsync()`
      - `ShouldContainSuccessAsync()`, `ShouldContainSuccessAndAsync()`, `ShouldContainErrorAsync()`, `ShouldContainErrorAndAsync()`
      - `ShouldHaveCountAsync()`, `ShouldHaveSuccessCountAsync()`, `ShouldHaveErrorCountAsync()`
      - `FirstShouldBeSuccessAsync()`, `FirstShouldBeErrorAsync()` for first-item assertions
      - `ShouldBeEmptyAsync()`, `ShouldNotBeEmptyAsync()` for stream emptiness
      - EncinaError-specific: `ShouldContainValidationErrorForAsync()`, `ShouldNotContainAuthorizationErrorsAsync()`, `ShouldContainAuthorizationErrorAsync()`, `ShouldAllHaveErrorCodeAsync()`
      - Helper: `CollectAsync()` to materialize async streams
  - **(NEW Issue #434)** BDD Handler and Saga Specification Testing:
    - `HandlerSpecification<TRequest, TResponse>` - Abstract base class for handler BDD testing:
      - `Given(Action<TRequest>)` - Setup request modifications
      - `GivenRequest(TRequest)` - Setup explicit request
      - `When(Action<TRequest>)`, `WhenAsync(Action<TRequest>, CancellationToken)` - Execute handler
      - `ThenSuccess(Action<TResponse>?)` - Assert success and validate
      - `ThenSuccessAnd()` - Assert success returning `AndConstraint<TResponse>`
      - `ThenError(Action<EncinaError>?)` - Assert error and validate
      - `ThenErrorAnd()` - Assert error returning `AndConstraint<EncinaError>`
      - `ThenValidationError(params string[])` - Assert validation error for properties
      - `ThenValidationErrorAnd(params string[])` - Returns `AndConstraint<EncinaError>`
      - `ThenErrorWithCode(string)` - Assert specific error code
      - `ThenErrorWithCodeAnd(string)` - Returns `AndConstraint<EncinaError>`
      - `ThenThrows<TException>()` - Assert exception thrown
      - `ThenThrowsAnd<TException>()` - Returns `AndConstraint<TException>`
    - `Scenario<TRequest, TResponse>` - Fluent inline scenario builder:
      - `Describe(string)` - Create named scenario
      - `Given(Action<TRequest>)` - Setup request modifications
      - `UsingHandler(Func<IRequestHandler<TRequest, TResponse>>)` - Set handler factory
      - `WhenAsync(TRequest, CancellationToken)` - Execute and return `ScenarioResult<TResponse>`
    - `ScenarioResult<TResponse>` - Result wrapper with assertions:
      - `IsSuccess`, `HasException`, `Result`, `Exception` properties
      - `ShouldBeSuccess(Action<TResponse>?)` - Assert success
      - `ShouldBeSuccessAnd()` - Returns `AndConstraint<TResponse>`
      - `ShouldBeError(Action<EncinaError>?)` - Assert error
      - `ShouldBeErrorAnd()` - Returns `AndConstraint<EncinaError>`
      - `ShouldBeValidationError(params string[])` - Assert validation error
      - `ShouldBeErrorWithCode(string)` - Assert specific error code
      - `ShouldThrow<TException>()` - Assert exception
      - `ShouldThrowAnd<TException>()` - Returns `AndConstraint<TException>`
      - Implicit conversion to `Either<EncinaError, TResponse>`
    - `SagaSpecification<TSaga, TSagaData>` - Abstract base class for saga BDD testing:
      - `GivenData(Action<TSagaData>)` - Setup saga data modifications
      - `GivenSagaData(TSagaData)` - Setup explicit saga data
      - `WhenComplete(CancellationToken)` - Execute saga from step 0
      - `WhenStep(int, CancellationToken)` - Execute saga from specific step
      - `WhenCompensate(int, CancellationToken)` - Execute compensation
      - `ThenSuccess(Action<TSagaData>?)` - Assert saga success
      - `ThenSuccessAnd()` - Returns `AndConstraint<TSagaData>`
      - `ThenError(Action<EncinaError>?)` - Assert saga error
      - `ThenErrorAnd()` - Returns `AndConstraint<EncinaError>`
      - `ThenErrorWithCode(string)` - Assert specific error code
      - `ThenThrows<TException>()` - Assert exception
      - `ThenThrowsAnd<TException>()` - Returns `AndConstraint<TException>`
      - `ThenCompleted()` - Assert saga completed successfully
      - `ThenCompensated()` - Assert compensation executed
      - `ThenFailed(string?)` - Assert saga failed with optional message
      - `ThenData(Action<TSagaData>)` - Validate saga data state
  - **(NEW Issue #362)** Module Testing Utilities for modular monolith testing:
    - `ModuleTestFixture<TModule>` - Fluent test fixture for isolated module testing:
      - `WithMockedModule<TModuleApi>(Action<MockModuleApi<TModuleApi>>)` - Mock dependent module with fluent setup
      - `WithMockedModule<TModuleApi>(TModuleApi)` - Mock dependent module with implementation
      - `WithFakeModule<TModuleApi, TFakeModule>()` - Register fake module implementation
      - `WithFakeModule<TModuleApi, TFakeModule>(TFakeModule)` - Register fake module instance
      - `WithService<TService>(TService)`, `WithService<TService, TImplementation>()` - Service registration
      - `ConfigureServices(Action<IServiceCollection>)` - Custom service configuration
      - `WithMockedOutbox()`, `WithMockedInbox()`, `WithMockedSaga()` - Messaging store mocking
      - `WithMockedScheduling()`, `WithMockedDeadLetter()`, `WithAllMockedStores()` - Additional stores
      - `WithFakeTimeProvider()`, `WithFakeTimeProvider(DateTimeOffset)` - Time control
      - `AdvanceTimeBy(TimeSpan)` - Time advancement
      - `Configure(Action<EncinaConfiguration>)` - Encina configuration
      - `SendAsync<TResponse>(IRequest<TResponse>)` - Send request returning `ModuleTestContext`
      - `PublishAsync(INotification)` - Publish notification capturing integration events
      - Properties: `Module`, `IntegrationEvents`, `Outbox`, `Inbox`, `SagaStore`, `ScheduledMessageStore`, `DeadLetterStore`, `TimeProvider`, `ServiceProvider`
      - `ClearStores()` - Reset all stores and captured events
      - `IDisposable` and `IAsyncDisposable` support
    - `ModuleTestContext<TResponse>` - Fluent assertion context for module test results:
      - `ShouldSucceed()`, `ShouldFail()` - Basic result assertions
      - `ShouldSucceedWith(Action<TResponse>)`, `ShouldFailWith(Action<EncinaError>)` - With validation
      - `ShouldSucceedAnd()`, `ShouldFailAnd()` - Return `AndConstraint<T>` for chaining
      - `ShouldFailWithMessage(string)` - Assert error contains message
      - `ShouldBeValidationError()` - Assert validation error
      - `OutboxShouldContain<T>()`, `OutboxShouldBeEmpty()`, `OutboxShouldHaveCount(int)` - Outbox assertions
      - `IntegrationEventShouldContain<T>()`, `IntegrationEventsShouldBeEmpty()` - Integration event assertions
      - Properties: `Fixture`, `Result`, `IsSuccess`, `Value`, `Error`
    - `IntegrationEventCollector` - Thread-safe collection for integration event assertions:
      - `Add(INotification)` (internal) - Capture events
      - `Clear()` - Clear captured events
      - `GetEvents<TEvent>()`, `GetFirst<TEvent>()`, `GetFirstOrDefault<TEvent>()`, `GetSingle<TEvent>()` - Query events
      - `Contains<TEvent>()`, `Contains<TEvent>(Func<TEvent, bool>)` - Check existence
      - `ShouldContain<TEvent>()`, `ShouldContain<TEvent>(Func<TEvent, bool>)` - Fluent assertions
      - `ShouldContainAnd<TEvent>()`, `ShouldContainSingle<TEvent>()`, `ShouldContainSingleAnd<TEvent>()` - With chaining
      - `ShouldNotContain<TEvent>()`, `ShouldBeEmpty()`, `ShouldHaveCount(int)`, `ShouldHaveCount<TEvent>(int)` - Additional assertions
    - `MockModuleApi<TModuleApi>` - Simple mock builder for module APIs using DispatchProxy:
      - `Setup(string methodName, Delegate)` - Configure method implementation
      - `SetupProperty(string propertyName, object?)` - Configure property value
      - `Build()` - Create proxy instance implementing the interface
    - `ModuleArchitectureRules` - Pre-built ArchUnitNET rules for modules:
      - `ModulesShouldBeSealed()` - Module implementations should be sealed
      - `IntegrationEventsShouldBeSealed()` - Integration events should be sealed
      - `DomainShouldNotDependOnInfrastructure(string domainNs, string infraNs)` - Layer dependency rule
    - `ModuleArchitectureAnalyzer` - Module dependency analysis:
      - `Analyze(params Assembly[])` - Analyze assemblies
      - `AnalyzeAssemblyContaining<T1>()`, `AnalyzeAssemblyContaining<T1, T2>()` - Type-based analysis
      - `Result` property returning `ModuleAnalysisResult`
      - `Architecture` property for ArchUnitNET access
    - `ModuleAnalysisResult` - Analysis result with assertions:
      - `Modules`, `Dependencies`, `CircularDependencies` properties
      - `ModuleCount`, `HasCircularDependencies` properties
      - `ShouldHaveNoCircularDependencies()` - Assert no cycles
      - `ShouldContainModule(string)` - Assert module exists
      - `ShouldHaveDependency(string source, string target)` - Assert dependency exists
      - `ShouldNotHaveDependency(string source, string target)` - Assert no dependency
    - Supporting records: `ModuleInfo`, `ModuleDependency`, `CircularDependency`
    - `DependencyType` enum: `Direct`, `PublicApi`, `IntegrationEvent`
  - **(NEW Issue #172)** Mutation Testing Helper Attributes (`Encina.Testing.Mutations` namespace):
    - `NeedsMutationCoverageAttribute` - Mark tests needing stronger assertions:
      - `Reason` (required) - Description of mutation coverage gap
      - `MutantId` (optional) - Stryker mutant ID from report
      - `SourceFile` (optional) - Path to source file with surviving mutant
      - `Line` (optional) - Line number where mutation was applied
      - `[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]` - Multiple instances per test allowed
    - `MutationKillerAttribute` - Document tests that kill specific mutations:
      - `MutationType` (required) - Type of mutation killed (e.g., "EqualityMutation", "ArithmeticMutation")
      - `Description` (optional) - Detailed description of what mutation is killed
      - `SourceFile` (optional) - Path to source file containing the mutation target
      - `TargetMethod` (optional) - Method name where mutation applies
      - `Line` (optional) - Line number where mutation applies
      - `[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]` - Multiple instances per test allowed
  - **(FIX Issue #497)** ModuleArchitectureAnalyzerTests: Fix false positive dependency detection:
    - **Root Cause**: Test modules (`OrdersModule`, `PaymentsModule`, `ShippingModule`) were in the same namespace, causing the analyzer to detect false dependencies
    - **Solution**: Moved test modules to distinct namespaces (`Encina.Testing.Tests.Modules.TestModules.Orders`, etc.) to simulate real modular architecture
    - **Additional Fix**: Adjusted `Result_DiscoversModulesInAssembly` test to verify module containment rather than exact match (assembly may contain additional test modules from other files)
    - Common mutation types: `EqualityMutation`, `ArithmeticMutation`, `BooleanMutation`, `UnaryMutation`, `NullCheckMutation`, `StringMutation`, `LinqMutation`, `BlockRemoval`
    - Documentation updated in `docs/en/guides/MUTATION_TESTING.md`

- **Encina.Testing.Testcontainers Package** - Docker container fixtures for integration tests (Issues #162, #163):
  - `SqlServerContainerFixture` - SQL Server container (mcr.microsoft.com/mssql/server:2022-latest)
  - `PostgreSqlContainerFixture` - PostgreSQL container (postgres:17-alpine)
  - `MySqlContainerFixture` - MySQL container (mysql:9.1)
  - `MongoDbContainerFixture` - MongoDB container (mongo:7)
  - `RedisContainerFixture` - Redis container (redis:7-alpine)
  - `ConfiguredContainerFixture<TContainer>` - Generic fixture for custom-configured containers
  - `EncinaContainers` - Static factory class for creating container fixtures:
    - `SqlServer()`, `PostgreSql()`, `MySql()`, `MongoDb()`, `Redis()` - Default configurations
    - Overloads accepting `Action<TBuilder>` for custom container configuration
  - All fixtures implement `IAsyncLifetime` for xUnit integration
  - Properties: `Container`, `ConnectionString`, `IsRunning`
  - Automatic cleanup with `WithCleanUp(true)`
  - **Respawn Integration** (Issue #163):
    - `DatabaseIntegrationTestBase<TFixture>` - Abstract base class combining Testcontainers with Respawn
    - `SqlServerIntegrationTestBase` - Pre-configured base class for SQL Server integration tests
    - `PostgreSqlIntegrationTestBase` - Pre-configured base class for PostgreSQL integration tests
    - `MySqlIntegrationTestBase` - Pre-configured base class for MySQL integration tests
    - Automatic database reset before each test via Respawn
    - Customizable `RespawnOptions` through property override
    - `ResetDatabaseAsync()` method for mid-test cleanup
    - Default exclusion of Encina messaging tables from cleanup

- **Encina.Testing.Architecture Package** - Architecture testing rules using ArchUnitNET (Issue #432, Phase 6 of #444):
  - `EncinaArchitectureRules` - Static class with pre-built architecture rules:
    - `HandlersShouldNotDependOnInfrastructure()` - Handlers should use abstractions
    - `HandlersShouldBeSealed()` - Handler classes should be sealed
    - `NotificationsShouldBeSealed()` - Notifications and events should be sealed
    - `BehaviorsShouldBeSealed()` - Pipeline behaviors should be sealed
    - `ValidatorsShouldFollowNamingConvention()` - Validators should end with "Validator"
    - `DomainShouldNotDependOnMessaging(namespace)` - Domain layer isolation
    - `DomainShouldNotDependOnApplication(domain, app)` - Domain independence
    - `ApplicationShouldNotDependOnInfrastructure(app, infra)` - Layer separation
    - `CleanArchitectureLayersShouldBeSeparated(domain, app, infra)` - Combined layer rules
    - `RepositoryInterfacesShouldResideInDomain(namespace)` - Repository interface location
    - `RepositoryImplementationsShouldResideInInfrastructure(namespace)` - Impl location
    - **(NEW Phase 6)** `RequestsShouldFollowNamingConvention()` - Requests should end with Command/Query
    - **(NEW Phase 6)** `AggregatesShouldFollowPattern(namespace)` - Aggregates should be sealed
    - **(NEW Phase 6)** `ValueObjectsShouldBeSealed()` - Value objects should be sealed
    - **(NEW Phase 6)** `SagasShouldBeSealed()` - Sagas should be sealed
    - **(NEW Phase 6)** `StoreImplementationsShouldBeSealed()` - Store impls should be sealed
    - **(NEW Phase 6)** `EventHandlersShouldBeSealed()` - Event handlers should be sealed
    - **(NEW Issue #166)** `HandlersShouldImplementCorrectInterface()` - Handlers must implement IRequestHandler, ICommandHandler, IQueryHandler, or INotificationHandler
    - **(NEW Issue #166)** `CommandsShouldImplementICommand()` - Commands must implement ICommand<TResponse> or ICommand
    - **(NEW Issue #166)** `QueriesShouldImplementIQuery()` - Queries must implement IQuery<TResponse> or IQuery
    - **(NEW Issue #166)** `HandlersShouldNotDependOnControllers()` - Handlers must not depend on presentation layer
    - **(NEW Issue #166)** `PipelineBehaviorsShouldImplementCorrectInterface()` - Behaviors must implement IPipelineBehavior<,>
    - **(NEW Issue #166)** `SagaDataShouldBeSealed()` - Saga data classes must be sealed for serialization
  - `EncinaArchitectureTestBase` - Abstract test class with pre-defined tests:
    - Override `ApplicationAssembly`, `DomainAssembly`, `InfrastructureAssembly`
    - Override namespace properties for layer separation rules
    - Pre-built test methods: `HandlersShouldNotDependOnInfrastructure()`, etc.
    - **(NEW Issue #166)** `HandlersShouldImplementCorrectInterface()` - Test handler interfaces
    - **(NEW Issue #166)** `CommandsShouldImplementICommand()` - Test command interfaces
    - **(NEW Issue #166)** `QueriesShouldImplementIQuery()` - Test query interfaces
    - **(NEW Issue #166)** `HandlersShouldNotDependOnControllers()` - Test handler-controller separation
    - **(NEW Issue #166)** `PipelineBehaviorsShouldImplementCorrectInterface()` - Test behavior interfaces
    - **(NEW Issue #166)** `SagaDataShouldBeSealed()` - Test saga data sealing
  - `EncinaArchitectureRulesBuilder` - Fluent builder for custom rule composition:
    - Chain multiple rules with fluent API
    - `Verify()` - Throws `ArchitectureRuleException` on violations
    - `VerifyWithResult()` - Returns `ArchitectureVerificationResult` without throwing
    - `ApplyAllStandardRules()` - Apply all standard Encina rules at once (excludes saga rules)
    - **(NEW)** `ApplyAllSagaRules()` - Apply saga-specific rules (opt-in)
    - `AddCustomRule(IArchRule)` - Add custom ArchUnitNET rules
    - **(NEW Phase 6)** `EnforceRequestNaming()` - Enforce request naming conventions
    - **(NEW Phase 6)** `EnforceSealedAggregates(namespace)` - Enforce sealed aggregates
    - **(NEW Phase 6)** `EnforceSealedValueObjects()` - Enforce sealed value objects
    - **(NEW Phase 6)** `EnforceSealedSagas()` - Enforce sealed sagas
    - **(NEW Phase 6)** `EnforceSealedEventHandlers()` - Enforce sealed event handlers
    - **(NEW Issue #166)** `EnforceHandlerInterfaces()` - Enforce handler interface implementation
    - **(NEW Issue #166)** `EnforceCommandInterfaces()` - Enforce command interface implementation
    - **(NEW Issue #166)** `EnforceQueryInterfaces()` - Enforce query interface implementation
    - **(NEW Issue #166)** `EnforceHandlerControllerIsolation()` - Enforce handler-controller separation
    - **(NEW Issue #166)** `EnforcePipelineBehaviorInterfaces()` - Enforce behavior interface implementation
    - **(NEW Issue #166)** `EnforceSealedSagaData()` - Enforce sealed saga data classes
  - `ArchitectureRuleViolation` - Record for rule violation details
  - `ArchitectureVerificationResult` - Result with `IsSuccess`, `IsFailure`, `Violations`
  - `ArchitectureRuleException` - Exception with formatted violation messages
- **Encina.Testing.Verify Package** - Snapshot testing integration with Verify (Issue #430):
  - `EncinaVerifySettings` - Configuration for Verify with Encina-specific scrubbers:
    - `Initialize()` - Configures scrubbers and converters (idempotent)
    - Automatic scrubbing of UTC timestamps (CreatedAtUtc, ProcessedAtUtc, etc.)
    - Automatic scrubbing of ISO 8601 timestamps in content
    - Stack trace removal from error messages
    - Custom EncinaError converter for clean output
  - `EncinaVerify` - Static helper methods for snapshot preparation:
    - `PrepareEither<TLeft, TRight>()` - Prepare Either results (shows IsRight, Value/Error)
    - `ExtractSuccess<TResponse>()` - Extract success value or throw
    - `ExtractError<TResponse>()` - Extract error or throw
    - `PrepareUncommittedEvents()` - Prepare aggregate events with metadata
    - `PrepareOutboxMessages()` - Prepare outbox messages for verification
    - `PrepareInboxMessages()` - Prepare inbox messages for verification
    - `PrepareSagaState()` - Prepare saga state for verification
    - `PrepareScheduledMessages()` - Prepare scheduled messages for verification
    - `PrepareDeadLetterMessages()` - Prepare dead letter messages for verification
    - **(NEW Phase 5)** `PrepareHandlerResult<TRequest, TResponse>()` - Prepare handler result with request context
    - **(NEW Phase 5)** `PrepareSagaStates()` - Prepare multiple saga states for verification
    - **(NEW Phase 5)** `PrepareValidationError()` - Prepare validation error for verification
    - **(NEW Phase 5)** `PrepareTestScenario<TResponse>()` - Prepare complete test scenario with outbox/sagas
  - **(NEW Phase 5)** `EncinaTestContextExtensions` - Extension methods for Either results:
    - `ForVerify<TResponse>()` - Prepare Either for snapshot verification
    - `SuccessForVerify<TResponse>()` - Extract success value for verification
    - `ErrorForVerify<TResponse>()` - Extract error for verification
  - Automatic GUID scrubbing with deterministic placeholders (Guid_1, Guid_2, etc.)
  - Integration with Verify.Xunit for xUnit test framework
- **Encina.Aspire.Testing Package** - Aspire integration testing support (Issue #418):
  - `WithEncinaTestSupport()` - Extension for `DistributedApplicationTestingBuilder`:
    - Registers fake stores for testing (outbox, inbox, saga, scheduled, dead letter)
    - Configurable data cleanup before each test
    - Customizable wait timeouts and polling intervals
  - `EncinaTestSupportOptions` - Configuration for test behavior:
    - `ClearOutboxBeforeTest`, `ClearInboxBeforeTest`, `ResetSagasBeforeTest`
    - `ClearScheduledMessagesBeforeTest`, `ClearDeadLetterBeforeTest`
    - `DefaultWaitTimeout`, `PollingInterval`
  - `EncinaTestContext` - Centralized access to test state and operations:
    - Direct access to fake stores for inspection
    - `ClearAll()`, `ClearOutbox()`, `ClearInbox()`, `ClearSagas()` methods
  - Assertion extensions for messaging patterns:
    - `AssertOutboxContainsAsync<T>()` - Verify outbox contains notification type
    - `AssertInboxProcessedAsync()` - Verify inbox message was processed
    - `AssertSagaCompletedAsync<T>()`, `AssertSagaCompensatedAsync<T>()` - Verify saga lifecycle
    - `AssertDeadLetterContainsAsync<T>()` - Verify dead letter contains message type
  - Wait helpers for async operations:
    - `WaitForOutboxProcessingAsync()` - Wait for all outbox messages to be processed
    - `WaitForSagaCompletionAsync<T>()` - Wait for specific saga to complete
  - Inspection helpers:
    - `GetPendingOutboxMessages()`, `GetRunningSagas<T>()`, `GetDeadLetterMessages()`
    - `GetEncinaTestContext()`, `GetOutboxStore()`, `GetSagaStore()`
  - Failure simulation for resilience testing:
    - `SimulateSagaTimeout()`, `SimulateSagaFailure()` - Saga failure scenarios
    - `SimulateOutboxMessageFailure()`, `SimulateOutboxDeadLetter()` - Outbox failures
    - `SimulateInboxMessageFailure()`, `SimulateInboxExpiration()` - Inbox failures
    - `AddToDeadLetterAsync()` - Directly add messages to dead letter store

- **Encina.Testing.Respawn Package** - Intelligent database cleanup for integration tests (Issue #427):
  - `DatabaseRespawner` - Abstract base class for provider-specific respawners
  - `SqlServerRespawner` - SQL Server implementation using Respawn library
  - `PostgreSqlRespawner` - PostgreSQL implementation using Respawn library
  - `MySqlRespawner` - MySQL/MariaDB implementation using Respawn library
  - `SqliteRespawner` - Custom SQLite implementation (Respawn doesn't support SQLite natively)
  - `RespawnOptions` - Configuration for table filtering, schema control, and Encina messaging tables
  - `RespawnerFactory` - Factory for creating respawners with automatic provider detection
  - `RespawnAdapter` - Enum for supported database adapters (SqlServer, PostgreSql, MySql, Oracle)
  - Features:
    - Foreign key-aware reset (deletes in correct dependency order)
    - `TablesToIgnore` - Exclude specific tables from cleanup
    - `SchemasToInclude`/`SchemasToExclude` - Schema filtering
    - `ResetEncinaMessagingTables` - Option to preserve Outbox/Inbox/Saga tables (default: true)
    - `WithReseed` - Reset identity columns (default: true)
    - `CheckTemporalTables` - Handle SQL Server temporal tables
    - `InferAdapter()` - Automatically detect database provider from connection string
    - Builder pattern support via `FromBuilder()` method
  - Async initialization with `InitializeAsync()` for lazy respawner setup
  - `GetDeleteCommands()` for debugging and verification

- **Encina.Testing.Bogus Package** - Realistic test data generation with Bogus (Issue #431):
  - `EncinaFaker<T>` - Base faker class for Encina requests with reproducibility:
    - Default seed (12345) for deterministic test data
    - `UseSeed()`, `WithLocale()`, `StrictMode()` configuration methods
    - Fluent API for custom rules
  - `OutboxMessageFaker` - Generate outbox messages for testing:
    - Default pending state with random notification types
    - `AsProcessed()` - Generate processed messages
    - `AsFailed(retryCount)` - Generate failed messages with error info
    - `WithNotificationType()`, `WithContent()` customization
  - `InboxMessageFaker` - Generate inbox messages for idempotency testing:
    - `AsProcessed(response)` - Generate processed with cached response
    - `AsFailed(retryCount)` - Generate failed messages
    - `AsExpired()` - Generate expired messages for cleanup tests
    - `WithMessageId()`, `WithRequestType()` customization
  - `SagaStateFaker` - Generate saga states for orchestration testing:
    - `AsCompleted()`, `AsCompensating()`, `AsFailed()`, `AsTimedOut()` lifecycle states
    - `WithSagaType()`, `WithSagaId()`, `WithData()`, `AtStep()` customization
  - `ScheduledMessageFaker` - Generate scheduled messages:
    - `AsDue()` - Generate messages ready for execution
    - `AsRecurring(cron)` - Generate recurring messages with cron expression
    - `AsRecurringExecuted()` - Generate recurring with last execution
    - `ScheduledAt()`, `WithRequestType()`, `WithContent()` customization
  - Extension methods for common Encina patterns:
    - Identifiers: `CorrelationId()`, `UserId()`, `TenantId()`, `IdempotencyKey()`
    - Types: `NotificationType()`, `RequestType()`, `SagaType()`, `SagaStatus()`
    - UTC dates: `RecentUtc()`, `SoonUtc()`
    - JSON: `JsonContent(propertyCount)`
  - Fake implementations: `FakeOutboxMessage`, `FakeInboxMessage`, `FakeSagaState`, `FakeScheduledMessage`
  - **Domain Model Faker Extensions** (Issue #161) - Extension methods for DDD patterns:
    - Entity ID generation (`Randomizer` extensions):
      - `EntityId<TId>()` - Generic type-switched entity ID generation (Guid, int, long, string)
      - `GuidEntityId()` - Generate non-empty GUID identifiers
      - `IntEntityId(min, max)` - Generate positive integer IDs (min >= 1)
      - `LongEntityId(min, max)` - Generate positive long IDs (min >= 1)
      - `StringEntityId(length, prefix)` - Generate alphanumeric IDs with optional prefix
    - Strongly-typed ID value generation (`Randomizer` extensions):
      - `StronglyTypedIdValue<TValue>()` - Generic value generation for StronglyTypedId
      - `GuidStronglyTypedIdValue()` - Non-empty GUID for StronglyTypedId<Guid>
      - `IntStronglyTypedIdValue(min, max)` - Positive int for StronglyTypedId<int>
      - `LongStronglyTypedIdValue(min, max)` - Positive long for StronglyTypedId<long>
      - `StringStronglyTypedIdValue(length, prefix)` - Alphanumeric for StronglyTypedId<string>
    - Value object generation (`Randomizer` and `Date` extensions):
      - `QuantityValue(min, max)` - Non-negative integers (default: 0-1000)
      - `PercentageValue(min, max, decimals)` - Decimal percentage 0-100 with precision
      - `DateRangeValue(daysInPast, daysSpan)` - (DateOnly Start, DateOnly End) tuple
      - `TimeRangeValue(minHourSpan, maxHourSpan)` - (TimeOnly Start, TimeOnly End) tuple
    - Seed reproducibility for all domain model methods:
      - All ID and value methods are fully reproducible with seed alone
      - Date/time methods (`DateRangeValue`, `TimeRangeValue`) are reproducible relative to the current base date/time (i.e., seed + current UTC date/time)

- **Encina.Testing.WireMock Package** - HTTP API mocking for integration tests (Issues #428, #164):
  - `EncinaWireMockFixture` - xUnit fixture for in-process WireMock server with fluent API:
    - HTTP method stubs: `StubGet()`, `StubPost()`, `StubPut()`, `StubPatch()`, `StubDelete()`
    - Advanced stubbing: `Stub()` with request configuration, `StubSequence()` for sequential responses
    - Fault simulation: `StubFault()` for EmptyResponse, MalformedResponse, Timeout
    - Delay simulation: `StubDelay()` for testing timeout handling
    - Request verification: `VerifyCallMade()`, `VerifyNoCallsMade()`, `GetReceivedRequests()`
    - Server management: `Reset()`, `ResetRequestHistory()`, `CreateClient()`
  - `WireMockContainerFixture` - Docker-based WireMock via Testcontainers:
    - Automatic container lifecycle management
    - Admin API access via `CreateAdminClient()`
    - Full isolation for CI/CD environments
  - **(NEW Issue #164)** `EncinaRefitMockFixture<TApiClient>` - Refit API client testing fixture:
    - Generic fixture for any Refit API interface
    - Auto-configured Refit client via `CreateClient()`
    - HTTP method stubs: `StubGet()`, `StubPost()`, `StubPut()`, `StubPatch()`, `StubDelete()`
    - Error simulation: `StubError()` with status code and error response
    - Delay simulation: `StubDelay()` for timeout testing
    - Request verification: `VerifyCallMade()`, `VerifyNoCallsMade()`
    - Server management: `Reset()`, `ResetRequestHistory()`
  - **(NEW Issue #164)** `WebhookTestingExtensions` - Webhook endpoint testing:
    - `SetupWebhookEndpoint()` - Generic webhook endpoint setup
    - `SetupOutboxWebhook()` - Outbox pattern webhook endpoint (expects JSON POST)
    - `SetupWebhookFailure()` - Simulate webhook failures for retry testing
    - `SetupWebhookTimeout()` - Simulate webhook timeouts for resilience testing
    - `VerifyWebhookReceived()` - Verify webhook was called with count
    - `VerifyNoWebhooksReceived()` - Verify no webhooks were sent
    - `GetReceivedWebhooks()` - Get all received webhook requests
    - `GetReceivedWebhookBodies<T>()` - Deserialize received webhook bodies
  - `FaultType` enum - Defines fault types: EmptyResponse, MalformedResponse, Timeout
  - `ReceivedRequest` record - Captures request path, method, headers, body, timestamp
  - Fluent method chaining for stub configuration
  - Automatic JSON serialization with camelCase naming

- **Encina.Testing.Shouldly Package** - Open-source assertion extensions (Issue #429):
  - `EitherShouldlyExtensions` - Shouldly-style assertions for `Either<TLeft, TRight>` types:
    - Success assertions: `ShouldBeSuccess()`, `ShouldBeRight()` with value/validator overloads
    - Error assertions: `ShouldBeError()`, `ShouldBeLeft()` with validator overloads
    - EncinaError-specific: `ShouldBeValidationError()`, `ShouldBeNotFoundError()`, `ShouldBeAuthorizationError()`, `ShouldBeConflictError()`, `ShouldBeInternalError()`
    - Code/message assertions: `ShouldBeErrorWithCode()`, `ShouldBeErrorContaining()`
    - Async versions: `ShouldBeSuccessAsync()`, `ShouldBeErrorAsync()`, etc.
  - `EitherCollectionShouldlyExtensions` - Batch operation assertions:
    - `ShouldAllBeSuccess()`, `ShouldAllBeError()` for verifying all results
    - `ShouldContainSuccess()`, `ShouldContainError()` for at-least-one verification
    - `ShouldHaveSuccessCount()`, `ShouldHaveErrorCount()` for exact counts
    - Helper methods: `GetSuccesses()`, `GetErrors()`
  - **(NEW Issue #170)** `StreamingShouldlyExtensions` - IAsyncEnumerable assertions:
    - `ShouldAllBeSuccessAsync()`, `ShouldAllBeErrorAsync()` for streaming results
    - `ShouldContainSuccessAsync()`, `ShouldContainErrorAsync()` for at-least-one verification
    - `ShouldHaveCountAsync()`, `ShouldHaveSuccessCountAsync()`, `ShouldHaveErrorCountAsync()` for counts
    - `FirstShouldBeSuccessAsync()`, `FirstShouldBeErrorAsync()` for first item assertions
    - `ShouldBeEmptyAsync()`, `ShouldNotBeEmptyAsync()` for stream emptiness
    - EncinaError-specific: `ShouldContainValidationErrorForAsync()`, `ShouldNotContainAuthorizationErrorsAsync()`, `ShouldContainAuthorizationErrorAsync()`, `ShouldAllHaveErrorCodeAsync()`
    - Helper: `CollectAsync()` to materialize async streams
  - `AggregateShouldlyExtensions` - Event sourcing assertions:
    - `ShouldHaveRaisedEvent<T>()` with predicate overloads
    - `ShouldHaveRaisedEvents<T>(count)` for multiple events
    - `ShouldNotHaveRaisedEvent<T>()` for negative assertions
    - `ShouldHaveNoUncommittedEvents()`, `ShouldHaveUncommittedEventCount()`
    - `ShouldHaveVersion()`, `ShouldHaveId()` for aggregate state
    - Helpers: `GetRaisedEvents<T>()`, `GetLastRaisedEvent<T>()`
  - Open-source alternative to FluentAssertions ($130/dev/year)

- **FakeTimeProvider** - Controllable time for testing (Issue #433):
  - `FakeTimeProvider` - Thread-safe TimeProvider implementation for testing time-dependent code
  - Time manipulation: `SetUtcNow()`, `Advance()`, `AdvanceToNextDay()`, `AdvanceToNextHour()`, `AdvanceMinutes()`, `AdvanceSeconds()`, `AdvanceMilliseconds()`
  - Timer support via `CreateTimer()` with full `ITimer` implementation
  - Frozen time scope via `Freeze()` method that restores time on dispose
  - `Reset()` methods to restore time and clear timers
  - `ActiveTimerCount` property for timer verification
  - Support for one-shot and periodic timers
  - Deterministic timer firing controlled by time advancement
  - **Concurrency Guarantees**:
    - **Thread-safe**: `SetUtcNow()`, `Advance()` (atomic updates); `CreateTimer()` (concurrent creation); `ActiveTimerCount`, `GetUtcNow()` (read-only accessors)
    - **Not thread-safe**: Manual timer manipulation (`Change()`, `Dispose()` on individual timers); composed sequences (e.g., read-then-advance across threads). These require external synchronization.

- **Encina.Testing.Fakes Package** - Test doubles for Encina components (Issue #426):
  - `FakeEncina` - In-memory IEncina implementation with verification methods
  - `FakeOutboxStore`, `FakeInboxStore`, `FakeSagaStore` - Messaging store fakes
  - `FakeScheduledMessageStore`, `FakeDeadLetterStore` - Additional store fakes
  - Thread-safe implementations preserving insertion order
  - Fluent API for setup: `SetupResponse()`, `SetupError()`, `SetupStream()`
  - Verification methods: `WasSent()`, `WasPublished()`, `GetSentRequests()`
  - DI extensions: `AddFakeEncina()`, `AddFakeStores()`, `AddEncinaTestingFakes()`

- **Encina.DomainModeling Package** - DDD tactical pattern building blocks (Issues #367, #369, #374):
  - `Entity<TId>` - Base class for entities with identity-based equality
  - `ValueObject` - Base class for value objects with structural equality
  - `SingleValueObject<TValue>` - Wrapper for single-value primitives with implicit conversion
  - `StronglyTypedId<TValue>` - Base class for type-safe identifiers with comparison and equality
  - `GuidStronglyTypedId<TSelf>` - GUID-based strongly typed IDs with `New()`, `From()`, `TryParse()`, `Empty`
  - `IntStronglyTypedId<TSelf>`, `LongStronglyTypedId<TSelf>`, `StringStronglyTypedId<TSelf>` - Numeric and string IDs
  - `AggregateRoot<TId>` - Base class for aggregate roots with domain event support
  - `AuditableAggregateRoot<TId>` - Aggregate with `CreatedAtUtc`, `CreatedBy`, `ModifiedAtUtc`, `ModifiedBy`
  - `SoftDeletableAggregateRoot<TId>` - Aggregate with soft delete (`IsDeleted`, `DeletedAtUtc`, `DeletedBy`)
  - `DomainEvent` and `RichDomainEvent` - Base records for domain events with correlation/causation tracking
  - `IntegrationEvent` - Base record for cross-boundary events with versioning
  - `IDomainEventToIntegrationEventMapper<TDomain, TIntegration>` - Anti-corruption layer mapper interface
  - Auditing interfaces: `IAuditable`, `ISoftDeletable`, `IConcurrencyAware`, `IVersioned`

- **Specification Pattern** (Issue #295) - Query composition and encapsulation:
  - `Specification<T>` - Base class with `And()`, `Or()`, `Not()` composition
  - `QuerySpecification<T>` - Extended specification with includes, ordering, paging, tracking options
  - `QuerySpecification<T, TResult>` - Specification with projection support via `Selector`
  - Expression tree composition for LINQ provider compatibility
  - Implicit conversion to `Expression<Func<T, bool>>`

- **Business Rules Pattern** (Issue #372) - Domain invariant validation:
  - `IBusinessRule` interface with `ErrorCode`, `ErrorMessage`, `IsSatisfied()`
  - `BusinessRule` abstract base class
  - `BusinessRuleViolationException` for throw-based validation
  - `BusinessRuleError` and `AggregateBusinessRuleError` records for ROP
  - `BusinessRuleExtensions`: `Check()`, `CheckFirst()`, `CheckAll()`, `ThrowIfNotSatisfied()`, `ThrowIfAnyNotSatisfied()`

- **Domain Service Marker** (Issue #377) - Semantic clarity:
  - `IDomainService` marker interface for domain services identification

- **Anti-Corruption Layer Pattern** (Issue #299) - Bounded context translation:
  - `TranslationError` record with factory methods (`UnsupportedType`, `MissingRequiredField`, `InvalidFormat`)
  - `IAntiCorruptionLayer<TExternal, TInternal>` sync interface
  - `IAsyncAntiCorruptionLayer<TExternal, TInternal>` async variant
  - `AntiCorruptionLayerBase<TExternal, TInternal>` with helper methods

- **Result Pattern Extensions** (Issue #468) - Fluent API for Either type:
  - Combination: `Combine()` for 2/3/4 values and collections
  - Conditional: `When()`, `Ensure()`, `OrElse()`, `GetOrDefault()`, `GetOrElse()`
  - Side effects: `Tap()`, `TapError()`
  - Async: `BindAsync()`, `MapAsync()`, `TapAsync()` (for `Task<Either>` and `Either`)
    - New Encina extensions in `Encina.Core.Extensions.EitherAsyncExtensions`
    - Namespace: `using Encina.Core.Extensions;`
    - Example: `Task<Either<L, R>> BindAsync<L, R, R2>(this Task<Either<L, R>> task, Func<R, Task<Either<L, R2>>> f)`
  - Conversion: `ToOption()`, `ToEither()` (from Option), `GetOrThrow()`

- **Rich Domain Event Envelope** (Issue #368) - Extended domain event metadata:
  - `IDomainEventMetadata` interface with `CorrelationId`, `CausationId`, `UserId`, `TenantId`, `AdditionalMetadata`
  - `DomainEventMetadata` record with factory methods (`Empty`, `WithCorrelation`, `WithCausation`)
  - `DomainEventEnvelope<TEvent>` - Wraps events with metadata, envelope ID, and timestamp
  - `DomainEventExtensions` - Fluent API: `ToEnvelope()`, `WithMetadata()`, `WithCorrelation()`, `Map()`

- **Integration Event Extensions** (Issue #373) - Cross-context event mapping:
  - `IAsyncDomainEventToIntegrationEventMapper<TDomain, TIntegration>` - Async mapper interface
  - `IFallibleDomainEventToIntegrationEventMapper<TDomain, TIntegration, TError>` - ROP mapper with Either
  - `IIntegrationEventPublisher` - Publisher interface with `PublishAsync()`, `PublishManyAsync()`
  - `IFallibleIntegrationEventPublisher<TError>` - ROP publisher variant
  - `IntegrationEventMappingError` and `IntegrationEventPublishError` - Structured error types
  - `IntegrationEventMappingExtensions` - `MapTo()`, `MapToAsync()`, `MapAll()`, `TryMapTo()`, `Compose()`

- **Generic Repository Pattern** (Issue #380) - Provider-agnostic data access:
  - `IReadOnlyRepository<TEntity, TId>` - Query operations with Specification support
  - `IRepository<TEntity, TId>` - Full CRUD operations extending read-only
  - `IAggregateRepository<TAggregate, TId>` - Aggregate-specific with `SaveAsync()`
  - `PagedResult<T>` - Pagination with `TotalPages`, `HasPreviousPage`, `HasNextPage`, `Map()`
  - `RepositoryError` - Error types (`NotFound`, `AlreadyExists`, `ConcurrencyConflict`, `OperationFailed`)
  - `RepositoryExtensions` - `GetByIdOrErrorAsync()`, `GetByIdOrThrowAsync()`, `AddIfNotExistsAsync()`, `UpdateIfExistsAsync()`
  - `EntityNotFoundException` - Exception for entity lookup failures

- **Ports & Adapters Factory Pattern** (Issue #475) - Hexagonal Architecture support:
  - `IPort`, `IInboundPort`, `IOutboundPort` - Port marker interfaces
  - `IAdapter<TPort>` - Adapter marker interface with port constraint
  - `AdapterBase<TPort>` - Base class with `Execute()`, `ExecuteAsync()` for error handling
  - `AdapterError` - Error types (`OperationFailed`, `Cancelled`, `NotFound`, `CommunicationFailed`, `ExternalError`)
  - `PortRegistrationExtensions` - DI registration: `AddPort<TPort, TAdapter>()`, `AddPortsFromAssembly()`

- **Result/DTO Mapping with ROP Semantics** (Issue #478) - Domain-to-DTO mapping:
  - `IResultMapper<TDomain, TDto>` - Sync mapper returning `Either<MappingError, TDto>`
  - `IAsyncResultMapper<TDomain, TDto>` - Async mapper variant
  - `IBidirectionalMapper<TDomain, TDto>` - Two-way mapping with `MapToDomain()`
  - `IAsyncBidirectionalMapper<TDomain, TDto>` - Async bidirectional variant
  - `MappingError` - Error types (`NullProperty`, `ValidationFailed`, `ConversionFailed`, `EmptyCollection`)
  - `ResultMapperExtensions` - `MapAll()`, `MapAllCollectErrors()`, `TryMap()`, `Compose()`
  - `ResultMapperRegistrationExtensions` - `AddResultMapper()`, `AddResultMappersFromAssembly()`

- **Application Services Interface** (Issue #479) - Use case orchestration:
  - `IApplicationService` - Marker interface for application services
  - `IApplicationService<TInput, TOutput>` - Typed service with `ExecuteAsync()`
  - `IApplicationService<TOutput>` - Parameterless service for scheduled tasks
  - `IVoidApplicationService<TInput>` - Service returning `Unit` on success
  - `ApplicationServiceError` - Error types (`NotFound`, `ValidationFailed`, `BusinessRuleViolation`, `ConcurrencyConflict`, `InfrastructureFailure`, `Unauthorized`)
  - `ApplicationServiceExtensions` - `ToApplicationServiceError()` for error conversion
  - `ApplicationServiceRegistrationExtensions` - `AddApplicationService()`, `AddApplicationServicesFromAssembly()`

- **Bounded Context Patterns** (Issues #379, #477) - Strategic DDD support:
  - `BoundedContextAttribute` - Mark types with bounded context membership
  - `ContextMap` - Document relationships between contexts with fluent API
  - `ContextRelationship` enum - Conformist, ACL, SharedKernel, CustomerSupplier, PublishedLanguage, SeparateWays, Partnership, OpenHost
  - `ContextRelation` record - Stores upstream/downstream context with relationship type
  - `BoundedContextModule` abstract class - Modular monolith module base with DI configuration
  - `IBoundedContextModule` interface - Module contract with `Name`, `Dependencies`, `ConfigureServices()`
  - `BoundedContextValidator` - Validate circular dependencies and orphaned consumers
  - `BoundedContextError` - Error types (`OrphanedConsumer`, `CircularDependency`, `ValidationFailed`)
  - `BoundedContextExtensions` - `GetBoundedContextName()`, `AddBoundedContextModules()`, `ValidateBoundedContexts()`
  - Mermaid diagram generation via `ToMermaidDiagram()`

- **Domain Language DSL** (Issue #381) - Fluent domain building:
  - `DomainBuilder<T, TBuilder>` - CRTP fluent builder with ROP
  - `AggregateBuilder<TAggregate, TId, TBuilder>` - Aggregate builder with business rule validation
  - `DomainBuilderError` - Error types (`MissingValue`, `ValidationFailed`, `BusinessRulesViolated`, `InvalidState`)
  - `DomainDslExtensions` - Fluent specification checks: `Is()`, `Satisfies()`, `Violates()`, `Passes()`, `Fails()`
  - Fluent validation: `EnsureValid()`, `EnsureNotNull()`, `EnsureNotEmpty()` returning Either
  - **Common Domain Types**:
    - `Quantity` struct - Non-negative quantity with arithmetic operators
    - `Percentage` struct - 0-100 percentage with `ApplyTo()`, `AsFraction`, `Complement`
    - `DateRange` struct - Date range with `Contains()`, `Overlaps()`, `Intersect()`, `ExtendBy()`
    - `TimeRange` struct - Time range with `Duration`, `Contains()`, `Overlaps()`

- **Vertical Slice + Hexagonal Hybrid Architecture** (Issue #476) - Feature slices:
  - `FeatureSlice` abstract class - Base for vertical slices with `FeatureName`, `ConfigureServices()`
  - `IFeatureSliceWithEndpoints` - Slices with HTTP endpoint configuration
  - `IFeatureSliceWithDependencies` - Slices with explicit inter-slice dependencies
  - `SliceDependency` record - Represents dependency on another slice (optional flag)
  - `FeatureSliceConfiguration` - Fluent configuration: `AddSlice<T>()`, `AddSlicesFromAssembly()`
  - `FeatureSliceExtensions` - `AddFeatureSlices()`, `AddFeatureSlice<T>()`, `GetFeatureSlices()`, `GetFeatureSlice()`
  - `FeatureSliceError` - Error types (`MissingDependency`, `CircularDependency`, `RegistrationFailed`)
  - **Use Case Handlers**:
    - `IUseCaseHandler` marker interface
    - `IUseCaseHandler<TInput, TOutput>` - Handler with input and output
    - `IUseCaseHandler<TInput>` - Command handler (void output)
    - `UseCaseHandlerExtensions` - `AddUseCaseHandler<T>()`, `AddUseCaseHandlersFromAssembly()`

- Comprehensive test coverage: 175 unit tests, 275 property tests, 531 contract tests, 80 guard tests (1061 total).
  - **Note**: Load tests (`[Trait("Category", "Load")]`) are excluded from default CI runs due to a .NET 10 JIT bug.
  - **Known Issue - CLR Crash on .NET 10** (Encina Issue #5):
    - **Scope**: Affects load tests only (NBomber + `IAsyncEnumerable<Either<EncinaError, T>>` under high concurrency). Production code is not affected.
    - **Affected Versions**: .NET 10.0.x (all current releases)
    - **Upstream Bug**: [dotnet/runtime#121736](https://github.com/dotnet/runtime/issues/121736) - Fixed in .NET 11, awaiting .NET 10.x backport
    - **CI/CD Mitigation**: Load tests are excluded via project name pattern (`*LoadTests*`) in [.github/workflows/ci.yml](.github/workflows/ci.yml). Dedicated load test workflow runs separately with workaround.
    - **Local Workaround**: Set `DOTNET_JitObjectStackAllocationConditionalEscape=0` before running load tests
    - **Internal Docs**: See [docs/releases/pre-v0.10.0/README.md](docs/releases/pre-v0.10.0/README.md#clr-crash-on-net-10-issue-5) "Known Issues" section

#### AI/LLM Patterns Issues (12 new features planned based on December 29, 2025 research)

- **MCP (Model Context Protocol) Support** (Issue #481) - MCP server/client integration
  - `MCPServerBuilder` for creating MCP servers in C#
  - `MCPClientBehavior` for consuming external MCP tools
  - Native integration with `IEncina` - expose handlers as AI tools
  - SSE and HTTP transports
  - Azure Functions support for remote MCP servers
  - Priority: HIGH - Industry standard adopted by OpenAI, Anthropic, Microsoft
- **Semantic Caching Pipeline Behavior** (Issue #482) - Embedding-based cache
  - `SemanticCachingPipelineBehavior<TRequest, TResponse>`
  - `ISemanticCacheProvider` abstraction with Redis, Qdrant providers
  - Similarity threshold configurable (default 0.95)
  - Reduces LLM costs by 40-70%, latency from 850ms to <120ms
  - New packages planned: `Encina.AI.Caching.Redis`, `Encina.AI.Caching.Qdrant`
  - Priority: HIGH - Major cost reduction
- **AI Guardrails & Safety Pipeline** (Issue #483) - Security for AI applications
  - `PromptInjectionDetectionBehavior` - OWASP #1 threat for LLMs
  - `PIIDetectionBehavior` - Detect and redact sensitive data
  - `ContentModerationBehavior` - Filter harmful content
  - `IGuardrailProvider` abstraction with Azure Prompt Shields, AWS Bedrock, OpenGuardrails
  - Configurable actions: Block, Warn, Log, Redact
  - New package planned: `Encina.AI.Safety`
  - Priority: HIGH - Essential for production AI
- **RAG Pipeline Patterns** (Issue #484) - Retrieval-Augmented Generation
  - `IRagPipeline<TQuery, TResponse>` abstraction
  - Query rewriting (multi-query, HyDE), chunk retrieval, re-ranking
  - Hybrid search (keyword + semantic)
  - Agentic RAG with query planning
  - Citation/source tracking
  - New package planned: `Encina.AI.RAG`
  - Priority: HIGH - Most demanded LLM pattern
- **Token Budget & Cost Management** (Issue #485) - LLM cost control
  - `TokenBudgetPipelineBehavior<TRequest, TResponse>`
  - `ITokenBudgetStore` with Redis/SQL providers
  - Per-user/tenant/request type limits
  - Cost estimation and reporting
  - Automatic fallback to cheaper models
  - Priority: MEDIUM - Enterprise cost control
- **LLM Observability Integration** (Issue #486) - AI-specific metrics
  - Enhancement to `Encina.OpenTelemetry`
  - `LLMActivityEnricher` with GenAI semantic conventions
  - Token usage metrics (input/output/cached/reasoning)
  - Time to first token (TTFT) measurement
  - Cost attribution per model/user/tenant
  - Integration with Langfuse, Datadog LLM Observability
  - Priority: HIGH - Production monitoring
- **Multi-Agent Orchestration Patterns** (Issue #487) - AI agent workflows
  - `IAgent` and `IAgentHandler<TRequest, TResponse>` interfaces
  - Orchestration patterns: Sequential, Concurrent, Handoff, GroupChat, Magentic
  - `IAgentSelector` for dynamic routing
  - Human-in-the-Loop (HITL) support
  - Cross-language agents via MCP
  - Semantic Kernel adapter: `SemanticKernelAgentAdapter`
  - New package planned: `Encina.Agents`
  - Priority: MEDIUM - Microsoft Agent Framework
- **Structured Output Handler** (Issue #488) - JSON schema enforcement
  - `IStructuredOutputHandler<TRequest, TOutput>` interface
  - `IJsonSchemaGenerator` with System.Text.Json support
  - Response validation with retry on failure
  - Fallback parsing for edge cases
  - Priority: MEDIUM - Schema guarantees
- **Function Calling Orchestration** (Issue #489) - LLM tool use
  - `IFunctionCallingOrchestrator` interface
  - `[AIFunction]` attribute for handler decoration
  - Auto/Manual/Confirm invocation modes
  - Parallel function calls support
  - Semantic Kernel plugin adapter: `EncinaPluginAdapter`
  - Priority: MEDIUM - Native LLM feature
- **Vector Store Abstraction** (Issue #490) - Embedding storage
  - `IVectorStore` and `IVectorRecord` abstractions
  - Integration with `IEmbeddingGenerator` (Microsoft.Extensions.AI)
  - Metadata filtering, hybrid search, batch operations
  - New packages planned: `Encina.VectorData`, `Encina.VectorData.Qdrant`, `Encina.VectorData.AzureSearch`, `Encina.VectorData.Milvus`, `Encina.VectorData.Chroma`, `Encina.VectorData.InMemory`
  - Priority: HIGH - Foundation for RAG and Semantic Cache
- **Prompt Management & Versioning** (Issue #491) - Enterprise prompt governance
  - `IPromptRepository` and `IPromptTemplateEngine`
  - Versioned prompt templates with A/B testing
  - Prompt analytics and performance tracking
  - Storage providers: FileSystem (YAML/JSON), Database, Git
  - Priority: LOW - Enterprise governance
- **AI Streaming Pipeline Enhancement** (Issue #492) - Token-level streaming
  - `IAIStreamRequest<TChunk>` and `TokenChunk` types
  - `BackpressureStreamBehavior` for slow consumers
  - SSE endpoint helper in `Encina.AspNetCore`
  - Time to first token (TTFT) measurement
  - Integration with `IChatClient.CompleteStreamingAsync`
  - Priority: MEDIUM - UX enhancement

#### Hexagonal Architecture Patterns Issues (10 new features planned based on December 29, 2025 research)

- **Domain Events vs Integration Events** (Issue #470) - Formal separation between domain events and integration events
    - `DomainEvent` base class with AggregateId, OccurredAtUtc, Version
    - `IntegrationEvent` base class extending INotification with EventType, CorrelationId
    - `IDomainEventHandler<TEvent>` for in-process synchronous processing
    - Integration with existing Outbox pattern for integration event publishing
    - Priority: CRITICAL - Foundational for DDD/microservices
  - **Specification Pattern** (Issue #471) - Composable, testable query encapsulation
    - `Specification<T>` and `Specification<T, TResult>` base classes
    - `ISpecificationRepository<T>` with EF Core and Dapper support
    - And/Or/Not composition operators
    - New packages planned: `Encina.Specifications`, `Encina.Specifications.EFCore`
    - Priority: CRITICAL - High demand (~9M downloads for Ardalis.Specification)
  - **Value Objects & Aggregates** (Issue #472) - DDD building blocks
    - `ValueObject` with structural equality
    - `StronglyTypedId<T>` for type-safe identifiers
    - `Entity<TId>` for non-root entities
    - `AggregateRoot<TId>` with domain events and version for concurrency
    - New package planned: `Encina.DDD`
    - Priority: CRITICAL - Core DDD patterns
  - **Domain Services** (Issue #473) - IDomainService marker interface
    - Pure domain logic without infrastructure dependencies
    - Auto-registration via `AddDomainServicesFromAssembly()`
    - Priority: HIGH - Complements handlers with domain logic
  - **Anti-Corruption Layer** (Issue #474) - External system isolation
    - `IAntiCorruptionLayer<TExternal, TDomain>` bidirectional interface
    - `IInboundAntiCorruptionLayer` and `IOutboundAntiCorruptionLayer`
    - ROP semantics for all translations
    - New package planned: `Encina.Hexagonal`
    - Priority: HIGH - Essential for integrations
  - **Ports & Adapters Factory** (Issue #475) - Hexagonal architecture formalization
    - `IPort`, `IInboundPort`, `IOutboundPort` marker interfaces
    - `AddPort<TPort, TAdapter>()` registration method
    - `AdapterBase<TPort>` with logging and error handling
    - Priority: HIGH - Formalizes hexagonal boundaries
  - **Vertical Slice + Hexagonal Hybrid** (Issue #476) - Feature organization
    - `IFeatureSlice` extending `IModule`
    - `MapEncinaSlices()` for endpoint mapping
    - Combines vertical slice organization with hexagonal boundaries
    - Priority: MEDIUM - Architectural guidance
  - **Bounded Context Modules** (Issue #477) - Module boundary contracts
    - `IBoundedContextModule` with PublishedIntegrationEvents and ConsumedIntegrationEvents
    - `IContextMap` for relationship visualization
    - Mermaid diagram generation
    - Priority: MEDIUM - SaaS and modular monolith support
  - **Result/DTO Mapping** (Issue #478) - Domain to DTO with ROP
    - `IResultMapper<TDomain, TDto>` interface
    - `IAsyncResultMapper<TDomain, TDto>` for async mappings
    - `MapAll()` and `MapAllCollectErrors()` extensions
    - Priority: MEDIUM
  - **Application Services** (Issue #479) - Use case orchestration
    - `IApplicationService<TInput, TOutput>` interface
    - Clear separation from Domain Services (logic vs orchestration)
    - Priority: MEDIUM

- New labels created for Hexagonal Architecture Patterns:
  - `area-application-services` - Application Services and use case orchestration (#2E8B57)
  - `area-ports-adapters` - Ports and Adapters (Hexagonal Architecture) patterns (#4169E1)
  - `area-hexagonal` - Hexagonal Architecture patterns and infrastructure (#6A5ACD)
  - `area-dto-mapping` - DTO mapping and object transformation patterns (#9370DB)
  - `clean-architecture` - Clean Architecture patterns and structure (#32CD32)
  - `abp-inspired` - Pattern inspired by ABP Framework (#FF6B6B)
  - `ardalis-inspired` - Pattern inspired by Steve Smith (Ardalis) libraries (#FF8C00)

#### TDD Patterns Issues (12 new features planned based on December 29, 2025 research)

- **Encina.Testing.Fakes** (Issue #426) - Test doubles for IEncina and messaging stores
    - `FakeEncina : IEncina` with configurable handlers
    - `FakeOutboxStore`, `FakeInboxStore`, `FakeSagaStore`, `FakeScheduledMessageStore`
    - Verification methods: `VerifySent<T>()`, `VerifyPublished<T>()`
    - Implemented: `Encina.Testing.Fakes`
    - Priority: HIGH - Foundational for unit testing
  - **Encina.Testing.Respawn** (Issue #427) - Intelligent database reset with Respawn
    - `RespawnDatabaseFixture<TContainer>` base class
    - FK-aware deterministic deletion (3x faster than truncate)
    - Integration with existing Testcontainers fixtures
    - Implemented: `Encina.Testing.Respawn`
    - Priority: HIGH - Essential for integration testing
  - **Encina.Testing.WireMock** (Issue #428) - HTTP API mocking for integration tests
    - `EncinaWireMockFixture` with fluent stubbing API
    - Fault simulation for resilience testing
    - Implemented: `Encina.Testing.WireMock`
    - Priority: HIGH - External API testing
  - **Encina.Testing.Shouldly** (Issue #429) - Open-source assertions (FluentAssertions alternative)
    - `ShouldBeSuccess()`, `ShouldBeError()` for `Either<EncinaError, T>`
    - Replaces FluentAssertions after commercial license change (Jan 2025)
    - Implemented: `Encina.Testing.Shouldly`
    - Priority: HIGH - Open-source assertion library
  - **Encina.Testing.Verify** (Issue #430) - Snapshot testing integration
    - `VerifyEither()`, `VerifyUncommittedEvents()`, `VerifySagaState()`
    - Automatic scrubbers for timestamps and GUIDs
    - New package planned: `Encina.Testing.Verify`
    - Priority: MEDIUM
  - **Encina.Testing.Bogus** (Issue #431) - Realistic test data generation
    - `EncinaFaker<TRequest>` base class with conventions
    - Pre-built fakers for messaging entities
    - New package planned: `Encina.Testing.Bogus`
    - Priority: MEDIUM
  - **Encina.Testing.Architecture** (Issue #432) - Architectural rules enforcement
    - `EncinaArchitectureRules` with CQRS/DDD rules
    - ArchUnitNET integration
    - New package planned: `Encina.Testing.Architecture`
    - Priority: MEDIUM
  - **FakeTimeProvider** (Issue #433) - Time control for testing
    - `FakeTimeProvider : TimeProvider` (.NET 8+ compatible)
    - `Advance()`, `SetUtcNow()`, `Freeze()` methods
    - Priority: MEDIUM - Added to Encina.Testing core
  - **BDD Specification Testing** (Issue #434) - Given/When/Then for handlers
    - `HandlerSpecification<TRequest, TResponse>` base class
    - Extension of existing `AggregateTestBase` pattern
    - Priority: LOW
  - **Encina.Testing.FsCheck** (Issue #435) - Property-based testing extensions
    - `EncinaArbitraries` for Encina types
    - `EncinaProperties` for common invariants
    - New package planned: `Encina.Testing.FsCheck`
    - Priority: LOW - Advanced testing
  - **Encina.Testing.Pact** (Issue #436) - Consumer-Driven Contract Testing
    - `EncinaPactConsumerBuilder`, `EncinaPactProviderVerifier`
    - Microservices contract verification
    - New package planned: `Encina.Testing.Pact`
    - Priority: LOW - Microservices focus
  - **Stryker.NET Configuration** (Issue #437) - Mutation testing templates
    - `stryker-config.json` templates
    - GitHub Actions and Azure DevOps workflows
    - `encina generate stryker` CLI command
    - Priority: LOW - Quality tooling

- New labels created for TDD Patterns:
  - `testing-property-based` - Property-based testing and invariant verification (#9B59B6)
  - `testing-contract` - Contract testing and consumer-driven contracts (#8E44AD)
  - `testing-bdd` - Behavior-driven development and Given/When/Then patterns (#A569BD)
  - `testing-time-control` - Time manipulation and FakeTimeProvider for testing (#7D3C98)
  - `testing-assertions` - Assertion libraries and fluent assertions (#6C3483)
  - `testing-database-reset` - Database cleanup and reset between tests (#5B2C6F)

#### Developer Tooling & DX Issues (11 new features planned based on December 29, 2025 research)

- **Encina.Analyzers** (Issue #438) - Roslyn analyzers and code fixes
    - 10+ analyzers: ENC001 (CancellationToken), ENC002 (Validator missing), ENC003 (Saga compensation)
    - Code fixes: generate handler skeleton, add CancellationToken, implement IIdempotentRequest
    - Compatible with NativeAOT and Source Generators
    - New package planned: `Encina.Analyzers`
    - Priority: HIGH - Compile-time error detection
  - **Saga Visualizer** (Issue #439) - State machine diagram generation
    - Generate Mermaid, Graphviz (DOT), PlantUML from saga definitions
    - Runtime visualization with current state highlighted
    - CLI: `encina visualize saga OrderFulfillmentSaga --output mermaid`
    - Priority: MEDIUM - Documentation and debugging
  - **Encina.Aspire** (Issue #440) - .NET Aspire integration package
    - `EncinaResource` as first-class Aspire resource
    - Dashboard panel for Outbox, Inbox, Sagas
    - Health checks and OTLP pre-configured
    - New packages planned: `Encina.Aspire`, `Encina.Aspire.Hosting`
    - Priority: HIGH - Modern .NET stack integration
  - **Encina.Diagnostics** (Issue #441) - Enhanced exception formatting
    - Pretty-print for `EncinaError` with box-drawing characters
    - Demystified stack traces (Ben.Demystifier-inspired)
    - Validation errors grouped by property
    - ANSI colors with plain text fallback
    - New package planned: `Encina.Diagnostics`
    - Priority: MEDIUM - Developer experience
  - **Hot Reload Support** (Issue #442) - Handler hot reload
    - Integration with `MetadataUpdateHandler.ClearCache`
    - Automatic pipeline cache invalidation
    - Development-only (no production impact)
    - Priority: MEDIUM - Inner loop improvement
  - **AI-Ready Request Tracing** (Issue #443) - Request/response capture
    - Automatic serialization with PII redaction
    - `[Trace]` attribute with RedactProperties
    - AI-compatible export format
    - Sampling strategies (errors always, slow requests, random)
    - Priority: MEDIUM - Modern debugging
  - **Enhanced Testing Fixtures** (Issue #444) - Testing improvements
    - `EncinaTestFixture` with fluent builder
    - Either assertions: `ShouldBeSuccess()`, `ShouldBeError()`
    - Time-travel for sagas: `AdvanceTimeBy()`
    - Outbox/Inbox assertions: `OutboxShouldContain<T>()`
    - Priority: MEDIUM - Testing DX
  - **Encina.Dashboard** (Issue #445) - Developer dashboard web UI
    - Local web UI for debugging (Hangfire Dashboard-style)
    - Panels: Handlers, Pipeline, Outbox, Inbox, Sagas, Cache, Errors
    - Real-time updates via SignalR
    - Actions: Retry Outbox, Cancel Saga, Invalidate Cache
    - New package planned: `Encina.Dashboard`
    - Priority: HIGH - Development visibility
  - **Encina.OpenApi** (Issue #446) - OpenAPI integration
    - Auto-generate OpenAPI spec from Commands/Queries
    - `app.MapEncinaEndpoints()` for endpoint generation
    - FluentValidation constraints in schema
    - OpenAPI 3.1 support (.NET 10)
    - New package planned: `Encina.OpenApi`
    - Priority: MEDIUM - API-first development
  - **Dev Containers Support** (Issue #447) - Container development
    - `.devcontainer/` configuration
    - GitHub Codespaces support
    - Docker Compose with Postgres, Redis, RabbitMQ
    - CLI: `encina add devcontainer --services postgres,redis`
    - Priority: LOW - Developer onboarding
  - **Interactive Documentation** (Issue #448) - Documentation site
    - Docusaurus or DocFX site
    - Playground with executable code
    - API reference from XML docs
    - Versioned documentation
    - Priority: LOW - Community growth

- New labels created for Developer Tooling:
  - `area-analyzers` - Roslyn analyzers and code fixes (#5C2D91)
  - `area-visualization` - Visualization and diagram generation (#9B59B6)
  - `area-dashboard` - Developer dashboard and monitoring UI (#E74C3C)
  - `area-diagnostics` - Diagnostics, error formatting, and debugging aids (#E67E22)
  - `area-devcontainers` - Dev Containers and Codespaces support (#0DB7ED)
  - `area-documentation-site` - Documentation website and interactive docs (#3498DB)

#### .NET Aspire Integration Patterns Issues (10 new features planned based on December 29, 2025 research)

- **Encina.Aspire.Hosting** (Issue #416) - AppHost integration package
    - `WithEncina()` extension method for `IResourceBuilder<ProjectResource>`
    - Custom `EncinaResource` for Dashboard visibility
    - Configuration propagation via environment variables
    - Custom commands: Process Outbox, Retry Dead Letters, Cancel Saga
    - New package planned: `Encina.Aspire.Hosting`
    - Priority: MEDIUM - Foundational for Aspire integration
  - **Encina.Aspire.ServiceDefaults** (Issue #417) - Service Defaults extension
    - `AddEncinaDefaults()` for `IHostApplicationBuilder`
    - OpenTelemetry integration (tracing + metrics)
    - Health checks for all messaging patterns
    - Standard Resilience pipeline integration
    - New package planned: `Encina.Aspire.ServiceDefaults`
    - Priority: HIGH - Centralizes cross-cutting concerns
  - **Encina.Aspire.Testing** (Issue #418) - Testing integration
    - `WithEncinaTestSupport()` for `DistributedApplicationTestingBuilder`
    - Assertion extensions: `AssertOutboxContains<T>()`, `AssertSagaCompleted<T>()`
    - Test data reset helpers (clear outbox, inbox, sagas)
    - Wait helpers: `WaitForOutboxProcessing()`, `WaitForSagaCompletion()`
    - New package planned: `Encina.Aspire.Testing`
    - Priority: HIGH - "Largest gap" per official Aspire roadmap
  - **Encina.Aspire.Dashboard** (Issue #419) - Dashboard extensions
    - Custom commands via `WithCommand()` API
    - Encina-specific metrics visibility
    - Commands: `process-outbox`, `retry-dead-letters`, `cancel-saga`
    - New package planned: `Encina.Aspire.Dashboard`
    - Priority: MEDIUM - Improves observability
  - **Encina.Dapr** (Issue #420) - Dapr building blocks integration
    - Dapr State Store for Saga/Scheduling state
    - Dapr Pub/Sub for Outbox publishing
    - Dapr Service Invocation for inter-service commands
    - Dapr Actors for saga orchestration (optional)
    - New package planned: `Encina.Dapr`
    - Priority: MEDIUM - CNCF graduated, high demand
  - **Encina.Aspire.Deployment** (Issue #421) - Deployment publishers
    - Azure Container Apps publisher with Encina infrastructure
    - Kubernetes manifests generation
    - Docker Compose environment support
    - KEDA scaling rules for processors
    - New package planned: `Encina.Aspire.Deployment`
    - Priority: LOW - azd already automates much
  - **Encina.Aspire.AI** (Issue #422) - AI Agent & MCP Server support
    - MCP Server to expose Encina state to AI agents
    - Tools: `analyze_saga_failure`, `retry_dead_letter`
    - Azure AI Foundry integration
    - Dashboard Copilot integration
    - New package planned: `Encina.Aspire.AI`
    - Priority: LOW - Roadmap 2026
  - **Modular Monolith Architecture Support** (Issue #423)
    - `IEncinaModule` interface with lifecycle hooks
    - `WithEncinaModules()` for AppHost
    - Inter-module communication via Encina messaging
    - Module isolation with separate DbContexts
    - Priority: MEDIUM - Trending architecture 2025
  - **Multi-Repo Support** (Issue #424)
    - `AddEncinaExternalService()` for services in other repos
    - Service discovery: Kubernetes, Consul, DNS
    - Shared message broker configuration
    - Contract-first approach
    - Priority: MEDIUM - Enterprise demand
  - **Hot Reload Support** (Issue #425)
    - Hot reload of handlers during development
    - Integration with `MetadataUpdateHandler`
    - State preservation (outbox, inbox, sagas)
    - Dashboard indication during reload
    - Priority: MEDIUM - Developer experience

- New labels created for Aspire integration:
  - `area-aspire` - Aspire hosting, orchestration, and deployment (#512BD4)
  - `area-mcp` - Model Context Protocol and AI agent integration (#8B5CF6)
  - `area-hot-reload` - Hot reload and live code updates (#F59E0B)

#### Cloud-Native Patterns Issues (11 new features planned based on December 29, 2025 research)

- **Encina.Aspire** (Issue #449) - .NET Aspire integration
    - `AddEncinaAspireDefaults()` extension method
    - Service Discovery integration for distributed handlers
    - OpenTelemetry pre-configured for Encina pipeline
    - Health checks for Outbox, Inbox, Sagas
    - Aspire Dashboard integration
    - New package planned: `Encina.Aspire`
    - Priority: HIGH - Official Microsoft cloud-native stack
  - **Encina.Dapr** (Issue #450) - Dapr Building Blocks integration
    - `DaprSagaStore`, `DaprOutboxStore`, `DaprInboxStore` via Dapr State API
    - `DaprOutboxPublisher` via Dapr Pub/Sub
    - `DaprDistributedLockProvider` via Dapr Lock API
    - Secrets injection via `[DaprSecret]` attribute
    - Cloud-agnostic: same code on AWS, Azure, GCP, on-prem
    - New package planned: `Encina.Dapr`
    - Priority: HIGH - CNCF graduated, multi-cloud demand
  - **Encina.FeatureFlags** (Issue #451) - Feature flags abstraction
    - `IFeatureFlagProvider` abstraction
    - `[Feature("key")]` attribute for handler injection
    - `FeatureFlagInjectionBehavior` pipeline behavior
    - Providers: ConfigCat, LaunchDarkly, Azure App Configuration, OpenFeature
    - New packages planned: `Encina.FeatureFlags`, `Encina.FeatureFlags.ConfigCat`, `Encina.FeatureFlags.LaunchDarkly`, `Encina.FeatureFlags.OpenFeature`
    - Priority: MEDIUM - Progressive deployment enabler
  - **Encina.Secrets** (Issue #452) - Secrets management abstraction
    - `ISecretsProvider` abstraction
    - `[Secret("key")]` attribute for DI injection
    - Secret rotation monitoring (optional)
    - Providers: Azure Key Vault, AWS Secrets Manager, HashiCorp Vault
    - New packages planned: `Encina.Secrets`, `Encina.Secrets.AzureKeyVault`, `Encina.Secrets.AwsSecretsManager`, `Encina.Secrets.HashiCorpVault`
    - Priority: MEDIUM - Security best practice
  - **Encina.ServiceDiscovery** (Issue #453) - Service discovery abstraction
    - `IServiceDiscoveryProvider` abstraction
    - Load balancing strategies: RoundRobin, Random, LeastConnections
    - `IEncina.SendToService<>()` extension methods
    - Providers: Kubernetes DNS, Consul, Aspire
    - New packages planned: `Encina.ServiceDiscovery`, `Encina.ServiceDiscovery.Kubernetes`, `Encina.ServiceDiscovery.Consul`
    - Priority: MEDIUM - Microservices fundamental
  - **Encina.HealthChecks** (Issue #454) - Kubernetes health probes
    - `OutboxHealthCheck`, `InboxHealthCheck`, `SagaHealthCheck`, `HandlerHealthCheck`
    - Separate endpoints: `/health/live`, `/health/ready`, `/health/startup`
    - Integration with `Microsoft.Extensions.Diagnostics.HealthChecks`
    - New package planned: `Encina.HealthChecks`
    - Priority: MEDIUM - Kubernetes deployment essential
  - **Encina.GracefulShutdown** (Issue #455) - Kubernetes graceful termination
    - `IInFlightRequestTracker` for active request tracking
    - `InFlightTrackingBehavior` pipeline behavior
    - Pre-stop delay for LB drain
    - Outbox flush before shutdown
    - New package planned: `Encina.GracefulShutdown`
    - Priority: MEDIUM - K8s reliability essential
  - **Encina.MultiTenancy** (Issue #456) - Multi-tenancy for SaaS
    - Tenant resolution: Header, Subdomain, Route, Claim, Custom
    - Data isolation: Row, Schema, Database strategies
    - `TenantAwareOutboxStore`, `TenantAwareSagaStore`, `TenantAwareInboxStore`
    - GDPR data residency support
    - New package planned: `Encina.MultiTenancy`
    - Priority: MEDIUM - SaaS market enabler
  - **Encina.CDC** (Issue #457) - Change Data Capture for Outbox
    - `ICdcProvider` abstraction for change streaming
    - CDC Orchestrator hosted service
    - Providers: SQL Server CDC, PostgreSQL Logical Replication, Debezium
    - Near real-time message capture, minimal database load
    - New packages planned: `Encina.CDC`, `Encina.CDC.SqlServer`, `Encina.CDC.PostgreSQL`
    - Priority: LOW - Alternative to polling, high complexity
  - **Encina.ApiVersioning** (Issue #458) - Handler versioning support
    - `[ApiVersion("1.0")]` attribute for handlers
    - Version resolution: Header, Query, Path, MediaType
    - Deprecation support with Sunset header
    - Version discovery endpoint
    - New package planned: `Encina.ApiVersioning`
    - Priority: LOW - API evolution support
  - **Encina.Orleans** (Issue #459) - Orleans virtual actors integration
    - `IGrainHandler<,>` interface
    - Orleans-based request dispatcher
    - Saga grains with Orleans state and reminders
    - Scheduling via grain timers
    - New package planned: `Encina.Orleans`
    - Priority: LOW - High-concurrency niche

#### Microservices Architecture Patterns Issues (12 new features planned based on December 29, 2025 research)

- **Service Discovery & Configuration Management** (Issue #382) - Foundational microservices pattern
    - `IServiceDiscovery` with `ResolveAsync`, `RegisterAsync`, `DeregisterAsync`, `WatchAsync`
    - `IConfigurationProvider` for externalized configuration
    - Multiple backends: Consul, Kubernetes DNS, .NET Aspire
    - New packages planned: `Encina.ServiceDiscovery`, `Encina.ServiceDiscovery.Consul`, `Encina.ServiceDiscovery.Kubernetes`, `Encina.ServiceDiscovery.Aspire`
    - Priority: CRITICAL - Fundamental pattern missing from Encina
  - **API Gateway / Backends for Frontends (BFF)** (Issue #383) - Essential modern architecture pattern
    - `IBffRequestAdapter` for proxying with Encina pipeline
    - `IResponseAggregator<T>` for combining multiple service responses
    - `[BffRoute]`, `[AggregateFrom]` declarative attributes
    - Microsoft YARP integration
    - New packages planned: `Encina.BFF`, `Encina.BFF.YARP`, `Encina.BFF.Aggregation`
    - Priority: CRITICAL - Very high demand in 2025
  - **Domain Events vs Integration Events Separation** (Issue #384) - DDD best practice
    - `IDomainEvent` for in-process bounded context events
    - `IIntegrationEvent` with `EventId`, `SourceService`, `Version`
    - `IIntegrationEventMapper<TDomain, TIntegration>` for automatic translation
    - `DomainToIntegrationEventBehavior` for Outbox auto-publishing
    - Priority: CRITICAL - Inspired by MassTransit, NServiceBus
  - **Multi-Tenancy Support** (Issue #385) - SaaS-critical pattern
    - `ITenantContext` with `CurrentTenant`, `IsolationLevel`
    - `TenantIsolationLevel`: SharedSchema, SeparateSchema, SeparateDatabase
    - `ITenantResolver` with Header, Claims, Subdomain, Route resolvers
    - `TenantFilteringPipelineBehavior` for automatic query filtering
    - EF Core integration with automatic tenant filtering
    - New packages planned: `Encina.MultiTenancy`, `Encina.MultiTenancy.EntityFrameworkCore`
    - Priority: HIGH - Essential for SaaS applications
  - **Anti-Corruption Layer (ACL)** (Issue #386) - Legacy integration pattern
    - `IAntiCorruptionLayer<TExternal, TDomain>` bidirectional translation
    - `[AntiCorruptionLayer]` attribute for HTTP clients (Refit integration)
    - `AntiCorruptionPipelineBehavior` for automatic translation
    - New package planned: `Encina.AntiCorruption`
    - Priority: HIGH - Essential for brownfield development
  - **Dapr Integration** (Issue #387) - CNCF graduated project integration
    - Dapr State Store as Outbox, Inbox, Saga store backend
    - Dapr Pub/Sub as message transport
    - Dapr Workflows as Saga backend alternative
    - Dapr Actors integration
    - New packages planned: `Encina.Dapr`, `Encina.Dapr.StateStore`, `Encina.Dapr.PubSub`
    - **Re-planned**: Previously deprecated, now restored due to CNCF graduation and high community demand
    - Priority: HIGH - Leading microservices framework 2025
  - **Virtual Actors (Orleans Integration)** (Issue #388) - High concurrency pattern
    - `IEncinaActor` and `IEncinaActor<TState>` abstractions
    - `EncinaGrain<TState>` base class for Orleans Grains
    - Full Encina pipeline support within actors
    - New packages planned: `Encina.Actors`, `Encina.Orleans`
    - Priority: MEDIUM - Gaming, IoT, high-concurrency use cases
  - **API Versioning Pipeline Behavior** (Issue #389) - API evolution support
    - `[ApiVersion]` attribute with deprecation support
    - `IApiVersionContext` with `CurrentVersion`, `IsDeprecated`
    - `IApiVersionResolver` with Header, QueryString, URL, MediaType resolvers
    - `ApiVersioningPipelineBehavior` for version-aware handler routing
    - Priority: MEDIUM - Important for production API evolution
  - **Enhanced Message Deduplication** (Issue #390) - Inbox pattern improvement
    - `IDeduplicationKeyGenerator<T>` for content-based keys
    - `SlidingWindowDeduplicationOptions` with Window, Strategy, Cleanup
    - `DeduplicationStrategy`: RejectSilently, ReturnCachedResult, ReturnError
    - `[Deduplicate]` declarative attribute
    - Priority: MEDIUM - Improves existing Inbox functionality
  - **Sidecar/Ambassador Pattern Support** (Issue #391) - Kubernetes-native pattern
    - `ISidecarProxy` for Encina as sidecar process
    - `IAmbassadorProxy` for client connectivity offloading
    - `EncinaSidecarHost` BackgroundService
    - Kubernetes deployment examples, Docker images
    - New package planned: `Encina.Sidecar`
    - Priority: MEDIUM - Important for containerized deployments
  - **Event Collaboration / Process Manager** (Issue #392) - Hybrid orchestration pattern
    - `IProcessManager<TState>` with `HandleEventAsync`, `GetAuditTrailAsync`
    - `ProcessManagerBase<TState>` base class
    - `[CorrelateBy]` attribute for event routing
    - `ProcessManagerRoutingBehavior` for automatic event dispatch
    - Dashboard/visibility queries
    - Priority: MEDIUM - Hybrid choreography/orchestration with visibility
  - **Eventual Consistency Helpers** (Issue #393) - Distributed systems helpers
    - `IEventualConsistencyMonitor` with `CheckAsync`, `WaitForConsistencyAsync`
    - `IConflictResolver<TState>` with LastWriteWins, Merge, ManualResolution strategies
    - `[EventuallyConsistent]` attribute with `MaxLagMs`, `WaitForConsistency`
    - `IReadYourWritesGuarantee` for session-level consistency
    - Priority: LOW - Nice-to-have for complex systems

- New labels created for Microservices patterns:
  - `area-service-discovery` - Service discovery and registry patterns
  - `area-configuration` - Configuration management and externalization
  - `orleans-integration` - Microsoft Orleans integration
  - `dapr-integration` - Dapr runtime integration
  - `yarp-integration` - Microsoft YARP reverse proxy integration
  - `consul-integration` - HashiCorp Consul integration
  - `aspire-integration` - .NET Aspire integration
  - `kubernetes-native` - Kubernetes-native patterns and deployment
  - `pattern-sidecar` - Sidecar and Ambassador patterns
  - `pattern-bff` - Backend for Frontend pattern

#### Security Patterns Issues (8 new features planned based on December 29, 2025 research)

- **Core Security Abstractions** (Issue #394) - Foundational security pattern
    - `ISecurityContext` with CurrentPrincipal, Permissions, Roles, Claims
    - `IPermissionEvaluator<TResource>` for dynamic permission evaluation
    - `SecurityPipelineBehavior` with `[Authorize]`, `[RequirePermission]`, `[RequireRole]` attributes
    - RBAC, ABAC, Permission-based authorization support
    - New package planned: `Encina.Security`
    - Priority: CRITICAL - Foundation for all security patterns
  - **Audit Trail Logging** (Issue #395) - Compliance-ready audit logging
    - `IAuditLogger` with who/what/when/where tracking
    - `AuditPipelineBehavior` for automatic capture
    - `[Auditable]` attribute with None, Minimal, Standard, Detailed levels
    - Storage backends: Database, Elasticsearch, Azure Table Storage, CloudWatch
    - Sensitive data redaction
    - New package planned: `Encina.Security.Audit`
    - Priority: CRITICAL - Required for SOX, HIPAA, GDPR, PCI compliance
  - **Field-Level Encryption** (Issue #396) - Data protection at rest
    - `IFieldEncryptor` with encrypt/decrypt/rotate key operations
    - `[Encrypt]` attribute for sensitive properties
    - `EncryptionPipelineBehavior` for automatic encrypt/decrypt
    - Key rotation with versioning, Azure Key Vault/AWS KMS integration
    - Crypto-shredding for GDPR (delete key = "forget" data)
    - New package planned: `Encina.Security.Encryption`
    - Priority: HIGH - PCI-DSS, GDPR sensitive data protection
  - **PII Masking** (Issue #397) - Personal data protection
    - `IPIIMasker` with mask/unmask/detect operations
    - `[PII]` attribute with Email, Phone, SSN, CreditCard, Address types
    - `PIIMaskingPipelineBehavior` for automatic response masking
    - Auto-detection, logging redaction
    - New package planned: `Encina.Security.PII`
    - Priority: HIGH - GDPR essential
  - **Anti-Tampering** (Issue #398) - Request integrity verification
    - `IRequestSigner` with HMAC-SHA256/512, RSA-SHA256, ECDSA
    - `[SignedRequest]` attribute for handlers requiring verification
    - `SignatureVerificationPipelineBehavior`
    - Timestamp validation, nonce management for replay attack prevention
    - New package planned: `Encina.Security.AntiTampering`
    - Priority: HIGH - API security for webhooks, inter-service communication
  - **Input Sanitization** (Issue #399) - OWASP Top 10 prevention
    - `ISanitizer<T>` with sanitize/validate operations
    - `[Sanitize]` attribute with Html, Sql, Command, Path, Url types
    - `SanitizationPipelineBehavior` for automatic input cleaning
    - XSS, SQL injection, command injection, path traversal prevention
    - New package planned: `Encina.Security.Sanitization`
    - Priority: HIGH - OWASP Top 10 prevention
  - **Secrets Management** (Issue #400) - Cloud-native secrets handling
    - `ISecretProvider` with get/set/rotate operations
    - `SecretProviderChain` for fallback between providers
    - Azure Key Vault, AWS Secrets Manager, HashiCorp Vault, GCP Secret Manager integration
    - Automatic rotation, TTL caching, access auditing
    - New packages planned: `Encina.Security.Secrets.*`
    - Priority: MEDIUM - Cloud-native best practice
  - **ABAC Engine** (Issue #401) - Advanced authorization
    - `IAbacEngine` with evaluate(subject, resource, action, environment)
    - Policy DSL for access policies
    - `AbacPipelineBehavior` for automatic evaluation
    - PDP/PEP pattern, OPA integration
    - New package planned: `Encina.Security.ABAC`
    - Priority: MEDIUM - Complex enterprise authorization

- New labels created for Security patterns:
  - `area-security` - Security patterns and authentication/authorization
  - `owasp-pattern` - Based on OWASP security best practices

- Compliance Patterns Issues - GDPR & EU Laws (14 new features planned based on December 29, 2025 research):
  - **GDPR Core Abstractions** (Issue #402) - Foundation for EU compliance
    - `IDataController`, `IDataProcessor` interfaces
    - `RoPARegistry` (Record of Processing Activities) - Art. 30
    - `GDPRCompliancePipelineBehavior` for automatic validation
    - New package planned: `Encina.Compliance.GDPR`
    - Priority: CRITICAL - Mandatory for EU operations
  - **Consent Management** (Issue #403) - Art. 7 compliance
    - `IConsentManager` with request/grant/withdraw/check operations
    - `[RequireConsent("purpose")]` attribute
    - Consent versioning, proof of consent, granular purposes
    - New package planned: `Encina.Compliance.Consent`
    - Priority: CRITICAL - Art. 7 requirement
  - **Data Subject Rights** (Issue #404) - Arts. 15-22 implementation
    - `IDataSubjectRightsService` for all GDPR rights
    - Right of Access (Art. 15), Rectification (Art. 16), Erasure (Art. 17)
    - Right to Portability (Art. 20), Restriction (Art. 18), Object (Art. 21)
    - Request tracking, 30-day SLA monitoring
    - New package planned: `Encina.Compliance.DataSubjectRights`
    - Priority: CRITICAL - Fundamental GDPR rights
  - **Data Residency** (Issue #405) - Data sovereignty enforcement
    - `IDataResidencyEnforcer` with geo-routing
    - `[DataResidency("EU")]` attribute
    - Multi-region database routing, SCCs validation
    - Cloud provider region mapping (AWS, Azure, GCP)
    - New package planned: `Encina.Compliance.DataResidency`
    - Priority: CRITICAL - Post-Schrems II requirement
  - **Data Retention** (Issue #406) - Storage limitation (Art. 5(1)(e))
    - `IRetentionPolicyEngine` with automatic deletion
    - Legal hold support, retention reporting
    - New package planned: `Encina.Compliance.Retention`
    - Priority: HIGH
  - **Anonymization** (Issue #407) - Art. 4(5) pseudonymization
    - `IAnonymizer` with k-anonymity, l-diversity, differential privacy
    - Crypto-shredding: delete key = data "forgotten"
    - New package planned: `Encina.Compliance.Anonymization`
    - Priority: HIGH
  - **Breach Notification** (Issue #408) - 72-hour notification (Arts. 33-34)
    - `IBreachNotificationService` with detection/assessment/notification
    - SIEM integration (Splunk, Azure Sentinel)
    - New package planned: `Encina.Compliance.BreachNotification`
    - Priority: HIGH
  - **DPIA Automation** (Issue #409) - Art. 35 impact assessment
    - `IDPIAService` with risk assessment and report generation
    - New package planned: `Encina.Compliance.DPIA`
    - Priority: MEDIUM
  - **Processor Agreements** (Issue #410) - Art. 28 compliance
    - `IProcessorAgreementService` for DPA management
    - New package planned: `Encina.Compliance.ProcessorAgreements`
    - Priority: MEDIUM
  - **Privacy by Design** (Issue #411) - Art. 25 enforcement
    - `IPrivacyByDesignValidator`, Roslyn analyzer
    - New package planned: `Encina.Compliance.PrivacyByDesign`
    - Priority: MEDIUM
  - **Cross-Border Transfer** (Issue #412) - Chapter V compliance
    - `ICrossBorderTransferValidator` with SCCs, BCRs, TIA
    - New package planned: `Encina.Compliance.CrossBorderTransfer`
    - Priority: MEDIUM
  - **Lawful Basis** (Issue #413) - Art. 6 tracking
    - `ILawfulBasisService` for processing validation
    - New package planned: `Encina.Compliance.LawfulBasis`
    - Priority: MEDIUM
  - **NIS2 Directive** (Issue #414) - EU 2022/2555 cybersecurity
    - `INIS2ComplianceService` for incident reporting
    - New package planned: `Encina.Compliance.NIS2`
    - Priority: MEDIUM
  - **EU AI Act** (Issue #415) - EU 2024/1689 AI governance
    - `IAIActComplianceService` for risk classification
    - New package planned: `Encina.Compliance.AIAct`
    - Priority: MEDIUM

- New labels created for Compliance patterns:
  - `area-compliance` - Regulatory compliance patterns (GDPR, NIS2, AI Act)
  - `eu-regulation` - Related to European Union regulations
  - `pattern-data-masking` - Data masking and PII protection
  - `pattern-consent-management` - Consent management and tracking
  - `pattern-data-sovereignty` - Data residency and sovereignty
  - `area-data-protection` - Data protection and privacy features

#### Advanced Validation Patterns Issues (10 new features planned based on December 2025 research)

- **Source-Generated Validation** (Issue #227) - Compile-time validation code generation
    - Zero reflection, NativeAOT and trimming compatible
    - ~1.6x faster, ~4.7x less memory (Validot benchmarks)
    - Attributes: `[GenerateValidation]`, `[NotEmpty]`, `[Email]`, `[Positive]`, etc.
    - New package planned: `Encina.Validation.Generators`
    - Inspired by Validot, Microsoft Options Validation Source Generator
  - **Domain/Value Object Validation** (Issue #228) - Always-Valid Domain Model
    - Value Objects with built-in validation
    - Factory methods returning `Either<EncinaError, T>` for ROP
    - Base classes: `ValueObject<TSelf>`, `SingleValueObject<TSelf, TValue>`
    - Common value objects: `Email`, `PhoneNumber`, `Url`, `Money`, `NonEmptyString`
    - New package planned: `Encina.Validation.Domain`
    - Inspired by Enterprise Craftsmanship, Milan Jovanovic's DDD patterns
  - **Consolidate ValidationPipelineBehavior** (Issue #229) - Remove duplicate behaviors
    - CRITICAL technical debt: Each provider has its own duplicated behavior
    - Affected: FluentValidation, DataAnnotations, MiniValidator packages
    - Solution: Use centralized `ValidationPipelineBehavior` from core
    - Low effort, high impact cleanup
  - **Enhanced Async/Cross-Field Validation** (Issue #230) - Database-backed validation
    - Extensions: `MustExistAsync()`, `MustBeUniqueAsync()`, `GreaterThan(x => x.OtherProperty)`
    - Cross-field comparison validators
    - Conditional validation with `WhenAsync()`, `UnlessAsync()`
  - **OpenAPI Schema Validation** (Issue #231) - Contract-first validation
    - Automatic validation against OpenAPI 3.1 schemas
    - Request/Response validation
    - Prevents API drift between contract and implementation
    - New package planned: `Encina.Validation.OpenApi`
    - Inspired by Zuplo, openVALIDATION
  - **Security-Focused Validation** (Issue #232) - OWASP-compliant validation
    - Prevents >90% of injection attacks (OWASP statistics)
    - Allowlist validators: `AllowlistPattern()`, `AllowlistValues()`
    - Injection prevention: `NoSqlInjection()`, `NoXss()`, `NoCommandInjection()`
    - Sanitizers: `SanitizeHtml()`, `StripHtml()`, `EncodeHtml()`
    - New package planned: `Encina.Validation.Security`
    - Inspired by OWASP Input Validation Cheat Sheet, ASVS
  - **Validation Error Localization** (Issue #233) - Internationalization support
    - Integration with ASP.NET Core `IStringLocalizer`
    - Built-in translations for 12+ languages
    - Placeholder support: `{PropertyName}`, `{PropertyValue}`
  - **Validation Result Aggregation** (Issue #234) - Multi-source validation
    - `ValidationAggregator` builder for combining validators
    - Strategies: FailFast, CollectAll, ParallelCollectAll, ParallelFailFast
    - Error source tracking and deduplication
  - **Zod-like Schema Builder** (Issue #235) - TypeScript-inspired fluent API
    - Chainable schema definitions
    - Parse returns `Either<EncinaError, T>`
    - New package planned: `Encina.Validation.Schema`
    - Inspired by Zod (TypeScript), zod-rs (Rust)
  - **Two-Phase Validation Pattern** (Issue #236) - Pipeline + Domain separation
    - Phase 1 (Pipeline): Fast structural validation
    - Phase 2 (Handler): Domain validation with repository access
    - Interfaces: `IDomainValidator<TRequest>`, `IDomainValidatedRequest`
    - Best practice for clean CQRS architecture

- New label created: `area-source-generators` for source generator-related features

#### Advanced Event Sourcing Patterns Issues (13 new features planned based on December 2025 research)

- **Decider Pattern Support** (Issue #320) - Functional event sourcing with pure functions
    - `IDecider<TCommand, TEvent, TState>` interface with `Decide`, `Evolve`, `InitialState`
    - Pure functions = trivial testing without mocks
    - Industry best practice 2025 (Marten, Wolverine recommended)
    - Aligns with Encina's Railway Oriented Programming philosophy
  - **Causation/Correlation ID Tracking** (Issue #321) - Distributed tracing metadata
    - `EventMetadata` with MessageId, CausationId, CorrelationId
    - Automatic propagation from `IRequestContext`
    - 30-40% reduction in troubleshooting time (empirical studies)
    - Integration with `Encina.OpenTelemetry`
  - **Crypto-Shredding for GDPR** (Issue #322) - GDPR Article 17 compliance
    - New package planned: `Encina.Marten.GDPR`
    - `[PersonalData]` attribute for PII properties
    - `ICryptoShredder` with key vault integrations (HashiCorp, Azure, AWS)
    - Delete encryption key = data becomes unreadable ("forgotten")
    - **No .NET library offers first-class GDPR support** - competitive differentiator
  - **Advanced Snapshot Strategies** (Issue #323) - Beyond event-count based
    - `SnapshotStrategy` enum: EventCount, TimeInterval, BusinessBoundary, Composite
    - `ISnapshotBoundaryDetector` for custom boundaries
    - Per-aggregate configuration
  - **Blue-Green Projection Rebuild** (Issue #324) - Zero-downtime updates
    - `IProjectionRebuildManager` with progress tracking
    - Build projection in secondary schema, switch when caught up
    - CLI command: `encina projections rebuild <name>`
  - **Temporal Queries** (Issue #325) - Point-in-time state reconstruction
    - `LoadAtAsync(id, timestamp)` for historical state
    - `ITemporalEventStore` for range queries
    - 30% improvement in debugging time
  - **Multi-Tenancy Event Sourcing** (Issue #326) - SaaS tenant isolation
    - `MultiTenancyMode`: Conjoined, Dedicated, SchemaPerTenant
    - Automatic tenant filtering in repositories
    - `ITenantManager` for provisioning
  - **Event Archival and Compaction** (Issue #327) - Storage management at scale
    - Hot/warm/cold tiering with cloud storage (Azure Blob, S3)
    - Stream compaction strategies
    - Background archival service
  - **Bi-Temporal Modeling** (Issue #328) - Transaction + Valid time
    - `IBiTemporalEvent` with ValidTime and TransactionTime
    - Timeline visualization for audit
    - Use cases: Financial, insurance, HR
  - **Visual Event Stream Explorer** (Issue #329) - CLI debugging tool
    - `encina events list/show/replay/trace` commands
    - Projection status and lag monitoring
    - Rich terminal output with Spectre.Console
  - **Actor-Based Event Sourcing** (Issue #330) - Alternative pattern
    - `IEventSourcedActor<TState, TCommand, TEvent>` interface
    - Orleans/Akka-inspired concurrency model
    - Automatic lifecycle management
  - **EventQL Preconditions** (Issue #331) - Query-based constraints
    - `IAppendPrecondition` interface
    - Built-in: `StreamExists`, `NoEventOfType<T>`, `ExpectedVersion`
    - Composite with `Preconditions.All()`/`Any()`
  - **Tri-Temporal Modeling** (Issue #332) - Full audit trail
    - Transaction + Valid + Decision time
    - Use cases: Fraud detection, legal discovery

- New labels created for Event Sourcing patterns:
  - `area-event-sourcing` - Event Sourcing patterns and infrastructure
  - `area-gdpr` - GDPR compliance and data privacy
  - `pattern-decider` - Decider pattern for functional event sourcing
  - `pattern-crypto-shredding` - Crypto-shredding pattern for GDPR compliance
  - `pattern-blue-green` - Blue-Green deployment/rebuild pattern
  - `pattern-temporal-query` - Temporal queries and time-travel pattern
  - `pattern-snapshot` - Aggregate snapshotting pattern
  - `area-archival` - Event archival and cold storage patterns
  - `area-developer-experience` - Developer experience and tooling improvements
  - `marten-integration` - Marten library integration

#### Advanced CQRS Patterns Issues (12 new features planned based on December 2025 market research)

- **Zero-Interface Handlers** (Issue #333) - Convention-based handler discovery
    - Handlers discovered by naming convention, no `IRequestHandler<,>` required
    - Static handlers supported (`public static class CreateOrderHandler`)
    - Reduces boilerplate and improves DDD alignment
    - Inspired by Wolverine's convention-based approach
    - Depends on #50 (Source Generators) for NativeAOT compatibility
  - **Idempotency Pipeline Behavior** (Issue #334) - Lightweight deduplication
    - `IIdempotencyStore` interface for key tracking
    - `IdempotencyPipelineBehavior<,>` for automatic verification
    - Cache-based storage (Redis, In-memory) for lightweight dedup
    - Complements existing Inbox pattern for simpler use cases (APIs, webhooks)
    - Inspired by MassTransit and Stripe patterns
  - **Request Timeout Behavior** (Issue #335) - Per-request timeouts
    - `[Timeout(Seconds = 30)]` attribute for declarative configuration
    - `IHasTimeout` interface for programmatic configuration
    - Fallback strategies: ThrowException, ReturnDefault, ReturnCached
    - OpenTelemetry integration for timeout events
    - Inspired by Brighter's timeout middleware
  - **Cursor-Based Pagination** (Issue #336) - O(1) pagination helpers
    - `ICursorPaginatedQuery<T>` interface
    - `CursorPaginatedResult<T>` with NextCursor, HasNextPage
    - `ToCursorPaginatedAsync()` EF Core extension
    - O(1) performance vs O(n) for offset pagination
    - Inspired by GraphQL Cursor Connections specification
  - **Request Versioning** (Issue #337) - Command/query upcasting
    - `[RequestVersion(1)]` attribute for versioning
    - `IRequestUpcaster<TFrom, TTo>` for automatic migration
    - Version chains: V1 → V2 → V3 automatic upcasting
    - Deprecation logging and metrics
    - Inspired by Axon Framework's event upcasting
  - **Multi-Tenant Context Middleware** (Issue #338) - Tenant isolation
    - `ITenantResolver` with built-in implementations (Header, Subdomain, Claims, Route)
    - `TenantValidationBehavior` for tenant validation
    - `TenantIsolationBehavior` for isolation enforcement
    - EF Core global query filter integration
    - Labels: `area-multitenancy`, `area-security`, `industry-best-practice`
  - **Batch Command Processing** (Issue #339) - Atomic batch operations
    - `IBatchCommand<TCommand, TResponse>` interface
    - Strategies: AllOrNothing, PartialSuccess, StopOnFirstError, ContinueOnError
    - Batch deduplication and parallel processing options
    - Inspired by MassTransit batch consumers
  - **Request Enrichment** (Issue #340) - Auto-populate from context
    - `[EnrichFrom(ContextProperty.UserId)]` for context properties
    - `[EnrichFromClaim("claim_name")]` for JWT claims
    - `IRequestEnricher<T>`, `IResponseEnricher<T>` interfaces
    - Reduces handler boilerplate
    - Inspired by Wolverine middleware
  - **Notification Fanout Strategies** (Issue #341) - Advanced delivery
    - New strategies: PriorityOrdered, Throttled, Quorum, FirstSuccessful
    - `[NotificationPriority]` attribute for ordering
    - Dead letter handling for failed notifications
    - Circuit breaker integration
  - **Request Composition** (Issue #342) - Combine multiple queries
    - `QueryComposer<TResult>` fluent builder
    - Parallel execution of independent queries
    - Dependency resolution for dependent queries
    - Reduces API chattiness
    - Inspired by GraphQL query composition
  - **Handler Discovery Analyzers** (Issue #343) - Compile-time validation
    - ENCINA001: Handler not registered
    - ENCINA002: Handler naming convention mismatch
    - ENCINA003: Query modifies state (anti-pattern)
    - ENCINA004: Response type mismatch
    - ENCINA005: Missing validator
    - Automatic code fixes
    - New package: `Encina.Analyzers`
  - **Progressive CQRS Adoption Guide** (Issue #344) - Documentation
    - Decision tree: when to use/not use CQRS
    - Adoption levels (0-4)
    - Anti-patterns to avoid
    - Vertical Slice Architecture integration

- New labels created for Advanced CQRS patterns:
  - `wolverine-inspired` - Pattern inspired by Wolverine library
  - `brighter-inspired` - Pattern inspired by Brighter library
  - `axon-inspired` - Pattern inspired by Axon Framework (Java)
  - `graphql-inspired` - Pattern inspired by GraphQL ecosystem
  - `pattern-batch-processing` - Batch processing and bulk command pattern
  - `pattern-request-composition` - Request composition and aggregation pattern

#### Domain Modeling Building Blocks Issues (15 new features planned based on December 29, 2025 DDD research)

- **Value Objects Base Class** (Issue #367) - Structural equality and immutability
    - `ValueObject<T>` abstract record with `GetEqualityComponents()`
    - ROP-compatible factory methods returning `Either<EncinaError, T>`
    - Prevents "primitive obsession" anti-pattern
    - Inspired by Vogen (~5M downloads demand)
    - New package planned: `Encina.DomainModeling`
    - Labels: `foundational`, `area-domain-modeling`, `aot-compatible`, `industry-best-practice`
  - **Rich Domain Events** (Issue #368) - Domain events with metadata
    - `DomainEvent` base record implementing `INotification`
    - Properties: EventId, OccurredAtUtc, EventVersion, CorrelationId, CausationId, AggregateId, AggregateVersion
    - Integrates with existing Encina pipeline and Marten event sourcing
    - Labels: `foundational`, `area-domain-modeling`, `area-messaging`, `area-observability`
  - **Entity Base Class** (Issue #369) - Identity equality for non-aggregates
    - `Entity<TId>` with identity-based equality
    - Separate from `AggregateRoot` for entities within aggregates
    - Labels: `foundational`, `area-domain-modeling`, `aot-compatible`
  - **Provider-Agnostic AggregateRoot** (Issue #370) - State-based persistence support
    - `AggregateRoot<TId>` extending `Entity<TId>` with domain event collection
    - `RaiseDomainEvent()`, `ClearDomainEvents()`, `CheckRule()` methods
    - Works with EF Core, Dapper (not just Marten event sourcing)
    - Labels: `foundational`, `area-domain-modeling`, `area-messaging`
  - **Specification Pattern** (Issue #371) - Composable query specifications
    - `Specification<T>` abstract class with `ToExpression()`
    - `And()`, `Or()`, `Not()` composition operators
    - New packages: `Encina.Specifications`, `Encina.Specifications.EntityFrameworkCore`
    - Inspired by Ardalis.Specification (~7M downloads)
    - Labels: `new-package`, `area-specifications`, `industry-best-practice`
  - **Business Rules Validation** (Issue #372) - Domain invariant validation
    - `IBusinessRule` interface with `ErrorCode`, `ErrorMessage`, `IsSatisfied()`
    - ROP extension: `Check()` returning `Either<EncinaError, Unit>`
    - Separate from input validation (FluentValidation, DataAnnotations)
    - Labels: `foundational`, `area-domain-modeling`, `area-validation`
  - **Integration Events** (Issue #373) - Cross-bounded-context events
    - `IntegrationEvent` base record with schema versioning
    - `IDomainToIntegrationEventMapper<TDomain, TIntegration>` interface
    - Bounded context isolation pattern
    - Labels: `area-domain-modeling`, `area-modular-monolith`, `area-microservices`
  - **Strongly Typed IDs** (Issue #374) - Type-safe entity identifiers
    - `StronglyTypedId<TValue>` base record
    - Convenience classes: `GuidId`, `IntId`, `LongId`, `StringId`
    - Prevents mixing `OrderId` with `CustomerId` at compile-time
    - Inspired by StronglyTypedId (~3M downloads)
    - Labels: `foundational`, `area-domain-modeling`, `aot-compatible`
  - **Soft Delete Pattern** (Issue #375) - Logical deletion with auto-filtering
    - `ISoftDeletable` interface with `IsDeleted`, `DeletedAtUtc`, `DeletedBy`
    - EF Core global query filter extension
    - Pipeline behavior for query filtering
    - Labels: `area-auditing`, `area-domain-modeling`, `area-gdpr`, `area-compliance`
  - **Auditing Pattern** (Issue #376) - Created/Modified tracking
    - `IAudited` interface with CreatedAtUtc/By, LastModifiedAtUtc/By
    - EF Core SaveChanges interceptor for auto-population
    - Uses `IRequestContext.UserId`
    - Labels: `area-auditing`, `area-domain-modeling`, `area-compliance`, `area-security`
  - **Domain Service Marker** (Issue #377) - Semantic interface for domain services
    - `IDomainService` marker interface
    - Auto-registration extension method
    - Labels: `area-domain-modeling`, `aot-compatible`
  - **Anti-Corruption Layer** (Issue #378) - External system isolation
    - `IAntiCorruptionTranslator<TExternal, TDomain>` interface
    - `IBidirectionalTranslator<,>` for two-way translation
    - Labels: `area-domain-modeling`, `area-integration`, `area-microservices`
  - **Bounded Context Helpers** (Issue #379) - Context mapping
    - `BoundedContext` base class with `Configure()`
    - `ContextMap` for documenting context relationships
    - Integration with existing `IModule` system
    - Labels: `area-domain-modeling`, `area-modular-monolith`, `area-architecture-testing`
  - **Generic Repository** (Issue #380) - Provider-agnostic repository abstraction
    - `IRepository<TAggregate, TId>` with CRUD operations
    - `IRepositoryWithSpecification<,>` for specification queries
    - Note: Controversial pattern - many prefer DbContext directly
    - Labels: `area-domain-modeling`, `area-specifications`
  - **Domain DSL Helpers** (Issue #381) - Fluent domain builders
    - `AggregateBuilder<TAggregate, TId, TBuilder>` with rule validation
    - Fluent extensions for ubiquitous language
    - Common domain types: `Quantity`, `Percentage`, `DateRange`
    - Labels: `area-domain-modeling`, `aot-compatible`

- New labels created for Domain Modeling patterns:
  - `area-domain-modeling` - Domain modeling building blocks (Entities, Value Objects, Aggregates)
  - `area-specifications` - Specification pattern for composable queries
  - `area-auditing` - Auditing, change tracking, and soft delete patterns (already existed)
  - `foundational` - Core building block that other features depend on

#### Vertical Slice Architecture Patterns Issues (12 new features planned based on December 29, 2025 research)

- **Feature Flags Integration** (Issue #345) - Microsoft.FeatureManagement integration
    - `[FeatureFlag("NewCheckoutFlow")]` attribute for handlers
    - `FeatureFlagPipelineBehavior<,>` for automatic verification
    - Built-in filters: Percentage, TimeWindow, Targeting, Contextual
    - New package planned: `Encina.FeatureFlags`
    - Labels: `area-feature-flags`, `saas-essential`, `industry-best-practice`, `aot-compatible`
  - **Multi-Tenancy Support** (Issue #346) - Comprehensive SaaS multi-tenant patterns
    - `ITenantResolver` with implementations (Header, Subdomain, Claim, Route, QueryString)
    - `TenantResolutionPipelineBehavior` and `TenantIsolationPipelineBehavior`
    - Database strategies: DatabasePerTenant, SchemaPerTenant, SharedDatabase
    - EF Core query filter integration
    - New packages planned: `Encina.MultiTenancy`, `Encina.MultiTenancy.EntityFrameworkCore`
    - Labels: `area-multi-tenancy`, `saas-essential`, `area-security`
  - **Specification Pattern Integration** (Issue #347) - Composable query specifications
    - `Specification<T>` base class with `And()`, `Or()`, `Not()` composition
    - `QuerySpecification<T>` with includes, ordering, paging
    - `ISpecificationEvaluator<T>` for EF Core and Dapper
    - New packages planned: `Encina.Specification`, `Encina.Specification.EntityFrameworkCore`
    - Inspired by Ardalis.Specification
  - **API Versioning Integration** (Issue #348) - Handler-level API versioning
    - `[ApiVersion("1.0")]` attribute for versioned handlers
    - `VersionedRequestDispatcher` for automatic routing
    - Deprecation headers: Deprecation, Sunset, Link
    - New package planned: `Encina.AspNetCore.Versioning`
  - **Request Batching / Bulk Operations** (Issue #349) - Batch processing for DDD
    - `BatchCommand<TCommand, TResponse>` and `BatchQuery<,>` wrappers
    - Strategies: AllOrNothing, PartialSuccess, StopOnFirstError, ParallelAll
    - Fluent `BatchBuilder<,>` API
    - New package planned: `Encina.Batching`
  - **Domain Events vs Integration Events** (Issue #350) - Clear separation (DDD best practice)
    - `IDomainEvent` (in-process) vs `IIntegrationEvent` (cross-boundary)
    - `IDomainEventDispatcher` with configurable timing (BeforeCommit, AfterCommit)
    - `IIntegrationEventPublisher` using Outbox pattern
    - `AggregateRoot<TId>` base class with domain event collection
    - EF Core `DomainEventDispatchingInterceptor`
  - **Audit Trail Pipeline Behavior** (Issue #351) - Compliance and auditing
    - `[Auditable]`, `[NotAuditable]` attributes
    - `IAuditStore` with EF Core, Dapper, Elasticsearch implementations
    - Sensitive data redaction with `ISensitiveDataRedactor`
    - New packages planned: `Encina.Auditing`, `Encina.Auditing.EntityFrameworkCore`
    - Labels: `area-auditing`, `area-gdpr`, `area-compliance`
  - **Modular Monolith Support** (Issue #352) - Architecture pattern 2025
    - `EncinaModule` base class with lifecycle hooks
    - `IModuleEventBus` for inter-module communication
    - `ModuleIsolationPipelineBehavior` for boundary enforcement
    - Database isolation strategies: SharedDatabase, SchemaPerModule
    - New package planned: `Encina.Modules`
    - Inspired by Milan Jovanovic, kgrzybek/modular-monolith-with-ddd
  - **CDC Integration** (Issue #353) - Change Data Capture patterns
    - `IChangeDataHandler<T>` for entity change handling
    - Providers: SQL Server CDC, PostgreSQL Logical Replication, Debezium/Kafka
    - New packages planned: `Encina.CDC`, `Encina.CDC.SqlServer`, `Encina.CDC.Debezium`
  - **Enhanced Streaming Support** (Issue #354) - IAsyncEnumerable improvements
    - `StreamCachingPipelineBehavior<,>` for caching streams
    - `StreamRateLimitingPipelineBehavior<,>` for per-item rate limiting
    - Backpressure strategies: Block, Drop, DropOldest, Error
    - Extension methods: `ToListAsync()`, `FirstOrDefaultAsync()`
  - **Enhanced Idempotency** (Issue #355) - Stripe-style idempotency keys
    - `[Idempotent]` attribute for handlers
    - `IIdempotencyStore` with TTL and distributed locking
    - ASP.NET Core middleware for X-Idempotency-Key header
    - Response caching with Idempotent-Replayed header
  - **Policy-Based Authorization Enhancement** (Issue #356) - Resource-based auth
    - `[AuthorizeRoles]`, `[AuthorizeClaim]` shortcuts
    - `[ResourceAuthorize(typeof(Order), "Edit")]` for resource-based auth
    - CQRS-aware default policies (Commands vs Queries)
    - Pre/Post authorization processors

- New labels created for Vertical Slice Architecture patterns:
  - `area-feature-flags` - Feature flags and feature toggles patterns
  - `area-authorization` - Authorization and access control patterns
  - `area-streaming` - Streaming and IAsyncEnumerable patterns
  - `area-batching` - Request batching and bulk operations
  - `area-specification` - Specification pattern for queries
  - `area-versioning` - API versioning patterns
  - `area-auditing` - Audit trail and logging patterns
  - `area-domain-events` - Domain events and integration events
  - `saas-essential` - Essential pattern for SaaS applications

#### Modular Monolith Architecture Patterns Issues (10 new features planned based on December 29, 2025 research)

- **Multi-Tenancy Support** (Issue #357) - Comprehensive SaaS multi-tenant patterns
    - `ITenantContext` with CurrentTenantId, CurrentTenantName, IsHost
    - `ITenantResolver` implementations: Header, Subdomain, QueryString, Route, Claim, Cookie
    - `DataIsolationLevel` enum: RowLevel, Schema, Database
    - `TenantContextPipelineBehavior` for automatic tenant propagation
    - EF Core integration with automatic `TenantQueryFilter`
    - New packages planned: `Encina.MultiTenancy`, `Encina.MultiTenancy.EntityFrameworkCore`, `Encina.MultiTenancy.AspNetCore`
    - Labels: `area-multitenancy`, `area-data-isolation`, `saas-enabler`, `cloud-azure`, `cloud-aws`
    - Inspired by ABP Framework, Milan Jovanović
  - **Inter-Module Communication** (Issue #358) - Integration Events pattern
    - `IDomainEvent : INotification` for in-process, synchronous events
    - `IIntegrationEvent : INotification` with EventId, OccurredAtUtc, SourceModule
    - `IIntegrationEventBus` for in-memory inter-module communication
    - `IModulePublicApi<TModule>` for module public contracts
    - Optional Outbox integration for reliability
    - Labels: `area-messaging`, `area-microservices`, `area-ddd`, `industry-best-practice`
    - Inspired by Microsoft Domain Events, Milan Jovanović
  - **Data Isolation per Module** (Issue #359) - Module boundary enforcement
    - `ModuleDataIsolation` enum: None, SeparateSchema, SeparateDatabase
    - `[ModuleSchema("orders")]` attribute for module schema declaration
    - `IModuleDbContext<TModule>` interface
    - Roslyn analyzer `ModuleDataIsolationAnalyzer` with rules:
      - ENC001: Cross-module DbContext access detected
      - ENC002: Direct table reference to another module's schema
      - ENC003: JOIN across module boundaries
    - Runtime query boundary enforcement
    - New packages planned: `Encina.Modular.Data`, `Encina.Modular.Data.Analyzers`
    - Labels: `area-data-isolation`, `roslyn-analyzer`, `area-architecture-testing`
    - Inspired by Milan Jovanović Data Isolation patterns
  - **Module Lifecycle Enhancement** (Issue #360) - Orleans/NestJS-inspired module system
    - Automatic module discovery: `DiscoverModulesFromAssemblies()`, `DiscoverModulesFromPattern()`
    - `[DependsOn(typeof(OtherModule))]` for dependency declaration
    - Topological sort for startup order
    - Expanded lifecycle hooks: OnModulePreConfigure, Configure, PostConfigure, Initialize, Started, Stopping, Stopped
    - Module exports (NestJS-inspired): `public override Type[] Exports`
    - `ModuleGraph` for visualization/debugging
    - Labels: `area-module-system`, `area-pipeline`, `industry-best-practice`
    - Inspired by Orleans Lifecycle, NestJS Modules
  - **Feature Flags Integration** (Issue #361) - Microsoft.FeatureManagement integration
    - `[FeatureGate("FeatureName")]` attribute for handlers
    - `FeatureGatePipelineBehavior` with short-circuit on disabled feature
    - `[FallbackHandler]` for fallback when feature is off
    - Per-tenant feature flags with `[TenantFeatureFilter]`
    - Azure App Configuration support
    - New packages planned: `Encina.FeatureManagement`, `Encina.FeatureManagement.AspNetCore`
    - Labels: `area-feature-flags`, `saas-enabler`, `cloud-azure`
  - **Module Testing Utilities** (Issue #362) - Encina.Testing extensions
    - `ModuleTestBase<TModule>` base class for isolated module testing
    - `WithMockedModule<TApi>()` for mocking module dependencies
    - `IntegrationEvents.ShouldContain<TEvent>()` assertions
    - `ModuleArchitecture.Analyze()` for architecture testing
    - `ModuleDataArchitecture` for data isolation tests
    - Given/When/Then helpers for saga testing
    - Labels: `area-testing`, `area-architecture-testing`, `testing-integration`
    - Inspired by ArchUnitNET, NetArchTest
  - **Anti-Corruption Layer Support** (Issue #363) - DDD pattern
    - `IAntiCorruptionLayer<TExternal, TInternal>` interface
    - `IAsyncAntiCorruptionLayer<,>` for complex async translations
    - `[ModuleAdapter(From, To)]` for inter-module adapters
    - `[ExternalSystemAdapter("LegacyERP")]` for external systems
    - `AntiCorruptionPipelineBehavior` for automatic translation
    - Auto-discovery of adapters
    - Labels: `area-acl`, `area-ddd`, `area-interop`, `area-microservices`
    - Inspired by Azure ACL Pattern
  - **Module Health & Readiness** (Issue #364) - Cloud-native module health
    - `IModuleHealthCheck` for per-module health checks
    - `IModuleReadinessCheck` for readiness probes
    - `ModuleHealthCheckBase` abstract class
    - Dependency-aware health propagation
    - ASP.NET Core integration: `AddEncinaModuleHealthChecks()`
    - Per-module endpoints: `/health/{moduleName}`
    - Labels: `area-health-checks`, `area-cloud-native`, `cloud-aws`, `cloud-azure`
  - **Vertical Slice Architecture Support** (Issue #365) - VSA formalization
    - `[VerticalSlice("Orders/PlaceOrder")]` attribute
    - `[SlicePipeline(...)]` for slice-scoped behaviors
    - Feature folder convention: `Features/{Domain}/{Slice}/`
    - CLI generator: `encina generate slice`
    - `SliceTestBase<TSlice>` for isolated testing
    - Labels: `area-vertical-slice`, `area-cli`, `industry-best-practice`
    - Inspired by Jimmy Bogard VSA
  - **Module Versioning** (Issue #366) - API evolution for modules
    - `[ModuleVersion("2.0")]` attribute
    - `[ModuleApiVersion]` for versioned public APIs
    - `[Deprecated("message", RemovalVersion = "3.0")]` with warnings
    - `IModuleVersionAdapter<TFrom, TTo>` for version bridging
    - Roslyn analyzer for deprecated API usage (ENC010)
    - Version compatibility validation at startup
    - Labels: `area-versioning`, `roslyn-analyzer`, `area-openapi`

- New labels created for Modular Monolith patterns:
  - `area-modular-monolith` - Modular Monolith architecture patterns
  - `area-data-isolation` - Data isolation and schema separation
  - `area-acl` - Anti-Corruption Layer patterns
  - `area-vertical-slice` - Vertical Slice Architecture patterns
  - `area-module-system` - Module system, lifecycle, and discovery
  - `saas-enabler` - Enables SaaS application development
  - `roslyn-analyzer` - Requires Roslyn analyzer implementation

#### Advanced Messaging Patterns Issues (15 new features planned based on market research)

- **Message Batching** (Issue #121) - Process multiple messages in a single handler invocation
    - Inspired by Wolverine 4.0's batch handler support
    - Time-based, count-based, and size-based batching modes
    - Integration with Outbox/Inbox patterns
  - **Claim Check Pattern** (Issue #122) - External storage for large message payloads
    - Store large payloads in Azure Blob, S3, or FileSystem
    - Pass only reference through message broker
    - Reduces messaging costs and improves throughput
    - New packages planned: `Encina.ClaimCheck`, `Encina.ClaimCheck.AzureBlob`, `Encina.ClaimCheck.AmazonS3`
  - **Message Priority** (Issue #123) - Priority-based message processing
    - Process high-priority messages before lower-priority ones
    - Anti-starvation mechanisms for fairness
  - **Enhanced Deduplication** (Issue #124) - Multiple deduplication strategies
    - MessageId, ContentHash, TimeWindow, IdempotencyKey strategies
    - Extends current Inbox pattern capabilities
  - **Multi-Tenancy Messaging** (Issue #125) - First-class SaaS tenant isolation
    - Automatic TenantId propagation in message context
    - Tenant-isolated stores (Outbox, Inbox, Saga)
    - Per-tenant configuration and rate limits
  - **Message TTL** (Issue #126) - Time-to-live and automatic expiration
    - Prevents processing stale data
    - Integration with Dead Letter Queue
  - **Request/Response RPC** (Issue #127) - RPC-style messaging
    - Synchronous-style communication over message brokers
    - Correlation ID management and timeout handling
  - **Saga Visibility** (Issue #128) - Enhanced saga observability
    - Query APIs for saga state
    - Step history and audit trail
    - Metrics for in-flight, completed, and failed sagas
  - **Message Encryption** (Issue #129) - Compliance-ready encryption
    - Transparent encryption/decryption in Outbox/Inbox
    - Multiple providers: Azure Key Vault, AWS KMS, Data Protection
    - GDPR, HIPAA, PCI-DSS compliance support
    - New packages planned: `Encina.Encryption`, `Encina.Encryption.AzureKeyVault`, `Encina.Encryption.AwsKms`
  - **Competing Consumers** (Issue #130) - Consumer group management
    - Consumer group registration and rebalancing
    - Partition assignment strategies
    - Kubernetes-native scaling support
  - **Backpressure & Flow Control** (Issue #131) - Overload protection
    - Producer rate limiting
    - Queue depth monitoring
    - Adaptive concurrency control
  - **W3C Trace Context** (Issue #132) - OpenTelemetry context propagation
    - Full W3C Trace Context support (traceparent, tracestate)
    - Baggage propagation for custom metadata
    - Activity integration for handlers
  - **Recurring Messages** (Issue #133) - Cron-style scheduling
    - Cron expression support with timezone handling
    - Missed occurrence strategies
    - Extends current Scheduling pattern
  - **Message Versioning** (Issue #134) - Schema evolution with upcasting
    - Version stamps on messages
    - Upcaster registry for automatic transformation
    - Zero-downtime deployments
  - **Poison Message Detection** (Issue #135) - Intelligent poison message handling
    - Automatic classification (transient, permanent, malformed)
    - Per-classification actions (retry, DLQ, quarantine)
    - Security violation alerting

#### Database Providers Patterns Issues (16 new features planned based on December 2025 research)

- **Generic Repository Pattern** (Issue #279) - Unified data access abstraction
    - `IRepository<TEntity, TId>` with GetByIdAsync, AddAsync, UpdateAsync, DeleteAsync, ListAsync
    - `IReadRepository<TEntity, TId>` for CQRS scenarios
    - Implementations for EF Core, Dapper, MongoDB
    - Inspired by Ardalis.Specification
  - **Specification Pattern** (Issue #280) - Reusable query encapsulation
    - `ISpecification<T>` with Criteria, Includes, OrderBy, Paging
    - `Specification<T>` base class with fluent API
    - Provider-specific evaluators (EF Core LINQ, Dapper SQL, MongoDB FilterDefinition)
  - **Unit of Work Pattern** (Issue #281) - Cross-aggregate transactions
    - `IUnitOfWork` with SaveChangesAsync, BeginTransactionAsync, CommitAsync, RollbackAsync
    - Repository factory method for transactional consistency
  - **Multi-Tenancy Database Support** (Issue #282) - SaaS tenant isolation
    - Strategies: Shared Schema, Schema-per-Tenant, Database-per-Tenant
    - Tenant resolvers: Header, Subdomain, JWT Claim, Route
    - New packages planned: `Encina.Tenancy`, `Encina.Tenancy.SharedSchema`, etc.
  - **Read/Write Database Separation** (Issue #283) - CQRS physical split
    - `IReadWriteDbContextFactory<TContext>` for read replica routing
    - Automatic routing based on IQuery/ICommand
    - Azure SQL ApplicationIntent=ReadOnly support
  - **Bulk Operations** (Issue #284) - High-performance data operations
    - `IBulkOperations<TEntity>`: BulkInsertAsync, BulkUpdateAsync, BulkDeleteAsync, BulkMergeAsync
    - Performance: Dapper 27-384x, EF Core 100x, ADO.NET 64-244x, MongoDB 13-112x faster
    - Measured with Testcontainers (SQL Server 2022, MongoDB 7)
  - **Soft Delete & Temporal Tables** (Issue #285) - Logical delete + history
    - `ISoftDeletable` interface with automatic global query filter
    - `ITemporalRepository<TEntity, TId>` for SQL Server temporal tables
    - Queries: GetAsOfAsync, GetHistoryAsync, GetChangedBetweenAsync
  - **Audit Trail Pattern** (Issue #286) - Change tracking
    - `IAuditableEntity` with CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
    - `AuditInterceptor` for automatic population via IRequestContext.UserId
    - TimeProvider integration for testable timestamps
  - **Optimistic Concurrency Abstractions** (Issue #287) - Conflict resolution
    - `IConcurrencyAware` (RowVersion) and `IVersioned` (integer version)
    - `IConcurrencyConflictResolver<TEntity>`: ClientWins, DatabaseWins, Merge strategies
    - `IConcurrentRepository<TEntity, TId>` with retry support
  - **CDC Integration** (Issue #288) - Change Data Capture
    - `ChangeEvent<T>` with Operation, Before, After, Metadata
    - `ICDCConsumer<T>`, `ICDCSubscriptionManager` interfaces
    - New packages planned: `Encina.CDC`, `Encina.CDC.Debezium`
    - Complements Outbox for legacy system integration
  - **Database Sharding Abstractions** (Issue #289) - Horizontal partitioning
    - `IShardable`, `IShardRouter<TEntity>`, `IShardedRepository<TEntity, TId>`
    - Strategies: Hash (consistent hashing), Range, Directory, Geo
    - Scatter-Gather for cross-shard queries
  - **Connection Pool Resilience** (Issue #290) - Pool monitoring and health
    - `IDatabaseHealthMonitor` with ConnectionPoolStats
    - Database-aware circuit breaker, connection warm-up
    - Integration with Encina.Extensions.Resilience
  - **Query Cache Interceptor** (Issue #291) - EF Core second-level cache
    - `IDbCommandInterceptor` for automatic query result caching
    - Automatic invalidation on SaveChanges
    - Integration with existing ICacheProvider
  - **Domain Entity Base Classes** (Issue #292) - DDD foundations
    - `Entity<TId>` with equality and domain events collection
    - `AggregateRoot<TId>` with audit + concurrency traits
    - `DomainEventDispatcher` SaveChanges interceptor
  - **Pagination Abstractions** (Issue #293) - Standardized paging
    - `PagedResult<T>` with TotalCount, TotalPages, HasNext/Previous
    - `PaginationOptions`, `SortedPaginationOptions`
    - `IPagedSpecification<T>` integration
  - **Cursor-based Pagination** (Issue #294) - Keyset pagination research
    - O(1) performance vs offset O(n) for large datasets
    - GraphQL Relay Connection spec compatible
    - Use cases: Infinite scroll, real-time feeds, public APIs

- New labels created (10) for database patterns:
  - `area-repository` - Repository pattern and data access abstractions
  - `area-unit-of-work` - Unit of Work and transaction patterns
  - `area-cqrs` - CQRS and read/write separation patterns
  - `area-bulk-operations` - Bulk insert, update, delete operations
  - `area-audit` - Audit trails and change tracking
  - `area-cdc` - Change Data Capture patterns
  - `area-sharding` - Database sharding and horizontal partitioning
  - `area-pagination` - Pagination patterns (offset, cursor, keyset)
  - `area-concurrency` - Concurrency control and conflict resolution
  - `area-connection-pool` - Connection pooling and management

#### Advanced DDD & Workflow Patterns Issues (13 new features planned based on December 29, 2025 research)

- **Specification Pattern** (Issue #295) - Query composition with reusable specifications
    - `ISpecification<T>` with Criteria, Includes, OrderBy, Paging
    - `Specification<T>` base class with fluent builder
    - AND/OR/NOT composition operators
    - Provider evaluators for EF Core and Dapper
    - Inspired by Ardalis.Specification
    - Labels: `pattern-specification`, `area-ddd`, `area-repository`, `industry-best-practice`
  - **Process Manager Pattern** (Issue #296) - Dynamic workflow orchestration
    - `IProcessManager<TData>` with `ProcessDecision` types
    - Dynamic routing vs predefined Saga sequences
    - Background processor for process advancement
    - Inspired by Enterprise Integration Patterns
    - Labels: `pattern-process-manager`, `area-workflow`, `area-eip`, `masstransit-inspired`
  - **State Machine Pattern (FSM)** (Issue #297) - Fluent state machine for entity lifecycle
    - `StateMachineBuilder<TState, TTrigger>` fluent API
    - Entry/exit actions, substates, guards
    - External state accessor for ORM integration
    - DOT graph export for visualization
    - Inspired by Stateless library (6k+ stars) and MassTransit Automatonymous
    - Labels: `pattern-state-machine`, `area-saga`, `area-workflow`, `masstransit-inspired`
  - **Claim Check Pattern** (Issue #298) - Large payload handling
    - `IClaimCheckStore` for external payload storage
    - `[ClaimCheck]` attribute with threshold and expiry
    - `ClaimCheckPipelineBehavior` for automatic handling
    - Providers: Azure Blob, S3, File System, In-Memory
    - Classic Enterprise Integration Pattern
    - Labels: `pattern-claim-check`, `area-eip`, `area-messaging`, `area-scalability`
  - **Anti-Corruption Layer (ACL)** (Issue #299) - Domain protection from external APIs
    - `IAntiCorruptionLayer<TExternal, TInternal>` interface
    - `AntiCorruptionLayerBehavior` for automatic translation
    - `IExternalEventTranslator` for integration events
    - Core DDD pattern by Eric Evans
    - Labels: `pattern-acl`, `area-ddd`, `area-integration`, `industry-best-practice`
  - **Feature Flag Integration** (Issue #300) - Microsoft.FeatureManagement pipeline
    - `[FeatureGate]` attribute with behavior options
    - `FeatureFlagPipelineBehavior` for short-circuit
    - Contextual targeting via IRequestContext
    - Use cases: dark deployments, canary releases, A/B testing
    - Labels: `pattern-feature-flags`, `area-feature-management`, `cloud-azure`
  - **Priority Queue Support** (Issue #301) - Priority-based message processing
    - `MessagePriority` enum (Critical, High, Normal, Low, Background)
    - `[MessagePriority]` attribute for requests
    - Priority-aware batch fetching in Outbox and Scheduling
    - Labels: `area-messaging`, `area-scheduling`, `area-scalability`
  - **Batching/Bulk Operations** (Issue #302) - Batch handler pattern
    - `IBatchHandler<TRequest, TResponse>` interface
    - `BatchingPipelineBehavior` with auto-batching
    - `[BatchOptions]` with MaxBatchSize, MaxDelayMs
    - Failure strategies: Individual, FailAll, RetryFailed
    - Labels: `area-bulk-operations`, `area-messaging`, `area-performance`
  - **Durable Execution / Checkpointing** (Issue #303) - Long-running workflow support
    - `IDurableContext` with ExecuteActivityAsync, WaitForEventAsync, CreateTimerAsync
    - `IDurableWorkflow<TInput, TOutput>` interface
    - Deterministic replay with history
    - Inspired by Azure Durable Functions and Temporal.io
    - Labels: `pattern-durable-execution`, `area-workflow`, `temporal-inspired`, `cloud-azure`
  - **Multi-Tenancy Pipeline Behavior** (Issue #304) - Automatic tenant isolation
    - `ITenantScopedQuery/Command` marker interfaces
    - `TenantIsolationPipelineBehavior` for enforcement
    - EF Core global query filter extension
    - `[AllowCrossTenant]` for admin scenarios
    - Labels: `area-multi-tenancy`, `area-security`, `area-compliance`, `industry-best-practice`
  - **AI Agent Orchestration** (Issue #305) - LLM agent coordination (Future)
    - `IAgentHandler<TRequest, TResponse>` with capabilities
    - Orchestration patterns: Sequential, Concurrent, Handoff
    - Semantic Kernel adapter
    - Inspired by Microsoft Agent Framework (Oct 2025)
    - Labels: `area-ai-ml`, `area-workflow`, `new-package`
  - **Integration Events** (Issue #306) - Modular Monolith inter-module events
    - `IIntegrationEvent` with EventId, OccurredAtUtc, SourceModule
    - `IModuleEventBus` for in-process publishing
    - Outbox integration for reliability
    - Inspired by Spring Modulith 2.0
    - Labels: `area-modular-monolith`, `area-messaging`, `area-microservices`
  - **Request Versioning Pattern** (Issue #307) - Request evolution and upcasting
    - `[RequestVersion]` and `IUpgradableFrom<T>` interfaces
    - `RequestVersioningBehavior` for auto-upgrade
    - `[DeprecatedRequest]` with warnings
    - Inspired by Marten Event Upcasting
    - Labels: `pattern-versioning`, `area-messaging`, `area-event-sourcing`

- New labels created (12) for DDD & Workflow patterns:
  - `pattern-specification` - Specification pattern for query composition
  - `pattern-process-manager` - Process Manager workflow orchestration
  - `pattern-state-machine` - Finite State Machine pattern
  - `pattern-claim-check` - Claim Check pattern for large payloads
  - `pattern-acl` - Anti-Corruption Layer pattern
  - `pattern-feature-flags` - Feature Flags/Toggles pattern
  - `pattern-durable-execution` - Durable Execution and checkpointing
  - `pattern-versioning` - Request/Event versioning pattern
  - `area-feature-management` - Feature flag management
  - `area-workflow` - Workflow and process orchestration
  - `temporal-inspired` - Pattern inspired by Temporal.io
  - `masstransit-inspired` - Pattern inspired by MassTransit

#### Advanced EDA Patterns Issues (12 new features planned based on December 29, 2025 research)

- Based on analysis of MassTransit, Wolverine 5.0, Temporal.io, Axon Framework, Debezium, and community demand
- **CDC (Change Data Capture) Pattern** (Issue #308) - Database change streaming
    - `ICdcConnector`, `IChangeEventHandler<TEntity>` for insert/update/delete
    - New packages planned: `Encina.CDC`, `Encina.CDC.Debezium`, `Encina.CDC.SqlServer`
    - Use case: Strangler Fig migration, legacy system integration
    - Labels: `area-cdc`, `area-microservices`, `industry-best-practice`, `aot-compatible`
  - **Schema Registry Integration** (Issue #309) - Event schema governance
    - `ISchemaRegistry` with GetSchema, RegisterSchema, CheckCompatibility
    - New packages planned: `Encina.SchemaRegistry`, `Encina.SchemaRegistry.Confluent`
    - Supports Avro, Protobuf, JsonSchema formats
    - Labels: `area-schema-registry`, `transport-kafka`, `area-compliance`, `industry-best-practice`
  - **Event Mesh / Event Gateway** (Issue #310) - Enterprise event distribution
    - `IEventMesh`, `IEventGateway` for cross-transport routing
    - New packages planned: `Encina.EventMesh`, `Encina.EventMesh.CloudEvents`
    - Cross-transport: Kafka → RabbitMQ → Azure Service Bus
    - Labels: `area-cloud-native`, `area-integration`, `industry-best-practice`
  - **Claim Check Pattern** (Issue #311) - Large payload external storage
    - `IClaimCheckStore` with Store/Retrieve/Delete and `ClaimTicket`
    - New packages planned: `Encina.ClaimCheck`, `Encina.ClaimCheck.AzureBlob`, `Encina.ClaimCheck.S3`
    - `[ClaimCheck]` attribute with ThresholdBytes
    - Labels: `area-eip`, `area-performance`, `cloud-azure`, `cloud-aws`
  - **Domain vs Integration Events** (Issue #312) - Clear event type separation
    - `IDomainEvent` (in-process), `IIntegrationEvent` (cross-service)
    - `IEventTranslator<TDomain, TIntegration>` for Anti-Corruption Layer
    - Core DDD pattern for bounded context isolation
    - Labels: `area-ddd`, `area-event-sourcing`, `area-modular-monolith`, `area-microservices`
  - **Event Correlation & Causation Tracking** (Issue #313) - Full event traceability
    - `IEventMetadata` with EventId, CorrelationId, CausationId, Timestamp
    - `EventCorrelationPipelineBehavior` for automatic propagation
    - OpenTelemetry integration with span tags
    - Labels: `area-observability`, `netflix-pattern`, `industry-best-practice`
  - **Temporal Queries (Time Travel)** (Issue #314) - Point-in-time state queries
    - `ITemporalRepository<T>` with GetAt(pointInTime), GetAtVersion, GetHistory
    - `AggregateDiff<T>` for state comparison
    - Use case: Auditing, debugging, what-if scenarios
    - Labels: `area-event-sourcing`, `area-compliance`, `industry-best-practice`
  - **Durable Execution / Workflow Engine** (Issue #315) - Lightweight Temporal.io-style
    - `IDurableWorkflow<TInput, TOutput>`, `IWorkflowContext`, `IWorkflowRunner`
    - Activities, durable timers, signals, deterministic replay
    - New packages planned: `Encina.DurableExecution`, `Encina.DurableExecution.EntityFrameworkCore`
    - Labels: `area-workflow`, `temporal-pattern`, `uber-pattern`, `netflix-pattern`
  - **Event Enrichment Pipeline** (Issue #316) - Batch enrichment for projections
    - `IEventEnricher<T>`, `IBatchEventEnricher` for N+1 avoidance
    - `EnrichmentContext` with StreamId, Version, Services
    - Inspired by Marten 4.13 EnrichEventsAsync
    - Labels: `area-event-sourcing`, `area-performance`, `area-pipeline`
  - **Process Manager Pattern** (Issue #317) - Long-running aggregate coordination
    - `IProcessManager<TState>`, `ProcessManagerBase<TState>`
    - Event-driven coordination vs Saga's predefined sequences
    - `IProcessManagerStore` for persistence
    - Labels: `area-saga`, `area-event-sourcing`, `area-coordination`
  - **Event Streaming Abstractions** (Issue #318) - First-class event streams
    - `IEventStreamPublisher`, `IEventStreamSubscription`
    - Consumer groups, position tracking, acknowledgment
    - Similar to Kafka consumer groups, RabbitMQ Streams
    - Labels: `area-event-streaming`, `transport-kafka`, `transport-redis`, `netflix-pattern`
  - **Idempotency Key Generator** (Issue #319) - Standardized key generation
    - `IIdempotencyKeyGenerator` with Generate, GenerateFromParts
    - `[IdempotencyKey]` attribute with Properties, Namespace, Format
    - Strategies: Hash (SHA256), Composite, UUID v5
    - Labels: `area-idempotency`, `stripe-pattern`, `uber-pattern`, `netflix-pattern`

- New labels created (6) for EDA patterns:
  - `area-schema-registry` - Schema Registry and event schema governance
  - `area-event-streaming` - Event streaming and persistent log patterns
  - `area-idempotency` - Idempotency and exactly-once processing
  - `uber-pattern` - Pattern inspired by Uber engineering
  - `stripe-pattern` - Pattern inspired by Stripe engineering
  - `temporal-pattern` - Pattern inspired by Temporal.io

#### Advanced Caching Patterns Issues (13 new features planned based on December 2025 research)

- **Cache Stampede Protection** (Issue #266) - Thundering herd prevention with multiple strategies
    - Inspired by FusionCache (most popular .NET caching library 2025)
    - Single-Flight pattern: Coalesce concurrent requests into one factory execution
    - Probabilistic Early Expiration (PER): Renew cache before expiration probabilistically
    - TTL Jitter: Add random variation to prevent synchronized expiration
    - Labels: `pattern-stampede-protection`, `area-resilience`, `area-performance`
  - **Eager Refresh / Background Refresh** (Issue #267) - Proactive cache refresh
    - Inspired by FusionCache's EagerRefreshThreshold
    - Refresh in background before TTL expires (e.g., after 80% of duration)
    - Users always get cached response, fresh data arrives asynchronously
    - Eliminates latency spikes from cache expiration
  - **Fail-Safe / Stale-While-Revalidate** (Issue #268) - Resilient caching
    - Serve stale data when factory fails or is slow
    - Soft/Hard timeout support: Return stale immediately if factory exceeds threshold
    - FailSafeDurationSeconds: Extended TTL for emergency use
    - Fail-safe throttling to prevent retry storms
  - **Cache Warming / Pre-warming** (Issue #269) - Cold cache elimination
    - `ICacheWarmer` interface for custom warmers
    - `[CacheWarmer]` attribute for automatic query warming
    - Startup warming via `CacheWarmingHostedService`
    - Configurable strategies: Sequential, Parallel, TopHeavy
  - **Cache Backplane** (Issue #270) - Multi-node synchronization
    - `ICacheBackplane` interface for L1 cache sync across instances
    - Redis backplane implementation with Pub/Sub
    - Modes: InvalidationOnly, SmallValueReplication, FullReplication
    - Node coordination and health tracking
  - **Enhanced Tag-Based Invalidation** (Issue #271) - Semantic cache grouping
    - `Tags` property on `[Cache]` attribute
    - `[CacheTag]` attribute for dynamic tags from response
    - `RemoveByTagAsync` on `ICacheProvider`
    - More efficient than pattern-based invalidation (O(1) vs scan)
  - **Read-Through / Write-Through Patterns** (Issue #272) - Alternative caching strategies
    - `CacheStrategy` enum: CacheAside, ReadThrough, WriteThrough, WriteBehind
    - Read-Through: Cache as primary data source
    - Write-Through: Synchronous write to cache + database
    - Write-Behind: Async persistence with batching
  - **Cache Metrics OpenTelemetry** (Issue #273) - Comprehensive observability
    - Counters: hits, misses, sets, removals, evictions
    - Histograms: latency, value size
    - Gauges: size_bytes, entry_count, hit_rate
    - Resilience: stampede_prevented, stale_served, backplane_messages
  - **Advanced Serialization** (Issue #274) - Performance optimization
    - Per-type serializer configuration
    - MemoryPack support (NativeAOT, ~10x faster than MessagePack)
    - Zstd compression (better ratio than LZ4)
    - Smart compression based on payload size
  - **Multi-Tenant Cache Policies** (Issue #275) - SaaS support
    - `CacheTenantPolicy` per tier (premium, standard, free)
    - Quotas: MaxEntries, MaxMemoryMb, DefaultDuration
    - Rate limiting per tenant
    - Tenant isolation levels: KeyPrefix, Database, Instance
  - **Cache Diagnostics & Debugging** (Issue #276) - Development tooling
    - HTTP headers: X-Cache-Status, X-Cache-Key, X-Cache-Age, X-Cache-TTL
    - Diagnostic endpoints: /cache/stats, /cache/keys, /cache/key/{key}
    - `ICacheInspector` API for programmatic access
    - Cache debugger middleware (?cache-debug=true)
  - **New Cache Providers** (Issue #277) - Expanded ecosystem
    - `Encina.Caching.Memcached` - Pure Memcached support
    - `Encina.Caching.MemoryPack` - AOT-friendly serialization
    - `Encina.Caching.Pogocache` - New 2025 cache (evaluate when stable)
  - **Auto-Recovery / Self-Healing** (Issue #278) - Automatic resilience
    - Retry logic with exponential backoff
    - Circuit breaker for cache operations
    - Automatic reconnection with `ICacheConnectionManager`
    - Fallback strategies: SkipCache, UseLocalMemory, UseSecondaryProvider
    - Self-healing backplane (auto-resubscribe, clear L1 on reconnect)

- New pattern labels created for caching:
  - `pattern-stampede-protection` - Cache stampede and thundering herd protection
  - `pattern-stale-while-revalidate` - Stale-While-Revalidate caching pattern
  - `pattern-read-through` - Read-Through caching pattern
  - `pattern-write-through` - Write-Through caching pattern
  - `pattern-cache-aside` - Cache-Aside caching pattern
  - `pattern-backplane` - Cache backplane synchronization pattern
  - `pattern-circuit-breaker` - Circuit Breaker resilience pattern
  - `fustioncache-inspired` - Pattern inspired by FusionCache library

- Issue #140 (Cache Stampede Prevention) closed as duplicate of #266 (more comprehensive)

#### Advanced Resilience Patterns Issues (9 new features planned based on 2025 research)

- **Hedging Pattern** (Issue #136) - Parallel redundant requests for latency reduction
    - Inspired by Polly v8 and Istio service mesh
    - Configure parallel requests with first-response-wins semantics
    - Latency percentile-based triggering (P95, P99)
    - Integration with OpenTelemetry for observability
  - **Fallback / Graceful Degradation** (Issue #137) - Alternative responses when primary operations fail
    - Inspired by Resilience4j and Polly
    - Cached fallbacks, static defaults, and degraded responses
    - Fallback chain with priority ordering
    - Circuit breaker integration for proactive fallback
  - **Load Shedding with Priority** (Issue #138) - Netflix/Uber-inspired priority-based request shedding
    - Request priority levels: Critical, Degraded, BestEffort, Bulk
    - Adaptive shedding based on system load metrics
    - Integration with rate limiting and circuit breakers
  - **Adaptive Concurrency Control** (Issue #139) - Netflix-inspired dynamic concurrency limits
    - Inspired by Netflix's `concurrency-limits` library
    - AIMD (Additive Increase/Multiplicative Decrease) algorithm
    - Gradient-based limit adjustment based on latency
    - TCP Vegas-style congestion control for services
  - ~~**Cache Stampede Prevention** (Issue #140)~~ - Closed as duplicate of #266 (more comprehensive)
    - Probabilistic early expiration to spread load
  - **Cascading Timeout Coordination** (Issue #141) - Timeout budget propagation across call chains
    - Request deadline propagation via gRPC-style patterns
    - Remaining budget calculation at each service hop
    - Early termination when budget exhausted
  - **Health Checks Standardization** (Issue #142) - Unified health checks across all providers
    - Kubernetes liveness/readiness/startup probe patterns
    - Consistent health check interface for all Encina providers
    - Health aggregation for composite systems
  - **Observability-Resilience Correlation** (Issue #143) - OpenTelemetry integration for resilience events
    - Resilience events as OpenTelemetry spans
    - Metrics for circuit breaker state, retry counts, fallback usage
    - Distributed tracing context propagation through resilience policies
  - **Backpressure / Flow Control** (Issue #144) - Reactive Streams-style backpressure for streaming
    - Producer/consumer rate coordination
    - Buffer overflow strategies (drop, block, latest)
    - Integration with IAsyncEnumerable and stream requests
  - **Chaos Engineering Integration** (Issue #145) - Polly Chaos strategies for fault injection testing
    - Latency injection, exception injection, result manipulation
    - Controlled chaos via feature flags
    - Integration with testing infrastructure

#### Advanced Scheduling Patterns Issues (15 new features planned based on 2025 research)

- **Cancellation & Update API** (Issue #146) - Cancel, reschedule, or update scheduled messages
    - Inspired by MassTransit, Hangfire, Temporal
    - `CancelAsync`, `RescheduleAsync`, `UpdatePayloadAsync` methods
    - Batch cancellation with filters
  - **Priority Queue Support** (Issue #147) - Priority-based message processing
    - Inspired by Meta FOQS and BullMQ
    - Priority levels: Critical, High, Normal, Low, Background
    - Anti-starvation with aging mechanism
  - **Idempotency Keys** (Issue #148) - Exactly-once execution guarantee
    - Inspired by Temporal, Azure Durable Functions
    - User-provided idempotency keys
    - Automatic duplicate detection and rejection
  - **Dead Letter Queue Integration** (Issue #149) - DLQ for failed scheduled messages
    - Integration with existing DLQ infrastructure
    - Automatic move after max retries
    - Inspection and replay capabilities
  - **Timezone-Aware Scheduling** (Issue #150) - Full timezone support
    - Inspired by Hangfire, Quartz.NET
    - DST transition handling
    - IANA timezone database support
  - **Rate Limiting for Scheduled** (Issue #151) - Prevent burst execution
    - Inspired by Meta FOQS, Redis Queue
    - Per-type and global rate limits
    - Token bucket algorithm
  - **Dependency Chains** (Issue #152) - DAG-based job dependencies
    - Inspired by Apache Airflow, Temporal
    - Job dependencies with `DependsOn`
    - Parallel execution of independent jobs
  - **Observability & Metrics** (Issue #153) - OpenTelemetry integration
    - Scheduling-specific spans and metrics
    - Queue depth, execution latency, success/failure rates
    - Grafana dashboard templates
  - **Batch Scheduling** (Issue #154) - Efficient bulk operations
    - `ScheduleManyAsync` for bulk scheduling
    - Transactional batch support
    - Optimized database operations
  - **Delayed Message Visibility** (Issue #155) - SQS-style visibility timeout
    - Inspired by Amazon SQS
    - Lease-based processing
    - Automatic re-queue on timeout
  - **Scheduling Persistence Providers** (Issue #156) - Backend adapters
    - Hangfire backend adapter
    - Quartz.NET backend adapter
    - Unified API across backends
  - **Execution Windows** (Issue #157) - Business hours support
    - Inspired by Quartz.NET, enterprise schedulers
    - Business hours and maintenance windows
    - Holiday calendar integration
  - **Schedule Templates** (Issue #158) - Reusable configurations
    - Named schedule templates
    - Template inheritance and override
    - Centralized schedule management
  - **Webhook Notifications** (Issue #159) - External system notifications
    - HTTP webhook on schedule events
    - Retry with exponential backoff
    - Signature verification for security
  - **Multi-Region Scheduling** (Issue #160) - Globally distributed scheduling
    - Inspired by Meta FOQS, Uber Cadence
    - Leader election per region
    - Cross-region coordination

#### Advanced Observability Patterns Issues (15 new features planned based on 2025 research)

- **Real Metrics Collection (EncinaMetrics)** (Issue #174) - Full IEncinaMetrics implementation
    - System.Diagnostics.Metrics for zero-allocation metrics
    - Histograms: `encina.request.duration`, `encina.handler.duration`
    - Counters: `encina.requests.total`, `encina.errors.total`
    - Gauges: `encina.active.handlers`, `encina.pending.outbox`
    - Standardized tags for all metrics
    - Inspired by MassTransit, Wolverine OpenTelemetry
  - **Correlation & Causation ID Support** (Issue #175) - Request tracking across services
    - CorrelationId propagation through pipeline
    - CausationId for message causation chains
    - Extension methods for context enrichment
    - Standard headers for all transports
    - Inspired by NServiceBus, MassTransit
  - **Baggage Propagation Utilities** (Issue #176) - W3C Baggage support
    - Helpers for IRequestContext baggage
    - AddBaggage(), GetBaggage(), GetAllBaggage()
    - Automatic propagation to handlers
    - Activity.Baggage integration
    - Inspired by OpenTelemetry spec, .NET Aspire
  - **Missing Semantic Convention Attributes** (Issue #177) - OTel messaging semantics
    - Complete messaging semantic attributes
    - messaging.system, messaging.operation, messaging.destination
    - Handler-specific attributes
    - OpenTelemetry Semantic Conventions compliant
  - **Encina.OpenTelemetry.AzureMonitor** (Issue #178) - Azure integration package
    - Azure Application Insights integration
    - Native Azure Monitor exporters
    - Live Metrics Stream support
    - Azure distributed tracing
  - **Encina.OpenTelemetry.AwsXRay** (Issue #179) - AWS integration package
    - AWS X-Ray via ADOT integration
    - Native AWS exporters
    - AWS Lambda instrumentation
    - AWS distributed tracing
  - **Encina.OpenTelemetry.Prometheus** (Issue #180) - Prometheus metrics package
    - Native /metrics endpoint
    - OpenMetrics format
    - Configurable labels
    - Grafana integration
  - **Encina.HealthChecks Package** (Issue #181) - Dedicated health checks
    - Kubernetes probes: liveness, readiness, startup
    - Health check aggregation
    - Status dashboard support
    - Pattern-specific health checks
  - **Encina.Serilog.OpenTelemetry** (Issue #182) - Serilog to OTel bridge
    - Serilog → OpenTelemetry Logs export
    - Automatic trace context enrichment
    - Optimized formatters
  - **Sampling Behaviors** (Issue #183) - Configurable trace sampling
    - Head and tail sampling
    - Probabilistic sampling
    - Rate-limiting sampler
    - Per-request-type sampling rules
  - **Request Tracing Behavior** (Issue #184) - Detailed request tracing
    - Per-request Activity spans
    - Handler timing
    - Pipeline step visibility
    - Error correlation
  - **Error Recording Enhancements** (Issue #185) - Enhanced error capture
    - Exception details in spans
    - Stack trace recording
    - Error categorization
    - OTel error semantic conventions
  - **Distributed Context Properties** (Issue #186) - Context propagation
    - Custom property propagation
    - Cross-service context sharing
    - W3C Trace Context extensions
  - **Grafana Dashboards** (Issue #187) - Pre-built visualizations
    - Main Encina dashboard JSON
    - Per-pattern dashboards (Outbox, Saga, etc.)
    - Configurable alerts
    - One-click import
  - **Aspire Dashboard Integration Guide** (Issue #188) - .NET Aspire docs
    - Aspire Dashboard configuration
    - Encina trace visualization
    - Local development setup

#### Web/API Integration Patterns Issues (18 new features planned based on December 2025 research)

- **Server-Sent Events** (Issue #189) - Native .NET 10 SSE support
    - Leverage `TypedResults.ServerSentEvents` API from ASP.NET Core 10
    - `SseEndpointExtensions` for easy endpoint registration
    - Heartbeat/keep-alive and automatic retry configuration
    - Integration with notification streaming patterns
    - Use cases: dashboards, real-time notifications, progress indicators
  - **REPR Pattern Support** (Issue #190) - Request-Endpoint-Response pattern
    - `EncinaEndpoint<TRequest, TResponse>` abstract base class
    - `CommandEndpoint` and `QueryEndpoint` specialized variants
    - Fluent `EndpointBuilder` for configuration
    - Auto-registration via assembly scanning
    - Alignment with Vertical Slice Architecture and CQRS
    - Inspired by FastEndpoints and industry best practices
  - **Problem Details RFC 9457** (Issue #191) - Updated error response standard
    - Update from RFC 7807 to RFC 9457 (supersedes 7807)
    - Automatic TraceId inclusion in error responses
    - `IExceptionHandler` implementation for global exception handling
    - Timestamp and error code in extensions
    - Validation problem details with proper grouping
  - **API Versioning Helpers** (Issue #192) - Comprehensive versioning support
    - Integration with `Asp.Versioning.Http` package
    - `[ApiVersion]` attribute support for handlers
    - Version-aware handler resolution
    - Deprecation headers (Sunset RFC 8594, Deprecation)
    - Multiple versioning strategies (URI, query, header, media type)
  - **Minimal APIs Organization** (Issue #193) - Endpoint organization extensions
    - `IEncinaEndpointModule` interface for modular organization
    - `MapEncinaModules` for auto-registration
    - `MapPostEncina`, `MapGetEncina`, etc. extension methods
    - `WithEncinaResponses()` for common response documentation
    - Feature folder conventions support
  - **Encina.SignalR Package** (Issue #194) - Real-time bidirectional communication
    - New package: `Encina.SignalR` (documented but not yet implemented)
    - `ISignalRNotificationBroadcaster` for broadcasting notifications
    - `EncinaHub` base class with Send/Publish methods
    - Group management with tenant isolation
    - `[BroadcastToSignalR]` attribute for automatic broadcasting
    - Integration with existing notification patterns
  - **GraphQL/HotChocolate Full Integration** (Issue #195) - Complete GraphQL support
    - Enhance existing `Encina.GraphQL` from basic bridge to full integration
    - Auto-generate Query/Mutation types from handlers with `[GraphQLQuery]`, `[GraphQLMutation]`
    - Subscription support with pub/sub integration
    - DataLoader base class for N+1 prevention
    - `Either<EncinaError, T>` → GraphQL error mapping via `EncinaErrorFilter`
    - RequestContext propagation to resolvers
  - **gRPC Improvements** (Issue #196) - Strong typing and streaming
    - Proto code generation from handler types
    - Strongly-typed service implementations (replacing reflection-based)
    - Server, client, and bidirectional streaming with `IAsyncEnumerable`
    - Service interceptors for logging, auth, metrics
    - `EncinaError` → gRPC Status code mapping
  - **Rate Limiting Pipeline Behavior** (Issue #197) - Handler-level rate limiting
    - `[RateLimit]` attribute for per-handler configuration
    - Support for Fixed/Sliding/Token/Concurrency limiters
    - Partition keys: User, Tenant, IP, ApiKey, Custom
    - Response headers: X-RateLimit-Limit, X-RateLimit-Remaining, Retry-After
    - Distributed rate limiting support (Redis, SQL Server)
    - Integration with ASP.NET Core rate limiting middleware
  - **OpenAPI 3.1 Enhanced** (Issue #198) - Schema generation and SDK support
    - Auto-generate OpenAPI schemas from handler types
    - Data annotation → OpenAPI constraint mapping
    - XML comments integration
    - Encina error response documentation
    - Client SDK generation helpers (NSwag, OpenAPI Generator)
    - YAML export support
  - **BFF Pattern Support** (Issue #199) - Backend for Frontend
    - New package: `Encina.AspNetCore.BFF`
    - `IBffAggregator` for query aggregation
    - Client-specific response transformation
    - Client detection (header, user-agent, claims)
    - Client-aware caching
    - Parallel aggregation with timeout and partial results
  - **AI/LLM Integration Patterns** (Issue #200) - Provider-agnostic AI integration
    - New packages: `Encina.AI`, `Encina.AI.OpenAI`, `Encina.AI.Azure`, `Encina.AI.Anthropic`, `Encina.AI.Ollama`, `Encina.AI.SemanticKernel`
    - `IAIProvider` abstraction for multiple LLM providers
    - `IAIRequest<TResponse>` integration with Encina pipeline
    - Chat completion, embedding, and structured output support
    - Streaming responses via `IAsyncEnumerable`
    - Prompt validation behavior (PII detection, injection prevention)
    - Semantic Kernel adapter for orchestration
    - Fallback chain for provider resilience
  - **Vertical Slice Architecture Templates** (Issue #201) - CLI scaffolding
    - `encina new feature <name>` command for complete feature slices
    - Generate command, query, endpoint, validator, and test files
    - Module registration and DI extension files
    - Custom template support
    - Interactive mode for guided generation
  - **WebHook Support** (Issue #202) - Webhook receiving and sending
    - `IWebhookHandler<TPayload>` interface
    - Signature validation (HMAC-SHA256, HMAC-SHA1)
    - Timestamp validation for replay attack prevention
    - Inbox pattern integration for idempotency
    - Webhook sender with retry and dead letter
    - Provider configuration (Stripe, GitHub, etc.)
  - **Health Aggregation Endpoint** (Issue #203) - Combined health checks
    - Aggregated `/health` endpoint
    - `/health/ready` and `/health/live` separation (Kubernetes probes)
    - `/health/detailed` with authorization
    - Module health check auto-discovery
    - Response caching
  - **Passkey Authentication** (Issue #204) - WebAuthn/FIDO2 support
    - `[RequirePasskey]` attribute for high-security operations
    - Integration with .NET 10 ASP.NET Core Identity passkey features
    - `IPasskeyChallenger` for challenge/response flow
    - Fallback to password option
  - **Google Cloud Functions** (Issue #205) - GCF integration
    - New package: `Encina.GoogleCloudFunctions`
    - `EncinaHttpFunction` base class
    - `EncinaCloudEventFunction` for Pub/Sub events
    - Context enrichment (correlation ID, trace ID)
    - Health check integration
  - **Cloudflare Workers** (Issue #206) - Edge computing integration
    - New package: `Encina.CloudflareWorkers`
    - `EncinaWorker` base class
    - KV storage integration (as cache provider)
    - Durable Objects for saga coordination (future)
    - D1 database integration (future)

- New Labels Created (Web/API - December 2025):
  - `area-ai-ml` - AI/ML and LLM integration patterns
  - `area-bff` - Backend for Frontend patterns
  - `area-openapi` - OpenAPI/Swagger documentation and generation
  - `area-webhooks` - Webhook receiving and sending patterns
  - `area-rate-limiting` - Rate limiting and throttling patterns
  - `area-health-checks` - Health checks and readiness probes
  - `area-authentication` - Authentication patterns (Passkeys, OAuth, etc.)
  - `cloud-cloudflare` - Cloudflare Workers and services

#### Advanced Testing Patterns Issues (13 new features planned based on 2025 research)

- **Test Data Generation** (Issue #161) - Bogus/AutoBogus integration for realistic test data
    - `EncinaFaker<T>` base class with Encina-specific conventions
    - Pre-built fakers for messaging entities (Outbox, Inbox, Saga, Scheduled)
    - Seed support for deterministic, reproducible tests
    - New package planned: `Encina.Testing.DataGeneration`
  - **Testcontainers Integration** (Issue #162) - Docker fixtures for database testing
    - Pre-configured fixtures: `SqlServerContainerFixture`, `PostgreSqlContainerFixture`, `MongoDbContainerFixture`, `RedisContainerFixture`
    - Integration with existing `EncinaFixture`
    - GitHub Actions CI/CD compatible
    - New package planned: `Encina.Testing.Testcontainers`
  - **Database Reset with Respawn** (Issue #163) - Intelligent cleanup between tests
    - `EncinaRespawner` factory with Encina-specific table exclusions
    - `DatabaseIntegrationTestBase` abstract class
    - 3x faster than truncate/recreate approach
  - **HTTP Mocking with WireMock** (Issue #164) - External API mocking
    - `EncinaWireMockFixture` with helpers: `SetupOutboxWebhook()`, `SetupExternalApi()`
    - `EncinaRefitMockFixture<TClient>` for Refit clients
    - Fault simulation: `SetupFault()`, `SetupDelay()`
    - New package planned: `Encina.Testing.WireMock`
  - **Snapshot Testing with Verify** (Issue #165) - Approval testing for complex responses
    - `EncinaVerifyExtensions` for Either, ValidationResult, EncinaError
    - Custom JSON converters for Encina types
    - Data scrubbing for non-deterministic values (GUIDs, dates)
  - **Architecture Testing with ArchUnitNET** (Issue #166) - CQRS architecture rules
    - `EncinaArchitectureRules` with pre-defined rules
    - `CommandsMustNotReturnVoid()`, `QueriesMustNotModifyState()`
    - `HandlersMustNotDependOnControllers()`, `DomainMustNotDependOnInfrastructure()`
    - New package planned: `Encina.Testing.Architecture`
  - **Handler Registration Tests** (Issue #167) - Verify all handlers are registered
    - `EncinaRegistrationAssertions.AllRequestsShouldHaveHandlers(assembly)`
    - `EncinaRegistrationAssertions.AllNotificationsShouldHaveHandlers(assembly)`
    - `RegistrationVerifier` fluent API
    - Early detection of missing handler registrations
  - **Pipeline Testing Utilities** (Issue #168) - Control behaviors in tests
    - `PipelineTestContext<TRequest, TResponse>` for pipeline testing
    - `WithBehavior<T>()`, `WithoutBehavior<T>()`, `WithMockedHandler()` methods
    - `VerifyBehaviorCalled<T>(Times)` verification
    - `PipelineTest.For<TRequest, TResponse>()` factory
  - **Messaging Pattern Helpers** (Issue #169) - Helpers for Outbox, Inbox, Saga, Scheduling
    - `OutboxTestHelper`: `CaptureMessages()`, `VerifyMessagePublished<T>()`
    - `InboxTestHelper`: `SimulateIdempotentMessage()`, `VerifyProcessedOnce()`
    - `SagaTestBase<TSaga, TData>`: Given/When/Then for sagas
    - `SchedulingTestHelper`: `AdvanceTimeAndGetDue()`, `VerifyCronNextExecution()`
  - **Improved Assertions** (Issue #170) - Fluent assertions with chaining
    - `AndConstraint<T>` for chained assertions
    - `ShouldBeSuccess().And.ShouldSatisfy(x => ...)`
    - Streaming assertions for `IAsyncEnumerable<Either<EncinaError, T>>`
    - Error collection assertions
  - **TUnit Support** (Issue #171) - Source-generated testing framework
    - `EncinaTUnitFixture` adapted for TUnit model
    - NativeAOT compatible (aligns with Source Generators #50)
    - 10-200x faster test execution
    - New package planned: `Encina.Testing.TUnit`
  - **Mutation Testing Integration** (Issue #172) - Stryker.NET configuration
    - Pre-configured `stryker-config.json` for Encina projects
    - `scripts/run-stryker.cs` helper script
    - GitHub Actions workflow for mutation testing
    - `MutationKillerAttribute` for edge case tests
  - **CI/CD Workflow Templates** (Issue #173) - Reusable GitHub Actions
    - `encina-test.yml` - Basic unit + integration tests
    - `encina-matrix.yml` - Cross-platform, multi-database testing
    - `encina-full-ci.yml` - Complete CI with architecture + mutation tests

- New Labels Created (Testing):
  - `area-testing` - Testing utilities and frameworks
  - `testing-integration` - Integration testing utilities
  - `testing-unit` - Unit testing utilities
  - `testing-mocking` - Mocking and stubbing utilities
  - `testing-snapshot` - Snapshot and approval testing
  - `testing-data-generation` - Test data generation and fixtures
  - `area-mutation-testing` - Mutation testing and test quality
  - `area-architecture-testing` - Architecture rules and verification
  - `area-ci-cd` - CI/CD pipelines and automation
  - `area-docker` - Docker and containerization
  - `aot-compatible` - NativeAOT and trimming compatible

#### Advanced Distributed Lock Patterns Issues (20 new features planned based on December 2025 research)

- **PostgreSQL Provider** (Issue #207) - Native PostgreSQL advisory locks
    - `pg_advisory_lock(key)` for exclusive locks
    - `pg_advisory_lock_shared(key)` for shared locks
    - `pg_try_advisory_lock()` for non-blocking acquisition
    - Session-level and transaction-level lock APIs
    - No additional table required, uses PostgreSQL native locks
    - Inspired by DistributedLock (madelson) - most requested database provider
  - **MySQL Provider** (Issue #208) - GET_LOCK-based locking
    - `GET_LOCK(name, timeout)` / `RELEASE_LOCK(name)` functions
    - Session-scoped locks with timeout support
    - Lock name validation and sanitization
    - Compatible with MySQL 5.7+, MariaDB 10.0+
  - **Azure Blob Storage Provider** (Issue #209) - Cloud-native blob leases
    - Blob lease acquisition with 15-60s duration (max 60s)
    - Auto-renewal background thread
    - `IAzureBlobLockProvider` specialized interface
    - Health checks for lease monitoring
    - Inspired by Azure SDK, Medallion.Threading
  - **DynamoDB Provider** (Issue #210) - AWS-native conditional writes
    - Conditional writes with `attribute_not_exists` for lock acquisition
    - TTL-based automatic expiration
    - Heartbeat mechanism for lease extension
    - Fence tokens for consistency
    - Inspired by AWS DynamoDB Locking Client
  - **Consul Provider** (Issue #211) - HashiCorp session-based locks
    - Session-based locking with configurable TTL
    - Health check integration for automatic lock release
    - Leader election primitives
    - Watch for lock state changes
    - Inspired by HashiCorp Consul
  - **etcd Provider** (Issue #212) - Lease-based distributed coordination
    - etcd lease creation with TTL
    - Transaction-based lock acquisition
    - Watch for key changes
    - Integration with etcd v3 API
    - Inspired by etcd.io distributed lock recipe
  - **ZooKeeper Provider** (Issue #213) - Ephemeral sequential nodes
    - Ephemeral sequential node creation
    - Watch mechanism for predecessor node deletion
    - Automatic lock release on session disconnect
    - Lock fairness through sequence ordering
    - Inspired by Apache ZooKeeper recipes
  - **Oracle Provider** (Issue #214) - DBMS_LOCK package
    - `DBMS_LOCK.REQUEST()` / `DBMS_LOCK.RELEASE()` procedures
    - Multiple lock modes (Shared, Exclusive)
    - Lock handle allocation and management
    - Compatible with Oracle 12c+
    - Inspired by DistributedLock (madelson)
  - **Distributed Semaphores** (Issue #215) - Counting locks for N-concurrent access
    - `IDistributedSemaphore` interface with count-based acquisition
    - `TryAcquireAsync(resource, maxCount, expiry)` method
    - `GetAvailableCountAsync(resource)` for monitoring
    - Use cases: rate limiting, connection pooling, resource throttling
    - Implementations: Redis (Lua scripts), PostgreSQL, SQL Server
    - Inspired by DistributedLock (madelson) semaphore support
  - **Leader Election** (Issue #216) - Cluster-wide leader selection
    - `ILeaderElectionProvider` interface for leader management
    - `AcquireLeadershipAsync()`, `IsLeaderAsync()` methods
    - `WatchLeadershipAsync()` for change notifications via `IAsyncEnumerable`
    - Automatic lease renewal
    - Use cases: singleton services, scheduled job coordination
    - Inspired by Consul, etcd, Kubernetes leader election
  - **Read/Write Locks** (Issue #217) - Multiple readers, exclusive writer
    - `IDistributedReadWriteLockProvider` interface
    - Shared read locks (multiple concurrent readers)
    - Exclusive write locks (single writer, no readers)
    - Upgrade/downgrade support where possible
    - Inspired by PostgreSQL `pg_advisory_lock_shared`, ReaderWriterLockSlim
  - **Fencing Tokens** (Issue #218) - Split-brain prevention
    - Monotonically increasing token with each lock acquisition
    - Storage rejects operations with stale tokens
    - Prevents processing with expired locks
    - Inspired by Martin Kleppmann "Designing Data-Intensive Applications"
  - **Multi-Resource Locks** (Issue #219) - Atomic multi-lock acquisition
    - Acquire multiple resources atomically
    - Deadlock prevention via consistent ordering
    - All-or-nothing semantics
    - Inspired by Two-Phase Locking protocol
  - **DistributedLockPipelineBehavior** (Issue #220) - Declarative handler locking
    - `[DistributedLock("{request.EntityId}", ExpirySeconds = 30)]` attribute
    - Key template support with property placeholders
    - Configurable expiry, retry, and wait timeouts
    - Automatic lock acquisition/release around handler
    - Integration with existing pipeline infrastructure
  - **LeaderElectionPipelineBehavior** (Issue #221) - Leader-only handler execution
    - `[RequiresLeadership("scheduler-leader")]` attribute
    - Handler only executes on current leader node
    - Non-leaders receive predefined fallback response
    - Automatic leadership monitoring
    - Use cases: singleton scheduled jobs, exclusive processors
  - **OpenTelemetry Integration for Locks** (Issue #222) - Metrics and traces
    - Metrics: `encina.lock.acquired`, `encina.lock.released`, `encina.lock.wait_time`, `encina.lock.contention`
    - Traces: Spans for lock acquisition, hold duration, release
    - Tags: lock type, resource name, outcome (acquired/timeout/failed)
    - Integration with existing Encina.OpenTelemetry infrastructure
  - **Auto-extend Locks** (Issue #223) - Automatic lease extension
    - Background renewal for long-running operations
    - Configurable extension interval (e.g., renew at 50% of expiry)
    - Graceful handling of extension failures
    - Prevents accidental lock expiration during processing
  - **Lock Metadata** (Issue #224) - Lock holder information
    - `GetLockInfoAsync(resource)` returning holder identity, acquisition time
    - Useful for debugging and operations
    - Optional: machine name, process ID, correlation ID
    - Read-only, does not affect lock semantics
  - **Lock Queuing & Fairness** (Issue #225) - FIFO ordering for waiters
    - Fair ordering: first waiter acquires lock first
    - Prevents starvation of long-waiting requesters
    - Optional: priority-based queuing
    - Implementations vary by backend capabilities
  - **RedLock Algorithm** (Issue #226) - High-availability multi-Redis locking
    - Consensus across N/2+1 Redis instances for lock acquisition
    - Clock drift compensation
    - Automatic retry with jitter on partial acquisition
    - `IRedLockProvider` as wrapper around `IDistributedLockProvider`
    - Inspired by Redis RedLock specification, RedLock.net

- New Labels Created (Distributed Lock - December 2025):
  - `area-distributed-lock` - Distributed locking patterns
  - `area-leader-election` - Leader election and coordination
  - `area-semaphore` - Distributed semaphores and counting locks
  - `area-coordination` - Distributed coordination primitives
  - `area-pipeline` - Pipeline behaviors and middleware

#### Message Transport Patterns Issues (29 new features planned based on December 2025 research)

- **New Message Transports (6 issues)**:
    - **Google Cloud Pub/Sub Transport** (Issue #237) - Native GCP integration
      - `IMessageTransportPubSub` interface with dead-lettering and ordering keys
      - Exactly-once delivery (Preview feature), flow control
      - Schema validation, message filtering, BigQuery subscriptions
      - New package planned: `Encina.Transport.GooglePubSub`
    - **AWS EventBridge Transport** (Issue #238) - Event-driven AWS integration
      - Event bus publishing with partner/custom buses
      - Content-based filtering with event patterns
      - Archive and replay, cross-account delivery
      - Schema discovery integration
      - New package planned: `Encina.Transport.EventBridge`
    - **Apache Pulsar Transport** (Issue #239) - Multi-tenant messaging
      - Exclusive, Shared, Failover, Key_Shared subscription types
      - Topic compaction, tiered storage, geo-replication
      - Schema registry with Avro/Protobuf/JSON
      - Pulsar Functions integration for stream processing
      - New package planned: `Encina.Transport.Pulsar`
    - **Redis Streams Transport** (Issue #240) - Redis-native streaming
      - `XADD`/`XREAD`/`XREADGROUP` command integration
      - Consumer groups with automatic rebalancing
      - Stream trimming (`MAXLEN`, `MINID`)
      - Pending Entry List (PEL) management, message acknowledgment
      - New package planned: `Encina.Transport.RedisStreams`
    - **Apache ActiveMQ Artemis Transport** (Issue #241) - Enterprise JMS-compatible broker
      - AMQP 1.0 and CORE protocol support
      - Scheduled messages, last-value queues, ring queues
      - Message grouping, large message support
      - Divert and bridge configurations
      - New package planned: `Encina.Transport.ActiveMQ`
    - **Dapr Transport** (Issue #242) - Cloud-agnostic pub/sub abstraction
      - Component-based pub/sub (40+ broker implementations)
      - CloudEvents format native support
      - Bulk publish, per-message metadata
      - Subscriber routing rules
      - New package planned: `Encina.Transport.Dapr`
  - **Enterprise Integration Patterns (9 issues)**:
    - **Message Translator** (Issue #243) - Transform message formats between systems
      - `IMessageTranslator<TFrom, TTo>` interface
      - Bidirectional translation support
      - AutoMapper and Mapster integration
    - **Content Enricher** (Issue #244) - Augment messages with external data
      - `IContentEnricher<TMessage>` interface
      - Async enrichment from external services
      - Caching support for enrichment data
    - **Splitter Pattern** (Issue #245) - Break composite messages into parts
      - `IMessageSplitter<TComposite, TPart>` interface
      - Correlation ID propagation
      - Sequential and parallel splitting
    - **Aggregator Pattern** (Issue #246) - Combine related messages
      - `IMessageAggregator<TPart, TResult>` interface
      - Time-based and count-based completion conditions
      - Correlation strategies (CorrelationId, custom keys)
    - **Claim Check Pattern** (Issue #247) - Large message handling
      - `IClaimCheckStore` interface for payload storage
      - Azure Blob, S3, local filesystem providers
      - Automatic check-in/check-out with message metadata
    - **Async Request-Reply** (Issue #248) - Correlation-based responses
      - `IAsyncRequestReply<TRequest, TResponse>` interface
      - Reply-to queue management
      - Timeout handling with continuation tokens
    - **Competing Consumers** (Issue #249) - Parallel message processing
      - `ICompetingConsumerPool` interface
      - Dynamic scaling based on queue depth
      - Affinity and stickiness options
    - **Message Filter** (Issue #250) - Route messages by content
      - `IMessageFilter<TMessage>` interface
      - Predicate-based filtering
      - Dead-letter routing for filtered messages
    - **Priority Queue** (Issue #251) - Priority-based message delivery
      - `IMessagePriority` interface for priority assignment
      - Multiple priority levels configuration
      - Fair scheduling to prevent starvation
  - **Advanced Transport Features (8 issues)**:
    - **Message Batching** (Issue #252) - Efficient bulk operations
      - `BatchPublisher<TMessage>` with configurable batch size/timeout
      - Async flush with backpressure
      - Per-transport batch optimization
    - **Native Delayed Delivery** (Issue #253) - Broker-native scheduling
      - `IDelayedDeliveryTransport` interface
      - `DelayUntil(DateTimeOffset)`, `DelayFor(TimeSpan)` methods
      - Fallback to Encina.Scheduling when not supported
    - **Message Deduplication** (Issue #254) - Transport-level idempotency
      - `IDeduplicationStrategy` interface
      - Content-hash, Message-ID, custom key strategies
      - Configurable deduplication window
    - **Partitioning** (Issue #255) - Ordered message delivery
      - `IPartitionKeyProvider<TMessage>` interface
      - Consistent hashing for partition assignment
      - Partition affinity for stateful consumers
    - **Consumer Groups** (Issue #256) - Coordinated consumption
      - `IConsumerGroup` interface
      - Automatic partition assignment and rebalancing
      - Offset tracking and commit strategies
    - **Bidirectional Streaming** (Issue #257) - gRPC streaming support
      - `IStreamingTransport<TRequest, TResponse>` interface
      - Client and server streaming modes
      - Flow control and backpressure
    - **Message Compression** (Issue #258) - Payload compression
      - `IMessageCompressor` interface
      - Gzip, Brotli, LZ4, Snappy algorithms
      - Content-encoding negotiation
    - **Schema Registry Integration** (Issue #259) - Schema evolution
      - `ISchemaRegistry` interface
      - Confluent Schema Registry, AWS Glue support
      - Compatibility checks (BACKWARD, FORWARD, FULL)
  - **Transport Interoperability (3 issues)**:
    - **CloudEvents Format Support** (Issue #260) - CNCF standard events
      - `ICloudEventsSerializer` interface
      - Structured and binary content modes
      - Extension attributes support
    - **NServiceBus Interoperability** (Issue #261) - Bridge to NServiceBus
      - Message format translation
      - Header mapping (NServiceBus ↔ Encina)
      - Gateway pattern for gradual migration
    - **MassTransit Interoperability** (Issue #262) - Bridge to MassTransit
      - Envelope format translation
      - Consumer adapter patterns
      - Saga state migration utilities
  - **Transport Observability (3 issues)**:
    - **Transport Health Checks** (Issue #263) - Liveness and readiness probes
      - `ITransportHealthCheck` interface per transport
      - Connection state, queue depth, consumer lag
      - ASP.NET Core Health Checks integration
    - **Transport Metrics** (Issue #264) - Performance metrics
      - Messages sent/received/failed per transport
      - Latency histograms (P50, P95, P99)
      - OpenTelemetry Metrics integration
    - **Transport Distributed Tracing** (Issue #265) - End-to-end tracing
      - W3C Trace Context propagation
      - Span creation for publish/consume operations
      - Baggage propagation for cross-service context

- New Labels Created (Message Transport - December 2025):
  - `transport-rabbitmq` - RabbitMQ transport provider
  - `transport-kafka` - Apache Kafka transport provider
  - `transport-azure-sb` - Azure Service Bus transport provider
  - `transport-sqs` - AWS SQS transport provider
  - `transport-redis` - Redis transport provider
  - `transport-nats` - NATS transport provider
  - `transport-pulsar` - Apache Pulsar transport provider
  - `transport-grpc` - gRPC transport provider
  - `transport-dapr` - Dapr transport provider
  - `transport-eventbridge` - AWS EventBridge transport provider
  - `transport-pubsub` - Google Cloud Pub/Sub transport provider
  - `transport-activemq` - Apache ActiveMQ Artemis transport provider

- New Labels Created (Previously):
  - `area-scheduling` - Scheduling and recurring message patterns
  - `area-saga` - Saga and Process Manager patterns
  - `area-encryption` - Message encryption and data protection
  - `area-scalability` - Horizontal scaling and consumer patterns
  - `area-polly` - Polly v8 integration and resilience strategies
  - `netflix-pattern` - Patterns inspired by Netflix OSS
  - `industry-best-practice` - Industry-proven patterns from major tech companies
  - `meta-pattern` - Patterns inspired by Meta/Facebook infrastructure (FOQS, etc.)

- CLI Scaffolding Tool (`Encina.Cli`) - Issue #47:
  - `encina new <template> <name>` - Create new Encina projects (api, worker, console)
    - Options: `--database`, `--caching`, `--transport`, `--output`, `--force`
  - `encina generate handler <name>` - Generate command handlers with optional response types
  - `encina generate query <name> --response <type>` - Generate query handlers
  - `encina generate saga <name> --steps <steps>` - Generate saga definitions
  - `encina generate notification <name>` - Generate notifications and handlers
  - `encina add caching|database|transport|validation|resilience|observability` - Add packages
  - Built with System.CommandLine 2.0 and Spectre.Console
  - Packaged as .NET global tool (`dotnet tool install Encina.Cli`)
  - Comprehensive test coverage: 65 tests (unit, guard)

#### Clean Architecture Patterns Issues (2 new features planned based on December 29, 2025 research)

- **Result Pattern Extensions** (Issue #468) - Fluent API for Either
    - `EitherCombineExtensions`: `Combine<T1, T2>()`, `Combine<T1, T2, T3>()` for combining multiple results
    - `EitherAccumulateExtensions`: Error accumulation instead of fail-fast
    - `EitherAsyncExtensions`: `BindAsync()`, `MapAsync()`, `TapAsync()` for async chains
    - `EitherHttpExtensions`: `ToProblemDetails()`, `ToActionResult()`, `ToResult()` for Minimal APIs
    - `EitherConditionalExtensions`: `When()`, `Ensure()`, `OrElse()` for conditional operations
    - Inspired by FluentResults, language-ext, CSharpFunctionalExtensions
    - Priority: MEDIUM - Improves ROP ergonomics
  - **Partitioned Sequential Messaging** (Issue #469) - Wolverine 5.0-inspired pattern
    - `IPartitionedMessage` interface with `PartitionKey`
    - Specialized interfaces: `ISagaPartitionedMessage`, `ITenantPartitionedMessage`, `IAggregatePartitionedMessage`
    - `IPartitionedQueueManager` with System.Threading.Channels
    - `PartitionedMessageBehavior` for pipeline integration
    - `IPartitionStore` for optional durability
    - Messages with same PartitionKey process sequentially; different partitions in parallel
    - Priority: MEDIUM - Critical for saga workflows and multi-tenancy

- New labels created for Clean Architecture Patterns:
  - `area-value-objects` - Value Objects and domain primitives (#2E8B57)
  - `area-strongly-typed-ids` - Strongly Typed IDs and identity patterns (#2E8B57)
  - `area-specification-pattern` - Specification pattern for queries (#2E8B57)
  - `area-domain-services` - Domain Services abstraction (#2E8B57)
  - `area-result-pattern` - Result/Either pattern and functional error handling (#9932CC)
  - `area-bounded-context` - Bounded Context and module boundaries (#2E8B57)

### Removed

- **API Versioning Helpers** (Issue #54) - Closed as "won't fix". `Asp.Versioning` provides complete HTTP-level versioning; adding `[ApiVersion]` to CQRS handlers would be redundant since versioning belongs on the public API surface (controllers/endpoints), not internal handlers.

### Deferred

- **ODBC Provider** (Issue #56) - Moved to post-1.0 evaluation. Valuable for legacy database scenarios but not critical for core 1.0 release.

### Added (Patterns & Infrastructure)

- Scatter-Gather Pattern (Issue #63):
  - Enterprise Integration Pattern for sending requests to multiple handlers and aggregating results
  - `IScatterGatherRunner` interface with `ExecuteAsync` method returning `Either<EncinaError, ScatterGatherResult<T>>`
  - `ScatterGatherBuilder` fluent API for defining scatter-gather operations:
    - `ScatterTo(name, handler)` for adding scatter handlers with multiple overloads (sync/async, with/without Either)
    - `WithPriority(int)` for handler ordering (lower = higher priority)
    - `WithMetadata(key, value)` for handler metadata
    - `ExecuteInParallel(maxDegreeOfParallelism?)` / `ExecuteSequentially()` for execution mode
    - `WithTimeout(TimeSpan)` for operation timeout
  - Four gather strategies via `GatherStrategy` enum:
    - `WaitForAll` - Wait for all handlers to complete (fail on any failure)
    - `WaitForFirst` - Return on first successful response
    - `WaitForQuorum` - Return when quorum count is reached
    - `WaitForAllAllowPartial` - Wait for all, tolerate partial failures
  - Gather configuration via `GatherBuilder`:
    - `GatherAll()` / `GatherFirst()` / `GatherQuorum(count)` / `GatherAllAllowingPartialFailures()`
    - `GatherWith(GatherStrategy, quorumCount?)` for explicit strategy
    - Aggregation methods: `TakeFirst()`, `TakeMin()`, `TakeMax()`, `Aggregate()`, `AggregateSuccessful()`
  - `ScatterGatherResult<TResponse>` with execution metrics:
    - `Response` - Aggregated result
    - `ScatterResults` - List of `ScatterExecutionResult<TResponse>` with handler name, result, duration
    - `SuccessCount`, `FailureCount`, `OperationId`, `TotalDuration`
  - `ScatterGatherOptions` configuration:
    - `DefaultTimeout` (default: 30s)
    - `ExecuteScattersInParallel` (default: true)
    - `MaxDegreeOfParallelism` (default: null/unlimited)
  - `ScatterGatherErrorCodes` for standardized error codes:
    - `scattergather.cancelled`, `scattergather.timed_out`
    - `scattergather.all_scatters_failed`, `scattergather.quorum_not_reached`
    - `scattergather.gather_failed`, `scattergather.scatter_failed`
  - High-performance logging with `LoggerMessage` source generators (EventIds 600-615)
  - DI integration via `MessagingConfiguration.UseScatterGather = true`
  - Comprehensive test coverage: 131 tests (unit, property, contract, guard, load)
  - Benchmarks for scatter-gather performance across strategies
  - Example: a fully working, copy-pastable scatter-gather example is available in
    [docs/examples.md](docs/examples.md). The changelog previously contained
    simplified placeholder snippets; the full examples (with imports and helper
    implementations) live in the documentation to avoid non-compilable copy/paste.

- Content-Based Router (Issue #64):
  - Enterprise Integration Pattern for routing messages based on content inspection
  - `IContentRouter` interface with `RouteAsync` methods returning `Either<EncinaError, ContentRouterResult<T>>`
  - `ContentRouterBuilder` fluent API for defining routing rules:
    - `When(condition)` / `When(name, condition)` for conditional routes
    - `RouteTo(handler)` with multiple overloads (sync/async, with/without Either)
    - `WithPriority(int)` for route ordering (lower = higher priority)
    - `WithMetadata(key, value)` for route metadata
    - `Default(handler)` / `DefaultResult(value)` for fallback handling
    - `Build()` to create immutable `BuiltContentRouterDefinition<TMessage, TResult>`
  - `ContentRouterOptions` configuration:
    - `ThrowOnNoMatch` - Return error when no route matches (default: true)
    - `AllowMultipleMatches` - Execute all matching routes (default: false)
    - `EvaluateInParallel` - Parallel route execution with `MaxDegreeOfParallelism`
  - `ContentRouterResult<TResult>` with execution metrics:
    - `RouteResults` - List of `RouteExecutionResult<TResult>` with route name, result, duration
    - `MatchedRouteCount`, `TotalDuration`, `UsedDefaultRoute`
  - `RouteDefinition<TMessage, TResult>` for route configuration
  - `ContentRouterErrorCodes` for standardized error codes:
    - `contentrouter.no_matching_route`, `contentrouter.route_execution_failed`
    - `contentrouter.cancelled`, `contentrouter.invalid_configuration`
  - High-performance logging with `LoggerMessage` source generators
  - DI integration via `MessagingConfiguration.UseContentRouter = true`
  - Comprehensive test coverage: 117 tests (unit, integration, property, contract, guard, load)
  - Benchmarks for routing performance
  - Example:

    ```csharp
    // RouteTo accepts both sync and async handlers
    var definition = ContentRouterBuilder.Create<Order, string>()
        .When("HighValue", o => o.Total > 10000)
            .WithPriority(1)
            .RouteTo(async (o, ct) => await ProcessHighValueOrder(o, ct))
        .When("International", o => o.IsInternational)
            .WithPriority(2)
            .RouteTo(async (o, ct) => await Task.FromResult(Right<EncinaError, string>("InternationalHandler")))
        .Default(async (o, ct) => await Task.FromResult(Right<EncinaError, string>("StandardHandler")))
        .Build();

    var result = await router.RouteAsync(definition, order);
    ```

- Distributed Lock Abstractions (Issue #55):
  - **Encina.DistributedLock** - Core abstractions for distributed locking
    - `IDistributedLockProvider` interface with `TryAcquireAsync`, `AcquireAsync`, `IsLockedAsync`, `ExtendAsync`
    - `ILockHandle` interface with lock metadata (`Resource`, `LockId`, `AcquiredAtUtc`, `ExpiresAtUtc`, `IsReleased`)
    - `LockAcquisitionException` for lock acquisition failures
    - `DistributedLockOptions` with `KeyPrefix`, `DefaultExpiry`, `DefaultWait`, `DefaultRetry`, `ProviderHealthCheck`
    - DI registration via `AddEncinaDistributedLock()`
  - **Encina.DistributedLock.InMemory** - In-memory provider for testing
    - `InMemoryDistributedLockProvider` with `ConcurrentDictionary` storage
    - `InMemoryLockOptions` with `WarnOnUse` flag
    - `TimeProvider` injection for testability
    - DI registration via `AddEncinaDistributedLockInMemory()`
  - **Encina.DistributedLock.Redis** - Redis provider for production
    - `RedisDistributedLockProvider` using StackExchange.Redis
    - Lua scripts for atomic lock release (owner verification)
    - Wire-compatible with Redis, Garnet, Valkey, Dragonfly, KeyDB
    - `RedisLockOptions` with `Database` and `KeyPrefix`
    - `RedisDistributedLockHealthCheck` implementing `IEncinaHealthCheck`
    - DI registration via `AddEncinaDistributedLockRedis()`
  - **Encina.DistributedLock.SqlServer** - SQL Server provider
    - `SqlServerDistributedLockProvider` using `sp_getapplock`/`sp_releaseapplock`
    - Session-scoped locks with automatic release on connection close
    - `SqlServerLockOptions` with `ConnectionString` and `KeyPrefix`
    - `SqlServerDistributedLockHealthCheck` implementing `IEncinaHealthCheck`
    - DI registration via `AddEncinaDistributedLockSqlServer()`
  - Updated `Encina.Caching` to reference `Encina.DistributedLock` abstractions
  - Comprehensive test coverage:
    - Unit tests for all providers
    - Integration tests with Testcontainers (Redis, SQL Server)
    - Property-based tests with FsCheck
    - Contract tests for `IDistributedLockProvider`
    - Guard clause tests for all public APIs
    - Load tests for high concurrency scenarios
    - Benchmarks for performance validation
  - Full documentation with usage examples

- Module-scoped Pipeline Behaviors (Issue #58):
  - **IModulePipelineBehavior<TModule, TRequest, TResponse>** interface for module-specific behaviors
  - **ModuleBehaviorAdapter** wraps module behaviors and filters execution by module ownership
  - **IModuleHandlerRegistry** maps handler types to their owning modules via assembly association
  - Request context extensions for module information:
    - `GetModuleName()` for retrieving current module name
    - `WithModuleName()` for setting module name (overloads for string and IModule)
    - `IsInModule()` for checking if context is in a specific module
  - DI extension methods:
    - `AddEncinaModuleBehavior<TModule, TRequest, TResponse, TBehavior>()` for registration
    - Overload with `ServiceLifetime` parameter for custom lifetimes
  - Case-insensitive module name matching
  - Null Object pattern with `NullModuleHandlerRegistry` for when modules aren't configured
  - Comprehensive test coverage: 100 module-related unit tests

- AWS Lambda Integration (Issue #60):
  - **Encina.AwsLambda** package for serverless function execution on AWS
  - API Gateway integration with result-to-response extensions:
    - `ToApiGatewayResponse<T>()` for standard 200 OK responses
    - `ToCreatedResponse<T>()` for 201 Created with Location header
    - `ToNoContentResponse()` for 204 No Content responses
    - `ToHttpApiResponse<T>()` for HTTP API (V2) responses
  - `ToProblemDetailsResponse()` for RFC 7807 compliant error responses
  - SQS trigger support with batch processing:
    - `ProcessBatchAsync<T>()` for partial batch failure reporting via `SQSBatchResponse`
    - `ProcessAllAsync()` for all-or-nothing processing
    - `DeserializeMessage<T>()` for type-safe message deserialization
    - Automatic `BatchItemFailures` for failed message IDs
  - EventBridge (CloudWatch Events) integration:
    - `ProcessAsync<TDetail, TResult>()` for strongly-typed event handling
    - `ProcessRawAsync<TDetail, TResult>()` for raw JSON event processing
    - `GetMetadata()` for extracting event metadata
    - `EventBridgeMetadata` class with Id, Source, DetailType, Account, Region, Time
  - `LambdaContextExtensions` for context information access:
    - `GetCorrelationId()`, `GetUserId()`, `GetTenantId()`
    - `GetAwsRequestId()`, `GetFunctionName()`, `GetRemainingTimeMs()`
  - `EncinaAwsLambdaOptions` for configuration:
    - `EnableRequestContextEnrichment` toggle
    - Customizable header names (`CorrelationIdHeader`, `TenantIdHeader`)
    - Claim types (`UserIdClaimType`, `TenantIdClaimType`)
    - `IncludeExceptionDetailsInResponse` for development debugging
    - `UseApiGatewayV2Format`, `EnableSqsBatchItemFailures` toggles
    - `ProviderHealthCheck` configuration
  - `AwsLambdaHealthCheck` implementing `IEncinaHealthCheck`
  - Error code to HTTP status mapping:
    - `validation.*` → 400 Bad Request
    - `authorization.unauthenticated` → 401 Unauthorized
    - `authorization.*` → 403 Forbidden
    - `*.not_found`, `*.missing` → 404 Not Found
    - `*.conflict`, `*.already_exists`, `*.duplicate` → 409 Conflict
  - DI registration via `AddEncinaAwsLambda()`
  - Comprehensive test coverage: 97 unit, 21 contract, 10 property, 21 guard tests
  - Benchmarks for API Gateway response creation performance
  - Full documentation with examples for API Gateway, SQS, and EventBridge triggers

- Azure Functions Integration (Issue #59):
  - **Encina.AzureFunctions** package for serverless function execution
  - HTTP Trigger integration with automatic result-to-response conversion:
    - `ToHttpResponseData<T>()` for standard responses
    - `ToCreatedResponse<T>()` for 201 Created with Location header
    - `ToNoContentResponse()` for 204 No Content responses
  - `ToProblemDetailsResponse()` for RFC 7807 compliant error responses
  - `EncinaFunctionMiddleware` for request context enrichment:
    - Automatic correlation ID extraction/generation
    - User ID extraction from claims
    - Tenant ID extraction from headers or claims
    - Structured logging for function execution
  - `FunctionContextExtensions` for context information access:
    - `GetCorrelationId()`, `GetUserId()`, `GetTenantId()`, `GetInvocationId()`
  - `EncinaAzureFunctionsOptions` for configuration:
    - `EnableRequestContextEnrichment` toggle
    - Customizable header names and claim types
    - `IncludeExceptionDetailsInResponse` for development
    - `ProviderHealthCheck` configuration
  - `AzureFunctionsHealthCheck` implementing `IEncinaHealthCheck`
  - Error code to HTTP status mapping:
    - `validation.*` → 400 Bad Request
    - `authorization.unauthenticated` → 401 Unauthorized
    - `authorization.*` → 403 Forbidden
    - `*.not_found`, `*.missing` → 404 Not Found
    - `*.conflict`, `*.already_exists`, `*.duplicate` → 409 Conflict
  - DI registration via `AddEncinaAzureFunctions()`
  - Middleware registration via `builder.UseEncinaMiddleware()`
  - Comprehensive test coverage: unit, contract, property, guard, benchmarks
  - Full documentation with examples for HTTP, Queue, and Timer triggers

- Durable Functions Integration (Issue #61):
  - Azure Durable Functions support with Railway Oriented Programming (ROP)
  - `OrchestrationContextExtensions` for ROP-compatible activity calls:
    - `CallEncinaActivityAsync<TInput, TResult>()` for Either-returning activities
    - `CallEncinaActivityWithResultAsync<TInput, TResult>()` for ActivityResult activities
    - `CallEncinaSubOrchestratorAsync<TInput, TResult>()` for sub-orchestrators
    - `WaitForEncinaExternalEventAsync<T>()` for external events with timeout
    - `CreateRetryOptions()` for retry configuration
    - `GetCorrelationId()` for instance ID access
  - `ActivityResult<T>` serializable wrapper for Either results:
    - `Success()` and `Failure()` factory methods
    - `ToEither()` for conversion back to Either
    - `ToActivityResult()` extension for Either conversion
  - `DurableSagaBuilder` fluent API for saga workflows:
    - `Step()` for adding saga steps
    - `Execute()` and `Compensate()` for activity configuration
    - `WithRetry()` for step-level retry options
    - `SkipCompensationOnFailure()` for idempotent operations
    - `WithTimeout()` for saga-level timeout
    - `WithDefaultRetryOptions()` for default retry configuration
    - Automatic compensation in reverse order on failure
  - `DurableSaga<TData>` executable saga with `ExecuteAsync()`
  - `DurableSagaError` with original error and compensation results
  - Fan-out/fan-in pattern extensions:
    - `FanOutAsync<TInput, TResult>()` for parallel activity execution
    - `FanOutAllAsync<TInput, TResult>()` requiring all to succeed
    - `FanOutFirstSuccessAsync<TInput, TResult>()` returning first success
    - `FanOutMultipleAsync<T1, T2>()` for different activities in parallel
    - `Partition<T>()` for separating successes from failures
  - `DurableFunctionsOptions` for configuration:
    - `DefaultMaxRetries`, `DefaultFirstRetryInterval`, `DefaultBackoffCoefficient`
    - `DefaultMaxRetryInterval`, `ContinueCompensationOnError`, `DefaultSagaTimeout`
    - `ProviderHealthCheck` configuration
  - `DurableFunctionsHealthCheck` implementing `IEncinaHealthCheck`
  - DI registration via `AddEncinaDurableFunctions()`
  - Comprehensive test coverage: 124 unit, 58 contract, 27 property, 19 guard tests
  - Full documentation with examples for orchestrations, sagas, and fan-out/fan-in

- Routing Slip Pattern for Dynamic Message Routing (Issue #62):
  - `RoutingSlipBuilder` fluent API for defining routing slips with inline step definitions
  - `RoutingSlipStepBuilder` for configuring individual steps with execute and compensate functions
  - `BuiltRoutingSlipDefinition<TData>` immutable definition ready for execution
  - `RoutingSlipStepDefinition<TData>` representing a single step in the itinerary
  - `RoutingSlipContext<TData>` for dynamic route modification during execution:
    - `AddStep()`, `AddStepNext()`, `InsertStep()` for adding steps
    - `RemoveStepAt()`, `ClearRemainingSteps()` for removing steps
    - `GetRemainingStepNames()` for inspecting itinerary
  - `RoutingSlipActivityEntry<TData>` for activity log with compensation data
  - `RoutingSlipResult<TData>` with execution metrics:
    - `RoutingSlipId`, `FinalData`, `StepsExecuted`, `StepsAdded`, `StepsRemoved`
    - `Duration`, `ActivityLog`
  - `IRoutingSlipRunner` interface and `RoutingSlipRunner` implementation with:
    - Step-by-step execution with dynamic modification tracking
    - Automatic compensation in reverse order on failure
    - Configurable compensation failure handling
    - High-performance logging with LoggerMessage (EventIds 400-415)
  - `RoutingSlipOptions` for configuration:
    - `DefaultTimeout`, `StuckCheckInterval`, `StuckThreshold`, `BatchSize`
    - `ContinueCompensationOnFailure` (default: true)
  - `RoutingSlipStatus` constants: Running, Completed, Compensating, Compensated, Failed, TimedOut
  - `RoutingSlipErrorCodes` with error codes:
    - `routingslip.not_found`, `routingslip.invalid_status`, `routingslip.step_failed`
    - `routingslip.compensation_failed`, `routingslip.timeout`
    - `routingslip.handler.cancelled`, `routingslip.handler.failed`
  - DI integration via `MessagingConfiguration.UseRoutingSlips`
  - Comprehensive test coverage: 137+ tests (unit, property, guard)
  - Example:

    ```csharp
    var definition = RoutingSlipBuilder.Create<OrderData>("ProcessOrder")
        .Step("Validate Order")
            .Execute(async (data, ctx, ct) => {
                // Validation logic
                return Right<EncinaError, OrderData>(data);
            })
        .Step("Process Payment")
            .Execute(async (data, ctx, ct) => {
                // Dynamically add verification step if needed
                if (data.RequiresVerification)
                    ctx.AddStepNext(verificationStep);
                return Right<EncinaError, OrderData>(data);
            })
            .Compensate(async (data, ctx, ct) => await RefundPaymentAsync(data))
        .Step("Ship Order")
            .Execute(async (data, ctx, ct) => {
                data.TrackingNumber = await ShipAsync(data);
                return Right<EncinaError, OrderData>(data);
            })
            .Compensate(async (data, ctx, ct) => await CancelShipmentAsync(data))
        .OnCompletion(async (data, ctx, ct) => await NotifyCompletedAsync(data))
        .WithTimeout(TimeSpan.FromMinutes(5))
        .Build();

    var result = await runner.RunAsync(definition, new OrderData());
    ```

- Event Versioning/Upcasting for Schema Evolution (Issue #37):
  - `IEventUpcaster` marker interface for event upcasters with `SourceEventTypeName`, `TargetEventType`, `SourceEventType`
  - `IEventUpcaster<TFrom, TTo>` strongly-typed generic interface with `Upcast(TFrom)` method
  - `EventUpcasterBase<TFrom, TTo>` abstract base class wrapping Marten's `EventUpcaster<TFrom, TTo>`
  - `LambdaEventUpcaster<TFrom, TTo>` for inline lambda-based upcasting without dedicated classes
  - `EventUpcasterRegistry` for discovering and managing event upcasters:
    - `Register<TUpcaster>()`, `Register(Type)`, `Register(IEventUpcaster)` registration methods
    - `TryRegister()` for non-throwing duplicate handling
    - `GetUpcasterForEventType(string)`, `GetAllUpcasters()`, `HasUpcasterFor(string)` lookup methods
    - `ScanAndRegister(Assembly)` for automatic assembly scanning
  - `EventVersioningOptions` for configuration:
    - `Enabled` toggle (default: false)
    - `ThrowOnUpcastFailure` option (default: true)
    - `AddUpcaster<TUpcaster>()` for type registration
    - `AddUpcaster<TFrom, TTo>(Func<TFrom, TTo>)` for inline lambda registration
    - `ScanAssembly(Assembly)`, `ScanAssemblies(params Assembly[])` for assembly scanning
    - `ApplyTo(EventUpcasterRegistry)` for applying configuration to registry
  - `ConfigureMartenEventVersioning` as `IConfigureOptions<StoreOptions>` for Marten integration
  - `EventVersioningErrorCodes` with error codes:
    - `event.versioning.upcast_failed`, `event.versioning.upcaster_not_found`
    - `event.versioning.registration_failed`, `event.versioning.duplicate_upcaster`
    - `event.versioning.invalid_configuration`
  - `VersioningLog` high-performance logging with LoggerMessage source generators (EventIds 100-129)
  - DI integration via `AddEventVersioning()` internal method
  - `AddEventUpcaster<TUpcaster>()` extension method for individual upcaster registration
  - Comprehensive test coverage: 85+ tests (unit, property, contract, guard, integration)
  - Example:

    ```csharp
    // Define upcaster class
    public class OrderCreatedV1ToV2Upcaster : EventUpcasterBase<OrderCreatedV1, OrderCreatedV2>
    {
        protected override OrderCreatedV2 Upcast(OrderCreatedV1 old)
            => new(old.OrderId, old.CustomerName, Email: "unknown@example.com");
    }

    // Configure with class-based upcasters
    services.AddEncinaMarten(options =>
    {
        options.EventVersioning.Enabled = true;
        options.EventVersioning.AddUpcaster<OrderCreatedV1ToV2Upcaster>();
        options.EventVersioning.ScanAssembly(typeof(Program).Assembly);
    });

    // Or use inline lambda for simple transformations
    services.AddEncinaMarten(options =>
    {
        options.EventVersioning.Enabled = true;
        options.EventVersioning.AddUpcaster<OrderCreatedV1, OrderCreatedV2>(
            old => new OrderCreatedV2(old.OrderId, old.CustomerName, "migrated@example.com"));
    });
    ```

- Snapshotting for large aggregates (Issue #52):
  - `ISnapshotable<TAggregate>` marker interface for aggregates supporting snapshots
  - `ISnapshot<TAggregate>` interface with `AggregateId`, `Version`, `CreatedAtUtc` properties
  - `Snapshot<TAggregate>` sealed class storing aggregate state at a specific version
  - `SnapshotOptions` for global and per-aggregate configuration:
    - `Enabled` - toggle snapshotting (default: false)
    - `SnapshotEvery` - event threshold for snapshot creation (default: 100)
    - `KeepSnapshots` - retention limit (default: 3, 0 = keep all)
    - `AsyncSnapshotCreation` - async vs sync snapshot creation (default: true)
    - `ConfigureAggregate<T>(snapshotEvery, keepSnapshots)` for per-aggregate overrides
  - `ISnapshotStore<TAggregate>` interface for snapshot storage:
    - `SaveAsync`, `GetLatestAsync`, `PruneAsync` with ROP error handling
  - `MartenSnapshotStore<TAggregate>` Marten-based implementation:
    - PostgreSQL document storage via Marten
    - Composite key: `{AggregateType}:{AggregateId}:{Version}`
    - Automatic pruning of old snapshots
  - `SnapshotEnvelope<TAggregate>` document wrapper for Marten storage
  - `SnapshotAwareAggregateRepository<TAggregate>` for optimized aggregate loading:
    - Loads from latest snapshot + replays only subsequent events
    - Automatic snapshot creation when threshold exceeded
    - Falls back to standard event replay if no snapshot exists
  - `SnapshotErrorCodes` with standardized error codes:
    - `snapshot.load_failed`, `snapshot.save_failed`, `snapshot.prune_failed`, `snapshot.invalid_state`
  - High-performance logging with `LoggerMessage` source generators (EventIds 100-159)
  - DI registration via `AddSnapshotableAggregate<TAggregate>()`
  - Comprehensive test coverage: 121 unit tests, property tests, contract tests, guard clause tests
  - Integration tests with Testcontainers/PostgreSQL
  - Example:

    ```csharp
    // Enable snapshotting for aggregates
    services.AddEncinaMarten(options =>
    {
        options.Snapshots.Enabled = true;
        options.Snapshots.SnapshotEvery = 100;
        options.Snapshots.KeepSnapshots = 3;

        // Per-aggregate configuration
        options.Snapshots.ConfigureAggregate<Order>(
            snapshotEvery: 50,
            keepSnapshots: 5);
    });

    // Register snapshotable aggregate
    services.AddSnapshotableAggregate<Order>();

    // Aggregate must implement ISnapshotable<TAggregate>
    public class Order : AggregateBase, ISnapshotable<Order>
    {
        // ... standard aggregate implementation
    }
    ```

- Projections/Read Models for CQRS read side (Issue #36):
  - `IReadModel` interface for read model abstraction with `Guid Id` property
  - `IReadModel<TId>` generic variant for strongly-typed identifiers
  - `IProjection<TReadModel>` interface with `ProjectionName` property
  - `IProjectionHandler<TEvent, TReadModel>` for handling events on existing read models:
    - `Apply(TEvent, TReadModel, ProjectionContext)` method
  - `IProjectionCreator<TEvent, TReadModel>` for creating read models from events:
    - `Create(TEvent, ProjectionContext)` method
  - `IProjectionDeleter<TEvent, TReadModel>` for conditional deletion:
    - `ShouldDelete(TEvent, TReadModel, ProjectionContext)` method
  - `ProjectionContext` with event metadata:
    - `StreamId`, `SequenceNumber`, `GlobalPosition`, `Timestamp`
    - `EventType`, `CorrelationId`, `CausationId`, `Metadata`
  - `IReadModelRepository<TReadModel>` for read model persistence:
    - `GetByIdAsync`, `GetByIdsAsync`, `QueryAsync`
    - `StoreAsync`, `StoreManyAsync`, `DeleteAsync`, `DeleteAllAsync`
    - `ExistsAsync`, `CountAsync`
    - ROP-based error handling with `Either<EncinaError, T>`
  - `IProjectionManager` for projection lifecycle management:
    - `RebuildAsync<TReadModel>` with configurable options
    - `GetStatusAsync`, `GetAllStatusesAsync` for monitoring
    - `StartAsync`, `StopAsync`, `PauseAsync`, `ResumeAsync` lifecycle methods
  - `RebuildOptions` for rebuild configuration:
    - `BatchSize` (default 1000), `DeleteExisting`, `OnProgress` callback
    - `StartPosition`, `EndPosition` for incremental rebuilds
    - `RunInBackground` for async rebuilding
  - `ProjectionStatus` with state tracking:
    - `State` enum: `Stopped`, `Starting`, `Running`, `CatchingUp`, `Rebuilding`, `Paused`, `Faulted`, `Stopping`
    - `LastProcessedPosition`, `EventsProcessed`, `LastUpdatedAtUtc`, `ErrorMessage`
  - `ProjectionRegistry` for projection registration and discovery:
    - `Register<TProjection, TReadModel>()` method
    - `GetProjectionsForEvent(Type)`, `GetProjectionForReadModel<T>()`, `GetAllProjections()`
  - `IInlineProjectionDispatcher` for synchronous projection updates:
    - `DispatchAsync(object, ProjectionContext)` for single event
    - `DispatchManyAsync(IEnumerable<(object, ProjectionContext)>)` for batch
  - Marten implementations:
    - `MartenReadModelRepository<TReadModel>` with Marten IDocumentSession
    - `MartenProjectionManager` with event stream processing
    - `MartenInlineProjectionDispatcher` for inline projection updates
  - `ProjectionOptions` for configuration:
    - `EnableInlineProjections`, `RebuildOnStartup`
    - `DefaultBatchSize`, `MaxConcurrentRebuilds`
    - `OnProjectionFaulted` callback
  - `IProjectionRegistrar` interface for startup registration
  - DI registration via `AddProjection<TProjection, TReadModel>()`
  - High-performance logging with `LoggerMessage` attributes
  - 80 tests: 30 unit, 22 property-based, 11 contract, 17 guard clause
  - **Note**: Example uses target-typed `new()` and `with` expression (requires read model to be a `record` type). See [Language Requirements](#language-requirements).
  - **Context-to-Model Mapping**: `ctx.StreamId` is a `Guid` (non-nullable struct, defaults to `Guid.Empty`). This maps directly to `IReadModel.Id` to correlate read models with their source aggregates. Guard against `Guid.Empty` if your domain requires a valid stream ID.
  - **Null Handling**: Event properties (e.g., `e.CustomerName`) may be null depending on your domain model. The `Create` method should validate required fields with `ArgumentNullException.ThrowIfNull()` or use null-coalescing for optional fields. The example below demonstrates defensive validation.
  - Example:

    ```csharp
    // Define a read model (must be a record to use 'with' expression)
    public record OrderSummary : IReadModel
    {
        public Guid Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
    }

    // Define a projection with input validation
    public class OrderSummaryProjection :
        IProjection<OrderSummary>,
        IProjectionCreator<OrderCreated, OrderSummary>,
        IProjectionHandler<OrderItemAdded, OrderSummary>
    {
        public string ProjectionName => "OrderSummary";

        public OrderSummary Create(OrderCreated e, ProjectionContext ctx)
        {
            // StreamId is a non-nullable Guid; validate against Guid.Empty only if your domain requires
            // Guid.Empty typically indicates "no meaningful identifier" - domain logic decides if this is valid
            if (ctx.StreamId == Guid.Empty)
                throw new ArgumentException("StreamId must represent a valid aggregate identifier", nameof(ctx));

            // Validate required event properties
            ArgumentNullException.ThrowIfNull(e.CustomerName, nameof(e.CustomerName));

            return new() { Id = ctx.StreamId, CustomerName = e.CustomerName };
        }

        public OrderSummary Apply(OrderItemAdded e, OrderSummary m, ProjectionContext ctx) =>
            m with { TotalAmount = m.TotalAmount + e.Price * e.Quantity };
    }

    // Register and use
    services.AddEncinaMarten(options => {
        options.Projections.EnableInlineProjections = true;
    }).AddProjection<OrderSummaryProjection, OrderSummary>();

    // Query read models
    var summary = await repository.GetByIdAsync(orderId);
    ```

- Bulkhead Isolation Pattern (Issue #53):
  - `BulkheadAttribute` for attribute-based bulkhead configuration:
    - `MaxConcurrency` - Maximum parallel executions allowed (default: 10)
    - `MaxQueuedActions` - Additional requests that can wait in queue (default: 20)
    - `QueueTimeoutMs` - Maximum time to wait in queue (default: 30000ms)
  - `IBulkheadManager` interface for bulkhead management:
    - `TryAcquireAsync` - Acquire permit with timeout and cancellation support
    - `GetMetrics` - Get current bulkhead metrics (concurrency, queue, rejection rate)
    - `Reset` - Reset bulkhead state for a key
  - `BulkheadManager` implementation with `SemaphoreSlim`:
    - Thread-safe concurrent dictionary for per-key bulkhead isolation
    - Automatic permit release via `IDisposable` pattern
    - `TimeProvider` injection for testability
  - `BulkheadPipelineBehavior<TRequest, TResponse>` for automatic bulkhead enforcement
  - `BulkheadAcquireResult` record struct with factory methods:
    - `Acquired()` - Successful acquisition with releaser
    - `RejectedBulkheadFull()` - Both concurrency and queue limits reached
    - `RejectedQueueTimeout()` - Queue wait timeout exceeded
    - `RejectedCancelled()` - Request cancelled while waiting
  - `BulkheadMetrics` record struct with calculated properties:
    - `ConcurrencyUtilization` - Percentage of concurrency capacity in use
    - `QueueUtilization` - Percentage of queue capacity in use
    - `RejectionRate` - Total rejection rate as percentage
  - `BulkheadRejectionReason` enum (`None`, `BulkheadFull`, `QueueTimeout`, `Cancelled`)
  - Automatic DI registration via `AddEncinaPolly()` (singleton manager)
  - Comprehensive test coverage: unit, integration, property-based, contract, guard, load tests
  - Performance benchmarks for acquire/release operations
  - Example:

    ```csharp
    // Limit payment processing to 10 concurrent executions
    [Bulkhead(MaxConcurrency = 10, MaxQueuedActions = 20)]
    public record ProcessPaymentCommand(PaymentData Data) : ICommand<PaymentResult>;

    // Limit external API calls with custom timeout
    [Bulkhead(MaxConcurrency = 5, MaxQueuedActions = 10, QueueTimeoutMs = 5000)]
    public record CallExternalApiQuery(string Endpoint) : IRequest<ApiResponse>;
    ```

- Dead Letter Queue (Issue #42):
  - `IDeadLetterMessage` interface for dead letter message abstraction
  - `IDeadLetterStore` interface for provider-agnostic storage:
    - `AddAsync`, `GetAsync`, `GetMessagesAsync`, `GetCountAsync`
    - `MarkAsReplayedAsync`, `DeleteAsync`, `DeleteExpiredAsync`
    - Pagination support with `skip` and `take` parameters
  - `IDeadLetterMessageFactory` for creating messages from failed requests
  - `DeadLetterFilter` for querying messages:
    - Factory methods: `All`, `FromSource`, `Since`, `ByCorrelationId`
    - Filter by source pattern, request type, correlation ID, date range
    - `ExcludeReplayed` option for pending messages only
  - `DeadLetterOptions` for configuration:
    - `RetentionPeriod` - how long to keep messages (default: 30 days)
    - `CleanupInterval` - background cleanup frequency (default: 1 hour)
    - `EnableAutomaticCleanup` - toggle cleanup processor
    - Integration flags: `IntegrateWithRecoverability`, `IntegrateWithOutbox`, etc.
    - `OnDeadLetter` callback for custom notifications
  - `DeadLetterOrchestrator` for coordinating DLQ operations
  - `IDeadLetterManager` with message replay capabilities:
    - `ReplayAsync(messageId)` - replay single message
    - `ReplayAllAsync(filter)` - batch replay with filter
    - `GetStatisticsAsync()` - queue statistics
    - `CleanupExpiredAsync()` - manual cleanup
  - `DeadLetterManager` implementation with reflection-based replay
  - `ReplayResult` and `BatchReplayResult` for replay operation results
  - `DeadLetterStatistics` with counts by source pattern
  - `DeadLetterHealthCheck` with warning/critical thresholds:
    - Configurable `WarningThreshold` (default: 10 messages)
    - Configurable `CriticalThreshold` (default: 100 messages)
    - `OldMessageThreshold` for stale message detection
  - `DeadLetterCleanupProcessor` background service for automatic cleanup
  - `DeadLetterSourcePatterns` constants: Recoverability, Outbox, Inbox, Scheduling, Saga, Choreography
  - `DeadLetterErrorCodes` for standardized error codes
  - High-performance logging with `LoggerMessage` attributes
  - DI registration via `AddEncinaDeadLetterQueue<TStore, TFactory>()`
  - Comprehensive test coverage: 75 unit tests, 11 integration tests, 22 property tests, 12 contract tests
  - **Built-in vs Custom Implementations**:
    - **Built-in (Testing)**: `FakeDeadLetterStore` in `Encina.Testing.Fakes` - In-memory store for unit/integration tests
    - **Custom (Production)**: Implement `IDeadLetterStore` and `IDeadLetterMessageFactory` for your persistence layer (EF Core, Dapper, ADO.NET, NoSQL, etc.)
    - **API Location**: Interface contracts in `Encina.Messaging.DeadLetter` namespace ([IDeadLetterStore.cs](src/Encina.Messaging/DeadLetter/IDeadLetterStore.cs), [IDeadLetterMessageFactory.cs](src/Encina.Messaging/DeadLetter/IDeadLetterMessageFactory.cs))
    - **Contract Tests**: Use `IDeadLetterStoreContractTests` base class to verify custom implementations
    - **Sample Implementation**: See [FakeDeadLetterStore.cs](src/Encina.Testing.Fakes/Stores/FakeDeadLetterStore.cs) for implementation reference
  - Example (Testing with built-in FakeDeadLetterStore):

    ```csharp
    // Testing scenario: Use built-in FakeDeadLetterStore
    services.AddEncinaDeadLetterQueue<FakeDeadLetterStore, FakeDeadLetterMessageFactory>(options =>
    {
        options.RetentionPeriod = TimeSpan.FromDays(30);
        options.EnableAutomaticCleanup = false; // Disable for testing
    });
    ```

  - Example (Production with custom store):

    ```csharp
    // Production scenario: Implement IDeadLetterStore for your persistence layer
    public class MyEfCoreDeadLetterStore : IDeadLetterStore
    {
        private readonly MyDbContext _context;
        public MyEfCoreDeadLetterStore(MyDbContext context) => _context = context;
        
        public async Task AddAsync(IDeadLetterMessage message, CancellationToken ct = default)
        {
            _context.DeadLetterMessages.Add(MapToEntity(message));
            await _context.SaveChangesAsync(ct);
        }
        // ... implement remaining interface methods
    }

    // Register with custom implementations
    services.AddEncinaDeadLetterQueue<MyEfCoreDeadLetterStore, MyDeadLetterMessageFactory>(options =>
    {
        options.RetentionPeriod = TimeSpan.FromDays(30);
        options.EnableAutomaticCleanup = true;
        options.IntegrateWithRecoverability = true;
        options.OnDeadLetter = async (msg, ct) =>
            await alertService.SendAlertAsync($"Message dead-lettered: {msg.RequestType}");
    });

    // Query and replay
    var stats = await manager.GetStatisticsAsync();
    var result = await manager.ReplayAsync(messageId);
    ```

- Low-Ceremony Sagas (Issue #41):
  - `SagaDefinition.Create<TData>(sagaType)` fluent API for defining sagas inline
  - `SagaStepBuilder<TData>` with `Execute()` and `Compensate()` methods
  - `ISagaRunner` interface for executing saga definitions
  - `SagaRunner` implementation with full lifecycle management:
    - Sequential step execution with data flow between steps
    - Automatic compensation in reverse order on failure
    - Exception handling with compensation continuation
  - `SagaResult<TData>` record with `SagaId`, `Data`, and `StepsExecuted`
  - Simplified overloads without `IRequestContext` parameter
  - Optional timeout configuration via `WithTimeout(TimeSpan)`
  - Auto-generated step names (`Step 1`, `Step 2`, etc.) when not specified
  - High-performance logging with `LoggerMessage` attributes
  - Full test coverage: unit, property-based, and contract tests
  - Automatic DI registration when `UseSagas = true`
  - Example:

    ```csharp
    var saga = SagaDefinition.Create<OrderData>("ProcessOrder")
        .Step("Reserve Inventory")
            .Execute(async (data, ct) => /* ... */)
            .Compensate(async (data, ct) => /* ... */)
        .Step("Process Payment")
            .Execute(async (data, ct) => /* ... */)
            .Compensate(async (data, ct) => /* ... */)
        .WithTimeout(TimeSpan.FromMinutes(5))
        .Build();

    // SagaResult<TData> contains IsSuccess, Data (final state), Error (if failed),
    // SagaId, and StepsExecuted count for observability
    var result = await sagaRunner.RunAsync(saga, initialData);

    if (result.IsSuccess)
    {
        // Saga completed successfully - continue business flow
        logger.LogInformation("Order {OrderId} processed. Steps: {Steps}", 
            result.Data.OrderId, result.StepsExecuted);
        await notificationService.SendConfirmationAsync(result.Data);
    }
    else
    {
        // Saga failed and compensations ran - handle the failure
        logger.LogError("Order saga failed: {Error}", result.Error?.Message);
        await alertService.NotifyFailureAsync(result.SagaId, result.Error);
    }
    ```

- Automatic Rate Limiting with Adaptive Throttling (Issue #40):
  - `RateLimitAttribute` with configurable properties:
    - `MaxRequestsPerWindow` - Maximum requests allowed in the time window
    - `WindowSizeSeconds` - Duration of the sliding window
    - `ErrorThresholdPercent` - Error rate threshold for adaptive throttling (default: 50%)
    - `CooldownSeconds` - Duration to remain in throttled state (default: 30s)
    - `RampUpFactor` - Rate of capacity increase during recovery (default: 1.5x)
    - `EnableAdaptiveThrottling` - Toggle adaptive behavior (default: true)
    - `MinimumThroughputForThrottling` - Minimum requests before error rate is calculated
  - `IRateLimiter` interface with `AcquireAsync`, `RecordSuccess`, `RecordFailure`, `GetState`, `Reset`
  - `AdaptiveRateLimiter` implementation with:
    - Sliding window rate limiting algorithm
    - State machine: `Normal` → `Throttled` → `Recovering` → `Normal`
    - Thread-safe `ConcurrentDictionary` for per-key state management
    - Automatic outage detection via error rate monitoring
    - Gradual recovery with configurable ramp-up
  - `RateLimitingPipelineBehavior<TRequest, TResponse>` for automatic rate limiting
  - `RateLimitResult` record struct with `Allowed()` and `Denied()` factory methods
  - `RateLimitState` enum (`Normal`, `Throttled`, `Recovering`)
  - `EncinaErrorCodes.RateLimitExceeded` error code
  - Automatic DI registration as singleton (shared state across requests)
  - Comprehensive test coverage: 104 unit tests, 22 property tests, 22 contract tests, 10 guard tests, 4 load tests
  - Performance benchmarks for rate limiter operations
- AggregateTestBase for Event Sourcing testing (Issue #46):
  - `AggregateTestBase<TAggregate, TId>` base class for Given/When/Then testing pattern
  - `AggregateTestBase<TAggregate>` convenience class for Guid identifiers
  - `Given(params object[] events)` for setting up event history
  - `GivenEmpty()` for testing aggregate creation scenarios
  - `When(Action<TAggregate>)` and `WhenAsync(Func<TAggregate, Task>)` for command execution
  - `Then<TEvent>()` and `Then<TEvent>(Action<TEvent>)` for event assertions
  - `ThenEvents(params Type[])` for verifying event sequence
  - `ThenNoEvents()` for idempotency testing
  - `ThenState(Action<TAggregate>)` for state assertions
  - `ThenThrows<TException>()` for exception assertions
  - `GetUncommittedEvents()` and `GetUncommittedEvents<TEvent>()` for direct access
  - Located in `Encina.Testing.EventSourcing` namespace
  - 75 tests covering unit, property, contract, and guard clause scenarios
- Health Check Abstractions (Issue #35):
  - `IEncinaHealthCheck` interface for provider-agnostic health monitoring
  - `HealthCheckResult` struct with `Healthy`/`Degraded`/`Unhealthy` status
  - `EncinaHealthCheck` abstract base class with exception handling
  - `OutboxHealthCheck` for monitoring pending outbox messages
  - `InboxHealthCheck` for monitoring inbox processing state
  - `SagaHealthCheck` for detecting stuck/expired sagas
  - `SchedulingHealthCheck` for monitoring overdue scheduled messages
  - Configurable warning/critical thresholds for all health checks
  - ASP.NET Core integration via `EncinaHealthCheckAdapter`
  - `CompositeEncinaHealthCheck` for aggregating multiple health checks
  - Extension methods: `AddEncinaHealthChecks()`, `AddEncinaOutbox()`, `AddEncinaInbox()`, `AddEncinaSaga()`, `AddEncinaScheduling()`
  - Kubernetes readiness/liveness probe compatible
- Automatic Provider Health Checks (Issue #113):
  - `ProviderHealthCheckOptions` for configuring provider health checks (enabled by default)
  - `DatabaseHealthCheck` abstract base class for database connectivity checks
  - Automatic health check registration when configuring Dapper providers:
    - `PostgreSqlHealthCheck` for PostgreSQL (Encina.Dapper.PostgreSQL)
    - `MySqlHealthCheck` for MySQL/MariaDB (Encina.Dapper.MySQL)
    - `SqlServerHealthCheck` for SQL Server (Encina.Dapper.SqlServer)
    - `OracleHealthCheck` for Oracle Database (Encina.Dapper.Oracle)
    - `SqliteHealthCheck` for SQLite (Encina.Dapper.Sqlite)
  - Automatic health check registration when configuring ADO.NET providers:
    - `PostgreSqlHealthCheck` for PostgreSQL (Encina.ADO.PostgreSQL)
    - `MySqlHealthCheck` for MySQL/MariaDB (Encina.ADO.MySQL)
    - `SqlServerHealthCheck` for SQL Server (Encina.ADO.SqlServer)
    - `OracleHealthCheck` for Oracle Database (Encina.ADO.Oracle)
    - `SqliteHealthCheck` for SQLite (Encina.ADO.Sqlite)
  - `EntityFrameworkCoreHealthCheck` for EF Core DbContext connectivity (Encina.EntityFrameworkCore)
  - `MongoDbHealthCheck` for MongoDB connectivity (Encina.MongoDB)
  - `RedisHealthCheck` for Redis/Valkey/KeyDB/Dragonfly/Garnet connectivity (Encina.Caching.Redis)
  - `RabbitMQHealthCheck` for RabbitMQ broker connectivity (Encina.RabbitMQ)
  - `KafkaHealthCheck` for Apache Kafka broker connectivity (Encina.Kafka)
  - `AzureServiceBusHealthCheck` for Azure Service Bus connectivity (Encina.AzureServiceBus)
  - `AmazonSQSHealthCheck` for Amazon SQS connectivity (Encina.AmazonSQS)
  - `NATSHealthCheck` for NATS server connectivity (Encina.NATS)
  - `MQTTHealthCheck` for MQTT broker connectivity (Encina.MQTT)
  - `MartenHealthCheck` for Marten/PostgreSQL event store connectivity (Encina.Marten)
  - `HangfireHealthCheck` for Hangfire scheduler status (Encina.Hangfire)
  - `QuartzHealthCheck` for Quartz.NET scheduler status (Encina.Quartz)
  - Configurable timeout, tags, and failure status
  - Opt-out via `config.ProviderHealthCheck.Enabled = false`
  - Integration tests with Testcontainers for all providers
  - `SignalRHealthCheck` for SignalR hub connectivity (Encina.SignalR)
  - `GrpcHealthCheck` for gRPC service connectivity (Encina.gRPC)
  - Health check documentation in all provider READMEs
- Modular Monolith support (Issue #57):
  - `IModule` interface for defining application modules
  - `IModuleLifecycle` interface for modules with startup/shutdown hooks
  - `IModuleRegistry` for runtime module discovery and lookup
  - `ModuleConfiguration` for fluent module registration
  - `ModuleLifecycleHostedService` for automatic lifecycle management
  - `AddEncinaModules()` extension method for service registration
  - Automatic handler discovery from module assemblies
  - Module ordering: start in registration order, stop in reverse (LIFO)
- Module Health Checks (Issue #114):
  - `IModuleWithHealthChecks` interface for modules to expose health checks
  - `IModuleHealthCheck` interface for module-specific health checks with `ModuleName` property
  - `AddEncinaModuleHealthChecks()` extension method for registering all module health checks
  - `AddEncinaModuleHealthChecks<TModule>()` for registering specific module health checks
  - Automatic tagging with `encina`, `ready`, and `modules` tags
  - Integration with ASP.NET Core health check endpoints
- Health Checks Integration Guide (Issue #115):
  - Comprehensive documentation for integrating Encina with AspNetCore.HealthChecks.* packages
  - Examples for microservice and modular monolith architectures
  - Kubernetes probes configuration (liveness, readiness, startup)
  - Recommended NuGet packages table for databases, caches, message brokers, cloud services
  - Best practices for health check organization and tagging
  - Located at `docs/guides/health-checks.md`
- Saga Not Found Handler support (Issue #43):
  - `IHandleSagaNotFound<TMessage>` interface for custom handling when saga correlation fails
  - `SagaNotFoundContext` with `Ignore()` and `MoveToDeadLetterAsync()` actions
  - `SagaNotFoundAction` enum (`None`, `Ignored`, `MovedToDeadLetter`)
  - `ISagaNotFoundDispatcher` for invoking registered handlers
  - `SagaErrorCodes.HandlerCancelled` and `SagaErrorCodes.HandlerFailed` error codes
  - Automatic DI registration when `UseSagas` is enabled
- Delegate Cache Optimization benchmarks (Issue #49):
  - New `CacheOptimizationBenchmarks.cs` for validating cache performance improvements
  - Benchmarks for TryGetValue vs GetOrAdd patterns
  - Type check caching comparison benchmarks
- Recoverability Pipeline (Issue #39):
  - Two-phase retry strategy: immediate retries (in-memory) + delayed retries (persistent/scheduled)
  - `RecoverabilityOptions` with configurable immediate retries (default 3), delayed retries (30s, 5m, 30m, 2h), exponential backoff, and jitter
  - `IErrorClassifier` interface with `DefaultErrorClassifier` for classifying errors as Transient/Permanent/Unknown
  - Error classification by exception type (TimeoutException → Transient, ArgumentException → Permanent)
  - Error classification by HTTP status codes (5xx → Transient, 4xx → Permanent)
  - Error classification by message patterns ("timeout", "connection" → Transient)
  - `RecoverabilityContext` for tracking retry state and history
  - `FailedMessage` record for dead letter queue handling with full context
  - `RecoverabilityPipelineBehavior<TRequest, TResponse>` pipeline behavior
  - `IDelayedRetryScheduler`, `IDelayedRetryStore`, `IDelayedRetryMessage` abstractions
  - `DelayedRetryScheduler` and `DelayedRetryProcessor` (BackgroundService) implementations
  - `OnPermanentFailure` callback for DLQ integration
  - Opt-in via `MessagingConfiguration.UseRecoverability = true`
  - 64 unit tests covering all recoverability scenarios

### Changed

- **Test Framework Migration from FluentAssertions to Shouldly** (Issue #495):
  - **BREAKING**: Replaced FluentAssertions with Shouldly across all 114 test projects
  - Motivation: FluentAssertions adopted commercial licensing ($130/developer/year) in January 2025
  - Shouldly remains MIT-licensed and is a mature, actively maintained library
  - All assertion syntax migrated:
    - `.Should().Be(x)` → `.ShouldBe(x)`
    - `.Should().BeTrue()` → `.ShouldBeTrue()`
    - `.Should().Throw<T>()` → `Should.Throw<T>(action)`
    - `.Should().HaveCount(n)` → `.Count.ShouldBe(n)` or `.ShouldHaveCount(n)`
    - `FluentActions.Invoking(...).Should().Throw<T>()` → `Should.Throw<T>(() => ...)`
  - Custom `Encina.Testing.Shouldly` package provides `Either<TLeft, TRight>` assertion extensions
  - Tests verified: 1000+ tests passing across core projects

- **Validation Architecture Consolidation** (Issue #229) - Remove duplicate validation behaviors:
  - **BREAKING**: Removed `Encina.FluentValidation.ValidationPipelineBehavior<TRequest, TResponse>` (use centralized `Encina.Validation.ValidationPipelineBehavior<,>`)
  - **BREAKING**: Removed `Encina.DataAnnotations.DataAnnotationsValidationBehavior<TRequest, TResponse>` (use centralized behavior)
  - **BREAKING**: Removed `Encina.MiniValidator.MiniValidationBehavior<TRequest, TResponse>` (use centralized behavior)
  - All validation now goes through `ValidationOrchestrator` + provider-specific `IValidationProvider`
  - DRY: Single `ValidationPipelineBehavior` in `Encina.Validation` namespace
  - Consistent error handling across all validation providers

- **Milestone Reorganization**: Phase 2 (364 issues) split into 10 incremental milestones:
  - v0.10.0 — DDD Foundations (31 issues)
  - v0.11.0 — Testing Infrastructure (25 issues)
  - v0.12.0 — Database & Repository (22 issues)
  - v0.13.0 — Security & Compliance (25 issues)
  - v0.14.0 — Cloud-Native & Aspire (23 issues)
  - v0.15.0 — Messaging & EIP (71 issues)
  - v0.16.0 — Multi-Tenancy & Modular (21 issues)
  - v0.17.0 — AI/LLM Patterns (16 issues)
  - v0.18.0 — Developer Experience (43 issues)
  - v0.19.0 — Observability & Resilience (87 issues)

- **Performance**: Optimized delegate caches to minimize reflection and boxing (Issue #49):
  - TryGetValue-before-GetOrAdd pattern on ConcurrentDictionary to avoid delegate allocation on cache hits
  - Cached `GetRequestKind` type checks to avoid repeated `IsAssignableFrom` calls on hot paths
  - Applied to both `RequestDispatcher` and `NotificationDispatcher`

- **BREAKING**: `EncinaErrors.Create()` and `EncinaErrors.FromException()` `details` parameter changed from `object?` to `IReadOnlyDictionary<string, object?>?` (Issue #34)
- **BREAKING**: `EncinaErrorExtensions.GetDetails()` now returns `IReadOnlyDictionary<string, object?>` instead of `Option<object>`
- `EncinaException` internal class now stores `Details` as `IReadOnlyDictionary<string, object?>` instead of `object?`
- `GetMetadata()` is now an alias for `GetDetails()` (both return the same dictionary)
- Saga timeout support (Issue #38):
  - `TimeoutAtUtc` property in `ISagaState` interface
  - `SagaStatus.TimedOut` status constant
  - `SagaErrorCodes.Timeout` error code
  - `TimeoutAsync()` method in `SagaOrchestrator` to mark sagas as timed out
  - `GetExpiredSagasAsync()` method in `ISagaStore` and all implementations
  - `StartAsync()` overload with timeout parameter
  - `DefaultSagaTimeout` and `ExpiredSagaBatchSize` options in `SagaOptions`
  - Full implementation across all providers (EF Core, Dapper, MongoDB, ADO.NET)
- Regex timeout protection against ReDoS attacks (S6444) in caching and SignalR components
- SQL injection prevention via `SqlIdentifierValidator` for dynamic table names
- ROP assertion extensions in `Encina.TestInfrastructure.Extensions`:
  - `ShouldBeSuccess()` / `ShouldBeRight()` - Assert Either is Right
  - `ShouldBeError()` / `ShouldBeLeft()` - Assert Either is Left
  - `ShouldBeBottom()` / `ShouldNotBeBottom()` - Assert Either default state
  - `AllShouldBeSuccess()` / `AllShouldBeError()` - Collection assertions
  - `ShouldContainSuccess()` / `ShouldContainError()` - Collection contains assertions
  - `ShouldBeErrorWithCode()`, `ShouldBeValidationError()`, `ShouldBeAuthorizationError()` - EncinaError assertions
  - Async variants: `ShouldBeSuccessAsync()`, `ShouldBeErrorAsync()`, `ShouldBeErrorWithCodeAsync()`
- Centralized messaging patterns with shared `Log.cs` and `TransactionPipelineBehavior.cs`
- Improved null handling in `InboxOrchestrator` response deserialization

### Fixed

- SonarCloud coverage detection with proper `SonarQubeTestProject` configuration
- Benchmark CSV parsing and mutation report path detection
- EF Core PropertyTests compilation errors (Issue #116):
  - Removed obsolete FsCheck 2.x files (`Generators.cs`, `OutboxStoreEFFsCheckTests.cs`)
  - Fixed `SagaStatus` type ambiguity in `SagaStoreEFPropertyTests.cs`
  - Fixed `SagaStatus` type ambiguity in `SagaStoreEFIntegrationTests.cs`

---

## [0.9.0]
