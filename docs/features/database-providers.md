---
title: "Database providers reference"
layout: default
parent: "Features"
---

# Database providers reference

This page is for a developer who already picked a data-access family (see [About Encina's data-access providers](../architecture/data-access-providers.md) if you have not) and now needs the package name, the registration method, which stores exist for each provider, and the SQL differences between SQL Server, PostgreSQL and MySQL. Encina ships **10 database providers**: ADO.NET, Dapper and EF Core each on SQL Server/PostgreSQL/MySQL, plus MongoDB. Oracle and SQLite are not part of this list; see [ADR-009](../architecture/adr/009-remove-oracle-provider-pre-1.0.md) and [ADR-024](../architecture/adr/024-remove-sqlite-provider-pre-1.0.md).

## Packages and registration

| Provider | Package | Primary registration |
|---|---|---|
| ADO.NET / SQL Server | `Encina.ADO.SqlServer` | `services.AddEncinaADO(configure)` |
| ADO.NET / PostgreSQL | `Encina.ADO.PostgreSQL` | `services.AddEncinaADO(configure)` |
| ADO.NET / MySQL | `Encina.ADO.MySQL` | `services.AddEncinaADO(configure)` |
| Dapper / SQL Server | `Encina.Dapper.SqlServer` | `services.AddEncinaDapper(configure)` |
| Dapper / PostgreSQL | `Encina.Dapper.PostgreSQL` | `services.AddEncinaDapper(configure)` |
| Dapper / MySQL | `Encina.Dapper.MySQL` | `services.AddEncinaDapper(configure)` |
| EF Core / SQL Server, PostgreSQL, MySQL | `Encina.EntityFrameworkCore` | `services.AddEncinaEntityFrameworkCore<TDbContext>(configure)` |
| MongoDB | `Encina.MongoDB` | `services.AddEncinaMongoDB(configure)` |

`configure` is `Action<MessagingConfiguration>` for ADO.NET, Dapper and EF Core, and `Action<EncinaMongoDbOptions>` for MongoDB. One package (`Encina.EntityFrameworkCore`) covers all three EF Core databases because EF Core's own provider (`Microsoft.EntityFrameworkCore.SqlServer`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `Pomelo.EntityFrameworkCore.MySql`) already carries the database-specific translation; Encina's store implementations (`OutboxStoreEF`, `InboxStoreEF`, and so on) run unchanged on whichever EF Core provider the `DbContext` uses.

Every family also exposes a repository/unit-of-work opt-in, independent of the messaging patterns:

- ADO.NET / SQL Server and PostgreSQL: `services.AddEncinaUnitOfWork()` registers `IUnitOfWork` as `UnitOfWorkADO`.
- ADO.NET / MySQL: `Encina.ADO.MySQL` defines `UnitOfWorkADO` in `src/Encina.ADO.MySQL/UnitOfWork/UnitOfWorkADO.cs`, but its `ServiceCollectionExtensions.cs` has no `AddEncinaUnitOfWork` method to register it, so there is no registration method — the other two ADO.NET databases have one. Tracking issue: [#1260](https://github.com/dlrivada/Encina/issues/1260).
- Dapper (all three databases): `services.AddEncinaUnitOfWork()` registers `IUnitOfWork` as `UnitOfWorkDapper`.
- EF Core: `services.AddEncinaUnitOfWork<TDbContext>(...)` registers `IUnitOfWork` as `UnitOfWorkEF`.
- MongoDB: `services.AddEncinaUnitOfWork()` registers `IUnitOfWork` as `UnitOfWorkMongoDB`.

## Feature × provider matrix

A cell names the concrete store type when the feature exists for that provider. A store type living in `src/` is the only evidence this table accepts; "not supported" means no such folder or type exists in that package.

| Feature | ADO SqlServer | ADO PostgreSQL | ADO MySQL | Dapper SqlServer | Dapper PostgreSQL | Dapper MySQL | EF Core | MongoDB |
|---|---|---|---|---|---|---|---|---|
| Outbox (`IOutboxStore`) | `OutboxStoreADO` | `OutboxStoreADO` | `OutboxStoreADO` | `OutboxStoreDapper` | `OutboxStoreDapper` | `OutboxStoreDapper` | `OutboxStoreEF` | `OutboxStoreMongoDB` |
| Inbox (`IInboxStore`) | `InboxStoreADO` | `InboxStoreADO` | `InboxStoreADO` | `InboxStoreDapper` | `InboxStoreDapper` | `InboxStoreDapper` | `InboxStoreEF` | `InboxStoreMongoDB` |
| Saga (`ISagaStore`) | `SagaStoreADO` | `SagaStoreADO` | `SagaStoreADO` | `SagaStoreDapper` | `SagaStoreDapper` | `SagaStoreDapper` | `SagaStoreEF` | `SagaStoreMongoDB` |
| Scheduling (`IScheduledMessageStore`) | `ScheduledMessageStoreADO` | `ScheduledMessageStoreADO` | `ScheduledMessageStoreADO` | `ScheduledMessageStoreDapper` | `ScheduledMessageStoreDapper` | `ScheduledMessageStoreDapper` | `ScheduledMessageStoreEF` | `ScheduledMessageStoreMongoDB` |
| Audit trail (`IAuditStore`) | `AuditStoreADO` | `AuditStoreADO` | `AuditStoreADO` | `AuditStoreDapper` | `AuditStoreDapper` | `AuditStoreDapper` | `AuditStoreEF` | `AuditStoreMongoDB` |
| Audit log (`IAuditLogStore`, entity interceptor) | `AuditLogStoreADO` | `AuditLogStoreADO` | `AuditLogStoreADO` | `AuditLogStoreDapper` | `AuditLogStoreDapper` | `AuditLogStoreDapper` | `AuditLogStoreEF` | `AuditLogStoreMongoDB` |
| Read audit (`IReadAuditStore`) | `ReadAuditStoreADO` | `ReadAuditStoreADO` | `ReadAuditStoreADO` | `ReadAuditStoreDapper` | `ReadAuditStoreDapper` | `ReadAuditStoreDapper` | `ReadAuditStoreEF` | `ReadAuditStoreMongoDB` |
| Repository (functional) | `FunctionalRepositoryADO` | `FunctionalRepositoryADO` | `FunctionalRepositoryADO` | `FunctionalRepositoryDapper` | `FunctionalRepositoryDapper` | `FunctionalRepositoryDapper` | `FunctionalRepositoryEF` | `FunctionalRepositoryMongoDB` |
| Unit of work (`IUnitOfWork`) | `UnitOfWorkADO` (present, not registered — see above) | `UnitOfWorkADO` | `UnitOfWorkADO` (present, not registered — see above) | `UnitOfWorkDapper` | `UnitOfWorkDapper` | `UnitOfWorkDapper` | `UnitOfWorkEF` | `UnitOfWorkMongoDB` |
| Bulk operations (`IBulkOperations`) | `BulkOperationsADO` | `BulkOperationsPostgreSQL` | `BulkOperationsMySQL` | `BulkOperationsDapper` | `BulkOperationsDapper` | `BulkOperationsDapper` | `BulkOperationsEFSqlServer` / `BulkOperationsEFPostgreSql` / `BulkOperationsEFMySql` (one `Encina.EntityFrameworkCore` package, selected by the EF Core provider in use) | `BulkOperationsMongoDB` |
| Specification (SQL/filter translation) | `SpecificationSqlBuilder` | `SpecificationSqlBuilder` | `SpecificationSqlBuilder` | `SpecificationSqlBuilder` | `SpecificationSqlBuilder` | `SpecificationSqlBuilder` | `SpecificationEvaluator` | `SpecificationEvaluatorMongoDB` / `SpecificationFilterBuilder` |
| Soft delete | `SoftDeleteSpecificationSqlBuilder` | `SoftDeleteSpecificationSqlBuilder` | `SoftDeleteSpecificationSqlBuilder` | `SoftDeleteSpecificationSqlBuilder` | `SoftDeleteSpecificationSqlBuilder` | `SoftDeleteSpecificationSqlBuilder` | `SoftDeleteRepositoryEF` | `SoftDeletableFunctionalRepositoryMongoDB` |
| Tenancy (tenant-aware repository) | `TenantAwareFunctionalRepositoryADO` | `TenantAwareFunctionalRepositoryADO` | `TenantAwareFunctionalRepositoryADO` | `TenantAwareFunctionalRepositoryDapper` | `TenantAwareFunctionalRepositoryDapper` | `TenantAwareFunctionalRepositoryDapper` | `TenantDbContext` / `TenantDbContextFactory` | `TenantAwareFunctionalRepositoryMongoDB` |

See also, for provider support tables of features not repeated here: [Temporal Tables](temporal-tables.md) (SQL Server native, PostgreSQL via extension, not available on MySQL or MongoDB), [Read/Write Separation](read-write-separation.md), [Database Resilience](database-resilience.md), and the nine Marten-only compliance modules described in [About Encina's data-access providers](../architecture/data-access-providers.md#what-the-compliance-modules-need).

## SQL dialect differences

Verified against the parameterised SQL in each provider's stores (`Outbox/OutboxStoreADO.cs`, `Repository/SpecificationSqlBuilder.cs`, `SoftDelete/SoftDeleteSpecificationSqlBuilder.cs`) and, for identifier quoting on MySQL, the schema DDL in `Sharding/Migrations/AdoMigrationHistoryStore.cs`. ADO.NET and Dapper share the same SQL text per database; EF Core issues its SQL through the EF Core provider's own LINQ translation and is not shown here.

| Aspect | SQL Server | PostgreSQL | MySQL |
|---|---|---|---|
| Parameter marker | `@param` | `@param` | `@param` |
| Paging | `SELECT TOP (@BatchSize) *` | `... LIMIT @BatchSize` | `... LIMIT @BatchSize` |
| Identifier quoting | `[ColumnName]` | `"ColumnName"` | `` `ColumnName` `` |
| Boolean comparison (soft delete filter) | `[IsDeleted] = 0` | `"IsDeleted" = false` | `` `IsDeleted` = 0 `` |
| GUID column read | `reader.GetGuid(...)` against a native `UNIQUEIDENTIFIER` | `reader.GetGuid(...)` against a native `uuid` | `reader.GetGuid(...)`; MySQL has no native GUID type, so the driver's GUID mapping applies to whatever column type the schema uses |

The `Scripts/*.sql` files inside `Encina.ADO.PostgreSQL` and `Encina.ADO.MySQL` (for example `Scripts/001_CreateOutboxMessagesTable.sql`) still contain SQL Server syntax (`[dbo].[OutboxMessages]`, `UNIQUEIDENTIFIER`, `NVARCHAR(MAX)`, a trailing `GO`) copied from the SQL Server package rather than PostgreSQL or MySQL DDL. This table's GUID and identifier-quoting facts come from the C# store code and the migration-history DDL instead, which do use the correct dialect per database. Tracking issue: [#1261](https://github.com/dlrivada/Encina/issues/1261).

## See also

- [About Encina's data-access providers](../architecture/data-access-providers.md) — why there are 10 providers, the provider-coherence rule, and how to choose a family.
- [Messaging in Encina](../messaging/index.md) — the patterns these stores implement (outbox, inbox, saga, scheduling).
- [Multi-Tenancy in Encina](multi-tenancy.md), [Soft Delete Pattern](soft-delete.md), [Temporal Tables](temporal-tables.md), [Read/Write Database Separation](read-write-separation.md), [Database Resilience](database-resilience.md).
