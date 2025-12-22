# Changelog

All notable changes to Encina will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- LoggerMessage source generators across all packages for CA1848 compliance (2025-12-21)
- SignalR integration package with EncinaHub base class (2025-12-21)
- MongoDB provider for messaging patterns (2025-12-21)
- EventStoreDB integration for event sourcing (2025-12-21)
- Choreography-based saga abstractions (event-driven) (2025-12-21)

### Changed

- All logging now uses high-performance `LoggerMessage` delegates instead of `ILogger.LogXxx()`

---

## [0.9.0] - 2025-12-21

### Added

#### Messaging Transports (12 packages)

- Encina.Wolverine - WolverineFx 5.7.1 integration
- Encina.NServiceBus - NServiceBus 9.2.8 integration
- Encina.RabbitMQ - RabbitMQ.Client 7.2.0 integration
- Encina.AzureServiceBus - Azure Service Bus 7.20.1 integration
- Encina.AmazonSQS - AWS SQS/SNS 4.0.2.3 integration
- Encina.Kafka - Confluent.Kafka 2.12.0 integration
- Encina.Redis.PubSub - StackExchange.Redis pub/sub
- Encina.InMemory - System.Threading.Channels message bus
- Encina.NATS - NATS.Net 2.6.11 with JetStream support
- Encina.MQTT - MQTTnet 5.0.1 integration
- Encina.gRPC - Grpc.AspNetCore 2.71.0 Encina service
- Encina.GraphQL - HotChocolate 15.1.11 bridge

#### Caching (8 packages)

- Encina.Caching - Core abstractions (ICacheProvider, ICacheKeyGenerator)
- Encina.Caching.Memory - IMemoryCache provider (109 tests)
- Encina.Caching.Hybrid - Microsoft HybridCache provider (.NET 9+)
- Encina.Caching.Redis - StackExchange.Redis provider
- Encina.Caching.Garnet - Microsoft Garnet provider
- Encina.Caching.Valkey - Valkey provider (Redis fork)
- Encina.Caching.Dragonfly - Dragonfly provider
- Encina.Caching.KeyDB - KeyDB provider

#### Resilience (3 packages)

- Encina.Extensions.Resilience - Microsoft standard resilience patterns
- Encina.Refit - Type-safe REST API clients integration
- Encina.Dapr - Service mesh integration (invocation, pub/sub, state, bindings, secrets)

---

## [0.8.0] - 2025-12-19

### Added

#### Database Providers (10 packages)

- Encina.Dapper.SqlServer - SQL Server optimized
- Encina.Dapper.PostgreSQL - PostgreSQL with Npgsql
- Encina.Dapper.MySQL - MySQL/MariaDB with MySqlConnector
- Encina.Dapper.Sqlite - SQLite for testing
- Encina.Dapper.Oracle - Oracle with ManagedDataAccess
- Encina.ADO.SqlServer - Raw ADO.NET (fastest)
- Encina.ADO.PostgreSQL - PostgreSQL optimized
- Encina.ADO.MySQL - MySQL/MariaDB optimized
- Encina.ADO.Sqlite - SQLite optimized
- Encina.ADO.Oracle - Oracle optimized

### Changed

- Established Testcontainers-based test architecture
- Created Encina.TestInfrastructure for shared test fixtures

---

## [0.7.0] - 2025-12-18

### Added

- Encina.Hangfire - Background job scheduling with Hangfire
- Encina.Quartz - Enterprise CRON scheduling with Quartz.NET

---

## [0.6.0] - 2025-12-17

### Added

- Encina.AspNetCore - Middleware, authorization, Problem Details
- Encina.Messaging - Shared abstractions for Outbox, Inbox, Sagas
- Encina.EntityFrameworkCore - EF Core implementation of messaging patterns

---

## [0.5.0] - 2025-12-15

### Added

- Encina.GuardClauses - Defensive programming with Ardalis.GuardClauses

---

## [0.4.0] - 2025-12-14

### Added

- Encina.MiniValidator - Lightweight validation (~20KB)

---

## [0.3.0] - 2025-12-13

### Added

- Encina.DataAnnotations - Zero-dependency validation with .NET attributes

---

## [0.2.0] - 2025-12-12

### Added

- Encina.FluentValidation - FluentValidation integration

---

## [0.1.0] - 2025-12-10

### Added

- Encina Core - Pure Railway Oriented Programming with `Either<EncinaError, T>`
- Request/Notification dispatch with Expression tree compilation
- Pipeline pattern (Behaviors, PreProcessors, PostProcessors)
- IRequestContext for ambient context
- Observability with ActivitySource and Metrics
- CQRS markers (ICommand, IQuery)
- PublicAPI Analyzers compliance

---

[Unreleased]: https://github.com/dlrivada/Encina/compare/v0.9.0...HEAD
[0.9.0]: https://github.com/dlrivada/Encina/compare/v0.8.0...v0.9.0
[0.8.0]: https://github.com/dlrivada/Encina/compare/v0.7.0...v0.8.0
[0.7.0]: https://github.com/dlrivada/Encina/compare/v0.6.0...v0.7.0
[0.6.0]: https://github.com/dlrivada/Encina/compare/v0.5.0...v0.6.0
[0.5.0]: https://github.com/dlrivada/Encina/compare/v0.4.0...v0.5.0
[0.4.0]: https://github.com/dlrivada/Encina/compare/v0.3.0...v0.4.0
[0.3.0]: https://github.com/dlrivada/Encina/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/dlrivada/Encina/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/dlrivada/Encina/releases/tag/v0.1.0
