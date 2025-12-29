# Encina Roadmap

> **Build resilient .NET applications with Railway Oriented Programming.**

This document outlines the vision, current status, and future direction of Encina. For detailed task tracking, see [GitHub Issues](https://github.com/dlrivada/Encina/issues) and [Milestones](https://github.com/dlrivada/Encina/milestones).

---

## Current Status

**Version**: Pre-1.0 (breaking changes allowed)
**Packages**: 53 active (including CLI tool)
**Target**: .NET 10

| Category | Packages | Status |
|----------|----------|--------|
| Core & Validation | 5 | ✅ Production |
| Web Integration | 2 | ✅ Production |
| Serverless | 2 | ✅ Production |
| Database Providers | 12 | ✅ Production |
| Messaging Transports | 10 (+ 6 planned) | ✅ Production |
| Caching | 8 | ✅ Production |
| Scheduling | 2 | ✅ Production |
| Resilience | 3 | ✅ Production |
| Distributed Lock | 4 | ✅ Production |
| Event Sourcing | 1 | ✅ Production |
| Observability | 1 | ✅ Production |
| Developer Tooling | 1 | ✅ Production |

### Quality Metrics

| Metric | Current | Target |
|--------|---------|--------|
| Test Count | 3,800+ | 5,000+ |
| Line Coverage | 67% | ≥85% |
| Mutation Score | 79.75% | ≥80% |
| Build Warnings | 0 | 0 |

---

## Design Principles

- **Functional First** — Pure ROP with `Either<EncinaError, T>` as first-class citizen
- **Explicit over Implicit** — Code should be clear and predictable
- **Performance Conscious** — Zero-allocation hot paths, Expression tree compilation
- **Composable** — Behaviors are small, composable units
- **Pay-for-What-You-Use** — All features are opt-in via satellite packages

---

## Development Phases

Progress is tracked via [GitHub Milestones](https://github.com/dlrivada/Encina/milestones).

### Phase 1: Stability ✅
*Ensure green CI and all tests passing*

Focus: Fix failing tests, re-enable excluded test projects, verify all workflows.

→ [View Phase 1 Issues](https://github.com/dlrivada/Encina/milestone/1) (Completed)

---

### Phase 2: Functionality (v0.10.0 → v0.19.0)
*Expand capabilities with new features*

Phase 2 has been reorganized into 10 incremental milestones for better manageability:

| Version | Milestone | Issues | Focus |
|---------|-----------|--------|-------|
| **v0.10.0** | [DDD Foundations](https://github.com/dlrivada/Encina/milestone/7) | 31 | Value Objects, Entities, Aggregates, Specifications, Domain Events, ACL |
| **v0.11.0** | [Testing Infrastructure](https://github.com/dlrivada/Encina/milestone/8) | 25 | Fakes, Respawn, WireMock, Shouldly, Verify, Bogus, Architecture tests |
| **v0.12.0** | [Database & Repository](https://github.com/dlrivada/Encina/milestone/9) | 22 | Generic Repository, Specification, Unit of Work, Bulk Operations, Pagination |
| **v0.13.0** | [Security & Compliance](https://github.com/dlrivada/Encina/milestone/10) | 25 | Core Security, Audit Trail, Encryption, GDPR, Consent, NIS2, AI Act |
| **v0.14.0** | [Cloud-Native & Aspire](https://github.com/dlrivada/Encina/milestone/11) | 23 | .NET Aspire, Dapr, Service Discovery, HealthChecks, Orleans |
| **v0.15.0** | [Messaging & EIP](https://github.com/dlrivada/Encina/milestone/12) | 71 | Enterprise Integration Patterns, Transports, Process Manager, Schema Registry |
| **v0.16.0** | [Multi-Tenancy & Modular](https://github.com/dlrivada/Encina/milestone/13) | 21 | Multi-Tenancy, Modular Monolith, Feature Flags, BFF Pattern |
| **v0.17.0** | [AI/LLM Patterns](https://github.com/dlrivada/Encina/milestone/14) | 16 | MCP, Semantic Caching, AI Guardrails, RAG, Vector Store, Multi-Agent |
| **v0.18.0** | [Developer Experience](https://github.com/dlrivada/Encina/milestone/15) | 43 | Roslyn Analyzers, Dashboard, OpenAPI, Hot Reload, Minimal API patterns |
| **v0.19.0** | [Observability & Resilience](https://github.com/dlrivada/Encina/milestone/16) | 87 | OpenTelemetry, Metrics, Circuit Breaker, Caching, Distributed Lock |

**Total: 364 issues across 10 milestones**

#### Milestone Details

##### v0.10.0 — DDD Foundations
*Prerequisites for all other features*

- Domain Events vs Integration Events separation
- Specification Pattern for query composition
- Value Objects & Aggregate base classes
- Strongly Typed IDs
- Domain Services abstraction
- Anti-Corruption Layer (ACL) pattern
- Ports & Adapters factory
- Result Pattern extensions (Either Fluent API)

→ [View v0.10.0 Issues](https://github.com/dlrivada/Encina/milestone/7)

##### v0.11.0 — Testing Infrastructure
*Complete testing toolkit*

- `Encina.Testing.Fakes` — Test doubles for IEncina
- `Encina.Testing.Respawn` — Database reset
- `Encina.Testing.WireMock` — HTTP API mocking
- `Encina.Testing.Shouldly` — Open-source assertions
- `Encina.Testing.Verify` — Snapshot testing
- `Encina.Testing.Bogus` — Test data generation
- `Encina.Testing.Architecture` — Architectural rules
- `FakeTimeProvider` — Time control for testing

→ [View v0.11.0 Issues](https://github.com/dlrivada/Encina/milestone/8)

##### v0.12.0 — Database & Repository
*Enterprise data access patterns*

- Generic Repository Pattern
- Specification Pattern with EF Core/Dapper
- Unit of Work
- Bulk Operations (Insert/Update/Delete)
- Soft Delete & Temporal Tables
- Audit Trail Pattern
- Optimistic Concurrency
- CDC Integration
- Pagination abstractions

→ [View v0.12.0 Issues](https://github.com/dlrivada/Encina/milestone/9)

##### v0.13.0 — Security & Compliance
*Production-ready for EU/GDPR*

- Core Security abstractions
- Audit Trail logging
- Field-Level Encryption
- PII Masking
- GDPR Core (RoPA, Consent, Data Subject Rights)
- Data Residency enforcement
- NIS2 Directive compliance
- AI Act compliance

→ [View v0.13.0 Issues](https://github.com/dlrivada/Encina/milestone/10)

##### v0.14.0 — Cloud-Native & Aspire
*.NET Aspire first-class integration*

- `Encina.Aspire` hosting package
- Aspire ServiceDefaults
- Dapr Building Blocks integration
- Service Discovery abstraction
- HealthChecks for Kubernetes
- Graceful Shutdown
- Orleans integration

→ [View v0.14.0 Issues](https://github.com/dlrivada/Encina/milestone/11)

##### v0.15.0 — Messaging & EIP
*Enterprise Integration Patterns*

- Process Manager pattern
- State Machine (FSM)
- Claim Check pattern
- Durable Execution
- Schema Registry
- Event Streaming
- New Transports (GCP Pub/Sub, Pulsar, Redis Streams)
- Message batching, compression, partitioning

→ [View v0.15.0 Issues](https://github.com/dlrivada/Encina/milestone/12)

##### v0.16.0 — Multi-Tenancy & Modular
*SaaS-ready architecture*

- Multi-Tenancy support
- Modular Monolith patterns
- Inter-Module Communication
- Feature Flags integration
- Module Lifecycle management
- BFF Pattern

→ [View v0.16.0 Issues](https://github.com/dlrivada/Encina/milestone/13)

##### v0.17.0 — AI/LLM Patterns
*AI-native CQRS framework*

- MCP (Model Context Protocol) support
- Semantic Caching pipeline behavior
- AI Guardrails & Safety
- RAG Pipeline patterns
- Token Budget & Cost Management
- LLM Observability
- Multi-Agent Orchestration
- Vector Store abstraction

→ [View v0.17.0 Issues](https://github.com/dlrivada/Encina/milestone/14)

##### v0.18.0 — Developer Experience
*First-class DX*

- Roslyn Analyzers & Code Fixes
- Developer Dashboard
- OpenAPI/Swagger integration
- Hot Reload support
- Dev Containers
- Minimal API patterns
- Performance optimizations

→ [View v0.18.0 Issues](https://github.com/dlrivada/Encina/milestone/15)

##### v0.19.0 — Observability & Resilience
*Production-ready monitoring*

- OpenTelemetry integration
- Metrics & Tracing
- Circuit Breaker patterns
- Advanced Caching patterns
- Distributed Lock providers
- Serverless enhancements
- Validation enhancements

→ [View v0.19.0 Issues](https://github.com/dlrivada/Encina/milestone/16)

---

#### Legacy Phase 2 Details (Completed Items)

Key areas already completed:
- **Saga Enhancements** — ✅ Timeouts, ✅ low-ceremony syntax, ✅ not-found handlers, Saga Visibility (#128)
- **Developer Tooling** — ✅ `Encina.Testing` package with fluent assertions, ✅ `Encina.Cli` scaffolding tool (Issue #47)
- **Performance** — ✅ Delegate cache optimization (Issue #49), Source generators for NativeAOT (Issue #50), Switch-based dispatch (Issue #51)
- **Enterprise Patterns** — ✅ Recoverability pipeline (Issue #39), ✅ Rate limiting (Issue #40), ✅ Dead Letter Queue (Issue #42), ✅ Bulkhead Isolation (Issue #53), ✅ Routing Slip (Issue #62), ✅ Scatter-Gather (Issue #63), ✅ Content-Based Router (Issue #64)
- **Cross-cutting** — ✅ Health checks (Issue #35), ✅ Projections/read models (Issue #36), ✅ Snapshotting (Issue #52), ✅ Event versioning (Issue #37), ✅ Distributed Lock (Issue #55)
- **Modular Monolith** — ✅ `IModule` interface, ✅ Module registry & lifecycle hooks, ✅ Module-scoped behaviors (Issue #58)
- **Serverless** — ✅ Azure Functions (Issue #59), ✅ AWS Lambda (Issue #60), ✅ Durable Functions (Issue #61)
- **Advanced Validation** (new - based on December 2025 research):
  - Source-Generated Validation (#227) — Compile-time validation with NativeAOT support (Validot-inspired)
  - Domain/Value Object Validation (#228) — Always-Valid Domain Model with ROP factory methods
  - Consolidate ValidationPipelineBehavior (#229) — Remove duplicate behaviors (CRITICAL)
  - Async/Cross-Field Validation (#230) — Database-backed and cross-property validation
  - OpenAPI Schema Validation (#231) — Contract-first validation against OpenAPI 3.1
  - Security Validation (#232) — OWASP-compliant input sanitization and injection prevention
  - Localization/i18n (#233) — Internationalized error messages with IStringLocalizer
  - Validation Aggregation (#234) — Multi-source validation with configurable strategies
  - Zod-like Schema Builder (#235) — TypeScript-inspired fluent schema API
  - Two-Phase Validation (#236) — Pipeline + Domain validation separation pattern
- **Advanced Messaging** (new):
  - Message Batching (#121) — Batch handlers for high-throughput scenarios
  - Claim Check (#122) — External storage for large payloads
  - Message Priority (#123) — Priority-based message processing
  - Enhanced Deduplication (#124) — Multiple deduplication strategies
  - Multi-Tenancy Messaging (#125) — First-class SaaS tenant isolation
  - Message TTL (#126) — Time-to-live and expiration
  - Request/Response RPC (#127) — RPC-style messaging
  - Message Encryption (#129) — Compliance-ready encryption
  - Competing Consumers (#130) — Consumer group management
  - Backpressure (#131) — Flow control for overloaded consumers
  - W3C Trace Context (#132) — OpenTelemetry context propagation
  - Recurring Messages (#133) — Cron-style scheduling
  - Message Versioning (#134) — Schema evolution with upcasting
  - Poison Message Detection (#135) — Intelligent poison message handling
- **Message Transports Expansion** (new - December 2025 research):
  - Encina.GoogleCloudPubSub (#237) — Complete cloud provider triangle (AWS, Azure, GCP)
  - Encina.AmazonEventBridge (#238) — AWS EventBridge with CloudEvents format
  - Encina.Pulsar (#239) — Apache Pulsar multi-tenant streaming
  - Encina.Redis.Streams (#240) — Redis Streams with consumer groups
  - Encina.ActiveMQ (#241) — Apache Artemis (TLP Dec 2025) via AMQP 1.0
  - Encina.Dapr (#242) — Dapr runtime integration with .NET Aspire
- **Enterprise Integration Patterns** (new - December 2025 research):
  - Message Translator (#243) — Format transformation between systems
  - Content Enricher (#244) — Message augmentation with external data
  - Splitter (#245) — Decompose composite messages
  - Aggregator (#246) — Combine related messages
  - Claim Check Pattern (#247) — Large message handling with external storage
  - Async Request-Reply (#248) — Correlation for long-running operations
  - Competing Consumers (#249) — Horizontal scaling abstraction
  - Message Filter (#250) — Selective message processing
  - Priority Queue (#251) — Priority-based message processing
- **Advanced Transport Features** (new - December 2025 research):
  - Message Batching (#252) — Bulk publish support across transports
  - Native Delayed Delivery (#253) — Broker-level scheduling (Azure SB, SQS, RabbitMQ)
  - Message Deduplication (#254) — Transport-level dedup (SQS FIFO, Kafka)
  - Message Partitioning (#255) — Partition key support for ordered processing
  - Consumer Groups (#256) — Unified consumer group abstraction
  - Bidirectional Streaming (#257) — gRPC-style streaming for high-throughput
  - Message Compression (#258) — Payload compression (gzip, lz4, zstd)
  - Schema Registry (#259) — Confluent/Azure Schema Registry integration
- **Transport Interoperability** (new - December 2025 research):
  - CloudEvents Format (#260) — CNCF standard event envelope
  - NServiceBus Interop (#261) — Consume/publish NServiceBus messages
  - MassTransit Interop (#262) — Consume/publish MassTransit messages
- **Transport Observability** (new - December 2025 research):
  - Transport Health Checks (#263) — Connection and queue health monitoring
  - Transport Metrics (#264) — Queue depth, processing rate, consumer lag
  - Distributed Tracing (#265) — W3C trace context propagation
- **Database Providers Patterns** (new - based on December 2025 research):
  - Generic Repository Pattern (#279) — `IRepository<TEntity, TId>` with CRUD + Specification
  - Specification Pattern (#280) — `ISpecification<T>` for reusable query encapsulation
  - Unit of Work Pattern (#281) — `IUnitOfWork` for cross-aggregate transactions
  - Multi-Tenancy Database Support (#282) — Tenant isolation (shared schema, schema-per-tenant, DB-per-tenant)
  - Read/Write Database Separation (#283) — CQRS physical split with read replicas
  - Bulk Operations (#284) — BulkInsert/Update/Delete/Merge (680x faster than SaveChanges)
  - Soft Delete & Temporal Tables (#285) — Logical delete + SQL Server temporal tables
  - Audit Trail Pattern (#286) — IAuditableEntity with CreatedAt/By, ModifiedAt/By
  - Optimistic Concurrency (#287) — IConcurrencyAware with conflict resolution
  - CDC Integration (#288) — Change Data Capture with Debezium/Kafka
  - Database Sharding (#289) — Horizontal partitioning with shard routing
  - Connection Pool Resilience (#290) — Pool monitoring, circuit breaker, warm-up
  - Query Cache Interceptor (#291) — EF Core second-level cache
  - Domain Entity Base Classes (#292) — Entity<TId>, AggregateRoot<TId> with domain events
  - Pagination Abstractions (#293) — PagedResult<T>, PaginationOptions, IPagedSpecification<T>
  - Cursor-based Pagination (#294) — Keyset pagination O(1) vs offset O(n) [RESEARCH]
- **Advanced DDD & Workflow Patterns** (new - based on December 2025 research):
  - Specification Pattern (#295) — `ISpecification<T>` for reusable query composition (Ardalis-inspired)
  - Process Manager Pattern (#296) — Workflow orchestration with dynamic decisions (EIP)
  - State Machine Pattern (#297) — Fluent FSM for entity lifecycle (Stateless/MassTransit-inspired)
  - Claim Check Pattern (#298) — Large payload handling with external storage (EIP)
  - Anti-Corruption Layer (#299) — Domain protection from external APIs (DDD)
  - Feature Flag Integration (#300) — Microsoft.FeatureManagement pipeline behavior
  - Priority Queue Support (#301) — Priority-based message processing in Outbox/Scheduling
  - Batching/Bulk Operations (#302) — `IBatchHandler<TRequest, TResponse>` for batch processing
  - Durable Execution (#303) — Checkpointing for long-running workflows (Temporal/Durable Functions-inspired)
  - Multi-Tenancy Pipeline (#304) — Automatic tenant isolation behavior
  - AI Agent Orchestration (#305) — LLM agent orchestration (Semantic Kernel/Microsoft Agent Framework)
  - Integration Events (#306) — Typed inter-module events for Modular Monolith
  - Request Versioning (#307) — Request upcasting and deprecation (Marten-inspired)
- **Advanced EDA Patterns** (new - based on December 2025 research):
  - CDC (Change Data Capture) (#308) — Database change streaming with Debezium integration
  - Schema Registry Integration (#309) — Event schema governance (Confluent/Azure Schema Registry)
  - Event Mesh / Event Gateway (#310) — Enterprise event distribution across bounded contexts
  - Claim Check Pattern (#311) — Large payload external storage for events
  - Domain vs Integration Events (#312) — Clear separation with Anti-Corruption Layer translation
  - Event Correlation & Causation (#313) — Full traceability with CorrelationId/CausationId
  - Temporal Queries (#314) — Time-travel queries for aggregates (Axon-inspired)
  - Durable Execution / Workflow Engine (#315) — Lightweight Temporal.io-style workflows
  - Event Enrichment Pipeline (#316) — Batch enrichment to avoid N+1 in projections
  - Process Manager Pattern (#317) — Long-running aggregate coordination (EIP)
  - Event Streaming Abstractions (#318) — First-class event streams with consumer groups
  - Idempotency Key Generator (#319) — Standardized key generation (Stripe/Uber patterns)
- **Advanced Event Sourcing Patterns** (new - based on December 2025 research):
  - Decider Pattern (#320) — Functional event sourcing with pure `Decide`/`Evolve` functions (industry best practice)
  - Causation/Correlation IDs (#321) — Automatic metadata tracking for distributed tracing
  - Crypto-Shredding GDPR (#322) — `Encina.Marten.GDPR` package with PII encryption and key deletion
  - Advanced Snapshot Strategies (#323) — TimeInterval, BusinessBoundary beyond event count
  - Blue-Green Projection Rebuild (#324) — Zero-downtime projection updates
  - Temporal Queries (#325) — Point-in-time state reconstruction with `LoadAtAsync`
  - Multi-Tenancy Event Sourcing (#326) — Conjoined, Dedicated, SchemaPerTenant modes
  - Event Archival/Compaction (#327) — Hot/warm/cold tiering with cloud storage
  - Bi-Temporal Modeling (#328) — Transaction time + Valid time for compliance
  - Visual Event Stream Explorer (#329) — CLI tool: `encina events list/trace/replay`
  - Actor-Based Event Sourcing (#330) — Orleans/Akka-inspired alternative pattern
  - EventQL Preconditions (#331) — Query-based consistency constraints
  - Tri-Temporal Modeling (#332) — Transaction + Valid + Decision time for auditing
- **Advanced CQRS Patterns** (new - based on December 2025 research):
  - Zero-Interface Handlers (#333) — Convention-based discovery without IRequestHandler (Wolverine-inspired)
  - Idempotency Pipeline Behavior (#334) — Lightweight deduplication at pipeline level
  - Request Timeout Behavior (#335) — Per-request timeouts with fallback strategies (Brighter-inspired)
  - Cursor-Based Pagination (#336) — O(1) pagination helpers for large datasets (GraphQL-inspired)
  - Request Versioning (#337) — Command/query upcasting with version chains (Axon-inspired)
  - Multi-Tenant Context Middleware (#338) — Automatic tenant resolution and isolation
  - Batch Command Processing (#339) — Atomic batch operations with strategies (MassTransit-inspired)
  - Request Enrichment (#340) — Auto-populate from context with attributes (Wolverine-inspired)
  - Notification Fanout Strategies (#341) — Priority, throttled, quorum delivery
  - Request Composition (#342) — Combine multiple queries in parallel (GraphQL-inspired)
  - Handler Discovery Analyzers (#343) — Roslyn compile-time validation
  - Progressive CQRS Adoption Guide (#344) — Documentation for when/how to use CQRS
- **Vertical Slice Architecture Patterns** (new - based on December 29, 2025 research):
  - Feature Flags Integration (#345) — Microsoft.FeatureManagement pipeline behavior, `[FeatureFlag]` attribute
  - Multi-Tenancy Support (#346) — ITenantResolver, TenantIsolationBehavior, database strategies (SaaS-essential)
  - Specification Pattern (#347) — Composable query specifications with `Specification<T>` (Ardalis-inspired)
  - API Versioning Integration (#348) — Versioned handlers with `[ApiVersion]`, VersionedRequestDispatcher
  - Request Batching (#349) — BatchCommand/BatchQuery with AllOrNothing, PartialSuccess strategies
  - Domain vs Integration Events (#350) — IDomainEvent, IIntegrationEvent, DomainEventDispatcher (DDD best practice)
  - Audit Trail Behavior (#351) — `[Auditable]` attribute, IAuditStore, sensitive data redaction (GDPR/compliance)
  - Modular Monolith (#352) — EncinaModule, IModuleEventBus, module isolation (architecture 2025)
  - CDC Integration (#353) — Change Data Capture with SQL Server, Debezium/Kafka providers
  - Enhanced Streaming (#354) — Stream pipeline behaviors, backpressure, parallel processing
  - Enhanced Idempotency (#355) — Stripe-style idempotency keys, X-Idempotency-Key header
  - Policy-Based Authorization (#356) — Resource-based auth, CQRS-aware policies
- **Modular Monolith Architecture Patterns** (new - based on December 29, 2025 research):
  - Multi-Tenancy Support (#357) — Comprehensive multi-tenancy with ITenantContext, DataIsolationLevel (Row/Schema/Database), tenant resolvers (SaaS-critical)
  - Inter-Module Communication (#358) — IDomainEvent vs IIntegrationEvent, IIntegrationEventBus, IModulePublicApi (DDD/microservices migration path)
  - Data Isolation per Module (#359) — ModuleDataIsolation, [ModuleSchema], Roslyn analyzer for cross-module access (enterprise architecture)
  - Module Lifecycle Enhancement (#360) — Module discovery, [DependsOn], lifecycle hooks, exports (Orleans/NestJS-inspired)
  - Feature Flags Integration (#361) — [FeatureGate], FeatureGatePipelineBehavior, [FallbackHandler], Azure App Configuration
  - Module Testing Utilities (#362) — ModuleTestBase, WithMockedModule, architecture tests, data isolation tests
  - Anti-Corruption Layer Support (#363) — IAntiCorruptionLayer, [ModuleAdapter], [ExternalSystemAdapter] (DDD pattern)
  - Module Health & Readiness (#364) — IModuleHealthCheck, dependency-aware health, per-module endpoints
  - Vertical Slice Architecture Support (#365) — [VerticalSlice], [SlicePipeline], CLI generator, SliceTestBase
  - Module Versioning (#366) — [ModuleVersion], [ModuleApiVersion], [Deprecated], Roslyn analyzer
- **Domain Modeling Building Blocks** (new - based on December 29, 2025 DDD research):
  - Value Objects (#367) — `ValueObject<T>` base class with structural equality, ROP factory methods (Vogen-inspired, ~5M downloads demand)
  - Rich Domain Events (#368) — `DomainEvent` base record with metadata (CorrelationId, CausationId, Version, AggregateId)
  - Entity Base Class (#369) — `Entity<TId>` with identity equality for non-aggregate entities
  - Provider-Agnostic AggregateRoot (#370) — `AggregateRoot<TId>` with domain events for state-based persistence (EF Core, Dapper)
  - Specification Pattern (#371) — Composable queries with `Specification<T>`, And/Or/Not operators (Ardalis-inspired, ~7M downloads)
  - Business Rules (#372) — `IBusinessRule` for domain invariants, separate from input validation
  - Integration Events (#373) — `IntegrationEvent` for cross-bounded-context, DomainToIntegrationEventMapper
  - Strongly Typed IDs (#374) — `StronglyTypedId<TValue>` for type-safe identifiers (StronglyTypedId-inspired, ~3M downloads)
  - Soft Delete Pattern (#375) — `ISoftDeletable` with auto-filtering, GDPR compliance
  - Auditing (#376) — `IAudited` with CreatedBy/ModifiedBy, auto-population via SaveChanges interceptor
  - Domain Service Marker (#377) — `IDomainService` interface for DI discovery
  - Anti-Corruption Layer (#378) — `IAntiCorruptionTranslator` for external system isolation
  - Bounded Context Helpers (#379) — `BoundedContext` base class, `ContextMap` for relationships
  - Generic Repository (#380) — `IRepository<T,TId>` abstraction with Specification support
  - Domain DSL Helpers (#381) — `AggregateBuilder`, fluent API for ubiquitous language
  - New packages planned: `Encina.DomainModeling`, `Encina.Specifications`, `Encina.Specifications.EntityFrameworkCore`
- **Microservices Architecture Patterns** (new - based on December 29, 2025 research):
  - Service Discovery (#382) — `IServiceDiscovery` with Consul, Kubernetes, Aspire backends (CRITICAL - foundational pattern)
  - BFF / API Gateway (#383) — `IBffRequestAdapter`, `IResponseAggregator`, YARP integration (CRITICAL - high demand 2025)
  - Domain vs Integration Events (#384) — `IDomainEvent`, `IIntegrationEvent`, automatic Outbox publishing (CRITICAL - DDD best practice)
  - Multi-Tenancy Support (#385) — `ITenantContext`, `ITenantResolver`, EF Core filtering, SaaS-essential (HIGH - enterprise critical)
  - Anti-Corruption Layer (#386) — `IAntiCorruptionLayer<,>`, Refit integration (HIGH - legacy integration)
  - Dapr Integration (#387) — State Store, Pub/Sub, Workflows as Saga backend (HIGH - CNCF leader)
  - Virtual Actors / Orleans (#388) — `IEncinaActor`, `EncinaGrain<TState>` (MEDIUM - high concurrency use cases)
  - API Versioning (#389) — `[ApiVersion]`, `ApiVersioningPipelineBehavior` (MEDIUM - API evolution)
  - Enhanced Deduplication (#390) — Content-based keys, sliding window, cached results (MEDIUM - Inbox improvement)
  - Sidecar/Ambassador (#391) — `ISidecarProxy`, Kubernetes deployment patterns (MEDIUM - cloud-native)
  - Process Manager (#392) — Hybrid choreography/orchestration with visibility (MEDIUM - EIP)
  - Eventual Consistency (#393) — Consistency monitor, conflict resolution (LOW - advanced helpers)
  - New packages planned: `Encina.ServiceDiscovery`, `Encina.ServiceDiscovery.Consul`, `Encina.BFF`, `Encina.BFF.YARP`, `Encina.MultiTenancy`, `Encina.AntiCorruption`, `Encina.Dapr`, `Encina.Actors`, `Encina.Orleans`, `Encina.Sidecar`
- **Security Patterns** (new - based on December 29, 2025 research):
  - Core Security (#394) — `ISecurityContext`, `SecurityPipelineBehavior`, RBAC/ABAC/Permission-based auth (CRITICAL - foundational)
  - Audit Trail (#395) — `IAuditLogger`, `AuditPipelineBehavior`, who/what/when/where compliance logging (CRITICAL - SOX/HIPAA/GDPR)
  - Field-Level Encryption (#396) — `IFieldEncryptor`, `[Encrypt]` attribute, Azure Key Vault/AWS KMS integration (HIGH - PCI-DSS/GDPR)
  - PII Masking (#397) — `IPIIMasker`, auto-detection, `[PII]` attribute, logging redaction (HIGH - GDPR essential)
  - Anti-Tampering (#398) — `IRequestSigner`, HMAC/RSA signatures, replay attack prevention (HIGH - API security)
  - Input Sanitization (#399) — `ISanitizer<T>`, XSS/SQL injection/command injection prevention (HIGH - OWASP Top 10)
  - Secrets Management (#400) — `ISecretProvider`, Vault/Azure/AWS/GCP integration, rotation (MEDIUM - cloud-native)
  - ABAC Engine (#401) — `IAbacEngine`, policy DSL, PDP/PEP pattern (MEDIUM - complex enterprise)
  - New packages planned: `Encina.Security`, `Encina.Security.Audit`, `Encina.Security.Encryption`, `Encina.Security.PII`, `Encina.Security.AntiTampering`, `Encina.Security.Sanitization`, `Encina.Security.Secrets`, `Encina.Security.ABAC`
- **Compliance Patterns - GDPR & EU Laws** (new - based on December 29, 2025 research):
  - GDPR Core (#402) — `IDataController`, `RoPARegistry`, `GDPRCompliancePipelineBehavior` (CRITICAL - EU mandatory)
  - Consent Management (#403) — `IConsentManager`, `[RequireConsent]`, versioning, proof of consent (CRITICAL - Art. 7)
  - Data Subject Rights (#404) — `IDataSubjectRightsService`, Arts. 15-22 (Access, Erasure, Portability) (CRITICAL - fundamental rights)
  - Data Residency (#405) — `IDataResidencyEnforcer`, geo-routing, Schrems II compliance (CRITICAL - post-Schrems II)
  - Retention Policies (#406) — `IRetentionPolicyEngine`, automatic deletion, legal hold (HIGH - Art. 5(1)(e))
  - Anonymization (#407) — `IAnonymizer`, pseudonymization, k-anonymity, crypto-shredding (HIGH - Art. 4(5))
  - Breach Notification (#408) — `IBreachNotificationService`, 72-hour workflow, SIEM integration (HIGH - Art. 33-34)
  - DPIA Automation (#409) — `IDPIAService`, risk assessment, DPA submission (MEDIUM - Art. 35)
  - Processor Agreements (#410) — `IProcessorAgreementService`, Art. 28 compliance (MEDIUM - B2B SaaS)
  - Privacy by Design (#411) — `IPrivacyByDesignValidator`, Roslyn analyzer, data minimization (MEDIUM - Art. 25)
  - Cross-Border Transfer (#412) — `ICrossBorderTransferValidator`, SCCs, adequacy, TIA (MEDIUM - Chapter V)
  - Lawful Basis (#413) — `ILawfulBasisService`, Art. 6 tracking, LIA workflow (MEDIUM - processing foundation)
  - NIS2 Directive (#414) — `INIS2ComplianceService`, incident reporting, supply chain security (MEDIUM - EU 2022/2555)
  - EU AI Act (#415) — `IAIActComplianceService`, risk classification, transparency requirements (MEDIUM - EU 2024/1689)
  - New packages planned: `Encina.Compliance.GDPR`, `Encina.Compliance.Consent`, `Encina.Compliance.DataSubjectRights`, `Encina.Compliance.DataResidency`, `Encina.Compliance.Retention`, `Encina.Compliance.Anonymization`, `Encina.Compliance.BreachNotification`, `Encina.Compliance.DPIA`, `Encina.Compliance.ProcessorAgreements`, `Encina.Compliance.PrivacyByDesign`, `Encina.Compliance.CrossBorderTransfer`, `Encina.Compliance.LawfulBasis`, `Encina.Compliance.NIS2`, `Encina.Compliance.AIAct`
- **.NET Aspire Integration Patterns** (new - based on December 29, 2025 research):
  - Encina.Aspire.Hosting (#416) — AppHost integration with `WithEncina()`, custom resources, service discovery (MEDIUM)
  - Encina.Aspire.ServiceDefaults (#417) — Service Defaults extension with OpenTelemetry, health checks, resilience (HIGH - foundational)
  - Encina.Aspire.Testing (#418) — Testing integration with `DistributedApplicationTestingBuilder`, assertions (HIGH - "largest gap")
  - Encina.Aspire.Dashboard (#419) — Dashboard extensions with custom commands for Outbox, Saga, Dead Letter (MEDIUM)
  - Encina.Dapr (#420) — Dapr building blocks: State Store, Pub/Sub, Service Invocation, Actors (MEDIUM - CNCF leader)
  - Encina.Aspire.Deployment (#421) — Deployment publishers for Azure Container Apps, Kubernetes, Docker Compose (LOW)
  - Encina.Aspire.AI (#422) — AI Agent & MCP Server support for Dashboard Copilot integration (LOW - roadmap 2026)
  - Modular Monolith Support (#423) — `IEncinaModule` with Aspire orchestration, module isolation (MEDIUM)
  - Multi-Repo Support (#424) — `AddEncinaExternalService()` for distributed repositories (MEDIUM - enterprise demand)
  - Hot Reload Support (#425) — Hot reload of handlers and configuration during development (MEDIUM - DX)
  - New packages planned: `Encina.Aspire.Hosting`, `Encina.Aspire.ServiceDefaults`, `Encina.Aspire.Testing`, `Encina.Aspire.Dashboard`, `Encina.Dapr`, `Encina.Aspire.Deployment`, `Encina.Aspire.AI`
  - New labels created: `area-aspire`, `area-mcp`, `area-hot-reload`
- **Advanced Caching** (new - based on December 2025 research):
  - Cache Stampede Protection (#266) — Single-Flight, PER, TTL Jitter (FusionCache-inspired)
  - Eager Refresh (#267) — Background refresh before expiration
  - Fail-Safe / Stale-While-Revalidate (#268) — Serve stale data on failure
  - Cache Warming (#269) — Pre-load cache on startup/deploy
  - Cache Backplane (#270) — Multi-node L1 synchronization via Pub/Sub
  - Tag-Based Invalidation (#271) — Invalidate by tags instead of patterns
  - Read/Write-Through Patterns (#272) — Alternative caching strategies
  - Cache Metrics (#273) — OpenTelemetry metrics for hit rate, latency
  - Advanced Serialization (#274) — MemoryPack, Zstd, per-type serializers
  - Multi-Tenant Policies (#275) — Quotas and policies per tenant (SaaS)
  - Cache Diagnostics (#276) — HTTP headers, debug endpoints
  - New Providers (#277) — Memcached, Pogocache, MemoryPack serializer
  - Auto-Recovery (#278) — Retry, Circuit Breaker, automatic reconnection
- **Advanced Resilience** (new - based on 2025 research):
  - Hedging Pattern (#136) — Parallel redundant requests for latency reduction (Polly v8)
  - Fallback / Graceful Degradation (#137) — Alternative responses when primary operations fail
  - Load Shedding with Priority (#138) — Netflix/Uber-inspired priority-based request shedding
  - Adaptive Concurrency Control (#139) — Netflix-inspired dynamic concurrency limits
  - ~~Cache Stampede Prevention (#140)~~ — Closed as duplicate of #266
  - Cascading Timeout Coordination (#141) — Timeout budget propagation across call chains
  - Health Checks Standardization (#142) — Unified health checks across all providers
  - Observability-Resilience Correlation (#143) — OpenTelemetry integration for resilience events
  - Backpressure / Flow Control (#144) — Reactive Streams-style backpressure for streaming
  - Chaos Engineering Integration (#145) — Polly Chaos strategies for fault injection testing
- **Advanced Scheduling** (new - based on 2025 research):
  - Cancellation & Update API (#146) — Cancel, reschedule, or update scheduled messages
  - Priority Queue Support (#147) — Priority-based message processing (Meta FOQS-inspired)
  - Idempotency Keys (#148) — Exactly-once execution with idempotency keys
  - Dead Letter Queue Integration (#149) — DLQ for failed scheduled messages
  - Timezone-Aware Scheduling (#150) — Full timezone support with DST handling
  - Rate Limiting for Scheduled (#151) — Prevent burst execution of due messages
  - Dependency Chains (#152) — DAG-based job dependencies
  - Observability & Metrics (#153) — OpenTelemetry integration for scheduling
  - Batch Scheduling (#154) — Efficient bulk schedule operations
  - Delayed Message Visibility (#155) — SQS-style visibility timeout
  - Scheduling Persistence Providers (#156) — Hangfire/Quartz backend adapters
  - Execution Windows (#157) — Business hours and maintenance window support
  - Schedule Templates (#158) — Reusable scheduling configurations
  - Webhook Notifications (#159) — External system notifications
  - Multi-Region Scheduling (#160) — Globally distributed scheduling
- **Advanced Testing** (new - based on 2025 research):
  - Test Data Generation (#161) — Bogus/AutoBogus integration for realistic test data
  - Testcontainers Integration (#162) — Docker fixtures for database testing
  - Database Reset with Respawn (#163) — Intelligent cleanup between tests
  - HTTP Mocking with WireMock (#164) — External API mocking
  - Snapshot Testing with Verify (#165) — Approval testing for complex responses
  - Architecture Testing (#166) — ArchUnitNET rules for CQRS patterns
  - Handler Registration Tests (#167) — Verify all handlers are registered
  - Pipeline Testing Utilities (#168) — Control behaviors in tests
  - Messaging Pattern Helpers (#169) — Helpers for Outbox, Inbox, Saga, Scheduling tests
  - Improved Assertions (#170) — Fluent assertions with chaining (Shouldly-style)
  - TUnit Support (#171) — Source-generated testing framework (NativeAOT compatible)
  - Mutation Testing Integration (#172) — Stryker.NET configuration and scripts
  - CI/CD Workflow Templates (#173) — Reusable GitHub Actions workflows
- **TDD Patterns** (new - based on December 29, 2025 research):
  - Encina.Testing.Fakes (#426) — Test doubles for IEncina and messaging stores (HIGH priority)
  - Encina.Testing.Respawn (#427) — Intelligent database reset with Respawn (HIGH priority)
  - Encina.Testing.WireMock (#428) — HTTP API mocking for integration tests (HIGH priority)
  - Encina.Testing.Shouldly (#429) — Open-source assertions (FluentAssertions alternative) (HIGH priority)
  - Encina.Testing.Verify (#430) — Snapshot testing integration
  - Encina.Testing.Bogus (#431) — Realistic test data generation
  - Encina.Testing.Architecture (#432) — Architectural rules enforcement with ArchUnitNET
  - FakeTimeProvider (#433) — Time control for testing (.NET 8+ TimeProvider)
  - BDD Specification Testing (#434) — Given/When/Then for handlers
  - Encina.Testing.FsCheck (#435) — Property-based testing extensions
  - Encina.Testing.Pact (#436) — Consumer-Driven Contract Testing
  - Stryker.NET Configuration (#437) — Mutation testing templates and scripts
- **Cloud-Native Patterns** (new - based on December 29, 2025 research):
  - Encina.Aspire (#449) — .NET Aspire integration with service discovery, health checks, resilience (HIGH priority)
  - Encina.Dapr (#450) — Dapr Building Blocks: State, Pub/Sub, Service Invocation, Locks (HIGH priority)
  - Encina.FeatureFlags (#451) — Feature flags with ConfigCat, LaunchDarkly, OpenFeature providers
  - Encina.Secrets (#452) — Secrets management with Azure Key Vault, AWS Secrets Manager, HashiCorp Vault
  - Encina.ServiceDiscovery (#453) — Service discovery with Kubernetes, Consul, Aspire backends
  - Encina.HealthChecks (#454) — Kubernetes health probes (liveness, readiness, startup)
  - Encina.GracefulShutdown (#455) — SIGTERM handling, in-flight request draining, Outbox flush
  - Encina.MultiTenancy (#456) — Multi-tenancy for SaaS (tenant resolution, data isolation)
  - Encina.CDC (#457) — Change Data Capture for Outbox (SQL Server, PostgreSQL, Debezium)
  - Encina.ApiVersioning (#458) — Handler versioning with `[ApiVersion]`, deprecation
  - Encina.Orleans (#459) — Orleans virtual actors integration for handlers and sagas
- **Developer Tooling & DX** (new - based on December 29, 2025 research):
  - Roslyn Analyzers & Code Fixes (#438) — `Encina.Analyzers` with 10+ analyzers (ENC001-ENC010) and code fixes (HIGH priority)
  - Saga Visualizer (#439) — Generate Mermaid/Graphviz/PlantUML diagrams from saga definitions
  - .NET Aspire Integration (#440) — `Encina.Aspire` with dashboard panel, health checks, OTLP (HIGH priority)
  - Enhanced Exception Formatting (#441) — `Encina.Diagnostics` with pretty-print for EncinaError
  - Hot Reload Support (#442) — Handler hot reload with `MetadataUpdateHandler.ClearCache`
  - AI-Ready Request Tracing (#443) — Request/response capture with PII redaction
  - Enhanced Testing Fixtures (#444) — Fluent assertions for Either, time-travel for sagas
  - Developer Dashboard (#445) — `Encina.Dashboard` local web UI (Hangfire-style) (HIGH priority)
  - OpenAPI Integration (#446) — `Encina.OpenApi` auto-generation from Commands/Queries
  - Dev Containers Support (#447) — `.devcontainer/` and GitHub Codespaces configuration
  - Interactive Documentation (#448) — Docusaurus/DocFX site with playground
  - New packages planned: `Encina.Analyzers`, `Encina.Aspire`, `Encina.Aspire.Hosting`, `Encina.Dashboard`, `Encina.Diagnostics`, `Encina.OpenApi`
  - New labels created: `area-analyzers`, `area-visualization`, `area-dashboard`, `area-diagnostics`, `area-devcontainers`, `area-documentation-site`
- **Web/API Integration** (new - based on December 2025 research):
  - Server-Sent Events (#189) — Native .NET 10 SSE support with `TypedResults.ServerSentEvents`
  - REPR Pattern Support (#190) — Request-Endpoint-Response for Vertical Slice Architecture
  - Problem Details RFC 9457 (#191) — Updated standard with automatic TraceId
  - API Versioning Helpers (#192) — Integration with `Asp.Versioning.Http`
  - Minimal APIs Organization (#193) — Extension methods for modular endpoint registration
  - Encina.SignalR Package (#194) — Real-time bidirectional communication (documented but not implemented)
  - GraphQL/HotChocolate Full (#195) — Schema generation, subscriptions, DataLoader
  - gRPC Improvements (#196) — Strong typing, proto generation, streaming
  - Rate Limiting Pipeline (#197) — Handler-level rate limiting with .NET 7+ middleware
  - OpenAPI 3.1 Enhanced (#198) — Auto-schema generation and SDK generation
  - BFF Pattern Support (#199) — Backend for Frontend with aggregation
  - AI/LLM Integration (#200) — Provider-agnostic AI integration (OpenAI, Azure, Ollama)
  - VSA Templates for CLI (#201) — Vertical Slice Architecture templates
  - WebHook Support (#202) — Signature validation and Inbox integration
  - Health Aggregation Endpoint (#203) — Combined `/health` with readiness/liveness
  - Passkey Authentication (#204) — WebAuthn/FIDO2 in authorization behavior
  - Google Cloud Functions (#205) — GCF integration parity with AWS/Azure
  - Cloudflare Workers (#206) — Edge computing with Workers and KV
- **Advanced Observability** (new - based on 2025 research):
  - Real Metrics Collection (#174) — System.Diagnostics.Metrics implementation (EncinaMetrics)
  - Correlation & Causation IDs (#175) — Request correlation and causation tracking
  - Baggage Propagation (#176) — W3C Baggage utilities for distributed context
  - Semantic Convention Attributes (#177) — Complete OTel messaging semantic attributes
  - Azure Monitor Integration (#178) — New package: Encina.OpenTelemetry.AzureMonitor
  - AWS X-Ray Integration (#179) — New package: Encina.OpenTelemetry.AwsXRay
  - Prometheus Metrics (#180) — New package: Encina.OpenTelemetry.Prometheus
  - Health Checks Package (#181) — New package: Encina.HealthChecks (dedicated)
  - Serilog Integration (#182) — New package: Encina.Serilog.OpenTelemetry
  - Sampling Behaviors (#183) — Configurable sampling for traces
  - Request Tracing Behavior (#184) — Detailed per-request tracing
  - Error Recording (#185) — Enhanced error capture in spans
  - Distributed Context Properties (#186) — Context propagation utilities
  - Grafana Dashboards (#187) — Pre-built dashboard templates
  - Aspire Dashboard Guide (#188) — .NET Aspire integration documentation
- **Advanced Distributed Lock** (new - based on December 2025 research):
  - PostgreSQL Provider (#207) — `pg_advisory_lock` native locks (HIGH priority)
  - MySQL Provider (#208) — `GET_LOCK` / `RELEASE_LOCK` functions
  - Azure Blob Storage Provider (#209) — Blob leases with auto-renewal
  - DynamoDB Provider (#210) — Conditional writes with TTL
  - Consul Provider (#211) — Session-based locking with health checks
  - etcd Provider (#212) — Lease-based locking with watch
  - ZooKeeper Provider (#213) — Ephemeral sequential nodes
  - Oracle Provider (#214) — DBMS_LOCK package
  - Distributed Semaphores (#215) — Limit concurrent access to N instances (HIGH priority)
  - Leader Election (#216) — Cluster-wide leader selection (HIGH priority)
  - Read/Write Locks (#217) — Multiple readers, single writer
  - Fencing Tokens (#218) — Split-brain prevention with monotonic tokens
  - Multi-Resource Locks (#219) — Atomic acquisition of multiple resources
  - DistributedLockPipelineBehavior (#220) — `[DistributedLock]` attribute (HIGH priority)
  - LeaderElectionPipelineBehavior (#221) — `[RequiresLeadership]` attribute (HIGH priority)
  - OpenTelemetry Integration (#222) — Metrics and traces for locks
  - Auto-extend Locks (#223) — Automatic lock extension for long operations
  - Lock Metadata (#224) — Information about lock holders
  - Lock Queuing & Fairness (#225) — FIFO ordering for waiters
  - RedLock Algorithm (#226) — High availability with multiple Redis instances
- **Clean Architecture Patterns** (new - based on December 29, 2025 research):
  - Result Pattern Extensions (#468) — Fluent API for `Either<EncinaError, T>`: Combine, Accumulate, ToActionResult, async chains
  - Partitioned Sequential Messaging (#469) — Sequential processing per partition key (SagaId, TenantId, AggregateId) with parallel cross-partition (Wolverine 5.0-inspired)
  - New labels created: `area-value-objects`, `area-strongly-typed-ids`, `area-specification-pattern`, `area-domain-services`, `area-result-pattern`, `area-bounded-context`
- **Hexagonal Architecture Patterns** (new - based on December 29, 2025 research):
  - Domain Events vs Integration Events (#470) — Formal separation between domain events (in-process) and integration events (cross-service)
  - Specification Pattern (#471) — Composable, testable queries with `Specification<T>` (Ardalis-inspired, ~9M downloads)
  - Value Objects & Aggregates (#472) — DDD base classes: `ValueObject`, `AggregateRoot<TId>`, `StronglyTypedId<T>`
  - Domain Services (#473) — `IDomainService` marker interface for cross-aggregate domain logic
  - Anti-Corruption Layer (#474) — `IAntiCorruptionLayer<TExternal, TDomain>` for external system integration
  - Ports & Adapters Factory (#475) — `IPort`, `IInboundPort`, `IOutboundPort` + explicit adapter registration
  - Vertical Slice + Hexagonal (#476) — `IFeatureSlice` combining feature organization with hexagonal boundaries
  - Bounded Context Modules (#477) — `IBoundedContextModule` with published/consumed event contracts
  - Result/DTO Mapping (#478) — `IResultMapper<TDomain, TDto>` with ROP semantics
  - Application Services (#479) — `IApplicationService<TInput, TOutput>` for use case orchestration
  - New packages planned: `Encina.DDD`, `Encina.Specifications`, `Encina.Specifications.EFCore`, `Encina.Hexagonal`
  - New labels created: `area-application-services`, `area-ports-adapters`, `area-hexagonal`, `area-dto-mapping`, `clean-architecture`, `abp-inspired`, `ardalis-inspired`
- **AI/LLM Patterns** (new - based on December 29, 2025 research):
  - MCP Support (#481) — Model Context Protocol server/client with `MCPServerBuilder`, SSE/HTTP transports
  - Semantic Caching (#482) — `SemanticCachingPipelineBehavior` with embedding-based cache (40-70% cost reduction)
  - AI Guardrails & Safety (#483) — `Encina.AI.Safety` with prompt injection, PII detection, content moderation
  - RAG Pipeline (#484) — `Encina.AI.RAG` with query rewriting, chunk retrieval, re-ranking
  - Token Budget (#485) — `TokenBudgetPipelineBehavior` with per-user/tenant limits, cost tracking
  - LLM Observability (#486) — `LLMActivityEnricher` with token metrics, TTFT, GenAI semantic conventions
  - Multi-Agent Orchestration (#487) — `Encina.Agents` with Sequential, Concurrent, Handoff, GroupChat patterns
  - Structured Output (#488) — `IStructuredOutputHandler` with JSON schema generation and validation
  - Function Calling (#489) — `IFunctionCallingOrchestrator` with `[AIFunction]` attribute for handlers
  - Vector Store (#490) — `Encina.VectorData` abstraction with Qdrant, Azure AI Search, Milvus providers
  - Prompt Management (#491) — `IPromptRepository` with versioning, A/B testing, analytics
  - AI Streaming (#492) — `IAIStreamHandler` with token-level streaming, backpressure, SSE integration
  - New packages planned: `Encina.MCP`, `Encina.AI.Safety`, `Encina.AI.RAG`, `Encina.Agents`, `Encina.VectorData`, `Encina.VectorData.*`

→ [View Phase 2 Issues](https://github.com/dlrivada/Encina/milestone/2)

### Phase 3: Testing & Quality
*Achieve reliability through comprehensive testing*

Focus: Increase coverage to ≥85%, complete Testcontainers fixtures, eliminate flaky tests.

→ [View Phase 3 Issues](https://github.com/dlrivada/Encina/milestone/3)

### Phase 4: Code Quality
*Static analysis and maintainability*

Focus: SonarCloud integration, eliminate code duplication, address complexity hotspots.

→ [View Phase 4 Issues](https://github.com/dlrivada/Encina/milestone/4)

### Phase 5: Documentation
*User-facing content and examples*

Key deliverables:
- Documentation site (DocFX + GitHub Pages)
- Quickstart guide (5 minutes to first request)
- Provider guides (all 39 packages documented)
- MediatR migration guide
- Runnable example projects

→ [View Phase 5 Issues](https://github.com/dlrivada/Encina/milestone/5)

### Phase 6: Release Preparation
*Security, publishing, and branding for 1.0*

Key deliverables:
- SLSA Level 2 compliance
- NuGet package publishing workflow
- Visual identity (logo, icons)
- Final documentation review

→ [View Phase 6 Issues](https://github.com/dlrivada/Encina/milestone/6)

---

## Not Implementing

| Feature | Reason |
|---------|--------|
| Generic Variance | Conflicts with "explicit over implicit" principle |
| EncinaResult wrapper | `Either<L,R>` from LanguageExt is sufficient |
| API Versioning Helpers (Issue #54) | `Asp.Versioning` already provides complete HTTP-level versioning; handler-level versioning is redundant |

## Deferred to Post-1.0

| Feature | Reason |
|---------|--------|
| ODBC Provider (Issue #56) | Valuable for legacy databases but not critical for core release; evaluate based on community demand |

### Deprecated Packages

The following packages were deprecated and removed:

| Package | Reason | Status |
|---------|--------|--------|
| Encina.Wolverine | Overlapping concerns with Encina's messaging | Deprecated |
| Encina.NServiceBus | Enterprise licensing conflicts | Deprecated |
| Encina.MassTransit | Overlapping concerns with Encina's messaging | Deprecated |
| ~~Encina.Dapr~~ | ~~Infrastructure concerns belong at platform level~~ | **Re-planned** (#387) - Dapr is CNCF graduated and high demand |
| Encina.EventStoreDB | Marten provides better .NET integration | Deprecated |

> Deprecated code preserved in `.backup/deprecated-packages/`
>
> **Note**: Encina.Dapr has been re-planned (Issue #387) based on December 2025 community research showing Dapr as the leading CNCF microservices framework.

---

## Contributing

We welcome contributions! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

**Pre-1.0 Policy**: Any feature can be added, modified, or removed without backward compatibility concerns.

**Post-1.0 Policy**: Breaking changes only in major versions following [Semantic Versioning](https://semver.org/).

---

## References

### Inspiration
- [LanguageExt](https://github.com/louthy/language-ext) — Functional programming for C#
- [MediatR](https://github.com/jbogard/MediatR) — Simple mediator implementation
- [Wolverine](https://wolverine.netlify.app/) — Next-gen .NET messaging

### Concepts
- [Railway Oriented Programming](https://fsharpforfunandprofit.com/rop/) — Scott Wlaschin
- [CQRS Pattern](https://martinfowler.com/bliki/CQRS.html) — Martin Fowler

---

## Links

- **Issues**: [github.com/dlrivada/Encina/issues](https://github.com/dlrivada/Encina/issues)
- **Milestones**: [github.com/dlrivada/Encina/milestones](https://github.com/dlrivada/Encina/milestones)
- **Changelog**: [CHANGELOG.md](CHANGELOG.md)
- **History**: [docs/history/](docs/history/)
