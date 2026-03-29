# Encina Weighted Coverage Report

Generated: 2026-03-29T20:08:33Z

## Overall: 33.17% (44,099.8 / 132,960 weighted lines)

## By Category

| Category | Packages | Tests | Coverage | Target | Status |
|----------|:--------:|:-----:|:--------:|:------:|:------:|
| Provider | 11 | U+G+I | 26.7% | 50% | 🔴 |
| Full | 36 | U+G+C+P+I | 39.1% | 85% | 🔴 |
| TestingLibrary | 11 | U+G | 35.8% | 70% | 🔴 |
| Logic | 7 | U+G+P | 39.6% | 80% | 🔴 |
| Cloud | 8 | U+G | 35.9% | 60% | 🔴 |
| CDC | 6 | U+G+I | 37.5% | 60% | 🔴 |
| Transport | 7 | U+G | 43.0% | 75% | 🔴 |
| Tooling | 1 | U+G | 22.2% | 60% | 🔴 |
| Caching | 7 | U+G+I | 39.2% | 70% | 🔴 |
| DistributedLock | 4 | U+G+I | 46.8% | 70% | 🔴 |
| Validation | 2 | U+G+C | 47.6% | 80% | 🔴 |

## By Package

| Package | Category | Unit | Guard | Contract | Property | Integ | Combined | Target | Gap |
|---------|----------|:----:|:-----:|:--------:|:--------:|:-----:|:--------:|:------:|:---:|
| Encina.Marten | Provider | 34% | 3% | - | - | 1% | 16.6% | 50% | -33% |
| Encina.MongoDB | Provider | 23% | 2% | - | - | 27% | 20.2% | 50% | -30% |
| Encina.Audit.Marten | Provider | 33% | 2% | - | - | 0% | 20.8% | 50% | -29% |
| Encina.Testing.Testcontainers | TestingLibrary | 43% | 0% | - | - | - | 21.4% | 70% | -49% |
| Encina.Testing.Pact | TestingLibrary | 43% | 0% | - | - | - | 21.5% | 70% | -49% |
| Encina.Cli | Tooling | 41% | 3% | - | - | - | 22.2% | 60% | -38% |
| Encina.Cdc.Debezium | CDC | 43% | 4% | - | - | 0% | 23.0% | 60% | -37% |
| Encina.AspNetCore | Cloud | 52% | 0% | - | - | - | 23.8% | 60% | -36% |
| Encina.Marten.GDPR | Provider | 43% | 3% | - | - | 0% | 24.6% | 50% | -25% |
| Encina.Compliance.DataResidency | Full | 35% | 13% | 0% | 0% | 0% | 25.5% | 85% | -59% |
| Encina.Compliance.CrossBorderTransfer | Full | 46% | 4% | 0% | 13% | 0% | 25.6% | 85% | -59% |
| Encina.Dapper.PostgreSQL | Provider | 34% | 9% | - | - | 27% | 25.9% | 50% | -24% |
| Encina.Messaging.Encryption.AzureKeyVault | Full | 37% | 11% | 0% | 0% | 0% | 26.3% | 85% | -59% |
| Encina.EntityFrameworkCore | Provider | 43% | 3% | - | - | 25% | 26.5% | 50% | -24% |
| Encina.Dapper.MySQL | Provider | 37% | 6% | - | - | 29% | 26.8% | 50% | -23% |
| Encina.Dapper.SqlServer | Provider | 47% | 6% | - | - | 28% | 27.3% | 50% | -23% |
| Encina.Compliance.LawfulBasis | Full | 47% | 3% | 0% | 0% | 0% | 27.5% | 85% | -58% |
| Encina.MQTT | Transport | 51% | 0% | - | - | - | 27.9% | 75% | -47% |
| Encina.Testing.Respawn | TestingLibrary | 50% | 0% | - | - | - | 29.0% | 70% | -41% |
| Encina.Testing.Verify | TestingLibrary | 59% | 0% | - | - | - | 29.4% | 70% | -41% |
| Encina.Compliance.Attestation | Full | 51% | 8% | 9% | 9% | 0% | 29.6% | 85% | -55% |
| Encina.ADO.SqlServer | Provider | 42% | 5% | - | - | 33% | 30.6% | 50% | -19% |
| Encina.ADO.PostgreSQL | Provider | 32% | 8% | - | - | 34% | 31.0% | 50% | -19% |
| Encina.Compliance.Retention | Full | 50% | 6% | 0% | 0% | 0% | 31.6% | 85% | -53% |
| Encina.Testing.Fakes | TestingLibrary | 64% | 0% | - | - | - | 31.8% | 70% | -38% |
| Encina.ADO.MySQL | Provider | 35% | 5% | - | - | 36% | 32.1% | 50% | -18% |
| Encina.Extensions.Resilience | Logic | 61% | 3% | - | 0% | - | 32.3% | 80% | -48% |
| Encina.Testing.Shouldly | TestingLibrary | 61% | 4% | - | - | - | 32.6% | 70% | -37% |
| Encina.Compliance.DataSubjectRights | Full | 53% | 6% | 0% | 0% | 2% | 33.0% | 85% | -52% |
| Encina.Caching.Hybrid | Caching | 68% | 0% | - | - | 0% | 33.3% | 70% | -37% |
| Encina.AzureFunctions | Cloud | 62% | 2% | - | - | - | 33.8% | 60% | -26% |
| Encina.Security.Audit | Full | 69% | 7% | 0% | 0% | 0% | 34.0% | 85% | -51% |
| Encina.Testing.Architecture | TestingLibrary | 70% | 0% | - | - | - | 34.9% | 70% | -35% |
| Encina.Compliance.NIS2 | Full | 72% | 5% | 1% | 0% | 0% | 35.0% | 85% | -50% |
| Encina.Caching.Redis | Caching | 68% | 0% | - | - | 29% | 35.1% | 70% | -35% |
| Encina.Caching | Full | 73% | 5% | 0% | 0% | 0% | 35.8% | 85% | -49% |
| Encina.Compliance.ProcessorAgreements | Full | 66% | 4% | 0% | 0% | 0% | 36.8% | 85% | -48% |
| Encina.Testing.Bogus | TestingLibrary | 72% | 2% | - | - | - | 36.9% | 70% | -33% |
| Encina.Compliance.Anonymization | Full | 61% | 7% | 0% | 0% | 0% | 37.2% | 85% | -48% |
| Encina.Polly | Logic | 82% | 8% | - | 0% | - | 37.4% | 80% | -43% |
| Encina.Security.ABAC | Full | 72% | 2% | 0% | 30% | 0% | 38.0% | 85% | -47% |
| Encina.OpenTelemetry | Logic | 48% | 1% | - | 0% | - | 38.1% | 80% | -42% |
| Encina.Redis.PubSub | Transport | 59% | 13% | - | - | - | 38.5% | 75% | -36% |
| Encina.Testing.WireMock | TestingLibrary | 77% | 0% | - | - | - | 38.5% | 70% | -31% |
| Encina.Cdc | CDC | 68% | 9% | - | - | 0% | 39.0% | 60% | -21% |
| Encina.gRPC | Cloud | 75% | 0% | - | - | - | 39.3% | 60% | -21% |
| Encina.DistributedLock.Redis | DistributedLock | 73% | 4% | - | - | 30% | 39.4% | 70% | -31% |
| Encina.Compliance.BreachNotification | Full | 64% | 9% | 0% | 0% | 0% | 39.7% | 85% | -45% |
| Encina.NATS | Transport | 75% | 0% | - | - | - | 39.8% | 75% | -35% |
| Encina.Messaging | Full | 80% | 3% | 21% | 52% | 35% | 40.6% | 85% | -44% |
| Encina.Quartz | Logic | 85% | 0% | - | 27% | - | 40.9% | 80% | -39% |
| Encina.Testing | TestingLibrary | 82% | 0% | - | - | - | 40.9% | 70% | -29% |
| Encina.Security.Sanitization | Full | 80% | 11% | 0% | 28% | 0% | 41.5% | 85% | -44% |
| Encina | Full | 73% | 8% | 0% | 1% | 42% | 41.5% | 85% | -44% |
| Encina.Compliance.PrivacyByDesign | Full | 81% | 6% | 2% | 0% | 0% | 42.2% | 85% | -43% |
| Encina.Security.AntiTampering | Full | 77% | 8% | 0% | 0% | 0% | 42.6% | 85% | -42% |
| Encina.Messaging.Encryption | Full | 73% | 7% | 0% | 0% | 0% | 42.9% | 85% | -42% |
| Encina.Messaging.Encryption.AwsKms | Full | 79% | 12% | 0% | 0% | 0% | 43.3% | 85% | -42% |
| Encina.Security.Secrets | Full | 78% | 2% | 1% | 12% | 0% | 43.6% | 85% | -41% |
| Encina.Caching.Valkey | Caching | 88% | 0% | - | - | 0% | 43.8% | 70% | -26% |
| Encina.Caching.KeyDB | Caching | 88% | 0% | - | - | 0% | 43.8% | 70% | -26% |
| Encina.SignalR | Cloud | 86% | 0% | - | - | - | 43.8% | 60% | -16% |
| Encina.Caching.Garnet | Caching | 88% | 0% | - | - | 0% | 43.8% | 70% | -26% |
| Encina.Caching.Dragonfly | Caching | 88% | 0% | - | - | 0% | 43.8% | 70% | -26% |
| Encina.Security.Secrets.AzureKeyVault | Full | 93% | 10% | 0% | 0% | 0% | 44.0% | 85% | -41% |
| Encina.Security.PII | Full | 81% | 6% | 0% | 9% | 0% | 44.4% | 85% | -41% |
| Encina.Security.Secrets.AwsSecretsManager | Full | 95% | 8% | 0% | 0% | 0% | 44.7% | 85% | -40% |
| Encina.Security.Encryption | Full | 79% | 7% | 0% | 0% | 0% | 45.0% | 85% | -40% |
| Encina.Tenancy.AspNetCore | Logic | 81% | 0% | - | 0% | - | 45.1% | 80% | -35% |
| Encina.GuardClauses | Full | 98% | 0% | 0% | 87% | 0% | 45.9% | 85% | -39% |
| Encina.Kafka | Transport | 87% | 0% | - | - | - | 46.0% | 75% | -29% |
| Encina.DataAnnotations | Validation | 93% | 0% | 0% | - | - | 46.3% | 80% | -34% |
| Encina.Security.Secrets.GoogleCloudSecretManager | Full | 98% | 11% | 0% | 0% | 0% | 46.8% | 85% | -38% |
| Encina.Caching.Memory | Caching | 83% | 14% | - | - | 0% | 46.9% | 70% | -23% |
| Encina.Security.Secrets.HashiCorpVault | Full | 97% | 12% | 0% | 0% | 0% | 47.3% | 85% | -38% |
| Encina.AwsLambda | Cloud | 87% | 6% | - | - | - | 47.8% | 60% | -12% |
| Encina.Testing.FsCheck | TestingLibrary | 96% | 0% | - | - | - | 48.0% | 70% | -22% |
| Encina.DistributedLock.InMemory | DistributedLock | 87% | 11% | - | - | 0% | 48.0% | 70% | -22% |
| Encina.Hangfire | Logic | 99% | 0% | - | 29% | - | 48.1% | 80% | -32% |
| Encina.Refit | Cloud | 92% | 6% | - | - | - | 48.9% | 60% | -11% |
| Encina.AmazonSQS | Transport | 95% | 0% | - | - | - | 49.1% | 75% | -26% |
| Encina.Compliance.Consent | Full | 80% | 15% | 0% | 0% | 0% | 49.5% | 85% | -36% |
| Encina.Compliance.AIAct | Full | 91% | 9% | 0% | 0% | 0% | 49.5% | 85% | -36% |
| Encina.IdGeneration | Full | 85% | 2% | 0% | 0% | 0% | 49.7% | 85% | -35% |
| Encina.RabbitMQ | Transport | 93% | 0% | - | - | - | 50.0% | 75% | -25% |
| Encina.MiniValidator | Validation | 100% | 0% | 0% | - | - | 50.0% | 80% | -30% |
| Encina.Compliance.GDPR | Full | 85% | 4% | 0% | 0% | 0% | 50.2% | 85% | -35% |
| Encina.DistributedLock.SqlServer | DistributedLock | 47% | 0% | - | - | 64% | 50.9% | 70% | -19% |
| Encina.AzureServiceBus | Transport | 97% | 0% | - | - | - | 51.0% | 75% | -24% |
| Encina.Messaging.Encryption.DataProtection | Full | 83% | 21% | 20% | 20% | 0% | 51.0% | 85% | -34% |
| Encina.Security | Full | 96% | 0% | 0% | 0% | 0% | 51.4% | 85% | -34% |
| Encina.Compliance.DPIA | Full | 78% | 27% | 0% | 0% | 0% | 52.0% | 85% | -33% |
| Encina.Tenancy | Logic | 84% | 0% | - | 0% | - | 57.9% | 80% | -22% |
| Encina.Aspire.Testing | Cloud | 100% | 0% | - | - | - | 58.1% | 60% | -2% |
| Encina.GraphQL | Cloud | 75% | 0% | - | - | - | 58.5% | 60% | -2% |
| Encina.Cdc.MongoDb | CDC | 93% | 16% | - | - | 0% | 60.5% | 60% | +0% |
| Encina.DistributedLock | DistributedLock | 100% | 28% | - | - | 0% | 63.9% | 70% | -6% |
| Encina.Cdc.SqlServer | CDC | 92% | 22% | - | - | 0% | 65.3% | 60% | +5% |
| Encina.Cdc.PostgreSql | CDC | 92% | 22% | - | - | 0% | 67.1% | 60% | +7% |
| Encina.Cdc.MySql | CDC | 96% | 16% | - | - | 0% | 68.4% | 60% | +8% |

<details><summary>File-level detail</summary>

### Encina (41.5%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| Core/Encina.Stream.cs | 7 | 0.0 | 0.0% |
| Core/StreamDispatcher.cs | 41 | 0.0 | 0.0% |
| Pipeline/StreamPipelineBuilder.cs | 25 | 0.0 | 0.0% |
| Sharding/Shadow/ShadowShardingServiceCollectionExtensions.cs | 43 | 0.0 | 0.0% |
| Sharding/ReferenceTables/PollingRefreshDetector.cs | 79 | 0.0 | 0.0% |
| Sharding/ReferenceTables/ReferenceTableDiagnostics.cs | 30 | 0.0 | 0.0% |
| Sharding/ReferenceTables/ReferenceTableReplicationService.cs | 97 | 0.0 | 0.0% |
| Sharding/ReferenceTables/ReferenceTableReplicator.cs | 198 | 0.0 | 0.0% |
| Sharding/Migrations/SchemaIntrospectionOptions.cs | 2 | 0.0 | 0.0% |
| Sharding/Configuration/CompoundRoutingBuilder.cs | 44 | 0.0 | 0.0% |
| Modules/ModuleBehaviorAdapter.cs | 16 | 0.0 | 0.0% |
| Modules/ModuleBehaviorServiceCollectionExtensions.cs | 41 | 0.0 | 0.0% |
| Modules/RequestContextModuleExtensions.cs | 18 | 0.0 | 0.0% |
| Modules/Isolation/ModuleIsolationViolationException.cs | 44 | 0.0 | 0.0% |
| Modules/Diagnostics/ModuleActivitySource.cs | 27 | 0.0 | 0.0% |
| Modules/Diagnostics/ModuleMetrics.cs | 23 | 0.0 | 0.0% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 771 | 22.4 | 2.9% |
| Results/EitherHelpers.cs | 22 | 2.5 | 11.4% |
| Sharding/ShardingServiceCollectionExtensions.cs | 268 | 31.5 | 11.8% |
| Sharding/TimeBased/InMemoryTierStore.cs | 34 | 6.0 | 17.6% |
| Sharding/Diagnostics/InstrumentedShardRouter.cs | 74 | 14.0 | 18.9% |
| Modules/Isolation/ModuleSchemaOptions.cs | 16 | 4.0 | 25.0% |
| Modules/ModuleHandlerRegistry.cs | 71 | 20.5 | 28.9% |
| obj/Release/net10.0/System.Text.RegularExpressions.Generator/System.Text.RegularExpressions.Generator.RegexGenerator/RegexGenerator.g.cs | 813 | 248.4 | 30.6% |
| Pipeline/Behaviors/CommandActivityPipelineBehavior.cs | 64 | 19.7 | 30.7% |
| Pipeline/Behaviors/QueryActivityPipelineBehavior.cs | 64 | 19.7 | 30.7% |
| Diagnostics/EncinaDiagnostics.cs | 26 | 8.0 | 30.8% |
| Pipeline/EncinaRequestGuards.cs | 40 | 12.5 | 31.2% |
| Sharding/Shadow/Behaviors/ShadowReadPipelineBehavior.cs | 55 | 17.3 | 31.5% |
| Pipeline/Behaviors/CommandMetricsPipelineBehavior.cs | 46 | 15.3 | 33.3% |
| Pipeline/Behaviors/QueryMetricsPipelineBehavior.cs | 46 | 15.3 | 33.3% |
| Validation/ValidationPipelineBehavior.cs | 12 | 4.0 | 33.3% |
| Sharding/Shadow/Behaviors/ShadowWritePipelineBehavior.cs | 31 | 10.3 | 33.3% |
| Dispatchers/Encina.RequestDispatcher.cs | 73 | 26.0 | 35.6% |
| Validation/ValidationOrchestrator.cs | 18 | 6.5 | 36.1% |
| Results/NullFunctionalFailureDetector.cs | 8 | 3.0 | 37.5% |
| Sharding/EntityShardRouter.cs | 16 | 6.0 | 37.5% |
| Sharding/Diagnostics/ShardingDiagnosticsServiceCollectionExtensions.cs | 55 | 21.5 | 39.1% |
| Sharding/Routing/GeoShardRouter.cs | 79 | 31.0 | 39.2% |
| Modules/ModuleConfiguration.cs | 24 | 10.0 | 41.7% |
| Sharding/Routing/DirectoryShardRouter.cs | 56 | 23.5 | 42.0% |
| Modules/Isolation/SqlSchemaExtractor.cs | 59 | 25.0 | 42.4% |
| Modules/ModuleServiceCollectionExtensions.cs | 28 | 12.0 | 42.9% |
| Modules/ModuleLifecycleHostedService.cs | 61 | 26.5 | 43.4% |
| Sharding/TimeBased/TierTransitionScheduler.cs | 107 | 47.0 | 43.9% |
| Sharding/ShardedSpecificationResult.cs | 9 | 4.0 | 44.4% |
| Sharding/Shadow/ShadowShardRouterDecorator.cs | 86 | 38.5 | 44.8% |
| Core/ServiceCollectionExtensions.cs | 67 | 30.0 | 44.8% |
| Sharding/Shadow/ShadowComparisonResult.cs | 10 | 4.5 | 45.0% |
| Sharding/Migrations/MigrationResult.cs | 11 | 5.0 | 45.5% |
| Dispatchers/Strategies/ParallelWhenAllDispatchStrategy.cs | 67 | 30.5 | 45.5% |
| Dispatchers/Encina.NotificationDispatcher.cs | 167 | 77.0 | 46.1% |
| Sharding/ShardedPagedResult.cs | 13 | 6.0 | 46.1% |
| Dispatchers/Strategies/ParallelDispatchStrategy.cs | 57 | 26.5 | 46.5% |
| Sharding/Routing/RangeShardRouter.cs | 74 | 34.5 | 46.6% |
| Sharding/Migrations/MigrationCoordinationOptions.cs | 15 | 7.0 | 46.7% |
| Dispatchers/MediatorAssemblyScanner.cs | 72 | 34.0 | 47.2% |
| Sharding/Migrations/Strategies/SequentialMigrationStrategy.cs | 38 | 18.0 | 47.4% |
| Core/Encina.cs | 60 | 29.0 | 48.3% |
| Sharding/Diagnostics/ShardingActivitySource.cs | 153 | 74.0 | 48.4% |
| Sharding/CompoundShardKey.cs | 33 | 16.0 | 48.5% |
| Sharding/Migrations/Strategies/ParallelMigrationStrategy.cs | 33 | 16.0 | 48.5% |
| Sharding/Configuration/ShardingOptions.cs | 86 | 42.0 | 48.8% |
| Sharding/Migrations/ShardedMigrationCoordinator.cs | 267 | 130.5 | 48.9% |
| Sharding/Diagnostics/ShardedDatabasePoolMetrics.cs | 47 | 23.0 | 48.9% |
| Sharding/Resharding/Phases/PlanningPhase.cs | 54 | 26.5 | 49.1% |
| Modules/Isolation/PostgreSqlPermissionScriptGenerator.cs | 166 | 82.0 | 49.4% |
| Modules/Isolation/SqlServerPermissionScriptGenerator.cs | 168 | 83.0 | 49.4% |
| Pipeline/EncinaBehaviorGuards.cs | 24 | 12.0 | 50.0% |
| Pipeline/EncinaNotificationGuards.cs | 26 | 13.0 | 50.0% |
| Pipeline/PipelineBuilder.cs | 75 | 37.5 | 50.0% |
| Validation/ValidationResult.cs | 18 | 9.0 | 50.0% |
| Sharding/AggregationResult.cs | 6 | 3.0 | 50.0% |
| Sharding/DefaultShardTopologyProvider.cs | 5 | 2.5 | 50.0% |
| Sharding/ShardAggregatePartial.cs | 6 | 3.0 | 50.0% |
| Sharding/ShardedCountResult.cs | 7 | 3.5 | 50.0% |
| Sharding/ShardedQueryResult.cs | 8 | 4.0 | 50.0% |
| Sharding/ShardKeyAttribute.cs | 1 | 0.5 | 50.0% |
| Sharding/ShardKeyExtractor.cs | 31 | 15.5 | 50.0% |
| Sharding/TimeBased/TimeBasedShardEntry.cs | 3 | 1.5 | 50.0% |
| Sharding/Routing/InMemoryShardDirectoryStore.cs | 20 | 10.0 | 50.0% |
| Sharding/Resharding/PhaseHistoryEntry.cs | 5 | 2.5 | 50.0% |
| Sharding/Resharding/ReshardingCheckpoint.cs | 3 | 1.5 | 50.0% |
| Sharding/Resharding/ShardMigrationProgress.cs | 4 | 2.0 | 50.0% |
| Sharding/ReplicaSelection/ReplicaHealthState.cs | 8 | 4.0 | 50.0% |
| Sharding/ReferenceTables/InMemoryReferenceTableStateStore.cs | 15 | 7.5 | 50.0% |
| Sharding/ReferenceTables/ReferenceTableHashComputer.cs | 17 | 8.5 | 50.0% |
| Sharding/ReferenceTables/ReplicationResult.cs | 12 | 6.0 | 50.0% |
| Sharding/Migrations/ColumnSchema.cs | 11 | 5.5 | 50.0% |
| Sharding/Migrations/MigrationProgress.cs | 9 | 4.5 | 50.0% |
| Sharding/Migrations/MigrationServiceCollectionExtensions.cs | 17 | 8.5 | 50.0% |
| Sharding/Migrations/SchemaComparer.cs | 32 | 16.0 | 50.0% |
| Sharding/Migrations/SchemaDriftReport.cs | 4 | 2.0 | 50.0% |
| Sharding/Migrations/ShardSchema.cs | 7 | 3.5 | 50.0% |
| Sharding/Migrations/TableSchema.cs | 6 | 3.0 | 50.0% |
| Sharding/Migrations/Strategies/CanaryFirstStrategy.cs | 23 | 11.5 | 50.0% |
| Sharding/Migrations/Strategies/RollingUpdateStrategy.cs | 31 | 15.5 | 50.0% |
| Sharding/Health/ShardedHealthSummary.cs | 18 | 9.0 | 50.0% |
| Sharding/Health/ShardHealthResult.cs | 15 | 7.5 | 50.0% |
| Sharding/Extensions/ShardedAggregationExtensions.cs | 26 | 13.0 | 50.0% |
| Sharding/Colocation/ColocationGroup.cs | 4 | 2.0 | 50.0% |
| Sharding/Aggregation/AggregationCombiner.cs | 40 | 20.0 | 50.0% |
| Modules/ModuleRegistry.cs | 30 | 15.0 | 50.0% |
| Modules/Isolation/ModuleExecutionContext.cs | 21 | 10.5 | 50.0% |
| Modules/Isolation/ModuleSchemaRegistry.cs | 58 | 29.0 | 50.0% |
| Dispatchers/Strategies/SequentialDispatchStrategy.cs | 8 | 4.0 | 50.0% |
| Database/ConnectionPoolStats.cs | 10 | 5.0 | 50.0% |
| Database/DatabaseHealthResult.cs | 12 | 6.0 | 50.0% |
| Sharding/CompoundShardKeyExtractor.cs | 80 | 40.5 | 50.6% |
| Sharding/TimeBased/ShardArchiver.cs | 62 | 31.5 | 50.8% |
| Sharding/Resharding/Phases/CopyingPhase.cs | 66 | 34.0 | 51.5% |
| Sharding/Resharding/Phases/ReplicatingPhase.cs | 64 | 33.0 | 51.6% |
| Sharding/Resharding/Phases/ReshardingPhaseExecutor.cs | 124 | 64.0 | 51.6% |
| Sharding/Resharding/Phases/CuttingOverPhase.cs | 90 | 46.5 | 51.7% |
| Sharding/Resharding/Phases/VerifyingPhase.cs | 59 | 30.5 | 51.7% |
| Sharding/TimeBased/PeriodBoundaryCalculator.cs | 53 | 27.5 | 51.9% |
| Sharding/Resharding/Phases/CleaningUpPhase.cs | 49 | 25.5 | 52.0% |
| Sharding/Resharding/ReshardingOrchestrator.cs | 179 | 95.0 | 53.1% |
| Sharding/Execution/ShardedQueryExecutor.cs | 125 | 66.5 | 53.2% |
| Sharding/TimeBased/TimeBasedShardRouter.cs | 183 | 99.5 | 54.4% |
| Sharding/ReplicaSelection/ReplicaHealthTracker.cs | 111 | 61.0 | 55.0% |
| Sharding/Resharding/ReshardingServiceCollectionExtensions.cs | 20 | 11.0 | 55.0% |
| Sharding/Health/ShardReplicaHealthCheck.cs | 72 | 40.0 | 55.6% |
| Sharding/Colocation/ColocationGroupRegistry.cs | 40 | 23.0 | 57.5% |
| Sharding/Resharding/ReshardingBuilder.cs | 18 | 10.5 | 58.3% |
| Core/RequestContext.cs | 47 | 27.5 | 58.5% |
| Sharding/ReplicaSelection/LeastLatencyShardReplicaSelector.cs | 29 | 17.5 | 60.3% |
| Errors/EncinaError.cs | 30 | 18.5 | 61.7% |
| Sharding/Resharding/ReshardingResult.cs | 8 | 5.0 | 62.5% |
| Sharding/ReplicaSelection/LeastConnectionsShardReplicaSelector.cs | 22 | 14.0 | 63.6% |
| Sharding/Routing/HashShardRouter.cs | 70 | 45.5 | 65.0% |
| Sharding/Routing/CompoundShardRouter.cs | 48 | 31.5 | 65.6% |
| Sharding/ShardTopology.cs | 45 | 30.0 | 66.7% |
| Sharding/Routing/GeoShardRouterOptions.cs | 3 | 2.0 | 66.7% |
| Sharding/Resharding/ReshardingState.cs | 12 | 8.0 | 66.7% |
| Sharding/Resharding/Phases/PhaseResult.cs | 6 | 4.0 | 66.7% |
| Sharding/ReplicaSelection/AcceptStaleReadsAttribute.cs | 6 | 4.0 | 66.7% |
| Sharding/ReferenceTables/EntityMetadata.cs | 9 | 6.0 | 66.7% |
| Sharding/Colocation/ColocationViolationException.cs | 35 | 23.5 | 67.1% |
| Sharding/Diagnostics/ShardedReadWriteMetrics.cs | 84 | 57.0 | 67.9% |
| Sharding/Colocation/ColocationGroupBuilder.cs | 19 | 13.0 | 68.4% |
| Sharding/Colocation/ColocatedWithAttribute.cs | 5 | 3.5 | 70.0% |
| Sharding/ReplicaSelection/WeightedRandomShardReplicaSelector.cs | 27 | 19.0 | 70.4% |
| Sharding/ReplicaSelection/ShardReplicaSelectorFactory.cs | 9 | 6.5 | 72.2% |
| Sharding/TimeBased/ArchiveOptions.cs | 8 | 6.0 | 75.0% |
| Sharding/Resharding/EstimatedResources.cs | 4 | 3.0 | 75.0% |
| Sharding/Resharding/ReshardingRequest.cs | 8 | 6.0 | 75.0% |
| Sharding/Resharding/RollbackMetadata.cs | 8 | 6.0 | 75.0% |
| Sharding/ShardInfo.cs | 23 | 17.5 | 76.1% |
| Sharding/Resharding/Phases/PhaseContext.cs | 15 | 11.5 | 76.7% |
| Sharding/Resharding/ReshardingProgress.cs | 7 | 5.5 | 78.6% |
| Sharding/ReplicaSelection/RoundRobinShardReplicaSelector.cs | 7 | 5.5 | 78.6% |
| Core/EncinaConfiguration.cs | 90 | 72.0 | 80.0% |
| Sharding/ReplicaSelection/RandomShardReplicaSelector.cs | 5 | 4.0 | 80.0% |
| Sharding/ReferenceTables/EntityMetadataCache.cs | 38 | 31.0 | 81.6% |
| Sharding/Migrations/MigrationCoordinationBuilder.cs | 20 | 16.5 | 82.5% |
| Sharding/Diagnostics/ShardRoutingMetrics.cs | 145 | 124.0 | 85.5% |
| Results/EncinaErrors.cs | 36 | 31.0 | 86.1% |
| Sharding/TimeBased/ShardTierInfo.cs | 18 | 15.5 | 86.1% |
| Sharding/ReferenceTables/ReferenceTableRegistry.cs | 27 | 24.0 | 88.9% |
| Sharding/ShardedReadWriteOptions.cs | 20 | 18.0 | 90.0% |
| Sharding/Resharding/ReshardingPlan.cs | 8 | 7.5 | 93.8% |
| Sharding/Migrations/ShardMigrationStatus.cs | 8 | 7.5 | 93.8% |
| Sharding/Shadow/ShadowShardingOptions.cs | 18 | 17.0 | 94.4% |
| Abstractions/IdGeneration/IdGenerationErrors.cs | 18 | 18.0 | 100.0% |
| Options/NotificationDispatchOptions.cs | 2 | 2.0 | 100.0% |
| Sharding/ShardedPaginationOptions.cs | 11 | 11.0 | 100.0% |
| Sharding/TimeBased/TierTransition.cs | 10 | 10.0 | 100.0% |
| Sharding/TimeBased/TimeBasedShardingOptions.cs | 9 | 9.0 | 100.0% |
| Sharding/TimeBased/Health/ShardCreationHealthCheckOptions.cs | 4 | 4.0 | 100.0% |
| Sharding/TimeBased/Health/TierTransitionHealthCheckOptions.cs | 4 | 4.0 | 100.0% |
| Sharding/Routing/CompoundShardRouterOptions.cs | 3 | 3.0 | 100.0% |
| Sharding/Routing/GeoRegion.cs | 10 | 10.0 | 100.0% |
| Sharding/Routing/HashShardRouterOptions.cs | 2 | 2.0 | 100.0% |
| Sharding/Routing/ShardRange.cs | 5 | 5.0 | 100.0% |
| Sharding/Resharding/KeyRange.cs | 1 | 1.0 | 100.0% |
| Sharding/Resharding/ReshardingOptions.cs | 7 | 7.0 | 100.0% |
| Sharding/Resharding/ShardMigrationStep.cs | 13 | 13.0 | 100.0% |
| Sharding/ReplicaSelection/StalenessOptions.cs | 3 | 3.0 | 100.0% |
| Sharding/ReferenceTables/ReferenceTableConfiguration.cs | 3 | 3.0 | 100.0% |
| Sharding/ReferenceTables/ReferenceTableGlobalOptions.cs | 4 | 4.0 | 100.0% |
| Sharding/ReferenceTables/ReferenceTableOptions.cs | 5 | 5.0 | 100.0% |
| Sharding/ReferenceTables/Health/ReferenceTableHealthCheckOptions.cs | 2 | 2.0 | 100.0% |
| Sharding/Migrations/DriftDetectionOptions.cs | 6 | 6.0 | 100.0% |
| Sharding/Migrations/MigrationOptions.cs | 5 | 5.0 | 100.0% |
| Sharding/Migrations/MigrationScript.cs | 21 | 21.0 | 100.0% |
| Sharding/Migrations/ShardSchemaDiff.cs | 10 | 10.0 | 100.0% |
| Sharding/Migrations/TableDiff.cs | 7 | 7.0 | 100.0% |
| Sharding/Diagnostics/ShardingMetricsOptions.cs | 8 | 8.0 | 100.0% |
| Sharding/Configuration/ScatterGatherOptions.cs | 3 | 3.0 | 100.0% |
| Sharding/Configuration/TimeBasedShardRouterOptions.cs | 14 | 14.0 | 100.0% |
| Modules/Isolation/ModuleIsolationOptions.cs | 40 | 40.0 | 100.0% |
| Diagnostics/DatabasePoolMetrics.cs | 29 | 29.0 | 100.0% |
| Diagnostics/EventIdRanges.cs | 51 | 51.0 | 100.0% |
| Database/DatabaseCircuitBreakerOptions.cs | 6 | 6.0 | 100.0% |
| Database/DatabaseResilienceOptions.cs | 5 | 5.0 | 100.0% |

