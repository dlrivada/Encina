# IntegrationTests - EF Core MySQL Sharding

## Status: Deferred

## Justification

### 1. Pomelo.EntityFrameworkCore.MySql Not Yet Compatible with EF Core 10

The Pomelo MySQL provider for EF Core has not yet released a version compatible with .NET 10 / EF Core 10.
The package reference is commented out in `Encina.IntegrationTests.csproj`:

```xml
<!-- Pomelo.EntityFrameworkCore.MySql: Waiting for v10.0.0 (EF Core 10 support) -->
<!-- <PackageReference Include="Pomelo.EntityFrameworkCore.MySql" /> -->
```

The `AddEncinaEFCoreShardingMySql<TContext, TEntity, TId>()` registration method requires a
`Pomelo.EntityFrameworkCore.MySql.Infrastructure.MySqlServerVersion` parameter which is unavailable.

### 2. Tests Are Pre-Written and Ready

The test files for EF Core MySQL sharding (CRUD, ScatterGather, RoutingVerification) follow the exact same
pattern as the other EF Core providers (Sqlite, SqlServer, PostgreSQL). When Pomelo v10.0.0 is released:

1. Uncomment the Pomelo package reference in `Encina.IntegrationTests.csproj`
2. Create the 3 test files using the same pattern as `EFCore/SqlServer/` tests
3. Replace `AddEncinaEFCoreShardingSqlServer` with `AddEncinaEFCoreShardingMySql` + server version delegate

### 3. Adequate Coverage from Other Test Types

- **ADO MySQL**: Full CRUD + routing integration tests verify MySQL sharding works at the database level
- **Dapper MySQL**: Full CRUD + routing integration tests verify MySQL sharding with Dapper
- **EF Core Sqlite/SqlServer/PostgreSQL**: Verify EF Core sharding infrastructure works correctly
- **Unit Tests**: Cover EF Core sharding DI registration, factory, and repository logic

## Related Files

- `src/Encina.EntityFrameworkCore/Sharding/ShardingServiceCollectionExtensions.cs` - MySQL registration method
- `tests/Encina.IntegrationTests/Sharding/EFCore/SqlServer/` - Reference pattern for MySQL tests
- `tests/Encina.IntegrationTests/Sharding/ADO/MySQL/` - Working MySQL integration tests via ADO
- `tests/Encina.IntegrationTests/Sharding/Dapper/MySQL/` - Working MySQL integration tests via Dapper

## Date: 2026-02-10
## Issue: Pomelo.EntityFrameworkCore.MySql v10.0.0 release tracking
