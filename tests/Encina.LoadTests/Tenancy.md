# Load Tests - Multi-Tenancy

## Status: Not Implemented

## Justification

Load tests for multi-tenancy across all providers are not implemented for the following reasons:

### 1. Tenancy Overhead is Minimal

The multi-tenancy implementation adds a single `WHERE TenantId = @TenantId` clause to SQL queries (or an equivalent filter in MongoDB). This adds negligible overhead compared to:

- Database I/O latency
- Network round-trip time
- Connection pooling overhead

### 2. No Distinct Performance Characteristics

Multi-tenancy doesn't introduce:

- New threading/concurrency patterns
- Memory allocation patterns worth benchmarking
- CPU-intensive operations

### 3. Provider Load Tests Would Test Database, Not Tenancy

Load testing tenancy would actually be testing the underlying database performance, not the tenancy implementation itself. The existing provider-specific load tests already cover database performance.

### 4. Recommended Alternative

If tenant isolation under load is a concern:

1. Use the existing NBomber scenarios in `Encina.NBomber/`
2. Configure multiple concurrent users with different tenant contexts
3. Monitor for cross-tenant data leakage (a functional concern, not performance)

### 5. Future Consideration

If load testing is needed, focus on:

- Concurrent writes from multiple tenants
- Query performance with large datasets partitioned by TenantId
- Index effectiveness for TenantId columns

## Providers Not Load Tested

- ADO: Sqlite, SqlServer, PostgreSQL, MySQL, Oracle
- Dapper: Sqlite, SqlServer, PostgreSQL, MySQL, Oracle
- EntityFrameworkCore
- MongoDB

## Related Files

- `tests/Encina.NBomber/` - NBomber load testing scenarios
- `src/Encina.*/Tenancy/` - Tenancy implementations

## Date: 2026-01-23

## Issue: #282