### Encina.ADO.MySQL (32.1%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| UnitOfWork/UnitOfWorkADO.cs | 79 | 0.0 | 0.0% |
| Tenancy/TenantConnectionFactory.cs | 52 | 0.0 | 0.0% |
| SoftDelete/SoftDeleteEntityMappingBuilder.cs | 180 | 0.0 | 0.0% |
| SoftDelete/SoftDeleteSpecificationSqlBuilder.cs | 64 | 0.0 | 0.0% |
| Sharding/FunctionalShardedRepositoryADO.cs | 658 | 0.0 | 0.0% |
| Sharding/ShardedConnectionFactory.cs | 73 | 0.0 | 0.0% |
| Sharding/ShardedReadWriteConnectionFactory.cs | 136 | 0.0 | 0.0% |
| Sharding/ShardingServiceCollectionExtensions.cs | 53 | 0.0 | 0.0% |
| Sharding/ReferenceTables/ReferenceTableStoreADO.cs | 86 | 0.0 | 0.0% |
| Sharding/ReferenceTables/ReferenceTableStoreFactoryADO.cs | 10 | 0.0 | 0.0% |
| Sharding/Migrations/AdoHelper.cs | 17 | 0.0 | 0.0% |
| Sharding/Migrations/AdoMigrationExecutor.cs | 35 | 0.0 | 0.0% |
| Sharding/Migrations/AdoMigrationHistoryStore.cs | 132 | 0.0 | 0.0% |
| Sharding/Migrations/MigrationServiceCollectionExtensions.cs | 5 | 0.0 | 0.0% |
| Sharding/Migrations/MySqlSchemaIntrospector.cs | 63 | 0.0 | 0.0% |
| ReadWriteSeparation/ReadWriteRoutingPipelineBehavior.cs | 28 | 0.0 | 0.0% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 5 | 0.0 | 0.0% |
| ReadWriteSeparation/ReadWriteSeparationHealthCheck.cs | 60 | 0.0 | 0.0% |
| Modules/ModuleAwareConnectionFactory.cs | 22 | 0.0 | 0.0% |
| Modules/SchemaValidatingCommand.cs | 66 | 0.0 | 0.0% |
| Extensions/AuditExtensions.cs | 4 | 0.0 | 0.0% |
| Auditing/AuditLogStoreADO.cs | 81 | 0.0 | 0.0% |
| Auditing/AuditStoreADO.cs | 286 | 0.0 | 0.0% |
| Auditing/ReadAuditStoreADO.cs | 215 | 0.0 | 0.0% |
| Anonymization/TokenMappingStoreADO.cs | 130 | 0.0 | 0.0% |
| ABAC/PolicyStoreADO.cs | 310 | 0.0 | 0.0% |
| Health/MySqlDatabaseHealthMonitor.cs | 34 | 1.0 | 2.9% |
| UnitOfWork/UnitOfWorkRepositoryADO.cs | 419 | 74.0 | 17.7% |
| ServiceCollectionExtensions.cs | 79 | 17.0 | 21.5% |
| Repository/SpecificationSqlBuilder.cs | 262 | 61.0 | 23.3% |
| Tenancy/TenantAwareSpecificationSqlBuilder.cs | 79 | 28.0 | 35.4% |
| Scheduling/ScheduledMessage.cs | 19 | 7.0 | 36.8% |
| Tenancy/TenantAwareFunctionalRepositoryADO.cs | 587 | 224.0 | 38.2% |
| Sagas/SagaState.cs | 10 | 4.0 | 40.0% |
| Tenancy/TenantEntityMappingBuilder.cs | 112 | 48.5 | 43.3% |
| Outbox/OutboxMessage.cs | 10 | 4.5 | 45.0% |
| Repository/FunctionalRepositoryADO.cs | 449 | 205.0 | 45.7% |
| Tenancy/TenancyServiceCollectionExtensions.cs | 73 | 34.0 | 46.6% |
| Scheduling/ScheduledMessageFactory.cs | 11 | 5.5 | 50.0% |
| Sagas/SagaStateFactory.cs | 11 | 5.5 | 50.0% |
| Outbox/OutboxMessageFactory.cs | 8 | 4.0 | 50.0% |
| Health/MySqlHealthCheck.cs | 8 | 4.0 | 50.0% |
| Outbox/OutboxProcessor.cs | 98 | 51.0 | 52.0% |
| Modules/SchemaValidatingConnection.cs | 39 | 24.0 | 61.5% |
| Repository/EntityMappingBuilder.cs | 73 | 55.5 | 76.0% |
| Pagination/CursorPaginationHelper.cs | 232 | 188.0 | 81.0% |
| BulkOperations/BulkOperationsMySQL.cs | 267 | 220.0 | 82.4% |
| ProcessingActivity/ProcessingActivityRegistryADO.cs | 115 | 99.0 | 86.1% |
| Inbox/InboxStoreADO.cs | 240 | 210.0 | 87.5% |
| Outbox/OutboxStoreADO.cs | 153 | 140.0 | 91.5% |
| Scheduling/ScheduledMessageStoreADO.cs | 219 | 201.0 | 91.8% |
| Sagas/SagaStoreADO.cs | 208 | 194.0 | 93.3% |
| Tenancy/ADOTenancyOptions.cs | 5 | 5.0 | 100.0% |
| ReadWriteSeparation/ReadWriteConnectionFactory.cs | 39 | 39.0 | 100.0% |

### Encina.ADO.PostgreSQL (31.0%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| UnitOfWork/UnitOfWorkADO.cs | 79 | 0.0 | 0.0% |
| Tenancy/TenantConnectionFactory.cs | 52 | 0.0 | 0.0% |
| Temporal/TemporalEntityMappingBuilder.cs | 93 | 0.0 | 0.0% |
| Temporal/TemporalRepositoryADO.cs | 321 | 0.0 | 0.0% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 35 | 0.0 | 0.0% |
| SoftDelete/SoftDeleteEntityMappingBuilder.cs | 180 | 0.0 | 0.0% |
| SoftDelete/SoftDeleteSpecificationSqlBuilder.cs | 64 | 0.0 | 0.0% |
| Sharding/FunctionalShardedRepositoryADO.cs | 658 | 0.0 | 0.0% |
| Sharding/ShardedConnectionFactory.cs | 73 | 0.0 | 0.0% |
| Sharding/ShardedReadWriteConnectionFactory.cs | 136 | 0.0 | 0.0% |
| Sharding/ShardingServiceCollectionExtensions.cs | 53 | 0.0 | 0.0% |
| Sharding/ReferenceTables/ReferenceTableStoreADO.cs | 87 | 0.0 | 0.0% |
| Sharding/ReferenceTables/ReferenceTableStoreFactoryADO.cs | 10 | 0.0 | 0.0% |
| Sharding/Migrations/AdoHelper.cs | 17 | 0.0 | 0.0% |
| Sharding/Migrations/AdoMigrationExecutor.cs | 35 | 0.0 | 0.0% |
| Sharding/Migrations/AdoMigrationHistoryStore.cs | 133 | 0.0 | 0.0% |
| Sharding/Migrations/MigrationServiceCollectionExtensions.cs | 5 | 0.0 | 0.0% |
| Sharding/Migrations/PostgreSqlSchemaIntrospector.cs | 63 | 0.0 | 0.0% |
| ReadWriteSeparation/ReadWriteRoutingPipelineBehavior.cs | 28 | 0.0 | 0.0% |
| ReadWriteSeparation/ReadWriteSeparationHealthCheck.cs | 60 | 0.0 | 0.0% |
| Modules/ModuleAwareConnectionFactory.cs | 22 | 0.0 | 0.0% |
| Modules/SchemaValidatingCommand.cs | 66 | 0.0 | 0.0% |
| Extensions/AuditExtensions.cs | 4 | 0.0 | 0.0% |
| Auditing/AuditLogStoreADO.cs | 81 | 0.0 | 0.0% |
| Auditing/AuditStoreADO.cs | 294 | 0.0 | 0.0% |
| Auditing/ReadAuditStoreADO.cs | 223 | 0.0 | 0.0% |
| Anonymization/TokenMappingStoreADO.cs | 130 | 0.0 | 0.0% |
| ABAC/PolicyStoreADO.cs | 310 | 0.0 | 0.0% |
| Health/PostgreSqlDatabaseHealthMonitor.cs | 34 | 1.0 | 2.9% |
| UnitOfWork/UnitOfWorkRepositoryADO.cs | 406 | 72.0 | 17.7% |
| Repository/SpecificationSqlBuilder.cs | 265 | 61.0 | 23.0% |
| ServiceCollectionExtensions.cs | 84 | 22.0 | 26.2% |
| Tenancy/TenantAwareSpecificationSqlBuilder.cs | 79 | 28.0 | 35.4% |
| Scheduling/ScheduledMessage.cs | 19 | 7.0 | 36.8% |
| Tenancy/TenantAwareFunctionalRepositoryADO.cs | 572 | 219.0 | 38.3% |
| Sagas/SagaState.cs | 10 | 4.0 | 40.0% |
| Outbox/OutboxMessage.cs | 10 | 4.5 | 45.0% |
| Repository/FunctionalRepositoryADO.cs | 438 | 202.0 | 46.1% |
| Tenancy/TenancyServiceCollectionExtensions.cs | 73 | 34.0 | 46.6% |
| Scheduling/ScheduledMessageFactory.cs | 11 | 5.5 | 50.0% |
| Sagas/SagaStateFactory.cs | 11 | 5.5 | 50.0% |
| Outbox/OutboxMessageFactory.cs | 8 | 4.0 | 50.0% |
| Health/PostgreSqlHealthCheck.cs | 8 | 4.0 | 50.0% |
| Outbox/OutboxProcessor.cs | 95 | 49.5 | 52.1% |
| Modules/SchemaValidatingConnection.cs | 39 | 25.0 | 64.1% |
| Tenancy/TenantEntityMappingBuilder.cs | 112 | 78.5 | 70.1% |
| Repository/EntityMappingBuilder.cs | 73 | 55.5 | 76.0% |
| Pagination/CursorPaginationHelper.cs | 232 | 188.0 | 81.0% |
| BulkOperations/BulkOperationsPostgreSQL.cs | 318 | 268.0 | 84.3% |
| ProcessingActivity/ProcessingActivityRegistryADO.cs | 115 | 99.0 | 86.1% |
| Inbox/InboxStoreADO.cs | 241 | 211.0 | 87.5% |
| Outbox/OutboxStoreADO.cs | 155 | 142.0 | 91.6% |
| Scheduling/ScheduledMessageStoreADO.cs | 220 | 202.0 | 91.8% |
| Sagas/SagaStoreADO.cs | 208 | 194.0 | 93.3% |
| Tenancy/ADOTenancyOptions.cs | 5 | 5.0 | 100.0% |
| ReadWriteSeparation/ReadWriteConnectionFactory.cs | 39 | 39.0 | 100.0% |

### Encina.ADO.SqlServer (30.6%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| Tenancy/TenantConnectionFactory.cs | 52 | 0.0 | 0.0% |
| Temporal/TemporalEntityMappingBuilder.cs | 92 | 0.0 | 0.0% |
| Temporal/TemporalRepositoryADO.cs | 295 | 0.0 | 0.0% |
| SoftDelete/SoftDeleteEntityMappingBuilder.cs | 180 | 0.0 | 0.0% |
| SoftDelete/SoftDeleteSpecificationSqlBuilder.cs | 69 | 0.0 | 0.0% |
| Sharding/FunctionalShardedRepositoryADO.cs | 658 | 0.0 | 0.0% |
| Sharding/ShardedConnectionFactory.cs | 74 | 0.0 | 0.0% |
| Sharding/ShardedReadWriteConnectionFactory.cs | 137 | 0.0 | 0.0% |
| Sharding/ShardingServiceCollectionExtensions.cs | 53 | 0.0 | 0.0% |
| Sharding/ReferenceTables/ReferenceTableStoreADO.cs | 93 | 0.0 | 0.0% |
| Sharding/ReferenceTables/ReferenceTableStoreFactoryADO.cs | 10 | 0.0 | 0.0% |
| Sharding/Migrations/AdoMigrationExecutor.cs | 35 | 0.0 | 0.0% |
| Sharding/Migrations/AdoMigrationHistoryStore.cs | 202 | 0.0 | 0.0% |
| Sharding/Migrations/MigrationServiceCollectionExtensions.cs | 5 | 0.0 | 0.0% |
| Sharding/Migrations/SqlServerSchemaIntrospector.cs | 76 | 0.0 | 0.0% |
| Modules/ModuleAwareConnectionFactory.cs | 22 | 0.0 | 0.0% |
| Modules/SchemaValidatingCommand.cs | 66 | 0.0 | 0.0% |
| Extensions/AuditExtensions.cs | 4 | 0.0 | 0.0% |
| Auditing/AuditLogStoreADO.cs | 81 | 0.0 | 0.0% |
| Auditing/AuditStoreADO.cs | 293 | 0.0 | 0.0% |
| Auditing/ReadAuditStoreADO.cs | 219 | 0.0 | 0.0% |
| Anonymization/TokenMappingStoreADO.cs | 130 | 0.0 | 0.0% |
| ABAC/PolicyStoreADO.cs | 316 | 0.0 | 0.0% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 35 | 0.7 | 1.9% |
| Health/SqlServerDatabaseHealthMonitor.cs | 37 | 1.0 | 2.7% |
| ReadWriteSeparation/ReadWriteSeparationHealthCheck.cs | 60 | 12.5 | 20.8% |
| ServiceCollectionExtensions.cs | 125 | 33.0 | 26.4% |
| UnitOfWork/UnitOfWorkRepositoryADO.cs | 402 | 113.0 | 28.1% |
| ReadWriteSeparation/ReadWriteRoutingPipelineBehavior.cs | 28 | 9.0 | 32.1% |
| Tenancy/TenantAwareSpecificationSqlBuilder.cs | 84 | 28.0 | 33.3% |
| Repository/SpecificationSqlBuilder.cs | 265 | 98.5 | 37.2% |
| Tenancy/TenantAwareFunctionalRepositoryADO.cs | 570 | 219.0 | 38.4% |
| UnitOfWork/UnitOfWorkADO.cs | 79 | 32.5 | 41.1% |
| Scheduling/ScheduledMessage.cs | 19 | 8.5 | 44.7% |
| Outbox/OutboxMessage.cs | 10 | 4.5 | 45.0% |
| Repository/FunctionalRepositoryADO.cs | 434 | 200.0 | 46.1% |
| Tenancy/TenancyServiceCollectionExtensions.cs | 73 | 34.0 | 46.6% |
| Tenancy/TenantEntityMappingBuilder.cs | 112 | 55.5 | 49.5% |
| Scheduling/ScheduledMessageFactory.cs | 11 | 5.5 | 50.0% |
| Sagas/SagaState.cs | 10 | 5.0 | 50.0% |
| Sagas/SagaStateFactory.cs | 11 | 5.5 | 50.0% |
| Outbox/OutboxMessageFactory.cs | 8 | 4.0 | 50.0% |
| Health/SqlServerHealthCheck.cs | 8 | 4.0 | 50.0% |
| BulkOperations/BulkOperationsADO.cs | 278 | 143.0 | 51.4% |
| Outbox/OutboxProcessor.cs | 95 | 49.5 | 52.1% |
| Modules/SchemaValidatingConnection.cs | 39 | 24.0 | 61.5% |
| Repository/EntityMappingBuilder.cs | 73 | 55.5 | 76.0% |
| Pagination/CursorPaginationHelper.cs | 230 | 186.0 | 80.9% |
| ProcessingActivity/ProcessingActivityRegistryADO.cs | 115 | 99.0 | 86.1% |
| Inbox/InboxStoreADO.cs | 239 | 209.0 | 87.5% |
| Outbox/OutboxStoreADO.cs | 152 | 139.0 | 91.5% |
| Scheduling/ScheduledMessageStoreADO.cs | 218 | 200.0 | 91.7% |
| Sagas/SagaStoreADO.cs | 206 | 192.0 | 93.2% |
| Tenancy/ADOTenancyOptions.cs | 5 | 5.0 | 100.0% |
| ReadWriteSeparation/ReadWriteConnectionFactory.cs | 39 | 39.0 | 100.0% |

### Encina.AmazonSQS (49.1%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| ServiceCollectionExtensions.cs | 35 | 12.5 | 35.7% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 65 | 31.0 | 47.7% |
| AmazonSQSMessagePublisher.cs | 153 | 76.0 | 49.7% |
| Health/AmazonSQSHealthCheck.cs | 14 | 7.0 | 50.0% |
| EncinaAmazonSQSOptions.cs | 9 | 9.0 | 100.0% |

### Encina.Aspire.Testing (58.1%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| EncinaTestContext.cs | 36 | 18.0 | 50.0% |
| EncinaTestSupportOptions.cs | 7 | 7.0 | 100.0% |

### Encina.AspNetCore (23.8%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| ApplicationBuilderExtensions.cs | 1 | 0.0 | 0.0% |
| DPIAEndpointExtensions.cs | 253 | 0.0 | 0.0% |
| HttpRegionContextProvider.cs | 55 | 0.0 | 0.0% |
| ProblemDetailsExtensions.cs | 70 | 0.0 | 0.0% |
| HttpDataResidencyContextExtensions.cs | 6 | 1.0 | 16.7% |
| Health/HealthCheckBuilderExtensions.cs | 217 | 57.5 | 26.5% |
| ServiceCollectionExtensions.cs | 19 | 6.0 | 31.6% |
| AuthorizationPipelineBehavior.cs | 175 | 58.3 | 33.3% |
| EncinaContextMiddleware.cs | 79 | 35.5 | 44.9% |
| Health/CompositeEncinaHealthCheck.cs | 36 | 16.5 | 45.8% |
| HttpAuditContextExtensions.cs | 12 | 6.0 | 50.0% |
| RequestContextAccessor.cs | 3 | 1.5 | 50.0% |
| Health/EmptyHealthCheck.cs | 2 | 1.0 | 50.0% |
| Health/EncinaHealthCheckAdapter.cs | 25 | 12.5 | 50.0% |
| Authorization/AuthorizationConfigurationExtensions.cs | 31 | 15.5 | 50.0% |
| Authorization/ResourceAuthorizeAttribute.cs | 5 | 2.5 | 50.0% |
| Authorization/ResourceAuthorizer.cs | 49 | 24.5 | 50.0% |
| EncinaAspNetCoreOptions.cs | 8 | 8.0 | 100.0% |
| Authorization/AuthorizationConfiguration.cs | 4 | 4.0 | 100.0% |

