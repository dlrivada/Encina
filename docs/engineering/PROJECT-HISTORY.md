# Encina project history

> What the closed issues of this repository decided, rejected, learned and changed, consolidated per area with a citation to the issue that holds the evidence. Produced by the Historian pass of 2026-09-22 over the 331 issues closed up to that date: evidence (body, human comments, referencing pull requests) extracted by script, knowledge items extracted and grouped by the free local model in bounded batches, citations validated automatically, and the editorial review, corrections and promotion candidates written by the maintainer's main agent. A first incremental pass on 2026-09-24 folded in the 32 issues closed between 2026-09-22 and 2026-09-24, using the same pipeline (`tools/ai/historian-extract-closed.ps1` with a `-Since` filter) with area assignment and section drafting reviewed before merging. Method and costs: [`HOW-ENCINA-IS-BUILT.md`](HOW-ENCINA-IS-BUILT.md) §3 and §6. Raw data: `artifacts/local-ai/historian/` on the maintainer's machine (not versioned).

## How to use this document

- A bullet states a fact and cites the issue where the evidence is (`(#123)`). Follow the link before relying on it; the citation is the claim's provenance, not its proof.
- Facts that later decisions reversed are listed under **Editorial corrections**, not deleted, so that the history stays honest.
- **Candidates for promotion** at the end lists what should become an ADR or a rule in `CLAUDE.md`; nothing there is a rule until the maintainer promotes it (human decision gate, `AI-DEVELOPMENT-MODEL.md` §15).
- New closed issues are folded in by re-running the Historian pass and appending, with the date, under the affected area.

## Outcomes of the 363 closed issues

| Outcome | Issues |
|---|---|
| Delivered | 304 |
| Closed as duplicate | 26 |
| Rejected with a reason | 10 |
| Superseded by another design or issue | 7 |
| Moved elsewhere | 1 |
| Closed without comments or references (no evidence) | 15 |

419 knowledge items were extracted: 200 decisions, 58 rejected alternatives, 67 rules, 73 things learned the hard way, 21 changes of direction; 18 were duplicates of another item and are merged below.

## Areas

### Core pipeline and results

#### Decisions

