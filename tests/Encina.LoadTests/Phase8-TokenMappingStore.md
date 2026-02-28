# LoadTests - Phase 8 TokenMappingStore Implementations

## Status: Not Implemented

## Justification

The `ITokenMappingStore` implementations across all 13 database providers are thin data access wrappers containing 2-5 lines of SQL per operation. Load testing these wrappers would primarily stress the underlying database engines rather than the Encina store code itself.

### 1. Thin Wrappers Over Database Operations

Each `TokenMappingStore` implementation (ADO.NET, Dapper, EF Core, MongoDB) follows the same minimal pattern:

- `StoreTokenAsync`: Single INSERT/upsert statement
- `GetOriginalValueAsync`: Single SELECT by token
- `GetTokenAsync`: Single SELECT by original value hash
- `RemoveTokenAsync`: Single DELETE by token
- `RemoveBySubjectAsync`: Single DELETE by subject identifier

The C# code is a direct passthrough to the database. Load testing would measure database throughput, not store implementation quality.

### 2. Connection Management Is External

Connection pooling, transaction coordination, and resource lifecycle are handled by:

- **ADO.NET**: `DbConnection` pooling managed by the ADO.NET provider
- **Dapper**: Delegates to ADO.NET connection pooling
- **EF Core**: `DbContext` pooling and connection management
- **MongoDB**: `MongoClient` connection pooling

The store implementations do not manage connections, pools, or transactions. They receive a connection/context and execute a single operation. Load testing would validate the database library's connection management, not the store code.

### 3. SQL Queries Are Trivially Simple

The SQL in each store is single-table, single-operation, parameterized queries:

```sql
-- Typical TokenMappingStore query complexity
INSERT INTO token_mappings (token, original_value_hash, ...) VALUES (@Token, @Hash, ...);
SELECT original_value FROM token_mappings WHERE token = @Token;
DELETE FROM token_mappings WHERE subject_id = @SubjectId;
```

There are no joins, subqueries, aggregations, or complex WHERE clauses that could exhibit performance pathologies under load. The query plans are trivial for any database optimizer.

### 4. Adequate Coverage from Other Test Types

- **Unit Tests**: Verify SQL generation correctness, parameter binding, and result mapping for all 13 providers. Cover edge cases like duplicate tokens, missing mappings, and concurrent store/retrieve operations
- **Guard Tests**: Verify null/empty parameter rejection for all public methods across all providers
- **Property Tests**: Verify store invariants (store-then-retrieve returns original, remove-then-get returns null) with randomized token values across all providers
- **Contract Tests**: Verify all 13 `ITokenMappingStore` implementations satisfy the interface contract identically
- **Integration Tests**: Verify operations against real database instances (Docker/Testcontainers) for all supported databases

### 5. Recommended Alternative

If load testing becomes necessary (e.g., to validate token lookup performance at scale for a specific deployment):

1. Use the existing Docker infrastructure to spin up database containers
2. Pre-populate the `token_mappings` table with realistic data volumes (100K, 1M, 10M rows)
3. Run concurrent `GetOriginalValueAsync` lookups using NBomber or a similar load testing framework
4. Focus on database-level metrics (query latency percentiles, connection pool utilization) rather than store-level metrics
5. This type of testing is better categorized as **database capacity planning** rather than store code validation

```bash
# Future load test execution (if implemented)
dotnet run --project tests/Encina.LoadTests -- --scenario TokenMappingStore --database SqlServer --rows 1000000
```

## Related Files

- `src/Encina.ADO.SqlServer/Anonymization/` - ADO.NET SQL Server implementation
- `src/Encina.ADO.PostgreSQL/Anonymization/` - ADO.NET PostgreSQL implementation
- `src/Encina.ADO.MySQL/Anonymization/` - ADO.NET MySQL implementation
- `src/Encina.ADO.Sqlite/Anonymization/` - ADO.NET SQLite implementation
- `src/Encina.Dapper.SqlServer/Anonymization/` - Dapper SQL Server implementation
- `src/Encina.Dapper.PostgreSQL/Anonymization/` - Dapper PostgreSQL implementation
- `src/Encina.Dapper.MySQL/Anonymization/` - Dapper MySQL implementation
- `src/Encina.Dapper.Sqlite/Anonymization/` - Dapper SQLite implementation
- `src/Encina.EntityFrameworkCore/Anonymization/` - EF Core implementation
- `src/Encina.MongoDB/Anonymization/` - MongoDB implementation
- `tests/Encina.UnitTests/Compliance/Anonymization/` - Unit tests
- `tests/Encina.IntegrationTests/Compliance/Anonymization/` - Integration tests

## Date: 2026-02-28
## Issue: #407
