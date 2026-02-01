-- =============================================
-- Create AuditLogs table for PostgreSQL
-- For audit trail tracking
-- =============================================

CREATE TABLE IF NOT EXISTS "AuditLogs"
(
    "Id" VARCHAR(256) NOT NULL,
    "EntityType" VARCHAR(256) NOT NULL,
    "EntityId" VARCHAR(256) NOT NULL,
    "Action" INTEGER NOT NULL,
    "UserId" VARCHAR(256) NULL,
    "TimestampUtc" TIMESTAMPTZ NOT NULL,
    "OldValues" TEXT NULL,
    "NewValues" TEXT NULL,
    "CorrelationId" VARCHAR(256) NULL,

    CONSTRAINT "PK_AuditLogs" PRIMARY KEY ("Id")
);

-- Composite index for efficient history lookups by entity
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_Entity" ON "AuditLogs" ("EntityType", "EntityId");

-- Index for time-based queries
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_Timestamp" ON "AuditLogs" ("TimestampUtc");

-- Partial index on UserId for user activity tracking (only non-null values)
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_UserId" ON "AuditLogs" ("UserId") WHERE "UserId" IS NOT NULL;

-- Partial index on CorrelationId for request correlation tracking (only non-null values)
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_CorrelationId" ON "AuditLogs" ("CorrelationId") WHERE "CorrelationId" IS NOT NULL;
