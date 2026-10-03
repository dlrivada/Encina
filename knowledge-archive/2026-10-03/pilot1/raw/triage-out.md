1|test|testing|Encina|delivered|Nine stream request and pipeline behavior tests failed due to missing DI handler registration.
2|bug|database|Encina.ADO.Oracle;Encina.ADO.Sqlite|delivered|SQL scripts for Oracle and SQLite packages incorrectly used SQL Server syntax preventing execution.
3|debt|observability|Encina.Wolverine;Encina.NServiceBus;Encina.RabbitMQ;Encina.AzureServiceBus;Encina.AmazonSQS;Encina.Kafka;Encina.Redis.PubSub;Encina.InMemory;Encina.NATS;Encina.MQTT;Encina.gRPC;Encina.GraphQL|delivered|Migrate logging in messaging packages to use LoggerMessage source generators for performance.
7|test|testing|Encina.Dapper.Sqlite;Encina.DataAnnotations;Encina.FluentValidation;Encina.Hangfire;Encina.MassTransit;Encina.MiniValidator;Encina.OpenTelemetry|delivered|Fifty-seven tests failed in CI due to validation and logging discrepancies across fifteen packages.
9|test|database|Encina.Dapper.Sqlite|delivered|Two contract tests failed because ScheduledMessage entity did not fully implement IScheduledMessage interface.
13|refactor|caching|Encina.Caching;Encina.Caching.Memory;Encina.Caching.Hybrid;Encina.Caching.Redis;Encina.Caching.Garnet;Encina.Caching.Valkey;Encina.Caching.Dragonfly;Encina.Caching.KeyDB|delivered|Apply Orchestrator pattern to centralize caching logic and reduce duplication across eight provider packages.
19|debt|testing|Encina|delivered|Increase code coverage threshold from 45% to 85% to meet quality standards.
21|feature|event-sourcing|Encina;Encina.Marten|delivered|Implement CQRS read-side abstractions including projections and read model stores for event sourcing.
949|bug|event-sourcing|Encina.Audit.Marten|delivered|Marten audit projections failed initialization because IServiceProvider parameter is rejected by JasperFx validation.
962|infra|mutation-testing|Encina|delivered|Widen mutation testing scope beyond smoke test and fix coverage capture performance issues.
1023|debt|testing|Encina.Testing|delivered|Migrate test projects to standardized Encina.Testing wrappers and update outdated NuGet package dependencies.
1027|infra|ci-cd|Encina|delivered|Pass per-folder test-case filters to Stryker to bypass xUnit v3 coverage analysis bug.
1042|infra|ci-cd|Encina|delivered|Dependabot NuGet updater job hits one-hour timeout preventing automated security advisory updates.
1050|debt|observability|Encina.OpenTelemetry;Encina|delivered|Register EventId range 7000-7099 for OpenTelemetry resharding logs in the central registry.
1155|debt|security|Encina.Security.AntiTampering|delivered|Make HMAC validation fail closed by default and require explicit opt-out to skip without HttpContext.
1160|bug|compliance|Encina.Compliance.Retention;Encina.Compliance.DataSubjectRights|delivered|Retention enforcement incorrectly erased all data categories of an entity instead of only expired ones.
1163|bug|core|Encina;Encina.ADO;Encina.Dapper;Encina.MongoDB;Encina.Security.ABAC;Encina.Security.Secrets;Encina.Compliance.NIS2|delivered|Twenty-eight call sites resolve unregistered IRequestContext from DI resulting in null values for identity.
1181|infra|agent-system|Encina|delivered|Implement nine improvements to the AI agent system including model choice, hooks, and turn limits.
1262|debt|database|Encina.EntityFrameworkCore|delivered|Remove dead code dispatching to unsupported Oracle and SQLite providers in bulk operations implementation.
1273|bug|mongodb|Encina.MongoDB|delivered|AddEncinaMongoDB registers InboxOrchestrator without registering the required InboxOptions dependency.