# Retention Module — Database Schema Reference

> **Status**: Reference documentation for provider implementations (Phase 7+).
> These scripts are NOT executed by the core package — each satellite provider creates tables using its own syntax.

## Tables

### RetentionPolicies

Stores retention policy definitions per data category.

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `Id` | `VARCHAR(32)` | PRIMARY KEY | GUID without hyphens |
| `DataCategory` | `VARCHAR(256)` | NOT NULL, UNIQUE | One policy per category |
| `RetentionPeriodTicks` | `BIGINT` | NOT NULL | `TimeSpan.Ticks` for portability |
| `AutoDelete` | `BIT` / `BOOLEAN` | NOT NULL | Provider-specific boolean type |
| `Reason` | `NVARCHAR(1024)` | NULL | Human-readable justification |
| `LegalBasis` | `NVARCHAR(512)` | NULL | GDPR lawful basis reference |
| `PolicyType` | `INT` | NOT NULL | 0=TimeBased, 1=EventBased, 2=ConsentBased |
| `CreatedAtUtc` | DateTimeOffset | NOT NULL | See provider-specific types below |
| `LastModifiedAtUtc` | DateTimeOffset | NULL | |

### RetentionRecords

Tracks the retention lifecycle of individual data entities.

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `Id` | `VARCHAR(32)` | PRIMARY KEY | GUID without hyphens |
| `EntityId` | `VARCHAR(256)` | NOT NULL, INDEX | Data entity identifier |
| `DataCategory` | `VARCHAR(256)` | NOT NULL | Links to RetentionPolicies |
| `PolicyId` | `VARCHAR(32)` | NULL | FK → RetentionPolicies(Id), nullable |
| `CreatedAtUtc` | DateTimeOffset | NOT NULL | When entity was registered |
| `ExpiresAtUtc` | DateTimeOffset | NOT NULL, INDEX | For expiration queries |
| `Status` | `INT` | NOT NULL | 0=Active, 1=Expired, 2=Deleted, 3=UnderLegalHold |
| `DeletedAtUtc` | DateTimeOffset | NULL | When data was actually deleted |
| `LegalHoldId` | `VARCHAR(32)` | NULL | FK → LegalHolds(Id), nullable |

### LegalHolds

Stores litigation holds that suspend data deletion.

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `Id` | `VARCHAR(32)` | PRIMARY KEY | GUID without hyphens |
| `EntityId` | `VARCHAR(256)` | NOT NULL, INDEX | Protected data entity |
| `Reason` | `NVARCHAR(1024)` | NOT NULL | Legal justification |
| `AppliedByUserId` | `VARCHAR(256)` | NULL | Who applied the hold |
| `AppliedAtUtc` | DateTimeOffset | NOT NULL | |
| `ReleasedAtUtc` | DateTimeOffset | NULL | NULL = still active |
| `ReleasedByUserId` | `VARCHAR(256)` | NULL | Who released the hold |

**Active holds query**: `WHERE ReleasedAtUtc IS NULL`

### RetentionAuditEntries

Immutable audit trail for all retention operations.

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `Id` | `VARCHAR(32)` | PRIMARY KEY | GUID without hyphens |
| `Action` | `VARCHAR(128)` | NOT NULL | e.g., "PolicyCreated", "RecordDeleted" |
| `EntityId` | `VARCHAR(256)` | NULL, INDEX | Affected entity, if applicable |
| `DataCategory` | `VARCHAR(256)` | NULL | Affected category, if applicable |
| `Detail` | `NVARCHAR(2048)` | NULL | Additional context |
| `PerformedByUserId` | `VARCHAR(256)` | NULL | Actor identity |
| `OccurredAtUtc` | DateTimeOffset | NOT NULL | When the action occurred |

## Provider-Specific DateTimeOffset Storage

| Provider | SQL Type | Notes |
|----------|----------|-------|
| **SQL Server** | `datetimeoffset(7)` | Native offset support |
| **PostgreSQL** | `timestamptz` | Stored as UTC |
| **MySQL** | `datetime(6)` | Microsecond precision, stored as UTC |
| **MongoDB** | `BsonDateTime` | UTC millisecond precision |

## Provider-Specific Boolean Storage

| Provider | SQL Type | Values |
|----------|----------|--------|
| **SQL Server** | `BIT` | 0 / 1 |
| **PostgreSQL** | `BOOLEAN` | true / false |
| **MySQL** | `TINYINT(1)` | 0 / 1 |

## Key Queries for Provider Implementations

### Get Expired Records (Enforcement)

```sql
-- Core query for enforcement cycle
SELECT * FROM RetentionRecords
WHERE ExpiresAtUtc < @NowUtc
  AND Status = 0  -- Active
ORDER BY ExpiresAtUtc ASC;
```

### Get Expiring Within Window (Alerts)

```sql
SELECT * FROM RetentionRecords
WHERE ExpiresAtUtc BETWEEN @NowUtc AND @WindowEnd
  AND Status = 0  -- Active
ORDER BY ExpiresAtUtc ASC;
```

### Check Active Legal Hold

```sql
SELECT CASE WHEN EXISTS (
    SELECT 1 FROM LegalHolds
    WHERE EntityId = @EntityId AND ReleasedAtUtc IS NULL
) THEN 1 ELSE 0 END;
```

### Get Policy by Category

```sql
SELECT * FROM RetentionPolicies
WHERE DataCategory = @DataCategory;
```

## Recommended Indexes

```sql
-- RetentionRecords
CREATE INDEX IX_RetentionRecords_EntityId ON RetentionRecords (EntityId);
CREATE INDEX IX_RetentionRecords_ExpiresAtUtc ON RetentionRecords (ExpiresAtUtc) WHERE Status = 0;
CREATE INDEX IX_RetentionRecords_Status ON RetentionRecords (Status);

-- RetentionPolicies
CREATE UNIQUE INDEX IX_RetentionPolicies_DataCategory ON RetentionPolicies (DataCategory);

-- LegalHolds
CREATE INDEX IX_LegalHolds_EntityId ON LegalHolds (EntityId);
CREATE INDEX IX_LegalHolds_Active ON LegalHolds (EntityId) WHERE ReleasedAtUtc IS NULL;

-- RetentionAuditEntries
CREATE INDEX IX_RetentionAuditEntries_EntityId ON RetentionAuditEntries (EntityId);
CREATE INDEX IX_RetentionAuditEntries_OccurredAtUtc ON RetentionAuditEntries (OccurredAtUtc);
```

> **Note**: Filtered indexes (`WHERE` clause in index) are supported by SQL Server and PostgreSQL. For MySQL, use standard indexes without the filter.
