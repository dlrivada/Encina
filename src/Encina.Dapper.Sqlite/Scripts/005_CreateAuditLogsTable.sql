-- =============================================
-- Create AuditLogs table for SQLite
-- For audit trail tracking
-- =============================================

CREATE TABLE IF NOT EXISTS "AuditLogs"
(
    "Id" TEXT NOT NULL,
    "EntityType" TEXT NOT NULL,
    "EntityId" TEXT NOT NULL,
    "Action" INTEGER NOT NULL,
    "UserId" TEXT NULL,
    "TimestampUtc" TEXT NOT NULL,
    "OldValues" TEXT NULL,
    "NewValues" TEXT NULL,
    "CorrelationId" TEXT NULL,

    PRIMARY KEY ("Id")
);

-- Composite index for efficient history lookups by entity
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_Entity" ON "AuditLogs" ("EntityType", "EntityId");

-- Index for time-based queries
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_Timestamp" ON "AuditLogs" ("TimestampUtc");

-- Index on UserId for user activity tracking
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_UserId" ON "AuditLogs" ("UserId");

-- Index on CorrelationId for request correlation tracking
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_CorrelationId" ON "AuditLogs" ("CorrelationId");
