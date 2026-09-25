# AGENTS.md traceability

This is a reference page (Diátaxis: reference, applied to the documentation set itself), for a contributor or agent who needs to confirm where a rule from the old root `CLAUDE.md` now lives after the 2026-09-25 split (#1353). The old file is frozen at [the engineering handbook](ENGINEERING-HANDBOOK.md); it is not maintained and its relative links were not fixed. The table below maps every `##`, `###` and `####` heading of that frozen snapshot, and every distinct rule inside a heading, to its destination: a section of the new root [`AGENTS.md`](../../AGENTS.md), a section of [`CLAUDE.md`](../../CLAUDE.md) (Claude Code specifics: Active Plans, orchestration, model routing, the closed-issue audit, CodeRabbit), a skill, an ADR or SPEC, or `Handbook-only` when the content is narrative, an example or history that was never itself a rule. `Handbook-only` is never used for something an agent or contributor is bound by; if a row says `Handbook-only`, the rule it illustrated is captured, restated, in a row above or below that points at `AGENTS.md` or `CLAUDE.md`.

The table was drafted by the local model (Qwen, through `tools/ai/local-ai-ask.cs`, four calls — one per quarter of the handbook) and then checked and completed by hand against the actual headings of `AGENTS.md` and `CLAUDE.md`.

| Old CLAUDE.md section | Destination | Note |
|---|---|---|
| ## Active Plans | CLAUDE.md | Claude Code specifics |
| ## Project Philosophy | Handbook-only | Section header |
| ### Pre-1.0 Development Status | AGENTS.md §1 | Project facts |
| ### Design Principles | AGENTS.md §1 | Project facts |
| ### Technology Stack | AGENTS.md §1 | Project facts |
| ### Scripting & Tooling Policy (MANDATORY) | AGENTS.md §2 | Scripting and tooling |
| ### Code Quality Standards | AGENTS.md §3 | Code rules |
| "Time comes from `TimeProvider`" | AGENTS.md §3 | TimeProvider rule |
| "Options that hold secrets never leak them" | AGENTS.md §3 | Secrets rule |
| "Database calls are asynchronous with a CancellationToken" | AGENTS.md §3 | Async DB calls rule |
| "Registration completeness: AddEncina* registers every option/dependency" | AGENTS.md §3 | Registration completeness rule |
| "Errors are never swallowed in background infrastructure" | AGENTS.md §3 | Error swallowing rule |
| "Compliance and security gates fail closed" | AGENTS.md §3 | Fail-closed gates rule |
| "EncinaError.Message never reaches logs/activity tags/health checks/plaintext storage" | AGENTS.md §3 | EncinaError.Message rule |
| ### Architecture Decisions | Handbook-only | Section header |
| #### Railway Oriented Programming (ROP) | AGENTS.md §3 | ROP pattern |
| #### Messaging Patterns (All Optional) | AGENTS.md §3 | Messaging patterns |
| #### Provider Coherence | AGENTS.md §3 | Provider coherence rule |
| #### Multi-Provider Implementation Rule (MANDATORY) | AGENTS.md §5 | Multi-provider rule |
| "The 10 Providers table" | AGENTS.md §5 | 10 database providers |
| #### Specialized Provider Categories (Beyond the 10 Database Providers) | AGENTS.md §5 | Specialized categories |
| ##### 1. Caching Providers (8 providers) - table + rules + required support | AGENTS.md §5 | Caching providers |
| ##### 2. Messaging Transport Providers (10 existing + 6 planned) - table + planned transports + rules + required support | AGENTS.md §5 | Transport providers |
| ##### 3. Distributed Lock Providers (3 existing + 7 planned; 1.0 ships 5) - table + 1.0 scope + rules + required support | AGENTS.md §5 | Lock providers |
| ##### 4. Validation Providers (3 providers) - table + rules + required support | AGENTS.md §5 | Validation providers |
| ##### 5. Scheduling Providers (2 + adapters) - table + rules | AGENTS.md §5 | Scheduling providers |
| ##### 6. Event Sourcing Providers (1 primary) - table + rules + event-sourced module rules (#777, #783, #784, #785, #949; ADR-019) | AGENTS.md §5 | Event sourcing providers |
| ##### 7. Cloud/Serverless Providers (3 providers) - table + cloud provider triangle rule (AWS+Azure shipped, GCP post-1.0, SPEC-000 DEC-003) | AGENTS.md §5 | Cloud providers |
| ##### 8. Resilience Providers (3 providers) - table | AGENTS.md §5 | Resilience providers |
| ##### 9. Observability Providers (1 + exporters) - table | AGENTS.md §5 | Observability providers |
| ##### 10. Testing Providers (12 packages) - table | AGENTS.md §5 | Testing providers |
| #### Provider Applicability Matrix | AGENTS.md §5 | Applicability matrix |
| #### When to Consider Each Provider Category | Handbook-only | Scenario table is narrative/example |
| #### Cross-Cutting Integration Rule (MANDATORY) | AGENTS.md §6 | Cross-cutting integration check |
| "The 12 Transversal Functions table" | AGENTS.md §6 | 12 transversal functions |
| #### Opt-In Configuration | Handbook-only | Example/config sample |
| #### Repository Pattern (Optional) | AGENTS.md §3 | Repository pattern |
| ### Naming Conventions | AGENTS.md §4 | Naming conventions |
| #### Messaging Entities | AGENTS.md §4 | Entities naming |
| #### Property Names (Standardized) | AGENTS.md §4 | Property names |
| #### Store Implementations | AGENTS.md §4 | Store implementations |
| #### Feature Folders | AGENTS.md §4 | Feature folders |
| ### Satellite Packages Philosophy | Handbook-only | Section header |
| #### Coherence Across Providers | AGENTS.md §3 | Provider coherence rule |
| #### Validation Libraries Support | AGENTS.md §5 | Validation providers |
| #### Validation Architecture (Orchestrator Pattern) | AGENTS.md §3 | Validation orchestrator |
| `### Testing Standards` | AGENTS.md §9 | Intro paragraph moved to Handbook-only |
| `#### Coverage Targets` | AGENTS.md §9 | |
| `#### Per-Flag Coverage System (Obligations Model) — CRITICAL` | AGENTS.md §9 | |
| `#### Test Types - Apply Where Appropriate` | AGENTS.md §9 | |
| `#### Mutation Testing System` | AGENTS.md §9 | |
| `#### Test Quality Standards` | AGENTS.md §9 | |
| `#### Docker Integration Testing` | Handbook-only | Example/code sample dropped |
| `#### Collection Fixtures (Container Reduction Strategy)` | AGENTS.md §9 | |
| `#### Test Organization` | AGENTS.md §9 | |
| `#### Test Coverage for All 10 Providers` | AGENTS.md §9 | Cross-ref to §5 |
| `#### Test Type Guidelines by Feature Category` | AGENTS.md §9 | |
| `#### IntegrationTests for Database Features` | AGENTS.md §9 | |
| `#### LoadTests Guidelines` | AGENTS.md §9 | |
| `#### BenchmarkTests Guidelines` | AGENTS.md §9 | |
| `#### BenchmarkDotNet Guidelines` | AGENTS.md §9 | |
| `#### Test Justification Documents (.md)` | AGENTS.md §9 | |
| `#### Supported Test Types (Encina.{Type}Tests)` | AGENTS.md §9 | |
| `#### Testing Workflow` | AGENTS.md §9 | |
| `#### Examples of Complete Test Coverage` | Handbook-only | Code examples dropped |
| `#### Test Data Management` | Handbook-only | Example dropped |
| `#### Test Output Conventions` | AGENTS.md §9 | |
| `#### Remember` | Handbook-only | Narrative dropped |
| `### Structured Logging & EventId Allocation (MANDATORY)` | AGENTS.md §7 | |
| `#### Central Registry` | AGENTS.md §7 | |
| `#### Current Range Map (Quick Reference)` | AGENTS.md §7 | |
| `#### Allocation Workflow (When Adding Structured Logging to a Feature)` | AGENTS.md §7 | |
| `#### Rules` | AGENTS.md §7 | |
| `#### Architecture Test Enforcement` | AGENTS.md §7 | |
| `### Code Analysis` | AGENTS.md §8 | |
| `### .NET 10 / C# 14 Reference (Released November 2025)` | AGENTS.md §1 | Historical notes moved to Handbook-only |
| `#### C# 14 New Features` | Handbook-only | Reference info only |
| `#### .NET 10 Breaking Changes` | Handbook-only | Reference info only |
| `#### PublicAPI Analyzers (RS0016/RS0017)` | AGENTS.md §8 | |
| `#### Official Documentation Links` | Handbook-only | External links dropped |
| `### Documentation` | AGENTS.md §8 | |
| `### Git Workflow` | AGENTS.md §10 | |
| `### Build Environment Known Issues` | AGENTS.md §8 | Historical notes condensed |
| `### Spanish/English` | AGENTS.md §10 | |
| ## Quick Reference | Handbook-only | Header row |
| ### When to Use Each Pattern | AGENTS.md §3 | "pattern choice quick reference" |
| ### Scheduling vs Hangfire/Quartz | AGENTS.md §5 | specialized scheduling category |
| ### Common Errors to Avoid | AGENTS.md §2 | prohibited commands |
| - no Obsolete, no migration helpers | AGENTS.md §2 | specific prohibition |
| - .NET 10 only | AGENTS.md §1 | technology stack |
| - ErrorMessage naming | AGENTS.md §3 | EncinaError.Message |
| - opt-in patterns | AGENTS.md §3 | messaging patterns |
| - no mixing provider code | AGENTS.md §5 | provider coherence |
| - no compromise for legacy | AGENTS.md §1 | design principles |
| - all 10 DB providers required | AGENTS.md §5 | the 10 database providers |
| - all 8 caching providers required | AGENTS.md §5 | specialized caching category |
| - all transports considered | AGENTS.md §5 | specialized transports category |
| - AWS/Azure/GCP triangle | AGENTS.md §5 | specialized cloud category |
| - justification .md files | AGENTS.md §9 | justification documents |
| - per-flag coverage targets | AGENTS.md §9 | per-flag obligations model |
| - BenchmarkSwitcher not BenchmarkRunner | AGENTS.md §9 | BenchmarkDotNet rules |
| - materialize IQueryable | AGENTS.md §3 | async DB calls |
| - EventId range registration | AGENTS.md §7 | range map |
| - no sparse EventId allocations | AGENTS.md §7 | allocation workflow |
| ### Remember | AGENTS.md §1 | pre-1.0 status |
| ## Issue Tracking & Project Documentation | Handbook-only | Intro narrative |
| ### GitHub Issues (Primary Issue Tracker) | AGENTS.md §11 | issue tracking policy |
| #### Issue Templates | AGENTS.md §11 | issue templates |
| - "Choosing the right template" scenario table | Handbook-only | example |
| - Prefix normalization rules | AGENTS.md §11 | issue body format |
| #### When to Create Issues | AGENTS.md §11 | workflow |
| - Spanish paragraph | AGENTS.md §11 | workflow |
| - Bulleted list: bugs found -> BUG immediately | AGENTS.md §11 | workflow |
| #### Issue Body Format (MANDATORY) | AGENTS.md §11 | issue body format |
| - Feature plans: FEATURE issue of any size gets implementation plan | AGENTS.md §11 | workflow |
| #### Workflow | AGENTS.md §11 | workflow |
| ### Project Documentation Files | AGENTS.md §11 | project documentation files |
| ### When Updating Documentation | AGENTS.md §11 | changelog rules |
| - After completing a feature -> changelog.d fragment | AGENTS.md §11 | changelog rules |
| - After major implementation phase -> update docs/releases/vX.Y.Z/README.md | AGENTS.md §11 | project documentation files |
| - After architectural decisions -> create ADR | AGENTS.md §11 | project documentation files |
| - After releasing -> changelog-fragments.cs script command | AGENTS.md §11 | changelog rules |
| ### DO NOT Track Issues Here | Handbook-only | narrative |
| ## CodeRabbit Integration | CLAUDE.md | Claude Code specific integration |
| ### CodeRabbit Features | CLAUDE.md | Claude Code specific integration |
| #### 1. Pull Request Reviews | CLAUDE.md | Claude Code specific integration |
| #### 2. Issue Enrichment (Automatic) | CLAUDE.md | Claude Code specific integration |
| #### 3. Linked Issue Validation | CLAUDE.md | Claude Code specific integration |
| #### 4. Plan Mode (Implementation Planning) | CLAUDE.md | Claude Code specific integration |
| #### 5. Configuration (.coderabbit.yaml) | CLAUDE.md | Claude Code specific integration |
| ### Workflow: Issues + CodeRabbit + Claude Code | CLAUDE.md | Claude Code specific integration |
| ### Tips for Effective CodeRabbit Usage | CLAUDE.md | Claude Code specific integration |
| ### Requesting CodeRabbit Analysis on Existing Issues | CLAUDE.md | Claude Code specific integration |