### Encina.Audit.Marten (20.8%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| MartenAuditRetentionService.cs | 49 | 0.0 | 0.0% |
| MartenAuditStore.cs | 202 | 0.0 | 0.0% |
| MartenReadAuditStore.cs | 158 | 0.0 | 0.0% |
| Projections/AuditEntryProjection.cs | 53 | 0.0 | 0.0% |
| Projections/AuditEntryReadModel.cs | 20 | 0.0 | 0.0% |
| Projections/ConfigureMartenAuditProjections.cs | 22 | 0.0 | 0.0% |
| Projections/ReadAuditEntryProjection.cs | 46 | 0.0 | 0.0% |
| Projections/ReadAuditEntryReadModel.cs | 13 | 0.0 | 0.0% |
| Health/MartenAuditHealthCheck.cs | 42 | 0.0 | 0.0% |
| Diagnostics/MartenAuditActivitySource.cs | 40 | 0.0 | 0.0% |
| Diagnostics/MartenAuditMeter.cs | 41 | 0.0 | 0.0% |
| Crypto/MartenTemporalKeyProvider.cs | 136 | 0.0 | 0.0% |
| Crypto/TemporalKeyDestroyedMarker.cs | 4 | 0.0 | 0.0% |
| Crypto/TemporalKeyDocument.cs | 6 | 0.0 | 0.0% |
| Crypto/TemporalPeriodHelper.cs | 56 | 14.0 | 25.0% |
| ServiceCollectionExtensions.cs | 20 | 8.0 | 40.0% |
| Events/EncryptedField.cs | 71 | 30.0 | 42.2% |
| Crypto/InMemoryTemporalKeyProvider.cs | 136 | 60.5 | 44.5% |
| Events/AuditEntryRecordedEvent.cs | 19 | 9.5 | 50.0% |
| Events/ReadAuditEntryRecordedEvent.cs | 12 | 6.0 | 50.0% |
| Crypto/TemporalKeyInfo.cs | 7 | 3.5 | 50.0% |
| AuditEventEncryptor.cs | 132 | 66.5 | 50.4% |
| MartenAuditErrors.cs | 81 | 81.0 | 100.0% |
| MartenAuditOptions.cs | 7 | 7.0 | 100.0% |

### Encina.AwsLambda (47.8%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 35 | 0.0 | 0.0% |
| Health/AwsLambdaHealthCheck.cs | 26 | 12.5 | 48.1% |
| LambdaContextExtensions.cs | 49 | 24.5 | 50.0% |
| ApiGatewayResponseExtensions.cs | 122 | 61.5 | 50.4% |
| SqsMessageHandler.cs | 79 | 40.5 | 51.3% |
| EventBridgeHandler.cs | 55 | 29.5 | 53.6% |
| ServiceCollectionExtensions.cs | 8 | 5.5 | 68.8% |
| EncinaAwsLambdaOptions.cs | 9 | 9.0 | 100.0% |

### Encina.AzureFunctions (33.8%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| Durable/OrchestrationContextExtensions.cs | 78 | 10.5 | 13.5% |
| HttpResponseDataExtensions.cs | 90 | 13.5 | 15.0% |
| Durable/FanOutFanInExtensions.cs | 63 | 11.5 | 18.2% |
| Durable/DurableSagaBuilder.cs | 126 | 33.0 | 26.2% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 25 | 11.0 | 44.0% |
| Durable/DurableFunctionsHealthCheck.cs | 34 | 16.5 | 48.5% |
| FunctionContextExtensions.cs | 48 | 24.0 | 50.0% |
| Durable/ActivityResult.cs | 34 | 17.0 | 50.0% |
| Durable/DurableServiceCollectionExtensions.cs | 7 | 3.5 | 50.0% |
| Health/AzureFunctionsHealthCheck.cs | 22 | 12.0 | 54.5% |
| EncinaFunctionMiddleware.cs | 29 | 17.0 | 58.6% |
| ServiceCollectionExtensions.cs | 10 | 6.5 | 65.0% |
| FunctionsWorkerApplicationBuilderExtensions.cs | 3 | 2.0 | 66.7% |
| EncinaAzureFunctionsOptions.cs | 11 | 11.0 | 100.0% |
| Durable/DurableFunctionsOptions.cs | 11 | 11.0 | 100.0% |

### Encina.AzureServiceBus (51.0%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| AzureServiceBusMessagePublisher.cs | 97 | 45.5 | 46.9% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 60 | 30.0 | 50.0% |
| ServiceCollectionExtensions.cs | 24 | 12.0 | 50.0% |
| Health/AzureServiceBusHealthCheck.cs | 10 | 5.0 | 50.0% |
| EncinaAzureServiceBusOptions.cs | 10 | 10.0 | 100.0% |

### Encina.Caching (35.8%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| Sharding/DirectoryCacheInvalidationMessage.cs | 4 | 0.0 | 0.0% |
| Sharding/Services/TopologyRefreshHostedService.cs | 50 | 0.0 | 0.0% |
| Health/QueryCacheHealthCheck.cs | 20 | 0.0 | 0.0% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 60 | 7.2 | 12.0% |
| Behaviors/DistributedIdempotencyPipelineBehavior.cs | 80 | 22.0 | 27.5% |
| Sharding/CachedShardDirectoryStore.cs | 107 | 30.0 | 28.0% |
| Sharding/ShardingCacheServiceCollectionExtensions.cs | 111 | 33.0 | 29.7% |
| Behaviors/QueryCachingPipelineBehavior.cs | 77 | 28.0 | 36.4% |
| Sharding/CachedShardTopologyProvider.cs | 61 | 24.0 | 39.3% |
| Behaviors/CacheInvalidationPipelineBehavior.cs | 63 | 25.7 | 40.7% |
| Sharding/CachedShardedQueryExecutor.cs | 86 | 40.0 | 46.5% |
| Abstractions/IQueryCacheKeyGenerator.cs | 1 | 0.5 | 50.0% |
| Attributes/CacheableQueryAttribute.cs | 19 | 9.5 | 50.0% |
| Attributes/CacheAttribute.cs | 11 | 5.5 | 50.0% |
| Attributes/InvalidatesCacheAttribute.cs | 9 | 4.5 | 50.0% |
| ServiceCollectionExtensions.cs | 41 | 20.5 | 50.0% |
| Sharding/ShardCacheKeyGenerator.cs | 6 | 3.0 | 50.0% |
| KeyGeneration/DefaultCacheKeyGenerator.cs | 82 | 41.5 | 50.6% |
| Configuration/CachingOptions.cs | 20 | 20.0 | 100.0% |
| Sharding/Configuration/DirectoryCacheOptions.cs | 5 | 5.0 | 100.0% |
| Sharding/Configuration/ScatterGatherCacheOptions.cs | 4 | 4.0 | 100.0% |
| Sharding/Configuration/ShardingCacheOptions.cs | 7 | 7.0 | 100.0% |

### Encina.Caching.Dragonfly (43.8%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| ServiceCollectionExtensions.cs | 16 | 7.0 | 43.8% |

### Encina.Caching.Garnet (43.8%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| ServiceCollectionExtensions.cs | 16 | 7.0 | 43.8% |

### Encina.Caching.Hybrid (33.3%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 35 | 3.3 | 9.5% |
| HybridCacheProvider.cs | 166 | 61.5 | 37.0% |
| ServiceCollectionExtensions.cs | 12 | 6.0 | 50.0% |

### Encina.Caching.KeyDB (43.8%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| ServiceCollectionExtensions.cs | 16 | 7.0 | 43.8% |

### Encina.Caching.Memory (46.9%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 89 | 10.0 | 11.2% |
| ServiceCollectionExtensions.cs | 9 | 4.5 | 50.0% |
| MemoryDistributedLockProvider.cs | 77 | 44.0 | 57.1% |
| MemoryPubSubProvider.cs | 101 | 58.5 | 57.9% |
| MemoryCacheProvider.cs | 110 | 64.0 | 58.2% |

### Encina.Caching.Redis (35.1%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| RedisDistributedLockProvider.cs | 86 | 15.0 | 17.4% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 89 | 16.0 | 18.0% |
| ServiceCollectionExtensions.cs | 49 | 19.5 | 39.8% |
| RedisPubSubProvider.cs | 96 | 42.5 | 44.3% |
| Health/RedisHealthCheck.cs | 23 | 10.5 | 45.6% |
| RedisCacheProvider.cs | 129 | 62.0 | 48.1% |

### Encina.Caching.Valkey (43.8%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| ServiceCollectionExtensions.cs | 16 | 7.0 | 43.8% |

### Encina.Cdc (39.0%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| Sharding/CdcDrivenRefreshHandler.cs | 34 | 0.0 | 0.0% |
| Sharding/ShardedCaptureOptions.cs | 7 | 0.0 | 0.0% |
| Sharding/ShardedCdcConnector.cs | 123 | 0.0 | 0.0% |
| Messaging/OutboxCdcHandler.cs | 73 | 0.0 | 0.0% |
| DeadLetter/InMemoryCdcDeadLetterStore.cs | 22 | 0.0 | 0.0% |
| Caching/CacheInvalidationSubscriberHealthCheck.cs | 21 | 0.0 | 0.0% |
| Caching/CacheInvalidationSubscriberService.cs | 41 | 0.0 | 0.0% |
| ServiceCollectionExtensions.cs | 58 | 11.0 | 19.0% |
| Processing/CdcProcessor.cs | 115 | 41.0 | 35.6% |
| Caching/Diagnostics/CacheInvalidationActivitySource.cs | 42 | 15.0 | 35.7% |
| DeadLetter/CdcDeadLetterEntry.cs | 9 | 3.5 | 38.9% |
| Sharding/ShardedCdcProcessor.cs | 76 | 32.0 | 42.1% |
| ChangeContext.cs | 4 | 2.0 | 50.0% |
| ShardedChangeEvent.cs | 4 | 2.0 | 50.0% |
| Health/CdcDeadLetterHealthCheck.cs | 54 | 27.0 | 50.0% |
| Caching/CdcTableNameResolver.cs | 7 | 3.5 | 50.0% |
| Processing/CdcDispatcher.cs | 118 | 61.0 | 51.7% |
| Health/ShardedCdcHealthCheck.cs | 64 | 35.0 | 54.7% |
| Health/CdcHealthCheck.cs | 53 | 29.5 | 55.7% |
| Messaging/CdcChangeNotification.cs | 25 | 14.0 | 56.0% |
| DeadLetter/Diagnostics/CdcDeadLetterMetrics.cs | 27 | 16.0 | 59.3% |
| Caching/QueryCacheInvalidationCdcHandler.cs | 64 | 38.5 | 60.2% |
| Errors/CdcErrors.cs | 13 | 8.0 | 61.5% |
| ChangeEvent.cs | 6 | 4.0 | 66.7% |
| ChangeMetadata.cs | 6 | 4.0 | 66.7% |
| Processing/InMemoryShardedCdcPositionStore.cs | 25 | 17.0 | 68.0% |
| Messaging/CdcMessagingBridge.cs | 30 | 20.5 | 68.3% |
| Processing/InMemoryCdcPositionStore.cs | 13 | 9.0 | 69.2% |
| CdcConfiguration.cs | 40 | 28.0 | 70.0% |
| CdcOptions.cs | 12 | 12.0 | 100.0% |
| Messaging/CdcMessagingOptions.cs | 14 | 14.0 | 100.0% |
| DeadLetter/CdcDeadLetterHealthCheckOptions.cs | 2 | 2.0 | 100.0% |
| Caching/QueryCacheInvalidationOptions.cs | 5 | 5.0 | 100.0% |
| Caching/Diagnostics/CacheInvalidationMetrics.cs | 26 | 26.0 | 100.0% |

### Encina.Cdc.Debezium (23.0%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| DebeziumCdcConnector.cs | 59 | 0.0 | 0.0% |
| DebeziumHttpListener.cs | 81 | 0.0 | 0.0% |
| Kafka/DebeziumKafkaConnector.cs | 163 | 0.0 | 0.0% |
| DebeziumEventMapper.cs | 76 | 35.5 | 46.7% |
| Kafka/DebeziumKafkaPosition.cs | 44 | 24.0 | 54.5% |
| ServiceCollectionExtensions.cs | 26 | 15.0 | 57.7% |
| DebeziumCdcPosition.cs | 17 | 11.3 | 66.7% |
| Kafka/DebeziumKafkaHealthCheck.cs | 3 | 2.5 | 83.3% |
| Health/DebeziumCdcHealthCheck.cs | 3 | 2.5 | 83.3% |
| DebeziumCdcOptions.cs | 10 | 10.0 | 100.0% |
| Kafka/DebeziumKafkaOptions.cs | 13 | 13.0 | 100.0% |

### Encina.Cdc.MongoDb (60.5%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| Health/MongoCdcHealthCheck.cs | 3 | 1.0 | 33.3% |
| MongoCdcOptions.cs | 7 | 3.5 | 50.0% |
| ServiceCollectionExtensions.cs | 9 | 5.5 | 61.1% |
| MongoCdcPosition.cs | 24 | 16.0 | 66.7% |

### Encina.Cdc.MySql (68.4%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| Health/MySqlCdcHealthCheck.cs | 3 | 1.0 | 33.3% |
| ServiceCollectionExtensions.cs | 9 | 5.5 | 61.1% |
| MySqlCdcPosition.cs | 46 | 30.0 | 65.2% |
| MySqlCdcOptions.cs | 10 | 10.0 | 100.0% |

### Encina.Cdc.PostgreSql (67.1%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| Health/PostgresCdcHealthCheck.cs | 3 | 1.0 | 33.3% |
| ServiceCollectionExtensions.cs | 9 | 5.5 | 61.1% |
| PostgresCdcPosition.cs | 20 | 12.7 | 63.3% |
| PostgresCdcOptions.cs | 7 | 7.0 | 100.0% |

### Encina.Cdc.SqlServer (65.3%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| Health/SqlServerCdcHealthCheck.cs | 3 | 1.0 | 33.3% |
| ServiceCollectionExtensions.cs | 9 | 5.5 | 61.1% |
| SqlServerCdcPosition.cs | 20 | 12.7 | 63.3% |
| SqlServerCdcOptions.cs | 5 | 5.0 | 100.0% |

### Encina.Cli (22.2%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| Program.cs | 5 | 0.0 | 0.0% |
| obj/Release/net10.0/System.Text.RegularExpressions.Generator/System.Text.RegularExpressions.Generator.RegexGenerator/RegexGenerator.g.cs | 87 | 0.0 | 0.0% |
| Commands/AddCommand.cs | 185 | 0.0 | 0.0% |
| Commands/GenerateCommand.cs | 303 | 0.0 | 0.0% |
| Commands/NewCommand.cs | 77 | 0.0 | 0.0% |
| Services/PackageManager.cs | 111 | 1.5 | 1.4% |
| Services/ProjectScaffolder.cs | 181 | 91.0 | 50.3% |
| Services/CodeGenerator.cs | 396 | 206.0 | 52.0% |

### Encina.Compliance.AIAct (49.5%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| Notifications/HumanOversightRequiredNotification.cs | 5 | 0.0 | 0.0% |
| Notifications/ProhibitedUseBlockedNotification.cs | 5 | 0.0 | 0.0% |
| Model/BiasIndicator.cs | 6 | 0.0 | 0.0% |
| Model/DataGap.cs | 4 | 0.0 | 0.0% |
| Model/ReclassificationRecord.cs | 6 | 0.0 | 0.0% |
| Notifications/AISystemReclassifiedNotification.cs | 6 | 1.0 | 16.7% |
| Model/TechnicalDocumentation.cs | 9 | 1.5 | 16.7% |
| DefaultDataQualityValidator.cs | 25 | 8.3 | 33.3% |
| AIActCompliancePipelineBehavior.cs | 112 | 39.7 | 35.4% |
| AIActOptionsValidator.cs | 14 | 5.0 | 35.7% |
| DefaultAIActComplianceValidator.cs | 61 | 25.0 | 41.0% |
| Diagnostics/AIActDiagnostics.cs | 41 | 17.0 | 41.5% |
| Health/AIActHealthCheck.cs | 69 | 30.0 | 43.5% |
| AIActAutoRegistrationDescriptor.cs | 1 | 0.5 | 50.0% |
| AIActAutoRegistrationHostedService.cs | 50 | 25.0 | 50.0% |
| DefaultAIActDocumentation.cs | 20 | 10.0 | 50.0% |
| Model/AIActComplianceResult.cs | 7 | 3.5 | 50.0% |
| Model/AISystemRegistration.cs | 10 | 5.0 | 50.0% |
| Model/BiasReport.cs | 5 | 2.5 | 50.0% |
| Model/DataQualityReport.cs | 8 | 4.0 | 50.0% |
| Model/HumanDecisionRecord.cs | 8 | 4.0 | 50.0% |
| Attributes/AITransparencyAttribute.cs | 5 | 2.5 | 50.0% |
| Attributes/HighRiskAIAttribute.cs | 5 | 2.5 | 50.0% |
| Attributes/RequireHumanOversightAttribute.cs | 2 | 1.0 | 50.0% |
| ServiceCollectionExtensions.cs | 27 | 14.0 | 51.9% |
| DefaultAIActClassifier.cs | 53 | 30.5 | 57.5% |
| DefaultHumanOversightEnforcer.cs | 12 | 7.5 | 62.5% |
| InMemoryAISystemRegistry.cs | 42 | 26.5 | 63.1% |
| AIActErrors.cs | 74 | 74.0 | 100.0% |
| AIActOptions.cs | 4 | 4.0 | 100.0% |

### Encina.Compliance.Anonymization (37.2%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| AnonymizationAutoRegistrationDescriptor.cs | 1 | 0.0 | 0.0% |
| AnonymizationAutoRegistrationHostedService.cs | 41 | 0.0 | 0.0% |
| Attributes/AnonymizeAttribute.cs | 4 | 0.0 | 0.0% |
| Attributes/PseudonymizeAttribute.cs | 2 | 0.0 | 0.0% |
| Attributes/TokenizeAttribute.cs | 2 | 0.0 | 0.0% |
| ServiceCollectionExtensions.cs | 33 | 0.0 | 0.0% |
| Diagnostics/AnonymizationDiagnostics.cs | 94 | 0.0 | 0.0% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 185 | 0.4 | 0.2% |
| AnonymizationPipelineBehavior.cs | 225 | 7.0 | 3.1% |
| Techniques/DataMaskingTechnique.cs | 59 | 16.5 | 28.0% |
| AnonymizationOptionsValidator.cs | 9 | 3.0 | 33.3% |
| Techniques/PerturbationTechnique.cs | 39 | 15.0 | 38.5% |
| DefaultAnonymizer.cs | 111 | 50.0 | 45.0% |
| Techniques/GeneralizationTechnique.cs | 75 | 35.5 | 47.3% |
| DefaultPseudonymizer.cs | 169 | 81.5 | 48.2% |
| DefaultTokenizer.cs | 159 | 77.5 | 48.7% |
| DefaultRiskAssessor.cs | 133 | 65.5 | 49.2% |
| TokenMappingEntity.cs | 7 | 3.5 | 50.0% |
| Techniques/SuppressionTechnique.cs | 13 | 6.5 | 50.0% |
| Techniques/SwappingTechnique.cs | 13 | 6.5 | 50.0% |
| Model/AnonymizationAuditEntry.cs | 19 | 9.5 | 50.0% |
| Model/AnonymizationResult.cs | 4 | 2.0 | 50.0% |
| Model/RiskAssessmentResult.cs | 7 | 3.5 | 50.0% |
| Model/TokenMapping.cs | 17 | 8.5 | 50.0% |
| Health/AnonymizationHealthCheck.cs | 52 | 26.0 | 50.0% |
| TokenMappingMapper.cs | 22 | 12.0 | 54.5% |
| InMemory/InMemoryAnonymizationAuditStore.cs | 25 | 14.0 | 56.0% |
| InMemory/InMemoryTokenMappingStore.cs | 38 | 22.0 | 57.9% |
| InMemory/InMemoryKeyProvider.cs | 61 | 41.0 | 67.2% |
| Model/KeyInfo.cs | 5 | 4.0 | 80.0% |
| Model/FieldAnonymizationRule.cs | 3 | 2.5 | 83.3% |
| AnonymizationErrors.cs | 125 | 125.0 | 100.0% |
| AnonymizationOptions.cs | 5 | 5.0 | 100.0% |
| Model/AnonymizationProfile.cs | 13 | 13.0 | 100.0% |
| Model/TokenizationOptions.cs | 3 | 3.0 | 100.0% |

### Encina.Compliance.Attestation (29.6%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| AttestationErrors.cs | 69 | 0.0 | 0.0% |
| AttestationHasher.cs | 9 | 0.0 | 0.0% |
| Attributes/AttestDecisionAttribute.cs | 3 | 0.0 | 0.0% |
| Health/AttestationHealthCheck.cs | 36 | 0.5 | 1.4% |
| Behaviors/AttestationPipelineBehavior.cs | 49 | 2.3 | 4.8% |
| Providers/HttpAttestationProvider.cs | 172 | 15.5 | 9.0% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 45 | 5.2 | 11.6% |
| Validation/HttpAttestationOptionsValidator.cs | 45 | 12.3 | 27.4% |
| Model/AuditRecord.cs | 9 | 2.5 | 27.8% |
| Diagnostics/AttestationDiagnostics.cs | 35 | 14.0 | 40.0% |
| Providers/InMemoryAttestationProvider.cs | 139 | 61.0 | 43.9% |
| Model/AttestationReceipt.cs | 9 | 4.0 | 44.4% |
| Providers/HashChainAttestationProvider.cs | 185 | 87.5 | 47.3% |
| ServiceCollectionExtensions.cs | 51 | 25.0 | 49.0% |
| HttpAttestationOptions.cs | 8 | 4.0 | 50.0% |
| Model/AttestationVerification.cs | 5 | 2.5 | 50.0% |
| ContentHasher.cs | 28 | 14.5 | 51.8% |
| AttestationOptions.cs | 18 | 18.0 | 100.0% |
| HashChainOptions.cs | 3 | 3.0 | 100.0% |

### Encina.Compliance.BreachNotification (39.7%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| BreachDeadlineMonitorService.cs | 101 | 0.0 | 0.0% |
| BreachNotificationMartenExtensions.cs | 4 | 0.0 | 0.0% |
| Notifications/AuthorityNotifiedNotification.cs | 4 | 0.0 | 0.0% |
| Notifications/BreachDetectedNotification.cs | 5 | 0.0 | 0.0% |
| Notifications/BreachResolvedNotification.cs | 4 | 0.0 | 0.0% |
| Notifications/DeadlineWarningNotification.cs | 4 | 0.0 | 0.0% |
| Notifications/SubjectsNotifiedNotification.cs | 4 | 0.0 | 0.0% |
| ServiceCollectionExtensions.cs | 26 | 0.0 | 0.0% |
| Model/DeadlineStatus.cs | 6 | 0.0 | 0.0% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 280 | 1.6 | 0.6% |
| Services/DefaultBreachNotificationService.cs | 255 | 49.5 | 19.4% |
| Diagnostics/BreachNotificationDiagnostics.cs | 90 | 27.0 | 30.0% |
| BreachDetectionPipelineBehavior.cs | 80 | 26.7 | 33.3% |
| BreachNotificationOptionsValidator.cs | 35 | 11.7 | 33.3% |
| Model/NotificationResult.cs | 5 | 2.0 | 40.0% |
| Health/BreachNotificationHealthCheck.cs | 98 | 46.0 | 46.9% |
| Model/SecurityEvent.cs | 18 | 8.5 | 47.2% |
| Attributes/BreachMonitoredAttribute.cs | 1 | 0.5 | 50.0% |
| ReadModels/BreachProjection.cs | 60 | 30.0 | 50.0% |
| ReadModels/BreachReadModel.cs | 32 | 16.0 | 50.0% |
| Model/BreachAuditEntry.cs | 15 | 7.5 | 50.0% |
| Model/BreachRecord.cs | 33 | 16.5 | 50.0% |
| Model/PhasedReport.cs | 15 | 7.5 | 50.0% |
| Model/PotentialBreach.cs | 6 | 3.0 | 50.0% |
| Detection/SecurityEventFactory.cs | 47 | 23.5 | 50.0% |
| Detection/Rules/AnomalousQueryPatternRule.cs | 32 | 16.0 | 50.0% |
| Detection/Rules/MassDataExfiltrationRule.cs | 32 | 16.0 | 50.0% |
| Detection/Rules/PrivilegeEscalationRule.cs | 28 | 14.0 | 50.0% |
| Detection/Rules/UnauthorizedAccessRule.cs | 32 | 16.0 | 50.0% |
| Detection/DefaultBreachDetector.cs | 56 | 31.0 | 55.4% |
| DefaultBreachNotifier.cs | 38 | 21.5 | 56.6% |
| Events/BreachNotificationEvents.cs | 60 | 40.0 | 66.7% |
| Aggregates/BreachAggregate.cs | 152 | 128.0 | 84.2% |
| BreachNotificationErrors.cs | 156 | 147.0 | 94.2% |
| BreachNotificationOptions.cs | 21 | 21.0 | 100.0% |

### Encina.Compliance.Consent (49.5%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| ConsentAutoRegistrationDescriptor.cs | 1 | 0.0 | 0.0% |
| ConsentAutoRegistrationHostedService.cs | 42 | 0.0 | 0.0% |
| ConsentMartenExtensions.cs | 4 | 0.0 | 0.0% |
| ServiceCollectionExtensions.cs | 23 | 0.0 | 0.0% |
| Health/ConsentHealthCheck.cs | 43 | 0.0 | 0.0% |
| ConsentOptionsValidator.cs | 25 | 7.3 | 29.3% |
| ConsentRequiredPipelineBehavior.cs | 97 | 37.0 | 38.1% |
| DefaultConsentValidator.cs | 44 | 18.0 | 40.9% |
| Services/DefaultConsentService.cs | 216 | 93.5 | 43.3% |
| Diagnostics/ConsentDiagnostics.cs | 48 | 21.0 | 43.8% |
| Events/ConsentEvents.cs | 58 | 26.0 | 44.8% |
| Attributes/RequireConsentAttribute.cs | 9 | 4.5 | 50.0% |
| Model/ConsentValidationResult.cs | 18 | 9.0 | 50.0% |
| ReadModels/ConsentProjection.cs | 52 | 26.0 | 50.0% |
| ReadModels/ConsentReadModel.cs | 16 | 8.0 | 50.0% |
| Aggregates/ConsentAggregate.cs | 115 | 92.0 | 80.0% |
| ConsentErrors.cs | 96 | 96.0 | 100.0% |
| ConsentOptions.cs | 21 | 21.0 | 100.0% |

