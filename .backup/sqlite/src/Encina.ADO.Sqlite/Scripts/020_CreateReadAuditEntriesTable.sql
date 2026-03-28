CREATE TABLE IF NOT EXISTS ReadAuditEntries (
    Id              TEXT    NOT NULL PRIMARY KEY,
    EntityType      TEXT    NOT NULL,
    EntityId        TEXT    NULL,
    UserId          TEXT    NULL,
    TenantId        TEXT    NULL,
    AccessedAtUtc   TEXT    NOT NULL,
    CorrelationId   TEXT    NULL,
    Purpose         TEXT    NULL,
    AccessMethod    INTEGER NOT NULL DEFAULT 0,
    EntityCount     INTEGER NOT NULL DEFAULT 0,
    Metadata        TEXT    NULL
);

CREATE INDEX IF NOT EXISTS IX_ReadAuditEntries_Entity ON ReadAuditEntries (EntityType, EntityId);
CREATE INDEX IF NOT EXISTS IX_ReadAuditEntries_AccessedAt ON ReadAuditEntries (AccessedAtUtc);
CREATE INDEX IF NOT EXISTS IX_ReadAuditEntries_UserId ON ReadAuditEntries (UserId);
CREATE INDEX IF NOT EXISTS IX_ReadAuditEntries_TenantId ON ReadAuditEntries (TenantId);
CREATE INDEX IF NOT EXISTS IX_ReadAuditEntries_CorrelationId ON ReadAuditEntries (CorrelationId);
CREATE INDEX IF NOT EXISTS IX_ReadAuditEntries_AccessMethod ON ReadAuditEntries (AccessMethod);