- High-priority duplicated files were centralized to reduce duplication from approximately 11% to 1.7%. (#20)
- EncinaError details were changed from object? to IReadOnlyDictionary<string, object?> to improve type safety and maintainability before version 1.0. (#34)
- Authorization is handled by integrating with ASP.NET Core infrastructure to add ROP semantics on top of native policies rather than replacing the existing system. (#356)
- Security abstractions use attribute-based discovery to remain consistent with existing behaviors for caching and resilience. (#394)
- The project adopted the standard .NET TimeProvider abstraction instead of creating a custom IClock interface. (#433)
- Either/Result pattern extensions including Map, Bind, Combine, and Ensure were implemented in the Encina.DomainModeling package. (#468)
- Value Objects and Aggregate Base Classes are implemented in the Encina.DomainModeling package rather than a separate Encina.DDD package. (#472)
- Ports and Adapters interfaces are implemented in the Encina.DomainModeling package instead of a proposed Encina.Hexagonal package. (#475)
- Vertical Slice features are implemented in the Encina.DomainModeling package under P5 alongside IUseCaseHandler interfaces. (#476)
- Bounded Context support is implemented in Encina.DomainModeling and integrated with the design from issue #379. (#477)
- Result and DTO mapping interfaces are implemented in the Encina.DomainModeling package rather than the proposed Encina.DDD package. (#478)
- Application Service abstractions are implemented in the Encina.DomainModeling package rather than the proposed Encina.DDD package. (#479)
- The AddBoundedContext factory overload must register the concrete type to ensure services are resolvable. (#522)
- Encina adopts Snowflake, ULID, UUIDv7, and ShardPrefixed as the standard distributed ID strategies. (#638)
- Core ID generation abstractions return Railway Oriented Programming types to handle errors consistently. (#638)

**2026-09-24 pass:**

- Request context propagation in Encina core uses an AsyncLocal-backed ambient accessor, supplemented by explicit overloads for non-HTTP entry points to handle cases without ambient context. (#1147)

#### Rules the project committed to

- The IDomainService marker interface is defined identically to the specification in issue #377. (#473)
- Time-dependent code must use an injected TimeProvider instead of direct DateTime.UtcNow calls to ensure deterministic testing. (#543, #1146)
- Encina enforces Railway Oriented Programming by using Either<EncinaError, T> instead of exceptions for business logic failures. (#669, #671)

**2026-09-24 pass:**

- Components must read IRequestContext via IRequestContextAccessor at the point of use, rather than resolving it directly from dependency injection. (#1163)

#### Alternatives considered and rejected

- Using Option<object> for empty details was rejected in favor of returning an empty dictionary for consistency. (#34)
- Custom [AuthorizeRoles] and [AuthorizeClaim] attributes were rejected because they duplicate existing ASP.NET Core functionality and create a maintenance burden. (#356)
- External policy engines like Casbin were rejected to avoid adding external dependencies and complexity. (#356)
- Using only ASP.NET Core Authorization Policies was rejected because they do not integrate with the CQRS pipeline or evaluate request content. (#394)
- Wrapping existing libraries like NUlid was rejected because it lacks shard embedding and reverse routing. (#638)
- Unreachable defensive throws in Either matches were replaced with direct casting or restructured MatchAsync calls. (#672, #675)

#### Things learned the hard way

- Guard clauses convert deep NullReferenceExceptions into earlier ArgumentNullExceptions at the call site. (#33)
- LanguageExt's Match method does not allow null return values, which can cause crashes if a pipeline behavior returns Left before the handler executes. (#120)
- Either<T1, T2> is not covariant, preventing direct assignment to Either<EncinaError, object> and requiring explicit property access. (#520)
- IEncina.Send uses a single generic parameter, requiring reflection to match MakeGenericMethod with one type argument. (#520)
- LanguageExt's Match method throws ResultIsNullException if a branch returns null, even for Left branches. (#674)

**2026-09-24 pass:**

- Pipeline behaviors previously received null values for UserId and TenantId because the context was not correctly seeded from the ambient request accessor. (#1147)
- Resolving an unregistered IRequestContext from dependency injection returns null, which silently disables tenant- and identity-dependent logic. (#1163)

#### No longer applicable

**2026-09-24 pass:**

- Casting a ValueTask result from IEncina.Send to Task caused invalid-cast exceptions and prevented delayed retries: IEncina.Send returns a ValueTask, not a Task, superseded by the fix in #1162. (#1272)

### Messaging patterns and transports

#### Decisions

- Messaging pattern behaviors and post-processors are centralized in Encina.Messaging to eliminate cross-provider duplication. (#12)
- Saga implementation remains separate for Orchestration and Choreography with improved documentation instead of code unification. (#16)
- Messaging transports remain as independent provider packages without a common Strategy abstraction. (#18)
- Saga timeouts are implemented via a RequestTimeout<T>() pattern to handle hanging business processes. (#38)
- The recoverability pipeline distinguishes errors into Transient, Permanent, and Unknown categories using an IErrorClassifier. (#39)
- Low-ceremony sagas use a SagaDefinition fluent API with SagaStepBuilder for execute and compensate steps, executing sequentially with reverse compensation on failure. (#41)
- Dead Letter Queue handling includes a unified IDeadLetterStore abstraction across providers with support for message inspection and replay. (#42)
- Saga not found scenarios are handled via a dedicated IHandleSagaNotFound interface with Ignore or MoveToDeadLetter actions. (#43)
- Routing Slip compensation executes in reverse order of the completed steps upon failure. (#62)
- Scatter-Gather supports four distinct gather strategies: WaitForAll, WaitForFirst, WaitForQuorum, and WaitForAllAllowPartial. (#63)
- Content-Based Router rules can be configured to evaluate in parallel via a specific options flag for performance optimization. (#64)
- Message encryption is implemented at the serializer level using AES-256-GCM to ensure compatibility with existing infrastructure and centralized key management. (#129)
- Encina provides separate packages for different key management strategies (Azure Key Vault, AWS KMS, ASP.NET Core Data Protection) to support diverse deployment environments. (#129)
- The Debezium integration maintains a 'no Java dependency' philosophy by using HTTP consumer patterns. (#288)
- CDC implementation supports both Debezium Server (HTTP) and Debezium Connect (Kafka) modes. (#288)
- CDC uses a specific ICdcDeadLetterStore interface rather than reusing the general IDeadLetterStore to accommodate CDC-specific metadata like position and connector ID. (#631)
- Guid Id is the primary key for ProcessingActivity entities, not the activity string format. (#681)
- Messaging store interfaces must return Either<EncinaError, T> or Either<EncinaError, Option<T>> to enforce ROP patterns and eliminate exception-based error handling. (#690)

**2026-09-24 pass:**

- Outbox messages that exhaust their retry limits must be moved to an observable dead-letter state to enable logging, metrics and requeue capability. (#1150)

#### Rules the project committed to

- Send and Publish methods must implement equivalent guard clauses for null parameters and cancellation tokens to ensure API consistency. (#33)
- Permanent failures are routed to a Dead Letter Queue (DLQ) while transient errors undergo immediate or delayed retries. (#39)
- Integration events must always be published via the Outbox pattern to ensure reliable delivery and consistency, while domain events remain in-process. (#384)
- RegisterActivityAsync must use INSERT-only semantics to match InMemoryProcessingActivityRegistry behavior, returning an error on duplicates rather than upserting. (#681)

**2026-09-24 pass:**

- Outbox processors must treat a Left result from `IEncina.Publish` as a failure that triggers the `MarkAsFailedAsync` path, so a message is never marked processed without a successful delivery. (#1151)
- Job adapters for Hangfire and Quartz must throw typed exceptions on Left results so the underlying scheduler records the failure and triggers its own retry logic. (#1152)
- Scheduling methods must propagate store errors from `AddAsync` rather than always returning the message ID, to prevent silent failure of recurring job registration. (#1153)

#### Alternatives considered and rejected

- Strategy pattern was rejected for Sagas because unifying Orchestration and Choreography under a common interface would be artificial and hide architectural decisions. (#16)
- Strategy pattern was rejected for messaging transports because unifying APIs would lose transport-specific features that users rely on. (#18)
- EF Core change tracking was rejected because it couples CDC to the application lifecycle and misses changes from other applications. (#353)
- Database triggers were rejected due to being database-specific and hard to maintain across environments. (#353)
- Polling-based change detection was rejected due to latency and increased database load. (#353)
- Logging failed events to an external message broker DLQ was rejected to avoid adding infrastructure dependencies at the CDC level. (#631)

#### Things learned the hard way

- SQLite datetime format incompatibility can cause scheduled message reschedule tests to fail, requiring specific handling or skipping in property-based tests. (#9)
- Scheduled messages could be stored but never executed because the ScheduledMessageProcessor background service was missing from the Encina.Messaging core. (#765)

**2026-09-24 pass:**

- Constant retry backoff in the outbox, despite documentation claiming exponential behavior, caused silent message loss during brief downstream outages. (#1150)
- MongoDB outbox registration sets up messaging manually and may omit the background `OutboxProcessor` hosted service, breaking the at-least-once delivery guarantee unless it is added explicitly. (#1289)

### Data access and database providers

#### Decisions

- Store implementations retain intentional duplication to maintain provider independence and avoid cross-provider dependencies. (#12)
- Generic repositories use Railway Oriented Programming with `Either<EncinaError, T>` for functional error handling. (#279, #287)
- Core repository abstractions reside in `Encina.DomainModeling`, separate from provider-specific implementations. (#279)
- Keyset pagination is supported alongside offset pagination to optimize performance for indexed tables. (#280)
- SQL Server bulk operations utilize `SqlBulkCopy`, while PostgreSQL uses the `COPY` command for efficiency. (#284)
- Soft delete is implemented via application-level filtering and interceptors rather than database-level Row Level Security. (#285)
- Soft delete functionality is located in `Encina.DomainModeling` with specific base classes and pipeline behaviors. (#285)
- Audit tracking uses interfaces and base classes rather than attribute-based reflection or handler-level logic. (#286)
- Audit interfaces and base classes are placed in the `Encina.DomainModeling` package. (#286)
- The `IConcurrencyAware` interface is implemented by aggregate root base classes to enforce versioning. (#287)
- Domain events are managed via a `SaveChanges` interceptor in EF Core and a collector interface for other providers. (#292)
- Specification Pattern was implemented in the `Encina.DomainModeling` package rather than as a separate package. (#295)
- Anti-Corruption Layer was implemented in the `Encina.DomainModeling` package with base class support. (#299)
- Domain and Integration Events are explicitly separated using distinct interfaces and a dedicated mapper interface in `Encina.DomainModeling`. (#312)
- Specification Pattern work is consolidated under issue #295 as the primary tracking issue. (#347, #463)
- Domain vs Integration Events separation is consolidated under issue #312 as the primary tracking issue. (#350, #462)
- Domain events use records to provide immutability and value equality, which are ideal for representing facts. (#368)
- Integration Events are implemented as a separate concept from Domain Events to maintain bounded context isolation and stable external contracts. (#373)
- `StronglyTypedId` uses a base record approach rather than source generators to avoid external dependencies and integrate better with Encina patterns. (#374)
- A marker interface was chosen over an abstract base class for Domain Services to allow multiple inheritance and flexible implementation. (#377)
- The Generic Repository is implemented as an optional abstraction for state-based persistence, acknowledging that `DbContext` can be used directly when full EF Core features are needed. (#380)
- Strongly typed IDs implementation is tracked under issue #374 rather than duplicate proposals. (#461)
- Domain Services abstraction is consolidated into issues #377 and #473, avoiding duplicate implementation efforts. (#466)
- Non-EF Core providers use an `ImmutableAggregateHelper` utility class instead of extending interfaces to avoid leaky abstractions. (#572)
- Oracle provider support was removed from the pre-1.0 release scope due to high maintenance cost and declining market position. (#541)
- Dapper and ADO.NET repositories inject `IRequestContext` and `TimeProvider` into constructors to auto-populate audit fields before persistence. (#623)
- Sharded migrations use four strategies: `Sequential`, `Parallel`, `RollingUpdate`, and `CanaryFirst`. (#651)
- Migration history is stored per-shard in a table named `__EncinaMigrationHistory`. (#651)
- Scatter-gather pagination uses two merge strategies: `OverfetchAndMerge` and `EstimateAndDistribute`. (#652)
- `TransactionPipelineBehavior` must use `DbConnection.OpenAsync` and `BeginTransactionAsync` instead of synchronous counterparts. (#794)

**2026-09-24 pass:**

- Provider registration methods must register all required dependencies, including options classes, to ensure dependency injection resolution succeeds: `AddEncinaMongoDB` registered `InboxOrchestrator` without registering the `InboxOptions` it depends on. (#1273)

#### Rules the project committed to

- The Oracle provider is excluded from pre-1.0 scope, leaving 13 supported providers for this feature. (#286)
- All 13 database providers must implement the connection pool monitoring and resilience abstractions. (#290)
- A multi-provider rule requires all database features to be implemented across all supported providers to ensure parity. (#536)
- All store implementations must support `TimeProvider` injection to ensure testability and provider coherence. (#667)
- Database provider implementations must use async overloads accepting `CancellationToken` to satisfy S6966. (#897)

**2026-09-24 pass:**

- All ADO.NET providers must expose an `AddEncinaUnitOfWork` method in `ServiceCollectionExtensions`, aligning with the multi-provider rule for all ten providers. (#1260)
- Removed database providers, Oracle and SQLite, must have every reference and dispatch branch purged from EF Core packages; references extended well beyond `BulkOperations`, with 21 source files still mentioning them. (#1262)

#### Alternatives considered and rejected

- `TransactionScope` was rejected due to issues with async code and distributed transactions. (#281)
- Event sourcing was rejected for simple soft delete scenarios due to significant added complexity. (#285)
- Pessimistic locking was rejected in favor of optimistic concurrency to maintain throughput and avoid deadlocks. (#287)
- Record-based entities were rejected because they do not support mutable state required for traditional entities. (#292)
- Generic tuple returns and interface-based pagination results were rejected in favor of concrete record types for clarity. (#293)
- External libraries like `Vogen` were rejected because they focus on single-value primitives and add maintenance burden for complex objects. (#367)
- Structs were rejected for value objects because they cannot be null and have default constructor issues. (#367)
- Separate metadata classes were rejected because they complicate serialization and require managing two objects. (#368)
- Interfaces without base classes were rejected because they force developers to implement equality logic repeatedly, leading to bugs. (#369)
- Records were rejected for entities because they use structural equality, which is incorrect for entities requiring identity equality. (#369)
- Creating separate packages per persistence strategy was rejected to prevent fragmentation of the shared domain model. (#370)
- Direct dependency on `Ardalis.Specification` was rejected because it lacks integration with Encina's `Either` pattern and adds external dependency. (#371)
- Exposing `DbContext` through `IUnitOfWork` was rejected to maintain abstraction boundaries and avoid coupling consumers to EF Core. (#572)
- Manual population of audit fields was rejected as error-prone and boilerplate-heavy, defeating the purpose of `IAuditableEntity`. (#623)
- Dapper DTO unused member warnings (S1144/S3459) are treated as false positives and suppressed rather than refactored. (#896)

#### Things learned the hard way

- Provider-specific SQL scripts must use native data types (e.g., `VARCHAR2`, `TEXT`) and syntax, as copy-pasting from SQL Server causes failures. (#2)
- Oracle requires `BindByName = true` for ADO and Dapper stores to handle positional binding correctly. (#270)
- Oracle stores GUIDs as `RAW(16)`, requiring specific byte array conversion for Dapper and ADO. (#270)
- SQL Server Change Tracking implementation requires Enterprise or Developer editions, which limits deployment options for standard users. (#457)
- PostgreSQL integration tests fail with 'relation does not exist' because EF Core quotes identifiers while raw SQL schemas create unquoted lowercase tables. (#570)
- EF Core `EnsureCreatedAsync` does not create tables for custom `DbContexts` if the database already contains pre-created tables from raw SQL schema initialization. (#571)
- `IDbConnection.Open()` is the only blocking call in the codebase and causes ThreadPool starvation under high concurrency. (#794)

**2026-09-24 pass:**

- ADO.NET schema scripts for PostgreSQL and MySQL contained incorrect SQL Server syntax, revealed while documenting the database providers. (#83)
- EF Core filtered indexes with unquoted identifiers fail on PostgreSQL because the system folds identifiers to lowercase, causing DDL errors such as a missing column. (#1128)
- PostgreSQL audit store queries failed because nullable parameters in filters were not cast, causing type mismatches; the fix casts the reused nullable parameters in the PostgreSQL audit queries. (#1129)
- Using `TryAdd` for both in-memory and database stores means registration order decides which store wins, which can silently lose audit evidence if the in-memory store is registered first. (#1269)

#### Changes of direction

- Research on cursor-based pagination was consolidated with implementation tracking in a separate issue to unify efforts. (#294)
- SQLite was removed from the supported provider matrix before 1.0, invalidating SQLite-specific distributed lock implementations. (#608)
- The CDC abstraction was implemented as `ICdcConnector` with `IAsyncEnumerable` streaming rather than the proposed `IChangeDataCapture` interface. (#621)
- Oracle provider was removed from the pre-1.0 scope, reducing the provider count from 16 to 13 for testing purposes. (#537)

#### No longer applicable

- Types such as `SagaStatus` and `OutboxMessage` were moved from `Encina.Messaging` to `Encina.EntityFrameworkCore`, causing compilation errors in test helpers that referenced old namespaces. (#116)

### Caching

#### Decisions

- Cache invalidation is automatic and targeted by extracting entity types from SQL commands. (#291)
- Cache invalidation uses a generic IChangeEventHandler<JsonElement> to process CDC events without requiring typed entity registration for every table. (#632)
- Secrets caching uses ICacheProvider with PubSub-based invalidation to ensure cross-instance consistency, replacing the previous IMemoryCache implementation. (#694)

#### Rules the project committed to

- Caching pipeline behaviors are centralized in Encina.Caching, with providers implementing only ICacheProvider interfaces. (#13)

#### Alternatives considered and rejected

- The third-party EFSecondLevelCache.Core library was rejected to avoid external dependencies and ensure integration with Encina abstractions. (#291)

#### Things learned the hard way

- ConcurrentDictionary.GetOrAlloc always allocates the factory delegate even on cache hits, so TryGetValue should be checked first on hot paths. (#49)
- CDC table names do not always match EF Core CLR type names, requiring explicit table-to-entity-type mappings for cache key generation. (#632)

#### No longer applicable

- The project consolidated cache stampede prevention work into issue #266 rather than implementing the specific strategies proposed in #140. (#140)

### Event sourcing and Marten

#### Decisions

- Encina.EventStoreDB was deprecated and excluded from feature implementations in favor of Encina.Marten to leverage existing PostgreSQL infrastructure (#17, #321).
- CQRS read-side abstractions were planned to support multi-provider integration including Marten and EventStoreDB (#36).
- Event versioning will use an IEventUpcaster interface integrated with provider-specific versioning like Marten (#37).
- Crypto-shredding was adopted for GDPR compliance in event sourcing to preserve immutability while enabling data deletion by destroying subject-specific keys (#322).
- CrossBorderTransfer was built using Marten event sourcing immediately, unlike earlier modules, due to its high complexity and specific requirements (#412).
- Event-sourced aggregate abstractions (AggregateBase and IAggregate) were moved from Encina.Marten to Encina.DomainModeling to remove a broken transitive dependency (#494).
- Compliance modules use Marten event sourcing instead of the standard 13-database-provider rule to satisfy GDPR Art. 5(2) immutable audit requirements (#776).
- DataSubjectRights transactions and idempotency are handled by Marten's IDocumentSession.SaveChangesAsync and optimistic concurrency, removing the need for IUnitOfWork (#778).
- The LawfulBasis module was extracted from Encina.Compliance.GDPR into its own package to follow the single-aggregate event-sourcing pattern (#779).
- The Retention module was migrated to Marten event sourcing with three aggregates to replace entity-based persistence and provide immutable audit trails (#783).
- DataResidency was migrated to Marten event sourcing using two aggregates (ResidencyPolicy and DataLocation) to replace 13 database providers (#784).
- Temporal key granularity is configurable with Monthly as the default, alongside Quarterly and Yearly options, to support different regulatory requirements (#799).
- MartenAuditStore uses crypto-shredding to destroy temporal keys in PurgeEntriesAsync rather than performing physical deletion of events (#800).
- IAggregate.Version must be a settable property to allow the repository to sync the version after loading from Marten (#818).
- GDPR Core remains an entity-based metadata registry without Marten Event Sourcing because it lacks stateful lifecycle transitions (#820).
- Marten projections must inject dependencies via IDocumentOperations and constructor injection rather than IServiceProvider to pass validation (#949).

#### Rules the project committed to

- Event-sourced events must implement INotification to enable automatic publishing via the EventPublishingPipelineBehavior (#412).
- InMemory stores are obsolete in event-sourced compliance modules and must be deleted, with unit tests mocking IAggregateRepository instead (#777).
- InMemory retention stores must be deleted after event sourcing migration, and unit tests must mock IAggregateRepository via NSubstitute (#783).
- InMemory DataResidency stores are obsolete and deleted, and tests must use Marten with PostgreSQL via Docker or Testcontainers for persistence validation (#784).

#### Alternatives considered and rejected

- EventStoreDB was rejected because it requires dedicated infrastructure and JavaScript projections, unlike Marten's native .NET support (#17).
- Using Marten's AggregateBase for all scenarios was rejected because it is specific to event sourcing and lacks state-based persistence features (#370).
- An external event store sidechannel was rejected because it adds complexity and diverges from the aggregate root pattern (#569).
- Entity-based persistence across 13 providers was rejected for compliance modules because it cannot provide an immutable audit trail (#776).

#### Things learned the hard way

- Marten's AggregateStreamAsync does not increment AggregateBase.Version during replay, which causes concurrency errors if not manually synced (#818).
- Unit tests calling projection.Create directly bypass Marten's projection graph validation, hiding invalid signature errors (#949).

#### Changes of direction

- Event Sourcing requires a Strategy pattern rather than a simple Orchestrator/Provider pattern due to fundamental differences between EventStoreDB and Marten (#15).
- The project supports immutable domain models by preserving events during copy operations rather than requiring mutable entities (#569).
- The Consent module was migrated to Marten event sourcing (#403).
- The DataSubjectRights module was migrated to Marten event sourcing (#404).
- The DataResidency module was migrated to Marten event sourcing (#405).
- The Retention module was migrated to Marten event sourcing (#406).
- The BreachNotification module was migrated to Marten event sourcing (#408).
- The DPIA module was migrated to Marten event sourcing (#409).
- The ProcessorAgreements module was migrated to Marten event sourcing (#410).

### Validation

#### Decisions

- Validation logic is centralized in Encina.Validation using an Orchestrator pattern with provider-specific implementations. (#14, #229)
- Business rules are explicitly separated from the Specification pattern, as specifications are for querying while rules guard state transitions. (#372)
- Fluent domain builders are implemented with integrated business rule validation to ensure domain invariants are checked during construction, unlike external test libraries. (#381)

#### Rules the project committed to

- Validation error details must handle exceptions using the LanguageExt Option<Exception> pattern (IsSome/IfSome) rather than direct access. (#11)

#### Alternatives considered and rejected

- FluentValidation was rejected for domain rules because it is designed for API boundary input validation, not domain invariants. (#372)

#### Things learned the hard way

- CustomValidationAttribute requires the target type to be public, so internal types will fail validation context enrichment tests. (#10)
- A breaking change was introduced by removing specific validation behaviors from satellite packages, such as Encina.FluentValidation.ValidationPipelineBehavior. (#229)

### Observability, health checks and logging

#### Decisions

- The project mandates the use of LoggerMessage source generators for high-performance logging instead of standard extension method overloads. (#3)
- Health checks are abstracted via IEncinaHealthCheck with a base class handling exceptions and specific checks for outbox, inbox, saga, and scheduling. (#35)
- An adapter class maps IEncinaHealthCheck to the standard ASP.NET Core IHealthCheck interface for integration. (#35)
- Health checks are automatically registered upon provider configuration but can be explicitly disabled by users to prevent unwanted overhead or conflicts. (#113)
- Module health checks are exposed via a dedicated IModuleHealthCheck interface and can be registered automatically for all modules or selectively for specific modules. (#114)
- ProcessingActivity metrics must reuse the shared GDPRDiagnostics.Meter instance rather than creating a new Meter. (#681)
- EventId collision prevention is handled by a central registry and architecture test rather than manual fixes. (#828, #829)

#### Rules the project committed to

- All infrastructure providers must support health checks with configurable timeouts and tags to allow for differentiated monitoring and alerting strategies. (#113)

#### Alternatives considered and rejected

- Roslyn Analyzers and Source Generators were rejected for EventId enforcement due to inability to share state or stability issues. (#829)

#### Things learned the hard way

- Log filtering by EventId is unreliable if modules share ranges, making production diagnostics ambiguous for overlapping IDs. (#828)
- .NET 10 introduces ambiguous overloads for Counter.Add and Histogram.Record with KeyValuePair parameters, requiring explicit TagList or array wrappers. (#867)

**2026-09-24 pass:**

- Reflection-based enforcement of ADR-021 failed to detect EventIds defined via LoggerMessage.Define, allowing ID collisions, because those EventIds are plain constructor arguments that EventIdUniquenessRule does not inspect. (#1125)

### Resilience

#### Decisions

- Rate limiting is designed to detect outages and dynamically adjust processing rates, integrating with Polly for circuit breaking. (#40)
- Bulkhead isolation is implemented using a SemaphoreSlim-based manager within the pipeline behavior to enforce concurrency limits. (#53)
- Database-specific resilience is implemented via provider-specific health monitors and Polly circuit breakers, not generic policies alone. (#290)
- Secrets resilience uses Polly ResiliencePipeline for retry, circuit breaker, and timeout, with stale cache fallback managed by CachedSecretReaderDecorator. (#743)

#### Rules the project committed to

- Cached secrets are served from the last-known-good value when the vault is unavailable, bounded by a configurable MaxStaleDuration with a default of 1 hour. (#743)

### Security and regulatory compliance

#### Decisions

- Audit trail functionality is implemented in the existing Encina.Security.Audit package rather than a new Encina.Auditing package (#351).
- Field encryption uses AES-256-GCM with the serialization format ENC:v1:{Algorithm}:{KeyId}:{Nonce}:{Tag}:{Ciphertext} (#396).
- ISecretProvider was split into ISecretReader, ISecretWriter, and ISecretRotator to satisfy the Interface Segregation Principle for read-only providers (#400, #452).
- DI-first registration was chosen as the primary method for secret access, with attribute-based injection considered secondary (#400).
- Consent enforcement is controlled by a ConsentEnforcementMode enum rather than a boolean flag to follow the GDPREnforcementMode pattern (#403).
- The package follows the Railway Oriented Programming pattern with Either results in all public interfaces (#403).
- NIS2 compliance is implemented as a stateless rule engine that does not require a persistence layer or provider stores (#414).
- Bias detection indicators must include confidence intervals to allow compliance teams to assess statistical significance (#415).
- Human oversight decision records must include a Rationale field to satisfy Article 14 explainability requirements (#415).
- Cryptographic attestation is decoupled from the core AIAct engine to keep it stateless, delegating to a separate attestation module (#415).
- Read auditing uses repository decorators with fire-and-forget logging to ensure audit operations never block read performance (#573).
- Audit logs store full old and new values rather than diffs to simplify querying and reconstruction (#574).
- ISecretProvider uses Railway Oriented Programming for explicit error handling rather than exceptions (#603).
- HashiCorp Vault provider implements KV v2 secrets engine with specific error mapping codes for 404 and 403 responses (#678).
- ABAC policy storage uses IPolicyStore with upsert semantics and a dedicated IPolicySerializer for polymorphic IExpression trees using $type discriminators (#691).
- XACML 3.0 XML is the only standardized policy format for interoperability with external systems (#692).
- ABAC policy mutations must be audited via IAuditStore using a fire-and-forget pattern to avoid blocking operations (#796).
- Encryption scope is partial, targeting PII fields such as UserId, IpAddress, UserAgent, Payloads, and Metadata only (#799).
- HttpAttestationProvider must fail closed with IsValid=false if the verification endpoint URL is not configured (#803).
- HashChainAttestationProvider dynamically supports SHA-256, SHA-384, and SHA-512 via HashChainOptions.HashAlgorithm (#850).
- ADR-022 documents security considerations including HMAC-SHA256 rationale, constant-time comparison, and SSRF protection (#861).
- The documented REST contract for HttpAttestationProvider requires POST /attest and GET /receipt/{attestationId} (#862).

**2026-09-24 pass:**

- Retention enforcement must treat legal-hold lookup errors as "hold active" to prevent accidental erasure, so the check fails closed. (#1143)
- US and Canada adequacy decisions are conditional on the recipient being certified under the respective framework, specifically DPF or PIPEDA. (#1145)
- HMAC validation must fail closed by default when HttpContext is absent; skipping validation requires an explicit opt-out configuration or attribute. (#1155)

#### Rules the project committed to

- Feature-specific stores must be placed in subfolders named after the feature, not the source package (#413).
- NIS2 incident notification follows a 4-phase timeline: 24h early warning, 72h incident, interim on request, and 1mo final (#414).
- EF Core audit storage uses a single implementation supporting all 4 SQL providers via IEncinaDbContext (#574).
- Entity properties must exactly match the domain model, using Purpose instead of Description and ThirdCountryTransfers as string? instead of bool (#681).
- HMAC key size must match the configured hash algorithm (32/48/64 bytes) to avoid failures (#850).
- Options classes exposing sensitive properties must use [JsonIgnore] and override ToString() to prevent leaks (#851).

**2026-09-24 pass:**

- Transfers to regions without an unconditional adequacy decision must be evaluated on the recipient's certification status, not just the destination region. (#1145)
- Subject ID extraction must support convertible types such as Guid and fall back to the caller's UserId only when no matching property exists. (#1149)
- Retention enforcement erasure must be scoped to the specific DataCategory of the expired record, not the entire entity. (#1160)
- Legal-hold lifting must fail closed if releasing records fails or if the check for other active holds errors. (#1161)

#### Alternatives considered and rejected

- Event Sourcing was rejected as the primary audit mechanism because it is overkill for CRUD applications and requires architectural commitment (#395).
- JWT was rejected for request integrity because it is designed for authentication and does not naturally include request path/method in the signature (#398).
- Relying solely on Razor's built-in encoding was rejected because it does not cover API responses or database storage contexts (#399).
- External policy engines like OPA or Casbin were rejected for the core engine due to network latency and lack of native pipeline integration (#401).
- External GRC platforms like OneTrust were rejected in favor of embedded compliance to avoid high costs and lack of domain integration (#402).
- Creating 13 new independent packages for database stores was rejected in favor of subfolders within existing provider packages (#413).
- External compliance tools were rejected because they may not integrate with the request pipeline (#413).
- SQL-level query interceptors were rejected because they capture all reads with high overhead and cannot identify specific returned entities (#573).
- Using native IConfiguration secret providers was rejected because they only support reading secrets, lacking write, delete, or list capabilities (#603).

**2026-09-24 pass:**

- The previous behavior of silently skipping HMAC validation for background jobs and tests was rejected as a fail-open security risk. (#1155)

#### Things learned the hard way

- Using an ephemeral HMAC key in HashChainAttestationProvider causes silent evidence loss on process restart, so a startup warning is required (#902).

**2026-09-24 pass:**

- Retention enforcement previously failed to mark records as expired, causing deletion attempts to fail with `RetentionErrors.InvalidStateTransition` and entities to be erased repeatedly. (#1142)
- Passing EntityId as the subject ID to the erasure executor is incorrect and leads to improper scoping, because the executor treats it as a data-subject ID. (#1160)
- Ignoring `ReleaseRecordAsync` failures leaves records `UnderLegalHold` indefinitely, which excludes them from expired sweeps. (#1161)
- `AddEncinaCrossBorderTransfer` must explicitly register `IAdequacyDecisionProvider` via `TryAdd` to prevent DI resolution failures of the validator when `AddEncinaDataResidency` is not called. (#1285)

#### No longer applicable

- The PII package implements the IPiiMasker interface from the audit package to replace the default NullPiiMasker (#397).

### Modules, tenancy and sharding

#### Decisions

- Module lifecycle hooks execute in registration order for startup and in reverse (LIFO) order for shutdown. (#57)
- Module-scoped behaviors are filtered by mapping handler types to owning modules via assembly association rather than direct registration. (#58)
- Multi-tenancy is implemented via a dedicated Encina.Tenancy package for core abstractions and middleware. (#282)
- Read/write routing is implemented via a MediatR pipeline behavior using a DatabaseRoutingContext. (#283)
- Consistent hashing uses XxHash64 with 64-bit values (ulong) instead of 32-bit to maximize collision resistance. (#289)
- MongoDB sharding primarily uses native mongos routing, with application-level sharding as a minimal fallback. (#289)
- Bounded Context support uses explicit attributes and a ContextMap to document relationships rather than relying on namespace conventions or assembly separation. (#379)
- ACL pattern issues are consolidated into issue #299 as the primary tracking location. (#386)
- Bounded Context boundary implementation is consolidated into issues #379 and #477 rather than this new proposal. (#464)
- Vertical Slice Architecture support is tracked under issue #365 as the primary issue for this feature. (#465)
- Module isolation was extended to all database providers using a schema validation and connection-per-module strategy. (#534)
- MongoDB uses a database-per-module strategy for module isolation, distinct from the schema-based approach used by SQL providers. (#549)
- Cross-shard writes are coordinated using the existing Saga pattern instead of implementing Two-Phase Commit due to complexity and lack of pre-1.0 demand. (#637)
- Reference table replication supports CDC-driven, polling, and manual refresh strategies. (#639)
- Compound shard keys use a record with ordered components to support mixed routing strategies like range plus hash. (#641)
- Sharded read/write separation uses a unified factory interface to handle shard routing and replica selection. (#644)
- Five replica selection strategies (RoundRobin, Random, LeastLatency, LeastConnections, WeightedRandom) are supported for sharded reads. (#644)
- Sharded CDC aggregates events into a unified stream while supporting per-shard streaming for specific consumers. (#646)
- Co-location configuration is validated at DI registration time to fail fast on mismatched shard keys. (#647)
- Online resharding is orchestrated as a six-phase workflow leveraging existing rebalancer, bulk operations, and CDC. (#648)
- Shadow sharding uses a decorator pattern to wrap existing routers without impacting the production code path. (#649)
- Time-based sharding uses a four-tier lifecycle (Hot, Warm, Cold, Archived) with automatic transitions. (#650)

#### Rules the project committed to

- Reference table replication uses XxHash64 for deterministic content hashing to detect changes. (#639)
- Cross-shard AVG aggregation must use two-phase logic (sum/count) to avoid mathematical errors from uneven shard sizes. (#640)
- Sharded CDC position tracking must use a composite key of shardId and connectorId to prevent data loss. (#646)
- Co-located entities must use the root entity's shard key value to ensure they reside on the same shard. (#647)
- Resharding state must be persisted after each phase to enable crash recovery and resume from the last completed step. (#648)
- Shadow writes must be fire-and-forget to ensure they never block or delay production responses. (#649)

#### Alternatives considered and rejected

- Row Level Security was considered but rejected for being database-specific and complex to manage. (#282)
- Database-level sharding solutions like Citus or Vitess were rejected to maintain portability. (#289)
- AutoMapper was rejected for ACLs because it handles property mapping but not the business logic and validation required for domain isolation. (#363)
- Sharing DTOs or events between modules was rejected to prevent tight coupling and domain concept leakage. (#363)
- Implementing Two-Phase Commit (2PC) was rejected as too complex, fragile, and incompatible with some databases like MongoDB. (#637)
- LINQ-based aggregation over materialized results was rejected to avoid wasting bandwidth on full data transfer. (#640)
- Manual string concatenation for shard keys was rejected due to brittleness and lack of partial key routing. (#641)

#### Things learned the hard way

- Time-based shard boundary calculations must be ISO 8601-compliant to handle period rollovers correctly. (#650)

#### No longer applicable

- MySQL Module Isolation tests are skipped due to pending Pomelo v10 support for the database-per-module simulation. (#548)

### Web, APIs and cloud hosting

#### Decisions

- API versioning is handled at the HTTP/Controller layer using Asp.Versioning rather than introducing version-aware routing for internal CQRS handlers. (#54)
- Activity results are wrapped in a serializable type to ensure compatibility with Durable Functions JSON serialization constraints. (#61)
- Cursor pagination uses a layered architecture to support both flat REST results and GraphQL Relay spec connections. (#336)

#### Alternatives considered and rejected

- A custom Encina.OpenApi package was rejected because .NET 10 native OpenAPI capabilities provide sufficient functionality without extra maintenance. (#48)

### Testing and the quality system

#### Decisions

- The project extended the existing Encina.Testing.Bogus package rather than creating a separate Encina.Testing.DataGeneration package for domain model faker support. (#161)
- Encina.Testing.Testcontainers was designed with a minimal API surface that manages only container lifecycle and exposes connection strings. (#162)
- Encina.Testing.WireMock was extended to include IAsyncLifetime implementation and an EncinaRefitMockFixture for Refit client testing. (#164)
- Architecture testing features were added to the existing Encina.Testing.Architecture package rather than creating a new package. (#166, #467)
- Streaming assertions for IAsyncEnumerable and fluent chaining via AndConstraint were added to the xUnit-based EitherAssertions. (#170)
- TUnit was selected as the testing framework for NativeAOT compatibility due to its source-generated test discovery and zero reflection usage. (#171)
- Mutation testing thresholds were set to 85% high, 70% low, and 60% break. (#172)
- Shouldly was adopted as the assertion library to avoid the commercial licensing costs associated with FluentAssertions. (#429)
- ArchUnitNET was selected for architecture testing over NetArchTest due to more active development and richer assertion syntax. (#432)
- Embedded C# Given/When/Then was chosen over SpecFlow to avoid Gherkin parsing overhead and external tooling dependencies. (#434)
- FsCheck was selected for property-based testing over Hedgehog because it has a larger .NET community despite Hedgehog's better shrinking. (#435)
- Mutation testing configuration is provided via external templates and CLI tools rather than being integrated into the Encina core library. (#437)
- Encina.Testing infrastructure was designated as the primary testing layer for all Encina packages to serve as living documentation. (#498)
- DomainModeling tests do not use EncinaTestFixture because the package lacks handlers. (#500)
- Testcontainers were retained for Oracle and complex existing tests, while Aspire Testing is used for PostgreSQL, SQL Server, and MySQL. (#502)
- Redis integration tests use Encina.Aspire.Testing with AddRedis, while Memory cache tests use FakeCacheProvider. (#503)
- NATS and MQTT transports retain Testcontainers for integration tests because they lack Aspire support. (#504)
- Resilience and Observability tests were refactored to adopt Encina.Testing.Bogus and Encina.Aspire.Testing. (#506)
- Property-based testing was selected for defining resilience invariants like retry and circuit breaker behaviors. (#506)
- EitherAssertions was adopted as the standard tool for validating Either results in validation provider tests. (#507)
- Encina.Testing.Fakes was chosen as the primary tool for testing Hangfire and Quartz scheduling packages. (#508)
- Testcontainers remains the primary infrastructure for component-level tests, while Aspire.Hosting.Testing is introduced for distributed scenarios. (#509)
- EF Core testing was expanded from a single generic provider to multiple specific database providers to match ADO and Dapper. (#539)
- Load tests were implemented specifically for Unit of Work, Multi-Tenancy, and Read/Write Separation, while other features were excluded. (#538)
- Benchmark tests were implemented only for Read/Write Separation because its replica selection algorithms are CPU-bound hot paths. (#540)
- Streaming assertions support early termination to handle infinite streams rather than forcing full materialization. (#529)
- Stryker mutate globs are project-relative (e.g., '**/*.cs') rather than solution-relative to ensure mutants are eligible. (#957)
- The mutation testing break threshold was set to 0 to prevent CI failures from realistic initial mutation scores. (#962)
- Stryker mutation runs use manual per-folder test-case filters derived from namespace conventions to scope tests to specific source folders. (#1027)
- Mutation testing runs all 17 source folders in parallel as a matrix job to ensure weekly data freshness for all shards. (#1028)

#### Rules the project committed to

- Messaging pattern test helpers must follow the Given/When/Then pattern established by AggregateTestBase. (#169)
- Encina must use Shouldly for testing assertions to maintain a fully open-source friendly license. (#495)
- Dogfooding refactors must replace direct assertions with EitherAssertions and manual setup with EncinaTestFixture. (#498)
- Core package tests must achieve 85% line and 80% branch coverage using Encina.Testing infrastructure. (#499)
- Messaging tests must use Encina.Testing.Fakes stores instead of manual mocks and must cover 85% lines and 80% branches. (#501)
- Integration tests for compliance modules must target real Marten/PostgreSQL via Docker/Testcontainers after ES migration, not InMemory stores. (#785, #786, #787, #788, #789, #790, #791, #792, #793)
- Modules with zero coverage must have integration, load, and benchmark tests formally justified via .md files if not implemented. (#899)
- Test projects must reference standardized Encina.Testing.* wrapper packages instead of raw libraries like FluentAssertions or Bogus. (#1023)

#### Alternatives considered and rejected

- PITest was rejected because it is Java-only and not applicable to .NET. (#172)
- OpenAPI/Swagger validation was rejected as a standalone solution because static schemas miss runtime behavior and state-dependent responses. (#436)
- Full migration to Aspire was rejected because it does not support Oracle, which is critical for enterprise customers. (#509)
- Modifying existing assertion methods was rejected to avoid breaking established behavior that expects full collection. (#529)
- Stryker perTest coverage analysis is unusable with xUnit v3 due to an upstream bug, forcing reliance on AllTests mode. (#962)

#### Things learned the hard way

- A .NET 10 JIT bug involving Conditional Escape Analysis causes CLR crashes with complex IAsyncEnumerable usage, fixable via the environment variable DOTNET_JitObjectStackAllocationConditionalEscape=0. (#5)
- NSubstitute cannot verify calls to static LoggerMessage delegates, requiring the use of custom test loggers or side-effect verification. (#6)
- The Encina.Testing.Verify package was already fully implemented with helpers like PrepareEither and EncinaErrorConverter before the related issue was closed. (#165)
- DistributedLock.Core 1.0.8 lacks .NET 10 assets, causing build failures for projects depending on Encina.Marten. (#494)
- gRPC integration tests required a fix for a reflection bug in GrpcEncinaService.SendAsync during dogfooding. (#505)
- Serverless and Scheduling tests rely on mocks rather than real infrastructure like Docker or Testcontainers. (#508)
- FsCheck's NonEmptyString generator can produce whitespace-only strings that fail ThrowIfNullOrWhiteSpace validation checks. (#510)
- NATS RequestAsync and MQTT subscription classes are difficult to mock with NSubstitute, requiring real broker integration tests for coverage. (#518)
- Several database integration test projects failed to compile due to missing Shouldly package references. (#519)
- Consolidating approximately 210 test projects into 7 caused unit tests to run sequentially, exceeding 2 hours in CI. (#530)
- Oracle database containers have a significantly slower startup time of 20-30 seconds compared to 2-5 seconds for other databases. (#541)
- GetOrAdd is unsafe for idempotent attestation because it may invoke the factory multiple times under contention; TryGetValue/TryAdd should be used. (#803)
- BenchmarkDotNet resolves relative artifact paths from the child process working directory, not the repository root, causing silent upload failures. (#923)
- Stryker.NET's 'solution' setting ignores the 'test-projects' allowlist, auto-discovering all test projects and causing phantom failures. (#957)

#### Changes of direction

- Integration tests for database features were required to use real databases via Testcontainers instead of justification documents. (#537)
- Load testing scope was expanded from database features to core messaging, caching, and distributed locking, with specific brokers deferred to later milestones. (#550)

#### No longer applicable

- FsCheck 2.x APIs are not compatible with FsCheck 3.x, requiring migration or removal of legacy generator code. (#116)
- MongoDB messaging store implementations exist in source but lacked integration test coverage, creating a parity gap with other providers. (#546)
- MongoDB read/write separation testing requires a replica set infrastructure rather than a single node, unlike SQL providers. (#547)

### CI, releases and repository process

#### Decisions

- A summary job named 'ci-result' aggregates CI outcomes, allowing branch protection to require a single check rather than all individual jobs. (#98)
- GitHub Actions templates were selected over Azure DevOps to align with the broader user base of Encina contributors. (#173)
- Domain vs Integration Events separation is consolidated into issue #312 as the primary tracking issue. (#470)
- Specification Pattern implementation is consolidated into issue #295 as the primary tracking issue. (#471)
- Issue #299 is designated as the primary issue for tracking the Anti-Corruption Layer pattern. (#474)
- The project mandates using solution filters (.slnf) instead of the full solution to prevent build crashes on the large codebase. (#496)
- Per-issue commits were preferred over a single mega-PR for implementing provider parity to facilitate code review. (#536)
- Issue #743 is the canonical issue for Secrets resilience patterns, superseding duplicate requests. (#795)
- The 'Encina.Secrets.*' package family was identified as duplicates of 'Encina.Security.Secrets.*' and required cleanup to resolve naming conflicts. (#1089)

**2026-09-24 pass:**

- The default worker model is Sonnet; Opus is reserved for unknown root causes or design-heavy tasks. (#1181)

#### Rules the project committed to

- Load tests are excluded from the standard CI pipeline to prevent crashes caused by the upstream .NET 10 JIT bug. (#5)
- Branch protection must apply to all users, including administrators, to ensure consistent enforcement of code quality and review requirements. (#98)
- Required status checks in branch protection must accurately reflect existing CI workflow jobs to prevent blocking merges with non-existent or retired checks. (#98)
- Directory.Build.rsp enforces -maxcpucount:1 and -nodeReuse:false to stabilize builds. (#496)
- GitHub Actions workflow permissions must be scoped at the job level rather than workflow level for least privilege. (#896)

**2026-09-24 pass:**

- No test suite may be excluded from CI without an open issue tracking the exclusion. (#1094)
- User-visible changes must be recorded as fragments in changelog.d/, enforced by CI scripts. (#1165)
- Workers must edit source files with the Edit/Write tools, never PowerShell `-replace` or `[IO.File]` writes. (#1181)

#### Alternatives considered and rejected

- A single mega-PR for all provider implementations was rejected due to its size making review difficult. (#536)
- Using the full Encina.slnx for builds was rejected in favor of solution filters due to instability. (#496)

#### Things learned the hard way

- The SonarCloud quality gate can report as failed due to stale data if the build has been broken for an extended period. (#75)
- Parallel MSBuild builds on the large Encina solution trigger intermittent Internal CLR error 0x80131506. (#496)
- Missing PublicAPI.Unshipped.txt entries can hide compilation errors in dependent packages by preventing their build phase. (#867)
- Dependabot nuget updates can exceed GitHub Actions 1-hour job timeouts in large repositories with Central Package Management, requiring job splitting. (#1042)
- NuGet audit warnings (NU1902/NU1904) are escalated to build errors by TreatWarningsAsErrors, causing CI failures if vulnerable transitive dependencies are not updated. (#1088)
- Projects can be intentionally excluded from the main solution file while still being built via ProjectReferences from test or integration projects. (#1089)

**2026-09-24 pass:**

- The Compliance test suite was previously excluded from CI shards, allowing significant test failures to go unnoticed. (#1094)
- Using PowerShell string replacement to edit source files can corrupt multiple files simultaneously. (#1181)

#### Changes of direction

- The comprehensive cross-cutting integration EPIC was superseded by smaller, milestone-specific EPICs (v0.13.5 through v0.19.0) to allow phased delivery. (#758)
- Digital Omnibus preparation work is tracked under the v0.16.1 milestone Epic rather than a standalone issue. (#809)

#### No longer applicable

- The monolithic v0.13.0 security epic was split into per-milestone EPICs (v0.13.0, v0.13.4, v0.13.5, v0.13.6, v0.16.1, v0.16.2). (#668)
- The issue was superseded by issue #820 which adopted proper project templates and labels. (#819)

### Documentation and developer experience

#### Decisions

- The CLI tool is built using System.CommandLine 2.0.1 and Spectre.Console for argument parsing and output. (#47)
- The project adopted a hybrid architecture where DocFX generates API references as flat HTML files that are integrated into the Jekyll-driven documentation site. (#91)
- The project documentation site uses the just-the-docs Jekyll theme for navigation and layout. (#914)

**2026-09-24 pass:**

- The quickstart documentation was designed to be completed in 5 minutes, prioritizing fast onboarding over comprehensive coverage. (#81)

#### Rules the project committed to

- DocFX configuration must explicitly exclude System and Microsoft namespaces and private members to prevent clutter and noise in the generated API reference. (#91)

#### Things learned the hard way

- Removing DocFX configuration for a Jekyll migration breaks the DocFX workflow if not explicitly reconfigured or removed, leaving the system in a state where no API docs are generated. (#91)
- The ModuleArchitectureAnalyzer may detect false positive dependencies between file-scoped modules due to namespace proximity or implicit transitive dependencies. (#497)

**2026-09-24 pass:**

- Writing the introduction and quickstart docs revealed significant documentation drift in the README and the patterns guide. (#80)

## Editorial corrections

Items the archaeology extracted as still relevant that later decisions superseded. They stay in the sections above as history; this list says what replaced them.

- **Solution filters (`.slnf`) are mandatory** (#496): superseded. No `.slnf` file exists; `CLAUDE.md` ("Build Environment Known Issues") records that the full `Encina.slnx` builds since the January 2026 test consolidation. `Directory.Build.rsp` with `-maxcpucount:1 -nodeReuse:false` is still in force.
- **85% line / 80% branch coverage targets** (#499, #501): superseded by the per-flag obligations model (`CLAUDE.md` "Per-Flag Coverage System", `docs/en/guides/TESTING.md`): targets are per package and per test type, declared in `.github/coverage-manifest/*.json`; branch coverage is not gated.
- **"13 supported providers"** (#286, #290): the database provider set is 10 since ADR-009 (Oracle removed) and ADR-024 (SQLite removed); `CLAUDE.md` "Multi-Provider Implementation Rule".
- **Mutation thresholds 85 / 70 / 60 and break at 0** (#172, #962): to be verified against the current Stryker configuration; the mutation methodology tracks per-file scores and has no project-wide target (`CLAUDE.md` "Mutation Testing System").
- **Aspire Testing versus Testcontainers split** (#502 to #509): the decision record is ADR-008; the items above are its history.
- **Encina.Secrets.* naming conflict** (#1089 as cited by #379's family): resolved by SPEC-000 DEC-001, `Encina.Security.Secrets.*` is canonical and `Encina.Secrets.*` was deleted (PR #1098).

## Candidates for promotion (human decision gate)

Decisions that are cited above but have no ADR, and rules the project follows that `CLAUDE.md` does not state. Each line is a proposal; the maintainer decides which to promote and which to leave as history.

### ADR candidates

| # | Decision | Evidence | Note |
|---|---|---|---|
| A1 | Marten is the event-sourcing provider; `Encina.EventStoreDB` is deprecated and excluded from new features | #17, #321 | ADR-019 covers the compliance modules only |
| A2 | Domain events and integration events are distinct concepts with a mapper; integration events leave the process only through the Outbox | #312, #373, #384 | Not recorded anywhere outside the issues |
| A3 | Recoverability classifies failures as transient, permanent or unknown through `IErrorClassifier`; permanent failures go to the dead-letter queue | #39 | |
| A4 | Message and field encryption live at the serializer level with AES-256-GCM and the `ENC:v1:{Algorithm}:{KeyId}:{Nonce}:{Tag}:{Cipher}` format; key management is a separate package per backend | #129, #396 | |
| A5 | Abstraction policy for provider families: no common Strategy where features differ (sagas, transports), a Strategy where the backends are fundamentally different (event sourcing) | #15, #16, #18 | Complements ADR-007 |
| A6 | API versioning belongs to the HTTP layer (`Asp.Versioning`), not to handler routing | #54 | Relevant to the open #389 and #458 |
| A7 | The standard `TimeProvider` replaces any custom clock and is injected into every store and repository | #433, #543, #667 | Also a rule candidate (R2) |
| A8 | Cross-shard writes coordinate through the Saga pattern, not two-phase commit | #637 | Check whether ADR-010 already states it |

### Rule candidates for `CLAUDE.md`

| # | Rule | Evidence | Where it would go |
|---|---|---|---|
| R1 | Assertions use Shouldly through `Encina.Testing.Shouldly`; test projects reference the `Encina.Testing.*` wrappers, never FluentAssertions or raw Bogus (licensing and dogfooding) | #429, #495, #1023 | Testing Standards |
| R2 | Production code never reads `DateTime.UtcNow`; it takes `TimeProvider` by injection | #543, #667 | Code Quality Standards |
| R3 | Options classes that hold secrets mark them `[JsonIgnore]` and override `ToString()` | #851 | Code Quality Standards / Security |
| R4 | Database providers call the async ADO overloads with a `CancellationToken` (`OpenAsync`, `ExecuteNonQueryAsync`, `BeginTransactionAsync`); Sonar S6966 | #794, #897 | Multi-Provider Implementation Rule |
| R5 | GitHub Actions permissions are declared per job, never at workflow level | #896 | Git Workflow / CI |
| R6 | Event-sourced compliance modules have no InMemory stores; unit tests mock `IAggregateRepository`, integration tests run against Marten in Testcontainers | #777, #783, #784, #785 | Testing Standards (relevant to #1095) |
| R7 | Marten projections receive dependencies through `IDocumentOperations` and constructor injection, never `IServiceProvider` | #949 | Event sourcing rules (open audit #952) |
| R8 | Feature-specific stores live in a subfolder named after the feature, not after the source package | #413 | Naming Conventions |
| R9 | Load tests are excluded from the standard CI pipeline (upstream .NET 10 JIT crash); `Directory.Build.rsp` keeps `-maxcpucount:1 -nodeReuse:false` | #5, #496 | Build Environment Known Issues |