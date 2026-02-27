-- =============================================
-- Create DSRAuditEntries table for SQLite
-- For GDPR Data Subject Rights audit trail
-- =============================================

CREATE TABLE IF NOT EXISTS DSRAuditEntries
(
    Id                TEXT NOT NULL PRIMARY KEY,
    DSRRequestId      TEXT NOT NULL,
    Action            TEXT NOT NULL,
    Detail            TEXT NULL,
    PerformedByUserId TEXT NULL,
    OccurredAtUtc     TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_DSRAuditEntries_DSRRequestId ON DSRAuditEntries (DSRRequestId);
CREATE INDEX IF NOT EXISTS IX_DSRAuditEntries_OccurredAtUtc ON DSRAuditEntries (OccurredAtUtc);
