# ADR-024: Remove SQLite Provider from Pre-1.0 Scope

## Status

**Accepted** - March 2026

## Context

Following [ADR-009](009-remove-oracle-provider-pre-1.0.md) (Oracle removal), Encina supported 13 database providers across three data access technologies plus MongoDB:

- **ADO.NET**: SQLite, SqlServer, PostgreSQL, MySQL (4)
- **Dapper**: SQLite, SqlServer, PostgreSQL, MySQL (4)
- **EF Core**: SQLite, SqlServer, PostgreSQL, MySQL (4)
- **MongoDB**: (1)

SQLite's greatest strength — its simplicity as an embedded, zero-configuration database — is also its fundamental weakness for the feature set Encina provides. The gap between SQLite's capabilities and what a production messaging/persistence framework requires creates a disproportionate maintenance burden, following the same pattern that led to Oracle's removal.

### SQLite-Specific Technical Challenges

| Aspect | Server-Based Providers | SQLite |
|--------|----------------------|--------|
| DateTime storage | Native datetime types | Text (ISO 8601 strings), format-sensitive comparisons |
| `datetime('now')` | N/A | Incompatible format with ISO 8601 (`2026-01-05 12:30:00` vs `2026-01-05T12:30:00.0000000`) |
| DateTimeOffset | Native support | No support (stored as text, requires manual gymnastics) |
| GUID storage | Native/binary | String representation (no native GUID type) |
| Concurrent writes | Full support | Single-writer (WAL mode helps but does not eliminate limitation) |
| Distributed scenarios | Client-server architecture | In-process only (embedded database) |
| ALTER TABLE | Full DDL support | Limited (`ADD COLUMN` only, no `DROP COLUMN`, no `IF NOT EXISTS`) |
| Boolean type | Native/bit | `0`/`1` integer |
| Network health checks | TCP/connection-based | Meaningless (file-based) |
| Read/Write separation | Replica support | Not applicable (single file) |
| Database resilience | Retry/circuit breaker over network | Not applicable (in-process) |

### Maintenance Burden

Every new feature required SQLite-specific workarounds:

1. **DateTime comparisons**: All Dapper SQLite stores must use parameterized `@NowUtc` with `DateTime.UtcNow` from C# instead of SQL's `datetime('now')` due to format incompatibility
2. **Schema management**: No `ALTER TABLE ADD COLUMN IF NOT EXISTS`, requiring existence checks before modifications
3. **GUID handling**: String-based storage requiring explicit conversion logic
4. **Connection management**: In-memory databases require shared connection lifecycle management that no other provider needs
5. **Feature exclusions**: Read/Write Separation, Database Resilience, Network Health Checks are meaningless for SQLite, requiring conditional feature flags or no-op implementations

Estimated effort distribution across remaining providers:

```
PostgreSQL ██████████████░░░░░░ 15%
MySQL      ██████████████░░░░░░ 15%
SQL Server ██████████████░░░░░░ 15%
MongoDB    ██████████░░░░░░░░░░ 10%
SQLite     ██████████████████████████████████████████████ 45%
```

### Testing Infrastructure Impact

SQLite's in-memory database model created unique testing complications not shared by any other provider:

- **Shared connection requirement**: `SqliteFixture` uses `Cache=Shared` in-memory mode; `CreateConnection()` returns the SAME shared connection object
- **Disposal hazards**: Three categories of disposal bugs discovered and fixed — direct disposal, health check disposal via factory, and wrapper disposal (e.g., `SchemaValidatingConnection`)
- **Parallelization disabled**: All SQLite collections require `DisableParallelization = true` due to single-writer constraint on shared in-memory databases
- **Unique fixture patterns**: Custom disposal rules, special connection factory registration, and documentation of patterns not needed by any other provider
- **Cognitive overhead**: Every developer working on integration tests must learn SQLite-specific rules that do not apply to the other 9 providers

### Feature Incompatibility

Several Encina features are architecturally incompatible with SQLite's embedded nature:

| Feature | Server-Based Providers | SQLite |
|---------|----------------------|--------|
| Read/Write Separation | Replicas distribute read load | Single file, no replicas |
| Database Resilience | Retry transient network failures | No network, no transient failures |
| Health Checks (network) | TCP probe to verify connectivity | File existence check only |
| Distributed Locks | `sp_getapplock`, `pg_advisory_lock` | No equivalent mechanism |
| Multi-Tenant (DB-per-tenant) | Connection string switching | File path switching (fragile) |
| Connection Pooling | Connection pool management | Single shared connection |

## Decision

**Remove SQLite provider support from the pre-1.0 release scope.**

This follows the same precedent established by [ADR-009](009-remove-oracle-provider-pre-1.0.md) for Oracle: when a provider's maintenance cost is disproportionate to its value for the target audience (production distributed systems), removal is the pragmatic choice during pre-1.0 development.

### Implementation

1. Move all SQLite source packages to `.backup/sqlite/` for preservation
2. Remove SQLite from solution and build configuration
3. Update documentation to reflect 10 providers (not 13)
4. Remove SQLite from CI/CD pipelines and Docker infrastructure
5. Delete SQLite integration tests (already completed prior to this ADR)
6. Update `CLAUDE.md` provider matrix and collection fixture references

### New Provider Matrix

| Technology | Providers |
|------------|-----------|
| ADO.NET | SqlServer, PostgreSQL, MySQL |
| Dapper | SqlServer, PostgreSQL, MySQL |
| EF Core | SqlServer, PostgreSQL, MySQL |
| MongoDB | MongoDB |
| **Total** | **10 providers** |

## Consequences

### Positive

1. **Faster development velocity** - Eliminates ~45% of provider-specific workaround effort per feature
2. **Simpler testing** - Removes unique shared-connection disposal rules, parallelization constraints, and SQLite-specific fixture patterns
3. **Cleaner architecture** - No need for feature flags or no-op implementations for network-dependent features (resilience, health checks, read/write separation)
4. **Faster CI/CD** - Fewer test collections, no SQLite-specific parallelization bottlenecks
5. **Reduced cognitive load** - Developers no longer need to learn SQLite-specific rules that differ from all other providers
6. **Aligned with target audience** - Production distributed systems use server-based databases, not embedded SQLite

### Negative

1. **No embedded database option** - Users wanting a simple, zero-config database for development or small deployments lose that option
2. **Feature parity perception** - 10 providers may appear less comprehensive than 13
3. **Future restoration effort** - Re-adding SQLite will require adapting to whatever API changes have occurred

### Mitigation

1. **Preserve all SQLite code** in `.backup/sqlite/` with full history
2. **Document restoration path** for future SQLite support
3. **Post-1.0 options**:
   - Partial SQLite support (core stores only, excluding network-dependent features)
   - Community-maintained SQLite packages
   - Official SQLite packages if demand materializes
4. **Development alternative** - Users can use PostgreSQL or MySQL via Docker for local development with minimal overhead

## Alternatives Considered

### 1. Continue Supporting SQLite

**Rejected** - The maintenance cost is disproportionate to the value. SQLite's embedded nature makes it architecturally incompatible with several Encina features (distributed locks, read/write separation, resilience), requiring workarounds or feature exclusions that complicate the codebase.

### 2. SQLite as a Reduced-Feature Provider

**Rejected for pre-1.0** - Maintaining a provider that supports only a subset of features creates user confusion about what works where, requires conditional feature detection, and still demands SQLite-specific workarounds for the features it does support.

### 3. SQLite for Testing/Development Only

**Rejected for pre-1.0** - Even as a development-only provider, the DateTime format incompatibilities, shared connection requirements, and DDL limitations impose the same maintenance burden. Docker-based PostgreSQL/MySQL provide a better development experience with production parity.

## Related

- [ADR-009](009-remove-oracle-provider-pre-1.0.md) - Remove Oracle provider from pre-1.0 scope (precedent)
- Backup location: `.backup/sqlite/`

## References

- [SQLite Datatypes Documentation](https://www.sqlite.org/datatype3.html)
- [SQLite ALTER TABLE Limitations](https://www.sqlite.org/lang_altertable.html)
- [SQLite WAL Mode](https://www.sqlite.org/wal.html)
