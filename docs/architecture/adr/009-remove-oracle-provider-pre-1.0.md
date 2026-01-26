# ADR-009: Remove Oracle Provider from Pre-1.0 Scope

## Status

**Accepted** - January 2026

## Context

Encina originally planned to support 16 database providers across three data access technologies:

- **ADO.NET**: SQLite, SqlServer, PostgreSQL, MySQL, Oracle (5)
- **Dapper**: SQLite, SqlServer, PostgreSQL, MySQL, Oracle (5)
- **EF Core**: SQLite, SqlServer, PostgreSQL, MySQL, Oracle (5)
- **MongoDB**: (1)

During development, Oracle proved to be significantly more problematic than other providers, requiring specialized handling for nearly every database operation.

### Oracle-Specific Technical Challenges

| Aspect | Standard Providers | Oracle |
|--------|-------------------|--------|
| Parameter prefix | `@param` | `:param` |
| GUID storage | Native/VARCHAR | `RAW(16)` requiring byte[] conversion |
| LIMIT syntax | `TOP`/`LIMIT n` | `FETCH FIRST n ROWS ONLY` |
| Identifier case | Flexible | UPPERCASE by default |
| Parameter binding | Named by default | Positional (requires `BindByName = true`) |
| DateTime precision | 7 fractional digits | 6 fractional digits |
| Boolean type | Native/bit | `NUMBER(1)` |

### Maintenance Burden

Estimated effort distribution across providers:

```
PostgreSQL ████████░░░░░░░░░░░░ 10%
MySQL      ████████░░░░░░░░░░░░ 10%
SQLite     ████████░░░░░░░░░░░░ 10%
SQL Server ████████████░░░░░░░░ 15%
Oracle     ██████████████████████████████████████████████ 45%
```

Every new feature requires Oracle-specific implementation:
- Custom SQL syntax
- Type conversions
- Parameter handling
- Schema definitions

### Testing Infrastructure Impact

- Oracle container startup: 20-30 seconds (vs 2-5s for others)
- Test suite execution: ~10x slower with Oracle tests
- Resource exhaustion: 30+ orphaned containers during parallel runs
- CI/CD costs: Significantly higher due to longer test times

### Market Analysis

| Database | Market Share (2025) | Trend |
|----------|---------------------|-------|
| PostgreSQL | ~25% | ↑ Growing |
| MySQL | ~20% | → Stable |
| SQL Server | ~18% | → Stable |
| MongoDB | ~12% | ↑ Growing |
| SQLite | ~10% | ↑ Growing |
| **Oracle** | ~8% | **↓ Declining** |

**Key observations:**
- New .NET projects rarely choose Oracle
- Enterprises actively migrating to PostgreSQL/cloud databases
- Oracle licensing ($17,500-47,500/CPU) drives migration
- Startups and scale-ups avoid Oracle entirely

## Decision

**Remove Oracle provider support from the pre-1.0 release scope.**

### Implementation

1. Move all Oracle code to `.backup/oracle/` for preservation
2. Remove Oracle from solution and build configuration
3. Update documentation to reflect 12 providers (not 16)
4. Remove Oracle from CI/CD pipelines
5. Remove Oracle from Docker infrastructure

### New Provider Matrix

| Technology | Providers |
|------------|-----------|
| ADO.NET | SQLite, SqlServer, PostgreSQL, MySQL |
| Dapper | SQLite, SqlServer, PostgreSQL, MySQL |
| EF Core | SQLite, SqlServer, PostgreSQL, MySQL |
| MongoDB | MongoDB |
| **Total** | **13 providers** |

## Consequences

### Positive

1. **Faster development velocity** - 45% less provider-specific work per feature
2. **Faster CI/CD** - Test suite runs significantly faster
3. **Simpler codebase** - Fewer special cases and workarounds
4. **More time for features** - Focus on functionality over provider parity
5. **Earlier release** - v1.0 can ship sooner
6. **Aligned with market** - Focus on growing/stable databases

### Negative

1. **No enterprise Oracle support at launch** - May limit adoption in some organizations
2. **Feature parity perception** - Some may see fewer providers as less mature
3. **Future restoration effort** - Re-adding Oracle will require work

### Mitigation

1. **Preserve all Oracle code** in `.backup/oracle/` with full history
2. **Document restoration path** for future Oracle support
3. **Post-1.0 options**:
   - Community-maintained Oracle packages
   - Enterprise Edition with Oracle support
   - Official Oracle packages if demand materializes

## Alternatives Considered

### 1. Continue Supporting Oracle

**Rejected** - The maintenance cost is disproportionate to the market demand. Every feature takes 45% longer to implement and test.

### 2. Oracle as Separate, Lower-Priority Package

**Rejected for pre-1.0** - Maintaining two-tier support would still require ongoing effort and creates user confusion about what's "officially supported."

### 3. Community-Only Oracle Support

**Considered for post-1.0** - This remains a viable option after the initial release, where community members could maintain Oracle packages with guidance from core documentation.

## Related

- Issue: [#541](https://github.com/dlrivada/Encina/issues/541) - Remove Oracle provider from pre-1.0 scope
- Backup location: `.backup/oracle/`

## References

- [Oracle ODP.NET Documentation](https://docs.oracle.com/en/database/oracle/oracle-database/19/odpnt/)
- [DB-Engines Ranking](https://db-engines.com/en/ranking)
- [Oracle Licensing Guide](https://www.oracle.com/corporate/pricing/)