### Encina.Compliance.CrossBorderTransfer (25.6%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| CrossBorderTransferMartenExtensions.cs | 5 | 0.0 | 0.0% |
| ServiceCollectionExtensions.cs | 22 | 0.0 | 0.0% |
| ReadModels/SCCAgreementReadModel.cs | 16 | 0.0 | 0.0% |
| ReadModels/TIAReadModel.cs | 15 | 0.0 | 0.0% |
| Notifications/TransferExpirationMonitor.cs | 126 | 0.0 | 0.0% |
| Notifications/TransferNotificationEvents.cs | 43 | 0.0 | 0.0% |
| Model/SCCValidationResult.cs | 6 | 0.0 | 0.0% |
| Attributes/RequiresCrossBorderTransferAttribute.cs | 4 | 0.0 | 0.0% |
| Services/DefaultTIAService.cs | 209 | 4.5 | 2.1% |
| Pipeline/TransferBlockingPipelineBehavior.cs | 126 | 3.0 | 2.4% |
| Services/DefaultSCCService.cs | 132 | 4.5 | 3.4% |
| ReadModels/ApprovedTransferReadModel.cs | 18 | 4.0 | 22.2% |
| Services/DefaultTransferValidator.cs | 129 | 33.7 | 26.1% |
| Errors/CrossBorderTransferErrors.cs | 108 | 35.0 | 32.4% |
| Diagnostics/CrossBorderTransferDiagnostics.cs | 59 | 19.5 | 33.0% |
| Model/TransferRequest.cs | 6 | 2.0 | 33.3% |
| Events/TIAEvents.cs | 32 | 12.5 | 39.1% |
| Model/SupplementaryMeasure.cs | 5 | 2.0 | 40.0% |
| Events/SCCEvents.cs | 20 | 8.5 | 42.5% |
| Events/ApprovedTransferEvents.cs | 22 | 9.5 | 43.2% |
| Services/DefaultApprovedTransferService.cs | 129 | 61.5 | 47.7% |
| Aggregates/TIAAggregate.cs | 89 | 43.5 | 48.9% |
| Aggregates/SCCAgreementAggregate.cs | 63 | 31.0 | 49.2% |
| Model/TIARiskAssessment.cs | 4 | 2.0 | 50.0% |
| Model/TransferValidationOutcome.cs | 25 | 12.5 | 50.0% |
| Health/CrossBorderTransferHealthCheck.cs | 50 | 25.0 | 50.0% |
| Aggregates/ApprovedTransferAggregate.cs | 64 | 32.0 | 50.0% |
| Services/DefaultTIARiskAssessor.cs | 80 | 45.0 | 56.2% |
| CrossBorderTransferOptionsValidator.cs | 29 | 16.3 | 56.3% |
| CrossBorderTransferOptions.cs | 16 | 16.0 | 100.0% |

### Encina.Compliance.DataResidency (25.5%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| DataResidencyAttributeInfo.cs | 4 | 0.0 | 0.0% |
| DataResidencyAutoRegistrationDescriptor.cs | 1 | 0.0 | 0.0% |
| DataResidencyAutoRegistrationHostedService.cs | 104 | 0.0 | 0.0% |
| DataResidencyFluentPolicyDescriptor.cs | 1 | 0.0 | 0.0% |
| DataResidencyFluentPolicyHostedService.cs | 48 | 0.0 | 0.0% |
| DataResidencyMartenExtensions.cs | 4 | 0.0 | 0.0% |
| DataResidencyPipelineBehavior.cs | 130 | 0.0 | 0.0% |
| NoCrossBorderTransferInfo.cs | 3 | 0.0 | 0.0% |
| ServiceCollectionExtensions.cs | 35 | 0.0 | 0.0% |
| Services/DefaultDataLocationService.cs | 248 | 0.0 | 0.0% |
| Services/DefaultResidencyPolicyService.cs | 182 | 0.0 | 0.0% |
| ReadModels/DataLocationProjection.cs | 38 | 0.0 | 0.0% |
| ReadModels/DataLocationReadModel.cs | 16 | 0.0 | 0.0% |
| ReadModels/ResidencyPolicyProjection.cs | 26 | 0.0 | 0.0% |
| ReadModels/ResidencyPolicyReadModel.cs | 12 | 0.0 | 0.0% |
| Model/RegionGroup.cs | 18 | 0.0 | 0.0% |
| Diagnostics/DataResidencyDiagnostics.cs | 85 | 0.0 | 0.0% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 240 | 0.0 | 0.0% |
| DataResidencyOptionsValidator.cs | 18 | 4.7 | 25.9% |
| DefaultRegionRouter.cs | 84 | 31.0 | 36.9% |
| DefaultCrossBorderTransferValidator.cs | 65 | 26.3 | 40.5% |
| Health/DataResidencyHealthCheck.cs | 57 | 24.5 | 43.0% |
| ResidencyPolicyBuilder.cs | 29 | 14.5 | 50.0% |
| Model/TransferValidationResult.cs | 19 | 9.5 | 50.0% |
| Attributes/DataResidencyAttribute.cs | 6 | 3.0 | 50.0% |
| Attributes/NoCrossBorderTransferAttribute.cs | 2 | 1.0 | 50.0% |
| DefaultRegionContextProvider.cs | 20 | 12.5 | 62.5% |
| Events/DataLocationEvents.cs | 33 | 22.5 | 68.2% |
| Events/ResidencyPolicyEvents.cs | 16 | 12.0 | 75.0% |
| Aggregates/DataLocationAggregate.cs | 77 | 61.5 | 79.9% |
| Aggregates/ResidencyPolicyAggregate.cs | 44 | 36.5 | 83.0% |
| DefaultAdequacyDecisionProvider.cs | 21 | 18.0 | 85.7% |
| Model/Region.cs | 20 | 17.5 | 87.5% |
| Model/RegionRegistry.cs | 80 | 79.0 | 98.8% |
| DataResidencyErrors.cs | 95 | 95.0 | 100.0% |
| DataResidencyOptions.cs | 15 | 15.0 | 100.0% |

### Encina.Compliance.DataSubjectRights (33.0%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| Attributes/RestrictProcessingAttribute.cs | 1 | 0.0 | 0.0% |
| DefaultDataSubjectIdExtractor.cs | 22 | 0.0 | 0.0% |
| DSRAutoRegistrationDescriptor.cs | 1 | 0.0 | 0.0% |
| DSRAutoRegistrationHostedService.cs | 33 | 0.0 | 0.0% |
| DSRMartenExtensions.cs | 4 | 0.0 | 0.0% |
| Erasure/HardDeleteErasureStrategy.cs | 11 | 0.0 | 0.0% |
| Export/CsvExportFormatWriter.cs | 41 | 0.0 | 0.0% |
| Export/JsonExportFormatWriter.cs | 36 | 0.0 | 0.0% |
| Export/XmlExportFormatWriter.cs | 27 | 0.0 | 0.0% |
| Locators/CompositePersonalDataLocator.cs | 39 | 0.0 | 0.0% |
| Model/DSRAuditEntry.cs | 6 | 0.0 | 0.0% |
| Model/DSRRequest.cs | 23 | 0.0 | 0.0% |
| Model/PersonalDataField.cs | 5 | 0.0 | 0.0% |
| Notifications/DataRectifiedNotification.cs | 5 | 0.0 | 0.0% |
| Notifications/ProcessingRestrictedNotification.cs | 4 | 0.0 | 0.0% |
| Notifications/RestrictionLiftedNotification.cs | 4 | 0.0 | 0.0% |
| PersonalDataMap.cs | 38 | 0.0 | 0.0% |
| ProcessingRestrictionPipelineBehavior.cs | 102 | 0.0 | 0.0% |
| Requests/RectificationRequest.cs | 6 | 0.0 | 0.0% |
| Requests/RestrictionRequest.cs | 4 | 0.0 | 0.0% |
| ServiceCollectionExtensions.cs | 29 | 0.0 | 0.0% |
| Health/DataSubjectRightsHealthCheck.cs | 58 | 0.0 | 0.0% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 170 | 8.0 | 4.7% |
| Notifications/DataErasedNotification.cs | 5 | 1.0 | 20.0% |
| Attributes/PersonalDataAttribute.cs | 5 | 1.5 | 30.0% |
| Services/DefaultDSRService.cs | 497 | 159.5 | 32.1% |
| Diagnostics/DataSubjectRightsDiagnostics.cs | 97 | 31.5 | 32.5% |
| DataSubjectRightsOptionsValidator.cs | 17 | 5.7 | 33.3% |
| Events/DSRRequestEvents.cs | 44 | 20.0 | 45.5% |
| Model/AccessResponse.cs | 4 | 2.0 | 50.0% |
| Model/ErasureResult.cs | 5 | 2.5 | 50.0% |
| Model/ExportedData.cs | 5 | 2.5 | 50.0% |
| Model/PersonalDataLocation.cs | 8 | 4.0 | 50.0% |
| Model/PortabilityResponse.cs | 3 | 1.5 | 50.0% |
| Model/RetentionDetail.cs | 3 | 1.5 | 50.0% |
| Requests/AccessRequest.cs | 3 | 1.5 | 50.0% |
| Requests/ErasureRequest.cs | 4 | 2.0 | 50.0% |
| Requests/ObjectionRequest.cs | 4 | 2.0 | 50.0% |
| Requests/PortabilityRequest.cs | 4 | 2.0 | 50.0% |
| Projections/DSRRequestProjection.cs | 46 | 23.0 | 50.0% |
| Projections/DSRRequestReadModel.cs | 23 | 11.5 | 50.0% |
| Erasure/DefaultDataErasureExecutor.cs | 138 | 75.5 | 54.7% |
| Export/DefaultDataPortabilityExporter.cs | 73 | 44.0 | 60.3% |
| Model/ErasureScope.cs | 4 | 2.5 | 62.5% |
| Aggregates/DSRRequestAggregate.cs | 87 | 58.5 | 67.2% |
| DataSubjectRightsOptions.cs | 10 | 10.0 | 100.0% |
| DSRErrors.cs | 158 | 158.0 | 100.0% |

### Encina.Compliance.DPIA (52.0%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| DPIAMartenExtensions.cs | 4 | 0.0 | 0.0% |
| Notifications/DPIAAssessmentCompleted.cs | 5 | 0.0 | 0.0% |
| Notifications/DPIAAssessmentExpired.cs | 4 | 0.0 | 0.0% |
| Notifications/DPOConsultationRequested.cs | 4 | 0.0 | 0.0% |
| Model/DPIAAuditEntry.cs | 8 | 0.0 | 0.0% |
| ServiceCollectionExtensions.cs | 32 | 0.5 | 1.6% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 265 | 14.4 | 5.4% |
| Model/DPIAAssessment.cs | 16 | 4.0 | 25.0% |
| Services/DefaultDPIAService.cs | 374 | 109.5 | 29.3% |
| DPIAOptionsValidator.cs | 22 | 7.7 | 34.9% |
| Diagnostics/DPIADiagnostics.cs | 119 | 43.0 | 36.1% |
| DPIARequiredPipelineBehavior.cs | 147 | 54.7 | 37.2% |
| DPIAReviewReminderService.cs | 100 | 38.0 | 38.0% |
| Model/Mitigation.cs | 5 | 2.0 | 40.0% |
| ReadModels/DPIAReadModel.cs | 19 | 8.5 | 44.7% |
| Health/DPIAHealthCheck.cs | 99 | 46.5 | 47.0% |
| DPIAAutoRegistrationHostedService.cs | 88 | 43.0 | 48.9% |
| Attributes/RequiresDPIAAttribute.cs | 3 | 1.5 | 50.0% |
| ReadModels/DPIAProjection.cs | 62 | 31.0 | 50.0% |
| Model/DPIAContext.cs | 6 | 3.0 | 50.0% |
| Model/DPIASection.cs | 5 | 2.5 | 50.0% |
| Model/DPOConsultation.cs | 8 | 4.0 | 50.0% |
| Model/RiskItem.cs | 5 | 2.5 | 50.0% |
| RiskCriteria/SpecialCategoryDataCriterion.cs | 32 | 16.5 | 51.6% |
| DPIAAutoDetector.cs | 87 | 46.0 | 52.9% |
| RiskCriteria/SystematicProfilingCriterion.cs | 15 | 8.0 | 53.3% |
| RiskCriteria/AutomatedDecisionMakingCriterion.cs | 14 | 7.5 | 53.6% |
| RiskCriteria/LargeScaleProcessingCriterion.cs | 14 | 7.5 | 53.6% |
| DefaultDPIAAssessmentEngine.cs | 107 | 58.0 | 54.2% |
| RiskCriteria/SystematicMonitoringCriterion.cs | 9 | 5.0 | 55.6% |
| RiskCriteria/VulnerableSubjectsCriterion.cs | 9 | 5.0 | 55.6% |
| Events/DPIAEvents.cs | 47 | 29.5 | 62.8% |
| Aggregates/DPIAAggregate.cs | 118 | 83.0 | 70.3% |
| Model/DPIAResult.cs | 7 | 6.0 | 85.7% |
| DefaultDPIATemplateProvider.cs | 470 | 465.0 | 98.9% |
| DPIAAutoRegistrationDescriptor.cs | 1 | 1.0 | 100.0% |
| DPIAErrors.cs | 97 | 97.0 | 100.0% |
| DPIAOptions.cs | 15 | 15.0 | 100.0% |
| Model/DPIATemplate.cs | 6 | 6.0 | 100.0% |

### Encina.Compliance.GDPR (50.2%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| Abstractions/IConsentStatusProvider.cs | 3 | 0.0 | 0.0% |
| GDPRAutoRegistrationHostedService.cs | 16 | 0.0 | 0.0% |
| Diagnostics/LawfulBasisDiagnostics.cs | 30 | 0.0 | 0.0% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 70 | 0.0 | 0.0% |
| DefaultGDPRComplianceValidator.cs | 1 | 0.3 | 33.3% |
| Diagnostics/GDPRDiagnostics.cs | 58 | 19.5 | 33.6% |
| GDPROptionsValidator.cs | 23 | 8.0 | 34.8% |
| GDPRCompliancePipelineBehavior.cs | 102 | 37.3 | 36.6% |
| Health/GDPRHealthCheck.cs | 57 | 27.0 | 47.4% |
| Export/CsvRoPAExporter.cs | 69 | 34.0 | 49.3% |
| Export/JsonRoPAExporter.cs | 83 | 41.0 | 49.4% |
| Attributes/LawfulBasisAttribute.cs | 8 | 4.0 | 50.0% |
| Attributes/ProcessingActivityAttribute.cs | 9 | 4.5 | 50.0% |
| GDPRAutoRegistrationDescriptor.cs | 1 | 0.5 | 50.0% |
| Model/ComplianceResult.cs | 15 | 7.5 | 50.0% |
| Model/DataProtectionOfficer.cs | 1 | 0.5 | 50.0% |
| Model/ProcessingActivity.cs | 14 | 7.0 | 50.0% |
| ProcessingActivityEntity.cs | 14 | 7.0 | 50.0% |
| Export/RoPAExportResult.cs | 6 | 3.0 | 50.0% |
| Diagnostics/ProcessingActivityDiagnostics.cs | 55 | 27.5 | 50.0% |
| ServiceCollectionExtensions.cs | 25 | 13.0 | 52.0% |
| ProcessingActivityMapper.cs | 43 | 22.5 | 52.3% |
| Health/ProcessingActivityHealthCheck.cs | 54 | 28.5 | 52.8% |
| InMemoryProcessingActivityRegistry.cs | 61 | 33.0 | 54.1% |
| Export/RoPAExportMetadata.cs | 5 | 3.5 | 70.0% |
| GDPRErrors.cs | 151 | 151.0 | 100.0% |
| GDPROptions.cs | 8 | 8.0 | 100.0% |
| Export/RoPAExportErrors.cs | 9 | 9.0 | 100.0% |

### Encina.Compliance.LawfulBasis (27.5%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| LawfulBasisMartenExtensions.cs | 4 | 0.0 | 0.0% |
| Model/LawfulBasisValidationResult.cs | 18 | 0.0 | 0.0% |
| Model/LIAValidationResult.cs | 18 | 0.0 | 0.0% |
| ServiceCollectionExtensions.cs | 28 | 0.0 | 0.0% |
| Diagnostics/LawfulBasisDiagnostics.cs | 60 | 0.0 | 0.0% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 95 | 0.0 | 0.0% |
| Services/DefaultLawfulBasisProvider.cs | 42 | 1.0 | 2.4% |
| Pipeline/LawfulBasisValidationPipelineBehavior.cs | 216 | 5.3 | 2.5% |
| AutoRegistration/LawfulBasisAutoRegistrationHostedService.cs | 54 | 3.5 | 6.5% |
| Health/LawfulBasisHealthCheck.cs | 75 | 10.5 | 14.0% |
| LawfulBasisOptionsValidator.cs | 17 | 5.0 | 29.4% |
| Services/DefaultLawfulBasisService.cs | 338 | 108.0 | 31.9% |
| AutoRegistration/LawfulBasisAutoRegistrationDescriptor.cs | 3 | 1.0 | 33.3% |
| Events/LawfulBasisEvents.cs | 28 | 12.5 | 44.6% |
| Events/LIAEvents.cs | 42 | 19.5 | 46.4% |
| ReadModels/LawfulBasisProjection.cs | 29 | 14.5 | 50.0% |
| ReadModels/LawfulBasisReadModel.cs | 14 | 7.0 | 50.0% |
| ReadModels/LIAProjection.cs | 46 | 23.0 | 50.0% |
| ReadModels/LIAReadModel.cs | 25 | 12.5 | 50.0% |
| Aggregates/LawfulBasisAggregate.cs | 67 | 33.5 | 50.0% |
| Aggregates/LIAAggregate.cs | 107 | 53.5 | 50.0% |
| LawfulBasisOptions.cs | 15 | 15.0 | 100.0% |
| Errors/LawfulBasisErrors.cs | 60 | 60.0 | 100.0% |

### Encina.Compliance.NIS2 (35.0%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 255 | 10.0 | 3.9% |
| Model/SupplierInfo.cs | 12 | 3.0 | 25.0% |
| NIS2CompliancePipelineBehavior.cs | 155 | 42.3 | 27.3% |
| NIS2Errors.cs | 125 | 36.0 | 28.8% |
| DefaultSupplyChainSecurityValidator.cs | 83 | 25.3 | 30.5% |
| NIS2OptionsValidator.cs | 55 | 17.3 | 31.5% |
| DefaultNIS2ComplianceValidator.cs | 140 | 47.7 | 34.0% |
| Diagnostics/NIS2Diagnostics.cs | 91 | 34.0 | 37.4% |
| Evaluators/MultiFactorAuthenticationEvaluator.cs | 16 | 6.5 | 40.6% |
| DefaultEncryptionValidator.cs | 51 | 21.0 | 41.2% |
| ServiceCollectionExtensions.cs | 30 | 12.5 | 41.7% |
| Evaluators/HumanResourcesSecurityEvaluator.cs | 30 | 13.0 | 43.3% |
| DefaultNIS2IncidentHandler.cs | 139 | 63.5 | 45.7% |
| Evaluators/EffectivenessAssessmentEvaluator.cs | 26 | 12.0 | 46.1% |
| Health/NIS2ComplianceHealthCheck.cs | 61 | 28.5 | 46.7% |
| NIS2ResilienceHelper.cs | 34 | 16.0 | 47.1% |
| Evaluators/CryptographyEvaluator.cs | 22 | 10.5 | 47.7% |
| Attributes/NIS2CriticalAttribute.cs | 1 | 0.5 | 50.0% |
| Attributes/NIS2SupplyChainCheckAttribute.cs | 6 | 3.0 | 50.0% |
| Attributes/RequireMFAAttribute.cs | 1 | 0.5 | 50.0% |
| DefaultMFAEnforcer.cs | 2 | 1.0 | 50.0% |
| NIS2AttributeInfo.cs | 24 | 12.0 | 50.0% |
| Model/ManagementAccountabilityRecord.cs | 12 | 6.0 | 50.0% |
| Model/NIS2ComplianceResult.cs | 23 | 11.5 | 50.0% |
| Model/NIS2Incident.cs | 23 | 11.5 | 50.0% |
| Model/NIS2MeasureContext.cs | 4 | 2.0 | 50.0% |
| Model/NIS2MeasureResult.cs | 17 | 8.5 | 50.0% |
| Model/SupplierRisk.cs | 4 | 2.0 | 50.0% |
| Model/SupplyChainAssessment.cs | 13 | 6.5 | 50.0% |
| Evaluators/BusinessContinuityEvaluator.cs | 7 | 3.5 | 50.0% |
| Evaluators/CyberHygieneEvaluator.cs | 15 | 7.5 | 50.0% |
| Evaluators/IncidentHandlingEvaluator.cs | 20 | 10.0 | 50.0% |
| Evaluators/NetworkSecurityEvaluator.cs | 7 | 3.5 | 50.0% |
| Evaluators/RiskAnalysisEvaluator.cs | 59 | 29.5 | 50.0% |
| Evaluators/SupplyChainSecurityEvaluator.cs | 16 | 8.0 | 50.0% |
| NIS2Options.cs | 41 | 41.0 | 100.0% |

### Encina.Compliance.PrivacyByDesign (42.2%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| Notifications/PrivacyDefaultOverridden.cs | 5 | 0.0 | 0.0% |
| ServiceCollectionExtensions.cs | 20 | 0.0 | 0.0% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 130 | 8.8 | 6.8% |
| Model/PrivacyFieldInfo.cs | 4 | 1.0 | 25.0% |
| Notifications/DataMinimizationViolationDetected.cs | 7 | 2.0 | 28.6% |
| DataMinimizationPipelineBehavior.cs | 200 | 63.0 | 31.5% |
| DefaultPrivacyByDesignValidator.cs | 160 | 52.3 | 32.7% |
| PrivacyByDesignOptionsValidator.cs | 27 | 9.0 | 33.3% |
| Model/PrivacyViolation.cs | 5 | 2.0 | 40.0% |
| Model/PurposeDefinition.cs | 10 | 4.0 | 40.0% |
| Model/PurposeValidationResult.cs | 5 | 2.0 | 40.0% |
| Diagnostics/PrivacyByDesignDiagnostics.cs | 76 | 31.5 | 41.5% |
| Model/PrivacyValidationResult.cs | 8 | 3.5 | 43.8% |
| InMemoryPurposeRegistry.cs | 56 | 26.5 | 47.3% |
| Attributes/EnforceDataMinimizationAttribute.cs | 1 | 0.5 | 50.0% |
| Attributes/NotStrictlyNecessaryAttribute.cs | 2 | 1.0 | 50.0% |
| Attributes/PrivacyDefaultAttribute.cs | 4 | 2.0 | 50.0% |
| Attributes/PurposeLimitationAttribute.cs | 4 | 2.0 | 50.0% |
| FieldMetadataCache.cs | 14 | 7.0 | 50.0% |
| PurposeBuilder.cs | 21 | 10.5 | 50.0% |
| Model/DefaultPrivacyFieldInfo.cs | 5 | 2.5 | 50.0% |
| Model/MinimizationReport.cs | 6 | 3.0 | 50.0% |
| Model/UnnecessaryFieldInfo.cs | 5 | 2.5 | 50.0% |
| Health/PrivacyByDesignHealthCheck.cs | 50 | 25.0 | 50.0% |
| DefaultDataMinimizationAnalyzer.cs | 89 | 45.5 | 51.1% |
| PurposeRegistrationHostedService.cs | 43 | 26.0 | 60.5% |
| PrivacyByDesignErrors.cs | 99 | 99.0 | 100.0% |
| PrivacyByDesignOptions.cs | 23 | 23.0 | 100.0% |

### Encina.Compliance.ProcessorAgreements (36.8%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| ProcessorAgreementsMartenExtensions.cs | 4 | 0.0 | 0.0% |
| Notifications/DPASignedNotification.cs | 6 | 0.0 | 0.0% |
| Notifications/DPATerminatedNotification.cs | 5 | 0.0 | 0.0% |
| Notifications/ProcessorRegisteredNotification.cs | 4 | 0.0 | 0.0% |
| Notifications/SubProcessorAddedNotification.cs | 5 | 0.0 | 0.0% |
| Notifications/SubProcessorRemovedNotification.cs | 4 | 0.0 | 0.0% |
| Model/ProcessorAgreementAuditEntry.cs | 9 | 0.0 | 0.0% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 265 | 9.2 | 3.5% |
| Services/DefaultDPAService.cs | 314 | 59.0 | 18.8% |
| ProcessorValidationPipelineBehavior.cs | 126 | 39.7 | 31.5% |
| Events/ProcessorEvents.cs | 33 | 11.5 | 34.9% |
| ProcessorAgreementOptionsValidator.cs | 21 | 7.3 | 34.9% |
| Diagnostics/ProcessorAgreementDiagnostics.cs | 124 | 43.5 | 35.1% |
| Model/DPAValidationResult.cs | 8 | 3.0 | 37.5% |
| Services/DefaultProcessorService.cs | 156 | 61.5 | 39.4% |
| Events/DPAEvents.cs | 37 | 15.0 | 40.5% |
| Notifications/DPAExpiredNotification.cs | 6 | 2.5 | 41.7% |
| Notifications/DPAExpiringNotification.cs | 7 | 3.0 | 42.9% |
| Health/ProcessorAgreementHealthCheck.cs | 71 | 31.5 | 44.4% |
| ReadModels/DPAReadModel.cs | 22 | 10.0 | 45.5% |
| Aggregates/ProcessorAggregate.cs | 71 | 33.0 | 46.5% |
| Aggregates/DPAAggregate.cs | 96 | 45.5 | 47.4% |
| Attributes/RequiresProcessorAttribute.cs | 1 | 0.5 | 50.0% |
| ReadModels/DPAProjection.cs | 49 | 24.5 | 50.0% |
| ReadModels/ProcessorProjection.cs | 37 | 18.5 | 50.0% |
| ReadModels/ProcessorReadModel.cs | 14 | 7.0 | 50.0% |
| Model/DataProcessingAgreement.cs | 14 | 7.0 | 50.0% |
| Model/DPAMandatoryTerms.cs | 28 | 14.0 | 50.0% |
| Model/Processor.cs | 11 | 5.5 | 50.0% |
| ServiceCollectionExtensions.cs | 18 | 9.5 | 52.8% |
| Scheduling/CheckDPAExpirationHandler.cs | 101 | 58.0 | 57.4% |
| ProcessorAgreementErrors.cs | 136 | 136.0 | 100.0% |
| ProcessorAgreementOptions.cs | 11 | 11.0 | 100.0% |

