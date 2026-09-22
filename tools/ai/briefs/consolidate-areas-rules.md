Assign every knowledge item listed at the end of this message to exactly one area of the Encina repository and mark duplicates. Follow every numbered point without omitting any. Output ONLY a JSON array, nothing else: no prose, no Markdown fences.

1. Output format (strict): one object per item, in input order, exactly `{"id": "<item id>", "area": "<area code>", "duplicate_of": "<item id or empty string>"}`. Every input item appears exactly once.

2. Area codes (use exactly these strings):
   - core: request pipeline, handlers, behaviors, Either/ROP results, EncinaError, guard clauses, core DI
   - messaging: outbox, inbox, saga, scheduling, recoverability, dead letters, transports, message encryption
   - data: database providers (ADO, Dapper, EF Core, MongoDB), repositories, unit of work, specifications, SQL scripts, migrations
   - caching: cache providers, cache behaviors, invalidation, stampede, backplanes
   - eventsourcing: Marten, event store, aggregates, projections, snapshots, crypto-shredding
   - validation: validation providers and orchestration
   - observability: health checks, OpenTelemetry, structured logging, EventIds, dashboards for metrics
   - security-compliance: security packages, PII, secrets, audit, GDPR, NIS2, AI Act and other regulations
   - testing-quality: test types, coverage, mutation, benchmarks, load tests, testing packages, DocRef citations
   - ci-process: CI workflows, branch protection, release engineering, versioning, issue process, repository conventions
   - docs-dx: documentation site, guides, README, developer tooling, CLI, analyzers, source generators
   - web-cloud: ASP.NET Core integration, minimal APIs, OpenAPI, API versioning, gRPC, GraphQL, Azure Functions, AWS Lambda, Aspire
   - modules-tenancy: modular monolith, module isolation, multi-tenancy, sharding, read/write separation
   - resilience: Polly, retries, circuit breakers, bulkheads, timeouts, rate limiting

3. duplicate_of: when two items state the same fact (same decision, rule or gotcha, possibly from different issues), mark the later one as a duplicate of the earlier id; otherwise use an empty string. Different facts about the same topic are not duplicates.

4. Decide from the statement text only. Do not invent areas outside the list.

5. Stop when the JSON array is complete.

Items:
