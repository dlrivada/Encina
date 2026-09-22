# Implementation plan prompt

This is the prompt the maintainer uses to turn a `[FEATURE]` issue into an implementation plan under `docs/plans/`. It is versioned here so that every author, human or agent, produces plans with the same structure (see `docs/plans/dsr-implementation-plan-404.md` for the reference style and [How Encina is built](../HOW-ENCINA-IS-BUILT.md) for where planning sits in the method).

**How to use it**

1. Replace `{{ISSUE_URL}}`, `{{ISSUE_NUMBER}}` and `{{FEATURE_NAME}}` (kebab-case, e.g. `dsr`, `breach-notification`).
2. Run it with the agent of your choice from the repository root, so the mandatory research can read the code and `CLAUDE.md`.
3. Review the plan, commit it as `docs/plans/{{FEATURE_NAME}}-implementation-plan-{{ISSUE_NUMBER}}.md`, and link it from the issue.

Issue bodies themselves follow the templates in `.github/ISSUE_TEMPLATE/` (headers verbatim; see `CLAUDE.md`, "Issue Tracking & Project Documentation").

---

```text
Create a detailed implementation plan for issue {{ISSUE_URL}}.
To draft the plan, take into account the existing code and the issues that have already been completed. Also check whether any prerequisites are necessary or advisable, such as open issues or prior implementations, even if no issue has been created for them.
INSTRUCTIONS:
1. MANDATORY RESEARCH (before writing the plan):
   - Read the full issue body (gh issue view {{ISSUE_NUMBER}})
   - Read issue comments for CodeRabbit plans or relevant discussion
   - Read CLAUDE.md thoroughly — understand project philosophy, architecture, provider categories, testing standards, and naming conventions
   - Explore existing code in the area that corresponds to identify established patterns (interfaces, stores, behaviors, diagnostics, DI registrations)
   - Identify which provider category applies (see "Provider Applicability Matrix" and "Specialized Provider Categories" in CLAUDE.md):
     - Database (13): ADO.NET ×4, Dapper ×4, EF Core ×4, MongoDB
     - Caching (8): Memory, Hybrid, Redis, Valkey, Dragonfly, Garnet, KeyDB, Memcached
     - Messaging Transport (10+): RabbitMQ, AzureServiceBus, AmazonSQS, Kafka, NATS, Redis.PubSub, MQTT, InMemory, gRPC, GraphQL
     - Distributed Lock (4+): InMemory, Redis, SqlServer, PostgreSQL...
     - Validation (3): FluentValidation, DataAnnotations, MiniValidator
     - Scheduling (2+): Built-in, Hangfire, Quartz
     - Cloud (3): AzureFunctions, AwsLambda, GoogleCloudFunctions
     - Resilience (3): Polly, Extensions.Resilience, Extensions.Http.Resilience
     - Observability (1+): OpenTelemetry + exporters
     - Testing (12): Testing, Testing.Fakes, Testing.Respawn, etc.
     - Or none — some features are provider-independent
   - Verify Event ID ranges already in use to avoid collisions (grep for EventId patterns in Diagnostics/ folders)
   - Understand which existing modules the feature integrates with
   - Evaluate the feature against ALL 12 transversal functions defined in CLAUDE.md "Cross-Cutting Integration Rule" (Caching, OpenTelemetry, Structured Logging, Health Checks, Validation, Resilience, Distributed Locks, Transactions, Idempotency, Multi-Tenancy, Module Isolation, Audit Trail). For each, determine: ✅ Include in this implementation, ⏭️ Defer to separate issue, or ❌ N/A.
2. PLAN STRUCTURE (all sections are mandatory):
   a) SUMMARY
      - What it implements, which standards/specifications it covers (if applicable)
      - Estimated scope (lines of code, file count)
      - Affected packages
      - Which provider category applies (or none)
   b) DESIGN CHOICES (collapsible <details>, minimum 4 decisions)
      Each decision includes:
      - "Options Considered" table with Pros/Cons
      - "Chosen Option" with clear name
      - "Rationale" explaining why
      Typical decisions to consider (select what applies):
      - Package placement (new package vs extend existing)
      - Domain model design (records, enums, value objects)
      - Store/persistence pattern (if stateful)
      - Pipeline behavior design (if cross-cutting)
      - Extensibility strategy (strategy pattern, composite, decorator, etc.)
      - Integration with existing modules
      - Provider scope (which provider category, how many implementations)
      - Configuration model (options class design)
   c) IMPLEMENTATION PHASES (each phase with two collapsible blocks)
      Block 1: <details><summary>Tasks</summary>
      - Numbered list of files to create/modify
      - Class name, namespace, interfaces it implements
      - Key methods with signatures
      - Constructor dependencies
      Block 2: <details><summary>Prompt for AI Agents — Phase N</summary>
      - Code block with self-contained prompt to execute the phase
      - Includes CONTEXT, TASK, KEY RULES, REFERENCE FILES
      - The prompt must be autonomous (an agent can execute it without additional context)
      Adapt phases to the feature. Common phase patterns:
      FOR FEATURES WITH PERSISTENCE (database/caching stores):
      - Core Models & Enums
      - Core Interfaces & Abstractions
      - Default / In-Memory Implementations
      - Pipeline Behavior (if cross-cutting)
      - Configuration, DI & Auto-Registration
      - Persistence Entity, Mapper & Provider-Specific Scripts
      - Multi-Provider Implementations (all providers in the applicable category)
      - Cross-Cutting Integration
      - Observability
      - Testing
      - Documentation & Finalization
      FOR TRANSPORT/INTEGRATION FEATURES (messaging, cloud):
      - Core Models & Contracts
      - Transport Abstractions
      - Provider Implementations (per transport/cloud platform)
      - Configuration & DI
      - Error Handling & Dead Letter
      - Cross-Cutting Integration
      - Observability
      - Testing
      - Documentation & Finalization
      FOR CROSS-CUTTING FEATURES (resilience, validation, multi-tenancy):
      - Core Abstractions & Models
      - Default Implementation
      - Pipeline Behavior / Middleware
      - Provider Integrations (if applicable)
      - Configuration & DI
      - Cross-Cutting Integration
      - Observability
      - Testing
      - Documentation & Finalization
      FOR DEVELOPER EXPERIENCE / TOOLING FEATURES:
      - Core API Design
      - Implementation
      - CLI / Template Integration (if applicable)
      - Configuration & DI
      - Cross-Cutting Integration
      - Testing
      - Documentation & Finalization
      CROSS-CUTTING INTEGRATION PHASE (always present for non-trivial features):
      - Integrate with all transversal functions marked ✅ in section g)
      - Common integrations: TenantId/ModuleId propagation, distributed locks for background processors, caching for query paths, audit events for state changes, resilience for external calls, validation at system boundaries
      OBSERVABILITY PHASE (always present for non-trivial features):
      - ActivitySource named after the package
      - Meter with dimensional counters (Counter<long>) with tags
      - [LoggerMessage] source generator with Event IDs in a new non-colliding range
      TESTING PHASE (always present, adapt scope to feature complexity):
      - Unit Tests: mocks, AAA pattern, fast execution
      - Guard Tests: ArgumentNullException for all public parameters
      - Contract Tests: verify all provider implementations follow the same contract (if multi-provider)
      - Property Tests: FsCheck, invariants, round-trip (if complex domain logic)
      - Integration Tests: real infrastructure via [Collection] fixtures (if persistence/transport)
      - Load Tests: implement if concurrency matters, .md justification if not
      - Benchmark Tests: implement if hot paths exist, .md justification if not
      DOCUMENTATION PHASE (always last and all of this documents mandatory):
      - XML doc comments on all new public APIs (<summary>, <remarks>, <param>, <returns>, <example>)
      - CHANGELOG.md — add entry under Unreleased section (### Added / ### Changed / ### Fixed)
      - ROADMAP.md — update if milestone or planned feature is affected
      - Package README.md — update if package behavior changes or new package created
      - docs/features/*.md — feature-specific documentation (usage guide, configuration, examples)
      - docs/INVENTORY.md — update if new files, packages, or modules are added
      - docs/architecture/adr/*.md — create ADR if the feature involves significant architectural decisions (new patterns, technology choices, trade-offs that future contributors need to understand)
      - PublicAPI.Shipped.txt / PublicAPI.Unshipped.txt — ensure all public symbols are tracked
      - docs/releases/vX.Y.Z/ — update release notes if applicable to a version
      - Build verification: dotnet build --configuration Release → 0 errors, 0 warnings
      - Test verification: dotnet test → all pass, coverage target ≥85%
   d) RESEARCH
      - Table of relevant standards/specifications (GDPR articles, EIP patterns, cloud specs, etc.)
      - Table of existing Encina infrastructure to leverage (component, location, usage in this feature)
      - Table of Event ID allocation (package, range, notes)
      - Table of estimated file count by category
   e) COMBINED AI AGENT PROMPTS
      - Single <details> block with the combined prompt for all phases
      - Includes PROJECT CONTEXT, IMPLEMENTATION OVERVIEW, KEY PATTERNS, REFERENCE FILES
   f) CROSS-CUTTING INTEGRATION MATRIX
      - Evaluate against ALL 12 transversal functions from CLAUDE.md "Cross-Cutting Integration Rule"
      | # | Function | Status | Notes |
      |---|----------|--------|-------|
      | 1 | Caching | ✅/⏭️/❌ | Justification |
      | 2 | OpenTelemetry | ✅/⏭️/❌ | Justification |
      | 3 | Structured Logging | ✅/⏭️/❌ | Justification |
      | 4 | Health Checks | ✅/⏭️/❌ | Justification |
      | 5 | Validation | ✅/⏭️/❌ | Justification |
      | 6 | Resilience | ✅/⏭️/❌ | Justification |
      | 7 | Distributed Locks | ✅/⏭️/❌ | Justification |
      | 8 | Transactions | ✅/⏭️/❌ | Justification |
      | 9 | Idempotency | ✅/⏭️/❌ | Justification |
      | 10 | Multi-Tenancy | ✅/⏭️/❌ | Justification |
      | 11 | Module Isolation | ✅/⏭️/❌ | Justification |
      | 12 | Audit Trail | ✅/⏭️/❌ | Justification |
      - For ⏭️ items: note if a GitHub issue already exists or needs to be created
      - For ✅ items: ensure the integration is included in the implementation phases (section c)

3. ENCINA RULES (always apply — see CLAUDE.md for full details):
   - .NET 10 / C# 14, nullable enabled
   - ROP: Either<EncinaError, T> on all store/handler methods
   - Provider coherence: if a feature touches provider-specific code, implement across ALL providers in that category
   - Store naming: {Feature}Store{Provider} (e.g., OutboxStoreADO, CacheProviderRedis)
   - Satellite DI: AddEncina{Feature}{Provider}() with TryAdd* — called BEFORE the core registration
   - SQLite specifics: dates in ISO 8601 "O" format, never datetime('now'), never dispose shared connection in tests
   - Pipeline behaviors: static per-generic-type attribute caching, enforcement modes (Block/Warn/Disabled)
   - Health checks: DefaultName const, Tags static array, scoped resolution via IServiceProvider.CreateScope()
   - Integration tests: [Collection("Provider-DB")] shared fixtures, ClearAllDataAsync in InitializeAsync
   - Observability: ActivitySource + Meter + [LoggerMessage] source generator
   - XML documentation on all public APIs
   - No [Obsolete], no backward compatibility, no migration helpers
   - Pre-1.0: choose the best solution, not the compatible one
   - Cross-cutting integration: every feature MUST be evaluated against all 12 transversal functions (see section f)
4. OUTPUT:
   Write the complete plan to docs/plans/{{FEATURE_NAME}}-implementation-plan-{{ISSUE_NUMBER}}.md
   Use markdown with collapsible sections (<details>), tables, and code blocks.
5. FORMAT REFERENCE:
   Follow exactly the style of docs/plans/dsr-implementation-plan-404.md
```
