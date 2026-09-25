# AGENTS.md traceability

This is a reference page (Diátaxis: reference, applied to the documentation set itself), for a contributor or agent who needs to confirm where a rule from the old root `CLAUDE.md` now lives after the 2026-09-25 split (#1353). The old file is frozen at [the engineering handbook](ENGINEERING-HANDBOOK.md); it is not maintained and its relative links were not fixed. The table below maps every `##`, `###` and `####` heading of that frozen snapshot, and every distinct rule inside a heading, to a one-line summary of the rule and its destination: a section of the new root [`AGENTS.md`](../../AGENTS.md), a section of [`CLAUDE.md`](../../CLAUDE.md) (Claude Code specifics: Active Plans, orchestration, model routing, the closed-issue audit, CodeRabbit), a skill, an ADR or SPEC, or `Handbook-only` when the content is narrative, an example or history that was never itself a rule. `Handbook-only` is never used for something an agent or contributor is bound by; if a row says `Handbook-only`, the rule it illustrated is captured, restated, in a row above or below that points at `AGENTS.md` or `CLAUDE.md`.

The table was drafted by the local model (Qwen, through `tools/ai/local-ai-ask.cs`, four calls — one per quarter of the handbook) and then checked and completed by hand against the actual headings of `AGENTS.md` and `CLAUDE.md`.

| Old section | Rule (short) | Destination | Note |
|---|---|---|---|
| Active Plans | Check the active plans in `docs/plans/` before starting work | AGENTS.md §1; CLAUDE.md "Active Plans" | The plans table itself lives in CLAUDE.md (maintainer decision) |
| Active Plans | Active plans table | CLAUDE.md "Active Plans" | Copied verbatim |
| Project Philosophy | Section heading | AGENTS.md §1 | |
| Project Philosophy > Pre-1.0 Development Status | Pre-1.0: no backward compatibility, breaking changes encouraged, no migration support, rename after 1.0 | AGENTS.md §1 | |
| Project Philosophy > Design Principles | Best solution first; clean architecture; pay-for-what-you-use; provider-agnostic; .NET 10 only | AGENTS.md §1 | |
| Project Philosophy > Technology Stack | .NET 10 only; latest C# features; .NET 10 breaking changes acceptable; nullable everywhere | AGENTS.md §1 | |
| Project Philosophy > Scripting & Tooling Policy (MANDATORY) | Only PowerShell or C# 14 file-based scripts; python and bash prohibited | AGENTS.md §2 | |
| Scripting & Tooling Policy > Allowed Execution Methods | PowerShell, C# 14 scripts, direct CLI invocation | AGENTS.md §2 | |
| Scripting & Tooling Policy > Prohibited | No python, bash constructs, Unix commands, here-docs, `sh -c`/`bash -c` | AGENTS.md §2 | Full command list kept |
| Scripting & Tooling Policy > PowerShell Equivalents for Common Operations | Equivalents table | AGENTS.md §2 (one line); handbook for the full table | The most used equivalents are inline |
| Scripting & Tooling Policy > C# 14 Script Examples (No .csproj Required) | Script examples | Handbook-only | Example |
| Scripting & Tooling Policy > Why This Policy Exists | Rationale of the policy | Handbook-only | Narrative; AGENTS.md §2 points to it |
| Project Philosophy > Code Quality Standards | No `[Obsolete]`, legacy code or migration paths; every line serves a purpose | AGENTS.md §3 | |
| Code Quality Standards | Time comes from `TimeProvider` | AGENTS.md §3 | #543, #667 |
| Code Quality Standards | Options holding secrets: `[JsonIgnore]` and `ToString()` override | AGENTS.md §3 | #851 |
| Code Quality Standards | Async database calls with `CancellationToken` | AGENTS.md §3 | S6966; #794, #897 |
| Code Quality Standards | Registration completeness, DI test with `ValidateOnBuild`/`ValidateScopes`; database store wins over in-memory default | AGENTS.md §3 | #1260, #1273, #1285, #1289; #1269, #1295 |
| Code Quality Standards | Errors never swallowed in background infrastructure | AGENTS.md §3 | #1150-#1153, #1184 |
| Code Quality Standards | Compliance and security gates fail closed | AGENTS.md §3 | SPEC-002 DEC-006 |
| Code Quality Standards | `EncinaError.Message` never reaches logs, tags, health checks or plaintext storage | AGENTS.md §3 | #1168, #1173, #1259, #1274 |
| Project Philosophy > Architecture Decisions | Section heading | AGENTS.md §3, §5 | |
| Architecture Decisions > Railway Oriented Programming (ROP) | `Either<EncinaError, T>`; no exceptions for business logic; validation returns `Either` | AGENTS.md §3 | ADR-001, ADR-006 |
| Architecture Decisions > Messaging Patterns (All Optional) | Outbox, Inbox, Saga, Scheduling, Transactions are optional | AGENTS.md §3 | |
| Architecture Decisions > Provider Coherence | Shared abstractions in `Encina.Messaging`; same interfaces, different implementations | AGENTS.md §3 | |
| Architecture Decisions > Multi-Provider Implementation Rule (MANDATORY) | Every provider-dependent feature for all 10 database providers; where the rule applies | AGENTS.md §5 | |
| Multi-Provider Implementation Rule | The 10 providers table | AGENTS.md §5 | |
| Multi-Provider Implementation Rule | Oracle and SQLite out of the matrix | AGENTS.md §5 | ADR-009, ADR-024 |
| Multi-Provider Implementation Rule | Provider-specific SQL differences | AGENTS.md §5 (SQL notes column) | |
| Multi-Provider Implementation Rule | Brokers, caching and event sourcing excluded from the database rule | AGENTS.md §5 | |
| Architecture Decisions > Specialized Provider Categories (Beyond the 10 Database Providers) | Each category has its own coherence rules | AGENTS.md §5 | |
| Specialized Provider Categories > 1. Caching Providers (8 providers) | 8 providers; when the rules apply; Get/Set/Remove, TTL, serialization, backplane | AGENTS.md §5 | 1.0 scope from SPEC-000 REQ-027 |
| Specialized Provider Categories > 2. Messaging Transport Providers (10 existing + 6 planned) | 10 + 6 planned; when the rules apply; Send/Publish, subscriptions, DLQ, metadata | AGENTS.md §5 | |
| Specialized Provider Categories > 3. Distributed Lock Providers | 1.0 set is exactly 5; post-1.0 list; TryAcquire with timeout, auto-release, cancellation | AGENTS.md §5 | #207, #208, SPEC-000 DEC-003 |
| Specialized Provider Categories > 4. Validation Providers (3 providers) | 3 providers; orchestrator integration, `ValidationResult`, same behavior | AGENTS.md §5 | |
| Specialized Provider Categories > 5. Scheduling Providers (2 + adapters) | Built-in, Hangfire, Quartz; `IScheduledMessageStore` and adapters | AGENTS.md §5 ("Other categories") | |
| Specialized Provider Categories > 6. Event Sourcing Providers (1 primary) | Marten; when the rules apply | AGENTS.md §5 ("Other categories") | |
| Specialized Provider Categories > 6. Event Sourcing Providers | No InMemory stores for event-sourced compliance modules; projections without `IServiceProvider`; one real-store projection test | AGENTS.md §3 | #777, #783-#785, #949; ADR-019 |
| Specialized Provider Categories > 7. Cloud/Serverless Providers (3 providers) | Triangle rule; 1.0 = AWS + Azure; GCP post-1.0, gap noted in the issue | AGENTS.md §5 | #205, SPEC-000 DEC-003 |
| Specialized Provider Categories > 8. Resilience Providers (3 providers) | Polly, Extensions.Resilience, Extensions.Http.Resilience | AGENTS.md §5 ("Other categories") | SPEC-002 P-30 notes the Http package does not exist yet |
| Specialized Provider Categories > 9. Observability Providers (1 + exporters) | OpenTelemetry and planned exporters | AGENTS.md §5 ("Other categories") | |
| Specialized Provider Categories > 10. Testing Providers (12 packages) | The 12 testing packages | AGENTS.md §5 ("Other categories") | |
| Architecture Decisions > Provider Applicability Matrix | Feature to provider category matrix | AGENTS.md §5 | Rows with identical columns merged |
| Architecture Decisions > When to Consider Each Provider Category | Scenario to providers; rule of thumb | AGENTS.md §5 ("Rule of thumb") | |
| Architecture Decisions > Cross-Cutting Integration Rule (MANDATORY) | Evaluate every feature against the 12 functions; Integrate, Defer or Not applicable | AGENTS.md §6 | ADR-018 |
| Cross-Cutting Integration Rule | The 12 functions table | AGENTS.md §6 | |
| Cross-Cutting Integration Rule | Common misses table | AGENTS.md §6 | |
| Architecture Decisions > Opt-In Configuration | Every messaging pattern disabled by default | AGENTS.md §3 | Example kept as one line |
| Architecture Decisions > Repository Pattern (Optional) | Repository optional, never forced; when to consider it | AGENTS.md §3 | Decision table condensed; philosophy in the handbook |
| Project Philosophy > Naming Conventions | Section heading | AGENTS.md §4 | |
| Naming Conventions > Messaging Entities | `OutboxMessage`, `InboxMessage`, `SagaState`, `ScheduledMessage` | AGENTS.md §4 | |
| Naming Conventions > Property Names (Standardized) | `RequestType`, `ErrorMessage`, `AtUtc`, `RetryCount`, descriptive ids | AGENTS.md §4 | |
| Naming Conventions > Store Implementations | `{Pattern}Store{Provider}` | AGENTS.md §4 | |
| Naming Conventions > Feature Folders | Folder named after the feature | AGENTS.md §4 | #413 |
| Project Philosophy > Satellite Packages Philosophy | Section heading | AGENTS.md §3 | |
| Satellite Packages Philosophy > Coherence Across Providers | Same interfaces and options, provider-specific implementations, switch by DI registration | AGENTS.md §3 (provider coherence) | Example in the handbook |
| Satellite Packages Philosophy > Validation Libraries Support | Several validation libraries; similar pattern for scheduling adapters | AGENTS.md §3, §5 | |
| Satellite Packages Philosophy > Validation Architecture (Orchestrator Pattern) | Orchestrator pattern and registration methods | AGENTS.md §3 | |
| Project Philosophy > Testing Standards | Balance thoroughness with velocity | AGENTS.md §9 | |
| Testing Standards > Coverage Targets | Per-flag line coverage against the manifest; no project-wide %; branch/method not gated; mutation per file | AGENTS.md §9 | |
| Testing Standards > Per-Flag Coverage System (Obligations Model) — CRITICAL | Each flag reaches its own target; coverable lines differ per flag | AGENTS.md §9 | Mechanics in docs/testing/coverage-measurement-methodology.md |
| Per-Flag Coverage System | Citations: never type coverage figures, use covref markers | AGENTS.md §9 | SPEC-001 |
| Per-Flag Coverage System | Tests must execute real package code; no reflection-only tests | AGENTS.md §9 | |
| Per-Flag Coverage System | Check the manifest; do not push until all flags reach target; CI Full ~40 min; `dotnet test` locally | AGENTS.md §9 | |
| Testing Standards > Test Types - Apply Where Appropriate | The seven test types and what each covers | AGENTS.md §9 ("Test projects") | Old `tests/{Package}.Tests/` locations superseded by the consolidated projects listed in the same file |
| Testing Standards > Mutation Testing System | Folder/filter arrays updated together; filter in `stryker-config.json`; mutref citations | AGENTS.md §9 | #1027, #1028 |
| Mutation Testing System | Coverage analysis off, matrix, accumulation (facts and reasons) | Handbook, docs/testing/mutation-measurement-methodology.md | Narrative; AGENTS.md §9 points to both |
| Testing Standards > Test Quality Standards | Good tests and things to avoid | AGENTS.md §9 ("Test quality") | |
| Test Quality Standards | Shouldly via `Encina.Testing.Shouldly`; no FluentAssertions; `Encina.Testing.*` wrappers | AGENTS.md §9 | #429, #495, #1023 |
| Testing Standards > Docker Integration Testing | Docker profiles; run script | AGENTS.md §9 | Example test in the handbook |
| Testing Standards > Collection Fixtures (Container Reduction Strategy) | Shared `[Collection]` fixtures; collections table; rules 1-5 | AGENTS.md §9 ("Integration test fixtures") | Template example in the handbook and docs/testing/integration-tests.md |
| Testing Standards > Test Organization | Consolidated test projects | AGENTS.md §9 ("Test projects") | Test counts are history |
| Testing Standards > Test Coverage for All 10 Providers | Tests cover all 10 providers | AGENTS.md §9 | |
| Testing Standards > Test Type Guidelines by Feature Category | Required test types by category | AGENTS.md §9 (table) | |
| Testing Standards > IntegrationTests for Database Features | Real integration tests; never a justification file | AGENTS.md §9 (table, justification rules) | |
| Testing Standards > LoadTests Guidelines | Load tests only for concurrent features | AGENTS.md §9 (table) | |
| Testing Standards > BenchmarkTests Guidelines | Benchmarks only for hot paths | AGENTS.md §9 (table) | |
| Testing Standards > BenchmarkDotNet Guidelines | `BenchmarkSwitcher`; materialize `IQueryable`; verify the filter; entities match; output to `artifacts/performance/` | AGENTS.md §9 ("BenchmarkDotNet") | #564; argument table condensed |
| Testing Standards > Test Justification Documents (.md) | Path, when allowed, when never, required content, unevaluated folders | AGENTS.md §9 ("Justification files") | Template headings kept |
| Testing Standards > Supported Test Types (Encina.{Type}Tests) | Test projects and when required | AGENTS.md §9 | |
| Testing Standards > Testing Workflow | Workflow; commands before committing; CI enforcement | AGENTS.md §9 ("Workflow"), §8 (CI enforces) | |
| Testing Standards > Examples of Complete Test Coverage | OutboxStore example | Handbook-only | Example |
| Testing Standards > Test Data Management | Use builders | AGENTS.md §9 | Builder example in the handbook |
| Testing Standards > Test Output Conventions | Outputs under `artifacts/`; forbidden root outputs; runsettings and scripts | AGENTS.md §9 ("Outputs") | |
| Testing Standards > Remember | Balance quality with velocity | AGENTS.md §9 ("Workflow") | |
| Project Philosophy > Structured Logging & EventId Allocation (MANDATORY) | Register every EventId range before use | AGENTS.md §7 | ADR-021 |
| Structured Logging > Central Registry | Location, format, discovery | AGENTS.md §7 | |
| Structured Logging > Current Range Map (Quick Reference) | Area to range map | AGENTS.md §7 (free ranges; registry is the source of truth); handbook for the snapshot map | Table is a fact derivable from `EventIdRanges.cs` |
| Structured Logging > Allocation Workflow (When Adding Structured Logging to a Feature) | Five steps | AGENTS.md §7 | |
| Structured Logging > Rules | Never unregistered, never outside the range, never sparse; source generator; grouping; XML doc | AGENTS.md §7 | |
| Structured Logging > Architecture Test Enforcement | `EventIdUniquenessRule`; `AssemblyRanges` map; `LoggerMessage.Define` scan | AGENTS.md §7 | #1125 |
| Project Philosophy > Code Analysis | Zero warnings; CA1848, CA2263, CA1716 | AGENTS.md §8 | |
| Project Philosophy > .NET 10 / C# 14 Reference (Released November 2025) | .NET 10 LTS through November 2028 | AGENTS.md §1 | |
| .NET 10 / C# 14 Reference > C# 14 New Features | C# 14 features | AGENTS.md §1 | |
| .NET 10 / C# 14 Reference > .NET 10 Breaking Changes | Breaking changes | AGENTS.md §1 | |
| .NET 10 / C# 14 Reference > PublicAPI Analyzers (RS0016/RS0017) | RS0016, RS0017, RS0036/37, format | AGENTS.md §8 | |
| .NET 10 / C# 14 Reference > Official Documentation Links | Links | Handbook-only | Narrative |
| Project Philosophy > Documentation | XML comments; examples; package READMEs; ADRs | AGENTS.md §8 | |
| Project Philosophy > Git Workflow | No force push; English commits; no AI attribution; job-level permissions | AGENTS.md §10 | #896 |
| Project Philosophy > Build Environment Known Issues | Full `.slnx` builds; keep `Directory.Build.rsp` flags; load tests and the JIT workaround | AGENTS.md §8 | #5, #496 |
| Project Philosophy > Spanish/English | Spanish to the maintainer; English in code, docs, commits; translate Spanish comments | AGENTS.md §10 | |
| Quick Reference | Section heading | AGENTS.md §3 | |
| Quick Reference > When to Use Each Pattern | Pattern choice | AGENTS.md §3 ("Pattern choice") | |
| Quick Reference > Scheduling vs Hangfire/Quartz | Domain messages vs infrastructure jobs | AGENTS.md §3 | |
| Quick Reference > Common Errors to Avoid | Items 1-17 | AGENTS.md §1 (3), §3 (1, 2, 5, 6, 7), §4 (4), §5 (8-11), §9 (12-15), §7 (16, 17) | Every item is a duplicate of a rule stated earlier in the old file |
| Quick Reference > Remember | Best solution, not the compatible one | AGENTS.md §1 | |
| Issue Tracking & Project Documentation | Section heading | AGENTS.md §11 | |
| Issue Tracking > GitHub Issues (Primary Issue Tracker) | All bugs, features and debt are GitHub issues | AGENTS.md §11 | |
| GitHub Issues > Issue Templates | Templates table; choosing the template; prefix normalization | AGENTS.md §11 | Choosing table merged into the "Use for" column |
| GitHub Issues > When to Create Issues | Never leave a problem unresolved or unrecorded; open and continue; report opened issues | AGENTS.md §11 | Original paragraph in Spanish, translated |
| GitHub Issues > Issue Body Format (MANDATORY) | Headers verbatim, sections per template, house style; feature plan with the prompt | AGENTS.md §11 | #1050, #949 |
| GitHub Issues > Workflow | Issue, plan, assign, reference, auto-close | AGENTS.md §11 | |
| Issue Tracking > Project Documentation Files | CHANGELOG, changelog.d, ROADMAP, releases, ADRs, documentation roadmap | AGENTS.md §11 | |
| Issue Tracking > When Updating Documentation | Changelog fragments; releases README; ADRs; release folding by the maintainer | AGENTS.md §11 | |
| Issue Tracking > DO NOT Track Issues Here | No known issues or tracking in the rules file | AGENTS.md §11 | Applies to AGENTS.md and CLAUDE.md |
| CodeRabbit Integration | Section heading | CLAUDE.md "CodeRabbit" | |
| CodeRabbit Integration > CodeRabbit Features | Feature overview | CLAUDE.md "CodeRabbit" | |
| CodeRabbit Features > 1. Pull Request Reviews | Review commands | CLAUDE.md "CodeRabbit" | |
| CodeRabbit Features > 2. Issue Enrichment (Automatic) | Enrichment only on create or edit | CLAUDE.md "CodeRabbit" | |
| CodeRabbit Features > 3. Linked Issue Validation | Clear titles, acceptance criteria, consistent terms, `Fixes #N`/`Closes #N` | AGENTS.md §11; CLAUDE.md "CodeRabbit" | Tool-agnostic parts in AGENTS.md |
| CodeRabbit Features > 4. Plan Mode (Implementation Planning) | `@coderabbitai plan`, `plan-me` label | CLAUDE.md "CodeRabbit" | |
| CodeRabbit Features > 5. Configuration (`.coderabbit.yaml`) | Configuration file | CLAUDE.md "CodeRabbit" | YAML examples in the handbook |
| CodeRabbit Integration > Workflow: Issues + CodeRabbit + Claude Code | Seven-step workflow | CLAUDE.md "CodeRabbit"; AGENTS.md §11 | |
| CodeRabbit Integration > Tips for Effective CodeRabbit Usage | Detailed descriptions, conventional commits, reference issues, read enrichment, plan mode | CLAUDE.md "CodeRabbit"; AGENTS.md §11 | |
| CodeRabbit Integration > Requesting CodeRabbit Analysis on Existing Issues | Comment to trigger analysis and read it first | CLAUDE.md "CodeRabbit" | |