### Encina.Compliance.Retention (31.6%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| Notifications/DataDeletedNotification.cs | 5 | 0.0 | 0.0% |
| Notifications/DataExpiringNotification.cs | 6 | 0.0 | 0.0% |
| Notifications/LegalHoldAppliedNotification.cs | 5 | 0.0 | 0.0% |
| Notifications/LegalHoldReleasedNotification.cs | 4 | 0.0 | 0.0% |
| Notifications/RetentionEnforcementCompletedNotification.cs | 3 | 0.0 | 0.0% |
| RetentionAutoRegistrationDescriptor.cs | 1 | 0.0 | 0.0% |
| RetentionAutoRegistrationHostedService.cs | 106 | 0.0 | 0.0% |
| RetentionEnforcementService.cs | 142 | 0.0 | 0.0% |
| RetentionFluentPolicyDescriptor.cs | 1 | 0.0 | 0.0% |
| RetentionFluentPolicyHostedService.cs | 44 | 0.0 | 0.0% |
| RetentionMartenExtensions.cs | 8 | 0.0 | 0.0% |
| RetentionValidationPipelineBehavior.cs | 155 | 0.0 | 0.0% |
| ServiceCollectionExtensions.cs | 34 | 0.0 | 0.0% |
| Health/RetentionHealthCheck.cs | 50 | 0.0 | 0.0% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 360 | 7.6 | 2.1% |
| Diagnostics/RetentionDiagnostics.cs | 128 | 37.0 | 28.9% |
| Events/RetentionRecordEvents.cs | 37 | 11.0 | 29.7% |
| RetentionOptionsValidator.cs | 21 | 7.0 | 33.3% |
| Services/DefaultRetentionRecordService.cs | 233 | 88.0 | 37.8% |
| Services/DefaultLegalHoldService.cs | 184 | 73.0 | 39.7% |
| Services/DefaultRetentionPolicyService.cs | 165 | 72.5 | 43.9% |
| ReadModels/RetentionRecordReadModel.cs | 17 | 7.5 | 44.1% |
| Attributes/RetentionPeriodAttribute.cs | 10 | 5.0 | 50.0% |
| ReadModels/LegalHoldProjection.cs | 20 | 10.0 | 50.0% |
| ReadModels/LegalHoldReadModel.cs | 12 | 6.0 | 50.0% |
| ReadModels/RetentionPolicyProjection.cs | 29 | 14.5 | 50.0% |
| ReadModels/RetentionPolicyReadModel.cs | 14 | 7.0 | 50.0% |
| ReadModels/RetentionRecordProjection.cs | 42 | 21.0 | 50.0% |
| Model/DeletionDetail.cs | 4 | 2.0 | 50.0% |
| Model/DeletionResult.cs | 7 | 3.5 | 50.0% |
| Model/ExpiringData.cs | 5 | 2.5 | 50.0% |
| Aggregates/RetentionRecordAggregate.cs | 77 | 39.5 | 51.3% |
| Events/RetentionPolicyEvents.cs | 22 | 15.5 | 70.5% |
| Events/LegalHoldEvents.cs | 13 | 9.5 | 73.1% |
| Aggregates/RetentionPolicyAggregate.cs | 56 | 47.5 | 84.8% |
| Aggregates/LegalHoldAggregate.cs | 35 | 30.0 | 85.7% |
| RetentionErrors.cs | 148 | 148.0 | 100.0% |
| RetentionOptions.cs | 45 | 45.0 | 100.0% |

### Encina.Dapper.MySQL (26.8%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| UnitOfWork/UnitOfWorkDapper.cs | 78 | 0.0 | 0.0% |
| TypeHandlers/ShardPrefixedIdTypeHandler.cs | 17 | 0.0 | 0.0% |
| TypeHandlers/SnowflakeIdTypeHandler.cs | 19 | 0.0 | 0.0% |
| TypeHandlers/UlidIdTypeHandler.cs | 19 | 0.0 | 0.0% |
| TypeHandlers/UuidV7IdTypeHandler.cs | 19 | 0.0 | 0.0% |
| Tenancy/TenantConnectionFactory.cs | 32 | 0.0 | 0.0% |
| SoftDelete/SoftDeleteEntityMappingBuilder.cs | 180 | 0.0 | 0.0% |
| SoftDelete/SoftDeleteSpecificationSqlBuilder.cs | 65 | 0.0 | 0.0% |
| Sharding/FunctionalShardedRepositoryDapper.cs | 607 | 0.0 | 0.0% |
| Sharding/ShardedReadWriteConnectionFactory.cs | 101 | 0.0 | 0.0% |
| Sharding/ShardingServiceCollectionExtensions.cs | 48 | 0.0 | 0.0% |
| Sharding/ReferenceTables/ReferenceTableStoreDapper.cs | 54 | 0.0 | 0.0% |
| Sharding/ReferenceTables/ReferenceTableStoreFactoryDapper.cs | 10 | 0.0 | 0.0% |
| Sharding/Migrations/MigrationServiceCollectionExtensions.cs | 5 | 0.0 | 0.0% |
| ReadWriteSeparation/ReadWriteRoutingPipelineBehavior.cs | 28 | 0.0 | 0.0% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 5 | 0.0 | 0.0% |
| ReadWriteSeparation/ReadWriteSeparationHealthCheck.cs | 60 | 0.0 | 0.0% |
| Pagination/CursorPaginationHelper.cs | 213 | 0.0 | 0.0% |
| Modules/SchemaValidatingCommand.cs | 66 | 0.0 | 0.0% |
| Extensions/AuditExtensions.cs | 4 | 0.0 | 0.0% |
| Auditing/AuditLogStoreDapper.cs | 56 | 0.0 | 0.0% |
| Auditing/AuditStoreDapper.cs | 222 | 0.0 | 0.0% |
| Auditing/ReadAuditStoreDapper.cs | 131 | 0.0 | 0.0% |
| Anonymization/TokenMappingStoreDapper.cs | 87 | 0.0 | 0.0% |
| ABAC/PolicyStoreDapper.cs | 188 | 0.0 | 0.0% |
| Health/DapperMySqlDatabaseHealthMonitor.cs | 34 | 1.0 | 2.9% |
| UnitOfWork/UnitOfWorkRepositoryDapper.cs | 423 | 66.0 | 15.6% |
| Repository/SpecificationSqlBuilder.cs | 256 | 55.5 | 21.7% |
| TypeHandlers/GuidTypeHandler.cs | 19 | 4.5 | 23.7% |
| ServiceCollectionExtensions.cs | 87 | 21.0 | 24.1% |
| Tenancy/TenantAwareSpecificationSqlBuilder.cs | 69 | 23.0 | 33.3% |
| Tenancy/TenantAwareFunctionalRepositoryDapper.cs | 421 | 150.0 | 35.6% |
| Repository/FunctionalRepositoryDapper.cs | 324 | 117.0 | 36.1% |
| Scheduling/ScheduledMessage.cs | 19 | 7.0 | 36.8% |
| Sagas/SagaState.cs | 10 | 4.0 | 40.0% |
| Tenancy/TenantEntityMappingBuilder.cs | 112 | 48.5 | 43.3% |
| Outbox/OutboxMessage.cs | 10 | 4.5 | 45.0% |
| Tenancy/TenancyServiceCollectionExtensions.cs | 62 | 28.5 | 46.0% |
| Scheduling/ScheduledMessageFactory.cs | 11 | 5.5 | 50.0% |
| Sagas/SagaStateFactory.cs | 11 | 5.5 | 50.0% |
| Outbox/OutboxMessageFactory.cs | 8 | 4.0 | 50.0% |
| Health/MySqlHealthCheck.cs | 8 | 4.0 | 50.0% |
| Outbox/OutboxProcessor.cs | 95 | 50.0 | 52.6% |
| Modules/SchemaValidatingConnection.cs | 39 | 25.0 | 64.1% |
| Modules/ModuleAwareConnectionFactory.cs | 25 | 19.0 | 76.0% |
| Repository/EntityMappingBuilder.cs | 73 | 55.5 | 76.0% |
| Sagas/SagaStoreDapper.cs | 126 | 96.0 | 76.2% |
| ProcessingActivity/ProcessingActivityRegistryDapper.cs | 71 | 55.0 | 77.5% |
| BulkOperations/BulkOperationsDapper.cs | 252 | 214.0 | 84.9% |
| Inbox/InboxStoreDapper.cs | 111 | 100.0 | 90.1% |
| Scheduling/ScheduledMessageStoreDapper.cs | 125 | 118.0 | 94.4% |
| Outbox/OutboxStoreDapper.cs | 79 | 77.0 | 97.5% |
| Tenancy/DapperTenancyOptions.cs | 5 | 5.0 | 100.0% |
| ReadWriteSeparation/ReadWriteConnectionFactory.cs | 39 | 39.0 | 100.0% |

### Encina.Dapper.PostgreSQL (25.9%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| UnitOfWork/UnitOfWorkDapper.cs | 78 | 0.0 | 0.0% |
| TypeHandlers/ShardPrefixedIdTypeHandler.cs | 17 | 0.0 | 0.0% |
| TypeHandlers/SnowflakeIdTypeHandler.cs | 19 | 0.0 | 0.0% |
| TypeHandlers/UlidIdTypeHandler.cs | 19 | 0.0 | 0.0% |
| TypeHandlers/UuidV7IdTypeHandler.cs | 19 | 0.0 | 0.0% |
| Tenancy/TenantConnectionFactory.cs | 32 | 0.0 | 0.0% |
| Temporal/TemporalEntityMappingBuilder.cs | 93 | 0.0 | 0.0% |
| Temporal/TemporalRepositoryDapper.cs | 223 | 0.0 | 0.0% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 35 | 0.0 | 0.0% |
| SoftDelete/SoftDeleteEntityMappingBuilder.cs | 180 | 0.0 | 0.0% |
| SoftDelete/SoftDeleteSpecificationSqlBuilder.cs | 65 | 0.0 | 0.0% |
| Sharding/FunctionalShardedRepositoryDapper.cs | 607 | 0.0 | 0.0% |
| Sharding/ShardedReadWriteConnectionFactory.cs | 101 | 0.0 | 0.0% |
| Sharding/ShardingServiceCollectionExtensions.cs | 48 | 0.0 | 0.0% |
| Sharding/ReferenceTables/ReferenceTableStoreDapper.cs | 55 | 0.0 | 0.0% |
| Sharding/ReferenceTables/ReferenceTableStoreFactoryDapper.cs | 10 | 0.0 | 0.0% |
| Sharding/Migrations/MigrationServiceCollectionExtensions.cs | 5 | 0.0 | 0.0% |
| ReadWriteSeparation/ReadWriteRoutingPipelineBehavior.cs | 28 | 0.0 | 0.0% |
| ReadWriteSeparation/ReadWriteSeparationHealthCheck.cs | 60 | 0.0 | 0.0% |
| Pagination/CursorPaginationHelper.cs | 213 | 0.0 | 0.0% |
| Modules/SchemaValidatingCommand.cs | 66 | 0.0 | 0.0% |
| Extensions/AuditExtensions.cs | 4 | 0.0 | 0.0% |
| Auditing/AuditLogStoreDapper.cs | 56 | 0.0 | 0.0% |
| Auditing/AuditStoreDapper.cs | 226 | 0.0 | 0.0% |
| Auditing/ReadAuditStoreDapper.cs | 135 | 0.0 | 0.0% |
| Anonymization/TokenMappingStoreDapper.cs | 87 | 0.0 | 0.0% |
| ABAC/PolicyStoreDapper.cs | 188 | 0.0 | 0.0% |
| Health/DapperPostgreSqlDatabaseHealthMonitor.cs | 34 | 1.0 | 2.9% |
| UnitOfWork/UnitOfWorkRepositoryDapper.cs | 389 | 52.0 | 13.4% |
| Repository/SpecificationSqlBuilder.cs | 256 | 55.5 | 21.7% |
| ServiceCollectionExtensions.cs | 87 | 21.0 | 24.1% |
| Repository/FunctionalRepositoryDapper.cs | 289 | 91.0 | 31.5% |
| Tenancy/TenantAwareSpecificationSqlBuilder.cs | 69 | 23.0 | 33.3% |
| Tenancy/TenantAwareFunctionalRepositoryDapper.cs | 402 | 135.0 | 33.6% |
| Scheduling/ScheduledMessage.cs | 19 | 7.5 | 39.5% |
| Sagas/SagaState.cs | 10 | 4.0 | 40.0% |
| Outbox/OutboxMessage.cs | 10 | 4.5 | 45.0% |
| Tenancy/TenancyServiceCollectionExtensions.cs | 62 | 28.5 | 46.0% |
| Scheduling/ScheduledMessageFactory.cs | 11 | 5.5 | 50.0% |
| Sagas/SagaStateFactory.cs | 11 | 5.5 | 50.0% |
| Outbox/OutboxMessageFactory.cs | 8 | 4.0 | 50.0% |
| Health/PostgreSqlHealthCheck.cs | 9 | 4.5 | 50.0% |
| Outbox/OutboxProcessor.cs | 95 | 50.0 | 52.6% |
| Modules/SchemaValidatingConnection.cs | 39 | 25.0 | 64.1% |
| Tenancy/TenantEntityMappingBuilder.cs | 112 | 78.5 | 70.1% |
| Modules/ModuleAwareConnectionFactory.cs | 25 | 19.0 | 76.0% |
| Repository/EntityMappingBuilder.cs | 73 | 55.5 | 76.0% |
| Sagas/SagaStoreDapper.cs | 126 | 96.0 | 76.2% |
| BulkOperations/BulkOperationsDapper.cs | 284 | 241.0 | 84.9% |
| ProcessingActivity/ProcessingActivityRegistryDapper.cs | 110 | 94.0 | 85.5% |
| Inbox/InboxStoreDapper.cs | 111 | 100.0 | 90.1% |
| Scheduling/ScheduledMessageStoreDapper.cs | 126 | 119.0 | 94.4% |
| Outbox/OutboxStoreDapper.cs | 79 | 77.0 | 97.5% |
| Tenancy/DapperTenancyOptions.cs | 5 | 5.0 | 100.0% |
| ReadWriteSeparation/ReadWriteConnectionFactory.cs | 39 | 39.0 | 100.0% |

### Encina.Dapper.SqlServer (27.3%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| TypeHandlers/ShardPrefixedIdTypeHandler.cs | 17 | 0.0 | 0.0% |
| TypeHandlers/SnowflakeIdTypeHandler.cs | 19 | 0.0 | 0.0% |
| TypeHandlers/UlidIdTypeHandler.cs | 19 | 0.0 | 0.0% |
| TypeHandlers/UuidV7IdTypeHandler.cs | 19 | 0.0 | 0.0% |
| Tenancy/TenantConnectionFactory.cs | 32 | 0.0 | 0.0% |
| Temporal/TemporalEntityMappingBuilder.cs | 92 | 0.0 | 0.0% |
| Temporal/TemporalRepositoryDapper.cs | 199 | 0.0 | 0.0% |
| SoftDelete/SoftDeleteEntityMappingBuilder.cs | 180 | 0.0 | 0.0% |
| SoftDelete/SoftDeleteSpecificationSqlBuilder.cs | 70 | 0.0 | 0.0% |
| Sharding/FunctionalShardedRepositoryDapper.cs | 607 | 0.0 | 0.0% |
| Sharding/ShardedReadWriteConnectionFactory.cs | 101 | 0.0 | 0.0% |
| Sharding/ShardingServiceCollectionExtensions.cs | 48 | 0.0 | 0.0% |
| Sharding/ReferenceTables/ReferenceTableStoreDapper.cs | 61 | 0.0 | 0.0% |
| Sharding/ReferenceTables/ReferenceTableStoreFactoryDapper.cs | 10 | 0.0 | 0.0% |
| Sharding/Migrations/MigrationServiceCollectionExtensions.cs | 5 | 0.0 | 0.0% |
| Pagination/CursorPaginationHelper.cs | 211 | 0.0 | 0.0% |
| Modules/SchemaValidatingCommand.cs | 66 | 0.0 | 0.0% |
| Extensions/AuditExtensions.cs | 4 | 0.0 | 0.0% |
| Auditing/AuditLogStoreDapper.cs | 56 | 0.0 | 0.0% |
| Auditing/AuditStoreDapper.cs | 223 | 0.0 | 0.0% |
| Auditing/ReadAuditStoreDapper.cs | 132 | 0.0 | 0.0% |
| Anonymization/TokenMappingStoreDapper.cs | 87 | 0.0 | 0.0% |
| ABAC/PolicyStoreDapper.cs | 194 | 0.0 | 0.0% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 35 | 0.7 | 1.9% |
| Health/DapperSqlServerDatabaseHealthMonitor.cs | 37 | 1.0 | 2.7% |
| UnitOfWork/UnitOfWorkRepositoryDapper.cs | 389 | 75.0 | 19.3% |
| ReadWriteSeparation/ReadWriteSeparationHealthCheck.cs | 60 | 12.5 | 20.8% |
| ServiceCollectionExtensions.cs | 134 | 34.0 | 25.4% |
| Tenancy/TenantAwareSpecificationSqlBuilder.cs | 74 | 23.0 | 31.1% |
| ReadWriteSeparation/ReadWriteRoutingPipelineBehavior.cs | 28 | 9.0 | 32.1% |
| Tenancy/TenantAwareFunctionalRepositoryDapper.cs | 404 | 135.0 | 33.4% |
| Repository/SpecificationSqlBuilder.cs | 258 | 94.0 | 36.4% |
| Sagas/SagaState.cs | 10 | 4.0 | 40.0% |
| UnitOfWork/UnitOfWorkDapper.cs | 78 | 32.5 | 41.7% |
| Repository/FunctionalRepositoryDapper.cs | 289 | 129.0 | 44.6% |
| Scheduling/ScheduledMessage.cs | 19 | 8.5 | 44.7% |
| Tenancy/TenancyServiceCollectionExtensions.cs | 62 | 28.5 | 46.0% |
| Tenancy/TenantEntityMappingBuilder.cs | 112 | 55.5 | 49.5% |
| Scheduling/ScheduledMessageFactory.cs | 11 | 5.5 | 50.0% |
| Sagas/SagaStateFactory.cs | 11 | 5.5 | 50.0% |
| Outbox/OutboxMessage.cs | 10 | 5.0 | 50.0% |
| Outbox/OutboxMessageFactory.cs | 8 | 4.0 | 50.0% |
| Health/SqlServerHealthCheck.cs | 8 | 4.0 | 50.0% |
| Outbox/OutboxProcessor.cs | 95 | 50.0 | 52.6% |
| Modules/SchemaValidatingConnection.cs | 39 | 25.0 | 64.1% |
| Modules/ModuleAwareConnectionFactory.cs | 25 | 19.0 | 76.0% |
| Repository/EntityMappingBuilder.cs | 73 | 55.5 | 76.0% |
| Sagas/SagaStoreDapper.cs | 124 | 95.0 | 76.6% |
| ProcessingActivity/ProcessingActivityRegistryDapper.cs | 71 | 55.0 | 77.5% |
| BulkOperations/BulkOperationsDapper.cs | 238 | 204.0 | 85.7% |
| Inbox/InboxStoreDapper.cs | 110 | 99.0 | 90.0% |
| Scheduling/ScheduledMessageStoreDapper.cs | 124 | 117.0 | 94.3% |
| Outbox/OutboxStoreDapper.cs | 78 | 76.0 | 97.4% |
| Tenancy/DapperTenancyOptions.cs | 5 | 5.0 | 100.0% |
| ReadWriteSeparation/ReadWriteConnectionFactory.cs | 39 | 39.0 | 100.0% |

### Encina.DataAnnotations (46.3%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| DataAnnotationsValidationProvider.cs | 22 | 10.0 | 45.5% |
| ServiceCollectionExtensions.cs | 5 | 2.5 | 50.0% |

### Encina.DistributedLock (63.9%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| LockAcquisitionException.cs | 7 | 3.5 | 50.0% |
| ServiceCollectionExtensions.cs | 6 | 3.0 | 50.0% |
| DistributedLockOptions.cs | 5 | 5.0 | 100.0% |

### Encina.DistributedLock.InMemory (48.0%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 20 | 2.7 | 13.3% |
| ServiceCollectionExtensions.cs | 12 | 6.0 | 50.0% |
| InMemoryDistributedLockProvider.cs | 110 | 59.0 | 53.6% |
| InMemoryLockOptions.cs | 1 | 1.0 | 100.0% |

### Encina.DistributedLock.Redis (39.4%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 20 | 4.7 | 23.3% |
| RedisDistributedLockProvider.cs | 113 | 44.5 | 39.4% |
| ServiceCollectionExtensions.cs | 38 | 15.0 | 39.5% |
| Health/RedisDistributedLockHealthCheck.cs | 24 | 12.0 | 50.0% |
| RedisLockOptions.cs | 1 | 1.0 | 100.0% |

### Encina.DistributedLock.SqlServer (50.9%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| Health/SqlServerDistributedLockHealthCheck.cs | 41 | 4.0 | 9.8% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 20 | 2.7 | 13.3% |
| ServiceCollectionExtensions.cs | 34 | 17.0 | 50.0% |
| SqlServerDistributedLockProvider.cs | 134 | 92.0 | 68.7% |
| SqlServerLockOptions.cs | 2 | 2.0 | 100.0% |

### Encina.EntityFrameworkCore (26.5%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| Temporal/TemporalRepositoryEF.cs | 146 | 0.0 | 0.0% |
| Temporal/TemporalRepositoryPostgreSqlEF.cs | 202 | 0.0 | 0.0% |
| Sharding/FunctionalShardedRepositoryEF.cs | 589 | 0.0 | 0.0% |
| Sharding/ShardedDbContextFactory.cs | 53 | 0.0 | 0.0% |
| Sharding/ShardedReadWriteDbContextFactory.cs | 93 | 0.0 | 0.0% |
| Sharding/ShardingServiceCollectionExtensions.cs | 98 | 0.0 | 0.0% |
| Sharding/ReferenceTables/ReferenceTableStoreEF.cs | 38 | 0.0 | 0.0% |
| Sharding/ReferenceTables/ReferenceTableStoreFactoryEF.cs | 12 | 0.0 | 0.0% |
| Sharding/Migrations/EfCoreMigrationExecutor.cs | 25 | 0.0 | 0.0% |
| Sharding/Migrations/EfCoreMigrationHistoryStore.cs | 127 | 0.0 | 0.0% |
| Sharding/Migrations/EfCoreMigrationOptions.cs | 5 | 0.0 | 0.0% |
| Sharding/Migrations/EfCoreSchemaIntrospector.cs | 63 | 0.0 | 0.0% |
| Sharding/Migrations/MigrationServiceCollectionExtensions.cs | 8 | 0.0 | 0.0% |
| Resilience/ConnectionPoolMonitoringInterceptor.cs | 19 | 0.0 | 0.0% |
| ProcessingActivity/ProcessingActivityEntityConfiguration.cs | 44 | 0.0 | 0.0% |
| ProcessingActivity/ProcessingActivityModelBuilderExtensions.cs | 3 | 0.0 | 0.0% |
| Modules/ModuleExecutionContextBehavior.cs | 24 | 0.0 | 0.0% |
| Modules/ModuleSchemaValidationInterceptor.cs | 52 | 0.0 | 0.0% |
| DomainEvents/DomainEventDispatcherExtensions.cs | 21 | 0.0 | 0.0% |
| Diagnostics/QueryCacheActivitySource.cs | 46 | 0.0 | 0.0% |
| Diagnostics/QueryCacheMetrics.cs | 24 | 0.0 | 0.0% |
| Diagnostics/SoftDeleteActivitySource.cs | 31 | 0.0 | 0.0% |
| Diagnostics/SoftDeleteMetrics.cs | 13 | 0.0 | 0.0% |
| Converters/IdGenerationModelConfigurationExtensions.cs | 10 | 0.0 | 0.0% |
| Converters/ShardPrefixedIdConverter.cs | 4 | 0.0 | 0.0% |
| Converters/SnowflakeIdConverter.cs | 4 | 0.0 | 0.0% |
| Converters/UlidIdConverter.cs | 4 | 0.0 | 0.0% |
| Converters/UuidV7IdConverter.cs | 4 | 0.0 | 0.0% |
| Configuration/EntityConfigurationExtensions.cs | 178 | 0.0 | 0.0% |
| BulkOperations/BulkOperationsEFMySql.cs | 294 | 0.0 | 0.0% |
| BulkOperations/BulkOperationsEFOracle.cs | 303 | 0.0 | 0.0% |
| Auditing/AuditEntryEntity.cs | 18 | 0.0 | 0.0% |
| Auditing/AuditEntryEntityConfiguration.cs | 65 | 0.0 | 0.0% |
| Auditing/AuditLogEntryEntityConfiguration.cs | 36 | 0.0 | 0.0% |
| Auditing/AuditLogStoreEF.cs | 39 | 0.0 | 0.0% |
| Auditing/AuditStoreEF.cs | 172 | 0.0 | 0.0% |
| Auditing/ReadAuditEntryEntityConfiguration.cs | 46 | 0.0 | 0.0% |
| Auditing/ReadAuditStoreEF.cs | 85 | 0.0 | 0.0% |
| Anonymization/TokenMappingStoreEF.cs | 67 | 0.0 | 0.0% |
| ABAC/ABACModelBuilderExtensions.cs | 4 | 0.0 | 0.0% |
| ABAC/PolicyEntityConfiguration.cs | 25 | 0.0 | 0.0% |
| ABAC/PolicySetEntityConfiguration.cs | 25 | 0.0 | 0.0% |
| ABAC/PolicyStoreEF.cs | 186 | 0.0 | 0.0% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 703 | 2.7 | 0.4% |
| ProcessingActivity/ProcessingActivityRegistryEF.cs | 86 | 1.0 | 1.2% |
| Resilience/EfCoreDatabaseHealthMonitor.cs | 12 | 1.0 | 8.3% |
| UnitOfWork/UnitOfWorkRepositoryEF.cs | 163 | 14.0 | 8.6% |
| Health/ReadWriteSeparationHealthCheck.cs | 107 | 16.5 | 15.4% |
| Caching/QueryCacheInterceptor.cs | 217 | 37.5 | 17.3% |
| obj/Release/net10.0/System.Text.RegularExpressions.Generator/System.Text.RegularExpressions.Generator.RegexGenerator/RegexGenerator.g.cs | 744 | 184.7 | 24.8% |
| ServiceCollectionExtensions.cs | 146 | 38.5 | 26.4% |
| Outbox/OutboxProcessor.cs | 94 | 25.0 | 26.6% |
| UnitOfWork/UnitOfWorkEF.cs | 96 | 28.5 | 29.7% |
| ReadWriteSeparation/ReadWriteRoutingPipelineBehavior.cs | 28 | 9.0 | 32.1% |
| SoftDelete/SoftDeleteRepositoryEF.cs | 163 | 55.0 | 33.7% |
| Tenancy/TenantDbContextFactory.cs | 51 | 17.5 | 34.3% |
| TransactionPipelineBehavior.cs | 58 | 21.3 | 36.8% |
| Extensions/QueryableCursorExtensions.cs | 234 | 88.5 | 37.8% |
| Repository/SpecificationEvaluator.cs | 88 | 35.0 | 39.8% |
| Auditing/AuditInterceptor.cs | 187 | 75.5 | 40.4% |
| Repository/FunctionalRepositoryEF.cs | 278 | 122.0 | 43.9% |
| DomainEvents/DomainEventDispatcherInterceptor.cs | 135 | 59.5 | 44.1% |
| ReadWriteSeparation/ReadWriteDbContextFactory.cs | 32 | 15.0 | 46.9% |
| Caching/SqlTableExtractor.cs | 51 | 24.5 | 48.0% |
| SoftDelete/SoftDeleteInterceptor.cs | 53 | 26.0 | 49.1% |
| Caching/CachedDataReader.cs | 175 | 86.0 | 49.1% |
| TransactionAttribute.cs | 1 | 0.5 | 50.0% |
| Scheduling/ScheduledMessage.cs | 17 | 8.5 | 50.0% |
| Scheduling/ScheduledMessageFactory.cs | 11 | 5.5 | 50.0% |
| Sagas/Saga.cs | 33 | 16.5 | 50.0% |
| Sagas/SagaState.cs | 14 | 7.0 | 50.0% |
| Sagas/SagaStateFactory.cs | 11 | 5.5 | 50.0% |
| Outbox/OutboxMessage.cs | 10 | 5.0 | 50.0% |
| Outbox/OutboxMessageFactory.cs | 8 | 4.0 | 50.0% |
| Inbox/InboxMessage.cs | 12 | 6.0 | 50.0% |
| Inbox/InboxMessageFactory.cs | 14 | 7.0 | 50.0% |
| Health/EntityFrameworkCoreHealthCheck.cs | 18 | 9.0 | 50.0% |
| Auditing/AuditLogEntryEntity.cs | 9 | 4.5 | 50.0% |
| Extensions/ImmutableUpdateExtensions.cs | 26 | 14.0 | 53.9% |
| Extensions/DbContextKeyExtensions.cs | 36 | 20.5 | 56.9% |
| Extensions/QueryablePagedExtensions.cs | 34 | 19.5 | 57.4% |
| Caching/DefaultQueryCacheKeyGenerator.cs | 43 | 26.0 | 60.5% |
| Extensions/QueryCachingExtensions.cs | 19 | 12.0 | 63.2% |
| Tenancy/TenantDbContext.cs | 70 | 48.0 | 68.6% |
| BulkOperations/BulkOperationsEF.cs | 21 | 16.0 | 76.2% |
| BulkOperations/BulkOperationsEFPostgreSql.cs | 284 | 226.0 | 79.6% |
| BulkOperations/BulkOperationsEFSqlite.cs | 305 | 245.0 | 80.3% |
| BulkOperations/BulkOperationsEFSqlServer.cs | 304 | 248.0 | 81.6% |
| Scheduling/ScheduledMessageStoreEF.cs | 90 | 74.0 | 82.2% |
| Inbox/InboxStoreEF.cs | 95 | 79.0 | 83.2% |
| Sagas/SagaStoreEF.cs | 67 | 62.0 | 92.5% |
| Outbox/OutboxStoreEF.cs | 61 | 57.0 | 93.4% |
| Caching/CachedQueryResult.cs | 9 | 8.5 | 94.4% |
| Tenancy/EfCoreTenancyOptions.cs | 4 | 4.0 | 100.0% |
| SoftDelete/SoftDeleteInterceptorOptions.cs | 4 | 4.0 | 100.0% |
| Scheduling/ScheduledMessageConfiguration.cs | 42 | 42.0 | 100.0% |
| Sagas/SagaStateConfiguration.cs | 38 | 38.0 | 100.0% |
| Outbox/OutboxMessageConfiguration.cs | 23 | 23.0 | 100.0% |
| Inbox/InboxMessageConfiguration.cs | 34 | 34.0 | 100.0% |
| DomainEvents/DomainEventDispatcherOptions.cs | 5 | 5.0 | 100.0% |
| Caching/QueryCacheOptions.cs | 7 | 7.0 | 100.0% |
| Auditing/AuditInterceptorOptions.cs | 7 | 7.0 | 100.0% |

### Encina.Extensions.Resilience (32.3%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| obj/Release/net10.0/Microsoft.Gen.Logging/Microsoft.Gen.Logging.LoggingGenerator/Logging.g.cs | 146 | 16.0 | 11.0% |
| Behaviors/StandardResiliencePipelineBehavior.cs | 46 | 18.0 | 39.1% |
| ServiceCollectionExtensions.cs | 49 | 19.5 | 39.8% |
| Configuration/StandardResilienceOptions.cs | 36 | 36.0 | 100.0% |

### Encina.GraphQL (58.5%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| ServiceCollectionExtensions.cs | 16 | 0.0 | 0.0% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 40 | 7.0 | 17.5% |
| GraphQLMediatorBridge.cs | 68 | 29.0 | 42.6% |
| EncinaGraphQLOptions.cs | 8 | 8.0 | 100.0% |
| Pagination/Connection.cs | 27 | 27.0 | 100.0% |
| Pagination/ConnectionExtensions.cs | 40 | 40.0 | 100.0% |
| Pagination/Edge.cs | 2 | 2.0 | 100.0% |
| Pagination/RelayPageInfo.cs | 11 | 11.0 | 100.0% |

### Encina.gRPC (39.3%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| GrpcMediatorService.cs | 155 | 54.0 | 34.8% |
| Health/GrpcHealthCheck.cs | 12 | 5.0 | 41.7% |
| CachingTypeResolver.cs | 7 | 3.5 | 50.0% |
| ServiceCollectionExtensions.cs | 16 | 8.0 | 50.0% |
| EncinaGrpcOptions.cs | 7 | 7.0 | 100.0% |

### Encina.GuardClauses (45.9%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| obj/Release/net10.0/System.Text.RegularExpressions.Generator/System.Text.RegularExpressions.Generator.RegexGenerator/RegexGenerator.g.cs | 71 | 25.8 | 36.3% |
| Guards.cs | 164 | 82.0 | 50.0% |

### Encina.Hangfire (48.1%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 35 | 15.0 | 42.9% |
| Health/HangfireHealthCheck.cs | 25 | 12.0 | 48.0% |
| HangfireNotificationJobAdapter.cs | 18 | 9.0 | 50.0% |
| HangfireRequestJobAdapter.cs | 20 | 10.0 | 50.0% |
| ServiceCollectionExtensions.cs | 36 | 18.0 | 50.0% |
| EncinaHangfireOptions.cs | 1 | 1.0 | 100.0% |

### Encina.IdGeneration (49.7%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| Diagnostics/IdGenerationMetrics.cs | 39 | 0.0 | 0.0% |
| Health/IdGeneratorHealthCheck.cs | 23 | 10.0 | 43.5% |
| Types/ShardPrefixedId.cs | 60 | 27.0 | 45.0% |
| Types/UuidV7Id.cs | 50 | 22.5 | 45.0% |
| Generators/SnowflakeIdGenerator.cs | 52 | 23.5 | 45.2% |
| Types/SnowflakeId.cs | 28 | 13.5 | 48.2% |
| Types/UlidId.cs | 127 | 61.5 | 48.4% |
| Generators/UlidIdGenerator.cs | 6 | 3.0 | 50.0% |
| Generators/UuidV7IdGenerator.cs | 6 | 3.0 | 50.0% |
| Generators/ShardPrefixedIdGenerator.cs | 35 | 18.0 | 51.4% |
| Extensions/ServiceCollectionExtensions.cs | 17 | 9.5 | 55.9% |
| Diagnostics/IdGenerationActivitySource.cs | 35 | 27.0 | 77.1% |
| Health/IdGeneratorHealthCheckOptions.cs | 1 | 1.0 | 100.0% |
| Configuration/IdGenerationOptions.cs | 16 | 16.0 | 100.0% |
| Configuration/ShardPrefixedOptions.cs | 2 | 2.0 | 100.0% |
| Configuration/SnowflakeOptions.cs | 19 | 19.0 | 100.0% |

### Encina.Kafka (46.0%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| ServiceCollectionExtensions.cs | 38 | 12.0 | 31.6% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 30 | 13.5 | 45.0% |
| KafkaMessagePublisher.cs | 101 | 46.5 | 46.0% |
| Health/KafkaHealthCheck.cs | 9 | 4.5 | 50.0% |
| EncinaKafkaOptions.cs | 10 | 10.0 | 100.0% |

### Encina.Marten (16.6%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| ConfigureMartenEventMetadata.cs | 41 | 0.0 | 0.0% |
| Versioning/ConfigureMartenEventVersioning.cs | 22 | 0.0 | 0.0% |
| Versioning/EventUpcasterBase.cs | 5 | 0.0 | 0.0% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 1663 | 36.7 | 2.2% |
| MartenEventMetadataQuery.cs | 196 | 18.5 | 9.4% |
| Projections/InlineProjectionDispatcher.cs | 112 | 14.0 | 12.5% |
| Projections/MartenProjectionManager.cs | 298 | 53.5 | 17.9% |
| EventPublishingPipelineBehavior.cs | 40 | 7.7 | 19.2% |
| Snapshots/SnapshotAwareAggregateRepository.cs | 259 | 67.3 | 26.0% |
| Projections/MartenReadModelRepository.cs | 150 | 40.3 | 26.9% |
| Snapshots/MartenSnapshotStore.cs | 133 | 37.5 | 28.2% |
| MartenAggregateRepository.cs | 145 | 44.3 | 30.6% |
| Versioning/EventVersioningOptions.cs | 34 | 12.0 | 35.3% |
| Health/MartenHealthCheck.cs | 8 | 3.0 | 37.5% |
| Snapshots/SnapshotOptions.cs | 20 | 8.0 | 40.0% |
| Versioning/EventUpcasterRegistry.cs | 57 | 28.0 | 49.1% |
| Projections/ProjectionRegistry.cs | 85 | 42.0 | 49.4% |
| Versioning/LambdaEventUpcaster.cs | 12 | 6.0 | 50.0% |
| Snapshots/SnapshotEnvelope.cs | 21 | 10.5 | 50.0% |
| Projections/ProjectionContext.cs | 21 | 10.5 | 50.0% |
| Projections/ProjectionStatus.cs | 10 | 5.0 | 50.0% |
| ServiceCollectionExtensions.cs | 54 | 29.0 | 53.7% |
| EventMetadataEnrichmentService.cs | 51 | 29.5 | 57.8% |
| EventMetadataOptions.cs | 28 | 27.0 | 96.4% |
| EncinaMartenOptions.cs | 9 | 9.0 | 100.0% |
| Projections/ProjectionOptions.cs | 5 | 5.0 | 100.0% |
| Instrumentation/MartenActivityEnricher.cs | 41 | 41.0 | 100.0% |

### Encina.Marten.GDPR (24.6%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| ConfigureMartenCryptoShredding.cs | 17 | 0.0 | 0.0% |
| CryptoShreddingAutoRegistrationDescriptor.cs | 1 | 0.0 | 0.0% |
| CryptoShreddingAutoRegistrationHostedService.cs | 63 | 0.0 | 0.0% |
| Events/PiiEncryptionFailedEvent.cs | 5 | 0.0 | 0.0% |
| Events/SubjectForgottenEvent.cs | 5 | 0.0 | 0.0% |
| Events/SubjectKeyRotatedEvent.cs | 5 | 0.0 | 0.0% |
| KeyStore/PostgreSqlSubjectKeyProvider.cs | 210 | 0.0 | 0.0% |
| KeyStore/SubjectForgottenMarker.cs | 4 | 0.0 | 0.0% |
| KeyStore/SubjectKeyDocument.cs | 7 | 0.0 | 0.0% |
| Model/CryptoShreddedFieldMetadata.cs | 4 | 0.0 | 0.0% |
| Model/SubjectKeyInfo.cs | 6 | 0.0 | 0.0% |
| Serialization/CryptoShredderSerializerFactory.cs | 14 | 0.0 | 0.0% |
| ServiceCollectionExtensions.cs | 27 | 0.0 | 0.0% |
| Health/CryptoShreddingHealthCheck.cs | 43 | 0.0 | 0.0% |
| Locator/MartenEventPersonalDataLocator.cs | 57 | 4.5 | 7.9% |
| Serialization/CryptoShredderSerializer.cs | 302 | 31.3 | 10.4% |
| CryptoShreddingOptionsValidator.cs | 13 | 4.3 | 33.3% |
| Diagnostics/CryptoShreddingDiagnostics.cs | 70 | 25.0 | 35.7% |
| KeyStore/InMemorySubjectKeyProvider.cs | 186 | 84.5 | 45.4% |
| Serialization/EncryptedFieldJsonConverter.cs | 50 | 23.0 | 46.0% |
| Metadata/CryptoShreddedFieldInfo.cs | 16 | 7.5 | 46.9% |
| Metadata/CryptoShreddedPropertyCache.cs | 42 | 20.0 | 47.6% |
| Attributes/CryptoShreddedAttribute.cs | 1 | 0.5 | 50.0% |
| DefaultForgottenSubjectHandler.cs | 11 | 5.5 | 50.0% |
| Model/CryptoShreddingResult.cs | 4 | 2.0 | 50.0% |
| Model/KeyRotationResult.cs | 6 | 3.0 | 50.0% |
| Model/SubjectEncryptionInfo.cs | 6 | 3.0 | 50.0% |
| Erasure/CryptoShredErasureStrategy.cs | 24 | 16.5 | 68.8% |
| CryptoShreddingOptions.cs | 7 | 5.0 | 71.4% |
| CryptoShreddingErrors.cs | 81 | 81.0 | 100.0% |

### Encina.Messaging (40.6%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| SoftDelete/SoftDeleteFilterContext.cs | 3 | 0.0 | 0.0% |
| Sagas/SagaNotFoundDispatcher.cs | 28 | 0.0 | 0.0% |
| Recoverability/RecoverabilityServiceCollectionExtensions.cs | 16 | 0.0 | 0.0% |
| Diagnostics/InboxActivitySource.cs | 38 | 0.0 | 0.0% |
| Diagnostics/MessagingStoreMetrics.cs | 80 | 0.0 | 0.0% |
| Diagnostics/OutboxActivitySource.cs | 38 | 0.0 | 0.0% |
| Diagnostics/SagaActivitySource.cs | 43 | 0.0 | 0.0% |
| Diagnostics/SchedulingActivitySource.cs | 39 | 0.0 | 0.0% |
| DeadLetter/DeadLetterServiceCollectionExtensions.cs | 16 | 0.0 | 0.0% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 236 | 2.8 | 1.2% |
| Recoverability/DelayedRetryProcessor.cs | 172 | 24.0 | 13.9% |
| MessagingServiceCollectionExtensions.cs | 94 | 17.5 | 18.6% |
| SoftDelete/SoftDeleteQueryFilterBehavior.cs | 19 | 4.5 | 23.7% |
| DeadLetter/DeadLetterManager.cs | 150 | 38.0 | 25.3% |
| DeadLetter/IDeadLetterMessageFactory.cs | 14 | 4.5 | 32.1% |
| Serialization/JsonMessageSerializer.cs | 14 | 4.7 | 33.3% |
| Recoverability/RecoverabilityPipelineBehavior.cs | 176 | 58.7 | 33.3% |
| Inbox/InboxPipelineBehavior.cs | 27 | 9.0 | 33.3% |
| TransactionPipelineBehavior.cs | 22 | 8.7 | 39.4% |
| DeadLetter/DeadLetterOrchestrator.cs | 189 | 74.5 | 39.4% |
| Health/InboxHealthCheck.cs | 19 | 7.5 | 39.5% |
| Health/DatabaseHealthCheck.cs | 34 | 13.5 | 39.7% |
| ScatterGather/BuiltScatterGatherDefinition.cs | 45 | 19.5 | 43.3% |
| ScatterGather/ScatterGatherRunner.cs | 268 | 116.5 | 43.5% |
| Health/OutboxHealthCheck.cs | 34 | 15.0 | 44.1% |
| Sagas/SagaOrchestrator.cs | 212 | 98.0 | 46.2% |
| Outbox/OutboxOrchestrator.cs | 77 | 36.0 | 46.8% |
| RoutingSlip/RoutingSlipStepBuilder.cs | 46 | 22.0 | 47.8% |
| ContentRouter/ContentRouterBuilder.cs | 79 | 38.0 | 48.1% |
| Health/TierTransitionHealthCheck.cs | 53 | 25.5 | 48.1% |
| ContentRouter/ContentRouter.cs | 178 | 86.0 | 48.3% |
| Sagas/LowCeremony/SagaRunner.cs | 80 | 39.0 | 48.8% |
| RoutingSlip/RoutingSlipContext.cs | 40 | 19.5 | 48.8% |
| Scheduling/SchedulerOrchestrator.cs | 156 | 76.5 | 49.0% |
| RoutingSlip/RoutingSlipRunner.cs | 107 | 52.5 | 49.1% |
| ScatterGather/ScatterGatherBuilder.cs | 151 | 74.5 | 49.3% |
| SqlIdentifierValidator.cs | 16 | 8.0 | 50.0% |
| ScatterGather/ScatterDefinition.cs | 16 | 8.0 | 50.0% |
| ScatterGather/ScatterExecutionResult.cs | 21 | 10.5 | 50.0% |
| ScatterGather/ScatterGatherResult.cs | 33 | 16.5 | 50.0% |
| Sagas/SagaNotFoundContext.cs | 29 | 14.5 | 50.0% |
| Sagas/LowCeremony/SagaDefinition.cs | 42 | 21.0 | 50.0% |
| Sagas/LowCeremony/SagaStepBuilder.cs | 28 | 14.0 | 50.0% |
| RoutingSlip/RoutingSlipActivityEntry.cs | 19 | 9.5 | 50.0% |
| RoutingSlip/RoutingSlipBuilder.cs | 41 | 20.5 | 50.0% |
| RoutingSlip/RoutingSlipResult.cs | 23 | 11.5 | 50.0% |
| RoutingSlip/RoutingSlipStepDefinition.cs | 16 | 8.0 | 50.0% |
| Recoverability/DelayedRetryScheduler.cs | 72 | 36.0 | 50.0% |
| Recoverability/FailedMessage.cs | 20 | 10.0 | 50.0% |
| Recoverability/RecoverabilityContext.cs | 61 | 30.5 | 50.0% |
| ReadWriteSeparation/DatabaseRoutingContext.cs | 13 | 6.5 | 50.0% |
| ReadWriteSeparation/DatabaseRoutingScope.cs | 19 | 9.5 | 50.0% |
| ReadWriteSeparation/ForceWriteDatabaseAttribute.cs | 3 | 1.5 | 50.0% |
| ReadWriteSeparation/LeastConnectionsReplicaSelector.cs | 38 | 19.0 | 50.0% |
| ReadWriteSeparation/RandomReplicaSelector.cs | 8 | 4.0 | 50.0% |
| ReadWriteSeparation/ReadWriteConnectionSelector.cs | 30 | 15.0 | 50.0% |
| ReadWriteSeparation/ReplicaSelectorFactory.cs | 31 | 15.5 | 50.0% |
| ReadWriteSeparation/RoundRobinReplicaSelector.cs | 10 | 5.0 | 50.0% |
| Outbox/OutboxPostProcessor.cs | 56 | 28.0 | 50.0% |
| Health/DeadLetterHealthCheck.cs | 54 | 27.0 | 50.0% |
| Health/HealthCheckResult.cs | 12 | 6.0 | 50.0% |
| Health/SagaHealthCheck.cs | 40 | 20.0 | 50.0% |
| Health/SchedulingHealthCheck.cs | 42 | 21.0 | 50.0% |
| Health/ShardCreationHealthCheck.cs | 47 | 23.5 | 50.0% |
| DeadLetter/DeadLetterCleanupProcessor.cs | 37 | 18.5 | 50.0% |
| DeadLetter/DeadLetterFilter.cs | 22 | 11.0 | 50.0% |
| ContentRouter/ContentRouterResult.cs | 30 | 15.0 | 50.0% |
| ContentRouter/RouteDefinition.cs | 24 | 12.0 | 50.0% |
| Health/DatabaseHealthMonitorBase.cs | 65 | 33.0 | 50.8% |
| Health/DatabasePoolHealthCheck.cs | 46 | 24.0 | 52.2% |
| Health/ReferenceTableHealthCheck.cs | 53 | 31.0 | 58.5% |
| Services/ConnectionWarmupHostedService.cs | 28 | 17.0 | 60.7% |
| Health/EncinaHealthCheck.cs | 23 | 15.0 | 65.2% |
| Temporal/TemporalTableOptions.cs | 6 | 4.0 | 66.7% |
| Auditing/AuditingOptions.cs | 6 | 4.0 | 66.7% |
| SoftDelete/SoftDeleteOptions.cs | 8 | 6.0 | 75.0% |
| DomainEvents/DomainEventsOptions.cs | 4 | 3.0 | 75.0% |
| obj/Release/net10.0/System.Text.RegularExpressions.Generator/System.Text.RegularExpressions.Generator.RegexGenerator/RegexGenerator.g.cs | 95 | 72.2 | 76.0% |
| MessagingConfiguration.cs | 47 | 40.0 | 85.1% |
| Tenancy/TenancyMessagingOptions.cs | 4 | 4.0 | 100.0% |
| Scheduling/SchedulingOptions.cs | 6 | 6.0 | 100.0% |
| ScatterGather/ScatterGatherOptions.cs | 7 | 7.0 | 100.0% |
| RoutingSlip/RoutingSlipOptions.cs | 5 | 5.0 | 100.0% |
| Recoverability/RecoverabilityOptions.cs | 16 | 16.0 | 100.0% |
| ReadWriteSeparation/ReadWriteSeparationOptions.cs | 5 | 5.0 | 100.0% |
| Outbox/OutboxOptions.cs | 5 | 5.0 | 100.0% |
| Health/ProviderHealthCheckOptions.cs | 5 | 5.0 | 100.0% |
| DeadLetter/DeadLetterOptions.cs | 9 | 9.0 | 100.0% |
| ContentRouter/ContentRouterOptions.cs | 6 | 6.0 | 100.0% |
| Choreography/ChoreographyOptions.cs | 5 | 5.0 | 100.0% |
| Caching/QueryCacheMessagingOptions.cs | 4 | 4.0 | 100.0% |

### Encina.Messaging.Encryption (42.9%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| Diagnostics/MessageEncryptionDiagnostics.cs | 63 | 2.0 | 3.2% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 105 | 5.2 | 5.0% |
| ServiceCollectionExtensions.cs | 47 | 14.0 | 29.8% |
| Serialization/EncryptingMessageSerializer.cs | 130 | 44.0 | 33.9% |
| Model/MessageEncryptionContext.cs | 5 | 2.0 | 40.0% |
| EncryptedMessageAttributeCache.cs | 9 | 4.5 | 50.0% |
| Attributes/EncryptedMessageAttribute.cs | 3 | 1.5 | 50.0% |
| EncryptedPayloadFormatter.cs | 36 | 18.5 | 51.4% |
| Health/MessageEncryptionHealthCheck.cs | 68 | 35.5 | 52.2% |
| DefaultMessageEncryptionProvider.cs | 52 | 31.5 | 60.6% |
| DefaultTenantKeyResolver.cs | 6 | 5.5 | 91.7% |
| MessageEncryptionErrors.cs | 91 | 91.0 | 100.0% |
| MessageEncryptionOptions.cs | 9 | 9.0 | 100.0% |
| Model/EncryptedPayload.cs | 6 | 6.0 | 100.0% |

### Encina.Messaging.Encryption.AwsKms (43.3%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 35 | 2.8 | 8.0% |
| ServiceCollectionExtensions.cs | 22 | 9.5 | 43.2% |
| AwsKmsKeyProvider.cs | 62 | 37.0 | 59.7% |
| AwsKmsOptions.cs | 4 | 4.0 | 100.0% |

### Encina.Messaging.Encryption.AzureKeyVault (26.3%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 35 | 0.0 | 0.0% |
| AzureKeyVaultKeyProvider.cs | 69 | 17.0 | 24.6% |
| ServiceCollectionExtensions.cs | 21 | 11.5 | 54.8% |
| AzureKeyVaultOptions.cs | 6 | 6.0 | 100.0% |

### Encina.Messaging.Encryption.DataProtection (51.0%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 20 | 3.2 | 16.0% |
| ServiceCollectionExtensions.cs | 7 | 4.0 | 57.1% |
| DataProtectionMessageEncryptionProvider.cs | 41 | 27.0 | 65.8% |
| DataProtectionEncryptionOptions.cs | 1 | 1.0 | 100.0% |

### Encina.MiniValidator (50.0%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| MiniValidationProvider.cs | 9 | 4.5 | 50.0% |
| ServiceCollectionExtensions.cs | 5 | 2.5 | 50.0% |

### Encina.MongoDB (20.2%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| MongoDbIndexCreator.cs | 155 | 0.0 | 0.0% |
| SoftDelete/SoftDeletableFunctionalRepositoryMongoDB.cs | 381 | 0.0 | 0.0% |
| SoftDelete/SoftDeleteEntityMappingBuilder.cs | 94 | 0.0 | 0.0% |
| SoftDelete/SoftDeleteSpecificationFilterBuilder.cs | 39 | 0.0 | 0.0% |
| Sharding/FunctionalShardedRepositoryMongoDB.cs | 832 | 0.0 | 0.0% |
| Sharding/MongoDbShardingOptions.cs | 14 | 0.0 | 0.0% |
| Sharding/ShardedMongoCollectionFactory.cs | 103 | 0.0 | 0.0% |
| Sharding/ShardedMongoDbDatabaseHealthMonitor.cs | 89 | 0.0 | 0.0% |
| Sharding/ShardedReadWriteMongoCollectionFactory.cs | 137 | 0.0 | 0.0% |
| Sharding/ShardingServiceCollectionExtensions.cs | 123 | 0.0 | 0.0% |
| Sharding/ReferenceTables/ReferenceTableStoreFactoryMongoDB.cs | 12 | 0.0 | 0.0% |
| Sharding/ReferenceTables/ReferenceTableStoreMongoDB.cs | 37 | 0.0 | 0.0% |
| Sharding/Migrations/MigrationServiceCollectionExtensions.cs | 5 | 0.0 | 0.0% |
| Sharding/Migrations/MongoMigrationExecutor.cs | 25 | 0.0 | 0.0% |
| Sharding/Migrations/MongoMigrationHistoryStore.cs | 128 | 0.0 | 0.0% |
| Sharding/Migrations/MongoSchemaIntrospector.cs | 70 | 0.0 | 0.0% |
| Serializers/IdGenerationSerializerRegistration.cs | 11 | 0.0 | 0.0% |
| Serializers/ShardPrefixedIdSerializer.cs | 12 | 0.0 | 0.0% |
| Serializers/SnowflakeIdSerializer.cs | 14 | 0.0 | 0.0% |
| Serializers/UlidIdSerializer.cs | 13 | 0.0 | 0.0% |
| Serializers/UuidV7IdSerializer.cs | 13 | 0.0 | 0.0% |
| Repository/SpecificationEvaluatorMongoDB.cs | 57 | 0.0 | 0.0% |
| ProcessingActivity/ProcessingActivityDocument.cs | 49 | 0.0 | 0.0% |
| ProcessingActivity/ProcessingActivityRegistryMongoDB.cs | 83 | 0.0 | 0.0% |
| Modules/ModuleAwareMongoCollectionFactory.cs | 53 | 0.0 | 0.0% |
| Extensions/AuditExtensions.cs | 4 | 0.0 | 0.0% |
| Extensions/CursorPaginationExtensions.cs | 207 | 0.0 | 0.0% |
| Auditing/AuditEntryDocument.cs | 76 | 0.0 | 0.0% |
| Auditing/AuditLogStoreMongoDB.cs | 31 | 0.0 | 0.0% |
| Auditing/AuditStoreMongoDB.cs | 135 | 0.0 | 0.0% |
| Auditing/ReadAuditEntryDocument.cs | 55 | 0.0 | 0.0% |
| Auditing/ReadAuditStoreMongoDB.cs | 102 | 0.0 | 0.0% |
| Anonymization/TokenMappingDocument.cs | 30 | 0.0 | 0.0% |
| Anonymization/TokenMappingStoreMongoDB.cs | 59 | 0.0 | 0.0% |
| Aggregation/AggregationPipelineBuilder.cs | 66 | 0.0 | 0.0% |
| ABAC/ABACBsonClassMapRegistration.cs | 46 | 0.0 | 0.0% |
| ABAC/PolicyDocument.cs | 6 | 0.0 | 0.0% |
| ABAC/PolicySetDocument.cs | 6 | 0.0 | 0.0% |
| ABAC/PolicyStoreMongo.cs | 182 | 0.0 | 0.0% |
| Health/MongoDbDatabaseHealthMonitor.cs | 58 | 2.0 | 3.5% |
| UnitOfWork/UnitOfWorkRepositoryMongoDB.cs | 320 | 37.0 | 11.6% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 499 | 58.3 | 11.7% |
| ReadWriteSeparation/ReadWriteMongoHealthCheck.cs | 70 | 8.5 | 12.1% |
| ServiceCollectionExtensions.cs | 230 | 34.0 | 14.8% |
| Inbox/InboxMessage.cs | 11 | 2.5 | 22.7% |
| ReadWriteSeparation/ReadWriteMongoCollectionFactory.cs | 68 | 17.0 | 25.0% |
| ReadWriteSeparation/ReadWriteRoutingPipelineBehavior.cs | 28 | 9.0 | 32.1% |
| Repository/FunctionalRepositoryMongoDB.cs | 291 | 110.0 | 37.8% |
| Tenancy/TenantAwareFunctionalRepositoryMongoDB.cs | 370 | 140.0 | 37.8% |
| Tenancy/TenantAwareMongoCollectionFactory.cs | 63 | 24.0 | 38.1% |
| Scheduling/ScheduledMessage.cs | 15 | 6.0 | 40.0% |
| Sagas/SagaState.cs | 10 | 4.0 | 40.0% |
| Outbox/OutboxMessage.cs | 10 | 4.0 | 40.0% |
| UnitOfWork/UnitOfWorkMongoDB.cs | 95 | 39.0 | 41.0% |
| Modules/MongoDbModuleIsolationOptions.cs | 12 | 5.0 | 41.7% |
| Tenancy/TenancyServiceCollectionExtensions.cs | 133 | 59.0 | 44.4% |
| Tenancy/TenantEntityMappingBuilder.cs | 93 | 45.0 | 48.4% |
| BulkOperations/BulkOperationsMongoDB.cs | 185 | 91.0 | 49.2% |
| Repository/SpecificationFilterBuilder.cs | 219 | 109.0 | 49.8% |
| Tenancy/TenantAwareSpecificationFilterBuilder.cs | 44 | 22.0 | 50.0% |
| Scheduling/ScheduledMessageFactory.cs | 11 | 5.5 | 50.0% |
| Sagas/SagaStateFactory.cs | 11 | 5.5 | 50.0% |
| Outbox/OutboxMessageFactory.cs | 8 | 4.0 | 50.0% |
| Inbox/InboxMessageFactory.cs | 8 | 4.0 | 50.0% |
| Health/MongoDbHealthCheck.cs | 16 | 8.0 | 50.0% |
| Auditing/AuditLogDocument.cs | 32 | 16.0 | 50.0% |
| Scheduling/ScheduledMessageStoreMongoDB.cs | 139 | 134.0 | 96.4% |
| Outbox/OutboxStoreMongoDB.cs | 96 | 93.0 | 96.9% |
| Inbox/InboxStoreMongoDB.cs | 133 | 129.0 | 97.0% |
| Sagas/SagaStoreMongoDB.cs | 117 | 115.0 | 98.3% |
| EncinaMongoDbOptions.cs | 50 | 50.0 | 100.0% |
| Tenancy/MongoDbTenancyOptions.cs | 12 | 12.0 | 100.0% |
| Repository/MongoDbRepositoryOptions.cs | 11 | 11.0 | 100.0% |
| ReadWriteSeparation/MongoReadWriteSeparationOptions.cs | 5 | 5.0 | 100.0% |

### Encina.MQTT (27.9%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| MQTTMessagePublisher.cs | 146 | 24.5 | 16.8% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 30 | 10.5 | 35.0% |
| ServiceCollectionExtensions.cs | 49 | 17.5 | 35.7% |
| Health/MQTTHealthCheck.cs | 7 | 3.5 | 50.0% |
| EncinaMQTTOptions.cs | 12 | 12.0 | 100.0% |

### Encina.NATS (39.8%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| NATSMessagePublisher.cs | 89 | 29.0 | 32.6% |
| ServiceCollectionExtensions.cs | 33 | 13.0 | 39.4% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 45 | 18.0 | 40.0% |
| Health/NATSHealthCheck.cs | 10 | 5.0 | 50.0% |
| EncinaNATSOptions.cs | 9 | 9.0 | 100.0% |

### Encina.OpenTelemetry (38.1%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| UnitOfWork/InstrumentedUnitOfWork.cs | 33 | 0.0 | 0.0% |
| UnitOfWork/UnitOfWorkMetricsInitializer.cs | 5 | 0.0 | 0.0% |
| Tenancy/TenancyMetricsInitializer.cs | 5 | 0.0 | 0.0% |
| SoftDelete/SoftDeleteMetricsInitializer.cs | 5 | 0.0 | 0.0% |
| Sharding/ShadowShardingActivityEnricher.cs | 14 | 0.0 | 0.0% |
| Repository/InstrumentedFunctionalRepository.cs | 148 | 0.0 | 0.0% |
| Repository/RepositoryMetricsInitializer.cs | 5 | 0.0 | 0.0% |
| QueryCache/InstrumentedCacheProvider.cs | 122 | 0.0 | 0.0% |
| QueryCache/QueryCacheMetricsInitializer.cs | 5 | 0.0 | 0.0% |
| MessagingStores/InstrumentedInboxStore.cs | 104 | 0.0 | 0.0% |
| MessagingStores/InstrumentedOutboxStore.cs | 66 | 0.0 | 0.0% |
| MessagingStores/InstrumentedSagaStore.cs | 89 | 0.0 | 0.0% |
| MessagingStores/InstrumentedScheduledMessageStore.cs | 91 | 0.0 | 0.0% |
| IdGeneration/IdGenerationMetricsInitializer.cs | 5 | 0.0 | 0.0% |
| BulkOperations/BulkOperationsMetricsInitializer.cs | 5 | 0.0 | 0.0% |
| BulkOperations/InstrumentedBulkOperations.cs | 40 | 0.0 | 0.0% |
| Audit/AuditMetricsInitializer.cs | 5 | 0.0 | 0.0% |
| Audit/InstrumentedAuditStore.cs | 111 | 0.0 | 0.0% |
| Sharding/TimeBasedShardingMetricsInitializer.cs | 39 | 1.5 | 3.9% |
| Migrations/MigrationMetricsInitializer.cs | 15 | 1.5 | 10.0% |
| ReferenceTable/ReferenceTableMetricsInitializer.cs | 12 | 1.5 | 12.5% |
| Cdc/ShardedCdcMetricsInitializer.cs | 12 | 1.5 | 12.5% |
| DatabasePoolMetricsInitializer.cs | 10 | 1.5 | 15.0% |
| Sharding/ColocationMetricsInitializer.cs | 10 | 1.5 | 15.0% |
| Modules/ModuleMetricsInitializer.cs | 9 | 1.5 | 16.7% |
| MessagingStores/MessagingStoreMetricsInitializer.cs | 9 | 1.5 | 16.7% |
| Migrations/MigrationActivitySource.cs | 33 | 7.0 | 21.2% |
| Behaviors/MessagingEnricherPipelineBehavior.cs | 26 | 8.7 | 33.3% |
| ServiceCollectionExtensions.cs | 101 | 40.0 | 39.6% |
| Resharding/ReshardingMetricsInitializer.cs | 17 | 7.5 | 44.1% |
| Resharding/ReshardingHealthCheck.cs | 108 | 57.0 | 52.8% |
| Migrations/SchemaDriftHealthCheck.cs | 80 | 44.0 | 55.0% |
| Resharding/ReshardingMetricsCallbacks.cs | 14 | 11.0 | 78.6% |
| Sharding/TimeBasedShardingMetricsCallbacks.cs | 10 | 8.0 | 80.0% |
| ReferenceTable/ReferenceTableMetricsCallbacks.cs | 10 | 8.0 | 80.0% |
| Cdc/ShardedCdcMetricsCallbacks.cs | 10 | 8.0 | 80.0% |
| Sharding/TimeBasedShardingMetrics.cs | 59 | 49.0 | 83.0% |
| Migrations/MigrationMetricsCallbacks.cs | 6 | 5.0 | 83.3% |
| Enrichers/MessagingActivityEnricher.cs | 57 | 48.0 | 84.2% |
| ReferenceTable/ReferenceTableMetrics.cs | 51 | 46.0 | 90.2% |
| Resharding/ReshardingMetrics.cs | 67 | 61.0 | 91.0% |
| Cdc/ShardedCdcMetrics.cs | 49 | 45.0 | 91.8% |
| Sharding/ColocationMetrics.cs | 25 | 24.0 | 96.0% |
| Migrations/MigrationMetrics.cs | 65 | 63.0 | 96.9% |
| EncinaOpenTelemetryOptions.cs | 3 | 3.0 | 100.0% |
| Sharding/ShadowShardingMetrics.cs | 39 | 39.0 | 100.0% |
| Resharding/ReshardingActivityEnricher.cs | 19 | 19.0 | 100.0% |
| Resharding/ReshardingActivitySource.cs | 27 | 27.0 | 100.0% |
| Resharding/ReshardingHealthCheckOptions.cs | 2 | 2.0 | 100.0% |
| Migrations/SchemaDriftHealthCheckOptions.cs | 3 | 3.0 | 100.0% |
| Enrichers/AuditActivityEnricher.cs | 11 | 11.0 | 100.0% |
| Enrichers/EventMetadataActivityEnricher.cs | 28 | 28.0 | 100.0% |
| Enrichers/MigrationActivityEnricher.cs | 22 | 22.0 | 100.0% |
| Enrichers/ReferenceTableActivityEnricher.cs | 14 | 14.0 | 100.0% |
| Enrichers/RepositoryActivityEnricher.cs | 14 | 14.0 | 100.0% |
| Enrichers/TenancyActivityEnricher.cs | 12 | 12.0 | 100.0% |

### Encina.Polly (37.4%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 145 | 16.7 | 11.5% |
| Behaviors/DatabaseCircuitBreakerPipelineBehavior.cs | 62 | 16.7 | 26.9% |
| ServiceCollectionExtensions.cs | 28 | 8.0 | 28.6% |
| Behaviors/CircuitBreakerPipelineBehavior.cs | 56 | 17.7 | 31.6% |
| Behaviors/RetryPipelineBehavior.cs | 56 | 19.0 | 33.9% |
| Behaviors/BulkheadPipelineBehavior.cs | 61 | 21.7 | 35.5% |
| Behaviors/RateLimitingPipelineBehavior.cs | 45 | 16.3 | 36.3% |
| Attributes/CircuitBreakerAttribute.cs | 25 | 12.5 | 50.0% |
| Attributes/RetryAttribute.cs | 5 | 2.5 | 50.0% |
| RateLimiting/RateLimitResult.cs | 20 | 10.0 | 50.0% |
| Predicates/DatabaseTransientErrorPredicate.cs | 59 | 31.0 | 52.5% |
| Bulkhead/BulkheadManager.cs | 86 | 46.0 | 53.5% |
| RateLimiting/AdaptiveRateLimiter.cs | 110 | 59.5 | 54.1% |
| Attributes/BulkheadAttribute.cs | 3 | 3.0 | 100.0% |
| Attributes/RateLimitAttribute.cs | 7 | 7.0 | 100.0% |

### Encina.Quartz (40.9%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| ServiceCollectionExtensions.cs | 57 | 15.0 | 26.3% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 45 | 19.0 | 42.2% |
| Health/QuartzHealthCheck.cs | 32 | 15.0 | 46.9% |
| QuartzNotificationJob.cs | 22 | 11.0 | 50.0% |
| QuartzRequestJob.cs | 36 | 18.0 | 50.0% |
| EncinaQuartzOptions.cs | 1 | 1.0 | 100.0% |

### Encina.RabbitMQ (50.0%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| ServiceCollectionExtensions.cs | 38 | 13.5 | 35.5% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 30 | 15.0 | 50.0% |
| RabbitMQMessagePublisher.cs | 79 | 39.5 | 50.0% |
| Health/RabbitMQHealthCheck.cs | 10 | 5.0 | 50.0% |
| EncinaRabbitMQOptions.cs | 11 | 11.0 | 100.0% |

### Encina.Redis.PubSub (38.5%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| RedisPubSubMessagePublisher.cs | 88 | 29.0 | 33.0% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 30 | 10.5 | 35.0% |
| ServiceCollectionExtensions.cs | 22 | 9.5 | 43.2% |
| EncinaRedisPubSubOptions.cs | 8 | 8.0 | 100.0% |

### Encina.Refit (48.9%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| ServiceCollectionExtensions.cs | 12 | 5.0 | 41.7% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 35 | 15.0 | 42.9% |
| Handlers/RestApiRequestHandler.cs | 43 | 24.0 | 55.8% |

### Encina.Security (51.4%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| SecurityContextAccessor.cs | 3 | 0.0 | 0.0% |
| SecurityPipelineBehavior.cs | 153 | 47.3 | 30.9% |
| Attributes/RequireAllRolesAttribute.cs | 4 | 2.0 | 50.0% |
| Attributes/RequireClaimAttribute.cs | 9 | 4.5 | 50.0% |
| Attributes/RequireOwnershipAttribute.cs | 4 | 2.0 | 50.0% |
| Attributes/RequirePermissionAttribute.cs | 5 | 2.5 | 50.0% |
| Attributes/RequireRoleAttribute.cs | 4 | 2.0 | 50.0% |
| Attributes/SecurityAttribute.cs | 1 | 0.5 | 50.0% |
| DefaultPermissionEvaluator.cs | 11 | 5.5 | 50.0% |
| DefaultResourceOwnershipEvaluator.cs | 15 | 7.5 | 50.0% |
| SecurityContext.cs | 30 | 15.0 | 50.0% |
| ServiceCollectionExtensions.cs | 16 | 8.0 | 50.0% |
| Health/SecurityHealthCheck.cs | 19 | 9.5 | 50.0% |
| Diagnostics/SecurityDiagnostics.cs | 32 | 16.0 | 50.0% |
| SecurityErrors.cs | 65 | 65.0 | 100.0% |
| SecurityOptions.cs | 7 | 7.0 | 100.0% |

### Encina.Security.ABAC (38.0%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| obj/Release/net10.0/System.Text.RegularExpressions.Generator/System.Text.RegularExpressions.Generator.RegexGenerator/RegexGenerator.g.cs | 42 | 0.0 | 0.0% |
| Attributes/RequireConditionAttribute.cs | 7 | 0.0 | 0.0% |
| ServiceCollectionExtensions.cs | 97 | 0.0 | 0.0% |
| Testing/EELTestHelper.cs | 70 | 0.0 | 0.0% |
| Providers/DefaultAttributeProvider.cs | 5 | 0.0 | 0.0% |
| Providers/DefaultPolicyInformationPoint.cs | 1 | 0.0 | 0.0% |
| Persistence/CachingPolicyStoreDecorator.cs | 141 | 0.0 | 0.0% |
| Persistence/PolicyCacheInvalidationMessage.cs | 5 | 0.0 | 0.0% |
| Persistence/PolicyCachePubSubHostedService.cs | 44 | 0.0 | 0.0% |
| EEL/EELExpressionDiscovery.cs | 16 | 0.0 | 0.0% |
| EEL/EELExpressionPrecompilationService.cs | 73 | 0.0 | 0.0% |
| Administration/InMemoryPolicyAdministrationPoint.cs | 125 | 0.0 | 0.0% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 404 | 10.8 | 2.7% |
| Builders/ConditionBuilder.cs | 146 | 6.5 | 4.5% |
| Functions/FunctionHelpers.cs | 93 | 20.0 | 21.5% |
| AttributeContextBuilder.cs | 39 | 10.5 | 26.9% |
| ABACPipelineBehavior.cs | 201 | 60.7 | 30.2% |
| Functions/Standard/ComparisonFunctions.cs | 253 | 94.5 | 37.4% |
| Functions/Standard/RegexFunctions.cs | 28 | 10.5 | 37.5% |
| Persistence/ExpressionJsonConverter.cs | 144 | 54.5 | 37.9% |
| Diagnostics/ABACDiagnostics.cs | 134 | 51.0 | 38.1% |
| Persistence/Xacml/XacmlDataTypeMap.cs | 80 | 31.5 | 39.4% |
| ABACAttributeInfo.cs | 20 | 8.0 | 40.0% |
| Attributes/RequirePolicyAttribute.cs | 5 | 2.0 | 40.0% |
| Functions/Standard/StringFunctions.cs | 101 | 41.0 | 40.6% |
| Builders/PolicySetBuilder.cs | 83 | 34.5 | 41.6% |
| Evaluation/XACMLPolicyDecisionPoint.cs | 268 | 115.0 | 42.9% |
| Model/PolicyDecision.cs | 8 | 3.5 | 43.8% |
| Builders/PolicyBuilder.cs | 92 | 40.5 | 44.0% |
| Functions/Standard/ArithmeticFunctions.cs | 151 | 67.5 | 44.7% |
| Evaluation/ConditionEvaluator.cs | 49 | 22.5 | 45.9% |
| CombiningAlgorithms/PermitOverridesAlgorithm.cs | 40 | 18.5 | 46.2% |
| Persistence/Xacml/XacmlXmlPolicySerializer.cs | 474 | 219.7 | 46.3% |
| Evaluation/TargetEvaluator.cs | 56 | 26.0 | 46.4% |
| ABACPolicySeedingHostedService.cs | 58 | 27.0 | 46.5% |
| Functions/Standard/BagFunctions.cs | 96 | 46.5 | 48.4% |
| CombiningAlgorithms/DenyOverridesAlgorithm.cs | 53 | 26.0 | 49.1% |
| EEL/EELCompiler.cs | 60 | 29.5 | 49.2% |
| Functions/DefaultFunctionRegistry.cs | 24 | 12.0 | 50.0% |
| Functions/DelegateFunction.cs | 5 | 2.5 | 50.0% |
| Functions/Standard/EqualityFunctions.cs | 71 | 35.5 | 50.0% |
| Functions/Standard/HigherOrderFunctions.cs | 155 | 77.5 | 50.0% |
| Functions/Standard/LogicalFunctions.cs | 65 | 32.5 | 50.0% |
| Functions/Standard/SetFunctions.cs | 102 | 51.0 | 50.0% |
| Functions/Standard/TypeConversionFunctions.cs | 83 | 41.5 | 50.0% |
| Model/AdviceExpression.cs | 3 | 1.5 | 50.0% |
| Model/AllOf.cs | 1 | 0.5 | 50.0% |
| Model/AnyOf.cs | 1 | 0.5 | 50.0% |
| Model/Apply.cs | 2 | 1.0 | 50.0% |
| Model/AttributeAssignment.cs | 3 | 1.5 | 50.0% |
| Model/AttributeBag.cs | 13 | 6.5 | 50.0% |
| Model/AttributeDesignator.cs | 4 | 2.0 | 50.0% |
| Model/AttributeValue.cs | 2 | 1.0 | 50.0% |
| Model/DecisionStatus.cs | 2 | 1.0 | 50.0% |
| Model/Match.cs | 3 | 1.5 | 50.0% |
| Model/Obligation.cs | 3 | 1.5 | 50.0% |
| Model/PolicyEvaluationContext.cs | 6 | 3.0 | 50.0% |
| Model/PolicyEvaluationResult.cs | 4 | 2.0 | 50.0% |
| Model/Rule.cs | 7 | 3.5 | 50.0% |
| Model/RuleEvaluationResult.cs | 4 | 2.0 | 50.0% |
| Model/Target.cs | 1 | 0.5 | 50.0% |
| Model/VariableDefinition.cs | 2 | 1.0 | 50.0% |
| Model/VariableReference.cs | 1 | 0.5 | 50.0% |
| ObligationExecutor.cs | 99 | 49.5 | 50.0% |
| Persistence/Xacml/XacmlFunctionRegistry.cs | 135 | 67.5 | 50.0% |
| Persistence/Xacml/XacmlNamespaces.cs | 25 | 12.5 | 50.0% |
| EEL/EELGlobals.cs | 4 | 2.0 | 50.0% |
| CombiningAlgorithms/CombiningAlgorithmFactory.cs | 19 | 9.5 | 50.0% |
| CombiningAlgorithms/DenyUnlessPermitAlgorithm.cs | 13 | 6.5 | 50.0% |
| CombiningAlgorithms/FirstApplicableAlgorithm.cs | 17 | 8.5 | 50.0% |
| CombiningAlgorithms/OnlyOneApplicableAlgorithm.cs | 31 | 15.5 | 50.0% |
| CombiningAlgorithms/OrderedDenyOverridesAlgorithm.cs | 4 | 2.0 | 50.0% |
| CombiningAlgorithms/OrderedPermitOverridesAlgorithm.cs | 4 | 2.0 | 50.0% |
| CombiningAlgorithms/PermitUnlessDenyAlgorithm.cs | 13 | 6.5 | 50.0% |
| Builders/AdviceBuilder.cs | 43 | 21.5 | 50.0% |
| Builders/ObligationBuilder.cs | 43 | 21.5 | 50.0% |
| Builders/RuleBuilder.cs | 54 | 27.0 | 50.0% |
| Builders/TargetBuilder.cs | 57 | 28.5 | 50.0% |
| Administration/PersistentPolicyAdministrationPoint.cs | 296 | 149.0 | 50.3% |
| Health/ABACHealthCheck.cs | 53 | 27.5 | 51.9% |
| Persistence/Xacml/XacmlMappingExtensions.cs | 157 | 94.0 | 59.9% |
| Persistence/PolicyEntityMapper.cs | 36 | 23.0 | 63.9% |
| ABACOptions.cs | 22 | 15.0 | 68.2% |
| Persistence/PolicyEntity.cs | 8 | 6.0 | 75.0% |
| Persistence/PolicySetEntity.cs | 8 | 6.0 | 75.0% |
| Persistence/DefaultPolicySerializer.cs | 35 | 26.3 | 75.2% |
| ABACErrors.cs | 202 | 166.0 | 82.2% |
| PolicyCachingOptions.cs | 6 | 5.0 | 83.3% |
| Model/Policy.cs | 11 | 9.5 | 86.4% |
| Model/PolicySet.cs | 11 | 9.5 | 86.4% |

### Encina.Security.AntiTampering (42.6%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| Nonce/DistributedCacheNonceStore.cs | 20 | 0.0 | 0.0% |
| ServiceCollectionExtensions.cs | 26 | 1.0 | 3.9% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 30 | 1.6 | 5.3% |
| Diagnostics/AntiTamperingDiagnostics.cs | 42 | 4.0 | 9.5% |
| Pipeline/HMACValidationPipelineBehavior.cs | 172 | 53.0 | 30.8% |
| Nonce/InMemoryNonceStore.cs | 34 | 13.5 | 39.7% |
| Health/AntiTamperingHealthCheck.cs | 62 | 30.0 | 48.4% |
| Attributes/RequireSignatureAttribute.cs | 2 | 1.0 | 50.0% |
| HMAC/SignatureComponents.cs | 17 | 8.5 | 50.0% |
| HMAC/HMACSigner.cs | 93 | 49.0 | 52.7% |
| Http/RequestSigningClient.cs | 51 | 32.0 | 62.8% |
| AntiTamperingOptions.cs | 18 | 17.0 | 94.4% |
| AntiTamperingErrors.cs | 49 | 49.0 | 100.0% |
| HMAC/SigningContext.cs | 5 | 5.0 | 100.0% |

### Encina.Security.Audit (34.0%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| AuditedReadOnlyRepository.cs | 101 | 0.0 | 0.0% |
| Notifications/SensitiveDataAccessedNotification.cs | 5 | 0.0 | 0.0% |
| Health/AuditStoreHealthCheck.cs | 15 | 0.0 | 0.0% |
| Health/ReadAuditStoreHealthCheck.cs | 15 | 0.0 | 0.0% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 154 | 2.8 | 1.8% |
| AuditedRepository.cs | 108 | 5.0 | 4.6% |
| obj/Release/net10.0/System.Text.RegularExpressions.Generator/System.Text.RegularExpressions.Generator.RegexGenerator/RegexGenerator.g.cs | 236 | 44.2 | 18.7% |
| Diagnostics/ReadAuditActivitySource.cs | 27 | 7.0 | 25.9% |
| ServiceCollectionExtensions.cs | 26 | 7.0 | 26.9% |
| AuditRetentionService.cs | 53 | 14.5 | 27.4% |
| AuditPipelineBehavior.cs | 98 | 35.3 | 36.0% |
| DefaultAuditEntryFactory.cs | 84 | 41.0 | 48.8% |
| AuditableAttribute.cs | 10 | 5.0 | 50.0% |
| AuditEntry.cs | 19 | 9.5 | 50.0% |
| AuditQuery.cs | 61 | 30.5 | 50.0% |
| NullPiiMasker.cs | 2 | 1.0 | 50.0% |
| PagedResult.cs | 25 | 12.5 | 50.0% |
| ReadAuditEntry.cs | 11 | 5.5 | 50.0% |
| ReadAuditEntryEntity.cs | 11 | 5.5 | 50.0% |
| ReadAuditEntryMapper.cs | 45 | 22.5 | 50.0% |
| ReadAuditQuery.cs | 50 | 25.0 | 50.0% |
| RequestMetadataExtractor.cs | 40 | 20.0 | 50.0% |
| Diagnostics/ReadAuditMeter.cs | 25 | 12.5 | 50.0% |
| ReadAuditRetentionService.cs | 63 | 33.0 | 52.4% |
| ReadAuditContext.cs | 4 | 2.5 | 62.5% |
| DefaultSensitiveDataRedactor.cs | 123 | 80.5 | 65.5% |
| AuditOptions.cs | 26 | 26.0 | 100.0% |
| ReadAuditErrors.cs | 43 | 43.0 | 100.0% |
| ReadAuditOptions.cs | 19 | 19.0 | 100.0% |

### Encina.Security.Encryption (45.0%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| Diagnostics/EncryptionDiagnostics.cs | 32 | 0.0 | 0.0% |
| EncryptionPipelineBehavior.cs | 106 | 22.3 | 21.1% |
| Health/EncryptionHealthCheck.cs | 72 | 31.5 | 43.8% |
| EncryptionOrchestrator.cs | 139 | 63.5 | 45.7% |
| EncryptedPropertyCache.cs | 30 | 14.0 | 46.7% |
| Attributes/EncryptAttribute.cs | 2 | 1.0 | 50.0% |
| Attributes/EncryptionAttribute.cs | 2 | 1.0 | 50.0% |
| EncryptedPropertyInfo.cs | 13 | 6.5 | 50.0% |
| Algorithms/AesGcmFieldEncryptor.cs | 90 | 45.5 | 50.6% |
| ServiceCollectionExtensions.cs | 19 | 10.5 | 55.3% |
| EncryptionContext.cs | 4 | 2.5 | 62.5% |
| EncryptedValue.cs | 5 | 5.0 | 100.0% |
| EncryptionErrors.cs | 46 | 46.0 | 100.0% |
| EncryptionOptions.cs | 5 | 5.0 | 100.0% |

### Encina.Security.PII (44.4%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| PIIErrors.cs | 29 | 0.0 | 0.0% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 45 | 2.8 | 6.2% |
| PIIMaskingPipelineBehavior.cs | 53 | 18.0 | 34.0% |
| Diagnostics/PIIDiagnostics.cs | 60 | 22.5 | 37.5% |
| PIIMasker.cs | 298 | 130.5 | 43.8% |
| Strategies/DateOfBirthMaskingStrategy.cs | 43 | 19.0 | 44.2% |
| Strategies/NameMaskingStrategy.cs | 29 | 13.5 | 46.5% |
| Health/PIIHealthCheck.cs | 66 | 31.5 | 47.7% |
| Strategies/IPAddressMaskingStrategy.cs | 24 | 11.5 | 47.9% |
| ServiceCollectionExtensions.cs | 28 | 13.5 | 48.2% |
| PIILoggerExtensions.cs | 18 | 9.0 | 50.0% |
| Strategies/AddressMaskingStrategy.cs | 20 | 10.0 | 50.0% |
| Strategies/CreditCardMaskingStrategy.cs | 33 | 16.5 | 50.0% |
| Strategies/EmailMaskingStrategy.cs | 21 | 10.5 | 50.0% |
| Strategies/FullMaskingStrategy.cs | 24 | 12.0 | 50.0% |
| Strategies/HashHelper.cs | 6 | 3.0 | 50.0% |
| Strategies/PhoneMaskingStrategy.cs | 35 | 17.5 | 50.0% |
| Strategies/SSNMaskingStrategy.cs | 32 | 16.0 | 50.0% |
| Attributes/MaskInLogsAttribute.cs | 6 | 3.0 | 50.0% |
| Attributes/PIIAttribute.cs | 7 | 3.5 | 50.0% |
| Attributes/SensitiveDataAttribute.cs | 6 | 3.0 | 50.0% |
| MaskingOptions.cs | 15 | 15.0 | 100.0% |
| PIIOptions.cs | 31 | 31.0 | 100.0% |

### Encina.Security.Sanitization (41.5%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| Diagnostics/SanitizationDiagnostics.cs | 42 | 0.0 | 0.0% |
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 45 | 3.2 | 7.1% |
| Sanitizers/ShellSanitizer.cs | 17 | 3.5 | 20.6% |
| InputSanitizationPipelineBehavior.cs | 73 | 16.3 | 22.4% |
| OutputEncodingPipelineBehavior.cs | 122 | 30.7 | 25.1% |
| obj/Release/net10.0/System.Text.RegularExpressions.Generator/System.Text.RegularExpressions.Generator.RegexGenerator/RegexGenerator.g.cs | 122 | 31.0 | 25.4% |
| Health/SanitizationHealthCheck.cs | 36 | 14.0 | 38.9% |
| SanitizationOrchestrator.cs | 66 | 28.5 | 43.2% |
| EncodingPropertyCache.cs | 44 | 20.0 | 45.5% |
| SanitizationPropertyCache.cs | 44 | 20.0 | 45.5% |
| ServiceCollectionExtensions.cs | 17 | 8.5 | 50.0% |
| Sanitizers/JsonSanitizer.cs | 4 | 2.0 | 50.0% |
| Sanitizers/SqlSanitizer.cs | 10 | 5.0 | 50.0% |
| Sanitizers/XmlSanitizer.cs | 14 | 7.0 | 50.0% |
| Profiles/SanitizationProfileBuilder.cs | 27 | 13.5 | 50.0% |
| Attributes/EncodeForHtmlAttribute.cs | 1 | 0.5 | 50.0% |
| Attributes/EncodeForJavaScriptAttribute.cs | 1 | 0.5 | 50.0% |
| Attributes/EncodeForUrlAttribute.cs | 1 | 0.5 | 50.0% |
| Attributes/SanitizeAttribute.cs | 2 | 1.0 | 50.0% |
| Attributes/SanitizeHtmlAttribute.cs | 1 | 0.5 | 50.0% |
| Attributes/SanitizeSqlAttribute.cs | 1 | 0.5 | 50.0% |
| Attributes/StripHtmlAttribute.cs | 1 | 0.5 | 50.0% |
| Sanitizers/HtmlSanitizerWrapper.cs | 22 | 13.0 | 59.1% |
| Encoders/DefaultOutputEncoder.cs | 18 | 11.5 | 63.9% |
| DefaultSanitizer.cs | 19 | 15.5 | 81.6% |
| Profiles/SanitizationProfile.cs | 12 | 10.0 | 83.3% |
| SanitizationOptions.cs | 25 | 24.0 | 96.0% |
| SanitizationErrors.cs | 17 | 17.0 | 100.0% |
| Profiles/SanitizationProfiles.cs | 60 | 60.0 | 100.0% |

### Encina.Security.Secrets (43.6%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 255 | 14.4 | 5.7% |
| Caching/SecretCachePubSubHostedService.cs | 62 | 11.2 | 18.1% |
| Caching/CachingSecretWriterDecorator.cs | 63 | 11.4 | 18.1% |
| Caching/SecretCacheInvalidationMessage.cs | 4 | 0.8 | 20.0% |
| Injection/SecretInjectionPipelineBehavior.cs | 66 | 14.3 | 21.7% |
| Caching/CachingSecretReaderDecorator.cs | 154 | 36.4 | 23.6% |
| Resilience/SecretsResiliencePipelineFactory.cs | 94 | 27.0 | 28.7% |
| ServiceCollectionExtensions.cs | 145 | 45.0 | 31.0% |
| Caching/SecretCachingOptions.cs | 4 | 1.6 | 40.0% |
| Configuration/SecretsConfigurationProvider.cs | 56 | 23.0 | 41.1% |
| Providers/ConfigurationSecretProvider.cs | 46 | 19.0 | 41.3% |
| Health/SecretsHealthCheck.cs | 58 | 25.5 | 44.0% |
| Providers/EnvironmentSecretProvider.cs | 29 | 13.5 | 46.5% |
| Providers/FailoverSecretReader.cs | 45 | 22.0 | 48.9% |
| Attributes/InjectSecretAttribute.cs | 7 | 3.5 | 50.0% |
| SecretReference.cs | 5 | 2.5 | 50.0% |
| Rotation/SecretRotationCoordinator.cs | 47 | 23.5 | 50.0% |
| Resilience/SecretsCircuitBreakerState.cs | 4 | 2.0 | 50.0% |
| Resilience/SecretsTransientErrorDetector.cs | 9 | 4.5 | 50.0% |
| Resilience/TransientSecretException.cs | 4 | 2.0 | 50.0% |
| Diagnostics/SecretsDiagnostics.cs | 30 | 15.0 | 50.0% |
| Configuration/SecretsConfigurationSource.cs | 7 | 3.5 | 50.0% |
| Auditing/AuditedSecretReaderDecorator.cs | 65 | 32.5 | 50.0% |
| Auditing/AuditedSecretRotatorDecorator.cs | 56 | 28.0 | 50.0% |
| Auditing/AuditedSecretWriterDecorator.cs | 56 | 28.0 | 50.0% |
| Resilience/ResilientSecretReaderDecorator.cs | 94 | 50.0 | 53.2% |
| Diagnostics/SecretsActivitySource.cs | 84 | 49.0 | 58.3% |
| Resilience/ResilientSecretRotatorDecorator.cs | 54 | 32.0 | 59.3% |
| Resilience/ResilientSecretWriterDecorator.cs | 54 | 32.0 | 59.3% |
| Diagnostics/SecretsMetrics.cs | 132 | 131.0 | 99.2% |
| SecretsErrors.cs | 104 | 104.0 | 100.0% |
| SecretsOptions.cs | 15 | 15.0 | 100.0% |
| Resilience/SecretsResilienceOptions.cs | 9 | 9.0 | 100.0% |
| Configuration/SecretsConfigurationOptions.cs | 5 | 5.0 | 100.0% |

### Encina.Security.Secrets.AwsSecretsManager (44.7%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 45 | 9.0 | 20.0% |
| ServiceCollectionExtensions.cs | 22 | 8.5 | 38.6% |
| AwsSecretsManagerProvider.cs | 108 | 59.0 | 54.6% |
| AwsSecretsManagerOptions.cs | 3 | 3.0 | 100.0% |

### Encina.Security.Secrets.AzureKeyVault (44.0%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 45 | 8.4 | 18.7% |
| ServiceCollectionExtensions.cs | 23 | 10.5 | 45.6% |
| AzureKeyVaultSecretProvider.cs | 80 | 45.0 | 56.2% |
| AzureKeyVaultOptions.cs | 4 | 3.0 | 75.0% |

### Encina.Security.Secrets.GoogleCloudSecretManager (46.8%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 45 | 9.0 | 20.0% |
| GoogleCloudSecretManagerProvider.cs | 110 | 61.5 | 55.9% |
| ServiceCollectionExtensions.cs | 16 | 9.0 | 56.2% |
| GoogleCloudSecretManagerOptions.cs | 1 | 1.0 | 100.0% |

### Encina.Security.Secrets.HashiCorpVault (47.3%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| obj/Release/net10.0/Microsoft.Extensions.Logging.Generators/Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator/LoggerMessage.g.cs | 40 | 8.0 | 20.0% |
| ServiceCollectionExtensions.cs | 22 | 11.0 | 50.0% |
| HashiCorpVaultSecretProvider.cs | 85 | 49.0 | 57.6% |
| HashiCorpVaultOptions.cs | 3 | 3.0 | 100.0% |

### Encina.SignalR (43.8%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| SignalRNotificationBroadcaster.cs | 71 | 25.5 | 35.9% |
| Health/SignalRHealthCheck.cs | 16 | 7.0 | 43.8% |
| MediatorHub.cs | 88 | 40.0 | 45.5% |
| BroadcastToSignalRAttribute.cs | 11 | 5.5 | 50.0% |
| ServiceCollectionExtensions.cs | 6 | 3.0 | 50.0% |
| SignalRNotificationBehavior.cs | 12 | 6.0 | 50.0% |
| SignalROptions.cs | 4 | 4.0 | 100.0% |

### Encina.Tenancy (57.9%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| Data/TenantConnectionFactoryBase.cs | 35 | 0.0 | 0.0% |
| Abstractions/ITenantStore.cs | 1 | 0.5 | 50.0% |
| Abstractions/TenantInfo.cs | 11 | 5.5 | 50.0% |
| Core/DefaultTenantProvider.cs | 12 | 6.0 | 50.0% |
| ServiceCollectionExtensions.cs | 50 | 25.0 | 50.0% |
| Stores/InMemoryTenantStore.cs | 23 | 11.5 | 50.0% |
| Health/TenantHealthCheck.cs | 13 | 6.5 | 50.0% |
| Configuration/TenancyOptions.cs | 8 | 8.0 | 100.0% |
| Data/TenantConnectionOptions.cs | 5 | 5.0 | 100.0% |
| Diagnostics/TenancyActivitySource.cs | 34 | 34.0 | 100.0% |
| Diagnostics/TenancyMetrics.cs | 22 | 22.0 | 100.0% |

### Encina.Tenancy.AspNetCore (45.1%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| ApplicationBuilderExtensions.cs | 2 | 0.0 | 0.0% |
| Health/HealthCheckBuilderExtensions.cs | 33 | 0.0 | 0.0% |
| Resolution/Resolvers/SubdomainTenantResolver.cs | 26 | 12.5 | 48.1% |
| Middleware/TenantResolutionMiddleware.cs | 30 | 15.0 | 50.0% |
| Resolution/Resolvers/ClaimTenantResolver.cs | 16 | 8.0 | 50.0% |
| Resolution/Resolvers/HeaderTenantResolver.cs | 14 | 7.0 | 50.0% |
| Resolution/Resolvers/RouteTenantResolver.cs | 14 | 7.0 | 50.0% |
| Resolution/TenantResolverChain.cs | 12 | 6.0 | 50.0% |
| ServiceCollectionExtensions.cs | 20 | 10.0 | 50.0% |
| Configuration/TenancyAspNetCoreOptions.cs | 18 | 18.0 | 100.0% |

### Encina.Testing (40.9%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| Modules/ModuleArchitectureRules.cs | 69 | 0.0 | 0.0% |
| Modules/ModuleTestContext.cs | 61 | 2.0 | 3.3% |
| Assertions/AndConstraint.cs | 30 | 4.5 | 15.0% |
| EncinaFixture.cs | 37 | 11.0 | 29.7% |
| Modules/ModuleArchitectureAnalyzer.cs | 219 | 75.5 | 34.5% |
| Assertions/EitherCollectionAssertions.cs | 150 | 54.5 | 36.3% |
| Sagas/SagaSpecification.cs | 125 | 49.5 | 39.6% |
| EncinaTestContext.cs | 184 | 74.0 | 40.2% |
| Assertions/EitherAssertions.cs | 99 | 40.0 | 40.4% |
| Modules/MockModuleApi.cs | 87 | 36.0 | 41.4% |
| Assertions/StreamingAssertions.cs | 74 | 31.0 | 41.9% |
| Messaging/InboxTestHelper.cs | 229 | 96.5 | 42.1% |
| Handlers/HandlerSpecification.cs | 104 | 44.5 | 42.8% |
| Messaging/SchedulingTestHelper.cs | 352 | 151.5 | 43.0% |
| Modules/ModuleTestFixture.cs | 138 | 60.5 | 43.8% |
| EncinaTestFixture.cs | 130 | 57.0 | 43.9% |
| Messaging/OutboxTestHelper.cs | 206 | 90.5 | 43.9% |
| Messaging/SagaTestHelper.cs | 377 | 171.0 | 45.4% |
| Handlers/ScenarioResult.cs | 89 | 42.0 | 47.2% |
| Time/FakeTimer.cs | 58 | 28.0 | 48.3% |
| EventSourcing/AggregateTestBase.cs | 121 | 59.0 | 48.8% |
| Mutations/MutationKillerAttribute.cs | 9 | 4.5 | 50.0% |
| Mutations/NeedsMutationCoverageAttribute.cs | 8 | 4.0 | 50.0% |
| Modules/CircularDependency.cs | 1 | 0.5 | 50.0% |
| Modules/IntegrationEventCollector.cs | 88 | 44.0 | 50.0% |
| Modules/ModuleDependency.cs | 1 | 0.5 | 50.0% |
| Modules/ModuleInfo.cs | 1 | 0.5 | 50.0% |
| Handlers/Scenario.cs | 36 | 18.0 | 50.0% |
| Time/FakeTimeProvider.cs | 75 | 42.0 | 56.0% |

### Encina.Testing.Architecture (34.9%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| EncinaArchitectureTestBase.cs | 74 | 0.0 | 0.0% |
| EncinaArchitectureRulesBuilder.cs | 119 | 46.0 | 38.7% |
| EventIdUniquenessRule.cs | 83 | 33.0 | 39.8% |
| EncinaArchitectureRules.cs | 229 | 97.0 | 42.4% |

### Encina.Testing.Bogus (36.9%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| SagaStateFaker.cs | 47 | 0.0 | 0.0% |
| EncinaFaker.cs | 129 | 34.0 | 26.4% |
| CacheValueFaker.cs | 38 | 18.5 | 48.7% |
| CacheKeyFaker.cs | 57 | 28.0 | 49.1% |
| ScheduledMessageFaker.cs | 66 | 32.5 | 49.2% |
| CacheEntryFaker.cs | 50 | 25.0 | 50.0% |
| OutboxMessageFaker.cs | 37 | 18.5 | 50.0% |

### Encina.Testing.Fakes (31.8%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| FakeEncina.cs | 90 | 3.5 | 3.9% |
| Providers/FakePubSubProvider.cs | 133 | 13.0 | 9.8% |
| Stores/FakeCdcDeadLetterStore.cs | 56 | 6.5 | 11.6% |
| Stores/FakeDeadLetterStore.cs | 116 | 22.0 | 19.0% |
| Providers/FakeCacheProvider.cs | 164 | 43.0 | 26.2% |
| Stores/FakeInboxStore.cs | 81 | 36.0 | 44.4% |
| Models/FakeDeadLetterMessage.cs | 35 | 16.5 | 47.1% |
| Stores/FakeScheduledMessageStore.cs | 104 | 49.5 | 47.6% |
| Models/FakeScheduledMessage.cs | 32 | 15.5 | 48.4% |
| ServiceCollectionExtensions.cs | 54 | 27.0 | 50.0% |
| Stores/FakeOutboxStore.cs | 79 | 39.5 | 50.0% |
| Stores/FakeSagaStore.cs | 79 | 39.5 | 50.0% |
| Models/FakeInboxMessage.cs | 23 | 11.5 | 50.0% |
| Models/FakeOutboxMessage.cs | 21 | 10.5 | 50.0% |
| Models/FakeSagaState.cs | 23 | 11.5 | 50.0% |
| Factories/FakeOutboxMessageFactory.cs | 8 | 4.0 | 50.0% |

### Encina.Testing.FsCheck (48.0%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| EncinaArbitraryProvider.cs | 9 | 3.5 | 38.9% |
| EncinaProperties.cs | 123 | 54.5 | 44.3% |
| ArbitraryMessages.cs | 48 | 21.5 | 44.8% |
| GenExtensions.cs | 99 | 49.0 | 49.5% |
| EncinaArbitraries.cs | 247 | 123.5 | 50.0% |
| PropertyTestBase.cs | 19 | 9.5 | 50.0% |

### Encina.Testing.Pact (21.5%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| XunitOutputAdapter.cs | 11 | 0.0 | 0.0% |
| EncinaPactProviderVerifier.cs | 356 | 26.5 | 7.4% |
| PactExtensions.cs | 73 | 9.5 | 13.0% |
| EncinaPactFixture.cs | 93 | 34.5 | 37.1% |
| EncinaPactConsumerBuilder.cs | 200 | 87.0 | 43.5% |

### Encina.Testing.Respawn (29.0%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| DatabaseRespawner.cs | 51 | 4.0 | 7.8% |
| SqliteRespawner.cs | 69 | 6.5 | 9.4% |
| MySqlRespawner.cs | 9 | 3.0 | 33.3% |
| PostgreSqlRespawner.cs | 9 | 3.0 | 33.3% |
| SqlServerRespawner.cs | 9 | 3.0 | 33.3% |
| RespawnerFactory.cs | 84 | 34.0 | 40.5% |
| RespawnOptions.cs | 19 | 19.0 | 100.0% |

### Encina.Testing.Shouldly (32.6%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| StreamingShouldlyExtensions.cs | 115 | 8.5 | 7.4% |
| EitherCollectionShouldlyExtensions.cs | 71 | 33.0 | 46.5% |
| EitherShouldlyExtensions.cs | 89 | 41.5 | 46.6% |
| AggregateShouldlyExtensions.cs | 47 | 22.0 | 46.8% |

### Encina.Testing.Testcontainers (21.4%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| MongoDbContainerFixture.cs | 3 | 0.0 | 0.0% |
| MySqlContainerFixture.cs | 6 | 0.0 | 0.0% |
| PostgreSqlContainerFixture.cs | 6 | 0.0 | 0.0% |
| RedisContainerFixture.cs | 3 | 0.0 | 0.0% |
| ContainerFixtureBase.cs | 37 | 2.0 | 5.4% |
| ConfiguredContainerFixture.cs | 52 | 9.0 | 17.3% |
| DatabaseIntegrationTestBase.cs | 48 | 10.0 | 20.8% |
| SqlServerContainerFixture.cs | 14 | 4.0 | 28.6% |
| EncinaContainers.cs | 30 | 15.0 | 50.0% |
| MySqlIntegrationTestBase.cs | 3 | 1.5 | 50.0% |
| PostgreSqlIntegrationTestBase.cs | 3 | 1.5 | 50.0% |
| SqlServerIntegrationTestBase.cs | 3 | 1.5 | 50.0% |

### Encina.Testing.Verify (29.4%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| EncinaTestContextExtensions.cs | 3 | 0.0 | 0.0% |
| EncinaVerify.cs | 187 | 39.0 | 20.9% |
| obj/Release/net10.0/System.Text.RegularExpressions.Generator/System.Text.RegularExpressions.Generator.RegexGenerator/RegexGenerator.g.cs | 181 | 60.0 | 33.1% |
| EncinaErrorConverter.cs | 21 | 7.0 | 33.3% |
| EncinaVerifySettings.cs | 45 | 22.5 | 50.0% |

### Encina.Testing.WireMock (38.5%)

| File | Lines | Covered | Coverage |
|------|:-----:|:-------:|:--------:|
| WireMockContainerFixture.cs | 18 | 0.0 | 0.0% |
| EncinaWireMockFixture.cs | 141 | 50.5 | 35.8% |
| WebhookTestingExtensions.cs | 93 | 39.5 | 42.5% |
| EncinaRefitMockFixture.cs | 96 | 44.0 | 45.8% |

</details>
